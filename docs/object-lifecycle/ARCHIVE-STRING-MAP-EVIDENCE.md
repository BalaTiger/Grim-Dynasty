# Archive string indexing and appended-record ID mapping

Date: 2026-10-20.

This document separates the observed native instruction sequence from a bounded, self-owned logical projection. It covers initial string indexing, later input-to-target mapping, and the post-import lookup/insertion path. It does not establish complete native archive initialization, allocator or exception behavior, pointer-cache initialization, or official archive compatibility.

The reviewer read saved project text only. Writing this document did not read an original or copied DLL, run a backend, execute tests, or modify source code.

## 1. Captures and identity

The following nine saved disassembly operations cover eight logical functions; `280490` required two adjacent fragments. Their inspection records report **3,326 selected code bytes and 895 instructions**, exit 0, and complete selected-byte coverage. Full-file hash reads and PE metadata are not included in those code-byte totals.

| Captured half-open range | Bytes / instructions | Inspection and instructions |
| --- | ---: | --- |
| `2805B0..280869` | 697 / 187 | [entry inspection](offline-archive-map-entry/inspection.json), [instructions](offline-archive-map-entry/2805b0-instructions.json) |
| `280490..280532` | 162 / 39 | [reserve inspection](offline-archive-map-reserve/inspection.json), [instructions](offline-archive-map-reserve/280490-instructions.json) |
| `280532..2805A8` | 118 / 35 | [reserve continuation](offline-archive-map-reserve-tail/inspection.json), [instructions](offline-archive-map-reserve-tail/280490-body-instructions.json) |
| `03C8E0..03CA47` | 359 / 110 | [lookup inspection](offline-archive-map-lookup/inspection.json), [instructions](offline-archive-map-lookup/03c8e0-instructions.json) |
| `283A80..283D56` | 726 / 206 | [initial inspection](offline-archive-map-initial/inspection.json), [instructions](offline-archive-map-initial/283a80-instructions.json) |
| `280AB0..280CD9` | 553 / 128 | [post-import inspection](offline-archive-map-publish/inspection.json), [instructions](offline-archive-map-publish/280ab0-instructions.json) |
| `283FF0..28407B` | 139 / 34 | [node inspection](offline-archive-map-node/inspection.json), [instructions](offline-archive-map-node/283ff0-instructions.json) |
| `283A00..283A7C` | 124 / 33 | [clear inspection](offline-archive-map-clear/inspection.json), [instructions](offline-archive-map-clear/283a00-instructions.json) |
| `283840..283A00` | 448 / 123 | [pointer-name lookup inspection](offline-archive-map-emptylookup/inspection.json), [instructions](offline-archive-map-emptylookup/283840-instructions.json) |

Every address in this document is a preferred image VA with prefix `0x180`, unless written in full. The `280490` first fragment alone was not a complete function: the continuation supplies its selector append, count update, and return. A `.pdata` boundary is not by itself proof of logical completeness.

All nine inspection records report this same original SHA256 before and after, and the same existing working-copy hash:

```text
4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153
```

The original is `D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll`; the existing copy is `.tooling/offline-cache-node/Engine.readonly.dll`. These are saved observations, not a new original-file verification by this document. Each inspection links the precise boundary/provenance record. No original was loaded or executed, and no Rea/Java backend was used for these captures.

Useful instruction-artifact hashes:

| Function / fragment | Instruction JSON SHA256 |
| --- | --- |
| `2805B0` | `3933282b41554e044696a70c4e076b18f2ef8d0443283242e7c1aa11f8293fea` |
| `280490` first | `d23271c68ed2bb53fc6a956a71486972986bf66b8a9116bba61a7dda940745f3` |
| `280490` continuation | `14cfe903f3093f3f73483a2d0c9ae9894bda542e4139e7d2d9d7d5b52ae1e44a` |
| `03C8E0` | `bf43efe33734c7fa8dd8d1757e40f025810217135e73b5059152b4d5a37f587d` |
| `283A80` | `819e3c51bd1688bf0778405467bd17f4a5f60f93eb75174871b38a6e5186c8f4` |
| `280AB0` | `a18a7c5b8634bad69286945ddc42921328e39ab5d3b5a347b26382219037c547` |
| `283FF0` | `819a6bcf8e55f49b825cfa0589f25089279a9dc13b42dee9f35d8f3a738c0b99` |
| `283A00` | `4ce242209f267fd912187ef27a34283b229e0ae9859e19d9b643d407da10bae3` |
| `283840` | `344ddc0db8aef5f3195896487ef7fd36aafb18e0733dbea7b4320d1d1a9c941c` |

