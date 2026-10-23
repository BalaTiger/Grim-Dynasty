$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
Set-Location -LiteralPath $root
$rootPrefix=$root+[IO.Path]::DirectorySeparatorChar
$checkpoint='.tooling/checkpoints/finish-partial-passed'
$destination=[IO.Path]::GetFullPath((Join-Path $root $checkpoint))
if(!$destination.StartsWith($rootPrefix,[StringComparison]::OrdinalIgnoreCase)) {throw 'Checkpoint must stay inside workspace'}
if(Test-Path -LiteralPath $destination) {throw 'Checkpoint already exists; inspect rather than overwrite'}
function Hash([string]$path) { (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() }
$priorManifestPath='docs/object-lifecycle/class-getter-validation/checkpoint-manifest.json'
$prior=Get-Content -LiteralPath $priorManifestPath -Raw | ConvertFrom-Json
# Preserve the prior checkpoint itself: compare its archived copies, not the
# current reports and runner which this phase intentionally updated.
foreach($entry in $prior.files) {
 if((Hash (Join-Path $prior.checkpoint $entry.path)) -ne $entry.sha256) {throw "Prior archived file changed: $($entry.path)"}
}
$acceptance=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'acceptance-finish-partial.json') -Raw | ConvertFrom-Json
foreach($entry in $acceptance.source_and_binary_manifest) {
 if((Hash $entry.path) -ne $entry.sha256) {throw "Accepted source/own binary changed: $($entry.path)"}
}
$paths=@($prior.files|ForEach-Object {$_.path})
$paths+=@($priorManifestPath,'src/task_items/FinishPartialObject.cs','src/task_items/FinishPartialObjectTests.cs','docs/object-lifecycle/FINISH-PARTIAL-EVIDENCE.md','docs/object-lifecycle/FINISH-PARTIAL-RESULT.md')
foreach($directory in @('docs/object-lifecycle/finish-partial-validation','.tooling/finish-partial-build')) {
 foreach($file in (Get-ChildItem -LiteralPath $directory -File)) {
  if($file.Name -ne 'checkpoint-manifest.json') {$paths+=$file.FullName.Substring($rootPrefix.Length).Replace('\','/')}
 }
}
$paths=@($paths|Sort-Object -Unique)
New-Item -ItemType Directory -Path $destination | Out-Null
$rows=@(foreach($path in $paths) {
 $source=[IO.Path]::GetFullPath((Join-Path $root $path))
 $target=[IO.Path]::GetFullPath((Join-Path $destination $path))
 if(!$source.StartsWith($rootPrefix,[StringComparison]::OrdinalIgnoreCase) -or !$target.StartsWith($destination+'\',[StringComparison]::OrdinalIgnoreCase)) {throw 'Unexpected file outside checkpoint scope'}
 $hash=Hash $source
 New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($target)) -Force | Out-Null
 Copy-Item -LiteralPath $source -Destination $target
 if((Hash $target) -ne $hash) {throw "Checkpoint copy mismatch: $path"}
 [ordered]@{path=$path;sha256=$hash;bytes=(Get-Item -LiteralPath $source).Length}
})
$manifest=[ordered]@{utc=[DateTime]::UtcNow.ToString('o');checkpoint=$checkpoint;kind='Incremental own source, own compiled artifacts, static evidence and validation checkpoint; not a complete standalone game';head=$acceptance.head;file_count=$rows.Count;all_copies_hash_verified=$true;original_binary_files_included=$false;assertions=$acceptance.assertions.total;combined_scenarios=6;existing_scenarios=1;previous_checkpoint=$prior.checkpoint;previous_checkpoint_files_verified=$prior.file_count;before_edit_copies='.tooling/checkpoints/finish-before-caller';files=$rows}
$manifestPath=Join-Path $PSScriptRoot 'checkpoint-manifest.json'
$manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
Copy-Item -LiteralPath $manifestPath -Destination (Join-Path $destination 'checkpoint-manifest.json')
if((Hash $manifestPath) -ne (Hash (Join-Path $destination 'checkpoint-manifest.json'))) {throw 'Manifest copy mismatch'}
# Recheck both current source and archive after copying; this catches concurrent edits.
foreach($entry in $rows) {
 if((Hash $entry.path) -ne $entry.sha256 -or (Hash (Join-Path $destination $entry.path)) -ne $entry.sha256) {throw "Final checkpoint verification mismatch: $($entry.path)"}
}
[ordered]@{status='passed';checkpoint=$checkpoint;files=$rows.Count;manifest_sha256=(Hash $manifestPath);previous_archive_files_verified=$prior.file_count}|ConvertTo-Json
