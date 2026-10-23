# M3 content and integration gates

These are results from bounded read-only evidence, not a compatible combat system. The accepted runnable scene is now `artifacts/m3-patrol-20261022-01/gd-scene.exe`; `m3-scene-20261022-03` retains the last actual window/input smoke. The patrol package passed headless checks and bitmap rendering; it was not automatically opened in a foreground window.

## Actual content selected

`content-candidates.json` inspected the 34,180-entry index, decoding no record bodies. It found 1,307 `Monster` and 1,260 `ArmorProtective*` entries. Class membership alone does not establish a usable enemy or equipment item.

`m3-content-slice/content-slice.json` subsequently decoded exactly two sorted candidates:

| Record | Observation | Consequence |
|---|---|---|
| `records/creatures/enemies/boss&quest/waveevent_burrwitchrift_zombie_a01.dbr` | Monster, 946 fields; `characterLife=0`, attribute equations reference `records/creatures/enemies/bios/bio_zombie_01.dbr`; level range 1..250 | Do not initialize an enemy with zero life, or claim level-one suitability from its name. Formula/loading semantics remain necessary. |
| `records/items/crafting/crafting_randomtorso_caster.dbr` | ArmorProtective_Chest, 658 fields; defensive protection 0; `itemCostName=records/game/itemcostformulas.dbr` | This crafting placeholder is not accepted wearable armour. Neither its zero protection nor the lack of a direct price establishes runtime equipment or merchant behaviour. |

Both reads retained the fixed original ARZ SHA256 `9e3532250cdb611a4cc989997c8f97fcec2d6c422063a845e6c43cd538c30603` before and after. No referenced record was automatically followed. The known sword's physical min/max 9/19 also does not establish random selection, hit chance, attack cadence, or final damage.

## Scene bridge

- Existing `KillCondition.Receive` counts each matching event, including a repeated victim/event. An owned event identity/commit ledger is needed before feeding it from scene deaths; the receiver does not prove native death production or native deduplication.
- Use `Receive/Evaluate` with project-owned scene presentation. Do not keep the fixture clock at zero and call the full `AutomaticTasks.Update`: its cinematic text display branch becomes Unsupported after the observed display-delay condition.
- Agent patrol/chase/return geometry may be a project-owned input/presentation adapter, like the accepted scene movement. It must not supply invented native health, damage, prices, attributes or equipment callbacks. Attack intent remains explicitly Unsupported until the combat producer is connected.
- New persistent actors/events require an explicit scene schema/content transition and validation. The patrol package writes schema 2/content `liaoxi-scene-v2` and explicitly migrates only the exact old schema 1/content `liaoxi-scene-v1`. Missing v2 patrol state is corrupt; silently resetting or ignoring actor state is not acceptable.

## Smallest remaining gameplay closure

The first gameplay milestone still requires one explicit enemy/attack profile with established initial attributes and attack-to-settlement inputs, an observed health/death path, an owned once-only event bridge, and save/reopen preserving that state. Merchant pricing/order and actual equipment qualification remain separate requirements for full M3. More low-level constructor coverage is not a prerequisite for this work.
