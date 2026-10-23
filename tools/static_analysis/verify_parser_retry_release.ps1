param([ValidateSet('rea-binary-parser-entry-retry-1','rea-binary-parser-entry-retry-2','rea-binary-parser-helpers','rea-binary-parser-vector-growth','rea-record-cache-publication','rea-record-cache-maintenance','rea-record-cache-node')][string]$Batch)
$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$batchPath = Join-Path $workspace "docs/object-lifecycle/$Batch"
$summary = Get-Content -LiteralPath (Join-Path $batchPath 'summary.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($summary.target.sha256_after -ne '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153') { throw 'Unexpected target fingerprint.' }
if (-not $summary.process_exit_confirmed -or -not $summary.owned_memory_profile.sampler_exit_confirmed) { throw 'Runner or sampler exit unconfirmed.' }
$inventory = @(Get-CimInstance Win32_Process | Select-Object ProcessId,ParentProcessId,Name,CreationDate)
$recorded = @($summary.owned_memory_profile.processes)
$liveSameIdentity = @()
$reusedIds = @()
foreach ($item in $recorded) {
    foreach ($row in @($inventory | Where-Object { $_.ProcessId -eq $item.pid })) {
        $delta = [math]::Abs(($row.CreationDate.ToUniversalTime() - [datetime]::Parse($item.creation_utc).ToUniversalTime()).Ticks)
        if ($delta -le 10) { $liveSameIdentity += $item }
        else { $reusedIds += [pscustomobject]@{pid=$item.pid;current_name=$row.Name;current_creation_utc=$row.CreationDate.ToUniversalTime().ToString('o')} }
    }
}
$activeAnalysis = @($inventory | Where-Object {$_.Name -match '^(javaw?|decompile)\.exe$'})
$runtimeBase = [IO.Path]::GetFullPath((Join-Path $workspace 'analysis-runtime/rea'))
$runtimeDirs = @(Get-ChildItem -LiteralPath $runtimeBase -Directory -Force | Where-Object {$_.Name -match '^rea-ghidra-[a-f0-9]+$'})
$result = [ordered]@{utc=[datetime]::UtcNow.ToString('o');batch=$Batch;recorded_process_count=$recorded.Count;remaining_same_identity_count=$liveSameIdentity.Count;reused_ids=$reusedIds;active_analysis_process_count=$activeAnalysis.Count;removed=@();remaining_runtime_files=$null}
if ($liveSameIdentity.Count -or $activeAnalysis.Count) { throw 'Processes remain; no runtime deletion attempted.' }
foreach ($directory in $runtimeDirs) {
    $full = [IO.Path]::GetFullPath($directory.FullName)
    if (-not $full.StartsWith($runtimeBase + '\',[StringComparison]::OrdinalIgnoreCase) -or $directory.Parent.FullName -ne $runtimeBase) { throw 'Runtime path outside exact parent.' }
    if ($directory.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Runtime directory is a reparse point.' }
    $entries = @(Get-ChildItem -LiteralPath $full -Recurse -Force)
    if (@($entries | Where-Object {$_.Attributes -band [IO.FileAttributes]::ReparsePoint}).Count) { throw 'Runtime contains a reparse point.' }
    $descriptor = Join-Path $full 'session.json'
    $session = Get-Content -LiteralPath $descriptor -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($session.target_sha256 -ne $summary.target.sha256_before -or $session.provider_version -ne '12.1.4' -or -not $session.run_id) { throw 'Runtime ownership metadata mismatch.' }
    if ((Get-Item -LiteralPath $descriptor).LastWriteTimeUtc -lt [datetime]::Parse($summary.started).ToUniversalTime()) { throw 'Runtime predates this batch.' }
    $result.removed += [pscustomobject]@{path=$full;files=@($entries | Where-Object {-not $_.PSIsContainer}).Count}
    Remove-Item -LiteralPath $full -Recurse -Force
    if (Test-Path -LiteralPath $full) { throw 'Runtime directory removal unconfirmed.' }
}
$result.remaining_runtime_files = @(Get-ChildItem -LiteralPath $runtimeBase -Recurse -Force -File).Count
$recordPath = Join-Path $batchPath 'resource-release.json'
if (Test-Path -LiteralPath $recordPath) { throw 'Never overwrite release evidence.' }
$result | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $recordPath -Encoding UTF8
$result | ConvertTo-Json -Depth 6
