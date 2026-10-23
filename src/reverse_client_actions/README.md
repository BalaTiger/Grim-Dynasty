# Evidence-backed action submission contracts

This reusable C# library implements the observed control flow and shared mutable
state of Game `127E40` (local submission), `1600F0` (permission decision), `089AC0`
(execution), `089BF0` (stop), `073BA0` (queue clear), and the proven portions of
`081870` and `07A6D0`. It is an isolated component for the corrected reverse
client route, not a running client, movement simulator, or an R1 acceptance.

Original evidence: `docs/reverse-client/r1-path-local-dispatch`,
`r1-action-executor`, `r1-action-support`, and `r1-navigation-binding`.
Each retained Rea response includes the tool method and exact original address.
The Game/Engine originals were read as data only, with before/after hashes.

## Supported behavior

- Conditional local submission, permission values 0/1/2/4 and other values;
  permission3 is resolved inside CheckAction through the current action.
- Repeated virtual calls, signed permission indexing, late engine/handler/current
  pointer reads, borrowed identity, and callbacks that change shared state.
- A known zero pre-action byte returns null. Unknown is not zero.
- Execution budget, stop/replacement, reentrant queue publication, ordered unlink,
  null queue payloads, and node-only cleanup after budget exhaustion.
- Missing fields or dependencies stop explicitly; callback failures retain the
  already performed writes. Native faults and exception cleanup are not emulated.

`Boundaries` contains the actual missing dependencies, not default-success
implementations. The managed `ReleaseNode` is a self-owned storage adaptation.
`AllocateLinks` still requires the unread `079CD0` contract; tests that inject a
node allocator are explicitly fixtures, not native allocator evidence.
The `07A6D0` invalid-pointer wraparound special case is outside the valid managed
node domain. Fields retain observed widths; this is not a native ABI layout.

## Remaining client dependencies

- Permission matrix initialization and valid action type producers.
- Skill lookup/type validation and concrete `+5A8` running-state behavior.
- Nonzero pre-action lookup, coordinates, allocation and constructor path.
- Concrete dependencies of the now-inspected MoveToAction `+8` begin path.
- Exact navigation binding is now Game `609F38` -> Engine `150590` -> `10C6C0`;
  the latter search body, navigation data and motion integration are still unknown.
- Physical mouse input, world-coordinate production, scene rendering and UI.

No original game, saves or Steam cloud are used by these fixtures. They do not
test loaded native behavior. Tests contain injected callbacks to stress ordering,
not to claim that an actual character moved or an original resource was rendered.

## Validation

The reviewed build entry is `tools/static_analysis/build_action_contracts.ps1`.
It compiles only these four source files using the installed .NET Framework C#
compiler, then runs only `ActionContractTests.exe`. It refuses to overwrite a
previous validation directory. Current execution results are retained in
`.tooling/r1-action-contract-validation-attempt4/result.json` and `tests.log`.
Final result: **45 scenarios,193 assertions**, both compilations and tests exit0.
The DLL and test executable are in that same directory. No historical suite was
rerun. See the stage report for preserved compile failures and the one necessary
rerun after the borrowed-slot interface correction.

## MoveTo start extension (2026-10-22)

`MoveActionState`, `MoveBoundaries` and `MoveContracts` add the recovered branch
and write order of MoveToAction start082C00/stop082EF0, Character.MoveTo05F870,
SetCurrentAttackTarget05E670, StopCurrentSkill534540 and scalar getter0819E0.
Its disk vtable binding is confirmed. Its production dependencies are incomplete.

The shared action/role/handler views are the same identities used by Executor.
Unknown fields remain unknown. Position copies support shared 8-byte cells and
the observed alternating16+8 order; arbitrary byte/DWORD overlap is unsupported.
Fixed LEA class-symbol identities bind once; mutable parent links are distinct
from those identities. Explicit own-adapter release observations stop subsequent
action field use. This does not detect native frees, reuse/ABA or asynchronous
threads. Virtual bindings are for the observed concrete MoveToAction class;
runtime vtable replacement is outside the supported domain.

