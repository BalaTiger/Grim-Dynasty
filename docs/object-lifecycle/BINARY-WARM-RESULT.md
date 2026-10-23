# Binary 已命中 getter 接合结果 — 2026-10-19

仅使用已保存证据，实现并连接了 **显式地址命中指针缓存、当前 C-string 内容相同 → 数字字段查找 → Class getter → CreatePartial**。本轮 fresh build 通过 **811 条断言、8 个组合场景，另有 1 个既有 ItemQuestDemo**。其中新增 Binary suite 65 条断言、2 个场景；原 746 条全部重跑，包含独立 ObjectId 的 20 条，未重复计数。构建耗时 6.059 秒，未启动 Rea/Ghidra 或重型索引。

[BinaryWarmTable.cs](../../src/task_items/BinaryWarmTable.cs) 保存两套完整、有序、有界桶链、显式 shift/mask、同一字符串池和 int buffer。查询地址由调用方明确提供，不从数组身份、GC 地址或 preferred VA 推导。地址及节点键按完整 64 位判等；先作低 6 位移位的 64 位逻辑右移，再截低 32 位及 mask。缓存 ID 先以完整 64 位选择池内名称，内容相同后才取低 32 位并符号扩展为字段键。名称比较按原始字节到相等 NUL 为止，不作编码或 locale 转换。

[RecordTableGetter.cs](../../src/task_items/RecordTableGetter.cs) 在同一表 gate 下连接名字校验和数字字段查找，保持同一池的元素身份。字段缺失或 tag≠2 返回同一 default；合法 buffer 读得负数/池外字符串索引返回 Known null；有效的空字符串仍返回原池项。未知存储和越界 buffer 读取为 Unsupported，不冒充原生 null/default。getter 不写缓存，不向外公开绕过名字解析的独立 numeric API。

[CreatePartialObject.cs](../../src/task_items/CreatePartialObject.cs) 增加显式 `classQueryAddress` 重载，原重载保留。普通表、archive absent、已知空表的早退不要求地址；Binary warm 路径缺地址会保留已发生的 LoadFile 命中计数并停止。Class 的 Known null 会到达既有工厂 null-query 边界，不替换为空类名。正常返回仍通过现有类树、显式自有构造回调和完整原名赋值。

证据映射、精确地址和限制见 [BINARY-WARM-EVIDENCE.md](BINARY-WARM-EVIDENCE.md)。快照要求完整稳定有效数据：最多 4096 桶/节点、4096 字符串、池 stored payload 合计 2 MiB、每个字符串 65536 字节、buffer 65536 项。构造时复制可变数组，保留不可变节点和字符串身份。这些是自有覆盖预算，不是恢复出的原生初始化、布局、分配或 parser。

两个新增组合场景真正执行 warm Binary Class → 基类初始化 → 32 字节原名及完整 RawKey → FinishPartial 取 19 字节 NUL 前缀 → 加载不同的表 → pending 删除及两个索引 8→64 扩容。成功场景在所有相关 gate 外向显式 own Read 传第二张表；缺 Read 场景在完成登记后报告 Unsupported，保留已完成副作用。测试还覆盖完整高位 ID/键、同字节不同地址、同地址不同数组、首匹配、共享池空/null/default 身份、cold/stale 不修改、未知读取和预算边界。own callbacks 不表示原生构造器或真实 Read 已恢复。

manager 的 **allocator +0x10** 与 **registration +0x28** 锁边界继续分离；本轮未改分配器或登记实现，未自动将 ID 分配接入生命周期。table gate 也未与它们合并。

冷指针缓存和内容变化的 stale 分支仍在已识别的回退入口返回 Unsupported，无桩补命中、lookup、更新或插入。cold 后续名字查到非 `-1` ID 才调用仍缺函数体证据的 `0x180284160`；并非所有 cold 分支都依赖该 helper。本轮没有恢复 `-1` 的完整路径或增加 `-1 → default` 快捷规则。表冷加载与解析、原生 ClassInfo population、派生构造和 Read、诊断及自动分配/登记/销毁仍是缺口。

验收保存在 [73 项源码/产物/工具哈希及分项结果](binary-warm-validation/acceptance-binary-warm.json)、[构建输出](binary-warm-validation/build-output.txt)、[ObjectId 回归](binary-warm-validation/object-id-regression-output.txt)、[耗时](binary-warm-validation/build-run.json)、[依赖检查](binary-warm-validation/task-items-dependencies.json) 和 [一致性检查](binary-warm-validation/consistency-check.json)。程序集仅引用 framework 和自有 GrimDynasty 模块，无原版 Engine/Game/Steam 依赖。独立静态复核未发现实质问题。

本轮没有读取或运行原游戏、存档、Steam 云数据，也未重算原版哈希；上轮固定 13 文件的 [哈希记录](finish-partial-validation/game-hashes-after.json) 原样保留，并在本轮验收中记录其文件哈希。既有 30 项验收文件中 27 项保持不变，另 3 项为本轮预期源码/构建入口修改。9 份复用证据、Rea runner、两个项目 MCP 配置和 UCRT provider 门禁未变；无新安装、后台服务、权限扩大、系统设置、commit 或 push。历史 894/16 和完整离线 UCRT 验收未重跑，不能与本轮结果混同。

修改前 3 文件备份在 `.tooling/checkpoints/binary-warm-before/`。通过版本连同既有静态证据、自有产物和验收保存在 `.tooling/checkpoints/binary-warm-passed/`，见 [检查点清单](binary-warm-validation/checkpoint-manifest.json)。这是增量工程检查点，不是完整可独立运行的原游戏。
