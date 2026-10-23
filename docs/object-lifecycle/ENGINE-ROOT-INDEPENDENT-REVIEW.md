# Independent review: Engine root registration evidence

Date: 2026-10-20.

**Current conclusion:** the saved evidence establishes the on-disk parent chain
`Actor -> Entity -> Object` and a conditional Engine CRT path to two bounded
initializer-range calls. The subsequent table/import evidence and three direct
writers establish conditional reverse append operations. They do not establish
successful startup, complete runtime children/sibling order, or successful
dispatch from Object to Game Item/QuestItem. See the dated addendum below.

This review reads project evidence only. It does not read an original DLL,
execute an analysis script, run a compiler or test, load a target, or start a
backend. The prior PowerShell AuthorizationManager refusal remains in effect;
this review neither retries that build nor substitutes an equivalent execution.
No previous assertion total is presented as a result of this stage.

## Static record identity and parent direction

Sources:

- [Existing Actor/Object records](offline-class-registration-metadata/metadata.json)
- [Entity record](offline-engine-class-parent-record/metadata.json)
- [Entity export binding](offline-engine-class-parent-exports/discovery.json)

All three records belong to the pinned Engine.dll image:

```text
size:   4485632 bytes
SHA256: 4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153
base:   0x180000000 (preferred image base, not a runtime address)
```

| ClassInfo RVA | Exported identity | Stored parent at +0x10 | Stored factory at +0x28 |
|---|---|---|---|
| `41CFC0` | `Actor::classInfo` | `18041CF30` | `180049810` |
| `41CF30` | `Entity::classInfo` | `18041CF00` | `1800499A0` |
| `41CF00` | `Object::classInfo` | `0` | `18004A0D0` |

The Entity binding is exact:
`?classInfo@Entity@GAME@@1VRTTI_ClassInfo@2@B`, name index 6124,
ordinal 6125, EAT index 6124, raw EAT bytes `30 cf 41 00`, non-forwarder.
The export lookup stopped at that match; it did not enumerate all aliases.

The Entity record contains vptr `180348830`, name pointer zero, signed count
zero, adjacent DWORD zero, and children pointer zero. The adjacent DWORD is not
assigned an invented meaning. Actor/Object also have zero name/count/children
in their saved file records. These are file values, not initialized runtime
states. A parent pointer does not prove the reverse children edge, its order,
or its successful publication.

The old Engine export-name observations at adjacent indices 1369 and 1370 are
`??_FResourceLoader@GAME@@QEAAXXZ` and a name beginning
`?AchievementIsUnlocked@`. Under the sorted ASCII export-name-table contract,
every name beginning `??__E?classInfo@Entity@GAME@@` falls strictly between
them. Therefore that named-export prefix is absent under that contract.
This does not exclude an unexported initializer, another naming scheme,
an ordinal-only entry, or initialization code generally.

## Entry and conditional CRT path

The [entry-field report](offline-engine-root-entry/metadata.json) preserves the
four bytes `44 d0 2d 00` at file offset 328: PE entry RVA `2DD044`.
The following saved instructions establish the route; this review does not
execute it.

| Saved fragment | Decisive observation |
|---|---|
| [2DD044](offline-engine-root-static-crt-entry/2dd044-instructions.json) | Reason 1 calls `2DD93C` at `2DD060`, then the entry tail-jumps to `2DCEF8` at `2DD07C`. The helper's behavior is not inferred. |
| [2DCEF8](offline-engine-root-static-crt-dispatch/2dcef8-instructions.json) | For reasons 1/2, `2DCFF0` is called at `2DCF35`; zero EAX skips the later initialization dispatch. On a nonzero result, `2DCCF8` is called at `2DCF50`, and its result is checked too. |
| [2DCCF8](offline-engine-root-static-crt-reason/2dccf8-instructions.json) | The original reason-1 branch reaches `2DCD2D`, moves original R8 to RDX, restores the local stack, and tail-jumps to `2DCD48`. Other reason branches are not treated as registration writers. |
| [2DCD48](offline-engine-root-static-crt-attach/2dcd48-instructions.json) | The two range calls below are conditional on preceding calls and explicit result checks. |

The outer dispatch also calls `2DD9E8` after the initialization dispatch.
For reason 1 and a zero result from that call, it invokes the visible reason-0
paths. Their cleanup effects and exception behavior are not reconstructed here.
Thus reaching a range call is not proof that the whole DLL attach succeeds.

## Attach range arguments and thunks

The attach body has explicit result gates at:

- `2DCD6A`: AL from `2DC68C` must be nonzero to continue.
- `2DCDA5`: AL from `2DC58C` must be nonzero.
- `2DCDE3`: EAX from the first range call must be zero.
- `2DCDEC`: AL from `2DC558` must be nonzero before the second range call.

