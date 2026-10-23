# 初始化、计时与完整原 ID 缓存链验收

2026-10-20。本轮补齐了正常成功构造下的 fresh archive/depot 前提，并将完整原 ID 字符串池接入自有读取、解析、getter 和缓存链。自有回归 **2264 条断言、83 组场景**通过；同一正式 `scrapmetal.dbr` 的受限探针另有 **123 条断言**通过。正式样本结论限于下述单条自有链路，完整原生兼容仍未验证。

工程：`C:\Users\zhuzi\Documents\Grim-Dynasty`。游戏安装：`D:\Steam\steamapps\common\Grim Dawn`。HEAD 仍为 `6dfdae419d1c92f7738eefe241937057d83134df`，没有 commit/push。

## 已闭合的行为

- 从已保存调用证据和固定函数片段确认：fresh archive 的 map/selector 初始状态、正常首次导入的零偏移 `[0]`、单偏移时跳过 remap 的原因，以及 query cache 的 shift 2。没有根据“只传了一个 ARZ”推断原生初始化。
- fresh depot 的正常空缓存为 8 桶、容量 8、负载因子 1.0，加载/命中/累计时间/最大时间为零。原生 gate、fallback 对象和分配器成功是静态正常路径的前提，自有 gate 为明确输入。
- `BinaryStringPool` 保留完整原始顺序及 ID，用全字节索引查询，按需建立稳定 `TableString` 身份。索引是自有实现；`NativeLayoutKnown=false`，不会伪造原生哈希桶、链长或地址。
- `ForFreshUniqueArchive` / `ForFreshUniqueArchiveWithClock` 自动生成完整 name-ID 上下文和初始化 pointer cache。现有多归档生成映射链保持通过；新大池接口只覆盖正常 fresh、单归档、字符串唯一且已含空串的情况。
- 计时转换独立为纯函数：QPC 使用 64 位环绕减法/乘 1000、带符号除法、低 32 位；毫秒路径使用 uint 环绕减法。未知输出槽和两个除法故障不替换成零值。起止模式可分别选择。
- 时钟只在实际 miss 的读取前、缓存提交和加载计数递增后采样。起始失败没有读取或提交；结束失败保留已发生的提交/计数，并维持 unresolved 状态。重复命中、关闭来源后的命中及同 attempt 重放均不重复读取或采样。

## 正式样本实际结果

只解码 `records/items/questitems/scrapmetal.dbr` 的固定 **323 字节压缩块 → 456 字节正文**。必要元数据是完整字符串表和记录目录；没有解码整库记录。

| 项目 | 结果 |
| --- | --- |
| 完整字符串池 | 82,999 项，原顺序和 ID 保持 |
| 记录名 / 空串 ID | 6883 / 82687 |
| `Class` / `QuestItem` ID | 13 / 28355 |
| parser | 38 字段逐项与已冻结正文比较，7 个布尔值，位字 `0x30` |
| 初始 pointer cache | shift 2，16,384 桶，容量 41,499，used 1 |
| 链路 | 首次 miss → parser → publication → getter → repeat hit → closed-source hit |
| 计数 | load 1，hit 2 |
| 字符串物化 | 最终仅 2 项、14 字节 payload |
| 关闭来源后的新 depot miss | 在解析/提交前报告 `System.ObjectDisposedException`，重放无重复副作用 |
| 格式读取 | 5 次、5,321,614 字节；另有两次 64 KiB 分块全文件指纹读取 |
| 探针进程峰值工作集 | 32,251,904 字节，约 30.76 MiB |

探针显式初始化自有 `Stopwatch` 基准和频率，真实起止转换得到 6/82，elapsed 为 76。这个数值仅记录本次探针，不是性能承诺，也不是 Engine 四个原生计时全局变量的初始化证据。第三次采样属于关闭来源后的新 depot 失败尝试；该次未进入读取成功、解析或提交。

## 测试与内存边界

新增 293 条断言、26 组场景。完整相关回归首次运行通过，耗时 12.8111331 秒；包括旧的多归档去重、同串异 ID、重复来源、顺序与重映射加载测试。没有将旧十模块打包验收描述为本轮重新执行。

正式探针首次编译因缺少方法闭合大括号失败，尚未生成/运行探针、未读正式样本；修复后一轮编译和首次运行通过。该修复只涉及探针，不需要重跑已经通过的产品回归。

预算为显式自有覆盖限制：raw 数组工作预算 8 MiB，完整池最多 131,072 项、8 MiB wire / 4 MiB payload，按需物化最多 1,024 项 / 256 KiB payload，单链保守数组计量上限 24 MiB。旧接口 4096 项 / 2 MiB 限制不变。24 MiB 不包括对象头、CLR/JIT/GC 和 OS 文件缓存，不声称是进程硬上限。本次保留 raw 数组 payload 4,643,134 字节，池索引与 memo 引用数组估计 995,988 字节。

## 仍未验证的边界

- Engine 计时 mode/base/frequency 等四个全局变量的 writer 与完整初始化；本次未追踪。
- 原生 allocator/OOM/EH、真实 critical section、fallback table 的构造及失败恢复、运行时地址和本机 ABI。
- 非 fresh 的完整归档 acquisition/合并生命周期、所有 selectors/offsets 选择；初始重复字符串造成的原 ID 对应仍按已有缺口明确拒绝。
- 一般字段数量及 CRT log 临界舍入；既有 16/17、32/33 字段拒绝和 pointer-cache 临界计数拒绝保持。
- 完整物品工厂、具体 `Read`、原生注册、全库兼容以及存档兼容。本工程不是 mod，不承诺恢复原始源码。

## 证据保全

本轮 dumpbin 固定片段共 **1490 字节、372 条指令、9 次调用**；边界/IAT 元数据读取和流式全文件指纹另行记账。没有整库反汇编或自动追踪未知调用。

Engine 原件与既有只读副本的读取前后 SHA256 均为：
`4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`

正式 `database.arz` 读取前后 SHA256 均为：
`9e3532250cdb611a4cc989997c8f97fcec2d6c422063a845e6c43cd538c30603`

未启动原游戏/Rea/Java，未安装、提权、重启或修改系统安全配置，未访问玩家存档或 Steam 云。

- [Archive 初始化证据](ARCHIVE-INITIALIZATION-EVIDENCE.md)
- [Depot 与计时证据](DEPOT-TICK-EVIDENCE.md)
- [产品回归日志](native-init-tick-validation/test-output.txt)
- [机器可读验收](native-init-tick-validation/acceptance-native-init-tick.json)
- [正式探针实际读区间及结果](official-fresh-cache-validation/probe.json)
- [本轮保全审计](native-init-tick-validation/preservation-audit.json)
- [恢复清单](../../.tooling/checkpoints/native-init-tick-complete-retry1/manifest.json)

历史 preflight 阻断报告保留不改；本结果取代其中已由本轮证据及实现闭合的 fresh 初始化、完整池和显式计时接线缺口。

首次封存的扩展名防护也拒绝了测试生成的自有 `.arz` fixture，停止后保留 54 个已复制文件且没有发布清单。恢复封存排除可从源码重建的 fixture，写入新的 `native-init-tick-complete-retry1` 目录；没有重跑测试或原版读取。
