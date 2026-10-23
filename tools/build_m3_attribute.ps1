param([Parameter(Mandatory=$true)][string]$PackageDirectory,[ValidateSet('attribute','statistics','first-insert')][string]$Component='attribute')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$target=[IO.Path]::GetFullPath($PackageDirectory)
if(!$target.StartsWith((Join-Path $root 'artifacts')+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Output must stay in project artifacts'}
if(Test-Path -LiteralPath $target){throw 'Fresh package required; prior evidence must be preserved'}
function Check-Path([string]$path){for($p=$path;$p;$p=Split-Path -Parent $p){if(Test-Path -LiteralPath $p){if((Get-Item -LiteralPath $p).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Reparse path rejected'}}}}
Check-Path $target
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if((Get-FileHash -LiteralPath $compiler -Algorithm SHA256).Hash -ne '46809206887326D2D24DB1EFF1F3064DE972C3451ABE766B49111450A5E08E00'){throw 'Compiler fingerprint changed'}
$module=if($Component -eq 'attribute'){'NativeAttributeGetter'}elseif($Component -eq 'statistics'){'NativeDamageStatistics'}else{'NativeStatisticsFirstInsert'}
$relative=@('src/combat_runtime/NativeDamageSettlement.cs',('src/combat_runtime/'+$module+'.cs'),('src/combat_runtime/'+$module+'Tests.cs'))
if($Component -eq 'first-insert'){$relative+=@('src/combat_runtime/NativeDamageStatistics.cs','src/combat_runtime/NativeDamageStatisticsTests.cs','src/combat_runtime/FirstInsertTestProgram.cs')}
$sources=@($relative | ForEach-Object {Join-Path $root $_})
$pins=@()
foreach($path in $sources){Check-Path $path;$pins+=@([ordered]@{path=$path.Substring($root.Length+1).Replace('\','/');sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()})}
if($pins[0].sha256 -ne 'b93325f98eb36bc9918b5c91a32661a8bcbaf6b289b8076b1d88622edbc02f1a'){throw 'Accepted settlement source changed; review regression scope'}
New-Item -ItemType Directory -Path $target | Out-Null
$manifest=[ordered]@{schema='grim-dynasty-m3-conditional-component-build-v1';component=$Component;started_utc=[DateTime]::UtcNow.ToString('o');status='Building';sources=$pins;scope='Own conditional combat dependency and settlement binding only. Console fixture, no scene/original binary access.'}
$manifestPath=Join-Path $target 'build-manifest.json'
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
function Invoke-Captured([string]$exe,[string[]]$arguments,[string]$logPath){
 $saved=$ErrorActionPreference
 try{$ErrorActionPreference='Continue';$log=@(& $exe @arguments 2>&1);$code=$LASTEXITCODE}finally{$ErrorActionPreference=$saved}
 [IO.File]::WriteAllLines($logPath,[string[]]$log,[Text.UTF8Encoding]::new($false))
 if($code -ne 0){throw ('Command failed with exit '+$code+'; see '+$logPath)}
 return ($log -join "`n")
}
try{
 $exe=Join-Path $target ($module+'Checks.exe')
 $entry=if($Component -eq 'first-insert'){'FirstInsertTestProgram'}else{$module+'Tests'}
 $arguments=@('/nologo','/noconfig',('/main:'+$entry),'/target:exe','/checked+','/optimize+','/debug-','/platform:x64','/codepage:65001','/reference:System.dll','/reference:System.Core.dll',('/out:'+$exe))+$sources
 $compile=Invoke-Captured -exe $compiler -arguments $arguments -logPath (Join-Path $target 'compile.log')
 foreach($pin in $pins){if((Get-FileHash -LiteralPath (Join-Path $root $pin.path) -Algorithm SHA256).Hash -ne $pin.sha256){throw 'Source changed during build'}}
 $log=Invoke-Captured -exe $exe -arguments @() -logPath (Join-Path $target 'checks.log')
 if($log -notmatch ('PASS '+$module+': (\d+) assertions / (\d+) groups')){throw 'Missing component test summary'}
 $report=[ordered]@{status='Passed';component=$Component;completed_utc=[DateTime]::UtcNow.ToString('o');assertions=[int]$Matches[1];groups=[int]$Matches[2];original_read=$false;window_launched=$false;old_suites_rerun=$false;boundary='Explicit known inputs and recorded conditions only. No initial enemy attributes, native ABI, attack/death, missing-key insertion or full M3 claim.'}
 if($Component -eq 'first-insert'){
  if($log -notmatch 'PASS NativeDamageStatistics: (\d+) assertions / (\d+) groups'){throw 'Missing affected existing-statistics regression'}
  $report['old_suites_rerun']=$true;$report['statistics_regression_assertions']=[int]$Matches[1];$report['statistics_regression_groups']=[int]$Matches[2]
  $report['boundary']='First empty-node insertion under explicit own sentinel/count inputs and normal own allocation. Existing-key regression rerun because shared state gained nullable color/count. No native initializer, nonempty missing-key rebalance, attack/death producer or full M3 claim.'
 }
 $report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $target 'acceptance.json') -Encoding UTF8
 $manifest.status='Passed';$manifest['exe_sha256']=(Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant();$manifest['finished_utc']=[DateTime]::UtcNow.ToString('o')
 $manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
 Write-Output ('M3_CONDITIONAL_PACKAGE='+$target)
}catch{
 $manifest.status='FailedOrIncomplete';$manifest['error']=$_.Exception.Message
 $manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
 throw
}
