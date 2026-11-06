param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$manifestPath=Join-Path $taskRoot 'tools\r1_player_run_speed_cap_build_fingerprints.json'
$manifestHash='162fffec1fdbf0565903580060b0af70192695b40365638d59d15be872820504'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$jsonAssembly='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Web.Extensions.dll'
$jsonAssemblyHash='5e6378edc06f5a61920aaadb2aba839dd3cf302d6192e08aacd4454ba4cff0b3'
$output=Join-Path $taskRoot '.tooling\r1-player-run-speed-cap-validation'
function Hash([string]$p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-Ancestors([string]$p){$cursor=[IO.Path]::GetFullPath($p);while($cursor){if(Test-Path -LiteralPath $cursor){if((Get-Item -LiteralPath $cursor).Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Reparse path rejected: '+$cursor)}};$parent=[IO.Directory]::GetParent($cursor);if($null -eq $parent){break};$cursor=$parent.FullName}}
function Under-Root([string]$relative){$p=[IO.Path]::GetFullPath((Join-Path $taskRoot $relative));if(!$p.StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Manifest path leaves workspace'};return $p}
function Check-File([string]$p,[string]$expected){Check-Ancestors $p;if($expected -notmatch '^[0-9a-f]{64}$'){throw ('Unfrozen input: '+$p)};if((Get-Item -LiteralPath $p).PSIsContainer -or (Hash $p) -ne $expected){throw ('Input identity changed: '+$p)}}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace; no arguments'}
Check-File $manifestPath $manifestHash
$manifest=Get-Content -LiteralPath $manifestPath -Raw|ConvertFrom-Json
if($manifest.schema -ne 'grim-dynasty.fixed-player-run-speed-cap-build.v1' -or $manifest.compiles -ne 2 -or $manifest.sources.Count -ne 2 -or $manifest.assemblies.Count -ne 36 -or $manifest.new_scenarios -ne 12){throw 'Unexpected fixed plan'}
if(($manifest.sources.path -join '|') -ne 'src/reverse_client_actions/GamePlayerRunSpeedCap.cs|src/reverse_client_actions/GamePlayerRunSpeedCapTests.cs'){throw 'Unexpected source partition'}
foreach($entry in $manifest.assemblies){if($entry.name -ne [IO.Path]::GetFileName($entry.name) -or (!$entry.name.EndsWith('.dll') -and $entry.name -ne 'NavigationSearchWrapperTests.exe' -and $entry.name -ne 'NativeAttributeGetterChecks.exe')){throw 'Unexpected dependency filename'}}
Check-Ancestors $output
if(Test-Path -LiteralPath $output){throw 'Existing output preserved; no overwrite/retry'}
function Check-Inputs {
 Check-File $compiler $compilerHash;Check-File $jsonAssembly $jsonAssemblyHash;Check-File $manifestPath $manifestHash
 foreach($entry in @($manifest.sources)+@($manifest.evidence)+@($manifest.attribute_source_pins)){Check-File (Under-Root $entry.path) $entry.sha256}
 Check-File (Under-Root $manifest.state_result.path) $manifest.state_result.sha256
 $prior=Get-Content -LiteralPath (Under-Root $manifest.state_result.path) -Raw|ConvertFrom-Json
 if($prior.status -ne 'passed-new-controller-state-request-integration-plus-affected-regression' -or $null -ne $prior.failure){throw 'State/request dependency receipt is not passed'}
 Check-File (Under-Root $manifest.attribute_manifest.path) $manifest.attribute_manifest.sha256
 Check-File (Under-Root $manifest.attribute_acceptance.path) $manifest.attribute_acceptance.sha256
 $attribute=Get-Content -LiteralPath (Under-Root $manifest.attribute_manifest.path) -Raw|ConvertFrom-Json
 $accepted=Get-Content -LiteralPath (Under-Root $manifest.attribute_acceptance.path) -Raw|ConvertFrom-Json
 if($attribute.status -ne 'Passed' -or $attribute.component -ne 'attribute' -or $accepted.status -ne 'Passed' -or $accepted.component -ne 'attribute' -or $accepted.assertions -ne 67 -or $accepted.groups -ne 6){throw 'Attribute dependency receipt is not passed'}
 foreach($entry in $manifest.attribute_source_pins){$record=@($attribute.sources|Where-Object { $_.path -eq $entry.path -and $_.sha256 -eq $entry.sha256 });if($record.Count -ne 1){throw 'Attribute source identity mismatch'}}
 foreach($entry in $manifest.assemblies){
  Check-File (Under-Root $entry.path) $entry.sha256
  if($entry.name -eq 'NativeAttributeGetterChecks.exe'){if($entry.sha256 -ne $attribute.exe_sha256){throw 'Attribute assembly receipt mismatch'}}
  else{$record=@($prior.artifacts|Where-Object { $_.name -eq $entry.name -and $_.sha256 -eq $entry.sha256 });if($record.Count -ne 1){throw ('Dependency receipt mismatch: '+$entry.name)}}
 }
 Check-File (Under-Root $manifest.rea_completed.path) $manifest.rea_completed.sha256
 $rea=Get-Content -LiteralPath (Under-Root $manifest.rea_completed.path) -Raw|ConvertFrom-Json
 if($rea.status -ne 'passed' -or $rea.completed_plan_calls -ne 14 -or !$rea.closed_session_verified -or !$rea.process_exit_confirmed -or !$rea.owned_memory_profile.job_empty_confirmed -or !$rea.owned_memory_profile.sampler_exit_confirmed -or $rea.target.sha256_before -ne $manifest.original_game_sha256 -or $rea.target.sha256_after -ne $manifest.original_game_sha256){throw 'Unclosed or changed Rea evidence'}
}
Check-Inputs
$report=[ordered]@{started_utc=[DateTime]::UtcNow.ToString('o');status='running';manifest_sha256=$manifestHash;compiler_sha256=$compilerHash;source_pins=$manifest.sources;assembly_pins=$manifest.assemblies;evidence_pins=$manifest.evidence;steps=@();artifacts=@();failure=$null;compiles=2;new_scenarios=0;new_assertions=0;old_test_suites_rerun=0;old_executable_entrypoints_invoked=@();old_sources_recompiled=@();original_read=$false;original_executed=$false;backend_started=$false;physical_mouse=$false;walking_accepted=$false;action_submitted=$false;gameplay_accepted=$false;support_boundary='Only finite conditional Player3E5A10 speed-cap semantics on explicit same-owner mutable fields and existing attribute getter. No complete069040, DurationDamageManager, database cap path, action submission, native initialization or game acceptance.'}
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
 $references=@('/reference:'+$jsonAssembly);foreach($entry in $manifest.assemblies){$references += '/reference:'+(Join-Path $output $entry.name)}
 $library=Join-Path $output 'GrimDynasty.PlayerRunSpeedCap.dll';$tests=Join-Path $output 'GamePlayerRunSpeedCapTests.exe'
 Run-Step 'compile-player-run-speed-cap' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@(Under-Root $manifest.sources[0].path))
 Run-Step 'compile-player-run-speed-cap-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:GamePlayerRunSpeedCapValidationProgram',"/out:$tests","/reference:$library")+$references+@(Under-Root $manifest.sources[1].path))
 Run-Step 'new-components' $tests @('new-components')
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-components.log') -Tail 1|ConvertFrom-Json
 if($summary.status -ne 'passed-new-player-speed-cap-components-only' -or $summary.scenarios -ne $manifest.new_scenarios -or $summary.assertions -le 0 -or $summary.old_test_suites_rerun -ne 0 -or $summary.native_math_calls -ne 0){throw 'Unexpected component summary'}
 foreach($flag in @('original_read','original_executed','backend_started','physical_mouse','walking_accepted','action_submitted','system_library_loaded')){if($summary.$flag -ne $false){throw ('Scope flag changed: '+$flag)}}
 $report.new_scenarios=$summary.scenarios;$report.new_assertions=$summary.assertions;$report['test_result']=$summary
 Check-Inputs;foreach($entry in $manifest.assemblies){Check-File (Join-Path $output $entry.name) $entry.sha256}
 $report.status='passed-new-player-run-speed-cap-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}};$report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;new_scenarios=$report.new_scenarios;new_assertions=$report.new_assertions;old_test_suites_rerun=0;result=(Join-Path $output 'result.json')}|ConvertTo-Json
