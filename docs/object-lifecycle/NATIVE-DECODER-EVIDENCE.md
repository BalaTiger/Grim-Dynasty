# Engine output-size decoder: evidence and safe-profile boundary

Date: 2026-10-20.

The archived body at `0x180278D20` implements an output-size-terminated, LZ4 fast-style algorithm. Its token grammar, length extensions, distance handling, copy tables, termination rules, and integer return values are now supported by direct static evidence. This establishes the observed algorithm; it does not identify a particular upstream library version or exported symbol.

The new safe profile is an explicitly restricted, self-owned implementation domain. It must distinguish its own `Unsupported` guards from native error returns and must not imply native malformed-input parity or absence of native partial effects. This document records evidence and the implementation contract, not test acceptance or official-archive compatibility.

The document reviewer read only existing project evidence and source text. No backend, original-binary execution, new original-file read, test execution, or analysis of `280870` was performed while writing it.

## 1. Fixed target and four static records

All four records identify the same original `D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll` and report unchanged original SHA256 before/after and unchanged existing working-copy SHA256 afterward:

```text
4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153
```

The working copy is `.tooling/offline-cache-node/Engine.readonly.dll`. These statements report the archived checks; they are not a fresh hash calculation by this document.

| Record | Observed scope | Record SHA256 |
| --- | --- | --- |
| [Boundary](offline-native-decoder-boundary/boundary.json) | PE metadata only; first unwind region `278D20..278DA8`; zero code bytes and no dumpbin invocation | `70af4ef2fee61350670cbeaf7eb3bac340974a1ebef12580007c38009de9de72` |
| [Entry inspection](offline-native-decoder-entry/inspection.json) | `0x180278D20..0x180278DA8`, 136 code bytes / 32 instructions | `58bee24f6b187154945c8dcc845dea5e71fe788009444d98eaa822dc0333d5dc` |
| [Body inspection](offline-native-decoder-body/inspection.json) | `0x180278DA8..0x180278F9C`, 500 code bytes / 142 instructions | `a9dc1129677821a18ee77e24414cc59275fb915c8428fdb490f893c3a52fa39a` |
| [Copy tables](offline-native-decoder-boundary/copy-tables.json) | Exactly two directly referenced 16-byte data ranges; zero new code bytes | `544109ec17d9d87c6d95fd4239b4c86ccc2f57aa901dda69ade4067324eb267a` |

Ranges are half-open. Entry and body together cover **636 code bytes / 174 instructions**, captured by two dumpbin invocations. The separate table read adds **32 directly referenced data bytes**. These totals exclude necessary PE metadata and streaming whole-file hash reads.

Both code inspections report exit 0 and complete byte coverage. The existing Microsoft dumpbin tool SHA256 is `cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815`. The original was neither loaded nor executed; no Rea/Ghidra backend was started.

The first `.pdata` range is a fragment, not a complete logical function: it branches to `278DA8` and `278F85`. The body capture joins the required continuation regions and reaches `RET` at `278F9B`. The source for the selected address was the already archived `280DD0 CALL 0x180278D20`.

Instruction-level references below use the [entry instruction list](offline-native-decoder-entry/278d20-instructions.json) and [body instruction list](offline-native-decoder-body/278d20-body-instructions.json). Readable dumpbin text is available for the [entry](offline-native-decoder-entry/278d20-dumpbin.txt) and [body](offline-native-decoder-body/278d20-body-dumpbin.txt).

## 2. Arguments, zero output, and return values

The observed register contract is:

| Input | Meaning |
| --- | --- |
| `RCX` | Compressed-input pointer; retained as the original base in `RBP` and current pointer in `RBX` |
| `RDX` | Output pointer; initial current output in `R9` |
| `R8D` | Requested output length, interpreted as signed int32 by `MOVSXD` at `278D56` |

