param([Parameter(Mandatory=$true)][int]$NewAssertions)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'));Set-Location -LiteralPath $root
function Hash([string]$path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
function Save([string]$name,$value){$value|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $PSScriptRoot $name) -Encoding UTF8}
$prior=Get-Content -LiteralPath 'docs/object-lifecycle/class-getter-validation/acceptance-class-getter.json' -Raw|ConvertFrom-Json
$total=20;$scenes=0;$suiteCount=0;$baseSeen=$false;$demoSeen=$false
foreach($line in (Get-Content -LiteralPath (Join-Path $PSScriptRoot 'build-output.txt'))){
 if($line -eq 'ObjectBaseConstructorTests: 40 assertions passed'){$total+=40;$baseSeen=$true}
 elseif($line.StartsWith('{')){
  $row=$line|ConvertFrom-Json;if($row.status -ne 'passed'){throw 'Failed suite'}
  if($row.PSObject.Properties.Name -contains 'fixed_assertions'){$total+=$row.fixed_assertions;$suiteCount++}
  if($row.PSObject.Properties.Name -contains 'scenarios'){$scenes+=$row.scenarios}
  if($row.PSObject.Properties.Name -contains 'trace'){$demoSeen=$true}
 }
}
if($NewAssertions -le 0 -or $total -ne (703+$NewAssertions) -or $scenes -ne 6 -or $suiteCount -ne 12 -or !$baseSeen -or !$demoSeen){throw 'Unexpected build totals'}
if((Get-Content -LiteralPath (Join-Path $PSScriptRoot 'object-id-regression-output.txt') -Raw).Trim() -ne 'Object ID assertions passed: 20'){throw 'Object ID result mismatch'}
$counts=[ordered]@{};foreach($property in $prior.assertions.PSObject.Properties){if($property.Name -ne 'total'){$counts[$property.Name]=$property.Value}}
$counts['FinishPartialObject']=$NewAssertions;$counts['total']=$total
$head=(& git rev-parse HEAD).Trim();if($head -ne $prior.head){throw 'HEAD changed'}
$priorVerified=@(foreach($entry in $prior.source_and_binary_manifest){
 if($entry.path -ne 'src/task_items/build_items.ps1'){
  if((Hash $entry.path) -ne $entry.sha256){throw "Prior accepted file changed: $($entry.path)"}
  [ordered]@{path=$entry.path;sha256=$entry.sha256;unchanged=$true}
 }
})
$configs=@(foreach($entry in $prior.project_configurations){if((Hash $entry.path) -ne $entry.sha256){throw 'Project configuration changed'};$entry})
if(@(& git diff --name-only HEAD -- 'src/rewards/*.cs').Count -ne 0){throw 'Rewards C# changed'}
$oldProvider=@((& git show 'HEAD:tools/build_offline.ps1')|Where-Object {$_ -like '*native_math_provider=*'})
$newProvider=@((Get-Content -LiteralPath 'tools/build_offline.ps1')|Where-Object {$_ -like '*native_math_provider=*'})
if($oldProvider.Count -ne 1 -or $newProvider.Count -ne 1 -or $oldProvider[0] -ne $newProvider[0]){throw 'UCRT provider record changed'}
$baselinePath='docs/object-lifecycle/class-getter-validation/game-hashes-after.json'
$baseline=Get-Content -LiteralPath $baselinePath -Raw|ConvertFrom-Json
if($baseline.game_root -ne 'D:\Steam\steamapps\common\Grim Dawn' -or @($baseline.files).Count -ne 13){throw 'Original baseline mismatch'}
$originals=@(foreach($entry in $baseline.files){
 $full=[IO.Path]::GetFullPath((Join-Path $baseline.game_root $entry.path))
 if(!$full.StartsWith($baseline.game_root+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Original path outside installation'}
 $hash=Hash $full;if($hash -ne $entry.sha256){throw "Original hash changed: $($entry.path)"}
 [ordered]@{path=$entry.path;sha256=$hash;expected=$entry.sha256;unchanged=$true}
})
Save 'game-hashes-after.json' ([ordered]@{utc=[DateTime]::UtcNow.ToString('o');game_root=$baseline.game_root;baseline=$baselinePath;operation='Read-only SHA256 of exact 13 existing inventory files; no executable load or execution';all_unchanged=$true;files=$originals})
$build='.tooling/finish-partial-build';$assembly=[Reflection.Assembly]::LoadFile((Join-Path $root "$build/GrimDynasty.TaskItems.dll"))
$refs=@($assembly.GetReferencedAssemblies()|ForEach-Object {$_.FullName})
if(@($refs|Where-Object {$_ -notmatch '^(GrimDynasty\.|mscorlib,|System\.)'}).Count -ne 0){throw 'Unexpected own assembly dependency'}
Save 'task-items-dependencies.json' ([ordered]@{assembly="$build/GrimDynasty.TaskItems.dll";references=$refs;original_binary_dependencies=@()})
$paths=@($prior.source_and_binary_manifest|ForEach-Object {$_.path.Replace('.tooling/class-getter-build/','.tooling/finish-partial-build/')})
$paths+=@('src/task_items/FinishPartialObject.cs','src/task_items/FinishPartialObjectTests.cs',"$build/FinishPartialObjectTests.exe")
$manifest=@(foreach($path in ($paths|Sort-Object -Unique)){[ordered]@{path=$path;sha256=(Hash $path)}})
$evidence=@(foreach($path in @('docs/object-lifecycle/rea-native-registration/05-ObjectManager-FinishPartial-order-assembly.json','docs/object-lifecycle/rea-record-getter-support/08-CString-copy-0x180022ce0-assembly.json','docs/object-lifecycle/OBJECT-NAME-EVIDENCE.md','docs/object-lifecycle/FINISH-PARTIAL-EVIDENCE.md')){[ordered]@{path=$path;sha256=(Hash $path)}})
Save 'acceptance-finish-partial.json' ([ordered]@{
 utc=[DateTime]::UtcNow.ToString('o');status='bounded-finish-partial-caller-passed';head=$head;assertions=$counts;combined_scenarios=6;new_combined_scenarios=2;existing_scenarios=@('ItemQuestDemo');own_build_directory=$build
 new_rea_calls=0;ghidra_or_heavy_index_started=$false;reused_evidence=$evidence;source_and_binary_manifest=$manifest
 connected_path='CreatePartial full stored-name load/getter/factory/name -> FinishPartial first-NUL prefix -> actual second LoadFile with potentially different table -> existing registration and two growth operations -> explicit own Read or Unsupported'
 entry_guards='Null object first, then unknown/zero uint32 ID; no name/depot/manager access for known short circuits'
 name_scope='Stable valid logical object storage with own trailing terminator; reached C-string prefix <=65536; fresh copy cap 15 or length|15 under normal allocation/copy/release contracts'
 object_name_or_raw_key_recomputed=$false;null_table_semantics='Preserve counted hit, require uncovered diagnostic even for empty name; no ID write, pending/index mutation or Read';miss_semantics='Preserve explicit loader Unsupported, do not turn it into a known-null result or diagnostic'
 registration_scope='Use existing real own registration implementation; caller supplies stable manager/depot/cache and optional explicit own Read; missing Read remains Unsupported after completed registration'
 native_crt_body_or_abi_claimed=$false;own_cleanup='Managed exception reporting and existing Monitor cleanup are own policy, not native EH or rollback'
 native_manager_population_implemented=$false;cache_miss_implemented=$false;binary_present_getter_implemented=$false;native_class_info_population_implemented=$false;native_derived_constructor_implemented=$false;load_failure_diagnostic_implemented=$false;concrete_object_read_implemented=$false;full_allocator_lifecycle_integrated=$false
 original_binary_hashes_unchanged=13;project_configurations=$configs;rewards_cs_changed=$false;ucrt_whitelist_changed=$false;full_offline_acceptance='Not rerun; historical 894/16 and unchanged UCRT gate remain distinct'
 approvals='No new backend execution or installation requested; no denied, pending or uncertain action in this phase';target_executed=$false;saves_or_steam_cloud_accessed=$false;global_configuration_changed=$false;additional_software_installed=$false;commit_or_push=$false
})
Save 'consistency-check.json' ([ordered]@{prior_accepted_files=$priorVerified;verified_assertions=$total;combined_scenarios=$scenes;existing_demo_passed=$demoSeen;source_manifest_hashes_verified=$true;original_hashes_verified=13;native_math_provider_line_unchanged=$true;head=$head})
[ordered]@{status='passed';assertions=$total;new_assertions=$NewAssertions;combined_scenarios=$scenes;new_rea_calls=0;original_hashes_unchanged=13;manifest_entries=$manifest.Count}|ConvertTo-Json
