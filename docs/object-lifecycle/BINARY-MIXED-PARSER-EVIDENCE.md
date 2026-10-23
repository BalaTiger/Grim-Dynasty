# 混合 tag0/1/2 记录解析的已有证据

2026-10-20。本次仅审阅工程中已有归档，不运行 Rea/Java，也不读取原游戏。依赖审查发现，完整加载路径仍缺少取数、缓存发布和真实工厂绑定的证据；目前可直接补齐的是现有 parser 对混合 tag0/1/2 字段的正常返回行为。完整缺口与查询优先级另见 [POST-PARSER-DEPENDENCIES.md](POST-PARSER-DEPENDENCIES.md)。

原始绑定是 Engine `13F300` 构造器写 `341FE0`，已存 `341FE8` 槽值指向 `13F510`；`1C4FF4` 是该 parser 的真实虚调用点。此处延用 [前次证据](BINARY-PARSER-EVIDENCE.md) 的固定映像与成功分配契约，不重新推定函数身份。

## 三组数据不能合并计数或偏移

来源：[13F510 完整汇编](rea-binary-parser-entry-retry-2/05-Binary-parser-entry-0x18013f510-assembly.json)，SHA256 `799736dfda45d5eb59e0d4e45766e43c150f1ece2272f0a3961b551592a8b635`，evidence ID `ev_aba7f6db31b8c95da3f075aaa45f9553639da2dc902fa6f3b6d1b207f7a8e909`。

| 类型 | 第一遍元素计数 | 准备向量 | 第二遍原样复制与 descriptor 起点 |
| --- | --- | --- | --- |
| tag0 | `13F5A7` 累计至 R9D | `13F654`，table `+30` | `13F73B` 把 R13D 赋给 R12D；`13F757` 复制 count×4 字节；`13F763` 增加该向量 index |
| tag1 | `13F5B1` 累计至 R15D | `13F662`，table `+48` | `13F780` 把 EBX 赋给 R12D；`13F79C` 复制 count×4 字节；`13F7A8` 增加该向量 index |
| tag2 | `13F5BB` 累计至 R10D | `13F676`，table `+60` | `13F7CB` 从 `[RSP+C8]` 取复制起点；`13F7E2` 复制；`13F7EE` 增加该 index |

`13F8AE` 在每个已处理字段末尾把更新后的 tag2 cursor 重新装入 R12。因此 tag0/1 暂时用 R12D 生成 descriptor 后，不会把它们的 offset 泄漏给下一个 tag2 字段。实现使用三个独立计数和三个独立数组，按实际 tag 选择存储与 offset。

三类准备均调用同一个已保存 [030D20](rea-binary-parser-helpers/05-Parser-helper-0x180030d20-assembly.json)。fresh 正请求经 [02E0F0](rea-binary-parser-vector-growth/05-Parser-vector-growth-0x18002e0f0-assembly.json) 和已有 [022F00](rea-container-reserve/05-Container-reserve-0x180022f00-assembly.json) 分配后清零，零请求是已证 no-op。没有为 tag0/1 引入新的未观察 helper。tag3 未接入，bit vector 仍只走已证零请求路径。

## 原始位模式与真实 getter

原版这些分支执行四字节元素的内存复制。自有 `CopyRawValues(typeTag)` 以 uint[] 克隆返回完整位模式，支持 0、1、2；不把 tag0/1 命名成已恢复的数值类型，不做浮点转换或规范化。旧 `CopyValues()` 只将 tag2 的相同位模式以 unchecked int32 视图导出，继续供既有字符串索引 getter 使用。

`13F897/13F89A/13F89E` 写实际 `{tag, per-type bufferIndex, elementCount}`。`13F8A2` 把 raw uint32 key 零扩展后交给 `284160`，后者仍在同一字段桶头前插。不同 tag 的相同 key 因此会相互遮盖，不能按类型拆成三个 key 字典。

[13FC80 getter](rea-record-getter-support/14-LoadTableBinary-id-getter-0x18013fc80-assembly.json) 在找到最新 key 后检查 tag 是否为 2；非 2 返回同一 default，不继续寻找更老的 tag2 节点。这使混合记录解析实际影响 `Class` 读取，而不是仅导出未使用的数组。

## 本次边界与核验要求

fresh、字段数 2–15、每字段 count 为正、完整稳定 reader 窗口、无 offset wrap 仍为前提。最多 65536 个 word 的自有预算按三个向量总和计算；不是每个向量分别允许 65536。四桶初始化仍来自已证有限正常 log 输入域；没有扩展到 N=0/1 或 N≥16。

应验证独立 literal mixed bytes 的两遍解析、各类型 offset/count、原始高位数据、跨类型重复键两种顺序，以及真实 getter 的 default/共享 pool 身份。还需验证三数组总预算、无 tag2 的非空记录、复制隔离、截断与仍未覆盖 tag3 的域拒绝。原 tag2 的 69 条回归保留；原来把 tag0/1 当作不支持的两个样例，必须改为真实未知 tag，不能继续把已支持输入当作负例。

原生坏数据、OOM、异常回滚、重复 Deserialize、原生 ABI 与完整加载均未因此恢复。所有域检查仍是自有覆盖门控；完整 `LoadFile` miss 继续 Unsupported。测试和实际结果由本次独立验收文件记录，本文不把静态证据审阅当作运行通过。
