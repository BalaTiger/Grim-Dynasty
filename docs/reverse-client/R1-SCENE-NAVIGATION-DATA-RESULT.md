# Real scene / navigation data investigation

2026-10-22. This stage continues the nearest-point component toward actual
scene-backed navigation. It does not establish R1 gameplay acceptance.

## Actual resource, not a synthetic mesh

The installed base archive is
`D:\Steam\steamapps\common\Grim Dawn\resources\Levels.arc` (221,754,949 bytes).
Its observed SHA256 before and after both approved reads was
`adfc68f1f5d5b388226a86f997dcff0c8510512993ede2e86d0e90a39ae65f87`.

`r1-levels-archive-index/inspection.json` and `entries.json` record the real
ARC v3 directory: only `mainmenu/mainmenu.map` (1,091,357 decoded bytes,
5 blocks) and `world001.map` (818,974,129 decoded bytes, 3,125 blocks).
The structural index read was 151 bytes, separate from streamed whole-file
hashing. No member was decoded by this index operation.

`r1-levels-first-blocks/inspection.json` records the next approved bounded
read: first blocks 0 and 5, compressed lengths 211,505 and 100,682, each
decoded to 262,144 bytes. The new JS LZ4 port passed its eight memory-only
cases. Both decoded blocks start with bytes `4d 41 50 09`. This is an
observation, not a recovered map-header contract. The main-menu prefix
contains `Levels/MainMenu/MainMenuLevel02.lvl`; this string is not an ARC
member or proof of a separate filesystem resource. Whole-member Adler32
checks were still unverified at that first-block stage.

The scoped source inventory found ARC/LZ4/ARZ/QST codecs, but no existing
MAP09/Level-format decoder to reuse. The previous 64 MiB whole-member bound
cannot accommodate the observed world member; it was not silently raised.

## Native path evidence

All listed bodies were obtained through real Rea/Ghidra
`procedure_pseudo_code`, `procedure_assembly`, and `procedure_callees` calls.
Engine SHA256 remained
`4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`.

- `r1-region-load-entry`: constructor196380 stores borrowed Region pointers
  in 24-byte entries; Update196790 coordinates readiness. IsNavDataLoaded150D20
  obtains Region+68 under its lock, then uses Level+5380/+5478 after unlock.
- `r1-region-loader-start-slot`: an exact eight-byte data read resolves the
  constructor's vtable342C70 slot0 to196DD0. The slot body requests
  BackgroundLoadLevel194550 when the current world+0A flag permits it.
- `r1-navmesh-production-entry`:10D920 takes the implementation+1A0 lock.
  Null data returns true; nonnull data performs a lookup at+138 against the
  captured+140 sentinel. This is not proof that mesh content exists.
- Level::CreatePathMesh12DFC0 is a geometry-building route. It removes old
  data, frees/clears Level+5478 and clears DWORD+5480, calls1E8DE0 with those
  output addresses, and calls AddData150440 with the Level's Region pointer
  only when+5478 is nonnull. Its normal return is unconditionally true.
  Neither this return nor the function's existence proves that ordinary
  loading must rebuild the mesh. Builder/CRT branches were not expanded.

- `r1-region-load-publication`:194440 synchronously calls194E20 and then
  PostLoadLevel1949F0 on success. AddData150440 separately captures two Level
  pointers under the Region lock. After unlock it reads the first's DWORD5480
  and the second's QWORD5478, then calls10B1E0 with the Region coordinate/name
  addresses. There is no safe null default or atomic combined snapshot.
- `r1-region-blob-loading`:194E20 connects successful normal Level loading to
  AddData when5478 is nonnull. Region+98 selects between external Level::Load
  12C6A0 and another195AF0 loading branch. Their bodies are recorded below.
  10B1E0 itself allocates a60-byte work item through109410 and
  queues it at implementation+7638; enqueue completion is not tile publication.
  The original saved-state RestoreState branch is excluded from follow-up.
- `r1-level-stream-and-navjob`: external12C6A0 and embedded195AF0 both call
  Level::Load12C770. The embedded path requests bytes using Region+98/+9C
  from the World's stream and copies the requested extent before releasing it.
  The stream's concrete binding and the map-directory writer remain unknown.
  Work-item109410 preserves the original blob pointer at+18, copies12-byte
  coordinates to+20, and copies nonempty blob bytes to a separate allocation
  at+30 (length DWORD+38). Its vtable is340A58. Borrowed identity and owned
  content must not be conflated.
- `r1-navigation-work-item-slots`: a fixed24-byte read at340A58 produced
  executable-address candidates1095C0/109540/109590. The reader does not
  establish their roles or full table membership/extent. Original hash stayed
  unchanged. A fixed Rea query of these bodies and12C770 is the next step.
- `r1-level-load-and-navjob-candidates` completed17/17 with normal cleanup.
  Level12C770 checks `LVL` and a signed-byte version (accepted0A..7F).
  Its chunk sizes are signed-extended DWORDs despite decompiler float labels.
  Chunk1C allocates/copies into5478 and5480 only when5478 is null, preserving
  an existing blob. Truncated chunks log and proceed into postprocessing;
  prior writes are not rolled back. This is not a strict safe-format validator.
  Work-item109540 passes original blob identity and separate owned data to
  1099B0, with a stack-local12-byte coordinate copy and its name field address.
  1095C0 wraps deletion and109590 formats a name; neither proves publication.
  `r1-world-region-entry-bindings` binds exact World::Load21A4F0 and Region
  constructor1916C0 from saved import names, reading2660metadata bytes only.
- `r1-world-load-and-nav-publication` completed11/11 with normal cleanup.
  World21A4F0 retains the resource stream at+110 and calls LoadMap223140 for
  accepted MAP headers; the unrelated WRL route is excluded.1099B0 accepts
  PTHD version1 data with24-byte header, coordinate checks and nested blocks.
  It passes layer bytes to264200 then calls265790 per grid coordinate. Thus
  PTHD cannot be assumed to contain already-finalized navigation mesh tiles.
  Invalid-header paths still reach a completion-record operation; its presence
  does not prove a valid mesh. Real layer ingestion/build and the MAP directory
  remain direct dependencies, rather than generic CRT/thread work.

## Continuation

The subsequent approved `r1-mainmenu-complete-member` read reused part0 and
read only records1..4 (48 bytes) and their 131,004 compressed bytes. It produced
the complete 1,091,357-byte main-menu member, verified Adler32 `760486028`,
and preserved the archive SHA256. Member SHA256 is
`a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289`.
The much larger world member has not been extracted.

`r1-map-directory-and-path-tiles` and `r1-map-region-and-tile-product` both
completed14/14 with normal cleanup.223140 parses metadata at offset8 with
the length stored at4. Its actual Region constructor191980 writes the final
two DWORD arguments to98/9C. Combined with195AF0, this establishes the real
main-menu Level extent `[263292,1091357)` (828,065 bytes), rather than a
magic-string guess. The saved metadata observation contains one Region.

264200 inserts borrowed RLTD-version1 layers into a separate compressed
tile cache; it does not publish a navigation mesh.2640F0 enumerates at most32
layer references per coordinate.2658D0 zeros its24-byte result, decompresses
the layer, builds regions/contours/polygons and calls26E3F0 to create final
mesh bytes. A success status with zero polygons can leave the entire output
zero. Nonempty intermediate results enter implementation+7690 staging;
the consumer that publishes them into the query mesh remains unclosed.

The new strict managed `NavigationPathingData` format adapter passed
14 isolated scenarios /167 assertions at18:16UTC, with no compiler warnings.
Its DLL and complete result are under `.tooling/r1-pathing-data-validation/`.
It preserves borrowed payload identity and ordered nested lengths. Its safe
malformed-input rejections are own behavior, not native failure semantics.
It neither constructs tiles nor substitutes callbacks for real data.

The actual Level has six declared chunks, fully consuming its828,065-byte
extent. Chunk1C starts at member422373 and contains442,996 bytes (SHA256
`0a0a38176e69847c02d65e90805da18c8b849fe1806e16bfc03bcebabc07ccee`).
`level-chunk-observation.json` records the source/constructor/loader evidence.
The approved `tools/run_r1_mainmenu_pathing_probe.ps1` reused the compiled
DLL on that real range and passed: grid(0,0)..(3,3),16 groups,9 layers.
All layer views retain the original member array and the input hashes stayed
unchanged. Each layer is49,208 bytes. This validates actual PTHD framing;
Terrain/Water/SectorLayers consumers and native Level.Load were not executed.

`r1-tile-cache-and-decompression` completed14/14 with normal cleanup.
263E40 initializes the layer pool and separate obstacle state; positive4096
tile parameters yield1024 buckets,index bits12,salt bits20.26CC50 allocates
and clears104+4*width*height bytes, copies the56-byte layer header, and calls
the actual compressor object's virtual slot+10. That slot must be resolved
before claiming a decoding algorithm or decoded data. The approved12-byte
PE data read now binds that slot to109820 and the constructor's34BADC
constant to float32(0.5), with688 metadata bytes and stable Engine hashes.
The subsequent `r1-tile-production-direct-stages` batch completed29/29:
109820 is a raw memcpy decoder, not ARC LZ4. It ignores the capacity argument,
copies its signed length, writes the copied count and returns40000000.
The managed decoder rejects unsafe lengths instead of emulating an overwrite.
No zero/callback stand-in has been used as a successful real tile.

Current status is
recorded in `r1-state-registration-chain/checkpoint.json`; inspect actual
results before retrying any operation. The next dependencies are contours,
polygons, final tile bytes and publication. Cross-polygon search remains pending
real mesh production. No generic thread/CRT branch was expanded.

Previous navigation suites were not rerun. No new client/gameplay claim is
made from the archive probes or static bodies. Original gameplay, input
projection, scene presentation and full navigation remain unaccepted.

## Real layer integration, 2026-10-22 18:58 UTC

The approved fixed build `tools/build_r1_navigation_layers.ps1` succeeded in
1.1 seconds with zero warnings. The three new suites passed46 scenarios and
1,078 assertions: decoder13/344, cache20/448, regions13/286. No older suite was
rerun. Cache's additional `RunSavedMainMenu` fixture entry was not invoked;
actual cache registration was exercised by the separate shared pipeline.
Result: `.tooling/r1-navigation-layers-validation/result.json`, SHA256
`03126519315780e8cbe35559668f348a4d8fa0c711c8b30ba8c471c481efa533`.

`MainmenuLayerPipeline.exe --climb 4` passed all9 actual layers through one
shared cache record, copied layer storage and in-place region output. The
observed dense region counts were2,8,8,19,20,6,6,14,5 (88 total). The saved
source and all build inputs retained their hashes. Output planes/hashes are
recorded in `real-shared-pipeline.log`; these are own outputs, not captured
native goldens. The explicit climb4 selects the evidenced .8f/.2f default
configuration domain, not an observed loaded game's configuration.

The new library implements only evidenced pool/add/enumerate/reference,
raw copy and268CF0 integer region semantics. Managed allocation, input bounds
and neighbor-overrun rejection are own platform/safety behavior. Obstacle
construction, native allocation failures and complete ABI remain excluded.
Failure-prefix tests cover ID exhaustion and rejected neighbor overflow.

