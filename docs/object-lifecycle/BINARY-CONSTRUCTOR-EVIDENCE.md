# Binary 表构造器：初始化顺序与未闭合依赖

2026-10-20。本页只读取已保存的查询结果，不读取原版、不启动后端、不修改行为源码或运行测试。证据绑定 Engine SHA256 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`；下列地址均为 preferred VA。`0x18013F300` 的完整 caller 汇编已经取得，但其两个直接 callee 尚未在本批取得 body，不能据此宣称构造器及所有初始状态已完整复现。

## 证据与查询关闭

| 归档 | Evidence ID | 文件 SHA256 |
| --- | --- | --- |
| [04 伪码](rea-binary-table-constructor/04-LoadTableBinary-constructor-0x18013f300-pseudocode.json) | `ev_ed7603a09587af45cab170fe4afc3f2b4c5b6d8c03b3738c6975faf9b99dd048` | `b69d34604178304f4f1d6b8a44f7676a9394bde597a5626bace0e5b100620e0e` |
| [05 汇编](rea-binary-table-constructor/05-LoadTableBinary-constructor-0x18013f300-assembly.json) | `ev_b1ad5be04fcc0fbf21d70590d288cbd2145ca6dd127d0be510fe7cd3f850f96f` | `bb3fa2e3f2bca620f66caca1894932b970d69761f63327e23982f4eea158ccb0` |
| [06 直接 callee](rea-binary-table-constructor/06-LoadTableBinary-constructor-0x18013f300-direct-callees.json) | `ev_773a4bada1a62767662e0449795dfbecb4725e71f6877a500f1d2ba8bbda4ff0` | `9399a24efa3888b9d7191141b2850e00a7345d6a38c9d906cab6e9cc8c04e4bc` |

以下均依据 `result.structuredContent.normalized_result`，以指令操作数为准。直接 callee 清单为 `13C1D0`、`0481F0`，此函数的 call relationship coverage 标记 complete；这不表示已分析这两个被调函数。

[汇总](rea-binary-table-constructor/summary.json)记录固定 **8/8 calls passed**，从 `02:20:57.004Z` 到 `02:22:22.365Z`，共 **85.361 秒**，首个分析请求耗时 83.304 秒。Engine 前后哈希一致，`targets_launched=false`；close 已确认、最终 session 已关闭、进程退出码 0、未强制终止且无错误。配置 Java heap 为 512M，44 次两秒采样中的最低空闲内存为 **12.523 GiB**，未触发 5 GiB 停止阈值。采样不是系统内存预留或未来峰值保证。[host 关闭复核](rea-binary-table-constructor/resource-release.json)于 `02:24:07.3643954Z` 确认所属 Rea PID 3360 与 Java 均不再存活、runtime 文件为空，空闲内存 **13.490 GiB**。

## 直接可见的写入顺序

入口 `RCX` 为原对象地址，`RDX` 为 archive 参数；分别保存在 `RSI`、`RDI`。下表的“清零”均指该指令当时的写入，不把辅助调用前的值擅自提升为返回时不变式。

| 顺序／指令地址 | 明确操作及宽度 |
| --- | --- |
| `13F320` | 以原 `this` 调用基类构造 `0x18013C1D0`。其返回值不用于替换保存的 `this`。基类字段、分配及其他副作用仍取决于该 body。 |
| `13F326..32D` | `LEA RAX,341FE0`，随后向 `[this+0]` 写 **qword**。这是 Binary 虚表地址的真实写指令，不再只是导出名称关联。 |
| `13F330..352` | `XOR EBP,EBP` 后，依序向 `+30/+38/+40/+48/+50/+58/+60/+68/+70` 写 **九个 qword 零**。 |
| `13F356..36A` | `RBX=this+78`；向 `+78/+80/+88` 写 **三个 qword 零**。另在 `RSP+58` 先写 qword `this+78`，再只把该栈槽低 **dword** 写零，高 32 位仍保留先前地址位。 |
| `13F36E..378` | 设置 `R8=&[RSP+58]`、`EDX=0`、`RCX=this+78`，调用 `0x1800481F0`。尚不知它读取栈槽的宽度、对对象/子对象的修改、分配或失败行为。 |
| `13F37D..39C` | 调用返回后依序清零：`+90` **qword**，`+A0/+A8` **qword**，`+B0` **dword**，`+B8/+C0` **qword**。 |
| `13F3A3` | 向 `+C8` 写 **qword**，值为保存的原 archive 参数。没有据此分配或复制 archive。 |
| `13F3AA..3C0` | 最后才向 `+98` 写 **qword 零**；`RAX=RSI`，恢复寄存器并返回原 `this`。 |

伪码中的 `in_R9` 和额外 `uVar2` 参数不能作为 helper ABI 证据：caller 没有在该调用前显式设置 R9；`RSP+20=-2` 是可见栈写入，不足以证明它是第五个业务参数。栈临时值也不是“一个 qword 零参数”。现阶段只保留上述真实寄存器、内存操作与调用点。

## 状态边界及后续 parser

`+B4..+B7` 没有本函数的直接写入，`+08..+2F` 也没有本函数逐字段初始化证据；不能将这些字节自动当零、确定的 padding 或入口值不变。基类 ctor 与 `0481F0` 都可能有其他写入。尤其 `+78/+80/+88` 的清零发生在 helper 之前，不能据此宣布对应容器最终为空；同样不能对其前面的全部字段作未经验证的调用后保持保证。helper 后明确覆盖的字段可按正常返回路径记录，但不构成整块 `0xD0` 内存清零、原生分配/EH 或失败回滚的恢复。

`table+0xC8` 保存的是 **DatabaseArchive 指针**，不是字符串池。既有 Binary getter 使用 archive 中的共享池；`archive+0xC8` 与 `table+0xC8` 属于不同对象的字段，不能混同，也不能由本构造器生成解析后的名称池、字段 buffer 或缓存 population。

[旧 LoadFile miss 汇编](rea-record-load-dependencies/11-LoadFile-miss-0x1801c4e20-assembly.json)在 `1C4FC8` 分配 `0xD0`，`1C4FE2` 调本 ctor，随后 `1C4FF4` 调返回对象实际 vptr 的 **`+0x08`** 槽，传入已初始化的临时 reader。此次查询确认了 `341FE0` 的直接写入；其后仍有未分析 helper，完整返回状态须结合 callee 补证。导出表 `341FE0` 的候选 parser 槽地址为 **`0x180341FE8`**，但该 qword 的值仍未保存，不能把槽地址当作函数入口。现有 [槽记录](record-interface-discovery/record-interface-vslots.json)只读过 Binary `+0x28 → 13FB60`，不能替代 parser 槽。

下一步需分别补齐实际槽值/目标身份以及 `13C1D0`、`0481F0` 的必要行为；之后再为确认的 parser 准备固定范围查询。本页没有读取该槽、猜 parser 地址、追查新 callee 或启动下一批。表的 parser 与对象 FinishPartial 最后的派生 Read 是不同调用；本构造器证据不证明任一解析结果或真实对象 Read 已恢复。
