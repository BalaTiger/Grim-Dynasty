# Independent review: Engine root writer helpers

Date: 2026-10-20.

The saved instructions establish the direct allocation/retry loop, old-storage
tail-jump chain, and callback-registration result mapping used by the three
Engine root writers. The separately bounded IAT report now independently binds
all seven selected imports. Concrete exception types, external CRT execution,
complete native initialization, and the full runtime root graph remain unproved.

This review reads project reports and instruction JSON only. It does not read
the original DLL, run any script/disassembler/compiler/test, load a DLL, or
start a backend. It does not retry the previously refused build or perform an
equivalent action. The earlier
[root-writer review](ENGINE-ROOT-INDEPENDENT-REVIEW.md) is left unchanged.

## Provenance and stopped batches

Pinned Engine image: 4,485,632 bytes, preferred base `180000000`, SHA256
`4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`.
Addresses below are RVAs unless a full preferred VA is shown; they are not
observed runtime addresses.

| Saved batch | Status | Code bytes | Metadata bytes | Saved dumpbin calls |
|---|---|---:|---:|---:|
| [helper-entries](offline-engine-root-static-helper-entries/inspection.json) | Failed at boundary guard; no code acquired | 0 | 168 | 0 |
| [helper-entry-prefixes](offline-engine-root-static-helper-entry-prefixes/inspection.json) | Verified | 39 | 168 | 3 |
| [helper-direct-targets](offline-engine-root-static-helper-direct-targets/inspection.json) | Verified | 147 | 240 | 3 |
| [helper-import-thunks-and-failure-wrappers](offline-engine-root-static-helper-import-thunks-and-failure-wrappers/inspection.json) | Verified | 94 | 216 | 7 |
| [helper-throw-targets](offline-engine-root-static-helper-throw-targets/inspection.json) | Failed after one verified thunk | 6 | 204 | 1 |
| [helper-constructor-prefixes](offline-engine-root-static-helper-constructor-prefixes/inspection.json) | Verified, fixed 32-byte prefixes | 64 | 180 | 2 |
| Total for these six batches | Two stopped batches retained | **350** | **1176** | **16** |

The first stop reports `No exact unwind start and no explicit prefix` and has
empty code reads/fragments. The later prefix batch explicitly limits the two
gap reads to eight bytes each; it does not pretend that a gap is a whole unwind
function. The final stopped batch retains the verified six-byte `2DDB26` thunk.
It then stops at the missing exact boundary for `2DD508`; that stopped batch
supplies no body for `2DD508` or `2DD568`. A subsequent explicit-prefix batch
acquires 32 bytes at each address. This does not change the failed batch into
an all-targets pass or disable its boundary guard.

All six reports retain matching original/tool before-and-after hashes.
The acquired fragments report byte-coverage verification and dumpbin exit 0.
These are observations in saved reports, not actions repeated by this review.
Unwind boundaries, thunk padding and acquisition success do not establish
exception-path completeness.

## Allocation request and retry

Sources: [2DC900](offline-engine-root-static-helper-entry-prefixes/2dc900-instructions.json)
and [2DC398](offline-engine-root-static-helper-direct-targets/2dc398-instructions.json).

`2DC900` tail-jumps to `2DC398`; its remaining three acquired bytes are `INT 3`
padding. The visible `2DC398..2DC3D4` body saves the incoming RCX request in RBX.
The first operation on that request is the call at `2DC3C4` to `2DDB38`,
not a call to the retry handler.

The exact decision sequence is:

1. Call `2DDB38` with the original 64-bit request.
2. If RAX is nonzero, return that same pointer at `2DC3D3`.
3. If RAX is zero, call `2DDB7A` with the same request at `2DC3A6`.
4. If its **EAX is any nonzero value**, reload the request and retry the first
   call. Positive and negative nonzero values take the same branch.
5. Only if that EAX is zero, compare the request with `UINT64_MAX`.
   Equality selects `2DD600` at `2DC3B5`; other requests select `2DD5E0`
   at `2DC3BC`.

