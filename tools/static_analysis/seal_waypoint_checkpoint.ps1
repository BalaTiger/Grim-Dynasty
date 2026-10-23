$ErrorActionPreference='Stop'
$repo='C:\Users\zhuzi\Documents\Grim-Dynasty'
if((Get-Location).Path -ne $repo){throw 'Unexpected workspace'}
$utf8=New-Object Text.UTF8Encoding($false)
$checkpoint='docs/reverse-client/r1-state-registration-chain/checkpoint.json'
$manifest='docs/reverse-client/r1-state-registration-chain/manifest.json'
$result=Get-Content -Raw -Encoding UTF8 .tooling/r1-waypoint-contract-validation/result.json|ConvertFrom-Json
if($result.status -ne 'passed-isolated-waypoint-contracts' -or $result.compile_library_exit -ne 0){throw 'No passing validation'}
foreach($t in $result.tests){if($t.compile_exit -ne 0 -or $t.run_exit -ne 0){throw 'Incomplete validation'}}
foreach($f in $result.inputs){if((Get-FileHash -LiteralPath $f.path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $f.sha256){throw 'Compiled input changed'}}
$batches=@('r1-waypoint-execution','r1-waypoint-queue-helpers','r1-portal-dependencies')
foreach($batch in $batches){$s=Get-Content -Raw -Encoding UTF8 ("docs/reverse-client/$batch/summary.json")|ConvertFrom-Json;if($s.status -ne 'passed' -or !$s.closed_session_verified -or !$s.process_exit_confirmed -or !$s.owned_memory_profile.job_empty_confirmed -or !$s.owned_memory_profile.sampler_exit_confirmed){throw 'Batch cleanup incomplete'}}
$data=Get-Content -Raw -Encoding UTF8 docs/reverse-client/r1-portal-camera-data/inspection.json|ConvertFrom-Json
if($data.status -ne 'passed' -or !$data.stable -or !$data.source_closed){throw 'Data read incomplete'}
$c=Get-Content -Raw -Encoding UTF8 $checkpoint|ConvertFrom-Json
$c.stage='waypoint-selection-queue-ordinary-path-tested-not-gameplay'
$c.last_event_utc=[DateTime]::UtcNow.ToString('o')
$c.scope='Shared waypoint selection/clear/grow/iterator/append and ordinary next waypoint/no-portal production.39new isolated scenarios/268assertions; portal/camera/import/native-input/scene remain unclosed.'
$c.current_uncompiled_changes=@()
$c.portal_dependencies_inflight.status='passed-11-of-11-cleanup-confirmed';$c.portal_dependencies_inflight.result_unknown_until_observed=$false
$c.execution_state.owned_backend_active=$false;$c.execution_state.pending_command=$false;$c.execution_state.uncertain_outcome=$false
$c.execution_state.transient_transport_disconnect='Preparation command outcome was uncertain; recovered read verified original backup and unchanged runner before continuing. No query duplicated. All later batches and build completed.'
$c.next_bounded_scope.primary=@('Integrate evidenced portal three-record producer and full60-byte frame without invented orientation','Use019230 and existing2EBB70 evidence; camera global/instance/setter remains unclosed','Use selected atan2f binding and float32 pi; CRT exact FP modes remain unobserved')
$c.next_bounded_scope.future_plan_prepared=$false;$c.next_bounded_scope.future_execution_requested=$false
$stage=[ordered]@{report='docs/reverse-client/R1-WAYPOINT-RESULT.md';validation='.tooling/r1-waypoint-contract-validation/result.json';new_scenarios=39;new_assertions=268;portal_scenarios=16;portal_assertions=81;waypoint_scenarios=23;waypoint_assertions=187;old45_35_15_rerun=$false;compile_attempts=1;compile_failures=0;test_failures=0;new_library_sha256='51c00e823cd943ef9b102e1716ce7e7f87a48830cb039e83ef9e47e125a0ad30';fixed_rea_calls=39;new_bodies=8;cleanup_verified=$true;gameplay_accepted=$false;data_evidence='docs/reverse-client/r1-portal-camera-data/inspection.json';data_bytes=1208;data_owner_uniqueness_proved=$false;unvalidated_growth_branch='offset>growth';independent_review='state_chain_review source/plan/data review; selected import coverage does not exclude overlapping descriptors'}
$c|Add-Member -NotePropertyName waypoint_component_result -NotePropertyValue $stage -Force
[IO.File]::WriteAllText((Join-Path $repo $checkpoint),($c|ConvertTo-Json -Depth 40)+"`n",$utf8)
$m=Get-Content -Raw -Encoding UTF8 $manifest|ConvertFrom-Json
$paths=@($m.files|ForEach-Object{$_.path})
$directories=@('docs/reverse-client/r1-waypoint-execution','docs/reverse-client/r1-waypoint-queue-helpers','docs/reverse-client/r1-portal-dependencies','docs/reverse-client/r1-portal-camera-data','.tooling/r1-waypoint-contract-validation','.tooling/r1-waypoint-execution-prepared','.tooling/r1-waypoint-queue-helpers-prepared','.tooling/r1-portal-dependencies-prepared')
foreach($d in $directories){$paths+=@(Get-ChildItem -LiteralPath $d -File|ForEach-Object{$_.FullName.Substring($repo.Length+1).Replace('\','/')})}
$paths+=@($result.inputs|ForEach-Object{$_.path})
$paths+=@($checkpoint,'docs/reverse-client/R1-WAYPOINT-RESULT.md','docs/reverse-client/r1-state-registration-chain/EVIDENCE.md','src/reverse_client_actions/README.md','tools/static_analysis/rea_native_registration.mjs','tools/static_analysis/rea-r1-waypoint-execution-plan.json','tools/static_analysis/rea-r1-waypoint-queue-helpers-plan.json','tools/static_analysis/rea-r1-portal-dependencies-plan.json','tools/static_analysis/read_r1_portal_camera_data.mjs','tools/static_analysis/build_waypoint_contracts.ps1','tools/static_analysis/seal_waypoint_checkpoint.ps1')
$inventory=@($paths|Sort-Object -Unique|ForEach-Object{
 $relative=$_;$full=[IO.Path]::GetFullPath((Join-Path $repo $relative))
 if(!$full.StartsWith($repo+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Inventory escaped workspace'}
 $item=Get-Item -LiteralPath $full
 if($item.Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Redirected inventory item'}
 [pscustomobject]@{path=$relative;bytes=$item.Length;sha256=(Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash.ToLowerInvariant()}
})
$m.utc=[DateTime]::UtcNow.ToString('o');$m.status='compiled-waypoint-contracts-not-R1-game';$m.files=$inventory;$m.pending_execution=$false;$m.isolated_scenarios=39;$m.isolated_assertions=268
$m|Add-Member -NotePropertyName validation_scope -NotePropertyValue 'This waypoint stage only; prior passed suites preserved and not rerun' -Force
[IO.File]::WriteAllText((Join-Path $repo $manifest),($m|ConvertTo-Json -Depth 15)+"`n",$utf8)
[pscustomobject]@{status='sealed';files=$inventory.Count;bytes=($inventory|Measure-Object -Property bytes -Sum).Sum;checkpoint_sha256=(Get-FileHash -LiteralPath $checkpoint -Algorithm SHA256).Hash.ToLowerInvariant();manifest_sha256=(Get-FileHash -LiteralPath $manifest -Algorithm SHA256).Hash.ToLowerInvariant();copied_evidence_trees=0;pending_execution=$false}|ConvertTo-Json -Compress
