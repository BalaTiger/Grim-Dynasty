$ErrorActionPreference='Stop'
if ($args.Count) { throw 'No arguments supported.' }
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if ($workspace -ne 'C:\Users\zhuzi\Documents\Grim-Dynasty') { throw 'Wrong workspace.' }
$source=[IO.Path]::GetFullPath((Join-Path $workspace 'analysis-runtime/rea/rea-ghidra-e4dbad03946d109dd66ecb819de833cd'))
$retainedParent=[IO.Path]::GetFullPath((Join-Path $workspace 'analysis-runtime/retained'))
$destination=[IO.Path]::GetFullPath((Join-Path $retainedParent 'failed-r1-player-mouse-handlers-e4dbad03946d109dd66ecb819de833cd'))
$proofDir=[IO.Path]::GetFullPath((Join-Path $workspace '.tooling/r1-player-mouse-handlers-retry-prepared'))
$summaryPath=[IO.Path]::GetFullPath((Join-Path $workspace 'docs/reverse-client/r1-player-mouse-handlers/summary.json'))
function Assert-PlainPath([string]$Path) {
    $full=[IO.Path]::GetFullPath($Path)
    if (-not ($full -eq $workspace -or $full.StartsWith($workspace+'\',[StringComparison]::OrdinalIgnoreCase))) { throw 'Path outside workspace.' }
    for($current=$full;;$current=[IO.Path]::GetDirectoryName($current)) {
        $item=Get-Item -LiteralPath $current -Force
        if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Reparse point refused.' }
        if ($current -eq $workspace) { break }
    }
    if ((Resolve-Path -LiteralPath $full).ProviderPath -ne $full) { throw 'Resolved path mismatch.' }
}
Assert-PlainPath $source; Assert-PlainPath $proofDir; Assert-PlainPath $summaryPath
if (Test-Path -LiteralPath $destination) { throw 'Retained destination already exists.' }
if ((Get-FileHash -LiteralPath $summaryPath).Hash.ToLowerInvariant() -ne 'bc292b53d91ade457fd392a6c420b15e0633f12d4f38f07523de725a8dfd84c2') { throw 'Failed summary changed.' }
$summary=Get-Content -LiteralPath $summaryPath -Raw|ConvertFrom-Json
if ($summary.status -ne 'failed' -or $summary.completed_plan_calls -ne 3 -or $summary.tool_calls -ne 4 -or $summary.closed_session_verified -or -not $summary.forced_termination -or $summary.child_exit_code -ne 92 -or -not $summary.process_exit_confirmed -or -not $summary.owned_memory_profile.job_empty_confirmed -or -not $summary.owned_memory_profile.sampler_exit_confirmed) { throw 'Prior cleanup prerequisites mismatch.' }
$ownerPath=Join-Path $source 'ownership.json'; Assert-PlainPath $ownerPath
if ((Get-FileHash -LiteralPath $ownerPath).Hash.ToLowerInvariant() -ne '83b033e35d03a6cbd9bc0ae340e998b39cf740470624b8af57bfb2fddea86b23') { throw 'Ownership record changed.' }
$owner=Get-Content -LiteralPath $ownerPath -Raw|ConvertFrom-Json
if ($owner.run_id -ne 'b974e276-b7a9-40ec-a75d-e37fc2ffdef8' -or $owner.pid -ne 31292 -or $owner.parent_pid -ne 31868 -or $owner.ownership_kind -ne 'windows-job-object' -or $owner.endpoint_path -ne (Join-Path $source 'bridge-endpoint.json')) { throw 'Ownership record mismatch.' }
if (@($summary.owned_memory_profile.processes|Where-Object {$_.pid -eq 31292 -and $_.parent_pid -eq 31868 -and $_.role -eq 'owned_backend_tree'}).Count -ne 1) { throw 'No matching saved Job member.' }
$snapshot=Join-Path $source 'target-cd7ecd7942d7.dll'; Assert-PlainPath $snapshot
if ((Get-FileHash -LiteralPath $snapshot).Hash.ToLowerInvariant() -ne $summary.target.sha256_after -or $summary.target.sha256_before -ne $summary.target.sha256_after) { throw 'Snapshot provenance mismatch.' }
function Check-Exited {
    $inventory=@(Get-CimInstance Win32_Process|Select-Object ProcessId,ParentProcessId,Name,CreationDate)
    $ids=@($summary.owned_memory_profile.processes|ForEach-Object {[int]$_.pid})
    $stillPresent=@($inventory|Where-Object {$ids -contains [int]$_.ProcessId})
    # Fail closed even on PID reuse; no process is terminated by this script.
    if ($stillPresent.Count) { throw 'A recorded PID is currently present; identity needs separate review.' }
    if (@($inventory|Where-Object {$_.Name -match '^(javaw?|decompile)\.exe$'}).Count) { throw 'An analysis process remains; no quarantine.' }
    return [pscustomobject]@{utc=[DateTime]::UtcNow.ToString('o');recorded_pid_count=$ids.Count;recorded_pids_present=0;analysis_process_count=0;external_dotnet=@($inventory|Where-Object Name -eq 'dotnet.exe'|Select-Object ProcessId,ParentProcessId,Name,CreationDate)}
}
function Inventory-Tree([string]$Base) {
    Assert-PlainPath $Base
    $queue=New-Object 'System.Collections.Generic.Queue[string]';$queue.Enqueue($Base)
    $rows=New-Object 'System.Collections.Generic.List[object]';$total=0L
    while($queue.Count) {
        $dir=$queue.Dequeue();Assert-PlainPath $dir
        foreach($item in @(Get-ChildItem -LiteralPath $dir -Force)) {
            if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Reparse member refused.' }
            if (-not $item.FullName.StartsWith($Base+'\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Member escaped tree.' }
            if ($rows.Count -ge 4096) { throw 'Inventory bound exceeded.' }
            $relative=$item.FullName.Substring($Base.Length+1)
            if($item.PSIsContainer){$queue.Enqueue($item.FullName);$rows.Add([pscustomobject]@{relative=$relative;kind='directory';bytes=0;sha256=$null})}
            else {
                $total += [long]$item.Length;if($total -gt 1073741824){throw 'One GiB preservation bound exceeded.'}
                $hash=(Get-FileHash -LiteralPath $item.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
                $rows.Add([pscustomobject]@{relative=$relative;kind='file';bytes=[long]$item.Length;sha256=$hash})
            }
        }
    }
    return @($rows|Sort-Object relative)
}
function Save-New([string]$Name,$Value) {
    $p=Join-Path $proofDir $Name;if(Test-Path -LiteralPath $p){throw 'Preserve earlier evidence.'}
    [IO.File]::WriteAllText($p,($Value|ConvertTo-Json -Depth 12)+[Environment]::NewLine,[Text.UTF8Encoding]::new($false))
}
$initialCheck=Check-Exited
$before=@(Inventory-Tree $source)
Save-New 'runtime-before.json' ([ordered]@{source=$source;destination=$destination;process_check=$initialCheck;entries=$before;credentials_printed=$false;original_target_read=$false})
if(-not(Test-Path -LiteralPath $retainedParent)){Assert-PlainPath (Split-Path $retainedParent -Parent);New-Item -ItemType Directory -Path $retainedParent|Out-Null}
Assert-PlainPath $retainedParent;Assert-PlainPath $source
if([IO.Path]::GetFullPath($destination) -ne $destination -or -not $destination.StartsWith($workspace+'\',[StringComparison]::OrdinalIgnoreCase) -or (Test-Path -LiteralPath $destination)){throw 'Destination validation failed.'}
$finalCheck=Check-Exited
Move-Item -LiteralPath $source -Destination $destination
Assert-PlainPath $destination
if(Test-Path -LiteralPath $source){throw 'Old active path remains; quarantine outcome uncertain.'}
$after=@(Inventory-Tree $destination)
$same=($before|ConvertTo-Json -Depth 5 -Compress) -ceq ($after|ConvertTo-Json -Depth 5 -Compress)
Save-New 'runtime-after.json' ([ordered]@{destination=$destination;entries=$after;matches_before=$same})
if(-not $same){throw 'Preservation inventory differs; keep all evidence and stop.'}
$remaining=@(Get-ChildItem -LiteralPath (Join-Path $workspace 'analysis-runtime/rea') -Force|Where-Object Name -Match '^rea-ghidra-')
$result=[ordered]@{status='quarantined-with-content-preserved';utc=[DateTime]::UtcNow.ToString('o');source=$source;destination=$destination;process_check_before_move=$finalCheck;files=@($after|Where-Object kind -eq 'file').Count;entries=$after.Count;bytes=($after|Measure-Object bytes -Sum).Sum;content_manifest_equal=$true;deleted_files=0;active_runtime_roots=$remaining.Count;normal_mcp_close_retroactively_claimed=$false;external_process_gate_waived=$false;runner_guard_changed=$false;backend_started=$false;restore='After checking no active session/path collision, move the retained directory back to the exact original path. No content was modified.'}
Save-New 'runtime-quarantine-result.json' $result
$result|ConvertTo-Json -Depth 8
