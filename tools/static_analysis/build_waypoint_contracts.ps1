$ErrorActionPreference='Stop'
$repo='C:\Users\zhuzi\Documents\Grim-Dynasty'
if((Get-Location).Path -ne $repo){throw 'Unexpected working directory'}
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if((Get-FileHash -LiteralPath $compiler -Algorithm SHA256).Hash.ToLowerInvariant() -ne '46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'){throw 'Compiler changed'}
$dependencies=@(
 @{file='.tooling\r1-move-contract-validation\GrimDynasty.Actions.dll';hash='3ea34074db9d5a67d7dffb702eb053b703dc3b754d0db442aeadd71247e15c58'},
 @{file='.tooling\r1-move-manager-validation\GrimDynasty.MoveManager.dll';hash='7563b6e32745e79ddc8308d2ea48ca7336050271d8dc8c9ce1e75346a52232da'}
)
$destination=Join-Path $repo '.tooling\r1-waypoint-contract-validation'
if(Test-Path -LiteralPath $destination){throw 'Validation already exists; preserve and review separately'}
$inputs=@('src/reverse_client_actions/PortalSelection.cs','src/reverse_client_actions/WaypointState.cs','src/reverse_client_actions/WaypointContracts.cs','src/reverse_client_actions/PortalSelectionTests.cs','src/reverse_client_actions/WaypointContractsTests.cs')
$sources=@($inputs|ForEach-Object{Join-Path $repo ($_.Replace('/','\'))})
$references=@()
foreach($dependency in $dependencies){
 $full=Join-Path $repo $dependency.file
 if((Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash.ToLowerInvariant() -ne $dependency.hash){throw 'Validated dependency changed'}
 $references+="/reference:$full"
 if((Get-Item -LiteralPath $full).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Redirected dependency'}
}
foreach($file in $sources){if((Get-Item -LiteralPath $file).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Redirected input'}}
$before=@($inputs|ForEach-Object{[pscustomobject]@{path=$_;bytes=(Get-Item -LiteralPath $_).Length;sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()}})
New-Item -ItemType Directory -Path $destination | Out-Null
$utf8=New-Object Text.UTF8Encoding($false)
$report=[ordered]@{started_utc=[DateTime]::UtcNow.ToString('o');status='started';compiler=$compiler;inputs=$before;dependencies=$dependencies;compile_library_exit=$null;tests=@();old_suites_rerun=$false;original_read=$false;original_executed=$false;r1_gameplay_accepted=$false;failure=$null}
try{
 foreach($dependency in $dependencies){$full=Join-Path $repo $dependency.file;$copy=Join-Path $destination ([IO.Path]::GetFileName($full));Copy-Item -LiteralPath $full -Destination $copy;if((Get-FileHash -LiteralPath $copy -Algorithm SHA256).Hash.ToLowerInvariant() -ne $dependency.hash){throw 'Runtime dependency copy mismatch'}}
 $library=Join-Path $destination 'GrimDynasty.Waypoints.dll'
 $output=@(& $compiler /nologo /warn:4 /warnaserror+ /optimize+ /platform:x64 /target:library @references "/out:$library" $sources[0] $sources[1] $sources[2] 2>&1)
 $report.compile_library_exit=$LASTEXITCODE
 [IO.File]::WriteAllText((Join-Path $destination 'compile-library.log'),($output -join "`r`n"),$utf8)
 if($report.compile_library_exit -ne 0){throw 'Library compile failed; see retained log'}
 for($index=3;$index -le 4;$index++){
  $name=[IO.Path]::GetFileNameWithoutExtension($sources[$index]);$exe=Join-Path $destination ($name+'.exe')
  $result=[ordered]@{name=$name;compile_exit=$null;run_exit=$null};$report.tests+=,$result
  $output=@(& $compiler /nologo /warn:4 /warnaserror+ /optimize+ /platform:x64 /target:exe @references "/reference:$library" "/out:$exe" $sources[$index] 2>&1)
  $result.compile_exit=$LASTEXITCODE
  [IO.File]::WriteAllText((Join-Path $destination ($name+'-compile.log')),($output -join "`r`n"),$utf8)
  if($result.compile_exit -ne 0){throw 'Test compile failed; see retained log'}
  $output=@(& $exe 2>&1);$result.run_exit=$LASTEXITCODE
  [IO.File]::WriteAllText((Join-Path $destination ($name+'.log')),($output -join "`r`n")+"`r`n",$utf8)
  if($result.run_exit -ne 0){throw 'Isolated tests failed; see retained log'}
  $output|ForEach-Object{Write-Output $_}
 }
 foreach($f in $before){if((Get-FileHash -LiteralPath $f.path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $f.sha256){throw 'Input changed during build'}}
 $report.status='passed-isolated-waypoint-contracts'
 $report.artifacts=@(Get-ChildItem -LiteralPath $destination -File|Where-Object{$_.Extension -in '.exe','.dll'}|ForEach-Object{[pscustomobject]@{path=$_.FullName;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}})
}catch{$report.status='failed';$report.failure=$_.Exception.Message;throw}
finally{$report.finished_utc=[DateTime]::UtcNow.ToString('o');[IO.File]::WriteAllText((Join-Path $destination 'result.json'),($report|ConvertTo-Json -Depth 8)+"`n",$utf8)}
