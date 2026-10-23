# QuestItem factory、构造与 Read 静态证据

日期：2026-10-20。本文件只整理已保存的本轮证据，没有重新读取原版、启动分析后端或执行测试，也没有修改生产代码。

已证的具体调用关系是：QuestItem ClassInfo 的磁盘 `+0x28` 指向工厂 `0x1801066B0`；该工厂调用导出构造 `0x180427C90`；构造在 Item 基类构造返回后写入最终 primary vptr `0x18078E058`；该表的 `+0x08` 槽指向 `0x180427E40`。这是具体对象构造和读取入口的直接证据，不再依赖类名字面含义推断。

但 **当前仍不能交付完整官方对象 Read**。运行时 ClassInfo 名称与根向下注册关系未闭合；实际构造依赖 Item/Actor 初始化；Read 必经 Item 读取、Actor::Load、字符串操作和图形服务。把这些依赖置为空操作或成功回调，会制造没有证据支持的成功路径。当前可闭合的独立行为范围，是明确以正常成功的基础构造完成为前提的 QuestItem 构造后缀；Read 部分只能确认下面的调用、分支和写入顺序，不能据此宣告其依赖已经实现。

本文所列 Game preferred VA 对应固定 x64 Game.dll：

```text
SHA256 cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2
preferred image base 0x180000000
```

地址是静态映像地址，不是已启动进程中的运行时指针。原始取证报告记录了只读打开、前后哈希一致和未加载/执行目标。哈希检查会遍历文件字节；它不能被描述为只读取了下面的 577 字节。此文档阶段只读取工程内的保存文件。

| 保存来源 | SHA256 |
| --- | --- |
| [固定导出](offline-questitem-exports/questitem-exports.json) | `53c9b253d57e25c50a577c58b5f093390d710ef76ef0585ee543b72ab4966cad` |
| [构造、Read 槽及 ClassInfo 数据](offline-questitem-boundaries/boundary.json) | `fa5ae01ef102377d8ec976607622c3aab8d1af23144d8a408e42cf15f3ec4f05` |
| [工厂边界](offline-questitem-factory-boundary/boundary.json) | `7f50d86ca63154ae11ab0d98a199e4eb316cadc08aa79685edc919149178b75b` |
| [三段逐字节核验报告](offline-questitem-core/inspection.json) | `2bb0b904a42abe435e7fcf956c444a6fe7e1a81209e3dba492f76a442e561663` |
| [工厂指令](offline-questitem-core/1066b0-instructions.json) | `7cb2615341316af1923113666af02c414e0e6b824d69d632c77f999b53f7179c` |
| [QuestItem 构造指令](offline-questitem-core/427c90-instructions.json) | `9bae1d2251fa300212a015bd47763bc37c214b2e2f3bc25e17a2d44e32590160` |
| [QuestItem Read 指令](offline-questitem-core/427e40-instructions.json) | `c3b33f63983226570596d7386e81c49f6e5d32117e86974b61b777ba5a21362d` |
| [字面量及 Item Read 边界](offline-questitem-read-dependencies/dependencies.json) | `34da4f65ac861c51fe2f64dda560543629c3652b1768e920af2213135f58abbc` |
| [Item Read 取证报告](offline-questitem-item-read/inspection.json) | `17e076619bdad87bdad191752b64c558f6f3b9cc1c333a413b334559817ad99d` |
| [Item Read 指令](offline-questitem-item-read/324c60-instructions.json) | `a600281f5ac3b70e5c5cf59964353a963e4b695c3c096ee977133f6e9642f324` |
| [七个精确导入绑定](offline-questitem-imports/import-bindings.json) | `94d695f5026e1f50a7c1a2968d6f091146db634e28618ef0565b671857aa6f5e` |

三段取证覆盖工厂 `[0x1801066B0,0x1801066DA)` 42 字节、构造 `[0x180427C90,0x180427D2C)` 156 字节、Read `[0x180427E40,0x180427FBB)` 379 字节，共 577 字节、131 条指令。各段均包含正常返回，保存段内的已显示条件跳转和循环目标均在该段内。完整的逐指令字节覆盖不等于已恢复被调用函数、异常展开或所有分配失败行为。

导出记录直接给出：

| 精确导出名字 | Game preferred VA |
| --- | --- |
| `??0QuestItem@GAME@@QEAA@XZ` | `0x180427C90` |
| `?classInfo@QuestItem@GAME@@1VRTTI_ClassInfo@2@B` | `0x180A5C150` |
| `??_7QuestItem@GAME@@6BObject@1@@` | `0x18078E058` |
| `??_7QuestItem@GAME@@6BIPhysics2@1@@` | `0x1806D5B18` |
| `??_7QuestItem@GAME@@6BICrowdAgent@CROWD@@@` | `0x18079F408` |
| `??_7QuestItem@GAME@@6B@` | `0x180771F40` |

