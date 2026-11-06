# Observer terminal handling — owned-program validation, 2026-11-04

The revised cleanup passed 30 deterministic tests and four live tests using only two existing, owned harmless programs. The original game was not restarted or attached. The frozen attempt-7 observer and all historical failures remain unchanged. The candidate is inactive: it has no launch configuration or launcher.

## Changes and review

- `tools/static_analysis/observer_terminal.py` separates verified detach, same-handle process exit, and unknown state. It retains actual Stop/Detach errors, bounds native exit confirmation to two seconds, and never calls Continue or Kill.
- The candidate observer uses the helper and preserves capture failure separately from safe debugger closure. A safe close is not successful tracing or a diagnosis of an exit code.
- Independent review identified and closed three gaps: StopID changes during cleanup, missing/empty stop reasons, and an invalid recorded breakpoint being mistaken for successful removal. The candidate also verifies breakpoint removal after the API returns.
- Eight of the 30 pure tests extract the actual candidate callbacks via AST. The candidate main module was only syntax-compiled, never executed. The other 22 tests exercise terminal and failure races through deterministic fakes; these are not live runtime evidence.

## Actual live outcomes

| Case | Observed outcome | Scope of pass |
|---|---|---|
| normal | Three owned hits; Detach state 9; after LLDB exited, second private GO produced debugger=0 and exit 0 | Normal controlled detach and continuation proven on the owned fixture |
| fast | One hit; Detach returned success but LLDB was exited; native wait 258 / exit code 0 became wait 0 after about 63 ms; DONE still reported debugger=1 | Correctly classified as exited, **not** normal detach |
| exitcode | Explicit ExitProcess(0x80000003); native wait 0; no cleanup Stop/Detach/Continue/Kill | Preserves the code without inferring an exception cause |
| debugbreak | Actual DebugBreak, foreign reason 6, exception description and PC saved; helper refused all four commands | Correct refusal; only the test harness subsequently killed its precisely identified disposable fixture. This is not a normal program exit or an original-game cleanup policy |

Canonical exception evidence is `debugbreak-result.json` and `debugbreak-events.ndjson` in the batch directory. They preserve the exact unsigned value `18446744073709551615`. The JavaScript aggregate `result.json` rounds that large integer and must not be used as the source for that field. Raw evidence was not rewritten.

## Execution and release

Batch: `.tooling/lldb-terminal-revision-20261104/`. Formal launcher execution completed with exit 0 at 13:44:01 UTC. No native fixture rebuild occurred; the existing owned-Job helper was compiled once as tool infrastructure. All five debugger invocations were hidden command-line processes.

The owner retained the 5 GiB startup and 2 GiB runtime physical/commit gates. Observed minima were 15.397 GiB physical and 24.854 GiB commit headroom. It observed an empty Job, disposed it normally, and requested no Job termination. At 13:45:26 UTC, a separate read-only check found all 25 recorded PIDs absent, no LLDB or original-game process, and no build/Java conflict. Physical availability was 15.702 GiB and commit headroom 25.221 GiB. Both execution sessions ended; no pending operation remains.

## Original-game retry remains paused

These results establish the tested cleanup branches, not safety of another original-game attachment. Attempt 7 still has zero captured calls; its actual exception address, first/second-chance event and reason for exit remain unknown. Exit code 0x80000003 alone is not a diagnosis.

Live tests did not exercise running-to-Stop cleanup or establish a distinct interrupt reason in this installed LLDB. The installed binding lacks `eStopReasonInterrupt`; reason 6 also represents the deliberate DebugBreak and must not be broadly whitelisted. Unknown live stops remain fail-closed/manual holds. Before proposing an original retry, the remaining stop/exception observation and recovery path needs bounded validation on owned programs and review. No original restart or attach is scheduled by this checkpoint.

No saves, Steam configuration, system security settings, original binary, production gameplay component, commit, push or deployment was changed in this task. Current backup integrity and post-crash save contents were not reread during this batch; the previous backup check remains historical evidence only.

The adjacent JSON records exact hashes, raw artifact paths, outcomes and the resume point.
