# 官方单记录缓存链：池预检完成，初始化边界停止

2026-10-20。本轮尚未完成 `scrapmetal.dbr` 的实际 ARZ → parser → 缓存首次发布闭环。已完成真实字符串池唯一性检查、现有调用链审查和内存核算；按“无法证明初始化则停”的要求，没有以空上下文、预热名称节点、零 tick 或自有时钟替代未知原生状态。

上一轮 **1971 条断言 / 57 组场景**及官方单记录 **38 字段**解析对照结果保持有效，本轮没有重跑。新增的检查器自检为独立 **47 条断言 / 8 组场景**，不并入产品回归总数。

## 本次实际只读结果

固定原档为 `D:/Steam/steamapps/common/Grim Dawn/database/database.arz`，58,529,720 B。
同一只读文件句柄前后以 64 KiB 缓冲完成流式 SHA256：

`9e3532250cdb611a4cc989997c8f97fcec2d6c422063a845e6c43cd538c30603`

语义读取范围仅为 24 B header、字符串 count 的 4 B 和偏移 54,551,066 开始的 3,978,638 B 完整字符串表。另有两次全文件指纹读取，经过所有原档字节，但不解析记录表、不提取或解压任何记录。没有写入或向工程复制原档/字符串表。

| 项目 | 实测结果 |
| --- | ---: |
| 原始字符串数量 / 唯一值数量 | 82,999 / 82,999 |
| 完整字节重复组 / 多余项 | 0 / 0 |
| 字符串 payload | 3,646,638 B |
| 空串 | 1 项，原 ID 82687 |
| 最大长度 | 259 B，原 ID 60364 |
| ID 13 / ID 28355 的池内容 | `Class` / `QuestItem` |

比较不做 Unicode 解码、大小写折叠或 NUL 截断。只排序独立的 ID 排列数组，wire、offsets、lengths 始终保持原 ID。表 SHA256 为
`657ecaace92c1f1b9863f04776d504167fcebceeaba15db660e18af2d5e13af0`。

因此，**在已知 fresh、正常成功的精确字节 initial-import 合同下**，此次池的去重不会改变原始 ID，后处理也不需补空串。这个结论不证明 archive 对象已初始化，不证明 `A+170/+178` 的 map 状态；没有执行原生 initial-import，也没有绕过自有 importer 的 65,536 项预算。两个指定池值只作字节核对，不重新推断记录的键值关系。

[真实池报告](official-cache-chain-pool-preflight/official-pool-report.json)；
[检查器自检](official-cache-chain-pool-preflight/selftest.json)。

## 当前阻塞按层区分

1. **直接的实现预算。** `ArzRecordBinarySource`、`SuppliedBinaryRecord`、`BinaryWarmState` 仍要求完整 `TableString[]`，限制 4096 项 / 2 MiB payload；真实池超出两项限制。raw reader 在显式 8 MiB 读取预算内处理同一归档元数据的成功证据已保存，但这不放宽后续投影预算。
2. **初始重复 ID。** 本次已排除该真实池的重复项风险。`ArchiveStringImportState.CreateRemapContext` 对未保持原 ID 的初始导入仍应拒绝，不新增修复映射。
3. **archive 初始化。** 已有 `283A80` / `283A00` / `284080` 证据不能证明任意 archive 的 map 指针为空，也未闭合 cache shift 的来源、完整构造和 offset 选择。对 82999 输入，pointer-cache helper 的参数为 request=20749、capacity=41499；正常准确 `log` 合同推得 16384 buckets，但不能由此补造 shift。现有 4096 bucket/entry 覆盖也不能直接容纳该完整缓存。
4. **接口合同与实际执行依赖。** `DeserializeFresh`、`InstallParsed` 和缓存发布不查询 `Names` / `NameIds`；然而当前 source / supplied API 强制它们为已知完整输入。没有用空索引、一个预热 `Class` 节点或 49 项引用子集代替完整上下文。缓存表目前只投影到 warm getter 所需状态，也不能因此声称已持有所有原生 table 行为。
5. **完整 miss 真正使用的未知输入。** `RecordDepotState` 的 cache、bucket capacity、load factor 和统计初值仍是显式输入。缺转换后 start tick，`RecordLoadFromSuppliedBinaryAttempt` 会在读取前停止；缺 end tick，则会在发布和 load count 增加后停止，标记 unresolved，阻止后续普通命中。没有把零或本机自有时钟数值当作已恢复的原生转换结果。

源代码及已有证据：

