# Item identity getter continuation

2026-10-21. Start from the sealed BE0 result at Game323968. Resolve the exact import609128 and its Engine body before implementing the getter and DWORD copy at32396E. Reuse cached caller bytes through32396E; any new caller read ends at the next call323977/609520. Do not infer getters solely from names or restore the remaining constructor tail in this step.

Baseline: `.tooling/checkpoints/item-constructor-be0-complete/manifest.json`, SHA256 `bd1994da660b59cec82c431bab258f1fec8beb8667e74bb3d11f307bf53be5b3`,461 selected files. Keep the same mutable Item/Actor state. Unknown input bytes stay unknown; Object construction does not establish a zero ID. No synthetic native error or callback should be invented for an ordinary getter.

Use fixed, bounded original reads only if saved proof is insufficient, with streaming before/after hashes and closed handles. No game/Rea/Java, player saves or Steam cloud, installations, security changes, commit or push. Compile reviewed own sources and targeted fixtures only through a fresh exact require_escalated approval. Never retry the historically denied build script or bypass execution policy.

Also inventory the remaining direct calls and global-state reads between the current boundary and saved Item return323A86, separating existing evidence from unresolved implementation/compatibility. Do not turn address proximity or passing own fixtures into a native compatibility claim.