The following real Rea batch `r1-nav-update-and-contours` completed17/17 with
normal close/process cleanup and unchanged Engine hash.151180 forwards the
manager implementation to10D990; it does not itself prove staging publication.
269590 still needs2694E0/2693F0 plus two direction tables;269850 needs269780.
These are the next bounded dependencies. No final mesh or path success is
claimed by the nine successful region outputs.

At19:01UTC `r1-nav-publication-and-contour-leaves` completed17/17 with normal
cleanup and unchanged Engine hash.10D990 explicitly passes mesh impl+8 and
the staging container+7690 to265680, then resets its end pointer without
testing EAX;265680 itself remains the publication dependency. The three
contour leaves are now available.269780's pseudo-code incorrectly shows
void/return; its full assembly contains the float32 segment-distance result
in XMM0 and is the supplementary evidence used for reconstruction. Reused
prior34BB44 bytes prove1.0f for the clamp; no duplicate original read was made.
The new approved32-byte direction read confirms X[-1,0,1,0],Z[0,1,0,-1],
with688 PE metadata bytes and stable before/after original hash.

## Real contours and isolated tile format, 19:19 UTC

`tools/build_r1_contours_and_tiledata.ps1` completed in1.05 seconds, zero
compiler warnings. New contour memory tests29/359, saved-member contour
pipeline1/309, and tile-format memory tests12/247 all passed:42 scenarios,
915 assertions. Result `.tooling/r1-contours-and-tiledata-validation/result.json`
SHA256 `21aa2986cf3bbe3e1c35eb45f62ad3e413e7177bb5e9e4bb88968700f17934fd`.
No previous test executable was rerun; earlier compiled DLLs were reused.

The actual9 layers produced88 valid contours (at least3 vertices),602 vertices
and zero0x80-marked vertices, preserving shared source/layer identity. This
observation is limited to explicit climb4/tolerance1.5, not an assertion about
native runtime defaults. It permits this fixed sample to omit the conditional
26B260/26B480 removal path; marked inputs remain unsupported. The reproducible
component driver is `MainmenuContourProbe.exe` in that validation directory.

`NavigationTileData` serializes the evidenced cold26E3F0 domain including
100-byte VAND7 header, transformed float3 vertices,32-byte polygons, reserved
links,12-byte detail records and fan triangles. It preserves separate binary32
operations and explicit unsupported numeric/input domains. Its12 scenarios
use own geometry; no actual sample has yet passed polygon construction into
this serializer. Native allocation/ABI and captured native goldens are absent.

The20-call `r1-nav-publish-and-polygon-core` batch completed with unchanged
Engine hash and normal cleanup.265680 performs old-tile lookup268820 and
removal268970 before insertion268270, frees the input on insertion failure
and does not roll the removal back. Those operations and the two triangulation
predicates26A860/26A9C0 are the next fixed query. The new polygon source is
still uncompiled and explicitly stops at missing predicate bodies.

## Actual tile bytes and shared mesh, 2026-10-22 19:52 UTC

Real saved mainmenu data now passes polygon production, VAND7 serialization,
shared mesh publication, and nine actual nearest-polygon queries. The fixed
explicit cold domain produces 9 tiles, 419 vertices, 245 polygons and 30,940
serialized bytes; insertion produces 520 directed internal links and 66
external links. No corridor result was injected. Mainmenu is a real resource,
not a verified playable combat world; runtime parameter values are unobserved.

Polygon tests: 20 scenarios/238 assertions, zero warnings, result
`.tooling/r1-real-tile-validation/result.json` (59076009...68b393).
Mesh tests: 23 scenarios/434 assertions, zero warnings, result
`.tooling/r1-real-mesh-validation-repair1/result.json`. First attempt compiled
all sources but failed on scenario19 because two own fixtures had five neighbor
slots instead of six. Original failure logs remain in
`.tooling/r1-real-mesh-validation`; production code was unchanged. The repair
recompiled only tests, repeated the18 previously passing mesh cases, then
completed the five remaining cases and actual resource integration for the
first time. Unrelated old test executables were not rerun. Earlier DLLs were
reused in this relevant integration.

Reproduce from repository root:
`& '.\.tooling\r1-real-mesh-validation-repair1\MainmenuMeshPipeline.exe' --explicit-default-domain`
This is an offline component driver, not a game. It reads only the saved
project member and writes console output. Same byte owners, mutable tile/poly
objects and existing query identity are used throughout. No default unknown
mesh, empty mesh or injected nearest callback substitutes for actual data.

Support remains: empty obstacles, null processor, explicit climb4/tolerance1.5,
cell .25/.2, walk2/.5/.8, origin0 and tile32 grid verified against saved headers,
normal managed allocation, stable finite typed views, offmesh=0/BV=0. Marked
vertex removal, arbitrary overlapping aliases, unknown runtime mutation and
native allocation/ABI remain unsupported. Native golden comparisons are absent.

Real Rea pseudo-code/assembly/callee batches `r1-mesh-tile-and-predicates`
20/20, `r1-mesh-links-and-triangulation-leaf`26/26, and
`r1-triangulation-and-tile-connection`17/17 completed with normal cleanup and
unchanged Engine hash. Targeted constant reads4+8 bytes also preserved hashes;
existing float1 and abs-mask evidence was reused. The next six-target23-call
batch is now formally approved/running for cross-polygon corridor/pool helpers.
No full search, funnel, physical mouse projection, rendered scene or R1 game
acceptance is claimed by the mesh success.

## Actual cross-polygon search, 2026-10-22 20:10 UTC

The new production corridor search passed20 new memory scenarios/181 assertions,
then ran twice over the actual saved resource's published mesh. The largest
reachable component has230 of245 polygons. Forward and reverse searches each
returned status40000000,31 polygon references and3 tile transitions; every
consecutive pair was verified against the actual mutable link records.
Endpoint selection traverses the real graph only to pick probe references;
that traversal supplies neither a path nor a callback to the recovered search.
Both endpoints are resolved by the existing real nearest-polygon producer.
The reverse invocation reuses the same query, node pool, queue and mesh.

Result: `.tooling/r1-real-corridor-validation/result.json`, SHA256
`5f5fd685da3b8eeb80bcec6c810db5ab6077e8e2e24f323cdb3173d2dc80215d`.
Compilation of the two new sources, tests and driver completed with zero
warnings. No old test executable was rerun. Existing DLLs and saved-member
producer were reused. Run from the repository root:
`& '.\.tooling\r1-real-corridor-validation\MainmenuCorridorPipeline.exe' --explicit-default-domain`

Search configuration is explicitly owned: pool4096/hash1024/heap4096 and64
area costs of1. The original initializer's2048/512 capacities are evidence,
not runtime values claimed by this probe. Unknown node position/high packed
bits are preserved. Partial/out-of-nodes/capacity flags and output-failure
prefixes are tested separately, not accepted as complete actual corridors.

The genuine Rea batches `r1-navigation-cross-poly-corridor`23/23 and
`r1-corridor-portal-helper`8/8 completed with normal close, process exit,
empty owned Job, sampler exit and unchanged Engine SHA256. Their summaries
are8b98e428...719e41 and3f483f64...cb39ed. Only two additional direct constants
were read,4 bytes each (34BB3C=3f7fbe77,34B9CC=3b808081); both before/after
hashes match. The0.5 operand was reused from prior raw-byte evidence.

This result is a reference corridor, not a walking trajectory or game.
262480 multi-poly straight-path/funnel, physical mouse-to-world projection,
actor motion and real scene rendering remain separate. The single262D30
wrapper query is now formally running as the next direct dependency.

## Actual resource waypoints, 2026-10-22 20:24 UTC

The same published real mesh now feeds the recovered 262480 multi-polygon
funnel through the real 262D30/262F10 portal producer. Forward and reverse
searches each traverse31 polygons and3 tile transitions, producing10 and9
waypoints respectively. Start/end flags, endpoint identity and finite coordinates
passed. An independent own XZ check sampled558/567 positions inside the returned
corridor polygon union; this is not swept collision, height following or actor
width validation. No injected route, empty mesh or point-producing callback was used.

Result: `.tooling/r1-real-straight-path-validation/result.json`, SHA256
`283a663cc6b06ec01717cfd044310688d8b26ded77c63163fc9738671a8d5c66`.
The formally approved build passed16 new scenarios/216 assertions, plus the
necessary20 corridor scenarios/181 assertions against the extended shared library.
All three compilations emitted zero warnings; unrelated old suites were not rerun.
The16 cases include actual geometric turns, reverse search, partial corridor,
capacity/flag handling and unknown/extent failure prefixes.

The 262D30 Rea query completed8/8 with normal cleanup and unchanged Engine hash.
The directly referenced first-portal threshold34B960 was read as raw358637BE,
one binary32 ULP above a decimal1e-6 literal; the implementation uses these bytes.
The four-byte read also retained matching original before/after hashes.

Run from repository root:
`& '.\.tooling\r1-real-straight-path-validation\MainmenuStraightPathPipeline.exe' --explicit-default-domain`
This executable is an offline component probe, not a playable client. It reads
the preserved project map member and prints evidence; no save is created.
Explicit cold-domain parameters and all previous support limits still apply.
Native runtime parameter values, golden output, physical mouse projection,
actor trajectory consumption and real scene rendering remain unverified.
Next work returns to the existing production MoveTo/navigation wrapper and
physical input/target-coordinate connection, rather than general engine internals.

## Consumer connection boundary

Saved Game08E200 calls609F38 for path evaluation. Its ordinary branch requests
endpoint WorldVec3, polygon token, length and direction; path-vector is null.
The separately observed08EAF0/08D9E0 queue holds a destination record and calls
609F08 to start navigation. The recovered funnel output must not be injected
into that queue as though the two interfaces were identical.

The fixed two-slot metadata read passed:609F08 binds Engine151130 MoveObject,
and609BB0 binds Engine22C620 PutOnFloor. Only254 metadata bytes were read;
Game/Engine before/after hashes and file identities match. Evidence is
`r1-move-and-floor-bindings/inspection.json`, SHA256
`66785d22a81c7f2ee2fc2fd497836b3b56f4a12310f31131f73721ef9a266665`.
Root reviewed before formal execution; the independent review completed after
execution and is not represented as a pre-execution review.

Existing screen-ray evidence is also retained: Engine044A10 GetImagePoint and
229700 GetRayThroughImagePoint have genuine saved bodies. The physical input,
viewport/activity source and ray-hit-to-WorldVec3/ObjectID connection to Game
15E1F0 remain unidentified. Portal camera coordinate adapters do not close it.

The subsequent fixed Rea batch `r1-navigation-consumer-direct` completed14/14
on2026-10-22 20:35 UTC. Summary SHA256:
`71d0e41ba73710e496a1db30f1b8a5b24d2485163a49fdca1cd15862baddccd2`.
Normal MCP close, process exit, empty owned Job and sampler exit were confirmed;
Engine before/after SHA256 remained identical. There is no pending backend.
Root reviewed before execution; independent runner review completed after launch.
Two offline preparation errors (missing final-session label and incomplete new
mode registration) were corrected and offline validation passed before any backend
launch; neither was an approval denial or original-program execution.

