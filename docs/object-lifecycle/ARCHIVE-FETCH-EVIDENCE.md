# Archive fetch evidence and the independent ARZv3 bridge boundary

Date: 2026-10-20.

This document records the bounded static evidence for Engine `0x180280E20` and `0x180280D20`. Their complete observed normal control flow is available, but their dependencies do not close the native archive-fetch operation. The implementation direction for this stage is an independent, bounded ARZv3 raw reader feeding the existing supplied-record interface. It must not claim to implement the unknown Engine decoder, archive initialization, or remapping.

This document is evidence analysis, not an implementation acceptance report. Writing it used existing project text only. No new backend, game execution, binary loading, or original-file read was performed by the document reviewer.

## 1. Six bounded disassemblies, two logical functions

The original target is `D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll`. The existing working copy is `.tooling/offline-cache-node/Engine.readonly.dll`. Each of the six inspection records reports the same original SHA256 before and after its operation and the same working-copy SHA256 afterward:

```text
4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153
```

These are the saved inspection results, not a fresh hash calculation by this document. Each record reports tool exit 0 and verified complete byte coverage of its selected range. The ranges below are half-open.

| Logical function | Captured range | Code bytes | Assembly and inspection |
| --- | --- | ---: | --- |
| `280E20` | `0x180280E20..0x180280EF2` | 210 | [assembly](offline-archive-fetch-inspection/280e20-dumpbin.txt), [inspection](offline-archive-fetch-inspection/inspection.json) |
| `280E20` | `0x180280EF2..0x180281042` | 336 | [assembly](offline-archive-fetch-continuation/280e20-continuation-dumpbin.txt), [inspection](offline-archive-fetch-continuation/inspection.json) |
| `280D20` | `0x180280D20..0x180280D2A` | 10 | [assembly](offline-archive-decode-inspection/280d20-dumpbin.txt), [inspection](offline-archive-decode-inspection/inspection.json) |
| `280D20` | `0x180280D2A..0x180280D4F` | 37 | [assembly](offline-archive-decode-continuation/280d2a-dumpbin.txt), [inspection](offline-archive-decode-continuation/inspection.json) |
| `280D20` | `0x180280D4F..0x180280DF1` | 162 | [assembly](offline-archive-decode-tail/280d20-tail-dumpbin.txt), [inspection](offline-archive-decode-tail/inspection.json) |
| `280D20` | `0x180280DF1..0x180280E1E` | 45 | [assembly](offline-archive-decode-epilogue/280d20-epilogue-dumpbin.txt), [inspection](offline-archive-decode-epilogue/inspection.json) |

The totals are **546 code bytes for `280E20` and 254 for `280D20`: 800 code bytes across six dumpbin invocations**. The byte total excludes necessary PE metadata and streaming whole-file hashing. It is not a claim that only 800 file bytes were read.

The tool is the existing Microsoft Visual Studio Build Tools `dumpbin.exe`, with recorded SHA256 `cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815`. No original DLL was loaded or executed, and no Rea/Ghidra backend was started for these captures.

PE `.pdata` entries delimit unwind regions and can split a logical function. The first 210-byte `280E20` region falls through at `280EF2` and branches to `281031`; it was not a complete logical function. Likewise, the first `280D20` region contains only its stack-allocation prologue. The additional captures follow those already identified continuations, not arbitrary callee bodies. The joined ranges now contain the observed returns and internal branch targets of both functions; they do not contain the decoder at `278D20` or remapper at `280870`.

The main execution thread also recorded two script-generation/path errors before original-file or dumpbin access. They were corrected before the successful captures above; they are not additional disassembly runs or successful analysis evidence.

## 2. Caller and actual argument use

The earlier [LoadFile miss assembly](rea-record-load-dependencies/11-LoadFile-miss-0x1801c4e20-assembly.json) is the caller provenance:

- `parameters.procedure`: `0x1801c4e20`.
- Evidence ID: `ev_23bf6a83dc20e5e716f1454ed05f04ab266b5e67430c35070017de7c68eb1937`.
- File SHA256: `d696e7b2141e5fe9ee6f518555d32a615ab174deabd6f676a15c299be1b7e3b1`.
- At `1C4EDF`, archive byte `+2E` bit 0 selects the fetch helper. `1C4EE5..1C4EF3` supplies the four register arguments.

`280E20` uses:

| Register | Observed meaning |
| --- | --- |
| `RCX` | Archive object, saved in `RBX` |
| `RDX` | Address of a qword data-pointer output, saved in `R15` |
| `R8` | Address of a DWORD size output, saved in `R14` |
| `R9D` | Raw 32-bit record ID |

The earlier caller pseudocode includes a possible fifth value `-2`. The complete `280E20` body does not read a fifth stack argument. The caller's stack constant alone does not establish a fifth business parameter.

The nested `280D20` call uses `RCX=archive`, `RDX=input pointer`, and `R9D=requested output length`. Its incoming `R8` is not read. At `280DD0`, `280D20` instead calls `278D20` with `RCX=input`, `RDX=scratch`, `R8D=requested output length`. Neither wrapper supplies the packed byte length to that final call.

