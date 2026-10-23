param(
    [Parameter(Mandatory=$true)][int]$RootProcessId,
    [Parameter(Mandatory=$true)][int]$ControllerProcessId,
    [Parameter(Mandatory=$true)][string]$RunNonce,
    [Parameter(Mandatory=$true)][string]$NodeExecutable,
    [Parameter(Mandatory=$true)][string]$OutputFile,
    [ValidateRange(500,5000)][int]$IntervalMilliseconds = 1000,
    [ValidateRange(1,600)][int]$DurationSeconds = 600
)
$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$outputPath = [IO.Path]::GetFullPath($OutputFile)
$wrapper = Join-Path $PSScriptRoot 'rea_profiled_server.mjs'
if ($RunNonce -notmatch '^[a-f0-9-]{36}$') { throw 'Invalid task identity nonce.' }
if (-not $outputPath.StartsWith($workspace + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Output outside workspace.' }
if (Test-Path -LiteralPath $outputPath) { throw 'Never overwrite measurements.' }
$nodePath = [IO.Path]::GetFullPath($NodeExecutable)
$writer = $null
$job = $null
$controlChannel = $null
$normalEmpty = $false
$rootCreation = $null
$stopRequested = $false
$phase = 'startup'
. (Join-Path $PSScriptRoot 'rea_sampler_failure.ps1')
$heavyPattern = '^(javaw?|UnrealEditor(-Cmd)?|UE4Editor(-Cmd)?|UnrealBuildTool|AutomationTool|ShaderCompileWorker|cl|link|csc|dotnet|MSBuild|UbaHost|UbaAgent|UbaSessionServer)\.exe$'
function Same-Instant([datetime]$left, [datetime]$right) {
    return [math]::Abs(($left.ToUniversalTime() - $right.ToUniversalTime()).Ticks) -le 10
}
try {
    Add-Type -Path (Join-Path $PSScriptRoot 'ReaOwnedJob.cs')
    $rootRow = Get-CimInstance Win32_Process -Filter "ProcessId=$RootProcessId"
    $controllerRow = Get-CimInstance Win32_Process -Filter "ProcessId=$ControllerProcessId"
    if (-not $rootRow -or -not $controllerRow) { throw 'Gated root or controller disappeared.' }
    if ([int]$rootRow.ParentProcessId -ne $ControllerProcessId) { throw 'Gated root has the wrong controller.' }
    if ($rootRow.CreationDate -lt $controllerRow.CreationDate) { throw 'Root creation predates controller.' }
    $argv = [GrimDynasty.StaticAnalysis.ReaOwnedJob]::SplitCommandLine([string]$rootRow.CommandLine)
    if ($argv.Length -ne 3 -or
        -not [IO.Path]::GetFullPath($argv[0]).Equals($nodePath, [StringComparison]::OrdinalIgnoreCase) -or
        -not [IO.Path]::GetFullPath($argv[1]).Equals($wrapper, [StringComparison]::OrdinalIgnoreCase) -or
        $argv[2] -cne "--owned-guard=$RunNonce") { throw 'Root argv does not identify the gated task.' }
    $rootCreation = $rootRow.CreationDate.ToUniversalTime()
    $preGateChildren = @(Get-CimInstance Win32_Process -Filter "ParentProcessId=$RootProcessId" | Select-Object ProcessId,ParentProcessId,Name,CreationDate)
    $job = New-Object GrimDynasty.StaticAnalysis.ReaOwnedJob($RootProcessId, $rootCreation, $nodePath)
    $job.WatchController($ControllerProcessId, $controllerRow.CreationDate.ToUniversalTime())
    $phase = 'initial-monitor-proof'
    if (($job.Probe($PID)).InJob -or ($job.Probe($ControllerProcessId)).InJob) { throw 'Monitor or controller unexpectedly belongs to backend Job.' }
    $phase = 'initial-root-proof'
    $rootProof = $job.Probe($RootProcessId)
    if (-not $rootProof.InJob -or $rootProof.HasExited) { throw 'Gated root Job membership not established.' }
    $rootCreation = $rootProof.CreationUtc
    $writer = New-Object IO.StreamWriter([IO.File]::Open($outputPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::Read), (New-Object Text.UTF8Encoding($false)))
    $writer.AutoFlush = $true
    $controlChannel = New-Object GrimDynasty.StaticAnalysis.ReaJobControl($job, $RunNonce)
    $deadline = [DateTime]::UtcNow.AddSeconds($DurationSeconds)
    do {
        $phase = 'control-check'
        if ($controlChannel.Failure) { throw $controlChannel.Failure }
        if ($job.ControllerTerminationError) { throw $job.ControllerTerminationError }
        $stopRequested = $controlChannel.StopRequested
        $sampleStarted = [DateTime]::UtcNow
        $phase = 'before-inventory-query'
        $members = $job.QueryMembers()
        $memberIds = @{}
        foreach ($member in $members.Members) { $memberIds[[int]$member.ProcessId] = $true }
        # Unrelated command lines are never requested or retained.
        $phase = 'process-inventory'
        $processes = @(Get-CimInstance Win32_Process | Select-Object ProcessId,ParentProcessId,Name,CreationDate)
        $gaps = New-Object 'Collections.Generic.List[object]'
        $external = New-Object 'Collections.Generic.List[object]'
        $rows = New-Object 'Collections.Generic.List[object]'
        $endedBeforeProof = New-Object 'Collections.Generic.List[object]'
        foreach ($row in $processes) {
            $taskPid = [int]$row.ProcessId
            $heavy = $row.Name -match $heavyPattern
            if (-not ($memberIds.ContainsKey($taskPid) -or $heavy -or $taskPid -eq $PID -or $taskPid -eq $ControllerProcessId -or $taskPid -eq $RootProcessId)) { continue }
            $proof = $null
            $phase = 'inventory-probe'
            try { $proof = $job.Probe($taskPid) }
            catch {
                # ERROR_INVALID_PARAMETER from OpenProcess means that PID no longer exists.
                # It is not admission to the Job, nor authority to terminate anything.
                $nativeError = $_.Exception.GetBaseException()
                if ($nativeError -is [ComponentModel.Win32Exception] -and $nativeError.NativeErrorCode -eq 87 -and $nativeError.Data['OpenProcess']) {
                    $endedBeforeProof.Add([pscustomobject]@{pid=$taskPid;name=$row.Name;creation_utc=$row.CreationDate.ToUniversalTime().ToString('o');reason='Process absent at handle query; ownership not inferred'})
                    continue
                }
                throw
            }
            if (-not (Same-Instant $proof.CreationUtc $row.CreationDate)) { throw 'PID creation changed between inventory and native handle probe.' }
            if ($proof.HasExited) {
                $endedBeforeProof.Add([pscustomobject]@{pid=$taskPid;name=$row.Name;creation_utc=$proof.CreationUtc.ToString('o');reason='Native process handle is signaled; process already exited';membership_observed=$proof.InJob;image_query_error_code=$proof.ExecutablePathErrorCode;image_query_diagnostic=$proof.ExecutablePathDiagnostic})
                continue
            }
            if ($heavy -and -not $proof.InJob) {
                $external.Add([pscustomobject]@{ProcessId=$taskPid;ParentProcessId=[int]$row.ParentProcessId;Name=$row.Name;creation_utc=$proof.CreationUtc.ToString('o');membership='Not in the explicit task Job'})
                continue
            }
            $role = if ($taskPid -eq $PID) { 'sampler' } elseif ($taskPid -eq $ControllerProcessId) { 'controller' } elseif ($proof.InJob) { 'owned_backend_tree' } else { $null }
            if ($null -eq $role) { continue }
            $process = $null
            $phase = 'process-counters'
            try {
                $process = [Diagnostics.Process]::GetProcessById($taskPid)
                $process.Refresh()
                if (-not (Same-Instant $process.StartTime.ToUniversalTime() $proof.CreationUtc)) { throw 'PID identity changed during counter read.' }
                $javaArguments = @()
                $heapSwitches = @()
                if ($row.Name -match '^javaw?\.exe$') {
                    $javaRow = Get-CimInstance Win32_Process -Filter "ProcessId=$taskPid"
                    if (-not $javaRow) { throw [ArgumentException]::new('Java exited before command observation.') }
                    if (-not (Same-Instant $javaRow.CreationDate $proof.CreationUtc)) { throw 'Java identity changed before command observation.' }
                    $javaArgv = [GrimDynasty.StaticAnalysis.ReaOwnedJob]::SplitCommandLine([string]$javaRow.CommandLine)
                    if ($javaArgv.Length -lt 2) { throw 'Owned Java command unavailable.' }
                    # The official batch uses both "java.exe" from its fixed PATH
                    # and an absolute JAVA_HOME\bin\java without the .exe suffix.
                    # Kernel executable identity, not argv[0], proves the file.
                    $javaCommand = [string]$javaArgv[0]
                    if ($javaCommand -notin @('java','java.exe')) {
                        if (-not [IO.Path]::IsPathRooted($javaCommand)) { throw 'Unexpected relative owned Java command.' }
                        if ([IO.Path]::GetExtension($javaCommand) -eq '') { $javaCommand += '.exe' }
                        if (-not [IO.Path]::GetFullPath($javaCommand).Equals($proof.ExecutablePath, [StringComparison]::OrdinalIgnoreCase)) { throw 'Owned Java executable and command disagree.' }
                    }
                    $javaArguments = @($javaArgv | Select-Object -Skip 1)
                    $heapSwitches = @($javaArguments | Where-Object { $_ -match '^-Xm[sx]' })
                }
                $rows.Add([pscustomobject]@{
                    pid=$taskPid;parent_pid=[int]$row.ParentProcessId;name=$row.Name;role=$role
                    creation_utc=$proof.CreationUtc.ToString('o')
                    ownership=if ($proof.InJob) { 'explicit-kernel-job' } else { 'monitor-or-controller' }
                    executable_path=$proof.ExecutablePath
                    private_commit_bytes=[long]$process.PrivateMemorySize64
                    lifetime_peak_commit_bytes_observed=[long]$process.PeakPagedMemorySize64
                    working_set_bytes=[long]$process.WorkingSet64
                    lifetime_peak_working_set_bytes_observed=[long]$process.PeakWorkingSet64
                    thread_count=$process.Threads.Count
                    java_heap_switches=$heapSwitches;java_arguments=$javaArguments
                })
            } catch [SystemException] {
                $counterFailure = $_
                $ended = $false
                $phase = 'counter-exit-probe'
                try { $ended = $job.Probe($taskPid).HasExited }
                catch {
                    $nativeError = $_.Exception.GetBaseException()
                    if ($nativeError -is [ComponentModel.Win32Exception] -and $nativeError.NativeErrorCode -eq 87 -and $nativeError.Data['OpenProcess']) { $ended = $true }
                    else { throw }
                }
                if (-not $ended) { $phase = 'process-counters'; throw $counterFailure }
                $gaps.Add([pscustomobject]@{pid=$taskPid;role=$role;reason='Previously proved member exited before counters completed'})
            } finally { if ($null -ne $process) { $process.Dispose() } }
        }
        $phase = 'system-memory'
        $memory = Get-CimInstance Win32_PerfFormattedData_PerfOS_Memory
        $phase = 'after-counters-query'
        $finalMembers = $job.QueryMembers()
        $jobEmpty = $finalMembers.ActiveProcesses -eq 0 -and $finalMembers.Members.Length -eq 0
        $phase = 'control-check'
        if ($controlChannel.Failure) { throw $controlChannel.Failure }
        if ($job.ControllerTerminationError) { throw $job.ControllerTerminationError }
        $record = [ordered]@{
            utc=$sampleStarted.ToString('o');sample_elapsed_ms=([DateTime]::UtcNow-$sampleStarted).TotalMilliseconds
            root_pid=$RootProcessId;root_creation_utc=$rootCreation.ToString('o')
            job=[ordered]@{policy='windows-job-v1';nonce=$RunNonce;root_assigned=$true;active_processes=$finalMembers.ActiveProcesses;probed_member_count=$finalMembers.Members.Length;empty_confirmed=$jobEmpty;stop_requested=$stopRequested;controller_exit_observed=$job.ControllerExitObserved}
            available_physical_bytes=[long]$memory.AvailableBytes;system_commit_bytes=[long]$memory.CommittedBytes;system_commit_limit_bytes=[long]$memory.CommitLimit
            counters=@($rows.ToArray());counter_read_gaps=@($gaps.ToArray())
            ended_before_counter_observation=@($endedBeforeProof.ToArray())
            membership_queries=[ordered]@{
                before_inventory=[ordered]@{active=$members.ActiveProcesses;ids=$members.NativeProcessIds;gone=$members.GoneProcessIds;nonmembers=$members.NonMemberProcessIds}
                after_counters=[ordered]@{active=$finalMembers.ActiveProcesses;ids=$finalMembers.NativeProcessIds;gone=$finalMembers.GoneProcessIds;nonmembers=$finalMembers.NonMemberProcessIds}
                member_identities_without_counters=@($members.Members | Where-Object { $memberPid=$_.ProcessId; -not @($rows.ToArray() | Where-Object {$_.pid -eq $memberPid}).Count } | Select-Object ProcessId,CreationUtc,HasExited)
                after_counters_member_identities=@($finalMembers.Members | ForEach-Object {
                    [ordered]@{
                        ProcessId=$_.ProcessId
                        CreationUtc=$_.CreationUtc.ToString('o')
                        CreationTicks=$_.CreationTicks.ToString([Globalization.CultureInfo]::InvariantCulture)
                        InJob=$_.InJob;HasExited=$_.HasExited
                        ExecutablePath=$_.ExecutablePath
                        ExecutablePathErrorCode=$_.ExecutablePathErrorCode
                        ExecutablePathDiagnostic=$_.ExecutablePathDiagnostic
                    }
                })
            }
            pre_gate_child_observations=$preGateChildren
            live_owned_identity_count=$finalMembers.ActiveProcesses
            external_build_or_java_processes=@($external.ToArray())
            note='Explicit kernel Job membership and process creation identity. Exited-before-observation rows are not adopted or killed. Sampling may miss short-lived peaks. Pre-gate console observations are not adopted, included in the owned sum, or terminated by PID.'
        }
        $phase = 'write-sample'
        $line = $record | ConvertTo-Json -Depth 8 -Compress
        $writer.WriteLine($line)
        [Console]::Out.WriteLine($line)
        if ($jobEmpty) { $normalEmpty = $true; break }
        $phase = 'deadline'
        if ([DateTime]::UtcNow -ge $deadline) { throw 'Owned Job monitoring reached its bounded deadline.' }
        Start-Sleep -Milliseconds $IntervalMilliseconds
    } while ($true)
} catch {
    $samplerFailure = $_
    try {
        Write-ReaSamplerFailure -Exception $samplerFailure.Exception -Phase $phase -RootProcessId $RootProcessId -ControllerProcessId $ControllerProcessId -OutputFile $outputPath
    } catch {
        # Do not serialize the secondary exception or replace the first failure.
        try { [Console]::Error.WriteLine('REA_SAMPLER_FAILURE_WRITE_FAILED') } catch { }
    }
    throw $samplerFailure
} finally {
    if ($null -ne $controlChannel) { $controlChannel.Dispose() }
    if ($null -ne $job) {
        # Dispose closes only this retained, non-inherited Job handle. Kill-on-close
        # covers its descendants even after the original root has exited.
        $job.Dispose()
    }
    if ($null -ne $writer) { $writer.Dispose() }
}
if (-not $normalEmpty) { throw 'Job emptiness was not confirmed before monitor exit.' }
