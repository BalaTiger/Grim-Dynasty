# tag3 与较多字段：静态证据及覆盖合同

本轮只扩展自有 fresh body parser。证据绑定 Engine SHA256
`4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`。
原生代码、Rea、Java 均不执行；正常分配、正常复制是明确前提，不据此恢复原生 ABI、OOM 或异常展开。

## tag3 的转换与索引

依据保存的 [13F510 assembly](rea-binary-parser-entry-retry-2/05-Binary-parser-entry-0x18013f510-assembly.json)，
SHA256 `799736dfda45d5eb59e0d4e45766e43c150f1ece2272f0a3961b551592a8b635`：

- 第一遍按 tag 分别累加 U16 count；tag3 每个元素仍占输入 4 字节。
- `13F822..13F82C` 每次读取完整 DWORD，并将 reader 游标推进 4。
- `13F830..13F843` 用累积 bit index 的 `>>5` 定位 DWORD、`&31` 定位位。
- `13F84A` 测试完整 DWORD：零走 BTR，任意非零走 BTS。`2`、`80000000`、`FFFFFFFF` 都为真，不是仅 `1` 为真，也不按 float 解释。
- 每个字段 descriptor 为 `{tag=3, startingBitIndex, elementCount}`。原 key 为 uint32 零扩展，重复 key 仍前插，最新字段优先。
- 字符串 getter 仅处理 tag2；较新的 tag3 会遮蔽旧 tag2 并返回原 default，不应继续查找旧字符串字段。

输出使用独立 `BooleanCount` 与 `CopyBooleanWords()`；`CopyRawValues(3)` 仍拒绝。
位导出按 LSB-first 的 uint32 word 表达，末字无效位归零属于自有逻辑导出约定；不把它等同于任意原生容器状态的填充内存。
tag0/1/2 保持原 uint32 位模式及各自独立 offset。

## fresh 存储准备链

已保存 constructor 证明三个 word-vector 指针与独立 bit count 初始为零。
`0C69F0` 的 fresh 正数分支调用 `047D30`，传递追加位数与零 fill byte。

本轮精确读取的必要路径为：

| 路径 | 已观察的作用 |
| --- | --- |
| `047D30` | 调 `047F10` 准备空间，随后逐个有效 bit 填零；最后构造输出迭代器 |
| `047F10..047F66` | 计算插入 bit index；fresh 为零；本域不走长度溢出分支 |
| `047F67..047F97` | 调 `048130(this, ceil((oldBits+delta)/32), &zeroDWORD)`；返回后发布 fresh bit count |
| `047F98..047FAF` | 返回起始 bit index，不进入非 fresh 的 `047FB0` 分支 |
| `048130` | 调 reserve、填充所需 DWORD、更新 word-vector end |
| `02DE40` | fresh 所需 capacity 等于 requested words；转入已有 `02E0F0` |
| `02E0F0 → 022F00` | 复用已有正常分配、零长度复制合同；仅修改三个 vector 指针，不修改 `+18` bit count |
| `047EA0` | 无 callee，仅写调用方提供的 16 字节 iterator 输出，不改容器 |

`0483A0` 的 `.pdata` 查询没有条目，失败诊断保留在
[words-fill-boundary](offline-binary-bits-words-fill-boundary/boundary.json)。
这不是权限拒绝，也不能以相邻函数地址猜测其长度。

最终以已知入口的有界增量前缀解码闭合该 leaf：
[0483A0 完整指令](offline-binary-bits-fill-prefix-walk-v3/0483a0-instructions.json)，
SHA256 `29a3815ce82ea25d998d3ffd7472f918bbf1ca1e2e5bd0239a109dda92611f3c`。
可达范围为 `0483A0..0483B9`，26 字节、10 条指令，无 CALL。
正 count 且正常 non-null 目标时，逐次执行 `MOV EAX,[R8] → MOV [RCX],EAX`，
然后目标加4、count减1，直到返回；不写 vector 元数据或 `+18`。
因此本 fresh 合同中 `ceil(bits/32)` 个完整 DWORD 从零开始，包括末字的未使用位；
这一观察不推广到非 fresh、重新解析或分配范围以外的 padding。

