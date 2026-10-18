# 第四轮：初始化链、资源入口与独立宿主go/no-go

2026-10-18。本轮切换至初始化与资源挂载，不重复保存路径。目标仍为保留原底层代码的独立游戏。本报告给出静态“部分顺序”，不是可以照抄执行的初始化配方。

## 决策

**NO-GO：目前不安全尝试执行链接/加载原Engine/Game的新独立宿主。** 已有证据足以证明初始化含文件写入、线程和设备副作用，且依赖不完整的全局对象、窗口/设置结构、C++ ABI与错误清理契约。试调用可能在未知路径产生文件、启动线程或触及平台服务；与当前仅只读原安装、不运行目标、不改Steam/DRM的范围不符。本轮没有制作或执行宿主，也没有通过注入/补丁/伪造平台身份来补齐契约。

GO范围：继续不执行目标的规格整理、原创资产与记录设计、经确认输出范围的独立工具格式实验。源码/SDK及合法独立运行时材料缺失不能靠继续零散反编译消除；本轮到此停止，不递归扩展其余函数。

## 实际证据与预算

先做主程序导入表和有限一次.text FF15/FF25调用候选搜索，随后按实际函数大小选择8个明确目标，入口unwind/叶函数区间合计5677字节，单个最大3049字节。官方Ghidra12.1.4 headless配置-noanalysis/-readOnly/-deleteProject、512M堆、-max-cpu 1；Engine/Game分模块处理。没有整库自动分析，但PE仍完整导入用于映射和符号，512M是Java堆配置，不是RSS硬限制。

8个反编译请求成功，完整记录于Engine/Game-function-evidence-bounded-v2.json及followup子目录。PE异常表有时只描述入口unwind区域，并非整个语义函数：InitializeObjects的尾跳、ShutdownObjects的外部基本块以及共享辅助例程被Ghidra展开。故字节合计仅表示显式选择区域，不能声称所有伪代码严格局限其中。本报告对这些函数的确认结论优先使用区域内指令/引用，不把展开内容当作完整函数验收。

## 文字化调用顺序清单

### 0. 原宿主入口与跨模块总顺序：未知

主EXE普通导入包含Engine构造/析构、Engine::Initialize、InitializeObjects/ShutdownObjects、LuaManager::Initialize与GameEngine::PostLuaInitialize等。这证明存在链接接口，不证明真实运行顺序。

主EXE入口RVA0x448310位于名为.bind的节。对.text的一次有限FF15/FF25导入目标候选搜索未命中所选入口。可能还存在其他调用形式、跳板或未解释引导层；结果不能用来断言加密、特定DRM或没有这些调用。本轮未分析入口层正文、解包、执行或绕过它。因而下面几个局部链之间的全局先后尚无法从主宿主证明。

### 1. Engine构造：已观察局部行为，外部前提未知

Engine::Engine位于0x180072930，选择1579字节。观察到写入虚表、初始化字符串/容器与多处成员、分配临界区相关存储并InitializeCriticalSection、设置计时器/Display/GameInfo对象、取得线程ID等。

不能把它当成纯内存清零。所选Engine::Initialize还会读取全局gEngine；gEngine赋值步骤、宿主对象总大小、GameEngine与Engine对象关系及完整构造/析构契约尚未建立。不要据伪代码参数或字段最大偏移猜一个足够大的缓冲区来调用构造器。

### 2. FileSystem归档源注册：局部入口已证实，调用时机未知

FileSystem::AddSourceArchive位于0x1800869d0，选择274字节：

分配helper(0x90) → FileSourceArchive构造 → Attach(path,另一个字符串参数) → 成功时将对象指针追加到partition相关容器并返回1；失败时执行虚表第0槽、带实参1的清理形态调用并返回0。

这证实了“先附着归档、成功后注册到分区容器”的局部机制；不能证明partition枚举含义、源优先级、所有ARC自动发现、完整所有权或应由谁释放。失败调用看似MSVC deleting-destructor模式，但未验证虚表与完整析构，不据此生成释放API。

它没有在所选Engine::Initialize的直接调用中出现，所以不能擅自把它排成启动第一步；主宿主可能预先准备FileSystem、也可能使用其他注册路径，仍未知。

### 3. Engine::Initialize：正常后续路径的顺序已观察

0x1800743d0，选择3049字节；导出签名显式依赖EWindow、GraphicsEngineSettings、FileSystem、C++字符串及多项布尔参数。局部顺序为：

1. 保存传入FileSystem指针及部分标志到Engine对象。
2. 以对象已有字符串组合.html/.xml日志名，调用_chmod，并用fopen_s的wt/wb模式打开文件；还调用线程亲和性相关API。**真实执行有写文件/线程副作用**，所以不能作为“只读试调用”。本轮只看代码，未执行这些操作。
3. CPU检测、Options与LocalizationManager准备。存在全局gEngine及虚函数依赖。
4. 分配并构造GraphicsEngine，调用CreateDevice(window,settings)；有失败处理分支，但无正式契约说明传空窗口即可安全跳过。
5. 创建日志类别，调用LoadMainDatabase（直接调用点0x1800748b3）。
6. 从Options/CPU相关数据计算资源线程参数，构造ResourceLoader，调用StartThreads（0x18007490d）。
7. 构造EffectResourceManager、ImageResourceManager、ConversationResourceManager，传入同一ResourceLoader；调用GraphicsEngine::Initialize。
8. 构造/初始化Physics、Input、Sound、Weather、连接管理器及World等。部分有条件分支，具体布尔语义和错误回滚仍未知。

这是函数内的静态部分顺序，不是每条都必然执行的运行追踪。不能仅凭某个分支跳过Input便宣布引擎支持安全无窗口独立宿主。

