# 下一闭合边界：二进制表 Class getter

本轮仅梳理工程中已保存的反编译证据，没有运行 Rea/Ghidra、编译、测试或索引，没有读取原游戏、存档或 Steam 云数据。上一轮 **746 断言、6 个组合场景和 1 个 Demo** 是保留的验收结果，不是本轮重新执行的结果。已复核上一验收记录中的 30 项工程源码/自有产物哈希，均未变。

结论：优先连接 `LoadTableBinary` 的 **archive 已存在、指针缓存已命中且内容仍一致** 路径。已有证据足以实现这条正常路径，不需要新查询。完整 cold-cache 成功路径则明确停在 `0x180284160`；不能把它省略，也不能只实现一个脱离名称解析的 numeric getter 后称为完成。

本轮交付边界与查询计划，保持行为源码及验收基线不变。原因是现有 Binary 接口只有 archive presence；实际接通需要引入显式地址、共享字符串池、两种桶链和有效解析缓冲区，并调整 CreatePartial 的 query 身份传递。它不是把一个 Unsupported 改成查字典的小修。本轮没有为增加断言数另造简化行为。

## 已有证据闭合到哪里

以下地址均为固定 Engine 映像的 preferred VA，SHA256 为 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`；不是推定的加载后地址。

| 已证分支 | 准确位置及行为 | 实现边界 |
| --- | --- | --- |
| Binary 外层 | [`13FB60` 汇编](rea-record-class-getters/08-LoadTableBinary-class-getter-0x18013fb60-assembly.json)：`13FB94` 加锁；`13FBA7` 将 archive 加 `0x88`；`13FBB1` 调 `283840`；`13FBBE` 调 `13FC80`；`13FBCA` 解锁 | 顺序完整。不能在 name→ID 返回 `-1` 时提前默认返回。 |
| 指针命中、内容相同 | [`283840` 汇编](rea-record-getter-support/11-LoadTableBinary-name-to-id-0x180283840-assembly.json)：`283880` 比较地址；`283954–283969` 比较 C-string；`2839F8` 返回缓存 ID 低 32 位 | 不调用 `284160`、不写缓存；可作为第一条真实接通路径。 |
| 指针命中、内容改变、名字查到非 `-1` ID | `2839B7` 调已有 `03C8E0`；`2839F4` 将 sign-extended ID 写回原节点 `+8` | 已有证据够描述更新，但要报告 getter 的写副作用；后续 Unsupported 不回滚。 |
| 同地址内容改变但名字未找到，或结果为 `-1` | `2839EB → 28391A` 返回 `-1`，不改旧缓存 ID | 返回值仍交给 numeric getter，不等于自动 default。 |
| 指针未命中、名字未找到或 ID 为 `-1` | `283906/283909 → 28391A` | 不插入，但仍继续 numeric；不能当成未知插入成功。 |
| 指针未命中、名字查到非 `-1` ID | `28390B–283915`：参数为 `context+0x58`、原 query 地址、sign-extended ID，调用 `284160` | **真实缺口。** 函数体/直接 callees 尚未取得；此处不能跳过去返回 ID。 |
| Numeric 字段读取 | [`13FC80` 汇编](rea-record-getter-support/14-LoadTableBinary-id-getter-0x18013fc80-assembly.json)：`13FC8A` 符号扩展 ID；按桶链匹配；`13FCCE` 检查 tag=2；`13FCE1` 读 int-buffer 的 signed string index | 无匹配或 tag≠2 返回同一 default；负/越界 string index 返回 null；有效项返回字符串存储身份，即使存储长度为零。 |

Numeric 的 `payload+4` 是无符号 buffer index，实际 `int-buffer` 读取之前没有已观察到的边界检查；模型必须明确提供有效数据，不能把未知/无效地址解释为 miss。`283938–283940` 在读取名字字符串时使用完整 **64 位 cached ID × 32**，之后才返回低 32 位；不能提前截断缓存 ID。

一个可复用的实际关系：Binary wrapper 传 `archive+0x88` 给名字解析器，解析器读取的向量指针在 `context+0x40`，所以两条路径读取同一 begin-pointer 字段 **`*(archive+0xC8)`**；numeric getter 的 end 为 `*(archive+0xD0)`。`archive+0xC8` 是存放指针的字段地址，不是字符串数组本身的起址。名称校验与返回值读取应共享同一池及 entry 身份，不能造两份互不相关的名字/值字典。

## 不开 Ghidra 可以实现的代码部分

以下为下一实现方案，本轮没有宣称已写入这些行为。

1. 增加有界、明确提供的 Binary 状态：pointer-cache 的 shift/mask/完整桶链、节点的 64 位指针 key 和 64 位 cached ID；同一个字符串池；numeric 字段桶链、tag/index 及有效 int-buffer。保留桶内顺序，区分缺少元数据与已知空桶。当前 `LoadTableBinarySnapshot.ArchivePresent` 不足以推断这些状态。
2. 给 query 增加**显式逻辑 64 位地址和当前可读 C-string**。native 比较的是地址值；同字节但地址不同不能自动命中，同地址但更换字节容器仍须检查当前内容。可用共享自有对象承载状态，但不能把 .NET 对象身份、数组 hash 或 GC 地址作为 native key。CreatePartial 的已保存 caller 使用固定 `LEA 0x18031B34C`；这是 preferred VA，不能未经明确映像绑定就当 runtime 指针。其当前每次 `new byte[]{Class...}` 的接口需要相应扩展。
3. 将“指针已命中、内容一致”的名字解析与 numeric 读取**直接接入现有 RecordTableGetter 和 CreatePartial**，保留同一 gate 下的顺序、解锁后的 value 身份和原有工厂调用。第一步不必实现池构造或原生 parser，但完整稳定 snapshot 是调用方明确前提，不是恢复出的原始数据。
4. 再接入 stale-hit 更新、名字未找到返回 `-1` 的已证分支。若 getter 会写缓存，新增其副作用结果并向 CreatePartial 聚合传播；当前 getter 只读的说明也要更新。名字 lookup 可复用已有完整字节 hash/比较逻辑，但 payload 必须明确为 ID，不能把 opaque table 强行解释为 ID。
5. 最后补齐 pointer-cache miss + 名字找到 ID 的插入。取得 `284160` 的真实行为和必要依赖之前，保留在该调用点 Unsupported。正常分配、有效缓冲区和稳定状态仍是覆盖前提。

有意义的验收应核对行为，不设断言数量目标：同字节不同地址、同地址内容变化、完整 64 位 cached ID、共享池身份、空字符串与 default 的区别、`-1` 仍进入字段查找、无效字符串索引返回 null、未覆盖插入前停止，以及缓存更新后下游失败仍保留更新。组合场景应让真实 Binary getter 的返回值进入现有 Class factory，而不是由测试直接提供类名。

## 必须补证时的最小准确查询

第一优先目标为 **Engine `0x180284160`**，来自已存 [`283840` direct callees](rea-record-getter-support/12-LoadTableBinary-name-to-id-0x180283840-direct-callees.json) 和调用点 `0x180283915`，不是猜测地址。

计划的三个只读分析请求为：

```json
[
  {"tool":"procedure_pseudo_code","args":{"procedure":"0x180284160"}},
  {"tool":"procedure_assembly","args":{"procedure":"0x180284160"}},
  {"tool":"procedure_callees","args":{"procedure":"0x180284160"}}
]
```

如以后获准运行，沿用固定 Engine SHA、单目标会话及显式 open/session/close 检查；三个分析调用加五个既有生命周期调用为 **8 次调用上限**。取得函数体和 direct callees 后先审查，不自动递归新目标；8 次调用不保证一次闭合全部下游依赖。本轮没有生成可执行 runner 模式、提交审批或执行这些请求。

**内存边界：** 保存的证据没有可归因于这个查询的峰值工作集，不能给出可靠的 GiB 估计。少量函数查询仍可能触发整份 Engine 的 Ghidra 导入与自动分析，不能因为只有三个查询就视为轻量任务。当前 UE 对照测试期间保持此批次未启动；没有后台等待或自动重试。

## 后续完整加载和生命周期的准确缺口

| 推荐顺序 | 已知目标/来源 | 为什么仍未闭合 |
| --- | --- | --- |
| Binary getter 插入 | `0x180284160`，上文 `283915` | 原始指针缓存的插入、分配、增长及失败后状态。先解决该边界，再扩展 cold 路径。 |
| Binary 表构造 | `0x18013F300`，[`LoadFile miss` 汇编](rea-record-load-dependencies/11-LoadFile-miss-0x1801c4e20-assembly.json) 的 `1C4FE2` | 须确认实际 vptr 和初始字段，不能把 supplied snapshot 当 parser 产物。 |
| Binary parser | miss caller `1C4FF4` 调构造结果 vslot `+8` | 保存的 vslot 证据只读取了 getter `+0x28`，未保存 parser 目标。若构造器确认 vtable 为导出表 `0x180341FE0`，待读槽地址为 **`0x180341FE8`**；先确认槽值再查询实际函数，不猜一个 parser VA。 |
| 字符串缓存发布 | `0x18005DEB0`（`1C508B`）与 `0x18002E770`（`1C50DA`） | 节点/名称/表所有权、发布与索引维护未闭合；不能复用 uint 注册容器冒充名称缓存。 |
| 普通表 fallback | `0x18013C1D0`（`1C5046`） | 此前 `1C5032` 已到达 engine vslot `+0x18` 诊断，其实际目标仍未知；不能跳过诊断后直接返回空表。 |
| archive 特殊取数 | `0x180280E20`（`1C4EF3`） | 仅 archive `+0x2E` bit0 为 1 的分支必需；可在正常直接数据分支之后单独补证。 |
| reader 的条件释放 | `0x1802DC8F8`（`1C5002`） | parser 改变 reader ownership 标志后才到达；正常无释放路径不能证明该分支完成。 |

miss caller 中已知的计时、失败次数、缓存发布后的计数变化，不值得脱离真实加载/发布实现单独增加统计模块。下一批也不应一次扩展到所有表构造、parser、类注册和 Read。

ObjectIdAllocator 与登记都关联 manager `+0x30` 的键，但这不足以接出自动生命周期。[分配器汇编](../rewards/rea-object-ids/ObjectManager-ID-assembly.json) 在 `17E4C5/17E4C9` 使用 manager **`+0x10`** 的锁，`17E61D/17E621` 释放它；`17E543` 调用 `0x18002DD80` 查询 manager `+0x30` 中的完整 raw counter，[helper 证据](../rewards/rea-object-ids/Object-ID-map-find.json) 没有插入或预留输出 ID。[FinishPartial 汇编](rea-native-registration/05-ObjectManager-FinishPartial-order-assembly.json) 的 `17E30C/17E310` 和 `17E377/17E37B` 使用的是 **`+0x28`**。相同 Enter/Leave import 不代表锁对象相同，不能合并为同一 Gate。现有保存证据尚未闭合自动 Allocate → CreatePartial → FinishPartial 的上层 caller、pending 插入及销毁，因此不能预留输出 ID、跳过零 ID 或声称销毁链已完成。早期容器报告中“共同 manager 临界区”的未来条件也不作为同锁已证据。

## 本轮停点

本轮停止在上述证据与实施设计：无新增行为代码、断言或编译结果。二进制表已命中且内容一致的路径是**无需新查询的下一实现候选**；完整未命中插入路径必须补 `284160` 证据。新查询状态为**未启动、未请求审批**，并非审批被拒绝或运行结果不明。未修改 UCRT 指纹门槛、Rea runner、配置、原游戏或存档；没有 commit/push。
