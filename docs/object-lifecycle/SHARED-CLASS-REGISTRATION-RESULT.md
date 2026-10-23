# Game / Engine 共享注册记录的局部接入

2026-10-21。HEAD `6dfdae419d1c92f7738eefe241937057d83134df`。保留现有修改，不 commit/push。

## 具体改动

新增 [SharedClassRegistration.cs](../../src/task_items/SharedClassRegistration.cs)，提供 `ApplyItem`、`ApplyQuestItem` 两个局部入口。它们直接操作原有 `EngineRootNode` 与 `EngineRootChildStorage`；调用者向 Engine writer 和 Game writer 传递的是同一对象，节点、槽数组、名称与 factory 身份不需要转换或事后同步。

Object、Entity、Actor、Item、QuestItem 的记录及 parent 由调用者明确提供。`EngineRootNode` 在这里是自有共用状态类型，不代表 Game 的地址属于 Engine 模块。原生身份仍须按模块与 RVA 区分；两个 DLL 的相同 preferred base 不构成相同身份。

旧 `ClassRegistrationInitialization` API、Engine writer、名称目录、快照适配及工厂查询器均不修改。新入口只复用 Game 模型的纯 `AllocationSize`、覆盖范围上限及 Game exit token；Game 的 Allocate、Free、RegisterExit 仍通过各自完整 helper 的显式自有 callback 提供，**不自动改用 Engine allocator 或 exit helper**。

直接共享入口相对完整重构的范围更小，代价是保留一段与原 Game writer 对应的局部控制流；后续若更新原生合同，两处需一起核对。不存在跨类型可变记录自动转换，调用者必须从一开始使用共同记录。

## 已存证据与必须保留的顺序

现有证据足够支持本段，无需新增原版读取：

- [Game 静态 writer 证据](CLASS-STATIC-INIT-EVIDENCE.md)及 [IAT / 名称 / ClassInfo 数据](offline-game-class-init-data/metadata.json)：Item 在 Game `5F04` 先将 parent 写成导入的 Engine Actor；QuestItem 不写 parent，只使用既有值。Item/QuestItem 名称分别为 `Item\0` / `QuestItem\0`。
- 字符串 callback 返回后重新读取 parent，再存储实际返回 name（包括 null）。后续 Allocate callback 即使修改 `node.Parent`，也不更换已选定的 parent。
- 分配请求用分配前 count 计算，复制用分配后当前 count/storage；保持顺序、重复槽、已知 null。随后为 append → count++ → 读取旧 storage → Free → 发布新 storage → RegisterExit。
- [Game helper 证据](CLASS-REGISTRATION-HELPERS-EVIDENCE.md)：Allocate/Free 为完整 helper 合同，RegisterExit 正常返回只能是 `0/-1`。`Known/-1` 表示局部返回了失败，不是注册成功。

缺失 callback、跨度不够、自有异常等均在到达相应阶段时停止，保留已完成字段写入和 callback 副作用。Free 中断时可能保留新 count、旧 children 和未发布分配；Free 正常返回后，外层仍按已证顺序发布其分配，不会合并或修复 callback 修改的 storage。失败不得自动重放。

