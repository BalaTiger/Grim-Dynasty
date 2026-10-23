# Binary 子对象初始化：零 count 分支与 parser 调用目标

2026-10-20。本页仅整合已保存的静态证据；编写时没有启动 Rea/Ghidra、读取原版文件、修改行为源码或运行构建。证据绑定 Engine SHA256 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`。下列地址均为该映像的 preferred VA，不是运行时观测地址。

结论：在 `0x18013F300` 已证的 `RDX=0` 调用条件下，`0x1800481F0` 只清零子对象的三个 qword，然后直接返回，不调用任何 callee、不读取 R8 指向的数据。结合构造器及已保存槽值，可以确认正常构造返回路径上的 `0x1801C4FF4` 虚调用目标为 **`0x18013F510`**。这项结论不等于 parser 行为或完整 LoadFile miss 已恢复。

## 新结果的来源和完整性

下表读取各文件的 `result.structuredContent.normalized_result`；分支、宽度和操作数以汇编为准。

| 归档 | Evidence ID | 文件 SHA256 |
| --- | --- | --- |
| [04 伪码](rea-binary-subobject-profile/04-Binary-subobject-init-0x1800481f0-pseudocode.json) | `ev_d02a6cccef9b02389097db816ac72acfacaa8c90c49e29eeee5456040a6b4e16` | `12e3f685ed9c95ae058adccc4d5ab452f1b9db76c7adbfecb5d7655e58631fb0` |
| [05 汇编](rea-binary-subobject-profile/05-Binary-subobject-init-0x1800481f0-assembly.json) | `ev_64fbbaeeb1b22b11236cb867925d7ca125b3258be2913d9e639dcbcfd431da60` | `72bc6f31bb56ea665fe88581e822cfa15f4ef6427060cd838ece42d74ca020ee` |
| [06 直接 callees](rea-binary-subobject-profile/06-Binary-subobject-init-0x1800481f0-direct-callees.json) | `ev_5786490406b52cac37404e5ab257be779d0e2ef19d90989ae76f44da521cf463` | `4889f1210a8f15e2a5c0e97ba0be2a6465319835d13b2e8860fcdac8171969c1` |

调用链与槽绑定采用以下既有文件：

| 依据 | Evidence ID／记录性质 | 文件 SHA256 |
| --- | --- | --- |
| [13F300 构造器汇编](rea-binary-table-constructor/05-LoadTableBinary-constructor-0x18013f300-assembly.json) | `ev_b1ad5be04fcc0fbf21d70590d288cbd2145ca6dd127d0be510fe7cd3f850f96f` | `bb3fa2e3f2bca620f66caca1894932b970d69761f63327e23982f4eea158ccb0` |
| [parser 槽记录](binary-parser-discovery/parser-slot.json) | 独立静态 PE 槽读取记录，无 Rea evidence ID | `156f5c8bcb13cf8dd6b6c783d6caebbcc0c234c7e741b0716311a5a1ecda66f8` |
| [LoadFile miss 汇编](rea-record-load-dependencies/11-LoadFile-miss-0x1801c4e20-assembly.json) | `ev_23bf6a83dc20e5e716f1454ed05f04ab266b5e67430c35070017de7c68eb1937` | `d696e7b2141e5fe9ee6f518555d32a615ab174deabd6f676a15c299be1b7e3b1` |

## Caller 给出的精确条件

`13F320` 先调用 `0x18013C1D0` 基类构造；在其正常返回后，`13F326/13F32D` 把 `0x180341FE0` 写入原对象的主 vptr。随后：

1. `13F356` 设置 `RBX=this+0x78`。
2. `13F35A` 将该地址作为 qword 写入栈槽 `[RSP+0x58]`；`13F35F` 只将其低 DWORD 清零。高 32 位仍来自原地址，不能把整个槽解释为 qword 零。
3. `13F363/366/36A` 先清零对象 `+78/+80/+88` 各 qword。
4. `13F36E` 令 `R8=&[RSP+0x58]`；`13F373` 执行 `XOR EDX,EDX`，按 x64 寄存器语义使完整 **RDX=0**；`13F375` 令 `RCX=this+0x78`。
5. `13F378` 调用 `0x1800481F0`。

本结论只需要上述真实参数。伪码的额外参数推测、未设置的 R9 或 caller 的其他栈槽不用于扩充 helper ABI。

## RDX=0 下可达的指令、读宽度与写入

| 地址 | 操作 | 该调用条件下的含义 |
| --- | --- | --- |
| `0481FF` | `XOR EAX,EAX` | 完整 RAX 清零。 |
| `048201` | `MOV RSI,R8` | 仅复制 64 位地址值；没有访问该地址指向的数据。 |
| `048204` | `MOV qword ptr [RCX],RAX` | 写 parent `+0x78` 的 8 字节零。 |
| `048207` | `MOV RDI,RDX` | 保存零 count，不读取其指向的数据。 |
| `04820A` | `MOV qword ptr [RCX+0x8],RAX` | 写 parent `+0x80` 的 8 字节零。 |
| `04820E` | `MOV RBX,RCX` | 保存子对象地址。 |
| `048211` | `MOV qword ptr [RCX+0x10],RAX` | 写 parent `+0x88` 的 8 字节零。 |
| `048215/048218` | `TEST RDX,RDX`；`JZ 04826F` | 按完整 64 位 count 判断，直接跳过非零分支。 |
| `04826F..04827E` | 恢复 RBX、RSI、栈及 RDI，`RET` | 从 helper 自有栈恢复寄存器并返回，没有后续对象访问或调用。 |

因此，此分支中 R8 指向的栈 local **没有任何解引用，既没有 DWORD 读取，也没有 QWORD 读取**。local 高 32 位的残留值不影响执行。除 helper 自有栈的保存／恢复外，对调用方对象的写入严格限于 `+78/+80/+88`；没有读取旧子对象字段，也没有访问 parent `+0`。

直接 callee 清单覆盖整个函数，不能据此断言该次调用执行了所有依赖。以下调用在 `RDX=0` 分支均不可达：

- `048230` 的错误处理 import `[0x180302560]`，清单标为 `EXTERNAL:0x100`。
- `04823F → 0x180022F00`，非零 count 的分配路径。
- `048262 → 0x1800483A0`，非零 count 的填充路径。

该次调用无需假定这些 callee 的内存写入或分配结果，也无需为证明 vptr 保持而追查 `0483A0`。构造器之前已清零的三项被 helper 再次清零后返回，因此其最终零值在此正常路径上也已明确。

## 正常返回域与实际虚调用绑定

推导前提是：基类构造已经正常返回；原对象及调用栈具有有效、相互不重叠的存储，相关指针加法没有溢出；没有并发或外部异步改写，且沿已保存的正常指令路径执行。这不覆盖无效存储、硬件异常、native EH 或失败回滚。

在上述域内：

1. `13F32D` 写入主 vptr `341FE0`。
2. 其后的唯一 helper `0481F0` 在已证零 count 分支没有 callee，且不写 parent `+0`。返回时保留 caller 使用的非易失寄存器。
3. `13F37D..13F3AA` 只写 parent 的 `+90/+A0/+A8/+B0/+B8/+C0/+C8/+98`，不再修改主 vptr；`13F3B1` 返回原 `this`。
4. miss caller 的 `1C4FE2` 调该构造器；`1C4FE7` 保存返回对象，`1C4FEA` 读其主 vptr，`1C4FED/1C4FF1` 传入临时 reader 与对象，`1C4FF4` 调 `[vptr+8]`。
5. 已保存槽记录将 `0x180341FE8` 映射到 `.rdata`、文件偏移 `0x340FE8`，8 字节为 **`10f5138001000000`**，按小端 qword 解码为 **`0x18013F510`**。该目标映射到同一固定 Engine 的可执行 `.text` 区。

所以，本分支的调用边现可绑定为：

`LoadFile miss 0x1801C4FF4 → [0x180341FE0+8] → 0x18013F510`

这是基于多份静态证据的调用目标推导，不是运行原版得到的轨迹。槽地址 `341FE8` 与调用目标 `13F510` 必须区分；该绑定也不自动赋予目标已证的函数名称、完整函数边界、记录协议或解析成功语义。

原始槽报告的 `actual_load_file_target_proven=false` 和“后置 helper 未分析”描述保留为当时状态，不改写原始记录。本页新增的零 count 分支证据解除了这一条后置 helper 保持性限制。

## 尚未覆盖及下一最小目标

- **非零 count 不在本页闭合域。** 已见函数会检查 `0x3FFFFFFFFFFFFFFF` 上限，并走分配、填充及可能的错误路径；其完整语义仍依赖 `022F00` 在元素宽度 4 下的契约、`0483A0` 的实际读取／写入以及异常行为。不能把零 count 分支结论推广为通用 vector 初始化器。
- **基类 `0x18013C1D0` 仍未恢复。** 它在显式 Binary vptr 写入之前执行，因此在正常返回前提下不再阻断本页的目标绑定；但 gate、基类字段、资源及完整构造成功／失败行为仍需该函数证据。
- **parser `0x18013F510` 的行为未知。** 下一最小补证目标是这个已绑定地址的伪码、汇编和直接 callees；取得函数体后再判断可以恢复的记录格式和有意义实现范围，本页不猜协议。
- **完整 LoadFile miss 尚未闭合。** 还包含 parser、取数和缓存发布等边界；不能把构造后的零字段转成 parsed table，也不能用 supplied callback 或空 snapshot 代替解析。对象 FinishPartial 的派生 Read 是另一条独立调用。

本页没有新增行为源码或断言，没有执行上述后续目标，也没有修改原始证据、查询计划、runner 或其他文件。
