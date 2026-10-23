# Binary record parser 有界实现与验收

2026-10-20。`BinaryRecordParser.DeserializeFresh` 已实现并接入 `task_items` 模块。当前本机相关回归为 **999 条断言、16 个组合场景，加 ItemQuestDemo 通过**；新增解析器为 **69 条断言、2 个实际记录字节 → parser → Class getter 场景**。隔离运行的同一 69 条断言不重复计入总数。这不是旧机“894/16、10 模块”的重新验收。

机器可读结果：[acceptance-binary-parser.json](parser-implementation-validation/acceptance-binary-parser.json)。行为来源与覆盖推导：[BINARY-PARSER-EVIDENCE.md](BINARY-PARSER-EVIDENCE.md)。该证据文档中的“待统一验收”是证据冻结时状态；本文及本次验收文件记录后续实际编译测试结果。

## 已实现的行为

[BinaryRecordParser.cs](../../src/task_items/BinaryRecordParser.cs) 接收自有 reader 的完整稳定字节窗口，按已观察到的两遍流程统计、准备存储、恢复初始 cursor，再解码 payload 和 descriptor。支持 fresh/tag2、2–15 字段、各字段 count 为正、累计不超过 65536 个 int32。自有 reader 复制输入、保留绝对 cursor 和一次消费状态，并由同一 gate 保护解析与释放。

字段头为 little-endian 的 `{uint16 tag, uint16 count, uint32 key}`，payload 为 count 个完整 int32。输出保留 descriptor 的 count 与累计 buffer index；raw key 零扩展到 uint64，重复键在桶头插入，因此 getter 找到最新匹配。多值 count、非零起始 offset、负数 payload、`FFFFFFFF` 键与 getter signed `-1` 的区别均有实际测试。

`ToWarmState` 只把实际解析所得 fields/values 接入既有 getter。共享字符串池、name context 和 query address 仍由调用方显式提供，没有伪造 archive、ClassInfo 或对象 Read。两个集成场景从独立 literal bytes 开始，未手填字段索引替代解析。

## 证据与验证

必要的 helper 批次为 **11/11**，首次向量分配补证为 **7/7**，分别耗时 92.354 秒和 93.916 秒。两批均观察到主分析 Java `-Xmx384M`、正常 MCP 关闭、Rea 退出 0、Job 为空，未强制终止。释放记录分别确认 61 和 52 个已记录身份全部退出；末批释放于 `2026-10-20T05:47:32.1182412Z`。旧归档已有 `022F00` 分配器调用逻辑，无需追加查询。完整来源哈希见 [evidence-sources.json](parser-implementation-validation/evidence-sources.json)。

- [隔离测试](parser-implementation-validation/isolated-test-output.txt)：新 parser DLL 与现有模块配合，69/2 通过。
- [模块构建和回归](parser-implementation-validation/integrated-test-output.txt)：使用系统 C# 编译器、`/checked+`，新构建模块的 979 条断言、16 场景及 demo 全部通过。随后验证包装脚本误将独立 Object ID 测试路径写成 `src/task_items/ObjectIdTests.cs`，因此包装脚本退出失败；原始失败日志保留。
- [补充 Object ID 回归](parser-implementation-validation/objectid-test-output.txt)：修正到实际 `src/rewards/ObjectIdTests.cs` 并补齐 TokenActions 引用，只补跑该测试，20 条断言通过，未重跑已通过的模块。最终总数为 999。
- [程序集引用](parser-implementation-validation/assembly-references.json)：新模块仅引用 .NET framework 与自有 GrimDynasty 模块。
- [最终只读复核](parser-implementation-validation/final-readonly-check.json)：Engine SHA256 仍为 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`，Java/decompile 为零，runtime 文件为零。

隔离脚本第一次受执行环境的 `AuthorizationManager/UnauthorizedAccess` 阻挡，未执行；该确切脚本经逐次批准后成功。其后两个验证脚本也分别获得批准。没有执行策略、安全设置或持久权限修改，没有审批未决或结果不确定的操作。末批 Rea 释放后只做了串行轻量 C# 单元验证与离线文档工作，没有占用 OW 的重负载窗口。

## 有意保留的边界

tag0/1/3、零 count、少于 2 或多于 15 字段、截断/无效输入、重复 Deserialize、offset wrap、native allocator/OOM/EH 与完整并发 ABI 不在此次等价覆盖域。reader 的无效构造参数抛自有参数异常；parser 的域检查在首次 mutation 前返回 Unsupported。这些是自有覆盖门控，不能解释成原版同样原子拒绝坏数据。正常分配/copy/zeroing、有限正常 log 仍是明确的成功契约。

完整 `LoadFile` miss、archive 数据与名称表填充、基类初始化 `13C1D0`、真实 ClassInfo/具体构造器及 Read 仍未恢复。当前 parser 能把已知记录字节转成真实 getter 可读状态，但没有把未知加载流程冒充已完成，也未形成完整物品工厂或原生对象注册。

源码与证据均留在当前未提交工作区；未 commit/push，未运行原游戏，未访问玩家存档或 Steam 云数据。完成检查点：[manifest.json](../../.tooling/checkpoints/binary-parser-complete/manifest.json)。
