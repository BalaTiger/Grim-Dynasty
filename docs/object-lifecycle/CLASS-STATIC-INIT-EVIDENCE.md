# Game entry and static-initialization route: bounded evidence draft

Date: 2026-10-20. Draft boundary: the saved import-binding report completed at
`2026-10-20T13:07:12.024Z`. The purpose is to locate the actual **Item / QuestItem
ClassInfo registration writers**, not to implement or execute DLL initialization.
The initializer table contents and individual writer identities have not yet
been read in the evidence summarized here. Later findings belong in an explicit
appendix rather than being implied by these entry-point observations.

This note was prepared by reading existing project JSON/instruction archives.
It did not read an original DLL, invoke dumpbin, start Rea/Java, load or execute
a DLL, repeat a getter/probe, run a build/test, or change product code/status.
The previous scalar-complete checkpoint reported **2,282 assertions / 83
scenarios**; that is historical acceptance, not a rerun or an increment made by
this phase.

## Fixed artifact and report identities

All addresses below are preferred image VAs in the same Game artifact:

```text
Game.dll
ImageBase: 0x180000000
Bytes: 11385856
SHA256: cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2
```

The archived analyses report unchanged before/after original fingerprints. The
five code stages report unchanged dumpbin identity and no failures. Their
recorded reads and executions are historical acquisition facts, not actions
performed while writing this note. Streaming fingerprints use 64 KiB buffers;
they are separate from semantic byte totals and do not establish an atomic
filesystem snapshot. The reports explicitly exclude target execution, backend
startup, installation and permission escalation.

| Saved report | SHA256 |
| --- | --- |
| [PE entry metadata](offline-game-crt-entry-metadata/metadata.json) | `ccc2b7859049df05a285bc37b49ca2cbeed61959b458c95e33191ab3834a01ac` |
| [Entry fragment](offline-class-static-crt-entry/inspection.json) | `128d84bec2848305b845e7e3206121e685f9fe64b001138f195c0c98e7081e34` |
| [Dispatch fragment](offline-class-static-crt-dispatch/inspection.json) | `0c62ba0c1b3bce951b89b26e77a6c2461c0581cec506508d9834e30a13a5738f` |
| [Reason selector](offline-class-static-crt-reason/inspection.json) | `08ded9b6ff52c160c3699f4c369f0c1420151772313f615f39bbff1818f557e7` |
| [Attach fragment](offline-class-static-crt-attach/inspection.json) | `fa0c4a5b7a5d1a2ccb3ed3a35d4a2c98a1192d4fc79cafae9c63a28eaa71366c` |
| [Two fixed jump thunks](offline-class-static-crt-thunks/inspection.json) | `7a776c85892b5583a068ff2fe9d96935825614f80d12a7706e2648b50488c4c1` |
| [Fixed CRT import metadata](offline-game-crt-imports-metadata/metadata.json) | `675999fee760e0a6b84e881818942b25950d1e4c563f2c6964fd13fa25c4ed2b` |

The optional-header `AddressOfEntryPoint` field at file offset 312 is exactly
`b4 b2 5d 00`, giving RVA `0x5DB2B4`. The pinned `.pdata` entry has
`BeginAddress=0x5DB2B4`, exclusive end `0x5DB2F1`, and unwind-info RVA
`0x7DEA70`. The metadata-only observation initially identified a PE entry,
**not** a CRT function name or an initializer array.

