# Prepared only. Root must review and obtain execution approval for this one run.
# No policy changes, retries, cleanup deletion, or production monitor replacement.
[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
if ($PSBoundParameters.Count -ne 0) { throw 'No arguments accepted.' }
if ($PSVersionTable.PSVersion.Major -ne 5 -or $PSVersionTable.PSVersion.Minor -ne 1 -or
    $PSVersionTable.PSEdition -cne 'Desktop' -or [IntPtr]::Size -ne 8) { throw 'Windows PowerShell 5.1 Desktop x64 required.' }
$workspace='C:\Users\zhuzi\Documents\Grim-Dynasty'
$expectedScript=Join-Path $workspace 'tools\static_analysis\test_owned_qfpi_lifecycle.ps1'
if (![String]::Equals([IO.Path]::GetFullPath($PSCommandPath),$expectedScript,[StringComparison]::OrdinalIgnoreCase)) { throw 'Fixed runner path required.' }
$source=Join-Path $workspace 'tools\static_analysis\OwnedQfpiLifecycleDiagnostic.cs'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$node='D:\NodeJS\node.exe'
$native=Join-Path $workspace 'tools\static_analysis\ReaOwnedJob.cs'
$sampler=Join-Path $workspace 'tools\static_analysis\measure_job_process_memory.ps1'
$pins=@(
    [pscustomobject]@{path=$source;sha256='6b8712e9cdcab6f7685e4eb8cc0a9c05fa6f4f0ff0edc4b5a731ab1fce0ac200'},
    [pscustomobject]@{path=$compiler;sha256='46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'},
    [pscustomobject]@{path=$node;sha256='63c259c81e5d472b5f11c8d506070130cb04a1ecf84b80377a34ed6ec9048088'},
    [pscustomobject]@{path=$native;sha256='f06ef3bb91be422afa3e1eef639243be2b711c4f701da462767649e34851b0c1'},
    [pscustomobject]@{path=$sampler;sha256='d2f95d1675ae6a79f72aa6f7620c18ef212c9b809f315628ff9abd184edd3e0d'}
)
$parent=Join-Path $workspace '.tooling'
$leaf='r0-qfpi-lifecycle-diagnostic-20261022'
$output=Join-Path $parent $leaf
$exe=Join-Path $output 'OwnedQfpiLifecycleDiagnostic.exe'
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
    $stream=[IO.File]::Open($Path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)
    try { $bytes=$utf8.GetBytes($Text); $stream.Write($bytes,0,$bytes.Length) } finally { $stream.Dispose() }
}
Assert-Normal $expectedScript $false
Assert-Normal $parent $true
Check-Pins
$existing=@(Get-ChildItem -LiteralPath $parent -Filter $leaf -Force | Where-Object { $_.Name -ieq $leaf })
if ($existing.Count -ne 0) { throw 'Existing attempt preserved; no overwrite or automatic retry.' }
$null=New-Item -ItemType Directory -Path $output -ErrorAction Stop
Assert-Normal $output $true
$record=[ordered]@{schema='owned-qfpi-lifecycle-runner-v1';status='started';started_utc=[DateTime]::UtcNow.ToString('o');inputs=$pins;source_scope='one standalone diagnostic source; no production sources compiled';compiler_exit=$null;fixture_exit=$null;final_inputs_preserved=$false;failure_type=$null}
$failed=$false
try {
    # /noconfig removes implicit response-file inputs; this source uses mscorlib.
    $compileOutput=@(& $compiler /noconfig /nologo /langversion:5 /platform:x64 /target:exe /checked+ /optimize+ /debug- "/out:$exe" $source 2>&1)
    $record.compiler_exit=$LASTEXITCODE
    Write-NewText (Join-Path $output 'compiler-output.txt') (($compileOutput | ForEach-Object { [string]$_ }) -join [Environment]::NewLine)
    if ($record.compiler_exit -ne 0) { throw 'Diagnostic compilation failed; no execution or retry.' }
    Assert-Normal $exe $false
    $record['executable_sha256']=(Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant()
    # The new executable owns the unnamed Jobs. A diagnostic timeout terminates
    # only this exact created process via its retained handle; Job close owns
    # child cleanup. A timeout never claims normal cleanup or a valid result.
    $info=New-Object Diagnostics.ProcessStartInfo
    $info.FileName=$exe; $info.WorkingDirectory=$output
    $info.UseShellExecute=$false; $info.CreateNoWindow=$true
    $info.RedirectStandardOutput=$true; $info.RedirectStandardError=$true
    $fixture=$null
    try {
        $fixture=[Diagnostics.Process]::Start($info)
        $null=$fixture.Handle
        $stdout=$fixture.StandardOutput.ReadToEndAsync()
        $stderr=$fixture.StandardError.ReadToEndAsync()
        $record['diagnostic_pid']=$fixture.Id
        $record['diagnostic_creation_utc']=$fixture.StartTime.ToUniversalTime().ToString('o')
        if (-not $fixture.WaitForExit(30000)) {
            $record['diagnostic_timed_out']=$true
            $fixture.Kill()
            $record['forced_diagnostic_exit_confirmed']=$fixture.WaitForExit(2000)
            throw 'Owned diagnostic reached its fixed 30 second deadline.'
        }
        $record.fixture_exit=$fixture.ExitCode
        if (-not $stdout.Wait(2000) -or -not $stderr.Wait(2000)) { throw 'Diagnostic output did not finish.' }
        Write-NewText (Join-Path $output 'fixture-output.txt') $stdout.Result
        Write-NewText (Join-Path $output 'fixture-stderr.txt') $stderr.Result
    } finally {
        if ($null -ne $fixture) {
            if (-not $fixture.HasExited) {
                $fixture.Kill()
                $record['finally_diagnostic_exit_confirmed']=$fixture.WaitForExit(2000)
            }
            $fixture.Dispose()
        }
    }
    if ($record.fixture_exit -ne 0) { throw 'Diagnostic failed; preserve raw results and do not retry.' }
    $record.status='completed-observations'
} catch {
    $failed=$true; $record.status='failed'; $record.failure_type=$_.Exception.GetType().FullName
} finally {
    try { Check-Pins; $record.final_inputs_preserved=$true }
    catch { $failed=$true; $record.status='failed'; $record['final_hash_failure_type']=$_.Exception.GetType().FullName }
    $record['finished_utc']=[DateTime]::UtcNow.ToString('o')
    $record['limitations']='A completed diagnostic is not a reproduced R0 failure or repaired monitor. No result is accepted as a live process role or memory sample. One approved run only.'
    Write-NewText (Join-Path $output 'runner-result.json') (($record | ConvertTo-Json -Depth 6) + [Environment]::NewLine)
}
if ($failed) { throw 'Owned QFPI lifecycle diagnostic failed; inspect preserved project output.' }
Write-Output ('Owned QFPI lifecycle observations saved: ' + $output)
