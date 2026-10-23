# Record-body remapping at Engine `0x180280870`

This document records the bounded static evidence for `280870` and its already captured caller `280E20`. It is an instruction-level contract for review, not an implementation acceptance report. No original binary, working-copy DLL, backend, or test was executed or read while writing this document; the inputs were the saved project evidence linked below.

## 1. Provenance and captured extent

The [boundary record](offline-record-remap-boundary/boundary.json) identifies the unwind region `[0x180280870, 0x180280957)`. The [inspection record](offline-record-remap-body/inspection.json), [instruction JSON](offline-record-remap-body/280870-instructions.json), and [dumpbin output](offline-record-remap-body/280870-dumpbin.txt) cover all **231 code bytes / 65 instructions**, including the final `RET` at `280956`. All observed branches stay within this capture, and there are **no call instructions or followed callee bodies**. Completeness follows from these instructions and exits; a `.pdata` entry alone would not establish a complete logical function.

| Artifact | SHA256 |
| --- | --- |
| Boundary JSON | `717e9c979bec52fbfd7d8d69e106d4141cb040e371509368302592703edba2bf` |
| Inspection JSON | `9593935e6f49e2d57a11219f2ddaff12368e19d357fee1620bba5a5297e12d6a` |
| Instruction JSON | `9937f4cc1348bb988154ba0431e0011a79702e51425662d51308be39315edff3` |
| Saved dumpbin output | `bab733563f701c47870fbec39acbb2168f324897c0340789cda9dae214d32ab4` |
| Selected 231 code bytes | `0122041d356a29a332543e03079ef8fbddc3e6a5dc780961b3732e6277314603` |

The boundary operation decoded PE metadata only and started no disassembler. The body operation used the existing Microsoft `dumpbin.exe`, exited 0, and verified the complete selected byte coverage. The body inspection records 36 ms for that tool invocation; this is neither a process-memory measurement nor a test duration.

Both operation records report unchanged original and existing working-copy SHA256:

```text
4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153
```

The recorded target is `D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll`; the existing copy is `.tooling/offline-cache-node/Engine.readonly.dll`. These are archived hash observations, not fresh hashes calculated by this document. The records exclude target loading/execution, Rea/Ghidra, installation, and original modification. The code-byte total excludes streaming whole-file hash reads and the recorded 832 bytes of PE metadata per operation.

## 2. Arguments, state, and early exits

The entry registers and their uses are:

| Argument/state | Observed role |
| --- | --- |
| `RCX` | Archive object; retained in `R10`. It must be readable even for an early return. |
| `RDX` | Record-body base address; retained in `R11`; copied to cursor `RAX` after the mapping-pointer guard. |
| `R8D` | Body length, retained as a 32-bit value in `EDI`; used by the final **unsigned** offset comparison. |
| `R9D` | 32-bit additive value, retained in `EBX`; additions preserve wraparound bits. |
| Archive qword `+170` | Mapping-table pointer, initially tested for null and reloaded for successful lookups. |
| Archive DWORD `+178` | **Signed** mapping-count bound, reread for each nonnegative candidate index. |

At `28087A`, the function reads archive `+170`; `28088E` returns immediately when that pointer is null. This means a **null mapping table**, not a null archive object. There is no check for a null archive pointer before its dereference.

With a nonnull mapping pointer, `280894` sets `RAX = bodyBase`, then `280897/28089A` returns if `R8D == 0`. Neither early branch reads or writes the body. They do not validate the body pointer, mapping count, or mapping elements. A safe API should not invent reads of these unused inputs on an early branch.

The body does not directly store to archive fields. Within a valid, stable, non-aliasing snapshot domain, it only mutates body key/value DWORDs. That qualification matters: the native code does not prevent the body from aliasing the archive or mapping array, and it reloads mapping state as execution proceeds.

## 3. Exact DWORD transformation

Both the field key (`2808B1..2808CE`) and each tag-2 value (`280906..280920`) use the same transform:

```text
sumBits = (originalBits + additiveBits) mod 2^32
index = interpret sumBits as signed int32

if index >= 0 and index < signed_int32(archive[178]):
    resultBits = mapping[index]       // one DWORD read
else:
    resultBits = sumBits

store resultBits
```

The `ADD EDX, EBX` is not checked arithmetic. `JS` tests the sign of the wrapped result; the subsequent `JGE` uses a signed comparison against archive `+178`. A model must not use an unsigned count comparison, reject all overflow, or reject an originally negative DWORD before adding. An originally negative bit pattern can wrap into a nonnegative in-range index.

The fallback is the **added result**, not the original input. A mapping count of zero or a negative signed count prevents every nonnegative index lookup but does not suppress addition or stores. A nonnull map with a zero additive value is not necessarily an identity operation: in-range inputs still select mapping elements.

