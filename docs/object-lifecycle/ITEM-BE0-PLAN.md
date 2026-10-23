# Item BE0 bounded continuation

2026-10-21. Continue from the sealed BA8 result at Game RVA 3238BE. Target the direct call at 3238DA through IAT 609980 with original Item+BE0 and argument 2. Reuse saved evidence first; independently resolve the actual import and constructor contract before implementing it. Only the continuous caller prefix and minimum direct dependencies are in scope.

Preserve the single mutable Item/Actor state, exact store widths, untouched and unknown bytes, original-this identity, and completed prefixes at coverage stops or callback failures. Do not infer initialized memory, loaded IAT behavior, or native exception cleanup from disk code or own fixtures. Stop at the next unsupported direct dependency.

Baseline: `.tooling/checkpoints/item-constructor-ba8-complete/manifest.json`, SHA256 `76d821bf457ecaeae1f208a413a020fd6b6e88baa44c5bb4e7bfbef20cf41b85`, 434 files. Verify this checkpoint and preserve existing edits. Save evidence and test state before final sealing.

Any necessary original reads remain fixed and bounded with before/after streaming hashes. No original module loading, game/Rea/Java, player saves or Steam cloud, security configuration changes, installs, commit or push. Compile and run only reviewed own sources through a separate exact `require_escalated` request; prior read-only approval does not authorize build execution. Do not rerun or substitute the historically denied build script.
