param([string]$Target,[string]$Pattern='Quest|Scriptable|GameReader|GameWriter',[string]$Output='managed-metadata.json',[string]$ResourceOut)
$ErrorActionPreference='Stop'
Add-Type -Path "$PSScriptRoot\ManagedMetadata.cs" -ReferencedAssemblies @('System.Reflection.Metadata','System.Collections.Immutable','System.Text.Json','System.Reflection.Emit','System.Reflection.Primitives','System.Runtime','System.Collections','System.Linq','System.IO.Compression','System.Text.RegularExpressions')
$result=[ManagedMetadata]::Dump($Target,$Pattern,$ResourceOut)
[System.IO.File]::WriteAllText((Join-Path $PSScriptRoot $Output),$result)
Write-Output "Saved $Output"