### 4. 主数据库：明确的加载调用序列，不等于覆盖语义已知

LoadMainDatabase位于0x180073a60，选择551字节，直接引用和短常量独立复核表明：

清TableDepot缓存 → 清理并替换Engine持有的数据库对象 → 基路径字符串 + 以下后缀，按顺序调用LoadDatabase：

```text
/database/database.arz        无条件调用
/gdx1/database/gdx1.arz       由对象标志0xc25控制
/gdx2/database/gdx2.arz       由对象标志0xc26控制
/gdx3/database/gdx3.arz       由对象标志0xc27控制
/mods/database.arz           由对象标志0xc21控制
```

最后的原有mods路径只是被观察到的代码能力，不是本项目交付路线。这里确认的是LoadDatabase调用顺序；同名记录是覆盖、合并、拒绝还是另有策略，要看尚未分析的LoadDatabase/Archive契约，不能以加载顺序直接宣布“后者必然覆盖前者”。数据库对象基路径、标志来源及失败策略也未全部建立。

### 5. Game对象入口：创建链已证实，总启动位置未知

InitializeObjects导出位于0x18036b240，入口区间47字节。直接指令给出：

GetCurrentThreadId → 分配helper，实参0x37868 → GameEngine构造器0x1802c07a0 → 写全局gGameEngine → 尾跳GameEngine::Initialize（0x1802d7230）。

这排除了“InitializeObjects只是无状态注册列表”的猜测：它确实创建全局游戏对象。0x37868是本构建观察到的分配请求，不是可跨版本依赖的公开对象大小。GameEngine构造器约5127字节，本轮没有显式分析；Initialize的尾跳展开伪代码不算其完整契约已验证。该入口应在Engine初始化前还是后、会否在内部设置gEngine，仍不能据当前证据定序。

### 6. Lua后阶段：前提已证实，宿主调用顺序未知

GameEngine::PostLuaInitialize位于0x1802f3450，选择101字节。它读取全局gEngine，取其LuaManager并GetState，随后传入常量QuestEventDispatch做相关辅助调用，结果作用于GameEngine内部成员。

因此至少要求全局Engine与LuaManager/State已经有效；不是一个可在任意时刻调用的独立函数。哪个阶段创建LuaState、加载了哪些脚本、如何处理缺失QuestEventDispatch，以及它与InitializeObjects之间的完整顺序，仍需正式契约。

### 7. 退出/销毁：不能给可执行配方

ShutdownObjects导出入口区间16字节能确认读取/检查gGameEngine；Ghidra继续展开了区间外的Shutdown/析构/释放形态代码。不能把该区间的反编译状态true当作完整销毁验证。

资源线程等待、Lua预关闭、Game关闭、数据库/资源管理器、FileSystem/设备及平台退出的逆序、异常回滚仍未证明。现有析构/Shutdown导入名不等于已知正确调用顺序。

## CRT与allocator已知范围

Engine构造、Initialize、LoadMainDatabase和AddSourceArchive多次调用0x1802dc398。60字节辅助函数表现为分配失败后的重试/失败处理形态；其中0x1802ddb38的6字节IAT跳板明确导向api-ms-win-crt-heap-l1-1-0.dll!malloc。主EXE/Engine/Game同时普通导入MSVCP140、VCRUNTIME140及UCRT heap函数，详见crt-dependencies.json。

这确认了MSVC C++运行库与实际malloc依赖，不证明任一模块的new/delete都可由宿主malloc/free替代。Game分配/释放helper的短入口不是同一个简单IAT跳板，本轮未递归追踪。std::string、小字符串布局、RTTI、异常、虚析构、容器与跨模块所有权都需要匹配ABI和明确创建/销毁接口；不能因共享某个CRT DLL就宣称跨模块任意释放安全。

## 当前必须取得的具体材料

1. **正式独立宿主/运行时许可与初始化入口文档**：是否允许独立产品使用Engine/Game及其依赖，平台产品身份和合法启动方式；不能用解包或平台模拟补证。
2. **对应构建的SDK或源码、头文件和导入库**：Engine/Game对象布局、EWindow/GraphicsEngineSettings/FileSystem/Partition、导出函数签名与隐藏返回值、全局指针与单例安排。
3. **ABI/构建契约**：MSVC工具集、CRT链接方式、packing/迭代器调试级别、allocator/异常边界、模块自有销毁函数和线程要求。
4. **生命周期与副作用契约**：日志/配置/缓存输出根、设备和线程开关、错误回滚、Lua创建与脚本集、退出join和销毁次序。现有只读范围不允许拿原版执行来试错。
5. **资源与内容运行包规格**：FileSystem分区及优先级、ARC挂载/ARZ加载冲突规则、原创内容所需的最小资源闭包与可发行依赖。

拿不到这些材料时，应保持NO-GO，不继续把零散导出逐个试调用。此处的停止不是权限拒绝；是静态材料尚不能支持安全独立宿主，且目前范围禁止目标执行。继续开发可独立构建的新引擎或转为mod都属于项目性质改变，未自行采取。

## 证据与完整性

call-sequence.json给出逐步status、证据位置和未知项；initialization-locations.json保存定位范围和导入；main-entry-metadata.json仅记录节/入口元数据；constant-and-crt-thunk-evidence.json记录资源名、日志模式和malloc跳板。各函数完整伪代码/指令及日志保留，不能将推导伪代码当原始源码。

末次主EXE、Engine/Game SHA256均与定位输入一致。没有运行目标、访问存档或Steam设置、修改原版、系统安全/全局环境、新装软件、提交或发布。本轮不扩写内容设计，不改变用户历史势力核心。
