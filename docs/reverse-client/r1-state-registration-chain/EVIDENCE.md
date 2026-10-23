# R1 player-state registration chain

Working evidence checkpoint, 2026-10-22. This is static native evidence, not a restored client or a passed gameplay test. Original Game.dll SHA256: `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`.

## Confirmed registry publication

`GAME::ControllerAI::AddState` at `180125730` is a direct writer of the same `node+40` used by the previously verified `SetState`:

- RCX is controller C; RDX is name N retained in RBP; R8 is incoming state pointer P retained in R14. The registry is C+290.
- First lookup uses `078960(registry,N)`, followed by the sentinel check and signed `019E40(N,node+20) >= 0` acceptance. The complete lookup/comparison contract is not recovered here.
- If that accepted node has a non-null value, `12578B` calls its vslot +0 with `(old,1)`. This alone does not establish destructor/deallocation semantics. It does not clear node+40 first, nor check old==P.
- After that potentially reentrant call, the function performs a fresh lookup and comparison. It must not be represented as retaining the first node.
- The existing-entry path stores the original P as a QWORD at `node+40` (`1257B0`). The insertion path calls `125F90`, then `07B920`; the latter returns a selected node through the stack out-slot, and `1257E6` stores the same P at that node+40. Allocation/insertion failure and rollback remain unknown.
- There is no pointer adjustment, object copy or incoming-P null check in these stores. Exceptions and callback effects beyond this function remain unverified.

Real Rea outputs: [pseudocode](../r1-game-add-state/04-AddState-0x180125730-procedure_pseudo_code.json), [assembly](../r1-game-add-state/05-AddState-0x180125730-procedure_assembly.json), [callees](../r1-game-add-state/06-AddState-0x180125730-procedure_callees.json), [callers](../r1-game-add-state/07-AddState-0x180125730-procedure_callers.json). The 9-call batch completed 09:59:39–10:02:49 UTC, with unchanged target hash and confirmed close/process/Job/sampler cleanup. Callees are partial (one unresolved indirect call). Caller coverage is labelled complete only for resolved Ghidra call references; indirect runtime callers remain unknown.

## Selected player registration source

The caller list contains 16 addresses. A bounded read of PE export metadata maps `18015E440` exactly to `?RegisterStates@ControllerPlayer@GAME@@MEAAXXZ`. See [metadata](../r1-registration-caller-metadata/inspection.json). It read 252197 metadata bytes, no function body, and preserved the original hash. Other controller bodies were not followed. Missing export names at some caller addresses are not absence proofs.

The fixed player-registration Rea batch completed 10:06:41–10:09:50 UTC, 9/9, with unchanged original hash and full cleanup. [Actual pseudocode](../r1-player-register-states/04-PlayerRegisterStates-0x18015e440-procedure_pseudo_code.json) and [assembly](../r1-player-register-states/05-PlayerRegisterStates-0x18015e440-procedure_assembly.json) show these first four registrations:

| State | Allocation bytes | vptr | AddState call |
| --- | --- | --- | --- |
| Startup | 20h | 180745390 | 18015E4BD |
| Idle | 28h | 180744E60 | 18015E539 |
| LongIdle | 20h | 180744BC8 | 18015E5B3 |
| MoveTo | 48h | 180744930 | 18015E638 |

Each allocation result is retained in RBX and passed unchanged as R8 to AddState. Common explicit writes are QWORD +8=original ControllerPlayer, QWORD +10=0, BYTE +18=0 and QWORD +0=vptr. Idle additionally writes DWORD +20=0. MoveTo calls imported WorldVec3 constructor `[180609720](object+30)` before registration; this report does not infer its effects. Unwritten padding/fields remain unknown. There is no local null-allocation branch; allocation failure/exception semantics are not established.

This function has no direct SetState call or C+2A0 store. It establishes registration, not initial activation. Re-registration may invoke old-object callbacks via AddState, so absence of a direct store does not prove absence of indirect side effects. Its empty resolved caller list does not rule out virtual invocation.

## Concrete mouse-slot identities

[Fixed disk metadata](../r1-player-state-slots/inspection.json) reads 18 qwords (144 bytes) from Startup, Idle and MoveTo tables, plus four referenced strings (25 bytes), with original hashes and file identity preserved. All three share:

