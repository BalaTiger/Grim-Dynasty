# Archive initialization: fresh normal state and first-load boundary

Date: 2026-10-20.

The new fixed captures and previously saved helper bodies now establish the archive's explicit map, selector, offset, string-index, and query-cache initial state on normal successful construction. They also establish why one successful load into that fresh archive has one zero offset and skips record remapping. These are static conclusions under the contracts below, not evidence that the original game was run or that the complete official-record cache/factory chain passed.

Writing this document read saved project evidence only. It did not read the original DLL, run dumpbin or a backend, execute tests, or change production code.

## 1. Captures and identity

All preferred VAs below have image prefix `0x180`. Both new batches report this same original and existing-copy SHA256 before and after:

`4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`

The original recorded by the inspections is `D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll`; the existing project copy is `.tooling/offline-cache-node/Engine.readonly.dll`. The fixed installed dumpbin SHA256 is:

`cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815`

| Half-open captured range | Code bytes | Instructions | Saved instructions |
| --- | ---: | ---: | --- |
| `27F9C0..27FB1B` | 347 | 70 | [archive constructor](offline-archive-init/27f9c0-instructions.json) |
| `2800D6..280143` | 109 | 30 | [load continuation](offline-archive-init/2800d6-instructions.json) |
| `280143..28015A` | 23 | 7 | [load return](offline-archive-init/280143-instructions.json) |
| `27FF20..27FFA4` | 132 | 39 | [region acquisition, including complete fresh branch](offline-archive-init/27ff20-instructions.json) |
| `02D950..02D9DC` | 140 | 44 | [offset append](offline-archive-init/02d950-instructions.json) |
| `03C740..03C7B2` | 114 | 31 | [string-index constructor](offline-archive-container-init/03c740-instructions.json) |
| `05D860..05D8D2` | 114 | 31 | [record-index/container constructor](offline-archive-container-init/05d860-instructions.json) |

The first [batch record](offline-archive-init/inspection.json), SHA256 `1c523cfa4b1d6c68e28fef23bac2b8397d9a0dc9352b2f006d8053d25a5a728b`, reports 751 code bytes, 190 instructions, and 748 PE metadata bytes. The second [batch record](offline-archive-container-init/inspection.json), SHA256 `6a84fcd4a292a97188761dc32215bb757bff73f84dd0ad1a532c9a2c3c697b51`, reports 228 code bytes, 62 instructions, and 712 PE metadata bytes. Every selected byte is covered by contiguous decoded instructions and matches its saved original slice. Both batches report unchanged original/copy/tool hashes, zero followed callee bodies, no target execution, and no backend.

The combined new body scope is **979 code bytes and 252 instructions**, across seven fixed dumpbin calls. This total excludes metadata and streaming whole-file hash reads. Controller peak RSS was 43,012 KiB and 42,424 KiB respectively; those figures are not total machine or native-game memory use.

Frozen boundary records:

- [Five fragments](offline-archive-init-boundaries/boundaries.json): SHA256 `fa7b87825a775fdfd48d51c42efc95ccbbdb3fb8d19c6bf972b7935054f034ee`.
- [Two constructors](offline-archive-container-init-boundaries/boundaries.json): SHA256 `b4203d5bb129602d42d2ab1bd591ef6bd7674be7e4188814915652b0441fe32e`. This record validates the three direct CALLs against all 347 already-saved constructor bytes before locating the two PE unwind entries.

A PE unwind entry is a fragment boundary. In particular, the new `27FF20` capture contains a complete fresh branch and only a prefix of the nonfresh branch; this document does not claim the whole function is closed.

## 2. Construction provenance and normal contracts

The [saved LoadMainDatabase body](../phase-1/round-4/followup/Engine-function-evidence-bounded-v2.json), SHA256 `cbae1984329cb2a490d5ea9224543535776ae518fc6f3fff89c7c97108e573bb`, entry `$[0]`, is `?LoadMainDatabase@Engine@GAME@@AEAAXXZ`.

