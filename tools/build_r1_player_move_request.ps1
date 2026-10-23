param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$manifestPath=Join-Path $taskRoot 'tools\r1_player_move_request_build_fingerprints.json'
$manifestHash='ec3665e6d7f37a2ec1607414cbe23d568c5f85d5a97bd2390ae03cea4b5610d2'
$jsonAssembly='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Web.Extensions.dll'
$jsonAssemblyHash='5e6378edc06f5a61920aaadb2aba839dd3cf302d6192e08aacd4454ba4cff0b3'
$output=Join-Path $taskRoot '.tooling\r1-player-move-request-validation'
function Hash([string]$p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-Ancestors([string]$p){$cursor=[IO.Path]::GetFullPath($p);while($cursor){if(Test-Path -LiteralPath $cursor){$item=Get-Item -LiteralPath $cursor;if($item.Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Reparse path not permitted: '+$cursor)}};$parent=[IO.Directory]::GetParent($cursor);if($null -eq $parent){break};$cursor=$parent.FullName}}
function Check-File([string]$p,[string]$expected){Check-Ancestors $p;if($expected -notmatch '^[0-9a-f]{64}$'){throw ('Unfrozen input: '+$p)};$item=Get-Item -LiteralPath $p;if($item.PSIsContainer -or (Hash $p) -ne $expected){throw ('Input identity changed: '+$p)}}
function Under-Root([string]$relative){$p=[IO.Path]::GetFullPath((Join-Path $taskRoot $relative));if(!$p.StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Manifest path leaves workspace'};return $p}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace; no arguments'}
Check-File $manifestPath $manifestHash
$manifest=Get-Content -LiteralPath $manifestPath -Raw|ConvertFrom-Json
if($manifest.schema -ne 'grim-dynasty.fixed-player-move-request-build.v1' -or $manifest.compiles -ne 2 -or $manifest.sources.Count -ne 2 -or $manifest.assemblies.Count -ne 35 -or $manifest.new_scenarios -ne 13){throw 'Unexpected fixed plan'}
foreach($entry in $manifest.assemblies){if($entry.name -ne [IO.Path]::GetFileName($entry.name) -or (!$entry.name.EndsWith('.dll') -and $entry.name -ne 'NavigationSearchWrapperTests.exe')){throw 'Only fixed flat managed dependency filenames permitted'}}
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
 steps=@();test_results=@();artifacts=@();failure=$null;compiles=2;scenarios=0;assertions=0
 old_test_suites_rerun=@();old_executable_entrypoints_invoked=@();old_source_recompiled=$false
 original_read=$false;original_executed=$false;backend_started=$false;security_settings_changed=$false
 system_library_loaded=$false;native_math_calls=0;full_tick=$false;physical_mouse=$false;walking_accepted=$false;action_submitted=$false;gameplay_accepted=$false;mouse_input_chain_complete=$false;active_move_end_completed=$false
 support_boundary='Actual Idle165CB0 to163490 ordinary prefix and3CE890 wrapper with shared45B4/4BF0/37748, borrowed D8, real07F070/22C240/MainPlayer2EBB70/Options getters/08E200. Saved-map navigation-start request reaches only unimplemented123D50 after exact local SSO/payload preparation. True Ground-start result4 remains an unsupported camera-retry branch. OWN hot initialization is explicit; no cold Engine/Options/connection/NavManager recovery, action submission, physical mouse, render or playable client is claimed.'
}
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
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$library=Join-Path $output 'GrimDynasty.PlayerMoveRequest.dll';$tests=Join-Path $output 'PlayerMoveRequestTests.exe'
 $production=@('PlayerMoveRequestContracts.cs')|ForEach-Object{Join-Path $source $_}
 $testing=Join-Path $source 'PlayerMoveRequestTests.cs'
 Run-Step 'compile-player-move-request' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+$production)
 Run-Step 'compile-player-move-request-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:PlayerMoveRequestValidationProgram',"/out:$tests","/reference:$library")+$references+@($testing))
 Run-Step 'new-components' $tests @()
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-components.log') -Tail 1|ConvertFrom-Json
 if($summary.status -ne 'passed-new-player-move-request-components-only' -or $summary.scenarios -ne 13 -or $summary.assertions -le 0){throw 'Unexpected new component summary'}
 foreach($flag in @('physical_mouse','walking_accepted','action_submitted','system_library_loaded','original_read','original_executed','backend_started')){if($summary.$flag -ne $false){throw ('Scope flag changed: '+$flag)}}
 if($summary.old_test_suites_rerun -ne 0 -or $summary.native_math_calls -ne 0){throw 'Unplanned old-suite/native-math execution'}
 if($summary.saved_nav_start_result -ne 0 -or $summary.saved_ground_start_result -ne 4 -or $summary.last_transition_caller -ne 1455788 -or $summary.saved_corridor_count -le 1 -or $summary.prior_fixture_setup_checks -ne 4){throw 'Unexpected actual request/scene boundary'}
 $report.test_results=@($summary);$report.scenarios=$summary.scenarios;$report.assertions=$summary.assertions
 Check-Inputs;foreach($entry in $manifest.assemblies){Check-File (Join-Path $output $entry.name) $entry.sha256}
 $report.status='passed-new-player-move-request-components-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}};$report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 16|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;scenarios=$report.scenarios;assertions=$report.assertions;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 6
