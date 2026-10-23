# Binary stale 查找、写回和副作用传播 — 2026-10-19

已在现有温路径之后接入静态证据完整的 **stale-hit 名称查找 → 先释放临时字符串 → 非 -1 ID 写回原缓存节点 → 继续数字 getter**，并将写回副作用传播到 CreatePartial。仅复用工程保存的证据；新增 Rea 调用 **0**，未启动 Ghidra 或重型索引。

全新 `.tooling/binary-stale-build/` 一次构建通过，耗时 **5.490 秒**。新增 [BinaryStaleGetterTests](../../src/task_items/BinaryStaleGetterTests.cs) **33 条断言、2 个组合场景**；重跑上轮全部 811 条，合计 **844 条断言、10 个组合场景，另有 1 个既有 ItemQuestDemo**。独立 ObjectId 的 20 条已计入总数。旧机 894/16 与完整离线 UCRT 验收未重跑。

实现与边界：

- [BinaryNameIdSnapshot](../../src/task_items/BinaryNameIdSnapshot.cs) 提供明确的 name→int32 节点，复用既有 `03C8E0` 完整字节 hash/桶段查找；不会将 `RecordCacheEntry.Table` 解释成 ID。query 只复制首 NUL 前缀，stored key 保留完整长度，包括其内嵌 NUL；无大小写、路径或 locale 转换。完整稳定链、桶数为正 2 的幂且原 mask=桶数−1 是调用方的覆盖前提，不是原生初始化的恢复结论。
- [BinaryWarmTable](../../src/task_items/BinaryWarmTable.cs) 只允许原命中节点的 CachedId 改变，地址、桶链和其他快照存储不变。临时字符串正常释放先于 ID 判定和写回。未找到名称或命中 ID=-1 不写旧值，但返回的 -1 仍进入数字字段查找，允许命中完整 `0xFFFFFFFFFFFFFFFF` 键。非 -1 的负 ID 按符号扩展写入 qword；本次写前不验证新 ID 能否索引名称池，未来读取缓存名字时才检查当次有效域。
- `CacheIdWritten`、`CacheIdChanged` 和写入前后值分开记录：即使写入相同值也报告发生过 store。getter 在进入 Binary 操作前持有效果记录，后续自己的操作失败仍保留已写事实；[CreatePartial](../../src/task_items/CreatePartialObject.cs) 将 getter 的 `MayHaveEffects` 纳入聚合。下游 Unsupported 不回滚已完成的写回、LoadFile 命中或其他效果。
- 原温路径不要求名称映射；未提供完整 name→ID 快照时，stale 仍在原覆盖边界 Unsupported。cold 指针 miss 始终停在已有边界，即使调用方提供名字表也不绕过插入。没有扩大到冷插入、parser、原生构造/Read 或自动分配生命周期。

两个新增组合场景从真实 CreatePartial 的 LoadFile 和 stale Class getter 开始：成功场景在更新后取得池内 Class，解锁后调用显式 own constructor，再赋完整 32 字节原名；另一个在缓存写回后遇到未知 numeric 状态，保留缓存 ID、加载计数和效果标志，停止在工厂之前。单项测试还验证随后查询转 warm、完整符号扩展键、首个匹配、原始高位字节/NUL、同值写入、负/未找到 ID，以及 buffer 失败和 Known null 后保留更新。

名称映射最多 4096 桶/节点、单 key 65536 字节、总 key 1 MiB。审查修复了适配器复制前的预算检查：先验证全部 key 的总量，再建立内部键副本，避免重复引用大 key 时先大量复制再拒绝。其余字符串池/buffer 预算保持前轮限制。共享可变缓存节点的所有访问须使用同一 table gate 或由调用方保证独占；不声明原生并发和异常/OOM 已恢复。manager **+0x10 allocator** 与 **+0x28 registration** 的两种锁边界继续分离。

证据和精确顺序见 [BINARY-STALE-EVIDENCE.md](BINARY-STALE-EVIDENCE.md)。验收包括 [76 项源码/产物/工具哈希与分项结果](binary-stale-validation/acceptance-binary-stale.json)、[构建输出](binary-stale-validation/build-output.txt)、[ObjectId 回归](binary-stale-validation/object-id-regression-output.txt)、[耗时](binary-stale-validation/build-run.json)、[依赖检查](binary-stale-validation/task-items-dependencies.json) 和 [一致性检查](binary-stale-validation/consistency-check.json)。独立代码复核在预算修复后未发现实质问题。旧 73 项清单除本轮预期修改的 4 文件外，其余 69 项哈希未变；复用的 13 份既有证据均已记录当前文件哈希，其中 9 份也与上轮检查点一致。

下一真实缺口仍是 cold 名称命中后的插入函数 **Engine `0x180284160`**，调用点 `0x180283915`。未来最小请求为该地址的 `procedure_pseudo_code`、`procedure_assembly`、`procedure_callees`，3 个分析调用、连同既有会话检查总上限 8 calls。没有可靠的该查询峰值内存证据，不能承诺一个 GiB 数；少量函数查询仍可能触发整个 Engine 导入/分析。**UE 等待系统余量期间保持未启动、未请求审批，无后台等待或自动重试。** 本轮 stale 已有证据足够，不因这个后续缺口留下未完成的 stale 更新。

修改前 4 文件备份在 `.tooling/checkpoints/binary-stale-before/`，通过版本保存于 `.tooling/checkpoints/binary-stale-passed/`，见 [检查点清单](binary-stale-validation/checkpoint-manifest.json)。这是增量工程与证据检查点，不是完整游戏。

本轮未读取/运行原游戏，未读取或修改玩家存档、Steam 云数据；未重新计算原版哈希，原样保留此前固定 13 文件的哈希记录及记录文件的校验值。没有安装、权限扩大、配置/系统改动、commit 或 push。Rea runner、两个项目 MCP 配置和 UCRT provider 门禁不变，无审批被拒绝、待批准或结果不明的动作。
