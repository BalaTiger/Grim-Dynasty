# Navigation continuation from the camera closure

This report preserves the earlier10C6C0 investigation. The subsequent three
targets and single-polygon implementation are now completed within the bounded
scope recorded in [navigation core result](R1-NAVIGATION-CORE-RESULT.md).
No playable client or complete navigation is validated by either stage.

New fixed Rea batch `r1-engine-navigation-search` inspected Engine10C6C0 only:
8/8 calls completed15:42:29–15:43:57UTC. Pseudocode, assembly and direct-call
responses are saved. Normal session/process/Job/sampler cleanup and unchanged
Engine before/after SHA256 are recorded in summary.json. No old target requery.

The previously saved150590 wrapper loads RCX from this+8 and tail-jumps10C6C0;
its stack byte argument is preserved. It is not itself a complete search body.

10C6C0 directly dereferences both region pointers. Signed integer region offsets
at+3C/+40/+44 are converted to float and added to local coordinates. A zero
region token is not a valid fabricated navigation region. The first two actual
calls are2610F0 at10C7EF and10C836, each reloading this+10 and sharing the
captured this+18 filter address, with different extents/output buffers. Both require a nonnegative status
and a nonzero reference before later processing.

After distance gating, a matching supplied reference can return true early.
That branch may write the optional destination via22C350/22C620; it skips the
later path length, path vector, direction and counter work. A success boolean
therefore cannot imply every optional output was written.

The noncached branch increments DWORD+76E8, calls261CB0 with limit400hex, then
262480 with limit800hex. It rejects failed status/zero counts and requires the
last corridor reference to match the requested end unless the final byte flag
allows the other case. Result buffers at+1F0,+11F0,+71F0 and their valid capacities
must be established; they are not initialized by assumptions.

With FP exceptions masked, UCOMISS/JNZ at10C872 sends NaN through the +0 branch;
COMISS/JA at10C883 rejects only ordered greater-than, so a NaN tolerance does not
reject. Length accumulation10CAC5 similarly treats a NaN segment as0. Plain
source-language comparisons would change these branches. The final direction
normalization has no observed zero-length fallback or count>=2 guard, yet reads
the second point. Status checks test the sign bit and counts test nonzero; do not
silently strengthen the native checks to positive counts.

The subsequent fixed targets2610F0,261CB0,262480 have been inspected. Their
actual bodies and required single-polygon helpers support a conditional own
component, not a full production search. The next producer gap is261950/260520
plus query/tile/filter initialization. Do not infer an external library algorithm
from code shape or function names.22C350/22C620 and vector emission remain
separate output dependencies; first reuse existing evidence before a new query.

Physical mouse input and target production remain open: existing044A10 consumes
2D coordinates/viewport but does not establish scene picking;04E170 dispatches
virtual+28 on a concrete object still to be bound. Existing00D480 returns false.
Those entry labels cannot substitute for a verified click-to-world target chain.

The three-target and direct-helper batches have finished and released resources.
The261950/260520 follow-up is not prepared or started; task autonomy remains
active. Runtime state and the exact continuation are in the main checkpoint;
the earlier navigation-search progress file remains historical stage evidence.
