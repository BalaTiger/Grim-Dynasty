param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$output=Join-Path $taskRoot '.tooling\r1-world-floor-validation'
$manifestPath=Join-Path $taskRoot 'tools\r1_world_floor_build_fingerprints.json'
$manifestHash='7f1d1cfd6700bdbf3064c847fbc0f080b0540942847c8dc10226ac8a91988711'

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
if(Test-Path -LiteralPath $output){throw 'Existing Floor evidence preserved; output must be new'}
Check-File $manifestPath $manifestHash
$manifest=Get-Content -LiteralPath $manifestPath -Raw|ConvertFrom-Json
if($manifest.schema -ne 'grim-dynasty.world-floor-build.v1' -or $manifest.source_review_ready -ne $true){throw 'Reviewed Floor source manifest required'}
if(@($manifest.sources).Count -ne 2 -or @($manifest.prior_results).Count -ne 3 -or @($manifest.dependencies).Count -ne 12){throw 'Fixed two-source and twelve-dependency plan required'}
foreach($prior in $manifest.prior_results){
 $path=Workspace-Path $prior.path;Check-File $path $prior.sha256;$result=Get-Content -LiteralPath $path -Raw|ConvertFrom-Json
 if($result.status -ne $prior.expected_status -or $null -ne $result.failure){throw ('Passed prior result required: '+$prior.path)}
 foreach($dependency in @($manifest.dependencies|Where-Object prior_result -EQ $prior.path)){
  $record=@($result.artifacts|Where-Object name -EQ $dependency.name)
  if($record.Count -ne 1 -or $record[0].sha256 -ne $dependency.sha256){throw ('Dependency not uniquely pinned by its passed result: '+$dependency.name)}
 }
}
function Check-Inputs {
 Check-Ancestors $output;Check-File $compiler $compilerHash;Check-File $manifestPath $manifestHash
 foreach($record in $manifest.prior_results){Check-File (Workspace-Path $record.path) $record.sha256}
 foreach($record in $manifest.sources){Check-File (Workspace-Path $record.path) $record.sha256}
 foreach($record in $manifest.dependencies){Check-File (Workspace-Path $record.path) $record.sha256}
 foreach($record in $manifest.evidence){Check-File (Workspace-Path $record.path) $record.sha256}
}
Check-Inputs
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash
 manifest=$manifestPath;manifest_sha256=$manifestHash;source_pins=$manifest.sources;dependency_pins=$manifest.dependencies;prior_result_pins=$manifest.prior_results;evidence_pins=$manifest.evidence
 steps=@();test_results=@();artifacts=@();old_test_suites_rerun=@();old_executable_entrypoints_invoked=@();failure=$null
 real_saved_ground_unknown_material_stop=$false;explicitly_own_established_true=$false;own_wrapper_true=$false
 real_material_binding_exercised=$false;real_map_floor_success=$false;native_load=$false;native_water_constructor_initialize=$false;old_movement_wrapper_wired=$false
 original_read=$false;original_executed=$false;backend_started=$false;gameplay_accepted=$false
 support_boundary='New Floor composition only: saved Ground must stop on Unknown material separately from explicit OWN flat Ground true. Shared canonical region/Level, real managed lock, real Tree/Block and saved empty Water payload under OWN opaque prerequisites. Actor/neighbor contents are explicit; eligible geometry and nonnull fallback remain Unsupported. No new material adapter, native Load/Initialize, old movement caller integration, old-suite rerun or gameplay claim.'
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
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$library=Join-Path $output 'GrimDynasty.WorldFloor.dll';$tests=Join-Path $output 'WorldFloorTests.exe'
 # Two sequential Framework csc invocations; no build server, restore, broad
 # project build, parallel compiler, original execution or analysis backend.
 Run-Step 'compile-world-floor' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@((Join-Path $source 'WorldFloorContracts.cs')))
 Run-Step 'compile-world-floor-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:WorldFloorTestProgram',"/out:$tests","/reference:$library")+$references+@((Join-Path $source 'WorldFloorTests.cs')))
 Run-Step 'new-world-floor-tests' $tests @((Join-Path $taskRoot 'docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin'))
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-world-floor-tests.log') -Raw|ConvertFrom-Json
 if($summary.status -ne 'passed' -or $summary.scenarios -ne 21 -or $summary.assertions -le 0 -or $summary.saved_member_sha256 -ne 'a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289'){throw 'Missing or unexpected Floor summary'}
 foreach($name in @('real_saved_ground_unknown_material_stop','explicitly_own_established_true','own_wrapper_true')){if($summary.$name -ne $true){throw ('Required distinct Floor result missing: '+$name)};$report[$name]=$true}
 foreach($name in @('real_material_binding_exercised','real_map_floor_success','native_load','native_water_constructor_initialize','old_movement_wrapper_wired','gameplay_accepted')){if($summary.$name -ne $false){throw ('Floor scope exceeded: '+$name)}}
 $report.test_results += $summary
 Check-Inputs;foreach($dependency in $manifest.dependencies){Check-File (Join-Path $output $dependency.name) $dependency.sha256}
 $report.status='passed-new-world-floor-own-composition-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;test_results=$report.test_results;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 6
