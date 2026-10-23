param(
    [Parameter(Mandatory=$true)][ValidateRange(1,2147483647)][int]$RootProcessId,
    [int]$ControllerProcessId = 0,
    [ValidateRange(0,600)][int]$DurationSeconds = 180,
    [ValidateRange(500,5000)][int]$IntervalMilliseconds = 1000,
    [Parameter(Mandatory=$true)][string]$OutputFile,
    [switch]$StreamSamples
)
$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$outputPath = [IO.Path]::GetFullPath($OutputFile)
if (-not $outputPath.StartsWith($workspace + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Output must stay within this workspace.' }
if (Test-Path -LiteralPath $outputPath) { throw 'Preserve previous measurements.' }
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $outputPath) | Out-Null
$encoding = [Text.UTF8Encoding]::new($false)
$stream = [IO.File]::Open($outputPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::Read)
$writer = [IO.StreamWriter]::new($stream, $encoding)
$writer.AutoFlush = $true
$deadline = [DateTime]::UtcNow.AddSeconds($DurationSeconds)
$owned = @{}
$rootCreation = $null
$controllerCreation = $null
try {
    do {
        $sampleStarted = [DateTime]::UtcNow
        $processes = @(Get-CimInstance Win32_Process | Select-Object ProcessId,ParentProcessId,Name,CreationDate,CommandLine)
        if ($ControllerProcessId -gt 0) {
            $controllerRow = $processes | Where-Object { [int]$_.ProcessId -eq $ControllerProcessId }
            if ($controllerRow) {
                $currentControllerCreation = $controllerRow.CreationDate.ToUniversalTime().ToString('o')
                if ($null -eq $controllerCreation) { $controllerCreation = $currentControllerCreation }
                if ($currentControllerCreation -ne $controllerCreation) { throw 'Controller PID reused; stop attribution.' }
            } elseif ($null -eq $controllerCreation) { throw 'Controller unavailable for initial identification.' }
        }
        $rootRow = $processes | Where-Object { [int]$_.ProcessId -eq $RootProcessId }
        if ($rootRow) {
            $creation = $rootRow.CreationDate.ToUniversalTime().ToString('o')
            if ($null -eq $rootCreation) { $rootCreation = $creation; $owned[$RootProcessId] = $creation }
            if ($creation -ne $rootCreation) { throw 'Root PID reused; stop attribution.' }
        } elseif ($null -eq $rootCreation) { throw 'Root process not available for identification.' }
        do {
            $added = $false
            foreach ($row in $processes) {
                $taskPid = [int]$row.ProcessId; $parentPid = [int]$row.ParentProcessId
                $rowCreation = $row.CreationDate.ToUniversalTime().ToString('o')
                if ((-not $owned.ContainsKey($taskPid) -or $owned[$taskPid] -ne $rowCreation) -and $owned.ContainsKey($parentPid)) {
                    $parent = $processes | Where-Object { [int]$_.ProcessId -eq $parentPid }
                    if ($parent -and $parent.CreationDate.ToUniversalTime().ToString('o') -eq $owned[$parentPid] -and $row.CreationDate -ge $parent.CreationDate) {
                        $owned[$taskPid] = $row.CreationDate.ToUniversalTime().ToString('o'); $added = $true
                    }
                }
            }
        } while ($added)
        $counterReadGaps = [Collections.Generic.List[object]]::new()
        $counters = @(foreach ($row in $processes) {
            $taskPid = [int]$row.ProcessId
            $isOwned = $owned.ContainsKey($taskPid) -and $row.CreationDate.ToUniversalTime().ToString('o') -eq $owned[$taskPid]
            $role = if ($taskPid -eq $PID) { 'sampler' } elseif ($ControllerProcessId -gt 0 -and $taskPid -eq $ControllerProcessId) { 'controller' } elseif ($isOwned) { 'owned_backend_tree' } else { $null }
            if ($null -ne $role) {
                $process = $null
                try {
                    $process = [Diagnostics.Process]::GetProcessById($taskPid)
                    $process.Refresh()
                    $startDeltaMs = [math]::Abs(($process.StartTime.ToUniversalTime() - $row.CreationDate.ToUniversalTime()).TotalMilliseconds)
                    if ($startDeltaMs -gt 1) { throw [InvalidOperationException]::new('PID creation identity changed between snapshot and counters.') }
                    [pscustomobject]@{
                        pid=$taskPid; parent_pid=[int]$row.ParentProcessId; name=$row.Name; role=$role
                        creation_utc=$row.CreationDate.ToUniversalTime().ToString('o')
                        private_commit_bytes=[long]$process.PrivateMemorySize64
                        lifetime_peak_commit_bytes_observed=[long]$process.PeakPagedMemorySize64
                        working_set_bytes=[long]$process.WorkingSet64
                        lifetime_peak_working_set_bytes_observed=[long]$process.PeakWorkingSet64
                        thread_count=$process.Threads.Count
                        java_heap_switches=@(if ($row.Name -match '^javaw?\.exe$') { [regex]::Matches([string]$row.CommandLine, '-Xm[sx][^\s]+').Value })
                    }
                } catch [ArgumentException] {
                    $counterReadGaps.Add([pscustomobject]@{pid=$taskPid;role=$role;reason='Process exited or identity unavailable during counter read'})
                } catch [InvalidOperationException] {
                    $counterReadGaps.Add([pscustomobject]@{pid=$taskPid;role=$role;reason=$_.Exception.Message})
                } finally { if ($null -ne $process) { $process.Dispose() } }
            }
        })
        $memory = Get-CimInstance Win32_PerfFormattedData_PerfOS_Memory
        $external = @($processes | Where-Object { (-not $owned.ContainsKey([int]$_.ProcessId) -or $owned[[int]$_.ProcessId] -ne $_.CreationDate.ToUniversalTime().ToString('o')) -and $_.Name -match '^(javaw?|UnrealEditor(-Cmd)?|UE4Editor(-Cmd)?|UnrealBuildTool|AutomationTool|ShaderCompileWorker|cl|link|csc|dotnet|MSBuild|UbaHost|UbaAgent|UbaSessionServer)\.exe$' } | Select-Object ProcessId,ParentProcessId,Name)
        $liveOwnedCount = @($processes | Where-Object { $owned.ContainsKey([int]$_.ProcessId) -and $owned[[int]$_.ProcessId] -eq $_.CreationDate.ToUniversalTime().ToString('o') }).Count
        $record = [ordered]@{
            utc=$sampleStarted.ToString('o'); sample_elapsed_ms=([DateTime]::UtcNow-$sampleStarted).TotalMilliseconds
            root_pid=$RootProcessId; root_creation_utc=$rootCreation
            available_physical_bytes=[long]$memory.AvailableBytes
            system_commit_bytes=[long]$memory.CommittedBytes; system_commit_limit_bytes=[long]$memory.CommitLimit
            counters=$counters; counter_read_gaps=@($counterReadGaps.ToArray()); live_owned_identity_count=$liveOwnedCount; external_build_or_java_processes=$external
            sampled_tree_private_commit_bytes=($counters | Where-Object {$_.role -eq 'owned_backend_tree'} | Measure-Object private_commit_bytes -Sum).Sum
            sampled_tree_working_set_bytes=($counters | Where-Object {$_.role -eq 'owned_backend_tree'} | Measure-Object working_set_bytes -Sum).Sum
            note='Read-only counters. Sum of working sets may double-count shared pages. Per-process observed lifetime peaks are not simultaneous tree peaks; the last interval before exit may be missing.'
        }
        $line = $record | ConvertTo-Json -Depth 6 -Compress
        $writer.WriteLine($line)
        if ($StreamSamples) { [Console]::Out.WriteLine($line) }
        if ([DateTime]::UtcNow -ge $deadline -or $liveOwnedCount -eq 0) { break }
        Start-Sleep -Milliseconds $IntervalMilliseconds
    } while ([DateTime]::UtcNow -lt $deadline)
} finally { $writer.Dispose() }
if (-not $StreamSamples) { Write-Output $outputPath }
