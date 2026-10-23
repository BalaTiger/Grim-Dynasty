# Actor 的 Engine 上下文来源与自有接入

本轮闭合了 Actor 尾部 `421428 → +678/+668` 的局部字段初始化来源，并将可变自有上下文接到 Actor 实际读取点。新增 **52 条断言／9 组场景**通过；受影响的 Completion **69／11**、Matrix **64／10**各复跑一次通过。仍未恢复原生 `gEngine` 发布者或完整 Engine、Item/QuestItem 构造。

## 原生证据与范围

先复用 `offline-actor-constructor-body/0259a0-instructions.json` 和 round-4 已归档 Engine 构造、Initialize 汇编文本。旧 `initialization-locations.json` 明确绑定 Engine SHA256；Initialize 的三条 outgoing READ 引用将 `421428` 标成 `?gEngine@GAME@@3PEAVEngine@1@EA`。这是旧符号证据，本轮没有重新查询导出表。

Actor `25D3D` 先读取全局对象，`25D44` 读其 `+678`，`25D4B` 将其与 `+668` 的完整 QWORD 比较，`25D55` 将相等结果写入 Actor `+4E1`。这不是线程 ID 比较，也不能据 Server/Stub 的不同名称推断身份必不相等。

| 新核验的局部事实 | 来源 |
| --- | --- |
| 保存构造 this、清零 EBP，并清零 `668/670/678/680` 四个字段 | `72951`、`7295E`、`72C26..72C42`；中间流程仍依赖已存完整构造汇编及正常返回条件 |
| Initialize 的 RSI 是 this | `74405` |
| Client：分配 `300`，调用 `15E060`，返回值写 `670` | `74D77..74D98`；只核验证据，不纳入本次三字段模型 |
| Server：分配 `340`，调用 `15A8B0`，返回 RAX 写 `678` | `74D98..74DB9`；旧引用明确命名 ServerConnectionManager 构造 |
| Stub：分配 `2A0`，调用 `15A7D0`，返回 RAX 写 `680` | `74DB9..74DDA`；旧引用明确命名 StubConnectionManager 构造 |
| 三次辅助构造在选择当前实例之前执行 | 分配各 `20`；调用 `172F30` 两次、`172ED0` 一次，原生分别写 `690/698/6A0` |
| 当前实例取最新的 Stub 字段 | `74E3D` **重新读取** `680`，`74E44` 写 `668`；不能沿用之前的构造返回值 |

[固定 reader](../../tools/static_analysis/read_actor_engine_context.mjs) 经全文审查后只执行一次，tool chunk `3029e0`，exit 0。[只读取证记录](offline-actor-engine-context/inspection.json) 保存全部原始切片、dumpbin 指令和缓存对应。总量为 **5 个窗口、248 B、51 条指令、5 次 dumpbin**，新增 PE 元数据、数据和导出读取均为 0。每次 dumpbin 只接收生成的小 PE，合计 16,896 B，从未接收原 DLL 路径或执行这些 PE。没有跟进新 callee。

原件为 `D:\Steam\steamapps\common\Grim Dawn\x64\Engine.dll`，4,485,632 B。同一只读句柄前后各一次完整流式 SHA256，缓冲为 64 KiB，结果均为：

`4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`

文件 stat、工具前后哈希、句柄关闭、逐字节指令覆盖均通过。全文件指纹 I/O 单列于语义读取量之外。Game DLL 本轮未读取，不把其旧哈希称作本轮新测。

## 自有模型与实际读取时机

[ActorEngineContext.cs](../../src/task_items/ActorEngineContext.cs) 只投影 `668/678/680`。`CreateOwnConstructedFields` 在独立的有效非零 own 对象身份上建立已证零字段；它既不分配原生对象，也不证明完整 Engine 构造成功。`ActorEngineSlot.Publish` 明确是自有绑定操作，不是恢复的 `421428` writer。

初始化 invocation 只表示到达 `74D98` 后的五对分配／构造调用，成功终点为 `74E4B`。调用接口观察直接参数和返回身份，不假定构造返回等于分配地址。Server、Stub 按原顺序写入；三个辅助构造仍须正常返回，之后才重读当前 `680` 并写 `668`。它们的输出字段 `690/698/6A0` 不在模型中，因此不能用本接口解释依赖这些字段的回调或完整 Engine 行为。

缺少正常返回报告就停在对应调用；分配身份未知时停在后续构造调用前。构造已正常返回但身份未知时，目标字段写为 unknown 后停在该 store 之后，避免保留陈旧已知值。辅助构造数值返回可未知，因为三字段投影不消费它；晚读的 `680` 未知则最终 `668` 也未知。覆盖停点不是推测出的原生失败或 EH 路径。own 回调异常直接传播，已经发生的写入与调用前缀保留；同一 invocation 禁止重放。

