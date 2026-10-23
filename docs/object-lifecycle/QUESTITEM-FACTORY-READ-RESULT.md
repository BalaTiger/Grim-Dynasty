# QuestItem 具体工厂与 Read：证据闭合，完整对象仍阻断

2026-10-20。本轮确认了具体 `GAME::QuestItem` 的工厂回调、构造后的主虚表及实际 `Read` 入口，并核对了其直接 `Item::Read`。**尚未完成官方废铁 → 可用自有物品对象的构造/Read 闭环**；没有把未知调用替换成成功回调，也没有新增一个只改类名的 Base 对象。

## 本轮确认的具体范围

| 环节 | 已确认的静态关系 |
| --- | --- |
| Class 来源 | 前轮实际 getter：key ID 13 → value ID 28355 → `QuestItem` |
| 类型符号 | Game 导出 `GAME::QuestItem` 构造、`classInfo` 及 Object 主虚表 |
| 工厂槽 | `classInfo 180A5C150 + 28` → `1801066B0` |
| 工厂行为 | 向 `1805DA6C4` 传入 `0xCD0`，将返回值传给构造 `180427C90`；正常返回路径保持构造后 `this` 指针关系，分配及失败语义仍未闭合 |
| 构造 | 先调用 `Item` 构造 `180323690`，再完成已确认的派生字段写入 |
| 最终主 vptr | `180427CAC` 写 `18078E058`；此后无进一步调用或覆盖 |
| Read | 主虚表 `+08` → `180427E40`；先调用 `Item::Read 180324C60` |
| 基础构造／读取 | 已绑定导入 `Actor::Actor`／`Actor::Load`，均不可省略 |

正常基础构造已经成功返回的前提下，QuestItem 后缀可确定：`+C68=0`，`+C70=0`，两个字符串 `+C78/+CA0` 长度零、容量 15、首字节零，`+C98=0`，`+CC4=0`，`+CC8=750`。构造没有写 `+CC0`，其他未观察字节不能默认归零。750 暂不赋予猜测的业务名称。

## 实际 Read 字段和废铁分支

以下键名来自 `Read` 的 RIP 引用字节，非字段名猜测：

| 查询及顺序 | 默认值 | 已观察效果 | 冻结废铁记录 |
| --- | --- | --- | --- |
| `bitmap` | 空串 | 扫描 C-string 长度并调用 `016460(&this+C78, …)`，再调用图形服务加载，返回指针写 `+C70` | `items/misc/bitmaps/scrap01.tex`，非空 |
| `trailEffect` | 空串 | 结果空则 `+C98=0`；非空进入效果创建及可能的虚调用 | 不存在 |
| `requiredQuestFile` | 空串 | 扫描长度并调用 `016460(&this+CA0, …)` | 不存在 |
| `requiredTaskUID` | 0 | table `+38` 返回 EAX，写 `+CC0` | 不存在 |
| `showQuestItemTag` | 0 | table `+58` 返回 AL，写 `+CC4` | 不存在 |

这三个 `required* / show*` 键与记录中的 `questFile1/taskUID1` 等字段不同。不能将它们互相代用。完整记录仍含 38 个已冻结字段，但字段存在不代表具体 `Read` 已消费它。

`trailEffect` 缺省可跳过其条件效果分支；它不能使前面的 `Item::Read` 和图形服务调用消失。`bitmap` 读取后无条件取得 GraphicsEngine、调用 `LoadTexture`，不能把“已拿到路径字符串”报告为纹理或完整物品初始化成功。

## 实现阻断及状态

阻断是**证据及实现覆盖缺口**，不是权限拒绝，也没有待批准或结果不明的外部动作：

1. **实际原生名称注册尚缺。** `QuestItem::classInfo` 的 `+08` 磁盘名称槽为零；运行时名称写入、从 Engine Object 根的 children 注册及遍历可达性未恢复。C++ 同名导出本身不能代替这个绑定。
2. **完整构造尚缺。** `Item` 构造无条件调用 `Actor::Actor`，还依赖多个 helper、GameEngine 全局状态和随机时间等行为。不能用已实现的简单 Object 基类构造代替。
3. **Read 起点尚缺。** `Item::Read` 的第一步是 `Actor::Load`。该函数及其状态效果未恢复；`Item::Read` 自身也包含内部对象虚调用、资源相关 helper 和多种 table getter。
4. **正式废铁的资源步骤不可跳过。** 已确认 `gEngine → GetGraphicsEngine → LoadTexture`；自有实现尚未提供等价服务及返回对象语义。返回空指针或仅保留文件名将是占位。
5. **现有接线须随具体实现调整。** 当前 `ObjectBaseState` 为 sealed；`CreatePartial` 仅接收该类型。`LoadedObjectRegistration` 使用 `Action` 并把正常返回当作 Read 完成，需要可表达失败及已发生副作用的具体 Read 结果。缓存表也尚未保留全 tag0/1/3 的不可变 parsed record，仅 trace 保留，不能从 tag2 Warm 反造。

本轮到具体 QuestItem 和一层直接 Item 读取为止，没有继续扩散 Actor 继承树、资源系统或运行时注册初始化。下一步明确目标应是上述基础构造/Load 与资源服务所需的最小自有合同；在它们成立前，完整 factory/Read 必须保持 Unsupported。仅实现派生初始化后缀不能完成本次目标，因此没有把这种后缀当作新成功物品交付。

## 验证与保全

- 固定反汇编 **4 段、1679 字节、394 条指令**；所有字节连续覆盖并与原件切片一致。
- 导出、`.pdata`、固定文字／数据及七个 IAT 绑定共 **10810 字节**的显式解释读取。该数包含必要固定数据，不包含流式全文件指纹或 dumpbin 内部元数据 I/O。
- 每批原 Game 文件前后指纹均保持 `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`。
- 本轮没有修改产品代码、读取正式 ARZ、运行产品测试或构造官方对象；此前 **2264 断言 / 83 场景及正式缓存探针 123 断言**是上一轮通过记录，没有算作本轮新验收。
- 一个新脚本最初存在重复局部变量声明，Node 在解析阶段拒绝；修复后首次实际执行通过，失败前没有读取原件或启动 dumpbin。
- 未启动原游戏/Rea/Java，未安装扩权、接触存档／Steam 云或 commit/push。官方完整兼容、原生计时全局及一般归档生命周期继续分别标为未完成。

[详细指令证据](QUESTITEM-FACTORY-READ-EVIDENCE.md) · [机器可读状态](questitem-factory-read-validation/acceptance.json) · [保全审计](questitem-factory-read-validation/preservation-audit.json) · [恢复清单](../../.tooling/checkpoints/questitem-evidence-complete/manifest.json)
