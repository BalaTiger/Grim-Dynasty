# 静态槽读取与内存预算审计 — 2026-10-20

后续状态：父线程已采纳下文同配置预算，parser专用守卫现已接入4.25GiB启动/2GiB停止，27项离线测试通过；详见[parser离线就绪](PARSER-QUERY-READINESS.md)。下文“未写入/提案”描述测量结束当时，不表示当前配置仍未应用。本轮仍未在新预算下启动后端。

**384M单后端实测已完成并释放：后端树private扫描峰值0.936GiB，含控制器与采样器1.109GiB，完整任务WS扫描峰值1.226GiB。** 原版哈希不变。旧6/5GiB线及7GiB建议不能再被描述为已测得的任务需求。

当前完整结果与限制见[子对象查询结果](BINARY-SUBOBJECT-RESULT.md)、[原始采样](rea-binary-subobject-profile/owned-memory.ndjson)、[逐PID汇总](rea-binary-subobject-profile/owned-memory-summary.json)、[独立重算](subobject-profile-validation/measured-budget.json)及[释放证据](rea-binary-subobject-profile/resource-release.json)。

## 已完成的低内存步骤

[槽读取](binary-parser-discovery/parser-slot.json)把`Engine 0x180341FE8`绑定到`0x18013F510`。没有启动Rea/Ghidra；64KiB流式哈希缓冲与PE映射验证后，仅解码指定槽8字节`10f5138001000000`，没有读取候选函数代码。槽文件偏移`0x340FE8`，目标映射为可执行`.text`中的`0x13E910`。

同一只读句柄前后SHA256均为`4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`。内部耗时19ms，写结果时Node `maxRSS=41096KiB`（约40.13MiB），不是private commit或最终退出峰值。[Node指标定义](https://nodejs.org/api/process.html#processresourceusage)。

这类低内存工作无需等待6–7GiB系统余量。槽记录当时“helper后vptr尚未知”的限制已由[零数量helper证据](BINARY-SUBOBJECT-EVIDENCE.md)补足；parser本体仍未查询。历史原始记录保持不变。

## 历史测量的边界

| 历史批次 | 样本 | 首次 / 最低系统free GiB | 系统下降GiB |
| --- | ---: | ---: | ---: |
| cold-insert | 76 | 13.435303 /11.859127 | 1.576176 |
| pointer-growth | 50 | 7.689529 /6.524269 | 1.165260 |
| table-constructor | 44 | 13.620331 /12.523014 | 1.097317 |

这些批次的`memory.ndjson`仅有UTC和全机free，**不能归属为Rea占用或峰值**。cold的单点Java工作集0.591GiB和`-Xmx512M`、growth的实际512M开关不能补足完整进程树峰值；constructor当时仅有512M配置。

private commit、工作集和进程历史峰值必须分开。[Microsoft计数定义](https://learn.microsoft.com/en-us/windows/win32/api/psapi/ns-psapi-process_memory_counters_ex)。本次使用.NET属性`PrivateMemorySize64`、`WorkingSet64`、`PeakPagedMemorySize64`、`PeakWorkingSet64`；其中任一项均不能直接等同JVM堆或磁盘换页量。

## 窗口交接与真实测量

此前曾观察云景`dotnet/cl`间歇出现，瞬时无进程不能代替协调锁。父线程明确确认UE正常退出并交出窗口后，才在`04:33:52 UTC`保存[预检](subobject-profile-validation/launch-preflight.json)并启动。执行区间`04:34:01.801–04:35:45.133`，8/8 calls通过；`04:36:13.996`复核已归属身份均退出、runtime为空。

实际Java PID42820使用`-Xmx384M`。70个样本覆盖75个PID＋创建时间身份，观察到34个decompiler身份、单轮最多10个。单后端与串行MCP不表示Ghidra内部单线程；本次CPU配置保持原样。

| 扫描总和峰值 | private commit GiB | WS GiB |
| --- | ---: | ---: |
| 后端树 | 0.935795 | 1.058125 |
| 含控制器及采样器 | 1.108681 | 1.225800 |
| 控制器及采样器自身 | 0.233208 | 0.267605 |

逐进程读取的实际周期平均约1.487秒、最大约1.675秒，每次读取最长约664ms。它不是原子快照或连续峰值；不同峰值时刻不可相减，历史PID峰值不能加成同时占用。短命进程、退出前末段与未观察到的父子链可能漏采，WS可能重复共享页。

最终原始样本、stderr与退出状态已独立核验；监控模块在finalization后会忽略部分晚到错误，因此不能只凭`failures=[]`断定终态完整。系统最低available10.543GiB、commit headroom24.751GiB仅表示当时余量，不用于推导任务占用。

## 数值提案与可恢复配置

同样本和配置的下一轮暂估 **2.25GiB任务额度**：把完整任务较大峰值1.225800GiB乘1.5，加0.25GiB，再向上取整至0.25GiB。50%和0.25GiB都是显式工程假设，用于采样遗漏及变化余量，不是测量结论或保证。

系统保留R独立决定。**R=2GiB的示例**对应启动available/commit headroom均4.25GiB，停止线各2GiB。提案没有写入runner，也未在低余量环境验证。新目标、查询规模、CPU/heap配置或并行负载变化时需重估，不保证任意环境安全。

本次为校准保留6GiB启动/5GiB物理停止线，并补充5GiB commit停止线。它们不是长期任务预算，也不限制槽读取、离线代码及文档工作。后端窗口约束来自实际并行任务协调，不是硬套内存建议。

[384M wrapper](../../tools/static_analysis/rea_profiled_server.mjs)只设置自有子进程`GHIDRA_HEADLESS_MAXMEM`，原`.codex/config.toml`和512M环境模块哈希不变；256M未测试。官方随附Windows headless脚本支持该变量。现有Rea接口未转发`-max-cpu`，普通Java options被过滤；没有注入未支持参数或修改Ghidra CPU配置。

[验收](subobject-profile-validation/acceptance.json)确认行为代码与已构建测试程序未变，已有930/14验收保留，本次未重跑。未安装新工具、更改系统设置、启动原游戏、访问存档/云数据或commit/push。没有拒绝、等待批准或执行结果不确定的操作；后续parser仍是证据缺口。
