# 独立构建可行性：模块边界审计 v1

2026-10-18。目标仍是保留原底层代码/系统的独立明末恶搞游戏，不是mod。此次不重复crashreport验证，不启动游戏或编辑器，不读取存档、Steam配置或云端数据。

## 结论与可推进范围

**已能在不改Steam/DRM和原安装的条件下推进静态边界识别、原创设计与数据规格；尚无证据证明能保留现有底层二进制并完成独立启动/独立构建。** 原代码分布并不等于一套可链接SDK：主EXE直接依赖Engine/Game的大量C++符号；游戏存档语义与底层写入/路径设置跨模块分布；Steam API在Engine中存在普通导入依赖。单独替换内容或调用SetSavePath不能证明完全独立、不会访问原用户数据。

本轮输出是候选接口与限制证据，不是重建源码或运行时绕过方案。没有把路线改成mod或换引擎重写。Rea/Ghidra可用，但其Windows P0默认在完整auto-analysis完成后才提供函数结果；为了避免对大库做整库自动分析，本轮对Engine/Game只使用轻量PE解析、符号与384字节局部探针，没有启动它们的Ghidra分析。

## 已证实：普通PE导入依赖

证据module-boundaries.json，记录文件hash、完整普通导入符号及有界导出例子。只覆盖普通导入；延迟导入、LoadLibrary/GetProcAddress等动态行为未覆盖。边表示链接依赖，不表示已观察每条运行调用。

| 调用方 | 被依赖模块 | 普通导入名数量 | 判断 |
|---|---|---:|---|
| x64/Grim Dawn.exe | Engine.dll | 992 | 主宿主直接使用引擎接口 |
| x64/Grim Dawn.exe | Game.dll | 2863 | 主宿主直接使用大量游戏层接口，不能假设只换一个main函数就能重建 |
| x64/Game.dll | Engine.dll | 1183 | 游戏层依赖引擎对象、资源/数据与通用服务 |
| x64/Engine.dll | steam_api64.dll | 14 | 平台链接边存在；不能仅凭此判定具体DRM行为或独立离线运行结果。本轮未分析/修改认证逻辑 |
| AssetManager.exe | 根目录32位Engine.dll | 26 | 官方资源构建工具复用引擎结构；这不是编译Engine/Game源码的工具链 |
| ArchiveTool.exe | 普通导入未见Engine/Game | 0 | 归档工具普通导入较独立；不据此断言无动态依赖或其产物能独立运行游戏 |

## 数据库与资源边界

已观察Engine导出FileSystem::AddSource/AddSourceArchive、FileSourceArchive、DatabaseArchive、BaseResourceManager、ResourceLoader、LoadTable相关名字。Game普通导入Engine的LoadTableFile、GetLoadTable、GetResourceLoader、Load/LoadFromTable等；这些是“内容经引擎读取并交给游戏对象使用”的强接口线索，实际优先级、验证与序列化格式仍未证实。

AssetManager确实导入LoadTableBinary构造、Serialize/Deserialize，以及Archive::OpenRead、GetFileEntry等。其ASCII中可复查database/、resources/、Database/database.arz、*.arc、*.arz，以及调用ArchiveTool提取templates.arc的命令格式。本机原始ARZ/ARC和templates.arc已在第一阶段核实存在；本轮未解包或复制资源档。

**推测**：ARZ偏数据库记录，ARC偏资源容器，工具与运行时共享数据实现。**未证实**：全部字段模板、扩展覆盖顺序、地图源可恢复性、外部资源根能否完整替换、独立数据发行所需基础包。类名与字符串不是文件格式规范；有名字不代表已验证可用API。

## 保存机制跨模块分布

Engine实际导出SaveManager::AddJob/DirectRead/DirectWrite/WaitForCompletion、Engine::SetSavePath/GetSavePath。主EXE普通导入这些保存接口。Game导出GameEngine::SaveGame、GetRootSavePath、GetUserSaveDataFolder、GetFullSaveFolder、GetPlayerFolder、GetSharedSavePath等；主EXE还普通导入GameEngine::GetRootSavePath。没有调用任何方法或读取其生成路径。

这支持“Game组织角色/地图/难度等保存语义，Engine提供路径和写入服务”的边界推测；并非证明所有保存操作都统一通过某一个setter。共享仓库、设置、地图状态、日志、截图等可能是另行路径，尚需验证。出现cloud相关符号仅记为编译接口存在，不触发访问或查询任何云内容。

局部字节证据six-function-byte-probes.json：6个明确导出各读64字节，共384字节，仅解码明确匹配的简单x64 getter：

- Engine::GetSavePath，RVA0x73900，首个函数为返回this+0x240地址的LEA/RET；只证实对象内字段访问，不证明字段来源、默认值或独立产品路径覆盖。
- Engine::GetDatabaseArchive，RVA0x777d0，为从this+0x3d0读取8字节字段后RET；只证实对象持有数据库指针候选，不证明数据库可任意替换。
- SetSavePath及Game的三个路径函数只保留局部字节，标为未解释，不凭前64字节猜完整语义。样本可能包含相邻函数字节，不能当作完整函数长度。

