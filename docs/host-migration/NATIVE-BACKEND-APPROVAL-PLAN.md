# 本机 Ghidra/JDK 安装、自检与静态查询审批计划

日期：2026-10-19。状态：仅完成只读核实与本计划；尚未按本计划下载、解压或运行 Ghidra/JDK，尚未修改启动器或 MCP 配置。本计划供一次性批准具体范围，不把旧机安装许可视为本机的新许可；保留逐次工具审批 `prompt`。

## 固定版本、来源与校验

选择 **Ghidra 12.1.4 PUBLIC** 与 **Eclipse Temurin JDK 21.0.12.1+1，Windows x64 / HotSpot / 完整 JDK**。本机已安装的 Rea 6.5.0 `dist/ghidra/GhidraInstallationPolicy.js` 明确 `SUPPORTED_GHIDRA_VERSION = "12.1.4"`、`SUPPORTED_GHIDRA_JAVA_MAJOR = 21`。这是 Rea 支持且验证过的组合，不宣称它是 Rea 官方默认推荐。

| 项目 | Ghidra | JDK |
| --- | --- | --- |
| ZIP 文件名 | `ghidra_12.1.4_PUBLIC_20260921.zip` | `OpenJDK21U-jdk_x64_windows_hotspot_21.0.12.1_1.zip` |
| SHA256 | `ddac49f903da9d5bac833e5cc79395098b9c33cfd3279be5f31bd00387d2d4db` | `f9d6e191ab098c0d416e7d588a24420a8621cd2f4720dab2459b8b7b2d2d8b4e` |
| 旧机下载已核实大小 | 569649598 bytes | 205073461 bytes |

固定下载 URL：

```text
https://github.com/NationalSecurityAgency/ghidra/releases/download/Ghidra_12.1.4_build/ghidra_12.1.4_PUBLIC_20260921.zip
https://github.com/adoptium/temurin21-binaries/releases/download/jdk-21.0.12.1%2B1/OpenJDK21U-jdk_x64_windows_hotspot_21.0.12.1_1.zip
```

