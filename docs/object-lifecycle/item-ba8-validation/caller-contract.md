# BA8 caller / raw import checkpoint

Completed acquisition `91d5b7`, exit0. Saved report SHA256 `4624f2030c5f5c8ae5501c00bd012869c124095476c9fe154c2378d63f8a5091`. No build/test has run for this stage.

Game caller `[323811,3238BE)` is 173 bytes / 25 instructions: initial NOP, twenty stores, two LEAs and two calls through 609720. Native nonvolatile RBP remains the earlier zero under the accepted normal ABI conditions; no fresh input-dependent branch occurs in this segment.

The twenty stores, in order (all values zero except indicated), are:

| RVA | Item offset | Width | Value |
| --- | --- | ---: | ---: |
| 323812 | B20 | 4 | 0 |
| 323818 | B24 | 2 | 0 |
| 323821 | B26 | 1 | 0 |
| 323828 | B28 | 8 | 0 |
| 32382F | B30 | 8 | 0 |
| 323836 | B38 | 8 | 0 |
| 32383D | B40 | 2 | 101h |
| 323846 | B44 | 4 | 0 |
| 32384C | B48 | 8 | 0 |
| 323853 | B50 | 8 | 0 |
| 32385A | B58 | 8 | 0 |
| 323861 | B60 | 2 | 1 |
| 32386A | B62 | 1 | 0 |
| 323871 | B70 | 8 | 0 |
| 323878 | B78 | 8 | 0 |
| 32387F | B80 | 8 | 0 |
| 323886 | B88 | 2 | 0 |
| 32388F | B90 | 8 | 0 |
| 323896 | B98 | 1 | 0 |
| 32389D | BA0 | 8 | 0 |

The B18..B1F gap and other holes remain unchanged; no padding clear is implied. `3238A4` sets RCX=Item+BA8 then `3238AB` calls `[609720]`. On return, `3238B1` sets RCX=Item+BC0 and `3238B8` calls the same slot. Neither return value is checked in this caller window. Native exception handling is not in scope.

The exact raw import was completed without repeating the archived descriptor/lookup-prefix reads: Engine descriptor has OFT A42AC0 and FT609000; lookup index228 bytes 3287a40000000000 points to A48732. New reads verify IAT609720's8bytes and29bytes of hint/name at A48732, yielding hint565 and `??0WorldVec3@GAME@@QEAA@XZ` plus NUL. Previous archive already preserved the Engine.dll name and descriptor bytes.

Game SHA256 before/after remains `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`; source closed, failures empty. One dumpbin invocation only received an owned3072byte tinyPE. Pending: use this exact imported symbol to locate the Engine export and body, not a guessed vector layout. The current model sources remain unchanged from the950 checkpoint; only a new build script is prepared, not executed.
