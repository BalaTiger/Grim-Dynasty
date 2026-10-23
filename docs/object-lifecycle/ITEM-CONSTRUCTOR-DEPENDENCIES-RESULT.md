# Item / QuestItem 工厂的直接构造依赖

2026-10-21。沿用共享注册记录与动态查询检查点；本段不重复查询模型，也不把自有 callback 返回值视为原生对象构造成功。

## 先清点已有证据

QuestItem `classInfo+28 → Game 1066B0 → 5DA6C4(0xCD0) → 427C90 → 323690` 的主路径正文均已保存。分配器的正常路径和导入身份也已在后续注册辅助函数取证中确认，不需要重新读取原件。

- [QuestItem 工厂](offline-questitem-core/1066b0-instructions.json) 将分配返回的完整 RAX 直接传给构造 RCX，没有 null 检查，最后直接转发构造返回值。
- [分配器](offline-class-static-helper-storage-bodies/5da6c4-instructions.json) 只有 malloc 返回非零才正常返回。零则调用 `_callnewh(size)`；EAX 任何非零值均重试，零转入异常包装。异常类型、外部 handler 策略与 EH 清理仍未知。不能将失败改成正常 null 返回。
- [QuestItem 构造](offline-questitem-core/427c90-instructions.json) 保存原 this，调用 Item 后仍使用原 this 写派生字段，忽略 Item 返回的 RAX，最后返回原 this。未写的 `+CC0` 和其他字节不能补零。
- [Item 构造](../rewards/rea-inventory-add/Item-constructor-primary-table-assembly.json) 保存正常路径 `323690..323A86 RET`，但没有因此证明完整 unwind / EH。旧 [ItemScalarInitialization](../../src/task_items/ItemScalarInitialization.cs) 仅覆盖 `323714..323743` 六次写入，不能替代父构造成功。

Item 的既有汇编含 18 个调用点、10 个目标：

| Game RVA 调用点 | 目标与已观察传递 |
|---|---|
| `3236B3` | IAT `609700`，Actor 构造，原 this |
| `3236EF / 3236FC` | `0410E0(this+538 / this+6C8)` |
| `32370E` | `1FA340(this+858)` |
| `32380C` | `428690(this+950)` |
| `3238AB / 3238B8` | IAT `609720`，分别以 this+BA8 / this+BC0 调用 |
| `3238DA` | IAT `609980`，this+BE0，EDX=2 |
| `323968 / 323977` | IAT `609128 / 609520`，原 this |
| `32399D..323A2D`，7 次 | `016460(dst, src, length)` |
| `323A6B` | `324560(this)` |

`609700` 的精确导入名称 `Engine.dll!??0Actor@GAME@@QEAA@XZ` 已由 [导入绑定](offline-questitem-imports/import-bindings.json) 确认。其他四个 IAT 的旧索引分别标识 WorldVec3、HookPack、GetObjectId、GetObjectName；旧名字索引不是本轮新增函数体证据。另有 `323A49` 经 `[Game A66288]` 读取 `+1B58/+1B5C`，写 Item `+B68/+B6C`，不能省略成默认值。

## 本段新增的有界目标

只选择两条已知直接关系：Item ClassInfo `A5C8A0+28` 的 `104E00`，以及已存调用点 `323702 LEA RCX,[RDI+858]; 32370E CALL 1FA340`。不按 QuestItem 工厂猜 Item 的请求大小，也不把旧伪码省略的参数当成无参。

[新的边界记录](offline-item-factory-dependency-boundaries/metadata.json) 使用精确 `.pdata` 二分查询，确认：

| 入口 | unwind 覆盖范围，末端不含 | 字节 | unwind RVA |
|---|---|---:|---|
| Item factory | `104E00..104E2A` | 42 | `7F5EDC` |
| Item +858 helper | `1FA340..1FA366` | 38 | `7DEB10` |

此次边界查询读取 916 字节 PE 元数据，31 次有界 read 操作；没有读取代码或 unwind 内容，没有扫描整个异常表。Game 前后 SHA256 相同：`cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`。两次全文件流式指纹另计，每次使用 64 KiB 缓冲。精确 `.pdata` 只建立片段边界，不自动证明整个逻辑函数或异常清理。

## 新增正文合同

[两段正文](offline-item-factory-dependencies/inspection.json) 共 80 B、23 条指令，逐字节与原件切片一致：

1. Item 工厂 `104E0D` 明确写 `ECX=0xC68`（3176），`104E12` 调用 `5DA6C4`，`104E1C` 将完整返回 RAX 交给 RCX，`104E1F` 调 Item `323690`。无 null 检查、无返回地址调整，正常路径转发构造 RAX。这个请求大小来自新正文，不是按 QuestItem 的 `0xCD0` 猜测。
2. `1FA34A / 1FA350 / 1FA354` 依次清零子对象 `+0/+8/+10` 三个 qword。`1FA348` 已清零 EDX，RCX 仍是调用时的子对象；`1FA358` 随后调用 `029BF0`。该调用不能仅因参数为零而省略。

因此再选择且只选择 `029BF0` 这一直接依赖，单独固定请求、验证 `.pdata`，正文硬上限 512 B，实际范围 `029BF0..029C5B`，107 B、30 条指令。未继续追踪其中其他调用。见 [独立取证记录](offline-item-858-zero-helper/inspection.json) 和 [指令](offline-item-858-zero-helper/029bf0-instructions.json)。

在当前已证 RDX=0 路径中：

