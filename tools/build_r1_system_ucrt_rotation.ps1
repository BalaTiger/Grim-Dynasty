param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$prior=Join-Path $taskRoot '.tooling\r1-world-same-region-publication-validation'
$priorHash='e61195d71263fe3d40ed725c4d3cfcb8de57a522d56b2e81d663fc3b9702489b'
$output=Join-Path $taskRoot '.tooling\r1-system-ucrt-rotation-validation-2'
$systemCrt='C:\Windows\System32\ucrtbase.dll'
$systemCrtHash='dedb7611e1abc1ff3852dcac3d4c93a048d729e25b061973b9969f22698f14ca'
$sourcePins=@{
 'SystemUcrtRotationMath.cs'='3e1456e9ec9be5349632724c80aa5802dad6b916e36e2220f820ac8cda7de9d0'
 'SystemUcrtRotationMathTests.cs'='24809d601d4241c3a5cb6aa87eef2378c1e0f5472ff7bbb337bb154244a5a233'
}
$dependencyPins=@{'GrimDynasty.Actions.dll'='3ea34074db9d5a67d7dffb702eb053b703dc3b754d0db442aeadd71247e15c58'}
$evidencePins=@{
 'docs\reverse-client\r1-game-rotation-math-imports\inspection.json'='1dc150ecf36c3012fbd1665992cac25ca1728a16af93981d83a0d13e1d951b46'
 'docs\reverse-client\r1-system-ucrt-rotation-profile.json'='def13e5755f5969dd042a4e058a73a38b77c5e5fa69574a4e246a32bb81d41e7'
 'src\rewards\SystemCrtMath.cs'='eab09ed5a5281c2390988be890d7d0986e2d3fde0b96a7a0b3788d549991aae2'
 'docs\host-migration\ucrt-standalone-diagnostic.json'='051bc8ddd6e324b9a485ff8f133aadee20a28f5a0ab47f9804b4662b0510d59f'
}
function Hash([string]$path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
function Plain([string]$path){
 $current=[IO.Path]::GetFullPath($path)
 while($current){
  $entry=Get-Item -LiteralPath $current -Force
  if($entry.Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Redirected path: '+$current)}
  $parent=[IO.Path]::GetDirectoryName($current);if($parent -eq $current){break};$current=$parent
 }
}
function Check-File([string]$path,[string]$expected){
 if($expected -notmatch '^[a-f0-9]{64}$'){throw ('Unfrozen input: '+$path)}
 Plain $path;$item=Get-Item -LiteralPath $path
 if($item.PSIsContainer){throw ('Not a regular file: '+$path)}
 if((Hash $path) -ne $expected){throw ('Fingerprint changed: '+$path)}
}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace and no arguments required'}
if(Test-Path -LiteralPath $output){throw 'Preserve existing result; output must be new'}
Plain (Split-Path -Parent $output)
$priorResult=Join-Path $prior 'result.json';Check-File $priorResult $priorHash
$old=Get-Content -LiteralPath $priorResult -Raw|ConvertFrom-Json
if($old.status -ne 'passed-new-world-same-region-publication-only' -or $null -ne $old.failure){throw 'Passed source assembly provenance required'}
foreach($name in $dependencyPins.Keys){
 $records=@($old.artifacts|Where-Object name -eq $name)
 if($records.Count -ne 1 -or $records[0].sha256 -ne $dependencyPins[$name]){throw ('Dependency pin absent: '+$name)}
}
function Check-Inputs{
 Check-File $compiler $compilerHash;Check-File $priorResult $priorHash;Check-File $systemCrt $systemCrtHash
 $crt=Get-Item -LiteralPath $systemCrt
 if($crt.Length -ne 1399656 -or $crt.VersionInfo.FileVersion -ne '10.0.26100.9711 (WinBuild.160101.0800)'){throw 'Unexpected own UCRT version/size'}
 foreach($name in $sourcePins.Keys){Check-File (Join-Path $taskRoot ('src\reverse_client_actions\'+$name)) $sourcePins[$name]}
 foreach($name in $dependencyPins.Keys){Check-File (Join-Path $prior $name) $dependencyPins[$name]}
 foreach($name in $evidencePins.Keys){Check-File (Join-Path $taskRoot $name) $evidencePins[$name]}
}
Check-Inputs
$binding=Get-Content -LiteralPath (Join-Path $taskRoot 'docs\reverse-client\r1-game-rotation-math-imports\inspection.json') -Raw|ConvertFrom-Json
if($binding.status -ne 'passed' -or !$binding.disk_bindings_complete -or $binding.bindings.Count -ne 3){throw 'Exact three original import bindings required'}
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash
 source_pins=$sourcePins;dependency_pins=$dependencyPins;evidence_pins=$evidencePins;prior_result=$priorResult;prior_result_sha256=$priorHash
 system_crt_path=$systemCrt;system_crt_file_sha256=$systemCrtHash;system_crt_file_version='10.0.26100.9711 (WinBuild.160101.0800)'
 steps=@();test_results=@();artifacts=@();old_test_suites_rerun=@();old_executable_entrypoints_invoked=@();failure=$null
 own_platform_adapter=$false;system_library_load_requested=$false;system_library_loaded=$null;native_math_calls=$null
 memory_image_hashed=$false;native_game_equivalence=$false;full_rotation_integrated=$false;original_game_read=$false;original_game_executed=$false;backend_started=$false
 floating_control_write_mask=0;security_settings_changed=$false;old_rewards_profile_modified=$false
 retry_reason='Only qualify three System.Environment references after CS0117 name collision. Prior first compile failed; no prior UCRT load or tests. This is repaired compilation and first test attempt, not repeated tests.'
 preserved_failed_result='.tooling\r1-system-ucrt-rotation-validation\result.json';preserved_failed_result_sha256='f23661c109d63d1e38dd6e5b54f8fe232b24e75580dda93453a5a2a70956a0bd'
 support_boundary='Own current Windows x64 System32 UCRT finite float adapter only: fixed absolute source, SYSTEM32 dependency search, disk file hash/version and actual module path/export ownership. Preloaded memory image is not hashed. Acos finite normal-or-zero [-1,1]; sin/cos [-4,4]; nearest/masked/denormal-preserved control queried only. No NaN/infinity/subnormal domain, errno/sticky-state equivalence, original-game execution/equivalence, CRT internals or full RotateTowards/591A10 integration.'
}
New-Item -ItemType Directory -Path $output|Out-Null
function Run-Step([string]$name,[string]$program,[string[]]$arguments){
 $stdout=Join-Path $output ($name+'.log');$stderr=Join-Path $output ($name+'.stderr.log');$savedPreference=$ErrorActionPreference
 try{$ErrorActionPreference='Continue';& $program @arguments 1> $stdout 2> $stderr;$ok=$?;$code=$LASTEXITCODE}finally{$ErrorActionPreference=$savedPreference}
 $report.steps += [ordered]@{name=$name;program=$program;arguments=$arguments;invocation_succeeded=$ok;exit_code=$code}
 if(!$ok -or $code -ne 0){throw "Step $name failed (invocation=$ok, exit=$code); logs preserved"}
 if($name.StartsWith('compile-') -and (Get-Item -LiteralPath $stdout).Length -ne 0){throw ('Compiler diagnostics: '+$name)}
 if((Get-Item -LiteralPath $stderr).Length -ne 0){throw ('Unexpected stderr: '+$name)}
}
try{
 foreach($name in $dependencyPins.Keys){Copy-Item -LiteralPath (Join-Path $prior $name) -Destination (Join-Path $output $name);Check-File (Join-Path $output $name) $dependencyPins[$name]}
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$library=Join-Path $output 'GrimDynasty.SystemUcrtRotationMath.dll';$tests=Join-Path $output 'SystemUcrtRotationMathTests.exe'
 $references=@('/reference:'+(Join-Path $output 'GrimDynasty.Actions.dll'))
 Run-Step 'compile-system-ucrt-rotation' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@((Join-Path $source 'SystemUcrtRotationMath.cs')))
 Run-Step 'compile-system-ucrt-rotation-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:SystemUcrtRotationMathTests','/reference:System.Web.Extensions.dll',"/out:$tests","/reference:$library")+$references+@((Join-Path $source 'SystemUcrtRotationMathTests.cs')))
 $report.system_library_load_requested=$true
 Run-Step 'new-system-ucrt-rotation-tests' $tests @()
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-system-ucrt-rotation-tests.log') -Tail 1|ConvertFrom-Json
 if($summary.status -ne 'passed' -or $summary.scenarios -ne 11 -or $summary.assertions -le 0 -or $summary.native_math_calls -ne 33 -or !$summary.own_platform_adapter -or $summary.library_sha256 -ne $systemCrtHash){throw 'Unexpected own platform fixture summary'}
 if($summary.control_mask_written -ne 0 -or $summary.control_before -ne $summary.control_after -or $summary.memory_image_hashed -ne $false -or $summary.original_read -ne $false -or $summary.original_executed -ne $false -or $summary.native_game_equivalence -ne $false -or $summary.full_rotation_integrated -ne $false -or $summary.old_suites_rerun -ne $false){throw 'Fixture violated declared platform-only boundary'}
 $report.test_results += [ordered]@{name='new-system-ucrt-rotation-tests';kind='new';scenarios=$summary.scenarios;assertions=$summary.assertions;native_math_calls=$summary.native_math_calls;export_rvas=$summary.exports}
 Check-Inputs;foreach($name in $dependencyPins.Keys){Check-File (Join-Path $output $name) $dependencyPins[$name]}
 $report.system_library_loaded=$true;$report.native_math_calls=$summary.native_math_calls;$report.own_platform_adapter=$true;$report.status='passed-new-own-system-ucrt-rotation-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;test_results=$report.test_results;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 7
