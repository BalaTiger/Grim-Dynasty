param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$output=Join-Path $taskRoot '.tooling\r1-world-same-region-publication-validation'
$sources=@{
 'WorldSameRegionPublicationContracts.cs'='ed0159326ea1e730c7527388858533526536660ff5ae380b835d1a4acae0d576'
 'WorldSameRegionPublicationTests.cs'='705c05f3c68507f554c67867a7c49374fbd1b7268efd9724f1622f32fafcf805'
}
$priorSpecs=@(
 [pscustomobject]@{directory='.tooling\r1-actor-bbox-canonical-dispatch-validation';sha256='4df9bb764c0dce64cd343cc0bd233420c0fd81fe9be3c5f0d3581a1c2aea4946';status='passed-canonical-actor-bounds-integration-and-affected-regression-only';names=@('GrimDynasty.Actions.dll','GrimDynasty.ActorBoundingBox.dll','GrimDynasty.EntityMovement.dll','GrimDynasty.GameMovementCallbacks.dll','GrimDynasty.MoveManager.dll','GrimDynasty.NavigationCorridor.dll','GrimDynasty.NavigationCrowdAdapter.dll','GrimDynasty.NavigationCrowdState.dll','GrimDynasty.NavigationMeshTiles.dll','GrimDynasty.NavigationNearest.dll','GrimDynasty.NavigationTileData.dll','GrimDynasty.WorldCoordinates.dll')},
 [pscustomobject]@{directory='.tooling\r1-world-publication-prerequisites-validation';sha256='43da143e7dca57b18fffa4316797c734a809d2a223a91295fb8b95d9a6855c42';status='passed-new-world-publication-prerequisites-only';names=@('GrimDynasty.WorldPublicationPrerequisites.dll')},
 [pscustomobject]@{directory='.tooling\r1-world-publication-gates-validation';sha256='ab2a1442c32b05c1255b1eedb31a6a7f9634065495d647e5e9b2e307226afc44';status='passed-new-world-publication-gates-only';names=@('GrimDynasty.WorldPublicationGates.dll')},
 [pscustomobject]@{directory='.tooling\r1-entity-spatial-tree-readpoint-validation';sha256='b7df91525fa9b72cf73751edf1e09b3e2f13442218b124e57d77b7b3d2c12237';status='passed-new-spatial-prune-readpoint-only';names=@('GrimDynasty.EntitySpatialTree.dll')}
)
$evidence=@{
 'docs\reverse-client\r1-position-publication\05-PositionPublication-0x18021bb40-procedure_assembly.json'='424610e3452d712d7a795ca7b4a1307bd269607163e390802deb107e83ef2c86'
 'docs\reverse-client\r1-same-region-movement\08-SameRegionMovement-0x18012f4f0-procedure_assembly.json'='cf6f5a0ed0faface5f8ddf543c58f3681e96a437e6a919670c54a9d9ce9ed01b'
 'docs\reverse-client\r1-level-move-entity-slots\inspection.json'='1eac17c7c6d015175f0b149dfa7587e507dab84b87eedfbfcaa90b81d2201690'
 'docs\reverse-client\r1-entity-publication-slot\inspection.json'='0ab3d46d0ee8d8f45cc784969617872d24a3948bb969b3318dd1327e1696237f'
 'docs\reverse-client\r1-game-level-publication-tail\05-GameLevelPublicationTail-0x180015880-procedure_assembly.json'='ff041d67da4aa7974b5546f7eab5c3293bcec346ce9474d93aae76b3db810ce7'
}
function Hash([string]$path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-Ancestors([string]$path){
 $cursor=[IO.Path]::GetFullPath($path)
 while($cursor){if(Test-Path -LiteralPath $cursor){$item=Get-Item -LiteralPath $cursor;if($item.Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Reparse path not permitted: '+$cursor)}};$parent=[IO.Directory]::GetParent($cursor);if($null -eq $parent){break};$cursor=$parent.FullName}
}
function Check-File([string]$path,[string]$expected){Check-Ancestors $path;$item=Get-Item -LiteralPath $path;if($expected -notmatch '^[0-9a-f]{64}$' -or $item.PSIsContainer -or (Hash $path) -ne $expected){throw ('Input identity changed or unfrozen: '+$path)}}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace, no arguments required'}
Check-Ancestors $output
if(!([IO.Path]::GetFullPath($output)).StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Output must stay inside fixed workspace'}
if(Test-Path -LiteralPath $output){throw 'Preserve existing validation; output must be new'}
# Only these15 named existing DLLs are accepted. Every name/hash must be in a
# pinned passed result; no source rebuild, old EXE or executable enumeration.
$dependencies=@{}
foreach($spec in $priorSpecs){
 $directory=Join-Path $taskRoot $spec.directory;$result=Join-Path $directory 'result.json';Check-File $result $spec.sha256
 $prior=Get-Content -LiteralPath $result -Raw|ConvertFrom-Json;if($prior.status -ne $spec.status -or $null -ne $prior.failure){throw ('Dependency validation not passed: '+$result)}
 foreach($name in $spec.names){
  $record=@($prior.artifacts|Where-Object {$_.name -eq $name});if($record.Count -ne 1 -or $record[0].sha256 -notmatch '^[0-9a-f]{64}$' -or $dependencies.ContainsKey($name)){throw ('Unexpected dependency identity: '+$name)}
  $dependencies.Add($name,[ordered]@{path=(Join-Path $directory $name);sha256=$record[0].sha256})
 }
}
if($dependencies.Count -ne 15){throw 'Unexpected dependency count'}
function Check-Inputs {
 Check-File $compiler $compilerHash
 foreach($spec in $priorSpecs){Check-File (Join-Path (Join-Path $taskRoot $spec.directory) 'result.json') $spec.sha256}
 foreach($name in $sources.Keys){Check-File (Join-Path $taskRoot ('src\reverse_client_actions\'+$name)) $sources[$name]}
 foreach($name in $dependencies.Keys){Check-File $dependencies[$name].path $dependencies[$name].sha256}
 foreach($name in $evidence.Keys){Check-File (Join-Path $taskRoot $name) $evidence[$name]}
}
Check-Inputs
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash
 source_pins=$sources;dependency_pins=$dependencies;native_evidence_pins=$evidence;prior_results=$priorSpecs
 steps=@();test_results=@();artifacts=@();failure=$null;old_test_suites_rerun=@();old_executable_entrypoints_invoked=@()
 own_same_region_publication=$false;real_mesh_production=$false;cross_region=$false;original_read=$false;original_executed=$false;backend_started=$false;gameplay_accepted=$false
 support_boundary='Observed World21BB40 and Level12F4F0 same-region stable own-state composition. Shared canonical Entity primary identity; preloaded Level and own reentrant region lock; actual bounds and spatial production calls; sequential input capture/Unknown/padding/4x16B publication and fixed observed no-op leaves. No callbacks as success substitutes. No native loading/global initialization, real mesh producer, nonempty attached IDs, cross-region deletion/addition, full movement loop or game acceptance.'
}
New-Item -ItemType Directory -Path $output|Out-Null
function Run-Step([string]$name,[string]$program,[string[]]$arguments){
 $stdout=Join-Path $output ($name+'.log');$stderr=Join-Path $output ($name+'.stderr.log');$oldPreference=$ErrorActionPreference
 try{$ErrorActionPreference='Continue'; & $program @arguments 1> $stdout 2> $stderr;$invocationSucceeded=$?;$code=$LASTEXITCODE}finally{$ErrorActionPreference=$oldPreference}
 $report.steps += [ordered]@{name=$name;program=$program;arguments=$arguments;invocation_succeeded=$invocationSucceeded;exit_code=$code}
 if(!$invocationSucceeded -or $code -ne 0){throw "Step $name failed (invocation=$invocationSucceeded, exit=$code); logs retained"}
 if((Get-Item -LiteralPath $stderr).Length -ne 0 -or ($name.StartsWith('compile-') -and (Get-Item -LiteralPath $stdout).Length -ne 0)){throw ('Unexpected diagnostics: '+$name)}
}
try{
 foreach($name in $dependencies.Keys){Copy-Item -LiteralPath $dependencies[$name].path -Destination (Join-Path $output $name);Check-File (Join-Path $output $name) $dependencies[$name].sha256}
 $references=@();foreach($name in ($dependencies.Keys|Sort-Object)){$references+='/reference:'+(Join-Path $output $name)}
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$library=Join-Path $output 'GrimDynasty.WorldSameRegionPublication.dll';$tests=Join-Path $output 'WorldSameRegionPublicationTests.exe'
 Run-Step 'compile-world-same-region-publication' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@((Join-Path $source 'WorldSameRegionPublicationContracts.cs')))
 Run-Step 'compile-same-region-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:WorldSameRegionPublicationTestProgram',"/out:$tests","/reference:$library")+$references+@((Join-Path $source 'WorldSameRegionPublicationTests.cs')))
 Run-Step 'new-same-region-tests' $tests @()
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-same-region-tests.log') -Raw|ConvertFrom-Json
 if($summary.status -ne 'passed' -or $summary.scenarios -ne 20 -or $summary.assertions -le 0 -or $summary.own_same_region_publication -ne $true -or $summary.real_mesh_production -ne $false -or $summary.cross_region -ne $false -or $summary.gameplay_accepted -ne $false){throw 'Missing or unexpected publication summary'}
 $report.test_results += [ordered]@{name='canonical-same-region-bounds-spatial-publication';kind='new';scenarios=$summary.scenarios;assertions=$summary.assertions}
 Check-Inputs;foreach($name in $dependencies.Keys){Check-File (Join-Path $output $name) $dependencies[$name].sha256}
 $report.own_same_region_publication=$true;$report.status='passed-new-world-same-region-publication-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}};$report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;test_results=$report.test_results;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 6
