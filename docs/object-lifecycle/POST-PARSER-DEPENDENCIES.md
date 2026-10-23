# Parser 之后：LoadFile miss 的真实依赖

最新检查点：[类注册追查与 Item 局部初始化](CLASS-REGISTRATION-AND-ITEM-SCALARS-RESULT.md)。新增 Item 323714..323743 六次标量写入段，18 条新断言通过；fresh 相关回归共 2282 条断言 / 83 组场景通过。具体类 getter 仅返回静态 ClassInfo，已读虚表槽呈清理形态；name/children 写入者、根注册及官方 Class 实际工厂分派仍未恢复，未生成猜测注册图或完整对象。首次 CS1513 编译失败已修复，失败记录保留，成功在独立 retry1 目录。未重读官方 ARZ；正式兼容、计时全局、一般归档生命周期继续未验。

以下保留历史阶段记录；当前注册与局部构造覆盖以上述结果为准。

最新证据检查点：[QuestItem 工厂与 Read](QUESTITEM-FACTORY-READ-RESULT.md)。已确认工厂、具体构造后缀、最终虚表 Read、五个派生读取键及七个导入；4 段共 1679 字节 / 394 指令。完整官方物品对象闭环仍阻于运行时类注册、Item/Actor 构造和 Actor::Load、图形资源服务及读取状态合同。本轮没有产品代码变更、新产品测试或官方 ARZ 读取；此前 2264 / 83 与单记录 123 断言仅保留为前次通过记录。原 Game 哈希保持，未启动原游戏/Rea/Java、安装扩权、接触存档/云或 commit/push。

以下保留历史结果；完整物品构造/Read 的当前状态以上述证据检查点为准。

最新检查点：[初始化与完整原 ID 缓存链](NATIVE-INITIALIZATION-CACHE-RESULT.md)。相关自有回归 2264 条断言 / 83 组场景通过；单条正式 scrapmetal 自有完整池链路另有 123 条断言通过，82,999 项原 ID 保持，首加载、Class getter、重复与关闭来源后命中均已验。正常 fresh 初始化前提有静态依据；显式自有时钟不代表四个原生计时全局初始化已恢复。完整原生兼容、工厂/注册、非 fresh 合并生命周期仍未验。原版哈希不变，无原游戏/Rea/Java、存档/云、安装扩权或 commit/push。

以下保留历史阶段记录；其中已闭合的旧阻断以上述新结果为准。

最新预检：[官方单记录缓存链停点](OFFICIAL-CACHE-CHAIN-PREFLIGHT-RESULT.md)。真实82999项池全字节唯一，空串原ID82687，最大259 B；原档前后哈希一致。检查器47/8独立自检通过，产品1971/57与官方38字段parser结果未重跑。源/完整池预算和archive/depot初始化、转换后tick仍阻止官方缓存首发；重复命中、关闭源后命中及新官方失败测试未执行。未填空上下文或伪造时钟，生产源码与限制不变。预检显式数组/缓冲4.81 MiB，Node maxRSS50.12 MiB，不是缓存链峰值。

以下保留历史检查点，不能把此前独立parser成功解释为本次官方缓存链通过。

最新检查点：[tag3 与官方废铁记录解析](BINARY-BOOLEAN-PARSER-RESULT.md)。自有回归 1971 断言 / 57 场景组通过；同一官方 scrapmetal 的 38 字段已由更新后的 parser 解析并逐项对照，7 个布尔位为 0x30。保留原 ID，不提供或重编号字符串池；完整多归档初始化、getter/factory 及原生分配/EH 仍未验证。16/17、32/33 字段的 log 舍入边界明确拒绝，非全部字段数兼容。全库预算未变，无原游戏/Rea/Java、存档/云、安装扩权或 commit/push。

以下保留历史检查点，其中旧覆盖范围及限制对应当时阶段。

