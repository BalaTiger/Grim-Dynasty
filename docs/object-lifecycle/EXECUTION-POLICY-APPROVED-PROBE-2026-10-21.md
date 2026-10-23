# 正式许可下的只读策略查询

2026-10-21。本轮按父线程明确要求，通过 `exec_command` 的 `require_escalated` 正式审批机制申请单次沙箱外只读查询。工具成功执行，退出码 0；没有使用持久命令前缀授权。

## 精确动作和结果

```powershell
& 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' -NoProfile -NonInteractive -Command 'Get-ExecutionPolicy -List'
```

工作目录为 `C:\Users\zhuzi\Documents\Grim-Dynasty`。审批理由明确限定：只读查询，不运行构建，不更改注册表、ACL 或执行策略，不使用 ExecutionPolicy Bypass。完整请求与工具结果保存在 [probe.json](execution-policy-approved-probe-2026-10-21/probe.json)。

| Scope | ExecutionPolicy |
|---|---|
| MachinePolicy | Undefined |
| UserPolicy | Undefined |
| Process | Undefined |
| CurrentUser | RemoteSigned |
| LocalMachine | Undefined |

该列表与用户提供的普通 Windows PowerShell 结果一致。宿主路径与前次诊断相同；前次已测其 PowerShell 版本为 5.1.26100.9711、64 位。本轮严格只执行策略列表命令，没有附加身份或权限查询，也不据 `require_escalated` 一词声称进程获得 Windows 管理员身份。

## 能得出的结论

[前次任务内查询](EXECUTION-HOST-COMPARISON-2026-10-21.md)在注册表打开阶段发生 SecurityException；本次正式许可下，同一路径宿主的同一策略列表命令成功。这支持当前策略读取差异与受限执行环境有关。本轮没有通过改变任何策略、ACL、签名或文件来源标记使其成功。

此对照没有识别具体拒绝规则，也不能还原昨日损坏的 AuthorizationManager 日志，或证明其具有相同、唯一根因。上述结论已由独立代理复核。

## 执行授权与下一步

本次许可仅覆盖该只读查询，不授予构建、测试或安全配置变更权限。查询已经完成，没有待批准或结果不明的执行。

本轮没有新增源码实现，因此不申请重跑旧构建充当进度。[Engine 根模型设计](ENGINE-ROOT-MODEL-DESIGN-PENDING.md)仍为待实现、未编译、未测试状态。后续若实际需要构建，应根据当时待验证的改动另行提交精确动作，通过正式审批后执行；本次查询结果不能代替该审批。

既有证据与设计未改动。上一检查点 manifest SHA256：
`114910ddf5c738b3b33153feb82512a6d3f2a8dc4ca22ab613ca4d14e9b4a9f7`。

本轮只有 1 次正式许可只读策略查询；构建、测试、安全设置修改、原版读取/运行、存档访问及 commit/push 均为零。

