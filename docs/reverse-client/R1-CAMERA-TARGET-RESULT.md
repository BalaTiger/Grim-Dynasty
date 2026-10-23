# Camera target integration — 2026-10-22

The isolated production path now implements Game2BE6E0 GetTarget, its three
Engine leaf dependencies, GetCoords and the portal caller's basis consumption.
It uses observed native contracts, not a successful-target test callback.
This is not a constructed native camera, real navigation or R1 gameplay acceptance.

## Evidence and implementation

`r1-camera-target-bindings/inspection.json` binds Game6094A0/609B70/60A690 to
Engine22C0B0/22C5D0/0716E0 using exact IAT, import name, hint, ordinal and export
address. OFT continuity reuses prior evidence; only372 new metadata bytes were
read, including4 bytes at7A8054 (`0000803f`, float1). No target code was decoded
by this reader. Both Game and Engine before/after hashes match the retained
original fingerprints. Descriptor uniqueness or loaded IAT state is not proved.

`r1-engine-camera-target` contains actual Rea `procedure_pseudo_code`,
`procedure_assembly` and `procedure_callees` responses for these three addresses.
14/14 calls including lifecycle completed15:28:21–15:29:49UTC; session closure,
process exit, empty owned Job and sampler exit all verified. Engine hash stayed
`4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`.

- GetRegion22C0B0 loads Q0 and returns it. Its shared WorldFrustum export label
  does not change the exact WorldVec3 binding.
- TranslateInRegion22C5D0 performs X=oldX+parameterX, then Y=parameterY+oldY,
  then Z=parameterZ+oldZ, with each scalar store before the next operand reads.
  It does not change Q0 or the padding DWORD14.
- GetCameraOffset0716E0 returns the borrowed inline address this+60, without
  reading or initializing the vector. Other export aliases do not supply rules.
- The existing11-byte WorldVec3 constructor proof in
  `docs/object-lifecycle/item-ba8-validation/worldvec3-normal-prefix.json` is
  reused: only Q0=0 and return original this. No repeat query; the failed larger
  neighboring decode remains excluded and is not recast as a successful run.
- Game2BE6E0's existing Rea body is reused. It returns the supplied hidden
  output. The null-region branch stores16 bytes before loading the source tail;
  the other branch captures the tail before two local translations. A second
  region query gates the late signed Count138 read and optional final offset.

`CameraTargetContracts.cs` retains24-byte field knowledge, exact same-address
identity, ordered writes and failure prefixes. Partial/shifted/cross-type aliasing
is explicitly outside this own view. Unknown values do not become zeros.

`EvidenceCameraBoundaries.cs` explicitly attaches camera views and binds the
observed concrete table. It does not install a vptr or supply initialized camera
fields. The hidden Q0 is shared with the GetTarget output; remaining DWORDs live
in an authoritative fine-grained sidecar. Coarse Q8/Q16 carrier fields are not a
full position projection and must not be consumed as such.

The portal adapter shares all nine Basis18 field objects with the raw GetCoords
output. All native writes run before the portal consumes basis6..8. DWORD14 is
untouched, not defaulted. General QWORD projection still stops when either half
is unknown. The portal caller's Position00 is not a complete camera position;
`RawFrameFor` is the authoritative raw view. The owner SetFrame argument is a
different frame, as verified in the saved portal assembly.

## Actual validation

First build15:40:16UTC passed28 new scenarios/273 assertions. Library compile,
test compile and test run exit0. No retry; prior26/23/39/45/35/15 suites were not
rerun. Twenty tests cover the wrapper and actual leaf helpers; eight cover the
new bridge and integration, with owner/class/network/atan/world publication
services explicitly represented by observations. No original program was read
or executed by this build, and no actual character movement was tested.

Executed through formal approval:

```powershell
& 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' -NoProfile -NonInteractive -File tools/static_analysis/build_camera_target_contracts.ps1
```

Result/log: `.tooling/r1-camera-target-validation/result.json`, `tests.log`.
Library: `GrimDynasty.CameraTarget.dll`, SHA256
`d7d4fd499220d92b4ba1335e1cacd6bb62bfa679f53f7588ac02b556175ae3dd`.
Test EXE SHA256:
`0b7e17e26f51efae4ae3f0b49fcd4aeb9d1292de7c3260d3b1b9327329774a4b`.
The fixed build destination refuses overwrite; existing artifacts are preserved.

Native MXCSR traps/status, FTZ/DAZ, NaN payload selection, arbitrary byte aliases,
concurrent native lifetime and loaded runtime compatibility remain unverified.
No native camera constructor/global initialization or scene renderer is supplied.

## Return to the main chain

The next fixed query is Engine10C6C0, already directly tail-called by the saved
NavManager150590 body. Existing Game08E200 path handling is reused; its+70 gate
is a DWORD integer comparison, not a float inference from pseudocode. Physical
mouse/world-target production and actual navigation data remain unresolved.
That query subsequently completed8/8 with normal cleanup and stable Engine hash;
its three actual search dependencies remain unimplemented. See
`R1-NAVIGATION-SEARCH-NEXT.md` and
`.tooling/r1-navigation-search-prepared/progress.json`. No pending query remains.
