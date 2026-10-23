# Parser 查询准备与执行状态 — 2026-10-20

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

以下为历史检查点，按各自时点理解。

先前监控诊断检查点：[监控失败诊断与受控验证](MONITOR-DIAGNOSTIC-RESULT.md)。新增 PID/同 handle 创建身份、Win32 错误码和失败步骤的白名单记录，保持原有 fail-closed 条件。Native 诊断 296 条、既有 Job 回归 115 条、投影测试 30 条、controller mock 79 项及不释放 Rea 门的真实采样器集成均通过。

原 QFPI 故障没有复现，根因仍未知；仅完成诊断增强，节点查询继续暂停、未重试。07:05:20 UTC 核查分析进程与 Rea runtime 文件为 0。本阶段无 Rea/Java 启动或原版读取；缓存索引实现验收仍为1209条断言、25组场景及Demo，节点创建与完整LoadFile miss仍未完成。

以下保留先前阶段记录，以各自时点为准。

先前缓存索引检查点：[缓存索引发布实现与验收](CACHE-INDEX-PUBLICATION-RESULT.md)。一次 6.433 秒串行回归通过 **1209 条断言、25 组场景及 ItemQuestDemo**，新增 98 条断言、4 组场景。显式已挂链候选的查重、移除、索引发布及有界单层扩桶已实现；部分修改后的 Unsupported 保留累计副作用和首次停点，重复调用不会重做。节点创建、完整缓存发布及 LoadFile miss 仍未完成。

维护固定批次 02EA80/02EB40 **11/11** 完成且正常关闭，54 个记录身份退出。节点批次 01F170/022860 发出 4 次、完成 3 次，因采样器 QueryFullProcessImageName 异常失败，无函数 body；事后 22 个记录身份退出，分析进程和 runtime 文件均为 0。根因因缺 PID/Win32 错误码仍未确定，未修复或重试，节点查询保持暂停；7 个保护文件未改，Engine 前后哈希一致。

以下保留先前检查点，以各自时点为准。

先前缓存前缀检查点：[缓存索引前缀已实现并验收](CACHE-PUBLICATION-PREFIX-RESULT.md)。新69条断言、3组场景；一次5.857秒串行回归共 **1111条断言、21组场景，另有ItemQuestDemo通过**。显式已挂链入口的逆向查重、唯一键重链和端点更新已恢复；在02EB40/02EA80前保留状态并返回Unsupported，原生返回节点/inserted均未知，完整缓存发布及LoadFile miss仍未完成。

固定05DEB0/02E770查询 **11/11** 已通过并正常关闭；06:19:02 UTC复核66个记录进程身份全退、Java/临时运行文件0，Engine哈希不变。两份后续固定11-call计划（维护02EA80/02EB40、节点01F170/022860）分别保存，均未注册或执行，不能自动追踪。

以下保留此前检查点，均以各自时点为准。

此前混合解析检查点：[混合 tag0/1/2 parser 已实现并验收](BINARY-MIXED-PARSER-RESULT.md)。三组原始 word 与各自 offset 由真实字节解析生成，跨类型重复键按统一索引前插；新增43条断言、2场景。一次5.817秒串行轻量回归共 **1042条断言、18场景，加ItemQuestDemo通过**。完整LoadFile/工厂/Read依赖已核对，仍保留Unsupported，没有以成功占位接链。

缓存发布下一固定计划为05DEB0/02E770各三项分析，共11calls。已离线核验，当前封存时未注册/未启动；OW窗口随后已明确归还，实际执行仍需实时资源与逐次批准。混合实现阶段没有Rea/Java或原版读取。

以下保留此前检查点，均以各自时点为准。

此前tag2检查点：[Binary parser 有界实现已验收](BINARY-PARSER-RESULT.md)。已从实际 fresh/tag2 记录字节生成 getter 使用的 fields/values；新增 69 条断言、2 个实际解析到 Class getter 场景。相关模块 979 条断言及 16 场景和 ItemQuestDemo 通过，独立 Object ID 补跑 20 条通过，总计 **999/16 + ItemQuestDemo**。首次包装脚本在模块全部通过后遇到 Object ID 测试路径错误，原日志保留，不能把该次包装命令写成 exit 0。