The separate [fixed literal and import record](offline-archive-map-publish/empty-query-and-compare-import.json), SHA256 `a3f8e80396046474daead1958056fc00d4733920760c6134356006255403eb39`, confirms:

- The one byte read at `0x180339CE0` is `00`, so the observed post-import C-string query is empty.
- The six bytes at `0x1802EE060` are `ff25aa460100`, a jump through IAT `0x180302710`.
- The normal PE import is `VCRUNTIME140.dll!memcmp`, thunk index 3, import-descriptor RVA 4282012.

That separate operation decoded **1 fixed data byte, 6 thunk code bytes, and 2,680 PE metadata bytes**, with the same original-before/original-after/copy hashes. It loaded no DLL and followed no CRT body. The six thunk bytes are additional to the 3,326 bytes from the nine dumpbin captures; they are not an extra dumpbin invocation or part of the 895 disassembled-instruction total. The import identity supports a normal successful comparison contract, not CRT failure or native exception equivalence.

## 2. Keep the three index spaces distinct

Let `A` be the archive object and `C = A + 0x88` its string-index context in `2805B0` and `280AB0`.

| State | Meaning supported by these callers |
| --- | --- |
| `C+08` | String-index list sentinel. |
| `C+10` | Qword node count; its old low 32 bits supply the ID for a new unique key. |
| `C+18`, `C+30` | Bucket endpoints and hash mask used by `03C8E0`. |
| `C+40/+48/+50` | Begin/end/capacity-end of 32-byte string objects forming the target pool. |
| `C+58` onward | Raw-query-address cache context, not the appended-ID map. |
| `A+170` | Appended-input mapping-array pointer. |
| `A+178` | Mapping-entry count, consumed as signed metadata by the record remapper. |
| `A+198` | Base recorded by the latest `280490` call. |
| `A+1A0/+1A8/+1B0` | Selector/base vector begin/end/capacity-end. |
| `A+1B8/+1C0` | Separate offsets vector used by the record-fetch caller. |

The target pool's unique IDs, appended-input positions, and address-cache keys are not interchangeable. In particular, an append base is not automatically the current unique-string count, an archive ordinal, a file offset, or the length of the initial source pool.

Native reads do not verify that the string-index node count equals the target-pool length, that payload IDs point to matching pool strings, or that every mapping entry is a valid target ID. Those are appropriate explicit invariants for a normal logical projection, not guards recovered from these bodies.

## 3. Full-byte name lookup: `03C8E0`

The query is a native string object. `03C8F7..03C911` obtains capacity and stored length and selects inline or heap bytes. `03C930..03C942` hashes **every stored byte**, using FNV-1a 64-bit with unsigned byte loads; an empty string retains the FNV offset basis.

The lookup selects `hash & mask`, starts at the bucket's first node, follows `next`, and stops at `last.Next`, or at the sentinel for an empty bucket. A node's key string is at `node+10`. The two comparison sequences at `03C98E..03CA1D` combine byte comparison over the minimum stored length with length equality.

Consequences:

- Embedded NUL is an ordinary stored byte here. No C-string truncation, case folding, slash normalization, Unicode interpretation, trimming, or locale conversion is shown.
- A zero-length string differs from a positive-length string beginning with NUL.
- The caller receives a node or sentinel through the output slot; `RAX` returns that slot's address. The lookup itself does not interpret `node+30` as an ID.
- A bounded dictionary abstraction can reproduce lookup outcomes for a complete valid index, but does not reproduce native bucket/list layout, comparison-call count, or corrupted-container behavior.

The comparison calls at `03C9C0` and `03CA0A` target `2EE060`; the fixed import record above binds that thunk to normal `memcmp`. Full stored lengths remain the caller's explicit inputs to the comparison.

## 4. Reader behavior shared by initial and append

