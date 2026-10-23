param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$previous=Join-Path $taskRoot '.tooling\r1-crowd-proximity-validation'
$statePrevious=Join-Path $taskRoot '.tooling\r1-crowd-state-validation'
$output=Join-Path $taskRoot '.tooling\r1-crowd-raycast-validation'
$priorHash='602f427aae0f700be31a9abb86a0d1e1a55b0fa7a9d2c7cb3803b293561738f8'
$statePriorHash='43de35ef971ef6f9b7f723817951c2028de46a0b29fd6afd6874b35a9a3bb99a'
$sourcePins=@{
 'NavigationCrowdRaycastContracts.cs'='0f6b8a1b5d1ec35002bd8c9825c4a90218ea6b154efab1b4e4e69fd9d2d24695'
 'NavigationCrowdRaycastTests.cs'='b653eb74d9c9e211d37a09e6a8d536e5ceb848e9ffab29692725cdec7ae22362'
}
# Reuse only immutable libraries proved by prior passed result manifests.
# No old source/test executable is rebuilt or executed by this recipe.
$dependencyPins=@{
 'GrimDynasty.Actions.dll'='3ea34074db9d5a67d7dffb702eb053b703dc3b754d0db442aeadd71247e15c58'
 'GrimDynasty.NavigationNearest.dll'='75f303c482679df6ba2c8f01aad7759ba9ed322ad6386100525b690cd9b9dfd0'
 'GrimDynasty.NavigationMeshTiles.dll'='0b8604244b7681e6f0f5e13a63c93f2f815069bbe74109e974938c94d9ee4d57'
 'GrimDynasty.NavigationCorridor.dll'='1afd6850ae3f2fc1c0f13c6562ce6b5749a3ea98d3b2e5792d67234d4c779e33'
 'GrimDynasty.NavigationCrowdAdapter.dll'='04215144406cd87357b98bd32911f96281453c4010e4f2f61e1c6997b065b73e'
}
$tileName='GrimDynasty.NavigationTileData.dll'
$tileHash='b0459f94ca130467c2ec7f8c0379700250926c5a8eb8a3e6f617910a3bb02cd9'
function Hash([string]$p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-File([string]$p,[string]$expected){
 if($expected -notmatch '^[0-9a-f]{64}$'){throw ('Unfrozen input: '+$p)}
 $item=Get-Item -LiteralPath $p
 if($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw ('Unexpected or redirected input: '+$p)}
 if((Hash $p) -ne $expected){throw ('Input fingerprint changed: '+$p)}
}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace, no arguments required'}
if(Test-Path -LiteralPath $output){throw 'Existing crowd-raycast evidence preserved; output must be new'}
$priorResult=Join-Path $previous 'result.json'
$statePriorResult=Join-Path $statePrevious 'result.json'
Check-File $priorResult $priorHash
Check-File $statePriorResult $statePriorHash
$old=Get-Content -LiteralPath $priorResult -Raw|ConvertFrom-Json
$stateOld=Get-Content -LiteralPath $statePriorResult -Raw|ConvertFrom-Json
if($old.status -ne 'passed-isolated-crowd-proximity-only' -or !$old.isolated_proximity_contracts -or $null -ne $old.failure){throw 'Passed proximity prerequisite is required'}
if($stateOld.status -ne 'passed-isolated-and-saved-resource-crowd-state-requests-only' -or !$stateOld.isolated_state_contracts -or !$stateOld.actual_saved_resource_requests -or $null -ne $stateOld.failure){throw 'Passed state prerequisite is required'}
foreach($name in $dependencyPins.Keys){
 $record=@($old.artifacts|Where-Object {$_.name -eq $name})
 if($record.Count -ne 1 -or $record[0].sha256 -ne $dependencyPins[$name]){throw ('Dependency not uniquely pinned by passed result: '+$name)}
}
$tileRecord=@($stateOld.artifacts|Where-Object {$_.name -eq $tileName})
if($tileRecord.Count -ne 1 -or $tileRecord[0].sha256 -ne $tileHash){throw 'TileData dependency not uniquely pinned by passed state result'}
function Check-Inputs {
 Check-File $compiler $compilerHash
 Check-File $priorResult $priorHash
 Check-File $statePriorResult $statePriorHash
 foreach($name in $sourcePins.Keys){Check-File (Join-Path $taskRoot ('src\reverse_client_actions\'+$name)) $sourcePins[$name]}
 foreach($name in $dependencyPins.Keys){Check-File (Join-Path $previous $name) $dependencyPins[$name]}
 Check-File (Join-Path $statePrevious $tileName) $tileHash
}
Check-Inputs
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash
 prior_result=$priorResult;prior_result_sha256=$priorHash;state_prior_result=$statePriorResult;state_prior_result_sha256=$statePriorHash
 source_pins=$sourcePins;dependency_pins=$dependencyPins;additional_dependency=[ordered]@{directory=$statePrevious;name=$tileName;sha256=$tileHash}
 steps=@();test_results=@();artifacts=@();previous_executables_invoked=@()
 affected_libraries_recompiled=@('NavigationCrowdRaycastContracts.cs');regression_test_sources=@()
 new_raycast_contracts=$false;actual_owned_mesh_links=$false;actual_nearest_adapter=$false
 original_read=$false;original_executed=$false;saved_map_read=$false;backend_started=$false
 native_loaded_raycast_observed=$false;complete_crowd_update_validated=$false;character_movement_validated=$false;gameplay_accepted=$false;failure=$null
 support_boundary='Recovered263290 and0309F0 in explicitly owned typed mesh/query/FP domain; fixture polygons and actual produced links only. Native loaded state, real scene/input and character movement remain unverified.'
}
New-Item -ItemType Directory -Path $output|Out-Null
function Run-Step([string]$name,[string]$program,[string[]]$arguments){
 $stdout=Join-Path $output ($name+'.log');$stderr=Join-Path $output ($name+'.stderr.log');$oldPreference=$ErrorActionPreference
 try{$ErrorActionPreference='Continue'; & $program @arguments 1> $stdout 2> $stderr; $invocationSucceeded=$?; $code=$LASTEXITCODE}finally{$ErrorActionPreference=$oldPreference}
 $report.steps += [ordered]@{name=$name;program=$program;arguments=$arguments;invocation_succeeded=$invocationSucceeded;exit_code=$code}
 if(!$invocationSucceeded -or $code -ne 0){throw "Step $name failed (invocation=$invocationSucceeded, exit=$code); logs retained"}
 if($name.StartsWith('compile-') -and (((Get-Item -LiteralPath $stdout).Length -ne 0) -or ((Get-Item -LiteralPath $stderr).Length -ne 0))){throw ('Unexpected compiler diagnostics: '+$name)}
}
function Run-Tests([string]$name,[string]$program,[int]$expectedScenarios){
 Run-Step $name $program @()
 $text=Get-Content -LiteralPath (Join-Path $output ($name+'.log')) -Raw
 $summaryMatches=[regex]::Matches($text,'(?m)^PASS (\d+) scenarios, (\d+) assertions;')
 if($summaryMatches.Count -ne 1){throw ('Missing unique test summary: '+$name)}
 $scenarios=[int]$summaryMatches[0].Groups[1].Value;$assertions=[int]$summaryMatches[0].Groups[2].Value
 if($scenarios -ne $expectedScenarios -or $assertions -le 0){throw ('Unexpected test summary counts: '+$name)}
 if((Get-Item -LiteralPath (Join-Path $output ($name+'.stderr.log'))).Length -ne 0){throw ('Unexpected test stderr: '+$name)}
 $report.test_results += [ordered]@{name=$name;kind='new';scenarios=$scenarios;assertions=$assertions}
}
try{
 foreach($name in $dependencyPins.Keys){Copy-Item -LiteralPath (Join-Path $previous $name) -Destination (Join-Path $output $name);Check-File (Join-Path $output $name) $dependencyPins[$name]}
 Copy-Item -LiteralPath (Join-Path $statePrevious $tileName) -Destination (Join-Path $output $tileName)
 Check-File (Join-Path $output $tileName) $tileHash
 $references=@();foreach($name in @('GrimDynasty.Actions.dll','GrimDynasty.NavigationNearest.dll','GrimDynasty.NavigationMeshTiles.dll','GrimDynasty.NavigationCorridor.dll','GrimDynasty.NavigationCrowdAdapter.dll')){$references += '/reference:'+(Join-Path $output $name)}
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$library=Join-Path $output 'GrimDynasty.NavigationCrowdRaycast.dll'
 Run-Step 'compile-crowd-raycast' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@((Join-Path $source 'NavigationCrowdRaycastContracts.cs')))
 $references += @("/reference:$library",('/reference:'+(Join-Path $output $tileName)))
 $tests=Join-Path $output 'NavigationCrowdRaycastTests.exe'
 Run-Step 'compile-raycast-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:NavigationCrowdRaycastTestProgram',"/out:$tests")+$references+@((Join-Path $source 'NavigationCrowdRaycastTests.cs')))
 Run-Tests 'new-raycast-tests' $tests 18
 $report.new_raycast_contracts=$true;$report.actual_owned_mesh_links=$true;$report.actual_nearest_adapter=$true
 Check-Inputs
 foreach($name in $dependencyPins.Keys){Check-File (Join-Path $output $name) $dependencyPins[$name]}
 Check-File (Join-Path $output $tileName) $tileHash
 $report.status='passed-new-isolated-crowd-raycast-and-nearest-adapter-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($f in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$f.Name;bytes=$f.Length;sha256=(Hash $f.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;steps=$report.steps;test_results=$report.test_results;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 7
