$ErrorActionPreference='Stop'
$project='D:\MyGitRepo\Grim-Dynasty\analysis\selected-kill-project'
$output='D:\MyGitRepo\Grim-Dynasty\docs\task-kills\ghidra-selected\subscriber-constructor-routes.json'
if(Test-Path -LiteralPath $output){throw 'Evidence exists; preserve it'}
$paths=Get-Content -LiteralPath 'D:\MyGitRepo\Grim-Dynasty\.tooling\installed-paths.json' -Raw | ConvertFrom-Json
$env:JAVA_HOME=$paths.jdk
$env:GHIDRA_HEADLESS_MAXMEM='512M'
$env:APPDATA='D:\MyGitRepo\Grim-Dynasty\analysis\selected-kill-runtime\Roaming'
$env:LOCALAPPDATA='D:\MyGitRepo\Grim-Dynasty\analysis\selected-kill-runtime\Local'
New-Item -ItemType Directory -Path $env:APPDATA,$env:LOCALAPPDATA -Force | Out-Null
$entries=@('0x180017050','0x1803cc3f0','0x180372b40','0x1803fd430','0x1802c07a0','0x1803c5b60')
$launcher=Join-Path $paths.ghidra 'support\analyzeHeadless.bat'
& $launcher $project 'TaskKills' -process 'Game.dll' -noanalysis -scriptPath $PSScriptRoot -preScript SeedObservedFunctions.java @entries -postScript ExportObservedFunctions.java $output @entries -max-cpu 2 -log 'D:\MyGitRepo\Grim-Dynasty\docs\task-kills\ghidra-selected\subscriber-constructor-routes.log'
if($LASTEXITCODE -ne 0){throw 'Selected follow-up analysis failed'}
