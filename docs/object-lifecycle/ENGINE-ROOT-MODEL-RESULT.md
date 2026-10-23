# Engine 根注册模型：实现与定向验证结果

2026-10-21。HEAD `6dfdae419d1c92f7738eefe241937057d83134df`，没有 commit/push。

## 结果

已实现可独立编译的最小自有根注册模型，并通过正式 `require_escalated` 审批运行一次新构建和一次新测试：**221 条断言、11 个场景组通过，退出码 0**。

```text
PASS: 221 assertions, 11 scenario groups
```

这验证显式自有状态和依赖下的局部合同，不代表完整 Engine 根图或正式样本兼容。旧测试、完整 TaskItems 模块及整工程构建均未重跑。

交付：

- [EngineRootRegistration.cs](../../src/task_items/EngineRootRegistration.cs)：模型与显式依赖结果。
- [EngineRootRegistrationTests.cs](../../src/task_items/EngineRootRegistrationTests.cs)：独立的新增 fixture 入口。
- [build_engine_root_registration.ps1](../../src/task_items/build_engine_root_registration.ps1)：只处理上述两份源码的固定路径构建。
- [构建前审查记录](engine-root-model-validation/preflight.json) 与 [完整运行记录、输入输出哈希](engine-root-model-validation/run.json)。

## 已实现的边界

三个 writer 分别保留已证的 record、字符串地址和退出 callback 标识。字符串内容仍未读取；模型只把地址当作不执行的标识，不使用类名补造字符串。

`Apply` 依次执行字符串依赖、捕获 parent、存储 name、按父 count 计算请求、分配、重读父 count/storage、顺序复制并追加、count++、释放旧存储、发布新存储和退出注册。它不创建默认根图、不改 parent/factory、不清自身 children，也不去重。

`Allocate` 保留完整 64 位请求。malloc 返回 null 才调用 handler；完整 32 位结果任意非零都以相同请求重试。handler 返回零才选择两个已证 throw wrapper；构造字段顺序为 +10、+08、+00。显式非局部退出与未知依赖分别传播；throw 依赖意外正常返回时标记 INT3 后继边界，不伪造 null 成功返回。

`RegisterExit` 用显式 cookie/state 计算 XOR 后 ROR64。非全一分支传递 state cell 身份；只有外部 EAX 恰为零才保留 callback。外层按完整 64 位 callback 判零，失败 RAX 为 `00000000FFFFFFFF`。空 callback 仍调用所选依赖。

结果区分 `Completed`、`Unknown`、`NonlocalExit`、`Unsupported`、`ResourceLimit`、`OwnFault` 和 `TrapContinuation`。`Completed + ReturnValue=-1` 表示局部流程走完但退出注册返回失败，不能称为注册成功。每个中断点保留已完成写入和显式依赖副作用。

## 新增验证范围

| 场景组 | 新断言 |
|---|---:|
| 三个 writer 身份、空 parent 与自身字段保留 | 27 |
| 字符串 null/非局部退出、调用后 parent 选择 | 7 |
| 有序复制、null/重复槽、重复追加、显式三节点链 | 15 |
| 32 位回绕、符号扩展与分配大小独立常量 | 7 |
| malloc 立即成功、所有非零 handler EAX、请求保持 | 16 |
| handler 修改 count/storage/parent、复制空间不足 | 10 |
| 两个 throw wrapper、构造字段、意外返回边界 | 25 |
| 分配/handler/free/exit 的非局部退出与部分状态 | 15 |
| XOR/ROR 常量、分支参数、callback 和返回位宽 | 54 |
| 未知依赖/空间、别名守卫、自有异常、预算停止 | 14 |
| 发布前后精确预算停止、缺失 free 依赖 | 31 |
| **合计** | **221** |