- `029BFA` 清零完整 RAX；`029BFF / 029C05 / 029C09` 再次清零三个 qword。
- `029C0D–029C10` 检查 RDX，直接跳到 `029C50`，不调用任何分配或异常 helper，正常返回时完整 RAX 仍是零。
- `1FA35D` 忽略该零返回值，改为返回保存的原子对象；Item 经 `323713` 的 NOP 后进入已实现的 `323714` 六次标量写入。

非零参数分支含阈值比较、导入调用与 `0162D0`，其一般语义、失败路径及返回 ABI 不属于新增模型。不能把整个 `029BF0` 命名为已恢复的 bool 接口。

## 最小自有实现

新增 [ItemConstructor858Prefix.cs](../../src/task_items/ItemConstructor858Prefix.cs)，独立入口对应 `323702..323743`。它使用可空 qword 保存 `+858/+860/+868`，完整执行上述固定零分支的效果，随后复用原 [ItemScalarInitialization](../../src/task_items/ItemScalarInitialization.cs)。已证覆盖为 **12 条原生对象 store 指令**：三个 qword 各清零两次，再执行六次标量写入；不同地址的总写入覆盖为 68 B。报告的指令数是所建模原生指令数，不是对 CLR JIT 实际存储指令的计量。

前提是执行已到达此局部入口、存储有效且独占；调用本接口本身不证明 Actor 和前面两个 `0410E0` 成功。状态是逻辑投影，不是原生内存。`Known` 只表示这个局部段完成，`IsCompleteConstructor` 恒为 false，不把结果接成完整 ClassInfo 工厂回调。

未知子对象在入口返回 Unsupported，不修改后续 scalar；未知 scalar 存储则在完成 helper 后停在 `323714`，保留三处已清零字段、helper 完成和返回身份。`+850`、`+888`、replica `+6B0` 等未写邻域保留原值或未知。这个缺失表示是自有覆盖边界，不是原生无效内存或 EH 行为。固定零路径没有额外 callback，也不制造一个可注入的原生失败点。

## 实际验证

经新的独立 `require_escalated` 审批，运行 [精确构建脚本](../../src/task_items/build_item_constructor_858_prefix.ps1)，一次构建与测试成功，退出码 0：**`PASS: 36 assertions, 6 scenario groups`**。

| 新场景组 | 断言 |
|---|---:|
| 未知存储与精确 continuation | 9 |
| 非零初值及原身份 alias | 5 |
| scalar 缺失保留 helper 前缀 | 6 |
| 子对象缺失不修改 scalar | 6 |
| 重复执行及不同对象隔离 | 6 |
| 一个新输入的旧 scalar API 稳定对照 | 4 |
| **合计** | **36** |

旧套件执行数为 **0**；旧 scalar 公开 API 仅在上述新对照输入中调用 **1 次**。不重复计算此前 18 条 scalar 断言或 71 条动态查询断言。原有 scalar 源码没有改动；新组合入口复用其内部 store 函数，避免 helper 已修改状态后再分配另一份结果报告。

[执行前清单](item-constructor-dependency-validation/preflight.json) 和 [运行记录](item-constructor-dependency-validation/run.json) 保存精确命令、正式审批结果、实际输出、边界与哈希。4 个构建输入和 7 个保全文件前后哈希相同；HEAD 和原有 12 项 tracked 改动的 numstat 未变。

产物位于 `.tooling/item-constructor-858-prefix-build-20261021/`：

- `GrimDynasty.ItemConstructor858Prefix.dll`，7680 B，SHA256 `a4f6c13558abef38870376d15d2477fde0ea00a5e4b8f0d2d4c521c61420ac9a`。
- `ItemConstructor858PrefixTests.exe`，16384 B，SHA256 `4d3a10dd9c249cee72888a18594690b3d86c803f6e17173c6feeb47afec80abb`。

[恢复检查点](../../.tooling/checkpoints/item-constructor-858-prefix-complete/manifest.json) 包含本段代码、fixture、构建产物、只读取证控制器及原始小片段、来源证据和上一检查点清单。

## 本段取证量与保全

真正新增正文 **3 段、187 B、53 条指令、3 次 dumpbin**。PE 元数据显式读取 **1120 B**（916 + 24 + 180）。另有 **6 次全文件 Game 指纹**，每次 64 KiB 流式缓冲；dumpbin 内部元数据 I/O 不包含在显式读取量内。两次正文控制器记录的峰值 RSS 分别为 43,208 KiB、42,592 KiB。

全部三次取证的 Game 前后哈希均为 `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`。两次正文批次的 dumpbin 前后哈希均为 `cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815`。Engine 本轮未读取，旧哈希仅作为归档证据保留；没有将它标为本轮新测。

## 未知边界

本轮尚未取得 Actor 构造的 Engine 精确入口和完整行为；`0410E0`、`428690`、字符串与全局服务也不能用空回调替代。factory 保存 allocation 到栈、写栈状态标记，不证明构造异常时已经释放。QuestItem factory 的已知 unwind 地址 `7F7CA4` 及 Item factory `7F5EDC` 尚未展开。完整 Item/QuestItem 构造、Read、运行时全图和正式样本兼容继续单独标为未验。

下一关键目标是仅解析已知精确导出 `Engine.dll!??0Actor@GAME@@QEAA@XZ`，确定其入口及必要边界；Game 导入 hint 14 不是 Engine RVA。当前阻断为证据和实现缺口，没有被拒、待批准或结果不明的执行动作。

原版只读；未运行原游戏、Rea 或 Java，未接触存档/Steam 云、安装工具、修改执行策略/ACL 或 commit/push。本次精确构建测试已获得独立正式审批，不代表后续执行自动获准。
