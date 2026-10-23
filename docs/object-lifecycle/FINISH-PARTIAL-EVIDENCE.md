# FinishPartial: caller boundary and bounded composition

Date: 2026-10-19. This document reviews already saved static evidence for the bounded `FinishPartialObject` wrapper over the existing record-cache loader and loaded-object registration model. Implementation acceptance is recorded separately from these evidence facts. No new Rea/Ghidra session, original-binary read, or original-program execution was performed for this review.

## Fixed artifact and evidence sources

All native addresses below refer to the same pinned x64 Engine artifact:

```text
D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll
SHA256 4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153
Function 0x18017E230
GAME::ObjectManager::FinishCreateObjectFromFilePartial(Object*, unsigned int)
```

| Source | Exact evidence identity and use |
| --- | --- |
| [Current-host complete caller assembly](rea-native-registration/05-ObjectManager-FinishPartial-order-assembly.json) | `ev_866e78d4d3d3d3a12206e39924ca20389433f0c4fc86690cc3209edbf74e973b`; establishes guards, byte reads, temporary lifetime, branches, and registration/virtual-call order |
| [Current-host direct callees](rea-native-registration/07-ObjectManager-FinishPartial-direct-callees.json) | `ev_6f00c4a170285042c7d7ba0c7b177767ad59d5373ef0e00869bdb72bb9818476`; includes `022CE0`, `023090`, `1C4D20`, `17FCA0`, `1356F0` and two lock imports; indirect diagnostic/Read targets remain unresolved |
| [Preserved same-hash caller pseudocode](../rewards/rea-object-ids/ObjectManager-FinishPartial.json) | `ev_12fd3d414d2756fc7d41d2af9963890b45a7f5bfbe05015dce56248d0e5b6ed2`; same Engine hash and exact function address; identifies the exported void signature, `TableDepot::LoadFile`, and diagnostic literal |
| [Counted-copy assembly](rea-record-getter-support/08-CString-copy-0x180022ce0-assembly.json) | `ev_11c9b1ee555aca0e8cce63184b7c54b2ead44ad1abfcc56803cecea33208c5c7`; fresh destination path for the already scanned C-string prefix |
| [Counted-copy pseudocode](rea-record-getter-support/07-CString-copy-0x180022ce0-pseudocode.json) | `ev_c5ca032727868d44251f418b7c8d7d1c4b43641998fe4d4ed2ee0e7e57632b2d`; normal copy/growth, alias and error branches |
| [Release assembly](rea-container-reserve/08-Container-reserve-0x180023090-assembly.json) and [pseudocode](rea-container-reserve/07-Container-reserve-0x180023090-pseudocode.json) | Release pseudocode `ev_a781171a7704a44b5e5c902ab1d448f9a728a2b226add902f1d4839ec3128d93`; conditional normal release and valid large-block metadata |

The artifacts identify Ghidra 12.1.4 and carry their own subject hash/provider/limitations in `result.structuredContent`. They are static analysis results, not original source. The prior [caller batch](rea-native-registration/summary.json), [copy-support batch](rea-record-getter-support/summary.json), and [reserve/release batch](rea-container-reserve/summary.json) already completed and closed. This phase reuses those results and does not add a query plan or backend call.

## Guards precede all object-name and manager reads

| Address | Observed control flow |
| --- | --- |
| `0x18017E230` / `0x18017E233` | Test object argument `RDX`; null jumps directly to `RET` at `0x18017E3C9` |
| `0x18017E261` / `0x18017E264` | For a nonnull object, test the supplied ID's low 32 bits; zero jumps to the ordinary epilogue at `0x18017E3B5` |
| `0x18017E26A` | Only after both guards, form object-name string address `object + 0x08` |

Neither native guard reads the object's name, stored registration keys, manager state, cache, or callback target. A model must preserve this short circuit: a known null object can return before needing an ID, and a nonnull object with a known zero ID can return before needing its name/depot/registration state. An unknown ID is not a known zero. The native entry is void; a model's `Known` result describes coverage/completion, not an invented native return value.

## Name prefix, temporary string, and LoadFile order

The nonnull/nonzero path chooses the object's logical name storage by capacity. At `0x18017E26E`, the code compares `object + 0x20` against 16. Capacity below 16 selects inline storage at `object + 0x08`; otherwise `0x18017E275` loads its stored buffer pointer.

