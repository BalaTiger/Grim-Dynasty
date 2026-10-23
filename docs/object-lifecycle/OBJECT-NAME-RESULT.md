# Object name assignment checkpoint — 2026-10-19

Later checkpoint: [bounded LoadFile cache hits and combined lifecycle regression](RECORD-LOAD-RESULT.md), with the [current overall progress map](LIFECYCLE-PROGRESS.md). The remaining-load discussion below describes this earlier checkpoint.

The `SetObjectName` call domain is now represented by independently compiled own code, including assignment to a distinct name string, self-alias rehash, short/heap capacity retention and bounded successful growth. The domain is valid logical byte strings with length at most 65536, exclusive access and normal native allocation/copy/release success. Generic substring assignment, native pointer/layout effects, invalid storage, exceptions and native OOM behavior are not claimed.

[ObjectNameAssignment.cs](../../src/task_items/ObjectNameAssignment.cs) models the setter, using [ObjectNameString.cs](../../src/task_items/ObjectNameString.cs) for stable name-storage identity, complete payload and capacity metadata. The existing base-constructor model now exposes name metadata through read-only properties. This preserves source-level readers, including its original tests; it is not binary compatibility with a previously compiled DLL.

## Restored behavior

The outer self-alias branch skips assignment but still hashes the complete stored length and writes the raw name key. Distinct sources copy their full payload and set a terminating NUL. Embedded NUL bytes participate in the signed-byte FNV32 loop. Neither text decoding, case folding nor slash normalization is introduced. The setter never assigns the object ID.

The target retains its capacity when the new payload fits, even when a heap name is shortened or cleared. The source capacity is not copied. Growth preserves the assembly's strict comparison: start with `rounded = requestedLength | 15`; replace it with `oldCapacity + oldCapacity/2` only when `oldCapacity/2 > floor(rounded/3)` in the bounded domain. For example, capacity 235 and requested length 336 produce 351, not 352. This cannot be replaced by a generic maximum formula.

Construction still leaves `RawKey28` and `Id2C` unknown or unchanged. Assigning an empty name is a separate operation that writes key `0x811C9DC5`. Budget rejection and own snapshot-allocation failure stop before model mutation; this is a coverage/implementation boundary, not a prediction of native allocation failure or rollback.

## Evidence and registration connection

[OBJECT-NAME-EVIDENCE.md](OBJECT-NAME-EVIDENCE.md) records the exact calling convention, helper paths, instruction addresses, growth proof and exclusions. Two fixed Rea 6.5.0 / Ghidra 12.1.4 batches queried `0x180023180` and `0x180022F70`: six analysis calls and ten lifecycle calls. Both [assignment](rea-object-name-assignment/summary.json) and [growth](rea-object-name-growth/summary.json) summaries confirm 8/8 passed, acknowledged closure, an empty final session, child exit 0 and unchanged Engine fingerprint. Static PE metadata separately binds the copy thunk to `VCRUNTIME140.dll!memcpy`; the CRT implementation is not restored.

The own pipeline is now: construct logical base state, successfully apply the requested name, then pass that same state's `Fields` to `LoadedObjectRegistration.Apply`. The setter supplies the previously unknown `RawKey28`; the registration coordinator can use it after its ID insertion, complete the raw-key insertion, unlock and invoke the explicitly supplied own callback. A known loaded table and actual callback remain required. A missing callback still produces Unsupported after the completed registration prefix.

The setter does not maintain existing indices. Renaming a previously registered model changes its field but does not re-key the old index entry. No automatic rename bookkeeping was invented. Concrete ClassInfo populations, derived constructors, LoadFile/record loading and real derived Read bodies remain incomplete. The next direct static candidates are LoadFile's `0x1801C4E20` miss helper and `0x18027B3C0` name-processing helper, followed by concrete table provenance and getter/Read behavior.

## Validation and checkpoint

A fresh build in `.tooling/object-name-build/` passed 37 new name-assignment assertions, the unchanged 40 constructor assertions and the prior 481 assertions: **558 assertions and one existing ItemQuestDemo scenario**. Fixtures include signed-byte/NUL golden vectors, self-alias, equal-value distinct sources, the 235/336 growth boundary, source/target isolation, the inclusive 65536-byte budget, unchanged target state on 65537-byte rejection, and the actual own registration coordinator. The own DLL references only framework and existing GrimDynasty assemblies.

The final per-suite counts and source/binary fingerprints are in [acceptance-object-name.json](object-name-validation/acceptance-object-name.json), with [build output](object-name-validation/build-output.txt), [Object ID regression](object-name-validation/object-id-regression-output.txt), [independent hash vectors](object-name-validation/hash-vectors.json) and [assembly references](object-name-validation/task-items-dependencies.json).

The previous runner, base-constructor source and build script are preserved under `.tooling/checkpoints/object-before-name/`. [The passed checkpoint manifest](object-name-validation/checkpoint-manifest.json) records the current source, fixed plans/runner, evidence and validation copied to `.tooling/checkpoints/object-name-passed/`. This is an incremental checkpoint, not a complete game or original-source restoration.

All 13 original executable/DLL hashes remain unchanged; see [the final hash check](object-name-validation/game-hashes-after.json). The UCRT whitelist is unchanged and the historical 894-assertion / 16-scenario acceptance is not claimed as rerun. No original game execution, save/cloud access, DRM workaround, extra software, global configuration change, reload/restart, commit or push occurred. Both action-level approvals were allowed; no action is denied, pending or uncertain. The approved Rea sessions were finite and closed.
