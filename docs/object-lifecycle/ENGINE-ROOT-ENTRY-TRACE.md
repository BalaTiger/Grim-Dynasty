# Engine entry-to-table evidence

2026-10-20. Bounded static continuation of ENGINE-ROOT-REGISTRATION-EVIDENCE.md.
No compilation, tests, game/DLL execution, Rea, Java, installation, policy changes,
saves/cloud access, commit or push. The previously refused build action was not
retried or replaced by an equivalent build command.

Five saved `offline-engine-root-static-*` batches add 697 code bytes.
These are address-level observations; a role label containing "CRT" is an
interpretation of the entry/reason pattern, not a substitute for import or
callee evidence.

| Observed fragment | Bytes | Relevant edge |
|---|---:|---|
| `2DD044` | 61 | Restores the three incoming arguments and tail-jumps at `2DD07C` to `2DCEF8`. For reason 1 it first calls `2DD93C`; that helper remains uninterpreted. |
| `2DCEF8` | 245 | For reason 1, calls `2DCFF0`, then `2DCCF8`, then `2DD9E8`, with nonzero gates before the latter two calls. |
| `2DCCF8` | 80 | Reason 1 takes `2DCD2D`, preserves the first argument and moves the third argument into RDX, then tail-jumps to `2DCD48`. |
| `2DCD48` | 299 | After earlier gates, calls `2DDBB6` with RCX=`303178`, RDX=`303188`; nonzero EAX takes a failure path. After another AL gate, calls `2DDBB0` with RCX=`302D48`, RDX=`303170`. |
| `2DDBB0`, `2DDBB6` | 6 + 6 | Tail-jump through IAT slots `302A58` and `302A50`, respectively. Import names are not inferred from adjacency. |

At `2DCD48`, a normal return from the second table call is followed by writing 2
to `420D40`; this call's return value is not tested. Failure branches in the
observed normal instruction flow return zero after the observed release helper.
Unread helpers, exception handlers and nonlocal exits remain outside this claim.

At `2DCEF8`, if the final reason-1 call (`2DD9E8`) returns zero, the code invokes
`2DD9E8`, `2DCCF8`, and `2DCFF0` with reason 0, ignoring their return values and
ultimately returning the original zero. If either preceding gate returns zero,
those later calls are skipped. The apparent clearing block at `2DCFD7` has no
observed normal incoming branch; no exception semantics are inferred.

The second table's half-open range spans exactly 1064 bytes / 133 pointer slots.
It is a bounded next metadata target, not proof that every slot is a class
initializer or that any initializer has run.

The saved AL from `2DC51C` is passed to `2DC840`; it is not a success gate.
The earlier `2DC68C` and `2DC58C` returns are tested as AL. If `420D40` is already
nonzero, `2DD7F4(7)` is called, but its nonreturning behavior is not established.
The observed failure paths do not themselves restore `420D40` from 1; unread
helpers may have side effects. After the second table returns and state becomes
2, additional pointer/callback checks and calls occur before `420DD0` is
incremented and 1 is returned. This investigation does not claim a complete
initialization, callback, rollback, or exception contract.
