# Follow-up diagnosis — 2026-10-19

## UCRT: deployment identity rejection versus numerical behavior

The production failure occurs at the SHA256 gate in `SystemCrtMath.Create`, before floating-control, locale, `powf` or numeric conversion calls. It is not a numerical mismatch observed by the failed production suite.

The independent Win11 diagnostic used the unchanged test-only `CrtReference` and existing System32 UCRT. Ten archived literal/edge constraints matched (seven exact bit values and three archived classification/value requirements); the separately saved nontrivial powf witness also matched exactly. The current 100,000-sample powf-versus-cast-double experiment reproduced seven differences and a maximum of one ULP. These are differences between two numerical implementations on the current host, not seven Win10-versus-Win11 regressions. Old full-corpus output hashes were not preserved, so matching summary metrics cannot establish full-corpus cross-version equality.

A second diagnostic read only the three authorized ARZ archives and the specific `records/creatures/pc/playerlevels.dbr` record from each. Archive, record and expression identities match the archived evidence. The complete output digests for 86 + 101 + 101 curve samples match the old digests. These ranges overlap; this is 288 output comparisons, not 288 distinct levels. It executes only the independent fixed-shape reference, not production CrtEquation/ExperienceScaling or quest integration.

Floating control and the system DLL hash remained unchanged. All three ARZ files remained unchanged. The accepted production hash, all acceptance gates and the failed full-build status remain unchanged.

Evidence: [diagnostic explanation](UCRT-DIAGNOSTIC.md), [standalone numerical results](ucrt-standalone-diagnostic.json), [curve results](ucrt-curve-diagnostic.json), [curve execution log](ucrt-curve-build-output.txt).

## MCP entry and actual availability

`codex-cli 0.140.0` accepts a temporary `-c mcp_servers.rea={...}` override for `mcp get rea --json`, returning the expected stdio launcher and three-tool allowlist. This read-only lookup did not launch Rea or change trust/configuration. Default project discovery remains unresolved, and the current task's native tool catalog has no Rea tools.

The earlier smoke sessions are closed and both subprocesses exited 0. The proven local wrapper can support a fresh bounded stdio invocation subject to the applicable approval boundary; there is no currently running persistent Rea connection. No app/OS restart or trust expansion was performed. See [MCP entry diagnosis](MCP-ENTRY-DIAGNOSIS.md).

## Concrete pending approval

The fixed backend plan is Ghidra 12.1.4 PUBLIC plus Eclipse Temurin JDK 21.0.12.1+1, Windows x64 HotSpot. Official ZIP URLs and SHA256 values were checked. Installation stays under `.tooling/ghidra` and `.tooling/jdk`; downloads under `.tooling/downloads`; Ghidra runtime goes under `analysis-runtime/rea` because its project paths reject dot-prefixed components.

The proposed approval includes downloading and verifying those two archives, safe local extraction, Java/Javac and Rea provider checks, backing up and updating the project launcher/configuration for fixed backend paths and a nine-tool static allowlist with prompt approval retained, then bounded object-factory/registration queries against the already-hashed Engine/Game DLLs. No global PATH/JAVA_HOME, project trust, system security policy, application restart or OS reboot is required. New MCP/headless subprocesses are part of the approved execution scope. Internal private temporary-file DACL/Job Object management is documented separately from system security configuration changes.

Exact paths, sources, hashes, settings, limits and first queries: [NATIVE-BACKEND-APPROVAL-PLAN.md](NATIVE-BACKEND-APPROVAL-PLAN.md).

Status: the additional Ghidra/JDK download, installation and execution actions have **not been attempted** and await explicit authorization. There is no rejected or uncertain backend installation outcome. No player save, Steam cloud/userdata, original game execution, DRM bypass, commit or push was involved.
