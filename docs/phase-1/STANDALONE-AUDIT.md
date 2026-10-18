> 最新状态：用户已明确批准，项目内Ghidra12.1.4与JDK21已安装并通过实际headless及单函数反编译；当前结果以GHIDRA-INSTALL-RESULT.md和ghidra-smoke证据为准。下文保留早期审计状态。

> 最新状态：用户已选择方案2；Rea6.5.0已备份注册并通过stdio握手（207工具）及open_binary文件识别。原生深度分析后端仍缺失。当前事实以REA-INSTALL-RESULT.md为准，下文保留早期审计历史。

> **Rea版本核验更新**：官方registry的6.6.0为404，latest及实际发布包为6.5.0。现已通过本机Node正常HTTPS下载6.5.0发布包并验证SHA512，与主会话证据一致；已静态核对setup参数与Codex配置行为。下文关于npm/TLS不可用与发布版本未核实的文字仅为历史记录，已由REA-PROPOSAL.md v2取代。没有执行setup或新增持久权限。
# 独立游戏技术审计补充

2026-10-18。本文件与 PLAN.md v2 反映最新用户目标；不再推进 mod 交付。全部检查仅静态读文件，未载入目标 DLL、启动游戏或修改安装。

## 本机二进制与依赖证据

完整普通 PE 导入与 SHA256 见 binary-inventory.json；可复现只读脚本为 inspect_pe.py，使用现有 Python 标准库，无第三方安装。共分析13个目标。

| 模块 | 已观察依赖 | 可得结论与限制 |
|---|---|---|
| 根目录 Grim Dawn.exe | x86；Engine.dll、Game.dll、lua51.dll、统计/崩溃模块、VC140/CRT、Windows网络API | 是预编译游戏程序，不是引擎 SDK |
| x64/Grim Dawn.exe | x64；同类依赖 | 存在两个架构；不代表任一可直接改名独立运行 |
| Engine.dll（两架构） | FreeImage、Lua5.1、zlib、VC140/CRT；x86导入steam_api.dll，x64导入steam_api64.dll | 存在 Steam API 依赖；仅凭导入表不能断言具体DRM或离线行为，未尝试绕过 |
| Game.dll（两架构） | Engine.dll、Lua5.1、VC140/CRT、WindowsAPI | 游戏逻辑与引擎耦合；没有可构建 C++ 源证据 |
| AssetManager.exe / Editor.exe / MapCompiler.exe | x86，依赖 Engine.dll；Editor另依赖Game.dll、Widget；部分使用mfc140 | 工具不是独立引擎构建器，运行也可能产生配置/日志；为保持安装隔离未启动 |
| DBREditor / QuestEditor / ConversationEditor | PE普通导入mscoree.dll | .NET入口；具体CLR目标/程序集依赖待核对，不能仅凭PE机器类型断定所有进程必须x86 |
| ArchiveTool.exe | x86、zlib、VC140/CRT等 | 资源归档工具，不能重建游戏底层代码 |

系统只读检查：.NET v4 Full 注册项、System32/SysWOW64 的 msvcp140.dll、SysWOW64 的 mfc140.dll 存在。这不是完整依赖解析或编辑器成功运行证明。导入审计未解析延迟导入、动态LoadLibrary、传递依赖或资源路径；不推断全部代码用途。

递归检查原安装305个文件：没有发现 .sln/.vcxproj/.cpp/.h/.pdb/.cmake/CMakeLists.txt，也无名称匹配EULA/license的文件。范围仅是当前安装目录，不代表用户其他目录没有获授权源码。当前工程起始没有游戏源代码。

## 能否完全在工程内构建和独立启动

**尚不能确认，当前没有可执行的独立构建路径。** 可在工程保存原创设计、清单与只读分析产物；官方 DBR/地图/资源编译能力即使验证成功，也不能等同编译 Engine.dll、Game.dll 或新产品入口。需要获授权源码/SDK或允许独立发行的运行时和接口。没有复制整套游戏或制作依赖原版的启动器。

获许可构建链到位后才验证：只读安装/SDK输入，项目内源/缓存/临时文件/日志/输出，明确所有进程工作目录；干净构建不向Steam安装或全局配置写入；独立入口不用原mod菜单；产品ID与本项目存档目录隔离；资源依赖闭包完整。若工具硬编码安装输出或用户配置，需先寻找官方参数/配置支持，不猜参数、不改原目录、不用符号链接绕过限制。

## 授权与发布边界（证据状态，不作法律结论）

Steam官方商店页 https://store.steampowered.com/app/219990/ 标明Crate Entertainment并声明Grim Dawn知识产权归属。未取得针对本项目的源码、引擎运行时、游戏代码/数据改编及独立发行许可证据。官方 mod 工具存在本身不证明可发布独立商业或非商业游戏；本次EULA网页获取失败，因此不引述未读条款。

需分别确认：Engine/Game二进制或源码再利用与发行、Steamworks独立产品集成、原数据库数值/文本及世界素材使用范围、Lua/FreeImage/zlib/Ogg/Vorbis等第三方通知与许可要求、编译工具可用范围、字体/音频/模型许可。只确认文件名称不能得出具体版本/许可义务，待真实获授权材料核对。不绕过DRM，不以反编译替代授权，不将原EXE/DLL/ARZ/ARC复制成项目成果。

## 指南与安装检查的剩余阻碍

本地PDF解析库缺失，尚未安装。浏览器尝试新标签只读打开file:///D:/Steam/steamapps/common/Grim%20Dawn/Grim%20Dawn%20Modding%20Guide.pdf，被浏览器安全策略拒绝：requested URL protocol is not allowed；只允许http/https。没有使用间接URL、本地代理或其他浏览器方式绕过。该浏览器动作已停止，后续仅使用批准的文件读取接口；官方指南尚未完成内容验证。

重试npm元数据保持TLS校验：Invoke-RestMethod失败The SSL connection could not be established；curl官方registry失败schannel AcquireCredentialsHandle SEC_E_NO_CREDENTIALS(0x8009030E)；网页工具registry不可访问、npm版本页403。源码6.6.0不等于发布包已核验。Rea审批详情见 REA-PROPOSAL.md。