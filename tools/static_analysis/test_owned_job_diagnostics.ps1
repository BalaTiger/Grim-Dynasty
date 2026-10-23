param(
    [string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)

# This standalone fixture compiles only project-owned sources. It never starts
# Rea/Java, loads a target DLL, queries unrelated PIDs, or changes production files.
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$nativeSource = Join-Path $PSScriptRoot 'ReaOwnedJob.cs'
$fixtureSource = Join-Path $PSScriptRoot 'OwnedJobDiagnosticFixture.cs'
$nodeFixture = Join-Path $PSScriptRoot 'owned_job_fixture.mjs'
$node = 'D:\NodeJS\node.exe'
$compiler = Join-Path $env:SystemRoot 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$expectedNativeSha256 = 'f06ef3bb91be422afa3e1eef639243be2b711c4f701da462767649e34851b0c1'
$imageAnchor = 'try { executablePath = ReadExecutable(process); }'
$exitAnchor = 'try { exitAfterImageFailure = ReadExited(process); }'
$nativeNamespaceAnchor = 'namespace GrimDynasty.StaticAnalysis'
$fixtureNamespaceAnchor = 'namespace GrimDynasty.StaticAnalysis.DiagnosticFixtureTemplate'
$utf8 = [Text.UTF8Encoding]::new($false)
$runNonce = [Guid]::NewGuid().ToString('N')
if ([string]::IsNullOrEmpty($OutputDirectory)) {
    $OutputDirectory = Join-Path $workspace ('.tooling/owned-job-diagnostics-' + $runNonce)
}
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (-not $output.StartsWith($workspace + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Fixture output must be inside the project.'
}
if (Test-Path -LiteralPath $output) { throw 'Fixture output already exists; refusing overwrite.' }
[IO.Directory]::CreateDirectory($output) | Out-Null

$started = [DateTime]::UtcNow
$records = [Collections.Generic.List[object]]::new()
$artifacts = [Collections.Generic.List[object]]::new()
$handles = [Collections.Generic.List[object]]::new()
$jobs = [Collections.Generic.List[object]]::new()
$assertions = 0
$failure = $null
$failureStage = 'initialization'
$productionHash = $null
$fixtureHash = $null
$nodeFixtureHash = $null
$control = $null
$baseline = $null
$shortOutcomes = [ordered]@{ normal=0; exited=0; open_process_87=0 }
$stepWhitelist = @(
    'OpenProcess', 'GetProcessTimes', 'IsProcessInJob',
    'QueryFullProcessImageName', 'WaitForSingleObject(after image failure)',
    'WaitForSingleObject(final)'
)

function Assert-True([bool]$Value, [string]$Label) {
    if (-not $Value) { throw ('Assertion failed: ' + $Label) }
    $script:assertions++
    $records.Add([ordered]@{ check=$Label; passed=$true })
}
function Replace-Unique([string]$Text, [string]$Anchor, [string]$Replacement) {
    if ([regex]::Matches($Text, [regex]::Escape($Anchor)).Count -ne 1) {
        throw 'A fixed source transformation anchor is absent or ambiguous.'
    }
    return $Text.Replace($Anchor, $Replacement)
}
function Find-NativeError([Exception]$ErrorObject) {
    $current = $ErrorObject
    for ($depth = 0; $null -ne $current -and $depth -lt 16; $depth++) {
        if ($current -is [ComponentModel.Win32Exception]) { return $current }
        $current = $current.InnerException
    }
    return $null
}
function Write-FixedDiagnostic([string]$Label, [ComponentModel.Win32Exception]$NativeError) {
    # Deliberately exclude Message, StackTrace, arbitrary Data, environment and argv.
    $data = $NativeError.Data
    $row = [ordered]@{
        diagnostic=$Label
        exception_type=$NativeError.GetType().FullName
        native_error_code=$NativeError.NativeErrorCode
    }
    foreach ($key in @(
        'ReaProbeProcessId', 'ReaProbeInJob', 'ReaProbeStep',
        'ReaProbeImageErrorCode', 'ReaProbeExitAfterImageFailure',
        'ReaProbeExitCheckErrorCode', 'ReaProbeFromQueryMembers',
        'ReaProbeMemberIndex', 'ReaProbeMemberCount'
    )) {
        if ($data.Contains($key)) { $row[$key] = $data[$key] }
    }
    if ($data.Contains('ReaProbeCreationUtc') -and $data['ReaProbeCreationUtc'] -is [DateTime]) {
        $created = [DateTime]$data['ReaProbeCreationUtc']
        $row['ReaProbeCreationUtc'] = $created.ToString('o')
        # JSON numbers cannot represent every 100 ns tick when consumed by JS.
        $row['ReaProbeCreationTicks'] = $created.Ticks.ToString([Globalization.CultureInfo]::InvariantCulture)
    }
    $row['open_process_marker'] = $data.Contains('OpenProcess') -and $data['OpenProcess'] -eq $true
    $records.Add($row)
}
function Capture-NativeFailure([scriptblock]$Action, [string]$Label) {
    $caught = $null
    try { & $Action | Out-Null } catch { $caught = $_.Exception }
    Assert-True ($null -ne $caught) ($Label + ': fails closed')
    $native = Find-NativeError $caught
    Assert-True ($null -ne $native) ($Label + ': retains Win32 exception type')
    Write-FixedDiagnostic $Label $native
    return $native
}
function Assert-ImageDiagnostic(
    [ComponentModel.Win32Exception]$ErrorObject, $Process, [bool]$InJob,
    [int]$ImageCode, [bool]$WaitFailure, [bool]$FromQuery, [string]$Label
) {
    $data = $ErrorObject.Data
    $expectedStep = if ($WaitFailure) { 'WaitForSingleObject(after image failure)' } else { 'QueryFullProcessImageName' }
    $expectedCode = if ($WaitFailure) { 6 } else { $ImageCode }
    Assert-True ($ErrorObject.NativeErrorCode -eq $expectedCode) ($Label + ': original native code')
    Assert-True ($data.Contains('ReaProbeProcessId') -and $data['ReaProbeProcessId'] -is [int] -and
        $data['ReaProbeProcessId'] -eq $Process.Id) ($Label + ': exact PID')
    Assert-True ($data.Contains('ReaProbeCreationUtc') -and $data['ReaProbeCreationUtc'] -is [DateTime] -and
        $data['ReaProbeCreationUtc'].Kind -eq [DateTimeKind]::Utc -and
        $data['ReaProbeCreationUtc'].Ticks -eq $Process.StartTime.ToUniversalTime().Ticks) ($Label + ': exact UTC creation ticks')
    Assert-True ($data.Contains('ReaProbeInJob') -and $data['ReaProbeInJob'] -is [bool] -and
        $data['ReaProbeInJob'] -eq $InJob) ($Label + ': observed membership')
    Assert-True ($data.Contains('ReaProbeStep') -and $data['ReaProbeStep'] -ceq $expectedStep -and
        $stepWhitelist -ccontains $data['ReaProbeStep']) ($Label + ': exact API step')
    Assert-True ($data.Contains('ReaProbeImageErrorCode') -and $data['ReaProbeImageErrorCode'] -is [int] -and
        $data['ReaProbeImageErrorCode'] -eq $ImageCode) ($Label + ': image error retained')
    Assert-True (-not $data.Contains('OpenProcess')) ($Label + ': image error is not OpenProcess disappearance')
    if ($WaitFailure) {
        Assert-True (-not $data.Contains('ReaProbeExitAfterImageFailure')) ($Label + ': unknown exit state stays absent')
        Assert-True ($data.Contains('ReaProbeExitCheckErrorCode') -and $data['ReaProbeExitCheckErrorCode'] -is [int] -and
            $data['ReaProbeExitCheckErrorCode'] -eq 6) ($Label + ': separate wait error retained')
    } else {
        Assert-True ($data.Contains('ReaProbeExitAfterImageFailure') -and $data['ReaProbeExitAfterImageFailure'] -is [bool] -and
            $data['ReaProbeExitAfterImageFailure'] -eq $false) ($Label + ': actual live wait result')
        Assert-True (-not $data.Contains('ReaProbeExitCheckErrorCode')) ($Label + ': no invented wait error')
    }
    if ($FromQuery) {
        Assert-True ($data.Contains('ReaProbeFromQueryMembers') -and $data['ReaProbeFromQueryMembers'] -eq $true -and
            $data.Contains('ReaProbeMemberIndex') -and $data['ReaProbeMemberIndex'] -is [int] -and $data['ReaProbeMemberIndex'] -eq 0 -and
            $data.Contains('ReaProbeMemberCount') -and $data['ReaProbeMemberCount'] -is [int] -and $data['ReaProbeMemberCount'] -eq 1) ($Label + ': exact singleton enumeration context')
    } else {
        Assert-True (-not $data.Contains('ReaProbeFromQueryMembers') -and -not $data.Contains('ReaProbeMemberIndex') -and
            -not $data.Contains('ReaProbeMemberCount')) ($Label + ': direct probe has no invented enumeration context')
    }
}
function Read-Ready($Process, [string]$Mode, [string]$Nonce) {
    $read = $Process.StandardOutput.ReadLineAsync()
    if (-not $read.Wait(10000) -or $null -eq $read.Result) { throw 'Fixture ready timeout or closed output.' }
    $hello = $read.Result | ConvertFrom-Json
    Assert-True ($hello.event -ceq 'ready' -and $hello.pid -eq $Process.Id -and
        $hello.mode -ceq $Mode -and $hello.nonce -ceq $Nonce) ('Exact owned fixture greeting: ' + $Mode)
}
function Start-Fixture([string]$Mode) {
    if ($Mode -cne 'root' -and $Mode -cne 'control') { throw 'Only fixed fixture modes are allowed.' }
    $nonce = [Guid]::NewGuid().ToString('N')
    $info = [Diagnostics.ProcessStartInfo]::new()
    $info.FileName = $node
    $info.Arguments = '"' + $nodeFixture + '" ' + $Mode + ' ' + $nonce
    $info.WorkingDirectory = $workspace
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.RedirectStandardInput = $true
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    $info.EnvironmentVariables.Remove('NODE_OPTIONS')
    $info.EnvironmentVariables.Remove('NODE_PATH')
    $process = [Diagnostics.Process]::Start($info)
    # Retain the original handle until final cleanup; never reacquire by PID.
    $null = $process.Handle
    $handles.Add($process)
    Read-Ready $process $Mode $nonce
    return $process
}
function Compile-Variant([string]$Name, [string]$ImageMethod, [bool]$WaitFailure) {
    $namespace = 'GrimDynasty.StaticAnalysis.DiagnosticRun' + $runNonce + '.' + $Name
    $derived = Replace-Unique $nativeText $nativeNamespaceAnchor ('namespace ' + $namespace)
    if (-not [string]::IsNullOrEmpty($ImageMethod)) {
        $derived = Replace-Unique $derived $imageAnchor (
            'try { executablePath = OwnedJobDiagnosticFixture.' + $ImageMethod + '(); }')
    }
    if ($WaitFailure) {
        $derived = Replace-Unique $derived $exitAnchor (
            'try { exitAfterImageFailure = OwnedJobDiagnosticFixture.FailExitCheck6(); }')
    }
    $derivedFixture = Replace-Unique $fixtureText $fixtureNamespaceAnchor ('namespace ' + $namespace)
    $variantDirectory = Join-Path $output $Name
    [IO.Directory]::CreateDirectory($variantDirectory) | Out-Null
    $nativePath = Join-Path $variantDirectory 'ReaOwnedJob.derived.cs'
    $helperPath = Join-Path $variantDirectory 'OwnedJobDiagnosticFixture.derived.cs'
    # Distinct file/assembly identities prevent LoadFrom from reusing an earlier
    # variant in the same Windows PowerShell process.
    $assemblyPath = Join-Path $variantDirectory ('OwnedJobDiagnostics-' + $runNonce + '-' + $Name + '.dll')
    [IO.File]::WriteAllText($nativePath, $derived, $utf8)
    [IO.File]::WriteAllText($helperPath, $derivedFixture, $utf8)
    $compilerArguments = @('/nologo','/target:library','/platform:x64','/langversion:4','/checked+',
        ('/out:' + $assemblyPath), $nativePath, $helperPath)
    $compilerMessages = @(& $compiler @compilerArguments 2>&1)
    $compileExit = $LASTEXITCODE
    # Compiler diagnostics refer only to generated project-owned source.
    [IO.File]::WriteAllLines((Join-Path $variantDirectory 'compiler-output.txt'), [string[]]$compilerMessages, $utf8)
    Assert-True ($compileExit -eq 0 -and (Test-Path -LiteralPath $assemblyPath -PathType Leaf)) ($Name + ': C#4 x64 checked compilation')
    $assembly = [Reflection.Assembly]::LoadFrom($assemblyPath)
    $jobType = $assembly.GetType($namespace + '.ReaOwnedJob', $true)
    $helperType = $assembly.GetType($namespace + '.OwnedJobDiagnosticFixture', $true)
    $artifacts.Add([ordered]@{
        variant=$Name; namespace=$namespace; fixed_image_method=$ImageMethod; fixed_wait_failure=$WaitFailure
        production_sha256=$productionHash
        derived_native_sha256=(Get-FileHash -LiteralPath $nativePath -Algorithm SHA256).Hash.ToLowerInvariant()
        derived_helper_sha256=(Get-FileHash -LiteralPath $helperPath -Algorithm SHA256).Hash.ToLowerInvariant()
        assembly_sha256=(Get-FileHash -LiteralPath $assemblyPath -Algorithm SHA256).Hash.ToLowerInvariant()
        namespace_transform_count=1
        image_transform_count=$(if ([string]::IsNullOrEmpty($ImageMethod)) { 0 } else { 1 })
        exit_transform_count=$(if ($WaitFailure) { 1 } else { 0 })
    })
    return [pscustomobject]@{ Name=$Name; JobType=$jobType; HelperType=$helperType }
}
function New-FixtureJob($Variant, $Process) {
    $constructor = $Variant.JobType.GetConstructor([type[]]@([int],[DateTime],[string]))
    if ($null -eq $constructor) { throw 'The fixed owned Job constructor was not found.' }
    $job = $constructor.Invoke([object[]]@($Process.Id, $Process.StartTime.ToUniversalTime(), $node))
    $jobs.Add($job)
    Assert-True ($job.LimitFlags -eq 0x2000) ($Variant.Name + ': only kill-on-close flag')
    return $job
}
function Wait-Exited($Process, [string]$Label) {
    Assert-True ($Process.WaitForExit(5000)) $Label
}
function Observe-ShortProcess($Job, $Process, [string]$Label) {
    try {
        $probe = $Job.Probe($Process.Id)
    } catch {
        $native = Find-NativeError $_.Exception
        if ($null -eq $native) { throw }
        Write-FixedDiagnostic $Label $native
        if ($native.NativeErrorCode -ne 87 -or -not $native.Data.Contains('OpenProcess') -or
            $native.Data['OpenProcess'] -ne $true -or $native.Data['ReaProbeStep'] -cne 'OpenProcess') {
            throw
        }
        Assert-True ($native.Data['ReaProbeProcessId'] -eq $Process.Id -and
            -not $native.Data.Contains('ReaProbeCreationUtc') -and -not $native.Data.Contains('ReaProbeInJob')) ($Label + ': genuine OpenProcess-87 classification only')
        $shortOutcomes.open_process_87++
        return
    }
    Assert-True ($probe.ProcessId -eq $Process.Id -and
        $probe.CreationTicks -eq $Process.StartTime.ToUniversalTime().Ticks) ($Label + ': pinned identity')
    if ($probe.HasExited) {
        $shortOutcomes.exited++
        $missingImage = [string]::IsNullOrEmpty([string]$probe.ExecutablePath)
        $validImage = if ($missingImage) {
            $null -ne $probe.ExecutablePathErrorCode -and
                -not [string]::IsNullOrEmpty([string]$probe.ExecutablePathDiagnostic)
        } else {
            $probe.ExecutablePath -ieq $node -and $null -eq $probe.ExecutablePathErrorCode -and
                $null -eq $probe.ExecutablePathDiagnostic
        }
        Assert-True $validImage ($Label + ': exited image state explicit')
    } else {
        $shortOutcomes.normal++
        Assert-True ($probe.InJob -and $probe.ExecutablePath -ieq $node -and
            $null -eq $probe.ExecutablePathErrorCode -and $null -eq $probe.ExecutablePathDiagnostic) ($Label + ': live identity fully readable')
    }
}

try {
    $failureStage = 'source-and-host-guards'
    Assert-True ([IntPtr]::Size -eq 8 -and [Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT) 'Windows x64 fixture host'
    foreach ($required in @($nativeSource,$fixtureSource,$nodeFixture,$node,$compiler)) {
        Assert-True (Test-Path -LiteralPath $required -PathType Leaf) 'Required existing fixture input'
    }
    $productionHash = (Get-FileHash -LiteralPath $nativeSource -Algorithm SHA256).Hash.ToLowerInvariant()
    $fixtureHash = (Get-FileHash -LiteralPath $fixtureSource -Algorithm SHA256).Hash.ToLowerInvariant()
    $nodeFixtureHash = (Get-FileHash -LiteralPath $nodeFixture -Algorithm SHA256).Hash.ToLowerInvariant()
    Assert-True ($productionHash -ceq $expectedNativeSha256) 'Reviewed production source SHA'
    $nativeText = [IO.File]::ReadAllText($nativeSource)
    $fixtureText = [IO.File]::ReadAllText($fixtureSource)
    Assert-True ([regex]::Matches($nativeText, [regex]::Escape($imageAnchor)).Count -eq 1 -and
        [regex]::Matches($nativeText, [regex]::Escape($exitAnchor)).Count -eq 1) 'Both Probe-specific fault anchors are unique'
    Assert-True (-not $nativeText.Contains('OwnedJobDiagnosticFixture')) 'Production source contains no fixture hook'
    [IO.File]::WriteAllBytes((Join-Path $output 'ReaOwnedJob.original.cs'), [IO.File]::ReadAllBytes($nativeSource))

    $failureStage = 'compile-baseline'
    $baseline = Compile-Variant 'Baseline' '' $false
    $control = Start-Fixture 'control'
    $baselineRoot = Start-Fixture 'root'
    $baselineJob = New-FixtureJob $baseline $baselineRoot
    Assert-True (-not $baselineJob.Probe($control.Id).InJob) 'External same-executable control stays outside the Job'
    $failureStage = 'baseline-openprocess-error'
    $invalidError = Capture-NativeFailure { $baselineJob.Probe([int]::MaxValue) } 'Unmodified OpenProcess failure'
    Assert-True ($invalidError.NativeErrorCode -eq 87 -and $invalidError.Data.Contains('OpenProcess') -and
        $invalidError.Data['OpenProcess'] -eq $true -and $invalidError.Data['ReaProbeStep'] -ceq 'OpenProcess' -and
        $invalidError.Data['ReaProbeProcessId'] -eq [int]::MaxValue) 'OpenProcess keeps its distinct native marker and API step'
    Assert-True (-not $invalidError.Data.Contains('ReaProbeCreationUtc') -and -not $invalidError.Data.Contains('ReaProbeInJob') -and
        -not $invalidError.Data.Contains('ReaProbeImageErrorCode') -and -not $invalidError.Data.Contains('ReaProbeExitCheckErrorCode')) 'OpenProcess failure does not invent unread facts'
    $baselineJob.Dispose()
    Wait-Exited $baselineRoot 'Baseline Job close removes its owned root'

    $variants = @(
        [pscustomobject]@{Name='Image5'; Method='FailImage5'; Code=5; WaitFailure=$false},
        [pscustomobject]@{Name='Image87'; Method='FailImage87'; Code=87; WaitFailure=$false},
        [pscustomobject]@{Name='ImageUnknown'; Method='FailImageUnknown'; Code=123456; WaitFailure=$false},
        [pscustomobject]@{Name='Image5Wait6'; Method='FailImage5'; Code=5; WaitFailure=$true}
    )
    foreach ($spec in $variants) {
        $failureStage = 'compile-' + $spec.Name
        $variant = Compile-Variant $spec.Name $spec.Method $spec.WaitFailure
        $root = Start-Fixture 'root'
        $job = New-FixtureJob $variant $root
        $failureStage = 'live-probe-' + $spec.Name
        $errorObject = Capture-NativeFailure { $job.Probe($root.Id) } ($spec.Name + ' live Probe')
        Assert-ImageDiagnostic $errorObject $root $true $spec.Code $spec.WaitFailure $false ($spec.Name + ' live Probe')
        $failureStage = 'query-members-' + $spec.Name
        $errorObject = Capture-NativeFailure { $job.QueryMembers() } ($spec.Name + ' QueryMembers')
        Assert-ImageDiagnostic $errorObject $root $true $spec.Code $spec.WaitFailure $true ($spec.Name + ' QueryMembers')
        $failureStage = 'external-control-' + $spec.Name
        $errorObject = Capture-NativeFailure { $job.Probe($control.Id) } ($spec.Name + ' external control')
        Assert-ImageDiagnostic $errorObject $control $false $spec.Code $spec.WaitFailure $false ($spec.Name + ' external control')
        Assert-True (-not $root.HasExited -and -not $control.HasExited) ($spec.Name + ': failures do not silently kill or adopt processes')
        if (-not $spec.WaitFailure) {
            $failureStage = 'exited-probe-' + $spec.Name
            $root.StandardInput.WriteLine('exit')
            $root.StandardInput.Flush()
            Wait-Exited $root ($spec.Name + ': controlled root exited before image failure')
            $exited = $job.Probe($root.Id)
            Assert-True ($exited.ProcessId -eq $root.Id -and $exited.CreationTicks -eq $root.StartTime.ToUniversalTime().Ticks -and
                $exited.HasExited) ($spec.Name + ': same retained process identity proves exit')
            Assert-True ($null -eq $exited.ExecutablePath -and $exited.ExecutablePathErrorCode -eq $spec.Code -and
                -not [string]::IsNullOrEmpty([string]$exited.ExecutablePathDiagnostic)) ($spec.Name + ': exited process explicitly permits missing image')
        }
        $imageCalls = [int]$variant.HelperType.GetProperty('ImageFaultCount').GetValue($null, $null)
        $exitCalls = [int]$variant.HelperType.GetProperty('ExitFaultCount').GetValue($null, $null)
        Assert-True ($imageCalls -eq $(if ($spec.WaitFailure) {3} else {4}) -and
            $exitCalls -eq $(if ($spec.WaitFailure) {3} else {0})) ($spec.Name + ': exact fixed fault invocation counts')
        $job.Dispose()
        Wait-Exited $root ($spec.Name + ': Job release leaves no owned root')
        Assert-True (-not $control.HasExited) ($spec.Name + ': Job release preserves external control')
    }

    $failureStage = 'unmodified-short-lived-fixtures'
    for ($iteration = 0; $iteration -lt 8; $iteration++) {
        $shortRoot = Start-Fixture 'root'
        $shortJob = New-FixtureJob $baseline $shortRoot
        Observe-ShortProcess $shortJob $shortRoot ('Short fixture ' + $iteration + ' before exit')
        $shortRoot.StandardInput.WriteLine('exit')
        $shortRoot.StandardInput.Flush()
        Observe-ShortProcess $shortJob $shortRoot ('Short fixture ' + $iteration + ' exit race')
        Wait-Exited $shortRoot ('Short fixture ' + $iteration + ' exit confirmed')
        Observe-ShortProcess $shortJob $shortRoot ('Short fixture ' + $iteration + ' after exit')
        $shortJob.Dispose()
        Assert-True (-not $control.HasExited) ('Short fixture ' + $iteration + ' cleanup preserves control')
    }
    Assert-True ($shortOutcomes.normal -ge 8 -and $shortOutcomes.exited -ge 8) 'Unmodified short fixtures observe live and exited states'
    Assert-True ((Get-FileHash -LiteralPath $nativeSource -Algorithm SHA256).Hash.ToLowerInvariant() -ceq $productionHash -and
        (Get-FileHash -LiteralPath $fixtureSource -Algorithm SHA256).Hash.ToLowerInvariant() -ceq $fixtureHash -and
        (Get-FileHash -LiteralPath $nodeFixture -Algorithm SHA256).Hash.ToLowerInvariant() -ceq $nodeFixtureHash) 'Production and fixture input hashes remain unchanged'
    $failureStage = 'complete'
}
catch {
    $failure = [ordered]@{ stage=$failureStage; exception_type=$_.Exception.GetType().FullName }
    $native = Find-NativeError $_.Exception
    if ($null -ne $native) { Write-FixedDiagnostic 'unexpected-native-failure' $native }
}
finally {
    foreach ($job in $jobs) {
        try { $job.Dispose() } catch {
            $records.Add([ordered]@{ cleanup_failure='job-dispose'; exception_type=$_.Exception.GetType().FullName })
            if ($null -eq $failure) { $failure = [ordered]@{ stage='cleanup-job'; exception_type=$_.Exception.GetType().FullName } }
        }
    }
    foreach ($process in $handles) {
        try {
            $isControl = $null -ne $control -and [object]::ReferenceEquals($process, $control)
            if ($null -eq $failure) {
                if ($isControl) {
                    if ($process.HasExited) {
                        $records.Add([ordered]@{ cleanup_failure='external-control-exited-before-teardown' })
                        $failure = [ordered]@{ stage='cleanup-control'; exception_type='FixtureAssertionFailure' }
                    } else {
                        Assert-True $true 'External control survived every owned Job release'
                    }
                } elseif (-not $process.WaitForExit(5000)) {
                    # Record failure, but still reach exact-handle fallback cleanup.
                    $records.Add([ordered]@{ cleanup_failure='owned-root-survived-job-release' })
                    $failure = [ordered]@{ stage='cleanup-process'; exception_type='FixtureAssertionFailure' }
                } else {
                    Assert-True $true 'All owned fixture roots exited after Job release'
                }
            }
            if (-not $process.HasExited) {
                # This retained handle belongs to the exact fixture we created.
                # Control termination is test teardown, not owned Job cleanup.
                $process.Kill()
                if (-not $process.WaitForExit(5000)) { throw 'Retained fixture cleanup timed out.' }
            }
        } catch {
            $records.Add([ordered]@{ cleanup_failure='retained-fixture'; exception_type=$_.Exception.GetType().FullName })
            if ($null -eq $failure) { $failure = [ordered]@{ stage='cleanup-process'; exception_type=$_.Exception.GetType().FullName } }
        } finally { $process.Dispose() }
    }
    $report = [ordered]@{
        status=$(if ($null -eq $failure) {'passed'} else {'failed'})
        started_utc=$started.ToString('o'); finished_utc=[DateTime]::UtcNow.ToString('o')
        assertions=$assertions; last_stage=$failureStage; failure=$failure
        source_sha256=[ordered]@{
            reviewed_production=$expectedNativeSha256
            observed_production=$productionHash
            diagnostic_fixture=$fixtureHash
            node_fixture=$nodeFixtureHash
            test_script=(Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash.ToLowerInvariant()
        }
        derived_artifacts=@($artifacts.ToArray())
        short_lived_observations=$shortOutcomes
        records=@($records.ToArray())
        scope='Project-owned Node fixtures and namespace-renamed C#4 test copies only; no Rea, Java, game data, production configuration changes or unrelated PID queries.'
        limitations=@(
            'Synthetic Win32 failures exercise managed fail-closed diagnostics; they do not reproduce or identify the earlier real QFPI failure.',
            'Only the test copy has fixed fault calls. Production source is pinned, checked unchanged, and has no fault hook.',
            'Retained child handles prevent intentional PID reuse. An unobserved OpenProcess-87 race is not claimed as exercised by the short-lived loop.',
            'Baseline tests read real creation, membership and exit facts. Fixed wait failure deliberately has no proven exit result.',
            'The tests do not exercise arbitrary Windows hosts or every possible native API failure.'
        )
    }
    [IO.File]::WriteAllText((Join-Path $output 'result.json'), ($report | ConvertTo-Json -Depth 10) + [Environment]::NewLine, $utf8)
    Write-Output ('Owned Job diagnostic fixture: ' + $report.status + ', ' + $assertions + ' assertions; ' + (Join-Path $output 'result.json'))
}
if ($null -ne $failure) { throw 'Owned Job diagnostic fixture failed; see its fixed-field result.json.' }
