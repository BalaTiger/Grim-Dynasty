param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$worldPrevious=Join-Path $taskRoot '.tooling\r1-world-coordinate-validation'
$pointPrevious=Join-Path $taskRoot '.tooling\r1-crowd-path-validation'
$output=Join-Path $taskRoot '.tooling\r1-entity-movement-validation'
$worldResultHash='93644347fd4018108c3b94c09142a24f81141da276a5a38b7f55ac138e6e28ac'
$pointResultHash='955a7b168d401088e1723522545709eea8da42653fc086576496c5ab8a526b4e'
$sourcePins=@{
 'EntityMovementContracts.cs'='5e68dfd6c2214f30dfc2749b2301ebdc067656cc2df8d7ece1017398a976f210'
 'EntityMovementTests.cs'='ad8edbd09cfb5b28a350bd320d19910b6db70e4fbad460072d14989114c96902'
}
# MoveManager is WorldCoordinates' existing transitive Displacement3 type
# dependency. It is copied, never rebuilt or run. No prior EXE is invoked.
$worldPins=@{
 'GrimDynasty.Actions.dll'='3ea34074db9d5a67d7dffb702eb053b703dc3b754d0db442aeadd71247e15c58'
 'GrimDynasty.MoveManager.dll'='7563b6e32745e79ddc8308d2ea48ca7336050271d8dc8c9ce1e75346a52232da'
 'GrimDynasty.WorldCoordinates.dll'='6365ad6a051d6ffbb4009824d98fa899bec89f5933dea383a9a3d98f193e71c7'
}
$pointPins=@{
 'GrimDynasty.NavigationNearest.dll'='75f303c482679df6ba2c8f01aad7759ba9ed322ad6386100525b690cd9b9dfd0'
}
function Hash([string]$path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-File([string]$path,[string]$expected){
 if($expected -notmatch '^[0-9a-f]{64}$'){throw ('Unfrozen input: '+$path)}
 $item=Get-Item -LiteralPath $path
 if($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw ('Unexpected or redirected input: '+$path)}
 if((Hash $path) -ne $expected){throw ('Input fingerprint changed: '+$path)}
}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace, no arguments required'}
if(Test-Path -LiteralPath $output){throw 'Existing entity-getter evidence preserved; output must be new'}
$worldResult=Join-Path $worldPrevious 'result.json';$pointResult=Join-Path $pointPrevious 'result.json'
Check-File $worldResult $worldResultHash
Check-File $pointResult $pointResultHash
$worldOld=Get-Content -LiteralPath $worldResult -Raw|ConvertFrom-Json
$pointOld=Get-Content -LiteralPath $pointResult -Raw|ConvertFrom-Json
if($worldOld.status -ne 'passed-isolated-world-coordinate-contracts-only' -or !$worldOld.isolated_world_coordinate_contracts -or $null -ne $worldOld.failure){throw 'Passed WorldCoordinates dependency is required'}
if($pointOld.status -ne 'passed-new-crowd-waypoint-contracts-and-saved-resource-continuation-only' -or !$pointOld.new_path_contracts -or $null -ne $pointOld.failure){throw 'Passed point-view dependency is required'}
foreach($name in $worldPins.Keys){$record=@($worldOld.artifacts|Where-Object {$_.name -eq $name});if($record.Count -ne 1 -or $record[0].sha256 -ne $worldPins[$name]){throw ('Dependency not uniquely pinned by passed result: '+$name)}}
foreach($name in $pointPins.Keys){$record=@($pointOld.artifacts|Where-Object {$_.name -eq $name});if($record.Count -ne 1 -or $record[0].sha256 -ne $pointPins[$name]){throw ('Point-view dependency not uniquely pinned by passed result: '+$name)}}
function Check-Inputs {
 Check-File $compiler $compilerHash
 Check-File $worldResult $worldResultHash
 Check-File $pointResult $pointResultHash
 foreach($name in $sourcePins.Keys){Check-File (Join-Path $taskRoot ('src\reverse_client_actions\'+$name)) $sourcePins[$name]}
 foreach($name in $worldPins.Keys){Check-File (Join-Path $worldPrevious $name) $worldPins[$name]}
 foreach($name in $pointPins.Keys){Check-File (Join-Path $pointPrevious $name) $pointPins[$name]}
}
Check-Inputs
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash
 world_prior_result=$worldResult;world_prior_result_sha256=$worldResultHash
 point_prior_result=$pointResult;point_prior_result_sha256=$pointResultHash
 source_pins=$sourcePins;world_dependency_pins=$worldPins;point_dependency_pins=$pointPins
 steps=@();test_results=@();artifacts=@();old_test_suites_rerun=@();old_executable_entrypoints_invoked=@()
 new_entity_getter_contracts=$false;parent_transform_success_validated=$false;position_publication_validated=$false
 original_read=$false;original_executed=$false;loaded_player_validated=$false;character_movement_validated=$false;gameplay_accepted=$false;failure=$null
 support_boundary='Seven recovered getters in explicit own memory views; parent coordinate slots180/188 and22A950 remain Unsupported; no loaded player, position publication, native lifetime/MXCSR or movement validation.'
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
 foreach($name in $worldPins.Keys){Copy-Item -LiteralPath (Join-Path $worldPrevious $name) -Destination (Join-Path $output $name);Check-File (Join-Path $output $name) $worldPins[$name]}
 foreach($name in $pointPins.Keys){Copy-Item -LiteralPath (Join-Path $pointPrevious $name) -Destination (Join-Path $output $name);Check-File (Join-Path $output $name) $pointPins[$name]}
 $references=@();foreach($name in @('GrimDynasty.Actions.dll','GrimDynasty.MoveManager.dll','GrimDynasty.WorldCoordinates.dll','GrimDynasty.NavigationNearest.dll')){$references += '/reference:'+(Join-Path $output $name)}
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$library=Join-Path $output 'GrimDynasty.EntityMovement.dll'
 Run-Step 'compile-entity-getters' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@((Join-Path $source 'EntityMovementContracts.cs')))
 $tests=Join-Path $output 'EntityMovementTests.exe'
 Run-Step 'compile-entity-getter-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:EntityMovementTestProgram',"/out:$tests","/reference:$library")+$references+@((Join-Path $source 'EntityMovementTests.cs')))
 Run-Step 'new-entity-getter-memory-tests' $tests @()
 $summaries=[regex]::Matches((Get-Content -LiteralPath (Join-Path $output 'new-entity-getter-memory-tests.log') -Raw),'(?m)^PASS (\d+) scenarios, (\d+) assertions;')
 if($summaries.Count -ne 1 -or [int]$summaries[0].Groups[1].Value -ne 15 -or [int]$summaries[0].Groups[2].Value -le 0){throw 'Missing or unexpected unique new-suite summary'}
 $report.test_results += [ordered]@{name='new-entity-getter-memory-tests';kind='new';scenarios=[int]$summaries[0].Groups[1].Value;assertions=[int]$summaries[0].Groups[2].Value}
 Check-Inputs
 foreach($name in $worldPins.Keys){Check-File (Join-Path $output $name) $worldPins[$name]}
 foreach($name in $pointPins.Keys){Check-File (Join-Path $output $name) $pointPins[$name]}
 $report.new_entity_getter_contracts=$true;$report.status='passed-new-entity-getter-contracts-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 9|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;steps=$report.steps;test_results=$report.test_results;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 6
