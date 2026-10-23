# ControllerAI state publication: bounded R1 evidence

The real Rea query at **Game 180123D50** establishes a writer of the controller's fallback action-state pointer. It does **not** establish the concrete registered state object, its mouse-action slots, or a working click-to-move path. No restored client implementation or gameplay acceptance is claimed.

## Actual query and provenance

After the earlier execution-channel disconnection, a same-channel read succeeded, the saved runner diff was reviewed, and frozen hashes matched. No query had started during the failed connection attempts. The offline verification was reused, not rerun.

The separately approved [fixed plan](../../tools/static_analysis/rea-r1-game-state-transition-plan.json) executed once from **2026-10-22 09:22:17.607 to 09:27:13.193 UTC**, with **8/8 calls passed**. Its only analysis operations were `procedure_pseudo_code`, `procedure_assembly` and `procedure_callees`, all at `0x180123d50`. The direct address source remains the saved call at `1801250CF` in GoToIdleState; no empty EXE query or raw EXE scan was repeated.

- [Pseudocode](r1-game-state-transition/04-StateTransition-0x180123d50-procedure_pseudo_code.json): **4,091 characters**, evidence `ev_148e05bcb2d7315b275fee6d19cb86f405c160c197504c155fef0254bc087515`.
- [Assembly](r1-game-state-transition/05-StateTransition-0x180123d50-procedure_assembly.json): evidence `ev_2c21a80466a1184f7c6608421fe27e87604d59ee30b74c906a6c71c9fe16540b`; file SHA256 `4dab3f1268e102b2723ea1cb75db8027f932cc3eec451a16d8a7f0595736519a`.
- [Callee inventory](r1-game-state-transition/06-StateTransition-0x180123d50-procedure_callees.json): evidence `ev_32d450529410127471a1343f074ac27bb43d36802377280baf18b53d4cc52ebe`. Coverage is explicitly **partial**: three computed calls have no resolved destination in that inventory. Its eight entries are not a complete call graph.
- Provider: installed **Rea 6.5.0 / Ghidra 12.1.4**. Export/decompiler label: `ControllerAI::SetState(const string&, const ControllerAIStateData&)`. Assembly controls the width/order conclusions below; decompiler types and names are not a full ABI recovery.
- Original `Game.dll` SHA256 before and after: `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`.

The [summary](r1-game-state-transition/summary.json), SHA256 `09766ab179645d987328e020bbf5dd0c8fc63ab042dfb2d2cffc41c3ae65e940`, confirms close acknowledgement, closed session, backend exit code 0, empty owned Job, sampler exit code 0 and no failures/forced termination. No `rea-ghidra-*` runtime directory remained. The observed 384M heap and resource gates were unchanged. Sampled total task working-set peak was **1,309,265,920 bytes**, minimum available physical memory **11,511,144,448 bytes**, and minimum commit headroom **27,876,028,416 bytes**. These samples do not bound later queries. The batch has released its backend resources.

## What the function actually establishes

Let `C` be the original controller, `N` the incoming name argument, and `D` the incoming state-data address. Assembly retains these as RSI, R14 and R15 respectively.

### Selection and missing-state path

`123D7B` calls **180078960** with RCX=`C+290`; RDX still holds the incoming name. The helper's search/order contract is not yet known. The function reads sentinel-like qword `[C+290]` **after** this call. If the returned pointer differs, it calls **180019E40** with `(N, returned_node+20)`. A negative EAX converts the result to the sentinel; nonnegative proceeds. Without the first helper's contract, this must not be simplified to an independently proven exact-name equality test.

A sentinel result calls the imported engine object's virtual `+18` with severity 2 and a missing-state format string, then returns. There are no explicit controller-state writes on this local path; lookup/comparison/logging effects remain dependencies. It is not a demonstrated side-effect-free Boolean failure.

### Flag and lifecycle paths

For an accepted node, `123DD2..123DD9` save BYTE `C+2D0` to BPL and clear that byte. The QWORD `C+2F0` is tested once at `123DE0`. This branch decision is **not** reevaluated after callbacks.

