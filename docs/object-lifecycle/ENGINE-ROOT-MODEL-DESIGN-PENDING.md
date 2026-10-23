# Engine root registration model: design pending implementation

2026-10-20. This document specifies a possible own-code model of the saved
Engine writer/helper contracts. It adds no implementation and records no
compilation, test execution, passing fixture, or native compatibility result.
Interface names below are proposals, not claims about existing APIs.

## Evidence and status

This design uses these archived project documents:

| Source | SHA256 |
|---|---|
| [ENGINE-ROOT-WRITERS-RESULT.md](ENGINE-ROOT-WRITERS-RESULT.md) | `fa968e82bb66f27a0131041349c44a0cb35a8b40a0298793b53390395f823c10` |
| [ENGINE-ROOT-HELPERS-RESULT.md](ENGINE-ROOT-HELPERS-RESULT.md) | `d3132f419deb477704012d2e8c22c8af727ebab1ce9d67b69f607f8c03400686` |
| [ENGINE-ROOT-REGISTRATION-EVIDENCE.md](ENGINE-ROOT-REGISTRATION-EVIDENCE.md) | `008b472f8537c87fddce5cb8c1f9035214ae3660543297ad6d4349974cb79188` |
| [ENGINE-ROOT-HELPERS-EVIDENCE.md](ENGINE-ROOT-HELPERS-EVIDENCE.md) | `08c26dd879317648263d7668b323b412f3251a719afa477407d2c8a65fa4540c` |

The later helper result resolves the earlier writer result's open helper
identities. It does not resolve the writer strings, callback bodies, full
root graph, or successful native startup. Engine evidence is independent of
similarly shaped Game helpers.

The pinned source identity recorded by those reports is Engine.dll,
4,485,632 bytes, SHA256
`4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`.
No source binary was opened to prepare this document. The archived root
investigation accounts for 2908/8192 code bytes; this design adds no binary
reads or disassembly.

## Model boundary and explicit input state

The smallest useful unit is one observed writer invocation over explicitly
supplied, mutable own records and own storage. It is not a default Engine
startup routine or a generated complete registry.

| Writer identity | Table slot | Writer RVA | Own-record identity corresponding to RVA | Unread input-string RVA | Unread tail-callback RVA |
|---|---:|---|---|---|---|
| Actor | 23 | `5C70` | `41CFC0` | `309E64` | `300F90` |
| Entity | 31 | `61F0` | `41CF30` | `309AC4` | `301110` |
| Object | 55 | `7270` | `41CF00` | `309AEC` | `301590` |

The type/export identities above do not prove the contents of the three
serialized name strings. String and callback RVAs should be opaque evidence
identifiers. A fixture may associate its own text or callable outcome with
such an identifier; it must label that association as fixture data. The
model must never call a native address.

An own record needs observable name (`+08`), parent (`+10`), raw 32-bit child
count (`+18`) and children-storage (`+20`) fields. Record identity is stable;
the parent selector and selected parent's fields are mutable. Existing
factory (`+28`) and unrelated fields are opaque and untouched. Storage needs
identity, an explicit own capacity, and readable/writable child slots so that
the model can expose a partly copied or appended allocation.

The disk parent chain Actor -> Entity -> Object -> null supports a fixture
that explicitly adopts those starting links. It does not prove those links
or zero counts are the state at real invocation time. A fixture can explicitly
invoke Actor, Entity, Object in the saved table-position order to examine
conditional effects; no default scheduler may present that as observed CRT
iteration or successful native initialization. Other table entries and
sibling registration must not be silently replaced with an empty graph.

## Proposed dependency contracts

Dependencies operate only on own tokens/state and return explicit outcomes.
Normal return, nonlocal dependency exit, insufficient evidence, and a model
resource stop must remain distinguishable. An unexpected host exception is
an implementation failure, not proof of a particular native exception.

