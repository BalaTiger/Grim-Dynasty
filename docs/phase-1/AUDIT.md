> 最新状态：用户已明确批准，项目内Ghidra12.1.4与JDK21已安装并通过实际headless及单函数反编译；当前结果以GHIDRA-INSTALL-RESULT.md和ghidra-smoke证据为准。下文保留早期审计状态。

> 最新状态：用户已选择方案2；Rea6.5.0已备份注册并通过stdio握手（207工具）及open_binary文件识别。原生深度分析后端仍缺失。当前事实以REA-INSTALL-RESULT.md为准，下文保留早期审计历史。

> **Rea版本核验更新**：官方registry的6.6.0为404，latest及实际发布包为6.5.0。现已通过本机Node正常HTTPS下载6.5.0发布包并验证SHA512，与主会话证据一致；已静态核对setup参数与Codex配置行为。下文关于npm/TLS不可用与发布版本未核实的文字仅为历史记录，已由REA-PROPOSAL.md v2取代。没有执行setup或新增持久权限。
> **目标更正（2026-10-18，后续用户明确指令优先）**：交付是独立恶搞游戏，不是从原版 mod 菜单加载的模组。“保留存档机制”仅指新玩家在新工程正常存读档，不要求原版角色兼容。本文件下方早先 mod 接口调查只作为格式与工具背景，不代表交付路线；所有 mod 构建/装载/原版存档兼容相关规划已由 PLAN.md 与 STANDALONE-AUDIT.md 替代。没有生成 mod 骨架或构建产物。
# Grim Dynasty 第一阶段审计

审计日期：2026-10-18。状态：只读盘点与设计完成；没有构建或运行 mod。

## 已验证事实

| 项目 | 本机证据与结论 |
|---|---|
| Steam 根目录 | D:\Steam；libraryfolders.vdf 中 path 一致 |
| 游戏目录 | D:\Steam\steamapps\common\Grim Dawn；由 steamapps\appmanifest_219990.acf 的 appid=219990、installdir=Grim Dawn 推导并检查目录存在 |
| Steam 基线 | buildid=25813250；TargetBuildID 同值；StateFlags=4。不能以此替代文件完整性验证 |
| 补丁号 | 两个架构 EXE 的 FileVersion/ProductVersion 均 0.3.0.0，不是可靠的发行补丁号；尚未启动游戏查版本 |
| DLC 安装证据 | manifest 中 dlcappid 483840、642280、897670、2699230；分别对应 Crucible、Ashes of Malmouth、Forgotten Gods、Fangs of Asterkarn；这是已装 depot 证据，不是账户授权查询 |
| 数据库 | database\database.arz 58,529,720 bytes；database\templates.arc 868,124；gdx1\database\GDX1.arz 42,420,279；gdx2\database\GDX2.arz 33,106,774；gdx3\database\GDX3.arz 48,508,393 |
| 官方工具文件 | AssetManager.exe、DBREditor.exe、Editor.exe、QuestEditor.exe、ConversationEditor.exe、ArchiveTool.exe、MapCompiler.exe、ModelCompiler.exe、TextureCompiler.exe、Viewer.exe 等存在；仅确认存在，未启动 |
| 官方文档文件 | Grim Dawn Modding Guide.pdf 6,533,036 bytes；ModdingTutorial.zip 540,303 bytes；PDF 尚未解析，不能声称已读其章节 |
| 文本与资源 | resources 中 Text_EN.arc、Text_ZH.arc、Quests.arc、Conversations.arc、Scripts.arc、Levels.arc、UI.arc、Items.arc 等存在；未解包 |
| 现有 mod | mods 下见 survivalmode；未修改 |
| 工程初始状态 | D:\MyGitRepo\Grim-Dynasty 初始仅见 .git；git ls-files 无输出；HEAD 不存在（fatal: Needed a single revision）。未发现工程及上级指定位置的 AGENTS.md |

初次 git status 输出全局 ignore 文件 Permission denied 警告，没有据此声称完整 clean。随后指定 NUL 的查询失败（fatal: cannot use NUL as an exclude file）；未更改 Git 配置。没有 commit、推送或重置。

## 基线 SHA256

- database\database.arz：9E3532250CDB611A4CC989997C8F97FCEC2D6C422063A845E6C43CD538C30603
- database\templates.arc：150A4D84D0336231BDF3D43D9036BDD4F6156BF1E25CE436560D021C9C4276D3
- Grim Dawn Modding Guide.pdf：28A0738F3812AFD57CFCC17596DB85D02032691F1FAD3D08D132A39FF192B469

## 接口审计与待验证事项

