$ErrorActionPreference = 'Stop'
$repo = 'C:\Users\zhuzi\Documents\Grim-Dynasty'
if ((Get-Location).Path -ne $repo) { throw 'Unexpected working directory' }
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if ((Get-FileHash -LiteralPath $compiler -Algorithm SHA256).Hash.ToLowerInvariant() -ne '46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00') { throw 'Compiler changed' }
$destination = Join-Path $repo '.tooling\r1-move-contract-validation'
if (Test-Path -LiteralPath $destination) { throw 'Validation output exists; preserve it and review any follow-up separately' }
$inputs = @('src/reverse_client_actions/ActionDispatch.cs','src/reverse_client_actions/ActionExecutor.cs','src/reverse_client_actions/MoveActionState.cs','src/reverse_client_actions/MoveBoundaries.cs','src/reverse_client_actions/MoveContracts.cs','src/reverse_client_actions/MoveContractsTests.cs')
$sourcePaths = @($inputs | ForEach-Object { Join-Path $repo ($_.Replace('/','\')) })
$before = @($inputs | ForEach-Object { $f=Get-Item -LiteralPath $_; if($f.Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Redirected input'}; [pscustomobject]@{path=$_;bytes=$f.Length;sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()} })
New-Item -ItemType Directory -Path $destination | Out-Null
$report = [ordered]@{started_utc=[DateTime]::UtcNow.ToString('o');status='started';compiler=$compiler;inputs=$before;compile_library_exit=$null;compile_tests_exit=$null;tests_exit=$null;original_files_read=$false;original_executed=$false;old_tests_rerun=$false;r1_gameplay_accepted=$false;failure=$null}
$utf8 = New-Object Text.UTF8Encoding($false)
try {
 $library=Join-Path $destination 'GrimDynasty.Actions.dll'
 $testexe=Join-Path $destination 'MoveContractsTests.exe'
 $output=@(& $compiler /nologo /warn:4 /warnaserror+ /optimize+ /platform:x64 /target:library "/out:$library" $sourcePaths[0] $sourcePaths[1] $sourcePaths[2] $sourcePaths[3] $sourcePaths[4] 2>&1)
 $report.compile_library_exit=$LASTEXITCODE
 [IO.File]::WriteAllText((Join-Path $destination 'compile-library.log'),($output -join "`r`n"),$utf8)
 if($report.compile_library_exit -ne 0){throw 'Library compilation failed; see retained log'}
 $output=@(& $compiler /nologo /warn:4 /warnaserror+ /optimize+ /platform:x64 /target:exe "/reference:$library" "/out:$testexe" $sourcePaths[5] 2>&1)
 $report.compile_tests_exit=$LASTEXITCODE
 [IO.File]::WriteAllText((Join-Path $destination 'compile-tests.log'),($output -join "`r`n"),$utf8)
 if($report.compile_tests_exit -ne 0){throw 'Test compilation failed; see retained log'}
 $output=@(& $testexe 2>&1)
 $report.tests_exit=$LASTEXITCODE
 [IO.File]::WriteAllText((Join-Path $destination 'tests.log'),($output -join "`r`n")+"`r`n",$utf8)
 if($report.tests_exit -ne 0){throw 'Isolated tests failed; see retained log'}
 foreach($f in $before){if((Get-FileHash -LiteralPath $f.path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $f.sha256){throw 'Source changed during build'}}
 $report.status='passed-isolated-move-contracts'
 $report.artifacts=@($library,$testexe|ForEach-Object{[pscustomobject]@{path=$_;sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()}})
 $output | ForEach-Object { Write-Output $_ }
} catch { $report.status='failed';$report.failure=$_.Exception.Message;throw }
finally { $report.finished_utc=[DateTime]::UtcNow.ToString('o');[IO.File]::WriteAllText((Join-Path $destination 'result.json'),($report|ConvertTo-Json -Depth 8)+"`n",$utf8) }
