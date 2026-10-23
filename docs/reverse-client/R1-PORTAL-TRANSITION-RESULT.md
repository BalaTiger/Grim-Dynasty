# R1 portal transition component - 2026-10-22

This stage extends the existing shared movement/waypoint component. It is not a
playable client or an R1 acceptance result. Native navigation, physical mouse
coordinates and real scene presentation remain unresolved. No original program
or real save is executed or modified.

## Recovered control and data flow

The existing Rea bodies for Game08EAF0 and08D9E0 now support three-waypoint
production and the kind1 transition branch in the same mutable manager state.
The producer reuses the exact hidden position passed to OwnerCoords08EB23 for
the first portal choke query; that query's return pointer is ignored. The second
choke uses a separate hidden buffer and its returned pointer. Three position
constructors precede records of kinds0,1,0, which are appended in that order.
The matrix import's separately zeroed scratch is not the waypoint's unobserved
Auxiliary48 field. A later failure retains any earlier appends.

The owner frame is60 bytes:24 bytes of position plus nine float32 basis cells.
Its transform follows the actual instruction order, including the Owner00 read
at08DC51 between partial frame writes. Player resolution preserves object
identity. The main-player query rereads the connection manager and obtains the
second ID separately; it does not cache the first lookup result.

The camera branch reads the Game global three times. It captures the setter
receiver and then the vtable identity, but loads virtual slot10 only after
atan2f and the float32 pi subtraction. Null and unknown fields stop at their
observed consumption points; callback failures retain the prefix. The world
frame update precedes optional navigation notification, the moving flag reset,
and recursive consumption of the next waypoint. A false recursive result does
not undo a completed portal transition.

## Additional original evidence

- GameEngine's current12-byte call anchor confirms inline camera+13B0 calling
  Game2BE3B0. See `r1-game-camera-anchor/inspection.json`.
- Rea `procedure_pseudo_code`, `procedure_assembly`, `procedure_callees` on
  Game2BE3B0 completed8/8 including lifecycle at14:32:45-14:35:50UTC, with clean
  session/process/Job/sampler shutdown and matching before/after Game hashes.
  The constructor writes vtable767938. Its base construction and later reset
  dependencies are observed, not implemented or presumed complete.
- Exact8-byte read of vtable+10 at767948 binds it to Game2BFAF0. This is a disk
  binding, not proof of a live instance or an immutable loaded vtable.
- Rea body/assembly/callees for2BFAF0 completed8/8 at14:44:40-14:47:37UTC
  with normal cleanup and stable Game hash. It is GameCamera::SetCameraYaw:
  first call60A6E8, then write the preserved incoming float to+59C. Failure
  before the base call returns must not publish the+59C value.
- Game609F00 binds by the selected Engine import descriptor and exact exported
  name to Engine229160, `WorldCamera::GetCoords`. See
  `r1-camera-coordinates-binding/inspection.json`. It does not prove loaded IAT
  state or uniqueness against overlapping descriptors.
- The selected60A6E8 import resolves to Engine0825C0. Reused OFT entries0..487
  plus a bounded1,968-byte continuation establish this descriptor's continuity
  through entry733. Total new metadata reads2,070 bytes; both originals retain
  their hashes. Other descriptors were not scanned for overlap.
- Engine229160 and0825C0 real pseudocode/assembly/callees completed11/11 at
  14:56:27-14:57:53UTC, normal cleanup and stable Engine hash.0825C0 writes only
  the float at+0C.229160 calls virtual+18 and uses the returned pointer's first
  QWORD; then it rereads camera+94/+9C and basis+70..+90. Output+14..+17 is not
  written. Preserving this hole requires a finer output representation than the
  previously frozen whole-QWORD position view.
- The Engine batch's immutable plan retains an obsolete `evidence_questions`
  prose field from its template. Its actual fixed call list and all returned
  targets are only229160/0825C0; old input/navigation/UI entries were not queried.
  The preparation command also encountered a PowerShell alias collision before
  any execution. The incomplete edit was inspected and corrected; offline
  verification passed on the complete pinned runner before the batch started.
