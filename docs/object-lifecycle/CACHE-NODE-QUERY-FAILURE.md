# Cache 节点固定查询失败诊断

日期：2026-10-20（UTC）。本页仅整理已保存的日志与守卫源码；未重试查询、启动后端、编译或修改守卫。

## 已确认的批次结果

固定节点批次原定 11 calls，目标为 `0x18001F170`、`0x180022860`。本次发出 4 calls、完成前 3 calls；第 4 call 为 `01F170` 伪码请求，未得到函数 body，第二个目标没有发出计划分析请求。[计划](../../tools/static_analysis/rea-record-cache-node-plan.json)、[进度](rea-record-cache-node/progress.ndjson)、[汇总](rea-record-cache-node/summary.json)

- 06:36:14.742Z 发出第 4 call；06:36:48.348Z 报 `Rea exited (0)`。采样器退出码为 1，标准错误报告 `QueryMembers` 调用中的 `QueryFullProcessImageName` 异常。
- `process_exit_confirmed`、`sampler_exit_confirmed` 均为 true；但 `close_acknowledged`、`closed_session_verified`、`job_empty_confirmed` 均为 false，`forced_termination` 为 true。进程退出码 0 不代表正常完成 MCP close 或查询成功。
- 采样器错误文本没有保留失败 PID、Win32 `NativeErrorCode`、创建身份或失败的前／后成员查询阶段，无法从本次记录定位具体进程及错误类别。[采样器错误](rea-record-cache-node/owned-memory-sampler-stderr.txt)
- 汇总中的 Engine 前后 SHA256 相同：`4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`。

## 现有退出处理与尚未证明的原因

[ReaOwnedJob.cs](../../tools/static_analysis/ReaOwnedJob.cs:137) 的 `Probe` 先用同一进程 handle 读取创建时间和明确 Job 成员关系，然后查询映像路径。映像查询抛出 Win32 异常时，仅在**同一 handle 的零等待已确认退出**后，才返回带错误码的 exited 诊断；否则原样抛出。`QueryMembers` 只将带 `OpenProcess` 标记的错误 87 视作枚举期间进程消失，不吞掉其他错误。所审源码 SHA256 为 `aaa8b1a58c16587457b2b3d015d0209ff9546a11d2c3350fb37b5648a77fee12`。

按该源码控制流和仍为 `QueryFullProcessImageName` 的异常名推断，本次失败时退出检查未确认 handle 已 signaled，因而没有进入允许缺失映像路径的 exited 分支。这不是“所有进程退出竞态均已覆盖”的反证：先前处理只覆盖映像查询失败后已经确认退出的情况。

最后有效采样为 06:36:45.7121595Z。末段 Job 成员数从 4 增至 10、再至 24，记录了 Java 子进程下新建的多对 `decompile.exe`／`conhost.exe`。新进程启动期间的查询窗口、终止中但尚未 signaled 的窗口，以及其他 Win32 查询错误，均是待区分的候选解释；**没有失败 PID／错误码，不能确定其中任何一个，更不能推定为权限问题或直接忽略错误**。Rea 退出与 sampler 退出的日志到达顺序也不足以单独证明底层因果顺序。[采样](rea-record-cache-node/owned-memory.ndjson)

## 资源与计量边界

本次 21 条有效采样已观察到主 Java `-Xmx384M`。最低可用物理内存为 13,042,905,088 bytes（约 12.15 GiB），最低 commit headroom 为 28,820,926,464 bytes（约 26.84 GiB），均远高于 2 GiB 停止线；最后有效样本的 `stop_requested`、`controller_exit_observed` 均为 false。已有记录不支持“低内存阈值触发停止”的解释。[采样汇总](rea-record-cache-node/owned-memory-summary.json)

采样峰值不是任务占用的硬上界。06:36:44.046 样本前／后成员查询为 4／10，最后样本为 20／24；后一次查询新增的成员没有该轮 counters。`member_identities_without_counters` 只检查该轮第一次成员集合，空数组不证明末尾全部成员均已计量。短命进程及失败前最后一段活动仍可能遗漏。

## 事后释放核查

06:38:40.9199487Z 的独立 [resource-release.json](rea-record-cache-node/resource-release.json) 确认：记录的 22 个进程身份均已退出，未见 PID 重用；活动分析进程（包括 Java）为 0，剩余 runtime 文件为 0。仅清理本轮 `analysis-runtime/rea/rea-ghidra-2e647eaa8177955240fad76ee0decc78` 目录，记录 49 files。

这份事后记录解决了后续资源占用核查，不将原批次的 `job_empty_confirmed=false` 改写为正常关闭，也不补出缺失函数 body。

## 下一步诊断建议

下一次获准执行前，先增强失败证据：在同一 handle 上保留失败 PID、创建时间、已读 Job 成员关系、API 名、原始 Win32 错误码和退出等待结果，并标明发生于哪次 `QueryMembers`。记录后继续保守抛错和专用 Job 清理；不得将未确认退出的进程视为已退出、跳过活进程身份检查或凭猜测重试。该建议尚未实施，本轮未改守卫。