There is no packed-input length or input-end argument. `RSI` becomes the output-end pointer by adding the sign-extended requested length to the output base. The code does not explicitly reject negative requested lengths, null pointers, pointer-arithmetic overflow, or unreadable memory. Such states are outside a safe valid-input domain.

For requested output length zero, `278D95..278DA3` still reads `input[0]`:

- If that byte is zero, return `EAX=1`.
- Otherwise, return `EAX=0xFFFFFFFF` (signed `-1`).

This consumes/inspects one input byte even though no output bytes are requested. It is not evidence that an empty or inaccessible input pointer is accepted.

For nonzero output:

- Success is reached only through the terminal-literal path at `278F59..278F71`, where the logical literal end must equal the requested output end.
- `278F6C/278F6E` returns the low 32 bits of `currentInput + terminalLiteralLength - originalInput`: the consumed input byte count, not the produced output count.
- The explicit error path at `278F73/278F75` returns the low 32 bits of `originalInput - currentInput - 1`. Within the ordinary bounded range this is `-(bytesConsumedSoFar)-1`.

The instructions use 32-bit result arithmetic. Arbitrarily large input progress could wrap these bit patterns; the safe profile must not extrapolate bounded positive/negative classifications to that unsupported range.

There is no final check against a supplied packed-input length because no such parameter exists. Bytes after the terminal literal sequence are not consumed merely because they are present in an enclosing buffer.

The caller `280D20` uses only the sign of the returned int32 to choose success/failure. Its scratch mutation and outer `280E20` publication sequence remain documented in [ARCHIVE-FETCH-EVIDENCE.md](ARCHIVE-FETCH-EVIDENCE.md).

## 3. Token grammar and the two proven copy-adjustment tables

At `278DC0..278DCF`, the decoder reads a token and takes the high nibble as literal length. A nibble of 15 enters the extension loop at `278DE0..278DEE`, adding bytes until one is not 255. There is no packed-input-end check in that loop.

After nonterminal literals, `278E2D` reads a little-endian unsigned 16-bit distance. The low token nibble supplies match length; 15 uses the analogous extension loop at `278E41..278E50`. `278E52` adds the minimum match length of four.

For distances below eight, the observed copy adjustments are:

```text
inc32 = [0, 1, 2, 1, 4, 4, 4, 4]
dec64 = [0, 0, 0, -1, 0, 1, 2, 3]
```

These are analysis labels for the arrays, not recovered symbol names.

- `inc32` is fully built by immediate stores at stack `+20..+3F` in the entry code.
- `278D39` loads 16 bytes at `0x18034C470`, saved at stack `+40`: int32 values `[0,0,0,-1]`.
- `278D44` loads 16 bytes at `0x18034C000`, saved at stack `+50`: int32 values `[0,1,2,3]`.
- Their concatenation is exactly the `dec64` array above. In particular, its distance-four entry is **zero**; no remembered upstream table should replace these observed bytes.
- `278E7D..278E90` indexes the first table to advance the match pointer, copies a DWORD, then sign-extends and subtracts the second table entry.

For valid positive distances into already produced output, these adjustments implement the overlapping repetition needed by the token grammar. This does not add a native distance-validity check.

## 4. Output-end thresholds and speculative copying

Let `op` be the logical current output, `oend` the requested output end, `literalEnd = op + literalLength`, and `matchEnd` the logical end after the match.

### Literal threshold: `oend - 8`

`278DF4/278DF7` compares `literalEnd` with `oend-8`.

- If `literalEnd <= oend-8`, the loop at `278E10..278E1E` copies qwords until its physical copy pointer reaches or passes the logical literal end.
- This is a do-while style copy: even a literal length of zero performs one 8-byte read and write.
- Input and output logical positions are then advanced by the literal length, not by the rounded-up physical copy amount.
- If `literalEnd > oend-8`, the decoder takes `278F59`; only `literalEnd == oend` is accepted, using the exact-size final copy.

