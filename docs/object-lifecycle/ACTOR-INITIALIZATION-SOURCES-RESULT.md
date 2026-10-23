# Actor 同步初始化来源与自有 writer

本段从 matrix-initialization 检查点继续。新增证据确认 `_initterm_e → 2DC908` 是 `420D88 / 420DB0 / 420DB8 / 420DC0 / 420DC8` 的直接初始化来源。新增自有代码只投影调用顺序、已证槽写入及条件性返回；原有 Actor/matrix 模型的真实同步前提没有自动放宽。

## 依赖顺序与静态证据

先记录[依赖图和读取优先级](ACTOR-INITIALIZATION-SOURCES-PLAN.md)，再读取已有引用指向的表；发现 `2DC908` 后复用旧归档精确 `.pdata` entry11167，没有重读函数边界。两次 dumpbin 均仅接收自建最小 PE（只含选定正文和自行生成的头部/填充），未接收原 DLL 路径，没有加载或执行任何原版代码。

| 批次及实际结果 | 新读范围 | 执行记录 |
| --- | --- | --- |
| [初始化表](offline-actor-initialization-tables/metadata.json) | `[303178,303188)`为`0,1802DC908`；`303198`首槽为0即停止，合计24 B | chunk `249b5c`，无子进程 |
| [初始化正文](offline-actor-crt-initializer/inspection.json) | 精确`2DC908..2DCA26`，286 B / 70条指令，复用成功归档原始边界 | chunk `0801e2`，1次dumpbin |
| [直接导入及字符串](offline-actor-crt-initializer-metadata/metadata.json) | 3个导入的OFT/hint/name共75 B；4个字符串含NUL共104 B | chunk `0bdcb7`，无子进程 |
| [同步初始化跳板](offline-actor-sync-init-thunk/inspection.json) | `2DDB20`固定6 B，FF25目标`3026F8`；OFT0及hint/name共45 B | chunk `6d9f6d`，1次dumpbin |

