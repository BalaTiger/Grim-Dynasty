# Item GameEngine float source integration

2026-10-21. **Paused at the user's request for a progress/cost review. No build or test ran this round; no new build approval was submitted.** All static readers finished and closed their original-file handles. Two own source files, a fixture and a build-script draft are saved. The build output directory does not exist. Do not resume execution or extend the call chain until the parent conversation reviews the audit.

## Result and source chain

The two GameEngine DWORDs used by Item are now traced to the primary `records/game/gameengine.dbr` record's `itemInfoRecord` string and the selected secondary table's `itemSparkleMinTime` and `itemSparkleMaxTime`. Game2CEC44 writes+1B58 after the first getter;2CEC65 writes+1B5C after the second. Explicit supplied defaults are binary32 bits40400000 and40A00000 (3 and5). Constructor initial writes are not assumed to be final loaded values.

The Binary path is independently bound: outer vptr+48 ->13FD50 -> reread vptr+98 ->1402E0 ->283840 name lookup ->140420 numerical lookup. Type1 selects raw floating-value DWORDs, with unsigned element-index clamping and32-bit index addition. There is no numeric conversion or arithmetic in140420. Miss/wrong-type selects the default; unreadable or unknown storage does not. The complete normal reachable CFG fits the112-byte bounded entry window, without asserting an exact .pdata function boundary.

GetLoadTable's1C4C30 helper returns depot+48 on a sentinel or null cached payload; it does not call a loader or increment the LoadFile hit counter. This establishes fallback selection, not the fallback object's construction, type or lifetime.

See [native evidence](ITEM-GLOBAL-LOAD-EVIDENCE.md) and the independently reviewed captures under `offline-item-global-load-*`, `offline-item-global-float-value` and `item-global-load-validation`.

## Own implementation

- `src/task_items/BinaryFloatTableGetter.cs` reuses the same name-cache nodes, shared pool, name-ID index and field lookup primitives. It preserves cold/stale/growth effects before later numeric stops. The new tag1 view requires the actual fresh parsed record's identical Gate and Fields, recovers the last duplicate descriptor, and copies raw DWORDs. Existing string APIs and production sources are unchanged.
- `src/task_items/ItemGameEngineFloatLoad.cs` starts only at the local two-call window. Its ordered outer/inner dispatch steps expose actual vptr rereads and inter-call changes. It writes the same `ItemGameEngineFields` object subsequently borrowed by the existing Item return chain, with precise4-byte stores and no post-copy synchronization. It does not fabricate a successful earlier LoadFromDatabase prefix or publish a global object.
- Known/unknown/null/invalid observations and unsupported dynamic targets remain distinct. First-store and name-cache effects survive a later stop. The following2CEC81 write to+1B60 is outside this window; its four bytes remain untouched here only.

Source review does not establish compilation, formal sample compatibility, native ABI, exception cleanup or concurrent behavior.

## Validation receipt

