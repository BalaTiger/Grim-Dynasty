# Engine 共享记录的动态工厂查询

2026-10-21。HEAD `6dfdae419d1c92f7738eefe241937057d83134df`。本段不 commit/push，保留既有改动。

## 证据与原生读取顺序

直接依据已保存的 [Engine 274EC0 汇编](rea-native-registration/09-Class-factory-helper-0x180274ec0-assembly.json)，共 52 条指令，入口至 `274F67` 两条返回路径均在报告中。`raw_result.value` 与 `normalized_result` 文本一致；文件 SHA256：

`bfa5f091e67fe9ee863c639d10d8cdf2424d0f564278baaaa37661d661794790`

证据标识：`ev_9a2d2b7151f8d4fa70667f4f982d4dc9e3bca50cfa6168989daab735c2724b53`；对应原 Engine SHA256 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`。这是已有归档，不是本段重新运行 Rea 或重新测量原件。本段原版读取为 **0**。

| Engine RVA | 已证行为 |
|---|---|
| `274ECF–274EDE` | 当前帧保存 node 和 query 指针；读取一次当前 Name 指针。 |
| `274EE1–274EF5` | 先读 query byte，再读 name byte，按无符号字节比较；首个相等 NUL 结束。不复制字符串。 |
| `274EFC–274F0F` | self 匹配才读取 factory `+28`，并尾跳。其返回值直接结束本帧，包括 null。 |
| `274F12–274F15` | mismatch 后才读取 signed count；非正数直接返回 null，不读取 children。 |
| `274F20–274F2B` | 每轮重读当前 node 的 children，再取当前 index 的 child，递归访问已取出的身份。 |
| `274F30–274F3E` | nonnull 立即返回；只有 null 才 index++、offset+=8，再重读父 count。若继续，下轮才重读 children/slot。 |

因此，callback 对 count、未来 storage/slot/name 及原 query 数组内容的修改，会在后续实际读取点生效。活跃父帧不会重新比较自己的 Name，也不会重置 index。外部变量改指向另一 query 数组，不会改变当前帧保存的原数组引用。

## 最小自有实现及边界

新增 [EngineDynamicFactoryDispatch.cs](../../src/task_items/EngineDynamicFactoryDispatch.cs)。`Create` 直接遍历现有共享 `EngineRootNode`，不经过 `EngineRootSnapshot`。原有六份模型及接口保持不变。

- 不复制 query、不预先克隆图、不预扫完整名称、不缓存节点结果，也不拒绝一切活动路径重复身份。callback 可以修改祖先名称，再经后续槽重新访问祖先并有限结束。
- 每次 visit 固定当前 node、query 引用及本次名称数组；随后按已证顺序查询。相同节点出现在多个槽时会再次访问和调用其工厂。
- 未提供的 name 字节、非 byte[] 名称、未知 factory、缺失 storage/span、null child 等只在真正需要读取的路径上停止。不用未发布注册分配来填补短跨度。
- own callback 抛异常、缺失字段及预算耗尽返回 `Unsupported`，不回滚已发生的 callback 副作用。它们是自有覆盖边界，不是原生容错或 native EH 合同。
- `VisitedNodes`、`CallbacksInvoked`、`ComparedBytes` 记录本次查询前缀；比较计数为已读取的字节对数。重入 `Create` 有独立计数。此模型不能强行中断任意 callback，调用者需控制 callback 自身的工作量。

自有上限：4096 次 visit、128 层、1 MiB 字节对比较、4096 个 factory 绑定；调用者显式提供更小预算。限制阻止无限循环，不代表原生搜索采用这些上限，也不覆盖 native index/offset 溢出域。

**当前覆盖范围固定 factory 身份及 identity→callback 映射。** `EngineRootNode.FactoryIdentity` 仍为只读；本次调用按引用身份保存明确的 callback 表。未知 callback 只在 self 匹配时要求已知；非法/重复绑定配置则在自有输入校验阶段拒绝。callback 修改传入 binding 数组不等于修改 native `+28`，本实现不会将其解释为 factory slot 写入。`Func<object>` 仅表示自有 object/null 回调，不能据此恢复构造器 ABI、假设 native 无参数或传入 this。

Count 在现有共同记录中是明确的 signed int；不能表示未知 native count。使用此接口需已有 count，不能用 0 伪装缺失证据。字段变化限于受控同步 callback，未验证并发写入、原生悬空指针或生命周期失效。

## 验证状态

已通过新的正式 `require_escalated` 请求执行一次 [固定构建脚本](../../src/task_items/build_engine_dynamic_factory_dispatch.ps1)：Windows PowerShell 5.1、已安装 .NET Framework csc，编译 7 个库源文件与 1 个新测试源文件，仅运行新测试程序。退出码 **0**，输出 **`PASS: 71 assertions, 8 scenario groups`**。没有使用 Bypass、持久权限前缀或旧脚本。

| 新场景组 | 断言 |
|---|---:|
| 稳定查询对照与惰性字节读取 | 12 |
| 工厂回调中 Game 注册追加，以及冻结快照对照 | 8 |
| 当前 count、替换 storage 与下一 slot 重读 | 8 |
| 原 query 引用、Name 重读、重复边与固定绑定 | 5 |
| self 命中及非空结果立即返回 | 6 |
| 惰性未知字段、引用身份绑定和异常前缀 | 10 |
| 工作预算、循环及有限的活动祖先重访 | 10 |
| 重入 Game free 失败，以及冻结快照对照 | 12 |
| **合计** | **71** |

新增测试中实际执行 **5 次旧快照查询接口对照**，涵盖 3 个稳定查询、1 个回调追加和 1 个 free 中断前缀；这不是旧 suite 重跑。旧套件本轮执行数为 **0**。共享记录及旧查询接口没有修改；6 个原有库源文件、4 个旧测试文件和 4 个旧构建脚本的哈希与上一检查点一致。构建前后全部 9 个输入文件哈希相同，HEAD 与原有 12 个 tracked 改动的 numstat 未变。

结果揭示了静态快照的两个具体缺口：回调将 Actor 的 children 从 1 个扩至 2 个时，实时查询访问新增 Item，冻结快照在旧 count 处结束；free 抛出后 count 为 2、已发布 storage 仍仅 1 槽时，实时查询在第二槽报未知，冻结快照只看旧 count。如果第一个工厂已返回非空，实时查询立即返回，不继续读取受损父状态。

[执行前清单](engine-dynamic-factory-validation/preflight.json) 与 [实际运行记录](engine-dynamic-factory-validation/run.json) 保存精确命令、审批方式、完整输出、输入/输出哈希和未重跑范围。产物位于 `.tooling/engine-dynamic-factory-dispatch-build-20261021/`：

- `GrimDynasty.EngineDynamicFactoryDispatch.dll`：37,376 B；SHA256 `2ac402b3b4f91d118356d292cac7c9fac37f4cdedb7f1654b8b504e8be1e5699`。
- `EngineDynamicFactoryDispatchTests.exe`：33,792 B；SHA256 `ac8a76fd9a2cb66348a84f139b52e6e38116b29d5de122dfaf8a9bc7f981fa7a`。

恢复检查点：[manifest.json](../../.tooling/checkpoints/engine-dynamic-factory-complete/manifest.json)。它包含本轮源码、精确构建脚本、测试产物、已有指令证据、执行记录及上一检查点清单。

## 尚未闭合的部分

本段只恢复已证查询控制流在上述自有覆盖域内的效果。可变 factory slot、原生构造器 ABI/实现、真实完整 root graph 和 CRT 调度、原生对象 Read/完整物品工厂、正式样本兼容仍未验证。未知字段返回 Unsupported 不表示原程序会安全返回失败。

后续应先选择一个已有定位的具体 factory 及其直接构造依赖，继续限定范围核对调用合同；不能把查询器返回自有 object 当成已创建原生对象。没有证据支持的新算法、注册顺序和构造副作用继续保持未知。

本轮没有运行原游戏、原 DLL、Rea 或 Java，没有读取原版或存档/Steam 云，没有安装工具、修改执行策略/ACL、执行旧被拒脚本或 commit/push。已完成的自有动态查询验证不代表原生动态工厂完整恢复。
