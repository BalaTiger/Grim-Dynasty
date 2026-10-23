# EXE mouse caller and initial/Idle boundary

This checkpoint advances R1 evidence only. It adds no restored game behavior and does not extend the historical WinForms/WASD prototype. Original files were read as data; no original program, player save or Steam cloud data was used. Earlier reports are preserved as historical results.

## Fixed static observations

The exact executable import slot for `ControllerPlayer::HandleActionFromMouse` is **1402D78A0**, established by the preceding [PE metadata](r1-mouse-link-metadata/inspection.json).

Two completed encoding-specific searches each read only the executable's 2,963,456-byte `.text` section plus 728 metadata bytes:

- [08:53:19 UTC result](r1-exe-mouse-candidates/inspection.json): zero `FF15`/`FF25` RIP-relative candidates targeting that slot.
- [08:56:07 UTC result](r1-exe-mouse-pointer-candidates/inspection.json): zero REX.W `MOV register,[RIP+disp32]` candidates loading that slot.

These are two different fixed operand encodings, not proof that no caller exists. Other encodings, register-indirect calls, thunks and runtime resolution are not covered. No further raw-code search was performed. Same-handle before/after executable SHA256 was `d3915e2599317b6670cc088ba958afb2b2f4fa8141cfd1d777ee02f9ea1fc4c4` in both probes.

The existing EXE import inventory names `?GoToIdleState@ControllerAI@GAME@@QEAAXXZ`. An [exact-symbol metadata lookup](r1-idle-entry-metadata/inspection.json) located Game **180125070**, with runtime-function fragment `[125070,1250FF)` (143 bytes). This lookup read 2,114 metadata bytes and no code. Its name alone is not a state-transition contract.

The [fixed decode](r1-prestart-idle-static/inspection.json), 09:08:20.791–09:08:21.016 UTC, read exactly two Game windows totalling **159 bytes**. Two installed dumpbin invocations decoded synthetic data files with generated headers, zero entry point and no imports. Neither original nor synthetic file was executed. All decoded instruction bytes exactly matched the saved original windows. Game SHA256 remained `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`; dumpbin's pinned hash was unchanged. No child was left running by the synchronous probe.

## Initial PreStart slots: narrow false result

The preceding constructor and [three-slot read](r1-mouse-link-metadata/prestart-slots.json) connect the initial PreStart vtable's `+1B0`, `+1B8`, `+1C8` to **18001A700**. The new [decoded prefix](r1-prestart-idle-static/01a700-decoded-prefix.json) is exactly `32 C0 C3`: `xor al,al; ret`.

This proves the disk entry's straight-line Boolean return is false. It clears **AL only**, not the full EAX/RAX register. The remaining thirteen `CC` bytes in the explicitly selected 16-byte window are not assigned to the function; there is no exact pdata boundary. Loaded code, replaced vtables and later selected action objects are not established by this observation.

A conditional reimplementation may use this false callback only when the supplied state identity is this evidenced PreStart table. It cannot use it for the active gameplay state. The existing dispatcher can reselect another object through `+2A0` or the nonzero `+2F0` path after callbacks; that live selection must not be replaced by a constructor snapshot.

## GoToIdleState: parameter preparation, not a proven writer

The [143-byte disassembly](r1-prestart-idle-static/125070-dumpbin.txt) contains 35 decoded instructions and no direct write through the original `this`. Let entry RSP be `S`; after the prologue, `P = S - 0x78` is the working RSP.

- Local `P+28` has byte `+0=0`, qword `+10=0`, qword `+18=15` before the first call. Other inline bytes are not assumed initialized.
- Local `P+48` receives qword `+0=0` and dword `+8=0`. Bytes `+C..+F` are not explicitly written. Its `+10` address is passed to an indirect call.
- Qword `P+20` receives `-2`. That value alone does not establish exceptional cleanup semantics.

| Instruction | Directly established argument values |
| --- | --- |
| `1250A8 → 180016460` | RCX=`P+28`, RDX=`18066DB6C`, R8D=4 |
| `1250BC → [180609720]` | RCX=`P+58` |
| `1250CF → 180123D50` | RCX=original `this`, RDX=`P+28`, R8=`P+48` |
| `1250EC → 180016620` | Only if post-call qword `[P+40]` is unsigned >=16: RCX=`[P+28]`, RDX=`[P+40]+1`, R8D=1 |

