param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$manifestPath=Join-Path $taskRoot 'tools\r1_controller_state_request_build_fingerprints.json'
$manifestHash='313ac3ed291d60e41b86ef9fdbc6c5e2aa1d275e3e69073d4d49a251c4527850'
$jsonAssembly='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Web.Extensions.dll'
$jsonAssemblyHash='5e6378edc06f5a61920aaadb2aba839dd3cf302d6192e08aacd4454ba4cff0b3'
$output=Join-Path $taskRoot '.tooling\r1-controller-state-request-validation'
function Hash([string]$p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-Ancestors([string]$p){$cursor=[IO.Path]::GetFullPath($p);while($cursor){if(Test-Path -LiteralPath $cursor){$item=Get-Item -LiteralPath $cursor;if($item.Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Reparse path not permitted: '+$cursor)}};$parent=[IO.Directory]::GetParent($cursor);if($null -eq $parent){break};$cursor=$parent.FullName}}
function Check-File([string]$p,[string]$expected){Check-Ancestors $p;if($expected -notmatch '^[0-9a-f]{64}$'){throw ('Unfrozen input: '+$p)};$item=Get-Item -LiteralPath $p;if($item.PSIsContainer -or (Hash $p) -ne $expected){throw ('Input identity changed: '+$p)}}
function Under-Root([string]$relative){$p=[IO.Path]::GetFullPath((Join-Path $taskRoot $relative));if(!$p.StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Manifest path leaves workspace'};return $p}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace; no arguments'}
Check-File $manifestPath $manifestHash
$manifest=Get-Content -LiteralPath $manifestPath -Raw|ConvertFrom-Json
if($manifest.schema -ne 'grim-dynasty.fixed-controller-state-request-build.v1' -or $manifest.compiles -ne 2 -or $manifest.sources.Count -ne 6 -or $manifest.assemblies.Count -ne 35 -or $manifest.new_component_scenarios -ne 6 -or $manifest.new_integration_scenarios -ne 1 -or $manifest.affected_request_regression_scenarios -ne 13){throw 'Unexpected fixed plan'}
foreach($entry in $manifest.assemblies){if($entry.name -ne [IO.Path]::GetFileName($entry.name) -or (!$entry.name.EndsWith('.dll') -and $entry.name -ne 'NavigationSearchWrapperTests.exe') -or $entry.name -eq 'GrimDynasty.PlayerMoveRequest.dll'){throw 'Only fixed flat dependency filenames; old Request library must not shadow replacement'}}
Check-Ancestors $output
if(!([IO.Path]::GetFullPath($output)).StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Output outside workspace'}
if(Test-Path -LiteralPath $output){throw 'Existing output preserved; no overwrite/retry'}
function Check-Inputs {
 Check-File $compiler $compilerHash;Check-File $jsonAssembly $jsonAssemblyHash;Check-File $manifestPath $manifestHash
 foreach($entry in $manifest.sources){Check-File (Under-Root $entry.path) $entry.sha256}
 foreach($entry in $manifest.evidence){Check-File (Under-Root $entry.path) $entry.sha256}
 foreach($entry in $manifest.prerequisites){Check-File (Under-Root $entry.path) $entry.sha256;$prior=Get-Content -LiteralPath (Under-Root $entry.path) -Raw|ConvertFrom-Json;if($prior.status -ne $entry.status -or $null -ne $prior.failure){throw ('Prerequisite no longer passed: '+$entry.path)}}
 foreach($entry in $manifest.assemblies){Check-File (Under-Root $entry.path) $entry.sha256;if(@($manifest.prerequisites|Where-Object path -EQ $entry.result).Count -ne 1){throw ('Unpinned artifact receipt: '+$entry.name)};$prior=Get-Content -LiteralPath (Under-Root $entry.result) -Raw|ConvertFrom-Json;$record=@($prior.artifacts|Where-Object {($_.name -eq $entry.name -or ($_.path -and [IO.Path]::GetFileName($_.path) -eq $entry.name)) -and $_.sha256 -eq $entry.sha256});if($record.Count -ne 1){throw ('Assembly lacks exact artifact receipt: '+$entry.name)}}
 foreach($entry in $manifest.rea_completed){Check-File (Under-Root $entry.path) $entry.sha256;$prior=Get-Content -LiteralPath (Under-Root $entry.path) -Raw|ConvertFrom-Json;if($prior.status -ne 'passed' -or $prior.completed_plan_calls -ne $entry.calls -or $prior.closed_session_verified -ne $true -or $prior.process_exit_confirmed -ne $true -or $prior.owned_memory_profile.job_empty_confirmed -ne $true -or $prior.owned_memory_profile.sampler_exit_confirmed -ne $true){throw ('Unclosed Rea evidence: '+$entry.path)}}
}
Check-Inputs
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash
 manifest=$manifestPath;manifest_sha256=$manifestHash;source_pins=$manifest.sources;prerequisites=$manifest.prerequisites;assembly_pins=$manifest.assemblies;evidence_pins=$manifest.evidence
 steps=@();test_results=@();artifacts=@();failure=$null;compiles=2;new_scenarios=0;new_assertions=0;regression_scenarios=0;regression_assertions=0
 old_test_suites_rerun=@('Only13 affected PlayerMoveRequest scenarios');old_executable_entrypoints_invoked=@();old_source_recompiled=@('PlayerMoveRequestContracts.cs for concrete same-library state transition')
 original_read=$false;original_executed=$false;backend_started=$false;security_settings_changed=$false
 system_library_loaded=$false;native_math_calls=0;full_tick=$false;physical_mouse=$false;walking_accepted=$false;action_submitted=$false;gameplay_accepted=$false;mouse_input_chain_complete=$false;active_move_end_completed=$false
 support_boundary='Actual SSO tree lookup/compare and123D50 state data/canonical-state publication composed with original1636A8 captured request owner and166020 entry prefix. Accepted saved-map OWN nav-center start reaches exact unimplemented069040; real Ground start result4 remains. No fabricated speed/action, native hot/cold initialization, physical input, renderer or playable client acceptance.'
}
New-Item -ItemType Directory -Path $output|Out-Null
function Run-Step([string]$name,[string]$program,[string[]]$arguments){
 $stdout=Join-Path $output ($name+'.log');$stderr=Join-Path $output ($name+'.stderr.log');$savedPreference=$ErrorActionPreference
 try{$ErrorActionPreference='Continue';& $program @arguments 1> $stdout 2> $stderr;$ok=$?;$code=$LASTEXITCODE}finally{$ErrorActionPreference=$savedPreference}
 $report.steps += [ordered]@{name=$name;program=$program;arguments=$arguments;invocation_succeeded=$ok;exit_code=$code}
 if(!$ok -or $code -ne 0){throw "Step $name failed (invocation=$ok, exit=$code); evidence retained"}
 if((Get-Item -LiteralPath $stderr).Length -ne 0 -or ($name.StartsWith('compile-') -and (Get-Item -LiteralPath $stdout).Length -ne 0)){throw ('Unexpected diagnostics: '+$name)}
}
function Check-Scope($value){foreach($flag in @('physical_mouse','walking_accepted','action_submitted','system_library_loaded','original_read','original_executed','backend_started')){if($value.$flag -ne $false){throw ('Scope flag changed: '+$flag)}};if($value.native_math_calls -ne 0){throw 'Unplanned native math execution'}}
try{
 foreach($entry in $manifest.assemblies){Copy-Item -LiteralPath (Under-Root $entry.path) -Destination (Join-Path $output $entry.name);Check-File (Join-Path $output $entry.name) $entry.sha256}
 $references=@('/reference:'+$jsonAssembly);foreach($entry in $manifest.assemblies){$references += '/reference:'+(Join-Path $output $entry.name)}
 $library=Join-Path $output 'GrimDynasty.PlayerMoveRequest.dll';$tests=Join-Path $output 'ControllerStateIntegratedTests.exe'
 $production=@($manifest.production_sources|ForEach-Object{Under-Root $_});$testing=@($manifest.test_sources|ForEach-Object{Under-Root $_})
 if($production.Count -ne 3 -or $testing.Count -ne 3){throw 'Unexpected exact source partition'}
 Run-Step 'compile-controller-state-request' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+$production)
 Run-Step 'compile-controller-state-request-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:ControllerStateIntegratedValidationProgram',"/out:$tests","/reference:$library")+$references+$testing)
 Run-Step 'new-components' $tests @('new-components')
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-components.log') -Tail 1|ConvertFrom-Json
 if($summary.status -ne 'passed-new-controller-state-request-integration-only' -or $summary.component_scenarios -ne 6 -or $summary.integration_scenarios -ne 1 -or $summary.scenarios -ne 7 -or $summary.assertions -le 0 -or $summary.old_test_suites_rerun -ne 0){throw 'Unexpected new component summary'}
 Check-Scope $summary
 if($summary.nav_start_result -ne 0 -or $summary.ground_start_result -ne 4 -or $summary.corridor_count -le 1 -or $summary.state_published -ne $true -or $summary.direction_written -ne $true -or $summary.stop_rva -ne 0x069040 -or $summary.request_continuation_complete -ne $false){throw 'Unexpected actual request/state entry boundary'}
 $report.test_results += $summary;$report.new_scenarios=$summary.scenarios;$report.new_assertions=$summary.assertions
 Run-Step 'affected-request-regression' $tests @('request-regression')
 $regression=Get-Content -LiteralPath (Join-Path $output 'affected-request-regression.log') -Tail 1|ConvertFrom-Json
 if($regression.status -ne 'passed-affected-request-regression-only' -or $regression.scenarios -ne 13 -or $regression.assertions -le 0 -or $regression.old_test_suites_rerun -ne 1){throw 'Unexpected affected regression summary'}
 Check-Scope $regression
 if($regression.nav_start_result -ne 0 -or $regression.ground_start_result -ne 4 -or $regression.corridor_count -le 1 -or $regression.transition_caller -ne 0x1636ac){throw 'Existing unbound request boundary changed'}
 $report.test_results += $regression;$report.regression_scenarios=$regression.scenarios;$report.regression_assertions=$regression.assertions
 Check-Inputs;foreach($entry in $manifest.assemblies){Check-File (Join-Path $output $entry.name) $entry.sha256}
 $report.status='passed-new-controller-state-request-integration-plus-affected-regression'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}};$report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 16|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;new_scenarios=$report.new_scenarios;new_assertions=$report.new_assertions;regression_scenarios=$report.regression_scenarios;regression_assertions=$report.regression_assertions;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 6
