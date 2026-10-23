# Object base and record-interface boundary — 2026-10-19

The later [name-assignment evidence](OBJECT-NAME-EVIDENCE.md) closes a bounded normal-success SetObjectName domain. The unimplemented-name statements below describe this earlier constructor checkpoint.

This continuation selects the exported Engine `GAME::Object()` as the smallest constructor candidate. It does not equate base-object initialization with a complete item factory or a concrete derived object's Read implementation.

## Proven caller chain

The existing CreatePartial assembly at `0x18017E170` calls `TableDepot::LoadFile` at `0x1801C4D20`, checks for a table, then calls that table's vslot `+0x28` with the Class key and default value. It passes the returned class name to the ClassInfo traversal at `0x180274EC0`. On a non-null constructed object, it calls `Object::SetObjectName` at `0x18003A780` with the requested record name.

The existing FinishPartial assembly at `0x18017E230` loads the table by the object's name, writes the ID, performs pending removal and both registration inserts under its gate, unlocks, and calls the incoming object's vslot `+0x08` with the loaded table. The registered raw key is read from `+0x28` after the first insert. The current independently compiled registration coordinator models this order with a caller-supplied callback; it does not supply a recovered native Read body.

Sources: [CreatePartial assembly](rea-native-registration/04-ObjectManager-CreatePartial-order-assembly.json), [FinishPartial assembly](rea-native-registration/05-ObjectManager-FinishPartial-order-assembly.json).

## Export and slot discovery

[Fixed PE export metadata](record-interface-discovery/record-interface-exports.json) independently identifies the Object default constructor at `0x18003A6B0`, SetObjectName at `0x18003A780` and LoadFile at `0x1801C4D20`. The export name of LoadFile returns a `LoadTable const*`; that signature alone does not determine its concrete dynamic type.

[Three fixed on-disk slot reads](record-interface-discovery/record-interface-vslots.json) bind these preferred addresses:

| Exported vtable | Slot | Stored target |
| --- | --- | --- |
| LoadTable, `0x180341F10` | `+0x28` | `0x18013C6B0` |
| LoadTableBinary, `0x180341FE0` | `+0x28` | `0x18013FB60` |
| Object base, `0x180339F78` | `+0x08` | `0x18000CE70` |

All three targets map to executable `.text` file bytes. The Object base target starts with `C2 00 00` followed by padding. This base-slot observation does not bind any concrete derived object's Read. Existing Game Item construction installs a different primary vtable, `0x18076A4C0`; its `+0x08` target is not established here.

The fixed batch [rea-object-base-plan.json](../../tools/static_analysis/rea-object-base-plan.json) queries only the three selected exported function bodies: pseudocode, assembly and direct callees for each, plus five lifecycle checks. Newly discovered callees and the two table-slot targets are outside this batch.

