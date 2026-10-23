# Binary 子对象查询与 384M 实测 — 2026-10-20

后续配置状态：本报告中的预算提案已由父线程采纳并接入parser专用模式，[27项离线守卫测试通过](PARSER-QUERY-READINESS.md)。本报告保留上一批次执行时的参数和事实，新parser批次尚未执行。

**固定8/8 calls成功，单个Rea/Ghidra后端正常关闭；`0481F0`零数量路径已闭合，LoadFile的正常构造返回路径可绑定到`0x18013F510`。** 本轮没有查询parser本体或新增行为实现。

父线程明确交出UE退出后的窗口后，实时预检未发现UE、编译或其他Java进程，可用物理内存约11.84GiB。实际Java参数为`-Xmx384M`，CPU配置未变，MCP请求串行。384M仅写入专用子进程环境，原项目512M环境模块与`.codex/config.toml`哈希均未改变。

## 执行与释放

- [查询摘要](rea-binary-subobject-profile/summary.json)：`04:34:01.801–04:35:45.133 UTC`，耗时 **103.332秒**。伪码、汇编、直接callees加生命周期共8 calls，无追加查询、强制终止或错误。
- [释放复核](rea-binary-subobject-profile/resource-release.json)：`04:36:13.996 UTC`所有已归属PID＋创建时间身份均退出，`analysis-runtime/rea`无文件，未发现UE/编译/Java重进程。随后仅离线整理。
- 父线程准备接回OW窗口后，`04:43:03.799 UTC`[再次复核](subobject-profile-validation/window-release-confirmation.json)：本任务剩余进程仍为0、runtime文件0；当时物理available为12.279GiB、commit余量26.610GiB。窗口已交回，不等待文档整理结束。
- `D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll`查询前后及释放时SHA256均为`4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`。
- [独立验收](subobject-profile-validation/acceptance.json)逐行核对70条采样、实际堆开关、最终零自有后端样本、采样器空stderr/退出0，以及峰值重算。没有计数读取缺口或观察到并行重进程。

监控模块进入收尾后会忽略部分晚到错误，所以另行解析原始末尾样本和退出状态，没有单凭`failures=[]`推断完整释放。轮询仍有下述计量边界。

## 可归属内存

| 每次扫描的总和，再取峰值 | PrivateMemorySize64 | WorkingSet64 |
| --- | ---: | ---: |
| 后端树 | 0.936 GiB | 1.058 GiB |
| 后端树＋控制器＋采样器 | **1.109 GiB** | **1.226 GiB** |
| 控制器＋采样器自身 | 0.233 GiB | 0.268 GiB |

[逐PID汇总](rea-binary-subobject-profile/owned-memory-summary.json)及[重算预算](subobject-profile-validation/measured-budget.json)保留字节值和峰值时间。各指标峰值可能发生于不同时刻，不能相减推导开销。每轮逐进程读取，不是原子快照。

Java PID42820最后观察到`PeakPagedMemorySize64`为 **704.496MiB**，`PeakWorkingSet64`为 **694.004MiB**；堆上限不等于JVM总占用。Rea Node对应值为425.270 /404.500MiB。75个身份含34个native decompiler身份，单轮最多10个；“单后端”不表示只有一个native子进程。

请求间隔1秒，实际平均约1.487秒、最大约1.675秒，每次读取最长约664ms。短命子进程、未观察到的父子链及退出前最后间隔可能漏计，WS求和可能重复共享页。`PeakPagedMemorySize64`是系统提供的历史峰值属性，不能当成连续采样的private时间序列或磁盘换页量。控制器及采样器已计入，但不涵盖全部系统计量服务开销。

系统最低available为10.543GiB、commit headroom为24.751GiB，仅说明当时余量，没有拿系统下降量冒充任务占用。

## 预算提案

同一Engine、版本、384M和相近查询下，暂估任务额度 **2.25GiB**：

`向上取整至0.25GiB〔max(1.108681,1.225800) × 1.5 + 0.25〕`

50%加成和额外0.25GiB是显式工程假设，**不是实测需求或最坏情况保证**。系统保留R另计：R=2GiB的示例对应启动available/commit headroom均至少4.25GiB，停止线为两者任一低于2GiB。此提案未写入runner，也未在低余量条件下验证；继续要求无并行重负载、归属采样和仅停止自有树。

本次为取得归属测量，仍采用6GiB启动、5GiB物理/commit停止的校准线，均未触发。旧7GiB建议及6/5线不是用户硬要求或实测需要，低内存槽读取及离线工作不受这些后端线限制。256M未测试，CPU限额未调整。

## 函数结果与下一边界

[精确指令证据](BINARY-SUBOBJECT-EVIDENCE.md)确认：caller使RDX=0，helper只向parent`+78/+80/+88`写三个qword零，随后返回，不解引用R8，也不触发非零分支的allocator、填充helper或错误处理。

结合构造器写`341FE0`、已保存槽`341FE8→13F510`与LoadFile`1C4FF4`的间接调用，正常构造返回域的实际目标已确定为`0x18013F510`。记录格式和解析行为仍未知。已准备[独立parser入口8-call计划](../../tools/static_analysis/rea-binary-parser-entry-plan.json)，仅该目标的伪码、汇编、直接callees，未注册runner模式或执行；当前零数量分支无需追查`0483A0`。基类`13C1D0`的gate/字段初始化仍是完整构造的独立缺口。

原82项基线中81项不变，唯一预期变化是Rea runner的profile模式。行为代码与已编译测试程序未改变；**930断言、14个组合场景及ItemQuestDemo是已有验收，本轮没有重跑**。未启动原游戏、访问存档/Steam云、安装工具、更改系统设置或commit/push。没有被拒绝或结果不确定的操作。
