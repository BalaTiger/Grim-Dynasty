# Read-only: no save paths, no Steam settings, no extraction or program launch.
[CmdletBinding()]
param([string]$SteamRoot = 'D:\Steam')
$ErrorActionPreference = 'Stop'
$manifest = Join-Path $SteamRoot 'steamapps\appmanifest_219990.acf'
$raw = Get-Content -LiteralPath $manifest -Raw
if ($raw -notmatch '"appid"\s+"219990"') { throw 'Unexpected app manifest' }
if ($raw -notmatch '"installdir"\s+"([^"\\/:]+)"') { throw 'Missing or unsafe installdir' }
$game = Join-Path (Join-Path $SteamRoot 'steamapps\common') $Matches[1]
$build = if ($raw -match '"buildid"\s+"(\d+)"') { $Matches[1] } else { 'unknown' }
[pscustomobject]@{Manifest=$manifest;Game=$game;Build=$build;Exists=(Test-Path -LiteralPath $game)}
foreach ($name in @('AssetManager.exe','DBREditor.exe','Editor.exe','QuestEditor.exe','ConversationEditor.exe','ArchiveTool.exe','Grim Dawn Modding Guide.pdf','database\database.arz','database\templates.arc','gdx1\database\GDX1.arz','gdx2\database\GDX2.arz','gdx3\database\GDX3.arz')) {
  $p = Join-Path $game $name
  if (Test-Path -LiteralPath $p -PathType Leaf) {
    $f = Get-Item -LiteralPath $p
    [pscustomobject]@{RelativePath=$name;Bytes=$f.Length;SHA256=(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash}
  } else { [pscustomobject]@{RelativePath=$name;Missing=$true} }
}