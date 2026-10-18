# Ghidra/JDK 安装与真实反编译验收

2026-10-18。用户已明确同意“Rea官方支持且验证过的Ghidra12.1.4+JDK21方案”，此前条件授权等待已解除。

## 安装与完整性

- Ghidra12.1.4：D:\MyGitRepo\Grim-Dynasty\.tooling\ghidra\ghidra_12.1.4_PUBLIC。
- Temurin JDK21.0.12.1+1：D:\MyGitRepo\Grim-Dynasty\.tooling\jdk\jdk-21.0.12.1+1。
- 两项均为官方便携ZIP，未运行系统安装器、未更改全局PATH/JAVA_HOME或系统安全设置，许可证和第三方通知保留在解压目录。
- Ghidra ZIP 569,649,598 bytes，SHA256 ddac49f903da9d5bac833e5cc79395098b9c33cfd3279be5f31bd00387d2d4db，与官方release一致。下载曾被远端关闭，HTTPS Range返回匹配偏移后续传，最终完整哈希通过。
- JDK ZIP 205,073,461 bytes，SHA256 f9d6e191ab098c0d416e7d588a24420a8621cd2f4720dab2459b8b7b2d2d8b4e，与Adoptium官方元数据一致。
- 正式来源：https://github.com/NationalSecurityAgency/ghidra/releases/tag/Ghidra_12.1.4_build ；https://api.adoptium.net/v3/assets/latest/21/hotspot?architecture=x64&image_type=jdk&os=windows&vendor=eclipse 。本次已固定确切ZIP URL与哈希，见ghidra-smoke/verified-packages.json。
- 原版目录只读，工具、下载、运行时临时文件与分析证据均在工程内；.tooling/.gitignore忽略大型工具和运行数据。

## Rea配置

保持Rea6.5.0与原有Codex注册；更新前备份C:\Users\Administrator\.codex\config.toml.rea-ghidra.backup。原始注册前备份C:\Users\Administrator\.codex\config.toml.rea.backup继续保留。

仅在[mcp_servers.rea.env]增加GHIDRA_INSTALL_DIR、JAVA_HOME指向上述工程工具路径，GHIDRA_HEADLESS_MAXMEM=512M，TEMP/TMP指向D:\MyGitRepo\Grim-Dynasty\.tooling\runtime。没有全局环境修改。移除rea后，配置与原始备份语义完全一致。实际stdio验证使用同样的环境；已打开的宿主会话可能需重新加载配置，未自动重启用户应用。

## 实测证据

1. java -version与javac -version退出0，报告21.0.12.1、64位完整JDK；原始输出见ghidra-smoke/java-version.txt和javac-version.txt。
2. rea doctor --provider ghidra --json退出0，healthy=true，Ghidra12.1.4、headless启动器、win_x86_64原生decompiler、JDK及Windows native authority必要检查均通过；见ghidra-smoke/doctor.json。environment_healthy=false仍包含未选择的Hopper/IDA/JADX和刻意未安装skill等全环境信息，不表示Ghidra专项失败。
3. 仅选择原安装x64/crashreport.dll（52,224 bytes）作为小范围原生PE验证；没有分析整个游戏，也没有执行目标DLL。open_binary显式provider_id=ghidra成功，binary_overview在default auto-analysis完成后返回，说明实际headless导入/分析成功，不只是检查启动脚本存在。
4. procedure_pseudo_code在观察到的PE入口地址0x1800067ec返回非空伪代码，包含entry、__security_init_cookie和dllmain_dispatch。原始证据ghidra-smoke/procedure_pseudo_code.json，便于阅读的导出ghidra-smoke/crashreport-entry.pseudo.c。证据ID ev_b969264fb42f5d798a6736f9d123f08a217be37f82c27167a65bd7cdb8ecce3c。
5. binary_session返回Ghidra能力可用；close_binary成功，临时分析会话按工具生命周期关闭。见同目录binary_session.json和close_binary.json。
6. 目标SHA256 3a97b45842d3d037f803d43834ef169a302e86f9ef6be6132df9389659795ea7，操作后重新计算与分析目标digest一致。

选择512M堆上限仅供小模块验证。查询本机Java进程有效堆参数的Get-CimInstance返回拒绝访问；没有扩大OS权限或重试绕过，因此不声称独立测量了有效JVM堆或RSS。该可选观测失败不影响已返回的真实伪代码证据。

## 结果解释与独立工程审计的下一步

现在已有真实Ghidra反编译文本。伪代码是工具推导结果，不是原始C/C++源码；本函数属于崩溃处理模块初始化，不是战斗、背包、商店、任务或存档逻辑。PDB CrashReport.pdb加载状态未知，原函数名/类型可能不完整；工具还明确记录终止调用流模型的原因未建立。

本次证明Rea+Ghidra在此Windows环境能完成一个小型DLL的只读原生分析；不证明整个Engine.dll/Game.dll可无损恢复、重编译或独立发行，不改变缺失源码/SDK/运行时授权的审计结论。

可在原许可与任务边界内继续做：先按Engine/Game导出与小范围静态引用建立启动、资源根、产品身份和独立存档路径接口问题清单，明确每个结论对应观察证据与未知项；随后仅围绕单一问题选函数，不做全量游戏分析。遇到DRM/Steam认证逻辑不尝试移除或绕过；独立启动仍需合法授权路径或正式SDK/源码支持。新工程存读档设计保持，不访问任何真实/云存档来试验。

本轮没有获得可独立构建的游戏工程或原始源码；没有commit、推送、发布、改原版、真实/云存档或Steam设置。
