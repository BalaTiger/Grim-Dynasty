# R1 input/camera entry evidence — not a usable-client milestone

The real Rea batch ran from **2026-10-22 06:57:30.257 UTC to 07:00:01.129 UTC** and passed **11/11 fixed calls**. This document interprets its saved evidence only. No implementation or new test was performed for this review.

The [summary](r1-engine-input/summary.json) records identical before/after Engine.dll SHA256 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`, closed session, confirmed backend and sampler exits, empty owned Job, no forced termination, and no errors. The original program was not launched. The records identify Ghidra 12.1.4; pseudocode is decompiler output, not original source.

These two functions establish an input-device field access and part of camera-ray construction. They do **not** establish a controllable scene, mouse selection, click-to-move, pathfinding, UI routing or R1 completion. Preferred addresses below are static addresses, not observed runtime addresses.

## Engine::GetInputDevice — RVA 077850

The [04 pseudocode](r1-engine-input/04-GetInputDevice-0x180077850-procedure_pseudo_code.json) identifies the exported member returning `GAME::InputDevice*`. The [05 assembly](r1-engine-input/05-GetInputDevice-0x180077850-procedure_assembly.json) is the complete observed body:

```asm
180077850  MOV RAX, qword ptr [RCX + 0x200]
180077857  RET
```

- `RCX` is the Engine object address; `RAX` receives the raw eight-byte value at byte offsets `+0x200..+0x207`. No hidden aggregate return storage is involved here.
- There is no `this` check or returned-pointer validation. If the readable field contains zero, zero is returned. A null/invalid `this` is not a safe-null return contract: the effective field address must be readable for normal completion.
- There are no calls or object/global writes in this body; [06 callees](r1-engine-input/06-GetInputDevice-0x180077850-procedure_callees.json) is empty. No device construction, ownership transfer or reference-count operation is shown.
- The field writer, initialization order, allowed null state, concrete device class, lifetime, mouse-coordinate/button representation and dispatch/window binding remain unknown. This getter is not evidence of concurrency safety or a working input pipeline. Ghidra's `__thiscall` label must not be read as an x86 calling convention for this x64 body.

## WorldCamera::GetRayThroughImagePoint — RVA 229700

Use [08 assembly](r1-engine-input/08-GetRayThroughImagePoint-0x180229700-procedure_assembly.json) for register and byte semantics. The decorated export in [07 pseudocode](r1-engine-input/07-GetRayThroughImagePoint-0x180229700-procedure_pseudo_code.json) returns `WorldRay` by value and takes `const Vec2&` and `const Viewport&`. Ghidra instead prints a three-parameter `Vec2*` prototype: its `Vec2 *param_1` occupies RDX but is actually hidden aggregate return storage; its `Viewport *param_2` occupies R8 but is the original Vec2 input; the original R9 Viewport input is omitted. The parameter slots present in that recovered prototype are therefore not the declared user arguments.

### Observed ABI

| Register on entry | Meaning supported by export plus assembly | Preservation/use |
| --- | --- | --- |
| RCX | `WorldCamera* this` | Saved to RBX at `229738`; vtable read at `22972F` |
| RDX | Caller-provided `WorldRay` return storage | Initial qword zero at `22971C`; saved to R14 at `229723` |
| R8 | Original `const Vec2&` image-point address | Saved to RSI at `229735`; forwarded in R8 at `22978B` |
| R9 | Original `const Viewport&` address | Saved to RDI at `229732`; forwarded in R9 at `229788` |
| RAX on normal return | The same return-storage address | `MOV RAX,R14` at `229896` |

This is a description of the observed machine interface, not a replacement C++ declaration. The full size, alignment and construction/destruction ABI of `WorldRay` are not established by this body alone.

### Ordered calls and reads

1. Write eight zero bytes at the return-storage address, before either virtual call.
2. Call the function pointer at `[this.vtable + 0x18]` at `22973B`, with `RCX=this` and `RDX=RSP+0x30`.
3. In order after that call: read camera bytes `this+0x94..+0x9B` at `22973E`; read the qword at returned RAX at `229757` (**token A** in this report); read camera bytes `this+0x9C..+0x9F` at `22975A`. Token A's native type, ownership and semantics remain unproved. The camera reads preserve the bits of two components plus one component, subsequently used by `SUBSS`. Copy token A and these components to return storage as detailed below.
4. Read the vtable again and call its `+0x18` entry again at `229781`, with `RCX=this` and a different scratch address, `RDX=RSP+0x88`. Save the qword at its returned address in RBP as **token B**. Neither stable virtual target nor equal results from the two calls is proved; do not collapse them into one getter.
5. Call direct target `180044A10` at `229796`, with `RCX=this+0x6C`, `RDX=RSP+0x20`, and the unchanged original image-point/viewport addresses in R8/R9. The pseudocode names it `Camera::GetImagePoint`; its body/complete contract is a next direct dependency.
6. After that direct call, first reload token A from return storage at `22979B`; then read eight bytes and four bytes through the helper's returned RAX at `22979E/2297A2`, subsequently treating them as three floats; then test token B and token A for zero at `2297AF/2297B4`. A zero token does not avoid either virtual call or `044A10`. Nonzero tokens are dereferenced at `+0x3C`, `+0x40`, `+0x44` as dwords. Origin floats are also read from return storage after the helper call, not from an immutable snapshot.

[09 direct callees](r1-engine-input/09-GetRayThroughImagePoint-0x180229700-procedure_callees.json) lists only `0x180044a10`. The two indirect calls are independently visible in the assembly; an incomplete direct-target list must not erase them. Input/output alias restrictions, callback effects and mutation during these reads remain unknown.

### Return-storage writes and partial state

| Store instruction | Return-storage bytes | Proven source |
| --- | --- | --- |
| `22971C MOV qword [RDX],0` | `+00..+07` | Zero, before first virtual call |
| `229774 MOVUPS [R14],XMM0` | `+00..+0F` (16 bytes) | token A, then the two components copied from `this+94/+98` |
| `229778 MOVSD [R14+10],XMM1` | `+10..+17` (8 bytes) | component bits from `this+9C` at `+10..+13`; remaining four bytes from stack `RSP+5C..+5F` |
| `22988C MOVSD [R14+18],XMM0` | `+18..+1F` (8 bytes) | final direction X and Y float bits |
| `229892 MOV dword [R14+20],EAX` | `+20..+23` (4 bytes) | final direction Z float bits |

The listing writes only four bytes to stack `RSP+58` before reading eight bytes there. It shows no explicit initialization of `RSP+5C..+5F` before that read. Consequently return bytes `+14..+17` have an **unproved value**, copied from that stack range; they must not be represented as guaranteed zero or a fourth coordinate. Whether these bytes are formal C++ padding requires layout evidence. The body does not write `+24` or higher, and does not establish `sizeof(WorldRay)` as either 36 or 40.

The stores are not transactional. If the first virtual call does not return normally, the initial qword zero has already occurred. If the second virtual call or `044A10` fails, the 24-byte origin copy has already occurred. Direction stores happen only after the shown arithmetic; the final eight-byte and four-byte stores are separate operations. There is no local rollback/error-result branch. External call side effects, exception cleanup and validity of aliased storage are not closed by this evidence.

### Scalar float and integer semantics

For each component in the nonzero-token branch, the body computes the following using the actual values read at those instructions:

```text
integer_delta = signed32(bitwise_mod_2^32(tokenB.dword - tokenA.dword))
d = scalar_float_add(
      scalar_float_sub(image_component, return_storage_origin_component),
      signed_int32_to_float(integer_delta))
