# Item final continuation plan

2026-10-21. Continue the verified same-state name prefix at Game323A49. The authorized scope is the directly referenced GameA66288 object/fields and the last direct call at323A6B to324560. No native runtime execution or broad engine scan is permitted.

The previous checkpoint is `.tooling/checkpoints/item-constructor-name-complete/manifest.json`, SHA256 `151a19331866c19f3560b93acb8088b07ec7e2ae150b238d3d7766a643ddf2e0`. The independent baseline verifies all530 live files and530 copies, HEAD6dfdae419d1c92f7738eefe241937057d83134df and the existing12 tracked-file edits (+60/-46).

Priority and read bounds:

1. Reuse saved Item caller and InitializeObjects evidence. The latter explicitly publishes GameEngine constructor2C07A0's returned pointer to A66288 at36B25F. This proves a publication route, not current object validity or a complete set of writers.
2. Locate324560's exact runtime-function boundary using cached PE metadata and bounded binary search (at most34 new12-byte entries). Read its body only if the entry is exact and at most1024 bytes; disassemble only a tiny generated PE containing that body. Do not follow new callees automatically.
3. If no saved evidence supplies the two GameEngine fields, use the directly named constructor2C07A0, with an exact-boundary body limit6144 bytes and at most408 bytes of new metadata. The optional eight disk bytes at A66288 describe only disk state. They cannot establish a loaded, valid GameEngine object. Any dependencies outside these bodies remain explicit.
4. Implement only established normal paths using the same mutable Item/Actor state and explicit borrowed global-object observations. Preserve precise store widths, source identity, live reads, unknown/invalid stops and all completed failure prefixes. Do not substitute default globals or call an unresolved target through an invented callback.
5. Review new sources, test inputs and fixed build script before requesting formal `require_escalated` execution. Run only new fixtures and the affected Name regression. Earlier build approvals do not authorize this build. Keep all outputs and actual execution results, then seal a recoverable checkpoint.

Originals stay read only with streaming64KiB before/after hashes. No game, Rea, Java, saves/cloud, installations, security/ACL/execution-policy changes, commit or push. Never rerun or replace the historical denied build script. Conditional normal return, unresolved alias/growth/exception paths, native ABI/runtime compatibility and official-sample compatibility must be reported separately.
