# Global-load 两个直接依赖：独立静态复核

2026-10-21。本次仅阅读工程中已保存的指令、报告及相关旧证据；没有读取原版 DLL、运行 reader/dumpbin/测试，或修改实现。所有地址为 preferred VA；下文省略共同前缀 `0x180`。这份复核不把磁盘 vtable 值等同于任意加载中对象的 vptr。

## 原始绑定与范围

主报告 [inspection.json](../offline-item-global-load-direct/inspection.json) SHA256 `dd24c4565a04de81051c8866e7d927cff0e807bd5ec48d88a899c1febff65b1c`，状态 `two-fixed-global-load-direct-bodies-decoded-review-required`。两个正文逐字节覆盖均成立，decode issues 与 failures 均为空；原件 source_closed/source_hash_preserved/source_stat_stable 均为 true。报告计正文 **385 B、90 条指令**，新 `.pdata` 元数据 **168 B**，另有固定槽数据 **8 B**，两次 dumpbin。此次 reviewer 没有重复这些动作。

| 对象 | 边界 / 绑定 | 指令文件 SHA256 | 原始正文 SHA256 |
| --- | --- | --- | --- |
| Binary indexed float | 磁盘 `341FE0+98=342078` 原始 `e002148001000000` → `1402E0`；exact `.pdata` index 5310，`[1402E0,140378)`，152 B / 38 条 | [binary-indexed-float-1402e0-instructions.json](../offline-item-global-load-direct/binary-indexed-float-1402e0-instructions.json) `ff43cb908455614a3e51d78ddeaefa697f06cb8939e1a309971c62863c8c5bec` | `0d10653e6dd2edbaf4748e98f038d110e15bd6ff182b9ce33e71bcf0382985fb` |
| GetLoadTable tail | `17EE80` 调整 `this+170` 后尾跳；exact `.pdata` index 7296，`[1C4C30,1C4D19)`，233 B / 52 条 | [get-load-table-tail-1c4c30-instructions.json](../offline-item-global-load-direct/get-load-table-tail-1c4c30-instructions.json) `579162e5522e880be8539db8ec81622b309e2c25c86104b151631925c5df9ece` | `8bce5bad7446488689a4f802d2b4b979fdacabdac244e92821a6101881ab3226` |

上一步 leaf 报告 SHA `75d55f7a959c2b012cc5e9374a25bf588f6457bf1f2ef5a8867805942a2d1e66`。`13FD50` 的正常入口仅 16 B / 4 条：重新读取 `[RCX]`，`XMM3=XMM2`，`R8=0`，尾跳 `[vptr+98]`。`13FD60` 的 `+90` 跳转是窗口后续未归属代码，不是本入口的下一步；没有直接边授权 `13FD70`。此次通过实际 `+98` 槽继续，避免用邻近函数猜目标。

## 1402E0：默认值、参数和名称缓存前缀

`140303` 将输入 XMM3 保存到 XMM6；`140306` 保存 R8D 到 ESI，`140309/14030C` 保存 query 与 this。`140318/14031C` 读取 `this+28` 的 gate 指针并进入；随后 `140323` 才读取 `this+C8`。

* **明确 C8=0**：`14032D → 140351`，跳过 query 内容、名称映射、指针缓存和 numeric getter；解锁后从 XMM6 返回原始 default 位。未知 C8 不能按这个分支处理。
* **明确非零 C8**：以本次采样的 context 加 `88`，原 query 指针不变，在 `140339` 调 `283840`。包装器没有 query 复制、归一化或地址替换，也没有在调用后重读 C8。
* helper 正常返回后，`14033E/140341/140344/140346` 恢复 default、原 R8D、EAX 的低 32 位和原 this；`140349` 直接调用 `140420(this, id32, originalR8D, default)`。`MOV EDX,EAX` 将 RDX 高位清零；这并不提前规定下一函数如何解释符号。这里不检查 `id32 == FFFFFFFF`，不能增加 `-1 → default` 捷径。
* 当前外层 `13FD50` 明确提供 R8D=0；该值是实际第三参数，只有后续 `140420` 正文才能进一步确定其意义。默认 float 从 ABI 第三位置 XMM2 移至第四位置 XMM3，不能继续按旧三参数 getter 接线。
* `14034E` 保存 numeric 实际 XMM0 结果到 XMM6，`140351/140355` 再读取 `this+28` gate 指针并解锁，`14035B` 返回保存结果。解锁后不重跑 lookup、不重读结果存储；正常非易失寄存器合同仍是前提。

