# Saved-evidence review: Game 49CB80

This review reads project evidence only. No original binary, decoder, build or test was run. The saved exact interval is `[49CB80,49CCC5)` (325 bytes), ending at `RET` at `49CCC4`. This closes the local forwarding body, not the death-event producer.

Evidence: [instructions](death-notification-target/direct-49cb80.instructions.json), SHA256 `ca9ea0099ddcc21070705e0fda6bf6d1162621744e8937b5ebebc8409e87ebbc`; [inspection](death-notification-target/inspection.json), SHA256 `0d0866cf6f5a40c6c49f75717ed5435a1be2d2249a4765d2611d6f784eb7c70d`. The recorded Game artifact has SHA256 `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`. Addresses below are RVAs unless stated otherwise.

## Confirmed local contract

The first cursor comes from `this+140`; its end is `this+148`. Each element supplies a **32-bit ID**, followed by a cursor increment of **4 bytes** (`49CC22`). The second cursor is loaded from `this+158` only after the first traversal completes. Its end is `this+160`; each element again supplies a 32-bit ID, but the cursor advances **0x30 bytes** (`49CC9B`). The remaining second-record fields are not read here.

Both loops call `[609098]`, then `019D40(returned pointer, ID)`. A null lookup result skips that element. Otherwise, instance virtual slot zero returns the type record: equality with preferred VA `180A5A770` passes; otherwise its `+10` parent is checked for null, direct equality, then `591480(parent, A5A770)`. A nonzero AL passes. There is no null check on the slot-zero return before reading its parent.

On a passing element, the instance's vtable is freshly loaded. Virtual offset **+1B8** (8-byte slot index 55) receives the resolved object and the original RDX, R8D, R9 and fifth stack argument. These values are forwarded without an event-number rewrite. There is no immediate event 11 production in this body, and no named callback implementation is identified.

Each loop rereads its end pointer after the callback/skip path; it does not capture a fixed count. The second start is also observed after all first-loop callbacks. Conversely, the current cursor is not refreshed from the start pointer. Reallocation or shrink therefore cannot be assumed safe, and equality termination is not a bounds guard. Duplicate IDs are not deduplicated. Lookup, type or callback failures/nonreturn stop later work; effects of earlier calls remain. No rollback is shown. The body itself writes no container fields; callees can change state.

## Identity search and remaining boundary

The existing fixed [registration metadata](../object-lifecycle/offline-class-registration-metadata/metadata.json), [export discovery](../object-lifecycle/offline-class-registration-exports/discovery.json), game initializer metadata/data and seven saved static-table batches do not identify `A5A770`. Exact-address searches over 224 saved batch instruction files found no explicit reference to `A5A770` or its 48-byte record neighborhood. This bounded negative result does not exclude indirect initialization or evidence outside those batches. No associated instance vtable or +1B8 implementation was located.

The smallest next data request is **48 bytes at `[A5A770,A5A7A0)`**, anchored by `49CBA3: 4c8d2dc6db5b00`. The existing ClassInfo layout gives name pointer +08, parent +10 and factory +28. Only if +08 is a nonzero file-backed data pointer should a bounded NUL-terminated name read (maximum 128 bytes) be considered. A zero disk name leaves runtime identity unresolved.

ClassInfo's +00 points to its own vtable; it is **not** the filtered instance vtable. Reading `A5A770+1B8`, or that ClassInfo vtable's +1B8, would not resolve the callback. An exact 8-byte instance-slot request needs separate, concrete instance-vtable provenance first. Even a base implementation would not establish every accepted subclass override. No new read plan or implementation is authorized by this report itself.
