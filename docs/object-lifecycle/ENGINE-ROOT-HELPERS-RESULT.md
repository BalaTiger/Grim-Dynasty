# Engine root helper contracts: static result

2026-10-20. HEAD `6dfdae419d1c92f7738eefe241937057d83134df`.
Continuation of `engine-root-writers-complete`, whose manifest SHA256 is
`f92e6ae9935d987c57d9cf4b154ca4e80b0b3edbf4e308eeca30d116a1f2c97c`.

## Outcome

The four requested Engine targets now have independently established static
call chains and local return/failure behavior. No Game helper contract was
used to identify an Engine function. No product implementation or execution
validation is claimed.

| Requested target | Independently bound contract |
|---|---|
| IAT `302BB8` | `_strdup` in `api-ms-win-crt-string-l1-1-0.dll`. The writer stores its returned RAX to name without testing for null; if it returns zero, that zero is stored and parent processing continues. |
| `2DC900` | Tail-jumps to `2DC398`: `malloc(size)`; only a zero return calls `_callnewh(size)`. Any nonzero 32-bit handler result retries the same size. Handler zero selects one of two `_CxxThrowException` wrappers according to `size == UINT64_MAX`. The visible normal return requires a nonzero pointer. |
| `2DC8F8` | Two tail jumps reach imported `free`. The wrappers add no destructor, storage clearing or boolean-result conversion. |
| `2DC8E0` | `2DC890` chooses `_crt_atexit` or `_register_onexit_function` from a runtime decoded selector, then normalizes the selected external result through the original callback pointer. For the observed nonzero callbacks, external EAX zero yields signed 32-bit 0; any nonzero external EAX yields -1. |

The branch selector is independently observed as
`ROR64([41C520] XOR [420D50], [41C520] & 63)`.
All-ones chooses `_crt_atexit(callback)`; otherwise the call is
`_register_onexit_function(&420D50, callback)`. No runtime branch is inferred
from disk contents. A null callback, if supplied, still reaches the selected
external call but produces -1 even when that call returns zero; acceptance of
such input by the CRT is not claimed.

`2DD5E0` and `2DD600` construct stack data and **call** the imported throw
entry, followed by `INT3`; they are not tail calls and have no ordinary RET.
The constructors' visible 32-byte prefixes write three fields and return
`this`, with no callee. Their descriptor arguments are respectively `3AF558`
and `3AF5B8`. Concrete exception types, descriptor layouts, payload text and
exception unwinding remain unverified. No `bad_alloc`/`bad_array_new_length`
identity is asserted.

The failure return of `2DC8E0` is EAX=`FFFFFFFF`, with zero-extended RAX
`00000000FFFFFFFF`, not a 64-bit all-ones value.

## Failure propagation into the root writer

The independent findings refine the earlier conditional writer model:

1. Name is published before child-array allocation. A nonlocal allocation
   failure therefore leaves that prior name store; no rollback was found.
2. `_callnewh` can be reached during allocation, so freezing count/storage
   across allocation is not supported. The request size is retained, while
   the writer rereads the selected parent's fields after allocation returns.
   The helper's normal nonzero return does not prove buffer capacity or make
   corrupt/mutating counts safe.
3. Append and count increment precede `free(oldStorage)`. New storage is
   published after that call returns. The wrapper does not translate a fault
   or other nonlocal exit into a handled false result.
4. Exit registration happens after publication. A returned registration
   failure does not undo name, count, append or children storage in the
   visible code. The raw external API's state changes on failure are not
   independently established here.
5. The writer's tail return is not proof of how CRT startup treats it.
   `_initterm` implementation, callback iteration, successful startup and
   exceptional cleanup were not executed or analyzed in this phase.

## Remaining model gaps

- Concrete throw-type identity and native EH/unwind cleanup; the project has
  no previously verified parser for the relevant MSVC descriptor layouts.
  Data addresses and constructor shape alone are insufficient.
- Runtime initialization of the selector state, registered callback bodies,
  and actual loaded CRT resolution/behavior, including custom new-handler
  effects. Binding names is not execution of external libraries.
- The three name string contents and tail callbacks remain unread; full
  sibling registration, other writers and complete root traversal are open.
- Native constructors/Read behavior and formal sample compatibility remain
  unverified. Original save compatibility is outside the independent
  project's goal.

The next model can encode these local contracts as explicit dependency
outcomes, preserving the observed ordering and partial states. It must not
silently supply a whole Engine root graph or promote a local fixture to native
compatibility. No further original-code scan is required to state the gaps.

## Evidence and accounting

- [Instruction-level reasoning](ENGINE-ROOT-HELPERS-EVIDENCE.md).
- [Exact import metadata](offline-engine-root-helper-imports/metadata.json),
  SHA256 `0b81751351efeaeb5bc8cd5103035887b5ffbd4798d29edf2be50c697f84d369`.
- [Independent review](ENGINE-ROOT-HELPERS-INDEPENDENT-REVIEW.md),
  SHA256 `b50ee0eee34bdeff591717d14d1fb37fd0bab7632ee9c5d85831be9c4f15eabd`.
- Six fixed code-analysis batches: **350 bytes / 101 instructions /
  16 bounded dumpbin calls**. The root-investigation ledger is now
  **2908 / 8192 code bytes**, including all failed-batch reservations.
- New semantic metadata: **1474 bytes** = 1176 bytes of bounded exception
  table probes + 298 exact import bytes. The metadata reader reused 54 bytes
  of previously archived descriptor/module data and did not reread headers.
- Two batches retain `failed` status from conservative unwind-boundary
  assertions. One read no code; the other saved one valid six-byte thunk
  before stopping at the next target. Explicit bounded gap prefixes then
  completed the required targets without disabling a guard. These were not
  authorization denials, hidden retries or uncounted reads.
- A legacy round-4 record was discovered after the new reads to contain the
  malloc/free thunk bytes. Those 12 repeated bytes are included in the 350
  byte count; they are not new discovery. That old record's named malloc
  summary lacked raw descriptor/OFT/name data, so the new exact 298-byte
  binding operation legitimately fills that evidence gap.
- Same-handle before/after hashes retained the pinned Engine SHA256 on every
  operation. Fingerprints stream in 64 KiB buffers and are counted separately
  from semantic bytes; dumpbin internal file I/O is not measured.
- Product source and previous evidence are preserved; no product tests were
  run. Recovery checkpoint:
  `.tooling/checkpoints/engine-root-helpers-complete/manifest.json`, dependent
  on `engine-root-writers-complete` and its prior chain.

Original, read-only:
`D:\Steam\steamapps\common\Grim Dawn\x64\Engine.dll` (4,485,632 bytes).
Unchanged SHA256:

```
4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153
```

## Execution restriction

This phase required no restricted implementation execution. The earlier
`powershell.exe -NoProfile -NonInteractive -File src/task_items/build_class_registration.ps1`
remains refused by AuthorizationManager; it was not retried, copied, renamed
or replaced by direct compiler/test commands. No policy change occurred.

A future implementation validation would require compiling the changed own
root-registration model and running targeted own fixtures for null string
results, handler retry/throw outcomes and exit-registration failures. That
would be build/test execution currently prohibited by the parent instruction;
it must be separately resolved before executing any such command. Nothing is
pending or of uncertain outcome here.

No game/DLL execution, Rea, Java, extra installation, escalation, saves or
Steam-cloud access, restart, commit or push occurred.
