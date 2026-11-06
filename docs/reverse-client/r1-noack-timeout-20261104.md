# 自有观察器：无人确认超时与保持验证

2026-11-04 UTC。仅运行既有无害 fixture 和隐藏 LLDB，没有启动原游戏或可见窗口，没有访问存档或修改 Steam 设置。

## 实际结果

| 用例 | 真实缺失首请求超时 | 原句柄保持 | 期间状态 |
|---|---:|---:|---|
| no-hit | 5.016 秒 | 2.063 秒、18 个样本 | Running，StopID 1，断点命中 0 |
| foreign-stop | 5.032 秒 | 2.031 秒、18 个样本 | Stopped，StopID 2，同一线程/PC/exception reason 6 |

两例均没有发布请求、用户 ACK 或 end；最初附加后各有一次 Continue，此后 Stop/Continue/Detach/Kill 调用数均为 0。专用 `NoAckTimeout` 的真实等待、原因及 traceback 被保存，不能把任意异常归类成这个预期负例。原生身份句柄从附加到保持持续打开。没有重跑旧套件或重编译 fixture；两个独立 supervisor 各编译一次既有 Job helper。

控制器必须保持 fixture 的输入管道，直到 owner 验证 fixture 和 LLDB 的 PID、精确创建时间、路径、Job 归属与存活，并将证明原子写入清理 ACK。之后 gate 才 exit 88，owner 终止**自有测试 Job**并确认空。no-hit 此时也可能因管道 EOF 正常走到 fixture 的退出分支；本轮未单独判定其最后退出原因。这个收尾属于破坏性的测试清理，不是目标无损恢复。

两例启动回执均为 `completed-owned-noack-negative`，Job 空、已释放，无执行或清理失败。正式执行 session 79377 / 27705 均 exit 0；最终工具块为 `459dcd` / `98c2cd`。独立只读复核 `b6b112` 于 15:45:10 UTC 确认 14 个已记录 PID 均不存在，LLDB、fixture、原游戏及构建/Java/UE 互斥列表均为空。物理可用 15.344 GiB、提交余量 24.971 GiB。本批 20 个 owner 资源采样中，最低物理 15.102 GiB、最低提交余量 24.721 GiB，原 5 GiB 启动与 2 GiB 运行保护未改变。

## 证据与边界

- [no-hit 启动及释放回执](../../.tooling/lldb-noack-timeout-20261104/no-hit/launch-result.json)
- [foreign-stop 启动及释放回执](../../.tooling/lldb-noack-timeout-20261104/foreign-stop/launch-result.json)
- [独立释放复核](../../.tooling/lldb-noack-timeout-20261104/independent-release.json)
- [结构化结果及证据 SHA256 索引](r1-noack-timeout-20261104.json)

独立审查核验每例 16 项输入 pin，原始/冻结结果、后端快照及 ready/result/ACK 哈希一致；没有删除或改写旧失败回执。原版退出码 `0x80000003` 的根因仍未知，本轮结果不能解释其退出原因。

已验证的是**专用 NoACK 超时后的保持**。未穷举磁盘错误、身份丢失或任意未知异常的所有失败路径。当前安装绑定没有经验证、可保留未知异常原处理语义的自动 Continue/Detach 路线；因此没有试图无损恢复未知异常，也没有获得原版安全复试的充分证据。

真人接管仍未验证。现有隐藏 batch 会话在 Python 内保持，尚无经测试的用户命令入口。若继续接管路线，必须先提供同一个调试会话的可操作入口，再由用户核对目标身份、停止线程、PC 和异常证据并明确处置；不能另附加第二个调试器、关闭当前 LLDB，或把无人确认超时当作允许恢复。任何可见界面须先通知用户。下一步只应收敛这个入口与异常处置限制，不自动重启原游戏，不扩展成调试器工程。
