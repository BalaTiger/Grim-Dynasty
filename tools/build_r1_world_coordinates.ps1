param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$previous=Join-Path $taskRoot '.tooling\r1-move-manager-validation'
$output=Join-Path $taskRoot '.tooling\r1-world-coordinate-validation'
$priorHash='2e2190737bd01e396f302d211189aae114decd35307f5ca764b442bcae149516'
$sourcePins=@{
 'WorldCoordinateContracts.cs'='42319bb83d786123ad8ccf62387e00620d15734f0d8e92799a89aba7894ceb83'
 'WorldCoordinateTests.cs'='9bfc0ec9f4b02abee41d49cc44dcc90e4d8a031a292d93d8800c1a4c64678deb'
}
$dependencyPins=@{
 'GrimDynasty.Actions.dll'='3ea34074db9d5a67d7dffb702eb053b703dc3b754d0db442aeadd71247e15c58'
 'GrimDynasty.MoveManager.dll'='7563b6e32745e79ddc8308d2ea48ca7336050271d8dc8c9ce1e75346a52232da'
}
function Hash([string]$p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-File([string]$p,[string]$expected){
 if($expected -eq 'PENDING'){throw ('Unfrozen input: '+$p)}
 $item=Get-Item -LiteralPath $p
 if($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw ('Unexpected or redirected input: '+$p)}
 if((Hash $p) -ne $expected){throw ('Input fingerprint changed: '+$p)}
}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace, no arguments required'}
if(Test-Path -LiteralPath $output){throw 'Existing world-coordinate evidence preserved; output must be new'}
$priorResult=Join-Path $previous 'result.json'
Check-File $priorResult $priorHash
$old=Get-Content -LiteralPath $priorResult -Raw|ConvertFrom-Json
if($old.status -ne 'passed-isolated-move-manager-contracts' -or $old.compile_extension_exit -ne 0 -or $old.compile_tests_exit -ne 0 -or $old.tests_exit -ne 0 -or $null -ne $old.failure){throw 'Validated dependency prerequisite failed'}
foreach($name in $dependencyPins.Keys){
 $path=Join-Path $previous $name
 $record=@($old.artifacts|Where-Object {$_.path -eq $path})
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
 steps=@();artifacts=@();old_test_executables_rerun=@();original_read=$false;original_executed=$false
 isolated_world_coordinate_contracts=$false;successful_floor_placement_validated=$false;real_world_loaded=$false;gameplay_accepted=$false;failure=$null
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
 foreach($name in @('GrimDynasty.Actions.dll','GrimDynasty.MoveManager.dll')){$references += '/reference:'+(Join-Path $output $name)}
 $source=Join-Path $taskRoot 'src\reverse_client_actions'
 $library=Join-Path $output 'GrimDynasty.WorldCoordinates.dll'
 Run-Step 'compile-world-coordinates' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@((Join-Path $source 'WorldCoordinateContracts.cs')))
 $tests=Join-Path $output 'WorldCoordinateTests.exe'
 Run-Step 'compile-world-coordinate-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:WorldCoordinateTestProgram',"/out:$tests","/reference:$library")+$references+@((Join-Path $source 'WorldCoordinateTests.cs')))
 Run-Step 'world-coordinate-memory-tests' $tests @()
 Check-Inputs
 foreach($name in $dependencyPins.Keys){Check-File (Join-Path $output $name) $dependencyPins[$name]}
 $report.isolated_world_coordinate_contracts=$true
 $report.status='passed-isolated-world-coordinate-contracts-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($f in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$f.Name;bytes=$f.Length;sha256=(Hash $f.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o')
 $report|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;steps=$report.steps;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 5