| Proposed dependency | Required input/output behavior | Evidence limit |
|---|---|---|
| String duplication | Receive the writer's fixed source token; normally return an own name-allocation token or null. May modify explicitly supplied state before returning. | IAT `302BB8` binds `_strdup`. Null is a normal result that the writer stores; do not invent a null guard, fallback text, registration-name match, or old-name release. Nonlocal exit occurs before the writer's parent load/name store. |
| Malloc | Receive the unchanged 64-bit size; return an own storage token or null, or an explicit nonlocal outcome. Own capacity is separately known. | `2DC900 -> 2DC398 -> 2DDB38 -> 302968` binds `malloc`. Nonzero identity alone does not establish valid native capacity or ownership. |
| New handler | Receive the same saved size after malloc returns null; return raw 32-bit EAX or a nonlocal outcome. State mutations must remain visible. | `2DDB7A -> 302960` binds `_callnewh`. Every nonzero EAX, including negative bit patterns, retries; zero selects a failure wrapper. |
| Throw dispatch | Receive the selected wrapper/descriptor identity and the three observed stack-object field values. Represent a nonlocal throw outcome without inventing its concrete C++ type or unwinding effects. | `2DDB26 -> 302708` binds `_CxxThrowException`. The wrappers call it and then contain `INT3`, with no ordinary return. An unexpected normal dependency return must stop as an unsupported trap continuation, not become null allocation or successful retry. |
| Free | Receive the selected parent's storage read immediately before the free call. Preserve explicit dependency effects and distinguish normal completion from nonlocal exit. | `2DC8F8 -> 2DC390 -> 2DDB3E -> 302970` binds `free`. The wrappers add no clearing, destructor, null guard or Boolean failure result. Own memory bookkeeping must not be described as recovered CRT internals. |
| Exit registration | Supply explicit cookie/state values and a mutable state-cell identity. Call the selected own `_crt_atexit` or `_register_onexit_function` dependency with the observed arguments; receive raw EAX or a nonlocal outcome. | `2DC890` reaches IAT `302A70` or `302A88`. Runtime global initialization, imported-library internals, acceptance of invalid inputs, and external state changes on failure are not recovered. |

The allocation coordinator's request is calculated once by the writer.
Malloc null -> handler nonzero -> retry reuses that exact size; parent changes
during a handler must not recalculate it. No zero-size normalization, retry
limit, handler-positive-only rule or null-success allocation is evidenced.
A finite own step/allocation budget is necessary for safe fixtures, but its
exhaustion is a model resource stop with a retained trace, not a native
failure code or a native retry limit.

When the handler returns zero, request `UINT64_MAX` selects `2DD600`; other
requests select `2DD5E0`. Their constructor writes occur in this order:

| Wrapper | Constructor | First write `+10` | Second write `+08` | Third write `+00` | Throw descriptor |
|---|---|---|---|---|---|
| `2DD5E0` | `2DD508` | 0 | `180307488` | `180307478` | `1803AF558` |
| `2DD600` | `2DD568` | 0 | `1803074B0` | `1803074A0` | `1803AF5B8` |

Both visible constructors return `this` and have no callee. Their address
values should remain opaque data identities: payload text, vtable semantics,
descriptor layouts, concrete exception types, and native EH cleanup remain
unknown.

## Writer sequence and retained partial state

The model should record the following events in order, with the fields read
at the point shown. It must not use an atomic rebuild-and-commit shortcut.

1. Invoke string duplication. Once it returns, load this record's current
   parent, then publish the returned name token, including null.
2. If that captured parent is null, skip child allocation/copy/free and
   proceed directly to exit registration. The writer does not clear its own
   child count/storage or alter its parent/factory.
3. Read the selected parent's raw 32-bit count. Increment with 32-bit wrap,
   sign-extend that result to 64 bits, interpret it as an unsigned multiplicand
   and multiply by 8; use `UINT64_MAX` on unsigned multiplication overflow.
   Preserve the resulting request across all allocation retries.