Both paths consume a count DWORD and then that many length-prefixed strings, subject to a **signed-positive** loop-count test. Their reader object supplies base at `+00`, current pointer at `+08`, and total length DWORD at `+10`. Neither body validates the count DWORD's readability before reading it.

For each string, append `280630..280661` and initial `283AE0..283B11` do this:

1. Read the length DWORD and immediately advance the reader pointer by 4.
2. Require unsigned length at most `0x100000`.
3. Compute the low-32-bit relative payload offset plus length with wraparound; compare that unsigned result with reader length.
4. Only if both checks pass, call `022CE0` to assign the payload bytes to the reusable temporary string, then advance the reader by the payload length.
5. Continue to string-index lookup in either case.

The temporary string is initialized to inline-empty **once**, before the loop. If either length check fails, native execution does not clear it, skip the claimed payload, reject the whole input, or return an error. It continues with its previous contents; the first failed item therefore uses the initial empty string. Since the payload was not consumed, the next iteration reads its beginning as the next length DWORD.

Length zero is different from failure: it passes the checks when the computed reader range permits it and assigns an empty string, replacing the previous temporary value.

A safe reader must check actual supplied storage before dereferencing it and can impose smaller budgets. Any resulting `Unsupported` is a self-owned coverage stop. It must not be reported as a native parse error or as proof that no earlier clear, cursor movement, reserve, node insertion, or mapping write happened.

## 5. Node construction: `283FF0`

This helper is not the previously recovered string/table move constructor `05DEB0`.

`28400D` calls `01F170` and keeps the returned node. The caller-supplied linkage and allocator behavior remain in that helper's normal-success contract; this body does not itself show the node's `next`/`prev` initialization.

For the node's string at `node+10`, the helper:

1. Loads the source string through the supplied pointer-to-source argument (`28402D/284032`).
2. Sets capacity to 15 and stored length to zero, then writes the empty terminator.
3. Calls `023180(destination, source, 0, npos)` at `284061`.
4. Only after that call returns, writes **DWORD zero** at `node+30` (`284066`).
5. Returns the node pointer.

This is a copy into newly constructed storage. It does not clear or move-from the source temporary. It does not initialize an arbitrary 8-byte opaque payload to zero; only the observed 4-byte ID payload store is established.

The test at `284028` is of `node+10`, not a reliable null-allocation check. Normal nonnull allocation, valid source storage, successful copy, and non-aliasing ownership must be explicit. A failed allocation or copy cannot be treated as a constructed node with a known zero payload or as an atomic no-effect operation.

## 6. Initial import: `283A80`

The initial function takes `C` directly, not `A`. It calls `283A00(C)` at `283AA9` **before reading the incoming count**. It then initializes the reusable temporary and loops over signed-positive count.

Each item uses the reader behavior above. A successful exact lookup at `283B20` immediately skips to the next input at `283CA8`; this test checks node presence, not its payload. Even an existing payload `0xFFFFFFFF` is not repaired.

For a new name under stable exclusive state:

1. `283B33` captures the old qword node count.
2. A second lookup precedes node construction; native code also contains a dynamic hit branch, which cannot occur under an unchanged complete index after the first miss.
3. `283B70` constructs a copied-name node.
4. The maximum-count test occurs after construction.
5. `283B97..283BA3` increments count and writes the two neighboring links.
6. `283BBB` invokes `02E770` to index the prelinked node.
7. `283BCE` stores the captured old count's low 32 bits as payload ID.
8. The pool append path handles capacity, constructs a copied string at the old pool end, then advances end by 32 at `283CA3`.

Under valid count/pool correspondence, unique names therefore receive IDs in first-occurrence order. Duplicates are omitted from the target pool. No per-input remap row is generated by this function.

That last distinction is essential. One must not invent an initial old-index-to-deduplicated-index mapping, preserve duplicate input positions by silently appending them, or pass a deduplicated pool as though it still had the original raw IDs. A pipeline that needs initial source IDs preserved must state and establish its extra uniqueness/order premise, or keep that connection unsupported.

The absence of direct appended-map stores does not by itself establish that `A+170` was null before or after initial import. The surrounding archive initialization and prior state remain separate.