必要 helper 两批 11/11 与 7/7 均完成并释放；最后释放时间为 2026-10-20T05:47:32.1182412Z。后续无 Rea 查询或重负载编译；最终 Java/decompile 为零、runtime 文件为零、Engine 哈希不变。完整 LoadFile miss、archive/name population、原生基类初始化及具体构造/Read 仍未闭合；其他 tag/field-count/坏输入不宣称等价覆盖。

以下是此前检查点的历史记录，其中“尚未实现/未重跑”等状态以各自记录时点为准。

此前查询检查点：[parser固定查询已完成](BINARY-PARSER-QUERY-RESULT.md)。专用Job与启动门控修复后，最终8/8调用通过，parser伪代码/汇编/直接callee已保存，正常MCP关闭、Rea退出0、Job为空均确认。68个记录进程身份已全部退出，runtime文件0，原版哈希不变。76项纯Node测试、115条真实Windows断言及门控联测通过；本轮没有新增行为实现或重新运行930/14历史行为验收。

以下为第一次执行前后保留的历史准备记录；“尚未重试/未取得正文”等描述不再代表当前状态。当前以[完成结果](BINARY-PARSER-QUERY-RESULT.md)及[最终守卫验证](GUARD-REPAIR-VALIDATION.md)为准。

## 历史：首次失败后的准备记录

**当时状态：OW交回窗口后已执行一次，固定8-call批次发出4次、完成3次；第4次因无法归属的Java进程触发守卫停止，没有取得parser正文。** 自有进程已退出，临时目录已验证归属后清理，运行文件为0；原版哈希不变。详见[失败批次与释放结果](BINARY-PARSER-ATTEMPT-RESULT.md)及[原始摘要](rea-binary-parser-entry/summary.json)。

本页以下离线验证描述的是执行前检查点，不能当作当前重试就绪证明。27项模拟测试没有覆盖本次暴露的短命中间父进程归属及辅助Java角色；必须先修复并验证这两点，继续保留384MiB主分析堆、4.25/2GiB双余量守卫。未知Java不能按名称或路径直接放行。本次不是预算不足，也不是审批拒绝；尚未重试。

## 已配置的固定范围

[独立计划](../../tools/static_analysis/rea-binary-parser-entry-plan.json)只针对已绑定的Engine `0x18013F510`，固定8 calls：

`session → open固定Engine → session → 伪码 → 汇编 → 直接callees → close → final session`

计划SHA256为`adf41b0f3ec4d59f11352b21eb3173c8cb077a638a452c0f21f488b6b21251ff`，已固定在runner。四份地址来源哈希匹配，计划参数通过已保存工具schema验证；真正执行时仍须比较当前MCP schema和固定Engine哈希。本轮没有为了验证而打开原版或启动MCP。

- 执行模式：`rea_native_registration.mjs --binary-parser-entry`；本次已执行且失败，**修复归属与角色检查前不得重试**。后续仍须确认窗口和实时资源。
- 安全离线检查：`rea_native_registration.mjs --verify-binary-parser-plan`。它在原版哈希读取、实时预检及任何spawn前返回。
- `docs/object-lifecycle/rea-binary-parser-entry`现已保存失败批次；不得删除或覆盖来重试，后续尝试需独立证据目录。
- 不自动追查callee、基类构造或其他地址；额外查询仍需重新评估范围和预算。

## 已应用的任务预算

[预算模块](../../tools/static_analysis/rea_resource_budget.mjs)固定`engine-parser-384m-v1`：

| 项目 | 数值及行为 |
| --- | --- |
| Java堆 | 384MiB，专用wrapper；必须观察到归属Java实际`-Xmx384M` |
| 启动 | physical available与system commit headroom均 **≥4.25GiB** |
| 运行停止 | 两项任一 **<2GiB**；恰好2GiB仍允许 |
| 并行 | 一个自有后端、MCP串行；观察到其他UE/编译/Java重负载则停止自有树 |
| 数据有效性 | 样本年龄最多5秒；无样本/接收停滞超过15秒触发失败；EOF、异常退出或坏数据不等超时 |
| 失败处理 | 锁存错误、阻止后续请求；只针对本次自有Rea树，不停止OW或其他进程 |

双指标预检在Rea spawn之前执行，使用固定只读PowerShell脚本、10秒超时；计数无效、预检失败、过时或存在重负载均不准启动。临近spawn再次检查快照新鲜度及物理余量。运行中由归属采样检查两种余量，独立物理监控继续保留。

