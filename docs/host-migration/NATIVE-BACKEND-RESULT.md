# Native static backend result — 2026-10-19

Later continuation: [REGISTRATION-MODEL-RESULT.md](../object-lifecycle/REGISTRATION-MODEL-RESULT.md) records the additional container evidence, bounded registration model and current host-diagnostic status. The validation counts and missing helper evidence below describe this earlier checkpoint.

The user approved the fixed installation and self-test plan in this turn. Ghidra 12.1.4 PUBLIC and Temurin JDK 21.0.12.1+1 are now installed only inside this project. Rea 6.5.0 successfully used Ghidra for the bounded Engine object-creation evidence batch. This does not mean the current task has acquired native Rea tools.

## Installation and verification

| Component | Project-relative location | Verification |
| --- | --- | --- |
| Rea 6.5.0 | `.tooling/rea/node_modules/rea-agents` | Pinned package identity, MCP identity and actual input/output tool schemas checked |
| Ghidra 12.1.4 PUBLIC | `.tooling/ghidra/ghidra_12.1.4_PUBLIC` | Official ZIP SHA256 `ddac49f903da9d5bac833e5cc79395098b9c33cfd3279be5f31bd00387d2d4db`, 569649598 bytes |
| Temurin 21.0.12.1+1 | `.tooling/jdk/jdk-21.0.12.1+1` | Official ZIP SHA256 `f9d6e191ab098c0d416e7d588a24420a8621cd2f4720dab2459b8b7b2d2d8b4e`, 205073461 bytes |
| Runtime/preferences | `analysis-runtime/rea` | Host preferences/temp and native session directories remain project-local |

Sources and package verification are retained in [download-verification.json](native-backend/download-verification.json); archive-entry validation and extraction are recorded in [extract-verification.json](native-backend/extract-verification.json). No installer, additional SDK, global PATH change or system Java registration was used.

The first Ghidra download was stopped after directory metadata repeatedly showed zero bytes. On closing the handle, the partial file contained 300871040 bytes. This was an incomplete download, not a failed content-hash match. The same fixed URL was resumed; the completed file matched its expected full SHA256 and size before extraction. The installer now uses bounded connection/inactivity timeouts. Nothing was executed from the incomplete archive.

`java -version`, `javac -version`, and `rea doctor --provider ghidra --json` all exited 0. Doctor reported `healthy: true`, Ghidra available at version 12.1.4, and all nine Ghidra checks passed, including full JDK, native decompiler and Windows native authority. See [selftest.json](native-backend/selftest.json). Other uninstalled providers and missing optional skill registration are not an instruction to install anything else.

## Project configuration

Original launcher and project configuration were copied to `.tooling/backups/native-backend` with a SHA256 manifest before editing. The project wrapper now uses the fixed backend, 512M Ghidra heap setting, and project-local host preference/temp directories. It removes inherited backend overrides and credentials rather than changing the host environment.

`.codex/config.toml` and [rea-project-config.toml](rea-project-config.toml) match SHA256 `7aeed98559ed397816f78ae53356cf65477c6479aba1ece58b5dcb50b83c09df`. They retain `default_tools_approval_mode = 'prompt'`, startup timeout 30 seconds, tool timeout 600 seconds, and these nine tools:

`binary_session`, `open_binary`, `close_binary`, `procedure_pseudo_code`, `procedure_assembly`, `procedure_info`, `procedure_callees`, `xrefs`, `address_name`.

The environment and tool allowlist are not an OS filesystem sandbox. The explicit authorization and the fixed query runner additionally constrain this batch to the verified Engine DLL and listed procedures. Persistent trust/global permission settings were not changed.

## Actual static execution

[The batch summary](../object-lifecycle/rea-native-registration/summary.json) records:

- 18 of 18 calls passed: 13 analysis calls and five lifecycle/status calls.
- Actual Rea 6.5.0 identity and Ghidra binding verified before analysis.
- CreatePartial and FinishPartial assembly/direct callees, plus pseudocode/assembly/direct callees for `0x180274EC0`, `0x18017FCA0`, and `0x1801356F0` saved.
- Ghidra default auto-analysis completed before results were served. Decompiled pseudocode is analysis output, not original source; the recorded PDB load state is unknown.
- `close_binary` acknowledged closure, final session verified no active target/provider/run, and the Rea child exited 0 without forced termination.
- The temporary `rea-ghidra-*` project directory was removed by normal close. Project-local host/JDK temporary directories remain; no unrelated cleanup was performed.

