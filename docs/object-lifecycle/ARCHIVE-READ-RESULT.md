# 有界只读 ARZ → LoadFile 接合验收

日期：2026-10-20。一次串行构建与回归通过 **1549 条断言、37 组场景及 ItemQuestDemo**；耗时 **9.4303796 秒**。新增 149 条断言、6 组场景。测试使用本项目生成的完整 ARZv3 小归档，证明独立格式读取器接入既有解析与缓存链；不证明官方归档或原生 archive loader 兼容。

## 已实现

- [ArzRawRecordReader.cs](../../src/resource_codec/ArzRawRecordReader.cs) 从借用的可定位只读流读取 ARZv3 头、字符串池和记录表，核验全部元数据范围，只读取所选记录的压缩 payload，并复用现有 LZ4 block codec。返回脱离输入流的原始 body、原始字符串字节和未重编号的字符串 ID；重复及空字符串池项保留。
- [ArzRecordBinarySource.cs](../../src/task_items/ArzRecordBinarySource.cs) 将上述原始数据投影为既有 supplied record 输入。调用者仍须明确提供 gate、name-ID / pointer-cache 上下文和已转换的时钟值。没有把 `ReadRecord` 字典或文本 DBR 反向编码为“原始记录”。
- [RecordLoadFromSuppliedBinary.cs](../../src/task_items/RecordLoadFromSuppliedBinary.cs) 新增 `FromArzRecord` 入口。仅真实 cache miss 才读取来源；随后执行实际 bounded mixed parser、同一 Table、节点和 caller 链接、索引发布、depot 提交及末尾统计。成功后关闭来源流，普通 `LoadFile` 命中仍可工作。
- 读取或投影失败记录尝试、已取得的快照及失败阶段；parser/publication 部分效果和 pending 防护沿用现有实现。`RunOnce` 重放不重复读取。来源流位置可能改变，此副作用单独记录，不混入 parser/cache 的 `MayHaveEffects`。

名称按 wrapper 的已知 C locale 规则处理，再与归档中完整 stored-name 字节精确匹配；不额外去掉 NUL、做 Unicode 大小写折叠或合并同名记录。原有 `ResourceContainers.cs`、`Lz4Block.cs` 保持原字节。

## 实际测试

[统一日志](archive-read-validation/test-output.txt) 与 [执行记录](archive-read-validation/test-summary.json)：

| 范围 | 断言 | 场景组 |
| --- | ---: | ---: |
| 25 套 task_items 模块测试 | 1447 | 35 |
| 新增 raw ARZ reader | 82 | 2 |
| 独立 Object ID 回归 | 20 | 0 |
| 合计 | 1549 | 37 |

模块中的新归档接合测试为 67 断言 / 4 场景。覆盖真实文件读取前后 SHA256、原始字段位模式和字符串 ID、只访问选中 payload、关闭输入后的 cache hit、精确名称与 NUL、坏 LZ4、元数据及预算提前拒绝、parser 拒绝、发布中断和末尾时钟缺失。测试通过并不把拒绝策略解释成原生 malformed-input 或 OOM/EH 行为。

自有样本：`.tooling/archive-read-build/fixtures/own-two-field.arz`，139 字节，1 条记录、2 个字段；30 字节 literal block 解为 28 字节 body。SHA256：

`8d7277fd58eece5313f44718b943bf02c3fb7cbac61019e376e2736a1bb83757`

[PEReader 元数据检查](archive-read-validation/assembly-references.json) 通过，仅引用 .NET 与自有模块，包括本次重新编译的 Resources；未加载原版程序集。

## 官方样本来源澄清

此前“官方样本字段 / 字符串池超预算”是对历史材料的判断，**不是本轮实际读取官方样本的结果**。准确口径如下：

| 历史样本 | 旧报告字段数 | 旧报告压缩块 → raw body 字节 |
| --- | ---: | ---: |
| Soldier mastery | 644 | 5070 → 9708 |
| `b000_sword.dbr` | 663 | 3517 → 7956 |
| `scrapmetal.dbr` | 38 | 323 → 456 |

