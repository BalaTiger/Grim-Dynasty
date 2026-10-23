# R1 Engine mouse core: saved evidence, not a usable client

The real Rea batch ran **2026-10-22 07:10:29.052–07:13:10.539 UTC**, using the summary timestamps, and passed **17/17 fixed calls**. This review reads only its saved output; it adds no implementation, test, original-binary read or backend execution.

[summary.json](r1-engine-mouse-core/summary.json), SHA256 `4d26ffeb36f6c1bc0e8b8faf0a77c7f601ef08b5ce31a6a2aa2ca56c2449ba77`, records:

- Engine.dll SHA256 before and after: `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`.
- Close acknowledged; closed session, backend exit code 0, empty owned Job and sampler exit code 0 confirmed; no forced termination; errors and owned-profile failures empty.
- 384M heap observed. Sampled backend-tree working-set peak **1,131,646,976 B**; including controller and sampler **1,333,321,728 B**; minimum available physical memory **5,553,238,016 B**. These are sampled Engine observations, not continuous maxima or Game measurements.

Addresses below are preferred static VAs/RVAs. Rea/Ghidra's selected symbol and inferred prototype are not sufficient to identify the role of a shared body. The interpretation below uses the saved assembly and export aliases together.

## What this batch actually closes

| Entry | Observed body | Direct next dependency |
| --- | --- | --- |
| Camera::GetImagePoint, RVA `044A10` | Computes a three-float result for modes 0/1; explicitly writes twelve zero bytes for every other mode | Mode 0 calls `2EE09C`, labelled `tanf` by pseudocode; camera/viewport initialization still unknown |
| WorldCamera::UpdateFromInput, `04E170` | Shared two-instruction virtual tail jump through vtable slot `+0x28` | The concrete WorldCamera vtable and selected target |
| NavManager::FindPath, `150590` | Replaces `this` by its `+8` implementation pointer and tail jumps | Actual path routine `10C6C0` |
| DisplayWidget::HandleMouseEvent, `00D480` | Shared `XOR AL,AL; RET` default body | Concrete widget override and caller's event-routing policy |

## GetImagePoint: calculation and ABI

Sources: [04 pseudocode](r1-engine-mouse-core/04-Camera-GetImagePoint-0x180044a10-procedure_pseudo_code.json), [05 assembly](r1-engine-mouse-core/05-Camera-GetImagePoint-0x180044a10-procedure_assembly.json), [06 callees](r1-engine-mouse-core/06-Camera-GetImagePoint-0x180044a10-procedure_callees.json). The observed instruction listing runs from `180044A10` through the `RET` at `180044C0F`.

The decorated export returns `Vec3` by value. **RCX = Camera this, RDX = hidden result storage, R8 = const Vec2 input, R9 = const Viewport input; RAX returns the same result-storage address.** Ghidra's printed `Vec2*` return and named parameters are misassigned around this hidden return slot. This agrees with the call from the previously inspected WorldRay builder at `229796`, where RCX is the WorldCamera subobject at `this+0x6C`.

The body reads a 32-bit mode at Camera `+0`. It reads two float components from the image input; no pixel-to-normalized-coordinate conversion or range check appears here. Define `x,y` as those input floats, `A = camera[+04,+08,+0C]`, `B = camera[+10,+14,+18]`, and the componentwise base from `camera[+1C,+20,+24]` plus `camera[+28,+2C,+30]`.

- **Mode 0:** calls `1802EE09C` with `camera[+34] * cHalf` in XMM0 and uses its XMM0 result `t`. The pseudocode identifies this call as `tanf`. It converts viewport `+8` and `+C` from signed 32-bit integers to floats and divides the first by the second. The scales are `u = ((viewport8 / viewportC) * t) * (cOne - (x+x))` and `v = (cOne - (y+y)) * t`.
- **Mode 1:** no calls or viewport reads. The scales are `u = ((cOne - (x+x)) * camera[+38]) * cHalf` and `v = ((cOne - (y+y)) * camera[+3C]) * cHalf`.
- Each of those branches forms the three result components as base plus `u*A` plus `v*B`, using scalar single-precision operations in the saved instruction order, then stores x/y/z separately at result `+0/+4/+8`.
- **All other mode values:** `044BE0..044BE5` explicitly writes zero as one qword and one dword, covering result `+0..+0xB`. This is a proved native default, not an invented fallback. The branch does not read the image/viewport inputs. It still requires the initial Camera mode read and writable result storage.

Pseudocode resolves `cHalf`/`cOne` as 0.5/1.0; assembly references `34BADC`/`34BB44`. This review did not read their raw original bytes or the body/import resolution of `2EE09C`. It does not claim bitwise CRT equivalence. The body has no viewport-zero, finite-value, clamp or epsilon guard. It does not normalize the result or perform collision/ray intersection. Projection-mode names, valid input range, basis meaning, field writers, concrete viewport layout beyond these reads, and initialization are not established. Output is three ordered stores, not an atomic transaction.

