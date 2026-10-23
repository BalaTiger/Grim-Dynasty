param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$jsonAssembly='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Web.Extensions.dll'
$jsonAssemblyHash='5e6378edc06f5a61920aaadb2aba839dd3cf302d6192e08aacd4454ba4cff0b3'
$output=Join-Path $taskRoot '.tooling\r1-real-ground-floor-integration-validation'
$manifestPath=Join-Path $taskRoot 'tools\r1_real_ground_floor_integration_build_fingerprints.json'
$manifestHash='8889c158b70181b1539ec33dc1a25c6a00dc305f858daa8a2ea79b9601546443'

function Hash([string]$path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-Ancestors([string]$path){
 $cursor=[IO.Path]::GetFullPath($path)
 while($cursor){
  if(Test-Path -LiteralPath $cursor){$item=Get-Item -LiteralPath $cursor;if($item.Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Reparse path not permitted: '+$cursor)}}
  $parent=[IO.Directory]::GetParent($cursor);if($null -eq $parent){break};$cursor=$parent.FullName
 }
}
function Check-File([string]$path,[string]$expected){
 Check-Ancestors $path;$item=Get-Item -LiteralPath $path
 if($expected -notmatch '^[0-9a-f]{64}$' -or $item.PSIsContainer -or (Hash $path) -ne $expected){throw ('Input identity changed: '+$path)}
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
if(Test-Path -LiteralPath $output){throw 'Existing real Ground/Floor integration evidence preserved; output must be new'}
Check-File $manifestPath $manifestHash
$manifest=Get-Content -LiteralPath $manifestPath -Raw|ConvertFrom-Json
if($manifest.schema -ne 'grim-dynasty.real-ground-floor-integration-build.v1' -or $manifest.source_review_ready -ne $true){throw 'Reviewed integration source manifest required'}
if(@($manifest.sources).Count -ne 1 -or @($manifest.prior_results).Count -ne 2 -or @($manifest.dependencies).Count -ne 14){throw 'Fixed one-source and fourteen-dependency plan required'}
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
 foreach($record in $manifest.prior_results){Check-File (Workspace-Path $record.path) $record.sha256}
 foreach($record in $manifest.sources){Check-File (Workspace-Path $record.path) $record.sha256}
 foreach($record in $manifest.dependencies){Check-File (Workspace-Path $record.path) $record.sha256}
 foreach($record in $manifest.evidence){Check-File (Workspace-Path $record.path) $record.sha256}
}
Check-Inputs
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash;json_assembly=$jsonAssembly;json_assembly_sha256=$jsonAssemblyHash
 manifest=$manifestPath;manifest_sha256=$manifestHash;source_pins=$manifest.sources;dependency_pins=$manifest.dependencies;prior_result_pins=$manifest.prior_results;evidence_pins=$manifest.evidence
 steps=@();test_results=@();artifacts=@();old_test_suites_rerun=@();old_executable_entrypoints_invoked=@();failure=$null
 saved_ground_floor_in_own_scene=$false;same_material_identity=$false;explicit_own_actor_neighbor_state=$false
 nav_height_used=$false;native_scene_startup=$false;native_water_constructor_initialize=$false;real_crowd_game_wired=$false;old_movement_wrapper_wired=$false
 original_read=$false;original_executed=$false;backend_started=$false;gameplay_accepted=$false
 support_boundary='Six new integration scenarios use saved129x129 Ground, same five live material views, actual shared Tree/Block/Floor and saved empty Water under explicitly OWN prerequisites. Actors and neighbors require explicit OWN publication; unknown state still stops. Caller copies and geometric triangle oracle are checked. No native cold scene, constructor/Initialize, navigation-height substitute, Crowd-to-Game or production movement wiring. Passed Floor21/material8 suites are not rerun.'
}
New-Item -ItemType Directory -Path $output|Out-Null
function Run-Step([string]$name,[string]$program,[string[]]$arguments){
 $stdout=Join-Path $output ($name+'.log');$stderr=Join-Path $output ($name+'.stderr.log');$oldPreference=$ErrorActionPreference
 try{$ErrorActionPreference='Continue'; & $program @arguments 1> $stdout 2> $stderr; $invocationSucceeded=$?;$code=$LASTEXITCODE}finally{$ErrorActionPreference=$oldPreference}
 $report.steps += [ordered]@{name=$name;program=$program;arguments=$arguments;invocation_succeeded=$invocationSucceeded;exit_code=$code}
 if(!$invocationSucceeded -or $code -ne 0){throw "Step $name failed (invocation=$invocationSucceeded, exit=$code); logs retained"}
 if((Get-Item -LiteralPath $stderr).Length -ne 0 -or ($name.StartsWith('compile-') -and (Get-Item -LiteralPath $stdout).Length -ne 0)){throw ('Unexpected diagnostics: '+$name)}
}
try{
 foreach($dependency in $manifest.dependencies){Copy-Item -LiteralPath (Workspace-Path $dependency.path) -Destination (Join-Path $output $dependency.name);Check-File (Join-Path $output $dependency.name) $dependency.sha256}
 $references=@();foreach($dependency in @($manifest.dependencies|Sort-Object name)){$references+='/reference:'+(Join-Path $output $dependency.name)}
 $source=Join-Path $taskRoot 'src\reverse_client_actions\RealGroundFloorIntegrationTests.cs';$tests=Join-Path $output 'RealGroundFloorIntegrationTests.exe'
 # Only the new integration executable is compiled. Both implementations are
 # pinned passed DLLs; no build server, restore or old test executable runs.
 Run-Step 'compile-real-ground-floor-integration-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:RealGroundFloorIntegrationTestProgram',"/out:$tests","/reference:$jsonAssembly")+$references+@($source))
 Run-Step 'new-real-ground-floor-integration-tests' $tests @($taskRoot)
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-real-ground-floor-integration-tests.log') -Raw|ConvertFrom-Json
 if($summary.status -ne 'passed' -or $summary.scenarios -ne 6 -or $summary.assertions -le 0 -or $summary.real_ground_cells -ne 16641 -or $summary.material_records -ne 5 -or $summary.saved_member_sha256 -ne 'a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289'){throw 'Missing or unexpected integration summary'}
 foreach($name in @('saved_ground_floor_in_own_scene','same_material_identity','explicit_own_actor_neighbor_state')){if($summary.$name -ne $true){throw ('Required integration result missing: '+$name)};$report[$name]=$true}
 foreach($name in @('nav_height_used','native_scene_startup','native_water_constructor_initialize','real_crowd_game_wired','old_movement_wrapper_wired','original_read','original_executed','gameplay_accepted')){if($summary.$name -ne $false){throw ('Integration scope exceeded: '+$name)}}
 if($summary.unknown_actor_stop -ne '13028a' -or $summary.unknown_neighbor_stop -ne '221f2e' -or $summary.live_material_stop -ne '1d1528'){throw 'Missing precise integrated Unknown stops'}
 if([Math]::Abs([double]$summary.floor_y-16.0587477684021) -ge .0002 -or [Math]::Abs([double]$summary.collision_distance-15.9412522315979) -ge .0002){throw 'Actual Floor result differs from saved geometric triangle oracle'}
 $report.test_results += $summary
 Check-Inputs;foreach($dependency in $manifest.dependencies){Check-File (Join-Path $output $dependency.name) $dependency.sha256}
 $report.status='passed-new-real-ground-floor-integration-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;test_results=$report.test_results;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 6
