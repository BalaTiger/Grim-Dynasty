param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$previous=Join-Path $taskRoot '.tooling\r1-ground-geometry-validation'
$output=Join-Path $taskRoot '.tooling\r1-ground-material-candidates-validation'
$priorResult=Join-Path $previous 'result.json'
$priorResultHash='b2732e8832be58b5b2856789fab21ef6b4b22fc67bb435e940d46a241d0a2ecd'
$sources=@{
 'GroundGeometryContracts.cs'='485d779e8f3712ab8fca4d055b2f935c10aea4cae6233335bd83eeb83926d854'
 'GroundMaterialCandidateContracts.cs'='c68f1681942bf2511b943afa28b6d3be541b61ee624fff8013169f7bdccab002'
 'GroundMaterialCandidateTests.cs'='8aaf6f37148147a55c84f35c17e17eed2d56e527275b8b1873aeac9b164631fc'
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
 'docs\reverse-client\r1-ground-ray-tree\23-GroundRayTree-0x1801d12f0-procedure_assembly.json'='9367198fcf07978f6ba927aa80a6dbf351c66a8ac0a3a79bd0c2ad217e544cfc'
 'docs\reverse-client\r1-ground-intersection-remaining\11-GroundIntersectionRemaining-0x1801deb80-procedure_assembly.json'='5e573fa7eb94b60e885304656fa1dfb997f35640b5d28163e8554b3fe3ac1ba8'
 'docs\reverse-client\r1-ground-face-and-unlink\13-GroundFaceUnlink-0x1801ce0e0-procedure_pseudo_code.json'='d1051c79ca9548834d1dc4bb802293f9418711489e9c7f28bc8bda8607fbfa43'
 'docs\reverse-client\r1-ground-face-and-unlink\14-GroundFaceUnlink-0x1801ce0e0-procedure_assembly.json'='61798838ae53d663ffe29a1fbab4b6c282e8e96cf43e561b2ef118b79d3affbb'
 'docs\object-lifecycle\offline-binary-bits-fill-prefix-walk-v3\0483a0-instructions.json'='29a3815ce82ea25d998d3ffd7472f918bbf1ca1e2e5bd0239a109dda92611f3c'
 'docs\object-lifecycle\offline-binary-bits-fill-prefix-walk-v3\inspection.json'='089f04a6e64e6fc5f11fee85e8b20d263d1eacf954626f90342bd86d780b904d'
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
if(Test-Path -LiteralPath $output){throw 'Existing candidate evidence preserved; output must be new'}
Check-File $priorResult $priorResultHash
$prior=Get-Content -LiteralPath $priorResult -Raw|ConvertFrom-Json
if($prior.status -ne 'passed-new-saved-ground-geometry-only' -or !$prior.saved_ground_pipeline -or $null -ne $prior.failure){throw 'Passed ground dependency result is required'}
$oldTests=@($prior.test_results|Where-Object {$_.name -eq 'saved-ground-sample-bounds'})
if($oldTests.Count -ne 1 -or $oldTests[0].scenarios -ne 8 -or $oldTests[0].assertions -ne 55){throw 'Expected old8/55 evidence, without rerunning it'}
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
 geometry_recompile_reason='Only TypeRecordCount, ReadPacked120, TypeRecordAt and ReadWeight shared-data accessors were added. Existing geometry logic is exercised only where the new candidate integration needs real Block bounds; old8/55 suite is not rerun.'
 saved_candidate_pipeline=$false;native_load=$false;full_construct_blocks=$false;floor_collision=$false;material_resources_loaded=$false
 original_read=$false;original_executed=$false;backend_started=$false;gameplay_accepted=$false
 support_boundary='Engine1DEB80 occupancy membership and1CE0E0 normal Begin insertion in an own bounded domain, shared decoded Ground/Block/packed data, finite binary32 and nonwrapping indices. Normal own allocation supports256 records and actual1.5x capacity growth. Arbitrary aliases, concurrent mutation and native allocator failure/cleanup remain unsupported. Materials stay Unknown; no native full Load, grass cleanup, complete tree/collision or gameplay claim.'
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
 $source=Join-Path $taskRoot 'src\reverse_client_actions'
 $geometry=Join-Path $output 'GrimDynasty.GroundGeometry.dll';$library=Join-Path $output 'GrimDynasty.GroundMaterialCandidates.dll';$tests=Join-Path $output 'GroundMaterialCandidateTests.exe'
 Run-Step 'compile-ground-geometry-accessors' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$geometry")+$references+@((Join-Path $source 'GroundGeometryContracts.cs')))
 Run-Step 'compile-material-candidates' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library","/reference:$geometry")+$references+@((Join-Path $source 'GroundMaterialCandidateContracts.cs')))
 Run-Step 'compile-candidate-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:GroundMaterialCandidateTestProgram',"/out:$tests","/reference:$geometry","/reference:$library")+$references+@((Join-Path $source 'GroundMaterialCandidateTests.cs')))
 Run-Step 'new-material-candidate-tests' $tests @((Join-Path $taskRoot 'docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin'))
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-material-candidate-tests.log') -Raw|ConvertFrom-Json
 if($summary.status -ne 'passed' -or $summary.scenarios -ne 12 -or $summary.assertions -le 0 -or $summary.real_blocks -ne 225 -or $summary.real_nonempty_blocks -le 0 -or $summary.real_candidate_memberships -le 0 -or $summary.real_membership_manifest_sha256 -notmatch '^[0-9a-f]{64}$' -or $summary.saved_member_sha256 -ne $evidence['docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin'] -or $summary.native_load -ne $false -or $summary.resolved_materials -ne $false -or $summary.full_construct_blocks -ne $false -or $summary.floor_collision -ne $false -or $summary.gameplay_accepted -ne $false){throw 'Missing or unexpected candidate summary'}
 $report.test_results += [ordered]@{name='ground-material-candidates-and-real225-blocks';kind='new';scenarios=$summary.scenarios;assertions=$summary.assertions;real_blocks=$summary.real_blocks;real_nonempty_blocks=$summary.real_nonempty_blocks;real_candidate_memberships=$summary.real_candidate_memberships;real_membership_manifest_sha256=$summary.real_membership_manifest_sha256}
 Check-Inputs;foreach($name in $dependencies.Keys){Check-File (Join-Path $output $name) $dependencies[$name]}
 $report.saved_candidate_pipeline=$true;$report.status='passed-new-ground-material-candidates-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 9|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;steps=$report.steps;test_results=$report.test_results;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 6
