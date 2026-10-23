# Engine 根名称与稳定快照查询接入

2026-10-21。HEAD `6dfdae419d1c92f7738eefe241937057d83134df`。本轮保留既有改动，不 commit/push。

## 新增原生静态证据

只读核实了三个已知名称地址，实际语义读取 **20 B**，每次 1 B，遇首个 NUL 即停。三个值均与已归档 writer 的 RIP 相对 LEA、ClassInfo `+8` 写入及同地址导出角色相符。

| 名称 RVA | 原始 bytes（含 NUL） | 确认文本 | writer RVA | ClassInfo RVA / name 字段 RVA |
|---|---|---|---|---|
| `309E64` | `4163746f7200` | Actor | `5C70` | `41CFC0` / `41CFC8` |
| `309AC4` | `456e7469747900` | Entity | `61F0` | `41CF30` / `41CF38` |
| `309AEC` | `4f626a65637400` | Object | `7270` | `41CF00` / `41CF08` |

原件固定为 `D:\Steam\steamapps\common\Grim Dawn\x64\Engine.dll`，4,485,632 B。使用同一 `O_RDONLY` 句柄做前后 fstat 及流式全文件指纹，SHA256 均为：

`4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`

两次指纹合计遍历 8,971,264 B，缓冲 64 KiB；与名称语义读取预算分开计数。fstat 与哈希均稳定，原件关闭后才写新证据目录。这不是原子快照或独占共享模式保证。没有新代码读取、PE header 读取、整库扫描、DLL 加载或原版执行。

- [固定读取器](../../tools/static_analysis/read_engine_root_names.mjs)：SHA256 `7089970401b8787b98c8e0c57980f90061688c638b0be533d85e67c63fea7228`。
- [原始读取报告](offline-engine-root-names/metadata.json)：SHA256 `d882b279afe354a7e8912ca3b70afd07d300a1d278b9e268520c98cdc60e5d79`；包含全部新读 bytes、文件偏移、已固定证据哈希及 writer 对应关系。

与此前已证磁盘 parent 链 `Actor(41CFC0) → Entity(41CF30) → Object(41CF00) → null` 对应，名称现在有实际字节依据。该对应不证明完整运行时图、其他兄弟节点或 CRT 注册调用顺序。此前名称尚未读取的历史报告保留原样，由本记录补足。

## 本轮最小接入

[EngineRootObservedNames.cs](../../src/task_items/EngineRootObservedNames.cs) 把以上三个特定 Engine image 的 preferred-VA token 映射成独立 byte[] 副本。它不归一化大小写、不猜编码，也不把数据 token 当作可运行地址。

[EngineRootSnapshot.cs](../../src/task_items/EngineRootSnapshot.cs) 将显式、独占、稳定的自有 `EngineRootNode` 状态捕获成现有 `ClassInfoSnapshot`，再由未改动的 [ClassFactoryDispatch.cs](../../src/task_items/ClassFactoryDispatch.cs) 查询。它是自有适配层，不是新发现的原生初始化算法。

- 通过明确的 factory 身份对象和自有 `Func<object>` 绑定工厂；身份比较使用引用，未绑定或空 callback 仍未知，不能补成返回 null 的伪工厂。捕获过程不执行 callback。
- 名称只接受实际提供的 byte[]，逐字节保留并复制；不补 NUL。null、其他对象或未终止的已知跨度可以保持未知，具体查询仍按所需前缀判断。
- 保留当前 signed count 和发布的 children；count 非正时不追子槽。正 count 的短跨度保留为短跨度，不把缺失尾部伪造为已知 null。已知 null 子指针、顺序、重复边及共享节点均保留。
- 不遍历 parent，不用 writer result 的未发布 `AllocatedStorage` 修复失败状态。count 已增加而新存储未发布的前缀可继续存在；exit 返回 `-1` 也不回滚已发布状态。
- `Captured` 只表示所提供状态可以捕获；`HasUnknownMetadata` 提示缺失部分，不表示注册成功或完整元数据。环和预算限制返回自有 `Unsupported`，不是原生校验规则。
- 上限为 4096 个不同节点、16384 个已知子槽、1 MiB 名称字节、128 层非 null 子节点、4096 个绑定。每个已捕获节点的槽只计一次，但重复槽不去重；共享子树还检查后续更深路径。调用者可明确指定更小预算。

现有查询器依据保存的 Engine `274EC0` 证据：self 先比较字节，首个相等 NUL 结束；self 匹配时调用工厂并直接返回，哪怕返回 null；child 返回 null 后父节点继续下一个 sibling，未知 callback 则返回 Unsupported。快照固定元数据，**不覆盖原生 factory callback 期间修改 children/count 后再重读的动态行为**。

