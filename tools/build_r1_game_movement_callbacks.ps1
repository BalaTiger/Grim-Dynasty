param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$entityPrevious=Join-Path $taskRoot '.tooling\r1-entity-movement-validation'
$crowdPrevious=Join-Path $taskRoot '.tooling\r1-crowd-path-validation'
$output=Join-Path $taskRoot '.tooling\r1-game-movement-callback-contracts-validation'
$entityResultHash='8f3ef00be0d94da17f6d3ec4ae79081a0c562e9492a9e4c6f6d0ae6c49a77fff'
$crowdResultHash='955a7b168d401088e1723522545709eea8da42653fc086576496c5ab8a526b4e'
$sourcePins=@{
 'EntityMovementContracts.cs'='74b2f2348f36562208616df81af2b6dcc56e6fdd5a144b6b3c68e3e1379a6348'
 'GameMovementCallbackContracts.cs'='5fcd747af2a3ecc44beeee1f783b04e0a0057f01c808eb708c385dcb6b929b7e'
 'GameMovementCallbackTests.cs'='00ba4d8d81fb5045baa1c3a4784641176cc04e967917f27ef08fbc5c39c7f98f'
}
# Existing validated dependencies are copied unchanged. Only Entity/Callback
# libraries and the combined new/affected test source compile. Old15 getter,
# navigation suites and existing EXE entry points do not run.
$nativeEvidencePins=@{
 'docs\reverse-client\r1-game-callback-remaining-retry-1\summary.json'='f6a70555b8ab3df2e8cd5f2de5a64108d6bca32ce2cee7d9b0269df28de97a29'
 'docs\reverse-client\r1-game-callback-remaining-retry-1\05-GameCallbackRemaining-0x18005b730-procedure_assembly.json'='59277ab2238b9722010af54c8fd5431bbd8fda44d9a42ebc8f86e8699ffa3ab5'
 'docs\reverse-client\r1-game-callback-remaining-retry-1\08-GameCallbackRemaining-0x180591480-procedure_assembly.json'='ff230e4b15a3ad64d428a07fd88fba77b516314b2ba6ced853948866f29e5752'
 'docs\reverse-client\r1-actor-radius\summary.json'='799e5728ca5c792bab078bf361cb952fdae1827ba103bfbd15b40be61f78c2de'
 'docs\reverse-client\r1-actor-radius\05-ActorRadius-0x18002b410-procedure_assembly.json'='52888d1fb0cf79270493b7c8cdd968278c048920b44c8b8ad34327657223558d'
}
$entityPins=@{
 'GrimDynasty.Actions.dll'='3ea34074db9d5a67d7dffb702eb053b703dc3b754d0db442aeadd71247e15c58'
 'GrimDynasty.MoveManager.dll'='7563b6e32745e79ddc8308d2ea48ca7336050271d8dc8c9ce1e75346a52232da'
 'GrimDynasty.WorldCoordinates.dll'='6365ad6a051d6ffbb4009824d98fa899bec89f5933dea383a9a3d98f193e71c7'
 'GrimDynasty.NavigationNearest.dll'='75f303c482679df6ba2c8f01aad7759ba9ed322ad6386100525b690cd9b9dfd0'
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
if(Test-Path -LiteralPath $output){throw 'Existing callback evidence preserved; output must be new'}
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
 foreach($name in $nativeEvidencePins.Keys){Check-File (Join-Path $taskRoot $name) $nativeEvidencePins[$name]}
 foreach($name in $entityPins.Keys){Check-File (Join-Path $entityPrevious $name) $entityPins[$name]}
 foreach($name in $crowdPins.Keys){Check-File (Join-Path $crowdPrevious $name) $crowdPins[$name]}
}
Check-Inputs
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash
 entity_prior_result=$entityResult;entity_prior_result_sha256=$entityResultHash
 crowd_prior_result=$crowdResult;crowd_prior_result_sha256=$crowdResultHash
 source_pins=$sourcePins;native_evidence_pins=$nativeEvidencePins;entity_dependency_pins=$entityPins;crowd_dependency_pins=$crowdPins
 steps=@();test_results=@();artifacts=@();old_test_suites_rerun=@('8 affected callback-prefix groups recompiled with proven-field failure addresses; no old EXE invoked');old_executable_entrypoints_invoked=@()
 new_callback_contracts=$false;conditional_created_validated=$false;conditional_update_validated=$false;full_registration_validated=$false
 original_read=$false;original_executed=$false;saved_map_read=$false;backend_started=$false
 loaded_player_validated=$false;character_movement_validated=$false;gameplay_accepted=$false;failure=$null
 support_boundary='Radius02B410/action05B730/parent591480 and complete Created/Update in explicit own shared-state domain. Unknown fields, class cycles and unbound Engine state stop Unsupported. No MOVED/RotateTowards, full registration, loaded state, position publication, native ABI/FP-mode compatibility or game acceptance.'
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
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$entityLibrary=Join-Path $output 'GrimDynasty.EntityMovement.dll'
 Run-Step 'compile-entity-with-radius' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$entityLibrary")+$references+@((Join-Path $source 'EntityMovementContracts.cs')))
 $references += '/reference:'+$entityLibrary
 $library=Join-Path $output 'GrimDynasty.GameMovementCallbacks.dll'
 Run-Step 'compile-game-callback-contracts' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@((Join-Path $source 'GameMovementCallbackContracts.cs')))
 $tests=Join-Path $output 'GameMovementCallbackTests.exe'
 Run-Step 'compile-game-callback-contract-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:GameMovementCallbackTests',"/out:$tests","/reference:$library")+$references+@((Join-Path $source 'GameMovementCallbackTests.cs')))
 Run-Step 'new-and-affected-game-callback-tests' $tests @()
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-and-affected-game-callback-tests.log') -Raw|ConvertFrom-Json
 if($summary.status -ne 'passed' -or $summary.scenarios -ne 20 -or $summary.new_scenarios -ne 12 -or $summary.new_assertions -le 0 -or $summary.affected_regression_scenarios -ne 8 -or $summary.affected_regression_assertions -ne 79 -or $summary.assertions -ne ($summary.new_assertions+$summary.affected_regression_assertions) -or $summary.native_execution -ne $false -or $summary.conditional_created_update -ne $true -or $summary.position_movement -ne $false){throw 'Missing or unexpected new/affected suite summary'}
 $report.test_results += [ordered]@{name='new-radius-action-parent-and-complete-callback-contracts';kind='new';scenarios=$summary.new_scenarios;assertions=$summary.new_assertions}
 $report.test_results += [ordered]@{name='affected-callback-prefix-regression';kind='affected-regression';scenarios=$summary.affected_regression_scenarios;assertions=$summary.affected_regression_assertions}
 Check-Inputs
 foreach($name in $entityPins.Keys){Check-File (Join-Path $output $name) $entityPins[$name]}
 foreach($name in $crowdPins.Keys){Check-File (Join-Path $output $name) $crowdPins[$name]}
 $report.new_callback_contracts=$true;$report.conditional_created_validated=$true;$report.conditional_update_validated=$true;$report.status='passed-new-callback-contracts-and-affected-prefix-regression-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 9|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;steps=$report.steps;test_results=$report.test_results;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 6
