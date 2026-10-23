# Navigation nearest production and initialization provenance

This stage extends the earlier navigation core. It is component recovery, not
R1 gameplay acceptance. Native evidence, own implementation and supplied test
data are separate below. Earlier stage reports remain historical snapshots.

## New native evidence

Actual Rea `procedure_pseudo_code`, `procedure_assembly` and `procedure_callees`
responses are retained, with each fixed batch's lifecycle, resource samples and
normal ownership cleanup in its summary:

| Evidence directory | New Engine RVAs | Completed calls | UTC on 2026-10-22 |
| --- | --- | --- | --- |
| `r1-navigation-nearest-helpers` | 261950, 260520 | 11/11 | 16:36:25–16:37:52 |
| `r1-navigation-nearest-dependencies` | 261320, 26D580, 01C510 | 14/14 | 16:44:15–16:45:43 |
| `r1-navmanager-constructor` | 150280 | 8/8 | 16:49:30–16:50:57 |
| `r1-navigation-implementation-constructor` | 10A530 | 8/8 | 16:56:59–16:58:26 |
| `r1-navigation-mesh-query-initialization` | 2664D0, 25F910 | 11/11 | 17:05:32–17:07:00 |

These are newly inspected bodies. Original Engine SHA256 before/after is
`4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`.
The original game was not executed. Each batch uses one384M backend with the
existing5GiB physical/commit launch floor,2GiB stop and identity/mutex guards.
Total:9 new function bodies,52 calls including25 lifecycle calls. No earlier
query was repeated. Exact summary hashes are pinned in the checkpoint sealer
and manifest; raw response files retain tool names, target digest and addresses.

Three bounded PE readers supplement the real decompilation: the nearest
absolute-value mask (4 payload bytes),26D580 barycentric bounds (8 bytes), and
the Game609C10 import/Engine export binding (156 metadata bytes). Their exact
read windows and both file hashes are in `r1-navigation-nearest-constants`,
`r1-navigation-26d580-constants` and `r1-navmanager-singleton-binding`.
Whole-file hash reads establish identity; they are not whole-binary searches.

## Recovered nearest-point behavior

261950 derives a tile rectangle from point/extents and mesh origin/tile sizes.
It enumerates Z then X, follows hash buckets, and retains the first32 matching
tiles per cell while still traversing the remaining chain. Each tile is passed
to261320. Count is written on normal/capacity return, not cleared initially.
Capacity return is40000010; ordinary return40000000. Reloads of query/mesh and
late fields are observable, rather than one immutable query snapshot.

261320 has both a linear polygon path and a quantized BV-tree path. Linear
enumeration applies polygon type, inclusion/exclusion flags and bounds. The
BV path uses ushort node bounds and signed negative escape indices. Quantization
uses float32 operations followed by64-bit CVTT conversion, then ushort masking.
Reference generation depends on the tile's real array position, salt and mesh
bit counts; arbitrary fixture reference numbers cannot replace this relation.
Capacity exhaustion does not skip remaining node/filter reads.

260520 validates the reference, handles the special two-vertex type, or copies
the original point before polygon containment/edge projection. Detail triangles
then optionally replace Y using26D580. No matching detail triangle leaves the
previous Y. The special branch uses a distance ratio, not an invented clamped
projection. Its write order differs from ordinary XYZ copies.26D580 preserves
the observed float32 operation order, tolerance bits and unordered comparisons.

2610F0 clears the required reference before enumeration, uses a128-reference
candidate buffer, ignores each closest-helper status and reuses closest-point
scratch across candidates. Stable finite ties keep the first candidate; the
actual COMISS branch also permits unordered replacement. Over-polygon distance
uses late tile climb state. Optional winning coordinates are written immediately,
but the final reference is written after the loop. Aliased input/output can
therefore affect later candidates. Empty results leave the optional point alone.

## Query and mesh initialization provenance

The Game IAT609C10 binding is now exact: exported
`GAME::Singleton<GAME::NavManager>::Get`, Engine RVA01C510. Its cold path uses
the observed critical-section/instance globals421380/4213B8, constructs a0x60
manager through150280 and publishes the returned pointer. This is static
publication evidence, not a verified native concurrent initialization.

150280 writes manager configuration and constructs a0x76F0 internal object
through10A530 at15036C, saving its returned pointer at manager+8. The existing
search path consumes implementation+10 as query and query+0 as mesh. Builder
and crowd constructors are identified but outside this bounded follow-up.
10A530 now establishes the next links. It writes the inline filter at+18:
64 cost floats initialized to1.0, and DWORD+100=`0000ffff` (includeFFFF,
exclude0000). It allocates and initially clears a0x68 mesh, stores it at
implementation+8 (10A910), then calls2664D0 (10A954). Mesh parameters at this
call have origin0, equal tile width/depth converted from configuration DWORD0,
maximum tiles1000hex and maximum polygons400hex. These values are constructor
evidence, not a populated navigation map.