每项数字来自独立人工展开，新 EXE 实际输出同时确认总数为 221/11。fixture 数组最多 8 槽，默认语义事件预算 128；极值 size 仅作为数值传入自有依赖，不据其大小分配数组。持续失败重试 fixture 另有独立 callback 上限，不只依赖被测模型退出。没有测量或宣称实际进程峰值内存。

## 审查、审批与产物

实现经独立静审核对 Engine 保存指令与 helper 合同，测试另经静态常量及计数复核。审查明确了两个口径：

- 提前别名检查、跨度检查及预算属于自有安全守卫；Trace 是语义进度事件，不是原生逐指令访问轨迹。
- 模型要求显式、独占、可表示的自有状态。未经控制的并发修改、无效原生指针、原生异常展开不在验证范围内。

构建前完整检查了脚本内容、两份输入及输出范围。正式审批动作是：

```text
C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe
  -NoProfile -NonInteractive
  -File C:\Users\zhuzi\Documents\Grim-Dynasty\src\task_items\build_engine_root_registration.ps1
```

本次是对新模型的独立构建审批，未将此前只读查询许可当作构建许可。没有设置持久命令前缀许可；未执行旧的 `build_class_registration.ps1`。

脚本使用已安装的 `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`，禁用 csc.rsp，指定 C# 5、checked arithmetic，固定新输出目录；拒绝已有输出及重解析路径。所有阶段均检查退出码，失败会保留现场。实际首轮成功，没有重试或替代构建。

输出目录 `.tooling/engine-root-model-build-20261021` 仅含：

| 文件 | 字节 | SHA256 |
|---|---:|---|
| GrimDynasty.EngineRootRegistration.dll | 18432 | `8c9e3c43f156bd28b344c4d26251f12bd212a697256292c621ce1d7aca8064b5` |
| EngineRootRegistrationTests.exe | 43520 | `56caf3f1d08e520a8a44fdbadd89ab385f7d6fae64d05c79ed7097c67b72e2ec` |

构建前后两份新源码、脚本哈希一致。旧 ClassRegistration 实现、测试、脚本与待实现设计哈希也保持不变。未覆盖用户既有改动。

## 静态依据与仍未验证的部分

依据仍为 [原设计](ENGINE-ROOT-MODEL-DESIGN-PENDING.md)、[writer 结果](ENGINE-ROOT-WRITERS-RESULT.md)、[helper 结果](ENGINE-ROOT-HELPERS-RESULT.md) 及其指令证据。原设计作为 2026-10-20 的冻结计划保留，本文件记录其后的局部实现进展。

本轮未重读原游戏二进制，沿用已封存的 Engine SHA256：
`4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`。
这不是本轮重新测量的哈希。新增原版语义读取量为 0。

以下仍未验证：

- 三个真实注册名称、退出 callback 内容、cookie/onexit 运行时初始化及实际 CRT 行为。
- 完整兄弟节点、完整根图、CRT 初始化调度及真实启动结果。
- 具体原生异常类型、分配所有权与容量、native EH/unwind。
- 与 Game Item/QuestItem 模型的跨模块连接、完整工厂/Read/对象加载链及正式样本兼容。
- 原存档兼容不属于本项目目标。

下一最小证据缺口是 **Engine RVA 309E64、309AC4、309AEC 的三个输入字符串**。建议后续仅对这些已知地址分别设置最多 64 B 的只读上限，遇 NUL 停止；截断或编码不明时继续保留未知。该建议不包含 callback 遍历、兄弟节点扫描或全引擎扫描，本轮未执行这些读取。

## 检查点

可恢复封存位置：`.tooling/checkpoints/engine-root-model-complete/manifest.json`。它保存本轮源码、脚本、产物、审查/运行记录和所依赖的核心设计与证据，并关联前一正式策略查询检查点。

本轮没有执行原游戏、原 DLL、Rea 或 Java，没有访问存档/Steam 云，也没有改执行策略、ACL 或其他安全设置。无待批准或执行结果不明的动作。

