# Class-field getters and the partial-object boundary

Date: 2026-10-19. This document records static evidence and the bounded interface now written in the project. The new implementation has not yet received its final build/test acceptance in this document. The original game was not run. Concrete native table construction, archive parsing, class-registry population, constructors, and derived-object `Read` remain separate obligations.

## Evidence identity

All functions below are in the fixed x64 Engine artifact:

```text
D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll
SHA256 4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153
```

The [getter batch summary](rea-record-class-getters/summary.json) records **11/11 calls passed**, close acknowledged, final closed session verified, and process exit confirmed. The [support batch summary](rea-record-getter-support/summary.json) records the same successful lifecycle for **17/17 calls**. Both record identical before/after Engine hashes and `targets_launched: false`. Analysis used Rea 6.5.0 with Ghidra 12.1.4; pseudocode is decompiler output, not recovered original source text.

| Batch | Fixed plan SHA256 | Completed UTC |
| --- | --- | --- |
| Getter bodies | `344044fcca3774a89eda63be0ab39035e8b5a93e05565abe32d5ac6d3b47aebc` | `2026-10-19T04:23:10.876Z` |
| Four direct helpers | `96cecea8e627427d61d275f3db086078bb076011c1b9af7e1827070ae4159547` | `2026-10-19T04:26:14.083Z` |

Each result's `result.structuredContent` preserves its function address, subject digest, provider, normalized body, and limitations. Principal assembly evidence is:

| Function | Assembly and evidence ID |
| --- | --- |
| `0x18013C6B0`, `LoadTable::GetValue(char const*, char const*)` | [getter 05](rea-record-class-getters/05-LoadTable-class-getter-0x18013c6b0-assembly.json), `ev_adef7a7356ecbdb85c27499c853261deafd746876dfeffe91ad40da611cd4901` |
| `0x18013FB60`, `LoadTableBinary::GetValue(char const*, char const*)` | [getter 08](rea-record-class-getters/08-LoadTableBinary-class-getter-0x18013fb60-assembly.json), `ev_d93b24e727701aecbda12dae18888d260725700fb0eb4669912000fd6f6e2376` |
| `0x18013EBC0`, Base tree search | [support 05](rea-record-getter-support/05-LoadTable-tree-search-0x18013ebc0-assembly.json), `ev_dead69e72c8521d6373c4e7c8cd1a978b843b04a73e9299c6060ba6a6d9876af` |
| `0x180022CE0`, counted string copy | [support 08](rea-record-getter-support/08-CString-copy-0x180022ce0-assembly.json), `ev_11c9b1ee555aca0e8cce63184b7c54b2ead44ad1abfcc56803cecea33208c5c7` |
| `0x180283840`, Binary name-to-ID | [support 11](rea-record-getter-support/11-LoadTableBinary-name-to-id-0x180283840-assembly.json), `ev_9c584721492cc653b6046112c36384593d01920cb7620a118ea68eb26d797b66` |
| `0x18013FC80`, Binary numeric getter | [support 14](rea-record-getter-support/14-LoadTableBinary-id-getter-0x18013fc80-assembly.json), `ev_4ab7f67f4bbdbbeb3813db082b578f4263554da3009dd02ac8fb340773015f69` |

The earlier fixed [vtable-slot metadata](record-interface-discovery/record-interface-vslots.json) maps exported `LoadTable` vtable `0x180341F10 + 0x28` to `0x18013C6B0`, and exported `LoadTableBinary` vtable `0x180341FE0 + 0x28` to `0x18013FB60`. These are on-disk preferred addresses. They establish the two declared types' slots, not the concrete runtime type of an arbitrary opaque cache payload.

## CreatePartial calls the factory before testing the class text

The [current-host caller assembly](rea-native-registration/04-ObjectManager-CreatePartial-order-assembly.json), evidence `ev_5685a6f351382b23dc14bd60345168e3962bab9b962c92becd0bc5a90bab521d`, establishes this order for `0x18017E170`:

1. `0x18017E180`: call `0x1801C4D20` with manager `+0x170` and the original record-name string. A null loaded table returns immediately.
2. `0x18017E1AC`: call loaded-table vtable `+0x28`. The query pointer is `0x18031B34C`; the default pointer is `0x18030805A`.
3. `0x18017E1BC`: call `0x180274EC0` with metadata root `0x18041CF00` and the getter's returned pointer.
4. On a nonnull factory result, `0x18017E1CF` calls `Object::SetObjectName` (`0x18003A780`) with that object and the **original record name**, then returns that object.
5. Only after a null factory result does the caller test the class pointer for null (`0x18017E1E7`) and its first byte for NUL (`0x18017E1EC`). A nonnull, nonempty class reaches the engine vtable `+0x18` diagnostic at `0x18017E20A`.

