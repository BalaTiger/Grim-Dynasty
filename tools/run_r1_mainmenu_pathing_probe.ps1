# Execute only after the parent obtains the normal approval for this exact script.
# No compiler, test executable, original game, backend or policy changes.
param()
$ErrorActionPreference = 'Stop'
$taskRoot = 'C:\Users\zhuzi\Documents\Grim-Dynasty'
$probe = Join-Path $taskRoot 'tools\static_analysis\probe_mainmenu_pathing_data.ps1'
$probeSha256 = 'bc18530c982e3fd4508386fb1ea26aabffd043d3bc86b4a26fb3946542549df0'
if ($args.Count -ne 0) { throw 'This fixed probe accepts no arguments' }
if ((Get-Location).Path -ne $taskRoot) { throw 'Fixed project working directory required' }
if ($PSVersionTable.PSVersion.Major -ne 5 -or -not [Environment]::Is64BitProcess) { throw 'Use the established 64-bit Windows PowerShell 5.1 host' }
if ((Get-FileHash -LiteralPath $probe -Algorithm SHA256).Hash.ToLowerInvariant() -ne $probeSha256) { throw 'Probe source differs from reviewed frozen SHA256' }
. $probe
Invoke-GrimDynastyMainmenuPathingProbe
