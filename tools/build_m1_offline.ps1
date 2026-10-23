param([string]$PackageDirectory)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (!$PackageDirectory) {$PackageDirectory=Join-Path $root ('artifacts\m1-offline-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))}
$target=[IO.Path]::GetFullPath($PackageDirectory)
if (!$target.StartsWith($root+'\',[StringComparison]::OrdinalIgnoreCase)) {throw 'Output must be within this project'}
if (Test-Path -LiteralPath $target) {throw 'Output already exists; choose a new package directory to preserve it'}
$cursor=Split-Path -Parent $target
while ($cursor) {
 if (Test-Path -LiteralPath $cursor) {if ((Get-Item -LiteralPath $cursor).Attributes -band [IO.FileAttributes]::ReparsePoint) {throw 'Reparse output ancestor not allowed'}}
 $cursor=Split-Path -Parent $cursor
}
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash=(Get-FileHash -LiteralPath $compiler -Algorithm SHA256).Hash.ToLowerInvariant()
if ($compilerHash -ne '46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00') {throw 'Compiler fingerprint changed; review before building'}
$relativeSources=@(
 'src/resource_codec/Lz4Block.cs','src/resource_codec/ResourceContainers.cs','src/quest_codec/QuestDefinition.cs',
 'src/condition_logic/ConditionEvaluator.cs','src/task_logic/TaskLogic.cs',
 'src/token_actions/TokenMutations.cs','src/token_actions/TaskStateMutations.cs','src/task_runtime/TaskLifecycle.cs',
 'src/task_auto/OfflineEvents.cs','src/task_auto/AutomaticTasks.cs','src/task_auto/IConditionRuntime.cs','src/task_auto/ICompletionRuntime.cs',
 'src/task_items/ItemCountQuery.cs','src/task_items/HasItemCondition.cs','src/task_items/HasItemDefinitionAdapter.cs',
 'src/task_items/InventoryTake.cs','src/task_items/InventoryAdd.cs','src/task_items/ExampleItemQuest.cs',
 'src/rewards/MoneyRewards.cs','src/rewards/RewardNames.cs','src/rewards/ExampleDeliveryQuest.cs','src/rewards/OwnDeliveryFixture.cs',
 'src/rewards/ExampleFullDelivery.cs','src/rewards/ExampleInventoryCycle.cs','src/rewards/OwnInventoryCycleFixture.cs',
 'src/offline_tool/CommandResult.cs','src/offline_tool/Program.cs','src/offline_tool/CodecCommands.cs','src/offline_tool/ReplayCommands.cs'
)
$sources=@($relativeSources | ForEach-Object {Join-Path $root $_})
$manifest=@(for($i=0;$i -lt $sources.Count;$i++) {
 $item=Get-Item -LiteralPath $sources[$i]
 if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {throw 'Reparse source not allowed'}
 [ordered]@{path=$relativeSources[$i];bytes=$item.Length;sha256=(Get-FileHash -LiteralPath $item.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}
})
New-Item -ItemType Directory -Path $target | Out-Null
$exe=Join-Path $target 'gd-offline.exe'
$arguments=@('/nologo','/noconfig','/target:exe','/main:GrimDynasty.Offline.Program','/checked+','/optimize+','/debug-',
 '/codepage:65001','/reference:System.dll','/reference:System.Core.dll','/reference:System.Web.Extensions.dll',"/out:$exe")+$sources
$compileOutput=@(& $compiler @arguments 2>&1)
$compileExit=$LASTEXITCODE
[IO.File]::WriteAllLines((Join-Path $target 'compile.log'),[string[]]$compileOutput,[Text.UTF8Encoding]::new($false))
if ($compileExit -ne 0) {throw "M1 compile failed ($compileExit); see $target\compile.log"}
$sampleTarget=Join-Path $target 'samples'
New-Item -ItemType Directory -Path $sampleTarget | Out-Null
foreach($name in @('m1-records.arz','m1-ration-cycle.qst','m1-quests.arc','expected.json','inventory-roundtrip.json','unsupported-prefix.json')) {
 Copy-Item -LiteralPath (Join-Path $root ('src/offline_tool/samples/'+$name)) -Destination (Join-Path $sampleTarget $name)
}
Copy-Item -LiteralPath (Join-Path $root 'src/offline_tool/README.md') -Destination (Join-Path $target 'README.md')
$build=[ordered]@{schema='grim-dynasty-m1-build-v1';built_utc=[DateTime]::UtcNow.ToString('o');compiler=$compiler;compiler_sha256=$compilerHash;
 sources=$manifest;source_count=$manifest.Count;exe_sha256=(Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant();
 scope='Fresh own offline CLI source build. No legacy suite, UCRT provider, game DLL or native construction model compiled/run.';
 reproducibility='Commands and samples reproducible; legacy compiler timestamps mean byte-identical binaries are not claimed.'}
$build | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $target 'build-manifest.json') -Encoding UTF8
& (Join-Path $PSScriptRoot 'test_m1_offline.ps1') -PackageDirectory $target
if (!$?) {throw 'M1 acceptance did not complete'}
$files=@(Get-ChildItem -LiteralPath $target -File -Recurse | Sort-Object FullName | ForEach-Object {
 [ordered]@{path=$_.FullName.Substring($target.Length+1).Replace('\','/');bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}
})
$files | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $target 'SHA256.json') -Encoding UTF8
Write-Output "M1_PACKAGE=$target"
