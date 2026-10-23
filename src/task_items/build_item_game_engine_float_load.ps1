# New parsed Binary float getter/local field window and the affected ItemReturn regression only.
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
$expectedScript=Join-Path $projectRoot 'src\task_items\build_item_game_engine_float_load.ps1'
if (![String]::Equals([IO.Path]::GetFullPath($PSCommandPath),$expectedScript,[StringComparison]::OrdinalIgnoreCase)) {
 throw 'Unexpected build script path'
}
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
# Pin reused from prior approved preflight records; never download a replacement.
$expectedCompilerSha256='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$librarySources=@(
 (Join-Path $projectRoot 'src\task_items\ActorConstructorPrefix.cs'),
 (Join-Path $projectRoot 'src\task_items\ActorConstructorArray.cs'),
 (Join-Path $projectRoot 'src\task_items\ActorConstructorContinuation.cs'),
 (Join-Path $projectRoot 'src\task_items\ActorConstructorCompletion.cs'),
 (Join-Path $projectRoot 'src\task_items\ActorMatrixInitialization.cs'),
 (Join-Path $projectRoot 'src\task_items\ActorEngineContext.cs'),
 (Join-Path $projectRoot 'src\task_items\ItemScalarInitialization.cs'),
 (Join-Path $projectRoot 'src\task_items\ItemConstructor858Prefix.cs'),
 (Join-Path $projectRoot 'src\task_items\ItemConstructorBridge.cs'),
 (Join-Path $projectRoot 'src\task_items\ItemConstructor950.cs'),
 (Join-Path $projectRoot 'src\task_items\ItemConstructorBA8.cs'),
 (Join-Path $projectRoot 'src\task_items\ItemConstructorBE0.cs'),
 (Join-Path $projectRoot 'src\task_items\ItemConstructorIdentity.cs'),
 (Join-Path $projectRoot 'src\task_items\ItemConstructorName.cs'),
 (Join-Path $projectRoot 'src\task_items\ItemConstructorReturn.cs'),
 (Join-Path $projectRoot 'src\task_items\BinaryFloatTableGetter.cs'),
 (Join-Path $projectRoot 'src\task_items\ItemGameEngineFloatLoad.cs'),
 (Join-Path $projectRoot 'src\task_items\RecordTableGetter.cs'),
 (Join-Path $projectRoot 'src\task_items\BinaryWarmTable.cs'),
 (Join-Path $projectRoot 'src\task_items\BinaryNameIdSnapshot.cs'),
 (Join-Path $projectRoot 'src\task_items\BinaryStringPool.cs'),
 (Join-Path $projectRoot 'src\task_items\BinaryRecordParser.cs'),
 (Join-Path $projectRoot 'src\task_items\RecordCache.cs'),
 (Join-Path $projectRoot 'src\task_items\LoadedObjectRegistration.cs'),
 (Join-Path $projectRoot 'src\task_items\RegistrationIndex.cs'),
 (Join-Path $projectRoot 'src\resource_codec\ArzRawRecordReader.cs'),
 (Join-Path $projectRoot 'src\resource_codec\ResourceContainers.cs'),
 (Join-Path $projectRoot 'src\resource_codec\Lz4Block.cs'),
 (Join-Path $projectRoot 'src\resource_codec\Lz4OutputSizeProfile.cs')
)
$testSource=Join-Path $projectRoot 'src\task_items\ItemGameEngineFloatLoadTests.cs'
$priorSource=Join-Path $projectRoot 'src\task_items\ItemConstructorReturnTests.cs'
$outputParent=Join-Path $projectRoot '.tooling'
$outputLeaf='item-game-engine-float-load-build-20261021'
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
# /noconfig disables csc.rsp. Existing parser/container sources additionally
# require the installed framework System and System.Core assemblies.
Assert-NormalExistingPath $expectedScript $false
Assert-NormalExistingPath $compiler $false
$compilerHash=(Get-FileHash -LiteralPath $compiler -Algorithm SHA256 -ErrorAction Stop).Hash
if (![String]::Equals($compilerHash,$expectedCompilerSha256,[StringComparison]::OrdinalIgnoreCase)) {
 throw 'Installed compiler differs from the reviewed SHA256 pin'
}
$frameworkReferences=@(
 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.dll',
 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Core.dll'
)
foreach ($reference in $frameworkReferences) {Assert-NormalExistingPath $reference $false}
foreach ($source in $librarySources) {Assert-NormalExistingPath $source $false}
Assert-NormalExistingPath $testSource $false
Assert-NormalExistingPath $priorSource $false
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
$library=Join-Path $target 'GrimDynasty.ItemGameEngineFloatLoad.dll'
$tests=Join-Path $target 'ItemGameEngineFloatLoadTests.exe'
$prior=Join-Path $target 'ItemConstructorReturnTests.exe'