| Slot | Actual target |
| --- | --- |
| +1B0 | 180161D00 |
| +1B8 | 180162290 |
| +1C8 | 1801624B0 |

Lifecycle +220 targets differ: Startup=180165A80, Idle=180165B30, MoveTo=180166020. Their +228 entries all point to 180015880; no semantics are inferred from that pointer. The recorded +0 entries likewise do not by themselves prove destructor behavior.

The Idle registration key at 180674CE0 and GoToIdleState selection key at 18066DB6C both contain exact bytes `49 64 6c 65 00` (length four plus terminator). Startup and MoveTo registration bytes also match those names. This closes the selected name/object/table identity chain on disk, conditional on successful initialization and selection; it is not a runtime vptr observation.

The three actual mouse handlers were submitted as one bounded batch. At 10:13:11 UTC the ownership monitor detected two external `dotnet.exe` processes (PID 37080, parent 9824; PID 37344, parent 37080), both outside this task's explicit Job. Their project/owner is unknown. The existing mutual-exclusion guard stopped the owned backend during the first pseudocode request. Only the three lifecycle setup calls completed; **no handler pseudocode, assembly or callee result was returned**. This is a resource-coordination stop, not an execution-approval denial or a semantic/test failure. No retry or external-process termination was attempted.

See [failed-batch summary](../r1-player-mouse-handlers/summary.json) and [monitor evidence](../r1-player-mouse-handlers/owned-memory.ndjson). Owned process exit, empty Job and sampler exit were confirmed, but normal MCP close was not confirmed (`forced_termination=true`, exit 92). The original Game hash remained unchanged. The exact task runtime `analysis-runtime/rea/rea-ghidra-e4dbad03946d109dd66ecb819de833cd` remains on disk for recovery; only its path, immediate filenames and sizes were inspected, not session credential content. It is not an active-process claim.

Resume from [checkpoint.json](checkpoint.json): coordinate an exclusive interval, verify current resources/identities and the retained runtime, preserve the failed output, then prepare a separately frozen retry for the same three targets and seek normal formal execution approval. Do not rerun the existing mode into its occupied output, lower thresholds or expand to unrelated handlers. No mock state object is substituted.

Read-only resume check at 10:21:19 UTC: PID 37080 is absent; PID 37344 remains with the same creation identity and image `D:\Unity2022\Editor\Data\NetCoreRuntime\dotnet.exe`. This identifies the Unity runtime installation, not the owning project. Available physical memory is 7008313344 bytes and commit headroom 20749299712 bytes, above the existing numeric launch gates; the external-process gate still fails. All queried old owned process identities are absent. The retained runtime's immediate names/sizes/timestamps were checked without reading credentials or deleting files. No retry plan or backend was started. Evidence: `.tooling/r1-player-mouse-handlers-retry-prepared/recovery-preflight.json`.

Recovery preparation at 10:27:12 UTC: after explicit authorization and a second check of all six recorded PIDs, ownership metadata, saved Job evidence and snapshot hash, the old runtime was moved reversibly to `analysis-runtime/retained/failed-r1-player-mouse-handlers-e4dbad03946d109dd66ecb819de833cd`. All 42 files (132437852 bytes) match the full before/after hash inventory; zero files were deleted. The installed client creates a new private runtime for new sessions; its normal `close()` relies on retained in-process handles and cannot retroactively close this dead instance. No adoption API was identified in the inspected lifecycle files. The original forced-close failure remains recorded. Startup guards were not relaxed.

The independent `--r1-player-mouse-handlers-retry` plan is now frozen and offline-verified, with the same three targets, 14 calls and a fresh output directory. It pins the failure and quarantine manifests. **No backend was launched**; Unity PID37344 was still present during the pre-move check. See `.tooling/r1-player-mouse-handlers-retry-prepared/runtime-quarantine-result.json`, `runtime-before.json`, `runtime-after.json` and `offline-verify.json`. Fresh resource/identity clearance and formal execution approval are still required.

## Mouse handlers recovered after resource clearance

