# M2 delivered — 2026-10-22

The headless runtime slice passes on this Win11 machine: new own character, accept task, deliver the carried item, receive money, save, exit and reopen, continue the second task, and preserve one-time rewards. The existing M1 package and its source hashes remain unchanged.

Accepted package: [artifacts/m2-session-20261022-01](../artifacts/m2-session-20261022-01). From the repository root:

```powershell
.\artifacts\m2-session-20261022-01\gd-session.exe new player "Border recruit"
.\artifacts\m2-session-20261022-01\gd-session.exe act player accept-delivery
.\artifacts\m2-session-20261022-01\gd-session.exe act player deliver
.\artifacts\m2-session-20261022-01\gd-session.exe show player
.\artifacts\m2-session-20261022-01\gd-session.exe act player accept-return
.\artifacts\m2-session-20261022-01\gd-session.exe act player return-item
```

Each command is a separate process. Own saves are under that package's `runtime-data/saves`. The pre-existing `hero` slot there was created only by acceptance; use `player` for personal experimentation. Money is 50 → 125 → 165; reward UIDs are [1,2]. Repeating completed actions neither restarts the task nor writes another reward. This business-level one-shot policy is explicitly project-owned; native task-start behavior was not changed.

The first build and acceptance succeeded: **18 fresh-process CLI cases, 53 CLI checks, 42 core checks**, completed `2026-10-22T04:02:23.3130765Z`. [Acceptance](../artifacts/m2-session-20261022-01/acceptance.json), [core log](../artifacts/m2-session-20261022-01/core-tests.log), and [README](../artifacts/m2-session-20261022-01/README.md) record the evidence and commands. Both compiler logs are empty. No previous independent suites were rerun.

The own format uses a strict journal, deterministic replay and a semantic state hash. It validates before committing loaded state, rejects duplicate JSON keys, malformed UTF8, unknown versions/actions, tampered checksums and inconsistent state summaries. Atomic replacement retains a backup; only explicit `recover` restores it. Exclusive slot locking, revision and unique commit identity reject stale writers, including when recovery recreates the same revision and business state. Returned views and independent sessions do not share mutable state. Task/inventory references within a reconstructed session retain the required ownership relationships.

Builds and tests used the formal approved execution mechanism, the existing compiler, project-only files and no new tools. Player saves/Steam cloud/original binaries were not accessed. No security settings, commits, pushes or deployments changed. The accepted EXE SHA256 is `e6b9388dae81d1cb8e492fab7f4f1c3d8cda930563ec48079c701f98332e3ebe`; acceptance SHA256 is `f9779ceb1033a17be28b7a9c80dbe1003ebdd9dd842826f4fd5cd05953170e10`.

Scope remains the existing own two-task inventory fixture and verified task/inventory/money modules. It is not arbitrary official quest execution, native Actor/Item construction, original save compatibility, combat, XP, trade or equipment simulation.

The user's later authorization supersedes M1's historical stop note: proceed through M3 without waiting at stage boundaries. Next is the first visible M3 node: an actual controllable Liaoxi scene using a lightweight C#/.NET presentation layer. Existing evidence does not yet establish combat settlement, AI, shop or equipment-action contracts; these are tracked separately and must not be faked by generating kill events or guessed prices.
