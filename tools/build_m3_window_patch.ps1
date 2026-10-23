param([string]$PackageDirectory, [switch]$ReuseCompiledWindow)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# This script compiles one own window executable and runs one bounded smoke.
# The 22 previously accepted model checks are reused, not executed again.
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$rootPrefix = $root.TrimEnd('\') + '\'
if (!$PackageDirectory) { $PackageDirectory = 'artifacts\m3-scene-20261022-02' }
if (![IO.Path]::IsPathRooted($PackageDirectory)) { $PackageDirectory = Join-Path $root $PackageDirectory }
$target = [IO.Path]::GetFullPath($PackageDirectory)
if (!$target.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Project output required' }
if (Test-Path -LiteralPath $target) { throw 'Output already exists; preserve it' }

function Assert-NoReparse([string]$Path) {
    for ($cursor = $Path; $cursor; $cursor = Split-Path -Parent $cursor) {
        if (Test-Path -LiteralPath $cursor) {
            if ((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw ('Reparse path rejected: ' + $cursor)
            }
        }
    }
}
function Hash([string]$Path) { (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant() }
function Write-Json([string]$Path, $Value) {
    $json = ConvertTo-Json -InputObject $Value -Depth 12
    [IO.File]::WriteAllText($Path, $json + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
}
Assert-NoReparse $target

$priorDirectory = Join-Path $root 'artifacts\m3-scene-20261022-01'
$priorManifestPath = Join-Path $priorDirectory 'build-manifest.json'
$priorManifestHash = '564a5fd9bfc7902d07d38242c91810fc60368fb5f1ab03df4e0fc278876cbdd7'
$priorAcceptancePath = Join-Path $priorDirectory 'acceptance.json'
$priorAcceptanceHash = '4d92a344eccb65152ae52f6d311a8cfb48671ca86f0ef6596c85886f8858f426'
$priorModelLogPath = Join-Path $priorDirectory 'scene-tests.log'
$priorModelLogHash = 'ef5a275226206b179420e4e568c632be3365b1aab466fd4b16cb3e95ae7e5a5d'
foreach ($path in @($priorManifestPath, $priorAcceptancePath, $priorModelLogPath)) { Assert-NoReparse $path }
if ((Hash $priorManifestPath) -ne $priorManifestHash) { throw 'Accepted M3 manifest changed' }
if ((Hash $priorAcceptancePath) -ne $priorAcceptanceHash) { throw 'Accepted M3 acceptance changed' }
if ((Hash $priorModelLogPath) -ne $priorModelLogHash) { throw 'Accepted M3 model log changed' }
$prior = Get-Content -LiteralPath $priorManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$priorAcceptance = Get-Content -LiteralPath $priorAcceptancePath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($priorAcceptance.status -ne 'Passed' -or $priorAcceptance.scene_checks -ne 22) { throw 'Expected 22 accepted model checks' }

$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$compilerHash = '46809206887326d2d24db1eff1f3064de972c3451abe766b49111450a5e08e00'
if ((Hash $compiler) -ne $compilerHash -or $prior.compiler_sha256 -ne $compilerHash) { throw 'Compiler fingerprint changed' }
$allowedChanges = @('src/scene_runtime/SceneWindow.cs', 'src/scene_runtime/SceneProgram.cs')
$sources = [Collections.Generic.List[string]]::new()
$sourceEquivalence = [Collections.Generic.List[object]]::new()
$seen = @{}
foreach ($source in $prior.sources) {
    $relative = [string]$source.path
    if ([IO.Path]::IsPathRooted($relative) -or !$relative.EndsWith('.cs', [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid accepted source path' }
    $path = [IO.Path]::GetFullPath((Join-Path $root $relative))
    if (!$path.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase) -or $seen.ContainsKey($path)) { throw 'Outside-project or duplicate source' }
    Assert-NoReparse $path
    $seen.Add($path, $true)
    $currentHash = Hash $path
    $allowed = $allowedChanges -contains $relative
    $unchanged = $currentHash -eq $source.sha256
    if (!$unchanged -and !$allowed) { throw ('Accepted model source changed: ' + $relative) }
    $sources.Add($path)
    $sourceEquivalence.Add([ordered]@{path=$relative; prior_sha256=$source.sha256; sha256=$currentHash; unchanged=$unchanged; allowed_window_patch=$allowed})
}
foreach ($relative in $allowedChanges) {
    if (!($sourceEquivalence | Where-Object { $_.path -eq $relative })) { throw ('Expected window source missing: ' + $relative) }
}

New-Item -ItemType Directory -Path $target | Out-Null
$exe = Join-Path $target 'gd-scene.exe'
$common = @('/nologo', '/noconfig', '/main:GrimDynasty.Scene.SceneProgram', '/checked+', '/optimize+', '/debug-', '/codepage:65001',
    '/reference:System.dll', '/reference:System.Core.dll', '/reference:System.Web.Extensions.dll', '/reference:System.Drawing.dll', '/reference:System.Windows.Forms.dll')
$sourceArguments = $sources.ToArray()
$compileReused = $false
if ($ReuseCompiledWindow) {
    # The first window run reached its closing save/reload, but Start-Process
    # yielded no exit code in this host. Reuse that successful compilation only.
    $priorExe = Join-Path $root 'artifacts\m3-scene-20261022-02\gd-scene.exe'
    Assert-NoReparse $priorExe
    if ((Hash $priorExe) -ne '6ce0ca305b2dabae821afa5ee5ff962c86cb406c401d183159a8002830584cc3') { throw 'Reuse executable changed' }
    if ((Hash (Join-Path $root $allowedChanges[0])) -ne '24b38026a7dc255b0392eb34cdec4b8dd698a7c24f612e7a7dcd568bd338c09d') { throw 'Reuse Window source changed' }
    if ((Hash (Join-Path $root $allowedChanges[1])) -ne 'b52e9b5d5e97c7bee58ff06cd9089be70c60c1798b201acc0df51b22e1f50cee') { throw 'Reuse Program source changed' }
    Copy-Item -LiteralPath $priorExe -Destination $exe
    $log = @('No compilation run: reused pinned executable from m3-scene-20261022-02.')
    $compileExit = 0
    $compileReused = $true
} else {
    $log = @(& $compiler @common /target:winexe "/out:$exe" @sourceArguments 2>&1)
    $compileExit = $LASTEXITCODE
}
[IO.File]::WriteAllLines((Join-Path $target 'compile.log'), [string[]]$log, [Text.UTF8Encoding]::new($false))
if ($compileExit -ne 0) { throw 'Scene patch compilation failed; preserve compile.log' }
foreach ($source in $sourceEquivalence) {
    if ((Hash (Join-Path $root $source.path)) -ne $source.sha256) { throw ('Source changed during compilation: ' + $source.path) }
}

$windowRoot = Join-Path $target 'validation\window'
New-Item -ItemType Directory -Path $windowRoot | Out-Null
# Visible GUI is intentional: this is the requested own controllable scene.
# No original game, old test executable, global keyboard input or other process runs.
$startInfo = [Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = $exe
$startInfo.Arguments = '--smoke "' + $windowRoot + '"'
$startInfo.WorkingDirectory = $target
$startInfo.UseShellExecute = $false
$startInfo.WindowStyle = [Diagnostics.ProcessWindowStyle]::Normal
$startInfo.RedirectStandardOutput = $true
$startInfo.RedirectStandardError = $true
$process = [Diagnostics.Process]::new()
$process.StartInfo = $startInfo
try {
    if (!$process.Start()) { throw 'Own scene process did not start' }
    [void]$process.Handle
    if (!$process.WaitForExit(15000)) {
        [void]$process.CloseMainWindow()
        if (!$process.WaitForExit(2000)) { $process.Kill(); $process.WaitForExit() }
        throw 'Owned scene smoke exceeded 15 seconds and was stopped; preserve diagnostics'
    }
    $windowExit = $process.ExitCode
    [IO.File]::WriteAllText((Join-Path $target 'window-stdout.log'), $process.StandardOutput.ReadToEnd(), [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText((Join-Path $target 'window-stderr.log'), $process.StandardError.ReadToEnd(), [Text.UTF8Encoding]::new($false))
    if ($windowExit -ne 0) { throw ('Scene window failed, exit ' + $windowExit + '; inspect window-stderr.log') }
} finally { $process.Dispose() }

$checks = [Collections.Generic.List[object]]::new()
function Check([string]$Name, [bool]$Passed) {
    if (!$Passed) { throw ('Window acceptance failed: ' + $Name) }
    $checks.Add([ordered]@{name=$Name; status='Passed'})
}
$smokePath = Join-Path $windowRoot 'window-smoke.json'
$reopenPath = Join-Path $windowRoot 'window-reopen.json'
Check 'smoke_and_actual_reload_reports_exist' ((Test-Path -LiteralPath $smokePath -PathType Leaf) -and (Test-Path -LiteralPath $reopenPath -PathType Leaf))
$smoke = Get-Content -LiteralPath $smokePath -Raw -Encoding UTF8 | ConvertFrom-Json
$reopened = Get-Content -LiteralPath $reopenPath -Raw -Encoding UTF8 | ConvertFrom-Json
$smokeX = [BitConverter]::ToSingle([BitConverter]::GetBytes([uint32]$smoke.x_bits), 0)
$smokeY = [BitConverter]::ToSingle([BitConverter]::GetBytes([uint32]$smoke.y_bits), 0)
Check 'right_arrow_routed_through_winforms' ($smoke.input -ceq 'Right arrow: WinForms preprocessing + window handler + timer')
Check 'timer_reached_capture_frame' ($smoke.frames -ge 28)
Check 'position_is_finite' (![Single]::IsNaN($smokeX) -and ![Single]::IsInfinity($smokeX) -and ![Single]::IsNaN($smokeY) -and ![Single]::IsInfinity($smokeY))
Check 'arrow_moved_world_position' ($smokeX -gt 450 -and $smokeY -eq 1150)
Check 'smoke_session_money_is_50' ($smoke.money -eq 50)
Check 'smoke_character_identity_present' ($smoke.character_id -cmatch '^[0-9a-f]{32}$')
Check 'reload_preserves_exact_position_bits' ($reopened.x_bits -eq $smoke.x_bits -and $reopened.y_bits -eq $smoke.y_bits)
Check 'reload_preserves_character_identity' ($reopened.character_id -ceq $smoke.character_id)
Check 'reload_money_and_empty_journal' ($reopened.money -eq 50 -and $reopened.journal_count -eq 0)
Check 'closing_save_is_second_revision' ($reopened.revision -eq 2)
Check 'reload_preserves_session_ownership' ($reopened.same_object_identity -eq $true -and $reopened.shared_world_identity -eq $true)

$savePath = Join-Path $windowRoot 'runtime-data\saves\scene-smoke.save'
Check 'closing_save_exists' (Test-Path -LiteralPath $savePath -PathType Leaf)
Check 'initial_save_backup_exists' (Test-Path -LiteralPath ($savePath + '.bak') -PathType Leaf)
Assert-NoReparse $savePath
$save = Get-Content -LiteralPath $savePath -Raw -Encoding UTF8 | ConvertFrom-Json
Check 'save_envelope_and_content' ($save.sha256 -cmatch '^[0-9a-f]{64}$' -and $save.payload.schema -eq 1 -and $save.payload.content -ceq 'liaoxi-scene-v1')
Check 'saved_bits_equal_actual_reload' ($save.payload.x_bits -eq $reopened.x_bits -and $save.payload.y_bits -eq $reopened.y_bits)
Check 'saved_session_equals_actual_reload' ($save.payload.session.character_id -ceq $reopened.character_id -and $save.payload.session.revision -eq $reopened.revision)
Check 'saved_session_content_and_empty_commands' ($save.payload.session.schema -eq 1 -and $save.payload.session.content -ceq 'liaoxi-m2-inventory-v1' -and @($save.payload.session.commands).Count -eq 0)
# window-reopen.json is emitted only after unchanged SceneStore.Load succeeds.
# That real own-model reload validates the envelope checksum, deterministic
# session journal/state checksum, coordinate bounds and ownership. PowerShell
# only compares its reported values; it does not implement a second decoder.

$pngPath = Join-Path $windowRoot 'window.png'
Check 'window_png_is_nonempty' ((Get-Item -LiteralPath $pngPath).Length -gt 10000)
$header = New-Object byte[] 24
$stream = [IO.File]::OpenRead($pngPath)
try { $read = $stream.Read($header, 0, $header.Length) } finally { $stream.Dispose() }
Check 'png_signature_and_ihdr' ($read -eq 24 -and [BitConverter]::ToString($header,0,8) -ceq '89-50-4E-47-0D-0A-1A-0A' -and [Text.Encoding]::ASCII.GetString($header,12,4) -ceq 'IHDR')
$pngWidth = [int64]$header[16]*16777216 + [int64]$header[17]*65536 + [int64]$header[18]*256 + $header[19]
$pngHeight = [int64]$header[20]*16777216 + [int64]$header[21]*65536 + [int64]$header[22]*256 + $header[23]
Check 'png_contains_full_form_dimensions' ($pngWidth -eq $smoke.form_width -and $pngHeight -eq $smoke.form_height)
Check 'capture_includes_frame_above_client' ($smoke.client_width -gt 0 -and $smoke.client_height -gt 0 -and $smoke.form_width -ge $smoke.client_width -and $smoke.form_height -gt $smoke.client_height)

foreach ($source in $sourceEquivalence) {
    if ((Hash (Join-Path $root $source.path)) -ne $source.sha256) { throw ('Source changed during smoke: ' + $source.path) }
}
$reusedRoot = Join-Path $target 'validation\reused-models'
New-Item -ItemType Directory -Path $reusedRoot | Out-Null
Copy-Item -LiteralPath $priorManifestPath -Destination (Join-Path $reusedRoot 'prior-build-manifest.json')
Copy-Item -LiteralPath $priorAcceptancePath -Destination (Join-Path $reusedRoot 'prior-acceptance.json')
Copy-Item -LiteralPath $priorModelLogPath -Destination (Join-Path $reusedRoot 'prior-scene-tests.log')
Copy-Item -LiteralPath (Join-Path $root 'src\scene_runtime\README.md') -Destination (Join-Path $target 'README.md')
$notes = @'
# Window patch validation

This package runs one newly compiled gd-scene.exe --smoke instance. See acceptance.json,
validation/window/window.png, window-smoke.json and window-reopen.json. The screenshot
uses the complete form dimensions. The smoke sends Right through this form's WinForms
preprocessing, then uses its normal window handler and timer; it is not physical keyboard
automation or manual playtesting. The same process closes, saves and actually reloads
its own slot; that load validates the envelope and replayed session state checksums.

New model checks: 0. Reused model checks: 22 from m3-scene-20261022-01, with the prior
manifest, acceptance and model log copied under validation/reused-models. Every prior
compiled source except SceneWindow.cs and SceneProgram.cs must retain its accepted hash.
source-equivalence.json records that comparison. Model tests, model render PNGs and
old executables are not rerun or copied. README references to scene-tests.log or
validation/models refer to the previous full package, not new results in this package.

Only the first M3 scene node is covered. Combat, AI, trade, equipment and original
runtime/sample compatibility remain outside this patch. All smoke state stays under
validation/window/runtime-data/saves; normal interactive saves stay under runtime-data/saves.
'@
[IO.File]::WriteAllText((Join-Path $target 'VALIDATION-NOTES.md'), $notes, [Text.UTF8Encoding]::new($false))
Write-Json (Join-Path $target 'source-equivalence.json') $sourceEquivalence.ToArray()
$manifest = [ordered]@{
    built_utc=[DateTime]::UtcNow.ToString('o'); compiler_sha256=$compilerHash; exe_sha256=(Hash $exe)
    script='tools/build_m3_window_patch.ps1'; script_sha256=(Hash $PSCommandPath)
    prior_manifest='artifacts/m3-scene-20261022-01/build-manifest.json'; prior_manifest_sha256=$priorManifestHash
    prior_acceptance_sha256=$priorAcceptanceHash; prior_model_log_sha256=$priorModelLogHash
    new_model_checks=0; reused_model_checks=22; permitted_source_changes=$allowedChanges; compilation_reused=$compileReused
    sources=@($sourceEquivalence | ForEach-Object { [ordered]@{path=$_.path; sha256=$_.sha256} })
}
Write-Json (Join-Path $target 'build-manifest.json') $manifest
$report = [ordered]@{
    schema='grim-dynasty-m3-window-patch-v1'; status='Passed'; completed_utc=[DateTime]::UtcNow.ToString('o')
    new_model_checks=0; reused_model_checks=22; new_window_checks=$checks.Count; checks=$checks.ToArray()
    model_source_equivalence='All prior sources unchanged except the two permitted window entry sources.'
    compile_exit=$compileExit; compilation_reused=$compileReused; window_exit=$windowExit; window_smoke=$smoke; actual_reload=$reopened
    screenshot=[ordered]@{path='validation/window/window.png'; width=$pngWidth; height=$pngHeight; complete_form=$true}
    closing_save=[ordered]@{path='validation/window/runtime-data/saves/scene-smoke.save'; sha256=(Hash $savePath); validation='Actual unchanged SceneStore.Load in smoke process, followed by read-only report and persisted-field comparisons.'}
    scope='M3 first visible scene checkpoint window patch only. No new model tests, combat, AI, trade, equipment or original runtime/sample compatibility claims.'
}
Write-Json (Join-Path $target 'acceptance.json') $report
$files = @(Get-ChildItem -LiteralPath $target -File -Recurse | Sort-Object FullName | ForEach-Object {
    [ordered]@{path=$_.FullName.Substring($target.Length+1).Replace('\','/'); bytes=$_.Length; sha256=(Hash $_.FullName)}
})
Write-Json (Join-Path $target 'SHA256.json') $files
Write-Output ('M3_WINDOW_PATCH new_model_checks=0 reused_model_checks=22 new_window_checks={0} package={1}' -f $checks.Count, $target)