## 验证状态

经独立源码及脚本文本复核后，使用正式 `require_escalated` 机制单独申请并执行一次精确构建与新 fixture。实际结果：**93 个新增断言 / 9 组场景全部通过，退出码 0**。没有失败重试或替代执行路线。

```text
PASS: 93 assertions, 9 scenario groups
```

| 新场景组 | 断言 |
|---|---:|
| 三个原始 literal 的 token 映射与独立副本 | 11 |
| 实际名称经显式 root writer、快照到工厂查询 | 12 |
| 字节、NUL 后缀、未终止及不透明名称 | 13 |
| self 优先、null 工厂结果与未知 callback | 6 |
| 已知前缀、null 槽和 signed count | 10 |
| 重复边、共享节点、引用身份绑定 | 8 |
| writer 部分状态、null strdup 和 exit 失败 | 12 |
| 循环、共享图更深路径和各项捕获预算 | 10 |
| 空根、缺少绑定和无效预算 | 11 |

fixture 的调用顺序、初始状态及工厂 callback 均为显式自有输入。小数组、64 B 分配 callback 上限和 128 步 writer 预算控制本次测试范围；没有分配与原生极值 count 等长的数组。

- [新增 fixture](../../src/task_items/EngineRootNameDispatchTests.cs)
- [精确构建脚本](../../src/task_items/build_engine_root_name_dispatch.ps1)
- [构建前记录](engine-root-name-dispatch-validation/preflight.json) / [实际运行与哈希记录](engine-root-name-dispatch-validation/run.json)

正式执行命令为同一 Windows PowerShell 5.1 的 `-NoProfile -NonInteractive -File ...\src\task_items\build_engine_root_name_dispatch.ps1`。脚本使用已安装 csc，`/noconfig /langversion:5 /checked+`，仅编译四份库源码和一份新 fixture，随后只运行新 EXE。输出目录固定为 `.tooling/engine-root-name-dispatch-build-20261021`，拒绝已有目录及重解析路径。

| 产物 | 字节 | SHA256 |
|---|---:|---|
| GrimDynasty.EngineRootNameDispatch.dll | 24576 | `7eb7ba958aeeec69d2ec53125cbe6bbe3dc7605e32c7337b5cfcb2d27e1775ff` |
| EngineRootNameDispatchTests.exe | 32256 | `df33dbefb170c6868820db39f22cb7e4725d7eb1841b93568c3b93c96457d111` |

六份构建输入的前后哈希一致。原 Engine root writer、旧 dispatcher、Game ClassRegistration 实现/测试/旧被拒脚本，以及此前设计文件均保留原哈希。tracked 改动仍为此前 12 份构建脚本；本段源码和记录均为新增文件。

此前根模型的 221 断言 / 11 场景组，以及更早测试，本轮均未重跑。它们不计入本轮新增验证。

## 下一最小缺口与兼容边界

Game `ClassRegistrationNode` 和 Engine `EngineRootNode` 仍是两套可变记录类型。Item writer 的已证合同是在 `_strdup` 之前将 Item parent 设为导入的 **同一个 Engine Actor 记录**；QuestItem 使用其现有 Item parent。用运行前复制、成功后同步会丢失依赖回调及失败中途可见状态，不能声称已连接这个跨模块共享注册链。

最直接的下一步是显式共用 backing state 或直接操作同一记录类型的局部 writer 接口，仅覆盖已证 Object/Entity/Actor/Item/QuestItem 五个角色。应保留对象及存储身份、模块加 RVA 标识、回调重读、部分写入和失败传播，不自行生成完整图或 CRT 调度。本轮只完成稳定根快照到查询接口的接入，没有实施该共享状态改造。

仍未验证：真实兄弟节点集和运行时顺序、exit callbacks 内容、cookie/onexit 初始化、原生构造器及 Read/完整物品工厂、动态查询中的元数据变动、正式样本兼容。原存档兼容不是项目目标。

## 检查点

可恢复检查点：`.tooling/checkpoints/engine-root-names-dispatch-complete/manifest.json`。封存本段源码、读取器、原始名称字节报告、构建前/运行记录、产物、直接支撑证据及此前根模型检查点引用。副本逐一比较 SHA256。

本轮没有启动原游戏、Rea、Java；没有接触存档/Steam 云，没有安装工具、更改执行策略/ACL 或执行旧被拒构建脚本。新构建独立经正式审批成功，先前只读查询批准没有充当构建批准。无剩余审批阻塞；上列兼容缺口仍保留。