At `073AAE` it requests `0x1D0` bytes, calls allocation helper `2DC398` at `073AB3`, passes the returned pointer in RCX at `073ABC`, calls `27F9C0` at `073ABF`, then publishes the result in `Engine+3D0` at `073AC5`. The first `074280` load call is at `073B13`; later calls are conditional on bytes `Engine+C25/C26/C27/C21`. A one-load model therefore means the state after the first successful load into the newly constructed archive, before any later load. It is not a claim about the machine's expansion configuration or the entire LoadMainDatabase invocation.

Let `A` be the archive and `C=A+88` the string-index context. The supported normal domain uses exclusive valid storage, successful nonnull allocations, successful ordinary copying/releasing, valid input lengths, and no concurrent mutations or unobserved aliases. Native OOM, exception unwinding, malformed pointers, and rollback are outside this domain. Native addresses may be represented by owned identities in a logical model; their numeric values must not be invented.

## 3. Container constructors and sentinel proof

`27F9C0` calls `05D860(A+48)` at `27F9F3`, `03C740(C)` at `27FA08`, and `05D860(A+108)` at `27FA3E`. The first `05D860` instance is the record-ID index used by the saved lookup/record-load bodies. The business role of the second instance at `A+108` is not established here.

Both constructor bodies:

1. Save the incoming container pointer in RDI.
2. Zero its first DWORD, sentinel pointer at `+8`, and node count at `+10`.
3. Pass `RDX=0` and `R8=0` to their respective node helpers.
4. Store the returned sentinel in `+8`.
5. Zero bucket vector begin/end/capacity pointers at `+18/+20/+28`.
6. Write `0x3F800000` at `+0`, establishing binary32 `1.0`.
7. Call `01B4F0(container,8)` and return the original container pointer.

There is no intervening CALL before the node helper and RCX still holds the incoming container at `03C779/05D899`. More decisively, both saved node helpers overwrite ECX with an allocation size before using the allocator; they do not consume the incoming RCX as a container or infer links from it.

| Helper | Existing complete evidence | Normal nonnull result for `RDX=0,R8=0` |
| --- | --- | --- |
| `01F170` | [offline instructions](offline-cache-node-inspection/01f170-instructions.json), `01F170..01F1B4`, SHA256 `d6ee428d3e56c754b9ba28c127c75f98ec382a55ccd2453d3d88e3a1e6b0c0e1` | `01F17A` requests 56 bytes; `01F18F/192` select the new node for both links; `01F19A/01F1A6` write `node.next=node.prev=node`. |
| `020960` | [saved assembly](rea-container-support/05-Container-support-0x180020960-assembly.json), through `0209A3 RET`, SHA256 `91a6e34b435b9b0a746483085fb8ad8ea173a8545e5d862792bf270fa812020f` | `02096A` requests 32 bytes; `02097F/982` select self; `02098A/996` write the two self links. |

Thus the self-linked sentinel is supported by instructions, not assumed from an STL resemblance. Neither helper establishes zeroed payload or padding, increments count, or makes null allocation a safe return. The tests involving `RAX+8` are not a valid null-allocation recovery contract. The older failed Rea query for `01F170` is historical; the later offline body supersedes that absence.

The already-saved [01B4F0](rea-registration-containers/11-Container-whole-chain-followup-0x18001b4f0-assembly.json), [01BFB0](rea-container-support/11-Container-support-0x18001bfb0-assembly.json), [01BD30](rea-container-support/08-Container-support-0x18001bd30-assembly.json), [021740](rea-container-fill/05-Container-fill-0x180021740-assembly.json), [01EF70](rea-container-fill/08-Container-fill-0x18001ef70-assembly.json), and [022F00](rea-container-reserve/05-Container-reserve-0x180022f00-assembly.json) cover the fresh endpoint-vector path under ordinary allocation/copy contracts. Starting from null vectors, request 8 reserves 16 qword endpoints, fills each with the sentinel, and stores mask 7 and bucket count 8.

