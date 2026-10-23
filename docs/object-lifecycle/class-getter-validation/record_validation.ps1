$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
Set-Location -LiteralPath $root
function Hash([string]$path) { (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() }
function Save([string]$name,$value) { $value | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $PSScriptRoot $name) -Encoding UTF8 }
$head=(& git rev-parse HEAD).Trim()
if ($head -ne '6dfdae419d1c92f7738eefe241937057d83134df') {throw 'Unexpected HEAD'}
$assertions=[ordered]@{RegistrationIndex=183;RegistrationGrowth=141;LoadedObjectRegistration=47;ObjectBaseConstructor=40;ObjectNameAssignment=37;ObjectLifecycleCombined=20;RecordLoadFile=42;RecordTableGetter=43;CreatePartialObject=40;ClassFactoryDispatch=40;ItemCondition=39;ItemAuto=11;ObjectId=20;total=703}
$actual=20; $combined=0; $jsonSuites=0; $baseSeen=$false; $demoSeen=$false
foreach($line in (Get-Content -LiteralPath (Join-Path $PSScriptRoot 'build-output.txt'))) {
 if($line -eq 'ObjectBaseConstructorTests: 40 assertions passed') {$actual+=40;$baseSeen=$true}
 elseif($line.StartsWith('{')) {
  $record=$line|ConvertFrom-Json
  if($record.status -ne 'passed'){throw 'Failed suite'}
  if($record.PSObject.Properties.Name -contains 'fixed_assertions') {$actual+=$record.fixed_assertions;$jsonSuites++}
  if($record.PSObject.Properties.Name -contains 'scenarios') {$combined+=$record.scenarios}
  if($record.PSObject.Properties.Name -contains 'trace') {$demoSeen=$true}
 }
}
if($actual -ne 703 -or $combined -ne 4 -or $jsonSuites -ne 11 -or !$baseSeen -or !$demoSeen) {throw 'Build log totals mismatch'}
if((Get-Content -LiteralPath (Join-Path $PSScriptRoot 'object-id-regression-output.txt') -Raw).Trim() -ne 'Object ID assertions passed: 20') {throw 'Object ID result mismatch'}
$deps=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'task-items-dependencies.json') -Raw | ConvertFrom-Json
if(@($deps.original_binary_dependencies).Count -ne 0) {throw 'Unexpected original dependency'}
$batches=@(foreach($mode in @('record-class-getters','record-getter-support')) {
 $summary=Get-Content -LiteralPath "docs/object-lifecycle/rea-$mode/summary.json" -Raw | ConvertFrom-Json
 $plan="tools/static_analysis/rea-$mode-plan.json"
 if($summary.status -ne 'passed' -or !$summary.closed_session_verified -or !$summary.close_acknowledged -or !$summary.process_exit_confirmed -or $summary.child_exit_code -ne 0 -or $summary.target.sha256_before -ne $summary.target.sha256_after -or $summary.completed_plan_calls -ne $summary.tool_calls -or $summary.plan_sha256 -ne (Hash $plan)) {throw "Incomplete Rea batch $mode"}
 [ordered]@{directory="rea-$mode";calls=$summary.tool_calls;analysis_calls=($summary.tool_calls-5);plan_sha256=$summary.plan_sha256;target_sha256=$summary.target.sha256_after;closed_session_verified=$true;child_exit_code=0}
})
$callTotal=0;foreach($batch in $batches){$callTotal+=[int]$batch['calls']}
if($callTotal -ne 28) {throw 'Rea call count mismatch'}
$priorAcceptance=Get-Content -LiteralPath 'docs/object-lifecycle/record-load-validation/acceptance-record-load.json' -Raw | ConvertFrom-Json
$unchanged=@(foreach($entry in $priorAcceptance.source_and_binary_manifest) {
 if($entry.path -like 'src/task_items/*.cs') {
  $hash=Hash $entry.path
  if($hash -ne $entry.sha256) {throw "Prior source changed: $($entry.path)"}
  [ordered]@{path=$entry.path;sha256=$hash;unchanged=$true}
 }
})
$configs=@(foreach($path in @('.codex/config.toml','docs/host-migration/rea-project-config.toml')) {
 $hash=Hash $path
 if($hash -ne '7aeed98559ed397816f78ae53356cf65477c6479aba1ece58b5dcb50b83c09df') {throw 'Project config changed'}
 [ordered]@{path=$path;sha256=$hash;unchanged=$true}
})
if(@(& git diff --name-only HEAD -- 'src/rewards/*.cs').Count -ne 0) {throw 'Rewards C# changed'}
$oldProvider=@((& git show 'HEAD:tools/build_offline.ps1')|Where-Object {$_ -like '*native_math_provider=*'})
$newProvider=@((Get-Content -LiteralPath 'tools/build_offline.ps1')|Where-Object {$_ -like '*native_math_provider=*'})
if($oldProvider.Count -ne 1 -or $newProvider.Count -ne 1 -or $oldProvider[0] -ne $newProvider[0]) {throw 'Native math provider record changed'}
$baselinePath='docs/object-lifecycle/record-load-validation/game-hashes-after.json'
$baseline=Get-Content -LiteralPath $baselinePath -Raw | ConvertFrom-Json
if($baseline.game_root -ne 'D:\Steam\steamapps\common\Grim Dawn' -or @($baseline.files).Count -ne 13) {throw 'Original file baseline mismatch'}
$gameHashes=@(foreach($entry in $baseline.files) {
 $full=[IO.Path]::GetFullPath((Join-Path $baseline.game_root $entry.path))
 if(!$full.StartsWith($baseline.game_root+'\',[StringComparison]::OrdinalIgnoreCase)) {throw 'Original path outside installation'}
 $hash=Hash $full
 if($hash -ne $entry.sha256) {throw "Original hash changed: $($entry.path)"}
 [ordered]@{path=$entry.path;sha256=$hash;expected=$entry.sha256;unchanged=$true}
})
Save 'game-hashes-after.json' ([ordered]@{utc=[DateTime]::UtcNow.ToString('o');game_root=$baseline.game_root;baseline=$baselinePath;operation='Read-only SHA256 of the exact 13 previously inventoried original files; no target loading or execution';all_unchanged=$true;files=$gameHashes})
$vectors=@(foreach($item in @(
 @{text="Records\MiXeD\Entry`0TAIL/AbC.dbr";expected='b0853751'},
 @{text='FIRST\PATH';expected='4724fbb0'},
 @{text='SECOND\PATH';expected='bbc228b4'}
)) {
 $bytes=[Text.Encoding]::ASCII.GetBytes($item.text);[uint64]$value=2166136261
 foreach($byte in $bytes) {$value=(($value -bxor [uint64]$byte)*16777619) -band [uint64]4294967295}
 $hex=$value.ToString('x8');if($hex -ne $item.expected){throw 'Independent name vector mismatch'}
 [ordered]@{payload_hex=([BitConverter]::ToString($bytes).Replace('-','').ToLowerInvariant());length=$bytes.Length;expected_fnv32=$hex}
})
Save 'hash-vectors.json' ([ordered]@{method='Independent PowerShell arithmetic over literal ASCII/NUL bytes, FNV1a32 modulo 2^32; all bytes below 128 so signed and unsigned extensions coincide';vectors=$vectors})
$paths=@($priorAcceptance.source_and_binary_manifest|ForEach-Object {$_.path.Replace('.tooling/record-load-build/','.tooling/class-getter-build/')})
$paths+=@('src/task_items/RecordTableGetter.cs','src/task_items/RecordTableGetterTests.cs','src/task_items/CreatePartialObject.cs','src/task_items/CreatePartialObjectTests.cs','tools/static_analysis/rea-record-class-getters-plan.json','tools/static_analysis/rea-record-getter-support-plan.json','.tooling/class-getter-build/RecordTableGetterTests.exe','.tooling/class-getter-build/CreatePartialObjectTests.exe')
$manifest=@(foreach($path in ($paths|Sort-Object -Unique)){[ordered]@{path=$path;sha256=(Hash $path)}})
Save 'acceptance-class-getter.json' ([ordered]@{
 utc=[DateTime]::UtcNow.ToString('o');status='bounded-class-getter-and-create-partial-passed';head=$head
 static_batches=$batches;static_call_total=28;assertions=$assertions;combined_scenarios=4;new_combined_scenarios=2;existing_scenarios=@('ItemQuestDemo');own_build_directory='.tooling/class-getter-build';source_and_binary_manifest=$manifest
 connected_path='Known cache hit -> actual ordinary Class getter -> actual class dispatch -> explicit own constructor -> original name assignment -> explicit second complete-string LoadFile -> existing registration with two growth operations -> explicit own Read or Unsupported'
 create_partial_scope='Recovered caller order using own typed table, explicit complete ClassInfo metadata and constructors; known null table short circuit; empty class still dispatched; null factory with nonempty class requires uncovered diagnostic; unsupported constructed instance retained'
 ordinary_getter_scope='Complete stable unique-key valid ordered tree represented by a strict unsigned-byte sorted snapshot; results and storage identity only, not tree shape, comparison count/order or native ABI'
 query_key_semantics='First NUL prefix; stored keys and value lengths remain complete';default_semantics='Same supplied default for absent fields or zero stored value length; nonempty leading-NUL value retains storage identity'
 maximum_string_bytes=65536;maximum_fields=4096;maximum_table_payload_bytes=2097152
 binary_getter_scope='Only explicitly absent archive shortcut returns exact default without key access';binary_pointer_cache_implemented=$false;binary_numeric_getter_static_evidence_available=$true;binary_numeric_getter_implemented=$false
 cache_miss_implemented=$false;native_table_parser_implemented=$false;native_class_info_population_implemented=$false;concrete_object_read_implemented=$false;spawn_diagnostic_implemented=$false;full_finish_partial_implemented=$false;full_allocator_lifecycle_integrated=$false
 native_crt_body_or_abi_claimed=$false;own_cleanup='Monitor finally and managed exception reporting are own policy, not recovered native exception handling or rollback'
 original_binary_hashes_unchanged=13;project_configurations=$configs;rewards_cs_changed=$false;ucrt_whitelist_changed=$false
 full_offline_acceptance='Not rerun; historical 894/16 acceptance and unchanged UCRT fingerprint gate remain distinct'
 approvals='Both fixed Rea batches allowed; no capacity error, timeout, denied, pending or uncertain action; no approval retry'
 persistent_mcp_connection_claimed=$false;target_executed=$false;saves_or_steam_cloud_accessed=$false;global_configuration_changed=$false;host_reloaded_or_restarted=$false;additional_software_installed=$false;commit_or_push=$false
})
Save 'consistency-check.json' ([ordered]@{prior_core_sources=$unchanged;verified_assertions=$actual;combined_scenarios=$combined;existing_demo_passed=$demoSeen;source_manifest_hashes_verified=$true;project_configurations=$configs;native_math_provider_line_unchanged=$true;rewards_cs_unchanged=$true;original_hashes_verified=13;head=$head})
[ordered]@{status='passed';assertions=$actual;combined_scenarios=$combined;existing_demo=$demoSeen;rea_calls=28;original_hashes_unchanged=13;manifest_entries=$manifest.Count}|ConvertTo-Json