The caller uses the data and size outputs after `280E20`; it does not consume the returned `AL` directly. Its missing-data fallback is separate from successful binary parsing and remains subject to the earlier caller evidence.

## 3. `280E20`: lookup, decode, optional remap, and outputs

### Lookup and output clearing

`280E36..280E83` applies FNV-1a 64-bit operations to the four little-endian bytes of the raw record ID. This is not a string-name hash. The map uses archive `+60` bucket endpoints, `+78` mask, and `+50` sentinel. It starts at the selected bucket's first node, follows `next`, and stops at `last.Next`; comparison is the raw DWORD ID at node `+10`.

After lookup, `280ECD..280ED2` initializes the two outputs to null/zero. A missing node returns false through `281031`. A found node also requires archive DWORD `+28 != 0` **or** qword `+20 != 0`; these are alternative conditions, not a requirement that both be nonzero.

`280EEE` takes the record-metadata pointer from node `+18`. Then:

1. `280EFC` loads record DWORD `+28` into `EDI`, zero-extending it.
2. `280EFF` loads record DWORD `+30` into `EBP`.
3. `280F05` adds archive qword `+20` to the input address.
4. `280F0F` calls `280D20`.
5. A false helper result returns immediately, leaving the two outputs zero. That does not prove the archive scratch state was unchanged.
6. On success, `280F31` writes record `+30` to archive `+194`.

### The two exact no-remap guards

For a valid vector layout, let:

```text
offsetCount = (archive[1C0] - archive[1B8]) / 4
dataBase = archive[20]
input = dataBase + zero_extend(record[28])
```

The native instruction uses arithmetic right shift by two on the pointer difference, followed by unsigned comparisons. A safe snapshot domain must establish valid vector pointers and nonnegative, bounded counts rather than interpreting malformed pointers as a legitimate vector.

There are two routes directly to output publication at `281017`:

- `280F49/280F4D`: `offsetCount <= 1`.
- Otherwise, `280F5E..280F68`: unsigned `input < dataBase + zero_extend(offsets[1])`.

If neither guard holds, `280870` is called at `281012`. An empty remap-ID vector at archive `+1A0/+1A8` **does not skip this call**: `280F83` jumps to the call setup at `281002`. Where the vector sizes admit the loop, the code updates `ESI` from remap values for qualifying offset thresholds; the final selected value, initially zero, is passed in `R9D`. An apparently zero remap value is not proof that `280870` would do nothing.

The order matters: decoding and the `+194` write happen before these guards. A self-owned API may reject unsupported remap states during domain preflight, but must label that as its input restriction, not the native function's failure sequence.

### Publication and ownership

`281017..281027` copies archive `+188` to the data-pointer output and archive `+194` to the size output, then returns true. The result points to archive scratch storage. This is not evidence of ownership transfer to the caller. An independent byte-array copy is a separate stable-snapshot conversion and must not be described as native allocation or lifetime equivalence.

## 4. `280D20`: scratch mutations in their actual order

The relevant archive fields are observed as scratch pointer `+188`, capacity DWORD `+190`, and length DWORD `+194`.

1. `280D3F/280D46` compares capacity and requested length **unsigned**. If capacity is sufficient, execution jumps to `280DC3`; pointer, capacity, and the previous length are not reset.
2. When capacity is insufficient, `280D48..280D58` calls `2DC8F8` on a nonnull old scratch pointer. Only afterward does `280D5D` set `+188` to null.
3. `280D68` calculates three times the requested length with 32-bit arithmetic. `280D6B` sets capacity `+190` to zero.
4. `280D75..280D8D` selects the unsigned maximum of that product and 1024, then rounds upward to a multiple of 1024. Native arithmetic can wrap; a bounded successful domain must explicitly exclude multiplication/rounding overflow.
5. With the scratch pointer null, `280D9F` calls `2DC900`. It then writes the returned pointer to `+188` at `280DA4`, the rounded capacity to `+190` at `280DAB`, and zero to `+194` at `280DB1`.
6. `280DC3..280DD0` invokes `278D20(input, scratch, requestedLength)`.
7. `280DED/280DEF` checks the signed decoder result. A nonnegative value reaches `280E14` and returns true. A negative value calls `07CE20` with a stack buffer, `0x80`, static address `0x180348888`, and the decoder result, then returns false. The static bytes and the body of `07CE20` were not inspected here.

Consequences for any future native model:

- Reuse keeps the scratch identity; growth invalidates the old storage before allocating its replacement. Preallocating managed bookkeeping for robustness is not evidence that native allocation precedes native release.
- Native allocation success, allocation failure, and CRT exceptions are separate contracts. This capture does not emulate the allocator or exception runtime.
- A reused scratch retains its old `+194` through the decoder call; the outer `280E20` overwrites it only on success. Growth has already reset it to zero.
- Decoder failure may leave partially written scratch. It cannot automatically be reported as a no-effect failure or safely retried with no state inspection.
- Newly allocated or spare-capacity bytes are not proven zero. A managed implementation must not expose such tail bytes as known native contents.
- A zero requested length and inconsistent capacity/pointer state are not automatically valid inputs to the unknown decoder. They require their own proven domain.

