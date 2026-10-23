# Rea MCP entry diagnosis — 2026-10-19

No configuration, project trust, global environment or running app was changed during this follow-up.

## Observed state

- The installed npm CLI reports `codex-cli 0.140.0`.
- The shell has no `CODEX_HOME` environment key. Its CLI executable is reached through `C:\Users\zhuzi\AppData\Roaming\npm\codex.ps1` and `@openai/codex/bin/codex.js`.
- Both checked user configurations (`C:\Users\zhuzi\.codex\config.toml` and `E:\Codex\codex-home\config.toml`) lack a Rea table. No trust record applicable to this project or its ancestors was found in those files. The project `.codex/config.toml` contains the Rea table.
- Default `codex mcp get rea --json` returned `No MCP server named 'rea' found` in the earlier check.
- An exact search of the current task's available tool names found no Rea tool/server prefix and no native exposure of the three selected Rea tools.

The [official MCP documentation](https://learn.chatgpt.com/docs/extend/mcp?surface=cli) supports user and trusted-project configuration. [Official environment documentation](https://learn.chatgpt.com/docs/config-file/environment-variables) describes the default home and `CODEX_HOME`. These do not establish which config root the already-running desktop task used, nor prove that `mcp get` is user-config-only. The exact cause of default discovery is unresolved; absence of a trust record is evidence, not a sufficient proof of the sole cause.

## Supported temporary CLI entry, read-only verified

The following temporary override followed by `mcp get` returned exit 0 and the expected stdio command, arguments, working directory and three-tool allowlist. `get` did not start Rea.

```powershell
codex -c "mcp_servers.rea={command='D:\NodeJS\node.exe',args=['C:\Users\zhuzi\Documents\Grim-Dynasty\tools\static_analysis\rea_project_server.mjs'],cwd='C:\Users\zhuzi\Documents\Grim-Dynasty',enabled_tools=['binary_session','open_binary','close_binary'],default_tools_approval_mode='prompt'}" mcp get rea --json
```

The output schema did not include `default_tools_approval_mode`. This proves the temporary table entry and allowlist parse; it does not independently prove runtime approval-mode enforcement. The existing project file specifies `prompt`, and no auto-approval or trust change was applied.

The CLI emitted a warning that it could not create temporary PATH aliases under the user `.codex\tmp` directory because sandbox access was denied. It continued and completed the read-only command successfully. No escalation or workaround was used for that ancillary write attempt.

## What is callable now

The two earlier smoke tests launched bounded Rea stdio subprocesses, initialized MCP, listed tools, opened and inspected the verified Engine/Game DLL identities, closed sessions and exited 0. Their summaries remain in `rea-smoke/summary.json` and `rea-project-smoke/summary.json`.

There is **no live persistent Rea session from those tests**, and Rea is **not present in this task's native tool catalog**. The local wrapper is proven executable and can be used by a newly authorized bounded stdio/CLI invocation. The existing smoke runner deliberately refuses to overwrite its evidence directories, so rerunning the identical command is not a persistent service interface.

No restart is needed to invoke a fresh subprocess through the verified local launcher. Automatically exposing native Rea tools in this already-running task has not been demonstrated, and this diagnosis does not authorize a new task, application restart, global registration or trust expansion.
