param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$manifestPath=Join-Path $taskRoot 'tools\r1_skill_reference_number_build_fingerprints.json'
$manifestHash='dcc394d5c65e5c9fbc805c5c532e9ef26e1dae2d7882dd4979e5b72d68a1aa68'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$output=Join-Path $taskRoot '.tooling\r1-skill-reference-number-validation'
function Hash([string]$p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-Ancestors([string]$p){$cursor=[IO.Path]::GetFullPath($p);while($cursor){if(Test-Path -LiteralPath $cursor){if((Get-Item -LiteralPath $cursor).Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Reparse path rejected: '+$cursor)}};$parent=[IO.Directory]::GetParent($cursor);if($null -eq $parent){break};$cursor=$parent.FullName}}
function Under-Root([string]$relative){$p=[IO.Path]::GetFullPath((Join-Path $taskRoot $relative));if(!$p.StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Manifest path leaves workspace'};return $p}
function Check-File([string]$p,[string]$expected){Check-Ancestors $p;if($expected -notmatch '^[0-9a-f]{64}$'){throw ('Unfrozen input: '+$p)};if((Get-Item -LiteralPath $p).PSIsContainer -or (Hash $p) -ne $expected){throw ('Input identity changed: '+$p)}}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace; no arguments'}
Check-File $manifestPath $manifestHash
$manifest=Get-Content -LiteralPath $manifestPath -Raw|ConvertFrom-Json
if($manifest.schema -ne 'grim-dynasty.fixed-skill-reference-number-build.v1' -or $manifest.compiles -ne 2 -or $manifest.sources.Count -ne 2 -or $manifest.assemblies.Count -ne 9){throw 'Unexpected fixed plan'}
if(($manifest.sources.path -join '|') -ne 'src/reverse_client_actions/SkillReferenceNumberContracts.cs|src/reverse_client_actions/SkillReferenceNumberTests.cs'){throw 'Unexpected source partition'}
foreach($entry in $manifest.assemblies){if($entry.name -ne [IO.Path]::GetFileName($entry.name) -or !$entry.name.EndsWith('.dll')){throw 'Only fixed flat managed DLL dependencies'}}
Check-Ancestors $output
if(Test-Path -LiteralPath $output){throw 'Existing output preserved; no overwrite/retry'}
function Check-Inputs {
 Check-File $compiler $compilerHash;Check-File $manifestPath $manifestHash
 foreach($entry in @($manifest.sources)+@($manifest.evidence)+@($manifest.unchanged_dependency_sources)){Check-File (Under-Root $entry.path) $entry.sha256}
 Check-File (Under-Root $manifest.prerequisite.path) $manifest.prerequisite.sha256
 $prior=Get-Content -LiteralPath (Under-Root $manifest.prerequisite.path) -Raw|ConvertFrom-Json
 if($prior.status -ne 'passed-new-skill-reference-components-only' -or $prior.scenarios -ne 6 -or $prior.assertions -ne 76 -or $null -ne $prior.failure){throw 'SkillReference dependency receipt is not passed'}
 foreach($entry in $manifest.unchanged_dependency_sources){$record=@($prior.source_pins|Where-Object{$_.path -eq $entry.path -and $_.sha256 -eq $entry.sha256});if($record.Count -ne 1){throw 'Existing shared source identity changed'}}
 foreach($entry in $manifest.assemblies){Check-File (Under-Root $entry.path) $entry.sha256;$record=@($prior.artifacts|Where-Object{$_.name -eq $entry.name -and $_.sha256 -eq $entry.sha256});if($record.Count -ne 1){throw ('Dependency artifact receipt mismatch: '+$entry.name)}}
 Check-File (Under-Root $manifest.rea_completed.path) $manifest.rea_completed.sha256
 $rea=Get-Content -LiteralPath (Under-Root $manifest.rea_completed.path) -Raw|ConvertFrom-Json
 if($rea.status -ne 'passed' -or $rea.completed_plan_calls -ne 18 -or !$rea.closed_session_verified -or !$rea.process_exit_confirmed -or !$rea.owned_memory_profile.job_empty_confirmed -or !$rea.owned_memory_profile.sampler_exit_confirmed -or $rea.target.sha256_before -ne $manifest.original_game_sha256 -or $rea.target.sha256_after -ne $manifest.original_game_sha256){throw 'Unclosed or changed saved movement-action evidence'}
}
Check-Inputs
$report=[ordered]@{started_utc=[DateTime]::UtcNow.ToString('o');status='running';manifest_sha256=$manifestHash;compiler_sha256=$compilerHash;source_pins=$manifest.sources;assembly_pins=$manifest.assemblies;evidence_pins=$manifest.evidence;steps=@();artifacts=@();failure=$null;compiles=2;new_scenarios=0;new_assertions=0;old_test_suites_rerun=0;old_executable_entrypoints_invoked=@();old_sources_recompiled=@();original_read=$false;original_executed=$false;backend_started=$false;physical_mouse=$false;action_submitted=$false;gameplay_accepted=$false;support_boundary='Only bounded531A30 skill-ID to reference search using the existing same-owner five vectors and exact ObjectId getter. Existing531C50 source and DLL are unchanged; new two-way consistency checks only. No action allocation/dispatch, constructor recovery, physical mouse or gameplay acceptance.'}
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
 $library=Join-Path $output 'GrimDynasty.SkillReferenceNumber.dll';$tests=Join-Path $output 'SkillReferenceNumberTests.exe'
 Run-Step 'compile-skill-reference-number' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@(Under-Root $manifest.sources[0].path))
 Run-Step 'compile-skill-reference-number-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:SkillReferenceNumberValidationProgram',"/out:$tests","/reference:$library")+$references+@(Under-Root $manifest.sources[1].path))
 Run-Step 'new-components' $tests @()
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-components.log') -Tail 1|ConvertFrom-Json
 if($summary.status -ne 'passed-new-skill-reference-number-components-only' -or $summary.scenarios -ne $manifest.new_scenarios -or $summary.assertions -le 0 -or $summary.old_test_suites_rerun -ne 0 -or $summary.native_math_calls -ne 0){throw 'Unexpected component summary'}
 foreach($flag in @('original_read','original_executed','backend_started','physical_mouse','action_submitted','system_library_loaded')){if($summary.$flag -ne $false){throw ('Scope flag changed: '+$flag)}}
 $report.new_scenarios=$summary.scenarios;$report.new_assertions=$summary.assertions;$report['test_result']=$summary
 Check-Inputs;foreach($entry in $manifest.assemblies){Check-File (Join-Path $output $entry.name) $entry.sha256}
 $report.status='passed-new-skill-reference-number-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}};$report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;new_scenarios=$report.new_scenarios;new_assertions=$report.new_assertions;old_test_suites_rerun=0;result=(Join-Path $output 'result.json')}|ConvertTo-Json
