param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$jsonAssembly='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Web.Extensions.dll'
$jsonAssemblyHash='5e6378edc06f5a61920aaadb2aba839dd3cf302d6192e08aacd4454ba4cff0b3'
$manifestPath=Join-Path $taskRoot 'tools\r1_game_path_precheck_build_fingerprints.json'
$manifestHash='4ea791c59a54468399ba8812223361701c559dfb5f8086b8b6ae0a822ab81231'
$output=Join-Path $taskRoot '.tooling\r1-game-path-precheck-validation'
$library=Join-Path $output 'GrimDynasty.GamePathPrecheck.dll'
$tests=Join-Path $output 'GamePathPrecheckTests.exe'
function Hash([string]$p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-Ancestors([string]$p){$cursor=[IO.Path]::GetFullPath($p);while($cursor){if(Test-Path -LiteralPath $cursor){$item=Get-Item -LiteralPath $cursor -Force;if($item.Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Reparse path not permitted: '+$cursor)}};$parent=[IO.Directory]::GetParent($cursor);if($null -eq $parent){break};$cursor=$parent.FullName}}
function Check-File([string]$p,[string]$expected){Check-Ancestors $p;if($expected -notmatch '^[0-9a-f]{64}$'){throw ('Unfrozen input: '+$p)};$item=Get-Item -LiteralPath $p;if($item.PSIsContainer -or (Hash $p) -ne $expected){throw ('Input identity changed: '+$p)}}
function Under-Root([string]$relative){if([IO.Path]::IsPathRooted($relative)){throw 'Manifest paths must be relative'};$p=[IO.Path]::GetFullPath((Join-Path $taskRoot $relative));if(!$p.StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Manifest path leaves workspace'};return $p}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace; no arguments'}
Check-File $manifestPath $manifestHash
$manifest=Get-Content -LiteralPath $manifestPath -Raw|ConvertFrom-Json
if($manifest.schema -ne 'grim-dynasty.fixed-game-path-precheck-build.v1' -or $manifest.compiles -ne 2 -or $manifest.sources.Count -ne 3 -or $manifest.assemblies.Count -ne 27 -or $manifest.prerequisites.Count -ne 4 -or $manifest.evidence.Count -ne 29 -or $manifest.new_scenarios -ne 6 -or $manifest.new_library -ne 'GrimDynasty.GamePathPrecheck.dll' -or $manifest.new_test_entrypoint -ne 'GamePathPrecheckTests'){throw 'Unexpected fixed plan'}
if(@($manifest.assemblies.name|Sort-Object -Unique).Count -ne 27){throw 'Duplicate assembly names'}
foreach($entry in $manifest.assemblies){if($entry.name -ne [IO.Path]::GetFileName($entry.name) -or (!$entry.name.EndsWith('.dll') -and $entry.name -ne 'NavigationSearchWrapperTests.exe')){throw 'Only fixed flat managed dependency filenames permitted'}}
Check-Ancestors $output
if(!([IO.Path]::GetFullPath($output)).StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Output outside workspace'}
if(Test-Path -LiteralPath $output){throw 'Existing output preserved; no overwrite/retry'}
function Check-Inputs {
 Check-File $compiler $compilerHash;Check-File $jsonAssembly $jsonAssemblyHash;Check-File $manifestPath $manifestHash
 foreach($entry in $manifest.sources){Check-File (Under-Root $entry.path) $entry.sha256}
 foreach($entry in $manifest.evidence){Check-File (Under-Root $entry.path) $entry.sha256}
 foreach($entry in $manifest.prerequisites){Check-File (Under-Root $entry.path) $entry.sha256;$prior=Get-Content -LiteralPath (Under-Root $entry.path) -Raw|ConvertFrom-Json;if($prior.status -ne $entry.status -or $null -ne $prior.failure){throw ('Prerequisite no longer passed: '+$entry.path)}}
 foreach($entry in $manifest.assemblies){
  Check-File (Under-Root $entry.path) $entry.sha256
  if(@($manifest.prerequisites|Where-Object path -EQ $entry.result).Count -ne 1){throw ('Unpinned artifact receipt: '+$entry.name)}
  $prior=Get-Content -LiteralPath (Under-Root $entry.result) -Raw|ConvertFrom-Json
  $record=@($prior.artifacts|Where-Object {($_.name -eq $entry.name -or ($_.path -and [IO.Path]::GetFileName($_.path) -eq $entry.name)) -and $_.sha256 -eq $entry.sha256})
  if($record.Count -ne 1){throw ('Assembly lacks exact artifact receipt: '+$entry.name)}
 }
 foreach($entry in $manifest.rea_completed){Check-File (Under-Root $entry.path) $entry.sha256;$prior=Get-Content -LiteralPath (Under-Root $entry.path) -Raw|ConvertFrom-Json;if($prior.status -ne 'passed' -or $prior.completed_plan_calls -ne $entry.calls -or $prior.closed_session_verified -ne $true -or $prior.process_exit_confirmed -ne $true -or $prior.owned_memory_profile.job_empty_confirmed -ne $true -or $prior.owned_memory_profile.sampler_exit_confirmed -ne $true){throw ('Unclosed Rea evidence: '+$entry.path)}}
}
Check-Inputs
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash;json_assembly=$jsonAssembly;json_assembly_sha256=$jsonAssemblyHash
 manifest=$manifestPath;manifest_sha256=$manifestHash;source_pins=$manifest.sources;prerequisites=$manifest.prerequisites;assembly_pins=$manifest.assemblies;evidence_pins=$manifest.evidence
 steps=@();test_results=@();samples=@();artifacts=@();failure=$null;compiles=0;scenarios=0;assertions=0
 old_test_suites_rerun=@();old_executable_entrypoints_invoked=@();old_source_recompiled=$false;prior_fixture_constructor='NavigationSearchWrapperTests.Scene';prior_fixture_initialization_checks=4
 actual_ground_start_attempted=$false;ground_start_result=$null;ground_start_path_accepted=$false;height_offset_added=$false;injected_success=$false
 original_read=$false;original_executed=$false;backend_started=$false;security_settings_changed=$false
 system_library_loaded=$false;native_math_calls=0;full_tick=$false;physical_mouse=$false;walking_accepted=$false;mouse_input_chain_complete=$false
 support_boundary='Actual same-Region08E200 via canonical GetCoords/PortalSelection/22C240/01C510 hot globals/150590+10C6C0 and saved Ground. Same MovementManager+70, actual path queries and delayed outputs. Unknown or unbound Region is an explicit OWN object-return adapter stop before the native opaque getter. Separate navigation-start and actual Ground-start samples; actual Ground-start return is recorded without forcing success or changing extents. No request installation, walking or physical mouse acceptance.'
}
New-Item -ItemType Directory -Path $output|Out-Null
function Run-Step([string]$name,[string]$program,[string[]]$arguments){
 if($program -ne $compiler -and $program -ne $tests){throw 'Only fixed compiler and new test entrypoint permitted'}
 foreach($argument in $arguments){if($argument -match '[\s"]'){throw 'This fixed no-space argument plan changed'}}
 $stdout=Join-Path $output ($name+'.log');$stderr=Join-Path $output ($name+'.stderr.log')
 $start=@{FilePath=$program;WorkingDirectory=$taskRoot;WindowStyle='Hidden';PassThru=$true;RedirectStandardOutput=$stdout;RedirectStandardError=$stderr}
 if($arguments.Count -gt 0){$start.ArgumentList=$arguments}
 $process=Start-Process @start
 try{
  $ownedProcessHandle=$process.Handle
  $completed=$process.WaitForExit(60000)
  if(!$completed){try{$process.Kill()}catch{if(!$process.HasExited){throw}};$process.WaitForExit();throw ('Fixed sixty-second step budget exceeded: '+$name)}
  $process.WaitForExit();$code=$process.ExitCode
  $report.steps += [ordered]@{name=$name;program=$program;arguments=$arguments;exit_code=$code;timeout_ms=60000}
  if($code -ne 0){throw ('Step failed: '+$name+'; exit='+$code+'; logs retained')}
  if((Get-Item -LiteralPath $stderr).Length -ne 0 -or ($name.StartsWith('compile-') -and (Get-Item -LiteralPath $stdout).Length -ne 0)){throw ('Unexpected diagnostics: '+$name)}
 }finally{$process.Dispose()}
}
try{
 foreach($entry in $manifest.assemblies){Copy-Item -LiteralPath (Under-Root $entry.path) -Destination (Join-Path $output $entry.name);Check-File (Join-Path $output $entry.name) $entry.sha256}
 $references=@('/reference:'+$jsonAssembly);foreach($entry in $manifest.assemblies){$references += '/reference:'+(Join-Path $output $entry.name)}
 $source=Join-Path $taskRoot 'src\reverse_client_actions'
 $production=@('WorldVectorDifferenceContracts.cs','GamePathPrecheckContracts.cs')|ForEach-Object{Join-Path $source $_}
 $testing=Join-Path $source 'GamePathPrecheckTests.cs'
 $report.compiles++
 Run-Step 'compile-game-path-precheck' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+$production)
 $report.compiles++
 Run-Step 'compile-game-path-precheck-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:GamePathPrecheckTests',"/out:$tests","/reference:$library")+$references+@($testing))
 Run-Step 'new-components-and-saved-ground-attempt' $tests @()
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-components-and-saved-ground-attempt.log') -Tail 1|ConvertFrom-Json
 if($summary.status -ne 'passed-new-game-path-precheck-components-only' -or $summary.scenarios -ne 6 -or $summary.assertions -le 0 -or $summary.samples.Count -ne 3){throw 'Unexpected new component summary'}
 foreach($flag in @('full_tick','physical_mouse','walking_accepted','height_offset_added','injected_success','system_library_loaded','original_read','original_executed','backend_started','prior_suite_entrypoints_called')){if($summary.$flag -ne $false){throw ('Scope flag changed: '+$flag)}}
 if($summary.old_test_suites_rerun -ne 0 -or $summary.native_math_calls -ne 0 -or $summary.prior_fixture_initialization_checks -ne 4 -or $summary.actual_ground_start_attempted -ne $true -or $summary.ground_start_result -lt 0 -or $summary.ground_start_result -gt 4 -or $summary.ground_start_path_accepted -ne ($summary.ground_start_result -eq 0)){throw 'Unplanned execution or missing actual Ground-start outcome'}
 $report.test_results=@($summary);$report.samples=$summary.samples;$report.scenarios=$summary.scenarios;$report.assertions=$summary.assertions
 $report.actual_ground_start_attempted=$true;$report.ground_start_result=$summary.ground_start_result;$report.ground_start_path_accepted=$summary.ground_start_path_accepted
 Check-Inputs;foreach($entry in $manifest.assemblies){Check-File (Join-Path $output $entry.name) $entry.sha256}
 $report.status='passed-new-game-path-precheck-components-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}};$report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 16|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;scenarios=$report.scenarios;assertions=$report.assertions;ground_start_result=$report.ground_start_result;ground_start_path_accepted=$report.ground_start_path_accepted;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 6
