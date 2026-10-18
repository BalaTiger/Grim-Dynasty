# Cold initialization and adjusted completion

This continuation supersedes the pending Name initialization and unintegrated point-action statements in `README.md`; that file and its original acceptance record are retained as historical evidence.

`tools/build_offline.ps1` now builds ten own managed modules and six standalone original scenario CLIs. The complete build passed 532 fixed assertions: the prior 487, 31 cold-name/adjustment/completion assertions, and 14 tribute assertions. `AdjustedQuestDemo.exe` starts with cold caches on difficulty 2, consumes two explicitly supplied kill events, completes the task, and writes wallet 50→55, skill points 4→10, attribute points 3→6, three reward-history banks, one parent reward-task UID, and an attribute notification. It then unregisters the kill handler. These are actual own runtime writes, not native game execution.

## Name first use: Rea succeeded

Rea 6.5.0 successfully queried `Engine.dll` Name::Create at `0x180150150`, by exported symbol and address. `rea-name/Name-Create.json` and `Name-Create-assembly.json` retain the successful raw results and provenance. The assembly sign-extends each source byte before XOR and multiplication by `0x1000193`; the initial value is `0x811c9dc5`, arithmetic wraps to 32 bits, and the input ends at the first NUL. This is not an interning table or persistent registration.

Both GiveMoney and GiveSkillPoint pass the literal `HeadEffect`, at Game.dll `0x1806bc640` and `0x1806bc6d0`. Its hash is `0xcb999846`. Their local static result is not used in subsequent reward arithmetic. Game.dll `_Init_thread_header` at `0x1805da920` and `_Init_thread_footer` at `0x1805da8c0` implement the compiler's synchronized first-use guard and epoch publication. Those helpers are direct Ghidra evidence, not Rea queries.

Rea also successfully recovered the Name string helper at `0x180022ce0`: this short, nonaliasing input takes the direct memcpy and terminator path. `HeadEffect` fits the 15-byte local buffer, so this use does not require heap allocation or a name table. The helper raw result retains Ghidra's terminal-flow caveats for other branches; those branches are not silently claimed recovered.

`RewardNames.cs` restores the hash and supports an explicitly known cold cache. A managed lock provides deterministic first-use semantics for this own runtime; it does not emulate Windows CRT TLS addresses, epoch values, allocation failure or native scheduling. Cache state is explicit and may be shared by the caller across runtime snapshots. Unknown or inconsistent caches remain Unsupported. Legacy warmed-cache snapshots continue to work.

## Reward adjustment and execution

`AdjustedCompletion.cs` implements the evidenced Task::AdjustRewards algorithm at `0x1804277b0`: evaluate each completion event under overrides 0, 1, 2, restoring the default override after each evaluation; only events true at all three receive a multiplier. The multiplier counts unrecorded reward-history banks from difficulty 0 through the current difficulty, excluding lower banks suppressed by player profile bits 1 and 2. An event failing any override remains at sentinel -1 and its current action value is not modified. A universal event with no missing banks receives multiplier 0.

All eligible action adjustments precede execution of any event. Skill and attribute actions multiply their immutable original amount at +0x14, wrapping to 32 bits and writing the current amount at +0x10. Money and tribute ModifyValue hooks are evidenced no-ops. Actual-difficulty event evaluation then selects execution in original vector order. The common automatic completion code records reward history and reward task UID and ends condition lifecycles afterward.

Supported action IDs are 4 (money), 15 (skill point), 18 (attribute point), and 26 (tribute), all version 0. Supported completion conditions are known null pointers or AND/OR collections of IsDifficulty v0 conditions with the default override. Other condition kinds, partial snapshots and aliased action objects return Unsupported before completion commits. This is a reusable adjustment loop with a bounded condition/action domain, not all original reward behavior.

## Additional restored branch: tribute

GiveTribute at `0x18046d160` calls Character::AddTribute at `0x180062290` for positive amounts. Addition wraps before an unsigned cap of 150. If the new value exceeds the old value, it sends a kind-only event 0x3F **before** writing field +0x16C8. Negative amounts use wrapped signed addition and a nonnegative clamp; zero clears the field. Neither produces that notification. Tests cover cap, overflow, zero, negatives, missing actors, unsupported subscribers, and automatic task-to-tribute completion. The GiveTribute vtable at `0x180796018` points to no-op before/after/ModifyValue hooks and the existing scalar Read implementation.

## Remaining static boundaries

Attribute event 0x32 and tribute event 0x3F production support known empty subscriber routes. Their nonempty native UI subscriber implementations remain Unsupported. Game.dll IAT registration xrefs and observed constructors were inspected; xref coverage is explicitly limited to the analyzed program and reference model. Grim Dawn.exe imports the same Register interface at `0x1402d5ab0`, but its on-disk .text has entropy 7.9999 bits/byte and its entry RVA `0x448310` is in .bind. A raw scan found no ordinary RIP-indirect call/jump candidates to that import. Packing/protection is a plausible cause, not proof of a particular loader or proof that the subscribers reside there. No unpacking, execution, memory dumping, DRM change or bypass was attempted. `exe-static-subscriber-boundary.json` records the measured evidence and uncertainty.

The further Rea Engine Register xref query succeeded: the three returned addresses `0x180371670`, `0x1803b4098`, `0x18042b968` map to nonexecutable .rdata, .rdata and .pdata. The observed Game constructors use routes 0x06, 0x0B, 0x21, 0x28, 0x2A, 0x2E, 0x39 and 0x3A, rather than 0x32. This bounds the investigation without proving the absence of indirect registrations or proving any original UI handler identity. Raw Rea results are in `rea-engine-events/`; direct Ghidra constructor evidence is in `subscriber-constructor-routes.json`.

GiveDevotion's positive branch writes point fields and then localizes text and calls ShowCinematicText; these visible effects are not silently discarded. GiveExperience dispatches CharacterExperienceOutbound, so it is not implemented as an invented direct XP increment. Both remain pending rather than falsely Known. Static evidence is retained in `reward-next-branches.json`; no original devotion or constellation data was changed.

Current acceptance, source/evidence hashes and package manifest are recorded in `acceptance-adjusted.json`. Original installation, saves, Steam/system settings were not written; no software install, original process launch, commit, push or publication occurred in this continuation.
