# M2 delivered; M3 scene and patrol nodes delivered

> Route superseded, 2026-10-22: follow [REVERSE-CLIENT-MILESTONES.md](REVERSE-CLIENT-MILESTONES.md), R0–R3. The user authorized the reverse-client correction; the self-drawn WinForms/GDI+ and WASD route is permanently stopped. The M1/M2/M3 material below is retained as historical tools, conditional models and original validation prototypes, not R-stage acceptance or an instruction to resume that route. Stop autonomous advancement immediately after the R3 initially usable client milestone.

Latest continuation: [death ownership / first statistics checkpoint](M3-DEATH-BRIDGE-CHECKPOINT.md). At 05:40/05:42 UTC, internal death deduplication/reward persistence and first empty statistics insertion passed on this host. No new playable combat build exists; the patrol launch path below remains current. The older chronological notes below retain their original scope; the new checkpoint supersedes their statements that the death bridge and first insertion are unimplemented.

The user authorized autonomous continuation through M3. There is no new approval request or changed goal at this checkpoint. Full M3 is not yet complete; this file records the actual deliverables and the next bounded engineering dependency.

| Stage | Observable output | Actual validation |
|---|---|---|
| M1 retained | `artifacts/m1-offline-20261022-03/gd-offline.exe`: resource inspection and supported replay commands | Previously accepted 19 cases / 69 checks; not rerun in M2/M3 |
| M2 complete | `artifacts/m2-session-20261022-01/gd-session.exe`: own character, two tasks, delivery, reward, own save, process exit/reopen | 18 CLI cases / 53 CLI checks and 42 core checks, accepted on this host |
| M3 visible node complete | `artifacts/m3-scene-20261022-03/gd-scene.exe`: actual border-post/road/beacon scene, player control, collision, camera, proximity tasks, coupled scene/session save/load | Initial 22 scene model checks retained; latest 22 window/persistence checks passed, actual exit 0, exact position/identity/money preserved on reload; full screenshot visually inspected |
| M3 authored patrol node complete | `artifacts/m3-patrol-20261022-01/gd-scene.exe`: road sentry patrol/approach/return, collision, actor-state save/reopen | 42 new agent checks, 16 new integration checks, 22 affected scene checks rerun; headless render inspected, no new window run |
| M3 full slice pending | Combat/death production, buying/selling, equipment actions, calibrated 10-20-minute route | Not implemented or accepted; authored patrol is not native AI compatibility |

Latest stage: [M3 patrol checkpoint](M3-PATROL-CHECKPOINT.md). The earlier `m3-scene-20261022-03` remains the last actual window/input acceptance; the new patrol EXE is compiled and tested headlessly. User-facing launch paths for both are documented. No foreground window was automatically opened after the user's latest instruction.

Run the scene from the repository root:

```powershell
.\artifacts\m3-patrol-20261022-01\gd-scene.exe
```

WASD/arrows move; E interacts near an NPC; F5 saves; F9 loads; Esc saves and exits. Space/B/I explicitly explain the unavailable combat/trade/equipment features. This is project-owned geometry/rendering over the verified task/inventory modules, not a recovered native renderer. Money 50 -> 125 -> 165 belongs to the accepted own fixture, not an asserted native new-character balance. M1/M2 packages are preserved separately.

## What actually advanced after the visible node

The window patch fixes full-form screenshot cropping, adds explicit arrow routing and verifies it through WinForms preprocessing, propagates closing-save failure, and displays named-slot startup/recovery errors. The first patch run (`-02`) compiled and reached save/reload, but its wrapper did not capture a process exit code. It remains incomplete. The corrected wrapper reused that exact compiled executable for `-03`; only one small window test repeated. Old model suites did not repeat. There are no leftover own GUI/compiler processes at the closing check.

One already identified original sword record was decoded with the accepted own M1 reader: 663 fields, min/max physical damage 9/19, body hash matching prior evidence. Its database ARZ hash was unchanged before/after. This establishes data, not damage cadence/formula or merchant pricing.

Bounded Game.dll investigation covered three named entries and five immediate dependencies, **4325 distinct original code/data bytes** total. A linear decode stopped on the slot gate's inline jump table; its Failed report remains unchanged. Saved bytes establish the table classification. Only the two unread ranges were subsequently read, with separate evidence. All original Game.dll before/after hashes matched. No original game, Rea, Java, player save or Steam-cloud access occurred.

