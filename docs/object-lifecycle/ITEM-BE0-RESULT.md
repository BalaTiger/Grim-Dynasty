# Item BE0：HookPack 两元素构造与调用方续接

2026-10-21。本段从 `3238BE` 条件性推进到 **`323968` 下一调用之前**。原始导入已确认 `Game [609980] → Engine!HookPack(unsigned int) → EBDE0`，参数为 2。自有模型保持同一 Item/Actor 状态，完成 264 字节数组观察、两个元素及返回后的字段初始化；没有执行原版或恢复完整 Item/QuestItem。

## 原生证据与实际读取范围

- 新 Game 窗口 `[3238BE,32396E)` 为 176 字节、31 条指令；包含下一调用指令用于确认停点，但未跟随它。`3238C2` 只写 `DWORD[Item+BD8]=FFFFFFFF`；`3238DA` 以原 Item+BE0、EDX=2 调用 `[609980]`。
- 原始 IAT、hint/name 和 Engine 导出绑定给出 hint 213、ordinal 214、RVA EBDE0。新增元数据总量为 74 字节。Engine 构造正文和边界全部复用已保存证据，没有新读 Engine 代码或 `.pdata`。
- 复核旧 EBDE0 的 197 字节、2DCC3C 的 110 字节、EBEB0 的 80 字节候选窗口与已存原始字节一致。EBEB0 只使用截至 EBEF5 RET 的 70 字节正常正文，其余 CC 不属于模型。
- 本轮共 1 次小型自有 PE 的 dumpbin、4 次 64 KiB 缓冲流式完整性哈希。原版句柄已关闭，Game/Engine 前后哈希一致；没有将原版路径交给 dumpbin。

Game SHA256：`cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`。

Engine SHA256：`4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`。

证据：[本轮原始核验](offline-item-be0-import/inspection.json)、[31 条指令](offline-item-be0-import/3238be-instructions.json)、[旧字节复核](item-be0-validation/reused-engine-bytes.json)、[完整静态合同](item-be0-validation/cached-contract.md)。原始核验一次成功，工具记录 `74fb2f`，exit 0。

## 写入顺序与自有模型

EBDE0 先写五个头字段：BE0 的 QWORD 身份、BE8 的 DWORD 0、BF0 的 QWORD 0、BF8 的 DWORD 2、BFC 的 BYTE 0。参数是保存的元素数量，不是根据 2/4 分派算法的类型编号。两元素各 128 字节，加 8 字节 cookie，正常初始分配请求为 **264 字节**。

显式提供有效初始分配和正常 memset 条件后，先清零 264 字节、写 cookie=2，再在每个元素之前询问独立的检查观察。叶构造每次精确写入 14 个字段；它本身不清零整个元素。只有两个叶构造都返回后，EBE8B 才发布 `BF0=allocation+8` 并返回原 Item+BE0。

调用方随后执行 17 次字段写入，先将 BE0 身份改为 `180770AD8`。C10 字符串先写 C28 容量，再写 C20 长度和 C10 首字节；刚写的容量 15 确定 inline 分支。最后写入 C64 单字节。新增捕获范围 C65 是写入观察边界，不是原生对象大小。

[ItemConstructorBE0.cs](../../src/task_items/ItemConstructorBE0.cs) 合计 **24 次 Item 字段写入、29 次数组写入**，另列 264 字节批量清零。回调拿到当前 Item 和数组；修改 BF8/cookie 不改变已保存的两元素循环，修改 BF0 会在最终发布时被覆盖。其他未写字节、未知 padding 和无关回调修改保持。

[Bridge](../../src/task_items/ItemConstructorBridge.cs) 仅新增 `CreateForBE0Continuation`，须在原 Actor→Bridge→950→BA8 链开始前选择；没有复制完成状态再同步。旧 8A0、B18、BC8 接口保持。

默认检查未知时停在 2DCC75。缺少分配观察时停在 EBE41，保留此前头写入；它不是原生 allocator 的 null 失败返回。检查 false/null、自有异常或存储覆盖不足均保留已完成前缀，不提前发布或执行后缀。CE70 的磁盘 `RET imm16=0` 不代表加载后检查行为，也不代表 EAX 返回 0。自有回调异常传播，不模拟原生 EH 或回滚。

## 实际验证

固定 [构建脚本](../../src/task_items/build_item_constructor_be0.ps1) 经单独正式 `require_escalated` 批准执行一次：3 次已安装 csc 编译、2 个自有测试程序。初始工具记录 `6eb5a0`，最终 `c9f2b9`，exit 0；没有编译警告。

| 实际执行套件 | 断言 | 场景组 | 次数 |
| --- | ---: | ---: | ---: |
| 新 ItemConstructorBE0Tests | 66 | 10 | 1 |
| 旧 ItemConstructorBA8Tests | 65 | 8 | 1 |

新测试覆盖独立完整字节预期、264 字节清零、精确位宽、未知 padding、共享身份、两轮动态修改、false/null/异常前缀、禁止重入重跑、短存储逐写入停点及容量先于较低地址写入的顺序。最大 Item/数组 token 分别在不重叠的 fixture 验证。

未重跑其他旧独立套件或历史统一验收。复用真实自有 Actor/950 前置链，不等于执行了它们的旧测试套件。

[冻结的 15 个输入](item-be0-validation/build-preflight.json)、[真实测试日志](item-be0-validation/build-result.json)、[构建后独立复核](item-be0-validation/post-build-independent.json) 分开记录。模型静审结论见 [独立审查](item-be0-validation/model-independent-review.md)。

## 恢复点与剩余边界

静态阶段已封存：`.tooling/checkpoints/item-be0-static-complete/manifest.json`，18 文件、205,202 字节，SHA256 `a19ce055baa5893a646379480d5df06bd4fb5bf3755ee2ba46228ed75ddc6148`。完成检查点：`.tooling/checkpoints/item-constructor-be0-complete/manifest.json`，保存此前选定文件、新证据、源码、测试和产物，以及修改前的 Bridge/进度记录。

下一直接依赖是 **`323968 → [609128](原 Item)`**；尚未执行该调用或其后的 `DWORD[Item+538]=EAX`。完整 Item/QuestItem、加载后的 IAT/guard、真实分配/new-handler/EH、gEngine 发布时序、连接管理器及正式样本兼容仍未验证。

本轮未运行游戏、Rea、Java，未接触玩家存档或 Steam 云，未修改安全配置、安装工具、commit 或 push。