The literal texts `"Class"`, `""`, and the diagnostic format `Unable to spawn object "%s".` are identified by the preserved [old CreatePartial pseudocode](../rewards/rea-object-ids/ObjectManager-CreatePartial.json), evidence `ev_e635e31ef2c093d6996584dcd5784950442d1e8172b0cab5a9bc5ea81ba930b1`. That file declares the **same Engine SHA256**, exact function `0x18017E170`, and Ghidra 12.1.4. The current assembly independently supplies the call order and literal addresses; this review did not reread literal bytes from the original binary.

The later null/nonempty checks do not protect the earlier factory call. An empty class string is still dispatched and may match explicitly supplied empty metadata. A null pointer is not a safe empty string in the native class comparator. A known null factory result with nonempty class reaches an unresolved diagnostic call; silently returning null as a fully equivalent outcome would omit that behavior. The diagnostic's formatting side effects, error delivery, exceptions, and engine state are not recovered here.

This caller does not allocate an Object ID, perform final registration, or invoke the object's record-reading slot. The [class-dispatch evidence](NATIVE-REGISTRATION-EVIDENCE.md) describes the separate stable metadata/callback domain. Its own callbacks are not recovered native constructors or a native calling convention.

## Base LoadTable getter

`0x18013C6B0` receives `this`, a C-string query, and a default pointer. Its normal path is:

- Enter the critical section addressed by `this + 0x28` at `0x18013C6E5`.
- Initialize a local string with length zero, capacity 15, and first byte NUL.
- Scan the query to its first NUL (`0x18013C703` through `0x18013C719`), then call `0x180022CE0` with that prefix length at `0x18013C723`.
- Search the tree at `this + 0x08` through `0x18013EBC0` at `0x18013C734`.
- Recheck the candidate against the query, release local heap storage when required, choose stored value or the original default, then leave the critical section at `0x18013C802` before returning the selected pointer.

The wrapper always scans/copies the query before the tree search, including when the supplied tree is empty. A reconstruction may not avoid a reached unknown query by returning default solely because its own field list is empty.

### Search and exact-match proof

The tree helper initially retains the sentinel as candidate and begins at sentinel `+0x08`. A node with flag byte `+0x19 == 0` is visited. Node key string storage starts at `+0x20`, stored length is `+0x30`, and capacity is `+0x38`; capacity below 16 selects inline storage, otherwise the stored pointer is used.

At `0x18013EC22`, the helper compares node key to query using `memcmp` over the smaller stored length, then breaks equal-prefix ties by length. A node key below the query selects right child `+0x10` (`0x18013EC2D`). A key greater than or equal to the query records that node as candidate and selects left child `+0x00` (`0x18013EC57`). Reaching a sentinel-flagged node ends the traversal. Thus, **on a valid stable BST ordered by this comparator**, the helper returns the first key greater than or equal to the query, or the sentinel.

The caller compares in the reverse direction, query against candidate. A negative result rejects the candidate at `0x18013C79E` through `0x18013C7A5`; nonnegative retains it. This check alone was insufficient to prove equality. Combined with the now-observed lower_bound behavior and the valid BST invariant, it accepts exactly an equal key.

The comparison is unsigned-byte lexicographic `memcmp`, with stored-length tie breaking. The previously verified [memcmp import evidence](record-load-discovery/memcmp-import.json) and [thunk assembly](rea-record-cache-compare/05-Record-cache-compare-0x1802ee060-assembly.json) identify the normal `VCRUNTIME140.dll!memcmp` contract. No lowercasing, slash conversion, text decoding, or locale comparison appears in these Base search bodies.

### Counted-copy boundary

`0x180022CE0` normally copies the supplied count, sets stored length, and writes the terminator. It has an overlap branch to `0x180023180`, a length-error branch, and a capacity-growth call to `0x180022F70`. Here the caller supplies a fresh local string with length zero, so the existing-content overlap range is empty. Within the existing bounded normal-success string-allocation/copy domain, length zero resets the local empty string; positive length copies the scanned query prefix, growing capacity when it exceeds 15.

The helper's full general alias/error domain is not required by this fresh-destination path. Existing growth/release evidence and [name-copy import identity](name-interface-discovery/name-copy-thunk-0x1802ddb0e.json) support the declared normal memory-operation contracts. Native OOM, CRT exceptions, invalid pointers, and allocation identity are not recovered by treating own managed allocation as successful native allocation.

### Stored value, default, and NUL are distinct

After the candidate check, `0x18013C7E3` tests **stored value length**, at node `+0x50`, against zero. It does not test the value's first byte or scan its contents. A positive-length value returns inline storage at node `+0x40` when capacity `+0x58 < 16`, otherwise the pointer held at `+0x40`. Candidate miss or zero stored length returns the exact supplied default pointer.