| Instruction archive | Explicit range, exclusive end | Bytes | Instructions |
| --- | --- | ---: | ---: |
| [Entry](offline-class-static-crt-entry/5db2b4-instructions.json) | `0x1805DB2B4..0x1805DB2F1` | 61 | 18 |
| [Dispatch](offline-class-static-crt-dispatch/5db168-instructions.json) | `0x1805DB168..0x1805DB25D` | 245 | 84 |
| [Reason selector](offline-class-static-crt-reason/5daf68-instructions.json) | `0x1805DAF68..0x1805DAFB8` | 80 | 25 |
| [Attach](offline-class-static-crt-attach/5dafb8-instructions.json) | `0x1805DAFB8..0x1805DB0E3` | 299 | 72 |
| [First table-call thunk](offline-class-static-crt-thunks/5dc3e0-instructions.json) | `0x1805DC3E0..0x1805DC3E6` | 6 | 1 |
| [Second table-call thunk](offline-class-static-crt-thunks/5dc3da-instructions.json) | `0x1805DC3DA..0x1805DC3E0` | 6 | 1 |
| Total | Six saved dumpbin invocations | **697** | **201** |

The four larger ranges are exact unwind fragments, not a claim of complete
exception/unwind behavior. The two thunks were explicit six-byte prefixes
without exact unwind entries; neighboring functions were not substituted.
The recorded semantic metadata total is
`184 + 168 + 168 + 168 + 180 + 180 + 104 = 1152` bytes. This excludes fingerprints
and unmeasured dumpbin internal metadata I/O. No initializer-table payload bytes
are included.

## Reason 1 route and its gates

The labels entry/dispatch/reason/attach describe observed control flow. They do
not assign recovered source-level names to the private functions.

1. **PE entry `0x1805DB2B4`.** It saves incoming `RCX`, `EDX`, and `R8`.
   `0x1805DB2CB..CE` compares `EDX` with 1; only that reason calls
   `0x1805DBB6C` at `0x1805DB2D0`. After this call returns, the saved original
   arguments are restored and `0x1805DB2EC` tail-jumps to `0x1805DB168`.
   The body/role of `5DBB6C` is not established by this batch.
2. **Dispatch `0x1805DB168`.** For reason 1, the unsigned check at
   `0x1805DB19D..A3` enters the attach-side route. The call at
   `0x1805DB1A5` reaches `0x1805DB260`; a zero `EAX` at `1B0..B2` exits
   before the next stage. With a nonzero return, the original three arguments
   are restored and `0x1805DB1C0` calls `0x1805DAF68`. Its zero result also
   exits at `0x1805DB1CB..CD`.
3. **Reason selector `0x1805DAF68`.** Input `EDX=1` takes the branch at
   `0x1805DAF73` to `0x1805DAF9D`. It moves the original third argument
   `R8` into `RDX`, restores its frame, and tail-jumps from `0x1805DAFA4`
   to `0x1805DAFB8`. Thus this observed attach target receives the original
   `RCX` and original `R8` in `RCX/RDX`; it does not receive reason 1 in `EDX`.
4. **Attach `0x1805DAFB8`.** It saves these arguments in `R14/RSI`.
   `0x1805DAFD5` calls `0x1805DAC64` with `ECX=0`; a false `AL` returns
   zero. It then calls `0x1805DAAF4` and preserves that `AL` in `BL`.
   A nonzero DWORD at `0x180A65C98` causes a call to `0x1805DBA18(7)`
   at `0x1805DB001`; the semantics of that helper are not assumed.
   The observed continuation writes state 1 at `0x1805DB006`.

Reaching the table calls is **conditional**, not guaranteed merely by the PE
entry receiving reason 1. At `0x1805DB010`, `0x1805DAB64` must return true.
The path then calls `5DBC60`, `5DAEB8` with `RCX=0x1805DBCAC`, `5DA72C`,
`5DAEB8` with `RCX=0x1805DA73C`, and `5DBC3C` before the first table call.
Their presence/order is observed; this note does not invent their side effects
or recursively recover their bodies.

After the first table call, nonzero `EAX` branches to the failure-side cleanup
at `0x1805DB080`. Zero proceeds to `0x1805DAB30` at `0x1805DB057`; its
`AL` must also be true to reach the second table call. After that call, the
code writes state 2 to `0x180A65C98` at `0x1805DB073` and clears `DIL`.
The common path calls `5DAE18` with the saved `BL`; nonzero `DIL` returns zero.

