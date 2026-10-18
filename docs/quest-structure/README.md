# QST2结构恢复验收

> 验证对象澄清（2026-10-18）：自写模块的编译、运行和离线测试不依赖原版DLL、Steam、BIOS或Sandbox；这些隔离/授权门槛仅约束合法原版的动态行为对照，不阻塞自写实现。最终目标是独立的行为等价可编译代码，当前尚非完整游戏。详见工程 docs/VALIDATION-TRACKS.md。

本报告更新2026-10-18早先“仅本地化”的结论。当前目标为恢复行为等价、可重新编译的实现，非逐字还原原始源码；仅调用原DLL不算最终完成。

## 已运行实现

`tools/resource_reader/quest_structure.py` 为独立Python实现；`test_quest_structure.py` 新增10项边界测试。旧解析器10项测试仍保留。CLI：

```powershell
python quest_structure.py 'D:\Steam\steamapps\common\Grim Dawn\resources\Quests.arc' gdareab/mq_murdersandworse.qst
```

任务图由文件偏移12开始：questUID、flags、taskCount；每个task为UID/flags、OnAccept事件列表、Objectives目标列表、OnComplete事件列表、isBlocker与dontPropagate单字节布尔值。目标有独立UID，事件没有持久UID。条件集合为空时不存运算符；非空时存And/Or，再存类型、版本、比较方式及类型参数。动作存类型、版本及参数。Lua payload为函数名和一组带类型的参数，不执行Lua。

图中无显式文本索引。官方 `Quest.GetTextNodes` 与 `Localization.InsertInto` 的遍历顺序给出关联：group、quest，然后按task顺序依次task名称、description、OnAccept事件、Objectives目标、OnComplete事件。解析器要求文本槽数与节点数严格相等，且结构区恰好消费至本地化偏移，不允许靠字符串扫描猜图边。

## 官方证据

1. 原版Quests.arc的247成员全部经过先前ARC解压与Adler32校验；本轮247个完整结构全部读取通过，拒绝0个。最初153个可读、94个在Lua动作上明确拒绝；从官方IL补齐该布局后全部通过。
2. 原版QuestEditor.exe为托管程序，Common.dll/Utility.dll为其内嵌Costura压缩资源。通过本机受信任 `System.Reflection.Metadata.PEReader` 读取元数据和完整IL；仅解压内嵌字节到私有研究目录，没有Assembly.Load、启动编辑器或执行原程序集。选定Read、Create、GetTextNodes、InsertInto及GameReader方法的完整IL和哈希见 `managed-evidence.json`。
3. 官方ArchiveTool早先单独导出的2182字节样本与ARC读取结果SHA256一致。本轮对两种来源分别解析，完整结构结果一致。不是把官方原生运行时行为对照测试冒充完成。
4. `docs/resource-contracts/Game-function-evidence-bounded-v2.json` 对原版x64 Game.dll的Quest2::Load、Task/Objective/Event读取和两个脚本集合入口按完整PE RuntimeFunction范围分析，最大函数2103字节。堆512MiB、单CPU、无全程序自动分析；范围外被调函数单独保留为不透明接口。
5. Rea独立请求及其结果位于本目录rea子目录。首次x86 Game.dll查询因客户端180秒超时未取回结果；服务日志显示实际约239秒完成，随后close成功。后续只提高等待上限至420秒，没有更改启动方法、安装版本或权限；实际结局见 `rea-quest-load/`，不能从open_binary成功推断反编译成功。

## 样本：Murderers and Worse

`graph-acceptance.json` 包含所有偏移、UID、类型参数、文本槽和完整目录覆盖列表。样本graph_end=783，25文本槽全部精确绑定；4任务、4目标、11完成事件、10条件、11动作。

| 步骤 | 已恢复的结构与关联 |
|---|---|
| Speak to Mornay，UID2770509568 | WaitForCompletion目标；完成后GiveItem(Scrap,3)，并按difficulty=0/1/2分别GiveMoney1000/2000/3000 |
| Repair the bridge，UID2215992832 | ServerHasToken(GD_FARMLANDBRIDGE_REPAIRED,true)；BeginQuestTask指向UID864511808；isBlocker=true |
| Kill Edmund，UID864511808 | ServerHasToken(EDMUND_DEAD,true)；BeginQuestTask指向UID4270938624；isBlocker=true |
| Report to Mornay，UID4270938624 | WaitForCompletion；CompleteQuest；GiveFaction(SURVIVORS,250)；按三种难度分别GiveExperience750/17000/42000 |

这些数值仅作为读取验收，不做修改。任务中没有直接写“杀死NPC”的执行指令：目标检测服务器token。token何时由对话、地图或脚本写入，是外部逻辑，不能仅凭本QST推断。

## 明确未恢复

- 条件/动作的原生运行时求值、触发次序、重复执行防护、多人同步、对话发放任务、token生产源和Lua函数体；解析参数不等于恢复这些行为。
- 节点flags的未知位含义、保留类型与版本>1；严格拒绝未知输入。对bool仅接受0/1，比原生非零即true更严格，因此不声称损坏输入行为逐项等价。
- 当前验证基础归档，不等于全部DLC数据或未来版本兼容。

## 后续实现接口

以已恢复结构建立不可变QuestDefinition/TaskDefinition/ConditionSpec/ActionSpec，运行时状态单独保存；恢复执行器前不把definition结构当成真实存档格式。未来行为测试应按给定状态输入比较条件结果、事件序列和状态差异，特别验证难度分支与多人token，不能只检查成功退出。
