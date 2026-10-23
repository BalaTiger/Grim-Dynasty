param()
$ErrorActionPreference = 'Stop'
$taskRoot = 'C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$output = Join-Path $taskRoot '.tooling\r1-navigation-layers-validation'
$sourcePins = @{
 'src\reverse_client_actions\NavigationLayerData.cs' = 'ceeac117c6e962a7aa45c297a637c5ba9ee7c175a48daed5570d04220ddebd5d'
 'src\reverse_client_actions\NavigationLayerDataTests.cs' = 'c2ad3d100411ecba96ed4210a79387a9af379ec145aa6e1f98980d87b6a88f5f'
 'src\reverse_client_actions\NavigationLayerCache.cs' = '2128abf6983dd23d25a3b8425d27c51666ed0a529889a4edb86cf0260b728efb'
 'src\reverse_client_actions\NavigationLayerCacheTests.cs' = '9853c6f6fc65b39be77790a1399ee91d1c8b952f3bd9152beed37a8d09e94d74'
 'src\reverse_client_actions\NavigationLayerRegions.cs' = '694bc1774517ba0a00d59716ad585fc9c437cbf0ab115982df6ae336dda4f6e2'
 'src\reverse_client_actions\NavigationLayerRegionsTests.cs' = '89063809024420bfdddbcba8bd384bdbf9c10ee6b16cef8a4e8dd27285b491fe'
 'src\reverse_client_actions\MainmenuLayerPipeline.cs' = '27c79388e0ee613c2b05b175d31c55178977f8f080dfc58fb6efb2c6c23b6f69'
 '.tooling\r1-pathing-data-validation\GrimDynasty.PathingData.dll' = '4f105f145ccdfb4b17b33b1167f8b700f06f38a4a07f8dc36cdf50f3d12aac4f'
 'docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin' = 'a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289'
}
if ($args.Count -ne 0) { throw 'No arguments supported' }
if ((Get-Location).Path -ne $taskRoot) { throw 'Fixed workspace required' }
if (Test-Path -LiteralPath $output) { throw 'Existing output preserved; inspect before another run' }
function Assert-Inputs {
 foreach ($relative in $sourcePins.Keys) {
  $path = Join-Path $taskRoot $relative
  if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $sourcePins[$relative]) { throw "Frozen input changed: $relative" }
 }
}
Assert-Inputs
$report = [ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o'); status='running'; compiler=$compiler; inputs=$sourcePins
 steps=@(); artifacts=@(); explicit_region_climb=4; climb_is_observed_native_runtime=$false
 original_read=$false; original_executed=$false; old_suites_rerun=$false
 final_mesh_built=$false; gameplay_accepted=$false; failure=$null
}
New-Item -ItemType Directory -Path $output | Out-Null
function Invoke-Logged([string]$name,[string]$program,[string[]]$arguments) {
 $log=Join-Path $output ($name+'.log')
 & $program @arguments *> $log
 $code=$LASTEXITCODE
 $report.steps += [ordered]@{name=$name;exit_code=$code;log=$log}
 if ($code -ne 0) { throw "Step $name failed with exit $code; inspect retained log" }
}
try {
 $pathing=Join-Path $output 'GrimDynasty.PathingData.dll'
 Copy-Item -LiteralPath (Join-Path $taskRoot '.tooling\r1-pathing-data-validation\GrimDynasty.PathingData.dll') -Destination $pathing
 $library=Join-Path $output 'GrimDynasty.NavigationLayers.dll'
 $sources=@('NavigationLayerData.cs','NavigationLayerCache.cs','NavigationLayerRegions.cs') | ForEach-Object {Join-Path $taskRoot ('src\reverse_client_actions\'+$_)}
 Invoke-Logged 'compile-library' $compiler (@('/nologo','/warn:4','/target:library',"/out:$library","/reference:$pathing")+$sources)
 foreach ($name in @('NavigationLayerDataTests','NavigationLayerCacheTests','NavigationLayerRegionsTests','MainmenuLayerPipeline')) {
  $exe=Join-Path $output ($name+'.exe')
  Invoke-Logged ('compile-'+$name) $compiler @('/nologo','/warn:4','/target:exe',"/out:$exe","/reference:$library","/reference:$pathing",(Join-Path $taskRoot ('src\reverse_client_actions\'+$name+'.cs')))
 }
 Invoke-Logged 'data-tests' (Join-Path $output 'NavigationLayerDataTests.exe') @()
 Invoke-Logged 'cache-tests' (Join-Path $output 'NavigationLayerCacheTests.exe') @()
 Invoke-Logged 'regions-tests' (Join-Path $output 'NavigationLayerRegionsTests.exe') @('--real-climb','4')
 Invoke-Logged 'real-shared-pipeline' (Join-Path $output 'MainmenuLayerPipeline.exe') @('--climb','4')
 Assert-Inputs
 $report.status='passed-real-data-through-regions-only'
} catch {
 $report.status='failed'; $report.failure=$_.Exception.Message
} finally {
 foreach ($artifact in Get-ChildItem -LiteralPath $output -File) {
  $report.artifacts += [ordered]@{name=$artifact.Name;bytes=$artifact.Length;sha256=(Get-FileHash -LiteralPath $artifact.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}
 }
 $report.finished_utc=[DateTime]::UtcNow.ToString('o')
 $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if ($report.status -eq 'failed') { throw $report.failure }
$report | ConvertTo-Json -Depth 6
