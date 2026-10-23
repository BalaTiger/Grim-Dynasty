# FinishPartial 名称边界接合 — 2026-10-19

本轮仅使用已保存的 Rea 汇编与 helper 证据，补上了 `FinishPartial` 的实际名称边界：**从对象原始名称取首个 NUL 前缀 → 重新 LoadFile → 按加载结果进入诊断或已有注册流程**。未启动 Rea、Ghidra、重型索引或其他后台分析进程，不需要新增查询。

此前组合测试显式传递完整名称给第二次 LoadFile，未实现这一 caller。新增 [FinishPartialObject.cs](../../src/task_items/FinishPartialObject.cs) 按 `Engine 0x18017E230` 的已有汇编顺序调用现有模块。空对象先返回；零 ID 在读取名称前返回；有效非零 ID 才复制名称的 C-string 前缀。局部字符串从容量 15 开始，正常成功域内长字符串容量为 `length | 15`，加载完成后的正常释放契约位于表空检查之前。

这个接合会改变真实可见的选择结果：对于 `Records\MiXeD\Entry\0TAIL/AbC.dbr` 的逻辑字节名称，CreatePartial 按完整 stored length 查询缓存，FinishPartial 则只查询 `Records\MiXeD\Entry`。两次可以命中不同的表。后续注册必须使用第二次加载的表，而对象名称和 `RawKey28` 仍保留完整原名及其哈希，不能从截断或规范化名称重算。

已知 null 表的命中仍增加命中计数，但随后必须到达引擎诊断，不能写 ID、删除 pending、插入索引或调用 Read。诊断槽尚未恢复，因此返回明确的 `UnimplementedLoadFailureDiagnostic`；即使名称为空也不跳过它。缓存 miss 仍停在未恢复的 loader，不能被当作已知 null 表，也不冒充已执行诊断。

非空表接入已有 `LoadedObjectRegistration`，保留 ID 写入在 manager gate 之前、pending 删除及双索引插入、解锁后 Read 的顺序。调用方未提供 Read 时，保留完成的登记状态并返回 Unsupported。这里的显式 own callback 不代表原生派生 Read 已恢复。

## 证据与覆盖范围

见 [精确地址与已有证据](FINISH-PARTIAL-EVIDENCE.md)。关键分支来自已归档的 [FinishPartial 汇编](rea-native-registration/05-ObjectManager-FinishPartial-order-assembly.json)；临时复制 `0x180022CE0` 的证据已在上一轮取得，正常增长与释放契约来自既有名称分析。没有新增猜测的表、类绑定、诊断或 Read 实现。

覆盖限于有效、稳定的自有对象和逻辑字符串、显式缓存与登记状态、已知 C locale 的相应输入域、正常字符串分配/复制/释放及已有容器预算。名称前缀最多 65536 字节；在首个 NUL 之后的字节不进入本次 LoadFile 的 ASCII/locale 检查。自有对象始终具备完整逻辑终止符；未恢复损坏原生存储、并发、指针寿命、原生异常或 OOM。

剩余缺口是二进制表非空 archive 的指针缓存、缓存 miss 和表解析、原生类注册/派生构造绑定、失败诊断、实际派生 Read，以及 ID 分配器与登记/销毁的完整接合。这轮补齐的是 caller 边界，不是完整原生物品工厂。

## 验证与保存

全新 `.tooling/finish-partial-build/` 统一构建一次通过，耗时约 **4.7 秒**。既有 703 条断言与新增 [FinishPartialObjectTests](../../src/task_items/FinishPartialObjectTests.cs) 的 43 条均通过，合计 **746 条断言**。三套组合测试共 **6 个场景**，另有既有 ItemQuestDemo 通过；单独运行的 ObjectId 20 条回归已计入总数，场景不重复折算为断言。

两个新增整体场景真实调用 CreatePartial 和 FinishPartial：完整 32 字节名称首次命中 creation table，经实际 Class getter 和工厂构造后，FinishPartial 取 19 字节前缀命中另一张 finish table；两个索引都从 `8→64` 扩容。成功场景在 gate 外将第二张表交给显式 own Read；缺失 Read 的场景保留全部完成状态并返回 Unsupported。测试同时验证未再次调用构造器、未重算完整名 raw key、旧节点身份、null 表计数回绕与诊断边界、miss 与 unknown cache 区别、ID 写入次序、回调异常不回滚，以及短/长临时容量。实现、测试与证据均完成独立静态审查，无未处理问题。

保留 [验收与 30 项源码/产物哈希](finish-partial-validation/acceptance-finish-partial.json)、[构建输出](finish-partial-validation/build-output.txt)、[运行耗时](finish-partial-validation/build-run.json)、[ObjectId 回归](finish-partial-validation/object-id-regression-output.txt)、[程序集依赖](finish-partial-validation/task-items-dependencies.json) 和 [13 个原版文件哈希复核](finish-partial-validation/game-hashes-after.json)。既有实现、Rea runner、项目配置及 UCRT provider 记录未变；本轮没有新的外部工具执行审批请求或未决操作。

修改前 build 脚本保存在 `.tooling/checkpoints/finish-before-caller/`。通过版本的源码、自有编译产物、证据及验收保存于 `.tooling/checkpoints/finish-partial-passed/`，由 [检查点清单](finish-partial-validation/checkpoint-manifest.json) 验证；这是基于上一检查点的增量保存，不是独立可运行游戏。上一检查点的 200 个归档文件也全部通过哈希复核。最新状态见 [生命周期进度](LIFECYCLE-PROGRESS.md)。

原游戏及存档未执行或修改，未访问 Steam 云数据，没有新增工具安装、系统配置更改、commit 或 push。UCRT 原有指纹门槛保持不变；历史 894/16 全验收没有重新运行。
