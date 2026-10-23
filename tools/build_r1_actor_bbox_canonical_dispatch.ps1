param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$previous=Join-Path $taskRoot '.tooling\r1-game-movement-callback-contracts-validation'
$output=Join-Path $taskRoot '.tooling\r1-actor-bbox-canonical-dispatch-validation'
$callbackResult=Join-Path $previous 'result.json'
$callbackResultHash='7756201f00ff66c5808b0e8c579769d7255700ce2f89fac4d199f5cb03a9f1ff'
$oldBoundsResult=Join-Path $taskRoot '.tooling\r1-actor-bounding-box-validation\result.json'
$oldBoundsResultHash='ec1c9b8e916b3a6c2bcfec8b0d84ebd8fd84c62abcb27387edf26bc46512060f'
$oldSource=Join-Path $taskRoot 'docs\reverse-client\r1-actor-bbox-dispatch-revision\ActorBoundingBoxContracts.before.cs'
$oldSourceHash='1829bfffb43caba447b227345ef67d4315db692c99f40e69f96cd134d9c32a3d'
$sources=@{
 'ActorBoundingBoxContracts.cs'='a4f2ae0830567976a7d8b0a2351cdeae52e3d9b0689ecac73664ce59700d08a5'
 'ActorBoundingBoxTests.cs'='a478d4ac1eb8fc2afd1db181ec50fe02a10b4098c2af07df080d582039a81cbf'
 'ActorBoundingBoxDispatchIntegrationTests.cs'='c973516a760d868aac6e910059d379ce13f94c482b0d06d7a642641cd11a5e8b'
}
$dependencies=@{
 'GrimDynasty.Actions.dll'='3ea34074db9d5a67d7dffb702eb053b703dc3b754d0db442aeadd71247e15c58'
 'GrimDynasty.EntityMovement.dll'='85d848625fce7d5fc580feb04839898bd580a96f0c48308c3aca5b8ab142f84d'
 'GrimDynasty.GameMovementCallbacks.dll'='21aa8443153c7a06575f25c32b4f7ef53d73b8d20c0cdc8ccda11c532ee0f5ed'
 'GrimDynasty.MoveManager.dll'='7563b6e32745e79ddc8308d2ea48ca7336050271d8dc8c9ce1e75346a52232da'
 'GrimDynasty.NavigationCorridor.dll'='1afd6850ae3f2fc1c0f13c6562ce6b5749a3ea98d3b2e5792d67234d4c779e33'
 'GrimDynasty.NavigationCrowdAdapter.dll'='04215144406cd87357b98bd32911f96281453c4010e4f2f61e1c6997b065b73e'
 'GrimDynasty.NavigationCrowdState.dll'='3c37f78adff8799df3d1da6624145d4cf1037f44416c7e550fa904c48ecf25e0'
 'GrimDynasty.NavigationMeshTiles.dll'='0b8604244b7681e6f0f5e13a63c93f2f815069bbe74109e974938c94d9ee4d57'
 'GrimDynasty.NavigationNearest.dll'='75f303c482679df6ba2c8f01aad7759ba9ed322ad6386100525b690cd9b9dfd0'
 'GrimDynasty.NavigationTileData.dll'='b0459f94ca130467c2ec7f8c0379700250926c5a8eb8a3e6f617910a3bb02cd9'
 'GrimDynasty.WorldCoordinates.dll'='6365ad6a051d6ffbb4009824d98fa899bec89f5933dea383a9a3d98f193e71c7'
}
function Hash([string]$path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-Ancestors([string]$path){
 $cursor=[IO.Path]::GetFullPath($path)
 while($cursor){if(Test-Path -LiteralPath $cursor){$item=Get-Item -LiteralPath $cursor;if($item.Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Reparse path not permitted: '+$cursor)}};$parent=[IO.Directory]::GetParent($cursor);if($null -eq $parent){break};$cursor=$parent.FullName}
}
function Check-File([string]$path,[string]$expected){Check-Ancestors $path;$item=Get-Item -LiteralPath $path;if($expected -notmatch '^[0-9a-f]{64}$' -or $item.PSIsContainer -or (Hash $path) -ne $expected){throw ('Input identity changed: '+$path)}}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace, no arguments required'}
Check-Ancestors $output
if(!([IO.Path]::GetFullPath($output)).StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Output must stay inside fixed workspace'}
if(Test-Path -LiteralPath $output){throw 'Preserve existing validation; output must be new'}
Check-File $callbackResult $callbackResultHash;Check-File $oldBoundsResult $oldBoundsResultHash
$prior=Get-Content -LiteralPath $callbackResult -Raw|ConvertFrom-Json;$old=Get-Content -LiteralPath $oldBoundsResult -Raw|ConvertFrom-Json
if($prior.status -ne 'passed-new-callback-contracts-and-affected-prefix-regression-only' -or $null -ne $prior.failure -or $old.status -ne 'passed-new-actor-bounding-box-only' -or $null -ne $old.failure){throw 'Required prior validation is not passed'}
foreach($name in $dependencies.Keys){$record=@($prior.artifacts|Where-Object {$_.name -eq $name});if($record.Count -ne 1 -or $record[0].sha256 -ne $dependencies[$name]){throw ('Dependency not pinned by passed callback result: '+$name)}}
# Reuse the eight exact workspace evidence pins from the immutable passed
# bounds result. No original binary is opened and no old test EXE is invoked.
$evidence=@{};foreach($property in $old.native_evidence_pins.PSObject.Properties){$evidence.Add($property.Name,$property.Value)}
if($evidence.Count -ne 8){throw 'Unexpected original bounds evidence set'}
function Check-Inputs {
 Check-File $compiler $compilerHash;Check-File $callbackResult $callbackResultHash;Check-File $oldBoundsResult $oldBoundsResultHash;Check-File $oldSource $oldSourceHash
 foreach($name in $sources.Keys){Check-File (Join-Path $taskRoot ('src\reverse_client_actions\'+$name)) $sources[$name]}
 foreach($name in $dependencies.Keys){Check-File (Join-Path $previous $name) $dependencies[$name]}
 foreach($name in $evidence.Keys){Check-File (Join-Path $taskRoot $name) $evidence[$name]}
}
Check-Inputs
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash
 source_pins=$sources;dependency_pins=$dependencies;native_evidence_pins=$evidence;callback_result=$callbackResult;callback_result_sha256=$callbackResultHash;prior_bounds_result=$oldBoundsResult;prior_bounds_result_sha256=$oldBoundsResultHash
 previous_source=$oldSource;previous_source_sha256=$oldSourceHash;steps=@();test_results=@();artifacts=@();failure=$null
 old_test_suites_rerun=@('29 Actor bounding-box scenarios recompiled against canonical primary identities; justified by changed dispatch acceptance');old_executable_entrypoints_invoked=@()
 own_actor_bounding_box=$false;canonical_dispatch=$false;mesh_resource_production=$false;attached_id_traversal=$false
 original_read=$false;original_executed=$false;backend_started=$false;position_publication_validated=$false;gameplay_accepted=$false
 support_boundary='Actor bounded domain unchanged; now consumes exactly the existing GameMovementPrimaryTable.Character/Player identities shared with Created/Update. No per-call vptr switching, callback map, loaded-table claim, real mesh producer, attached-ID traversal, complete position publication or game acceptance.'
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
 foreach($name in $dependencies.Keys){Copy-Item -LiteralPath (Join-Path $previous $name) -Destination (Join-Path $output $name);Check-File (Join-Path $output $name) $dependencies[$name]}
 $references=@();foreach($name in ($dependencies.Keys|Sort-Object)){$references+='/reference:'+(Join-Path $output $name)}
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$library=Join-Path $output 'GrimDynasty.ActorBoundingBox.dll';$regression=Join-Path $output 'ActorBoundingBoxTests.exe';$integration=Join-Path $output 'ActorBoundingBoxDispatchIntegrationTests.exe'
 Run-Step 'compile-canonical-actor-bounds' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@((Join-Path $source 'ActorBoundingBoxContracts.cs')))
 Run-Step 'compile-affected-bounds-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:ActorBoundingBoxTestProgram',"/out:$regression","/reference:$library")+$references+@((Join-Path $source 'ActorBoundingBoxTests.cs')))
 Run-Step 'affected-bounds-regression' $regression @()
 $summary=Get-Content -LiteralPath (Join-Path $output 'affected-bounds-regression.log') -Raw|ConvertFrom-Json
 if($summary.status -ne 'passed' -or $summary.scenarios -ne 29 -or $summary.assertions -ne 444 -or $summary.own_actor_bounding_box -ne $true -or $summary.gameplay_accepted -ne $false){throw 'Missing or unexpected affected bounds summary'}
 $report.test_results += [ordered]@{name='bounds-canonical-dispatch-regression';kind='affected-regression';scenarios=$summary.scenarios;assertions=$summary.assertions}
 Run-Step 'compile-canonical-integration-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:ActorBoundingBoxDispatchIntegrationTestProgram',"/out:$integration","/reference:$library")+$references+@((Join-Path $source 'ActorBoundingBoxDispatchIntegrationTests.cs')))
 Run-Step 'new-canonical-integration-tests' $integration @()
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-canonical-integration-tests.log') -Raw|ConvertFrom-Json
 if($summary.status -ne 'passed' -or $summary.new_scenarios -ne 4 -or $summary.new_assertions -le 0 -or $summary.canonical_dispatch -ne $true){throw 'Missing or unexpected canonical integration summary'}
 $report.test_results += [ordered]@{name='same-primary-created-update-bounds';kind='new';scenarios=$summary.new_scenarios;assertions=$summary.new_assertions}
 Check-Inputs;foreach($name in $dependencies.Keys){Check-File (Join-Path $output $name) $dependencies[$name]}
 $report.own_actor_bounding_box=$true;$report.canonical_dispatch=$true;$report.status='passed-canonical-actor-bounds-integration-and-affected-regression-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}};$report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 9|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;test_results=$report.test_results;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 6
