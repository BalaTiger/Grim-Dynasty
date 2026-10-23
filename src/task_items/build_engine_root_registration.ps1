# Execution requires a separate reviewed approval. This file does not alter policy.
# A failed attempt keeps its directory. No automatic cleanup, overwrite or retry.
[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
if ($args.Count -ne 0 -or $PSBoundParameters.Count -ne 0) {throw 'This fixed build accepts no arguments'}

$expectedProjectRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
if (![String]::Equals($projectRoot,$expectedProjectRoot,[StringComparison]::OrdinalIgnoreCase)) {
 throw 'This build is restricted to the fixed Grim-Dynasty workspace'
}
$expectedScript=Join-Path $projectRoot 'src\task_items\build_engine_root_registration.ps1'
if (![String]::Equals([IO.Path]::GetFullPath($PSCommandPath),$expectedScript,[StringComparison]::OrdinalIgnoreCase)) {
 throw 'Unexpected build script path'
}
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$source=Join-Path $projectRoot 'src\task_items\EngineRootRegistration.cs'
$testSource=Join-Path $projectRoot 'src\task_items\EngineRootRegistrationTests.cs'
$outputParent=Join-Path $projectRoot '.tooling'
$outputLeaf='engine-root-model-build-20261021'
$target=[IO.Path]::GetFullPath((Join-Path $outputParent $outputLeaf))
$rootPrefix=$projectRoot+[IO.Path]::DirectorySeparatorChar
if (!$target.StartsWith($rootPrefix,[StringComparison]::OrdinalIgnoreCase)) {throw 'Project output required'}

function Assert-NormalExistingPath([string]$LiteralPath,[bool]$DirectoryRequired) {
 $cursor=[IO.Path]::GetFullPath($LiteralPath)
 $first=$true
 while ($true) {
  $item=Get-Item -LiteralPath $cursor -Force -ErrorAction Stop
  if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
   throw "Reparse points are outside this build scope: $cursor"
  }
  if ($first) {
   if ($DirectoryRequired -ne [bool]$item.PSIsContainer) {throw "Unexpected path type: $cursor"}
  } elseif (!$item.PSIsContainer) {throw "Ancestor is not a directory: $cursor"}
  $parent=[IO.Directory]::GetParent($cursor)
  if ($null -eq $parent) {break}
  $cursor=$parent.FullName
  $first=$false
 }
}

# Check each exact input and all ancestors, including the compiler's ancestors.
# The compiler's implicit mscorlib is sufficient; csc.rsp is disabled with /noconfig.
Assert-NormalExistingPath $expectedScript $false
Assert-NormalExistingPath $compiler $false
Assert-NormalExistingPath $source $false
Assert-NormalExistingPath $testSource $false
Assert-NormalExistingPath $outputParent $true
# A fixed-name directory lookup also rejects an existing dangling link by name.
$existing=@(Get-ChildItem -LiteralPath $outputParent -Filter $outputLeaf -Force -ErrorAction Stop |
 Where-Object {[String]::Equals($_.Name,$outputLeaf,[StringComparison]::OrdinalIgnoreCase)})
if ($existing.Count -ne 0) {
 throw 'Fixed output already exists. Preserve the failed attempt and obtain separate authorization to handle that exact directory before retrying; this script never deletes or overwrites it.'
}
$created=New-Item -ItemType Directory -Path $target -ErrorAction Stop
Assert-NormalExistingPath $target $true
if (![String]::Equals([IO.Path]::GetFullPath($created.FullName),$target,[StringComparison]::OrdinalIgnoreCase)) {
 throw 'Created output path differs from the fixed target'
}
if (@(Get-ChildItem -LiteralPath $target -Force -ErrorAction Stop).Count -ne 0) {
 throw 'New output directory is not empty'
}
$library=Join-Path $target 'GrimDynasty.EngineRootRegistration.dll'
$tests=Join-Path $target 'EngineRootRegistrationTests.exe'

# Only the two new source files are compiled. No project script or old suite runs.
& $compiler /noconfig /nologo /langversion:5 /target:library /checked+ /optimize+ /debug- "/out:$library" $source
$libraryExit=$LASTEXITCODE
if ($libraryExit -ne 0) {throw "Engine root module compilation failed with exit code $libraryExit"}
Assert-NormalExistingPath $library $false
& $compiler /noconfig /nologo /langversion:5 /target:exe /checked+ /optimize+ /debug- "/reference:$library" "/out:$tests" $testSource
$testCompileExit=$LASTEXITCODE
if ($testCompileExit -ne 0) {throw "Engine root tests compilation failed with exit code $testCompileExit"}
Assert-NormalExistingPath $tests $false
& $tests
$testExit=$LASTEXITCODE
if ($testExit -ne 0) {throw "Engine root tests failed with exit code $testExit"}
