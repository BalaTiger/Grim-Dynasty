param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$manifestPath=Join-Path $taskRoot 'tools\r1_waypoint_production_build_fingerprints.json'
$manifestHash='a66598fbe49072aed764675385ce56408017d031128bae4d523328e62bfb2367'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$output=Join-Path $taskRoot '.tooling\r1-waypoint-production-validation'
function Hash([string]$p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-Ancestors([string]$p){$cursor=[IO.Path]::GetFullPath($p);while($cursor){if(Test-Path -LiteralPath $cursor){if((Get-Item -LiteralPath $cursor).Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Reparse path rejected: '+$cursor)}};$parent=[IO.Directory]::GetParent($cursor);if($null -eq $parent){break};$cursor=$parent.FullName}}
function Under-Root([string]$relative){$p=[IO.Path]::GetFullPath((Join-Path $taskRoot $relative));if(!$p.StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Manifest path leaves workspace'};return $p}
function Check-File([string]$p,[string]$expected){Check-Ancestors $p;if($expected -notmatch '^[0-9a-f]{64}$'){throw ('Unfrozen input: '+$p)};if((Get-Item -LiteralPath $p).PSIsContainer -or (Hash $p) -ne $expected){throw ('Input identity changed: '+$p)}}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace; no arguments'}
Check-File $manifestPath $manifestHash
$manifest=Get-Content -LiteralPath $manifestPath -Raw|ConvertFrom-Json
if($manifest.schema -ne 'grim-dynasty.fixed-waypoint-production-build.v1' -or $manifest.preparation_status -ne 'frozen-not-executed' -or $manifest.compiles -ne 2 -or $manifest.sources.Count -ne 8 -or $manifest.assemblies.Count -ne 47){throw 'Unexpected or unfrozen fixed plan'}
$fixedProduction='src/reverse_client_actions/GameControllerMoveProductionContracts.cs|src/reverse_client_actions/GameAssociatedLookupContracts.cs|src/reverse_client_actions/GameWaypointProductionContracts.cs'
$fixedTests='src/reverse_client_actions/PlayerMoveRequestTests.cs|src/reverse_client_actions/GameControllerMoveProductionTests.cs|src/reverse_client_actions/GameControllerMoveProductionIntegrationTests.cs|src/reverse_client_actions/GameWaypointProductionIntegrationTests.cs|src/reverse_client_actions/GameWaypointValidationProgram.cs'
if(($manifest.production_sources -join '|') -ne $fixedProduction -or ($manifest.test_sources -join '|') -ne $fixedTests -or ($manifest.sources.path -join '|') -ne ($fixedProduction+'|'+$fixedTests)){throw 'Unexpected source partition'}
if($manifest.waypoint_scenarios -ne 8 -or $manifest.prior_fixture_setup_checks_excluded -ne 20 -or $manifest.old_suites_to_run.Count -ne 0){throw 'Unexpected fixed test scope'}
if(@($manifest.assemblies.name|Group-Object|Where-Object{$_.Count -ne 1}).Count -ne 0){throw 'Duplicate assembly name'}
foreach($entry in $manifest.assemblies){if($entry.name -ne [IO.Path]::GetFileName($entry.name) -or $entry.name -eq 'GrimDynasty.ControllerMoveProduction.dll' -or (!$entry.name.EndsWith('.dll') -and $entry.name -notin @('NavigationSearchWrapperTests.exe','NativeAttributeGetterChecks.exe'))){throw 'Unexpected or shadowing managed dependency'}}
Check-Ancestors $output
if(Test-Path -LiteralPath $output){throw 'Existing output preserved; no overwrite/retry'}
function Check-Inputs {
 Check-File $compiler $compilerHash;Check-File $manifestPath $manifestHash
 foreach($entry in @($manifest.sources)+@($manifest.evidence)+@($manifest.fixture_resources)+@($manifest.unchanged_dependency_sources)+@($manifest.prior_artifacts_preserved)){Check-File (Under-Root $entry.path) $entry.sha256}
 $receipts=@{}
 foreach($entry in $manifest.prerequisites){Check-File (Under-Root $entry.path) $entry.sha256;$prior=Get-Content -LiteralPath (Under-Root $entry.path) -Raw|ConvertFrom-Json;if($prior.status -ne $entry.status -or $null -ne $prior.failure){throw ('Prerequisite is not passed: '+$entry.path)};$receipts[$entry.path]=$prior}
 foreach($entry in $manifest.assemblies){Check-File (Under-Root $entry.path) $entry.sha256;if(!$receipts.ContainsKey($entry.receipt)){throw 'Missing dependency receipt'};$record=@($receipts[$entry.receipt].artifacts|Where-Object{($_.name -eq $entry.name -or [IO.Path]::GetFileName($_.path) -eq $entry.name) -and $_.sha256 -eq $entry.sha256});if($record.Count -ne 1){throw ('Dependency artifact receipt mismatch: '+$entry.name)}}
 foreach($entry in $manifest.rea_completed){Check-File (Under-Root $entry.path) $entry.sha256;$rea=Get-Content -LiteralPath (Under-Root $entry.path) -Raw|ConvertFrom-Json;if($rea.status -ne 'passed' -or $rea.completed_plan_calls -ne $entry.calls -or !$rea.closed_session_verified -or !$rea.process_exit_confirmed -or !$rea.owned_memory_profile.job_empty_confirmed -or !$rea.owned_memory_profile.sampler_exit_confirmed -or $rea.target.sha256_before -ne $entry.target_sha256 -or $rea.target.sha256_after -ne $entry.target_sha256){throw ('Unclosed or changed saved Rea evidence: '+$entry.path)}}
}
Check-Inputs
$report=[ordered]@{started_utc=[DateTime]::UtcNow.ToString('o');status='running';manifest_sha256=$manifestHash;compiler_sha256=$compilerHash;source_pins=$manifest.sources;assembly_pins=$manifest.assemblies;evidence_pins=$manifest.evidence;dependency_selections=$manifest.dependency_selections;steps=@();artifacts=@();failure=$null;compiles=2;waypoint_scenarios=0;waypoint_assertions=0;prior_fixture_setup_checks_excluded=0;assertion_count_scope='Current new Waypoint suite only; old fixture setup checks separately reported and excluded.';old_test_suites_rerun=0;old_executable_entrypoints_invoked=@();old_sources_recompiled_for_fixture_only=@($manifest.test_sources[0..2]);original_read=$false;original_executed=$false;backend_started=$false;system_library_loaded=$false;native_math_calls=0;physical_mouse=$false;walking_accepted=$false;action_submission_completed=$false;gameplay_accepted=$false;support_boundary=$manifest.boundary}
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
 $production=Join-Path $output 'GrimDynasty.ControllerMoveProduction.dll';$tests=Join-Path $output 'GameWaypointTests.exe'
 $references=@($manifest.assemblies|ForEach-Object{'/reference:'+(Join-Path $output $_.name)})
 Run-Step 'compile-controller-waypoint-production' $compiler ($flags+@('/target:library',"/out:$production")+$references+@($manifest.production_sources|ForEach-Object{Under-Root $_}))
 Run-Step 'compile-waypoint-tests' $compiler ($flags+@('/target:exe','/main:GameWaypointValidationProgram',"/out:$tests","/reference:$production")+$references+@($manifest.test_sources|ForEach-Object{Under-Root $_}))
 Run-Step 'waypoint' $tests @()
 $summary=Get-Content -LiteralPath (Join-Path $output 'waypoint.log') -Tail 1|ConvertFrom-Json
 if($summary.status -ne 'passed-shared-waypoint-agent-target-prefix-only' -or $summary.scenarios -ne $manifest.waypoint_scenarios -or $summary.assertions -le 0 -or $summary.prior_fixture_setup_checks_excluded -ne 20 -or $summary.old_test_suites_rerun -ne 0 -or $summary.native_math_calls -ne 0 -or $summary.next_stop_rva -ne '0x5ae40'){throw 'Unexpected Waypoint summary'}
 foreach($flag in @('original_read','original_executed','backend_started','system_library_loaded','physical_mouse','walking_accepted','action_submission_completed','gameplay_accepted')){if($summary.$flag -ne $false){throw ('Scope flag changed: '+$flag)}}
 foreach($flag in @('actual_target_submitted','repeat_preserved_identity','ignored_floor_zero','post_submission_prefix','begin_waypoint_connected')){if($summary.$flag -ne $true){throw ('Missing actual Waypoint prefix: '+$flag)}}
 $report.waypoint_scenarios=$summary.scenarios;$report.waypoint_assertions=$summary.assertions;$report.prior_fixture_setup_checks_excluded=$summary.prior_fixture_setup_checks_excluded;$report['test_result']=$summary
 Check-Inputs;foreach($entry in $manifest.assemblies){Check-File (Join-Path $output $entry.name) $entry.sha256}
 $report.status='passed-shared-waypoint-agent-target-production-prefix-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}};$report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;waypoint_scenarios=$report.waypoint_scenarios;waypoint_assertions=$report.waypoint_assertions;prior_fixture_setup_checks_excluded=$report.prior_fixture_setup_checks_excluded;old_test_suites_rerun=0;action_submission_completed=$false;walking_accepted=$false;result=(Join-Path $output 'result.json')}|ConvertTo-Json
