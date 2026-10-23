# Actor 数组构造接合 — 2026-10-21

本轮完成了 EBDE0(this+198,4) 的有界静态调查、最小自有数组构造模型及与原有Actor前缀的条件接合。**正式审批后一次构建/测试通过64条新断言、11组场景。** 默认入口仍停在未观测的加载后检查前；只有显式磁盘初值模型继续构造并到达25C55。302D38 已定位为 Load Configuration 的检查函数槽，不能把其磁盘初值推广为实际运行期行为。

## 入口及五次头部写入

前轮 [Actor 报告](ACTOR-CONSTRUCTOR-RESULT.md) 的验收只覆盖其已说明的局部前缀，不替代本轮验证。[07D020 已存正文](offline-actor-base-dependency/07d020-instructions.json) 在 18007D0C8 形成原对象 this+198h，07D0D4 设置 EDX=4，07D0DC 调用 1800EBDE0。只有正常返回后，07D0E8 才把该子对象的首 qword 改为身份值 18033E828，不能提前执行此写入。

[0EBDE0 正文](offline-actor-198-constructor/0ebde0-instructions.json) 对应精确 .pdata 范围 [0EBDE0,0EBEA5)，197 字节、53 条解码指令。令 S=this+198h，分配之前依次执行：

| 指令 RVA | 目标与精确宽度 |
| --- | --- |
| 0EBE06 | qword [S+00]=180339B70，仅静态身份值 |
| 0EBE0B | dword [S+08]=0 |
| 0EBE0E | qword [S+10]=0 |
| 0EBE12 | dword [S+18]=EDX，本调用为4 |
| 0EBE15 | byte [S+1C]=0 |

这些写入不能填平头部空洞；S+0C..0F、S+1D 以后的未写字节不能由相邻零值推断。后续依赖未完成时，已执行的写入仍须保留，不能回滚成“未发生”。

## 520字节分配、清零、cookie与发布

0EBE19 的 MOV EBP,EDX 保留32位计数并清除RBP高32位。0EBE20..0EBE3A 用无符号乘法形成 count*80h，乘法溢出选择全1，再加8并在进位时选择全1。本次固定 count=4，因此请求为 4*128+8=520 字节。这里不据固定正常域扩展任意计数或异常合同。

0EBE41 调用 2DC900。既存 [8字节入口证据](offline-engine-root-static-helper-entry-prefixes/2dc900-instructions.json) 跳转到 [2DC398 正文](offline-engine-root-static-helper-direct-targets/2dc398-instructions.json)。2DC3C4 经 2DDB38 调用 IAT 302968，已由[导入绑定](offline-engine-root-helper-imports/metadata.json)确认为 api-ms-win-crt-heap-l1-1-0.dll!malloc。malloc返回零时调用 _callnewh；任意非零EAX重试同一尺寸，零值进入已存throw-wrapper路径。可见正常返回要求非零指针。

这支持显式“正常分配已成功且存储有效”的合同，不代表CRT内部、OOM恢复、全部throw行为或 native EH 已恢复。不能把自有分配回调返回null说成此helper的正常失败返回。原caller仍保留 0EBE4E TEST RAX,RAX、0EBE51 JE 0EBE8B 的防御分支；若走该分支，RDI保持零并发布零指针。这一可见分支不改变分配helper的正常返回合同。

非零分支在 0EBE5B 以分配起点、0、520 调用 2DDB14。其6字节 FF25 跳板指向 IAT 302770；[本轮精确导入证据](offline-actor-198-zero-import/metadata.json)将其绑定为 VCRUNTIME140.dll!memset。这里只使用标准正常成功合同，没有读取或运行CRT实现。

随后 0EBE60 写入分配起点qword cookie=4；0EBE63 使RDI=起点+8。0EBE85 传给数组迭代器的参数是首元素地址、步长128、计数4、构造入口0EBEB0，栈参数另提供析构入口身份0EBF00。**只有迭代器正常返回后，0EBE8B 才发布 [S+10]=RDI**；0EBE8F 准备返回S。中断时不能提前发布，也不能否认此前已经完成的分配、清零、cookie和元素写入。

## 四次leaf构造及检查边界

[2DCC3C 迭代器](offline-actor-198-array-helpers/2dcc3c-instructions.json)精确正文110字节、35条指令。正常路径从索引0开始，每次先在 2DCC75 以构造入口为RCX调用2DD4C0，返回后才在 2DCC7D CALL RSI 以当前元素为RCX调用构造器；随后地址加128、完成计数加1，直至4次完成。不能省略检查、倒置次序或提前增加完成计数。

