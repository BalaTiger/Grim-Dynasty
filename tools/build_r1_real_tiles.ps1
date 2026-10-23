param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$output=Join-Path $taskRoot '.tooling\r1-real-tile-validation'
# Source hashes are frozen after the final predicate evidence review.
$pins=@{
 'src\reverse_client_actions\NavigationLayerPolygons.cs'='0e4cb0e9332aa418baf5c1d28687203fefa0025bdf721013cd9b9891b6f9ac70'
 'src\reverse_client_actions\NavigationLayerPolygonsTests.cs'='793b2cd6bf22949b7f094cbd877f85b59d341d52fe581fc5144197ddd152913d'
 'src\reverse_client_actions\MainmenuTilePipeline.cs'='bd76aa516aab1e8be5f88b98e8e4f41e3cbe0a2564a8fd5995404177480733c9'
 '.tooling\r1-navigation-layers-validation\GrimDynasty.PathingData.dll'='4f105f145ccdfb4b17b33b1167f8b700f06f38a4a07f8dc36cdf50f3d12aac4f'
 '.tooling\r1-navigation-layers-validation\GrimDynasty.NavigationLayers.dll'='80137bab7ceb54a3688655501b2d5c66b59de05d97a9a65edd052e8953e254ee'
 '.tooling\r1-contours-and-tiledata-validation\GrimDynasty.NavigationContours.dll'='c63bacce93c83700d3113ec36ade462a67b48b1030f2e293a39322e149cad263'
 '.tooling\r1-contours-and-tiledata-validation\GrimDynasty.NavigationTileData.dll'='b0459f94ca130467c2ec7f8c0379700250926c5a8eb8a3e6f617910a3bb02cd9'
 'docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin'='a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289'
}
if($args.Count -ne 0){throw 'No arguments supported'}
if((Get-Location).Path -ne $taskRoot){throw 'Fixed workspace required'}
if(Test-Path -LiteralPath $output){throw 'Existing output preserved; inspect before another run'}
function Check-Inputs {
 foreach($relative in $pins.Keys){if((Get-FileHash -LiteralPath (Join-Path $taskRoot $relative) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $pins[$relative]){throw "Frozen input changed: $relative"}}
}
Check-Inputs
$report=[ordered]@{started_utc=[DateTime]::UtcNow.ToString('o');status='running';inputs=$pins;steps=@();artifacts=@();original_read=$false;original_executed=$false;old_suites_rerun=$false;real_tile_serialized=$false;mesh_published=$false;gameplay_accepted=$false;failure=$null}
New-Item -ItemType Directory -Path $output|Out-Null
function Run-Step([string]$name,[string]$program,[string[]]$arguments){
 & $program @arguments *> (Join-Path $output ($name+'.log'))
 $code=$LASTEXITCODE;$report.steps += [ordered]@{name=$name;exit_code=$code}
 if($code -ne 0){throw "Step $name failed with exit $code; retained log in $output"}
}
try{
 foreach($name in @('GrimDynasty.PathingData.dll','GrimDynasty.NavigationLayers.dll')){Copy-Item -LiteralPath (Join-Path $taskRoot ('.tooling\r1-navigation-layers-validation\'+$name)) -Destination (Join-Path $output $name)}
 foreach($name in @('GrimDynasty.NavigationContours.dll','GrimDynasty.NavigationTileData.dll')){Copy-Item -LiteralPath (Join-Path $taskRoot ('.tooling\r1-contours-and-tiledata-validation\'+$name)) -Destination (Join-Path $output $name)}
 $pathing=Join-Path $output 'GrimDynasty.PathingData.dll';$layers=Join-Path $output 'GrimDynasty.NavigationLayers.dll'
 $contours=Join-Path $output 'GrimDynasty.NavigationContours.dll';$tiledata=Join-Path $output 'GrimDynasty.NavigationTileData.dll'
 $polygons=Join-Path $output 'GrimDynasty.NavigationPolygons.dll';$source=Join-Path $taskRoot 'src\reverse_client_actions'
 Run-Step 'compile-polygons' $compiler @('/nologo','/warn:4','/target:library',"/out:$polygons","/reference:$contours","/reference:$pathing","/reference:$layers",(Join-Path $source 'NavigationLayerPolygons.cs'))
 $tests=Join-Path $output 'NavigationLayerPolygonsTests.exe';$pipeline=Join-Path $output 'MainmenuTilePipeline.exe'
 Run-Step 'compile-polygon-tests' $compiler @('/nologo','/warn:4','/target:exe',"/out:$tests","/reference:$polygons","/reference:$contours","/reference:$pathing","/reference:$layers",(Join-Path $source 'NavigationLayerPolygonsTests.cs'))
 Run-Step 'compile-real-pipeline' $compiler @('/nologo','/warn:4','/target:exe',"/out:$pipeline","/reference:$polygons","/reference:$contours","/reference:$pathing","/reference:$layers","/reference:$tiledata",(Join-Path $source 'MainmenuTilePipeline.cs'))
 Run-Step 'polygon-memory-tests' $tests @()
 Run-Step 'real-tile-pipeline' $pipeline @('--explicit-default-domain')
 Check-Inputs
 $report.real_tile_serialized=$true
 $report.status='passed-real-saved-data-to-tile-bytes-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o')
 $report|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;started=$report.started_utc;finished=$report.finished_utc;steps=$report.steps;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 4
