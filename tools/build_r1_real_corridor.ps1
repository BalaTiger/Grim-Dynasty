param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$previous=Join-Path $taskRoot '.tooling\r1-real-mesh-validation-repair1'
$output=Join-Path $taskRoot '.tooling\r1-real-corridor-validation'
$priorHash='4cb3ae747781deff0275f3cbfaf3f463a717b4ec19d60486dad9d6c785e7712b'
$sourcePins=@{
 'NavigationSearchStorage.cs'='38b7a5c302971ee0f8a74e4b7c3a93a247ec86374fb300e4b5bfdfff6cbed4f5'
 'NavigationCorridorContracts.cs'='2ebee7bc8327051e8a84aea8f6f67082b761bab149cc7ed99e8773b0b48b630a'
 'NavigationCorridorTests.cs'='5cf46881d78882ee3eede26a99da8023ec88ceb9d29e13e3b0dc086b32d33c7d'
 'MainmenuCorridorPipeline.cs'='f8969eb117d2d3f0505ffa86802d67d68b6bef9b2555d3e910723beb7081912b'
}
function Hash([string]$p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace, no arguments required'}
if(Test-Path -LiteralPath $output){throw 'Existing corridor evidence preserved'}
if((Hash (Join-Path $previous 'result.json')) -ne $priorHash){throw 'Prior mesh result changed'}
$old=Get-Content -LiteralPath (Join-Path $previous 'result.json') -Raw|ConvertFrom-Json
if($old.status -ne 'passed-real-saved-data-mesh-and-nearest-only' -or !$old.mesh_published){throw 'Real mesh prerequisite failed'}
function Check-Inputs {
 foreach($p in $sourcePins.Keys){if($sourcePins[$p] -eq 'PENDING' -or (Hash (Join-Path $taskRoot ('src\reverse_client_actions\'+$p))) -ne $sourcePins[$p]){throw ('Unfrozen source: '+$p)}}
 if((Hash (Join-Path $taskRoot 'docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin')) -ne 'a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289'){throw 'Saved member changed'}
 foreach($a in $old.artifacts){if($a.name.EndsWith('.dll') -or $a.name -eq 'MainmenuTilePipeline.exe'){
  if((Hash (Join-Path $previous $a.name)) -ne $a.sha256){throw ('Previously compiled artifact changed: '+$a.name)}
 }}
}
Check-Inputs
$report=[ordered]@{started_utc=[DateTime]::UtcNow.ToString('o');status='running';prior_result_sha256=$priorHash;source_pins=$sourcePins;steps=@();artifacts=@();old_test_executables_rerun=$false;original_read=$false;original_executed=$false;actual_cross_polygon_corridor=$false;funnel_or_gameplay_accepted=$false;failure=$null}
New-Item -ItemType Directory -Path $output|Out-Null
function Run-Step([string]$name,[string]$program,[string[]]$arguments){
 $oldPreference=$ErrorActionPreference
 try{$ErrorActionPreference='Continue'; & $program @arguments 1> (Join-Path $output ($name+'.log')) 2> (Join-Path $output ($name+'.stderr.log'));$invocationSucceeded=$?;$code=$LASTEXITCODE}
 finally{$ErrorActionPreference=$oldPreference}
 $report.steps += [ordered]@{name=$name;invocation_succeeded=$invocationSucceeded;exit_code=$code}
 if(!$invocationSucceeded -or $code -ne 0){throw "Step $name failed (invocation=$invocationSucceeded, exit=$code); logs retained"}
}
try{
 foreach($a in $old.artifacts){if($a.name.EndsWith('.dll') -or $a.name -eq 'MainmenuTilePipeline.exe'){Copy-Item -LiteralPath (Join-Path $previous $a.name) -Destination (Join-Path $output $a.name)}}
 $references=@()
 foreach($dll in @('GrimDynasty.NavigationMeshTiles.dll','GrimDynasty.Actions.dll','GrimDynasty.NavigationNearest.dll','GrimDynasty.NavigationTileData.dll')){$references += '/reference:'+(Join-Path $output $dll)}
 $source=Join-Path $taskRoot 'src\reverse_client_actions'
 $library=Join-Path $output 'GrimDynasty.NavigationCorridor.dll'
 Run-Step 'compile-corridor' $compiler (@('/nologo','/warn:4','/target:library',"/out:$library")+$references+@((Join-Path $source 'NavigationSearchStorage.cs'),(Join-Path $source 'NavigationCorridorContracts.cs')))
 $references += "/reference:$library"
 $tests=Join-Path $output 'NavigationCorridorTests.exe'
 Run-Step 'compile-corridor-tests' $compiler (@('/nologo','/warn:4','/target:exe',"/out:$tests")+$references+@((Join-Path $source 'NavigationCorridorTests.cs')))
 $pipeline=Join-Path $output 'MainmenuCorridorPipeline.exe';$producer=Join-Path $output 'MainmenuTilePipeline.exe'
 Run-Step 'compile-real-corridor' $compiler (@('/nologo','/warn:4','/target:exe',"/out:$pipeline")+$references+@("/reference:$producer",(Join-Path $source 'MainmenuCorridorPipeline.cs')))
 Run-Step 'corridor-memory-tests' $tests @()
 Run-Step 'real-corridor-pipeline' $pipeline @('--explicit-default-domain')
 Check-Inputs
 $report.actual_cross_polygon_corridor=$true;$report.status='passed-actual-cross-polygon-corridor-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($f in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$f.Name;bytes=$f.Length;sha256=(Hash $f.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o')
 $report|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;steps=$report.steps;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 5