The blockage above is historical. At 11:31:55 UTC the external Java process was absent, no heavy conflict process was observed, and both memory gates passed. The separately approved retry completed **14/14 calls, 11:32:18–11:35:29 UTC**. [Retry summary](../r1-player-mouse-handlers-retry-1/summary.json) records unchanged Game.dll hashes, normal close, owned process/Job/sampler exit, no forced termination and no errors. The original failed evidence and quarantined runtime remain preserved. This is real Rea/Ghidra pseudocode plus assembly, not a manual reconstruction presented as a tool result.

Let S be the selected state, C=[S+8], and role=[S+10]. Parameter names below follow the existing HandleActionFromMouse ABI; their physical mouse-button meanings remain unproven.

- **161D00 / SelectPrimaryAction / +1B0:** item, NPC and fixed-actor selection may dispatch +1E8/+1F8/+1F0 when the freshly read C+430/C+432 bytes permit it. Destructible and skill branches use additional class/skill dependencies that are not closed. The general fallback rereads Options.GetInt(0xD); only value zero calls state +280 with `(S,b3,b4,position)`, ignores that callback's return and returns true. Nonzero returns false. Earlier option reads have different conditions and cannot be merged with this final read. Decoration selection may derive an alternate position through object +3F0 before the captured state +280 call. **True proves the movement-slot invocation returned, not movement success.** No missing option value or role pointer is defaulted.
- **162290 / SelectSecondaryAction / +1B8:** the role's +4A70 byte can block work. A secondary option with nonzero DWORD+10 requires SecondaryStatus==1 before activation. Otherwise skill dispatch may call state +260, then a default-skill path when allowed. The option's virtual ID getter can be called twice and the state vptr is captured before the second getter; these cannot be flattened into one snapshot. **Every normal return is false even when calls caused side effects.**
- **1624B0 / ActivateSuperSkill / +1C8:** two GetDisplayedSkillSet calls are distinct. The first validates availability; the second may differ or be null. A queued option is consumed by clearing the second set's first QWORD before invoking the state skill slot; otherwise alternate configuration selects +58/+60. Role byte +4A61 is cleared even when the second getter returns null. True depends on +260 returning nonzero; later failure does not undo the earlier consumption.

Original responses: [primary](../r1-player-mouse-handlers-retry-1/04-PlayerMouse-0x180161d00-procedure_pseudo_code.json), [secondary](../r1-player-mouse-handlers-retry-1/07-PlayerMouse-0x180162290-procedure_pseudo_code.json), [super skill](../r1-player-mouse-handlers-retry-1/10-PlayerMouse-0x1801624b0-procedure_pseudo_code.json), with adjacent assembly/callee files. Native callback ordering, pointer identity and repeated reads were independently reviewed. These summaries are not full object/skill contracts.

The next bounded metadata read [action slots](../r1-player-action-slots/inspection.json) preserved the original hash and read 72 vtable bytes (2308 total metadata bytes), without code execution. Idle +280 is **165CB0**, MoveTo +280 is **1662D0**, and both +260 slots are 165CD0. MoveTo lifecycle +220 was already identified as **166020**. The next fixed query covers only the two movement slots and that entry callback; skill dispatch is not recursively expanded. An on-disk false-return helper does not establish loaded runtime behavior.

## Movement entry relationship

The next real Rea batch completed **14/14, 11:43:03–11:46:05 UTC**, with unchanged original hashes and normal session/process/Job/sampler cleanup. [Summary](../r1-player-move-entries/summary.json). The original program was not run.

