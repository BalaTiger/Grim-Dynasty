# Token仓库闭环、受限任务状态写入与奖励查询

2026-10-18。自有 C# 源码位于 `src/token_actions/`，使用既有 Windows Framework csc 重编译为 `.tooling/token-actions-build/GrimDynasty.TokenActions.dll`。只引用 mscorlib 与自有 TaskLogic/Conditions；不引用或执行原版 DLL、Steam，不访问真实存档、进程、网络。该模块对显式模拟快照写入，不意味着读写游戏状态。

## 新增可用范围

1. Repository `SetPlayerToken`：查找精确 uint32 玩家键；缺失玩家行时无操作；存在行时按完整字符串精确查找，缺少该token才追加。**不自动创建玩家行**。
2. Repository `RemovePlayerToken`：缺失行/缺失token无操作；删除有序列表中的**首个**精确匹配，保持其余顺序。批量赋值可保留重复token，因此首次删除语义不可替换为RemoveAll。
3. Repository `SetPlayerTokens`：创建/替换指定玩家行，保留源顺序与重复项，复制列表内容；空输入清空但保留行。服务槽返回非空且客户端玩家ID等于目标ID时更新服务器玩家键，否则保留。所有必要上下文先验证；未知上下文不产生部分写入。
4. Task.Start/Complete/Reset **无回调子域**：详见下表。写入后能被已有任务条件模块读取，状态按难度隔离。
5. ActionCollection.HasRewardActions：纯整数分支分类。原生分类集合为 `{4,5,6,7,8,15,18,20,25,26}`；GiveToken/RemoveToken不属于此集合。空列表false，按顺序短路。该函数只识别类型字段，不执行动作、不推断未识别整数的动作行为。
6. Task.HasRewardActions（事件向量重载）：先Evaluate事件，再看动作指针/奖励分类；找到奖励即短路。空向量false。未知条件不会因为动作列表为空而被跳过。目标向量重载未在本批实现。

字符串域是可由有效UTF-8解码的名称，区分大小写，无归一化。仓库写入函数接受完整TriggerToken名称，**不截断NUL**；已有条件包装查询会先经strlen截断，两者不可混同。原生内存分配/析构的资源生命周期不在离线值语义域；假定分配成功和有效、调用期间稳定的快照。字典Complete标记、已知null与未知缺失继续区别处理。

| 任务方法 | 已恢复子域 | Unsupported分支 |
|---|---|---|
| Start | runtimeBlocked非零时直接状态2，无需难度；未阻塞时当前难度目标列表明确为空；旧状态1还要求QuestFlags bit4未置位且onAccept事件列表明确为空 | 教程解锁、onAccept执行、目标生命周期回调 |
| Complete | 旧状态不等于2，难度状态银行完整；默认写3，旧状态1且QuestFlags bit4时恢复1；当前难度同步 | 旧状态2时，即使executeRewards=false也有生命周期/出站通知，整个分支返回Unsupported，不先部分写3 |
| Reset | 旧状态不等于3无操作；旧状态3且当前难度目标列表明确为空，状态和当前难度状态写1 | 非空目标计数器与生命周期回调 |

这些是原生方法的受限输入域，不是新制定的任务规则。不会把任务文件isBlocker当作运行时blocked；不会把状态3直接当作已发奖励；未重写奖励标志、结算或任务调度。

## 实际静态证据

Rea 6.5.0 / Ghidra实际调用保存于 `rea/`：48个只读静态查询，`progress.json` consumed48/pending0，close_binary成功。采用之前已安装、批准的同一MCP启动方式，无新安装、持久配置或权限变更。固定本机Game.dll SHA256：`cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`。

| 功能 | 当前x64 Game.dll地址 |
|---|---|
| Repository.SetPlayerToken / RemovePlayerToken / SetPlayerTokens | 0x180424aa0 / 0x180424b50 / 0x180424a20 |
| Token向量append / erase / assign / clear | 0x1803a5750 / 0x1803e8830 / 0x18029b670 / 0x18029bbb0 |
| Token map索引/插入入口 | 0x1803b46a0 |
| Task.Start / Complete / Reset | 0x180426750 / 0x180426980 / 0x180426ca0 |
| ActionCollection.HasRewardActions | 0x18046e490 |
| Task.HasRewardActions事件重载 | 0x180427700 |
| GiveToken / RemoveToken Execute | 0x18046d080 / 0x18046e070 |
| Player.BestowToken / RemoveToken | 0x1803d53f0 / 0x1803d56a0 |

动作工厂0x1804686e0的case9/10确定具体vtable，实际Execute位于slot1；探测到的slot3/4/5共享空函数不能冒充Execute。字符串相等依赖复用前批已验证的 Engine.dll TriggerToken.operator== 0x1801f51f0→memcmp+长度helper0x1800227e0。

完整GiveToken/RemoveToken动作当前**未实现、未执行**。GiveToken入口调用Player.BestowToken，该方法先调用PlayStats.TokenGranted，新增token后还调用出站虚表槽+0x248、发送事件0x2e并写标志；重复token也会先经过统计。RemoveToken即使本地未找到匹配仍调用出站槽+0x250。当前API `ExecuteFullTokenAction` 对这两个动作明确Unsupported且不写快照；不能用Repository闭环替代这些副作用。完整活跃任务Complete同样明确Unsupported。限制来自尚未还原的调用语义，非权限失败，不需要用户重启或改设置。

## 编译与验收

运行 `src/token_actions/build_token_actions.ps1`，现有csc产生自有DLL和CLI：

- TokenMutationTests：64条固定断言。涵盖缺失行、重复项、首项删除、顺序、拷贝/自赋值、NUL、Unicode、uint32高位键、服务器键更新、Unsupported无部分写入，以及状态读写闭环。
- RewardClassifierTests：39条固定断言。逐项固定类型表、空向量、未知输入、短路与负整数域。
- EventRewardProbeTests：18条固定断言。条件先于动作检查、Wait不跳过、奖励短路、指针已知/未知与输入顺序。

合计121条新增固定断言通过，预期值来自实际静态分支，不以另一份同构算法为oracle。三类CatalogProbe以只读Quests.arc为参数：

- 全247份定义、全部3346动作（包括目标节点9动作）。439个GiveToken和227个RemoveToken参数，各自进入显式模拟玩家42的仓库set→read true→remove→read false，共666组闭环。**这是实际参数的仓库集成，不是原生动作执行证明**。
- 奖励分类遍历3901节点、3346动作，2091奖励类型动作，2091节点含奖励；不执行奖励、不判定资源中的事件实际会触发。
- 事件奖励查询接入每难度918任务事件向量：难度0 true272/false646/Unsupported0；难度1、2各true271/false646/Unsupported1。上下文明确固定等级10、普通角色、模式0、无token、完整空任务索引、服务槽空；一个未覆盖v1条件保持Unsupported。

详细输出见 `acceptance.json` 和 `*-catalog-results.json`，含本轮源码/产物哈希、依赖与输入哈希。编译脚本变更先逐字节确认仍是本轮内容，保留before-reward/before-event-reward备份，再原子写入；没有覆盖用户改动。无commit、推送、部署、软件安装、系统或原游戏更改。独立原版动态验收仍未进行。

## 后续静态分支

优先沿出站接口+0x248/+0x250、PlayStats.TokenGranted、事件0x2e的订阅者继续追踪完整token动作；任务活跃完成还需目标生命周期+0x40和出站完成槽+0x238。可继续选择有完整值语义的子模块；任何未确认依赖保持Unsupported，不能因模拟断言通过而宣布原版动态等价。