Other directly called helpers remain unexpanded in this review; their normal
return must not be assumed when claiming whole-attach success.

| Call site | RCX start | RDX end | Saved thunk and IAT slot |
|---|---|---|---|
| `1802DCDDE` | `180303178` | `180303188` | [2DDBB6](offline-engine-root-static-initializer-thunks/2ddbb6-instructions.json): `JMP [180302A50]` |
| `1802DCDFE` | `180302D48` | `180303170` | [2DDBB0](offline-engine-root-static-initializer-thunks/2ddbb0-instructions.json): `JMP [180302A58]` |

The first pair spans 16 bytes; the second spans 1064 bytes (133 qword-sized
positions). Subsequent saved metadata, reviewed in the addendum, binds the
imports and preserves the second table's contents. The separate first table
was not read. Pointer positions alone do not establish execution order or
successful completion. No adjacent table or end-neighbor bytes are needed to
state the observed arguments.

The attach body writes DWORD `[180420D40]=1` at `2DCD96`, before these calls.
After the second call returns, it writes `2` at `2DCE03`; it does not test that
call's return register first. This is observed control flow, not a proof of
successful registration by every possible target. Visible early-failure paths
must not be described as rolling back every earlier write.

## Evidence checks and limits

The five saved inspection reports all state
`fixed-engine-root-fragments-verified`, unchanged pinned original hash/stat,
empty failures, and successful byte-covered fragments. Their reviewed code
extents sum to **697 bytes / 201 instructions / six fragments**. These are
saved static evidence counts, not newly executed operations or tests.

| Report | SHA256 |
|---|---|
| [crt-entry](offline-engine-root-static-crt-entry/inspection.json) | `5ee250e52c8aed2fd40a85900f49100bcd2054b40abf61ee1883a2307ae4b5b4` |
| [crt-dispatch](offline-engine-root-static-crt-dispatch/inspection.json) | `193471ed69fea0b535b78491f1709a1d19a35c0c08d18391742d7a4b468192d9` |
| [crt-reason](offline-engine-root-static-crt-reason/inspection.json) | `dbf9dcfb39929bf2331f07ce34dc10aeb3a87688dd8fc53a59e8fefec1156cc5` |
| [crt-attach](offline-engine-root-static-crt-attach/inspection.json) | `f4b4a7a8d1fb008f1d7f216b4549dff6d6389c9903609c128a7bc62cff9eaea7` |
| [initializer-thunks](offline-engine-root-static-initializer-thunks/inspection.json) | `07efe567de1b0435f0f3a3184745fcb2fd9e8674ae9e129df3c328f5b61b9231` |

This review inspected the saved instruction records and report metadata. It
did not rerun the disassembler or independently reread original machine bytes.
Unwind fragments do not establish exception-path or whole-function completeness.

## 2026-10-20 addendum: table bindings and three direct writers

The saved [table and import report](offline-engine-root-initializer-table/table.json)
has SHA256
`29d059bdb204e1116c6a856537267152d54848c0fd8fc82fefc43914bbf4fa6e`.
Its normal-import records bind IAT `302A50` to `_initterm_e` and `302A58` to
`_initterm` in `api-ms-win-crt-runtime-l1-1-0.dll`. It preserves 1064 bytes at
`[302D48,303170)`: 133 slots, null slot 0 and 132 non-null slots.
The report records unchanged pinned Engine hashes, no DLL load/execution,
and no target-code reads in that metadata operation. The import identity
does not independently prove the CRT implementation or iteration semantics.

The following table indices are **stored pointer positions**, not an observed
runtime callback order. All instruction addresses below are RVAs.

| Record / table slot | Saved prefix | Saved continuations | Visible code |
|---|---|---|---|
| Actor / 23 | [5C70](offline-engine-root-static-initializer-slot23/5c70-instructions.json) | [5C9B](offline-engine-root-static-actor-writer-continuations/5c9b-instructions.json), [5D0F](offline-engine-root-static-actor-writer-continuations/5d0f-instructions.json) | `5C70..5D20`, 176 bytes |
| Entity / 31 | [61F0](offline-engine-root-static-root-field-search-b/61f0-instructions.json) | [621B](offline-engine-root-static-entity-object-writer-continuations/621b-instructions.json), [628F](offline-engine-root-static-entity-object-writer-continuations/628f-instructions.json) | `61F0..62A0`, 176 bytes |
| Object / 55 | [7270](offline-engine-root-static-root-field-search-i/7270-instructions.json) | [729B](offline-engine-root-static-entity-object-writer-continuations/729b-instructions.json), [730F](offline-engine-root-static-entity-object-writer-continuations/730f-instructions.json) | `7270..7320`, 176 bytes |

