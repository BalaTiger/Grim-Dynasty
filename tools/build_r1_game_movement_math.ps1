param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$prior=Join-Path $taskRoot '.tooling\r1-entity-movement-validation'
$output=Join-Path $taskRoot '.tooling\r1-game-movement-math-validation'
$priorHash='8f3ef00be0d94da17f6d3ec4ae79081a0c562e9492a9e4c6f6d0ae6c49a77fff'
$sourcePins=@{
 'GameMovementMathContracts.cs'='a7c8bb5f8fe25fe5f94b8aa3a20de854951af2a9ec535c0066d39bca9e1041c5'
 'GameMovementMathTests.cs'='1be864ed7a5a0e1158b206006fabd2e439851a90103cf8a3d447ad75d9c054f1'
}
$dependencyPins=@{
 'GrimDynasty.Actions.dll'='3ea34074db9d5a67d7dffb702eb053b703dc3b754d0db442aeadd71247e15c58'
 'GrimDynasty.NavigationNearest.dll'='75f303c482679df6ba2c8f01aad7759ba9ed322ad6386100525b690cd9b9dfd0'
}
$evidencePins=@{
 'docs\reverse-client\r1-game-movement-math\summary.json'='f2824305c00a8807be3273eac11359ba5b789180d4ef3680fe100ee8cc76669d'
 'docs\reverse-client\r1-position-publication-binding\inspection.json'='cc920aecf8f89daa22850eb8e23ce110ec180ea8e3a42d8cd610dcb0c94a8161'
 'docs\reverse-client\r1-game-movement-math\07-GameMovementMath-0x180591b80-procedure_pseudo_code.json'='91ff315148f29814fab21f26d99d4c9b9cfa0b87897ba5448248ac2a41612f0b'
 'docs\reverse-client\r1-game-movement-math\08-GameMovementMath-0x180591b80-procedure_assembly.json'='ea6e0c0525c418839a234772a8e7ff6016decbe49ad682093a0e292f65320e99'
 'docs\reverse-client\r1-game-movement-math\09-GameMovementMath-0x180591b80-procedure_callees.json'='e0f8dcff5299d03293a80e3f718b2598cfc28b36063688668f75d98b864b1b72'
 'docs\reverse-client\r1-game-movement-math\10-GameMovementMath-0x18007e6f0-procedure_pseudo_code.json'='3f3e903843836d824965c24f9cb94da49dca0d8b83a468ebbaee6c93d2dcbd68'
 'docs\reverse-client\r1-game-movement-math\11-GameMovementMath-0x18007e6f0-procedure_assembly.json'='09ebd4dd314f74955a8c897cb6eac2468c9674decbb8618e9b22c547e90f339a'
 'docs\reverse-client\r1-game-movement-math\12-GameMovementMath-0x18007e6f0-procedure_callees.json'='8016f3e6b3b81945e555b21431924a128b9a7870c17b4669565e6f82436c6643'
}
function Hash([string]$path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-File([string]$path,[string]$expected){
 if($expected -notmatch '^[a-f0-9]{64}$'){throw ('Unfrozen input: '+$path)}
 $item=Get-Item -LiteralPath $path
 if($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw ('Unexpected/redirected input: '+$path)}
 if((Hash $path) -ne $expected){throw ('Fingerprint changed: '+$path)}
}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace and no arguments required'}
if(Test-Path -LiteralPath $output){throw 'Preserve existing mathematical validation; output must be new'}
$priorResult=Join-Path $prior 'result.json';Check-File $priorResult $priorHash
$old=Get-Content -LiteralPath $priorResult -Raw|ConvertFrom-Json
if($old.status -ne 'passed-new-entity-getter-contracts-only' -or !$old.new_entity_getter_contracts -or $null -ne $old.failure){throw 'Passed dependency provenance required'}
foreach($name in $dependencyPins.Keys){$record=@($old.artifacts|Where-Object name -eq $name);if($record.Count -ne 1 -or $record[0].sha256 -ne $dependencyPins[$name]){throw ('Dependency not pinned in passed result: '+$name)}}
function Check-Inputs {
 Check-File $compiler $compilerHash;Check-File $priorResult $priorHash
 foreach($name in $sourcePins.Keys){Check-File (Join-Path $taskRoot ('src\reverse_client_actions\'+$name)) $sourcePins[$name]}
 foreach($name in $dependencyPins.Keys){Check-File (Join-Path $prior $name) $dependencyPins[$name]}
 foreach($name in $evidencePins.Keys){Check-File (Join-Path $taskRoot $name) $evidencePins[$name]}
}
Check-Inputs
$native=Get-Content -LiteralPath (Join-Path $taskRoot 'docs\reverse-client\r1-game-movement-math\summary.json') -Raw|ConvertFrom-Json
if($native.status -ne 'passed' -or $native.completed_plan_calls -ne 14 -or !$native.closed_session_verified -or !$native.process_exit_confirmed -or !$native.owned_memory_profile.job_empty_confirmed -or !$native.owned_memory_profile.sampler_exit_confirmed){throw 'Real math query did not complete cleanly'}
$constants=Get-Content -LiteralPath (Join-Path $taskRoot 'docs\reverse-client\r1-position-publication-binding\inspection.json') -Raw|ConvertFrom-Json
$one=@($constants.constants|Where-Object rva -eq '0x7a8054')
if($constants.status -ne 'passed' -or $one.Count -ne 1 -or $one[0].bytes -ne 4 -or $one[0].raw_hex -ne '0000803f'){throw 'Exact disk one source mismatch'}
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash
 source_pins=$sourcePins;dependency_pins=$dependencyPins;prior_result=$priorResult;prior_result_sha256=$priorHash;evidence_pins=$evidencePins
 steps=@();test_results=@();artifacts=@();old_test_suites_rerun=@();old_executable_entrypoints_invoked=@()
 new_math_contracts=$false;original_read=$false;original_executed=$false;backend_started=$false
 position_publication_validated=$false;trigonometry_closed=$false;native_fp_environment_validated=$false;gameplay_accepted=$false;failure=$null
 support_boundary='Only separate-buffer Game591B80 basis and07E6F0 composition. Exact ordered binary32 arithmetic and scalar/paired writes; own masked ordinary FP domain. Native MXCSR/NaN payload/ABI, shifted aliases, trigonometry, coordinate publication and game acceptance remain unverified.'
}
New-Item -ItemType Directory -Path $output|Out-Null
function Run-Step([string]$name,[string]$program,[string[]]$arguments){
 $stdout=Join-Path $output ($name+'.log');$stderr=Join-Path $output ($name+'.stderr.log');$savedPreference=$ErrorActionPreference
 try{$ErrorActionPreference='Continue'; & $program @arguments 1> $stdout 2> $stderr;$ok=$?;$code=$LASTEXITCODE}finally{$ErrorActionPreference=$savedPreference}
 $report.steps += [ordered]@{name=$name;program=$program;arguments=$arguments;invocation_succeeded=$ok;exit_code=$code}
 if(!$ok -or $code -ne 0){throw "Step $name failed (invocation=$ok, exit=$code); logs preserved"}
 if($name.StartsWith('compile-') -and (Get-Item -LiteralPath $stdout).Length -ne 0){throw ('Compiler diagnostics: '+$name)}
 if((Get-Item -LiteralPath $stderr).Length -ne 0){throw ('Unexpected stderr: '+$name)}
}
try{
 foreach($name in $dependencyPins.Keys){Copy-Item -LiteralPath (Join-Path $prior $name) -Destination (Join-Path $output $name);Check-File (Join-Path $output $name) $dependencyPins[$name]}
 $references=@($dependencyPins.Keys|Sort-Object|ForEach-Object {'/reference:'+(Join-Path $output $_)})
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$library=Join-Path $output 'GrimDynasty.GameMovementMath.dll';$tests=Join-Path $output 'GameMovementMathTests.exe'
 Run-Step 'compile-game-movement-math' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@((Join-Path $source 'GameMovementMathContracts.cs')))
 Run-Step 'compile-game-movement-math-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:GameMovementMathTests',"/out:$tests","/reference:$library")+$references+@((Join-Path $source 'GameMovementMathTests.cs')))
 Run-Step 'new-game-movement-math-tests' $tests @()
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-game-movement-math-tests.log') -Tail 1|ConvertFrom-Json
 if($summary.status -ne 'passed' -or $summary.scenarios -ne 13 -or $summary.assertions -le 0 -or $summary.native_execution -ne $false -or $summary.old_suites_rerun -ne $false -or $summary.movement_publication -ne $false -or $summary.trigonometry_closed -ne $false -or $summary.gameplay_accepted -ne $false){throw 'Unexpected new isolated mathematical test summary'}
 $report.test_results += [ordered]@{name='new-game-movement-math-tests';kind='new';scenarios=$summary.scenarios;assertions=$summary.assertions}
 Check-Inputs;foreach($name in $dependencyPins.Keys){Check-File (Join-Path $output $name) $dependencyPins[$name]}
 $report.new_math_contracts=$true;$report.status='passed-new-game-movement-math-contracts-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 9|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;steps=$report.steps;test_results=$report.test_results;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 6
