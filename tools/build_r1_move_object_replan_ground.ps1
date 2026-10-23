param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$jsonAssembly='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Web.Extensions.dll'
$jsonHash='5e6378edc06f5a61920aaadb2aba839dd3cf302d6192e08aacd4454ba4cff0b3'
$systemCrt='C:\Windows\System32\ucrtbase.dll'
$systemCrtHash='dedb7611e1abc1ff3852dcac3d4c93a048d729e25b061973b9969f22698f14ca'
$manifestPath=Join-Path $taskRoot 'tools\r1_move_object_replan_ground_build_fingerprints.json'
$manifestHash='b8f0f6050f3c2b568e1e534408bd02cef5e684172f1ababa3ced8ab8ff947695'
$output=Join-Path $taskRoot '.tooling\r1-move-object-replan-ground-validation'
function Hash([string]$p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-Ancestors([string]$p){$cursor=[IO.Path]::GetFullPath($p);while($cursor){if(Test-Path -LiteralPath $cursor){$item=Get-Item -LiteralPath $cursor;if($item.Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Reparse path not permitted: '+$cursor)}};$parent=[IO.Directory]::GetParent($cursor);if($null -eq $parent){break};$cursor=$parent.FullName}}
function Check-File([string]$p,[string]$expected){Check-Ancestors $p;if($expected -notmatch '^[0-9a-f]{64}$'){throw ('Unfrozen input: '+$p)};$item=Get-Item -LiteralPath $p;if($item.PSIsContainer -or (Hash $p) -ne $expected){throw ('Input identity changed: '+$p)}}
function Under-Root([string]$relative){$p=[IO.Path]::GetFullPath((Join-Path $taskRoot $relative));if(!$p.StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Manifest path leaves workspace'};return $p}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace; no arguments'}
Check-File $manifestPath $manifestHash
$manifest=Get-Content -LiteralPath $manifestPath -Raw|ConvertFrom-Json
if($manifest.schema -ne 'grim-dynasty.fixed-move-object-replan-ground-build.v1' -or $manifest.compiles -ne 2 -or $manifest.sources.Count -ne 6 -or $manifest.assemblies.Count -ne 46 -or $manifest.suites.Count -ne 3){throw 'Unexpected fixed plan'}
if(@($manifest.assemblies|Where-Object {$_.name -eq 'GrimDynasty.NavigationCrowdReadyTick.dll'}).Count -ne 0){throw 'Old ReadyTick must not enter dependency closure'}
if(@($manifest.assemblies.name|Select-Object -Unique).Count -ne 46){throw 'Duplicate dependency name'}
Check-Ancestors $output
if(!([IO.Path]::GetFullPath($output)).StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Output outside workspace'}
if(Test-Path -LiteralPath $output){throw 'Existing output preserved; no overwrite/retry'}
function Check-Inputs {
 Check-File $compiler $compilerHash;Check-File $jsonAssembly $jsonHash;Check-File $systemCrt $systemCrtHash;Check-File $manifestPath $manifestHash
 $crt=Get-Item -LiteralPath $systemCrt;if($crt.Length -ne 1399656 -or $crt.VersionInfo.FileVersion -ne '10.0.26100.9711 (WinBuild.160101.0800)'){throw 'Frozen OWN UCRT version/size changed'}
 foreach($entry in $manifest.sources){Check-File (Under-Root $entry.path) $entry.sha256}
 foreach($entry in $manifest.evidence){Check-File (Under-Root $entry.path) $entry.sha256}
 foreach($entry in $manifest.prerequisites){Check-File (Under-Root $entry.path) $entry.sha256;$prior=Get-Content -LiteralPath (Under-Root $entry.path) -Raw|ConvertFrom-Json;if($prior.status -ne $entry.status -or $null -ne $prior.failure){throw ('Prerequisite no longer passed: '+$entry.path)}}
 foreach($entry in $manifest.assemblies){Check-File (Under-Root $entry.path) $entry.sha256;$prior=Get-Content -LiteralPath (Under-Root $entry.result) -Raw|ConvertFrom-Json;$record=@($prior.artifacts|Where-Object {$_.name -eq $entry.name -and $_.sha256 -eq $entry.sha256});if($record.Count -ne 1){throw ('Assembly lacks exact artifact receipt: '+$entry.name)}}
}
Check-Inputs
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash
 manifest=$manifestPath;manifest_sha256=$manifestHash;source_pins=$manifest.sources;prerequisites=$manifest.prerequisites;assembly_pins=$manifest.assemblies;evidence_pins=$manifest.evidence
 steps=@();test_results=@();sample_results=@();artifacts=@();failure=$null;compiles=2
 prior_sources_recompiled=@('NavigationCrowdTickViews.cs','NavigationCrowdTickTailContracts.cs','GameMovedReachedTailContracts.cs','NavigationCrowdTickPrefixContracts.cs')
 prior_sources_changed=@('NavigationCrowdTickPrefixContracts.cs');old_test_suites_rerun=@();old_executable_entrypoints_invoked=@();targeted_prior_case_regressions=@('State1 and both replan branches stop before membership publication: same frozen Tiny constructor, revised sync expectation25C7D2')
 old_ready_tick_dll_reused=$false;ready_tick_identity_preserved=$true;original_read=$false;original_executed=$false;backend_started=$false;security_settings_changed=$false;ucrt_control_changed=$false
 frozen_ucrt_loader_recompiled=$false;system_library_load_requested=$false;system_library_loaded=$false;native_math_calls=0
 real_saved_ground_floor=$false;move_object_produced_request=$false;real_sync_submit=$false;single_agent_two_frames=$false;target_prewritten=$false;work_prewritten=$false;full_tick=$false;physical_mouse=$false;native_scene_startup=$false;gameplay_accepted=$false
 support_boundary='Existing Agent/Character+38, shared Group/Crowd/Query and explicit OWN address registration sidecar. Actual Move151130 produces Target/State2/NeedReplan; actual synchronous25C7DB Submit must Complete50 before clearing pending flag. Real saved-map search and original Created481 parameters feed bounded ReadyTick/Moved/Ground/publication/tail twice. BSS, filter costs, locks, registration-map address, cold position and per-frame gates are explicit OWN inputs. Failure keeps State6/Reason4/global velocity/timer prefix and stops at unknown slot38 without clearing replan. State1, async, native AddAgent/map construction/startup/real mouse/multi-agent tick remain unclosed.'
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
 # No old ReadyTick DLL is copied/referenced. The single replacement library
 # retains the exact assembly simple name consumed by the frozen fixture EXE.
 $libraryRefs=@();$testRefs=@();foreach($entry in $manifest.assemblies){$r='/reference:'+(Join-Path $output $entry.name);$testRefs += $r;if($entry.name -notin @('NavigationCrowdReadyTickTests.exe','NavigationMoveObjectTests.exe','NavigationSearchWrapperTests.exe')){$libraryRefs += $r}}
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$library=Join-Path $output 'GrimDynasty.NavigationCrowdReadyTick.dll';$tests=Join-Path $output 'NavigationMoveObjectReplanGroundTests.exe'
 Run-Step 'compile-ready-tick-with-sync' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$libraryRefs+@((Join-Path $source 'NavigationCrowdTickViews.cs'),(Join-Path $source 'NavigationCrowdTickTailContracts.cs'),(Join-Path $source 'GameMovedReachedTailContracts.cs'),(Join-Path $source 'NavigationCrowdTickPrefixContracts.cs'),(Join-Path $source 'NavigationCrowdSynchronousReplanContracts.cs')))
 Run-Step 'compile-move-replan-ground-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:NavigationMoveObjectReplanGroundTests',"/out:$tests","/reference:$library","/reference:$jsonAssembly")+$testRefs+@((Join-Path $source 'NavigationMoveObjectReplanGroundTests.cs')))
 foreach($suite in $manifest.suites){
  if($suite.uses_ucrt){$report.system_library_load_requested=$true}
  Run-Step $suite.name $tests ([string[]]@($suite.arguments))
  $summary=Get-Content -LiteralPath (Join-Path $output ($suite.name+'.log')) -Tail 1|ConvertFrom-Json
  if($summary.status -ne $suite.status -or $summary.scenarios -ne $suite.scenarios -or $summary.assertions -le 0 -or $summary.full_tick -ne $false -or $summary.physical_mouse -ne $false){throw ('Unexpected suite summary: '+$suite.name)}
  $report.test_results += [ordered]@{name=$suite.name;kind=$suite.kind;scenarios=$summary.scenarios;assertions=$summary.assertions;native_math_calls=$summary.native_math_calls};$report.native_math_calls += $summary.native_math_calls
  if($suite.saved){
   $s=$summary.sample
   if($null -eq $s -or $s.tile_count -ne 9 -or $s.corridor_count -lt 2 -or $s.tile_crossings -lt 1 -or $s.flags -ne 1153 -or $s.move_reference -eq 0 -or $s.sync_count -ne 1 -or $s.frames.Count -ne 2 -or $s.native_registration -ne $false -or $s.full_tick -ne $false -or $s.native_math_calls -le 0){throw 'Saved MoveObject/sync/real corridor boundary failed'}
   foreach($name in @('same_agent','target_not_prewritten','work_not_prewritten','move_object_produced_request','real_sync_submit','real_created','frame_gate_inputs_owned')){if($s.$name -ne $true){throw ('Saved chain did not establish '+$name)}}
   for($n=0;$n -lt 2;$n++){$frame=$s.frames[$n];if($frame.work_cleared -ne $true -or $frame.update_returned -ne $true -or $frame.suffix_reference -eq 0 -or $frame.generation -ne 1 -or $frame.sync -ne ($n -eq 0)){throw 'A frame lacks actual sync/update/suffix/cleanup'}}
   $report.sample_results += [ordered]@{name=$suite.name;sample=$s}
  }
 }
 if($report.native_math_calls -le 0){throw 'No actual pinned UCRT rotation exercised'}
 Check-Inputs;foreach($entry in $manifest.assemblies){Check-File (Join-Path $output $entry.name) $entry.sha256}
 $report.system_library_loaded=$true;$report.real_saved_ground_floor=$true;$report.move_object_produced_request=$true;$report.real_sync_submit=$true;$report.single_agent_two_frames=$true;$report.status='passed-new-move-object-sync-replan-ground-two-frames-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}};$report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 18|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;test_results=$report.test_results;sample_results=$report.sample_results;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 14
