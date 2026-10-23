param([string]$OutputDirectory=(Join-Path $PSScriptRoot '..\..\.tooling\token-actions-build'))
$ErrorActionPreference='Stop'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))+[IO.Path]::DirectorySeparatorChar
$target=[IO.Path]::GetFullPath($OutputDirectory)
if (!$target.StartsWith($root,[StringComparison]::OrdinalIgnoreCase)) {throw 'Project output required'}
New-Item -ItemType Directory -Path $target -Force | Out-Null
foreach($name in @('GrimDynasty.Conditions.dll','GrimDynasty.TaskLogic.dll','GrimDynasty.Resources.dll','GrimDynasty.Quests.dll')) {Copy-Item -LiteralPath (Join-Path $PSScriptRoot "..\..\.tooling\task-logic-build\$name") -Destination $target -Force}
& $compiler /nologo /target:library /checked+ /optimize+ "/reference:$target\GrimDynasty.TaskLogic.dll" "/reference:$target\GrimDynasty.Conditions.dll" "/out:$target\GrimDynasty.TokenActions.dll" "$PSScriptRoot\TokenMutations.cs" "$PSScriptRoot\TaskStateMutations.cs" "$PSScriptRoot\RewardClassifier.cs" "$PSScriptRoot\EventRewardProbe.cs"
if ($LASTEXITCODE -ne 0) {throw 'Token module compile failed'}
& $compiler /nologo /target:exe /checked+ /optimize+ "/reference:$target\GrimDynasty.Conditions.dll" "/reference:$target\GrimDynasty.TaskLogic.dll" "/reference:$target\GrimDynasty.TokenActions.dll" "/out:$target\TokenMutationTests.exe" "$PSScriptRoot\TokenMutationTests.cs"
if ($LASTEXITCODE -ne 0) {throw 'Token tests compile failed'}
& "$target\TokenMutationTests.exe"
if ($LASTEXITCODE -ne 0) {throw 'Token assertions failed'}
& $compiler /nologo /target:exe /checked+ /optimize+ /reference:System.Web.Extensions.dll "/reference:$target\GrimDynasty.Conditions.dll" "/reference:$target\GrimDynasty.TaskLogic.dll" "/reference:$target\GrimDynasty.TokenActions.dll" "/reference:$target\GrimDynasty.Resources.dll" "/reference:$target\GrimDynasty.Quests.dll" "/out:$target\TokenActionCatalogProbe.exe" "$PSScriptRoot\TokenActionCatalogProbe.cs"
if ($LASTEXITCODE -ne 0) {throw 'Token catalog compile failed'}

& $compiler /nologo /target:exe /checked+ /optimize+ "/reference:$target\GrimDynasty.Conditions.dll" "/reference:$target\GrimDynasty.TokenActions.dll" "/out:$target\RewardClassifierTests.exe" "$PSScriptRoot\RewardClassifierTests.cs"
if ($LASTEXITCODE -ne 0) {throw 'Reward tests compile failed'}
& "$target\RewardClassifierTests.exe"
if ($LASTEXITCODE -ne 0) {throw 'Reward assertions failed'}
& $compiler /nologo /target:exe /checked+ /optimize+ /reference:System.Web.Extensions.dll "/reference:$target\GrimDynasty.Conditions.dll" "/reference:$target\GrimDynasty.TokenActions.dll" "/reference:$target\GrimDynasty.Resources.dll" "/reference:$target\GrimDynasty.Quests.dll" "/out:$target\RewardCatalogProbe.exe" "$PSScriptRoot\RewardCatalogProbe.cs"
if ($LASTEXITCODE -ne 0) {throw 'Reward catalog compile failed'}

& $compiler /nologo /target:exe /checked+ /optimize+ "/reference:$target\GrimDynasty.Conditions.dll" "/reference:$target\GrimDynasty.TaskLogic.dll" "/reference:$target\GrimDynasty.TokenActions.dll" "/out:$target\EventRewardProbeTests.exe" "$PSScriptRoot\EventRewardProbeTests.cs"
if ($LASTEXITCODE -ne 0) {throw 'Event reward tests compile failed'}
& "$target\EventRewardProbeTests.exe"
if ($LASTEXITCODE -ne 0) {throw 'Event reward assertions failed'}
& $compiler /nologo /target:exe /checked+ /optimize+ /reference:System.Web.Extensions.dll "/reference:$target\GrimDynasty.Conditions.dll" "/reference:$target\GrimDynasty.TaskLogic.dll" "/reference:$target\GrimDynasty.TokenActions.dll" "/reference:$target\GrimDynasty.Resources.dll" "/reference:$target\GrimDynasty.Quests.dll" "/out:$target\EventRewardCatalogProbe.exe" "$PSScriptRoot\EventRewardCatalogProbe.cs"
if ($LASTEXITCODE -ne 0) {throw 'Event reward catalog compile failed'}