```

The dword differences are x86 32-bit `SUB` operations, followed by `MOVD` / `CVTDQ2PS`. There is no overflow check or saturation. This is not a wider integer difference converted afterward. `SUBSS` precedes the float addition; the instruction sequence must not be algebraically reassociated into a different precision or order.

For `dx=XMM4`, `dy=XMM6`, `dz=XMM5`, the normalization sequence at `22984C..229873` is:

```text
xx = MULSS(dx, dx)
yy = MULSS(dy, dy)
zz = MULSS(dz, dz)
s  = ADDSS(ADDSS(yy, xx), zz)
length = SQRTSS(s)
scale  = DIVSS(constant_at_18034BB44, length)
out_y  = MULSS(dy, scale)
out_x  = MULSS(dx, scale)
out_z  = MULSS(dz, scale)
```

These are scalar single-precision instructions, not double arithmetic, reciprocal-square-root approximation, or a fused multiply/add. The sum order is **`(dy² + dx²) + dz²`**. The shown body has no finite check, epsilon test or zero-length fallback.

If either token is zero, all three components instead receive the value loaded from `180338458`, and the same normalization still runs. Ghidra represents this constant as `INFINITY` and `18034BB44` as `1.0`; the current assembly record supplies addresses, not the raw constant bytes. Exact constant-bit confirmation and native MXCSR rounding/exception/FTZ/DAZ state remain outside this batch. Under the usual masked IEEE environment and those decoded constant values, infinity normalization and a zero-length vector can produce NaNs rather than a valid zero direction. That conditional observation is not a measured native-runtime result or a proved NaN payload/exception contract.

## Next bounded evidence and remaining gaps

The direct numerical dependency is `Engine 044A10`. The already identified entries `WorldCamera::UpdateFromInput 04E170`, `NavManager::FindPath 150590` and `DisplayWidget::HandleMouseEvent 00D480` may be inspected only by their separately fixed batch; this report does not infer a call chain among them or authorize a full-tree expansion.

Still missing: the camera virtual slot target/type and both returned-token contracts; `044A10` projection/viewport semantics; constant bits and relevant FP state; creation/lifetime of the input device; mouse target selection and action dispatch; navigation/collision integration; scene/resource/UI loading; real client execution. Game `ControllerPlayer::HandleActionFromMouse 15E1F0` remains a separate module/resource question, with no new Game execution in this work.

## Frozen evidence fingerprints

| File under `r1-engine-input/` | SHA256 |
| --- | --- |
| `summary.json` | `0eaeb55e753fc53b9a1b8a330d1812367440864d7fc9e7e1607137e3bdd160ba` |
| `04-GetInputDevice-0x180077850-procedure_pseudo_code.json` | `6b989faee9673755ddef0109aced590b83c032d0086d00a10c0c9ec24a475be3` |
| `05-GetInputDevice-0x180077850-procedure_assembly.json` | `0f8e5d617035027ff7dc2caef8386926bdf6d5cda52f9329a8aa861df4b30327` |
| `06-GetInputDevice-0x180077850-procedure_callees.json` | `e98b845bfceeb108b4ffc60d04a9d4572ecbddce39b936fac53255f16168d037` |
| `07-GetRayThroughImagePoint-0x180229700-procedure_pseudo_code.json` | `dd6ccbded5cca87060504ebe406913e34fca3cde3164ad78301787dc03582c15` |
| `08-GetRayThroughImagePoint-0x180229700-procedure_assembly.json` | `4e70b6429b52f378c9decc3362e30bc018597fcff55bee0ac8e37df1da9c227e` |
| `09-GetRayThroughImagePoint-0x180229700-procedure_callees.json` | `41ffac79c8bba29064443d32bd8d3c05196fd6ff527665d982934a29b4e2f956` |

Review scope: saved Rea output and existing address provenance only. No original-binary read, native execution, new build/test, implementation change, save access or security-setting change was performed to produce this document.
