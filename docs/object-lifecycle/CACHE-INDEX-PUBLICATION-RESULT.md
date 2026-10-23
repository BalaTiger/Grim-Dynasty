# Cache 索引发布有界实现与验收

2026-10-20 UTC。本轮一次串行回归通过 **1209 条断言、25 组场景及 ItemQuestDemo**，其中新增 98 条断言、4 组场景；21 个模块套件共 1189 条断言，另有 Object ID 20 条。实际运行时间为 06:46:17.1657360Z–06:46:23.6018577Z，耗时 6.433 秒，使用已有 C# 4 编译器和 `/checked+`。[测试摘要](cache-closure-validation/test-summary.json)、[完整输出](cache-closure-validation/test-output.txt)、[验收记录](cache-closure-validation/acceptance-cache-index.json)

## 实现范围

[RecordCachePublicationAttempt.cs](../../src/task_items/RecordCachePublicationAttempt.cs) 从显式提供的“候选节点已挂链、count 已增加，旧 bucket endpoints 尚未包含候选”的自有状态开始，实现 `02E770 / 02EA80 / 02EB40` 的有界正常返回行为：

- 按完整 stored byte length 进行 hash、反向查重、唯一节点移位及 bucket endpoints 更新。
- 重复候选先摘链并减少 count；在有效独占存储、正常释放的契约下移除候选，不析构或 Dispose 其不透明 Table。没有按字符串长度猜测 native capacity 或 free 次数。
- load factor 严格大于阈值才增长；bucket 数小于 512 时乘 8，否则乘 2。支持最多一层增长、目标 bucket 数不超过 4096，沿真实 `01B4F0` reserve/reset/fill 调用链保留已有较大容量。
- rehash 每轮读取当前头，直到处理到先前保存的尾节点身份；即使旧尾刚因重复而被移除，仍正确终止。
- 正常完成时才给出 `Inserted`、`ReturnedNodeId` 和可用于现有查询 API 的完成快照。

构造函数建立的是自有显式快照，不是恢复的原版节点构造。`01F170 / 022860` 的分配、链接与字符串转移仍未知；整个 `05DEB0`、完整缓存发布调用方和 LoadFile miss 没有接通。普通 fallback 构造、archive 取数、原生 ClassInfo 填充、具体物品构造及 Read 也仍未完成。[维护指令证据](CACHE-MAINTENANCE-EVIDENCE.md)

## 部分修改后的调用契约

覆盖范围外的递归增长或 bucket 预算在唯一节点前缀已写入后、调用 `02EAE9 → 01B4F0` 之前停止，保留已有修改并返回 `Unsupported`。这属于实现覆盖边界，不宣称是原版的失败或回滚行为。

`RunOnce()` 缓存并返回同一首次结果，后续调用不重新执行。结果保留最初原因、停点和累计副作用标记；`RequiresResolution` 未解除时拒绝导出完成快照。入口本身已经有挂链/count 修改，因此 `MayHaveEffects` 始终为 true。

旧 [RecordCachePublicationPrefix.cs](../../src/task_items/RecordCachePublicationPrefix.cs) 也增加累计 `MayHaveEffects`、`RequiresResolution`、`FirstResult` 和 `FirstStopAddress`。重复调用的本次 `PrefixMutated=false` 不再能被理解为整次尝试无副作用。

[新增测试](../../src/task_items/RecordCachePublicationAttemptTests.cs) 覆盖阈值相等、重复候选在过载状态下直接返回、单层扩桶、保留容量、旧尾删除终止、两类部分修改后的 Unsupported、重复调用同一结果、旧前缀累计副作用，以及真实 CreatePartial/FinishPartial miss 仍停在未实现 loader、不会进入工厂/注册/Read。完成快照接入真实 LoadFile hit 的测试不构成冷加载恢复。

## 固定查询与故障处理

维护批次 `02EA80 / 02EB40` 完成 **11/11 calls**，正常 close、session 关闭、Job 清空和 sampler 退出均确认。06:35:06.9661813Z 的独立释放记录确认 54 个记录身份全部退出，分析进程及 runtime 文件均为 0。[批次摘要](rea-record-cache-maintenance/summary.json)、[释放核查](rea-record-cache-maintenance/resource-release.json)

节点批次 `01F170 / 022860` 发出 4 calls、完成 3 calls；第 4 次 `01F170` 伪码请求未取得函数 body。采样器因 `QueryMembers → QueryFullProcessImageName` 异常退出 1，控制器停止本轮。原批次没有正常 close 或 Job-empty 确认；06:38:40.9199487Z 的事后核查确认 22 个记录身份退出、分析进程与 runtime 文件为 0。事后释放不改写原失败结果。[失败诊断](CACHE-NODE-QUERY-FAILURE.md)、[原始摘要](rea-record-cache-node/summary.json)、[事后释放](rea-record-cache-node/resource-release.json)

已在实现和回归完成后复查错误、退出码、采样及守卫控制流。日志没有失败 PID、原始 Win32 错误码和失败查询阶段，不能证实启动/退出竞态或其他原因；最后有效样本的物理内存和 commit 余量也不支持低内存阈值触发。**根因未明，未实施猜测性修复，节点查询保持暂停，未重试。** 7 个保护实现文件保持原哈希；未放宽资源阈值或身份检查。[重试决策](cache-closure-validation/node-query-decision.json)

## 保存与边界

[检查点清单](../../.tooling/checkpoints/cache-index-complete/manifest.json) 保存代码、当前文档、原始查询日志、失败证据、自有编译产物和验收记录。保留上一个 233 文件检查点及本轮 61 文件修改前快照；独立复核无阻断问题。

HEAD 保持 `6dfdae419d1c92f7738eefe241937057d83134df`，未 commit/push。原版只作静态读取，本轮两批查询前后 Engine SHA256 均为 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`。未运行原游戏或原版 DLL，未访问玩家存档/Steam 云，未新增安装或持久权限更改。本轮没有被拒绝、待批准或结果不明的操作；剩余阻断是节点证据缺失及采样器根因未明。