The success continuation contains another conditional indirect call at
`0x1805DB0C0`, following helpers `5DBC58`, `5DAD7C`, and `5DB8B8`.
Its target is not assigned a symbol or class-registration role here. The
visible tail increments DWORD `0x180A65CDC` at `0x1805DB0C2` and returns 1.
Back in the dispatch fragment, a successful reason-side result reaches
`0x1805DBC18` at `0x1805DB1D7`. A reason-1 zero result from that later call
enters the visible reason-0 cleanup calls at `1F3`, `200`, and `20D`.
Those conditions prevent a claim that this fragment alone proves successful
whole-DLL initialization or its failure/exception cleanup.

## Table arguments and now-confirmed import bindings

| Call site | RCX start argument | RDX end argument | Transfer and static import identity |
| --- | --- | --- | --- |
| `0x1805DB04E` | `0x18060C880`, set at `5DB047` | `0x18060C890`, set at `5DB040` | `5DC3E0`: `FF 25 5A F5 02 00` → qword slot `0x18060B940` → `_initterm_e` |
| `0x1805DB06E` | `0x18060BAB8`, set at `5DB067` | `0x18060C878`, set at `5DB060` | `5DC3DA`: `FF 25 68 F5 02 00` → qword slot `0x18060B948` → `_initterm` |

The binding claim is supported by the **subsequent saved import report**, not
inferred from a familiar CRT code pattern. Descriptor 6 names
`api-ms-win-crt-runtime-l1-1-0.dll`; its `FirstThunk` RVA is `0x60B920`.
Verified OFT entries 4 and 5 are RVAs `0xA45400` and `0xA45408`, pointing
to hint/name records at `0xA57A20` (`_initterm_e`, hint 55) and
`0xA57A14` (`_initterm`, hint 54). Adding their respective eight-byte indices
to `FirstThunk` gives exactly the slots addressed by the saved jump bytes.
Only 104 bytes of fixed import metadata were read; IAT payload contents,
imported CRT code, runtime resolution and neighboring imports were not read.

The first start/end argument difference is `0x10` (16 bytes, two qword
widths). The second is `0xDC0` (3520 bytes, 440 qword widths). These are the
exact parameter spans and arithmetic sizes, **not counts of observed nonnull
initializers or successful registration calls**. Their table payloads have
not been inspected at this draft boundary.

