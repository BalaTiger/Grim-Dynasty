# Parser guard repair validation — 2026-10-20

本记录验证进程归属、资源守卫及启动门控。守卫测试使用项目自有 Node、PowerShell、.NET fixture 或保存的输入，没有启动 Rea、Java、Ghidra，没有读取原游戏二进制或玩家数据。后文另引用真实 parser retry1 的失败与清理记录；守卫测试通过不代表 parser 已查询成功，也不增加游戏行为恢复的验收断言。

| 验证 | 最终结果 | 证据 |
| --- | --- | --- |
| 纯 Node 三套测试 | **76 项通过**：Java role 21 + Job profile 28 + resource budget 27；主线程最终工具回执 exit 0、134.2587 ms | 完整联合 stdout 未保存；已有 [Job profile 单批 28 项日志](parser-guard-repair-validation/job-profile-tests.txt) 保留 |
| Windows x64 真实 Job fixture，含退出竞态 | **最终 115 条断言通过**，0 cleanup errors、`error=null`；WinPS 5.1.26100.9711，耗时 8.7698295 秒；实际覆盖映像读取失败且同句柄确认退出的分支 | [最终原样结果](parser-guard-repair-validation/native-job-fixture-exit-race-result.json) |
| 历史 Windows Job fixture | **113 条断言通过**，0 failed checks、0 cleanup errors、`error=null`，耗时 8.9299435 秒；对应退出竞态修复前的 native 源码 | [保留的历史结果](parser-guard-repair-validation/native-job-fixture-result.json) |
| 生产 wrapper 的未释放门控联测 | `passed=true`、`gate_released=false`、Job 空、sampler exit 0 且输出排空；root exit **92** 是本次显式 Job stop 的指定退出值 | [原样复制的结果](parser-guard-repair-validation/start-gate-result.json) |
| 固定 parser 查询计划 | 主线程离线校验通过，仍为固定 **8 calls** | [固定计划](../../tools/static_analysis/rea-binary-parser-entry-plan.json)；不表示本记录执行了这些 calls |

先前“75”是手工加总错误；最终联合数为 **21 + 28 + 27 = 76**。以上联合运行的数字来自主线程工具回执，没有重建或伪造完整日志。三套测试的对应复现入口为：

```powershell
& 'D:\NodeJS\node.exe' --test tools/static_analysis/rea_java_role.test.mjs tools/static_analysis/rea_job_memory_profile.test.mjs tools/static_analysis/rea_resource_budget.test.mjs
```

真实 fixture 的固定执行入口为下列命令，不使用 `ExecutionPolicy Bypass`，不修改系统策略：

```powershell
& "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe" -NoLogo -NoProfile -NonInteractive -File 'C:\Users\zhuzi\Documents\Grim-Dynasty\tools\static_analysis\test_owned_job.ps1'
```

## 真实覆盖与清理证据

[最终 115 条结果](parser-guard-repair-validation/native-job-fixture-exit-race-result.json) 保留并通过了历史 113 条覆盖：PID 创建身份及可执行文件不匹配时拒绝、具体 Job 的成员关系、短命父进程退出后的存活子进程、同目录无关 Node 不属于 Job。`PROC_THREAD_ATTRIBUTE_JOB_LIST` 创建的内层 Job 子进程仍属于外层 Job，也出现在外层成员列表中。

测试分别检查根先退出但子进程仍存活、显式 TerminateJob、最后 Job 句柄关闭、异常 finally、controller 死亡以及 Job owner 被强制停止时的清理；无关对照 Node 均保持存活。有效 stop、EOF、非法命令及无输入 Dispose 都有实测；没有把仍有成员的 Job 报为清理成功。最终 retained-process 清理未记录错误。这里只引用这些 fixture 的记录，没有用一次根退出推断所有后代已退出。

门控联测中 wrapper 始终没有收到 release，因此没有导入 Rea。记录中的 `expected_heap_observed=false` 是未启动 Java 的预期结果，不是已经验证了真实 JVM 的堆参数。该运行只证明真实 wrapper、sampler、Job 控制及退出路径能在门控关闭时协同工作。

## 两次早期 fixture 失败

前两次均通过 18 条断言后在成员列表断言停止，原始结果保留在：

- `.tooling/owned-job-fixture-70542dd95e3f4fb9987687fd75cb2c62/result.json`
- `.tooling/owned-job-fixture-6c28aec057f94398bc38f55caaf7620f/result.json`

