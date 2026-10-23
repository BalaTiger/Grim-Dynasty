param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$jsonAssembly='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Web.Extensions.dll'
$jsonAssemblyHash='5e6378edc06f5a61920aaadb2aba839dd3cf302d6192e08aacd4454ba4cff0b3'
$output=Join-Path $taskRoot '.tooling\r1-navigation-search-wrapper-validation'
$manifestPath=Join-Path $taskRoot 'tools\r1_navigation_search_wrapper_build_fingerprints.json'
$manifestHash='dc05130b23aa836ba4ff908fbe04d9cfc08a7f99221d5f307533058e6651c614'

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
if(Test-Path -LiteralPath $output){throw 'Existing wrapper evidence preserved; output must be new'}
Check-File $manifestPath $manifestHash
$manifest=Get-Content -LiteralPath $manifestPath -Raw|ConvertFrom-Json
if($manifest.schema -ne 'grim-dynasty.navigation-search-wrapper-build.v1' -or $manifest.source_review_ready -ne $true){throw 'Frozen reviewed wrapper manifest required'}
if(@($manifest.sources).Count -ne 3 -or @($manifest.dependencies).Count -ne 21 -or @($manifest.prior_results).Count -ne 2 -or @($manifest.evidence).Count -ne 13){throw 'Fixed three-source, twenty-one-DLL plan required'}
if(@($manifest.dependencies.name|Sort-Object -Unique).Count -ne 21){throw 'Dependency DLL names must be unique'}
if($manifest.new_test_entrypoint -ne 'NavigationSearchWrapperTests' -or $manifest.new_library -ne 'GrimDynasty.NavigationSearchWrapper.dll' -or $manifest.compiles -ne 2 -or $manifest.expected_scenarios -ne 11 -or $manifest.expected_assertions -ne 113){throw 'Fixed eleven-scenario, two-compile plan required'}
foreach($role in @('library','test','test-helper')){if(@($manifest.sources|Where-Object role -EQ $role).Count -ne 1){throw ('Exactly one source per fixed role required: '+$role)}}
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
 reused_production_method='MainmenuTilePipeline.BuildExplicitColdTiles';new_test_entrypoint='NavigationSearchWrapperTests';old_production_dlls_recompiled=$false;helper_source_recompiled=$true;helper_source='src/reverse_client_actions/MainmenuTilePipeline.cs'
 own_initialization=$true;observed_ctor_extent=@(.05,3,.05);loaded_native_extent=$false;ground_destination_success_required=$false;height_offset_added=$false;injected_success=$false
 native_loaded_state=$false;native_pick_caller=$false;physical_mouse=$false;walking_accepted=$false;original_read=$false;original_executed=$false;backend_started=$false;security_settings_changed=$false
 support_boundary='Actual saved-map Nearest/Corridor/StraightPath/Translate/Floor composition through new Engine10C6C0/150590 wrapper. Same canonical Region, explicit OWN constructor scalar extent and filter/pool state, finite typed storage, null output vector. Ordinary endpoint path must succeed; actual Ground destination result is recorded without forcing success. Unknown Query10 is an explicit OWN adapter stop. No old Main/suite, native scene startup or walking acceptance.'
}
New-Item -ItemType Directory -Path $output|Out-Null
$library=Join-Path $output 'GrimDynasty.NavigationSearchWrapper.dll'
$probe=Join-Path $output 'NavigationSearchWrapperTests.exe'
function Run-Step([string]$name,[string]$program,[string[]]$arguments){
 if($program -ne $compiler -and $program -ne $probe){throw 'Only the fixed compiler and new probe may execute'}
 foreach($argument in $arguments){if($argument -match '[\s"]'){throw 'This fixed no-space argument plan changed'}}
 $stdout=Join-Path $output ($name+'.log');$stderr=Join-Path $output ($name+'.stderr.log')
 $start=@{FilePath=$program;WorkingDirectory=$taskRoot;WindowStyle='Hidden';PassThru=$true;RedirectStandardOutput=$stdout;RedirectStandardError=$stderr}
 if($arguments.Count -gt 0){$start.ArgumentList=$arguments}
 $process=Start-Process @start
 try{
  # Retain this exact started process handle through wait and any termination.
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
 $librarySource=Workspace-Path (@($manifest.sources|Where-Object role -EQ 'library')[0].path)
 Run-Step 'compile-new-navigation-search-wrapper' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@($librarySource))
 $report.compiles=1
 $testSources=@();foreach($source in @($manifest.sources|Where-Object role -NE 'library')){$testSources+=Workspace-Path $source.path}
 Run-Step 'compile-new-navigation-search-wrapper-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:NavigationSearchWrapperTests',"/out:$probe","/reference:$jsonAssembly","/reference:$library")+$references+$testSources)
 $report.compiles=2
 Run-Step 'new-navigation-search-wrapper' $probe @()
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-navigation-search-wrapper.log') -Tail 1|ConvertFrom-Json
 if($summary.status -ne 'passed-new-navigation-search-wrapper-checks' -or $summary.scenarios -ne 11 -or $summary.assertions -ne 113 -or @($summary.samples).Count -ne 2){throw 'Unexpected new wrapper summary'}
 if($summary.own_initialization -ne $true){throw 'Explicit OWN initialization must be reported'}
 foreach($name in @('native_loaded_state','injected_success','old_suite_main_called','original_read','original_executed')){if($summary.$name -ne $false){throw ('Probe exceeded declared boundary: '+$name)}}
 $endpoint=$summary.samples[0];$ground=$summary.samples[1]
 if($endpoint.name -ne 'saved-map-endpoints' -or $endpoint.nearest_from -eq 0 -or $endpoint.nearest_to -eq 0 -or $endpoint.corridor -lt 2 -or $endpoint.waypoints -lt 2 -or $endpoint.length -le 0 -or @($endpoint.output).Count -ne 3){throw 'Actual nontrivial endpoint route is required'}
 if($ground.name -ne 'saved-ground-destination' -or ($ground.result -ne 0 -and $ground.result -ne 1) -or @($ground.requested).Count -ne 3 -or [Math]::Abs([double]$ground.requested[1]-16.05875) -gt .0002){throw 'Actual saved Ground destination result required'}
 if($ground.result -eq 0 -and $null -ne $ground.output){throw 'A failed Ground destination must not invent output'}
 if($ground.result -eq 1 -and ($ground.end_reference -eq 0 -or @($ground.output).Count -ne 3)){throw 'A successful Ground destination needs actual reference and output'}
 foreach($sample in $summary.samples){if(@($sample.second_extent).Count -ne 3 -or $sample.second_extent[0] -ne 10 -or $sample.second_extent[1] -ne 3 -or $sample.second_extent[2] -ne 10){throw 'Native second extent capture changed'}}
 if(@($endpoint.first_extent).Count -ne 3 -or [Math]::Abs([double]$endpoint.first_extent[0]-.05) -gt .00000001 -or $endpoint.first_extent[1] -ne 3 -or [Math]::Abs([double]$endpoint.first_extent[2]-.05) -gt .00000001){throw 'Explicit constructor scalar extent changed'}
 $report.test_results += [ordered]@{name='new-navigation-search-wrapper';kind='new';scenarios=$summary.scenarios;assertions=$summary.assertions}
 $report.samples=$summary.samples
 Check-Inputs;foreach($dependency in $manifest.dependencies){Check-File (Join-Path $output $dependency.name) $dependency.sha256}
 $report.status='passed-new-navigation-search-wrapper-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;test_results=$report.test_results;samples=$report.samples;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 8
