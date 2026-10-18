# 隔离动态对照：当前阻碍

> 验证对象澄清（2026-10-18）：自写模块的编译、运行和离线测试不依赖原版DLL、Steam、BIOS或Sandbox；这些隔离/授权门槛仅约束合法原版的动态行为对照，不阻塞自写实现。最终目标是独立的行为等价可编译代码，当前尚非完整游戏。详见工程 docs/VALIDATION-TRACKS.md。

2026-10-18只读检查结果：Windows10 Pro10.0.19045；MAXSUN MS-TZZ B560M，HypervisorPresent=false；i7-11700，VMMonitorModeExtensions=true、SLAT=true，但VirtualizationFirmwareEnabled=false。`Containers-DisposableClientVM` 与 `Microsoft-Hyper-V-All` 均为Disabled。未找到WindowsSandbox、vmconnect、Docker、VirtualBox、QEMU、vmrun启动器。`wsl --list --quiet` 返回exit1及帮助文本，不构成可用Windows原程序隔离环境。

普通沙箱内CIM最初AccessDenied；同样只读查询以现有审批流程获准后成功。`isolation-capabilities.json` 保存原始结果，功能枚举0已另行核实显示Disabled。未修改任何功能、固件、安全设置、网络规则或Steam配置。

## 被阻止的动作

目标动作：在独立Windows环境运行合法原程序，采集初始化、资源、条件/动作行为并与恢复实现比较。**当前未执行**，因为没有已验证隔离环境，无法证明真实Documents、AppData、Steam userdata/云同步服务不可达。不能用新cwd、环境变量、新存档参数或普通子进程冒充隔离；宿主原程序不启动。

受阻的是原版动态对照用例。自写最小场景、项目专用状态与纯模块测试可独立离线推进；行为等价结论按已获证据限定，不宣称全部原版已对照通过。

## 可供后续确认的路径，均未执行

若用户选择Windows Sandbox，需要先在固件启用CPU虚拟化（人工固件设置，不能由此任务替用户执行），再由管理员启用Windows功能并可能重启。候选系统命令如下，仅为审批提案：

```powershell
Enable-WindowsOptionalFeature -Online -FeatureName Containers-DisposableClientVM -All -NoRestart
```

该命令会改变系统功能，可能安装依赖；现有项目/分析授权不包含执行它。当前不需要为纯模块编译安装.NET SDK；已安装Framework编译器可用。

Sandbox/VM建成后仍须单独验收：网络关闭；无Steam账号或同步客户端；无宿主真实用户目录/Steam userdata映射；仅项目专用目录可写；必要原程序/资源位置只读映射且不复制整套游戏；剪贴板、音视频和其他宿主集成按测试所需最小配置。合法运行如依赖Steam授权，必须走正常授权机制，不能绕过DRM，也不能因失败改成宿主直接启动。无法满足时停止该原程序对照用例。

隔离环境先只运行自写文件可达性探针和原创最小场景；探针只检查配置中人为设置的哨兵，不扫描或读取真实存档。确认宿主文件不可写、外部网络不可达、退出后宿主哨兵及安装哈希不变，再考虑原程序。新状态只写项目专属格式和目录；其含义、版本和兼容性单独规定，不冒称GD存档格式。

这是需要主会话处理的环境变更决策，不是Rea安装问题。没有被自动审查拒绝的待重试调用；没有在拒绝后换路线。
