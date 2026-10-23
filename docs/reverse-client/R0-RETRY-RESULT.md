# R0 真实工具基线通过；继续 R1

2026-10-22 UTC。新生产监控经最小接入后，正式审批的单次 R0 查询于 **06:51:19.607–06:54:16.379** 完成：8/8 调用成功，错误列表为空。此次取得真实伪代码，不以安装或连接成功代替分析。旧失败与隔离测试记录完整保留；此结果不反推旧错误 5 的根因。

## 实际证据

- 已安装 Rea 6.5.0，经本地 stdio MCP `tools/call` 调用；分析后端 Ghidra 12.1.4，实际 JVM 堆 `-Xmx384M`。
- 固定输入 `D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll`，前后 SHA256 均为 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`。
- `procedure_pseudo_code`：地址 `0x180274ec0`；06:51:25.592 发出，06:54:14.282 完成；返回 1,071 字符函数体 `FUN_180274ec0`，包含字符串比较、子记录递归和工厂间接调用。跳表/间接跳转警告原样保留，不声称间接目标全部解析。
- 随后 `procedure_assembly`、`procedure_callees` 成功；正常 `close_binary` 后 `binary_session` 确认关闭，进程退出 0、未强停、Job 空与采样器退出均确认，运行临时目标目录已由正常关闭释放。

原始记录：[summary.json](r0-tool-baseline-terminal-retry/summary.json)、[progress.ndjson](r0-tool-baseline-terminal-retry/progress.ndjson)、[伪代码](r0-tool-baseline-terminal-retry/04-Class-factory-helper-0x180274ec0-pseudocode.json)、[汇编](r0-tool-baseline-terminal-retry/05-Class-factory-helper-0x180274ec0-assembly.json)、[直接调用](r0-tool-baseline-terminal-retry/06-Class-factory-helper-0x180274ec0-direct-callees.json)。

伪代码响应 SHA256：`c9db9099fea35f38867c4267328c79d4c14446bd98dea8411889bb3ac892648b`；evidence ID：`ev_805e96c1314cc529195bd2de8f5fed58a5e22cc801015a8a910e103aef5b803c`。总结 SHA256：`73ab36ba20f05200bf51426eae585ce2049058a1fe4e39e7819f2ad0e06dd877`。

## 接入与资源

生产 `ReaOwnedJob.cs` SHA256 为 `6b507ae86d9a3e05432d3bd13b0053ebe9fe17312dd3edba5939ef4a8c3d0e40`，与隔离候选算法相同；只恢复生产命名空间并调整说明。原源与原 runner 保存在 `.tooling/r0-monitor-production-integration/`。采样器、系统权限、策略及归属要求未改。新固定计划 `5da37b99…` 和执行 runner `7ef77e2b…` 的记录对应本次，不覆盖首次 R0。

仍使用 Rea 的 4.25 GiB 启动／2 GiB 停止门槛，没有采用 UE 门槛。91 次任务采样中，系统最少可用物理 4,216,000,512 B、最少提交余量 17,304,727,552 B；包含控制器和采样器的同时工作集采样峰值 1,238,827,008 B。它们是采样值，不保证捕获短命进程的连续峰值。本次未触发资源停止或监控失败。

## 下一步

R0 工具可用性通过，继续 R1 的两个已定位 Engine 入口：`GetInputDevice 0x180077850`、`WorldCamera::GetRayThroughImagePoint 0x180229700`。下一批单独固定计划、重新资源预检、正式审批、保存原始响应；不自动扩展调用树，不恢复旧 WASD 自绘路线。只有上述两函数也不能代表鼠标命中、点击移动、导航、UI 或场景链完整。

本阶段没有新增游戏实现或游戏验收，没有运行原游戏、访问真实存档、改系统设置、commit/push。原错误原因及 100 ms 等待是否在此真实运行中起决定作用仍未证明；当前成功事实与因果判断分开保留。
