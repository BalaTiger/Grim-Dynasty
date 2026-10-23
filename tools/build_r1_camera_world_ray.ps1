param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$jsonAssembly='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Web.Extensions.dll'
$jsonAssemblyHash='5e6378edc06f5a61920aaadb2aba839dd3cf302d6192e08aacd4454ba4cff0b3'
$systemCrt='C:\Windows\System32\ucrtbase.dll'
$systemCrtHash='dedb7611e1abc1ff3852dcac3d4c93a048d729e25b061973b9969f22698f14ca'
$output=Join-Path $taskRoot '.tooling\r1-camera-world-ray-validation'
$manifestPath=Join-Path $taskRoot 'tools\r1_camera_world_ray_build_fingerprints.json'
$manifestHash='41234804bec444bb14f6da1e32e339248c667ff8846bbddc7e7829aa696742e1'

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
function Artifact-Name($record){
 if($null -ne $record.name -and [string]$record.name -ne ''){return [string]$record.name}
 if($null -ne $record.path -and [string]$record.path -ne ''){return [IO.Path]::GetFileName([string]$record.path)}
 return ''
}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace and no arguments required'}
Check-Ancestors $output
if(!([IO.Path]::GetFullPath($output)).StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Output must stay in fixed workspace'}
if(Test-Path -LiteralPath $output){throw 'Existing Camera/WorldRay evidence preserved; output must be new'}
Check-File $manifestPath $manifestHash
$manifest=Get-Content -LiteralPath $manifestPath -Raw|ConvertFrom-Json
if($manifest.schema -ne 'grim-dynasty.camera-world-ray-build.v1' -or $manifest.source_review_ready -ne $true){throw 'Reviewed final Camera/WorldRay source manifest required'}
if(@($manifest.sources).Count -ne 6 -or @($manifest.prior_results).Count -ne 3 -or @($manifest.dependencies).Count -ne 16){throw 'Fixed six-source and sixteen-dependency plan required'}
if(@($manifest.dependencies.name|Sort-Object -Unique).Count -ne 16){throw 'Dependency DLL names must be unique'}
foreach($dependency in $manifest.dependencies){
 if($dependency.name -ne [IO.Path]::GetFileName($dependency.name) -or $dependency.name -eq 'GrimDynasty.SystemUcrtRotationMath.dll'){throw 'Dependency names cannot redirect output or copy the old UCRT provider'}
}
foreach($prior in $manifest.prior_results){
 $path=Workspace-Path $prior.path;Check-File $path $prior.sha256;$result=Get-Content -LiteralPath $path -Raw|ConvertFrom-Json
 if($result.status -ne $prior.expected_status -or $null -ne $result.failure){throw ('Passed prior result required: '+$prior.path)}
 foreach($dependency in @($manifest.dependencies|Where-Object prior_result -EQ $prior.path)){
  # Older CameraTarget receipts use absolute path; newer receipts use name.
  $record=@($result.artifacts|Where-Object {(Artifact-Name $_) -eq $dependency.name})
  if($record.Count -ne 1 -or $record[0].sha256 -ne $dependency.sha256){throw ('Dependency not uniquely pinned by its passed result: '+$dependency.name)}
 }
}
function Check-Inputs {
 Check-Ancestors $output;Check-File $compiler $compilerHash;Check-File $jsonAssembly $jsonAssemblyHash;Check-File $manifestPath $manifestHash
 Check-File $systemCrt $systemCrtHash;$crt=Get-Item -LiteralPath $systemCrt
 if($manifest.platform.path -ne $systemCrt -or $manifest.platform.sha256 -ne $systemCrtHash -or $crt.Length -ne 1399656 -or $crt.VersionInfo.FileVersion -ne '10.0.26100.9711 (WinBuild.160101.0800)'){throw 'Fixed own system UCRT profile changed'}
 foreach($record in $manifest.prior_results){Check-File (Workspace-Path $record.path) $record.sha256}
 foreach($record in $manifest.sources){Check-File (Workspace-Path $record.path) $record.sha256}
 foreach($record in $manifest.dependencies){Check-File (Workspace-Path $record.path) $record.sha256}
 foreach($record in $manifest.evidence){Check-File (Workspace-Path $record.path) $record.sha256}
}
Check-Inputs
$tangent=Get-Content -LiteralPath (Workspace-Path 'docs/reverse-client/r1-engine-camera-tanf-import/inspection.json') -Raw|ConvertFrom-Json
if($tangent.status -ne 'passed' -or !$tangent.disk_bindings_complete -or @($tangent.bindings).Count -ne 1 -or $tangent.code_bytes -ne 6 -or $tangent.metadata_bytes -ne 74){throw 'Exact passed Camera tangent binding is required'}
$binding=$tangent.bindings[0]
if($binding.thunk_rva -ne '0x2ee09c' -or $binding.iat_rva -ne '0x302a18' -or $binding.descriptor_index -ne 24 -or $binding.index -ne 17 -or $binding.symbol -ne 'tanf' -or $binding.dll -ne 'api-ms-win-crt-math-l1-1-0.dll'){throw 'Camera tangent import identity mismatch'}
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash;json_assembly=$jsonAssembly;json_assembly_sha256=$jsonAssemblyHash
 manifest=$manifestPath;manifest_sha256=$manifestHash;source_pins=$manifest.sources;dependency_pins=$manifest.dependencies;prior_result_pins=$manifest.prior_results;evidence_pins=$manifest.evidence
 system_crt_path=$systemCrt;system_crt_file_sha256=$systemCrtHash;system_crt_file_version='10.0.26100.9711 (WinBuild.160101.0800)'
 steps=@();test_results=@();artifacts=@();old_test_suites_rerun=@();old_executable_entrypoints_invoked=@();failure=$null
 system_library_load_requested=$false;system_library_loaded=$null;native_math_calls_new=$null;native_math_calls_affected_regression=$null
 own_camera_platform=$false;shared_camera_world_ray=$false;actual_saved_ground_ray=$false;rotation_bounds_unchanged=$false
 physical_mouse=$false;native_pick_caller=$false;native_scene_startup=$false;original_read=$false;original_executed=$false;backend_started=$false;gameplay_accepted=$false
 memory_image_hashed=$false;original_runtime_equivalence=$false;floating_control_write_mask=0;security_settings_changed=$false
 support_boundary='New own Camera044A10/WorldRay229700 shared-state component and actual saved Ground ray integration. Concrete GameGetTarget is reused; explicit own camera/viewport/region and finite arithmetic/tanf domains. System32 tangent is an own platform profile, not original-runtime equivalence. Only eleven affected rotation-loader scenarios are recompiled and rerun; old Node9 and Crowd suites remain untouched. Physical events, native pick caller, scene startup and gameplay are unclosed.'
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
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$provider=Join-Path $output 'GrimDynasty.SystemUcrtRotationMath.dll';$camera=Join-Path $output 'GrimDynasty.CameraWorldRay.dll'
 $providerTests=Join-Path $output 'SystemUcrtCameraMathTests.exe';$cameraTests=Join-Path $output 'CameraWorldRayTests.exe';$groundTests=Join-Path $output 'CameraGroundRayIntegrationTests.exe';$oldTests=Join-Path $output 'AffectedSystemUcrtRotationMathTests.exe'
 $actionsReference='/reference:'+(Join-Path $output 'GrimDynasty.Actions.dll')
 Run-Step 'compile-system-ucrt-camera-provider' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$provider",$actionsReference)+@((Join-Path $source 'SystemUcrtRotationMath.cs')))
 Run-Step 'compile-camera-world-ray-contracts' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$camera","/reference:$provider")+$references+@((Join-Path $source 'CameraWorldRayContracts.cs')))
 Run-Step 'compile-new-camera-provider-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:SystemUcrtCameraMathTests',"/out:$providerTests","/reference:$provider","/reference:$jsonAssembly",$actionsReference)+@((Join-Path $source 'SystemUcrtCameraMathTests.cs')))
 Run-Step 'compile-new-camera-world-ray-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:CameraWorldRayTestProgram',"/out:$cameraTests","/reference:$provider","/reference:$camera")+$references+@((Join-Path $source 'CameraWorldRayTests.cs')))
 Run-Step 'compile-new-camera-ground-ray-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:CameraGroundRayIntegrationTestProgram',"/out:$groundTests","/reference:$provider","/reference:$camera","/reference:$jsonAssembly")+$references+@((Join-Path $source 'CameraGroundRayIntegrationTests.cs')))
 Run-Step 'compile-affected-rotation-loader-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:SystemUcrtRotationMathTests',"/out:$oldTests","/reference:$provider","/reference:$jsonAssembly",$actionsReference)+@((Join-Path $source 'SystemUcrtRotationMathTests.cs')))
 $report.system_library_load_requested=$true
 Run-Step 'new-camera-provider-tests' $providerTests @()
 $new=Get-Content -LiteralPath (Join-Path $output 'new-camera-provider-tests.log') -Tail 1|ConvertFrom-Json
 if($new.status -ne 'passed' -or $new.scenarios -ne 7 -or $new.assertions -ne 55 -or $new.native_math_calls -ne 8 -or !$new.own_camera_platform -or !$new.rotation_bounds_unchanged -or !$new.single_loader -or !$new.default_exports_unchanged -or $new.library_sha256 -ne $systemCrtHash){throw 'Unexpected new Camera provider summary'}
 if($new.control_mask_written -ne 0 -or $new.control_before -ne $new.control_after -or $new.memory_image_hashed -ne $false -or $new.original_read -ne $false -or $new.original_executed -ne $false -or $new.original_runtime_equivalence -ne $false -or $new.old_suites_rerun -ne $false){throw 'Camera provider fixture exceeded declared boundary'}
 $report.test_results += [ordered]@{name='new-camera-provider-tests';kind='new';scenarios=$new.scenarios;assertions=$new.assertions;native_math_calls=$new.native_math_calls;exports=$new.exports}
 $report.system_library_loaded=$true;$report.own_camera_platform=$true
 Run-Step 'new-camera-world-ray-tests' $cameraTests @()
 $cameraSummaries=[regex]::Matches((Get-Content -LiteralPath (Join-Path $output 'new-camera-world-ray-tests.log') -Raw),'(?m)^PASS (\d+) scenarios, (\d+) assertions; new shared camera image and ray contracts only; native_tanf_calls=(\d+)\s*$')
 if($cameraSummaries.Count -ne 1 -or [int]$cameraSummaries[0].Groups[1].Value -ne 14 -or [int]$cameraSummaries[0].Groups[2].Value -ne 143 -or [int]$cameraSummaries[0].Groups[3].Value -ne 3){throw 'Unexpected Camera/WorldRay summary'}
 $report.test_results += [ordered]@{name='new-camera-world-ray-tests';kind='new';scenarios=[int]$cameraSummaries[0].Groups[1].Value;assertions=[int]$cameraSummaries[0].Groups[2].Value;native_math_calls=[int]$cameraSummaries[0].Groups[3].Value}
 $report.shared_camera_world_ray=$true
 Run-Step 'new-camera-ground-ray-tests' $groundTests @($taskRoot)
 $ground=Get-Content -LiteralPath (Join-Path $output 'new-camera-ground-ray-tests.log') -Tail 1|ConvertFrom-Json
 if($ground.status -ne 'passed-new-camera-ground-integration' -or $ground.scenarios -ne 2 -or $ground.assertions -ne 58 -or $ground.native_tangent_calls -ne 0){throw 'Unexpected saved Ground camera-ray summary'}
 foreach($name in @('actual_game_camera_getters','actual_image_projection','actual_ray_normalization','saved_ground_material_tree','short_distance_miss','explicit_own_camera_and_same_region_query')){if($ground.$name -ne $true){throw ('Missing actual camera-ground result: '+$name)}}
 foreach($name in @('physical_mouse','native_pick_caller','native_scene_startup','original_read','original_executed','gameplay_accepted')){if($ground.$name -ne $false){throw ('Ground camera fixture exceeded scope: '+$name)}}
 foreach($name in @('camera_y','hit_y','hit_distance','triangle_oracle')){if($null -eq $ground.$name -or [Double]::IsNaN([double]$ground.$name) -or [Double]::IsInfinity([double]$ground.$name)){throw ('Missing finite Ground observation: '+$name)}}
 if([double]$ground.hit_distance -le 0 -or [Math]::Abs([double]$ground.hit_y-[double]$ground.triangle_oracle) -ge .0002 -or [Math]::Abs([double]$ground.hit_distance-([double]$ground.camera_y-[double]$ground.triangle_oracle)) -ge .0002){throw 'Ground hit does not match its independently decoded triangle oracle'}
 $report.test_results += [ordered]@{name='new-camera-ground-ray-tests';kind='new';scenarios=$ground.scenarios;assertions=$ground.assertions;native_math_calls=$ground.native_tangent_calls;observations=$ground}
 $report.actual_saved_ground_ray=$true;$report.native_math_calls_new=$new.native_math_calls+[int]$cameraSummaries[0].Groups[3].Value+$ground.native_tangent_calls
 $report.old_test_suites_rerun += 'SystemUcrtRotationMathTests: eleven affected shared-loader regression scenarios only, freshly compiled against new provider'
 Run-Step 'affected-rotation-loader-tests' $oldTests @()
 $regression=Get-Content -LiteralPath (Join-Path $output 'affected-rotation-loader-tests.log') -Tail 1|ConvertFrom-Json
 if($regression.status -ne 'passed' -or $regression.scenarios -ne 11 -or $regression.assertions -ne 96 -or $regression.native_math_calls -ne 33 -or !$regression.own_platform_adapter -or $regression.library_sha256 -ne $systemCrtHash){throw 'Unexpected affected rotation regression summary'}
 if($regression.control_mask_written -ne 0 -or $regression.control_before -ne $regression.control_after -or $regression.memory_image_hashed -ne $false -or $regression.original_read -ne $false -or $regression.original_executed -ne $false -or $regression.native_game_equivalence -ne $false){throw 'Affected regression exceeded declared boundary'}
 $report.test_results += [ordered]@{name='affected-rotation-loader-tests';kind='affected-regression';scenarios=$regression.scenarios;assertions=$regression.assertions;native_math_calls=$regression.native_math_calls}
 $report.native_math_calls_affected_regression=$regression.native_math_calls;$report.rotation_bounds_unchanged=$true
 Check-Inputs;foreach($dependency in $manifest.dependencies){Check-File (Join-Path $output $dependency.name) $dependency.sha256}
 $report.status='passed-new-camera-world-ray-ground-and-affected-rotation-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;test_results=$report.test_results;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 8
