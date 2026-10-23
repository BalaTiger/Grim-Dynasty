# New Actor synchronization initialization projection; separate execution approval.
# Uses the installed compiler. No download, policy/ACL/environment change or cleanup.
# Failed attempts retain the fixed output directory and are never overwritten here.
[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
if ($args.Count -ne 0 -or $PSBoundParameters.Count -ne 0) {throw 'This fixed build accepts no arguments'}
if ($PSVersionTable.PSVersion.Major -ne 5 -or $PSVersionTable.PSVersion.Minor -ne 1 -or $PSVersionTable.PSEdition -ne 'Desktop') {
 throw 'This reviewed build requires Windows PowerShell 5.1 Desktop'
}
$expectedProjectRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
if (![String]::Equals($projectRoot,$expectedProjectRoot,[StringComparison]::OrdinalIgnoreCase)) {
 throw 'This build is restricted to the fixed Grim-Dynasty workspace'
}
$expectedScript=Join-Path $projectRoot 'src\task_items\build_actor_synchronization_initialization.ps1'
if (![String]::Equals([IO.Path]::GetFullPath($PSCommandPath),$expectedScript,[StringComparison]::OrdinalIgnoreCase)) {
 throw 'Unexpected build script path'
}
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$librarySource=Join-Path $projectRoot 'src\task_items\ActorSynchronizationInitialization.cs'
$testSource=Join-Path $projectRoot 'src\task_items\ActorSynchronizationInitializationTests.cs'
$outputParent=Join-Path $projectRoot '.tooling'
$outputLeaf='actor-synchronization-initialization-build-20261021'
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

# Validate exact inputs and every ancestor before creating output.
# /noconfig disables csc.rsp; the existing compiler's implicit mscorlib is sufficient.
Assert-NormalExistingPath $expectedScript $false
Assert-NormalExistingPath $compiler $false
Assert-NormalExistingPath $librarySource $false
Assert-NormalExistingPath $testSource $false
Assert-NormalExistingPath $outputParent $true
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
$library=Join-Path $target 'GrimDynasty.ActorSynchronizationInitialization.dll'
$tests=Join-Path $target 'ActorSynchronizationInitializationTests.exe'

# Exactly one library source and one new test source; no old suites/scripts.
& $compiler /noconfig /nologo /langversion:5 /target:library /checked+ /optimize+ /debug- "/out:$library" $librarySource
$libraryExit=$LASTEXITCODE
if ($libraryExit -ne 0) {throw "Actor synchronization initialization module compilation failed with exit code $libraryExit"}
Assert-NormalExistingPath $library $false
& $compiler /noconfig /nologo /langversion:5 /target:exe /checked+ /optimize+ /debug- "/reference:$library" "/out:$tests" $testSource
$testCompileExit=$LASTEXITCODE
if ($testCompileExit -ne 0) {throw "Actor synchronization initialization tests compilation failed with exit code $testCompileExit"}
Assert-NormalExistingPath $tests $false
& $tests
$testExit=$LASTEXITCODE
if ($testExit -ne 0) {throw "Actor synchronization initialization tests failed with exit code $testExit"}
