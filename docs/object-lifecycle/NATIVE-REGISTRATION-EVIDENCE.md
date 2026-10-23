# Native creation control flow and class-factory dispatch evidence

Date: 2026-10-19. This analysis covers the class-factory helper and its CreatePartial/FinishPartial callers. Container and index helper internals are reviewed separately; their calls are identified here without inventing their semantics.

## Evidence identity and completed batch

The bounded batch in `rea-native-registration/summary.json` reports 18 of 18 plan calls passed, successful close acknowledgement, a final closed session, and confirmed process exit. Engine SHA256 before and after was identical:

```text
4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153
```

Target: `D:\Steam\steamapps\common\Grim Dawn\x64\Engine.dll`, PE x86-64. Analysis provider: Ghidra 12.1.4 through Rea 6.5.0. These are static observations, not execution of the target or reconstruction of original source text.

The analysis reads the already-recorded results below. It does not issue another backend query.

| Result file under `rea-native-registration/` | Evidence ID |
| --- | --- |
| `04-ObjectManager-CreatePartial-order-assembly.json` | `ev_5685a6f351382b23dc14bd60345168e3962bab9b962c92becd0bc5a90bab521d` |
| `05-ObjectManager-FinishPartial-order-assembly.json` | `ev_866e78d4d3d3d3a12206e39924ca20389433f0c4fc86690cc3209edbf74e973b` |
| `06-ObjectManager-CreatePartial-direct-callees.json` | `ev_dd73b5930a0780100ed97cbe134a830ce82671b4630cd050e1f7822fbf195fe3` |
| `07-ObjectManager-FinishPartial-direct-callees.json` | `ev_6f00c4a170285042c7d7ba0c7b177767ad59d5373ef0e00869bdb72bb9818476` |
| `08-Class-factory-helper-0x180274ec0-pseudocode.json` | `ev_805e96c1314cc529195bd2de8f5fed58a5e22cc801015a8a910e103aef5b803c` |
| `09-Class-factory-helper-0x180274ec0-assembly.json` | `ev_9a2d2b7151f8d4fa70667f4f982d4dc9e3bca50cfa6168989daab735c2724b53` |
| `10-Class-factory-helper-0x180274ec0-direct-callees.json` | `ev_45d1337d5776aff223f93171ecab854f2e4b853ad762ec78078b87b49e8034d3` |

For reproducibility, the payload is in each file's `result.structuredContent.normalized_result`; `parameters.procedure`, `subject.digest.sha256`, `provider`, and `limitations` identify its scope. The original bounded request sequence is `tools/static_analysis/rea-native-registration-plan.json`.

## Class-factory helper: control flow is sufficiently observed

Function: Engine `0x180274EC0`. The pseudocode and assembly agree on the following fields and behavior. Field names below describe observed use, not recovered declarations.

| Metadata offset | Observed use |
| --- | --- |
| `+0x08` | Pointer to a NUL-terminated name byte sequence |
| `+0x18` | Signed 32-bit child count |
| `+0x20` | Pointer to an array of child metadata pointers, eight bytes per entry |
| `+0x28` | Factory function pointer used when the current name matches |

1. Compare the query bytes against the current metadata name bytes. `MOVZX` loads individual bytes at `0x180274EE1` and `0x180274EE5`; subtraction and a zero test implement exact equality. There is no observed case conversion, slash normalization, locale operation, or character decoding.
2. Equal bytes are consumed until the matched byte is NUL. Bytes beyond the first NUL do not participate. An empty query is not rejected in advance; it can match an empty metadata name.
3. On a current-node match, load the pointer at metadata `+0x28` (`0x180274EFC`), restore this function's frame, and **tail jump** through that pointer at `0x180274F0F`.
4. On a mismatch, compare the signed child count to zero (`0x180274F12` / `JLE`). A count of zero or less returns zero without reading a child-array element.
5. A positive count enters array-order recursion. Each iteration loads the child array pointer and the current child pointer, passes the original query, then recursively calls `0x180274EC0` at `0x180274F2B`.
6. A nonzero recursive result returns immediately (`TEST RAX` / `JNZ` at `0x180274F30`–`0x180274F33`). A zero result advances to the next child. Exhausting the children returns zero.

