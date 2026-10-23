param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$output=Join-Path $taskRoot '.tooling\r1-real-mesh-validation'
$pins=@{
 'src\reverse_client_actions\NavigationMeshTiles.cs'='0419760954bf0707a7336ec681de99ef3936013b1f78426822a5ba8e67ca62a0'
 'src\reverse_client_actions\NavigationMeshTilesTests.cs'='d54789f41cd93230e056c9c7c40442eaecd88e026daba8952d1ddeefd3ad121f'
 'src\reverse_client_actions\MainmenuMeshPipeline.cs'='3ff9f87cd4dca7d8f8876a212c5f25e84f2741865fc9e1dc71299d0fed942455'
 '.tooling\r1-navigation-nearest-validation\GrimDynasty.Actions.dll'='3ea34074db9d5a67d7dffb702eb053b703dc3b754d0db442aeadd71247e15c58'
 '.tooling\r1-navigation-nearest-validation\GrimDynasty.NavigationNearest.dll'='75f303c482679df6ba2c8f01aad7759ba9ed322ad6386100525b690cd9b9dfd0'
 '.tooling\r1-real-tile-validation\GrimDynasty.PathingData.dll'='4f105f145ccdfb4b17b33b1167f8b700f06f38a4a07f8dc36cdf50f3d12aac4f'
 '.tooling\r1-real-tile-validation\GrimDynasty.NavigationLayers.dll'='80137bab7ceb54a3688655501b2d5c66b59de05d97a9a65edd052e8953e254ee'
 '.tooling\r1-real-tile-validation\GrimDynasty.NavigationContours.dll'='c63bacce93c83700d3113ec36ade462a67b48b1030f2e293a39322e149cad263'
 '.tooling\r1-real-tile-validation\GrimDynasty.NavigationTileData.dll'='b0459f94ca130467c2ec7f8c0379700250926c5a8eb8a3e6f617910a3bb02cd9'
 '.tooling\r1-real-tile-validation\GrimDynasty.NavigationPolygons.dll'='6e33b858bc8073d59beacd7cc0a94d5ad15c24aa6f8f3ac966f2966175fa6856'
 '.tooling\r1-real-tile-validation\MainmenuTilePipeline.exe'='4d006bb08378360bdcccca167bc8d841b7f2ddbef48b55325ab9973658d173e7'
 'docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin'='a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289'
}
if($args.Count -ne 0){throw 'No arguments supported'}
if((Get-Location).Path -ne $taskRoot){throw 'Fixed workspace required'}
if(Test-Path -LiteralPath $output){throw 'Existing output preserved; inspect before another run'}
function Check-Inputs {
 foreach($relative in $pins.Keys){if((Get-FileHash -LiteralPath (Join-Path $taskRoot $relative) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $pins[$relative]){throw "Frozen input changed: $relative"}}
}
Check-Inputs
$report=[ordered]@{started_utc=[DateTime]::UtcNow.ToString('o');status='running';inputs=$pins;steps=@();artifacts=@();original_read=$false;original_executed=$false;old_suites_rerun=$false;producer_reused_in_relevant_integration=$true;mesh_published=$false;gameplay_accepted=$false;failure=$null}
New-Item -ItemType Directory -Path $output|Out-Null
function Run-Step([string]$name,[string]$program,[string[]]$arguments){
 & $program @arguments *> (Join-Path $output ($name+'.log'))
 $code=$LASTEXITCODE;$report.steps += [ordered]@{name=$name;exit_code=$code}
 if($code -ne 0){throw "Step $name failed with exit $code; retained log in $output"}
}
try{
 foreach($relative in $pins.Keys){if($relative.StartsWith('.tooling\')){Copy-Item -LiteralPath (Join-Path $taskRoot $relative) -Destination (Join-Path $output ([IO.Path]::GetFileName($relative)))}}
 $actions=Join-Path $output 'GrimDynasty.Actions.dll';$nearest=Join-Path $output 'GrimDynasty.NavigationNearest.dll';$tiledata=Join-Path $output 'GrimDynasty.NavigationTileData.dll'
 $producer=Join-Path $output 'MainmenuTilePipeline.exe';$mesh=Join-Path $output 'GrimDynasty.NavigationMeshTiles.dll';$source=Join-Path $taskRoot 'src\reverse_client_actions'
 Run-Step 'compile-mesh' $compiler @('/nologo','/warn:4','/target:library',"/out:$mesh","/reference:$actions","/reference:$nearest",(Join-Path $source 'NavigationMeshTiles.cs'))
 $tests=Join-Path $output 'NavigationMeshTilesTests.exe';$pipeline=Join-Path $output 'MainmenuMeshPipeline.exe'
 Run-Step 'compile-mesh-tests' $compiler @('/nologo','/warn:4','/target:exe',"/out:$tests","/reference:$mesh","/reference:$actions","/reference:$nearest","/reference:$tiledata",(Join-Path $source 'NavigationMeshTilesTests.cs'))
 Run-Step 'compile-real-pipeline' $compiler @('/nologo','/warn:4','/target:exe',"/out:$pipeline","/reference:$mesh","/reference:$actions","/reference:$nearest","/reference:$tiledata","/reference:$producer",(Join-Path $source 'MainmenuMeshPipeline.cs'))
 Run-Step 'mesh-memory-tests' $tests @()
 Run-Step 'real-mesh-pipeline' $pipeline @('--explicit-default-domain')
 Check-Inputs
 $report.mesh_published=$true
 $report.status='passed-real-saved-data-mesh-and-nearest-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o')
 $report|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;started=$report.started_utc;finished=$report.finished_utc;steps=$report.steps;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 4
