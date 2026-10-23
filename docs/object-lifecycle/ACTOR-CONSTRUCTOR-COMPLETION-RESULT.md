# Actor 构造器：有条件热路径正常返回结果

本轮将此前停在 0x180025CC9 → BDBE0 的自有模型，接到子对象返回、Actor 发布及尾部正常返回。覆盖条件包括已选择的文件初始 guard profile、正常初次分配、有效独占且不重叠的存储、两次独立 TLS 热路径/来源字节观察，以及有效全局对象字段观察。**独立正式审批后的精确构建一次成功，新 fixture 通过 69 条断言、11 组场景。** 这是条件明确的自有正常路径模型，完整原生构造及正式样本兼容仍未验证。

## 新证据与归档绑定

地址均为 Engine.dll preferred VA，简写为 RVA。固定原件为 4,485,632 B，SHA256 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`。两批均检查原件前后哈希、状态稳定、句柄关闭及工具哈希，并逐字节验证所选精确正文的反汇编覆盖。

| 证据 | 范围 / SHA256 |
| --- | --- |
| [第一批执行记录](offline-actor-remaining-dependencies/inspection.json) | `6639fbb457720057edb6ea37834e6f5aa2534593b26020fda7d83044b73325c5` |
| [BDBE0 正文](offline-actor-remaining-dependencies/0bdbe0-instructions.json) | [BDBE0, BDF65)，901 B / `2deb380656810ec48388c9c68f8b5b319e5acd9d9733bfef84def99abe104240` |
| [第二批执行记录](offline-actor-subobject-direct-dependencies/inspection.json) | `50c454f0f9fa6ffd0d38852903e9850d12f7e1cf82b0b2d3dca4e0fc8b95cb12` |
| [038500 正文](offline-actor-subobject-direct-dependencies/038500-instructions.json) | [038500, 038596)，150 B / `8e8618c59d98d1a7568fcaa086e0a2b59fb8f6f058a482d7a104d5662ce4c087` |
| [273AB0 正文](offline-actor-subobject-direct-dependencies/273ab0-instructions.json) | [273AB0, 273B29)，121 B / `0fc850ae074a5f4f4ea6046d79f0a96f13c731314a34d40af9b6d16e6d58a3b7` |
| [既有 01F170 正文](offline-cache-node-inspection/01f170-instructions.json) | `d6ee428d3e56c754b9ba28c127c75f98ec382a55ccd2453d3d88e3a1e6b0c0e1` |
| [既有 0481F0 汇编](rea-binary-subobject-profile/05-Binary-subobject-init-0x1800481f0-assembly.json) | `72bc6f31bb56ea665fe88581e822cfa15f4ef6427060cd838ece42d74ca020ee` |

01F170 的旧 [提取记录](offline-cache-node-inspection/extraction.json) 和 [反汇编记录](offline-cache-node-inspection/disassembly-summary.json) 分别为 SHA256 `a80331fcc18a851b65bde9bfd01415142de2443df160c557a1847158bd073d2e`、`527c94eabef4304ed1b3ea848a14e2f4d76062f05184c5324df54b47d7c4b439`，绑定同一 Engine 原件及只读副本，确认 68 B 正文的完整覆盖。0481F0 的 structuredContent.subject 同样绑定上述 Engine SHA；本轮只读缓存 normalized_result，未重启旧查询，也未重新读取这两个 helper 的原件正文。

## 子对象与 sentinel 的正常写序

BDBE0 对传入的 856 B 子对象存储执行已证写入。0x1800BDC86 将 038500 放入 R9，0x1800BDC94 调用已有 2DCC3C iterator，元素起点为子对象+0x110、stride=0x68、count=1。这里只接续前轮显式文件初始检查目标的条件模型；不把加载后的 guard 当作已知空操作，也不覆盖 iterator 的异常清理。

038500 在若干元素字段写入后，于 0x180038564 调用 01F170，EDX/R8D 均为零。**零链接参数不等于零分配：01F170 在 0x18001F17A 请求 0x38（56）B，并在 0x18001F185 调用 2DC398。** 在正常非零分配返回前提下，0x18001F18F/192 将两个待写链接都设为返回指针，0x18001F19A/1A6 分别写 sentinel+0 和 +8，使其自链接。其余 40 B 没有因此得到初始化证明。这里的 56 B 是分配尺寸，01F170 已存正文长度为 68 B。

随后 038500 把 sentinel 返回值发布到元素+0x38，并继续既有标量写入。正常分配 helper 合同复用已有 [2DC398 正文](offline-engine-root-static-helper-direct-targets/2dc398-instructions.json) 和 [精确导入绑定](offline-engine-root-helper-imports/metadata.json)；没有恢复 malloc/new-handler 重试或失败/EH。

BDBE0 在 0x1800BDCE2 以 count=0 调用 0481F0，目标为子对象+0x270。缓存汇编的 048204、04820A、048211 依次写目标+0/+8/+0x10 三个 QWORD 为零；048215/218 检测零计数后跳到返回。该特定分支不分配、不调用填充 helper，也不读取 R8 所指 payload。重复零写仍计为实际 store；后续子对象+0x288 的清零属于 caller，不能归到 helper 写集。

## 273AB0：只接有符号比较成立的热分支

0x180273AB4 读取 GS:[0x58]，273ABD 使用 420DD4 的索引，273AC8 取得 TLS block，273ACC 读取 block+4 的线程 epoch。273ACF 比较 guard 423F88 与 EAX，273AD5 使用 **JG（有符号大于）**。因此热分支条件是 signed int32 guard <= signed int32 thread epoch；成立时 273AD7 直接返回 preferred identity 0x18041D490，273AE2 返回。

该返回地址不证明其 48 B 内容已初始化。BDBE0 在 0x1800BDE22 和 0x1800BDEB9 各调用一次，随后各执行三次 16 B 读取/复制，写入子对象+0xE0 与 +0x1E4。模型为两次调用分别接收 TLS 有效性、guard/epoch 和独立 48 B 观察，不合并成一次采样，不假定两次字节相同，也不凭地址将内容补为单位矩阵或全零。每次分三块处理；后续块未知时，保留此前完成的块和更早构造写入。

guard > epoch 时进入 273AE3，并在 273AEA 调用 **2DCAE0**；随后可能检查 guard==-1、写部分全局数据，再在 273B22 调用 **2DCA80**。两条慢路径 helper 的正常同步/初始化合同仍未闭合，当前模型在其入口停为 Unsupported，不以已知最终地址替代慢路径执行，也没有给 -1 增设无证据快捷分支。

## 常量、返回身份与 Actor 尾部

本轮新文件数据为：34BB44 的 4 B `0000803f`，以及 34C1E0 的 16 B `0000803f00000000000000000000803f`。BDBE0 的 34C3B0 读取复用 [前轮固定常量记录](offline-actor-continuation-constant/metadata.json)，不重复原件读取。它们均为磁盘初值；选择这些位模式的 API 明示 ArchivedFileImage，不证明加载后相同地址的运行时内容。

BDBE0 正文保留入参身份于 RDI，0x1800BDF4D 执行 MOV RAX,RDI，随后正常返回。只有按本覆盖域走到此处，模型才报告该子对象身份已返回。Actor caller 继而在 0x180025CCF 将它写到 this+0x1E0，再写两个 flag、子对象+0x118 的 Actor back-reference、+0x110 的 DWORD 等尾部字段。此前未知返回时禁止发布的边界仍适用于任何提前停止结果。

0x180025D3D 的 421428 槽在已固定 PE 中完全位于 .data 的虚拟尾部。本轮没有对应的 8 B 文件读取，只保存 **file-initial zero-fill** 说明；没有读取加载后的指针或解引用目标。其运行时初始化仍缺。正常尾部需显式观察一个有效对象身份，并按 caller 顺序取得 +0x678、+0x668 两个 QWORD；相等结果决定 Actor+0x4E1，随后完成余下固定零写，最终返回 Actor 身份。不能把虚拟零填充当作可用运行时对象。

## 自有模型与验收边界

新增 [ActorConstructorCompletion.cs](../../src/task_items/ActorConstructorCompletion.cs) 复用既有 continuation 状态，使用可空字节保存未知区域；子对象、sentinel 和数组/Actor 的独占不重叠有效性是输入前提，token 只是自有身份，不会解引用原生地址。快照输入防止调用方数组修改改变观察，未观测 padding 不统一清零。

`ActorMatrixWarmObservation` 分开表达 TLS 路径是否有效、两个 signed epoch 与 48 B 来源；`ActorGlobalObservation` 分开表达对象身份和两个按序字段。`ActorCompletionObservations.WithArchivedConstants` 明示选择文件常量。`ActorConstructorCompletion.Continue` 要求此前停在 025CC9 且显式文件 guard 前提成立；缺 sentinel、常量、TLS/块字节或全局字段时，在相应依赖处保留已发生前缀。

完整条件满足时结果是 `ConditionalNormalReturn`、`NormalReturnReached=true`，但 `IsCompleteNativeConstructor` 始终为 false。BDBE0 返回后才可能设置 `ActorObjectPublished`；若此后缺全局观察，已发布子对象及 back-reference 不回滚。自有覆盖守卫不等于原生错误返回，托管 OOM、原生 ABI、分配失败/EH、TLS 慢初始化、421428 的真实 writer 及实际 loaded guard 都未被模拟。

模型、fixture 和固定构建脚本均经静态审阅，[执行前预检](actor-constructor-completion-validation/preflight-reviewed.json) 冻结六个构建输入并核对十四个既有源文件。[实际执行记录](actor-constructor-completion-validation/run.json) 保存正式 `require_escalated` 动作及完整输出：chunk `629944`，exit 0，一次构建、一次新测试调用。执行前记录和证据审阅中的 pending 状态保留为历史，不代表当前验收状态。

| 新场景组 | 断言数 |
| --- | ---: |
| CompleteArchivedIntegration | 8 |
| PriorAndIdentityGuards | 7 |
| SentinelBoundary | 6 |
| ConstantDependencyStops | 6 |
| RawConstantsAndInputCopies | 6 |
| MatrixEpochBoundaries | 7 |
| FirstMatrixChunks | 6 |
| SecondMatrixChunks | 6 |
| GlobalEquality | 4 |
| GlobalObservationStops | 7 |
| WidthAndStorageIsolation | 6 |
| **合计** | **69** |

新 fixture 覆盖两次矩阵读取的六个部分块停点、有符号 epoch 极值与慢路径拒绝、sentinel 自链接及未写 padding、全 64 位全局比较和分阶段未知字段、原始浮点位模式与输入副本隔离。完整条件路径自然调用未改动的 Prefix、Array、Continuation 模型；**旧测试套件运行数为 0**，本轮没有重跑历史 48/64/47 断言测试集，也没有原生运行或动态差分。统计的 store 次数保留重复写入：子对象构造 108、sentinel 2、Actor 尾部 16、返回对象 back-reference 2；包含前轮后的 Actor 总写次数为 158，不把它们当作独立字段数或恢复程度。

[执行后清单](actor-constructor-completion-validation/postbuild-inventory.json) 确认合并去重后的 17 个输入/既有源码均未变化；HEAD 仍为 `6dfdae419d1c92f7738eefe241937057d83134df`，原有十二份 tracked diff 未变。

| 源码或自有产物 | SHA256 |
| --- | --- |
| ActorConstructorCompletion.cs | `79c9b29f989d1fb4c0c018d32667ab2012b2f1dec2cf9ffefaae17b68bdb3457` |
| ActorConstructorCompletionTests.cs | `075909d98d502d19f20f2bd738d1d6d7a85f34f03b2a0c28cfec3b9e4abfb5b2` |
| build_actor_constructor_completion.ps1 | `21ffd120450891394710b520eb1f9ac741230c951930a63094275368acc46fea` |
| GrimDynasty.ActorConstructorCompletion.dll，29696 B | `590651bc064dcf6f369390c601447d9e9500b19db9112e1a9ddd3a66efeec82b` |
| ActorConstructorCompletionTests.exe，33792 B | `a898e9d8d3920863d2ee8b45f9a209959c284284ca19017d8083b0325a8793d4` |

两个自有二进制位于 `.tooling/actor-constructor-completion-build-20261021/`。构建只使用现有 .NET Framework 编译器、四份自有模型与一个新 fixture，不加载 Engine/Game DLL，不运行旧被拒脚本，不修改策略、注册表或 ACL。

## 本轮实际读取和执行范围

| 固定执行 | 新代码 | 新 .pdata | 新文件数据 | 虚拟零填充说明 | dumpbin |
| --- | ---: | ---: | ---: | ---: | ---: |
| 第一批，chunk d2b6e1 | 901 B / 169 指令 | 156 B | 4 B | 8 B，未文件读取 | 1 |
| 第二批，chunk 3a46e0 | 271 B / 64 指令 | 324 B | 16 B | 0 | 2 |
| 合计 | **1172 B / 233 指令** | **480 B** | **20 B** | **8 B，非新增读取** | **3** |

两批各有 before/after 两次同句柄完整哈希，合计 **4 次 Engine 全文件哈希 pass**，均使用 64 KiB 缓冲区并匹配固定 SHA；完整哈希读取量不计入上表语义字节。工具哈希及文件状态均保持，固定正文逐字节覆盖验证成功。第一批所得直接引用决定第二批的两个精确目标；没有自动递归追踪，未继续读取 2DCAE0/2DCA80 或解引用全局目标。dumpbin 内部读取 PE 元数据的字节数未计量；表中为 Node reader 的明确语义读取。两次 Node 控制进程的 maxRSS 分别为 43112、43888 KiB，不代表并发进程总峰值。本轮没有启动 Rea/Java 或运行原件。

两个 reader 源码保留编写时的 PREPARED ONLY 注释，不据该注释反推它们尚未执行；本轮实际执行状态以两份 inspection.json 和上述工具 chunk 为准。取证后的文档整理与封存只使用工程归档、自有源码和自有构建产物，没有追加原件读取。既有源文件、构建脚本与历史报告保持不变，进度页仅追加本段结果。

## 距离完整正常构造的剩余证据

子对象 038500→01F170、0481F0 的固定分支及 BDBE0 正常返回身份已闭合；273AB0 只覆盖有明确输入观察的热分支。项目两个限定归档目录的文件名索引中尚未找到 2DCAE0/2DCA80 独立正文，慢路径未实现。这不等于断言其他名称的归档必然不含相关线索。

实际运行还需要确定：加载后的检查目标；TLS 路径/epoch 的真实有效性；41D490 的三个读取块在两次调用时的内容；421428 的 writer、初始化时序、对象生命周期和 +678/+668 状态来源。现有归档只证明多个 Engine 路径读取同一槽，尚未找到其写入来源。分配重试、异常清理和正式样本兼容也仍未验。因而本轮完成的是明确条件域内的正常路径模型，不能将整个 native Actor 构造称为已恢复。

下一最小证据优先是 273AB0 已直接引用的 2DCAE0/2DCA80 合同及 41D490 初始化关系；421428 仍应沿已有引用寻找 writer，不能从磁盘零填充推断运行时状态。下一段是否读取原件或执行新实现，继续受原有范围和正式审批限制。

恢复材料封存在 [actor-constructor-completion-complete/manifest.json](../../.tooling/checkpoints/actor-constructor-completion-complete/manifest.json)：包含前一检查点选定材料、本段源码、两批有界证据、实际验收日志和自有产物。清单记录逐项源/副本哈希与字节数；不包含原版完整 DLL、存档或云数据。未 commit/push。