固定 `?Read@QuestItem@GAME@@` 前缀没有命名导出匹配；Read 地址来自 Object 对应 vtable 的实际 `+0x08` 槽值。没有命名导出不能证明未覆写、继承或没有该方法。

ClassInfo `0x180A5C150` 的 48 字节磁盘内容为：

| 偏移 | 磁盘 qword | 当前能够证明的内容 |
| --- | --- | --- |
| `+00` | `0x1807A7E58` | 保存的数据指针；不能据此制造初始化行为 |
| `+08` | `0` | 通用分派器读取的名称槽在磁盘上为零 |
| `+10` | `0x180A5C8A0` | 静态 parent/type 关系候选；不证明父节点的 children 已包含当前节点 |
| `+18` | `0` | 磁盘上的 count 所在存储为零；不证明运行时 child 状态 |
| `+20` | `0` | 磁盘上的 children 指针槽为零 |
| `+28` | `0x1801066B0` | 可固定读取的 factory 目标 |

后续保存的 `classinfo_vtable` 只显示 `+00=0x180591430` 和紧接在该表 `+08` 的 qword `0x7D0000007B`。不能把第二个值当作另一个有效函数槽，也不能从一个已读函数指针推断 ClassInfo 已完成名称或 children 注册。这个表的 `+08` 邻接值与上表 ClassInfo 对象自身的 `+08` 名称槽是不同地址。

先前通用分派证据中，Engine `0x180274EC0` 从 metadata `+08` 按 C-string 比较名称，匹配后读取 `+28` 并尾跳；从 Engine 根 `0x18041CF00` 向下遍历 `+18/+20` 的 children。因而这里的 C++ 导出名字与记录内 serialized `Class="QuestItem"` 相同，仍不能代替以下静态初始化证据：

- 初始化如何使 `0x180A5C158` 指向实际 `"QuestItem"` C-string，并建立初始化后的有效 factory 槽。
- 该节点如何进入从 Engine 根可达的向下 children 链，以及到达顺序、同名节点和先行分派结果的必要前提。
- 这些初始化相对于对象创建的有效顺序和稳定性。

仅有 parent 指针不能替代向下注册关系。本文不据名字将 QuestItem 改称或映射为 ItemQuest，也不把未知注册图构造为单节点成功 fixture 后声称原生注册已闭合。

工厂在 `0x1801066BD` 把 `ECX` 设为 `0xCD0`（3280），在 `0x1801066C2` 调用 `0x1805DA6C4`。调用结果保存到栈，并在 `0x1801066CC` 作为 `RCX` 传给 `0x180427C90`。构造调用位于 `0x1801066CF`；之后没有转换、子对象地址调整或额外调用，工厂直接返回构造留下的 `RAX`。

因此 `0xCD0` 是直接观察到的请求参数及后续对象存储候选，不是所有字节初始化的证明。`0x1805DA6C4` 的分配、失败、异常及释放语义未因这个调用闭合。工厂没有对其返回值做 null 检查；不能将未知分配行为替换为正常返回 null，也不能据栈标记推定异常清理已恢复。

QuestItem 构造在 `0x180427C96` 保存传入 `this`，在 `0x180427C99` 调用 Game `0x180323690`。这是已有导出/汇编证据中的 Item 构造入口。该调用正常返回后，直到 QuestItem 构造结束不再调用任何函数；精确后缀如下：

| 指令 VA | 对传入 `this` 的写入 |
| --- | --- |
| `0x180427C9E` | byte `+C68 = 0` |
| `0x180427CA5 / 0x180427CAC` | qword `+00 = 0x18078E058` |
| `0x180427CAF / 0x180427CB6` | qword `+30 = 0x1806D5B18` |
| `0x180427CBA / 0x180427CC1` | qword `+38 = 0x18079F408` |
| `0x180427CC5 / 0x180427CCC` | qword `+530 = 0x180771F40` |
| `0x180427CD5` | qword `+C70 = 0` |
| `0x180427CDC` | qword `+C88 = 0` |
| `0x180427CE3` | qword `+C90 = 15` |
| `0x180427CEE` | byte `+C78 = 0` |
| `0x180427CF4` | qword `+C98 = 0` |
| `0x180427CFB` | qword `+CB0 = 0` |
| `0x180427D02` | qword `+CB8 = 15` |
| `0x180427D0D` | byte `+CA0 = 0` |
| `0x180427D13` | byte `+CC4 = 0` |
| `0x180427D1C` | dword `+CC8 = 0x2EE`（750） |

