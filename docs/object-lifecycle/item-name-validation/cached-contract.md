# Item 名称段：已保存证据库存

本记录限定 Game `323977..323A49`，不执行323A49的全局读取。本审阅代理只读工程缓存，未读取原版、启动 reader、dumpbin、Rea、构建或测试，未改生产代码。主代理随后分别审阅并执行本轮固定 reader，新增结果在下文单列；旧库存结论保留其时间边界。以下未知状态不是 native 失败分支。

## 609520 的已存绑定

- `offline-item-identity-import/32396e-instructions.json` 已逐字节保存323974 `488bcf`（RCX=原 Item RDI）、323977 `ff15a35b2e00`（RIP 相对指向 Game IAT609520）。这是本轮可直接复用的调用证据，不需要为该 CALL 再读原件。
- `module-boundaries.json` 的 `$[2].importModules[3]` 是 Engine.dll，`symbols[164]` 为 `?GetObjectName@Object@GAME@@QEBAPEBDXZ`。
- `offline-questitem-imports/import-bindings.json` 的 `normal_imports[3].raw_metadata.descriptor` 保存原始20B `c02aa4000000000000000000a66aa50000906000`：OFT A42AC0、FT609000、DLL名A56AA6。其 `ownership_lookup_prefix.bytes` 中第164项8B为 `9079a40000000000`，指向 hint/name A47990；`609000+164*8=609520`。原始DLL名为 `456e67696e652e646c6c00`。

旧七目标导入报告没有将609520单独选作目标读取 hint/name，索引164不能当成 hint、ordinal 或 Engine RVA。现有 `record-interface-discovery/record-interface-exports.json` 的9个固定选择也没有该 getter，不能从相邻 GetObjectId/SetObjectName 地址推测入口。

初版库存限定检查了名称/字符串相关直接归档目录与 `docs/rewards/rea-inventory-add` 的文件名、已知接口导出记录和 Item 构造正文：当时未找到名为 GetObjectName 或 Game016460 的独立已保存正文。此否定仅限上述范围，不声称全工程不存在。相关目录包括 rea-object-base、rea-object-name-assignment、rea-object-name-growth、name-interface-discovery、record-interface-discovery，以及 object-lifecycle 中名称含 item/name/assignment/string 的直接目录文件名；未递归扫描其他目录或原版。此后新取证的016460正文见后节，不再把它列为“尚无正文”。

主代理已审阅并单次执行 `resolve_item_name_import.mjs`；保存的 `offline-item-name-import/inspection.json` 状态为 `fixed-item-name-import-export-verified`。原始 hint/name 确认 hint3191、精确名称；Engine index3191/name RVA3E8A8A、ordinal index3191/ordinal3192、EAT目标 **3A760**。新增98B元数据、6次定点读取，正文/`.pdata`/dumpbin均为0，Game/Engine各前后两次完整哈希一致、stat稳定、句柄关闭、failures为空。该阶段只证明导出入口，返回算法由随后独立正文补证。

## Getter 新正文的独立正常路径核对

主代理另行全文审阅并单次执行 `read_object_name_getter.mjs`，读取 `[3A760,3A780)` 固定32B，复用已证 `.pdata` 前end3A73D/后begin3A780，无新增元数据。`offline-object-name-getter/inspection.json` 为 `fixed-object-name-entry-decoded-manual-review-required`；原件前后哈希/stat、句柄关闭、工具哈希、全部bytecoverage均通过，failures为空，dumpbin一次且只处理synthetic PE。本审阅代理独立读了保存的20条解码行，正常入口只使用前15B/5条：

| RVA | 原始字节 | 正常含义 |
| --- | --- | --- |
| 3A760 | 4883792010 | 比较当前 this+20 的QWORD capacity与16。 |
| 3A765 | 488d4108 | RAX=this+8；这是取地址，不读取inline payload。 |
| 3A769 | 7203 | unsigned capacity<16直接跳到RET。 |
| 3A76B | 488b00 | 只有capacity>=16才读取[this+8]完整QWORD作为RAX。 |
| 3A76E | c3 | 返回所选身份。 |

没有CMOV、length读取、callback或callee。inline分支无需假定first byte/padding已知；heap分支要求当前指针QWORD已知，但不在getter内部解引用指针，已知0也只是原始返回值，不能当作已知空串。真正字符访问在Game32397D开始，当前观察缺失或无效own地址只能另行停点。RET后17B包含3A770的邻接入口及padding，不属于当前getter，不能因候选窗口读取了它就扩大模型范围。32B和pdata间隙均不是完整函数/EH边界声明。

## 调用方已证顺序

`Item-constructor-primary-table-assembly.json` 的 `result.structuredContent.normalized_result` 保存下列原调用顺序；同目录 Item-constructor.json 伪代码仅作交叉定位，不能替代指令证据。RBP于323714置0，RSI于3238BE置为全1，这两个非易失寄存器作为正常调用返回前提保留。

