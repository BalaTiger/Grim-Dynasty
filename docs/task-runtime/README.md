# 可独立运行的任务状态链（2026-10-18）

本批把原来仅能判定条件/空目标写入的代码，推进为可独立执行的**接取→token入站→目标由假变真→显式无奖励完成→完成查询→重置**链。实现为自有 `GrimDynasty.TaskRuntime.dll`，源码在 `src/task_runtime/`。现有 Windows csc 可重编译；不引用原版DLL/Steam。读取官方资源的Catalog CLI与完全不读取原版资源的原创演示分别提供。

## 直接运行

运行 `src/task_runtime/build_runtime.ps1`，或直接运行 `.tooling/task-runtime-build/TaskRuntimeDemo.exe`。

原创“辽东军报”命令行场景使用自有任务键 `ming/liaodong_report` 和token `MILITARY_REPORT_VERIFIED`，输出七步JSON轨迹：

1. 接取，任务状态1→2。
2. 目标查询false。
3. 应用已解码的token入站命令，更新指定玩家仓库。
4. 目标查询true。
5. 显式执行无奖励完成，状态2→3，dont_propagate阻止网络发送。
6. QuestTaskComplete查询true。
7. 重置，状态3→1，目标计数状态归零。

这是没有图形、地图或存档的**离线逻辑演示**，不是已可进入游戏游玩的mod。任务初始状态、仓库玩家行、提供者身份和所有上下文均显式声明。没有调用Task.Update自动完成，没有发放奖励、消息或网络包；未以此声称原游戏动态行为已验证。

## 本批恢复了什么

- 12种条件的开始/完成/重置虚表槽（+0x38/+0x40/+0x48）都指向0x180015880；实际Rea反汇编为单条RET。因此带这些条件的非空目标可以安全走生命周期，原先的“仅空目标”限制可放宽。支持类ID为0/1/3/4/5/7/11/14/17/18/19/20，v0及ID11 v1；未知类/版本仍Unsupported。
- Task.Start在无onAccept事件且不需要教程解锁的域内写状态2，再执行已证实为空的目标钩子。运行时blocked非零的原生分支仍直接写2，跳过目标钩子。isBlocker定义与运行时blocked继续分开。
- 活跃状态2的Task.Complete(false)写状态3、当前难度状态3、+0xa0对应难度字节1，并执行已证实为空的目标完成钩子。**只在已明确指定GameEngineOutboundInterface且出站查找不会进入网络发送时支持**。当前出站方法先写待发送任务键/UID，再按Repository键查任务；其+0xc2非零时跳过网络。Task.Read已证明QST版本9的第二个末尾布尔字段dont_propagate写入+0xc2。缺失的已知完整索引也会跳过发送，未知索引/字段不能冒充缺失。
- Task.Reset可处理含已知空钩子条件的非空目标，将每个目标的+0x38状态归零，再写任务和当前难度状态1。不会擅自清除完成记录字节；原生Reset没有该写入。
- token入站Give/Remove的实际方法只构造完整TriggerToken、调用既有Repository增删、析构临时值，无额外玩家统计或事件调用。因此已接入现有仓库模块，保留完整字符串、缺失玩家行无操作、去重授予和首匹配删除语义。提供的是**已经解码的入站参数处理**，没有恢复网络接收或包格式。
- PlayStats.TokenGranted已完整还原纯值语义：仅精确全长匹配`SURVIVALMODE_DEFENSEBUILT`或`SURVIVALMODE_POWERUPACTIVE`时，分别对两个32位计数器加1；保留32位回绕。其他token不需要计数器上下文且不改变统计。这解决了玩家授予链中的统计依赖，但未据此宣布整个玩家授予动作可用。

调用期间快照必须稳定；有效指针/字符串/分配成功等内存域以显式对象表示。API在未知依赖处预检并返回Unsupported，避免先部分写入后伪称完成。目标计数器需按Objective对象提供；缺少计数器时Reset拒绝写入。出站身份不能按“单机/联机”猜测，要求显式knownOutbound配置。