Not executed: `ItemGameEngineFloatLoadTests.cs` (author's static estimate51 assertions/10 groups, **not a passed test count**) and the affected prior ItemConstructorReturn suite (historical59/10). The prior ItemName76/9 and other suites were not rerun. The script draft `src/task_items/build_item_game_engine_float_load.ps1` proposes serial3 compiler invocations and2 own EXEs; its final execution review and separate formal approval remain undone. The earlier denied class-registration script was not rerun or replaced.

Frozen inputs at pause:

| File | Bytes | SHA256 |
| --- | ---: | --- |
| BinaryFloatTableGetter.cs | 15663 | `2289b851867fd604b2d1c35aa4676f2f41bb4c7148bbc5274a339a1192330aa7` |
| ItemGameEngineFloatLoad.cs | 16574 | `db5753b07e35a128632252263bc450601c66d8e9142dce8bb02ffdd6c5e0ffa0` |
| ItemGameEngineFloatLoadTests.cs | 29428 | `bf3399190e13ad8f5fefb852f43ffc81c5de4e453c0cc4318053e695af2bdf9e` |
| build_item_game_engine_float_load.ps1 | 9861 | `932ed56042535138ff68c49bcd56c1b6912a857bc4997350228a52edfef88a9f` |

The production source and initial fixture text were reviewed; the final fixture additions and build draft are preserved without claiming completed execution review. Independent final source review is recorded in `model-review.md` and `window-review.md`.

## Remaining usable milestone and boundaries

The shortest proposed usable milestone is one reviewed offline demonstration: a supplied own secondary binary record produces both values in the shared GameEngine view and feeds the existing conditional Item return, without hand-supplying those two scalar values. First review and run the already saved targeted fixture exactly once, then package only that finite demonstration if requested. This is a local fixture milestone, not completion of all1904 calls in LoadFromDatabase. It is a recommendation requiring parent review, not authorization to continue.

Remaining native conditions include actual archive fetching and record selection/values, Singleton identity, depot fallback lifetime/dynamic type, Base textual-float parser4DD80 and format316034, gEngine publication and later writes, allocator/thread/EH behavior. Existing Item RNG state, FP environment and exceptional arithmetic, string alias/growth/cleanup and complete QuestItem construction remain unclosed. No actual gameplay or formal sample has been run.

Original Game/Engine modules remain read-only with matching pinned before/after hashes in every static reader. No game, Rea, Java, saves/cloud, install, security/ACL/policy change, commit or push. The existing12 tracked changes (+60/-46) and HEAD6dfdae419d1c92f7738eefe241937057d83134df remain unchanged.

Recovery checkpoints: prior565-file `.tooling/checkpoints/item-constructor-return-complete/manifest.json` and this round's15-file `.tooling/checkpoints/item-global-load-writers-static/manifest.json`. Later saved evidence is indexed by `item-global-load-validation/static-inventory.json`; the pause manifest records current live hashes rather than copying another full cumulative checkpoint. No files or old checkpoints were deleted.

## Progress and cost audit

The existing project contains10 source areas for resource/quest decoding, conditions, tokens/tasks, rewards and item/object lifecycle. Previously compiled own modules and fixtures provide useful offline behavior checks. The previously tested ItemReturn integration reaches conditional own Item return, with external state observations and uncovered branches; this round's new float-loading connection remains uncompiled. The project does not yet provide a playable independent game, complete native Item factory, complete QuestItem behavior or established formal-sample compatibility. No defensible percentage-to-completion can be inferred from assertion counts.

The metadata-only census in `item-global-load-validation/safe-stop-size-audit.json` was taken before writing this audit's final text. Decimal MB below describe files, not model tokens:

| Current area | Files | Bytes |
| --- | ---: | ---: |
| src (all files) | 341 | 2622622 |
| C# within src | 234 | 2048308 |
| docs (reports plus static evidence) | 3719 | 75471563 |
| tools | 286 | 2415338 |
| checkpoints,100 directories | 12912 | 207974305 |

The234 C# files comprise90 filenames ending Test/Tests.cs (1272132 bytes) and144 other C# files (776176 bytes). Relative to the current Git HEAD, untracked src/docs/tools totals are134/2994/202 files and1718239/36089599/2154902 bytes. These cumulative working-tree counts are **not** all authored in this round and cannot be attributed to one agent/turn.

Repeated checkpoint copies are real: the BE0, Name and Return complete manifests hold461/530/565 selected files (5.80/6.87/7.63 MB payload). Each contains the same5502-byte ActorConstructorPrefix.cs with SHA256 `c51710c512abe864d16feecd7972a779804d9d0cdc419b117e86ba7e37134b83`. This proves repeated copies, not that all208 MB is duplicate content. This round created only the15-file writer checkpoint (2651822 payload bytes), referenced the prior565-file checkpoint, and did not recopy it. The current safe stop adds a hash manifest, not another payload snapshot.

This round added32237 bytes of production C#,29428 bytes of unexecuted fixture,9861 bytes of unexecuted build script, seven bounded-reader scripts, and evidence/reviews. Static inventory covers63 selected files/2994609 bytes at its timestamp, including8 synthetic PEs; semantic original reads total50218 bytes with no repeated semantic ranges,8 dumpbin calls and16 streaming hash passes. None of those synthetic PEs or original DLLs was executed.

No tests were repeated this round. Earlier progress records do show affected-suite reruns across adjacent integration stages; that is regression work, but its token cost cannot be quantified here. Parallel evidence/model/fixture work and independent reviews also caused repeated reading and reasoning over some of the same material. Many small continuation stages, extensive reports and cumulative checkpoints create real coordination overhead even where their evidence is useful.

Recommendation: **pause and adjust the delivery plan**, rather than immediately pursue another native helper. After parent review, cap the next action at one reviewed small build and its two selected own suites, then deliver the offline demonstration or stop. Keep unresolved native providers explicit; use incremental manifests, avoid repeated complete payload copies and require a user-visible milestone before another dependency investigation. No automatic continuation is scheduled.

Available tool results do not expose this task's actual token ledger or billed amount. File size, tool count and assertion count cannot be converted into an honest token/currency estimate.
