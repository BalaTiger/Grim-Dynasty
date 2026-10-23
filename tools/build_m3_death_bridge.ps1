param([Parameter(Mandatory=$true)][string]$PackageDirectory)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$target=[IO.Path]::GetFullPath($PackageDirectory)
if(!$target.StartsWith((Join-Path $root 'artifacts')+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Project artifacts output required'}
if(Test-Path -LiteralPath $target){throw 'Fresh output required'}
function Check-Path([string]$path){for($p=$path;$p;$p=Split-Path -Parent $p){if(Test-Path -LiteralPath $p){if((Get-Item -LiteralPath $p).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Reparse path rejected'}}}}
Check-Path $target
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if((Get-FileHash -LiteralPath $compiler -Algorithm SHA256).Hash -ne '46809206887326D2D24DB1EFF1F3064DE972C3451ABE766B49111450A5E08E00'){throw 'Compiler fingerprint changed'}
$m1Path=Join-Path $root 'artifacts/m1-offline-20261022-03/build-manifest.json'
if((Get-FileHash -LiteralPath $m1Path -Algorithm SHA256).Hash -ne '98EB9692A44BBB0E301942E693A74751810BDF5ECBD62DDE89A1DE41BD15F3A5'){throw 'M1 manifest changed'}
$m1=Get-Content -LiteralPath $m1Path -Raw | ConvertFrom-Json
foreach($pin in $m1.sources){if((Get-FileHash -LiteralPath (Join-Path $root $pin.path) -Algorithm SHA256).Hash -ne $pin.sha256){throw ('M1 baseline changed: '+$pin.path)}}
$relative=@($m1.sources | ForEach-Object {$_.path})+@(
 'src/task_kills/KillCondition.cs','src/session_runtime/StrictJson.cs','src/session_runtime/SessionState.cs',
 'src/session_runtime/SessionStore.cs','src/session_runtime/SessionProgram.cs','src/session_runtime/SessionCoreTests.cs',
 'src/session_runtime/SessionDeathJournal.cs','src/session_runtime/SessionDeathObjective.cs','src/session_runtime/SessionDeathTests.cs',
 'src/scene_runtime/SceneWorld.cs','src/scene_runtime/SceneState.cs','src/scene_runtime/SceneRenderer.cs',
 'src/scene_runtime/SceneAgents.cs','src/scene_runtime/SceneAgentPersistence.cs','src/scene_runtime/SceneTests.cs',
 'src/scene_runtime/ScenePatrolTests.cs','src/scene_runtime/SceneDeathTests.cs','src/scene_runtime/DeathBridgeTestProgram.cs')
$sources=@($relative | ForEach-Object {Join-Path $root $_});$pins=@()
foreach($path in $sources){Check-Path $path;$pins+=@([ordered]@{path=$path.Substring($root.Length+1).Replace('\','/');sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()})}
$legacy=Join-Path $root 'artifacts/m2-session-20261022-01/runtime-data/saves/hero.save'
Check-Path $legacy
$legacyHash=(Get-FileHash -LiteralPath $legacy -Algorithm SHA256).Hash.ToLowerInvariant()
if($legacyHash -ne 'c9aa2cde9e62e694d7703a82ebb24ca142f4d2abdee5d24d2711a3af68eaea9b'){throw 'Previously accepted legacy fixture changed'}
$cliScript=Join-Path $PSScriptRoot 'test_m2_session.ps1';Check-Path $cliScript
$cliHash=(Get-FileHash -LiteralPath $cliScript -Algorithm SHA256).Hash.ToLowerInvariant()
New-Item -ItemType Directory -Path $target | Out-Null
$manifest=[ordered]@{schema='grim-dynasty-m3-death-bridge-build-v1';status='Building';started_utc=[DateTime]::UtcNow.ToString('o');sources=$pins;
 legacy_fixture_sha256=$legacyHash;cli_regression_script_sha256=$cliHash;scope='Internal explicitly external death fixtures, one session wallet/journal, atomic scene publication. No combat producer, original read, GUI or native runtime claim.'}
$manifestPath=Join-Path $target 'build-manifest.json'
$manifest | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
function Invoke-Captured([string]$exe,[string[]]$arguments,[string]$logPath){
 $saved=$ErrorActionPreference
 try{$ErrorActionPreference='Continue';$log=@(& $exe @arguments 2>&1);$code=$LASTEXITCODE}finally{$ErrorActionPreference=$saved}
 [IO.File]::WriteAllLines($logPath,[string[]]$log,[Text.UTF8Encoding]::new($false))
 if($code -ne 0){throw ('Command failed '+$code+': '+$logPath)}
 return ($log -join "`n")
}
try{
 $common=@('/nologo','/noconfig','/target:exe','/checked+','/optimize+','/debug-','/platform:x64','/codepage:65001','/reference:System.dll','/reference:System.Core.dll','/reference:System.Web.Extensions.dll','/reference:System.Drawing.dll')
 foreach($kind in @('session','checks')){
  $main=if($kind -eq 'session'){'GrimDynasty.Session.SessionProgram'}else{'GrimDynasty.Scene.DeathBridgeTestProgram'}
  $name=if($kind -eq 'session'){'gd-session.exe'}else{'DeathBridgeChecks.exe'}
  $compileArgs=$common+@(('/main:'+$main),('/out:'+(Join-Path $target $name)))+$sources
  $compile=Invoke-Captured -exe $compiler -arguments $compileArgs -logPath (Join-Path $target ('compile-'+$kind+'.log'))
 }
 foreach($pin in $pins){if((Get-FileHash -LiteralPath (Join-Path $root $pin.path) -Algorithm SHA256).Hash -ne $pin.sha256){throw 'Source changed during compilation'}}
 $validation=Join-Path $target 'validation';New-Item -ItemType Directory -Path $validation | Out-Null
 Copy-Item -LiteralPath $legacy -Destination (Join-Path $validation 'legacy-m2.save')
 if((Get-FileHash -LiteralPath (Join-Path $validation 'legacy-m2.save') -Algorithm SHA256).Hash -ne $legacyHash){throw 'Old fixture changed'}
 $log=Invoke-Captured -exe (Join-Path $target 'DeathBridgeChecks.exe') -arguments @($validation) -logPath (Join-Path $target 'core-tests.log')
 if($log -notmatch 'M3_DEATH_BRIDGE SESSION=(\d+) SCENE=(\d+) OLD_M2_FIXTURE=1 SCENE_REGRESSION=(\d+) PATROL_REGRESSION=(\d+)'){throw 'Missing bridge test summary'}
 $report=[ordered]@{status='Passed';completed_utc=[DateTime]::UtcNow.ToString('o');session_new=[int]$Matches[1];scene_new=[int]$Matches[2];legacy_fixture=1;scene_rerun=[int]$Matches[3];patrol_integration_rerun=[int]$Matches[4];window_launched=$false;original_read=$false;
 boundary='External fixture observations only. Enemy HP/attack/death production remain Unsupported. Existing M2 core/CLI and scene persistence rerun due journal/schema integration; unrelated combat modules not rerun.'}
 foreach($phase in @('prepare-restart','verify-restart')){
  $restart=Invoke-Captured -exe (Join-Path $target 'DeathBridgeChecks.exe') -arguments @($phase,(Join-Path $validation 'restart/runtime-data/saves')) -logPath (Join-Path $target ($phase+'.log'))
  if($restart -notmatch ('M3_DEATH_RESTART '+$phase+'=(\d+)')){throw 'Missing process restart result'}
  $report[$phase]=[int]$Matches[1]
 }
 if((Get-FileHash -LiteralPath $cliScript -Algorithm SHA256).Hash -ne $cliHash){throw 'CLI regression script changed'}
 & $cliScript -PackageDirectory $target
 if(!$?){throw 'CLI regression failed'}
 $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $target 'death-acceptance.json') -Encoding UTF8
 $manifest.status='Passed';$manifest['finished_utc']=[DateTime]::UtcNow.ToString('o');$manifest['checks_sha256']=(Get-FileHash -LiteralPath (Join-Path $target 'DeathBridgeChecks.exe') -Algorithm SHA256).Hash.ToLowerInvariant()
 $manifest | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
 Write-Output ('M3_DEATH_BRIDGE_PACKAGE='+$target)
}catch{
 $manifest.status='FailedOrIncomplete';$manifest['error']=$_.Exception.Message
 $manifest | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
 throw
}