| Reached Base condition | Returned identity | Subsequent class comparison |
| --- | --- | --- |
| No exact key | Original default | Reads the returned default only if later dispatch reaches it |
| Exact key, stored value length zero | Original default | Same rule; no stored-value pointer is returned |
| Exact key, positive length, first byte NUL | Stored value | Sees an empty C string; this is **not** the default branch |
| Exact key, positive length, interior NUL | Stored value, including its full underlying payload | Consumes bytes only through the first NUL |
| Positive-length value whose required storage/terminator is unknown | No invented contents | The independent covered domain must reject the reached unknown data |

The getter returns a borrowed storage pointer after unlocking. It does not copy the selected value into caller-owned storage. A stable own model must explicitly preserve entry/string identity and lifetime through class dispatch; native concurrent mutation or post-unlock lifetime guarantees have not been established.

## Query key and record name use different lengths

Only the **query argument** is converted to a C-string prefix for the Base search: `Class\0suffix` queries `Class`. Stored node keys retain their complete stored lengths. A stored key `Class\0suffix` is greater than and unequal to query `Class`; truncating stored keys would change behavior. An empty query may match a zero-length stored key.

This differs from the full stored-length normalization/cache lookup in [Record Load evidence](RECORD-LOAD-EVIDENCE.md) and the full stored-length name assignment/hash in [Object Name evidence](OBJECT-NAME-EVIDENCE.md). The class value has yet another boundary: the getter selects storage using its stored length, while the later factory comparator uses bytes through the first NUL. The original record name passed to `SetObjectName` is not replaced by the class query, the class text, or the cache's normalized key.

## Binary LoadTable wrapper and observed helpers

At `0x18013FB94`, the Binary wrapper enters the critical section from `this + 0x28`, then reads `this + 0xC8` at `0x18013FB9B`.

- **Known null context/archive:** branch directly to unlock at `0x18013FBCA`, then return the original default pointer at `0x18013FBD0`. This path does not read query bytes or default contents. An explicitly known null default is a possible returned identity.
- **Present context/archive:** pass `context + 0x88` and the original query pointer to `0x180283840` at `0x18013FBB1`. Pass the low 32 bits of its result to `0x18013FC80(this, id, default)` at `0x18013FBBE`, unlock, and return that result unchanged.

The wrapper does not reject ID `0xFFFFFFFF`, null returns, empty values, or leading NUL. Base empty-value fallback must not be imposed on it. Unknown context is not equivalent to a null context, and this analysis does not prove how a native constructor initializes the field.

### Name-to-ID is stateful and includes raw-pointer identity

The observed `0x180283840` body first checks a cache rooted at helper-context `+0x70`. Bucket selection uses the original query pointer shifted by metadata at `+0x58`, narrowed to 32 bits, and masked by `+0x60`. The chain compares query **pointer identity** against node `+0x00` and follows node `+0x10`. A cache node carries its ID in `+0x08`.

A pointer hit compares the current query C string against the string indexed by that ID in context `+0x40`. Matching contents return the cached ID. Changed contents trigger a C-string copy and the existing `0x18003C8E0` lookup; a known successful lookup updates the cached node's `+0x08` at `0x1802839F4`. A lookup miss returns `-1` without that update.

A pointer-cache miss also copies the C-string prefix and calls `0x18003C8E0`. A successful non-`-1` ID calls **`0x180284160(context + 0x58, originalQueryPointer, signExtendedID)`** at `0x180283915`. Its insertion/allocation/cache-maintenance body has not been queried or restored. The [direct-callee evidence](rea-record-getter-support/12-LoadTableBinary-name-to-id-0x180283840-direct-callees.json) records this exact unresolved target.

Consequently, a supplied plain name-to-ID dictionary would omit pointer identity, mutable cache metadata, cached-content verification, insertion, and update effects. This phase deliberately stops the supported Present-context path before entering this helper; it does not extend analysis indefinitely through the cache implementation.

### Numeric getter is observed, but not independently implemented

`0x18013FC80` searches a chain rooted at `this + 0xB8`, using sign-extended 32-bit ID, shift metadata `+0xA0`, and mask `+0xA8`. No chain/key match returns the supplied default. A matched payload whose type tag is not 2 also returns default (`0x18013FCCE`).

For tag 2, payload `+0x04` selects a 32-bit entry in `this + 0x60`; that entry becomes a signed string index into the archive/context's string pool (`+0xC8` begin, `+0xD0` end, 32-byte stride). An index outside the observed range or a negative index returns **null**, not default (`0x18013FD1D`). A valid index selects inline/pointer storage by string capacity and returns it without a stored-length emptiness check. There is no observed bounds check for the earlier payload-to-buffer indexing step, so valid parsed buffers remain a necessary precondition.

