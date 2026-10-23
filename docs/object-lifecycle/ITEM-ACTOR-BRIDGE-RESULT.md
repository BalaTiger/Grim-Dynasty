# Item 两个子对象与条件性 Actor 的共享构造前缀

本轮已把实际调用现有 Actor 条件模型的路径接入 Item：Actor 正常返回 → 四个 Item vptr → `this+538` 的 `0410E0` → `this+6C8` 的 `0410E0` → 已有 `+858` helper → 六次标量写入。结果停在 Game RVA **`323743`**，`IsCompleteItemConstructor` 保持 false。新增 **55 条断言 / 9 组**通过；受影响的两组旧套件各运行一次，另通过 **36 + 18 条**。

## 本轮新增原生证据

输入为 `D:\Steam\steamapps\common\Grim Dawn\x64\Game.dll`，11,385,856 B。只读前后 SHA256 都是 `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`。

边界查询只新增读取 **144 B metadata / 12 次读取**；`0410E0` 无 exact `.pdata` entry。前项为 `[410BC,410C7)`，后项从 `41200` 开始，不能据此把整个空隙称为函数。随后仅读取明确调用目标 `[410E0,41200)` 的 288 B 候选窗口，以及 caller `[3236B0,323743)` 的 147 B，共 **435 B / 75 条解码指令**。两个读取阶段合计四次完整流式 Game 哈希，均一致。两次 dumpbin 仅处理自制 tiny PE（合计 3,584 B），没有加载原 DLL。本轮没有重新读取 Engine.dll。

人工按入口至 RET 复核后，正常正文是 **`[410E0,411FD)`，285 B、44 条指令、41 次内存写入**，最后三个 `CC` 排除。正文没有 CALL、分支或传入内存值读取；`411EE` 先保存原 RCX 为返回身份，`411F1` 再写 QWORD `+178=1`，`411FC` 返回。写集共 231 个不重叠字节，最高写至 `+18C` 前；十处 inline payload、其它空洞和尾部四字节均不能隐式清零。`190` 只是两个 caller 目标的间距，不单凭它恢复完整原生类型大小。

关键别名由正文直接确定：第一对象 `538+178` 把 Item `+6B0..6B7` 写成 QWORD 1；第二对象 `6C8+188` 只把 Item `+850..853` 写成 DWORD 0，`+854..857` 继续保留旧值或未知。旧 `+850` QWORD 观察值因此不能被补成零。

完整有序写集、证据哈希、导入绑定和调用顺序见 [native-contract.json](item-actor-bridge-validation/native-contract.json)。原始采集状态保留在 [inspection.json](offline-item-subobjects/inspection.json)，其中“awaiting manual review”是采集时状态；人工正常路径结论在独立合同文件中补充，没有篡改采集记录。Game IAT `609700` → `Engine.dll!??0Actor@GAME@@QEAA@XZ` 使用已保存绑定，未读取加载后的 IAT。

## 自有实现与失败边界

[ItemConstructorBridge.cs](../../src/task_items/ItemConstructorBridge.cs) 的 `ItemConstructorStorage` 直接持有同一个 Actor 对象引用；`0..52F` 始终路由至它，tail 使用自有 nullable 字节保存已知/未知状态。`ItemScalarInitializationState` 与 `ItemConstructor858Subobject` 新增 live view：读取、写入及邻字段观察都访问同一份状态，没有构造后复制同步。旧独立构造 API 保留；三个旧 readonly 邻字段改为同名只读属性，这里只验证重新编译后的源码使用方式，不承诺旧二进制 ABI。

Bridge 可真正执行已有 Array → Continuation → Completion / Matrix / Engine slot 链，也可接收同一 Actor 状态的已完成报告。它继承 Actor 已明确的磁盘初值 profile、分配成功观察、独占矩阵同步/通知返回及有效全局上下文条件，未把条件放宽为加载后保证。自己的返回身份一致性 guard 不是原生 RAX 判断：原生 Item caller 保留 RDI，并忽略 Actor 和两个子对象的返回值。

