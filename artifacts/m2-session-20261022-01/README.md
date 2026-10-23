# M2 headless Liaoxi supply session

This is the project-owned session for the Liaoxi supply slice. It reuses the M1 inventory, task event, HasItem condition and money-reward modules. It does not construct original native actors or read original saves.

```powershell
.\gd-session.exe new hero "Border recruit"
.\gd-session.exe act hero accept-delivery
.\gd-session.exe act hero deliver
.\gd-session.exe show hero
.\gd-session.exe act hero deliver
.\gd-session.exe act hero accept-return
.\gd-session.exe act hero return-item
.\gd-session.exe show hero
```

Each invocation is a new process: successful mutations save automatically. The new own character starts with 50 money and the three-ration inventory fixture. Delivery completes the first task, leaving 125 money; returning the same retained item completes the second, leaving 165. Repeating completed business actions does not restart the task or pay again. Character identity persists; reconstructed tasks and inventory share the same owned world within each session, while separate loaded sessions have separate mutable objects.

Saves live only in this executable's `runtime-data/saves` directory. Use a short slot identifier, not a path. This is a versioned project format with a checked journal and state summary. Unknown actions or versions are not guessed. Corruption is an error: `show` does not silently load a backup. Explicit `recover <slot>` validates and restores the prior backup. Replacement is atomic within the save directory, preserves a backup and rejects stale writes; do not edit a save by hand.

Every command emits one ASCII-safe JSON object with schema `grim-dynasty-session-v1`. Codes: 0 Known, 3 Unsupported, 2 invalid/corrupt/conflicting input, 1 internal error. `act` returns `changed` and the resulting `state`; `new`, `show`, and `recover` return the state directly. Unsupported commands do not save partial state.

This is a narrow headless runtime slice, using the existing own inventory-cycle content. It is not an arbitrary QST executor, native character factory, original save format, combat simulator, XP system or playable graphical version. The sample's initial money/item values are project-authored fixture values, not claims about original new-character balance.

`acceptance.json` records actual fresh-process CLI acceptance. `core-tests.log` records the own store/ownership/validation tests. `build-manifest.json` gives source and compiler provenance; the unchanged accepted M1 remains in its separate package. Use `tools/build_m2_session.ps1` from the repository with the environment's required formal execution approval to reproduce into a new directory.
