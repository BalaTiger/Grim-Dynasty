# Game mouse action entry — real Rea result

The fixed eight-call batch completed **2026-10-22 08:02:52.645–08:06:20.717 UTC**. [summary.json](r1-game-mouse-calibration/summary.json) records all calls passed, normal close, backend exit 0, empty owned Job, sampler exit, no forced termination and no errors. No `rea-ghidra-*` runtime directory remained when checked after completion.

The input was `D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll`; both before/after SHA256 values are `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`. Original target execution was false. Rea 6.5.0 / Ghidra 12.1.4 returned **2,894 characters of nonempty pseudocode** for `18015E1F0`, `ControllerPlayer::HandleActionFromMouse`, with evidence ID `ev_b51808102067ee1a64708f2e4c710948409294f4e13abc2745f2da714909dac4`.

- [Raw pseudocode response](r1-game-mouse-calibration/04-HandleActionFromMouse-0x18015e1f0-procedure_pseudo_code.json), SHA256 `7ea092e0563eed2085965b3ecc41bb6fa899ecc8fa2849e3af2de7701c903a54`.
- [Raw assembly](r1-game-mouse-calibration/05-HandleActionFromMouse-0x18015e1f0-procedure_assembly.json), instruction starts `15E1F0..15E3F1`.
- [Direct-callee inventory](r1-game-mouse-calibration/06-HandleActionFromMouse-0x18015e1f0-procedure_callees.json); coverage explicitly partial, with four unresolved indirect call sites.
- Summary SHA256 `1397b49afa2556c97b2c1ea5ab9807a92c1a1306cd98262d10e697a2ed4d571e`.

## Concrete behavior established

This member already receives a `WorldVec3` reference; the body does not convert screen coordinates or perform ray/terrain intersection. Its four bool parameters are retained as **b1..b4** because their mouse-button/action meanings are not established. The additional unsigned-int reference and optional bool pointer likewise have no proved gameplay names yet.

1. Call `128530` with the controller; if its returned pointer is null, return false without the explicit field-clear sequence below. Otherwise call that object's virtual slot `+460`. If AL is zero, or its byte `+242A` is nonzero, clear controller byte `+438`, controller dword `+43C`, and the referenced uint, in that order, then return false.
2. On the other path, copy 24 bytes from the supplied WorldVec3 to local storage (tail qword first, then leading 16 bytes). If b3 is nonzero, `05BC20` (labelled Character::IsMoving) returns true, and `05BCE0` (CanInterruptMovement) returns true, call `05FC90` (StopMoving) with two true arguments. Their bodies were not queried in this batch.
3. Select an action object: controller `+2A0` when qword `+2F0` is zero; otherwise follow `+2E8`, dereference once, then read qword `+10`. Call its virtual slot `+1C8` with b3, b4, the local position and the uint reference.
4. If that call returns nonzero, clear the optional bool byte when its pointer is nonnull, then return **false**. Therefore this branch's nonzero callback result must not be equated to the outer function's return value.
5. If slot `+1C8` returns zero and either controller byte `+431` or b1 is nonzero, reselect the action object from the controller fields. With b2 nonzero, call slot `+1B0`, store its AL into controller byte `+432`, then return AL. With b2 zero, call slot `+1B8` and return AL without that `+432` store. Both paths pass the same local position storage and uint reference, plus their observed bool/byte arguments including a fresh read of controller `+430`.

The assembly passes this in RCX, b1/b2/b3 in DL/R8B/R9B; the entry stack slots `+28/+30/+38/+40` hold b4, WorldVec3 reference, uint reference and bool pointer. Return is AL, with no hidden aggregate return buffer. The body has no local rollback if a dependency does not return normally. Callbacks may alter the controller/position/reference state; the subsequent reselection and reads cannot be replaced with an immutable initial snapshot.

## What remains unproved

The direct callee set is `05BC20`, `05BCE0`, `05FC90`, `128530`. No direct edge to the previously inspected Engine projection or FindPath entries is established. Concrete action-object types, the `+460/+1C8/+1B0/+1B8` targets, boolean meanings, pointer initialization and lifetime, screen-position production, path search, movement execution and complete failure behavior remain unknown. This is an action dispatcher, not a recovered mouse-controlled client.

The next useful bounded evidence is the caller supplying b1..b4/WorldVec3 and the initialization/vtable identity of the selected action object (`+2A0` or the `+2E8/+2F0` path). Choose those concrete targets before implementing action semantics. No additional function query, UI prototype or gameplay implementation was performed in this batch.

## Resource observation

The new preflight at 08:02:04.9403101 UTC had 7,929,413,632 bytes available physical memory and no conflicting build/Java process. Six operational files matched the frozen checkpoint; the output directory and prior analysis runtime were absent. The formally approved fixed command then performed its own fresh preflight and proceeded with the unchanged **384M heap, 5 GiB launch, 2 GiB stop** settings.

The actual query observed the expected heap, sampled **1,311,408,128 B** maximum total working set including controller/sampler, minimum available physical **6,182,899,712 B**, and minimum commit headroom **10,414,309,376 B**. These are one successful fixed Game trial's samples, not a continuous maximum or a bound for other Game functions. No budget, timeout, privilege or system setting was changed. Earlier resource-block records remain historical and unchanged.
