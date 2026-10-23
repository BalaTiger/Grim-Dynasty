# Item 对象身份调用：缓存合同与剩余直接链

本记录只读整理工程中已保存的索引、指令和源码。本审阅代理未读取原版、运行 reader、构建或测试，未修改生产源码。主代理已分别全文审阅并单次执行 `tools/static_analysis/resolve_item_identity_import.mjs` 和 `tools/static_analysis/read_object_id_getter.mjs`；本文只读核对两阶段的成功输出。阶段2的入口字节已独立人工核对到正常 RET，不将候选窗口误作完整函数元数据边界。

## 当前调用的确定身份

已保存的 Game 调用方在 323965 执行 `MOV RCX,RDI`，323968 执行 `CALL [180609128]`。因此传入的是保存的原 Item this，不是 +538 子对象。最近的逐字节证据为：

- `offline-item-be0-import/3238be-instructions.json`：323965 raw=`488bcf`；323968 raw=`ff15ba572e00`，RIP 相对解码得到 IAT 609128。
- `module-boundaries.json` 的 `$[2].importModules[3]` 是 Engine.dll，`symbols[37]` 精确为 `?GetObjectId@Object@GAME@@QEBAIXZ`，其中 `Id` 大小写按原始符号保留。
- `offline-questitem-imports/import-bindings.json` 的 `normal_imports[3].raw_metadata` 保存 Engine descriptor：OFT A42AC0、FT 609000、DLL 名称 RVA A56AA6。OFT 第37项 raw=`e05ca40000000000` 指向 hint/name A45CE0；`609000+37*8=609128`。

旧七导入报告没有把 609128 作为独立目标读取 hint/name；不能使用其他符号的 hint 或把索引37当成 Engine RVA。阶段1 reader 因此只补这个 Game IAT/hint/name和 Engine 对应 hint 的精确 name/ordinal/EAT，名称不符即停，不扫描其他导出。

新 `offline-item-identity-import/inspection.json` 已确认：hint/name A45CE0 的 hint=3189；Engine name table index3189/name RVA3E8A19、ordinal index3189/ordinal3190、EAT target=3A740。确切入口为 **Engine 18003A740**。该阶段正文读取数为0；入口行为由下述独立阶段2补证。阶段1报告状态 `fixed-item-id-import-export-and-caller-copy-verified`、failures为空，新增 Game15B、元数据88B、一次 dumpbin；Game/Engine 前后哈希保持。

调用返回后，旧完整 Item 汇编记录 `32396E MOV DWORD [RDI+538],EAX`，再在 323974 设置原 this，323977 调用 `[180609520]`。阶段1的 `[32396E,32397D)` 15B 已完整核对为这三条指令：`898738050000`、`488bcf`、`ff15a35b2e00`；包含下一调用以固定边界，但不跟随它。本段最小接合合同是 **读取当前 Item+2C 的 DWORD，复制到 +538，然后停在 323977**；已知原始位值包括0或高位为1时，getter 均没有额外分支。

## 阶段2实际入口与边界

`offline-object-id-getter/03a740-entry.bin` 的8B是 `8b412cc3cccccccc`。逐字节反汇编中，正常路径仅两条指令、4B：

| Engine RVA | 原始字节 | 已证操作 |
| --- | --- | --- |
| 3A740 | 8b412c | `MOV EAX,DWORD PTR [RCX+2C]`，读取当前 this 的4B；EAX 写入同时清 RAX 高32位。 |
| 3A743 | c3 | `RET`，没有额外调用、分配、校验或字段写入。 |

3A744..3A747 的4个 CC 是正常 RET 之后的字节，不计入 getter 正常执行。此次没有找到3A740的精确 `.pdata` 起点；已界定前一项结束3A73D、下一项开始3A780。8B只是完全位于该空隙内的显式入口候选，不能把空隙或8B宣称为完整函数/EH边界。实际正常合同来自已绑定导出入口到 RET 的直接指令，不依赖上述边界推测。

阶段2报告状态为 `fixed-object-id-entry-decoded-manual-review-required`，本记录完成其要求的人工正常入口审阅；没有改写旧报告状态。报告记录13次二分步骤、8次新 `.pdata` 读取共96B、一次8B正文读取、一次仅针对 synthetic PE 的 dumpbin、6条解码行（2条正常指令加4条 INT3），failures为空、逐字节覆盖成功。`cached_entries` 有7次命中，其中包括本次读入后再次命中的1508/1509；不能把这7次全部称为历史证据复用。原件前后两次64KiB缓冲流式哈希一致、stat稳定、句柄关闭，工具前后哈希一致；原件未被加载或交给 dumpbin，没有跟随 callee。

## Getter 缓存与不能替代的模型

阶段1之前限定检查以下既有元数据：`record-interface-discovery/record-interface-exports.json`、`offline-class-registration-exports/discovery.json`、`offline-engine-class-parent-exports/discovery.json`、`offline-actor-constructor-boundary/metadata.json`、`offline-depot-ctor-exports/depot-ctor-exports.json`。其中未找到精确 GetObjectId 的导出入口或独立正文；record-interface 的9项仅包含相关构造、vtable、SetObjectName 和 LoadFile。此结论仅适用于这些旧缓存，不是对整个工程或 Engine 的全库否定；新的入口3A740已由上节原始导出绑定独立确定。