- [Idle slot assembly](../r1-player-move-entries/05-PlayerMove-0x180165cb0-procedure_assembly.json) is one instruction: `165CB0: JMP 163490`. Rea returned a long pseudocode body at the thunk request, but that does not substitute for the destination's own assembly. The next batch obtains that assembly and direct callees without asking for the same pseudocode again.
- [MoveTo request assembly](../r1-player-move-entries/08-PlayerMove-0x1801662d0-procedure_assembly.json) obtains a role-coordinate snapshot and subtracts it from the supplied destination. With an explicit subtraction result `(dx,dy,dz)`, it computes reciprocal length from `dx*dx + dz*dz`; y is **reciprocal multiplied by +0**, not a guaranteed zero. The dot accumulation is `(newY*storedY + newX*storedX) + newZ*storedZ`, with single-precision operations in that order. `COMISS; JBE` sends less/equal **and unordered** to 163490, assuming comparison completes under the floating-point environment. Only ordered greater takes the cache path. Unmasked floating-point exceptions, NaN payloads, FTZ/DAZ and constant bit patterns are not covered by this local summary.
- The cache path performs a **16-byte load/store** from input to `S+30`, then an **8-byte load/store** from input+10 to `S+40`. A full 24-byte pre-snapshot would change overlap behavior. The bool parameters are forwarded unchanged only on the fallback path; no blanket bool gate applies to the cache path.
- [MoveTo OnBegin assembly](../r1-player-move-entries/11-PlayerMove-0x180166020-procedure_assembly.json) calls GetCurrentStateData twice. First, it copies data+10..+27 into temporary A (16+8), and queries A's Region; null returns immediately. It then obtains a role-coordinate snapshot, rereads the controller and the data getter, and copies a second destination into separate temporary B. **B determines the direction; A is the eventual MoveTo destination.** Do not merge the getter calls.
- OnBegin writes direction as QWORD S+20, DWORD S+28, clears DWORD S+24, then normalizes and writes 8+4 bytes. It invokes the default WorldVec3 constructor at B's temporary address and copies the constructor's **returned pointer** into S+30 using 16+8, without assuming zero initialization or return identity. After rereading/refilling the role cache, `GetRunSpeed(false)` supplies speed; it rereads the controller and calls **124340(controller,A,0,0,5,speed)**.

This closes the registered movement entry to controller-dispatch relationship. It does not establish path generation, motion integration, valid Region production, physical mouse mapping or scene presentation. The next fixed batch is 163490 assembly/callees, 124340 pseudocode/assembly/callees and 125A00 assembly. No new gameplay/model tests or old test reruns have occurred in this stage.

## Default request, state data and action submission

The **11-call dispatch batch passed 11:54:02–11:57:03 UTC**, after a transient exec-channel disconnect was diagnosed with one harmless timestamp check. No batch was pending during that disconnect, and no completed operation was repeated. [Summary](../r1-player-move-dispatch/summary.json) confirms normal cleanup and original hash preservation.

[163490 assembly](../r1-player-move-dispatch/04-MoveDispatch-0x180163490-procedure_assembly.json) confirms the first bool alone gates an immediate return. A valid Region and role-dependent distance checks precede the first direct `3CE890` Player::FindPath call. Result zero publishes MoveTo using the path output. The 32-bit `ADD -2; TEST FFFFFFFD` selects **2 or 4** for a possible camera-height projection retry; their semantic enum names remain unknown. Other/failing results proceed to the straight-move-point fallback; that fallback requires low-byte result 1 and another successful FindPath before publication. The role +45B4 branch instead calls StopMoving(true,true) then SetPathPosition; those bodies are not silently substituted with teleportation.

Assembly-specific distinctions: `16358A COMISS/JC` returns on less or unordered; the `163621` and `1638E3` comparisons return on less/equal/unordered. The height comparison at `16370C COMISS/JNC` **enters** projection on less or unordered, unlike an ordinary C# or JavaScript `<`. These are conditional on floating-point comparison completing. All three state-publication temporaries explicitly zero only +0..+B; +C..+F stays unwritten, then the path output is copied 16+8 to +10..+27. Do not create a zero-filled 40-byte native state-data claim.

[125A00 assembly](../r1-player-move-dispatch/09-MoveDispatch-0x180125a00-procedure_assembly.json) obtains the C+2E8 list sentinel and its first node. For each node it calls state-at-node+10 virtual +210. AL==0 returns node+18. Otherwise, **after the callback**, it reads node.next and rereads C+2E8 for comparison. If traversal finishes it returns C+2A8. It does not test C+2F0. Callback effects, node lifetime, cycles and the predicate's full meaning are still unresolved; no snapshot traversal or invented fallback is installed.

[124340 assembly](../r1-player-move-dispatch/07-MoveDispatch-0x180124340-procedure_assembly.json) allocates 0x88, calls GetAI once for `531A30(role+850, suppliedSkillID)`, calls GetAI again for a 16+8 coordinate snapshot, then reads C+30 immediately before constructing `082B10`. The original destination pointer, explicit flags, computed skill reference, speed and animation type are forwarded. It tail-calls `127DD0` with the **constructor-returned RAX**, not an assumed allocation identity. Native allocation failure and exception cleanup are not established here.