`MoveBoundaries` supplies **no successful navigation, coordinate, lookup, skill,
animation or sound defaults**. Its test overrides are observations used to check
branches and failure prefixes. A missing dependency's `Outcome.Address` is its
named dependency address; an unobserved field or callback exception uses the
instruction address. It is not a universal native program counter. Native EH,
MXCSR traps/status, original memory layout and loaded-state compatibility are
not claimed. The 05F870 incoming XMM2 scalar is read by the caller but unused in
the inspected callee; it must not become an invented arrival radius.

Targeted validation uses `tools/static_analysis/build_move_contracts.ps1` and
`.tooling/r1-move-contract-validation/result.json`. The build compiles the five
library inputs plus the new test source, without including the older45 tests.
The actual result is35 new scenarios/180 assertions, both compilations and tests
exit0. This does not rerun the older45 scenarios.

`MoveManagerContracts.cs` adds the inspected08E170 arithmetic and08ED00/05D9A0
stop/position-correction contracts. `MoveManagerBoundaries` implements these three
methods but keeps position differences, world/navigation imports and08EAF0
movement production explicit. It is compiled as a small extension referencing
the frozen core DLL. Its separate15 scenarios/86 assertions passed in
`.tooling/r1-move-manager-validation`, without rerunning the earlier35 or45.
See [the complete stage result](../../docs/reverse-client/R1-MOVE-START-RESULT.md)
for source evidence, commands and the remaining queue/portal/navigation gap.

`PortalSelection.cs`, `WaypointState.cs` and `WaypointContracts.cs` now add
08DF30,073F70,08F340,074110,08F4C0 and the ordinary08D9E0/no-portal08EAF0 paths.
They share the original manager identity and preserve unknown cells, returned
pointer distinctions, live field reads and consumed failure prefixes. The new
DLL and two test programs passed39 scenarios/268 assertions on this machine;
the previous45/35/15 suites were not rerun. Explicit own storage/import fixtures
are not actual navigation. That frozen build leaves portal kind1/three-record
production unsupported. See [waypoint result](../../docs/reverse-client/R1-WAYPOINT-RESULT.md).

`PortalTransitionState.cs`, `PortalFrameMath.cs` and `PortalTransitionContracts.cs`
extend the same state with the observed three-record producer and kind1 frame,
player/camera decision and recursive next-waypoint path. Shared hidden buffers,
exact write widths, callback-visible reads and failure prefixes are preserved.
The first targeted build passed23 new scenarios/211 assertions; two cover the
base hook interface, without rerunning the older39/45/35/15 suites. The frozen
artifact is `.tooling/r1-portal-transition-validation/GrimDynasty.PortalTransitions.dll`.
Imports and test observations remain distinct from actual navigation, camera
construction, physical input and scene presentation. See the
[portal result](../../docs/reverse-client/R1-PORTAL-TRANSITION-RESULT.md).

`PortalCameraContracts.cs` implements the observed base yaw write, the Game yaw
wrapper and raw Engine GetCoords output in an explicitly attached camera view.
The output keeps DWORD10 and DWORD14 separate: native writes leave DWORD14
untouched. Projection into the older QWORD view remains Unsupported when either
half is unknown. The Game GetTarget body is now evidenced but its region,
translation and offset dependencies are not implemented; `CameraFrame609F00`
still has no successful default. Camera construction is also not recovered.

The separate first-pass build passed26 new scenarios/201 assertions in
`.tooling/r1-portal-camera-validation`, referencing the frozen portal DLL. It did
not rerun the23 portal tests or older suites. Both stages total49 new scenarios
and412 assertions; their mock imports are not actual movement or scene evidence.
The executed build recipe is
`tools/static_analysis/build_portal_camera_contracts.ps1`. Its fixed output
directory is already occupied and will not be overwritten; any future rebuild
needs a separately reviewed fresh output destination.

