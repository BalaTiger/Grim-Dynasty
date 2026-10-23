# ClassInfo 运行时注册调查

日期：2026-10-20。状态：**证据调查完成，运行时注册尚未闭合**。本文只整理工程内已经保存的取证结果，没有在文档阶段读取原件、执行 dumpbin/Rea/Java、运行测试或修改生产代码。

本轮确认了三个 ClassInfo 磁盘记录、一个实际虚表槽和两个具名 getter。两个 getter 只返回现有 ClassInfo 地址，虚表槽呈现清理形态；它们都不是已经找到的名称或 children 初始化者。`name +08`、向下 `children +18/+20` 的写入者、注册顺序及根可达关系仍未知。本轮没有注册产品实现，也没有完成官方记录 `Class="QuestItem"` 到真实工厂的实际分派验证。

下文所有 VA 均为对应模块的 preferred VA，基址为 `0x180000000`，不是运行中的指针。固定原件身份为：

| 模块 | 文件字节数 | SHA256 |
| --- | ---: | --- |
| Game.dll | 11385856 | `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2` |
| Engine.dll | 4485632 | `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153` |

五批新增证据的报告及三份指令记录如下；本文编写时已重新核对这些工程文件的 SHA256：

| 保存文件 | SHA256 |
| --- | --- |
| [导出查询](offline-class-registration-exports/discovery.json) | `0aa3b2f40baef67b2e60e572d472733ba40be7a156155ccde814e477b1f46a58` |
| [ClassInfo 定点数据及槽边界](offline-class-registration-metadata/metadata.json) | `54aa510cffe5a281c54e6de4a594f40224585822f0b40cad6c847f8318ef76b7` |
| [ClassInfo 槽取证报告](offline-classinfo-slot/inspection.json) | `38a045003583c3d0ac9a68fb80a0ce43285365c8cea33b9c511d6e0f057dad9e` |
| [Game 591430 指令](offline-classinfo-slot/591430-instructions.json) | `909c6fc39c03eeba69d617bc14400f09a1c92ccedbf9e6ea11312a3acabe698f` |
| [两个 getter 的边界调查](offline-class-registration-getter-boundaries/boundaries.json) | `3fe20de3382e33e69ca512d9f54fb22a01ff3e85ea53b6789aa1a116f6622531` |
| [两个 getter 取证报告](offline-classinfo-getters/inspection.json) | `ff0361a23abdadee5dd466630f26007794058d9f1abe7a5a0655ba039faf1d75` |
| [Game 051810 指令](offline-classinfo-getters/51810-instructions.json) | `c1bb39e790e9cdf43038745205d17613f2c53291cece02b8d3926fa80b2a06ad` |
| [Game 029A50 指令](offline-classinfo-getters/29a50-instructions.json) | `1fa933f28771ab99abfbd86cbd16059f51d3075541d1c233525b40d103251cc1` |

本轮引用的先前证据也按文件字节核对，且不计入本轮新增读取量：

| 保存文件 | SHA256 |
| --- | --- |
| [QuestItem ClassInfo 磁盘记录](offline-questitem-boundaries/boundary.json) | `fa5ae01ef102377d8ec976607622c3aab8d1af23144d8a408e42cf15f3ec4f05` |
| [QuestItem 关联数据及 ClassInfo 虚表邻接值](offline-questitem-read-dependencies/dependencies.json) | `34da4f65ac861c51fe2f64dda560543629c3652b1768e920af2213135f58abbc` |
| [QuestItem 工厂及 Read 已有结论](QUESTITEM-FACTORY-READ-EVIDENCE.md) | `a194d5501f869de06994aa2e633c6e09f9b823f6eb60c2dae9ede9120ce75bf5` |
| [Engine 工厂分派汇编](rea-native-registration/09-Class-factory-helper-0x180274ec0-assembly.json) | `bfa5f091e67fe9ee863c639d10d8cdf2424d0f564278baaaa37661d661794790` |
| [Engine CreatePartial 调用顺序](rea-native-registration/04-ObjectManager-CreatePartial-order-assembly.json) | `7a32e57757bc0cb2d336cd63ade820108fc1f3babb767c8150db54b5ad277d98` |
| [旧初始化导出定位](../phase-1/round-4/initialization-locations.json) | `bdc0e1838162463d18fce89dfea20fe6baf073d889db757e62c59ae3ed8faf1e` |
| [旧 Game 初始化入口汇编](../phase-1/round-4/Game-function-evidence-bounded-v2.json) | `1e8421fc6b83004fd39259ca19ba97faa7df520cf49200572cecd8a2bb80c502` |

