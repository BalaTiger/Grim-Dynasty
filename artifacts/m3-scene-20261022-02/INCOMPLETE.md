# Window attempt not accepted

Compilation succeeded (empty compile.log). The own window produced its arrow-movement report, closing save and actual SceneStore reload report, with empty stdout/stderr. No own scene process remained after the call.

The PowerShell wrapper received a null Process.ExitCode after Start-Process/WaitForExit/Refresh and stopped before acceptance checks. The actual process exit code from this attempt was not captured; it must not be reported as confirmed zero. There is no acceptance.json for this attempt.

The retry uses a retained System.Diagnostics.Process handle and the exact same compiled executable, SHA256 `6ce0ca305b2dabae821afa5ee5ff962c86cb406c401d183159a8002830584cc3`, in a fresh output directory. No model tests or compilation need to repeat. This was a harness result-collection failure, not an approval denial.
