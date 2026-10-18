> 最新状态：用户已明确批准，项目内Ghidra12.1.4与JDK21已安装并通过实际headless及单函数反编译；当前结果以GHIDRA-INSTALL-RESULT.md和ghidra-smoke证据为准。下文保留早期审计状态。

# Rea 6.5.0 安装注册与实测结果

2026-10-18。用户明确选择方案2，接受工具实际权限较宽，并要求操作目标仍限游戏目录只读、项目目录可写。目录范围是操作约束，不是已实现的硬隔离。此报告取代此前“等待注册/尚未执行setup”的历史状态。

## 已完成与配置差异

从已核验6.5.0缓存执行安装配置：

```powershell
npx --yes --registry=https://registry.npmjs.org --cache='C:\Users\Administrator\Documents\Codex\2026-10-18\task-2\.npm-cache' rea-agents@6.5.0 setup --client codex --skill=false --yes --json
```

setup退出0、status=ready、appliedActions=[configured_codex]，配置检查state=aligned。未安装个人skill或Hopper，未安装Ghidra/IDA/JDK。

宿主配置：C:\Users\Administrator\.codex\config.toml。
原始备份：C:\Users\Administrator\.codex\config.toml.rea.backup。
固定启动环境前的第二份备份：C:\Users\Administrator\.codex\config.toml.rea-launcher.backup。
原始备份SHA256：fc71ff4d70f98c165f85240524765c7ceeb9c00df5a0bb1d2c27f4a6befa4513，与注册前配置hash一致。

新增配置等价于：

```toml
[mcp_servers.rea]
command = "cmd.exe"
args = ["/d", "/c", "npx", "-y", "rea-agents@6.5.0", "mcp"]
startup_timeout_sec = 30

[mcp_servers.rea.env]
npm_config_registry = "https://registry.npmjs.org"
npm_config_cache = "C:\\Users\\Administrator\\Documents\\Codex\\2026-10-18\\task-2\\.npm-cache"
```

最后两项将未来npx启动固定到已验证官方registry及本任务缓存，避免回退本机默认npmmirror与先前不可写默认缓存；仅服务自身环境，不改npm全局设置。已用tomllib将当前配置移除rea后与原始备份比较，全部无关配置保持一致。没有改变系统权限或TEMP/TMP。

## MCP握手与一次只读目标检查

通过与注册相同的cmd.exe/npx启动命令、相同registry/cache环境启动stdio MCP，以独立JSON-RPC测试客户端验证：initialize成功，protocolVersion=2024-11-05，serverInfo.name=rea、version=6.5.0；notifications/initialized后tools/list成功，207个工具。

证据：rea-mcp-handshake.json保留完整initialize和tools/list（含工具schema，约9.8MB）；rea-verification-summary.json是精简结果。该验证是实际stdio服务握手，不代表当前已打开的Codex会话自动热加载新工具；宿主可能仍需重新载入配置/重开会话，未擅自重启应用。

对D:\Steam\steamapps\common\Grim Dawn\x64\Grim Dawn.exe调用open_binary成功，仅静态识别：format=pe，kind=executable，architecture=x86_64，SHA256=d3915e2599317b6670cc088ba958afb2b2f4fa8141cfd1d777ee02f9ea1fc4c4。与此前Python标准库PE清单一致，证据rea-open-binary.json。没有启动目标EXE、注入进程或修改目标文件。成功范围仅文件身份识别，不是函数反编译成功。

另一次inspect_binary_layout失败，已保留rea-readonly-inspection.json：capability_unavailable/unsupported_provider；pwntools-elf后端说明“The initial real-verified pwntools artifact profile supports Linux x64 only.”不重试该不支持操作，不换电脑或规避平台约束。

## 自检与后端

先前受限执行上下文的native self-test在C:\Users\ADMINI~1路径组件Win32 5拒绝；在用户明确批准、更宽实际权限且通过执行审批的安装验证中，该错误不再出现。没有改短路径、ACL、OS设置或安全选项。只能据此推断与执行权限上下文相关，未证明短路径拼写本身有错；不把已消失错误表述为已修复Windows系统问题。

doctor显示environment_healthy=false、availableProviders=[]。Ghidra未配置，IDA未配置，JADX jar未配置。PATH无ghidraRun/analyzeHeadless/ida/java/javac；检查Program Files、Program Files(x86)、用户Programs、D盘顶层与相关目录名均未发现可复用后端。不是全盘扫描，不能断言其他任意私有目录绝无安装。未得到可用原生反汇编/反编译能力。

## 后续后端提案（未安装，待批准）

首选Ghidra 12.1.4，匹配Rea当前doctor要求12.1.x。正式来源：https://github.com/NationalSecurityAgency/ghidra/releases/tag/Ghidra_12.1.4_build 。发布资产ghidra_12.1.4_PUBLIC_20260921.zip约543MB，官方页面SHA256为ddac49f903da9d5bac833e5cc79395098b9c33cfd3279be5f31bd00387d2d4db。

拟议下载URL：https://github.com/NationalSecurityAgency/ghidra/releases/download/Ghidra_12.1.4_build/ghidra_12.1.4_PUBLIC_20260921.zip 。批准后先下载到工程.tooling/downloads，核对SHA256，再解压到工程.tooling/ghidra；不写原安装、系统PATH、注册表或创建服务。不会直接运行下载的安装脚本。

Ghidra官方12.1指南要求64位JDK21；本机未发现PATH/JAVA_HOME可用JDK。可选官方指南链接的Adoptium Temurin21 Windows x64 ZIP（https://adoptium.net/temurin/releases/?version=21），先查询正式元数据并固定具体补丁版本/URL/hash，向用户报告后下载解压到工程.tooling/jdk；不安装系统JDK或改全局Java。Java与Ghidra的临时/缓存/日志目录也须规划在工程内，并用官方配置支持验证，不能声称默认不会写用户目录。

获准并完成包校验后才配置Rea服务的GHIDRA_INSTALL_DIR与JAVA_HOME，并验证doctor、桥接启动与一次函数分析。此步骤新增软件与配置，尚未执行。JADX主要面向Android，不为本机PE游戏优先安装；IDA需用户已有授权及正式MCP集成，不擅装。

参考依赖文档：https://raw.githubusercontent.com/NationalSecurityAgency/ghidra/Ghidra_12.1.4_build/GhidraDocs/GettingStarted.md 。本次GitHub API资产请求403，但官方release与expanded_assets网页可读；没有以失败API结果假称已下载Ghidra。

## 保持边界

本轮必要安装/配置写入仅限已披露Codex配置、备份与任务npm缓存；分析目标仅白名单游戏EXE，项目内保存证据。未访问云存档、真实存档、Steam设置，未改原版、系统安全或下载额外分析后端，未commit、推送或发布。