The new bodies show that151130 adjusts a nonnull Entity pointer by38, loads
manager+10 then implementation+8, preserves the destination inR8 and tail-jumps
25B300. The decompiler omitted that destination argument in its call expression;
the assembly is authoritative. No actual motion is proved by this forwarding body.
22C350 with bool=true skips portal intersection selection but still adds the
coordinates and checks region containment via0664D0. A false containment result
requires21CFB0 region lookup and193AC0 relative conversion; it is not equivalent
to the previously recovered22C5D0 in-region arithmetic overload.
22C620 returns false on null region or failed221DA0 World::PutOnFloor. On success
it copies24 bytes back from a temporary initialized here with only20 bytes;
the tail DWORD must remain unknown unless the callee proves its write.

Next minimum continuation:25B300 actual movement request,0664D0 containment and
221DA0 real floor production. Region-crossing21CFB0/193AC0 are conditional direct
dependencies, not default success. No new consumer implementation or tests are
claimed from these three new bodies; the saved real-geometry component remains
the latest executable validation. This is still short of a usable reversed game.

## Movement consumers and world-coordinate validation, 2026-10-22

The subsequent `r1-movement-floor-core` batch completed17/17 and
`r1-crowd-movement-direct` completed20/20 actual Rea calls. Each saved pseudo-code,
assembly and direct callees; normal session/process/job/sampler exit and unchanged
Engine SHA256 were confirmed. Summaries are in those evidence directories.
The latter summary SHA256 is
`fe035fd2c4f8fff11229a973eb232b7d15215b53137eeffedcfde8436adbbc3c`.

25B300 requires an already registered crowd agent, rejects insufficient travel
and blocking agents, projects the destination through a live query virtual slot,
then writes the actual request state. This is separate from25B6D0/25C440 frame
updates. The02F800 constructor establishes an outer navigation adapter, allocates
the crowd manager and initializes a query through the already recovered25F910.
The crowd manager constructor25A880, synchronous path submission25A4F0 and
overlap query25E320 are the next direct dependencies. The query adapter's actual
virtual targets and the agent callback receiving final positions are not yet
bound; injecting the previously produced funnel into Game's waypoint queue would
not reproduce this structure.

`WorldCoordinateContracts.cs` now implements0664D0 and the bool=true same-region
22C350 branch with one authoritative QWORD/DWORD coordinate view. It preserves
ordered writes, NaN comparisons, explicit region identities and unknown padding.
The separately approved four-byte34C450 read confirmed raw mask80000000; its
688 metadata bytes and before/after hashes are recorded in
`r1-region-bounds-mask/inspection.json`. The mask is explicitly initialized, never
silently supplied to unknown state; its loaded runtime value was not observed.

Formal build `tools/build_r1_world_coordinates.ps1` passed15 new scenarios and133
assertions with zero compiler warnings. Result:
`.tooling/r1-world-coordinate-validation/result.json`, SHA256
`93644347fd4018108c3b94c09142a24f81141da276a5a38b7f55ac138e6e28ac`.
No old test executable was rerun and the old action/movement interfaces were
unchanged. These tests also validate22C620's20-byte temporary and genuine
221DA0 early-false/unsupported prefixes, with no successful-floor callback.
Cross-region lookup stops at22C593 and ordinary floor work at221DCE. The latter
needs actual level/terrain/water/actor-intersection data; navmesh height is not a
substitute. Successful24-byte floor writeback is not validated.

This addition is an isolated component, not yet connected to the older coarse
QWORD coordinate views or a running character. Actual motion, mouse-hit production,
scene presentation and native golden comparisons remain unaccepted. Preserve the
existing real-map navigation executable; do not label this component a game client.

The next `r1-crowd-query-production` batch completed14/14 with normal cleanup,
summary SHA256 `540695b2a21d334be54b0fa2bd8c43e3289acea9fade89d945ed407ef0c1ac0c`.
25A880 proves manager+B8 captures the outer object supplied by02F800.25A4F0
locks, checks the captured request generation, then calls that object's slot0
or18 and publishes completion only after the called operation returns. A changed
generation during the query is not rechecked by this function.25E320 preserves
the requesting agent identity through25A1C0, excludes self, uses asymmetric masks
and searches actual group members in stored order; there is no implicit empty
neighbors precondition.

Two approved metadata reads then bound outer slots0/18/20 to030790/030B30/030BB0
(24B plus688B PE metadata) and Game609F58 to Engine151020 NavManager::AddObject
(150B import/export metadata). Reports: `r1-crowd-query-slots/inspection.json`
and `r1-add-object-binding/inspection.json`. Both original DLL hashes remained
unchanged. The update group's+8 pointer and agent+8 position callback are still
unbound. An old Actor constructor stores vptr33F078 at this+38, but matching an
offset alone does not prove that this is the registered agent's callback object.

The crowd query adapters are now compiled and exercised against actual saved
map tiles. Rea `r1-crowd-adapter-registration` completed17/17 and the focused
`r1-crowd-vector-registration` completed8/8; both closed normally with empty
owned Jobs and unchanged Engine hashes. The brief executor disconnect during the
latter batch did not cause a retry: original session73508 returned exit0 and its
completed evidence was recovered. Summary SHA256
`bf5217e21a8a0f2a7a725c29733bac5d96443ad3ffb6a6ceca159a3b8379daa8`.

`NavigationCrowdAdapterContracts.cs` directly invokes the production nearest and
corridor algorithms for030B30/030BB0/030790. Slot18 tests status bit30; slot20
uses the actual XMM2 radius for XZ and live stored Y, then requires bit30 and a
nonzero polygon reference. Slot0 tests failure bit31, resizes the borrowed vector
to32 before search and shrinks only after a nonnegative result. Native partial
paths can therefore return true without reaching the target. Existing same-hash
Rea030D20 evidence was reused, with no repeat query. Preallocated capacity only,
typed pointer identities and a self-owned BCL Monitor are explicit platform
domains; allocation, arbitrary byte aliases, native synchronization and native
exception cleanup remain outside the supported domain.

Formal build `tools/build_r1_crowd_adapter.ps1` compiled three new sources with
zero warnings and passed8 new scenarios/94 assertions. No old library was
recompiled and no old test executable was rerun. The new saved-resource driver
ran11 slot18 and9 slot20 projections across all9 actual tiles, then a slot0
cross-tile corridor `00400401,00400802`, independently checking endpoint arrival
and actual link adjacency. It reused the existing production tile-building
method, not its old executable entry point. Result:
`.tooling/r1-crowd-adapter-validation/result.json`, SHA256
`9dc3b8b80510b007fc79f7b356092989f57cc1fa86fe68f5b12fdc8125e88ff8`.

25AE20 now proves registration precedes the entity interface slot0 initialization
callback: allocation/construction, list/count/hash publication occur first,
then the interface receives agent+48/+54/+10. Duplicate registration enters the
assertion path, not a success result. Agent+8 identity and the update group's+8
query source still require25E6C0/25A250/25C340. The tested adapters do not create
a loaded crowd, move a character, produce mouse hits or render a game scene.

The follow-up14/14 registration batch then closed those two static identities:
25E6ED stores the incoming Entity+38 interface directly at agent+8;25A3DF/25A3F1
copy Manager+B8 to a newly created group's+8, with group+0=Manager.25A250 returns
the existing first nonnull group immediately: it must not be described as a
recovered spatial group selector.25C35D publishes the member index before the
034CF0 insertion call. These are original assembly observations, not loaded-state
observations or proof of the Game Player's final interface vtable. Summary
`r1-crowd-agent-group-registration/summary.json` SHA256
`eb627b52177b8e9b2b804f9383a9dfc6ad3e179aa8129be8b125fb319eae6f20`;
all owned cleanup succeeded and Engine hash stayed unchanged. The next movement
work is limited to the actual index/neighbor/waypoint helpers and interface
callbacks consumed by25C440, including the parameter-refresh slot+10 as well as
the final position-publication slot+18. No empty-neighbor or cleared-flags
assumption substitutes for these dependencies.

The next23/23 Rea batch recovered proximity Prepare25E900, Insert25EA00,
Neighbors25EB90, borrowed-waypoint25BF40, append034CF0 and reserve058190.
Summary SHA256 `bc08e52772df1c5ff42063bcade04ee162daa47b544d00194da980e17bd50b48`;
normal owned cleanup and unchanged Engine hash. These reveal two direct vector
growth dependencies034EC0/034FE0; they are not replaced with guessed native
allocation semantics. The bounded32B Actor interface read proved all four base
slots0/10/18/28 at33F078 target CE70. This is a base construction table, not proof
of Player's final interface or a successful position publication. The subsequent
Game constructor-name search returned Character052030 and Player3CC3F0;
6/6 calls closed cleanly with unchanged Game hash, summary SHA256
`def1d0911951c67449794738404529609e9e626c23a381af7e8d731824e58a1f`.

`NavigationCrowdStateContracts.cs` now shares Agent/Group/Manager identities and
the embedded request. It preserves exact initialized fields, first-group reuse,
member-index publication before append, and failures after member publication.
The synchronous25C78D fragment copies current position and target, resets vector
ends, increments generation, clears completion and invokes the actual25A4F0
adapter. A formal new-only build passed6 scenarios/119 assertions with zero
warnings. Its saved-map driver built9 real tiles, joined a preallocated group,
and produced generation1/completion1 with corridor00400401→00400802 across one
tile boundary. Agent position was deliberately unchanged; no interface callback
or character motion was claimed. Result `.tooling/r1-crowd-state-validation/result.json`,
SHA256 `43de35ef971ef6f9b7f723817951c2028de46a0b29fd6afd6874b35a9a3bb99a`.
No old production library was rebuilt and no old test executable rerun.

`NavigationCrowdProximityContracts.cs` additionally implements the actual
Prepare/Insert/Neighbors chain over those shared Group/Agent fields. Seven new
pure-memory scenarios/80 assertions passed with zero warnings. They check free
list construction and reuse without erasing gaps, identity and mask filtering,
strict3D distance, negative truncation, hash collisions, Y→Z→X and chain order,
post-pop failure prefixes, and retained output before unsupported growth.
Result `.tooling/r1-crowd-proximity-validation/result.json`, SHA256
`602f427aae0f700be31a9abb86a0d1e1a55b0fa7a9d2c7cb3803b293561738f8`.
Only two new sources were compiled and this new test executable run; no old
tests or saved-map pipeline were rerun. This proves the declared own index
domain, not a complete avoidance update or position publication. Neighbor
output growth at25EE4F remains Unsupported pending its actual034FE0 body.