At `283CC3..283CDE`, even when the input count was nonpositive, the function invokes `284080(C+58, trunc(N/4), trunc(N/2))`. The arguments derive from the **original signed input count**, not the deduplicated target size. This pointer-cache initialization tail is not closed by the present capture. A logical result after string indexing is not the complete native initializer's return.

## 7. Clear prefix: `283A00`

The new clear body establishes this order:

1. `283A1B`: call `02E180(C+8)`.
2. `283A28`: call `01B4F0(C, 8)`.
3. `283A36`: call `01ED80(poolBegin, poolEnd)`.
4. `283A3F`: set pool end to its begin, retaining the separately stored capacity.
5. `283A45/283A49`: write zero qwords at `C+5C` and `C+64`.
6. Free a nonnull pointer at `C+70`, then null it; repeat for `C+78`.

The first three calls still require normal container-clear, bucket-reset, and string-range-release contracts. They are not inlined by this caller capture. The explicit writes and calls concern the string context and its query-address cache; they are not an observed reset of archive `A+170/+178`, the selector vector, or the data-offset vector.

Within an owned logical model, clearing the dictionary and target pool may be the useful projection. If pointer-cache fields are omitted, they must remain a declared omission. If cleanup stops or throws, preceding destruction or field writes cannot be claimed rolled back. The count read that follows clear in `283A80` also cannot be moved before it and then described as the native sequence.

## 8. Append reserve and base: `280490`

Let `M` be the old mapping-count DWORD and `N` the requested append-count DWORD. The native function always attempts a replacement mapping allocation:

```text
newCountBits = uint32(M + N)
signedCount64 = sign_extend_int32(newCountBits)
allocationBytes = unsigned_64_multiply_with_saturation(4, signedCount64)
```

At `2804A7` the count addition wraps in 32 bits; `2804AD` sign-extends it. The unsigned multiplication uses overflow saturation to all-one bits. This is not a checked nonnegative array-length calculation. A normal bounded model must exclude negative or overflowing resulting allocation sizes.

The complete observed order is:

1. `2804C6`: allocate replacement storage through `2DC900`.
2. If old `A+170` is nonnull, copy `sign_extend(oldCount) << 2` bytes through `2DDB0E`, then release the old pointer through `2DC8F8`.
3. Save old count in ESI; `280504` publish the replacement pointer at `A+170`.
4. `280512` write old count to `A+198`.
5. Append that same old count to the selector vector at `A+1A0`, including capacity handling through `02DE40` when needed; advance vector end by 4 at `28058C`.
6. `280593` increase `A+178` by `N`.
7. Return the old count in `EAX`.

Thus the append base is **the cumulative number of earlier appended raw entries**, including their duplicates. It is not the number of unique target strings. Initial import contributes no count in this function unless some independent caller/state initialization has explicitly done so.

`N == 0` is not a no-op: replacement allocation/copy/release and selector append still occur, and selectors can contain repeated bases. Negative input counts also reach reserve before `2805B0` decides whether to iterate.

The new allocation's additional cells are not shown initialized. A partial append must not fabricate all unwritten cells as known zeros. An old nonnull map has its old prefix copied; a null old map with a nonzero old count does not magically supply a valid prefix. A successful logical projection therefore needs explicit valid prior storage and must distinguish pending/unwritten entries on a later stop.

Failure order also matters: allocation precedes old release, whereas publication of the new map and `+198` precedes selector growth and final count update. If a later operation fails, native state may already have changed. A managed implementation that prepares replacement storage early for safety must preserve its declared logical stages and cannot claim allocator/EH equivalence.

## 9. Appended strings: `2805B0`

The entry reads the incoming count and advances reader by 4 at `2805DB..2805E2`. It initializes the temporary string, calls `280490` at `2805FD`, and stores the returned base. Only then does it test `A+170` for null and `N` for signed-positive at `280607..28061B`.

For each item, the first name lookup is at `280673`. If its node payload is any DWORD other than `0xFFFFFFFF`, the payload becomes that item's mapping value immediately. Other negative bit patterns are not treated as “missing.”

If the first result is sentinel or payload `0xFFFFFFFF`, the code performs a second lookup. A node found at `280699` skips construction and pool append, even if its payload is still `-1`; the final lookup can therefore retain `-1` as the mapping value. There is no automatic repair of an existing `-1` payload.

For a stable genuine miss:

1. Capture the old context node count (`2806AC`).
2. Perform another lookup, then call `283FF0` if still absent.
3. After construction, check the maximum count.
4. Increment node count and publish two neighboring links (`28070D..280719`).
5. Invoke `02E770` (`280731`).
6. Write captured old count's low 32 bits to the returned node's ID field (`280744`).
7. Append a copied temporary string to the target pool, with capacity handling; advance its end (`2807EB`).
8. Look up the name again (`2807FE`) and read its payload or use `0xFFFFFFFF`.
9. Write the mapping row at `28081B`.

The row subscript is **zero-extended `uint32(base + inputIndex)`**, not the signed-relative pointer calculation used inside record remapper `280870`. Both base arithmetic and valid destination storage must be bounded explicitly.

The third lookup's dynamic-hit branch still writes a new payload and appends a pool string. Stable exclusive state excludes that intervening change; it is an assumption that makes a simple logical implementation valid, not proof the branch is absent from native code.

The temporary's copy semantics matter here: pool/node construction does not consume it. A failed later length check can reuse its previous value as described above. Ordinary repeats share an existing target ID while still consuming a distinct appended mapping slot.

## 10. Post-import lookup/insertion: `280AB0` and `283840`

`280AB0` calls `283840(C, address 339CE0)` first and returns directly if the result DWORD is not `0xFFFFFFFF`. The fixed data record confirms the first byte at that address is zero, establishing an empty C-string query without scanning any further bytes.

If the lookup returns `-1`, the function explicitly constructs a **zero-length** temporary string (`R8D=0` at `280B01`, copy call `280B10`). It then checks the exact stored-byte string index. If no such key exists, it performs the same normal node construction, linkage/indexing, ID assignment, and pool append sequence. Finally it calls `283840` again and returns that result.

“Ensures an empty string” still requires state qualifications:

- If an exact empty key already exists with payload `-1`, `280B37` skips insertion and does not repair it; the final lookup can still return `-1`.
- Any first lookup result other than `-1` skips the body, including other negative bit patterns.
- The result is not forced to ID zero. A missing empty string is appended at the current unique target count.
- This path does not append mapping-array rows or increment `A+178`.
- A warm query-address-cache hit compares C strings; a stored string with leading NUL can compare equal to an empty query even though it is not a zero-length stored key. Consistent valid cache/index/pool state is required before asserting the returned ID represents the exact empty stored key.

A sufficient ordinary logical domain is a fresh or correctly correlated query-address cache, a stable complete exact-byte index and pool, valid corresponding non-`-1` IDs, and successful normal insertion/copy contracts. In that domain an existing exact empty key retains its ID, or a missing empty key is appended. Arbitrary cache contents and pre-existing `-1` payloads are outside that guarantee. Omitting the native query-cache actions still makes this a logical empty-key projection rather than full execution of `280AB0`.

`283840` is not a pure dictionary lookup:

1. With a query-address cache present, it selects a bucket using the raw 64-bit query address shifted by the low six shift bits, then truncates to 32 bits before mask selection.
2. It compares **address values**, follows the cache chain, and on a hit uses the full cached qword ID to locate a pool string at `poolBegin + (id << 5)`.
3. It compares that pool string against the query as C strings (`283954..283967`), stopping at NUL. A matching hit returns the cached ID's low DWORD.
4. A miss copies the query's C-string prefix, calls full-byte lookup `03C8E0`, frees the temporary when needed, and for any ID other than `-1` calls `284160(C+58, queryAddress, signExtend(id))` **before returning**.
5. A stale hit performs the same fresh name lookup. If the result is not `-1`, it sign-extends and writes the new qword ID to the existing cache entry at `2839F4`. A fresh result of `-1` leaves that old cache ID unchanged and returns `-1`.

The warm path does not validate the cached qword against pool bounds before dereferencing it. These native invalid-state behaviors are not acceptable managed-array contracts. Raw pointer identity must not be replaced with a newly allocated byte-array object's identity or automatically with a preferred image VA.

The previously developed bounded pointer-cache modules do not turn this document into a claim that the full native `284080` initialization and all `284160` allocation/EH paths have been recovered. A map-generation projection may deliberately return the logical empty-key outcome while declaring pointer-cache effects omitted. It must not label that result as executing the entire native post-import function.