The important null-result boundary follows from the tail jump: **when the current node's name matches, its factory's result is returned directly, including zero. The matching node's own children are not searched after a zero factory result.** If this node was reached recursively, its parent receives zero and continues with the next sibling.

The factory pointer at metadata `+0x28` is not an object-vtable slot. The decompiler explicitly warns that it could not recover the indirect jump and treated it as a call. The assembly establishes the tail transfer. It does not establish the concrete callback target, callback ABI signature, allocation behavior, or constructor implementation. In particular, the residual argument registers at the tail jump must not be treated as a recovered semantic callback interface.

The callee inventory contains only `0x180274EC0` itself. Its relationship coverage is labelled complete for resolved Ghidra call references, with an explicit warning that computed or indirect targets remain unknown. It therefore does **not** close the `+0x28` factory target.

## Safe scope of the next independent primitive

The observed control flow is sufficient to implement an independent `ClassFactoryDispatch` primitive over an explicitly supplied, stable metadata graph and explicit callback contracts. This restores class-tree matching and callback dispatch within that declared domain. It does not restore a concrete native constructor, the game's live class registry, native object registration, or the full Item factory.

The planned primitive will not connect to `InventoryAdd` or claim that supplied fixture descriptors are native objects. Unverified metadata or callback behavior remains `Unsupported`, distinct from a known callback returning null.

Required boundaries for implementation and tests:

- Preserve raw byte comparison, including bytes above `0x7F`, case differences, empty names, and ignored suffix bytes after NUL. A missing byte needed by the comparison is `Unsupported`; a prior known mismatch needs no later byte or terminator. Do not append a guessed terminator.
- Preserve self priority, child-array order, first nonnull result, and the matched-node/null-result rule described above.
- Require only metadata reached by the selected control flow. A self match does not read children. A self mismatch does not use that node's factory pointer. A nonpositive signed child count does not read its child array.
- Do not equate a missing/zero factory pointer with a valid callback that returns null. The native code has no protective null check before its indirect jump.
- Shared child nodes may appear at several array positions. The native traversal revisits them and may invoke their callbacks repeatedly. A global visited set or memoized callback result would change behavior. Any cycle guard must apply to the active recursion path, not suppress valid shared-node revisits.
- Declare a stable finite graph and stable name bytes as preconditions. This helper contains no lock, cycle detector, or depth budget. A reconstruction guard that rejects cycles, instability, or excessive traversal is an explicit covered-domain limit, not an observed native behavior.
- Callback effects are not rolled back. A known callback may run and return null before traversal reaches unknown metadata or an unknown callback. Returning `Unsupported` later must not be described as proof that the entire operation made no changes. Previously executed callback traces/effects must remain visible.
- A pure search for the first matching class name is insufficient in the general case: null callback results affect which later siblings are searched. Callback outcomes cannot be guessed or silently treated as absent nodes.

## CreatePartial: confirmed calls and branches

Function: Engine `0x18017E170`. Register flow and historical same-hash pseudocode together identify this sequence:

| Call site | Confirmed operation |
| --- | --- |
| `0x18017E180` | Direct call `0x1801C4D20`, with manager `+0x170` and the input record name; historical symbol: `TableDepot::LoadFile` |
| `0x18017E18B` | Null loaded-table result branches directly to return |
| `0x18017E1AC` | Virtual call through the loaded table's vtable `+0x28`; historical pseudocode supplies `"Class"` and `""` |
| `0x18017E1B2` | Load root metadata address `0x18041CF00`; historical pseudocode names it `Object::classInfo` |
| `0x18017E1BC` | Direct call `0x180274EC0` with root metadata and returned class-name pointer |
| `0x18017E1CF` | On nonnull factory result, direct call `0x18003A780` with that object and original record name; historical symbol: `Object::SetObjectName` |
| `0x18017E20A` | On null factory result and a nonnull, nonempty class name, call the engine's vtable `+0x18` diagnostic slot |

