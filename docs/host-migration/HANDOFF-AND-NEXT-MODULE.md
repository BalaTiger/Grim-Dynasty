# Handoff verification and next object-lifecycle module

Recorded: 2026-10-19. Workspace: `C:\Users\zhuzi\Documents\Grim-Dynasty`.

This document records the completed read-only handoff inspection and the limits of the existing static evidence. It does not replace historical evidence or claim that the old host's tests have passed on this host.

## Migration conclusion

The Object ID handoff is present and its source changes are already incorporated into the current checkout. Do not reapply `uncommitted.patch` or roll back to the old baseline to repeat the migration.

- Inspected HEAD: `6dfdae419d1c92f7738eefe241937057d83134df` (`check point`).
- Its direct parent: `da38ff671e1af25346a976e92b5f7ae93aab733f` (`demo`).
- The supplementary commit is dated `2026-10-19T01:39:49+08:00` and incorporates 21 changed files, including Object ID source, tests, scenario, build changes, backups, and both handoff archives.
- The initial inspection found a clean worktree and local cached `origin/main` equal to HEAD. No remote refresh was performed. Subsequent host-portability work is separate from this initial observation.
- `docs/object-lifecycle/SHUTDOWN-HANDOFF.md` correctly describes the earlier shutdown snapshot at `da38ff6`, when the changes were uncommitted. Its warning is historical, not evidence that the present checkout lacks those changes.

## Archive and file verification

`Grim-Dynasty-object-id-handoff-v2.zip` is 87,058 bytes. Its measured SHA256 exactly matches `docs/object-lifecycle/handoff-v2-sha256.txt`:

```text
35e9ca95efb230eee040d5ef34cb2de11ac32702ce23511fd36ec08c6511a5a1
```

The ZIP was inspected in memory without extraction or overwriting files.

| Check | Result |
| --- | --- |
| ZIP entries | 47 unique normalized paths |
| Unsafe absolute or parent-traversal entry names | None |
| Migration manifest entries | 43 raw entries, 39 after normalizing separators |
| Conflicting duplicate manifest entries | None |
| Manifest entries present in ZIP with matching length and SHA256 | 39 of 39 |
| `project/` entries | 41 |
| Current project files exactly equal to archived bytes | 5 |
| Current project files differing only by LF/CRLF | 34 |
| Current project files with other content differences | 0 |
| Project entries absent from current checkout | 2 host-specific tooling records |

`core.autocrlf=true` explains the checkout's newline differences. A raw working-tree SHA256 mismatch must not be treated as lost source without checking this conversion. The original archive remains the byte-exact reference.

The two absent project entries are retained intact in the ZIP:

- `.tooling/installed-paths.json`: old Ghidra/JDK locations under `D:\MyGitRepo\Grim-Dynasty`; these are not valid destination-host discovery results.
- `.tooling/offline-system/manifest.json`: old host's generated-output hashes and environment evidence; it is not a claim that the destination has those output files or has passed their checks.

The six `workspace/` entries are also present: two Rea plans, two JavaScript runners, an object-creation export candidate list, and a save-interface export candidate list. They remain references inside the archive. The runners contain old project/game paths and an old account's npm-cache path; do not execute them unchanged.

## Validation scope and host portability

The archived build log's assertion counts sum to **894**, including **20 new Object ID assertions**. It records **16 standalone scenarios** and **10 own modules**. These numbers agree with `acceptance-object-ids.json` and `OBJECT-ID-COMPLETION.md`.

This handoff inspection did not build or execute the code. **894/16/10 is historical evidence, not destination-host validation.** Any later local build must be reported with its own output and environment details.

The historical build scripts use old `D:\MyGitRepo\Grim-Dynasty` paths and a strict Windows 10 System32 UCRT fingerprint. Destination path adaptation and Win11 runtime validation must be handled explicitly before claiming equivalent results. The archived UCRT hash is:

```text
3c60056371f82e4744185b6f2fa0c69042b1e78804685944132974dd13f3b6d9
```

The historical static-analysis target fingerprints recorded in acceptance are:

| Target | Historical SHA256 |
| --- | --- |
| `x64/Game.dll` | `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2` |
| `x64/Engine.dll` | `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153` |
| `resources/Quests.arc` | `cff68edb899b5b6a84c4d91e651ab52c558b8ed5e1c9813f443b1844c19e385d` |

Those are reference hashes, not measurements made by this document's handoff inspection. Resolve the actual local installation independently and compare its files before reusing address-specific evidence.

## Implemented scope

`src/task_items/ObjectIdAllocator.cs` implements the statically derived `CreateObjectID` state machine: counter and sentinel handling, sticky wrapped mode, raw-key collision lookup, network prefix/fallback, and serialized allocation. It does not reserve an ID in the native indexes.