- [ARZ source](../../src/task_items/ArzRecordBinarySource.cs)、[supplied record](../../src/task_items/SuppliedBinaryRecord.cs)、[完整 miss 与失败前缀](../../src/task_items/RecordLoadFromSuppliedBinary.cs)。
- [warm pool / pointer cache](../../src/task_items/BinaryWarmTable.cs)、[名称索引](../../src/task_items/BinaryNameIdSnapshot.cs)、[depot 状态](../../src/task_items/RecordLoadFile.cs)。
- [initial import 与 map 边界](ARCHIVE-STRING-MAP-EVIDENCE.md)、[已保存的 284080 汇编](rea-binary-parser-helpers/09-Parser-helper-0x180284080-assembly.json)。

## 内存核算及按需方案

以下区分实际检查器内存、现有代码按真实文件尺寸计算的数组计费，以及未实现的方案。均不是完整链的实测峰值。

| 口径 | 字节 |
| --- | ---: |
| 本次检查器：wire + offsets + lengths + sorted IDs | 4,974,626 |
| 上项加 64 KiB hash buffer、header 和 count | 5,040,190（4.81 MiB） |
| 本次 Node 报告 maxRSS | 51,320 KiB（50.12 MiB） |
| 当前 raw reader 元数据累计数组 payload：`T + 8N + R + 28` | 5,985,283 |
| 当前 raw reader 同一记录完整计费：再加 name 39、class 9、packed 323、两份 body 456 | 5,986,566 |
| raw 返回对象的保留数组 payload | 4,643,134 |
| 假设仅放宽常量，沿用当前投影/克隆链的累计数组 payload 下界（8 B 引用） | 15,937,217 |

最后一项含两次完整字符串 payload 复制、四份池引用数组和额外正文副本，尚不含字符串对象/数组头、对齐、完整名称索引、parser 和 publication；它不是同时存活大小或进程峰值。实际 CLR/JIT/GC、线程栈、容器 capacity 还须单独测量。原有 8 MiB 是单次 raw reader 的累计数组计费上限，不能作为整个加载链总内存上限。

可行的低内存设计候选是保留完整 3,978,638 B wire 与全部 offset/length 索引，总基础 payload **4,642,630 B**，按原始 ID 延迟物化稳定的 `TableString`，并为已物化条目另设预算。完整 Count 仍是 82999；未物化、预算不足或读取失败不能伪装成越界 null/default。这个方案尚未实现，因为它不能解决上述初始化前提。

若把字符串 payload 本身改为延迟文件 I/O，单是 offset/length 索引为 663,992 B，但还必须解决共享源的存活、稳定性、跨表同步，以及源关闭后 getter 的语义。保留完整不可变 wire 的方案更贴近当前 detached 契约。两者都不是把原池裁剪成49项的新小库。

## 四项正式测试的状态

| 请求项 | 本轮状态 |
| --- | --- |
| 首次实际读取 → parser → 缓存发布 | 未执行；完整 source/context 与初始化前提未满足 |
| 同一 depot 的重复命中 | 未执行；不能预置缓存节点冒充真实首次发布 |
| 源关闭后命中 | 未执行；须先有同一真实首发完成状态 |
| 正式链失败状态 | 未执行新的正式链尝试；仅核对现有代码和既有自有 fixture 的失败前缀，不把它计为新的官方测试 |

上一轮官方 38 字段 parser 成功和既有自有 ARZ 的首发/命中测试，均不升级为本轮请求的官方端到端通过。当前停点是证据和输入合同缺失，**不是权限拒绝或等待批准**；没有结果不明的原版操作。

## 保全与下一步

生产源码、生产限制、现有回归记录保持不变；修改前80个工程文件已备份，上一检查点248份当前文件及副本已核对。HEAD 仍为 `6dfdae419d1c92f7738eefe241937057d83134df`。

下一步应先补齐实际路径所需的 archive/map 状态、depot 初始化和 tick 转换合同，再决定完整 pool provider 的适配及正式缓存测试；不能把支持未知上下文的新模式偷偷当作原生初始化完成。

本轮未读取 Engine.dll，未新增原生反汇编；未启动游戏/Rea/Java，未安装扩权、访问存档/Steam 云或 commit/push。

[机器状态与内存账](official-cache-chain-pool-preflight/readiness.json)；
[保全审计](official-cache-chain-pool-preflight/preservation-audit.json)；
[预检封存](../../.tooling/checkpoints/official-cache-chain-preflight-complete/manifest.json)。
