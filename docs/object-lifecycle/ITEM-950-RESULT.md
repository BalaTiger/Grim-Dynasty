# Item +950：正常前缀接合结果

2026-10-21，本段已从既有 Item `323743` 接到 `32380C → 428690(this+950)` 的条件性正常返回，停在 `323811`。新模型使用同一个 Item／Actor 可变对象；没有复制完成态或事后同步。**正式构建一次通过：新增 80 条断言／9 组，旧 Bridge 回归 55 条／9 组。**这不是完整 Item、QuestItem、原生 ABI 或正式样本兼容验收。

## 新闭合的证据

| 部分 | 原生磁盘证据与实际模型范围 |
| --- | --- |
| Item `323743..323811` | 17 次字段写入：四段 inline 初始化按容量 QWORD→长度 QWORD→首 BYTE；四个 vptr 身份值；948 的 DWORD。随后以 Item+950 调用 wrapper。 |
| `428690..4286EE` | 先清零子对象 +0 QWORD；调用 `5DAED0(this+18, 28h, 10, 16710, 165D0)`；仅正常返回后清零 +1A8 DWORD 与 +1B0/+1B8/+1C0 QWORD，返回原 this。 |
| `5DAED0..5DAF3E` | 每轮先调用 Game 5DB8B8 检查 callback，再调用 16710；元素正常返回后才增加 stride 和已完成计数。正常正文不读取或调用第五参数。 |
| `16710` | 有界 32 字节窗口中的前 23 字节是正常 leaf，后 9 字节 CC 排除。三次写入依次为容量 +18 QWORD=15、长度 +10 QWORD=0、首 BYTE=0；返回原 this。 |
| Game 检查入口 | 独立确认 `5DB8B8: jmp [18060BAA8]`，槽磁盘初值为 180015880，目标首条完整指令为 `ret 0`。没有借用 Engine CE70 结论。 |

十个元素从 Item+968 开始，步长 28h。每个元素保留 inline payload 的 +1..+F 以及 stride 尾部 +20..+27。wrapper 末尾只写 AF8 的低 DWORD，AFC..AFF 保留。全部已知正常写入的最小投影为 B18 exclusive，总计 **52 次新字段写入**，不计栈上的计数和参数保存。

第五参数 165D0 的正文也已独立读取：容量小于 16 时重置字段；容量不小于 16 时先调用未知 16620。它如何参与异常清理仍未证明，本轮没有追踪 16620、展开 EH 或在模型中调用它。

## 自有实现与条件

- [ItemConstructor950.cs](../../src/task_items/ItemConstructor950.cs) 消费实际旧 Bridge 结果，默认停在首次 `5DAF09`：保留 caller 17 次和 wrapper 1 次写入，零元素完成。每次检查都需要明确的自有返回观察；没有将磁盘 `ret 0` 自动推广为加载后的检查保证。
- [ItemConstructorBridge.cs](../../src/task_items/ItemConstructorBridge.cs) 仅扩展存储构建方式：命名工厂 `CreateFor950Continuation` 在构造开始前选择 B18 投影。旧构造器仍保持 8A0 默认范围、370 tail 上限与旧 token 地址边界。Actor、Sub858、Scalars 始终共享原对象。
- 正常情况下完成 17+1+30+4 次写入，停在 `323811`。第 k 次检查停止时保留 18+3k 次写入、k 个完整元素，没有 suffix 或清理。短投影逐次检查准确写入范围；这代表自有观察不足，不代表原生异常。
- 自有检查回调可见实时共享状态；其修改、抛错和已有前缀保留。重入／重跑同一 invocation 被拒绝。没有把自有异常转换成推定的原生失败路径。

## 实际验证

正式 `require_escalated` 批准执行固定 [build_item_constructor_950.ps1](../../src/task_items/build_item_constructor_950.ps1)，工具记录 `f67be2`，exit 0；3 次已安装 csc 编译、2 个自有测试 EXE，唯一一次构建，没有编译警告。输出位于 `.tooling/item-constructor-950-build-20261021`。

| 实际执行 | 断言 | 分组 | 次数 |
| --- | ---: | ---: | ---: |
| 新 ItemConstructor950Tests | 80 | 9 | 1 |
| 受影响的旧 ItemConstructorBridgeTests | 55 | 9 | 1 |

新 fixture 覆盖独立全字节 oracle、所有十个检查停点、未知字段、payload/stride padding、动态修改、短存储边界、共享身份、最大 token、抛错和重入。它复用真实自有 Actor→Bridge 前置代码，但没有运行旧 Actor、858 或 Scalar 独立套件；没有重跑历史统一验收。测试 source 最初的成功状态拼写在编译前审查时修正，不是一次失败测试。

实际日志见 [build-result.json](item-950-validation/build-result.json)，13 个编译前输入及其哈希见 [build-preflight.json](item-950-validation/build-preflight.json)。构建后输入哈希全部一致。独立复核另存 [post-build-independent.json](item-950-validation/post-build-independent.json)。

## 取证范围与恢复

原版唯一来源为 `D:\Steam\steamapps\common\Grim Dawn\x64\Game.dll`，SHA256 始终为：

`cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`

本轮新增语义读取共 **532 字节代码窗口、228 字节元数据、8 字节数据**；19 次元数据读取，8 次小型自有 PE 的 dumpbin 调用，16 次原版前后流式哈希，缓冲区 64 KiB。没有将原版路径交给 dumpbin，没有加载原版 DLL；本轮未读取 Engine.dll。

两次解析停止完整保留：六字节入口窗口截断七字节指令；补最后一字节后，解析器未计入 dumpbin 换行字节。后续只离线核对已有输出，再读取直接槽位／磁盘目标，未重读已取得的代码。这不是批准拒绝或测试失败。详见 [guard-contract-review.md](item-950-validation/guard-contract-review.md)。

两次模型容量中断后都核对了已保存结果；最新进程枚举没有 node/csc/dumpbin/本段测试候选进程，无未决执行。恢复记录见 [resume-check-2.json](item-950-validation/resume-check-2.json)。实现子任务最终通知受容量限制，但源码已落盘，根已接手审查并完成正式构建，因此当前没有容量或审批阻塞。

中间静态检查点 `.tooling/checkpoints/item-950-static-stage1/manifest.json` 包含 34 个文件／179,949 字节，SHA256 `1a2e3b676844e54f355792b78cde78dfdba39c60487a00c9e23ae86c86c97353`。完成检查点位于 `.tooling/checkpoints/item-constructor-950-complete/manifest.json`；保留前次 Bridge 源版本、所有失败取证记录、源码／测试／产物与证据。

## 剩余直接缺口

下一最小段是已保存 Item 汇编中的 `323811` NOP 后，从 `323812` 字段写入到 `3238AB → [180609720](this+BA8)`；同槽随后用于 this+BC0。本轮没有追踪该槽、增加那些字段或宣称后续构造已恢复。

仍保留既有外部条件：gEngine 发布与时序、连接管理器、实际加载后 Game／Engine 检查槽、真实同步／分配／EH、完整 Item／QuestItem 和正式样本兼容。没有启动游戏、Rea 或 Java，没有接触玩家存档／Steam 云、修改安全设置、安装工具、commit 或 push。
