param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$manifestPath=Join-Path $taskRoot 'tools\r1_controller_move_production_build_fingerprints.json'
$manifestHash='20de5658558638e5a7aa19daed5c0b425a932de5337373e64141226dcc7a1dde'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$output=Join-Path $taskRoot '.tooling\r1-controller-move-production-validation'
function Hash([string]$p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-Ancestors([string]$p){$cursor=[IO.Path]::GetFullPath($p);while($cursor){if(Test-Path -LiteralPath $cursor){if((Get-Item -LiteralPath $cursor).Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Reparse path rejected: '+$cursor)}};$parent=[IO.Directory]::GetParent($cursor);if($null -eq $parent){break};$cursor=$parent.FullName}}
function Under-Root([string]$relative){$p=[IO.Path]::GetFullPath((Join-Path $taskRoot $relative));if(!$p.StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Manifest path leaves workspace'};return $p}
function Check-File([string]$p,[string]$expected){Check-Ancestors $p;if($expected -notmatch '^[0-9a-f]{64}$'){throw ('Unfrozen input: '+$p)};if((Get-Item -LiteralPath $p).PSIsContainer -or (Hash $p) -ne $expected){throw ('Input identity changed: '+$p)}}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace; no arguments'}
Check-File $manifestPath $manifestHash
$manifest=Get-Content -LiteralPath $manifestPath -Raw|ConvertFrom-Json
if($manifest.schema -ne 'grim-dynasty.fixed-controller-move-production-build.v1' -or $manifest.preparation_status -ne 'frozen-not-executed' -or $manifest.compiles -ne 4 -or $manifest.sources.Count -ne 15 -or $manifest.assemblies.Count -ne 40){throw 'Unexpected or unfrozen fixed plan'}
$fixedActions='src/reverse_client_actions/ActionDispatch.cs|src/reverse_client_actions/ActionExecutor.cs|src/reverse_client_actions/MoveActionState.cs|src/reverse_client_actions/MoveBoundaries.cs|src/reverse_client_actions/MoveContracts.cs'
$fixedProduction='src/reverse_client_actions/GameControllerMoveProductionContracts.cs'
$fixedRequest='src/reverse_client_actions/PlayerMoveRequestContracts.cs|src/reverse_client_actions/ControllerStateTransitionState.cs|src/reverse_client_actions/ControllerStateTransitionContracts.cs|src/reverse_client_actions/GameDurationDamageManager.cs|src/reverse_client_actions/GameRunSpeedState.cs|src/reverse_client_actions/GameRunSpeedContracts.cs'
$fixedTests='src/reverse_client_actions/PlayerMoveRequestTests.cs|src/reverse_client_actions/GameControllerMoveProductionTests.cs|src/reverse_client_actions/GameControllerMoveProductionIntegrationTests.cs'
if(($manifest.actions_sources -join '|') -ne $fixedActions -or ($manifest.production_sources -join '|') -ne $fixedProduction -or ($manifest.request_sources -join '|') -ne $fixedRequest -or ($manifest.test_sources -join '|') -ne $fixedTests -or ($manifest.sources.path -join '|') -ne ($fixedActions+'|'+$fixedProduction+'|'+$fixedRequest+'|'+$fixedTests)){throw 'Unexpected source partition'}
if($manifest.component_scenarios -ne 10 -or $manifest.integration_scenarios -ne 2 -or $manifest.affected_regression_scenarios -ne 1){throw 'Unexpected fixed test counts'}
if(@($manifest.assemblies.name|Group-Object|Where-Object{$_.Count -ne 1}).Count -ne 0){throw 'Duplicate assembly name'}
foreach($entry in $manifest.assemblies){if($entry.name -ne [IO.Path]::GetFileName($entry.name) -or $entry.name -in @('GrimDynasty.Actions.dll','GrimDynasty.PlayerMoveRequest.dll') -or (!$entry.name.EndsWith('.dll') -and $entry.name -notin @('NavigationSearchWrapperTests.exe','NativeAttributeGetterChecks.exe'))){throw 'Unexpected or shadowing managed dependency'}}
Check-Ancestors $output
if(Test-Path -LiteralPath $output){throw 'Existing output preserved; no overwrite/retry'}
function Check-Inputs {
 Check-File $compiler $compilerHash;Check-File $manifestPath $manifestHash
 foreach($entry in @($manifest.sources)+@($manifest.evidence)+@($manifest.fixture_resources)+@($manifest.unchanged_dependency_sources)+@($manifest.prior_actions_preserved)){Check-File (Under-Root $entry.path) $entry.sha256}
 $receipts=@{}
 foreach($entry in $manifest.prerequisites){Check-File (Under-Root $entry.path) $entry.sha256;$prior=Get-Content -LiteralPath (Under-Root $entry.path) -Raw|ConvertFrom-Json;if($prior.status -ne $entry.status -or $null -ne $prior.failure){throw ('Prerequisite is not passed: '+$entry.path)};$receipts[$entry.path]=$prior}
 foreach($entry in $manifest.assemblies){Check-File (Under-Root $entry.path) $entry.sha256;if(!$receipts.ContainsKey($entry.receipt)){throw 'Missing dependency receipt'};$record=@($receipts[$entry.receipt].artifacts|Where-Object{$_.name -eq $entry.name -and $_.sha256 -eq $entry.sha256});if($record.Count -ne 1){throw ('Dependency artifact receipt mismatch: '+$entry.name)}}
 foreach($entry in $manifest.rea_completed){Check-File (Under-Root $entry.path) $entry.sha256;$rea=Get-Content -LiteralPath (Under-Root $entry.path) -Raw|ConvertFrom-Json;if($rea.status -ne 'passed' -or $rea.completed_plan_calls -ne $entry.calls -or !$rea.closed_session_verified -or !$rea.process_exit_confirmed -or !$rea.owned_memory_profile.job_empty_confirmed -or !$rea.owned_memory_profile.sampler_exit_confirmed -or $rea.target.sha256_before -ne $entry.target_sha256 -or $rea.target.sha256_after -ne $entry.target_sha256){throw ('Unclosed or changed saved Rea evidence: '+$entry.path)}}
}
Check-Inputs
$report=[ordered]@{started_utc=[DateTime]::UtcNow.ToString('o');status='running';manifest_sha256=$manifestHash;compiler_sha256=$compilerHash;source_pins=$manifest.sources;assembly_pins=$manifest.assemblies;evidence_pins=$manifest.evidence;steps=@();artifacts=@();failure=$null;compiles=4;new_component_scenarios=0;integration_scenarios=0;affected_regression_scenarios=0;component_run_assertions=0;integration_assertions=0;assertion_count_scope='Component assertion count includes one affected old Dispatch API regression; no separate new-only assertion total is claimed.';old_test_suites_rerun=0;old_executable_entrypoints_invoked=@();old_sources_recompiled=@($manifest.actions_sources)+@($manifest.request_sources);original_read=$false;original_executed=$false;backend_started=$false;system_library_loaded=$false;native_math_calls=0;physical_mouse=$false;walking_accepted=$false;action_submission_completed=$false;handler_prefix_observed=$false;gameplay_accepted=$false;unsupported_rva='0x082c98';support_boundary='Actual saved-map request, navigation, state entry, run speed, MoveTo constructor and local dispatch reach the unknown Gate26AF at082C98. Handler current/busy prefix is retained; Begin, action submission and walking remain incomplete.'}
New-Item -ItemType Directory -Path $output|Out-Null
function Run-Step([string]$name,[string]$program,[string[]]$arguments){
 $stdout=Join-Path $output ($name+'.log');$stderr=Join-Path $output ($name+'.stderr.log');$savedPreference=$ErrorActionPreference
 try{$ErrorActionPreference='Continue';& $program @arguments 1> $stdout 2> $stderr;$ok=$?;$code=$LASTEXITCODE}finally{$ErrorActionPreference=$savedPreference}
 $report.steps += [ordered]@{name=$name;program=$program;arguments=$arguments;invocation_succeeded=$ok;exit_code=$code}
 if(!$ok -or $code -ne 0){throw "Step $name failed (invocation=$ok, exit=$code); evidence retained"}
 if((Get-Item -LiteralPath $stderr).Length -ne 0 -or ($name.StartsWith('compile-') -and (Get-Item -LiteralPath $stdout).Length -ne 0)){throw ('Unexpected diagnostics: '+$name)}
}
try{
 foreach($entry in $manifest.assemblies){Copy-Item -LiteralPath (Under-Root $entry.path) -Destination (Join-Path $output $entry.name);Check-File (Join-Path $output $entry.name) $entry.sha256}
 $flags=@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64')
 $actions=Join-Path $output 'GrimDynasty.Actions.dll';$production=Join-Path $output 'GrimDynasty.ControllerMoveProduction.dll';$request=Join-Path $output 'GrimDynasty.PlayerMoveRequest.dll';$tests=Join-Path $output 'ControllerMoveProductionTests.exe'
 Run-Step 'compile-actions' $compiler ($flags+@('/target:library',"/out:$actions")+@($manifest.actions_sources|ForEach-Object{Under-Root $_}))
 $references=@("/reference:$actions")+@($manifest.assemblies|ForEach-Object{'/reference:'+(Join-Path $output $_.name)})
 Run-Step 'compile-controller-move-production' $compiler ($flags+@('/target:library',"/out:$production")+$references+@($manifest.production_sources|ForEach-Object{Under-Root $_}))
 Run-Step 'compile-player-move-request' $compiler ($flags+@('/target:library',"/out:$request","/reference:$production")+$references+@($manifest.request_sources|ForEach-Object{Under-Root $_}))
 Run-Step 'compile-controller-move-tests' $compiler ($flags+@('/target:exe','/main:GameControllerMoveProductionValidationProgram',"/out:$tests","/reference:$production","/reference:$request")+$references+@($manifest.test_sources|ForEach-Object{Under-Root $_}))
 foreach($mode in @('components','integration')){
  Run-Step $mode $tests @($mode)
  $summary=Get-Content -LiteralPath (Join-Path $output ($mode+'.log')) -Tail 1|ConvertFrom-Json
  $expected=if($mode -eq 'components'){11}else{2}
  if($summary.status -ne 'passed-controller-move-production-prefix-only' -or $summary.mode -ne $mode -or $summary.scenarios -ne $expected -or $summary.assertions -le 0 -or $summary.old_test_suites_rerun -ne 0 -or $summary.native_math_calls -ne 0 -or $summary.handler_prefix_observed -ne $true){throw ('Unexpected summary: '+$mode)}
  foreach($flag in @('original_read','original_executed','backend_started','system_library_loaded','physical_mouse','walking_accepted','action_submission_completed','gameplay_accepted')){if($summary.$flag -ne $false){throw ('Scope flag changed: '+$flag)}}
  if($mode -eq 'components'){
   if($summary.component_scenarios -ne 10 -or $summary.integration_scenarios -ne 0 -or $summary.affected_regression_scenarios -ne 1){throw 'Unexpected component/regression counts'}
   $report.new_component_scenarios=10;$report.affected_regression_scenarios=1;$report.component_run_assertions=$summary.assertions;$report['component_test_result']=$summary
  }else{
   if($summary.component_scenarios -ne 0 -or $summary.integration_scenarios -ne 2 -or $summary.affected_regression_scenarios -ne 0 -or $summary.stop_rva -ne '0x82c98' -or $summary.nav_result -ne 0 -or $summary.corridor_count -le 1 -or $summary.ai_lookup_count -ne 4 -or $summary.begin_returned -ne $false){throw 'Unexpected actual saved-map boundary'}
   foreach($flag in @('run_speed_returned','constructor_returned','handler_published','begin_entered','alive_returned','coordinates_returned')){if($summary.$flag -ne $true){throw ('Missing actual integration prefix: '+$flag)}}
   $report.integration_scenarios=2;$report.integration_assertions=$summary.assertions;$report.handler_prefix_observed=$true;$report['integration_test_result']=$summary
  }
 }
 Check-Inputs;foreach($entry in $manifest.assemblies){Check-File (Join-Path $output $entry.name) $entry.sha256}
 $report.status='passed-controller-move-production-prefix-and-saved-map-integration-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}};$report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;new_component_scenarios=$report.new_component_scenarios;component_run_assertions=$report.component_run_assertions;integration_scenarios=$report.integration_scenarios;integration_assertions=$report.integration_assertions;affected_regression_scenarios=1;old_test_suites_rerun=0;unsupported_rva=$report.unsupported_rva;action_submission_completed=$false;walking_accepted=$false;result=(Join-Path $output 'result.json')}|ConvertTo-Json