任务额度2.25GiB来自上一轮完整任务较大扫描峰值1.225800GiB，乘1.5、加0.25GiB并向上取整；再加2GiB系统保留量得4.25GiB启动线。加成与保留量是明确工程假设，不是实测最低需求。仅适用于此次Engine指纹、Rea/Ghidra版本、384M及相近固定查询；不是任意后续查询的上界，也没有做低余量实机压力试验。

原项目512M环境模块、`.codex/config.toml`与CPU设置未改变；旧批次的6/5线保留其历史含义，不覆盖新parser模式。所有新参数仅在工作区，可由[修改前备份](../../.tooling/checkpoints/parser-budget-before/manifest.json)恢复。

## 监控收尾修复

`beginFinalization`不再关闭预算监控：只允许关闭期间预期的根进程消失。直到runner收到自有Rea的实际exit事件，低余量、外部重负载和监控失效仍触发一次自有树停止。确认后端退出后，迟到错误仍计入失败，但不按过期PID再执行停止。

采样器退出事件与输出流关闭分别记录；等待`close`、处理尾部数据及不完整JSON帧后才写摘要。拒绝退出或输出无法排空会明确标为不确定，不静默报告成功。一次停止锁存与多条失败记录分开。

## 已执行的离线验证

[测试日志](parser-budget-validation/guard-tests-v2.txt)：**27/27 tests通过**，测试主体约109ms；仅使用模拟子进程、内存流与历史采样，未运行真实taskkill、Rea、Java或采样器。覆盖：

- 两种启动指标恰好4.25GiB与少1字节，两种运行指标恰好2GiB与少1字节。
- 无效/过时计数、预检超时/坏JSON/诊断、外部重负载、无root、PID创建身份变化、384M不符及监控失联。
- 全部70条历史样本重放，归属峰值与原始实测一致。
- exit早于stdout尾部、迟到坏数据、不完整帧、拒绝退出及输出未排空。
- 关闭期间低commit/外部负载/坏数据/失联仍停止存活后端；后端确认退出后的迟到失败只记录。

[离线计划校验](parser-budget-validation/offline-plan-verification.json)确认固定来源、预算和8-call参数。JS及新PowerShell脚本语法通过。测试并不等于完整MCP生命周期已在新预算下实跑；下一窗口仍需实际执行及释放核验。

## 为什么没有新增行为实现

目前闭合的是零count初始化及调用目标绑定。`RecordLoadFile` miss仍为Unsupported，返回表仅来自缓存命中；`BinaryWarmState`需要真实的Fields/Values及archive名称状态，构造器零值无法生成这些数据。空archive返回原default已被已有getter覆盖，再加空表包装不增加功能。

下一最小有效目标为：**记录字节经过有证据支持的parser分支生成字段状态，再由现有GetValue读取Class。** 先查询`13F510`明确读取格式、宽度、游标和写集。caller不检查parser返回值，而检查reader`+0x14`字节决定是否释放`+0`指针，因此不预设`bool ParseSuccess`接口或buffer所有权。

完整构造另需`13C1D0`的基类/gate行为；把LoadFile miss接成成功还需`05DEB0/02E770`的发布及相关副作用。没有用callback、预填snapshot或空record替代这些证据。

行为源码、已编译程序和已有930断言/14场景验收未变，本轮没有运行行为构建。没有原游戏启动、原版读取、存档/云数据访问、安装、系统改动或commit/push；没有审批拒绝或不确定的真实后端执行。

2026-10-20 static registration investigation: [Item/QuestItem writer evidence and remaining gaps](CLASS-STATIC-INIT-RESULT.md). The bounded Game CRT initializer table has located Item slot 115 and QuestItem slot 205. This is static evidence, not proof of successful runtime registration or complete native item construction. Product sources remain unchanged; previous 2282 assertions / 83 scenario groups were not rerun. See the result document for exact store order and blockers.

2026-10-20 registration helpers: [bounded helper evidence and local Item/QuestItem model](CLASS-REGISTRATION-HELPERS-RESULT.md). The native allocation/release/exit-registration call chains are now identified. The own local writer model preserves duplicate appends and partial effects; external allocator/runtime behavior and the Engine root registry remain outside it. Only the new targeted tests ran; previous 2282 assertions / 83 scenario groups were not rerun. Formal sample compatibility remains unverified.
