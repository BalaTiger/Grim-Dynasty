# 记录体字符串重映射验收

2026-10-20。已补齐 `0x180280870` 的定点静态证据，实现有界原地重映射，并通过显式映射上下文接入自有 ARZ 解码、解析和缓存链。最终 C#4 `/checked+` 回归通过 **1725 条断言、46 组场景、Demo 与 PEReader 检查**，成功运行耗时 **9.6961207 秒**。本轮新增 **82 条断言、5 组场景**。

## 证据与实现合同

PE runtime-function 边界为 `[0x180280870, 0x180280957)`，共 **231 字节、65 条指令**，一次已有 dumpbin 调用覆盖至 `ret`，无直接或间接调用、无必要续段或额外常量表。没有追入新 helper。详见 [静态证据](RECORD-REMAP-EVIDENCE.md)。

- `archive+170` 映射表指针为空时直接跳过；表存在而计数为零或负数时，仍对字段执行加法和写入。
- 每个字段先改写名称 ID；32 位加法回绕后，只在结果按有符号 int32 解释为非负且小于 `archive+178` 的有符号计数时查表。否则保留相加结果。查表得到的值不再加偏移、不再重映射。
- tag 2 的每个值按同一规则原地处理；tag 0、1、3 只跳过 `count*4` 字节，其值保持原样。其他 tag 仅前进八字节 header，不能假定未知类型的 payload 被跳过。
- 长度只参与循环末尾的无符号游标比较。函数没有逐字段边界校验，没有报告越界 ID 的错误分支，也没有可用的布尔返回合同。
- 写入有顺序且无回滚：后续值或映射单元不可访问时，先前字段、当前字段名称及先前值可能已经改写。

[RecordStringRemap.cs](../../src/resource_codec/RecordStringRemap.cs) 提供 `ApplyChecked(body, declaredLength, context)`，修改调用者独占的自有 scratch。`Completed` 仅表示有界模型走到遍历出口；`Unsupported` 表示自有覆盖域之外，不能解释为原生返回错误。结果记录游标、完整字段数和实际 uint32 写入次数，包括同值写入。

覆盖域限制为 body 最多 1 MiB、映射物理快照最多 65,536 项、稳定且不与 body 别名的映射存储，以及已知非负声明长度。按需检查实际读取，不提前回滚或拒绝所有后续字段。标量跳过可以越过声明长度甚至数组末端，因为该路径没有读取 payload；tag 2 的实际读取仍须落在提供的数组中。模型不复现访问违例、原生地址回绕、分配失败或异常 ABI。

## 显式接合

[ArzRecordRemapContext.cs](../../src/task_items/ArzRecordRemapContext.cs) 要求调用者提供已知映射和目标字符串池；它不生成全局映射、不去重、不推定多归档导入或 offset 向量选择规则。目标池保留传入顺序和重复字符串，采用现有 parser 的 4096 项、2 MiB 总量边界。

[ArzRecordBinarySource.cs](../../src/task_items/ArzRecordBinarySource.cs) 的新重载可以显式选择该上下文，旧重载默认不重映射。实际 cache miss 取得并解码 raw record 后，复制其 body 为私有 scratch，执行重映射，再把变换后的 body 和显式目标池交给已有 parser。原始文件、raw record、原始字符串池及记录名称 metadata 保持原样。

[RecordLoadFromSuppliedBinary.cs](../../src/task_items/RecordLoadFromSuppliedBinary.cs) 的 trace 保留 scratch、结果和中断不确定标记。检查停止后可读取已写前缀；不创建 parser/table，不发布缓存。`OwnRemapMayHaveWrites` 与原有 parser/cache/depot 的 `MayHaveEffects` 分开表示。`RunOnce` 保持粘性，失败后不会再次执行加法。最长成功路径使用 15/16 个阶段槽。

这不是完整原生 archive fetch。native scratch 所有权、归档初始化和合并、映射表生成、offset/base 选择及完整物品工厂仍未恢复；`NativeArchiveFetchRecovered` 和 `NativeArchiveRead` 仍为 false。

## 实际验证与修复记录

| 范围 | 断言 | 场景组 |
| --- | ---: | ---: |
| 27 套 task_items 模块，含新多归档接合 35/3 | 1508 | 40 |
| raw ARZ reader | 82 | 2 |
| 输出长度解码器 | 68 | 2 |
| 新记录体重映射核心 | 47 | 2 |
| Object ID | 20 | 0 |
| 总计 | 1725 | 46 |

[核心测试](../../src/resource_codec/RecordStringRemapTests.cs) 覆盖空表与无表的差异、signed count、加法回绕、越界 fallback、映射输出不再次处理、全部 tag 的游标行为、声明长度与可读数组范围的区别、延迟查表以及部分写入。

[接合测试](../../src/task_items/RecordLoadFromArzRemapTests.cs) 使用两份实际自制 ARZ 文件，源字符串池顺序不同且各有重复字符串。通过显式给定的十项映射及 base 0/5，两份 28 字节 body 都得到相同目标 ID；真实 Class getter 返回同一个目标字符串对象，关闭文件后缓存仍命中。两个物理文件各为 **131 字节**，读取前后哈希一致：

- `own-remap-a.arz`：`d4f83976b3ec85851d18460d9a2485f22da5553fec86accdcd92b360d137dd58`
- `own-remap-b.arz`：`678d1c2c78b42463d52ba943611a4b18db29ba3ae44c196cb4a5f7d9460f826c`

接合还覆盖映射存储不足和不完整字符串字段：前者保留已写 key，后者保留已写 key/value，后续字保持原样，未发布缓存。负数和超过映射计数的 ID 保留相加结果，后续既有 getter 对负字符串索引返回已知 null，不由 remapper 发明错误。

第一次构建停在新增测试辅助函数的参数类型不匹配：加载结果公开为 `object`，测试误写成 `RecordTableSnapshot`。只将测试辅助参数改为 `object`，匹配既有 `RecordTableGetter.GetValue`。失败日志与修复前测试源码保留在 [首次记录](record-remap-validation/test-summary.json)；随后完整重跑一次通过，见 [成功日志](record-remap-retry1-validation/test-output.txt) 和 [执行记录](record-remap-retry1-validation/test-summary.json)。总计两次构建尝试，其中一次完整通过，不能称首次全过。

## 范围与交接

边界评估和定点反汇编均确认原版及既有副本 SHA256 为 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`。231 字节范围不包括 64 KiB 分块流式全文件哈希或 PE 元数据读取；未测进程树内存峰值。

本轮没有读取官方归档样本，合成 fixture 不证明官方兼容。没有启动原游戏、Rea/Java、重跑监控、安装扩权、修改系统安全设置、访问玩家存档或 Steam 云，也没有 commit/push。

结构化验收见 [acceptance-record-remap.json](record-remap-retry1-validation/acceptance-record-remap.json)。新增量检查点引用前序 `native-decoder-complete` 检查点，恢复时须保留此前依赖链。当前没有新 helper 待批、被拒绝的操作或执行结果不确定的后台任务。