字段数直接来自 [旧 resource-reader 验收](../resource-reader/acceptance.json)，另由 [旧 compiled-containers 验收](../compiled-containers/acceptance.json) 的官方 DBR 比较复核；字节数和 body 哈希来自 [旧 LZ4 验收](../resource-contracts/compiled-lz4-acceptance.json)。这三个字段数超过当前 parser 的 15 字段覆盖上限，但本轮未重读、重跑这些样本。

旧 JSON 没有直接记录准确的字符串池项数。**34,180 是完整 base 库的记录数，11,956 是英文 tag 数**。结合 [旧读取实现](../../tools/resource_reader/gd_resources.py) 中“各记录名从同一字符串池索引取得，并拒绝重复记录名”，可以推得保留该完整库原始池时，池至少有 34,180 项，超过当前 4096 项预算。这是下限推论，不是池总数实测，也不代表每个样本实际引用超过 4096 项。

[旧命令及输出清单](../resource-reader/official-commands.json) 最后明确记录了旧机 `D:\MyGitRepo\Grim-Dynasty\.tooling\resource-reader-validation\` 下的 `mastery.dbr`、`sword.dbr`、`scrap.dbr`。它们是导出文本，不能直接当作二进制 parser 的 raw body。旧 [accept_lz4.py](../../tools/resource_reader/accept_lz4.py) 的输出路径及样本次序则指向旧项目 `.tooling/resource-reader-build/fixtures/0.lz4block`、`1.lz4block`、`2.lz4block`；这是压缩块，未找到独立解压 raw body 文件的保存位置记录。

本轮只将旧项目根映射为 `C:\Users\zhuzi\Documents\Grim-Dynasty`，核对上述六个精确路径，结果均为**该路径未找到**。另 `.tooling/resource-containers-build/fixtures` 未找到，它是合成 fixture 目录。没有全盘扫描、没有检查旧机路径，也不能由此推断文件被删除、其他位置不存在或本机 `database.arz` 缺失。完整来源和本机存在性检查见 [sample-provenance.json](archive-read-validation/sample-provenance.json)。

## 覆盖边界

接合入口保持既有 parser 覆盖域：2–15 个字段、tag 0/1/2、body 最多 1 MiB、池最多 4096 项 / 2 MiB、单字符串最多 64 KiB。raw reader 的 8 MiB 配置上限只限制所计入的读取工作数组字节，不是接合全流程或进程树的内存峰值；CLR 开销、投影和导出副本另计。来源需在读取期间保持稳定，由调用者负责关闭。

本轮针对两个已知逻辑函数做了 6 次固定范围 dumpbin，核验 800 字节 / 196 条指令；没有追踪未知 callee body。`.pdata` 中的短条目是函数片段，后续仅沿已见 fallthrough / 分支补齐。详见 [原生取数证据](ARCHIVE-FETCH-EVIDENCE.md)。

已确认 `280E20` 的 ID 查询、scratch 输出及 remap 条件，以及 `280D20` 的容量 / 调用次序；**Engine `278D20` 解码器、`280870` 字符串重映射、归档初始化与跨归档合并仍未恢复**。已有 ArchiveTool 的 LZ4 符号不是 Engine 解码器绑定证据。普通缺少输入来源的 miss 仍返回 `UnimplementedRecordCacheMissLoader`。完整 native ctor/fallback、factory、derived Read、时钟及异常 ABI、完整对象工厂仍未闭合。

## 文件与执行范围

六份静态检查都验证原版及既有副本 SHA256 为：

`4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`

这些检查包含固定文件的流式全文件哈希；800 字节只指反汇编代码量，不是总读取字节。此后测试只执行自有程序集。未启动原游戏、Rea/Java；未重跑监控测试，未安装、扩权或修改安全设置，未访问玩家存档 / Steam 云，未 commit/push。

验收清单见 [acceptance-archive-read.json](archive-read-validation/acceptance-archive-read.json)。本轮增量检查点引用既有 487 文件基线，保留原历史修改和证据；下一步仍须分别解决原生 decoder/remap 与官方格式样本覆盖，不能从合成样本通过推导原生兼容。