fixture 当时给短命父进程启动的 leaf 仅调用 `unref()`，没有使用 `detached:true`，未建立所需的“父退出后 leaf 持续存活”前提。第二次诊断在先前 Probe 时仍见 leaf 存活，紧接着成员列表已不含它，不能据此认定 native 成员枚举有错，也不能据该记录确定终止它的精确内核时点。修复只改变 fixture 的生命周期，并加强成员查询前后 `InJob && !HasExited` 断言；native 实现及成员匹配数量要求没有放宽。随后 113 条通过，查询前后均观测到 leaf 存活且列表内恰有一个匹配成员。

## 退出竞态修复与 retry1

真实 [retry1 summary](rea-binary-parser-entry-retry-1/summary.json) 记录 2026-10-20 05:29:45.906–05:30:35.701 UTC：已发出 4 calls，前三项完成，第 4 项 parser 伪代码请求未成功返回。sampler 的 [stderr](rea-binary-parser-entry-retry-1/owned-memory-sampler-stderr.txt) 在 `Probe` 内记录 `QueryFullProcessImageName` 失败，sampler exit 1；该批次仍是失败，正常 MCP close 和 Job 空状态均未由运行中监控确认，不能把 child exit 0 当查询成功。

独立 [resource-release](rea-binary-parser-entry-retry-1/resource-release.json) 随后确认记录的 **26 个进程身份全部退出**（remaining same identity 0、无复用 PID），存活分析进程 **0**、无存活分析 Java；已确认属于该批次的 runtime 清理后剩余文件 **0**。这些是后续释放证据，没有改写失败 summary。

最小 native 修复仍在同一持有的进程句柄上读取创建身份、成员关系和退出状态。映像查询失败时，只有该句柄随后确认已退出，才保留退出证明并返回空路径及错误诊断；进程仍存活或退出状态查询失败仍抛错，不按路径猜测归属。

最终 fixture 在已持有原进程句柄、并明确等待根退出后执行 Probe，实际观测到 `image_unavailable_branch_observed=true`、`ExecutablePathErrorCode=31`、`ExecutablePath=null`、`HasExited=true`，且 PID/创建身份一致。因此此次已经实测映像不可读的退出分支，未仅以成功路径代替。结果对应主测试 SHA256 `375095e8eb23913d366ab26d113487bb038ecba7e46b1376073ecb58227561a8`、native SHA256 `aaa8b1a58c16587457b2b3d015d0209ff9546a11d2c3350fb37b5648a77fee12`；历史 113 条结果保持原样。

## 生产守卫边界

- 先以专用 Windows Job、PID 与创建身份证明归属，再分类 Java 角色。历史父 PID、同路径或进程名不能替代归属证据；不采用通用 Java 白名单。
- controller 与 sampler 留在专用 Job 外；wrapper 在归属验证和首个有效样本完成前保持门控。失败、失联及控制通道异常只清理该专用 Job；不启用 breakaway、不更改宿主 Job 或安全策略。
- 固定角色匹配区分启动探针与主分析 JVM。主分析要求本轮固定 `384M`；探针不因缺少主 JVM 堆参数而被当作未知外部进程，但必须先满足归属和精确角色契约。未知 Java、未知命令形状仍停止。
- 启动前可用物理内存与 commit headroom 均须至少 **4.25 GiB**；运行中任一低于 **2 GiB**、外部重负载、监控断联或无效数据均使本轮失败。正常收尾仍检查预算及晚到错误，以 Job 空和退出确认完成清理。
- 样本总和不是连续峰值；短命进程和末尾采样间隔仍可能漏测。内存采样、Java 命令行及本机嵌套 Job fixture 不能证明任意未来宿主环境兼容。pre-assignment conhost 不被追认为该 Job 成员。

三份 JSON 使用 CreateNew 写入，未覆盖旧证据，复制前后字节及 SHA256 一致：

| 文件 | 字节 | SHA256 |
| --- | ---: | --- |
| `native-job-fixture-result.json` | 37080 | `18ee1a69e93e4f1ef443f845288f19a6124c1fb5b61df7ec9f4080a6f1f7f4a5` |
| `native-job-fixture-exit-race-result.json` | 39415 | `44c0b0387a167f447061875f0981aba1106577550e1bdd295c587015409d05ca` |
| `start-gate-result.json` | 3122 | `0a9f101edeea18c40bdb231b8d65d547a0f304f77c3f77ce52d6dbe65731a8f0` |
