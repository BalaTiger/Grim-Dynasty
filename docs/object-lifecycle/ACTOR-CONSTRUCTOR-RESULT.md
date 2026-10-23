# Actor constructor dependency — 2026-10-21

## Exact entry and scope

The saved Game import binding (`offline-questitem-imports/import-bindings.json`,
SHA256 `94d695f5026e1f50a7c1a2968d6f091146db634e28618ef0565b671857aa6f5e`)
maps Game IAT `609700` to Engine.dll `??0Actor@GAME@@QEAA@XZ`.
The stored Item constructor listing calls this slot at Game `3236B3` with the
original `this`; Item preserves that pointer in RDI and overwrites RAX after
the call. Import hint 14 is not an RVA.

The new fixed export lookup resolves name index 14 / ordinal 15 to Engine RVA
`0259A0`. Its exact `.pdata` entry (index 1113) is
`a05902008d5d020048793600`: `[0259A0,025D8D)`, 1005 bytes, unwind RVA `367948`.
The unwind data and exceptional cleanup are outside this investigation.

[Boundary metadata](offline-actor-constructor-boundary/metadata.json) records
934 metadata bytes, zero code bytes read by that lookup, and no subprocesses.
The `code_bytes` property is the discovered length, not a body read counter.
The lookup uses bounded binary search of the exact named export and runtime
function tables. It does not scan either table in full or infer the entry
from the import hint.

Engine original: `D:\Steam\steamapps\common\Grim Dawn\x64\Engine.dll`,
4,485,632 bytes, SHA256
`4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`.
Before/after full hashes use 64 KiB buffers and are separate from the semantic
read budget. Hash/stat stability is not an atomic snapshot.

## Newly observed Actor body

[Body listing](offline-actor-constructor-body/0259a0-instructions.json), SHA256
`59f402f428e04e2928bb485e58fa85896ac08db4548bff6fa33444a58fa521a7`, contains
186 instructions. All 1005 bytes match the independently read fragment, with
one installed dumpbin invocation and a 12-byte `.pdata` recheck. The original
and installed tool hashes remained identical before and after. dumpbin's
internal metadata I/O is not included in the controller's semantic read count.

There are exactly four explicit CALL instructions:

| Actor RVA | Target RVA | Observed input and return use |
| --- | --- | --- |
| `0259BB` | `07D020` | Original RCX/this, retained in RBX. No this adjustment. Return RAX is overwritten. |
| `025C5F` | `022CE0` | RCX still this+`250` from `025A38`, RDX preferred address `180307E61`, R8D=0. Return RAX is not consumed. |
| `025CBC` | `2DC398` | ECX=`358h`; RAX is supplied as the next call's RCX. No null check. |
| `025CC9` | `0BDBE0` | RCX is the preceding return; its own RAX is saved at this+`1E0` and subsequently dereferenced. |

The `0BDBE0` return is not proven equal to its input. The caller writes two
fields on that returned object (`+118=original this`, `+110=0`), after storing
the return in this+`1E0` and setting this+`248`/`4C0` to 1. Replacing the
callee with a callback that returns its input would invent an ABI contract.

The body also reads 16 bytes at preferred address `18034C3B0` into this+`464`,
four bytes at `18034BB44` into both this+`49C`/`4A0`, and a global pointer at
`180421428`. Equality of that global object's qwords `+668` and `+678` becomes
this+`4E1`. These data values and the global object's runtime state are not
read or fabricated in this batch. `+3C0` is a **word** store of 1; its width
must not be collapsed to a boolean or expanded across adjacent bytes.

The normal tail returns original this (`025D7F MOV RAX,RBX`). There is no
ordinary failure-return branch in this fragment. Callee failure, unwind
cleanup, rollback and exceptional final state are not established by that
observation.

## Conditional own field model

The first Actor dependency was then read within its own exact `.pdata`
boundary: Engine `[07D020,07D24A)`, 554 bytes / 106 instructions, entry index
2488, unwind RVA `370CB8`. It cost 168 metadata bytes, one dumpbin invocation,
and no further traversal. See [direct dependency listing](offline-actor-base-dependency/07d020-instructions.json),
SHA256 `64d3237ddb1279a40f12c471d45a9fd02e9b9b043ed00aa52be6730dcf7b4671`.

