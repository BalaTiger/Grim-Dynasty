$ErrorActionPreference='Stop'
$repo='C:\Users\zhuzi\Documents\Grim-Dynasty'
if((Get-Location).Path -ne $repo){throw 'Unexpected working directory'}
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if((Get-FileHash -LiteralPath $compiler -Algorithm SHA256).Hash.ToLowerInvariant() -ne '46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'){throw 'Compiler changed'}
$core=Join-Path $repo '.tooling\r1-move-contract-validation\GrimDynasty.Actions.dll'
$coreHash='3ea34074db9d5a67d7dffb702eb053b703dc3b754d0db442aeadd71247e15c58'
if((Get-FileHash -LiteralPath $core -Algorithm SHA256).Hash.ToLowerInvariant() -ne $coreHash){throw 'Validated core changed'}
$destination=Join-Path $repo '.tooling\r1-move-manager-validation'
if(Test-Path -LiteralPath $destination){throw 'Validation exists; preserve and review follow-up separately'}
$inputs=@('src/reverse_client_actions/MoveManagerContracts.cs','src/reverse_client_actions/MoveManagerContractsTests.cs')
$sources=@($inputs|ForEach-Object{Join-Path $repo ($_.Replace('/','\'))})
foreach($file in @($core)+$sources){if((Get-Item -LiteralPath $file).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Redirected input'}}
$before=@($inputs|ForEach-Object{[pscustomobject]@{path=$_;bytes=(Get-Item -LiteralPath $_).Length;sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()}})
New-Item -ItemType Directory -Path $destination | Out-Null
$utf8=New-Object Text.UTF8Encoding($false)
$report=[ordered]@{started_utc=[DateTime]::UtcNow.ToString('o');status='started';compiler=$compiler;inputs=$before;validated_core=$core;core_sha256=$coreHash;compile_extension_exit=$null;compile_tests_exit=$null;tests_exit=$null;old45_and_new35_suites_rerun=$false;original_files_read=$false;original_executed=$false;r1_gameplay_accepted=$false;failure=$null}
try{
 $runtimeCore=Join-Path $destination 'GrimDynasty.Actions.dll'
 Copy-Item -LiteralPath $core -Destination $runtimeCore
 if((Get-FileHash -LiteralPath $runtimeCore -Algorithm SHA256).Hash.ToLowerInvariant() -ne $coreHash){throw 'Runtime dependency copy mismatch'}
 $extension=Join-Path $destination 'GrimDynasty.MoveManager.dll'
 $testexe=Join-Path $destination 'MoveManagerContractsTests.exe'
 $output=@(& $compiler /nologo /warn:4 /warnaserror+ /optimize+ /platform:x64 /target:library "/reference:$core" "/out:$extension" $sources[0] 2>&1)
 $report.compile_extension_exit=$LASTEXITCODE
 [IO.File]::WriteAllText((Join-Path $destination 'compile-extension.log'),($output -join "`r`n"),$utf8)
 if($report.compile_extension_exit -ne 0){throw 'Extension compile failed; see retained log'}
 $output=@(& $compiler /nologo /warn:4 /warnaserror+ /optimize+ /platform:x64 /target:exe "/reference:$core" "/reference:$extension" "/out:$testexe" $sources[1] 2>&1)
 $report.compile_tests_exit=$LASTEXITCODE
 [IO.File]::WriteAllText((Join-Path $destination 'compile-tests.log'),($output -join "`r`n"),$utf8)
 if($report.compile_tests_exit -ne 0){throw 'Test compile failed; see retained log'}
 $output=@(& $testexe 2>&1);$report.tests_exit=$LASTEXITCODE
 [IO.File]::WriteAllText((Join-Path $destination 'tests.log'),($output -join "`r`n")+"`r`n",$utf8)
 if($report.tests_exit -ne 0){throw 'Isolated extension tests failed; see retained log'}
 foreach($f in $before){if((Get-FileHash -LiteralPath $f.path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $f.sha256){throw 'Source changed during build'}}
 if((Get-FileHash -LiteralPath $core -Algorithm SHA256).Hash.ToLowerInvariant() -ne $coreHash){throw 'Core changed during build'}
 $report.status='passed-isolated-move-manager-contracts'
 $report.artifacts=@($runtimeCore,$extension,$testexe|ForEach-Object{[pscustomobject]@{path=$_;sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()}})
 $output | ForEach-Object{Write-Output $_}
}catch{$report.status='failed';$report.failure=$_.Exception.Message;throw}
finally{$report.finished_utc=[DateTime]::UtcNow.ToString('o');[IO.File]::WriteAllText((Join-Path $destination 'result.json'),($report|ConvertTo-Json -Depth 8)+"`n",$utf8)}
