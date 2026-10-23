param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest

# Fixed five-name saved-ground batch. This script is prepared for separate root
# review/approval. It never compiles, loads a game DLL, or follows references.
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
if($root -cne 'C:\Users\zhuzi\Documents\Grim-Dynasty' -or $args.Count -ne 0){throw 'Fixed workspace and no arguments required'}
$planRelative='tools/static_analysis/r1-terrain-records-plan.json'
$planHash='c2dbdadce283a0f6d87dae990e8978ab1961f63a465780843c4329c04b5a2084'
$inputPath='D:\Steam\steamapps\common\Grim Dawn\database\database.arz'
$inputBytes=58529720
$inputHash='9e3532250cdb611a4cc989997c8f97fcec2d6c422063a845e6c43cd538c30603'
$toolRelative='artifacts/m1-offline-20261022-03/gd-offline.exe'
$toolHash='c4a84feb1bedbaa9b08598ff2cdd01ab0aeaa0bf7a57bbfbcf937faa38ba508b'
$nameProofRelative='docs/reverse-client/r1-mainmenu-ground-layout/inspection.json'
$nameProofHash='64b6321a8a1e3a7c0fa0648cfd6aa3ad7e2ce65e5cfd47c094b3a40ad22335a6'
$archiveProofRelative='docs/m3-evidence/content-formulas/manifest.json'
$archiveProofHash='079eef1f317dee8d7b64c1e6e94df91fe90032c1b7e6147f6b275e64eb2d2653'
$savedMemberHash='a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289'
$outputRelative='docs/reverse-client/r1-terrain-records'
$expectedNames=@(
 'records/terraintextures/earth/flat_rockdark.dbr',
 'records/terraintextures/grass/poly_grasssparserocky.dbr',
 'records/terraintextures/earth/flat_soilforestroots.dbr',
 'records/terraintextures/plants/poly_cornstalks_dead01.dbr',
 'records/terraintextures/earth/flat_leaflittermaplebrown.dbr'
)
$expectedOffsets=@(356505,356556,372999,389441,405886)
$expectedBytes=@(47,55,54,57,59)
$utf8=[Text.UTF8Encoding]::new($false,$true)

