# Binary 指针缓存增长：slot 迁移证据

2026-10-20。本文只读取已保存的查询与资源记录，说明 `Engine 0x180284200` 的静态行为；没有再次调用后端、读取原游戏、编译或测试。实现及验收结果另行记录。以下地址是固定 Engine 映像的 preferred VA，不是推定运行时地址；反编译伪码不是原始源代码。

本机实际 **stdio MCP 服务已验证成功**：[initialize](rea-binary-pointer-growth/initialize.json) 返回 `rea 6.5.0` 与协议 `2024-11-05`，随后有界 MCP 请求实际取得 Ghidra 结果并完成关闭。这与“当前 Codex 对话工具清单没有直接注入 Rea 工具”是两件事；缺少对话内直接工具入口不代表本机 Rea 服务不可用，也不能把配置存在本身当成连接成功证据。本轮依据实际请求记录作出上述判断，没有为本文补做连接或分析请求。

## 本次查询与释放

[summary](rea-binary-pointer-growth/summary.json) 记录 **8/8 calls passed**，仅查询目标的伪码、汇编、直接 callee，加既有会话/open/close 检查；没有自动展开新 callee。开始于 `01:45:49.895Z`，结束于 `01:47:27.431Z`，总计 **97.536 秒**；首个分析请求耗时 **88.866 秒**。这些是静态查询耗时，不是实现或测试耗时。

