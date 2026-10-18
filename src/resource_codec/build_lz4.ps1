param([string]$OutputDirectory='D:\MyGitRepo\Grim-Dynasty\.tooling\resource-reader-build')
$ErrorActionPreference='Stop'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) {throw "Existing .NET Framework C# compiler missing: $compiler"}
$root=[IO.Path]::GetFullPath('D:\MyGitRepo\Grim-Dynasty')+[IO.Path]::DirectorySeparatorChar
$target=[IO.Path]::GetFullPath($OutputDirectory)
if (!$target.StartsWith($root,[StringComparison]::OrdinalIgnoreCase)) {throw 'Build output must remain inside project'}
New-Item -ItemType Directory -Path $target -Force | Out-Null
& $compiler /nologo /target:exe /optimize+ /checked+ "/out:$target\Lz4Probe.exe" "$PSScriptRoot\Lz4Block.cs"
if ($LASTEXITCODE -ne 0) {throw "Compiler failed: $LASTEXITCODE"}
& "$target\Lz4Probe.exe" --self-test
if ($LASTEXITCODE -ne 0) {throw 'Compiled self-tests failed'}
