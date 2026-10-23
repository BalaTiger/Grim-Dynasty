# 构造器之后的最小查询边界 — 2026-10-20

历史检查点说明：下文记录当时的离线准备状态。此后槽读取与384M helper批次已完成，当前状态以[子对象查询结果](BINARY-SUBOBJECT-RESULT.md)及[进度首页](LIFECYCLE-PROGRESS.md)为准；不改写当时的原始验证记录。

本轮仅离线读取工程中的证据、源码和工具契约。**Rea/Ghidra 启动 0 次，原版读取 0 次，构建和测试执行 0 次**；云景构建期间保持后端关闭。未新增行为代码，既有 **930 条断言、14 场景及 ItemQuestDemo** 仍是上一验收。[本轮基线核验](post-constructor-boundary/baseline-verification.json)确认当前82项源码、工具及自有产物哈希全部一致。

新查询仍为数据计划，未注册 runner 模式或执行。此计划不新增权限；未来执行遇到逐次批准、拒绝或不明结果，应按原有流程停止并报告。

[计划核验](post-constructor-boundary/plan-verification.json)记录JSON可解析、精确8次顺序、原白名单和来源哈希；实际执行前仍要重读工具schema。[离线检查点清单](post-constructor-boundary/checkpoint-manifest.json)保存新计划、来源证据和当前进度，不包含原版二进制。

## 下一最小动作：候选槽 8 字节

[固定槽读取计划](../../tools/static_analysis/binary-parser-slot-read-plan.json)只选择 Engine `0x180341FE8` 的8字节 little-endian qword。候选文件偏移 `0x340FE8` 来自同一固定映像的旧导出和 section 映射；执行时重新验证 PE32+、x64、preferred image base、唯一 file-backed section 及完整8字节边界，不能直接使用旧偏移读值。

源文件固定为 `D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll`，SHA256 固定为 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`。除 slot 的8字节解码外，所需读取仅为源文件前后哈希和验证映射的 PE headers/section table；**哈希会读完整文件**，不能宣称总共只访问8字节。不读取目标函数字节、不跟随跳转、不遍历其他槽、不重新扫描导出、不启动后端或加载 DLL 执行。

输出应保留原始8字节、slot映射、候选 preferred VA、目标section映射、前后哈希和限制。目标为零、越界、非 executable/file-backed、映射歧义或哈希变化时停止；即使目标落在代码section，也不等于已经证明函数入口或 Deserialize 身份。当前没有为未知 parser 填地址，也没有读取此槽。

这一步只确认 **固定虚表的候选表项**。构造器写 vptr 后仍调用未知 helper，因此槽值本身不能证明 LoadFile 在构造返回后实际调用了它。

## 下一最小 Rea 批次：`0x1800481F0`，8 calls

[固定 helper 计划](../../tools/static_analysis/rea-binary-subobject-init-plan.json)仅查询 `0x1800481F0`。地址来自 [构造汇编](rea-binary-table-constructor/05-LoadTableBinary-constructor-0x18013f300-assembly.json)的真实 `CALL 13F378`，源证据 SHA256 `bb3fa2e3f2bca620f66caca1894932b970d69761f63327e23982f4eea158ccb0`。

调用顺序固定为：`session → open固定Engine → session → helper伪码 → helper汇编 → helper直接callees → close → final session`，总计8次。槽读取不在此批次内，两个动作可独立准备；未知 parser、基类 ctor 和新发现 callees 均不自动追加。优先选择这个 helper，是因为实际顺序为：

`13C1D0 基类构造 → 写 vptr 341FE0 → 0481F0(this+78,0,&local) → 返回原this → LoadFile调实际vptr+8`

需要回答的具体问题：

1. helper 对 R8 指向的栈 local 读取多宽、参与哪些分支？caller先写qword地址、再只清低DWORD，不能预设整个local为零，也不能从伪码猜R9或第五个业务参数。
2. 在此 caller 的 `EDX=0` 输入下，哪些字节、指针和容量按何顺序改变？哪些分配及失败分支仍缺证据？
3. direct写入、负偏移、别名或callee是否可触及parent vptr（helper相对偏移 `-0x78`）或此前字段？若有未闭合调用/指针逃逸，vptr保持继续记为未知，不能仅凭“helper通常只写子对象”推断。
4. 能确认的子对象终态是什么？清零 `+78/+80/+88` 是调用前事实，不能直接当成返回时空容器。

未来启动仍需重负载窗口明确释放、fresh host preflight及原有逐次审批。沿用单自有进程树、512 MiB heap、至少6 GiB启动线、两秒采样低于5 GiB只停止自有树；此前建议的7 GiB保守准备预算仍适用。查询结束立即关闭并核实进程与runtime释放，再离线整理。这里没有进行当前主机内存测量，也没有以历史13.49 GiB作为本轮运行许可。

## 基类构造与 parser 的后续边界

`0x18013C1D0` 在 `13F320` 被调用，既有导出将其绑定为 `LoadTable::LoadTable()`。限定工程存档内未找到它或 `0481F0` 的独立完整函数体；调用引用和导出名称不算已有 body。

基类 ctor 位于 Binary vptr 显式写入之前，因此其早期 vptr 会被后续写入覆盖；它主要阻断 **基类字段及 gate 等初始化行为** 的完整恢复。需要分析时，最小范围也是该函数伪码/汇编/callees及五个生命周期调用，共8次，不顺带扩大到普通表fallback或缓存发布。此轮只记录这个准确目标，未新增第二个 Rea 数据计划。

当槽值与 helper 的必要副作用证据足够时，才能把候选 parser 绑定为实际调用边。如果 helper 仍有未闭合依赖，则先保留未知。之后仅为已确认目标准备独立固定查询，并根据实际 body 选择最小正常成功分支；不根据名称、旧32位装饰名、已有 `+28` getter 或 Object 基类 Read 猜协议。

## 可验证的实现目标与进入条件

当前证据不足以新增可接行为模块。`RecordLoadFile` miss 没有“构造空表后跳过parser直接成功”的已证分支；现有空 archive/default 行为已经覆盖，不再增加构造壳或 supplied-table loader。`table+C8` 是 archive 指针，不是 `archive+C8` 字符串池，不能由构造字段零值生成整个 `BinaryWarmState`。

下一项有意义的行为目标是：**在明确、由证据限定的成功域内，将记录字节解析成现有 Binary getter 可读取的字段状态**。只有以下事实齐备后才实现和测试：

- 输入布局、读取宽度、端序、长度/边界、字段标识和 buffer 索引的生成规则有 parser 指令依据。
- 所选分支需要的 base/helper 初始状态与 archive前提已闭合；未知字段不能变成默认零。只覆盖正常成功外部契约时，必须明确排除原生分配/EH，并保持既有 Unsupported 边界。
- 部分写入、reader推进及释放所有权按实际顺序传播；不能自行增加parser bool成功检查或失败回滚。

届时验证应包括：已证最小记录产生的字段/索引和值、经现有 getter 的真实查询结果，以及证据支持的边界输入和已完成副作用。输入字节和预期值须从新证据推导，不能提前编造空记录格式。`CreatePartial` 可继续利用已明确的typed状态作有界验证，但在取数、`05DEB0/02E770`缓存发布及其顺序未闭合前，仍不得报告原生 `LoadFile miss → CreatePartial` 全链完成。

本轮停在上述证据边界。没有待批准或被拒绝的查询，没有不明执行结果；未改原游戏、未访问存档或Steam云数据、未commit/push。后端继续关闭，既有查询/增长检查点保留。
