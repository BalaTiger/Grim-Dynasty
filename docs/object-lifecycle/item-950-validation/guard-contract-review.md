# Independent Game pre-call dependency

The saved caller at 5DAF09 directly invokes Game 5DB8B8 before every call to 16710. The exact seven-byte instruction at 5DB8B8 is `48 FF 25 E9 01 03 00`, decoded by installed dumpbin as `jmp qword ptr [18060BAA8]`. The slot's disk bytes are `80 58 01 80 01 00 00 00`, identifying preferred-image VA 180015880. Its complete first instruction is `C2 00 00`, decoded as `ret 0`.

This proves the disk initializer path independently of Engine. A valid call stack and an unchanged loaded slot/target would produce normal return without an object-field store at this target. It does not prove that the loaded slot remains unchanged, that a loader/policy accepts this callback, or that arbitrary runtime checks have no effects. No loaded game, guard writer, global scan or exception path was inspected. The model therefore retains a default unresolved check and requires a separate own observation on every iteration; tests exercise explicit conditional sources rather than executing the original target.

The two failed parsing attempts are retained, not overwritten:

1. Tool chunk `fa1d39`: six-byte candidate window `48ff25e90103` truncated the REX-prefixed seven-byte jump; dumpbin output was incomplete. Original hashing/closure succeeded.
2. Tool chunk `10158f`: only the missing byte at 5DB8BE was read (`00`). dumpbin decoded the complete jump but wrapped byte seven onto a continuation line. The parser wrongly compared just the first line and stopped before reading the slot. Original hashing/closure succeeded.

The complete saved dumpbin was then reviewed offline, including the continuation byte. The subsequent reader did not repeat either original code read:

- `a8b073`: slot 60BAA8, 8 bytes; disk target 15880, first byte C2. No error, target marked unresolved until its immediate was available.
- `7a8688`: only 15881..15883, 2 immediate bytes 0000; one tiny-PE dumpbin confirmed `ret 0`.

Every original read was read-only with matching before/after Game SHA256 `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`. Neither parsing stop was an authorization rejection, test failure, or execution of original code. No failed reader was retried or overwritten. The archived failure and later offline repair remain separately reviewable.

Total original semantic acquisition for this round: 532 code bytes (prior 522 plus 6+1+1+2), 228 metadata bytes, and 8 data bytes. There were 19 metadata operations, eight dumpbin calls (including both retained parse stops), and sixteen full-file fingerprint passes using 64 KiB buffers. Engine.dll was not read in this round.
