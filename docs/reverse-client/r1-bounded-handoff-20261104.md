# Bounded no-hit / foreign-stop handoff: prepared, live validation blocked

The two-case owned-fixture implementation was completed and independently reviewed. **Neither new live case ran.** Formal execution reached the resource preflight at **2026-11-04 14:47:01.041 UTC**: physical availability **3.890 GiB**, below the unchanged **5 GiB** admission gate; commit headroom **9.202 GiB**. The snapshot also listed external `dotnet.exe` PID 31628, parent 47796; its ownership was not inferred or altered. The owner stopped before compiling its helper, creating a worker, creating a Job, or starting LLDB/fixture processes. This was a known preflight failure, not an approval denial or uncertain test outcome.

## Proposed finite validation

| Case | Evidence required before acknowledgment | State to retain |
|---|---|---|
| No breakpoint hit within the bounded window | Existing normal fixture, no first GO; exact late breakpoint remains at hit count 0; one-second observation with the same native identity alive and LLDB Running | Pause automated debugger actions only. **The target is still running, not suspended.** Never call the existing terminal helper, whose Running branch would issue Stop. |
| Foreign exception | Existing fixture's genuine DebugBreak; unchanged candidate rejects reason 6; snapshot includes native identity, StopID, thread PCs and reasons | Keep the same stopped state, StopID and thread evidence. No Continue, Detach or active Stop. |

Both cases use the same live LLDB session and a private fixed-schema read-only controller protocol. Requests bind batch nonce, session token, PID, creation time, LLDB process UniqueID, StopID, sequence and readiness snapshot token. The frozen test has five negative requests: wrong nonce, stale StopID, Continue, Detach and Kill. Only a valid `inspect-only` request is acknowledged; no request text is executed as a debugger command or expression. Snapshots are revalidated before and after every request.

After acknowledgment, the probe must retain the state for two seconds, measured with its monotonic clock, while collecting fresh snapshots. The gate independently checks identity, zero mutation counters, request-byte hashes and the UTC ordering of request publication, ACK, hold, explicit end and cleanup. A distinct sequence-7 `end-owned-test` request is required before the harness terminates its identity-verified disposable fixture. There is no second GO and no claim of natural completion for those cleanup exits.

An ACK means **the owned test controller observed the evidence**, not that a person has taken control. The actual human-takeover and unknown-recovery flags remain false. The planned no-hit case proves only a bounded no-hit observation, not that any arbitrary program can be known never to hit a breakpoint.

## Failure handling and evidence preservation

Review found that returning from a failed Python probe could allow LLDB `--batch` to continue toward quit and implicit detach. The frozen revision instead records failure, emits the exact FAILED marker and stays inside the same probe. Persistence/finalization errors also retain this hold and report the error when output is available. The gate responds by ending its worker so the existing owner can terminate **only its disposable owned Job**. Existing 60-second debugger / 240-second owner bounds and 2 GiB runtime protection remain unchanged.

This fallback is test cleanup, **not** a nondestructive original-game handoff policy. No-ACK timeout, FAILED-hold execution, controller disappearance and resource-loss branches were not live-tested in this batch. The two planned positive-to-hold cases also remain unexecuted because admission failed. Do not count code review, frozen hashes or the earlier deferred-detach success as this batch passing.

## Conditions still needed for a real user handoff

1. A person must be able to access the **same live debugger session** and its evidence. The current hidden batch process has no verified interactive user frontend; opening a second debugger is not an established transfer mechanism.
2. The debugger's owner and handles must remain alive while the person decides. Automatic batch exit, owner timeout or kill-on-close Job disposal cannot be described as preserving an unknown original stop. The disposable-target policy above must not be copied onto the original game.
3. The user must be told whether the target is Running or Stopped, the exact known identity and stop evidence, and which actions may change exception handling. Unknown exceptions must not be resumed or detached automatically. Absence of acknowledgment must remain “not handed off.”

Therefore **there is not yet a demonstrated complete, reliable original-game handoff/recovery path** for either an indefinitely unhit breakpoint or an unknown exception. The safe current boundary is to keep original tracing paused. The earlier owned deferred-detach test remains valid only when an exact owned breakpoint is actually reached.

## Saved state and next finite step

Prepared files and the failed preflight receipt are in `.tooling/lldb-bounded-handoff-20261104/`: `probe.py`, `gate.mjs`, `manifest.json`, `launch-owned.ps1`, `prepared.json`, and `launch-result.json`. There is no aggregate `result.json`, no case result, and no ongoing execution. The stage JSON beside this report records hashes and the exact admission sample. The first receipt is immutable; the owner intentionally refuses to overwrite it.

If resource conditions later permit resumption, first perform a fresh read-only check of both 5 GiB admission thresholds and external conflicts. Prepare a distinct reviewed attempt/receipt, preserving this failure and all guards, then execute only the two frozen cases. Do not replay previous suites or launch the original. A new attempt has not been scheduled or started here.

No new software, visible application, original program, save/Steam access, security-setting change, commit, push or deployment occurred. No external process was stopped. Formal launcher session 52215 ended with exit 1 (final chunk `30a263`); its receipt says `cleanup_state=no-worker-started`, not an unknown Job release.
