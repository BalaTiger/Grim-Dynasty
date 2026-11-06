# 同一 LLDB 会话的只读输入验证

本阶段范围为已有无害 fixture、隐藏 LLDB、同一会话的只读命令。原游戏、Steam、存档与可见应用均不启动或修改。结果以本文件末尾实际执行段及结构化回执为准。

## 工具与官方合同

本机是 `LLDB 9.0.0 (JetBrains IDE bundle; x86_64; rider-lldb-9; build 367)`，复用已保存版本输出；未重复运行版本测试。安装的 Python binding SHA256 为 `18c0d00039e6b622d73bd2d662a04a10944c5c4855d07ce083d346bd216da9c6`，包含命令解释器与 debugger/process 唯一 ID。其 `SBProcess.Continue()` 和 `Detach()` 均无异常处置参数，不能直接套用新版文档的重载。

- LLVM9 Windows `DoResume` 对当前异常使用 `MaskException`，分离路径也可能将异常标为已处理。`process handle` 处理的是 UnixSignals，不能据此认定 Windows SEH 会传回应用。这里是**上游实现证据**，不是 JetBrains 私有 fork 的完整动态证明。[ProcessWindows](https://github.com/llvm/llvm-project/blob/llvmorg-9.0.0/lldb/source/Plugins/Process/Windows/Common/ProcessWindows.cpp#L183)、[DebuggerThread](https://github.com/llvm/llvm-project/blob/llvmorg-9.0.0/lldb/source/Plugins/Process/Windows/Common/DebuggerThread.cpp#L184)、[process handle](https://github.com/llvm/llvm-project/blob/llvmorg-9.0.0/lldb/source/Commands/CommandObjectProcess.cpp#L1348)。
- Win32 区分 `DBG_CONTINUE` 与 `DBG_EXCEPTION_NOT_HANDLED`；后者把 first chance 留给应用处理，但未处理的 second chance 仍可终止进程。“转交异常”不能承诺进程存活，也不能从 LLDB 的 Python 线程绕过其调试事件循环直接调用底层继续接口。[Microsoft ContinueDebugEvent](https://learn.microsoft.com/en-us/windows/win32/api/debugapi/nf-debugapi-continuedebugevent)。
- `--batch` 会在命令结束后退出；同会话入口应在启动时预设。LLVM9 driver 在启动命令后转入 stdin 命令循环。`FindDebuggerWithID` 不是跨进程接管接口，新开 `lldb -p` 也不是连接原调试会话。[CLI](https://lldb.llvm.org/man/lldb.html)、[LLVM9 Driver](https://github.com/llvm/llvm-project/blob/llvmorg-9.0.0/lldb/tools/driver/Driver.cpp#L600)。

## 有限验证设计

只启动一个既有 `debugbreak` fixture。LLDB 从启动即使用 stdin pipe、非 batch、无排队 quit。准备函数只在身份核实与真实停止后返回 CLI，并以会话全局强引用保留原生句柄。控制器只发两条原生只读命令 `process status`、`thread list`，及三次固定身份快照；核对同一个 debugger ID、process UniqueID、PID/创建时间、句柄、StopID、线程 PC/reason/data。

最终快照保存后不再返回 CLI，避免 Node 退出关闭输入管道，导致 LLDB 自动 quit/Detach。控制器与 owner 通过已有 ready/ACK 协议核对两份活身份，再只清理自有 Job。此清理是终止测试对象，不是无损恢复。未复跑先前的 NoACK 等套件。

## 人工接管边界

隐藏 pipe 的读取端属于同一 LLDB，但写入端目前属于测试控制器；自动命令成功不等于真人已经接管。下一次真人只读验收需先向用户说明并通知具体界面或受限命令入口，在启动前连接到同一会话。用户需核对目标 PID/创建身份、停止线程、PC 与异常证据，再明确请求只读查看；不得把 `continue`、`detach`、`quit` 或关闭窗口当作安全退出。

人工参与不能补足缺少异常处置能力。当前仍无已证的未知异常无损恢复路线，本实验也不构成原版安全复试证明。父线程已另行确认用户授权新的原版单次命中追踪；其后已通过 Steam 正常启动原版并进入既有角色场景，尚未附加调试器。该授权不改变未知异常禁止自动 Continue/Detach 的边界，也不把自有测试的破坏性 Job 清理迁移到原版。

## 实际执行

本批已完成真实执行及独立只读复核。正式执行 session `45091`，exit `0`，final chunk `e9878b`；owner 状态为 `completed-owned-same-session-readonly`。本阶段只运行一个既有自有 `debugbreak` fixture，没有运行原版、Steam、Rea 或可见应用，也未重跑旧套件。17 项固定文件指纹均匹配，准备文件及历史失败回执保留。

实际 stdin 输入恰为以下五条；前三次快照之外的 baseline 由准备函数生成，共四次快照：

1. `process status`
2. `script gd_same.checkpoint("after-status")`
3. `thread list`
4. `script gd_same.checkpoint("after-threads")`
5. `script gd_same.checkpoint("final")`

原始 stdout 证实两条只读 CLI 命令确在同一会话执行。四快照处于 `2026-11-04T16:09:21.609128Z` 至 `16:09:21.755631Z`：LLDB PID `51024`、fixture PID `53156`、debugger ID / process UniqueID / selected process UniqueID 均为 `1`；创建 FILETIME `134382821605419577`、句柄 `0x2f8`、state `5`、StopID `2` 与全部六条线程的 reason/data/PC 保持一致。原生句柄始终 `wait=258`、`exit_code=259`，不能据此称进程已经退出。

准备阶段仅有一次 Continue 离开初始附加停止；真实 DebugBreak 停止之后，Stop / Continue / Detach / Kill 均为 `0`。主线程 `0xa688` 的 reason `6`、数据 `0xffffffffffffffff`、PC `0x7ffbedb43123` 被原有检查明确拒绝，`keep_stopped=true`。backend 记录 first-chance `0x80000003`，stdout 没有 `unexpected continuation`。这些记录没有证明异常可以安全恢复。

最终快照保存后留在原 Python 调用内持有句柄，不返回 CLI。owner 于 `16:09:22.5283183Z` 证明 fixture 与 LLDB 的准确创建身份均属于自有 Job 且仍存活；gate 于 `16:09:22.550Z` 读取 ACK 后才以 `88` 退出。随后 owner 明确终止自有测试 Job，确认空 Job 并释放，`failure` / `cleanup_failure` 均为 null，于 `16:09:22.6099471Z` 完成。这里是**破坏性清理自有 fixture 和调试器**，不是正常退出或无损恢复。

独立释放检查 session `25777`、final chunk `cac767` 的既有回执时间为 `16:11:04.7199838Z`：八个记录 PID 均不存在，相关调试器、fixture 或原版进程列表为空。本次归档没有重新采样资源或进程。

结构化结果与全部阶段证据哈希见 [r1-same-session-stdin-20261104.json](r1-same-session-stdin-20261104.json)。主要原始证据：

- [owner 执行与释放回执](../../.tooling/lldb-same-session-stdin-20261104/launch-result.json)
- [五条输入与汇总结果](../../.tooling/lldb-same-session-stdin-20261104/result.json)
- [真实 CLI 输出](../../.tooling/lldb-same-session-stdin-20261104/foreign-stop-debugger.stdout.log)
- [原始探针结果](../../.tooling/lldb-same-session-stdin-20261104/probe-result.json)
- [独立释放回执](../../.tooling/lldb-same-session-stdin-20261104/independent-release.json)

已通过范围仅为：同一个隐藏 LLDB 会话可接受两条受限只读命令，期间未知停止保持。真人接管、异常无损恢复、异常输入丢失后的可恢复性及原版游戏复试安全均未由本批验证；没有新增原版运行或可玩性结论。

## 后续原版现场（独立于本批自有测试）

父线程的新 UI 回执记录于 `2026-11-04T16:21:24.1298387Z`：Steam 显示离线、Grim Dawn 单游戏云开关关闭，未修改设置；通过一次 Steam Library Start 正常启动原版，既有角色 `coldbreath`（75级）已进入库恩堡，游戏 HWND 为 `10617210`。没有创建或改变角色、恢复或覆盖存档，尚未附加调试器，也没有新的移动命中采样。该信息在本阶段归档后才传回，已更正当前恢复指针；最初“未启动”的归档状态仍保存在检查点历史中。

这只是原版现场与后续准备状态，不是逆向程序可用性或安全追踪结论。详见 [原版重新进入场景回执](r1-original-relaunch-ui-20261104.json)，SHA256 `ed580a49cf10b1b9ab505af7d154ed1dc520bf9f75ef24f250ac6805f634a63f`。后续附加仍需准确进程身份及原有实时保护；不得因自有测试通过而自动恢复未知异常。