The new contracts locate, but do not yet close, the gameplay gaps:

- Damage entry delegates to a path containing absorption, virtual filters, callbacks and global state. It reaches `068AB0` for settlement and a conditional source/kill path through `535A60`. Existing KillCondition is only the receiver. No trustworthy attack-to-death producer is connected yet.
- Ordinary equipment slot priority, numeric gate mapping and pointer/flag notification order are now recorded. Qualification helpers `0CF6E0`/`326860`, owner registration `12B7D0`/`12B870`, type/slot initialization and paired weapon paths remain unclosed. The observed bypass flag is not set artificially to obtain success.
- Eight fixed candidate export prefixes did not locate a buy/sell entry. This is not proof that trading is absent. Existing money/inventory mutations are insufficient to infer merchant prices, transaction ordering or rollback.

See [combat review](m3-evidence/COMBAT-ENTRY-REVIEW.md), [equipment review](m3-evidence/EQUIPMENT-ENTRY-REVIEW.md), [scene checkpoint](M3-SCENE-CHECKPOINT.md) and [M2 delivery](M2-SESSION-DELIVERY.md).

## Next execution boundary

The next useful M3 work is one ordinary single-target attack/health/death branch that feeds the existing kill-condition module, starting from the directly observed `068AB0` settlement and `535A60` notification dependencies. Determine whether it closes without unresolved runtime inputs before building a combat fixture. Keep unrelated constructor/CRT recovery out of this scope. Do not fill unknown prices, item types, attributes, callbacks or globals with convenient defaults merely to make the scene playable. Native AI, merchant trading and the 10-20-minute route remain explicit later requirements.

After that checkpoint, bounded evidence added 235 bytes for `068AB0`, 47+82 bytes for `535A60` and its direct successor fragments, 126 bytes for `3FE390`, and a fixed 32-byte `CF6E0` prefix window. Total distinct M3 code/data bytes read is now **4847**; this is an evidence-scope count, not a recovery percentage. `CF6E0` has no `.pdata` entry and the fixed window has no return; it remains incomplete. The source hash stayed unchanged throughout. A reusable plan reader now handles proved direct branches and bounded leaf probes, avoiding one copied script per function.

The new conditional settlement implementation passed **129 assertions / 11 groups**, including width/rounding, borrowed identity, callback reentry and failure prefixes. It is not installed as a fake successful combat callback in the scene. Two candidate resource bodies and two directly referenced formula records were decoded with original ARZ hashes unchanged; the enemy's zero raw life value cannot be used as initialized life. Formula strings do not yet establish native numeric bindings or actual merchant prices.

No approval was denied or is pending in this stage. One helper agent temporarily lost its execution transport; root-side execution continued normally and every actual execution outcome is recorded. The remaining issue is incomplete behavioral evidence/implementation. Further original reads and own builds retain the existing bounded scope and formal execution-approval rules. No commit, push, deployment, installation or security-setting change was performed. Checkpoints are hash manifests, not cumulative source backups. Per-task token/cost accounting is not visible and is not estimated.

## Conditional combat dependencies accepted at 05:24 UTC

The continuation has now closed the recorded finite nonnegative attribute-getter path and the existing-key statistics/final path, with real bindings to the existing settlement interfaces. `m3-attribute-20261022-01` passed 67 assertions / 6 groups; `m3-statistics-20261022-01` passed 73 assertions / 8 groups. Both builds had empty compiler logs, no original reads or windows, and no repeated old suites. These are isolated console fixtures, not a new playable scene package. Full M3 remains pending.

Eight additional constant bytes, 214 bytes of lookup code and 721 bytes of directly called insertion helpers were read with original hashes preserved. The cumulative distinct M3 window scope is 5,878 bytes plus 12 constant bytes. Missing-key insertion still needs three observed helpers; default success, initialized enemy attributes and attack/death production are not supplied. The owned death/reward bridge design records the necessary same-session journal/wallet integration and remains unimplemented. Current resumable state and bounded next steps are in [M3-NEXT-GAMEPLAY-STEP.md](M3-NEXT-GAMEPLAY-STEP.md).