以下为官方 mod 工作流的候选接口，并非本轮已核对的本机 DBR 字段路径。禁止在未提取模板验证前杜撰可用字段。

| 内容 | 首选接口/工具 | 下一步验证 |
|---|---|---|
| 数据记录 | Asset Manager、DBR Editor、DBR 源记录编译为 mod ARZ | 只提取必要模板与少量基线记录到工程私有工作区；查继承、资源引用与 DLC 覆盖顺序 |
| 文本 | tag 键值文本编译为 mod 资源 | 编码、中文字体、语言覆盖优先级、重复 tag 行为；保持 tag/record 稳定标识 |
| 地图 | World Editor（Editor.exe）、MapCompiler | 源地图可取得程度、区域连接、导航、出生/传送点、代理与地形编辑；不假设打包 Levels.arc 等于可编辑源地图 |
| 对话/任务 | ConversationEditor、QuestEditor、脚本资源 | 任务 ID、token、触发器、Lua 沙箱 API 与地图对象绑定 |
| 职业/技能 | DBR 模板、技能树 UI/位图资源 | 掌握栏、技能链接、名称图标与数值字段白名单；先做名称包装，不新增职业槽 |
| 星座 | devotion 相关 DBR、技能与 UI 布局资源 | 星点上限、星座/亲和 ID、连线与界面坐标、重置及触发绑定；28 组设计先不写数据 |
| 模型/材质 | 官方模型/纹理编译器、Viewer、已有 Max 插件 | DCC 版本、骨骼/动画兼容、碰撞和导出链；插件存在不表示现代 DCC 可直接使用 |

保持引擎、难度、存档机制、战斗、背包、商店、阵营子系统以及除星座外的数值。地图与任务拓扑变化会改变经验获取节奏、刷怪密度和经济产出，即使单体数值不动也必须回归比较。

## Rea 状态与安装提案（未执行）

会话工具清单没有 Rea MCP；Get-Command rea 没有结果；标准用户 npm bin 下未找到 rea shim；Codex 配置未见 rea MCP 项。不能排除其他宿主/私有目录存在安装，但本会话未接通。

本机 node.exe 为 C:\Program Files\nodejs\node.exe，v24.14.1。2026-10-18 所读 GitHub main/package.json 显示 rea-agents 6.6.0，Node engines 为 ^22.19.0 || ^24.11.0 || >=26.0.0，本机满足源码声明。main 可变，npm 发布版本及完整性尚未确认。

只读请求 https://registry.npmjs.org/rea-agents/6.6.0 失败：The SSL connection could not be established, see inner exception. 网页工具访问 registry 也返回 Internal Error。没有运行仓库代码、npx、setup、doctor 或新增权限。

主会话若决定安装，先解决 npm TLS 并核实 tarball/integrity/版本，再审阅以下精确拟议命令（本文件不是执行许可）：

```powershell
[已撤回：6.6.0未发布；仅参考REA-PROPOSAL.md v2中的6.5.0命令]
# 审阅实际计划并获确认后才可能执行：
[已撤回：6.6.0未发布；不得执行]
```

来源：https://github.com/morluto/rea 和其链接的 rea-agents npm 包。预期宿主配置为 C:\Users\Administrator\.codex\config.toml（当前文件已只读查看）；实际路径、备份与启动命令以 dry-run 为准。npx 会下载并执行包，即使 dry-run 也不是“只读源码查看”。--skill=false 避免安装个人 skill；不允许安装 Hopper 或其他依赖。用户级 MCP 注册会供该 Codex 宿主持续调用，并不天然限制在本工程，文件、进程、网络权限不能以一句“只读分析”保证；须单独限制权限。仅在官方接口确实缺失时考虑静态结构分析，禁止进程注入、DRM 绕过或修改游戏。

参考源码：https://raw.githubusercontent.com/morluto/rea/main/package.json ；https://raw.githubusercontent.com/morluto/rea/main/docs/installation.md 。本机官方 PDF 是首选文档；在线 Grim Dawn modding 地址本轮均返回 Internal Error，未拿来补证。

## 阻碍与安全边界

PDF 解析器 pypdf/pdfplumber/fitz 均未安装；pdftotext 不在 PATH。bundled workspace dependency 接口返回 Workspace dependency tools are only supported for local threads. 不安装依赖，文档内章节与具体模板字段待验证。

本轮未访问 Steam userdata/云存档、真实存档或远端云文件；未下载云存档、修改原版安装/Steam 设置、迁移存档、运行游戏/编辑器。工程只新增审计设计文件，不复制或发布原游戏资源。