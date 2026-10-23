param(
    [string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$node = 'D:\NodeJS\node.exe'
$fixture = Join-Path $PSScriptRoot 'owned_job_fixture.mjs'
$nestedWorker = Join-Path $PSScriptRoot 'test_owned_job_nested_worker.ps1'
$controlWorker = Join-Path $PSScriptRoot 'test_owned_job_control_worker.ps1'
$nativeSource = Join-Path $PSScriptRoot 'ReaOwnedJob.cs'
$powershell = Join-Path $env:SystemRoot 'System32/WindowsPowerShell/v1.0/powershell.exe'
$runNonce = [Guid]::NewGuid().ToString('N')
if ([string]::IsNullOrEmpty($OutputDirectory)) {
    $OutputDirectory = Join-Path $workspace ('.tooling/owned-job-fixture-' + $runNonce)
}
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (-not $output.StartsWith($workspace + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Fixture output must be inside the project.'
}
if (Test-Path -LiteralPath $output) { throw 'Fixture output already exists; refusing overwrite.' }
[IO.Directory]::CreateDirectory($output) | Out-Null
$records = [Collections.Generic.List[object]]::new()
$handles = [Collections.Generic.List[object]]::new()
$jobs = [Collections.Generic.List[object]]::new()
$failure = $null
$started = [DateTime]::UtcNow
$assertions = 0

function Assert-True([bool]$Value, [string]$Label) {
    if (-not $Value) { throw ('Assertion failed: ' + $Label) }
    $script:assertions++
    $records.Add([ordered]@{ check=$Label; passed=$true; utc=[DateTime]::UtcNow.ToString('o') })
}
function Expect-Throw([scriptblock]$Action, [string]$Label) {
    $thrown = $null
    try { & $Action | Out-Null } catch { $thrown = $_.Exception.Message }
    Assert-True ($null -ne $thrown) $Label
    $records.Add([ordered]@{ expected_error=$Label; message=$thrown })
}
function Read-Event($Process, [string]$ExpectedEvent) {
    $read = $Process.StandardOutput.ReadLineAsync()
    if (-not $read.Wait(10000)) { throw ('Fixture event timeout: ' + $ExpectedEvent) }
    if ($null -eq $read.Result) { throw ('Fixture output closed before: ' + $ExpectedEvent) }
    $event = $read.Result | ConvertFrom-Json
    if ($event.event -ne $ExpectedEvent) { throw ('Expected ' + $ExpectedEvent + '; got ' + $read.Result) }
    return $event
}
function Start-Node([string]$Mode) {
    $nonce = [Guid]::NewGuid().ToString('N')
    $info = [Diagnostics.ProcessStartInfo]::new()
    $info.FileName = $node
    $info.Arguments = '"' + $fixture + '" ' + $Mode + ' ' + $nonce
    $info.WorkingDirectory = $workspace
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.RedirectStandardInput = $true
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    $info.EnvironmentVariables.Remove('NODE_OPTIONS')
    $info.EnvironmentVariables.Remove('NODE_PATH')
    $process = [Diagnostics.Process]::Start($info)
    # Retain the exact creation object/handle; cleanup never looks up a new PID.
    $null = $process.Handle
    $handles.Add($process)
    $hello = Read-Event $process 'ready'
    Assert-True ($hello.pid -eq $process.Id -and $hello.nonce -eq $nonce -and $hello.mode -eq $Mode) ('Exact fixture hello: ' + $Mode)
    $row = Get-CimInstance Win32_Process -Filter ('ProcessId = ' + $process.Id)
    $argv = [GrimDynasty.StaticAnalysis.ReaOwnedJob]::SplitCommandLine([string]$row.CommandLine)
    Assert-True ($argv.Length -eq 4 -and $argv[0] -eq $node -and $argv[1] -eq $fixture -and $argv[2] -eq $Mode -and $argv[3] -eq $nonce) ('Exact fixture argv/nonce: ' + $Mode)
    Assert-True ([int]$row.ParentProcessId -eq $PID) ('Fixture direct controller: ' + $Mode)
    return $process
}
function New-OwnedJob($Process) {
    $job = [GrimDynasty.StaticAnalysis.ReaOwnedJob]::new($Process.Id, $Process.StartTime.ToUniversalTime(), $node)
    $jobs.Add($job)
    Assert-True ($job.LimitFlags -eq 0x2000) 'Job readback has only kill-on-close'
    Assert-True ($job.Probe($Process.Id).InJob) 'Root belongs to the specific Job'
    Assert-True (-not $job.Probe($PID).InJob) 'Fixture supervisor stays outside the Job'
    Assert-True ($job.QueryMembers().Members.Length -eq 1) 'Assigned gated fixture Job initially contains only its root'
    return $job
}
function Open-Leaf($Root) {
    $Root.StandardInput.WriteLine('release')
    $Root.StandardInput.Flush()
    $leafEvent = Read-Event $Root 'leaf'
    $leaf = [Diagnostics.Process]::GetProcessById([int]$leafEvent.pid)
    $null = $leaf.Handle
    $handles.Add($leaf)
    $parentExit = Read-Event $Root 'parent-exit'
    Assert-True ($parentExit.pid -eq $leafEvent.parentPid -and $parentExit.code -eq 0) 'Short-lived intermediate parent exited normally'
    return $leaf
}
function Wait-Exited($Process, [string]$Label) {
    Assert-True ($Process.WaitForExit(5000)) $Label
}
function Wait-JobEmpty($Job, [int]$TimeoutMilliseconds) {
    $clock = [Diagnostics.Stopwatch]::StartNew()
    do {
        $members = $Job.QueryMembers()
        if ($members.ActiveProcesses -eq 0 -and $members.NativeProcessIds.Length -eq 0 -and $members.Members.Length -eq 0) { return }
        if ($clock.ElapsedMilliseconds -ge $TimeoutMilliseconds) { throw 'Owned Job is not empty; cleanup remains unconfirmed.' }
        Start-Sleep -Milliseconds 25
    } while ($true)
}

try {
    Add-Type -Path $nativeSource
    $control = Start-Node 'control'
    $candidate = Start-Node 'root'
    Expect-Throw {
        $bad = [GrimDynasty.StaticAnalysis.ReaOwnedJob]::new($candidate.Id, $candidate.StartTime.ToUniversalTime().AddSeconds(1), $node)
        $bad.Dispose()
    } 'Creation mismatch refuses assignment'
    Assert-True (-not $candidate.HasExited) 'Rejected candidate remains gated/alive'
    Expect-Throw {
        $bad = [GrimDynasty.StaticAnalysis.ReaOwnedJob]::new($candidate.Id, $candidate.StartTime.ToUniversalTime(), $powershell)
        $bad.Dispose()
    } 'Executable mismatch refuses assignment'
    $job = New-OwnedJob $candidate
    $probeError = $null
    $typedCatchSelected = $false
    try { $job.Probe([int]::MaxValue) | Out-Null }
    catch [ComponentModel.Win32Exception] {
        $typedCatchSelected = $true
        $probeError = $_.Exception
    }
    catch { $probeError = $_.Exception }
    Assert-True ($null -ne $probeError) 'Invalid PID probe fails explicitly'
    $nativeProbeError = $probeError
    while ($null -ne $nativeProbeError -and -not ($nativeProbeError -is [ComponentModel.Win32Exception])) {
        $nativeProbeError = $nativeProbeError.InnerException
    }
    Assert-True ($null -ne $nativeProbeError -and $nativeProbeError.NativeErrorCode -eq 87) 'Invalid PID native cause is retained as Win32 error 87'
    $records.Add([ordered]@{
        probe_exception_shape=$true
        typed_win32_catch_selected=$typedCatchSelected
        outer_type=$probeError.GetType().FullName
        outer_native_error=$probeError.NativeErrorCode
        actual_native_type=$nativeProbeError.GetType().FullName
        actual_native_error=$nativeProbeError.NativeErrorCode
    })
    Assert-True (-not $job.Probe($control.Id).InJob) 'Same Node executable and fixture directory do not confer membership'
    $leaf = Open-Leaf $candidate
    $leafProbe = $job.Probe($leaf.Id)
    Assert-True ($leafProbe.InJob -and -not $leafProbe.HasExited) 'Surviving child remains live in Job after intermediate parent exit'
    $members = $job.QueryMembers()
    $postQueryLeafProbe = $job.Probe($leaf.Id)
    $matchingMembersCount = @($members.Members | Where-Object {$_.ProcessId -eq $leaf.Id -and $_.InJob}).Count
    $records.Add([ordered]@{
        membership_diagnostic=$true
        expected_leaf_pid=$leaf.Id
        expected_leaf_type=$leaf.GetType().FullName
        direct_probe=$leafProbe
        post_query_probe=$postQueryLeafProbe
        native_process_ids=@($members.NativeProcessIds)
        native_assigned_count=$members.NativeAssignedCount
        active_processes=$members.ActiveProcesses
        members=@($members.Members)
        gone_process_ids=@($members.GoneProcessIds)
        nonmember_process_ids=@($members.NonMemberProcessIds)
        matching_members_count=$matchingMembersCount
    })
    Assert-True ($postQueryLeafProbe.InJob -and -not $postQueryLeafProbe.HasExited) 'Child remains live in Job after member enumeration'
    Assert-True ($matchingMembersCount -eq 1) 'Job enumeration proves child without live parent-chain reconstruction'
    Expect-Throw { Wait-JobEmpty $job 0 } 'Live members prevent a successful cleanup report'
    $candidate.StandardInput.WriteLine('exit')
    $candidate.StandardInput.Flush()
    Wait-Exited $candidate 'Root may exit while child survives'
    $exitedRootProbe = $job.Probe($candidate.Id)
    Assert-True ($exitedRootProbe.ProcessId -eq $candidate.Id -and $exitedRootProbe.HasExited -and $exitedRootProbe.CreationTicks -eq $job.RootCreationUtc.Ticks) 'Retained exited root probe preserves the exact creation identity'
    $exitedImageMissing = [string]::IsNullOrEmpty([string]$exitedRootProbe.ExecutablePath)
    $exitedImageProof = if ($exitedImageMissing) {
        $null -ne $exitedRootProbe.ExecutablePathErrorCode -and $exitedRootProbe.ExecutablePathErrorCode -ne 0 -and
            -not [string]::IsNullOrEmpty([string]$exitedRootProbe.ExecutablePathDiagnostic)
    } else {
        $exitedRootProbe.ExecutablePath -eq $node -and $null -eq $exitedRootProbe.ExecutablePathErrorCode -and
            $null -eq $exitedRootProbe.ExecutablePathDiagnostic
    }
    Assert-True $exitedImageProof 'Exited root image is either the expected executable or an explicit exited-process read failure'
    $records.Add([ordered]@{
        exited_root_diagnostic=$exitedRootProbe
        image_unavailable_branch_observed=$exitedImageMissing
        note='A retained handle pins this already-exited fixture process. A successful image query does not exercise the image-unavailable branch.'
    })
    Assert-True (-not $leaf.HasExited -and $job.Probe($leaf.Id).InJob) 'Root exit does not prove Job empty'
    $job.Terminate(91)
    Wait-Exited $leaf 'TerminateJob clears surviving child after root exit'
    Wait-JobEmpty $job 5000
    Assert-True (-not $control.HasExited) 'TerminateJob preserves unrelated same-directory Node'
    $job.Dispose()
    Expect-Throw { $job.QueryMembers() } 'Disposed Job query fails instead of claiming empty'
    Expect-Throw { $job.Terminate(1) } 'Disposed Job termination fails explicitly'

    $closeRoot = Start-Node 'root'
    $closeJob = New-OwnedJob $closeRoot
    $closeLeaf = Open-Leaf $closeRoot
    $closeJob.Dispose()
    Wait-Exited $closeRoot 'Last Job handle close terminates root'
    Wait-Exited $closeLeaf 'Last Job handle close terminates child'
    Assert-True (-not $control.HasExited) 'Job close preserves unrelated Node'

    $exceptionRoot = Start-Node 'root'
    $exceptionJob = New-OwnedJob $exceptionRoot
    $exceptionLeaf = Open-Leaf $exceptionRoot
    Expect-Throw {
        try { throw 'Deliberate fixture failure before normal shutdown' }
        finally { $exceptionJob.Dispose() }
    } 'Exception path still closes only the owned Job'
    Wait-Exited $exceptionRoot 'Exception cleanup terminates assigned root'
    Wait-Exited $exceptionLeaf 'Exception cleanup terminates assigned descendant'
    Assert-True (-not $control.HasExited) 'Exception cleanup leaves unrelated process running'

    $watchedController = Start-Node 'control'
    $watchedRoot = Start-Node 'root'
    $watchedJob = New-OwnedJob $watchedRoot
    $watchedLeaf = Open-Leaf $watchedRoot
    Expect-Throw {
        $watchedJob.WatchController($watchedController.Id, $watchedController.StartTime.ToUniversalTime().AddSeconds(1))
    } 'Controller creation mismatch refuses watcher'
    Expect-Throw {
        $watchedJob.WatchController($watchedRoot.Id, $watchedRoot.StartTime.ToUniversalTime())
    } 'Controller watcher rejects its owned root'
    $watchedJob.WatchController($watchedController.Id, $watchedController.StartTime.ToUniversalTime())
    Assert-True (-not $watchedJob.Probe($watchedController.Id).InJob) 'Watched controller remains outside owned Job'
    $watchedController.Kill()
    Wait-Exited $watchedController 'Only the self-created simulated controller was stopped'
    Wait-Exited $watchedRoot 'Native controller watcher terminates root without a CIM sample'
    Wait-Exited $watchedLeaf 'Native controller watcher terminates remaining descendants'
    Wait-JobEmpty $watchedJob 5000
    Assert-True ($watchedJob.ControllerExitObserved -and $null -eq $watchedJob.ControllerTerminationError) 'Controller exit and successful Job termination are reported'
    Assert-True (-not $control.HasExited) 'Controller-death cleanup preserves unrelated Node'
    $watchedJob.Dispose()

    # The nested worker is a test-only process. Its native helper uses the same
    # PROC_THREAD_ATTRIBUTE_JOB_LIST creation shape as Rea; no vendor code runs.
    $nestedNonce = [Guid]::NewGuid().ToString('N')
    $start = [Diagnostics.ProcessStartInfo]::new()
    $start.FileName = $powershell
    $start.Arguments = '-NoLogo -NoProfile -NonInteractive -File "' + $nestedWorker + '" -NodePath "' + $node + '" -FixturePath "' + $fixture + '" -Nonce ' + $nestedNonce
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.WorkingDirectory = $workspace
    $start.RedirectStandardInput = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.EnvironmentVariables.Remove('NODE_OPTIONS')
    $start.EnvironmentVariables.Remove('NODE_PATH')
    $nested = [Diagnostics.Process]::Start($start)
    $null = $nested.Handle
    $handles.Add($nested)
    $hello = Read-Event $nested 'ready'
    Assert-True ($hello.pid -eq $nested.Id) 'Nested fixture remains gated before outer assignment'
    $outer = [GrimDynasty.StaticAnalysis.ReaOwnedJob]::new($nested.Id, $nested.StartTime.ToUniversalTime(), $powershell)
    $jobs.Add($outer)
    Assert-True ($outer.Probe($nested.Id).InJob -and -not $outer.Probe($PID).InJob) 'Nested fixture outer Job excludes supervisor'
    $nested.StandardInput.WriteLine('release')
    $nested.StandardInput.Flush()
    $nestedEvent = Read-Event $nested 'nested'
    $nestedChild = [Diagnostics.Process]::GetProcessById([int]$nestedEvent.childPid)
    $null = $nestedChild.Handle
    $handles.Add($nestedChild)
    Assert-True ($nestedEvent.innerMembership -eq $true) 'Job-list launched child proves inner Job membership'
    Assert-True ($outer.Probe($nestedChild.Id).InJob) 'Atomic inner Job-list launch retains inherited outer Job'
    Assert-True (@($outer.QueryMembers().Members | Where-Object {$_.ProcessId -eq $nestedChild.Id -and $_.InJob}).Count -eq 1) 'Outer Job enumeration includes nested child'
    $outer.Dispose()
    Wait-Exited $nested 'Outer Job close terminates nested worker'
    Wait-Exited $nestedChild 'Outer Job close terminates inner Job child'
    Assert-True (-not $control.HasExited) 'Nested cleanup preserves unrelated Node'

    foreach ($controlMode in @('valid','eof','invalid','dispose','owner-crash')) {
        $controlStart = [Diagnostics.ProcessStartInfo]::new()
        $controlStart.FileName = $powershell
        $workerMode = if ($controlMode -eq 'owner-crash') {'valid'} else {$controlMode}
        $controlStart.Arguments = '-NoLogo -NoProfile -NonInteractive -File "' + $controlWorker + '" -Mode ' + $workerMode
        $controlStart.UseShellExecute = $false
        $controlStart.CreateNoWindow = $true
        $controlStart.WorkingDirectory = $workspace
        $controlStart.RedirectStandardInput = $true
        $controlStart.RedirectStandardOutput = $true
        $controlStart.RedirectStandardError = $true
        $controlStart.EnvironmentVariables.Remove('NODE_OPTIONS')
        $controlStart.EnvironmentVariables.Remove('NODE_PATH')
        $controlProcess = [Diagnostics.Process]::Start($controlStart)
        $null = $controlProcess.Handle
        $handles.Add($controlProcess)
        $ready = Read-Event $controlProcess 'monitor-ready'
        $controlledChild = [Diagnostics.Process]::GetProcessById([int]$ready.rootPid)
        $null = $controlledChild.Handle
        $handles.Add($controlledChild)
        $sample = Read-Event $controlProcess 'sample-ready'
        Assert-True ($sample.rootPid -eq $ready.rootPid) ('Empty stdin does not block membership sampling: ' + $controlMode)
        if ($controlMode -eq 'owner-crash') {
            $controlProcess.Kill()
            Wait-Exited $controlProcess 'Self-created Job owner process was forcibly stopped'
            Wait-Exited $controlledChild 'Kernel handle cleanup kills member when its Job owner crashes'
            Assert-True (-not $control.HasExited) 'Job owner crash does not terminate unrelated Node'
            continue
        }
        if ($controlMode -eq 'valid') {
            $controlProcess.StandardInput.WriteLine('{"command":"stop-owned-job","nonce":"' + $ready.controlNonce + '"}')
            $controlProcess.StandardInput.Flush()
        } elseif ($controlMode -eq 'invalid') {
            $controlProcess.StandardInput.WriteLine('{"command":"wrong-command","nonce":"' + $ready.controlNonce + '"}')
            $controlProcess.StandardInput.Flush()
        } elseif ($controlMode -eq 'eof') {
            $controlProcess.StandardInput.Close()
        }
        $controlResult = Read-Event $controlProcess 'control-result'
        Assert-True (-not $controlResult.timedOut) ('Control fixture completes without hidden timeout: ' + $controlMode)
        Assert-True ($controlResult.disposeElapsedMs -lt 1000) ('Control Dispose does not join its blocked stdin reader: ' + $controlMode)
        if ($controlMode -eq 'valid') {
            Assert-True ($controlResult.stopRequested -and $null -eq $controlResult.failure -and $controlResult.childExited) 'Exact stop command terminates assigned child with success state'
        } elseif ($controlMode -eq 'dispose') {
            Assert-True (-not $controlResult.stopRequested -and $null -eq $controlResult.failure -and -not $controlResult.childExited) 'Disposing an idle control reader neither blocks nor silently kills Job'
        } else {
            Assert-True (-not $controlResult.stopRequested -and -not [string]::IsNullOrEmpty([string]$controlResult.failure) -and $controlResult.childExited) ('Control input fails closed: ' + $controlMode)
        }
        Wait-Exited $controlProcess ('Control worker exits normally: ' + $controlMode)
        Assert-True ($controlProcess.ExitCode -eq 0) ('Control worker reports successful assertions: ' + $controlMode)
        Wait-Exited $controlledChild ('Control worker final cleanup leaves no fixture child: ' + $controlMode)
        Assert-True (-not $control.HasExited) ('Control cleanup preserves unrelated Node: ' + $controlMode)
        $records.Add([ordered]@{control_mode=$controlMode; result=$controlResult})
    }
}
catch {
    $failure = $_.Exception.ToString()
}
finally {
    foreach ($job in $jobs) {
        try { $job.Dispose() } catch {
            $records.Add([ordered]@{cleanup_error=$_.Exception.Message})
            if ($null -eq $failure) { $failure = $_.Exception.ToString() }
        }
    }
    foreach ($process in $handles) {
        try {
            if (-not $process.HasExited) {
                # This exact retained process object was created/captured by this
                # test; never terminate a new process found through a recycled PID.
                $process.Kill()
                if (-not $process.WaitForExit(5000)) { throw 'Fixture process did not exit after direct cleanup.' }
            }
        } catch {
            $records.Add([ordered]@{cleanup_error=$_.Exception.Message})
            if ($null -eq $failure) { $failure = $_.Exception.ToString() }
        } finally { $process.Dispose() }
    }
    $report = [ordered]@{
        status=$(if ($null -eq $failure) {'passed'} else {'failed'})
        started_utc=$started.ToString('o'); finished_utc=[DateTime]::UtcNow.ToString('o')
        powershell_version=$PSVersionTable.PSVersion.ToString(); pointer_bytes=[IntPtr]::Size
        assertions=$assertions; records=@($records.ToArray()); error=$failure
        scope='Only project-owned Node/PowerShell/.NET fixtures. No Rea, Java, game binary or game data accessed.'
        limitations=@('Tests observe real Windows Job behavior on this host; they do not prove every future host Job hierarchy is compatible.',
          'Pre-assignment conhost processes are not asserted to belong to the new Job.',
          'Production IPC gating and Java role classification are separately tested by the controller suites.',
          'No real PID reuse or forced Win32 API failure is manufactured; mismatch/disposed/nonempty error branches are exercised.')
        source_sha256=@{
            ReaOwnedJob=(Get-FileHash -LiteralPath $nativeSource -Algorithm SHA256).Hash.ToLowerInvariant()
            test_owned_job=(Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash.ToLowerInvariant()
            node_fixture=(Get-FileHash -LiteralPath $fixture -Algorithm SHA256).Hash.ToLowerInvariant()
            nested_worker=(Get-FileHash -LiteralPath $nestedWorker -Algorithm SHA256).Hash.ToLowerInvariant()
            nested_native=(Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'OwnedJobNestedFixture.cs') -Algorithm SHA256).Hash.ToLowerInvariant()
            control_worker=(Get-FileHash -LiteralPath $controlWorker -Algorithm SHA256).Hash.ToLowerInvariant()
        }
    }
    $json = $report | ConvertTo-Json -Depth 12
    [IO.File]::WriteAllText((Join-Path $output 'result.json'), $json + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
    Write-Output ('Owned Job fixture: ' + $report.status + ', ' + $assertions + ' assertions; ' + (Join-Path $output 'result.json'))
}
if ($null -ne $failure) { throw $failure }