[ActorConstructorCompletion.cs](../../src/task_items/ActorConstructorCompletion.cs) 新增 `IActorGlobalStateSource` 和 `ContinueWithStateSources`。源仅在到达 `25D3D` 时调用一次，早期矩阵回调造成的槽换绑和字段变化可见；旧静态 observation API 保留原语义。源采样一次上下文引用，按 Actor 顺序读取 Server 和当前实例，然后仍走原有 unknown/无效身份停点。

这个采样需要调用者对对象生命周期、字段写入和整个读取序列提供自有排他访问；它不提供原生线程的原子快照保证。矩阵接口也继续要求同一有效同步域及独立稳定的逻辑线程 epoch。自有宿主可以用平台锁／条件变量满足这些条件，但本轮没有编写或实测多线程适配器，没有将零句柄、某 API 曾返回或单线程测试通过当作同步有效。`guard=-1` 等待分支仍保留停点，未继续深挖通用 CRT。

## 本轮真实验证

[fixture](../../src/task_items/ActorEngineContextTests.cs) 使用自有数据和回调，覆盖有效对象中的零相等、64 位身份比较、构造返回与分配身份分离、晚读改值、十个调用位置的失败前缀、unknown 写入、已知零输入、own 异常与重放限制、跨 Actor 调用的 live 槽、尾部采样与早停。

| 新场景组 | 断言 |
| --- | ---: |
| OwnCreationAndIdentity | 6 |
| CallArgumentsAndPublication | 7 |
| LateStubRead | 4 |
| FailurePrefixes | 11 |
| UnknownValuesAndZeroInputs | 6 |
| OwnExceptionsAndReplay | 4 |
| SlotSampleAndLateBinding | 4 |
| UnknownTailStages | 7 |
| SourceTimingAndExceptions | 3 |
| 合计 | **52** |

[固定构建脚本](../../src/task_items/build_actor_engine_context.ps1) 经 root 全文审核、10 个输入哈希核对以及单独正式 `require_escalated` 审批执行一次，chunks `f3a5e5`、`be6afe`，exit 0：4 次 csc、3 次自有 EXE。Completion 69 条／11 组和 Matrix 64 条／10 组是受共用完成路径改动影响的回归，各一次；其余历史套件执行数为 0。本次编译输出无警告。独立静审也未发现阻断，其结论在 root 构建完成后收到，四个新改输入哈希与构建前一致。

旧同步 fixture 第 139 行的 CS0675 已只读核对：循环下标明确为 0–63，`(ulong)i` 与 `(ulong)(uint)i` 在整个可达范围相同，cookie 的低 6 位为零。这一警告不影响该测试取值；未改其源码，未重新运行旧同步套件。

[运行记录](actor-engine-context-validation/run.json)、[预检](actor-engine-context-validation/preflight-reviewed.json)、[静审](actor-engine-context-validation/model-review.json)、[证据核对](actor-engine-context-validation/evidence-review.json) 和 [执行后清单](actor-engine-context-validation/postbuild-inventory.json) 保存实际范围与哈希。输出目录 `.tooling/actor-engine-context-build-20261021/` 包含 43,008 B 自有 DLL 和三个 fixture EXE。10 个构建输入及基线中的 200 个选定文件在构建期间未变；HEAD 仍为 `6dfdae419d1c92f7738eefe241937057d83134df`，原有 12 项 tracked 改动仍为 +60/-46。

## 可用里程碑与仍缺依赖

已可在自有排他上下文下，由明确的局部 Engine 字段初始化和实时槽采样支持 Actor 的**条件性正常返回**，调用方不必提前手填两个相等比较字段的静态快照。连接管理器构造本体仍由明确观察接口供给；不同名字不保证不同返回身份。

原生 `gEngine` writer 仍未知：两个已存 Engine 正文均未写该槽；旧宿主资料只有导入、未定位具体创建/发布 callsite。已有 Game `InitializeObjects → GameEngine` 链发布的是另一个 `gGameEngine`，不能替代。原生发布时序、后续模式切换写入者、Server/Stub/辅助构造正文、加载后 guard、原生分配失败/异常清理及实际线程同步都未闭合。没有把零填充对象当有效对象，完整 Item/QuestItem、原生运行时全图和正式样本兼容仍未验。

下一可用里程碑应回到 Item `323690` 中 Actor 正常返回后的两个 `0410E0` 子对象调用，先核对其直接合同并衔接已完成的 Item `+858` 段，而不是继续通用 CRT。该后续工作还需要独立有界证据和必要构建审批；本轮没有擅自展开。

本轮检查点为 [actor-engine-context-complete/manifest.json](../../.tooling/checkpoints/actor-engine-context-complete/manifest.json)，保留上一清单、选定源码、证据、fixture 和本次产物。已有修改保留，仅增补上下文模型、Actor 接口及本轮记录。没有运行原游戏/Rea/Java、触及存档或 Steam 云、安装工具、改安全设置、commit/push。没有剩余被拒、待批准或结果不明的执行动作；阻点是上述证据与实现缺口。
