param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$prior=Join-Path $taskRoot '.tooling\r1-ground-block-fc-copy-validation'
$output=Join-Path $taskRoot '.tooling\r1-ground-water-empty-validation'
$priorHash='7163537075c2be8cc649b3d358beabb3d1cd631f943590d017bb82b36db3f8cc'
$sources=@{
 'GroundWaterContracts.cs'='5ae3a19a8f96b862f6eeecbc102e25b2b774aebc14be1bbda8349681b8534744'
 'GroundWaterTests.cs'='e26eaeb5a16366af7539da77dfcabce02a0bb407e7331ccb5256f6e803a06cb2'
}
$dependencies=@{
 'GrimDynasty.Actions.dll'='3ea34074db9d5a67d7dffb702eb053b703dc3b754d0db442aeadd71247e15c58'
 'GrimDynasty.MoveManager.dll'='7563b6e32745e79ddc8308d2ea48ca7336050271d8dc8c9ce1e75346a52232da'
 'GrimDynasty.WorldCoordinates.dll'='6365ad6a051d6ffbb4009824d98fa899bec89f5933dea383a9a3d98f193e71c7'
 'GrimDynasty.NavigationNearest.dll'='75f303c482679df6ba2c8f01aad7759ba9ed322ad6386100525b690cd9b9dfd0'
 'GrimDynasty.EntityMovement.dll'='85d848625fce7d5fc580feb04839898bd580a96f0c48308c3aca5b8ab142f84d'
 'GrimDynasty.GroundGeometry.dll'='b9d70554dddcdc5936731647ec4caaa5b86fc0660f329ab3d044b9b956b7416b'
 'GrimDynasty.GroundMaterialCandidates.dll'='57363683c84b1cc3d1fae94ee3e6ed7ad24190f6cc3ffc8e83c99454868724fe'
 'GrimDynasty.GroundTriangleIntersection.dll'='ab1260f81b132b5b7db7bdd0dd5b721e9e100fc7d665269c6b562b9d0c551401'
 'GrimDynasty.GroundBlockIntersection.dll'='67c72c5b9fbeffa329a6e4dc2609c8b45ba0656b49d12fed758e5c397e1a70d2'
}
$evidence=@{
 'docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin'='a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289'
 'docs\reverse-client\r1-mainmenu-complete-member\water-chunk-datum.json'='0ffac5ec0d486eba6507ccb510b88f446dcd1fc5106d7446c2ea023b82fc0155'
 'docs\reverse-client\r1-ground-resources-and-water\08-GroundResourcesWater-0x18012c590-procedure_assembly.json'='cd9359266f1c151487a7101ac17cd53a74996effb41c8c57f113b93cc31f3ce9'
 'docs\reverse-client\r1-ground-resources-and-water\11-GroundResourcesWater-0x1801f81e0-procedure_assembly.json'='34090b3017b93bc02a360aab379ad9d86fc5686af1447698a4673b9c56074c80'
 'docs\reverse-client\r1-ground-resources-and-water\14-GroundResourcesWater-0x1801f9bd0-procedure_assembly.json'='68c98ac82656608a3785037e5fb01188244545f76aea931e41abb02bf808a83b'
 'docs\reverse-client\r1-ground-resources-and-water\summary.json'='4b1442d60a79bc05c1fb29969ae257a2aea574763a8d5716a7a956fee748a72e'
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
if(Test-Path -LiteralPath $output){throw 'Existing water evidence preserved; output must be new'}
$priorResult=Join-Path $prior 'result.json'
Check-File $priorResult $priorHash
$old=Get-Content -LiteralPath $priorResult -Raw|ConvertFrom-Json
if($old.status -ne 'passed-ground-block-fc-copy-regression' -or $null -ne $old.failure){throw 'Passed FC-copy dependency required'}
foreach($name in $dependencies.Keys){
 $record=@($old.artifacts|Where-Object {$_.name -eq $name})
 if($record.Count -ne 1 -or $record[0].sha256 -ne $dependencies[$name]){throw ('Dependency not pinned by passed FC result: '+$name)}
}
function Check-Inputs {
 Check-Ancestors $output
 Check-File $compiler $compilerHash;Check-File $priorResult $priorHash
 foreach($name in $sources.Keys){Check-File (Join-Path $taskRoot ('src\reverse_client_actions\'+$name)) $sources[$name]}
 foreach($name in $dependencies.Keys){Check-File (Join-Path $prior $name) $dependencies[$name]}
 foreach($name in $evidence.Keys){Check-File (Join-Path $taskRoot $name) $evidence[$name]}
}
Check-Inputs
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash
 source_pins=$sources;dependency_pins=$dependencies;native_and_saved_evidence_pins=$evidence
 prerequisite_result=$priorResult;prerequisite_result_sha256=$priorHash
 steps=@();test_results=@();artifacts=@();old_test_suites_rerun=@();old_executable_entrypoints_invoked=@();failure=$null
 real_saved_empty_water_payload=$false;explicit_own_nonnull_prerequisites=$true;native_constructor_initialize=$false
 real_material_resolved=$false;world_floor=$false;original_read=$false;original_executed=$false;backend_started=$false;gameplay_accepted=$false
 support_boundary='Observed empty Water Load1F81E0/GetIntersection1F9BD0 using real saved24B chunk9 and same mutable count fields. Tests explicitly establish OWN opaque nonnull10B0/10B8 identities; they do not prove nativeCtor1F6A90/Initialize1F6E80 or valid native pointees. Unknown prerequisites and nonempty branches remain Unsupported; no default miss/success. Output Inf/+0/+0/+0 and untouched material are checked. No native fullLoad, World floor, nonempty water, original execution or gameplay acceptance. No old suite rerun.'
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
 foreach($name in $dependencies.Keys){Copy-Item -LiteralPath (Join-Path $prior $name) -Destination (Join-Path $output $name);Check-File (Join-Path $output $name) $dependencies[$name]}
 $references=@();foreach($name in ($dependencies.Keys|Sort-Object)){$references+='/reference:'+(Join-Path $output $name)}
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$library=Join-Path $output 'GrimDynasty.GroundWater.dll';$tests=Join-Path $output 'GroundWaterTests.exe'
 Run-Step 'compile-ground-water' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@((Join-Path $source 'GroundWaterContracts.cs')))
 Run-Step 'compile-ground-water-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:GroundWaterTestProgram',"/out:$tests","/reference:$library")+$references+@((Join-Path $source 'GroundWaterTests.cs')))
 Run-Step 'new-ground-water-tests' $tests @((Join-Path $taskRoot 'docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin'))
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-ground-water-tests.log') -Raw|ConvertFrom-Json
 if($summary.status -ne 'passed' -or $summary.scenarios -ne 20 -or $summary.assertions -le 0 -or $summary.scope -ne 'real saved empty Water chunk; explicit OWN nonnull initialization prerequisites; no native constructor/Initialize or nonempty water'){throw 'Missing or unexpected empty Water summary'}
 $report.test_results += [ordered]@{name='real-empty-water-own-prerequisites';kind='new';scenarios=$summary.scenarios;assertions=$summary.assertions}
 Check-Inputs;foreach($name in $dependencies.Keys){Check-File (Join-Path $output $name) $dependencies[$name]}
 $report.real_saved_empty_water_payload=$true;$report.status='passed-new-real-empty-water-with-own-prerequisites-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 9|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;test_results=$report.test_results;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 6