1. 323977调用 getter 后，32397D直接检查返回指针 RAX 所指首字节。空串在323982设置 RSI=0；非空在323987先递增 RSI（初值-1），32398A检查 `[RAX+RSI]` 并循环直到 NUL。首字节在非空路径会再次读取；没有读取“已存长度字段”来代替这个循环。
2. 323990/323997/32399A设置 RCX=Item+540、R8=上述计数、RDX=同一返回指针，再在32399D调用 Game016460。getter 的 inline/heap 选择和返回身份已由上节正文证明；其指向的当前可观察字节及有效期仍是后续读取的必要前提，不能从符号名称或旧snapshot补出。
3. 后续六次016460调用的 R8均为0，但源 token 各自明确；没有证据允许省略调用或假定它们不读取源/不释放/不分配。

| 调用点 | RCX目的偏移 | RDX源 token | R8 |
| --- | --- | --- | --- |
| 32399D | 540 | getter实际返回指针 | NUL循环计数 |
| 3239B3 | 560 | 1806196DE | 0 |
| 3239C9 | 580 | 1806196DF | 0 |
| 3239E5 | 5A8 | 180619703 | 0 |
| 3239FB | 5C8 | 18061975D | 0 |
| 323A11 | 5E8 | 18061975E | 0 |
| 323A2D | 610 | 18061975F | 0 |

纯字段写入穿插在这些调用之间，不能全部提前：3239CE DWORD+5A0=0；323A16 DWORD+608=0；最后一次调用正常返回后323A32 QWORD+630=0、323A39 DWORD+698=0、323A3F DWORD+6B0=1。下一条323A49全局读取是本轮边界，不在此记录中推测其类或状态。

## 现有字符串证据可复用与不可套用之处

`OBJECT-NAME-EVIDENCE.md` 与 Engine023180/022F70正文已经证明 **Engine SetObjectName 3A780 的指定调用域**：外层先处理同 string 对象别名；不同对象调用023180时 position=0/count=UINT64_MAX；容量不足进入022F70；正常增长包含分配、旧内容拷贝、旧buffer释放、安装新buffer及终止符。一般重叠来源、任意substring/self-helper路径、overflow、allocator失败和异常清理不在旧模型合同内。

`ObjectNameString` 可作为显式提供的完整逻辑字节串输入：它复制 payload、用数组长度当逻辑长度、要求已知合法容量并假定已知NUL终止。它不保存原生地址、共享 Actor 内存、未知byte或未使用padding，因此不能当本轮实际 getter 的实时指针，也不能自动把原 Item+8 解释为其 payload。早先构造时的空名不能覆盖当前状态；此前条件性检查回调允许修改同一共享字段。

`ObjectNameAssignment.Apply` 更新独立 `ObjectBaseState.Name` 并计算/写+28哈希，提供的是完成后的正常逻辑结果；它不是 Game016460，不会给出本轮逐写前缀、原生返回身份或失败状态。即使目标偏移看似字符串布局，也不能套用023180的容量增长公式、释放条件、别名规则、返回值或异常合同。旧 Engine `2DDB0E -> IAT302768 -> VCRUNTIME140!memcpy` 绑定仅用于已经证明到达该 thunk 的 Engine路径，不能未经 Game016460直接调用证据迁移过去。

前段0410E0已建立相应目的子对象的某些 length/capacity/首byte，但这些值存在于当前共享storage且可被显式回调修改。后续必须以执行到当前调用点时的实际字段为准，不能无条件重新建一个fresh字符串、预采样七次调用输入或抹去partial failure。

## 新取证的 Game016460 正常分支

主代理单次执行另一固定 reader 后，`offline-item-name-assign/inspection.json` 保存精确 `.pdata` 项 `[16460,1658D)`，301B正文、94条指令全部逐字节核对；新增24B元数据，原件哈希/stat/关闭成功、failures为空。本审阅代理已独立读完整指令，没有运行该 reader。

该正文保存 RCX=destination、RDX=source、R8=count；首先检查源是否指向目的当前内容内部，这个检查**早于count为0的分支**。source=0跳过别名检查；非零source先按目的capacity选择 inline地址或heap指针，source<base可直接判定非别名，不读oldLength；否则比较 `base+oldLength` 与source，source等于末端也不属于别名。只有 `[base,base+oldLength)` 内部来源进入164D2 tail jump→161A0，参数为同目的对象、源偏移和count。这条未读helper不能替换成no-op，即使count为0。地址相加是原始64位算术；自有地址域守卫不能伪装成native分支。

非别名路径在164D7比较count与UINT64_MAX-1；超出范围会在164E4调用 IAT60B680并接INT3，身份/异常合同未闭合。164EB读取目的capacity；不足时164FB调用16340(destination,count,oldLength)。本轮未跟随161A0、16340或IAT60B680。