`CameraTargetContracts.cs` and `EvidenceCameraBoundaries.cs` now replace the
missing Game GetTarget dependency for the observed concrete camera class with
the saved wrapper and three real Engine leaf contracts. The portal-only bridge
shares basis cells and keeps unknown padding in a fine-grained raw sidecar;
coarse position fields are not a general complete coordinate projection.
The first new build passed28 scenarios/273 assertions without old-suite reruns.
The production camera chain is conditional on explicitly supplied valid views
and initialized fields; it is not camera construction or native gameplay.
See [camera target result](../../docs/reverse-client/R1-CAMERA-TARGET-RESULT.md)
and `.tooling/r1-camera-target-validation/GrimDynasty.CameraTarget.dll`.

`NavigationCoreContracts.cs` adds reference validation, equal-reference corridor
production and the single-polygon straight-output path, including recovered
XZ polygon geometry, endpoint projection with Y interpolation, append and
equality. Explicit own mesh/reference inputs and constants are required. The
default geometry uses the inspected Engine bodies; wrapper Mock tests are
separately labelled. Unknown fields, released views and unimplemented multi-
polygon search/funnel do not become synthetic success.

The first targeted build passed34 new scenarios/275 assertions with zero
warnings or retries. It compiles only the two new sources against the frozen
Actions DLL; older suites were not rerun. Artifact:
`.tooling/r1-navigation-core-validation/GrimDynasty.NavigationCore.dll`.
The retained `NavigationCoreTests.exe` exercises this bounded component. The
build recipe `tools/static_analysis/build_navigation_core_contracts.ps1`
refuses to overwrite its occupied output directory. Actual nearest-reference
production, navigation data/query initialization, mouse target and scene/render
are not recovered by these tests. See
[navigation core result](../../docs/reverse-client/R1-NAVIGATION-CORE-RESULT.md).

`NavigationSpatialState.cs`, `NavigationEnumerationContracts.cs` and
`NavigationNearestContracts.cs` add actual linear/BV candidate production,
closest detail height and nearest selection on explicitly supplied mutable
mesh views. They preserve core identities, stable ties, NaN branches, output
alias effects, late reads and partial failure states. The producer connects
to the earlier same-poly corridor/straight path without fabricated references.

The first isolated build passed27 new scenarios/286 assertions with no warnings
or retries.20 scenarios use default leaves/own data domains;7 explicitly label
Mock observations. Older suites were not rerun. Retained artifact:
`.tooling/r1-navigation-nearest-validation/GrimDynasty.NavigationNearest.dll`;
test launcher `NavigationNearestTests.exe` in the same directory. This is a
component executable, not a game. The build recipe refuses occupied output.

Actual Rea construction evidence now traces the manager to mesh/query/filter
initialization. It creates an empty mesh and does not establish loaded scene
tiles; unchecked initialization status is not a valid-state guarantee. Real
data population, cross-poly search, physical input/projection and scene/render
remain unclosed. See [nearest result](../../docs/reverse-client/R1-NAVIGATION-NEAREST-RESULT.md).

`NavigationPathingData.cs` is a strict managed format adapter for the recovered
PTHD version1 header and ordered nested length records. Its borrowed byte views
retain the source array; malformed-input bounds checks are own safety behavior,
not native status semantics. It does not build or publish navigation tiles.
The first isolated build passed14 scenarios/167 assertions with no warnings;
older suites were not rerun. The frozen DLL and result are in
`.tooling/r1-pathing-data-validation/`; `tools/build_r1_pathing_data.ps1`
refuses to overwrite that output. Real resource observations and the next
tile-building dependencies are recorded in
[scene/navigation result](../../docs/reverse-client/R1-SCENE-NAVIGATION-DATA-RESULT.md).

`NavigationLayerCache`, `NavigationLayerData` and `NavigationLayerRegions`
extend actual saved PTHD data through registration, raw decoding and integer
region construction. The first build passed46 new scenarios/1,078 assertions
and a separate real9-layer shared-identity pipeline, with zero warnings and
no old suites rerun. Frozen outputs are under
`.tooling/r1-navigation-layers-validation/`; run `MainmenuLayerPipeline.exe
--climb 4` there for the fixed saved sample. Climb is an explicit own experiment
in the evidenced default configuration domain; loaded runtime settings were
not observed. Source hashes are checked, and no original file or save is read.
This is a component tool, not a game: contours, polygon mesh serialization,
publication, cross-polygon navigation, input projection and rendering remain.

