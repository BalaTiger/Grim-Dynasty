# 隔离操作分工：目前只核查，重启暂缓

> 验证对象澄清（2026-10-18）：自写模块的编译、运行和离线测试不依赖原版DLL、Steam、BIOS或Sandbox；这些隔离/授权门槛仅约束合法原版的动态行为对照，不阻塞自写实现。最终目标是独立的行为等价可编译代码，当前尚非完整游戏。详见工程 docs/VALIDATION-TRACKS.md。

用户已同意总体方案；当前其他任务运行中，**不启用功能、不安装、不重启、不启动沙盒或原游戏**。本轮只读检查并在工程保存记录和配置草案。

## 本机复核与可靠性

2026-10-18 10:39 UTC：CIM与注册表一致显示Windows10 Pro22H2，19045.6466，64位。i7-11700，8核16线程，VMMonitorModeExtensions和SLAT均true，VirtualizationFirmwareEnabled=false；ComputerSystem的HypervisorPresent=false，与固件虚拟化未启用的判断一致。它们是Windows当前硬件报告，尚不是人工查看BIOS的结果；用户改完BIOS后仍需复查。

OS可见约31.88GiB内存，其中检查时约15.11GiB空闲；C盘约99.26GiB、D盘约1.42TiB空闲。满足Sandbox的官方前置门槛：至少4GB内存（推荐8GB）、1GB空闲盘、2CPU核；拟给初始探针4GiB。内存空闲为瞬时值，实际启动前重查。[微软安装要求](https://learn.microsoft.com/en-us/windows/security/application-security/application-isolation/windows-sandbox/windows-sandbox-install)

Sandbox、Hyper-V hypervisor、Hyper-V-All、Containers、VirtualMachinePlatform及HypervisorPlatform当前均Disabled。Sandbox详细元数据的RestartRequired枚举为**Possible**，不是“系统现在必须重启”。CBS和WindowsUpdate两个常见待重启注册表标记均false，也不等于穷尽所有待重启原因。

当前检查进程已持有管理员令牌。未来功能命令可由助手执行，不必要求用户手输命令；若届时Windows出现UAC/安全桌面确认或需要管理员凭据，用户本人处理。当前工具不能代用户进入BIOS或完成安全桌面/账号验证。

## 最小操作表

| 操作 | 助手/用户分工 | 宿主重启 | 现在是否推进 |
|---|---|---|---|
| 复核配置、整理工程内探针与结果目录草案 | 助手 | 无 | 可以；本轮只保存草案与检查记录 |
| 保存其他任务、确定维护窗口 | 用户 | 先不重启 | 用户决定何时其他任务已可中断 |
| BIOS/UEFI开启CPU虚拟化（常见VT-x/VMX） | 用户本人；主板菜单尚未核实 | 需要退出当前OS并重新启动 | 暂缓 |
| 启用Sandbox及系统自动选择的父功能/依赖 | 助手可用管理员命令；UAC若出现由用户确认 | 可能需要，实际返回和后续状态决定 | **暂缓，不趁当前无重启先启用** |
| 宿主重启及必要登录 | 用户安排；助手不自动重启 | 会中断其他任务 | 暂缓，等待明确维护窗口 |
| 启动配置并跑自写隔离探针 | 前置完成后助手可用命令；若需窗口交互由用户处理 | 通常无额外宿主重启 | 目前不启动 |
| 若合法Steam授权需要登录/Steam Guard | 用户本人完成；不能复用宿主用户数据映射 | 通常无宿主重启 | 仅列为可能步骤，尚未解决和执行 |

## 功能依赖与执行顺序

最小功能入口仍是以下**待执行**命令；-All由Windows启用父功能及其默认依赖，-NoRestart只阻止自动重启或重启提示，不消除重启需求。它仍会改变系统，因此当前不执行。[DISM参数说明](https://learn.microsoft.com/en-us/powershell/module/dism/enable-windowsoptionalfeature)

```powershell
Enable-WindowsOptionalFeature -Online -FeatureName Containers-DisposableClientVM -All -NoRestart
```

只读查看本机Sandbox组件清单还发现Containers客户端管理、Hyper-V hypervisor、Guest Kernel、虚拟设备和Utility VM等包；这些是Windows服务组件依赖，**不是让用户逐项手动勾选的列表**。不预先要求安装Docker、第三方VM、WSL或启用整套Hyper-V管理工具。若实际功能服务提示缺少组件源或前置条件，先报告，不扩大安装范围。

微软给出的标准顺序是：固件虚拟化→Windows Sandbox功能→若提示则重启→复核并启动。该顺序最容易判断问题，但可能跨越不止一次启动。

能否合并：将来同一维护窗口中，若系统允许在固件虚拟化尚未打开时先准备Sandbox功能，可用-NoRestart暂不重启，再在安排好的启动过程中进入BIOS开启虚拟化，随后让Windows完成配置，尝试合并启动次数。但本机未试此路径，前置校验可能不允许，组件配置也可能要求追加重启；**不能保证只重启一次**。当前不为尝试合并而启用功能。

## 无网络、无宿主用户映射的Steam限制

本机Engine.dll/x64 Engine.dll普通PE导入表确有steam_api.dll/steam_api64.dll及SteamInternal_SteamAPI_Init等依赖；这是静态依赖证据，不是运行结果。Valve文档指出Steamworks初始化依赖运行中的Steam客户端、相同OS用户上下文及当前账户拥有该App许可。[Valve初始化说明](https://partner.steamgames.com/doc/sdk/api#initialization_and_shutdown)

拟议全新Sandbox没有宿主Steam客户端和授权上下文，仅只读映射游戏文件，**因此原版启动存在明确授权/平台初始化限制，不能保证可运行**。尚未验证本机游戏在失败时的离线处理路径，也不据导入表断言必定退出。Valve的通用DRM说明不能证明本机EXE具体用了哪种封装。

本轮没有登录Steam、读取登录缓存、挂载userdata、启动原游戏或改Steam设置。若正常授权要求沙盒内客户端安装、联网或登录，那是需要另行列明范围的准备，不自动打开网络，也不把宿主登录缓存或真实存档目录带进沙盒。可先用自写恢复模块和原创数据验证隔离；原游戏对照暂不承诺。专用且已有合法离线授权的独立测试环境可能更合适，但目前没有已核实环境，仍不自行换电脑。

## 当前可完成的事

保持现有系统运行：保存配置示例、检查XML和映射白名单、准备自写可达性探针代码/合成哨兵设计、整理合法授权待验证清单，并继续离线格式/纯逻辑恢复。无需重启；不会自动把.example变成启动动作。实际启用、安装、登录和启动均等维护窗口及相应前置结论。
