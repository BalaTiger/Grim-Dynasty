param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$previous=Join-Path $taskRoot '.tooling\r1-real-straight-path-validation'
$output=Join-Path $taskRoot '.tooling\r1-crowd-adapter-validation'
$priorHash='283a663cc6b06ec01717cfd044310688d8b26ded77c63163fc9738671a8d5c66'
$sourcePins=@{
 'NavigationCrowdAdapterContracts.cs'='a8b84543c281f478b500f8ea0c3ba56849422e40cac903a7124e41d0d43d0940'
 'NavigationCrowdAdapterTests.cs'='5f5c2b9bcae625e69d1c596792c070124ef03fa2ad23816f8469869a022320ea'
 'MainmenuCrowdAdapterPipeline.cs'='9f78249e8e5faa3db17ebb67b48b022be4cbae894f619ad704fad4623b092c23'
}
# Only fixed production artifacts are reused. No old test executable is copied
# or invoked; MainmenuTilePipeline.exe is referenced for its production method.
$dependencyPins=@{
 'GrimDynasty.Actions.dll'='3ea34074db9d5a67d7dffb702eb053b703dc3b754d0db442aeadd71247e15c58'
 'GrimDynasty.NavigationNearest.dll'='75f303c482679df6ba2c8f01aad7759ba9ed322ad6386100525b690cd9b9dfd0'
 'GrimDynasty.NavigationMeshTiles.dll'='0b8604244b7681e6f0f5e13a63c93f2f815069bbe74109e974938c94d9ee4d57'
 'GrimDynasty.NavigationCorridor.dll'='1afd6850ae3f2fc1c0f13c6562ce6b5749a3ea98d3b2e5792d67234d4c779e33'
 'GrimDynasty.NavigationTileData.dll'='b0459f94ca130467c2ec7f8c0379700250926c5a8eb8a3e6f617910a3bb02cd9'
 'GrimDynasty.NavigationPolygons.dll'='6e33b858bc8073d59beacd7cc0a94d5ad15c24aa6f8f3ac966f2966175fa6856'
 'GrimDynasty.NavigationContours.dll'='c63bacce93c83700d3113ec36ade462a67b48b1030f2e293a39322e149cad263'
 'GrimDynasty.NavigationLayers.dll'='80137bab7ceb54a3688655501b2d5c66b59de05d97a9a65edd052e8953e254ee'
 'GrimDynasty.PathingData.dll'='4f105f145ccdfb4b17b33b1167f8b700f06f38a4a07f8dc36cdf50f3d12aac4f'
 'MainmenuTilePipeline.exe'='4d006bb08378360bdcccca167bc8d841b7f2ddbef48b55325ab9973658d173e7'
}
$member=Join-Path $taskRoot 'docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin'
$memberHash='a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289'
function Hash([string]$p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-File([string]$p,[string]$expected){
 if($expected -eq 'PENDING'){throw ('Unfrozen input: '+$p)}
 $item=Get-Item -LiteralPath $p
 if($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw ('Unexpected or redirected input: '+$p)}
 if((Hash $p) -ne $expected){throw ('Input fingerprint changed: '+$p)}
}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace, no arguments required'}
if(Test-Path -LiteralPath $output){throw 'Existing crowd-adapter evidence preserved; output must be new'}
$priorResult=Join-Path $previous 'result.json'
Check-File $priorResult $priorHash
$old=Get-Content -LiteralPath $priorResult -Raw|ConvertFrom-Json
if($old.status -ne 'passed-actual-straight-path-only' -or !$old.actual_waypoints -or $null -ne $old.failure){throw 'Validated navigation dependency prerequisite failed'}
foreach($name in $dependencyPins.Keys){
 $record=@($old.artifacts|Where-Object {$_.name -eq $name})
 if($record.Count -ne 1 -or $record[0].sha256 -ne $dependencyPins[$name]){throw ('Dependency is not uniquely pinned by the passed result: '+$name)}
}
function Check-Inputs {
 Check-File $compiler $compilerHash
 Check-File $priorResult $priorHash
 foreach($p in $sourcePins.Keys){Check-File (Join-Path $taskRoot ('src\reverse_client_actions\'+$p)) $sourcePins[$p]}
 foreach($p in $dependencyPins.Keys){Check-File (Join-Path $previous $p) $dependencyPins[$p]}
 Check-File $member $memberHash
 if((Get-Item -LiteralPath $member).Length -ne 1091357){throw 'Pinned saved mainmenu member extent changed'}
}
Check-Inputs
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash
 prior_result=$priorResult;prior_result_sha256=$priorHash;source_pins=$sourcePins;dependency_pins=$dependencyPins
 saved_member=$member;saved_member_sha256=$memberHash;saved_member_bytes=1091357
 steps=@();artifacts=@();old_sources_recompiled=@();old_test_executables_rerun=@();old_executable_entrypoints_invoked=@()
 reused_production_entry='MainmenuTilePipeline.BuildExplicitColdTiles';explicit_own_domain_argument='--explicit-default-domain'
 original_read=$false;original_executed=$false;isolated_adapter_contracts=$false;actual_saved_resource_adapter_queries=$false
 native_loaded_crowd_observed=$false;character_movement_validated=$false;gameplay_accepted=$false;failure=$null
}
New-Item -ItemType Directory -Path $output|Out-Null
function Run-Step([string]$name,[string]$program,[string[]]$arguments){
 $oldPreference=$ErrorActionPreference
 try{
  $ErrorActionPreference='Continue'
  & $program @arguments 1> (Join-Path $output ($name+'.log')) 2> (Join-Path $output ($name+'.stderr.log'))
  $invocationSucceeded=$?
  $code=$LASTEXITCODE
 }finally{$ErrorActionPreference=$oldPreference}
 $report.steps += [ordered]@{name=$name;invocation_succeeded=$invocationSucceeded;exit_code=$code}
 if(!$invocationSucceeded -or $code -ne 0){throw "Step $name failed (invocation=$invocationSucceeded, exit=$code); logs retained"}
}
try{
 foreach($name in $dependencyPins.Keys){
  $runtime=Join-Path $output $name
  Copy-Item -LiteralPath (Join-Path $previous $name) -Destination $runtime
  Check-File $runtime $dependencyPins[$name]
 }
 $references=@()
 foreach($name in @('GrimDynasty.Actions.dll','GrimDynasty.NavigationNearest.dll','GrimDynasty.NavigationMeshTiles.dll','GrimDynasty.NavigationCorridor.dll')){$references += '/reference:'+(Join-Path $output $name)}
 $source=Join-Path $taskRoot 'src\reverse_client_actions'
 $library=Join-Path $output 'GrimDynasty.NavigationCrowdAdapter.dll'
 Run-Step 'compile-crowd-adapter' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@((Join-Path $source 'NavigationCrowdAdapterContracts.cs')))
 $references += @("/reference:$library",('/reference:'+(Join-Path $output 'GrimDynasty.NavigationTileData.dll')))
 $tests=Join-Path $output 'NavigationCrowdAdapterTests.exe'
 Run-Step 'compile-crowd-adapter-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:NavigationCrowdAdapterTestProgram',"/out:$tests")+$references+@((Join-Path $source 'NavigationCrowdAdapterTests.cs')))
 $driver=Join-Path $output 'MainmenuCrowdAdapterPipeline.exe'
 $producer=Join-Path $output 'MainmenuTilePipeline.exe'
 Run-Step 'compile-real-crowd-adapter' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:MainmenuCrowdAdapterPipeline',"/out:$driver","/reference:$producer")+$references+@((Join-Path $source 'MainmenuCrowdAdapterPipeline.cs')))
 Run-Step 'crowd-adapter-memory-tests' $tests @()
 $report.isolated_adapter_contracts=$true
 # This flag names an explicit OWN cold configuration, never a native default.
 Run-Step 'real-crowd-adapter-pipeline' $driver @('--explicit-default-domain')
 $report.actual_saved_resource_adapter_queries=$true
 Check-Inputs
 foreach($name in $dependencyPins.Keys){Check-File (Join-Path $output $name) $dependencyPins[$name]}
 $report.status='passed-isolated-and-saved-resource-crowd-adapter-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($f in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$f.Name;bytes=$f.Length;sha256=(Hash $f.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o')
 $report|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;steps=$report.steps;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 5
