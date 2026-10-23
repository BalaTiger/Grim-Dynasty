param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$previous=Join-Path $taskRoot '.tooling\r1-crowd-growth-validation'
$resourcePrevious=Join-Path $taskRoot '.tooling\r1-crowd-state-validation'
$output=Join-Path $taskRoot '.tooling\r1-crowd-path-validation'
$priorHash='f9005adbc8c3becc734cbcf19d6f5e8563ccfc9a877e2b7e5944621f03956796'
$resourcePriorHash='43de35ef971ef6f9b7f723817951c2028de46a0b29fd6afd6874b35a9a3bb99a'
$member=Join-Path $taskRoot 'docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin'
$memberHash='a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289'
$sourcePins=@{
 'NavigationCrowdPathContracts.cs'='bc25e51d7037e666eb09ada9e1f71154a3453bc93d2747628a8e7b76e346ed6d'
 'NavigationCrowdPathTests.cs'='721a21465f5726e8bfd7e040d983640526c40f801bc41cded52beb273e3d1637'
 'MainmenuCrowdRequestPipeline.cs'='21fdb0f1763421f6cd00145b8808f6efa70efdba3e4aff02adbdc78d603bab2e'
 'MainmenuCrowdPathPipeline.cs'='e2812e0bec247af07c034dabc29aff7295cadf8d72d1d0cca1c9e49356f3605e'
}
$dependencyPins=@{
 'GrimDynasty.Actions.dll'='3ea34074db9d5a67d7dffb702eb053b703dc3b754d0db442aeadd71247e15c58'
 'GrimDynasty.NavigationNearest.dll'='75f303c482679df6ba2c8f01aad7759ba9ed322ad6386100525b690cd9b9dfd0'
 'GrimDynasty.NavigationMeshTiles.dll'='0b8604244b7681e6f0f5e13a63c93f2f815069bbe74109e974938c94d9ee4d57'
 'GrimDynasty.NavigationCorridor.dll'='1afd6850ae3f2fc1c0f13c6562ce6b5749a3ea98d3b2e5792d67234d4c779e33'
 'GrimDynasty.NavigationCrowdAdapter.dll'='04215144406cd87357b98bd32911f96281453c4010e4f2f61e1c6997b065b73e'
 'GrimDynasty.NavigationCrowdState.dll'='3c37f78adff8799df3d1da6624145d4cf1037f44416c7e550fa904c48ecf25e0'
 'GrimDynasty.NavigationTileData.dll'='b0459f94ca130467c2ec7f8c0379700250926c5a8eb8a3e6f617910a3bb02cd9'
}
# Frozen saved-resource producer dependencies only. The EXE is referenced as
# an assembly and its BuildExplicitColdTiles method is used; its Main is not run.
$resourcePins=@{
 'GrimDynasty.PathingData.dll'='4f105f145ccdfb4b17b33b1167f8b700f06f38a4a07f8dc36cdf50f3d12aac4f'
 'GrimDynasty.NavigationLayers.dll'='80137bab7ceb54a3688655501b2d5c66b59de05d97a9a65edd052e8953e254ee'
 'GrimDynasty.NavigationContours.dll'='c63bacce93c83700d3113ec36ade462a67b48b1030f2e293a39322e149cad263'
 'GrimDynasty.NavigationPolygons.dll'='6e33b858bc8073d59beacd7cc0a94d5ad15c24aa6f8f3ac966f2966175fa6856'
 'MainmenuTilePipeline.exe'='4d006bb08378360bdcccca167bc8d841b7f2ddbef48b55325ab9973658d173e7'
}
function Hash([string]$p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-File([string]$p,[string]$expected){
 if($expected -notmatch '^[0-9a-f]{64}$'){throw ('Unfrozen input: '+$p)}
 $item=Get-Item -LiteralPath $p
 if($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw ('Unexpected or redirected input: '+$p)}
 if((Hash $p) -ne $expected){throw ('Input fingerprint changed: '+$p)}
}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace, no arguments required'}
if(Test-Path -LiteralPath $output){throw 'Existing crowd-path evidence preserved; output must be new'}
$priorResult=Join-Path $previous 'result.json';$resourcePriorResult=Join-Path $resourcePrevious 'result.json'
Check-File $priorResult $priorHash
Check-File $resourcePriorResult $resourcePriorHash
$old=Get-Content -LiteralPath $priorResult -Raw|ConvertFrom-Json
$resourceOld=Get-Content -LiteralPath $resourcePriorResult -Raw|ConvertFrom-Json
if($old.status -ne 'passed-new-crowd-growth-and-affected-state-proximity-regressions-only' -or !$old.new_growth_contracts -or !$old.state_regression -or !$old.proximity_regression -or $null -ne $old.failure){throw 'Passed shared-state prerequisite is required'}
if($resourceOld.status -ne 'passed-isolated-and-saved-resource-crowd-state-requests-only' -or !$resourceOld.actual_saved_resource_requests -or $null -ne $resourceOld.failure){throw 'Passed saved-resource request prerequisite is required'}
foreach($name in $dependencyPins.Keys){$record=@($old.artifacts|Where-Object {$_.name -eq $name});if($record.Count -ne 1 -or $record[0].sha256 -ne $dependencyPins[$name]){throw ('Dependency not uniquely pinned by passed result: '+$name)}}
foreach($name in $resourcePins.Keys){$record=@($resourceOld.artifacts|Where-Object {$_.name -eq $name});if($record.Count -ne 1 -or $record[0].sha256 -ne $resourcePins[$name]){throw ('Saved-resource dependency not uniquely pinned by passed result: '+$name)}}
function Check-Inputs {
 Check-File $compiler $compilerHash
 Check-File $priorResult $priorHash
 Check-File $resourcePriorResult $resourcePriorHash
 foreach($name in $sourcePins.Keys){Check-File (Join-Path $taskRoot ('src\reverse_client_actions\'+$name)) $sourcePins[$name]}
 foreach($name in $dependencyPins.Keys){Check-File (Join-Path $previous $name) $dependencyPins[$name]}
 foreach($name in $resourcePins.Keys){Check-File (Join-Path $resourcePrevious $name) $resourcePins[$name]}
 Check-File $member $memberHash
 if((Get-Item -LiteralPath $member).Length -ne 1091357){throw 'Pinned saved member extent changed'}
}
Check-Inputs
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash
 prior_result=$priorResult;prior_result_sha256=$priorHash;resource_prior_result=$resourcePriorResult;resource_prior_result_sha256=$resourcePriorHash
 source_pins=$sourcePins;dependency_pins=$dependencyPins;resource_pins=$resourcePins
 saved_member=$member;saved_member_sha256=$memberHash;saved_member_bytes=1091357
 steps=@();test_results=@();sample_result=$null;artifacts=@();old_test_suites_rerun=@();old_executable_entrypoints_invoked=@()
 new_library='NavigationCrowdPathContracts.cs';recompiled_producer='MainmenuCrowdRequestPipeline.cs'
 reused_production_entries=@('MainmenuCrowdRequestPipeline.BuildExplicitRequestContext','MainmenuTilePipeline.BuildExplicitColdTiles')
 sample_arguments=@('--explicit-default-domain','--own-disk-ce70-noop')
 new_path_contracts=$false;actual_saved_resource_waypoints=$false;original_read=$false;original_executed=$false
 own_disk_ce70_noop_selected=$true;native_loaded_ce70_observed=$false;native_allocator_exceptions_validated=$false
 character_movement_validated=$false;gameplay_accepted=$false;failure=$null
 support_boundary='Actual shared request-to-waypoint adapter and trim/borrow with recovered stride12 growth in explicit own allocation and disk-leaf CE70 adapter domains; no native loaded hook, movement or R1 game acceptance.'
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
 foreach($name in $dependencyPins.Keys){Copy-Item -LiteralPath (Join-Path $previous $name) -Destination (Join-Path $output $name);Check-File (Join-Path $output $name) $dependencyPins[$name]}
 foreach($name in $resourcePins.Keys){Copy-Item -LiteralPath (Join-Path $resourcePrevious $name) -Destination (Join-Path $output $name);Check-File (Join-Path $output $name) $resourcePins[$name]}
 $references=@();foreach($name in $dependencyPins.Keys){$references += '/reference:'+(Join-Path $output $name)}
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$library=Join-Path $output 'GrimDynasty.NavigationCrowdPath.dll'
 Run-Step 'compile-crowd-path' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@((Join-Path $source 'NavigationCrowdPathContracts.cs')))
 $pathReferences=$references+@("/reference:$library")
 $tests=Join-Path $output 'NavigationCrowdPathTests.exe'
 Run-Step 'compile-path-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:NavigationCrowdPathTestProgram',"/out:$tests")+$pathReferences+@((Join-Path $source 'NavigationCrowdPathTests.cs')))
 $producer=Join-Path $output 'MainmenuCrowdRequestProducer.dll';$producerReferences=$references
 foreach($name in $resourcePins.Keys){$producerReferences += '/reference:'+(Join-Path $output $name)}
 Run-Step 'compile-request-producer' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$producer")+$producerReferences+@((Join-Path $source 'MainmenuCrowdRequestPipeline.cs')))
 $driver=Join-Path $output 'MainmenuCrowdPathPipeline.exe'
 Run-Step 'compile-real-crowd-path' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:MainmenuCrowdPathPipeline',"/out:$driver","/reference:$producer")+$pathReferences+@((Join-Path $source 'MainmenuCrowdPathPipeline.cs')))
 Run-Step 'new-path-memory-tests' $tests @()
 $summaryMatches=[regex]::Matches((Get-Content -LiteralPath (Join-Path $output 'new-path-memory-tests.log') -Raw),'(?m)^PASS (\d+) scenarios, (\d+) assertions;')
 if($summaryMatches.Count -ne 1 -or [int]$summaryMatches[0].Groups[1].Value -ne 15 -or [int]$summaryMatches[0].Groups[2].Value -le 0){throw 'Missing or unexpected unique new-suite summary'}
 $report.test_results += [ordered]@{name='new-path-memory-tests';kind='new';scenarios=[int]$summaryMatches[0].Groups[1].Value;assertions=[int]$summaryMatches[0].Groups[2].Value}
 $report.new_path_contracts=$true
 Run-Step 'real-crowd-path-pipeline' $driver @('--explicit-default-domain','--own-disk-ce70-noop')
 $sampleText=Get-Content -LiteralPath (Join-Path $output 'real-crowd-path-pipeline.log') -Raw
 if([regex]::Matches($sampleText,'(?m)^PASS actual saved tiles=9, same request corridor-to-waypoint cold growth and borrowed/in-place trim;').Count -ne 1){throw 'Missing unique new saved-resource continuation summary'}
 $sampleMatches=[regex]::Matches($sampleText,'(?m)^CROWD PATH tiles=(\d+) corridor=(\d+) tileCrossings=(\d+) waypoints=(\d+) removedPrefix=(\d+) remaining=(\d+) ')
 if($sampleMatches.Count -ne 1){throw 'Missing sample counts'}
 $report.sample_result=[ordered]@{tiles=[int]$sampleMatches[0].Groups[1].Value;corridor=[int]$sampleMatches[0].Groups[2].Value;tile_crossings=[int]$sampleMatches[0].Groups[3].Value;waypoints=[int]$sampleMatches[0].Groups[4].Value;removed_prefix=[int]$sampleMatches[0].Groups[5].Value;remaining=[int]$sampleMatches[0].Groups[6].Value;position_unchanged=$true}
 $report.actual_saved_resource_waypoints=$true
 Check-Inputs
 foreach($name in $dependencyPins.Keys){Check-File (Join-Path $output $name) $dependencyPins[$name]}
 foreach($name in $resourcePins.Keys){Check-File (Join-Path $output $name) $resourcePins[$name]}
 $report.status='passed-new-crowd-waypoint-contracts-and-saved-resource-continuation-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($f in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$f.Name;bytes=$f.Length;sha256=(Hash $f.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;steps=$report.steps;test_results=$report.test_results;sample_result=$report.sample_result;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 7
