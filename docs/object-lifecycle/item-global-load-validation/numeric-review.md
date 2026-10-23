# Binary float 数值读取：140420 独立证据复核

2026-10-21。本次仅阅读 workspace 归档；没有原版读取、reader/dumpbin/构建/测试执行，也未修改实现。模型静审另行报告，本文只固定已经取得的机器码合同。地址均为 preferred VA，以下省略 `0x180` 前缀。

## 证据与控制流范围

* [inspection.json](../offline-item-global-float-value/inspection.json)，SHA256 `9eed3516ef05b1cc0de18a24e3ea2b8e28050cb29480ffaaba8be7dc3930da4a`。
* [140420-instructions.json](../offline-item-global-float-value/140420-instructions.json)，SHA256 `eb9b6744620b368f3999ed5cda38ed8dd9e84ef2ae07fbf1e215c4252ba258ad`。
* [140420-entry-window.bin](../offline-item-global-float-value/140420-entry-window.bin)，SHA256 `dc5545b6d0f7bc5a0f7812ea8863b9a839f1ad5a1ccb3d2c4786bedf21fabe51`。

报告状态 `bounded-float-value-entry-window-decoded-manual-cfg-required`。这次根侧已完成的新读为 112 B code、36 B `.pdata` metadata、1 次 dumpbin；source_closed/source_hash_preserved/source_stat_stable 为 true，failures/decode_issues 为空，byte_coverage_verified 为 true。

目标由上一正文 `140349 CALL 140420` 直接定位。`.pdata` 不存在该 exact start：前一条结束 `140419`，后一条开始 `140490`。固定候选读取 `[140420,140490)`，**不能把 gap、候选窗口或尾 CC 当作真实函数边界**。

人工 CFG 核验：7 个条件跳转全部指向本窗口中的已解码指令：`140430/140451/140468/140471 → 140461`，`140456 → 140465`，`14045F → 140453`，`140479 → 14047F`。两个正常出口为 `140464 RET` 和 `14048E RET`；没有 CALL、间接跳转或窗口外边。正常入口覆盖到第二个 RET 共 111 B / 35 条；`14048F CC` 是另保留的第 112 字节/第 36 条，不是正常路径动作。链表循环能否终止及内存有效性仍需状态前提；窗口内部 CFG 不保证任意损坏链也能返回。

## 桶和首匹配节点

入口 `140420` 首先读取 `this+B8` 桶数组指针；`140427` 保存 this；`14042A MOVSXD R9,EDX` 将 name helper 返回的低32位按 signed int32 扩展成完整 qword key。已知空桶数组在 `140430` 直接选择默认值，不读取 shift、mask、节点或 payload；未知数组不能冒充已知空。

非空数组依序读取 mask DWORD `this+A8`（140432）、shift DWORD `this+A0`（14043C），执行：

```text
key64 = sign_extend_32(id32)
bucket = uint32(key64 >> (shift & 63)) & mask32
```

这里 `>>` 是 qword **逻辑**右移；`140445 MOV EDX,EDX` 在移位之后截低32位，随后 `AND RDX,RAX`。不能先截 key、使用有符号算术移位、或擅自把 mask 替换成 bucketCount−1。`14044A` 读取选中桶头。

从 `140453` 按完整 qword 比较节点 `+0` key；不匹配才由 `140458` 沿 `+10` next 前进。选择**链中第一个匹配**，不按描述符排序、不合并重复项、不继续寻找“第一个 type1”。桶头或链尾已知 null 返回原 default；未知节点/链/可达性属于未覆盖读取，不是 native miss。

`140465/140468` 再测试已选节点；随后 `14046A` 取节点 `+8` payload 指针，`14046E` 比较 payload 的 DWORD tag 是否恰好为1。tag≠1直接默认；没有 payload-null 默认分支，也没有 buffer 或 count 的提前读取。坏 payload 指针不能包装成正常 type mismatch。

## 无符号 clamp、两次32位 wrap 与原始位返回

tag1 时才执行：

