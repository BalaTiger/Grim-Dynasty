param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$manifestPath=Join-Path $taskRoot 'tools\r1_game_moved_saved_dependency_repair_fingerprints.json'
$manifestHash='3521cda0f1a37677cb94df06ca5f7d58c24dee8ba9e3f49aead3c94000757d26'
$output=Join-Path $taskRoot '.tooling\r1-game-moved-saved-dependency-repair-validation'
function Hash([string]$p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-Ancestors([string]$p){$cursor=[IO.Path]::GetFullPath($p);while($cursor){if(Test-Path -LiteralPath $cursor){$item=Get-Item -LiteralPath $cursor;if($item.Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Reparse path not permitted: '+$cursor)}};$parent=[IO.Directory]::GetParent($cursor);if($null -eq $parent){break};$cursor=$parent.FullName}}
function Check-File([string]$p,[string]$expected){Check-Ancestors $p;if($expected -notmatch '^[0-9a-f]{64}$'){throw ('Unfrozen input: '+$p)};$item=Get-Item -LiteralPath $p;if($item.PSIsContainer -or (Hash $p) -ne $expected){throw ('Input identity changed: '+$p)}}
function Under-Root([string]$relative){$p=[IO.Path]::GetFullPath((Join-Path $taskRoot $relative));if(!$p.StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Manifest path leaves workspace'};return $p}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace; no arguments'}
Check-File $manifestPath $manifestHash;$manifest=Get-Content -LiteralPath $manifestPath -Raw|ConvertFrom-Json
if($manifest.schema -ne 'grim-dynasty.fixed-game-moved-saved-dependency-repair.v1' -or $manifest.compiles -ne 0 -or $manifest.saved_resource_scenarios -ne 1 -or $manifest.assemblies.Count -ne 40 -or $manifest.added_dependencies.Count -ne 1){throw 'Unexpected bounded repair plan'}
if($manifest.added_dependencies[0].name -ne 'MainmenuTilePipeline.exe'){throw 'Only the identified managed reference may be added'}
Check-Ancestors $output;if(Test-Path -LiteralPath $output){throw 'Existing repair output preserved; no overwrite/retry'}
function Check-Inputs {
 Check-File $manifestPath $manifestHash
 foreach($entry in $manifest.inputs){Check-File (Under-Root $entry.path) $entry.sha256}
 foreach($entry in $manifest.system_files){Check-File $entry.path $entry.sha256}
 foreach($entry in $manifest.assemblies){
  Check-File (Under-Root $entry.path) $entry.sha256
  $receipt=Get-Content -LiteralPath (Under-Root $entry.result) -Raw|ConvertFrom-Json
  $record=@($receipt.artifacts|Where-Object {$_.name -eq $entry.name -and $_.sha256 -eq $entry.sha256});if($record.Count -ne 1){throw ('Assembly lacks exact saved artifact record: '+$entry.name)}
 }
 $prior=Get-Content -LiteralPath (Under-Root $manifest.partial_build_result) -Raw|ConvertFrom-Json
 if($prior.status -ne 'failed' -or $prior.failure -ne 'Step new-real-crowd-game-moved failed (invocation=False, exit=1); evidence retained'){throw 'Original failure differs from diagnosed loader failure'}
 foreach($name in @('compile-game-moved','compile-game-moved-tests','new-skill-moved-tests','new-game-moved-tests')){$step=@($prior.steps|Where-Object {$_.name -eq $name});if($step.Count -ne 1 -or !$step[0].invocation_succeeded -or $step[0].exit_code -ne 0){throw ('Completed prior stage not confirmed: '+$name)}}
 $skill=Get-Content -LiteralPath (Under-Root $manifest.skill_stdout) -Tail 1|ConvertFrom-Json
 $component=Get-Content -LiteralPath (Under-Root $manifest.component_stdout) -Tail 1|ConvertFrom-Json
 if($skill.status -ne 'passed-skill-moved' -or $skill.scenarios -ne 8 -or $skill.assertions -ne 73 -or $component.status -ne 'passed-game-moved-component' -or $component.scenarios -ne 13 -or $component.assertions -ne 236 -or $component.native_math_calls -ne 9){throw 'Prior21 passed scenarios must be evidenced by their exact stdout, not rerun'}
 $dependency=Get-Content -LiteralPath (Under-Root $manifest.added_dependencies[0].result) -Raw|ConvertFrom-Json
 if($dependency.status -ne 'passed-new-crowd-waypoint-contracts-and-saved-resource-continuation-only' -or $null -ne $dependency.failure){throw 'Missing assembly is not from the pinned passed producer result'}
}
Check-Inputs
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';manifest=$manifestPath;manifest_sha256=$manifestHash
 partial_build_result=$manifest.partial_build_result;prior_completed_stdout=@($manifest.skill_stdout,$manifest.component_stdout);prior_completed_scenarios=21;prior_completed_assertions=309;prior_component_native_math_calls=9
 missing_dependency_reason=$manifest.reason;assemblies=$manifest.assemblies;added_dependencies=$manifest.added_dependencies;dependency_closure=$manifest.dependency_closure;input_pins=$manifest.inputs
 compiles=0;steps=@();test_results=@();sample_result=$null;artifacts=@();failure=$null
 old_test_suites_rerun=@();passed_scenarios_rerun=0;old_executable_entrypoints_invoked=@();original_failed_directory_modified=$false
 system_library_load_requested=$false;system_library_loaded=$false;native_math_calls=0;original_read=$false;original_executed=$false;backend_started=$false;security_settings_changed=$false;ucrt_control_changed=$false
 real_saved_ground_floor=$false;real_crowd_callback_continuation=$false;full_tick=$false;physical_mouse=$false;native_scene_startup=$false;native_loaded_skill_collections_known=$false;gameplay_accepted=$false
 support_boundary='Repair managed assembly probing by supplying the existing passed MainmenuTilePipeline.exe as a referenced assembly. Invoke only the already-compiled new GameMovedIntegrationTests --saved-resource mode. Same explicit OWN scene/actor/neighbor/skill/frame state and actual saved Ground/UCRT/publication boundaries apply; full tick, native scene startup and gameplay remain unvalidated.'
}
New-Item -ItemType Directory -Path $output|Out-Null
try{
 foreach($entry in $manifest.assemblies){Copy-Item -LiteralPath (Under-Root $entry.path) -Destination (Join-Path $output $entry.name);Check-File (Join-Path $output $entry.name) $entry.sha256}
 $program=Join-Path $output 'GameMovedIntegrationTests.exe';$stdout=Join-Path $output 'new-real-crowd-game-moved.log';$stderr=Join-Path $output 'new-real-crowd-game-moved.stderr.log';$savedPreference=$ErrorActionPreference;$report.system_library_load_requested=$true
 try{$ErrorActionPreference='Continue';& $program '--saved-resource' 1> $stdout 2> $stderr;$ok=$?;$code=$LASTEXITCODE}finally{$ErrorActionPreference=$savedPreference}
 $report.steps += [ordered]@{name='new-real-crowd-game-moved-only';program=$program;arguments=@('--saved-resource');invocation_succeeded=$ok;exit_code=$code}
 if(!$ok -or $code -ne 0){throw "Saved-resource-only run failed (invocation=$ok, exit=$code); evidence retained"}
 if((Get-Item -LiteralPath $stderr).Length -ne 0){throw 'Unexpected saved-resource diagnostics'}
 $summary=Get-Content -LiteralPath $stdout -Tail 1|ConvertFrom-Json
 if($summary.status -ne 'passed-real-crowd-game-moved' -or $summary.scenarios -ne 1 -or $summary.assertions -le 0){throw 'Unexpected saved-resource summary'}
 $s=$summary.sample
 if($null -eq $s -or $s.tile_count -ne 9 -or $s.corridor_count -lt 2 -or $s.tile_crossings -lt 1 -or $s.native_math_calls -le 0){throw 'Actual saved corridor or UCRT path not established'}
 foreach($name in @('same_agent','actual_position_changed','floor_succeeded','rotation_returned','notification_returned','callback_returned')){if($s.$name -ne $true){throw ('Saved callback did not establish '+$name)}}
 foreach($name in @('nav_height_used','full_tick')){if($s.$name -ne $false){throw ('Saved callback exceeded boundary '+$name)}}
 $report.test_results += [ordered]@{name='saved-resource-only-after-managed-dependency-repair';scenarios=1;assertions=$summary.assertions}
 $report.sample_result=$s;$report.native_math_calls=$s.native_math_calls;$report.system_library_loaded=$true
 Check-Inputs;foreach($entry in $manifest.assemblies){Check-File (Join-Path $output $entry.name) $entry.sha256}
 $report.real_saved_ground_floor=$true;$report.real_crowd_callback_continuation=$true;$report.status='passed-new-real-crowd-game-moved-after-managed-dependency-repair'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}};$report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;test_results=$report.test_results;sample_result=$report.sample_result;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 8

