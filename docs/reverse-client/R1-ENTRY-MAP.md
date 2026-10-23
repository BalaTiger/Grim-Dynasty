# R1 entry addresses: export identity only

The approved metadata query completed at **2026-10-22 06:05:07 UTC**. All six exact symbols were found as non-forwarded exports in executable `.text` sections. No function body was read or decoded, and no backend or original program was started. Section permissions do not establish function boundaries or behavior.

| Module | Export, abbreviated from its exact decorated name | RVA | Preferred VA | Saved importing module |
| --- | --- | --- | --- | --- |
| Game.dll | `ControllerPlayer::HandleActionFromMouse` | `0x15E1F0` | `0x18015E1F0` | Grim Dawn.exe |
| Engine.dll | `NavManager::FindPath` | `0x150590` | `0x180150590` | Game.dll |
| Engine.dll | `Engine::GetInputDevice` | `0x077850` | `0x180077850` | Game.dll |
| Engine.dll | `WorldCamera::UpdateFromInput` | `0x04E170` | `0x18004E170` | Grim Dawn.exe |
| Engine.dll | `WorldCamera::GetRayThroughImagePoint` | `0x229700` | `0x180229700` | Grim Dawn.exe |
| Engine.dll | `DisplayWidget::HandleMouseEvent` | `0x00D480` | `0x18000D480` | Grim Dawn.exe |

Both modules have preferred image base `0x180000000`; these are static preferred addresses, not observed runtime addresses. Complete decorated names, ordinals and name-table indices are retained in the [inspection report](r1-entry-addresses/inspection.json).

The report records **7,076 semantic metadata bytes**: Game 1,974 and Engine 5,102, with 14 and 54 distinct export names encountered respectively. Binary search queried the six fixed names; no full export traversal, `.pdata` lookup, function-body read, call-chain expansion or subprocess occurred. Full-file hashing is separate from these metadata totals.

| Original artifact | Bytes | Before/after SHA256, identical |
| --- | ---: | --- |
| `D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll` | 11,385,856 | `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2` |
| `D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll` | 4,485,632 | `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153` |

Evidence pins:

- [Inspection](r1-entry-addresses/inspection.json): `45d64a0445791c15add8b0a21e3a08f949d06877281cf106fdb84bfdcd991a4a`.
- [Fixed plan](r1-entry-addresses-plan.json): `302b62b56c9ba40f61e49ec8da6224cfa8733f1a468b1ab84652f91681467154`.
- [Reader](../../tools/static_analysis/discover_r1_entry_addresses.mjs): `8a29b38c4056ef778213913a0ca14d93ebdf66d07c1b6bbe3aadc32291d2ed12`.
- [Saved import provenance](../phase-1/boundary-audit/module-boundaries.json): `d1ec79916baeb45b3257f7c7c2ce356c262f804ae9b2577888a0d14de53e7ba7`.

## Bounded next recommendation and limits

The next candidate pair is **Engine `077850` and `229700`**, for separately approved Rea inspection of input-device access and image-point ray construction. Their names motivate selection; their contracts, sizes, outputs and dependencies remain unverified. Do not automatically follow their callees or expand to the other four entries.

This metadata result does not resolve R0's reported monitor error 5 or authorize execution around it. No new execution plan is created here.

R1 still lacks proved mouse-target selection, click-to-move dispatch, input-update ordering, pathfinding/collision rules and failure behavior. The imported names do not establish caller sites or a chain between these functions. UI mouse handling is not evidence of world-target selection; a camera ray is not a hit test. No native runtime, whole-input-chain or gameplay compatibility claim follows from this address map.