本段新读取代码292 B / 71条指令、表及导入元数据144 B、字符串104 B，合计540 B语义读取。四批各做前后两次64 KiB缓冲的完整文件哈希，合计8次遍历；哈希遍历不计作局部语义读取。所有批次 Engine SHA256 均为 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`，句柄关闭且无失败。哈希及stat稳定不能证明原子快照。

6 B读取是已知直接调用的导入跳板识别，**不是** VCRUNTIME 实现正文或新的完整函数边界。静态绑定为 `VCRUNTIME140.dll!__vcrt_InitializeCriticalSectionEx`。未读取VCRUNTIME DLL、导入槽运行时payload、原生堆对象或线程状态。

TLS callback数组磁盘首项为零；这排除了该文件表中的回调目标，并不证明加载后表不可变、Windows loader的TLS分配有效，或 `GS:[58] / index420DD4 / block+4` 已可用。已证8 B TLS模板及own逻辑thread工厂继续复用，不重复实现。

## 2DC908 的独立合同

1. `2DC926 → 2DDB20` 调用 `__vcrt_InitializeCriticalSectionEx(&420D88,4000,0)`；返回值未检查。正常返回不等于已证明critical section有效。
2. `2DC932` 调用 `GetModuleHandleW(L"kernel32.dll")`。零结果走 `2DCA15 → 2DD7F4(7)`，后接`INT3`。本段未解析fatal helper，不构造普通错误返回。
3. 依次通过 `GetProcAddress` 查询 `InitializeConditionVariable`、`SleepConditionVariableCS`、`WakeAllConditionVariable`。三次查询全部完成后，才按该顺序逐项判零。
4. 三者均非零：`2DC98C`先清`420DB8`；`2DC997`经已知间接调用guard后，`2DC9A3`调用第一个目标`(&420DB0)`；**在该回调返回后**，`2DC9A5`才读取cookie `41C520`。随后将 `ROL64(wait,cookie&63) XOR cookie` 写`420DC0`，再将wake的同式写`420DC8`。这与既有wait/notify中的 `ROR64(cookie XOR encoded,cookie&63)` 相配。
5. 任一查询为零：`2DC9FC`调用 `CreateEventW(0,1,0,0)`；`2DCA02`先写返回值到`420DB8`，再判零。零结果走`2DCA20 → 2DD7F4(7)`后接`INT3`；这一分支不写两个编码槽。
6. 两种成功分支都在`2DC9D9`向已有`2DC8E0`注册退出回调`2DCA28`；这里没有调用cleanup正文。注册helper的返回值（含-1）被`2DC9E3`清EAX覆盖，所以在所有前置调用正常返回的路径上返回0。不能据返回0推断注册成功或同步有效。

初始化函数没有写thread/global epoch或矩阵数据。原有matrix helper的guard/epoch发布顺序、已知尾12 B初始化和cold/warm/header-existing模型继续沿用。

## 新自有模型与验证边界

[ActorSynchronizationInitialization.cs](../../src/task_items/ActorSynchronizationInitialization.cs) 提供可变own初始化状态、按原始调用点记录的调用接口、槽写入记录和条件性结果。`CreateOwnStorage(cookie)`显式分配own零值槽，不把磁盘零填充视为有效OS对象。函数指针仅为整数token，从不转换为可执行地址。

模型允许own回调在condition初始化期间更新共享cookie，并在其后重新读取；编码结果由模型写入，fixture无需手工拼接两个编码槽。三个query先执行，再按真实短路顺序检查，避免“后面的未知值”错误阻止已知前项为零的fallback。事件返回未知时仍记录未知写入，随后停在测试点；间接guard、回调、cookie和退出注册缺少正常返回时都保留已完成前缀。fatal handler正常返回的自有观察只产生`FatalHandlerReturnedToInt3`停点，不触发真实trap。

`CriticalSectionCallReturned` / `ConditionCallReturned`只表示记录到正常返回，`IsNativeSynchronizationReady`恒为false。没有新增线程、锁、等待、P/Invoke、动态加载或原生回调执行，也没有根据这些记录替旧matrix模型将同步/通知条件设置为true。每个invocation对象只运行一次；新调用需新建invocation，底层状态可以共享。

[执行前预检](actor-initialization-sources-validation/preflight-reviewed.json)固定3个输入；[独立模型静审](actor-initialization-sources-validation/model-review.json)及[证据复核](actor-initialization-sources-validation/evidence-review.json)分别保存结论与哈希。预检中的pending是执行前历史状态，以[实际执行记录](actor-initialization-sources-validation/run.json)为准。

正式`require_escalated`审批后，本段固定构建执行一次（chunk `7e770f`，exit0），用既有csc编译两次并运行一个自有测试EXE。新增**53条断言/8组场景全部通过**：

| 场景组 | 新增断言 |
| --- | ---: |
| ConditionOrderAndCookie | 8 |
| EncodingVectors | 6 |
| FallbackEachMissingPointer | 6 |
| FatalAndWriteOrder | 7 |
| UnknownValueVsReturnPrefixes | 9 |
| LateUnknownShortCircuit | 5 |
| NoReplayAndOwnExceptions | 5 |
| SnapshotIsolation | 7 |
| 合计 | 53 |

编码有独立预计算的count0/1/63/17固定向量（含非零高位cookie），另有64个旋转计数×两个token的往返核对，后者聚合计为1条断言，未扩大成128条。fixture也核对回调期间event清零可见、回调后cookie更新、全部query先执行、event写入后失败、cleanup的32位-1被忽略及own异常前缀。它不模拟native EH。

编译有1条`CS0675`警告，定位fixture第139行的有符号循环下标转ulong后按位OR；该循环显式约束为0..63，所覆盖值不存在符号扩展差异。保留这份已编译测试源码和原始输出，没有为了隐藏警告再构建。旧suite运行数为**0**；旧模型接口及源码未改，不把历史测试计作新增进度。

[执行后清单](actor-initialization-sources-validation/postbuild-inventory.json)确认3个构建输入未变，上一检查点158个选定工程文件在构建期间均保持原哈希；HEAD仍为`6dfdae419d1c92f7738eefe241937057d83134df`，既有12份tracked差异仍为+60/-46。产物在`.tooling/actor-synchronization-initialization-build-20261021/`：

| 产物 | 大小 | SHA256 |
| --- | ---: | --- |
| GrimDynasty.ActorSynchronizationInitialization.dll | 9728 B | `339c892339be75f1b6c35cefcf1c195b5b65e8799ea72f0ace58b10750e51641` |
| ActorSynchronizationInitializationTests.exe | 22016 B | `938a8ab7da17dcfae0abbc1c5fea3a0e8f942dd6427635ff3a23af39d9638d99` |

## 已缩减条件与仍未解决的部分

| 项目 | 本段结果 | 仍需的证据或运行时条件 |
| --- | --- | --- |
| 初始化入口 | 已确定CRT init_e表→2DC908 | 实际加载/attach顺序没有原版运行验证 |
| TLS callbacks | 磁盘表首零，模板工厂可继续用于own逻辑线程 | loader实际TLS index/block与真实线程行为 |
| 编码回调槽 | 已证writer、三次查询顺序、cookie读取时机及可逆位运算；own代码自动生成两槽 | 动态导出解析成功、cookie运行时writer及loaded guard有效性 |
| 同步对象来源 | 已确定critical section调用、condition初始化与event fallback分支 | API内部实现、对象有效性、实际并发/等待/通知调度 |
| 失败和退出 | 已保留调用和写入前缀，明确fatal/INT3和未检查的注册返回 | 2DD7F4内部行为、2DCA28 cleanup及native EH |
| Actor尾部421428 | 原有已存材料中仅有读取，没有明确writer入口 | 保持停止，不全引擎扫描寻找writer |

下一最小范围可从已明确的注册回调`2DCA28..2DCA50`（旧归档已给40 B精确边界）补退出资源处置，或从已知CRT entry直接调用`2DD93C`核对cookie writer；需继续分别画清依赖及预算。真实多线程同步、加载后状态和正式原版样本兼容仍未验证。完整正常Actor构造仍依赖这些外部条件与原有分配/loaded guard/421428对象观察。

没有运行原游戏、Rea或Java，没有接触存档或Steam云，没有安装、修改执行策略/ACL或commit/push。上轮158项检查点及既有修改保留；本段材料封存于[actor-initialization-sources-complete/manifest.json](../../.tooling/checkpoints/actor-initialization-sources-complete/manifest.json)。清单包含旧选定材料、旧manifest、本段源码/取证/验证及自有产物；不包含原版完整DLL、存档或云数据。本段仅追加进度记录，未改旧产品源码。
