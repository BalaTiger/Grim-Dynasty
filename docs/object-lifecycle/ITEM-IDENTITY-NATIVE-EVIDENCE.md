# Item GetObjectId: bounded native evidence

2026-10-21. This is static evidence for the installed x64 binaries. Neither binary was loaded or executed. The original hashes remained unchanged before and after each bounded reader. The caller's saved original Item identity is the argument to Game RVA 323968.

## Import and caller

`resolve_item_identity_import.mjs` ran once (tool chunk `92781a`, exit0). Its fixed import/export lookup binds Game IAT609128, Engine import index37, to `?GetObjectId@Object@GAME@@QEBAIXZ`: hint3189, ordinal3190, Engine RVA3A740. It did not infer an implementation from that name.

The new caller read covers exactly `[32396E,32397D)`,15 bytes. Its first instruction `89 87 38 05 00 00` stores EAX into DWORD[original Item+538]. The next instruction restores RCX=original Item; the subsequent call at323977 targets IAT609520 and is the stopping point. Raw caller bytes for323965/323968 were reused from the sealed BE0 evidence.

## Getter entry and return

`read_object_id_getter.mjs` ran once (tool chunk `4c9ea4`, exit0). A bounded exception-table lookup read8 new entries/96 bytes; cached entries also include cache hits within this lookup. The previous entry ends at3A73D and the next begins at3A780. Thus there is no exact runtime-function record for3A740: the reader reports a candidate in a metadata gap, not a whole-function metadata boundary.

The fixed8-byte read at3A740 is `8b412cc3cccccccc`. Manual review of contiguous instructions establishes the ordinary entry path:

| Engine RVA | Raw | Operation |
| --- | --- | --- |
| 3A740 | 8B 41 2C | MOV EAX,DWORD[RCX+2C] |
| 3A743 | C3 | RET |

The remaining four CC bytes are after this return and are not part of the ordinary path. There is no call, branch, store, allocation, or callback before RET. The EAX write supplies a32-bit value and zeros the upper32 bits of RAX. A valid readable this+2C is a precondition; invalid-memory faults and asynchronous mutation are not reconstructed here. This observation concerns the pinned disk code, not a guarantee about a modified loaded process or IAT.

## Own model consequence

Read the current shared Item/Actor storage at+2C for4 bytes when the continuation runs. Do not capture the source during invocation creation, copy another object snapshot, consult the unrelated+28 raw key, or manufacture an ID allocation/registration event. On a known value, perform the single4-byte caller store to+538, leaving+53C and all other bytes intact, and stop at323977. If any source byte is unknown, retain every prior effect and the old destination. That is an own coverage stop, not a native branch or failure return. Existing Object/Actor construction has not established a value at+2C.

## Read and preservation accounting

Stage1:15 Game code bytes+88 metadata bytes, one dumpbin on a new tiny PE, four streaming hash passes. Stage2:8 Engine code bytes+96 metadata bytes, one dumpbin on a new tiny PE, two streaming hash passes. Total:23 new code bytes,184 metadata bytes, two tiny-PE disassemblies, six original hash passes with64KiB buffers. No callee was followed, no full-image disassembly was made, and no original path was passed to dumpbin.

- Game SHA256: `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`.
- Engine SHA256: `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`.
- Stage1 inspection SHA256: `c0b80a81f7094c25816017d8e4adaeeff74c835f3049daabd0842b9563740b59`.
- Stage2 inspection SHA256: `ece892d8a5f9fea5a7575bcb85fee8270ff6e08becbba696e5c81beb00095475`.
- Stage2 instruction record SHA256: `11c512c676433b49a407707fba880dcebfd110c886bb36eb92821d463586f7d1`.

The remaining Item constructor tail and QuestItem boundary are inventoried in `item-identity-validation/cached-contract.md`. This evidence does not close those dependencies or establish native runtime/sample compatibility.
