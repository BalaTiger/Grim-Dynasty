# Windows 11 Rea installation plan — 2026-10-19

Source authorized by user: https://github.com/morluto/rea#quick-start
Official npm: https://registry.npmjs.org/rea-agents/6.5.0
Pinned version: 6.5.0, matching the handoff. Node v24.14.0 satisfies the published engine range. Repository metadata: git+https://github.com/morluto/rea.git.
Tarball: https://registry.npmjs.org/rea-agents/-/rea-agents-6.5.0.tgz
Integrity: sha512-C6BG7LyrAdR74mSjertM6PrxJkcNZ2bfQNOJornPo9erDEp2tyPRVYPnV5Yodr+1RqewCQla68WulXlVnveD+A==

## Proposed local installation

```powershell
npm install --prefix 'C:\Users\zhuzi\Documents\Grim-Dynasty\.tooling\rea' --cache 'C:\Users\zhuzi\Documents\Grim-Dynasty\.tooling\npm-cache' --registry=https://registry.npmjs.org --ignore-scripts --omit=optional --no-audit --no-fund --save-exact rea-agents@6.5.0
```

Only Rea and its required npm runtime dependencies will be placed in the project. npm lifecycle scripts are disabled; optional Frida is omitted. No global install, PATH/registry/security changes, Ghidra/JDK/Hopper install, or automatic setup command. The install requires network access unavailable in the default sandbox; approval review must evaluate this exact operation.

## Subsequent separate actions

Inspect the downloaded launcher and setup code before execution. Use the pinned local Node entry point for a bounded MCP initialize/tools-list/static-PE/close session. Confirm no target executable is launched. Native decompilation needs a separately available provider; no extra provider is authorized for installation in this task.

Prepare a project-scoped MCP configuration with direct Node invocation and a static-only tool allowlist after examining the actual catalog. Do not silently alter global Codex configuration or apply unrestricted auto-approval. Configuration writing and running external Rea code are distinct actions and remain subject to applicable per-action approval.

Scope: project read/write, verified game installation read-only, necessary tool configuration. Never access saves or Steam cloud/userdata. No original game execution, DRM/Steam circumvention, reboot, commit or push.