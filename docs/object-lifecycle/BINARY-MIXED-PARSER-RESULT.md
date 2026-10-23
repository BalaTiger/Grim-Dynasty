# 混合类型记录解析：实现结果

2026-10-20。已经把现有有界 parser 扩展到混合 tag0/1/2，完成源码修改与实际轻量回归，未停在计划。新增 **43 条断言、2 个组合场景**；当前相关回归为 **1042 条断言、18 个组合场景，加 ItemQuestDemo 全部通过**。一次串行模块构建和测试耗时 **5.817 秒**，命令退出 0。上一轮 999/16 是保留的历史检查点。

[机器可读验收](binary-mixed-validation/acceptance-binary-mixed.json) · [测试输出](binary-mixed-validation/test-output.txt) · [测试摘要](binary-mixed-validation/test-summary.json) · [行为证据](BINARY-MIXED-PARSER-EVIDENCE.md)

## 本段实际完成

先核对了完整 `LoadFile`、ClassInfo 工厂和具体 Read 的已有证据，再选择无需追加查询的三种字段复制分支。三个类型均调用已证的相同向量准备 helper，按各自元素计数分配和复制；bit3 路径没有被混入。

[BinaryRecordParser.cs](../../src/task_items/BinaryRecordParser.cs) 现在保存三组独立原始 32 位数据，descriptor 保留实际 tag、该类型的 buffer index 及 count。`CopyRawValues(typeTag)` 返回克隆的 uint[]；旧 `CopyValues()` 仍返回 tag2 的 signed int32 视图。没有为 tag0/1 猜测浮点或其他数值意义。多个类型的同 key 仍共用同一字段索引并前插，最新非 tag2 字段会让真实 Class getter 返回原 default，不回退到更旧的字符串字段。

[新增测试](../../src/task_items/BinaryMixedRecordParserTests.cs) 从 literal mixed bytes 经实际 parser 和现有 getter 验证交错 offset、原始高位数据、跨类型重复键的两种顺序、共享字符串身份、空 tag2 缓冲、复制隔离及三数组总预算。字段数 2–15、正 count、总计最多 65536 个 word 的域保持不变。旧 tag2 套件仍为 69 条；其中把 tag0/1 当成不支持的两个负例已替换为真正未知 tag，未把新增成功域继续当作错误输入。

基线 46 个文件中，源码和构建仅有三项预期变化：parser、原 parser 测试的一行 tag 集合、build_items 新增混合测试套件。新测试是新增文件。进度文档随后按本次验收更新；原完整加载、Class factory 及 FinishPartial 源码未修改。

## 完整加载和原生工厂仍缺什么

| 缺口 | 已保存的准确锚点 | 未覆盖范围 |
| --- | --- | --- |
| archive 取数 | Engine `1C4EF3 → 280E20`；caller 传 archive、data/length 输出地址与 raw ID | helper body、真实存储/解压与缓存所有权；不能拿格式层字段字典替代 |
| 普通表及 Binary 基类初始化 | Engine `1C5046 → 13C1D0`，Binary ctor `13F320 → 13C1D0` | base/gate 初始化、普通 fallback；诊断路径也未恢复 |
| parser 后的缓存发布 | Engine `1C508B → 05DEB0`、`1C50DA → 02E770` | 节点构造、桶发布、重复处理及增长；caller 在第二 helper 前已经改变链和 count |
| 真实工厂绑定 | Engine `17E1B2` 使用 root `41CF00`，`17E1BC → 274EC0`，后者尾调用 metadata `+28` | root/children population、实际 callback 与具体 allocation/ctor wrapper 的对应关系 |
| Item/派生 Read | Game Item ctor `3236BA/3236C1` 写 primary vptr `76A4C0`；Engine FinishPartial `17E38A` 调动态 vptr `+8` | Game 槽 `76A4C8` 的实际值、函数 body 以及各派生类最终 vptr；基类 RET 不能替代派生 Read |
| Item 创建入口桥接 | Game `324120` 的已存伪码调用 `12C910` 后设置 replica 并调用 vslot `+3F0` | `12C910` body 和实际分配/ID/创建/登记顺序；此处没有已存 asm 的精确 CALL 指令位置 |

LoadFile 的更完整推导见 [POST-PARSER-DEPENDENCIES.md](POST-PARSER-DEPENDENCIES.md)；工厂与 Read 的来源哈希见 [factory-read-boundary.json](post-parser-boundary/factory-read-boundary.json)。依赖文档中“正在实施”是其冻结时点状态，mixed parser 的后续实测结果以本文为准。

## 下一批已具体化，尚未启动

[固定查询计划](../../tools/static_analysis/rea-record-cache-publication-plan.json) 只选 `05DEB0` 与 `02E770`，各伪码、汇编、直接 callees，连同五个会话生命周期调用共 **11 calls**。精确 caller 地址、参数已知与未定项、要回答的问题、固定 Engine 哈希和 Job/384M/4.25/2 GiB 守卫要求均已写入。没有自动追踪 callee 或混入其他目标。

[离线验证](post-parser-boundary/query-plan-validation.json) 已通过保存 schema、调用顺序、源哈希、直接 CALL 及 runner 未改变检查。在本混合解析检查点封存时，计划为 `prepared-not-registered-not-started`，**未启动 Rea/Java，未注册 runner 新模式，没有后台等待或重试**。父线程随后已明确归还 OW 窗口并允许按固定清单继续；实际执行仍需注册并审查该确切 runner 模式、实时资源预检及当次审批。查询及后续实现将另留记录，不能倒记为本次混合解析测试的一部分。当前没有审批拒绝、待决批准或不确定执行结果。

本段只读工程归档，没有重新读取或运行原版，也没有访问存档/Steam 云或改变持久权限。原版 SHA 沿用上一已验证记录并保留其核验时间，不把历史指纹说成本段重新计算。源码未 commit/push。完成检查点：[manifest.json](../../.tooling/checkpoints/binary-mixed-complete/manifest.json)。
