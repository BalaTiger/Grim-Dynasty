param([ValidateSet('Download','Extract')][string]$Stage)
$ErrorActionPreference='Stop'
$ProgressPreference='SilentlyContinue'
$project=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$downloads=Join-Path $project '.tooling\downloads'
$evidence=Join-Path $project 'docs\host-migration\native-backend'
$packages=@(
 [pscustomobject]@{Name='ghidra_12.1.4_PUBLIC_20260921.zip';Url='https://github.com/NationalSecurityAgency/ghidra/releases/download/Ghidra_12.1.4_build/ghidra_12.1.4_PUBLIC_20260921.zip';Sha256='ddac49f903da9d5bac833e5cc79395098b9c33cfd3279be5f31bd00387d2d4db';Bytes=569649598L;Parent='.tooling\ghidra';Root='ghidra_12.1.4_PUBLIC'},
 [pscustomobject]@{Name='OpenJDK21U-jdk_x64_windows_hotspot_21.0.12.1_1.zip';Url='https://github.com/adoptium/temurin21-binaries/releases/download/jdk-21.0.12.1%2B1/OpenJDK21U-jdk_x64_windows_hotspot_21.0.12.1_1.zip';Sha256='f9d6e191ab098c0d416e7d588a24420a8621cd2f4720dab2459b8b7b2d2d8b4e';Bytes=205073461L;Parent='.tooling\jdk';Root='jdk-21.0.12.1+1'}
)
New-Item -ItemType Directory -Path $downloads,$evidence -Force | Out-Null
$records=@()
foreach($package in $packages){
 $archive=Join-Path $downloads $package.Name
 if(!(Test-Path -LiteralPath $archive)){
  if($Stage -ne 'Download'){throw "Missing verified download: $archive"}
  $partial=$archive+'.partial'
  $resumeDownload=Test-Path -LiteralPath $partial
  if($resumeDownload -and (Get-Item -LiteralPath $partial).Length -ge $package.Bytes){throw "Complete or oversized partial preserved for review: $partial"}
  Write-Output "Downloading $($package.Name); resume=$resumeDownload"
  Invoke-WebRequest -UseBasicParsing -Uri $package.Url -OutFile $partial -Resume:$resumeDownload -ConnectionTimeoutSeconds 30 -OperationTimeoutSeconds 60 -MaximumRetryCount 0
  $hash=(Get-FileHash -LiteralPath $partial -Algorithm SHA256).Hash.ToLowerInvariant()
  if($hash -ne $package.Sha256 -or (Get-Item -LiteralPath $partial).Length -ne $package.Bytes){throw "Download identity mismatch; preserved $partial"}
  Move-Item -LiteralPath $partial -Destination $archive
 }
 $observed=(Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
 if($observed -ne $package.Sha256 -or (Get-Item -LiteralPath $archive).Length -ne $package.Bytes){throw "Archive identity mismatch: $archive"}
 Write-Output "Verified $($package.Name)"
 $destination=Join-Path $project $package.Parent
 $installation=Join-Path $destination $package.Root
 if($Stage -eq 'Extract'){
  $destination=[IO.Path]::GetFullPath($destination)
  if(!$destination.StartsWith($project+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Extraction outside project rejected'}
  if(Test-Path -LiteralPath $destination){throw "Existing extraction directory preserved: $destination"}
  Add-Type -AssemblyName System.IO.Compression.FileSystem
  $zip=[IO.Compression.ZipFile]::OpenRead($archive)
  try{
   foreach($entry in $zip.Entries){
    $relative=$entry.FullName.Replace('/','\')
    if([IO.Path]::IsPathRooted($relative) -or $relative.Contains(':')){throw 'Unsafe absolute ZIP entry'}
    $resolved=[IO.Path]::GetFullPath((Join-Path $destination $relative))
    if(!$resolved.StartsWith($installation+'\',[StringComparison]::OrdinalIgnoreCase) -and $resolved.TrimEnd('\') -ne $installation){throw 'ZIP entry outside expected version root'}
    if((($entry.ExternalAttributes -shr 16) -band 0xf000) -eq 0xa000){throw 'ZIP symlink rejected'}
   }
  }finally{$zip.Dispose()}
  Write-Output "Extracting $($package.Name)"
  Expand-Archive -LiteralPath $archive -DestinationPath $destination
  if(!(Test-Path -LiteralPath $installation -PathType Container)){throw 'Expected installation root missing'}
 }
 $records+=[ordered]@{name=$package.Name;url=$package.Url;sha256=$observed;bytes=$package.Bytes;installation=$installation;extracted=(Test-Path -LiteralPath $installation -PathType Container)}
 $record=[ordered]@{utc=[DateTime]::UtcNow.ToString('o');stage=$Stage;packages=$records;software_executed=$false}
 [IO.File]::WriteAllText((Join-Path $evidence ($Stage.ToLowerInvariant()+'-verification.json')),($record|ConvertTo-Json -Depth 5),[Text.UTF8Encoding]::new($false))
}
