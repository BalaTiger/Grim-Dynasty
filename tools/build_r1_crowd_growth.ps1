param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$previous=Join-Path $taskRoot '.tooling\r1-crowd-proximity-validation'
$statePrevious=Join-Path $taskRoot '.tooling\r1-crowd-state-validation'
$output=Join-Path $taskRoot '.tooling\r1-crowd-growth-validation'
$priorHash='602f427aae0f700be31a9abb86a0d1e1a55b0fa7a9d2c7cb3803b293561738f8'
$statePriorHash='43de35ef971ef6f9b7f723817951c2028de46a0b29fd6afd6874b35a9a3bb99a'
$sourcePins=@{
 'NavigationCrowdStateContracts.cs'='452aa247b515dcacafe886849f9799a9c6682c1b9059b6475b8b12d819aef399'
 'NavigationCrowdProximityContracts.cs'='c86de20b1035afcfb60bb3adc6c8517a98d366f0fda9841d8f671dd8c594f1e8'
 'NavigationCrowdGrowthTests.cs'='734c614d5806c7a7a3ca77ab7a655c418b9de6fdc63517e7f783bc74dd7e5348'
 'NavigationCrowdStateTests.cs'='9377e75bc48a208bc1cdde94ee9f66c0fae5b57d72b80153ddfd210d0eea3bd3'
 'NavigationCrowdProximityTests.cs'='fc80ea071b0179f332e47da99a51534f94204ade979812643659bb380ebced7c'
}
# Only the two affected production libraries are rebuilt. Old production
# dependencies are copied from passed results; no old executable is reused.
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
if(Test-Path -LiteralPath $output){throw 'Existing crowd-growth evidence preserved; output must be new'}
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
 affected_libraries_recompiled=@('NavigationCrowdStateContracts.cs','NavigationCrowdProximityContracts.cs')
 regression_test_sources=@('NavigationCrowdStateTests.cs','NavigationCrowdProximityTests.cs')
 regression_change='Only Proximity case7 changes from unresolved25EE4F to actual caller-computed capacity4 growth; its output accessor follows the current published buffer. Historical result retained.'
 new_growth_contracts=$false;state_regression=$false;proximity_regression=$false
 original_read=$false;original_executed=$false;saved_map_read=$false;actual_nine_tile_pipeline_rerun=$false
 native_allocator_exceptions_validated=$false;native_loaded_crowd_observed=$false;complete_crowd_update_validated=$false;character_movement_validated=$false;gameplay_accepted=$false;failure=$null
 support_boundary='Recovered pointer-vector growth/copy/publication in the explicit successful managed allocation domain; native allocator and exception behavior remain unverified.'
}
New-Item -ItemType Directory -Path $output|Out-Null
function Run-Step([string]$name,[string]$program,[string[]]$arguments){
 $stdout=Join-Path $output ($name+'.log');$stderr=Join-Path $output ($name+'.stderr.log');$oldPreference=$ErrorActionPreference
 try{$ErrorActionPreference='Continue'; & $program @arguments 1> $stdout 2> $stderr; $invocationSucceeded=$?; $code=$LASTEXITCODE}finally{$ErrorActionPreference=$oldPreference}
 $report.steps += [ordered]@{name=$name;program=$program;arguments=$arguments;invocation_succeeded=$invocationSucceeded;exit_code=$code}
 if(!$invocationSucceeded -or $code -ne 0){throw "Step $name failed (invocation=$invocationSucceeded, exit=$code); logs retained"}
 # /nologo + /warnaserror+ must produce no compiler diagnostics at all.
 if($name.StartsWith('compile-') -and (((Get-Item -LiteralPath $stdout).Length -ne 0) -or ((Get-Item -LiteralPath $stderr).Length -ne 0))){throw ('Unexpected compiler diagnostics: '+$name)}
}
function Run-Tests([string]$name,[string]$program,[string]$kind,[int]$expectedScenarios){
 Run-Step $name $program @()
 $text=Get-Content -LiteralPath (Join-Path $output ($name+'.log')) -Raw
 $summaryMatches=[regex]::Matches($text,'(?m)^PASS (\d+) scenarios, (\d+) assertions;')
 if($summaryMatches.Count -ne 1){throw ('Missing unique test summary: '+$name)}
 $scenarios=[int]$summaryMatches[0].Groups[1].Value;$assertions=[int]$summaryMatches[0].Groups[2].Value
 if($scenarios -ne $expectedScenarios -or $assertions -le 0){throw ('Unexpected test summary counts: '+$name)}
 if((Get-Item -LiteralPath (Join-Path $output ($name+'.stderr.log'))).Length -ne 0){throw ('Unexpected test stderr: '+$name)}
 $report.test_results += [ordered]@{name=$name;kind=$kind;scenarios=$scenarios;assertions=$assertions}
}
try{
 foreach($name in $dependencyPins.Keys){Copy-Item -LiteralPath (Join-Path $previous $name) -Destination (Join-Path $output $name);Check-File (Join-Path $output $name) $dependencyPins[$name]}
 Copy-Item -LiteralPath (Join-Path $statePrevious $tileName) -Destination (Join-Path $output $tileName)
 Check-File (Join-Path $output $tileName) $tileHash
 $references=@();foreach($name in @('GrimDynasty.Actions.dll','GrimDynasty.NavigationNearest.dll','GrimDynasty.NavigationMeshTiles.dll','GrimDynasty.NavigationCorridor.dll','GrimDynasty.NavigationCrowdAdapter.dll')){$references += '/reference:'+(Join-Path $output $name)}
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$stateLibrary=Join-Path $output 'GrimDynasty.NavigationCrowdState.dll'
 Run-Step 'compile-crowd-state' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$stateLibrary")+$references+@((Join-Path $source 'NavigationCrowdStateContracts.cs')))
 $references += "/reference:$stateLibrary"
 $proximityLibrary=Join-Path $output 'GrimDynasty.NavigationCrowdProximity.dll'
 Run-Step 'compile-crowd-proximity' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$proximityLibrary")+$references+@((Join-Path $source 'NavigationCrowdProximityContracts.cs')))
 $references += @("/reference:$proximityLibrary",('/reference:'+(Join-Path $output $tileName)))
 $growthTests=Join-Path $output 'NavigationCrowdGrowthTests.exe';$stateTests=Join-Path $output 'NavigationCrowdStateTests.exe';$proximityTests=Join-Path $output 'NavigationCrowdProximityTests.exe'
 Run-Step 'compile-growth-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:NavigationCrowdGrowthTestProgram',"/out:$growthTests")+$references+@((Join-Path $source 'NavigationCrowdGrowthTests.cs')))
 Run-Step 'compile-state-regression' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:NavigationCrowdStateTestProgram',"/out:$stateTests")+$references+@((Join-Path $source 'NavigationCrowdStateTests.cs')))
 Run-Step 'compile-proximity-regression' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:NavigationCrowdProximityTestProgram',"/out:$proximityTests")+$references+@((Join-Path $source 'NavigationCrowdProximityTests.cs')))
 Run-Tests 'new-growth-tests' $growthTests 'new' 7
 $report.new_growth_contracts=$true
 Run-Tests 'state-regression' $stateTests 'affected-regression' 6
 $report.state_regression=$true
 Run-Tests 'proximity-regression' $proximityTests 'affected-regression' 7
 $report.proximity_regression=$true
 Check-Inputs
 foreach($name in $dependencyPins.Keys){Check-File (Join-Path $output $name) $dependencyPins[$name]}
 Check-File (Join-Path $output $tileName) $tileHash
 $report.status='passed-new-crowd-growth-and-affected-state-proximity-regressions-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($f in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$f.Name;bytes=$f.Length;sha256=(Hash $f.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;steps=$report.steps;test_results=$report.test_results;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 7
