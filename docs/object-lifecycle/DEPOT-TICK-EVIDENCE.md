# Depot 初始状态与 LoadFile tick 转换证据

2026-10-20。本页只汇总已保存的指令、导入绑定、源码和验收记录；编写时没有新增原版读取、反汇编、后端或测试执行。下列 VA 均属于既有 Engine 映像，SHA256 为 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`。原版前后哈希一致是所链接取证记录的结果，本页没有重新计算原版哈希。

## 初始 depot：正常完成构造后的逻辑状态

[TableDepot 构造器](offline-depot-ctors/1c4ad0-instructions.json) 的完整范围为 `0x1801C4AD0..0x1801C4B7B`（末端不含），171 字节；[取证记录](offline-depot-ctors/inspection.json) 保存逐字节覆盖与哈希核验。[ObjectManager 构造器](offline-depot-ctors/17dbf0-instructions.json) 在 `0x18017DD0A` 形成 `this+0x170`，于 `0x18017DD11` 调用该 depot 构造器。

令 depot 基址为 `D`，缓存容器基址为 `C=D+8`。已观察到的顺序为：

1. `0x1801C4AF7` 写 depot vptr `0x1803438F0`，`0x1801C4B03` 调用 `0x18003C740(C)`。
2. 准备 `D+0x68` 的门控成员；`D+0x78` 先清零，`0x1801C4B30` 请求 `0x28` 字节，保存返回指针后于 `0x1801C4B3C` 调用 IAT `0x180302318`。既有 [Engine 有界证据](../phase-1/round-4/Engine-function-evidence-bounded-v2.json) 将同一槽标为 `InitializeCriticalSection`，[导入清单](../phase-1/boundary-audit/module-boundaries.json) 列出 `KERNEL32.dll!InitializeCriticalSection`；这是复用已有槽符号关联，未做新的该槽 PE 绑定或 API 执行。
3. `0x1801C4B43` 在 `D+0x50` 写 qword 零，覆盖 load/hit 两个 DWORD；`0x1801C4B47` 清 `D+0x58` 的 DWORD。
4. `0x1801C4B4D` 请求 `0x30` 字节，`0x1801C4B5A` 调用 `0x18013C1D0`，`0x1801C4B60` 把返回值存到 `D+0x48`。
5. `0x1801C4B64` 在 `D+0x5C` 写 qword 零，覆盖 elapsed-total/max 两个 DWORD，然后返回。此清零在前述构造调用之后，不能推广到构造失败路径。

[03C740 指令](offline-archive-container-init/03c740-instructions.json) 及其 [逐字节取证记录](offline-archive-container-init/inspection.json) 补齐了缓存初态：

| 指令 VA | 已证写入或调用 |
| --- | --- |
| `0x18003C76D / 0x18003C770` | `C+8` 的 sentinel 指针、`C+0x10` 的 qword 节点数均先清零 |
| `0x18003C774..0x18003C779` | `RDX=0,R8=0`，调用 `0x18001F170` |
| `0x18003C77E` | 将返回的 sentinel 存到 `C+8` |
| `0x18003C781 / 785 / 789` | 将 endpoint vector 的 begin/end/capacity 指针 `C+0x18/+0x20/+0x28` 清零 |
| `0x18003C78D` | 写 binary32 位型 `0x3F800000`，即 load factor `1.0` |
| `0x18003C793..0x18003C799` | 调用 `0x18001B4F0(C,8)` |

[01F170 的已保存完整 body](offline-cache-node-inspection/01f170-instructions.json) 在 `0x18001F17A` 请求 `0x38` 字节；参数 `RDX=0` 时，`0x18001F18F/192` 选新节点自身为两个链接值，`0x18001F19A/1A6` 写 `next=prev=self`。这证明正常非空分配的 sentinel 自链接，不能据此声称 payload/padding 清零或分配失败安全返回。

[01B4F0 汇编](rea-registration-containers/11-Container-whole-chain-followup-0x18001b4f0-assembly.json)（evidence ID `ev_ce058af65993a657ec2262c46d9dddb99a0da13719d96c19f5ee9f5e76bd1073`）在 `0x18001B501` 计算 `2*8` 个端点槽，于 `0x18001B53C` 调 [01BFB0 reserve](rea-container-support/11-Container-support-0x18001bfb0-assembly.json)，于 `0x18001B56C` 调 [01BD30 fill](rea-container-support/08-Container-support-0x18001bd30-assembly.json)。已保存的 [022F00 分配包装](rea-container-reserve/05-Container-reserve-0x180022f00-assembly.json)、[021740 copy](rea-container-fill/05-Container-fill-0x180021740-assembly.json) 与 [01EF70 fill](rea-container-fill/08-Container-fill-0x18001ef70-assembly.json) 支持 fresh null-vector 的正常成功路径：分配 16 个 qword 端点槽、全部填 sentinel，end/capacity 均为 begin+128。`0x18001B57A/57E` 最后写 `C+0x30=7`（mask）、`C+0x38=8`（bucket count）。模型的 bucket capacity 因而为 8，不能把 16 个端点槽误称为 16 桶。

[RecordDepotInitialization.CreateFresh](../../src/task_items/RecordDepotInitialization.cs) 据此返回独立自有状态：空 8 桶缓存、容量 8、load factor 1.0，以及 load/hit/total/max 均为 0。门控参数必须显式提供。它是“分配、门控初始化及被调用构造器正常完成”合同下的逻辑投影，不创建原生 vptr、指针、critical section 或 `D+0x48` 对象，也没有把未建模的 `D+0x58` 暴露为已恢复功能。

## 两种采样转换及精确整数语义

主要 caller 为 [1C4E20 汇编](rea-record-load-dependencies/11-LoadFile-miss-0x1801c4e20-assembly.json)，evidence ID `ev_23bf6a83dc20e5e716f1454ed05f04ab266b5e67430c35070017de7c68eb1937`。[固定双槽 PE 记录](offline-load-ticks-imports/tick-import-bindings.json) 保留 descriptor、OFT/name 字节与文件偏移，并确认：

| 分支 | 导入槽与绑定 | start / end 调用 VA |
| --- | --- | --- |
| QPC | `0x180302208` → `KERNEL32.dll!QueryPerformanceCounter` | `0x1801C4E4D` / `0x1801C510D` |
| millis | `0x180302788` → `WINMM.dll!timeGetTime` | `0x1801C4E73` / `0x1801C5130` |

该绑定只读取 PE 元数据，没有执行计时 API、读取 BSS 值或证明运行时初始化。

QPC 分支将显式输出槽 `sample`、base、frequency 都按 int64 位型输入，执行：

```text
delta    = wrap64(sample - base)
dividend = wrap64(delta * 1000)
quotient = signed_divide_toward_zero(dividend, frequency)
tick32   = low32(quotient)
```

`0x1801C4E57/5E/65/67` 分别为 SUB、低 64 位 IMUL、CQO、IDIV，end 的对应指令为 `0x1801C5117/1E/25/27`。不能替换成浮点、先除后乘或任意精度乘法。`frequency=0`、以及截断后的 `dividend=INT64_MIN` 且 `frequency=-1` 是两类 IDIV fault；例如 `sample=±2^60,base=0` 乘 1000 都会 wrap 到 `INT64_MIN`。

两个 QPC 调用后都直接加载输出槽，没有检查返回 BOOL。因此 BOOL=false 但槽位已独立确定时，仍按该位型转换；BOOL=true 也不能为未观测槽补值。自有 [RecordLoadTicks](../../src/task_items/RecordLoadTicks.cs) 对未知槽先返回 `Unsupported/UnknownQpcOutputSlot`，不会虚构零或抢先判定除零；两种已知 IDIV fault 返回 `DivideFault` 且无 tick。这是显式诊断结果，不是原生异常返回值、恢复策略或 fallback 行为。

millis 分支只消费 EAX：`tick32=wrap32(rawTimeGetTime-base32)`，SUB 位于 `0x1801C4E79` 与 `0x1801C5136`。未见将高位解释为负数后夹到零的分支。

start 在 `0x1801C4E40`、end 在 `0x1801C5100` 各自重读模式 flag；base/divisor 也分别读取，没有将两次模式或参数锁成同一快照的指令合同。纯 API 因而允许各次显式选择不同模式。即使模式与上下文相同，也必须分别转换后相减：`base=0,frequency=3,start=1,end=3` 得 `333` 与 `1000`，elapsed 为 `667`，把原始差值一次转换只得 `666`。

## 发布、计数及 elapsed 写序

[LoadFile wrapper](rea-object-base/11-TableDepot-LoadFile-0x1801c4d20-assembly.json) 在 `0x1801C4D97` 查询缓存；命中于 `0x1801C4DB9` 增加 DWORD `D+0x54`，取出同一 table，不进入 miss 计时。未命中才在 `0x1801C4DAF` 调 `1C4E20`；已有 wrapper 门控覆盖这段调用，不额外合并 archive/table 或对象注册的门控。

miss 的正常 Binary 分支完成解析、节点发布和索引维护后，`0x1801C50DA` 从 `02E770` 返回；临时名称若仍持 heap，则在 `0x1801C50F7` 调 `023090`。后续严格顺序为：

| VA | 写入 |
| --- | --- |
| `0x1801C50FC` | `LoadCount50=wrap32(LoadCount50+1)` |
| `0x1801C5100..0x1801C5136` | 重新选择模式，采集并转换 end |
| `0x1801C513C` | `elapsed=wrap32(endTick32-startTick32)` |
| `0x1801C513F` | `ElapsedTotal5C=wrap32(ElapsedTotal5C+elapsed)` |
| `0x1801C5143/47/49` | unsigned CMP/JBE；仅 elapsed 大于旧值才写 `ElapsedMaximum60=elapsed` |
| `0x1801C514D` | 返回已有 table |

总计会 wrap；最大值是单次 uint elapsed 的无符号最大值，不是 wrap 后总计的最大值。正常计时只影响这些统计字段，不参与 table/key 的选择，但 end fault 发生在发布和 load++ 之后，不能描述为“整个操作没有效果”。

[自有 miss 接合](../../src/task_items/RecordLoadFromSuppliedBinary.cs) 在真实 ARZ miss 读取前才调用显式 start delegate，cache 和 LoadCount 已提交后才调用 end delegate；source/attempt 构造、命中与同一 attempt 重放不重新采样。`StartTickConversion/EndTickConversion` 保存返回对象。start 未知、DivideFault 或 delegate 抛错在归档 I/O 之前停止；end 对应失败保留已完成 table/cache 与 LoadCount，elapsed 未写，depot 留在 `RequiresResolution`。这些 pending/once/异常诊断是自有 API 安全边界，不声称原生采用相同异常传播、回滚或重试规则。[捕获时钟测试](../../src/task_items/RecordLoadCapturedClockTests.cs) 对两端的三类失败、采样位置和闭源命中分别验收。

## 未恢复的四个全局 writer 与独立 Stopwatch

现有证据只确认 caller 读取，尚未取得以下四个值的初始化 writer、调用顺序或失败策略：

| 全局 VA | caller 使用方式 |
| --- | --- |
| `0x18041C8B0` | byte 模式 flag |
| `0x1804217B0` | QPC int64 base |
| `0x1804217A0` | QPC int64 divisor/frequency |
| `0x1804227AC` | millis DWORD base |

导入清单含 `QueryPerformanceFrequency`，不证明它如何写上述 divisor，也不证明 mode/base 的默认值。静态文件中的值不能替代运行时初始化；本页不推荐猜地址或扫描寻找 writer。

[已执行的自有 probe 源码](../../tools/static_analysis/ProbeOfficialFreshCache.cs) 显式检查 `Stopwatch.IsHighResolution` 和正频率，自取 `Stopwatch.Frequency` 与基准 timestamp，采样 delegate 每次另取 timestamp 后调用纯转换器。它没有读取或写入这四个 Engine globals，也没有恢复原生 clock init。传给转换器的已知槽与成功标记属于自有 Stopwatch 适配合同，不是观测到原生 QPC BOOL。

[已保存 probe](official-fresh-cache-validation/probe.json) 的首次成功 miss 记录两次独立采样：frequency `10000000`、base `4025782857415`，start raw `4025782922826` → `6`，end raw `4025783682708` → `82`，elapsed `76`。采样时 cache/load 分别为 `0/0` 与 `1/1`；随后的两次同表命中不再采样。第三次采样属于另一个 fresh depot 对已关闭 source 的失败 miss，不能计作首次成功 miss 的额外 end。这个耗时不是原生游戏对照值、性能承诺或硬资源上界。

## 已保存验收及范围

[统一自有回归日志](native-init-tick-validation/test-output.txt) 已通过 **2264 断言 / 83 groups**，另含既有 ItemQuestDemo；[运行摘要](native-init-tick-validation/test-summary.json) 记录一次完成、`/checked+`、用时 `12.8111331` 秒。包括纯 tick `50/2`、fresh depot `38/4`、captured clock `58/7`；这些是总体的子集，不另行相加。[程序集检查](native-init-tick-validation/assembly-references.json) 以 PEReader 读元数据通过，没有 Engine/Game/Steam 程序集引用。

单条真实 ARZ 数据的自有 fresh-cache probe 另有 **123 断言**，原档前后 SHA256 均为 `9e3532250cdb611a4cc989997c8f97fcec2d6c422063a845e6c43cd538c30603`；这些来自已保存记录，不并入自有合成回归计数。它验证读取、解码、解析、Class getter、首次发布和后续同 table 命中，没有运行原版游戏、DLL、原生时钟或原生对象工厂，也不是 native differential execution。

`D+0x48` 的 `13C1D0` 内部状态、原生 allocator/OOM、critical-section 失败、native EH/unwind、一般 fallback 及完整 factory/Read 不由本页恢复。选中的正常 Binary 路径不读取 `D+0x48`，可将该构造视为正常完成的外部前提；不能把它伪造成已知空表。miss 的可见 fallback `0x1801C5009..0x1801C5046` 还包含计数、诊断及新的普通表构造，不等同于返回该成员；默认未供应 miss 仍保留拒绝边界。

主要证据文件的 SHA256：

| 文件 | SHA256 |
| --- | --- |
| `offline-depot-ctors/1c4ad0-instructions.json` | `444df3e2546b1e31b22d676c2d75e5be81ead66249d681c5d5e321892bb5f03c` |
| `offline-archive-container-init/03c740-instructions.json` | `d65e5c255b54001645a97b3b244beb9e608b8f6353be6b694a087797949507e3` |
| `offline-cache-node-inspection/01f170-instructions.json` | `d6ee428d3e56c754b9ba28c127c75f98ec382a55ccd2453d3d88e3a1e6b0c0e1` |
| `rea-registration-containers/11-Container-whole-chain-followup-0x18001b4f0-assembly.json` | `1529d7985f5678abfcb62119eceb01f23758eef0bcf53ed51487961f6f31d644` |
| `rea-record-load-dependencies/11-LoadFile-miss-0x1801c4e20-assembly.json` | `d696e7b2141e5fe9ee6f518555d32a615ab174deabd6f676a15c299be1b7e3b1` |
| `offline-load-ticks-imports/tick-import-bindings.json` | `eb1e49dfe763e207be743b91d03b2b4c066057063da052cb3e1e3a97807aab0e` |
