# 待确认的最小隔离方案

> 验证对象澄清（2026-10-18）：自写模块的编译、运行和离线测试不依赖原版DLL、Steam、BIOS或Sandbox；这些隔离/授权门槛仅约束合法原版的动态行为对照，不阻塞自写实现。最终目标是独立的行为等价可编译代码，当前尚非完整游戏。详见工程 docs/VALIDATION-TRACKS.md。

这是对 `ISOLATION-GATE.md` 的具体化，未实施。离线格式恢复不依赖该环境，继续进行。

## 已核实本机

Windows10 Pro10.0.19045，64位；MAXSUN MS-TZZ B560M主板，Intel i7-11700。CPU支持虚拟机监控扩展和SLAT，但固件虚拟化报告关闭，系统当前未运行hypervisor。Windows Sandbox与Hyper-V功能均Disabled，没有发现Sandbox、Hyper-V连接器、Docker、VirtualBox、QEMU或VMware启动器。WSL列表命令exit1且仅返回帮助文本，未证明可用发行版。

现有.NET Framework C#编译器、.NET6/8运行时、Python、Node、Rea6.5.0、Ghidra12.1.4及JDK21足以继续离线模块恢复；不需要为当前工作安装SDK或虚拟化软件。

## 最小本机路径及所需权限

1. 用户在本机BIOS/UEFI中启用Intel CPU虚拟化，常见名称Intel Virtualization Technology、VT-x或VMX。尚未核实本主板菜单位置，不编造具体菜单路径。保存并退出固件会重启；这一步须用户操作。
2. 另行批准后，由管理员执行以下Windows功能变更；`-All`由系统选择Sandbox所需依赖，无需先单独请求开启整套Hyper-V管理工具。可能需要系统组件源和按提示重启；本任务不自动重启。[微软安装说明](https://learn.microsoft.com/en-us/windows/security/application-security/application-isolation/windows-sandbox/windows-sandbox-install)

```powershell
Enable-WindowsOptionalFeature -Online -FeatureName Containers-DisposableClientVM -All -NoRestart
```

确切申请范围：仅上述Sandbox功能及其必需依赖、以后明确同意的重启和一次配置文件启动；不包括额外虚拟化软件、其他固件选项、安全策略、驱动、账号或Steam配置变更。固件与系统功能当前均未改动。

3. 启动前检查可用内存、功能状态和路径映射；先运行自写探针，再考虑原程序。提供 `sandbox-minimal.wsb.example` 供审阅，尚不具备已验证可运行结论。初始探针不自动运行任何游戏。

## 文件和云端边界

拟用Sandbox网络关闭、剪贴板/打印/音视频重定向关闭；只映射原版安装目录只读、项目专用探针输入目录只读、项目专用结果目录可写。不映射宿主用户目录、真实Documents/AppData或Steam userdata；不登录同步账号，不运行云客户端。容器自己的用户目录和状态与宿主分开。配置控制机制以[微软配置文档](https://learn.microsoft.com/en-us/windows/security/application-security/application-isolation/windows-sandbox/windows-sandbox-configure-using-wsb-file)为依据。

映射仅为本机只读访问，不复制整套原游戏。写入仅限 `D:\MyGitRepo\Grim-Dynasty\.tooling\isolated-stage\out`。探针用人为创建的哨兵验证映射权限，不扫描真实存档。确认网络和目录隔离、退出后宿主哨兵/安装哈希不变，才会提议原程序行为采样。

Steam版原游戏能否在该干净离线环境通过正常授权启动仍未验证。需要授权、额外安装或联网时报告精确缺口，不尝试DRM绕过，也不退回宿主运行。Sandbox启动成功本身不代表原游戏可用或真实存档隔离已验收。

## 已有独立机器或环境替代

若用户已有专用Windows测试机或隔离VM，且具备合法原版、干净用户状态、无真实存档/云同步通路，并能提供受控结果目录，可替代本机固件与功能变更。需要先指定具体机器/环境并明确授权接入、只读检查和目标执行范围；本任务目前只核实当前本机，没有可宣称已连接的独立环境。此前“不换电脑”约束仍有效，不自行搜索或切换其他设备。

普通新cwd、新账户、新存档参数或仅改环境变量，尚不足证明原程序不会访问宿主真实数据。