The class-name result is passed into the factory helper before the caller's later null/nonempty diagnostic checks. Those later checks are not a guard making a null class-name pointer safe inside the helper. The loaded table's concrete getter target and return guarantees remain unresolved.

This function does not call `CreateObjectID`, the two registration helpers, or the object's final table-reading virtual slot in the displayed body. Creating a partial object cannot be equated with finishing registration.

The direct-callee result lists `0x18003A780`, `0x1801C4D20`, and `0x180274EC0`. Coverage is **partial**, explicitly missing two indirect call targets: the table getter and diagnostic dispatch.

## FinishPartial: confirmed successful-path ordering

Function: Engine `0x18017E230`.

- A null object returns immediately at `0x18017E230`–`0x18017E233`.
- A zero low-32-bit ID returns through the epilogue at `0x18017E261`–`0x18017E264`. Neither guard path calls external routines or mutates the modeled object/manager state.
- Otherwise, the name storage begins at object `+0x08`. The code chooses inline or pointer storage using the capacity field at object `+0x20`, scans until NUL, and constructs a temporary name via `0x180022CE0`.
- It loads the table through `0x1801C4D20` at `0x18017E2C3`, releases temporary heap storage through `0x180023090` when necessary, and then checks the loaded table.
- A null table calls the engine's diagnostic vtable slot `+0x18` at `0x18017E3B2`. It does not enter the success sequence below.

The successful sequence is established by these instruction addresses:

| Order | Address | Observed effect or call |
| --- | --- | --- |
| 1 | `0x18017E309` | Write the supplied ID to object `+0x2C` |
| 2 | `0x18017E310` | Indirect import call via `0x180302328`, with manager critical-section pointer at `+0x28`; historical same-hash pseudocode identifies `EnterCriticalSection` |
| 3 | `0x18017E325` | Call `0x18017FCA0(manager + 0x128, &ID)` |
| 4 | `0x18017E34A` | Call `0x1801356F0(manager + 0x30, result-storage, {ID, object}, extra-byte)` |
| 5 | `0x18017E34F` | Read the second key as a 32-bit value from object `+0x28` |
| 6 | `0x18017E372` | Call `0x1801356F0(manager + 0x70, result-storage, {second-key, object}, extra-byte)` |
| 7 | `0x18017E37B` | Indirect import call via `0x180302330`; historical same-hash pseudocode identifies `LeaveCriticalSection` |
| 8 | `0x18017E38A` | Object virtual call through vtable `+0x08`, with the loaded table as the second argument |

The object ID write occurs **before** acquiring the manager lock. The final virtual call occurs **after** releasing it. The caller displays no conditional branch on the two index-helper return values and does not read their local result-storage between those calls and the final dispatch. This establishes caller ordering, not helper internals or exception/rollback semantics.

The second index key is the raw 32-bit field at object `+0x28`, not a name string inferred by the reconstruction. Both index calls load an extra byte from caller stack storage into `R9D`; its necessity and meaning must come from the helper analysis, not a guess at this caller.

The final object slot is called `Read` in the prior handoff. This new assembly directly proves `object-vtable +0x08` dispatch and its ordering; it does not identify the selected concrete class's `Read` target or its effects.

The direct-callee result includes `EXTERNAL:0x76`, `EXTERNAL:0x77`, `0x180022CE0`, `0x180023090`, `0x1801356F0`, `0x18017FCA0`, and `0x1801C4D20`. Coverage is **partial** and explicitly omits two indirect targets. The caller's object slot `+0x08` and diagnostic slot `+0x18` must remain unresolved rather than treated as no-ops.

