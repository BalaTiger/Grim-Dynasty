param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$previous=Join-Path $taskRoot '.tooling\r1-real-mesh-validation'
$output=Join-Path $taskRoot '.tooling\r1-real-mesh-validation-repair1'
$testsSource=Join-Path $taskRoot 'src\reverse_client_actions\NavigationMeshTilesTests.cs'
$testsHash='93b6e7d7c923631b971b92674f8e780edb1972ab033c47a31c2dbebb3365e40d'
$previousHash='698d131a62e5c099deae5bf86a4d435309e126a3d85ddb4ee81f644b5d4bf956'
function Hash([string]$p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace, no arguments required'}
if(Test-Path -LiteralPath $output){throw 'Existing repair evidence preserved'}
if((Hash (Join-Path $previous 'result.json')) -ne $previousHash){throw 'Prior failure report changed'}
$old=Get-Content -LiteralPath (Join-Path $previous 'result.json') -Raw|ConvertFrom-Json
if($old.status -ne 'failed' -or $old.steps.Count -ne 3){throw 'Unexpected prior result'}
foreach($s in $old.steps){if($s.exit_code -ne 0){throw 'Prior compilation did not succeed'}}
function Check-Inputs {
 if((Hash $testsSource) -ne $testsHash){throw 'Repaired fixture changed'}
 foreach($p in $old.inputs.PSObject.Properties){
  if($p.Name -eq 'src\reverse_client_actions\NavigationMeshTilesTests.cs'){continue}
  if((Hash (Join-Path $taskRoot $p.Name)) -ne $p.Value){throw ('Prior frozen input changed: '+$p.Name)}
 }
 foreach($a in $old.artifacts){if($a.name.EndsWith('.dll') -or $a.name -eq 'MainmenuTilePipeline.exe' -or $a.name -eq 'MainmenuMeshPipeline.exe'){
  if((Hash (Join-Path $previous $a.name)) -ne $a.sha256){throw ('Previously compiled artifact changed: '+$a.name)}
 }}
}
Check-Inputs
$report=[ordered]@{started_utc=[DateTime]::UtcNow.ToString('o');status='running';prior_result_sha256=$previousHash;test_source_sha256=$testsHash;recompiled='tests only';steps=@();artifacts=@();prior_successful_scenarios_repeated=18;original_read=$false;original_executed=$false;mesh_published=$false;gameplay_accepted=$false;failure=$null}
New-Item -ItemType Directory -Path $output|Out-Null
function Run-Step([string]$name,[string]$program,[string[]]$arguments){
 # Preserve stderr and native exit code even when a test returns failure.
 $oldPreference=$ErrorActionPreference
 try{$ErrorActionPreference='Continue'; & $program @arguments 1> (Join-Path $output ($name+'.log')) 2> (Join-Path $output ($name+'.stderr.log'));$code=$LASTEXITCODE}
 finally{$ErrorActionPreference=$oldPreference}
 $report.steps += [ordered]@{name=$name;exit_code=$code}
 if($code -ne 0){throw "Step $name failed with exit $code; logs retained"}
}
try{
 foreach($a in $old.artifacts){if($a.name.EndsWith('.dll') -or $a.name -eq 'MainmenuTilePipeline.exe' -or $a.name -eq 'MainmenuMeshPipeline.exe'){Copy-Item -LiteralPath (Join-Path $previous $a.name) -Destination (Join-Path $output $a.name)}}
 $tests=Join-Path $output 'NavigationMeshTilesTests.exe'
 $arguments=@('/nologo','/warn:4','/target:exe',"/out:$tests")
 foreach($dll in @('GrimDynasty.NavigationMeshTiles.dll','GrimDynasty.Actions.dll','GrimDynasty.NavigationNearest.dll','GrimDynasty.NavigationTileData.dll')){$arguments += '/reference:'+(Join-Path $output $dll)}
 $arguments += $testsSource
 Run-Step 'compile-mesh-tests' $compiler $arguments
 Run-Step 'mesh-memory-tests' $tests @()
 Run-Step 'real-mesh-pipeline' (Join-Path $output 'MainmenuMeshPipeline.exe') @('--explicit-default-domain')
 Check-Inputs
 $report.mesh_published=$true;$report.status='passed-real-saved-data-mesh-and-nearest-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($f in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$f.Name;bytes=$f.Length;sha256=(Hash $f.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o')
 $report|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;steps=$report.steps;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 5