`+C78/+C88/+C90` 和 `+CA0/+CB0/+CB8` 分别呈现空字符串的首字节、长度和容量初值。这里没有清零其余 inline bytes、padding 或整个分配块。**这个构造后缀没有写 `+CC0`**；不能将它默认成零。后续 Read 才会显式写该字段。

`0x180427D19` 将最初的 `this` 放入 `RAX`，`0x180427D2B` 返回。最终 primary vptr 的写入位于所有基类调用之后，所以在正常返回域内，传回工厂的对象指针没有地址调整，其 `+08` 读取入口可由磁盘 vtable 槽直接连到 `0x180427E40`。这一关系并不证明基类其他状态已实现。

Item 构造与 Item Read 的首个基础调用现在有精确导入绑定：

| 来源调用点（Game VA） | IAT VA | 导入符号 |
| --- | --- | --- |
| `0x1803236B3`，Item 构造 | `0x180609700` | `Engine.dll!??0Actor@GAME@@QEAA@XZ`，`Actor::Actor()` |
| `0x180324C87`，Item Read | `0x180609710` | `Engine.dll!?Load@Actor@GAME@@UEAAXAEBVLoadTable@2@@Z`，`Actor::Load(LoadTable const&)` |

构造来源及其旧证据 SHA 保存在新 [import-bindings.json](offline-questitem-imports/import-bindings.json) 的 `source_evidence[2]`。本轮 Item Read 指令在 `[0x180324C60,0x1803250AE)` 范围保存了 1102 字节、263 条指令：保留输入 `this/table` 后首先调用 Actor::Load，之后仍有多种 table getter、字符串 helper、对象操作和字段写入。导入身份的确认不等于 Actor 构造或 Actor::Load 的实现已恢复；更不能拿 Engine Object 的简单基础构造或 base Read 返回指令代替 Actor 和 Item 的工作。

QuestItem Read 首先在 `0x180427E5B` 直接调用 `0x180324C60(this, table)`，然后才进入五项派生字段读取。下面的实际键和三个空串默认值来自 `dependencies.json` 的 `literals`，不是根据记录内容猜测：

| 顺序、查询点（Game VA） | 实际键与键 VA | 槽/默认值 | 后续写入与顺序 |
| --- | --- | --- | --- |
| 1，`0x180427E74` | `bitmap`，`0x1806B4CC4` | table `+28`；`0x18061ABC5` 的字节为 `00` | `0x180427E99` 调 `016460(&this+C78, result, C-string length)`；经过图形服务后，`0x180427EBE` 写 qword `+C70` |
| 2，`0x180427ED9` | `trailEffect`，`0x1806B4EB8` | table `+28`；`0x1806163AD` 的字节为 `00` | 条件调用 `078DA0`；`0x180427EF0` 写 qword `+C98`；非零结果再进入条件效果分支 |
| 3，`0x180427F4B` | `requiredQuestFile`，`0x1806B4E78` | table `+28`；`0x18061ABC6` 的字节为 `00` | `0x180427F6C` 调 `016460(&this+CA0, result, C-string length)` |
| 4，`0x180427F81` | `requiredTaskUID`，`0x1806B4E68` | table `+38`；`R8D=0` | `0x180427F84` 将返回的原始 `EAX` 写到 dword `+CC0` |
| 5，`0x180427F9B` | `showQuestItemTag`，`0x1806B4EA0` | table `+58`；`R8D=0` | `0x180427FAD` 将返回的原始 `AL` 写到 byte `+CC4` |

三个默认指针的字节内容都已确认为空串，但地址身份不同；不应因此把全部查询调用折叠成同一个占位。最后两个槽的默认参数和返回写入宽度已经观察到，其确切 getter 实现、类型转换和异常边界仍须由相应槽/函数证据支持，不能仅凭键名补写类型语义。

第一项 `bitmap` 和第三项 `requiredQuestFile` 的查询结果会直接被解引用并扫描到 NUL，没有先检查 null；已知空串与 null 必须区分。`016460` 的复制/赋值、容量、别名和失败语义不能仅凭调用参数视为已经恢复。

`bitmap` 的服务链在第一段字符串处理后必经：

