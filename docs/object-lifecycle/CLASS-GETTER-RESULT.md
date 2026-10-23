# Class getter 与部分创建路径检查点 — 2026-10-19

后续检查点：[FinishPartial 名称边界接合](FINISH-PARTIAL-RESULT.md) 已补齐首个 NUL 前缀的实际重新加载与注册接口，746 条断言通过。本文保留此前 703 条断言检查点及当时的组合范围。

本轮把此前分离的模块连接为一条自有调用路径：**已知缓存表 → 实际普通表 Class getter → 类工厂分派 → 已供构造回调 → 原始名称赋值**。`CreatePartialObject` 调用已实现的 getter，不接受用固定返回值替代 getter 的回调。已有缓存、类图和构造回调仍由调用方明确提供；这不证明原生缓存初始化、真实 ClassInfo population 或完整派生构造已经恢复。

[RecordTableGetter.cs](../../src/task_items/RecordTableGetter.cs) 实现普通 `LoadTable` 的有界正常查询结果：对查询键取首个 NUL 前缀，按完整存储键作无符号字节字典序查找。完整、稳定、唯一键的有效树以严格有序字段快照表示，保留字段和值的身份；不模拟原树形、比较次数、原生指针和并发。字段缺失或值的**存储长度**为零时返回同一 default；长度非零而首字节为 NUL 时仍返回同一存储值。

[CreatePartialObject.cs](../../src/task_items/CreatePartialObject.cs) 保留原版 caller 顺序：已知空 table 直接结束；非空 table 查询 `Class`，默认值为空 C-string；返回类名随即进入工厂分派，空类名也不跳过；只有非空且属于已覆盖模型的对象才调用 SetObjectName。赋名使用原始记录名称的同一对象引用，保留其大小写、反斜杠和完整 payload，不以局部缓存 key 或 class 名称替代。已发生的命中计数、构造回调效果和部分对象都保留。

工厂返回 null 而类名非空时，caller 还有引擎诊断调用；目前明确返回 `UnimplementedSpawnDiagnostic`，没有省略诊断后宣称完整 Known-null。工厂返回未覆盖类型时保留原 instance 并停止，不将类型转换失败混同为构造失败。

## 静态证据与剩余链路

两批 Rea/Ghidra 查询分别为 [getter 11 次](rea-record-class-getters/summary.json) 和 [直接 helper 17 次](rea-record-getter-support/summary.json)，共 **28 次**。两次逐次审批均成功，均确认关闭会话、退出码 0 和 Engine 哈希不变；没有遇到审批容量满或超时，也未重试审批。

[详细证据](CLASS-GETTER-EVIDENCE.md) 记录了 `0x18013EBC0` 的 lower_bound 行为以及 caller 的反向比较；两者结合才足以把有效有序树查询归结为精确键匹配。局部字符串复制通过本轮 `0x180022CE0` 汇编和此前已确认的增长/复制/释放成功契约闭合。

二进制表 getter 目前只实现 `+0xC8` 明确为空时直接返回同一默认值的分支，它不读取查询键。非空 archive 的 `0x180283840` 会操作以**原始查询指针**为 key 的缓存，并可能调用尚未补齐的 `0x180284160` 写入；不能替换成纯名称到 ID 字典。`0x18013FC80` 的静态行为虽已取得，但未作为孤立替代品绕过该链。该函数对无对应项/非字符串类型返回 default，对字符串池索引无效返回 null，二者也不能混同。

完整成功链仍缺：二进制表非空 archive getter、缓存 miss 的实际记录读取与表解析、原生类注册/派生构造绑定、真实对象 Read，以及分配器与登记/销毁的全链接合。进度见 [生命周期图](LIFECYCLE-PROGRESS.md)。下一段可针对二进制名称指针缓存和字符串池身份继续静态补证；不必重做普通表 getter、名称赋值或已验证的注册增长。

## 验证与保存

全新 `.tooling/class-getter-build/` 构建通过 **703 条断言**：此前 620 条全部回归通过，新增 [getter 测试](../../src/task_items/RecordTableGetterTests.cs) 43 条及 [CreatePartial 测试](../../src/task_items/CreatePartialObjectTests.cs) 40 条。两套组合测试共 4 个场景及既有 ItemQuestDemo 均通过；场景数没有再次加到断言总数中。自有程序集只引用框架及其他自有 GrimDynasty 程序集。

本轮两个新组合场景连续经过 typed cache 命中、实际 Class getter、实际工厂分派、显式自有基类构造和完整原始名称赋值，再显式调用第二次完整字符串 LoadFile，并接入 pending 删除、两个索引各自 `8→64` 增长。成功场景的 own Read 在所有 gate 外看到同一对象和表；缺失 Read 的场景在相同已完成前缀之后返回 Unsupported。第二次完整字符串加载是明确的自有接口组合，尚未恢复 FinishPartial 的原名称 getter/C-string 路径，不把这两个场景称为完整原生 FinishPartial。

其他断言覆盖 query 首个 NUL 与完整 stored key/value 的区别、默认值身份和 known-null、unsigned 字节排序、输入复制、容量与总量边界、空 Class 仍参与分派、构造失败后的诊断断点、未覆盖实例保留及构造回调修改原名称引用后的 self-alias 赋值。实现、测试及接口语义均完成独立静态复核，无未处理问题。

保留 [验收与源码/产物哈希](class-getter-validation/acceptance-class-getter.json)、[完整构建输出](class-getter-validation/build-output.txt)、[Object ID 20 条回归](class-getter-validation/object-id-regression-output.txt)、[独立名称哈希向量](class-getter-validation/hash-vectors.json)、[程序集依赖](class-getter-validation/task-items-dependencies.json) 及 [13 个原版文件的哈希复核](class-getter-validation/game-hashes-after.json)。既有核心实现未改，当前 HEAD 仍为 `6dfdae419d1c92f7738eefe241937057d83134df`。

修改前 runner/build 已保存于 `.tooling/checkpoints/class-before-getter/`。通过版本的源码、查询计划/runner、静态证据、进度图和验收记录保存于 `.tooling/checkpoints/class-getter-passed/`，由 [检查点清单](class-getter-validation/checkpoint-manifest.json) 逐项验证哈希；这是接续前一检查点的增量保存，不是完整可运行游戏。

原版保持只读静态分析，没有启动游戏、执行原 DLL、读取玩家存档或 Steam 云数据，没有安装额外工具、修改系统/全局配置、commit 或 push。历史 894 断言 / 16 场景全验收仍未在本机重跑，UCRT 原有指纹白名单保持不变。
