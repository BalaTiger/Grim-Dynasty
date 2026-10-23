param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('valid', 'eof', 'invalid', 'dispose')]
    [string]$Mode
)

# Self-owned fixture only. Its controller supplies the real stdin command/EOF.
# Mode changes only the deadline; it never simulates a ReaJobControl result.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$ownedJob = $null
$control = $null
$child = $null
$childStarted = $false
$workerExitCode = 0

function Write-ControlFixtureMessage {
    param([object]$Message)
    [Console]::Out.WriteLine(($Message | ConvertTo-Json -Compress -Depth 5))
    [Console]::Out.Flush()
}

try {
    $fixedScriptRoot = 'C:\Users\zhuzi\Documents\Grim-Dynasty\tools\static_analysis'
    $fixedNode = 'D:\NodeJS\node.exe'
    $fixedFixture = Join-Path $fixedScriptRoot 'owned_job_fixture.mjs'
    if ([IntPtr]::Size -ne 8 -or [Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
        throw 'This fixture requires Windows x64.'
    }
    if (-not [string]::Equals([IO.Path]::GetFullPath($PSScriptRoot), $fixedScriptRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'The worker must run from its fixed project directory.'
    }
    if (-not (Test-Path -LiteralPath $fixedNode -PathType Leaf) -or
        -not (Test-Path -LiteralPath $fixedFixture -PathType Leaf)) {
        throw 'The fixed Node executable and fixture must already exist.'
    }
    Add-Type -Path (Join-Path $fixedScriptRoot 'ReaOwnedJob.cs') | Out-Null
    $runIdentity = [Guid]::NewGuid()
    $childNonce = $runIdentity.ToString('N')
    $controlNonce = $runIdentity.ToString('D')

    $start = [Diagnostics.ProcessStartInfo]::new()
    $start.FileName = $fixedNode
    $start.Arguments = '"' + $fixedFixture + '" control ' + $childNonce
    $start.WorkingDirectory = [IO.Path]::GetFullPath((Join-Path $fixedScriptRoot '../..'))
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardInput = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    # Do not inherit NODE_OPTIONS/NODE_PATH or a caller's preload configuration.
    $start.EnvironmentVariables.Clear()
    foreach ($entryName in @('SystemRoot', 'WINDIR', 'TEMP', 'TMP')) {
        $entryValue = [Environment]::GetEnvironmentVariable($entryName)
        if (-not [string]::IsNullOrEmpty($entryValue)) {
            $start.EnvironmentVariables[$entryName] = $entryValue
        }
    }
    if ([string]::IsNullOrEmpty($start.EnvironmentVariables['SystemRoot'])) {
        throw 'SystemRoot is required for the fixed Windows fixture.'
    }
    $child = [Diagnostics.Process]::new()
    $child.StartInfo = $start
    if (-not $child.Start()) { throw 'The fixed Node control fixture did not start.' }
    $childStarted = $true
    # Force retention of this process's handle before reading its creation time.
    # Cleanup uses this Process instance, never a reopened PID or a process tree.
    [void]$child.Handle
    $childCreatedUtc = $child.StartTime.ToUniversalTime()
    $stderrTask = $child.StandardError.ReadToEndAsync()
    # This is a redirected child pipe, not Console.In.ReadLineAsync.
    $readyTask = $child.StandardOutput.ReadLineAsync()
    if (-not $readyTask.Wait(5000)) { throw 'The Node control fixture did not become ready within five seconds.' }
    $readyLine = $readyTask.GetAwaiter().GetResult()
    if ($null -eq $readyLine) { throw 'The Node control fixture closed stdout before ready.' }
    $ready = $readyLine | ConvertFrom-Json
    if ($ready.event -cne 'ready' -or $ready.mode -cne 'control' -or
        $ready.nonce -cne $childNonce -or [int]$ready.pid -ne $child.Id -or $child.HasExited) {
        throw 'The Node control fixture ready identity differs.'
    }
    $ownedJob = [GrimDynasty.StaticAnalysis.ReaOwnedJob]::new(
        $child.Id, $childCreatedUtc, $fixedNode)
    $initialProbe = $ownedJob.Probe($child.Id)
    if (-not $initialProbe.InJob -or $initialProbe.HasExited) {
        throw 'The Node control fixture is not live in the exact owned Job.'
    }
    $control = [GrimDynasty.StaticAnalysis.ReaJobControl]::new($ownedJob, $controlNonce)
    Write-ControlFixtureMessage ([ordered]@{
        event = 'monitor-ready'
        pid = $PID
        rootPid = $child.Id
        controlNonce = $controlNonce
    })

    $samplingWatch = [Diagnostics.Stopwatch]::StartNew()
    $deadlineMilliseconds = if ($Mode -ceq 'dispose') { 4000 } else { 10000 }
    $samplePublished = $false
    $timedOut = $false
    while ($true) {
        $members = $ownedJob.QueryMembers()
        if (-not $samplePublished) {
            $liveRoot = @($members.Members | Where-Object {
                $_.ProcessId -eq $child.Id -and $_.InJob -and -not $_.HasExited
            })
            if ($liveRoot.Count -ne 1) { throw 'The first control sample did not contain the live owned root.' }
            Write-ControlFixtureMessage ([ordered]@{
                event = 'sample-ready'
                pid = $PID
                rootPid = $child.Id
                activeProcesses = $members.ActiveProcesses
                sampleElapsedMs = $samplingWatch.ElapsedMilliseconds
            })
            $samplePublished = $true
        }
        if ($control.StopRequested -or $null -ne $control.Failure) { break }
        if ($samplingWatch.ElapsedMilliseconds -ge $deadlineMilliseconds) {
            $timedOut = $Mode -cne 'dispose'
            break
        }
        Start-Sleep -Milliseconds 50
    }

    if ($control.StopRequested -or $null -ne $control.Failure) {
        # Observe whether the control reader itself terminated the Job before
        # the finally block performs any independent cleanup.
        [void]$child.WaitForExit(3000)
    }
    $disposeWatch = [Diagnostics.Stopwatch]::StartNew()
    $control.Dispose()
    $disposeWatch.Stop()
    # Dispose serializes with a dispatch, so any dispatch failure is visible here.
    $stopRequested = $control.StopRequested
    $failure = $control.Failure
    $control = $null
    $childExited = $child.HasExited
    Write-ControlFixtureMessage ([ordered]@{
        event = 'control-result'
        pid = $PID
        rootPid = $child.Id
        stopRequested = $stopRequested
        failure = $failure
        childExited = $childExited
        disposeElapsedMs = $disposeWatch.Elapsed.TotalMilliseconds
        timedOut = $timedOut
    })
    if ($timedOut -and $Mode -cne 'dispose') { $workerExitCode = 1 }
}
catch {
    $workerExitCode = 1
    Write-ControlFixtureMessage ([ordered]@{
        event = 'error'
        pid = $PID
        message = $_.Exception.Message
    })
}
finally {
    # ReaJobControl must stop dispatching before its Job handle is disposed.
    if ($null -ne $control) {
        try { $control.Dispose() }
        catch {
            $workerExitCode = 1
            Write-ControlFixtureMessage ([ordered]@{ event = 'error'; pid = $PID; message = $_.Exception.Message })
        }
    }
    if ($null -ne $ownedJob) {
        try { $ownedJob.Dispose() }
        catch {
            $workerExitCode = 1
            Write-ControlFixtureMessage ([ordered]@{ event = 'error'; pid = $PID; message = $_.Exception.Message })
        }
    }
    if ($null -ne $child) {
        try {
            if ($childStarted -and -not $child.WaitForExit(3000)) {
                # This retained creation handle belongs only to this fixture.
                $child.Kill()
                if (-not $child.WaitForExit(3000)) { throw 'Retained fixture process did not exit after cleanup.' }
            }
        }
        catch {
            $workerExitCode = 1
            Write-ControlFixtureMessage ([ordered]@{ event = 'error'; pid = $PID; message = $_.Exception.Message })
        }
        finally { $child.Dispose() }
    }
}
exit $workerExitCode
