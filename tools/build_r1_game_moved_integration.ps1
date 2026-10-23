param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$jsonAssembly='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Web.Extensions.dll'
$jsonHash='5e6378edc06f5a61920aaadb2aba839dd3cf302d6192e08aacd4454ba4cff0b3'
$systemCrt='C:\Windows\System32\ucrtbase.dll'
$systemCrtHash='dedb7611e1abc1ff3852dcac3d4c93a048d729e25b061973b9969f22698f14ca'
$manifestPath=Join-Path $taskRoot 'tools\r1_game_moved_integration_build_fingerprints.json'
$manifestHash='1117422c3b68006a47d37f76d6b7afbbbab531e03587b916597aad4a39bd9b79'
$output=Join-Path $taskRoot '.tooling\r1-game-moved-integration-validation'
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
if($manifest.schema -ne 'grim-dynasty.fixed-game-moved-integration-build.v1' -or $manifest.compiles -ne 2 -or $manifest.sources.Count -ne 5 -or $manifest.skill_scenarios -ne 8 -or $manifest.saved_resource_scenarios -ne 1){throw 'Unexpected fixed plan'}
Check-Ancestors $output
if(!([IO.Path]::GetFullPath($output)).StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Output outside workspace'}
if(Test-Path -LiteralPath $output){throw 'Existing output preserved; no overwrite/retry'}
function Check-Inputs {
 Check-File $compiler $compilerHash;Check-File $jsonAssembly $jsonHash;Check-File $systemCrt $systemCrtHash;Check-File $manifestPath $manifestHash
 $crt=Get-Item -LiteralPath $systemCrt;if($crt.Length -ne 1399656 -or $crt.VersionInfo.FileVersion -ne '10.0.26100.9711 (WinBuild.160101.0800)'){throw 'Own UCRT version/size changed'}
 foreach($entry in $manifest.sources){Check-File (Under-Root $entry.path) $entry.sha256}
 foreach($entry in $manifest.evidence){Check-File (Under-Root $entry.path) $entry.sha256}
 foreach($entry in $manifest.prerequisites){Check-File (Under-Root $entry.path) $entry.sha256;$prior=Get-Content -LiteralPath (Under-Root $entry.path) -Raw|ConvertFrom-Json;if($prior.status -ne $entry.status -or $null -ne $prior.failure){throw ('Prerequisite no longer passed: '+$entry.path)}}
 foreach($entry in $manifest.dependencies){Check-File (Under-Root $entry.path) $entry.sha256;$prior=Get-Content -LiteralPath (Under-Root $entry.result) -Raw|ConvertFrom-Json;$record=@($prior.artifacts|Where-Object {$_.name -eq $entry.name -and $_.sha256 -eq $entry.sha256});if($record.Count -ne 1){throw ('Dependency not uniquely backed by passed result: '+$entry.name)}}
}
Check-Inputs
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash
 manifest=$manifestPath;manifest_sha256=$manifestHash;source_pins=$manifest.sources;prerequisites=$manifest.prerequisites;dependency_pins=$manifest.dependencies;evidence_pins=$manifest.evidence;dependency_selections=$manifest.dependency_selections
 steps=@();test_results=@();sample_result=$null;artifacts=@();failure=$null;compiles=2
 old_test_suites_rerun=@();old_executable_entrypoints_invoked=@();original_read=$false;original_executed=$false;backend_started=$false;security_settings_changed=$false;ucrt_control_changed=$false
 system_library_load_requested=$false;system_library_loaded=$false;native_math_calls=0
 real_saved_ground_floor=$false;real_crowd_callback_continuation=$false;full_tick=$false;physical_mouse=$false;native_scene_startup=$false;native_loaded_skill_collections_known=$false;gameplay_accepted=$false
 support_boundary='Frozen one-Work Crowd production resumes only its exact known25E087 boundary diagnostic plus canonical Agent/owner and observed table rechecks; calls actual066EE0 ordinary branch, sameRegion22C690/TranslateTrue, actual saved Ground Floor, existing05E160 UCRT rotation and same Entity bbox/spatial publication, then533410 with explicit OWN empty collections. The original integer tick argument and own scene/parameters are explicit. Full tick tail, original game runtime, native map actor/neighbor/skill population, physical mouse, stale pause query, special13/15 movement and nonempty skill target1B0 remain unclosed.'
}
New-Item -ItemType Directory -Path $output|Out-Null
function Run-Step([string]$name,[string]$program,[string[]]$arguments){
 $stdout=Join-Path $output ($name+'.log');$stderr=Join-Path $output ($name+'.stderr.log');$savedPreference=$ErrorActionPreference
 try{$ErrorActionPreference='Continue';& $program @arguments 1> $stdout 2> $stderr;$ok=$?;$code=$LASTEXITCODE}finally{$ErrorActionPreference=$savedPreference}
 $report.steps += [ordered]@{name=$name;program=$program;arguments=$arguments;invocation_succeeded=$ok;exit_code=$code}
 if(!$ok -or $code -ne 0){throw "Step $name failed (invocation=$ok, exit=$code); evidence retained"}
 if((Get-Item -LiteralPath $stderr).Length -ne 0 -or ($name.StartsWith('compile-') -and (Get-Item -LiteralPath $stdout).Length -ne 0)){throw ('Unexpected diagnostics: '+$name)}
}
function Read-Summary([string]$name,[string]$status,[int]$expected){$summary=Get-Content -LiteralPath (Join-Path $output ($name+'.log')) -Tail 1|ConvertFrom-Json;if($summary.status -ne $status -or $summary.scenarios -ne $expected -or $summary.assertions -le 0){throw ('Unexpected new-suite summary: '+$name)};$report.test_results += [ordered]@{name=$name;kind='new';scenarios=$summary.scenarios;assertions=$summary.assertions};return $summary}
try{
 foreach($entry in $manifest.dependencies){Copy-Item -LiteralPath (Under-Root $entry.path) -Destination (Join-Path $output $entry.name);Check-File (Join-Path $output $entry.name) $entry.sha256}
 $references=@();foreach($entry in $manifest.dependencies){$references += '/reference:'+(Join-Path $output $entry.name)}
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$library=Join-Path $output 'GrimDynasty.GameMoved.dll';$tests=Join-Path $output 'GameMovedIntegrationTests.exe'
 Run-Step 'compile-game-moved' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@((Join-Path $source 'SkillMovedContracts.cs'),(Join-Path $source 'GameMovedContracts.cs'),(Join-Path $source 'GameMovedReachedContinuationContracts.cs')))
 Run-Step 'compile-game-moved-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:GameMovedIntegrationTests',"/out:$tests","/reference:$library","/reference:$jsonAssembly")+$references+@((Join-Path $source 'SkillMovedTests.cs'),(Join-Path $source 'GameMovedIntegrationTests.cs')))
 Run-Step 'new-skill-moved-tests' $tests @('--skills');$skill=Read-Summary 'new-skill-moved-tests' 'passed-skill-moved' $manifest.skill_scenarios
 $report.system_library_load_requested=$true
 Run-Step 'new-game-moved-tests' $tests @();$component=Read-Summary 'new-game-moved-tests' 'passed-game-moved-component' $manifest.component_scenarios
 if($component.native_math_calls -le 0){throw 'New Game component did not exercise actual UCRT rotation'}
 Run-Step 'new-real-crowd-game-moved' $tests @('--saved-resource');$saved=Read-Summary 'new-real-crowd-game-moved' 'passed-real-crowd-game-moved' $manifest.saved_resource_scenarios
 $s=$saved.sample
 if($null -eq $s -or $s.tile_count -ne 9 -or $s.corridor_count -lt 2 -or $s.tile_crossings -lt 1 -or $s.native_math_calls -le 0){throw 'Actual saved corridor or new UCRT path not established'}
 foreach($name in @('same_agent','actual_position_changed','floor_succeeded','rotation_returned','notification_returned','callback_returned')){if($s.$name -ne $true){throw ('New saved callback did not establish '+$name)}}
 foreach($name in @('nav_height_used','full_tick')){if($s.$name -ne $false){throw ('Saved callback exceeded boundary '+$name)}}
 $report.sample_result=$s;$report.system_library_loaded=$true;$report.native_math_calls=$component.native_math_calls+$s.native_math_calls
 Check-Inputs;foreach($entry in $manifest.dependencies){Check-File (Join-Path $output $entry.name) $entry.sha256}
 $report.real_saved_ground_floor=$true;$report.real_crowd_callback_continuation=$true;$report.status='passed-new-game-moved-and-real-crowd-callback-continuation-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}};$report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;test_results=$report.test_results;sample_result=$report.sample_result;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 8

