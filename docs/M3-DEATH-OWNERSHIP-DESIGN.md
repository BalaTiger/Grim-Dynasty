# M3 death ownership bridge - design only

Status: **Not implemented**. Date: 2026-10-22. This document records a read-only design review and a proposed bounded implementation. It records no new build, test, window run, native execution or verified combat death. The accepted patrol checks are not repeated.

The native attack/damage/death producer is still incomplete. The next bridge can be developed with explicitly supplied **external fixture death observations**. Such observations test the consumer, ownership, replay and persistence contracts; they do not establish that an attack killed a native character. The interactive scene must retain its unsupported attack result. No default HP, damage, merchant price or weapon class is introduced.

## Existing contracts that determine the design

- `src/task_kills/KillCondition.cs:57-65` consumes an event body containing source ID, victim ID and record name. It does not prove death, identify the current session's player, or deduplicate deaths. Every matching repeated body can increment the counter again. A Player source qualifies even if it is not the session's main player. A Monster source can resolve through its owner ID. Unknown required source/victim information is Unsupported.
- `KillCondition.cs:38,65` normalizes configured names, then compares the incoming event name with ordinal equality. The bridge must not silently normalize an incorrectly supplied event name. Its narrow profile requires an explicitly registered victim; it does not exploit the consumer's known-missing-victim branch.
- `KillCondition.cs:42-55` provides a usable Receive/Evaluate boundary. Complete `AutomaticTasks.Update` has an unresolved cinematic notification path when the clock gate opens. The bridge will not freeze the clock or call that full task update as if it were recovered.
- `src/task_auto/OfflineEvents.cs:55-65` delivers on a live, reentrant subscriber list. Delivery can stop after earlier subscriber effects. The first bridge must have a private, known condition graph; it must not broadcast to arbitrary callbacks and assume rollback.
- `src/session_runtime/SessionState.cs:41-49` rebuilds a candidate model from the journal before every ordinary action. `View()` at line 95 returns a new DTO; authoritative money is `model.Base.Wallet.Money16C4`. A side wallet, a write to `View().money`, or a direct unjournaled live-wallet change will not survive subsequent commands and deterministic reload.
- `SessionState.cs:85-91` rejects duplicate/nonchanging journal commands. `Payload`, `StateHash` and `FromPayload` currently encode and replay four string commands under schema 1 / `liaoxi-m2-inventory-v1`.
- `src/rewards/MoneyRewards.cs:15-24` is the reusable money primitive. `Known(false)` may mean a successful capped reward. It can also mean an unresolved-to-the-main-player operation did nothing, so the bridge must establish actual wallet/player ownership before invoking it. **Amount zero clears the wallet**; an empty reward plan, not amount zero, represents no reward.
- `src/scene_runtime/SceneState.cs` already commits the nested session, player position and visual agent snapshots together. It checks character ID, revision and fingerprint; flushes and validates a temporary file; then replaces the canonical save. Current outer format is schema 2 / `liaoxi-scene-v2`.
- `src/scene_runtime/SceneAgentPersistence.cs:7-14` explicitly defines a visual patrol, not a Monster initializer or combat profile. That patrol must not be relabeled as a killable native entity merely to exercise this bridge.

## Smallest supported domain

Use one explicitly activated own objective and a small fixed external fixture entity registry. All configuration is immutable and identified by a definition version/hash:

- One session character ID and one encounter ID.
- Stable own entity IDs and victim generations. These are fixture identities, not a claim to native object-ID allocation.
- Explicit source-to-owner bindings, entity kinds, required raw condition fields and exact record names. They come from the fixture definition, not from unchecked claims in an incoming observation.
- A positive, bounded objective threshold within the signed comparison domain supported by HasKilled.
- An explicit reward plan resolving to already supported `MoneyAction` operations. Test values must be labeled own fixture values; no amount becomes an inferred native reward or gameplay default.

The minimum profile can admit direct Player sources only. Owned-Monster attribution remains a separately admitted profile after its owner binding is supplied and tested. Unknown producers, profiles, fields or record bindings return Unsupported. Wrong character/encounter/source ownership is rejected without consuming a receipt.

A fixture observation is an explicit input assertion, not a cryptographic or native-runtime proof. A checksum establishes saved-data consistency, not that a real attack happened. Do not add an interactive key, public CLI action, or generic `verified=true` switch for creating these observations.

## Proposed interfaces

Names below are proposed, not existing APIs:

```text
SessionDeathProfile
  immutable profile ID/hash, objective ID, encounter ID, entity registry,
  required count and fixed reward plan

ExternalDeathObservation
  character ID, encounter ID, profile ID/hash,
  source entity ID, victim entity ID, victim generation, exact record name,
  origin = ExternalFixture

SessionState.BeginDeathObjective(profile)
SessionState.PrepareExternalDeath(observation) -> candidate + result
SceneStore.CommitExternalDeath(slot, expectedScene, observation) -> saved candidate + result
```