Game 的已保存指纹为 `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`，Engine 为 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`。本段原版读取为 **0**，这些是归档证据中的指纹，并非本段重新测量。

## 本轮验证状态

源码与脚本经独立静审后，单独通过正式 `require_escalated` 请求执行一次新构建与 fixture，**77 个新增断言 / 8 组场景通过，退出码 0**。没有失败重试，没有复用以前的只读查询或构建批准。

```text
PASS: 77 assertions, 8 scenario groups
```

| 新场景组 | 断言 |
|---|---:|
| 显式五节点共享链与现有查询路径 | 12 |
| Game 字符串回调中的 Engine 重入与 parent 重读 | 6 |
| 分配回调增长后的容量不足、收缩后的成功重读 | 8 |
| Engine 分配回调中的 Game 重入 | 5 |
| 重复边及多次调用同一工厂 | 6 |
| Free 重入后正常覆盖发布或异常保留 | 12 |
| 共享依赖缺失及 exit 返回合同 | 8 |
| 4 个新增稳定输入与旧 Game API 的局部对照 | 20 |

增长 fixture 中，外层原请求为 8 B，回调追加后目标只容纳一个槽，外层保留复制前缀并在 append 处停止。没有扩大分配伪造成功。Free 重入 fixture 也区分了正常返回后外层覆盖发布与异常中断后保留内层发布；前者可能留下新 count 和短存储跨度，查询将此保持为未知尾部。

- [新增实现](../../src/task_items/SharedClassRegistration.cs)
- [新增 fixture](../../src/task_items/SharedClassRegistrationTests.cs)
- [精确构建脚本](../../src/task_items/build_shared_class_registration.ps1)
- [构建前记录](shared-class-registration-validation/preflight.json) / [运行与哈希记录](shared-class-registration-validation/run.json)

构建使用同一 Windows PowerShell 5.1、已安装 csc 和 `/noconfig /langversion:5 /checked+`，固定六份库源码、一份新 fixture；仅运行新 EXE。新输出目录 `.tooling/shared-class-registration-build-20261021` 不允许已有目录、重解析路径或覆盖。

| 产物 | 字节 | SHA256 |
|---|---:|---|
| GrimDynasty.SharedClassRegistration.dll | 35328 | `13cadb207559e5984616e36e7d15a93e291abecf749f2006c8bfcb7be90a6eb9` |
| SharedClassRegistrationTests.exe | 35328 | `aa965dabbc924c22249814058f5e21a1dd0b3ef23f79780acb5008f370420627` |

八份构建输入前后哈希一致。五份既有模型源码、三份既有 fixture 及其构建脚本保持原哈希；旧被拒脚本未执行。tracked 差异仍为既有 12 份构建脚本的 60 行增加 / 46 行删除。

测试将使用小型自有图和受控同步 callback。重入 callback 只是验证共享状态在正确阶段可见，不证明原生 CRT 实际这样重入。完整五角色链也只是调用者明确提供的 fixture，不是自动恢复的运行时全图。

旧 Game、Engine root 221 断言以及上一段名称/快照 93 断言套件**均未重跑**。新增 fixture 确实调用了 4 个旧 Game API 对照输入，并通过新整合场景执行既有 Engine writer、快照和 dispatcher；这是新增整合测试的执行范围，不是旧套件整体通过的声明。实际最多分配 3 个指针槽，独立 callback 上限为 64 B，Engine writer 步数预算为 128。

## 仍未验证的范围

本段接入可将共享状态交给已有 `EngineRootSnapshot.Capture` 和 `ClassFactoryDispatch.Create`，但快照是在状态稳定时捕获的。它不覆盖原生查询 factory callback 期间动态修改 count/storage 后的继续遍历。

完整兄弟节点集、真实 CRT 初始化调度、exit callback 内容、cookie/onexit 初始化、原生异常/内存所有权、原生构造器与 Read、完整物品工厂和正式样本兼容仍未验证。不以自有整合 fixture 证明原生全运行时兼容；不支持原存档兼容。

最直接的下一局部缺口是动态工厂查询：先核对保存的 Engine `274EC0` 指令中 callback 后 count/storage 的读取顺序，再决定如何直接遍历共享记录。当前稳定快照不能替代这项运行时合同；本轮没有扩大实现到该范围。

## 检查点

可恢复检查点为 `.tooling/checkpoints/shared-class-registration-complete/manifest.json`，包括本段源码、构建脚本、fixture、产物、构建前/运行记录及直接 Game writer 证据，并引用上一段名称/快照检查点。封存时逐项验证源与副本 SHA256。

本轮没有运行原游戏、原 DLL、Rea、Java，没有接触存档/Steam 云，没有安装工具、更改执行策略或 ACL、运行旧被拒脚本或 commit/push。当前无审批阻塞，上述兼容缺口仍保留。