# Record all 32 reviewed inputs plus the compiler/framework identities before any
# compiler invocation. The fresh output rule protects these logs on failure.
$inputRecords=@()
foreach ($inputPath in @($librarySources)+@($testSource,$priorSource,$expectedScript)) {
 $inputItem=Get-Item -LiteralPath $inputPath -ErrorAction Stop
 $inputRecords += [ordered]@{path=$inputPath.Substring($rootPrefix.Length).Replace('\','/');bytes=$inputItem.Length;sha256=(Get-FileHash -LiteralPath $inputPath -Algorithm SHA256).Hash.ToLowerInvariant()}
}
$referenceRecords=@()
foreach ($reference in $frameworkReferences) {
 $referenceRecords += [ordered]@{path=$reference;bytes=(Get-Item -LiteralPath $reference).Length;sha256=(Get-FileHash -LiteralPath $reference -Algorithm SHA256).Hash.ToLowerInvariant()}
}
[ordered]@{schema='item-game-engine-float-load-build-inputs-v1';compiler=$compiler;compiler_sha256=$compilerHash.ToLowerInvariant();sources=$inputRecords;framework_references=$referenceRecords} |
 ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $target 'inputs.json') -Encoding UTF8
$referenceArgs=@('/reference:'+$frameworkReferences[0],'/reference:'+$frameworkReferences[1])

# Exactly 29 library sources and two suites: three compiler invocations total.
# Every native command's merged output is retained, including failed attempts.
& $compiler /noconfig /nologo /langversion:5 /target:library /checked+ /optimize+ /debug- @referenceArgs "/out:$library" @librarySources 2>&1 |
 Tee-Object -FilePath (Join-Path $target 'compile-library.txt')
$libraryExit=$LASTEXITCODE
if ($libraryExit -ne 0) {throw "Float load module compilation failed with exit code $libraryExit"}
Assert-NormalExistingPath $library $false
& $compiler /noconfig /nologo /langversion:5 /target:exe /checked+ /optimize+ /debug- @referenceArgs "/reference:$library" "/out:$tests" $testSource 2>&1 |
 Tee-Object -FilePath (Join-Path $target 'compile-new-tests.txt')
$testCompileExit=$LASTEXITCODE
if ($testCompileExit -ne 0) {throw "Float load tests compilation failed with exit code $testCompileExit"}
Assert-NormalExistingPath $tests $false
& $compiler /noconfig /nologo /langversion:5 /target:exe /checked+ /optimize+ /debug- @referenceArgs "/reference:$library" "/out:$prior" $priorSource 2>&1 |
 Tee-Object -FilePath (Join-Path $target 'compile-return-regression.txt')
$priorCompileExit=$LASTEXITCODE
if ($priorCompileExit -ne 0) {throw "Affected ItemReturn regression compilation failed with exit code $priorCompileExit"}
Assert-NormalExistingPath $prior $false

# Exactly two own test executables. No previous build scripts or other suites.
Write-Output 'NEW SUITE: ItemGameEngineFloatLoadTests (counts reported separately)'
& $tests 2>&1 | Tee-Object -FilePath (Join-Path $target 'new-tests.txt')
$testExit=$LASTEXITCODE
if ($testExit -ne 0) {throw "Float load tests failed with exit code $testExit"}
Write-Output 'AFFECTED REGRESSION: ItemConstructorReturnTests (counts reported separately)'
& $prior 2>&1 | Tee-Object -FilePath (Join-Path $target 'return-regression.txt')
$priorExit=$LASTEXITCODE
if ($priorExit -ne 0) {throw "Affected ItemReturn regression failed with exit code $priorExit"}
