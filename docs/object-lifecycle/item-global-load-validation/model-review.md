# BinaryFloatTableGetter 源码独立静审

2026-10-21。只读审查 `src/task_items/BinaryFloatTableGetter.cs`，**15663 B，SHA256 `2289b851867fd604b2d1c35aa4676f2f41bb4c7148bbc5274a339a1192330aa7`**。同时定点核对既有 `BinaryRecordParser.cs`、`BinaryWarmTable.cs` 的 parser 插入顺序、count 预检查及字段桶算法。没有修改生产源码、运行程序/编译/测试或读取原版。本结论不是执行验收；尚未评审后续两字段加载 window 的最终版本。

**结论：所声明的正常 fresh parsed own 覆盖域内，未发现阻断问题。** 原始证据分别见已冻结的 [numeric-review.md](numeric-review.md)、[direct-review.md](direct-review.md)；前者 SHA `b08b071816ea0a17bf77351be99c97ac8f20d9533556dfdcbe00e9a234d82bd9`，后者 SHA `ade55f7667899e363ac9607c6be4d16265047bcf6cf72b80bf3c585933ba7070`。本文件不会修改这些取证文档。

## 主要核对结果

1. **先名称，再数值。** `BinaryFloatNameLookup.Resolve` 复用现有名字缓存节点、typed ID 索引、共享字符串池和插入 primitive；它没有调用旧 tag2 string numeric getter。保留完整 query 地址、warm 的内容复核、stale 写回与 cold 插入/增长 effects。名称正常返回 -1 仍进入 numeric，未增加 default 捷径。
2. **默认路径按需读取。** 明确 absent archive 不要求 query/name cache/parsed record；有 context 的 field miss 或 tag≠1 不要求 parsed 的 count/raw buffer。未知 default 只在真正选择默认结果时阻断；未知 context、payload、buffer 没有被改成正常 default。
3. **桶与实际首节点。** numeric 调用 `warm.Fields.Find(unchecked((ulong)(long)id))`，沿既有完整 key 桶链选首匹配。既有 `BinaryBuckets.Bucket` 保留 qword 逻辑 shift、shift 后 uint 截断、mask 的顺序。没有按低32位 key 或“找到任意 type1”替代首匹配。
4. **parsed 身份约束。** 到达 tag1 后才要求 `ReferenceEquals(parsed.Gate, gate)` 及 `ReferenceEquals(parsed.Fields, warm.Fields)`。这使 count/raw words 来自同一个不可变 fresh-parser 表示；不能用另一个字段内容相似的记录拼接出成功。该引用守卫是自有表示的覆盖约束，不是原生额外分支。
5. **重复 descriptor 选择。** parser 按读取顺序建立 descriptors，但把新 field node prepend 到桶链。实现先取得实际首节点，再倒序选择最后一个完整 key 相同的 descriptor，核 tag 与 BufferIndex。因 `parsed.Fields` 同引用且不可变，这对应该首节点；没有错误选择较早重复项。若首节点为其他 tag，直接默认，不退回旧 type1。
6. **索引与原始位。** uint 无符号比较、`unchecked(count-1)`、`unchecked(baseIndex+selected)` 与 `CopyRawValues(1)` 的 DWORD 取值匹配正常域。没有 CLR 解析、float/double 转换或 finite/NaN 筛选。最终 own buffer 越界停止，没有伪称 native 存在同样 bounds check。
7. **失败前缀。** 同一 effects 对象在名称过程前创建，numeric missing parsed、身份不符、unsupported 或 own exception 都不回滚已完成缓存写入。返回结果保留 `Name`/`Numeric` 及独立缓存修改标记；catch 不把异常变成 Known/default。

## 明确保留的限制

* `count==0` 在原生 140420 中会 wrap 为 FFFFFFFF；实现的 `ZeroFloatElementCountOutsideFreshParsedDomain` 不是原生失败或默认分支。既有 `DeserializeFresh` 的 precheck 明确拒绝零 count，且 parsed/descriptor 构造入口非公开，所以本次允许的真实 fresh parsed 输入不会到达该状态。若未来扩大 parser 域，必须重新处理零 count，而不能沿用此拒绝充当原生行为。
* table-kind、archive/context、query 逻辑地址、同步和存储有效性仍由调用者明确提供。模型不执行 native vtable；固定 managed Gate 是 own 表示，不能据此声称原生的 gate 指针永不变化或任意 loaded vptr 都相同。
* managed `finally` 在 unsupported/own exception 时退出 Gate；注释已明确它不恢复原生 EH、并发原子性或 rollback。正常字符串分配/释放、托管分配失败也不是 native allocator/unwind 实现。
* 描述符和 raw words 的副本只是在稳定 own 状态下读取所需数据；它们不是 native 逐指令访问计数或峰值内存证明。此次没有执行任何测试，也未验证正式原始样本、完整 GameEngine 初始化或完整 native table 生命周期。
* Base getter 的 `4DD80` 与格式串合同、GetLoadTable fallback 的类型/创建与存续、真实 archive 初始化及加载仍各自保留。Binary 数值路径通过不能用来默认这些未知输入。

后续 window 组合若继续接入此 getter，需要保留两次 getter 及中间第一次字段写入的顺序，并保留第二次 getter 失败时第一次字段和两次名称缓存的既成副作用。其最终源码在本文件首次冻结时尚未评审，不能把本结论扩大为整个加载 window 已通过。