The approved034EC0/034FE0 batch subsequently stopped at its first pseudo-code
request: only3 lifecycle calls completed, no function-body response was saved.
At21:48:39Z the sampler's after-counters QueryMembers encountered Windows error5
from QueryFullProcessImageName on owned PID18300, creation ticks639283025161805106.
The same-handle exit check did not confirm termination, so the existing guard
correctly failed closed. This was a runtime monitor failure, not approval denial
or physical-memory exhaustion (minimum observed available memory14,316,044,288B).
Summary `r1-crowd-vector-growth/summary.json` SHA256
`e58ed47f4ff779696016b9ea77626d8dad5957467f81363cf988c967c2454631`;
Engine before/after hashes remain equal. Wrapper and sampler exited, but normal
MCP close and an empty Job were not confirmed and are not retroactively claimed.
At21:50:07Z a read-only snapshot found all27 recorded/sample-membership PIDs
absent. The owned runtime directory remains preserved in place; no process was
terminated, runtime directory moved, protection changed, or query retried.
Player/Character constructor queries are a saved unregistered draft. Further
backend execution requires resolving this monitor/recovery condition first.

Recovery follow-up (2026-10-22 22:08 UTC): independent comparison found no
regression in the previous exit-race handling. The exact system cause of error5
remains unknown; missing same-handle exit evidence still fails closed. Only four
wait-budget diagnostic facts were added, after isolated49-assertion/10-group
kernel-handle tests and39 serializer assertions. No permission,identity check,
100ms budget or resource threshold changed. All27 recorded PIDs were rechecked
absent. The exactly owned failed runtime was reversibly moved into
`analysis-runtime/retained/failed-r1-crowd-vector-growth-3f638a53485fa8f0a86b2d3f67878f03`;
49-file content manifests remained equal. Failed CIM inventory was explicitly
recorded as failed and was not used as proof of absence.

The fixed retry returned11/11 responses, including actual nonempty034EC0/034FE0
pseudocode and assembly, and closed normally with empty Job and process exits.
`r1-crowd-vector-growth-retry-1/summary.json` SHA256
`b94fed1c486afde89f3c79984b32e072c5a7cd33cd999dcf0c63bfdad527c05a`;
Engine before/after hashes remained unchanged. The failed batch still has only
3 completed calls and unconfirmed historical close/empty-Job; it is not
retroactively relabelled. Growth chooses at least requested size or1.5x existing
capacity; actual reserve allocates/copies/releases before publishing capacity,
end and begin in that order. Own implementation and targeted validation follow.

A separate bounded32B read proved query-table slots+8/+10 at33A6C8/33A6D0 target
030900/0309F0 and retained four exact motion-constant bit patterns. Report
`r1-crowd-motion-bindings/inspection.json` SHA256
`cefbf931ba37144e6400be0d7a0c3c894f9a82828c19723d78be440a729334d3`.
These are disk bindings, not observed loaded dispatch or character motion.

The next Game batch completed11/11 real Rea calls and cleanly released the Job,
with Game.dll hashes unchanged. Summary `r1-game-entity-constructors/summary.json`
SHA256 `4380a26444ce8cec142b8016326981a4aefcf8df26e0c8a8f833dc03fcd4001e`.
Character052030 writes its +38 vptr to6DF690 at052074/05207B; Player3CC3F0
calls Character052030, then overwrites the same +38 vptr with787080 at3CC433/43A.
This is the missing derived construction binding. The next bounded slot read
will resolve the necessary callbacks; the table write alone proves neither
loaded dispatch nor completion of all constructor dependencies. No complete
Player construction or movement is claimed by this evidence.

Growth integration passed its formally approved local build:7 new scenarios/88
assertions, State6/119 and affected Proximity7/83 regressions; five compiles,
zero diagnostics. Only Proximity's former full-output stop case and its live
buffer accessor needed updating. No saved-map driver or unrelated old suite ran.
Result `.tooling/r1-crowd-growth-validation/result.json` SHA256
`f9005adbc8c3becc734cbcf19d6f5e8563ccfc9a877e2b7e5944621f03956796`.
Group/member/neighbor pointer vectors now grow in the explicit normal managed
allocation domain. Native allocator/OOM/EH and continuous motion remain unproved.

The approved64B derived-interface read also passed with unchanged Game hashes:
Character slots00/10/18/28 target066C20/066D60/066EE0/067170; Player's slot00 is
3E8200 while the other three targets match. Report
`r1-player-movement-interface-slots/inspection.json` SHA256
`5045ca6aa8708930c026e116289a673d279010e50c357d80437ee7d3775c8696`.
These are exact static addresses, not yet callback ABI/body or loaded dispatch.

The real Game callbacks then returned20/20 responses and clean close/empty Job;
Game hashes were unchanged. Summary `r1-game-movement-interface/summary.json`
SHA256 `185bc461ac2ffef387a1d894582188cc18ff71d6523986b23d5325a0090df553`.
Character creation writes flags0x481; Player calls it and ORs0x80000044, giving
0x800004C5 plus the conditional0x20. Path and avoidance branches therefore
cannot be skipped by choosing a conveniently simple configuration.
Callbacks use Entity+38 and subtract38 internally. Slot10 receives the shared
parameter area, while slot18 receives the original DWORD tick and an extended
CrowdAgentData view beginning at Agent+48. Its +18 is Vector60, not Heading54.
Moved's actual position route converts into a fresh WorldVec3, requires real
PutOnFloor success, then calls05E160 RotateTowards and533410 SkillManager.Moved.
These remaining world/scene dependencies are explicit; no movement success is
manufactured from the newly recovered callbacks or fixture coordinates.
The fixed motion-branch batch returned29/29 actual responses and closed normally,
with empty owned Job and confirmed sampler/process exits. Summary
`r1-crowd-motion-branches/summary.json` SHA256
`d443fe60cf6e72100ad30e0d18e5d41ad758f61b47553f808feb3d71b3d5faf0`.
Engine hashes remained unchanged.030900 calls the already recovered262480 real
straight-path producer with max4, then resizes from the actual output count;
the decompiler's apparent constant0 at the final resize is contradicted by
0309B6/0309C0 assembly.0309F0 uses nearest queries followed by263290; this is
not a substitute for the existing point projection helper.030C60 point storage
growth and25F250 avoidance have explicit direct dependencies still to close.

The approved callback-dependency read consumed64B of primary-table slots and
1384B of exact import/export metadata, preserving both original hashes. Report
`r1-game-callback-dependencies/inspection.json` SHA256
`c0f0348d8f0712b35efd22d7ed36282065bfc31c78ee18b0f1d870e036a4afab`.
Entity GetCoords binds07F070, GetMotion07EB20, GetRegion07F130;
WorldVec3 GetWorldPosition22C1A0; Actor GetHeight02B460;
GetLastFrameUpdated07E7C0 and Engine GetFrameCount077880.
Character and Player primary slots+1C8/+458/+830 both bind5D9F8C/05B730/05FC30;
class slot0 differs019D20/016D10. gEngine's exact exported address is421428,
without reading its payload. These are disk bindings only; getter bodies,
loaded identities and world position publication are not implied by the names.
The getter batch completed26/26 with unchanged Engine hash and normal complete
cleanup. Summary `r1-engine-callback-getters/summary.json` SHA256
`66ca39735f5e5efb7dee030e27d4b2aff031e6fdaeca2b92711670305a6054d9`.
Actual07F070 copies four16B blocks from Entity+D8 to the hidden output when
Parent130 is null:64B, including the final four bytes. The earlier60B frame type
is insufficient for this body. Non-null Parent130 instead dispatches parent
slots180/188 and22A950, which remain explicit dependencies.07F130 walks Parent130
to the root before loading+D8.07EB20 returns the borrowed Entity+1D0 address.
22C1A0 writes hidden output X,Z,Y in that order and returns that output pointer;
its integer region offsets and local coordinates are live inputs,not defaults.
Height is Actor+4A0 times+3AC; frame values are DWORD Entity+178 and Engine+640.
These bodies establish field/identity contracts,not valid object initialization.

The separate exact raycast threshold4B read passed without changing Engine:
34BB38 raw `a4707d3f`, binary32bits `3f7d70a4`.
`r1-crowd-raycast-threshold/inspection.json` SHA256
`e61be46fbafe53e8b413ea3fe50083e006a9533692467cf40b6035efc6dacc1d`.
This is the disk operand for030AEC; no loaded operand or FP-mode observation is
claimed. New shared-waypoint source is pending compilation and growth evidence.

## Direct growth and raycast bodies, 2026-10-22 22:59 UTC

The fixed Rea batches r1-crowd-direct-algorithms (14/14),
r1-crowd-point-reallocation (8/8), and r1-crowd-point-copy (8/8)
returned real pseudo-code, assembly and direct-callee results. Each preserved
Engine SHA256 and confirmed normal session close, process exit, empty owned
Job and sampler exit. The latest summary is
r1-crowd-point-copy/summary.json, SHA256
2157fd2bc737adf72402a85383eb0863ec220743de87c806b3efde47ccd5172f.

030E10 reserves additional stride12 points and calls030EE0. That body allocates,
reads the old End/Begin for021300, then rereads old headers before freeing and
publishing Capacity, End, Begin.021300 is a forward per-point copy: load/store8B,
then load/store4B, increment both addresses12B; no direct callees. This is not a
whole-range memmove snapshot. Native allocator/EH and loaded00CE70 remain outside
the own normal allocation and explicitly selected disk-leaf adapter domain.
The new shared waypoint implementation/tests are still pending compilation.

263290 is the actual navigation raycast consumed by0309F0. Its clipping and
partial-link traversal are being integrated with the existing real mesh views.
Exact4B read r1-raycast-parallel-threshold/inspection.json confirmed34B93C raw
77cc2b32, bits322bcc77; SHA256
079af19f84c0af4b068d84e9a67d1478adfb8fef8ec1d8853635be22b6ede742.
25EF20's pseudo-code omits substantial terms and incorrectly presents a void
return; assembly returns a float in XMM0. Avoidance is not implemented from that
incomplete pseudo-code. No actor position has yet been published by these steps.

## New adapter validation and bounded recovery, 2026-10-22 23:11 UTC

Formal local build of NavigationCrowdRaycastContracts completed with no compiler
diagnostics:18 new scenarios/198 assertions. It covers actual produced fixture
mesh links, polygon clipping, partial portal intervals, result truncation, unknown
fields and0309F0's two real nearest queries. No old suites ran.
Result:.tooling/r1-crowd-raycast-validation/result.json
SHA256:d95b7c020e06c48016411b7a88fb6e0709614d5e659ab71604c2b53ada90fce9.

The shared waypoint build completed four compilations without diagnostics,
15 new scenarios/238 assertions, and one new saved-resource continuation.
The existing9-tile producer was reused through BuildExplicitRequestContext;
a real2-polygon corridor crossed1 tile boundary, produced2 points from the native
null waypoint triple, then removed1 start point and retained1 point. Borrowed
identity and unchanged Agent.Position48 were checked. Cold parameters and the
own disk-CE70 no-op choice are explicit, not loaded-original observations.
Result:.tooling/r1-crowd-path-validation/result.json
SHA256:955a7b168d401088e1723522545709eea8da42653fc086576496c5ab8a526b4e.
The first attempt stopped before output creation/compilation because one recipe
hash literal transposed bdc to dbc. The unchanged reviewed source was verified;
only that literal was corrected, all20 inputs independently hashed, and the
corrected script was formally approved. Failure evidence remains in
.tooling/r1-crowd-path-precompile-failure.json. No old test suites/Main ran.

