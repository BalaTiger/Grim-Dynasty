# Item 返回前的全局字段与最后调用：缓存合同

范围是已保存证据中的 Game323A49..323A86、A66288的对象来源，以及直接关联的GameEngine构造/初始化入口。本审阅代理未读取原版、运行原程序/reader/Ghidra/构建或测试，未修改旧源码或证据。以下缓存路径及SHA用于复核；没有为了否定结果扫描全引擎。

## 明确的对象发布者

`docs/phase-1/round-4/Game-function-evidence-bounded-v2.json` 数组第1项（label=`InitializeObjects`）保存实际指令，正常顺序为：

| Game RVA | 保存指令/作用 |
| --- | --- |
| 36B244 | CALL IAT60B578，旧outgoingReferences标为GetCurrentThreadId。 |
| 36B24A | ECX=37868，下一次分配调用的实际请求。 |
| 36B24F | CALL5DA6C4。 |
| 36B254 | RCX=RAX，传入分配返回身份。 |
| 36B257 | CALL2C07A0，outgoingReferences精确符号为 `??0GameEngine@GAME@@QEAA@XZ`。 |
| 36B25C | RCX=RAX，保留构造返回身份供后续初始化。 |
| 36B25F | MOV QWORD[A66288],RAX，outgoingReferences精确符号为 `?gGameEngine@GAME@@3PEAVGameEngine@1@EA`。 |
| 36B26A | 尾跳2D7230，符号为 `?Initialize@GameEngine@GAME@@QEAAXXZ`。 |

因此A66288与GameEngine的关系不是根据字段值或名字猜测，而有已存发布store和调用目标支持。此处并未执行该链；分配请求37868不是跨版本公开对象尺寸承诺。全局发布发生在构造正常返回之后、Initialize之前；不能据此认为初始化已成功或后续对象字段不变。

同文件第2项ShutdownObjects只保存16B入口，能够证明36B274读取同一全局并在36B27B测试；伪代码展开出的后续销毁/清零超出已选指令区间，不作为本轮已证writer。InitializeObjects伪代码同样越过tail jump展开了更多行为，本记录只使用其有对应指令的部分。

## 已缓存GameEngine构造的具体写入

`docs/task-kills/ghidra-selected/subscriber-constructor-routes.json` 的 `functions[4]`：

- `observed_entry=1802c07a0`，`name=GameEngine`；body标为 `[[1802c07a0, 1802c1ba6]]`，含978行assembly，覆盖到正常RET。
- 2C07C6执行 `MOV RDI,RCX`；本体没有后续RDI重赋值，正常callee遵守非易失寄存器约定。2C1B8B执行 `MOV RAX,RDI`，2C1BA6 RET，选回原this。
- 2C0D1B先调用300EA0，2C0D20保存其返回值到this+1B48；随后下面两条紧邻store之间没有call。

| RVA | 实际宽度/目标 | 所写原始位值 |
| --- | --- | --- |
| 2C0D23 | DWORD this+1B58 | 40400000。 |
| 2C0D2D | **QWORD** this+1B5C | 0000000040A00000；同时覆盖+1B60..+1B63为0。 |

本记录不把这些位值命名为浮点、范围、等级或任何业务字段。构造本体中没有其它直接提及+1B58/+1B5C的store；但这只说明文本中的显式偏移写入，**不能证明构造返回时仍保持上述值**。后续有大量直接/间接callee，未恢复其写入集合，也不排除别名或间接覆盖。

## 明确的后续数据库加载依赖

同一构造缓存的assembly精确保存：

- 2C1891从this+1A8取字符串指针；2C1898..2C18C0建立临时字符串并扫描长度。
- 2C18C4 CALL016460，用该来源填充临时对象。
- 2C18CA `LEA RDX,[RBP-28]`，2C18CE `MOV RCX,RDI`，**2C18D1 CALL2C8750**。
- 2C18D7再次选临时对象，2C18DB CALL0165D0。

