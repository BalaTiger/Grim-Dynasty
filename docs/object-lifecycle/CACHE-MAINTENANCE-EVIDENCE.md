# 记录缓存维护：正常完成域与尚未恢复的节点创建

日期：2026-10-20。本文件记录静态证据、覆盖前提及推导，不是实现验收报告。本文编写只读取工程归档，没有运行 Rea、Ghidra、编译器或原游戏，没有重新读取原版文件。

新证据补齐了 `02E770` 所调用的 `02EA80` 和 `02EB40`。在显式、完整、稳定的**候选已经链入并计数**入口上，可以重建不增长的索引完成、一次有界增长，以及重复候选的正常释放。此前 [CACHE-PUBLICATION-EVIDENCE.md](CACHE-PUBLICATION-EVIDENCE.md) 保存的是这两个 helper 尚未知时的停止点；保留该历史文档，不将当时的前缀验收追认成完整成功。

这个范围仍不执行 `05DEB0`，不恢复它依赖的 `01F170/022860`，也不意味着 `LoadFile` 冷加载、完整物品工厂或真实派生 `Read` 已连通。

## 1. 查询完成与失败批次必须分开

### 维护批次正常完成

[rea-record-cache-maintenance/summary.json](rea-record-cache-maintenance/summary.json) 记录：

