$ErrorActionPreference='Stop'
$repo='C:\Users\zhuzi\Documents\Grim-Dynasty'
if((Get-Location).Path -ne $repo){throw 'Unexpected workspace'}
$utf8=New-Object Text.UTF8Encoding($false)
$checkpoint='docs/reverse-client/r1-state-registration-chain/checkpoint.json'
$manifest='docs/reverse-client/r1-state-registration-chain/manifest.json'
$validation=@('.tooling/r1-portal-transition-validation','.tooling/r1-portal-camera-validation')
$results=@();$counts=@()
foreach($dir in $validation){
 $r=Get-Content -Raw -Encoding UTF8 (Join-Path $dir 'result.json')|ConvertFrom-Json
 if($r.status -notlike 'passed-isolated-portal-*-contracts' -or $r.compile_library_exit -ne 0 -or $r.compile_test_exit -ne 0 -or $r.test_run_exit -ne 0){throw 'No complete passing validation'}
 foreach($f in $r.inputs){if((Get-FileHash -LiteralPath $f.path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $f.sha256){throw 'Compiled input changed'}}
 foreach($f in $r.dependencies){if((Get-FileHash -LiteralPath (Join-Path $repo $f.file) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $f.hash){throw 'Frozen dependency changed'}}
 foreach($f in $r.artifacts){if((Get-FileHash -LiteralPath $f.path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $f.sha256){throw 'Compiled artifact changed'}}
 $log=Get-Content -Raw -Encoding UTF8 (Join-Path $dir 'tests.log')
 $match=[regex]::Match($log,'PASS (\d+) scenarios, (\d+) assertions;')
 if(!$match.Success){throw 'Missing explicit test count'}
 $counts+=[pscustomobject]@{result=(Join-Path $dir 'result.json').Replace('\','/');scenarios=[int]$match.Groups[1].Value;assertions=[int]$match.Groups[2].Value;old_suites_rerun=$false}
 $results+=$r
}
$batches=@('r1-game-camera-constructor','r1-game-camera-angle','r1-engine-portal-camera','r1-game-camera-position')
foreach($batch in $batches){
 $s=Get-Content -Raw -Encoding UTF8 ("docs/reverse-client/$batch/summary.json")|ConvertFrom-Json
 if($s.status -ne 'passed' -or !$s.closed_session_verified -or !$s.process_exit_confirmed -or !$s.owned_memory_profile.job_empty_confirmed -or !$s.owned_memory_profile.sampler_exit_confirmed){throw 'Incomplete backend cleanup'}
 if($s.target.sha256_before -ne $s.target.sha256_after){throw 'Original fingerprint changed'}
}
# No termination: cleanup evidence is authoritative; active analysis remnants stop sealing.
if(@(Get-ChildItem -LiteralPath 'analysis-runtime/rea' -Directory -Filter 'rea-ghidra-*').Count -ne 0){throw 'Active backend runtime remains'}
$dataDirs=@('r1-game-camera-anchor','r1-camera-coordinates-binding','r1-game-camera-slot-binding','r1-camera-yaw-binding','r1-game-camera-position-slot')
foreach($dir in $dataDirs){$d=Get-Content -Raw -Encoding UTF8 ("docs/reverse-client/$dir/inspection.json")|ConvertFrom-Json;if($d.status -ne 'passed'){throw 'Incomplete data evidence'}}
$c=Get-Content -Raw -Encoding UTF8 $checkpoint|ConvertFrom-Json
$c.stage='portal-transition-and-camera-contracts-tested-not-gameplay'
$c.last_event_utc=[DateTime]::UtcNow.ToString('o')
$c.scope='Three-waypoint production, full owner-frame transform, portal/player branch and exact camera yaw/partial-coordinate output tested. Game GetTarget and real world/navigation/input/scene remain unclosed.'
$c.current_uncompiled_changes=@()
$c.new_tests=($counts|Measure-Object scenarios -Sum).Sum
$c.execution_state.owned_backend_active=$false;$c.execution_state.pending_command=$false;$c.execution_state.uncertain_outcome=$false
$c.next_bounded_scope.primary=@('Game2BE6E0 GetTarget: reuse609720/6094A0 evidence; identify609B70 TranslateInRegion and60A690 camera-offset imports only as required','Integrate fine-grained coordinate output without filling untouched DWORD14; keep frozen whole-QWORD projection Unsupported when padding unknown','Then real navigation/mouse-coordinate/scene integration; no whole-engine expansion or prototype replacement')
$c.next_bounded_scope.future_plan_prepared=$false;$c.next_bounded_scope.future_execution_requested=$false
$c|Add-Member -NotePropertyName portal_camera_result -NotePropertyValue ([pscustomobject]@{report='docs/reverse-client/R1-PORTAL-TRANSITION-RESULT.md';validation=$counts;fixed_rea_calls=35;prior_rea_queries_repeated=$false;compile_attempts=2;compile_failures=0;test_failures=0;gameplay_accepted=$false;native_get_target_implemented=$false;whole_frame_projection_has_unknown_padding_stop=$true}) -Force
[IO.File]::WriteAllText((Join-Path $repo $checkpoint),($c|ConvertTo-Json -Depth 40)+"`n",$utf8)
$progress='.tooling/r1-portal-transition-prepared/progress.json'
$p=Get-Content -Raw -Encoding UTF8 $progress|ConvertFrom-Json
$p.stage=$c.stage;$p|Add-Member -NotePropertyName camera_validation -NotePropertyValue $counts[1] -Force
$p|Add-Member -NotePropertyName pending_execution -NotePropertyValue $false -Force
[IO.File]::WriteAllText((Join-Path $repo $progress),($p|ConvertTo-Json -Depth 12)+"`n",$utf8)
$m=Get-Content -Raw -Encoding UTF8 $manifest|ConvertFrom-Json
$paths=@($m.files|ForEach-Object{$_.path})
$directories=@($validation)+@('.tooling/r1-portal-transition-prepared','.tooling/r1-game-camera-constructor-prepared','.tooling/r1-game-camera-angle-prepared','.tooling/r1-engine-portal-camera-prepared','.tooling/r1-game-camera-position-prepared')
foreach($d in @($batches)+@($dataDirs)){$directories+="docs/reverse-client/$d"}
foreach($d in $directories){$paths+=@(Get-ChildItem -LiteralPath $d -File|ForEach-Object{$_.FullName.Substring($repo.Length+1).Replace('\','/')})}
foreach($r in $results){$paths+=@($r.inputs|ForEach-Object{$_.path.Substring($repo.Length+1).Replace('\','/')})}
$paths+=@($checkpoint,'docs/reverse-client/R1-PORTAL-TRANSITION-RESULT.md','src/reverse_client_actions/README.md','tools/static_analysis/rea_native_registration.mjs','tools/static_analysis/build_portal_transition_contracts.ps1','tools/static_analysis/build_portal_camera_contracts.ps1','tools/static_analysis/seal_portal_checkpoint.ps1')
foreach($name in @('game-camera-constructor','game-camera-angle','engine-portal-camera','game-camera-position')){$paths+="tools/static_analysis/rea-r1-$name-plan.json"}
foreach($name in @('game_camera_anchor','camera_coordinates_binding','game_camera_slot_binding','camera_yaw_binding','game_camera_position_slot')){$paths+="tools/static_analysis/read_r1_$name.mjs"}
$inventory=@($paths|Sort-Object -Unique|ForEach-Object{
 $relative=$_;$full=[IO.Path]::GetFullPath((Join-Path $repo $relative))
 if(!$full.StartsWith($repo+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Inventory escaped workspace'}
 $item=Get-Item -LiteralPath $full;if($item.Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Redirected inventory item'}
 [pscustomobject]@{path=$relative;bytes=$item.Length;sha256=(Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash.ToLowerInvariant()}
})
$m.utc=[DateTime]::UtcNow.ToString('o');$m.status=$c.stage;$m.files=$inventory;$m.pending_execution=$false
$m.isolated_scenarios=($counts|Measure-Object scenarios -Sum).Sum;$m.isolated_assertions=($counts|Measure-Object assertions -Sum).Sum
$m.validation_scope='Current portal/camera stages only; previous suites preserved, not rerun'
[IO.File]::WriteAllText((Join-Path $repo $manifest),($m|ConvertTo-Json -Depth 15)+"`n",$utf8)
[pscustomobject]@{status='sealed';files=$inventory.Count;bytes=($inventory|Measure-Object bytes -Sum).Sum;validation=$counts;checkpoint_sha256=(Get-FileHash $checkpoint -Algorithm SHA256).Hash.ToLowerInvariant();manifest_sha256=(Get-FileHash $manifest -Algorithm SHA256).Hash.ToLowerInvariant();copied_evidence_trees=0;pending_execution=$false}|ConvertTo-Json -Depth 5
