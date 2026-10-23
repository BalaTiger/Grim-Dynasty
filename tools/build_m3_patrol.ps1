param([Parameter(Mandatory=$true)][string]$PackageDirectory)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$target=[IO.Path]::GetFullPath($PackageDirectory)
if(!$target.StartsWith((Join-Path $root 'artifacts')+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Artifact output must stay in project artifacts'}
if(Test-Path -LiteralPath $target){throw 'Fresh package required; do not overwrite prior evidence'}
function Check-Path([string]$path){for($p=$path;$p;$p=Split-Path -Parent $p){if(Test-Path -LiteralPath $p){if((Get-Item -LiteralPath $p).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Reparse path rejected'}}}}
Check-Path $target
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if((Get-FileHash -LiteralPath $compiler -Algorithm SHA256).Hash -ne '46809206887326D2D24DB1EFF1F3064DE972C3451ABE766B49111450A5E08E00'){throw 'Compiler fingerprint changed'}
$m2Path=Join-Path $root 'artifacts/m2-session-20261022-01/build-manifest.json'
if((Get-FileHash -LiteralPath $m2Path -Algorithm SHA256).Hash -ne 'ADE46A6314AD0E4E8DE5D8327E51E77401C88FF35151ED9300291808225A32B6'){throw 'Accepted M2 manifest changed'}
$m2=Get-Content -LiteralPath $m2Path -Raw | ConvertFrom-Json
foreach($source in $m2.sources){if((Get-FileHash -LiteralPath (Join-Path $root $source.path) -Algorithm SHA256).Hash -ne $source.sha256){throw ('M2 source changed: '+$source.path)}}
$relative=@($m2.sources | ForEach-Object {$_.path})+@(
 'src/scene_runtime/SceneWorld.cs','src/scene_runtime/SceneRenderer.cs','src/scene_runtime/SceneState.cs',
 'src/scene_runtime/SceneWindow.cs','src/scene_runtime/SceneProgram.cs','src/scene_runtime/SceneTests.cs',
 'src/scene_runtime/SceneAgents.cs','src/scene_runtime/SceneAgentsTests.cs','src/scene_runtime/SceneAgentPersistence.cs','src/scene_runtime/ScenePatrolTests.cs',
 'src/combat_runtime/NativeDamageSettlement.cs','src/combat_runtime/NativeDamageSettlementTests.cs')
$sources=@($relative | ForEach-Object {Join-Path $root $_})
$fingerprints=@()
foreach($path in $sources){Check-Path $path;$fingerprints+=@([ordered]@{path=$path.Substring($root.Length+1).Replace('\','/');sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()})}
New-Item -ItemType Directory -Path $target | Out-Null
$manifest=[ordered]@{schema='grim-dynasty-m3-patrol-build-v1';started_utc=[DateTime]::UtcNow.ToString('o');status='Building';sources=$fingerprints;
 scope='Own scene patrol integration and bounded settlement model. Headless tests/bitmap rendering only, no window launch, original resources or game process. Complete combat/death/trade/equipment not claimed.'}
$manifestPath=Join-Path $target 'build-manifest.json'
$manifest | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
try{
 $common=@('/nologo','/noconfig','/main:GrimDynasty.Scene.SceneProgram','/checked+','/optimize+','/debug-','/platform:x64','/codepage:65001',
 '/reference:System.dll','/reference:System.Core.dll','/reference:System.Web.Extensions.dll','/reference:System.Drawing.dll','/reference:System.Windows.Forms.dll')
 foreach($kind in @('window','tests')){
  $name=if($kind -eq 'window'){'gd-scene.exe'}else{'SceneChecks.exe'}
  $type=if($kind -eq 'window'){'/target:winexe'}else{'/target:exe'}
  $log=@(& $compiler @common $type ('/out:'+(Join-Path $target $name)) @sources 2>&1);$code=$LASTEXITCODE
  [IO.File]::WriteAllLines((Join-Path $target ('compile-'+$kind+'.log')),[string[]]$log,[Text.UTF8Encoding]::new($false))
  if($code -ne 0){throw ('Compile failed: '+$kind)}
 }
 foreach($pin in $fingerprints){if((Get-FileHash -LiteralPath (Join-Path $root $pin.path) -Algorithm SHA256).Hash -ne $pin.sha256){throw 'Source changed during build'}}
 # Only the console test entry is executed. It renders to bitmaps without a Form.
 $testExe=Join-Path $target 'SceneChecks.exe'
 $log=@(& $testExe --test-patrol (Join-Path $target 'validation') 2>&1);$code=$LASTEXITCODE
 [IO.File]::WriteAllLines((Join-Path $target 'checks.log'),[string[]]$log,[Text.UTF8Encoding]::new($false))
 if($code -ne 0){throw ('Headless tests exited '+$code)}
 $text=$log -join "`n"
 if($text -notmatch 'M3_PATROL_CHECKS=(\d+) INTEGRATION=(\d+) SCENE_REGRESSION=(\d+)'){throw 'Missing patrol test summary'}
 $report=[ordered]@{status='Passed';completed_utc=[DateTime]::UtcNow.ToString('o');patrol_checks=[int]$Matches[1];integration_checks=[int]$Matches[2];scene_regression_checks=[int]$Matches[3];window_launched=$false;original_read=$false}
 if($text -notmatch 'PASS NativeDamageSettlement: (\d+) assertions / (\d+) groups'){throw 'Missing settlement test summary'}
 $report['settlement_assertions']=[int]$Matches[1];$report['settlement_groups']=[int]$Matches[2]
 $report['boundary']='Own adapters/conditional settlement only. No complete native attack/death, AI, equipment, commerce or full M3 acceptance.'
 $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $target 'acceptance.json') -Encoding UTF8
 $manifest.status='Passed';$manifest['exe_sha256']=(Get-FileHash -LiteralPath (Join-Path $target 'gd-scene.exe') -Algorithm SHA256).Hash.ToLowerInvariant()
 $manifest['checks_sha256']=(Get-FileHash -LiteralPath $testExe -Algorithm SHA256).Hash.ToLowerInvariant();$manifest['finished_utc']=[DateTime]::UtcNow.ToString('o')
 $manifest | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
 Write-Output ('M3_PATROL_PACKAGE='+$target)
}catch{
 $manifest.status='FailedOrIncomplete';$manifest['error']=$_.Exception.Message
 $manifest | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
 throw
}