Reused constructor evidence: [WorldVec3 normal prefix](../../object-lifecycle/item-ba8-validation/worldvec3-normal-prefix.json), SHA256 `b47c38e7951dc1d522a90b1f233df4c0130c88bbb720c257bd7fdd7616aa0386`, and its pinned import/export/raw-byte sources were rechecked without rereading the original or running old tests. For 166020, conditional on the inspected binding, the default constructor at temporary B clears only B[0..7] and returns B. Thus S+30..37 becomes zero and S+38..47 preserves B's trailing 16 bytes **as they were immediately before construction**. Until the subtraction helper's mutation contract is known, do not unconditionally call that the untouched second state-data snapshot.

[Fixed constant/key read](../r1-move-constants/inspection.json) at 12:01:44 UTC read 37 target bytes within 725 total data/header bytes, preserving the original hash and stable file identity. Disk values: 7A8050=`3F7FFFFD` (0.9999998211860657), 7A8054=`3F800000` (1), 7A8024=`3F000000` (0.5), 7A7FA0=`3A23D70B` (0.0006250000442378223). The three directly referenced keys at 675168/67522C/675268 are all exact `MoveTo\0`, matching the registered name. Disk values are not loaded-state invariants.

## Path request and concrete MoveToAction

The **18-call action/path batch passed 12:01:24–12:04:32 UTC**, with original Game hash unchanged and normal session/process/Job/sampler cleanup. [Summary](../r1-movement-action-path/summary.json). No native program or gameplay test was executed.

- [Player::FindPath 3CE890](../r1-movement-action-path/04-MoveAction-0x1803ce890-procedure_pseudo_code.json) calls GetMainPlayer using one gGameEngine value. Only on identity match does it reread gGameEngine and inspect DWORD+37748; nonzero, or zero plus signed OptionInt(13)>0, selects zero policy. Otherwise the disk constant at 7A81AC remains selected. Immediately before calling 08E200 it reads the original Player's +DC0 manager and +4BF0 float. The ten call arguments are manager, original destination, constant7A80E4, inputFloat1, Player+4BF0, selectedPolicy, original output pointer, null, inputFloat2, false. The result is passed through. The two newly referenced constants are not substituted from decompiler decimal labels.
- [MoveToAction 082B10](../r1-movement-action-path/08-MoveAction-0x180082b10-procedure_assembly.json) first calls imported CharacterAction construction at 609ED0, then writes base vptr, DWORD+8=id, DWORD+C=0, WORD+10=0100, DWORD+14=437A0000, BYTE+18=0; constructs this+20 via the already evidenced WorldVec3 import; then writes final vptr **1806E54C0**. It copies start and destination in two separate 16+8 sequences to +48 and +60, then DWORDs at +78/+7C/+80/+84, finally DWORD+C=4 and BYTE+10=1. The final flag bytes +10/+11 are **01/01**. Normal return is explicitly original this. Padding and base-constructor effects are not invented; source reads occur after the imported calls, not at function entry.
- [HandleAction 127DD0](../r1-movement-action-path/11-MoveAction-0x180127dd0-procedure_assembly.json) optionally sends through the network shim only when Engine network-enabled and action+50 allow it. It rereads Engine before obtaining that shim. On normal continuation, all branches tail-jump to **127E40 with original controller and action identities**. The local dispatch's success, execution and ownership are not yet inferred. No networking implementation is being added.
- [GetAI 078D40](../r1-movement-action-path/13-MoveAction-0x180078d40-procedure_assembly.json) looks up the controller's DWORD+30 object ID, then checks its returned class/parent relation to metadata180A5CBA0; accepted results preserve the original object pointer, otherwise return null. It is not a direct field getter. Its lookup/class dependencies remain separate.
- [Skill reference 531A30](../r1-movement-action-path/15-MoveAction-0x180531a30-procedure_assembly.json) has **no input-zero shortcut**. It searches five ordered pointer ranges (20/28,258/260,378/380,2C8/2D0,360/368), skipping null entries without removing their index. An object ID of zero can match. The last four groups add current DWORD bases3BC/3C0/3C4/3C8 with 32-bit arithmetic. Only all-unmatched returns FFFFFFFF. Range pointers and bounds are reread around ID calls; no all-container snapshot is assumed.

