param([string]$OutputDirectory=(Join-Path $PSScriptRoot '..\..\.tooling\class-registration-helpers-build'))
$ErrorActionPreference='Stop'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))+[IO.Path]::DirectorySeparatorChar
$target=[IO.Path]::GetFullPath($OutputDirectory)
if (!$target.StartsWith($root,[StringComparison]::OrdinalIgnoreCase)) {throw 'Project output required'}
New-Item -ItemType Directory -Path $target -Force | Out-Null
& $compiler /nologo /target:library /checked+ /optimize+ "/out:$target\GrimDynasty.ClassRegistration.dll" "$PSScriptRoot\ClassRegistrationInitialization.cs"
if ($LASTEXITCODE -ne 0) {throw 'Class registration module compile failed'}
& $compiler /nologo /target:exe /checked+ /optimize+ "/reference:$target\GrimDynasty.ClassRegistration.dll" "/out:$target\ClassRegistrationInitializationTests.exe" "$PSScriptRoot\ClassRegistrationInitializationTests.cs"
if ($LASTEXITCODE -ne 0) {throw 'Class registration tests compile failed'}
& "$target\ClassRegistrationInitializationTests.exe"
if ($LASTEXITCODE -ne 0) {throw 'Class registration tests failed'}