## 5. Wire fields and relocated native metadata

The older [database ownership evidence](../resource-contracts/database-ownership/Engine-function-evidence-bounded-v2.json), SHA256 `6779a5d15657b3058907af317d5e6869e4fa0509b8ec6670dbddd7e27627f0c5`, contains the selected `280160` and `280220` bodies.

`280220` reads a record's ID, a length-prefixed class string, and then five DWORDs. Its class string resides at native record `+8`; ID remapping is a distinct flag-dependent path and cannot be assumed absent for every loaded archive.

| Native metadata | Wire relationship and instruction evidence |
| --- | --- |
| `+28` | `uint32(wireOffset + low32(regionPointer) - low32(archiveBase))`, at `280298..2802A2` and `280352..28035B` |
| `+2C` | Next wire DWORD, named packed length by the independent codec, at `28035E/280361` |
| `+30` | Following wire DWORD, named unpacked length by the independent codec, at `280364/280367`; now observed as the fetch output length |
| `+34`, `+38` | Last two wire DWORDs, retained as timestamp words by the independent codec, at `28036A..28037C` |

The independent [ResourceContainers.cs](../../src/resource_codec/ResourceContainers.cs) reads those same five words and maps the first to an absolute file offset by adding 24. Native record `+28` is instead a relocated offset relative to archive `+20`. Equating these two numeric offsets without accounting for relocation would be wrong.

`280160` obtains a whole data region through `27FF20`, using `recordTableOffset - 24` and an output offset. Under its copy guards it copies from `fileBase + 24`, then calls `02D950(archive + 1B8, &offset)` and returns the acquired base plus the current offset. It does not fetch or decode one record.

The older [followup evidence](../resource-contracts/followup/Engine-function-evidence-bounded-v2.json), SHA256 `3fb2d8de177305c978a6e64d8d2c26130919a4b26593d3c3145bae47642011de`, has a `280000` pseudocode sequence connecting data-region acquisition to table loading. Its saved assembly, however, ends at `2800D3` and already branches outside its selected range to `280143`. Its plan's “complete PE RuntimeFunction range” label does not make it a complete logical-function assembly capture.

Consequently, a single ARZv3 file by itself does not prove that a native archive object has at most one offset entry. The archive's initial vector state, `27FF20` allocation/offset behavior, and `02D950` append behavior are not closed by these two old evidence files. The no-remap domain must be an explicit valid-state premise until native initialization is recovered.

## 6. Decoder identity and the chosen implementation boundary

The existing `docs/resource-contracts`, `docs/resource-reader`, and `docs/object-lifecycle` text was searched for a standalone `278D20` body or a binding to an LZ4 symbol. The only address-specific hits were the new `280DD0` call records. No independent target body, pseudocode, or callee listing was found.

[resource-procedures.json](../resource-reader/rea/resource-procedures.json) does enumerate LZ4 names, but its subject is **x86 ArchiveTool.exe**, SHA256 `6fcbbf7089305a7ae6d9fd64606d4826f38894a727108f85aac0e17fb17079e5`. Its `LZ4_decompress_safe` is `0x40C6F0` and `LZ4_decompress_fast` is `0x40CD00`. Neither address identifies x64 Engine `0x180278D20`. A similar argument shape is not a symbol or implementation proof.

[compiled-lz4-acceptance.json](../resource-contracts/compiled-lz4-acceptance.json) records six selected blocks totaling 402,061 decoded bytes matching the prior Python results. Its own limitation excludes invalid-input parity and game-runtime parity. This supports the independent file-format codec, not a new native-address binding.

The current implementation direction is therefore:

- Read a bounded, explicitly supplied ARZv3 input with the independent raw reader.
- Preserve raw record bytes, complete original string-pool order and IDs, and the record's exact raw name/metadata. Do not use the higher-level `ReadRecord` dictionary conversion as a substitute for native parser input.
- Feed the existing supplied-record/parser/cache continuation with explicit ownership and coverage restrictions.
- Keep the native archive object, its initialization and scratch lifecycle, decoder `278D20`, and remapper `280870` outside the success claim. Do not substitute a success callback for any of them.

Synthetic files can verify the independent reader, budgets, raw-byte preservation, and its integration with self-owned code. They do not establish native malformed-input behavior, native ABI compatibility, runtime archive initialization, remapping, or complete game-load equivalence. No test totals or implementation completion status are asserted here.

Further native closure would need evidence for `278D20`; for paths failing both skip guards, `280870`; and for claims about naturally initialized archive state, the relevant initialization/allocation/vector helpers. Negative-result diagnostics and exceptional allocation behavior also remain outside the normal successful contract. This stage stops additional original-function reads and does not pursue those dependencies.
