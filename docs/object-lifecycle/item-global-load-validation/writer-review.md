# 2C8750两字段writer独立静审

只读本轮已归档的完整Game2C8750正文及指定旧缓存；本审阅代理没有读取原版、运行reader/构建/测试或修改实现。正文由主代理单次读取：pdata10475为[2C8750,2D41A1)，47697B、10886条指令、1904个call、715个branch，唯一显式RET在2D41A0。报告逐字节覆盖成功、decode issue为0、工具退出0、原件前后固定SHA相同、stat稳定、handle关闭，failures为空。这证明文件正文已取得，不证明1904个callee正常返回或整个数据库初始化已恢复。

## 两次字段写入的准确局部合同

| Game RVA | 实际操作 |
| --- | --- |
| 2CEC27 | 从当前RSI表重新读vptr到RAX。 |
| 2CEC2A | MOVSS XMM15,[7A80B8]，读取第一默认值的原始DWORD。 |
| 2CEC33..3E | XMM2=XMM15；RDX=696378；RCX=RSI。 |
| 2CEC41 | CALL [RAX+48]。 |
| **2CEC44** | **MOVSS DWORD[R13+1B58],XMM0**，raw `f3410f1185581b0000`。 |
| 2CEC4D | 再从同一RSI身份重新读当前vptr到RAX。 |
| 2CEC50 | MOVSS XMM2,[7A80C8]，此时才读取第二默认值DWORD。 |
| 2CEC58..5F | RDX=696360；RCX=RSI。 |
| 2CEC62 | 再CALL当前vptr的+48。 |
| **2CEC65** | **MOVSS DWORD[R13+1B5C],XMM0**，raw `f3410f11855c1b0000`。 |

两个store直接复制XMM0低32位，没有在调用后测试成功标志、做浮点算术或将NaN等位模式替换为默认。每次返回前，调用、返回原始位和对象可写性都仍是必要前提。第一store完成后第二调用未正常返回时，第一store已发生；不能回滚、提前写第二字段或预采样第二默认值/vptr。callee可能经其它引用改变own状态，其效果不能被“只读getter”名字自动排除。

局部终点宜明确为**2CEC6E**，不是LoadFromDatabase返回：随后2CEC7E调用同表+38，2CEC81以DWORD写原this+1B60。此前构造在+1B5C使用QWORD写过的高半部，在这里有单独后续writer。若只覆盖两字段，不执行或猜测这第三调用；保留relative8..11而不再写零。全文只有上述三处直接出现+1B58/+1B5C/+1B60的操作数；这不是对间接别名或callee写集合的排除证明。

## RSI来自另一记录，不是入口R14表

入口2C8815调用IAT609A90后，2C881B把返回RAX保存到R14（另存[rbp+1F0]）。后面也曾在2C8C0A把另一返回值存RSI；它随后被多次改用，不能作为当前writer的表来源。

本段真实链为：

1. 2CA3FD读R14的vptr，2CA400设R8=61812F，2CA407设RDX=694728，2CA40E设RCX=R14，2CA411调用+28字符串槽，得到**另一记录名的来源指针**。此时尚未核固定literal内容，不能在此文档猜记录名或key文本。
2. 2CA414把局部[rbp+3A0]字符串capacity设15，2CA41F长度设RSI，2CA426首byte设SIL。此时RSI来自2CA340的XOR ESI,ESI，之后直到此处没有重赋；之前子对象删除/错误分支汇合回2CA3A8。长度/首byte因此是0。R15在2C8A21设为全1，随后这段扫描把R8从-1递增，并以SIL=0检查C字符串；正常可读NUL终止来源是必要前提。
3. 2CA449 RDX=来源指针，2CA44C RCX=&局部字符串，2CA453 CALL016460，实际构造该局部记录名；字符串分配、alias/增长及失败边界不能略去。
4. 2CA459 CALL IAT609098，2CA45F把结果作为RCX；2CA462 RDX=&同一局部字符串，**2CA469 CALL IAT609090**。返回RAX没有被此caller测试或保存。
5. **2CA46F再次CALL IAT609098**，2CA475设RCX为这次返回值，2CA478传同一局部字符串，**2CA47F CALL IAT609A90，2CA485 RSI=RAX**。后续到两个字段writer之间没有重赋RSI；正常callee遵守非易失寄存器约定是此身份追踪的条件。

现有module-boundaries索引中，Game导入Engine的symbol[18]/[19]/[338]分别对应609090 `ObjectManager::LoadTableFile`、609098 `Singleton<ObjectManager>::Get`、609A90 `ObjectManager::GetLoadTable`；精确修饰名为：

```text
?LoadTableFile@ObjectManager@GAME@@QEAAPEBVLoadTable@2@AEBV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@@Z
?Get@?$Singleton@VObjectManager@GAME@@@GAME@@SAPEAVObjectManager@2@XZ
?GetLoadTable@ObjectManager@GAME@@QEBAAEBVLoadTable@2@AEBV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@@Z
```

这只是旧索引的身份候选，原始导入绑定由主代理另作固定核验。特别是RSI来自第二个GetLoadTable引用返回，不能直接假设与第一次LoadTableFile返回相同；两次Singleton Get亦是实际独立调用。第一次返回null在本caller没有“跳过字段”分支。RSI之后首次显式解引用在2CA5D2，远早于2CEC27；不允许把null表虚构成合法空表或依靠默认参数掩盖该前提。

## R13身份：中途借用，之后恢复原this

