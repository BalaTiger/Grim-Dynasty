# Item name: bounded native evidence

2026-10-21. Only completed observations below are asserted. Original Game and Engine binaries are read-only and have not been loaded/executed. The current caller starts at Game323977 and the outer investigation boundary is before323A49's unverified global read.

## Exact import

Fixed metadata reader `resolve_item_name_import.mjs` ran once, chunk4814fb, exit0. It reuses the saved raw323977 CALL and Game Engine descriptor/OFT index164; the index is not used as an export hint. New IAT609520 and hint/nameA47990 bytes identify `?GetObjectName@Object@GAME@@QEBAPEBDXZ`, hint3191. The exact matching Engine export is ordinal3192, entry3A760. There is no fallback export scan.

This stage read98 metadata bytes, no code, used no dumpbin, and performed four64KiB-buffer streaming hash passes. Both originals remained pinned. Inspection SHA256: `c3ba3e79ecceb98225a9034a85649807d8d00444feccf771b294a41d1e443f31`.

## Getter identity and borrowed lifetime

Reader `read_object_name_getter.mjs` ran once, chunk1aefae, exit0. It reused the previous getter's verified metadata gap (previous entry ends3A73D; next starts3A780), read a fixed32-byte candidate at3A760, and decoded it with one tiny-PE dumpbin invocation. No new metadata was read. Two64KiB-buffer Engine hash passes matched and the handle closed.

Manual review limits the normal target to15 bytes/five instructions:3A760 compares QWORD[this+20] against16;3A765 forms this+8;3A769 branches below16 directly to RET3A76E; otherwise3A76B loads the pointer QWORD from[this+8] before the same RET. This is an explicit branch, not CMOV. It reads neither stored length nor payload, writes nothing, and calls/allocates/releases nothing. The remaining17 candidate bytes include padding and a neighboring entry at3A770; these are excluded from the contract. The32-byte candidate and metadata gap are not the function size.

The result is a borrowed raw address identity: inline returns original this+8; heap-selected returns the currently stored pointer. A known zero heap pointer still returns zero in these bytes, rather than taking a native error branch. Unknown capacity or pointer cannot establish a return in the own model. The getter does not prove backing validity, termination, ownership, or a lifetime extension. Later changes to the selected bytes are visible through the same address; later changes to the object's pointer/capacity do not retroactively change a returned address. Invalidated/missing backing stops own coverage when the consumer dereferences it. Concurrent native races and allocator reuse are outside the exclusive-access fixture contract.

Inspection SHA256: `3a103ba8bf7890803c8d684388fdfe32bbfebe5c7573f623a9c4264c56763ead`; instruction record SHA256: `636cd2ae26751ab7971ab2639663a123740dd6596ba825d20e7aa604203094f4`.

## Game016460, shared by seven caller sites

Fixed reader `read_item_name_assign.mjs` ran once, chunk9e9458, exit0. It reused fixed cached caller/PE/runtime-function data and read24 new metadata bytes. The exact runtime-function record covers `[16460,1658D)`:301 code bytes/94 decoded instructions. A single dumpbin invocation used only a new tiny PE. Original Game's two streaming hash passes matched and the read handle closed. No callee, exception-unwind body or neighboring function was read.

The destination string-object address is saved in RBX, supplied source pointer in RSI and count in RDI. These are raw tokens/byte counts; this is not the Engine023180 algorithm.

- With nonzero source, the helper selects destination storage from capacity+18: inline object address if below16, otherwise QWORD[object]. It tests whether unsigned source lies in `[selectedStorage,selectedStorage+length)`. On that internal-source case it tail-jumps to Game161A0 at164D2, passing destination as both objects, offset and count. The target remains unexamined; alias handling must not be invented. Null source skips this check.
- The nonalias path checks count against UINT64_MAX-1 at164D7. The larger value reaches indirect error call60B680 at164E4 then INT3; target behavior remains unknown.
- If count exceeds capacity,164FB calls Game16340 with old length. Growth, allocation and failure/cleanup of that target remain unknown and are not imported from Engine string evidence.
- For count0 with sufficient capacity,16516 reads capacity,1651B writes QWORD length0, then either16524 writes a zero byte through the heap pointer or1653D writes the inline first byte. Thus a missing observed heap buffer can stop after the length write. Return RAX is the original destination string-object identity, not the payload address. Capacity and other inline bytes are retained.
- For positive count with sufficient capacity,1655E calls Game5DC344 with selected destination storage, supplied source and count. Its target identity must be established separately. After a normal copy return,16563 reloads capacity,16568 writes length, and1656E or16573 selects storage for the terminating byte at16576. Do not take a metadata snapshot across an unobserved call or claim a native failure rollback.

Inspection SHA256: `5a9167e7cf619e8c07ff33d74fee66e021de32bf6c638e54b67758eb7579c317`. Decoded instruction record SHA256: `acf327dda597d7a6eb974fb35ebd9720d79b102d015fafd11585fd122454dfb5`.

## Positive-copy import identity

Fixed reader `resolve_item_name_copy.mjs` ran once, chunk0187f3, exit0. It reads only the6-byte5DC344 thunk, decodes its FF25 jump to IAT60B7C0, and verifies the one candidate import through the already saved VCRUNTIME descriptor plus a bounded lookup prefix/IAT/hint-name record. The result is `VCRUNTIME140.dll!memcpy`, index5/hint60/name RVA A57684. There were65 new metadata bytes, no dumpbin or callee-body read, and two successful Game streaming hash passes.

This binds the on-disk dependency, not its loaded implementation or fault behavior. Own positive copies require explicit normal memcpy semantics and observed valid disjoint source/destination ranges. Arbitrary overlap is not converted to memmove. Unknown backing/content or absent normal-copy permission stops before the copy; that coverage check does not establish native failure atomicity. Alias161A0, growth16340 and fatal60B680 remain unexamined.

Inspection SHA256: `a9a02ca102126261641099e1aca4bd758d19ab5486d1075a2e9579f628a59adf`.

## Cached caller scope

The archived Item constructor supplies GetObjectName's returned pointer to a byte-by-byte NUL scan. It does not use stored string length. For a nonempty first byte, the loop rereads index0 after incrementing RSI from-1, then advances until NUL. The seven016460 destinations are Item+540,560,580,5A8,5C8,5E8,610. Only the first count is scanned; the remaining six counts are zero, with preserved explicit source tokens6196DE,6196DF,619703,61975D,61975E,61975F (image base180000000).

Intervening scalar writes are DWORD[5A0]=0 and DWORD[608]=0; after the final call they are QWORD[630]=0, DWORD[698]=0, DWORD[6B0]=1. The permitted outer stop is323A49, before reading GameA66288. All of these facts reuse the pinned prior Item assembly; no new caller scan was performed.

## Preservation

All four fixed readers completed once. New semantic reads total307 Game code bytes,32 Engine candidate bytes and187 metadata bytes (526 bytes total). Two tiny-PE disassemblies and ten64KiB-buffer original-hash passes were performed. Only the15-byte GetObjectName normal body is used from its32-byte candidate. No alias/growth/fatal function, memcpy implementation, globalA66288 object or Game324560 body was read.

Game SHA256 `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`; Engine SHA256 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`. No game/Rea/Java, saves/cloud, installs, security changes, commit or push. Unknown addresses, contents and lifetimes are evidence limits, not permission to supply empty strings or assume native compatibility.