在已证非别名、capacity足够且count=0的范围，16516先比较当前capacity与16，1651B写length0，然后使用比较flags选择heap或inline，在16524/1653D写首byte0。容量与padding不修改，返回原目的对象；这是两次字段写入，**不能泛化为所有零长输入**。对于正count，1655E调用5DC344传目的选定buffer、source、count；返回后16563重新读取capacity，16568写length，随后按新的capacity/指针选择写终止符，最后返回原目的对象。copy thunk身份已由随后单独取证闭合，见下节；增长/别名/异常合同不能借用旧Engine字符串模型。

## Copy thunk 独立磁盘绑定

`offline-item-name-copy/inspection.json` 已成功记录固定6B `FF 25 76 F4 02 00`，独立RIP计算得到 Game IAT60B7C0。复用的VCRUNTIME140.dll descriptor5具有OFT A45258/FT60B798；新读取的OFT第5项及IAT均为 `8476a50000000000`，原始hint/name `3c006d656d63707900` 确认为 hint60、`memcpy`，并验证目标前的OFT项未终止。该记录实际新增6Bcode和65Bmetadata（48B OFT前缀+8B IAT+9B hint/name），没有子进程，原件哈希/stat/关闭成功，failures为空。

本审阅只读该原始记录；没有使用邻接memmove候选，也没有运行CRT实现。此绑定不等于观察到了loaded IAT目标或提供native重叠复制、fault/EH合同。自有模型若覆盖正count正常拷贝，必须明确有效可读源、可写目的和非重叠条件，以及未知byte/自有预算的覆盖停点；不能为了清除未闭合状态调用任意占位callback或将所有字节假定为0。

## 已执行范围与剩余最小证据

Game609520的8B IAT 与 A47990 的固定 hint/name、Engine hint定点名称/ordinal/EAT已经按前述最小范围执行完成，没有fallback扫描。旧 descriptor、OFT164原始值、Engine PE/export布局以及已保存323977原始调用均被复用。

真实 getter 已绑定3A760，固定32B reader也已实际成功执行并完成上述人工正常路径审阅；不需要重读getter或相邻入口。016460正文现已读，剩余直接helper仅按实际所选分支闭合；遇到未证增长、别名、copy或清理行为需保留明确停点，禁止自动展开异常链。

未来自有接合需要保留同一 Item/Actor storage、明确表示 getter返回身份及其对应的可观察字节、逐次读取NUL而不补终止符、真实调用顺序和窄写邻域。未知源字节、未知heap指针、短捕获范围或自有扫描预算只能构成模型覆盖停点；不声称原版有对应guard。正式runtime/并发/异常与完整Item/QuestItem仍未闭合。

## ItemConstructorName 冻结源码独立审阅

已只读全文审阅新 `ItemConstructorName.cs` 首稿，并复核作者最终冻结版本19574B、SHA `9c63a69c1ac593f4625c9a2596740effb7be107426cbe3eeb01a66d52884b2e5` 的修改。未发现声明的正常覆盖域内阻断；本结论不是编译、测试或原版兼容验证，本审阅代理没有执行这些动作，也未代改源码。

- Getter读取当前共享Actor的capacity；inline只返回this+8，heap才读当前指针，既不读取length也不预读payload。扫描保留32397D的首byte读取，并在非空路径按32398A从index0再次读取；未知byte不补零，getter不将heap null变成空串。
- 赋值保留源为0跳过别名、source<base跳过length、区间内部别名先于count/error/growth的顺序。作者已修正首稿漏记的heap alias确定后的164AC第三次指针读取；最终版在该读取完成后才停164D2，不执行未证161A0。
- count0正常非别名路径先比较capacity、再写length、再选择当前目的并写NUL，未更改capacity/padding。正count在明确normalMemcpy条件下只覆盖有界、完整已知、有效且不重叠的own区间；预检源byte属于自有覆盖选择，不声称native memcpy失败原子化。拷贝可影响同一状态里的metadata，因此返回后会重新读取capacity、写length，并按需要再次读取heap pointer后写终止符。
- 覆盖停点保留真实已完成前缀：例如zero路径length已写而目标终止符地址未知时不回滚；copy完成后若后续观察不足也不抹去copy。未证增长16340、别名161A0、length错误IAT60B680保持各自停点。范围溢出、映射和预算为own守卫，不添加native分支。
- 整体continuation验证prior/同一State与Memory身份，每个invocation单次尝试，按原顺序执行7次赋值和5次穿插scalar写，成功只到323A49，完整Item恒false。区域数组仅复制容器引用，region与Item内容保持可变live观察；模型明确串行排他使用，没有native并发/原子性保证，raw映射不证明合法C++字符串对象。