The normal returned state of each of these three containers is therefore:

| Container-relative field | Known normal value |
| --- | --- |
| `+0` DWORD | `0x3F800000` (`float 1.0`) |
| `+8` | Owned nonnull sentinel `S`, with `S.next=S.prev=S` |
| `+10` qword | 0 nodes |
| `+18` | Endpoint storage `B` for 16 pointers |
| `+20/+28` | `B+128` |
| `+30/+38` qword | mask 7 / bucket count 8 |
| Endpoint values | All 16 are `S` |

This establishes 8 logical buckets and capacity for 8 buckets, not 16 logical buckets. No payload/padding value is inferred.

## 4. Explicit archive state after successful `27F9C0`

These stores occur in the captured outer constructor; container-dependent values below additionally use section 3.

| Field | Final normal value and decisive instruction |
| --- | --- |
| `A+0` | Vtable address `1803488C8`, `27F9E7`; no ABI-use claim follows. |
| `A+8` DWORD | 2, `27FAE2`. |
| `A+C..1B` | Zero from the two qword stores at `27FB00/04`. |
| `A+20/+28` | Null data-region pointer / zero length, `27FA9E/A2`. |
| `A+2C..43` | Six zero header DWORDs, `27FA92/96/9A`. |
| `C+40/+48/+50` | Null string-pool begin/end/capacity, `27FA0F/13/17`. |
| `C+58` DWORD | Query-address-cache shift 2. The qword store of 2 at `27FA1B` also sets `C+5C` bucket count to 0. |
| `C+60/+64/+68` DWORD | Cache mask/capacity/used all 0, `27FA23/27`. |
| `C+70/+78` | Null cache bucket/entry pointers, `27FA2A/2E`. |
| `A+148/+150/+158` | Zero qwords, `27FA44/4B/52`; no additional business meaning assigned. |
| `A+160` | Record-index sentinel `S48`, because `27FAE9..F0` writes `[A+50].next`. The earlier zero store at `27FA59` is overwritten. |
| `A+168` byte | 0, `27FAF7`. |
| `A+170/+178` | Null appended map / zero DWORD count, `27FAA5/AC`. |
| `A+180` | Null, `27FAB2`. |
| `A+188` | Result of the `0x2800`-byte scratch allocation, `27FAC6..D0`; nonnull is a normal-allocation premise. |
| `A+190/+194` DWORD | Capacity `0x2800` (10,240) / length 0: the qword store at `27FAD7` proves both. |
| `A+198` DWORD | Latest append base 0, `27FA8A`. |
| `A+1A0/+1A8/+1B0` | Null selector-vector begin/end/capacity, `27FA60/67/6E`. |
| `A+1B8/+1C0/+1C8` | Null offsets-vector begin/end/capacity, `27FA75/7C/83`. |

Unlisted gaps and padding remain unspecified. This is not a zero-initialization claim for the entire `0x1D0` allocation.

## 5. First-load flag, header mutation, and call order

Combine the [older `280000` prefix](../resource-contracts/followup/Engine-function-evidence-bounded-v2.json), entry `$[2]`, with the new `2800D6` and `280143` fragments. The combined saved instructions now cover the relevant returns and calls through `280159 RET`.

At entry, `280013/27` tests the old `A+2C`; `R14B` initially equals 0. The fresh constructor proves that old header DWORD is zero. This selects the first-load branch and leaves `R14B=0`. An existing nonzero header instead uses a stack header and sets `R14B=1` at `2800CC`.

The new continuation proves:

1. `2800E1`: `280160(A,header,input,inputLength)` acquires/copies a data region.
2. Only for a nonzero append flag, `2800EE` stores the returned region in `A+180`.
3. If the region is nonnull, `2800FD/28010B` passes `R14B` as the fifth argument to `280960`.
4. `280113..126` passes the same flag as the sixth argument to `280220`, with the region pointer as its fifth argument.
5. `280137` calls `2809F0` afterward.

