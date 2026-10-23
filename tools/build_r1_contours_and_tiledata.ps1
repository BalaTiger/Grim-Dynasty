param()
$ErrorActionPreference = 'Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$output=Join-Path $taskRoot '.tooling\r1-contours-and-tiledata-validation'
$pins=@{
 'src\reverse_client_actions\NavigationLayerContours.cs'='cef6dcc363249e88550d33d0a256c3a75d59897f7596e47a5e90c9cdc97abf0d'
 'src\reverse_client_actions\NavigationLayerContoursTests.cs'='bc5effedaa9cd324460e0047b36a084bb33bf62696a4d60862d740d05a93161d'
 'src\reverse_client_actions\MainmenuContourProbe.cs'='ad0c0224a82bf22022ff466e5ea5a445d457c0bec38cceb96eff134ed482b84b'
 'src\reverse_client_actions\NavigationTileData.cs'='172a8f5fbe41ecb479a2eaf095b9bfe5ced93258e8fb41776c5966ba2cb34f42'
 'src\reverse_client_actions\NavigationTileDataTests.cs'='7c6bc7994e58864bc29ab63c64b6089b89f95bc36d0f8c6edfa8ebb409f0e8e4'
 '.tooling\r1-navigation-layers-validation\GrimDynasty.NavigationLayers.dll'='80137bab7ceb54a3688655501b2d5c66b59de05d97a9a65edd052e8953e254ee'
 '.tooling\r1-navigation-layers-validation\GrimDynasty.PathingData.dll'='4f105f145ccdfb4b17b33b1167f8b700f06f38a4a07f8dc36cdf50f3d12aac4f'
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
 $pathing=Join-Path $output 'GrimDynasty.PathingData.dll';$layers=Join-Path $output 'GrimDynasty.NavigationLayers.dll'
 foreach($name in @('GrimDynasty.PathingData.dll','GrimDynasty.NavigationLayers.dll')){Copy-Item -LiteralPath (Join-Path $taskRoot ('.tooling\r1-navigation-layers-validation\'+$name)) -Destination (Join-Path $output $name)}
 $contours=Join-Path $output 'GrimDynasty.NavigationContours.dll';$tile=Join-Path $output 'GrimDynasty.NavigationTileData.dll'
 $source=Join-Path $taskRoot 'src\reverse_client_actions'
 Run-Step 'compile-contours' $compiler @('/nologo','/warn:4','/target:library',"/out:$contours","/reference:$pathing","/reference:$layers",(Join-Path $source 'NavigationLayerContours.cs'))
 Run-Step 'compile-tiledata' $compiler @('/nologo','/warn:4','/target:library',"/out:$tile",(Join-Path $source 'NavigationTileData.cs'))
 $contourTests=Join-Path $output 'NavigationLayerContoursTests.exe';$tileTests=Join-Path $output 'NavigationTileDataTests.exe';$probe=Join-Path $output 'MainmenuContourProbe.exe'
 Run-Step 'compile-contour-tests' $compiler @('/nologo','/warn:4','/target:exe',"/out:$contourTests","/reference:$pathing","/reference:$layers","/reference:$contours",(Join-Path $source 'NavigationLayerContoursTests.cs'))
 Run-Step 'compile-tiledata-tests' $compiler @('/nologo','/warn:4','/target:exe',"/out:$tileTests","/reference:$tile",(Join-Path $source 'NavigationTileDataTests.cs'))
 Run-Step 'compile-real-probe' $compiler @('/nologo','/warn:4','/target:exe',"/out:$probe","/reference:$contourTests",(Join-Path $source 'MainmenuContourProbe.cs'))
 Run-Step 'contour-memory-tests' $contourTests @()
 Run-Step 'tiledata-memory-tests' $tileTests @()
 Run-Step 'real-contour-probe' $probe @()
 Check-Inputs
 $report.status='passed-real-contours-and-own-tile-format-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o')
 $report|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;started=$report.started_utc;finished=$report.finished_utc;steps=$report.steps;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 4
