# 输出长度解码器静态核对与显式接口验收

2026-10-20。本轮确认 `0x180278D20` 是按输出长度结束的 **LZ4 fast 类算法**，并实现有明确安全覆盖域的独立接口，接入自有 ARZ 读取链。一次 C#4 `/checked+` 回归通过 **1643 条断言、41 组场景、Demo 与 PEReader 检查**，耗时 **9.5489027 秒**；新增 94 条断言、4 组场景。

这不是原生函数执行结果，也不构成官方归档兼容证明。没有推定具体上游版本、导出符号或把地址绑定到现有 `Lz4Block.Decode`。

## 证据与实际差异

先做 PE 元数据体积评估，入口片段只有 136 字节；随后仅沿入口已见分支补齐 500 字节，两个定点 dumpbin 共 **636 字节 / 174 条指令**。另读取两个直接引用的只读数据表，共 32 字节。复用同一 Engine 哈希下已有的 `memcpy` 导入绑定，没有跟入其他函数。详细指令、复制表、错误和写入次序见 [NATIVE-DECODER-EVIDENCE.md](NATIVE-DECODER-EVIDENCE.md)。

| 合同 | 原生静态证据 | 既有 `Lz4Block.Decode` |
| --- | --- | --- |
| 参数 | `src`、`dst`、有符号 int32 输出长度；没有 packed 长度 | 完整输入数组、输出长度 |
| 结束与返回 | 最后 literal 恰好填满输出，返回已消耗输入量，可留下尾随输入 | 按输入数组耗尽循环，最后校验输出长度，返回新数组 |
| 零输出 | 仍读首字节；零返回 1，非零返回 −1 | 允许空输入 / 零输出 |
| match 尾部 | `end−12` 选复制路径；`matchEnd > end−5` 拒绝 | 不要求相同尾部余量，旧 match-ending 用例成功 |
| 显式长度错误 | 有界正常域内返回 `−当前输入游标−1` | 抛 managed 格式异常 |
| 无效距离 / 内存 | 无零距离和历史越界检查，错误返回前可能已经写入输出 | 显式检查距离及输入范围，失败不返回私有输出 |

原生零距离还存在正常返回的可推导路径，不能说它必然拒绝。其 wildcopy 会读写超过逻辑 literal/match 末端的字节；这些物理写入、访问故障和异常 ABI 没有由本次安全接口模拟。

## 实现

[Lz4OutputSizeProfile.cs](../../src/resource_codec/Lz4OutputSizeProfile.cs) 提供 `DecodeChecked(source, expected)`：

- `Success` 返回消耗量及独立输出快照；尾随输入可保留。
- `NativeLengthRejected` 只用于三个已证分支，保留准确游标及负返回码。
- `Unsupported` 表示本实现的安全或预算边界，返回码为 null，不能解释为原生函数同样拒绝。

覆盖域为输入最多 2 MiB、输出最多 1 MiB、稳定无别名输入、有效正距离且历史完全在已产出内容中。非末尾 literal 还检查至少 8 字节、按 8 字节向上取整的 wildcopy 源范围；逻辑复制使用安全 managed 操作。失败不暴露本实现的私有输出，**不代表原生 scratch 无副作用或会回滚**。分配及运行时异常仍不承诺原生等价。

[ArzRawRecordReader.cs](../../src/resource_codec/ArzRawRecordReader.cs) 新增显式 `ArzRawDecodeProfile.ObservedEngineOutputSize` 选择，记录 `DecodeProfile` 和 `PackedBytesConsumed`。[ArzRecordBinarySource.cs](../../src/task_items/ArzRecordBinarySource.cs) 将其传给已有真实 cache-miss 链。旧重载仍选 `BoundedInput`；旧 `Lz4Block.cs` 与 `ResourceContainers.cs` 的原始字节未改。

新 profile 返回对象持有一个输出数组，导出给 raw record 再复制一个，因此 raw-read 工作预算计入两份 body。这里的预算不是 CLR、接合全过程或进程树的内存峰值。

## 实际验收

[日志](native-decoder-validation/test-output.txt) 与 [执行记录](native-decoder-validation/test-summary.json)：

| 测试范围 | 断言 | 场景组 |
| --- | ---: | ---: |
| 26 套 task_items 模块（含新增接合 26 / 2） | 1473 | 37 |
| raw ARZ reader 回归 | 82 | 2 |
| 新输出长度 profile | 68 | 2 |
| Object ID 回归 | 20 | 0 |
| 总计 | 1643 | 41 |

profile 验证覆盖零输出、literal / match 长度扩展、距离 1–9 及 256/257、重叠、零 literal 序列、`end−8/−5/−12` 边界、精确错误游标、尾随数据与独立输出。旧解码器的 match-ending 成功用例保留，并明确展示它与新接口的合同不同。

接合测试的一个场景是真实自制 ARZ 文件 → 解码 → mixed parser → 同一 Table → 实际节点/索引发布 → cache hit；另一个场景验证失败隔离与预算，不能称为两个成功加载场景。文件读取前后 SHA256 一致，关闭来源后普通 `LoadFile` 命中通过，重放不再读取或重复统计。

自有 `.tooling/native-decoder-build/fixtures/own-output-profile.arz` 为 **135 字节**：packed=31、body=28，profile 成功消耗 **30** 字节，尾随 `AA` 未消费；默认输入长度解码器仍拒绝该额外不完整 offset。SHA256：

`efa27a69ad7202981a9629a66f7953cb26bb96a52af947d5a9c730c1ba7d0cd1`

本轮重建的 `own-two-field.arz` 仍为139字节，哈希与上一轮一致。[PEReader 检查](native-decoder-validation/assembly-references.json) 通过，程序集只引用 framework 和自有模块，没有 Engine/Game/Steam 引用。

## 原生 archive 与样本边界

完整 archive fetch 仍未恢复：新 profile 不包含 archive 初始化、scratch 生命周期、`280870` 字符串重映射、多归档合并、完整 ctor/fallback/factory/Read 或异常 ABI。`NativeArchiveFetchRecovered` 和 trace 的 `NativeArchiveRead` 仍为 false；无显式来源的 miss 仍然拒绝。

官方样本证据沿用上一轮 [来源澄清](archive-read-validation/sample-provenance.json)：644/663/38 字段来自旧报告；34,180 是记录数，完整库字符串池至少同等规模是下限推论。本轮没有读取官方样本、查找旧机或扩大扫描范围，不能从本轮合成 fixture 推出官方兼容。

四份静态记录均确认原版及已有副本 SHA256 为 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`。代码/表字节数不包括流式全文件哈希及 PE 元数据读取。未启动原游戏、Rea/Java，未重跑监控，未安装扩权、修改安全设置、触碰存档/云或 commit/push。

本轮清单见 [acceptance-native-decoder.json](native-decoder-validation/acceptance-native-decoder.json)，增量检查点引用上一轮117文件基线。`280870` 的后续结论另记于 [remap-readiness.json](native-decoder-validation/remap-readiness.json)；当前没有把未知 remap 行为补成成功。
