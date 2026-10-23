$ErrorActionPreference='Stop'
$repo='C:\Users\zhuzi\Documents\Grim-Dynasty'
if((Get-Location).Path -ne $repo){throw 'Unexpected working directory'}
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if((Get-FileHash -LiteralPath $compiler -Algorithm SHA256).Hash.ToLowerInvariant() -ne '46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'){throw 'Compiler changed'}
$dependency=Join-Path $repo '.tooling\r1-move-contract-validation\GrimDynasty.Actions.dll'
$dependencyHash='3ea34074db9d5a67d7dffb702eb053b703dc3b754d0db442aeadd71247e15c58'
if((Get-FileHash -LiteralPath $dependency -Algorithm SHA256).Hash.ToLowerInvariant() -ne $dependencyHash){throw 'Frozen dependency changed'}
$destination=Join-Path $repo '.tooling\r1-navigation-nearest-validation'
if(Test-Path -LiteralPath $destination){throw 'Validation exists; preserve and review separately'}
$librarySources=@('NavigationCoreContracts.cs','NavigationSpatialState.cs','NavigationEnumerationContracts.cs','NavigationNearestContracts.cs')|ForEach-Object{Join-Path $repo ('src\reverse_client_actions\'+$_)}
$testSource=Join-Path $repo 'src\reverse_client_actions\NavigationNearestTests.cs'
$sources=@($librarySources)+@($testSource)
foreach($file in @($sources)+@($dependency)){if((Get-Item -LiteralPath $file).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Redirected input'}}
if((Get-FileHash -LiteralPath $librarySources[0] -Algorithm SHA256).Hash.ToLowerInvariant() -ne '7f905b3b03cdc123c584c8224b0e780639b4c09e4e697d65b48083b434430002'){throw 'Prior core source changed; review required'}
$before=@($sources|ForEach-Object{[pscustomobject]@{path=$_;bytes=(Get-Item -LiteralPath $_).Length;sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()}})
New-Item -ItemType Directory -Path $destination|Out-Null
$utf8=New-Object Text.UTF8Encoding($false)
function InvokeCaptured([string]$file,[string[]]$arguments,[string]$label){
 $start=New-Object Diagnostics.ProcessStartInfo
 $start.FileName=$file
 $start.Arguments=(@($arguments|ForEach-Object{if($_.Contains('"')){throw 'Unexpected quote in fixed argument'};'"'+$_+'"'}) -join ' ')
 $start.WorkingDirectory=$repo;$start.UseShellExecute=$false;$start.CreateNoWindow=$true
 $start.RedirectStandardOutput=$true;$start.RedirectStandardError=$true
 $process=New-Object Diagnostics.Process;$process.StartInfo=$start
 try{
  if(!$process.Start()){throw 'Process did not start'}
  $stdoutTask=$process.StandardOutput.ReadToEndAsync();$stderrTask=$process.StandardError.ReadToEndAsync()
  $process.WaitForExit();$stdout=$stdoutTask.GetAwaiter().GetResult();$stderr=$stderrTask.GetAwaiter().GetResult();$code=$process.ExitCode
  [IO.File]::WriteAllText((Join-Path $destination ($label+'.stdout.log')),$stdout,$utf8)
  [IO.File]::WriteAllText((Join-Path $destination ($label+'.stderr.log')),$stderr,$utf8)
  [IO.File]::WriteAllText((Join-Path $destination ($label+'.log')),$stdout+$stderr,$utf8)
  return [pscustomobject]@{exit_code=$code;stdout=$stdout;stderr=$stderr}
 }finally{$process.Dispose()}
}
$report=[ordered]@{started_utc=[DateTime]::UtcNow.ToString('o');status='started';compiler=$compiler;inputs=$before;dependencies=@(@{file='.tooling/r1-move-contract-validation/GrimDynasty.Actions.dll';hash=$dependencyHash});compile_library_exit=$null;compile_test_exit=$null;test_run_exit=$null;old_suites_rerun=$false;original_read=$false;original_executed=$false;r1_gameplay_accepted=$false;failure=$null}
try{
 $copy=Join-Path $destination 'GrimDynasty.Actions.dll'
 Copy-Item -LiteralPath $dependency -Destination $copy
 if((Get-FileHash -LiteralPath $copy -Algorithm SHA256).Hash.ToLowerInvariant() -ne $dependencyHash){throw 'Runtime dependency copy mismatch'}
 $library=Join-Path $destination 'GrimDynasty.NavigationNearest.dll'
 $run=InvokeCaptured $compiler (@('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:library',"/reference:$dependency","/out:$library")+$librarySources) 'compile-library'
 $report.compile_library_exit=$run.exit_code
 if($report.compile_library_exit -ne 0){throw 'Library compile failed; see retained log'}
 $exe=Join-Path $destination 'NavigationNearestTests.exe'
 $run=InvokeCaptured $compiler @('/nologo','/warn:4','/warnaserror+','/optimize+','/platform:x64','/target:exe',"/reference:$dependency","/reference:$library","/out:$exe",'/main:NavigationNearestTestProgram',$testSource) 'compile-tests'
 $report.compile_test_exit=$run.exit_code
 if($report.compile_test_exit -ne 0){throw 'Tests compile failed; see retained log'}
 $run=InvokeCaptured $exe @() 'tests';$report.test_run_exit=$run.exit_code
 if($report.test_run_exit -ne 0){throw 'Isolated nearest tests failed; see retained log'}
 Write-Output $run.stdout
 foreach($f in $before){if((Get-FileHash -LiteralPath $f.path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $f.sha256){throw 'Input changed during build'}}
 $report.status='passed-isolated-navigation-nearest-contracts'
 $report.artifacts=@(Get-ChildItem -LiteralPath $destination -File|Where-Object{$_.Extension -in '.exe','.dll'}|ForEach-Object{[pscustomobject]@{path=$_.FullName;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}})
}catch{$report.status='failed';$report.failure=$_.Exception.Message;throw}
finally{$report.finished_utc=[DateTime]::UtcNow.ToString('o');[IO.File]::WriteAllText((Join-Path $destination 'result.json'),($report|ConvertTo-Json -Depth 8)+"`n",$utf8)}
