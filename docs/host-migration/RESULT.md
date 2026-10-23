# Windows 11 continuation result — 2026-10-19

Latest continuation: [Binary stale lookup, cache-ID writes and effect propagation](../object-lifecycle/BINARY-STALE-RESULT.md), with 844 passing assertions, 10 combined scenarios plus the existing demo, no new backend queries or original-game reads, and an [overall progress map](../object-lifecycle/LIFECYCLE-PROGRESS.md).

This file records the initial migration checkpoint. The later user-approved Ghidra/JDK installation and successful native static queries are recorded in [NATIVE-BACKEND-RESULT.md](NATIVE-BACKEND-RESULT.md); that report supersedes the backend/configuration status below. Current-task MCP loading is detailed in [MCP-PERSISTENCE.md](MCP-PERSISTENCE.md).

## Handoff and original inputs

The later checkpoint commit is already applied: HEAD `6dfdae419d1c92f7738eefe241937057d83134df`, directly after `da38ff671e1af25346a976e92b5f7ae93aab733f`. The shutdown document's UNCOMMITTED statement describes the earlier shutdown, not the current checkout. No patch was reapplied and no archive files overwrote the working tree.

The v2 archive SHA256 is `35e9ca95efb230eee040d5ef34cb2de11ac32702ce23511fd36ec08c6511a5a1`. All 39 normalized migration entries verify; current source matches after accounting for checkout line endings. The two missing `.tooling` records are host-specific old installation paths and an old build manifest, not missing source. See [handoff details and next queries](HANDOFF-AND-NEXT-MODULE.md).

The game root is `D:\Steam\steamapps\common\Grim Dawn`, established from this host's Steam registry, library list and appmanifest, not assumed from the old host. App ID 219990, build ID 25813250. All 13 original executable/DLL sizes and SHA256 values match the prior inventory; `resources/Quests.arc` also matches. Final verification is in [verification-summary.json](verification-summary.json).

## Rea installed and exercised

The official [Rea quick start](https://github.com/morluto/rea#quick-start) and published `rea-agents@6.5.0` metadata were checked. Node v24.14.0 satisfies its engine requirement. Fixed version 6.5.0 was installed into `.tooling/rea`, with an in-project npm cache, lifecycle scripts disabled and optional dependencies omitted. npm exited 0 and added 146 packages. Frida is absent. No extra Ghidra, JDK, Hopper or other backend was installed.

The lockfile's Rea package entry matches:

```text
https://registry.npmjs.org/rea-agents/-/rea-agents-6.5.0.tgz
sha512-C6BG7LyrAdR74mSjertM6PrxJkcNZ2bfQNOJornPo9erDEp2tyPRVYPnV5Yodr+1RqewCQla68WulXlVnveD+A==
```

The authoritative integrity value is also retained in [REA-PLAN.md](REA-PLAN.md) and the installed package lockfile.

Separate permission reviews allowed the official metadata read, local installation, bounded Rea execution, project configuration write and launcher verification. No action received an automatic approval rejection. No blanket auto-approval setting was added.

Actual stdio MCP initialize/tools-list and Engine.dll/Game.dll open/session/close calls passed twice: [direct launcher](rea-smoke/summary.json) and [registered project launcher](rea-project-smoke/summary.json). The server reported 6.5.0 and a 207-tool catalog. Both child processes exited 0; sessions closed and input hashes stayed unchanged. This proves static identity inspection, not native decompilation.

Project `.codex/config.toml` exposes only `binary_session`, `open_binary`, `close_binary`, with `default_tools_approval_mode = 'prompt'`. The launcher excludes inherited credentials, implicit HOPPER_TARGET_PATH and backend overrides, and directs temporary files into `.tooling/rea-runtime`. Tool allowlisting and environment filtering are not filesystem isolation. Provider readiness can perform package-native temporary-file/process checks; no target game code was executed. No snapshot operation was requested.

**Registration discovery remains unconfirmed:** the local CLI command `codex mcp get rea --json` returned `No MCP server named 'rea' found`. The project file is present and the exact launcher works, but automatic loading by this task/app has not been demonstrated. Official [Codex documentation](https://developers.openai.com/codex/mcp) limits project configurations to trusted projects. No project trust/global configuration was changed and no app or OS was restarted. A later authorized configuration step must resolve loading; do not report the current chat's tool catalog as already hot-reloaded.

## Build migration and actual host results

Twelve current build scripts received 43 path-only substitutions, using `$PSScriptRoot` instead of `D:\MyGitRepo\Grim-Dynasty`. Historical backups and behavior source were preserved. PowerShell syntax and diff checks passed. All 12 output-boundary tests still reject an outside-project destination.

The full build was attempted and failed at the unchanged CRT gate. Independent continuation compiled all ten own DLLs and passed 823 assertions across 29 suites, fifteen scenarios and eight additional LZ4 self-tests. ObjectIdTests passed all 20 assertions; RuntimeIdInventoryDemo passed. See [complete execution log](windows11-build-output.txt).

The current System32 UCRT is version `10.0.26100.9711`, SHA256 `dedb7611e1abc1ff3852dcac3d4c93a048d729e25b061973b9969f22698f14ca`. The approved old profile requires `3c60056371f82e4744185b6f2fa0c69042b1e78804685944132974dd13f3b6d9`. `SystemCrtMath.Create` therefore returns `Unsupported / SystemUcrtFingerprintChanged`. CrtMathTests, ExperienceScalingTests and CrtLevelUpQuestDemo fail. No fingerprint was replaced, test gate skipped or successful package manifest fabricated. The old 894-assertion/16-scenario result has not been reproduced on this host.

## Remaining work

1. Resolve project MCP configuration loading without silently changing project trust or broadening permissions.
2. Supply an authorized native analysis provider. Rea reports no bound provider; Ghidra is `not_configured`. Ghidra 12.1.x and a compatible full JDK are needed for the planned native queries. Installing or running additional backend tools requires separate approval; it was not attempted here.
3. Obtain the missing class-factory, container/index and concrete Read/constructor evidence listed in HANDOFF-AND-NEXT-MODULE.md. Existing evidence only proves narrow early-return cases; a successful factory/registration implementation would currently be speculation. No behavior module was added.
4. Establish an independently validated Win11 UCRT numerical profile before changing the strict accepted fingerprint or claiming complete equivalence.

No player saves, Steam cloud/userdata, DRM/Steam bypass, original game execution, reboot, system security configuration, commit or push was involved. Original binary/resource hashes and system UCRT hash remain unchanged.
