# R1 action submission component — 2026-10-22

The mouse movement request chain now reaches a reusable, compiled **action
submission/permission/execution component**. This closes a control-flow and
shared-state implementation gap; it does not produce actual character movement
or satisfy R1 scene/gameplay acceptance.

## New original evidence

Two bounded Rea batches used real `procedure_pseudo_code`, `procedure_assembly`
and `procedure_callees` calls. No prior body query was repeated.

| Batch | UTC interval | Result | Newly read bodies |
| --- | --- | --- | --- |
| [action executor](r1-action-executor/summary.json) | 12:27:58–12:31:12 | 14/14 including five lifecycle calls | 1600F0,081870,089AC0 |
| [direct support](r1-action-support/summary.json) | 12:34:03–12:37:11 | 17/17 including five lifecycle calls | 089BF0,07A6D0,073BA0,5405A0 |

Both confirmed normal MCP close, process exit, empty owned Job and sampler exit;
neither forcibly stopped a process. Original Game SHA256 remained
`cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`.

[Exact navigation binding](r1-navigation-binding/inspection.json) read1360 bytes
of selected import/export metadata, plus streaming identity hash passes:
Game IAT609F38, OFT index487/RVA A439F8, hint2247, exact mangled FindPath name,
Engine ordinal2248 -> **Engine RVA150590**. The saved Engine assembly already
proves the wrapper reads `this+8` and tail-forwards to **10C6C0**. It was reused,
not requeried. Game and Engine before/after hashes and file identities matched.
This is a disk binding, not an observation of a loaded IAT or navigation result.

## Contracts and implemented boundary

- CheckAction1600F0 keeps the first signed incoming type for the permission row;
  its possible second type call is only the type8 gate. It performs a separate
  GetAI, rereads current-action pointers after callbacks, reads the engine global
  late, and uses signed `firstType*26+currentType`. Matrix3 rereads current and
  pending before virtual+80. Unknown cells are not default permissions.
- PreAction081870 returns null for a **known zero** byte+18. The nonzero branch
  requires lookup/class validation, allocation and coordinates; it snapshots
  owner ID for lookup but reads it again after allocation/coordinates for the
  constructor. The implementation stops there rather than fabricating a pre-action.
- Local127E40 preserves original-role identity, late handler/current/pending
  reads, repeated-pointer callbacks, and exact cleanup/publication order.
- Execute089AC0 consumes a signed budget before the busy check. Normal execution
  publishes before action+8 and clears busy only after normal return. Stop089BF0
  rereads current after virtual+18 before invoking virtual0(1). Clear073BA0
  resets sentinel/count before releasing nodes; it does not destroy payload actions.
- Reentrant insertion allocates before the capacity check, whose unsigned
  subtraction rejects exactly count `0x0AAAAAAAAAAAAAA9`.07A6D0 reads a borrowed
  action slot after079CD0 returns; the own interface preserves this mutable slot.
  **079CD0 is still unread**, so default allocation stops explicitly. Injected
  allocators in tests are fixture observations, not recovery of that function.
- IsRunningSkill5405A0 is now observed: it snapshots SkillManager+240 ID, performs
  object lookup and Skill-class validation, then checks concrete virtual+5A8.
  Those object/runtime dependencies remain explicit; it is not replaced with false.

Source: [component and support boundary](../../src/reverse_client_actions/README.md).
Managed allocation/lifetime representation is an authored platform adaptation.
No original ABI, native heap/EH, arbitrary aliasing, floating-point environment,
permission-matrix initialization or real virtual implementation is claimed.

## Actual validation and artifacts

Final [build result](../../.tooling/r1-action-contract-validation-attempt4/result.json):
library compile0, test compile0, test exit0; **45 scenarios /193 assertions**:
27/140 dispatch and18/53 executor. The tests include the real Check->Local->Execute
composition with explicitly supplied virtual observations, reentry, late reads,
duplicate identity, unknown fields, null payloads, budget exhaustion and failure
prefixes. This is isolated component verification, not native differential testing.

- Library: `.tooling/r1-action-contract-validation-attempt4/GrimDynasty.Actions.dll`
- Test executable: `.tooling/r1-action-contract-validation-attempt4/ActionContractTests.exe`
- Raw log: `.tooling/r1-action-contract-validation-attempt4/tests.log`
- Run the built tests from the repo with
  `& '.\.tooling\r1-action-contract-validation-attempt4\ActionContractTests.exe'`.
  This command does not open a game or create a character save.

Build history is retained honestly: attempt1 failed source-path parsing before
compilation; attempt2 compiled the library but found a test assertion name
shadowing the inherited Check method; attempt3 passed45/192. Independent review
then identified the borrowed-slot interface limitation; attempt4 verifies its
repair and added assertion. No historical project suite was rerun. The final
rerun was necessary for the changed public boundary and its integration tests.

## Exact continuation

Prioritize concrete **MoveToAction vtable1806E54C0 slot+8**, the action-start
operation that must drive real movement, and its directly needed slots. Resolve
the disk target with bounded metadata before querying the body; do not install a
test callback as its production implementation.079CD0 is the remaining direct
allocator dependency for the reentrant queue. Navigation10C6C0, portal predicate,
physical mouse/coordinate production, real resources/rendering and UI still
block R1. Do not expand a generic CRT tree or restart the old WASD prototype.

All requested actions in this stage completed or reached explicit unsupported
implementation boundaries. No approval was denied, no operation is pending, and
no Rea/Java process remains owned by this batch. Originals/saves/security settings
were not modified; no commit, push or deployment occurred. R3's user-verifiable
game stop condition has **not** been reached.
