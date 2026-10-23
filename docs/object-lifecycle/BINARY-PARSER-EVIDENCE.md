# Binary record parser：fresh tag2 正常返回域

2026-10-20。本文只使用工程中已有静态归档，记录 `0x18013F510` 及其必要 helper 的可实现边界。Engine SHA256 为 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`；地址均为 preferred VA。来源文件、字节哈希及 evidence ID 见 [evidence-sources.json](parser-implementation-validation/evidence-sources.json)。**实现与测试待统一验收；静态查询成功不等于行为测试通过。**

最小正常域为：fresh typed storage；完整、稳定、无 offset wrap 的 reader 窗口；字段数 `N=2..15`；所有字段 tag 均为 2，且各字段 count 为正；累计不超过 65536 个 int32。每个字段是 little-endian 的 8 字节头及 `count*4` 字节 payload：首 DWORD 低 16 位为 tag、高 16 位为 count，第二 DWORD 为 raw uint32 key。payload 保留全部 32 位，并以 int32 暴露；负数与高位值不在解析阶段改写成有效 pool index。

## 已观察到的两遍解析与 fresh 初始化

[parser 汇编](rea-binary-parser-entry-retry-2/05-Binary-parser-entry-0x18013f510-assembly.json) 在 `13F53D/13F541` 使用 table `+28` 的锁，保存 reader 当前相对 base 的低 32 位 offset。第一遍 `13F590..13F5E3` 读头、推进 cursor、统计各 tag 的元素总数与已识别字段数；第二遍前在 `13F6E3..13F6ED` 恢复初始 offset。native reader 的 base/current/length 分别位于 `+0/+8/+10`，这不是自有 reader 的 ABI。

[构造器](rea-binary-table-constructor/05-LoadTableBinary-constructor-0x18013f300-assembly.json) 在基类构造正常返回后，把三组 begin/end/capacity（table `+30..+70`）写零；[0481F0 的已证 count=0 分支](rea-binary-subobject-profile/05-Binary-subobject-init-0x1800481f0-assembly.json) 把 `+78/+80/+88` 写零且无 callee；caller 再把 bit count `+90`、descriptor pointer `+98` 及 field index 的已写字段清零。因此 fresh typed/bit storage 有实际来源，不以调用方预分配掩盖首次增长。此推导不把未知 padding 或整个对象内存视为零，也不恢复基类构造内部。

在本覆盖域，三个向量的准备调用仍按 `13F654 → 13F662 → 13F676` 发生。前两个 requested=old size=0；[030D20](rea-binary-parser-helpers/05-Parser-helper-0x180030d20-assembly.json) 的相等分支直接返回。第三个 requested 为累计 tag2 元素数 `m>0`：

1. fresh size/capacity=0 时，`030DC7` 向 `02E0F0` 传入向量地址和 `RDX=m`；不能采用伪码漏显第二参数的签名。
2. [02E0F0](rea-binary-parser-vector-growth/05-Parser-vector-growth-0x18002e0f0-assembly.json) 在 `02E112` 调用 `022F00(m,4)`，`02E127` 仍调用零字节 memmove，fresh null begin 在 `02E13E` 跳过旧块释放。
3. `02E15F → 02E167 → 02E16B` 依次写 capacity=`newBase+4m`、end=`newBase`、begin=`newBase`。返回后 `030DE6` 清零新增区域，`030DFD` 才推进逻辑 end。
4. `13F694` 仍调用 [0C69F0](rea-binary-parser-helpers/07-Parser-helper-0x1800c69f0-assembly.json)；旧 bit count=新 bit count=0 时，`0C6A07 → 0C6A94 → 0C6B81` 返回，不调用 bit 增长/删除 helper，也不写容器。

[旧 022F00 正文](rea-container-reserve/05-Container-reserve-0x180022f00-assembly.json) 已证明 count=0 返回 null、检查 count×size 溢出；字节数小于 4096 时直接调用 `2DC398(bytes)`，否则申请 `bytes+39`、32 字节对齐并在 aligned−8 保存原 base。它不清零 payload、不改向量字段；size=4 的分支边界为 m=1024。底层分配器、memmove/memset/memcpy 的内部实现、原生地址与 OOM/EH 未恢复。本实现沿用 [既有正常成功契约](OBJECT-NAME-EVIDENCE.md)：有效自有存储、分配和内存操作正常成功；大块原生存储符合已证对齐元数据。自有数组不声称复现 allocator hooks、中途故障、异常清理或失败回滚。

## Field index、descriptor 与重复顺序

parser 在 `13F6AF` 调用 [284080](rea-binary-parser-helpers/09-Parser-helper-0x180284080-assembly.json)，参数为 `table+A0, N>>1, N`。该 helper 计算 `log(request)*1.4426950408889634`，以 `CVTTSD2SI RAX` 转换，再按 EAX 无符号值 clamp 到 2..16，最后移位产生桶数。在 `N=2..15`、request=1..7 的有限正常 log 契约下结果为 4 桶、mask=3。这里不运行或替换 UCRT，也不泛化为任意输入的浮点等价。

`2840E0/2840EB` 分别写 mask 与 bucket count；分配并清零 4 个桶头后，`284124/284129` 分配 `24*N` 字节并发布 slot pointer，`284136` 写容量 N，**只清零 `24*(N>>1)` 字节 slot 区域**。helper 不写 shift 和 used count；它们保留构造器的零。不能声称全部 N 个 slot 初始为零。后续每个插入都覆盖自己的完整 24 字节节点，且恰好 N 次插入不会进入增长。

`13F6CC/13F6D1` 分配并发布 `12*N` 字节 descriptor 存储。第二遍 tag2 分支 `13F7C4..13F7EE` 复制 payload 并推进 reader 与 buffer index；`13F897/13F89A/13F89E` 保存三个 DWORD `{tag, startingBufferIndex, elementCount}`。**elementCount 必须独立保留**，即使现有单值 getter 只使用 tag/index。

`13F8A2 MOV EDX,ESI` 把 raw uint32 key 零扩展为 64 位，`R8` 仍是 descriptor 地址，随后 `13F8A9` 调用 [284160](rea-binary-cold-insert/05-Binary-cold-insert-0x180284160-assembly.json)。该 helper 本身保存完整 RDX，并不截断 key。桶定位为 `uint32(key >> (shift&63)) & mask`；这里 shift=0、mask=3。写序为 used count → slot key → descriptor pointer → slot.next → bucket head，没有 duplicate 检查，因此同桶节点按插入顺序反向链接，后来的同 key descriptor 是首次查找命中。不得复用名字 pointer-cache caller 的 signed ID 参数解释此处 payload。

## 覆盖边界与外部上下文

N=1 对应 log(0)，其异常/MXCSR 路径未闭合；N>=16 涉及未模拟的浮点整数边界。其他 tag、零 count、重解析、截断输入及解析预算之外的输入，由自有解析 API 返回 coverage `Unsupported`；非法窗口、超过 1 MiB 的输入数组等自有 reader 构造参数则在构造时抛参数异常。原生异常路径不建模。这些边界**不表示 native parser 会拒绝、回滚或具有同样错误返回值**。

输入 clone、ownerGate 身份要求、预检、Consumed 标记、Dispose 和托管异常报告是自有 API 的存储与生命周期边界，不是 native BinaryReader ABI。只有通过预检后才重放两遍 cursor 写入；已发生的 cursor/consumption 不因后续自有失败而抹去。自有 gate 保障该 reader 与解析目标的独占使用，不合并 manager allocator/registration 锁，也不宣称 native EH 清理等价。

解析产物只建立 typed values、完整 descriptor 和 field index。共享 archive string pool、名字到 ID 的索引及 pointer cache 仍由调用者显式提供；可与既有 getter 连接，但不伪造这些 archive 内容。`table+C8` 是 archive 参数，不等于 `archive+C8` 的共享池。完整 [LoadFile miss](rea-record-load-dependencies/11-LoadFile-miss-0x1801c4e20-assembly.json) 的 archive 读取、reader 建立、对象构造与 cache publication 尚未整体恢复；解析也不是实际派生对象的 Read。

## 本轮静态查询及待验收状态

| 归档 | 完成与时长 | 关闭、资源复核 |
| --- | --- | --- |
| [三 helper summary](rea-binary-parser-helpers/summary.json) | 11/11 passed；05:40:31.528Z–05:42:03.882Z，92.354 秒 | close/session 已确认，退出 0、无强制终止；[release](rea-binary-parser-helpers/resource-release.json) 记录 61 个身份，同身份剩余 0、analysis active 0、runtime 文件 0 |
| [vector growth summary](rea-binary-parser-vector-growth/summary.json) | 7/7 passed；05:45:23.049Z–05:46:56.965Z，93.916 秒 | close/session 已确认，退出 0、无强制终止；[release](rea-binary-parser-vector-growth/resource-release.json) 记录 52 个身份，同身份剩余 0、analysis active 0、runtime 文件 0 |

两批合计 18 calls，均使用已验证 Windows Job 归属、384M heap、physical/commit 双余量启动 4.25 GiB / 停止 2 GiB；Job empty 与 sampler 退出 0 均确认，未触发止损。两批原版前后哈希均一致，`targets_launched=false`。这些是已完成查询的测量与关闭事实，不是后续批次资源上界。本次证据整理没有启动任何后端、读取原版、编译或测试；不写“实现已通过”的结论。
