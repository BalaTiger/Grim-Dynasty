# Game mouse query: prepared and tested, blocked before launch

Status on 2026-10-22: **no Game pseudocode was obtained by this attempt**. The fixed query command received formal execution approval, then exited with code 1 before any backend launch:

```text
Less than 5 GiB free physical memory; backend not started
```

This is a resource-gate result, not an approval denial, monitor failure, Ghidra failure or Game analysis result. The initial free-memory check runs before the original-binary hash read or evidence-directory creation. No Game backend, original game or new GUI was launched. The `docs/reverse-client/r1-game-mouse-calibration` output directory was absent when this checkpoint was recorded.

The subsequent single approved read-only preflight at **07:32:28.7024451 UTC** reported available physical memory **3,759,390,720 B (~3.50 GiB)**, committed bytes **48,889,872,384** of **60,959,854,592**, and an external `dotnet.exe` process (PID 37360, parent 42648). Its project/owner was not identified and it was not stopped. The raw values, failed command and test outcomes are retained in [resource-block.json](../../.tooling/r1-game-target-profile-validation/resource-block.json).

## Completed prerequisites

- The production same-handle terminal-state monitor repair is integrated with backups; the real R0 query passed 8/8. See [R0 result](R0-RETRY-RESULT.md).
- Real Engine input/camera queries passed 11/11, followed by the four-entry batch passing 17/17. Original hashes were unchanged and both owned backends closed. See [input/camera evidence](R1-INPUT-CAMERA-EVIDENCE.md) and [mouse-core evidence](R1-MOUSE-CORE-EVIDENCE.md).
- Review found Game-specific runner issues before any real Game launch: the open-session label, the monitor descriptor's Engine-only hash and the Engine-only snapshot argument. The corrected code selects only the fixed Engine or Game profile; descriptor and argv stay bound to the same selection. Unknown targets fail before sampler spawn. Default Engine behavior, Job ownership, creation identity, descriptor freshness, exact Java argv and 384M checks remain.
- **13/13 new synthetic target tests passed**, exit 0. **52/52 existing affected Engine role/Job tests passed**, exit 0. These are test cases, not assertion counts or native-runtime observations. Logs are under `.tooling/r1-game-target-profile-validation/`. The old native terminal-race suite and gameplay/model suites were not rerun.
- The final offline Game plan verification passed. It does not validate real Game startup or the actual descriptor lifecycle.

## Exact continuation

After the external workload has ended and a fresh preflight confirms both physical and commit headroom at least **5 GiB**, no external build/UE/Java workload, and no prior task runtime, use the existing formal execution mechanism for:

```powershell
& 'D:\NodeJS\node.exe' 'C:\Users\zhuzi\Documents\Grim-Dynasty\tools\static_analysis\rea_native_registration.mjs' --r1-game-mouse-calibration
```

The frozen plan `rea-r1-game-mouse-calibration-plan.json` SHA256 is `608d4fd05ffacecb95b83aed40ecce39092192282fe0a54f319b2fb06dadc70b`. It contains eight calls, including only `procedure_pseudo_code`, `procedure_assembly` and `procedure_callees` for **Game `18015E1F0` / ControllerPlayer::HandleActionFromMouse**. Target SHA256 is `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`.

Keep the single 384M backend, 2 GiB running stop threshold, existing 330-second provider startup and 600-second request limits. The 5 GiB launch requirement is a provisional first-Game calibration choice, not an observed Game peak. Do not lower it, stop another project's process, change permissions or automatically repeat a failed query. This checkpoint has no uncertain in-flight operation.

After a successful result, inspect the actual controller body before selecting the next finite dependencies; do not infer mouse controls from export names or repeat the completed Engine batches. Navigation's known next direct target is Engine `18010C6C0`. [Platform inventory](R1-PLATFORM-INVENTORY.md) records the independent scene/rendering gaps. R1, R2 and R3 remain unaccepted; the old WASD/GDI+ prototype is preserved and stopped.