初次 `/RANGE:entry,entry` 的 dumpbin 退出码为0，但只输出原始字节 `48`，脚本报 `Undecoded instruction`。
第二次前缀读取将 `48`、`85` 分成两行，触发读取器过严的单行断言 `2 != 1`。
两次失败原样保留；修复对连续无 mnemonic 字节行的识别后，v3 仅在当前前缀尚未完整时增加1字节，
每条至多15字节，独立核对实际原件字节，按明确控制流追踪，在 RET 停止。
该最终取证耗时0.830秒，累计请求51字节、去重26字节。全 bit-helper 链独立代码共984字节，
另有 log 跳板6字节和常数8字节；没有把重复前缀或邻近字节计作新增函数体。
[全过程与哈希](offline-binary-bits-fill-prefix-walk-v3/inspection.json)
保留准确命令、每次区间、地址映射、控制流及前后指纹。

由此闭合 fresh old=0、正请求不超过65,536 bit 的初始化、填零、end 与 bit count 发布路径。
分配器成功仍是已有合同；非 fresh 插入/移动、异常展开及 OOM 不在闭合范围。

## 字段容量与桶数

`13F6A5..13F6AF` 向 `284080` 传入 `request=N>>1`、slot capacity=N。
该 helper 对 `log(request) × binary64 log2(e)` 截断，再将指数夹取到 2..16。
[定点导入与常数证据](offline-binary-parser-log/log-import-and-multiplier.json) 确认：

- `2EE07E` 的 6 字节跳板指向 `api-ms-win-crt-math-l1-1-0.dll!log`。
- `34BBC8` 的 8 字节为 `fe822b654715f73f`，binary64 bits `3ff71547652b82fe`。
- 未执行目标 CRT，也未用宿主 Math.Log 代替目标运行结果。

在正常、有限且准确的 log 合同下：

| 字段数 N | request | 桶数 | 覆盖状态 |
| --- | --- | --- | --- |
| 2..15 | 1..7 | 4 | 覆盖 |
| 16/17 | 8 | 截断整数边界未证 | preflight 拒绝 |
| 18..31 | 9..15 | 8 | 覆盖 |
| 32/33 | 16 | 截断整数边界未证 | preflight 拒绝 |
| 34..63 | 17..31 | 16 | 覆盖 |
| >63 | — | — | 自主覆盖上限，不是原生格式上限 |

CRT 精确返回位及 MXCSR 未恢复，不能把两个边界直接当成数学 log2。
native slot 分配为 `24*N`，实际初始清零仅 `24*(N>>1)`；descriptor 独立分配 `12*N`。
fresh used=0、capacity=N，最多 N 次插入不会触发 `284200` 增长。
N=38 时为 16 桶、38 槽，初始清零 456 字节；不宣称所有槽的 native padding 均已初始化。

## 拒绝、失败与所有权

完整稳定 reader 窗口、正 count、总输入 word 数不超过 65,536 是自有覆盖前提。
截断、未知 tag、零 count、预算及舍入边界在 preflight 拒绝，游标不动、reader 不消费。
这不是原生 malformed-input、访问异常或回滚行为的恢复。

进入两遍解析后 reader 已消费；自有 managed 异常继续保留已写游标，并在 finally 释放 gate，不发布不完整 record。
测试覆盖确定性的参数异常、拒绝和生命周期；没有通过制造 OOM 推断 native EH。
所有导出数组均复制，parsed record 独立于 reader 生命周期。

## 官方样本的验证边界

parser 本身不需要字符串池。官方复验保留全部原 ID，针对此前已验证的同一归档哈希和一条 scrapmetal 记录，
只重读固定 323 字节压缩载荷，并与冻结的 456 字节 body 哈希及 38 字段观察逐项比较。
前后整档哈希使用 64 KiB 流式 buffer；它们属于指纹读取，不是整档内存加载。

原归档 82,999 个字符串仍超过现有 Source 的 4,096 项 / 2 MiB 限制，也超过 generated import 的源项限制。
本轮不改变这些预算，不裁剪、重编号或构造伪完整 pool。
原 ID 的 lazy resolver 可独立设计，但需稳定身份、明确缺项与越界区别，并供名称及数值 getter 共享；本轮无需实现它即可验证 body。
字段解析成功不能推出完整多归档初始化、真实 getter、ClassInfo 工厂、原生对象注册或存档兼容。