2C8750先把入口RCX写到入口RSP+8。随后7次push共38h，2C8760令RBP=入口RSP−2B18h，因此**[RBP+2B20h]正是入口RSP+8**。

2C87E3先以RCX初始化R13；2C98EE令RAX=R13+374E0，2C98F5暂存到[rbp+2B38]，2C992F再把这个子对象指针加载到R13，供后续局部循环。**2C9AFD从[rbp+2B20]恢复R13为入口保存的RCX**。从入口保存到该恢复点，没有另一个显式写入[rbp+2B20]或等价[rsp+2C20]；2B38是另一个被重复使用的scratch槽，不能混同。

恢复后直到两个writer和函数剩余正文，未见R13/r13d/r13w/r13b再赋值；结尾2D4199 POP R13才恢复调用者寄存器。故两个目标store以原GameEngine this为基址，不以临时this+374E0为基址。此结论要求正常可读栈及callee正常遵守非易失寄存器/栈合同，不宣称异常展开或任意内存破坏下仍成立。

## 控制流和失败前缀

对保存完整正文中的直接branch做定点核对：没有从2CEC44之前直接跳到2CEC6E或更后以跳过两次store的边；没有区间外的直接branch跳入[2CA485,2CEC6E)；在两store之前没有显式RET或间接JMP。局部2CEC27..2CEC65之间没有条件跳转。此前有大量循环、调用和局部条件路径；只有它们实际完成并到达局部窗口，才执行这里的两次调用/写入。上述控制流事实不能证明循环终止、调用成功、无异常或表生命周期有效。

适当的局部模型应把“已经到达2CEC27、同一有效table身份、当前vptr/slot与两个返回原始位”作为明确边界；未恢复此前链时不能把从入口直接运行两个store称为整个LoadFromDatabase完成。第一个调用尚无正常返回观察时，两条直接store尚未执行，但不能据此否认该call可能已有副作用；第一个store后第二调用未知时，第一store已发生，后续观察到的call效果也不能回滚。正常返回但结果位未知时，应停在未执行store前，或明确将执行过的目标DWORD标未知，不能留下旧构造常量冒充新值。

## 指定旧缓存能支持的包装关系

本轮只查rea-object-base的函数文件/plan/summary、record-load相关plan/summary与文档、record-interface exports和module-boundaries分类候选。rea-object-base只有Object ctor3A6B0、SetObjectName3A780、TableDepot::LoadFile1C4D20；record-load-dependencies只有name27B3C0、lookup3C8E0、miss1C4E20。该有限范围未找到ObjectManager::LoadTableFile/GetLoadTable的独立entry/body，不能把它们等同1C4D20。

旧TableDepot合同可以在wrapper实际证明连接后复用：命中返回node+30同一身份（已知null也可命中）；miss有数据时构造LoadTableBinary并调用解析槽，缺数据正常分支经诊断后构造LoadTable。当前opaque缓存payload不能自动判为Binary。下一最小依赖是这三个固定导入/实际Engine入口、所需wrapper关系、实际+48槽及固定key/default字节；无需重新拆解已经闭合的TableDepot命中路径，也不能先假设浮点getter等于+28字符串getter。

## 固定证据SHA256

| 相对路径 | SHA256 |
| --- | --- |
| docs/object-lifecycle/offline-item-global-load-body/inspection.json | 26a01be264dbad6706bdc6e8553d37ba8811f7cf523eda93e0e5d21d0a9f33a8 |
| docs/object-lifecycle/offline-item-global-load-body/instructions.json | cee579cfa26e049167595ac55792993201e6268bd459474da6689f648e382262 |
| 上述inspection.exact_sha256（固定47697B正文） | b3f481a409a68dde7d65601d3beeeea085d958e5b1879cfc7a3dd41963b5f661 |
| docs/phase-1/boundary-audit/module-boundaries.json | d1ec79916baeb45b3257f7c7c2ce356c262f804ae9b2577888a0d14de53e7ba7 |
| docs/object-lifecycle/rea-object-base/plan.json | 8b6421039b191246c80e86d3aa1a230cf96103f988e23d91ab757b3b6fd39c4b |
| docs/object-lifecycle/rea-object-base/summary.json | ba3cc82b9e55930e8b74b9999074b6bbf96a11ead5b96c00ab03b7cb05884f8f |
| docs/object-lifecycle/rea-record-load-dependencies/plan.json | 37a2b2648eddb7c2fa4aac656577ba583cf0f99d7b0ae0346e20dc06e508d88b |
| docs/object-lifecycle/rea-record-load-dependencies/summary.json | 72face55a5a8beb18db2d10092b5c6ac17200f4bd7ffa05715bee9ae6a96d7f2 |
| docs/object-lifecycle/rea-object-base/11-TableDepot-LoadFile-0x1801c4d20-assembly.json | 09c5e996bbe2d23b35ad7de140e77940899602a35a0209bc5f070ee8bbc2b166 |
| docs/object-lifecycle/rea-record-load-dependencies/11-LoadFile-miss-0x1801c4e20-assembly.json | d696e7b2141e5fe9ee6f518555d32a615ab174deabd6f676a15c299be1b7e3b1 |
| docs/object-lifecycle/record-interface-discovery/record-interface-exports.json | 33d7527e64565cc4e56a31b3665b41e004ad9e53f93739e3a645f205cf0b1603 |
| docs/object-lifecycle/record-interface-discovery/record-interface-vslots.json | d2b77d66e08c05c83822f9f03d66565e18d016cdca400377853c866c7c3a4b9c |
