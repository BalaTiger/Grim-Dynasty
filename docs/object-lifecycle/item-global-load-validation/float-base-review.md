# Base浮点getter13CA00独立静审

本审阅只读归档指令及相关旧合同，没有读取原版、执行reader/构建/测试或修改实现。结论：Base树搜索部分可复用既有稳定有效树合同，但匹配后的4DD80格式解析仍是明确依赖；不得使用旧字符串getter的空值默认规则或CLR Parse替代。

## 正文与身份

`offline-item-global-load-getters/inspection.json.targets[0]`记录LoadTable导出vtable341F10的+48槽341F58，raw `00ca138001000000`，指向Engine13CA00。exact pdata5245为[13CA00,13CB96)，raw `00ca130096cb1300c8373800`，406B/107指令，逐字节覆盖成功，decode_issues为空。整个四目标报告status为`fixed-global-load-getters-with-frontiers`，failures为空、source_closed/hash_preserved/stat_stable均true；这不是其它三个无exact入口已取得正文的证明。

## 查询与旧搜索的可复用部分

- 13CA23先把输入XMM2保存到非易失XMM6，作为原始默认值；13CA26保存query到RBX，13CA29保存this到RSI。
- 13CA2C令RDI=this+18，13CA34读[RDI+10]即this+28的锁指针，13CA38调用IAT302328。锁内初始化局部字符串capacity15/length0/首byte0，扫描query到首NUL，即使树为空也不省略这些步骤。
- 13CA76调用既有022CE0，以已扫描长度复制query；13CA87调用既有13EBC0，容器为this+8。13CA8F取该容器sentinel；候选非sentinel时，以min(query length,candidate key length)比较query与candidate，长度为0时跳过memcmp，非零在13CAD6调用既有2EE060。
- 13CADF..13CAF6用长度打破相同前缀，并以有符号比较结果是否为负决定拒绝候选。单靠这一比较是query>=candidate；结合13EBC0的lower_bound及稳定有效有序树前提，才推得exact key match。不是任意/损坏树上的全等证明。
- 13CB10必要时释放局部query的heap储存，然后重置其元数据；13CB35才重新比较候选与当前sentinel。正常分配、复制、释放和稳定树域可沿旧Base字符串getter合同复用，不能宣称原生OOM/EH或并发修改行为已恢复。

既有证据证明13EBC0按完整stored key字节和长度排序；query使用首NUL前缀，存储key不能截断。2EE060是已绑定VCRUNTIME140!memcmp的thunk，比较为unsigned-byte lexicographic；没有大小写、路径或locale转换。现有`LoadTableSnapshot`的完整、有界、唯一排序视图可复用结果选择，但不应声称原生树形、比较次数或坏指针行为等价。

## 匹配后的局部解析合同

13CB2C先把XMM6低32位写入局部 `[rsp+A0]`。如果候选是sentinel，13CB39直接跳13CB6B，不调用4DD80。若匹配：

| RVA | 操作 |
| --- | --- |
| 13CB3B | RCX=node+40，即stored value字符串对象。 |
| 13CB3F..46 | 以value capacity(node+58)是否>=16选择heap指针；否则保持inline node+40。 |
| 13CB49 | R8=&局部[rsp+A0]，该DWORD已初始化为原default位。 |
| 13CB51 | RDX=316034，固定format来源地址。 |
| 13CB58 | CALL4DD80。 |
| 13CB5D..60 | 比较EAX与1；任意不等于1的**正常返回**跳13CB6B。 |
| 13CB62 | 仅EAX==1时，从局部[rsp+A0]加载低32位到XMM6。 |
| 13CB6B..6F | 重新读锁指针，调用IAT302330离开锁。 |
| 13CB75/95 | XMM0=XMM6，然后RET。 |

这里**没有node+50的stored value length空值检查**。旧字符串getter13C6B0会对stored length0选择默认，13CA00不会这样提前决定；匹配的空串、首byte为NUL或包含其它文本均进入4DD80，除非其指针/存储本身无效。此caller也没有扫描value、验证全串消费或验证返回浮点是否有限。

