param([string]$PackageDirectory)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (!$PackageDirectory) {$PackageDirectory=Join-Path $root ('artifacts\m2-session-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))}
$target=[IO.Path]::GetFullPath($PackageDirectory)
if (!$target.StartsWith($root+'\',[StringComparison]::OrdinalIgnoreCase)) {throw 'Project output required'}
if (Test-Path -LiteralPath $target) {throw 'Preserve existing output; choose a new directory'}
$cursor=Split-Path -Parent $target
while($cursor){if(Test-Path -LiteralPath $cursor){if((Get-Item -LiteralPath $cursor).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Reparse output ancestor'}};$cursor=Split-Path -Parent $cursor}
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash=(Get-FileHash -LiteralPath $compiler -Algorithm SHA256).Hash.ToLowerInvariant()
if($compilerHash -ne '46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'){throw 'Compiler fingerprint changed'}
$m1ManifestPath=Join-Path $root 'artifacts/m1-offline-20261022-03/build-manifest.json'
if((Get-FileHash -LiteralPath $m1ManifestPath -Algorithm SHA256).Hash -ne '98EB9692A44BBB0E301942E693A74751810BDF5ECBD62DDE89A1DE41BD15F3A5'){throw 'M1 manifest changed'}
$m1=Get-Content -LiteralPath $m1ManifestPath -Raw | ConvertFrom-Json
foreach($source in $m1.sources){if((Get-FileHash -LiteralPath (Join-Path $root $source.path) -Algorithm SHA256).Hash -ne $source.sha256){throw ('M1 source changed: '+$source.path)}}
$relativeSources=@($m1.sources | ForEach-Object {$_.path})+@('src/session_runtime/StrictJson.cs','src/session_runtime/SessionState.cs','src/session_runtime/SessionStore.cs','src/session_runtime/SessionProgram.cs')
$sources=@($relativeSources | ForEach-Object {Join-Path $root $_})
foreach($path in $sources){if((Get-Item -LiteralPath $path).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Reparse input source'}}
New-Item -ItemType Directory -Path $target | Out-Null
$common=@('/nologo','/noconfig','/target:exe','/checked+','/optimize+','/debug-','/codepage:65001','/reference:System.dll','/reference:System.Core.dll','/reference:System.Web.Extensions.dll')
$exe=Join-Path $target 'gd-session.exe'
$log=@(& $compiler @common /main:GrimDynasty.Session.SessionProgram "/out:$exe" @sources 2>&1);$code=$LASTEXITCODE
[IO.File]::WriteAllLines((Join-Path $target 'compile.log'),[string[]]$log,[Text.UTF8Encoding]::new($false))
if($code -ne 0){throw 'M2 compile failed; see compile.log'}
$testSources=$sources+@((Join-Path $root 'src/session_runtime/SessionCoreTests.cs'),(Join-Path $root 'src/session_runtime/CoreTestProgram.cs'))
$testExe=Join-Path $target 'SessionCoreTests.exe'
$log=@(& $compiler @common /main:GrimDynasty.Session.CoreTestProgram "/out:$testExe" @testSources 2>&1);$code=$LASTEXITCODE
[IO.File]::WriteAllLines((Join-Path $target 'compile-tests.log'),[string[]]$log,[Text.UTF8Encoding]::new($false))
if($code -ne 0){throw 'M2 test compile failed; see compile-tests.log'}
$testRoot=Join-Path $target 'validation/core/runtime-data/saves'
$log=@(& $testExe $testRoot 2>&1);$code=$LASTEXITCODE
[IO.File]::WriteAllLines((Join-Path $target 'core-tests.log'),[string[]]$log,[Text.UTF8Encoding]::new($false))
if($code -ne 0){throw 'M2 core tests failed; see core-tests.log'}
Copy-Item -LiteralPath (Join-Path $root 'src/session_runtime/README.md') -Destination (Join-Path $target 'README.md')
$manifest=[ordered]@{schema='grim-dynasty-m2-build-v1';built_utc=[DateTime]::UtcNow.ToString('o');compiler=$compiler;compiler_sha256=$compilerHash;
 sources=@($testSources|ForEach-Object{[ordered]@{path=$_.Substring($root.Length+1).Replace('\','/');sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()}});
 exe_sha256=(Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant();scope='M2 own session/store and fixed tests; M1 sources preserved; no original game or saves'}
$manifest|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $target 'build-manifest.json') -Encoding UTF8
& (Join-Path $PSScriptRoot 'test_m2_session.ps1') -PackageDirectory $target
if(!$?){throw 'M2 CLI acceptance did not complete'}
$files=@(Get-ChildItem -LiteralPath $target -File -Recurse|Sort-Object FullName|ForEach-Object{[ordered]@{path=$_.FullName.Substring($target.Length+1).Replace('\','/');bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}})
$files|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $target 'SHA256.json') -Encoding UTF8
Write-Output "M2_PACKAGE=$target"
