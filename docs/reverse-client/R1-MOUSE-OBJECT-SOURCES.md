# Mouse caller and action-object source investigation

## Scope and verified sources

This is bounded R1 evidence, not recovered gameplay. Original executables were opened as read-only data; no game was executed, player saves accessed, security settings changed, or commits made. The legacy WASD/GDI+ prototype is not extended.

[PE metadata](r1-mouse-link-metadata/inspection.json) located the executable's exact imported `ControllerPlayer::HandleActionFromMouse` IAT slot at **1402D78A0** (RVA 2D78A0, lookup index 295). The slot is not a caller instruction. Game's default `ControllerPlayer` constructor is **18015D5A0**, with runtime-function interval `[15D5A0,15D76C)`. The separate copy constructor at 51AD0 is not used here. The exported ControllerPlayer vftable is **1806E0350**; that alone does not identify the separately selected action object.

Metadata reads totalled 5,298 bytes across the two modules, with zero code reads or child processes. Same-handle before/after SHA256 and file identity checks passed:

- `Grim Dawn.exe`: `d3915e2599317b6670cc088ba958afb2b2f4fa8141cfd1d777ee02f9ea1fc4c4`.
- `Game.dll`: `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`.

## Real Rea mouse-links batch

[Summary](r1-game-mouse-links/summary.json): **9/9 calls passed**, 2026-10-22 08:28:50.517 to 08:32:02.252 UTC, Rea 6.5.0 / Ghidra 12.1.4. Original Game hash unchanged; close, exit code 0, empty owned Job and sampler exit confirmed. No forced termination or errors. Sampled total working-set peak including controller/sampler was 1,303,830,528 bytes; minimum available physical memory was 13,675,741,184 bytes. These are discrete samples, not a bound for another query. The 384M heap and 5/2 GiB launch/stop thresholds stayed unchanged.

The batch queried only xrefs at `18015E1F0`, plus pseudocode, assembly and direct callees of `18015D5A0`. [Raw xrefs](r1-game-mouse-links/04-HandleActionFromMouse-0x18015e1f0-xrefs.json) returned two untyped locations. A subsequent **744-byte metadata-only read** [classified them](r1-mouse-link-metadata/xref-classification.json):

| Source address | Actual PE record | Implication |
| --- | --- | --- |
| 180881608 | Export address table, ordinal 17385, RVA value 15E1F0 | Not a call |
| 180A7C744 | Runtime-function table entry `[15E1F0,15E3F2)`, unwind 807D98 | Not a call |

No Game call instruction was established. This does not prove that no runtime caller exists. The EXE caller is a separate cross-module gap; its fixed IAT xrefs query is recorded in [the pending plan](r1-exe-mouse-import-query-pending.json), not executed or silently included in Game analysis.

## Constructor evidence relevant to dispatch

[Pseudocode](r1-game-mouse-links/05-ControllerPlayer-default-constructor-0x18015d5a0-procedure_pseudo_code.json) and [assembly](r1-game-mouse-links/06-ControllerPlayer-default-constructor-0x18015d5a0-procedure_assembly.json) establish:

1. `15D5C0` calls **180122960**, labelled `ControllerAI::ControllerAI`, with the original `this`. Its body was not part of the mouse-links batch.
2. `15D5CD` writes **1806E0350** to `[this]`. This is the controller's own vtable, not proof for the action object selected through `+2A0` or `+2E8/+2F0`.
3. `15D6A7` writes a **16-bit 0100h** at `this+430`: little-endian bytes `+430=0`, `+431=1`. `15D6B0` separately writes byte `+432=0`.
4. Byte `+438=0` and dword `+43C=0` are written only at `15D707/15D70E`, after intervening member constructor calls. They cannot be treated as cleared before those calls.
5. The body has no direct write to `+2A0/+2E8/+2F0`; the selected action object's source and vtable remain unproved by this constructor alone. Normal return is the original `this`; complete member construction and exceptional cleanup are outside this investigation.

## Parent constructor and role binding: real second batch

The fixed scope was only `122960` and the already-proved mouse-dispatch direct callee `128530`. [Its summary](r1-game-mouse-state/summary.json) records **11/11 calls passed**, 2026-10-22 08:39:10.692 to 08:42:21.447 UTC. Both functions returned nonempty pseudocode (2,412 and 784 characters respectively), assembly and callee inventories. The same Game hash was preserved before/after; close, exit 0, empty owned Job and sampler exit passed, with no forced termination or errors. Sampled total working-set peak was 1,316,118,528 bytes; minimum available physical memory was 13,101,752,320 bytes. No `rea-ghidra-*` runtime directory remained after cleanup.

