# Navigation core: bounded investigation and supported implementation

This is a continuation of the camera contract closure, not playable-client
acceptance. Raw input and real navigation data remain separate prerequisites.

## Fixed native evidence

`r1-navigation-core/` contains actual Rea `procedure_pseudo_code`,
`procedure_assembly` and `procedure_callees` responses for Engine RVA2610F0,
261CB0 and262480. The fixed14-call batch passed2026-10-22
15:59:05-16:00:33UTC, including5 lifecycle calls. Summary SHA256:
`86ce524507630b0e63bef507b76de58d090a1308592c10eec0246705ede59922`.
Normal session/process/Job/sampler cleanup and unchanged Engine SHA256
`4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`
are recorded. The original was not executed. Earlier queries were not repeated.

2610F0 clears the required reference output, calls261950 with a128-entry
candidate buffer, and inspects candidates using260520. A negative enumeration
status maps to80000008. A nonnegative result with signed count<=0 returns
40000000 with reference0 and leaves the optional point untouched. Candidate
point updates precede the final reference write. The helper return from260520
is not checked. Its geometry, enumeration and data initialization are still
unknown. COMISS branches, including unordered behavior, must be implemented
from assembly rather than the decompiler's plain comparisons. The8-byte
follow-up below establishes34BDCC and34B938 only. Constant34C410 remains
uninspected, and the nearest-search helper contract is not implemented.

261CB0 receives uint32 references in EDX/R8D, not the float values suggested
by pseudocode. It clears the count before zero-reference checks and invokes
2688C0 twice using the same captured mesh pointer. Valid equal references
write one reference and count1, returning40000000, without touching the node
pool or heap. Unequal-reference search uses query+58/+60, mesh topology and
filter costs. Its status includes detail bits10(buffer limit),20(node shortage)
and40(partial path); success does not establish destination reachability.

262480 clears the count before reading the first corridor reference. It calls
260AD0 on both endpoint polygons, then2623B0 to append the first point. A
single-polygon corridor bypasses262D30, the funnel loop and its TLS state,
but still needs endpoint projection and25F770 endpoint equality. Output
coordinate writes and flag/count writes have observable order and partial
failure states.

The apparent caller-stack alias is not two active output pointers. The
incoming arg8 value is never read:26261E overwrites its low byte before use
as an internal flag. Arg9 is the count pointer captured in R13. The apparent
arg11 is likewise written before use. Do not turn decompiler scratch slots
into an invented output-reference array.

## Current implementation scope

The bounded helper set2688C0,260AD0,2623B0,25F770 completed17/17 calls at
16:06:45-16:08:13UTC, again with normal cleanup and unchanged Engine hash.
Its summary SHA256 is
`4e56505a90a6493e94452070770baeed213ba818b6b81ab5fa49b2213cb5eb0b`.
The remaining direct geometry helper26D730 completed8/8 calls at
16:15:12-16:16:39UTC. Its body has no further direct callees. Summary SHA256:
`e3e3933bdeb13d693c64c39301f10a2353225122b45c8c49966131460b639b77`.
All three batches (39 calls, including15 lifecycle calls) contain8 newly
inspected bodies. Each includes real Rea pseudocode, assembly and callees;
none is a repeated query from an earlier stage. All cleanup was normal.

`NavigationCoreContracts.cs` now implements reference validity, equal-reference
corridor production,260AD0 endpoint clamp,26D730 polygon geometry,25F770
equality,2623B0 append and the single-polygon262480 output path. These are
independent C# implementations of the observed supported branches, using
explicitly supplied own mesh/reference data. The default clamp uses actual
recovered geometry, not a fixture endpoint passthrough.

26D730 uses XZ crossings and squared edge distances. An inside point retains
its original Y; outside points are projected to an edge with XYZ interpolation.
Strict crossing makes opposite polygon boundaries behave differently, and a
NaN interpolation parameter becomes positive zero. The implementation preserves
instruction-ordered float32 operations, previous-edge indices, duplicate-point
flag replacement, late reads and partial output. Unknown payloads can be copied
without inventing values; required unknown fields stop explicitly. Zero-reference
clamp captures but does not consume an unknown mesh pointer, matching the
observed unused MOV. Deferred buffer views check lifetime at actual reads.

