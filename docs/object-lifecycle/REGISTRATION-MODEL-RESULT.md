# Bounded registration model — 2026-10-19

Later continuation: [single-layer growth and reserve](REGISTRATION-GROWTH-RESULT.md) extends this model. The unsupported-growth/clear-reserve statements and test counts below describe this historical checkpoint; its source copies are preserved under `.tooling/checkpoints/registration-before-growth/`.

This continues the [native backend and class-dispatch checkpoint](../host-migration/NATIVE-BACKEND-RESULT.md). The game remains at `D:\Steam\steamapps\common\Grim Dawn`; the project remains at `C:\Users\zhuzi\Documents\Grim-Dynasty`. The verified v2 handoff was already present. No older patch was reapplied.

## Static evidence

Three additional finite Rea 6.5.0 / Ghidra 12.1.4 batches passed: [20 container calls](rea-registration-containers/summary.json), [17 supporting calls](rea-container-support/summary.json), and [11 fill-path calls](rea-container-fill/summary.json). These are **48 calls, including 33 analysis calls and 15 lifecycle/status calls**. The earlier 18-call class-dispatch batch is separate.

All three batches confirmed the same Engine SHA256 before and after, acknowledged close, verified an empty final session, and exited with code 0 without forced termination. [All 13 inventoried original binaries remain unchanged](registration-container-validation/game-hashes-after.json). No target binary was loaded for execution.

[REGISTRATION-CONTAINER-EVIDENCE.md](REGISTRATION-CONTAINER-EVIDENCE.md) connects the recovered control flow to the saved pseudocode and assembly. The last two leaf functions establish that the existing-capacity clear path copies an empty range, fills 16 sentinel pointer slots, and restores eight logical buckets without shrinking capacity.

The observed release address `0x1802DC390` starts with an internal relative jump, not a direct IAT thunk. [The fixed-address PE check](registration-container-validation/free-thunk-0x1802dc390.json) stopped there. Its actual allocator implementation and side effects were not recovered. Decompiled labels alone are not proof of a runtime import identity.

## Independently compiled behavior

[RegistrationIndex.cs](../../src/task_items/RegistrationIndex.cs) models explicit caller-supplied snapshots using a sentinel doubly linked list and first/last endpoints for each bucket. It preserves raw uint keys, FNV-1a over four little-endian bytes, reverse duplicate lookup, existing-node identity and payload, occupied-bucket prepend, empty-bucket append, and deletion of the first contiguous matching key range. Empty-container misses take the clear/reset path too.

Successful results cover the observable list, bucket and payload state under a **normal allocation/release success assumption**. They do not claim native allocation, freeing, address lifetime, exception behavior, or raw object layout equivalence. Bounds on bucket count, node count and load factor are model coverage limits, not native restrictions.

Growth is detected after the new node is linked, as in the observed caller. The module retains that prefix and returns `Unsupported`, with an unknown final insertion flag. A clear requiring reserve likewise retains the already-cleared list. Such states become incomplete and reject subsequent mutations; they cannot be silently reused as completed native snapshots. Reserve, rehash, allocation failure and native exception behavior remain outside coverage.

[LoadedObjectRegistration.cs](../../src/task_items/LoadedObjectRegistration.cs) applies the observed sequence to these own containers and an already-loaded table:

`null object / zero ID guards → write Id2C → acquire own gate → erase Pending128 → insert ById30 → read RawKey28 → insert ByRawKey70 → release gate → supplied own Read callback`

Known duplicate insertion flags are ignored by the coordinator, so they preserve old mappings while Read still receives the incoming object. Unknown fields and operations are checked only when reached; previous ID writes, deletions and insertions are retained. Results expose completed/failed stages, actual container outcomes and whether Read began. Managed callback exceptions and `finally` cleanup follow an explicit own contract, not a recovered native exception ABI.

The manager references are stable and the caller supplies exclusive access through the own gate. Arbitrary concurrent mutation, native lock identity and integration with the existing ObjectIdAllocator key set are not covered. These modules are not connected to InventoryAdd or the original game.

## Validation

The project-local TaskItems build passed 183 new container assertions and 43 new registration-sequence assertions (**226 new assertions**). The same build also passed 40 existing ClassFactory assertions, 39 item-condition assertions, 11 item-auto assertions and ItemQuestDemo. A separate run against the newly built TaskItems DLL passed all 20 existing Object ID assertions: **110 existing assertions and one existing scenario** in total.

The own DLL references only framework assemblies and existing GrimDynasty modules. The build script parses successfully and `git diff --check` passes. No original Engine/Game/Steam assembly was loaded for execution or referenced by the own module.

The final targeted validation counts and artifact hashes are recorded in [acceptance-registration-model.json](registration-container-validation/acceptance-registration-model.json). Build output, Object ID regression output and assembly dependency inspection are retained alongside it.

The previous machine's 894 assertions / 16 scenarios are historical acceptance. This targeted validation is not a new full offline pass. The current UCRT fingerprint gate and its old whitelist are unchanged.

## Host loading and remaining work

This task still has no native Rea tool in its exposed catalog. The parent reports that a newly created same-project task also lacked Rea tools. No more tasks, host reload, application restart or global configuration changes were attempted. The approved stdio client closed after each batch; it is not persistent MCP injection or a background service.

[MCP-HOST-DIAGNOSTICS.md](../host-migration/MCP-HOST-DIAGNOSTICS.md) records supported read-only configuration/status interfaces and their exact local protocol shapes. A confirmed connection to the actual current host is still missing. No private session/history, credential file or mixed host log was read. This is an unavailable diagnostic path, not a denied action or a proven trust/configuration fault.

The full item factory still lacks record loading, the actual ClassInfo population and concrete constructors, name/key derivation, concrete native Read behavior, allocator/free internals, reserve/rehash, and integration with the live allocation/index lifecycle. The next step must select one of these from direct static evidence; do not treat the bounded registration model as a completed factory.

All requested action-level approvals for this continuation were allowed. No action remains pending or denied. No player saves, Steam cloud/userdata, DRM workaround, additional installation, system-security change, commit or push was involved.