[ControllerAI assembly](r1-game-mouse-state/05-ControllerAI-constructor-0x180122960-procedure_assembly.json) now provides a concrete initial action-object source:

- `1229C8` explicitly clears qword `this+2A0`. The `+2E8/+2F0` pair is explicitly cleared at `122A17/122A1A`; `122A23` calls `079C80` and its result is written to `+2E8`. The helper's structure and effects are not yet established; do not name this structure a vector or action stack merely from its layout.
- After the intervening constructor dependencies, `122AC3` calls the allocation-labelled `5DA6C4` with `ECX=20h` (32 bytes). The returned pointer is immediately dereferenced: qword `+8` gets the controller, qword `+10` gets zero, and byte `+18` gets zero. No local null check precedes these stores. Other bytes and allocator/exception behavior are not inferred.
- `122AD9/122AE0` write vtable **1806E40A0** to the allocated object's first qword. Rea labels it `ControllerAIStatePreStart`. `122AE3/122AEA` publish the **same pointer** to controller `+308` and `+2A0`. No further call intervenes before normal return. This closes the constructor's final default-object identity; it does not establish which state a later mouse event selects.
- The earlier parent call `122980 -> 12C9F0` and unrelated members were not expanded. This investigation does not claim complete ControllerAI construction or exception cleanup.

[The role-binding helper assembly](r1-game-mouse-state/08-Role-binding-helper-0x180128530-procedure_assembly.json) reads **DWORD controller+30**, calls the imported ObjectManager singleton getter, then `019D40(manager, id)`. A null result returns null. Otherwise virtual slot zero supplies a class record; the helper accepts direct `Player::classInfo` identity at `180A5CAB0`, immediate parent identity, or a successful `591480(parent, Player::classInfo)` predicate. Failed checks return null; success returns the **original lookup pointer**, with no copy. The lookup, singleton, class predicate and lifetime contracts remain dependencies. This is separate from the allocated action-state identity and cannot be replaced with the state pointer.

## Three initial-state slots resolved

[The final bounded PE read](r1-mouse-link-metadata/prestart-slots.json) was anchored in the saved constructor's vtable write. It read exactly **24 vtable bytes**, plus PE headers and binary-searched runtime-function metadata (1,252 bytes total including the slots), with zero instruction reads or subprocesses. Game hash and handle/path identity checks passed again.

| PreStart vtable offset | Entry preferred VA | Disk target preferred VA |
| --- | --- | --- |
| +1B0 | 1806E4250 | **18001A700** |
| +1B8 | 1806E4258 | **18001A700** |
| +1C8 | 1806E4268 | **18001A700** |

The target maps to executable `.text`; no containing `.pdata` entry was found. Its body was **not read**, so neither a return value nor a whole-function boundary is asserted. These disk entries identify the constructed PreStart table only. The live table could be replaced, the controller's `+2A0` could change, or the `+2F0 != 0` branch could select a different object via `+2E8`. None of those active-state transitions is established here. Do not use this initial table as a universal mapping for all mouse actions.

## Exact continuation and limits

1. **Caller path:** execute only the separately prepared EXE IAT xrefs query at `1402D78A0` after reviewing its fixed EXE Java target identity. Classify returned sources and resolve a caller before querying its body. Game xrefs cannot fill this gap.
2. **Initial-state dispatch:** the new exact target is Game `18001A700`. A bounded Rea body query can establish its contract; absent `.pdata` must not be treated as an inferred adjacent function boundary.
3. **Active-state dispatch:** locate the activation/state-transition writer for `+2A0` or the nonzero `+2F0` branch before assigning meanings to its slots. Avoid continuing down `ControllerCombat`, allocator, string or WorldVec3 construction solely to accumulate coverage.

No new game implementation or gameplay test was added. Two offline plan/schema verifications and the two real Rea batches are tool/evidence validation, not game compatibility tests. Existing gameplay/math suites were not rerun. Both backend sessions are closed; no execution approval was denied or left pending. The remaining blockers are missing native behavior/identity evidence, not a request to change security settings. Existing artifacts and the old prototype remain preserved.
