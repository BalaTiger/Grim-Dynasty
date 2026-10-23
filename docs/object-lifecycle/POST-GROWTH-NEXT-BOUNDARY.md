# 扩容后的下一连通边界 — 2026-10-20

本轮仅离线读取工程已有证据并准备查询数据计划。**Rea/Ghidra 启动 0 次，原游戏读取 0 次，无行为代码修改或测试重跑。** [基线核验](post-growth-boundary/baseline-verification.json) 的 82/82 源码、自有产物及工具哈希仍匹配；930 条断言、14 场景和 ItemQuestDemo 是上一轮已经通过的结果，不是本轮新增验收。

选择下一步为 **Engine `0x18013F300`，LoadTableBinary 构造器**。它是 `LoadFile` miss 中已直接确认的调用，能先确定真实解析对象的初始状态和虚表，再识别 parser。工厂 population 与派生 Read 也有精确锚点，但尚没有足够证据直接接成具体对象。当前 OutOfWindow 使用重负载窗口，本轮停在新查询前；没有请求审批、收到拒绝或不明执行结果。

## 当前连通链与最小缺口

`CreatePartial → LoadFile → 缓存未命中 → Binary 构造/解析 → 缓存发布 → 已有 Binary Class getter → 类分派 → 具体构造 → FinishPartial → 真实 Read`

已有实现覆盖显式完整快照上的缓存命中、Binary getter 的 warm/stale/cold/增长、显式类元数据分派、自有构造及 Read 回调、名称赋值和有界登记。它们尚不能从原始记录字节产生实际表、选择真实原生工厂或执行真实派生 Read。不能用测试输入替代这些缺口后称为整链完成。

| 路径 | 已确认事实 | 最小未完成证据 |
| --- | --- | --- |
| Binary 构造/解析（本轮选择） | `1C4FE2→13F300`，随后 `1C4FF4` 调构造返回对象的 vtable `+8`；已保存导出 ctor 与虚表名称 | `13F300` body、实际 vptr 写入及 archive/字段初始化；然后确定该 vptr 的 `+8` 槽值与 parser body |
| 原生类工厂 | `17E1B2` 取 Engine root `41CF00`，`17E1BC→274EC0`；`274F0F` 跳 metadata `+28` callback | root/children/population 的实际值及写入者、到具体类 metadata 的关系、真实 callback 与分配/构造 wrapper |
| 具体 Item/派生 Read | Game `323690` 的主 vptr 写入 `76A4C0` 已保存；FinishPartial 在 Engine `17E38A` 调对象 vtable `+8` | Game 槽地址 `76A4C8` 的 qword、目标 body；具体派生类型最终 vptr 仍应单独证明 |
| Item 创建入口桥接 | Game `324120→12C910`，返回后还有 replica 设置和虚表 `+3F0` 调用 | `12C910` body；不能据调用名推断自动 ID 分配/创建/登记的顺序 |

