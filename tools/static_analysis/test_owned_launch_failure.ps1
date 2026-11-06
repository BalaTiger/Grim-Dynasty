# Prepared only. Inert exceptions and fake QueryMembers; no OS Job/process APIs.
param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$output=[IO.Path]::GetFullPath($OutputDirectory)
if(-not $output.StartsWith($workspace+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Project output required'}
$cursor=$output
while($cursor){if(Test-Path -LiteralPath $cursor){if((Get-Item -LiteralPath $cursor).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Reparse output rejected'}};$parent=[IO.Directory]::GetParent($cursor);if($null -eq $parent){break};$cursor=$parent.FullName}
if(Test-Path -LiteralPath $output){throw 'Existing test output preserved'}
$projection=Join-Path $PSScriptRoot 'rea_sampler_failure.ps1'
$patch=Join-Path $workspace '.tooling/r1-owned-launcher-failure-prepared/launcher-diagnostics.patch'
if((Get-FileHash $projection).Hash.ToLowerInvariant() -cne 'dc1da620b889cd727e9eb2fe3eaea733fa565f31cd0007a6270d7543317c89a2' -or (Get-FileHash $patch).Hash.ToLowerInvariant() -cne '28f9e2ccd823656ee2540ac4f537ac0c2d8c3eb8e16ad3d80f80ac5bebe9d6c0'){throw 'Frozen projection/patch changed'}
. $projection
# Load exactly the proposed replacement function, never the launcher or old suite.
$lines=@(Get-Content -LiteralPath $patch);$start=[Array]::IndexOf($lines,'+function Record-Members([string]$phase){');$end=$start
while($end -ge 0 -and $end -lt $lines.Count -and $lines[$end] -cne '+}'){$end++}
if($start -lt 0 -or $end -ge $lines.Count){throw 'Exact replacement function not found'}
$body=(@($lines[$start..$end]|ForEach-Object {$_.Substring(1)}) -join [Environment]::NewLine)
. ([scriptblock]::Create($body))
$job=[pscustomobject]@{}
$job|Add-Member -MemberType ScriptMethod -Name QueryMembers -Value {if($script:injectedException){throw $script:injectedException};return $script:fakeMembers}
$report=[ordered]@{worker_pid=62100;job_samples=@();probe_failures=@();probe_failure_capture_errors=@();status='failed';failure='original-primary-failure';cleanup_failure=$null}
$script:injectedException=$null;$script:fakeMembers=[pscustomobject]@{ActiveProcesses=0;NativeProcessIds=@();Members=@()}
$checks=New-Object 'Collections.Generic.List[object]';$cases=0;$failure=$null;$started=[DateTime]::UtcNow;$secret='TEST_ONLY_PRIVATE_DATA_6bc021'
function Check([bool]$value,[string]$name){if(!$value){throw ('Failed: '+$name)};$checks.Add([ordered]@{name=$name;passed=$true})}
function Observe-Failure([string]$phase){$caught=$null;try{$null=Record-Members $phase}catch{$caught=$_};if($null -eq $caught){throw 'Expected original QueryMembers failure'};return $caught}
[IO.Directory]::CreateDirectory($output)|Out-Null
try{
 $creation=[DateTime]::SpecifyKind([DateTime]::new(2026,11,3,4,19,36),[DateTimeKind]::Utc).AddTicks(4362869)
 $native=[ComponentModel.Win32Exception]::new(5,$secret)
 $native.Data['ReaProbeProcessId']=[int]62101;$native.Data['ReaProbeCreationUtc']=$creation;$native.Data['ReaProbeInJob']=$true
 $native.Data['ReaProbeStep']='QueryFullProcessImageName';$native.Data['ReaProbeImageErrorCode']=[int]5;$native.Data['ReaProbeExitAfterImageFailure']=$false
 $native.Data['ReaProbeFromQueryMembers']=$true;$native.Data['ReaProbeMemberIndex']=[int]1;$native.Data['ReaProbeMemberCount']=[int]3
 $native.Data['ReaProbeTerminalWaitAvailableAtEntry']=$true;$native.Data['ReaProbeTerminalWaitAttempted']=$true
 $native.Data['ReaProbeTerminalWaitConsumerPid']=[int]62102;$native.Data['ReaProbeTerminalWaitExitResult']=$false;$native.Data['UnlistedSecret']=$secret
 $script:injectedException=$native;$seen=Observe-Failure 'running';$envelope=$report.probe_failures[0];$d=$envelope.failure
 Check ($seen.Exception -is [Management.Automation.MethodInvocationException]) 'Actual PowerShell method-call wrapper used'
 Check ([object]::ReferenceEquals($seen.Exception.GetBaseException(),$native)) 'Original native exception identity rethrown'
 Check ($d.native_error_code -eq 5 -and $d.process_id -eq 62101 -and $d.in_job -eq $true) 'Nested error/PID/Job identity retained'
 Check ($d.creation_utc -ceq $creation.ToString('o') -and $d.creation_ticks -ceq $creation.Ticks.ToString([Globalization.CultureInfo]::InvariantCulture)) 'Exact UTC creation identity retained'
 Check ($d.terminal_wait_available_at_entry -eq $true -and $d.terminal_wait_attempted -eq $true -and $d.terminal_wait_consumer_pid -eq 62102 -and $d.terminal_wait_exit_result -eq $false) 'Shared wait consumer differs from failed PID; false is preserved'
 Check ($d.exit_confirmed_after_image_failure -eq $false -and $null -eq $d.exit_check_error_code -and $d.from_query_members -eq $true -and $d.member_index -eq 1 -and $d.member_count -eq 3) 'Observed same-handle state and member position retained'
 Check ($envelope.launcher_phase -ceq 'running' -and $d.sampler_phase -ceq 'unknown') 'Launcher phase is separate from sampler phase'
 $attempt=[DateTime]::Parse($envelope.sample_attempt_utc,[Globalization.CultureInfo]::InvariantCulture,[Globalization.DateTimeStyles]::RoundtripKind)
 $observed=[DateTime]::Parse($d.utc,[Globalization.CultureInfo]::InvariantCulture,[Globalization.DateTimeStyles]::RoundtripKind)
 Check ($attempt.Kind -eq [DateTimeKind]::Utc -and $observed.Kind -eq [DateTimeKind]::Utc -and $attempt -le $observed) 'Attempt UTC and later diagnostic UTC are distinct observations'
 Check ($report.job_samples.Count -eq 0 -and $report.status -ceq 'failed') 'Failure never becomes a valid sample or success'
 $firstJson=$envelope|ConvertTo-Json -Depth 6 -Compress;Check (!$firstJson.Contains($secret)) 'Messages and arbitrary Data are excluded';$cases++

 $cleanup=[ComponentModel.Win32Exception]::new(6,$secret);$cleanup.Data['ReaProbeProcessId']=[int]62103;$cleanup.Data['ReaProbeStep']='WaitForSingleObject(after image failure)'
 $cleanup.Data['ReaProbeImageErrorCode']=[int]5;$cleanup.Data['ReaProbeExitCheckErrorCode']=[int]6;$script:injectedException=$cleanup
 $null=Observe-Failure 'release';$d=$report.probe_failures[1].failure
 Check ($report.probe_failures.Count -eq 2 -and $report.probe_failures[1].launcher_phase -ceq 'release') 'Release failure appends a separate record'
 Check (($report.probe_failures[0]|ConvertTo-Json -Depth 6 -Compress) -ceq $firstJson -and $report.failure -ceq 'original-primary-failure' -and $null -eq $report.cleanup_failure) 'Logging cannot overwrite primary failure or cleanup outcome'
 Check ($d.native_error_code -eq 6 -and $d.image_query_error_code -eq 5 -and $d.exit_check_error_code -eq 6 -and $null -eq $d.exit_confirmed_after_image_failure) 'Image failure and unknown failed exit query stay separate';$cases++

 $script:injectedException=[Exception]::new($secret);$null=Observe-Failure 'running';$d=$report.probe_failures[2].failure
 Check ($null -eq $d.process_id -and $null -eq $d.in_job -and $null -eq $d.native_error_code -and $null -eq $d.terminal_wait_exit_result) 'Missing observations remain null'
 Check ($d.error_kind -ceq 'Other' -and !(($d|ConvertTo-Json -Compress).Contains($secret))) 'Other errors do not invent native identity or leak text';$cases++

 $native.Data['ReaProbeInJob']=$false;$native.Data['ReaProbeTerminalWaitAvailableAtEntry']=$false;$native.Data['ReaProbeTerminalWaitAttempted']=$false
 $native.Data.Remove('ReaProbeTerminalWaitConsumerPid');$native.Data.Remove('ReaProbeTerminalWaitExitResult');$script:injectedException=$native;$null=Observe-Failure 'running';$d=$report.probe_failures[3].failure
 Check ($d.in_job -eq $false -and $d.terminal_wait_available_at_entry -eq $false -and $d.terminal_wait_attempted -eq $false) 'False identity/wait fields are not dropped'
 Check ($null -eq $d.terminal_wait_consumer_pid -and $null -eq $d.terminal_wait_exit_result) 'No invented consumer or timed-wait outcome';$cases++

 $savedProjection=(Get-Item Function:New-ReaSamplerFailureRecord).ScriptBlock
 try{
  function New-ReaSamplerFailureRecord {throw 'Injected inert projection failure'}
  $seen=Observe-Failure 'running'
 }finally{Set-Item Function:New-ReaSamplerFailureRecord -Value $savedProjection}
 Check ([object]::ReferenceEquals($seen.Exception.GetBaseException(),$native)) 'Projection failure still rethrows original query exception'
 Check ($report.probe_failures.Count -eq 4 -and $report.probe_failure_capture_errors.Count -eq 1 -and $report.probe_failure_capture_errors[0].recording_failed -eq $true) 'Projection failure is a separate secondary marker'
 Check ($report.failure -ceq 'original-primary-failure' -and $report.status -ceq 'failed' -and $report.job_samples.Count -eq 0) 'Secondary failure does not erase or accept the original';$cases++

 $script:injectedException=$null;$members=Record-Members 'release'
 Check ([object]::ReferenceEquals($members,$script:fakeMembers) -and $report.job_samples.Count -eq 1) 'Successful query still returns the same sample object'
 Check ($report.probe_failures.Count -eq 4 -and $report.probe_failure_capture_errors.Count -eq 1 -and $report.status -ceq 'failed') 'Later empty sample does not turn prior failure into success';$cases++
}catch{$failure=$_.Exception.ToString()}
finally{
 $result=[ordered]@{status=$(if($null -eq $failure){'passed-owned-launch-failure-logging'}else{'failed-owned-launch-failure-logging'});started_utc=$started.ToString('o');finished_utc=[DateTime]::UtcNow.ToString('o');scenarios=$cases;assertions=$checks.Count;checks=@($checks.ToArray());failure=$failure;probe_failures=$report.probe_failures;capture_errors=$report.probe_failure_capture_errors;processes_started=0;os_jobs_created=0;original_read=$false;protection_changes=$false}
 [IO.File]::WriteAllText((Join-Path $output 'result.json'),($result|ConvertTo-Json -Depth 8)+[Environment]::NewLine,[Text.UTF8Encoding]::new($false))
 $result|Select-Object status,scenarios,assertions,failure|ConvertTo-Json
}
if($null -ne $failure){throw $failure}