| Order | Address | Observed operation |
| --- | --- | --- |
| 1 | `0x18017E27D`–`0x18017E28F` | Initialize a fresh local string with capacity 15, stored length zero, and first byte NUL |
| 2 | `0x18017E294`–`0x18017E2AA` | Read name bytes until the first NUL; an initial NUL selects count zero |
| 3 | `0x18017E2B1` | Call `0x180022CE0(local, nameStorage, scannedPrefixLength)` |
| 4 | `0x18017E2B7`–`0x18017E2C3` | Call `0x1801C4D20(manager + 0x170, localString)` |
| 5 | `0x18017E2C8` | Save the returned table pointer in `R14` |
| 6 | `0x18017E2CB`–`0x18017E2E4` | Read the local capacity; if it is at least 16, call `0x180023090(localBuffer, capacity + 1, 1)` |
| 7 | `0x18017E2E9`–`0x18017E2FB` | Reset local capacity to 15, length to zero, and first byte to NUL |
| 8 | `0x18017E300` / `0x18017E303` | Only now test whether the saved table is null and select diagnostic versus registration |

The scan does **not** read the object's stored name length. For valid string storage, it ends at the first interior NUL or at the known terminator at the end of the payload. The entire name need not be empty for the scanned prefix to be empty. Unknown or unreadable required bytes, a missing terminator, invalid native storage, and a prefix outside the declared budget are outside the supported model domain; a missing terminator cannot be invented.

The wrapper must pass the temporary prefix to `RecordLoadFile`, not pass the original full name directly. The loader then performs its existing full stored-length normalization and cache lookup on that **new prefix string**. Its normal cache-hit contract still requires a complete stable cache, valid bucket metadata, and known C locale for any reached `tolower` operation. An empty prefix or a prefix consisting entirely of backslashes does not reach that import. High-bit bytes or unknown data solely after the first NUL must not be used to reject the prefix's ASCII/locale domain.

This differs from CreatePartial: that caller passes its original full stored-length record-name string to LoadFile, then sets the resulting object's name from the original name. With an interior NUL, CreatePartial and FinishPartial may therefore query different complete cache keys and return different tables. A model must not force them to share the first call's result.

## Fresh-copy and release normal-success contracts

The local destination begins with length zero and capacity 15. In `0x180022CE0`, the existing-content alias interval is therefore empty, so the generic overlap transfer to `0x180023180` is not needed on this valid fresh-destination path. Within the existing bounded domain:

- Count zero preserves an empty local string with its terminator.
- Count 1 through 15 copies exactly the prefix into inline storage, writes the count and terminator, and retains capacity 15.
- Larger covered counts call the previously observed growth helper `0x180022F70`; from initial capacity 15, normal capacity becomes `prefixLength | 15`. The final copy sets the exact prefix length and terminator.
- The FinishPartial caller later releases only the temporary heap buffer, with that temporary capacity plus one and element size one. It does not free or shorten the object's original name.

The [name/growth evidence](OBJECT-NAME-EVIDENCE.md) describes normal `022F70` growth and the [verified memcpy import](name-interface-discovery/name-copy-thunk-0x1802ddb0e.json). The release helper `023090` checks capacity-size arithmetic and, for blocks of at least 4096 bytes, the 32-byte alignment and hidden allocation base's 8..39-byte displacement before transferring to `0x1802DC390`. The internal allocation/free implementation and native failure paths remain outside this review.

Successful allocation, copy, and release of valid owned storage are explicit contracts shared with the existing modules. Own managed temporary lifetime/cleanup is not a claim about native allocation identity, allocator hooks, OOM, invalid-parameter handlers, exception unwinding, or rollback. On a normal returned LoadFile result, the observed temporary release/reset precedes the table test and therefore precedes ID assignment, registration, or diagnostic dispatch. Cleanup after an own Unsupported result must be labelled an own policy, not a proven native exception path.

## Null table always reaches the unresolved diagnostic

At `0x18017E303`, a null table branches to `0x18017E38F`. This branch selects the **original object's name storage again** using its current capacity, loads `gEngine`, and calls engine vtable `+0x18` at `0x18017E3B2`. There is no null/nonempty-name guard comparable to CreatePartial's class-text diagnostic check. Even a zero-length name prefix reaches this diagnostic if the returned table is null.

The preserved same-hash pseudocode identifies the format as `Unable to finish object creation (%s).`; the current assembly places its address `0x18031D090` in `R8`, severity value 1 in `EDX`, and the original name-storage pointer in `R9`. Literal identity here comes from the already saved pseudocode; this review did not read new original bytes.

The caller's null-table branch bypasses the ID write at `0x18017E309` and all displayed registration calls. The diagnostic body is unknown and may have its own effects; this does not prove that arbitrary native state remains unchanged after executing that unknown call. The own wrapper should instead stop at an explicit diagnostic Unsupported boundary, without invoking registration or inventing diagnostic completion.