The [saved `280960` body](../resource-contracts/database-ownership/Engine-function-evidence-bounded-v2.json), entry `$[1]`, checks that fifth argument at `2809AF`: zero calls `283A80(C)` at `2809C7`; nonzero calls append importer `2805B0(A)` at `2809B9`. Both then call `280AB0` at `2809CF`. The record loader `280220` likewise selects its raw ID read at `280318` when the sixth argument is zero, bypassing its appended map lookup.

This closes the flag provenance for a fresh first load. It does not validate every possible header. In particular, `280033` writes the incoming first DWORD to `A+2C` **before** the version/flag checks. A rejected nonzero header can remain in the archive. A later retry could therefore take the append branch. Do not move validation ahead of mutation and describe the result as native order, or retry a failed load as though construction had just occurred.

Also, `2800F8` can skip all three table calls when region acquisition returns null, then `280141` sets `AL=1`. Hence a true return from `280000` alone is not proof that region acquisition, strings, or records completed. The saved outer `074280` caller does not use that return value as a complete-import check.

## 6. First region, one zero offset, and the no-remap guard

For this normal path require a readable complete input, record-table offset greater than 24 and at most input length, no wrapped size/range arithmetic, and successful region allocation/copy. Let `D=recordTableOffset-24`.

In [280160](../resource-contracts/database-ownership/Engine-function-evidence-bounded-v2.json), entry `$[0]`, the output offset starts at zero and `280191` calls `27FF20(A,D,&offset)`. The new helper body proves:

- A zero request returns null at `27FF3C..4E`.
- For fresh `A+20=null` and `A+28=0`, positive `D` takes `27FF5D..87`.
- It allocates `D`, writes the result to `A+20`, writes `D` to `A+28`, explicitly writes offset 0 at `27FF6C`, and returns `A+20` at the already-captured `27FF87 RET`.
- The nonfresh branch starting at `27FF88` is not complete in this capture. It is unnecessary for the fresh domain and remains outside it.

Under the saved copy guards, `280160` copies the data region from `input+24`. Since offset is zero, it then takes `280208/280210`, calling `02D950(A+1B8,&zero)`. The offsets vector is still empty from construction.

The new `02D950` body plus the already-saved [02DE40](offline-binary-bits-words-reserve/02de40-instructions.json) and [02E0F0](rea-binary-parser-vector-growth/05-Parser-vector-growth-0x18002e0f0-assembly.json) establish the normal fresh append without a new helper query:

1. The stack offset does not alias the owned vector; `02D950` takes `02D9AC`.
2. Null end equals null capacity, so `02D9BA` requests one DWORD.
3. Fresh `02DE40` selects capacity 1 and tail-calls `02E0F0`.
4. Ordinary successful allocation and zero-length movement yield a 4-byte storage block, end at begin, capacity-end at begin+4.
5. `02D9CA` writes DWORD 0 and `02D9CC` advances end by 4.

The resulting logical offsets vector is exactly **`[0]`, count 1**. Its pointer identity is allocated, not numerically predetermined. The selector vector remains empty, latest base remains 0, and the appended map stays null with count 0 on the initial-import path.

The [saved `280E20` fetch evidence](ARCHIVE-FETCH-EVIDENCE.md) tests offset count at `280F49/4D`. Count at most 1 goes directly to output publication and does not call `280870`. Thus the first successful fresh load now has a supported reason to skip remapping; this conclusion no longer rests merely on the fact that the caller supplied one ARZ.

The guard occurs after decoding and the `A+194` length write. Metadata preflight in owned code may be safer, but it is a declared policy, not the native execution order.

## 7. Initial strings and address-cache state

