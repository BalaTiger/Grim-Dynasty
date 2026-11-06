param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$manifestPath=Join-Path $taskRoot 'tools\r1_move_begin_integration_retry1_build_fingerprints.json'
$manifestHash='13e1835763cb7f65dcb46cd0ebcd4cc98094f80cba28e148caf881f0d2c6eac2'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$output=Join-Path $taskRoot '.tooling\r1-move-begin-integration-validation-retry1'
function Hash([string]$p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-Ancestors([string]$p){$cursor=[IO.Path]::GetFullPath($p);while($cursor){if(Test-Path -LiteralPath $cursor){if((Get-Item -LiteralPath $cursor).Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Reparse path rejected: '+$cursor)}};$parent=[IO.Directory]::GetParent($cursor);if($null -eq $parent){break};$cursor=$parent.FullName}}
function Under-Root([string]$relative){$p=[IO.Path]::GetFullPath((Join-Path $taskRoot $relative));if(!$p.StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Manifest path leaves workspace'};return $p}
function Check-File([string]$p,[string]$expected){Check-Ancestors $p;if($expected -notmatch '^[0-9a-f]{64}$'){throw ('Unfrozen input: '+$p)};if((Get-Item -LiteralPath $p).PSIsContainer -or (Hash $p) -ne $expected){throw ('Input identity changed: '+$p)}}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace; no arguments'}
Check-File $manifestPath $manifestHash
$manifest=Get-Content -LiteralPath $manifestPath -Raw|ConvertFrom-Json
if($manifest.schema -ne 'grim-dynasty.fixed-move-begin-integration-retry1-build.v1' -or $manifest.preparation_status -ne 'frozen-not-executed' -or $manifest.compiles -ne 1 -or $manifest.production_sources_compiled -ne $false -or $manifest.sources.Count -ne 8 -or $manifest.assemblies.Count -ne 43){throw 'Unexpected or unfrozen fixed plan'}
$fixedProduction='src/reverse_client_actions/GameControllerMoveProductionContracts.cs|src/reverse_client_actions/GameAssociatedLookupContracts.cs'
$fixedTests='src/reverse_client_actions/PlayerMoveRequestTests.cs|src/reverse_client_actions/GameControllerMoveProductionTests.cs|src/reverse_client_actions/GameControllerMoveProductionIntegrationTests.cs|src/reverse_client_actions/GameAssociatedLookupTests.cs|src/reverse_client_actions/GameControllerMoveBeginIntegrationTests.cs|src/reverse_client_actions/GameMoveBeginValidationProgram.cs'
if(($manifest.production_sources -join '|') -ne $fixedProduction -or ($manifest.test_sources -join '|') -ne $fixedTests -or ($manifest.sources.path -join '|') -ne ($fixedProduction+'|'+$fixedTests)){throw 'Unexpected source partition'}
if($manifest.associated_scenarios -ne 12 -or $manifest.begin_scenarios -ne 11 -or $manifest.begin_affected_regression_scenarios -ne 2 -or $manifest.old_suites_to_run.Count -ne 0){throw 'Unexpected fixed test scope'}
if(@($manifest.assemblies.name|Group-Object|Where-Object{$_.Count -ne 1}).Count -ne 0){throw 'Duplicate assembly name'}
foreach($entry in $manifest.assemblies){if($entry.name -ne [IO.Path]::GetFileName($entry.name) -or $entry.name -eq 'GrimDynasty.ControllerMoveProduction.dll' -or (!$entry.name.EndsWith('.dll') -and $entry.name -notin @('NavigationSearchWrapperTests.exe','NativeAttributeGetterChecks.exe'))){throw 'Unexpected or shadowing managed dependency'}}
Check-Ancestors $output
if(Test-Path -LiteralPath $output){throw 'Existing output preserved; no overwrite/retry'}
function Check-Inputs {
 Check-File $compiler $compilerHash;Check-File $manifestPath $manifestHash
 Check-File (Under-Root $manifest.production_artifact.path) $manifest.production_artifact.sha256
 if($manifest.production_artifact.sha256 -ne '3737de04c31caf3e70b33d357360b7d85739bc75b5b52bdc4672f58c1976079a'){throw 'Unexpected production artifact'}
 foreach($pin in @($manifest.prior_failed_run.result,$manifest.prior_failed_run.manifest,$manifest.prior_failed_run.error)){Check-File (Under-Root $pin.path) $pin.sha256}
 $failed=Get-Content -LiteralPath (Under-Root $manifest.prior_failed_run.result.path) -Raw|ConvertFrom-Json
 if($failed.status -ne 'failed' -or $failed.manifest_sha256 -ne $manifest.prior_failed_run.manifest.sha256 -or $failed.steps.Count -ne 3 -or $failed.failure -ne 'Step associated failed (invocation=False, exit=1); evidence retained'){throw 'Previous failure changed'}
 if($failed.steps[0].name -ne 'compile-controller-move-production' -or !$failed.steps[0].invocation_succeeded -or $failed.steps[0].exit_code -ne 0 -or $failed.steps[1].name -ne 'compile-move-begin-tests' -or !$failed.steps[1].invocation_succeeded -or $failed.steps[1].exit_code -ne 0 -or $failed.steps[2].name -ne 'associated' -or $failed.steps[2].exit_code -ne 1){throw 'Previous compile/test sequence changed'}
 if(@($failed.artifacts|Where-Object{$_.name -eq 'GrimDynasty.ControllerMoveProduction.dll' -and $_.sha256 -eq $manifest.production_artifact.sha256}).Count -ne 1){throw 'Compiled production receipt mismatch'}
 foreach($source in $manifest.production_sources){$current=@($manifest.sources|Where-Object{$_.path -eq $source});$compiled=@($failed.source_pins|Where-Object{$_.path -eq $source});if($current.Count -ne 1 -or $compiled.Count -ne 1 -or $current[0].sha256 -ne $compiled[0].sha256){throw 'Production source differs from compiled receipt'}}
 foreach($entry in @($manifest.sources)+@($manifest.evidence)+@($manifest.fixture_resources)+@($manifest.unchanged_dependency_sources)+@($manifest.prior_artifacts_preserved)){Check-File (Under-Root $entry.path) $entry.sha256}
 $receipts=@{}
 foreach($entry in $manifest.prerequisites){Check-File (Under-Root $entry.path) $entry.sha256;$prior=Get-Content -LiteralPath (Under-Root $entry.path) -Raw|ConvertFrom-Json;if($prior.status -ne $entry.status -or $null -ne $prior.failure){throw ('Prerequisite is not passed: '+$entry.path)};$receipts[$entry.path]=$prior}
 foreach($entry in $manifest.assemblies){Check-File (Under-Root $entry.path) $entry.sha256;if(!$receipts.ContainsKey($entry.receipt)){throw 'Missing dependency receipt'};$record=@($receipts[$entry.receipt].artifacts|Where-Object{$_.name -eq $entry.name -and $_.sha256 -eq $entry.sha256});if($record.Count -ne 1){throw ('Dependency artifact receipt mismatch: '+$entry.name)}}
 foreach($entry in $manifest.rea_completed){Check-File (Under-Root $entry.path) $entry.sha256;$rea=Get-Content -LiteralPath (Under-Root $entry.path) -Raw|ConvertFrom-Json;if($rea.status -ne 'passed' -or $rea.completed_plan_calls -ne $entry.calls -or !$rea.closed_session_verified -or !$rea.process_exit_confirmed -or !$rea.owned_memory_profile.job_empty_confirmed -or !$rea.owned_memory_profile.sampler_exit_confirmed -or $rea.target.sha256_before -ne $entry.target_sha256 -or $rea.target.sha256_after -ne $entry.target_sha256){throw ('Unclosed or changed saved Rea evidence: '+$entry.path)}}
}
Check-Inputs
$report=[ordered]@{started_utc=[DateTime]::UtcNow.ToString('o');status='running';manifest_sha256=$manifestHash;compiler_sha256=$compilerHash;source_pins=$manifest.sources;assembly_pins=$manifest.assemblies;evidence_pins=$manifest.evidence;steps=@();artifacts=@();failure=$null;compiles=1;production_compile_reused=$true;production_artifact=$manifest.production_artifact;prior_failed_run=$manifest.prior_failed_run;test_correction=$manifest.test_correction;associated_scenarios=0;associated_assertions=0;begin_scenarios=0;begin_assertions=0;new_integration_scenarios=0;affected_regression_scenarios=0;prior_fixture_setup_checks_excluded=0;assertion_count_scope='Associated assertion count reruns the corrected previously failed component mode. Begin was not executed previously and combines9new integration and2affected regressions;28oldfixturechecks excluded.';old_test_suites_rerun=0;old_executable_entrypoints_invoked=@();old_sources_recompiled_for_fixture_only=@($manifest.test_sources[0..2]);original_read=$false;original_executed=$false;backend_started=$false;system_library_loaded=$false;native_math_calls=0;physical_mouse=$false;walking_accepted=$false;action_submission_completed=$false;gameplay_accepted=$false;support_boundary=$manifest.boundary}
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
 Copy-Item -LiteralPath (Under-Root $manifest.production_artifact.path) -Destination (Join-Path $output 'GrimDynasty.ControllerMoveProduction.dll')
 Check-File (Join-Path $output 'GrimDynasty.ControllerMoveProduction.dll') $manifest.production_artifact.sha256
 $flags=@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64')
 $production=Join-Path $output 'GrimDynasty.ControllerMoveProduction.dll';$tests=Join-Path $output 'GameMoveBeginTests.exe'
 $references=@($manifest.assemblies|ForEach-Object{'/reference:'+(Join-Path $output $_.name)})
 Run-Step 'compile-move-begin-tests' $compiler ($flags+@('/target:exe','/main:GameMoveBeginValidationProgram',"/out:$tests","/reference:$production")+$references+@($manifest.test_sources|ForEach-Object{Under-Root $_}))
 foreach($mode in @('associated','begin')){
  Run-Step $mode $tests @($mode)
  $summary=Get-Content -LiteralPath (Join-Path $output ($mode+'.log')) -Tail 1|ConvertFrom-Json
  $expected=if($mode -eq 'associated'){$manifest.associated_scenarios}else{$manifest.begin_scenarios}
  if($summary.status -ne 'passed-associated-and-begin-prefix-only' -or $summary.mode -ne $mode -or $summary.scenarios -ne $expected -or $summary.assertions -le 0 -or $summary.old_test_suites_rerun -ne 0 -or $summary.native_math_calls -ne 0){throw ('Unexpected summary: '+$mode)}
  foreach($flag in @('original_read','original_executed','backend_started','system_library_loaded','physical_mouse','walking_accepted','action_submission_completed','gameplay_accepted')){if($summary.$flag -ne $false){throw ('Scope flag changed: '+$flag)}}
  if($mode -eq 'associated'){
   if($summary.new_scenarios -ne 12 -or $summary.affected_regression_scenarios -ne 0 -or $summary.prior_fixture_setup_checks_excluded -ne 0){throw 'Unexpected associated coverage counts'}
   $report.associated_scenarios=$summary.scenarios;$report.associated_assertions=$summary.assertions;$report['associated_result']=$summary
  }else{
   if($summary.new_scenarios -ne 9 -or $summary.affected_regression_scenarios -ne 2 -or $summary.prior_fixture_setup_checks_excluded -ne 28 -or $summary.saved_corridor_count -le 1 -or $summary.next_stop_rva -ne '0x8eaf0'){throw 'Unexpected Begin integration boundary or coverage counts'}
   foreach($flag in @('gate_crossed','associated_miss_returned','distance_returned','skill_returned','already_there_returned')){if($summary.$flag -ne $true){throw ('Missing actual integration prefix: '+$flag)}}
   $report.begin_scenarios=$summary.scenarios;$report.begin_assertions=$summary.assertions;$report.new_integration_scenarios=$summary.new_scenarios;$report.affected_regression_scenarios=$summary.affected_regression_scenarios;$report.prior_fixture_setup_checks_excluded=$summary.prior_fixture_setup_checks_excluded;$report['begin_result']=$summary
  }
 }
 Check-Inputs;foreach($entry in $manifest.assemblies){Check-File (Join-Path $output $entry.name) $entry.sha256}
 Check-File $production $manifest.production_artifact.sha256
 $report.status='passed-associated-and-shared-begin-integration-prefix-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}};$report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;associated_scenarios=$report.associated_scenarios;associated_assertions=$report.associated_assertions;begin_scenarios=$report.begin_scenarios;begin_assertions=$report.begin_assertions;new_integration_scenarios=$report.new_integration_scenarios;affected_regression_scenarios=$report.affected_regression_scenarios;prior_fixture_setup_checks_excluded=$report.prior_fixture_setup_checks_excluded;old_test_suites_rerun=0;action_submission_completed=$false;walking_accepted=$false;result=(Join-Path $output 'result.json')}|ConvertTo-Json