最新实际数据检查点：[本机官方 ARZ 单记录检验](OFFICIAL-RECORD-PROBE-RESULT.md)。本机 database.arz（58,529,720 B，v3，82,999字符串）已实际只读验证。scrapmetal 单条323 B→456 B，两自有decoder逐字节一致；现有parser实际在第5字段tag3拒绝，游标0。完整source池预算也实际拒绝；38字段超15覆盖域是另一个明确缺口。独立wire观察不计作native parser兼容；未提高生产预算、未新增合成断言/重跑全套。原归档前后哈希相同；无原游戏/Rea/Java、存档云、安装扩权或commit/push。

以下保留历史检查点；此前“正式样本未读”描述仅适用于各自当时的阶段。

最新检查点：[归档字符串映射生成结果](ARCHIVE-STRING-MAP-RESULT.md)。自动生成去重池、追加 mapping/base，并绑定实际来源池接入重映射加载链。三个自有物理 ARZ 验证同串异 ID、重复项及顺序；首次回归 **1847 条断言、52 组场景、Demo 与 PEReader 通过**（新增122/6）。初始重复 ID 的加载拒绝、原生构造/指针缓存/offset 选择器与 allocator/EH 缺口明确保留；正式样本兼容未验。9次定点 dumpbin 共3326字节/895指令，原版哈希不变；无原游戏/Rea/Java、安装扩权、存档云访问或commit/push。

以下为历史检查点，按各自记录时间理解。

最新检查点：[记录体字符串重映射验收](RECORD-REMAP-RESULT.md)。280870已完成231字节/65条指令定点补证，无新helper；有界原地接口按字段类型处理key和字符串值，保留回绕、范围外相加结果及已写前缀。显式映射与目标池接入自有ARZ读取/解析/缓存链，旧重载不自动remap。

最终 **1725条断言、46组场景、Demo与PEReader通过**（新增82/5）；首次新增测试参数类型编译失败已保留，修正后完整重跑一次通过。两份不同字符串池/重复字符串合成ARZ、越界ID和部分写入均验证。未恢复原生映射生成、归档合并初始化、scratch生命周期及完整factory；未读官方样本，不宣称官方兼容。原版哈希不变；未运行原游戏/Rea/Java，未安装扩权、访问存档云或commit/push。

以下保留历史检查点，结论按其记录时间理解。

最新检查点：[输出长度解码器验收](NATIVE-DECODER-RESULT.md)。先评估136字节入口，再沿已见分支补齐500字节及32字节复制表，确认278D20的LZ4 fast类输出长度语义。新增显式安全profile，保留旧输入长度decoder；真实自有ARZ经parser和缓存接合，一次回归 **1643条断言、41组场景、Demo及PEReader通过**，新增94/4。

成功返回输入消耗量、允许尾随输入；安全guard的Unsupported与原生长度负返回分开，未复制原生越界内存及失败写入。未读取官方样本或声称兼容。回归完成后仅评估已有280870记录，缺少函数体，remap/初始化/factory等仍未闭合。原版/副本哈希一致；未启动原游戏/Rea/Java、未安装扩权、未触存档云、未commit/push。

以下保留历史检查点，按各自记录时点理解。

最新检查点：[有界只读 ARZ 接合验收](ARCHIVE-READ-RESULT.md)。真实自有 ARZ 文件经 raw LZ4 → 原始字符串池/body → mixed parser → 同一 Table 与缓存发布；关闭来源后普通命中通过。一次构建通过 **1549 条断言、37 组场景及 Demo**，新增149/6，PEReader检查通过。

本轮自有 fixture 通过不代表官方或原生兼容。官方样本规模仅引用旧报告元数据；记录指向的本机项目路径未找到，未推断删除。固定 dumpbin 六次共800字节，原版/副本哈希一致；278D20 decoder、280870 remap、native初始化/factory/Read仍未闭合。未启动Rea/Java/原游戏、未访问存档云、未安装扩权、未commit/push。