| 指令 | 观察 |
| --- | --- |
| `140473 MOV EAX,[payload+8]` | 读取 DWORD elementCount |
| `140476 CMP R8D,EAX` / `140479 JB 14047F` | **无符号** requestedIndex < count 时保留原 index |
| `14047B LEA R8D,[RAX-1]` | 否则 effectiveIndex=`unchecked(count-1)`，写32位并零扩展 |
| `14047F MOV ECX,[payload+4]` | 此时才读 DWORD baseIndex |
| `140482 MOV RAX,[this+48]` | 读取本表 raw DWORD buffer 指针 |
| `140486 ADD ECX,R8D` | bufferIndex=`unchecked(baseIndex+effectiveIndex)`，32位 wrap 后零扩展 |
| `140489 MOVSS XMM0,[RAX+RCX*4]` | 读取该 DWORD 原始位；随后 RET |

所以 count=0 不存在特殊 default 分支：所有 uint requestedIndex 都不小于0，effectiveIndex 变为 `FFFFFFFF`。例如 baseIndex=1 最后 wrap 到0；baseIndex=0 则为 `FFFFFFFF`。自有已捕获 buffer 可在最终实际读取越界时停止，但不能在 count=0 时直接返回 default，也不能把整个计算改成64位相加。索引越界不是这里已证的 native bounds check。

默认路径 `140461 MOVAPS XMM0,XMM3` 直接返回 caller 提供的 default（对 float 结果关注低32位）；tag1成功路径直接从 buffer MOVSS。两者都没有文本解析、整数转浮点、缩放、算术、finite/NaN筛选或 float→double→float 中转。负零、NaN payload、无穷等 DWORD 位必须保留；是否适合更上层算术是不同合同。成功值读取不需要 default 内容已知，默认分支也不应预先要求未到达的 payload/count/buffer 已知。

## 与已证 wrapper / parser 接合的最小约束

上一 [direct-review.md](direct-review.md) 已固定 `1402E0`：gate 内先读 C8；非零时先执行 `283840` 的名称缓存过程，再将 EAX、不变 this、R8D 与原 default 交到这里。`13FD50` 标量入口将 R8D设0。因此这一 numeric body 可以闭合该明确 Binary 路径中的数值步骤，但不识别未知 vptr、创建 archive 或替代 Base 的 `4DD80` 文本解析。

1. 必须保留当前表的同一 `Fields` 与 `Gate`。独立提供一份 parsed record 不能无条件当作该表的 payload/buffer；若只支持解析后稳定 own 状态，可把同一对象引用约束写成自有覆盖 guard。不得靠字段值相等掩盖两个来源不同的记录。
2. `BinaryFieldSnapshot` 决定桶顺序和实际首匹配 payload；`BinaryParsedRecord.CopyDescriptors()` 补充 count，tag1 raw values 补充 DWORD 数据。它们必须来自同一次 parse，不能先用 name key 在 descriptors 中取第一项。既有 parser 将新同 key 节点 prepend，故重复 descriptor 要与实际首节点对应，通常是最后一次插入；若最后一个同 key 为其他 tag，native直接默认，不回退较早的 tag1。
3. parser 插入字段 key 时由 uint 零扩展；numeric 将 int32 ID 符号扩展。这一区别对高位 key 有意义，不能仅按低32位匹配而改变原桶链语义。任意自有负 key 场景也应按实际完整 qword 比较。
4. count/index/raw buffer 的读取保持上述 lazy 顺序；自有范围错误不转为默认结果。未知 default 仅在返回默认值的已到达分支阻断。
5. 名称步骤已经完成的 stale 写回、cold count/node/head 发布或增长等事实，必须在 numeric unsupported/default/值返回之后保留。不能调用旧 string numeric getter来间接取 float，更不能为了 numeric metadata 不足而回滚名称缓存。
6. 这是正常有效存储下的逻辑投影。窗口未读 EH 元数据；gate、malloc/free、错误内存和竞态的 native行为没有因这个无 CALL leaf 而补全。模型自己的异常处理、预算和 captured bounds 应明确标成 own 覆盖边界。

独立静态结论：本窗口足以确定上述入口的正常数值读取 CFG，无需根据末尾 CC 继续读取邻居。实现与测试若采用这些合同，应只对到达的观察要求已知，保留前缀并清楚区分默认返回与未覆盖数据。实际模型静审尚未作为本文通过结论。
