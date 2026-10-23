# Single-layer registration-index growth — 2026-10-19

Later continuation: [Object base constructor and record-interface boundary](OBJECT-BASE-RESULT.md).

This continues the [bounded registration model checkpoint](REGISTRATION-MODEL-RESULT.md). The next implemented gap is successful bucket reserve and **one layer of rehash**, preserving the observed linked-list order and node identities. Concrete native constructors and Read bodies were not added.

## Evidence and scope

The fixed Rea 6.5.0 / Ghidra 12.1.4 [reserve plan](../../tools/static_analysis/rea-container-reserve-plan.json) made 11 calls: six analysis queries and five lifecycle/status queries. Both direct callees, `0x180022F00` and `0x180023090`, came from the already observed `0x18001BFB0` reserve helper. [The batch summary](rea-container-reserve/summary.json) confirms 11/11 passed, unchanged Engine SHA256, acknowledged closure, an empty final session and child exit 0 without forced termination.

The helpers check allocation-size multiplication. Blocks below 4096 bytes use direct allocation; blocks at or above that size request 39 extra bytes and produce a 32-byte-aligned pointer with its original allocation address saved immediately before it. The corresponding release path validates alignment and the saved-base offset before calling the release routine. The model operates on logical bucket slots in managed arrays. It does not reconstruct native pointer bytes, allocator internals, invalid-parameter handlers or failure behavior.

[REGISTRATION-GROWTH-EVIDENCE.md](REGISTRATION-GROWTH-EVIDENCE.md) records the assembly-derived algorithm, caller relationships and the proof that nested maintenance is a no-op in the supported domain.

## Implemented behavior

After a new unique node is linked, growth still triggers only when binary32 load is strictly greater than the threshold. Bucket counts below 512 grow eightfold; counts at or above 512 grow twofold within the model's bounded domain. Reserve increases capacity exactly to the requested bucket count when needed, while already sufficient capacity is retained. Clear can now reserve eight buckets when the old capacity is smaller.

Rehash resets bucket endpoints, saves the original tail node, and repeatedly processes the current global head. A new bucket is appended to the processed tail region; a distinct key in an existing bucket is prepended to that bucket. An equal key preserves the first encountered node and payload, removes the later duplicate and decreases count. The loop stops after processing the saved old tail by identity, even if that iteration deleted it. No Dictionary, replacement-node rebuild or numeric bucket sorting substitutes for this order.

Successful growth requires the requested bucket count to be at most 4096 and the load after that single growth to be at or below the threshold before rehash starts. Count can only stay the same or decrease during rehash, so the maintenance calls made by each successful node move cannot trigger another growth in this domain. Multiple independent insert calls may each perform a supported growth.

If one layer is insufficient or the target exceeds the coverage budget, the model stops **after the original insertion and before bucket reset**. The linked node remains observable, the final insertion flag stays unknown, and the container is marked incomplete. Subsequent mutations are rejected. This is an explicit coverage boundary, not a claim about native failure or rollback.

Normal allocation/release success and valid native storage remain assumptions of the observable list/bucket/payload equivalence claim. Native address lifetime, CRT internals, OOM, alignment faults, exception propagation and recursive growth are not restored. The existing 65536-node and 4096-bucket budgets remain model restrictions. The coordinator itself did not change; its tests now verify that successful growth in both indices completes before the supplied own Read callback runs outside the gate.

## Validation and checkpoint

The new TaskItems build passed 141 growth assertions, 183 revised container regressions, 47 registration-sequence assertions, 40 ClassFactory assertions, 39 item-condition assertions and 11 item-auto assertions. The existing ItemQuestDemo also passed. A separate Object ID regression run against the new DLL passed 20 assertions: **481 assertions and one existing scenario** in total. The own DLL references only framework and existing GrimDynasty assemblies.

Final per-suite counts, source/build fingerprints, dependencies and restrictions are recorded in [acceptance-registration-growth.json](registration-growth-validation/acceptance-registration-growth.json). [Build output](registration-growth-validation/build-output.txt) and [Object ID regression output](registration-growth-validation/object-id-regression-output.txt) are retained alongside it. The original-binary check is [game-hashes-after.json](registration-growth-validation/game-hashes-after.json).

Before edits, the prior four model/test source files, build script, runner, report and acceptance record were preserved under `.tooling/checkpoints/registration-before-growth/`; `source-manifest.json` records the seven source/report copies and the runner was separately hash-verified before editing. The old evidence batches, build directory and acceptance JSON remain intact. The new successful source checkpoint is described by [checkpoint-manifest.json](registration-growth-validation/checkpoint-manifest.json).

These are targeted tests of independently compiled own code. The historical 894-assertion / 16-scenario full acceptance is not claimed as newly passing. The UCRT fingerprint gate and whitelist remain unchanged.

## Remaining boundaries

The complete item factory still lacks record loading, concrete ClassInfo populations/constructors, name/key derivation, native Read implementations, native allocation/free behavior, recursive maintenance, and integration with the live ObjectIdAllocator index and manager lock. The old allocator and InventoryAdd wiring were not changed.

This continuation did not retry unsupported persistent-host diagnostics. The approved Rea connection was finite and closed after the evidence batch; no native-tool injection or persistent MCP connection is claimed. No additional software, global configuration/trust change, restart, original-game execution, player-save/Steam-cloud access, DRM workaround, commit or push occurred. The requested action-level approval was allowed; no operation is denied or awaiting approval.
