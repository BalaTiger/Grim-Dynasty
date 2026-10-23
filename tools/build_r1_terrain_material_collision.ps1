param()
$ErrorActionPreference='Stop'
# Prepared only. Root executed the fixed23B reader; this build is separately
# pending root review and resource release. Its actual receipt is pinned below.
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$jsonAssembly='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Web.Extensions.dll'
$jsonAssemblyHash='5e6378edc06f5a61920aaadb2aba839dd3cf302d6192e08aacd4454ba4cff0b3'
$previous=Join-Path $taskRoot '.tooling\r1-ground-spatial-tree-validation'
$output=Join-Path $taskRoot '.tooling\r1-terrain-material-collision-validation'
$priorResult=Join-Path $previous 'result.json'
$priorResultHash='0ad251a681a3834a0ea4039ad7c215f6bbeefcf46b5672cc9584a2f801adcc42'
$readerHash='8be08c76e92519ad71b0cb465113b3f2e9813fcfaee9bbc4004d15dd57996e04'
$ReviewedOperandReportSha256='56db24887edb67e188ba5f034d3734c24042e973c70be4c3e833896942b1a75f'
$receipt=Join-Path $taskRoot 'docs\reverse-client\r1-terrain-surface-strings\inspection.json'
$sources=@{
 'TerrainMaterialCollisionAdapter.cs'='54bbe4f2553da8980363addd331573372e8ea05de15e725d244997d02d5bbad3'
 'TerrainMaterialCollisionTests.cs'='bed69ce8d14d2f5a1bf72b41817410515f1cc9a8830ac3666fd2a9052ea5c2e2'
}
$dependencies=@{
 'GrimDynasty.Actions.dll'='3ea34074db9d5a67d7dffb702eb053b703dc3b754d0db442aeadd71247e15c58'
 'GrimDynasty.EntityMovement.dll'='85d848625fce7d5fc580feb04839898bd580a96f0c48308c3aca5b8ab142f84d'
 'GrimDynasty.GroundBlockIntersection.dll'='67c72c5b9fbeffa329a6e4dc2609c8b45ba0656b49d12fed758e5c397e1a70d2'
 'GrimDynasty.GroundGeometry.dll'='b9d70554dddcdc5936731647ec4caaa5b86fc0660f329ab3d044b9b956b7416b'
 'GrimDynasty.GroundMaterialCandidates.dll'='57363683c84b1cc3d1fae94ee3e6ed7ad24190f6cc3ffc8e83c99454868724fe'
 'GrimDynasty.GroundSpatialTree.dll'='06e457c22322efc89111a42f822e01c11929a2c8a2918c7c7b2b945efdfd316d'
 'GrimDynasty.GroundTriangleIntersection.dll'='ab1260f81b132b5b7db7bdd0dd5b721e9e100fc7d665269c6b562b9d0c551401'
 'GrimDynasty.MoveManager.dll'='7563b6e32745e79ddc8308d2ea48ca7336050271d8dc8c9ce1e75346a52232da'
 'GrimDynasty.NavigationNearest.dll'='75f303c482679df6ba2c8f01aad7759ba9ed322ad6386100525b690cd9b9dfd0'
 'GrimDynasty.WorldCoordinates.dll'='6365ad6a051d6ffbb4009824d98fa899bec89f5933dea383a9a3d98f193e71c7'
}
$evidence=@{
 'tools\static_analysis\read_r1_terrain_surface_strings.mjs'=$readerHash
 'tools\static_analysis\read_r1_terrain_surface_strings.plan.json'='0711e4f382930322564293669b861edb3780f0616f22a95498e442a38a5623a4'
 'docs\reverse-client\terrain-material-record-bindings.json'='3c880e35336ec2e4f55c02f1fd4e070594d80a6ff30f4e95608375f570473081'
 'docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin'='a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289'
 'docs\reverse-client\r1-terrain-type-material\04-TerrainTypeMaterial-0x1801e1940-procedure_pseudo_code.json'='26f717082e3030352ffe34277406c1132c45662d0867b707c27df55375579038'
 'docs\reverse-client\r1-terrain-type-material\05-TerrainTypeMaterial-0x1801e1940-procedure_assembly.json'='6c4e417d3b9c908a716eb680ad7ca8f3cf0b1c3e53f847ba891950ccee8730e5'
 'docs\reverse-client\r1-terrain-records\terrain-0.json'='99174ba1196eae2c109ad46c18fdcbf2e368923d49117739d0496829d3389d93'
 'docs\reverse-client\r1-terrain-records\terrain-1.json'='d3d75935c98d5909f95ce0ca5c330e110dd42f5a4251ec881706c95c00edd4bf'
 'docs\reverse-client\r1-terrain-records\terrain-2.json'='267ec1e9b06cd9c54812eeaee3e0a8a7b7ff7cc9223f42823de48c2a43213895'
 'docs\reverse-client\r1-terrain-records\terrain-3.json'='6512c54802bcd3b6ad1bb83fe86b8b35a5f0e98397245a8347501cece66c7b4b'
 'docs\reverse-client\r1-terrain-records\terrain-4.json'='e829033a0fb91dfb904973a53809b1eda34f41c5369a9b22726637dc4cde98ee'
 'src\reverse_client_actions\GroundBlockIntersectionContracts.cs'='4542efbc0d2be6d2e3539c5daaed34c8aa4b39537cea447c882b825eaba568b9'
 'src\reverse_client_actions\GroundSpatialTreeContracts.cs'='926275130fcbc85df2bad1a012a0914fa241e0c19ca289f37174623d7a247726'
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
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace and no arguments required'}
Check-Ancestors $output
if(!([IO.Path]::GetFullPath($output)).StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Output must stay inside fixed workspace'}
if(Test-Path -LiteralPath $output){throw 'Existing material validation evidence preserved; output must be new'}
Check-File $priorResult $priorResultHash
$prior=Get-Content -LiteralPath $priorResult -Raw|ConvertFrom-Json
if($prior.status -ne 'passed-new-ground-spatial-tree-only' -or $null -ne $prior.failure -or $prior.prior_result_sha256 -ne '7163537075c2be8cc649b3d358beabb3d1cd631f943590d017bb82b36db3f8cc'){throw 'Passed tree rooted in corrected FC-copy library required'}
foreach($name in $dependencies.Keys){$record=@($prior.artifacts|Where-Object {$_.name -eq $name});if($record.Count -ne 1 -or $record[0].sha256 -ne $dependencies[$name]){throw ('Dependency not uniquely pinned by passed tree result: '+$name)}}
function Check-Inputs {
 Check-Ancestors $output
 Check-File $compiler $compilerHash;Check-File $jsonAssembly $jsonAssemblyHash;Check-File $priorResult $priorResultHash
 Check-File $receipt $ReviewedOperandReportSha256
 foreach($name in $sources.Keys){Check-File (Join-Path $taskRoot ('src\reverse_client_actions\'+$name)) $sources[$name]}
 foreach($name in $dependencies.Keys){Check-File (Join-Path $previous $name) $dependencies[$name]}
 foreach($name in $evidence.Keys){Check-File (Join-Path $taskRoot $name) $evidence[$name]}
}
Check-Inputs
$observation=Get-Content -LiteralPath $receipt -Raw|ConvertFrom-Json
if($observation.status -ne 'passed' -or $observation.script_sha256 -ne $readerHash -or $observation.plan_sha256 -ne $evidence.'tools\static_analysis\read_r1_terrain_surface_strings.plan.json' -or $observation.data_bytes -ne 23 -or $observation.read_operations -ne 6 -or $observation.disk_observations_complete -ne $true -or $observation.proofs_verified -ne $true){throw 'Actual reviewed fixed23B receipt required before compiling'}
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash
 json_assembly=$jsonAssembly;json_assembly_sha256=$jsonAssemblyHash;source_pins=$sources;dependency_pins=$dependencies;evidence_pins=$evidence
 operand_report=$receipt;operand_report_sha256=$ReviewedOperandReportSha256;prior_result=$priorResult;prior_result_sha256=$priorResultHash
 steps=@();test_results=@();artifacts=@();old_test_suites_rerun=@();old_executable_entrypoints_invoked=@();failure=$null
 real_material_resolved=$false;native_load=$false;world_floor=$false;gameplay_accepted=$false;original_read=$false;original_executed=$false;backend_started=$false
 support_boundary='Strict five saved TerrainType records mapped to stable per-record OWN collision MaterialView using exact observed surface bytes. Same saved GroundData,225 Block states and actual Tree query. Corrected FC-copy and passed Tree DLLs reused. Unknown/missing inputs are Unsupported. No native TerrainType allocation,fullLoad,shader,ObjectManager,Water/World floor,original execution or gameplay acceptance. Only new adapter/integration scenarios execute.'
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
 $references=@('/reference:'+$jsonAssembly);foreach($name in ($dependencies.Keys|Sort-Object)){$references+='/reference:'+(Join-Path $output $name)}
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$library=Join-Path $output 'GrimDynasty.TerrainMaterialCollision.dll';$tests=Join-Path $output 'TerrainMaterialCollisionTests.exe'
 Run-Step 'compile-terrain-material-adapter' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@((Join-Path $source 'TerrainMaterialCollisionAdapter.cs')))
 Run-Step 'compile-terrain-material-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:TerrainMaterialCollisionTestProgram',"/out:$tests","/reference:$library")+$references+@((Join-Path $source 'TerrainMaterialCollisionTests.cs')))
 Run-Step 'new-terrain-material-tests' $tests @($taskRoot,$ReviewedOperandReportSha256,$readerHash)
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-terrain-material-tests.log') -Raw|ConvertFrom-Json
 if($summary.status -ne 'passed' -or $summary.scenarios -ne 8 -or $summary.assertions -le 0 -or $summary.real_entries -ne 225 -or $summary.same_material_identity -ne $true -or $summary.block_tree_same_hit -ne $true -or $summary.real_material_resolved -ne $true){throw 'Missing or unexpected material integration summary'}
 foreach($name in @('native_load','world_floor','gameplay_accepted')){if($summary.$name -ne $false){throw ('Material adapter exceeded support boundary: '+$name)}}
 $report.test_results += $summary
 Check-Inputs;foreach($name in $dependencies.Keys){Check-File (Join-Path $output $name) $dependencies[$name]}
 $report.real_material_resolved=$true;$report.status='passed-new-terrain-material-collision-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;test_results=$report.test_results;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 6