新增取证量按各报告记录的显式读取累计如下。相同地址被再次读取仍计入读取量；这是累计读取量，不是唯一地址覆盖量。

| 批次 | 显式 metadata/data 字节 | 代码字节 / 指令 | dumpbin 调用 | 原件整文件哈希遍历 |
| --- | ---: | ---: | ---: | --- |
| 导出查询 | Game 8718 + Engine 6340 = 15058 | 0 / 0 | 0 | Game 2，Engine 2 |
| ClassInfo 定点数据及槽边界 | Game 224 + Engine 112 = 336 | 0 / 0 | 0 | Game 2，Engine 2 |
| ClassInfo 槽正文 | 12 | 76 / 21 | 1 | Game 2 |
| getter 边界调查 | 276 | 0 / 0 | 0 | Game 2 |
| getter 正文 | 0 | 16 / 4 | 2 | Game 2 |
| 合计 | **15682** | **92 / 25** | **3** | **14 次：Game 10，Engine 4** |

336 字节的 ClassInfo 批次内已包含 168 字节定点数据：三个 48 字节记录和三次 8 字节虚表槽读取；其余 168 字节为 `.pdata` 查询。Actor/Object 的虚表槽地址相同，但报告分别读取并计数。168 不能再加到 15682 上。导出查询的 ASCII 名称读取已包含在各自的 `metadata_read_bytes` 中，同样不重复相加。

14 次原件 SHA256 检查使用 64 KiB 缓冲遍历完整文件，**全文件哈希字节不计入上表语义读取量**；因此不能把 92 字节或 15682 字节描述成原件全部 I/O。已有 dumpbin 的 `/RANGE` 限制输出区间，其内部 metadata I/O 没有被仪表化；上表也不是 dumpbin 实际总读取量。两个正文批次分别记录工具哈希前后一致，工具哈希不计入 14 次原件遍历。工具 SHA256 为 `cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815`。

五批报告均保留原件哈希前后一致、未加载/执行目标的记录。固定导出查询不遍历整个名称表；固定边界查询不扫描整个异常表；没有新建整 DLL 副本。只读打开及前后哈希/stat 检查不等于原子快照或独占 `FileShare.Read` 保证。本文没有重新执行这些批次。

导出查询的 `files[].searches` 在 Game 和 Engine 两个模块对以下精确前缀均未选中具名导出：

```text
??0RTTI_ClassInfo@GAME@@
??1RTTI_ClassInfo@GAME@@
?Register@RTTI_ClassInfo@GAME@@
??__E?classInfo@QuestItem@GAME@@
```

这仅是在报告所声明的 PE 名称排序与固定查询合同下的阴性结果；不证明未导出函数、内联代码、静态初始化、其他名称的注册入口或相关类不存在。查询中出现 `Register` 这个前缀也不是已有此方法的证据，不能据此建立或命名产品接口。

选中的五个导出在 `files[].candidates` 中保留完整名字和地址：

| 模块 | 精确导出 | preferred VA |
| --- | --- | --- |
| Game | `?classInfo@Item@GAME@@1VRTTI_ClassInfo@2@B` | `0x180A5C8A0` |
| Game | `?GetRTTIClassInfo@QuestItem@GAME@@UEBAAEBVRTTI_ClassInfo@2@XZ` | `0x180051810` |
| Game | `?GetRTTIClassInfo@Item@GAME@@UEBAAEBVRTTI_ClassInfo@2@XZ` | `0x180029A50` |
| Engine | `?classInfo@Actor@GAME@@1VRTTI_ClassInfo@2@B` | `0x18041CFC0` |
| Engine | `?classInfo@Object@GAME@@1VRTTI_ClassInfo@2@B` | `0x18041CF00` |

