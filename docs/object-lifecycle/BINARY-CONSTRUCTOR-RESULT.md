# LoadTableBinary 构造器查询结果 — 2026-10-20

固定 `0x18013F300` 查询 **8/8 calls 完成并关闭**。本轮确认 `LoadFile` miss 所调用构造器的直接虚表写入和构造顺序；helper 返回后的完整状态仍待补证，未增加行为源码或新断言。已有 **930 条断言、14 个组合场景及 ItemQuestDemo** 是上一轮通过的基线，本轮没有重跑测试。完整 miss loader、原生工厂和实际派生 Read 仍未闭合。

## 查询与释放

- 目标：`D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll`，静态读取。查询前后 SHA256 均为 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`。
- 时间：`02:20:57.004Z` 至 `02:22:22.365Z`，85.361 秒。正常关闭确认、最终无活动目标、子进程退出码 0；没有强制终止或查询错误。见 [查询汇总](rea-binary-table-constructor/summary.json)。
- Java 配置上限仍为 512 MiB；启动空闲 13.620 GiB，44 次采样最低 12.523 GiB，未触发低于 5 GiB 的停止线。这些是系统空闲观测，不是自有进程峰值或内存预留保证。本轮结束前未单独捕获活跃 Java PID/命令行，不将配置值冒称实际进程参数观测。
- `02:24:07Z` 的 [host 释放复核](rea-binary-table-constructor/resource-release.json) 未发现自有 Rea PID 3360 或任何 Java/Javaw，`analysis-runtime/rea` 文件为空，空闲 13.490 GiB。重负载窗口已释放。
- 固定 [计划](../../tools/static_analysis/rea-binary-table-constructor-plan.json) SHA256：`9eb574c85e2bd680d20623deb50079df655913b05979aa29f036bbb4e9492d6e`。runner 只注册此固定模式并接入既有 guard；工具白名单、目标和 guard 算法未扩大，MCP 配置哈希未变。

[计划核验](binary-constructor-validation/plan-verification.json) 确认源计划哈希与 summary 相符；归档 `plan.json` 经 runner 重新格式化，字节哈希为 `69a104ba506991d17ee6a310251a5acbb927b1345c3df5e0a7a33aae04cff283`，JSON 值与源计划完全一致。runner 语法检查通过。

## 新确认的边界

[汇编](rea-binary-table-constructor/05-LoadTableBinary-constructor-0x18013f300-assembly.json) 显示：先调用基类构造 `0x18013C1D0`，再于 `0x18013F326/32D` 将 `0x180341FE0` 写到 `this+0`；初始化一组字段，在 `this+0x78` 调 `0x1800481F0`，保存传入的 archive 到 `this+0xC8`，正常返回原 `this`。两个 direct callees 的关系清单为 complete，但它们各自的 body 尚未查询。字段写入宽度与顺序详见 [构造器证据](BINARY-CONSTRUCTOR-EVIDENCE.md)。

这为已保存的 `LoadFile` miss `0x1801C4FE2 → 0x18013F300 → 0x1801C4FF4 [返回对象 vptr+8]` 提供了有构造器写入证据的候选槽地址 **`0x180341FE8`**。该地址是槽的地址，**不是解析函数入口**。vptr 写入后的 helper 尚未分析，因此完整返回状态仍需补证。本体没有直接解析记录字节的指令，两个初始化依赖也尚未闭合，不能用全零表快照或 supplied table 回调冒充已完成的 Binary 解析。

这里 table `+0xC8` 保存 archive 指针，与 archive `+0xC8` 的共享字符串池是不同层级。构造器未建立现有 `BinaryWarmState` 所需的完整 archive 名称缓存、名称索引和字符串池；也不能用 numeric 字段初始为空跳过 Binary getter 先执行的 archive 名称查找与缓存副作用。

没有依据本次字段写入新增孤立的构造壳或假定协议测试；`RecordLoadFile` 的 miss 继续报告 Unsupported。现有源码和自有产物的一致性由 [基线核验](binary-constructor-validation/baseline-verification.json) 记录：82 项中 81 项未变，唯一预期变化为固定查询 runner；行为代码、测试产物、MCP 配置均保持上一通过基线。

## 下一步（本轮未执行）

先在相同固定 Engine SHA 下核实 PE 映射，再定点读取 **`0x180341FE8` 的 8 字节**；旧导出映射给出的候选文件偏移为 `0x340FE8`，执行时仍须复核。将槽值绑定到同一 x64 映像后，才可准备实际 parser 目标的独立伪码/汇编/callees 批次。本次计划明确排除了槽读取和后续目标查询，故本轮没有执行它们，也没有启动第二个后端。

后续还需闭合 `0x18013C1D0`、`0x1800481F0` 的必要初始化行为，以及已有 miss 的取数和缓存发布依赖；不能把 parser 目标的确认等同于完整 miss 成功。原生 class factory population、具体 callback 和 Item/派生 Read 保持各自独立缺口。

本轮没有挂起或被拒绝的操作，查询无不明结果。未读写玩家存档或 Steam 云数据，未启动原游戏、修改原版、安装新工具、扩大持久权限、commit 或 push。此前执行通道断开导致的未启动状态已由本轮实际完成的查询取代。