The next frozen outputs are `.tooling/r1-contours-and-tiledata-validation/`:
`MainmenuContourProbe.exe` processes the same saved sample into88 valid contours
and602 vertices, with zero0x80 markers under explicit climb4/tolerance1.5.
The two new components and sample probe passed42 scenarios/915 assertions
without warnings. `GrimDynasty.NavigationTileData.dll` has only own-geometry
format tests so far; it has not yet serialized this real sample's polygons.
These are bounded components, not a runnable reversed game.

As of2026-10-22 19:52UTC, the later polygon/tile pipeline passed20 memory
scenarios/238 assertions and produced9 real tiles (419 vertices,245 polygons,
30,940 bytes). The shared mesh pipeline then passed23 scenarios/434 assertions
and actual insertion/lookup/nearest:520 internal links,66 external links,nine
nearest queries. Two test arrays with missing sixth slots were repaired after
an initial failure;18 previously passed mesh scenarios were necessarily rerun.
Production mesh code was unchanged. Unrelated historical suites were not rerun.

From repository root, run:
`& '.\.tooling\r1-real-mesh-validation-repair1\MainmenuMeshPipeline.exe' --explicit-default-domain`

This offline driver reads the saved project member only and prints results;
it is not a playable client. The explicit cold domain and remaining runtime,
allocation/ABI, cross-polygon/funnel, physical input and rendering limitations
are recorded in the scene/navigation result above. Actual serialized bytes,
link owners and query views share identity; no injected path result is used.

The actual corridor driver is now available in
`.tooling/r1-real-corridor-validation/MainmenuCorridorPipeline.exe` with
`--explicit-default-domain`.20 new scenarios/181 assertions and two actual
31-polygon,3-tile-transition searches passed without compiler warnings.
This adds corridor generation to the same saved-data/mesh/nearest chain;
waypoint generation, physical input, movement and rendering are still separate.
See the scene/navigation result for the explicit configuration and failure-domain
limits. No old test executable was rerun in that corridor validation.

Real resource waypoint production is now available from the repository root:
`& '.\.tooling\r1-real-straight-path-validation\MainmenuStraightPathPipeline.exe' --explicit-default-domain`.
The original real mesh feeds nearest, corridor and the recovered multi-polygon
funnel:10 forward and9 reverse waypoints.16 new scenarios/216 assertions and
20 relevant corridor regressions/181 assertions passed with zero warnings.
The executable is an offline probe, creates no save and supplies no game window.
Own XZ sampling validates finite route consistency only; physical input,
projection, actor motion, real rendering and native runtime equivalence remain
separate. See the scene/navigation result and frozen result manifest for exact
support limits, source hashes and reproducible logs.

The recovered crowd query slots can be exercised against those same saved tiles:
`& '.\.tooling\r1-crowd-adapter-validation\MainmenuCrowdAdapterPipeline.exe' --explicit-default-domain`.
The new adapter calls production nearest/corridor code directly;8 new scenarios
and94 assertions passed, plus all9 real tile projections and one cross-tile
corridor. No old test executable was rerun. The flag acknowledges self-owned
cold parameters, preallocated32-reference storage and managed locking; it does
not claim native runtime defaults. This is an offline probe without a window or
save, and does not yet register or move a character. Native partial paths may
return adapter success; the probe separately verifies actual target arrival.

The shared Agent request path has a separate offline probe:
`& '.\.tooling\r1-crowd-state-validation\MainmenuCrowdRequestPipeline.exe' --explicit-default-domain`.
It joins a preallocated own group and sends the embedded Agent request through
the recovered synchronous submission and real map query adapter. Six new
scenarios/119 assertions passed, plus a real two-polygon cross-tile request.
The flag acknowledges explicit cold parameters, typed storage and managed
locking. No window/save is created; Agent position remains unchanged until the
actual movement update and derived character interface are recovered.
