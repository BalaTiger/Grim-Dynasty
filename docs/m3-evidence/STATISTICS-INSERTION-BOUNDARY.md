# Statistics insertion: saved-body boundary

Reviewed only `damage-statistics-insertion/direct-403ba0.instructions.json` and `direct-403be0.instructions.json`, plus their run report. The approved pass read 721 bytes: `[403BA0,403BD4)` and `[403BE0,403E7D)`. Both decoded contiguously; original Game.dll SHA256 remained `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`. No new native reads, downstream investigation, implementation or tests were performed. All offsets below are hexadecimal.

## 403BA0: local node preparation, not a proven allocator

The function preserves input R8, then calls unresolved `019C90`. With its returned pointer `p`, the observed order is:

1. Compute `p+1C`; write **two bytes zero** at `p+18`.
2. Test the computed `p+1C` pointer. This is not a preceding null check on `p`; the `+18` write has already occurred.
3. If the computed pointer is nonzero, load a pointer through the original R8 argument, load **eight bytes** from that pointer, and copy those exact eight bytes to `p+1C`.
4. Write **four bytes zero** at `p+24`, then return the original `019C90` result in RAX.

Thus the key copy is 64 bits and the adjacent initial value is a 32-bit zero bit pattern. No wider zero fill, allocation size, complete node initialization, key ownership or constructor semantics are established. The zero at `+24` follows the key copy. If a preceding access or helper fails, later writes/return do not occur; rollback is not shown.

## 403BE0: empty/nonempty hint dispatch

Use `C` for original RCX, `out` for original RDX, and `hint` for original R8. R9 supplies the candidate key pointer. The initial split tests **qword `[C+8]`** against zero.

For zero, load head pointer `[C]`, pass it in R9 with R8B=1 to unresolved `04DA10`, forward the saved stack value, and return `out` after normal completion. This proves the empty-case dispatch, not successful insertion or initialized links.

For nonzero, the body distinguishes `hint == [head]`, `hint == head`, and interior hints. It compares candidate and node keys as **two signed 32-bit components**, first component first; the second is consulted on equality. Node components are at `+1C/+20`. Interior cases walk qword links at `+0/+8/+10`, using byte `+19` as a traversal marker, then check predecessor/successor ordering. These observed offsets do not establish complete sentinel or balancing invariants.

Accepted local positions call `04DA10` with an observed 0/1 flag and selected neighboring pointer. Other cases, including equality paths, call unresolved `403E80`. On that fallback's normal return, load eight bytes through its returned pointer and write **eight bytes to `out`**, then return `out`. Duplicate-key behavior cannot be inferred without that helper. The trailing INT3 at `403E7C` follows the normal RET and is not an additional insertion action.

## Remaining boundary

`019C90`, `04DA10` and `403E80` remain unknown. Their allocation, link/count updates, duplicate handling, ownership and failure cleanup are not supplied by these bodies. Preparation and publication must not be treated as one atomic transaction: initialized key/value bytes can exist before insertion completes, and a helper may modify state before failing. This pass closes local widths and dispatch evidence; it does not yet close missing-key insertion for the runtime statistics path.
