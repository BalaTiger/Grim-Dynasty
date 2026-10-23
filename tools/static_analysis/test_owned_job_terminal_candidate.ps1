# Prepared only. Root must review and approve this one standalone run.
# No production replacement, policy changes, cleanup deletion or automatic retry.
[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
if ($PSBoundParameters.Count -ne 0) { throw 'No arguments accepted.' }
if ($PSVersionTable.PSVersion.Major -ne 5 -or $PSVersionTable.PSVersion.Minor -ne 1 -or
    $PSVersionTable.PSEdition -cne 'Desktop' -or [IntPtr]::Size -ne 8) { throw 'Windows PowerShell 5.1 Desktop x64 required.' }
$workspace='C:\Users\zhuzi\Documents\Grim-Dynasty'
$expectedScript=Join-Path $workspace 'tools\static_analysis\test_owned_job_terminal_candidate.ps1'
if (![String]::Equals([IO.Path]::GetFullPath($PSCommandPath),$expectedScript,[StringComparison]::OrdinalIgnoreCase) -or
    ![String]::Equals([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..')),$workspace,[StringComparison]::OrdinalIgnoreCase)) { throw 'Fixed workspace and runner path required.' }
$candidate=Join-Path $workspace 'tools\static_analysis\ReaOwnedJobTerminalCandidate.cs'
$testSource=Join-Path $workspace 'tools\static_analysis\OwnedJobTerminalCandidateTests.cs'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$systemReference='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.dll'
$coreReference='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Core.dll'
$node='D:\NodeJS\node.exe'
$native=Join-Path $workspace 'tools\static_analysis\ReaOwnedJob.cs'
$sampler=Join-Path $workspace 'tools\static_analysis\measure_job_process_memory.ps1'
$pins=@(
    [pscustomobject]@{path=$candidate;sha256='6d07a2a1e29565930ebd091f070128aeb6a59feaafd8e8fcdce01bb7b4a743a9'},
    [pscustomobject]@{path=$testSource;sha256='c1d345ef478706df6419ffee795f4864a1cdb85075f8c131008ea69e6c7ad40f'},
    [pscustomobject]@{path=$compiler;sha256='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'},
    [pscustomobject]@{path=$systemReference;sha256='2b3c17c6208a0b4b6beb94e1a066f99ba06cdb2ea919479e99d47e8c6d96dc71'},
    [pscustomobject]@{path=$coreReference;sha256='8b898bed54ae8cc61f9c58cc3eb062724f8357d2bdda338b5d363654ecff333d'},
    [pscustomobject]@{path=$node;sha256='63c259c81e5d472b5f11c8d506070130cb04a1ecf84b80377a34ed6ec9048088'},
    [pscustomobject]@{path=$native;sha256='f06ef3bb91be422afa3e1eef639243be2b711c4f701da462767649e34851b0c1'},
    [pscustomobject]@{path=$sampler;sha256='d2f95d1675ae6a79f72aa6f7620c18ef212c9b809f315628ff9abd184edd3e0d'}
)
$parent=Join-Path $workspace '.tooling'
$leaf='r0-owned-job-terminal-candidate-20261022-attempt2'
$output=Join-Path $parent $leaf
$library=Join-Path $output 'ReaOwnedJobTerminalCandidate.dll'
$derivedSource=Join-Path $output 'ReaOwnedJobTerminalCandidate.Instrumented.cs'
$exe=Join-Path $output 'OwnedJobTerminalCandidateTests.exe'
$utf8=New-Object Text.UTF8Encoding($false)
function Assert-Normal([string]$LiteralPath,[bool]$Directory) {
    $cursor=[IO.Path]::GetFullPath($LiteralPath); $first=$true
    while ($true) {
        $item=Get-Item -LiteralPath $cursor -Force -ErrorAction Stop
        if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Redirected path refused.' }
        if ($first) { if ([bool]$item.PSIsContainer -ne $Directory) { throw 'Unexpected path type.' } }
        elseif (!$item.PSIsContainer) { throw 'Ancestor is not a directory.' }
        $next=[IO.Directory]::GetParent($cursor); if ($null -eq $next) { break }
        $cursor=$next.FullName; $first=$false
    }
}
function Check-Pins {
    foreach ($pin in $pins) {
        Assert-Normal $pin.path $false
        if ((Get-FileHash -LiteralPath $pin.path -Algorithm SHA256).Hash.ToLowerInvariant() -cne $pin.sha256) { throw 'Pinned input changed.' }
    }
}
function Write-NewText([string]$Path,[string]$Text) {
    Assert-Normal $output $true
    if (![String]::Equals([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($Path)),$output,[StringComparison]::OrdinalIgnoreCase)) { throw 'Fixed output directory required.' }
    $stream=[IO.File]::Open($Path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)
    try { $bytes=$utf8.GetBytes($Text); $stream.Write($bytes,0,$bytes.Length) } finally { $stream.Dispose() }
}
function Replace-Unique([string]$Text,[string]$Anchor,[string]$Replacement) {
    $first=$Text.IndexOf($Anchor,[StringComparison]::Ordinal)
    if ($first -lt 0 -or $Text.LastIndexOf($Anchor,[StringComparison]::Ordinal) -ne $first) { throw 'Candidate instrumentation anchor must occur exactly once.' }
    return $Text.Replace($Anchor,$Replacement)
}
Assert-Normal $expectedScript $false
Assert-Normal $parent $true
Check-Pins
$existing=@(Get-ChildItem -LiteralPath $parent -Filter $leaf -Force -ErrorAction Stop | Where-Object { $_.Name -ieq $leaf })
if ($existing.Count -ne 0) { throw 'Existing attempt preserved; no overwrite, cleanup or automatic retry.' }
$null=New-Item -ItemType Directory -Path $output -ErrorAction Stop
Assert-Normal $output $true
if (@(Get-ChildItem -LiteralPath $output -Force -ErrorAction Stop).Count -ne 0) { throw 'Fresh output must be empty.' }
$record=[ordered]@{schema='owned-job-terminal-candidate-runner-v1';status='started';started_utc=[DateTime]::UtcNow.ToString('o');inputs=$pins;
    source_scope='unmodified standalone candidate library, then test-only derived candidate and one test source; production monitor is not compiled or replaced';
    candidate_compile_exit=$null;tests_compile_exit=$null;fixture_exit=$null;final_inputs_preserved=$false;failure_type=$null}
$record['runner_sha256']=(Get-FileHash -LiteralPath $expectedScript -Algorithm SHA256).Hash.ToLowerInvariant()
$failed=$false
try {
    # Compile the original candidate first. Its DLL is not a reference of the test executable.
    $candidateOutput=@(& $compiler /noconfig /nologo /langversion:5 /platform:x64 /target:library /checked+ /optimize+ /debug- "/reference:$systemReference" "/reference:$coreReference" "/out:$library" $candidate 2>&1)
    $record.candidate_compile_exit=$LASTEXITCODE
    Write-NewText (Join-Path $output 'candidate-compiler-output.txt') (($candidateOutput | ForEach-Object { [string]$_ }) -join [Environment]::NewLine)
    if ($record.candidate_compile_exit -ne 0) { throw 'Original candidate compilation failed; no second compile or execution.' }
    Assert-Normal $library $false
    $record['candidate_library_sha256']=(Get-FileHash -LiteralPath $library -Algorithm SHA256).Hash.ToLowerInvariant()
    Check-Pins
    $derived=[IO.File]::ReadAllText($candidate,$utf8)
    $imageAnchor='try { executablePath = ReadExecutable(process); }'
    $imageReplacement='try { executablePath = TerminalFault.Image(process.DangerousGetHandle(), delegate { return ReadExecutable(process); }); }'
    $waitAnchor='uint result = Native.WaitForSingleObject(process, milliseconds);'
    $waitReplacement='uint result = TerminalFault.Wait(process.DangerousGetHandle(), milliseconds, delegate { return Native.WaitForSingleObject(process, milliseconds); });'
    $derived=Replace-Unique $derived $imageAnchor $imageReplacement
    $derived=Replace-Unique $derived $waitAnchor $waitReplacement
    Write-NewText $derivedSource $derived
    Assert-Normal $derivedSource $false
    $record['derived_source_sha256']=(Get-FileHash -LiteralPath $derivedSource -Algorithm SHA256).Hash.ToLowerInvariant()
    $record['instrumentation']=[ordered]@{image_anchor_count=1;wait_anchor_count=1;production_hook_added=$false;candidate_modified=$false}
    # Both definitions are compiled into one assembly; the baseline DLL is deliberately excluded.
    $testOutput=@(& $compiler /noconfig /nologo /langversion:5 /platform:x64 /target:exe /checked+ /optimize+ /debug- "/reference:$systemReference" "/reference:$coreReference" "/out:$exe" $derivedSource $testSource 2>&1)
    $record.tests_compile_exit=$LASTEXITCODE
    Write-NewText (Join-Path $output 'tests-compiler-output.txt') (($testOutput | ForEach-Object { [string]$_ }) -join [Environment]::NewLine)
    if ($record.tests_compile_exit -ne 0) { throw 'Instrumented candidate tests compilation failed; no execution or retry.' }
    Assert-Normal $exe $false
    $record['executable_sha256']=(Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant()
    $info=New-Object Diagnostics.ProcessStartInfo
    $info.FileName=$exe; $info.WorkingDirectory=$output
    $info.UseShellExecute=$false; $info.CreateNoWindow=$true
    $info.RedirectStandardOutput=$true; $info.RedirectStandardError=$true
    $fixture=$null; $stdout=$null; $stderr=$null; $terminationAttempted=$false
    try {
        $fixture=[Diagnostics.Process]::Start($info)
        $retainedFixtureHandle=$fixture.Handle
        if ($retainedFixtureHandle -eq [IntPtr]::Zero) { throw 'Created fixture handle unavailable.' }
        $stdout=$fixture.StandardOutput.ReadToEndAsync()
        $stderr=$fixture.StandardError.ReadToEndAsync()
        $record['fixture_pid']=$fixture.Id
        $record['fixture_creation_utc']=$fixture.StartTime.ToUniversalTime().ToString('o')
        if (-not $fixture.WaitForExit(30000)) {
            $record['fixture_timed_out']=$true; $terminationAttempted=$true
            $fixture.Kill()
            $record['forced_fixture_exit_confirmed']=$fixture.WaitForExit(2000)
            throw 'Owned test fixture reached its fixed 30 second deadline.'
        }
        $record.fixture_exit=$fixture.ExitCode
    } finally {
        try {
            if ($null -ne $fixture -and -not $fixture.HasExited -and -not $terminationAttempted) {
                $terminationAttempted=$true
                $fixture.Kill()
                $record['finally_fixture_exit_confirmed']=$fixture.WaitForExit(2000)
            }
            if ($null -ne $stdout -and $null -ne $stderr) {
                $captureComplete=[Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($stdout,$stderr),2000)
                $record['output_capture_complete']=$captureComplete
                if ($captureComplete) {
                    Write-NewText (Join-Path $output 'fixture-output.txt') $stdout.Result
                    Write-NewText (Join-Path $output 'fixture-stderr.txt') $stderr.Result
                } else {
                    Write-NewText (Join-Path $output 'fixture-output.txt') 'Output capture incomplete; no completed output is claimed.'
                    Write-NewText (Join-Path $output 'fixture-stderr.txt') 'Output capture incomplete; no completed output is claimed.'
                    throw 'Fixture output did not finish within its bounded capture wait.'
                }
            }
        } finally { if ($null -ne $fixture) { $fixture.Dispose() } }
    }
    if ($record.fixture_exit -ne 0) { throw 'Candidate tests failed; preserve results and do not retry.' }
    $record.status='completed-tests'
} catch {
    $failed=$true; $record.status='failed'; $record.failure_type=$_.Exception.GetType().FullName
} finally {
    try {
        Check-Pins
        if ((Get-FileHash -LiteralPath $expectedScript -Algorithm SHA256).Hash.ToLowerInvariant() -cne $record.runner_sha256) { throw 'Runner changed during the attempt.' }
        $record.final_inputs_preserved=$true
    } catch { $failed=$true; $record.status='failed'; $record['final_hash_failure_type']=$_.Exception.GetType().FullName }
    $record['finished_utc']=[DateTime]::UtcNow.ToString('o')
    $record['limitations']='Test-only fault injection is not an R0 failure reproduction or production monitor repair. A timeout does not confirm normal child cleanup. One separately approved run only.'
    Write-NewText (Join-Path $output 'runner-result.json') (($record | ConvertTo-Json -Depth 6) + [Environment]::NewLine)
}
if ($failed) { throw 'Owned Job terminal candidate tests failed; inspect preserved project output.' }
Write-Output ('Owned Job terminal candidate results saved: ' + $output)
