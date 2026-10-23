# Engine root registration: bounded static result

Date: 2026-10-20. HEAD: `6dfdae419d1c92f7738eefe241937057d83134df`.

## Result and scope

The 48-byte record at Engine RVA `41CF30` is precisely bound to the named
export `?classInfo@Entity@GAME@@1VRTTI_ClassInfo@2@B`. The disk parent chain is
Actor `41CFC0` -> Entity `41CF30` -> Object `41CF00` -> null.

Three direct initialization writers are now identified by actual record
operands, with their visible normal control flow saved as three exact unwind
fragments each (43 + 116 + 17 = 176 bytes). This supplies static evidence for
the reverse child-registration operations. It does not establish successful
native initialization, a complete root graph, or formal-sample compatibility.

| Record | Initializer-table slot (zero based) | Writer RVA | Input string address, unread | Tail callback address, unread |
|---|---:|---|---|---|
| Actor `41CFC0` | 23 | `5C70` | `309E64` | `300F90` |
| Entity `41CF30` | 31 | `61F0` | `309AC4` | `301110` |
| Object `41CF00` | 55 | `7270` | `309AEC` | `301590` |

The table indices describe saved pointer positions. They are not observations
of executed callbacks. CRT implementation/iteration behavior and successful
module startup have not been executed or independently analyzed here.

## Record and table provenance

- [Parent record](offline-engine-class-parent-record/metadata.json): exactly
  48 new bytes. Entity's stored parent is preferred VA `18041CF00`; its stored
  name/count/children are zero and its factory is `1800499A0`, not followed.
- [Named export](offline-engine-class-parent-exports/discovery.json): 166 new
  export-metadata bytes; exact Entity export at name index 6124, RVA `41CF30`.
- [Entry field](offline-engine-root-entry/metadata.json): 4 new bytes establish
  Engine entry `2DD044`.
- [Entry trace](ENGINE-ROOT-ENTRY-TRACE.md): direct entry/reason edges reach
  the saved array arguments. Earlier gates, later callbacks and exception
  semantics remain qualified.
- [Import bindings and table](offline-engine-root-initializer-table/table.json):
  96 new import bytes bind `302A50` to `_initterm_e` and `302A58` to `_initterm`
  in `api-ms-win-crt-runtime-l1-1-0.dll`; 1064 bytes preserve all 133 slots of
  `[302D48,303170)`, including null slot 0. The separate 16-byte
  `_initterm_e` input array was not read.

Engine source: `D:\Steam\steamapps\common\Grim Dawn\x64\Engine.dll`,
4,485,632 bytes, unchanged SHA256:

```
4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153
```

## Direct writer behavior

All three saved writers have the following observed address-level sequence:

1. Call IAT `302BB8` with the fixed input-string address.
2. After that call returns, load this record's parent at `+10`, then store RAX
   to this record's name at `+08`. There is no observed null-return test.
3. If that loaded parent is null, skip the child-storage path.
4. Otherwise read its 32-bit count, increment in 32 bits, sign-extend, multiply
   unsigned by 8 and choose all-ones on multiplication overflow; call `2DC900`.
5. Copy existing children in order using the selected parent's current
   count/storage. The loop rereads those fields; it does not reload this
   record's parent selector. Append this record's own fixed address.
6. Increment the parent's count, pass its old storage to `2DC8F8`, then publish
   the new storage at the parent's `+20`.
7. Pass the record-specific callback address to a tail call to `2DC8E0`.

No visible duplicate check, parent assignment, factory assignment, or reset
of the writer's own children/count occurs. The retained disk parent values
therefore support conditional operations Entity.children += Actor and
Object.children += Entity, provided those values are still current and all
required calls/accesses complete normally. Object's disk parent is null;
with that value retained, its own writer skips append and does not erase
children previously registered into Object. The same holds for Entity's own
children while Entity registers into Object.

This is a conditional static conclusion. It does not prove the absence of
other writers, the contents/order of sibling arrays, or the runtime state
before/after arbitrary callbacks.

## Failure boundaries and remaining evidence

Engine `302BB8`, `2DC900`, `2DC8F8` and `2DC8E0` have not yet been bound to
their independent Engine import/helper contracts. The similarly shaped Game
helpers are not proof of their identity or failure behavior.

The observed writers do not guard the result of `2DC900` against null. If it
does not return, name was already stored. If copying fails, the new span may
be partly written. If `2DC8F8` does not return, append and count increment
precede publication of the new storage. The tail registration occurs after
publication. No rollback is inferred. Negative/corrupt counts are not
reinterpreted as a safe supported state.

The input strings, tail callbacks/destructors, helpers, complete exception
paths, other sibling registration, native constructors and full dispatch
from the Engine root remain open. Formal native samples and save
compatibility remain unverified; the independent project is not intended
to load original saves.

The next narrow evidence targets, if continued, are the now-directly-observed
Engine IAT `302BB8` and helpers `2DC900`, `2DC8F8`, `2DC8E0`, plus only their
necessary import metadata. Do not start another broad candidate search.

## Search limits and negative evidence

The writer search examined first unwind fragments at saved table slots
23..56: 34 fragments of 43 bytes. Only Actor, Entity and Object continuations
were followed. The other 31 entries account for 1333 bytes, used only to
exclude their record-write operands. Their names, factories, business logic
and continuations were not investigated.

Two selection heuristics were explicitly disproved and retained in the plans:
export-name order predicted slots 28/40, and record-layout distance predicted
26/27. Their actual name-field stores are `41CEA8`, `41CB48`, `41CDE8` and
`41CDB8`, respectively. Neither heuristic is a native ordering rule. Entity
and Object were accepted only after observing stores to `41CF38` and
`41CF08`, and later appends of `41CF30` and `41CF00`.

## Verification and restoration

- 19 fixed disassembly batches: **2558 code bytes / 640 instructions /
  46 bounded dumpbin calls**, within the cumulative 8192-byte code budget.
- New semantic metadata: **5506 bytes** = 4128 bytes of bounded exception-table
  lookups + 48 record + 166 export + 4 entry + 96 import + 1064 array.
- Every saved fragment passed byte-coverage/hash checks; every batch retained
  matching Engine/dumpbin before/after hashes. Whole-file fingerprints use
  64 KiB buffers and are separate from semantic-byte counts. dumpbin's
  internal I/O is not measured.
- This phase changed evidence/readers only. Product source and earlier
  checkpoints are preserved. No new compilation or product tests were run.
  Earlier 65-assertion results are historical and not claimed as this phase's
  verification.
- Independent review: [ENGINE-ROOT-INDEPENDENT-REVIEW.md](ENGINE-ROOT-INDEPENDENT-REVIEW.md).
- Recovery checkpoint: `.tooling/checkpoints/engine-root-writers-complete/manifest.json`;
  it depends on the earlier `class-registration-helpers-complete` checkpoint
  and its prior chain.

## Authorization boundary

The earlier action
`powershell.exe -NoProfile -NonInteractive -File src/task_items/build_class_registration.ps1`
was refused by PowerShell AuthorizationManager before compilation/testing.
That refusal remains in force. This phase did not retry that script, copy or
rename it, use an equivalent build command, or change policy. No build is
pending or of uncertain outcome in this phase; the refused action remains
unresolved. The allowed independent read-only metadata/dumpbin analysis above
completed normally.

No game/DLL execution, Rea, Java, extra installation, privilege changes,
saves/Steam-cloud access, restart, commit or push occurred.