## 证据、实现与验收分开

Rea 6.5.0 / Ghidra实际静态查询在 `rea/`，共17个查询，close_binary已完成；一次会话复用分析结果，未反复启动每个函数。输入Game.dll与Quests.arc哈希复核不变。条件钩子指针表见condition-lifecycle-hooks.json；工厂/vtable对应关系沿用前批实际证据。

| 依据 | x64 Game.dll地址 |
|---|---|
| Task.Start / Complete / Reset（前批实际静态） | 0x180426750 / 0x180426980 / 0x180426ca0 |
| Task.Read，dont_propagate→+0xc2 | 0x1804256d0 |
| GameEngineOutboundInterface.CompleteQuestTask | 0x18030e7a0 |
| GameEngineInboundInterface.CompleteQuestTask（只评估，未恢复其完整执行） | 0x180311d20 |
| token入站Give / Remove | 0x180311de0 / 0x180311e50 |
| PlayStats.TokenGranted | 0x1803fed10 |
| 生命周期空函数 | 0x180015880 |
| Task.Update（评估依赖，未实现） | 0x180427170 |
| Task.AdjustRewards（评估依赖，未实现） | 0x1804277b0 |
| Quest2Event.Execute（评估依赖） | 0x180421e30 |

独立编译测试42条固定断言通过。原创演示七步完整通过，不读取任何原版资源。官方资源接入CLI则遍历247份任务，筛出5份实际dont_propagate=true、无onAccept/教程、条件生命周期在支持域的参数集合；三个难度共15条显式状态链完成。其中9条目标查询true，6条false；原生Complete(false)并不先检查目标，因此后6条属于显式完成调用，**不能算自动目标完成**。真实任务键命名未推断，测试使用明确的fixture键绑定。

另有666个官方token动作参数验证已解码入站Give→仓库查询true→Remove→查询false；439个GiveToken参数进入已恢复的统计函数，计数均未变。后者符合这些任务参数不匹配两个专用统计名称，并未用全零测试替代两个匹配分支、大小写、长度、溢出边界的固定断言。

## 仍未覆盖的分支与下一步

| 依赖 | 当前状态 | 后续可行路径 |
|---|---|---|
| dont_propagate=false的完成出站 | Unsupported；未调用网络发送虚表+0x68 | 可继续静态解析ConnectionManager各提供者与排队/发送契约；实际网络联机效果不在离线验收范围 |
| Task.Update自动完成 | 未实现 | 静态函数已取得；还需条件changed字节+0x14、目标OnChanged、事件0x2a及Complete(true)链。不能把本批显式完成改称自动完成 |
| 完成奖励/AdjustRewards | Unsupported | 需恢复条件难度override虚表+0x58、跨难度资格/历史字节和动作调整+0x28、动作执行与数值依赖；不能简单写状态3后忽略奖励 |
| 玩家GiveToken完整动作 | Unsupported | 统计依赖已解决；仍需玩家出站Give、事件0x2e及对应订阅处理。入站处理不是该动作的替代品 |
| EventManager回调 | 未实现完整订阅分派 | 可静态恢复通用分派与具体处理函数；实际注册列表是运行时上下文，应显式建模/验证，不能猜为空 |
| 原版动态对照 | 未执行，继续禁止 | 当前结果仅证明自有模块可编译运行、静态规则/固定断言/参数接入自洽；没有证明实际游戏会选用这些快照或派发提供者 |

尚未证明上述剩余纯逻辑必须依赖原版动态才能继续；因此不以环境限制为由阻止后续静态恢复。下一优先项是事件分派的离线契约与目标变化处理，再接回自动完成及有奖励完成。没有新的权限请求，也没有游戏、存档、Steam、系统、重启、安装、提交或发布操作。