- 记录中的目标为 `D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll`，查询前后 SHA256 均为 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`；`targets_launched=false`，没有执行原 DLL。
- 采用 `-Xmx512M`；[内存采样](rea-binary-pointer-growth/memory.ndjson) 首次空闲 **7.690 GiB**、最低 **6.524 GiB**，2 秒间隔共 50 个样本，未触发 5 GiB 停止阈值。6 GiB 启动阈值与采样监控不是操作系统内存预留，Java heap 上限也不是进程树总内存上限。
- 正常 close 已确认，最终会话关闭，Rea exit code 0，无强制终止、无记录错误。[host 释放复核](rea-binary-pointer-growth/resource-release.json) 于 `01:47:44.8115886Z` 确认 owned Rea **20744** 与 Java **21120** 均无匹配进程，runtime 文件列表为空；该次复核空闲内存 **7.515 GiB**。

## 函数行为与原生写入顺序

来源为 [04 伪码](rea-binary-pointer-growth/04-Binary-pointer-growth-0x180284200-pseudocode.json)、[05 汇编](rea-binary-pointer-growth/05-Binary-pointer-growth-0x180284200-assembly.json) 和 [06 直接 callee](rea-binary-pointer-growth/06-Binary-pointer-growth-0x180284200-direct-callees.json)。RCX 是 pointer-cache 子结构，EDX 是请求的 uint32 新 capacity。旧 [`0x180284160` caller](rea-binary-cold-insert/05-Binary-cold-insert-0x180284160-assembly.json) 在 used count≥capacity 时，`2841D8` 以 32 位加法计算 capacity×2，`2841DA` 调本函数，返回后于 `2841E8` 递归重试原地址/ID 的插入。

| 顺序与地址 | 已观察事实 |
| --- | --- |
| `284217–28422F` | 将 EDX 零扩展后乘 `0x18`；MUL 溢出选择全 1 尺寸，随后调用 `0x1802DC900` 申请新块。uint32 请求值乘 24 本身不会超出 uint64，但不能据此推断分配一定成功。 |
| `284234–284244` | 以新块、零值及 **newCapacity×8 个连续字节**调 `0x1802DDB14` 清零入口。分配尺寸是 ×24，清零长度却仅 ×8，不能描述成整块新 slot 数组全清零或逐槽清零首字段。 |
| `284259–2842CB` | 按旧 `+0x0C` **capacity** 循环，不是 `+0x10` used count。每个索引 `i` 的旧 slot 地址和 ID 分别复制到 `newBase+i×24` 的 `+0`、`+8`；其 `+0x10` next 为 null 则写 null，否则迁移到新块同一 slot index。 |
| `2842CD–284320` | 按 `+0x04` 的 uint32 **桶数量**遍历既有 `+0x18` 桶头数组，非 null 桶头就地替换为新块同 index 的指针；null 桶头不写。此时 `+0x20` 仍保存旧 slotBase。 |
| `284322–28432B` | 以旧 slotBase 和 EDX=`0x18` 调 `0x1802DC8F8` 释放入口；桶头迁移已经完成。 |
| `284330 / 284339` | 最后依次写 `+0x20=newBase`、`+0x0C=requestedCapacity`。本函数不写 `+0x10` used count。 |

三个直接 callee 只有 `0x1802DC900`、`0x1802DDB14`、`0x1802DC8F8`。此次没有独立查询其函数体或绑定 IAT；伪码中的 `operator_new`、`memset`、`free` 标签仅用于描述正常成功外部契约，不等于已经恢复这些入口的完整实现、分配器身份或异常 ABI。

**这里不 rehash，也不更换桶数组。** 函数不重新计算地址 hash，不改 `+0x00` shift、`+0x04` 桶数或 `+0x08` mask，也不更换 `+0x18` 桶头数组指针。地址和 CachedId 按原 slot index 复制，next 与桶头只更换存储基址，所以正常有效域内桶归属、碰撞链顺序和原 slot index 均保留。不能把桶数推定为 mask+1；新容量与桶数也不是同一量。

**指针迁移以 slot index 为核心。** `284299–2842BA` 与 `2842F4–284316` 先计算旧指针减旧基址，经 signed 除以 24 的 magic multiply/shift 序列取得商，再用 `MOV EAX,EAX` 取低 32 位，最后形成 `newBase + index×24`。可重编译模型的正常域应要求指针为旧数组内、按 24 字节对齐的已知 slot；在此域 index 迁移精确。负差值、越界、非对齐或截断绕回不能悄悄修正为有效引用。保留逻辑 slot index 不表示保留原生绝对地址；原生块迁移后绝对指针改变。

## 可实现的正常域与未覆盖行为

最小正常域可限定为完整有效快照、`usedCount=oldCapacity>0`、无 uint32 翻倍绕回且新容量处于自有预算内，并明确提供旧 capacity 范围内的全部 slot 和完整桶链。caller 的完整有效快照若保证 usedCount≤capacity，则其增长入口到达时正好是相等；不能由此改写原生的 unsigned ≥ 判断。函数复制旧 capacity 个 slot，不允许在部分旧槽未知时只复制 used count 并声称等价。零容量、缩容、损坏元数据、巨大请求及递归无法进展仍需明确拒绝或保留边界。

自有实现可以在正常成功前提下建立新的存储表示并保留 slot-index 关系，但不得把“申请→部分清零→逐槽复制/next 迁移→原桶头就地修改→释放旧块→写新基址→写新容量”描述成原子操作或 native 失败自动回滚。桶头写入发生在旧块释放和基址切换前；中途 native allocator/free/OOM/EH、并发可见性和未知地址寿命不属于本页已恢复范围。后续递归插入或 numeric/工厂操作若失败，正常增长已经完成的效果仍须保留并向上层报告。名字查找 miss/-1 仍由原 caller 绕过插入，不因新增增长能力改变。

本页仅建立上述静态覆盖边界；不宣称 Binary parser、原生缓存 population、实际派生构造/Read 或自动对象生命周期已恢复，也不新增查询计划的执行权限。实现与测试结论由对应结果及验收记录给出。

| 证据文件 | SHA256 |
| --- | --- |
| 04 伪码 | `ae428504b84d33ca8143fc709a9ae8c11e6486573fd989814f9d8c729311d21e` |
| 05 汇编 | `3943f530acbcd6a1ef7a73ac1c2f4c84314c19e2e995a0529dad03a63341b128` |
| 06 直接 callee | `ff5b46a353faa68de0b42fbfde38585736cbdfa9ba1bd338cc3dc8c23f0ac03a` |