Thus a native literal copy may read beyond the logical literal bytes into following compressed data and may write beyond the current logical literal end. The absence of an input-length parameter matters here.

### Match thresholds: `oend - 12` and `oend - 5`

Before testing the final match bounds, the function has already written the distance DWORD and copied the first eight bytes of match data.

- At `278EA7/278EAA`, **match end**, not match start, is compared with `oend-12`. Equality takes the fast path.
- That fast path writes a second qword at `278F1F/278F22`, even for matches shorter than 16 bytes. Longer matches continue qword copying until the logical end is reached/passed.
- If `matchEnd > oend-12`, the tail path at `278EB4/278EB7` rejects `matchEnd > oend-5`. Equality with `oend-5` is allowed.
- The accepted tail path copies qwords while their starting address is below `oend-7`, then uses a byte loop as needed. Logical progress resumes at `matchEnd`, even if physical copying went farther.

The `end-12` test chooses a copying path; it is not a blanket claim that every match must end 12 bytes before output end. The `end-5` check enforces the remaining final-literal margin for matches that reach the tail path.

Under a valid output span, ordinary nonnegative bounded sizes, nonoverflowing pointer arithmetic, and readable copy sources, these direct writes remain within the requested output span. They can nevertheless overwrite bytes beyond the logical current literal or match end. That destination-bound observation does not prove input safety, valid match history, or exception parity.

## 5. Distance zero, missing history checks, and partial effects

There is no explicit `distance == 0` or `distance > producedBytes` rejection.

At `278E2D..278E38`, the code reads the distance, computes `match = logicalOutput - distance`, and immediately stores the distance as a DWORD at logical output. This store occurs before the match-extension loop and before the tail-match rejection.

For distance zero:

1. The seed DWORD is zero.
2. `278E5F..278E79` reads/writes the first four bytes from that same location.
3. `inc32[0]=0`; `278E85/278E87` copies that zero DWORD to output `+4`.
4. `dec64[0]=0`; subsequent copies continue from the now-written data.

Therefore zero distance is not simply a guaranteed native error or an unavoidable uninitialized-memory read. One statically traceable example is a zero-literal, distance-zero, four-byte match followed by nine final literals:

```text
00 00 00 90 61 62 63 64 65 66 67 68 69
requested output = 13
```

Within valid readable/writable buffers, the observed path yields four zero bytes followed by `abcdefghi`, with consumed-input result 13. This is a control-flow derivation, not execution of the original DLL, and it is outside the safe profile's selected positive-distance domain.

For distance greater than already produced output, the match pointer lies before the output start. No recorded check converts that into a controlled negative result. Whether the read succeeds, faults, or sees unrelated bytes depends on memory outside the self-contained output domain. This is not proof of an intentional dictionary API.

Errors are also not rollback:

- Earlier literals/matches remain written.
- The distance seed and first eight match bytes are written before the `oend-5` rejection.
- A later source read, extension read, or copy helper can fail after prior writes.

A safe self-owned implementation may refuse these operations or discard its private output on failure. It must not describe that policy as evidence that native output or archive scratch remains unchanged.

## 6. Final-copy import binding and runtime limits

The final literal path calls `0x1802DDB0E` at `278F67`, with `RCX=output`, `RDX=current input`, and `R8=literal length`.

The earlier [fixed import-binding evidence](name-interface-discovery/name-copy-thunk-0x1802ddb0e.json), SHA256 `78874df16fdbda694338800a35aa327a39a5db73cd0ec0e2d46ad54dca5f0549`, is pinned to the same Engine SHA256 as this capture. It records:

```text
0x1802DDB0E: FF 25 54 4C 02 00
JMP qword ptr [0x180302768]
normal import: VCRUNTIME140.dll!memcpy
```

This is an on-disk normal-import association, not runtime resolution or a new execution of the CRT. Normal successful, valid nonoverlapping-copy semantics are the applicable contract. The CRT implementation, faults, allocation/runtime behavior, and exceptional ABI are not reconstructed here.