以下保留历史检查点，按各自记录时点理解。

最新检查点：[有界 supplied LoadFile miss 接合与验收](SUPPLIED-LOADFILE-RESULT.md)。复用既有静态证据，将真实 mixed parser、同一 Table、fresh 名称复制/移动、节点构造、caller count/邻链写入及索引发布接入同一 depot。成功后普通 LoadFile 可命中新缓存；部分停止保留图和副作用，并阻止旧缓存被当作可用状态。

一次串行回归 **1400 条断言、31 组场景及 Demo** 全通过（新增140/4），PEReader元数据检查通过。实际 archive 取数、完整 native ctor/factory/Read、时钟转换与 EH 仍未恢复；只有明确供应已核验 bytes/context/转换后tick的有界分支可成功，普通缺数据 miss 仍明确拒绝。本轮未读原版或副本、未启动 Rea/Java/dumpbin；上轮原版哈希4c2ea14c…a93153证据保留，无安装、提权、存档访问、commit/push。

以下为历史检查点，按各自时点理解。

先前检查点：[离线节点构造与验收](OFFLINE-CACHE-NODE-RESULT.md)。使用已有 dumpbin 静态取得01F170、022860及已知caller共289字节，逐字节交叉核对，并定点绑定memmove导入。独立节点构造的正常成功域已实现：短串复制、堆存储身份转移、源重置及Table复制；不隐式挂链或增加count。

行为回归1260条断言、27组场景及Demo通过（新增51/2）。原wrapper在测试后的ReflectionOnly元数据步骤失败，记录保留；只读PEReader补检通过，未重跑测试。Rea/Java未启动，节点查询继续暂停，监控故障根因未知。完整LoadFile miss、原生allocator/EH及完整工厂仍未完成。原版哈希保持4c2ea14c…a93153，无安装、提权、存档访问、commit/push。

以下为当时的依赖分析；RCX来源及节点正常构造缺口已由上述新证据关闭，其余完整调用链边界保留。

最新诊断更新：[监控失败诊断与受控验证](MONITOR-DIAGNOSTIC-RESULT.md)。新增 PID/同 handle 创建身份、Win32 错误码和失败步骤的白名单记录，保持原有 fail-closed 条件。Native 诊断 296 条、既有 Job 回归 115 条、投影测试 30 条、controller mock 79 项及不释放 Rea 门的真实采样器集成均通过。

原 QFPI 故障没有复现，根因仍未知；仅完成诊断增强，节点查询继续暂停、未重试。07:05:20 UTC 核查分析进程与 Rea runtime 文件为 0。本阶段无 Rea/Java 启动或原版读取；缓存索引实现验收仍为1209条断言、25组场景及Demo，节点创建与完整LoadFile miss仍未完成。

最新执行更新（2026-10-20）：[缓存索引发布实现与验收](CACHE-INDEX-PUBLICATION-RESULT.md)。一次 6.433 秒串行回归通过 **1209 条断言、25 组场景及 ItemQuestDemo**，新增 98 条断言、4 组场景。显式已挂链候选的查重、移除、索引发布及有界单层扩桶已实现；部分修改后的 Unsupported 保留累计副作用和首次停点，重复调用不会重做。节点创建、完整缓存发布及 LoadFile miss 仍未完成。

维护固定批次 02EA80/02EB40 **11/11** 完成且正常关闭，54 个记录身份退出。节点批次 01F170/022860 发出 4 次、完成 3 次，因采样器 QueryFullProcessImageName 异常失败，无函数 body；事后 22 个记录身份退出，分析进程和 runtime 文件均为 0。根因因缺 PID/Win32 错误码仍未确定，未修复或重试，节点查询保持暂停；7 个保护文件未改，Engine 前后哈希一致。

以下计划中的“未注册/未执行”均为其历史记录时点；当前以上述执行结果为准。

