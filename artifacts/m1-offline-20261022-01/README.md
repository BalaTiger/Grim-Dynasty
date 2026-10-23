# Grim Dynasty M1 offline tool — 0.1

An independent .NET Framework 4.x command-line tool for local resource inspection and a bounded, project-authored inventory/task/money replay. Requires Windows with .NET Framework 4.x; no installation, game DLL, Steam, Rea, Java or UCRT math provider is required. This is the M1 offline deliverable, not a playable game.

From this package directory in PowerShell:

```powershell
.\gd-offline.exe capabilities
.\gd-offline.exe inspect-record .\samples\m1-records.arz ming/items/military_ration.dbr
.\gd-offline.exe inspect-quest .\samples\m1-ration-cycle.qst
.\gd-offline.exe inspect-quest .\samples\m1-quests.arc quests/m1-ration-cycle.qst
.\gd-offline.exe replay inventory-roundtrip
.\gd-offline.exe replay .\samples\inventory-roundtrip.json
.\gd-offline.exe replay .\samples\unsupported-prefix.json
```

The first replay removes three rations, completes the first task, adds the same retained object back, completes the second task, then repeats inventory events. Money changes **50 → 125 → 165**; reward task UIDs remain **[1,2]**; final inventory contains object **100** with the same object identity. The duplicate add still increments the model's pre-deduplication counter. This verifies the covered completed-task event path, not arbitrary task restart idempotence.

The unsupported-prefix sample deliberately stops on `grant-xp` after four successful steps. It returns exit **3**, money **125**, and the preserved state/trace. No rollback or invented XP implementation occurs. `replay inventory-destroy-unsupported` exercises the existing model's unsupported destruction branch and retains the object/state.

Every invocation emits one JSON object (`schema=grim-dynasty-offline-v1`). Exit codes: **0 Known**, **3 Unsupported**, **2 InvalidInput**, **1 InternalError**. A record/quest that decodes successfully is not thereby executable or runtime compatible.

| Supported in M1 | Boundary |
|---|---|
| ARZ v3 selected record, ARC v3 selected QST member, QST2 v9 definition | No whole-install scan, object construction or Lua execution. Unknown versions/types and non-finite numeric view return Unsupported. Missing/malformed inputs return InvalidInput. |
| Local explicit files; case/slash-normalized archive lookup | UNC/device/alternate-stream/network-drive paths rejected. Inputs read-only; no default game/save paths. |
| Original inventory-cycle fixture and finite JSON action list | Only `start-delivery`, `update-delivery`, `start-return`, `update-return`, `take-all`, `add-existing`, `destroy-all`; destruction returns Unsupported. JSON cannot supply arbitrary game world state. |
| Existing item, task condition/event updates, money and reward history | No native item factory, Actor ABI, live globals, rendering/input/combat, saves, or original-save compatibility. |
| Fresh current-source build for selected modules | Whole legacy build and old standalone suites are not part of this package. XP/CRT fingerprint gate remains unchanged and its provider is not linked. |

Limits: archives 512 MiB, tables/payloads 8 MiB, 100000 entries, 250000 ARZ strings, 4096 selected ARC blocks, replay input 64 KiB/64 steps, output 4 Mi characters. Finite binary32 fields include raw `float_bits`; numeric negative zero is retained. The sample files contain only original project-authored data; no original game assets are bundled.

The resource inspection samples and replay fixture use the same record name and task/reward shape, but **replay uses the built-in own fixture**. It does not load arbitrary decoded archives into a native factory. Arbitrary resource-to-runtime integration remains outside M1.

For actual validation results see `acceptance.json` and `validation/*.json`. `build-manifest.json` records this compiler and every source hash; `SHA256.json` records package files. The manifest's machine path is provenance only; the executable has no dependency on that checkout or working directory.

To reproduce from the repository, review and run `tools/build_m1_offline.ps1 -PackageDirectory <new directory inside repository>` using the environment's required execution approval. It builds the explicit source closure and runs only this CLI's fixed acceptance. It refuses existing output directories; it changes no execution policy or permissions. Legacy compiler timestamps mean byte-identical executable output is not promised.
