param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$previous=Join-Path $taskRoot '.tooling\r1-ground-block-fc-copy-validation'
$output=Join-Path $taskRoot '.tooling\r1-ground-spatial-tree-validation'
$priorResult=Join-Path $previous 'result.json'
$priorResultHash='7163537075c2be8cc649b3d358beabb3d1cd631f943590d017bb82b36db3f8cc'
$sources=@{
 'GroundSpatialTreeContracts.cs'='926275130fcbc85df2bad1a012a0914fa241e0c19ca289f37174623d7a247726'
 'GroundSpatialTreeTests.cs'='01d3a3eba0ccfeb1a3dc1f790a100d4b958597fb705088362f58f3656780d4ae'
}
$dependencies=@{
 'GrimDynasty.Actions.dll'='3ea34074db9d5a67d7dffb702eb053b703dc3b754d0db442aeadd71247e15c58'
 'GrimDynasty.EntityMovement.dll'='85d848625fce7d5fc580feb04839898bd580a96f0c48308c3aca5b8ab142f84d'
 'GrimDynasty.GroundBlockIntersection.dll'='67c72c5b9fbeffa329a6e4dc2609c8b45ba0656b49d12fed758e5c397e1a70d2'
 'GrimDynasty.GroundGeometry.dll'='b9d70554dddcdc5936731647ec4caaa5b86fc0660f329ab3d044b9b956b7416b'
 'GrimDynasty.GroundMaterialCandidates.dll'='57363683c84b1cc3d1fae94ee3e6ed7ad24190f6cc3ffc8e83c99454868724fe'
 'GrimDynasty.GroundTriangleIntersection.dll'='ab1260f81b132b5b7db7bdd0dd5b721e9e100fc7d665269c6b562b9d0c551401'
 'GrimDynasty.MoveManager.dll'='7563b6e32745e79ddc8308d2ea48ca7336050271d8dc8c9ce1e75346a52232da'
 'GrimDynasty.NavigationNearest.dll'='75f303c482679df6ba2c8f01aad7759ba9ed322ad6386100525b690cd9b9dfd0'
 'GrimDynasty.WorldCoordinates.dll'='6365ad6a051d6ffbb4009824d98fa899bec89f5933dea383a9a3d98f193e71c7'
}
$evidence=@{
 'docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin'='a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289'
 'docs\reverse-client\r1-ground-ray-tree\08-GroundRayTree-0x18011a600-procedure_assembly.json'='9966a5e96dabc3cca8eacf499afe6bea40330af505a30cdef2f24999d60859ef'
 'docs\reverse-client\r1-ground-ray-tree\11-GroundRayTree-0x1801cfdf0-procedure_assembly.json'='8b4a48a15e631ea4fd8374d6ec9cbca3af7a6a226d32ce2f924953c1e12e6533'
 'docs\reverse-client\r1-ground-intersection-remaining\20-GroundIntersectionRemaining-0x1801d0290-procedure_assembly.json'='b2f61bad9d1e654136e859a139a52cc1cb7bc946be315547645d855e43db8275'
 'docs\reverse-client\r1-ground-face-and-unlink\11-GroundFaceUnlink-0x1801cff90-procedure_assembly.json'='50194bf64c796d90a918e0349e7cf1679a4115bf537a2539bc662d0b74488866'
 'docs\reverse-client\r1-ground-floor-geometry\05-GroundFloorGeometry-0x1801d0100-procedure_assembly.json'='e6208532b42d939df7051d4547f1cd65dbfa24b4c262d7a7450f1ed0313a5b50'
 'docs\reverse-client\r1-ground-floor-geometry\11-GroundFloorGeometry-0x1801d0e20-procedure_assembly.json'='45b7816c25261a5cfd6b313cc67031527dfe22416b015723bdd8912e8c2e0b7b'
 'docs\reverse-client\r1-ground-blocks-and-node-destroy\14-GroundBlocksNodeDestroy-0x1801ce910-procedure_assembly.json'='c409e0f20684d60e076ed27861ebeece894cfdcc37808c4eb4c4b32a05f98dcf'
 'docs\reverse-client\r1-ground-blocks-and-node-destroy\05-GroundBlocksNodeDestroy-0x18011bb30-procedure_assembly.json'='cbe9bf28437a6d85bdd64e177d56da23a1964f4d10d099f394e9038cd545279b'
 'docs\reverse-client\r1-spatial-ground-direct\05-SpatialGroundDirect-0x18011be80-procedure_assembly.json'='f210719a7883f5f6e6f55b3fe7cc399dda90bce4c733f86777ef6f2e949a17dc'
 'docs\reverse-client\r1-spatial-ground-direct\08-SpatialGroundDirect-0x18011bbc0-procedure_assembly.json'='1f8618dfe1a6c815e9bb5e75f2fb9d87ec273acea425e49c929d1ebe7fa16416'
 'docs\reverse-client\r1-ground-intersection-slots\inspection.json'='a07945f156610a3ee5b49b9a954079b46f1a04610bde8d2cf20a04af1ab177c7'
 'docs\reverse-client\r1-ground-loader-slot\inspection.json'='aa504399179a7ff4d570d967544706e8ccc988e88cb34a7f282beb3514700215'
 'docs\reverse-client\r1-ground-geometry-bindings\inspection.json'='8be1f21796126e33fae05d412b4690b8d7cf5618bda89593d4f6021230621d17'
 'docs\reverse-client\r1-spatial-ground-callback-slots\inspection.json'='57cceb2a10b9aef18d7e4b2a8d175d35b5e02db7f069efe86faa00a99101c28b'
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
if(Test-Path -LiteralPath $output){throw 'Existing tree evidence preserved; output must be new'}
Check-File $priorResult $priorResultHash
$prior=Get-Content -LiteralPath $priorResult -Raw|ConvertFrom-Json
if($prior.status -ne 'passed-ground-block-fc-copy-regression' -or $null -ne $prior.failure){throw 'Passed corrected Block FC-copy result required'}
if($prior.source_pins.'GroundBlockIntersectionContracts.cs' -ne '4542efbc0d2be6d2e3539c5daaed34c8aa4b39537cea447c882b825eaba568b9'){throw 'Prior Block source is not the corrected opaque-FC revision'}
foreach($name in $dependencies.Keys){$record=@($prior.artifacts|Where-Object {$_.name -eq $name});if($record.Count -ne 1 -or $record[0].sha256 -ne $dependencies[$name]){throw ('Dependency not uniquely pinned by passed FC result: '+$name)}}
function Check-Inputs {
 Check-Ancestors $output
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
 shared_ground_tree=$false;real225_entries=$false;real_material_unknown_stop=$false;real_external_miss=$false;own_fixture_query_hits=$false
 real_material_resolved=$false;native_load=$false;world_floor=$false;original_read=$false;original_executed=$false;backend_started=$false;gameplay_accepted=$false
 support_boundary='Observed11A600 initialization,1CE910 publication portion,1CFDF0 insertion,1D0290 refinement,1CFF90 unlink,shared11BE80/11BBC0/11BB30 node operations and1D0100 actual Block intersection query. Same225 real saved Block/candidate identities, finite sequential OWN arithmetic/allocation/cold-link adaptation. Real material pointers remain Unknown and stop; explicit OWN fixtures exercise hit selection and opaque material DWORD transfer through the actual20B copy. Corrected passed Block FC-copy library required. No native fullLoad,original execution,Water/World floor,complete mouse movement or gameplay acceptance. Seven new tree scenarios only; no old suite rerun.'
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
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$library=Join-Path $output 'GrimDynasty.GroundSpatialTree.dll';$tests=Join-Path $output 'GroundSpatialTreeTests.exe'
 Run-Step 'compile-ground-spatial-tree' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@((Join-Path $source 'GroundSpatialTreeContracts.cs')))
 Run-Step 'compile-ground-tree-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:GroundSpatialTreeTestProgram',"/out:$tests","/reference:$library")+$references+@((Join-Path $source 'GroundSpatialTreeTests.cs')))
 Run-Step 'new-ground-tree-tests' $tests @((Join-Path $taskRoot 'docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin'))
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-ground-tree-tests.log') -Raw|ConvertFrom-Json
 if($summary.status -ne 'passed' -or $summary.scenarios -ne 7 -or $summary.assertions -le 0 -or $summary.real_entries -ne 225 -or $summary.real_nodes -le 1 -or $summary.real_max_depth -le 0 -or $summary.real_nonroot_entries -le 0 -or $summary.real_material_unknown_stop -ne $true -or $summary.real_external_miss -ne $true -or $summary.own_fixture_query_hits -ne $true -or $summary.saved_member_sha256 -ne $evidence.'docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin'){throw 'Missing or unexpected tree summary'}
 foreach($name in @('real_material_resolved','native_load','world_floor','gameplay_accepted')){if($summary.$name -ne $false){throw ('Tree fixture exceeded support boundary: '+$name)}}
 $report.test_results += [ordered]@{name='new-ground-spatial-tree';kind='new';scenarios=$summary.scenarios;assertions=$summary.assertions;real_entries=$summary.real_entries;real_nodes=$summary.real_nodes;real_max_depth=$summary.real_max_depth;real_nonroot_entries=$summary.real_nonroot_entries;real_material_unknown_stop=$summary.real_material_unknown_stop;real_external_miss=$summary.real_external_miss;own_fixture_query_hits=$summary.own_fixture_query_hits}
 Check-Inputs;foreach($name in $dependencies.Keys){Check-File (Join-Path $output $name) $dependencies[$name]}
 $report.shared_ground_tree=$true;$report.real225_entries=$true;$report.real_material_unknown_stop=$true;$report.real_external_miss=$true;$report.own_fixture_query_hits=$true
 $report.status='passed-new-ground-spatial-tree-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 9|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;test_results=$report.test_results;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 6
