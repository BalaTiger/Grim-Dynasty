param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$manifestPath=Join-Path $taskRoot 'tools\r1_player_mouse_build_fingerprints.json'
$manifestHash='ecd53da6b05aef4fc0b08c37cb14a9e21e23ddc2361d300a50cbb3c9f080193c'
$output=Join-Path $taskRoot '.tooling\r1-player-mouse-validation'
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
if(Test-Path -LiteralPath $output){throw 'Existing player-mouse evidence preserved; output must be new'}
Check-File $manifestPath $manifestHash
$manifest=Get-Content -LiteralPath $manifestPath -Raw|ConvertFrom-Json
if($manifest.schema -ne 'grim-dynasty.player-mouse-build.v1' -or $manifest.source_review_ready -ne $true){throw 'Reviewed final player-mouse manifest required'}
if(@($manifest.sources).Count -ne 4 -or @($manifest.dependencies).Count -ne 3 -or @($manifest.evidence).Count -ne 7){throw 'Fixed four-source, three-dependency, seven-evidence plan required'}
if(@($manifest.dependencies.name|Sort-Object -Unique).Count -ne 3){throw 'Dependency DLL names must be unique'}
foreach($dependency in $manifest.dependencies){if($dependency.name -ne [IO.Path]::GetFileName($dependency.name)){throw 'Dependency names cannot redirect output'}}
function Check-Inputs {
 Check-Ancestors $output;Check-File $compiler $compilerHash;Check-File $manifestPath $manifestHash
 Check-File (Workspace-Path $manifest.prior_result.path) $manifest.prior_result.sha256
 foreach($record in $manifest.sources){Check-File (Workspace-Path $record.path) $record.sha256}
 foreach($record in $manifest.dependencies){Check-File (Workspace-Path $record.path) $record.sha256}
 foreach($record in $manifest.evidence){Check-File (Workspace-Path $record.path) $record.sha256}
}
Check-Inputs
$prior=Get-Content -LiteralPath (Workspace-Path $manifest.prior_result.path) -Raw|ConvertFrom-Json
if($prior.status -ne 'passed-isolated-world-coordinate-contracts-only' -or !$prior.isolated_world_coordinate_contracts -or $null -ne $prior.failure){throw 'Passed canonical actions/movement/position dependency receipt required'}
foreach($dependency in $manifest.dependencies){
 $records=@($prior.artifacts|Where-Object {$_.name -eq $dependency.name})
 if($records.Count -ne 1 -or $records[0].sha256 -ne $dependency.sha256){throw ('Dependency not uniquely pinned by its passed result: '+$dependency.name)}
}
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash
 manifest=$manifestPath;manifest_sha256=$manifestHash;source_pins=$manifest.sources;dependency_pins=$manifest.dependencies;prior_result_pin=$manifest.prior_result;evidence_pins=$manifest.evidence
 steps=@();test_results=@();artifacts=@();failure=$null;old_test_suites_rerun=@();old_executable_entrypoints_invoked=@()
 new_four_mouse_entry_contracts=$false;shared_controller_and_move_character=$false;opaque_pointer_and_b4_forwarding=$false
 external_observation_fixtures=$true;native_player_lookup_validated=$false;native_options_getter_validated=$false;movement_request_chain_validated=$false
 physical_mouse=$false;native_button_mapping=$false;actual_ground_click_to_agent=$false;system_native_library_load_requested=$false
 original_read=$false;original_executed=$false;backend_started=$false;gameplay_accepted=$false;security_settings_changed=$false
 support_boundary='Four exact saved Game mouse entry bodies on canonical Controller/MoveCharacter views and WorldPosition24. Explicit input bytes carry no physical-button meaning. Field snapshots retain Unknown and alias identity; callbacks are explicit test observations only. Concrete unknown dependencies stop at their own target; no fake successful skill/movement or fabricated ActionRecord. Tests establish component branch/write/alias behavior only, not a playable ground-click pipeline.'
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
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$library=Join-Path $output 'GrimDynasty.PlayerMouse.dll';$tests=Join-Path $output 'PlayerMouseContractsTests.exe'
 Run-Step 'compile-player-mouse-contracts' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@((Join-Path $source 'PlayerMouseState.cs'),(Join-Path $source 'PlayerMouseContracts.cs'),(Join-Path $source 'PlayerMouseSkillSelectors.cs')))
 Run-Step 'compile-player-mouse-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:PlayerMouseContractTestProgram',"/out:$tests","/reference:$library")+$references+@((Join-Path $source 'PlayerMouseContractsTests.cs')))
 Run-Step 'new-player-mouse-tests' $tests @()
 $summaries=[regex]::Matches((Get-Content -LiteralPath (Join-Path $output 'new-player-mouse-tests.log') -Raw),'(?m)^PASS (\d+) scenarios, (\d+) assertions; new player mouse state-flow contracts with explicit external observations only\s*$')
 if($summaries.Count -ne 1 -or [int]$summaries[0].Groups[1].Value -ne $manifest.test_expectations.scenarios -or [int]$summaries[0].Groups[2].Value -ne $manifest.test_expectations.assertions){throw 'Unexpected new player-mouse suite summary'}
 $report.test_results += [ordered]@{name='new-player-mouse-tests';kind='new';scenarios=[int]$summaries[0].Groups[1].Value;assertions=[int]$summaries[0].Groups[2].Value}
 Check-Inputs;foreach($dependency in $manifest.dependencies){Check-File (Join-Path $output $dependency.name) $dependency.sha256}
 $report.new_four_mouse_entry_contracts=$true;$report.shared_controller_and_move_character=$true;$report.opaque_pointer_and_b4_forwarding=$true
 $report.status='passed-new-player-mouse-entry-contracts-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;test_results=$report.test_results;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 8
