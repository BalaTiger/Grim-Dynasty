param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$previous=Join-Path $taskRoot '.tooling\r1-world-publication-prerequisites-validation'
$output=Join-Path $taskRoot '.tooling\r1-world-publication-gates-validation'
$priorResult=Join-Path $previous 'result.json'
$priorResultHash='43da143e7dca57b18fffa4316797c734a809d2a223a91295fb8b95d9a6855c42'
$sources=@{
 'WorldPublicationGates.cs'='afabaa9d47a13daa52737579a16f90c301e3e6c3c5274d12f6ecdb82dc9d820a'
 'WorldPublicationGateTests.cs'='336cf23736511009d47eea3898bc70ed839a6b05df6608a8cbacbc4aade04545'
}
$dependencies=@{
 'GrimDynasty.Actions.dll'='3ea34074db9d5a67d7dffb702eb053b703dc3b754d0db442aeadd71247e15c58'
 'GrimDynasty.MoveManager.dll'='7563b6e32745e79ddc8308d2ea48ca7336050271d8dc8c9ce1e75346a52232da'
 'GrimDynasty.WorldCoordinates.dll'='6365ad6a051d6ffbb4009824d98fa899bec89f5933dea383a9a3d98f193e71c7'
 'GrimDynasty.NavigationNearest.dll'='75f303c482679df6ba2c8f01aad7759ba9ed322ad6386100525b690cd9b9dfd0'
 'GrimDynasty.EntityMovement.dll'='85d848625fce7d5fc580feb04839898bd580a96f0c48308c3aca5b8ab142f84d'
}
$evidence=@{
 'docs\reverse-client\r1-engine-entity-publication-core\11-EngineEntityPublication-0x180078110-procedure_assembly.json'='1af9956a63dfe3e0d1b118eb3984c541ff19a4b28a5628ea6cbfe5088d557fb1'
 'docs\reverse-client\r1-game-entity-publication-gates\05-EntityPublicationGate-0x180016d20-procedure_assembly.json'='d94b38635e8083b876c35f89d14b78dbe9c5afd78a8c5ec18f656bd6cc4ca458'
 'docs\reverse-client\r1-game-entity-publication-gates\08-EntityPublicationGate-0x1803d5bc0-procedure_assembly.json'='f903c05cdd867c4618bfc6429bba90392c1738579b5b849cc5a55c279eac8033'
 'docs\reverse-client\r1-engine-entity-publication-core\05-EngineEntityPublication-0x18000ce70-procedure_assembly.json'='4ebf842b2fceb4adc70e1dfcc1f6d231d888c27cc8b4ecb673c6f85681023458'
}
function Hash([string]$path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-File([string]$path,[string]$expected){
 $item=Get-Item -LiteralPath $path
 if($expected -notmatch '^[0-9a-f]{64}$' -or $item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -or (Hash $path) -ne $expected){throw ('Input identity changed: '+$path)}
}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace, no arguments required'}
if(Test-Path -LiteralPath $output){throw 'Existing gate evidence preserved; output must be new'}
Check-File $priorResult $priorResultHash
$prior=Get-Content -LiteralPath $priorResult -Raw|ConvertFrom-Json
if($prior.status -ne 'passed-new-world-publication-prerequisites-only' -or !$prior.own_preloaded_prerequisites -or $null -ne $prior.failure){throw 'Passed dependency result is required'}
foreach($name in $dependencies.Keys){$record=@($prior.artifacts|Where-Object {$_.name -eq $name});if($record.Count -ne 1 -or $record[0].sha256 -ne $dependencies[$name]){throw ('Dependency not pinned by passed result: '+$name)}}
function Check-Inputs {
 Check-File $compiler $compilerHash;Check-File $priorResult $priorResultHash
 foreach($name in $sources.Keys){Check-File (Join-Path $taskRoot ('src\reverse_client_actions\'+$name)) $sources[$name]}
 foreach($name in $dependencies.Keys){Check-File (Join-Path $previous $name) $dependencies[$name]}
 foreach($name in $evidence.Keys){Check-File (Join-Path $taskRoot $name) $evidence[$name]}
}
Check-Inputs
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash
 source_pins=$sources;dependency_pins=$dependencies;native_evidence_pins=$evidence;prior_result=$priorResult;prior_result_sha256=$priorResultHash
 steps=@();test_results=@();artifacts=@();old_test_suites_rerun=@();old_executable_entrypoints_invoked=@();failure=$null
 own_publication_gates=$false;disk_leaf_opt_in_validated=$false;loaded_leaf_verified=$false
 original_read=$false;original_executed=$false;backend_started=$false;position_publication_validated=$false;gameplay_accepted=$false
 support_boundary='Three evidenced gate functions on explicitly bound own shared Engine state, including AL-only return, 678-before-668 captures, borrowed global identity and explicit Unknown stops. Optional CE70 disk-leaf opt-in only. No network initialization, loaded imports/code, remaining callbacks, spatial publication or playable-game acceptance.'
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
 foreach($name in $dependencies.Keys){Copy-Item -LiteralPath (Join-Path $previous $name) -Destination (Join-Path $output $name);Check-File (Join-Path $output $name) $dependencies[$name]}
 $references=@();foreach($name in ($dependencies.Keys|Sort-Object)){$references+='/reference:'+(Join-Path $output $name)}
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$library=Join-Path $output 'GrimDynasty.WorldPublicationGates.dll';$tests=Join-Path $output 'WorldPublicationGateTests.exe'
 Run-Step 'compile-world-publication-gates' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@((Join-Path $source 'WorldPublicationGates.cs')))
 Run-Step 'compile-gate-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:WorldPublicationGateTests',"/out:$tests","/reference:$library")+$references+@((Join-Path $source 'WorldPublicationGateTests.cs')))
 Run-Step 'new-gate-tests' $tests @()
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-gate-tests.log') -Raw|ConvertFrom-Json
 if($summary.status -ne 'passed' -or $summary.scenarios -ne 12 -or $summary.assertions -le 0 -or $summary.own_publication_gates -ne $true -or $summary.disk_leaf_opt_in -ne $true -or $summary.loaded_leaf_verified -ne $false -or $summary.position_publication -ne $false -or $summary.gameplay_accepted -ne $false){throw 'Missing or unexpected gate summary'}
 $report.test_results += [ordered]@{name='shared-engine-character-player-publication-gates';kind='new';scenarios=$summary.scenarios;assertions=$summary.assertions}
 Check-Inputs;foreach($name in $dependencies.Keys){Check-File (Join-Path $output $name) $dependencies[$name]}
 $report.own_publication_gates=$true;$report.disk_leaf_opt_in_validated=$true;$report.status='passed-new-world-publication-gates-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 9|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;steps=$report.steps;test_results=$report.test_results;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 6
