param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$output=[IO.Path]::GetFullPath($OutputDirectory)
if (-not $output.StartsWith($workspace+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) { throw 'Project output required' }
if (Test-Path -LiteralPath $output) { throw 'Test output already exists' }
[IO.Directory]::CreateDirectory($output) | Out-Null
. (Join-Path $PSScriptRoot 'rea_sampler_failure.ps1')
$checks=[Collections.Generic.List[object]]::new()
$started=[DateTime]::UtcNow
$failure=$null
$secret='TEST_ONLY_DO_NOT_SERIALIZE_9af146'
function Check([bool]$Value,[string]$Name) {
    if (-not $Value) { throw ('Failed: '+$Name) }
    $checks.Add([ordered]@{name=$Name;passed=$true})
}
try {
    $native=[ComponentModel.Win32Exception]::new(87,$secret)
    $creation=[DateTime]::SpecifyKind([DateTime]::new(2026,10,20,6,0,0),[DateTimeKind]::Utc).AddTicks(1234567)
    $native.Data['ReaProbeProcessId']=[int]62001
    $native.Data['ReaProbeCreationUtc']=$creation
    $native.Data['ReaProbeInJob']=$true
    $native.Data['ReaProbeStep']='QueryFullProcessImageName'
    $native.Data['ReaProbeImageErrorCode']=[int]87
    $native.Data['ReaProbeExitAfterImageFailure']=$false
    $native.Data['ReaProbeFromQueryMembers']=$true
    $native.Data['ReaProbeMemberIndex']=[int]2
    $native.Data['ReaProbeMemberCount']=[int]4
    $native.Data['UnknownSecret']=$secret
    $wrapped=[Reflection.TargetInvocationException]::new($secret,$native)
    $record=New-ReaSamplerFailureRecord -Exception $wrapped -Phase 'after-counters-query' -RootProcessId 62001 -ControllerProcessId 62002
    $json=$record | ConvertTo-Json -Compress
    Check ($record.native_error_code -eq 87) 'PowerShell-style wrapper preserves native code'
    Check ($record.process_id -eq 62001) 'Failed PID is projected'
    Check ($record.creation_utc -ceq $creation.ToString('o')) 'Known UTC creation retained'
    Check ($record.creation_ticks -is [string] -and $record.creation_ticks -ceq $creation.Ticks.ToString()) '100 ns creation identity is lossless text'
    Check ($record.in_job -eq $true) 'Observed exact Job membership retained'
    Check ($record.probe_step -ceq 'QueryFullProcessImageName') 'API step retained'
    Check ($record.sampler_phase -ceq 'after-counters-query') 'Final query phase retained'
    Check ($record.image_query_error_code -eq 87) 'Image failure code retained'
    Check ($record.exit_confirmed_after_image_failure -eq $false) 'Unsignaled same-handle state retained'
    Check ($record.from_query_members -eq $true -and $record.member_index -eq 2 -and $record.member_count -eq 4) 'Membership enumeration position retained'
    Check (-not $json.Contains($secret)) 'Message and unknown Data never emitted'
    Check ($json.Length -lt 2000) 'Diagnostic remains a bounded small record'
    Check ($null -eq $record.exit_check_error_code) 'No invented wait error'
    $double=[ComponentModel.Win32Exception]::new(6,$secret)
    $double.Data['ReaProbeProcessId']=[int]62001
    $double.Data['ReaProbeStep']='WaitForSingleObject(after image failure)'
    $double.Data['ReaProbeImageErrorCode']=[int]5
    $double.Data['ReaProbeExitCheckErrorCode']=[int]6
    $d=New-ReaSamplerFailureRecord $double 'before-inventory-query' 62001 62002
    Check ($d.native_error_code -eq 6 -and $d.image_query_error_code -eq 5 -and $d.exit_check_error_code -eq 6) 'Image and secondary wait failure codes remain separate'
    Check ($null -eq $d.exit_confirmed_after_image_failure) 'Failed wait is unknown rather than exited'
    Check ($null -eq $d.creation_utc -and $null -eq $d.in_job) 'Missing observations stay unknown'
    $bad=[ComponentModel.Win32Exception]::new(4321,$secret)
    $bad.Data['ReaProbeProcessId']=$secret
    $bad.Data['ReaProbeCreationUtc']=$secret
    $bad.Data['ReaProbeInJob']=$secret
    $bad.Data['ReaProbeStep']=$secret
    $bad.Data['ReaProbeImageErrorCode']=$secret
    $bad.Data['ReaProbeFromQueryMembers']=$secret
    $bad.Data['ReaProbeMemberIndex']=$secret
    $badRecord=New-ReaSamplerFailureRecord $bad $secret 62001 62002
    Check ($badRecord.native_error_code -eq 4321) 'Unknown Win32 codes are preserved without success classification'
    Check ($badRecord.sampler_phase -ceq 'unknown' -and $badRecord.probe_step -ceq 'unknown') 'Unknown phase text is not logged'
    Check ($null -eq $badRecord.process_id -and $null -eq $badRecord.in_job -and $null -eq $badRecord.creation_utc) 'Wrong scalar types are not coerced'
    Check (-not (($badRecord | ConvertTo-Json -Compress).Contains($secret))) 'Known keys with secret values cannot leak strings'
    $other=New-ReaSamplerFailureRecord ([Exception]::new($secret)) 'startup' 62001 62002
    Check ($other.error_kind -eq 'Other' -and $null -eq $other.native_error_code -and $null -eq $other.process_id) 'Non-native failure does not acquire a fake identity'
    $native.Data['ReaProbeCreationUtc']=[DateTime]::SpecifyKind($creation,[DateTimeKind]::Unspecified)
    $unknownDate=New-ReaSamplerFailureRecord $native 'inventory-probe' 62001 62002
    Check ($null -eq $unknownDate.creation_utc -and $null -eq $unknownDate.creation_ticks) 'Unspecified date is not assumed UTC'
    $native.Data['ReaProbeCreationUtc']=$creation
    $samplePath=Join-Path $output 'owned-memory.ndjson'
    Write-ReaSamplerFailure -Exception $wrapped -Phase 'after-counters-query' -RootProcessId 62001 -ControllerProcessId 62002 -OutputFile $samplePath
    $saved=[IO.File]::ReadAllText($samplePath+'.failure.json')
    $savedRecord=$saved | ConvertFrom-Json
    Check ($savedRecord.process_id -eq 62001 -and $savedRecord.native_error_code -eq 87) 'Sidecar contains native identity and failure'
    Check (-not (Test-Path -LiteralPath $samplePath)) 'Failure is not appended as a valid memory sample'
    Check (-not $saved.Contains($secret)) 'Sidecar excludes secret test marker'
    $refused=$false
    try { Write-ReaSamplerFailure -Exception $native -Phase 'startup' -OutputFile $samplePath } catch { $refused=$true }
    Check $refused 'Existing failure sidecar cannot be overwritten'
    Check ([IO.File]::ReadAllText($samplePath+'.failure.json') -ceq $saved) 'First sidecar is preserved byte for byte'
    $originalError=[Console]::Error
    $brokenError=[IO.StringWriter]::new()
    $brokenError.Dispose()
    $brokenSamplePath=Join-Path $output 'broken-stderr.ndjson'
    try {
        [Console]::SetError($brokenError)
        Write-ReaSamplerFailure -Exception $native -Phase 'before-inventory-query' -RootProcessId 62001 -ControllerProcessId 62002 -OutputFile $brokenSamplePath
    } finally { [Console]::SetError($originalError) }
    Check (Test-Path -LiteralPath ($brokenSamplePath+'.failure.json')) 'Broken stderr does not prevent independent sidecar'
    $brokenSaved=[IO.File]::ReadAllText($brokenSamplePath+'.failure.json') | ConvertFrom-Json
    Check ($brokenSaved.native_error_code -eq 87 -and $brokenSaved.process_id -eq 62001) 'Sidecar preserves the first native failure when stderr is broken'
    $outside=Join-Path ([IO.Directory]::GetParent($workspace).FullName) 'never-created-diagnostic-test'
    $refused=$false
    try { Write-ReaSamplerFailure -Exception $native -Phase 'startup' -OutputFile $outside } catch { $refused=$true }
    Check $refused 'Outside-workspace sidecar rejected before writing'
} catch { $failure=$_.Exception.Message }
finally {
    $report=[ordered]@{
        status=$(if ($null -eq $failure) {'passed'} else {'failed'})
        started_utc=$started.ToString('o');finished_utc=[DateTime]::UtcNow.ToString('o')
        assertions=$checks.Count;checks=@($checks.ToArray());error=$failure
        scope='Inert exception projection, redaction and workspace-sidecar tests; no child processes, Rea, Java or original files.'
        source_sha256=@{
            projection=(Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'rea_sampler_failure.ps1') -Algorithm SHA256).Hash.ToLowerInvariant()
            test=(Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash.ToLowerInvariant()
        }
    }
    [IO.File]::WriteAllText((Join-Path $output 'result.json'),($report | ConvertTo-Json -Depth 8)+[Environment]::NewLine,[Text.UTF8Encoding]::new($false))
    Write-Output ('Sampler diagnostic projection: '+$report.status+', '+$checks.Count+' assertions')
}
if ($null -ne $failure) { throw $failure }