- 开始 `2026-10-20T06:33:12.816Z`，结束 `2026-10-20T06:34:42.849Z`。
- 固定计划 SHA256：`d4f2231584dd5ed268cdddb3eb0ef55b3aa3743840fac669bf187b7712a6dac8`。
- 11 次调用全部完成并通过；关闭确认、最终关闭会话验证、进程退出确认均成功，未强制结束，退出码为 0，错误列表为空。
- 固定目标 `D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll`；归档中的查询前后 SHA256 均为 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`。

[resource-release.json](rea-record-cache-maintenance/resource-release.json) 于 `2026-10-20T06:35:06.9661813Z` 确认：54 个已记录进程身份均退出，没有 PID 重用条目，剩余分析进程和 runtime 文件均为零。54 是本批累计观察的身份数，不是同时运行的进程数。

### 节点创建批次未取得函数体

[rea-record-cache-node/summary.json](rea-record-cache-node/summary.json) 的状态为 `failed`。11 项计划中完成 3 项，尝试了第 4 项 `01F170` 伪码查询；因此准确说法是 **3/11 完成、4 次请求已发出**，不是完成了 4 项，也不是取得了部分函数体。

[progress.ndjson](rea-record-cache-node/progress.ndjson) 和 [采样器 stderr](rea-record-cache-node/owned-memory-sampler-stderr.txt) 记录第四项等待期间采样器 `QueryMembers` 遇到 `QueryFullProcessImageName` 异常，随后失去正常监控并结束自有后端。本批没有 04–09 函数结果文件；`01F170` 的 body 未取得，后续 `022860` 请求未执行。`close_acknowledged=false`、`closed_session_verified=false`、`forced_termination=true`；即使记录的后端退出码为 0，也不能称正常关闭或查询成功。

独立的事后 [resource-release.json](rea-record-cache-node/resource-release.json) 在 `2026-10-20T06:38:40.9199487Z` 确认：22 个记录身份均退出、剩余分析进程为零；已确认的临时目录清理了 49 个文件，最终 runtime 文件为零。这个释放证明没有补齐函数证据，也没有改变失败批次的状态。节点批次归档的 Engine 前后 SHA256 同样保持上述固定值。

| 状态证据 | 文件 SHA256 |
| --- | --- |
| 维护 summary | `aa99fcfbff1b170d874bd942aa65dcf964344b3d6d9493e06563a6270eaa8ea1` |
| 维护 resource-release | `02fd59519fb6023d4f0b0c816f65c5e9747a46dabdf24ba6dd3930281a72e43a` |
| 节点 summary | `83abb26e8271f471d7f25749d0acab5cb780840b0cc59e04dfa797ec4d601a5e` |
| 节点 resource-release | `306846a7bb9a4effd9882f18567c95b081336f037928031f4ee1f18731268116` |

## 2. 实际函数体与可复核标识

以下文件均以对应地址作为 `parameters.procedure`，`operation=procedure_assembly`。不是仅从 caller 提及函数名推断其实现。

| 函数 / 汇编 | evidence_id | 文件 SHA256 |
| --- | --- | --- |
| [02EA80](rea-record-cache-maintenance/05-Cache-helper-0x18002ea80-assembly.json) | `ev_094b103338b6f278032d13b390421c0b9d479229117ae0d5d3d1f6b9bff63c30` | `ccb9240a89f420481fd7ea4cff2ab29f443a2a216fe8b8f0b819a3c62a2ba0b1` |
| [02EB40](rea-record-cache-maintenance/08-Cache-helper-0x18002eb40-assembly.json) | `ev_4b3e6ebcee849558624d15a53c05a1984376660be2ed05602ebc44b9d18f1134` | `7ffff90e46dcd2dba145120736aac5b31be195a46f6328b3af4aebc5d8f00b65` |
| [01B4F0](rea-registration-containers/11-Container-whole-chain-followup-0x18001b4f0-assembly.json) | `ev_ce058af65993a657ec2262c46d9dddb99a0da13719d96c19f5ee9f5e76bd1073` | `1529d7985f5678abfcb62119eceb01f23758eef0bcf53ed51487961f6f31d644` |
| [01BFB0](rea-container-support/11-Container-support-0x18001bfb0-assembly.json) | `ev_aa12f8d8ad38b503a5e8729002814d3842754a43951d6db1b170193521d8ea1a` | `e245bbb88f5ac7801c6203c62a05d90adee8f59bb2b5f891621ff8301fcb1de0` |
| [01BD30](rea-container-support/08-Container-support-0x18001bd30-assembly.json) | `ev_f1ffa31fde2b903ddaf309c4eed43312a4f025d76eb1a619b5219df48ea7781f` | `88f4814ff458a3bed6d446d822b77159db2db968b819f111b6b3d957b80f9549` |
| [021740](rea-container-fill/05-Container-fill-0x180021740-assembly.json) | `ev_807dfd5d098dc270b3fafa79077d832ec3981f3d892c251bcffa73ee46fa1c79` | `113d652d2b71152724b1ad33b9c12b753cc354888ad2dc58e373745c3aa68829` |
| [01EF70](rea-container-fill/08-Container-fill-0x18001ef70-assembly.json) | `ev_48344c09ba5257c7e6304e5e7ea211b367faeaeb9002c8335dadc108c2b9655f` | `d3fb698bbf1700687cfb11e3e52892e245a98b71c8ce42e27cac7e4a26250567` |
| [022F00](rea-container-reserve/05-Container-reserve-0x180022f00-assembly.json) | `ev_d798754fa6332fd4aae9fd10191ff688f96ccd71d70babeb8119351a5e605aaf` | `b77108a28c7f3488b15c2dd81d208eb31d67fa29a7b21f8b8033acb79fb3ed7b` |
| [023090](rea-container-reserve/08-Container-reserve-0x180023090-assembly.json) | `ev_6e043e558b0c46b7323ec3eeadec3f847e8f52d343f64388976bfce89eff3a26` | `722c381af2bea6298e9e7b7c4515194cb73805e01de8142bb88b1e2a7e37905e` |

[06：02EA80 直接 callees](rea-record-cache-maintenance/06-Cache-helper-0x18002ea80-direct-callees.json) 为 `01B4F0`、`02E770`；[09：02EB40 直接 callees](rea-record-cache-maintenance/09-Cache-helper-0x18002eb40-direct-callees.json) 为 `023090`、`2DC390`。因此复用旧 `01B4F0` 等证据来自同一实际函数地址，不是将 raw uint32 特化代码类推成字符串键实现。

## 3. 浮点阈值：严格大于才增长

`02EA80` 读容器 `+0x10` 的 qword 节点数、`+0x38` 的 qword 逻辑桶数及 `+0` 的 float 最大负载因子。

`0x18002EA9C–0x18002EAB7` 把计数和桶数转换为单精度；机器码还包含高位为 1 时的修正路径。`2EABB` 使用 `DIVSS`，`2EABF` 使用 `COMISS`，`2EAC2 JBE` 跳到返回处。对有限正常输入，这意味着：

```text
single(count) / single(bucketCount) <= maxLoadFactor  => 不增长
single(count) / single(bucketCount) >  maxLoadFactor  => 进入增长路径
```

相等不增长。这里不能改用整数除法、双精度阈值或大于等于。NaN、非正因子、零桶数、超大 qword 以及浮点异常环境不属于下述最小模型域；排除它们是自有覆盖边界，不是原版输入检查的恢复。

例如节点数不超过 4096、桶数为不超过 4096 的二次幂时，两个整数及比值均能精确表示为 binary32；这允许用可验证的有界状态避免猜测高位转换分支或舍入环境。最大负载因子和实际桶容量仍须显式提供，不能从节点数量猜测，也不能默认原生初始化值。

## 4. 增长倍率、reserve 和清空桶端点

`2EAC4–2EAE6`：

- 旧逻辑桶数小于 `0x200` 时乘 8。
- 否则，旧值小于 `0x0FFFFFFFFFFFFFFF` 时乘 2。
- 更大的值不乘 2；这个原生分支存在，但不纳入小规模模型。

因此受限域内的 `256 → 2048` 和 `512 → 1024` 均是实际规则，不能统一为翻倍。`2EAE9` 把目标逻辑桶数交给 `01B4F0`。

`01B4F0` 的真实链路如下：

1. `1B501` 计算 `2 * targetBuckets` 个 qword 端点。`1B513–1B516` 比较现有容量；不足时 `1B53C` 调用 `01BFB0`。这里需要独立的容量元数据，逻辑桶数不能冒充容量。
2. `01BFB0` 在 `1BFD2` 调用 `022F00(targetSlots, 8)`，按目标数量精确 reserve；`1BFE4 → 021740` 复制旧 `[begin,end)`，不是复制所有未使用容量。非空旧存储在 `1C00D → 023090` 按旧 capacity 释放；`1C01B/1C023/1C027` 随后发布新 capacity、end、begin。这个 reserve 不采用 1.5 倍增长。
3. 无论是否 reserve，`1B55A` 先把 end 设为 begin，`1B56C` 调用 `01BD30` 填入 `2 * targetBuckets` 个 sentinel。
4. 此调用域已经保证容量足够且 end=begin，所以 `01BD30` 走 `1BE6D` 分支，在 `1BEA2 → 021740` 处理空范围，再由 `1BEC2 → 01EF70` 填 sentinel。不会进入它另一条独立扩容分支。
5. `1B57A/1B57E` 最后写 mask=`targetBuckets-1` 和新的逻辑桶数。节点链本身没有因此重新分配。

`022F00` 和 `023090` 继续采用已保存的正常分配/释放合同。字节数达到 4096 时，分配包装额外保留 39 字节并对齐到 32 字节，原始 base 存于 aligned pointer−8；释放包装验证对齐、base 在前且差值在 8..39，再调用底层释放入口。小规模模型可以表达可观察元数据和成功结果，不生成原生地址，也不恢复 OOM、无效隐藏头、错误处理或分配器内部。

## 5. 重排必须使用保存的旧尾与当前表头

清空桶端点后：

- `2EAF2` 判断全局链是否为空。
- `2EB01` 保存此时 `sentinel.Previous` 的旧尾身份，整个循环不更换这个终止身份。
- `2EB10/2EB1C` 每轮重新取当前 `sentinel.Next`，`2EB26` 用它调用 `02E770`。
- `2EB2B/2EB2E` 在本轮调用返回后比较本轮节点与保存的旧尾身份，相等即结束。

不能预先保存 next 再遍历，也不能按旧桶号或新桶号遍历。`02E770` 会把处理后的唯一节点移到新桶 first 之前，新桶为空时移到全局链尾；重复候选则被删除。已处理节点组成索引区间，尚未处理节点仍由当前表头取出。每个节点的键按完整 stored bytes 做 FNV64 和比较，不能换成 raw uint32 哈希。

如果旧尾节点在最后一次处理中恰好是重复键，`02EB40` 释放它后仍以保存的身份比较并结束。这里的机器码没有再解引用该已释放节点；模型也不能在删除之后读取它的 next 以寻找下一项。

重排中被保留的是先被本循环索引的相等键节点；其表身份保持。未重复的同桶节点前插，可能改变全局链和桶内顺序。不能用字典覆盖 payload 代替，也不能把旧 lookup 的前向搜索结果直接当作重排去重结果。

### 一次有界增长中嵌套维护为 no-op 的证明

每次 `02E770` 的唯一键路径仍会调用 `02EA80`；重复键路径删除候选并直接返回，不调用它。允许一次增长的充分前提是：

1. 目标桶数、存储容量和节点数在明确预算内，所有计数/指针运算属于上述有效域。
2. 在开始 reserve/reset 之前，按**当前完整计数**证明 `single(count) / single(targetBuckets) <= maxLoadFactor`。
3. 重排循环不增加计数：唯一键路径不改变 count，重复键路径只减一。

因此每次嵌套维护的比值都不大于预检值，均落在 `2EAC2` 的返回分支。这里省去的是已有证明的无副作用递归调用，不是用成功 callback 跳过未知 helper。

不能先假设之后会删除几个重复键，再用较小计数通过预检；首个唯一节点的嵌套维护可能发生在那些删除之前。若目标预算不满足或仍需第二层增长，可在自有覆盖边界停止，但此前 `02E770` 已完成的链表/桶端点变化必须保留，不能声称回滚或成功。重复键路径本身不进入增长维护，不能因为可能的增长预算而提前拒绝一个本来只需删除候选的分支。

## 6. 重复节点删除与正常释放合同

`02EB40` 的已证顺序：

| 指令点 | 行为 |
| --- | --- |
| `0x18002EB58` | `candidate.Previous.Next = candidate.Next` |
| `2EB62` | `candidate.Next.Previous = candidate.Previous` |
| `2EB66` | 容器 `+0x10` 的 qword count 减一 |
| `2EB6E–2EB80` | 若字符串 capacity ≥16，调用 `023090(buffer, capacity+1, 1)` |
| `2EB85 / 2EB92 / 2EBA2` | capacity=15、stored length=0、内联首字节=0 |
| `2EBA5` | 调用底层 `2DC390(candidate)`，随后返回 |

本函数不写 bucket.first/last，也不读取 node `+0x30` 的表 payload，没有表对象虚调用或析构。因为候选尚未被当前桶索引，摘除它不应修改那些只指向已处理幸存节点的端点。正常返回后，外层 `02E770` 才在 `2E881/2E884` 写已有匹配节点和 `inserted=false`。

现有 `RecordCacheEntry` 只有完整字节键和 opaque Table。**键长度不能推出原生 capacity**：短键也可能保留堆容量。最小抽象可明确假设所有节点键都有有效 owned 存储并且释放正常完成，只记录逻辑节点移除，不猜测是否执行额外字符串堆释放、分配尺寸或释放次数。若以后要暴露这些细节，必须增加明确存储元数据；不能从长度制造它们。

对于实际走堆释放的分支，`capacity+1` 的有效性和大块隐藏头等约束仍由正常释放合同保证。`2DC390` 内部没有恢复；旧 [free-thunk 证据](registration-container-validation/free-thunk-0x1802dc390.json) 是 `E9… / not_ff25_stopped`，没有证明某个 CRT 导入身份。伪码中的 `free` 标签不能扩大这个结论。

模型中的 tombstone、移出有效节点集合或断开托管引用只表示“节点存储已逻辑释放、不得再次作为有效节点使用”。它们不是原生 free 后字节内容的重建；汇编没有给候选自身 next/previous 清零。保存的诊断快照或不可变键字节仍可用于报告，但不等于可再解引用原生地址。opaque Table 对象身份应保留，不调用 `Dispose`、delete callback 或虚构析构。

## 7. 新覆盖域的入口和调用端限制

正常完成域仍从显式入口开始：有效 sentinel 双链、完整稳定桶区间、明确 mask/逻辑桶数/容量/负载因子，候选位于表头、已计数且尚未被桶索引，完整键及 opaque Table 已由调用方提供。正常分配、释放和可读键存储是条件合同，来源没有冒充 `05DEB0` 的实际执行。

完成这里的 `02E770` 可以有真实的 `(node, inserted)` 结果；这个局部 Known 只属于该 helper。它不能自动提升为整个 `LoadFile` 或 CreatePartial/FinishPartial 已成功，更不能把候选 Table 直接送入工厂来绕过未知冷加载。真实 `RecordLoadFile` 的 miss 在其前置路径未闭合时仍应停止。

调用端必须区分入口已经发生的计数/链入、本次维护产生的修改，以及 Unsupported 后没有新增修改的重复尝试。覆盖预算或自有异常导致停止时，要保留首次停止信息和已经修改的状态；不得把后一次“未新增修改”当作前一次已回滚。

节点创建所需 `01F170` 与 `022860` 本批没有 body。维护批次的成功与节点批次事后清理完成都没有消除这个缺口。本文不提出或启动额外查询，不新增测试计数，也不宣称 `RecordCachePublicationAttempt` 或其他新实现已经编译、通过测试或验收。
