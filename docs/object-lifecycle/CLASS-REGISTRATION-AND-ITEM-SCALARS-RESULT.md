# 类注册追查与 Item 局部初始化 — 2026-10-20

**本轮完成了一个独立 Item 基础初始化段，运行时 QuestItem 注册绑定仍未恢复。** 没有新建猜测的注册树，没有调用未恢复的工厂，也没有返回完整物品对象。旧 ClassFactoryDispatch 实现保持不变。

## 运行时注册的实际进展和停点

精确查询两个 DLL 中九个限定导出前缀，确认 Item、Actor、Object 的 ClassInfo 与两个 GetRTTIClassInfo 地址；没有匹配的 RTTI_ClassInfo 构造、析构、Register 或 QuestItem 全局初始化器导出。这不证明内部或内联实现不存在。

| 观察 | 得到的结论 |
| --- | --- |
| QuestItem getter `Game 051810..051818` | `LEA RAX,A5C150; RET`，只返回 ClassInfo 地址，不做初始化 |
| Item getter `Game 029A50..029A58` | `LEA RAX,A5C8A0; RET`，同样没有写入 |
| Item ClassInfo `Game A5C8A0` | name、parent、count、children 磁盘值为零，factory 为 `104E00`；零值不证明运行时赋值已经发生 |
| Actor ClassInfo `Engine 41CFC0` | 磁盘 parent 为 `41CF30`，不是已经证明的 Object 直接子节点 |
| Object ClassInfo `Engine 41CF00` | 磁盘 name/count/children 为零，factory 为 `04A0D0` |
| QuestItem/Item 共用 ClassInfo 虚表目标 `Game 591430` | 将 `+08`、`+20` 和条件性的自身指针传给 helper，呈现清理形态；本段未直接写入 name/children/factory，helper 的完整语义尚未恢复 |

先前保存的 InitializeObjects 入口会分配并构造 GameEngine，不能因名字而视为无状态注册列表；本轮没有沿它追踪整个引擎。也没有读取与注册缺乏引用关系的邻接工厂函数。

仍缺少 `QuestItem A5C158` 名称写入者、Item 父链的实际建立者、children 分配／插入及次序、根向下可达性和初始化时序。这些事实不能由 C++ 导出名字、静态 parent 候选或官方字段字符串替代。因此本轮**没有完成“官方 Class 值确实到达 QuestItem 工厂”的运行时注册验证**。

已证分派规则仍为原始字节区分大小写、self 优先、按原子节点数组顺序递归；匹配节点直接跳转 factory。祖先是否继续其他子节点取决于该工厂返回的对象指针。不执行构造时，最多能确认第一次匹配处的待跳转目标，不能擅自断言最终创建结果。本轮未增加重复的调用者自填注册图接口。

详细证据及各文件指纹见 [CLASS-REGISTRATION-INVESTIGATION.md](CLASS-REGISTRATION-INVESTIGATION.md)。

## 已落地的独立基础初始化段

[ItemScalarInitialization.cs](../../src/task_items/ItemScalarInitialization.cs) 恢复保存汇编中 `Game 323714..323743`（不含终点）的局部操作。该地址跨度 47 字节、7 条已保存指令，本轮没有重新读取原件中的这段字节。

| 指令 | 精确写入 |
| --- | --- |
| `323714` | `XOR EBP,EBP`，不依赖此前寄存器值 |
| `323716 / 32371D / 323724` | qword `+870 / +878 / +880 = 0` |
| `32372B` | qword `+88C = 1`，不是仅写低 DWORD |
| `323736` | qword `+894 = 0` |
| `32373D` | dword `+89C = 0` |

独立状态使用可空字段区分未知和已知零，重叠 DWORD 从同一 qword 投影。`+888..88B` 及 replica stack `+6B0` 保留已有值或未知状态，不随邻近字段补零。正常操作完成六次、共 44 字节的写集合，并报告下一位置 `323743`。

`Known` 只表示上述局部段完成，`IsCompleteConstructor` 恒为 false。null 状态返回 Unsupported，无写入或虚构的执行进度。操作不返回 Item/QuestItem，不实现 ClassInfo 构造回调；没有跳过 Actor、前序 helper、后序字符串、全局读取或资源加载并宣称它们成功。有效存储、适用布局和独占访问仍是这个逻辑投影的前提，原生无效内存、异常及并发行为不在覆盖域内。

## 实际验证与保全

- 新增 [ItemScalarInitializationTests.cs](../../src/task_items/ItemScalarInitializationTests.cs) 的 **18 条断言通过**：未知／既有存储、完整 qword 覆盖、别名视图、未写区域保留、null、不同状态隔离和重复写入。
- fresh 串行构建和既有相关回归共 **2282 条断言 / 83 组场景通过**，使用已有 csc、`/checked+ /optimize+`；执行日志与摘要保存在 [retry1 验证目录](class-registration-retry1-validation/test-summary.json)。
- 首次编译因新增文件缺命名空间结束括号而失败（CS1513），尚未执行该轮产品测试；补齐后改用新输出目录重跑。初次 [失败记录](class-registration-validation/test-summary.json) 保留，没有覆盖成成功。
- 本轮没有读取官方 ARZ 或重跑单记录 probe；前次 123 条官方单记录断言仍是历史结果，不能计作本轮注册或物品验证。
- 新定点原件代码读取共 **92 字节 / 25 指令 / 3 次 dumpbin**；显式元数据及固定数据读取 **15682 字节**，其中固定 ClassInfo 数据为 168 字节。另有 14 次全文件流式指纹读取，使用 64 KiB 缓冲；dumpbin 内部元数据 I/O 不计入上述显式读取量。
- 原 Game 与 Engine 的前后 SHA256 均保持，分别为 `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2` 和 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`。
- 未启动原游戏、Rea、Java，未安装／扩权、接触玩家存档或 Steam 云，未 commit/push。正式样本兼容、原生时钟全局、一般归档生命周期及完整物品构造／Read 继续单独标为未完成。

阻断是**缺少实际注册写入者的证据及其自有实现**，不是权限拒绝；没有待批准或结果不明的外部动作。下一最小目标须从名称／children 的实际写入引用产生，不能据邻接布局或预期效果制造注册算法。

[机器验收状态](class-registration-retry1-validation/acceptance.json) · [保全审计](class-registration-retry1-validation/preservation-audit.json) · [恢复清单](../../.tooling/checkpoints/class-registration-complete/manifest.json)
