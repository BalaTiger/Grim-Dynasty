param([string]$OutputDirectory=(Join-Path $PSScriptRoot '..\..\.tooling\task-items-build'))
$ErrorActionPreference='Stop'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))+[IO.Path]::DirectorySeparatorChar
$target=[IO.Path]::GetFullPath($OutputDirectory)
if (!$target.StartsWith($root,[StringComparison]::OrdinalIgnoreCase)) {throw 'Project output required'}
New-Item -ItemType Directory -Path $target -Force | Out-Null
foreach($name in @('GrimDynasty.Conditions.dll','GrimDynasty.TaskLogic.dll','GrimDynasty.TokenActions.dll','GrimDynasty.TaskRuntime.dll','GrimDynasty.TaskAuto.dll','GrimDynasty.TaskKills.dll','GrimDynasty.Quests.dll')) {Copy-Item -LiteralPath (Join-Path $PSScriptRoot "..\..\.tooling\task-kills-build\$name") -Destination $target -Force}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '..\..\.tooling\quest-definition-build\GrimDynasty.Quests.dll') -Destination $target -Force
& $compiler /nologo /target:library /checked+ /optimize+ "/out:$target\GrimDynasty.Resources.dll" "$PSScriptRoot\..\resource_codec\Lz4Block.cs" "$PSScriptRoot\..\resource_codec\Lz4OutputSizeProfile.cs" "$PSScriptRoot\..\resource_codec\RecordStringRemap.cs" "$PSScriptRoot\..\resource_codec\ArchiveStringImport.cs" "$PSScriptRoot\..\resource_codec\ResourceContainers.cs" "$PSScriptRoot\..\resource_codec\ArzRawRecordReader.cs"
if ($LASTEXITCODE -ne 0) {throw 'Raw resource module compile failed'}
$refs=@("/reference:$target\GrimDynasty.Conditions.dll","/reference:$target\GrimDynasty.TaskLogic.dll","/reference:$target\GrimDynasty.TokenActions.dll","/reference:$target\GrimDynasty.TaskRuntime.dll","/reference:$target\GrimDynasty.TaskAuto.dll","/reference:$target\GrimDynasty.TaskKills.dll","/reference:$target\GrimDynasty.Resources.dll")
& $compiler /nologo /target:library /checked+ /optimize+ @refs "/out:$target\GrimDynasty.TaskItems.dll" "$PSScriptRoot\ItemCountQuery.cs" "$PSScriptRoot\HasItemCondition.cs" "$PSScriptRoot\HasItemDefinitionAdapter.cs" "$PSScriptRoot\InventoryTake.cs" "$PSScriptRoot\InventoryAdd.cs" "$PSScriptRoot\ObjectIdAllocator.cs" "$PSScriptRoot\ClassFactoryDispatch.cs" "$PSScriptRoot\ItemScalarInitialization.cs" "$PSScriptRoot\ClassRegistrationInitialization.cs" "$PSScriptRoot\RegistrationIndex.cs" "$PSScriptRoot\LoadedObjectRegistration.cs" "$PSScriptRoot\ObjectBaseConstructor.cs" "$PSScriptRoot\ObjectNameString.cs" "$PSScriptRoot\ObjectNameAssignment.cs" "$PSScriptRoot\RecordCache.cs" "$PSScriptRoot\RecordLoadFile.cs" "$PSScriptRoot\RecordTableGetter.cs" "$PSScriptRoot\BinaryStringPool.cs" "$PSScriptRoot\RecordLoadTicks.cs" "$PSScriptRoot\RecordDepotInitialization.cs" "$PSScriptRoot\BinaryWarmTable.cs" "$PSScriptRoot\BinaryNameIdSnapshot.cs" "$PSScriptRoot\BinaryRecordParser.cs" "$PSScriptRoot\RecordCachePublicationPrefix.cs" "$PSScriptRoot\RecordCachePublicationAttempt.cs" "$PSScriptRoot\RecordCacheNodeConstruction.cs" "$PSScriptRoot\RecordCacheCallerPublicationAttempt.cs" "$PSScriptRoot\SuppliedBinaryRecord.cs" "$PSScriptRoot\ArzRecordRemapContext.cs" "$PSScriptRoot\ArzRecordBinarySource.cs" "$PSScriptRoot\RecordLoadFromSuppliedBinary.cs" "$PSScriptRoot\CreatePartialObject.cs" "$PSScriptRoot\FinishPartialObject.cs"
if ($LASTEXITCODE -ne 0) {throw 'Item module compile failed'}
$refs+="/reference:$target\GrimDynasty.TaskItems.dll"
foreach($suite in @('RegistrationIndexTests','RegistrationGrowthTests','LoadedObjectRegistrationTests','ObjectBaseConstructorTests','ItemScalarInitializationTests','ClassRegistrationInitializationTests','ObjectNameAssignmentTests','ObjectLifecycleCombinedTests','RecordLoadFileTests','RecordTableGetterTests','CreatePartialObjectTests','FinishPartialObjectTests','BinaryWarmGetterTests','BinaryStaleGetterTests','BinaryColdGetterTests','BinaryPointerGrowthTests','BinaryRecordParserTests','BinaryMixedRecordParserTests','BinaryBooleanRecordParserTests','RecordCachePublicationPrefixTests','RecordCachePublicationAttemptTests','RecordCacheNodeConstructionTests','RecordCacheCallerPublicationTests','RecordLoadFromSuppliedBinaryTests','RecordLoadFromArzRecordTests','RecordLoadFromArzProfileTests','RecordLoadFromArzRemapTests','RecordLoadFromArzGeneratedMapTests','RecordLoadTicksTests','RecordDepotInitializationTests','BinaryStringPoolTests','BinaryArchiveContextTests','RecordLoadFromFreshArchiveTests','RecordLoadCapturedClockTests')) {
 $testSources=@("$PSScriptRoot\$suite.cs")
 if ($suite -eq 'RecordLoadFromArzRecordTests' -or $suite -eq 'RecordLoadFromArzProfileTests' -or $suite -eq 'RecordLoadFromArzRemapTests' -or $suite -eq 'RecordLoadFromArzGeneratedMapTests' -or $suite -eq 'RecordDepotInitializationTests' -or $suite -eq 'BinaryStringPoolTests' -or $suite -eq 'BinaryArchiveContextTests' -or $suite -eq 'RecordLoadFromFreshArchiveTests' -or $suite -eq 'RecordLoadCapturedClockTests') {$testSources+="$PSScriptRoot\..\resource_codec\ArzRawFixture.cs"}
 & $compiler /nologo /target:exe /checked+ /optimize+ @refs "/out:$target\$suite.exe" @testSources
 if ($LASTEXITCODE -ne 0) {throw "$suite compile failed"}
 & "$target\$suite.exe"
 if ($LASTEXITCODE -ne 0) {throw "$suite failed"}
}
& $compiler /nologo /target:exe /checked+ /optimize+ @refs "/out:$target\ClassFactoryDispatchTests.exe" "$PSScriptRoot\ClassFactoryDispatchTests.cs"
if ($LASTEXITCODE -ne 0) {throw 'Class factory dispatch tests compile failed'}
& "$target\ClassFactoryDispatchTests.exe"
if ($LASTEXITCODE -ne 0) {throw 'Class factory dispatch tests failed'}
& $compiler /nologo /target:exe /checked+ /optimize+ @refs "/out:$target\ItemConditionTests.exe" "$PSScriptRoot\ItemConditionTests.cs"
if ($LASTEXITCODE -ne 0) {throw 'Item tests compile failed'}
& "$target\ItemConditionTests.exe"
if ($LASTEXITCODE -ne 0) {throw 'Item tests failed'}
& $compiler /nologo /target:exe /checked+ /optimize+ @refs "/out:$target\ItemAutoTests.exe" "$PSScriptRoot\ItemAutoTests.cs" "$PSScriptRoot\ExampleItemQuest.cs"
if ($LASTEXITCODE -ne 0) {throw 'Item auto tests compile failed'}
& "$target\ItemAutoTests.exe"
if ($LASTEXITCODE -ne 0) {throw 'Item auto tests failed'}
& $compiler /nologo /target:exe /checked+ /optimize+ /reference:System.Web.Extensions.dll @refs "/out:$target\ItemQuestDemo.exe" "$PSScriptRoot\ItemQuestDemo.cs" "$PSScriptRoot\ExampleItemQuest.cs"
if ($LASTEXITCODE -ne 0) {throw 'Item demo compile failed'}
& "$target\ItemQuestDemo.exe"
if ($LASTEXITCODE -ne 0) {throw 'Item demo failed'}
foreach($probe in @('ItemCatalogProbe','ItemAutoCatalogProbe')) {
 & $compiler /nologo /target:exe /checked+ /optimize+ /reference:System.Web.Extensions.dll @refs "/reference:$target\GrimDynasty.Quests.dll" "/out:$target\$probe.exe" "$PSScriptRoot\$probe.cs"
 if ($LASTEXITCODE -ne 0) {throw 'Item catalog compile failed'}
}
