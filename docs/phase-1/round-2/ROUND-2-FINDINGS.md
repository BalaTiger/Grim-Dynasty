# 第二轮：有界函数、路径分流与独立构建结论

2026-10-18。已阅读上一轮STANDALONE-BOUNDARIES.md的最小验证计划。本轮针对6个明确函数做静态反编译/直接引用核对，并完成原创阵营及10职业内容方案；不重复crashreport、不启动目标程序、不打开任何存档，不分析或修改Steam认证。

## 实际方法、预算与证据

使用已批准Ghidra12.1.4与JDK21.0.12.1+1。官方本地support/analyzeHeadlessREADME.md确认-noanalysis、-readOnly、-max-cpu及-deleteProject选项。本轮未使用Rea默认的整库auto-analysis：直接调用同一官方Ghidra headless，-noanalysis导入只读原DLL，脚本仅显式反汇编/反编译所选函数；临时数据库、脚本、日志均在工程内。它是既有Rea后端的有界补充工作流，不冒称本轮结果来自Rea tools/call。

JVM堆配置512M，-max-cpu 1；两个模块依次处理，没有并行大库分析。PE仍须完整导入以保留映射、导入表和符号，不能把-noanalysis说成仅读取844字节或完全不占模块内存。这里的有界预算是6个显式选定函数体共844字节，不是总文件读取量或进程RSS硬上限。

| 模块与函数 | 首选VA | 显式函数范围 | 直接证据 |
|---|---|---:|---|
| Engine::SetSavePath | 0x1800738e0 | 25字节；局部RET/尾跳转 | Engine-function-evidence.json与bounded-v2、TSV、指令及引用 |
| Engine::GetBaseDataPathFromOptions | 0x180073700 | 464字节；PE异常表范围 | Engine-function-evidence.json |
| Engine::GetDatabaseArchive | 0x1800777d0 | 8字节getter | Engine-function-evidence.json |
| GameEngine::GetRootSavePath | 0x1802c7f50 | 95字节；PE异常表范围 | Game-function-evidence.json |
| GameEngine::GetUserSaveDataFolder | 0x1802e3270 | 157字节；PE异常表范围 | Game-function-evidence.json |
| GameEngine::GetBaseFolder | 0x1802e3100 | 95字节；PE异常表范围 | Game-function-evidence.json |

六个decompileCompleted均为true。可读伪代码在Engine-selected-functions.pseudo.c和Game-selected-functions.pseudo.c；完整指令、outgoingReferences与限制保留在JSON。地址均绑定本机已记录SHA256，不是跨版本稳定API。

### 有界分析的重要限制

SetSavePath尾跳到共享字符串辅助例程0x180023180。Ghidra伪代码会展开共享尾调用，甚至尝试声明不透明目标后仍有展开；因此其整段伪代码不等于25字节函数体的原始代码。setter结论只以显式指令和直接引用为准，不借展开部分认定完整字符串实现。未做整库自动分析，但不能声称反编译器从未读取辅助例程字节。

脚本中的AutoAnalysisManager.setIgnoreChanges被Ghidra提示不在分析线程因而无效；它不构成限制保障。实际保障依据是明确-noanalysis启动参数、所选AddressSet、未调用analyzeAll、串行运行和可复查日志，不依据该无效方法。日志和JSON不隐藏这些诊断。

## 从未知推进出的结论

### 1. Game保存根路径包含固定产品身份片段

GetRootSavePath在0x1802c7f6f直接调用尚未解释的FUN_180593560，随后在0x1802c7f82向字符串处理例程FUN_180301350传入常量地址0x180693d00。对该地址的独立文件字节读取确认ASCII内容为 `\My Games\Grim Dawn`，不是仅从导出名推测。

GetBaseFolder在0x1802e311f直接调用GetRootSavePath，并在0x1802e3132向同一字符串处理例程传入单个反斜杠常量。**已证实**：原产品身份字面量参与该路径构造链，不能把“存在SetSavePath导出”作为新产品已完全隔离的证据。**未证实**：FUN_180593560返回哪种系统目录、字符串辅助例程完整语义、全部调用方是否使用此链；没有在磁盘上解析、访问或测试这一保存路径。

检查FUN_180593560的32字节入口发现它不是简单FF25导入跳板；此前可通过IAT直接命名该调用的猜测已排除。下一次需要单独给它分配函数预算，而不是把它当已知GetDocumentsFolder函数。

### 2. 用户保存子目录确有状态分流，状态含义未知

