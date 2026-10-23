# Item name continuation result

2026-10-21. The own conditional constructor now continues from323977 through GetObjectName, the NUL scan, seven adjacent Game016460 assignments and five scalar stores. Its successful boundary is **323A49, before reading global GameA66288**. It does not claim complete Item/QuestItem construction, a recovered general string library, or native runtime/sample compatibility.

## What was established

Game IAT609520 independently resolves to Engine3A760 `GetObjectName`. The15-byte normal body returns original this+8 when QWORD[this+20]<16; otherwise it loads the currently stored pointer from[this+8]. It reads no length or payload and performs no allocation/release. A known zero heap pointer is a returned value, not a native empty-string branch.

This is a borrowed address identity. The own memory view retains the same Item/Actor reference and explicitly supplied mutable regions; it never substitutes ObjectNameString's logical snapshot. Changes to the selected bytes remain visible; later changes to the object's pointer/capacity do not change an already returned address. Missing/invalidated regions or unknown bytes stop at the consumer. No ownership transfer or lifetime extension is inferred, and actual native allocation/free/reuse remains unobserved.

Game016460's exact301-byte runtime-function body establishes alias checks before the zero-count check, capacity testing, no-growth writes and original destination-object return identity. Zero count writes length first, then the terminator. Positive count calls5DC344, now bound to VCRUNTIME140.dll!memcpy, then rereads capacity, writes length and selects the terminating address from current metadata. The own model requires explicit normal memcpy behavior and valid disjoint observed ranges; it does not replace overlap with memmove. Aliased sources, growth, fatal-count handling, unknown observations and coverage limits preserve an explicit frontier instead of being supplied defaults.

The implementation is `src/task_items/ItemConstructorName.cs`. No existing production source was modified. It supports a65536-position own scan budget, preserves the nonempty caller's repeated index0 read, and never substitutes the stored name length for its NUL scan. Memory regions and budgets are fixture coverage, not recovered native allocation/length limits. Raw metadata-mutation fixtures demonstrate live rereads; they do not certify valid native C++ string objects.

## Disposition of the previous nine calls

“Conditional” below means the observed normal path is implemented and exercised by own fixtures, subject to valid known state, no unresolved alias/growth branch, and explicit normal memcpy conditions where copying occurs. It does not mean every branch of the target function or every loaded process is compatible.

| Game caller | Target / destination | This round |
| --- | --- | --- |
| 323977 | IAT609520 → Engine3A760 GetObjectName | Normal getter closed; return token and live consumer behavior tested |
| 32399D | Game016460 → Item+540, scanned name count | Conditional no-growth assignment closed; no default name; growth/alias remain stops |
| 3239B3 | Game016460 → Item+560, count0 | Conditional assignment closed; actual length/terminator writes retained |
| 3239C9 | Game016460 → Item+580, count0 | Conditional assignment closed |
| 3239E5 | Game016460 → Item+5A8, count0 | Conditional assignment closed |
| 3239FB | Game016460 → Item+5C8, count0 | Conditional assignment closed |
| 323A11 | Game016460 → Item+5E8, count0 | Conditional assignment closed |
| 323A2D | Game016460 → Item+610, count0 | Conditional assignment closed |
| 323A6B | Game324560(original Item) | Unentered/unresolved; lies after the global-state boundary |

Thus eight of the nine call sites are connected in the conditional path. The seven assignments perform fourteen modeled field stores, with additional copy bytes only for positive counts; caller scalar stores at5A0/608/630/698/6B0 follow the proven order and widths. Default invocation still stops before positive memcpy unless its normal behavior is explicitly selected. Zero-length later calls retain their literal source tokens and still undergo alias checks; they are not treated as no-ops.

The current stop does **not** read A66288 or its object's fields+1B58/+1B5C, and does not write Item+B68/+B6C from guessed values. These provenance/validity/value dependencies must be established before the final324560 call and Item RET323A86. The saved QuestItem15-store suffix still requires a valid Item normal return.

## Actual validation and scope

The exact reviewed build `src/task_items/build_item_constructor_name.ps1` ran once through formal `require_escalated` approval. It used Windows PowerShell5.1 without execution-policy overrides, three csc invocations and two own test executable invocations. Initial tool chunk113ce6 produced both suite reports; completion chunk1d7b2b confirmed exit0. No build session remains pending.

| Suite | Assertions | Groups | Execution |
| --- | --- | --- | --- |
| New ItemConstructorNameTests | 76 | 9 | Once, all passed |
| Existing ItemConstructorIdentityTests | 49 | 7 | Once, all passed |

There were no compiler warnings. Other old standalone suites ran zero times; their previous totals are not newly validated results. New fixtures reuse the actual own Actor→Item bridge→950→BA8→BE0→Identity chain. They cover pointer identity/lifetime, unknown/null inputs, exact NUL scanning, full Item byte oracles, zero and positive copy order, alias/growth/fatal stops, raw post-copy metadata rereads, every later assignment's retained failure prefix and prior/single-attempt guards.

The17 frozen inputs are listed in `item-name-validation/build-preflight.json`; the complete initial/completion tool results are in `build-result.json`. `post-build-independent.json` verifies inputs, three outputs and preserved prior selected files. Build artifacts are in `.tooling/item-constructor-name-build-20261021`.

## Evidence, limitations and recovery

See `ITEM-NAME-NATIVE-EVIDENCE.md` and `item-name-validation/cached-contract.md` for exact instructions, branch conditions, hashes and independent review. Four fixed readers each ran once. New reads total307 Game code bytes,32 Engine candidate bytes and187 metadata bytes, with two tiny-PE disassemblies and ten streaming original-hash passes using64KiB buffers. Only15 bytes of the Engine candidate belong to this getter's normal path; the remaining17 bytes, including a neighboring entry, are excluded from the model. No whole-engine scan occurred.

Unresolved branch dependencies are Game161A0 alias handling,16340 growth, and the60B680 fatal target. Native memcpy implementation/faults, allocation/free/exception cleanup, concurrent mutation and loaded IAT targets remain external conditions. Earlier Actor/Item conditions also remain: ID writer, gEngine publication/connection-manager state, loaded guards and real runtime initialization. None is proved by these own tests.

- Starting identity checkpoint: `.tooling/checkpoints/item-constructor-identity-complete/manifest.json`, SHA256 `fa6cafe3f78a7d25ff367722adc7949a698f82438b5fd707a73e9d297bf1f355`,493 selected files.
- Static checkpoint: `.tooling/checkpoints/item-name-static-complete/manifest.json`,24 files/285959 payload bytes, SHA256 `5da10b3b48bc2ced8da0224f89489c8f1d129951574adbe4d9ee863c9f21c73f`. It intentionally preserves the pre-model-review cached-contract version.
- Final checkpoint: `.tooling/checkpoints/item-constructor-name-complete/manifest.json`, with selected previous state, new evidence/code/validation and three own build outputs; final counts/hashes are recorded in that manifest.

Original Game SHA256 remains `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`; Engine remains `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`. No game/Rea/Java, saves/cloud, installation, security-setting change, commit or push occurred. No execution denial, uncertain outcome or pending approval remains in this round.
