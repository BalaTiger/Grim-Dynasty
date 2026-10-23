# Object base constructor checkpoint — 2026-10-19

Later continuation: [Object name assignment and registration connection](OBJECT-NAME-RESULT.md).

The exported Engine `GAME::Object()` initialization is now represented by independently compiled own code. Its exact write set is small and closed: base dispatch identity, name capacity 15, name length 0 and the first name byte NUL. It has no callees. **It does not initialize the raw name key at `+0x28` or the object ID at `+0x2C`.** Those fields remain unknown or retain the caller's previously known values.

The implementation is [ObjectBaseConstructor.cs](../../src/task_items/ObjectBaseConstructor.cs). `Create()` produces separate own field storage with both untouched fields unknown. `Initialize(existingFields)` retains that exact field-storage object and its values. The base dispatch marker and empty-name metadata are logical state; they are not original addresses, a native object layout, cleared padding, allocation/destruction behavior, a complete item constructor, SetObjectName or Read.

## Evidence and selection

[OBJECT-BASE-EVIDENCE.md](OBJECT-BASE-EVIDENCE.md) records the caller chain, export identities, virtual-slot mapping, exact constructor writes and the remaining name/record boundaries. The fixed Rea 6.5.0 / Ghidra 12.1.4 batch queried the default constructor, SetObjectName and TableDepot::LoadFile only: nine analysis calls and five lifecycle calls. [The batch summary](rea-object-base/summary.json) confirms 14/14 passed, unchanged Engine fingerprint, acknowledged closure, an empty final session and child exit 0 without forced termination.

SetObjectName hashes the full stored string length using signed bytes and 32-bit FNV arithmetic; embedded NUL bytes do not terminate that loop. Its ordinary assignment path depends on `0x180023180`, which has not been analyzed in this continuation. No name-assignment implementation was added. LoadFile's name processing, cache lookup and cache-miss loading dependencies likewise remain outside the implemented scope.

The exported Object base `+0x08` slot points to bytes beginning `C2 00 00`. This is not evidence of the actual Read body of a concrete Item or other derived object. No Read method or empty Read substitute was added. Existing test callbacks remain explicitly supplied own callbacks.

## Validation

A fresh build in `.tooling/object-base-build/` passed **40 new constructor assertions** and the existing 481 assertions plus ItemQuestDemo: **521 assertions and one existing scenario**. The new tests cover retained field identity and bit patterns, independent construction, the explicit own ClassInfo callback, and registration after construction. An unknown raw key stops registration at RawKeyRead, preserves the already completed ID/pending/index effects, releases the own gate and does not invoke Read. Separately known keys, including zero and high-bit values, remain usable.

The standalone Object ID regression initially used a relative source argument rejected by the legacy compiler; rerunning with the absolute project source path compiled and passed all 20 assertions. No source change was needed for that invocation issue. The own DLL references only framework and existing GrimDynasty assemblies.

[Acceptance and fingerprints](object-base-validation/acceptance-object-base.json), [build output](object-base-validation/build-output.txt), [Object ID regression output](object-base-validation/object-id-regression-output.txt), [assembly references](object-base-validation/task-items-dependencies.json) and [original binary hashes](object-base-validation/game-hashes-after.json) are retained. The historical 894-assertion / 16-scenario acceptance is not claimed as rerun; the UCRT fingerprint whitelist remains unchanged.

## Checkpoint and next bounded work

The previous runner and build script are preserved under `.tooling/checkpoints/object-before-base/`; the prior growth checkpoint remains intact. [The new checkpoint manifest](object-base-validation/checkpoint-manifest.json) records the current own sources, fixed plans/runner, static evidence and validation records copied to `.tooling/checkpoints/object-base-passed/`. This is an incremental source/evidence checkpoint, not a complete standalone game package.

The next direct static candidates are the name assignment helper `0x180023180`, or LoadFile's miss helper `0x1801C4E20` to establish concrete table selection before claiming a table getter implementation. Both table vslot targets are now known from static metadata, but their behavior has not been restored. Concrete ClassInfo populations, derived constructors, real Read implementations, record loading and full factory/registration integration remain incomplete. These are explicit implementation boundaries; this work has not established a need for dynamic game execution.

All 13 tracked original executable/DLL hashes remain unchanged. The action-level approval for the fixed batch was allowed; no action is denied, pending or uncertain. No extra software, global configuration/trust change, host reload/restart, original-game execution, save/cloud access, DRM workaround, commit or push occurred. The finite approved Rea connection was closed; no persistent native MCP injection is claimed.
