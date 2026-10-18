# Reward write audit and supported scope

The independent runtime now supports an original two-kill task reaching automatic completion, writing its wallet from 50 to 125, recording the task UID in its parent quest reward vector, marking reward history, and unregistering its kill-condition handler. These are actual writes in our own managed runtime, not original game execution or a playable game.

Run `tools/build_offline.ps1` to build ten own DLLs and five original scenario executables under `.tooling/offline-system`. `RewardQuestDemo.exe` demonstrates the chain. The complete build passed 487 fixed assertions: the prior 436 plus 17 money, 14 completion integration, and 20 point-action assertions. No original DLL is linked or loaded. The package manifest records all dependencies and SHA256 values.

## Static evidence

All addresses below refer to the installed x64 Game.dll with SHA256 `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`.

| Function | Address | Evidence |
|---|---|---|
| GiveMoney Execute | 0x18046c410 | ../task-kills/ghidra-selected/reward-money.json |
| Character AddMoney | 0x180062240 | same |
| GiveSkillPoint Execute | 0x18046ceb0 | same |
| GiveAttribPoint Execute | 0x18046aae0 | ../task-kills/ghidra-selected/reward-attribute.json |
| Point ModifyValue | 0x18046ac70 | ../task-kills/ghidra-selected/reward-read.json |
| Task Complete | 0x180426980 | ../token-actions/rea/Task-Complete-assembly.json and prior task evidence |
| Task AdjustRewards | 0x1804277b0 | ../task-runtime/rea/Task-AdjustRewards.json |

The three new reward JSON files were produced with the already installed Ghidra 12.1.4 and our observed-entry scripts using the cached project, not successful Rea MCP calls. Their `source` field says so. Earlier Rea Task-Complete/AdjustRewards evidence is retained separately. A decompiler name may represent a folded function alias: the condition override setter is evidenced by its vtable slot and `MOV [RCX+0x24],EDX`, not by the unrelated exported alias displayed by Ghidra.

## Exact supported branches

GiveMoney v0 executes signed amount branches. A positive amount wraps at 32 bits before an unsigned cap of 2,000,000,000. A negative amount wraps signed addition then clamps negative results to zero. A zero amount **clears the wallet**. Character AddMoney has different zero behavior. Tests preserve that distinction.

MoneyCompletion accepts known null condition pointers only, previews the full event/action sequence before committing, and executes actions in event order. It writes task/reward history in the evidenced order and appends the task UID once when a reward action exists. Repeating after reset appends another UID; it does not invent deduplication. Missing UID lookup is a no-op only with a complete index. Unknown parent identity or reward storage rejects before writes.

PointRewards is separately callable; it is **not yet integrated into MoneyCompletion or automatic quest reward adjustment**. Skill points use field +0x176C, wrapped signed addition and a nonnegative clamp. Attribute points use +0x1768 with wrapped addition and no clamp, then construct and send event 0x32 containing the player object ID at +4. Zero attribute grants still produce the notification. Both skip grants on difficulty 0/1 when profile flag 1/2 respectively is set. Difficulty 2 does not read those profile bits. Point ModifyValue uses original amount +0x14 multiplied with 32-bit wrap; repeated calls do not compound current amount +0x10.

The attribute implementation produces its notification from the write operation, rather than receiving a predeclared input event. It supports a known empty subscriber route only. Nonempty or incomplete subscriber state is Unsupported before mutation; event-0x32 UI subscribers are not recovered. This does not recover native combat, death, loot or inventory event production.

## Validation and limits

`MoneyCatalogProbe.exe <authorized Quests.arc>` read all 247 definitions and passed 254 actual amount parameters, including one negative amount, through the own unconditional scenario. It did not reproduce those original quests, their conditions, or a native dynamic oracle. `acceptance.json` stores this result, current source hashes, evidence hashes and the package manifest.

Money and skill actions require an explicit warmed internal Name-cache snapshot. First-use Name::Create and thread-cache initialization remain Unsupported. Stable player, difficulty/profile, closed UI gates, empty objective actions, supported event routes, and outbound suppression are explicit runtime scope requirements. Non-null completion conditions, the general AdjustRewards loop, experience/devotion/item/faction rewards, actual combat producers and reward UI remain pending. No inferred success is substituted for those branches.

No original process was launched; no original installation, real/cloud save, Steam setting or OS setting was written. No software was installed and no commit, push or publication occurred in this continuation. Existing source versions are preserved as `.before-rewards` and `.before-points` backups. This result is an offline subsystem increment, not completion of the independent game or the Ming-era playable slice.