Next direct scope: CharacterMovementManager::FindPath **08E200**, LocalHandleAction **127E40**, and the directly used GetMainPlayer **2EBB70** getter. The remaining gap is actual path generation and local command consumption, not another reimplementation of the already evidenced query/registration model. No new client executable or gameplay acceptance is claimed.

## Path-result contract and local action ownership

The **12-call path/local batch passed 12:09:47–12:12:51 UTC**, with unchanged original hash and all normal cleanup checks confirmed. [Summary](../r1-path-local-dispatch/summary.json). These are still static contracts, not successful gameplay.

[CharacterMovementManager::FindPath 08E200](../r1-path-local-dispatch/05-PathLocal-0x18008e200-procedure_assembly.json) first captures owner coordinates and calls **08DF30 CheckForPortal**. This predicate is not yet recovered, so the implementation must not simply choose the non-portal branch. Non-portal navigation invokes **Game IAT609F38**, whose exact Engine target still needs import/export verification; a similarly named saved Engine function is not sufficient binding proof.

In the non-portal branch, a non-null length-output pointer causes DWORD manager+70 to be cleared **before** navigation. At 08E43C..442, the returned local word and manager+70 are compared as **32-bit integers**, not floats. The pseudocode's reused stack-slot type is misleading. Conditional on normal completion and the native navigation writes, the function's own result/output contract is:

| Path | Own writes following navigation |
| --- | --- |
| Non-portal, success and equal DWORDs | Optional 24-byte position only; no length or further manager+70 write |
| Non-portal, success and different DWORDs | Position, then optional length, then manager+70, in that order |
| Non-portal, result 1/2/3/4 | No success output writes; earlier manager+70 clear remains |
| Portal, both path calls succeed | Second navigation receives original caller position-output pointer; optional sum of both lengths, return0 |
| Portal, either navigation fails | Return4; cannot promise caller output unchanged because second navigation can receive it directly |

The success position copy loads **both 16 and 8 bytes before either store**, unlike the alternating copy order in several other recovered routines. Same NaN bit patterns compare equal in the DWORD check; +0 and -0 bit patterns differ. Floating-point branches remain flag-sensitive: unordered squared length selects initial distance zero; eligible short-path comparison can return1 on unordered; unordered dot comparison returns3; ratio failure returns2 only for ordered greater. No native unmasked-exception behavior is claimed.

[LocalHandleAction 127E40](../r1-path-local-dispatch/08-PathLocal-0x180127e40-procedure_assembly.json) gets an initial role and calls controller+D0. Results0/4 enter execution (4 first calls role+8A8); result1 replaces A8 after its old callback; result2 conditionally invokes input action slot0(1); other results return. For the execution path, the exact sequence is oldA0 capture -> A0=0 -> oldA0+20 callback -> reread A0 and slot0(1) -> **capture A8 then A0=0** -> capturedA8 slot0(1) -> A8=0. Consequently an A8 callback may leave a new A0. There is no general two-field clear or same-pointer skip.

Next, input action+60 is called. If non-null, a freshly read A0 is handled, original input is published to A0, and the returned action is executed. If null, original input is executed without that extra clear/publication. The action handler is read from the **initial role's +24C0 after callbacks**, and **089AC0 Execute** receives an explicit local DWORD initialized to100. The two Ghidra jumptable warnings are ordinary `JMP [vtable+0]` tails in the assembly, not a missing switch-table barrier. Callback target contracts and lifetime remain open.

[GetMainPlayer 2EBB70](../r1-path-local-dispatch/10-PathLocal-0x1802ebb70-procedure_assembly.json) calls manager-at-engine+40F8 slot+18 twice. First returned object's DWORD+10 is only a nonzero gate. It rereads manager, calls again and uses the second ID, with no second zero check, for object lookup and Player-class validation. Manager, result identity and ID cannot be merged into one snapshot.