新项目存读档生命周期仍按PLAN.md设计；不需要原版存档兼容，也不做迁移。要复用底层序列化，必须获得可用初始化/路径/产品身份接口及其全覆盖证据；不能用“名字叫SetSavePath”替代这种证据。

## 游戏系统与官方编辑器边界

Game有25113个命名导出；Inventory、Merchant、Faction、QuestManager/QuestSystem、PlayerCharacter等候选类别匹配798项。Engine有6283个命名导出，亦包含少量游戏相关名字；命名空间都使用GAME，所以不能用命名空间判断独立软件包。类别正则是检索工具，可能误命中，计数不等于系统功能数量或可调用SDK接口数量。

新增Rea实际只读元数据检查：inspect_managed_artifact(path=DBREditor.exe,detail=summary)成功，证据rea-managed-artifact.json。识别CIL/AnyCPU，尽管PE machine为0x014c；assembly版本2017.7.28.0、metadata版本v2.0.50727，64个TypeDef、578个MethodDef、8个AssemblyRef、20个ManifestResource。该assembly版本不是游戏补丁号，metadata版本也不等于已确认运行所需.NET版本。TargetFramework为空且runtime family未知；没有加载程序集、解析依赖、执行编辑器或反编译C#。

由此更正“PE机器字段x86就代表编辑器只能32位运行”的过度推断。托管编辑器能够读取记录并不提供原生Engine/Game源码、头文件或构建系统。

## 证据等级与阻碍

| 问题 | 当前状态 | 还缺什么 |
|---|---|---|
| 原引擎/游戏二进制分离 | 已证实两个DLL及大量导入边 | 公开/授权ABI、头文件、对象生命周期及初始化顺序 |
| 资源/记录容器接口 | 导入导出与工具字符串已观察 | 最小模板/记录格式验证、装载覆盖顺序、资源依赖闭包 |
| 独立新存档命名空间 | 有路径/保存接口候选，未验证覆盖 | 不接触原目录的完整生命周期契约；仅获许可且可隔离环境中验证 |
| 独立启动且保持平台机制 | 未证实 | 权利方支持的独立入口/产品身份/运行时分发方式；不移除认证或伪造AppID |
| 可重编译并保留原底层代码 | 没有源码/工程证据 | 获授权源码/SDK、第三方构建依赖及可发行运行时许可 |
| 数值冻结与星宿例外 | 设计约束已建立，未全量验证数据 | 记录级字段对照与测试；不以数值文本外观推断系统行为 |

大量C++导出含std::string/STL/对象类型，版本、布局、调用约定、分配器/CRT及虚表契约都不是稳定C接口。没有头文件时不能把导出表转换为可靠linkable SDK；生成伪代码不恢复编译配置、缺失类型、源文件边界或授权。

## 不改变项目性质的最小后续验证方案

1. 保持当前授权可直接做：对剩余资源接口候选作静态分组、导入/导出交叉核对、版本hash固定；完善原创明末文本与地图规格、28宿设计、新项目存读档契约；不运行原游戏。
2. 最小格式验证：先静态确认官方ArchiveTool/数据库编译命令与输出范围，只针对必要模板和一个原创记录，在工程私有临时目录验证读取/编译。禁止整套解包、提交或发行原资源；若工具默认写原安装/全局配置，未能用官方选项隔离就暂停，不能改原安装迁就工具。此步骤尚未执行。
3. 少量关键函数验证：优先只验证资源根设置、保存路径组成与对象初始化这几项问题，事先列输入、预期证据和预算。Rea默认整库自动分析不满足轻量要求时，先评估官方headless的-noanalysis和精确地址脚本能否在工程私有副本上可靠完成；没有实测前不执行Engine/Game全库分析。不把局部反编译的路径字符串当作完整存档隔离证明。
4. 独立宿主验证需额外材料：在获授权SDK/源码或允许的独立运行时接口到位后，才制作工程自己的最小入口，使用原创一条记录/一个地图块与本项目新测试档验证，不借用原版入口、Steam设置或真实存档。缺少授权初始化契约时不通过DLL注入/补丁/平台模拟来“验证”。

当前阻碍是独立入口/ABI/资源装载契约和源码/运行时授权材料缺失，并非Rea未接通。继续静态审计与原创设计无需扩大权限；尝试原版进程、平台变更、真实存档或把产品改成mod/重写均不在本次路线内。

## 可复查与输入完整性

module-boundaries.json保存普通导入全名、按类别最多40个导出例子和ASCII字符串最多60个例子及完整匹配计数；ASCII候选有一部分本身就是导入/导出名，不把它们当成第二份独立行为证据。six-function-byte-probes.json记录相对路径、输入hash、RVA、文件偏移与hex。解析脚本同目录保留，重新运行会拒绝覆盖既有证据；如需复查应选新输出目录。

input-integrity-recheck.json确认本轮5个输入文件的末次SHA256均与盘点时一致。本轮没有原版文件写入、源码重建、存档访问、配置/权限扩张或新软件安装；没有commit、推送或发布。