All addresses above refer to the pinned Engine image SHA256 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`, except the explicitly labelled Game Item vtable. No runtime addresses or original code execution are used.

## Constructor write set

The body at `0x18003A6B0` has seven instructions, including its final return, and no direct callees. It performs exactly these writes to caller-supplied valid object storage:

| Instruction | Observed effect |
| --- | --- |
| `0x18003A6B0` / `0x18003A6B7` | Load `0x180339F78` and store that base-vtable pointer at object `+0x00`. |
| `0x18003A6BD` | Store qword `15` at object `+0x20`, the name-capacity field. |
| `0x18003A6C5` | Store qword `0` at object `+0x18`, the name-length field. |
| `0x18003A6CD` | Store one zero byte at object `+0x08`, the first inline name byte. |

`MOV RAX,RCX` at `0x18003A6BA` makes the return value the supplied object pointer; `0x18003A6D1` returns. There is no allocation, helper call, registration, ID assignment or record loading in this body. It does **not** write object `+0x28` or `+0x2C`. It does not zero all inline name bytes: the unobserved bytes after the initial NUL must not be invented as initialized zero storage. The write set also does not establish the complete object size or a derived-object layout.

This is initialization of an already provided object-storage region, not proof that the factory callback at ClassInfo `+0x28` has been reconstructed. The base vtable's separately observed `+0x08` target beginning with `RET 0` establishes only that particular base-slot target. A derived constructor can replace the vtable; no concrete Item Read is identified or implemented by this constructor analysis.

Sources: [constructor pseudocode](rea-object-base/04-Object-default-constructor-0x18003a6b0-pseudocode.json), [complete constructor assembly](rea-object-base/05-Object-default-constructor-0x18003a6b0-assembly.json), [empty direct-callee set](rea-object-base/06-Object-default-constructor-0x18003a6b0-direct-callees.json).

## Name assignment boundary

SetObjectName at `0x18003A780` first compares the address of the object's name field (`this+8`) to the supplied string-object address. At `0x18003A794`, equality skips the assignment helper; otherwise `0x18003A7A0` calls `0x180023180(destination, source, 0, UINT64_MAX)`. This helper is its only direct callee, and its body is outside this batch. Ordinary assignment, overlap handling, growth, copying and failure behavior therefore remain unclosed. The self-alias branch skips assignment but still computes and stores the following key; it is not a complete no-op.

After assignment returns, the function reads the resulting stored length from object `+0x18` (`0x18003A7A5`) and starts EAX at `0x811C9DC5` (`0x18003A7AB`). For each byte in that **stored length**, it selects inline storage when capacity is below 16 or indirect storage otherwise, then executes:

```text
0x18003A7CE  MOVSX ECX, byte ptr [R8 + RCX]
0x18003A7D5  XOR   ECX, EAX
0x18003A7DA  IMUL  EAX, ECX, 0x01000193
```

Thus the observed 32-bit recurrence is `hash = (hash XOR sign_extend_32(byte)) * 0x01000193`, modulo 2^32. The sign extension matters for bytes `0x80..0xFF`: this is not the usual unsigned-byte FNV-1a interpretation. The loop compares its index against the saved string length at `0x18003A7E0`; it does not stop at a zero byte. Embedded NUL bytes inside the declared length participate, while a conventional terminator at index `length` is outside the loop. There is no evidence here of case folding, text decoding, slash normalization or a record-name parser. The index is incremented through the 32-bit EDX register; this observation does not establish behavior for arbitrarily large invalid/unsupported string states.

Finally, `0x18003A7E5` stores the hash to object `+0x28`. A zero-length name stores the unchanged seed `0x811C9DC5`. This differs from the default constructor, which leaves `+0x28` untouched. No `+0x2C` ID write appears. The later FinishPartial registration key read at `0x18017E34F` is consequently related to this write **when a successful SetObjectName path actually preceded it**; the model must not assume that every supplied object's raw field has already been computed this way.

No SetObjectName or hashing API is added in this step. In particular, implementing a managed assignment followed by this recurrence would silently replace the unexamined `0x180023180` contract. The constructor implementation below leaves the existing registration-key field as supplied or unknown.

Sources: [SetObjectName pseudocode](rea-object-base/07-Object-SetObjectName-0x18003a780-pseudocode.json), [SetObjectName assembly](rea-object-base/08-Object-SetObjectName-0x18003a780-assembly.json), [assignment dependency](rea-object-base/09-Object-SetObjectName-0x18003a780-direct-callees.json).

## LoadFile boundary

TableDepot::LoadFile at `0x1801C4D20` establishes a cache-oriented caller sequence, not a complete record-loading implementation:

| Address | Observed operation |
| --- | --- |
| `0x1801C4D48` / `0x1801C4D4C` | Load the critical-section pointer from depot `+0x78` and enter through the previously identified import. |
| `0x1801C4D53`–`0x1801C4D79` | Initialize an empty local string and call `0x180023180` to assign the supplied name. |
| `0x1801C4D84` | Call `0x18027B3C0` on the local string. This is a candidate normalization step; its actual transformation is not observed here. |
| `0x1801C4D97` | Call `0x18003C8E0` with depot `+0x08`, an output slot, and the local string. |
| `0x1801C4DA1` / `0x1801C4DA5` | Compare the returned node/iterator value against the sentinel stored at depot `+0x10`. |
| `0x1801C4DAF` | On the sentinel/miss branch, call `0x1801C4E20(depot, localString)` and retain its returned pointer. |
| `0x1801C4DB9` / `0x1801C4DBC` | On the non-sentinel/hit branch, increment the 32-bit field at depot `+0x54`, then read the table pointer from result-node `+0x30`. |
| `0x1801C4DC0`–`0x1801C4DF0` | Release local heap string storage through `0x180023090` if required and reset its local string metadata. |
| `0x1801C4DF9` / `0x1801C4DFF` | Leave the critical section, then return the retained table pointer. |

The unresolved bodies `0x180023180`, `0x18027B3C0`, `0x18003C8E0` and `0x1801C4E20` prevent claims about assignment, normalization, cache equality/order, lookup side effects, miss loading, archive precedence, table ownership or concrete dynamic type. The hit branch alone does not justify replacing the cache with a string-keyed dictionary. The normal-path unlock does not establish exception cleanup or rollback after any preceding helper fails.

The earlier `0x180023090` evidence describes its valid-storage normal release wrapper; that does not close the other dependencies or native allocator/free internals. Neither returned path binds the actual dynamic table to the base LoadTable or LoadTableBinary vtable. The two fixed `+0x28` slot targets in the discovery table remain candidates until object provenance is established, and their function bodies are outside this batch. No LoadFile implementation or concrete table getter is added.

Sources: [LoadFile pseudocode](rea-object-base/10-TableDepot-LoadFile-0x1801c4d20-pseudocode.json), [LoadFile assembly](rea-object-base/11-TableDepot-LoadFile-0x1801c4d20-assembly.json), [direct dependencies](rea-object-base/12-TableDepot-LoadFile-0x1801c4d20-direct-callees.json).

## Reproducible implementation

[ObjectBaseConstructor.cs](../../src/task_items/ObjectBaseConstructor.cs) implements only the constructor's logical initialization over its own state:

- `ObjectBaseState.Dispatch` identifies the observed base-object dispatch identity; it is neither a native vptr nor a callable Read implementation.
- The logical name has length 0, observed native-capacity metadata 15, and first byte 0. `CopyNameBytes()` returns the empty logical name; it does not expose or zero-fill unobserved inline padding.
- `Initialize(existingFields)` keeps the same `RegistrationObject` reference and does not modify its nullable `Id2C` or `RawKey28` fields. Its null-argument rejection is an own API precondition for valid supplied model state, not a reconstructed native null check.
- `Create()` starts both untouched registration fields as unknown (`null`), not as known zero. Managed allocation of the model wrapper is not asserted to reproduce native allocation or constructor-caller ownership.

There is no Read method, SetObjectName implementation, table access, ID allocation or index registration in this file. Calling the initialization helper does not establish that the live class factory selected this constructor, that a complete item exists, or that a concrete derived Read ran.

For reproduction, the exact request plan is linked above and the normalized function bodies are under each saved result's `result.structuredContent.normalized_result`. The [batch summary](rea-object-base/summary.json) records 14/14 calls passed, confirmed closed session, process exit code 0 and identical Engine SHA256 before/after. Those are evidence-collection results, distinct from independent implementation tests.