表中短地址均省略各映像的 `0x180` 前缀。Engine 与 Game 是不同二进制，不能跨映像套用槽值。Engine 固定 SHA256 为 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`；Game 固定 SHA256 为 `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`。

## 解析路径的准确顺序

来源：[miss 汇编](rea-record-load-dependencies/11-LoadFile-miss-0x1801c4e20-assembly.json)，evidence ID `ev_23bf6a83dc20e5e716f1454ed05f04ab266b5e67430c35070017de7c68eb1937`；[direct callees](rea-record-load-dependencies/12-LoadFile-miss-0x1801c4e20-direct-callees.json) 为 partial，不包含全部间接调用目标。

1. 外层 [LoadFile](rea-object-base/11-TableDepot-LoadFile-0x1801c4d20-assembly.json) 在自己的 depot gate 内于 `1C4DAF` 进入 miss；不能给 miss 另造一个已证原生锁。
2. archive 名称查找 `1C4EAF→03C8E0` 得到 ID。`archive+2E bit0` 为 1 时调用 `280E20`；否则按 uint ID 桶链取得 descriptor，使用 `+28` offset 与 `+2C` size。特殊取数 body 与有效内存域仍未闭合。
3. `1C4FB4..FBF` 建 reader：`+0x00=data`、`+0x08=data`、`+0x10=uint32 size`、`+0x14=0`。`1C4FC8` 请求 `0xD0` 字节；`1C4FE2` 调 ctor，传入重新读取的 archive。
4. `1C4FF4` 调构造返回对象的 vslot `+8`，随后没有观察到 parser 返回值检查。parser 返回后才看 reader `+0x14`；若非零，`1C5002` 释放**当时的 reader+0x00**。不能预设始终释放原输入，也不能自行添加 parse-result 成功布尔分支。
5. 表还需要发布到名称缓存：`1C508B→05DEB0`；`1C50B5..C1` 先增加链 count、写两个链指针；`1C50DA→02E770` 再维护索引；之后才释放临时名称、更新统计。两个 helper 尚无独立 body，不能替换为原子字典插入或假定失败回滚。
6. 取数失败路径会先增失败计数，再于 `1C5032` 调 engine vslot `+18` 诊断，然后 `1C5046→13C1D0` 构造普通表，仍进入公共发布。不能把它简化为空表返回。

因此只增加 reader 壳、统计计数或返回 supplied table 的 loader，不能补上本次实际连通缺口。当前不新增这些替代实现，也不增加仅验证假定协议的断言。

## 下一固定查询：仅构造器，8 calls

[待执行数据计划](../../tools/static_analysis/rea-binary-table-constructor-plan.json) 使用固定 Engine、既有 Rea 6.5.0/Ghidra provider、既有工具白名单，共以下 8 项：

1. `binary_session(expected_package_version="6.5.0")`
2. `open_binary(fixed Engine path, provider_id="ghidra")`
3. `binary_session(expected_package_version="6.5.0")`
4. `procedure_pseudo_code(procedure="0x18013f300")`
5. `procedure_assembly(procedure="0x18013f300")`
6. `procedure_callees(procedure="0x18013f300")`
7. `close_binary()`
8. `binary_session(expected_package_version="6.5.0")`

地址双重来源是 miss 的精确 CALL 和 [导出记录](record-interface-discovery/record-interface-exports.json) 中 ordinal 274：`??0LoadTableBinary@GAME@@QEAA@PEAVDatabaseArchive@1@@Z`。需要回答的具体问题是：构造器实际写哪个 vptr、是否保留 archive 参数、哪些字段/容器初始化、调用哪些 helper、最终返回哪个对象指针。发现 callee 后只归档审阅，不自动扩查或补实现。

计划为 **prepared-not-started**；未向现有 runner 注册新模式，未改配置或 guard。未来执行须等待重负载窗口释放，并沿用该次工具调用的审批流程；计划自身不扩展授权。沿用单会话、512 MiB heap、启动至少6 GiB空闲、两秒采样低于5 GiB时仅终止自有树的现有边界；少量函数查询仍可能触发整份 Engine 导入，不能视作轻量操作。

### 构造器证据之后的条件步骤

当前导出表给出的 `LoadTableBinary` vtable 是 `0x180341FE0`，但导出名称不证明 ctor 将它写入返回对象。[既有槽读取](record-interface-discovery/record-interface-vslots.json) 仅保存该表 `+28→13FB60`，没有 `+8`。

**只有 ctor 证据确认实际 vptr 后**，才准备一次独立的定点 PE qword 读取。若确认值恰为 `0x180341FE0`，槽地址为 **`0x180341FE8`**（现有导出映射推得文件偏移 `0x340FE8`；执行前须重新验证 PE 映射及固定 SHA）。读取8字节并将返回地址与同一 x64 映像导出匹配；槽地址本身不是 parser 函数地址。若出现其他 vptr、调整指针或未知动态绑定，停止并修订范围。

目标解析后，再对**已确认的实际函数地址**准备独立伪码/汇编/callees批次。现阶段不填一个猜测的 parser VA。旧 [AssetManager 导入清单](../phase-1/boundary-audit/module-boundaries.json:8051) 的 `?Deserialize@LoadTableBinary@GAME@@UAEXAAVBinaryReader@2@@Z` 只是32位装饰名线索，不能替代 x64 槽绑定。上述定点读取和 parser 查询均不包含在当前8次计划中，均未启动。

## 工厂与 Read 的证据边界

根 metadata `Engine 41CF00` 只由 [CreatePartial caller](rea-native-registration/04-ObjectManager-CreatePartial-order-assembly.json) 确认为分派输入；[分派 body](rea-native-registration/09-Class-factory-helper-0x180274ec0-assembly.json) 确认 `+08=name / +18=signed child count / +20=children / +28=callback`。没有据此恢复 root 的实际字段或 population。Game [AddItem](../rewards/rea-inventory-add/Inventory-AddItem-order-assembly.json) 在 `31ED7B` 使用 `Item::classInfo=0x180A5C8A0` 并沿 `+10` parent 做类型检查，只能作为以后定点 metadata/写引用分析的锚点，不能证明它已连接 Engine root。

[Item 构造汇编](../rewards/rea-inventory-add/Item-constructor-primary-table-assembly.json) 在 `3236BA/3236C1` 写主表 `76A4C0`；尚缺 `76A4C8` 的槽值。Engine Object 基类 `339F78+8→00CE70` 的 RET 只适用于该基类槽，不适用于 Item 或具体派生类型。已知构造器也不等于工厂 allocation wrapper，不能直接填入 `+28` callback。Game [CreateItem](../rewards/rea-item-creation/Item-CreateItem-replica.json) 的 `12C910` bridge 仍是另一明确缺口。

原生表解析、具体工厂与派生 Read 将按实际依赖逐段接通；当前维持 Unsupported，保留既有930/14行为基线。不 commit/push，不扩大二进制/工具范围，不占用 OutOfWindow 重负载窗口。
