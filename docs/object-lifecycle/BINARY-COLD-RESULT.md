# Binary cold 插入与生命周期接合 — 2026-10-19

已实现 **cold 名称查找 → 临时字符串释放 → 有剩余容量时插入缓存 → numeric getter → CreatePartial → FinishPartial** 的有界正常路径。扩容 `0x180284200` 仍明确 Unsupported，没有增长桩或虚构默认结果。本阶段只读取工程内已保存证据，新增 Rea 调用 **0**，未启动 Ghidra 或读取原游戏文件。

验证合计 **894 条断言、12 个组合场景，另含 1 个既有 ItemQuestDemo**。其中 [BinaryColdGetterTests](../../src/task_items/BinaryColdGetterTests.cs) 新增 **50 条、2 场景**，原有844条全部重跑，包括独立 ObjectId 20条。数字来自这次自有模块验收，不等同于旧机的894条／16场景完整验收；UCRT门槛未改。

## 实现范围

[BinaryWarmTable.cs](../../src/task_items/BinaryWarmTable.cs) 为 pointer cache 增加显式 used count、capacity 和完整 slots 的构造入口。容量不从桶数推导，最多4096 slots；used slots 必须与所有完整桶链中的节点按对象身份一一对应，允许不同槽持有相同地址键。槽身份是快照实例和固定数组下标，unused null cells 表示已经分配的可写单元；空 outer array 仍表示没有桶数组。

在持有既有 table gate 时，以原始64位查询地址选桶。自有节点／新链数组先准备好，然后按 **Count → slot → head** 发布，新增节点置于旧桶链头部。正常提交阶段不分配或调用用户回调。这是正常成功路径的逻辑表示，未声称原生中途内存错误、OOM或异常展开等价。原有节点地址和身份保持稳定，stale 仍只写该节点的 CachedId。

cold 的名称 lookup 使用首NUL前缀和已有 typed int32 映射。临时字符串先释放；name miss／ID=-1 不检查插入容量、不写缓存，仍以符号扩展的 -1 进入 numeric。其他负 ID 原样符号扩展到qword，不提前用名称池范围阻断插入。只有 numeric 真正到达 string 类型取值时，才按 buffer → pool metadata → bounds/sign 的顺序读取；不跳过未知池信息。

[RecordTableGetter.cs](../../src/task_items/RecordTableGetter.cs) 将 CacheCountWritten 和 CacheNodeInserted 独立于 stale 的 CacheIdWritten 报告，并纳入 MayHaveEffects。CreatePartial 的既有聚合因此保留 cold 的 count、slot、head 更新：后续 numeric 不可用、构造器抛异常或 FinishPartial 缺少真实 Read 都不回滚已完成状态。

重复地址的首次命中走 warm／stale；不会因为缓存已满而错误调用增长。相同名称、不同地址属于不同 pointer cache 项，分别 prepend。`count >= capacity`（含0/0）到达 `UnimplementedBinaryPointerCacheGrowth:0x180284200`；缺少插入 metadata、桶存储、映射或可读输入也各有显式边界。

## 验证与保存

- [完整模块日志](binary-cold-validation/build-output.txt)、[ObjectId回归](binary-cold-validation/object-id-regression-output.txt)、[计时与修正记录](binary-cold-validation/build-run.json)。新增测试先修正了旧C#编译器不允许的局部变量同名；独立ObjectId命令补齐绝对Windows源码路径和既有TokenActions引用后通过。失败尝试日志保留，未反复重跑已通过模块。
- [79项源码／产物／工具哈希验收](binary-cold-validation/acceptance-binary-cold.json) 与 [一致性记录](binary-cold-validation/consistency-check.json)。旧76项中71项未变，5项均为预期修改（本轮4项及前置查询阶段的runner）。旧测试16份中15份未改，另一份更新为新覆盖边界并加强状态检查。
- [程序集依赖](binary-cold-validation/task-items-dependencies.json) 仅含framework及自有模块，不依赖原版Engine/Game/Steam二进制。
- 修改前副本在 `.tooling/checkpoints/binary-cold-before/`；通过版本保存在 `.tooling/checkpoints/binary-cold-passed/`，对应 [检查点清单](binary-cold-validation/checkpoint-manifest.json)。

两个新组合场景实际从 cold Class getter 开始，经自有构造、完整原名称赋值，再进入 FinishPartial 的首NUL前缀重载、pending移除和两个索引增长。提供 own Read 时成功；缺少Read时在已完成注册之后Unsupported，全部既成副作用保留。其他测试覆盖槽顺序与链顺序不同、碰撞prepend、首NUL和高位字节、signed ID、cold→warm→stale、满容量／零容量以及numeric／构造失败。

## 下一个最小证据查询

目标是 **Engine `0x180284200`**，直接调用点为 `0x1802841DA`。仅需该地址的 `procedure_pseudo_code`、`procedure_assembly`、`procedure_callees`；连同会话检查、打开与关闭，仍是单会话8调用。重点确定增长后的capacity/count、slots及桶头如何迁移，以及零容量和溢出路径；不能从函数名或旧容器模块类比补全。

这批后续查询**尚未启动**，等待OW释放窗口。前置 [284160静态查询](BINARY-COLD-QUERY-RESULT.md) 已正常关闭并释放资源，原版Engine哈希前后一致。本实现阶段保留该证据及历史13文件哈希记录，不重复扫描原版。没有存档／云数据访问、额外工具安装、持久权限扩大、系统改动、commit或push；原生工厂population、具体构造／Read、记录miss loader及自动Allocate→Create→Finish仍未闭合。