The normal tail restores RBX/RDI/RSP and returns without explicitly setting a result value. Constant bytes at `66DB6C`, the called procedures' effects, complete argument types, ownership and exceptional paths are not derived here. In particular, this fragment does not prove an Idle object identity, its mouse slots, or a write to `this+2A0/+2F0`. **180123D50** is a directly established next state-transition candidate, not an implemented transition.

## EXE query preparation and verification

The fixed [Rea plan](../../tools/static_analysis/rea-r1-exe-mouse-callers-plan.json) contains one `xrefs(0x1402d78a0)` operation and five session/open/close calls. It has an explicit `client` target profile and `target-d3915e259931.exe` snapshot name, checked against the installed Rea naming implementation. Engine/Game profiles and ownership checks remain intact. The initial EXE budget is explicitly unmeasured: 384M heap, 5 GiB physical and commit launch thresholds, 2 GiB stop thresholds, unchanged deadlines and fail-closed monitoring.

New pure Node/mock profile tests passed **8/8** once, including wrong module/hash/snapshot rejection and the selected profile reaching the monitor. One offline plan/schema verification passed with no original read or backend launch. These are tool checks, not gameplay tests. Old gameplay, mathematical and constructor suites were not rerun. Backups, diffs, logs and frozen hashes are in [the preparation manifest](../../.tooling/r1-exe-mouse-callers-prepared/review-manifest.json).

The real EXE query was separately approved after a read-only preflight reported 13,757,435,904 available physical bytes, 25,805,012,992 commit headroom bytes and no listed Java/UE/build processes.

## Actual EXE Rea result and cleanup

The [batch summary](r1-exe-mouse-callers/summary.json) records **6/6 calls passed**, 2026-10-22 **09:10:03.878–09:11:06.752 UTC**. The sole analysis operation was real Rea 6.5.0 / Ghidra 12.1.4 `xrefs(0x1402d78a0)`; this batch did **not** request procedure pseudocode. Its [raw response](r1-exe-mouse-callers/04-HandleActionFromMouse-IAT-0x1402d78a0-xrefs.json), evidence ID `ev_d4e7974d80b78b179f610d88c4e1dcc94ecffeda924a070c05de4827f8e0f587`, has both `raw_result=[]` and `normalized_result=[]`, `relationship_coverage=null` and `locations=[]`.

This is a valid empty reference observation, not a recovered caller and not proof that no runtime caller exists. Ghidra reports completed default analysis before serving operations; its exact-address reference projection is nevertheless not a complete runtime-call contract. No retry, further EXE scan or automatically expanded query followed. The caller's Boolean parameter meanings and mouse-button mapping remain unknown.

The executable hash before/after equals the pinned `d3915e...` value given above. Close acknowledgement, closed session, backend exit code 0, empty owned Job, sampler exit code 0 and drained sampler output were verified. There were no monitoring failures, resource-stop triggers, forced termination or errors. No `rea-ghidra-*` runtime directory remained after cleanup.

The sampled total task working-set peak (including controller and sampler) was **1,183,367,168 bytes**, private commit peak **1,214,197,760 bytes**, minimum available physical memory **10,990,460,928 bytes**, and minimum commit headroom **22,633,230,336 bytes**. The 384M heap was observed. These discrete samples describe this first EXE batch only, not a continuous maximum or future query bound. Immutable plan/summary budget fields saying `measured_client_peak_available=false` describe the pre-launch assumption; this paragraph records the actual new trial measurement without rewriting that assumption or lowering gates.

## Remaining implementation boundary

No platform input adapter, mouse-button interpretation, screen-to-world connection, active action-state graph or restored click-to-move path is implemented by this checkpoint. The next evidence must resolve a caller or the direct state transition before assigning gameplay meaning to the dispatch flags. It must not expand into generic allocators, string libraries or unrelated controller inheritance merely to increase function coverage.

The next smallest fixed Game query set is `procedure_pseudo_code`, `procedure_assembly` and `procedure_callees` for **180123D50**, established by the direct call at `1250CF`. This set is a candidate plan only, not registered or executed by this checkpoint. It should identify whether and how the routine mutates controller state; unknown deeper helpers remain explicit boundaries. There is no reason to retry the same empty EXE query without materially new address or reference evidence.

All actions in this checkpoint completed; none was denied or left pending. Remaining blockers are missing behavior evidence, not security-policy changes. A hash-only checkpoint manifest records the new outputs and continuation without copying previous evidence trees or rerunning previous tests.
