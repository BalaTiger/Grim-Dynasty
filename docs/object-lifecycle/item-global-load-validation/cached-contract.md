# GameEngine 两字段数据库加载：已有证据与下一边界

本记录只读工程缓存和相关自有源码；没有读取原版、运行reader/构建/测试或扩大为全引擎检索。它为下一次固定读取提供来源，不预先断言2C8750的算法、字段key或getter类型。本轮没有重复上一阶段对三个历史目录的完整否定搜索。

## 已有直接来源

上一阶段冻结的 `item-return-validation/cached-global-contract.md` 已记录 `subscriber-constructor-routes.json.functions[4]` 的GameEngine构造路径。相关保存assembly为：2C18CA取临时字符串到RDX，2C18CE将原this RDI送入RCX，**2C18D1直接CALL2C8750**；2C18D7..2C18DB随后销毁该临时字符串。伪代码同位置称为LoadFromDatabase；直接target及this参数来自assembly，不依靠方法名猜测。

该历史JSON没有逐指令raw bytes。本轮之前的固定inputs报告只核了2C07C6、2C0D23、2C1B8B和36B25F四处34B锚点，**不包括2C18D1**。若要对新目标的直接调用来源加强原始字节绑定，应明确另取该CALL及必要参数指令，不把四锚点当作整个构造体已核。

先前两条已核初始化store为DWORD this+1B58=40400000、QWORD this+1B5C=0000000040A00000；第二条同时覆盖+1B60..63。它们位于2C8750之前，不能作为其返回后的默认值。当前任务应优先查加载函数如何写这两字段，而非把构造常量注入Item返回模型。

## 可直接复用的Game PE/pdata元数据

`offline-item-factory-dependency-boundaries/metadata.json` 保存固定Game SHA对应的raw DOS/COFF/optional/section headers，原件前后hash/stat稳定且关闭成功：

| 字段 | 值 |
| --- | --- |
| Game SHA256 | cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2 |
| 文件长度 | 11385856 |
| PE file offset / image base | 272 / 180000000 |
| exception RVA / file offset | A6A000 / 10896384 |
| exception bytes / entry count | 267516 / 22293 |
| .text RVA / raw offset | 1000 / 400 |
| 2C8750的文件映射 | 2C7B50；这只是地址映射，不是函数长度。 |

此报告有27个已读pdata项，位于`assessments[].probes`；可复用其raw bytes与index，不必重复取metadata。`offline-item-return-dependency/inspection.json.encountered_pdata`另保存15个已遇见项（其中有复用项），也可按index去重合并。上述报告均未给出2C8750的exact entry。前者已有index8360的205810..205839和index11146的3015F0..30162A，可作为排序前提下的已证二分夹界；这不授权读取两端或其间的代码。

下一阶段仍需对**固定2C8750**解析精确pdata项。如果没有exact start，只记录containing/gap与已遇见邻项，不能把整个间隙或邻近函数作为正文。精确pdata本身也可能只界定一个异常处理片段，不证明全部逻辑控制流已完整。

本轮只检查了必要的 `module-boundaries.json` 相关索引。它的分类exportExamples每类只保留40项，未列该LoadFromDatabase目标；因此没有可直接引用的精确导出条目，也不能据此断言函数未导出。优先使用已经明确的直接CALL目标，不因名字缺失而扩大符号扫描。

## 现有数据库模型能复用什么

现有 `RecordTableGetter.cs` 的公共结果是`TableString`：Base路径对完整、唯一、按unsigned-byte排序的key视图查找；Binary路径进入已有name-to-ID/cache以及字符串值逻辑。当前源码支持已实现的Binary分支，因此早期CLASS-GETTER文档中“present尚未实现”的旧阶段说明不是当前源码能力边界。

但它**不是浮点getter**。`CLASS-GETTER-EVIDENCE.md`中“numeric getter”指Engine13FC80的**数字ID查询**：tag2选择字符串池index，其返回仍是字符串指针/默认值。`BinaryWarmTable.cs`实际也在type tag!=2时选默认字符串。不能将其改名或套在float调用上。

