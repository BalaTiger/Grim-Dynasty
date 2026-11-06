# Inert v2 launcher failure projection tests; no real Probe, Job or process.
param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'));$output=[IO.Path]::GetFullPath($OutputDirectory)
if(!$output.StartsWith($workspace+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Project output required'}
$cursor=$output
while($cursor){if(Test-Path -LiteralPath $cursor){if((Get-Item -LiteralPath $cursor).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Reparse output rejected'}};$parent=[IO.Directory]::GetParent($cursor);if($null -eq $parent){break};$cursor=$parent.FullName}
if(Test-Path -LiteralPath $output){throw 'Existing output preserved'}
$projection=Join-Path $PSScriptRoot 'rea_sampler_failure.ps1';$patch=Join-Path $workspace '.tooling/r1-owned-launcher-failure-prepared/launcher-diagnostics-v2.patch'
if((Get-FileHash $projection).Hash.ToLowerInvariant() -cne 'dc1da620b889cd727e9eb2fe3eaea733fa565f31cd0007a6270d7543317c89a2' -or (Get-FileHash $patch).Hash.ToLowerInvariant() -cne '02cfcc1afc9f821d0ccb7b4c024dfdbe9ef087624b71b9b72d866c4ca6d348f7'){throw 'Frozen projection/patch changed'}
. $projection
$lines=@(Get-Content $patch)
foreach($signature in @('+function Record-Members([string]$phase){',"+function Record-FailureDiagnostic([Exception]`$exception,[ValidateSet('primary','cleanup')][string]`$kind){")){
 $start=[Array]::IndexOf($lines,$signature);$end=$start;while($end -ge 0 -and $end -lt $lines.Count -and $lines[$end] -cne '+}'){$end++}
 if($start -lt 0 -or $end -ge $lines.Count){throw 'Exact proposed function missing'}
 . ([scriptblock]::Create((@($lines[$start..$end]|ForEach-Object {$_.Substring(1)}) -join [Environment]::NewLine)))
}
$job=[pscustomobject]@{};$job|Add-Member ScriptMethod Probe {throw $script:injectedException};$job|Add-Member ScriptMethod QueryMembers {throw $script:injectedException}
$script:memberFailureException=$null;$script:memberFailureAttemptUtc=$null;$script:memberFailurePhase=$null
$report=[ordered]@{worker_pid=62200;job_samples=@();probe_failures=@();probe_failure_capture_errors=@();status='failed';failure=$null;cleanup_failure=$null;primary_failure_diagnostic=$null;cleanup_failure_diagnostic=$null;primary_failure_diagnostic_capture_failed=$false;cleanup_failure_diagnostic_capture_failed=$false}
$checks=New-Object 'Collections.Generic.List[object]';$cases=0;$failure=$null;$started=[DateTime]::UtcNow
function Check([bool]$value,[string]$name){if(!$value){throw ('Failed: '+$name)};$checks.Add([ordered]@{name=$name;passed=$true})}
[IO.Directory]::CreateDirectory($output)|Out-Null
try{
 $native=[ComponentModel.Win32Exception]::new(5,'inert direct Probe failure');$native.Data['ReaProbeProcessId']=[int]62201;$native.Data['ReaProbeInJob']=$true;$native.Data['ReaProbeStep']='QueryFullProcessImageName';$native.Data['ReaProbeImageErrorCode']=[int]5;$native.Data['ReaProbeExitAfterImageFailure']=$false
 $script:injectedException=$native;$direct=$null
 try{$null=$job.Probe()}catch{$direct=$_;$report.status='failed';$report.failure=$_.Exception.ToString();Record-FailureDiagnostic $_.Exception 'primary'}
 $d=$report.primary_failure_diagnostic
 Check ($null -ne $direct -and $direct.Exception -is [Management.Automation.MethodInvocationException]) 'Direct Probe-style method failure captured'
 Check ($d.failure.native_error_code -eq 5 -and $d.failure.process_id -eq 62201 -and $d.failure.in_job -eq $true -and $d.failure.exit_confirmed_after_image_failure -eq $false) 'Outer capture projects nested native facts'
 Check ($null -eq $d.member_sample_attempt_utc -and $null -eq $d.member_sample_phase) 'Direct Probe has no fabricated member-sample start'
 Check ($d.failure_observed_utc -match 'Z$' -and $report.failure -ceq $direct.Exception.ToString() -and $report.status -ceq 'failed') 'Observed failure UTC does not replace original failure/status';$cases++

 $primary=$null
 try{$null=Record-Members 'running'}catch{$primary=$_;$report.status='failed';$report.failure=$_.Exception.ToString();Record-FailureDiagnostic $_.Exception 'primary'}
 $d=$report.primary_failure_diagnostic
 Check ([object]::ReferenceEquals($primary.Exception.GetBaseException(),$native)) 'Member-query rethrow preserves original native object'
 Check ($d.member_sample_attempt_utc -ceq $report.probe_failures[0].sample_attempt_utc -and $d.member_sample_phase -ceq 'running') 'Only exact exception identity links attempted member sample'
 Check ($report.job_samples.Count -eq 0 -and $report.probe_failures.Count -eq 1) 'Failed member query remains separate from valid samples'
 $primaryJson=$d|ConvertTo-Json -Depth 6 -Compress;$primaryText=$report.failure;$cases++

 $cleanup=[ComponentModel.Win32Exception]::new(6,'inert cleanup failure');$cleanup.Data['ReaProbeProcessId']=[int]62202;$cleanup.Data['ReaProbeStep']='WaitForSingleObject(final)';$script:injectedException=$cleanup;$cleanupRecord=$null
 try{$null=$job.Probe()}catch{$cleanupRecord=$_;$report.cleanup_failure=$_.Exception.ToString();$report.status='failed';Record-FailureDiagnostic $_.Exception 'cleanup'}
 Check ($report.cleanup_failure_diagnostic.failure.native_error_code -eq 6 -and $report.cleanup_failure_diagnostic.failure.process_id -eq 62202) 'Cleanup error has separate native diagnostic'
 Check ($null -eq $report.cleanup_failure_diagnostic.member_sample_attempt_utc -and $null -eq $report.cleanup_failure_diagnostic.member_sample_phase) 'Different exception cannot borrow prior sample UTC'
 Check (($report.primary_failure_diagnostic|ConvertTo-Json -Depth 6 -Compress) -ceq $primaryJson -and $report.failure -ceq $primaryText) 'Cleanup logging preserves original primary evidence'
 Check ($report.cleanup_failure -ceq $cleanupRecord.Exception.ToString()) 'Original cleanup text retained independently';$cases++

 $cleanupJson=$report.cleanup_failure_diagnostic|ConvertTo-Json -Depth 6 -Compress;$cleanupText=$report.cleanup_failure;$saved=(Get-Item Function:New-ReaSamplerFailureRecord).ScriptBlock
 try{
  function New-ReaSamplerFailureRecord {throw 'inert serialization failure'}
  Record-FailureDiagnostic $primary.Exception 'primary';Record-FailureDiagnostic $cleanupRecord.Exception 'cleanup'
 }finally{Set-Item Function:New-ReaSamplerFailureRecord -Value $saved}
 Check ($report.primary_failure_diagnostic_capture_failed -eq $true -and $report.cleanup_failure_diagnostic_capture_failed -eq $true) 'Secondary capture failures recorded per channel'
 Check (($report.primary_failure_diagnostic|ConvertTo-Json -Depth 6 -Compress) -ceq $primaryJson -and ($report.cleanup_failure_diagnostic|ConvertTo-Json -Depth 6 -Compress) -ceq $cleanupJson) 'Capture failure does not overwrite existing diagnostics'
 Check ($report.failure -ceq $primaryText -and $report.cleanup_failure -ceq $cleanupText -and $report.status -ceq 'failed') 'Capture errors never replace original failure or create success';$cases++
}catch{$failure=$_.Exception.ToString()}
finally{
 $result=[ordered]@{status=$(if($null -eq $failure){'passed-owned-launch-outer-failure-logging'}else{'failed-owned-launch-outer-failure-logging'});started_utc=$started.ToString('o');finished_utc=[DateTime]::UtcNow.ToString('o');scenarios=$cases;assertions=$checks.Count;checks=@($checks.ToArray());failure=$failure;primary_diagnostic=$report.primary_failure_diagnostic;cleanup_diagnostic=$report.cleanup_failure_diagnostic;processes_started=0;os_jobs_created=0;original_read=$false;protection_changes=$false}
 [IO.File]::WriteAllText((Join-Path $output 'result.json'),($result|ConvertTo-Json -Depth 8)+[Environment]::NewLine,[Text.UTF8Encoding]::new($false));$result|Select-Object status,scenarios,assertions,failure|ConvertTo-Json
}
if($null -ne $failure){throw $failure}
