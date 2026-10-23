# New Actor engine context and the two affected regressions only.
# Execution requires separate approval. No download, policy/ACL/environment change or cleanup.
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
$expectedScript=Join-Path $projectRoot 'src\task_items\build_actor_engine_context.ps1'
if (![String]::Equals([IO.Path]::GetFullPath($PSCommandPath),$expectedScript,[StringComparison]::OrdinalIgnoreCase)) {
 throw 'Unexpected build script path'
}
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
# Pin reused from both prior approved Actor preflight records; never download a replacement.
$expectedCompilerSha256='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$librarySources=@(
 (Join-Path $projectRoot 'src\task_items\ActorConstructorPrefix.cs'),
 (Join-Path $projectRoot 'src\task_items\ActorConstructorArray.cs'),
 (Join-Path $projectRoot 'src\task_items\ActorConstructorContinuation.cs'),
 (Join-Path $projectRoot 'src\task_items\ActorConstructorCompletion.cs'),
 (Join-Path $projectRoot 'src\task_items\ActorMatrixInitialization.cs'),
 (Join-Path $projectRoot 'src\task_items\ActorEngineContext.cs')
)
$testSource=Join-Path $projectRoot 'src\task_items\ActorEngineContextTests.cs'
$completionSource=Join-Path $projectRoot 'src\task_items\ActorConstructorCompletionTests.cs'
$matrixSource=Join-Path $projectRoot 'src\task_items\ActorMatrixInitializationTests.cs'
$outputParent=Join-Path $projectRoot '.tooling'
$outputLeaf='actor-engine-context-build-20261021'
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

# Validate fixed inputs and every ancestor before creating output.
# /noconfig disables csc.rsp; the pinned compiler's implicit mscorlib is sufficient.
Assert-NormalExistingPath $expectedScript $false
Assert-NormalExistingPath $compiler $false
$compilerHash=(Get-FileHash -LiteralPath $compiler -Algorithm SHA256 -ErrorAction Stop).Hash
if (![String]::Equals($compilerHash,$expectedCompilerSha256,[StringComparison]::OrdinalIgnoreCase)) {
 throw 'Installed compiler differs from the reviewed SHA256 pin'
}
foreach ($source in $librarySources) {Assert-NormalExistingPath $source $false}
Assert-NormalExistingPath $testSource $false
Assert-NormalExistingPath $completionSource $false
Assert-NormalExistingPath $matrixSource $false
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
$library=Join-Path $target 'GrimDynasty.ActorEngineContext.dll'
$tests=Join-Path $target 'ActorEngineContextTests.exe'
$completion=Join-Path $target 'ActorConstructorCompletionTests.exe'
$matrix=Join-Path $target 'ActorMatrixInitializationTests.exe'

# Exactly six library sources and three suites: four compiler invocations total.
& $compiler /noconfig /nologo /langversion:5 /target:library /checked+ /optimize+ /debug- "/out:$library" @librarySources
$libraryExit=$LASTEXITCODE
if ($libraryExit -ne 0) {throw "Actor engine context module compilation failed with exit code $libraryExit"}
Assert-NormalExistingPath $library $false
& $compiler /noconfig /nologo /langversion:5 /target:exe /checked+ /optimize+ /debug- "/reference:$library" "/out:$tests" $testSource
$testCompileExit=$LASTEXITCODE
if ($testCompileExit -ne 0) {throw "Actor engine context tests compilation failed with exit code $testCompileExit"}
Assert-NormalExistingPath $tests $false
& $compiler /noconfig /nologo /langversion:5 /target:exe /checked+ /optimize+ /debug- "/reference:$library" "/out:$completion" $completionSource
$completionCompileExit=$LASTEXITCODE
if ($completionCompileExit -ne 0) {throw "Affected Actor completion regression compilation failed with exit code $completionCompileExit"}
Assert-NormalExistingPath $completion $false
& $compiler /noconfig /nologo /langversion:5 /target:exe /checked+ /optimize+ /debug- "/reference:$library" "/out:$matrix" $matrixSource
$matrixCompileExit=$LASTEXITCODE
if ($matrixCompileExit -ne 0) {throw "Affected Actor matrix regression compilation failed with exit code $matrixCompileExit"}
Assert-NormalExistingPath $matrix $false

# Exactly three own test executables. No previous build scripts or synchronization suite.
Write-Output 'NEW SUITE: ActorEngineContextTests (counts reported separately)'
& $tests
$testExit=$LASTEXITCODE
if ($testExit -ne 0) {throw "Actor engine context tests failed with exit code $testExit"}
Write-Output 'AFFECTED REGRESSION: ActorConstructorCompletionTests (counts reported separately)'
& $completion
$completionExit=$LASTEXITCODE
if ($completionExit -ne 0) {throw "Affected Actor completion regression failed with exit code $completionExit"}
Write-Output 'AFFECTED REGRESSION: ActorMatrixInitializationTests (counts reported separately)'
& $matrix
$matrixExit=$LASTEXITCODE
if ($matrixExit -ne 0) {throw "Affected Actor matrix regression failed with exit code $matrixExit"}