`BinaryRecordParser.cs`已经保存tag0/1/2原始DWORD，`CopyRawValues(typeTag)`可输出其位值，`CopyDescriptors()`保留key、TypeTag、BufferIndex、ElementCount。源码明确不赋予tag0/1数字或float语义。这些原始buffer/字段身份可作为以后浮点读取模型的候选基础，但**不证明浮点tag、默认规则、索引边界、类型转换或数组选择**。`ToWarmState`/`CopyValues()`当前专门取tag2，不是通用数值读取接口。

`record-interface-vslots.json`只核过LoadTable/LoadTableBinary的+28字符串槽以及Object+8；没有核float虚槽。旧相关getter缓存可在新正文实际命中相同helper后复用，但不能根据“LoadTable”名字就认定float的调用槽、锁顺序或fallback与字符串getter相同。

## 下一证据优先级

1. 固定2C8750边界及有界正文：首先定位对this+1B58/+1B5C的真实store、前后分支与直接调用，确认返回宽度、默认实参、字段顺序及中途失败前缀。不要先假定两个调用均为float getter。
2. 只对上述writer实际引用的常量key、直接callee或vtable slot取得最小绑定。若走虚调用，须区分已知表类型与未知动态vptr；不能单靠静态基类槽代替实际派发。
3. 如实际证明复用name-to-ID、字段桶或原始buffer，使用已保存对应合同及数据；若float类型/默认/索引语义尚未证，保留未知。不得把数字ID字符串getter当浮点getter，也不通过CLR字符串解析制造原生算法。
4. 与现有ItemReturn连接时继续使用同一own字段观察，按实际load阶段逐写保留前缀。LoadFromDatabase后仍可能有其它callee/Initialize修改字段；本段成功不能自动证明完整GameEngine返回状态、运行时全局发布或正式样本值。

## 来源SHA256

| 相对路径 | SHA256 |
| --- | --- |
| docs/object-lifecycle/item-return-validation/cached-global-contract.md | 52a4d181902a881e3baae0e1f33a3748833d8d54b8696fedc60e37bf27efdb37 |
| docs/task-kills/ghidra-selected/subscriber-constructor-routes.json | e907b67d57a568784a19f887a3c71883b0f7726a14defee449cf8693422ee165 |
| docs/object-lifecycle/offline-item-return-inputs/inspection.json | 288a5ae6cc72a4a2e68b7c1f03f14476a2f1e5a1a9794fe2f9bdb710a09cba4f |
| docs/object-lifecycle/offline-item-factory-dependency-boundaries/metadata.json | 59265ca3af388f48af8da9a66bee439949fa8cfeca5fe1cc40a066998ad46d29 |
| docs/object-lifecycle/offline-item-return-dependency/inspection.json | fc0f85739966bf84241cc277b6c4d2ae7129a9daa27792ed1245e9cb7b409ff0 |
| docs/phase-1/boundary-audit/module-boundaries.json | d1ec79916baeb45b3257f7c7c2ce356c262f804ae9b2577888a0d14de53e7ba7 |
| docs/object-lifecycle/record-interface-discovery/record-interface-exports.json | 33d7527e64565cc4e56a31b3665b41e004ad9e53f93739e3a645f205cf0b1603 |
| docs/object-lifecycle/record-interface-discovery/record-interface-vslots.json | d2b77d66e08c05c83822f9f03d66565e18d016cdca400377853c866c7c3a4b9c |
| docs/object-lifecycle/CLASS-GETTER-EVIDENCE.md | f97e8c031e5f33e4108f5d3749f4c6b8c6b677d3744339bd2733c84411daa41b |
| src/task_items/RecordTableGetter.cs | 856da47a74f9bfee9236aa403ee20e4fac7885616ff9c07e79b6a5f38ac17ecb |
| src/task_items/BinaryRecordParser.cs | a1c8da1974de4d4749644e3e63681fda017ea3271c3d715537c37cb0f89d9ac9 |
| src/task_items/BinaryWarmTable.cs | bd202839399b3388203dc060dfaa48a43c0264130cdd1fc284f195c8413d8328 |
