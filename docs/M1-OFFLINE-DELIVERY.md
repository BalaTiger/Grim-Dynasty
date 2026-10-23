# M1 offline delivery — 2026-10-22

Status: **delivered and accepted on this Win11 machine**. The result is a usable independent offline CLI for resource/quest inspection and a bounded inventory/task/money replay. This closes the agreed fixed-sample M1; it does not close M2 or provide a playable reverse-engineered game.

## Run now

From the repository root:

```powershell
.\artifacts\m1-offline-20261022-03\gd-offline.exe replay inventory-roundtrip
.\artifacts\m1-offline-20261022-03\gd-offline.exe capabilities
```

The accepted package is [artifacts/m1-offline-20261022-03](../artifacts/m1-offline-20261022-03). Its [README](../artifacts/m1-offline-20261022-03/README.md) gives all commands, supported domains, JSON schema, limits and exit codes. It is 36 files / 223326 bytes including validation evidence. The executable is 126976 bytes and uses the installed .NET Framework; it does not depend on old-machine paths, prior build outputs, original game DLLs or an installation step.

## Observable acceptance

- `inspect-record` reads the original ARZ v3 sample: QuestItem record, health 125, speed 1.5, label, enabled flag, and negative-zero bits `0x80000000`. Normalized record lookup selects the same payload.
- `inspect-quest` reads standalone QST2 v9 or the same member from ARC v3: quest UID 3, two named tasks, HasItem conditions, money rewards 75 and 40. The decoded member hash equals the standalone QST hash.
- `replay inventory-roundtrip` executes the existing own modules, not a canned success response: money **50 → 125 → 165**, rewards **[1,2]**, final inventory **[100]**, and retained object identity. Duplicate add keeps the single inventory entry while preserving its counter side effect. Completed-task callbacks detach and later inventory events do not repeat the rewards in this fixture.
- JSON replay gives the same final state as the named scenario. Unknown `grant-xp` stops at step index 4 with money 125 and the completed prefix intact. Actual unsupported destruction retains the object/state.
- Missing/truncated input, unsupported version, invalid scenario, unknown command and UNC input are distinguished. An unrelated working directory, a path with spaces and a Chinese filename pass.

[acceptance.json](../artifacts/m1-offline-20261022-03/acceptance.json) records **19 CLI cases / 69 checks Passed**, completed `2026-10-22T03:46:06.3319528Z`. Environment: Windows build 26200, CLR 4.0.30319.42000, PowerShell 5.1.26100.9711. [Raw outputs](../artifacts/m1-offline-20261022-03/validation) and fixture hash checks support the result.

## Build, fixes and cost boundary

The new [build script](../tools/build_m1_offline.ps1) compiles an explicit **29-source** closure (25 existing files, 4 new CLI files) into one executable. The [acceptance script](../tools/test_m1_offline.ps1) runs only the M1 fixed cases. Both were reviewed before execution, and each of the three executions used the formal `require_escalated` mechanism. No permissions were changed and no execution bypass was used.

All three source builds succeeded with empty compiler diagnostics. Attempt `-01` stopped when PowerShell 5.1 corrupted localized exception text while capturing native output. ASCII JSON Unicode escaping fixed that transport issue. Attempt `-02` then exposed `InvalidDataException` being misclassified as InternalError; explicit input-error handling fixed it. Attempt `-03` passed all acceptance. Earlier attempt outputs remain labelled incomplete; they are not deliverables. Repeated M1 cases were due to these concrete fixes, not new native-function investigation.

No old standalone suite was rerun. The current integration cases exercise the selected existing resource, quest, condition, task, event, inventory and money paths; they are not a fresh full-project regression. The UCRT math provider and previous uncompiled two-field/Actor constructor work were excluded from this closure, not modified or declared valid. `SystemCrtMath.cs` remains SHA256 `eab09ed5a5281c2390988be890d7d0986e2d3fde0b96a7a0b3788d549991aae2`.

The calibration has produced a concrete runnable M1 within this work segment. No waiting to consume the two-day window is needed. This result does not justify a project-completion percentage or a reliable playable-version date. No task-level token or billing source was available; no cost is estimated.

## Compatibility and stop boundary

Known is bounded by the tool's supported profile; it is not a claim of native runtime compatibility. Current acceptance uses only project-authored samples. Historical official-resource decoding evidence was not rerun here. Unsupported remains explicit for other versions/types/actions, non-finite numeric views, XP/CRT evaluation, native factories/destruction and live engine state.

Resource inspection and replay share a record name and task/reward shape, but replay constructs the existing **built-in own fixture**. Arbitrary decoded archive graphs are not automatically instantiated into runtime objects. A user-visible resource-to-runtime scenario loader would be a separately bounded next milestone.

Rendering, input, scene, combat, own-save workflow, full Item/QuestItem/Actor ABI, mutable original global initialization and official runtime samples remain outside M1. No original game/Rea/Java was run, no original game or player-save/Steam-cloud file was read or changed, and no commit, push or deployment occurred.

Stop here for parent review. Do not automatically proceed to M2/M3 or resume native helper chains. If continuation is approved, prefer one headless scenario input/output acceptance over open-ended function coverage.

## Evidence and recovery

- Executable SHA256: `c4a84feb1bedbaa9b08598ff2cdd01ab0aeaa0bf7a57bbfbcf937faa38ba508b`.
- [Build manifest](../artifacts/m1-offline-20261022-03/build-manifest.json): source hashes and compiler fingerprint; SHA256 `98eb9692a44bbb0e301942e693a74751810bdf5ecbd62dde89a1de41bd15f3a5`.
- Acceptance SHA256: `6d035f6c674e5e555093406a598c735b2dcc08fa7cd2a278da2c8ca9c61067c8`.
- [Package hash manifest](../artifacts/m1-offline-20261022-03/SHA256.json): SHA256 `ee1c12e9a27dec62d4ec9b50db0c6cf01590544f9a2557ae99acd71ba4e2b8da`.
- Every packaged hash and all 29 current source hashes were rechecked after acceptance, without rerunning the tool.
- The stage checkpoint records hashes/references only; no cumulative project snapshot was copied. Existing 12 tracked build-script changes remain unchanged; HEAD remains `6dfdae419d1c92f7738eefe241937057d83134df`.
