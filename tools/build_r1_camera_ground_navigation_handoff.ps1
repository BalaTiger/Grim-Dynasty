param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$jsonAssembly='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Web.Extensions.dll'
$jsonAssemblyHash='5e6378edc06f5a61920aaadb2aba839dd3cf302d6192e08aacd4454ba4cff0b3'
$output=Join-Path $taskRoot '.tooling\r1-camera-ground-navigation-handoff-validation'
$manifestPath=Join-Path $taskRoot 'tools\r1_camera_ground_navigation_handoff_build_fingerprints.json'
$manifestHash='c829fa2e5131da8b110930e391595da744da3f39813aff51ec4d9a08faf2414d'

function Hash([string]$path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-Ancestors([string]$path){
 $cursor=[IO.Path]::GetFullPath($path)
 while($cursor){
  if(Test-Path -LiteralPath $cursor){$item=Get-Item -LiteralPath $cursor -Force;if($item.Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Reparse path not permitted: '+$cursor)}}
  $parent=[IO.Directory]::GetParent($cursor);if($null -eq $parent){break};$cursor=$parent.FullName
 }
}
function Check-File([string]$path,[string]$expected){
 Check-Ancestors $path;$item=Get-Item -LiteralPath $path
 if($expected -notmatch '^[0-9a-f]{64}$' -or $item.PSIsContainer -or (Hash $path) -ne $expected){throw ('Input identity changed or unfrozen: '+$path)}
}
function Workspace-Path([string]$relative){
 if([IO.Path]::IsPathRooted($relative)){throw 'Manifest paths must be workspace-relative'}
 $path=[IO.Path]::GetFullPath((Join-Path $taskRoot $relative))
 if(!$path.StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Manifest path left fixed workspace'}
 return $path
}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace and no arguments required'}
Check-Ancestors $output
if(!([IO.Path]::GetFullPath($output)).StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Output must stay in fixed workspace'}
if(Test-Path -LiteralPath $output){throw 'Existing handoff evidence preserved; output must be new'}
Check-File $manifestPath $manifestHash
$manifest=Get-Content -LiteralPath $manifestPath -Raw|ConvertFrom-Json
if($manifest.schema -ne 'grim-dynasty.camera-ground-navigation-handoff-build.v1' -or $manifest.source_review_ready -ne $true){throw 'Frozen reviewed handoff manifest required'}
if(@($manifest.sources).Count -ne 2 -or @($manifest.dependencies).Count -ne 26 -or @($manifest.prior_results).Count -ne 2 -or @($manifest.evidence).Count -ne 10){throw 'Fixed two-source, twenty-six-DLL plan required'}
if(@($manifest.dependencies.name|Sort-Object -Unique).Count -ne 26){throw 'Dependency DLL names must be unique'}
if($manifest.new_test_entrypoint -ne 'CameraGroundNavigationHandoffTestProgram' -or $manifest.compiles -ne 1 -or $manifest.expected_scenarios -ne 3 -or $manifest.expected_assertions -ne 89){throw 'Fixed independent three-scenario plan required'}
foreach($dependency in $manifest.dependencies){if($dependency.name -ne [IO.Path]::GetFileName($dependency.name) -or !$dependency.name.EndsWith('.dll')){throw 'Only flat production DLL names may be copied'}}
foreach($prior in $manifest.prior_results){
 $path=Workspace-Path $prior.path;Check-File $path $prior.sha256;$result=Get-Content -LiteralPath $path -Raw|ConvertFrom-Json
 if($result.status -ne $prior.expected_status -or $null -ne $result.failure){throw ('Passed prior result required: '+$prior.path)}
 foreach($dependency in @($manifest.dependencies|Where-Object prior_result -EQ $prior.path)){
  $record=@($result.artifacts|Where-Object name -EQ $dependency.name)
  if($record.Count -ne 1 -or $record[0].sha256 -ne $dependency.sha256){throw ('Dependency not uniquely pinned by its passed result: '+$dependency.name)}
 }
}
function Check-Inputs {
 Check-Ancestors $output;Check-File $compiler $compilerHash;Check-File $jsonAssembly $jsonAssemblyHash;Check-File $manifestPath $manifestHash
 foreach($record in $manifest.sources){Check-File (Workspace-Path $record.path) $record.sha256}
 foreach($record in $manifest.dependencies){Check-File (Workspace-Path $record.path) $record.sha256}
 foreach($record in $manifest.prior_results){Check-File (Workspace-Path $record.path) $record.sha256}
 foreach($record in $manifest.evidence){Check-File (Workspace-Path $record.path) $record.sha256}
}
Check-Inputs
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash;json_assembly=$jsonAssembly;json_assembly_sha256=$jsonAssemblyHash
 manifest=$manifestPath;manifest_sha256=$manifestHash;source_pins=$manifest.sources;dependency_pins=$manifest.dependencies;prior_result_pins=$manifest.prior_results;evidence_pins=$manifest.evidence
 steps=@();test_results=@();samples=@();artifacts=@();failure=$null;compiles=0;old_test_suites_rerun=@();old_executable_entrypoints_invoked=@()
 reused_production_method='MainmenuTilePipeline.BuildExplicitColdTiles';new_test_entrypoint='CameraGroundNavigationHandoffTestProgram';production_dlls_recompiled=$false;helper_source_recompiled=$true;helper_source='src/reverse_client_actions/MainmenuTilePipeline.cs'
 explicit_own_origin0=$true;search_extent_changed=$false;height_offset_added=$false;camera_navigation_hit_required=$false
 camera_mode=1;system_math_provider_supplied=$false;system_library_profile_opened=$false;native_pick_caller=$false;native_scene_startup=$false;walking_accepted=$false
 original_read=$false;original_executed=$false;backend_started=$false;security_settings_changed=$false
 support_boundary='Three new fixed saved-mainmenu points through actual camera ray, saved Ground, same canonical origin0 Region, actual22C1A0 and actual nearest. The camera reference is reported even if zero. Two ready Ground points must remain real nearest misses with extent2. Explicit own fixture, not loaded native scene, native pick caller or walking acceptance. No old Main or suite runs.'
}
New-Item -ItemType Directory -Path $output|Out-Null
$probe=Join-Path $output 'CameraGroundNavigationHandoffTests.exe'
function Run-Step([string]$name,[string]$program,[string[]]$arguments){
 if($program -ne $compiler -and $program -ne $probe){throw 'Only the fixed compiler and new probe may execute'}
 foreach($argument in $arguments){if($argument -match '[\s"]'){throw 'This fixed no-space argument plan changed'}}
 $stdout=Join-Path $output ($name+'.log');$stderr=Join-Path $output ($name+'.stderr.log')
 $start=@{FilePath=$program;WorkingDirectory=$taskRoot;WindowStyle='Hidden';PassThru=$true;RedirectStandardOutput=$stdout;RedirectStandardError=$stderr}
 if($arguments.Count -gt 0){$start.ArgumentList=$arguments}
 $process=Start-Process @start
 try{
  # Keep this started instance's native handle open through wait/termination.
  $ownedProcessHandle=$process.Handle
  $completed=$process.WaitForExit(60000)
  if(!$completed){
   try{$process.Kill()}catch{if(!$process.HasExited){throw}}
   $process.WaitForExit()
   throw ('Fixed sixty-second step budget exceeded: '+$name)
  }
  $process.WaitForExit();$code=$process.ExitCode
  $report.steps += [ordered]@{name=$name;program=$program;arguments=$arguments;exit_code=$code;timeout_ms=60000}
  if($code -ne 0){throw ('Step failed: '+$name+'; exit='+$code+'; logs retained')}
  if((Get-Item -LiteralPath $stderr).Length -ne 0 -or ($name.StartsWith('compile-') -and (Get-Item -LiteralPath $stdout).Length -ne 0)){throw ('Unexpected diagnostics: '+$name)}
 }finally{$process.Dispose()}
}
try{
 foreach($dependency in $manifest.dependencies){Copy-Item -LiteralPath (Workspace-Path $dependency.path) -Destination (Join-Path $output $dependency.name);Check-File (Join-Path $output $dependency.name) $dependency.sha256}
 $references=@();foreach($dependency in @($manifest.dependencies|Sort-Object name)){$references+='/reference:'+(Join-Path $output $dependency.name)}
 $sources=@();foreach($source in $manifest.sources){$sources+=Workspace-Path $source.path}
 Run-Step 'compile-new-camera-ground-nearest' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:CameraGroundNavigationHandoffTestProgram',"/out:$probe","/reference:$jsonAssembly")+$references+$sources)
 $report.compiles=1
 Run-Step 'new-camera-ground-nearest' $probe @()
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-camera-ground-nearest.log') -Tail 1|ConvertFrom-Json
 if($summary.status -ne 'passed-new-camera-ground-nearest-handoff-checks' -or $summary.scenarios -ne 3 -or $summary.assertions -ne 89 -or @($summary.samples).Count -ne 3){throw 'Unexpected new handoff summary'}
 if($summary.explicit_own_origin0 -ne $true){throw 'One explicit origin0 frame required'}
 foreach($name in @('search_extent_changed','height_offset_added','native_pick_caller','native_scene_startup','walking_accepted','old_suite_main_called','original_read','original_executed')){if($summary.$name -ne $false){throw ('Probe exceeded declared boundary: '+$name)}}
 $names=@('camera-click','ready-frame-1-ground','ready-frame-2-ground')
 $fixedX=@(64.25,63.0971069,63.1942139);$fixedZ=@(64.75,52.82203,52.84406)
 for($n=0;$n -lt 3;$n++){
  $sample=$summary.samples[$n]
  if($sample.name -ne $names[$n] -or $sample.region_token -ne 77 -or $sample.actual_camera_getters -ne 2 -or @($sample.region_offset).Count -ne 3 -or @($sample.region_offset|Where-Object {$_ -ne 0}).Count -ne 0){throw 'Unexpected canonical Region or actual camera sample'}
  if(@($sample.extent).Count -ne 3 -or $sample.extent[0] -ne .125 -or $sample.extent[1] -ne 2 -or $sample.extent[2] -ne .125){throw 'Query extent changed'}
  if(@($sample.ground).Count -ne 3 -or [Math]::Abs([double]$sample.ground[0]-$fixedX[$n]) -gt .00001 -or [Math]::Abs([double]$sample.ground[2]-$fixedZ[$n]) -gt .00001){throw 'Fixed Ground XZ sample changed'}
  if([bool]$sample.navigation_hit -ne ([uint32]$sample.nearest_reference -ne 0)){throw 'Actual returned reference and reported hit disagree'}
  if($sample.nearest_reference -eq 0 -and $null -ne $sample.projected){throw 'A nearest miss must not invent a projected point'}
  if($n -gt 0 -and ($sample.expected_negative -ne $true -or $sample.nearest_reference -ne 0 -or $sample.walkable_xz -le 0 -or $sample.walkable_xyz -ne 0)){throw 'Ready Ground exclusion negative case changed'}
 }
 $report.test_results+= [ordered]@{name='new-camera-ground-nearest';kind='new';scenarios=$summary.scenarios;assertions=$summary.assertions}
 $report.samples=$summary.samples
 Check-Inputs;foreach($dependency in $manifest.dependencies){Check-File (Join-Path $output $dependency.name) $dependency.sha256}
 $report.status='passed-new-camera-ground-nearest-handoff-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;test_results=$report.test_results;samples=$report.samples;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 8