This body does **not** explicitly call Object `03A6B0`. Its opening writes
the same Object table identity `180339F78`, empty name capacity/length/first
byte, and then overwrites the primary identity with `18033E538`, secondary
identities `18033DF48` at +30 and `18033F078` at +38. It clears qwords
`78/80/88`, initializes the string at `A8`, clears qword `D8`, and clears
dword `140`. These are 15 object-store instructions before `07D0C8`.
The original Object model is a corroborating write-set projection, not a
replacement for this body or proof of a separate Object call.

`ApplyEntryUntilFirstSubobject` follows Actor's first call and implements
exactly that 15-store entry prefix. It stops at `07D0C8`, before argument
setup for the body's sole explicit CALL at `07D0DC -> 0EBDE0`, with
RCX=this+`198`, EDX=4. No `0EBDE0` body or decorated constructor identity is
inferred; in particular this report does not name `07D020` as Entity solely
from its position in the chain. It has not returned to Actor at this stop,
so this prefix does not run Actor's 71-store continuation automatically.

After that call's normal return the saved body installs identity `18033E828`
at this+`198` and performs more scalar initialization. It includes word
`+174=0100`, word `+CB=0`, word `+C8=0`, word `+1B8=0`, and an unaligned qword
`+1BC=0`; widening or realigning these writes would alter neighboring bytes.
The normal tail returns original this. `0EBDE0` effects, exceptional cleanup
and the resulting final exceptional state remain unresolved. That latter
continuation is recorded as evidence, not included in the own entry model.

[ActorConstructorPrefix.cs](../../src/task_items/ActorConstructorPrefix.cs)
also implements `0259C1..025C55`, conditional on `07D020` having returned
normally with valid, exclusively owned storage. It neither invokes nor
certifies that prerequisite. It performs the following 71 observed stores,
in observed order:

| Field range | Observed stores |
| --- | --- |
| `00,30,38` | Qword identities `18033A0D8,18033A098,18033F078` respectively. |
| `1E8,1F0,1F8` | Three zero qwords. |
| String positions `208,228,250,270,290,2B0,2D0,2F0,388,3D8,440,4E8` | Each capacity(+18)=15, length(+10)=0, first payload byte=0, in that order. |
| `310..380` at stride 8 | Fifteen zero qwords. |
| `3D0` | Dword `FFFFFFFF`. |
| `418,400,410` | Zero bytes, interleaved with the following scalar stores as in the listing. |
| `420,438,3F8` | Zero qwords. |
| `404,414` | Dwords `3F4CCCCD`, `40B00000`; retained as bits, with no field-name inference. |
| `408` | Qword `AC44`. |
| `510,518,520` | Three zero qwords. |
| `200` | Last zero byte before the next call setup. |

Because capacity has just been set to 15, each observed comparison takes the
inline arm on valid exclusive storage. Only the first inline byte is zeroed;
the other 15 inline bytes retain their supplied/unknown values.

The own nullable-byte projection has extent `530` for the observed fields;
this is not proof of sizeof(Actor), valid native allocation size, native
object storage or callable vtables. Preferred-image table values are identity
tokens, not runtime pointers. Unwritten bytes remain nullable or retain their
supplied observation. Both results preserve the same state identity and report
`KnownPrefix`; the post-base variant resumes at `025C55`. They always report
`IsCompleteConstructor=false`. The pending helper is separately identified
as `025C5F -> 022CE0`, with this+`250` and length zero.

Null own state is an unsupported API input with zero effects; it is not an
observed native null check or failure policy. The own result is allocated
before stores. No callback, exception catch or cleanup is used to stand in
for native constructor behavior. Existing Object, Item, dynamic factory and
registration implementations remain untouched. Wiring this conditional
fragment as a complete factory would skip unresolved work.

## Validation status

The exact new [build script](../../src/task_items/build_actor_constructor_prefix.ps1)
was inspected with its input/output scope, then executed once through a new
formal `require_escalated` request using Windows PowerShell 5.1. It compiled
one own library source and one new fixture source with the installed .NET
Framework C# compiler (`/noconfig /langversion:5 /checked+ /optimize+ /debug-`).
No old build script, static reader or original DLL was executed by the build.
The exact requested operation completed with exit 0 (tool chunk `19dc6f`).

**48 new assertions / 8 scenario groups passed** (7+6+5+6+5+5+8+6):

