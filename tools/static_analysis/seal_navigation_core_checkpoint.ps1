$ErrorActionPreference='Stop'
$repo='C:\Users\zhuzi\Documents\Grim-Dynasty'
if((Get-Location).Path -ne $repo){throw 'Unexpected workspace'}
$utf8=New-Object Text.UTF8Encoding($false)
$checkpoint='docs/reverse-client/r1-state-registration-chain/checkpoint.json'
$manifest='docs/reverse-client/r1-state-registration-chain/manifest.json'
$validation='.tooling/r1-navigation-core-validation'
function Hash([string]$path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
function SaveJson([string]$path,$value){[IO.File]::WriteAllText((Join-Path $repo $path),($value|ConvertTo-Json -Depth 40)+"`n",$utf8)}
$r=Get-Content -LiteralPath "$validation/result.json" -Raw -Encoding UTF8|ConvertFrom-Json
if($r.status -ne 'passed-isolated-navigation-core-contracts' -or $r.compile_library_exit -ne 0 -or $r.compile_test_exit -ne 0 -or $r.test_run_exit -ne 0){throw 'No complete passing validation'}
foreach($f in $r.inputs){if((Hash $f.path) -ne $f.sha256){throw 'Compiled input changed'}}
foreach($f in $r.dependencies){if((Hash (Join-Path $repo $f.file)) -ne $f.hash){throw 'Frozen dependency changed'}}
foreach($f in $r.artifacts){if((Hash $f.path) -ne $f.sha256){throw 'Compiled artifact changed'}}
$log=Get-Content -LiteralPath "$validation/tests.log" -Raw -Encoding UTF8
$match=[regex]::Match($log,'PASS (\d+) scenarios, (\d+) assertions;')
if(!$match.Success){throw 'Missing explicit test count'}
$counts=[pscustomobject]@{result="$validation/result.json";scenarios=[int]$match.Groups[1].Value;assertions=[int]$match.Groups[2].Value;old_suites_rerun=$false}
$batchDefs=@(
 @{name='r1-navigation-core';calls=14;hash='86ce524507630b0e63bef507b76de58d090a1308592c10eec0246705ede59922'},
 @{name='r1-navigation-single-poly';calls=17;hash='4e56505a90a6493e94452070770baeed213ba818b6b81ab5fa49b2213cb5eb0b'},
 @{name='r1-navigation-polygon-geometry';calls=8;hash='e3e3933bdeb13d693c64c39301f10a2353225122b45c8c49966131460b639b77'}
)
$batches=@()
foreach($b in $batchDefs){
 $path="docs/reverse-client/$($b.name)/summary.json"
 if((Hash $path) -ne $b.hash){throw 'Query summary changed'}
 $s=Get-Content -LiteralPath $path -Raw -Encoding UTF8|ConvertFrom-Json
 if($s.status -ne 'passed' -or $s.completed_plan_calls -ne $b.calls -or !$s.closed_session_verified -or !$s.process_exit_confirmed -or !$s.owned_memory_profile.job_empty_confirmed -or !$s.owned_memory_profile.sampler_exit_confirmed){throw 'Incomplete query or cleanup'}
 if($s.target.sha256_before -ne $s.target.sha256_after -or $s.target.sha256_before -ne '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153' -or $s.targets_launched){throw 'Invalid original protection evidence'}
 $batches+=[pscustomobject]@{summary=$path;sha256=$b.hash;calls=$b.calls;cleanup_verified=$true;original_hash_preserved=$true}
}
if(@(Get-ChildItem -LiteralPath 'analysis-runtime/rea' -Directory -Filter 'rea-ghidra-*').Count -ne 0){throw 'Active backend runtime remains'}
$constants='docs/reverse-client/r1-navigation-constants/inspection.json'
if((Hash $constants) -ne '16c88b4a9126a4752e2575e8dc02c4b87f9f25655660ff7289c69a4132fc754b'){throw 'Constant evidence changed'}
$c=Get-Content -LiteralPath $checkpoint -Raw -Encoding UTF8|ConvertFrom-Json
$c.stage='navigation-single-polygon-contracts-tested-not-gameplay'
$c.last_event_utc=[DateTime]::UtcNow.ToString('o')
$c.scope='Observed navigation reference validity, same-reference corridor and single-polygon endpoint geometry/output components; explicit supplied mesh/reference prerequisites. Nearest production, unequal-reference search/funnel, original query/data initialization, physical mouse coordinate source and scene/render remain unclosed.'
$c.new_tests=$counts.scenarios;$c.old_tests_rerun=$false;$c.current_uncompiled_changes=@();$c.r1_functional_acceptance=$false
$c.execution_state.owned_backend_active=$false;$c.execution_state.pending_command=$false;$c.execution_state.uncertain_outcome=$false
$c.execution_state.approval_denied=$false;$c.execution_state.last_execution_exit_code=0;$c.execution_state.last_all_cleanup_verified=$true
$c.next_bounded_scope.primary=@('261950 and260520: produce actual nearest references/points from input coordinates; do not replace with fixture observations','Identify query+10 and navigation tile/filter initialization through confirmed NavManager construction references, without whole-engine scanning','Then close unequal-reference search and funnel only for necessary scene routes; physical mouse target and actual scene/render remain separate requirements')
$c.next_bounded_scope.navigation=@('261CB0/262480 full bodies evidenced; only equal-reference/single-polygon execution implemented','Original navigation data loading and query publication remain unproven; typed fixture state is not recovered native initialization')
$c.next_bounded_scope.future_plan_prepared=$false;$c.next_bounded_scope.future_execution_requested=$false
$c.current_query=[pscustomobject]@{mode='--r1-navigation-polygon-geometry';plan='tools/static_analysis/rea-r1-navigation-polygon-geometry-plan.json';plan_sha256='c13fd848eaa2af9fd6d9c945594f420a44190780ed5ae7a719d2cb7e806c8242';runner_sha256='20158ee9318055835bb797d2caf8448233e1f7b8d3106520434592d9c6418eae';output='docs/reverse-client/r1-navigation-polygon-geometry';status='passed-8-of-8-cleanup-confirmed';exec_session_id=17198;exec_exited=$true;formal_approval='approved'}
$c|Add-Member -NotePropertyName navigation_core_component_result -NotePropertyValue ([pscustomobject]@{report='docs/reverse-client/R1-NAVIGATION-CORE-RESULT.md';validation=$counts;status='passed-isolated-contracts-not-gameplay';compile_attempts=1;compile_failures=0;test_failures=0;old_tests_rerun=$false;fixed_rea_calls=39;new_native_bodies=8;prior_queries_repeated=$false;evidence=$batches;constant_payload_bytes=8;native_data_initialization_proven=$false;nearest_production_implemented=$false;original_gameplay_accepted=$false}) -Force
SaveJson $checkpoint $c
foreach($name in @('navigation-core','navigation-single-poly','navigation-polygon-geometry')){
 $progress=".tooling/r1-$name-prepared/progress.json"
 $p=Get-Content -LiteralPath $progress -Raw -Encoding UTF8|ConvertFrom-Json
 $p.stage=$c.stage;$p.utc=$c.last_event_utc;$p.pending_execution=$false;$p.backend_started=$false
 $p|Add-Member -NotePropertyName validation -NotePropertyValue $counts -Force
 $p|Add-Member -NotePropertyName next_query_started -NotePropertyValue $false -Force
 SaveJson $progress $p
}
$m=Get-Content -LiteralPath $manifest -Raw -Encoding UTF8|ConvertFrom-Json
$paths=@($m.files|ForEach-Object{$_.path})
$directories=@($validation,'docs/reverse-client/r1-navigation-constants')
foreach($name in @('navigation-core','navigation-single-poly','navigation-polygon-geometry')){
 $directories+=".tooling/r1-$name-prepared";$directories+="docs/reverse-client/r1-$name"
 $paths+="tools/static_analysis/rea-r1-$name-plan.json"
}
foreach($d in $directories){$paths+=@(Get-ChildItem -LiteralPath $d -File|ForEach-Object{$_.FullName.Substring($repo.Length+1).Replace('\','/')})}
$paths+=@($r.inputs|ForEach-Object{$_.path.Substring($repo.Length+1).Replace('\','/')})
$paths+=@($checkpoint,'docs/reverse-client/R1-NAVIGATION-CORE-RESULT.md','docs/reverse-client/R1-NAVIGATION-SEARCH-NEXT.md','src/reverse_client_actions/README.md','tools/static_analysis/rea_native_registration.mjs','tools/static_analysis/read_r1_navigation_constants.mjs','tools/static_analysis/build_navigation_core_contracts.ps1','tools/static_analysis/seal_navigation_core_checkpoint.ps1','docs/object-lifecycle/offline-actor-remaining-dependencies/inspection.json')
$inventory=@($paths|Sort-Object -Unique|ForEach-Object{
 $relative=$_;$full=[IO.Path]::GetFullPath((Join-Path $repo $relative))
 if(!$full.StartsWith($repo+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Inventory escaped workspace'}
 $item=Get-Item -LiteralPath $full
 if($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Invalid inventory item'}
 [pscustomobject]@{path=$relative;bytes=$item.Length;sha256=(Hash $full)}
})
$m.utc=[DateTime]::UtcNow.ToString('o');$m.status=$c.stage;$m.files=$inventory;$m.pending_execution=$false
$m.isolated_scenarios=$counts.scenarios;$m.isolated_assertions=$counts.assertions
$m.validation_scope='Current navigation core stage only. Prior suites preserved, not rerun. Supplied mesh/ref fixture state is not native navigation initialization or gameplay.'
SaveJson $manifest $m
[pscustomobject]@{status='sealed';files=$inventory.Count;bytes=($inventory|Measure-Object bytes -Sum).Sum;validation=$counts;checkpoint_sha256=(Hash $checkpoint);manifest_sha256=(Hash $manifest);copied_evidence_trees=0;pending_execution=$false}|ConvertTo-Json -Depth 5
