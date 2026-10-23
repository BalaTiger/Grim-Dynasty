# R1 implementable contracts and missing connections

This matrix summarizes the saved Rea evidence, not a new native run. Source details and original responses are linked in [input/camera evidence](R1-INPUT-CAMERA-EVIDENCE.md) and [four-entry evidence](R1-MOUSE-CORE-EVIDENCE.md). It deliberately distinguishes a calculable operation from a working client subsystem. The resource-block paragraphs below preserve the earlier 07:43 checkpoint; they are historical. Current Game mouse/state results and active work are in the [registration-chain checkpoint](r1-state-registration-chain/checkpoint.json) and [native evidence](r1-state-registration-chain/EVIDENCE.md).

| Entry | Proven operation that can be represented | Required unknowns before client integration |
| --- | --- | --- |
| Engine `077850`, GetInputDevice | Read the qword at an explicit Engine object's `+200`, preserving pointer identity | Field writer, concrete input device, lifetime, event production and dispatch |
| Engine `229700`, GetRayThroughImagePoint | Ordered virtual calls, origin copy, call to `044A10`, segment-relative difference and scalar normalization; hidden output buffer established | Concrete virtual slot `+18`, token/segment state, valid camera initialization, native floating-point environment, ray intersection; no zero-length guard was observed |
| Engine `044A10`, GetImagePoint | Explicit-input mode 1 arithmetic is independently testable. Modes other than 0/1 explicitly clear twelve output bytes. Mode 0 additionally calls `2EE09C` | Initialization and meaning of fields, coordinate production/range, mode choice, viewport dimensions, `tanf` target behavior and full numeric compatibility |
| Engine `04E170`, UpdateFromInput | Preserve `this` and tail-forward through vtable slot `+28` | Concrete vtable target and its update contract; no update algorithm is present in the wrapper |
| Engine `150590`, FindPath | Substitute implementation pointer from `this+8`, preserve argument slots, tail-forward to `10C6C0` | Actual search algorithm, navigation/collision data, output writes, failure state and motion integration |
| Engine `00D480`, DisplayWidget::HandleMouseEvent | Return bool false without reading this/event or making callbacks | Meaning of false at the caller, concrete widget overrides, capture/propagation and world-click suppression |
| Game `15E1F0`, ControllerPlayer::HandleActionFromMouse | Real Rea body, state selection and interception/primary/secondary dispatch now recovered; concrete registered shared handlers `161D00`, `162290`, `1624B0` were recovered in the successful 11:32 retry. See the current evidence link above for ordered reads and side effects | Physical input-boolean meanings, WorldVec3 producer, actual movement/skill dependencies, initial activation and full overlay ownership; dispatch returns do not establish movement success |

## Proven and unproven edges

The current body evidence proves **`229700 → 044A10`** and **`150590 → 10C6C0`**. The latter body remains unread. It also proves indirect forwarding slots, but not their concrete targets.

The later [action/navigation result](R1-ACTION-SUBMISSION-RESULT.md) establishes the movement path from Game state handlers through Player/CharacterMovementManager FindPath and Game609F38 to Engine150590. Other input/camera/UI connections remain unproven. A plausible client sequence — event, UI routing, camera conversion, scene intersection, action choice, navigation and actor motion — is an investigation checklist, not a recovered call graph. Do not connect the functions with default-success callbacks or guessed flags merely because their names fit that checklist.

## Isolated arithmetic validation scope

The chosen light test is only the `044A10` mode 1 formula. Supply both basis vectors, both three-component base vectors, width/height, image x/y and both scalar constants explicitly. The output is a three-component calculation, not a world destination or collision result.

For each scalar step, the model uses binary32 rounding. In mode 1 the observed sequence is:

```text
u = ((cOne - (x + x)) * width) * cHalf
v = ((cOne - (y + y)) * height) * cHalf
result = ((D + C) + u * A) + v * B
```

Rea decodes `cOne`/`cHalf` as 1.0/0.5. The prior Actor [completion evidence](../object-lifecycle/ACTOR-CONSTRUCTOR-COMPLETION-RESULT.md) also records disk bytes `0000803f` at `34BB44`; that is not a loaded-runtime invariant. This light work adds no original-byte read for `34BADC`. Constants therefore remain explicit test inputs, not silently installed camera defaults.

Test fixtures may use x/y values between 0 and 1 to make hand calculations easy, but that does not prove the producer's range. The observed body has no clamp; an outside-range fixture checks extrapolation. Rounding fixtures distinguish the actual operation order from reassociated algebra. This model assumes ordinary round-to-nearest binary32 operations without native exception/FTZ/DAZ equivalence claims. It does not emulate memory aliasing, faults, hidden-return ABI or object construction. Missing-input errors belong to the self-owned test interface, not a newly inferred native error contract.

No WASD controls, window, game scene or UI substitute is created by these tests. Mode 0, `tanf`, ray hit testing and navigation are outside this small validation.

Actual light validation: **9/9 new tests passed**, exit code 0, using the existing Node runtime with `--test --test-reporter=tap --test-concurrency=1 tools/static_analysis/camera_mode1_math.test.mjs`. The [49-line model](../../tools/static_analysis/camera_mode1_math.mjs) and [small fixture suite](../../tools/static_analysis/camera_mode1_math.test.mjs) check hand-computable center/corners, non-axis vectors, outside-range extrapolation, explicit constants, float32 rounding/order, missing-field rejection and independent result storage. [Raw TAP output](../../.tooling/r1-camera-mode1-validation/tests.tap) is retained. No old suite was rerun in this light phase. The model deliberately covers explicit finite float32 inputs with stable unaliased values; that restriction is its own interface boundary, not evidence of native validation.

## Next finite action

After the light work, take one fresh resource preflight. Only if the existing 5 GiB physical/commit gate and no-conflicting-workload condition pass, continue the already fixed Game `15E1F0` query through formal execution. Otherwise retain the exact snapshot and stop that backend action without lowering its gate or terminating an unknown process. Rendering and scene-data gaps remain separately recorded in [the platform inventory](R1-PLATFORM-INVENTORY.md).

That one post-work preflight was completed at **2026-10-22 07:43:03.6326803 UTC**: physical available **4,266,741,760 B (~3.97 GiB)**; commit **48,690,941,952 / 60,959,854,592 B**; external workload inventory empty. Physical memory still fails the unchanged 5 GiB launch gate. **The fixed Game command was not attempted again**, no backend was launched, and no process was terminated. [Raw snapshot](../../.tooling/r1-camera-mode1-validation/resource-preflight.json) and [light-test result](../../.tooling/r1-camera-mode1-validation/result.json) are retained. No operation remains in flight.

Current action submission implementation and45/193 isolated validation are recorded in [the action component result](R1-ACTION-SUBMISSION-RESULT.md). Historical resource and pending-query paragraphs above are preserved checkpoint history, not current blockers.
