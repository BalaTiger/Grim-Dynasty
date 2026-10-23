# Actor 构造器续段：空赋值、连续前缀与针对性验收

已闭合 Engine Actor 固定调用 `025C5F → 022CE0` 的 fresh-inline、count=0 正常分支，并把返回后的连续前缀接到下一未知构造调用 `025CC9 → 0BDBE0` 前。新增自有模块经单次正式审批构建，47 条断言、8 组通过；旧 suite 未重跑。该结果仍依赖显式磁盘初始 guard profile 和已知正常分配观察，不是完整 Actor 构造或原生运行时兼容证明。

## 证据与身份

正文完整地址为 Engine.dll 的 preferred VA，简写地址及模型 ResumeInstructionRva 为 RVA。原件路径 `D:\Steam\steamapps\common\Grim Dawn\x64\Engine.dll`，身份为 4,485,632 字节、SHA256 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`。缓存 helper 的 subject.name 与 subject.digest.sha256 均绑定此 Engine；分支分析使用 structuredContent.normalized_result，没有把重复封装文本当成另一份证据。

| 证据 | SHA256 |
| --- | --- |
| [022CE0 伪代码](rea-record-getter-support/07-CString-copy-0x180022ce0-pseudocode.json) | `4916b868bd68e2fc63022a9d97cb7317187395873514701ef2001f6684ee1c52` |
| [022CE0 汇编](rea-record-getter-support/08-CString-copy-0x180022ce0-assembly.json) | `d9a273b1b818d9a83aaffd434bd592aedfdebcb14a74ca23b3e1a2431cc7a05e` |
| [Actor 0259A0 完整正文](offline-actor-constructor-body/0259a0-instructions.json) | `59f402f428e04e2928bb485e58fa85896ac08db4548bff6fa33444a58fa521a7` |
| [本轮固定 16 字节常量](offline-actor-continuation-constant/metadata.json) | `ba62396d83b9e10a71afab41473f62bdba18d45d80b2ded371a856ba66f95110` |

022CE0 汇编 evidence_id 为 `ev_11c9b1ee555aca0e8cce63184b7c54b2ead44ad1abfcc56803cecea33208c5c7`；实际分支和写入顺序以汇编为准，伪代码只作交叉说明。

## 零长度调用的窄正常域

此前已证前缀使 this+0x250 的字符串处于内联容量 15、长度 0 的状态。0x180025C55 把 R8D 清零，0x180025C58 将 RDX 设为 0x180307E61，0x180025C5F 调用 0x180022CE0；RCX 是该字符串对象。此处只采用有效、独占、前序正常返回的状态，不把 helper 泛化为任意损坏或已含内容的字符串操作。

022CE0 在 0x180022D0F..0x180022D24 判断 source 是否位于目的字符串的 [data, data+length) 区间。length=0 时区间为空，即使 source 数值等于 data，也不能进入内部 alias 分支。因此该具体调用不读取 source payload，也不需要补读 0x180307E61 的字面量。容量 15 已满足 count=0，长度上限和扩容路径均不进入；0x180022D91 的零长度分支直接执行两次写入：

- 0x180022D9B：字符串对象+0x10 的 QWORD 写 0，即 this+0x260 的长度。
- 0x180022DBD：字符串对象首 BYTE 写 0，即 this+0x250 的首字节。

这两次 store 即使原值已经是零仍执行；容量和其余内联字节不因此清空。该路径不调用 023180、022F70、memcpy 或长度错误处理。它返回目的字符串地址，但不能据此推导后续另一个构造器的返回值。

## 正常返回后的固定写序与常量

Actor 正文中 0x180025C64..0x180025C9C 有八次标量写入，EDI 来自已证前序的零值。偏移均相对于 this：

| 指令 VA | 目的偏移与宽度 | 位值 |
| --- | --- | --- |
| 0x180025C64 | +0x3B0，DWORD | 0 |
| 0x180025C6A | +0x3B4，BYTE | 0 |
| 0x180025C71 | +0x3B8，DWORD | 0x3F800000 |
| 0x180025C7B | +0x3BC，DWORD | 0x3F000000 |
| 0x180025C85 | +0x3C8，BYTE | 0 |
| 0x180025C8C | +0x3C0，WORD | 1 |
| 0x180025C95 | +0x460，BYTE | 0 |
| 0x180025C9C | +0x3C4，DWORD | 0 |

0x180025CA2 的指令字节 `0f100507673200` 明确指向 0x18034C3B0。此次只读取 [RVA 0x34C3B0, 0x34C3C0) 的 16 字节文件数据，结果为 `0000803f0000803f0000803f0000803f`；两个 little-endian QWORD 均为 `0x3f8000003f800000`。0x180025CA9 将这 16 字节写到 this+0x464，随后 0x180025CB0 将 this+0x474 的 BYTE 写零。这里保留原始位模式，不凭字段偏移猜测业务含义。

这份常量来自磁盘映像，不能单独证明加载后相应内存仍未被修改。采用该值的自有投影须明确使用已观测文件初值。前轮 [数组构造报告](ACTOR-CONSTRUCTOR-ARRAY-RESULT.md) 中的 0x180302D38/CE70 也是同类限制：LoadConfig 绑定的是 GuardCFCheckFunctionPointer，文件初始目标字节为 `C2 00 00`，只支持明确选择该文件初始检查目标的行为；不能将实际加载后的 guard slot 默认当作空操作。

## 分配边界与未完成部分

0x180025CB7 传 ECX=0x358（856），0x180025CBC 调用已归档 [2DC398 分配 wrapper](offline-engine-root-static-helper-direct-targets/2dc398-instructions.json)。该正文 SHA256 为 `0a7c6ecf1ff10400847871340eeba0c9239bb9a3e89d7281d03c45587088b887`；[既有精确导入绑定](offline-engine-root-helper-imports/metadata.json) 将其调用链绑定到 malloc、_callnewh 等。可复用的是正常成功返回非零存储的显式外部合同，不是已实现 CRT 重试、分配失败或异常处理。

0x180025CC1 保存分配返回值，0x180025CC6 将它传为 RCX，0x180025CC9 调用尚未闭合的 0x1800BDBE0。0x180025CCF 写入 this+0x1E0 的是 BDBE0 返回的 RAX；不能假定它等于传入分配指针，也不能在未知调用尚未恢复时提前发布字段或伪造成功。后续对返回对象+0x118/+0x110 的写入同样依赖该正常合同。

完整正常构造仍至少受以下边界约束：BDBE0 的构造与返回身份；0x18034BB44 的后续常量读取；0x180421428 的运行时全局指针及其 +0x668/+0x678 状态；前序 base/allocator/iterator 的正常返回前提；实际加载后的 guard 行为。原生 EH、失败清理和异常类型也未因本轮固定写序得到恢复。对未知调用或自有中断保留已发生前缀，不把未闭合部分接成成功占位。

## 自有接合与停止语义

[ActorConstructorContinuation.cs](../../src/task_items/ActorConstructorContinuation.cs) 直接复用已有可变 Actor 字段观察和数组构造结果，不复制 Actor 状态、不改旧模块。`ActorEmptyAssignment.ApplyFreshTarget` 只接受已知长度 0、容量 15，执行两次写入；未知或其他字符串状态拒绝，未实现通用字符串分支。

`ContinueWithObservation` 接受显式 16 字节数据和已知初次分配成功的 856 字节 token；`ContinueUsingArchivedConstant` 明确选择本轮文件常量。常量输入先复制，后续调用方修改不会改变已写字段；位模式逐字节保留，不做浮点运算。两种入口都要求前段已经显式采用 `ArchivedFileInitialCheckUsed`。默认路线仍停在未知 loaded guard，提供后续数据不能跨过该停点。原生 MOVUPS 的两次自有 QWORD 存储计作一次原生 store。

| 条件 | 自有结果 / RVA 停点 | 新增 Actor store | 前缀效果 |
| --- | --- | ---: | --- |
| 前序未完成 | Unsupported，保留前序停点 | 0 | 不修改已有 Actor/array |
| 常量长度非 16 | InvalidConstantObservation / 025C5F | 0 | 自有输入守卫，未进入新赋值 |
| 缺常量观察 | MissingConstantObservation / 025CA2 | 10 | 空赋值与八次标量写入保留；向量和后续 flag 未写 |
| 缺或非法初次分配观察 | MissingKnownInitialAllocation 或 InvalidKnownInitialAllocationIdentity / 025CBC | 12 | 向量与 flag 也保留；未调用构造器 |
| 观察齐全 | ConditionalPrefix / 025CC9 | 12 | 累计 142 次 Actor store；仅保存待传入 BDBE0 的 token |

token 的非零和地址跨度不溢出检查属于自有观察域，不是原生失败算法。结果始终 `SubobjectConstructorInvoked=false`、`IsCompleteActorConstructor=false`；没有写 this+0x1E0。正常初次分配观察没有覆盖 new-handler 重试或重入。自有托管执行也只覆盖正常返回；没有用 catch、回滚或 Unsupported 结果替代托管 OOM/原生 EH。

## 本轮读取、构建与验收

新增原件语义读取只有上述 16 字节数据；同一只读句柄前后各执行一次 64 KiB 缓冲区的完整 SHA256，两个结果均匹配固定 Engine 身份，文件状态稳定且句柄已关闭。这里的 16 字节不包含两次完整哈希的读取量。本轮没有新增 code bytes、dumpbin、Rea/Java 或原游戏执行，也没有读取 0x18034BB44、0x180421428 或 source literal 的内容。

本报告复用了缓存汇编与导入证据，没有重新运行原件，也没有原生动态差分测试。缓存 022CE0 查询的既有 summary 确认同 SHA、正常关闭及进程退出；这不计为本轮 Rea 执行。[证据复核记录](actor-constructor-continuation-validation/evidence-review.json) 保存其绑定、选定分支和当前读取范围。

[固定构建脚本](../../src/task_items/build_actor_constructor_continuation.ps1) 在执行前完整审阅，使用同一 Windows PowerShell 5.1，通过单独 `require_escalated` 正式批准执行；没有执行策略、ACL 或其他安全设置变更，没有 Bypass 或此前被拒脚本的重试。它用既有 csc `/noconfig /langversion:5 /checked+` 编译三个自有库源码和一个新测试源码，在 fresh 目录生成两个产物，只执行新测试 EXE 一次。

[执行记录](actor-constructor-continuation-validation/run.json)：工具 chunk `665fa3`，exit 0，0.628308744 秒，单次构建及测试调用成功。

| 新 fixture 组 | 通过断言 |
| --- | ---: |
| IndependentFreshAssignment | 6 |
| FreshGuards | 4 |
| IncompletePrior | 6 |
| CompleteSuppliedContinuation | 9 |
| MissingConstant | 6 |
| AllocationObservationGuards | 6 |
| ArchivedConstantCopies | 6 |
| InvalidConstantLengths | 4 |
| 合计 | 47 / 8 组 |

新 suite 自然调用既有数组 API 准备 10 个显式文件 guard prior 和 2 个默认 prior，并通过它们整合原 Actor prefix。新增 continuation 共调用 13 次（11 supplied、2 archived），独立空赋值调用 9 次。旧 64/11 数组 suite、48/8 prefix suite 及其他旧 suite 均未编译或重跑，不能把历史总计写成本次验收。

[构建前记录](actor-constructor-continuation-validation/preflight-reviewed.json) 核对 5 个输入和 11 个既有源码/脚本；[构建后清单](actor-constructor-continuation-validation/postbuild-inventory.json) 确认去重 14 个文件全未变化。HEAD 保持 `6dfdae419d1c92f7738eefe241937057d83134df`，已有 12 个 tracked build-script 改动保持原状，无 commit/push。

| 产物 | 字节 | SHA256 |
| --- | ---: | --- |
| `.tooling/actor-constructor-continuation-build-20261021/GrimDynasty.ActorConstructorContinuation.dll` | 17408 | `7d748bd9132e9ae61a0dff7248000546dcee0a193b1134e01389394cbe0c277c` |
| `.tooling/actor-constructor-continuation-build-20261021/ActorConstructorContinuationTests.exe` | 22528 | `55bdbe6dac79e9166a49fab34625587a21a1c6339e3070e9b0ef3758ebebe6f1` |

恢复检查点目标：[actor-constructor-continuation-complete/manifest.json](../../.tooling/checkpoints/actor-constructor-continuation-complete/manifest.json)，采用 fresh 目录，逐文件比较源与副本哈希并复读清单；具体数量和封存结果以 manifest 为准。仅收录自有源码、产物、报告和小范围归档证据，不收录原件 DLL 或存档。

下一最小证据是 BDBE0 的精确边界、直接构造依赖及真实正常返回身份。其后仍需 34BB44 固定数据与 421428 全局对象状态来源，才能决定 Actor 剩余正常写序的可实现范围。实际 loaded guard、外部分配失败/EH、正式样本兼容保持未验证；不能按当前 caller 中剩余一个明确调用推断只有一个函数尚缺。
