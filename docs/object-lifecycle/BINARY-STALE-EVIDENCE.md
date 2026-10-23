# Binary stale 名称缓存：已有证据与实现边界

本文证据复核仅读取工程中已有静态证据，补充 [warm 路径说明](BINARY-WARM-EVIDENCE.md)，不改写其历史范围；未新增 Rea/Ghidra 查询或索引，未读取/运行原游戏及存档/云数据。实现测试见 [本轮结果](BINARY-STALE-RESULT.md)。证据绑定 Engine SHA256 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`；下列地址为 preferred VA。

覆盖入口仍是完整地址已命中、旧 CachedId 可用于读取共享池中的缓存名字；仅当缓存名字与 query 的 C-string 内容不同，才进入 [`0x180283840` 的 `28396F–2839F8`](rea-record-getter-support/11-LoadTableBinary-name-to-id-0x180283840-assembly.json)。此路径不重新按地址选节点，不进入 cold 插入 helper。

| 顺序 | 已存指令与语义 |
| --- | --- |
| 复制 query 前缀 | `283972/28397F/283984` 初始化 capacity=15、length=0、首字节 NUL；`283989–283999` 扫描 query 的首个 NUL；`2839A3` 调 `0x180022CE0` 复制该显式长度，不包含终止 NUL 之后的 suffix。 |
| 查找 typed ID | `2839B5` 先令 EDI=-1，`2839B7` 调 `0x18003C8E0`；`2839C1` 比较 sentinel，命中时 `2839C7` 读取节点 `+0x30` 的 dword ID。这个值必须明确建模为 int32，不能把 opaque table、null 或未知 payload 转成 ID。 |
| 先释放临时字符串 | `2839CA–2839D3` 检查临时 capacity；heap 分支以 pointer、capacity+1、element size=1 在 `2839E3` 调 `0x180023090`；inline 分支直接前进。两者均在 ID 判定和缓存写回之前结束临时存储使用。 |
| 再判定与写回 | `2839E8/2839EB` 仅排除 `-1`；其他 int32 在 `2839F1 MOVSXD RAX,EDI` 后，由 `2839F4` 写入**原命中节点** `+8` 的完整 qword，`2839F8` 返回其低 32 位。 |
| 继续 numeric | miss 或显式 ID=-1 在 `2839EB → 28391A` 返回 -1，**不写旧 CachedId**。外层 `13FBB9/13FBBE` 仍把返回 EAX 交给 numeric getter，没有 `-1 → default` 捷径。 |

[`022CE0`](rea-record-getter-support/08-CString-copy-0x180022ce0-assembly.json) 在正常分配成功、有效且不重叠的源/新临时存储域内，复制显式长度并补终止 NUL；它不负责重新解释 query 编码。[`023090`](rea-container-reserve/08-Container-reserve-0x180023090-assembly.json) 保留其尺寸、对齐及大块分配元数据检查；模型只覆盖正常有效分配的释放契约，不执行原版 free，也不把 managed cleanup 等同于 native 异常行为。

[`03C8E0`](rea-record-load-dependencies/08-LoadFile-lookup-0x18003c8e0-assembly.json) 在 `03C930–03C942` 对临时字符串**完整 stored length**逐字节 MOVZX、XOR、乘法执行 FNV64，seed=`0xCBF29CE484222325`，prime=`0x100000001B3`；这里 stored length 已是首 NUL 前的前缀长度。查找按 hash 与原容器 mask 选桶，在完整桶范围内依顺序比较完整字节及长度，取首个相等节点；不调用 LoadFile 的大小写/斜杠归一化。可复用 [RecordCacheSnapshot](../../src/task_items/RecordCache.cs) 的不可变 key、FNV64 与完整比较，但其 **bucketCount 为正 2 的幂、native mask=bucketCount−1、同桶节点连续且链完整** 必须成为显式覆盖前提，不能推广到任意 mask，也不能混同 pointer/numeric 的 shift/mask 桶链。此实现要求节点含已知 int32 ID；未知节点在快照构造处拒绝，缺少完整快照时 getter 返回 Unsupported，不能将其当作 miss 或 -1。

**负非 -1 ID 照写。** 例如 -2 写成 `0xFFFFFFFFFFFFFFFE`，然后 numeric 将低 32 位再符号扩展作为 key。对本次查到的新 ID，写回前没有 pool 范围校验；即使 numeric 随后缺失、返回 default/known null 或遇到未覆盖数据，写回也已经发生。旧 CachedId 在进入 stale 前读取缓存名字时的有效池前提仍保留，不能混同这两个时点。未来再次使用已写回但无法定位有效池元素的 CachedId，可在那次读取处报 Unsupported，不回滚前次写入。

**传播副作用，保持锁分离。** cache-write 的事实、原节点身份及写入前后值应随 getter 子结果传播至 CreatePartial；后续 factory/名称赋值或 numeric 的失败不抹去它。miss/-1 不发生该写，但外层已有 LoadFile hit 等其他效果仍各自保留。名称解析、写回与 numeric 均在 [`13FB60`](rea-record-class-getters/08-LoadTableBinary-class-getter-0x18013fb60-assembly.json) 的同一表 gate 内，结束后再解锁；不得与 manager `+0x10` 的 allocator 锁、manager `+0x28` 的 registration 锁合并。它不自动预留 ID、绑定 native Read 或闭合上层自动生命周期。

**下一真实缺口仍是 cold 插入 `0x180284160`。** 调用点 `283915` 及 [direct callees](rea-record-getter-support/12-LoadTableBinary-name-to-id-0x180283840-direct-callees.json) 已保存。未来最小分析请求仅为该地址的 `procedure_pseudo_code`、`procedure_assembly`、`procedure_callees` 三项；加现有 open/session/close 生命周期检查共 **8 calls**，不自动递归追查后续目标，也不保证一次闭合。没有可靠的该查询峰值内存证据，少量查询仍可能触发整个 Engine 导入/分析；**UE 内存不足期间不启动、不后台等待重试**。本轮这些请求未执行，cold 插入仍保留 Unsupported，不以直接返回查得 ID 代替插入。
