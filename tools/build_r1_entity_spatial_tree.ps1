param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$previous=Join-Path $taskRoot '.tooling\r1-actor-bbox-canonical-dispatch-validation'
$output=Join-Path $taskRoot '.tooling\r1-entity-spatial-tree-validation'
$priorResult=Join-Path $previous 'result.json'
$priorResultHash='4df9bb764c0dce64cd343cc0bd233420c0fd81fe9be3c5f0d3581a1c2aea4946'
$sources=@{
 'EntitySpatialTreeContracts.cs'='994c094298f91e7ebc4c624de6595dbfe334aa5ab7bda909c36269f255fae443'
 'EntitySpatialTreeTests.cs'='262a69449d08ae9106fcc7ec69585dc129b15c4445eac6a1aca31d856cdf0ee8'
}
$dependencies=@{
 'GrimDynasty.Actions.dll'='3ea34074db9d5a67d7dffb702eb053b703dc3b754d0db442aeadd71247e15c58'
 'GrimDynasty.MoveManager.dll'='7563b6e32745e79ddc8308d2ea48ca7336050271d8dc8c9ce1e75346a52232da'
 'GrimDynasty.WorldCoordinates.dll'='6365ad6a051d6ffbb4009824d98fa899bec89f5933dea383a9a3d98f193e71c7'
 'GrimDynasty.NavigationNearest.dll'='75f303c482679df6ba2c8f01aad7759ba9ed322ad6386100525b690cd9b9dfd0'
 'GrimDynasty.EntityMovement.dll'='85d848625fce7d5fc580feb04839898bd580a96f0c48308c3aca5b8ab142f84d'
 'GrimDynasty.GameMovementCallbacks.dll'='21aa8443153c7a06575f25c32b4f7ef53d73b8d20c0cdc8ccda11c532ee0f5ed'
 'GrimDynasty.ActorBoundingBox.dll'='0d95e34f3a1111bd639f3f4fe915aec0ae2d07779198ac4a6559e5edb1b83d1f'
}
$evidence=@{
 'docs\reverse-client\r1-engine-entity-publication-core\14-EngineEntityPublication-0x1801333e0-procedure_assembly.json'='2db2d6adad1754fbe16912de4373aca737c489299fd5d9d7c781e0235c273f31'
 'docs\reverse-client\r1-engine-spatial-ground\05-EngineSpatialGround-0x180134500-procedure_assembly.json'='6dfe2938844693c4bffecd4c9340076fc0278c28c195bc6f4bfa669eddecf0fb'
 'docs\reverse-client\r1-engine-spatial-ground\08-EngineSpatialGround-0x180133bc0-procedure_assembly.json'='8d28038976736e5bc058a518e35a1c8e2ff52c0fe589bfa5aaee8cdaa2669fbd'
 'docs\reverse-client\r1-engine-spatial-ground\11-EngineSpatialGround-0x180133a10-procedure_assembly.json'='251bbe9710cb15d8b3684db448d7a8cfe8273cd682f88ade6a675e53850a1fec'
 'docs\reverse-client\r1-spatial-ground-direct\05-SpatialGroundDirect-0x18011be80-procedure_assembly.json'='f210719a7883f5f6e6f55b3fe7cc399dda90bce4c733f86777ef6f2e949a17dc'
 'docs\reverse-client\r1-spatial-ground-direct\08-SpatialGroundDirect-0x18011bbc0-procedure_assembly.json'='1f8618dfe1a6c815e9bb5e75f2fb9d87ec273acea425e49c929d1ebe7fa16416'
 'docs\reverse-client\r1-ground-blocks-and-node-destroy\05-GroundBlocksNodeDestroy-0x18011bb30-procedure_assembly.json'='cbe9bf28437a6d85bdd64e177d56da23a1964f4d10d099f394e9038cd545279b'
 'docs\reverse-client\r1-ground-loader-slot\inspection.json'='aa504399179a7ff4d570d967544706e8ccc988e88cb34a7f282beb3514700215'
 'docs\reverse-client\r1-spatial-ground-callback-slots\inspection.json'='57cceb2a10b9aef18d7e4b2a8d175d35b5e02db7f069efe86faa00a99101c28b'
}
function Hash([string]$path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-File([string]$path,[string]$expected){
 $item=Get-Item -LiteralPath $path
 if($expected -notmatch '^[0-9a-f]{64}$' -or $item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -or (Hash $path) -ne $expected){throw ('Input identity changed: '+$path)}
}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace, no arguments required'}
if(Test-Path -LiteralPath $output){throw 'Existing spatial evidence preserved; output must be new'}
Check-File $priorResult $priorResultHash
$prior=Get-Content -LiteralPath $priorResult -Raw|ConvertFrom-Json
if($prior.status -ne 'passed-canonical-actor-bounds-integration-and-affected-regression-only' -or $null -ne $prior.failure){throw 'Passed canonical Actor dependency result is required'}
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
 source_pins=$sources;dependency_pins=$dependencies;native_evidence_pins=$evidence;prior_result=$priorResult;prior_result_sha256=$priorResultHash
 steps=@();test_results=@();artifacts=@();old_test_suites_rerun=@();old_executable_entrypoints_invoked=@();failure=$null
 own_nonempty_tree_moves=$false;shared_actor_bounds=$false;own_managed_free_adapter=$false;loaded_native_verified=$false
 original_read=$false;original_executed=$false;backend_started=$false;position_publication_validated=$false;gameplay_accepted=$false
 support_boundary='Evidenced own spatial-tree insert/unlink/refine/update, Y propagation, fixed disk node destructor, shared Actor bounds, and managed allocation/free with live capacity reclamation. Explicit own startup extents and finite sequential arithmetic/lifetime domain. Unknown dynamic tables remain Unsupported. No native heap/ABI/EH, loaded concurrency, scene bootstrap, complete World publication or playable-game acceptance.'
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
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$library=Join-Path $output 'GrimDynasty.EntitySpatialTree.dll';$tests=Join-Path $output 'EntitySpatialTreeTests.exe'
 Run-Step 'compile-entity-spatial-tree' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@((Join-Path $source 'EntitySpatialTreeContracts.cs')))
 Run-Step 'compile-spatial-tree-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:EntitySpatialTreeTests',"/out:$tests","/reference:$library")+$references+@((Join-Path $source 'EntitySpatialTreeTests.cs')))
 Run-Step 'new-spatial-tree-tests' $tests @()
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-spatial-tree-tests.log') -Raw|ConvertFrom-Json
 if($summary.status -ne 'passed' -or $summary.scenarios -ne 23 -or $summary.assertions -le 0 -or $summary.own_nonempty_tree_moves -ne $true -or $summary.shared_actor_bounds -ne $true -or $summary.loaded_native_verified -ne $false -or $summary.position_publication -ne $false -or $summary.gameplay_accepted -ne $false){throw 'Missing or unexpected spatial-tree summary'}
 $report.test_results += [ordered]@{name='new-nonempty-spatial-tree-movement-lifetime-and-prefixes';kind='new';scenarios=$summary.scenarios;assertions=$summary.assertions}
 Check-Inputs;foreach($name in $dependencies.Keys){Check-File (Join-Path $output $name) $dependencies[$name]}
 $report.own_nonempty_tree_moves=$true;$report.shared_actor_bounds=$true;$report.own_managed_free_adapter=$true;$report.status='passed-new-entity-spatial-tree-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 9|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;steps=$report.steps;test_results=$report.test_results;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 6
