# M3 first visible checkpoint — 2026-10-22

**M2 is complete; M3 is in progress, not complete.** The first M3 node now opens a real controllable scene with an owned character, world coordinates, collision and camera. This follows the user's authorization to continue without pausing at stage boundaries.

Latest accepted window package: [m3-scene-20261022-03/gd-scene.exe](../artifacts/m3-scene-20261022-03/gd-scene.exe), SHA256 `6ce0ca305b2dabae821afa5ee5ff962c86cb406c401d183159a8002830584cc3`. Run `./artifacts/m3-scene-20261022-03/gd-scene.exe` from the repository root. Its [acceptance](../artifacts/m3-scene-20261022-03/acceptance.json) passed at `2026-10-22T04:34:04.9970578Z`: 22 window/persistence checks, actual process exit 0, full 1116x799 form capture, exact position/identity/money retained after closing save and actual SceneStore reload. The [new screenshot](../artifacts/m3-scene-20261022-03/validation/window/window.png) was visually inspected and includes both control lines.

This patch adds explicit arrow input routing, tests Right through WinForms preprocessing and the window message handler, propagates closing-save failure, and displays named-slot/recovery startup errors. The unchanged 22 model checks from `-01` are referenced by pinned source/evidence hashes and were **not rerun**; M1/M2 suites were not rerun either. Only SceneWindow/SceneProgram compiled sources changed. One patch compilation in `-02` was reused for `-03`. The `-02` window reached save/reload, but its PowerShell wrapper did not capture the process exit code, so it remains explicitly incomplete. A retained Process handle fixed the harness; only the own window smoke repeated. No simultaneous scene instances remained.

The first full package and its historical results below remain preserved unchanged. New bounded native evidence is under [m3-evidence](m3-evidence): one previously identified sword record, fixed named-export lookup, three exact entry bodies and their direct dependency assessment. These are evidence inputs, not newly playable combat/equipment/trading features. See [combat entry review](m3-evidence/COMBAT-ENTRY-REVIEW.md) and [equipment entry review](m3-evidence/EQUIPMENT-ENTRY-REVIEW.md). No arbitrary damage formula or merchant price has been added.

Run [gd-scene.exe](../artifacts/m3-scene-20261022-01/gd-scene.exe), or from the repository root:

```powershell
.\artifacts\m3-scene-20261022-01\gd-scene.exe
```

WASD/arrow keys move, E talks to a nearby NPC, F5 saves, F9 reloads, Esc saves/exits. The original code-drawn scene is 2200×1600: a border post with buildings and a gated wall, a courier road, and an abandoned beacon. The two NPC interactions use the accepted M2 task/inventory/money rules. Character, task journal and exact binary32 map position save together in the package's own `runtime-data/saves/scene-player.save`; there is no original save access.

The first scene build passed **22 checks** and a real-window smoke, completed `2026-10-22T04:10:47.5720134Z`. The window processed the same key-down/up handler and timer as the interactive application; recorded x moved from 430 to **612.5323**, y remained 1150, and the window exited with code 0. This verifies the input code path, not a claim that a human manually playtested it. The [captured window](../artifacts/m3-scene-20261022-01/validation/window/window.png) shows the rendered scene. It captures the form at client dimensions, so the bottom edge of the screenshot is cropped; the live form retains its full client area.

Tests cover wall collision, an open gate, diagonal speed, the entire walkable route to both NPCs, proximity-gated interaction, real two-task rewards, coupled scene/session reopen, retained ownership, stale writes, checksum-valid but blocked positions, explicit backup recovery and nonblank rendering. [Acceptance](../artifacts/m3-scene-20261022-01/acceptance.json), [model checks](../artifacts/m3-scene-20261022-01/scene-tests.log), [start/end renders](../artifacts/m3-scene-20261022-01/validation/models) and [commands](../artifacts/m3-scene-20261022-01/README.md) are available. Compiler logs are empty. Only one lightweight own GUI instance was opened and it closed automatically; no heavy native game or reverse-engineering GUI ran.

The new scene is a thin C#/.NET Framework WinForms/GDI+ presentation layer over the existing project modules. The repository had no existing graphics-engine implementation or game scene assets. `RECOVERY-TARGET.md` expressly permits an independent minimal scene; this does not switch the project to a mod or load the original DLLs. Movement, collision geometry and art are project-owned adapters, not recovered native rendering/physics.

## Remaining M3 requirements and concrete evidence boundaries

Combat settlement/death production, simple AI, buying/selling, equipment actions and a calibrated 10–20-minute playable loop remain **unimplemented**. The UI says so; no guessed damage, price or original balance is substituted.

- Existing `task_kills/KillCondition.cs` consumes already-produced kill events and its README says it does not produce actual combat. Saved receiver evidence is Game RVA `0x471180`, boundary `0x180471180–0x18047139A`, in `docs/task-kills/ghidra-selected/followup.json`. This is not a damage/death producer.
- Saved `docs/rewards/rea-inventory-add/Controller-GiveItem.json` identifies Game RVA `0x15F4E0`, `ControllerPlayer::GiveItemToPlayer`: nonzero ID → `EquipmentCtrl::AutoInsertItem(this+0xD0,id)` → fallback `PlayerInventoryCtrl::AddItem(this+0x470,id,true,flag)`. It does not establish equipment slot selection, stat/level requirements or swap semantics. AutoInsertItem's precise entry/body is the next direct equipment evidence target.
- Inventory add/remove and money mutations do not establish merchant inventory, buy/sell prices, transaction ordering or failure rollback. A real buy/sell entry is not yet identified in the saved subset.
- `docs/resource-reader/acceptance-installed.json` identifies an already-verified original sword record `records/items/gearweapons/swords1h/b000_sword.dbr` (663 fields, body SHA256 `6ba63bf042d53170a1e3d6e9a59fc121ae4fe29d8aef825b9958be212644f9e1`) and Soldier mastery. The current summary does not contain the actual damage/price values; the old exported file is absent at its documented local mapping. It is an evidence locator, not permission to invent values.

Next work is bounded by these M3 feature gaps. Do not resume the unrelated Actor constructor/CRT chain or label this checkpoint as full M3. Preserve the already usable M1/M2/scene packages and stage results while resolving the next direct evidence contract.
