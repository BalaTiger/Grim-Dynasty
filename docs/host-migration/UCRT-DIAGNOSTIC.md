# Win11 UCRT diagnostic, 2026-10-19

The full build remains failed at its unchanged deployment fingerprint gate. The failure does **not** establish a numerical difference: the production gate returned before any UCRT numerical function or floating-control query ran. Separate diagnostics subsequently matched the limited archived values and all three archived finite curve output digests described below. Neither diagnostic accepted a new provider profile.

## Where the existing tests stop

`src/rewards/SystemCrtMath.cs` checks the requested locale, Windows x64, registry major version 10, and then the SHA256 of `C:\Windows\System32\ucrtbase.dll`, in that order. This host passes the first platform checks but returns `Unsupported / SystemUcrtFingerprintChanged` at the hash check. `_controlfp_s`, `_create_locale`, `_atof_l`, and `powf` are later operations and were not reached by the failed production initialization.

- `CrtMathTests.cs`: the unknown-locale assertion passes; the second assertion requiring successful provider creation fails. Its 100,000-sample loop, literal/power comparisons, parser checks and integrated quest checks did not run in that test execution.
- `ExperienceScalingTests.cs`: its first assertion requiring provider creation fails. None of its scaling assertions ran. `ExperienceScaling.Scale` itself does not call `powf`: a nonzero modifier requires the verified provider's floating-control check before the managed binary32 arithmetic. The zero-modifier branch requires no provider, but the suite initializes the provider before exercising it.
- `CrtLevelUpQuestDemo.cs`: provider initialization fails before equation compilation or quest execution.

The actual x64 provider status and the build failures are retained in [windows11-build-output.txt](windows11-build-output.txt). The historical 894 assertions and 16 scenarios remain old-host evidence. This host's current production verification remains 823 passing assertions and 15 passing scenarios, plus eight separate LZ4 self-tests.

## Separate diagnostic and results

[UcrtStandaloneDiagnostic.cs](UcrtStandaloneDiagnostic.cs) was compiled with only the unchanged test-only `src/rewards/CrtReference.cs`, the installed .NET Framework x64 C# compiler, and framework libraries. It references no production GrimDynasty assembly and calls only the existing System32 UCRT. It uses a private C locale, frees it, queries `_controlfp_s` with a zero mask, and never sets the control word or global locale. Results are in [ucrt-standalone-diagnostic.json](ucrt-standalone-diagnostic.json); source hashes, compilation and execution status are in [ucrt-diagnostic-build-output.txt](ucrt-diagnostic-build-output.txt).

| Evidence | Current result | What it establishes |
|---|---|---|
| Six archived literal checks and four power edge checks | 10/10 match | Seven exact binary32 comparisons and three archived value/classification checks on those named inputs |
| Archived nontrivial `powf` sample | Exact match | Inputs `1134153466`, `1080358339` produce output bits `1312670141` on both hosts |
| Archived cast-double result for the same inputs | Exact match | `(float)Math.Pow` output bits remain `1312670142` for that sample |
| Same seed 1234567, 100,000 current-system samples | Seven `powf` versus cast-double differences, maximum 1 ULP; same first differing inputs/results | The available historical aggregate and first sample match; this is not a full cross-host output comparison |
| Floating-control queries | 524319 before and after | No change in the queried control word; this is not a claim about errno or floating exception status flags |
| System DLL SHA256 before and after | Identical | The system DLL was not modified |

The six literals are `1.16`, `1.0000000596046447753906250000000001`, `1.000000059604645`, `16777217`, `.`, and `1..2`. The power cases are `(0,0)`, `(-0,3)`, `(-1,0.5)`, and `(0,-1)`. Where the old assertion specified only NaN or zero equality, the diagnostic does not invent a NaN-payload or signed-zero equivalence requirement.

Current UCRT version: `10.0.26100.9711 (WinBuild.160101.0800)`.

- Old accepted SHA256: `3c60056371f82e4744185b6f2fa0c69042b1e78804685944132974dd13f3b6d9`.
- Current before/after SHA256: `dedb7611e1abc1ff3852dcac3d4c93a048d729e25b061973b9969f22698f14ca`.
- Newly recorded current-corpus input-bits SHA256: `493b1b1818e4487330fa2cd19c1746061a30a3039a1f7bbebde4814a2697d36b`.
- Newly recorded current-corpus `powf` output-bits SHA256: `9733cce3c6b353e26c2e66332cd4b3ae72f0b18bb1a6ce080686720e447fa9a0`.

