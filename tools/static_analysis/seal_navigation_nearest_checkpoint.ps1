$ErrorActionPreference='Stop'
$repo='C:\Users\zhuzi\Documents\Grim-Dynasty'
if((Get-Location).Path -ne $repo){throw 'Unexpected workspace'}
$utf8=New-Object Text.UTF8Encoding($false)
$checkpoint='docs/reverse-client/r1-state-registration-chain/checkpoint.json'
$manifest='docs/reverse-client/r1-state-registration-chain/manifest.json'
$validation='.tooling/r1-navigation-nearest-validation'
function Hash([string]$path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
function SaveJson([string]$path,$value){[IO.File]::WriteAllText((Join-Path $repo $path),($value|ConvertTo-Json -Depth 40)+"`n",$utf8)}
$r=Get-Content -LiteralPath "$validation/result.json" -Raw -Encoding UTF8|ConvertFrom-Json
if($r.status -ne 'passed-isolated-navigation-nearest-contracts' -or $r.compile_library_exit -ne 0 -or $r.compile_test_exit -ne 0 -or $r.test_run_exit -ne 0){throw 'No complete passing validation'}
foreach($f in $r.inputs){if((Hash $f.path) -ne $f.sha256){throw 'Compiled input changed'}}
foreach($f in $r.dependencies){if((Hash (Join-Path $repo $f.file)) -ne $f.hash){throw 'Frozen dependency changed'}}
foreach($f in $r.artifacts){if((Hash $f.path) -ne $f.sha256){throw 'Compiled artifact changed'}}
$match=[regex]::Match((Get-Content -LiteralPath "$validation/tests.log" -Raw -Encoding UTF8),'PASS (\d+) scenarios, (\d+) assertions;')
if(!$match.Success){throw 'Missing explicit test count'}
$counts=[pscustomobject]@{result="$validation/result.json";scenarios=[int]$match.Groups[1].Value;assertions=[int]$match.Groups[2].Value;old_suites_rerun=$false}
$batchDefs=@(
 @{name='navigation-nearest-helpers';calls=11;hash='981ab6c632e542f6a48449e4279d8781d87d888342214177d9c19108ba87a859'},
 @{name='navigation-nearest-dependencies';calls=14;hash='6cde3561346003056c716d757cb53259e6e987c3f86402cdfc5eaf5a0942924b'},
 @{name='navmanager-constructor';calls=8;hash='f3699df8eaa097692b4ce3fb872ac4e2bc0b119cc4285f125e2e74ffef78d9dc'},
 @{name='navigation-implementation-constructor';calls=8;hash='21bca14c454194ae6cf753cc539ef37771b2b07e5b52d9e06fe8689b74f98e81'},
 @{name='navigation-mesh-query-initialization';calls=11;hash='64a5f766300138d7145f3c8401a56fbe8f16946609375a1cc95928700c47b486'}
)
$batches=@()
foreach($b in $batchDefs){
 $path="docs/reverse-client/r1-$($b.name)/summary.json"
 if((Hash $path) -ne $b.hash){throw 'Query summary changed'}
 $s=Get-Content -LiteralPath $path -Raw -Encoding UTF8|ConvertFrom-Json
 if($s.status -ne 'passed' -or $s.completed_plan_calls -ne $b.calls -or !$s.closed_session_verified -or !$s.process_exit_confirmed -or !$s.owned_memory_profile.job_empty_confirmed -or !$s.owned_memory_profile.sampler_exit_confirmed){throw 'Incomplete query or cleanup'}
 if($s.target.sha256_before -ne $s.target.sha256_after -or $s.target.sha256_before -ne '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153' -or $s.targets_launched){throw 'Invalid original protection evidence'}
 $batches+=[pscustomobject]@{summary=$path;sha256=$b.hash;calls=$b.calls;cleanup_verified=$true;original_hash_preserved=$true}
}
if(@(Get-ChildItem -LiteralPath 'analysis-runtime/rea' -Directory -Filter 'rea-ghidra-*').Count -ne 0){throw 'Active backend runtime remains'}
$c=Get-Content -LiteralPath $checkpoint -Raw -Encoding UTF8|ConvertFrom-Json
$c.stage='navigation-nearest-contracts-tested-not-gameplay'
$c.last_event_utc=[DateTime]::UtcNow.ToString('o')
$c.scope='Recovered tile/BV candidate production, closest detail height and nearest selection on explicitly supplied mutable mesh views, integrated with the earlier same-poly path. Native constructor provenance is evidence, not real mesh population or playable navigation.'
$c.new_tests=$counts.scenarios;$c.old_tests_rerun=$false;$c.current_uncompiled_changes=@();$c.r1_functional_acceptance=$false
$c.execution_state.owned_backend_active=$false;$c.execution_state.pending_command=$false;$c.execution_state.uncertain_outcome=$false
$c.execution_state.approval_denied=$false;$c.execution_state.last_execution_exit_code=0;$c.execution_state.last_all_cleanup_verified=$true
$c.owned_backend_active=$false;$c.pending_command=$false
$c.resume_rule='Completed batches and new targeted tests retained, normal cleanup confirmed. Continue only the recorded direct initialization/data or cross-polygon dependencies with fresh guards and formal approval; never rerun occupied query modes or substitute fixture gameplay.'
$c.next_bounded_scope.primary=@('Close native query/mesh initialization and actual tile/data population through confirmed calls','Then necessary cross-polygon search/funnel and mouse-to-world target production, with real scene presentation required separately')
$c.next_bounded_scope.navigation=@('Nearest production and same-poly path implemented only within explicit owned finite typed storage and default FP domain','Original native mesh data and physical movement remain unverified; no real scene input or R1 game acceptance')
$c.next_bounded_scope.future_plan_prepared=$false;$c.next_bounded_scope.future_execution_requested=$false
$c|Add-Member -NotePropertyName navigation_nearest_component_result -NotePropertyValue ([pscustomobject]@{report='docs/reverse-client/R1-NAVIGATION-NEAREST-RESULT.md';validation=$counts;status='passed-isolated-contracts-not-gameplay';compile_attempts=1;compile_failures=0;test_failures=0;compiler_warnings=0;checkpoint_seal_attempts=2;checkpoint_seal_failures=1;old_tests_rerun=$false;fixed_rea_calls=($batchDefs|ForEach-Object{$_.calls}|Measure-Object -Sum).Sum;new_native_bodies=9;prior_queries_repeated=$false;evidence=$batches;constant_payload_bytes=12;import_export_metadata_bytes=156;native_mesh_population_proven=$false;nearest_production_implemented_in_explicit_domain=$true;original_gameplay_accepted=$false}) -Force
SaveJson $checkpoint $c
foreach($b in $batchDefs){
 $progress=".tooling/r1-$($b.name)-prepared/progress.json"
 $p=Get-Content -LiteralPath $progress -Raw -Encoding UTF8|ConvertFrom-Json
 $p.stage='completed-and-component-tested';$p.utc=$c.last_event_utc
 foreach($kv in @{pending_execution=$false;backend_started=$false;offline_verification='passed';formal_approval='approved-and-executed';independent_review='passed';validation=$counts}.GetEnumerator()){$p|Add-Member -NotePropertyName $kv.Key -NotePropertyValue $kv.Value -Force}
 $p|Add-Member -NotePropertyName completion_summary -NotePropertyValue "docs/reverse-client/r1-$($b.name)/summary.json" -Force
 $p|Add-Member -NotePropertyName next_action -NotePropertyValue 'This fixed batch completed; do not replay. Resume from main checkpoint direct data/search gaps.' -Force
 SaveJson $progress $p
}
$m=Get-Content -LiteralPath $manifest -Raw -Encoding UTF8|ConvertFrom-Json
$paths=@($m.files|ForEach-Object{$_.path})
$directories=@($validation,'docs/reverse-client/r1-navigation-nearest-constants','docs/reverse-client/r1-navigation-26d580-constants','docs/reverse-client/r1-navmanager-singleton-binding')
foreach($b in $batchDefs){$directories+=".tooling/r1-$($b.name)-prepared";$directories+="docs/reverse-client/r1-$($b.name)";$paths+="tools/static_analysis/rea-r1-$($b.name)-plan.json"}
foreach($d in $directories){$paths+=@(Get-ChildItem -LiteralPath $d -File|ForEach-Object{$_.FullName.Substring($repo.Length+1).Replace('\','/')})}
$paths+=@($r.inputs|ForEach-Object{$_.path.Substring($repo.Length+1).Replace('\','/')})
$paths+=@($checkpoint,'docs/reverse-client/R1-NAVIGATION-NEAREST-RESULT.md','src/reverse_client_actions/README.md','tools/static_analysis/rea_native_registration.mjs','tools/static_analysis/read_r1_navigation_nearest_constants.mjs','tools/static_analysis/read_r1_navigation_26d580_constants.mjs','tools/static_analysis/read_r1_navmanager_singleton_binding.mjs','tools/static_analysis/build_navigation_nearest_contracts.ps1','tools/static_analysis/seal_navigation_nearest_checkpoint.ps1')
$inventory=@($paths|Sort-Object -Unique|ForEach-Object{
 $relative=$_;$full=[IO.Path]::GetFullPath((Join-Path $repo $relative))
 if(!$full.StartsWith($repo+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Inventory escaped workspace'}
 $item=Get-Item -LiteralPath $full
 if($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Invalid inventory item'}
 [pscustomobject]@{path=$relative;bytes=$item.Length;sha256=(Hash $full)}
})
$m.utc=[DateTime]::UtcNow.ToString('o');$m.status=$c.stage;$m.files=$inventory;$m.pending_execution=$false
$m.isolated_scenarios=$counts.scenarios;$m.isolated_assertions=$counts.assertions
$m.validation_scope='Current nearest-production stage only. Prior suites preserved and not rerun. Supplied data is not native mesh population, input or gameplay.'
SaveJson $manifest $m
[pscustomobject]@{status='sealed';files=$inventory.Count;bytes=($inventory|Measure-Object bytes -Sum).Sum;validation=$counts;checkpoint_sha256=(Hash $checkpoint);manifest_sha256=(Hash $manifest);copied_evidence_trees=0;pending_execution=$false}|ConvertTo-Json -Depth 5
