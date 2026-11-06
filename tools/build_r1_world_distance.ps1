param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$manifestPath=Join-Path $taskRoot 'tools\r1_world_distance_build_fingerprints.json'
$manifestHash='a2cb24f9a45b4227ba8df4dc3133529f929500d98c5d7ca21ba7bd3579d69eb8'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$output=Join-Path $taskRoot '.tooling\r1-world-distance-validation'
function Hash([string]$p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-Ancestors([string]$p){$cursor=[IO.Path]::GetFullPath($p);while($cursor){if(Test-Path -LiteralPath $cursor){if((Get-Item -LiteralPath $cursor).Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Reparse path rejected: '+$cursor)}};$parent=[IO.Directory]::GetParent($cursor);if($null -eq $parent){break};$cursor=$parent.FullName}}
function Under-Root([string]$relative){$p=[IO.Path]::GetFullPath((Join-Path $taskRoot $relative));if(!$p.StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Manifest path leaves workspace'};return $p}
function Check-File([string]$p,[string]$expected){Check-Ancestors $p;if($expected -notmatch '^[0-9a-f]{64}$'){throw ('Unfrozen input: '+$p)};if((Get-Item -LiteralPath $p).PSIsContainer -or (Hash $p) -ne $expected){throw ('Input identity changed: '+$p)}}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace; no arguments'}
Check-File $manifestPath $manifestHash
$manifest=Get-Content -LiteralPath $manifestPath -Raw|ConvertFrom-Json
if($manifest.schema -ne 'grim-dynasty.fixed-world-distance-build.v1' -or $manifest.preparation_status -ne 'frozen-not-executed' -or $manifest.compiles -ne 2 -or $manifest.sources.Count -ne 2 -or $manifest.assemblies.Count -ne 3 -or $manifest.new_scenarios -ne 8){throw 'Unexpected or unfrozen fixed plan'}
if(($manifest.sources.path -join '|') -ne 'src/reverse_client_actions/WorldDistanceContracts.cs|src/reverse_client_actions/WorldDistanceTests.cs'){throw 'Unexpected source partition'}
if(($manifest.assemblies.name -join '|') -ne 'GrimDynasty.Actions.dll|GrimDynasty.WorldCoordinates.dll|GrimDynasty.MoveManager.dll'){throw 'Unexpected dependency closure'}
Check-Ancestors $output
if(Test-Path -LiteralPath $output){throw 'Existing output preserved; no overwrite/retry'}
function Check-Inputs {
 Check-File $compiler $compilerHash;Check-File $manifestPath $manifestHash
 foreach($entry in @($manifest.sources)+@($manifest.evidence)){Check-File (Under-Root $entry.path) $entry.sha256}
 Check-File (Under-Root $manifest.prerequisite.path) $manifest.prerequisite.sha256
 $prior=Get-Content -LiteralPath (Under-Root $manifest.prerequisite.path) -Raw|ConvertFrom-Json
 if($prior.status -ne 'passed-controller-move-production-prefix-and-saved-map-integration-only' -or $null -ne $prior.failure){throw 'Dependency receipt is not passed'}
 foreach($entry in $manifest.assemblies){Check-File (Under-Root $entry.path) $entry.sha256;$record=@($prior.artifacts|Where-Object{$_.name -eq $entry.name -and $_.sha256 -eq $entry.sha256});if($record.Count -ne 1){throw ('Dependency artifact receipt mismatch: '+$entry.name)}}
 Check-File (Under-Root $manifest.rea_completed.path) $manifest.rea_completed.sha256
 $rea=Get-Content -LiteralPath (Under-Root $manifest.rea_completed.path) -Raw|ConvertFrom-Json
 if($rea.status -ne 'passed' -or $rea.completed_plan_calls -ne 8 -or !$rea.closed_session_verified -or !$rea.process_exit_confirmed -or !$rea.owned_memory_profile.job_empty_confirmed -or !$rea.owned_memory_profile.sampler_exit_confirmed -or $rea.target.sha256_before -ne $manifest.original_engine_sha256 -or $rea.target.sha256_after -ne $manifest.original_engine_sha256){throw 'Unclosed or changed saved Engine evidence'}
}
Check-Inputs
$report=[ordered]@{started_utc=[DateTime]::UtcNow.ToString('o');status='running';manifest_sha256=$manifestHash;compiler_sha256=$compilerHash;source_pins=$manifest.sources;assembly_pins=$manifest.assemblies;evidence_pins=$manifest.evidence;steps=@();artifacts=@();failure=$null;compiles=2;new_scenarios=0;new_assertions=0;old_test_suites_rerun=0;old_executable_entrypoints_invoked=@();old_sources_recompiled=@();original_read=$false;original_executed=$false;backend_started=$false;system_library_loaded=$false;native_math_calls=0;physical_mouse=$false;action_submission_completed=$false;walking_accepted=$false;gameplay_accepted=$false;different_region_unsupported_rva='0x2247fb';support_boundary='Only Engine224770 null-region and same-region distance contract on existing shared coordinate cells. Different-region execution stops at2247FB. No Game wiring, movement submission, physical mouse, native runtime or gameplay acceptance.'}
New-Item -ItemType Directory -Path $output|Out-Null
function Run-Step([string]$name,[string]$program,[string[]]$arguments){
 $stdout=Join-Path $output ($name+'.log');$stderr=Join-Path $output ($name+'.stderr.log');$savedPreference=$ErrorActionPreference
 try{$ErrorActionPreference='Continue';& $program @arguments 1> $stdout 2> $stderr;$ok=$?;$code=$LASTEXITCODE}finally{$ErrorActionPreference=$savedPreference}
 $report.steps += [ordered]@{name=$name;program=$program;arguments=$arguments;invocation_succeeded=$ok;exit_code=$code}
 if(!$ok -or $code -ne 0){throw "Step $name failed (invocation=$ok, exit=$code); evidence retained"}
 if((Get-Item -LiteralPath $stderr).Length -ne 0 -or ($name.StartsWith('compile-') -and (Get-Item -LiteralPath $stdout).Length -ne 0)){throw ('Unexpected diagnostics: '+$name)}
}
try{
 foreach($entry in $manifest.assemblies){Copy-Item -LiteralPath (Under-Root $entry.path) -Destination (Join-Path $output $entry.name);Check-File (Join-Path $output $entry.name) $entry.sha256}
 $references=@($manifest.assemblies|ForEach-Object{'/reference:'+(Join-Path $output $_.name)})
 $library=Join-Path $output 'GrimDynasty.WorldDistance.dll';$tests=Join-Path $output 'WorldDistanceTests.exe'
 $flags=@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64')
 Run-Step 'compile-world-distance' $compiler ($flags+@('/target:library',"/out:$library")+$references+@(Under-Root $manifest.sources[0].path))
 Run-Step 'compile-world-distance-tests' $compiler ($flags+@('/target:exe','/main:WorldDistanceValidationProgram',"/out:$tests","/reference:$library")+$references+@(Under-Root $manifest.sources[1].path))
 Run-Step 'new-components' $tests @()
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-components.log') -Tail 1|ConvertFrom-Json
 if($summary.status -ne 'passed-new-world-distance-components-only' -or $summary.scenarios -ne $manifest.new_scenarios -or $summary.assertions -le 0 -or $summary.old_test_suites_rerun -ne 0 -or $summary.native_math_calls -ne 0 -or $summary.different_region_unsupported_rva -ne '0x2247fb'){throw 'Unexpected component summary'}
 foreach($flag in @('original_read','original_executed','backend_started','system_library_loaded','physical_mouse','action_submission_completed','walking_accepted','gameplay_accepted','null_constant_inferred')){if($summary.$flag -ne $false){throw ('Scope flag changed: '+$flag)}}
 $report.new_scenarios=$summary.scenarios;$report.new_assertions=$summary.assertions;$report['test_result']=$summary
 Check-Inputs;foreach($entry in $manifest.assemblies){Check-File (Join-Path $output $entry.name) $entry.sha256}
 $report.status='passed-new-world-distance-components-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}};$report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;new_scenarios=$report.new_scenarios;new_assertions=$report.new_assertions;old_test_suites_rerun=0;result=(Join-Path $output 'result.json')}|ConvertTo-Json