| 停点 | 已完成状态 |
| --- | --- |
| Actor 依赖缺失 | 保留 Actor 自己的实际前缀，Item vptr / tail 未开始 |
| `3236E1`：缺少第四 vptr 投影 | 前三个 Item vptr 已写，Actor 正常返回已完成 |
| `3236EF` / `3236FC`：缺少对应子对象投影 | 保留较早 vptr 和已完成子对象 |
| `323702`：缺少 `+858` 投影 | 两个 `0410E0` 均已完成 |
| `323714`：缺少标量投影 | `+858` 的六次 helper 写入仍保留 |
| `323743` | 当前条件性 Item 前缀完成；未声称整个构造器返回 |

这些“投影不足”是自有模型覆盖边界，不是原生分配失败或访问违例合同。`0410E0` 的正常正文没有错误返回分支；本轮未恢复异常清理、无效指针或并发行为。自有 storage 抛异常时已写状态保留，不把它称为原生 EH 等价。一次 Bridge invocation 拒绝重放；显式 leaf 操作仍可重复执行其已证写集。

## 实际执行与保存

全文检查 [构建脚本](../../src/task_items/build_item_constructor_bridge.ps1)、[测试源码](../../src/task_items/ItemConstructorBridgeTests.cs) 和输入范围后，通过独立正式 `require_escalated` 审批执行一次：四次系统 csc 调用、一个自有库、三个自有测试 EXE。工具块 `0afea4` / `8d1d5d`，最终退出码 0，无编译警告。脚本没有覆盖旧输出或调用历史被拒脚本。精确输入和操作见 [preflight](item-actor-bridge-validation/preflight-reviewed.json)，原始执行输出见 [run.json](item-actor-bridge-validation/run.json)。

| 本轮实际运行的套件 | 断言 | 组数 / 场景报告 | 次数 |
| --- | ---: | ---: | ---: |
| 新 ItemConstructorBridgeTests | 55 | 9 组 | 1 |
| 旧 ItemConstructor858PrefixTests | 36 | 6 组 | 1 |
| 旧 ItemScalarInitializationTests | 18 | 原输出 scenarios=0 | 1 |

新增 fixture 验证独立字节预期、padding/未知高 DWORD、共享身份、回调期间状态、延迟全局读取、真实 Actor 依赖停点、Item 部分投影停点及自有异常前缀。其它旧套件，包括 Actor 旧套件，本轮运行 **0 次**；新集成实际调用旧 Actor 模型不计作旧测试复跑。

构建前的 249 项前检查点 live/copy 全部一致；构建后 13 个构建输入与 249 项 live 仍一致，随后只追加进度记录。两份旧 Item 源码不属于前一 249 项清单，已分别与 `item-constructor-858-prefix-complete` 的保留副本关联。[baseline](item-actor-bridge-validation/baseline.json) 和 [postbuild inventory](item-actor-bridge-validation/postbuild-inventory.json) 记录全部哈希。HEAD 仍为 `6dfdae419d1c92f7738eefe241937057d83134df`，已有 12 项 tracked diff 不变；新实现与证据仍未提交。

恢复检查点：`.tooling/checkpoints/item-actor-bridge-complete/manifest.json`，包含前检查点选定文件、必要旧 Item 证据与旧源副本、本轮源码/测试/构建产物/证据/报告。封存后的数量、字节和复核结果以 manifest 为准。

## 下一个直接缺口

已保存 caller 显示 `323743` 起有 `+8A8/+8C8/+8E8/+928` inline 初始化与 `+908..920` 身份字段，接着 `32380C → 428690(this+950)`。最直接的下一段是核对这段连续前缀与 `428690` 的准确边界、正常依赖和返回合同，再决定可接入范围；本轮没有假设或实现该 helper。

gEngine 发布者/时序、连接管理器正文、加载后可变 guard、原生分配/异常清理/线程语义、Item 后续及完整 QuestItem 均未闭合。正式原生样本兼容仍未验证。不运行原游戏/Rea/Java，不触及玩家存档或 Steam 云，不改执行策略/ACL/其它安全设置，不 commit/push。
