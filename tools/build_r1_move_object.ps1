param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$manifestPath=Join-Path $taskRoot 'tools\r1_move_object_build_fingerprints.json'
$manifestHash='19a1996afe550f877b38b663d53fd4034664ed33a4b13b39a04700f7ba394928'
$output=Join-Path $taskRoot '.tooling\r1-move-object-validation'
function Hash([string]$path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-Ancestors([string]$path){
 $cursor=[IO.Path]::GetFullPath($path)
 while($cursor){
  if(Test-Path -LiteralPath $cursor){$item=Get-Item -LiteralPath $cursor -Force;if($item.Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Reparse path not permitted: '+$cursor)}}
  $parent=[IO.Directory]::GetParent($cursor);if($null -eq $parent){break};$cursor=$parent.FullName
 }
}
function Check-File([string]$path,[string]$expected){
 Check-Ancestors $path;$item=Get-Item -LiteralPath $path
 if($expected -notmatch '^[0-9a-f]{64}$' -or $item.PSIsContainer -or (Hash $path) -ne $expected){throw ('Input identity changed or unfrozen: '+$path)}
}
function Workspace-Path([string]$relative){
 if([IO.Path]::IsPathRooted($relative)){throw 'Manifest paths must be workspace-relative'}
 $path=[IO.Path]::GetFullPath((Join-Path $taskRoot $relative))
 if(!$path.StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Manifest path left fixed workspace'}
 return $path
}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace and no arguments required'}
Check-Ancestors $output
if(!([IO.Path]::GetFullPath($output)).StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Output must stay in fixed workspace'}
if(Test-Path -LiteralPath $output){throw 'Existing MoveObject evidence preserved; output must be new'}
Check-File $manifestPath $manifestHash
$manifest=Get-Content -LiteralPath $manifestPath -Raw|ConvertFrom-Json
if($manifest.schema -ne 'grim-dynasty.fixed-move-object-build.v1' -or $manifest.source_review_ready -ne $true -or $manifest.compiles -ne 2 -or $manifest.new_scenarios -ne 24){throw 'Unexpected fixed MoveObject plan'}
if(@($manifest.sources).Count -ne 2 -or @($manifest.dependencies).Count -ne 12 -or @($manifest.evidence).Count -ne 13 -or @($manifest.prior_results).Count -ne 3){throw 'Fixed two-source/twelve-dependency/thirteen-evidence/three-prior plan required'}
if(@($manifest.dependencies.name|Sort-Object -Unique).Count -ne 12){throw 'Dependency DLL names must be unique'}
foreach($dependency in $manifest.dependencies){if($dependency.name -ne [IO.Path]::GetFileName($dependency.name)){throw 'Dependency names cannot redirect output'}}
function Check-Inputs {
 Check-Ancestors $output;Check-File $compiler $compilerHash;Check-File $manifestPath $manifestHash
 foreach($record in $manifest.prior_results){Check-File (Workspace-Path $record.path) $record.sha256}
 foreach($record in $manifest.sources){Check-File (Workspace-Path $record.path) $record.sha256}
 foreach($record in $manifest.dependencies){Check-File (Workspace-Path $record.path) $record.sha256}
 foreach($record in $manifest.evidence){Check-File (Workspace-Path $record.path) $record.sha256}
}
Check-Inputs
foreach($receipt in $manifest.prior_results){
 $prior=Get-Content -LiteralPath (Workspace-Path $receipt.path) -Raw|ConvertFrom-Json
 if($prior.status -ne $receipt.expected_status -or $null -ne $prior.failure){throw 'Passed dependency receipts required'}
 foreach($dependency in @($manifest.dependencies|Where-Object result -EQ $receipt.path)){
  $records=@($prior.artifacts|Where-Object {$_.name -eq $dependency.name})
  if($records.Count -ne 1 -or $records[0].sha256 -ne $dependency.sha256){throw ('Dependency not uniquely pinned by its passed result: '+$dependency.name)}
 }
}
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash
 manifest=$manifestPath;manifest_sha256=$manifestHash;source_pins=$manifest.sources;dependency_pins=$manifest.dependencies;prior_result_pins=$manifest.prior_results;evidence_pins=$manifest.evidence
 steps=@();test_results=@();artifacts=@();failure=$null;old_test_suites_rerun=@();old_executable_entrypoints_invoked=@()
 new_move_object_contracts=$false;actual_eight_byte_hash=$false;actual_projection=$false;shared_agent_state=$false;old_path_compression=$false
 native_registration=$false;agent_motion=$false;physical_mouse=$false;gameplay_accepted=$false;external_success_callbacks=$false
 original_read=$false;original_executed=$false;backend_started=$false;security_settings_changed=$false;system_native_library_load_requested=$false
 support_boundary=$manifest.support_boundary
}
New-Item -ItemType Directory -Path $output|Out-Null
function Run-Step([string]$name,[string]$program,[string[]]$arguments){
 $stdout=Join-Path $output ($name+'.log');$stderr=Join-Path $output ($name+'.stderr.log');$oldPreference=$ErrorActionPreference
 try{$ErrorActionPreference='Continue'; & $program @arguments 1> $stdout 2> $stderr;$ok=$?;$code=$LASTEXITCODE}finally{$ErrorActionPreference=$oldPreference}
 $report.steps += [ordered]@{name=$name;program=$program;arguments=$arguments;invocation_succeeded=$ok;exit_code=$code}
 if(!$ok -or $code -ne 0){throw "Step $name failed (invocation=$ok, exit=$code); logs retained"}
 if((Get-Item -LiteralPath $stderr).Length -ne 0 -or ($name.StartsWith('compile-') -and (Get-Item -LiteralPath $stdout).Length -ne 0)){throw ('Unexpected diagnostics: '+$name)}
}
try{
 foreach($dependency in $manifest.dependencies){Copy-Item -LiteralPath (Workspace-Path $dependency.path) -Destination (Join-Path $output $dependency.name);Check-File (Join-Path $output $dependency.name) $dependency.sha256}
 $references=@();foreach($dependency in @($manifest.dependencies|Sort-Object name)){$references+='/reference:'+(Join-Path $output $dependency.name)}
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$library=Join-Path $output 'GrimDynasty.NavigationMoveObject.dll';$tests=Join-Path $output 'NavigationMoveObjectTests.exe'
 Run-Step 'compile-move-object-contracts' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@((Join-Path $source 'NavigationMoveObjectContracts.cs')))
 Run-Step 'compile-move-object-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:NavigationMoveObjectTestProgram',"/out:$tests","/reference:$library")+$references+@((Join-Path $source 'NavigationMoveObjectTests.cs')))
 Run-Step 'new-move-object-tests' $tests @()
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-move-object-tests.log') -Tail 1|ConvertFrom-Json
 if($summary.status -ne 'passed-move-object-request-only' -or $summary.scenarios -ne 24 -or $summary.assertions -le 0 -or $summary.agent_motion -ne $false -or $summary.native_registration -ne $false -or $summary.physical_mouse -ne $false){throw 'Unexpected focused MoveObject test summary'}
 $report.test_results += [ordered]@{name='new-move-object-tests';kind='new';scenarios=$summary.scenarios;assertions=$summary.assertions}
 Check-Inputs;foreach($dependency in $manifest.dependencies){Check-File (Join-Path $output $dependency.name) $dependency.sha256}
 $report.new_move_object_contracts=$true;$report.actual_eight_byte_hash=$true;$report.actual_projection=$true;$report.shared_agent_state=$true;$report.old_path_compression=$true
 $report.status='passed-new-move-object-request-and-shared-state-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}};$report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;test_results=$report.test_results;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 8
