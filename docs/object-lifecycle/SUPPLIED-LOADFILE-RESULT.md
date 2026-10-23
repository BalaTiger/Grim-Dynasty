# 供应 Binary 记录后的有界 LoadFile 集成验收

2026-10-20。本轮把显式供应的已核验记录字节接入现有 LoadFile 包装层、真实有界 parser、节点构造、caller 链接写入、缓存索引与计数尾部。完成后，同一个 depot 的普通 `RecordLoadFile.Apply` 能命中新发布的同一 Table 对象。这里恢复的是明确输入契约下的组合行为，完整原生归档读取和通用 LoadFile miss 仍未完成。

统一串行轻量回归一次通过：**1400 条断言、31 组场景及 ItemQuestDemo**，耗时 **9.6341332 秒**。其中新增 caller 73 条断言、0 组场景，供应记录集成 67 条断言、4 组场景；此前 1260 条断言、27 组场景全部重跑。本轮未重跑 monitor 测试，也未执行原版对照程序。

## 实际接合与写入顺序

实现入口为 [RecordLoadFromSuppliedBinaryAttempt](../../src/task_items/RecordLoadFromSuppliedBinary.cs)，输入由 [SuppliedBinaryRecord](../../src/task_items/SuppliedBinaryRecord.cs) 表达。它复用 [RecordLoadFile](../../src/task_items/RecordLoadFile.cs) 的 depot 锁、完整名称复制、规范化与缓存查找；已有缓存命中不要求供应字节，也不进入 miss 流程。

| 阶段 | 已有静态依据与本轮接合 |
| --- | --- |
| 已确认记录输入 | 从 `1C4FB4..1C4FBF` 的有效 borrowed data/length、ownership byte 为零这一边界接入。字节与 archive 上下文必须由调用者明确供应；没有模拟实际 fetch 成功。 |
| 创建与解析 | reader 先建立，真实自有 Table 身份在 parser 前建立；运行现有 `13F510` 有界双遍解析，再在同一 table gate 内把解析结果安装回同一 Table。没有以预填 fields/values 代替解析。 |
| 临时名称 | 对应 `1C504E..1C5075`：从规范化名称复制完整 stored bytes，fresh capacity 为长度不超过 15 时的 15，否则为 `length \| 15`，随后放入同一 Table 引用。原请求名称不被移动或改写。 |
| 节点与 caller | [RecordCacheCallerPublicationAttempt](../../src/task_items/RecordCacheCallerPublicationAttempt.cs) 实际调用节点构造及名称 move；随后依次执行 `1C50B5` count 增加、`1C50B9` 原首节点 previous 写入、`1C50C1` 前节点 next 写入，再把同一组活链数组交给 `02E770` 索引与维护。 |
| 完成缓存 | 仅已完成 publication 才投影为完成快照并安装到同一 depot。新节点、临时 pair、完成缓存与成功返回值保留 Table 身份；不以 publication 的返回节点替换 caller 保存的 Table。 |
| miss 尾部 | 名称 move 已使临时源成为 capacity 15、length 0，正常路径跳过 `1C50E8` 后的临时 heap 释放。随后按 `1C50FC` 增加 `LoadCount50`，再执行转换后 tick 的 32 位差值、`+5C` 累加及 `+60` 无符号最大值更新。 |

caller 的 `1C4FF4` 虚调用之后没有检查 parser 返回的 AL/EAX。本实现中的 parser `Unsupported` 表示超出已证覆盖域，不能解释成原生 parser 返回 false 后会停止。成功域之外保留明确停点，不继续制造成功 Table。

## 正常成功域与输入所有权

`SuppliedBinaryRecord` 克隆完整规范化 key、记录字节和 shared-pool 数组；池内 `TableString` 为不可变对象，保留其身份。名称缓存及 name-ID context 保留显式共享身份，其使用仍要求同一 table gate 或独占访问，不能由构造器凭空生成。

