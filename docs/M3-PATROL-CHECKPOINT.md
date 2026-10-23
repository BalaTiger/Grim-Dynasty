# M3 scene patrol checkpoint

The new package is `artifacts/m3-patrol-20261022-01/gd-scene.exe`. It preserves the earlier scene package and adds an authored road sentry that patrols, approaches the player's actual character identity, and returns to its home when the player leaves its leash. It uses the existing map collision and cannot walk through the fort wall. This is a project-owned scene adapter, not recovered native AI or a combat-ready enemy.

From the repository root:

```powershell
.\artifacts\m3-patrol-20261022-01\gd-scene.exe
```

The default `player` slot creates a new project-owned recruit when missing and loads it when present. Saves stay under this package's `runtime-data/saves`; no original-game save is read. For a separate slot use `gd-scene.exe patroltrial "Patrol Recruit"`. Controls remain WASD/arrows, E, F5, F9 and Esc. Walk east out of the fort gate to find the road sentry. Combat, equipment and merchant actions remain explicitly unavailable.

## Validation on this host

Accepted at `2026-10-22T05:00:21.0550504Z`:

- 42 new scene-agent checks: patrol/chase/return, target identity, actual wall/gate geometry, time-step bounds, isolation, atomic snapshot restore and explicit Unsupported attack.
- 16 new integration checks: the timer-facing scene API, exact float-bit position/facing persistence, continued movement after reload, foreign-character and blocked-position rejection, unknown schema rejection, explicit old-format migration, and actual actor drawing.
- 22 existing scene checks rerun because scene state/storage/render integration changed. The M1 and M2 standalone suites did not rerun; their source hashes and accepted packages remain intact.
- 129 assertions / 11 groups for the new conditional `068AB0` settlement model. They validate its own finite numeric domain, shared callback state, reentry and failure prefixes. Test-owned callbacks are not native function implementations and are not installed into the scene.

Both compiler logs are empty. This build ran **headless**: no Form or foreground window was opened. The prior `m3-scene-20261022-03` window/input acceptance remains the last actual window run. The new bitmap `validation/patrol/patrol-scene.png` was visually inspected and shows the player and sentry in the scene; it is not a window screenshot. No Library upload/ID exists.

The EXE SHA256 is `a801a839c2d41244ce549c23a51516f2e66eae90f75459cd46cc65fe28a67d4b`. The build manifest records all 47 source hashes and the console-test executable hash.

## Save boundary

Scene schema 2 / content `liaoxi-scene-v2` saves each configured patrol's exact position, facing, mode, intent, target, blocked flag and patrol index with the session. Restore validates the full configured identity set before changing it. A v2 save missing an actor is corrupt; it does not respawn the actor silently. Only the exact known schema-1/content-v1 format initializes the newly added visual patrol, preserving the old character/session/position and flagging the migration in the window. Other versions remain Unsupported. Earlier package files are not migrated or modified automatically.

## Next gameplay gap

The whole M3 slice remains incomplete. Actual attack generation, enemy initial attributes, complete damage/death side effects, once-only kill/reward production, trading and equipment actions are not accepted. The new health writer model deliberately stops at unknown gate/attribute/statistics interfaces and does not make the scene attackable.

The latest bounded evidence closes local health writes and the `535A60` notification iteration, but not their full gameplay context. `CF6E0` has no `.pdata` entry; its fixed 32-byte probe shows attribute-array loads and arithmetic but no return, so it remains an explicitly incomplete prefix. `3FE390` calls `403AC0` twice before updating a statistics value and a type-one maximum; those two lookups cannot be assumed identical or replaced by a default callback. Enemy and item-cost formula strings are now saved, but their runtime parameter bindings, numeric evaluation and transaction/initialization order remain unknown. See the content and combat evidence reports.

No approval denial is pending. No commit, push, install, security-setting change, original-game/Rea/Java execution or player-save access occurred. Continue from this checkpoint toward one verified enemy/attack/kill objective; do not restart unrelated constructors or repeat the accepted suites merely as progress.