The subsequent fixed Game6 batch returned only3 lifecycle calls; call4 pseudo-code
for019D20 did not return. Failure summary995d942f82bf422e666ae8557f97f34c682e2489be6300464e0aed7ccfef3838
is retained under r1-game-callback-core. New diagnostics prove the once-only100ms
wait opportunity had already been used byPID39924 with confirmed exit. Later
PID41796's same-handle zero-wait did not confirm exit after image-query error5.
Fail-closed was correct; elapsed wait and the underlying Windows cause remain
unknown. No monitor policy/permissions/resource gates were changed.
All26 recorded PIDs were subsequently absent. The exact owned failed runtime
was reversibly moved to analysis-runtime/retained/failed-r1-game-callback-core-cf94a6b7868b84ea869e89d2a19dd997;
43 files/226029208 bytes were hash-preserved, not copied or deleted. Old normal
close and Job-empty results remain false. One same-target retry is being prepared
under fresh original guards; no Game callback implementation is claimed passed.

EntityMovementContracts.cs is a reviewed but uncompiled source for seven actual
getters and shared64-byte coordinates. Created/Update/publication are still
pending direct bodies. R1/R2/R3 gameplay acceptance remains false.

## Partial retry and exact radius binding, 2026-10-22 23:28 UTC

The same-target Game retry ended with an input-resolution failure after9 plan
calls, not another monitor error. It returned real019D20 and016D10 pseudo-code,
assembly and callees: Character class identity180A5CBA0 and Player180A5CAB0.
The5D9F8C request failed when Rea could not find a procedure and its bare-hex
fallback treated the address as a file offset. The remaining three bodies had
not been issued. Summary d1eb4e87f69c429fbe14d65bfdb409dd6b3bda1dea1858b0dc8fedf11cdfb1a4
retains failed status; close/process-exit/Job-empty/sampler-exit are all true,
and original Game hash is unchanged. This is not an approval denial or evidence
that the candidate has no behavior.

A separately reviewed/approved fixed disk reader observed6 bytes at5D9F8C:
FF25EEF20200. The exact binding is Game IAT609280, Engine.dll import
?GetRadius@Actor@GAME@@UEBAMXZ, export02B410. Only82 metadata bytes were read;
both original before/after hashes agree. No radius body was read or simulated.
Evidence: r1-game-radius-thunk-binding/inspection.json.

A fresh14-call batch now queries only previously unissued05B730,591480,05E160.
The original identity/ownership/resource gates and monitor policy are unchanged.
Entity getter tests are being prepared; no new actor position publication or
R1 gameplay acceptance is claimed.
## Entity getters validated; interrupted query retained, 2026-10-22 23:40 UTC

The seven Entity/Actor/Engine/WorldVec3 getters now compile locally with zero
compiler diagnostics.15 new scenarios/129 assertions passed, covering64-byte
interleaved copying, shared borrowed cells, unsigned frame fields, X/Z/Y store
prefixes and unknown/lifetime stops. No old test suites ran. Result:
.tooling/r1-entity-movement-validation/result.json
SHA256:8f3ef00be0d94da17f6d3ec4ae79081a0c562e9492a9e4c6f6d0ae6c49a77fff.
These getters do not publish character positions or validate loaded gameplay.

The Game-three-target execution session disappeared after23:29:31. Its first
three lifecycle calls returned; call4/05B730 did not return. No body, summary,
normal close or terminal Job/sampler record exists. The last saved monitor sample
has no error and about13.3GiB available; the interruption cause is unknown.
Evidence:r1-game-callback-remaining/interruption-observation.json. Do not count
call4 as complete or replace the absent summary with a synthetic success/failure.

A formal recovery script stopped before inventory/move because historical PID42564
was present. A separate formally approved CIM query established a different
identity: historical java created23:26:43.135068Z, current svchost created
23:34:09.149409Z. Earlier sandbox-only Get-Process visibility was incomplete.
The formal snapshot is .tooling/r1-game-callback-remaining-retry-prepared/formal-process-diagnosis.json
SHA256:17f74f6dbb33f201636aaf9fcc13be934e07fb7e862f37b2e22273aabe0e85f1.
The first helper also treated all Node processes as conflicts, including observed
Codex runtime executables; the actual existing heavy-process monitor excludes
Node. A narrowly reviewed helper correction is being prepared with exact identity
checks and the unchanged native monitor/heavy-resource rules. No process was
terminated, no runtime moved, no security policy changed and no retry started.

A call-site evidence correction is retained: Created25AF13 uses the original
25AE20 callback parameter captured inRSI, while Update25C9AF reloadsAgent.Callback08.
These distinct identities must not be forcibly synchronized. New callback source
is still uncompiled and stops on unreturned radius/action/parent-helper bodies;
it is not a complete registration, update or movement path.

## Callback prefixes validated; strict recovery stop, 2026-10-22 23:54 UTC

The isolated callback prefixes now pass 8 new scenario groups / 79 assertions,
with two compilations and zero diagnostics. No old suite was rerun. Evidence:
`.tooling/r1-game-movement-callback-prefix-validation/result.json`, SHA256
`03d03827384e6f9eca2906cb789ca9853768a545724ae92a3aef03edfc69b568`.
Radius, action and parent-helper bodies still stop explicitly as Unsupported;
full Created/Update and actual motion are not validated.

The strict recovery classifier passed 55 synthetic cases / 165 assertions.
Its formally approved recovery invocation then stopped before inventory/move:
PID26932 is currently a cmd.exe created at23:37:21.294733Z, while the historical
record is PID-only in the sample starting23:28:35.1485832Z (elapsed505.7726ms).
That observation-time evidence is being reviewed, not silently merged with a
different known process generation. Evidence and all runtime files are retained.
No process was changed and no query was restarted. This was the helper's safe
guard, not an approval rejection. The original monitor policy remains unchanged.

At23:58:24Z a formal read-only snapshot still observed the same current cmd.exe
PID26932. That diagnostic accidentally passed the historical JSON envelope
instead of its identities array; the classifier correctly rejected it as
historical-pid-unknown. Its full identity classification is invalid and was not
used for recovery. The direct current-process observation remains recorded in
`.tooling/r1-game-callback-remaining-retry-v2-prepared/formal-recheck-after-stop.json`.
Review of the sampler proved elapsed_ms is computed from two UtcNow values,
without an independent monotonic clock. No timestamp exemption was introduced.
The unchanged-policy recheck wrapper is prepared but unexecuted; there is no
pending approved operation and no new query. The runtime remains in its original
active directory; its evidence has not been moved, deleted, or relabelled clean.

A final read-only hash check at this checkpoint again matched Game.dll
`cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`
and Engine.dll
`4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`.
This does not retroactively supply the interrupted batch's missing terminal
cleanup record. The next direct bodies remain Game05B730/591480/05E160 and
Engine02B410. Floor placement, position publication, physical mouse coordinates
and scene presentation are still incomplete; R1 gameplay is not accepted.

## Current blocking process identified, 2026-10-23 00:09 UTC

The current PID26932 is a cmd shell running
`call ./scripts/launch_codex_app_tools_mcp.cmd ./server.mjs`, with reported
parent34744 (Codex exec-server), direct child11048 (Codex cua_node server.mjs)
and43860 (conhost). This is the current Codex app-tools MCP service identity;
the reported PID ancestry is not used to infer historical membership.
The read-only query ran as zhuzi, with Administrator role false.

One same-handle query, requesting only QUERY_LIMITED_INFORMATION|SYNCHRONIZE,
matched the CIM microsecond and cmd image. Native creation is
23:37:21.2947337Z; both zero-time waits found the process alive; the query handle
closed successfully. `IsProcessInJob(handle,NULL)` succeeded with true. This
proves some Job membership, not identity with the old unnamed Rea Job. No access
denial occurred and no old-member exclusion is justified. Evidence:
`.tooling/r1-pid26932-identification/same-handle-query-result.json`, SHA256
`66ab165200344d2014cb178a01b3867313767a9f6060d395cad3703fd5bc66e0`.
The nonzero script exit is its deliberate unresolved-result stop, not an approval
rejection. No runtime movement, backend restart, or process modification followed.

The saved old sample really did confirm26932 in the old Job:20 IDs,20 successful
member probes and no gone/nonmember IDs. Its creation identity was omitted because
the serializer used the earlier members collection instead of finalMembers.
The isolated serialization candidate preserves both collections; production
sampler and all admission/resource rules remain unchanged pending validation.

If no independent identity evidence becomes available, the concrete human action
is to coordinate other Codex work and approve restarting this app-tools MCP
service. This session exposes no dedicated service restart tool. Normal exit and
reopening of the Codex desktop application is a broader user-operated fallback,
affecting Codex connections and potentially active tasks, not Windows or Steam.
Neither operation is authorized or performed by this diagnostic.

The isolated additive serializer passed its one new pure-data test run at00:12Z:
5 positive scenarios and6 malformed-data checks in the test validator. It covers
late-only/first-only members, reused PID with distinct creation, exited missing
image and empty arrays, without inventing successful cleanup. Malformed-data
rejection is a test assertion, not a newly introduced production admission rule.
Result: `.tooling/r1-member-identity-serialization-candidate/test-result.json`,
SHA256 `812f576f55ce8989638accf2a668a6b1b19e0d23e2869328be63ba7822c3062d`.
Production sampler, owned-Job code and runner hashes remain unchanged. No old
suite ran, no candidate was deployed, and this cannot reconstruct the omitted
identity in the old sample. No native movement queries were resumed.

## Post-restart recovery, 2026-10-23 02:06 UTC

The user restarted Codex. A complete formal CIM snapshot confirmed PID26932 and
the old sampler/controller absent. Pinned run source and all113 original samples
establish a private, non-inheritable, kill-on-close Job owned by that sampler.
This supports a separately labelled Job-lifetime inference; it does not create
the missing normal-close, terminal Job-empty, or call4 response records.

The small recovery supplement passed40 synthetic scenarios/122 assertions, with
no rerun of the old55 cases. Formal retention then succeeded:120 files,
303049271 bytes, identical before/after content manifests, no deletion or process
termination, and no active runtime directory. Evidence:
`.tooling/r1-game-callback-remaining-post-restart-prepared/runtime-quarantine-result.json`.
The old runtime is preserved under
`analysis-runtime/retained/interrupted-r1-game-callback-remaining-8038465c1a920f04ed2efb88a70891ee`.
Fresh checks admitted only the exact signed new Codex Node image; unknown processes
remained blockers. No security or original-file change was made.

The next fixed queries remain Game05B730/591480/05E160 and Engine02B410.
Previously returned019D20/016D10 responses will be reused. Recovery and its tests
are infrastructure work, not new movement functionality or R1 game acceptance.

## Four resumed targets returned, 2026-10-23 02:23 UTC

Game retry returned all14 planned calls; Engine radius returned all8. Both include
nonempty real procedure_pseudo_code/assembly results, normal session close,
process exit, empty Job, sampler exit and unchanged original hashes. Raw evidence:
`r1-game-callback-remaining-retry-1` and `r1-actor-radius`; summary hashes respectively
`f6a70555b8ab3df2e8cd5f2de5a64108d6bca32ce2cee7d9b0269df28de97a29` and
`799e5728ca5c792bab078bf361cb952fdae1827ba103bfbd15b40be61f78c2de`.

