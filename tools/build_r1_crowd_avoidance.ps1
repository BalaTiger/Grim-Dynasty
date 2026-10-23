param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$prior=Join-Path $taskRoot '.tooling\r1-crowd-growth-validation'
$priorHash='f9005adbc8c3becc734cbcf19d6f5e8563ccfc9a877e2b7e5944621f03956796'
$platform=Join-Path $taskRoot '.tooling\r1-system-ucrt-crowd-validation'
$platformHash='ab3431a1ffb442a9349730e81965138a7a98935a2f0efc5c33770ddd74064297'
$output=Join-Path $taskRoot '.tooling\r1-crowd-avoidance-validation'
$systemCrt='C:\Windows\System32\ucrtbase.dll'
$systemCrtHash='dedb7611e1abc1ff3852dcac3d4c93a048d729e25b061973b9969f22698f14ca'
$sources=@{
 'NavigationCrowdAvoidanceContracts.cs'='c6ea460f26a0c9c9b00de70ed7f712006ab4a8ad0b93da0dff9aa2fa0448c949'
 'NavigationCrowdAvoidanceTests.cs'='c48df36bd2692c0153307b073e9b8bd1d06c47b02ebdad75e621b459dad73e2f'
}
# Core NavigationView/Point live in NavigationNearest. CrowdState's public
# Group/Request type graph also references Adapter/Corridor/MeshTiles/TileData.
# Reuse only passed production DLLs, never any old test or resource-pipeline EXE.
$dependencyPins=@{
 'GrimDynasty.Actions.dll'='3ea34074db9d5a67d7dffb702eb053b703dc3b754d0db442aeadd71247e15c58'
 'GrimDynasty.NavigationNearest.dll'='75f303c482679df6ba2c8f01aad7759ba9ed322ad6386100525b690cd9b9dfd0'
 'GrimDynasty.NavigationMeshTiles.dll'='0b8604244b7681e6f0f5e13a63c93f2f815069bbe74109e974938c94d9ee4d57'
 'GrimDynasty.NavigationCorridor.dll'='1afd6850ae3f2fc1c0f13c6562ce6b5749a3ea98d3b2e5792d67234d4c779e33'
 'GrimDynasty.NavigationCrowdAdapter.dll'='04215144406cd87357b98bd32911f96281453c4010e4f2f61e1c6997b065b73e'
 'GrimDynasty.NavigationCrowdState.dll'='3c37f78adff8799df3d1da6624145d4cf1037f44416c7e550fa904c48ecf25e0'
 'GrimDynasty.NavigationTileData.dll'='b0459f94ca130467c2ec7f8c0379700250926c5a8eb8a3e6f617910a3bb02cd9'
}
$platformPins=@{'GrimDynasty.SystemUcrtRotationMath.dll'='2e82ebf9208f7bafe6a53bb469ba3cfaff9ac236b7faf124a58c759a8d38a85a'}
$evidence=@{
 'docs\reverse-client\r1-crowd-motion-branches\20-CrowdMotionBranch-0x18025f250-procedure_assembly.json'='4ccf1f9256b2ab72f5367398a2717d6316e22ca769104f96695dccb90cac9b57'
 'docs\reverse-client\r1-crowd-direct-algorithms\11-CrowdDirectAlgorithm-0x18025ef20-procedure_assembly.json'='f600da86dd2159b8e4bf691f2f3b8e7b31caf6f360766a445d09ad5595d48bdf'
 'docs\reverse-client\r1-crowd-tick-operands\inspection.json'='a61c64361ffb10912b26e103456f12fc1b26b7434f9d1c17b5dac9c2ee112529'
 'docs\reverse-client\r1-engine-crowd-math-imports\inspection.json'='f0fa298bf90996afdd7fb04a898a7c037827d3c858b33fb4515c19d8db5e4a02'
 'docs\reverse-client\r1-system-ucrt-crowd-profile.json'='535be8051b9f605b6a210c1ff857adb68e9a720c6a2bc60aa85299cee1104b15'
 'docs\reverse-client\r1-mesh-connection-constants\inspection.json'='e3dab5d3d29a21bbd3c25f71c1bb51831638d0d1414c0be3e5394ba5e5edd0fc'
 'docs\reverse-client\r1-crowd-motion-bindings\inspection.json'='cefbf931ba37144e6400be0d7a0c3c894f9a82828c19723d78be440a729334d3'
 'docs\reverse-client\r1-navigation-constants\inspection.json'='16c88b4a9126a4752e2575e8dc02c4b87f9f25655660ff7289c69a4132fc754b'
 'docs\reverse-client\r1-navigation-compressor-slot\inspection.json'='191e63e5cc34d9b1abf1326426827a27041743913768a3159e73af3dc62126de'
 'docs\reverse-client\r1-region-bounds-mask\inspection.json'='d3c63d6c8ed7c470da6a01b88146c75cecd8d91abfb654771be1e9961ea0fb75'
 'docs\object-lifecycle\offline-actor-remaining-dependencies\inspection.json'='6639fbb457720057edb6ea37834e6f5aa2534593b26020fda7d83044b73325c5'
}
function Hash([string]$path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
function Plain([string]$path){
 $cursor=[IO.Path]::GetFullPath($path)
 while($cursor){if(Test-Path -LiteralPath $cursor){$item=Get-Item -LiteralPath $cursor;if($item.Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Redirected path: '+$cursor)}};$parent=[IO.Directory]::GetParent($cursor);if($null -eq $parent){break};$cursor=$parent.FullName}
}
function Check-File([string]$path,[string]$expected){Plain $path;$item=Get-Item -LiteralPath $path;if($expected -notmatch '^[0-9a-f]{64}$' -or $item.PSIsContainer -or (Hash $path) -ne $expected){throw ('Unfrozen input or changed identity: '+$path)}}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace and no arguments required'}
Plain $output
if(!([IO.Path]::GetFullPath($output)).StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Output must stay in workspace'}
if(Test-Path -LiteralPath $output){throw 'Preserve existing result; output must be new'}
$priorResult=Join-Path $prior 'result.json';$platformResult=Join-Path $platform 'result.json'
Check-File $priorResult $priorHash;Check-File $platformResult $platformHash
$old=Get-Content -LiteralPath $priorResult -Raw|ConvertFrom-Json
$math=Get-Content -LiteralPath $platformResult -Raw|ConvertFrom-Json
if($old.status -ne 'passed-new-crowd-growth-and-affected-state-proximity-regressions-only' -or $null -ne $old.failure){throw 'Passed crowd production dependency provenance required'}
if($math.status -ne 'passed-new-system-ucrt-crowd-and-affected-rotation-only' -or !$math.own_crowd_platform -or !$math.rotation_bounds_unchanged -or $null -ne $math.failure){throw 'Passed concrete system-UCRT crowd profile required'}
foreach($name in $dependencyPins.Keys){$records=@($old.artifacts|Where-Object {$_.name -eq $name});if($records.Count -ne 1 -or $records[0].sha256 -ne $dependencyPins[$name]){throw ('Dependency identity absent: '+$name)}}
foreach($name in $platformPins.Keys){$records=@($math.artifacts|Where-Object {$_.name -eq $name});if($records.Count -ne 1 -or $records[0].sha256 -ne $platformPins[$name]){throw ('Passed platform identity absent: '+$name)}}
function Check-Inputs {
 Check-File $compiler $compilerHash;Check-File $priorResult $priorHash;Check-File $platformResult $platformHash;Check-File $systemCrt $systemCrtHash
 $crt=Get-Item -LiteralPath $systemCrt;if($crt.Length -ne 1399656 -or $crt.VersionInfo.FileVersion -ne '10.0.26100.9711 (WinBuild.160101.0800)'){throw 'Own UCRT version/size changed'}
 foreach($name in $sources.Keys){Check-File (Join-Path $taskRoot ('src\reverse_client_actions\'+$name)) $sources[$name]}
 foreach($name in $dependencyPins.Keys){Check-File (Join-Path $prior $name) $dependencyPins[$name]}
 foreach($name in $platformPins.Keys){Check-File (Join-Path $platform $name) $platformPins[$name]}
 foreach($name in $evidence.Keys){Check-File (Join-Path $taskRoot $name) $evidence[$name]}
}
Check-Inputs
$operands=Get-Content -LiteralPath (Join-Path $taskRoot 'docs\reverse-client\r1-crowd-tick-operands\inspection.json') -Raw|ConvertFrom-Json
if($operands.status -ne 'passed-with-unbacked-ranges' -or !$operands.stable -or !$operands.source_closed -or $operands.sha256_before -ne '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153' -or $operands.sha256_after -ne $operands.sha256_before){throw 'Fixed original evidence is not stable and closed'}
if($operands.global_runtime_values_observed -ne $false -or $operands.unbacked_ranges.Count -ne 2){throw 'BSS values must remain unobserved'}
foreach($range in $operands.unbacked_ranges){if($range.classification -ne 'NoDiskBytes' -or $range.observed_bytes -ne 0 -or $range.value_inferred -ne $false){throw 'No fabricated BSS initial value permitted'}}
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash
 source_pins=$sources;dependency_pins=$dependencyPins;platform_pins=$platformPins;evidence_pins=$evidence
 prior_result=$priorResult;prior_result_sha256=$priorHash;platform_result=$platformResult;platform_result_sha256=$platformHash
 system_crt_path=$systemCrt;system_crt_file_sha256=$systemCrtHash;system_crt_file_version='10.0.26100.9711 (WinBuild.160101.0800)'
 original_evidence_sha256=$operands.sha256_before;steps=@();test_results=@();artifacts=@();failure=$null
 old_executable_entrypoints_invoked=@();old_test_suites_rerun=@();old_sources_recompiled=@()
 system_library_load_requested=$false;system_library_loaded=$null;native_math_calls=$null;floating_control_write_mask=0;memory_image_hashed=$false
 bss_defaults_inferred=$false;original_read=$false;original_executed=$false;backend_started=$false;security_settings_changed=$false
 isolated_avoidance_selector_validated=$false;tick_executed=$false;character_movement_validated=$false;gameplay_accepted=$false
 support_boundary='Own shared Group.Vector80 header and stride64 records, exact finite normal-or-zero25EF20 scoring and25F250 preprocessing/22-candidate three-refinement selection using fixed concrete system UCRT crowd view. BSS fallback stays unknown unless explicitly supplied by a caller. No live original, real tick, movement/floor/publication, arbitrary native byte aliases, native exceptions, nondefault FP control, original CRT equivalence or game acceptance.'
}
New-Item -ItemType Directory -Path $output|Out-Null
function Run-Step([string]$name,[string]$program,[string[]]$arguments){
 $stdout=Join-Path $output ($name+'.log');$stderr=Join-Path $output ($name+'.stderr.log');$savedPreference=$ErrorActionPreference
 try{$ErrorActionPreference='Continue';& $program @arguments 1> $stdout 2> $stderr;$ok=$?;$code=$LASTEXITCODE}finally{$ErrorActionPreference=$savedPreference}
 $report.steps += [ordered]@{name=$name;program=$program;arguments=$arguments;invocation_succeeded=$ok;exit_code=$code}
 if(!$ok -or $code -ne 0){throw "Step $name failed (invocation=$ok, exit=$code); logs preserved"}
 if((Get-Item -LiteralPath $stderr).Length -ne 0 -or ($name.StartsWith('compile-') -and (Get-Item -LiteralPath $stdout).Length -ne 0)){throw ('Unexpected diagnostics: '+$name)}
}
try{
 foreach($name in $dependencyPins.Keys){Copy-Item -LiteralPath (Join-Path $prior $name) -Destination (Join-Path $output $name);Check-File (Join-Path $output $name) $dependencyPins[$name]}
 foreach($name in $platformPins.Keys){Copy-Item -LiteralPath (Join-Path $platform $name) -Destination (Join-Path $output $name);Check-File (Join-Path $output $name) $platformPins[$name]}
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$library=Join-Path $output 'GrimDynasty.NavigationCrowdAvoidance.dll';$tests=Join-Path $output 'NavigationCrowdAvoidanceTests.exe'
 $references=@();foreach($name in $dependencyPins.Keys){$references += '/reference:'+(Join-Path $output $name)};foreach($name in $platformPins.Keys){$references += '/reference:'+(Join-Path $output $name)}
 Run-Step 'compile-crowd-avoidance' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@((Join-Path $source 'NavigationCrowdAvoidanceContracts.cs')))
 Run-Step 'compile-new-crowd-avoidance-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:NavigationCrowdAvoidanceTests','/reference:System.Web.Extensions.dll',"/out:$tests","/reference:$library")+$references+@((Join-Path $source 'NavigationCrowdAvoidanceTests.cs')))
 $report.system_library_load_requested=$true
 Run-Step 'new-crowd-avoidance-tests' $tests @()
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-crowd-avoidance-tests.log') -Tail 1|ConvertFrom-Json
 if($summary.status -ne 'passed' -or $summary.scenarios -ne 29 -or $summary.scoring_preprocess_scenarios -ne 17 -or $summary.selector_scenarios -ne 12 -or $summary.assertions -le 0 -or $summary.native_math_calls -ne 345){throw 'Unexpected29-case avoidance summary or actual math call count'}
 if(!$summary.own_crowd_platform -or $summary.library_sha256 -ne $systemCrtHash -or $summary.control_mask_written -ne 0 -or $summary.control_before -ne $summary.control_after -or $summary.memory_image_hashed -ne $false){throw 'Fixture violated fixed platform/query-only FP boundary'}
 foreach($name in @('bss_defaults_inferred','original_read','original_executed','tick_executed','character_movement_validated','gameplay_accepted','original_runtime_equivalence')){if($summary.$name -ne $false){throw ('Fixture exceeded support boundary: '+$name)}}
 $report.test_results += [ordered]@{name='new-crowd-avoidance-tests';kind='new';scenarios=$summary.scenarios;assertions=$summary.assertions;scoring_preprocess_scenarios=17;selector_scenarios=12;native_math_calls=$summary.native_math_calls;control_before=$summary.control_before;control_after=$summary.control_after;exports=$summary.exports}
 $report.system_library_loaded=$true;$report.native_math_calls=$summary.native_math_calls
 Check-Inputs;foreach($name in $dependencyPins.Keys){Check-File (Join-Path $output $name) $dependencyPins[$name]};foreach($name in $platformPins.Keys){Check-File (Join-Path $output $name) $platformPins[$name]}
 $report.isolated_avoidance_selector_validated=$true;$report.status='passed-new-crowd-avoidance-selector-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}};$report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;test_results=$report.test_results;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 7
