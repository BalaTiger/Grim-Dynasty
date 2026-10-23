param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$previous=Join-Path $taskRoot '.tooling\r1-world-publication-prerequisites-validation'
$output=Join-Path $taskRoot '.tooling\r1-ground-geometry-validation'
$priorResult=Join-Path $previous 'result.json'
$priorResultHash='43da143e7dca57b18fffa4316797c734a809d2a223a91295fb8b95d9a6855c42'
$sources=@{
 'GroundGeometryContracts.cs'='e53a96997d19c9f4823ba1609c33e7e052b70996ef22c497957b9ef3f60d78c1'
 'GroundGeometryTests.cs'='cacf5e3a2a9e22f1ab43c3b4bc2f724d5f056d4f8669388a30ed54fef62d4433'
}
$dependencies=@{
 'GrimDynasty.Actions.dll'='3ea34074db9d5a67d7dffb702eb053b703dc3b754d0db442aeadd71247e15c58'
 'GrimDynasty.MoveManager.dll'='7563b6e32745e79ddc8308d2ea48ca7336050271d8dc8c9ce1e75346a52232da'
 'GrimDynasty.WorldCoordinates.dll'='6365ad6a051d6ffbb4009824d98fa899bec89f5933dea383a9a3d98f193e71c7'
 'GrimDynasty.NavigationNearest.dll'='75f303c482679df6ba2c8f01aad7759ba9ed322ad6386100525b690cd9b9dfd0'
 'GrimDynasty.EntityMovement.dll'='85d848625fce7d5fc580feb04839898bd580a96f0c48308c3aca5b8ab142f84d'
}
$evidence=@{
 'docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin'='a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289'
 'docs\reverse-client\r1-mainmenu-ground-layout\inspection.json'='64b6321a8a1e3a7c0fa0648cfd6aa3ad7e2ce65e5cfd47c094b3a40ad22335a6'
 'docs\reverse-client\r1-spatial-ground-direct\11-SpatialGroundDirect-0x1801de090-procedure_assembly.json'='280b8fc0515859b8de565e69e9ee40dcb047d76631ca4c9e368d3177faa105ff'
 'docs\reverse-client\r1-ground-ray-tree\14-GroundRayTree-0x1801de880-procedure_assembly.json'='c425b96739e4702a4f622b0fbdf3730b0e2c6a61730cc78df8cf7299db85f72f'
 'docs\reverse-client\r1-ground-floor-geometry\08-GroundFloorGeometry-0x1801d0fc0-procedure_assembly.json'='68aa0e88dbeb9f18b6dc5b2d644affe8e8fee7848ac8bb9674e82b7783b2ff07'
 'docs\reverse-client\r1-ground-floor-geometry\11-GroundFloorGeometry-0x1801d0e20-procedure_assembly.json'='45b7816c25261a5cfd6b313cc67031527dfe22416b015723bdd8912e8c2e0b7b'
 'docs\reverse-client\r1-ground-blocks-and-node-destroy\14-GroundBlocksNodeDestroy-0x1801ce910-procedure_assembly.json'='c409e0f20684d60e076ed27861ebeece894cfdcc37808c4eb4c4b32a05f98dcf'
 'docs\reverse-client\r1-ground-geometry-bindings\inspection.json'='8be1f21796126e33fae05d412b4690b8d7cf5618bda89593d4f6021230621d17'
 'docs\reverse-client\r1-navigation-compressor-slot\inspection.json'='191e63e5cc34d9b1abf1326426827a27041743913768a3159e73af3dc62126de'
}
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
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace, no arguments required'}
Check-Ancestors $output
if(!([IO.Path]::GetFullPath($output)).StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Output must stay inside fixed workspace'}
if(Test-Path -LiteralPath $output){throw 'Existing ground evidence preserved; output must be new'}
Check-File $priorResult $priorResultHash
$prior=Get-Content -LiteralPath $priorResult -Raw|ConvertFrom-Json
if($prior.status -ne 'passed-new-world-publication-prerequisites-only' -or !$prior.own_preloaded_prerequisites -or $null -ne $prior.failure){throw 'Passed dependency result is required'}
foreach($name in $dependencies.Keys){$record=@($prior.artifacts|Where-Object {$_.name -eq $name});if($record.Count -ne 1 -or $record[0].sha256 -ne $dependencies[$name]){throw ('Dependency not pinned by passed result: '+$name)}}
function Check-Inputs {
 Check-File $compiler $compilerHash;Check-File $priorResult $priorResultHash
 foreach($name in $sources.Keys){Check-File (Join-Path $taskRoot ('src\reverse_client_actions\'+$name)) $sources[$name]}
 foreach($name in $dependencies.Keys){Check-File (Join-Path $previous $name) $dependencies[$name]}
 foreach($name in $evidence.Keys){Check-File (Join-Path $taskRoot $name) $evidence[$name]}
}
Check-Inputs
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash
 source_pins=$sources;dependency_pins=$dependencies;native_and_saved_evidence_pins=$evidence;prior_result=$priorResult;prior_result_sha256=$priorResultHash
 steps=@();test_results=@();artifacts=@();old_test_suites_rerun=@();old_executable_entrypoints_invoked=@();failure=$null
 saved_ground_pipeline=$false;native_load=$false;full_construct_blocks=$false;floor_collision=$false;material_resources_loaded=$false
 original_read=$false;original_executed=$false;backend_started=$false;gameplay_accepted=$false
 support_boundary='OWN bounded type2 cold-data adapter from the saved 129x129 member, explicit disk Terrain slot100 selection, Engine1DE880 finite scalar sampling and Engine1D0FC0 bounds with1CE910 block descriptor dimensions. Same owned data identity and per-operation binary32. Header unknowns retained; materials and gEngine+640 unresolved. OWN extent and finite-domain stops are not native failure contracts. No native Load graphics callbacks, slot48 completion, spatial publication, ray intersection or gameplay acceptance.'
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
 foreach($name in $dependencies.Keys){Copy-Item -LiteralPath (Join-Path $previous $name) -Destination (Join-Path $output $name);Check-File (Join-Path $output $name) $dependencies[$name]}
 $references=@();foreach($name in ($dependencies.Keys|Sort-Object)){$references+='/reference:'+(Join-Path $output $name)}
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$library=Join-Path $output 'GrimDynasty.GroundGeometry.dll';$tests=Join-Path $output 'GroundGeometryTests.exe'
 Run-Step 'compile-ground-geometry' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@((Join-Path $source 'GroundGeometryContracts.cs')))
 Run-Step 'compile-ground-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:GroundGeometryTestProgram',"/out:$tests","/reference:$library")+$references+@((Join-Path $source 'GroundGeometryTests.cs')))
 Run-Step 'new-ground-tests' $tests @((Join-Path $taskRoot 'docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin'))
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-ground-tests.log') -Raw|ConvertFrom-Json
 if($summary.status -ne 'passed' -or $summary.scenarios -ne 8 -or $summary.assertions -le 0 -or $summary.saved_ground_pipeline -ne $true -or $summary.native_load -ne $false -or $summary.full_construct_blocks -ne $false -or $summary.floor_collision -ne $false -or $summary.gameplay_accepted -ne $false){throw 'Missing or unexpected ground summary'}
 $report.test_results += [ordered]@{name='saved-ground-sample-bounds';kind='new';scenarios=$summary.scenarios;assertions=$summary.assertions}
 Check-Inputs;foreach($name in $dependencies.Keys){Check-File (Join-Path $output $name) $dependencies[$name]}
 $report.saved_ground_pipeline=$true;$report.status='passed-new-saved-ground-geometry-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 9|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;steps=$report.steps;test_results=$report.test_results;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 6
