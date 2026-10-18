# Shutdown / migration handoff

Safe stop reached. No active Rea or build. Last validation: 894 fixed assertions, 16 own scenarios, 10 own modules; new runtime ID scene passed. See OBJECT-ID-COMPLETION.md. No original game execution or saves touched. No commit/push/install/config changes performed.

Baseline HEAD: da38ff671e1af25346a976e92b5f7ae93aab733f
Local origin/main cached ref: da38ff671e1af25346a976e92b5f7ae93aab733f
These match, but no remote network refresh was performed. Latest Object ID changes are UNCOMMITTED and are NOT represented by that commit. Clone alone loses this phase. Earlier baseline exists in local origin/main; remote freshness not independently verified.

Transfer Grim-Dynasty-object-id-handoff.zip alongside clone. It contains complete modified file versions, new files, tracked binary diff, stage evidence, and file SHA256 list. Verify migration-files.json; apply working versions to a clean checkout at the exact baseline (preserve any new-device work first). Archive project/ paths map to repo root. workspace/ holds static Rea runners/plans for reference; change all absolute paths before use. No original game resources or tool installations are bundled. Build outputs are reproducible and not required for transfer; package manifest included as evidence. Keep entire existing local repo as backup if possible.

Pending: full native object factory/registration/Read, generation and real saving remain Unsupported. This stage dependency list is retained in DEPENDENCIES.md; final detailed explanatory append was not made before shutdown, see separate completion document. No migration connection to another device was established; wait for user command there.

Tools observed: Rea rea-agents 6.5.0 (existing offline npm cache), Node v24.21.0, Ghidra 12.1.4 PUBLIC, JDK 21.0.12.1+1, .NET Framework64 v4.0.30319 csc (C#5), Python 3.14.0, Git 2.53.0.windows.2. Own builds use existing System32 UCRT and strict fingerprint; a different host must validate or explicitly handle portability before claiming equivalent tests. No installer run. Rea reference command: npx --offline --no rea-agents@6.5.0 mcp; cache availability required. setup/install/persistent MCP configuration requires separate authorization. Existing .tooling/installed-paths.json is host-specific. Build scripts contain absolute D: paths: adapt carefully on destination. Do not copy game DLLs/assets; reverify destination Steam manifest, version and x64 hashes. Do not access saves.
