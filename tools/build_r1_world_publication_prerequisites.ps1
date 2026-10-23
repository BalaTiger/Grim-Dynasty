param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$previous=Join-Path $taskRoot '.tooling\r1-game-movement-callback-contracts-validation'
$output=Join-Path $taskRoot '.tooling\r1-world-publication-prerequisites-validation'
$priorResult=Join-Path $previous 'result.json'
$priorResultHash='7756201f00ff66c5808b0e8c579769d7255700ce2f89fac4d199f5cb03a9f1ff'
$sources=@{
 'WorldPublicationPrerequisites.cs'='6ad4df1db09081b007cbc708897e292d46ab2ff03ce2d9f416b933565db2f7ba'
 'WorldPublicationPrerequisitesTests.cs'='26e36b5117db8efa065ffffecfdc0d43c1c829d15680ad03ef502b8c0aed36c5'
}
$dependencies=@{
 'GrimDynasty.Actions.dll'='3ea34074db9d5a67d7dffb702eb053b703dc3b754d0db442aeadd71247e15c58'
 'GrimDynasty.MoveManager.dll'='7563b6e32745e79ddc8308d2ea48ca7336050271d8dc8c9ce1e75346a52232da'
 'GrimDynasty.WorldCoordinates.dll'='6365ad6a051d6ffbb4009824d98fa899bec89f5933dea383a9a3d98f193e71c7'
 'GrimDynasty.NavigationNearest.dll'='75f303c482679df6ba2c8f01aad7759ba9ed322ad6386100525b690cd9b9dfd0'
 'GrimDynasty.EntityMovement.dll'='85d848625fce7d5fc580feb04839898bd580a96f0c48308c3aca5b8ab142f84d'
}
$evidence=@{
 'docs\reverse-client\r1-position-publication\08-PositionPublication-0x180228ab0-procedure_assembly.json'='c5236490badf773ceb4f3b7f8678e6005a0cc6ca6c0496e5907207a7f2d8b3fc'
 'docs\reverse-client\r1-same-region-movement\05-SameRegionMovement-0x1801943e0-procedure_assembly.json'='62481de8eca05657b7645a229a8be7271dc374a7c3791b283a83542ed570c36f'
 'docs\reverse-client\r1-region-load-publication\08-RegionLoadPublication-0x180194440-procedure_assembly.json'='d169c6ce0dd410356d94385e7962da389d7b08cf1e00785bf5ebf76d2b0c54cf'
}
function Hash([string]$path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-File([string]$path,[string]$expected){
 $item=Get-Item -LiteralPath $path
 if($expected -notmatch '^[0-9a-f]{64}$' -or $item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -or (Hash $path) -ne $expected){throw ('Input identity changed: '+$path)}
}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace, no arguments required'}
if(Test-Path -LiteralPath $output){throw 'Existing prerequisite evidence preserved; output must be new'}
Check-File $priorResult $priorResultHash
$prior=Get-Content -LiteralPath $priorResult -Raw|ConvertFrom-Json
if($prior.status -ne 'passed-new-callback-contracts-and-affected-prefix-regression-only' -or !$prior.new_callback_contracts -or $null -ne $prior.failure){throw 'Passed dependency result is required'}
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
 own_preloaded_prerequisites=$false;own_monitor_adapter_validated=$false;native_critical_section_validated=$false;cold_load_validated=$false
 original_read=$false;original_executed=$false;backend_started=$false;position_publication_validated=$false;gameplay_accepted=$false
 support_boundary='Constructor first-QWORD write and Region1943E0(false) in explicit preloaded own state with real managed reentrant Monitor adapter. No native lock layout/lifetime, cold loading, changed-lock-pointer recovery, Entity callbacks, spatial publication, loaded-game or gameplay acceptance.'
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
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$library=Join-Path $output 'GrimDynasty.WorldPublicationPrerequisites.dll';$tests=Join-Path $output 'WorldPublicationPrerequisitesTests.exe'
 Run-Step 'compile-world-publication-prerequisites' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$library")+$references+@((Join-Path $source 'WorldPublicationPrerequisites.cs')))
 Run-Step 'compile-prerequisite-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:WorldPublicationPrerequisitesTests',"/out:$tests","/reference:$library")+$references+@((Join-Path $source 'WorldPublicationPrerequisitesTests.cs')))
 Run-Step 'new-prerequisite-tests' $tests @()
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-prerequisite-tests.log') -Raw|ConvertFrom-Json
 if($summary.status -ne 'passed' -or $summary.scenarios -ne 9 -or $summary.assertions -le 0 -or $summary.own_monitor_adapter -ne $true -or $summary.native_critical_section -ne $false -or $summary.cold_load -ne $false -or $summary.position_publication -ne $false -or $summary.gameplay_accepted -ne $false){throw 'Missing or unexpected prerequisite summary'}
 $report.test_results += [ordered]@{name='constructor-preloaded-level-and-own-monitor';kind='new';scenarios=$summary.scenarios;assertions=$summary.assertions}
 Check-Inputs;foreach($name in $dependencies.Keys){Check-File (Join-Path $output $name) $dependencies[$name]}
 $report.own_preloaded_prerequisites=$true;$report.own_monitor_adapter_validated=$true;$report.status='passed-new-world-publication-prerequisites-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{
 foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}}
 $report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 9|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;steps=$report.steps;test_results=$report.test_results;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 6