伪代码同一位置标为 `LoadFromDatabase(this,local_50)`；精确call目标及this实参由assembly独立支持。2C8750是本轮比“构造默认值”更关键的后续直接依赖，可能改变该对象状态。不能把调用省略后建立默认全局观察，更不能把构造默认值当作真实Item运行时读取值。

限定检查 task-kills/ghidra-selected、phase-1/round-4、rewards/rea-inventory-add 中相关已保存JSON：2C8750只命中上述构造call，未找到独立该entry正文；2D7230仅命中InitializeObjects的tail引用，未找到独立初始化正文。相关round-2/round-3计划入口也没有这两个目标；没有继续展开其中无关路径。这个结论有范围限制，不是全工程不存在的证明。

## 缓存来源与证据强度

subscriber-constructor-routes.json本身只记录source、program=`Game.dll`、functions，**没有内嵌原版SHA、逐指令原始bytes或本轮before/after原件哈希**。其同名log记录2026-10-18复用旧机 `analysis/selected-kill-project/TaskKills` 的 `/Game.dll`，明确将2C07A0作为Seed/ExportObservedFunctions目标并写出该JSON。analysis.log记录初次导入旧机Game.dll的路径。`docs/task-kills/acceptance.json.native_input_sha256["x64/Game.dll"]` 记录固定原版SHA `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`，但其静态摘要并非对这份后续JSON每一原始字节的绑定。

另外，`initialization-locations.json.initializationExports` 为同一固定GameSHA列出GameEngine ctor2C07A0、5127B边界候选，与缓存body末端2C1BA6一致；它也独立支持InitializeObjects入口和符号关系。可复用的是历史受限静态汇编与相互一致的入口/身份记录，不应把它升级成整个缓存正文已经完成本轮raw-byte coverage验证。本轮随后只核验了下方附录列出的四处34B锚点，没有重复读取整个5127B构造体；其余历史缓存限制仍成立。

## Item端真实读取与返回边界

`Item-constructor-primary-table-assembly.json` 的 `result.structuredContent.normalized_result` 保存：

| Game RVA | 操作 |
| --- | --- |
| 323A49 | RCX=QWORD[A66288]，只在这里捕获一次对象身份。 |
| 323A50 | EAX=DWORD[RCX+1B58]。 |
| 323A56 | DWORD[Item+B68]=EAX。 |
| 323A5C | EAX=DWORD[同一RCX+1B5C]。 |
| 323A62 | DWORD[Item+B6C]=EAX。 |
| 323A68 | RCX=原Item RDI。 |
| 323A6B | CALL324560。 |
| 323A71 | 在该call正常返回之后，RAX=原Item RDI。 |
| 323A86 | RET。 |

第二次字段读取使用已经捕获的RCX，不重读A66288。两个值均按DWORD原始位复制，不从GameEngine的QWORD初始化写入推导当前运行时值。若模型缺少slot、有效对象身份或任一当前字段观察，应在相应读取点停下；第一条复制已完成而第二字段未知时，不能回滚B68或提前写B6C。一次性预采样两字段会隐藏读取顺序和失败前缀，应明确自己的串行/排他覆盖合同。

324560的精确目标来自上述直接CALL；初版限定旧缓存库存仅找到调用引用，随后主代理已取得独立新正文，见下一节。不能把它当作空函数、普通getter或无副作用通知。即使两个DWORD复制完成，也仍须该callee真实正常返回，才能执行保存的原Item返回路径；本记录不宣称Item构造已经闭合。

## 新324560正文的独立审阅

`offline-item-return-dependency/inspection.json` 状态为 `exact-return-dependency-body-decoded-review-required`，精确pdata项11780为 `[324560,3245DB)`、123B、28条指令；新增168B metadata，逐字节核对成功，原件前后哈希/stat、关闭以及工具哈希成功，failures为空。该reader由主代理执行，本审阅代理只读保存的完整指令。当前正常路径实际是：