后续更新（2026-10-20）：混合tag0/1/2已完成，随后固定缓存发布查询11/11通过并释放；[缓存索引前缀验收](CACHE-PUBLICATION-PREFIX-RESULT.md)为当前检查点（1111条断言/21组场景）。原计划“未注册/未执行”是下文历史记录时点；目前的新缺口为02EA80/02EB40及01F170/022860，后续数据计划尚未执行。**以下保留查询前的依赖分析，不作为最新执行状态。**

2026-10-20，离线核对。OW 当前占用重负载窗口；本次没有启动 Rea/Java、读取原版、编译、修改行为代码、commit 或 push。本文只记录已有调用证据和下一查询建议；[dependencies.json](post-parser-boundary/dependencies.json) 保存来源哈希、缺口与查询范围，不是 runner 计划或执行许可。

**完整 LoadFile miss 仍未闭合。** 已恢复的有界 parser 不替代 archive 取数、普通表构造或 cache publication。[RecordLoadFile.cs](../../src/task_items/RecordLoadFile.cs) 仍在 miss 返回 `UnimplementedRecordCacheMissLoader`；[RecordCacheSnapshot](../../src/task_items/RecordCache.cs) 只有稳定快照查询，没有原生发布入口。当前可离线继续的是已证 parser tag0/1/2 的原始 32 位数据路径；mixed-tag 增量正在实施，本文不把它列为已经通过验收。

## 已证调用链与参数

核心依据为 [1C4D20 wrapper 汇编](rea-object-base/11-TableDepot-LoadFile-0x1801c4d20-assembly.json) 和 [1C4E20 miss 汇编](rea-record-load-dependencies/11-LoadFile-miss-0x1801c4e20-assembly.json)。后者 evidence ID 为 `ev_23bf6a83dc20e5e716f1454ed05f04ab266b5e67430c35070017de7c68eb1937`，文件 SHA256 为 `d696e7b2141e5fe9ee6f518555d32a615ab174deabd6f676a15c299be1b7e3b1`。以指令参数为准，伪码省略参数或推测的额外参数均不作为 ABI。

