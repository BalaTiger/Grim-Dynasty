$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$reader=Join-Path $root 'artifacts\m1-offline-20261022-03\gd-offline.exe'
$source='D:\Steam\steamapps\common\Grim Dawn\database\database.arz'
$directory=Join-Path $root 'docs\m3-evidence'
$output=Join-Path $directory 'sword-record.json'
$summary=Join-Path $directory 'sword-read-summary.json'
if(Test-Path -LiteralPath $output){throw 'Preserve existing sword evidence'}
if(Test-Path -LiteralPath $summary){throw 'Preserve existing summary'}
if((Get-FileHash -LiteralPath $reader -Algorithm SHA256).Hash -ne 'C4A84FEB1BEDBAA9B08598FF2CDD01AB0AEAA0BF7A57BBFBCF937FAA38BA508B'){throw 'Accepted own reader changed'}
foreach($path in @($root,(Join-Path $root 'docs'),$directory)){if(Test-Path -LiteralPath $path){if((Get-Item -LiteralPath $path).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Reparse output ancestor'}}}
$before=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
$result=@(& $reader inspect-record $source 'records/items/gearweapons/swords1h/b000_sword.dbr')
$code=$LASTEXITCODE
$after=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
if($before -ne $after){throw 'Original archive hash changed during read'}
if($code -ne 0){throw ('Own reader returned '+$code+': '+($result -join ' '))}
$json=$result -join "`n";$decoded=$json|ConvertFrom-Json
if($decoded.status -ne 'Known' -or $decoded.data.record.body_sha256 -ne '6ba63bf042d53170a1e3d6e9a59fc121ae4fe29d8aef825b9958be212644f9e1'){throw 'Selected record does not match prior static evidence'}
New-Item -ItemType Directory -Path $directory -Force|Out-Null
[IO.File]::WriteAllText($output,$json,[Text.UTF8Encoding]::new($false))
$fields=@($decoded.data.record.fields.PSObject.Properties)
$selected=[ordered]@{}
foreach($field in $fields){if($field.Name -match '^(offensivePhysical|weaponDamage|weaponClassification|itemCost|itemLevel|levelRequirement|characterBaseAttackSpeed|strengthRequirement|dexterityRequirement|intelligenceRequirement)'){$selected[$field.Name]=$field.Value}}
[ordered]@{completed_utc=[DateTime]::UtcNow.ToString('o');operation='One explicit ARZ record via accepted own read-only decoder; no original executable used';source=$source;source_sha256_before=$before.ToLowerInvariant();source_sha256_after=$after.ToLowerInvariant();record=$decoded.data.record.name;body_sha256=$decoded.data.record.body_sha256;field_count=$fields.Count;selected_fields=$selected;boundary='Data values only; no combat formula, merchant pricing or equipment semantics established'}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $summary -Encoding UTF8
Write-Output ('M3_SWORD_RECORD fields='+$fields.Count+' original_hash_unchanged=True')