05B730 reads the DWORD at full Character+24B4.591480 tests Parent10, not self,
and recursively checks ancestors; callers consume AL, not an invented full-width
return.02B410 loads4A0,49C,4A4, computes(49C^2+4A0^2)+4A4^2, takes its zero arm
also on unordered UCOMISS, otherwise SQRT, then multiplies3A8. It is not the
GetHeight3AC formula. These contracts are being integrated, not yet compiled here.

05E160 RotateTowards has four publication paths through Game IAT609BB8,
World::SetCoords, after obtaining coordinates. Its direct matrix helpers591A10,
591B80 and07E6F0 plus exact publication import binding are the next bounded scope.
Small-direction publication can return false; this Boolean must not be replaced
by a generic movement-success flag. Full movement/mouse hit/scene remain open.

At02:30Z the new isolated Entity/Callback build passed:3 compilations with no
diagnostics,12 new groups/77 assertions and8 affected prefix regressions/79
assertions. Old15 getter and navigation suites were not rerun. Result:
`.tooling/r1-game-movement-callback-contracts-validation/result.json`, SHA256
`7756201f00ff66c5808b0e8c579769d7255700ce2f89fac4d199f5cb03a9f1ff`.
Created and Update now return in the explicit shared-state domain, including
live action/parent mutations, borrowed parameter identity, native-unwritten24,
unknown-field failure prefixes, NaN/zero radius and unsigned frame boundaries.
No MOVED/position publication/native loaded-game acceptance is claimed.

The next Game math batch also returned14/14 and cleaned up normally; summary
`f2824305c00a8807be3273eac11359ba5b789180d4ef3680fe100ee8cc76669d`.
591A10 calls sinf/cosf;591B80 and07E6F0 have no direct callees. The exact disk
binding reader identified World::SetCoords atEngine21BB40 and WorldCoords
constructor at228AB0, reading258 metadata bytes including36 constant bytes,
with both original hashes unchanged. These new bodies/bindings are evidence,
not yet a completed position publisher or a playable scene.

## Position publication boundary and math validation, 2026-10-23

The Engine21BB40/228AB0 batch returned11/11, with normal session/Job/process/sampler closure and unchanged original hash. Summary SHA256: `cca1bb3618172f018173050b940f65bbeb56afeab66013eceeacefe5c928d2f1`. WorldCoords228AB0 clears only its first8 bytes and returns the same address; remaining bytes are not initialized. World::SetCoords21BB40 checks old/new regions and Entity virtual190. Even its same-region path obtains a level through1943E0, constructs local48-byte Coords, and calls12F4F0 Level::MoveEntity. Cross-region removal/insertion, network-list changes and destruction are outside the current normal-path scope. A field-copy substitute would omit observable behavior.

The separate-buffer591B80 basis and07E6F0 composition component was compiled twice without diagnostics;13 new scenarios/270 assertions passed, with no old suite rerun. Result `.tooling/r1-game-movement-math-validation/result.json`, SHA256 `6efc90a43e9a306c9a47e250b0584c62622c9cd0b1785608472d4220abb63b35`. It preserves ordered binary32 operations, D7/D10 addition order, unknown-consumption and partial-write boundaries, output identity, signed zero and degenerate NaNs. Overlapping outputs, native FP environment/NaN payload equivalence and591A10 sinf/cosf remain outside its supported domain. Actual position publication, physical mouse input and scene rendering are still unaccepted.

At02:47:38Z the next fixed Engine1943E0/12F4F0 query was blocked by prelaunch workload/resource guards: external Unity2022 VBCSCompiler `dotnet.exe` PID36084, created02:44:30.0635610Z; physical4466425856 bytes and commit headroom11592675328 bytes. No backend started, no query output directory was created,0 MCP calls occurred, and no execution remains pending. This was a guard block, not an approval denial. PID26932 and active task runtimes remain absent. The frozen command is `D:\NodeJS\node.exe tools/static_analysis/rea_native_registration.mjs --r1-same-region-movement`; retry only after a fresh normal resource/identity/mutex check confirms recovery. No external process was changed.

Independent lightweight disk metadata observation then passed: two8-byte slots at Game6DF860 and787290 point respectively to016D20 (Character) and3D5BC0 (Player). Evidence `r1-entity-publication-slot/inspection.json`, SHA256 `0ab3d46d0ee8d8f45cc784969617872d24a3948bb969b3318dd1327e1696237f`; Game before/after hash unchanged. No executable body was read or invoked. Target names, behavior, loaded overrides and table extent remain unverified; no matching body filename was found in the scoped reverse-client evidence inventory. These distinct candidates must not be replaced by a common default Boolean. The next safe continuation is the frozen two-target Engine batch, followed by these exact virtual targets if still required by its normal path.

## Same-region prerequisites and exact thunk targets, 2026-10-23

The resource guard recovered at03:32:59Z (11132678144 physical bytes,13813760000 commit-headroom bytes, no conflicting workload); this resumed the current session only. No scheduler or persistent automation was created. Engine1943E0/12F4F0 then returned11/11 with normal cleanup, unchanged input and summary SHA256 `1266e603157f4ac42b7f8cb164daa057de79e96c40b95f029b94bbd4336ce3b2`. GuaranteedGetLevel(false) invokes194440(false,false), resets88, checks90, enters Critical18 and returns the captured Level68 after leaving. The explicitly preloaded194440 arm returns without reading render-ready state; cold loading is still outside the own implementation.

Level12F4F0 captures input and region before GetCoords, then rereads input for the actual four16-byte coordinate stores. It captures vptr after the first store for slot270, reloads vptr for slotD8, calls spatial1333E0, and reloads again for tail slot90. Copying coordinates alone would omit these observable effects.

Game publication-gates batch ended with9 completed calls, then call10 failed because Rea did not identify a function at5D9FF2 and fell back to treating the numeric VA as a file offset. Its summary SHA256 is `3e1d3b25f3031b3fdbcb3ac8419d855502784d3accf68b633fe63fe2ad8132b3`. Session, process, Job and sampler cleanup all completed normally; there is no pending operation. Successful Game016D20 returns AL=1; Player3D5BC0 reads gEngine and returns the inverse of IsNetworkServer. These responses were retained and not repeated.5D9F02 and015880 did not run in that batch.

A subsequent bounded read verified two6-byte FF25 thunks plus302 bytes of exact import/export metadata:5D9FF2->609308->Engine00CE70 BillboardPunctuation;5D9F02->6091C8->Engine02A990 UpdateBoundingBox;609A40->Engine078110 IsNetworkServer. Both originals retained their full before/after hashes and were closed. Evidence `r1-entity-publication-thunks/inspection.json`, SHA256 `f536acfdd3216dac8d020a129735f2093e693fbeb6627847eafcc03ecac1911a`. These are disk bindings, not observations of loaded dispatch or successful publication.

The new own WorldPublicationPrerequisites component passed two compilations without diagnostics and9 new scenarios/70 assertions; no old suite was rerun. Result `.tooling/r1-world-publication-prerequisites-validation/result.json`, SHA256 `43da143e7dca57b18fffa4316797c734a809d2a223a91295fb8b95d9a6855c42`. It implements the first-QWORD-only constructor and explicit preloaded level access with shared identity and real managed recursive Monitor ownership. It does not claim native CRITICAL_SECTION layout/lifetime, cold loading, cross-region movement, post-publication callbacks, spatial updates, or gameplay acceptance. The next bounded work is the actual publication callback/spatial bodies; the existing R1/R2/R3 gameplay acceptance flags remain false.

## Publication dependencies and real ground source, 2026-10-23 04:13 UTC

Three fixed Rea batches completed with nonempty real bodies, normal process/Job/sampler/session cleanup and stable original hashes: Engine publication-core17/17 (summary762137cc388110d1ca7d9a03c1ea396fb09df460c41b2e711017b2bee93b13e6); Game level-tail11/11 (4c269cb791f865f70be615c352dfe6c88eefef519dfa67924f7c3d0ca36a400b); Engine spatial-ground20/20 (1c7c9a62f71ad344b71d89208b0a1495b8ccf44933629a0de0353adf330c5215). These are evidence completion counts, not recovered gameplay percentages.

Engine078110 compares live identities678 then668; Player3D5BC0 inverts AL. The separate own gate component passed12 new scenarios/81 assertions, two compilations with no diagnostics, no old-suite rerun: `.tooling/r1-world-publication-gates-validation/result.json`, SHA256 `ab2a1442c32b05c1255b1eedb31a6a7f9634065495d647e5e9b2e307226afc44`. Unknown global/pointers remain explicit stops. The observed disk00CE70 leaf is opt-in own behavior, not a claim about loaded code.

Actor02A990 uses real IsInWorld07ED70 (Parent130==0 and regionD8!=0), then a borrowed mesh selected by Game06DA30 from2668/2670. Both-null reaches unimplemented609688 Actor fallback. Bounding boxes use exact binary32 operation/store order and existing mask34C410; attached-ID lists cannot be assumed empty. Game015880 is a folded disk RET0 target. Spatial1333E0 invokes134500/133BC0/133A10, which maintain real linked-list membership, child selection and bounds;11BE80/11BBC0 are the remaining direct leaves. A null node or forced depth8 must not be used to label movement complete.

Saved mainmenu ground chunk6 contains type2 at member273260, verified4B with saved-member hash stable. Datum: `r1-mainmenu-complete-member/ground-type-datum.json`, SHA256 `728179dc13f72c823688eca748de21e87842496ae6a91e4ab5793d3ebb2a1f0c`. Its actual constructor1DDA90 calls TerrainBase and installs primaryvptr343B60; Level12C770 subsequently calls slot88. The precise loader still needs binding. Navigation vertex height remains separate from native floor collision. No game, original save, security configuration, commit or push was performed; actual movement, mouse hit and rendered scene acceptance remain pending.

## Shared position publication and real ground boundary, 2026-10-23 05:00 UTC

The own same-region chain now runs through World21BB40 -> Level12F4F0 -> Actor02A990 -> spatial1333E0 using one mutable Entity, canonical Character/Player dispatch identity, borrowed mesh bounds and the same Box44. The20 new integration scenarios passed721 assertions, two clean compilations, no old suites rerun. Result `.tooling/r1-world-same-region-publication-validation/result.json`, SHA256 `e61195d71263fe3d40ed725c4d3cfcb8de57a522d56b2e81d663fc3b9702489b`. Output DLL SHA256 `ea1ed7cfac9b45e8fdbd01007e846b1ac791603ad15e19309407292aec0e4549`. This is an end-to-end **position publication component given explicit own preloaded state**, not an end-to-end mouse movement or game test. Real mesh production, native loaded globals, cross-region behavior and scene rendering are outside this result.