The admitted fixture profile is supplied by compiled test setup, not populated from arbitrary observation fields. Production has no admitted damage/death producer yet and returns `CombatDamageProducerNotIntegrated` (or an equally explicit Unsupported reason).

`PrepareExternalDeath` constructs a complete candidate from the same ordered session journal. It does not mutate the live graph. `CommitExternalDeath` carries the existing character ID, revision and fingerprint into a candidate scene, copies valid player/agent state, saves once, and only then returns a candidate for the caller to publish. Creating a fresh character or resetting the revision is not a clone operation.

## Once-only processing and reward ownership

The authoritative death key is `(encounter ID, victim entity ID, victim generation)`. A producer message ID alone is insufficient: retries could use a new message ID for the same death. Store the accepted source/record/profile facts associated with each death key.

For each observation:

1. Validate the current character, encounter, activated objective, admitted producer/profile and exact registered entity identities. Reject source ownership mismatches before treating a receipt as a harmless duplicate. Do not fill absent fields with zero, Player or Monster defaults.
2. If the death key already exists with the same admitted facts, return `AlreadyApplied` without appending a command or delivering Event 11 again. If the same key carries conflicting source/record/generation facts, reject it explicitly.
3. On the candidate graph, construct the known `KillWorld` and `KillCondition`, register the condition through its supported Start contract, and feed the typed body through the private Event 11 route or its Receive boundary. Check the actual result; a dispatched handler count alone does not mean the counter changed. Evaluate the condition using the known comparison contract. No clock value is needed for this Receive/Evaluate path.
4. If the goal becomes satisfied and has no reward receipt, execute its fixed reward plan against **the candidate's `model.Base.Wallet`**, retaining the same world, inventory, item and wallet ownership graph used by M2. Require the resolved main player and wallet owner to match the session before executing. Do not reuse `deliver`, remove rations, or create a second wallet to obtain a reward.
5. After a Known reward result, record the own reward receipt even when the wallet amount did not change. The operation's `Changed` includes the new death/objective/receipt state, not merely the money delta. Empty reward plans still complete the own objective once. Unknown reward behavior aborts the candidate, so the accepted event can be retried without a live partial grant.
6. Append one canonical external-death command whose deterministic replay reproduces the count, goal transition, money effect and receipt together. Publish only after a successful scene save. Repeated completed-objective observations do not restart the objective or re-register rewards.

The reward key is tied to character, encounter, objective and definition version. It is an own one-shot ledger; it must not masquerade as a recovered native quest-history row. Do not append a fabricated task UID to the M2 quest's `RewardTaskUidsAt50`.

Candidate isolation is the project's admission/commit policy. It is not a statement that native callbacks roll back their partial effects. The first implementation excludes arbitrary subscribers and unsupported native callback prefixes rather than discarding their effects and claiming native equivalence.

## Exact Session schema and journal changes

Introduce **Session schema 2**, content **`liaoxi-m3-death-bridge-v1`**, for the bridge-enabled session. Preserve the eight existing envelope payload keys (`schema`, `content`, `commit_id`, `character_id`, `character_name`, `revision`, `commands`, `state_sha256`); change `commands` to a strictly validated ordered array of objects:

```text
{ kind: "m2", action: <one of the existing four actions> }
{ kind: "begin-death-objective", profile_id, profile_sha256, encounter_id, objective_id }
{ kind: "external-death", observation: <the exact admitted fields above> }
```

There is no public arbitrary `grant-money` command and no separately ordered reward journal. The external-death command deterministically invokes the configured reward when its goal first becomes satisfied. This preserves the order of reward effects relative to ordinary M2 commands; arithmetic ordering must not be assumed interchangeable.

Extend the candidate graph with the own objective, registered condition, accepted death facts and reward receipts. Reconstruct all of them during replay. Keep their authoritative state in Session, alongside the wallet, rather than in a Scene-only side ledger. Scene can display immutable views of these values.

Extend `StateHash()` to cover definition/profile binding, encounter and owner identity, ordered accepted deaths, condition count and relevant dirty flags, own objective status, reward receipts, and existing M2 state. Reconstructed receipts/counters must agree; do not load independent cached `count`, `complete` or `rewarded` flags as authority. Enforce exact key sets, integer widths, string domains, known command kinds, duplicate/conflicting death handling and command limits.

Retain the 4096-command bound and the existing 256 KiB scene-save limit. Structured commands are larger than the old strings, so reject an oversized candidate before publishing or acknowledging it; increasing the count limit is outside this bridge.

Legacy handling must be explicit:

1. Keep the current schema-1 reader and its old state-hash calculation intact. Validate a legacy save using its original four-string journal and original hash before conversion.
2. When explicitly enabling the bridge profile, convert each legacy command to an equivalent `kind: m2` entry in the same order. Preserve character ID, wallet/items/tasks, store revision and expected old fingerprint. Begin with no accepted deaths and no reward receipts; activation is an explicit new command.
3. Persist schema 2 only on the new successful commit. Merely reading an old save must not award anything, invent a completed goal or rewrite the file.
4. Unknown session schema/content/profile is Unsupported. Malformed known data is corruption. Do not silently interpret missing bridge fields as an empty ledger.

