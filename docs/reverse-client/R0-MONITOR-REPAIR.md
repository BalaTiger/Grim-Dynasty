# R0 监控退出处理：隔离修复与验证

**后续状态：用户已授权生产接入，最小差异已接入并保留回退；06:54 UTC 真实 R0 8/8 通过，继续 R1。见 [R0-RETRY-RESULT.md](R0-RETRY-RESULT.md)。下文是隔离阶段的完整历史，不覆盖原失败或扩大因果结论。**

2026-10-22 UTC。状态：隔离候选已编译，修正夹具后的第二轮 36 断言／8 组通过；未修改生产监控，未再次启动 Rea/Ghidra。原 R0 错误 5 的实际根因仍未知。用户要求继续在现有权限内修复，不以未复现为调查终点；仍不得省略归属校验、提高权限或把拒绝读取视为成功。

## 已定位的实际失败

原始证据见 [R0-TOOL-BASELINE.md](R0-TOOL-BASELINE.md)。错误来自 `ReaOwnedJob.Probe` 中的 `QueryFullProcessImageNameW`，不是 `OpenProcess`，也不是 PowerShell 执行策略。该次同一 handle 已成功完成创建时间读取和 `IsProcessInJob`；查询映像返回 Win32 5，随后的零等待返回未收到退出信号。失败子进程的实际映像身份仍未知。