Spatial production passed23 new scenarios/273 assertions, including nonempty linked membership, child allocation, Y propagation, pruning/destruction and reclaimed allocation capacity. Result `.tooling/r1-entity-spatial-tree-validation/result.json`, SHA256 `eb1bf1dde1adc7d9bf1adb0eaa72cf0a2d79e704c39d8c52f1276c4e8e467823`. Independent review found one precise failure-address error: parent dereference occurs at11BBF0, not LEA11BBDE. The sole source correction passed3 additional scenarios/17 assertions with no repeat of the23; revised result `.tooling/r1-entity-spatial-tree-readpoint-validation/result.json`, SHA256 `b7df91525fa9b72cf73751edf1e09b3e2f13442218b124e57d77b7b3d2c12237`. The World build uses this revised DLL. Canonical Actor identity integration had separately passed4 new/192 assertions plus29 affected bbox regression scenarios/444 assertions; those results were reused, not repeated for these builds.

Actual saved mainmenu ground has a129x129 sample layout and five terrain records. The conditional layout reader consumed the exact149073-byte chunk to its end and preserved the saved member hash (`r1-mainmenu-ground-layout/inspection.json`, SHA256 `64b6321a8a1e3a7c0fa0648cfd6aa3ad7e2ce65e5cfd47c094b3a40ad22335a6`). Native1CE910 uses15x15 blocks of strideE0. This establishes data layout and construction inputs, not height interpolation or collision. Native loading also has graphics/global dependencies; an own cold-data loader must remain explicitly separate from claiming that nativeLoad succeeded.

Real Rea1D0100/1D0FC0/1D0E20 bodies returned14/14 with normal cleanup and stable original hash (`r1-ground-floor-geometry/summary.json`, SHA256 `f0ad572426d34f1f5b21a848dbdee54020a1db3747a78cbdc259e26ec896663c`). A52-byte fixed read then bound Terrain+100 to1DE880, Block+10 to1D1330, Block+18 to00CE80 and Block+48 to1D12F0; it also recorded exact +Inf and all four XOR sign-mask lanes. Evidence `r1-ground-geometry-bindings/inspection.json`, SHA256 `8be1f21796126e33fae05d412b4690b8d7cf5618bda89593d4f6021230621d17`; Engine hash before/after `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`. Slot targets alone are not function behavior or loaded-dispatch verification.

Remaining direct links for the minimal real-ground movement loop: actual sample/ray/block/tree semantics (seven-target ray/tree batch completed26/26; subsequent ground-intersection batch completed1DF220, then stopped at1DE850 address-resolution error and cleaned up, see live checkpoint); PutOnFloor221DA0 post-hit checks including130270; Character066EE0 ->05E160 with actual floor output and agent direction; exact trig/platform contracts for ordinary turning and the533410 movement suffix; real mesh/source state; physical mouse hit coordinates and scene presentation. Existing real-resource navigation and this position publisher are still separate tested components. No nav-height substitution, default success, game acceptance, original execution, save access or persistent scheduler is claimed.

## Actual tick boundary and current platform math, 2026-10-23 05:21 UTC

The current real-resource corridor/waypoint pipeline explicitly preserves agent Position48; it does not execute the movement tick.25B300 updates a target request.25C440 produces the per-frame position/direction, including avoidance and collision corrections, then passes a borrowed agent+48 address to callback+18. Character066EE0 reads its direction at argument+18/+20, i.e.agent+60, not agent+54. This tick and its required avoidance branches are not yet implemented. Existing path and position-publication tests must not be described as actual character movement.

The separate current-host System32 UCRT float adapter passed11 new scenarios/96 assertions and33 finite math calls, two clean compilations, old suites0. Result `.tooling/r1-system-ucrt-rotation-validation-2/result.json`, SHA256 `ca025983c2731fedf773a0235cfeaa1152e03388c22bed80e716c7b15dab67a1`. First compile failed on a namespace collision and remains preserved in the unnumbered output directory; only three System.Environment qualifications were needed. No tests or library load occurred in that failed attempt. This is an own platform profile, not verification of the original game's loaded CRT;05E160/591A10 integration is still pending. The control word was queried with zero write mask, not changed.

The saved-ground pipeline is now compiled and tested:129x129 owned samples -> explicit disk1DE880 scalar dispatch ->15x15 descriptor blocks -> actual1D0FC0 bounds;8 new scenarios/55 assertions, two compilations without diagnostics, old suites0. Result `.tooling/r1-ground-geometry-validation/result.json`, SHA256 `b2732e8832be58b5b2856789fab21ef6b4b22fc67bb435e940d46a241d0a2ecd`. DLL SHA256 `61f523091f69bddbe510b64fbd6134daf0843efe919844a95d6d0d399ab77ed1`. Independent review corrected pointer-capture versus dereference timing before the first build. Materials remain unresolved; gEngine+640 remains unknown. No native Load, ray/floor collision or gameplay success is claimed.

1DF220 pseudo/assembly/callees were obtained and preserved. It builds two triangles per requested grid cell and delegates plane construction to044660 and face intersection to1CF830, updating distance/X/Z in order. The next1DE850 request failed because Rea could not resolve the function and interpreted the numeric VA as a file offset. It was not an authorization failure. All backend/session/Job/sampler cleanup checks passed; Engine hash remained unchanged. Remaining work is a bounded leaf-window inspection and corrected fixed query set, without repeating1DF220.

## Turn-to-publication integration, 2026-10-23 05:38 UTC

Actual05E160 branches and591A10 now feed the existing matrix/World/Level/BBox/spatial implementations using the same Character/Player Entity and gEngine cell.26 new scenarios/719 assertions passed, including both limited-turn directions, unequal-rate interpolation, signed ticks, native unordered early returns, real nonempty spatial effects and partial state on Unknown failures. Two compilations emitted no diagnostics; old suites0. Result `.tooling/r1-game-rotation-publication-validation/result.json`, SHA256 `5f736143cb7fc317d5f8f3db51a7595b337db91a9cacd808e5a95570f17b23e6`; DLL SHA256 `52133b2a65f04d1bb21599337cff013c9a46d57bc58bafb9a55d920beccdfd66`. Native AL is preserved and is not labeled movement success. Input positions/mesh in this fixture are explicit own state: actual Crowd tick, floor production and native mesh producer are not covered.

The seven remaining ground entries returned26/26 with clean shutdown and stable Engine hash; summary `r1-ground-intersection-remaining/summary.json`, SHA256 `07f285ca456414b7c5c460291edebc4528b94a1e347bfbdbbf4c23b13019e883`.112 saved code bytes separately established two tiny disk entrypoints; their manual decode is explicitly distinct from Rea.11 exact tick constants were read in44 bytes. Two12-byte global ranges have no disk backing and remain Unknown; zero was not inferred. The current four-entry query is limited to the newly confirmed triangle/face and ground list dependencies. No old successful queries are repeated.

## Real ground geometry and avoidance, 2026-10-23 06:06 UTC

The four-entry ground batch completed17/17 and closed its session, process, Job and sampler, with unchanged original hash. Summary `r1-ground-face-and-unlink/summary.json`, SHA256 `556d843ff2bbf7851f31be0107fbbfa5c37bb8efa46f3100af3de25c560dee19`. This resolved the direct plane/inside-face and vector/unlink bodies; it is evidence acquisition, not game acceptance.

The saved129x129 ground cells now feed the actual two-triangle topology, normalized plane, ray-plane, projected inside-face and back-face checks. Ground bounds use the observed candidate-plane ray-box algorithm. The new10 triangle scenarios/51 assertions include saved-map quads and a nonplanar fixture whose triangle height differs from bilinear sampling;8 ray-bounds scenarios/33 assertions cover inside/parallel/edge/unknown paths. Result `.tooling/r1-ground-triangle-intersection-validation-2/result.json`, SHA256 `47da03a253746f5308a994ccae0bf8fb89105e617e0e89681b503b1f27c46326`. Three compilations had no diagnostics. The first attempt failed compilation on two own AABB integer address constants; no test ran then, its logs remain in the unnumbered directory, and only the uint suffixes changed. No previous suite was rerun. This is an end-to-end saved-ground **geometric ray intersection component**, not complete Block/World floor collision.

Actual packed material membership also runs: all225 saved blocks have nonempty candidates,543 memberships total, checked against an independent union of serialized bitmap bytes. The normal Begin-insertion/growth path uses one vector and the same Ground/Block records; records and weights are borrowed rather than copied for future intersection. New12 scenarios/1555 assertions passed, including bounded failure prefixes and record-identity checks. Result `.tooling/r1-ground-material-candidates-validation/result.json`, SHA256 `634931e218ceda58adb905a5cc0efd51ec502cfa5bb3f3b3897afcabb05e6969`. The geometry library was recompiled only to add four read-only accessors; the old8 suite was not rerun. These are candidate indices, **not resolved material resources**. Material pointers remain Unknown. Default-empty lists cannot represent this real map.

Three fixed Engine thunks were independently bound to atan2f/cosf/sinf with18 code bytes and122 import-metadata bytes; original Engine hash/stat stayed stable and the file closed. Evidence `r1-engine-crowd-math-imports/inspection.json`, SHA256 `f0fa298bf90996afdd7fb04a898a7c037827d3c858b33fb4515c19d8db5e4a02`. The shared current-host UCRT loader now exposes a separate concrete Crowd view while retaining the old rotation bounds. New11 scenarios/172 assertions and45 real float calls passed; the unchanged11/96 rotation-platform suite was recompiled/rerun because its shared loader changed (33 calls). Result `.tooling/r1-system-ucrt-crowd-validation/result.json`, SHA256 `ab3431a1ffb442a9349730e81965138a7a98935a2f0efc5c33770ddd74064297`. No old26 rotation-composition suite was rerun. FP control was queried with zero write mask, never changed; this remains an own platform profile, not original loaded-runtime verification.

Complete25EF20 scoring and25F250 preprocessing/22-candidate three-refinement selection now consume the same Group.Vector80 records and this concrete math view. New29 scenarios/121 assertions passed,345 real math calls, with unchanged queried control word. Result `.tooling/r1-crowd-avoidance-validation/result.json`, SHA256 `94e0dbd32594cb27b59edc4080889430376c426d98e973fd77308fd9ded66fbc`. Unknown BSS fallback is preserved and fails only when actually consumed. Old suites0. This produces an avoidance velocity from explicit own inputs; it does not execute25C440 or move a character.

The mouse-to-real-ground movement loop remains incomplete. The immediate integration work is: bind Work40/neighbor88 authoritative headers to real neighbor production and25C440, finish Ground Block1D1330/material-resource identity and its tree query, then feed actual floor output into066EE0/rotation/publication (including its still-unclosed533410 suffix). Existing LevelLoad evidence also identifies the saved24-byte chunk9 as Water input, so Water cannot silently be set null. A bounded material/water query is being prepared from those exact calls. Physical mouse-to-world coordinates, production mesh and rendered scene presentation remain separate unfinished links. No original runtime, original save, security change, commit/push, R1 game acceptance or initially usable reversed program is claimed.

## Ground resource boundary and UE resource handoff, 2026-10-23 06:23 UTC