There is no visible size-zero normalization, retry bound, null-return success,
or immediate failure merely because the request equals `UINT64_MAX`.
If either failure wrapper returned normally to this caller, the visible caller
would retry; the wrapper bodies instead end at `INT 3` after another call.
Neither fact licenses inventing a normal null-return path.

The writer has already stored its name before this allocation helper is
reached. If allocation or its handler fails to return, that earlier write is
not rolled back by the visible code.

## Old-storage release path

Sources: [2DC8F8](offline-engine-root-static-helper-entry-prefixes/2dc8f8-instructions.json),
[2DC390](offline-engine-root-static-helper-direct-targets/2dc390-instructions.json),
and [2DDB3E](offline-engine-root-static-helper-import-thunks-and-failure-wrappers/2ddb3e-instructions.json).

The chain is `2DC8F8 -> 2DC390 -> 2DDB3E -> [180302970]`.
The first two entries contain a tail jump followed by three padding bytes;
the final thunk is a six-byte indirect jump. No argument transformation,
local null check, table-payload destruction, or rollback is visible in this
chain. The independently bound final import is `free`, as recorded below.

In the root writers, append and count increment precede this chain; publishing
the new children pointer follows its return. Thus a failure here can leave a
new count with the old children pointer still published. A normal external
release contract is a condition on this path, not proof of atomic publication.

## Callback registration and return mapping

Sources: [2DC890](offline-engine-root-static-helper-direct-targets/2dc890-instructions.json)
and [2DC8E0](offline-engine-root-static-helper-entry-prefixes/2dc8e0-instructions.json).

Let `C` be the incoming callback pointer, `K = qword[18041C520]`, and
`W = qword[180420D50]`. The helper computes
`ROR64(K XOR W, K & 63)`. This is the observed arithmetic; these global values
are not independently sampled runtime state.

- If that decoded value is `UINT64_MAX`, call `2DDBA4(C)` at `2DC8B8`.
- Otherwise call `2DDB98(&[180420D50], C)` at `2DC8C9`.
- At `2DC8D0`, test the external **32-bit EAX for exact zero**. Zero returns
  the original 64-bit callback `C`; every nonzero value returns zero.

The outer `2DC8E0` calls this helper, then maps its RAX to EAX:
nonzero RAX becomes `0`, zero RAX becomes `0xFFFFFFFF` (32-bit `-1`).
The EAX write zero-extends the architectural RAX; it is not a 64-bit all-ones
result. In particular, a null callback produces outer `-1` even when the
external call returned EAX zero. Nonzero positive error codes must not be
misclassified as success.

This result is produced after the writer's new children storage has already
been published. A returned failure code does not undo those writes. Nor does
registration imply that the callback was invoked, will run successfully, or
has been restored as an independent implementation.

## Failure wrappers and IAT addresses

The saved wrappers
[2DD5E0](offline-engine-root-static-helper-import-thunks-and-failure-wrappers/2dd5e0-instructions.json)
and
[2DD600](offline-engine-root-static-helper-import-thunks-and-failure-wrappers/2dd600-instructions.json)
reserve stack space, pass `rsp+20` to `2DD508` or `2DD568`, then pass that
stack address in RCX and respectively `1803AF558` or `1803AF5B8` in RDX to
`2DDB26`. An `INT 3` follows.

The subsequent saved prefixes
[2DD508](offline-engine-root-static-helper-constructor-prefixes/2dd508-instructions.json)
and [2DD568](offline-engine-root-static-helper-constructor-prefixes/2dd568-instructions.json)
each reach `RET`, followed by one acquired `INT 3` padding byte. They call no
other helper. With incoming destination pointer RCX, their exact store order is:

| Entry | First write | Second write | Third write | Return |
|---|---|---|---|---|
| `2DD508` | `qword[this+10] = 0` at `2DD50A` | `qword[this+08] = 180307488` at `2DD515` | `qword[this+00] = 180307478` at `2DD520` | RAX = this |
| `2DD568` | `qword[this+10] = 0` at `2DD56A` | `qword[this+08] = 1803074B0` at `2DD575` | `qword[this+00] = 1803074A0` at `2DD580` | RAX = this |