Game 两个字段调用各自先读取 table vptr 决定 `+48`；进入 `13FD50` 又读取一次当前 vptr 决定 `+98`。因此不能在整个两个字段加载前只固定一次 vptr。磁盘 Binary vtable 的这两个槽已绑定，但调用时的实际对象类型与存储有效性仍需明确。

已有 [`283840` 指令](../rea-record-getter-support/11-LoadTableBinary-name-to-id-0x180283840-assembly.json) SHA `b107eb37978d90f0ea5eb7b2be403ec3a189740162f4092b6b931f0e0b133af5` 可直接复用名称步骤：完整 query 指针身份选缓存链；warm 还需当前 C-string 与缓存 ID 所指共享池字符串相等。stale 成功查得非 -1 ID 后在 `2839F4` 更新原命中节点；cold 成功查得非 -1 ID 后在 `283915` 调 `284160` 插入，正常返回后才继续 numeric。负非 -1 ID 的符号扩展写入不能省略；名称 miss/-1 不写相应缓存，但仍返回 EAX 给 numeric。

这些是有副作用的步骤，不是一个纯字典查询。旧 [`BINARY-STALE-EVIDENCE.md`](../BINARY-STALE-EVIDENCE.md) 与 [`BINARY-COLD-RESULT.md`](../BINARY-COLD-RESULT.md) 已分别记录写回/插入前缀，后续 [`BINARY-GROWTH-RESULT.md`](../BINARY-GROWTH-RESULT.md) 又扩展了增长正常域；不能把更早文档中的“cold 全未实现”当当前源码状态。现有 `RecordTableGetter` 的 string 数值步骤终到 `13FC80`，不能为复用名称缓存而先运行那个 string getter，再转换返回值为 float。

若名称 helper 无法继续，不能声称 numeric 已调用；若 numeric 未知，名称缓存的已完成写入仍保留。若任一 helper、门锁或释放不正常返回，当前正文只证明正常控制流，未恢复 native EH、锁回收或 rollback。托管模型的 finally 不构成原生异常合同。最小新的算法边界是实际直接调用的 **140420**；不能用 Base 文本解析或旧 tag=2 字符串行为代替。

## 1C4C30：查表返回与 fallback

这里的 this 已是 `ObjectManager+170` 指向的 depot。`1C4C4F/1C4C58/1C4C5C` 使用 depot+78 保存的 gate。接下来初始化本地 string（capacity15/length0/首 byte0），`1C4C89` 调 `023180(local, originalString, position0, countUINT64_MAX)`，然后 `1C4C93` 调 `27B3C0`，`1C4CA6` 调 `03C8E0(depot+8, outNode, normalizedLocal)`。

这是旧 LoadFile 使用的全 stored-length 复制/名称处理/哈希查找组合，可复用它们的已证正常域。名称处理含反斜杠转斜杠、其他字节经显式 CRT tolower；不是 Base 字段键比较，也不是无条件 Unicode/不区分大小写字符串操作。未知 locale、字节或容器结构不能变成 lookup miss。

返回选择的读取顺序：

