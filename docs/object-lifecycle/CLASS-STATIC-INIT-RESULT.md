# Item / QuestItem 静态注册写入源

2026-10-20。本轮已定位两个真实初始化入口并补齐其直接可见写入顺序。**未建立完整运行时注册图，未新增注册产品实现。** 原有 Item 标量初始化段保持原样，仍不是完整构造器。

主对话流中断后，已检查现存输出并恢复暂停的并行核对；没有重跑已完成的取证任务或测试。各次固定检查所需的哈希与边界复核读取均有记录。本轮静态调查已结束，不留运行中的后端或安装动作。

## 已定位的入口和数据

Game 的 PE 入口经 reason 1 分支到达 CRT 初始化路径。精确导入绑定确认 `_initterm` 使用表区间 `[0x18060BAB8, 0x18060C878)`。440 个槽中有一个零项，439 个不同非零直接目标；431 个有精确展开片段边界，8 个没有。后者未补读邻居。

按表顺序检查了 224 个直接目标的精确片段，定位两个写入者后停止。其余目标没有继续展开；未对其他物品类建立语义模型，也没有整库扫描。

| 项目 | Item | QuestItem |
|---|---|---|
| 零基表槽 | 115 | 205 |
| 表槽 preferred VA | `0x18060BE50` | `0x18060C120` |
| 直接入口 | `0x180005EF0` | `0x180009E10` |
| 本次拼接的三个展开片段 | `[5EF0,5F29)`、`[5F29,5F9F)`、`[5F9F,5FB0)` | `[9E10,9E3B)`、`[9E3B,9EAF)`、`[9EAF,9EC0)` |
| ClassInfo | Game `0x180A5C8A0` | Game `0x180A5C150` |
| 已读名称字节（含 NUL） | `49 74 65 6d 00` | `51 75 65 73 74 49 74 65 6d 00` |
| 名称字面量地址 | Game `0x18066A34C` | Game `0x18066A9C8` |

这确认了同一 Game 表中的相对顺序，不能证明全部前置初始化成功或运行时图已就绪。

IAT `Game 0x180609EB0` 精确绑定 Engine 导出的 Actor ClassInfo；`Game 0x18060BA38` 精确绑定 CRT `_strdup`。没有执行这些导入。

此前文件数据已保存 factory 槽：Item `+28 → Game 0x180104E00`，QuestItem `+28 → Game 0x1801066B0`；QuestItem 文件中的 parent 指向 Item。此次两个入口的直接指令没有写 factory 槽。**这是文件绑定与直接写入的区别，不是运行时 creator 已成功注册的证明。**

## 直接可见的写入顺序

Item 在 `5F04` 先把 Actor 导入数据地址写到自身 `+10`，随后调用 `_strdup("Item")`，重新读取 parent，在 `5F18` 将返回值写到自身 `+08`。

QuestItem 调用 `_strdup("QuestItem")`，重新读取自身 `+10`，在 `9E2A` 将返回值写到自身 `+08`。这两段都没有检查名称返回值是否为零。

parent 为零时直接进入末尾转交。parent 非零时，两个入口执行同形的可见序列：

1. 读取 parent 的 signed 32-bit count，先在寄存器中执行 32-bit `+1`，符号扩展后做 unsigned 64-bit `×8`；乘法溢出时将请求值改为 `UINT64_MAX`。调用 `Game 5DAF40`，本轮未展开该 helper。
2. 保存返回指针；当当前 parent count 为正时，按原顺序逐项复制旧 children 指针。循环会重读 count 和旧数组地址，不能擅自替换成不可变快照。
3. 用再次读取的 count 作为下标，把自身 ClassInfo 指针写入新区域。
4. 增加 parent 的 count。
5. 读取 parent 的旧 children 指针，调用 `Game 5DA5DC`。
6. helper 返回后，才将新指针写入 parent 的 `+20`。
7. 最后转交 `Game 5DAEB8`，参数分别为 `Game 604450`、`Game 605530`。本轮未跟随该 helper 或两个参数指向的代码。

| 写入/调用 | Item VA 尾部 | QuestItem VA 尾部 |
|---|---|---|
| 追加自身指针 | `5F86` | `9E96` |
| parent count 增加 | `5F8A` | `9E9A` |
| 旧 children 指针交给 helper | `5F91` | `9EA1` |
| 发布新 children 指针 | `5F96` | `9EA6` |
| 最后转交 | `5FAB` | `9EBB` |

可见路径没有字符串比较或重复节点检查。名称写入早于 children 更新；count 增加早于新指针发布。helper 的异常、空返回、内存访问故障或末尾转交结果均未恢复；可见路径没有回滚这些前缀写入，因此不能实现为“失败则完全不变”的原子注册。

## 尚未实现的部分及最小下一步

**当前状态是证据/实现缺口，不是批准被拒、批准待处理或执行结果不明。**

- 动作：实现真实注册状态转换并判定失败结果。目标：三个已知 helper `5DAF40`、`5DA5DC`、`5DAEB8` 及必要的直接依赖。原因：本轮只恢复调用和写入顺序，未证明它们的分配、旧区域处理、末尾注册及异常语义。下一步可先检查工程内已有证据，若缺失，再为这些明确入口单独做边界查询与定点静态读取；无需重读本轮两个 writer。
- 动作：从真实 Engine root 验证官方 `Class="QuestItem"` 的可达查询。目标：Engine root `0x18041CF00` 到 Actor/Item 的实际注册关系和 children 顺序。原因：两个 Game writer 无法证明完整根图或所有前置初始化的结果。下一步应只定位该链上必要写入源，保持其他物品类未展开。
- 动作：完整实例创建与 Read。目标：既有 Item/Actor、资源服务等缺口。本轮未扩到这些模块，也未把已有标量段或 factory 地址当成完整对象。

因此，本轮不以手工补图、假成功 helper 或新造算法接入现有 `ClassFactoryDispatch`。正式样本兼容、原生运行时对象注册和完整物品工厂继续单独标记未验。

## 校验与保存

- 原版：`D:\Steam\steamapps\common\Grim Dawn\x64\Game.dll`。
- 原版 SHA256 前后均为 `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`。
- 本轮合计：代码 **11,241 B / 2,599 条指令 / 234 次固定范围 dumpbin**；显式 metadata/data **26,683 B**，包含表指针 3,520 B 和名称字面量 15 B。共有 36 次原版全文件哈希，缓冲区 64 KiB；哈希读取量和 dumpbin 内部 metadata I/O 不计入上述语义字节数。
- 未启动原游戏、原 DLL、Rea 或 Java，未安装、扩权、接触存档/Steam 云，未 commit/push；本轮没有读取正式 ARZ。
- 产品源码未改，既有 91 项基线及 186 项上一检查点的当前文件/副本均核对通过。之前 **2,282 断言 / 83 场景组**为上一轮成功结果，本轮没有重跑，也没有新增断言。
- HEAD：`6dfdae419d1c92f7738eefe241937057d83134df`。

详细指令与来源见 [证据记录](CLASS-STATIC-INIT-EVIDENCE.md)、[续段报告](offline-class-static-writer-continuations/inspection.json)、[名称及导入绑定](offline-game-class-init-data/metadata.json)。本轮验收与保留审计保存于 `class-static-init-validation/`，恢复清单为 `.tooling/checkpoints/class-static-init-complete/manifest.json`；该增量检查点需配合此前检查点。
