# ItemGameEngineFloatLoad 窗口独立静审

2026-10-21。审查最终 `src/task_items/ItemGameEngineFloatLoad.cs`，**16574 B，SHA256 `db5753b07e35a128632252263bc450601c66d8e9142dce8bb02ffdd6c5e0ffa0`**。先全文审阅 16572 B / `d87b8d42d921bc598324ea13b8be763d33e4c5be3772c3d63a1ddb715b847ea7`，再核作者唯一括号修正：`first+((ulong)ByteCount-1)` 避免有效末地址等于 ulong.MaxValue 时，checked 模式下中间先加12再减1溢出。修正未改变 API 或正常派发顺序。

**结论：所声明的单 owner、稳定 own getter 域内，未发现阻断问题。** 本次只有工程归档和源码阅读；未修改生产源码、读取原版、运行构建或测试。静审结论不是运行通过，也不证明完整 LoadFromDatabase、GameEngine 构造或 Item 构造。

核对依据：Game caller `offline-item-global-load-body/instructions.json` SHA `cee579cfa26e049167595ac55792993201e6268bd459474da6689f648e382262`；inputs `offline-item-global-load-inputs/inspection.json` SHA `75046b8f1e04fff6d60c07a175ffdfa20cbb24218aa8ff424d39894190b9f1d0`。真实 own getter 源码 SHA `2289b851867fd604b2d1c35aa4676f2f41bb4c7148bbc5274a339a1192330aa7`，另见已冻结 [model-review.md](model-review.md)。原位字段类来自未修改 `ItemConstructorReturn.cs` SHA `2a23081b4c995b4bc73c46827d6b8ef3a0b82ffd5cd994f8f1376472a1936feb`。

## 四步顺序与读取时点

`DispatchFirst → CompleteFirst → DispatchSecond → CompleteSecond` 由 step 严格限制，每一步进入操作前先消费。未知/不支持令 step=-1；不能在同一 invocation 上恢复、跳步或重放。不存在“之前整个 2C8750 前缀已成功”的虚构证明参数，入口明确仅从本地 `2CEC27` 窗口开始。

各次 Dispatch 都按 `outer vptr → 当次默认 DWORD → key 地址设置 → outer +48 slot` 的顺序推进。第一调用的原始位置为 `2CEC27/2A/37/41`，第二调用为 `2CEC4D/50/58/62`。缺少默认位在 native 实际读取默认的位置停止，即使预计 getter 可能不用默认值也不跳过该读取。默认值及 query 逻辑地址在这次 Dispatch 捕获；第二次 default 在第二次 Dispatch 才采样，不在窗口入口预读。

outer slot 使用刚读出的 vptr/backing，只有已观察目标 `13FD50` 被接通。Complete 再独立读取同一 table 对象当前 vptr，读取其 `+98` slot，只有已观察目标 `1402E0` 被接通。因此两调用共有四次实际 vptr 观察点；outer 与 inner 之间更换 vptr/backing 可见，不能沿用进入窗口时的一份 vtable。null、unknown、失效 backing、Token 不符与不支持目标保持区分。

Complete 调用真正的 `BinaryFloatTableGetter.GetValue`，即 `13FD50` 提供 index0 的链。没有调用者回调直接供应成功 float 返回值，也没有用 string getter 冒充 numeric。`BinaryTable`、`ParsedRecord` 和已捕获 query 地址对应的当前 bytes 在这一步读取；替换 inputs 中的 key 对象不倒改已经捕获的该次 key 地址，修改该 key 观察的 bytes 则能影响尚未执行的 getter。这些可变观察仍受同一逻辑地址存储一致性的显式前提约束。

## 原位写入、失败与身份

成功 getter 后分别在 `2CEC44`、`2CEC65` 向同一 `ItemGameEngineFields` 的 relative0、relative4 做 DWORD Store。原 GameEngine Token 和同一字段对象引用保留；没有复制成新字段对象或发布新 global slot。relative8..11（原 +1B60 部分）不写；未扩大到后续 `2CEC81`。MOVSS 结果按 uint 原位保存，没有 NaN/负零归一化或算术变换。

首次完成后才允许第二次 Dispatch。第二次 default、vptr、slot、名称解析或 numeric 不足时，首次 DWORD 及已经发生的名称缓存效果仍保留。即使第一/第二 getter 成功，目标 State 在实际 Store 时未知/失效也会停止，而不是倒退已完成 getter；`Getter` 子结果保留缓存 effect。表、默认和未到达的第二 key 不被预先假定为已加载正式状态。

`MayHaveEffects` 聚合已完成 caller stores 及两个 getter 报告的名称缓存 effects。对逃出的托管分配异常，没有伪造 native EH 或原子回滚承诺；每步预先消费阻止重试复制既成效果。固定 managed Gate 的正常 acquire/release 及异常 finally 属于 getter 的显式 own 合同，不能据此声称实际原生锁对象或线程行为已恢复。

## 别名与边界

新增重叠检查比较 GameEngine 的12个已观察字段字节与 table 前缀 D0 B、vtable 前缀 A0 B，以及已观察 query 字节范围。它们明确是 **own 逻辑视图的一致性/覆盖守卫**，不是 native 新分支或真实内存扫描。守卫可能在尚未实际 dereference query 的 absent-archive 场景也拒绝已知重叠；这不能被描述为 native query读取或失败。

Token 构造上界保证 table/vtable 已知前缀和 GameEngine 字段地址可表示。最后地址的括号修正也避免合法最高地址的 checked 中间溢出；重叠判断不计算可能 wrap 的 query end。未知 query 长度只可检测已知起点所在范围，不能证明未知其余范围不重叠。没有地址投影的 Binary cache/parsed/allocator 存储仍需明确有效且不别名；所有地址相同的 logical views 必须互相一致。模型未声称这些有界检查已经证明完整 native 非别名。

`WithArchivedInputs` 只在显式调用时提供 `itemSparkleMinTime\0` / `itemSparkleMaxTime\0` 及 raw defaults `40400000` / `40A00000`，与固定 inputs 报告一致；两个 logical query 地址仍由调用者提供。文件常量不是已加载值不变的保证，Token 也不是执行原版指针。

最终 `ReachedWindowEnd` 仅表示到 `2CEC6E`；三个完整构造/加载属性恒 false。此前 record acquisition、fallback 创建及类型、真实 ObjectManager/LoadTable 生命周期、Base 文本解析、后续全局发布和完整游戏行为均未通过这个局部窗口获得证明。