1. `1C4CAB` 读取 outNode；`1C4CB0` 与当前 `depot+10` sentinel 比较。
2. sentinel → `1C4CC3`，读取 **`[depot+48]` 的 qword**。
3. 非 sentinel → `1C4CB6` 检查当前 node+30 qword；为零也去同一 fallback。
4. 非零 payload → `1C4CBD` 再读取 node+30 到 RDI。正常稳定存储域中返回该非零身份；两次实际内存读取不能被描述成 native 原子事务。
5. `1C4CC7..1C4CF7` 按 capacity 释放/重置本地字符串；`1C4D00` 解锁；`1C4D06` 将已保存 RDI 返回，`1C4D18 RET`。

fallback 是保存的指针值，不是 `depot+48` 字段本身的地址，也不是在这里构造一个新对象。此正文不检查 fallback 非零、不读其 vptr、不增加引用、不复制表、不调用 parser，不证明其具体类型/创建者/所有权/解锁后生命周期。精确 export 的 C++ reference 返回形态不能代替这些运行时前提；已知零 fallback 与未知 fallback 是不同状态。正常返回前的本地 string cleanup 不释放返回的 table。

| 情况 | 1C4D20 LoadFile（旧证据） | 1C4C30 GetLoadTable（新证据） |
| --- | --- | --- |
| 命中节点，payload 非零 | hit DWORD+54 先递增，再返回 payload | 返回 payload；本正文无计数写入 |
| 命中节点，payload=0 | 仍 hit++，返回已知零 | 读取并返回 depot+48 fallback；本正文无计数写入 |
| sentinel | 调 1C4E20 miss loader，包含独立加载/发布/计数合同 | 读取并返回 depot+48 fallback；不进入 miss loader |

因此 Game `2CA469` 的 LoadTableFile 已完成的副作用，在随后 `2CA47F` GetLoadTable 不能抹去；Get 本身也不能重复增加一次 LoadFile hit。Game 两次分别获取 ObjectManager，第二次结果不能因为前一次正常返回就默认为相同实例或相同 depot。已证 caller 丢弃第一次 LoadTableFile 返回值，RSI 来自第二次 Get；要证明它等于第一次返回值，仍需相同 owner、查询/归一化环境、稳定缓存/生命周期等状态前提。

## 复用与剩余边界

可复用的是正常 gate、局部 string copy/release、明确 locale 下的名称归一化、完整桶链查找，以及 Binary 名称指针缓存的已证副作用。可新建显式 own depot fallback 观察，不可把它默认设为新 Base 表或 null。返回表类型应在真正派发前验证；未知类型应保留停点，不应为了接通两字段而强制当 Binary。

新增必要证据只剩这条 Binary 直接链中的 `140420` 算法。Base 的 `4DD80`/格式字节缺口、实际 native 初始化/原始 archive fetch、fallback 创建和存续、加载中 vptr、锁与 EH 等仍各自保留；本复核不要求扩大到整个 ObjectManager、Engine 或所有 getter。

相关旧证据复核哈希：`RECORD-LOAD-EVIDENCE.md` = `6af3536df671f8f86c6fd6d2779eaa925d937ece9e9d1fa5a27897d87a6191da`；`rea-record-load-dependencies/05-LoadFile-name-processing-0x18027b3c0-assembly.json` = `8097aba369f485d561765e1bb33eb9b3cc41c404ac6bc1ed87b346f61701d2ef`；同目录 `08-LoadFile-lookup-0x18003c8e0-assembly.json` = `f8567c9508576018f836e131b89a9ceb6181a349a2bf6373dd2442370705701a`；`BINARY-STALE-EVIDENCE.md` = `4cb35e227d34984f0e785d53fd0e5ce311e9d2e5ccc6aff1e71dceb6090fc1d7`；`BINARY-COLD-RESULT.md` = `8cf5354e413af7998d644c3c514a3c3bc67252e03237cdf9b1519fbce51de0c8`；`BINARY-GROWTH-RESULT.md` = `9fab9d2c53c174cc73ddbc3dcb1cb6deca893224f856c6c698af7ad2ec3e25ab`。