The new digests serialize little-endian binary32 values in sample order; each input is x then y. There is no corresponding old-host full-corpus digest in the inspected evidence, so neither new digest is a cross-version equality proof. The JSON retains all seven current differing pairs for later comparison. A change in aggregate `powf` versus `Math.Pow` statistics would not, by itself, identify which implementation changed.

## Additional finite curve comparison

[acceptance-crt.json](../rewards/acceptance-crt.json) records three old curve comparisons: levels 0 through 85 (86 values) and two copies of levels 0 through 100 (101 values each), for 288 total comparisons. Their binary32 output digests are `3fed605ea9f18497b0988113d04ae1b64209be306e1dcbe907501ac7eefe8a91` and `85ac7a9d418a8ddd7e37399733039b761437b06b286efc8b53cd8c23bd9daf86`. The recorded expression SHA256 is `fca2d52130c957f3c3cf2de30e7d17bbfe31422d59f7e887c1d3e1e60f0ab099`.

The first standalone diagnostic above did not read game resources. A subsequently authorized supplemental probe, [UcrtCurveDiagnostic.cs](UcrtCurveDiagnostic.cs), read exactly these three installed ARZ files under `D:\Steam\steamapps\common\Grim Dawn`: `database\database.arz`, `gdx1\database\GDX1.arz`, and `gdx2\database\GDX2.arz`. It decoded only `records/creatures/pc/playerlevels.dbr` using the already compiled own `GrimDynasty.Resources.dll`. It validated all three record-body hashes, the expression hash, and maximum levels against the old acceptance record **before numerical calls**. It then used the unchanged `CrtReference.OfficialShape` to calculate each complete finite sequence with the current system UCRT. It did not link `Rewards.dll` or run production parser, scaling or quest integration code.

Results are in [ucrt-curve-diagnostic.json](ucrt-curve-diagnostic.json); build/source hashes and execution details are in [ucrt-curve-build-output.txt](ucrt-curve-build-output.txt). All three complete output digests match their archived values:

| Archive | Inclusive levels / values | Current and archived output SHA256 |
|---|---|---|
| `database/database.arz` | 0–85 / 86 | `3fed605ea9f18497b0988113d04ae1b64209be306e1dcbe907501ac7eefe8a91` |
| `gdx1/database/GDX1.arz` | 0–100 / 101 | `85ac7a9d418a8ddd7e37399733039b761437b06b286efc8b53cd8c23bd9daf86` |
| `gdx2/database/GDX2.arz` | 0–100 / 101 | `85ac7a9d418a8ddd7e37399733039b761437b06b286efc8b53cd8c23bd9daf86` |

The corresponding record-body hashes matched exactly:

- Base: `2f5b3f3768fbff986edfab9558e38414ddfed010889a6591c20082094c17ed37`.
- GDX1: `7a24126cfb8c691cd464d788060111125979372e07b002ba9d0e884db4b27d13`.
- GDX2: `e6418d7531e3bd7de9acb616fd5e2d515ec46a6004293ac6b048e491ab22155f`.

All three expressions match the shared expression hash above. Each entire ARZ also matches its archived SHA256, and every ARZ before/after hash is identical; the JSON retains all full-file hashes. The UCRT before/after SHA256 is unchanged, and the control word remains 524319. The JSON additionally retains the five parsed coefficient tokens/bits and all current finite output bit patterns for reproducibility.

This provides a direct archived-digest comparison for 86+101+101 = **288 outputs across three records**, including overlapping level ranges. It is stronger evidence than matching the 100,000-sample aggregate statistics, whose complete old output digest remains unavailable. The 288 comparisons are not 288 distinct levels or a proof for all `powf` inputs.

No numerical regression was observed for the named archived exact values or these three finite curve output sequences. There is insufficient evidence to claim cross-version equivalence for the full old random sample corpus, all expressions, scaling integration, or original game behavior. Across the two diagnostics, only the supplemental curve probe read the three expressly authorized ARZ files; neither executed the original game, read or loaded an original installation DLL, accessed GDX3 or other installation resources, or touched player saves or Steam cloud data. Production source, its accepted fingerprint and its test gates remain unchanged; no successful full-build manifest or new accepted configuration was written.
