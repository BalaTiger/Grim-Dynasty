param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$manifestPath=Join-Path $taskRoot 'tools\r1_crowd_tick_production_build_fingerprints.json'
$manifestHash='4542bfb1ed7d37e572e167759ae4d8aeca92ec845c06498c671a770e27006bd5'
$output=Join-Path $taskRoot '.tooling\r1-crowd-tick-production-validation'
function Hash([string]$p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-Ancestors([string]$p){
 $cursor=[IO.Path]::GetFullPath($p)
 while($cursor){if(Test-Path -LiteralPath $cursor){$item=Get-Item -LiteralPath $cursor;if($item.Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Reparse path not permitted: '+$cursor)}};$parent=[IO.Directory]::GetParent($cursor);if($null -eq $parent){break};$cursor=$parent.FullName}
}
function Check-File([string]$p,[string]$expected){Check-Ancestors $p;if($expected -notmatch '^[0-9a-f]{64}$'){throw ('Unfrozen input: '+$p)};$item=Get-Item -LiteralPath $p;if($item.PSIsContainer -or (Hash $p) -ne $expected){throw ('Input identity changed: '+$p)}}
function Under-Root([string]$relative){$p=[IO.Path]::GetFullPath((Join-Path $taskRoot $relative));if(!$p.StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Manifest path leaves workspace'};return $p}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace; no arguments'}
Check-File $manifestPath $manifestHash
$manifest=Get-Content -LiteralPath $manifestPath -Raw|ConvertFrom-Json
if($manifest.schema -ne 'grim-dynasty.fixed-crowd-tick-production-build.v1' -or $manifest.compiles -ne 2 -or $manifest.new_scenarios -ne 22 -or $manifest.saved_resource_scenarios -ne 1){throw 'Unexpected fixed plan'}
Check-Ancestors $output
if(!( [IO.Path]::GetFullPath($output)).StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Output outside workspace'}
if(Test-Path -LiteralPath $output){throw 'Existing output preserved; no overwrite/retry'}
function Check-Inputs {
 Check-File $compiler $compilerHash;Check-File $manifestPath $manifestHash;Check-Ancestors $output
 foreach($entry in $manifest.sources){Check-File (Under-Root $entry.path) $entry.sha256}
 foreach($entry in $manifest.evidence){Check-File (Under-Root $entry.path) $entry.sha256}
 foreach($entry in $manifest.prerequisites){Check-File (Under-Root $entry.path) $entry.sha256;$prior=Get-Content -LiteralPath (Under-Root $entry.path) -Raw|ConvertFrom-Json;if($prior.status -ne $entry.status -or $null -ne $prior.failure){throw ('Prerequisite no longer passed: '+$entry.path)}}
 foreach($entry in $manifest.dependencies){Check-File (Under-Root $entry.path) $entry.sha256;$prior=Get-Content -LiteralPath (Under-Root $entry.result) -Raw|ConvertFrom-Json;$record=@($prior.artifacts|Where-Object {$_.name -eq $entry.name -and $_.sha256 -eq $entry.sha256});if($record.Count -ne 1){throw ('Dependency not uniquely backed by passed result: '+$entry.name)}}
 Check-File (Under-Root $manifest.saved_member.path) $manifest.saved_member.sha256;if((Get-Item -LiteralPath (Under-Root $manifest.saved_member.path)).Length -ne $manifest.saved_member.bytes){throw 'Saved member extent changed'}
}
Check-Inputs
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash
 manifest=$manifestPath;manifest_sha256=$manifestHash;source_pins=$manifest.sources;prerequisites=$manifest.prerequisites;dependency_pins=$manifest.dependencies;evidence_pins=$manifest.evidence
 steps=@();test_results=@();sample_result=$null;artifacts=@();failure=$null
 old_test_suites_rerun=@();old_executable_entrypoints_invoked=@();compiles=2
 production_reused=@('MainmenuCrowdRequestPipeline.BuildExplicitRequestContext','MainmenuTilePipeline.BuildExplicitColdTiles','NavigationCrowdPathContracts.StraightPath030900','NavigationCrowdProximityContracts','NavigationCrowdAvoidanceContracts.Select25F250','NavigationCrowdAdapterContracts.Project030B30')
 original_read=$false;original_executed=$false;saved_resource_read=$true;backend_started=$false;security_settings_changed=$false;ucrt_control_changed=$false
 explicit_own_globals=$true;loaded_bss_inferred=$false;full_tick=$false;physical_mouse=$false;ground_floor=$false;callback_executed=$false;character_movement_validated=$false;gameplay_accepted=$false
 support_boundary='Reached one-Work/one-Member conditional producer, real proximity and waypoint steering, real avoidance/UCRT, real Position48 writes and navigation projection. Known OWN BSS/configuration only; Unknown fails with retained prefix. Stops before real066EE0 at25E087. Earlier whole-tick prerequisites, collisions with neighbors, floor/frame/publication and physical mouse remain unverified.'
}
New-Item -ItemType Directory -Path $output|Out-Null
function Run-Step([string]$name,[string]$program,[string[]]$arguments){
 $stdout=Join-Path $output ($name+'.log');$stderr=Join-Path $output ($name+'.stderr.log');$savedPreference=$ErrorActionPreference
 try{$ErrorActionPreference='Continue';& $program @arguments 1> $stdout 2> $stderr;$ok=$?;$code=$LASTEXITCODE}finally{$ErrorActionPreference=$savedPreference}
 $report.steps += [ordered]@{name=$name;program=$program;arguments=$arguments;invocation_succeeded=$ok;exit_code=$code}
 if(!$ok -or $code -ne 0){throw "Step $name failed (invocation=$ok, exit=$code); evidence retained"}
 if((Get-Item -LiteralPath $stderr).Length -ne 0 -or ($name.StartsWith('compile-') -and (Get-Item -LiteralPath $stdout).Length -ne 0)){throw ('Unexpected diagnostics: '+$name)}
}
function Read-Summary([string]$name,[int]$expected){$summary=Get-Content -LiteralPath (Join-Path $output ($name+'.log')) -Tail 1|ConvertFrom-Json;if($summary.status -ne 'passed-conditional-position-production-not-full-tick' -or $summary.scenarios -ne $expected -or $summary.assertions -le 0){throw ('Unexpected new-suite summary: '+$name)};$report.test_results += [ordered]@{name=$name;kind='new';scenarios=$summary.scenarios;assertions=$summary.assertions};return $summary}
try{
 foreach($entry in $manifest.dependencies){Copy-Item -LiteralPath (Under-Root $entry.path) -Destination (Join-Path $output $entry.name);Check-File (Join-Path $output $entry.name) $entry.sha256}
 $references=@();foreach($entry in $manifest.dependencies){$references += '/reference:'+(Join-Path $output $entry.name)}
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$library=Join-Path $output 'GrimDynasty.NavigationCrowdTickProduction.dll';$tests=Join-Path $output 'NavigationCrowdTickProductionTests.exe'
 Run-Step 'compile-crowd-tick-production' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@((Join-Path $source 'NavigationCrowdTickProductionContracts.cs')))
 Run-Step 'compile-new-tick-production-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/define:SAVED_RESOURCE_PIPELINE','/reference:System.Web.Extensions.dll','/main:NavigationCrowdTickProductionTests',"/out:$tests","/reference:$library")+$references+@((Join-Path $source 'NavigationCrowdTickProductionTests.cs')))
 Run-Step 'new-tick-production-tests' $tests @();$summary=Read-Summary 'new-tick-production-tests' 22
 Run-Step 'real-saved-resource-position-production' $tests @('--saved-resource');$sample=Read-Summary 'real-saved-resource-position-production' 1
 $s=$sample.sample
 if($null -eq $s -or $s.tiles -ne 9 -or $s.corridor -lt 2 -or $s.tile_crossings -lt 1 -or !$s.same_agent -or !$s.actual_position_changed -or $s.projection -ne '030B30-success' -or $s.reference -eq 0 -or $s.stop -ne '25E087'){throw 'Saved-resource actual position/projection prefix not established'}
 if($s.before.Count -ne 3 -or $s.after.Count -ne 3 -or ($s.before[0] -eq $s.after[0] -and $s.before[2] -eq $s.after[2])){throw 'Saved sample did not change actual XZ position'}
 foreach($name in @('physical_mouse','ground_floor','full_tick')){if($s.$name -ne $false){throw ('Sample exceeded support boundary: '+$name)}}
 $report.sample_result=$s;Check-Inputs
 foreach($entry in $manifest.dependencies){Check-File (Join-Path $output $entry.name) $entry.sha256}
 $report.status='passed-new-conditional-crowd-position-production-and-saved-resource-prefix-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}};$report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;test_results=$report.test_results;sample_result=$report.sample_result;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 7