## UpdateFromInput: shared virtual forwarding, not input logic

[07 pseudocode](r1-engine-mouse-core/07-WorldCamera-UpdateFromInput-0x18004e170-procedure_pseudo_code.json) lists WorldCamera::UpdateFromInput among several same-address `InitializeDefault` exports; its primary ConversationResource label does not change the requested entry's address. [08 assembly](r1-engine-mouse-core/08-WorldCamera-UpdateFromInput-0x18004e170-procedure_assembly.json) is exactly:

```asm
18004E170  MOV RAX, qword ptr [RCX]
18004E173  JMP qword ptr [RAX + 0x28]
```

For the zero-argument WorldCamera member, RCX remains the original object and the caller's return address/stack is forwarded unchanged. No input-device read, camera update, null check or concrete destination is shown. The empty [09 callee list](r1-engine-mouse-core/09-WorldCamera-UpdateFromInput-0x18004e170-procedure_callees.json) covers resolved references only; it does not make this a leaf or prove that the indirect target is harmless. The next evidence needed is the actual WorldCamera vtable writer/entry, not another guess from the shared primary symbol.

## FindPath: preserve the real argument slots

Sources: [10 pseudocode](r1-engine-mouse-core/10-NavManager-FindPath-0x180150590-procedure_pseudo_code.json), [11 assembly](r1-engine-mouse-core/11-NavManager-FindPath-0x180150590-procedure_assembly.json), [12 callees](r1-engine-mouse-core/12-NavManager-FindPath-0x180150590-procedure_callees.json). The four listed instructions span starts `150590..15059D`:

```asm
MOVZX EAX, byte ptr [RSP + 0x50]
MOV RCX, qword ptr [RCX + 0x8]
MOV byte ptr [RSP + 0x50], AL
JMP 18010C6C0
```

The export declares two `const WorldVec3&` inputs, a float, then `WorldVec3*`, `uint*`, `float*`, `mem::vector<WorldVec3>*`, `Vec3*`, and a bool. Under the observed x64 interface the first two references remain in **RDX/R8**; the float is in **XMM3**, not an integer R9 argument. Entry stack slots `+28/+30/+38/+40/+48/+50` carry the remaining six arguments. The wrapper preserves them, including rewriting only the original low bool byte at `+50`; it does not canonicalize it to 0/1. RCX becomes the qword at the manager's `+8`.

The pseudocode's apparent one-argument call loses these forwarded arguments and must not become an implementation signature. There is no local allocation, path search, input validation or success/failure branch. The result of `10C6C0` returns directly to the caller; the export's bool return does not prove the underlying output-write or failure contract. Implementation-pointer ownership, coordinate semantics, path vector layout, optional outputs, collision/nav data and failure prefixes remain unknown. **`18010C6C0` is the exact next direct navigation target.** The callee report's terminal-flow annotation is consistent with this tail jump, not proof of a no-return algorithm.

## HandleMouseEvent: false default only

[13 pseudocode](r1-engine-mouse-core/13-DisplayWidget-HandleMouseEvent-0x18000d480-procedure_pseudo_code.json) lists the exact DisplayWidget mouse-event export alongside many unrelated same-address defaults. Its primary Entity label is not an alternative discovery of the requested UI handler. [14 assembly](r1-engine-mouse-core/14-DisplayWidget-HandleMouseEvent-0x18000d480-procedure_assembly.json) is `XOR AL,AL; RET` (`00D480`, `00D482`); [15 callees](r1-engine-mouse-core/15-DisplayWidget-HandleMouseEvent-0x18000d480-procedure_callees.json) is empty.

For the declared member, RCX is this and RDX is the event reference, but neither is read. AL becomes zero; upper RAX bits are not promised zero. There are no object/event writes or callbacks in this body. This establishes a bool-false default only. Whether false means unhandled, how parents propagate it, which controls override it, and whether UI consumes a world click require callers/overrides. **No working UI event pipeline was recovered.**

## Minimum route to actual mouse movement

The batch closes the calculation inside the WorldRay helper and identifies forwarding boundaries. It does not connect screen events to a scene. The next relevant controller entry remains the already located **Game ControllerPlayer::HandleActionFromMouse, RVA `15E1F0`**; its body must show how selection, destination and action intent are chosen before choosing further dependencies. Its first Game run has a separate provisional calibration plan, not an inherited Engine peak claim.

Within Engine, navigation's concrete next target is **`10C6C0`**; camera updating additionally needs the actual `+28` vtable target. Ray hit testing, input coordinate/button production, UI capture, nav-data initialization and actor motion still need evidence. Existing container decoding does not supply a renderable/collidable scene. No saved result here authorizes automatic callee expansion or restores the stopped WinForms/WASD route; **R1 remains incomplete**.
