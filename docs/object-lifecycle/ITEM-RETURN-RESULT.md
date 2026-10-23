# Conditional Item normal return

2026-10-21. The own shared-state continuation now reaches **Item RET323A86**, conditional on the earlier Actor/Item prefixes, an explicitly observed valid GameEngine object, the covered exact numeric domain and an explicit normal external-call result. This closes the previous final direct-call integration gap; it does not establish a complete native constructor, recovered GameEngine initialization, or official Item/QuestItem compatibility.

## What changed

`src/task_items/ItemConstructorReturn.cs` adds the final continuation without changing existing production sources. It captures A66288's supplied pointer identity once when `Begin` runs, retains the same borrowed object after slot replacement, and reads its two live DWORDs in order into Item+B68 and Item+B6C. An unknown or invalid second field preserves the first write. The twelve-byte source view keeps the original QWORD initialization width visible without pretending that Item reads twelve bytes. Separate own backing that would alias Item's observed addresses is explicitly outside coverage.

The source relation is now concrete: InitializeObjects publishes GameEngine2C07A0's returned pointer at36B25F, before calling Initialize2D7230. The constructor stores40400000 at+1B58 as a DWORD and0000000040A00000 at+1B5C as a QWORD. However, its later2C18D1 call to LoadFromDatabase2C8750 and other initialization calls remain unresolved. Those constants are therefore **not** installed as default current global values. Narrow raw-byte verification rebinds the two stores, this capture, return selection and publication to the pinned original.

The last direct target324560 is now independently bounded and decoded:123 bytes,28 instructions. Its scale operand is the disk binary32 value1000.0 at7A8164, and its external call is bound to `api-ms-win-crt-utility-l1-1-0.dll!rand` atIAT60BA70. The model pauses at that call for an explicit return observation; it executes no native RNG and invents no seed or sequence. After the observation, it rereads current Item+B68 and the supplied scale. It retains the pre-call divisor and does not recalculate it from a later B6C value.

The zero-divisor route is deliberately preserved: it first writes Item+B64, still reaches the external call, rereads B68 and scale, then stops at IDIV3245B9. No successful fallback, early failure or rollback is inserted. Signed division overflow and unsupported numeric operations retain their precise frontiers. Only a normal helper return permits the caller's323A71 selection of original Item identity and323A86 return.

Numerical coverage is intentionally narrower than full SSE. Integer dyadic arithmetic accepts exactly representable finite normal binary32 values and zero; CVTT truncates toward zero within signed32-bit bounds. The caller must explicitly assert a stable masked nearest/even environment. Operations requiring rounding, subnormal/nonfinite values, invalid conversions and full MXCSR/exception behavior remain unsupported. Phase APIs are own observation points with exclusive access within each phase; they are not claims that native rand provides an Item callback or that native access is atomic.

## Actual execution

The fully reviewed fixed script `src/task_items/build_item_constructor_return.ps1` ran once through formal `require_escalated` approval. It used18 frozen inputs, three installed-csc invocations and two own test executables, writing only the new `.tooling/item-constructor-return-build-20261021` directory. Tool chunk69c5a1 began the run; completion00d01d confirmed exit0. There were no compiler warnings, denied actions or pending build sessions.

| Suite | Assertions | Groups | Actual runs |
| --- | ---: | ---: | ---: |
| New ItemConstructorReturnTests | 59 | 10 | 1, all passed |
| Affected ItemConstructorNameTests | 76 | 9 | 1, all passed |
| Other old standalone suites | Not rerun | Not rerun | 0 |

New fixtures exercise the actual own Actor→Item bridge→950→BA8→BE0→Identity→Name chain. They check a whole captured Item-byte oracle and retained BE0 allocation, original Item/Actor reference identity, live borrowing and invalidation, unknown/partial fields, no default globals, post-call reads, exact widths, delayed zero division, signed raw-register boundaries, floating-point coverage stops and phase guards. The observed3/5, scale1000, external EAX1001 fixture reaches Item return with B64=4001. Negative EAX and alternative-scale edge fixtures are explicitly synthetic register observations, not claims about actual rand outputs or the archived scalar.

See `item-return-validation/build-preflight.json` for18 input hashes and `build-result.json` for the complete initial/completion tool results. `model-review.md` and `numeric-review.md` contain independent static reviews; they are kept separate from executed test evidence. `post-build-independent.json` records final input/output and baseline verification.

## Closure and remaining route

The previous nine call sites now have connected own conditional paths: GetObjectName, seven016460 no-growth assignments, and the final324560. This last site introduces an explicit rand-return observation boundary, so nine connected sites do not mean every callee branch or runtime prerequisite has been recovered. The complete Item caller's earlier conditions also still apply.

The next bounded evidence priority is **2C18D1→2C8750**, focusing on writers of GameEngine+1B58/+1B5C, their database sources and exact keys. Initialize2D7230 and publication/lifetime ordering must remain separate until directly supported. The present model can consume explicit valid current observations, but cannot construct a trustworthy native-equivalent GameEngine from defaults.

After that, runtime determinism still needs the relevant rand seed/state/return contract and broader floating-point behavior where real inputs require rounding. QuestItem's saved15-store suffix can be connected to a valid conditional Item return with the same allocation identity and preserved unknown padding; this round does not extend storage or implement that suffix/factory wrapper. Allocation failure, string161A0 alias handling,16340 growth, the60B680 fatal path, loaded guards/imports, real synchronization, exception cleanup and official sample compatibility remain unresolved. These are separate from the normal-path fixture result and must not be erased by its passing assertions.

## Evidence and recovery

Only406 new original semantic bytes were read:157 code,245 metadata and4 scalar bytes. There was one tiny-PE dumpbin invocation and four64KiB-buffer Game hash passes. Game SHA256 remained `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`. Engine was not newly read this round. No original DLL was loaded or executed.

- Starting checkpoint: `.tooling/checkpoints/item-constructor-name-complete/manifest.json`,530 selected files, SHA256 `151a19331866c19f3560b93acb8088b07ec7e2ae150b238d3d7766a643ddf2e0`.
- Static checkpoint: `.tooling/checkpoints/item-return-static-complete/manifest.json`,20 files/294287 payload bytes, SHA256 `c2a45bd665f43d4c72d319aa603d20bed5ba167bd9d980ede183c4d1a31960e3`.
- Final checkpoint: `.tooling/checkpoints/item-constructor-return-complete/manifest.json`, containing selected prior state, new source/evidence/validation and the three own build outputs. Its manifest records final counts and copy hashes.

One unrelated read-only query failed to create its process because the execution transport disconnected. A subsequent read succeeded; no build was pending or retried as a result. The two static readers and the approved build each ran only once. No game/Rea/Java, saves/Steam cloud, installation, security/ACL/execution-policy change, commit or push occurred. Existing tracked edits and HEAD are preserved.
