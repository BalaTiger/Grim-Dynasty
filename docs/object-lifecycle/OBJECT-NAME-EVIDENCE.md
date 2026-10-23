# Object name assignment: bounded normal-success evidence

Date: 2026-10-19. This continuation closes the ordinary SetObjectName assignment path identified in [OBJECT-BASE-EVIDENCE.md](OBJECT-BASE-EVIDENCE.md), within the explicit byte-string and allocation contracts below. It does not reconstruct general substring operations, native storage, a record loader, a concrete derived Read or the complete item factory.

All native addresses refer to the pinned x64 Engine.dll with SHA256 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`. The fixed [assignment plan](../../tools/static_analysis/rea-object-name-assignment-plan.json) queries only `0x180023180`; the [growth plan](../../tools/static_analysis/rea-object-name-growth-plan.json) queries only `0x180022F70`. Each contains three analysis operations and five lifecycle operations. Results are saved in `result.structuredContent.normalized_result`, with procedure, provider and subject hash metadata. These are static observations, not original-program execution.

## SetObjectName entry and alias behavior

The outer function at `0x18003A780` computes the address of its own name field (`this+8`) and compares that string-object address with the supplied source at `0x18003A791`–`0x18003A794`:

- True self-alias skips the assignment helper entirely. It still hashes the current full name and writes object `+0x28`.
- Distinct string objects call `0x180023180` at `0x18003A7A0`, with position `0` and count `UINT64_MAX`. Equal byte content is not self-alias and does not skip this call.

The helper reads source length, checks position, and clamps count to `sourceLength-position` at `0x180023195`–`0x1800231BE`. At this fixed outer call, valid bounded input therefore selects the entire source length. No substring offset or truncation policy is needed for this entry point.

The helper's own destination==source branch calls `0x180022E30`. That branch is unreachable from this covered SetObjectName call: the outer function already bypasses the helper for the same string object. Consequently `022E30` was not queried or implemented. This does not authorize treating it as a no-op in a future general-purpose string API.

Sources: [outer assembly](rea-object-base/08-Object-SetObjectName-0x18003a780-assembly.json), [assignment assembly](rea-object-name-assignment/05-Object-name-assignment-0x180023180-assembly.json), [assignment direct callees](rea-object-name-assignment/06-Object-name-assignment-0x180023180-direct-callees.json).

## Distinct assignment with sufficient capacity

`0x180023214` compares destination capacity with the clamped count. When capacity already suffices, there is no growth-helper call and capacity is retained:

| Case | Observed writes and calls |
| --- | --- |
| Empty source | `0x18002324B` writes destination length 0; `0x180023254` or `0x18002325C` writes the first logical name byte to NUL, selecting heap or inline storage from destination capacity. No memcpy and no release occur in this branch. |
| Nonempty source | Source and destination independently choose inline storage for capacity below 16, or their stored pointer otherwise. `0x180023270` copies exactly the clamped length from source+position. `0x18002327A` writes that length; `0x180023288` writes a terminating NUL at that index. |

Only the logical payload, length and terminator are replaced; capacity is not copied from the source and is not shrunk. Assigning an empty or shorter name to heap storage does not establish that old storage was released. Native bytes beyond the new terminator are not cleared by the displayed code and are not exposed as initialized padding by the model.

Source capacity is used solely to select source storage. A source string with ample capacity does not transfer that capacity to the destination. Distinct, equal-valued input still takes this assignment path; source bytes, length and capacity remain unchanged.

The normal-copy contract requires valid, disjoint source and destination storage for distinct string objects. Equality of string-object addresses is handled by the outer alias branch; arbitrary overlapping native backing stores are outside the model.

## Growth selection: preserve the strict comparison

When the new length exceeds destination capacity, `0x180023221` calls `0x180022F70(destination,newLength,oldLength)`. In the bounded non-overflow domain, the assembly at `0x180022F9A`–`0x180022FDA` selects capacity as follows:

```text
rounded = newLength | 15
half = oldCapacity >> 1
if half > floor(rounded / 3):
    newCapacity = oldCapacity + half
else:
    newCapacity = rounded