The [numeric direct-callee list](rea-record-getter-support/15-LoadTableBinary-id-getter-0x18013fc80-direct-callees.json) is empty; that does not prove its input tables, pointers, or parsed data are initialized correctly. This body is documented but will not become an isolated successful model that bypasses the name-to-ID helper's unresolved effects.

## Minimum independent interface and scope assessment

A bounded Base getter is justified over an **explicitly typed, complete, stable snapshot of unique fields sorted by unsigned-byte lexicographic order over full stored keys**. Such a snapshot can reproduce the valid BST's lower_bound/exact-match result without reproducing native node allocation or tree shape. Duplicate, unsorted, incomplete, unstable, unknown, or out-of-budget supplied snapshots are outside this declared domain; they must not become an invented known miss. Search cost and damaged-tree behavior are not claimed equivalent.

The interface must keep the following distinctions:

- Known concrete `LoadTable` versus known concrete `LoadTableBinary`, versus unknown table kind. An arbitrary opaque table cached by the prior module is not automatically a Base table. Native constructor/vptr selection and parsed-field population are not inferred.
- Original default identity versus selected entry/value identity, even when both expose the same empty C-string bytes. Unknown identity is not known null, and bytes must not be synthesized for an unknown pointer.
- Full stored keys and values versus the C-string query prefix and the factory's C-string class comparison. Bounds/terminator checks belong to the reached reads; a short-circuit path should not require unread contents.
- Successful own gate entry/exit and stable storage as explicit normal-operation contracts. Native concurrency, lock failures, invalid-memory faults, and exception unwinding remain outside the equivalence claim.
- Explicit callback and metadata contracts for the class factory. No native ClassInfo population, concrete constructor, ID allocation/registration, InventoryAdd integration, or actual derived `Read` follows merely from a successful getter.

For Binary, the minimum supported branch is an explicitly **known absent archive/context**, with normal gate operations and an explicit default identity. An unknown archive state is Unsupported; a present archive is Unsupported before `0x180283840`. Query bytes can remain unknown on the absent branch because native code does not read them there. This narrow branch must not be justified by assuming default-initialized native fields.

When composing CreatePartial, preserve loaded-table null short-circuit, getter dispatch, class-factory dispatch, then name assignment to a nonnull result in that order. A known null factory result with a nonempty class reaches the unresolved diagnostic boundary; prior callback effects must remain visible and must not be described as rolled back. Missing/unknown data and implementation budgets must not silently turn an unknown branch into a native null/default result.

## Written implementation and completed acceptance

The current [RecordTableGetter.cs](../../src/task_items/RecordTableGetter.cs) implements the above bounded interface. `TableString` clones a complete payload of at most 65,536 bytes, retaining interior NUL bytes; `CopyCStringBytes` returns the full payload plus a logical trailing NUL, not a prefix-truncated copy. `LoadTableField` retains key/value object identity. `LoadTableSnapshot` clones the field array and requires at most 4,096 complete, strictly ordered unique fields with at most 2 MiB of aggregate key/value payload. Its binary lower_bound search preserves the valid-tree result and selected entry identity, not original tree shape or comparison count.

The Base path performs gate entry, bounded C-string prefix copy, field lookup, stored-length fallback selection, and gate exit. The Binary path supports only `ArchivePresent == false`; unknown and present states return distinct Unsupported reasons. The absent branch can accept a null/unknown query without reading it. `Known` with a null value denotes an explicitly supplied null default; callers must inspect status instead of equating every null `Value` with absence or failure. Own cleanup/exception reporting is explicitly not native exception equivalence.

The current [CreatePartialObject.cs](../../src/task_items/CreatePartialObject.cs) composes the existing bounded LoadFile module, the typed Class getter with `Class` and empty default, the explicit class graph/callback primitive, and name assignment. It preserves the original record-name object for the post-callback name assignment. A known null loaded table returns before needing any getter/class metadata. Empty class text still enters the factory. A null constructor result with nonempty class returns Unsupported at `SpawnDiagnostic`; it does not fabricate diagnostic completion. The result retains prior subresults, callback effects, and a constructed instance even if a later stage becomes Unsupported.

The fresh unified build passed 703 assertions: the prior 620 plus 43 getter assertions and 40 CreatePartial assertions. Both combination suites passed four scenarios in total, and the existing ItemQuestDemo passed separately. The 20 ObjectId regression assertions are included in 703. See the [completed acceptance](class-getter-validation/acceptance-class-getter.json) and [result report](CLASS-GETTER-RESULT.md) for the exact scope and artifacts. The historical 620-assertion record is preserved. Both recorded analysis sessions are closed; no further backend query is requested here.
