param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$previous=Join-Path $taskRoot '.tooling\r1-crowd-state-validation'
$output=Join-Path $taskRoot '.tooling\r1-crowd-proximity-validation'
$priorHash='43de35ef971ef6f9b7f723817951c2028de46a0b29fd6afd6874b35a9a3bb99a'
$sourcePins=@{
 'NavigationCrowdProximityContracts.cs'='3b336246ce2eb74d187fc62616ada6ecae2c63f4d8275002087382ab18da60af'
 'NavigationCrowdProximityTests.cs'='8e6d2cda69030743b10c39206fb9975bf711e8d50b5e950ab36fbafcac6c4709'
}
# Fixed production dependency closure only. No prior executable is copied or
# invoked, no resource producer is run, and no old source is recompiled.
$dependencyPins=@{
 'GrimDynasty.Actions.dll'='3ea34074db9d5a67d7dffb702eb053b703dc3b754d0db442aeadd71247e15c58'
 'GrimDynasty.NavigationNearest.dll'='75f303c482679df6ba2c8f01aad7759ba9ed322ad6386100525b690cd9b9dfd0'
 'GrimDynasty.NavigationMeshTiles.dll'='0b8604244b7681e6f0f5e13a63c93f2f815069bbe74109e974938c94d9ee4d57'
 'GrimDynasty.NavigationCorridor.dll'='1afd6850ae3f2fc1c0f13c6562ce6b5749a3ea98d3b2e5792d67234d4c779e33'
 'GrimDynasty.NavigationCrowdAdapter.dll'='04215144406cd87357b98bd32911f96281453c4010e4f2f61e1c6997b065b73e'
 'GrimDynasty.NavigationCrowdState.dll'='df66dc35edcade52d44aa794b0aa881c072f6c22caf374340b5f54e34ead6463'
}
function Hash([string]$p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-File([string]$p,[string]$expected){
 if($expected -notmatch '^[0-9a-f]{64}$'){throw ('Unfrozen input: '+$p)}
 $item=Get-Item -LiteralPath $p
 if($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw ('Unexpected or redirected input: '+$p)}
 if((Hash $p) -ne $expected){throw ('Input fingerprint changed: '+$p)}
}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace, no arguments required'}
if(Test-Path -LiteralPath $output){throw 'Existing crowd-proximity evidence preserved; output must be new'}
$priorResult=Join-Path $previous 'result.json'
Check-File $priorResult $priorHash
$old=Get-Content -LiteralPath $priorResult -Raw|ConvertFrom-Json
if($old.status -ne 'passed-isolated-and-saved-resource-crowd-state-requests-only' -or !$old.isolated_state_contracts -or !$old.actual_saved_resource_requests -or $null -ne $old.failure){throw 'Validated crowd-state dependency prerequisite failed'}
foreach($name in $dependencyPins.Keys){
 $record=@($old.artifacts|Where-Object {$_.name -eq $name})
 if($record.Count -ne 1 -or $record[0].sha256 -ne $dependencyPins[$name]){throw ('Dependency is not uniquely pinned by the passed result: '+$name)}
}
function Check-Inputs {
 Check-File $compiler $compilerHash
 Check-File $priorResult $priorHash
 foreach($p in $sourcePins.Keys){Check-File (Join-Path $taskRoot ('src\reverse_client_actions\'+$p)) $sourcePins[$p]}
 foreach($p in $dependencyPins.Keys){Check-File (Join-Path $previous $p) $dependencyPins[$p]}
}
Check-Inputs
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash
 prior_result=$priorResult;prior_result_sha256=$priorHash;source_pins=$sourcePins;dependency_pins=$dependencyPins
 steps=@();artifacts=@();old_sources_recompiled=@();old_test_executables_rerun=@();old_executable_entrypoints_invoked=@()
 original_read=$false;original_executed=$false;saved_map_read=$false;actual_nine_tile_pipeline_rerun=$false
 isolated_proximity_contracts=$false;native_loaded_crowd_observed=$false;complete_crowd_update_validated=$false
 character_movement_validated=$false;gameplay_accepted=$false;failure=$null
 support_boundary='Prepare25E900,Insert25EA00,Neighbors25EB90 under explicit own typed identities,normal managed allocations,finite normal binary32 inputs and disjoint preallocated output;034FE0 output growth remains Unsupported'
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
 foreach($name in @('GrimDynasty.Actions.dll','GrimDynasty.NavigationNearest.dll','GrimDynasty.NavigationMeshTiles.dll','GrimDynasty.NavigationCorridor.dll','GrimDynasty.NavigationCrowdAdapter.dll','GrimDynasty.NavigationCrowdState.dll')){$references += '/reference:'+(Join-Path $output $name)}
 $source=Join-Path $taskRoot 'src\reverse_client_actions'
 $library=Join-Path $output 'GrimDynasty.NavigationCrowdProximity.dll'
 Run-Step 'compile-crowd-proximity' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@((Join-Path $source 'NavigationCrowdProximityContracts.cs')))
 $references += "/reference:$library"
 $tests=Join-Path $output 'NavigationCrowdProximityTests.exe'
 Run-Step 'compile-crowd-proximity-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:NavigationCrowdProximityTestProgram',"/out:$tests")+$references+@((Join-Path $source 'NavigationCrowdProximityTests.cs')))
 Run-Step 'crowd-proximity-memory-tests' $tests @()
 $report.isolated_proximity_contracts=$true
 Check-Inputs
 foreach($name in $dependencyPins.Keys){Check-File (Join-Path $output $name) $dependencyPins[$name]}
 $report.status='passed-isolated-crowd-proximity-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($f in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$f.Name;bytes=$f.Length;sha256=(Hash $f.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o')
 $report|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;steps=$report.steps;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 5
