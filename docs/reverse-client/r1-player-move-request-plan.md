# Bounded mouse request integration

Preparation only until the separate MainPlayer build has a successful receipt.
No original files or analysis backend are read/launched by this plan.

The new library composes the saved Idle Move280 target `165CB0 -> 163490`
and wrapper `3CE890` with the existing actor, manager, Engine, coordinates,
MainPlayer lookup, Options getters and `08E200` precheck. New unique cells
are only actor `45B4/4BF0` and existing GameEngine `37748`; they start Unknown.
The coordinate and navigation layers borrow one `MoveContext.EngineGlobal`
cell. No copied actor, default successful callback or mirrored Engine global
is admitted.

The formal action will compile this one new library and its 13 new scenarios,
then run the new test executable once. Existing libraries are pinned and
copied into the isolated new output directory. The saved-map sample reuses
only the existing `NavigationSearchWrapperTests.Scene` constructor and its
four setup checks; no prior suite entrypoint is called or old source compiled.

Expected reviewable endpoints:

- Explicit early returns and Unknown stops preserve the actual instruction
  order, including unknown Region token consumed at `1634DC`, not `22C0B0`.
- Five MOVSS loads preserve Unknown until their actual MULSS consumers;
  two real null-Region difference paths verify the first and second gates.
- The concrete mouse request reaches real MainPlayer/Options/precheck gates.
- The declared OWN navigation start traverses saved navigation and Ground,
  prepares the exact six-byte `MoveTo` and local 40-byte payload, then stops
  at the unimplemented `123D50` transition. No action is installed.
- The actual Ground-projected start keeps the real path result `4` and stops
  at the uncomposed camera retry. No height offset or query extent change
  converts it into movement success.

Cold initialization, special reposition, camera retry/projection, full
transition/Agent submission, physical mouse, rendering and playable client
acceptance remain outside this candidate. Native ABI/EH, concurrent mutation,
MXCSR traps and FTZ/DAZ behavior are not verified by these managed tests.
