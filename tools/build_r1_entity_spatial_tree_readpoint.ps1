param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$previous=Join-Path $taskRoot '.tooling\r1-entity-spatial-tree-validation'
$output=Join-Path $taskRoot '.tooling\r1-entity-spatial-tree-readpoint-validation'
$priorResult=Join-Path $previous 'result.json'
$priorResultHash='eb1bf1dde1adc7d9bf1adb0eaa72cf0a2d79e704c39d8c52f1276c4e8e467823'
$priorLibrary=Join-Path $previous 'GrimDynasty.EntitySpatialTree.dll'
$priorLibraryHash='78937f1a68038238b256070f56ea8def3907d75ea5ac004352a629abd4808543'
$priorSourceHash='994c094298f91e7ebc4c624de6595dbfe334aa5ab7bda909c36269f255fae443'
$sources=@{
 'EntitySpatialTreeContracts.cs'='9416fde5663facd59308c1da419c07dbafd0036b590f7d5e3067907bd5b81367'
 'EntitySpatialTreeReadPointTests.cs'='c290214f6a663d9072f3d01d217c95d7f9b54f6544fa89ec0b576337f3220e97'
}
$dependencies=@{
 'GrimDynasty.Actions.dll'='3ea34074db9d5a67d7dffb702eb053b703dc3b754d0db442aeadd71247e15c58'
 'GrimDynasty.MoveManager.dll'='7563b6e32745e79ddc8308d2ea48ca7336050271d8dc8c9ce1e75346a52232da'
 'GrimDynasty.WorldCoordinates.dll'='6365ad6a051d6ffbb4009824d98fa899bec89f5933dea383a9a3d98f193e71c7'
 'GrimDynasty.NavigationNearest.dll'='75f303c482679df6ba2c8f01aad7759ba9ed322ad6386100525b690cd9b9dfd0'
 'GrimDynasty.EntityMovement.dll'='85d848625fce7d5fc580feb04839898bd580a96f0c48308c3aca5b8ab142f84d'
 'GrimDynasty.GameMovementCallbacks.dll'='21aa8443153c7a06575f25c32b4f7ef53d73b8d20c0cdc8ccda11c532ee0f5ed'
 'GrimDynasty.ActorBoundingBox.dll'='0d95e34f3a1111bd639f3f4fe915aec0ae2d07779198ac4a6559e5edb1b83d1f'
}
$evidence=Join-Path $taskRoot 'docs\reverse-client\r1-spatial-ground-direct\08-SpatialGroundDirect-0x18011bbc0-procedure_assembly.json'
$evidenceHash='1f8618dfe1a6c815e9bb5e75f2fb9d87ec273acea425e49c929d1ebe7fa16416'
function Hash([string]$path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-File([string]$path,[string]$expected){
 $item=Get-Item -LiteralPath $path
 if($expected -notmatch '^[0-9a-f]{64}$' -or $item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -or (Hash $path) -ne $expected){throw ('Input identity changed: '+$path)}
}
function Check-MinimalSourceRevision {
 $text=[IO.File]::ReadAllText((Join-Path $taskRoot 'src\reverse_client_actions\EntitySpatialTreeContracts.cs'),[Text.Encoding]::UTF8)
 $old='    parent=N(parent,context,0x11bbde,i);if(hops++>=context.OwnTransitionLimit)throw new MissingContract(i.At,"Own finite prune domain exhausted");'
 $new='    // 11BBDE is LEA only; the first parent dereference is MOV [RBX]11BBF0.'+"`n"+$old.Replace('0x11bbde','0x11bbf0')
 if(!$text.Contains($new)){throw 'Expected exact readpoint revision fragment missing'}
 $restored=$text.Replace($new,$old);$hasher=[Security.Cryptography.SHA256]::Create()
 try{$restoredHash=([BitConverter]::ToString($hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes($restored)))).Replace('-','').ToLowerInvariant()}finally{$hasher.Dispose()}
 if($restoredHash -ne $priorSourceHash){throw 'Revision exceeds the single readpoint change and its comment'}
}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace, no arguments required'}
if(Test-Path -LiteralPath $output){throw 'Existing revision evidence preserved; output must be new'}
Check-File $priorResult $priorResultHash;Check-File $priorLibrary $priorLibraryHash
$prior=Get-Content -LiteralPath $priorResult -Raw|ConvertFrom-Json
if($prior.status -ne 'passed-new-entity-spatial-tree-only' -or $null -ne $prior.failure -or $prior.source_pins.'EntitySpatialTreeContracts.cs' -ne $priorSourceHash){throw 'Original passed23 source/result is required'}
if(@($prior.test_results).Count -ne 1 -or $prior.test_results[0].scenarios -ne 23 -or $prior.test_results[0].assertions -ne 273){throw 'Original23/273 result changed'}
$oldLibraryRecord=@($prior.artifacts|Where-Object {$_.name -eq 'GrimDynasty.EntitySpatialTree.dll'})
if($oldLibraryRecord.Count -ne 1 -or $oldLibraryRecord[0].sha256 -ne $priorLibraryHash){throw 'Original spatial DLL is not pinned by passed result'}
foreach($name in $dependencies.Keys){$record=@($prior.artifacts|Where-Object {$_.name -eq $name});if($record.Count -ne 1 -or $record[0].sha256 -ne $dependencies[$name]){throw ('Dependency not pinned by passed result: '+$name)}}
function Check-Inputs {
 Check-File $compiler $compilerHash;Check-File $priorResult $priorResultHash;Check-File $priorLibrary $priorLibraryHash;Check-File $evidence $evidenceHash
 foreach($name in $sources.Keys){Check-File (Join-Path $taskRoot ('src\reverse_client_actions\'+$name)) $sources[$name]}
 foreach($name in $dependencies.Keys){Check-File (Join-Path $previous $name) $dependencies[$name]}
 Check-MinimalSourceRevision
}
Check-Inputs
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash
 source_pins=$sources;dependency_pins=$dependencies;native_evidence=$evidence;native_evidence_sha256=$evidenceHash
 prior_result=$priorResult;prior_result_sha256=$priorResultHash;prior_library=$priorLibrary;prior_library_sha256=$priorLibraryHash;prior_source_sha256=$priorSourceHash;minimal_source_revision_verified=$true
 prior_passed_scenarios=23;prior_passed_assertions=273;prior_scenarios_rerun=0;steps=@();test_results=@();artifacts=@();old_test_suites_rerun=@();old_executable_entrypoints_invoked=@();failure=$null
 new_prune_readpoint_only=$false;loaded_native_verified=$false;original_read=$false;original_executed=$false;backend_started=$false;position_publication_validated=$false;gameplay_accepted=$false
 support_boundary='Single Prune11BBC0 parent dereference address correction from LEA11BBDE to first MOV11BBF0. New known-null/released/unbound failure-prefix tests only. Prior23/273 retained, not rerun. No wider native or gameplay compatibility claim.'
}
New-Item -ItemType Directory -Path $output|Out-Null
function Run-Step([string]$name,[string]$program,[string[]]$arguments){
 $stdout=Join-Path $output ($name+'.log');$stderr=Join-Path $output ($name+'.stderr.log');$oldPreference=$ErrorActionPreference
 try{$ErrorActionPreference='Continue'; & $program @arguments 1> $stdout 2> $stderr; $invocationSucceeded=$?;$code=$LASTEXITCODE}finally{$ErrorActionPreference=$oldPreference}
 $report.steps += [ordered]@{name=$name;program=$program;arguments=$arguments;invocation_succeeded=$invocationSucceeded;exit_code=$code}
 if(!$invocationSucceeded -or $code -ne 0){throw "Step $name failed (invocation=$invocationSucceeded, exit=$code); logs retained"}
 if((Get-Item -LiteralPath $stderr).Length -ne 0 -or ($name.StartsWith('compile-') -and (Get-Item -LiteralPath $stdout).Length -ne 0)){throw ('Unexpected diagnostics: '+$name)}
}
try{
 foreach($name in $dependencies.Keys){Copy-Item -LiteralPath (Join-Path $previous $name) -Destination (Join-Path $output $name);Check-File (Join-Path $output $name) $dependencies[$name]}
 $references=@();foreach($name in ($dependencies.Keys|Sort-Object)){$references+='/reference:'+(Join-Path $output $name)}
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$library=Join-Path $output 'GrimDynasty.EntitySpatialTree.dll';$tests=Join-Path $output 'EntitySpatialTreeReadPointTests.exe'
 Run-Step 'compile-spatial-readpoint-revision' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@((Join-Path $source 'EntitySpatialTreeContracts.cs')))
 Run-Step 'compile-new-readpoint-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:EntitySpatialTreeReadPointTests',"/out:$tests","/reference:$library")+$references+@((Join-Path $source 'EntitySpatialTreeReadPointTests.cs')))
 Run-Step 'new-readpoint-tests' $tests @()
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-readpoint-tests.log') -Raw|ConvertFrom-Json
 if($summary.status -ne 'passed' -or $summary.scenarios -ne 3 -or $summary.assertions -le 0 -or $summary.new_prune_readpoint_only -ne $true -or $summary.original23_rerun -ne $false -or $summary.loaded_native_verified -ne $false -or $summary.gameplay_accepted -ne $false){throw 'Missing or unexpected readpoint-only summary'}
 $report.test_results += [ordered]@{name='new-parent-readpoint-and-failure-prefixes';kind='new';scenarios=$summary.scenarios;assertions=$summary.assertions}
 Check-Inputs;foreach($name in $dependencies.Keys){Check-File (Join-Path $output $name) $dependencies[$name]}
 $report.new_prune_readpoint_only=$true;$report.status='passed-new-spatial-prune-readpoint-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 9|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;steps=$report.steps;test_results=$report.test_results;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 6
