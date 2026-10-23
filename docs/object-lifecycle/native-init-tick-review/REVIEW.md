# LoadFile tick 转换：离线证据复核

本页仅复核已有工程归档与源码。未读取原版或 DLL 副本，未运行 dumpbin、Rea、Java、游戏、时钟 API 或测试；未修改生产代码和现有状态文档。

## 已有证据足以恢复转换算术，尚不足以恢复初始化

主证据为 [1C4E20 完整汇编](../rea-record-load-dependencies/11-LoadFile-miss-0x1801c4e20-assembly.json)，evidence ID `ev_23bf6a83dc20e5e716f1454ed05f04ab266b5e67430c35070017de7c68eb1937`，文件 SHA256 `d696e7b2141e5fe9ee6f518555d32a615ab174deabd6f676a15c299be1b7e3b1`。其 subject 是 Engine SHA256 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`；本轮没有重新读取 Engine。

| 分支 | 开始采样 | 结束采样 | 指令给出的合同 |
| --- | --- | --- | --- |
| `byte[18041C8B0] != 0` | `1C4E49..1C4E6E`，`1C4E4D CALL [180302208]` | `1C5109..1C512E`，`1C510D CALL [180302208]` | RCX 指向栈上 8 字节输出。读取该输出，减 `qword[1804217B0]`，低 64 位 `IMUL 1000`，`CQO` 后对 `qword[1804217A0]` 做有符号 `IDIV`。最终只使用商的低 32 位。 |
| flag 为零 | `1C4E73 CALL [180302788]`，至 `1C4E7F` | `1C5130 CALL [180302788]`，至 `1C5136` | 无显式参数；取返回 EAX，减 `dword[1804227AC]`，按 32 位保留结果。 |

高精度分支的准确顺序是：`d = wrap64(sample - base)`；`n = wrap64(d * 1000)`；把 `n` 解释为 signed int64，再做 signed int64 除法（向零截断）；取商低 32 位。不能用浮点、先除后乘或任意精度乘法替代指令中的低 64 位乘法。

开始和结束分别在 `1C4E40`、`1C5100` 读取 flag；base 和 divisor 也分别读取。调用者没有快照或相等检查，因此“两次模式和全局值保持稳定”只能是明确的覆盖前提。即使模式稳定，也不能把两次转换之差一般化为一次对差值的转换：例如正常正数 `base=0, frequency=3, start=1, end=3`，前者是 `1000-333=667`，后者是 `trunc(2000/3)=666`。

## 导入身份及尚缺证据

[现有伪码](../rea-record-load-dependencies/10-LoadFile-miss-0x1801c4e20-pseudocode.json) 将两个调用分别标为 `QueryPerformanceCounter`、`timeGetTime`。文件 SHA256 为 `7e552f06145e50d4848d47a2c119d823c5d3a3be77abfc1d7b63ff50d750ac6f`。

[旧普通 PE 导入清单](../../phase-1/boundary-audit/module-boundaries.json) 的同 SHA Engine 项确认导入 `KERNEL32.dll!QueryPerformanceCounter`、`KERNEL32.dll!QueryPerformanceFrequency` 和 `WINMM.dll!timeGetTime`；该清单 SHA256 为 `d1ec79916baeb45b3257f7c7c2ce356c262f804ae9b2577888a0d14de53e7ba7`。清单没有保留 IAT 槽地址，故它与伪码共同支持名称判断，但不是两槽原始字节绑定记录。

最小下一补证仅为对 **IAT VA `0x180302208` 与 `0x180302788`** 做定点普通 PE import 解析，保留 descriptor、OriginalFirstThunk、名称与文件偏移；无需新反汇编转换函数，因为算术完整内联于现有 caller。该建议本轮未执行。

在已检查的工程文档/body 库存中，`41C8B0 / 4217A0 / 4217B0 / 4227AC` 只找到本 caller 的读取，未找到初始化 writer body。`QueryPerformanceFrequency` 出现在导入清单不能证明它如何设置 divisor、如何选择模式或如何处理初始化失败。若要恢复 native 初始化，下一目标应是这四个精确 global 的写入来源及其调用顺序；目前没有有证据的 writer 函数地址，不应猜地址或顺带扩查其他函数。原文件静态初值也不能替代运行后的初始化值。

## 回绕、失败与状态顺序

`1C50FC` 先增 DWORD `depot+50`。结束采样和转换完成后，`1C513C SUB EAX,R12D` 得到 modulo-2^32 elapsed；`1C513F` 将 elapsed 按 DWORD 回绕累加到 `+5C`；`1C5143..149` 用无符号 `JBE` 比较并更新 `+60` 最大值；最后 `1C514D` 返回已经构造的 Table。

这条路径没有用 elapsed 决定 Table、解析结果、key、cache 节点或 fallback。它仍改变可观察统计字段，而且结束采样位于 cache 发布、临时名称清理和 `+50` 增量之后；不能称为“失败时无状态影响”。[外层 wrapper](../rea-object-base/11-TableDepot-LoadFile-0x1801c4d20-assembly.json) 的 hit 路径只增 `+54`，不进入 miss 计时段。该 wrapper SHA256 为 `09c5e996bbe2d23b35ad7de140e77940899602a35a0209bc5f070ee8bbc2b166`。

两个 QPC 调用之后均不测试返回 BOOL；紧接着读取栈输出。若外部调用未提供有效输出，caller 没有已证的零值或 timeGetTime fallback，栈槽还可能未初始化或已被其他局部用途复用。正常成功外部 API 合同可作为覆盖前提；失败不能伪造成功 tick。

`IDIV` 的 divisor 为零、或 signed `INT64_MIN / -1`，没有本地保护分支；除法故障和异常展开不在本页恢复范围。SUB/IMUL 的溢出标志也没有被检查，正常返回时保留其低位结果。现有 evidence 不能证明 native EH、无修改失败或自动切换时钟。

## 当前自有实现与可行最小边界

[SuppliedBinaryRecord.cs](../../../src/task_items/SuppliedBinaryRecord.cs) 的 `ConvertedStartTick32/ConvertedEndTick32` 仍是显式 `uint?` 输入，并没有原始时钟采样或上述 int64 转换。[RecordLoadFromSuppliedBinary.cs](../../../src/task_items/RecordLoadFromSuppliedBinary.cs) 在读取/解析前拒绝未知 start；未知 end 则停在 cache 已安装、`LoadCount50` 已增加之后，保留 pending 和检查用前缀，不重复执行。已知值按 unchecked uint 相减，再由 [RecordLoadFile.cs](../../../src/task_items/RecordLoadFile.cs) 累加 `+5C`、更新 unsigned `+60`。本模块没有计时 float 累加。

因此可以首先独立实现并测试“明确提供原始 sample、模式、base、divisor”的算术转换原语，覆盖整数回绕与截断；这不要求新函数 body，也不等于 native 时钟初始化或采样已经恢复。接入实际自有时钟前仍须界定外部 API 成功合同和自有初始化策略，不能把 base=0、模拟 ticks 或任意模式默认值包装成已恢复的 native 状态。若保持 native 初始化目标，则继续停在上述四个 globals 的初始化缺口。

现有注入测试只覆盖转换后 uint 值，包括未知 start/end、计数及 elapsed 回绕、高位无符号最大值和 cache hit；本轮未重跑，亦未作原版运行对照。
