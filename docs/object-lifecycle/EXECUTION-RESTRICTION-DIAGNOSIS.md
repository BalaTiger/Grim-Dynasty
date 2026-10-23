# 执行限制：只读诊断结果

2026-10-20。本轮未运行被拒脚本、替代构建或测试；未修改执行策略、文件来源标记、签名、证书、注册表权限或其他安全设置。

## 结论

**历史 AuthorizationManager 拒绝的精确根因仍未知。** 当前可确认的限制是：原调用宿主 Windows PowerShell 查询执行策略时发生注册表访问 `SecurityException`；单独读取相关 HKCU 执行策略键的属性也被拒绝。两者是相关线索，尚不能证明就是历史拒绝的同一原因。

脚本当前未签名，但没有 `Zone.Identifier`。当前 PowerShell 7 的 `RemoteSigned` 来自 Codex 自带运行时的配置文件。它不是原 Windows PowerShell 的有效策略，不能用来断言原命令本应获准，也不能单凭未签名断言原命令必定被拒。

## 原动作与原始证据

原动作于 `2026-10-20T14:17:47.261Z` 开始、`14:17:47.578Z` 结束，退出码 1：

```text
C:/Windows/System32/WindowsPowerShell/v1.0/powershell.exe
  -NoProfile -NonInteractive
  -File C:\Users\zhuzi\Documents\Grim-Dynasty\src\task_items\build_class_registration.ps1
  -OutputDirectory C:\Users\zhuzi\Documents\Grim-Dynasty\.tooling\class-registration-helpers-build
```

状态为**已拒绝**，不是等待批准或执行结果不明。原运行器在该退出码后停止，未进入其后续整模块编译。本轮没有重试。

| 原文件 | 字节数 / SHA256 |
|---|---|
| [targeted-run.json](class-registration-helpers-validation/targeted-run.json) | `3e2a6f9bf5eede3458691d56af540ca781b6c40ee872a653d682fdb47cf39331` |
| [完整原 stderr](class-registration-helpers-validation/targeted-tests-stderr.log) | 185 B / `7cfea845efe07e3250aec8115415154ffa192d6f0b5f2d559a96ce5d4897d25f` |
| [原 stdout](class-registration-helpers-validation/targeted-tests-stdout.log) | 0 B |

已读取 stderr 全文并保留原件。其中文包含损坏的替换字符，不能恢复原始中文。可辨认字段为 `AuthorizationManager`、`SecurityError`、`ParentContainsErrorRecordException`、`FullyQualifiedErrorId : UnauthorizedAccess`。这些字段不足以识别具体策略、签名、来源标记或注册表根因。本轮不会把它改写成某条更具体的错误。

历史另一次 direct-csc 记录不解释或解除这次拒绝，也不构成本轮替代执行的授权。这是保存的 PowerShell 运行时拒绝；现有证据不能将其归类为工具自动审批系统的拒绝。

## 当前只读观测

以下为本轮工具输出的结构化转录；不是重新运行原动作的日志。机器状态是否与历史执行瞬间一致，尚未证明。详见 [observations.json](execution-restriction-diagnosis/observations.json)。

| 查询对象 | 实际结果 | 能证明的范围 |
|---|---|---|
| `src/task_items/build_class_registration.ps1` | 1166 B；SHA256 `a866d77d7ba3a3a1c193815135f0d3d90fa8547d985c541c3bb08f78d9982dc5`；修改时间 `2026-10-20T14:16:18.9420723Z` | 与原运行记录的脚本哈希相同 |
| `Get-AuthenticodeSignature` | `NotSigned` / `None`；无签名者 | 仅证明当前无签名。其通用 StatusMessage 不是有效执行策略测试 |
| 脚本 ADS 枚举 | 只有 `:$DATA`，1166 B；无 `Zone.Identifier` | 当前脚本没有该来源标记；不证明历史状态或其他来源判定 |
| 当前宿主 | PowerShell Core 7.6.5；有效策略 `RemoteSigned` | 只适用于此次 Core 宿主 |
| Core `Get-ExecutionPolicy -List` | MachinePolicy / UserPolicy / Process / CurrentUser 均 `Undefined`；LocalMachine `RemoteSigned` | 没有从该查询发现 Core 的组策略覆盖 |
| Core 配置来源 | `$PSHOME\powershell.config.json` 的 `Microsoft.PowerShell:ExecutionPolicy` 为 `RemoteSigned`；未见 `ScriptExecution` 覆盖；用户配置文件不存在 | 已定位本次 Core 策略的配置来源，未修改配置 |
| 原宿主的只读策略查询 | `powershell.exe -NoProfile -NonInteractive -Command` 内的 `Get-ExecutionPolicy` 和 `Get-ExecutionPolicy -List` 均因注册表权限抛 `System.Security.SecurityException`；退出码 1 | **未取得 Windows PowerShell 的有效策略或各作用域值** |
| HKCU 原宿主策略键属性 | `Requested registry access is not allowed.` | 属性未能读取；不能把失败后显示的空值当作“不存在” |
| 同键 `Get-Acl` | 未取得可用描述符；输出 Owner/Protected 为 null、Rules 为空 | 不能据此称“无 ACL”、定位某条拒绝规则或识别所有者 |