The final bounded [slot/constant read](../r1-local-action-slots/inspection.json), 12:16:39 UTC, preserved Game hashes and read36 target bytes (724 including headers). Concrete disk targets: ControllerPlayer **+D0=1801600F0**; MoveToAction **+60=180081870**, **+50=180081860**. Their bodies have not been queried. It also confirms 7A81AC=`BF800000` (-1),7A80E4=`41200000` (10),7A80F0=`41700000` (15). This completes the referenced constant values without treating them as loaded-state guarantees.

Next finite work: actual action predicate1600F0, action+60 helper081870 and executor089AC0; the network-only081860 can remain out of an explicitly offline slice. In parallel use bounded import/export metadata to establish609F38 and inspect08DF30 only as required to determine the portal predicate. Do not substitute an always-accept predicate, null action helper, no-portal default or invented navigation algorithm. No new gameplay code, builds or tests were produced by this evidence stage, and no old suite was rerun.

## Preserved downstream contract

See [SetState evidence](../R1-STATE-TRANSITION-EVIDENCE.md). In particular preserve the old-state +228 callback's reentrant early return; the sequential 16-byte load/store, 16-byte load/store, then 8-byte load/store; and the late node+40 read before publication to C+2A0. Do not replace this with an all-fields snapshot or automatic rollback.

`HandleActionFromMouse` selects C+2A0 when C+2F0 is zero, otherwise an overlay state from C+2E8. Its +1C8 interception and fresh +1B0/+1B8 selection are already evidenced. The concrete registered targets above supersede any attempt to treat the initial PreStart object's false-return slot as active-player behavior. Overlay ownership and initial activation are still not closed. See [mouse object sources](../R1-MOUSE-OBJECT-SOURCES.md).

The preceding sections describe historical evidence-only stages. The later action component now has45 isolated scenarios/193 assertions passing; see the current result below. No historical project tests were repeated. R1 functional acceptance still requires the actual click-movement/navigation chain and original-resource presentation; this registry evidence does not satisfy that acceptance by itself.

## Current action component result

See [action submission result](../R1-ACTION-SUBMISSION-RESULT.md): two new Rea batches31/31 total calls, exact609F38 navigation binding, compiled reusable C# library, and45 isolated scenarios/193 assertions. Previous next-target statements for1600F0/081870/089AC0 are superseded by this result. Real MoveToAction+8, navigation10C6C0, physical input/coordinates and rendering remain unclosed; this is not R1 game acceptance.

## Subsequent MoveTo start result

The unread-MoveToAction+8 statement immediately above is now superseded by
[the MoveTo start result](../R1-MOVE-START-RESULT.md). Its exact target082C00,
the concrete stop082EF0 and eight direct/helper bodies were obtained in three
successful fixed Rea batches43/43 including lifecycle. A compiled shared-state
start/stop component passed35 new scenarios/180 assertions; its independently
compiled movement-manager extension passed15/86, without repeating the prior45
or the35-case suite. No original game or native differential tests were run.

The current direct production gap is08EAF0's queue/portal/waypoint pipeline:
073F70 ->08DF30 ->08F4C0 ->08D9E0, with path-dependent imported portal/floor
operations. The existing079CD0 allocator and Engine10C6C0 navigation-search gaps
remain, as do real mouse/coordinate production and scene/UI integration. The
default unresolved mover still stops explicitly. R1 gameplay and R3's usable
program stop condition have not been reached.

## Subsequent waypoint result

The direct pipeline above has now been read through073F70/08DF30/08F4C0/08D9E0
and the required08F340/074110 helpers. Three fixed Rea batches39/39 calls include
the next019230/5DC3F8 portal dependencies; source hashes and clean shutdown are
retained. The new shared queue/selection/ordinary-waypoint component passed39
scenarios/268 assertions on this host, first compile/test attempt, without old
suite reruns. See [waypoint result](../R1-WAYPOINT-RESULT.md).

This closes conditional queue production/consumption, not real movement. Portal
kind1/full coordinates, camera instance/setter, actual navigation/floor imports,
079CD0, input/coordinate production and scene/UI still block R1/R3 acceptance.
5DC3F8's selected import binding is CRT atan2f, and7A80BC is float32 pi; neither
proves loaded state or supplies the missing camera object. No original execution.
