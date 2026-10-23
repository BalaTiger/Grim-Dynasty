param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$prior=Join-Path $taskRoot '.tooling\r1-system-ucrt-rotation-validation-2'
$priorHash='ca025983c2731fedf773a0235cfeaa1152e03388c22bed80e716c7b15dab67a1'
$output=Join-Path $taskRoot '.tooling\r1-system-ucrt-crowd-validation'
$systemCrt='C:\Windows\System32\ucrtbase.dll'
$systemCrtHash='dedb7611e1abc1ff3852dcac3d4c93a048d729e25b061973b9969f22698f14ca'
$sources=@{
 'SystemUcrtRotationMath.cs'='a35ffee6cfe3f70fe4205767bd032fcbfa1ccc71eae9bc5e8b20fa461c60d639'
 'SystemUcrtCrowdMathTests.cs'='5341ca60dd713ea76a2fab332625d8263f699e3411835ccd96ee9e8946cbf116'
 'SystemUcrtRotationMathTests.cs'='24809d601d4241c3a5cb6aa87eef2378c1e0f5472ff7bbb337bb154244a5a233'
}
$dependencyPins=@{'GrimDynasty.Actions.dll'='3ea34074db9d5a67d7dffb702eb053b703dc3b754d0db442aeadd71247e15c58'}
$evidence=@{
 'docs\reverse-client\r1-engine-crowd-math-imports\inspection.json'='f0fa298bf90996afdd7fb04a898a7c037827d3c858b33fb4515c19d8db5e4a02'
 'docs\reverse-client\r1-system-ucrt-crowd-profile.json'='535be8051b9f605b6a210c1ff857adb68e9a720c6a2bc60aa85299cee1104b15'
 'docs\reverse-client\r1-system-ucrt-crowd-prior\SystemUcrtRotationMath.cs'='3e1456e9ec9be5349632724c80aa5802dad6b916e36e2220f820ac8cda7de9d0'
 'docs\reverse-client\r1-crowd-motion-branches\20-CrowdMotionBranch-0x18025f250-procedure_assembly.json'='4ccf1f9256b2ab72f5367398a2717d6316e22ca769104f96695dccb90cac9b57'
 'docs\reverse-client\r1-crowd-tick-operands\inspection.json'='a61c64361ffb10912b26e103456f12fc1b26b7434f9d1c17b5dac9c2ee112529'
 'docs\reverse-client\r1-system-ucrt-rotation-profile.json'='def13e5755f5969dd042a4e058a73a38b77c5e5fa69574a4e246a32bb81d41e7'
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
$priorResult=Join-Path $prior 'result.json';Check-File $priorResult $priorHash
$old=Get-Content -LiteralPath $priorResult -Raw|ConvertFrom-Json
if($old.status -ne 'passed-new-own-system-ucrt-rotation-only' -or $null -ne $old.failure){throw 'Passed old rotation platform provenance required'}
foreach($name in $dependencyPins.Keys){$records=@($old.artifacts|Where-Object {$_.name -eq $name});if($records.Count -ne 1 -or $records[0].sha256 -ne $dependencyPins[$name]){throw ('Dependency identity absent: '+$name)}}
function Check-Inputs {
 Check-File $compiler $compilerHash;Check-File $priorResult $priorHash;Check-File $systemCrt $systemCrtHash
 $crt=Get-Item -LiteralPath $systemCrt;if($crt.Length -ne 1399656 -or $crt.VersionInfo.FileVersion -ne '10.0.26100.9711 (WinBuild.160101.0800)'){throw 'Own UCRT version/size changed'}
 foreach($name in $sources.Keys){Check-File (Join-Path $taskRoot ('src\reverse_client_actions\'+$name)) $sources[$name]}
 foreach($name in $dependencyPins.Keys){Check-File (Join-Path $prior $name) $dependencyPins[$name]}
 foreach($name in $evidence.Keys){Check-File (Join-Path $taskRoot $name) $evidence[$name]}
}
Check-Inputs
$binding=Get-Content -LiteralPath (Join-Path $taskRoot 'docs\reverse-client\r1-engine-crowd-math-imports\inspection.json') -Raw|ConvertFrom-Json
if($binding.status -ne 'passed' -or !$binding.disk_bindings_complete -or $binding.bindings.Count -ne 3){throw 'Three exact Engine import bindings required'}
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash
 source_pins=$sources;dependency_pins=$dependencyPins;evidence_pins=$evidence;prior_result=$priorResult;prior_result_sha256=$priorHash
 system_crt_path=$systemCrt;system_crt_file_sha256=$systemCrtHash;system_crt_file_version='10.0.26100.9711 (WinBuild.160101.0800)'
 steps=@();test_results=@();artifacts=@();failure=$null;old_executable_entrypoints_invoked=@();old_test_suites_rerun=@()
 own_crowd_platform=$false;rotation_bounds_unchanged=$false;system_library_load_requested=$false;system_library_loaded=$null
 native_math_calls_new=$null;native_math_calls_affected_regression=$null;memory_image_hashed=$false;floating_control_write_mask=0
 original_read=$false;original_executed=$false;backend_started=$false;security_settings_changed=$false;gameplay_accepted=$false
 affected_regression_reason='Shared SystemUcrtRotationMath source adds lazy atan2f and sealed crowd view; recompile unchanged old11-test source against new DLL to verify existing4 exports,33 calls and [-4,4] rotation domain. No old26 rotation-composition or other suite.'
 support_boundary='One fixed current System32 UCRT owner; explicit R4 atan2f(y,x) and separate crowd sin/cos [-4,9] own finite normal-or-zero domain. Actual module path/export ownership and disk hash remain checked, not memory-image hash. Unknown scalar operands are caller responsibility; no native caller integration or original runtime equivalence. FP control query only, no errno/sticky flags/NaN/subnormal/nondefault environment guarantee.'
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
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$library=Join-Path $output 'GrimDynasty.SystemUcrtRotationMath.dll';$newTests=Join-Path $output 'SystemUcrtCrowdMathTests.exe';$oldTests=Join-Path $output 'AffectedSystemUcrtRotationMathTests.exe'
 $references=@('/reference:'+(Join-Path $output 'GrimDynasty.Actions.dll'))
 Run-Step 'compile-shared-system-ucrt-crowd' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@((Join-Path $source 'SystemUcrtRotationMath.cs')))
 Run-Step 'compile-new-system-ucrt-crowd-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:SystemUcrtCrowdMathTests','/reference:System.Web.Extensions.dll',"/out:$newTests","/reference:$library")+$references+@((Join-Path $source 'SystemUcrtCrowdMathTests.cs')))
 Run-Step 'compile-affected-system-ucrt-rotation-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:SystemUcrtRotationMathTests','/reference:System.Web.Extensions.dll',"/out:$oldTests","/reference:$library")+$references+@((Join-Path $source 'SystemUcrtRotationMathTests.cs')))
 $report.system_library_load_requested=$true
 Run-Step 'new-system-ucrt-crowd-tests' $newTests @()
 $new=Get-Content -LiteralPath (Join-Path $output 'new-system-ucrt-crowd-tests.log') -Tail 1|ConvertFrom-Json
 if($new.status -ne 'passed' -or $new.scenarios -ne 11 -or $new.assertions -le 0 -or $new.native_math_calls -ne 45 -or !$new.own_crowd_platform -or !$new.rotation_bounds_unchanged -or !$new.single_loader -or $new.library_sha256 -ne $systemCrtHash){throw 'Unexpected new crowd summary'}
 if($new.control_mask_written -ne 0 -or $new.control_before -ne $new.control_after -or $new.memory_image_hashed -ne $false -or $new.original_executed -ne $false -or $new.original_runtime_equivalence -ne $false){throw 'New fixture violated declared boundary'}
 $report.test_results += [ordered]@{name='new-system-ucrt-crowd-tests';kind='new';scenarios=$new.scenarios;assertions=$new.assertions;native_math_calls=$new.native_math_calls;exports=$new.exports}
 $report.system_library_loaded=$true;$report.native_math_calls_new=$new.native_math_calls
 $report.old_test_suites_rerun += 'SystemUcrtRotationMathTests: affected shared-loader regression only'
 Run-Step 'affected-system-ucrt-rotation-tests' $oldTests @()
 $regression=Get-Content -LiteralPath (Join-Path $output 'affected-system-ucrt-rotation-tests.log') -Tail 1|ConvertFrom-Json
 if($regression.status -ne 'passed' -or $regression.scenarios -ne 11 -or $regression.assertions -ne 96 -or $regression.native_math_calls -ne 33 -or !$regression.own_platform_adapter -or $regression.library_sha256 -ne $systemCrtHash){throw 'Unexpected affected rotation regression summary'}
 if($regression.control_mask_written -ne 0 -or $regression.control_before -ne $regression.control_after -or $regression.memory_image_hashed -ne $false -or $regression.original_read -ne $false -or $regression.original_executed -ne $false -or $regression.native_game_equivalence -ne $false){throw 'Regression fixture violated boundary'}
 $report.test_results += [ordered]@{name='affected-system-ucrt-rotation-tests';kind='affected-regression';scenarios=$regression.scenarios;assertions=$regression.assertions;native_math_calls=$regression.native_math_calls}
 Check-Inputs;foreach($name in $dependencyPins.Keys){Check-File (Join-Path $output $name) $dependencyPins[$name]}
 $report.native_math_calls_affected_regression=$regression.native_math_calls;$report.own_crowd_platform=$true;$report.rotation_bounds_unchanged=$true;$report.status='passed-new-system-ucrt-crowd-and-affected-rotation-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}};$report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;test_results=$report.test_results;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 7
