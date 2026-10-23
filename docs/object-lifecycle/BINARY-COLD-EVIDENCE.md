# Binary cold 插入：有剩余容量的静态证据

本文依据 [已完成的静态查询记录](BINARY-COLD-QUERY-RESULT.md)、新 [`0x180284160` 汇编](rea-binary-cold-insert/05-Binary-cold-insert-0x180284160-assembly.json) 与 [旧名称解析 caller](rea-record-getter-support/11-LoadTableBinary-name-to-id-0x180283840-assembly.json)。新汇编 SHA256 已核对为 `0ef12f91312c6b8a06284d249927b6873bb531c3f3de690f4c7d581e1199ea41`；证据绑定 Engine SHA256 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`，地址均为 preferred VA。本次文档工作只读取工程资料，没有启动 Rea/Ghidra、索引、编译或测试，没有读取/运行原游戏及存档/云数据。

**有剩余容量的正常域已实现，独立静态复核与自有测试均已通过。** 实现范围限于明确有效的缓存存储且 used count 小于 capacity 的插入；真实扩容 helper `0x180284200` 仍未知。

验证记录：新增 BinaryColdGetterTests **50 条断言、2 个全链场景**通过；既有 **844 条断言**全部重跑，包括单独补跑的 ObjectId **20 条**。本阶段合计 **894 条断言、12 个组合场景及既有 ItemQuestDemo**。见 [模块测试日志](binary-cold-validation/build-output.txt)、[独立 ObjectId 回归日志](binary-cold-validation/object-id-regression-output.txt)及已生成的 [验收清单](binary-cold-validation/acceptance-binary-cold.json)。首次测试编译的 CS0136 仅通过重命名循环变量修正；ObjectId 首次命令失败后补用绝对 Windows **源码**路径与既有 TokenActions 引用，独立编译/运行通过，编译器路径从一开始就是绝对路径，attempt 日志保留。模块阶段记录的 5.3632597 秒包含该首次 ObjectId 命令失败，之后独立补跑为 0.2234634 秒；这些是各阶段实测，不将失败尝试改写为成功运行。本实现验证未新增 Rea 查询或原版读取。

| helper RCX 相对偏移 | 精确含义 |
| --- | --- |
| `+0x00 / +0x08` | uint32 shift / mask；桶索引为 `uint32(address >> (shift & 63)) & mask` |
| `+0x0C / +0x10` | uint32 capacity / used count；`28417F CMP`、`284182 JNC` 作**无符号**比较 |
| `+0x18 / +0x20` | 桶头指针数组 / 连续 slot 数组；每 slot `0x18` 字节 |
| slot `+0 / +8 / +0x10` | 完整 64 位 query 地址 / 完整传入 ID / next 指针 |

这里 RCX 是名称 context 的 `+0x58` 子结构，不是 archive 或 table 本体。有容量时，`284184–284198` 选取 **`slotBase + oldCount * 24`**；逻辑地址用完整 64 位值，shift 只取低 6 位，mask 作用于移位结果低 32 位。count、capacity、slot 存储及桶头关系必须有明确有效的快照依据，不能用 .NET 容器容量、query 对象身份或假想 heap 地址替代。当前 slot 的正常可写性是覆盖前提，不预测无效 native 地址的行为。

[BinaryPointerCacheSnapshot](../../src/task_items/BinaryWarmTable.cs) 的六参数构造器显式接收 usedCount、capacity、completeSlots，以固定数组 index 作为 slot 身份。已用 slot 的唯一节点引用与全部桶链节点一一对应；未用的 null cell 表示已分配、可写的空槽，不是未知存储。旧构造器保留“缺少插入 metadata”的状态，不自动推定容量。

| 写入次序 | 已观察动作 |
| --- | --- |
| `2841A0` | 将 used count 写为 oldCount+1 |
| `2841A3` | 写新 slot 的完整 query 地址 |
| `2841AE` | 写新 slot 的完整传入 ID |
| `2841B6 / 2841BA` | 读取旧桶头，写入新 slot.next |
| `2841C2` | 将该桶头改为新 slot |

这是一项**前插**：原链顺序保留在新节点之后；有容量分支没有分配、释放、外部调用或重复检查。`2841C6` 仅确认 AL=1，不能凭反编译推断整个 RAX 的返回类型。caller 不使用该返回值：`28391A` 从此前保留的 ESI 返回名字 ID，再交给 numeric getter。

自有实现先准备尚未挂接的 node 和替代桶行，再开始可观察的 `Count → slot → head` 发布。这样承载正常成功结果及 slot/链身份；它**不声称逐条原生内存写入之间的故障、分配异常或中途异常行为等价**，也不把这些写入称作原子操作。

**调用方顺序不能提前。** 指针地址未命中后，caller 先扫描 query 到首个 NUL，复制前缀，按完整前缀字节 FNV64 查 typed int32 名字 ID，再在 `2838E8–283901` 完成临时字符串正常释放。`283906/283909` 对名字 miss 或值为 -1 直接跳过插入，返回 -1 继续 numeric；这些分支不需要可用插入容量，不能因为容量未知或已满而提前拒绝。其他 ID 在 `28390B MOVSXD R8,ESI` 符号扩展，`28390E` 传子结构、`283912` 传原 query 地址，再于 `283915` 调插入；负非 -1 ID 也照传，不预先以字符串池范围拦截。

当前 legacy 快照只要有已知 NameIds，cold 路径也先完成查找与临时字符串释放：ID=-1 继续 numeric；仅非 -1、确实需要插入时才因 metadata 缺失返回 `UnknownBinaryPointerCacheInsertionStorage`。已知满容量则停于 `UnimplementedBinaryPointerCacheGrowth:0x180284200`。

**重复与身份边界。** helper 本身不是 insert-unique；直接调用时没有“重复即无变化”的证据。正常稳定完整 caller 路径已先按完整地址搜索：同地址命中走 warm/stale，不再次 cold 插入；同字节但不同地址仍可各自插入，不能按字节全局去重。碰撞桶的新节点必须成为桶头，保留原节点与链身份，不能用排序或覆盖旧值替代前插。

**后续失败保留副作用。** 插入完成后，即使 numeric 找不到字段、返回 default/known null、遇到未覆盖数据，或 CreatePartial 的后续工厂失败，used count、新 slot 和桶头都已经改变。Binary 子结果现报告 `Cold`、`CacheCountWritten`、`CacheNodeInserted`、前后 count 与 `InsertedSlotIndex`；getter 的 `MayHaveEffects` 包含 count/insertion 写入，并经 CreatePartial 原聚合传播。无容量分支在转入未知扩容前尚未执行上述 slot/count 写入；前序 LoadFile hit 等效果仍独立保留。Numeric 保持先读取 buffer、再要求 pool metadata、最后判断负/越界 string index 的顺序，不因负数预先跳过必要读取。整个名称解析、插入与 numeric 顺序仍在既有表锁内；不与 manager `+0x10` allocator 锁或 `+0x28` registration 锁合并，不自动绑定原生 Read 或预留对象 ID。

**下一最小查询仅针对 `0x180284200`。** `count >= capacity` 时 `2841D8` 在 EDX 中以 uint32 加法翻倍 capacity，`2841DA` 调 `284200`，随后 `2841E8` 递归调用 `284160`。已有 [direct callees](rea-binary-cold-insert/06-Binary-cold-insert-0x180284160-direct-callees.json) 只列此 helper 和自调用；没有证明扩容的分配、重排、溢出、零容量、失败或递归终止行为。当前须在该调用边界 Unsupported，不能猜测 reserve 成功。

未来建议：对 `0x180284200` 分别请求 `procedure_pseudo_code`、`procedure_assembly`、`procedure_callees`，连同既有 open/session/close 检查共 **8 calls**；不写可执行 runner、不自动追查新 callee，本次不执行。上一查询的实际内存采样不等于下一目标的可靠峰值预测，少量请求仍可能触发整个 Engine 导入/分析；`-Xmx` 也不是全系统内存保证。UE 内存不足期间不启动、不后台重试，后续执行与资源监控另行确定。
