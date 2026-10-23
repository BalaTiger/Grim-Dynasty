# Item +950: saved static contract before implementation

All conclusions below concern the pinned Game.dll disk image. They are not observations of a loaded game. No build or tests have run in this phase.

`323743..323811` performs 17 stores before calling `428690` with Item+950. Four inline strings use capacity QWORD 15, length QWORD 0, first BYTE 0, in that order. Their bases are 8A8, 8C8, 8E8, 928. Four vptr tokens occupy 908/910/918/920; 948 is a DWORD zero. Unwritten payload and padding remain unchanged.

`428690..4286EE` first stores a QWORD zero at subobject+0. It then invokes `5DAED0(subobject+18, 28, 10, 16710, 165D0)`. Only after normal return are DWORD +1A8 and QWORD +1B0/+1B8/+1C0 cleared. The wrapper returns its original this; the Item caller ignores this value. This makes B18 the exclusive minimum captured Item extent for known normal writes.

`5DAED0..5DAF3E` compares count before each iteration. It calls Game 5DB8B8 with the callback token, then calls the callback with the current element pointer. Only callback normal return advances the pointer by stride and increments the constructed count. Its normal body never reads or invokes the fifth argument. Exception funclets/unwind behavior remain unexamined.

The explicit `16710..16730` candidate window contains a 23-byte normal leaf followed by nine CC padding bytes. Its three writes are QWORD +18=15, QWORD +10=0, BYTE +0=0; it returns original this. There are no calls, branches or input memory reads on this leaf. The ten elements begin at Item+968, stride 28 hex, ending at AF8 exclusive. Each element preserves bytes +1..+F and +20..+27.

`165D0..16620` resets capacity, length and first byte. Capacity >=16 first invokes unknown 16620 with the stored pointer. Its presence as the fifth argument does not establish actual cleanup order, cleanup invocation, or failure semantics. That direct call is not required for the normal construction path and is not followed here.

New normal writes total 52: caller 17, wrapper 5, elements 30. This count is conditional on all ten Game 5DB8B8 checks returning normally. Without such evidence the default model must stop before the first check, preserving 18 earlier stores and zero constructed elements. Engine CE70 cannot supply a Game contract.

Independent review searched 35 existing Game helper/CRT JSON files and two indices. It found only an additional Game 5DB8B8 call at 5DB0B0 before CALL RBX, in `offline-class-static-crt-attach/5dafb8-instructions.json` (SHA256 3db2c5e60ff28a89c99cd4bbe6a3b81af093775f8a985697d513a537e279dd84). No body, slot or disk initializer was archived there.

Evidence instruction SHA256 values:

- 5daed0-exact: 5a5d2ba0f80bb6204ca67eb6431d4c521bb5cd146488d13b24fb403976980bf3
- 16710-candidate: 29529a512a28a997e62d55fb3a8ad8a367684388b9e4f74c04ae04ab78689377
- 165d0-exact: e2bb537b4b7e495a7ebb4d7f43d364dd2faaa3a14f93278a4c70a42e75bbfcd5

Recovery: resume from these saved records, do not repeat the successful 222-byte acquisition. Next stage is the bounded direct Game check dependency, then isolated own fixtures and separately approved build.
