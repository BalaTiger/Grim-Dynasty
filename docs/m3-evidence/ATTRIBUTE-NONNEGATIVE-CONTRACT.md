# Conditional nonnegative attribute getter

The saved `CF6E0..CF700` prefix and `CF700..CF760` continuation establish one ordered, finite, nonnegative-base path ending at `CF73D: ret`. The negative/unordered branch and the incomplete instruction beginning at `CF75C` are outside this contract. The decoder's `LeafWindowUnknown` classification was not an authorization denial.

The subsequent `attribute-scalar-constants` plan read only eight data bytes, no new instructions and no dumpbin invocation. Both target addresses were derived from the three saved RIP-relative MULSS encodings. They lie in readable, nonwritable, nonexecutable `.rdata` (`40000040`):

| RVA | Original bytes | Binary32 bits | Value |
|---|---|---|---|
| `7A8054` (earlier continuation) | `00 00 80 3f` | `3f800000` | +1 |
| `7A8134` | `00 00 c8 42` | `42c80000` | 100 |
| `7A7FC8` | `0a d7 23 3c` | `3c23d70a` | binary32 approximation to 0.01 |

The last value must remain the exact saved float, not a double literal 0.01. Applying the model to loaded behavior requires an explicit condition that these saved constant bits remain applicable; a disk observation does not prove arbitrary runtime patching cannot happen.

Let `i` be signed EDX, `C` the float at `this+1D0+4*i`, `A` at `this+4*i`, and `B` at `this+E8+4*i`. The native sequence reads C then A, adds +1 to C, compares A with zero, multiplies the sum by the first constant, reads B, and applies MAXSS before branching on the earlier comparison. A negative A therefore does not justify skipping the earlier B read or intermediate operations. There is no proved array-length guard or 58-element range contract here.

For the ordered A>=0 branch, with a stable masked nearest-even floating-point environment and gradual underflow, each RN32 below is a separate binary32 operation:

```text
m = MAXSS(RN32(RN32(C + 1f) * 100f), +0f)
t = RN32(RN32(B * FloatBits(3c23d70a)) + 1f)
t = RN32(t * A)
t = RN32(t * m)
t = RN32(t * FloatBits(3c23d70a))
return MAXSS(t, +0f)
```

MAXSS equal-zero selection uses its second operand (+0). Preserve intermediate rounding and stop at nonfinite/out-of-domain operations; do not algebraically cancel 100 with 0.01. The local branch contains no calls or object stores. Missing fields are unobserved, not zero. Initialization, memory validity, source property names, loaded ABI, exceptions, concurrent mutation and the negative branch remain unverified.

The eight-byte read report is `attribute-scalar-constants/inspection.json`, SHA256 `218960c43f175863f9153f6b8d4475291bfcb6479e499a5a3d500028f0cbda2f`. The source Game.dll hash was checked before and after and remained `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`. The report records 688 bytes of bounded PE metadata, zero code bytes, zero `.pdata` bytes and zero tool launches. Whole-file hashing uses 64 KiB chunks; it is not an instruction scan.

Own implementation and validation are reported separately in the package acceptance. This evidence alone does not establish an initialized enemy, an attack input, a completed death event, or full M3 gameplay.