- key 与包装层规范化结果按完整长度、逐字节严格绑定，包含内部 NUL；既不只比较哈希，也不截取 C-string 前缀。空 stored length 在 `1C4E90..1C4E95` 会走原生 fallback，本入口因此拒绝，不用供应数据绕过该分支。
- 包装层沿用显式 `KnownC` 正常域及原有高位字节限制；没有检测或默认原游戏运行时 locale。
- 解析沿用 fresh storage、完整窗口、2 至 15 个字段、tag 0/1/2、每字段正 count、总计不超过 65536 个 word、输入数组不超过 1 MiB 的域。tag 0/1 保留原始 word；未据此恢复其浮点或数值 getter 语义。其他 tag、零 count、截断及域外字段数仍是覆盖拒绝，不是对原生坏输入行为的结论。
- 缓存种子须为完整、有效且各桶连续的快照；容量和正有限 load factor 明确供应。沿用已有最多 4096 桶、4096 节点及 1 MiB key 总量等自有预算，以及有界单层增长规则。这些不是原生硬限制。
- 节点与名称操作以有效、非别名、fresh 目标和正常分配、复制、释放成功为前提。名称长度不超过 65536 字节；临时名称容量不会传播请求对象原先更大的 capacity。
- `ConvertedStartTick32`、`ConvertedEndTick32` 是各次原生计时转换完成后的低 32 位显式输入，不是原始时钟 API 返回值。本轮没有伪造时钟、推断频率或实现原生计时调用。

reader 持有自有克隆字节，退出时执行自有 Dispose，并保留 cursor/Consumed 诊断。原 caller 的 borrowed reader 正常路径 ownership byte 为零，不执行 native free；自有清理不能被称为原生析构、异常清理或回滚等价。

## 部分完成、拒绝与重复调用

普通 `RecordLoadFile.Apply` 在没有缓存命中时仍返回 `UnimplementedRecordCacheMissLoader`。供应入口缺少输入返回 `UnimplementedRecordArchiveFetch`；名称不匹配、空名 fallback、缺少 publication 配置或开始 tick，均在解析前停止。无副作用的 parser preflight 拒绝保留原 cache 与计数，不把 fresh 空 Table 当作解析成功。

解析、移名、count、邻链或桶端点写入后再失败，不回滚已发生状态，也不暴露旧完成快照为可用缓存。depot 保存 `PendingMiss`、`PendingPublication` 与诊断用 `LastCompletedCache`；pending 时 `Cache` 为 null，普通查找、CreatePartial 和 FinishPartial 均不能通过旧 cache 继续。后续结果继续携带 `RequiresResolution` 与累计 `MayHaveEffects`。

结束 tick 未知的停止点在缓存 publication 和 `LoadCount50` 增加之后：已完成 cache、Table 与计数保留，elapsed 尚未写入，depot 保持 pending。已知 tick 使用 unchecked uint 减法与累加，最大值比较为无符号；miss 不误增 `HitCount54`，也不伪造 fallback 的 `+58` 计数。

每个 attempt 只执行一次；重复调用复用既有结果，不重新解析、移动源名称、链接或增计数。实现另在 wrapper 结果分配前记录命中、锁及查找状态，在 parser 尚未返回报告时保留 in-flight 副作用不确定性，避免把报告分配失败误记成无修改。这两处自有异常记录修正已独立静态复核；本轮没有宣称实际触发了 OOM，也没有由这些修正推导原生 EH 行为。

## 本轮验收与资源记录

[完整输出](supplied-loadfile-validation/test-output.txt) 与 [执行摘要](supplied-loadfile-validation/test-summary.json) 记录从 `2026-10-20T08:00:42.4021313Z` 至 `08:00:52.0483967Z` 的同一次成功运行。使用现有 Framework64 `csc.exe`、C#4 与 `/checked+`，产物位于 `.tooling/supplied-loadfile-build`。

| 范围 | 断言 | 场景 |
| --- | ---: | ---: |
| 此前全部行为回归，含独立 ObjectId 20 条 | 1260 | 27 |
| 新增 caller 构造、count、邻链与 publication | 73 | 0 |
| 新增供应记录 LoadFile 集成 | 67 | 4 |
| 合计，另有 ItemQuestDemo 通过 | **1400** | **31** |

新增 4 组场景分别覆盖真实 mixed 字节解析后首次 miss、普通二次 hit 与另一 key 的 miss；增长停点及旧 cache 防护；结束 tick 未知时的已完成前缀；无符号计时回绕和高位最大值。其他新增断言覆盖输入克隆、完整名称绑定、空名与无供应拒绝、parser 覆盖拒绝、Table/共享池身份、源 move、节点与桶边界及重复调用。

