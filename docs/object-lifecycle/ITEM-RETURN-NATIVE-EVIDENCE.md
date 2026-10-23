# Item final call and global source evidence

2026-10-21. This is static evidence, not a loaded-game observation. The retained caller at Game323A49 captures the GameA66288 pointer once, copies DWORD[borrowed+1B58] to Item+B68, then DWORD[the same borrowed+1B5C] to Item+B6C. There is no second global-slot read between these fields. Only after324560 returns normally does the caller select original Item RDI into RAX at323A71 and reach RET323A86.

## Global source and remaining initialization boundary

The saved InitializeObjects route allocates37868 bytes, passes the returned pointer to GameEngine2C07A0, publishes that constructor's returned RAX into A66288 at36B25F, then tail-calls Initialize2D7230. The slot is symbolized as gGameEngine in the saved reference record. This is a publication route, not proof of current validity, completed initialization, or all writers.

The saved GameEngine constructor captures RCX into RDI at2C07C6 and selects RDI as its return identity at2C1B8B. Its local stores are DWORD[this+1B58]=40400000 at2C0D23 and **QWORD**[this+1B5C]=0000000040A00000 at2C0D2D; the latter also writes four zero bytes at1B60..1B63. Item consumes two DWORDs, so its copy widths must not be inferred from that QWORD initialization.

Later in the same constructor,2C18CE selects the same this and2C18D1 calls2C8750, identified in the cached pseudocode as LoadFromDatabase. Its write set is unresolved. Other later calls and Initialize2D7230 also remain outside this round. Therefore those initialization constants are not asserted to be the values present when Item construction runs. Own fixtures must supply explicit current fields and object lifetime. No disk zero or fabricated GameEngine instance may satisfy this dependency.

See `item-return-validation/cached-global-contract.md` for selected cache paths, hashes, provenance strengths and limitations. The historical Ghidra JSON lacks embedded per-instruction raw bytes; this round's narrow input reader verifies only the selected source stores and identity/publication instructions, not the entire GameEngine constructor.

## Last call: exact bounded body

`read_item_return_dependency.mjs` ran once, tool chunk affea8, exit0. The fixed target was Game324560, justified by caller323A6B with original Item RCX. A bounded search found runtime-function entry11780, raw `60453200db45320094ea7d00`, covering324560..3245DB exclusive,123 bytes. It used168 new metadata bytes,123 code bytes and one2048-byte synthetic PE disassembly. All28 decoded instructions concatenate exactly to the original slice. Game was opened read only; before/after SHA256 remained `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2` with stable file stat and closed handle. No callee was automatically followed.

| RVA | Normal instruction effect |
| --- | --- |
| 32456A | Capture DWORD Item+B6C as binary32 XMM1. |
| 324578 | Subtract current binary32 Item+B68 into XMM0. |
| 324580 | Multiply XMM0 by DWORD[Game7A8164]. |
| 324588 | CVTTSS2SI to signed EDI; this divisor stays captured across the external call. |
| 32458E | Nonzero EDI jumps to3245A2. Zero follows the next three instructions. |
| 324590 / 324598 / 32459C | Multiply captured B6C by the same constant, truncate to EAX, store DWORD Item+B64. This branch then **falls through**. |
| 3245A2 | Call IAT60BA70. No Item argument is set up by this sequence. |
| 3245A8 | After a normal external return, reread current Item+B68. |
| 3245B0 / 3245B1 | Sign-extend returned EAX with CDQ; multiply the reread B68 by the same constant. |
| 3245B9 | Signed32-bit IDIV by the captured EDI. Zero divisor and INT_MIN/-1 have no normal arithmetic result. |
| 3245BB / 3245BF | Move signed remainder EDX into XMM0; convert to binary32. |
| 3245C2 / 3245C6 / 3245CA | Add scaled current B68, truncate to EAX, store DWORD Item+B64. |
| 3245DA | Helper RET; caller then chooses original Item identity. |

The zero-span store is a failure prefix, not a successful fallback: the external call and the later B68 read/multiply occur before IDIV discovers zero. Own coverage cannot move that failure earlier or roll back B64. Likewise changing B6C during a deliberately staged own observation does not recalculate EDI, while changing B68 affects the later read. Staging is an observation facility; no native callback, concurrency or reentrancy behavior is inferred.

SSE rounding/control state, NaNs, infinities, subnormals, masked/unmasked exceptions and invalid integer conversion are not proved by disassembly. The own implementation is restricted to its explicitly documented exact finite binary32 domain and normal floating-point conditions. A coverage stop is not a fabricated native error return. Native random state, complete string alias/growth handling and unwind/cleanup remain separate gaps.

## Fixed proof hashes

- Reader: `tools/static_analysis/read_item_return_dependency.mjs`,16025 bytes, SHA256 `21057e280d1a846a5e5928d8cf344651ef0e4c62711ce98a3631b70b6822179f`.
- Inspection: `offline-item-return-dependency/inspection.json`, SHA256 `fc0f85739966bf84241cc277b6c4d2ae7129a9daa27792ed1245e9cb7b409ff0`.
- Saved GameEngine cache: `docs/task-kills/ghidra-selected/subscriber-constructor-routes.json`, SHA256 `e907b67d57a568784a19f887a3c71883b0f7726a14defee449cf8693422ee165`.
- Saved publication cache: `docs/phase-1/round-4/Game-function-evidence-bounded-v2.json`, SHA256 `1e8421fc6b83004fd39259ca19ba97faa7df520cf49200572cecd8a2bb80c502`.

## Verified inputs and narrow cache rebinding

`resolve_item_return_inputs.mjs` was fully reviewed and ran once, chunk476945, exit0. The fixed descriptor8 atA429E0, first OFT slotA45530 and IAT60BA70 independently bind to **api-ms-win-crt-utility-l1-1-0.dll!rand**, hint27, hint/name RVA A57774. This is the complete API-set DLL name, not a separate game utility.dll. No CRT implementation, loaded IAT, seed/state or return sequence was read or executed.

The four disk bytes at7A8164 are `00007a44`, binary32 bits447A0000, exactly1000.0. They establish the file's operand, not an immutable loaded-memory promise. The own model reads its supplied scale observation at each corresponding MULSS.

The following34 code bytes all matched their saved instruction semantics:2C0D23's two stores (21B),36B25F's publication (7B),2C07C6's this capture (3B), and2C1B8B's return selection (3B). In particular the raw QWORD opcode is retained; no whole GameEngine body or LoadFromDatabase was reread. A66288's disk slot was not read because it could not establish loaded object validity.

This second reader used77 metadata bytes,4 data bytes and34 code bytes, with no subprocess. Inspection SHA256 is `288a5ae6cc72a4a2e68b7c1f03f14476a2f1e5a1a9794fe2f9bdb710a09cba4f`; reader SHA256 is `6900f004dc8961c67bb020080fa5f6822ba4a1be512fc93835875fff6c91269d`.

Across both readers, this round used **157 new Game code bytes +245 metadata bytes +4 data bytes =406 semantic bytes**, one tiny-PE dumpbin call and four original Game hash passes with64KiB buffers. All before/after hashes match the existing pin. Engine was not newly read this round. No original executable image was loaded or executed; the synthetic slice was only disassembled. Own model compilation/tests are recorded separately and are not native runtime evidence.