已核 Object 源码/证据提供的是另一层合同：

- Object 基础构造只写 +00 dispatch、+20 capacity15、+18 length0、+08 首字节0，不写 +28/+2C。Actor 原始前缀也未把这两个字段初始化为零。
- `ObjectBaseConstructor.Create()` 的 `RegistrationObject` 字段保持 nullable unknown；`ObjectIdAllocator`/FinishPartial 的已有逻辑不等于当前 getter 正文，也不能证明构造进行到此时 ID 已经分配。
- `ObjectNameString` 是完整逻辑 payload/capacity 的独立模型，不是当前 `ItemConstructorStorage.Actor` 的实时视图。先前显式检查回调能修改同一 Item/Actor 字节，不能用早期“空名构造”替代当前字段读取。

阶段2已确证 getter 读 +2C 的 DWORD。因此模型必须读取**当前同一 shared storage** 的确切4B，并在任一字节未知时保留自有覆盖停点；不得补0、调用分配器来制造值，或把未知观察解释成 native getter 失败。原始指令没有 unknown/null 检查；自有未知状态、缺失 storage 和范围守卫只表示模型覆盖不足，不能宣称恢复了 native 无效内存访问或异常语义。正常 DWORD 写 +538 也不得扩大为 QWORD 写，+53C 及邻域保留。

## 新 Identity 模型的独立静审

已只读全文核对 `src/task_items/ItemConstructorIdentity.cs`（5470B，SHA见下表），以及它调用的现有 shared storage 读写接口，未发现正常覆盖域内的阻断。`ItemObjectIdGetter.Read` 在实际调用时读取同一 Actor 字节的 +2C DWORD；任何未知字节产生自有覆盖停点，已知值通过 uint 再转 ulong，保留 EAX 对 RAX 的零扩展，不把高位数据当错误码。Getter没有字段写入、分配器调用或额外 callback。

`ItemConstructorIdentityInvocation` 不在构造时采样 ID，要求 prior 已达到323968、状态为 ConditionalItemPrefix且停点一致；每个 invocation 只允许一次尝试。正常 getter 返回后只将所得值写到 +538 的4B，保留 +53C 及邻域，记录一次 caller store，再停323977。缺失/未完成 prior 和 unknown ID 均不执行该写入；调用方始终保留相同 State。该模型未执行下一名称 getter，`IsCompleteItemConstructor` 恒为 false。本结论仅是源码与保存字节的静态一致性审查，本审阅代理未编译或运行新旧测试。

## 距 Item 正常返回的剩余调用

完整旧汇编位于 `Item-constructor-primary-table-assembly.json` 的 `result.structuredContent.normalized_result`。从当前 323968 到 323A86 RET 共10次调用；**即使当前 GetObjectId 完成，仍有9次调用、3个不同目标以及一条全局对象依赖**：

| Game 调用点 | 剩余目标 | 参数/依赖 |
| --- | --- | --- |
| 323977 | IAT 609520 | 原 Item；索引164名称 `?GetObjectName@Object@GAME@@QEBAPEBDXZ`，OFT raw=`9079a40000000000`→A47990。当前不补读此绑定或正文。 |
| 32399D | Game 016460 | dst=Item+540，src=GetObjectName 返回值，R8=调用方逐字节找 NUL 算出的长度。 |
| 3239B3 | Game 016460 | dst=Item+560，src token=1806196DE，R8=0。 |
| 3239C9 | Game 016460 | dst=Item+580，src token=1806196DF，R8=0。 |
| 3239E5 | Game 016460 | dst=Item+5A8，src token=180619703，R8=0。 |
| 3239FB | Game 016460 | dst=Item+5C8，src token=18061975D，R8=0。 |
| 323A11 | Game 016460 | dst=Item+5E8，src token=18061975E，R8=0。 |
| 323A2D | Game 016460 | dst=Item+610，src token=18061975F，R8=0。 |
| 323A6B | Game 324560 | 原 Item；其正常副作用/返回和直接依赖尚未在本轮检查。 |

这里的7次 016460 是同一 Game 目标，不是已恢复的 Engine 023180 或 022CE0。不能因“都是字符串赋值”借用算法，也不能只因六处 R8=0 就省略调用；必须先取得该输入域内的实际正文。当前工作不扩大读取这些函数或字面量。

GetObjectName 后的调用方先检查返回指针所指首字节；空串将 RSI 置0，非空从此前的 -1 递增并继续找 NUL。这要求有效、可观察的当前名字字节，不能从独立逻辑名称快照或原始构造期空串偷换。是否内联/间接、返回身份和失败边界由 getter 正文决定。

其余已存尾部写入依次包括 +5A0 DWORD0、+608 DWORD0、+630 QWORD0、+698 DWORD0、+6B0 DWORD1。之后 323A49 读取 `[Game A66288]`，323A50 读取所得对象的 DWORD +1B58 并于323A56写 Item+B68，323A5C 再读 DWORD +1B5C 并于323A62写 Item+B6C。这个服务对象身份、初始化和字段值不能用默认值替代；当前不推断其类名，也不把另一模块的全局状态接入。