function Assert-NoReparse([string]$path){
 $cursor=[IO.Path]::GetFullPath($path)
 while($cursor){
  if(Test-Path -LiteralPath $cursor){
   if((Get-Item -LiteralPath $cursor).Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Reparse path rejected: '+$cursor)}
  }
  $parent=[IO.Directory]::GetParent($cursor);if($null -eq $parent){break};$cursor=$parent.FullName
 }
}
function Workspace-Path([string]$relative){
 if(!$relative -or [IO.Path]::IsPathRooted($relative) -or $relative.Contains(':') -or $relative.Contains('\') -or $relative -notmatch '^[a-zA-Z0-9_./-]+$'){throw 'Expected simple relative path with forward slashes'}
 if(@($relative.Split('/') | Where-Object {$_ -eq '' -or $_ -eq '.' -or $_ -eq '..'}).Count -ne 0){throw 'Empty/dot/parent path component rejected'}
 $resolved=[IO.Path]::GetFullPath((Join-Path $root $relative))
 if(!$resolved.StartsWith($root+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Path outside workspace'}
 Assert-NoReparse $resolved;return $resolved
}
function Stream-Hash([IO.Stream]$stream){
 $stream.Position=0;$sha=[Security.Cryptography.SHA256]::Create()
 try{
  $buffer=New-Object byte[] 65536
  while(($count=$stream.Read($buffer,0,$buffer.Length)) -gt 0){[void]$sha.TransformBlock($buffer,0,$count,$buffer,0)}
  [void]$sha.TransformFinalBlock((New-Object byte[] 0),0,0)
  return ([BitConverter]::ToString($sha.Hash)).Replace('-','').ToLowerInvariant()
 }finally{$sha.Dispose()}
}
function Read-PinnedJson([string]$path,[string]$expectedHash,[int]$maxBytes){
 Assert-NoReparse $path
 $stream=[IO.File]::Open($path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
 try{
  if($stream.Length -gt $maxBytes){throw ('JSON exceeds limit: '+$path)}
  if((Stream-Hash $stream) -cne $expectedHash){throw ('JSON fingerprint changed: '+$path)}
  $stream.Position=0;$reader=[IO.StreamReader]::new($stream,$utf8,$true,4096,$true)
  try{return ($reader.ReadToEnd() | ConvertFrom-Json)}finally{$reader.Dispose()}
 }finally{$stream.Dispose()}
}
function Write-Manifest($value,[string]$path){
 Assert-NoReparse $path
 [IO.File]::WriteAllText($path,($value | ConvertTo-Json -Depth 12),$utf8)
}

Assert-NoReparse $root
$plan=Read-PinnedJson (Workspace-Path $planRelative) $planHash 32768
if($plan.schema -cne 'grim-dynasty-r1-fixed-terrain-record-plan-v1'){throw 'Unexpected fixed plan schema'}
if($plan.input.path -cne $inputPath -or $plan.input.bytes -ne $inputBytes -or $plan.input.sha256 -cne $inputHash){throw 'Plan archive differs from fixed source'}
if($plan.tool.path -cne $toolRelative -or $plan.tool.sha256 -cne $toolHash -or $plan.tool.command -cne 'inspect-record'){throw 'Plan tool differs from accepted M1'}
if($plan.name_proof.path -cne $nameProofRelative -or $plan.name_proof.sha256 -cne $nameProofHash -or $plan.archive_proof.path -cne $archiveProofRelative -or $plan.archive_proof.sha256 -cne $archiveProofHash){throw 'Plan proof selection changed'}
if($plan.max_records -ne 5 -or $plan.recursive -ne $false -or $plan.follow_references -ne $false -or $plan.dlc_overlay_order_established -ne $false -or $plan.native_material_identity_established -ne $false -or $plan.output_directory -cne $outputRelative -or $plan.hash_buffer_bytes -ne 65536){throw 'Fixed scope changed'}
$requests=@($plan.records);if($requests.Count -ne 5){throw 'Exactly five fixed terrain names required'}
$archiveProof=Read-PinnedJson (Workspace-Path $archiveProofRelative) $archiveProofHash 1048576
if($archiveProof.schema -cne 'grim-dynasty-m3-direct-record-read-v1' -or $archiveProof.status -cne 'Collected' -or $archiveProof.input.path -cne $inputPath -or $archiveProof.input.bytes -ne $inputBytes -or $archiveProof.input.sha256 -cne $inputHash -or $archiveProof.sha256_before -cne $inputHash -or $archiveProof.sha256_after -cne $inputHash -or $archiveProof.tool_sha256_after -cne $toolHash){throw 'Saved proof does not establish source/tool identity'}
$nameProof=Read-PinnedJson (Workspace-Path $nameProofRelative) $nameProofHash 1048576
if($nameProof.schema -cne 'grim-dynasty.saved-ground-layout.v1' -or $nameProof.status -cne 'passed-conditional-saved-layout-only' -or $nameProof.source.sha256_before -cne $savedMemberHash -or $nameProof.source.sha256_after -cne $savedMemberHash -or $nameProof.source.stable -ne $true -or @($nameProof.terrain_types).Count -ne 5){throw 'Saved ground name proof changed'}
for($index=0;$index -lt 5;$index++){
 $request=$requests[$index];$entry=$nameProof.terrain_types[$index]
 if($request.terrain_index -ne $index -or $request.record_name -cne $expectedNames[$index] -or $request.output_file -cne ('terrain-'+$index+'.json') -or $request.name_member_offset -ne $expectedOffsets[$index] -or $request.name_bytes -ne $expectedBytes[$index]){throw 'Plan name/order/range differs from fixed saved data'}
 if($entry.index -ne $index -or $entry.name_ascii -cne $request.record_name -or $entry.name_member_offset -ne $request.name_member_offset -or $entry.name_bytes -ne $request.name_bytes -or $entry.ascii_only -ne $true -or $entry.nul_bytes -ne 0){throw 'Name proof does not establish exact selected record'}
}
$outputPath=Workspace-Path $outputRelative
if(Test-Path -LiteralPath $outputPath){throw 'Output exists: preserve prior evidence and do not implicitly rerun'}
$toolPath=Workspace-Path $toolRelative
Assert-NoReparse $inputPath
$toolStream=$null;$inputStream=$null;$manifest=$null;$manifestPath=$null;$caught=$null
try{
 # Read-only shared-read handles prevent writes/deletion while five calls run.
 # The ARZ is hashed once before and once after the whole batch, not per record.
 $toolStream=[IO.File]::Open($toolPath,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
 if((Stream-Hash $toolStream) -cne $toolHash){throw 'Accepted M1 executable fingerprint changed'}
 $inputStream=[IO.File]::Open($inputPath,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
 if($inputStream.Length -ne $inputBytes){throw 'Original ARZ size changed'}
 $before=Stream-Hash $inputStream
 if($before -cne $inputHash){throw 'Original ARZ fingerprint changed; no record command started'}
 Assert-NoReparse $outputPath
 [void](New-Item -ItemType Directory -Path $outputPath)
 $manifestPath=Join-Path $outputPath 'manifest.json'
 $manifest=[ordered]@{
  schema='grim-dynasty-r1-fixed-terrain-record-read-v1';status='Running';started_utc=[DateTime]::UtcNow.ToString('o')
  plan=$planRelative;plan_sha256=$planHash;script='tools/static_analysis/read_r1_terrain_records.ps1'
  script_sha256=(Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash.ToLowerInvariant()
  name_proof=$plan.name_proof;archive_proof=$plan.archive_proof;input=$plan.input;tool=$plan.tool;output_directory=$outputRelative
  sha256_before=$before;sha256_after=$null;tool_sha256_after=$null;hash_buffer_bytes=65536
  max_records=5;recursive=$false;follow_references=$false;invocations_started=0;successful_decodes=0;records=@()
  dlc_overlay_order_established=$false;native_material_identity_established=$false;gameplay_accepted=$false
  scope='Five exact saved terrain names in one pinned base ARZ through the accepted own decoder. No recursive lookup, constructor/ABI claim, game DLL load, native game execution, saves/cloud, compiler, backend, installation, or security change.'
 }
 Write-Manifest $manifest $manifestPath
 foreach($request in $requests){
  $arguments=@('inspect-record',$inputPath,$request.record_name)
  $entry=[ordered]@{terrain_index=$request.terrain_index;record_name=$request.record_name;output_file=$request.output_file;arguments=$arguments;status='Attempting';exit_code=$null;output_sha256=$null}
  $manifest.records+=@($entry);$manifest.invocations_started++;Write-Manifest $manifest $manifestPath
  # Fixed argument array; names are never interpolated into evaluated shell code.
  $previousPreference=$ErrorActionPreference
  try{$ErrorActionPreference='Continue';$nativeOutput=@(& $toolPath @arguments 2>&1);$nativeExit=$LASTEXITCODE}finally{$ErrorActionPreference=$previousPreference}
  $outputFile=Join-Path $outputPath $request.output_file
  Assert-NoReparse $outputFile
  $raw=[string]::Join([Environment]::NewLine,[string[]]$nativeOutput)+[Environment]::NewLine
  # Preserve returned evidence before validation; never parse an oversized JSON.
  [IO.File]::WriteAllText($outputFile,$raw,$utf8)
  $entry.exit_code=$nativeExit;$entry.output_sha256=(Get-FileHash -LiteralPath $outputFile -Algorithm SHA256).Hash.ToLowerInvariant();$entry.status='Returned'
  $entry['output_bytes']=$utf8.GetByteCount($raw)
  Write-Manifest $manifest $manifestPath
  if($entry.output_bytes -gt 4194304){throw 'Accepted decoder output exceeded fixed JSON limit; exact output retained'}
  if($nativeExit -ne 0){throw ('M1 record command exited '+$nativeExit+'; exact output retained: '+$request.output_file)}
  $decoded=$raw | ConvertFrom-Json
  if($decoded.schema -cne 'grim-dynasty-offline-v1' -or $decoded.command -cne 'inspect-record' -or $decoded.status -cne 'Known' -or $decoded.data.input -cne $inputPath -or $decoded.data.record.name -cne $request.record_name){throw 'M1 output does not match exact fixed request'}
  $entry.status='Decoded';$entry['class']=$decoded.data.record.class;$entry['body_sha256']=$decoded.data.record.body_sha256
  $manifest.successful_decodes++;Write-Manifest $manifest $manifestPath
 }
 $manifest.status='DecodedAwaitingFinalHashes'
}catch{
 $caught=$_
 if($null -ne $manifest){$manifest.status='FailedOrIncomplete';$manifest['error']=$_.Exception.Message}
}finally{
 try{
  if($null -ne $inputStream -and $null -ne $manifest){
   $manifest.sha256_after=Stream-Hash $inputStream;$manifest.tool_sha256_after=Stream-Hash $toolStream
   if($inputStream.Length -ne $inputBytes -or $manifest.sha256_after -cne $inputHash -or $manifest.tool_sha256_after -cne $toolHash){throw 'Final original/tool fingerprint verification failed'}
   if($manifest.status -ceq 'DecodedAwaitingFinalHashes'){$manifest.status='Collected'}
  }
 }catch{
  if($null -eq $caught){$caught=$_}
  if($null -ne $manifest){$manifest.status='FailedOrIncomplete';$manifest['final_hash_error']=$_.Exception.Message}
 }finally{
  if($null -ne $inputStream){$inputStream.Dispose()}
  if($null -ne $toolStream){$toolStream.Dispose()}
  if($null -ne $manifest){$manifest['handles_closed']=$true;$manifest['finished_utc']=[DateTime]::UtcNow.ToString('o');Write-Manifest $manifest $manifestPath}
 }
}
if($null -ne $caught){throw $caught}
Write-Output ('R1_TERRAIN_RECORD_MANIFEST='+$manifestPath)