1. 32456A读取Item+B6C到XMM1，保存this到RBX；复制XMM1后，在324578减当前Item+B68，再于324580乘 `[Game7A8164]` 的binary32，324588以CVTTSS2SI截断为有符号32位EDI。
2. EDI非零跳至3245A2；EDI为0时，324590另将原XMM1（先前读取的B6C）乘同一地址的scale，324598转换为EAX，32459C先写Item+B64。**该路径不早退，也不跳过后续除法。**
3. 两个分支都在3245A2调用IAT60BA70。该外部调用后，3245A8重新读取Item+B68，3245B1重新读取scale并相乘。外部返回EAX经3245B0的CDQ符号扩展后，在3245B9执行signed IDIV EDI。
4. 正常除法余数EDX在3245BB进入XMM0，3245BF以CVTDQ2PS转换为binary32，再于3245C2加到重新计算的下界乘积；3245C6再CVTTSS2SI，3245CA最终写Item+B64，3245DA RET。

因此EDI=0必须在IDIV处保留除零边界，不能把早期B64写当作成功shortcut；早期写入和外部call效果均已经发生。INT_MIN除以-1的商溢出、NaN/无穷/超范围CVTT、SSE异常/舍入环境也不能用普通数学公式掩盖。只覆盖明确有限且精确的binary32子域是可声明的自有限制，不代表已恢复整个硬件浮点环境。外部call后的B68/scale重读必须保留；不能在入口预采样全部值，也不能在未知/失败后回滚先前写入。

本节初读时只根据IAT地址记录外部依赖；后续固定原始绑定见附录。导入名字并不证明返回分布、当前随机状态或默认值，文件scale也不是运行时不变保证。返回本callee并不消费它的RAX作为Item身份：原caller在323A71重新选择RDI。

## 已完成的固定输入与缓存锚点核验

主代理已单次执行 `tools/static_analysis/resolve_item_return_inputs.mjs`；本审阅代理只读其保存报告 `offline-item-return-inputs/inspection.json`。实际状态为 `fixed-return-inputs-and-cache-anchors-verified`，`failures=[]`，`source_closed=true`，stat稳定、前后SHA均为固定Game SHA。没有子进程，没有Engine新读取，没有读取A66288磁盘槽，也没有调用或加载原版。

- descriptor8的OFT为A45530、FirstThunk为60BA70，精确模块名为 `api-ms-win-crt-utility-l1-1-0.dll`。第0个OFT与文件IAT均为 `7477a50000000000`，指向A57774；精确hint/name字节 `1b0072616e6400` 绑定 **hint27 / rand**。该结果把3245A2的固定IAT调用绑定到导入名字；没有执行rand、读取其状态或跟随其实现。
- 7A8164的4B文件值为 `00007a44`，原始binary32位为 **447A0000 / 1000**。原正文在324580、324590条件分支、3245B1分别读取同一地址；文件常量不能抹去这些实时读取点，也不保证运行时值未被修改。
- 下表四处原始字节均与历史assembly锚点完全一致。该核验加强具体writer、this与return身份的原版绑定；它不证明整个GameEngine构造体、2C8750或Initialize已完整恢复，更不证明字段到返回时仍未改变。

| Game RVA | 新核验字节 | 独立支持的范围 |
| --- | --- | --- |
| 2C0D23 | `c787581b00000000404048c7875c1b00000000a040`（21B） | 连续DWORD +1B58=40400000、QWORD +1B5C=0000000040A00000；后者同时覆盖+1B60..63。 |
| 36B25F | `48890522b06f00`（7B） | 向A66288写入RAX；构造返回来源仍由上文保存的调用链支持。 |
| 2C07C6 | `488bf9`（3B） | RDI=RCX。 |
| 2C1B8B | `488bc7`（3B） | RAX=RDI。 |

