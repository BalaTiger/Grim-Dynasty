param([string]$OutputDirectory=(Join-Path $PSScriptRoot '..\..\.tooling\condition-evaluator-build'))
$ErrorActionPreference='Stop'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))+[IO.Path]::DirectorySeparatorChar
$target=[IO.Path]::GetFullPath($OutputDirectory)
if (!$target.StartsWith($root,[StringComparison]::OrdinalIgnoreCase)) {throw 'Project output required'}
New-Item -ItemType Directory -Path $target -Force | Out-Null
& $compiler /nologo /target:library /checked+ /optimize+ "/out:$target\GrimDynasty.Conditions.dll" "$PSScriptRoot\ConditionEvaluator.cs" "$PSScriptRoot\ConditionDefinitionAdapter.cs"
if ($LASTEXITCODE -ne 0) {throw 'Condition module compile failed'}
& $compiler /nologo /target:exe /checked+ /optimize+ "/reference:$target\GrimDynasty.Conditions.dll" "/out:$target\ConditionEvaluatorTests.exe" "$PSScriptRoot\ConditionEvaluatorTests.cs"
if ($LASTEXITCODE -ne 0) {throw 'Condition tests compile failed'}
foreach($name in @('GrimDynasty.Resources.dll','GrimDynasty.Quests.dll')) {Copy-Item -LiteralPath (Join-Path $PSScriptRoot "..\..\.tooling\quest-definition-build\$name") -Destination "$target\$name" -Force}
& $compiler /nologo /target:exe /checked+ /optimize+ /reference:System.Web.Extensions.dll "/reference:$target\GrimDynasty.Conditions.dll" "/reference:$target\GrimDynasty.Resources.dll" "/reference:$target\GrimDynasty.Quests.dll" "/out:$target\ConditionCatalogProbe.exe" "$PSScriptRoot\ConditionCatalogProbe.cs"
if ($LASTEXITCODE -ne 0) {throw 'Catalog probe compile failed'}
& "$target\ConditionEvaluatorTests.exe"
if ($LASTEXITCODE -ne 0) {throw 'Condition self-tests failed'}