These field writes are now directly evidenced; their address operands and the
wrapper's RDX metadata operands are not decoded exception class names. Invalid
destination writes can fail after an earlier store; no rollback is visible.

| Saved entry | Observed indirect target | Independently bound import |
|---|---|---|
| `2DDB38` | IAT `302968` | `api-ms-win-crt-heap-l1-1-0.dll!malloc` |
| `2DDB3E` | IAT `302970` | `api-ms-win-crt-heap-l1-1-0.dll!free` |
| `2DDB7A` | IAT `302960` | `api-ms-win-crt-heap-l1-1-0.dll!_callnewh` |
| `2DDBA4` | IAT `302A70` | `api-ms-win-crt-runtime-l1-1-0.dll!_crt_atexit` |
| `2DDB98` | IAT `302A88` | `api-ms-win-crt-runtime-l1-1-0.dll!_register_onexit_function` |
| [2DDB26](offline-engine-root-static-helper-throw-targets/2ddb26-instructions.json) | IAT `302708` | `VCRUNTIME140.dll!_CxxThrowException` |
| Root writers' earlier name call | IAT `302BB8` | `api-ms-win-crt-string-l1-1-0.dll!_strdup` |

The [import metadata report](offline-engine-root-helper-imports/metadata.json),
SHA256 `0b81751351efeaeb5bc8cd5103035887b5ffbd4798d29edf2be50c697f84d369`,
records 298 new metadata bytes and reuses 54 previously pinned bytes for
descriptor 14 and its module name. It validates selected descriptor/OFT/name
fields against each exact caller IAT address. No code, IAT payload, imported
DLL or callback was read by that operation. Pinned Engine before/after hashes
match. The earlier malloc-symbol record lacked complete raw binding fields;
the new report independently records those selected fields.

Consequently the allocation loop is linked to `malloc` and `_callnewh`, the
release chain to `free`, the two callback branches to the named exit APIs,
the name call to `_strdup`, and the wrappers to `_CxxThrowException`.
This closes import identity, not the external CRT implementation or observed
runtime library resolution. In particular, `_strdup` has no visible null-result
guard in these writers, and exit-registration results retain the exact EAX
and callback-pointer mapping described above.

The throw-API binding does not identify the concrete types from the unbound
constructor field addresses or the unread metadata at `3AF558`/`3AF5B8`.
No allocator internals, OOM recovery,
exception matching, unwinding cleanup, or process-wide termination behavior
is reconstructed here.

## Remaining boundary

The reviewed helper shapes narrow the local writer contract. They do not
establish successful `_initterm` execution, runtime parent stability, other
writers, complete siblings/order, concrete object construction/Read, or
compatibility. Static pointer positions remain storage order.

A later bounded implementation must preserve the existing name/append/count/
release/publication/tail sequence and expose partial effects on stops. It
must not silently convert every allocator failure to a null return, clamp
corrupt counts, treat positive registration errors as success, or make repeated
native writer calls idempotent. No product implementation or tests were added
by this review.

## Saved report hashes

| Batch | SHA256 |
|---|---|
| helper-entries | `fb5531eb22d4bfb8200f154698be1d1e00d9c5fe9a3c254ae5434ee86422a4eb` |
| helper-entry-prefixes | `91073998bfc8ab7f647dfdc4174bf17a979d5ad8153c2658f413bf40e67574a9` |
| helper-direct-targets | `86c3d4e2d2a37fce41a013c2645a45cb0be7060ff8398c0ec14f927dc3eda215` |
| helper-import-thunks-and-failure-wrappers | `0deb403d187232e400e1a6f6ccbc4886580ae8627b6a2556aca2844e5f5e43ab` |
| helper-throw-targets | `fad714e89b60e7d4a727452d432201f41d7898ad90c18c80bbd04684ea95d2ed` |
| helper-constructor-prefixes | `0fce43cd034c51aa780933bd9df6a96977a32b1b2e859f17c31d61c7b7b82f50` |
