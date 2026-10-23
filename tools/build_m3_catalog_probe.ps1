param([switch]$Slice)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if($root -ne 'C:\Users\zhuzi\Documents\Grim-Dynasty'){throw 'This reviewed evidence probe is pinned to the current workspace'}
if($Slice){
 $target=Join-Path $root 'artifacts\m3-content-slice-20261022-01'
 $reportRelative='docs/m3-evidence/m3-content-slice/content-slice.json'
 $bodyBudget=2;$probeArguments=@('-slice')
}else{
 $target=Join-Path $root 'artifacts\m3-catalog-probe-20261022-01'
 $reportRelative='docs/m3-evidence/content-candidates.json'
 $bodyBudget=0;$probeArguments=@()
}
$report=Join-Path $root $reportRelative
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if(Test-Path -LiteralPath $target){throw 'Probe artifact directory exists; refusing overwrite or implicit rerun'}
if(Test-Path -LiteralPath $report){throw 'Catalog evidence exists; refusing overwrite or implicit rerun'}
function Assert-NoReparse([string]$path){
 for($cursor=$path;$cursor;$cursor=Split-Path -Parent $cursor){
  if(Test-Path -LiteralPath $cursor){if((Get-Item -LiteralPath $cursor).Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Reparse path rejected: '+$cursor)}}
 }
}
Assert-NoReparse $target
Assert-NoReparse $report
$compilerHash=(Get-FileHash -LiteralPath $compiler -Algorithm SHA256).Hash.ToLowerInvariant()
if($compilerHash -ne '46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'){throw 'Compiler fingerprint changed; review required'}
$sourcePins=@(
 @{path='src/resource_codec/Lz4Block.cs';sha256='821dfbfe3695482b0da88b5dc8a948600b6c98c4015c1ae978dbd23b1105be24'},
 @{path='src/resource_codec/ResourceContainers.cs';sha256='f414cdecdaa58ba7d24cd7a8d6384f98874fc4a267fa3512e3b80bfb634b82a3'},
 @{path='tools/static_analysis/M3CatalogProbe.cs';sha256='f515d18c4b5f6e53c9307f2fd06e2d5f766e6f2a62c417b8dd1f314a9438b614'}
)
$sources=@()
foreach($pin in $sourcePins){
 $path=Join-Path $root $pin.path;Assert-NoReparse $path
 if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $pin.sha256){throw ('Source fingerprint changed: '+$pin.path)}
 $sources+=$path
}
New-Item -ItemType Directory -Path $target | Out-Null
if(!(Test-Path -LiteralPath (Split-Path -Parent $report))){New-Item -ItemType Directory -Path (Split-Path -Parent $report) | Out-Null}
$exe=Join-Path $target 'M3CatalogProbe.exe'
$arguments=@('/nologo','/noconfig','/target:exe','/main:M3CatalogProbe','/checked+','/optimize+','/debug-',
 '/codepage:65001','/reference:System.dll','/reference:System.Core.dll','/reference:System.Web.Extensions.dll',"/out:$exe")+$sources
$compileOutput=@(& $compiler @arguments 2>&1)
$compileExit=$LASTEXITCODE
[IO.File]::WriteAllLines((Join-Path $target 'compile.log'),[string[]]$compileOutput,[Text.UTF8Encoding]::new($false))
if($compileExit -ne 0){throw "Catalog probe compile failed ($compileExit); original ARZ was not opened by this script"}
$manifest=[ordered]@{schema='grim-dynasty-m3-catalog-probe-build-v1';created_utc=[DateTime]::UtcNow.ToString('o');
 compiler=$compiler;compiler_sha256=$compilerHash;sources=$sourcePins;exe_sha256=(Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant();
 input='D:\Steam\steamapps\common\Grim Dawn\database\database.arz';input_expected_bytes=58529720;
 input_expected_sha256='9e3532250cdb611a4cc989997c8f97fcec2d6c422063a845e6c43cd538c30603';
 report=$reportRelative;execution_status='Running';slice_mode=[bool]$Slice;record_body_budget=$bodyBudget;arguments=$probeArguments;
 previous_probe_source_sha256='dacd25fa81ce85f0a2b2192e8b6432b5a585e6e8177b0950cadebeb53fba31c4';
 preserved_index_evidence='docs/m3-evidence/content-candidates.json';
 scope='One fixed helper execution. Default: index only, Monster/ArmorProtective. Slice: Monster name contains zombie and item ArmorProtective_Chest, at most 8 each; decode first actual name from each nonempty sorted group (maximum 2 records, each at most 8MiB packed/unpacked). No recursive references, texture extraction, game process/DLL, player saves, tests, GUI, installation or security changes. Original SHA256 uses 64KiB blocks before/after.'}
$manifestPath=Join-Path $target 'build-manifest.json'
$manifest | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
try {
 $runOutput=@(& $exe @probeArguments 2>&1)
 $runExit=$LASTEXITCODE
 [IO.File]::WriteAllLines((Join-Path $target 'run.log'),[string[]]$runOutput,[Text.UTF8Encoding]::new($false))
 if($runExit -ne 0){throw "Catalog helper exited $runExit; inspect retained run.log"}
 $evidence=Get-Content -LiteralPath $report -Raw -Encoding UTF8 | ConvertFrom-Json
 if($evidence.status -ne 'Collected' -or $evidence.sha256_before -ne $manifest.input_expected_sha256 -or $evidence.sha256_after -ne $manifest.input_expected_sha256 -or $evidence.bodyreadcount -lt 0 -or $evidence.bodyreadcount -gt $bodyBudget){throw 'Catalog evidence did not meet pinned read-only scope'}
 if(@($evidence.enemy_candidates.entries).Count -gt 8 -or @($evidence.armor_candidates.entries).Count -gt 8){throw 'Candidate output exceeded reviewed cap'}
 if($Slice){
  $expectedBodies=[int]($evidence.enemy_candidates.total_matches -gt 0)+[int]($evidence.armor_candidates.total_matches -gt 0)
  if($evidence.schema -ne 'grim-dynasty-m3-content-slice-v1' -or $evidence.bodyreadcount -ne $expectedBodies -or @($evidence.decoded_records).Count -ne $expectedBodies){throw 'Slice did not decode exactly the first selected record of each nonempty group'}
 }elseif($evidence.schema -ne 'grim-dynasty-m3-content-candidates-v1'){throw 'Unexpected default catalog schema'}
 $manifest.execution_status='Collected'
 $manifest['report_sha256']=(Get-FileHash -LiteralPath $report -Algorithm SHA256).Hash.ToLowerInvariant()
 $manifest['completed_utc']=[DateTime]::UtcNow.ToString('o')
 $manifest | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
 Write-Output ('M3_CATALOG_REPORT='+$report)
} catch {
 $manifest.execution_status='FailedOrIncomplete'
 $manifest['error']=$_.Exception.Message
 $manifest | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
 throw
}