A known null payload can be a successful cache hit in the existing loader. Its hit-count increment has already happened before the wrapper reaches this diagnostic boundary and must remain recorded. Unknown LoadFile results or a cache miss are different: they stop within the loader's unresolved domain and must not be relabelled as a known null-table diagnostic result.

## Nonnull table hands off to the existing registration sequence

Only a normally returned, known nonnull table reaches the sequence already covered by [LoadedObjectRegistration](../../src/task_items/LoadedObjectRegistration.cs):

| Order | Address | Observed effect |
| --- | --- | --- |
| 1 | `0x18017E309` | Write supplied ID to object `+0x2C` |
| 2 | `0x18017E310` | Enter manager critical section from `+0x28` |
| 3 | `0x18017E325` | Erase the ID-selected range from manager `+0x128` through `0x18017FCA0` |
| 4 | `0x18017E34A` | Insert `{ID, object}` into manager `+0x30` through `0x1801356F0` |
| 5 | `0x18017E34F` | Read current raw 32-bit key from object `+0x28` |
| 6 | `0x18017E372` | Insert `{RawKey28, object}` into manager `+0x70` through the same index helper |
| 7 | `0x18017E37B` | Leave the manager critical section |
| 8 | `0x18017E38A` | Call the object's vtable `+0x08`, passing the exact loaded table |

The supplied ID is written before the manager gate is acquired. The caller ignores duplicate-insertion flags, and reads `RawKey28` after the first index insertion; the existing registration model already preserves that order and completed side-effect prefixes. The wrapper must not preflight all downstream registration inputs before the loader and thereby remove effects that occur earlier in the native sequence.

There is **no SetObjectName call and no RawKey recomputation** in FinishPartial. In particular, a name containing an interior NUL may have a RawKey previously computed over its complete stored length, including the suffix, while FinishPartial loads only the C-string prefix. Registration must consume the current original `RawKey28`, not a hash of the prefix or normalized cache key. Object/name/registration-field identities remain the same; only the temporary loading string is new.

This path does not query `Class`, rerun the class factory, or call a constructor. It therefore need not require the loaded payload to be a typed Class-getter snapshot merely to reach the existing explicit own Read contract. A known nonnull table identity must still be preserved and passed unchanged to that contract.

The actual derived object's `+0x08` target and record-reading effects are not recovered. The existing `Action<RegistrationObject, object>` is an own callback interface, not a native ABI or restored Read. Callback failure/Unknown after registration cannot erase the preceding ID/index effects. The [registration container](REGISTRATION-CONTAINER-EVIDENCE.md) and [growth](REGISTRATION-GROWTH-EVIDENCE.md) coverage limits remain in force.

## Supported composition and remaining boundaries

The already observed caller and helpers are sufficient for a bounded wrapper with this order:

```text
object/id guards
  -> select stable valid name storage and scan first NUL
  -> construct a fresh prefix string
  -> existing bounded RecordLoadFile
  -> release/reset the temporary under the normal-success contract
  -> known null table: stop at unresolved finish diagnostic
  -> known nonnull table: existing LoadedObjectRegistration
       -> explicit own Read callback after unlock
```

The covered domain must explicitly supply stable valid own object/name state, readable terminated bytes for the reached prefix, normal string allocation/copy/release, the existing complete cache and locale conditions, and the existing registration/index/gate/callback contracts when those later stages are reached. Unsupported/unknown state is distinct from a known null, zero ID, empty prefix, duplicate insertion, or missing cache key. Prior loader counts, registrations, and callback effects remain visible if a later stage is unsupported.

This wrapper does not close cold loading/record parsing, native manager initialization, native lock identity/concurrency, ClassInfo population, derived construction, the finish diagnostic, actual Read, native exceptions, or full ObjectIdAllocator/InventoryAdd lifecycle integration. Its finite budgets and managed cleanup are explicit own coverage limits. The original game remains static-only; no saves or Steam cloud data are involved.

The implementation is [FinishPartialObject.cs](../../src/task_items/FinishPartialObject.cs). Acceptance is governed by [FINISH-PARTIAL-RESULT.md](FINISH-PARTIAL-RESULT.md) and [finish-partial-validation/acceptance-finish-partial.json](finish-partial-validation/acceptance-finish-partial.json). This document records evidence rather than a separate test count; it changes no historical evidence, source file, runner, backend configuration, or acceptance record, and requests no new tool installation or backend execution.
