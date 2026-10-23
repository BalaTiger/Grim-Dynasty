# Item BA8／BC0 构造段结果

2026-10-21，本段从 Item `323811` 接续到 `3238BE`，已完成调用前20次字段写入与 `BA8`、紧邻 `BC0` 两次同槽构造。**关键发现：该 WorldVec3 默认构造仅清零首个 QWORD，不能把整个24字节区域清零。**自有模型仍使用同一 Item／Actor 状态。正式构建一次成功：新增65条断言／8组，旧950回归80条／9组；不是完整 Item／QuestItem 或原生运行时兼容验收。

## 新证据

- Game `[323811,3238BE)` 的173字节完整窗口含25条指令。20次字段写入共107个不重叠字节；`B40` 为WORD0101，`B60` 为WORD0001。B18..B1F、B27、B42..43、B63..6F、B8A..8F、B99..B9F均不写，尤其没有补零B68/B6C。
- 原始 IAT、hint/name 与已归档 descriptor／lookup prefix 将 `[609720]` 精确绑定到 `Engine.dll!??0WorldVec3@GAME@@QEAA@XZ`，不是仅根据名称索引推测。`3238AB` 接收 Item+BA8，正常返回后 `3238B8` 接收原 Item+BC0；两次返回值都不被 caller 检查。
- Engine 的固定 hint565 name／ordinal／EAT 校验将导出定位到 `228AB0`，ordinal566。没有精确 `.pdata` 起点，邻接元数据只提供范围约束，未把 gap 当作函数大小。
- 单独选择的48字节入口窗口中，`228AB0..228ABB` 是完整11字节、3指令正常路径：`QWORD[this]=0 → RAX=this → RET`。无输入字段读取、分支或直接调用。随后5字节CC排除；228AC0起的邻接线性代码不参与本模型。

两次8字节构造写入后，本段合计 **22次字段写入／123个写入字节**。`BB0..BBF` 保持原值或未知。最小已知 Item 写入投影为BC8 exclusive；这不是 WorldVec3 或 Item 的原生分配大小。

详细证据：[caller-contract.md](item-ba8-validation/caller-contract.md)、[native-contract.md](item-ba8-validation/native-contract.md)、[正常入口离线核对](item-ba8-validation/worldvec3-normal-prefix.json)。

## 自有实现

[ItemConstructorBA8.cs](../../src/task_items/ItemConstructorBA8.cs) 消费实际旧950结果，仅在其条件性到达323811时继续。`RunWithArchivedWorldVec3Body()` 明确采用已归档正常正文；不会解析实际运行时 IAT，也不执行原版。所有旧950条件继续保留，尤其磁盘 guard 的 `ret0` 没有变成加载后的保证。

[ItemConstructorBridge.cs](../../src/task_items/ItemConstructorBridge.cs) 新增命名工厂 `CreateForBA8Continuation`，在原 Actor／Bridge／950链开始前选择BC8范围，保留旧8A0与B18工厂行为。Actor、Sub858、Scalars和各前置结果始终共享同一状态，没有完成后复制或回填。

20个 caller 写入逐个检查准确位宽；第一个子构造必须返回后才进入第二个。自有存储不足时保留已完成写入，并同时记录 Engine叶入口228AB0和 Game调用点3238AB／3238B8。没有把这类观察范围不足说成原生异常，也没有发明 callback、析构、清理或回滚。成功与停止的同一 invocation 都不能重放。

## 实际构建与回归

固定 [build_item_constructor_ba8.ps1](../../src/task_items/build_item_constructor_ba8.ps1) 经单独正式 `require_escalated` 批准，工具记录 **71fb18，exit0**。使用已安装、固定哈希 csc，3次编译、2个自有测试EXE，唯一一次构建，无编译警告。全部输出在 `.tooling/item-constructor-ba8-build-20261021`。

| 实际运行 | 断言 | 分组 | 次数 |
| --- | ---: | ---: | ---: |
| 新 ItemConstructorBA8Tests | 65 | 8 | 1 |
| 旧 ItemConstructor950Tests | 80 | 9 | 1 |

新 fixture 覆盖独立完整字节 oracle、未知padding、20个逐写入短存储停点、两个leaf存储边界、首调用未返回时禁止第二调用、原this返回身份、跨前置回调的同一状态、旧profile与token边界、前置结果未完成及重复调用拒绝。前置链复用自有Actor／Bridge代码，不等于重跑这些旧套件。本轮没有执行历史Bridge、858、Scalar或Actor独立套件，也未重跑旧统一验收。

[实际日志](item-ba8-validation/build-result.json) 与 [14个输入预检](item-ba8-validation/build-preflight.json) 分开记录。独立构建后核验见 [post-build-independent.json](item-ba8-validation/post-build-independent.json)。

## 原版完整性、停止记录与恢复

本轮唯一新增原版范围：Game173代码字节＋37元数据字节；Engine193元数据字节＋48代码窗口字节。合计 **221代码窗口字节、230元数据字节**，2次自有小PE dumpbin和6次64KiB缓冲区流式哈希。没有加载原版DLL或把原版路径交给dumpbin。所有原版句柄已关闭，前后哈希一致：

- Game.dll：`cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`
- Engine.dll：`4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`

48字节窗口的整段解析报告保留为失败：末尾截断了已返回正常正文之后的邻接代码。另存的离线核对只采纳前11字节，不重读原版、不重跑dumpbin、不推断截断内容。随后一次仅读取缓存的工具连接出现pong_timeout，最初缓存读是否完成无法确认；同一无副作用读在恢复后成功。原版读取早已完整落盘且关闭，未重试原版读取或构建；见 [host-read-recovery.json](item-ba8-validation/host-read-recovery.json)。该事件不是批准拒绝或测试失败，当前任务结果无阻塞。

本轮静态阶段检查点 `.tooling/checkpoints/item-ba8-static-complete/manifest.json` 包含21文件／77,865字节，SHA256 `5c265c18d64def3b98ca7ee8fe1a1eedafac858e9b187157df2fedf2fbdb7bd7`。完成检查点：`.tooling/checkpoints/item-constructor-ba8-complete/manifest.json`，保留旧Bridge源版本、所有原始／失败记录、新源码、测试、产物与验证记录。

## 下一直接缺口

下一段为已保存 caller 中的 `3238BE` 起：设置RSI=-1、BD8的DWORD，再到 `3238DA → [609980](this+BE0, 2)`。本轮没有追该槽、增加这些字段或执行其逻辑。

完整 Item／QuestItem、正式样本兼容、加载后的IAT／guard、gEngine发布时序、连接管理器与真实分配／同步／异常处理仍未验证。未运行游戏、Rea或Java，未触及玩家存档／Steam云、安全设置，未安装工具、commit或push。
