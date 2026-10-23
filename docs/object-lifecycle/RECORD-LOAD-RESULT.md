# 记录加载链检查点 — 2026-10-19

后续检查点：[Class getter 与部分创建路径](CLASS-GETTER-RESULT.md) 已连接普通表 getter、类工厂、构造及原始名称赋值，703 条断言通过。本文保留此前 620 条断言检查点的结果与当时断点。

已补齐 LoadFile 名称处理、缓存查找和 miss 加载分支的静态证据，并实现**有明确前提的缓存命中路径**。这让已知缓存表能够作为后续自有构造、赋名与注册测试的真实输入边界；完整记录文件加载、原生工厂绑定和派生 Read 仍未完成。

[RecordLoadFile.cs](../../src/task_items/RecordLoadFile.cs) 实现正常成功域内的局部名称复制、名称处理、查找、命中计数及解锁返回；[RecordCache.cs](../../src/task_items/RecordCache.cs) 表示稳定的完整缓存快照及桶内顺序。名称长度最多 65536，缓存最多 4096 项、4096 桶、1 MiB key 总量。已知 C locale 是调用方显式前提，不是对游戏运行时的推断。未知 locale、全部高位字节和 miss 都有明确 Unsupported 边界；空名称或全反斜杠名称无需调用 tolower。

名称按完整 stored length 处理，嵌入 NUL 继续参与；反斜杠变为正斜杠，不消除重复分隔符或处理 `..`。查找使用 unsigned-byte FNV64 和完整字节相等，不复用对象名称的 signed-byte FNV32。保存的缓存键不会再次转换。桶内从 first 向 next 查到 last 后停止，重复相等键保留首个匹配项。命中计数按 uint32 回绕；已知 null 表值仍属于命中，不能当成 miss。

两个固定 Rea/Ghidra 批次共 **22 次调用**，分别为 [三个直接依赖 14/14](rea-record-load-dependencies/summary.json) 和 [比较入口 8/8](rea-record-cache-compare/summary.json)。均确认会话关闭、子进程退出 0、Engine 哈希不变。静态 PE 元数据确认 `tolower` 和 `memcmp` 的导入身份；实现采用正常有效输入下的外部函数契约，没有恢复或执行 CRT 内部代码。见 [完整证据和覆盖条件](RECORD-LOAD-EVIDENCE.md)。

## 整体进度和剩余断点

[生命周期进度图](LIFECYCLE-PROGRESS.md) 同时展示 CreatePartial、FinishPartial 与分配器接合位置。构造、SetObjectName、ID 写入、pending 删除、双索引插入及有界增长已有自有实现；图中仍保留以下完整成功链断点：

- 原生 ClassInfo population、实际派生构造目标和对应构造效果。
- 缓存 miss 的数据库取数及原始缓存初始化；分支中 `0x180280E20`、表构造 `0x18013F300/0x18013C1D0`、二进制表虚解析、缓存节点 `0x18005DEB0/0x18002E770` 尚未形成完整实现。
- LoadTable/LoadTableBinary 的 Class getter `0x18013C6B0/0x18013FB60`。
- 具体派生对象 vslot +8 的真实 Read，以及分配器与登记/销毁的完整生命周期接合。

新证据已确认 miss 的正常失败分支会记录错误并构造 `LoadTable`，成功取得数据的分支构造 `LoadTableBinary` 后调用其解析槽。两者随后都会缓存结果并更新计数/耗时。当前代码在到达 miss 时停止，未用空表、空 Read 或 no-op 回调冒充这些行为。下一次可直接围绕上述表构造及 getter/解析目标继续静态补证，不需要重新拆解已闭合的名称和缓存命中路径。

## 验证与保存

全新 `.tooling/record-load-build/` 构建通过 **620 条断言**：原有 558 条，加上 [缓存加载测试](../../src/task_items/RecordLoadFileTests.cs) 的 42 条和 [生命周期组合测试](../../src/task_items/ObjectLifecycleCombinedTests.cs) 的 20 条。两个新增组合场景和一个既有 ItemQuestDemo 场景均通过；断言数字已包含组合测试，没有重复累加场景为断言。只引用框架与自有 GrimDynasty 程序集。

组合测试串起自供 ClassInfo 工厂回调、基类构造、名称容量 `15→31→47`、pending 删除和两个索引的 `8→64` 增长，并核对稳定对象/旧节点身份和锁外 own Read。缺失回调的第二场景在全部登记和增长后仍为 Unsupported。缓存测试还将命中的同一表对象传给现有注册协调器，验证对象原名称及 raw key 未被局部小写化修改，真实 Read 断点和已完成副作用均被保留。

关键测试覆盖完整 NUL 长度、unsigned FNV64 独立期望值、碰撞/重复键/桶终点、名称输入与输出快照隔离、已知 null 表命中、计数回绕、未知 locale、高字节、缺少缓存、65536/65537 长度边界，以及短/长局部名称容量。源代码、二进制和逐套件计数见 [验收记录](record-load-validation/acceptance-record-load.json)；另保留 [构建输出](record-load-validation/build-output.txt)、[Object ID 回归](record-load-validation/object-id-regression-output.txt)、[独立哈希向量](record-load-validation/hash-vectors.json) 和 [程序集引用](record-load-validation/task-items-dependencies.json)。

修改前 runner/build 已存于 `.tooling/checkpoints/record-before-load/`。本轮通过版本的自有源码、固定查询计划/runner、静态证据、进度图和验收记录保存至 `.tooling/checkpoints/record-load-passed/`，并由 [检查点清单](record-load-validation/checkpoint-manifest.json) 逐项记录/验证哈希。它是接续已有检查点的增量保存，不是完整可运行游戏或原始源码恢复。

13 个原版 exe/DLL 均与固定清单一致，见 [哈希复核](record-load-validation/game-hashes-after.json)。原版仅静态读取；没有启动游戏、加载原 DLL 执行、访问玩家存档或 Steam 云数据。旧 UCRT 指纹白名单保持不变，本轮不宣称重跑了旧机 894 断言 / 16 场景全验收。没有安装额外软件、修改全局配置、reload/restart、commit 或 push。两个逐次 Rea 执行动作均获准；没有 denied、pending 或 uncertain 的动作。
