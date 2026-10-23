# Item return numeric review

Target: `src/task_items/ItemConstructorReturn.cs`, 18,784 bytes.

SHA-256: `2a23081b4c995b4bc73c46827d6b8ef3a0b82ffd5cd994f8f1376472a1936feb`.

This independent review was static only. No source, reader, build, test, native module or floating-point instruction was executed. Production source was not modified. No blocking defect was identified in the reviewed numeric domain; this is not a test-pass or native-compatibility claim.

## Integer bounds and exact binary32 domain

- `Parts` yields a nonzero normal significand in `[2^23, 2^24-1]` and exponent in `[-149, 104]`; zero is handled separately. Subnormal and nonfinite inputs stop explicitly.
- `Multiply` forms a product below `2^48`, within `UInt64`. Exponent addition remains within `Int32`.
- `Add` accepts exponent separation at most 39. The larger aligned magnitude is below `2^63`; adding the unshifted smaller magnitude still remains below `2^63`. Equal-exponent addition is below `2^25`. Thus the accepted shifts and sums do not overflow `UInt64`. Greater separation between two nonzero normal inputs cannot yield an exact binary32 sum or difference and stops as requiring rounding.
- `Compose` removes trailing zero bits, then accepts at most 24 significant bits and a normal exponent, or signed zero. Its significand shift stays within 24 bits; conversion to `UInt32` is bounded. Cancellation and signed-zero handling match the explicitly required nearest/even environment within this exact domain.
- `FromInt32` negates through `Int64`, so `Int32.MinValue` does not overflow during magnitude construction. Remainders requiring rounded integer-to-binary32 conversion stop rather than being silently approximated.

## Conversion, division and field arithmetic

- `Truncate` implements truncation toward zero. Positive results are bounded by `2^31-1`; negative magnitude `2^31` is accepted only as `Int32.MinValue`. Larger magnitudes stop as outside conversion coverage. The explicit exponent guards prevent C# shift-count masking from changing the intended result, including shifts of at least 64.
- Before `%`, the model stops for divisor zero and for `Int32.MinValue / -1`. For remaining operands, C# signed remainder matches the low-32-bit `CDQ`/`IDIV` arithmetic contract. The saved divisor is retained across the external observation boundary.
- The zero-divisor path preserves its earlier DWORD store to `+B64`, then still reaches the random-call observation and division frontier. It is not treated as a successful fallback.
- Global-to-Item copies at `+B68` and `+B6C`, and dependency stores at `+B64`, are each four bytes. Little-endian DWORD reconstruction and narrowing are bounded by that width.
- The borrowed-field constructor bounds its final address; `ItemConstructorStorage` bounds the captured Item extent. Consequently both inclusive range endpoints used by the alias guard fit `UInt64`. Rejecting overlap of the separate borrowed 12-byte backing with Item storage is an explicit coverage restriction, not a native error branch.

## Limits

The review does not establish complete SSE behavior. Rounded arithmetic, subnormal inputs/results, NaNs, infinities, overflow, invalid conversions, MXCSR flags and unmasked exceptions remain outside coverage. The caller must explicitly supply the stable normal masked floating-point environment, loaded scale observation and external return observation. No RNG sequence, runtime publication, concurrent behavior, exception cleanup or full Item/QuestItem compatibility was validated.