默认与失败必须区分：

| 已知正常路径 | 返回位/状态含义 |
| --- | --- |
| 无exact key | 保留原default低32位；仍需此前query处理和正常unlock。 |
| 匹配且4DD80正常返回EAX!=1 | 保留原XMM6/default，即便helper可能修改过局部输出。 |
| 匹配且4DD80正常返回EAX==1 | 使用helper返回后局部DWORD的实际位；caller本身不证明helper必然写了全部4B。 |
| helper尚无正常返回、返回EAX未知、EAX==1但局部输出未知 | 未闭合依赖，不是已知解析失败；不能改成default成功。 |
| 输入/锁/候选/存储未知，或分配/释放/解锁异常 | 无已证正常返回；不能通过默认值掩盖。 |

默认值保存和返回只是寄存器/内存位复制，因此这段本身没有将default位标准化或以数值转换替换NaN/负零等模式。解析helper的浮点、locale、舍入、范围和异常合同仍未证明。就本体而言，EAX==1只是一个精确数值判据，不足以推出format内容、输出类型或任何C/CRT API名称。

helper在锁内、局部query已清理后调用；正常return之后才unlock。若helper有副作用或抛出，不能把自有finally解锁包装成已恢复原生EH，也不能回滚已经观察的前缀。返回是低32位数值位模式，不是借用value字符串的返回指针；实际解析期间仍需value存储有效且稳定。

## 必须新增的最小依赖边界

1. 由13CB58的直接CALL定位Engine4DD80，先取固定exact pdata或明确有界入口，再核其实际正常路径。若它只是包装/转发，只有实际观察的callee或导入可以继续绑定；不能先给它起scanf之类的身份。
2. 由13CB51定位316034，固定小上限逐字节到首NUL读取格式原始bytes；不假定`%f`或接受何种尾随输入。如果发现需第二格式/选项/locale，也必须由新正文具体操作数限定。
3. 在helper/格式未闭合前，最多实现明确的外部normal-return与局部输出观察合同；若目标是实际数据库float解析，则仍应保留此缺口，不以CLR文化、TryParse成功率或十进制转换实现冒充原生算法。
4. 该Base身份只适用于明确LoadTable的+48。二进制表入口13FD50及ObjectManager wrapper关系需各自新证据；不能根据同一签名自动复用Base parser路径。

## 来源SHA256

| 相对路径 | SHA256 |
| --- | --- |
| docs/object-lifecycle/offline-item-global-load-getters/inspection.json | 4be3c63f988ab540100dbb80129506bc13fa254f5bfa868fd358b45090900b78 |
| docs/object-lifecycle/offline-item-global-load-getters/base-float-13ca00-instructions.json | 67b17a2dbd57ae39caf3b31f8c1fa9ca97997372b5a36dfb1fbd1de6debb9183 |
| docs/object-lifecycle/offline-item-global-load-getters/base-float-13ca00-exact.bin | 16074498dbf35484d195352e7f556d196563a974cc5b68d89b3f7421e9b3f0cc |
| docs/object-lifecycle/CLASS-GETTER-EVIDENCE.md | f97e8c031e5f33e4108f5d3749f4c6b8c6b677d3744339bd2733c84411daa41b |
| docs/object-lifecycle/rea-record-getter-support/05-LoadTable-tree-search-0x18013ebc0-assembly.json | 5f780de8caae1f7420feab6e8fb020da924259b34d4534eee4ca805a6ee8165d |
| docs/object-lifecycle/rea-record-getter-support/08-CString-copy-0x180022ce0-assembly.json | d9a273b1b818d9a83aaffd434bd592aedfdebcb14a74ca23b3e1a2431cc7a05e |
| docs/object-lifecycle/record-load-discovery/memcmp-import.json | da3d62fe0d3b6ad9d8f501b1311b86216cf367c2072121bdc4632b0387fe9618 |
| docs/object-lifecycle/rea-record-cache-compare/05-Record-cache-compare-0x1802ee060-assembly.json | 626c6d2295f6614850500f4c1b4299a97a08fa8dd2e9f80210c3cc985d03c9f2 |
