param([string]$OutputDirectory='D:\MyGitRepo\Grim-Dynasty\.tooling\task-kills-build')
$ErrorActionPreference='Stop'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$root=[IO.Path]::GetFullPath('D:\MyGitRepo\Grim-Dynasty')+[IO.Path]::DirectorySeparatorChar
$target=[IO.Path]::GetFullPath($OutputDirectory)
if (!$target.StartsWith($root,[StringComparison]::OrdinalIgnoreCase)) {throw 'Project output required'}
New-Item -ItemType Directory -Path $target -Force | Out-Null
foreach($name in @('GrimDynasty.Conditions.dll','GrimDynasty.TaskLogic.dll','GrimDynasty.TokenActions.dll','GrimDynasty.TaskRuntime.dll','GrimDynasty.TaskAuto.dll','GrimDynasty.Resources.dll','GrimDynasty.Quests.dll')) {Copy-Item -LiteralPath "D:\MyGitRepo\Grim-Dynasty\.tooling\task-auto-build\$name" -Destination $target -Force}
$refs=@("/reference:$target\GrimDynasty.Conditions.dll","/reference:$target\GrimDynasty.TaskLogic.dll","/reference:$target\GrimDynasty.TokenActions.dll","/reference:$target\GrimDynasty.TaskRuntime.dll","/reference:$target\GrimDynasty.TaskAuto.dll")
& $compiler /nologo /target:library /checked+ /optimize+ @refs "/out:$target\GrimDynasty.TaskKills.dll" "$PSScriptRoot\KillCondition.cs" "$PSScriptRoot\KillDefinitionAdapter.cs"
if ($LASTEXITCODE -ne 0) {throw 'Kill module compile failed'}
$refs+="/reference:$target\GrimDynasty.TaskKills.dll"
& $compiler /nologo /target:exe /checked+ /optimize+ @refs "/out:$target\KillConditionTests.exe" "$PSScriptRoot\KillConditionTests.cs"
if ($LASTEXITCODE -ne 0) {throw 'Kill tests compile failed'}
& "$target\KillConditionTests.exe"
if ($LASTEXITCODE -ne 0) {throw 'Kill tests failed'}
& $compiler /nologo /target:exe /checked+ /optimize+ @refs "/out:$target\KillAutoTests.exe" "$PSScriptRoot\KillAutoTests.cs" "$PSScriptRoot\ExampleKillQuest.cs"
if ($LASTEXITCODE -ne 0) {throw 'Kill auto tests compile failed'}
& "$target\KillAutoTests.exe"
if ($LASTEXITCODE -ne 0) {throw 'Kill auto tests failed'}
& $compiler /nologo /target:exe /checked+ /optimize+ /reference:System.Web.Extensions.dll @refs "/out:$target\KillQuestDemo.exe" "$PSScriptRoot\KillQuestDemo.cs" "$PSScriptRoot\ExampleKillQuest.cs"
if ($LASTEXITCODE -ne 0) {throw 'Kill demo compile failed'}
& "$target\KillQuestDemo.exe"
if ($LASTEXITCODE -ne 0) {throw 'Kill demo failed'}
& $compiler /nologo /target:exe /checked+ /optimize+ /reference:System.Web.Extensions.dll @refs "/reference:$target\GrimDynasty.Resources.dll" "/reference:$target\GrimDynasty.Quests.dll" "/out:$target\KillCatalogProbe.exe" "$PSScriptRoot\KillCatalogProbe.cs"
if ($LASTEXITCODE -ne 0) {throw 'Kill catalog compile failed'}

& $compiler /nologo /target:exe /checked+ /optimize+ /reference:System.Web.Extensions.dll @refs "/reference:$target\GrimDynasty.Resources.dll" "/reference:$target\GrimDynasty.Quests.dll" "/out:$target\KillAutoCatalogProbe.exe" "$PSScriptRoot\KillAutoCatalogProbe.cs"
if ($LASTEXITCODE -ne 0) {throw 'Kill auto catalog compile failed'}