仅在最后 324560 正常返回后，323A71 才选原 RDI 为 RAX，323A86 RET。上述未闭合调用和全局读取仍阻断完整 Item 构造，不能将推进一次 DWORD 复制描述为接近原版兼容。

## QuestItem 与完整对象边界

旧 `offline-questitem-core/427c90-instructions.json` 已保存 QuestItem 在 Item 正常返回后的15次派生写入，本次重新只读核对：唯一调用是427C99→323690；后缀没有其他调用。它写 C68 BYTE0、四个 vptr 身份、C70 QWORD0、两组字符串元数据/首字节（C78/C88/C90 与 CA0/CB0/CB8）、C98 QWORD0、CC4 BYTE0 和 CC8 DWORD750，保留未写的 CC0 和 padding。427D19 选择原 this，427D2B RET。它仍以 Item 的真实正常完成为前提，不能提前接到当前局部 prefix 上。

完整工厂还保留分配器/new-handler/异常清理、真实加载后检查和原生对象注册条件。完整 Read 是另一个阶段：QuestItem Read 调 Item Read，后者首先调用 Actor::Load，另有 getter、字符串和图形服务；这些不因构造字段推进而闭合。已保存正常路径和自有条件模型不等于正式样本、完整 runtime graph 或原版兼容验证。

## 来源 SHA256

| 工程相对路径 | SHA256 |
| --- | --- |
| docs/rewards/rea-inventory-add/Item-constructor-primary-table-assembly.json | 7a078d11495b5b7c10a07d7b6309c9d633f8ec39dfc444e5f0decfbd5efe1a3f |
| docs/phase-1/boundary-audit/module-boundaries.json | d1ec79916baeb45b3257f7c7c2ce356c262f804ae9b2577888a0d14de53e7ba7 |
| docs/object-lifecycle/offline-questitem-imports/import-bindings.json | 94d695f5026e1f50a7c1a2968d6f091146db634e28618ef0565b671857aa6f5e |
| docs/object-lifecycle/offline-item-be0-import/inspection.json | 1392ed5594df6499211ce2c0910abf7a5f8fba7501aa1b38361acfdfd21822e5 |
| docs/object-lifecycle/offline-item-be0-import/3238be-instructions.json | 62c166c9aa0b41f44d3f6628a48496de7eaf54d511a1fe6161932e3001ed118d |
| src/task_items/ObjectBaseConstructor.cs | 863810dbeb3c7ebc8304879c0d2484f48997a1592be2fe7cf8d7e2db97edce94 |
| src/task_items/ObjectNameString.cs | dd9910120e90971d63e190596dd9dc925d2cc41dfb29337fbea55a5aca2f06e6 |
| src/task_items/ActorConstructorPrefix.cs | c51710c512abe864d16feecd7972a779804d9d0cdc419b117e86ba7e37134b83 |
| src/task_items/ItemConstructorIdentity.cs | a3953d2338b21973c032c731a53c188fd2326aab63cdcf7f4fb3de4ec5fc723a |
| docs/object-lifecycle/QUESTITEM-FACTORY-READ-EVIDENCE.md | a194d5501f869de06994aa2e633c6e09f9b823f6eb60c2dae9ede9120ce75bf5 |
| tools/static_analysis/resolve_item_identity_import.mjs | 29c90fc665114b79822665cbb065e091eb5ddfe86b03451f835235195fd09bf0 |
| docs/object-lifecycle/offline-item-identity-import/inspection.json | c0b80a81f7094c25816017d8e4adaeeff74c835f3049daabd0842b9563740b59 |
| docs/object-lifecycle/offline-item-identity-import/32396e-instructions.json | 6d28b1412db5c9d04a261a5b480dfc6869fef32aca613cbeaf080616b8db0ba5 |
| tools/static_analysis/read_object_id_getter.mjs | 57bdee5bdc5db15bde593b57d503ea8e99083fef4dfcafc98e1c3807f065d524 |
| docs/object-lifecycle/offline-actor-constructor-boundary/metadata.json | 39d0980070ec9aceb95974039fd50d2ae64db223d9ce5b84b280693b1f95890b |
| docs/object-lifecycle/offline-object-id-getter/inspection.json | ece892d8a5f9fea5a7575bcb85fee8270ff6e08becbba696e5c81beb00095475 |
| docs/object-lifecycle/offline-object-id-getter/03a740-entry.bin | b4ca624c2cc81d3849867110716a92bdc2e96151001976a5b2be0ebefbba762b |
| docs/object-lifecycle/offline-object-id-getter/03a740-instructions.json | 11c512c676433b49a407707fba880dcebfd110c886bb36eb92821d463586f7d1 |
| docs/object-lifecycle/offline-questitem-core/427c90-instructions.json | 9bae1d2251fa300212a015bd47763bc37c214b2e2f3bc25e17a2d44e32590160 |
