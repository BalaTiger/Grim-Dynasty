# 记录缓存发布：静态证据与明确停止点

日期：2026-10-20。本文只记录保存的静态证据及可重建范围，不是本轮实现或测试验收报告。

`0x18002E770` 的查重、已链入节点重排和桶端点写入已有完整函数体。它的两条正常结果路径仍分别依赖未知的 `0x18002EA80` 和 `0x18002EB40`；即使称为“无增长”场景，也不能跳过这些调用并返回成功。当前可选择的实现范围是：显式提供已经链入的有效入口快照，执行已证前缀，在未知调用之前返回 `Unsupported` 并保留实际发生的前缀副作用。

## 1. 本批证据来源与结束状态

原始证据目录：[rea-record-cache-publication](rea-record-cache-publication/)。

[summary.json](rea-record-cache-publication/summary.json) 记录：

- 开始 `2026-10-20T06:17:19.668Z`，结束 `2026-10-20T06:18:48.380Z`。
- 固定计划共 11 次工具调用，完成 11 次，各条记录均为 `passed`；其中两个函数各有伪码、汇编、直接 callee 三项，另有初始会话、打开、打开后会话、关闭、最终会话五项。
- `close_acknowledged`、`closed_session_verified`、`process_exit_confirmed` 均为 `true`；`forced_termination=false`，`child_exit_code=0`，`errors=[]`。
- `targets_launched=false`。这是 Rea/Ghidra 静态分析，没有运行原游戏。
- 固定目标为 `D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll`；归档记录的前后 SHA256 均为 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`。
- 归档计划 SHA256 为 `b8edfd6904516091befee8c69d6111080b5c7f58ad342b6a69d378f5589eb762`。本次文档审阅只读取工程归档，没有重新读取原版或启动后端。

[resource-release.json](rea-record-cache-publication/resource-release.json) 在 `2026-10-20T06:19:02.8620588Z` 记录 66 个已记录进程身份均退出，`remaining_same_identity_count=0`、`active_analysis_process_count=0`、`remaining_runtime_files=0`，没有 PID 重用条目。不要把 66 个历史身份误写为同时驻留的 66 个进程。

证据的 `operation`、`parameters.procedure` 与下面函数地址相符。伪码只是反编译表示，具体读写顺序以汇编为准。

| 文件 | evidence_id | 文件 SHA256 |
| --- | --- | --- |
| [04：05DEB0 伪码](rea-record-cache-publication/04-Cache-publication-0x18005deb0-pseudocode.json) | `ev_4227801715c2d6fde95bc2c52936feb8906d26fb170c37e5ad9f4881d50ce438` | `2a886efe29266b71b696a16f1f707b4477dfd1a590ce94c3fdf972a65dd029b0` |
| [05：05DEB0 汇编](rea-record-cache-publication/05-Cache-publication-0x18005deb0-assembly.json) | `ev_58fd83bd301795b7c4f186903d19d345ff9ded2395479a15634d4d1fb9cf5dab` | `a4fe68eb62328f6715c70fe810645e60d6045720d8ad94ae11a1379260d917c9` |
| [06：05DEB0 直接 callees](rea-record-cache-publication/06-Cache-publication-0x18005deb0-direct-callees.json) | `ev_048dbe925ddb8fbaf7188bfc73577a1c1f3c17433c96eddfab3cbf5ae8a4b1ce` | `f2f6f385e085bbe548a16c2d3c4141a26ffe717e12f67b87bfb3dea2f89e5aa7` |
| [07：02E770 伪码](rea-record-cache-publication/07-Cache-publication-0x18002e770-pseudocode.json) | `ev_6f61925cf899cf0f2c60ce1df51e9480a1b4012e09ec538a07cb2cd3ad2c0eff` | `43f00f5da5f46ea7917e501add20768a111f4b43ffcfd720ff5420a8b6e9c8c3` |
| [08：02E770 汇编](rea-record-cache-publication/08-Cache-publication-0x18002e770-assembly.json) | `ev_25cbb65bb78e322192f30486715a8be46ab329e9ff271fe008e5c5c6a0a2c3fd` | `398e58ec9e5fc35f198d6f679242e2201028e026552f75c99a08a6f041dcf867` |
| [09：02E770 直接 callees](rea-record-cache-publication/09-Cache-publication-0x18002e770-direct-callees.json) | `ev_c1ae635a7ba43ccdeef97b445d4e73df389d88c5ac24374b0c154cb1d8604001` | `0ec12ee5bc842b6fbc388dac071f03b6696a348405fa842826b9f962fdecbcf1` |

## 2. 调用方先产生的状态不能丢失

旧 [1C4E20 汇编](rea-record-load-dependencies/11-LoadFile-miss-0x1801c4e20-assembly.json)（文件 SHA256 `d696e7b2141e5fe9ee6f518555d32a615ab174deabd6f676a15c299be1b7e3b1`）给出真实调用顺序：

1. `0x1801C504E–0x1801C5070` 建立临时字符串并调用 `023180` 复制记录名称；`1C5075` 把新表指针放入临时键值对象。
2. `1C5079–1C508B` 取 sentinel 的第一个节点，将 next/previous 参数和临时键值对象传给 `05DEB0`。`05DEB0` 的依赖仍未闭合，不能凭这些参数声称原生分配、节点链接初始化和字符串复制已恢复。
3. `1C5093–1C50AE` 是节点创建之后的最大数量检查。正常路径在 `1C50B5` 先增加 depot `+0x18` 的计数，再在 `1C50B9` 和 `1C50C1` 将新节点接入全局双链表头部。
4. `1C50C4–1C50DA` 以 depot `+8` 为容器，传入新节点、它的 `+0x10` 键以及结果缓冲区，调用 `02E770`。因此 `02E770` 接收的是已经链入的候选节点，不是 detached node。
5. 只有 `02E770` 返回后，`1C50E0–1C50F7` 才按临时字符串容量释放其堆存储；`1C50FC` 增加 depot `+0x50` 的加载计数，再更新耗时字段。
6. `1C514D` 返回保存在 RDI 的新表指针，没有读取 `02E770` 的结果 pair。重复键情况下的缓存内表身份、被创建表的生命期和 caller 返回值不能直接合并为“字典覆盖成功”。

新的入口快照只能显式代表上述调用点已经存在的状态，不能声称它执行了尚未恢复的 `05DEB0`、冷加载、原生构造或解析链。停在 `02E770` 内部时，已有计数增加和头部链入不能回滚成“完全无副作用”；临时字符串释放、后续计数与 caller 返回尚未发生，也不能伪造。

## 3. `05DEB0` 已证写入与两个未知依赖

直接 callees 恰为 `01F170`、`022860`。

| 指令点 | 确认的行为 |
| --- | --- |
| `0x18005DEC2` | 先调用 `01F170`；其分配方式、节点前两字段初始化、失败/异常语义没有独立函数体证据。 |
| `05DECA–05DED1` | 计算返回地址 `+0x10`，测试这个计算结果是否为零。不可把它改述为普通 `returnedNode != null` 检查。 |
| `05DED3 / 05DEDE / 05DEE9` | 对 `node+0x10` 处字符串对象设置 capacity `0xF`、length `0`、首字节零。 |
| `05DEEC` | 调用 `022860(destination=node+0x10, source=入口 R9)`。这是实际 callee，不能拿已恢复的 `022CE0` 或 `023180` 代替。 |
| `05DEF1 / 05DEF5` | 上述调用返回后，读取 source `+0x20` 并写入 node `+0x30`；这是表 payload 的复制点。 |
| `05DEFE–05DF0B` | 返回原节点地址。 |

在 `docs/object-lifecycle`、`docs/rewards`、`tools/static_analysis` 的现存文本中，`01F170`、`022860` 只找到本批 04/05/06 的调用引用，没有找到以它们为入口的独立完整 body。这个结论只覆盖所检查的工程归档，不是说原版不存在这些函数。

## 4. `02E770` 查重与实际字符串语义

入口 RCX 是容器，RDX 是结果缓冲区，R8 是字符串键对象，R9 是已链入候选节点。容器 `+8` 是 sentinel，`+0x18` 是 first/last 桶数组，`+0x30` 是 mask。节点 `+0`/`+8` 是 next/previous，`+0x10` 是字符串键，`+0x30` 是表 payload。

- `0x18002E7A3–0x18002E7B6` 使用键的完整 stored length，并按 capacity `<16` 选择内联/堆指针。
- `2E7E0–2E7F2` 对每个字节做 `MOVZX`，采用 FNV-1a 64 位初始值 `0xCBF29CE484222325`、乘数 `0x100000001B3`，按 64 位回绕。嵌入 NUL 和高位字节都参与，不是 C 字符串截断，也没有再次归一化或转小写。
- `2E7F4–2E7FB` 对哈希与容器 mask 做 AND，再乘 16 定位 first/last pair。
- `2E808` 保存 bucket.first。非空桶在 `2E818/2E81D` 由 bucket.last.Next 开始，`2E830` 每次先取 previous 再比较，因此按 **last 到 first 的倒序** 查找。
- `2E854–2E874` 用两个 stored length 的较小值调用 `2EE060`，长度为零跳过调用，随后要求两个长度相等。该调用的正常可读缓冲区比较合同已由旧 [memcmp 导入证据](record-load-discovery/memcmp-import.json) 支持；本轮没有重新展开 CRT 或错误路径。
- 若快照显式含有多个相等键，选择的是倒序遇到的第一个，即当前桶序中最后一个相等节点。旧 [03C8E0 lookup](rea-record-load-dependencies/08-LoadFile-lookup-0x18003c8e0-assembly.json) 沿 next 前向找第一个相等节点；二者不能共用一个忽略顺序的字典查找。

### 重复键停止点

`2E876/2E879` 传入容器和候选节点，`2E87C` 必调用 `02EB40`。只有该调用返回后，`2E881/2E884` 才写结果 `(匹配到的已有节点, false)`。

在这个调用之前，`02E770` 本体没有写链表、桶端点或计数，但调用方的候选节点链入和计数增加已经存在。现有证据不能断言 `02EB40` 会怎样摘链、减计数、销毁字符串、释放节点或影响表 payload；也不能把未知调用的完成结果表述为“已保留旧表并释放新表”。前缀实现必须停在 `0x18002EB40`，保留入口状态，可以报告已经识别的重复节点身份，但不能伪造 native out pair 已被写入。

## 5. 新键的链表重排、桶写入与停止点

对完整稳定桶，查完无匹配后 RBX 等于先前保存的 bucket.first；空桶时它等于 sentinel。用 `n` 表示候选节点、`a=n.Next`、`b=RBX`：

- `2E8AC` 若 `b==a`，候选已经紧邻目标之前，跳过重接。
- 否则执行下面六次写入，按实际指令顺序把已经挂链的 `n` 移到 `b` 之前。

| 指令点 | 实际写入与有效双链域中的含义 |
| --- | --- |
| `0x18002E8B5` | `n.Previous.Next = a` |
| `2E8BC` | `a.Previous.Next = b`；此时 `a.Previous` 仍为 `n`，即设置 `n.Next=b`。 |
| `2E8C3` | `b.Previous.Next = n` |
| `2E8CE` | `b.Previous = a.Previous`；此时后者仍为 `n`。 |
| `2E8D6` | `a.Previous = n.Previous`；使用 `n` 的原 previous。 |
| `2E8DA` | `n.Previous = 原 b.Previous` |

空桶以 sentinel 为目标，候选被移到全局链尾；`2E8EE/2E8F5` 同时设置 bucket.first 和 bucket.last 为候选。已有桶以原 first 为目标，`2E901` 只把 first 更新为候选，last 保持。

`2E906–2E923` 另有调整 last 的机器码。对于这里选定的“完整、稳定桶，候选新建且尚未被任何桶索引”域，无匹配出口满足 `RBX==原 bucket.first`，重接只写节点指针，不改桶数组，故非空桶必走 `2E901`，这段 last 修正分支不可达。这是受限域推导，不能删除原版实际存在的分支，也不能推广到损坏快照、重复调用同一候选、并发改桶或其他未知 caller。

`02E770` 本体没有增加/减少计数。随后 `2E92B` **无条件**调用 `02EA80(container)`；只有它返回后，`2E931/2E934` 才写结果 `(candidate, true)`。

本函数没有 `COMISS`、负载阈值或“容量够则跳过 helper”的判断。不能借鉴 uint32 注册索引的另一特化，就推定 `02EA80` 在当前场景是 no-op。新键前缀执行完可报告已发生的 relink/桶端点改变，并在 `0x18002EA80` 返回 `Unsupported`；不能返回“无增长发布成功”，不能调用一个总是成功的占位 callback 越过停止点。

## 6. 前缀模型的最小有效域

这里定义的是便于检验的自有模型边界，不是新增的原生输入校验语义：

- 显式完整、有限、稳定的 sentinel 双链表和所有 bucket.first/last；双向链接一致，所有既有索引节点恰在其哈希桶连续区间内。
- 候选有独立节点身份，已在全局表头链入、计数已包含它，但没有任何桶把它列为索引节点。它的完整不可变键与入口 query 相同，表 payload 保留对象身份。
- 旧桶区间不能误把位于表头但未索引的候选算入。首尾及相邻桶关系按入口快照表达，不从节点数量推断 mask、容量或增长规则。
- 哈希、正常 `memcmp`、指针身份和六次链表写入的可观察结果保留。域检查、资源上限和托管准备必须在首次模型写入之前；拒绝一个不符合自有域的快照不等于模拟原版对损坏内存的反应。
- 对重复键停在 `02EB40` 前；对新键停在 `02EA80` 前。返回内容应区分入口已有副作用与本体本次改变，不能在未知 callee 或后续 caller 操作未执行时写成功标记。

现有 [RecordCache.cs](../../src/task_items/RecordCache.cs) 的只读快照支持完整字节键、next 顺序和桶区间查询，但没有可变 previous/sentinel 链、节点所有权、容量/负载增长合同。不能直接把它当作完整原生 publication 容器。已有 raw uint32 注册特化与 Binary 24-byte slot 容器也不构成这里的字符串节点构造、删除和重排证明。

## 7. 下一缺口与最小证据计划

现存限定归档搜索中，`02EA80`、`02EB40` 只命中本批 07/08/09 的 caller 引用；没有独立完整 body。以下是下一步需要的证据，不是已经执行或获准启动的新查询：

| 目标 | 实际来源 | 必须回答的问题 |
| --- | --- | --- |
| `0x18002EA80` | `02E770+0x1BB`，即 `0x18002E92B` 的直接 call | 正常无增长判定是否存在、在何时读取哪些字段；是否仍有必经依赖；是否重排或增长，副作用顺序与完成边界。 |
| `0x18002EB40` | `02E770+0x10C`，即 `0x18002E87C` 的直接 call | 候选摘链、计数、字符串/节点释放、payload 所有权的实际语义及必经依赖。 |
| `0x18001F170` | `05DEB0+0x12`，即 `0x18005DEC2` 的直接 call | 节点存储取得与 next/previous 初值，正常分配合同和未知依赖。 |
| `0x180022860` | `05DEB0+0x3C`，即 `0x18005DEEC` 的直接 call | 字符串 copy/move/别名行为、源对象变化及实际分配/释放依赖；不能仅按“复制 helper”名称猜测。 |

若下一目标是把**显式已链入入口**推进到 publication 返回，最小固定批次仅查询前两个函数：每个 `procedure_pseudo_code`、`procedure_assembly`、`procedure_callees` 三项，共 6 项分析，加 `binary_session → open_binary → binary_session` 和 `close_binary → binary_session` 五项生命周期，合计 11 次。目标和哈希沿用已固定 Engine，保留既有内存、所有权、关闭和结果校验；具体计划/runner 注册应由主任务另行生成和批准，不扩大到任意地址或自动追随新 callees。

若以后要恢复调用方的真实节点创建，再独立决定是否查询后两个函数；同样可以构成 6 项分析加 5 项生命周期的有界批次。获得 body 后仍需检查新的必经依赖，不能预先承诺 11 次就能闭合完整冷加载。

本批 11/11 与正常释放证明的是静态查询完成。本文不新增测试计数，不宣称前缀实现已通过验收，也不将原版冷加载、原生对象注册、实际派生 `Read` 或完整游戏行为记为已恢复。
