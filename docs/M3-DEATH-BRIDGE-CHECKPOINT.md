# Death ownership and first statistics record checkpoint

Accepted on this host on 2026-10-22, at 05:40 / 05:42 UTC. These are internal console deliverables, not a new playable combat scene.

## Deliverables and reproduction

- `artifacts/m3-death-bridge-20261022-01/DeathBridgeChecks.exe`: explicitly external death fixture -> existing KillCondition -> same Session wallet/journal -> durable scene candidate -> reopen. `death-acceptance.json` records 297 new session checks, 22 new scene checks, 15 prepare-process and 47 fresh-process continuation checks, and one pinned pre-change M2 save read. Affected regressions: 42 M2 core, 18 CLI cases / 53 CLI assertions, 22 scene, 16 patrol integration checks. Both compiler logs are empty.
- `artifacts/m3-first-insert-20261022-02/NativeStatisticsFirstInsertChecks.exe`: first empty statistics node allocation/insertion and actual Settlement.Final binding. 46 new assertions / 6 groups, plus 73 affected existing-statistics assertions / 8 groups. Compiler log is empty. `-01` retains the first compilation failure (test runner omitted Main's string[] argument); no tests ran in that attempt.
- The latest interactive scene remains `artifacts/m3-patrol-20261022-01/gd-scene.exe`. It is preserved, not rebuilt or launched here. It still has movement/collision/tasks/patrol/save but no combat/trade/equip. No new screenshot or Library upload.

Reviewed build commands, each with a fresh project artifacts directory and formal execution approval:

```powershell
.\tools\build_m3_death_bridge.ps1 -PackageDirectory 'C:\Users\zhuzi\Documents\Grim-Dynasty\artifacts\<fresh-bridge-name>'
.\tools\build_m3_attribute.ps1 -Component first-insert -PackageDirectory 'C:\Users\zhuzi\Documents\Grim-Dynasty\artifacts\<fresh-statistics-name>'
```

## What is now reusable

The death bridge uses one ordered typed journal and the actual session wallet; a subsequent delivery/inventory command no longer loses a side reward. The fixture with two admitted deaths and +25 reward can continue through both old delivery tasks to 190 money. Duplicate receipts after reopening do not award again; a capped successful reward still consumes its receipt; an empty reward plan executes no amount-zero action. Source character, encounter, profile hash, victim generation and exact record name are admitted explicitly before deduplication.

Schema 1 remains readable with its original hash calculation and string journal. Migration to schema 2 is explicit, through internal fixture activation. Unknown/corrupt states are rejected. A saved pre-change M2 fixture (SHA256 `c9aa2cde9e62e694d7703a82ebb24ca142f4d2abdee5d24d2711a3af68eaea9b`) was read without modifying its package.

Prepared death candidates bind the source object, full journal/state fingerprint, revision and store fingerprint, and seal candidate state. Thus an unsaved ordinary command invalidates a stale candidate. Scene commit clones current world/patrol, saves the whole candidate atomically, and returns it only after success. Lock and stale-disk failures preserve both the visible scene and existing save. In-memory facing is preserved by the clone; the old scene save format still does not persist player facing.

The first-statistics continuation reuses the actual first lookup result/output/trace and resumes the second lookup, rather than adding a preliminary lookup. An actual fresh managed node represents the observed logical native allocation request of 0x28 bytes. It follows the 15 observed write instructions, with exact pointer/key/word/float widths and partial-state stops. Nullable color/count fields were added to the existing shared state; the old query algorithm stayed unchanged. Nonempty missing-key insertion/rotations, allocator failure/new-handler behavior and native container initialization remain outside coverage.

## New bounded native evidence

- `019C90..019CD5`: 69 bytes, first-node storage and its three head links. Reused old project evidence for `5DA6C4`; no new CRT chain expansion.
- `04DA10..04DCB7`: 679 bytes. Explicit empty root insertion, count/head links, root color and caller-output identity. Nonempty rebalance bytes were retained as evidence, not implemented as a guessed generic map.
- `49CB80..49CCC5`: 325 bytes. Two listener lists (strides 4 and 0x30), object lookup, class filtering against A5A770, instance virtual +1B8. It forwards arguments; it does not yet establish event11 production. See `m3-evidence/DEATH-NOTIFICATION-DISPATCH-BOUNDARY.md`.
- `character-input-targets.json`: 3,562 metadata bytes, zero body bytes. `Character::Load` is exactly RVA054E70 with `.pdata` range [054E70,058440), 13,776 bytes. The exact `Character::Attack` prefix did not match. No alternate-name or body scan followed automatically.

Game.dll SHA256 remained `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2` before/after every read. Through these reads, distinct M3 code/window scope totals 6,951 bytes plus 12 constant bytes; this is not a project-completion percentage.

Subsequent fixed metadata-only read: `listener-classinfo/inspection.json` records 48 bytes at A5A770. Its +08 disk name pointer is zero, so the permitted name read was not performed. There were 688 PE metadata bytes, zero new code bytes, zero subprocesses, and preserved before/after hash. This leaves the runtime initialization writer and instance callback identity unknown; no default class/event meaning was assigned. Total additional class-record data is 48 bytes, counted separately from code and constants.

## Still missing for playable combat

The bridge deliberately accepts only an internal `ExternalFixture` origin. Neither CLI nor interactive scene can manufacture a death. Actual enemy level/bio/HP binding, attack damage production, gate +838 and complete damage/death notification still need evidence. Raw enemy life=0 is not initialized HP; sword min/max9/19 do not define fixed damage or random selection. Existing `charLevel` resolver evidence is not proven to mean this enemy's level. Equipment/merchant transactions and the calibrated 10-20-minute route remain later M3 requirements.

Current narrow follow-up: identify A5A770 via its directly referenced class record; determine the minimum Character::Load evidence needed for initialized life. No full engine/constructor/CRT expansion. Continue under existing M3 authorization; no new permission question is needed unless a concrete operation requires approval. No original game, Rea/Java, player save/cloud access, installs, security changes, commit/push or deployment. Unchanged getter/settlement/M1 suites were not rerun. Checkpoints store hashes/references, not source backups; per-task token/cost data is unavailable.
