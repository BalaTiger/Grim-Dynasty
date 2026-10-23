param([Parameter(Mandatory=$true)][string]$PackageDirectory)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$package=[IO.Path]::GetFullPath($PackageDirectory)
if (!$package.StartsWith($root+'\',[StringComparison]::OrdinalIgnoreCase)) {throw 'Project package required'}
$exe=Join-Path $package 'gd-offline.exe'
$samples=Join-Path $package 'samples'
$validation=Join-Path $package 'validation'
if (Test-Path -LiteralPath $validation) {throw 'Validation already exists; preserve prior results'}
New-Item -ItemType Directory -Path $validation | Out-Null
$script:checks=0
$script:cases=New-Object 'System.Collections.Generic.List[object]'
function Check([bool]$ok,[string]$label) {if (!$ok) {throw "Acceptance failed: $label"};$script:checks++}
function Invoke-Case([string]$name,[string[]]$Arguments,[int]$ExpectedExit,[string]$Status) {
 $output=@(& $exe @Arguments)
 $code=$LASTEXITCODE
 $raw=$output -join "`n"
 [IO.File]::WriteAllText((Join-Path $validation ($name+'.json')),$raw,[Text.UTF8Encoding]::new($false))
 $value=$raw | ConvertFrom-Json
 Check ($code -eq $ExpectedExit) "$name exit=$ExpectedExit"
 Check ($value.schema -eq 'grim-dynasty-offline-v1' -and $value.status -eq $Status) "$name schema/status"
 $script:cases.Add([ordered]@{name=$name;exit_code=$code;status=$value.status})
 return $value
}
$before=@{}
Get-ChildItem -LiteralPath $samples -File | ForEach-Object {$before[$_.Name]=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}
$expected=Get-Content -LiteralPath (Join-Path $samples 'expected.json') -Raw | ConvertFrom-Json
$r=Invoke-Case 'help' @('help') 0 'Known'
Check ($r.data.commands.Count -ge 6) 'documented unified commands'
$r=Invoke-Case 'capabilities' @('capabilities') 0 'Known'
Check (($r.data.unsupported -join ' ') -match 'XP/CRT') 'XP provider scope explicit'
$arz=Join-Path $samples 'm1-records.arz'
$qst=Join-Path $samples 'm1-ration-cycle.qst'
$arc=Join-Path $samples 'm1-quests.arc'
$r=Invoke-Case 'record' @('inspect-record',$arz,$expected.record_name) 0 'Known'
Check ($r.data.record.class -eq $expected.record_class -and $r.data.record.fields.health -eq 125 -and $r.data.record.fields.label -eq 'Original ration') 'ARZ decoded original fields'
Check ($r.data.record.fields.speed -eq 1.5 -and $r.data.float_bits.negative_zero -eq '0x80000000') 'binary32 values and negative-zero bits'
Check ((Get-Content -LiteralPath (Join-Path $validation 'record.json') -Raw) -match '"negative_zero":-0\.0') 'negative zero numeric JSON'
$normalized=Invoke-Case 'record-normalized' @('inspect-record',$arz,'MING\ITEMS\MILITARY_RATION.DBR') 0 'Known'
Check ($normalized.data.record.body_sha256 -eq $r.data.record.body_sha256) 'normalized record lookup same payload'
$r=Invoke-Case 'quest' @('inspect-quest',$qst) 0 'Known'
$quest=$r.data.quest
Check ($quest.uid -eq 3 -and $quest.tasks.Count -eq 2 -and $quest.graph_end -eq $expected.quest_graph_end) 'QST graph'
for($i=0;$i -lt 2;$i++) {
 Check ($quest.tasks[$i].text.en.text -eq $expected.tasks[$i].name) "QST task $i localized name"
 Check ($quest.tasks[$i].objectives[0].conditions.conditions[0].type -eq 'HasItem') "QST task $i condition"
 Check ($quest.tasks[$i].on_complete[0].actions[0].parameters.amount -eq $expected.tasks[$i].give_money) "QST task $i money"
}
$r=Invoke-Case 'quest-arc' @('inspect-quest',$arc,$expected.arc_member) 0 'Known'
Check ($r.data.quest.sha256 -eq $quest.sha256) 'ARC payload equals standalone QST'
$r=Invoke-Case 'roundtrip' @('replay','inventory-roundtrip') 0 'Known'
Check ($r.data.steps_completed -eq 11 -and $r.data.initial.money -eq 50 -and $r.data.final.money -eq 165) 'actual task money progression'
Check ($r.data.trace[3].after.money -eq 125 -and $r.data.trace[7].after.money -eq 165) 'both rewards applied at completion'
Check (($r.data.final.reward_task_uids -join ',') -eq '1,2' -and ($r.data.final.inventory_ids -join ',') -eq '100' -and $r.data.final.same_object_identity) 'reward history and shared item identity'
Check ($r.data.trace[8].after.money -eq 165 -and $r.data.trace[8].after.engine_add_count -eq 9 -and $r.data.final.engine_add_count -eq 10) 'duplicate add keeps reward but preserves pre-dedup counter'
Check (($r.data.final.attached_16 -notcontains $true) -and ($r.data.final.attached_17 -notcontains $true) -and $r.data.final.add_last_delivered -eq 0) 'completed subscriptions detached'
$referenceFinal=$r.data.final | ConvertTo-Json -Depth 12 -Compress
$r=Invoke-Case 'roundtrip-json' @('replay',(Join-Path $samples 'inventory-roundtrip.json')) 0 'Known'
Check (($r.data.final | ConvertTo-Json -Depth 12 -Compress) -eq $referenceFinal) 'file-driven replay equals named fixture'
$r=Invoke-Case 'unsupported-prefix' @('replay',(Join-Path $samples 'unsupported-prefix.json')) 3 'Unsupported'
Check ($r.data.steps_completed -eq 4 -and $r.data.stopped_at -eq 4 -and $r.data.final.money -eq 125 -and $r.data.trace[4].observed_state_unchanged) 'unknown action preserves successful prefix'
$r=Invoke-Case 'unsupported-destroy' @('replay','inventory-destroy-unsupported') 3 'Unsupported'
Check ($r.reason -eq 'FullTakeActorMethodsOrDestructionNotRecovered' -and $r.data.trace[0].observed_state_unchanged -and $r.data.final.same_object_identity) 'actual unsupported destruction path preserves identity/state'
$r=Invoke-Case 'missing-record' @('inspect-record',$arz,'missing.dbr') 2 'InvalidInput'
$r=Invoke-Case 'missing-file' @('inspect-quest',(Join-Path $validation 'absent.qst')) 2 'InvalidInput'
$badVersion=Join-Path $validation 'version-10.qst'
$bytes=[IO.File]::ReadAllBytes($qst);$bytes[4]=10;[IO.File]::WriteAllBytes($badVersion,$bytes)
$r=Invoke-Case 'unsupported-version' @('inspect-quest',$badVersion) 3 'Unsupported'
$truncated=Join-Path $validation 'truncated.qst';[IO.File]::WriteAllBytes($truncated,[byte[]](81,83,84,50))
$r=Invoke-Case 'truncated' @('inspect-quest',$truncated) 2 'InvalidInput'
$invalid=Join-Path $validation 'invalid.json';[IO.File]::WriteAllText($invalid,'{"schema":"wrong","fixture":"inventory-cycle","steps":[]}')
$r=Invoke-Case 'invalid-replay' @('replay',$invalid) 2 'InvalidInput'
$r=Invoke-Case 'unknown-command' @('not-a-command') 2 'InvalidInput'
$r=Invoke-Case 'unc-rejected' @('inspect-quest','\\invalid-host\share\fixture.qst') 2 'InvalidInput'
Check ($r.reason -match 'local|UNC|device') 'UNC rejected before file access'
$spaceDirectory=Join-Path $validation 'path with spaces';New-Item -ItemType Directory -Path $spaceDirectory | Out-Null
$spaced=Join-Path $spaceDirectory 'sample quest.qst';Copy-Item -LiteralPath $qst -Destination $spaced
Push-Location -LiteralPath $validation
try {$r=Invoke-Case 'foreign-cwd-spaces' @('inspect-quest',$spaced) 0 'Known'} finally {Pop-Location}
Check ($r.data.quest.sha256 -eq $quest.sha256) 'no hardcoded working directory/input root'
$unicodeName=([string][char]0x4EFB)+([string][char]0x52A1)+'.qst'
$unicodePath=Join-Path $spaceDirectory $unicodeName
Copy-Item -LiteralPath $qst -Destination $unicodePath
$r=Invoke-Case 'unicode-path' @('inspect-quest',$unicodePath) 0 'Known'
Check ($r.data.input -eq $unicodePath -and $r.data.quest.sha256 -eq $quest.sha256) 'Unicode path survives PowerShell JSON transport'
Get-ChildItem -LiteralPath $samples -File | ForEach-Object {Check ((Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash -eq $before[$_.Name]) ('read-only fixture '+$_.Name)}
$report=[ordered]@{schema='grim-dynasty-m1-acceptance-v1';status='Passed';completed_utc=[DateTime]::UtcNow.ToString('o');cases=$script:cases;case_count=$script:cases.Count;assertions=$script:checks;
 platform=[Environment]::OSVersion.VersionString;clr=[Environment]::Version.ToString();powershell=$PSVersionTable.PSVersion.ToString();
 scope='New CLI integration acceptance over reused own modules and original project-authored fixtures. No old standalone suites rerun. No original game files read or run.'}
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $package 'acceptance.json') -Encoding UTF8
Write-Output ("M1_ACCEPTANCE cases={0} assertions={1} Passed" -f $script:cases.Count,$script:checks)