本reader实际新增34B代码锚点、77B导入metadata、4B常量，共115B语义读取，10次固定read；before/after各一遍64KiB缓冲全文件哈希，0次dumpbin。加上324560 reader的123B代码/168B metadata，本轮合计 **157B code + 245B metadata + 4B data = 406B语义读取、1次dumpbin、4遍Game原件全文件哈希**。全文件哈希字节不计入语义读取预算。文件身份检查也不等于运行时原子快照或官方兼容验证。

本附录为本轮静态证据的最终状态；独立产品模型审查另存 `model-review.md`，不将尚未执行的模型称为验证通过。

## 最小后续定点建议

1. 不再重复读取GameEngine整个构造体。2C18D1直接调用的2C8750是明确的后续依赖候选；本轮已决定不追其新正文。以后若获准，只围绕+1B58/+1B5C直接writer、数据源和精确key制定最小范围，不恢复整个GameEngine或自动追所有callee。当前保留未知。
2. 324560精确正文、scale文件值与固定rand导入绑定均已取得，不重复读取；rand运行时状态/返回观察、SSE环境和未知异常路径仍是独立条件，不自动跟随子调用。
3. 若仅做Item端条件性模型，应由显式实时own对象观察提供slot及两DWORD，保留上述读取/写入前缀；不得把默认值或A66288磁盘初值当作runtime初始化证明。任何磁盘slot读取都只能记录文件初值。

## 来源SHA256

| 相对路径 | SHA256 |
| --- | --- |
| docs/phase-1/round-4/Game-function-evidence-bounded-v2.json | 1e8421fc6b83004fd39259ca19ba97faa7df520cf49200572cecd8a2bb80c502 |
| docs/phase-1/round-4/initialization-locations.json | bdc0e1838162463d18fce89dfea20fe6baf073d889db757e62c59ae3ed8faf1e |
| docs/phase-1/round-4/INITIALIZATION-GO-NOGO.md | 2db81d99ac1c9d242ad419420a620ce8d28b30f3dfa27243f1bc05e611186b41 |
| docs/task-kills/ghidra-selected/subscriber-constructor-routes.json | e907b67d57a568784a19f887a3c71883b0f7726a14defee449cf8693422ee165 |
| docs/task-kills/ghidra-selected/subscriber-constructor-routes.log | cfabe316432f2dcc9b2335382803f03a8a901268e7c1bca14d91d284fc527e16 |
| docs/task-kills/ghidra-selected/analysis.log | 087e9105325e5d3b69660b66899fa000cc566f774797f4b4ef74ee9ba9ac09b4 |
| docs/task-kills/acceptance.json | 1066ce6686bf5305263816dec52e7a56eff9f6403f3e27e6d6c05b45d9505cf7 |
| docs/rewards/rea-inventory-add/Item-constructor-primary-table-assembly.json | 7a078d11495b5b7c10a07d7b6309c9d633f8ec39dfc444e5f0decfbd5efe1a3f |
| docs/object-lifecycle/offline-item-return-dependency/inspection.json | fc0f85739966bf84241cc277b6c4d2ae7129a9daa27792ed1245e9cb7b409ff0 |
| docs/object-lifecycle/offline-item-return-dependency/324560-instructions.json | 1998091f5ea21ab1f993401c4c83fa793760f286621a7b7d9d21d6abd8d854ef |
| docs/object-lifecycle/offline-item-return-dependency/324560-exact.bin | 0863ea902ea83c1343553f6679e385094a1b5dc7bbd9c2a4106c546bd9954092 |
| docs/object-lifecycle/offline-item-return-inputs/inspection.json | 288a5ae6cc72a4a2e68b7c1f03f14476a2f1e5a1a9794fe2f9bdb710a09cba4f |
| tools/static_analysis/resolve_item_return_inputs.mjs（报告记录的执行版本） | 6900f004dc8961c67bb020080fa5f6822ba4a1be512fc93835875fff6c91269d |
