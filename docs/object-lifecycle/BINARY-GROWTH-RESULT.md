# Binary 指针缓存扩容接合结果 — 2026-10-20

已完成 `0x180284200` 的真实 Rea MCP 静态查询，以及正常成功域内的自有实现与生命周期接合。统一 fresh build 和独立 ObjectId 回归一次通过：**930 条断言、14 个组合场景，另有既有 ItemQuestDemo**。本轮新增 BinaryPointerGrowth **36 条、2 场景**；此前 894 条全部重跑，其中 cold suite 的旧增长断点预期已随已观察的新行为更新。这是当前有界模块验收，不是旧机完整离线 894/16 的重跑，UCRT 门禁未改。

## 本机 Rea 验证与资源释放

使用已有 Rea 6.5.0、Ghidra 12.1.4、项目 stdio 启动器和既有 9 工具白名单，执行固定 **8 calls**：会话检查、打开固定 Engine、目标伪码/汇编/直接 callee、关闭及最终会话检查。实际分析成功，证明 MCP 服务和 Ghidra 后端可用；当前 Codex 会话没有直接注入 Rea 工具，这个 UI/连接入口状态没有被冒充为已修复，也没有为此改持久权限或重装软件。

[查询记录](rea-binary-pointer-growth/summary.json) 的开始/结束为 **01:45:49.895 / 01:47:27.431 UTC**，共 **97.536 秒**；首个分析请求 88.866 秒。实际 Java 参数为 `-Xmx512M`，50 次两秒采样最低可用内存 **6.524 GiB**，未触发保护。正常 close、最终无活动目标、Rea exit 0 均已确认；[主机复核](rea-binary-pointer-growth/resource-release.json) 于 **01:47:44 UTC** 确认 Rea PID 20744、Java PID 21120 均不存在，分析 runtime 无文件，空闲 **7.515 GiB**。本轮重型窗口已经释放；这些观测不代表未来内存保证。

固定目标 `D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll` 的查询前后 SHA256 均为 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`。分析是静态读取，没有运行目标 DLL。保留此前 13 文件原版哈希记录，没有重新扫描存档或云数据。

## 实现及覆盖前提

[汇编与证据说明](BINARY-GROWTH-EVIDENCE.md) 确认：函数扩大 **slot 存储**，按原槽索引复制完整 Address/ID，并修正 Next 和桶头；不重新散列，不改变 shift、mask、桶数、链顺序或 used count。caller 以 uint32 加法翻倍容量，再重新执行插入。原生分配尺寸为新容量×24，而清零调用仅覆盖新容量×8；未用槽的自有 `null` 表示可写单元，不声称 native bytes 全零。

[BinaryWarmTable.cs](../../src/task_items/BinaryWarmTable.cs) 将增长接在已有 cold 插入前。完整有效快照限定 `usedCount == capacity > 0`，翻倍后不超过 4096。增长创建新 Entry 身份，保留槽索引及字节值，按旧桶链顺序替换为新引用；旧 Entry 仅保留历史状态，不再属于当前缓存。`buckets.Length` 明确代表原生 `+04` 的完整桶数，不从 mask 或 capacity 推测。

自有表示先完成所有迁移分配，再发布桶链、新 slots 和 capacity，最后由已有插入路径消费原 count 槽并前插一次。该实现覆盖正常分配、复制、释放成功域，不等同于原生中途故障、内存访问宽度、并发可见性或异常展开。三个 callee 只有当前 caller 的调用与伪码标签，未独立恢复其完整函数体或精确 IAT 绑定。

新增 `CacheStorageGrown` 和前后容量记录；[RecordTableGetter.cs](../../src/task_items/RecordTableGetter.cs) 将增长并入 `MayHaveEffects`，CreatePartial 的既有聚合继续传播。numeric、构造器或缺失 own Read 导致 Unsupported 时，已完成迁移和插入仍保留。若自有异常发生在详细 Binary 结果返回前，外层仍保留增长标志，但不保证存在 Binary 详细容量记录。

容量 0 仍翻倍为 0，原 caller 会再次递归，因此自有实现明确返回 `BinaryPointerCacheZeroCapacityGrowthOutsideCoveredDomain`；不创造最小容量。翻倍超过预算返回 `BinaryPointerCacheGrowthOutsideCoveredDomain`，在首次缓存写前停止。已有大容量的 spare 插入、warm/stale 访问不受增长预算提前阻断；name miss/-1 仍跳过插入/增长进入 numeric。

## 验证与保存

- [新增测试](../../src/task_items/BinaryPointerGrowthTests.cs) 验证槽序与链序不同、重复地址首命中、迁移后 stale 只写新节点、两次独立增长、3→6、2048→4096、2049 的最后 spare 与后续增长拒绝、完整 64 位值、负 ID、后续 numeric/构造失败及两种 Create→Finish/own Read 结果。
- [统一构建日志](binary-growth-validation/build-output.txt)、[ObjectId 日志](binary-growth-validation/object-id-regression-output.txt)、[执行记录](binary-growth-validation/build-run.json)：总耗时 **6.5619772 秒**，本轮无失败重试。产物在 `.tooling/binary-growth-build/`。
- [82 项验收清单](binary-growth-validation/acceptance-binary-growth.json)、[一致性检查](binary-growth-validation/consistency-check.json)：旧 79 项中 74 不变、5 为预期修改，没有额外变化。原有 16 个模块测试源中仅 cold 测试更新，另增 growth suite；[程序集引用](binary-growth-validation/task-items-dependencies.json) 只有 framework 和自有 GrimDynasty 模块。
- 修改前副本在 `.tooling/checkpoints/binary-growth-before/`；通过版本和静态证据在 `.tooling/checkpoints/binary-growth-passed/`，以 [检查点清单](binary-growth-validation/checkpoint-manifest.json) 为准。先前 cold 检查点保留。

下一真实缺口仍是完整原生加载/工厂链：LoadFile miss loader、Binary 解析与 population、实际派生构造/Read，以及 ObjectId 分配到创建/登记/销毁的自动接合。已有 `0x1801C4E20` miss body 可作为下一轮依赖拆分起点；本轮没有自动扩查 callee 或开始另一后端会话。源码和产物均未 commit/push/deploy，未安装额外工具、修改安全设置或扩大持久权限。