| 调用/读取点（Game VA） | 精确导入或数据 | 观察到的关系 |
| --- | --- | --- |
| `0x180427E9E`、`0x180427EA5` | `[0x180609050]`，`Engine.dll!?gEngine@GAME@@3PEAVEngine@1@EA` | 先得到导入数据地址，再读取其中的引擎指针 |
| `0x180427EA8` | `[0x180609440]`，`Engine::GetGraphicsEngine()` | 将引擎指针作为 `this` 调用 |
| `0x180427EB8` | `[0x180609470]`，`GraphicsEngine::LoadTexture(std::string const&)` | 以上一次返回值作为 `this`，传入 `&object+C78` |
| `0x180427EBE` | 对象 qword `+C70` | 无条件保存 LoadTexture 返回值 |

即使 `bitmap` 的查询结果为空串，代码也只把扫描长度置零，再调用 `016460`，随后继续 GetGraphicsEngine 和 LoadTexture；这里没有跳过图形服务的空串分支。导入证据没有读取 `gEngine` 的实际数据、GraphicsEngine 对象或纹理资源，也没有恢复纹理加载、缓存、引用计数或失败行为。不能伪造引擎实例、返回任意纹理 token，或让 LoadTexture 空操作成功来完成官方样本。

`trailEffect` 才有可缺省的条件路径。第二查询返回后，`0x180427EDF` 先置 `RAX=0`：

1. 结果为 null，或结果首字节为零：跳过 `0x180078DA0`，于 `0x180427EF0` 写 `+C98=0`。
2. 非 null 且非空：`0x180427EEB` 调用 `0x180078DA0`，随后保存其返回值到 `+C98`。调用点明确设置 `RDX` 为查询结果；不应为其他参数寄存器发明未证的语义。
3. `+C98` 为零时直接到 `0x180427F37`，不执行下面三个调用；非零时才进入：
   - `0x180427EFF` 经 `[0x180609A58]` 调 `Engine.dll!EffectEntity::StopEmitting()`，`RCX` 为保存到 `this+C98` 的效果指针值。
   - `0x180427F08` 调 `0x180591990`，其返回值进入 `R8`。
   - `0x180427F31` 调对象 vtable `+168`，`RCX=this`、`RDX=[this+C98]`、`R8=591990` 返回值；`R9` 来自导入数据地址 `[0x180609AE8]`，符号为 `Engine.dll!?empty@Names@GAME@@2VName@2@B`。四个栈参数位置 `+20/+28/+30/+38` 仅明确写了零 byte，不能扩展成全宽初始化的证明。

这证明缺省/空 `trailEffect` 可以跳过该条件分支，不证明其他非空值可用占位对象代替。`078DA0`、`591990`、对象 `+168` 的语义和 `Names::empty` 的对象状态尚未闭合。现有记录里某字段是否实际缺失，还需按已证 getter 的查找、类型和默认规则判断；不能只凭希望避开依赖而选择这个分支。

Read 的顺序为：

```text
Item Read / Actor::Load 及其他基础工作
→ bitmap 查询、C-string 扫描、调用 016460(&this+C78, …)
→ GetGraphicsEngine → LoadTexture → 写 C70
→ trailEffect 查询 → 可选 078DA0 → 写 C98
→ 条件 StopEmitting / 591990 / 对象 vslot+168
→ requiredQuestFile 查询、扫描、调用 016460(&this+CA0, …)
→ requiredTaskUID 查询 → 写 CC0
→ showQuestItemTag 查询 → 写 CC4
→ 返回
```

中途失败不能回写成“对象未改变”：Item/Actor 的基础工作可能已经发生，字符串、纹理结果、效果结果以及后续字段分别在不同位置完成写入。对于 LoadTexture 返回 null，当前段只证明该值被保存，不能自动推定异常或整体失败；对于返回/操作未被证实的 helper，也不能反向推定一定成功或没有副作用。

当前明确边界为：

- 可证明 factory 的请求参数、构造调用和正常返回转发；分配与异常清理仍是外部缺口。
- 可在 Item 基础构造正常完成、有效存储和 ABI 保持的前提下，精确投影 QuestItem 构造后缀的写集合及返回身份。这不等于完整原生构造已实现。
- 可证明该后缀最终 vptr 与 Read 槽的连接，并记录完整派生 Read 段的控制流。
- 无法用当前证据实现 Item/Actor 初始化与读取、图形服务、所有字符串 helper、条件效果调用及未解析 getter 的完整行为。
- ClassInfo 的运行时名称、children 注册和根分派仍缺证据；匹配的 C++ 名字不能代替 serialized 注册关系。

因此不得将 Item Read、Actor::Actor、Actor::Load、GetGraphicsEngine 或 LoadTexture 置为成功占位来通过官方对象读取。以前的官方原始记录、parser、getter 或 cache 验证不应被重命名为本轮的完整对象工厂/Read 验证。本文件没有增加成功断言、场景或正式样本兼容结论，也没有恢复原始源码、执行原游戏或触及玩家存档。