The current outer **Scene schema 2 may remain unchanged** because the bridge state is nested within the versioned session journal. Its nested session reader must intentionally accept both supported formats. The first fixture must not add fake combat tombstones to the visual patrol. If later verified combat requires separately persisted actor lifecycle fields, define a new outer scene schema and cross-check those fields against the same session death facts at that time.

## Durable commit, reopening and recovery

Prepare the candidate without changing the live scene; check source/ownership and replay invariants; call the existing locked, fingerprint/revision-checked scene save; publish/acknowledge only after success. Keep scene state and session reward in the same canonical file and replacement operation. Do not write a receipt sidecar before or after the wallet save.

After a known save failure, retain the previous live scene and let the caller retry. After an uncertain outcome, reload the canonical file and inspect the stable death key/reward receipt before replaying. A stale writer must report conflict, not overwrite a newer receipt. A normal restart reconstructs the same result and a retry returns `AlreadyApplied` with no new reward.

Explicit `.bak` recovery rolls the whole scene/session timeline back together. Exactly-once here means normal processing/retry/reopen of the current canonical timeline; it does not promise a non-rollback receipt ledger across explicit backup restoration. A stronger cross-recovery guarantee would require a separate durable policy and is outside this minimum.

## Source change boundary for implementation

| File | Required change |
|---|---|
| New `src/session_runtime/SessionDeathObjective.cs` | Typed external fixture observation/profile, ownership admission, stable death keys, own objective/receipt state, Receive/Evaluate bridge and replay helpers. No attack/HP producer. |
| `src/session_runtime/SessionState.cs` | Integrate the bridge into the same candidate graph and ordered journal; typed schema-2 commands, strict legacy read/conversion, expanded hash and immutable views. Keep the actual shared wallet authoritative. |
| `src/scene_runtime/SceneState.cs` | Stage and commit complete candidates through SceneStore; support the intended nested session versions; no second wallet or receipt sidecar. |
| New `src/scene_runtime/SceneDeathBridgeTests.cs` | Headless explicit external-fixture observations; persistence, duplicate/conflict and ownership checks. No interactive attack binding. |
| Existing session/scene tests and build source lists | Add only the relevant new test entry and run the affected replay/ownership/save cases under the normal execution approval process. |

`KillCondition.cs`, `OfflineEvents.cs` and `MoneyRewards.cs` should remain unchanged in this minimum. No changes to SceneWindow attack input, native AI, weapon inventory types, prices or combat initialization are required to test the bridge. No new original binary investigation is a prerequisite for this isolated consumer fixture.

## Acceptance for the proposed implementation

The following are required new cases, not results already obtained:

1. Explicitly activate the fixture objective; accept a correctly owned death; observe exactly one counter increment and one receipt/reward at the configured threshold.
2. Retry the same body, then a different message carrying the same victim generation; neither increments nor grants again. Conflicting facts for that generation are rejected.
3. Save, exit the own headless process, reopen, and retry. Exact character/encounter identity, count, receipt, money and shared object ownership remain consistent.
4. Interleave a death reward with the normal delivery/return commands, then replay/reopen. The reward survives and the original item/world ownership relationships still hold.
5. Reject another character's Player source, incorrect pet owner if that profile is admitted, unknown victim/generation, wrong record/profile/encounter and unsupported producer. Rejected observations do not consume a key or reward receipt.
6. Exercise capped money and explicit empty reward plans. A first successful reward receipt is recorded even with zero money delta. Separately prove that amount zero is not treated as an empty plan.
7. Reject malformed/duplicate journal commands, mismatched counter/receipt hashes, wrong profile definitions, recomputed-envelope data with invalid ownership, unsupported versions and oversized candidates. No corrupt save is defaulted to a fresh goal.
8. A late candidate failure leaves the live scene unchanged. Stale-writer conflict and an uncertain-save retry do not duplicate rewards. Normal backup recovery retains its explicitly stated rollback boundary.

Because Session reconstruction, serialization and hashing change, **targeted old M2 regression is mandatory**: all four commands in order, duplicate completions, reward totals, exact object/world identity, legacy save read, process reopen, unknown/corrupt state and stale writer handling. Also rerun the scene coupled-save/agent restore cases affected by nested Session changes. The accepted patrol movement/geometry suite need not be repeated unless its own code changes. Report new bridge checks separately from these necessary regressions.

## Stop boundary

Stop this design at the isolated externally supplied death fixture and its durable consumer bridge. Do not advertise a playable attack, infer a Monster from the visual patrol, add placeholder life values, invent a price or equipment type, or expand into an unrelated constructor/CRT graph. Verified combat production remains a distinct dependency. This document is frozen as a proposed design until implementation is explicitly assigned.