Engine SHA256 before/after is `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`. All 13 previously inventoried original executable/DLL hashes also remain unchanged: [game-hashes-after.json](native-backend/game-hashes-after.json).

## Current task tool loading

This delegated execution task still exposes no Rea native tool or MCP-refresh tool. The source conversation's separate tool catalog has not been queried here. The successful analysis above used a finite, approved stdio client; it closed afterwards. It is not a background Windows service or a persistent client connection.

The local 0.140.0 App Server schema supports `config/mcpServer/reload`, but it cannot filter by server or task and may queue refreshes for other loaded tasks. No verified connection to this task's existing host is exposed. No host reload, app restart, trust expansion, global registration or new task was attempted. [MCP-PERSISTENCE.md](MCP-PERSISTENCE.md) records exact protocol shapes and the remaining client-side steps; a successful reload response alone would not prove current-tool injection.

## Independently compiled next primitive

The closed control flow at Engine `0x180274EC0` is now implemented in [ClassFactoryDispatch.cs](../../src/task_items/ClassFactoryDispatch.cs) and included by the existing TaskItems build. It uses immutable observed ClassInfo snapshots and supplied **own** callbacks. It does not call native addresses or connect to InventoryAdd.

Matching is byte-exact and NUL-terminated, including empty and high-byte names. A matching node immediately returns its callback result, even null; only a nonmatching node walks its signed-count child array in order. Parent traversal continues after a child's known-null result, and repeated references invoke shared nodes repeatedly. Unknown accessed metadata/callbacks remain `Unsupported`; unused branches need not be known. Earlier callback side effects are retained and reported. Snapshot mutation, native constructor ABI and native exception propagation are outside this module's equivalence claim. Callback exceptions become `Unsupported`, not a fabricated known-null result. The depth/visit budgets are explicit coverage limits, not claimed native restrictions.

Validation on the newly compiled own DLL:

- 40 new class-dispatch assertions passed.
- 39 existing item-condition assertions, 11 item-auto assertions and the existing ItemQuestDemo passed.
- 20 existing Object ID assertions passed against the new TaskItems DLL.
- Assembly references contain only framework and own GrimDynasty modules; no Engine, Game or Steam dependency.

See [class-dispatch-build-output.txt](native-backend/class-dispatch-build-output.txt), [object-id-regression-output.txt](native-backend/object-id-regression-output.txt) and [task-items-dependencies.json](native-backend/task-items-dependencies.json). The first optional metadata inspection used `ReflectionOnlyLoadFrom`, which is unavailable in this PowerShell runtime; the tests had already passed. The follow-up used `LoadFile` on the **own** TaskItems assembly and successfully inspected references. No original DLL was loaded for execution.

These are targeted validation results, not a new full offline-package pass. The broader UCRT failure was not bypassed or rerun unnecessarily.

## Remaining behavioral boundaries

The new factory helper evidence closes only the class-tree matching and constructor-callback dispatch control flow. Actual concrete constructors, `LoadFile`, name/key derivation, record `Read`, and the full successful registration path are not thereby implemented.

For the registration indexes, duplicate keys preserve the old payload; FinishPartial ignores the insertion flags and still proceeds. The `+0x128` helper deletes a selected range, but the mapping from input ID to that range remains unknown. New-key node allocation/maintenance and range-selection/deletion helpers still need evidence. Do not replace these with dictionary assignment or claim rollback.

The existing full-build UCRT fingerprint blocker remains unchanged. The separate numeric diagnostics are not full acceptance of the new CRT, and the previous machine's 894 assertions are not reported as newly passing here.

No game target was launched or loaded for execution. No player saves, Steam userdata/cloud, DRM/Steam bypass, system-security change, restart, commit or push was involved. All requested action-level approvals were allowed; no action is awaiting approval and no automatic approval review rejection remains.
