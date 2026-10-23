# Binary warm getter：已有证据与覆盖边界

依据 [下一边界分析及证据清单](NEXT-BOUNDARY-ANALYSIS.json)，本文证据复核仅读取工程证据与源码，所列证据哈希核对一致；未启动 Rea/Ghidra 或索引，未读取/运行原游戏及存档/云数据。实现测试另见 [本轮结果](BINARY-WARM-RESULT.md)。证据绑定 Engine SHA256 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`；以下为 preferred VA，不是推定的运行时地址。

本轮只接通 **archive 存在、指针缓存命中、C-string 内容仍相同** 的路径。完整稳定快照由调用方明确提供，不代表恢复了 parser、池构造、缓存填充或 native Read。

| 证据 | 精确行为 |
| --- | --- |
| [Binary 外层 `0x18013FB60`](rea-record-class-getters/08-LoadTableBinary-class-getter-0x18013fb60-assembly.json) | `13FB94` 加表内锁；archive 为 `*(table+0xC8)`，缺失则保留同一 default；`13FBA7` 传 `archive+0x88` 给名字解析器，`13FBB1` 调 `283840`；`13FBB9/13FBBE` 将 EAX 原样交给 `13FC80`；`13FBCA` 解锁。没有 `-1` 提前返回。 |
| [指针缓存 `0x180283840`](rea-record-getter-support/11-LoadTableBinary-name-to-id-0x180283840-assembly.json) | context `+0x58/+0x60/+0x70` 为 shift/mask/桶数组；节点 `+0/+8/+0x10` 为完整 64 位地址/CachedId/next。`283880` 比较完整地址，`283889` 沿链前进，取首个匹配。 |
| [Numeric getter `0x18013FC80`](rea-record-getter-support/14-LoadTableBinary-id-getter-0x18013fc80-assembly.json) | `13FC8A` 将 EDX 符号扩展为 64 位 key；table `+0xA0/+0xA8/+0xB8` 为另一套 shift/mask/桶数组。节点 `+0/+8/+0x10` 为完整 key/payload/next，`13FCB3/13FCB8` 按完整 key 顺序查链。 |

两种桶索引均为 `unchecked((uint)(key64 >> (int)(shift & 63))) & mask32`：`28386A/28386D/283870` 和 `13FCA2/13FCA5/13FCA7` 分别执行 **低 6 位 shift 的 64 位逻辑右移 → 截低 32 位 → AND mask**。不能先截 key，不能将 mask 自动替成 bucketCount−1。[BinaryWarmTable.cs](../../src/task_items/BinaryWarmTable.cs) 保留两套完整桶链、桶内顺序和首个匹配；空行是已知空桶，缺失组件不是空桶。空外数组表示 null 桶数组：名称路径仍停于 cold 边界，numeric 路径返回 default。快照预算是自有覆盖限制。

`283938–283940` 先用**完整 64 位 CachedId × 32**选名字字符串，仅在内容相同后由 `2839F8` 返回低 32 位。不得先截断 CachedId，或把无效池索引/指针绕回解释成 native null。名字 context=`archive+0x88`，所以其 begin 字段 `context+0x40` 正是 `archive+0xC8`；numeric 也读 `*(archive+0xC8)`，end 为 `*(archive+0xD0)`。这里的字段地址不是数组起址。两条路径使用**同一个 `TableString[]` 池及元素身份**，不能拆成独立名字/值字典。

`283954–283969` 以 MOVZX 逐字节比较缓存字符串和 query，首个差异进入 stale，相等 NUL 结束；不解释编码、不折叠大小写、不比较 NUL 后 suffix。query 使用显式 64 位逻辑地址和当前可读字节，不从 .NET 对象身份、hash 或 GC 地址推导；同字节不同地址不自动命中，同地址也必须验内容。有效池索引、可读 query 与稳定存储是前提。

Numeric 的缺失节点或 `13FCCE` 的 tag≠2 返回**同一 default**（`UsedDefault=true`）。tag=2 时，payload `+4` 是 uint buffer index；`13FCE1` 从 `*(table+0x60)` 的 int-buffer 读取 signed string index。`13FCFA–13FD1D` 对负数/池外 index 返回**已知 null**，不是 default；有效 index 返回同一池元素，即使 stored length 为零或首字节为 NUL。未知 payload、未知 buffer、越界 buffer index 属于 Unsupported，不能伪装成原生的 null/default 分支。

[RecordTableGetter.cs](../../src/task_items/RecordTableGetter.cs) 在同一表 gate 下连接上述两步，保留 Binary 子结果与存储身份；[CreatePartialObject.cs](../../src/task_items/CreatePartialObject.cs) 将 getter 返回的 C-string 交给工厂。其 `Class\0` 逻辑地址由 `classQueryAddress` 明确传入：已有 caller 在 `0x18017E1A0` 的 `LEA 0x18031B34C` 不自动成为运行时指针。旧重载保留 unknown 地址；普通表/absent archive 不要求地址。Binary known null 进入既有工厂 null-query 边界，不改为空类名。

**Cold/stale 保持 Unsupported，不写缓存。** 已存证据表明 cold 名字查到非 `-1` ID 后在 `283915` 调缺口 helper `0x180284160`；stale 查到非 `-1` ID 则在 `2839F4` 写回原节点。两者此次均未实现。名字未找到或返回 `-1` 的分支仍须按 native 外层继续 numeric，不能增设 `-1 → default` 捷径；当前有限 warm 池也不支持用越界 CachedId 制造负 NameId。将来加入 stale 写回须传播副作用，不能因下游失败回滚。当前 getter 只读，已有 LoadFile 命中计数和后续 own callback/名称写入仍按各自结果保留。

**锁边界仍分离。** [allocator 证据](../rewards/rea-object-ids/ObjectManager-ID-assembly.json) 的 `17E4C5/17E4C9` 与 `17E61D/17E621` 使用 manager **`+0x10`**；`17E543` 调 [查找 helper `02DD80`](../rewards/rea-object-ids/Object-ID-map-find.json)，没有预留 ID。[FinishPartial](rea-native-registration/05-ObjectManager-FinishPartial-order-assembly.json) 的 `17E30C/17E310` 与 `17E377/17E37B` 使用 manager **`+0x28`**。同一 Enter/Leave import 不证明同一锁对象；table、allocator、registration 不合并 Gate，不自动接入 ObjectIdAllocator，不宣称上层自动生命周期、pending 插入或销毁已恢复。
