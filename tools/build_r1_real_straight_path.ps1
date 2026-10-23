param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$previous=Join-Path $taskRoot '.tooling\r1-real-corridor-validation'
$output=Join-Path $taskRoot '.tooling\r1-real-straight-path-validation'
$priorHash='5f5fd685da3b8eeb80bcec6c810db5ab6077e8e2e24f323cdb3173d2dc80215d'
$sourcePins=@{
 'NavigationSearchStorage.cs'='79f08b230d65c5f9c1115c738d7a634b180337fd9953bc651df963294aa614c2'
 'NavigationCorridorContracts.cs'='2ebee7bc8327051e8a84aea8f6f67082b761bab149cc7ed99e8773b0b48b630a'
 'NavigationPortalWrapper.cs'='d5bbc2674fbf63fe4e59c1c5907718fb72016ea2f35bf755a9135e576d4fadd8'
 'NavigationStraightPathContracts.cs'='912d4decd268c8e611c95f097346d4ecbf8b5a20efc69f2c1555606456360325'
 'NavigationStraightPathTests.cs'='ba34acb57e60f32fe468db0c9881dff7f85e6954082c16f1f4674b5c9dbe3d97'
 'MainmenuStraightPathPipeline.cs'='de54b930308ac456178f485339cbb042a0d43f6665963f65e8ebbfedba3f9eb2'
}
function Hash([string]$p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace, no arguments required'}
if(Test-Path -LiteralPath $output){throw 'Existing straight-path evidence preserved'}
if((Hash (Join-Path $previous 'result.json')) -ne $priorHash){throw 'Prior corridor result changed'}
$old=Get-Content -LiteralPath (Join-Path $previous 'result.json') -Raw|ConvertFrom-Json
if($old.status -ne 'passed-actual-cross-polygon-corridor-only' -or !$old.actual_cross_polygon_corridor){throw 'Real corridor prerequisite failed'}
function Check-Inputs {
 foreach($p in $sourcePins.Keys){if($sourcePins[$p] -eq 'PENDING' -or (Hash (Join-Path $taskRoot ('src\reverse_client_actions\'+$p))) -ne $sourcePins[$p]){throw ('Unfrozen source: '+$p)}}
 if((Hash (Join-Path $taskRoot 'docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin')) -ne 'a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289'){throw 'Saved member changed'}
 foreach($a in $old.artifacts){if($a.name.EndsWith('.dll') -or $a.name -eq 'MainmenuTilePipeline.exe' -or $a.name -eq 'NavigationCorridorTests.exe'){
  if((Hash (Join-Path $previous $a.name)) -ne $a.sha256){throw ('Previously compiled artifact changed: '+$a.name)}
 }}
}
Check-Inputs
$report=[ordered]@{started_utc=[DateTime]::UtcNow.ToString('o');status='running';prior_result_sha256=$priorHash;source_pins=$sourcePins;steps=@();artifacts=@();old_test_executables_rerun=@('NavigationCorridorTests.exe');original_read=$false;original_executed=$false;actual_waypoints=$false;gameplay_accepted=$false;failure=$null}
New-Item -ItemType Directory -Path $output|Out-Null
function Run-Step([string]$name,[string]$program,[string[]]$arguments){
 $oldPreference=$ErrorActionPreference
 try{$ErrorActionPreference='Continue'; & $program @arguments 1> (Join-Path $output ($name+'.log')) 2> (Join-Path $output ($name+'.stderr.log'));$invocationSucceeded=$?;$code=$LASTEXITCODE}
 finally{$ErrorActionPreference=$oldPreference}
 $report.steps += [ordered]@{name=$name;invocation_succeeded=$invocationSucceeded;exit_code=$code}
 if(!$invocationSucceeded -or $code -ne 0){throw "Step $name failed (invocation=$invocationSucceeded, exit=$code); logs retained"}
}
try{
 foreach($a in $old.artifacts){if(($a.name.EndsWith('.dll') -and $a.name -ne 'GrimDynasty.NavigationCorridor.dll') -or $a.name -eq 'MainmenuTilePipeline.exe' -or $a.name -eq 'NavigationCorridorTests.exe'){Copy-Item -LiteralPath (Join-Path $previous $a.name) -Destination (Join-Path $output $a.name)}}
 $references=@()
 foreach($dll in @('GrimDynasty.NavigationMeshTiles.dll','GrimDynasty.Actions.dll','GrimDynasty.NavigationNearest.dll','GrimDynasty.NavigationTileData.dll')){$references += '/reference:'+(Join-Path $output $dll)}
 $source=Join-Path $taskRoot 'src\reverse_client_actions'
 $library=Join-Path $output 'GrimDynasty.NavigationCorridor.dll'
 Run-Step 'compile-navigation-extension' $compiler (@('/nologo','/warn:4','/target:library',"/out:$library")+$references+@((Join-Path $source 'NavigationSearchStorage.cs'),(Join-Path $source 'NavigationCorridorContracts.cs'),(Join-Path $source 'NavigationPortalWrapper.cs'),(Join-Path $source 'NavigationStraightPathContracts.cs')))
 $references += "/reference:$library"
 $tests=Join-Path $output 'NavigationStraightPathTests.exe'
 Run-Step 'compile-straight-path-tests' $compiler (@('/nologo','/warn:4','/target:exe',"/out:$tests")+$references+@((Join-Path $source 'NavigationStraightPathTests.cs')))
 $pipeline=Join-Path $output 'MainmenuStraightPathPipeline.exe';$producer=Join-Path $output 'MainmenuTilePipeline.exe'
 Run-Step 'compile-real-straight-path' $compiler (@('/nologo','/warn:4','/target:exe',"/out:$pipeline")+$references+@("/reference:$producer",(Join-Path $source 'MainmenuStraightPathPipeline.cs')))
 Run-Step 'corridor-relevant-regression' (Join-Path $output 'NavigationCorridorTests.exe') @()
 Run-Step 'straight-path-memory-tests' $tests @()
 Run-Step 'real-straight-path-pipeline' $pipeline @('--explicit-default-domain')
 Check-Inputs
 $report.actual_waypoints=$true;$report.status='passed-actual-straight-path-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($f in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$f.Name;bytes=$f.Length;sha256=(Hash $f.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o')
 $report|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;steps=$report.steps;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 5