Own geometry supports the observed six-vertex scratch domain. Arbitrary byte
overlap, concurrent mutation/lifetime, native ABI/EH and nondefault MXCSR are
outside scope. Same-typed shared cells are supported where represented.
Nearest-candidate production, multipolygon search/funnel, actual map loading,
query initialization and physical mouse-to-world coordinates remain unclosed.
Supplying references or endpoint observations in an owned fixture is not a
substitute for those production paths.

## Data and initialization boundary

Observed reads require query+0 to identify a mesh. Mesh+58/+5C/+60 are the
reference's salt/tile/polygon bit counts; shifts follow the instruction's
masked counts. Mesh+30 bounds the tile index, +50 locates0x68-byte tile slots.
A tile's +0 salt must match, +8 header must be nonnull, and header+18 must
bound the polygon index. Validity only produces AL; upper return bits are
not a boolean contract.

Endpoint geometry additionally reads tile+10 polygon records (stride20hex),
tile+18 vertices (stride12bytes), polygon+1E vertex count, and uint16 vertex
indices starting+4. Pool+58, heap+60 and filter costs are needed only by the
unequal-reference corridor branch and are not initialized by this work.

No constructor/writer that publishes the caller's query+10, loads navigation
tiles or sets the filter has yet been identified. A self-owned typed view can
be initialized explicitly for tests, but neither a default constructor nor a
zero-filled block establishes valid original navigation state. Runtime data
availability remains a production blocker distinct from helper mathematics.

## Constants and reproducible validation

`r1-navigation-constants/inspection.json` records a bounded original read at
16:16:17UTC: only8 payload bytes, Engine34BDCC=`7f7fffff` (max finite float)
and34B938=`31800000` (equality threshold source). Before/after Engine hashes
match. Its SHA256 is
`16c88b4a9126a4752e2575e8dc02c4b87f9f25655660ff7289c69a4132fc754b`.
The1.0 value at34BB44 is reused from the previously saved
`docs/object-lifecycle/offline-actor-remaining-dependencies/inspection.json`
(SHA256 `6639fbb457720057edb6ea37834e6f5aa2534593b26020fda7d83044b73325c5`).
Explicit own initialization uses these observed values; disk values do not
prove loaded mutable state or the native TLS/CRT cold path.

The formally approved isolated build ran2026-10-22 16:27:55-16:27:56UTC:
one build attempt, zero compile/test failures, no compiler warnings. It compiled
only the two new sources against the frozen Actions DLL. Result: **34 scenarios,
275 assertions passed**. Existing suites, including the28 camera scenarios,
were preserved and not rerun. The first24 scenarios test production leaves and
explicit Mock wrapper observations; the final10 use production geometry and
default boundaries with self-owned mesh data. None is original-game execution.

- Result: `.tooling/r1-navigation-core-validation/result.json` and `tests.log`.
- Library: `.tooling/r1-navigation-core-validation/GrimDynasty.NavigationCore.dll`,
  SHA256 `2dbb6cf7e2d6361a31e7f19c23c96e13bef057c9cf5c2d24bf952c8ac504bca5`.
- Test executable: `.tooling/r1-navigation-core-validation/NavigationCoreTests.exe`.
- Executed recipe: `tools/static_analysis/build_navigation_core_contracts.ps1`.
  It refuses to overwrite the retained validation directory. Future rebuilds
  need a reviewed fresh output destination and the normal execution approval.

From the repository, the retained test artifact can be invoked (through the
normal required execution approval) with:

```powershell
& .\.tooling\r1-navigation-core-validation\NavigationCoreTests.exe
```

This is a component test, not a playable program. The next necessary producer
gap is261950/260520 nearest-reference/point generation and the confirmed query,
tile and filter initialization source. Unequal-reference search/funnel, real
mouse target production and scene/render remain needed for R1 acceptance.
No further query was started in this stage. Checkpoint and manifest are extended
in place; no evidence-tree copies, original writes, save access or commit/push.
