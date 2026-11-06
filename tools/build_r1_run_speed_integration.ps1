param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$manifestPath=Join-Path $taskRoot 'tools\r1_run_speed_integration_build_fingerprints.json'
$manifestHash='16442dfdcdfcd47e7e83d1261174b227eb94e1e5efbdbdd220153a28a96c65c0'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$jsonAssembly='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Web.Extensions.dll'
$jsonAssemblyHash='5e6378edc06f5a61920aaadb2aba839dd3cf302d6192e08aacd4454ba4cff0b3'
$output=Join-Path $taskRoot '.tooling\r1-run-speed-integration-validation'
function Hash([string]$p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-Ancestors([string]$p){$cursor=[IO.Path]::GetFullPath($p);while($cursor){if(Test-Path -LiteralPath $cursor){if((Get-Item -LiteralPath $cursor).Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Reparse path rejected: '+$cursor)}};$parent=[IO.Directory]::GetParent($cursor);if($null -eq $parent){break};$cursor=$parent.FullName}}
function Under-Root([string]$relative){$p=[IO.Path]::GetFullPath((Join-Path $taskRoot $relative));if(!$p.StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Manifest path leaves workspace'};return $p}
function Check-File([string]$p,[string]$expected){Check-Ancestors $p;if($expected -notmatch '^[0-9a-f]{64}$'){throw ('Unfrozen input: '+$p)};if((Get-Item -LiteralPath $p).PSIsContainer -or (Hash $p) -ne $expected){throw ('Input identity changed: '+$p)}}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace; no arguments'}
Check-File $manifestPath $manifestHash
$manifest=Get-Content -LiteralPath $manifestPath -Raw|ConvertFrom-Json
if($manifest.schema -ne 'grim-dynasty.fixed-run-speed-integration-build.v1' -or $manifest.compiles -ne 2 -or $manifest.sources.Count -ne 12 -or $manifest.assemblies.Count -ne 37 -or $manifest.production_sources.Count -ne 6 -or $manifest.test_sources.Count -ne 6 -or $manifest.regression_scenarios -ne 7){throw 'Unexpected fixed plan'}
foreach($entry in $manifest.assemblies){if($entry.name -ne [IO.Path]::GetFileName($entry.name) -or (!$entry.name.EndsWith('.dll') -and $entry.name -ne 'NavigationSearchWrapperTests.exe' -and $entry.name -ne 'NativeAttributeGetterChecks.exe') -or $entry.name -eq 'GrimDynasty.PlayerMoveRequest.dll'){throw 'Unexpected dependency or shadowing old Request library'}}
Check-Ancestors $output
if(Test-Path -LiteralPath $output){throw 'Existing output preserved; no overwrite/retry'}
function Check-Inputs {
 Check-File $compiler $compilerHash;Check-File $jsonAssembly $jsonAssemblyHash;Check-File $manifestPath $manifestHash
 foreach($entry in @($manifest.sources)+@($manifest.evidence)){Check-File (Under-Root $entry.path) $entry.sha256}
 foreach($entry in $manifest.prerequisites){Check-File (Under-Root $entry.path) $entry.sha256;$prior=Get-Content -LiteralPath (Under-Root $entry.path) -Raw|ConvertFrom-Json;if($prior.status -ne $entry.status -or $null -ne $prior.failure){throw ('Prior validation no longer passed: '+$entry.path)}}
 $cap=Get-Content -LiteralPath (Under-Root $manifest.cap_result) -Raw|ConvertFrom-Json
 if($cap.status -ne 'passed-new-player-run-speed-cap-only' -or $cap.new_scenarios -ne 12 -or $cap.new_assertions -ne 72){throw 'Unexpected cap dependency receipt'}
 foreach($entry in $manifest.assemblies){Check-File (Under-Root $entry.path) $entry.sha256;$record=@($cap.artifacts|Where-Object { $_.name -eq $entry.name -and $_.sha256 -eq $entry.sha256 });if($record.Count -ne 1){throw ('Dependency artifact receipt mismatch: '+$entry.name)}}
 foreach($entry in $manifest.rea_completed){Check-File (Under-Root $entry.path) $entry.sha256;$rea=Get-Content -LiteralPath (Under-Root $entry.path) -Raw|ConvertFrom-Json;if($rea.status -ne 'passed' -or $rea.completed_plan_calls -ne $entry.calls -or !$rea.closed_session_verified -or !$rea.process_exit_confirmed -or !$rea.owned_memory_profile.job_empty_confirmed -or !$rea.owned_memory_profile.sampler_exit_confirmed -or $rea.target.sha256_before -ne $manifest.original_game_sha256 -or $rea.target.sha256_after -ne $manifest.original_game_sha256){throw ('Unclosed or changed Rea evidence: '+$entry.path)}}
}
Check-Inputs
$report=[ordered]@{started_utc=[DateTime]::UtcNow.ToString('o');status='running';manifest_sha256=$manifestHash;compiler_sha256=$compilerHash;source_pins=$manifest.sources;assembly_pins=$manifest.assemblies;evidence_pins=$manifest.evidence;steps=@();test_results=@();artifacts=@();failure=$null;compiles=2;new_scenarios=0;new_assertions=0;regression_scenarios=0;regression_assertions=0;old_test_suites_rerun=@('Only7 affected controller-state/request integration scenarios');old_executable_entrypoints_invoked=@();old_sources_recompiled=@('PlayerMoveRequestContracts.cs','ControllerStateTransitionState.cs','ControllerStateTransitionContracts.cs');old_test_sources_recompiled=@('PlayerMoveRequestTests.cs for public fixture only; Run not invoked','ControllerStateTransitionTests.cs','ControllerStateRequestIntegrationTests.cs');original_read=$false;original_executed=$false;backend_started=$false;physical_mouse=$false;walking_accepted=$false;action_submitted=$false;gameplay_accepted=$false;support_boundary='Conditional ordinary Player069040 composition through real DurationDamageManager and existing attribute/cap contracts. Explicit OWN hot fields and saved resource fixtures only. Accepted state-entry path must stop at actual unimplemented124340; no action submission, physical input, native cold initialization or gameplay acceptance.'}
New-Item -ItemType Directory -Path $output|Out-Null
function Run-Step([string]$name,[string]$program,[string[]]$arguments){
 $stdout=Join-Path $output ($name+'.log');$stderr=Join-Path $output ($name+'.stderr.log');$savedPreference=$ErrorActionPreference
 try{$ErrorActionPreference='Continue';& $program @arguments 1> $stdout 2> $stderr;$ok=$?;$code=$LASTEXITCODE}finally{$ErrorActionPreference=$savedPreference}
 $report.steps += [ordered]@{name=$name;program=$program;arguments=$arguments;invocation_succeeded=$ok;exit_code=$code}
 if(!$ok -or $code -ne 0){throw "Step $name failed (invocation=$ok, exit=$code); evidence retained"}
 if((Get-Item -LiteralPath $stderr).Length -ne 0 -or ($name.StartsWith('compile-') -and (Get-Item -LiteralPath $stdout).Length -ne 0)){throw ('Unexpected diagnostics: '+$name)}
}
function Check-Scope($value){foreach($flag in @('original_read','original_executed','backend_started','physical_mouse','walking_accepted','action_submitted','system_library_loaded')){if($value.$flag -ne $false){throw ('Scope flag changed: '+$flag)}};if($value.native_math_calls -ne 0){throw 'Unplanned native math execution'}}
try{
 foreach($entry in $manifest.assemblies){Copy-Item -LiteralPath (Under-Root $entry.path) -Destination (Join-Path $output $entry.name);Check-File (Join-Path $output $entry.name) $entry.sha256}
 $references=@('/reference:'+$jsonAssembly);foreach($entry in $manifest.assemblies){$references += '/reference:'+(Join-Path $output $entry.name)}
 $library=Join-Path $output 'GrimDynasty.PlayerMoveRequest.dll';$tests=Join-Path $output 'GameRunSpeedIntegratedTests.exe'
 $production=@($manifest.production_sources|ForEach-Object{Under-Root $_});$testing=@($manifest.test_sources|ForEach-Object{Under-Root $_})
 Run-Step 'compile-run-speed-integration' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+$production)
 Run-Step 'compile-run-speed-integration-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:GameRunSpeedValidationProgram',"/out:$tests","/reference:$library")+$references+$testing)
 Run-Step 'new-components' $tests @('new-components')
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-components.log') -Tail 1|ConvertFrom-Json
 if($summary.status -ne 'passed-new-run-speed-components-and-integration-only' -or $summary.scenarios -ne $manifest.new_scenarios -or $summary.duration_scenarios -ne $manifest.duration_scenarios -or $summary.run_speed_scenarios -ne $manifest.run_speed_scenarios -or $summary.integration_scenarios -ne $manifest.integration_scenarios -or $summary.assertions -le 0 -or $summary.old_test_suites_rerun -ne 0){throw 'Unexpected new component summary'}
 Check-Scope $summary
 if($summary.stop_rva -ne 0x124340 -or $summary.run_speed_returned -ne $true -or $summary.nav_start_result -ne 0 -or $summary.corridor_count -le 1){throw 'Bound state entry did not reach actual action-construction boundary through the saved route'}
 $report.test_results += $summary;$report.new_scenarios=$summary.scenarios;$report.new_assertions=$summary.assertions
 Run-Step 'affected-state-regression' $tests @('state-regression')
 $regression=Get-Content -LiteralPath (Join-Path $output 'affected-state-regression.log') -Tail 1|ConvertFrom-Json
 if($regression.status -ne 'passed-affected-controller-state-regression-only' -or $regression.scenarios -ne 7 -or $regression.assertions -le 0 -or $regression.stop_rva -ne 0x069040 -or $regression.old_test_suites_rerun -ne 1){throw 'Unexpected unbound-state regression summary'}
 Check-Scope $regression
 $report.test_results += $regression;$report.regression_scenarios=$regression.scenarios;$report.regression_assertions=$regression.assertions
 Check-Inputs;foreach($entry in $manifest.assemblies){Check-File (Join-Path $output $entry.name) $entry.sha256}
 $report.status='passed-new-run-speed-integration-plus-affected-state-regression'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}};$report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 14|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;new_scenarios=$report.new_scenarios;new_assertions=$report.new_assertions;regression_scenarios=$report.regression_scenarios;regression_assertions=$report.regression_assertions;result=(Join-Path $output 'result.json')}|ConvertTo-Json