与先前已保存的 QuestItem 记录对照，磁盘字段为：

| 模块、记录地址 | `+00` | `+08` name | `+10` parent | `+18` count 存储 / `+20` children | `+28` factory |
| --- | --- | --- | --- | --- | --- |
| Game QuestItem `0x180A5C150`，先前证据 | `0x1807A7E58` | `0` | `0x180A5C8A0` | `0 / 0` | `0x1801066B0` |
| Game Item `0x180A5C8A0`，本轮 | `0x1807A7E58` | `0` | `0` | `0 / 0` | `0x180104E00` |
| Engine Actor `0x18041CFC0`，本轮 | `0x180348830` | `0` | `0x18041CF30` | `0 / 0` | `0x180049810` |
| Engine Object `0x18041CF00`，本轮 | `0x180348830` | `0` | `0` | `0 / 0` | `0x18004A0D0` |

表中 parent 是槽位用途的简称；磁盘值本身不证明相应父节点的 children 已包含当前节点。Item 的 `+10=0` 尤其**不能证明已经发生或必将发生某次跨 DLL 动态赋值**，也不能代替建立者、赋值目标和时序证据。Actor 的非零 parent 仅记录地址，不能仅凭相邻布局把 `0x18041CF30` 命名为某类。磁盘上的 name/count/children 为零也不是可直接拿来成功分派的运行时状态。

Game 记录的首个虚表槽为 `0x180591430`；Engine 记录的首槽为 `0x180274E30`，本轮未读取后者正文。先前 `classinfo_vtable` 保存的 Game 表 `0x1807A7E58+08` 邻接 qword 为 `0x7D0000007B`，不能当作下一个有效函数指针；它与 ClassInfo 对象自身 `+08` 名称槽是两个不同地址。

Game `[0x180591430,0x18059147C)` 的 76 字节、21 条指令完整覆盖了所取区间，正常路径如下：

| 指令位置 | 直接观察 |
| --- | --- |
| `0x18059143A`、`0x180591444` | 取得 `0x1807A7E58`，写到 `this+00` |
| `0x180591449`、`0x18059144D` | 读 `this+08`，将该值作为 `RCX` 调用 `[0x18060B888]` |
| `0x180591453`、`0x180591457` | 读 `this+20`，将该值作为 `RCX` 调用 `0x1805DA5DC` |
| `0x18059145C`、`0x18059145F` | 检查原始 `EDX` 保存值的低位 `flags & 1` |
| `0x180591461` 至 `0x180591469` | 该位非零时设置 `EDX=0x30`、`RCX=this`，再调用 `0x1805DA5DC` |
| `0x18059146E`、`0x18059147B` | 返回最初的 `this` |

这是清理/释放形态，不是 name/children 的建立流程。直接正文仅写回 `+00`，没有为 `+08/+10/+18/+20/+28` 建立内容。IAT 与释放 helper 的完整实现、失败行为和异常清理没有在本轮展开；不能由这一形态反推出相应分配或注册算法，更不能把它当 ClassInfo 构造来运行或投影成成功注册。

两个 getter 的 `.pdata` 查询均没有精确 `BeginAddress` 项，且前项范围不包含目标；该批只记录 `leaf-candidate-no-exact-unwind-entry`，未选取相邻正文，也未据空结果推定函数长度。后续固定入口各读取 8 字节，以区间内实际 `RET` 收束：

```text
Game 0x180051810: 48 8D 05 39 A9 A0 00  LEA RAX,[0x180A5C150]
Game 0x180051817: C3                    RET

Game 0x180029A50: 48 8D 05 49 2E A3 00  LEA RAX,[0x180A5C8A0]
Game 0x180029A57: C3                    RET
```

