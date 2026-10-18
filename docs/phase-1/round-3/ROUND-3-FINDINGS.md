# 第三轮最小静态实验：保存基路径与分流

2026-10-18。本轮仅完成上一轮约定的小实验后停止，不扩大为完整保存系统逆向，不扩写内容设计，不运行原程序或任何保存函数。

## 已完成与预算

沿用官方Ghidra12.1.4/JDK21，-noanalysis、-readOnly、-deleteProject、-max-cpu 1，堆配置512M。Game.dll完整导入用于地址/符号映射，但没有整库自动分析。显式反编译4个函数，共667字节，全部成功：

| 地址 | 目标 | 显式范围 |
|---|---|---:|
| 0x180593560 | 保存基路径来源 | 240字节，小于1KiB上限 |
| 0x180301350 | 字符串组合辅助A | 98字节 |
| 0x180089e10 | 字符串追加辅助B | 322字节 |
| 0x1802ef740 | GameEngine::SetCloudStorage | 7字节叶函数 |

证据Game-function-evidence-bounded-v2.json含伪代码、选定范围指令及直接引用；Game-headless.log/Game-script.log记录执行，save-path-selected-functions.pseudo.c便于阅读。范围、命令包装和脚本保留。共享尾调用可能被反编译器展开，显式函数字节数不是所有底层读取或伪代码涵盖字节的硬上限；未递归请求其他函数。

## 新结论一：基路径是Documents特殊目录

FUN_180593560在0x180593589和0x1805935bc通过IAT地址0x18060b778调用SHGetSpecialFolderPathA。指令明确将第三参数寄存器R8D设为5、第四参数R9D设为0。Microsoft官方Windows SDK头文件定义CSIDL_PERSONAL=0x0005，对应Documents/My Documents；API文档说明fCreate=0不请求创建目录。这里仅使用文档解释静态参数，没有在本机调用该API或解析用户Documents位置。

该函数在API成功、缓冲非空的正常分支把结果构造成字符串，并把正斜杠转换为反斜杠；失败后有第二次相同API查询，全部失败路径的安全性未作完整证明。

反编译文本对该Windows API显示了额外第五参数；官方原型只有四个参数，必须以调用指令与官方原型核对，不把伪代码原样当作可编译调用声明。

官方证据：
- https://raw.githubusercontent.com/microsoft/win32metadata/main/generation/WinSDK/RecompiledIdlHeaders/um/ShlObj_core.h ，CSIDL_PERSONAL定义及API原型；本机Windows SDK10.0.26100.0头文件也核对同值。
- https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/nf-shlobj_core-shgetspecialfolderpatha ，四参数原型与fCreate语义。
- https://learn.microsoft.com/en-us/windows/win32/shell/csidl ，Documents语义。特殊目录可因系统配置而异，本轮不猜绝对盘符或目录位置。

## 新结论二：两处辅助例程支持字符串拼接推断

FUN_180301350扫描第三实参的NUL终止长度，将该字符串和长度传给FUN_180089e10，再组织输出字符串。后者的非别名分支将新长度计算为旧长度+追加长度，确保容量，向旧末尾位置复制数据，更新长度并补NUL。其别名分支及分配/复制callee没有递归展开；这不是完整std::string实现的形式证明。

结合上一轮已核对的直接调用与常量，正常本地分支的静态表达式为：

```text
Documents特殊目录
  + \My Games\Grim Dawn       (GetRootSavePath)
  + \                         (GetBaseFolder)
  + save\                     (GetUserSaveDataFolder的本地前缀分支)
```

这比“字符串出现在二进制里”更强：有明确调用边、常量地址、长度和拼接行为证据。但它只是正常返回分支的静态构造表达式，不是本机已解析/已打开的路径，也不证明所有保存类别都经过它。

## 新结论三：0x375ea是SetCloudStorage写入的字段

一次有限.text字节扫描共得到61处0x375ea位移字节候选，其中只识别出两种直接写入候选，详细位置和上下文见state-displacement-candidates.json。字节命中可能属于其他对象/指令/数据，不是61条已验证字段访问。

其中Game.dll RVA0x2ef740与导出名?SetCloudStorage@GameEngine@GAME@@QEAAX_N@Z精确对应，实际7字节指令为：

```text
MOV byte ptr [RCX + 0x375ea], DL
RET
```

因此上一轮GetUserSaveDataFolder测试的同一对象字段，现在可以有证据地称为GameEngine的CloudStorage标志。结合此前分支：字段为零时调用GetBaseFolder再追加save\；非零时跳过该前缀、在新字符串上追加save\。这是本地绝对根与相对保存键构造的静态分流证据，**不是已验证云同步或Steam远端写入行为**。

另一写入候选位于GameEngine构造器（起点0x1802c07a0，PE异常表跨度5127字节）。它超过预算，本轮未反编译；不据单个候选直接宣称完整初始化顺序或运行默认值已知。未遍历剩余59个候选，也未追查Steam云接口。

## 对独立构建可行性的影响

现在可以停止猜测保存根来源和该分流字段名：原Game正常本地路径链会查询系统Documents并拼入原产品名称，CloudStorage分支构造不同形式的键。此前仅有Engine::SetSavePath导出不足以证明覆盖这条Game路径链，本轮更明确地表明“原二进制不变且仅换启动入口”不能被当作已验证的独立存档方案。

没有据此修改产品路径常量、调用SetCloudStorage、拦截函数、切换Steam设置或测试实际落盘。仍缺：正式初始化与全部文件路径契约、可使用的源码/SDK或独立运行时授权、资源装载完整规格。伪代码没有消除这些缺口。

## 本轮停止点与最小后续决策

本次三个具体未知项已获得新证据，继续无限追踪保存实现的收益有限，先停止这一支审计。若下一阶段需要证明“独立新产品全部文件都隔离”，优先取得合法初始化/路径配置契约和源码/SDK材料，形成测试矩阵；不通过运行原版、读取原存档或修改Steam来补证。仅有 setter/局部路径函数不能满足完整运行验收。

尚未证明实际云上传/下载、共享仓库与难度地图数据的完整写入覆盖、所有状态位写入来源、调用方初始化默认值，也未重建独立宿主。本轮不把这些事项列为已解决。

## 内容设计状态纠正

用户核心要求保持：农民军、女真分别对应两大敌对势力。原型冲突可以用各势力内部的虚构污染分支/邪术军团解释，但不能未经审阅改成完全无关的新组织取代核心。上一轮所有映射仍为候选；跨势力灾异方案不作为已采纳默认。本轮不扩写新方案，不将真实民族整体非人化；巨型兽人感仍只用于虚构变异精锐。

## 完整性与执行范围

Game.dll末次SHA256为cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2，与前轮一致。verification-summary.json保存摘要。输出/脚本/临时数据库均在工程内，目标未运行；未访问原存档、云存档、Steam设置，未修改原版、系统安全或全局环境，未新增软件、commit或发布。