## Independently reviewed registration-helper evidence

`0x1801356F0` compares raw uint keys after hashing their four bytes. Its duplicate path at `0x180135790`–`0x1801357AA` returns the existing node and writes a zero insertion flag at result-storage `+0x08`. It does not overwrite the stored object, increment the count, allocate a node, or invoke the post-insertion helper. Since FinishPartial ignores this flag, duplicate insertion is not a demonstrated reason to abort, roll back, or skip the second index/final object call. See [index pseudocode](rea-native-registration/14-Registration-index-helper-0x1801356f0-pseudocode.json) and [assembly](rea-native-registration/15-Registration-index-helper-0x1801356f0-assembly.json).

The new-key branch calls `0x180021A80` before checking the count limit, then modifies linked nodes/buckets and invokes `0x1800207C0`. Only after that call returns does it publish the inserted result. These two callees' allocation/copying, maintenance, failure and cleanup semantics remain unexamined. The count-limit path has an indirect terminal call followed by `INT3`; the evidence expressly records an unresolved no-return basis. Dictionary assignment does not reproduce this contract.

`0x18017FCA0` obtains a `[first,last)` range from `0x18017FE40`, counts it, deletes it and returns that count. If it spans the entire chain, the helper resets the head/count, releases nodes and calls `0x18001B4F0(container,8)`. Otherwise it calls `0x180020880` per node. This proves range deletion, but not how the input ID selects that range. An empty container can meet the whole-chain condition, so an empty selected range is not proof that the helper has no side effects. See [range-deletion pseudocode](rea-native-registration/11-Manager-container-128-helper-0x18017fca0-pseudocode.json) and [assembly](rea-native-registration/12-Manager-container-128-helper-0x18017fca0-assembly.json).

The next minimum registration evidence set is:

| Direct callee | Unclosed behavior |
| --- | --- |
| `0x18017FE40` | Input ID to range; equality, duplicates and misses |
| `0x180020880` | Single-node removal and associated container state |
| `0x18001B4F0` | Whole-chain follow-up with argument 8 and its failure behavior |
| `0x180021A80` | Allocation and key/object copying; failures/cleanup |
| `0x1800207C0` | Post-link maintenance and state if it fails |

`0x1802DC390` is labelled `free` by the decompiler; that identity can be confirmed separately before assigning a complete memory-management contract. No additional backend query was performed for this review. The duplicate branch alone is not a complete native registration implementation.

## Remaining behavior that this review does not close

The minimal class-dispatch primitive can proceed within the declared domain. Successful native partial-object creation and full registration still require evidence for:

1. `0x1801C4D20` (`TableDepot::LoadFile`): lookup, caching, ownership, failure behavior, and the concrete loaded-table type.
2. The loaded-table vtable `+0x28` target: `Class` field/default semantics, returned storage, and pointer validity.
3. Actual metadata rooted at `0x18041CF00`: child population/order, class names, registry lifetime, and the chosen `+0x28` function pointer.
4. Each concrete factory callback and constructor reached by the chosen class. The metadata tail jump does not itself reveal allocation or initialized object state.
5. `0x18003A780` (`Object::SetObjectName`): name storage and any relationship to object `+0x28`.
6. `0x180022CE0` and `0x180023090` if reproducing the native name-storage path: allocation, copying, failure, and ownership semantics.
7. The unexamined container/index callees listed above before claiming the native registration success path.
8. The chosen object's vtable `+0x08` target and its full record-read effects.
9. Diagnostic slot `+0x18` behavior if failure-path diagnostics become part of the equivalence claim.
10. Concurrent metadata mutation, registration visibility, exceptions, and rollback. The displayed caller and helper bodies do not supply a complete contract for those behaviors.

No original game execution, save access, Steam-cloud access, new backend query, or source edit was performed for this analysis document. It records the current evidence and the narrowly authorized next primitive; it is not evidence that a concrete native factory or the complete registration lifecycle has been implemented.
