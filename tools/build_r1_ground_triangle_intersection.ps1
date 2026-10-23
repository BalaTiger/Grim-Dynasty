param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$previous=Join-Path $taskRoot '.tooling\r1-ground-geometry-validation'
$output=Join-Path $taskRoot '.tooling\r1-ground-triangle-intersection-validation-2'
$priorResult=Join-Path $previous 'result.json'
$priorResultHash='b2732e8832be58b5b2856789fab21ef6b4b22fc67bb435e940d46a241d0a2ecd'
$sources=@{
 'GroundTriangleIntersectionContracts.cs'='bf4d9ca253d780739858a131fdac29291f0dc6c45ef7931779dd72a7cb5b0b2b'
 'GroundTriangleIntersectionTests.cs'='0a31ab4d1e22e0d1d0765f3092a7683e8a6e3debfeb34d74ea0441c0d40ce3c1'
 'GroundRayBoundsContracts.cs'='ff3f884ad172736f512d0bc9014be1a7048dcec79b25ca507a6cb64d943e0a22'
 'GroundRayBoundsTests.cs'='b67b767af5b22930c3771770b16acb1ec888b806db220f2ea9a5b79689e1ecc5'
}
$dependencies=@{
 'GrimDynasty.Actions.dll'='3ea34074db9d5a67d7dffb702eb053b703dc3b754d0db442aeadd71247e15c58'
 'GrimDynasty.MoveManager.dll'='7563b6e32745e79ddc8308d2ea48ca7336050271d8dc8c9ce1e75346a52232da'
 'GrimDynasty.WorldCoordinates.dll'='6365ad6a051d6ffbb4009824d98fa899bec89f5933dea383a9a3d98f193e71c7'
 'GrimDynasty.NavigationNearest.dll'='75f303c482679df6ba2c8f01aad7759ba9ed322ad6386100525b690cd9b9dfd0'
 'GrimDynasty.EntityMovement.dll'='85d848625fce7d5fc580feb04839898bd580a96f0c48308c3aca5b8ab142f84d'
 'GrimDynasty.GroundGeometry.dll'='61f523091f69bddbe510b64fbd6134daf0843efe919844a95d6d0d399ab77ed1'
}
$evidence=@{
 'docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin'='a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289'
 'docs\reverse-client\r1-ground-intersection-direct\05-GroundIntersectionDirect-0x1801df220-procedure_assembly.json'='4d4ae9e452737e215850afe7dc31a2133ac38b0bb151a307fdb1b21140447e5c'
 'docs\reverse-client\r1-ground-intersection-remaining\05-GroundIntersectionRemaining-0x180044660-procedure_assembly.json'='3d645ba1524af0a6d5e0827530077edbb459deacea975093d0e6d5e247c039b7'
 'docs\reverse-client\r1-ground-intersection-remaining\08-GroundIntersectionRemaining-0x1801cf830-procedure_assembly.json'='85a0810e274c8ceb4a1f85167c1212f33bc9ac364942b43b4706ea836966652a'
 'docs\reverse-client\r1-ground-face-and-unlink\05-GroundFaceUnlink-0x180051b30-procedure_assembly.json'='3ad0431fe5b4bb8e213fd3025177bc235ad920b2859dd03a700b88b849632de6'
 'docs\reverse-client\r1-ground-face-and-unlink\08-GroundFaceUnlink-0x1801cf420-procedure_assembly.json'='e50bc2eb66a19510e0d48279ab26e9165fe9d51e249e490b1e7ee9315cbb3bd6'
 'docs\object-lifecycle\offline-actor-remaining-dependencies\inspection.json'='6639fbb457720057edb6ea37834e6f5aa2534593b26020fda7d83044b73325c5'
 'docs\reverse-client\r1-navigation-nearest-constants\inspection.json'='02c4ec5592fdac55c1bf652b1eabdab326232c59150942f8222cd2da652d14e1'
 'docs\reverse-client\r1-ground-geometry-bindings\inspection.json'='8be1f21796126e33fae05d412b4690b8d7cf5618bda89593d4f6021230621d17'
 'docs\reverse-client\r1-ground-ray-tree\05-GroundRayTree-0x18003d780-procedure_assembly.json'='8a624a15cf7dcf167ee3c5afd5c4e1d24b64f92b17a526ee893431d55a9dffd6'
 'docs\reverse-client\r1-ground-intersection-remaining\17-GroundIntersectionRemaining-0x18003d760-procedure_assembly.json'='d1adafa15ee18d642815bec35e88d894f7ef8d25dba1f9b2f5ec3c36ef87542e'
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
if(Test-Path -LiteralPath $output){throw 'Existing triangle evidence preserved; output must be new'}
Check-File $priorResult $priorResultHash
$prior=Get-Content -LiteralPath $priorResult -Raw|ConvertFrom-Json
if($prior.status -ne 'passed-new-saved-ground-geometry-only' -or !$prior.saved_ground_pipeline -or $null -ne $prior.failure){throw 'Passed ground dependency result is required'}
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
 ground_plane=$false;ground_intersection=$false;saved_triangle_ray=$false;ground_ray_bounds=$false;native_load=$false;floor_collision=$false;material_resources_loaded=$false
 original_read=$false;original_executed=$false;backend_started=$false;gameplay_accepted=$false
 support_boundary='Saved member and passed GroundGeometry binary share the same OwnGroundData cells. Finite scalar projections of1DF220,044660,051B30,1CF420,1CF830 and03D780/03D760, explicit observed constants, per-operation binary32, true two-triangle topology, immediate distance/X/Z writes. OWN finite-domain/extent limits are not native failure contracts. Math.Sqrt rounded to binary32 is an OWN platform projection. No native Load callbacks, Block postconstruct/material selection, spatial query dispatch, world floor publication or gameplay acceptance. Previous ground suite is not rerun.'
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
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$library=Join-Path $output 'GrimDynasty.GroundTriangleIntersection.dll';$tests=Join-Path $output 'GroundTriangleIntersectionTests.exe'
 Run-Step 'compile-ground-triangle-intersection' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@((Join-Path $source 'GroundTriangleIntersectionContracts.cs'),(Join-Path $source 'GroundRayBoundsContracts.cs')))
 Run-Step 'compile-ground-triangle-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:GroundTriangleIntersectionTestProgram',"/out:$tests","/reference:$library")+$references+@((Join-Path $source 'GroundTriangleIntersectionTests.cs')))
 Run-Step 'new-ground-triangle-tests' $tests @((Join-Path $taskRoot 'docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin'))
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-ground-triangle-tests.log') -Raw|ConvertFrom-Json
 if($summary.status -ne 'passed' -or $summary.scenarios -ne 10 -or $summary.assertions -le 0 -or $summary.ground_plane -ne $true -or $summary.ground_intersection -ne $true -or $summary.saved_triangle_ray -ne $true -or $summary.native_load -ne $false -or $summary.floor_collision -ne $false -or $summary.gameplay_accepted -ne $false){throw 'Missing or unexpected triangle summary'}
 $report.test_results += [ordered]@{name='saved-ground-triangle-ray';kind='new';scenarios=$summary.scenarios;assertions=$summary.assertions}
 $boundsTests=Join-Path $output 'GroundRayBoundsTests.exe'
 Run-Step 'compile-ground-ray-bounds-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:GroundRayBoundsTestProgram',"/out:$boundsTests","/reference:$library")+$references+@((Join-Path $source 'GroundRayBoundsTests.cs')))
 Run-Step 'new-ground-ray-bounds-tests' $boundsTests @()
 $boundsSummary=Get-Content -LiteralPath (Join-Path $output 'new-ground-ray-bounds-tests.log') -Raw|ConvertFrom-Json
 if($boundsSummary.status -ne 'passed' -or $boundsSummary.scenarios -ne 8 -or $boundsSummary.assertions -le 0 -or $boundsSummary.original_executed -ne $false -or $boundsSummary.full_floor_collision -ne $false -or $boundsSummary.gameplay_accepted -ne $false){throw 'Missing or unexpected ray-bounds summary'}
 $report.test_results += [ordered]@{name='ground-ray-bounds';kind='new';scenarios=$boundsSummary.scenarios;assertions=$boundsSummary.assertions}
 Check-Inputs;foreach($name in $dependencies.Keys){Check-File (Join-Path $output $name) $dependencies[$name]}
 $report.ground_plane=$true;$report.ground_intersection=$true;$report.saved_triangle_ray=$true;$report.ground_ray_bounds=$true;$report.status='passed-new-saved-ground-triangle-ray-and-bounds-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 9|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;steps=$report.steps;test_results=$report.test_results;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 6
