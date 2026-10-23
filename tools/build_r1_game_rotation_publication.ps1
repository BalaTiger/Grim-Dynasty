param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$output=Join-Path $taskRoot '.tooling\r1-game-rotation-publication-validation'
$systemCrt='C:\Windows\System32\ucrtbase.dll'
$systemCrtHash='dedb7611e1abc1ff3852dcac3d4c93a048d729e25b061973b9969f22698f14ca'
$sources=@{
 'GameRotationPublicationContracts.cs'='3853bf9c2f4e561f645826fdefb55ff4ea4a0273021c659820961f55f1ad0416'
 'GameRotationPublicationTests.cs'='63869eb8b2df88f98809506286be472f131383043ed2b50a3aba1221e74e05c2'
}
$priorSpecs=@(
 [pscustomobject]@{directory='.tooling\r1-world-same-region-publication-validation';sha256='e61195d71263fe3d40ed725c4d3cfcb8de57a522d56b2e81d663fc3b9702489b';status='passed-new-world-same-region-publication-only';names=@('GrimDynasty.Actions.dll','GrimDynasty.ActorBoundingBox.dll','GrimDynasty.EntityMovement.dll','GrimDynasty.EntitySpatialTree.dll','GrimDynasty.GameMovementCallbacks.dll','GrimDynasty.MoveManager.dll','GrimDynasty.NavigationCorridor.dll','GrimDynasty.NavigationCrowdAdapter.dll','GrimDynasty.NavigationCrowdState.dll','GrimDynasty.NavigationMeshTiles.dll','GrimDynasty.NavigationNearest.dll','GrimDynasty.NavigationTileData.dll','GrimDynasty.WorldCoordinates.dll','GrimDynasty.WorldPublicationPrerequisites.dll','GrimDynasty.WorldPublicationGates.dll','GrimDynasty.WorldSameRegionPublication.dll')},
 [pscustomobject]@{directory='.tooling\r1-game-movement-math-validation';sha256='6efc90a43e9a306c9a47e250b0584c62622c9cd0b1785608472d4220abb63b35';status='passed-new-game-movement-math-contracts-only';names=@('GrimDynasty.GameMovementMath.dll')},
 [pscustomobject]@{directory='.tooling\r1-system-ucrt-rotation-validation-2';sha256='ca025983c2731fedf773a0235cfeaa1152e03388c22bed80e716c7b15dab67a1';status='passed-new-own-system-ucrt-rotation-only';names=@('GrimDynasty.SystemUcrtRotationMath.dll')}
)
$evidence=@{
 'docs\reverse-client\r1-game-callback-remaining-retry-1\11-GameCallbackRemaining-0x18005e160-procedure_assembly.json'='722a7bbea67a6242d0e06bc8db2dc6614b02a25ac872ec7c1bd046cfa9060a49'
 'docs\reverse-client\r1-game-movement-math\05-GameMovementMath-0x180591a10-procedure_assembly.json'='9af598bbe5f1c1996d7b89885d69318ff94bd74ba26e219f4efecc1a33eeb4f5'
 'docs\reverse-client\r1-position-publication-binding\inspection.json'='cc920aecf8f89daa22850eb8e23ce110ec180ea8e3a42d8cd610dcb0c94a8161'
 'docs\reverse-client\r1-game-rotation-math-imports\inspection.json'='1dc150ecf36c3012fbd1665992cac25ca1728a16af93981d83a0d13e1d951b46'
 'docs\reverse-client\r1-system-ucrt-rotation-profile.json'='def13e5755f5969dd042a4e058a73a38b77c5e5fa69574a4e246a32bb81d41e7'
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
# Only18 named DLLs from three pinned passed results. No old source rebuild,
# old test entrypoint, native game, Rea/Java or original file is invoked/read.
$dependencies=@{}
foreach($spec in $priorSpecs){
 $directory=Join-Path $taskRoot $spec.directory;$result=Join-Path $directory 'result.json';Check-File $result $spec.sha256
 $prior=Get-Content -LiteralPath $result -Raw|ConvertFrom-Json;if($prior.status -ne $spec.status -or $null -ne $prior.failure){throw ('Dependency validation not passed: '+$result)}
 foreach($name in $spec.names){
  $record=@($prior.artifacts|Where-Object {$_.name -eq $name});if($record.Count -ne 1 -or $record[0].sha256 -notmatch '^[0-9a-f]{64}$' -or $dependencies.ContainsKey($name)){throw ('Unexpected dependency identity: '+$name)}
  $dependencies.Add($name,[ordered]@{path=(Join-Path $directory $name);sha256=$record[0].sha256})
 }
}
if($dependencies.Count -ne 18){throw 'Unexpected dependency count'}
function Check-Inputs {
 Check-File $compiler $compilerHash;Check-File $systemCrt $systemCrtHash
 $crt=Get-Item -LiteralPath $systemCrt;if($crt.Length -ne 1399656 -or $crt.VersionInfo.FileVersion -ne '10.0.26100.9711 (WinBuild.160101.0800)'){throw 'Own UCRT version/size changed'}
 foreach($spec in $priorSpecs){Check-File (Join-Path (Join-Path $taskRoot $spec.directory) 'result.json') $spec.sha256}
 foreach($name in $sources.Keys){Check-File (Join-Path $taskRoot ('src\reverse_client_actions\'+$name)) $sources[$name]}
 foreach($name in $dependencies.Keys){Check-File $dependencies[$name].path $dependencies[$name].sha256}
 foreach($name in $evidence.Keys){Check-File (Join-Path $taskRoot $name) $evidence[$name]}
}
Check-Inputs
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash
 source_pins=$sources;dependency_pins=$dependencies;native_evidence_pins=$evidence;prior_results=$priorSpecs
 system_crt_path=$systemCrt;system_crt_file_sha256=$systemCrtHash;system_crt_file_version='10.0.26100.9711 (WinBuild.160101.0800)'
 steps=@();test_results=@();artifacts=@();failure=$null;old_test_suites_rerun=@();old_executable_entrypoints_invoked=@()
 own_rotation_publication=$false;real_floor_input_production=$false;original_runtime_validated=$false;gameplay_accepted=$false
 system_library_load_requested=$false;system_library_loaded=$null;native_math_calls=$null;memory_image_hashed=$false
 original_read=$false;original_executed=$false;backend_started=$false;security_settings_changed=$false;floating_control_write_mask=0
 support_boundary='Own stable same-Entity composition of actual Game05E160 four branches and591A10 ordered axis rotation, existing Basis591B80/Compose07E6F0, and World21BB40/Level12F4F0 to shared bbox and nonnull spatial production. Exact saved constants selected explicitly; evidenced UCOMISS unordered direct arms are retained, including vertical zero-horizontal normalization. Finite bounds apply only at actual UCRT calls; no blanket finite gate or NaN-payload/FP-status/trap equivalence. System32 UCRT is an own platform choice, not original loaded-runtime equivalence. Raw AL is a turn-branch return, not movement success. Real CrowdAgentMoved/floor/input/mesh producers, attached IDs, cross-region paths and game acceptance remain unclosed. No arbitrary callbacks, native ABI/EH/concurrency or special FP modes claimed.'
}
New-Item -ItemType Directory -Path $output|Out-Null
function Run-Step([string]$name,[string]$program,[string[]]$arguments){
 $stdout=Join-Path $output ($name+'.log');$stderr=Join-Path $output ($name+'.stderr.log');$oldPreference=$ErrorActionPreference
 try{$ErrorActionPreference='Continue'; & $program @arguments 1> $stdout 2> $stderr;$ok=$?;$code=$LASTEXITCODE}finally{$ErrorActionPreference=$oldPreference}
 $report.steps += [ordered]@{name=$name;program=$program;arguments=$arguments;invocation_succeeded=$ok;exit_code=$code}
 if(!$ok -or $code -ne 0){throw "Step $name failed (invocation=$ok, exit=$code); logs retained"}
 if((Get-Item -LiteralPath $stderr).Length -ne 0 -or ($name.StartsWith('compile-') -and (Get-Item -LiteralPath $stdout).Length -ne 0)){throw ('Unexpected diagnostics: '+$name)}
}
try{
 foreach($name in $dependencies.Keys){Copy-Item -LiteralPath $dependencies[$name].path -Destination (Join-Path $output $name);Check-File (Join-Path $output $name) $dependencies[$name].sha256}
 $references=@();foreach($name in ($dependencies.Keys|Sort-Object)){$references+='/reference:'+(Join-Path $output $name)}
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$library=Join-Path $output 'GrimDynasty.GameRotationPublication.dll';$tests=Join-Path $output 'GameRotationPublicationTests.exe'
 Run-Step 'compile-game-rotation-publication' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@((Join-Path $source 'GameRotationPublicationContracts.cs')))
 Run-Step 'compile-game-rotation-publication-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:GameRotationPublicationTests',"/out:$tests","/reference:$library")+$references+@((Join-Path $source 'GameRotationPublicationTests.cs')))
 $report.system_library_load_requested=$true
 Run-Step 'new-game-rotation-publication-tests' $tests @()
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-game-rotation-publication-tests.log') -Raw|ConvertFrom-Json
 if($summary.status -ne 'passed' -or $summary.scenarios -ne 26 -or $summary.assertions -le 0 -or $summary.native_math_calls -le 0 -or $summary.own_rotation_publication -ne $true -or $summary.real_floor_input_production -ne $false -or $summary.original_runtime_validated -ne $false -or $summary.gameplay_accepted -ne $false){throw 'Missing or unexpected rotation summary'}
 $report.test_results += [ordered]@{name='new-real-rotation-same-entity-publication';kind='new';scenarios=$summary.scenarios;assertions=$summary.assertions;native_math_calls=$summary.native_math_calls}
 Check-Inputs;foreach($name in $dependencies.Keys){Check-File (Join-Path $output $name) $dependencies[$name].sha256}
 $report.system_library_loaded=$true;$report.native_math_calls=$summary.native_math_calls;$report.own_rotation_publication=$true;$report.status='passed-new-game-rotation-publication-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}};$report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;test_results=$report.test_results;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 6