It next allocates/clears a0x68 query, publishes it at implementation+10
(10A9A7), passes the mesh to25F910 (10A9B2), and continues to thread/event setup.
Neither initialization return is tested in the caller. The complete constructor
also invokes unexamined subobjects/cache/thread operations; reproducing its
entire runtime or synchronization is not claimed.

The subsequent2664D0/25F910 batch confirms the distinction. With these known
parameters, mesh initialization allocates0x68000 bytes of tile slots and0x2000
bytes of buckets, with bucketCount400hex/mask3FFhex and tile/poly/salt bit counts
12/10/10. Zeroing and reverse free-list construction leave every header null,
each salt1, and the free-list head at tile0. There is no populated scene tile.
Allocation failure returns80000004 without rolling back prior published
pointers. Insufficient salt bits return80000008 after array/free-list/bit writes.

25F910 writes query.mesh0 first (25F92C), then handles the large pool+58
(800/200hex), small pool+50 (40/20hex), and heap+60. Pool construction calls
26DC10; its inner allocations remain unexamined. The heap's inner0x4008-byte
allocation is stored at25FAB7 without a null check; success tests the outer
heap object. Thus a40000000 status does not prove every inner allocation valid.
The10A530 caller checks neither initialization status. These failures must not
be converted into valid defaults in a later own initialization implementation.
No thread, allocator or builder dependency was recursively queried.

## Own implementation and validation status

New sources are `NavigationSpatialState.cs`, `NavigationEnumerationContracts.cs`
and `NavigationNearestContracts.cs`. They extend the frozen Core identities
through explicitly attached mutable sidecars. Unknown fields do not acquire
default values. Own typed-array address declarations represent reference
generation without inventing native addresses. Own finite traversal limits,
released views, malformed tables and unsupported address overlap stop explicitly.

The formally approved isolated build ran17:08:22–17:08:23UTC: **27 scenarios,
286 assertions passed**, one build attempt, no failed compile/test, zero compiler
warnings.20 scenarios use default recovered leaves/own storage domains;7 are
explicitly labelled Mock boundary observations. The default chain includes
coordinate/extents→linear or BV candidates→closest detail height→nearest
reference/point→the existing same-poly corridor and straight endpoints.

Review found two early pointer-consumption errors before the first build. They
were corrected using pointer snapshots and a separate lazy point-address view:
ordinary gather reads polygon indices before vertex data; triangle LEA does not
read the formed point until26D580 consumes a component. New tests cover unknown,
missing and released data in the exact resulting failure prefix, as well as a
non-consuming Mock height boundary. No old successful tests were repeated.

The executed recipe is `tools/static_analysis/build_navigation_nearest_contracts.ps1`.
It compiles the unchanged Core source with the new sources against the frozen
Actions DLL. All older artifacts and suites, including34 core scenarios, remain
preserved and were not rerun. The output directory refuses overwrite; future
rebuilds need a reviewed fresh destination and normal execution approval.

- `.tooling/r1-navigation-nearest-validation/result.json`: input/compiler/artifact
  hashes and exit codes; stdout/stderr are saved separately and as combined logs.
- `GrimDynasty.NavigationNearest.dll`, SHA256
  `75f303c482679df6ba2c8f01aad7759ba9ed322ad6386100525b690cd9b9dfd0`.
- `NavigationNearestTests.exe`, SHA256
  `15d27dee12d8cb5726aabbca669141655b80820192a0c57da2fe7f3b0ac91e2d`.

The retained component artifact can be invoked from the repository, with normal
required execution approval:

```powershell
& .\.tooling\r1-navigation-nearest-validation\NavigationNearestTests.exe
```

It is not a game launcher. No original file is read by these tests.

## Remaining acceptance boundary

Self-owned meshes exercise recovered production leaves, not original map data.
Cross-polygon search/funnel, real query/mesh population, physical mouse target
projection and scene/render remain unclosed. Normal float32/default FP settings
and explicit typed storage are the supported implementation domain; arbitrary
native byte aliasing, thread races, ABI/EH and nondefault MXCSR are not claimed.
No navigation test is presented as a playable game or successful physical move.

The next practical boundary is real tile/header/vertex/detail/BV population and
its publication into the established mesh, then necessary cross-polygon search.
26DC10 is a confirmed query-pool dependency, not a reason to expand general CRT
work. It has not been queried in this stage. Mouse projection and actual scene
presentation remain independent prerequisites even after navigation closes.

Checkpoint/manifest are extended in place, without copying evidence trees.
The first sealing attempt stopped before writes because Windows PowerShell5.1
`Measure-Object` did not expose hashtable keys as properties. Its script and
failure record are retained in the validation directory. The sum expression
was corrected; sealing alone was retried, with no query/build/test repetition.
No original writes, real save/cloud access, security changes or commit/push.
