param()
$ErrorActionPreference = 'Stop'
$taskRoot = 'C:\Users\zhuzi\Documents\Grim-Dynasty'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$output = Join-Path $taskRoot '.tooling\r1-pathing-data-validation'
$sourcePins = @{
    'src\reverse_client_actions\NavigationPathingData.cs' = '9250226273e19f74c7ba117dff0f65f3effb015927f0f1c32cfb7a6c6e29a85a'
    'src\reverse_client_actions\NavigationPathingDataTests.cs' = '54ed901adaab09b6c0fc9c4da19b957f792c4429eb40b7759e05b061d7b4848f'
}
if ($args.Count -ne 0) { throw 'No arguments supported' }
if ((Get-Location).Path -ne $taskRoot) { throw 'Fixed workspace required' }
if (Test-Path -LiteralPath $output) { throw 'Existing output preserved; inspect before another run' }
foreach ($relative in $sourcePins.Keys) {
    $source = Join-Path $taskRoot $relative
    if ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant() -ne $sourcePins[$relative]) {
        throw "Source is not the reviewed frozen input: $relative"
    }
}
$report = [ordered]@{
    started_utc = [DateTime]::UtcNow.ToString('o')
    status = 'running'
    compiler = $compiler
    inputs = $sourcePins
    compile_library_exit = $null
    compile_tests_exit = $null
    tests_exit = $null
    original_read = $false
    original_executed = $false
    old_suites_rerun = $false
    gameplay_accepted = $false
    failure = $null
    artifacts = @()
}
New-Item -ItemType Directory -Path $output | Out-Null
try {
    $library = Join-Path $output 'GrimDynasty.PathingData.dll'
    $testExe = Join-Path $output 'NavigationPathingDataTests.exe'
    & $compiler /nologo /warn:4 /target:library "/out:$library" (Join-Path $taskRoot 'src\reverse_client_actions\NavigationPathingData.cs') *> (Join-Path $output 'compile-library.log')
    $report.compile_library_exit = $LASTEXITCODE
    if ($LASTEXITCODE -ne 0) { throw 'Library compile failed; inspect compile-library.log' }
    & $compiler /nologo /warn:4 /target:exe "/out:$testExe" "/reference:$library" (Join-Path $taskRoot 'src\reverse_client_actions\NavigationPathingDataTests.cs') *> (Join-Path $output 'compile-tests.log')
    $report.compile_tests_exit = $LASTEXITCODE
    if ($LASTEXITCODE -ne 0) { throw 'Test compile failed; inspect compile-tests.log' }
    & $testExe *> (Join-Path $output 'tests.log')
    $report.tests_exit = $LASTEXITCODE
    if ($LASTEXITCODE -ne 0) { throw 'Own data-format tests failed; inspect tests.log' }
    foreach ($artifact in @($library, $testExe)) {
        $report.artifacts += [ordered]@{ path=$artifact; sha256=(Get-FileHash -LiteralPath $artifact -Algorithm SHA256).Hash.ToLowerInvariant() }
    }
    $report.status = 'passed-isolated-pathing-data-format'
} catch {
    $report.status = 'failed'
    $report.failure = $_.Exception.Message
} finally {
    $report.finished_utc = [DateTime]::UtcNow.ToString('o')
    $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding UTF8
}
if ($report.status -ne 'passed-isolated-pathing-data-format') { throw $report.failure }
$report | ConvertTo-Json -Depth 6
