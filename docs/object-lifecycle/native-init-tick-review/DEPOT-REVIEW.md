# Depot constructor: saved import binding and bounded normal contract

2026-10-20. This review reads project evidence only. It adds no original/copy binary read, backend, tool execution, test run, or production-code change.

The fixed Engine identity is SHA256 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`. Addresses below are preferred static VAs in that image.

## Reuse the saved `InitializeCriticalSection` binding

The first item of [the saved round-4 Engine evidence](../../phase-1/round-4/Engine-function-evidence-bounded-v2.json) describes `??0Engine@GAME@@QEAA@XZ`, range `180072930..180072F5A`. Its `outgoingReferences` contains:

| Source instruction | Referenced slot | Saved symbol |
| --- | --- | --- |
| `0x180072AD0` | `0x180302318` | `PTR_InitializeCriticalSection_180302318` |
| `0x180072B73` | `0x180302318` | `PTR_InitializeCriticalSection_180302318` |
| `0x180072D01` | `0x180302318` | `PTR_InitializeCriticalSection_180302318` |

The matching `instructions` entries are `CALL qword ptr [0x180302318]`. The saved evidence explicitly says that only the selected bounded bodies were disassembled/decompiled, without auto-analysis; callee bodies were not analyzed.

[The saved module inventory](../../phase-1/boundary-audit/module-boundaries.json), entry `relativePath="x64/Engine.dll"` with the fixed hash above, independently lists `InitializeCriticalSection` under `importModules[module="KERNEL32.dll"].symbols`. That inventory does not retain a slot offset. Together these are an existing Ghidra slot-symbol association and a PE import inventory; **this review does not claim a new independent OFT/IAT/descriptor reread** or a live Windows binding.

The new [TableDepot instructions](../offline-depot-ctors/1c4ad0-instructions.json) call the same slot at `1C4B3C`. [ObjectManager instructions](../offline-depot-ctors/17dbf0-instructions.json) also call it at `17DC3F`, `17DC6A`, and `17DCB6`. Thus the saved association can be reused without another original read. No new single-slot inspection script is needed.

## Constructor order and the unopened LoadTable member

For depot base `D`, the saved `1C4AD0..1C4B7B` range shows this normal-return order:

1. Store the depot vptr, then call `0x18003C740` with `D+8` at `1C4B03`.
2. Prepare the gate member at `D+68`; set its pointer at `D+78` to zero, request `0x28` bytes through `2DC398`, store the returned pointer, and pass it to slot `302318`.
3. `1C4B43` stores zero as a qword at `D+50`, covering both load/hit DWORDs. `1C4B47` clears the fallback-count DWORD at `D+58`.
4. Request `0x30` bytes at `1C4B4D`; pass the allocation to `0x18013C1D0` at `1C4B5A`; store its returned value in `D+48` at `1C4B60`.
5. `1C4B64` stores zero as a qword at `D+5C`, covering elapsed-total/max DWORDs, and the constructor returns.

`ObjectManager` forms `this+170` at `17DD0A` and calls this depot constructor at `17DD11`, linking the member to the already saved CreatePartial/FinishPartial LoadFile calls.

For the **non-fallback Binary-success path**, the `D+48` member can remain an opaque, unread result under an explicit external contract: the observed allocation and `LoadTable` construction complete normally with valid storage. Its internal fields, actual runtime address, destructor, and exceptional behavior are not recovered here. The projection need not execute an unknown constructor or invent a getter callback; it must not expose fabricated contents as known.

The saved [LoadFile wrapper](../rea-object-base/11-TableDepot-LoadFile-0x1801c4d20-assembly.json) and [miss body](../rea-record-load-dependencies/11-LoadFile-miss-0x1801c4e20-assembly.json) do not read `D+48` on this selected path. Their successful Binary path obtains a separate table from `13F300` and the parser. Even the visible fallback branch at `1C5035..1C5046` allocates and constructs a new `LoadTable`; it does not establish that `D+48` is that branch's returned table. “Fallback/default table member” is therefore only a descriptive label, not a recovered general-use contract.

This unread member alone does not require stopping the selected normal path. It also does not prove complete native depot construction or fallback behavior. If allocation, gate initialization, or `13C1D0` does not complete normally, the zeroed tail and constructor return cannot be claimed: elapsed/max clearing occurs after that call. No native failure rollback or exception cleanup is inferred.

## Remaining cache-construction dependency

The normal empty-cache projection still requires the direct body of `0x18003C740` (RVA `0x3C740`), reached with `C=D+8` at `1C4B03`. Its separate investigation is owned by the parent task. The current constructor body alone does **not** set a justified bucket count, capacity, mask, sentinel/list identity, or float load factor; those must come from the actual helper evidence, not a standard-library default or an empty managed dictionary. This review neither expands that query nor preclaims its result.

## Evidence fingerprints

| Project artifact | SHA256 |
| --- | --- |
| `phase-1/round-4/Engine-function-evidence-bounded-v2.json` | `ab597744f4a4cf617c6227fe7a50100e5b03733aa48dda43a730f65509bdca26` |
| `phase-1/boundary-audit/module-boundaries.json` | `d1ec79916baeb45b3257f7c7c2ce356c262f804ae9b2577888a0d14de53e7ba7` |
| `phase-1/round-4/initialization-locations.json` | `bdc0e1838162463d18fce89dfea20fe6baf073d889db757e62c59ae3ed8faf1e` |
| `offline-depot-ctors/1c4ad0-instructions.json` | `444df3e2546b1e31b22d676c2d75e5be81ead66249d681c5d5e321892bb5f03c` |
| `offline-depot-ctors/17dbf0-instructions.json` | `f13fec74ace3f5085eb535780de04fd3fd5b2962e20d93bd83e486aeff98f6a1` |
| `offline-depot-ctors/inspection.json` | `46e043885cca33285e62baf364a714561c14b4bf22f6dd097e3172c8b98675b7` |

The [two-range inspection record](../offline-depot-ctors/inspection.json) reports 511 code bytes, 616 metadata bytes, exact byte coverage, no failures, and unchanged original/copy hashes. These are the parent's completed inspection results, not additional reads performed by this review. No product acceptance counts are added here.
