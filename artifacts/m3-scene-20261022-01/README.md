# 辽西驿堡 — M3 first scene checkpoint

Run `gd-scene.exe` (or double-click it). The default own slot is `player`; a missing slot creates a new own recruit, while a corrupt or unsupported existing slot fails explicitly. `gd-scene.exe another-slot "Recruit name"` selects a separate character. No game installation or original save is used.

You can walk through an actual 2200×1600 scene: the border post, gate, courier road and abandoned beacon. WASD or arrow keys move; the character collides with walls, buildings, tree trunks and rocks. The camera follows. At the quartermaster, press **E** to accept and then deliver the three carried rations. Follow the road to the supply clerk and press **E** to accept and finish the return check. These actions use M2's actual task/inventory/money modules: 50 → 125 → 165, with no repeated completion rewards.

**F5** saves, **F9** reloads, **Esc** saves and exits. Position, character and task journal are stored together under this package's own `runtime-data/saves/scene-<slot>.save`. The prior version is `.bak`; `gd-scene.exe --recover <slot>` explicitly validates/restores that backup. Unknown versions, bad checksums, blocked map positions and stale writers are rejected, not replaced with defaults.

This is **M3's first visible sub-milestone, not completed M3**. Combat/damage/death generation, AI, buying/selling, equipment actions and a calibrated 10–20-minute playable loop remain unimplemented. Space/B/I report those unavailable capabilities. No attack formula, price, native equipment rule or original balance is invented. Movement, scene geometry, original drawn art and input are project-owned presentation adapters; they are not claims of recovered native rendering or physics.

The lightweight C#/.NET Framework WinForms/GDI+ implementation follows the repository's independent-code route and adds no engine/tool installation. All scene art is original code drawing. M1 and M2 are retained separately and unchanged.

See `acceptance.json`, `scene-tests.log` and `validation/window/window.png` for actual checks. The window smoke opens a real own window, feeds the same key handler/timer used by the interactive application, records a changed world position and captures the window before closing. This is not a claim of manual user playtesting. `validation/models/scene-start.png` and `scene-end.png` use the same renderer as the window.
