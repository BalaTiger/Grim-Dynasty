param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$jsonAssembly='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Web.Extensions.dll'
$jsonHash='5e6378edc06f5a61920aaadb2aba839dd3cf302d6192e08aacd4454ba4cff0b3'
$systemCrt='C:\Windows\System32\ucrtbase.dll'
$systemCrtHash='dedb7611e1abc1ff3852dcac3d4c93a048d729e25b061973b9969f22698f14ca'
$manifestPath=Join-Path $taskRoot 'tools\r1_finite_ground_start_build_fingerprints.json'
$manifestHash='8b9f003bea4df4c1604779507f3b5d24aa462c914dc6ebde166d22341746738e'
$output=Join-Path $taskRoot '.tooling\r1-finite-ground-start-validation'
function Hash([string]$p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-Ancestors([string]$p){$cursor=[IO.Path]::GetFullPath($p);while($cursor){if(Test-Path -LiteralPath $cursor){$item=Get-Item -LiteralPath $cursor;if($item.Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Reparse path not permitted: '+$cursor)}};$parent=[IO.Directory]::GetParent($cursor);if($null -eq $parent){break};$cursor=$parent.FullName}}
function Check-File([string]$p,[string]$expected){Check-Ancestors $p;if($expected -notmatch '^[0-9a-f]{64}$'){throw ('Unfrozen input: '+$p)};$item=Get-Item -LiteralPath $p;if($item.PSIsContainer -or (Hash $p) -ne $expected){throw ('Input identity changed: '+$p)}}
function Under-Root([string]$relative){$p=[IO.Path]::GetFullPath((Join-Path $taskRoot $relative));if(!$p.StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Manifest path leaves workspace'};return $p}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace; no arguments'}
Check-File $manifestPath $manifestHash
$manifest=Get-Content -LiteralPath $manifestPath -Raw|ConvertFrom-Json
if($manifest.schema -ne 'grim-dynasty.fixed-finite-ground-start-build.v1' -or $manifest.compiles -ne 1 -or $manifest.probes -ne 1 -or $manifest.candidate_limit -ne 16 -or $manifest.sources.Count -ne 1 -or $manifest.assemblies.Count -ne 51){throw 'Unexpected finite probe plan'}
if(@($manifest.assemblies.name|Select-Object -Unique).Count -ne 51){throw 'Duplicate dependency name'}
$ready=@($manifest.assemblies|Where-Object {$_.name -eq 'GrimDynasty.NavigationCrowdReadyTick.dll'})
if($ready.Count -ne 1 -or $ready[0].sha256 -ne 'f5616991f5ee1fdbb9eb73ce8e90ab761d65985baa8812f6707c77f0a4905d28'){throw 'Latest actual sync ReadyTick is mandatory'}
Check-Ancestors $output
if(!([IO.Path]::GetFullPath($output)).StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Output outside workspace'}
if(Test-Path -LiteralPath $output){throw 'Existing output preserved; no overwrite/retry'}
function Check-Inputs {
 Check-File $compiler $compilerHash;Check-File $jsonAssembly $jsonHash;Check-File $systemCrt $systemCrtHash;Check-File $manifestPath $manifestHash
 $crt=Get-Item -LiteralPath $systemCrt;if($crt.Length -ne 1399656 -or $crt.VersionInfo.FileVersion -ne '10.0.26100.9711 (WinBuild.160101.0800)'){throw 'Frozen OWN UCRT version/size changed'}
 foreach($entry in @($manifest.sources)+@($manifest.evidence)){Check-File (Under-Root $entry.path) $entry.sha256}
 foreach($entry in $manifest.prerequisites){Check-File (Under-Root $entry.path) $entry.sha256;$prior=Get-Content -LiteralPath (Under-Root $entry.path) -Raw|ConvertFrom-Json;if($prior.status -ne $entry.status -or $prior.status -notlike 'passed-*' -or $null -ne $prior.failure){throw ('Prerequisite no longer passed: '+$entry.path)}}
 foreach($entry in $manifest.assemblies){Check-File (Under-Root $entry.path) $entry.sha256;$prior=Get-Content -LiteralPath (Under-Root $entry.result) -Raw|ConvertFrom-Json;$record=@($prior.artifacts|Where-Object {$_.name -eq $entry.name -and $_.sha256 -eq $entry.sha256});if($record.Count -ne 1){throw ('Assembly lacks exact artifact receipt: '+$entry.name)}}
 $old=Get-Content -LiteralPath (Under-Root $manifest.old_failure_receipt) -Raw|ConvertFrom-Json
 $sample=@($old.samples|Where-Object {$_.name -eq 'saved-ground-start-to-ground-destination'})
 if($old.ground_start_result -ne 4 -or $old.ground_start_path_accepted -ne $false -or $sample.Count -ne 1 -or $sample[0].result -ne 4 -or $sample[0].first_nearest -ne $true -or $sample[0].second_nearest -ne $false -or $sample[0].height_offset_added -ne $false){throw 'Prior real Ground-start failure must remain intact'}
}
Check-Inputs
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';manifest=$manifestPath;manifest_sha256=$manifestHash
 compiler=$compiler;compiler_sha256=$compilerHash;source_pins=$manifest.sources;assembly_pins=$manifest.assemblies;prerequisites=$manifest.prerequisites;evidence_pins=$manifest.evidence
 steps=@();probe_result=$null;artifacts=@();failure=$null;compiles=1;old_source_recompiled=$false;old_suites_rerun=@();old_executable_entrypoints_invoked=@()
 source_of_initial_y='Actual saved Ground Floor return only';own_start_selection=$true;native_spawn=$false;candidate_limit=16;old_ground_failure_rerun=$false;latest_ready_tick_reused=$true
 original_read=$false;original_executed=$false;backend_started=$false;security_settings_changed=$false;height_offset_added=$false;mesh_changed=$false;physical_mouse=$false;full_tick=$false;gameplay_accepted=$false
 system_library_load_requested=$false;system_library_loaded=$false;native_math_calls=0;ground_initial_two_frames_accepted=$false
 support_boundary=$manifest.success_boundary;stop_boundary=$manifest.stop_boundary;own_state=$manifest.own_state
}
New-Item -ItemType Directory -Path $output|Out-Null
function Run-Step([string]$name,[string]$program,[string[]]$arguments){
 $stdout=Join-Path $output ($name+'.log');$stderr=Join-Path $output ($name+'.stderr.log');$savedPreference=$ErrorActionPreference
 try{$ErrorActionPreference='Continue';& $program @arguments 1> $stdout 2> $stderr;$ok=$?;$code=$LASTEXITCODE}finally{$ErrorActionPreference=$savedPreference}
 $report.steps += [ordered]@{name=$name;program=$program;arguments=$arguments;invocation_succeeded=$ok;exit_code=$code}
 if(!$ok -or $code -ne 0){throw "Step $name failed (invocation=$ok, exit=$code); partial evidence retained"}
 if((Get-Item -LiteralPath $stderr).Length -ne 0 -or ($name.StartsWith('compile-') -and (Get-Item -LiteralPath $stdout).Length -ne 0)){throw ('Unexpected diagnostics: '+$name)}
}
try {
 $refs=@();foreach($entry in $manifest.assemblies){$copied=Join-Path $output $entry.name;Copy-Item -LiteralPath (Under-Root $entry.path) -Destination $copied;Check-File $copied $entry.sha256;$refs+=('/reference:'+$copied)}
 $probe=Join-Path $output 'FiniteGroundStartMovementProbe.exe'
 Run-Step 'compile-finite-ground-start-probe' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:FiniteGroundStartMovementProbe',"/out:$probe","/reference:$jsonAssembly")+$refs+@((Under-Root $manifest.sources[0].path)))
 $report.system_library_load_requested=$true
 Run-Step 'finite-ground-start-probe' $probe ([string[]]@())
 $s=Get-Content -LiteralPath (Join-Path $output 'finite-ground-start-probe.log') -Tail 1|ConvertFrom-Json;$report.probe_result=$s
 if($s.status -ne 'completed-finite-ground-start-probe' -or $s.assertions -le 0 -or $s.candidate_limit -ne 16 -or $s.candidates_attempted -lt 1 -or $s.candidates_attempted -gt 16 -or $s.attempts.Count -ne $s.candidates_attempted){throw 'Invalid finite probe summary'}
 foreach($name in @('height_offset_added','mesh_changed','physical_mouse','full_tick','original_read','native_spawn')){if($s.$name -ne $false){throw ('Probe crossed boundary: '+$name)}}
 if($s.unknown_stops_immediately -ne $true -or $s.own_start_selection -ne $true -or $s.old_suites_rerun -ne 0 -or $s.navigation_resource_scene_constructor_calls -ne 1 -or $s.asset_constructor_calls -ne 1 -or $s.character_scene_constructor_calls -ne $s.candidates_attempted){throw 'Unexpected fixture lifecycle or policy'}
 if(($s.first_extent -join ',') -ne '0.05,3,0.05' -or ($s.second_extent -join ',') -ne '10,3,10' -or ($s.move_extent -join ',') -ne '0.125,2,0.125'){throw 'Original fixed extents changed'}
 $caller=$s.own_precheck_caller_inputs;if($caller.radius -ne 10 -or $caller.minimum_distance -ne 0 -or $caller.maximum_ratio -ne 0 -or $caller.minimum_dot -ne -1 -or $caller.ratio_baseline -ne 0 -or $caller.force_ratio -ne 0 -or $caller.native_mouse_thresholds -ne $false){throw 'Fixed OWN caller inputs changed'}
 if($s.old_failure_preserved.receipt -ne $manifest.old_failure_receipt -or $s.old_failure_preserved.result -ne 4 -or $s.old_failure_preserved.rerun -ne $false){throw 'Old failure not retained'}
 $offsets=@(-2,-0.75,0.75,2);for($n=0;$n -lt $s.candidates_attempted;$n++){$a=$s.attempts[$n];$dx=$offsets[$n%4];$dz=$offsets[[int][Math]::Floor($n/4)];if($a.index -ne ($n+1) -or $a.offset_xz[0] -ne $dx -or $a.offset_xz[1] -ne $dz -or $a.input_xz[0] -ne (64.25+$dx) -or $a.input_xz[1] -ne (64.75+$dz) -or $a.floor_ray_seed_y -ne 29.5 -or $a.ground_lock_released -ne $true -or $a.own_start_selection -ne $true -or $a.native_spawn -ne $false){throw 'Candidate not the fixed OWN plan'};if($n -lt ($s.candidates_attempted-1) -and $a.outcome -notin @('normal-start-floor-rejection','normal-destination-floor-rejection','normal-precheck-rejection','normal-move-object-rejection')){throw 'Probe advanced after a success or Unsupported'}}
 $last=$s.attempts[$s.candidates_attempted-1]
 switch($s.outcome){
  'supported' {
   if($last.outcome -ne 'supported' -or $s.selected_index -ne $s.candidates_attempted -or $last.precheck_result -ne 0 -or $last.move_result -ne 1 -or $last.frames.Count -ne 2 -or $last.sync_count -lt 1 -or $s.native_math_calls -le 0){throw 'Actual Ground/precheck/Move/sync/two-frame chain missing'}
   foreach($name in @('same_agent','ground_initial_y_preserved_until_tick')){if($last.$name -ne $true){throw ('Shared Ground-start identity missing: '+$name)}}
   if($last.target_prewritten -ne $false -or $last.work_prewritten -ne $false){throw 'Request or Work prewritten'}
   for($n=0;$n -lt 2;$n++){$f=$last.frames[$n];if($f.frame -ne ($n+1) -or $f.work_cleared -ne $true -or $f.update_returned -ne $true -or $f.suffix_reference -eq 0 -or ($n -eq 0 -and $f.sync -ne $true)){throw 'A frame lacks real update/sync/suffix/cleanup'}}
   $report.ground_initial_two_frames_accepted=$true;$report.status='passed-finite-own-ground-start-two-frames-only'
  }
  'no-supported-point' {if($s.selected_index -ne 0 -or $s.candidates_attempted -ne 16 -or $last.outcome -notlike 'normal-*-rejection'){throw 'Incomplete finite normal-rejection plan'};$report.status='completed-bounded-no-supported-ground-start'}
  'unsupported' {if($s.selected_index -ne 0 -or $last.outcome -ne 'unsupported' -or $null -eq $last.unsupported_at -or [string]::IsNullOrEmpty($last.unsupported_reason)){throw 'Missing exact Unsupported stop'};$report.status='blocked-finite-ground-start-at-supported-boundary'}
  default {throw 'Unknown finite outcome'}
 }
 $report.system_library_loaded=$true;$report.native_math_calls=$s.native_math_calls
 Check-Inputs;foreach($entry in $manifest.assemblies){Check-File (Join-Path $output $entry.name) $entry.sha256}
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}};$report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 22|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;probe_result=$report.probe_result;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 16
