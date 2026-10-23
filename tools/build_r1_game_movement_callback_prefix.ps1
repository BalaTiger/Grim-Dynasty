param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$entityPrevious=Join-Path $taskRoot '.tooling\r1-entity-movement-validation'
$crowdPrevious=Join-Path $taskRoot '.tooling\r1-crowd-path-validation'
$output=Join-Path $taskRoot '.tooling\r1-game-movement-callback-prefix-validation'
$entityResultHash='8f3ef00be0d94da17f6d3ec4ae79081a0c562e9492a9e4c6f6d0ae6c49a77fff'
$crowdResultHash='955a7b168d401088e1723522545709eea8da42653fc086576496c5ab8a526b4e'
$sourcePins=@{
 'GameMovementCallbackContracts.cs'='9681e1d5815554d251491725f1ed4c9e0d7f54c8d773c4f14bec907567d73b40'
 'GameMovementCallbackTests.cs'='53ae8551e672102084b9d9b96808592184aca1e4f7db640df8a0109f266cab56'
}
# These are existing validated libraries, copied unchanged. Only the two new
# callback prefix sources compile; no existing test executable runs.
$entityPins=@{
 'GrimDynasty.Actions.dll'='3ea34074db9d5a67d7dffb702eb053b703dc3b754d0db442aeadd71247e15c58'
 'GrimDynasty.MoveManager.dll'='7563b6e32745e79ddc8308d2ea48ca7336050271d8dc8c9ce1e75346a52232da'
 'GrimDynasty.WorldCoordinates.dll'='6365ad6a051d6ffbb4009824d98fa899bec89f5933dea383a9a3d98f193e71c7'
 'GrimDynasty.NavigationNearest.dll'='75f303c482679df6ba2c8f01aad7759ba9ed322ad6386100525b690cd9b9dfd0'
 'GrimDynasty.EntityMovement.dll'='1e49dc73df87d6912b06c1535de41d1a553f23380d59dafcf4397b22818f9e13'
}
$crowdPins=@{
 'GrimDynasty.NavigationCrowdState.dll'='3c37f78adff8799df3d1da6624145d4cf1037f44416c7e550fa904c48ecf25e0'
 'GrimDynasty.NavigationCrowdAdapter.dll'='04215144406cd87357b98bd32911f96281453c4010e4f2f61e1c6997b065b73e'
 'GrimDynasty.NavigationCorridor.dll'='1afd6850ae3f2fc1c0f13c6562ce6b5749a3ea98d3b2e5792d67234d4c779e33'
 'GrimDynasty.NavigationMeshTiles.dll'='0b8604244b7681e6f0f5e13a63c93f2f815069bbe74109e974938c94d9ee4d57'
 'GrimDynasty.NavigationTileData.dll'='b0459f94ca130467c2ec7f8c0379700250926c5a8eb8a3e6f617910a3bb02cd9'
}
function Hash([string]$path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-File([string]$path,[string]$expected){
 if($expected -notmatch '^[0-9a-f]{64}$'){throw ('Unfrozen input: '+$path)}
 $item=Get-Item -LiteralPath $path
 if($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw ('Unexpected or redirected input: '+$path)}
 if((Hash $path) -ne $expected){throw ('Input fingerprint changed: '+$path)}
}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace, no arguments required'}
if(Test-Path -LiteralPath $output){throw 'Existing callback prefix evidence preserved; output must be new'}
$entityResult=Join-Path $entityPrevious 'result.json';$crowdResult=Join-Path $crowdPrevious 'result.json'
Check-File $entityResult $entityResultHash;Check-File $crowdResult $crowdResultHash
$entityOld=Get-Content -LiteralPath $entityResult -Raw|ConvertFrom-Json
$crowdOld=Get-Content -LiteralPath $crowdResult -Raw|ConvertFrom-Json
if($entityOld.status -ne 'passed-new-entity-getter-contracts-only' -or !$entityOld.new_entity_getter_contracts -or $null -ne $entityOld.failure){throw 'Passed Entity dependency is required'}
if($crowdOld.status -ne 'passed-new-crowd-waypoint-contracts-and-saved-resource-continuation-only' -or !$crowdOld.new_path_contracts -or $null -ne $crowdOld.failure){throw 'Passed Crowd dependency is required'}
foreach($name in $entityPins.Keys){$record=@($entityOld.artifacts|Where-Object {$_.name -eq $name});if($record.Count -ne 1 -or $record[0].sha256 -ne $entityPins[$name]){throw ('Entity dependency not pinned by passed result: '+$name)}}
foreach($name in $crowdPins.Keys){$record=@($crowdOld.artifacts|Where-Object {$_.name -eq $name});if($record.Count -ne 1 -or $record[0].sha256 -ne $crowdPins[$name]){throw ('Crowd dependency not pinned by passed result: '+$name)}}
function Check-Inputs {
 Check-File $compiler $compilerHash;Check-File $entityResult $entityResultHash;Check-File $crowdResult $crowdResultHash
 foreach($name in $sourcePins.Keys){Check-File (Join-Path $taskRoot ('src\reverse_client_actions\'+$name)) $sourcePins[$name]}
 foreach($name in $entityPins.Keys){Check-File (Join-Path $entityPrevious $name) $entityPins[$name]}
 foreach($name in $crowdPins.Keys){Check-File (Join-Path $crowdPrevious $name) $crowdPins[$name]}
}
Check-Inputs
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash
 entity_prior_result=$entityResult;entity_prior_result_sha256=$entityResultHash
 crowd_prior_result=$crowdResult;crowd_prior_result_sha256=$crowdResultHash
 source_pins=$sourcePins;entity_dependency_pins=$entityPins;crowd_dependency_pins=$crowdPins
 steps=@();test_results=@();artifacts=@();old_test_suites_rerun=@();old_executable_entrypoints_invoked=@()
 new_callback_prefixes=$false;complete_created_validated=$false;complete_update_validated=$false;full_registration_validated=$false
 original_read=$false;original_executed=$false;saved_map_read=$false;backend_started=$false
 loaded_player_validated=$false;character_movement_validated=$false;gameplay_accepted=$false;failure=$null
 support_boundary='Shared Entity/Agent callback prefixes and two extracted call sites only. Radius02B410/action05B730/parent591480 stop Unsupported; no successful full Created/Update, full registration, loaded state, position movement, native ABI or game acceptance.'
}
New-Item -ItemType Directory -Path $output|Out-Null
function Run-Step([string]$name,[string]$program,[string[]]$arguments){
 $stdout=Join-Path $output ($name+'.log');$stderr=Join-Path $output ($name+'.stderr.log');$oldPreference=$ErrorActionPreference
 try{$ErrorActionPreference='Continue'; & $program @arguments 1> $stdout 2> $stderr; $invocationSucceeded=$?; $code=$LASTEXITCODE}finally{$ErrorActionPreference=$oldPreference}
 $report.steps += [ordered]@{name=$name;program=$program;arguments=$arguments;invocation_succeeded=$invocationSucceeded;exit_code=$code}
 if(!$invocationSucceeded -or $code -ne 0){throw "Step $name failed (invocation=$invocationSucceeded, exit=$code); logs retained"}
 if($name.StartsWith('compile-') -and (((Get-Item -LiteralPath $stdout).Length -ne 0) -or ((Get-Item -LiteralPath $stderr).Length -ne 0))){throw ('Unexpected compiler diagnostics: '+$name)}
 if((Get-Item -LiteralPath $stderr).Length -ne 0){throw ('Unexpected stderr: '+$name)}
}
try{
 foreach($name in $entityPins.Keys){Copy-Item -LiteralPath (Join-Path $entityPrevious $name) -Destination (Join-Path $output $name);Check-File (Join-Path $output $name) $entityPins[$name]}
 foreach($name in $crowdPins.Keys){Copy-Item -LiteralPath (Join-Path $crowdPrevious $name) -Destination (Join-Path $output $name);Check-File (Join-Path $output $name) $crowdPins[$name]}
 $references=@();foreach($name in (@($entityPins.Keys)+@($crowdPins.Keys)|Sort-Object)){$references += '/reference:'+(Join-Path $output $name)}
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$library=Join-Path $output 'GrimDynasty.GameMovementCallbacks.dll'
 Run-Step 'compile-game-callback-prefixes' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@((Join-Path $source 'GameMovementCallbackContracts.cs')))
 $tests=Join-Path $output 'GameMovementCallbackTests.exe'
 Run-Step 'compile-game-callback-prefix-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:GameMovementCallbackTests',"/out:$tests","/reference:$library")+$references+@((Join-Path $source 'GameMovementCallbackTests.cs')))
 Run-Step 'new-game-callback-prefix-tests' $tests @()
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-game-callback-prefix-tests.log') -Raw|ConvertFrom-Json
 if($summary.status -ne 'passed' -or $summary.scenarios -ne 8 -or $summary.assertions -le 0 -or $summary.native_execution -ne $false -or $summary.complete_created_or_update -ne $false -or $summary.position_movement -ne $false){throw 'Missing or unexpected new-prefix-suite summary'}
 $report.test_results += [ordered]@{name='new-game-callback-prefix-tests';kind='new';scenarios=$summary.scenarios;assertions=$summary.assertions}
 Check-Inputs
 foreach($name in $entityPins.Keys){Check-File (Join-Path $output $name) $entityPins[$name]}
 foreach($name in $crowdPins.Keys){Check-File (Join-Path $output $name) $crowdPins[$name]}
 $report.new_callback_prefixes=$true;$report.status='passed-new-game-movement-callback-prefixes-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 9|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;steps=$report.steps;test_results=$report.test_results;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 6
