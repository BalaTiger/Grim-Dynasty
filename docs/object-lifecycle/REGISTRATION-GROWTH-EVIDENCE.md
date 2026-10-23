# 注册容器单层增长：静态证据与实现范围

日期：2026-10-19。本轮在现有 `RegistrationIndex` 的显式链和桶模型上增加单层增长、节点重排及正常成功 reserve。新增测试为 `src/task_items/RegistrationGrowthTests.cs`。旧分配器、原生对象构造器和 Read 均不在本轮实现范围；不执行原版，不读取玩家存档或 Steam 云。

唯一分析目标仍是 x64 Engine.dll，SHA256 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`。本轮新增证据目录 [rea-container-reserve](rea-container-reserve/summary.json) 共 11 次调用，正常关闭，工具退出码 0，前后原版哈希不变。分析器返回的是静态伪代码/汇编证据，不是原始源码或执行结果。

## 成功范围

容器仍只接收调用者显式输入的自有完整快照。节点身份使用 C# 引用，payload 是不解释内容的对象引用；实现保留双向全局链、sentinel、每桶 first/last，不使用 Dictionary 重新赋值或重建节点。

原有限制保持：节点至多 65536，桶数为正 2 的幂且至多 4096，已知容量至多 4096 桶，阈值为有限正 float。计数及 power-of-two 桶数的 binary32 转换和商在此域精确。新键插入链接后若负载超过阈值，允许一轮增长，前提为：

- 新桶数不超过 4096；旧桶数小于 512 时乘 8，否则乘 2。
- 插入后计数除以新桶数不大于阈值。这保证重排内部再次调用维护时不触发第二轮增长。
- 节点和桶存储的分配、迁移及释放契约正常成功。C# 数组不模拟原生地址、对齐头、CRT 分配器/free 的内部行为或其未知副作用。

已有容量不够时按目标桶数**精确 reserve**，已有容量足够时保留容量。由此小于 8 桶容量的全清和空容器删除未命中现在可在正常 reserve 条件下成功重置到 8 桶，不再固定拒绝。

## Reserve 调用链和容量

`0x18001B4F0` 计算 `2 * targetBuckets` 个指针槽；只有容量不足才调用 `0x18001BFB0`。该函数按传入槽数分配，复制旧 `[begin,end)` 的槽值，释放旧存储，再更新 begin/end/capacity；不是按 1.5 倍增长。旧逻辑 end 对应 `2 * oldBucketCount`，不对应所有已分配 capacity。随后 `01B4F0` 将 end 设为 begin，调用 `01BD30` 填目标数量的 sentinel 端点并更新 mask 与逻辑桶数。

已知 `01BD30` 的这一路现有容量足够；`021740(begin,begin,...)` 空范围不读写，`01EF70` 填每个 qword 端点。新 reserve 之后不会再次进入 `01BD30` 自己的增长分支。实现先完整分配并复制两组模型端点数组，再发布新数组/容量，随后填 sentinel。该安排只模拟正常成功时可观察的元数据，不承诺与原生异常清理等价。

新增 helper 的直接汇编证据：

| 函数 | 观察到的正常路径及边界 |
| --- | --- |
| `0x180022F00(count,size)` | count=0 返回 null；检查乘积溢出。字节数小于 4096 时直接申请该大小；至少 4096 时申请大小加 39，将返回指针向 32 字节边界对齐，并将原始 base 存在 aligned-8。 |
| `0x180023090(pointer,count,size)` | 检查乘积；至少 4096 字节时要求 pointer 为 32 字节对齐，读取 pointer-8 的 base，验证 base 在 pointer 之前且差值在 8..39，最后跳到释放入口 `0x1802DC390`。小块直接释放 pointer。 |

模型最大目标桶数 4096 对应 8192 个 8-byte 槽，乘积为 65536，相关长度与加 39 不溢出。成功域以实际原生存储满足上述有效地址/对齐契约为前提，模型自身不生成或解析这些地址。`0x1802DC390` 仍未追入其内部；已有定点证据只确认该入口是 E9 跳转。OOM、无效参数、异常处理及分配/释放内部未恢复，本轮不扩大 CRT 分析。

证据：[reserve 调用者](rea-container-support/11-Container-support-0x18001bfb0-assembly.json)、[桶重置调用者](rea-registration-containers/11-Container-whole-chain-followup-0x18001b4f0-assembly.json)、[申请包装](rea-container-reserve/05-Container-reserve-0x180022f00-assembly.json)、[释放包装](rea-container-reserve/08-Container-reserve-0x180023090-assembly.json)。

## 重排顺序与重复键

`0x1800207C0` 在重置新桶后保存**当时的全局旧尾节点身份**。循环每次重新读取 `head.Next`，将该节点交给 `0x18001FD50`；调用返回后比较本次节点与旧尾身份，相同则结束。它不是沿预先保存的 next 遍历，也不是按新桶号扫描。

`0x18001FD50` 按新 mask 对当前节点的 raw uint 键定位桶，再从该桶 last 反向查重：

1. 已处理桶中存在同键时，摘除本次节点、count 减一并释放该节点，保留已有节点和值；该分支直接返回，不调用增长维护。
2. 没有同键时，将本次节点移到该桶 first 前；新空桶则移到全局尾端。若它的 next 已是目标位置，则不做链 splice，仍按规则更新端点。
3. 新空桶 first/last 都设为当前节点；已有桶 first 变为当前节点，last 保留。
4. 非重复路径调用 `0207C0` 再维护，然后返回本次节点。单层域中 count 只保持或因去重减少，故每次嵌套维护都是可证明的无增长返回，不需要实现递归重建。

重置后，端点只指向本轮已处理的存活节点。当前未处理节点不会是这些端点；因此重复删除路径只摘全局链，不应调用普通 Erase 的桶端点更新。处理过的桶段构成链后部，未处理节点仍在前部；每次取当前 head.Next，直到处理旧尾。这一规则也覆盖**旧尾节点本次被重复删除**：仍按已保存身份结束，不能改成比较重排后的尾节点或读取被删节点的 next。

结果是新桶段按首次遇见顺序排列；每个新桶内，不同键在被处理时前插，形成相对于原链遇见顺序的逆序。等键只有首次遇见的节点存活，其 payload 不被后来的重复值覆盖。这是上述控制流在限定域的结果，不是依据常见哈希表惯例设定的策略。

例如，B=8 的原链 `[0,8,64,16,128,1]` 插入新空桶键 2 后为 `[0,8,64,16,128,1,2]`；增长到 64 桶后的新桶号依次为 `[53,61,53,5,53,36,23]`，最终链为 `[128,64,0,8,16,1,2]`。桶段不是按桶号排序。

旧尾重复例：原链 `[0(A),8,0(C)]`，同旧桶的新键 64 插入后为 `[64(N),0(A),8,0(C)]`。增长到 64 桶后最终为 `[0(A),64(N),8]`，原尾 `0(C)` 被删除，循环立即结束。若重排前反向查重会见到 C，重排后保留的却是本轮先遇到的 A；用字典“最后赋值获胜”会得出错误结果。

证据：[维护循环](rea-registration-containers/17-Index-post-link-maintenance-0x1800207c0-assembly.json)、[节点重排与重复分支](rea-container-support/14-Container-support-0x18001fd50-assembly.json)。

## 不支持范围及副作用

单层覆盖检查发生在原 Insert 已分配/链接新节点、更新端点和计数之后，任何桶 reserve/reset 之前。目标桶数超过 4096，返回 `RegistrationIndexGrowthBucketBudgetOutsideCoveredDomain`；一轮增长后仍超阈值，返回 `RegistrationIndexRecursiveGrowthOutsideCoveredDomain`。两者 Stage 都为 `PostLinkGrowth`，保留旧桶数/容量及新节点，`Inserted=null`、`MayHaveEffects=true`、`StateComplete=false`。这是一项明确的**模型覆盖边界**，不是声称原生在该处失败或没有后续副作用。

不预先去掉显式 seed 中的重复项来降低计数，从而绕过单层检查：原函数在首个非重复节点处理后就再次维护，尚未来得及删除后续重复节点。对输入完整且已超载的 seed，直接命中重复键仍不进入维护，因此可成功返回原节点而不增长。

实际 C# 桶数组分配 OOM 返回 `RegistrationIndexBucketAllocationFailureNotReconstructed`，Stage 为 `BucketReserve`。调用者此前的插入或清空前缀不回滚，状态标为不完整；不模拟原生 OOM/invalid-parameter 后的内部状态。未知前缀状态仍拒绝后续 Insert/Erase/Clear，不能当成成功完成的原生快照复用。

## 验证设计

新增 `RegistrationGrowthTests` 覆盖以下有判别力的行为：

- 8 倍和 2 倍增长，含旧 B=256 与 B=512 的分界；不足容量精确 reserve，充足容量保留。
- 两次独立调用先后从 8 增至 64、再增至 512，检查可变端点存储再次 reserve 后仍保留节点和值身份；这不扩大到一次调用中的递归增长。
- 新桶按首次遇见排列、同桶不同键前插；检查明确预期全局链、first/last、原节点及 payload 身份。
- 重排后继续删除和清空，确认端点与链一致；清空只回到 8 个逻辑桶并保留较大容量。
- 旧尾重复删除后终止、首个相同键值保留、相邻旧尾无需 splice 仍更新 first。
- 单层增长后负载等于阈值成功；仍超阈值及超 4096 保留原插入前缀并阻止后续突变。
- 不允许用未来的重复删除推迟首个嵌套维护；普通重复查重绕过增长。
- 原容量小于 8 时的全链删除和空 miss 成功 reserve 至 8，并可继续 raw uint 键插入。

主任务统一构建已通过 `RegistrationGrowthTests` 的 **141 项断言**；相关容器、协调器和既有模块合计通过 481 项断言及 1 个场景，13 个原版文件的哈希保持不变。完整结果见 [单层增长验证结果](REGISTRATION-GROWTH-RESULT.md)。静态证据调用成功不替代独立代码测试；本轮没有原生执行、原生地址恢复、原始源码恢复或完整物品工厂完成的声明。
