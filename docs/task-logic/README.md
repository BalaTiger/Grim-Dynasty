# 离线任务判定模块（静态行为还原子集）

2026-10-18。本轮新增 `src/task_logic/` 和独立编译产物 `.tooling/task-logic-build/GrimDynasty.TaskLogic.dll`。使用现有 Windows .NET Framework C# 编译器；不安装工具。模块引用自有 `GrimDynasty.Conditions.dll`、mscorlib、System.Core，不引用原版 Game.dll、Engine.dll 或 Steam 接口。输入是显式声明的模拟世界快照，输出 Known/Unsupported，不访问进程、存档、网络或系统游戏状态。

## 已实现范围与依据

实际 Rea 6.5.0 / Ghidra 静态反编译结果保存在本目录 `rea/`、`rea-engine/`。地址属于当前本机 x64 模块，不能视作跨版本地址。输入模块 SHA256 见 acceptance.json。

| 条件/调用者 | Game.dll 地址 | 还原边界 |
|---|---|---|
| IsLevel，ID3 | 0x180473020 | 主玩家等级 uint32，与存储阈值比较；六种选择器，实际值在左。与 IsDifficulty 的有符号/存储值在左不同 |
| HasToken，ID7 | 0x180472c30 | 使用主玩家，忽略传入 Entity；期望布尔反转 |
| Player.HasToken | 0x1803d57f0 | 模式值等于1和其他模式使用不同 token 库，按当前难度选择；不擅自给模式1命名 |
| ServerHasToken，ID18 | 0x1804747e0 | Repository.ServerHasToken 0x1804247e0，使用指定服务器玩家 uint32 键 |
| AnyoneHasToken，ID20 | 0x18046f760 | Repository.AnyoneHasToken 0x1804248f0，按 uint32 键顺序遍历所有玩家 token |
| OnQuest，ID0 | 0x180473e20 | 任一合格任务状态2；服务模块虚表+0x18返回非空时绕过运行时阻塞标志 |
| QuestTaskComplete，ID1 | 0x1804741e0 | 精确任务 UID，状态3；查无任务/任务集时直接 false |
| OnQuestTask，ID14 | 0x180473fa0 | 状态2与相同服务槽/阻塞规则；查无任务时将 false 与 expected 比较，和 ID1 不同 |
| QuestBlocked，ID19 | 0x1804740f0 | 任一任务状态不等于3且运行时阻塞字节非零 |
| HasQuestObjective，ID11 | 0x1804729d0 | 首先要求状态2与调用者可见性；v0默认目标UID0，v1支持指定目标UID |
| Task.AreObjectivesMet | 0x180426e10 | 当前难度的所有目标；跳过 WaitForCompletion；空目标集 true |
| Task.GetObjectiveByUid | 0x180426fc0 | 当前难度目标列表按顺序取首个匹配 UID |
| Quest2Event.Evaluate | 0x180421d10 | 已知空条件指针 true；非空 AND/OR，不跳过 WaitForCompletion |

任务索引字符串比较 helper 0x180018580 为同长度、同字节精确匹配。任务 UID 查找 0x1804214b0；状态 getter 0x1804266f0、运行时阻塞 getter 0x1804266d0。运行时 blocked 在 +0xc1，不能替换成 QST 文件的 isBlocker 定义。服务提供者对象的领域名称尚未确认，API 明确保留中性字段 `ServerServiceSlot18NonNull`，不把它猜成“联机模式”。

Engine.dll 的 TriggerToken.operator== 0x1801f51f0 调用 0x1800227e0：比较 std::string 全长与 memcmp，区分大小写，无路径/Unicode归一化。条件包装函数通过 strlen 复制查询，所以查询 NUL 后缀被截断；库中 token 仍保留完整长度。API 仅覆盖可由有效 UTF-8 解码的字符串，非法孤立代理字符 Unsupported。TriggerToken 构造器地址0x1801f5150也已读取；未推断无关字段用途。

## 快照契约

`World` 是调用者声明的离线模拟状态，绝不表示从真实玩家或存档采集。QuestState.Tasks、存在的 token 列表和难度目标列表表示完整声明的有序快照。任务索引与 Repository token 索引额外需要 Complete 标记，否则缺失不能冒充“不存在”。本地 token 库需显式给出对应模式/难度的列表；空列表与未提供列表不同。主玩家需明确解析成功；不模拟原生无效指针读取为 false。

已知空指针与未提供快照不同。EventGate 仅在 pointerKnown=true 时可把 null 视为 true；旧通用 ConditionDefinitionAdapter 对缺失容器仍保留 Unsupported，避免把事件调用者语义扩散到其他调用者。目标条件空指针语义仅在已还原的目标调用路径使用。

所有快照在一次调用期间须保持不变。存在重复任务 UID 的不一致列表/索引状态不在支持域内；指定目标仍保留线性首次匹配。递归超过64层返回 Unsupported，用于拒绝循环/超深模拟快照，未声称这是原生限制。未知类型、未知版本、无服务槽上下文、未知难度向量、缺少状态均 Unsupported。v0已验证类型3/7/18/20/0/1/14/19，ID11支持v0/v1；ID4/5/17沿用上一模块。

本轮没有恢复任务状态写入、接取/完成动作、奖励发放、token 增删、任务调度、Lua执行或玩家保存格式。此模块是纯判定层，不是完整任务运行器或引擎替代品。东方题材地图/内容重写与28星宿设计仍遵循既有规划；本轮不变更原游戏数据或数值。

## 可重复验证

在项目运行 `src/task_logic/build_tasks.ps1`，产生自有 DLL 和两个 CLI。TaskLogicTests.exe 有71条固定预期断言，覆盖无任务反转差异、状态2/3、非零阻塞字节、服务槽绕过、目标跳过Wait的差异、空/未知指针、短路、uint32高位、token模式/难度隔离、服务器/任意玩家范围、NUL、Unicode与递归拒绝。

`TaskCatalogProbe.exe "D:\Steam\steamapps\common\Grim Dawn\resources\Quests.arc"` 只读官方资源，将247份定义的参数接入自有模块。模拟场景明确固定：等级10、普通角色、游戏模式0、无token、完整空任务索引、服务槽返回空。定义数据不是运行时状态；没有按资源路径猜测 Repository 任务键映射。

结果见 catalog-results.json：3340事件、561目标、2435条件；1525空事件条件全部由已证实的事件调用者规则恢复。三个难度场景各3901组，Known3657 / Unsupported244；剩余244组遇到未覆盖的v1条件（HasKilled/HasItem），不能据此声明整个任务系统已还原。前一轮通用目录判定为 Known2015 / Unsupported1886，但调用者/skip规则现更精确，应结合此范围说明比较。

独立动态验收仍暂停。当前证据是“真实静态函数 → 自有可编译实现 → 固定断言 → 官方资源参数接入”；原游戏/DLL执行对照尚未发生。不调用Steam、不接触云/本地真实存档，不修改游戏安装或设置，无安装、commit、推送、发布。
