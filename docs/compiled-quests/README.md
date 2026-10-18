# 可编译QST2任务定义模块

验证对象是**本项目自行实现的解码器**，可离线编译运行，不依赖原Game/Engine DLL或Steam。原版授权仅涉及另一条原版动态对照路线，详见 `../VALIDATION-TRACKS.md`；它不阻塞本模块开发。当前不是完整游戏恢复。

## 实现与运行

`src/quest_codec/QuestDefinition.cs` 提供 `GrimDynasty.Quests.QuestDefinition.Read(byte[])`，返回任务、节点、条件/动作参数、Lua调用描述及本地化文本关联。只解码定义，不执行其中任何条件、动作或脚本。

依赖本项目自写 `GrimDynasty.Resources.dll`（只读ARC/LZ4等）及.NET基础库。使用既有Framework csc，不安装软件：

```powershell
& .\src\quest_codec\build_quests.ps1
& .\.tooling\quest-definition-build\QuestProbe.exe 'D:\Steam\steamapps\common\Grim Dawn\resources\Quests.arc' --member gdareab/mq_murdersandworse.qst
```

构建产物均位于 `.tooling/quest-definition-build`：自写Quests库、CLI和自写Resources库的副本。原DLL未复制进此构建目录。`--all`逐行输出全部定义；`--fixture`仅允许读取私有工程夹具。定义API无Steam、网络或真实存档入口。

## 依据与验收

格式依据此前官方程序集完整IL：Quest/Task/Objective/Event的Read、Scriptable工厂及各Read、Lua参数、GetTextNodes/InsertInto遍历顺序；另有Rea实际返回的原生Quest2::Load证据。见 `../quest-structure/managed-evidence.json` 与 `../quest-structure/rea-quest-load/Quest2-Load-full.json`。伪代码/IL仅是证据，此处C#是自行编写并实际编译的实现。

正式差分结果见 `acceptance.json`：

- 247个基础任务全部比较通过；包含459个任务步骤、361个OnAccept事件、561个目标、2979个OnComplete事件。
- 2435条条件与3346条动作的类型、版本、参数、偏移及文本槽全量一致；Lua参数含类型和原始数值，不运行Lua。
- 所有payload字段与Python比较；仅排除Python输出中的coverage说明文字。布尔值检查类型，浮点按float32位值比较。
- 官方ArchiveTool已导出的任务样本另行输入C#和Python，完整解码结构一致；源归档前后SHA256未变。
- 16项合成边界验证通过：未知类型/版本、非规范bool、运算符、图区边界、文本槽数、截断、偏移/数量超限、尾字节、重复语言、非法UTF-8；另覆盖多语言/非BMP汉字、Lua动作及Lua条件浮点返回。

边界测试发现.NET默认JSON序列化会丢失负零符号，CLI现对数值作明确输出，保留`-0.0`；解码API本身保存double承载的float32值。非有限浮点在CLI传输中明确拒绝，库不执行或推断其语义；全部本机基础任务传输通过。

复跑需要现有Resources构建和少量官方样本，报告新建不覆盖；边界测试每次给新run-id：

```powershell
cd tools/resource_reader
python verify_compiled_quests.py --output ../../docs/compiled-quests/acceptance-local-new.json
python test_compiled_quests.py --run-id local-new
```

## 未恢复与边界

仅QST2 version9；解码对象上限8MiB，集合/节点/文本均有界。保留flags原数值，不猜未知位。版本>1或保留脚本类型拒绝，bool仅接受0/1，比原版非零即true更严格；不宣称损坏输入完全复现原程序行为。

尚未实现条件求值、任务推进次序、奖励执行/重复防护、多人状态、Lua函数体、DLC全量兼容、战斗/渲染和原存档。自写最小场景及状态接口可另行离线推进；原版动态对照仍等待已同意但暂缓的隔离准备，不影响这些自写模块的编译运行。
