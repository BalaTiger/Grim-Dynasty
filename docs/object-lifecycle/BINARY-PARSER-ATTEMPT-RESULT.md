# Binary parser 固定查询尝试：guard 停止，未取得函数体

2026-10-20，时间均为 UTC。04:58 的固定 8-call 批次实际发出 **4 次调用，完成 3 次**；第 4 次 `procedure_pseudo_code(0x18013F510)` 未返回分析结果，随后因进程监控 guard 停止而记为失败。没有取得 parser 伪码、汇编或 callees，不能据本次尝试实现 parser、构造空 record 成功，或把 LoadFile miss 改为成功。

所属进程已退出，后续已清理该次运行的临时目录，最新记录为 **runtime 剩余 0 文件**。正常 MCP close 和最终 closed-session 状态仍未确认。这是运行期 guard 中止，**目前没有审批拒绝记录**。

本页仅读取保存的执行记录并写入结论；没有运行后端、构建、读取原版文件或修改行为代码。

## 实际执行范围

[summary.json](rea-binary-parser-entry/summary.json) 记录批次开始于 `04:58:22.999Z`，结束于 `04:58:26.835Z`，状态为 `failed`。计划 SHA256 为 `adf41b0f3ec4d59f11352b21eb3173c8cb077a638a452c0f21f488b6b21251ff`。固定目标为 Engine `0x18013F510`；记录中的原版前后 SHA256 均为 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`，`targets_launched=false`。

[调用进度](rea-binary-parser-entry/progress.ndjson) 与 summary 一致：

| 调用 | 实际状态 |
| --- | --- |
| 1：`binary_session` | `04:58:24.539Z` 完成，passed。 |
| 2：`open_binary` | `04:58:24.551Z` 完成，passed。 |
| 3：`binary_session` | `04:58:24.580Z` 完成，passed。 |
| 4：`procedure_pseudo_code(0x18013F510)` | `04:58:24.580Z` 发出；没有收到分析结果，`04:58:25.575Z` 因 guard 错误记录为 failed。 |
| 余下计划调用 | 未执行；没有 parser 汇编／callees 结果，也没有完成计划内的正常 close 和最终 session 确认。 |

`tool_calls=4` 与 `completed_plan_calls=3` 不能写成“4 次分析成功”或“8/8 完成”。第 4 次错误来自监控中止，不是已经取得的函数体报告，也不是 parser 本身的成功／失败行为。

## 停止原因与未知 Java 归属

[owned-memory.ndjson](rea-binary-parser-entry/owned-memory.ndjson) 中：

- `04:58:23.2168819Z` 的采样没有列出外部 build/Java。
- `04:58:24.8462633Z` 的采样在 `external_build_or_java_processes` 中列出 **`java.exe PID 24988`，`ParentProcessId 23716`**。该采样没有把它列入已确认的 owned backend tree。
- `04:58:25.368Z` 的 progress 事件为 `memory-stop`，错误原文为 `Owned memory monitoring: Another UE/build/Java process is active`，该事件的 `free_gib=null`。
- `04:58:26.3783170Z` 的后续采样中，`live_owned_identity_count=0`，外部 build/Java 列表也为空。

这里的“external”是采样器当时的分类，不是已完成的进程归属证明。现有记录没有提供 PID 24988 的命令行、创建身份及通往已知 owned 根的完整父链，也没有确认 PID 23716 的归属。因此，不能断言该 Java 属于另一个任务，也不能断言它只是本次 Rea/Ghidra 的短命辅助或探测进程。**短命辅助 Java 的解释仍是未证实假设**；前后采样中出现／消失本身不能补足归属证据。

summary 将停止记为强制终止：`forced_termination=true`、`process_exit_confirmed=true`、子进程退出码 `1`。这与正常 MCP 清理成功有区别；不能以进程退出改写 `close_acknowledged=false` 或 `closed_session_verified=false`。

## 预算保持不变，观测余量未触线

本次采用 `engine-parser-384m-v1` 预算：

| 项目 | 固定配置／记录 |
| --- | --- |
| Java heap 配置 | **384M** |
| 启动物理余量和系统 commit 余量门槛 | **两者均 ≥4.25 GiB** |
| 运行停止门槛 | **任一余量 <2 GiB**；恰好2 GiB允许 |
| 预算中的任务余量 | 2.25 GiB |

这些数值不因本次失败上调或下调。本页没有修改预算、guard、计划或 runner，也没有登记一次已准备、已获批准或已启动的重试。

[启动检查](rea-binary-parser-entry/launch-preflight.json) 的可用物理内存为 `12,682,199,040` 字节，且未列出外部 build/Java。owned 采样的最低可用物理内存为 **`12,068,384,768` 字节，约 11.240 GiB**，最低系统 commit 余量约 **23.580 GiB**，均没有显示物理余量触及上述门槛。summary 的另一组 memory 采样最低值为约 11.262 GiB；两个采样口径不合并为一个更精确的值。

因此，已记录的直接停止原因是未能归入 owned 树的 Java 被 guard 检出，**不是已观测到的物理内存不足**。`memory-stop` 事件名称及 `stop_triggered=true` 不能单独解释成内存阈值触发。

`expected_heap_observed=false`：384M 是本次配置值，尚未采得匹配的 Java heap 开关，不能冒称已从实际 JVM 命令行验证。采样只有 3 次；短命进程、最后采样间隔及未采得的峰值仍有局限，不据此保证未来峰值或修改预算。

## 进程释放和 runtime 清理的最终状态

按时间区分两份记录：

1. [resource-release.json](rea-binary-parser-entry/resource-release.json)，`04:59:38.2697770Z`：`remaining_owned_identities=[]`、`owned_processes_released=true`、`selected_live_processes=[]`。所属进程已经释放，但当时临时目录仍有 **4 个文件**，故 `runtime_empty=false`。`normal_close_confirmed=false`。
2. [runtime-cleanup.json](rea-binary-parser-entry/runtime-cleanup.json)，`05:01:12.4818204Z`：确认 owned 进程不在后，仅删除已验证停止的该次运行目录；`removed=true`，**`remaining_runtime_files=0`**，`game_original_touched=false`。记录还明确：较早一次因时间值比较未通过而没有删除，最终采用 UTC 时刻比较后完成清理。

所以最新文件清理状态应采用 **05:01 的剩余 0 文件**，不能继续把 04:59 的 `runtime_empty=false` 当作最终状态。两份原始记录都保留，不相互覆盖或改写。最终可确认的是“owned 进程已退出、该次 runtime 残留已清理”；**正常 MCP close 未获确认**仍然成立。

## 对实现和后续工作的影响

[先前的零 count／槽绑定证据](BINARY-SUBOBJECT-EVIDENCE.md) 仍然支持正常构造返回路径上的 `1C4FF4 → 13F510`。本次失败没有取得该目标的函数体，未增加 parser 协议、reader 游标／释放标志、字段写入或成功判定的证据。

现有 `RecordLoadFile` miss 继续属于未实现范围；不能用 supplied callback、预填 typed snapshot 或零字段默认值代替原 parser。基类 `13C1D0`、完整 miss 发布及其他既有边界也没有因此闭合。

本次记录停在失败原因、资源释放和清理结果，不表示已安排或获准自动重试。没有审批拒绝需要解除；尚需厘清的是未知 Java 的归属与 guard 分类，而不是绕过监控。没有运行新的查询、构建或测试，既有验收数字不作为本批新执行结果。

已安装实现复核指出：[WindowsOwnedProcess.js](../../.tooling/rea/node_modules/rea-agents/dist/windows/WindowsOwnedProcess.js) 的 launch 仅返回 leader PID、opaque handle 及 Job 保证，poll 仅返回 `activeProcesses` 数量，没有成员 PID／创建时间；[原生README](../../.tooling/rea/node_modules/rea-agents/native/windows/README.md)将其描述为 unnamed、non-inherited Job；[ProcessOwnershipObservation](../../.tooling/rea/node_modules/rea-agents/dist/process/ProcessOwnershipObservation.js) 的 win32 `listProcesses` 直接返回空数组。因此当前没有可直接复用、低改动的成员身份接口，不能仅凭 Job 保证或进程数量把未知 Java 自动纳入 owned 树。

另有独立角色检查问题：已安装启动器会在主 JVM 启动前运行 `java -version` 和 `LaunchSupport` 辅助阶段，这些调用并未传入主分析堆参数；当前 profile 却对所有已归属 Java 统一要求 `-Xmx384M`，可能导致再次误停。必须先证明归属，再验证角色，辅助进程仍应计入任务内存，主分析 JVM 继续验证384M；不能只根据名称或路径豁免。

相关既有 27 项测试未覆盖此次拓扑，本次没有重跑行为测试。后续若处理该问题，须先取得并独立验证严格的成员身份、生命周期及辅助 Java 角色证据；当前不修改监控、不自动重试。后续只读复核仍为已知自有 PID 不存在、runtime 0 文件；精确清理依据保留以上两份带时间戳的原始记录。源文件哈希、行号及未完成前提见[归属诊断](parser-query-validation/startup-attribution-diagnosis.json)；执行文件未变及失败记录一致性见[离线封存核验](parser-query-validation/stopped-attempt-verification.json)。