4. Allocate through the malloc/handler/throw coordinator. Keep the selected
   parent identity captured before allocation. Do not reread this record's
   parent selector after a dependency mutates it.
5. After allocation returns, reread the selected parent's current count and
   children storage at the observed loop accesses. Copy existing children
   in order into the new storage. Do not freeze the earlier count/storage or
   allocate a different capacity merely because these values changed.
6. Read the current count for the append index, append this record's identity,
   and increment the selected parent's in-memory 32-bit count.
7. Read its current old-storage identity and call free. Only after normal
   completion, publish the new storage identity to that parent's `+20`.
8. Invoke exit registration with this writer's fixed callback token, and
   preserve the returned signed 32-bit result. No rollback follows a returned
   registration failure in the saved writer.

No duplicate test appears. Repeating a writer appends another occurrence when
its parent remains nonnull. Later invocation of Entity's own writer does not
erase previously appended Actor children; invoking Object with a null parent
does not erase children already appended into Object.

Raw arithmetic can be modeled independently for all 32-bit inputs. This does
not make negative/corrupt counts, invalid storage, insufficient capacity or
uncontrolled concurrent mutation supported writer inputs. Own safety guards
must identify the exact operation that cannot be represented and preserve
the earlier trace/state. They must not replace an unsafe native access with
a successful empty copy, resize silently, clamp an index, or claim that a
model guard is a native check. Extra fixture instrumentation between modeled
reads is test machinery, not an inferred native callback.

| Stop point | State/effects that must remain visible |
|---|---|
| String duplication does not return | Its explicit external effects; the writer has not yet loaded parent or stored name. |
| Allocation/handler/throw does not return | Name has already been stored; selected parent and saved size remain traceable. Explicit dependency mutations are retained. |
| Copy or append cannot complete | Already copied slots and any completed append remain observable in the new own allocation; do not claim it has been published. |
| Free does not return | Append and parent count increment already occurred; the writer has not published the new storage. Dependency effects are not rolled back. |
| Exit registration returns -1 or exits nonlocally | Prior name/count/storage publication remains. External registration effects are not assumed atomic or reversed. |
| Own resource limit is reached | The modeled prefix and dependency calls remain; result is incomplete, never a normal native return. |

## Exit registration arithmetic

Read explicit 64-bit values corresponding to `G=[41C520]` and
`E=[420D50]`, then compute `D=ROR64(G XOR E, G & 63)`. No default disk-value
branch is permitted.

- `D == UINT64_MAX`: call `_crt_atexit(callback)`. The observed machine state
  also retains all-ones in RDX, but the model should not invent a second
  declared argument for that import.
- Otherwise call `_register_onexit_function(stateCell, callback)`, where
  `stateCell` represents the address `&420D50`, not the decoded value D.
- External EAX equal to zero returns the original callback token from
  `2DC890`; any nonzero EAX returns zero.
- `2DC8E0` maps a nonzero token to EAX=0 and zero to EAX=`FFFFFFFF`.
  The latter is signed 32-bit -1 and zero-extended RAX=`00000000FFFFFFFF`,
  not 64-bit all-ones.

The three observed callback addresses are nonzero. A null-token helper
fixture may check that the import is still invoked and the final result is
-1 even on external EAX=0. Such a fixture does not establish that a native
CRT accepts a null callback.

## Planned own-fixture matrix (not implemented or executed)

Fixtures should assert event order, arguments, mutation snapshots and the
result category. A return value alone is insufficient. Literal names and
tokens used by the fixtures are explicitly own data.

