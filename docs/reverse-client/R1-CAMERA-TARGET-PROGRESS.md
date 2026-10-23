# Camera target continuation — 2026-10-22

Active scope: reuse Game2BE6E0 GetTarget evidence; bind only its three unresolved
Engine imports6094A0/609B70/60A690 and literal7A8054, then inspect necessary
direct bodies and implement the supported camera path. No repeated prior query.

Prior portal/camera49 scenarios412 assertions remain frozen and are not rerun.
Runner and camera source before changes are in
`.tooling/r1-camera-target-prepared`. No new backend or build has started.

15:21:23UTC approved read-only resource query: physical available17728102400B,
commit headroom28762284032B, no matching heavy processes or active Rea runtime.
The earlier sandbox CIM query failed; its zero values were invalid and were not
used as a resource measurement. Fresh launcher guards remain mandatory.

GetTarget returns its supplied hidden output. Its null-region branch stores the
first16 bytes before reading the source tail; the other branch captures all24
bytes before translations. The portal caller consumes only camera basis6..8;
untouched output DWORD14 must not be defaulted merely to satisfy a QWORD adapter.

Not yet achieved: production GetTarget imports, real navigation and movement,
physical mouse/world-coordinate input, real scene/render/UI, R1/R3 acceptance.

Final update15:40UTC: the three imports and production GetTarget/basis bridge
are implemented; first build28scenarios273assertions passed. The earlier lines
are the start checkpoint, not current unresolved camera status. See
`R1-CAMERA-TARGET-RESULT.md`. Navigation10C6C0 subsequently completed8/8; no
pending backend remains. The real navigation/input/scene boundaries are unchanged.
