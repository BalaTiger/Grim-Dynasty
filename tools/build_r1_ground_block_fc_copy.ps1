param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$candidate=Join-Path $taskRoot '.tooling\r1-ground-material-candidates-validation'
$triangle=Join-Path $taskRoot '.tooling\r1-ground-triangle-intersection-validation-2'
$output=Join-Path $taskRoot '.tooling\r1-ground-block-fc-copy-validation'
$blockBaseline=Join-Path $taskRoot '.tooling\r1-ground-block-intersection-validation\result.json'
$blockBaselineHash='e1fc057812c6e78678095f11e6e5da09e9a57dc8ddfba906df12230f36d1a7b7'
$candidateResultHash='634931e218ceda58adb905a5cc0efd51ec502cfa5bb3f3b3897afcabb05e6969'
$triangleResultHash='47da03a253746f5308a994ccae0bf8fb89105e617e0e89681b503b1f27c46326'
$sources=@{
 'GroundBlockIntersectionContracts.cs'='4542efbc0d2be6d2e3539c5daaed34c8aa4b39537cea447c882b825eaba568b9'
 'GroundBlockIntersectionTests.cs'='348e81c8bc301660de374a4e39efc1890901da062e6805dcee3cf2838a73fa44'
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
}
$evidence=@{
 'docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin'='a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289'
 'docs\reverse-client\r1-ground-ray-tree\17-GroundRayTree-0x1801d1330-procedure_assembly.json'='d357bf689cb3222286ff59bc8bfa10e72905ef3906e65108095d9aee63bfdae1'
 'docs\reverse-client\r1-ground-leaf-windows\manual-disk-decode.json'='b9cb5ebeac0db7910bbe3728e8c20526b4fff202ed7058ce48b9016ab9d55de0'
 'docs\reverse-client\r1-ground-intersection-slots\inspection.json'='a07945f156610a3ee5b49b9a954079b46f1a04610bde8d2cf20a04af1ab177c7'
 'docs\reverse-client\r1-navigation-portal-constant\inspection.json'='3543631f0f86f88fedbd7f89fc57b813b1b8c5d345b6f46b6a87b55ac0629908'
 'docs\reverse-client\r1-spatial-ground-direct\11-SpatialGroundDirect-0x1801de090-procedure_assembly.json'='280b8fc0515859b8de565e69e9ee40dcb047d76631ca4c9e368d3177faa105ff'
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
function Dependency-Path([string]$name){if($name -eq 'GrimDynasty.GroundTriangleIntersection.dll'){Join-Path $triangle $name}else{Join-Path $candidate $name}}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace, no arguments required'}
Check-Ancestors $output
if(!([IO.Path]::GetFullPath($output)).StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Output must stay inside fixed workspace'}
if(Test-Path -LiteralPath $output){throw 'Existing block evidence preserved; output must be new'}
Check-File (Join-Path $candidate 'result.json') $candidateResultHash
Check-File (Join-Path $triangle 'result.json') $triangleResultHash
Check-File $blockBaseline $blockBaselineHash
$priorBlock=Get-Content -LiteralPath $blockBaseline -Raw|ConvertFrom-Json
if($priorBlock.status -ne 'passed-new-ground-block-intersection-only' -or $null -ne $priorBlock.failure){throw 'Passed original Block result required'}
$priorCandidate=Get-Content -LiteralPath (Join-Path $candidate 'result.json') -Raw|ConvertFrom-Json
$priorTriangle=Get-Content -LiteralPath (Join-Path $triangle 'result.json') -Raw|ConvertFrom-Json
if($priorCandidate.status -ne 'passed-new-ground-material-candidates-only' -or $null -ne $priorCandidate.failure){throw 'Passed candidate result required'}
if($priorTriangle.status -ne 'passed-new-saved-ground-triangle-ray-and-bounds-only' -or $null -ne $priorTriangle.failure){throw 'Passed triangle/AABB result required'}
foreach($name in $dependencies.Keys){
 $prior=$priorCandidate;if($name -eq 'GrimDynasty.GroundTriangleIntersection.dll'){$prior=$priorTriangle}
 $record=@($prior.artifacts|Where-Object {$_.name -eq $name})
 if($record.Count -ne 1 -or $record[0].sha256 -ne $dependencies[$name]){throw ('Dependency not pinned by passed result: '+$name)}
}
function Check-Inputs {
 Check-File $compiler $compilerHash
 Check-File $blockBaseline $blockBaselineHash
 Check-File (Join-Path $candidate 'result.json') $candidateResultHash;Check-File (Join-Path $triangle 'result.json') $triangleResultHash
 foreach($name in $sources.Keys){Check-File (Join-Path $taskRoot ('src\reverse_client_actions\'+$name)) $sources[$name]}
 foreach($name in $dependencies.Keys){Check-File (Dependency-Path $name) $dependencies[$name]}
 foreach($name in $evidence.Keys){Check-File (Join-Path $taskRoot $name) $evidence[$name]}
}
Check-Inputs
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash
 source_pins=$sources;dependency_pins=$dependencies;native_and_saved_evidence_pins=$evidence
 candidate_result_sha256=$candidateResultHash;triangle_result_sha256=$triangleResultHash;block_baseline_sha256=$blockBaselineHash
 steps=@();test_results=@();artifacts=@();old_test_suites_rerun=@('GroundBlockIntersection:8 relevant scenarios, FC expectation corrected');old_executable_entrypoints_invoked=@();failure=$null
 ground_block_intersection=$false;saved_geometry_to_resource_unknown=$false;real_material_resolved=$false;native_load=$false;world_floor=$false
 original_read=$false;original_executed=$false;backend_started=$false;gameplay_accepted=$false
 support_boundary='Precise1D152D MOV/1D1533 MOV correction: an established valid material-field view copies opaque DWORD knowledge; Unknown FC no longer incorrectly stops XYZ. Unknown resource pointer still stops at1D1528 TEST and an object lacking a valid FC view still stops at1D152D. Eight affected Block scenarios rerun with corrected FC expectation, plus one exact-DWORD-alias scenario; no other old suite rerun. Real saved member still has unknown resource pointers and stops after actual geometric distance. No native full Load, material resolution, spatial tree/World publication, water, original execution or gameplay acceptance.'
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
 foreach($name in $dependencies.Keys){Copy-Item -LiteralPath (Dependency-Path $name) -Destination (Join-Path $output $name);Check-File (Join-Path $output $name) $dependencies[$name]}
 $references=@();foreach($name in ($dependencies.Keys|Sort-Object)){$references+='/reference:'+(Join-Path $output $name)}
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$library=Join-Path $output 'GrimDynasty.GroundBlockIntersection.dll';$tests=Join-Path $output 'GroundBlockIntersectionTests.exe'
 Run-Step 'compile-ground-block-intersection' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@((Join-Path $source 'GroundBlockIntersectionContracts.cs')))
 Run-Step 'compile-ground-block-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:GroundBlockIntersectionTestProgram',"/out:$tests","/reference:$library")+$references+@((Join-Path $source 'GroundBlockIntersectionTests.cs')))
 Run-Step 'new-ground-block-tests' $tests @((Join-Path $taskRoot 'docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin'))
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-ground-block-tests.log') -Raw|ConvertFrom-Json
 if($summary.status -ne 'passed' -or $summary.scenarios -ne 9 -or $summary.assertions -le 0 -or $summary.ground_block_intersection -ne $true -or $summary.saved_geometry_to_resource_unknown -ne $true -or $summary.real_material_resolved -ne $false -or $summary.native_load -ne $false -or $summary.world_floor -ne $false -or $summary.gameplay_accepted -ne $false){throw 'Missing or unexpected Block summary'}
 $report.test_results += [ordered]@{name='ground-block-fc-copy';kind='affected-regression-and-one-new-alias';scenarios=$summary.scenarios;assertions=$summary.assertions;affected_scenarios=8;new_scenarios=1}
 Check-Inputs;foreach($name in $dependencies.Keys){Check-File (Join-Path $output $name) $dependencies[$name]}
 $report.ground_block_intersection=$true;$report.saved_geometry_to_resource_unknown=$true;$report.status='passed-ground-block-fc-copy-regression'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 9|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;steps=$report.steps;test_results=$report.test_results;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 6
