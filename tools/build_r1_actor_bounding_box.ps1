param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$previous=Join-Path $taskRoot '.tooling\r1-world-publication-prerequisites-validation'
$output=Join-Path $taskRoot '.tooling\r1-actor-bounding-box-validation'
$priorResult=Join-Path $previous 'result.json'
$priorResultHash='43da143e7dca57b18fffa4316797c734a809d2a223a91295fb8b95d9a6855c42'
$sources=@{
 'ActorBoundingBoxContracts.cs'='1829bfffb43caba447b227345ef67d4315db692c99f40e69f96cd134d9c32a3d'
 'ActorBoundingBoxTests.cs'='a478d4ac1eb8fc2afd1db181ec50fe02a10b4098c2af07df080d582039a81cbf'
}
$dependencies=@{
 'GrimDynasty.Actions.dll'='3ea34074db9d5a67d7dffb702eb053b703dc3b754d0db442aeadd71247e15c58'
 'GrimDynasty.MoveManager.dll'='7563b6e32745e79ddc8308d2ea48ca7336050271d8dc8c9ce1e75346a52232da'
 'GrimDynasty.WorldCoordinates.dll'='6365ad6a051d6ffbb4009824d98fa899bec89f5933dea383a9a3d98f193e71c7'
 'GrimDynasty.NavigationNearest.dll'='75f303c482679df6ba2c8f01aad7759ba9ed322ad6386100525b690cd9b9dfd0'
 'GrimDynasty.EntityMovement.dll'='85d848625fce7d5fc580feb04839898bd580a96f0c48308c3aca5b8ab142f84d'
}
$evidence=@{
 'docs\reverse-client\r1-engine-entity-publication-core\08-EngineEntityPublication-0x18002a990-procedure_assembly.json'='7903afdba78a2aa2b7f7d1d6c59b52bcc3778966752253f56b2fb245dd81ae8f'
 'docs\reverse-client\r1-engine-spatial-ground\17-EngineSpatialGround-0x18007ed70-procedure_assembly.json'='0a9113a67c01cdcecd4451164dcc1e9bac31973943594ca501e283d7b652d11c'
 'docs\reverse-client\r1-game-level-publication-tail\08-GameLevelPublicationTail-0x18006da30-procedure_assembly.json'='c885a06d24956ab1571c8255c62d791cc3cca90093c2ec7f49489acee6ea320b'
 'docs\reverse-client\r1-navigation-nearest-constants\inspection.json'='02c4ec5592fdac55c1bf652b1eabdab326232c59150942f8222cd2da652d14e1'
 'docs\reverse-client\r1-entity-bounds-thunk\inspection.json'='a575640eda54d25d8754cba60588990d45806a91998d6028d05ec104ff41377a'
 'docs\reverse-client\r1-entity-bounds-slots\inspection.json'='465f039df3eda0556c16429cebf99062fae0d2990dde667a5b62a31b5a650c5e'
 'docs\object-lifecycle\offline-actor-base-dependency\07d020-instructions.json'='64d3237ddb1279a40f12c471d45a9fd02e9b9b043ed00aa52be6730dcf7b4671'
 'docs\object-lifecycle\offline-actor-constructor-body\0259a0-instructions.json'='59f402f428e04e2928bb485e58fa85896ac08db4548bff6fa33444a58fa521a7'
}
function Hash([string]$path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-Ancestors([string]$path){
 $resolved=[IO.Path]::GetFullPath($path);$cursor=$resolved
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
if(Test-Path -LiteralPath $output){throw 'Existing bounds evidence preserved; output must be new'}
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
 source_pins=$sources;dependency_pins=$dependencies;native_evidence_pins=$evidence;prior_result=$priorResult;prior_result_sha256=$priorResultHash
 steps=@();test_results=@();artifacts=@();old_test_suites_rerun=@();old_executable_entrypoints_invoked=@();failure=$null
 own_actor_bounding_box=$false;mesh_resource_production=$false;attached_id_traversal=$false
 original_read=$false;original_executed=$false;backend_started=$false;position_publication_validated=$false;gameplay_accepted=$false
 support_boundary='Observed Engine02A990 normal zero-count attached-ID domain with exact IsInWorld07ED70, Character06DA30 first-nonnull borrowed mesh, explicitly selected Character/Player primary identity and cached mask. Shared Entity and box cells; scalar binary32 operation/write order and live field rereads. No real mesh producer, Actor mesh fallback, nonempty attachment traversal, loaded virtual-table/ABI/FP-mode compatibility, spatial publication or gameplay acceptance.'
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
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$library=Join-Path $output 'GrimDynasty.ActorBoundingBox.dll';$tests=Join-Path $output 'ActorBoundingBoxTests.exe'
 Run-Step 'compile-actor-bounding-box' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@((Join-Path $source 'ActorBoundingBoxContracts.cs')))
 Run-Step 'compile-bounds-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:ActorBoundingBoxTestProgram',"/out:$tests","/reference:$library")+$references+@((Join-Path $source 'ActorBoundingBoxTests.cs')))
 Run-Step 'new-bounds-tests' $tests @()
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-bounds-tests.log') -Raw|ConvertFrom-Json
 if($summary.status -ne 'passed' -or $summary.scenarios -ne 29 -or $summary.assertions -le 0 -or $summary.own_actor_bounding_box -ne $true -or $summary.mesh_resource_production -ne $false -or $summary.attached_id_traversal -ne $false -or $summary.position_publication -ne $false -or $summary.gameplay_accepted -ne $false){throw 'Missing or unexpected bounds summary'}
 $report.test_results += [ordered]@{name='shared-actor-bounds-explicit-no-attached-ids';kind='new';scenarios=$summary.scenarios;assertions=$summary.assertions}
 Check-Inputs;foreach($name in $dependencies.Keys){Check-File (Join-Path $output $name) $dependencies[$name]}
 $report.own_actor_bounding_box=$true;$report.status='passed-new-actor-bounding-box-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 9|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;steps=$report.steps;test_results=$report.test_results;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 6