| Scenario | Required observation |
|---|---|
| Null parent and normal name/exit | No allocation/free; own existing children and factory stay intact; name and exit result are recorded. |
| Null strdup result | Null name is stored; nonnull parent still follows allocation/append/free/publication. |
| Parent changed by string dependency | The parent selected after string return is used; no earlier parent snapshot is substituted. |
| Normal nonempty parent | Prior children retain order, including null slots and repeated identities; self is appended; count increments before free and new storage publishes after it. |
| Repeated writer | A duplicate occurrence is appended; previous name allocation is not automatically released by the writer. |
| Explicit Actor -> Entity -> Object fixture | With explicitly adopted links, later writers preserve children registered earlier. This demonstrates only those local invocations. |
| Malloc immediate nonzero | Handler/throw are not called; capacity is independently supplied and checked by own storage. |
| Malloc null then handler returns 1, `0x100`, `0x80000000`, or `0xFFFFFFFF` | Every nonzero raw EAX retries with the original size; a later nonzero allocation continues. |
| Handler mutates selected parent count/storage and this record's parent | Request and selected parent identity stay fixed; subsequent accesses use the selected parent's updated fields, not the new parent selector. |
| Mutation makes capacity/storage invalid | Stop at the unsupported access with the earlier prefix retained; do not resize or report a completed registration. |
| Handler zero, ordinary size versus all-ones size | Correct wrapper/constructor/descriptor identities and field-write order; no fabricated concrete exception type. |
| Throw dependency unexpectedly returns | Incomplete unsupported trap continuation; no null allocation result or successful retry. |
| Count arithmetic boundary vectors | Independent raw-size checks cover 0, 1, `7FFFFFFE`, `7FFFFFFF`, `80000000`, `FFFFFFFE`, `FFFFFFFF`; corrupt-count writer completion is not asserted. |
| Failure injected at string, allocation, copy, free, or exit | Exact completed prefix and external effects remain; no invented rollback, clearing or generic false return. |
| Cookie selector branches | Independently chosen own goldens with cookie low values 0, 1, 63, 64, 65 verify the `&63` rotation mask and XOR-before-ROR order; all-ones/non-all-ones D selects the correct import and state-cell identity. |
| Exit EAX 0, 1, `0x100`, `80000000`, `FFFFFFFF` with nonzero callback | Only zero yields final 0; all nonzero results yield signed 32-bit -1 after state publication. A token with only its high 32 bits set checks that the callback zero test uses all 64 bits. |
| Null callback helper-only fixture | Selected import still runs; final -1 even when its EAX is zero; no native-valid-input claim. |
| Repeated null allocation/nonzero handler until own budget | Explicit model resource stop with exact retry trace; no claim of a finite native retry limit. |

If a pure helper is tested across invalid raw values, its arithmetic result
must remain separate from a writer-level claim that the corresponding native
memory accesses are valid. Failure fixtures similarly test an explicit own
dependency contract, not a recovered Windows fault or EH implementation.

## Delivery and compatibility gates

A later implementation may be called a local contract model only after its
interfaces and trace semantics match this evidence. A later validation report
must state which own fixtures actually compiled and ran, with their new
results. Historical test counts and evidence byte/hash verification are not
substitutes for implementation tests.

The following stay outside a successful local model result:

- Real string contents and serialized registration-name matching.
- Runtime selector/onexit initialization, CRT startup iteration and treatment
  of writer return values, callback/destructor execution and loaded imports.
- Other initializers/siblings, complete root availability, and absence of
  other mutations to the records.
- Concrete native allocation ownership/capacity, custom new-handler behavior,
  exception-type decoding, native EH/unwind cleanup and arbitrary faults.
- Native class factories, full constructors/Read paths, an official record's
  actual Class dispatch/object loop, or formal native-sample compatibility.
- Original save compatibility, which is outside this independent project's
  intended goal.

No implementation source, build/test script, security setting or prior
evidence is changed by this document. The earlier
`powershell.exe -NoProfile -NonInteractive -File src/task_items/build_class_registration.ps1`
was refused by PowerShell AuthorizationManager. This design neither retries
it nor authorizes an equivalent build command. The parent is separately
diagnosing that restriction read-only; no compilation/test action is pending
or of uncertain outcome from this design task.