[Microsoft's `_initterm` / `_initterm_e` reference](https://learn.microsoft.com/en-us/cpp/c-runtime-library/reference/initterm-initterm-e?view=msvc-170)
documents start/end table pointers and skipping null entries during traversal.
It gives the void versus int-returning signatures; the caller's explicit
`EAX` check independently establishes the first call's gating role here.
This is supporting external CRT contract information, not an observation
that Game's initializer entries have executed. No CRT function or DLL is
called as part of this work.

## What remains before Item / QuestItem writer attribution

The next evidence boundary is the contents of the **already identified,
fixed table parameter spans**, followed only as separately authorized.
Pointer order, null entries, and precise source slots must remain recorded;
this phase does not scan all `.rdata`, infer a `.CRT` section by name, or
automatically follow every nonnull pointer.

An Item or QuestItem registration claim requires an actual selected writer's
instructions/data linking the class name and metadata node to its factory
pointer and parent/child registration. The present CRT route supplies neither
those writer bodies nor the actual ClassInfo child-array population/order.
An import name, table address, nearby function or exported class name alone
cannot provide that missing link.

Even after a writer is found, the Engine `0x180274EC0` rules in
[class-factory evidence](NATIVE-REGISTRATION-EVIDENCE.md) still apply: the first
matching node tail-transfers through `+0x28`; continuation at its parent
depends on the returned object's null/non-null result. Static target
identification does not execute a constructor or prove final dispatch,
initialized object state, registration, or the concrete `Read` vslot target.
No synthetic callback, empty object, or repeated Class getter is used to
replace these obligations.

This draft is frozen with table contents and Item / QuestItem registration
writers unresolved. The parent phase may append separately sourced findings.

## Appended evidence: selected Item and QuestItem initializer writers

This appendix supersedes only the preceding draft's statement that these
selected table entries and writer fragments were unresolved. The original
11096-byte draft is preserved byte for byte (SHA256
`52809ef901a3b7545a4be50fabf6fcc2f579adb2d206afd49ab2102b90f571be`).
The appendix was prepared by reading saved project evidence only. No original
DLL, imported function, initializer, constructor or getter was executed or
reread for this documentation update; no backend, build or test was run.
The earlier 2282-assertion / 83-scenario scalar checkpoint remains historical.

### Frozen sources and the selected initializer table slots

All addresses below are preferred VAs in the same Game image already pinned
above: SHA256
`cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`.
The source reports record matching before/after image hashes; this appendix
checks the report hashes, without repeating their original-file operations.

| Saved evidence | SHA256 |
| --- | --- |
| [Initializer table metadata](offline-game-class-init-table/metadata.json) | `e347eb6fa9647ca63cfa48fe0deb60320f0d8e3f6921e18269a6ee560a658c8b` |
| [Item prefix](offline-class-static-table-batch-4/5ef0-instructions.json) | `d65d8ac724d67af4a25ea5f283dad8b2f6397c6906d961e64e610cc4baae95d1` |
| [Item prefix inspection](offline-class-static-table-batch-4/inspection.json) | `b792b1858bdeba01dbf1b76e2fd9ead2d8d2576b407dcc5d28203a03829b08fe` |
| [QuestItem prefix](offline-class-static-table-batch-7/9e10-instructions.json) | `c92322c0a51d234f9868b63bea8e076b685f2ba587ea9697ea5ec50999c87ba2` |
| [QuestItem prefix inspection](offline-class-static-table-batch-7/inspection.json) | `0074c19190c7ef08f13c6897153964ecb6b159d1ab1bb10b1355113cb5987871` |
| [Exact import and literal bindings](offline-game-class-init-data/metadata.json) | `652dc44ea2bc1183cc797bd4b3419a87a6fa5ff3570e216df634273ac204549c` |
| [Four writer continuations](offline-class-static-writer-continuations/inspection.json) | `5c5498246f2d2b4ed14cb5cb12c0e8238b167c4320c642007e8f7f302756ecbf` |

The saved initializer table is the previously identified `_initterm` range
`[0x18060BAB8, 0x18060C878)`: 3520 bytes, 440 qword slots, one null slot
and 439 distinct non-null targets. Its table-byte SHA256 is
`771321f54306a577cec8b5dd32fa4d7e0636492204aaf7429463de497237391a`.
The metadata report records 17144 semantic metadata bytes, including the
bounded table and boundary lookups, and zero interpreted code bytes.

| Zero-based slot | Slot VA | Stored target | Selected prefix range, exclusive end |
| --- | --- | --- | --- |
| 115 | `0x18060BE50` | `0x180005EF0` | `[0x180005EF0, 0x180005F29)`, 57 bytes |
| 205 | `0x18060C120` | `0x180009E10` | `[0x180009E10, 0x180009E3B)`, 43 bytes |

These are actual saved table pointers and their relative table positions.
They establish conditional traversal order if the earlier reason-1 path
reaches and completes those entries. They do not establish that the DLL was
initialized, that all intervening initializers completed, or the complete
runtime order of a parent's children.

### Import and class-name bindings

The fixed data resolver read 179 semantic metadata bytes, including the
15 literal bytes below. It read neither IAT payloads nor imported code.

| Instruction/data source | Verified binding |
| --- | --- |
| IAT `0x180609EB0` | `Engine.dll`, descriptor 3 / thunk 470, `?classInfo@Actor@GAME@@1VRTTI_ClassInfo@2@B` |
| IAT `0x18060BA38` | `api-ms-win-crt-string-l1-1-0.dll`, descriptor 9 / thunk 2, `_strdup` |
| Literal `0x18066A34C`, used at `0x180005EFD` | `49 74 65 6D 00`, `Item` plus NUL, 5 bytes |
| Literal `0x18066A9C8`, used at `0x180009E16` | `51 75 65 73 74 49 74 65 6D 00`, `QuestItem` plus NUL, 10 bytes |

The Actor binding uses OFT RVA `0xA43970` and hint/name RVA `0xA4BBF8`;
the `_strdup` binding uses OFT RVA `0xA454F8` and hint/name RVA `0xA57912`.
This is an on-disk import-identity result. It does not supply runtime IAT
values or prove a non-null result, allocation success, ownership/lifetime,
exception behavior or the memory contract of `_strdup`.

### Prefix write sets and established node identities

For Item, `0x180005EF6` loads the qword through the verified Actor classInfo
IAT slot. `0x180005F04` stores that value at `0x180A5C8B0`.
`0x180005EFD` supplies the Item literal in RCX, and `0x180005F0B` calls
through the verified `_strdup` slot. `0x180005F18` stores the actual returned
RAX at `0x180A5C8A8`, without a preceding null-result check in the saved flow.
`0x180005F11` reloads the parent pointer from `0x180A5C8B0`; the test/branch
at `0x180005F1F` / `0x180005F22` skips the parent-update block when it is zero.
The nonzero branch reads parent DWORD `+0x18` at `0x180005F24` and increments
EAX at `0x180005F27`.

For QuestItem, `0x180009E16` supplies its literal in RCX and `0x180009E1D`
calls the same verified `_strdup` import. `0x180009E2A` stores returned RAX at
`0x180A5C158`. The parent pointer is loaded from `0x180A5C160` at
`0x180009E23`; `0x180009E31` / `0x180009E34` test it and skip the parent-update
block when zero. The nonzero branch reads parent DWORD `+0x18` at
`0x180009E36` and increments EAX at `0x180009E39`. These selected instructions
do not write or resolve the value initially stored at `0x180A5C160`.

The continuations independently identify the appended self addresses:
Item `0x180A5C8A0` at `0x180005F7F`, QuestItem `0x180A5C150` at
`0x180009E8F`. Consequently, the absolute prefix writes above identify
name-result storage at each self `+0x08` and parent-pointer storage/read at
self `+0x10`. This is supported by the actual self operands, not by inferring
a node base from proximity alone. No factory-slot `+0x28` initialization is
present in these selected fragments.

### Normal-flow parent update, with helper semantics left explicit

The continuation report contains four fixed fragments, 268 code bytes and
66 decoded instructions. The bounds are unwind fragments; they do not imply
complete exception-path coverage. No callees were followed in that batch.

| Fragment JSON | Range, exclusive end | Bytes | JSON SHA256 |
| --- | --- | --- | --- |
| [Item parent update](offline-class-static-writer-continuations/5f29-instructions.json) | `[0x180005F29, 0x180005F9F)` | 118 | `207d11ee1089aa497e239d36a3318f80a671beef888b0593cfe02130543b2289` |
| [Item tail](offline-class-static-writer-continuations/5f9f-instructions.json) | `[0x180005F9F, 0x180005FB0)` | 17 | `1e941e6d621670a04e84f460fbef1ecd7238155cc7084055a9cd166ead22220a` |
| [QuestItem parent update](offline-class-static-writer-continuations/9e3b-instructions.json) | `[0x180009E3B, 0x180009EAF)` | 116 | `8b19b190cbee2ecba2db6406a2a7847279b6c966da28e6200e6266f2ab3cbc0b` |
| [QuestItem tail](offline-class-static-writer-continuations/9eaf-instructions.json) | `[0x180009EAF, 0x180009EC0)` | 17 | `1a31b668f86697359530516a175bcfe64208e96fdca3b9c2aa50d3f36f70c928` |

For a valid, stable non-null parent, nonnegative non-overflowing count and
normal helper returns providing valid storage, both saved flows perform the
following operations in this order. Those conditions describe the static
normal path; they do not assert that its helper contracts or failures have
been reconstructed.

| Operation | Item instruction(s) | QuestItem instruction(s) |
| --- | --- | --- |
| Sign-extend the already incremented EAX, multiply by 8; select all-ones on multiplication overflow; pass result in RCX to `0x1805DAF40` | `0x180005F2E` through `0x180005F47` | `0x180009E40` through `0x180009E59` |
| Preserve the helper's returned pointer in RDI | `0x180005F4F` | `0x180009E61` |
| If signed parent count is positive, copy old child qwords from parent `+0x20` in increasing index order | `0x180005F52` through `0x180005F79` | `0x180009E64` through `0x180009E89` |
| Sign-extend the current parent count and store self at that index in the returned storage | `0x180005F7B`, `0x180005F7F`, `0x180005F86` | `0x180009E8B`, `0x180009E8F`, `0x180009E96` |
| Increment parent DWORD `+0x18` | `0x180005F8A` | `0x180009E9A` |
| Load the old parent `+0x20` pointer into RCX and call `0x1805DA5DC` | `0x180005F8D`, `0x180005F91` | `0x180009E9D`, `0x180009EA1` |
| Publish the returned-storage pointer from RDI into parent `+0x20` | `0x180005F96` | `0x180009EA6` |
| Load the fixed argument address into RCX, restore the frame, then tail-jump to `0x1805DAEB8` | `0x180005F9F` gives `0x180604450`; jump at `0x180005FAB` | `0x180009EAF` gives `0x180605530`; jump at `0x180009EBB` |

In the normal arithmetic domain the size passed to `0x1805DAF40` is
`8 * (oldCount + 1)`. The actual prefix uses a 32-bit EAX increment before
sign-extension; overflow, negative counts and invalid parent/storage cases
must not be replaced with this simplified normal-domain expression.
The copy loop rereads the parent count and old pointer; the stable-parent
condition matters when describing it as copying exactly the old child list.

The parent-count increment precedes the call with the old pointer, and the
new parent child-array pointer is published only after that call returns.
This ordering is evidence of a possible intermediate write set, not an
atomic update or a proved rollback contract. The null-parent branch skips
the entire parent-update block but still reaches the corresponding tail.
The selected instructions do not justify naming `0x1805DAF40` an allocator,
`0x1805DA5DC` a free routine or `0x1805DAEB8` an atexit routine. Likewise,
`0x180604450` / `0x180605530` are the observed argument addresses; their
callback behavior has not been established here.

### Updated conclusion and remaining boundary

The saved CRT table now connects the conditional reason-1 traversal to two
concrete writer entries. The exact literals, imported names and continuations
establish Item/QuestItem name-result stores and their conditional self-append
operations on a parent child array. In particular, Item's parent operand is
sourced from the Actor classInfo import, and the append preserves the existing
child order before placing self at the current count.

This does not yet establish QuestItem's stored parent value, a complete graph
reachable from the Engine ClassInfo root `0x18041CF00`, the initial contents of
all participating nodes, all other writers, either node's factory `+0x28`
value, or final duplicate-name dispatch. The child order within one completed
append is established; the full runtime tree and factory-return-dependent
search outcome are not. The direct helper bodies, failure/exception paths and
fixed tail-argument behavior are also outside this evidence closure.

There is therefore no claim of an executed initializer, complete native
factory, constructed concrete Item/QuestItem, concrete `Read`, or completed
CreatePartial/FinishPartial lifecycle. Product code, tests, acceptance/status
artifacts and previous validation results are unchanged by this appendix.
Further findings can be appended separately with their own provenance.
