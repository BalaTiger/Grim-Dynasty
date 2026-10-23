# Independent Engine helper evidence

2026-10-20. Continuation of ENGINE-ROOT-WRITERS-RESULT.md, without product
implementation, build/tests, original execution, policy changes or retries of
the refused build. Every new original read targets the same pinned Engine.dll.
Game helper names and contracts are not used as proof.

## Direct edges and address-level behavior

| Writer target | Observed direct chain | Contract before import binding |
|---|---|---|
| IAT `302BB8` | Indirect call from all three writers | Receives the fixed string address; RAX is stored to name without a null-result check. |
| `2DC900` | `2DC900 -> 2DC398 -> 2DDB38 -> IAT 302968` | Returned zero enters the handler/retry path. The visible normal return is reached only with nonzero RAX. |
| `2DC8F8` | `2DC8F8 -> 2DC390 -> 2DDB3E -> IAT 302970` | Tail-jump chain; no destructor call, null guard or boolean normalization is added by these wrappers. |
| `2DC8E0` | `2DC8E0 -> 2DC890` | Maps a nonzero pointer return to 0 and zero to -1. |

The first helper-entry plan stopped before code/disassembly because `2DC900`
has no exact unwind start. The saved metadata delimits a gap through `2DC908`;
explicit 8-byte prefixes then captured the two direct JMPs and their padding.
This was a static boundary assertion, not an authorization failure or a
changed protection. The original failed report remains in the cumulative ledger.

## Allocation-like path, independently observed

`2DC398` keeps the request in RBX and first calls `2DDB38`. Nonzero RAX returns.
Zero RAX calls `2DDB7A -> IAT 302960` with the same request. Any nonzero EAX,
including a negative 32-bit value, retries the original size; it is not a
positive-only check.

Only when that handler call returns zero is request `UINT64_MAX` tested:

- all-ones request calls `2DD600`;
- every other request calls `2DD5E0`.

These wrappers construct a stack object and call `2DDB26 -> IAT 302708`:

| Failure branch | Constructor | Constructor's visible writes | Descriptor passed as RDX |
|---|---|---|---|
| Other size | `2DD508` | `[this+10]=0`, `[this+08]=180307488`, `[this]=180307478`, return this | `1803AF558` |
| All-ones size | `2DD568` | `[this+10]=0`, `[this+08]=1803074B0`, `[this]=1803074A0`, return this | `1803AF5B8` |

The wrapper has `INT3` immediately after the imported call and no ordinary
return instruction. Import identity is established separately; concrete
exception class identity is not inferred from adjacency or from Game.
The contents and semantic types of the referenced `+08`/`+00` data and
descriptors have not been followed by these code reads.

The first constructor attempt saved the valid six-byte `2DDB26` thunk and
stopped before `2DD508` because no exact unwind start existed; it never reached
`2DD568`. The saved boundary then established a 32-byte gap at `2DD508`, so
an explicitly bounded 32-byte prefix was read. Both constructor prefixes
contain a visible return followed by padding. The failed batch and its six
bytes remain counted.

## Registration-result path, independently observed

Let `cookie` be the QWORD loaded from `41C520`, `state` the QWORD loaded from
`420D50`, and `cb` the incoming callback pointer. `2DC890` calculates:

```
selector = ROR64(cookie XOR state, cookie & 63)
```

- If selector is all-ones, it calls `2DDBA4 -> IAT 302A70` with `cb`.
- Otherwise it calls `2DDB98 -> IAT 302A88` with `&state` and `cb`.
- If the external EAX is zero, it returns the original `cb`; otherwise it
  returns zero.

`2DC8E0` then returns signed 32-bit 0 for a nonzero pointer and -1 for zero.
The failure bit pattern in EAX is `FFFFFFFF`; the write to EAX zero-extends
RAX, so it is not a 64-bit all-ones return. Thus for the
three observed nonzero callback addresses it returns 0 exactly when the
selected external call returns EAX zero. A null callback remains an edge case:
even external EAX zero becomes -1 after the pointer normalization. No claim
is made that the external API accepts such a callback.

The runtime cookie/state values and their initialization are not read here.
No branch is selected merely from an on-disk initial value. The visible code
does not undo registration, free storage or roll back the writer's prior
changes on an error result. External state changes on failure remain external
contracts, not an inferred atomicity guarantee.

## Consequences for the root writer model

- A normal return from the allocation chain supplies a nonzero pointer. That
  does not establish allocation capacity, buffer validity or behavior of an
  installed custom handler. Handler side effects are possible before retry.
- The writer captures its parent after the string call, but before allocation.
  It rereads the selected parent's count/storage after allocation; a model
  must not freeze those fields across a callback-capable allocation path.
- Name publication precedes allocation; append/count increment precede the
  old-storage call; new storage publication precedes registration.
- A nonlocal allocation failure leaves the already-stored name. A later
  failure need not restore append/count/storage. No exception handling or
  rollback in the three writer fragments has been established.
- A registration return failure occurs after child-array publication. Caller
  treatment of that return and successful CRT iteration remain separate.

The exact imports, reader accounting and final limitations are recorded in
ENGINE-ROOT-HELPERS-RESULT.md after binding and independent review.
