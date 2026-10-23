# 监控失败诊断增强与受控验证

2026-10-20 UTC。已完成诊断增强及轻量测试，**原节点查询的 `QueryFullProcessImageName` 故障仍未复现、根因仍未知，节点查询保持暂停，未重试。** 本阶段没有启动 Rea/Java 或读取原版文件。此前缓存索引实现的 **1209 条断言、25 组场景及 Demo** 验收不变，见[索引实现结果](CACHE-INDEX-PUBLICATION-RESULT.md)。

## 改动

[ReaOwnedJob.cs](../../tools/static_analysis/ReaOwnedJob.cs) 在原 Win32 异常的固定字段中保留失败 PID、同一 handle 已读到的 UTC 创建时间、Job 成员关系、API 步骤和原始错误码。映像查询失败后的退出检查结果单独记录；若退出检查也失败，同时保留映像错误和 wait 错误。`QueryMembers` 补充成员索引和集合大小，未读到的事实保持未知。

没有新增重试、重新打开 PID、生产故障开关或接纳条件。只有原先的 `OpenProcess` 错误 87 可按进程已消失处理；QFPI 错误 87、5 或未知错误仍向外抛出。只有同一 handle 已确认退出，才允许返回缺少映像路径的已退出诊断。Job 限制、控制器 watchdog、停止路径和清理逻辑保持原行为。

[采样器](../../tools/static_analysis/measure_job_process_memory.ps1) 标记初始/末尾成员查询、进程 counters 等阶段；[白名单投影](../../tools/static_analysis/rea_sampler_failure.ps1) 将失败写为 `REA_SAMPLER_FAILURE` stderr 记录和独立的 `owned-memory.ndjson.failure.json`。记录不进入有效内存样本流，不包含环境变量、命令行、任意异常 Message/StackTrace/Data 或 executable path。创建时间 ticks 作为十进制字符串保存，避免 JavaScript 数字精度损失。

诊断 sidecar 使用 `CreateNew`，不能覆盖首份证据。stderr 损坏时仍尝试 sidecar；诊断写入失败不替换原异常，采样器继续原样重抛并进入既有 Job 清理。资源阈值和 controller 实现未修改。

## 实际验证

| 验证 | 实际结果 | 范围 |
| --- | --- | --- |
| [既有 Windows Job 回归](monitor-diagnostic-validation/owned-job-regression/result.json) | 115 条断言通过，07:00:46–07:00:55 | 修改后的生产守卫，实际自有 Node/PowerShell 进程及 Job；身份、控制器退出、嵌套 Job、清理 |
| [投影/脱敏测试 v2](monitor-diagnostic-validation/projection-v2/result.json) | 30 条断言通过 | 包装异常、双错误、未知字段、ticks 精度、拒覆盖、损坏 stderr；不启动子进程 |
| [控制器 mock](monitor-diagnostic-validation/controller-tests.txt) | 79 tests passed、0 failed | 新增 3 项：首样本前、分析中和 finalization 的结构化失败；内存充足仍失败，不伪造样本/Job-empty |
| [受控 Native 诊断](monitor-diagnostic-validation/native-diagnostics/result.json) | 296 条断言通过，07:04:28–07:04:30 | 生产源改名副本基线与 4 种固定故障副本，实际自有 Node 进程 |
| [真实采样器门控集成](../../.tooling/owned-start-gate-1c7aa512-69e9-4de3-9ba6-e7cf25d58474/result.json) | passed；Job empty、sampler exit 0 | 门始终未释放，未导入 Rea；root exit 92 是专用停止命令的预期结果 |

Native 诊断的固定副本注入 QFPI 5、87、123456 及映像 5 后 wait 6。测试保存派生源码与哈希，生产源码无注入 hook。活进程仍拒绝未知身份；已退出进程仅凭同一 handle 的退出证明允许缺失映像。Job 释放后确认全部 13 个 owned roots 已退出：其中两个仍存活至 Job 关闭并被终止，其余先受控退出。独立 control 在此前仍存活，最终由其原始保留 handle 清理。

八轮未注入的真实短命进程测试观察到 **16 次 normal、8 次 exited、0 次 OpenProcess-87 竞态**。没有观察到此前真实 QFPI 故障；不存在“已证明具体故障修复”的结论。注入测试只验证错误记录和原有保守路径，不能证明此前故障原因，也不授权为了采集日志启动 Rea。早期投影版本的 28 条通过记录保留在 `projection/`，当前验收以 v2 的 30 条为准。

控制器在 sampler 已退出后仍报告失败并调用专用停止接口；该接口不会向已退出 sampler 的管道伪造发送 stop frame。诊断不能把这类失败改为正常 close。

## 释放与重试决定

[07:05:20 的只读核查](monitor-diagnostic-validation/resource-release.json) 确认活动 Java/decompile 为 0，工程 Rea runtime 文件为 0。所有实际测试均已结束；没有待确认的运行进程。最初受限 CIM 检查不可用，其输出未用作零进程证明；随后单次批准的只读核查成功取得上述记录，没有审批拒绝。

原节点批次仍是发出 4 calls、完成 3 calls、无函数 body、未正常关闭的失败记录；事后释放成功没有改写原结果。诊断增强没有补回当时缺失的 PID 或 Win32 错误码，也没有解决 `01F170 / 022860` 的节点创建证据缺口。[原失败诊断](CACHE-NODE-QUERY-FAILURE.md)

[验收与决定](monitor-diagnostic-validation/acceptance-monitor-diagnostics.json) 明确记录：`root_cause_proven=false`、`behavior_repair_applied=false`、`node_query_retry_performed=false`。现有证据不满足此前“具体修复经验证后才可重试”的条件，继续暂停。

[新检查点](../../.tooling/checkpoints/monitor-diagnostic-complete/manifest.json) 包含诊断源码、受控测试副本、日志和先前索引阶段交付物。保留 290 文件索引检查点及 14 文件诊断前快照。未 commit/push、未新增安装、未改持久权限、未访问玩家存档/Steam 云。
