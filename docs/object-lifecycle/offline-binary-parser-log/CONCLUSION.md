# Fixed parser log dependency inspection

The inspection completed using read-only file operations. It did not load Engine.dll, execute its CRT imports, start Rea/Java/Ghidra, evaluate a host `Math.Log`/`Math.log`, or change production code/tests.

## Exact new evidence

[log-import-and-multiplier.json](log-import-and-multiplier.json) records:

- `0x1802EE07E`, six bytes at file offset `3069054`: `ff2544490100`.
- This is a RIP-relative indirect jump through `0x1803029C8`.
- The corresponding normal PE import is `api-ms-win-crt-math-l1-1-0.dll!log`, descriptor RVA `4282232`, thunk index `7`. This is the on-disk API-set import identity, not a determination of a live host's resolved CRT module/version or function implementation.
- `0x18034BBC8`, eight bytes at file offset `3451848`: `fe822b654715f73f`.
- The multiplier's binary64 bits are `0x3ff71547652b82fe`; its round-trip decimal is `1.4426950408889634`.

The fixed interpretation reads were exactly **6 code bytes and 8 data bytes**, plus **3,607 bytes of bounded PE/import metadata**. Each metadata read was at most 4,096 bytes, with an enforced aggregate limit of 65,536 bytes. Metadata reads, including offsets and sizes, are retained in the JSON.

Both the original and the already-existing read-only copy were fingerprinted before and after the inspection. Each of the four SHA256 passes read `4,485,632` bytes in chunks no larger than 65,536 bytes. All four digests remained:

`4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`

PE/code/data interpretation used the existing copy. The original was opened only for the streaming fingerprint passes. No other function body or constant range was read.

## Pinned caller evidence

The new inspector verifies these saved file hashes and their common Engine subject digest before opening either Engine file:

| Saved evidence | SHA256 |
| --- | --- |
| `../rea-binary-parser-helpers/09-Parser-helper-0x180284080-assembly.json` | `9e00cc98d04590c79eff8dbeadce59906abcc6f1923961daf0008cc1dd09a43e` |
| `../rea-binary-parser-helpers/08-Parser-helper-0x180284080-pseudocode.json` | `fa0b1ae47cf9dc9a329053d6befd5e13c5486a3624fc883df2ff8e0dd115f7c8` |
| `../rea-binary-parser-entry-retry-2/05-Binary-parser-entry-0x18013f510-assembly.json` | `799736dfda45d5eb59e0d4e45766e43c150f1ece2272f0a3961b551592a8b635` |

`13F6A5..13F6AF` passes `table+0xA0`, `N>>1`, and `N` to `284080`. The fixed dependency addresses are directly present at `2840A4` (`CALL 2EE07E`) and `2840A9` (`MULSD [34BBC8]`).

`2840B1` truncates the binary64 multiplication result to a signed 64-bit integer with `CVTTSD2SI RAX`. The subsequent comparisons operate on **EAX as an unsigned value**, clamping that low 32-bit value to `2..16` before the shift.

## Request 8 remains a boundary

For `N=16`, the request is `8`. The ordinary mathematical `log` contract and this multiplier's exact bits do **not** establish the target CRT's returned binary64 bits or the active `MULSD` rounding environment. A finite product just below `3` truncates to exponent `2` and yields four buckets; a product at or above `3` but below `4` yields exponent `3` and eight buckets.

Therefore this evidence does not justify selecting eight buckets merely by replacing the observed expression with integer `floor(log2(request))`. It also does not justify borrowing a result from a different host runtime. The `N=16` boundary remains outside any claimed bit-exact bucket initialization until a sufficiently precise external-function/floating-environment contract is established.

For `N=38`, the request is `19`. Under the already-explicit normal finite accurate-log contract, the product is approximately `4.2479`, away from an integer boundary, and truncates to exponent `4`: sixteen buckets and mask `15`. This is a conditional normal-domain conclusion, not a recovered CRT body or a blanket extension to all `N>15`.

The slot and descriptor sizes do not depend on this log result. In normal allocation success, `N=16` allocates 384 slot bytes, clears only the first 192, and allocates 192 descriptor bytes; `N=38` allocates 912 slot bytes, clears only the first 456, and allocates 456 descriptor bytes. Fresh constructor state supplies `shift=0` and `used=0`; at most `N` insertions fit the `N` slots without reaching `284160`'s growth branch. These are deductions from the previously saved caller/initializer/insertion evidence, not additional original-file reads in this inspection.

## Deliverable hashes

- [Inspector](../../../tools/static_analysis/inspect_binary_parser_log_offline.mjs): `2fa2d7e47a3b68725b049725f6c55ac6687fdf6d87deafcf115eb188722df416`.
- [JSON evidence](log-import-and-multiplier.json): `00a84af294de1a2ea427385d35bb2da1b5e311ac1a0995654359de64d0cd0f40`.

`node --check` passed, followed by one successful fixed inspection. The script refuses an existing output directory, uses exclusive output-file creation, and cannot silently overwrite this run. No approval rejection occurred.
