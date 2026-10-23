# R0 当前工具验证与阻塞

**最新状态（06:54 UTC）：生产最小差异接入后，单次真实 R0 查询 8/8 通过并正常释放后端。见 [R0-RETRY-RESULT.md](R0-RETRY-RESULT.md)。已进入 R1；下文保留此前失败和诊断的历史事实，不将旧失败改写为成功。**

2026-10-22 UTC。此记录描述真实执行，不是安装状态推断。R0 尚未通过；R1–R3 尚无新客户端验收。

06:39 更新：监控隔离候选已完成 36 断言／8 组验证，4/4 自有夹具进程退出；生产监控未替换，尚未发出新的 Rea 查询。首轮夹具失败、修正依据、实际结果及精确续接点见 [R0-MONITOR-REPAIR.md](R0-MONITOR-REPAIR.md)。这不改变下文真实 R0 失败或根因未证实的结论。

新里程碑见 [REVERSE-CLIENT-MILESTONES.md](../REVERSE-CLIENT-MILESTONES.md)：R0 实际 Rea 证据；R1 原版鼠标输入、移动、相机和场景路径；R2 攻击、死亡、拾取、装备与 HUD；R3 初步可用程序并立即停止。旧 M1/M2/M3 保留为工具、条件模型与原创验证原型。

## 本次真实执行

- 使用已安装 Rea 6.5.0 的本地 stdio MCP。当前会话没有直接注入 Rea 工具，因此通过项目客户端发出真实 `tools/call`，不是伪造或模拟工具输出。
- 正式批准运行 `D:\NodeJS\node.exe tools/static_analysis/rea_native_registration.mjs --r0-tool-baseline`。计划固定 8 次调用，只查询 Engine `0x180274ec0`，目的是验证当前工具，不将重复基线算新游戏行为。
- 固定目标 `D:\Steam\steamapps\common\Grim Dawn\x64\Engine.dll`；前后 SHA256 均为 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`。
- 06:00:40.272–06:00:57.199：完成 `binary_session`、`open_binary`、`binary_session` 三次；第 4 次 `procedure_pseudo_code` 在 06:00:56.811 发出，未返回伪代码，错误为 `Rea exited (0)`。
- Rea 身份、版本、Ghidra 绑定和 schema 核对通过；不等于 Ghidra 反编译通过。未观察到实际分析 JVM 堆，不能说已经启动完整反编译。
- 原始请求、实际响应、时间、错误和哈希见 [summary.json](r0-tool-baseline/summary.json)、[progress.ndjson](r0-tool-baseline/progress.ndjson)、[tool-catalog-selected.json](r0-tool-baseline/tool-catalog-selected.json)。

## 已确定错误与未知

[原始诊断](r0-tool-baseline/owned-memory.ndjson.failure.json)记录：采样器 `before-inventory-query` 阶段，`QueryFullProcessImageName` 返回 Win32 5；PID 24608、创建时间 `06:00:56.7458381Z`，同一 handle 已核实属于本任务 Job，随后零等待未观察到退出信号。没有取得该子进程映像，不能称它为 Java，也不能由 PID 猜测身份。采样器失败触发既有 Job 清理。

启动物理可用 5,402,648,576 bytes，运行采样最低 4,959,277,056 bytes；不是 2 GiB 下限触发。配置仍是单后端、384 MiB 堆、启动物理/提交余量各至少 4.25 GiB，未降低资源门槛或关闭监控。

源码请求了 QFPI 所需的 `PROCESS_QUERY_LIMITED_INFORMATION`，且使用同一 SafeHandle。微软文档列明该访问权可用于此调用：[QueryFullProcessImageName](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-queryfullprocessimagenamea)。当前证据不能确定是子进程初始化/退出时序还是其他访问限制；不修改 ACL、执行策略或原工具权限，也不把等待超时解释成进程完全正常运行。

06:01:41 的批准只读查询确认本次已记录进程及 Java/decompile 不再存在，见 [resource-release.json](r0-tool-baseline/resource-release.json)。生成的临时目标副本与会话描述已在身份/路径核验后清理，见 [runtime-cleanup.json](r0-tool-baseline/runtime-cleanup.json)。原始 DLL 未改变。此事后释放不改写“未正常 close、当时 Job-empty 未确认”的失败结果。

## 当前下一步

有界自有短命进程实验已完成，见下节；没有取得具体可验证修复，保留阻塞。不将后来查询成功回填为先前身份，不放宽生产监控，也不凭改变查询目标重启后端。

并行准备 R1 的有界导出地址定位：`ControllerPlayer::HandleActionFromMouse`、`NavManager::FindPath` 与已知路径参数入口；现有导入符号不等于已证实调用链，不恢复 WASD 原型路线。未运行游戏构建或游戏测试，没有新增游戏能力结论。

## 有界自有进程诊断结果

06:13:05.999–06:13:08.496，经正式批准编译并执行独立诊断：8 个固定 Node 子进程串行创建，4 个自然退出、4 个受控自有 Job 终止；100 次观察，8/8 退出已确认。结果见 [result.json](../../.tooling/r0-qfpi-lifecycle-diagnostic-20261022/result.json)、[runner-result.json](../../.tooling/r0-qfpi-lifecycle-diagnostic-20261022/runner-result.json) 和原始 [observations.ndjson](../../.tooling/r0-qfpi-lifecycle-diagnostic-20261022/observations.ndjson)。

16 次映像查询错误均为 Win32 31，且出现在已收到退出信号时；**未复现原失败中“错误 5 且随后仍未收到退出信号”的组合**。这不证明原失败是退出竞态，也未证明更改等待时间能修复。生产 `ReaOwnedJob.cs` 和采样器前后哈希保持一致，没有身份兜底或权限变化。此为工具诊断，不能计入游戏功能测试。

首次诊断脚本因 PowerShell 5.1 严格模式中 `$args` 未定义而在编译前退出。修正该入口读取后才完成上述唯一实验；没有重跑实验或覆盖结果。首次错误见 [diagnostic-runner-preflight-failure.json](r0-tool-baseline/diagnostic-runner-preflight-failure.json)。这不是正式审批拒绝，也不是原监控故障修复。

06:14:01.380 的 [资源复核](r0-tool-baseline/resource-release-after-diagnostic.json) 确认 Rea/Java/decompile/自有诊断进程为零，无 `rea-ghidra-*` 临时目录；物理可用 4,647,395,328 bytes、提交余量 19,704,360,960 bytes。未发出第二次伪代码请求，释放同机重型时段给 OW。

六个客户端入口已由小范围元数据查询定位，详见 [R1-ENTRY-MAP.md](R1-ENTRY-MAP.md)。下一批仅 Engine `077850`、`229700` 的固定计划已准备为 `tools/static_analysis/rea-r1-engine-input-plan.json`，**未注册为可运行 batch、未执行**。它必须等待监控故障的具体解决与验证，以及正式执行审批和可用资源时段。当前最小技术缺口是取得失败子进程的可靠生命周期/身份信息以区分原因；本轮自有 Node 样本不能替代该事实。未继续 PE 函数体取证，未恢复旧构造链或 WASD 路线。
