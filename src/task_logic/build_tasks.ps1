param([string]$OutputDirectory=(Join-Path $PSScriptRoot '..\..\.tooling\task-logic-build'))
$ErrorActionPreference='Stop'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))+[IO.Path]::DirectorySeparatorChar
$target=[IO.Path]::GetFullPath($OutputDirectory)
if (!$target.StartsWith($root,[StringComparison]::OrdinalIgnoreCase)) {throw 'Project output required'}
New-Item -ItemType Directory -Path $target -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '..\..\.tooling\condition-evaluator-build\GrimDynasty.Conditions.dll') -Destination $target -Force
foreach($name in @('GrimDynasty.Resources.dll','GrimDynasty.Quests.dll')) {Copy-Item -LiteralPath (Join-Path $PSScriptRoot "..\..\.tooling\quest-definition-build\$name") -Destination $target -Force}
& $compiler /nologo /target:library /checked+ /optimize+ "/reference:$target\GrimDynasty.Conditions.dll" "/out:$target\GrimDynasty.TaskLogic.dll" "$PSScriptRoot\TaskLogic.cs" "$PSScriptRoot\TaskDefinitionAdapter.cs"
if ($LASTEXITCODE -ne 0) {throw 'Task logic compile failed'}
& $compiler /nologo /target:exe /checked+ /optimize+ "/reference:$target\GrimDynasty.Conditions.dll" "/reference:$target\GrimDynasty.TaskLogic.dll" "/out:$target\TaskLogicTests.exe" "$PSScriptRoot\TaskLogicTests.cs"
if ($LASTEXITCODE -ne 0) {throw 'Task tests compile failed'}
& "$target\TaskLogicTests.exe"
if ($LASTEXITCODE -ne 0) {throw 'Task fixed assertions failed'}
& $compiler /nologo /target:exe /checked+ /optimize+ /reference:System.Web.Extensions.dll "/reference:$target\GrimDynasty.Conditions.dll" "/reference:$target\GrimDynasty.TaskLogic.dll" "/reference:$target\GrimDynasty.Resources.dll" "/reference:$target\GrimDynasty.Quests.dll" "/out:$target\TaskCatalogProbe.exe" "$PSScriptRoot\TaskCatalogProbe.cs"
if ($LASTEXITCODE -ne 0) {throw 'Task catalog compile failed'}
