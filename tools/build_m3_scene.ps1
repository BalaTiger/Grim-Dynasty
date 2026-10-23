param([string]$PackageDirectory)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if(!$PackageDirectory){$PackageDirectory=Join-Path $root ('artifacts\m3-scene-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))}
$target=[IO.Path]::GetFullPath($PackageDirectory)
if(!$target.StartsWith($root+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Project output required'}
if(Test-Path -LiteralPath $target){throw 'Output already exists; preserve it'}
$cursor=Split-Path -Parent $target
while($cursor){if(Test-Path -LiteralPath $cursor){if((Get-Item -LiteralPath $cursor).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Reparse output ancestor'}};$cursor=Split-Path -Parent $cursor}
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash=(Get-FileHash -LiteralPath $compiler -Algorithm SHA256).Hash.ToLowerInvariant()
if($compilerHash -ne '46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'){throw 'Compiler fingerprint changed'}
$m2Path=Join-Path $root 'artifacts/m2-session-20261022-01/build-manifest.json'
if((Get-FileHash -LiteralPath $m2Path -Algorithm SHA256).Hash -ne 'ADE46A6314AD0E4E8DE5D8327E51E77401C88FF35151ED9300291808225A32B6'){throw 'M2 manifest changed'}
$m2=Get-Content -LiteralPath $m2Path -Raw|ConvertFrom-Json
foreach($source in $m2.sources){if((Get-FileHash -LiteralPath (Join-Path $root $source.path) -Algorithm SHA256).Hash -ne $source.sha256){throw ('Accepted M2 source changed: '+$source.path)}}
$relative=@($m2.sources|ForEach-Object{$_.path})+@('src/scene_runtime/SceneWorld.cs','src/scene_runtime/SceneRenderer.cs','src/scene_runtime/SceneState.cs','src/scene_runtime/SceneWindow.cs','src/scene_runtime/SceneProgram.cs','src/scene_runtime/SceneTests.cs',
 'src/scene_runtime/SceneAgents.cs','src/scene_runtime/SceneAgentsTests.cs','src/scene_runtime/SceneAgentPersistence.cs','src/scene_runtime/ScenePatrolTests.cs',
 'src/combat_runtime/NativeDamageSettlement.cs','src/combat_runtime/NativeDamageSettlementTests.cs')
$sources=@($relative|ForEach-Object{Join-Path $root $_})
foreach($path in $sources){if((Get-Item -LiteralPath $path).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Reparse source'}}
New-Item -ItemType Directory -Path $target|Out-Null
$common=@('/nologo','/noconfig','/main:GrimDynasty.Scene.SceneProgram','/checked+','/optimize+','/debug-','/codepage:65001',
 '/reference:System.dll','/reference:System.Core.dll','/reference:System.Web.Extensions.dll','/reference:System.Drawing.dll','/reference:System.Windows.Forms.dll')
$exe=Join-Path $target 'gd-scene.exe';$testExe=Join-Path $target 'SceneTests.exe'
$log=@(& $compiler @common /target:winexe "/out:$exe" @sources 2>&1);$code=$LASTEXITCODE
[IO.File]::WriteAllLines((Join-Path $target 'compile.log'),[string[]]$log,[Text.UTF8Encoding]::new($false));if($code -ne 0){throw 'Scene compilation failed'}
$log=@(& $compiler @common /target:exe "/out:$testExe" @sources 2>&1);$code=$LASTEXITCODE
[IO.File]::WriteAllLines((Join-Path $target 'compile-tests.log'),[string[]]$log,[Text.UTF8Encoding]::new($false));if($code -ne 0){throw 'Scene test compilation failed'}
$modelRoot=Join-Path $target 'validation/models'
$log=@(& $testExe --test $modelRoot 2>&1);$code=$LASTEXITCODE
[IO.File]::WriteAllLines((Join-Path $target 'scene-tests.log'),[string[]]$log,[Text.UTF8Encoding]::new($false));if($code -ne 0){throw 'Scene checks failed'}
$text=$log -join "`n";if($text -notmatch 'M3_SCENE_CHECKS=(\d+)'){throw 'Scene assertion count missing'};$checks=[int]$Matches[1]
$windowRoot=Join-Path $target 'validation/window';New-Item -ItemType Directory -Path $windowRoot|Out-Null
# The user requested an actual visible, controllable own scene. Launch only this
# freshly built instance, exercise its key handler/timer, then close automatically.
$process=Start-Process -FilePath $exe -ArgumentList @('--smoke',('"'+$windowRoot+'"')) -WorkingDirectory $target -WindowStyle Normal -PassThru
if(!$process.WaitForExit(15000)){[void]$process.CloseMainWindow();if(!$process.WaitForExit(2000)){$process.Kill();$process.WaitForExit()};throw 'Owned scene window smoke timed out and was stopped'}
if($process.ExitCode -ne 0){throw 'Scene window returned failure'}
$smoke=Get-Content -LiteralPath (Join-Path $windowRoot 'window-smoke.json') -Raw|ConvertFrom-Json
$smokeX=[BitConverter]::ToSingle([BitConverter]::GetBytes([uint32]$smoke.x_bits),0)
$smokeY=[BitConverter]::ToSingle([BitConverter]::GetBytes([uint32]$smoke.y_bits),0)
if($smoke.frames -lt 28 -or $smokeX -le 450 -or $smokeY -ne 1150){throw 'Visible window did not move through actual input handler/timer'}
if((Get-Item -LiteralPath (Join-Path $windowRoot 'window.png')).Length -lt 10000){throw 'Window render missing'}
Copy-Item -LiteralPath (Join-Path $root 'src/scene_runtime/README.md') -Destination (Join-Path $target 'README.md')
$report=[ordered]@{schema='grim-dynasty-m3-scene-checkpoint-v1';status='Passed';completed_utc=[DateTime]::UtcNow.ToString('o');scene_checks=$checks;
 window_smoke=$smoke;window_exit=$process.ExitCode;scope='M3 first visible scene checkpoint only: movement/collision/NPC M2 tasks/own combined scene save. Combat, AI, trade and equipment not implemented or claimed.'}
$report|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $target 'acceptance.json') -Encoding UTF8
$manifest=[ordered]@{built_utc=[DateTime]::UtcNow.ToString('o');compiler_sha256=$compilerHash;exe_sha256=(Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant();
 sources=@($sources|ForEach-Object{[ordered]@{path=$_.Substring($root.Length+1).Replace('\','/');sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()}})}
$manifest|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $target 'build-manifest.json') -Encoding UTF8
$files=@(Get-ChildItem -LiteralPath $target -File -Recurse|Sort-Object FullName|ForEach-Object{[ordered]@{path=$_.FullName.Substring($target.Length+1).Replace('\','/');bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}})
$files|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $target 'SHA256.json') -Encoding UTF8
Write-Output ("M3_SCENE_CHECKPOINT checks={0} window_input=Passed package={1}" -f $checks,$target)
