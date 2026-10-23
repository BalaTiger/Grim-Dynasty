# R1 waypoint pipeline checkpoint — 2026-10-22

This stage projects observed Game movement contracts into an isolated shared-state
component. It is not a moving game client, a native ABI wrapper, or R1 acceptance.
Original files and real saves are not changed; the original game is not executed.

## Native evidence

- `r1-waypoint-execution/`: fixed Rea `procedure_pseudo_code`,
  `procedure_assembly`, `procedure_callees` for073F70/08DF30/08F4C0/08D9E0;
  17/17 including lifecycle, 13:40:24–13:43:37 UTC, clean close/process/Job/sampler.
- `r1-waypoint-queue-helpers/`: same tools for08F340/074110;11/11,
  13:53:50–13:57:48 UTC, normal cleanup. Summary SHA256
  `4a4d7d99ef010477f5df232da0b92e531de58603a9695c3943313199ea5eabcd`.
- `r1-portal-dependencies/`: fixed direct019230/5DC3F8 batch11/11,
  14:10:11–14:13:10 UTC, normal cleanup. Summary SHA256
  `6f53e49cc0ba780e9427c7fd1b70c6ccbdead8810a3ecfd6287779423fa277f5`.
  019230 returns the same input identity only after the Player class check
  (A5CAB0, parent+10,591480); null/rejected types return null. 5DC3F8 is a JMP
  through60B8F0; Rea labels its pseudocode `atan2f`. Callees is empty, which does
  not prove no indirect target. The fixed data follow-up resolves the selected
  descriptor binding and directly referenced constant (below).
- All batches target only
  `D:\Steam\steamapps\common\Grim Dawn\x64\Game.dll`, expected SHA256
  `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`.
  Existing384M Java,5GiB launch/2GiB stop, process ownership and mutex guards stay.

`r1-portal-camera-data/inspection.json` independently reads only bounded PE
ownership metadata,60B8F0 and7A80BC:1,208 bytes, zero code bytes. Selected import
descriptor A42A44 binds60B8F0 to `api-ms-win-crt-math-l1-1-0.dll!atan2f`
(OFT A45360,index10,hint86);7A80BC is bits40490FDB,3.1415927410125732.
Source hashes before/after match. This proves the selected descriptor covers
the slot, not that no overlapping descriptor exists; no loaded IAT, system DLL,
CRT FP environment, camera instance or virtual setter identity was observed.
This is supplementary static data evidence; it does not replace Rea pseudocode.

## Implemented scope

`PortalSelection.cs` preserves target-region then source-region queries, captured
signed portal count, candidate order, returned difference-pointer identity,
binary32 operation order and unordered COMISS/JC selection. It does not pick
the nearest portal or invent a valid region.

`WaypointState.cs`/`WaypointContracts.cs` preserve the same MovementManager
identity. Queue map, capacity, offset and count are live fields. Clear drains
count before reverse frees, rereads the map across callbacks and preserves offset
when initially empty. Growth preserves native segment placement and publication
order. Append copies five16-byte chunks after allocation and increments count
even if the allocator returns null. The iterator returns its supplied output,
copies offset/proxy, and writes only its proven link zero.

Next consumes a record before kind/navigation checks. Ordinary kind0 retains
both world-vector calls, late owner reads, returned coordinate identity,
region conversion and floor placement before the moving flag is set. Unknown
kind and navigation rejection do not roll back consumption or clear the flag.
The no-portal producer composes these real contracts; default unresolved imports
still stop. Tests use explicit mock storage and observed import results.

## Validation checkpoint

Source review and first build completed14:13:45–14:13:46 UTC. New tests passed:
**16 portal-selection scenarios/81 assertions;23 queue/consumer/producer
scenarios/187 assertions, total39/268**. Library, both test compilations and both
test executions exited0; no failed build or test retry. See
`.tooling/r1-waypoint-contract-validation/result.json` and both test logs.
Compiled library SHA256:
`51c00e823cd943ef9b102e1716ce7e7f87a48830cb039e83ef9e47e125a0ad30`.
The executed build command was:

```powershell
& 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' -NoProfile -File tools/static_analysis/build_waypoint_contracts.ps1
```

It writes only `.tooling/r1-waypoint-contract-validation/`, compiles three new
sources and two test programs against the frozen core/manager DLLs. The older45,
35 and15 scenario suites are not rerun. Formal build approval is separate from
query approval; this execution followed confirmed backend cleanup. The script
refuses to overwrite an existing result directory. Do not rerun it to inflate
progress; changed components require a separately reviewed targeted validation.

## Remaining boundaries

Portal three-record production and kind1's full60-byte coordinate frame,
orientation/camera path and recursive next waypoint remain explicit Unsupported.
Large native map headers, heap/EH and noncanonical grow `offset > growth` states
are not validated. Managed memmove/memset projections and test allocators are
own platform adapters, not recovered native heap behavior. Partial-byte aliases,
concurrent memory writes and special native MXCSR modes remain outside scope.

079CD0 action allocation, Engine10C6C0 navigation search, actual movement/import
bodies, physical mouse/coordinate sources and scene/UI integration remain open.
Passing these isolated tests cannot satisfy R1 or R3. The next bounded work is
the portal/player/camera direct dependencies, then actual navigation; no new
WASD or self-drawn game prototype is introduced.
