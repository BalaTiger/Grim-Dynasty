param()
$ErrorActionPreference='Stop'
$taskRoot='C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
$prior=Join-Path $taskRoot '.tooling\r1-crowd-growth-validation'
$output=Join-Path $taskRoot '.tooling\r1-crowd-borrowed-header-validation'
$priorHash='f9005adbc8c3becc734cbcf19d6f5e8563ccfc9a877e2b7e5944621f03956796'
$sources=@{
 'NavigationCrowdBorrowedPointerContracts.cs'='708048a0906d498a534ba93468a474fd3318869f9b310cb8694b6b5f43d12686'
 'NavigationCrowdTickHeaderContracts.cs'='2f23f38c967b7ae8d73dcfdfb6c189ad8af804b00597b8ce9cfffe1d5c335f09'
 'NavigationCrowdBorrowedHeaderTests.cs'='798102e64fac6d78f65250ff9cbe413e9261c065b08712b9716fd57558d287bf'
 'NavigationCrowdStateContracts.cs'='5777a5d208a95ee9ae72d352ac871a70bb72925382ffa2cccc0c945245506154'
 'NavigationCrowdProximityContracts.cs'='f73d99326f23377d60f8cf5938a24f5591f7404687f6f0833485d89d59d7eeaf'
 'NavigationCrowdGrowthTests.cs'='734c614d5806c7a7a3ca77ab7a655c418b9de6fdc63517e7f783bc74dd7e5348'
 'NavigationCrowdProximityTests.cs'='fc80ea071b0179f332e47da99a51534f94204ade979812643659bb380ebced7c'
}
$dependencyPins=@{
 'GrimDynasty.Actions.dll'='3ea34074db9d5a67d7dffb702eb053b703dc3b754d0db442aeadd71247e15c58'
 'GrimDynasty.NavigationNearest.dll'='75f303c482679df6ba2c8f01aad7759ba9ed322ad6386100525b690cd9b9dfd0'
 'GrimDynasty.NavigationMeshTiles.dll'='0b8604244b7681e6f0f5e13a63c93f2f815069bbe74109e974938c94d9ee4d57'
 'GrimDynasty.NavigationCorridor.dll'='1afd6850ae3f2fc1c0f13c6562ce6b5749a3ea98d3b2e5792d67234d4c779e33'
 'GrimDynasty.NavigationCrowdAdapter.dll'='04215144406cd87357b98bd32911f96281453c4010e4f2f61e1c6997b065b73e'
 'GrimDynasty.NavigationTileData.dll'='b0459f94ca130467c2ec7f8c0379700250926c5a8eb8a3e6f617910a3bb02cd9'
}
$backupPins=@{
 'NavigationCrowdStateContracts.cs'='452aa247b515dcacafe886849f9799a9c6682c1b9059b6475b8b12d819aef399'
 'NavigationCrowdProximityContracts.cs'='c86de20b1035afcfb60bb3adc6c8517a98d366f0fda9841d8f671dd8c594f1e8'
}
$evidence=@{
 'docs\reverse-client\r1-crowd-vector-growth-retry-1\05-CrowdVectorGrowth-0x180034ec0-procedure_assembly.json'='ed9e2ba9ce77858ed5825f28c71cfdbe267833720cdb9e6c0a96d17dcdd25065'
 'docs\reverse-client\r1-crowd-vector-growth-retry-1\08-CrowdVectorGrowth-0x180034fe0-procedure_assembly.json'='fe796dd50332b6e123a02d1e1952e25c2c762cea7eeecac11d4c6daa5f3fc74c'
 'docs\reverse-client\r1-crowd-motion-helpers\05-CrowdMotionHelper-0x18025e900-procedure_assembly.json'='14037df10371dd50724313624c052d28d9e920361ccb9a8210ade5934bc475ce'
 'docs\reverse-client\r1-crowd-motion-helpers\08-CrowdMotionHelper-0x18025ea00-procedure_assembly.json'='03dd923a703fe4ae7c98d2ae7ded6cb16e52926598a9abaaaab1d9b6e1632d91'
 'docs\reverse-client\r1-crowd-motion-helpers\11-CrowdMotionHelper-0x18025eb90-procedure_assembly.json'='df302b0d88ff901dd212d8f104f3f95d206b6abd2056fccb853e012823968e21'
 'docs\reverse-client\r1-crowd-movement-direct\08-CrowdMovementDirect-0x18025c440-procedure_assembly.json'='62da8b3465041af3522b3e36a72e4680485d0c9b2a832ab8b3207ce27bf5b8ae'
 'docs\reverse-client\r1-crowd-motion-bindings\inspection.json'='cefbf931ba37144e6400be0d7a0c3c894f9a82828c19723d78be440a729334d3'
 'docs\reverse-client\r1-navigation-compressor-slot\inspection.json'='191e63e5cc34d9b1abf1326426827a27041743913768a3159e73af3dc62126de'
}
function Hash([string]$p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()}
function Check-Ancestors([string]$p){
 $cursor=[IO.Path]::GetFullPath($p)
 while($cursor){
  if(Test-Path -LiteralPath $cursor){$item=Get-Item -LiteralPath $cursor;if($item.Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Reparse path not permitted: '+$cursor)}}
  $parent=[IO.Directory]::GetParent($cursor);if($null -eq $parent){break};$cursor=$parent.FullName
 }
}
function Check-File([string]$p,[string]$expected){Check-Ancestors $p;if($expected -notmatch '^[0-9a-f]{64}$'){throw ('Unfrozen input: '+$p)};$item=Get-Item -LiteralPath $p;if($item.PSIsContainer -or (Hash $p) -ne $expected){throw ('Input identity changed: '+$p)}}
if($args.Count -ne 0 -or (Get-Location).Path -ne $taskRoot){throw 'Fixed workspace, no arguments required'}
Check-Ancestors $output
if(!([IO.Path]::GetFullPath($output)).StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Output must stay inside fixed workspace'}
if(Test-Path -LiteralPath $output){throw 'Prior evidence preserved; fresh output required'}
$priorResult=Join-Path $prior 'result.json'
Check-File $priorResult $priorHash
$old=Get-Content -LiteralPath $priorResult -Raw|ConvertFrom-Json
if($old.status -ne 'passed-new-crowd-growth-and-affected-state-proximity-regressions-only' -or $null -ne $old.failure){throw 'Passed growth prerequisite required'}
foreach($name in $dependencyPins.Keys){$record=@($old.artifacts|Where-Object {$_.name -eq $name});if($record.Count -ne 1 -or $record[0].sha256 -ne $dependencyPins[$name]){throw ('Dependency is not uniquely recorded by passed result: '+$name)}}
function Check-Inputs {
 Check-Ancestors $output
 Check-File $compiler $compilerHash;Check-File $priorResult $priorHash
 foreach($name in $sources.Keys){Check-File (Join-Path $taskRoot ('src\reverse_client_actions\'+$name)) $sources[$name]}
 foreach($name in $dependencyPins.Keys){Check-File (Join-Path $prior $name) $dependencyPins[$name]}
 foreach($name in $backupPins.Keys){Check-File (Join-Path $taskRoot ('docs\reverse-client\r1-crowd-borrowed-header-prior\'+$name)) $backupPins[$name]}
 foreach($name in $evidence.Keys){Check-File (Join-Path $taskRoot $name) $evidence[$name]}
}
Check-Inputs
$report=[ordered]@{
 started_utc=[DateTime]::UtcNow.ToString('o');status='running';compiler=$compiler;compiler_sha256=$compilerHash
 source_pins=$sources;dependency_pins=$dependencyPins;evidence_pins=$evidence;prior_source_backup_pins=$backupPins
 prior_result=$priorResult;prior_result_sha256=$priorHash;steps=@();test_results=@();artifacts=@();failure=$null
 old_executables_invoked=@();affected_regressions=@('NavigationCrowdGrowthTests:7','NavigationCrowdProximityTests:7');avoidance29_rerun=$false;tick_views9_rerun=$false
 original_read=$false;original_executed=$false;backend_started=$false;security_settings_changed=$false;bss_defaults_inferred=$false
 shared_borrowed_headers_validated=$false;reached_caller_fragments_only=$true;tick_executed=$false;character_movement_validated=$false;gameplay_accepted=$false
 support_boundary='Authoritative Work40 object and Agent88 ulong header views in an explicit OWN address domain, real shared034EC0/034FE0 growth,25EA00/25EB90 production, and reached25C832/25CB73 caller fragments. Earlier query/replan/callback prerequisites and later25BF40/tick/movement are not bypassed or claimed complete. Native allocator/EH, arbitrary address aliases, loaded globals and original FP equivalence remain unverified.'
}
New-Item -ItemType Directory -Path $output|Out-Null
function Run-Step([string]$name,[string]$program,[string[]]$arguments){
 $stdout=Join-Path $output ($name+'.log');$stderr=Join-Path $output ($name+'.stderr.log');$savedPreference=$ErrorActionPreference
 try{$ErrorActionPreference='Continue';& $program @arguments 1> $stdout 2> $stderr;$ok=$?;$code=$LASTEXITCODE}finally{$ErrorActionPreference=$savedPreference}
 $report.steps += [ordered]@{name=$name;program=$program;arguments=$arguments;invocation_succeeded=$ok;exit_code=$code}
 if(!$ok -or $code -ne 0){throw "Step $name failed (invocation=$ok, exit=$code); logs preserved"}
 if((Get-Item -LiteralPath $stderr).Length -ne 0 -or ($name.StartsWith('compile-') -and (Get-Item -LiteralPath $stdout).Length -ne 0)){throw ('Unexpected diagnostics: '+$name)}
}
function Read-Regression([string]$name){$matches=[regex]::Matches((Get-Content -LiteralPath (Join-Path $output ($name+'.log')) -Raw),'(?m)^PASS (\d+) scenarios, (\d+) assertions;');if($matches.Count -ne 1 -or [int]$matches[0].Groups[1].Value -ne 7 -or [int]$matches[0].Groups[2].Value -le 0){throw ('Unexpected regression summary: '+$name)};$report.test_results += [ordered]@{name=$name;kind='affected-regression';scenarios=7;assertions=[int]$matches[0].Groups[2].Value}}
try{
 foreach($name in $dependencyPins.Keys){Copy-Item -LiteralPath (Join-Path $prior $name) -Destination (Join-Path $output $name);Check-File (Join-Path $output $name) $dependencyPins[$name]}
 $references=@();foreach($name in $dependencyPins.Keys){$references += '/reference:'+(Join-Path $output $name)}
 $source=Join-Path $taskRoot 'src\reverse_client_actions';$stateLibrary=Join-Path $output 'GrimDynasty.NavigationCrowdState.dll';$proximityLibrary=Join-Path $output 'GrimDynasty.NavigationCrowdProximity.dll'
 Run-Step 'compile-shared-header-state' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$stateLibrary")+$references+@((Join-Path $source 'NavigationCrowdStateContracts.cs'),(Join-Path $source 'NavigationCrowdBorrowedPointerContracts.cs')))
 $references += "/reference:$stateLibrary"
 Run-Step 'compile-borrowed-proximity-callers' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/out:$proximityLibrary")+$references+@((Join-Path $source 'NavigationCrowdProximityContracts.cs'),(Join-Path $source 'NavigationCrowdTickHeaderContracts.cs')))
 $references += "/reference:$proximityLibrary"
 $tests=Join-Path $output 'NavigationCrowdBorrowedHeaderTests.exe';$growthTests=Join-Path $output 'NavigationCrowdGrowthTests.exe';$proximityTests=Join-Path $output 'NavigationCrowdProximityTests.exe'
 Run-Step 'compile-new-borrowed-header-tests' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/reference:System.Web.Extensions.dll','/main:NavigationCrowdBorrowedHeaderTests',"/out:$tests")+$references+@((Join-Path $source 'NavigationCrowdBorrowedHeaderTests.cs')))
 Run-Step 'compile-growth-regression' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:NavigationCrowdGrowthTestProgram',"/out:$growthTests")+$references+@((Join-Path $source 'NavigationCrowdGrowthTests.cs')))
 Run-Step 'compile-proximity-regression' $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe','/main:NavigationCrowdProximityTestProgram',"/out:$proximityTests")+$references+@((Join-Path $source 'NavigationCrowdProximityTests.cs')))
 Run-Step 'new-borrowed-header-tests' $tests @()
 $summary=Get-Content -LiteralPath (Join-Path $output 'new-borrowed-header-tests.log') -Tail 1|ConvertFrom-Json
 if($summary.status -ne 'passed' -or $summary.scenarios -ne 18 -or $summary.assertions -le 0 -or !$summary.shared_authoritative_headers -or !$summary.own_address_domain -or !$summary.reached_caller_fragments_only){throw 'Unexpected18-case shared-header summary'}
 foreach($name in @('original_read','original_executed','bss_defaults_inferred','tick_executed','character_movement_validated','gameplay_accepted')){if($summary.$name -ne $false){throw ('Fixture exceeded support boundary: '+$name)}}
 $report.test_results += [ordered]@{name='new-borrowed-header-tests';kind='new';scenarios=$summary.scenarios;assertions=$summary.assertions}
 Run-Step 'growth-regression' $growthTests @();Read-Regression 'growth-regression'
 Run-Step 'proximity-regression' $proximityTests @();Read-Regression 'proximity-regression'
 Check-Inputs;foreach($name in $dependencyPins.Keys){Check-File (Join-Path $output $name) $dependencyPins[$name]}
 $report.shared_borrowed_headers_validated=$true;$report.status='passed-new-shared-crowd-headers-and-affected-growth-proximity-only'
}catch{$report.status='failed';$report.failure=$_.Exception.Message}
finally{foreach($file in Get-ChildItem -LiteralPath $output -File){$report.artifacts += [ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Hash $file.FullName)}};$report.finished_utc=[DateTime]::UtcNow.ToString('o');$report|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8}
if($report.status -eq 'failed'){throw $report.failure}
[ordered]@{status=$report.status;test_results=$report.test_results;result=(Join-Path $output 'result.json')}|ConvertTo-Json -Depth 7