- Existing `r1-portal-camera-data/inspection.json` proves the selected CRT
  atan2f binding and pi bits40490FDB. Exact native MXCSR/CRT behavior remains
  outside the managed component.
- The+18 disk slot is Game767950 ->2BE6E0. Its actual Rea body is
  `GameCamera::GetTarget`, not a simple field getter. The8-call batch completed
  15:03:16-15:06:13UTC with normal cleanup and unchanged Game hash. It invokes
  WorldVec3 construction609720, two region checks6094A0, conditional in-region
  translations609B70 and camera offset60A690. It returns its supplied output
  identity. These dependencies are recorded, not replaced with a default target.

## Validation state

Source review has checked the shared hidden buffer, scalar operation order,
callback-visible rereads and failure prefixes against the saved Rea assembly.
First build/test completed14:49:38-14:49:39UTC:23 new scenarios/211 assertions,
library compilation, test compilation and execution all exit0. No failed attempt
or retry. Results and logs: `.tooling/r1-portal-transition-validation/result.json`
and `tests.log`. The library SHA256 is
`35d69c4f8e9cd6d81d5181f19f3d236766cdc3afa376e109d33a7a53e3c9ba07`.
Prior39 waypoint tests and earlier45/35/15 suites are preserved, not repeated.
Two of the new scenarios are necessary regressions for the base hook changes.

Approved command executed:

```powershell
& 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' -NoProfile -NonInteractive -File tools/static_analysis/build_portal_transition_contracts.ps1
```

It compiles six component sources and one test source against the frozen core
and manager DLLs. It only writes its new validation directory and refuses to
overwrite it. Original files are not read by this build.

The separate camera extension now implements Engine0825C0 and Game2BFAF0 in
one explicitly attached shared state: base+0C is written, then Game+59C. It also
implements raw Engine229160 using a finer output view. D10 and D14 are separate;
all native writes complete while D14 remains untouched. A separate own projection
to the frozen QWORD view rejects unknown halves at own boundary0, without
misreporting that as a native failure or skipping later native basis writes.
The virtual+18 result must be explicitly supplied. It is not a recovered
GameCamera constructor or GetTarget implementation. `CameraFrame609F00` therefore
still defaults to Unsupported in the main portal adapter.

First camera build completed15:10:08-15:10:09UTC and passed26 new scenarios/201
assertions, both compilations and execution exit0.
Result: `.tooling/r1-portal-camera-validation/result.json`.
The camera library SHA256 is
`4ef08fb844c729aaaaddfde616eec9618d495074d845354e3fab66cf3c86a323`.
It references the frozen passed portal DLL and compiles only the two new camera
files. No prior suite, including the23 portal scenarios, was rerun.

```powershell
& 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' -NoProfile -NonInteractive -File tools/static_analysis/build_portal_camera_contracts.ps1
```

This stage totals49 new scenarios/412 assertions and35 real Rea calls across
five function targets, with no query retry or repeated old function batch.
These numbers describe component coverage, not gameplay completion.

The new tests explicitly use mock imports and storage. Remaining native import
bodies keep Unsupported boundaries; no successful navigation or scene behavior
is supplied by default. Memory aliases finer than the represented cells,
native exception cleanup, concurrent writes and special floating-point modes
remain outside scope.

Current resumable state is `.tooling/r1-portal-transition-prepared/progress.json`.
The main checkpoint and manifest are updated by `seal_portal_checkpoint.ps1` only
after verifying both build outputs, source hashes and all backend cleanup.
No evidence tree is copied; earlier artifacts remain in their original locations.

The next direct gap is Game2BE6E0's region/translation/offset dependencies and
the partial-DWORD bridge, using the saved body. Real Engine navigation search,
physical mouse/world-coordinate sources, rendering, resources and scene/UI
integration still prevent R1/R3 gameplay acceptance. No new playable executable
is delivered by this stage and no WASD/self-drawn substitute is introduced.