2026-10-19 已在线核对两个官方附件页的文件名与 SHA256，均与 [旧机 verified-packages.json](../phase-1/ghidra-smoke/verified-packages.json) 一致：[Ghidra 附件](https://github.com/NationalSecurityAgency/ghidra/releases/expanded_assets/Ghidra_12.1.4_build)、[Temurin 附件](https://github.com/adoptium/temurin21-binaries/releases/expanded_assets/jdk-21.0.12.1%2B1)。在线页面显示约 543 MB / 196 MB；表中精确字节数来自旧机记录，下载后仍须重新核对。未下载 ZIP 来完成本次核实。

[Ghidra 官方安装说明](https://github.com/NationalSecurityAgency/ghidra/blob/Ghidra_12.1.4_build/GhidraDocs/GettingStarted.md) 支持直接解压安装，要求 64 位 JDK 21；个人目录安装不需要管理员。Rea 的 Java HeadlessScript 路径不需要额外 Python/PyGhidra：[Rea Windows Ghidra 说明](https://github.com/morluto/rea/blob/main/docs/windows-ghidra-p0.md)。

## 精确目录

所有下列相对路径均相对于 `C:\Users\zhuzi\Documents\Grim-Dynasty`。

| 用途 | 绝对路径 |
| --- | --- |
| 下载及校验记录 | `C:\Users\zhuzi\Documents\Grim-Dynasty\.tooling\downloads` |
| Ghidra 解压后根目录 | `C:\Users\zhuzi\Documents\Grim-Dynasty\.tooling\ghidra\ghidra_12.1.4_PUBLIC` |
| JDK 解压后根目录 | `C:\Users\zhuzi\Documents\Grim-Dynasty\.tooling\jdk\jdk-21.0.12.1+1` |
| Rea 固定入口 | `C:\Users\zhuzi\Documents\Grim-Dynasty\.tooling\rea\node_modules\rea-agents\scripts\rea.mjs` |
| 项目启动器 | `C:\Users\zhuzi\Documents\Grim-Dynasty\tools\static_analysis\rea_project_server.mjs` |
| Rea/Ghidra 运行父目录 | `C:\Users\zhuzi\Documents\Grim-Dynasty\analysis-runtime\rea` |
| 安装与自检证据 | `C:\Users\zhuzi\Documents\Grim-Dynasty\docs\host-migration\native-backend` |
| 新的函数静态证据 | `C:\Users\zhuzi\Documents\Grim-Dynasty\docs\object-lifecycle\rea-native-registration` |
| 修改前启动器/项目配置备份 | `C:\Users\zhuzi\Documents\Grim-Dynasty\.tooling\backups\native-backend` |

**运行父目录不得沿用 `.tooling\rea-runtime`。** 本机 Rea 6.5.0 的 `GhidraSessionRoot.js:1-43` 说明 Ghidra 项目路径的任何元素都不能以 `.` 开头；Windows 下这种输入会退回最近的无点前缀祖先。固定为 `analysis-runtime\rea`，使会话不意外落到工程根。工具安装位置不作为 Ghidra 项目路径，仍保留在 `.tooling`。

## 项目内偏好、临时文件和日志：已核实的实现

以下代码均来自本机固定的 `.tooling/rea/node_modules/rea-agents/dist/ghidra/`，只读检查过，未修改供应商代码。

| 内容 | 已支持机制与代码依据 |
| --- | --- |
| Rea 会话父目录 | `GhidraSessionRoot.js` 从 `os.tmpdir()` 选择无点前缀路径；`GhidraClient.js:394-399` 在它下面创建 `rea-ghidra-*` 私有会话目录。启动器导入 Rea 前固定 `TEMP/TMP=analysis-runtime\rea`。 |
| Ghidra 项目与快照 | `GhidraLauncher.js:440-450` 定义会话下 `project`、`home`、`tmp`、`cache`、`config`、`data`；使用临时项目，运行参数含 `-readOnly`、`-deleteProject`。 |
| JVM 用户目录与临时目录 | `GhidraJavaLaunch.js:63-65` 为 LaunchSupport 和实际 JVM 显式传 `-Duser.home=<会话>\home`、`-Djava.io.tmpdir=<会话>\tmp`；`GhidraLauncher.js:542-551` 为 Windows 批处理子进程构造带引号的 `JDK_JAVA_OPTIONS`，含上述两个属性与 `-XX:-UsePerfData`。不从上级继承任意 JVM 参数。 |
| Windows 用户偏好和缓存 | `GhidraLauncher.js:518-539` 将子进程 `USERPROFILE/HOME` 指向会话 `home`，`APPDATA` 指向 `config`，`LOCALAPPDATA` 指向 `cache`，`TEMP/TMP/TMPDIR` 指向 `tmp`，`XDG_CONFIG_HOME/XDG_CACHE_HOME/XDG_DATA_HOME` 指向对应子目录。 |
| Ghidra JDK 选择偏好 | `GhidraLauncher.js:452-505` 明确预建会话内 `config\ghidra\ghidra_12.1.4_PUBLIC`，供 LaunchSupport 写 JDK 选择，不依赖真实个人配置目录。 |
| Ghidra 日志 | `GhidraLauncher.js:449-450` 和其 headless 参数将 `-log`、`-scriptlog` 分别指向会话 `ghidra.log`、`script.log`；调用结果和 stdout/stderr 由项目驱动保存到上表证据目录，保留前必须遮蔽会话 token。 |
| JDK 选择及环境清理 | `GhidraInstallation.js:290-305` 清空继承的 `_JAVA_OPTIONS`、`JAVA_TOOL_OPTIONS`、`JDK_JAVA_OPTIONS`、`GHIDRA_JAVA_OPTIONS`，仅在子进程 PATH 前添加固定 JDK 的 `bin`。不改用户或系统 PATH。 |

启动器外层也要把 Rea 进程的 `USERPROFILE/HOME`、`APPDATA`、`LOCALAPPDATA` 固定到 `analysis-runtime\rea\host\home`、`host\config`、`host\cache`，不再继承真实个人目录。先创建这些目录，再导入 Rea。Rea 的 Ghidra 会话随后使用其更细的自动隔离目录。

限制：这只是已经核实的环境和启动参数隔离，不是 OS 文件系统沙箱。`rea doctor` 的 Java 版本探针在 `GhidraInstallation.js` 中使用 `-XshowSettings:properties -version`，并清空 JVM 注入环境；它不经过完整 Ghidra 会话隔离，不能把会话内 `-Duser.home` 保证直接套给 doctor。计划使用固定工程内的外层用户/临时目录捕获自检输出，并检查实际输出及失败信息；若观察到需要额外写入项目外的动作则停下报告，不修改供应商校验代码来绕过。

Rea Windows native 会自动为自己的临时文件管理私有 DACL、以 Job Object 管理其子进程，并使用经过认证的 `127.0.0.1` 临时端口通信。这些是其本地实现，不代表批准修改系统安全策略、全局 ACL、用户令牌或防火墙。不额外运行 `icacls`、`Set-Acl`、提权或系统安全配置命令。若其原生控制失败，保留错误并停止，不削弱控制。

## 需一次性批准的执行动作

1. **获取固定的两个 ZIP。** 用现有 PowerShell HTTPS 请求从上面的固定 GitHub 官方发布 URL 下载到 `.tooling\downloads`，允许正常 GitHub 发布 CDN 重定向。用 `Get-FileHash -Algorithm SHA256` 核对固定 SHA256、核对字节数，保存来源和校验 JSON。下载中断可在同一文件范围内重试；完整校验失败不得解压/执行。不得安装其他下载器。
2. **便携解压。** 用现有 .NET ZIP API 只读检查条目，拒绝绝对路径、越出目的目录的路径及不符合固定顶层目录的布局；用 `Expand-Archive -LiteralPath` 解压到上表两个新目录。目录已存在或非空时先检查并报告，不覆盖既有安装。保留许可证；原版游戏目录不写入。
3. **准备受限子进程环境与版本自检。** 创建上表工程内目录；只为本次子进程设置固定 `GHIDRA_INSTALL_DIR`、`JAVA_HOME`、`REA_ANALYSIS_PROVIDER=ghidra`、`GHIDRA_HEADLESS_MAXMEM=512M`、`REA_LOG_LEVEL=warn` 及项目内用户/临时目录。执行固定 JDK 的 `bin\java.exe -version`、`bin\javac.exe -version`；记录 stdout/stderr 和退出码。不得执行系统安装器、服务安装、GUI 或游戏。
4. **更新项目启动器和项目 MCP 配置。** 先在工程备份当前内容及哈希，再修改 `tools/static_analysis/rea_project_server.mjs`：在现有环境过滤后显式加入两个固定工具路径、固定 Ghidra provider 和内存上限，改运行父目录为 `analysis-runtime\rea`，固定外层偏好目录。仍不透传任意 `GHIDRA_*` / `JAVA_*` / `REA_*` 或凭证；仍检查 Rea 版本 6.5.0。当前 wrapper 过滤继承的 Ghidra/JDK 设置，因此只写 MCP env 而不更新 wrapper 不会生效。
5. **项目配置范围。** 只更新 `.codex/config.toml` 的现有 `[mcp_servers.rea]` 和对应 env 块，并同步 `docs/host-migration/rea-project-config.toml` 作为可审阅副本；保留其他配置。固定命令 `D:\NodeJS\node.exe` 和项目 wrapper 不变。`default_tools_approval_mode = 'prompt'` 保持不变。`startup_timeout_sec = 30` 保持；将单个静态查询 `tool_timeout_sec` 从 60 提到 600，使 Rea 默认约 330 秒冷启动窗口有机会完成。这是有限等待，不放宽原生安全控制。项目 `.codex` 写入如另需审批，就停在该审批动作；不得改全局 Codex 配置代替。
6. **扩展静态 allowlist，仅以下 9 项。** `binary_session`、`open_binary`、`close_binary`（已有）及 `procedure_pseudo_code`、`procedure_assembly`、`procedure_info`、`procedure_callees`、`xrefs`、`address_name`。本机 `GhidraFunctionValues.js:15-64` 和 `GhidraInventoryValues.js:13-75` 已确认后六项契约。它们用于伪代码、汇编、函数边界、直接被调用者、地址交叉引用和符号名；不添加重命名、注释写入、动态进程或浏览器工具。执行前仍用实际 MCP `tools/list` 核对工具名和参数；不匹配则停下报告，不自动扩展权限。
7. **后端自检。** 在同样的固定环境下执行 `D:\NodeJS\node.exe C:\Users\zhuzi\Documents\Grim-Dynasty\.tooling\rea\node_modules\rea-agents\scripts\rea.mjs doctor --provider ghidra --json`。Ghidra 版本、headless、JDK、原生 decompiler 和 Windows native 控制必须通过。未安装的其他 provider 不作为加装理由。使用项目 launcher 建立 MCP，验证实际会话 provider 为 Ghidra，再进入首次静态查询。需要让客户端重建本项目 MCP 子进程时仅重建该连接，不重启电脑或用户应用。
8. **有限静态查询。** 只读打开下节两个已确认哈希的 DLL，指定 `provider_id=ghidra`；只查询列出的对象工厂/注册相关地址和从其实际汇编/调用结果直接解析出的必要依赖。不运行、加载或调试目标 DLL，不调用目标入口。每个查询记录输入 SHA256、地址、Rea/Ghidra 版本、Evidence 与局限；结束执行 `close_binary` 并核对关闭/清理结果。若为自动驱动新增或调整脚本，仅限 `tools/static_analysis/` 的这组查询驱动，使用相同目标/工具白名单、有限超时和最终关闭流程；不能用驱动绕过 `prompt` 或审批拒绝。
9. **保留证据与结束核验。** 保存两个 ZIP 校验记录、版本输出、doctor JSON、MCP 静态结果及关闭结果；分析前后重新计算两个原版 DLL 的 SHA256。若自动清理失败，不递归删除未知目录；先确认实际路径属于本次工程会话再决定后续处理。任何失败都保留状态，不宣称已通过；不 commit/push。

以上涉及的外部程序运行范围只有固定 Temurin Java/Javac、现有 Node/Rea、固定 Ghidra headless 及其自带 decompiler/桥接子进程。不会安装 Hopper、IDA、Frida、Python、编译器或其他工具。无需 Windows 重启，不修改全局 PATH/JAVA_HOME、系统 Java、Steam 设置、系统安全策略或全局 ACL。

## 首次目标及查询边界

游戏根由本机 Steam 注册表、`libraryfolders.vdf`、`appmanifest_219990.acf` 独立确认，Steam build ID 为 `25813250`。此次已核实的输入：

| 文件 | SHA256 |
| --- | --- |
| `D:\Steam\steamapps\common\Grim Dawn\x64\Engine.dll` | `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153` |
| `D:\Steam\steamapps\common\Grim Dawn\x64\Game.dll` | `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2` |

从 [交接与下一模块记录](HANDOFF-AND-NEXT-MODULE.md) 中有证据的坐标开始：Engine `0x18017E170`（CreatePartial）、`0x18017E230`（FinishPartial）的汇编/调用顺序；Engine `0x180274EC0`（类工厂）、`0x18017FCA0`（manager +0x128 容器）、`0x1801356F0`（两项索引插入）；必要时 Game `0x18012C910`（共享创建 helper）。函数名解析和后续依赖地址必须来自返回证据，不能猜测表项或虚函数目标。

首次查询不需要增加其他原版 DLL。新模块实现仍以实际静态证据和独立自写测试为准；缺失构造、Read 或注册语义继续标 `Unsupported`，不把 C# 字典插入当完整原生注册。

玩家存档、Steam userdata/云数据、DRM/Steam 绕过和原游戏启动始终不在范围内。

## 当前未完成/待批准项

本计划只说明已核实来源、代码支持和下一步范围。ZIP 本机实际下载哈希、安装布局、JDK/Ghidra 运行健康、实际用户/临时/日志路径和新函数结果尚待执行验证。旧机 `GHIDRA-INSTALL-RESULT.md` 的成功记录仅供对照，不证明本机已完成。

本轮只新增本文档；没有自动审批拒绝、未知结果的安装动作，或仍在运行的 Ghidra/JDK 安装任务。后续若触发逐次批准或被自动审批拒绝，停止该动作并报告确切命令、目标、原因与已完成步骤。
