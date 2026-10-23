param([string]$OutputDirectory=(Join-Path $PSScriptRoot '..\..\.tooling\resource-containers-build'),[string]$Lz4Source=(Join-Path $PSScriptRoot '..\..\src\resource_codec\Lz4Block.cs'),[switch]$IncludeTests)
$ErrorActionPreference='Stop'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))+[IO.Path]::DirectorySeparatorChar
$target=[IO.Path]::GetFullPath($OutputDirectory)
if (!$target.StartsWith($root,[StringComparison]::OrdinalIgnoreCase)) {throw 'Output must remain inside project'}
New-Item -ItemType Directory -Path $target -Force | Out-Null
& $compiler /nologo /target:library /optimize+ /checked+ "/out:$target\GrimDynasty.Resources.dll" $Lz4Source "$PSScriptRoot\ResourceContainers.cs"
if ($LASTEXITCODE -ne 0) {throw 'Library compilation failed'}
& $compiler /nologo /target:exe /optimize+ /checked+ /reference:System.Web.Extensions.dll "/reference:$target\GrimDynasty.Resources.dll" "/out:$target\ResourceProbe.exe" "$PSScriptRoot\ResourceProbe.cs"
if ($LASTEXITCODE -ne 0) {throw 'CLI compilation failed'}
if ($IncludeTests) {
 & $compiler /nologo /target:exe /optimize+ /checked+ /reference:System.Web.Extensions.dll "/reference:$target\GrimDynasty.Resources.dll" "/out:$target\ResourceFixtureProbe.exe" "$PSScriptRoot\ResourceFixtureProbe.cs"
 if ($LASTEXITCODE -ne 0) {throw 'Fixture runner compilation failed'}
}
Write-Output "Built $target\GrimDynasty.Resources.dll and ResourceProbe.exe"