正常循环没有直接调用析构入口。栈上的完成标志、进度和析构参数不能替代尚未读取的异常处理及清理证据；未恢复0EBF00析构行为、EH/unwind逻辑、失败时析构个数或释放策略。

[0EBEB0 固定80字节区间](offline-actor-198-leaf-prefixes/0ebeb0-instructions.json)包含到0EBEF5 RET的17条实际指令和其后10个INT3，并非取得了精确unwind入口。可见正常路径无callee，以EAX=0执行14次存储，然后返回原RCX：

| 依次写入的元素内偏移 | 宽度与值 |
| --- | --- |
| 18、10、00 | qword15、qword0、byte0 |
| 30、38、20 | qword0、qword15、byte0，顺序与第一组不同 |
| 40、48、50、58 | 四个qword0 |
| 60 | dword FFFFFFFE，不可扩大为qword |
| 68、70、78 | 三个qword0 |

四个128字节元素分别始于分配起点+8、+136、+264、+392。元素内其他字节在本路径上已知为零，依据是此前覆盖520字节的memset；**leaf本身没有初始化全部128字节**。这一区别涵盖两段inline字符串的其余字节和+64..67空洞。

## 302D38：已定位槽，未确认运行时行为

2DD4C0 的精确 .pdata 查询未匹配入口，[停止报告](offline-actor-array-check/inspection.json)没有选择相邻正文。后续首字节检查读到48而停止，没有把它当RET；再经单独固定8字节请求，[入口证据](offline-actor-array-check-entry/2dd4c0-entry-instructions.json)得到 2DD4C0 JMP qword ptr [180302D38]，后跟一个INT3填充字节。

