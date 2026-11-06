# Original attempt 7: asynchronous debugger investigation

2026-11-04 UTC. Original game tracing remains paused. No original executable was started or loaded as a target in this investigation; no save or Steam data/settings were accessed. All fixture processes were hidden. Production reconstruction code and the failed original observer were not changed.

## Confirmed original facts

Attempt 7 attached at about 12:52:11 UTC and armed a software breakpoint at loaded Game.dll + 0x15E1F0. After entering the existing character's scene and the first nearby-ground click, the observer recorded `original-exited` at about 12:55:42 and LLDB printed exit status `0x80000003`. `trace-events.ndjson` is empty; neither `latest_stop_inspection` nor `observed_native_hit_count` was recorded. This proves zero captured calls, not that no native breakpoint ever occurred.

The observed minima were 13.758 GiB physical available and 20.808 GiB commit headroom, above the 2 GiB runtime guards. Exit preceded the 600-second limit. There is no recorded resource or timeout trigger. The precise exception address, first/second-chance event and reason for original termination remain unknown.

The cleanup defect is separate: one native liveness check had not yet confirmed exit, then the code required LLDB `stopped` even though LLDB was already `exited`. It entered permanent holding. Later, a same-creation native handle confirmed signaled termination and exit code `0x80000003`; the independently approved cleanup released only the owned observer processes. Historical failure and `safe_to_close_debugger=false` are preserved.

Original evidence: [attempt-7 checkpoint](r1-click-attempt7-exit-20261104.json), [raw receipt](../../.tooling/gd-click-observation-20261104/trace-result.json), [LLDB output](../../.tooling/gd-click-observation-20261104/lldb.stdout.log).

## Actual hidden fixture experiments

Same installed LLDB/library fingerprints, `windows` process plugin, synchronous attachment followed by `SetAsync(True)` and `GetState/GetStopID` polling. The probes consume no listener events. Exact executable path/hash, PID and creation time were checked; 5 GiB admission, 2 GiB runtime protection and owned Job supervision remained enabled.

| Experiment | Actual result | Scope of conclusion |
|---|---|---|
| Existing one-call fixture | Exact owned hit 1; breakpoint removed; Detach returned success but state was 10/exited; native wait was 258 with exit code 0; fixture printed `debugger=1` and exited 0. Probe failed, preserved. | Immediate state-9 assertion can fail during fast exit. This was not successful verified detach. |
| New controlled normal fixture | Three exact owned hits; first two continued; third removed breakpoint and detached with state 9. After LLDB exited, a second private GO produced `debugger=0`, result 42 and exit 0. | This asynchronous polling route can work. Missing explicit `WaitForEvent` is not established as the original failure cause. |
| Explicit exit-code fixture | `ExitProcess(0x80000003)`; zero breakpoint hits; same native handle signaled with code `0x80000003`. | The exit status alone does not establish an exception event or its origin. |
| Deliberate DebugBreak fixture | Foreign reason 6, data UINT64_MAX and an exception description/address were recorded. No Continue or Detach followed that stop. Only the precisely identified disposable fixture was killed. Native wait changed 258 to 0 in about 60 ms. | Foreign exceptions must remain rejected. Initial attach reason 6 cannot become a global resume whitelist. The negative-case pass is not normal program completion. |

First experiment: [result](../../.tooling/lldb-async-investigation-20261104/poll-result.json), [event journal](../../.tooling/lldb-async-investigation-20261104/poll-events.ndjson).

Controlled experiments: [normal](../../.tooling/lldb-async-controlled-20261104/normal-result.json), [exit code](../../.tooling/lldb-async-controlled-20261104/exitcode-result.json), [DebugBreak](../../.tooling/lldb-async-controlled-20261104/debugbreak-result.json), [normal fixture output](../../.tooling/lldb-async-controlled-20261104/normal-fixture.stdout.log), [batch result](../../.tooling/lldb-async-controlled-20261104/result.json).

One new native fixture was compiled once; no prior project test suite or Rea query was repeated. Fixture SHA256: `8c06864cbde3976bed269aa79492c849ec5cc8efd6365d73f77e8634f7d2f1b8`.

## Resource release and limitations

The first experiment's Job was observed empty and disposed. All three controlled cases passed, but their launcher failed its five-second empty-Job observation because its owned compiler `vctip.exe` was still present. That historical failure remains unchanged; the existing kill-on-close Job was disposed.

The independent final check at 13:22:12 UTC found no remaining PID in the union of 25 recorded workers, Job samples and child steps; no LLDB or original game instance remained. Physical available was 15.614 GiB and commit headroom 25.412 GiB. This current release evidence does not fabricate a past empty-Job observation. [Final process check](../../.tooling/lldb-async-controlled-20261104/final-union-release-check.json).

## Bounded next step

Before any further original observation, prepare and review an observer-only cleanup change: distinguish detached from exited, and for exited use a bounded wait on the exact original native handle. If terminal identity remains uncertain, preserve unknown and stop. Never transfer the disposable-fixture Kill branch into the original observer. Keep the compiler helper timeout issue separate from the original exit cause.

The original cause still needs exception-address and event-level evidence from a separately reviewed observation plan. This investigation does not authorize a new original launch and does not demonstrate a reconstructed interactive game.

Microsoft documents that `ExitProcess` accepts the supplied exit code, and that kernel shutdown after an exit debug event can complete later than event delivery. These explain possible distinctions, not the original root cause: [ExitProcess](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-exitprocess), [Debugging events](https://learn.microsoft.com/en-us/windows/win32/debug/debugging-events). Upstream LLVM 9 CLI event handling is only a reference for the separately built JetBrains binary: [CommandInterpreter](https://raw.githubusercontent.com/llvm/llvm-project/llvmorg-9.0.0/lldb/source/Interpreter/CommandInterpreter.cpp).
