# Bounded owned-controller hold validated

The two prepared cases ran successfully on 2026-11-04 after current-session low-frequency resource monitoring met the unchanged admission conditions. **This validates bounded read-only controller acknowledgment and state retention on owned test programs. It does not establish human takeover or nondestructive recovery from an unknown original-game stop.**

| Case | Observation and valid acknowledgment | Actual retained state |
|---|---|---|
| `no-hit` | 1.000-second no-hit window; valid read-only ACK followed by a 2.047-second hold, 20 hold snapshots | Same native process identity, LLDB Running (6), StopID 1, owned late breakpoint hit count 0. No first GO. Only automated debugger actions paused; the target was not suspended. |
| `foreign-stop` | Real fixture DebugBreak rejected by the unchanged candidate; valid read-only ACK followed by a 2.062-second hold, 20 hold snapshots | Same identity, LLDB Stopped (5), StopID 2, reason 6, thread PCs and reasons. The exception thread was `0xd6ec`, PC `0x7ffbedb43123`, description address `0x7ffbedb43122`. No unknown exception was resumed. |

Each case rejected five requests: wrong nonce, stale StopID, Continue, Detach and Kill. The sixth request accepted only `inspect-only`. All request/ACK hashes and identity/StopID/snapshot bindings matched. Both cases had zero Stop/Continue/Detach/Kill calls during acknowledgment and hold. Each had one initial attach Continue **before** that interval. A separately bound sequence-7 `end-owned-test` request then permitted one identity-verified Kill of that case's disposable fixture. Cleanup timestamps follow end confirmation; there is no unexpected-continuation marker or DONE output. The fixture exit codes of zero came from explicit test cleanup, not natural completion or recovery.

The actual record therefore supports **10 negative-request rejections, 2 valid controller acknowledgments, and 2 bounded holds**. Acknowledgment came from the owned test controller, not a person. The `safe_to_close_debugger=true` terminal flag came only after fixture termination was independently proved. The original helper, candidate and previous evidence remain unchanged. No old completed suite or original query was repeated; the native fixture was reused without recompilation.

## Resources and release

There were 13 read-only samples from 14:52:48 to 15:20:03 UTC, recorded in `resource-watch.ndjson`. No persistent automation or scheduled task was created. The final two samples, 127.633 seconds apart, showed:

- 15:17:55 UTC: 5.479 GiB physical, 10.162 GiB commit headroom, no conflict.
- 15:20:03 UTC: 5.398 GiB physical, 10.226 GiB commit headroom, no conflict.

The exact owner performed its own fresh checks before execution. The run lasted **15:20:22.166–15:20:36.195 UTC**, formal session 48319, exit 0, final chunk `881668`. Ten owner samples had minima **5.156 GiB physical / 9.979 GiB commit headroom**; the original 5 GiB admission and 2 GiB running gates remained intact. Only the existing supervision helper compiled once; no native fixture or game build occurred.

The owner observed an empty Job and disposed it without a termination request, failure or cleanup failure. An independent read-only check at **15:21:04.544 UTC** found all **14 recorded PIDs** absent, no LLDB/original-game process and no external build/Java conflict. Availability was **7.174 GiB physical / 10.187 GiB commit headroom**. The resource-watch cell completed before launching the tests; no monitoring process, test, Job or tool call remains pending.

The earlier 14:47 resource-gate failure is preserved in `.tooling/lldb-bounded-handoff-20261104/`. This run used `.tooling/lldb-bounded-handoff-20261104-retry1/`; the independent reviewer confirmed only output-directory literals and matching hashes changed. No protection was relaxed and no approval denial was bypassed.

## Remaining boundary and next decision

No-ACK timeout, failure-hold execution, controller disappearance, or resource loss during handoff were not exercised. The source-level fail-hold revision prevents returning normally into batch quit and delegates **disposable owned Job** cleanup to the owner, but that is not a nondestructive policy for the original game.

Actual human takeover still needs a verified way for the person to access the **same** live debugger session, an owner/handle lifetime that survives the handoff, and an explicit decision for an unknown exception. The current hidden `--batch` session and automatic controller ACK do not provide those conditions. No-hit does not guarantee that a future safe breakpoint will occur. The earlier exact-breakpoint deferred-detach success remains a separate, narrower result.

Keep original tracing paused. This stage schedules no original retry and no additional tests. If further work is requested, the next finite decision is whether to establish an interactive same-session ownership path, then test its timeout/disappearance policy only on disposable targets. Any visible application launch must first be communicated as previously required. No new software or security change was needed for this batch.

Raw case results, ACK/request files, snapshots, backend logs and owner/release receipts are in the retry directory. Their hashes are recorded in the companion stage JSON. The independent result review confirmed both holds, rejection counts, snapshot stability, cleanup ordering, matching evidence hashes and the absence of unintended continuation. Original-game cause/compatibility remains unknown; no original program, save/Steam data, security settings, commit, push or deployment was changed.
