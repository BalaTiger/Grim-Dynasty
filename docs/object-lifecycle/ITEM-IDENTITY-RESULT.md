# Item identity continuation result

2026-10-21. The minimal gap at Game323968 is closed in the own conditional construction model. Game IAT609128 is now independently bound to Engine3A740 `GetObjectId`; its normal entry is `MOV EAX,DWORD[RCX+2C]; RET`. The same mutable Item/Actor state supplies the current4-byte ID. The caller copies that value into DWORD[Item+538] and the continuation stops at Game323977, before GetObjectName.

## Evidence and implementation

- `ITEM-IDENTITY-NATIVE-EVIDENCE.md` records the exact import, bounded entry evidence and original-hash preservation. New reads were15 Game code bytes,8 Engine code bytes and184 metadata bytes. Two dumpbin invocations consumed only synthetic tiny PEs; six streaming original-hash passes used64KiB buffers. No full-engine scan or native execution occurred.
- `src/task_items/ItemConstructorIdentity.cs` adds the own getter/result and single-attempt continuation. Existing production sources are unchanged. The getter reads at Run time, returns the32-bit value with zero-extended RAX, and introduces no callback, allocator or registration event. Only the4-byte destination is changed; adjacent unknown bytes remain unknown.
- A missing prior frontier or unknown source byte keeps the previous effects and destination. Unknown+2C is an own evidence/coverage stop at3A740, not a simulated native failure or an assumed zero ID. Existing construction does not establish the ID writer or value at that point.
- Static reader, decoded body and model received independent review. `item-identity-validation/cached-contract.md` contains the reviewed current contract and cached tail inventory. The static checkpoint retains the earlier post-body/pre-model-review document version intentionally.

## Actual validation

The reviewed fixed `build_item_constructor_identity.ps1` was invoked once using formal `require_escalated` approval, Windows PowerShell5.1, with no execution-policy override. Tool chunk `a45a20` returned exit0 directly; no pending process/session remains from that invocation. It made exactly three csc invocations and ran two own test executables once each:

| Suite | Actual assertions | Scenario groups | Meaning |
| --- | --- | --- | --- |
| New ItemConstructorIdentityTests | 49 | 7 | Live same-state getter/copy, high-bit values, exact byte effects, unknown-byte stops, prior callback mutations, rejected incomplete prefixes and single-attempt behavior |
| Existing ItemConstructorBE0Tests | 66 | 10 | Affected prior continuation regression |

There were no compiler warnings. No other old suite was run; previous totals are not new acceptance in this round. The integration fixture reaches the real own Actor→Item bridge→950→BA8→BE0 prefix before this continuation, using explicitly supplied conditional observations. These are self-authored fixtures, not executions against a native runtime or official sample.

The16 build inputs are pinned in `item-identity-validation/build-preflight.json`. The complete tool output is in `build-result.json`; independent input/output and prior-checkpoint checks are in `post-build-independent.json`. Three outputs reside in `.tooling/item-constructor-identity-build-20261021`.

## Distance to normal Item/QuestItem return

The next direct gap is **323977 → [609520](original Item)**, named GetObjectName by saved import evidence. Its exact exported entry/body is not newly verified here. The saved Item tail reaches RET at323A86, but address proximity does not establish a nearly complete usable object. From the present frontier it still contains:

1. One GetObjectName call and a caller-side byte scan to NUL; current valid string storage/pointer and length must be established.
2. Seven calls to Game016460, using the returned name and six zero-length arguments. That function's algorithm/return/failure behavior cannot be borrowed from other Engine string helpers merely because the purpose appears similar.
3. A Game globalA66288 object, with DWORD fields+1B58 and+1B5C copied into Item+B68/+B6C. Its source, validity and values remain explicit preconditions.
4. One call to Game324560, after which the cached tail selects original Item for RAX and reaches RET. Its direct behavior remains unclosed.

Thus nine call sites/three unique targets plus the global-object reads remain on this tail. Earlier conditional Actor/Item dependencies also remain, including actual gEngine publication/connection-manager state, loaded callback/synchronization prerequisites, allocation and exceptional cleanup, and the Object ID writer/lifecycle that supplies+2C. None is discharged by this getter.

The saved QuestItem wrapper has15 suffix stores after Item returns, without a new suffix call. Those stores cannot establish complete QuestItem construction before a valid Item normal return exists. Full factory allocation/cleanup, registration and record-loading behavior, and official sample/runtime compatibility remain unverified.

## Recovery and preservation

- Starting sealed BE0 checkpoint: `.tooling/checkpoints/item-constructor-be0-complete/manifest.json`, SHA256 `bd1994da660b59cec82c431bab258f1fec8beb8667e74bb3d11f307bf53be5b3`.
- Static checkpoint: `.tooling/checkpoints/item-identity-static-complete/manifest.json`,19 files/219952 payload bytes, SHA256 `025b611def6d24d36f94c708a6ec560c5dce4f1657e767e512b07ff2a0bea096`. Its initially null byte-total metadata was corrected by integer summation and all19 copies reverified; no evidence read or test was repeated.
- Final checkpoint: `.tooling/checkpoints/item-constructor-identity-complete/manifest.json`; contains selected prior state, this round's evidence/source/build outputs and validation records. Its manifest supplies the final file count and hashes.

Original Game SHA256 remains `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`; Engine remains `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`. No game/Rea/Java, player saves/cloud data, installation, security-setting change, commit or push was involved. No permission denial or unresolved execution outcome occurred in this round.