| Fixture group | Assertions | Covered risk |
| --- | ---: | --- |
| UnknownObservation | 7 | Exactly 444 post-base bytes become known, including full 64-bit identity tokens; untouched fields stay unknown. |
| MixedInputAndNarrowWrites | 6 | Mixed unknown/nonzero input, qword AC44 width, dword and byte neighbor preservation. |
| TwelveInlineStrings | 5 | Twelve first-byte/length/capacity sets; all 180 other inline bytes remain unchanged. |
| StopBeforeDependencies | 6 | Correct next call metadata; no guessed allocated-object, constant-load or global-derived field values. |
| IdentityRepetitionAndIsolation | 5 | Same state identity, local repetition and separate state/snapshot isolation. |
| OwnGuardsAndOutputCopies | 5 | Explicit unsupported null, fixed observation bounds and snapshot ownership. |
| EntryUnknownObservation | 8 | Exactly 94 final known bytes from 15 stores (the primary token is overwritten), stop inside 07D020. |
| EntryMixedNeighborsAndStop | 6 | Existing Object-like input is not treated as base completion; string tails, +140 dword neighbors, and all bytes from +198 onward remain unchanged. |

These fixtures do not invoke unknown prerequisites. Local segment repetition
is an own observation test, not proof that reconstructing an already live
native object is valid. The two segments are never automatically chained.
**Zero old suites and zero old implementation API cases were rerun.** Six
prior Object/name/registration/Item/dynamic-query sources were hash-checked
unchanged. The tracked diff remains the prior 12 build scripts, +60/-46;
HEAD remains `6dfdae419d1c92f7738eefe241937057d83134df`.

Records: [reviewed preflight](actor-constructor-prefix-validation/preflight-reviewed.json),
[execution and exact output](actor-constructor-prefix-validation/run.json),
[input/output and preservation hashes](actor-constructor-prefix-validation/postbuild-inventory.json).
The first attempt to record preflight used a bare PowerShell `false` literal
and wrote a null JSON record; it never invoked a compiler or test. That null
record is retained separately from the corrected reviewed preflight. There
was only one build/test invocation and no failed build retry.

| New artifact | Bytes | SHA256 |
| --- | ---: | --- |
| `ActorConstructorPrefix.cs` | 5502 | `c51710c512abe864d16feecd7972a779804d9d0cdc419b117e86ba7e37134b83` |
| `ActorConstructorPrefixTests.cs` | 19320 | `a68136dbca5dbe2792eed4a90e9dfd5bafba4f49463282a8fde4d588555f1e58` |
| `build_actor_constructor_prefix.ps1` | 4603 | `12919611d07735aab7b09bcb528528b5c2dfcba7b9ccd819e2c29d06d1e1172d` |
| `GrimDynasty.ActorConstructorPrefix.dll` | 6144 | `8a9702cfc05fac43785cc881b1d87857c9982dfbbc5512de9a5f59b7186a7fc3` |
| `ActorConstructorPrefixTests.exe` | 21504 | `643cca482c0dd14fab5806af02261314706a70e08cb6f04aadd8b18f6fa15988` |

## Scope, checkpoint and next evidence

This batch's original reads were limited to Engine: **1559 code bytes,
1114 metadata bytes, 292 decoded instructions**, two dumpbin calls. There
were six full Engine fingerprint passes with 64 KiB buffers; all matched
the original pinned hash. Three Node controllers peaked at 42728, 43052 and
43156 KiB respectively; this is controller RSS, not a combined system or
compiler/tool memory measurement. No Game binary reread was necessary.

The next smallest native dependency is **Engine `0EBDE0`**, justified by
the exact call at `07D0DC`, with this+`198` and EDX=4. Its body, bounded
metadata and any direct dependencies are not read in this batch. Closing it
is needed before the 07D020 continuation can be treated as reachable normal
initialization. Actor still separately requires `022CE0`, `2DC398`,
`0BDBE0`, the two fixed constants, and the global object's runtime contract.
Nothing here establishes their exception cleanup or an end-to-end factory.

Checkpoint: `.tooling/checkpoints/actor-constructor-prefix-complete/manifest.json`.
It contains the selected own source, evidence fragments and provenance,
validation records and new build artifacts, with source/copy size and SHA256
verification. No original DLL, game executable, player save or cloud data is
included. No game/Rea/Java was launched; no installation, security setting,
commit or push occurred. Full Actor/Item construction, native ABI/EH and
formal sample compatibility remain explicitly unverified.
