param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$manifestPath=Join-Path $taskRoot 'tools\r1_player_lookup_build_fingerprints.json'
$manifestHash='3a8d3a31f6fb871e1f1c906c6e38df9d113854c6c479f12713518c0716ca9d52'
$output=Join-Path $taskRoot '.tooling\r1-player-lookup-validation'
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
if(Test-Path -LiteralPath $output){throw 'Existing player-lookup evidence preserved; output must be new'}
Check-File $manifestPath $manifestHash
$manifest=Get-Content -LiteralPath $manifestPath -Raw|ConvertFrom-Json
if($manifest.schema -ne 'grim-dynasty.player-lookup-build.v1' -or $manifest.source_review_ready -ne $true){throw 'Reviewed final player-lookup manifest required'}
if(@($manifest.sources).Count -ne 2 -or @($manifest.dependencies).Count -ne 8 -or @($manifest.evidence).Count -ne 3 -or @($manifest.prior_results).Count -ne 2){throw 'Fixed two-source, eight-dependency, three-evidence, two-prior plan required'}
if(@($manifest.dependencies.name|Sort-Object -Unique).Count -ne 8){throw 'Dependency DLL names must be unique'}
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
 if($prior.status -ne $receipt.expected_status -or $null -ne $prior.failure){throw 'Passed reached-goal and mouse dependency receipts required'}
 foreach($dependency in @($manifest.dependencies|Where-Object prior_result -EQ $receipt.path)){
  $records=@($prior.artifacts|Where-Object {$_.name -eq $dependency.name})
  if($records.Count -ne 1 -or $records[0].sha256 -ne $dependency.sha256){throw ('Dependency not uniquely pinned by its passed result: '+$dependency.name)}
 }
}
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash
 manifest=$manifestPath;manifest_sha256=$manifestHash;source_pins=$manifest.sources;dependency_pins=$manifest.dependencies;prior_result_pins=$manifest.prior_results;evidence_pins=$manifest.evidence
 steps=@();test_results=@();artifacts=@();failure=$null;old_test_suites_rerun=@();old_executable_entrypoints_invoked=@()
 new_player_lookup_contracts=$false;actual_existing_hash_lookup=$false;canonical_class_and_move_character=$false;shared_controller_object_id_cell=$false
 external_success_callbacks=$false;loaded_native_player_validated=$false;native_options_getter_validated=$false;movement_request_chain_validated=$false
 physical_mouse=$false;native_button_mapping=$false;actual_ground_click_to_agent=$false;system_native_library_load_requested=$false
 original_read=$false;original_executed=$false;backend_started=$false;gameplay_accepted=$false;security_settings_changed=$false
 support_boundary='Actual saved Game128530 uses passed hot Engine01C040, passed own locked019D40 hash lookup and canonical MoveCharacter/Entity bridge. Controller30 is one unknown-initialized shared cell; Node.Object18 is never copied into another role. Observed class slots use the existing canonical symbols and591480. Adapter changes only ResolvePlayer; the passed four mouse entries are referenced without recompilation or old tests. CanAct, options, request chain, physical input and gameplay remain unclosed.'
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
 foreach($dependency in $manifest.dependencies){Copy-Item -LiteralPath (Workspace-Path $dependency.path) -Destination (Join-Path $output $dependency.name);Check-File (Join-Path $output $dependency.name) $dependency.sha256}
 $references=@();foreach($dependency in @($manifest.dependencies|Sort-Object name)){$references+='/reference:'+(Join-Path $output $dependency.name)}
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$library=Join-Path $output 'GrimDynasty.PlayerMousePlayerLookup.dll';$tests=Join-Path $output 'PlayerMousePlayerLookupTests.exe'
 Run-Step 'compile-player-lookup-contracts' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@((Join-Path $source 'PlayerMousePlayerLookupContracts.cs')))
 Run-Step 'compile-player-lookup-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:PlayerMousePlayerLookupTestProgram',"/out:$tests","/reference:$library")+$references+@((Join-Path $source 'PlayerMousePlayerLookupTests.cs')))
 Run-Step 'new-player-lookup-tests' $tests @()
 $summaries=[regex]::Matches((Get-Content -LiteralPath (Join-Path $output 'new-player-lookup-tests.log') -Raw),'(?m)^PASS (\d+) scenarios, (\d+) assertions; actual player lookup composition without successful external callbacks\s*$')
 if($summaries.Count -ne 1 -or [int]$summaries[0].Groups[1].Value -ne $manifest.test_expectations.scenarios -or [int]$summaries[0].Groups[2].Value -ne $manifest.test_expectations.assertions){throw 'Unexpected new player-lookup suite summary'}
 $report.test_results += [ordered]@{name='new-player-lookup-tests';kind='new';scenarios=[int]$summaries[0].Groups[1].Value;assertions=[int]$summaries[0].Groups[2].Value}
 Check-Inputs;foreach($dependency in $manifest.dependencies){Check-File (Join-Path $output $dependency.name) $dependency.sha256}
 $report.new_player_lookup_contracts=$true;$report.actual_existing_hash_lookup=$true;$report.canonical_class_and_move_character=$true;$report.shared_controller_object_id_cell=$true
 $report.status='passed-new-actual-player-lookup-contracts-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;test_results=$report.test_results;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 8
