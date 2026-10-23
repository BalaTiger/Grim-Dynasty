param(
    [string]$NodePath = 'D:\NodeJS\node.exe',
    [string]$FixturePath = 'C:\Users\zhuzi\Documents\Grim-Dynasty\tools\static_analysis\owned_job_fixture.mjs',
    [Parameter(Mandatory=$true)][string]$Nonce
)

# Self-owned fixture protocol only. Do not import Rea or create children before release.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$nestedFixture = $null
$workerExitCode = 0

function Write-FixtureMessage {
    param([object]$Message)
    [Console]::Out.WriteLine(($Message | ConvertTo-Json -Compress -Depth 4))
    [Console]::Out.Flush()
}

try {
    $fixedScriptRoot = 'C:\Users\zhuzi\Documents\Grim-Dynasty\tools\static_analysis'
    $fixedNode = 'D:\NodeJS\node.exe'
    $fixedFixture = Join-Path $fixedScriptRoot 'owned_job_fixture.mjs'
    if ([IntPtr]::Size -ne 8 -or [Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
        throw 'This fixture requires Windows x64.'
    }
    if (-not [string]::Equals([IO.Path]::GetFullPath($PSScriptRoot), $fixedScriptRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'The worker must run from its fixed project directory.'
    }
    if ([string]::IsNullOrWhiteSpace($NodePath) -or -not [IO.Path]::IsPathRooted($NodePath) -or
        -not [string]::Equals([IO.Path]::GetFullPath($NodePath), $fixedNode, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Only the fixed D:\NodeJS\node.exe is allowed.'
    }
    if ([string]::IsNullOrWhiteSpace($FixturePath) -or -not [IO.Path]::IsPathRooted($FixturePath) -or
        -not [string]::Equals([IO.Path]::GetFullPath($FixturePath), $fixedFixture, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Only the fixed project fixture script is allowed.'
    }
    if ($Nonce -cnotmatch '\A[a-f0-9]{32}\z') {
        throw 'Nonce must contain exactly 32 lowercase hexadecimal characters.'
    }
    if (-not (Test-Path -LiteralPath $fixedNode -PathType Leaf) -or
        -not (Test-Path -LiteralPath $fixedFixture -PathType Leaf)) {
        throw 'The fixed Node executable and fixture must already exist.'
    }
    Write-FixtureMessage ([ordered]@{ event = 'ready'; pid = $PID })
    $command = [Console]::ReadLine()
    if ($null -ne $command -and $command -cne 'exit') {
        if ($command -cne 'release') { throw 'Expected release, exit, or EOF before starting the nested fixture.' }
        # Add-Type may invoke a compiler on Windows PowerShell. It belongs after
        # release, so such a child is covered by the worker's verified outer Job.
        Add-Type -Path (Join-Path $fixedScriptRoot 'OwnedJobNestedFixture.cs') | Out-Null
        $nestedFixture = [GrimDynasty.StaticAnalysis.OwnedJobNestedFixture]::new(
            $fixedNode, $fixedFixture, $Nonce)
        Write-FixtureMessage ([ordered]@{
            event = 'nested'
            pid = $PID
            childPid = $nestedFixture.ChildPid
            creationUtc = $nestedFixture.CreationUtc.ToString('o', [Globalization.CultureInfo]::InvariantCulture)
            creationFileTime = $nestedFixture.CreationFileTime
            innerMembership = $nestedFixture.InnerMembership
        })
        $command = [Console]::ReadLine()
        if ($null -ne $command -and $command -cne 'exit') {
            throw 'Expected exit or EOF after starting the nested fixture.'
        }
    }
}
catch {
    $workerExitCode = 1
    Write-FixtureMessage ([ordered]@{ event = 'error'; pid = $PID; message = $_.Exception.Message })
}
finally {
    if ($null -ne $nestedFixture) {
        try { $nestedFixture.Dispose() }
        catch {
            $workerExitCode = 1
            Write-FixtureMessage ([ordered]@{ event = 'error'; pid = $PID; message = $_.Exception.Message })
        }
    }
}
exit $workerExitCode