A mapped value is written exactly as its DWORD bits. It receives no second addition, recursive mapping, sign rejection, or string-pool validation here. This function does not establish that any returned mapping entry is a valid ID for a later consumer.

For example, at the level of this transform, `0xFFFFFFFF + 1` becomes index 0 and can read `mapping[0]`; `0x7FFFFFFF + 1` becomes `0x80000000` and is stored without lookup. These are arithmetic deductions from the instructions, not executions of the original function.

## 4. Header reads, writes, and tag dispatch

Let `cursor` initially equal `bodyBase`. Each iteration starts unconditionally once the nonnull-map/nonzero-length guards pass:

1. `2808A0` reads the key DWORD at `cursor + 4`.
2. `2808A3/2808AA` computes the low-32-bit relative offset of that key.
3. `2808A7` reads the header DWORD at `cursor`.
4. `2808AD` advances `RAX` by 8 bytes.
5. The key is transformed and **stored at `2808CE` before tag dispatch or payload processing**.
6. `2808D2` extracts unsigned low-16-bit `typeTag`; `2808D6` extracts unsigned high-16-bit `elementCount`.

Thus the layout is a 4-byte `(count << 16) | tag` header, then a 4-byte key, then type-dependent payload traversal. The header DWORD is captured before the key store. The function does not use a separate total-field count.

| Tag | Behavior after the key store | Instruction points |
| --- | --- | --- |
| 0 | Skip `elementCount * 4` bytes, without reading or writing those payload words. | `2808DA/2808DC` → `280934/28093C` |
| 1 | Same payload skip. | `2808DE/2808E1` → `280934/28093C` |
| 2 | Read, transform, and write each payload DWORD in forward order. | `2808E3..28092D` |
| 3 | Same payload skip as tags 0 and 1. | `28092F/280932` → `280934/28093C` |
| Other 16-bit tags | Do not skip `count * 4`; advance only the already consumed 8-byte header. If the loop continues, following bytes are interpreted as another header. | `280932` → `28093F` |

`elementCount` is at most 65535. Its local `* 4` skip is therefore at most 262140 bytes and does not itself overflow a DWORD. The native code still does not prove the resulting pointer lies in the body or that cumulative offsets are safe.

For tag 2, `2808E8..2808F0` establishes the destination address, then the count-zero branch at `2808F3/2808F6` bypasses all payload reads and writes. A zero count is not a native rejection: the key has already been written, and the iteration has consumed 8 bytes.

Each nonempty tag-2 step is specifically:

```text
280900: read original payload DWORD
280902: advance source cursor by 4
280906..28091D: wrapping add and conditional mapping-table read
280920: write result DWORD
280923: advance destination pointer by 4
280927/28092B: decrement element count and repeat
```

In the bounded ordinary domain, source and destination refer to the same payload location, so the operation is in place. Previous payload stores remain visible before subsequent reads. There is no transactional staging, rollback, temporary output buffer, allocation, or disposal in this function.

The tag-3 skip and unknown-tag traversal are remapper facts only. They do not establish that the independently recovered parser supports those tags. A connected safe parser/remapper pipeline may retain a narrower tag domain and must label it explicitly.

## 5. Signed write offsets and unsigned termination

Two different integer interpretations must not be conflated:

- Key destination `2808CB/2808CE` is `bodyBase + sign_extend_int32(low32(cursorBeforeHeader + 4 - bodyBase))`.
- Tag-2 destination initialization `2808E8..2808F0` similarly sign-extends the low-32-bit relative payload offset before adding the body base.
- End test `28093F..280946` compares `uint32(cursor - bodyBase)` with the length DWORD using `JB`.

For nonwrapping bodies whose offsets remain below `2^31`, these write destinations equal the intended forward positions. Large or inconsistent native ranges can instead produce signed-relative write addresses before the body, a wrapped end offset, or other invalid memory access. A bounded managed model must state its ordinary-range premise rather than replace these cases with a claim of native checked-pointer behavior.

The loop is effectively a do/while after the early exits. It checks the length **only after** the whole header and its selected payload action. There is no header-fit test, payload-fit test, final-offset equality requirement, or per-value length check:

- A length in 1…7 still reaches both initial DWORD reads and the key store, if the pointed memory is otherwise accessible.
- A tag-0/1/3 count can move the cursor past the supplied length without reading the skipped payload. If the low-32-bit offset then compares greater than or equal to the length, the function returns.
- A tag-2 count reads and writes all indicated values before the final length comparison, even when they extend past the supplied length.
- An unknown tag may reinterpret its nominal payload as further headers. It does not return an “unsupported tag” status.

These observations describe missing native validation; they are not permission for a safe implementation to read or write outside its owned input. Whole-input preflight, finite budgets, and unsupported results for excluded layouts are safe-profile rules, not equivalent native malformed-input handling.

## 6. Return registers and partial effects

There is no boolean result construction:

