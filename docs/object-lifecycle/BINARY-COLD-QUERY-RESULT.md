# Binary cold pointer-cache insertion: static query

2026-10-19 UTC. The bounded Rea query completed: **8/8 calls passed**, one temporary Ghidra session, no additional callee queries. This is new static evidence, not a new implementation or acceptance count. The existing implementation checkpoint remains 844 assertions, 10 combined scenarios and ItemQuestDemo.

## Execution and release

- Rea 6.5.0 / Ghidra 12.1.4; actual Java command line contained `-Xmx512M` (PID 33996). Rea PID 43888.
- Started 07:13:14.529Z; finished 07:15:44.144Z (149.615 seconds). First query took 141.907 seconds for import/analysis; the same session served the other two queries.
- Normal close acknowledged, final session closed, Rea process exit confirmed with code 0, no forced termination, no errors. At 07:16:08.949Z neither owned PID nor any Java process remained. The project runtime file inventory was empty.
- Free physical memory before launch: 13.504 GiB; minimum across 76 samples: 11.859 GiB; after close: 13.632 GiB. The two-second monitor never triggered its 5 GiB stop threshold. Heap and sampled free memory are not an OS-wide memory reservation.
- Input: `D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll`. Before and after SHA-256: `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`.
- No target execution, save/cloud access, tool installation, persistent permission changes, commit or push. HEAD remained `6dfdae419d1c92f7738eefe241937057d83134df`.

See [summary](rea-binary-cold-insert/summary.json), [resource observations](rea-binary-cold-insert/resource-verification.json), [samples](rea-binary-cold-insert/memory.ndjson), and [guard mock checks](binary-cold-query-guard-selfcheck.json).

## Observed behavior of 0x180284160

The caller at `0x180283915` supplies the pointer-cache substructure, the original query address, and the sign-extended name ID. Its temporary name string has already been released. A name lookup result of -1 skips insertion. The caller ignores the helper return value and returns its previously computed ID to the numeric getter.

The new assembly establishes this cache layout relative to the helper's RCX:

| Offset | Observed use |
| --- | --- |
| +0x00 | Shift count; x64 qword shift uses low six bits |
| +0x08 | uint32 bucket mask |
| +0x0C | uint32 capacity |
| +0x10 | uint32 used count |
| +0x18 | Bucket-head pointer array |
| +0x20 | Contiguous slot array, 24 bytes per slot |

At `0x18028417F` the comparison is unsigned. If used count is below capacity, the new slot is `slotBase + oldCount * 24`. The bucket is `uint32(queryAddress >> (shift & 63)) & mask`. The exact write order is:

1. `0x1802841A0`: increment used count.
2. `0x1802841A3`: store the full query address at slot +0.
3. `0x1802841AE`: store the full supplied ID at slot +8.
4. `0x1802841B6` / `0x1802841BA`: read the previous bucket head and store it at slot +0x10.
5. `0x1802841C2`: replace the bucket head with the new slot.

This prepends a slot. There is no duplicate check, allocation, release, or external call in the capacity-available branch. `AL` is set to 1 before return; the pseudocode's inferred `undefined8` return type does not prove the upper return bits.

If count is at least capacity, `0x1802841D8` doubles the uint32 capacity argument, `0x1802841DA` calls **0x180284200**, and `0x1802841E8` recursively calls this insertion helper with the preserved address and ID. The body of 0x180284200 is still unknown. This evidence does not establish its allocation, rehashing, overflow, zero-capacity, or failure behavior.

## Next implementation boundary

A bounded implementation can now cover the complete caller path for a known name ID when an explicit, valid cache snapshot has spare slot capacity: release temporary name, prepend the node, then continue the numeric getter. It must preserve original logical addresses, signed-ID widening, collision-chain order, and cache effects if the later numeric lookup fails. Name ID -1 still proceeds to the numeric getter without insertion.

Full-capacity and unknown storage remain explicit unsupported boundaries until 0x180284200 is evidenced. No game-source implementation was changed in this query task and no new implementation tests were claimed.

## Evidence identities

| File | Evidence ID | SHA-256 |
| --- | --- | --- |
| [Pseudocode](rea-binary-cold-insert/04-Binary-cold-insert-0x180284160-pseudocode.json) | `ev_f9fd6f55a866ca168e80a0f4f3f71088cf83a5b245c09ce162d14630aed2f834` | `7c980e48d7996417b8b23b5ba2dd8d9714e96d5fad24d648c29a758ea20e0d31` |
| [Assembly](rea-binary-cold-insert/05-Binary-cold-insert-0x180284160-assembly.json) | `ev_dee00d3f2d7df9abc0b13f9dd8f01fb515188f2424e9028b751c42e8e37407c2` | `0ef12f91312c6b8a06284d249927b6873bb531c3f3de690f4c7d581e1199ea41` |
| [Direct callees](rea-binary-cold-insert/06-Binary-cold-insert-0x180284160-direct-callees.json) | `ev_61c8217641b9ff13021ea5a69eac4264ae0fb33e6af6db43974a4418170b5eb8` | `12a86597860ca2d9e6c20b158d8527eed02a92a0fd4af7e444a5df4ab82fa90b` |

The call set contains the recursive self-call and 0x180284200 only. Ghidra pseudocode is recovered analysis, not original source. Raw saved responses retain provider limitations.