生产代码对查询句柄请求 `PROCESS_QUERY_LIMITED_INFORMATION (0x1000) | SYNCHRONIZE (0x100000)`。微软文档分别规定：[QFPI](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-queryfullprocessimagenamew) 与 [IsProcessInJob](https://learn.microsoft.com/en-us/windows/win32/api/jobapi/nf-jobapi-isprocessinjob) 接受有限查询权限；[WaitForSingleObject](https://learn.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-waitforsingleobject) 要求同步权限。这说明所请求的访问位符合接口要求，不能据此断言每个目标必然可读，也不能证明原错误来自哪种系统限制。

## 为什么保留映像查询

- 根进程创建时，路径用于将实际内核进程与固定 Node 启动器核对，随后绑定专用 Job。
- 活跃 Java 的映像路径与命令、角色、堆配置联合核验；进程名字或父 PID 不能替代。
- 创建时间和同一 handle 防止将 PID 重用误作原进程；明确的 Job handle 证明当前任务归属。
- 已收到退出信号的进程不进入活进程计数/Java 角色认可，原采样器会把它记录为已退出观察；这时允许映像不可用，不能回填虚假路径。

终止后进程对象会置为 signaled，持有的 handle 仍可在退出后使用，见微软的 [终止语义](https://learn.microsoft.com/en-us/windows/win32/procthread/terminating-a-process) 与 [句柄生命周期](https://learn.microsoft.com/en-us/windows/win32/procthread/process-handles-and-identifiers)。官方文档没有保证 QFPI 错误 5 一定表示退出竞态；本修复不作该推断。

## 有限候选变更

仅在映像查询报错 5、已确认属于本任务 Job、首次零等待仍未收到退出信号时，允许一次至多 100 ms 的同句柄退出等待。没有再次打开 PID、没有再次查询映像、没有替代 API 或额外权限。

只有该 handle 实际置为 signaled 才返回“已退出、映像不可用”的明确状态，保留原错误。等待超时、等待 API 失败、归属未知/非成员、其他未解释错误均不新增成功路径。现有 Job 清理、资源门槛、控制器 watchdog 和活进程身份要求保持原义。聚合等待也须有界，不能以成员数量累积出无界暂停。

这修复的是可审查的终止状态处理窗口；即使隔离测试通过，也只能称候选处理经验证，不能宣称原 R0 故障已解决。真实受控重试仍须后续资源时段、既有正式审批和原始响应证据。

## 最小验证范围

在生产源的隔离副本上测试正常查询、已归属进程在等待内退出、仍活跃超时、非成员不进入额外等待、未知错误及等待错误传播。保留真实创建句柄与 Job，只清理自有进程；故障注入明确标为测试数据，不能当原 R0 自然复现。通过前不提升生产候选，不启动原版、Rea 或 Java，不展开游戏函数调查。

## 首轮实际执行（保留失败）

06:31:19.243–06:31:21.564，经正式 `require_escalated` 审批运行普通用户 PowerShell 5.1 隔离测试。原候选 DLL 与测试派生 EXE 均编译成功，编译器输出均为空；夹具退出码 1。前 30 条检查通过，失败于“两成员枚举共享等待额度”预期，整套结果仍为失败。4/4 自有进程已确认退出，生产源和采样器前后哈希一致。

原始结果：[runner-result.json](../../.tooling/r0-owned-job-terminal-candidate-20261022/runner-result.json)、[test-result.json](../../.tooling/r0-owned-job-terminal-candidate-20261022/test-result.json)、[逐条输出](../../.tooling/r0-owned-job-terminal-candidate-20261022/fixture-output.txt)、[失败信息](../../.tooling/r0-owned-job-terminal-candidate-20261022/fixture-stderr.txt)。这是测试失败，非审批拒绝。该夹具未保存聚合动作的实际返回/异常码，需先补足精确诊断；不能直接据断言失败修改候选或称 R0 原故障已复现。

候选仅增加终止状态观察机会；每个公开 `Probe` 最多一次 100 ms，整个 `QueryMembers` 共享一次 100 ms，**不是整轮采样总额 100 ms**。真实进程、Job 与句柄仍使用系统 API；映像错误由测试注入，正向测试先结束已证明自有的进程再用真实等待确认，不证明自然竞态时间或原 R0 根因。

## 夹具前提修正依据

安装 Node 的版本为 24.14.0。该版本 [官方 libuv 源码](https://github.com/nodejs/node/blob/v24.14.0/deps/uv/src/win/process.c#L65-L91) 为普通子进程创建父退出时清理的内部 Job；[启动分支](https://github.com/nodejs/node/blob/v24.14.0/deps/uv/src/win/process.c#L988-L1035) 对 detached 子进程跳过该内部绑定，也未设置 `CREATE_BREAKAWAY_FROM_JOB`。因此首轮用例“终止任一成员后另一成员必定仍活着”的假设不成立。但首轮缺失实际返回、错误码和退出成员记录，不能据源码倒填那次发生的事实。

第二轮只修正夹具为 detached 子进程，继续独立验证真实 Job 归属、创建时间和未退出状态后才加入可终止对象集合；并记录聚合返回/错误码、退出成员及存活状态。候选监控不因夹具失败而修改。此标志只用于自有测试，不改变 Rea 启动或系统设置。

06:35:14.512 的 [只读资源快照](r0-tool-baseline/resource-release-during-monitor-repair.json) 无匹配 Rea/Java/诊断或 UE 重型进程，可用物理 6,136,266,752 B、提交余量 19,658,973,184 B。未再次发送 Rea 请求；该快照不代表锁定了独占时段。

## 第二轮实际通过

06:38:28.280–06:38:30.195，经另一次正式审批执行修正后的固定 runner。两次编译与夹具退出码均为 0，编译器及夹具 stderr 均为空。**36 断言、8 组通过，4/4 自有进程退出已确认**。这是同一套测试在夹具修正后的重跑，不累加首轮 30 条为新增覆盖，也没有重跑旧游戏套件。

原始结果：[runner-result.json](../../.tooling/r0-owned-job-terminal-candidate-20261022-attempt2/runner-result.json)、[test-result.json](../../.tooling/r0-owned-job-terminal-candidate-20261022-attempt2/test-result.json)、[实际诊断和断言](../../.tooling/r0-owned-job-terminal-candidate-20261022-attempt2/fixture-output.txt)。聚合用例实际记录 root 29556 退出、child 43872 仍活跃；同一次枚举只进行一次 100 ms 等待，第二成员继续抛错误 5，最终清理确认其退出。候选、测试、runner 及生产源前后 pin 均一致。

- 候选 `ReaOwnedJobTerminalCandidate.cs`：`6d07a2a1e29565930ebd091f070128aeb6a59feaafd8e8fcdce01bb7b4a743a9`。
- 测试 `OwnedJobTerminalCandidateTests.cs`：`c1d345ef478706df6419ffee795f4864a1cdb85075f8c131008ea69e6c7ad40f`。
- runner `test_owned_job_terminal_candidate.ps1`：`4cb76fb40aa84358a2ee5e43bf12e9b2c6583ed8d589c4a7e6f10add3732ffa9`。
- [修正前源码可恢复记录](../../.tooling/r0-owned-job-terminal-candidate-attempt2-source-diff.json) 保存两份旧文本，重新计算的哈希严格匹配首轮输入；首轮失败目录未覆盖。

06:39:24.931 的 [最终只读快照](r0-tool-baseline/resource-release-after-monitor-candidate.json) 无匹配 Rea/Java/诊断或 UE 重型进程，相关工作集合计为 0；可用物理 6,584,983,552 B，提交余量 20,370,853,888 B。本轮未占重型时段，没有新的 `procedure_pseudo_code` 请求。

## 精确续接点与边界

下一次真实 Rea 尝试前，先将已验证的最小差异接入生产监控并核对真实新哈希与调用方 pin；不带入测试注入或测试 detached 标志。原候选已经编译，生产集成仍未发生。随后重新取得可用资源及同机独占窗口，使用正式审批执行固定 Engine `0x180274ec0` 的 R0 基线，保留新尝试的独立输出。原失败与本轮测试目录均不得覆盖。

若真实活跃进程再次无法取得映像，且同一句柄在有界等待后仍未退出，则继续拒绝并保存真实错误，不能放宽权限或把未知进程当作 Java。本轮没有证据要求管理员权限、修改 ACL 或系统策略。只有真实 Rea 返回伪代码后，才可推进已准备的 R1 Engine `077850`／`229700`；不重新展开 PE 函数调查。

已验证的是退出处理合同与自有夹具。未验证的是原 R0 失败根因、候选对真实 Rea 的效果、短命进程资源峰值、原版动态行为及客户端兼容性。保留句柄身份不使分次采样成为原子快照；候选未改变原有计数边界。此处按本轮轻量隔离修复范围封存，生产监控与采样器保持原哈希。
