param(
 [Parameter(Mandatory=$true)][string]$PlanPath,
 [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedPlanSha256
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest

# Reviewed direct-reference reader. No compiler, recursive traversal, or game execution.
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
if($root -cne 'C:\Users\zhuzi\Documents\Grim-Dynasty'){throw 'This reader is pinned to the reviewed workspace'}
$inputPath='D:\Steam\steamapps\common\Grim Dawn\database\database.arz'
$inputBytes=58529720
$inputHash='9e3532250cdb611a4cc989997c8f97fcec2d6c422063a845e6c43cd538c30603'
$toolRelative='artifacts/m1-offline-20261022-03/gd-offline.exe'
$toolHash='c4a84feb1bedbaa9b08598ff2cdd01ab0aeaa0bf7a57bbfbcf937faa38ba508b'
$utf8=[Text.UTF8Encoding]::new($false,$true)

function Assert-NoReparse([string]$path){
 for($cursor=$path;$cursor;$cursor=Split-Path -Parent $cursor){
  if(Test-Path -LiteralPath $cursor){
   if((Get-Item -LiteralPath $cursor).Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Reparse path rejected: '+$cursor)}
  }
 }
}
function Workspace-Path([string]$relative,[string]$prefix){
 if(!$relative -or [IO.Path]::IsPathRooted($relative) -or $relative.Contains(':') -or $relative.Contains('\') -or $relative -notmatch '^[a-zA-Z0-9_./-]+$'){throw 'Expected a simple workspace-relative path using forward slashes'}
 if(@($relative.Split('/') | Where-Object {$_ -eq '' -or $_ -eq '.' -or $_ -eq '..'}).Count -ne 0){throw 'Empty, dot, or parent path component rejected'}
 if(!$relative.StartsWith($prefix,[StringComparison]::Ordinal)){throw ('Path outside reviewed prefix: '+$prefix)}
 $resolved=[IO.Path]::GetFullPath((Join-Path $root $relative))
 if(!$resolved.StartsWith($root+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Path outside workspace'}
 Assert-NoReparse $resolved
 return $resolved
}
function Stream-Hash([IO.Stream]$stream){
 $stream.Position=0
 $sha=[Security.Cryptography.SHA256]::Create()
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
  if($stream.Length -gt $maxBytes){throw ('JSON exceeds reviewed byte limit: '+$path)}
  if((Stream-Hash $stream) -cne $expectedHash){throw ('JSON fingerprint changed: '+$path)}
  $stream.Position=0
  $reader=[IO.StreamReader]::new($stream,$utf8,$true,4096,$true)
  try{return ($reader.ReadToEnd() | ConvertFrom-Json)}finally{$reader.Dispose()}
 }finally{$stream.Dispose()}
}
function Exact-Property($object,[string]$name){
 $matches=@($object.PSObject.Properties | Where-Object {$_.Name -ceq $name})
 if($matches.Count -ne 1){throw ('Missing or ambiguous exact property: '+$name)}
 return $matches[0].Value
}
function Write-Manifest($manifest,[string]$path){
 [IO.File]::WriteAllText($path,($manifest | ConvertTo-Json -Depth 12),$utf8)
}

Assert-NoReparse $root
$planFile=Workspace-Path $PlanPath 'docs/m3-evidence/plans/'
$plan=Read-PinnedJson $planFile $ExpectedPlanSha256 32768
if($plan.schema -cne 'grim-dynasty-m3-direct-record-plan-v1'){throw 'Unsupported plan schema'}
if($plan.input.path -cne $inputPath -or $plan.input.sha256 -cne $inputHash -or $plan.input.bytes -ne $inputBytes){throw 'Plan input differs from reviewed ARZ'}
if($plan.tool.path -cne $toolRelative -or $plan.tool.sha256 -cne $toolHash){throw 'Plan tool differs from accepted M1 executable'}
if($plan.max_records -ne 2 -or $plan.recursive -ne $false){throw 'Plan must declare maximum two direct records and no recursion'}
$requests=@($plan.records)
if($requests.Count -lt 1 -or $requests.Count -gt 2){throw 'Plan must request one or two records'}
$outputPath=Workspace-Path $plan.output_directory 'docs/m3-evidence/'
if(Test-Path -LiteralPath $outputPath){throw 'Output directory exists; refusing overwrite or implicit rerun'}
$proofPath=Workspace-Path $plan.proof.path 'docs/m3-evidence/'
if($plan.proof.sha256 -notmatch '^[0-9a-f]{64}$'){throw 'Invalid proof fingerprint'}
$proof=Read-PinnedJson $proofPath $plan.proof.sha256 2097152
if($plan.proof.schema -cne 'grim-dynasty-m3-content-slice-v1' -or $proof.schema -cne $plan.proof.schema -or $proof.status -cne 'Collected' -or $proof.sha256_before -cne $inputHash -or $proof.sha256_after -cne $inputHash){throw 'Proof does not establish the reviewed source archive'}
$seenNames=@();$seenFiles=@()
foreach($request in $requests){
 if($request.record_name -isnot [string] -or !$request.record_name.StartsWith('records/',[StringComparison]::Ordinal) -or !$request.record_name.EndsWith('.dbr',[StringComparison]::Ordinal) -or $request.record_name -notmatch '^[a-zA-Z0-9_/&. -]+$' -or $request.record_name.Contains('..')){throw 'Invalid direct record name'}
 if($request.output_file -notmatch '^[a-z0-9][a-z0-9-]{0,63}\.json$' -or $request.output_file -eq 'manifest.json'){throw 'Invalid record output filename'}
 if($seenNames -contains $request.record_name -or $seenFiles -contains $request.output_file){throw 'Duplicate record or output filename'}
 $seenNames+=$request.record_name;$seenFiles+=$request.output_file
 $sources=@($proof.decoded_records | Where-Object {$_.name -ceq $request.source.record_name})
 if($sources.Count -ne 1){throw 'Proof source record must match exactly once'}
 $source=$sources[0]
 if($source.class -cne $request.source.class -or $source.body_sha256 -cne $request.source.body_sha256){throw 'Proof source class/body fingerprint differs'}
 $reference=Exact-Property $source.fields $request.source.field
 if($reference -isnot [string] -or $reference -cne $request.source.value -or $reference -cne $request.record_name){throw 'Target is not the exact saved direct-reference field value'}
}

$toolPath=Workspace-Path $toolRelative 'artifacts/'
Assert-NoReparse $inputPath
$toolStream=$null;$inputStream=$null;$manifest=$null;$manifestPath=$null;$caught=$null
try{
 # Both handles remain open with FileShare.Read, preventing writers during the run.
 $toolStream=[IO.File]::Open($toolPath,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
 if((Stream-Hash $toolStream) -cne $toolHash){throw 'Accepted M1 executable fingerprint changed'}
 $inputStream=[IO.File]::Open($inputPath,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
 if($inputStream.Length -ne $inputBytes){throw 'Original ARZ size changed'}
 $before=Stream-Hash $inputStream
 if($before -cne $inputHash){throw 'Original ARZ fingerprint changed; no record command was started'}
 [void](New-Item -ItemType Directory -Path $outputPath)
 $manifestPath=Join-Path $outputPath 'manifest.json'
 $manifest=[ordered]@{
  schema='grim-dynasty-m3-direct-record-read-v1';status='Running';started_utc=[DateTime]::UtcNow.ToString('o');
  plan=$PlanPath;plan_sha256=$ExpectedPlanSha256;script='tools/static_analysis/read_m3_records.ps1';
  script_sha256=(Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash.ToLowerInvariant();
  proof=$plan.proof;input=$plan.input;tool=$plan.tool;output_directory=$plan.output_directory;
  sha256_before=$before;sha256_after=$null;tool_sha256_after=$null;hash_buffer_bytes=65536;
  max_records=2;recursive=$false;invocations_started=0;successful_decodes=0;records=@();
  scope='Only pinned own M1 inspect-record invocations for saved direct references. No formula evaluation, recursive follow-up, extraction of other resources, native game execution, saves, installation, compilation, or security changes.'
 }
 Write-Manifest $manifest $manifestPath
 foreach($request in $requests){
  $arguments=@('inspect-record',$inputPath,$request.record_name)
  $entry=[ordered]@{record_name=$request.record_name;source=$request.source;output_file=$request.output_file;arguments=$arguments;status='Attempting';exit_code=$null;output_sha256=$null}
  $manifest.records+=@($entry);$manifest.invocations_started++
  Write-Manifest $manifest $manifestPath
  # The three arguments are passed separately; record text is never evaluated as shell code.
  # PS 5.1 can turn captured native stderr into terminating NativeCommandError
  # under Stop. Capture it before judging the result; restore this local preference.
  $previousPreference=$ErrorActionPreference
  try{
   $ErrorActionPreference='Continue'
   $nativeOutput=@(& $toolPath @arguments 2>&1)
   $nativeExit=$LASTEXITCODE
  }finally{$ErrorActionPreference=$previousPreference}
  $outputFile=Join-Path $outputPath $request.output_file
  $raw=[string]::Join([Environment]::NewLine,[string[]]$nativeOutput)+[Environment]::NewLine
  [IO.File]::WriteAllText($outputFile,$raw,$utf8)
  $entry.exit_code=$nativeExit
  $entry.output_sha256=(Get-FileHash -LiteralPath $outputFile -Algorithm SHA256).Hash.ToLowerInvariant()
  $entry.status='Returned'
  Write-Manifest $manifest $manifestPath
  if($nativeExit -ne 0){throw ('M1 record command exited '+$nativeExit+'; preserved output: '+$request.output_file)}
  $decoded=$raw | ConvertFrom-Json
  if($decoded.schema -cne 'grim-dynasty-offline-v1' -or $decoded.command -cne 'inspect-record' -or $decoded.status -cne 'Known' -or $decoded.data.input -cne $inputPath -or $decoded.data.record.name -cne $request.record_name){throw 'M1 output does not match the exact approved record command'}
  $entry.status='Decoded';$entry['class']=$decoded.data.record.class;$entry['body_sha256']=$decoded.data.record.body_sha256
  $manifest.successful_decodes++
  Write-Manifest $manifest $manifestPath
 }
 $manifest.status='DecodedAwaitingFinalHashes'
}catch{
 $caught=$_
 if($null -ne $manifest){$manifest.status='FailedOrIncomplete';$manifest['error']=$_.Exception.Message}
}finally{
 try{
  if($null -ne $inputStream -and $null -ne $manifest){
   $manifest.sha256_after=Stream-Hash $inputStream
   $manifest.tool_sha256_after=Stream-Hash $toolStream
   if($inputStream.Length -ne $inputBytes -or $manifest.sha256_after -cne $inputHash -or $manifest.tool_sha256_after -cne $toolHash){throw 'Final original/tool fingerprint verification failed'}
   if($manifest.status -ceq 'DecodedAwaitingFinalHashes'){$manifest.status='Collected'}
  }
 }catch{
  if($null -eq $caught){$caught=$_}
  if($null -ne $manifest){$manifest.status='FailedOrIncomplete';$manifest['final_hash_error']=$_.Exception.Message}
 }finally{
  if($null -ne $inputStream){$inputStream.Dispose()}
  if($null -ne $toolStream){$toolStream.Dispose()}
  if($null -ne $manifest){$manifest['finished_utc']=[DateTime]::UtcNow.ToString('o');Write-Manifest $manifest $manifestPath}
 }
}
if($null -ne $caught){throw $caught}
Write-Output ('M3_RECORD_MANIFEST='+$manifestPath)