- On the null-mapping-pointer exit, this function has not assigned `RAX`.
- On the zero-length exit with a nonnull mapping pointer, `RAX` is the supplied body pointer.
- On a normal completed traversal, `RAX` is the cursor after the last processed header/payload action.

This does not establish a declared C++ return type. It does establish that interpreting `AL` or `EAX` as a success/failure flag would be unsupported. The known caller ignores the return register.

No native failure status is produced for an unsupported tag, invalid mapping span, truncated body, bad pointer, or signed-offset problem. A memory fault or other abnormal interruption can occur after earlier stores. In particular:

1. An out-of-range candidate index is not a failure; its wrapped added bits are stored.
2. An invalid mapping element read for the current key can interrupt before that key is written, while earlier fields remain modified.
3. The current key is already stored before any tag-2 payload lookup. A later mapping/payload fault can leave that key and earlier payload values modified.
4. Count-zero and unknown-tag fields can still perform a key store.
5. Even when a store writes identical bytes, the store was executed; “no changed bytes” is different from “no mutation stage reached.”

A self-owned safe implementation can preflight all bounds or calculate a detached result before publication. If it does so, an unsupported result with no exposed output or no committed mutation describes the safe API only. It must not claim the native function would reject at the same point, leave the original body unchanged, or be safely retryable after a native partial attempt.

## 7. Relationship to `280E20`

The saved [caller continuation](offline-archive-fetch-continuation/280e20-continuation-instructions.json), SHA256 `f02600514aa9ab1adbfe3a5cf2bf70eb121e1d749b692c50ba14986533af44e1`, proves the surrounding sequence; [ARCHIVE-FETCH-EVIDENCE.md](ARCHIVE-FETCH-EVIDENCE.md) documents its full capture.

After successful decoding, `280F31` writes archive `+194`. Remapping is skipped only if the valid offset-vector count is at most 1, or the unsigned input pointer is before `dataBase + offsets[1]`. Otherwise `281002..281012` calls:

```text
280870(archive, archive[188], record[30], selectedDWORD)
```

The new body establishes that `selectedDWORD` is used as the additive bits for every traversed key and every tag-2 value. An empty selector vector does not skip this call; it supplies the caller's initialized zero. Zero addition can still map IDs.

The caller does not test the remapper return value. It reloads archive `+188/+194`, publishes those outputs, and returns true. The new body contains no direct archive-field stores, so under the explicit non-aliasing domain the scratch identity and length remain unchanged by this particular helper. The caller's reloads alone had not previously proved that fact.

The output remains borrowed archive scratch. This evidence does not close native archive initialization, lifetime/concurrency of the mapping vector, construction of the selector/offset vectors, scratch allocation failures, or complete archive loading. A single-file ARZv3 input is still not proof of the caller's no-remap branch or a specific native mapping state.

The prior [remap readiness record](native-decoder-validation/remap-readiness.json) deliberately reported insufficient evidence before this capture. Its unknown-body assessment is historical. The new body closes the helper's ordinary instruction contract; it does not retroactively establish native runtime fixtures, official compatibility, or acceptance of a new implementation.

## 8. Implementation review checkpoints

Review any implementation against the following concrete boundaries:

1. Expose mapping-pointer presence, signed count, additive bits, and owned body range explicitly. Do not infer a populated native mapping table from a parsed ARZv3 string pool or assume all nonnull tables have positive counts.
2. Preserve early-exit order: mapping-null first, then length-zero. Neither branch accesses body content. Invalid archive ownership remains a separate safe-domain concern.
3. Use unchecked 32-bit addition followed by signed range selection; retain fallback sum bits and arbitrary mapped DWORD bits. Do not remap the mapping output again.
4. Preserve the distinction between field keys, tag-2 values, untouched payloads for tags 0/1/3, and unknown-tag header-only advancement. Do not silently reuse the parser's accepted tags as the complete native dispatch.
5. Represent zero counts, signed count bounds, repeated IDs, and order-dependent writes honestly. Exclude mapping/body/archive aliasing explicitly if using an immutable mapping snapshot.
6. Bound header/payload spans, cumulative offsets, work, and own allocation before mutation when the safe API promises atomic publication. Report these as coverage guards, not recovered native errors.
7. If exposing an in-place attempt, retain cumulative stores/effects and a sticky first result on later failure. If exposing a detached result, keep its lack of external partial writes separate from native partial effects.
8. Do not synthesize a native boolean return or claim that whole-record decode, archive initialization, or downstream string-pool validity is supplied by this helper.

Useful distinguishing tests include signed wrapping into index 0, wrapping into a negative index, zero/negative signed counts, mapping values with the high bit set, additive-zero remapping, zero-count fields, mixed payload tags, duplicate IDs, partial/truncated safe inputs, and mapping-null/zero-length inputs that deliberately make unused state unavailable. Such tests would validate the chosen safe implementation and its restrictions; this document does not assert that they have run.