| 阶段及准确调用点 | 已观察输入、操作和边界 |
| --- | --- |
| `1C4D4C`；`1C4D79 → 023180`；`1C4D84 → 27B3C0` | wrapper 使用 depot `+78` 的锁，复制完整名称后处理全 stored length。`record-load-discovery` 的 [tolower](record-load-discovery/tolower-import.json) 和 [memcmp](record-load-discovery/memcmp-import.json) 仅绑定磁盘 import，不证明运行时 locale 或 archive loader。 |
| `1C4D97 → 03C8E0`；miss 时 `1C4DAF → 1C4E20` | cache 容器为 depot+8，传入已处理名称。miss helper 运行于外层 depot 锁内；hit 另在 `1C4DB9` 增加 depot+54。 |
| `1C4E89`；`1C4EAF → 03C8E0` | 从 `gEngine+3D0` 取得 archive。名称 stored length 为零时直接走 fallback；否则对 archive+88 查完整名称，非 sentinel 节点的 `+30` DWORD 是 raw uint32 ID；`FFFFFFFF` 走 fallback。 |
| `1C4EDF`；flag set 时 `1C4EF3 → 280E20` | 检查 archive+2E 的 bit0。调用时 `RCX=archive, RDX=&dataPointer, R8=&lengthDWORD, R9D=ID`，两个输出此前为零；调用后的 RAX 不作成功判定，而是读取输出指针/长度。未见独立 body，不能命名为已恢复 LZ4 reader，也不把伪码中的 unwind 临时值当第五业务参数。 |
| flag clear 的 `1C4F04..1C4FAC` | 以 ID 四个 little-endian 字节做 unsigned FNV1a64，mask 为 archive+78；archive+60 是 16 字节 bucket endpoint 数组，sentinel 为 archive+50。沿 next 查 node+10 的 uint key。命中后还检查 archive+28 或 archive+20 非零；从 node+18 的 metadata 取 DWORD `+28` offset、`+2C` length，dataPointer=`archive+20+uintOffset`。该分支不是整个 ARZ 文件格式或压缩解码的证明。 |
| `1C4FB4..1C4FBF`；`1C4FC8 → 2DC398`；`1C4FE2 → 13F300` | 非 null data 建立临时 reader：base=current=data、length DWORD、ownership byte=0。申请 0xD0 字节，并重新读取 `gEngine+3D0` 作为 Binary ctor 的 archive 参数。不能默认两个全局读取之间 archive 永远相同；正常稳定上下文须显式成立。 |
| `1C4FF4 → [returnedTable.vptr+8]` | 既有槽、ctor 零分支和 parser 档案已将正常路径接到 `13F510`。该调用没有 parser bool 成功检查。`1C4FF8` 检查 reader ownership byte，非零时 `1C5002 → 2DC8F8`；当前 bounded parser 不改这个 byte。 |
| fallback `1C5009..1C504B` | 先增加 depot+58，再调用 engine 虚表+18 的真实诊断，申请 0x30 字节并在 `1C5046` 调 `13C1D0`。普通 LoadTable ctor 的独立 body 未找到；不能把数据缺失实现为 Known null、空表占位或空诊断回调。 |
| 汇合 `1C5070 → 023180`；`1C508B → 05DEB0` | 复制已处理名称到临时 `{name, table}` payload。随后明确设置 `RDX=当前首节点, R8=首节点.prev, R9=&临时payload`；**RCX 未在前一 023180 调用后显式重设**，其作用必须由 body 确认，不能猜一个 allocator/this 参数。RAX 返回的节点参与后续链接。 |
| `1C50B5..1C50DA → 02E770` | 先增加 depot+18 的 qword list count，再写首节点.prev 和返回节点.prev.next。之后 `RCX=depot+8, RDX=&输出栈槽, R8=当时 sentinel.next+10, R9=当时 sentinel.next`。helper 是否处理重复、重排桶、维护负载或增长未知；不能用 Dictionary 赋值代替。 |
| `1C50F7 → 023090`；`1C50FC..1C5149` | 清理临时名字后增加 depot+50；按时间源分支计算 elapsed，累加 depot+5C，并按 unsigned 比较更新 +60 最大值。计时、诊断、发布前缀与异常边界不得由“返回 table”占位掩盖。 |

## ARZ v3 为什么不能绕过 280E20

[旧 280000 档案](../resource-contracts/followup/Engine-function-evidence-bounded-v2.json) 的已存指令为：`280013` 读 archive+2C；值为零的首载路径在 `28002C` 读输入首 DWORD，`280033` 把它写到 archive+2C。[现有资源 codec](../../src/resource_codec/ResourceContainers.cs) 按 little-endian U16 marker、U16 version 读取文件头；marker=2/version=3 的四字节是 `02 00 03 00`，DWORD 为 `00030002`。因此，在该首载路径完成且相关字段未被后续改写的条件下，archive+2E 是 `03`，bit0 为 1，miss 将走 `280E20`。不能选 flag-clear 内联切片路径冒充这条 ARZ v3 取数路径，也不能只凭该位把 280E20 内部正式命名为某个压缩算法。

这份旧记录声明范围到 `2800D5`，listing 止于 `2800D3`，而伪码包含后续处理。上述结论仅使用范围内明确的 header 读写指令，不据伪码宣称整个导入过程已闭合。[数据库所有权档案](../resource-contracts/database-ownership/Engine-function-evidence-bounded-v2.json) 另有 `280160` 载荷、`280960` 字符串、`280220` 记录表、`2809F0` 摘要范围：记录 offset 重基址、追加时字符串 ID 映射及已有记录替换均有线索，依赖 helper 和完整存储模式仍未全部恢复。

## 资源 codec 的实际可复用部分