Core 配置完整路径：

```text
C:\Users\zhuzi\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\powershell.config.json
```

不可读取属性的键：

```text
HKEY_CURRENT_USER\SOFTWARE\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell
```

只读检查还得到：

- `HKLM` 与 `HKCU` 下 `SOFTWARE\Policies\Microsoft\Windows\PowerShell` 均不存在。
- `HKLM` 与 `HKCU` 下 `SOFTWARE\Policies\Microsoft\PowerShellCore` 均不存在。
- `HKLM\SOFTWARE\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell` 存在；本轮查询的 `ExecutionPolicy`、`EnableScripts`、`UseWindowsPowerShellPolicySetting` 未返回值。
- Core 当前进程及用户、机器环境中的 `PSExecutionPolicyPreference` 未返回值。
- 原 `powershell.exe` 文件版本为 `10.0.26100.8875`；这是可执行文件版本，不是由此推定的 PowerShell 语言版本或有效策略。

上述特定路径未发现配置性组策略证据；**不等于证明机器未受组织管理**。未取得键权限描述，未检查广泛的组织、安全或设备管理数据，因而不能区分键 ACL、执行宿主隔离、管理产品或其他限制来源。没有通过其他注册表 API、换身份或权限提升重试被拒的数据读取。

## 官方说明与因果边界

Microsoft 的 [Windows PowerShell 执行策略说明](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_execution_policies?view=powershell-5.1) 说明了注册表作用域、组策略优先级以及 AllSigned / RemoteSigned 的差别；该文也列出来源区域验证导致通用 AuthorizationManager 失败的情况。这只说明错误名称本身不唯一，不是本机故障诊断。

[PowerShell 7 配置说明](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_powershell_config?view=powershell-7.6) 说明 `$PSHOME` 与用户目录中的 `powershell.config.json`、执行策略键及配置优先级。这支持本轮对 Core 配置来源的解释，不能补齐失败的 Windows PowerShell 查询。

## 最小合规后续步骤与审批边界

目前没有证据支持执行 Unblock-File、降低执行策略或改用另一构建入口。最小后续步骤是由执行环境所有者确认**上述确切 HKCU 键在原 Windows PowerShell 宿主中的读取权限来源**，区分宿主隔离与机器自身权限/管理限制，取得该宿主的有效策略后再决定处理办法。可供所有者比对的只读目标已明确；本轮没有自行换宿主权限、换身份或绕过隔离。

- 本轮只读诊断和文档整理已经获得授权并完成，无额外审批正在等待。
- 如需修复注册表 ACL、宿主隔离规则、组织策略、签名信任或其他持久设置，必须先提出具体变更和理由，取得用户及适用管理者审批。当前原因未明，尚无可提交的确定修复动作。
- 如原因后来确认为必须签名，应遵循该环境认可的签名流程；当前不能把“补签名”称为已证明的修复，也不会创建或安装自签名信任。
- 构建脚本或等效编译执行继续暂停。诊断没有解除已明确的禁令；之后恢复必须获得明确授权，并符合实际执行环境限制。

## 可独立交付的设计

[ENGINE-ROOT-MODEL-DESIGN-PENDING.md](ENGINE-ROOT-MODEL-DESIGN-PENDING.md) 将已封存的 Engine writer/helper 合同整理为待实现接口、写入顺序、部分状态、依赖失败传播及计划 fixture。它不包含实现或已通过的测试结果。原生具体异常类型、运行时根图、字符串内容及正式样本兼容仍未验证。

本轮原版二进制读取、游戏/Rea/Java 启动、构建、测试、策略变更、存档访问及 commit/push 均为零。
