# R1 MoveTo start contracts — 2026-10-22

The existing submission/executor component now reaches the actual concrete
MoveToAction start and stop control flow, using the same action/character/handler
identities. This is a reusable, compiled isolated component, **not a character
that moves in an original scene and not R1 gameplay acceptance**.

## Original evidence actually obtained

| Evidence | UTC | Result / bounded targets |
| --- | --- | --- |
| [Start slot](r1-move-start-slot/inspection.json) | 12:53:27 | MoveToAction6E54C0+8 -> Game082C00 |
| [Rea start](r1-move-start/summary.json) | 12:54:51–12:57:59 | 8/8;082C00 pseudo/assembly/callees |
| [Direct slots/constants](r1-move-start-dependency-slots/inspection.json) | 12:59:25 | +18->082EF0,+68->0819E0;100f constant |
| [Rea direct start dependencies](r1-move-start-dependencies/summary.json) | 13:01:45–13:04:47 | 18/18;05F870,05E670,534540,082EF0 and0819E0 |
| [Tolerance constant](r1-move-tolerance/inspection.json) | 13:17:30 | RVA7A7FD8,4bytes `CDCCCC3C`,float32 `0x3CCCCCCD` |
| [Rea movement-manager dependencies](r1-move-manager/summary.json) | 13:21:55–13:25:10 | 17/17;08E170,08EAF0,08ED00,05D9A0 |

All three fixed Rea batches used real `procedure_pseudo_code`,
`procedure_assembly` and `procedure_callees` operations, except the short0819E0
getter which required assembly only. Total43 calls include15 lifecycle calls;
the28 body operations cover10 newly inspected functions. These counts are tool
evidence, not recovered gameplay features. No successful previous query was
repeated. All three batches confirmed normal close, process exit, empty owned
Job and sampler exit, without forced termination.

Original Game.dll remained
`cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`
before/after each original read. The source was opened as data only. The last
batch's sampled combined private-memory peak was1216593920bytes, with the384M
Java heap and existing5GiB launch/2GiB stop protections unchanged. A sampled peak
is not a future memory guarantee.

## Implemented behavior and important differences

- **082C00 start** resolves the owner and class, checks movement/region/gate,
  notifies the associated object, optionally corrects distance, stops the old
  skill, writes the target, invokes Character.MoveTo and then freshly resolves
  the skill. Initial normal failure calls action+18 only; it does not invoke the
  handler's destructive Stop operation. Unknown or exceptional dependencies
  retain the already performed writes and executor busy/publication prefix.
- **05E670 target** writes DWORD flags, copies16bytes then loads/copies8bytes,
  resolves the incoming skill reference, then writes the resulting target ID.
  Unknown data is copied as unknown. Restricted whole-word overlap is modeled;
  general byte/DWORD cross-field aliasing is not.
- **534540 skill stop** clears the old ID only after successful class validation
  and before the skill callback. A callback's republished ID remains, and its
  updated context is read late. Fixed LEA class-symbol addresses cannot be
  replaced during callbacks; parent contents can change.
- **05F870 Character.MoveTo** rereads the movement manager at the observed
  boundaries, handles failed movement and animation independently, and preserves
  the `UCOMISS/JZ` unordered-or-equal animation branch. The caller reads action14,
  but this callee does not use that incoming scalar as a movement radius.
- **082EF0 action stop** captures flags before the first skill-reference query,
  reads the skill context late, conditionally clears the speed byte, selects the
  preferred sound object, stops movement, queries the reference again, and only
  then clears action11. No finally-based cleanup is added. Explicit own-adapter
  action invalidation stops subsequent use; native lifetime/ABA is not detected.
- **08E170 AlreadyThere** uses the coordinate-return pointer and an unresolved
  position-difference import, then performs each scalar arithmetic operation in
  binary32 order. The ordered comparison is tolerance²>=distance²; NaN is false.
- **08ED00/05D9A0** preserve captured manager/late owner identities and failure
  prefixes. Position correction deliberately ignores GetCoords' returned pointer,
  overwrites its hidden buffer after both16/8 source loads, and invokes609F68
  after world repositioning; this is distinct from stopping through609F50.

Source and supported-domain details: [component README](../../src/reverse_client_actions/README.md).
Managed storage, word views, knowledge tracking and binary32 rounding are authored
platform adaptations. Native ABI, EH, MXCSR traps/flags/FTZ/DAZ, asynchronous
threads, loaded vtable mutation and full original-runtime compatibility are not
claimed. Fixture observations do not establish missing imports or navigation.

## Actual verification

[Start-component build](../../.tooling/r1-move-contract-validation/result.json)
passed both compilations and tests with exit0: **35 new scenarios/180 assertions**.
This includes two necessary integrations through the existing Executor. The
older45 scenarios were not rerun. The reviewed test fixture's negative8byte
overlap uses an initial word equal to the preceding flags write; it does not
pretend to model arbitrary DWORD overlap. The first approved build passed.

- Core library: `.tooling/r1-move-contract-validation/GrimDynasty.Actions.dll`
- Test executable: `.tooling/r1-move-contract-validation/MoveContractsTests.exe`
- Build entry: `tools/static_analysis/build_move_contracts.ps1`

[Movement-manager extension validation](../../.tooling/r1-move-manager-validation/result.json)
also passed both compilations and tests with exit0: **15 new scenarios/86 assertions**.
The extension references the frozen validated core DLL; it did not rebuild or
rerun the35-case or older45-case suites. Three targeted integrations use the
real implemented control flow with explicitly supplied unresolved observations.
Both approved builds passed on their first execution, with empty compiler logs.
This stage therefore adds **50 scenarios/266 assertions**, no native gameplay
tests and no reruns of previously passing suites.

- Extension: `.tooling/r1-move-manager-validation/GrimDynasty.MoveManager.dll`
- Extension tests: `.tooling/r1-move-manager-validation/MoveManagerContractsTests.exe`
- Build entry: `tools/static_analysis/build_move_manager_contracts.ps1`

From the repository root, the retained test executables can be run directly:

```powershell
& '.\.tooling\r1-move-contract-validation\MoveContractsTests.exe'
& '.\.tooling\r1-move-manager-validation\MoveManagerContractsTests.exe'
```

These commands are isolated validations, not game launch commands. Build entries
refuse to overwrite an existing result directory; a future rebuild requires an
explicitly reviewed new output destination. No uncompiled source remains in this
stage. The only duplicate core DLL is the extension's required runtime dependency,
checked against the same hash; evidence is sealed by a hash inventory, not copied
into another large checkpoint tree.

## Exact next gap

08EAF0 is now observed, but not implemented as successful motion. It first calls
**073F70** on manager+40, obtains coordinates and calls **08DF30 CheckForPortal**.
The non-portal branch copies the destination and calls PutOnFloor; the portal
branch creates three differently tagged waypoints and obtains portal data.
Both use **08F4C0** queue insertion and finally return **08D9E0 MoveToNextWaypoint**.
The first minimum missing production contract is073F70; the next bounded useful
set is its queue ownership,08F4C0,08DF30 and08D9E0. None may be replaced with an
empty queue, no-portal predicate or successful fixture mover.

079CD0 remains unresolved for reentrant action allocation. Engine10C6C0 search,
navigation data and actual motion updates, physical mouse binding, coordinate
production, original scene/resources, camera/UI integration and gameplay remain
unclosed. No new playable executable, native differential run or R1 acceptance
is reported. No original game, saves/cloud, system security, commit or push was
touched. The checkpoint records an autonomous continuation point; the user's
R3 stop-and-wait condition has not been reached.