The epilogue also calls `2DD110` following the recorded cookie load/XOR sequence. That target was not followed; a compiler/runtime check is not a recovered format-error handler.

## 7. Comparison with the existing `Lz4Block.Decode`

The existing [Lz4Block.cs](../../src/resource_codec/Lz4Block.cs) has SHA256 `821dfbfe3695482b0da88b5dc8a948600b6c98c4015c1ae978dbd23b1105be24`. It is an independent raw-block codec with its existing contract; this evidence does not silently redefine that contract.

| Concern | Observed `278D20` | Existing `Lz4Block.Decode` |
| --- | --- | --- |
| Input extent | Pointer only; no packed end | Complete byte array with explicit bounds checks |
| Output | Writes caller-provided storage | Allocates and returns a new byte array |
| Termination | Output length reached by final literals | Iterates through the supplied input, then checks output size |
| Success result | Consumed input count, low int32 | Output byte array |
| Trailing input | Can remain unread after success | Included in the supplied block and processed/validated |
| Zero output | Reads first byte; zero gives result 1 | Empty input with zero output is accepted |
| Distance validation | No zero/history guard | Rejects zero or distance exceeding produced output |
| Tail structure | `end-8` literal path and `end-5` match margin | No corresponding native tail-margin requirement |
| Length/input errors | Selected output tests plus unconstrained pointer reads | Checked lengths and controlled managed exceptions |
| Failure output | Caller storage may already be modified | Partially built private array is not returned |

For example, the existing codec's positive self-test `{0x14, 'a', 1, 0}` with expected output 9 terminates in a match and is accepted by that codec. If the native input pointer has sufficient readable backing for its speculative qword read, `278D20` reaches a match ending at `oend`, then rejects it at `278EB7`; the input consumed at that point is four bytes, giving error result `-5`, after output writes. With insufficient readable backing it could fail earlier as a memory access, which is another reason not to equate a bounded array test with native malformed-input execution.

The earlier successful file-format samples remain evidence for the old codec's selected valid blocks. They do not prove that all its accepted inputs satisfy this newly observed output-size profile.

## 8. Safe-profile contract and remaining scope

A safe output-size profile may implement the proven successful token semantics with managed byte-wise overlapping copies rather than reproducing speculative qword traffic. Its valid domain must be explicit:

- Known readable input bytes and a finite nonnegative output budget.
- Checked index/length arithmetic and a bounded consumed-input result.
- Positive distances referring only to already produced bytes; no hidden prefix/history or alias assumptions.
- The observed terminal-literal and tail-match conditions.
- Sufficient logical input for the selected successful parse; no assertion that the original pointer was readable outside that logical extent.
- Normal successful managed allocation and copy behavior.

A native output-bound error encountered within the modeled input domain can retain its proven consumed-position/error-code relation. A safety guard for truncated input, zero distance, unavailable history, overflow, or an implementation budget instead returns **`Unsupported`**; it must not invent the native error code or claim that the native function would reject at the same point.

Failure does not expose the profile's private partially written output. This is a self-owned API guarantee, not a native no-effect guarantee: the original function and its enclosing archive scratch can already have changed. Zero-output handling and trailing-input consumption likewise require explicit profile results rather than inheriting the old codec's empty-block behavior.

The semantic safe-profile implementation and its actual tests are reported separately. No test totals, official-sample compatibility, or implementation acceptance are asserted in this evidence document.

This stage closes the bounded static decoder analysis and the two direct data references. It does not recover archive initialization, `280870` remapping, multi-archive merging, native allocation/exception behavior, or the complete native LoadFile/object factory. Historical [ARCHIVE-FETCH-EVIDENCE.md](ARCHIVE-FETCH-EVIDENCE.md) correctly records that the decoder was unknown at its earlier stage; this document supplies the later decoder evidence without rewriting that history.