[Existing import evidence](ARCHIVE-STRING-MAP-EVIDENCE.md) still governs raw byte equality, duplicate omission, first-occurrence target IDs, reusable temporary-string behavior on failed length checks, and partial effects.

The constructor now proves the initial query-cache shift. `283A00` clears cache count/mask/capacity/used and releases/nulls its pointers while preserving `C+58=2`. `283A80` then calls `284080(C+58,trunc(N/4),trunc(N/2))`, where N is the original raw count.

The [saved `284080` body](rea-binary-parser-helpers/09-Parser-helper-0x180284080-assembly.json) and [log binding](offline-binary-parser-log/log-import-and-multiplier.json) can be reused under their stated normal numeric/allocation contract. For N=82,999, request is 20,749 and capacity is 41,499; a normal accurate finite log calculation gives 16,384 buckets, mask 16,383. Bucket storage is 131,072 bytes; entry storage is 995,976 bytes. The helper zeroes all buckets but only `24*request=497,976` bytes of entry storage; unused capacity must not be declared wholly zero. Used remains zero from clear.

Post-import `280AB0` actually performs empty-name lookup through `283840`, so address-cache work is part of native initialization even when a later probe never invokes a getter. Under the now-established fresh cache, valid corresponding pool IDs, and successful empty-string insertion if required, its successful lookup can use the saved [284160 normal insertion branch](rea-binary-cold-insert/05-Binary-cold-insert-0x180284160-assembly.json). For the stated positive capacity, the first inserted query-cache entry uses slot 0, increments used to 1, and records the empty-literal query address and ID.

The actual query address is a runtime identity; the preferred static VA is not an observed ASLR address. A logical implementation may expose an owned address token while retaining the observed bucket arithmetic and identity semantics.

Initial deduplication still does not generate an old-ID map. Preserving original source IDs requires the complete source pool's uniqueness/order premise or different explicit evidence. This initialization result does not authorize cropping the 82,999-entry pool to a record's referenced subset or disabling the existing duplicate-ID rejection.

## 8. Failure prefixes and remaining boundaries

The normal fresh-state projection is supported; it is not a transaction or a complete runtime implementation.

| Later stop | Earlier effects that cannot be silently erased |
| --- | --- |
| Container or scratch allocation | Earlier container construction, sentinel publication, or archive field stores may already exist. Scratch capacity is written even though a null allocation is outside the supported domain. |
| Header validation | The first header DWORD may already have replaced `A+2C`. |
| Region allocation/copy | A successful allocation has published `A+20/+28`; native allocation failure and null handling are not modeled as atomic rollback. |
| Offset append | Region data has already been copied. |
| String import | Header and offset state exist; initial clear and earlier string/node/pool effects may already be committed. |
| Cache initialization or empty-name lookup | String indexing may be complete while address-cache initialization remains incomplete. |
| Record import | Strings and offsets are already established; prior record rows can have been published. |

There is **no missing sentinel allocation body** for this normal fresh path: `01F170` and `020960` are saved and reused. The three newly inspected constructors can be projected using the saved normal allocation, sentinel, reserve, and fill helpers. No new original query is proposed by this document.

Remaining separate limits are:

- Full native allocator/EH/failure semantics and malformed-state behavior.
- Nonfresh region growth after `27FF88`, repeated archive loads, and arbitrary merge histories.
- Complete source validation/uniqueness and explicit managed budgets for the full pool, name index, pointer cache, and retained snapshots.
- Full record-index population and any behavior beyond the separately supported record-reader/parser/publication contracts.
- Actual TableDepot construction provenance, timing conversion and application-wide lifecycle where not established by separate evidence. Similar container layouts do not by themselves identify a depot instance.
- Native ABI execution, original-runtime comparison, formal official-sample compatibility, complete factory/getter/item construction, and original save compatibility.

The existing parser-only official probe remains a separate result. This document does not add assertions, scenarios, or successful full-chain tests, and it does not relabel a supplied-state test as proof of actual native initialization.