新增[槽与Load Configuration元数据](offline-actor-array-check-slot/metadata.json)只读了8字节槽和72字节元数据：槽的磁盘字节为 70ce008001000000，磁盘初值为18000CE70；Load Configuration位于RVA35D3D0，目录大小及声明大小均148，其+112 GuardCFCheckFunctionPointer为180302D38，GuardFlags=100h。字段格式出处沿用报告引用的 [Microsoft PE/COFF格式](https://learn.microsoft.com/en-us/windows/win32/debug/pe-format)。

该匹配说明文件中的槽与指定检查字段关联，**不证明实际运行值、不证明加载器未替换它，也不证明检查总会正常返回**。

[CE70首字节报告](offline-actor-array-check-file-target/metadata.json)读到C2，按原定非C3停止条件保留原样。新的[两字节操作数报告](offline-actor-array-check-ret-immediate/metadata.json)仅补读[CE71,CE73)，没有重读首字节；组合固定x64编码为 **C2 0000，RET imm16=0**。这证明磁盘初值所指入口立即返回、无额外栈调整；没有读取邻域、没有假称获得精确.pdata边界，也没有执行该指令。它不证明加载后的检查会采用同一目标，更不允许省略运行期检查。

## 最小自有模型及条件边界

[ActorConstructorArray.cs](../../src/task_items/ActorConstructorArray.cs)使用独立的nullable-byte观察存储和数值身份token；不会解引用这些原生地址、调用原版代码或写入真实guard槽。数组观察范围固定520字节，不扩展任意计数，也不把它当作真实malloc/对象ABI。独立leaf只写94字节，保留两段15字节inline余部及4字节padding，共34字节。

两个入口有意保留不同证据条件：

* `ApplyUntilUnresolvedCheck`：先运行已存15次entry写入和5次子对象头写入。缺少已知首次分配结果时返回自有Unsupported，停在EBE41，20次Actor写入仍保留；这是缺观察输入，不是原生malloc返回null。提供有效分配观察时，执行520字节清零和cookie=4，停在2DCC75，构造/检查计数均为0，数组指针保持0，不进入后段。
* `ApplyWithArchivedFileInitialCheckTarget`：显式采用已归档的CE70文件初值行为，并要求首次malloc已成功、memset正常返回、有效独占存储。依次完成四次检查行为投影及四次leaf，在迭代器正常结束后才发布payload token。随后完成07D020的38次后续写入和原有Actor的71次写入。结果为`ConditionalPrefix`，Actor写入130次、数组字段写入57次、清零520字节，四元素/四检查；仍停在25C55，`IsCompleteActorConstructor=false`。

正常组合的计数为Actor `15+5+1+38+71=130`、数组 `1+4*14=57`。保留`+174:word=0100`、`+CB:word=0`、`+1BC:qword=0`等窄写/非对齐写及未写邻域。缺Actor/缺allocation的条件入口直接保留默认入口停点，不制造成功。没有用任意回调或异常catch模拟原生构造/检查/EH，没有接入完整动态factory。

此条件域排除malloc失败后的new-handler副作用及重入、CRT故障、加载后guard的未知行为、构造器异常和析构回滚。`BaseReturned`表示自有条件投影完成了已证正常段，不是原生游戏实际执行的观测。原有ActorConstructorPrefix源码保持不变；通过本轮新整合fixture调用它是必要的接口组合验证，不等于重跑其旧48断言。

独立静审还核对了自有托管异常边界：复用的post-base API会分配自己的报告对象，此时数组已发布、38次base后写已完成。如果该托管分配失败，调用会抛出而不会返回本轮结果；调用方持有的state/array仍可能已变更。本模型只覆盖正常托管执行，Unsupported只标记缺观察输入，不承诺任意异常的结果返回或回滚。不加入catch把这种自有异常伪装为原生错误/EH。

## 本轮显式读取账目

下表汇总本轮九份报告；前轮Actor/base及allocator等复用证据不重复计入。固定前缀、无精确边界的停止请求及重复读取均保留：

| 报告 | 元数据B | 槽数据B | 代码读取B | dumpbin次数 | 已解码指令 |
| --- | ---: | ---: | ---: | ---: | ---: |
| [198-constructor](offline-actor-198-constructor/inspection.json) | 156 | 0 | 197 | 1 | 53 |
| [198-array-helpers](offline-actor-198-array-helpers/inspection.json) | 372 | 0 | 116 | 2 | 36 |
| [198-leaf-prefixes](offline-actor-198-leaf-prefixes/inspection.json) | 0 | 0 | 81 | 1 | 27 |
| [198-zero-import](offline-actor-198-zero-import/metadata.json) | 78 | 0 | 0 | 0 | 0 |
| [array-check停止](offline-actor-array-check/inspection.json) | 168 | 0 | 0 | 0 | 0 |
| [array-check-entry](offline-actor-array-check-entry/inspection.json) | 0 | 0 | 8 | 1 | 2 |
| [array-check-slot](offline-actor-array-check-slot/metadata.json) | 72 | 8 | 0 | 0 | 0 |
| [file-target首字节停止](offline-actor-array-check-file-target/metadata.json) | 0 | 0 | 1 | 0 | 0 |
| [ret-immediate操作数](offline-actor-array-check-ret-immediate/metadata.json) | 0 | 0 | 2 | 0 | 1 |
| **合计** | **846** | **8** | **405** | **5** | **119** |

81字节包括未反汇编的2DD4C0首字节1字节，它随后又包含在8字节区间内。因此405是累计代码读取量，互异代码字节为404。118条dumpbin指令包含11个INT3填充指令，另1条C2 0000由固定编码及3字节证据组成；不把单独48或C2另计为完整指令。helpers批次虽然停止，但此前已验证的6字节跳板和110字节迭代器照实计入。

九份报告均保存原件前后相同SHA256，合计18次全Engine流式指纹读取，缓冲各64KiB，与上表语义读取分列。原件为4,485,632字节的Engine.dll，固定SHA256：

4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153

既装dumpbin的固定SHA256为：

cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815

涉及工具的报告保存前后相同工具指纹。dumpbin内部元数据I/O未计入控制器显式账目。每份报告保留Node控制器自身RSS，本轮范围41,236至43,296KiB，最高约42.28MiB；它不是加上dumpbin后的任务树峰值、系统内存上界或编译预算。固定小范围、有限二分、64KiB缓冲及有界工具输出降低读取量，不构成全进程内存硬上限。[读取汇总及逐报告哈希](actor-constructor-array-validation/static-read-summary.json)保留完整账目。

## 实际验证结果

新[构建脚本](../../src/task_items/build_actor_constructor_array.ps1)限定未改动的Actor前缀源码、新数组模块和一份[新测试源码](../../src/task_items/ActorConstructorArrayTests.cs)。完整审查脚本、输入和固定新输出范围后，另行通过 `require_escalated` 正式机制执行 Windows PowerShell 5.1 的精确 `-NoProfile -NonInteractive -File` 动作；没有沿用只读查询批准，没有使用Bypass或重试旧被拒脚本。安装的.NET Framework编译器以 `/noconfig /langversion:5 /checked+ /optimize+ /debug-` 编译两份library source及一份fixture，然后仅运行自有测试EXE。执行一次、退出0、工具块 `764334`，没有失败构建或重复测试。

| 新场景 | 通过断言 | 核对范围 |
| --- | ---: | --- |
| UnknownLeafObservation | 6 | 独立leaf的94字节写入/34字节未知保留及cookie不变 |
| NonzeroTailAndLastElement | 5 | 非零尾部、最后元素边界、dword不扩大为qword |
| OwnershipAndSeparateElements | 5 | 输入/输出副本与独立数组隔离、共享自身观察身份 |
| LayoutAndOwnGuards | 6 | 520字节布局、元素偏移及自有输入边界 |
| MissingInitialAllocation | 7 | 20次Actor写入、119个已知字节和缺分配停点 |
| MissingActorDoesNotUseAllocation | 4 | 空Actor不触碰提供的数组、不报告后段成功 |
| DefaultStopsAtLoadedCheck | 7 | 清零/cookie后的精确前缀，无检查/元素/发布 |
| ArchivedConditionalArrayAndScope | 8 | 全520字节oracle、136个leaf空洞由memset归零、整链计数及未知后段 |
| ConditionalNarrowStoresAndNeighbors | 7 | 完整未写掩码、word/byte/非对齐qword及字符串尾部 |
| ExplicitProfileDoesNotChangeDefault | 5 | 显式条件路径不会使默认未知检查自动成功 |
| ArchivedProfilePreservesMissingInputStops | 4 | 条件入口仍保留缺Actor/分配时的停点与前缀 |
| **合计** | **64** | **11组** |

**旧套件实际重跑数为0。** 新fixture直接调用默认组合API5次、显式条件API5次；旧15次entry与71次post-base段通过新整合自然覆盖，不能说旧48断言也重新通过。更早的Item、factory、registration、root和总验收套件均未重跑，也未把其旧计数加入64。8份旧源码/测试文件及本轮4个构建输入均在构建后核对哈希一致；HEAD仍为 `6dfdae419d1c92f7738eefe241937057d83134df`，跟踪差异仍为原12个构建脚本、+60/-46。

记录：[审查前置记录](actor-constructor-array-validation/preflight-reviewed.json)、[实际执行及原始输出](actor-constructor-array-validation/run.json)、[输入/输出及保留文件哈希](actor-constructor-array-validation/postbuild-inventory.json)。

| 新产物 | 字节 | SHA256 |
| --- | ---: | --- |
| ActorConstructorArray.cs | 8940 | e08ecb7797516f31e453bfb48e85ef146ad6ba2fe44d623548a46fa848f5e66e |
| ActorConstructorArrayTests.cs | 32141 | 3167750b2cd620b697ae954a1c92896342b336d66e86d8477e90980ae0040594 |
| build_actor_constructor_array.ps1 | 4702 | 4183282fb109889d04231986c30e10e7548de35ee45c939122a0adf5089e01b4 |
| GrimDynasty.ActorConstructorArray.dll | 12288 | 258eec2eaea377c1f6b271b0b87cb420ec184a5fd3e2d5fff38fd9606379310b |
| ActorConstructorArrayTests.exe | 31744 | cc81dbbd243e61db5246aa995df55b1d5234443df038640f9446b7b132a0c9d2 |

## 检查点及下一关键证据

检查点清单目标为 `.tooling/checkpoints/actor-constructor-array-complete/manifest.json`，封存数量、字节数及逐文件源/副本一致性以该清单和封存工具结果为准。包含本轮自有源码、静态碎片/停止记录、必要复用证据、执行记录、新构建产物及前轮清单，不包括完整原版DLL、游戏EXE、玩家存档或云数据。

下一直接依赖仍为 **Engine 025C5F → 022CE0**，输入this+250、静态源身份180307E61、长度0。需要其边界和正常合同，不能仅凭长度0将它删去。再后面仍有size358的分配及0BDBE0构造/返回身份、两个固定数据区、全局421428派生状态。0EBF00析构/EH、加载后的guard检查/失败行为以及allocator失败重入仍未恢复；不会以本轮条件构造宣称原生运行期全图、完整Actor/Item/QuestItem工厂或正式样本兼容。

本轮保持原版只读，不运行原游戏、Rea或Java，不安装额外工具，不读取存档或Steam云，不修改执行策略/ACL等安全设置，不commit/push。既有修改保持原样。
