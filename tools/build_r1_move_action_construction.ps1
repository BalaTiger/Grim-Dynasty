param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$manifestPath=Join-Path $taskRoot 'tools\r1_move_action_construction_build_fingerprints.json'
$manifestHash='c4efe287d97d06b8e20f8b1d475760803cd6cbfa7d031d46941f38282b99e89b'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$output=Join-Path $taskRoot '.tooling\r1-move-action-construction-validation'
function Hash([string]$p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-Ancestors([string]$p){$cursor=[IO.Path]::GetFullPath($p);while($cursor){if(Test-Path -LiteralPath $cursor){if((Get-Item -LiteralPath $cursor).Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Reparse path rejected: '+$cursor)}};$parent=[IO.Directory]::GetParent($cursor);if($null -eq $parent){break};$cursor=$parent.FullName}}
function Under-Root([string]$relative){$p=[IO.Path]::GetFullPath((Join-Path $taskRoot $relative));if(!$p.StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Manifest path leaves workspace'};return $p}
function Check-File([string]$p,[string]$expected){Check-Ancestors $p;if($expected -notmatch '^[0-9a-f]{64}$'){throw ('Unfrozen input: '+$p)};if((Get-Item -LiteralPath $p).PSIsContainer -or (Hash $p) -ne $expected){throw ('Input identity changed: '+$p)}}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace; no arguments'}
Check-File $manifestPath $manifestHash
$manifest=Get-Content -LiteralPath $manifestPath -Raw|ConvertFrom-Json
if($manifest.schema -ne 'grim-dynasty.fixed-move-action-construction-build.v1' -or $manifest.preparation_status -ne 'frozen-not-executed' -or $manifest.compiles -ne 4 -or $manifest.sources.Count -ne 10 -or $manifest.assemblies.Count -ne 7){throw 'Unexpected or unfrozen fixed plan'}
$fixedActions='src/reverse_client_actions/ActionDispatch.cs|src/reverse_client_actions/ActionExecutor.cs|src/reverse_client_actions/MoveActionState.cs|src/reverse_client_actions/MoveBoundaries.cs|src/reverse_client_actions/MoveContracts.cs'
$fixedWorld='src/reverse_client_actions/WorldCoordinateContracts.cs'
$fixedNew='src/reverse_client_actions/GameMoveActionConstruction.cs|src/reverse_client_actions/WorldPositionWordBridge.cs'
$fixedTests='src/reverse_client_actions/GameMoveActionConstructionTests.cs|src/reverse_client_actions/WorldPositionWordBridgeTests.cs'
if(($manifest.actions_sources -join '|') -ne $fixedActions -or ($manifest.world_sources -join '|') -ne $fixedWorld -or ($manifest.new_production_sources -join '|') -ne $fixedNew -or ($manifest.test_sources -join '|') -ne $fixedTests -or ($manifest.sources.path -join '|') -ne ($fixedActions+'|'+$fixedWorld+'|'+$fixedNew+'|'+$fixedTests)){throw 'Unexpected fixed source partition'}
$fixedDependencies='GrimDynasty.MoveManager.dll|GrimDynasty.GameReachedGoal.dll|GrimDynasty.GameMoved.dll|GrimDynasty.EntityMovement.dll|GrimDynasty.GameMovementCallbacks.dll|GrimDynasty.NavigationCrowdState.dll|GrimDynasty.NavigationNearest.dll'
if(($manifest.assemblies.name -join '|') -ne $fixedDependencies){throw 'Unexpected dependency set; old Actions/WorldCoordinates must not shadow the rebuilt libraries'}
if($manifest.constructor_scenarios -ne 11 -or $manifest.bridge_scenarios -ne 12 -or $manifest.affected_regression_scenarios -ne 3 -or $manifest.new_scenarios -ne 23){throw 'Unexpected fixed test counts'}
Check-Ancestors $output
if(Test-Path -LiteralPath $output){throw 'Existing output preserved; no overwrite/retry'}
function Check-Inputs {
 Check-File $compiler $compilerHash;Check-File $manifestPath $manifestHash
 foreach($entry in @($manifest.sources)+@($manifest.evidence)+@($manifest.unchanged_dependency_sources)){Check-File (Under-Root $entry.path) $entry.sha256}
 foreach($entry in $manifest.prerequisites){Check-File (Under-Root $entry.path) $entry.sha256;$prior=Get-Content -LiteralPath (Under-Root $entry.path) -Raw|ConvertFrom-Json;if($prior.status -ne $entry.status -or $null -ne $prior.failure){throw ('Prerequisite is not passed: '+$entry.path)}}
 Check-File (Under-Root $manifest.dependency_receipt.path) $manifest.dependency_receipt.sha256
 $dependencyReceipt=Get-Content -LiteralPath (Under-Root $manifest.dependency_receipt.path) -Raw|ConvertFrom-Json
 if($dependencyReceipt.status -ne 'passed-new-skill-reference-components-only' -or $dependencyReceipt.scenarios -ne 6 -or $dependencyReceipt.assertions -ne 76 -or $null -ne $dependencyReceipt.failure){throw 'Dependency receipt changed'}
 foreach($entry in $manifest.assemblies){Check-File (Under-Root $entry.path) $entry.sha256;$record=@($dependencyReceipt.artifacts|Where-Object{$_.name -eq $entry.name -and $_.sha256 -eq $entry.sha256});if($record.Count -ne 1){throw ('Dependency artifact receipt mismatch: '+$entry.name)}}
 foreach($entry in $manifest.rea_completed){Check-File (Under-Root $entry.path) $entry.sha256;$rea=Get-Content -LiteralPath (Under-Root $entry.path) -Raw|ConvertFrom-Json;if($rea.status -ne 'passed' -or $rea.completed_plan_calls -ne $entry.calls -or !$rea.closed_session_verified -or !$rea.process_exit_confirmed -or !$rea.owned_memory_profile.job_empty_confirmed -or !$rea.owned_memory_profile.sampler_exit_confirmed -or $rea.target.sha256_before -ne $entry.target_sha256 -or $rea.target.sha256_after -ne $entry.target_sha256){throw ('Unclosed or changed saved Rea evidence: '+$entry.path)}}
}
Check-Inputs
$report=[ordered]@{started_utc=[DateTime]::UtcNow.ToString('o');status='running';manifest_sha256=$manifestHash;compiler_sha256=$compilerHash;source_pins=$manifest.sources;assembly_pins=$manifest.assemblies;evidence_pins=$manifest.evidence;steps=@();artifacts=@();failure=$null;compiles=4;new_scenarios=0;new_assertions=0;affected_regression_scenarios=0;affected_regression_assertions=0;old_test_suites_rerun=0;old_executable_entrypoints_invoked=@();old_sources_recompiled=@($manifest.actions_sources)+@($manifest.world_sources);original_read=$false;original_executed=$false;backend_started=$false;physical_mouse=$false;walking_accepted=$false;action_submitted=$false;gameplay_accepted=$false;support_boundary='Only bounded Game082B10/Engine008300 construction and explicit DWORD/QWORD shared knowledge views. New tests exercise existing16+8 and three-word-capture production paths without rerunning old suites. No allocation, submission, physical mouse, real movement or game acceptance.'}
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
 $actions=Join-Path $output 'GrimDynasty.Actions.dll';$world=Join-Path $output 'GrimDynasty.WorldCoordinates.dll';$library=Join-Path $output 'GrimDynasty.MoveActionConstruction.dll';$tests=Join-Path $output 'MoveActionConstructionTests.exe'
 Run-Step 'compile-actions' $compiler ($flags+@('/target:library',"/out:$actions")+@($manifest.actions_sources|ForEach-Object{Under-Root $_}))
 Run-Step 'compile-world-coordinates' $compiler ($flags+@('/target:library',"/out:$world","/reference:$actions",('/reference:'+(Join-Path $output 'GrimDynasty.MoveManager.dll')))+@($manifest.world_sources|ForEach-Object{Under-Root $_}))
 $references=@("/reference:$actions","/reference:$world")+@($manifest.assemblies|ForEach-Object{'/reference:'+(Join-Path $output $_.name)})
 Run-Step 'compile-move-action-construction' $compiler ($flags+@('/target:library',"/out:$library")+$references+@($manifest.new_production_sources|ForEach-Object{Under-Root $_}))
 Run-Step 'compile-move-action-construction-tests' $compiler ($flags+@('/target:exe','/main:GameMoveActionConstructionValidationProgram',"/out:$tests","/reference:$library")+$references+@($manifest.test_sources|ForEach-Object{Under-Root $_}))
 foreach($mode in @('new-components','affected-regression')){
  Run-Step $mode $tests @($mode)
  $summary=Get-Content -LiteralPath (Join-Path $output ($mode+'.log')) -Tail 1|ConvertFrom-Json
  $expected=if($mode -eq 'new-components'){$manifest.new_scenarios}else{$manifest.affected_regression_scenarios}
  if($summary.status -ne ('passed-move-action-construction-'+$mode) -or $summary.scenarios -ne $expected -or $summary.assertions -le 0 -or $summary.old_test_suites_rerun -ne 0 -or $summary.native_math_calls -ne 0){throw ('Unexpected component summary: '+$mode)}
  foreach($flag in @('original_read','original_executed','backend_started','physical_mouse','walking_accepted','action_submitted','system_library_loaded','gameplay_accepted')){if($summary.$flag -ne $false){throw ('Scope flag changed: '+$flag)}}
  if($mode -eq 'new-components'){if($summary.constructor_scenarios -ne 11 -or $summary.bridge_scenarios -ne $manifest.bridge_scenarios -or $summary.affected_regression_scenarios -ne 0){throw 'Unexpected new component counts'};$report.new_scenarios=$summary.scenarios;$report.new_assertions=$summary.assertions;$report['new_test_result']=$summary}
  else{if($summary.constructor_scenarios -ne 0 -or $summary.bridge_scenarios -ne 0 -or $summary.affected_regression_scenarios -ne $manifest.affected_regression_scenarios){throw 'Unexpected affected regression counts'};$report.affected_regression_scenarios=$summary.scenarios;$report.affected_regression_assertions=$summary.assertions;$report['affected_test_result']=$summary}
 }
 Check-Inputs;foreach($entry in $manifest.assemblies){Check-File (Join-Path $output $entry.name) $entry.sha256}
 $report.status='passed-move-action-construction-and-word-bridge-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}};$report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;new_scenarios=$report.new_scenarios;new_assertions=$report.new_assertions;affected_regression_scenarios=$report.affected_regression_scenarios;affected_regression_assertions=$report.affected_regression_assertions;old_test_suites_rerun=0;result=(Join-Path $output 'result.json')}|ConvertTo-Json