GetUserSaveDataFolder先初始化输出字符串，再测试对象偏移0x375ea处的字节。为零的分支在0x1802e32c0调用GetBaseFolder；另一分支跳过该前缀步骤。二者随后都在0x1802e32fe向字符串处理例程FUN_180089e10传入地址0x18069a2b8和长度5。文件字节确认常量为 `save\`。

**已证实**：是否走BaseFolder前缀受该状态位控制，后续使用相同save常量；这是具体的控制流分叉，不是只看函数名。**未证实**：该字节代表云/本地、主战役/其他模式、可移植模式或别的状态；本轮不擅自命名为CloudSave。字符串helper未逐条验证，所以“另一分支最终一定解析到当前目录”仍不能作为运行事实。

### 3. Engine资源根输入不是无条件透传

GetBaseDataPathFromOptions直接调用Engine::GetOptions与Options::GetString，观察到选项枚举实参1。函数检查输入末尾斜杠、调用FindFirstFileExA，并按返回与文件属性判断进入不同分支；伪代码显示属性等于0x10才走保留路径分支，失败分支传入空字符串常量，非空结果处理反斜杠常量。

**已证实**：存在基于选项字符串和文件系统查询结果的验证/规范化控制流，不能把任意资源路径当作已接受。**未证实**：枚举1的公开选项名、默认来源、优先级、完整ARZ/ARC挂载规则；不凭符号名编造命令行开关。FindFirstFileExA只作为静态导入调用证据，本轮没有执行此查询。

### 4. 保存setter与数据库getter依赖已初始化C++对象

SetSavePath的显式指令先把this调整到this+0x240，与源引用做同址比较，然后尾跳共享例程；上一轮GetSavePath返回的正是同一字段地址。GetDatabaseArchive返回this+0x3d0中的指针。它们不是无状态的C风格路径/数据库API。

**已证实**：这两个接口操作对象内部状态，字段偏移与上一轮局部字节证据一致。**未证实**：构造器何时完成这些状态、对象与allocator/CRT生命周期、调用线程、数据库何时可用。伪代码对含std::string返回值的函数产生多个undefined8参数，而导出签名表明有隐藏返回缓冲等ABI因素；不能把这些参数直接复制成可编译头文件。

## 本轮仍未跨过的独立构建边界

原Game路径链含原产品身份常量，且不能证明Engine setter覆盖所有Game路径。为了保持“不改原二进制、不改Steam/DRM、不碰原存档”，本轮不会修改常量、拦截/注入函数、伪造平台身份或运行原版来观察落盘。

能够继续的是只读静态调用契约审计和原创内容规格。不能据当前证据保证仅换资源/启动器就成为独立游戏。缺口仍包括授权源码/SDK或合法独立运行时、对象初始化和稳定ABI、全部文件路径覆盖以及完整资源依赖。反编译成功没有提供这些材料。

## 下一次最小静态实验

1. 仅对FUN_180593560查询PE异常表边界，预算不超过1KiB，识别直接调用的系统API/辅助函数和输出字符串来源，不调用它、不开目录。目标是解释根路径的基路径；如果超过预算先报告而非整库分析。
2. 只验证FUN_180301350、FUN_180089e10中必需的字符串组合语义，或通过现有编译符号/有界指令建立等价证据；不自动递归反编译所有callee。
3. 用有界字节/指令检索定位0x375ea状态位的显式写入候选，逐项核对少量setter；在证据不足前继续称“状态位”，不凭路径名猜云/本地行为。
4. 将初始化列成契约清单：Engine对象获取/构造、Options准备、资源源注册、数据库挂载、Game状态初始化。每次仅验证一个具体调用边；创建独立宿主、运行写档或改平台行为不包含在上述静态实验中。

## 原创内容交付

MING-FACTIONS-AND-MASTERIES.md给出首批阵营、10职业包装、3种农民军/女真与灾异映射方案、虚构“裂甲巨魁”精锐和据点任务切片。保留数值/子系统，28宿为唯一明确数值例外；不把真实族群整体非人化。该稿为原创设计，不是已修改数据或已完成全部本机阵营枚举。

## 执行问题与副作用如实记录

首轮启动因原路径空格被拆分，在导入前失败；日志显示读取了默认Ghidra工具偏好，并执行其工具文件缓存维护。没有游戏或存档读取，但这不符合项目专属运行目录的预期。随后用严格引号的PowerShell包装参数，并将子进程USERPROFILE/HOME/APPDATA/LOCALAPPDATA/TEMP/TMP显式设为工程内路径；成功运行日志确认偏好和缓存位置在工程内，没有全局环境修改。未删除或“清理”默认用户工具目录。

另一轮在开始处理前因Ghidra不接受工程路径中的点开头组件失败；改用同一工程内analysis-runtime/round-2/projects，属于合法名称修正，不改权限或安全策略。该目录只存临时数据库，-readOnly/-deleteProject，不作为可交付游戏工程。

Engine/Game成功处理后重新计算SHA256均匹配前轮，见input-integrity.json。未接触原存档、云存档、Steam设置；未启动目标模块、提交或发布。没有新增软件、系统安全设置或项目性质变更。