新 wrapper 的 [程序集依赖检查](supplied-loadfile-validation/assembly-references.json) 也在本次运行中通过，使用 `PEReader/GetMetadataReader` 只读自有程序集元数据，没有 `Assembly.Load`。引用仅为自有模块、mscorlib 与 System.Core，没有 Engine、Game 或 Steam 依赖。上一阶段在行为测试后失败的 ReflectionOnly wrapper、随后 metadata 恢复及 monitor 原始记录保持历史原样；本轮成功不能改写它们。

[资源观察](supplied-loadfile-validation/resource-observation.json) 在 `08:01:54.2274078Z` 记录分析进程数为 0，`analysis-runtime/rea` 与 `.tooling/rea-runtime` 文件数均为 0。它是固定范围的结束时观察，没有测量本轮进程树峰值，不能据此声称整个运行期间的硬内存上界。本轮未重跑 monitor、启动 Rea/Java/dumpbin、读取原版或工作副本、安装新增软件、扩大权限、修改系统安全设置、访问存档/Steam 云或 commit/push；执行限于自有编译、测试及既有运行时的元数据读取。

## 证据与封存边界

本轮复用既有工程归档，没有新增原版函数读取。主要依据为 [LoadFile 包装层 `1C4D20`](rea-object-base/11-TableDepot-LoadFile-0x1801c4d20-assembly.json)、[miss caller `1C4E20`](rea-record-load-dependencies/11-LoadFile-miss-0x1801c4e20-assembly.json)、[完整名称赋值 `023180`](rea-object-name-assignment/05-Object-name-assignment-0x180023180-assembly.json)、[正常容量增长 `022F70`](rea-object-name-growth/05-Object-name-growth-0x180022f70-assembly.json) 与 [释放 helper `023090`](rea-container-reserve/08-Container-reserve-0x180023090-assembly.json)。

parser 依据 [mixed parser 证据](BINARY-MIXED-PARSER-EVIDENCE.md)；节点和 memmove 绑定依据 [上一阶段节点结果](OFFLINE-CACHE-NODE-RESULT.md) 及 [固定 import 记录](offline-cache-node-inspection/move-import-binding.json)。publication 依据已存 [05DEB0](rea-record-cache-publication/05-Cache-publication-0x18005deb0-assembly.json)、[02E770](rea-record-cache-publication/08-Cache-publication-0x18002e770-assembly.json)、[02EA80](rea-record-cache-maintenance/05-Cache-helper-0x18002ea80-assembly.json) 与 [02EB40](rea-record-cache-maintenance/08-Cache-helper-0x18002eb40-assembly.json)。

相关原版为 `D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll`。上轮 [extraction 记录](offline-cache-node-inspection/extraction.json) 的前后 SHA256 均为：

`4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`

这是既存原版指纹，本轮没有重新读取原 DLL 或其副本计算该值。此前 289 字节有限反汇编属于上一阶段，不能计为本轮新增分析。

本轮前检查点为 `.tooling/checkpoints/supplied-loadfile-before/manifest.json`，时间 `07:45:02.402Z`，保存 53 个文件，记录此前 `.tooling/checkpoints/offline-cache-node-complete/manifest.json` 的 376 项原文件与副本均已核验。before manifest 的 SHA256 为 `3c36359a0fda978e603cc12fa887096e4f6d5729fe293a4f8602079f78d24dd0`。

本轮总账与独立记录的固定入口为 [acceptance-supplied-loadfile.json](supplied-loadfile-validation/acceptance-supplied-loadfile.json)、[boundary-evidence.json](supplied-loadfile-validation/boundary-evidence.json)、[preservation-audit.json](supplied-loadfile-validation/preservation-audit.json) 和 [review-record.json](supplied-loadfile-validation/review-record.json)。完成检查点路径为 `.tooling/checkpoints/supplied-loadfile-complete/manifest.json`，由主线程在最终文档冻结后封存；副本范围和完成状态以该 manifest 为准。

实际原生 archive fetch（包括 `280E20`）、完整 Binary/base 构造语义、fallback 诊断、计时 API、allocator/OOM/EH、原生 ClassInfo population、具体物品工厂、真实 Read 与完整对象注册仍不由本轮恢复。注入有效字节后的自有代码实测及静态调用顺序核对，不能等同于一次原版动态对照或完整游戏行为等价验收。