## 11. Minimum useful logical implementation and stopping rules

A self-owned implementation can use the captured ordering to produce a useful bounded state:

- An ordered target pool of immutable byte strings and a complete exact-byte name-to-ID index.
- A separate appended mapping vector, its logical count, latest base, and selector history.
- An owned reader cursor, reusable temporary string, explicit operation stages, and a first-result journal.
- Ordinary successful clear, node supply, string copy/release, indexing, vector growth, and allocation contracts, rather than native addresses or allocator emulation.

The ordinary success domain should establish all of the following:

1. Stable exclusive ownership and non-aliasing of reader bytes, temporary strings, pool, index, and mapping storage.
2. Complete, valid container metadata, no concurrent change between repeated lookups, and count/pool/ID correspondence for newly generated IDs.
3. Known readable scalar headers and payloads, bounded cumulative work/storage, nonnegative normal counts, and no overflow in the selected safe arithmetic domain.
4. Explicit handling of duplicates and exact byte identity. No normalization, implicit archive merging, initial source-ID repair, or inferred selector generation beyond observed inputs.
5. Known normal success for unexpanded allocation/container operations, or an `Unsupported` boundary before crossing them. Such contracts must be named as assumptions, not counted as independently recovered native bodies.
6. Explicit omission or separately supported modeling of address-cache initialization and post-lookup cache effects.

Useful stops include insufficient reader storage, own string/pool/map budgets, impossible count arithmetic, unknown container state, and unsupported downstream initialization. A safety stop can intentionally differ from the native malformed-input path, but must say so and preserve any already completed logical state.

Specific prefixes that can survive a stop include:

| Later stopping point | Earlier effects that may already exist |
| --- | --- |
| Initial count/header read | Initial clear has already run. |
| Append count validation or first string | Count DWORD consumed; reserve/base/selector/count state may already be changed. |
| A later string's bounds/budget check | Earlier strings, pool entries and mapping rows; current length DWORD consumption; prior temporary contents. |
| Node maximum-count or index stage | Node allocation/copy may have occurred; count/link writes may precede index completion. |
| Pool capacity/copy stage | Node count, links, index and ID payload may already be committed. |
| Final appended mapping write | Target dictionary/pool may already include the new string. |
| Pointer-cache initialization or post-lookup | Logical strings/mappings may already be complete; query-cache effects can be separate partial work. |

There is no native rollback transaction in these callers. An in-place owned attempt must retain cumulative effects and prevent an automatic retry from applying the append twice. If a safe implementation instead uses whole-input preflight or staged publication, it should identify that policy and not equate its no-effect rejection with native failure behavior.

## 12. Connection to record remapping and remaining boundaries

[RECORD-REMAP-EVIDENCE.md](RECORD-REMAP-EVIDENCE.md) describes how `280870` adds the selected base to each field key and tag-2 value, then consults the appended mapping array. [ARCHIVE-FETCH-EVIDENCE.md](ARCHIVE-FETCH-EVIDENCE.md) describes `280E20` selecting that base through a separate offsets vector.

The current functions explain how bases and appended rows are recorded. They do not fully establish acquisition/ordering of the offsets vector, complete archive initialization or merge policy, initial-record ID compatibility after deduplication, scratch ownership, or the full record-load/factory/Read chain.

The following remain explicit dependencies or conditional runtime contracts:

- `01F170` node supply/link initialization; `02E180`, `01B4F0`, and `01ED80` normal clear/reset/release effects.
- `02E770` indexing, including the applicable string-key container state; previously captured bounded container behavior is not a generic malformed-container or allocator guarantee.
- `034F50` target-string vector growth, `02DE40` selector growth, and the `0219A0` alias append branch when not excluded.
- String assign/copy/release and memcpy/allocator behavior under valid normal inputs; native OOM, alignment-error branches, imported fatal diagnostics, and exception unwinding are not reproduced.
- Native pointer-cache tail `284080` and the relevant effects of `284160` reached by `283840`, unless explicitly linked to separately supported state/contracts.

The two narrow data checks have been incorporated in section 1. Implementation/test acceptance totals belong to the separate phase result and acceptance records; no such totals or completion claims are asserted here.
