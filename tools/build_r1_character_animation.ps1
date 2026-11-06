param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$manifestPath=Join-Path $taskRoot 'tools/r1_character_animation_build_fingerprints.json'
$manifestHash='79b4b2f7dfae15444ec161bb3e20b11b5f414d8a6f66b284f79cf7fc593805ac'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$output=Join-Path $taskRoot '.tooling/r1-character-animation-validation'
function Hash([string]$p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-Ancestors([string]$p){$cursor=[IO.Path]::GetFullPath($p);while($cursor){if(Test-Path -LiteralPath $cursor){if((Get-Item -LiteralPath $cursor).Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Reparse path rejected: '+$cursor)}};$parent=[IO.Directory]::GetParent($cursor);if($null -eq $parent){break};$cursor=$parent.FullName}}
function Under-Root([string]$p){$full=[IO.Path]::GetFullPath((Join-Path $taskRoot $p));if(!$full.StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Input leaves workspace'};return $full}
function Check-File([string]$p,[string]$expected){Check-Ancestors $p;if($expected -notmatch '^[0-9a-f]{64}$' -or (Get-Item -LiteralPath $p).PSIsContainer -or (Hash $p) -ne $expected){throw ('Changed or unfrozen input: '+$p)}}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace; no arguments'}
Check-File $manifestPath $manifestHash
$manifest=Get-Content -LiteralPath $manifestPath -Raw|ConvertFrom-Json
$sources='src/reverse_client_actions/GameCharacterAnimationContracts.cs|src/reverse_client_actions/GameCharacterAnimationTests.cs|src/reverse_client_actions/GameCharacterAnimationValidationProgram.cs'
$names='GrimDynasty.Actions.dll|GrimDynasty.EntityMovement.dll|GrimDynasty.GameMovementCallbacks.dll|GrimDynasty.WorldCoordinates.dll|GrimDynasty.NavigationNearest.dll|GrimDynasty.NavigationCrowdState.dll|GrimDynasty.MoveManager.dll|GrimDynasty.NavigationCrowdAdapter.dll|GrimDynasty.NavigationCorridor.dll|GrimDynasty.NavigationMeshTiles.dll'
if($manifest.schema -ne 'grim-dynasty.fixed-character-animation-build.v1' -or $manifest.status -ne 'frozen-not-executed' -or $manifest.compiles -ne 1 -or $manifest.scenarios -ne 7 -or ($manifest.sources.path -join '|') -ne $sources -or ($manifest.assemblies.name -join '|') -ne $names){throw 'Unexpected fixed plan'}
Check-Ancestors $output
if(Test-Path -LiteralPath $output){throw 'Existing output preserved; no overwrite/retry'}
function Check-Inputs {
 Check-File $compiler $compilerHash;Check-File $manifestPath $manifestHash
 foreach($entry in @($manifest.sources)+@($manifest.evidence)){Check-File (Under-Root $entry.path) $entry.sha256}
 Check-File (Under-Root $manifest.receipt.path) $manifest.receipt.sha256
 $prior=Get-Content -LiteralPath (Under-Root $manifest.receipt.path) -Raw|ConvertFrom-Json
 if($prior.status -ne 'passed-shared-waypoint-agent-target-production-prefix-only' -or $null -ne $prior.failure){throw 'Waypoint receipt did not pass'}
 foreach($entry in $manifest.assemblies){
  if($entry.path -ne ('.tooling/r1-waypoint-production-validation/'+$entry.name)){throw 'Dependency outside fixed prior output'}
  Check-File (Under-Root $entry.path) $entry.sha256
  if(@($prior.artifacts|Where-Object{$_.name -eq $entry.name -and $_.sha256 -eq $entry.sha256}).Count -ne 1){throw ('Missing unique receipt artifact: '+$entry.name)}
 }
}
Check-Inputs
$report=[ordered]@{started_utc=[DateTime]::UtcNow.ToString('o');status='running';manifest_sha256=$manifestHash;compiler_sha256=$compilerHash;source_pins=$manifest.sources;assembly_pins=$manifest.assemblies;receipt=$manifest.receipt;steps=@();artifacts=@();failure=$null;compiles=1;scenarios=0;assertions=0;old_test_suites_rerun=0;state_patch_applied=$false;original_read=$false;original_executed=$false;backend_started=$false;animation_resources_loaded=$false;native_play_restored=$false;production_animation_connected=$false;physical_mouse=$false;gameplay_accepted=$false}
New-Item -ItemType Directory -Path $output|Out-Null
function Run-Step([string]$name,[string]$program,[string[]]$arguments){
 $stdout=Join-Path $output ($name+'.log');$stderr=Join-Path $output ($name+'.stderr.log');$saved=$ErrorActionPreference
 try{$ErrorActionPreference='Continue';& $program @arguments 1> $stdout 2> $stderr;$ok=$?;$code=$LASTEXITCODE}finally{$ErrorActionPreference=$saved}
 $report.steps += [ordered]@{name=$name;program=$program;arguments=$arguments;invocation_succeeded=$ok;exit_code=$code}
 if(!$ok -or $code -ne 0){throw "Step $name failed (invocation=$ok, exit=$code); evidence retained"}
 if((Get-Item -LiteralPath $stderr).Length -ne 0 -or ($name -eq 'compile' -and (Get-Item -LiteralPath $stdout).Length -ne 0)){throw ('Unexpected diagnostics: '+$name)}
}
try{
 foreach($entry in $manifest.assemblies){Copy-Item -LiteralPath (Under-Root $entry.path) -Destination (Join-Path $output $entry.name);Check-File (Join-Path $output $entry.name) $entry.sha256}
 $exe=Join-Path $output 'GameCharacterAnimationTests.exe'
 $refs=@($manifest.assemblies|ForEach-Object{'/reference:'+(Join-Path $output $_.name)})
 Run-Step 'compile' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:GameCharacterAnimationValidationProgram',"/out:$exe")+$refs+@($manifest.sources|ForEach-Object{Under-Root $_.path}))
 Run-Step 'getter-tests' $exe @()
 $summary=Get-Content -LiteralPath (Join-Path $output 'getter-tests.log') -Tail 1|ConvertFrom-Json
 if($summary.status -ne 'passed-character-animation-getter-components-only' -or $summary.scenarios -ne 7 -or $summary.assertions -le 0 -or $summary.old_test_suites_rerun -ne 0){throw 'Unexpected getter test result'}
 foreach($flag in @('original_read','original_executed','backend_started','animation_resources_loaded','native_play_restored','production_animation_connected','physical_mouse','gameplay_accepted')){if($summary.$flag -ne $false){throw ('Unexpected scope: '+$flag)}}
 $report.scenarios=$summary.scenarios;$report.assertions=$summary.assertions;$report['test_result']=$summary
 Check-Inputs;foreach($entry in $manifest.assemblies){Check-File (Join-Path $output $entry.name) $entry.sha256}
 $report.status=$summary.status
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}};$report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;scenarios=$report.scenarios;assertions=$report.assertions;compiles=1;old_test_suites_rerun=0;production_animation_connected=$false;result=(Join-Path $output 'result.json')}|ConvertTo-Json
