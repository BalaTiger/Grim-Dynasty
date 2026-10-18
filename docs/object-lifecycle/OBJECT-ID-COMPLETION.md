# Object lifecycle checkpoint

Validated: 894 fixed assertions (20 new ID assertions), ten own DLL modules, sixteen standalone scenarios. Build completed successfully before shutdown request. See object-id-build-output.txt and runtime-id-inventory-scene.json. Runtime scene allocated ID 42, counter 43, added supplied own descriptor, then removed it; task wallet 50 -> 90 -> 165, reward UIDs [2,1]. Descriptor supply/dictionary insertion is fixture setup, not native construction/registration/generation.

Implemented Engine CreateObjectID 0x18017E4B0: critical section, uint counter, low28 sentinel, sticky nonzero wrapped mode, raw-key collision lookup, network slot prefix and fallback. Does not reserve/insert IDs. Unknown required fields/incomplete index or collision budget >4096 returns Unsupported without mutation. Stable index snapshot required; concurrent external registration not ported.

Creation dependency: Game Item::CreateItem 0x180324120 calls record creation helper, SetItemReplicaInfo and virtual initialization. Engine CreatePartial 0x18017E170 needs TableDepot Class lookup, class factory and SetObjectName. FinishPartial 0x18017E230 loads record, writes ID, registers ID/name indexes, then calls virtual Read. ID assignment alone cannot replace these. GiveItem Generate/OnActivate full seed/factory/cache lifecycle remains unsupported.

SaveReagents 0x1802D9D00 and SaveTransferStash 0x1802D9960 are static evidence only: zero raw flag 0x36DD3/0x36DD2 skips writes; nonzero depends on shared-path/filesystem/serialization/DirectWrite. No save path resolution or file access executed. Do not infer semantic flag names.

11 Rea 6.5.0 static calls completed, both sessions closed. Original x64 Game/Engine and Quests.arc hashes unchanged. No active build/query sessions. Next: investigate minimal record factory/Read branch; keep unknown construction and saving Unsupported.