The Block1D1330 composition passed8 new scenarios/49 assertions, two clean compilations and no old suite rerun. It uses the actual saved samples, candidate vector and weight planes. Three real sample rays reach the material-pointer TEST1D1528 with a computed distance; unresolved identity correctly stops before XYZ publication. Result: `.tooling/r1-ground-block-intersection-validation/result.json`, SHA256 `e1fc057812c6e78678095f11e6e5da09e9a57dc8ddfba906df12230f36d1a7b7`; library SHA256 `798b5fd57278e551cead653eef1ad368a19c7c75812f705f0de880f06f04c7b9`. Subsequent source review identified an overly narrow own behavior: once a valid non-null material view exists,1D152D/1D1533 only copy its raw+FC DWORD. Unknown bits should propagate to the result rather than stop before XYZ. That precise correction and affected tests are being prepared, not yet built; the historical8/49 result applies only to its pinned source. Unknown material pointers still stop at TEST.

Rea/Ghidra returned genuine nonempty pseudocode, assembly and callees for1CE350/12C590/1F81E0/1F9BD0 (17/17 calls). Summary: `r1-ground-resources-and-water/summary.json`, SHA256 `4b1442d60a79bc05c1fb29969ae257a2aea574763a8d5716a7a956fee748a72e`.1CE350 calls17DEA0 CreateObjectFromFile(name,0,true), checks the returned dynamic class against TerrainType41CD20, and destroys/logs a wrong-class object before returning null. It does not establish the real material allocation or+FC producer by itself. Existing M1 inspect-record/ArzRawRecordReader can be reused for the five named terrain records; this batch did not inspect those original database records or infer their fields.

Saved mainmenu chunk9 has the exact24 bytes `a9cbedfe0200000000000000000000000000000000000000`: magicFEEDCBA9,version2,type count0,layer count0,path count0,wave count0.1F81E0 requires both10B0 and10B8 non-null before consuming this header. With established prerequisites, the actual empty payload skips resource/layer/path/wave creation and returns true; this is evidence of empty data, not a null Water object.1F9BD0 initializes distance to+Inf and XYZ to+0, leaves material untouched, and returns false for those zero counts.12C590 first replaces old Water, then calls constructor1F6A90 and Initialize1F6E80; their initialization is still a direct unclosed dependency. Any own initial-state adapter must be labeled as such.

At06:16:51Z the batch closed its session/process and confirmed Job empty plus sampler exit. Original Engine SHA256 before/after is unchanged `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`. The parent then reserved the heavy-execution window for OutOfWindow UE. There is no pending Rea call or build. No next backend/build/test is to start until the parent releases that window; saved-evidence review and lightweight source work continue. This is resource coordination, not an approval denial or a failed test. A sandbox CIM read failed earlier; its emitted zero was invalid and never treated as a resource measurement. The authorized runner subsequently measured9.038GiB available and passed its unchanged launch/mutex guards.

Still missing for one actual mouse-target move: authoritative25C440 work/neighbor storage and position-producing tick; valid real terrain material identities plus Ground tree/World-floor integration; Water initialization, real Level actor/neighbor state and the066EE0→533410 suffix; physical mouse hit-to-world coordinates and real scene rendering. The passed chains are separate components: real navigation to waypoints, saved-ground geometric intersection, and own coordinate/rotation-to-publication fixtures. They do not yet form an end-to-end mouse/game loop. No R1 gameplay acceptance or initially usable reverse-client program is claimed.

At the final clean checkpoint, lightweight sources are frozen but unbuilt: Block FC propagation correction (8 affected scenarios+1 new alias case), Ground tree (7 new), borrowed Crowd headers (18 new+7 Growth+7 Proximity affected regressions), and actual empty Water draft (20 proposed). The Block and Crowd changes received independent read-only review; tree insertion/refinement/unlink and query were statically checked. None of these reviews is a build/test pass. No next Rea runner was prepared or dispatched for Water initializers.

## Resumed ground/movement integration, 2026-10-23 06:50 UTC

The parent released the UE resource window. A live preflight at06:36:57Z found14.16GiB physical available and no conflicting build/Java processes; each Rea launch still checks the original5GiB admission threshold. Four saved changes were built serially and passed: Block opaqueFC copy9scenarios/52assertions (8affected+1new), authoritative Crowd headers18/117new with Growth7/88 and Proximity7/83 affected regressions, Ground tree7/2430new, and real empty Water20/86new. No unrelated historical suite was repeated. The tree uses225 saved-map block identities,312 nodes, depth6, and preserves unknown material stops. Water uses the actual24B saved payload but still has explicitly own initialization prerequisites. All build logs and exact DLL/source pins remain in the corresponding .tooling validation directories and checkpoint.

Rea/Ghidra returned nonempty pseudocode, assembly and callees for Water1F6A90/1F6E80 (11/11), then closed session/process/Job/sampler normally. Original Engine hash stayed unchanged. Summary docs/reverse-client/r1-water-initialization/summary.json, SHA256 d8d743a85a277c989d2977a3c7228e2fa0da096b002f3c7e0d53b3be36e175dd. Initialize writes borrowed Terrain/Grid, requires gEngine+1F8 and its+58 RenderDevice, publishes10B8, and calls further render/geometry initialization before valid10B0. An empty payload therefore does not prove that Water can be null or initialized without the render-device branch. Constructor203940 and those direct initializers remain explicit; no renderer success is synthesized.

A bounded2386B export-metadata lookup found default TerrainType constructor1E0F10 (no containing pdata found) and Load1E1940 (exact pdata entry), plus an unneeded copy constructor that will not be followed. Five exact terrain records from the pinned base database.arz decoded successfully with unchanged archive hash and closed read-only handles. All classes areTerrainType; surfaceType strings areStone/Grass/Dirt/Dirt/Dirt in saved-map order. They do not yet prove the runtime numeric+FC field, object identity, or DLC precedence. Rea queries for only the direct constructor/Load dependencies are being prepared.

One mouse-target move is still incomplete: real material allocation/field binding, Water and Level/Region cold state, actual Crowd position production through066EE0, physical hit-to-world coordinates and real scene presentation remain unfinished. Existing navigation, geometric collision and position-publication components have not yet been joined into a working game loop. No original runtime, saves, security settings, commit or push was touched.

## Clean resource handoff, 2026-10-23T07:05:37.455Z

TerrainType batch completed9/9 with genuine Load pseudocode/assembly/callees and default-constructor assembly throughRET1E10A0. Session,process,Job,sampler closed normally; Engine hash unchanged. The exact byte/length surface comparisons establishUnwalkable0,Grass1,Stone5,Water7. Six other case strings require only23 new data bytes; their fixed plan is saved, reader/adapter not yet written. Full shader loading is not a prerequisite for a separately labeled own collision-only record adapter, but no unresolved field is defaulted. The12B World-floor operand read also passed with unchanged Engine hash.

The parent reserved the next heavy window for two OW comparisons. No Rea/build/test is running or pending; no authorization was denied. This turn validated46new scenarios plus22affected regression scenarios, with zero unrelated historical suites repeated. New Crowd production(22component+1real-resource planned), WorldFloor, and Water-prefix sources are unbuilt. Game066EE0 composition is only analyzed; the newly confirmed Engine22C690 coordinate-conversion predecessor and Game533410 suffix are not implemented. See the small hash-only reference manifest `docs/reverse-client/r1-ground-movement-resource-handoff-20261023T0702Z.json` and current checkpoint; no old evidence trees were copied. Resume only after the parent releases the resource window. No complete mouse/scene/game loop or initial playable reverse client is claimed.

### 2026-10-23 light-work resume and measured serial validation

GD is not a UE project or dependent on OW. Source work and the fixed23B read continued while UE was present. After live process checks showed UE/build/Java absent, three reviewed fixed builds ran sequentially: Crowd23 scenarios/95 assertions, WorldFloor21/135, and real Material→Block/Tree8/384; no old suites rerun. Durations and resource snapshots are in `r1-ground-movement-light-work-20261023.json`; process peak RAM was not measured. Rea retains its actual external-process mutex and5GiB gate; blanket-pausing all C# compilation was scheduling, not a native requirement.

Saved real9-tile navigation now changes the same Agent Position48 and projects a nonzero nav reference, stopping25E087. Actual5-record materials now give a real Ground tree hit. WorldFloor passes separately with explicitOWN state; real-map Floor integration and Game callback wiring remain pending, as do physical mouse input and rendering. These results are not game acceptance.

The next6 integration scenarios/120 assertions passed in0.940s, using the same real saved Ground/5 material objects through the actual WorldFloor core and wrapper. HitY16.0587463 agrees with independent triangle16.0587477684021, and differs from bilinear16.057873368263245; no nav height is used. Unknown actors13028A, neighbors221F2E and live material1D1528 remain exact stops. Actor/neighbor contents andWater prerequisites are explicitly OWN: this is saved-ground collision integration, not native scene startup, Game066EE0 wiring or mouse gameplay. Result: `.tooling/r1-real-ground-floor-integration-validation/result.json`.


## 2026-10-23 08:48 UTC: saved terrain publication and bounded repeated movement

- World-position initialization: 9 new scenarios / 88 assertions; saved 22C690 order only, no claim of complete native Level startup. Receipt: .tooling/r1-world-position-initialization-validation/result.json.
- Actual saved navigation to Game Moved, real Ground Floor, rotation and Entity position: 22 new / 373 total. First build passed 21 components, then stopped at a missing managed MainmenuTilePipeline dependency. The exact missing previously passed assembly was added to an isolated repair folder; only the previously failed saved sample ran again (1 / 64), with no recompilation or old test rerun. Receipt: docs/reverse-client/r1-game-moved-integration-result-20261023.json.
- Camera getter → original image projection/normalization → real saved Ground tree ray: 23 new / 256; 11 affected existing SystemUcrtRotationMath scenarios / 96 rerun for the shared loader change. Receipt: .tooling/r1-camera-world-ray-validation/result.json (SHA256 927e66c82ec1e7c797cee446380922bbfd031ea5355a853515baec6e7775e52d).
- Original Created flags0x481, speed2, minimum1.1 and limit400 retained through actual ready prefix → producer → Game callback/Floor/publication → tail, for two frames sharing one Agent/Character/Work allocation: 32 new / 309, zero old suites. Receipt: .tooling/r1-crowd-ready-tick-validation/result.json (SHA256 c66bd4047d3aa7c33e34f65d9792b67af2e4ce5231873f06823ad5d362b3efef).

The continuous sample moved XZ in both frames; published real Ground Y was17.2317123 then17.2245941, distinct from navigation Y22.1706848 then22.157896. Work was cleared only by the actual tail. Engine frame counts100/101 and prior-frame gates99/100 are explicit OWN inputs; no native frame-lifecycle producer was restored. One ready agent, no replan, empty neighbor/actor/skill domains, finite arithmetic/platform math and supported material/tree domains remain explicit prerequisites. Goal and stuck notifications stop at their real unimplemented callbacks. Physical mouse events, native pick caller, cold scene startup, rendering/HUD and playable acceptance remain false.

Small serial builds took about0.5–3.7seconds in the recorded tool receipts; peak build memory was not measured. At08:45:28Z the read-only snapshot showed15416090624B physical available,27276091392/52369920000B system commit and no external build/Java conflicts. No UE wait is required solely because these are compilations; Rea still uses unchanged5GiB launch/2GiB stop and process ownership/mutex protections. These durations do not establish a safe concurrent UE peak budget.

Next direct evidence is limited to arrival05FC30/019D40/01C040/0EC010 and229700 caller attribution; it does not imply a usable mouse-controlled client yet.
