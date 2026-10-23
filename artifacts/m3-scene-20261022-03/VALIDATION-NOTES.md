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