两段均没有条件分支、调用或内存写入，直接返回固定 ClassInfo 地址；它们没有懒初始化，也没有建立运行时 name、parent、children 或 factory。这一结果只确认两个 getter 的行为，不证明其返回记录已经完成注册。

已有 Engine `0x180274EC0` 分派汇编仍决定“最终选中”的含义：

1. `0x180274ED4` 取当前节点 `+08`，`0x180274EE1/0x180274EE5` 按原始字节精确比较；匹配 NUL 时完成，未见大小写或编码转换。
2. 当前节点匹配时，`0x180274EFC` 取 `+28`，`0x180274F0F` 尾跳。该工厂即使返回零，也不会再搜索这个匹配节点自己的 children。
3. 当前节点不匹配时，`0x180274F12/0x180274F15` 判断 signed count；非正数返回零，正数按 `+20` 数组顺序递归。
4. `0x180274F30/0x180274F33` 在递归结果非零时立即返回；零结果使父节点继续下一兄弟。共享节点可再次被访问，不能全局去重来改变回调次数。

因此“第一个名字匹配候选”不等于“最终成功返回的工厂对象”。先行匹配节点的工厂必须真正产生相应返回结果；回调副作用可能已经发生，不能跳过、猜测为零或回滚成未执行。只有确定实际注册图、遍历顺序、名字内容、已到达工厂及其结果，才能确定官方查询的最终分派。CreatePartial 的 `0x18017E1B2` 使用 Engine 根 `0x18041CF00`，在 `0x18017E1BC` 调分派；单独构造一个名称为 QuestItem 的自有节点不能替代这条根向下链的证据。

已有 QuestItem 工厂到构造、最终 vptr 和 Read 的连接继续有效，但它位于“已经选中并调用该工厂”之后。具名 C++ 导出和 getter 地址不证明 serialized `Class="QuestItem"` 已通过真实根图到达它，也不解除已有 Item/Actor、图形服务及其他 Read 依赖缺口。

相关旧初始化索引提供真实自由函数 `InitializeObjects = Game 0x18036B240` 和 `ShutdownObjects = Game 0x18036B270`，并记录主程序普通导入；导入存在不证明运行顺序。旧 `Game-function-evidence-bounded-v2.json` 的 `label="InitializeObjects"` 已保存入口指令：

```text
0x18036B244 → CALL [0x18060B578]
0x18036B24A → ECX=0x37868
0x18036B24F → CALL 0x1805DA6C4
0x18036B257 → CALL GameEngine constructor 0x1802C07A0
0x18036B25F → 写全局 0x180A66288
0x18036B26A → JMP 0x1802D7230
```

该入口实际创建全局 GameEngine，不能仅据 `InitializeObjects` 名字当成简单 ClassInfo registry。入口的实际尾跳是 `0x1802D7230`；展开伪代码中出现的 `0x180590300` 或 InstanceGroupManager 操作不等于入口直接调用，更不是已证的 QuestItem name/children 写入。旧证据同时警告 `.pdata` 的入口区域不等于展开伪代码覆盖的完整语义函数。本轮没有重读这些旧入口，也没有追踪整个 GameEngine 构造或尾跳目标。

当前必须保留的缺口是：

- 谁写入 ClassInfo 的运行时 `name +08`，具体字节、存储和有效期如何建立。
- 谁建立 Item 的实际 parent 关系，谁把 QuestItem/Item 等节点加入根可达的 children 数组，以及分配、插入顺序、重复来源和生命周期。
- 根图何时稳定；先行同名节点及其工厂结果如何影响此次查询，失败后的已执行前缀如何保留。
- 实际工厂返回及后续对象初始化/Read 的完整行为。

这些缺口不能用磁盘零值、任意手工注册图、成功回调或“第一个候选”替代。本文对应的注册调查止于有来源的导出、固定数据、实际虚表槽和两个 getter；不追未关联的 `.pdata` 邻居，不展开全 GameEngine，不新增注册产品实现或注册成功断言，也不形成官方 Class 实际分派兼容结论。其他独立模块的实现及验收不由本文报告或否定。