The runtime inventory scenario explicitly supplies an own fixture descriptor. That descriptor and its dictionary insertion are not native construction, native registration, record `Read`, generation, or saving. These unsupported areas must remain explicit.

## Existing creation evidence

The principal evidence files are:

- `docs/rewards/rea-object-ids/ObjectManager-CreatePartial.json`: Engine `0x18017E170`, evidence `ev_e635e31ef2c093d6996584dcd5784950442d1e8172b0cab5a9bc5ea81ba930b1`.
- `docs/rewards/rea-object-ids/ObjectManager-FinishPartial.json`: Engine `0x18017E230`, evidence `ev_12fd3d414d2756fc7d41d2af9963890b45a7f5bfbe05015dce56248d0e5b6ed2`.
- `docs/rewards/rea-item-creation/Item-CreateItem-replica.json`: Game `0x180324120`.
- `docs/rewards/rea-item-creation/Controller-CreateItemInInventory.json`: Game `0x18012B610`.

The two ObjectManager files contain observed Ghidra 12.1.4 pseudocode, with no raw-result limitations reported. They do not include an assembly cross-check or complete callee evidence: `relationship_coverage` is null and `locations` is empty. The archive plans cover exactly the 11 already-recorded queries; they do not contain additional factory or `Read` function bodies.

`CreatePartial` shows this sequence:

1. `TableDepot::LoadFile(manager + 0x170, recordName)`.
2. If the result is null, return null.
3. Call LoadTable vslot `+0x28` with `"Class"` and default `""`.
4. Call Engine `FUN_180274EC0(&Object::classInfo, className)`.
5. If factory creation failed with a nonempty class name, issue a diagnostic; otherwise, on success, call `Object::SetObjectName`.
6. Return the factory result.

`FinishPartial` first returns immediately for a null object or zero ID. For a nonnull object and nonzero ID, it loads the record identified by the object's name. Its successful record-load path is ordered as follows:

1. Write `Object + 0x2C = ID`.
2. Enter the manager's critical section.
3. Call `FUN_18017FCA0(manager + 0x128, ID)`.
4. Call `FUN_1801356F0(manager + 0x30, { ID, object })`.
5. Call `FUN_1801356F0(manager + 0x70, { Object + 0x28, object })`.
6. Leave the critical section.
7. Call the object's virtual slot `+0x08` with the loaded table.

The second index's observed key is `Object + 0x28`; it must not be replaced by a guessed name-string key. The operation performed by `FUN_18017FCA0` is not established merely by its position in this sequence. Duplicate-key behavior and the effects of the final virtual call are still unknown.

## Next module and evidence gaps

The only directly implementable new boundary from existing evidence is the `FinishPartial` null-object/zero-ID no-op guard. It performs no record load, ID write, locking, registration, or virtual call. A known-null `LoadFile` result also identifies a failure path, but does not establish filesystem lookup semantics or the diagnostic implementation. These narrow boundaries do not amount to a successful factory or registration module.

For meaningful successful creation/registration, obtain new static queries before writing the implementation:

| Query target | Evidence required |
| --- | --- |
| Engine `0x180274EC0` | Class lookup, inheritance matching, factory dispatch, failure rules, and selected constructor |
| Engine `0x18017FCA0` | Meaning and mutation rules of the container at manager `+0x128` |
| Engine `0x1801356F0` | Both indexes' key rules, duplicate handling, insertion/overwrite behavior, and failures |
| Engine `0x18017E170` and `0x18017E230`, assembly | Verify branch conditions, call signatures, and write/call ordering |
| `TableDepot::LoadFile`, address to resolve | Record lookup, caching, ownership, and failure semantics |
| LoadTable vslot `+0x28`, target to resolve | `Class` getter, default behavior, and string lifetime |
| `Object::SetObjectName`, address to resolve | Name storage and relationship to the field at `Object + 0x28` |
| Selected class constructor and object vslot `+0x08` | Initial state and complete record-read effects |
| Game `0x18012C910` | Shared creation helper; the two existing caller pseudocodes even show differing apparent argument counts |
| `SetItemReplicaInfo` and Item vslot `+0x3F0`, if advancing the Item path | Replica writes and item initialization |

The project search found callers but no verified bodies for these missing callees. Earlier bounded `InitializeObjects` evidence concerns group affiliation and explicitly lacks full auto-analysis/callee coverage; it does not close these gaps.

Recommended next step: query the smallest class-factory and registration dependency set above using an authorized static backend, choose one concrete supported class/record branch, and keep unverified branches `Unsupported`. Do not use a supplied descriptor or a C# dictionary insertion as a substitute for native registration.

## Inspection boundaries

This verification performed no game execution, save or Steam-cloud access, extraction over existing files, installation, source changes, commit, or push. No applicable `AGENTS.md` was found during the scoped instruction search. No approval was denied or pending during the read-only inspection. The remaining creation blocker is missing static evidence, not a failed or uncertain installation action.