`ResourceInput(Stream).Read(offset,count)` 与 [Lz4Block.Decode(input,expected)](../../src/resource_codec/Lz4Block.cs) 是已有自有格式实现；`ArzArchive.Entries` 提供 offset、packed、unpacked。保留同一个 ResourceInput 时，可组合 `Entries → input.Read → Lz4Block.Decode` 得到原始 record body，不必调用原版或制造 table 回调。输入源、具体条目及大小必须明确且受界限检查。

不能把 `ArzArchive.ReadRecord` 的字典输出反向重建成 native parser 输入：它已把字符串 ID 转成文本，按字段名建立 Dictionary、拒绝重复字段，私有字符串池未公开；`ResourceUtil.Key` 还进行 Unicode `ToLowerInvariant` 和尾 NUL 去除，与已证 native byte-string 全长名称处理不相同。条目字段是公开可变值，独立 bridge 还需自有快照和范围校验。共享 archive pool、name→ID 索引、DLC 合并与缓存 ownership 不能由格式读者自动推出。

[历史格式验收](../compiled-containers/acceptance.json) 对照了 34,180 个 record body 和 9,623,280 个字段；[边界测试](../compiled-containers/boundary-tests.json) 与 [LZ4 验收](../resource-contracts/compiled-lz4-acceptance.json) 是格式层证据，不是本轮重测或 native LoadFile miss 场景验收。当前可无新增查询地推进两件独立工作：已有 tag0/1/2 复制分支的 bounded raw-word parser；之后可做原始 ARZ body 到 parser 的格式组合验证。二者均不能解除 RecordLoadFile 的 miss Unsupported。

## 推荐下一批与明确未执行项

优先查询 **`05DEB0` 与 `02E770` 两个 cache publication helper**。它们位于 Binary 成功和普通 fallback 汇合后的必经路径，先闭合它们才能描述真实 cache 节点与桶发布；单独补普通表构造只能增加构造证据，仍不能完成 miss。两者各 `pseudo + assembly + direct-callees` 为 6 次分析，加 5 次生命周期共 **11 calls**。若只取各自 pseudo+assembly，则是 **9 calls**，不能称为 11。

主线程已保存 [rea-record-cache-publication-plan.json](../../tools/static_analysis/rea-record-cache-publication-plan.json)，状态为 `prepared-not-registered-not-started`，固定 11 calls。它尚未注册 runner、未执行，必须等待 OW 释放窗口及既有批准和 fresh preflight。每个 helper 的 direct-callees 只用于列出缺口，不授权自动追入。继续沿用已验证 Job 归属与 384M / 4.25 GiB 启动 / 2 GiB 停止配置；旧测量不是新批次资源上界。

`13C1D0` 的 base/gate 初始化与普通 fallback 构造作为**独立后续**；`280E20` 取数作为**另一个独立后续**。不将这四个函数混进同一批。当前没有新查询执行、待批准动作或不明执行结果；未知步骤继续显式停住，不能补成功占位、supplied table loader 或 own 诊断回调。

2026-10-20 static registration investigation: [Item/QuestItem writer evidence and remaining gaps](CLASS-STATIC-INIT-RESULT.md). The bounded Game CRT initializer table has located Item slot 115 and QuestItem slot 205. This is static evidence, not proof of successful runtime registration or complete native item construction. Product sources remain unchanged; previous 2282 assertions / 83 scenario groups were not rerun. See the result document for exact store order and blockers.

2026-10-20 registration helpers: [bounded helper evidence and local Item/QuestItem model](CLASS-REGISTRATION-HELPERS-RESULT.md). The native allocation/release/exit-registration call chains are now identified. The own local writer model preserves duplicate appends and partial effects; external allocator/runtime behavior and the Engine root registry remain outside it. Only the new targeted tests ran; previous 2282 assertions / 83 scenario groups were not rerun. Formal sample compatibility remains unverified.
