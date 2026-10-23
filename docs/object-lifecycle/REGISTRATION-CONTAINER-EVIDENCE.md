# 对象注册容器：四批静态证据与实现边界

后续范围见 [单层增长证据](REGISTRATION-GROWTH-EVIDENCE.md) 与 [增长验收](REGISTRATION-GROWTH-RESULT.md)。下文记录前一阶段的条件域；其中增长及小容量清空的 Unsupported 边界已在后续正常成功域内扩展，历史源码和验收保留在阶段检查点。

日期：2026-10-19。本记录依据 Engine.dll 的静态结果，不运行原版、不读取存档。第一批 18 次调用见 `rea-native-registration/`，第二批 20 次见 `rea-registration-containers/`，第三批 17 次见 `rea-container-support/`，第四批 11 次见 `rea-container-fill/`。调用成功表示工具返回成功，不表示全部下层语义已闭合。独立实现及测试为 `src/task_items/RegistrationIndex.cs` 与 `RegistrationIndexTests.cs`；只覆盖下述明确条件域。

目标：`D:\Steam\steamapps\common\Grim Dawn\x64\Engine.dll`；SHA256 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`。

## 已闭合的结构和键规则

以下来自指令读写，而非依据 STL 类型名猜测：

| 位置 | 已观察用途 |
| --- | --- |
| 容器 `+0x00` | `float` 增长阈值，由 `0x1800207C0` 比较 |
| `+0x08` | 双向循环链的 sentinel 指针 |
| `+0x10` | 节点数量，64 位 |
| `+0x18/+0x20/+0x28` | 桶端点向量的起始、当前末尾、容量末尾坐标；已有容量足够时的全清已闭合，reserve 不在实现域 |
| `+0x30` | 哈希桶 mask |
| `+0x38` | 逻辑桶数量 |
| 节点 `+0x00/+0x08` | next/previous |
| 节点 `+0x10` | 比较用的原始 uint 键 |
| 节点 `+0x10..+0x1F` | 索引插入时复制的整个 16-byte payload；调用者在 payload `+0` 放 uint 键、`+8` 放对象指针，中间 padding 不得臆造语义 |

哈希：以 `0xCBF29CE484222325` 为起点，按 uint 的小端四个字节依次做 `hash = (hash XOR byte) * 0x100000001B3`，每步 64 位环绕；桶号为 `hash & mask`。不能掩掉键的高 4 位，也不解释字符串名称。每个桶占两个 8-byte 端点，分别为 first 与 last；空桶两者均为 sentinel，非空桶的 exclusive end 是 `last.next`。

建议独立模型显式保存 sentinel、节点身份、双向全局链、每桶 first/last、数量、mask、逻辑桶数和已知容量。只用字典赋值无法表达已观察到的顺序、同键范围及已有节点身份。C# 引用可表示独立模型里的节点/对象身份，不声称恢复了原生地址或内存管理。

## 范围选择与删除

`0x18017FE40` 没有直接 callee。它从目标桶的 first 沿 next 扫到 `last.next`，找到第一个相同 uint 键后，继续沿 next 扩展**连续**的同键段。结果为 `[firstMatch, firstDifferentOrBucketEnd)`；未命中返回 `(sentinel,sentinel)`。

因此它不保证删除全表所有散落的相同键，也不证明 `manager +0x128` 本身只允许唯一键。若独立模型支持初始快照里的重复项，必须保留其顺序；不能把快照预先转为 HashSet 后丢失多重性。

`0x180020880` 的单节点删除顺序已经清楚：

1. 用被删节点的 uint 键定位桶。
2. 若节点同时是 first/last，将两端设 sentinel；仅为 last 时改 last 为 previous；仅为 first 时改 first 为 next；中间节点不改端点。
3. 修改前后节点的连接，节点数量减一。
4. 调用 `0x1802DC390`（伪代码标为 `free`），返回原 next。

`0x1802DC390` 的释放入口身份尚未闭合。定点静态字节证据是 `E9 A9 17 00 00 CC`，不是预期的 IAT 跳板；本轮没有追跳或扩大 CRT 分析，见 [定点检查](registration-container-validation/free-thunk-0x1802dc390.json)。独立模型仅在节点分配/释放正常成功的契约前提下重建调用者层的链和桶元数据，不等价实现分配器、释放器及其内部副作用。调用者层未观察到对 payload 对象执行析构或注销；模型不调用 payload 析构，不能据此证明未知释放入口内部绝无其他行为。

`0x18017FCA0` 先计数所选范围，再删除并返回该计数：

- 部分范围：逐节点调用 `0x180020880`，其他节点顺序保持。
- 恰好覆盖全链：重置 sentinel 的 next/previous，将数量置零，逐节点调用上述释放入口，然后调用 `0x18001B4F0(container,8)`。
- 非空容器未命中：空范围不等于整链，返回 0，没有节点删除。
- **空容器未命中：空范围就是整链，仍调用 `0x18001B4F0(container,8)`。** 不能把返回 0 统一解释成状态完全不变。

`0x18001B4F0(container,B)` 计算所需端点项数 `2*B`。若现有向量容量不足，会调用 `0x18001BFB0`；随后将向量当前末尾写回起始，并调用 `0x18001BD30`，传入起始、项数及 sentinel 的地址；正常返回后写 `mask=B-1` 和 `bucketCount=B`。因此全清正常完成后逻辑桶数是 8、mask 是 7。第三、四批已证明已有至少 16 个指针槽容量时不分配、只填 16 个 sentinel 端点并将当前末尾设为起始加 128 字节，已分配容量保持原值；具体调用链见后文。

证据：[范围选择](rea-registration-containers/04-Container-range-selection-0x18017fe40-pseudocode.json)、[范围汇编](rea-registration-containers/05-Container-range-selection-0x18017fe40-assembly.json)、[单节点删除](rea-registration-containers/07-Container-node-removal-0x180020880-pseudocode.json)、[全清后处理](rea-registration-containers/10-Container-whole-chain-followup-0x18001b4f0-pseudocode.json)。

## 插入、重复键和桶顺序

连接第一批 `0x1801356F0` 的汇编可得：

- 重复检查从桶 last 沿 previous 向 first 扫描。命中返回该已有节点，结果布尔为 false；不覆盖 payload，不增加数量，不调用分配和增长维护。
- 对结构正常且键唯一的索引，新键在**已有桶**插到该桶 first 之前；该桶 first 更新为新节点，last 不变。
- 新键对应**空桶**时，新桶段附在全局链末尾，first/last 都变为新节点。
- 新节点暂先建立在全局链头，再按上述位置重接。若只暴露操作完成后的模型状态，可按最终位置实现，但失败/异常路径不能据此假定原版是原子的。

`0x180021A80` 调用 `0x180020960`，然后将调用者 payload 的 16 字节复制到返回节点 `+0x10`。其伪代码错误地推断返回类型为 void；汇编保留 RAX，且上层把 RAX 用作新节点。第三批证明 `0x180020960` 请求 32 字节，正常成功后将传入的 next/previous 写到节点 `+0/+8`；若传入 next 为零，改为新节点自指。这里正常插入调用传入旧全局 first 和 sentinel。分配失败、OOM 及异常后的状态仍未闭合。

在分配 helper 返回后，`0x1801356F0` 才检查数量极限 `0x07FFFFFFFFFFFFFE`，然后链接节点、增加数量并调用 `0x1800207C0`；该维护正常返回后才输出 inserted=true。极限异常发生在节点分配之后；OOM、异常清理和回滚均不能推断。

证据：[原始插入汇编](rea-native-registration/15-Registration-index-helper-0x1801356f0-assembly.json)、[节点 payload 复制](rea-registration-containers/14-Index-node-allocation-0x180021a80-assembly.json)。

## 增长触发与容量规则

`0x1800207C0` 的汇编证明：

1. 将节点数量和逻辑桶数按 unsigned-64 到 binary32 的实现方式转换，再以 `DIVSS` 计算负载比。
2. `COMISS` 后 `JBE` 跳过增长，因此有限正常数值域内仅在 `load > threshold` 时增长，等于阈值不增长。NaN、异常掩码和极端转换不纳入首个模型覆盖域。
3. 若需要增长且旧桶数小于 `0x200`，新桶数为旧值乘 8。
4. 若旧桶数至少 `0x200`、但小于 `0x0FFFFFFFFFFFFFFF`，乘 2；达到或高于后一个界限时保持桶数，然后仍进入重建流程。
5. 调用 `0x18001B4F0(container,newBucketCount)`，再按链头逐个调用 `0x18001FD50` 重排原节点，直到处理完原链尾节点。

这里 `0x1800207C0` 伪代码把 `0x18001B4F0` 第一个参数误写成 `float fVar5`；汇编的 RCX 保留容器指针，应以汇编为准。不能只按伪代码签名移植。

已知增长倍率不等于已恢复增长。虽然填桶的已有容量路径现已闭合，但 reserve 的底层分配、迁移释放及完整 rehash 仍在覆盖域外。实现遵循已观察的顺序：**先链接新节点、更新端点和数量，再在增长维护入口返回 Unsupported**。保留该前缀的实际副作用，不提前拒绝并伪称未插入，也不假装执行了扩容。此时 `Inserted=null`，因为原生 inserted=true 输出在维护返回之后；`Node` 可指向已链接的模型节点，`StateComplete=false`，后续 Insert/Erase/Clear 均拒绝继续突变。此状态不能当作完整原生快照重用。

证据：[增长维护汇编](rea-registration-containers/17-Index-post-link-maintenance-0x1800207c0-assembly.json)。

## 第三、四批补证及已实现条件域

第三批已读取此前所列四个 helper 的伪代码、汇编和直接 callees；第四批补齐已有容量填桶路径上的两个直接 callee。以下是实际闭合范围，不由容器名称或惯例推断：

| 地址 | 已观察行为与实现边界 |
| --- | --- |
| `0x180020960` | 请求 32 字节；正常成功写 next/previous，配合 `021A80` 的 16-byte payload 复制，支持正常成功分配的元数据模型。 |
| `0x18001BD30` | 全清时 end=position=begin。已有容量足够且填充数量 16 时，先调用 `021740(begin,begin,begin+16)`，再 `01EF70(begin,16,&localSentinel)`，随后 end 增加 128 字节；这一路的后续原有元素填充循环为零次。 |
| `0x180021740` | 空源范围时计数为零，直接返回目标地址，不读写内存、不调用其他函数。第四批 04/05/06。 |
| `0x18001EF70` | 有效目标存储及 count=16 时，逐项写入传入 qword sentinel，共 16 次，无直接 callee。第四批 07/08/09。 |
| `0x18001BFB0` | 调用 `022F00` 分配、`021740` 迁移、`023090` 释放旧存储，再更新 begin/end/capacity；本轮不实现 reserve。 |
| `0x18001FD50` | 重排节点时也反向查重；发现重复会移除并释放当前节点、减 count；未重复时重接并再次调用 `0207C0`。因此不能将 rehash 简化成仅换 mask；本轮不实现。 |
| `0x1802DC390` | 尚未确认实际释放器身份；只取正常释放成功契约条件，不声称无未知内部副作用。 |

证据：[节点初始化](rea-container-support/05-Container-support-0x180020960-assembly.json)、[端点向量填充调用者](rea-container-support/08-Container-support-0x18001bd30-assembly.json)、[空范围迁移](rea-container-fill/05-Container-fill-0x180021740-assembly.json)、[qword 填充](rea-container-fill/08-Container-fill-0x18001ef70-assembly.json)、[重排及重复释放](rea-container-support/14-Container-support-0x18001fd50-assembly.json)。

已实现的是明确自有快照上的 raw uint 查重、无增长成功插入、首次连续同键范围删除、容量足够时整链清空和空 miss 重置。类内使用显式 sentinel 双向链与每桶 first/last，未使用 Dictionary 或 HashSet 替代。节点引用和值对象身份均保持，不恢复原生地址或 payload padding。

构造器 `RegistrationIndex(int bucketCount, int bucketCapacity, float maxLoadFactor, RegistrationEntry[] seed)` 的容量单位是**桶数**（原生端点指针槽数量的一半）。seed 是调用者明确提供的完整模型状态，按输入顺序建链，各桶在全局链中必须连续，允许同桶存在重复键甚至被不同键隔开的同键段；不解析原生内存，不读取存档。输入数组在构造后不保留为可变状态来源。

自身覆盖限制：bucketCount 为 1..4096 内的正 2 的幂，bucketCapacity 在 bucketCount..4096 内，节点数量至多 65536，阈值为有限正 float。这使小计数和 2 的幂除数下的 binary32 转换/除法精确。限制是重建模型范围，不是原版限制；不满足构造约束的显式快照报参数异常，未知快照不能默认为空。

`RegistrationIndexResult` 的 `Supported/Reason/MayHaveEffects/Stage` 区分完成和边界；`Inserted` 仅在 Insert 正常返回时为 true/false，重复时 `Node/Value` 返回原节点及旧值；`RemovedCount` 仅在删除/清空正常返回时给出数量。全清且容量不足 8 桶时先清空链、数量归零，然后在 reserve 入口停止，返回 `MayHaveEffects=true` 并使 `StateComplete=false`，保留旧桶元数据，不伪造成功填桶。成功全清只写前 8 个 first/last 及逻辑桶数，不缩小 capacity；未使用槽不成为逻辑快照。

模型节点数量上限检查发生在分配前，是显式的自身预算边界，返回 Unsupported 且无变化；它不模拟原生极端 count 检查（原生发生在分配后）。实际 C# 节点分配 OOM 返回 Unsupported，不承诺原生 OOM 的状态或清理等价。未知增长/clear reserve 与这种分配前预算检查严格区分，前者保留已经发生的前缀副作用。完整原生分配器/free、reserve、rehash、异常清理、极端浮点及原生内存无效状态都不在成功域。

## 与对象注册和现有模块的界线

本记录能支撑的是容器操作模型，不是完整物品工厂。首次建议不改 `ObjectIdAllocator` 的 `Gate`、`IndexedKeys` 或 `ItemWorld.Objects`。后续若接入，必须明确共同 manager 临界区及实时 ID 索引，保留分配器不预留 ID、使用 raw counter 查键的既有行为。

`FinishPartial` 已观察顺序为：记录加载成功后锁外写 `Object+0x2C=ID`，入锁，按 ID 删除 `manager+0x128` 范围，插入 `+0x30` ID 索引，插入 `+0x70` 原始 `Object+0x28` uint 键索引，解锁，再调用对象 vslot `+0x08`。两次插入返回的布尔均被忽略；重复键不阻止后续注册步骤或 Read，不能新增“重复则失败/回滚”的策略。

完整 `LoadFile`、`SetObjectName`、具体构造器和 vslot Read 仍未由本批证据恢复。`ClassFactoryDispatch` 的自有快照/自供回调协议也不补齐这些缺失部分。

## 有判别力的测试设计

- 验证 uint 0、最高位、相同低位不同高位及小端四字节哈希；不同桶和同桶冲突均覆盖。
- 重复键传入不同对象，返回原节点/对象，数量及链顺序完全不变；缺失状态不当作空。
- 新空桶段在全局尾部；已有桶新键在该桶首部；交错桶插入后仍保持每个桶连续。
- 范围只选择首次命中后的连续同键段，保留同桶不等键以及后面的分离同键段；桶边界不能越界。
- 删除 first/last/middle/唯一节点后检查链序、端点和数量；随后反向查重及再插入检查剩余节点可正常使用。
- 非空未命中无变化；空容器未命中触发逻辑桶重置路径；整链删除返回原数量并重置到 8 桶，不能把保留容量错写成缩容。
- 负载恰好等于阈值不增长；略超阈值为首版覆盖域外；已存在键不走增长检查，即使起始容器已超阈值。
- growth 和 clear reserve 检查已有插入/删除副作用及 `StateComplete=false`，后续三种突变均拒绝；完整超载 seed 上的重复键仍可成功，因为没有调用维护。节点预算边界则明确无变化。
- 完整协调器仅在依赖闭合后测试：Read 在锁外、两次重复插入仍会到达 Read；不得用只检查字典项目数的测试宣称原生注册完成。

主任务统一构建执行已通过 `RegistrationIndexTests` 的 183 项断言；跨模块验证结果与总体边界见 [对象注册模型结果](REGISTRATION-MODEL-RESULT.md)。源文件与测试仅实现上述自有容器条件域。未接入旧 `ObjectIdAllocator`、未执行原版、未读写玩家存档或 Steam 云，也不声称完整原生对象注册已完成。
