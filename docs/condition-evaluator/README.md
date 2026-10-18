# 条件求值：静态证据支持的最小可编译子集

验证对象是本项目自写实现，不是运行中的Grim Dawn。代码仅使用本地模拟输入，不加载原DLL、Steam接口或真实存档。原版动态对照仍暂停；本工作不依赖BIOS/Sandbox/重启。

## 当前覆盖

`src/condition_logic/ConditionEvaluator.cs` 提供单条件与集合求值，返回 `Known + bool` 或 `Unsupported + null + reason`。当前支持条件version0：

| 类型ID | 条件 | 自写模拟输入 | 已确认的原生值语义 |
|---|---|---|---|
| 4 | IsDifficulty | Difficulty及条件自身的DifficultyOverride | 先取得GameInfo难度；override不为0xffffffff时替换。按存储阈值与实际值比较，顺序比较采用int32有符号解释 |
| 5 | IsHardcore | MainPlayerHardcoreByte | 比较主玩家offset0x4bd5的原始字节与条件存储字节；不是“非零即true”，比较selector未使用 |
| 17 | WaitForCompletion | 无 | 该虚函数槽指向恒false共享叶函数；不是完成任务或推进任务的实现 |

不支持的条件、版本、上下文或覆盖域外selector/operator均返回Unsupported，不把未知规则伪装成false。

## Rea原生证据与关键边界

`evidence-summary.json` 提供6份实际Rea函数证据ID、完整结果链接、vtable链与汇编；目标x64 Game.dll SHA256为`cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`。

- 组合函数0x180474680（225字节）：operator0为And、1为Or，按顺序短路；显式空And=true、显式空Or=false。布尔参数跳过type17，**不是**切换And/Or。跳过在虚调用前发生，被短路或跳过的未知叶不求值。
- 工厂0x18046ec30（2240字节）确认type4/5/17的vtable。组合函数调用vtable+0x28，对应难度0x180472f20、Hardcore0x180472ff0、Wait0x18001a700。类角色由这条链确认，不靠反编译器显示名称猜测。
- 难度汇编在`CMP [RBX+0x20], EAX`后分别使用SETL/SETLE/SETZ/SETGE/SETG/SETNZ。因此selector0..5依次为 `stored<actual`、`stored<=actual`、`stored==actual`、`stored>=actual`、`stored>actual`、`stored!=actual`。这与通常假设的“actual 对 threshold”方向不同。uint资源参数保留原位值，排序时重解释int32。
- override sentinel为0xffffffff；GetGameInfo/GetDifficulty在选择override之前调用。自写接口不模拟原getter副作用；需要显式提供上下文，缺失则Unsupported，不创造游戏状态。
- Hardcore的原生读取对象是全局主玩家，不是集合传入的任意Entity。模拟上下文名称明确为MainPlayerHardcoreByte；不声称已恢复主玩家构造、身份或字段生产路径。
- Wait的3字节汇编为XOR AL,AL / RET。链接器折叠使Rea显示大量其他导出名称；同样，集合构造器被命名为AuraManager，但同地址存在对应条件集合导出。依据vtable和原始指令确定角色。
- 构造器0x180019fc0只置零vector三指针，未初始化operator。旧证据Quest2Event::Read仅在count>0时分配条件集合。所以**序列化空列表的null operator不自动当And**：定义适配器暂返回Unsupported，等待恢复调用者的空容器门逻辑。显式创建的And/Or空集合则按上述函数处理。

原生无效比较selector/集合operator会返回false；当前恢复API对这些未覆盖状态有意报告Unsupported。version0以外也未纳入本子集。这是覆盖边界，不是对全部非法状态声称行为等价。

## 编译和测试

```powershell
& .\src\condition_logic\build_conditions.ps1
& .\.tooling\condition-evaluator-build\ConditionEvaluatorTests.exe
& .\.tooling\condition-evaluator-build\ConditionCatalogProbe.exe 'D:\Steam\steamapps\common\Grim Dawn\resources\Quests.arc'
```

使用既有Framework csc，无下载或系统安装。核心 `GrimDynasty.Conditions.dll` 仅引用.NET；目录中的Resources/Quests库是本项目自写库，只供目录集成探针解码输入数据。

测试证据分三层：

1. **静态原生规格**：实际Rea伪代码+汇编+工厂/vtable对应链，确认方向、符号性、override、原始字节、短路、空显式集合与skip标志。
2. **自写代码测试**：`unit-acceptance.json` 的50项固定断言，期待值直接按上述分支手工列出，不使用Python求值器作行为oracle。覆盖三种关系的全部selector、int32高位/负一、override哨兵、raw255字节、缺失上下文、Unsupported及访问/跳过顺序。
3. **官方数据集成**：`catalog-acceptance.json` 使用247任务中1784条难度条件、260条Wait及其他391条作为参数，在模拟难度0/1/2与skip开/关六种配置下运行自写代码。官方难度条件全是Equal，阈值0/1/2数量595/594/595，与输入清单交叉核对；这不能证明其他比较或边界，后者依靠层1/2。

每种模拟配置处理3901个节点集合；2015个得到Known，1886个保持Unsupported，其中1525个为未恢复调用者语义的空原生容器。skip打开使260个Wait相关结果由false变true；没有执行奖励、脚本或任务推进。IsHardcore在基础任务中没有实例，当前仅有静态证据和合成输入测试。

**未进行原版动态运行或逐输入执行差分**。所以这是静态证据支持、经自写测试的值层子集，不是原游戏全行为的独立动态证明，更不是“与Python同实现一致就证明行为等价”。

## 待恢复

其他条件类型、角色/实体与主玩家输入生产、token/背包/阵营查询、Lua、时间与随机源、事件调用者空容器门、原getter副作用、任务推进/动作执行和完整游戏。下一优先证据目标可为Quest2Event调用门或IsLevel的实体类型及取级链；未取得证据前继续Unsupported。

本轮Rea启动曾被自动审查误判为未批准的新安装。核对既有用户批准、持久固定6.5.0配置、已验证tarball及缓存入口后，同方法重试一次获准并完成上述分析；没有换启动路线，也无新增安装/权限配置。两个会话均已关闭。
