# Item BE0 模型独立静审

结论：**在源码明确声明的自有串行、成功初始分配、显式检查返回观察覆盖域内，未发现阻断问题。** 这是静态源码审查，不是构建、测试或原版运行验收。本审阅未执行生产代码、reader、编译器或测试，也未读取原版。

审查冻结输入：

| 文件 | 字节数 | SHA256 |
| --- | ---: | --- |
| src/task_items/ItemConstructorBE0.cs | 11835 | 02d321f63f3f12ba8ad26878e9e5224be3e041e5fc4210dd1de051a2e780d10c |
| src/task_items/ItemConstructorBridge.cs | 13461 | 80a8464b8e41a4e7da73cf4373d27e9fd76c6d79706a56addffb0a2b464d6381 |

对照依据为本轮 [cached-contract.md](cached-contract.md)（SHA256 `7fd468a700b34c46ef541d0216a94f302f718d7cb4c749c81e289c4f86c7bffd`），以及以下已保存原始指令：

- Game 调用方 `offline-item-be0-import/3238be-instructions.json`，SHA256 `62c166c9aa0b41f44d3f6628a48496de7eaf54d511a1fe6161932e3001ed118d`：176B/31 条。
- Engine EBDE0 `offline-actor-198-constructor/0ebde0-instructions.json`，SHA256 `9083e6b9037ded4e5ee7224b5544ac2b9abae3b6a1ecca612e9d7663b8f484ce`：197B/53 条。
- 迭代器 `offline-actor-198-array-helpers/2dcc3c-instructions.json`，SHA256 `a8c49acbfc80c793124df217df0aace5fb16786e4aa13ba660739d5751d794c3`。
- 叶构造 `offline-actor-198-leaf-prefixes/0ebeb0-instructions.json`，SHA256 `cced24bf2aa60a7272eaea738d25c28337fc7bb23f332c563682d93cad146bb5`，只采用至 EBEF5 RET 的正常部分，排除其后 CC。

## 正常顺序与计数

1. 只有 prior 明确到达 3238BE、状态为 ConditionalItemPrefix、停点匹配，才执行同一 Item 存储上的 BD8 DWORD FFFFFFFF。没有拿看似初始化的字节替代上游结果。
2. EBDE0 的五个头部写入按 EBE06/0B/0E/12/15 的位宽和顺序执行：BE0 QWORD 身份、BE8 DWORD0、BF0 QWORD0、BF8 DWORD2、BFC BYTE0。每个 Item 写入前独立检查完整捕获范围。
3. 缺分配观察时停 EBE41，保留前述六次 Item 写入，不清数组、不发布指针，也不把未知分配解释成 native null 分支。已知数组固定 264B，先清全段，再写 cookie2。
4. 迭代次数来自固定 ElementCount=2，对应 native 保存的 EBP/R15；不会重读可变 BF8 或 cookie。每轮先检查，再执行 14 次 EBEB0 叶写入，完成后才增加已构造计数。叶写顺序和宽度与归档一致，未清除叶自身未写的 34B。
5. 两叶均完成后，EBE8B 才将 BF0 写为原数组 token+8。此写覆盖回调对 BF0 的变更；不会采用回调写入的 BF0 作为最终数组指针。返回子对象身份保持原 Item token+BE0。
6. 只有上述正常返回后才写 BE0 最终身份 180770AD8，并执行 17 次调用方后缀写入。字符串按 **C28 capacity15 → C20 length0 → C10 首 BYTE0**，没有按地址排序。C64 BYTE 是本段末写；停在 323968，未调用下一导入，也未执行 32396E 的 EAX 字段写入。

正常统计为 24 次 Item 写入（1+5+1+17）、29 次数组写入（cookie1+2×14）、总 53 次 Store 指令；memset 264B 单独统计，两次检查单独统计。回调自身的写入不会冒充归档指令计数。

## 回调、未知与失败前缀

检查源每轮接收同一 Item/array 引用、当前 index 和固定 EBEB0 token。可见上一轮叶写及之前回调的修改；修改 BF8、cookie、BF0 或未覆盖字节不会被提前快照掩盖。

调用检查源之前已记录 PendingElementIndex、PreElementCheck 阶段、2DCC75 停点及 CheckSourceCalls。null/false 保留回调副作用和已完成元素，阻止当前叶、未来叶及发布。回调抛异常直接传播；Result 保留 `ItemBE0CheckNotCompleted` 和实际前缀，不伪造 native EH、清理或回滚。单次 invocation 在首次尝试前即置 HasRun，防止失败后、成功后或回调内重放同一实例。

短 Item 捕获逐写停止，不回滚此前写入。若构造已经正常返回而后缀范围不足，保留 WrapperReturned、ArrayPublished、返回 token 和已完成后缀，CallSiteRva 已清空；不能把这种覆盖不足误记为子对象没有返回。数组长度固定且其 Store 先检查完整范围，正常叶内不需要新增未知分支。

## 旧 profile 与覆盖边界

Bridge 只新增 `ContinuationBE0Extent=0xC65`、`MaximumBE0TailBytes=0x735` 和新工厂。对当前文件在内存中仅去掉这项常量及新工厂/comment 后，SHA256 精确恢复到上一冻结版本 `4249ca6add1fd16704ddd1408d527c171fec496102a53d3a9a7efc457b559bae`。因此旧 8A0/B18/BC8 profile、默认值、各自 token 上界及所有其他 Bridge 代码保持不变。

新 profile 仍保留 Actor 的同一引用，tail 克隆，既有 Sub858/Scalars 为共享实时 view；C65 仅是观察范围。数组 token 非零且覆盖完整 264B，输入与导出观察均复制；它是成功初始分配的自有观察，不是分配器实现。

本审查没有把加载后 guard、真实 IAT 或实际分配失败/EH 宣称恢复。默认 Run 缺检查源仍停 2DCC75；显式源只提供本次正常返回覆盖条件。原版存档、完整 Item 构造、实际 native 对象及兼容性均未验证。测试文件只用于核对接口意图，本审查不宣布任何测试通过或断言计数。
