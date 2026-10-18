# Local experience completion

This continuation adds a separately callable experience branch and a new original scenario, without changing the protected-subscriber boundary. The current guarded build produces ten own DLLs and seven standalone original scenario executables and passes 563 fixed assertions: the prior 532 plus 31 experience assertions.

`ExperienceQuestDemo.exe` consumes two explicitly supplied kill events, automatically completes its task, constructs and locally dispatches an experience Bonus, and writes own runtime experience 100→175, lifetime experience 200→275, formula input -1→4, and a life-statistics row. Level remains 4 and canonical life/energy/stamina remain 100/40/30. The parent reward task UID is recorded and the kill handler is detached. This is an actual state-writing own scenario, not original dynamic validation or a restored combat producer.

## Rea evidence

Rea 6.5.0 completed all 14 planned static queries successfully. `rea-experience/` retains the raw calls, provider provenance, decompiler caveats, progress and stderr. The parallel cached Ghidra exports remain separately labeled in `docs/task-kills/ghidra-selected/experience-*.json`; they are not misrepresented as Rea results.

| Function | Game.dll address |
|---|---|
| CharacterExperienceOutbound | 0x1802ed8f0 |
| Outbound HandleBonusToClient | 0x18030d1c0 |
| Inbound HandleBonusToClient | 0x18030fc70 |
| PlayerManagerClient HandleBonus | 0x1803f6e80 |
| Character TakeBonus | 0x180067e30 |
| Character ReceiveExperience | 0x18005fe30 |
| Experience-control increment helper | 0x18028e990 |
| SkillManager AddExperience | 0x180540740 |
| CharacterBio TakeBonus | 0x18008c5d0 |
| CharacterBio GetBonusLifeAmount | 0x18008c4c0 |

Additional Rea queries recovered the current-experience field getter and last/next getter wrappers. Those latter wrappers delegate to further formula helpers and are not claimed to provide a recovered threshold interpreter. The input Game.dll SHA256 is `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`.

## Restored path

GiveExperience v0 first initializes its own `HeadEffect` Name cache, resolves the main player, then calls CharacterExperienceOutbound with player object ID and unsigned amount. The static literal at `0x1806bc5b8` is again `HeadEffect`. Its action vtable's ModifyValue hook is RET; experience amounts are not multiplied by missing reward-history banks.

CharacterExperienceOutbound constructs a Bonus with target ID at +0, unsigned experience at +0x38, experience source 2 at +0x3C, origin ID 0 at +0x60, and zero/empty other fields. Its virtual slot +0x90 dispatches HandleBonusToClient. The GameEngine constructor initializes its default active interface from the inbound interface, while another interface invokes network transport. Our route snapshot explicitly selects LocalInbound; NetworkOutbound and Unknown return Unsupported before mutation. No pretend dont-propagate flag is added to this method.

The local interface obtains PlayerManagerClient. Its HandleBonus compares the Bonus origin against client player ID; equality suppresses processing. Otherwise it resolves a Player-compatible target and invokes virtual +0x660. The Player primary vtable points that slot to Character::TakeBonus. Its source-2 experience path runs before the IsAlive check, so a known dead target may still receive experience. The direct CharacterExperienceOutbound entry supports the evidenced missing-client-manager no-op without initializing an action cache. The GiveExperience action entry additionally requires a stable client identity across its repeated queries.

ReceiveExperience queries experience gain modifier 0x37. The current implementation requires its explicit result to be zero. It increments +0x1764 with uint32 wrap and writes the signed clamped equation input at +0x1710, then compares against the rounded threshold. The comparison uses unsigned accumulated experience against `(uint)(int64)(floatResult + 0.5f)`. In the supported below-threshold branch, no level-up hook runs. A known empty SkillManager skill vector makes its AddExperience return false. Finally lifetime field +0x1968 increments with uint32 wrap.

For an alive target, zero other Bonus fields do not universally mean no effects: native Bio code still applies clamps, updates a dirty flag when +0x16B1 is nonzero, and touches the life-statistics map. `ZeroBonusBio` therefore requires stable finite getter snapshots and canonical bounded life/energy/stamina. Within that domain the clamps retain their values; the dirty +0x1698 write and map operator[]/float addition are implemented. Over-cap, nonfinite or partial Bio contexts remain Unsupported rather than silently dropping changes.

ExperienceCompletion supports experience-only completion events, with known null gates or the existing IsDifficulty condition subset. It previews the complete ordered award sequence before task completion commits. A later action crossing a threshold prevents earlier awards, cold-cache initialization, reward history and UID writes. General mixed action vectors remain outside this provider; existing money/point/tribute providers remain available.

## Explicit remaining limits

The threshold is an explicit sampled virtual-evaluation result bound to the current equation input. The original equation parser/interpreter is **not recovered**. The original base ARZ player-level record and its experienceLevelEquation field were inspected read-only; no assumption about DLC override resolution was made. The new scenario uses its own declared threshold sample 1000 at level 4, not a claimed official threshold or changed original balance.

The model currently covers a finite threshold-result domain up to 1,000,000,000 and known level below maximum. Threshold crossing, maximum-level XP clamping, nonzero modifiers, nonempty/unknown devotion-skill experience vectors, level-up skill/pet/mastery updates, particles, sound and cinematic text remain Unsupported. The scenario's printed readout is own CLI output, not a recovered native UI data model. Existing native 0x32/0x3F nonempty subscribers remain Unsupported; no protected code was unpacked or bypassed.

Current acceptance is `acceptance-experience.json`, including package/source/evidence hashes, the scenario trace and unchanged original-file hashes. No original process, real/cloud save or network transport was used. No system/Steam setting, installation, commit, push or publication occurred. Prior source versions and acceptance records are retained.