Each writer consists of saved 43-, 116- and 17-byte unwind fragments. Their
visible normal control flow is now available; exception paths and callees
are not thereby closed.

1. The fixed string address is passed to IAT `302BB8`: Actor `309E64`,
   Entity `309AC4`, Object `309AEC`. These input-string contents remain unread.
2. After the call returns, the writer reads its parent at `+10`, stores RAX
   to its own name at `+08`, and tests the loaded parent for null. There is
   no visible name-result null check. Name stores are `5C8A -> 41CFC8`,
   `620A -> 41CF38` and `728A -> 41CF08`.
3. A non-null parent supplies its DWORD count. The increment wraps in 32
   bits before sign extension; unsigned multiplication by 8 is saturated
   to all-ones on multiplication overflow. `2DC900` receives that request.
   Its returned pointer is not visibly checked for null.
4. The copy loop rereads the selected parent's count and storage, copying
   entries in order. It does not reload the writer's parent selector.
   The writer appends its own fixed record address using the current count.
5. The count is incremented, old storage is passed to `2DC8F8`, and only
   after that call returns is the new storage published at parent `+20`.
6. The tail call to `2DC8E0` receives Actor callback `300F90`, Entity
   `301110` or Object `301590`; those callback bodies remain unread.

| Writer | Append self | Increment parent count | Old-storage helper | Publish parent storage | Tail jump |
|---|---|---|---|---|---|
| Actor | `5CF6` | `5CFA` | `5D01` | `5D06` | `5D1B` |
| Entity | `6276` | `627A` | `6281` | `6286` | `629B` |
| Object | `72F6` | `72FA` | `7301` | `7306` | `731B` |

Under unchanged saved parent values and normal completion of the required
calls/accesses, these writes support `Entity.children += Actor` and
`Object.children += Entity`. With Object's parent still zero, its writer
branches at `7294` to `730F` after the name store. It does **not** clear its
own children/count. Entity's writer likewise does not reset its own children
while appending Entity to Object. None of these writers assigns its parent,
factory, or vptr, and no duplicate check is present. Invoking a non-null-parent
writer again would attempt another append; native idempotence is not established.

Partial states matter: name publication precedes the allocation request;
copying may have written part of the new span; the append and count increment
precede the old-storage helper; the tail call follows storage publication.
Failure must not be described as an atomic rollback. Engine `302BB8`,
`2DC900`, `2DC8F8` and `2DC8E0` still lack independently reviewed Engine
contracts here. Similar Game code does not close those gaps.

The writer search used the first 43-byte unwind fragments at slots 23..56:
34 fragments, with continuations followed only for these three records.
The other 31 prefixes do not establish their full behavior. The retained
export-order and record-distance candidate assumptions were disproved;
neither is an ordering rule. The accepted writers are identified by actual
name-store and append-record operands.

The [stage result](ENGINE-ROOT-WRITERS-RESULT.md) reports 19 bounded batches,
2558 code bytes, 640 instructions and 46 dumpbin calls, plus 5506 semantic
metadata bytes (including 4128 exception-table bytes). The earlier 697-byte
CRT subset above is included in this total. This addendum reads saved outputs
only; it does not rerun any of those operations or claim a new product test.

Reviewed writer-report SHA256 values:

| Report | SHA256 |
|---|---|
| [Actor prefix](offline-engine-root-static-initializer-slot23/inspection.json) | `ce14b4749b43cef1fe407b7f3e2b23a5eb1c5c333f362af782b4c41d0b503297` |
| [Actor continuations](offline-engine-root-static-actor-writer-continuations/inspection.json) | `0775d4e9376ebf8bf385679c7427671488fb832bd60e49fbde8d39ff41b8068b` |
| [Entity prefix batch](offline-engine-root-static-root-field-search-b/inspection.json) | `6292214a0ea0dfa246602a5688300280123e577f70664bc7a9f1efde8734d96f` |
| [Object prefix batch](offline-engine-root-static-root-field-search-i/inspection.json) | `a0b54f850753c24121ef48db0294bd9be931ac78872a3310f38f2b77f4d87760` |
| [Entity/Object continuations](offline-engine-root-static-entity-object-writer-continuations/inspection.json) | `86bc8f1a4d0b26d74082ddf68bddf86f114440dd5fc5ff2add8b24ffb8be104e` |

Remaining boundaries are successful CRT startup, helper/exception contracts,
runtime parent stability, other writers and complete sibling contents/order.
Existing Game Item/QuestItem writers are separate conditional evidence.
Even a root-to-Item route does not prove `Class="QuestItem"` dispatch: the
known dispatcher searches self first, then ordered children, with continuation
depending on callback results. Earlier reachable nodes cannot be silently
skipped. No complete runtime registry, native constructor/Read implementation,
or compatibility result is established by this review.
