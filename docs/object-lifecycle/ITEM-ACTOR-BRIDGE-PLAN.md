# Item / Actor 共享构造前缀：本轮范围

本轮目标是把现有条件性 Actor 正常返回接入 Item `323690` 的两个 `0410E0` 子对象，再复用已完成的 `+858` 与六次标量写入，停在 `323743`。不是完整 Item / QuestItem 工厂，也不恢复原存档兼容性。

1. 复用已保存的 Item caller 与 Game IAT `609700` → Engine Actor 构造器绑定。先查询 `0410E0` 的 `.pdata` 边界；无 exact entry 时仅取明确 CALL 目标到下一 entry 前的 288 B 候选窗口，另取 caller 147 B。人工逐条核对正常控制流，不把 `.pdata` 空隙当作函数边界。
2. `0410E0..0411FD` 正常路径实际为 285 B / 44 指令 / 41 次写入；尾部三个 `CC` 排除。无 CALL、分支或传入内存值读取，返回原 `RCX`。保留所有未写字节，包括十处 inline payload 和末端 padding；不推断异常清理。
3. `ItemConstructorStorage` 的前 `530` 字节直接使用同一 Actor 引用；tail 为自有已知/未知字节。两个旧模块以 live view 读写这份状态，禁止事后复制同步。特别检查 `538+178=6B0` 的 QWORD 1 和 `6C8+188=850` 的 DWORD 0，不抹掉 `854..857`。
4. 构造顺序：Actor → 四个 Item vptr → `538` → `6C8` → `858` 两次 triplet → 六次标量写入。缺少自有投影或 Actor 前置条件时保留已经发生的状态；投影不足不是原生内存故障，自己的 storage exception 也不是已恢复的原生 EH。
5. 全文审查构建脚本与输入后，单独通过正式 `require_escalated` 申请四次 C# 编译和三个自有测试程序。新增 Bridge 套件；只重跑受共享 view 修改影响的 Item858 和 ItemScalar 旧套件。Actor 源码保持不变，新集成自然调用旧 Actor 链，不把它说成所有旧 Actor 套件复跑。
6. 验证输入/输出哈希，保存执行原文与有界证据，将原版本和本轮源码、测试、报告一起封存检查点。不得运行原游戏、Rea、Java，不触及存档/Steam 云，不修改安全设置，不 commit/push。

静态证据与构建结果分别记录。构建前没有“测试通过”结论；最后结论见 `ITEM-ACTOR-BRIDGE-RESULT.md`。
