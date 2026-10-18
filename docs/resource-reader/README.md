# 本地只读资源解析器：阶段验收

> 验证对象澄清（2026-10-18）：自写模块的编译、运行和离线测试不依赖原版DLL、Steam、BIOS或Sandbox；这些隔离/授权门槛仅约束合法原版的动态行为对照，不阻塞自写实现。最终目标是独立的行为等价可编译代码，当前尚非完整游戏。详见工程 docs/VALIDATION-TRACKS.md。

2026-10-18；目标安装 `D:\Steam\steamapps\common\Grim Dawn`。

已实现可运行的 Python 标准库解析器，非引擎重建或独立游戏宿主。输入接口仅接受该安装目录中的 `.arz/.arc`；使用只读 mmap。无存档接口、重打包接口、DLL 加载或游戏启动接口。低层构造器接受内存字节以测试合成样本。

## 运行

在 `tools/resource_reader` 执行（需要 Python 3.11+；本机验收为 3.14.5）：

```powershell
python test_gd_resources.py
python gd_resources.py 'D:\Steam\steamapps\common\Grim Dawn\database\database.arz' --match 'records/skills/playerclass01/*' --limit 10
python gd_resources.py 'D:\Steam\steamapps\common\Grim Dawn\database\database.arz' --record 'records/items/gearweapons/swords1h/b000_sword.dbr'
python gd_resources.py 'D:\Steam\steamapps\common\Grim Dawn\resources\Quests.arc' --quest-member 'gdareab/mq_murdersandworse.qst'
python accept_resources.py --output acceptance-local.json
```

验收脚本创建新 JSON，拒绝覆盖已有输出；默认读取项目 `.tooling/resource-reader-validation` 中已经导出的少量官方对照样本，不会启动官方工具。没有对照样本时不会声称完成官方交叉验证；见结果字段 `official_comparison` 和 `official_arc_comparisons`。本次正式报告全部具备。CLI 可输出原版文本，仅供本机研究，不应打包进发行版。

## 已验证格式

| 部分 | 实现与验证范围 |
|---|---|
| ARZ | 24 字节 `<HHIIIII` 头，marker=2/version=3；变长记录索引、字符串表；记录数据偏移为索引保存值+24；raw LZ4 解压 |
| DBR 字段 | `<HHI` 类型/数量/字段名索引；0/3 为 int32，1 为 float32，2 为字符串索引；保留零值与数组，拒绝越界和重复字段 |
| ARC | 28 字节七个 uint32 头，magic=0x435241/version=3；12 字节分块表，文件名表，44 字节成员表；直接块及混合 LZ4/原始分块；Adler32 |
| 文本 | UTF-8/BOM 与 UTF-16 BOM；tag=value，保留归档成员和行号 |
| QST2 | version9；offset8 的 uint32 定位本地化目录；语言与字符串均为长度前缀；逐项含空字符串，严格读到文件尾 |
| 引用 | DBR 字段可枚举资源/tag 引用并连接英文文本；QST 图区资源字符串仅为候选观察，不等于已解码任务条件、动作或图边 |

解压单对象上限64MiB，QST观察扫描上限8MiB。格式只对本机已验版本负责；不宣称兼容未来补丁或其他 ARC/ARZ 版本。未做 DLC 覆盖优先级合并，也未覆盖地图、网格、动画、材质、音频及完整任务操作码。

参考阅读了 [gd-edit ARZ 格式实现](https://github.com/Odie/gd-edit/blob/master/src/gd_edit/io/arz.clj) 与 [ARC 格式实现](https://github.com/Odie/gd-edit/blob/master/src/gd_edit/io/arc.clj)，仅阅读，未下载执行其代码。此处为自行编写的 Python 实现。Rea 实际 stdio MCP 使用固定6.5.0，Ghidra12.1.4，对本机官方 ArchiveTool 静态分析；证据在 `rea/`。

## 验收结果

- 10 项合成边界测试通过：LZ4 字面量/重叠复制、截断/无效距离/超限、混合分块、校验错误、字段类型/索引、零值/数组、本地化空字符串/尾部检查、输入路径白名单。
- 基础数据库完整索引34,180条；选定 Soldier mastery 的644字段、Gladius 的663字段和Scrap任务道具的38字段，共1,345字段，与官方单条 DBR 导出全部一致。浮点按官方十进制输出以相对/绝对5e-6容差比较；未修改数值。
- 基础英文归档11成员全部解压并校验，读取11,956个tag。技能/物品两份文本与官方导出字节SHA256一致。
- 基础任务归档247成员全部解压并校验，全部本地化目录严格解析通过。样本 `gdareab/mq_murdersandworse.qst`：The Outlaw Threat / Murderers and Worse，25个本地化字符串槽，包括空槽；任务关联候选Scrap DBR存在且可解析名称。样本任务原字节与官方导出SHA256一致。
- 正式 JSON：`acceptance.json`，包括来源行号、全部样本任务文本、输入哈希、官方比对和明确覆盖边界。没有将全量原版资源复制到工程。

## 官方工具断言事故与修复

本任务调用 `ArchiveTool.exe` 时，`-extract` 的输出根目录/任务嵌套目录尚未创建，触发用户所见 `archivewriter.cpp:848 / outputFile != INVALID_HANDLE_VALUE` 原生断言。不是 MCP 注入错误。程序等待弹窗造成超时；Python `subprocess.run` 在超时后终止其自己启动的子进程。没有点击 Ignore Always、没有假定这些调用成功、没有终止别的任务进程。

cwd 为 `D:\MyGitRepo\Grim-Dynasty\.tooling\resource-reader-validation`，所有输出都在其下。先建立 `tags`，后建立 `quest/gdareab`，相同抽样参数重试成功。官方 `-database` 的单条输出参数是完整目标文件名，而非目标目录，已通过 Rea 分派函数核对。成功导出仅：mastery.dbr、sword.dbr、scrap.dbr、tags/tags_skills.txt、tags/tags_items.txt、quest/gdareab/mq_murdersandworse.qst。

`official-commands.json` 保留任务样本超时；`official-quest-retry.json` 保留修复后exit0。最早缺少tags目录的60秒超时单独记录在 `incident.json`。最终没有ArchiveTool进程残留。输入hash复核见 `incident.json`：数据库与首阶段审计基线一致；ArchiveTool与Rea分析基线一致；两份ARC在首次验收（首次失败之后）及后续重试前后相同。早于首次尝试的ARC hash未记录，不夸称该时间段已有hash证明。源码操作范围与命令均为读取归档/工程内导出。

## Rea 审查结局

曾因自动审查未识别既有批准，拒绝固定 `npx -y rea-agents@6.5.0 mcp` 的调用。核对持久配置、缓存版本和已验证tarball后，以同一脚本/同一启动方法重试一次获准，实际完成 open_binary、search_strings、search_procedures、procedure_pseudo_code、xrefs、close_binary。没有更换启动路线；当前没有待批阻塞。反编译为Ghidra伪代码，不能当成原始源码。

## 下一阶段边界

资源访问第一切片已验收；尚未获得独立宿主可运行结论。下一步可沿静态资源加载器追踪Archive/Database的内存所有权、ABI、分配释放和生命周期，先完善数据契约；任务图操作码仍需独立验证。先前宿主初始化有日志、设备、线程等副作用，不能以调用Initialize代替隔离验证。动态运行、额外调试权限或新隔离环境仍须按约定先报告范围。此阶段没有原版游戏启动、真实/云存档访问或独立宿主试运行。