静态checkpoint中的旧83b9版合同及全部原始报告保留不动；本节和copy绑定进入最终模型阶段文档版本，不通过改写旧证据消除其原有时间边界。

## 固定来源 SHA256

| 工程相对路径 | SHA256 |
| --- | --- |
| docs/object-lifecycle/offline-item-identity-import/inspection.json | c0b80a81f7094c25816017d8e4adaeeff74c835f3049daabd0842b9563740b59 |
| docs/object-lifecycle/offline-item-identity-import/32396e-instructions.json | 6d28b1412db5c9d04a261a5b480dfc6869fef32aca613cbeaf080616b8db0ba5 |
| docs/object-lifecycle/offline-questitem-imports/import-bindings.json | 94d695f5026e1f50a7c1a2968d6f091146db634e28618ef0565b671857aa6f5e |
| docs/phase-1/boundary-audit/module-boundaries.json | d1ec79916baeb45b3257f7c7c2ce356c262f804ae9b2577888a0d14de53e7ba7 |
| docs/rewards/rea-inventory-add/Item-constructor-primary-table-assembly.json | 7a078d11495b5b7c10a07d7b6309c9d633f8ec39dfc444e5f0decfbd5efe1a3f |
| docs/object-lifecycle/record-interface-discovery/record-interface-exports.json | 33d7527e64565cc4e56a31b3665b41e004ad9e53f93739e3a645f205cf0b1603 |
| docs/object-lifecycle/OBJECT-NAME-EVIDENCE.md | 59e468e3103273a5593e646c6666a4077b7dcacbab482ddfcc75afab3e815d64 |
| src/task_items/ObjectNameString.cs | dd9910120e90971d63e190596dd9dc925d2cc41dfb29337fbea55a5aca2f06e6 |
| src/task_items/ObjectNameAssignment.cs | 55d0c434931a62ec1f3504e22abf4301e7b9d5d3493fae3c57a29f3beddc8a3d |
| docs/object-lifecycle/rea-object-name-assignment/05-Object-name-assignment-0x180023180-assembly.json | 366e92084dae96b73957c3e932927417909acbeaa6531888fe1c3bb8efa6110a |
| docs/object-lifecycle/rea-object-name-growth/05-Object-name-growth-0x180022f70-assembly.json | e9d598f8f6cf8a486d6c0b55247f48dfdacf3103c38814fe9337a133af2b205c |
| docs/object-lifecycle/name-interface-discovery/name-copy-thunk-0x1802ddb0e.json | 78874df16fdbda694338800a35aa327a39a5db73cd0ec0e2d46ad54dca5f0549 |
| tools/static_analysis/resolve_item_name_import.mjs | 2b5ac69401211b32ff8e361853a422e69bc53581f1baadd44e7d4583e83ac34e |
| docs/object-lifecycle/offline-item-name-import/inspection.json | c3ba3e79ecceb98225a9034a85649807d8d00444feccf771b294a41d1e443f31 |
| tools/static_analysis/read_object_name_getter.mjs | 964edd1feed8de67d6553fdd0cb233fbbc7d5b6cf76b0d0b98f9d5170e384f44 |
| docs/object-lifecycle/offline-object-name-getter/inspection.json | 3a103ba8bf7890803c8d684388fdfe32bbfebe5c7573f623a9c4264c56763ead |
| docs/object-lifecycle/offline-object-name-getter/03a760-instructions.json | 636cd2ae26751ab7971ab2639663a123740dd6596ba825d20e7aa604203094f4 |
| docs/object-lifecycle/offline-object-name-getter/03a760-entry.bin | 63cf3029dbee1de1a842b86464bf6a05de63cf2d9fbc4999f7e3c6f03dff07b2 |
| docs/object-lifecycle/offline-item-name-assign/inspection.json | 5a9167e7cf619e8c07ff33d74fee66e021de32bf6c638e54b67758eb7579c317 |
| docs/object-lifecycle/offline-item-name-assign/016460-instructions.json | acf327dda597d7a6eb974fb35ebd9720d79b102d015fafd11585fd122454dfb5 |
| docs/object-lifecycle/offline-item-name-assign/016460-exact.bin | e36a5e1f2c7fa5e9e7f1ee9fef5880533281d5c43cc43c543abcdf0a1c95ab4a |
| docs/object-lifecycle/offline-item-name-copy/inspection.json | a9a02ca102126261641099e1aca4bd758d19ab5486d1075a2e9579f628a59adf |
| docs/object-lifecycle/offline-item-name-copy/5dc344-entry.bin | 2414c8fc6c9a152316b95583b1248b709d7c3891ddc483577f97c46802e2da5d |
| src/task_items/ItemConstructorName.cs | 9c63a69c1ac593f4625c9a2596740effb7be107426cbe3eeb01a66d52884b2e5 |