| Entry branch | Observed control flow |
| --- | --- |
| `C+2F0 != 0` | Copy the incoming 40 bytes, publish the fallback pointer, restore saved flag. No old `+228` or new `+220` call in this branch. This does not remove or replace the separately selected overlay object. |
| `C+2F0 == 0`, old `C+2A0 == null` | Copy data, publish pointer, call new object's `+220`, then restore saved flag. |
| `C+2F0 == 0`, old `C+2A0 != null` | Call old object's `+228`. Re-read BYTE `C+2D0`; if nonzero, return immediately without the outer copy/publication or flag restoration. |
| Old callback returns with flag zero | Set local BPL=1; perform the data-dependent release sequence, then copy/publish, call new `+220`, and finally write BYTE `C+2D0=1`. |

The virtual slot names are kept numeric: their full lifecycle semantics and concrete destinations are unknown. There is no local old/new pointer equality test to skip them. Calling the same stored object again cannot automatically be treated as a no-op.

### Data-dependent release sequence

On the last branch, `180125A00(C)` is called and its first DWORD is compared with the current first DWORD at `D`. Only a mismatch enters the release sequence. The getter is called again for the first DWORD and later again for DWORD `+4`; its pointer and data must not be cached across these calls.

Each path uses an imported ObjectManager getter and **180019190** lookup. For a nonnull result it calls **180078D40(C)**, an imported object-ID getter, and **18055D830** with the looked-up object's qword `+2838` or `+2840`. The second path is not guarded by a separate comparison of the second incoming DWORD. Full lookup, object lifetime, slot-release and getter semantics remain unknown. No speculative default value is supplied for them.

### Exact copying and late pointer publication

Both branches perform the same memory sequence:

1. Read 16 bytes at `D`; write them at `C+2A8`.
2. Read 16 bytes at `D+10`; write them at `C+2B8`.
3. Read 8 bytes at `D+20`; write them at `C+2C8`.
4. Only then read QWORD `selected_node+40` and write that pointer to QWORD `C+2A0` (`123ED6` or `123F11`).

The actual copy is **16 + 16 + 8 bytes**, despite the pseudocode rendering part of one branch as smaller fields. The input address and selected node address survive intervening callbacks in registers, but their contents are read later. This neither proves ownership nor lifetime. An implementation must not snapshot all 40 bytes or the node's state pointer before callbacks; partially overlapping input can also distinguish sequential chunks from a generic `memmove`.

On the zero-`+2F0` branch, the newly published object is called at virtual `+220`. After normal callback return the final byte write restores BPL. Thus that callback's modification of `C+2D0` is overwritten, whereas any callback modification of `C+2A0` is not republished by this outer invocation. Exceptional paths and rollback are not established; normal-path evidence does not license transactional cleanup.

## Relationship to mouse dispatch and remaining gap

The preceding [HandleActionFromMouse evidence](R1-MOUSE-OBJECT-SOURCES.md) selects `[C+2A0]` only while qword `C+2F0` is zero. Otherwise it follows `[C+2E8] -> [container] -> [node+10]`. After the first mouse callback at `+1C8`, it selects again before `+1B0/+1B8`.

SetState now proves a **fallback pointer writer**, but contains no direct write to `C+2E8` or `C+2F0`. It does not supply a concrete state vtable. The existing ControllerAI constructor only initializes the `C+290` structure and publishes the separate PreStart pointer; that is not evidence of a later named registration's `node+40` value. No active state object or mouse-slot target is guessed from the name `Idle` or `SetState`.

The bounded next identity gap is the **registration writer that puts a concrete state object into the `C+290` lookup structure's node+40**. Locate that writer from existing direct initialization/registration references before selecting another native body query. The known `180078960` lookup and `180019E40` comparison are direct dependencies if lookup semantics block that identity mapping, but they are not substitute proof of the registered object. Do not expand allocator/string internals or rerun the empty EXE xrefs merely for coverage.

## Candidate acceptance cases; not executed tests

Once registration/object identity and its actual mouse slots are established, a narrowly scoped model can test: missing-state handling without fabricated state; nonzero-`+2F0` fallback publication without lifecycle calls; old-state callback reentry and early return; late node+40 mutation visibility; sequential chunk overlap; same-object transitions; incoming data changes across callbacks; and entry-callback changes followed by the final flag write. Unknown native dependencies must remain explicit, not silently implemented by fixture callbacks and presented as recovered gameplay.

No new model or tests were added in this checkpoint because concrete active-state identity and mouse slots are still missing. The earlier single offline plan check was not repeated after reconnection; old suites were not run. This is new native evidence, **not R1 functional acceptance**: the full original click movement chain and real scene/resource presentation are still required. No original game, save, cloud data, security setting, commit, push or deployment was involved.
