# Running-to-Stop observation — 2026-11-04

Three bounded observations completed on the existing owned harmless fixture. **Two real Stop calls succeeded, but their stops remain unrecognized for automatic detach by the unchanged observer.** The third process exited before Stop could be called; an overlapping Stop/exit race was not exercised. These are observation results, not original-game safety or gameplay acceptance.

## Actual results

| Case | Before helper | Real helper Stop | Observed stop/terminal | Subsequent handling |
|---|---|---|---|---|
| stop-before-go | SDK Running (6), original native handle alive; fixture waiting for first private GO | One call, success | StopID 1 → 2, Stopped (5), reason 6, data `0xffffffffffffffff`, exception `0x80000003`; independent event `interrupted=true` | Candidate callback rejected the reason; helper returned Unknown without Detach/Continue/Kill. Harness alone verified and terminated its own disposable fixture |
| stop-after-work | Three actual late calls completed (`result=42`), SDK Running, fixture waiting for second GO | One call, success | Same classification and event flag as above | Same refusal and owned-only test cleanup; no second GO |
| stop-exit-race | Explicit ExitProcess already complete, SDK Exited (10), native wait 0 | Zero calls | Preserved exit `0x80000003` | No Stop/Detach/Continue/Kill. Saved as `not-tested-exit-before-Stop`; no retry to manufacture overlap |

Both live Stop descriptions reported exception address `0x7ffbf07a3f40`, with PC one byte later. This is a runtime address from these owned processes, not evidence of the original game's fault address. Their reason 6 and sentinel data overlap with prior owned DebugBreak observations; the cause cannot be identified from those fields alone.

The test fixture was genuinely running from the debugger's perspective, but the two steady-state cases deliberately waited on a private input pipe. This is not a busy render loop, arbitrary multithreaded workload, or original-game validation.

## Evidence and unchanged boundaries

The new probe reuses the exact `observer_terminal.py` and extracts the candidate's actual `inspect_stop` callback through AST. It does not call Stop before the helper, rewrite its result, whitelist reason 6, or alter production code. A separate listener consumes only its own state-event queue. Each event carries process PID/unique ID, state, interrupted/restarted flags and description. All three listeners unsubscribed successfully; no listener errors were recorded. Large integers are stored as hex strings to preserve precision through the Node aggregate.

`interrupted=true` is useful corroborating event data in these controlled experiments. It is not sufficient authorization to resume an arbitrary exception stop. The installed bindings expose `GetInterruptedFromEvent` but not `GetStopEventForStopID`. Upstream SBProcess implements Stop through Halt and exposes the event flag; upstream process handling also permits an interruption request to mark a later natural stop. These references explain the API boundary, not equivalence with the installed JetBrains fork. [LLDB 9 SBProcess source](https://github.com/llvm/llvm-project/blob/llvmorg-9.0.0/lldb/source/API/SBProcess.cpp), [LLDB process implementation](https://lldb.llvm.org/cpp_reference/Process_8cpp_source.html).

Canonical results, timestamped events, private fixture stdout and the exact command/identity receipts are in `.tooling/lldb-running-stop-20261104/`. No previous suite was replayed and no fixture was recompiled. The owned-Job helper was compiled once as existing supervision infrastructure. The existing helper, candidate and frozen original observer hashes remain unchanged.

## Original failure log addendum

The separate `r1-attempt7-log-addendum-20261104.md` records the new read-only audit. The only saved worker cleanup request is later than original termination. Initial-attach thread data is not a failure snapshot, and zero completed captures does not establish zero breakpoint hits. All stdout/stderr was retained, but the original probe did not collect exception address/chance or backend event evidence. No missing causal details can be reconstructed from those saved files. There is no complete pre-failure Stop-call audit; do not convert the lack of a recorded call into proof it never happened.

## Release and resume point

Formal execution ended successfully at **14:00:58 UTC**. The owner observed an empty Job and disposed it, with no Job termination request, failure or cleanup failure. Resource minima were **14.332 GiB physical** and **24.244 GiB commit headroom**, with the original 5 GiB admission and 2 GiB running gates unchanged. At **14:01:51 UTC**, an independent read-only check found all **17 recorded PIDs** absent, no LLDB/original-game process and no build/Java conflict. Availability was 14.026 GiB physical / 24.718 GiB commit headroom. No execution remains pending.

Original-game tracing remains paused. What is now known is the actual running-to-Stop representation and correct conservative refusal on this installed debugger. What remains unproven is non-destructive recovery from that stop, overlapping Stop/exit handling in a real run, and the original exit cause. A separately reviewed bounded owned-program recovery plan is the next possible step; this checkpoint schedules no original launch/attach and no new tests.

No saves, Steam settings, system security settings, original binary, production gameplay code, commit, push or deployment was changed. Historical failures and the inactive candidate are preserved.
