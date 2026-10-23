param([Parameter(Mandatory=$true)][string]$PackageDirectory)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$package=[IO.Path]::GetFullPath($PackageDirectory)
if(!$package.StartsWith($root+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Own project package required'}
$exe=Join-Path $package 'gd-session.exe'
$saveRoot=Join-Path $package 'runtime-data/saves'
$save=Join-Path $saveRoot 'hero.save'
$validation=Join-Path $package 'validation/cli'
if(Test-Path -LiteralPath $validation){throw 'Preserve previous acceptance'}
New-Item -ItemType Directory -Path $validation | Out-Null
$script:checks=0;$script:cases=New-Object 'System.Collections.Generic.List[object]'
function Check([bool]$ok,[string]$why){if(!$ok){throw ('M2 acceptance failed: '+$why)};$script:checks++}
function Run([string]$name,[string[]]$Arguments,[int]$ExpectedExit,[string]$Status){
 $out=@(& $exe @Arguments);$code=$LASTEXITCODE;$raw=$out -join "`n"
 [IO.File]::WriteAllText((Join-Path $validation ($name+'.json')),$raw,[Text.UTF8Encoding]::new($false))
 $r=$raw|ConvertFrom-Json
 Check ($code -eq $ExpectedExit) ($name+' exit')
 Check ($r.schema -eq 'grim-dynasty-session-v1' -and $r.status -eq $Status) ($name+' status/schema')
 $script:cases.Add([ordered]@{name=$name;exit_code=$code;status=$r.status});return $r
}
$r=Run 'help' @('help') 0 'Known'
$name='Recruit '+[char]0x8FB9+[char]0x519B
$r=Run 'new' @('new','hero',$name) 0 'Known'
$identity=$r.data.character_id
Check ($identity.Length -eq 32 -and $r.data.name -eq $name -and $r.data.revision -eq 1 -and $r.data.money -eq 50) 'new character/Unicode/revision/money'
Check ($r.data.same_object_identity -and $r.data.shared_world_identity -and !$r.data.delivery_accepted) 'new state ownership'
$hash=(Get-FileHash -LiteralPath $save -Algorithm SHA256).Hash
$r=Run 'out-of-order' @('act','hero','deliver') 3 'Unsupported'
Check ((Get-FileHash -LiteralPath $save -Algorithm SHA256).Hash -eq $hash) 'out-of-order action does not save'
$r=Run 'accept' @('act','hero','accept-delivery') 0 'Known'
Check ($r.data.changed -and $r.data.state.delivery_accepted -and !$r.data.state.delivery_completed -and $r.data.state.money -eq 50) 'accept task without reward'
$r=Run 'deliver' @('act','hero','deliver') 0 'Known'
Check ($r.data.state.delivery_completed -and $r.data.state.money -eq 125 -and $r.data.state.inventory_ids.Count -eq 0) 'deliver completes and removes carried item'
$r=Run 'reopen-midway' @('show','hero') 0 'Known'
Check ($r.data.character_id -eq $identity -and $r.data.delivery_completed -and $r.data.money -eq 125 -and $r.data.shared_world_identity) 'fresh process restores identity/ownership/mid-quest state'
$revision=$r.data.revision;$hash=(Get-FileHash -LiteralPath $save -Algorithm SHA256).Hash
$r=Run 'duplicate-delivery' @('act','hero','deliver') 0 'Known'
Check (!$r.data.changed -and $r.data.state.money -eq 125 -and $r.data.state.revision -eq $revision -and (Get-FileHash -LiteralPath $save -Algorithm SHA256).Hash -eq $hash) 'duplicate completion no write/no reward'
$r=Run 'restart-completed' @('act','hero','accept-delivery') 0 'Known'
Check (!$r.data.changed -and $r.data.state.money -eq 125) 'completed task cannot restart for reward'
$r=Run 'accept-return' @('act','hero','accept-return') 0 'Known'
Check ($r.data.state.return_accepted -and !$r.data.state.return_completed) 'second task accepted after resume'
$r=Run 'return-item' @('act','hero','return-item') 0 'Known'
Check ($r.data.state.return_completed -and $r.data.state.money -eq 165 -and ($r.data.state.reward_task_uids -join ',') -eq '1,2' -and ($r.data.state.inventory_ids -join ',') -eq '100') 'second task reward and restored inventory'
$r=Run 'reopen-complete' @('show','hero') 0 'Known'
Check ($r.data.character_id -eq $identity -and $r.data.money -eq 165 -and $r.data.same_object_identity -and $r.data.shared_world_identity) 'complete state persists across exit/reopen'
$hash=(Get-FileHash -LiteralPath $save -Algorithm SHA256).Hash
$r=Run 'unsupported' @('act','hero','grant-xp') 3 'Unsupported'
Check ($r.data.money -eq 165 -and (Get-FileHash -LiteralPath $save -Algorithm SHA256).Hash -eq $hash) 'Unsupported preserves committed state'
$r=Run 'duplicate-slot' @('new','hero','Another recruit') 2 'InvalidInput'
Check ((Get-FileHash -LiteralPath $save -Algorithm SHA256).Hash -eq $hash) 'new character cannot overwrite occupied slot'
$r=Run 'path-slot' @('show','../hero') 2 'InvalidInput'
# Damage only this acceptance-created project save. Load must report corruption;
# recovery is a separate explicit command, not an implicit fallback.
$beforeCorruption=[IO.File]::ReadAllBytes($save)
[IO.File]::WriteAllBytes($save,[byte[]](123,34,98,97,100))
$r=Run 'corrupt-load' @('show','hero') 2 'InvalidInput'
Check ($r.reason -match 'SessionCorruptException') 'corrupt save is explicit'
$r=Run 'explicit-recover' @('recover','hero') 0 'Known'
Check ($r.data.character_id -eq $identity -and $r.data.money -eq 125 -and $r.data.return_accepted -and !$r.data.return_completed) 'explicit recovery uses validated previous state'
$r=Run 'resume-after-recover' @('act','hero','return-item') 0 'Known'
Check ($r.data.state.money -eq 165 -and ($r.data.state.reward_task_uids -join ',') -eq '1,2') 'recovered continuation still rewards once'
Push-Location -LiteralPath $validation
try{$r=Run 'foreign-cwd' @('show','hero') 0 'Known'}finally{Pop-Location}
Check ($r.data.money -eq 165 -and $r.data.character_id -eq $identity) 'save root belongs to executable rather than working directory'
$coreLog=Get-Content -LiteralPath (Join-Path $package 'core-tests.log') -Raw
if($coreLog -notmatch 'M2_CORE_ASSERTIONS=(\d+)'){throw 'Core assertion result missing'}
$coreCount=[int]$Matches[1]
$report=[ordered]@{schema='grim-dynasty-m2-acceptance-v1';status='Passed';completed_utc=[DateTime]::UtcNow.ToString('o');case_count=$script:cases.Count;cli_assertions=$script:checks;core_assertions=$coreCount;cases=$script:cases;
 scope='Fresh processes with own character/session/saves only; no original game/save access; unchanged M1 not rerun';platform=[Environment]::OSVersion.VersionString}
$report|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $package 'acceptance.json') -Encoding UTF8
Write-Output ("M2_ACCEPTANCE cases={0} cli_checks={1} core_checks={2} Passed" -f $script:cases.Count,$script:checks,$coreCount)