```

The multiplication by `0xAAAAAAAAAAAAAAAB` and high-product shift compute unsigned division by three. The `CMP` / `JBE` at `0x180022FC3`–`0x180022FC6` makes the comparison strict: equality retains `rounded`. Outside the bounded domain, the native body has additional maximum-length and addition-overflow branches; this implementation does not claim their behavior.

This formula is **not** always `max(rounded, oldCapacity + floor(oldCapacity/2))`. For old capacity **235** and requested length **336**:

```text
rounded = 336 | 15 = 351
half = 235 >> 1 = 117
floor(351 / 3) = 117
117 > 117 is false, so capacity = 351
```

The simplified max formula would incorrectly produce 352. These capacities are reachable through successive ordinary assignments from the base constructor: requested lengths 16, 32, 48, 71, 106 and 158 produce capacities 31, 47, 70, 105, 157 and 235 before the 336-byte assignment.

Normal growth proceeds in this order:

1. At `0x180022FDE`–`0x180022FE7`, allocate `newCapacity+1` one-byte elements through `0x180022F00`.
2. If old length is nonzero, copy exactly those old payload bytes through `0x1802DDB0E` at `0x180023024`.
3. If old capacity is at least 16, release the old buffer using its **old capacity+1**, element size 1, through `0x180023090` at `0x18002303F`.
4. Reset the temporary string metadata/first byte, install the new buffer and selected capacity, restore the old length and write its terminator (`0x180023044`–`0x18002307B`).
5. Return to `023180`, which copies the new source payload, writes the new length and its terminator as described above.

The old-payload copy happens before the later full replacement. The logical implementation exposes the normal completed result; it does not claim to reproduce native intermediate buffer states, allocation counts, allocator hooks, exception cleanup or partial failure effects.

Sources: [growth pseudocode](rea-object-name-growth/04-Object-name-growth-0x180022f70-pseudocode.json), [growth assembly](rea-object-name-growth/05-Object-name-growth-0x180022f70-assembly.json), [three growth callees](rea-object-name-growth/06-Object-name-growth-0x180022f70-direct-callees.json).

## Copy and allocation identities

The fixed [copy-thunk inspection](name-interface-discovery/name-copy-thunk-0x1802ddb0e.json) records bytes `FF 25 54 4C 02 00` at `0x1802DDB0E`: a RIP-relative indirect jump through IAT slot `0x180302768`. Ordinary PE import metadata associates that slot with `VCRUNTIME140.dll!memcpy`. This proves the on-disk import association; no DLL was loaded and no runtime import target or CRT function body was executed. Successful copy semantics are an explicit contract, not an inferred equivalence for invalid or overlapping addresses.

Previously collected allocation/release evidence applies with element size 1:

- [`0x180022F00`](rea-container-reserve/05-Container-reserve-0x180022f00-assembly.json) checks the count-size product, allocates directly below 4096 bytes, or allocates an additional 39 bytes and returns a 32-byte-aligned pointer with the original base stored at aligned-8 for larger blocks.
- [`0x180023090`](rea-container-reserve/08-Container-reserve-0x180023090-assembly.json) uses the allocated capacity-size product. For a large block it validates alignment and the hidden base's 8..39-byte displacement, then transfers to the release entry `0x1802DC390`. The release entry's internal implementation remains unexamined.

The successful model assumes valid owned storage, successful allocation/copy/release, and valid large-block metadata where applicable. It does not reconstruct native addresses, OOM, invalid-parameter handlers, invalid storage or allocator/free internals. This is the same conditional normal-success boundary as the earlier container work, with string capacity measured in bytes rather than bucket pairs.

## Hashing after assignment

After either the self-alias skip or a completed distinct assignment, the outer SetObjectName reads the resulting stored length and hashes all bytes in that length. The seed is `0x811C9DC5`; `MOVSX ECX,byte` at `0x18003A7CE` sign-extends each byte to 32 bits, then XOR and multiplication implement:

```text
hash = (hash XOR sign_extend_32(byte)) * 0x01000193  (mod 2^32)
```

Bytes `0x80..0xFF` therefore differ from an unsigned-byte FNV-1a implementation. Embedded NUL bytes inside the stored length participate. A conventional terminating NUL at index `length` is not included. No case folding, character decoding, name parsing or path normalization occurs in this outer hashing loop. An empty name writes the seed.

At `0x18003A7E5`, the hash is stored to object `+0x28`. No ID write at `+0x2C` appears. This write also occurs on true self-alias, even though the name storage and capacity are unchanged. The unsigned 32-bit loop counter is sufficient in the declared 65536-byte domain; arbitrary larger lengths are not covered.

Source: [SetObjectName assembly](rea-object-base/08-Object-SetObjectName-0x18003a780-assembly.json).

## Reproducible model and its limits

The implementation is [ObjectNameString.cs](../../src/task_items/ObjectNameString.cs), [ObjectNameAssignment.cs](../../src/task_items/ObjectNameAssignment.cs), and the name-state getters in [ObjectBaseConstructor.cs](../../src/task_items/ObjectBaseConstructor.cs).

`ObjectNameString` is an explicit valid logical snapshot: bytes are copied on input, length is the payload length, capacity is supplied metadata at least 15 and at least the length but not `UINT64_MAX`, and the terminator is known zero. It is not a recovered std::string constructor or a native buffer. Its copies protect model bytes from external mutation; unused storage and native pointer bits are absent.

`ObjectNameAssignment.Apply` covers SetObjectName, not general `023180`. It requires a valid target, a valid source, exclusive access and the normal contracts above. It preserves the `ObjectBaseState`, its stable `Name` object, the same `RegistrationObject` fields object, dispatch identity and `Id2C`. A distinct assignment replaces only owned logical name bytes/capacity, then writes `RawKey28`; a true alias retains the name and still writes the key. It does not copy source capacity. `NameLength`, `NameCapacity` and `FirstNameByte` now reflect the current logical name; the first-byte getter resolves logical payload and does not expose heap-pointer bits at native `+8`. Getter compatibility is at source level after rebuilding, not a promise about an old compiled DLL ABI.

The declared name-length budget is **65536 bytes**, and the destination-capacity budget is **131072**. These are own coverage limits, not native string limits. A growth operation inside the length budget necessarily starts with old capacity at most 65535; hence the half-growth candidate is at most `65535 + 32767 = 98302`, while the rounded candidate is at most `65536 | 15 = 65551`. Therefore **98302 is the maximum growth capacity reachable from this constructor and these bounded assignments**. It is attainable by assigning length 65535 from the initial capacity 15, then length 65536. The 131072 guard leaves room above every normally reachable result and makes the accepted capacity boundary explicit. Source capacity is not copied or used as the destination budget; it remains independently supplied valid source metadata.

Unknown inputs and budget violations return `Unsupported` before mutation. A caught failure while obtaining the owned managed byte snapshot likewise reports no model mutation; it does not claim native OOM rollback. Native overflow branches, allocation exceptions, invalid storage, concurrent mutation, overlapping distinct backing stores and general substring/erase helper `0x180022E30` remain outside the implemented contract.

Result flags distinguish name writes from key writes: true alias has `AssignmentSkipped=true`, `NameWritten=false`, `CapacityGrew=false`, but `KeyWritten=true` and therefore `MayHaveEffects=true`. Distinct equal-valued names still report the assignment and subsequent key write. Managed byte-array allocation is an implementation detail; `CapacityGrew` refers to the observed string-capacity metadata transition, not managed allocation events.

## Registration and record boundaries

After a supported assignment, `RawKey28` is known and can be consumed by [LoadedObjectRegistration](../../src/task_items/LoadedObjectRegistration.cs). That coordinator still requires a known loaded table and an explicit own Read callback; assigning a name does not load its record, identify a concrete table getter, invoke a native constructor or supply a concrete Read body.

SetObjectName's observed body does not update manager indexes. Renaming an already registered object therefore does **not** automatically remove or replace the previously stored key in an existing registration index. The model preserves that boundary: changing `RawKey28` is not an invented reindexing operation. Any caller-level rename/unregister protocol requires separate evidence.

The saved [assignment summary](rea-object-name-assignment/summary.json) and [growth summary](rea-object-name-growth/summary.json) describe the two bounded static runs; implementation validation is recorded separately by the parent task. This document does not treat successful evidence collection as a substitute for code tests, modify historical results, or claim native execution, original-source recovery or a complete object factory.
