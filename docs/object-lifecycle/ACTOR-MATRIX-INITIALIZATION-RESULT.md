# Actor 矩阵静态初始化：证据、模型与验收结果

本段已取得 273AB0 慢路径直接调用的四份固定正文、锁与事件导入绑定，以及矩阵、global epoch、TLS 目录和模板的文件初值。新增自有模型已接入既有 Actor 构造链：第一调用按已证顺序初始化，第二调用读取同一状态并走 warm。**正式审批的精确构建一次成功，新增 64 条断言 / 10 组场景通过；受影响的旧 Completion 69 条断言 / 11 组场景另行通过。** 自有初始化仍要求明确的正常同步与通知合同；这里没有执行原版 DLL、Windows 同步 API 或原生 TLS 初始化，完整原生 Actor 构造及正式样本兼容仍未验证。

## 固定证据与实际读取

以下地址均为固定 Engine.dll 的 preferred VA，表中省略基址 `0x180000000`。原件大小 4,485,632 B，SHA256 为 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`。四次取证的 before/after 流式哈希均匹配，正文取得精确 `.pdata` BeginAddress 后才读取和反汇编，没有选择相邻函数或自动追踪新 callee。

| 已保存证据 | 精确范围 / SHA256 |
| --- | --- |
| [第一批执行记录](offline-actor-matrix-initialization/inspection.json) | `54d33ae66b7aea0358d7e652db7743725654584f3cdfe2a5010a6ff183e48a7e` |
| [header 2DCAE0](offline-actor-matrix-initialization/2dcae0-instructions.json) | `[2DCAE0,2DCB47)`，103 B / 24 条指令；`80a3ae3ef767ed745a2080c4494c193b4a2fae0f9497d8e3df700f4d0e4d0f0d` |
| [footer 2DCA80](offline-actor-matrix-initialization/2dca80-instructions.json) | `[2DCA80,2DCAE0)`，96 B / 20 条指令；`50e12e544bce9b0f21de60be8e46f4a1b40d0f895d6edf6e12bce34754a63822` |
| [第二批执行记录](offline-actor-matrix-sync-dependencies/inspection.json) | `5941d7ab01b2c2bd96957c8eeb369cb033aa79a256a54d371e026e9b870045c3` |
| [通知 2DCB48](offline-actor-matrix-sync-dependencies/2dcb48-instructions.json) | `[2DCB48,2DCBA3)`，91 B / 22 条指令；`d49cbcc2312102ca3e6fae5601202933c85185aa9a69e3bd2ba8fd0427b3c54b` |
| [等待 2DCBA4](offline-actor-matrix-sync-dependencies/2dcba4-instructions.json) | `[2DCBA4,2DCC3B)`，151 B / 38 条指令；`348c2f95e7130eb809f19eb57d79f698911effad62c5ddf34134e90641896efc` |
| [锁导入绑定](offline-actor-matrix-lock-imports/metadata.json) | `dc731e02d7a089020b24e0cacd2aa4b9feebec723514be205737a57d0895d451` |
| [事件导入绑定](offline-actor-matrix-event-imports/metadata.json) | `80740b26495217c1854ff797d3150f62c542134fc9d5c5c7ec147d3b9b9f3d03` |

| 本段执行 | 正文 | `.pdata` | 其他元数据 | 文件数据 | 虚拟零填充说明 | dumpbin |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 第一批 `6494be` | 199 B / 44 指令 | 168 B | 0 | 48 B | 8 B | 2 |
| 锁导入 `c184ae` | 0 | 0 | 62 B | 0 | 0 | 0 |
| 第二批 `76efd5` | 242 B / 60 指令 | 168 B | 48 B TLS | 4 B global + 8 B TLS 模板 | 0 | 2 |
| 事件导入 `116b73` | 0 | 0 | 72 B | 0 | 0 | 0 |
| **合计** | **441 B / 104 指令** | **336 B** | **182 B** | **60 B** | **8 B，未读取文件字节** | **4** |

元数据合计为 **518 B**（336 B `.pdata`、48 B TLS、134 B imports）。四次固定读取各执行前后全文件哈希，共 **8 次 Engine 流式 hash pass**，每次使用 64 KiB 缓冲；哈希读取不混入语义字节计数。dumpbin 内部 PE 读取未单独计量，上表是控制器记录的精确读取与进程次数。reader 中保留的 PREPARED ONLY 注释是编写阶段说明，实际执行状态以上述 inspection/metadata 和 chunk 为准。取证后的文档整理与封存未追加原件读取；自有构建和测试的实际范围见下方验收记录。

## 文件初值与运行时前提

41D490 的 48 B 文件内容只有 DWORD 偏移 `+0/+0x10/+0x20` 为 `3F800000`，其余字节为零。guard 423F88 与 TLS index 420DD4 各 4 B 位于 `.data` 虚拟尾部，记录的是 loader 文件映像零填充依据，没有对应文件字节读取，也不证明调用时 guard 或 index 的值。41C51C 的 4 B 实读为 `00000080`，即 signed int32 `INT_MIN`。

PE32+ TLS 目录字段位于文件偏移 496；目录 RVA 为 35D468，长度恰 40 B。它给出模板 `[0x180449000,0x180449008)`、AddressOfIndex=`0x180420DD4`、AddressOfCallbacks=`0x180303198`、SizeOfZeroFill=0、Characteristics=`0x300000`。完整模板实读 8 B 为 `0000000000000080`，因此模板 `+4` 的 int32 为 `INT_MIN`；它完整落在已记录 `.tls` 的 9 B virtual 范围内。callbacks 仅记录地址，没有跟随数组或调用任何 callback。

这些证据允许显式构建“采用归档文件初值”的自有状态，不证明 GS:[58]、loader 分配的 TLS index、线程 block、已加载 cookie/guard 或同步对象已经有效。模型不能把虚拟 index=0 自动当作运行时 TLS 访问合同，也不能把磁盘 48 B 等同任意调用时的矩阵观察。

## 273AB0、header 与 footer 的顺序

既有 [273AB0 正文](offline-actor-subobject-direct-dependencies/273ab0-instructions.json) SHA256 为 `0fc850ae074a5f4f4ea6046d79f0a96f13c731314a34d40af9b6d16e6d58a3b7`。273ACF/273AD5 先作 signed int32 比较：`guard <= threadEpoch` 直接走 warm 返回 41D490；只有 `guard > threadEpoch` 才调用 header。不能脱离外层比较，把所有 guard=-1 都视为等待。

header 2DCAE0 在 2DCAF0 调用 `EnterCriticalSection(&420D88)`，随后按实际 guard 分支：

- guard=0：2DCAFB 写为 -1，跳到解锁尾部；返回 caller 后，273AEF 检查 guard=-1，才执行初始化。
- guard=-1：2DCB00 设 ECX=100，2DCB05 调用 2DCBA4；2DCB0A 无条件回到 2DCAF6 重读 guard，**不检查 AL 返回值**。
- 其他 guard：2DCB2A 读 **global epoch 41C51C**，2DCB30 写线程 TLS block+4；不是把 guard 值复制给线程。随后正常解锁返回，caller 跳过初始化。

273AB0 的初始化仅有两次源写入：273B11 先写 41D490+0x2C 的 DWORD 为零，273B1A 再写 +0x24 的 QWORD 为零，合计末 **12 B**。前 36 B 原样保留；该分支没有重建完整矩阵或写入三处 `3F800000`。然后 273B22 调 footer。

footer 2DCA80 的正常序列为：进入同一 critical section；2DCA96 读 global，2DCAA9 在 32 位 EAX 中 `INC`（wrap）；2DCAAB 写 global；2DCAB1 写传入 guard；2DCAC6 **重读 global**；2DCACC 写线程 epoch；2DCAD0 解锁；最后 2DCADB 尾跳通知 2DCB48。`INT_MAX + 1` 的位模式为 `INT_MIN`，没有饱和或额外失败分支。走到通知依赖时，先前 global/guard/thread 和源尾部写入已经发生；通知正常返回尚未得到保证时，模型仍保留此前写入，不虚构回滚。

## 通知与等待仍需要实际运行时合同

锁导入已精确绑定 KERNEL32.dll：IAT302328=`EnterCriticalSection`，302330=`LeaveCriticalSection`。事件导入已精确绑定 302278=`SetEvent`、302288=`ResetEvent`、3023A8=`WaitForSingleObjectEx`；这证明导入身份，未执行这些 API，也不证明句柄有效或返回成功。

通知 2DCB48 在 2DCB4E 读取 420DB8。非零时 2DCB8A 调 SetEvent，之后重新读取 420DB8，并在 2DCB9C 尾跳 ResetEvent；两次读取不能一般化为稳定同一句柄。为零时，取 41C520 的 cookie 与 420DC8 的编码目标，按 `ROR64(cookie XOR encoded, cookie & 63)` 解码，2DCB73 调 2DD4C0 验证路径，再以 RCX=&420DB0 尾跳解码目标。该分支不是无操作；实际 cookie、编码目标、loaded guard 和 callback 正常返回均不能由文件初值代替。

等待 2DCBA4 同样区分 420DB8。非零分支先 LeaveCriticalSection，调用 `WaitForSingleObjectEx(handle,100,0)`，再 EnterCriticalSection；其 AL 表示 API DWORD 返回值是否为零。零分支使用 420DC0 的编码 callback，经 cookie 解码与 2DD4C0 路径，以 `(&420DB0,&420D88,100)` 调用；其 AL 表示 callback EAX 是否非零。header 在两条路径之后都忽略 AL、重读 guard。句柄/callback 生命周期、竞争线程的 guard 发布、等待进展及异常路径未恢复；不能用导入名称或 AL 布尔值补一个必成功等待。

## 新自有模型与接合边界

[ActorMatrixInitialization.cs](../../src/task_items/ActorMatrixInitialization.cs) 提供 `ActorMatrixInitializationState`、`ActorMatrixThreadState` 与实现 `IActorMatrixStateSource` 的 `ActorMatrixInitializationSource`。可空 int32/可空 48 B 状态保留未知值，输入与 `CopySource()` 采用副本；store 计数保留重复写入。`CreateArchivedInitialState()` 与 `CreateArchivedTemplateThread()` 是明确标记 provenance 的自有逻辑状态工厂，不创建 Windows TLS 或原生全局对象。

`Read()` 先执行 signed warm 判断；慢路径要求显式 `normalExclusiveSynchronization`：整个 Read 期间状态独占，锁与解锁正常返回，且没有可改变重读值的并发/重入。该合同不是 OS 同步实现。guard=-1 的竞争路径停在 `UnresolvedMatrixWait`，没有忙等、猜测调度结果或人为变更 guard。

guard=0 路径按已证顺序写 guard=-1、源尾 12 B、递增 global、发布 guard 和线程 epoch。通知单独受 `normalNotificationReturn` 合同限制；为 false 时以 `UnresolvedMatrixNotification` 停止，保留上述真实模型前缀。缺失 global 时也保留此前已完成的 guard/source 写入。warm 路径不需要慢路径同步或通知合同；既有 header 分支写线程 epoch 后以 `HeaderExisting` 返回。

与 [ActorConstructorCompletion.cs](../../src/task_items/ActorConstructorCompletion.cs) 的接合仍在实际矩阵调用点才调用 source；结果区分 `Warm`、`Initialized`、`HeaderExisting` 或带 StopRva 的 Unsupported。两次矩阵调用分别观察状态；首次初始化后第二次可以走 warm。返回的 48 B 仍须逐块已知，不能因 helper 已返回就补齐未知前 36 B。原有纯 warm 观察接口保留其边界，不自动升级成慢路径成功。

## 已验收范围与保留的前提

| 状态 | 自有模型现在能建立的内容 | 仍不能由此推出的原生状态 |
| --- | --- | --- |
| 新建逻辑状态 | 明确采用归档 profile：guard=0、global/thread epoch=INT_MIN、48 B 来源按已证文件位模式初始化 | 当前进程中的 GS/TLS index/block、callback 执行顺序或加载后内容 |
| 首次初始化 | 在显式独占和正常同步/通知合同下，写末12 B并将 global/guard/thread 更新到 INT_MIN+1 | critical section、事件句柄、编码回调及其 writer 已恢复 |
| 后续调用 | 同一逻辑线程直接 warm；新逻辑线程可经 header 从共享 global 更新自身 epoch | 竞争、等待进度、重入或跨原生线程等价性 |
| Actor 其他前提 | 继续保留已有对象身份、前缀状态和构造链 | loaded guard、421428 有效对象及其字段来源仍需独立证据/观察 |

因此新路径不再要求 fixture 手工提供两份预先采样的 warm epoch/48 B 矩阵观察；它从自有共享初始化状态在实际调用点产生结果。正常同步和通知并未被文件零填充替代。

[执行前预检](actor-matrix-initialization-validation/preflight-reviewed.json) 冻结八个构建输入；[独立静态审阅](actor-matrix-initialization-validation/model-review.json) 对照指令核对 signed 分支、停点、32 位回绕和延迟接入。[实际执行记录](actor-matrix-initialization-validation/run.json) 保存 chunk `2435b3` 的完整输出：正式 `require_escalated`，exit 0，一次构建、三次编译、两个自有测试 EXE 各运行一次。

| 新场景组 | 断言数 |
| --- | ---: |
| StorageAndArchivedFactories | 6 |
| ColdThenWarm | 6 |
| SharedStateHeaderExisting | 6 |
| EntryAndSynchronizationStops | 7 |
| GlobalAndNotificationPrefixes | 7 |
| SignedOrderingAndWrap | 6 |
| DeferredIntegration | 7 |
| EarlyIntegrationStops | 6 |
| PartialMatrixSources | 6 |
| ReadResultsAndOldApiParity | 7 |
| **新增合计** | **64** |

新 fixture 验证 cold→warm 的真实状态变化、第二逻辑线程的 header-existing 分支、guard=-1 的外层 signed warm 优先顺序、INT_MAX 回绕、global/同步/通知未知前缀、三个首矩阵块停点、第二矩阵独立部分复制及旧入口稳定输入对照。spy 有独立的最多两次调用硬上限，核对实际 Read 时机，没有执行 native callback。

旧 `ActorConstructorCompletionTests.cs` 未改动，经新构建单独重编译并运行 **69 / 11**；这是延迟接入影响到原入口后的必要回归，不计作新增断言。其他旧 suite 运行数为 **0**。新整合场景自然调用 Prefix/Array/Continuation，不宣称重跑它们的历史 suite。

[执行后清单](actor-matrix-initialization-validation/postbuild-inventory.json) 确认 19 个去重后的输入及既有源码在构建期间保持一致；HEAD 为 `6dfdae419d1c92f7738eefe241937057d83134df`，原有十二份 tracked diff 未变。

| 源码或产物 | SHA256 |
| --- | --- |
| ActorMatrixInitialization.cs | `4f9e51fdbb10b13724c59f05e696f5caeaf87ab3128df46800a3fdb3d0fedfd4` |
| ActorConstructorCompletion.cs | `3d087a1865b5597f7ad30241cea7b3a06db864a4c5fc1a5bd5650d932e678f62` |
| ActorMatrixInitializationTests.cs | `d64a170f11917f9ddef3962aa18f08f759120cfdbeb79f3e848b6c1429759b64` |
| build_actor_matrix_initialization.ps1 | `aa4a696207fd365a0e232654401c7d308f46a9660e666964b0828b7cd2a90154` |
| GrimDynasty.ActorMatrixInitialization.dll，37376 B | `819c672ac6c0226cc7f67c808232c6b74a431f2a6cbd6c31459e876520310097` |
| ActorMatrixInitializationTests.exe，31744 B | `05d8abf495bbe55b09336fabe5bf48e3e9b69255820900010a5302ba83a3c28c` |
| 本次编译的 ActorConstructorCompletionTests.exe，33792 B | `d5097c5aa8fda2495af12f8b3eacde671a7e4f88d3a22297d68e6490af355860` |

三个自有产物位于 `.tooling/actor-matrix-initialization-build-20261021/`。执行前和证据审阅记录中的 pending 状态保留为历史，不代表最终验收状态。

## 尚未闭合的原生运行时范围

本段没有恢复 critical section/event/420DB0 的创建与销毁、420DB8/420DC0/420DC8 的 runtime writer、cookie 和 loaded guard 的有效性、TLS callbacks/loader 初始化或竞争等待调度，也未模拟原生 EH。通知是明确的正常返回前提，不是从已知 helper 地址推出的无条件成功。

Actor 尾部 421428 的有限既有归档仍只有读取用途，没有已证初始化 writer。本段沿用旧 Actor caller 及先前范围，未扩大扫描、读取该 slot 的运行时目标或虚构全局对象。完整 Actor 构造仍依赖既有正常分配、有效独占存储、loaded guard 与该全局对象观察，`IsCompleteNativeConstructor` 不因此变为 true。

下一最小取证缺口是同步对象及 420DB8/420DC0/420DC8 的初始化 writer、TLS callbacks 与其时序；需要先从明确引用确定目标，不扩大为全引擎扫描。421428 的独立 writer 仍未定位。自有 fixture 通过不等于原版并发/TLS 对照运行。

恢复材料位于 [actor-matrix-initialization-complete/manifest.json](../../.tooling/checkpoints/actor-matrix-initialization-complete/manifest.json)，包含上一检查点选定材料、本段有界证据、源码和实际验收产物。清单记录逐项源/副本哈希；上一检查点保留原 Completion 版本，原件完整 DLL、存档与云数据不进入本段封存。没有原游戏/Rea/Java、安装或安全设置变化，也没有 commit/push。
