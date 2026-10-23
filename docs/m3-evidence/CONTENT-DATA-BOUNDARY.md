# M3 content data boundary - 2026-10-22

Status: the root agent formally executed the fixed two-reference plan successfully. `content-formulas/manifest.json` records `Collected`, two invocations/two successful decodes, both exit codes 0, the final reviewed script hash `076aaccce3c8a2bb38a09b988110d72a4cdb0c66473c4d33dff44a1e8f7c6065`, unchanged original ARZ before/after hash, and unchanged accepted M1 executable hash. This update only reviews those saved outputs; no new execution, original read, formula evaluation, or reference expansion occurred.

## Saved facts

Source: `m3-content-slice/content-slice.json`, SHA-256 `3e5fe5ce265fa2367685d36531fd6bde2ebbbfc5af0b9da0fcbd9f60739a804d`. Its original ARZ before/after hashes both equal `9e3532250cdb611a4cc989997c8f97fcec2d6c422063a845e6c43cd538c30603`.

| Selected actual record | Directly observed fields | Boundary |
| --- | --- | --- |
| `records/creatures/enemies/boss&quest/waveevent_burrwitchrift_zombie_a01.dbr` | Class `Monster`; `templateName=database/templates/monster.tpl`; `characterLife=0`; `characterAttributeEquations=records/creatures/enemies/bios/bio_zombie_01.dbr` | This is a sorted-name wave-event candidate, not an established level-one combat opponent. Stored `characterLife=0` does not establish runtime initial health or death. The equation text is now known; its inputs, evaluation order and overrides remain unverified. |
| `records/items/crafting/crafting_randomtorso_caster.dbr` | Class `ArmorProtective_Chest`; `templateName=database/templates/armor_chest.tpl`; `itemNameTag=tagCraftRandomTorso_Caster`; `itemLevel=1`; `itemClassification=Magical`; `defensiveProtection=0`; `itemCostName=records/game/itemcostformulas.dbr` | This crafting random-torso placeholder is not a concrete equipment acceptance sample. Its stored level/protection values do not establish generated armor statistics or equip effects. The cost reference alone establishes no merchant buy/sell contract. |

## Formula records now observed

Both decoded `record.class` values are exactly the empty string `""`; do not substitute a class inferred from `templateName`. All fields in these two records have `field_types=2` (strings), including formula text and template names. No numeric evaluation or floating-point precision is established by decoding these strings.

`bio-zombie.json` has template `database/templates/characterattributeequations.tpl`, body SHA-256 `2ac2f28a6485fbdc2d985765ba82786eb7b414bcd832b98f1f77710d9ddd4593`, and nine formulas. Exact examples:

| Field | Stored text |
| --- | --- |
| `characterLife` | `((charLevel*4)^1.33)+24` |
| `characterStrength` | `(charLevel*4.5)+10` |
| `characterDexterity` | `(charLevel*6.5)+10` |
| `characterIntelligence` | `(charLevel*6)+15` |
| `characterOffensiveAbility` | `(charLevel*6)+5` |
| `characterDefensiveAbility` | `(charLevel*3)+25` |
| `characterMana` | `((charLevel*8)^1.22)+100` |
| `characterLifeRegen` | `(((charLevel/15+1) + lifeRegen) * (1 + lifeRegenMod/100))*elapsedTime` |
| `characterManaRegen` | `(((charLevel*2+10) + manaRegen) * (1 + manaRegenMod/100))*elapsedTime` |

`item-cost-formulas.json` has template `database/templates/itemcost.tpl`, body SHA-256 `31851adc97ae1c6f86b33c447c2198331189d364dd22044ea57d1c5407af8925`, seven cost formulas and fifteen requirement formulas. The latter cover named armor locations, weapons, shields, rings and amulets. Relevant exact texts:

```text
armorCostEquation = 0.88 * (((defenseAttrArmor*0.12+itemLevel*4)^1.1)+0.8*(itemPrefixCost+itemSuffixCost)+(itemLevel*(totalAttCount-1))*8+30)
weaponCostEquation = 0.88 * ((((itemLevel*5)+(damageAvgBase/(1-charAttackSpeed)*(1+damageAvgPierceRatio/52))*0.5)^1.1)+0.8*(itemPrefixCost+itemSuffixCost)+(itemLevel*(totalAttCount - 2))*15+80)
chestStrengthEquation = (1*(itemLevel*6.7)^1.3889-5.8*(itemLevel*1.62)^1.41)*0.97+35
swordDexterityEquation = 0.33*((itemLevel*21)+5.0*((damageAvgBase*0.5)*(1+(damageAvgPierceRatio/25)))-9.2*(0.05*(itemLevel^1.65)-1))-3
```

Cost inputs vary by selected formula: `itemLevel`, `itemPrefixCost`, `itemSuffixCost`, `totalAttCount`, plus armor `defenseAttrArmor`, weapon `damageAvgBase`/`damageAvgPierceRatio`/`charAttackSpeed`, or shield `shieldBlockDefense`/`shieldBlockChance`. Requirement formulas use their own subsets of level, base damage, pierce ratio, attribute count and shield fields. None of those variable names proves a binding to a similarly named stored DBR field. Missing inputs must not become zero.

The text includes denominators `1-charAttackSpeed`; `weaponRanged2hCostEquation` instead contains `1-charAttackSpeed+0.01`. Preserve that difference. No accepted runtime parameter range, division-by-zero policy, interpretation of `^`, rounding, overflow, or non-finite policy has been established. These are dependencies/conditions for a future evaluator, not evaluated values or a native numeric contract. The saved formulas contain no observed merchant buy/sell multiplier or transaction/ownership procedure.

## Remaining boundary and a bounded tool option

The two direct data references are closed, but loading/selection and evaluation remain separate: actual `charLevel` and difficulty/scaling inputs; whether/how the monster's direct fields combine with equation results; modifier order, clamps and final initial life; requirement formula selection versus explicit requirements; generated item/affix values and attribute-count definition; and conversion from cost to merchant prices. The crafting placeholder still supplies no concrete armor acceptance sample. Damage settlement and equipment actions are not recovered by this data read.

A safe next implementation option is an **expression inspection tool** over these saved strings: preserve text/hash, recognize only observed literals, identifiers, parentheses and `+ - * / ^` tokens, report variable dependencies and explicitly missing caller-supplied inputs, and reject unsupported syntax. It need not assign precedence, interpret `^`, or compute values. Any later evaluator must declare its own numeric model and explicit inputs until native evaluation evidence exists; its results must not be labeled native-compatible health, requirements or prices. This is a proposal, not implementation or permission to expand evidence collection.

The saved output SHA-256 values recorded by the run are `0d2bb942186b76c3f6fceedd8c12fe0e504e5a7d79cf0a5803e22e1ece395b1c` (`bio-zombie.json`) and `5f1a015c96f1ea5927b6c7777454cd436285ee55dfb3ec7139d42e8d28811e37` (`item-cost-formulas.json`). The reader script is frozen. No formula model/test or new original read was run for this report update.
