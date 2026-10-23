$ErrorActionPreference='Stop'
$repo='C:\Users\zhuzi\Documents\Grim-Dynasty'
if((Get-Location).Path -ne $repo){throw 'Unexpected workspace'}
$utf8=New-Object Text.UTF8Encoding($false)
$checkpoint='docs/reverse-client/r1-state-registration-chain/checkpoint.json'
$manifest='docs/reverse-client/r1-state-registration-chain/manifest.json'
$validation='.tooling/r1-camera-target-validation'
function Hash([string]$path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
function SaveJson([string]$path,$value){[IO.File]::WriteAllText((Join-Path $repo $path),($value|ConvertTo-Json -Depth 40)+"`n",$utf8)}
$r=Get-Content -LiteralPath "$validation/result.json" -Raw -Encoding UTF8|ConvertFrom-Json
if($r.status -ne 'passed-isolated-camera-target-contracts' -or $r.compile_library_exit -ne 0 -or $r.compile_test_exit -ne 0 -or $r.test_run_exit -ne 0){throw 'No complete passing validation'}
# Validate the newly compiled sources only. Older suites retain their historic source hashes.
foreach($f in $r.inputs){if((Hash $f.path) -ne $f.sha256){throw 'Compiled input changed'}}
foreach($f in $r.dependencies){if((Hash (Join-Path $repo $f.file)) -ne $f.hash){throw 'Frozen dependency changed'}}
foreach($f in $r.artifacts){if((Hash $f.path) -ne $f.sha256){throw 'Compiled artifact changed'}}
$log=Get-Content -LiteralPath "$validation/tests.log" -Raw -Encoding UTF8
$match=[regex]::Match($log,'PASS (\d+) scenarios, (\d+) assertions;')
if(!$match.Success -or $match.Groups[1].Value -ne '28' -or $match.Groups[2].Value -ne '273'){throw 'Unexpected test count'}
$counts=[pscustomobject]@{result="$validation/result.json";scenarios=28;assertions=273;old_suites_rerun=$false}
$batchDefs=@(
 @{name='r1-engine-camera-target';calls=14;hash='2585a18d21187d6ec1d8405bdd8e92c837d484aeba857edc551358a209ff17a6'},
 @{name='r1-engine-navigation-search';calls=8;hash='1124d1801ed4fc1122baa71d093e1723a7ad252c3e2a276965e0ccdbdc1dbe7c'}
)
$batches=@()
foreach($b in $batchDefs){
 $path="docs/reverse-client/$($b.name)/summary.json"
 if((Hash $path) -ne $b.hash){throw 'Query summary changed'}
 $s=Get-Content -LiteralPath $path -Raw -Encoding UTF8|ConvertFrom-Json
 if($s.status -ne 'passed' -or $s.completed_plan_calls -ne $b.calls -or !$s.closed_session_verified -or !$s.process_exit_confirmed -or !$s.owned_memory_profile.job_empty_confirmed -or !$s.owned_memory_profile.sampler_exit_confirmed){throw 'Incomplete query or backend cleanup'}
 if($s.target.sha256_before -ne $s.target.sha256_after -or $s.target.sha256_before -ne '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153' -or $s.targets_launched){throw 'Invalid original protection evidence'}
 $batches+=[pscustomobject]@{summary=$path;sha256=$b.hash;calls=$b.calls;cleanup_verified=$true;original_hash_preserved=$true}
}
if(@(Get-ChildItem -LiteralPath 'analysis-runtime/rea' -Directory -Filter 'rea-ghidra-*').Count -ne 0){throw 'Active backend runtime remains'}
$data='docs/reverse-client/r1-camera-target-bindings/inspection.json'
if((Hash $data) -ne '2a64f2baaf6549b24fc2d0e3071c6e91c5ff2b18de3195d990dc32b23026608a'){throw 'Binding evidence changed'}
$c=Get-Content -LiteralPath $checkpoint -Raw -Encoding UTF8|ConvertFrom-Json
$c.stage='camera-target-integrated-navigation-dependencies-evidenced-not-gameplay'
$c.last_event_utc=[DateTime]::UtcNow.ToString('o')
$c.scope='Evidence-based GameCamera GetTarget, region/translation/offset helpers and shared portal basis bridge compiled and tested. Navigation 10C6C0 body evidenced; its three search dependencies and actual navigation data, physical mouse target source and scene/render remain unclosed.'
$c.new_tests=28;$c.old_tests_rerun=$false;$c.current_uncompiled_changes=@();$c.r1_functional_acceptance=$false
$c.execution_state.owned_backend_active=$false;$c.execution_state.pending_command=$false;$c.execution_state.uncertain_outcome=$false
$c.execution_state.approval_denied=$false;$c.execution_state.last_execution_exit_code=0;$c.execution_state.last_all_cleanup_verified=$true
$c.next_bounded_scope.primary=@('Engine2610F0,261CB0,262480: exact direct bodies called by10C6C0; no algorithm inferred from structure','Establish navigation query+10/filter+18, valid region mapping and actual navigation data; preserve cached-ref and partial-output semantics','Return to physical mouse concrete virtual slot+28 and target coordinate source, then actual scene/render; no WASD replacement')
$c.next_bounded_scope.navigation=@('10C6C0 body now evidenced, not implemented; next three direct search bodies and real data remain unknown','Translate22C350/PutOnFloor dependencies are distinct from the now-evidenced TranslateInRegion22C5D0')
$c.next_bounded_scope.future_plan_prepared=$false;$c.next_bounded_scope.future_execution_requested=$false
$c.current_query=[pscustomobject]@{mode='--r1-engine-navigation-search';plan='tools/static_analysis/rea-r1-engine-navigation-search-plan.json';plan_sha256='8e8d2f1c6dac84a7d56065b8b5b9e794c847d93083ed52350cfb17b990ffc22d';runner_sha256='7608be5c463764feeb561e1fd9d099efcd95e8ac199803d79b9953ad141fdcbe';output='docs/reverse-client/r1-engine-navigation-search';status='passed-8-of-8-cleanup-confirmed';exec_session_id=37761;exec_exited=$true;formal_approval='approved'}
$c|Add-Member -NotePropertyName camera_target_result -NotePropertyValue ([pscustomobject]@{report='docs/reverse-client/R1-CAMERA-TARGET-RESULT.md';validation=$counts;status='passed-isolated-contracts-not-gameplay';compile_attempts=1;compile_failures=0;test_failures=0;old_tests_rerun=$false;production_helpers_evidenced=$true;portal_basis_only_bridge=$true;native_constructed_camera=$false;whole_coordinate_projection_supported=$false;original_gameplay_accepted=$false;fixed_rea_calls=22;prior_queries_repeated=$false;evidence=$batches}) -Force
$c|Add-Member -NotePropertyName navigation_search_result -NotePropertyValue ([pscustomobject]@{report='docs/reverse-client/R1-NAVIGATION-SEARCH-NEXT.md';evidence=$batches[1];implemented=$false;next_direct_targets=@('0x1802610f0','0x180261cb0','0x180262480');next_plan_prepared=$false;next_query_started=$false}) -Force
$c.portal_camera_result|Add-Member -NotePropertyName historical_scope -NotePropertyValue 'Prior 49-scenario stage only; GetTarget implementation status superseded by camera_target_result. Old results and artifacts preserved without rerun.' -Force
SaveJson $checkpoint $c
$progress='.tooling/r1-camera-target-prepared/progress.json'
$p=Get-Content -LiteralPath $progress -Raw -Encoding UTF8|ConvertFrom-Json
$p.stage='camera-target-integrated-and-tested-not-gameplay';$p.utc=$c.last_event_utc;$p.pending_execution=$false;$p.build_started=$true
$p|Add-Member -NotePropertyName validation -NotePropertyValue $counts -Force
$p|Add-Member -NotePropertyName build_outcome -NotePropertyValue 'passed-first-attempt-28-scenarios-273-assertions' -Force
SaveJson $progress $p
$navProgress='.tooling/r1-navigation-search-prepared/progress.json'
$n=Get-Content -LiteralPath $navProgress -Raw -Encoding UTF8|ConvertFrom-Json
$n|Add-Member -NotePropertyName next_plan_prepared -NotePropertyValue $false -Force
$n|Add-Member -NotePropertyName next_query_started -NotePropertyValue $false -Force
SaveJson $navProgress $n
$m=Get-Content -LiteralPath $manifest -Raw -Encoding UTF8|ConvertFrom-Json
$paths=@($m.files|ForEach-Object{$_.path})
$directories=@($validation,'.tooling/r1-camera-target-prepared','.tooling/r1-navigation-search-prepared','docs/reverse-client/r1-camera-target-bindings','docs/reverse-client/r1-engine-camera-target','docs/reverse-client/r1-engine-navigation-search')
foreach($d in $directories){$paths+=@(Get-ChildItem -LiteralPath $d -File|ForEach-Object{$_.FullName.Substring($repo.Length+1).Replace('\','/')})}
$paths+=@($r.inputs|ForEach-Object{$_.path.Substring($repo.Length+1).Replace('\','/')})
$paths+=@($checkpoint,'docs/reverse-client/R1-CAMERA-TARGET-RESULT.md','docs/reverse-client/R1-CAMERA-TARGET-PROGRESS.md','docs/reverse-client/R1-NAVIGATION-SEARCH-NEXT.md','src/reverse_client_actions/README.md','tools/static_analysis/rea_native_registration.mjs','tools/static_analysis/read_r1_camera_target_bindings.mjs','tools/static_analysis/build_camera_target_contracts.ps1','tools/static_analysis/seal_camera_target_checkpoint.ps1','tools/static_analysis/rea-r1-engine-camera-target-plan.json','tools/static_analysis/rea-r1-engine-navigation-search-plan.json')
$inventory=@($paths|Sort-Object -Unique|ForEach-Object{
 $relative=$_;$full=[IO.Path]::GetFullPath((Join-Path $repo $relative))
 if(!$full.StartsWith($repo+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Inventory escaped workspace'}
 $item=Get-Item -LiteralPath $full
 if($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Invalid inventory item'}
 [pscustomobject]@{path=$relative;bytes=$item.Length;sha256=(Hash $full)}
})
$m.utc=[DateTime]::UtcNow.ToString('o');$m.status=$c.stage;$m.files=$inventory;$m.pending_execution=$false
$m.isolated_scenarios=28;$m.isolated_assertions=273
$m.validation_scope='Current camera target stage only. Previous suites and artifacts preserved, not rerun. Navigation body evidenced but not implemented.'
SaveJson $manifest $m
[pscustomobject]@{status='sealed';files=$inventory.Count;bytes=($inventory|Measure-Object bytes -Sum).Sum;validation=$counts;checkpoint_sha256=(Hash $checkpoint);manifest_sha256=(Hash $manifest);copied_evidence_trees=0;pending_execution=$false}|ConvertTo-Json -Depth 5
