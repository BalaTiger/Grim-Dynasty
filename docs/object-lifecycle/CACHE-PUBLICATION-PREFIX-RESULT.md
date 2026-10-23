# 缓存索引前缀实现与验收

2026-10-20。新增 [RecordCachePublicationPrefix.cs](../../src/task_items/RecordCachePublicationPrefix.cs)，恢复 `Engine!02E770` 在两个必经未知调用之前的行为。**它不是完整缓存发布：有效输入也返回 `Unsupported`，原生返回节点和 inserted 标志始终未知。** [RecordLoadFile](../../src/task_items/RecordLoadFile.cs) 的 miss 仍未接入。

入口是明确供给的自有快照：候选节点已经位于全局链首，计数已经包含它，桶端点只覆盖旧节点。构造快照不代表已经实现 `05DEB0`、原生字符串转移或 LoadFile 的前序步骤；节点 ID 只是诊断身份。调用方须提供稳定完整数据和独占访问。

实现覆盖：

- 对完整 stored bytes 使用 unsigned FNV64；包含内嵌 NUL，不做 C-string 截断或名称规范化。
- 从桶尾逆向查重，包含桶首。重复时保留候选、旧节点、计数及端点，在 `02EB40` 前停止；匹配节点仅作为观察结果，不能冒充尚未写出的原生返回值。
- 唯一键把候选移到旧桶首之前；空桶移到全局尾部。更新桶首及必要的桶尾后，在 `02EA80` 前停止。计数不再增加。
- 保留可检查的双向链和端点；诊断数组隔离复制。前缀只执行一次，不能把中间状态继续当成完成的缓存。

覆盖边界沿用现有有界缓存：桶数为 1–4096 的二次幂，包含候选最多 4096 节点，单键最多 65536 字节，总键最多 1 MiB。仅旧节点要求同桶连续，不能拿暂时挂链的候选一起验证完整缓存布局。损坏或并发状态、原生 allocator/EH、节点生命周期及维护 helper 均未恢复。

## 静态查询与资源释放

[固定查询](rea-record-cache-publication/summary.json) 完成 **11/11 calls**，仅分析 `05DEB0`、`02E770` 的 pseudo/assembly/direct-callees，运行区间为 `06:17:19–06:18:48 UTC`。沿用 384MiB Java 堆、双资源阈值与归属 Job 守卫，实时堆检查通过，没有强制终止。计入 controller/sampler 后，观测私有提交峰值为 1,215,696,896 字节、工作集峰值为 1,313,579,008 字节；这是该批观测值，不是未来函数资源上界。

会话 close/final-session、Job 为空、后端和 sampler 退出均确认。[释放复核](rea-record-cache-publication/resource-release.json) 在 `06:19:02 UTC` 确认 66 个已记录进程身份均退出、分析进程和临时运行文件均为零。读取目标为 `D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll`，查询前后 SHA256 均为 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`；没有执行原版。

精确指令点、证据 ID、来源哈希和未闭合依赖见 [CACHE-PUBLICATION-EVIDENCE.md](CACHE-PUBLICATION-EVIDENCE.md)。`05DEB0` 还依赖 `01F170/022860`，不能凭 fresh string 初始化推断分配大小或 copy/move。`02E770` 的两条返回值写入都在未知 helper 之后，不能以相似的 uint 注册容器代替。

## 实际测试

[测试源码](../../src/task_items/RecordCachePublicationPrefixTests.cs) 新增 **69 条断言、3 组场景**：跨桶与相邻重链、空桶移尾、正向 lookup 与逆向重复匹配的差别、嵌 NUL/高位字节/长度差异、未知返回值、保留副作用、快照隔离及预算边界。

一次串行回归于 `06:25:31–06:25:37 UTC` 完成，耗时 **5.857 秒**，合计 **1111 条断言、21 组场景，另有 ItemQuestDemo 通过**。其中 20 套模块测试共 1091 条，另有 ObjectId 20 条。使用现有 .NET C#4 编译器和 `/checked+`，仅执行自有程序集；构建输出独立写入 `.tooling/cache-prefix-build`，没有覆盖上一检查点。

原始记录：[test-output.txt](cache-prefix-validation/test-output.txt)、[test-summary.json](cache-prefix-validation/test-summary.json)、[assembly-references.json](cache-prefix-validation/assembly-references.json)。独立审查与验收汇总由同目录的 `independent-review.json`、`acceptance-cache-prefix.json` 固定，副本清单为 `.tooling/checkpoints/cache-prefix-complete/manifest.json`。

## 明确下一步

[next-plans.json](cache-prefix-validation/next-plans.json) 保存两份**分别为 11 calls、尚未注册或执行**的固定数据计划：

1. 优先 [缓存维护计划](../../tools/static_analysis/rea-record-cache-maintenance-plan.json)：只查 `02EA80/02EB40`，才能判断当前前缀后的维护与重复候选处理。
2. 独立的 [节点创建计划](../../tools/static_analysis/rea-record-cache-node-plan.json)：只查 `01F170/022860`，闭合分配、链接初始化及字符串转移。

两份计划不能自动合并或追踪 callees。执行仍须明确的工作窗口与范围、注册前复核、即时资源预检和逐次审批。本轮没有新的安装或持久权限变更，也没有触碰玩家存档、Steam 云、系统安全设置、commit 或 push。完整 LoadFile miss、`280E20` 取数、普通 fallback ctor、ClassInfo 注册、具体物品构造与原生 `Read` 仍未闭合。
