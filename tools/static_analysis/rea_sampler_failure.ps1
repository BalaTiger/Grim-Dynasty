# Allowlisted projection: never serialize Message, StackTrace, InvocationInfo,
# arbitrary Exception.Data, argv, executable paths or environment values.
function New-ReaSamplerFailureRecord {
    param(
        [Parameter(Mandatory=$true)][Exception]$Exception,
        [Parameter(Mandatory=$true)][string]$Phase,
        [int]$RootProcessId,
        [int]$ControllerProcessId
    )
    $allowedPhases = @('startup','initial-monitor-proof','initial-root-proof',
        'before-inventory-query','process-inventory','inventory-probe',
        'process-counters','counter-exit-probe','system-memory',
        'after-counters-query','write-sample','control-check','deadline')
    $allowedSteps = @('OpenProcess','GetProcessTimes','IsProcessInJob',
        'QueryFullProcessImageName','WaitForSingleObject(after image failure)',
        'WaitForSingleObject(final)')
    $base = $Exception.GetBaseException()
    $probeError = $null
    $cursor = $Exception
    for ($depth=0; $null -ne $cursor -and $depth -lt 16; $depth++) {
        if ($cursor.Data.Contains('ReaProbeProcessId')) { $probeError=$cursor; break }
        $cursor=$cursor.InnerException
    }
    $record = [ordered]@{
        schema='rea-sampler-failure-v1'
        event='sampler-failure'
        utc=[DateTime]::UtcNow.ToString('o')
        sampler_phase=$(if ($allowedPhases -ccontains $Phase) {$Phase} else {'unknown'})
        root_pid=$RootProcessId
        controller_pid=$ControllerProcessId
        error_kind=$(if ($base -is [ComponentModel.Win32Exception]) {'Win32Exception'} elseif ($base -is [InvalidOperationException]) {'InvalidOperationException'} else {'Other'})
        native_error_code=$(if ($base -is [ComponentModel.Win32Exception]) {$base.NativeErrorCode} else {$null})
        process_id=$null
        creation_utc=$null
        creation_ticks=$null
        in_job=$null
        probe_step=$null
        image_query_error_code=$null
        exit_confirmed_after_image_failure=$null
        exit_check_error_code=$null
        from_query_members=$null
        member_index=$null
        member_count=$null
        terminal_wait_available_at_entry=$null
        terminal_wait_attempted=$null
        terminal_wait_consumer_pid=$null
        # Boolean exit result of terminal_wait_consumer_pid's timed wait; that
        # PID may differ from process_id. Not a raw WAIT DWORD. Null means no
        # completed timed-wait result, never an inferred timeout or exit.
        terminal_wait_exit_result=$null
        disposition='fail-closed; original exception rethrown; owned Job cleanup remains required'
    }
    if ($null -ne $probeError) {
        $data=$probeError.Data
        foreach ($pair in @(
            @('ReaProbeProcessId','process_id'),
            @('ReaProbeImageErrorCode','image_query_error_code'),
            @('ReaProbeExitCheckErrorCode','exit_check_error_code'),
            @('ReaProbeMemberIndex','member_index'),
            @('ReaProbeMemberCount','member_count'),
            @('ReaProbeTerminalWaitConsumerPid','terminal_wait_consumer_pid'))) {
            if ($data[$pair[0]] -is [int]) { $record[$pair[1]]=$data[$pair[0]] }
        }
        foreach ($pair in @(
            @('ReaProbeInJob','in_job'),
            @('ReaProbeExitAfterImageFailure','exit_confirmed_after_image_failure'),
            @('ReaProbeFromQueryMembers','from_query_members'),
            @('ReaProbeTerminalWaitAvailableAtEntry','terminal_wait_available_at_entry'),
            @('ReaProbeTerminalWaitAttempted','terminal_wait_attempted'),
            @('ReaProbeTerminalWaitExitResult','terminal_wait_exit_result'))) {
            if ($data[$pair[0]] -is [bool]) { $record[$pair[1]]=$data[$pair[0]] }
        }
        $creation=$data['ReaProbeCreationUtc']
        if ($creation -is [DateTime] -and $creation.Kind -eq [DateTimeKind]::Utc) {
            $record.creation_utc=$creation.ToString('o')
            # Keep 100 ns identity exact for JavaScript consumers.
            $record.creation_ticks=$creation.Ticks.ToString([Globalization.CultureInfo]::InvariantCulture)
        }
        if ($data['ReaProbeStep'] -is [string]) {
            $record.probe_step=$(if ($allowedSteps -ccontains $data['ReaProbeStep']) {$data['ReaProbeStep']} else {'unknown'})
        }
    }
    return [pscustomobject]$record
}

function Write-ReaSamplerFailure {
    param(
        [Parameter(Mandatory=$true)][Exception]$Exception,
        [Parameter(Mandatory=$true)][string]$Phase,
        [int]$RootProcessId,
        [int]$ControllerProcessId,
        [Parameter(Mandatory=$true)][string]$OutputFile
    )
    $workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
    $path=[IO.Path]::GetFullPath($OutputFile)+'.failure.json'
    if (-not $path.StartsWith($workspace+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) {
        throw 'Failure diagnostic output must remain inside the workspace.'
    }
    $record=New-ReaSamplerFailureRecord -Exception $Exception -Phase $Phase -RootProcessId $RootProcessId -ControllerProcessId $ControllerProcessId
    $json=$record | ConvertTo-Json -Depth 4 -Compress
    # Separate from stdout memory samples; the controller already archives stderr.
    try { [Console]::Error.WriteLine('REA_SAMPLER_FAILURE '+$json) }
    catch { } # A broken stderr must not prevent the independent sidecar attempt.
    $stream=$null
    $writer=$null
    try {
        $stream=[IO.File]::Open($path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)
        $writer=[IO.StreamWriter]::new($stream,[Text.UTF8Encoding]::new($false))
        $writer.WriteLine($json)
        $writer.Flush()
    } finally {
        if ($null -ne $writer) { $writer.Dispose() }
        elseif ($null -ne $stream) { $stream.Dispose() }
    }
}
