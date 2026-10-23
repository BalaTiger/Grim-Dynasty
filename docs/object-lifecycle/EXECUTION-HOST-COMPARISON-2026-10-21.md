# 用户窗口与任务宿主：只读对照

2026-10-21。依据用户新增的普通 Windows PowerShell 策略列表，进行一次有界复核。未重试构建、换构建宿主、复制被拒脚本或修改安全设置。旧诊断和待实现设计保持原样。

## 当前结论

**已确认两处的策略读取结果不同；尚未证明历史拒绝由某条具体的执行环境限制导致。** 用户普通窗口能返回 `CurrentUser=RemoteSigned`；任务启动的原路径 Windows PowerShell 仍在打开注册表键时失败。当前错误发生在策略读取阶段，不能将它改写成签名不合格或策略值为 Restricted/AllSigned。

| 对照项 | 用户提供的普通窗口 | 当前任务侧实测 |
|---|---|---|
| 来源 | 可信转述 `Sentinel_0083caba18888191b7c4c68e065a6fe4` | 本轮只读命令输出 |
| 账号/SID | 未提供 | `DESKTOP-9RQJ5OB\\zhuzi` / `S-1-5-21-1110971053-2661352112-2715398133-1001` |
| 实际路径 | 未提供；提示符目录不等于可执行文件路径 | `C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe` |
| PowerShell 版本 | 未提供 | `5.1.26100.9711`，Desktop，64 位 |
| 可执行文件版本 | 未提供 | `10.0.26100.8875`，495616 B；与 PowerShell 语言版本分别记录 |
| 令牌 | 未提供 | 任务命令链的 `whoami /groups` 显示 `Mandatory Label\Low Mandatory Level`，SID `S-1-16-4096`；未取得完整限制 SID 列表 |
| 策略列表 | MachinePolicy/UserPolicy/Process/LocalMachine 均 Undefined；CurrentUser RemoteSigned | 查询抛 SecurityException，未取得各作用域值 |
| 单独有效策略查询 | 未提供；列表支持该会话为 RemoteSigned 的推断 | 查询抛 SecurityException，结果未知 |
| HKCU 策略键 | 本次未提供直接属性读取 | 键可见，读取属性被拒绝 |
| HKLM 策略键 | 本次未提供直接属性读取 | 键可见，所查询 ExecutionPolicy 返回 null |

任务默认 Core 宿主仍为 `C:\Users\zhuzi\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe`，版本 7.6.5、64 位。它与本轮 Windows PowerShell 子进程报告相同账号 SID。旧记录中 Core 配置文件的 LocalMachine RemoteSigned、用户窗口的 CurrentUser RemoteSigned、任务 Windows PowerShell 的读取错误是三项不同观测。

设备 connected/attached/authorized 是父线程提供的连接状态，不能推出相同令牌、注册表访问权或构建执行许可。

## 错误定位与记录

已保存 [完整解码工具输出](execution-host-comparison-2026-10-21/windows-powershell-query.tool-output.json) 和 [结构化对照](execution-host-comparison-2026-10-21/host-comparison.json)。本轮显式使用 UTF-8 输出，保留了未损坏的中文异常和调用栈；没有改动旧日志。

`Get-ExecutionPolicy` 与 `Get-ExecutionPolicy -List` 都报告：

```text
System.Security.SecurityException: 不允许所请求的注册表访问权。
Microsoft.Win32.RegistryKey.OpenSubKey(String name, Boolean writable)
System.Management.Automation.Internal.SecuritySupport.GetLocalPreferenceValue(...)
System.Management.Automation.Internal.SecuritySupport.GetExecutionPolicy(...)
Microsoft.PowerShell.Commands.GetExecutionPolicyCommand.BeginProcessing()
```

直接读取以下键的属性也得到 SecurityException：

```text
HKEY_CURRENT_USER\SOFTWARE\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell
```

该键的 Test-Path 返回 true。可见性不等于属性读取权限。异常中的 `MyComputer` 是异常附带的程序集区域信息，不能充当原脚本的来源标记或解释历史拒绝。函数签名中的 `Boolean writable` 也没有显示实际参数值，不能据此声称只读查询尝试写入。

诊断进程退出码为 0，是因为每个查询的异常被捕获并序列化；**不表示策略查询成功**。本轮在新的相关证据和明确只读授权下复核一次。失败后未通过替代注册表 API、换身份或提高权限重试。

## 为什么尚不能归因到低完整性

Microsoft 的 [Mandatory Integrity Control 说明](https://learn.microsoft.com/en-us/windows/win32/secauthz/mandatory-integrity-control) 区分完整性约束与 DACL；默认强制标签限制向更高完整性对象写入。低完整性本身并不能证明本次读取必然被拒。

[Restricted Tokens 说明](https://learn.microsoft.com/en-us/windows/win32/secauthz/restricted-tokens) 还说明限制 SID 可以增加访问检查。当前没有该键的可用安全描述符、任务令牌的完整限制 SID，或宿主给出的具体拒绝规则，因此不能选择某一机制作为已证根因。

旧 AuthorizationManager 日志没有完整的底层异常栈。当前注册表读取失败与历史错误相关，但不能补写成历史日志已证明的原因，也不能将其误称为自动审批拒绝。

## 明确缺口与最小后续动作

尚缺的不是另一份泛化执行策略列表，而是：

1. 提供原列表的用户窗口的实际进程路径、版本/位数、账号 SID 和令牌完整性。现有提示符不能完成这些比对。
2. 上述确切 HKCU 键对应的访问限制来源：适用 DACL/强制标签、限制 SID 或宿主拒绝归因。上一轮 Get-Acl 未返回可用描述符，本轮未重复它或尝试绕过。
3. 历史拒绝与当前异常的直接联系；现有损坏日志无法提供此项。

若执行环境所有者之后允许正式许可下的对照，最小动作应仍是**同一路径 Windows PowerShell 的只读身份、策略列表和该单一 HKCU 属性查询**，以观察正式许可是否改变结果。范围应逐项写入审批，不附带 `-File`、编译器、游戏、网络或安全设置变化。它可以提供当前环境差异的证据，但仍不自动授权运行原构建。

本轮尚未满足“证明历史失败来自受限环境”的条件，因此没有发起构建审批、沙箱外执行或将用户的 RemoteSigned 当作执行许可。也没有发起新的待批准动作；所有诊断查询均已结束，结果明确。

## 保留状态

- [旧检查点](../../.tooling/checkpoints/execution-restriction-diagnosis/manifest.json) 的 6 项当前文件和副本长度、哈希均由独立只读核验确认一致；manifest SHA256 为 `1abcf107b997dd1840618a7fddc1ce40f25725190f4beb01989f80d6ed60d92a`。
- [待实现设计](ENGINE-ROOT-MODEL-DESIGN-PENDING.md) SHA256 仍为 `1b90976c3fa748c4e796f2c1a7f85454388d2593dfd5eee9e1edc90bdc45f00e`；它仍是未实现、未编译、未测试的设计。
- Git HEAD 仍为 `6dfdae419d1c92f7738eefe241937057d83134df`。既有未提交改动保留。
- 本轮构建、测试、安全设置修改、原版读取/运行、存档访问和 commit/push 均为零。
