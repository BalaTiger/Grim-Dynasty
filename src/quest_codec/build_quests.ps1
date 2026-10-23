param([string]$OutputDirectory=(Join-Path $PSScriptRoot '..\..\.tooling\quest-definition-build'))
$ErrorActionPreference='Stop'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))+[IO.Path]::DirectorySeparatorChar
$target=[IO.Path]::GetFullPath($OutputDirectory)
if (!$target.StartsWith($root,[StringComparison]::OrdinalIgnoreCase)) {throw 'Project output required'}
New-Item -ItemType Directory -Path $target -Force | Out-Null
$resources=(Join-Path $PSScriptRoot '..\..\.tooling\resource-containers-build\GrimDynasty.Resources.dll')
Copy-Item -LiteralPath $resources -Destination "$target\GrimDynasty.Resources.dll" -Force
& $compiler /nologo /target:library /checked+ /optimize+ "/reference:$target\GrimDynasty.Resources.dll" "/out:$target\GrimDynasty.Quests.dll" "$PSScriptRoot\QuestDefinition.cs"
if ($LASTEXITCODE -ne 0) {throw 'Quest module compilation failed'}
& $compiler /nologo /target:exe /checked+ /optimize+ /reference:System.Web.Extensions.dll "/reference:$target\GrimDynasty.Resources.dll" "/reference:$target\GrimDynasty.Quests.dll" "/out:$target\QuestProbe.exe" "$PSScriptRoot\QuestProbe.cs"
if ($LASTEXITCODE -ne 0) {throw 'Quest CLI compilation failed'}
Write-Output "Built independent Quests module and CLI at $target"
