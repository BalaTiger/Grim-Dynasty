# Engine 根关系的有界静态证据

2026-10-20。本段从 `class-registration-helpers-complete` 检查点继续，只处理 Engine 中 Actor 的父 ClassInfo、其身份及注册写入来源。上一检查点 190 个当前文件和 190 份副本均经只读长度/SHA256 核验，无差异。

## 执行限制

先前 `build_class_registration.ps1` 被 PowerShell `AuthorizationManager` 拒绝的状态保持原样。本段不重试该脚本，不复制、改名或改用等效命令完成被拒绝的构建，不修改执行策略，不进行任何编译或测试。

本段新增工具只做已经允许的只读静态分析，或者保存工程内分析记录。无游戏/DLL 加载、Rea、Java、安装、扩权、存档/Steam 云访问、commit 或 push。不得用前一轮测试次数充当本轮结果。

## 41CF30 的精确记录

文件 `D:\Steam\steamapps\common\Grim Dawn\x64\Engine.dll`，4,485,632 B，SHA256：

```
4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153
```

既有 `offline-class-registration-metadata/metadata.json` 已读 Actor `41CFC0` 的 `+10` 指针为 preferred VA `18041CF30`，但既有 Object 读取止于 `41CF30`，并未覆盖新目标。新工具 `read_engine_class_parent_record.mjs` 只读取 RVA `41CF30`、文件偏移 `4306736` 的 48 B，没有自动跟随任何指针。

新报告：[offline-engine-class-parent-record/metadata.json](offline-engine-class-parent-record/metadata.json)。

| 偏移 | 文件值 | 本轮可作的陈述 |
|---|---|---|
| `+00` | `180348830` | 静态虚表指针；未展开 |
| `+08` | 0 | 名称指针尚为空 |
| `+10` | `18041CF00` | 静态 parent 指向已知 Object ClassInfo |
| `+18` | 32-bit 0 | 静态 children count 为 0 |
| `+1C` | 32-bit 0 | 邻接字原值；不自行赋予语义 |
| `+20` | 0 | children storage 指针尚为空 |
| `+28` | `1800499A0` | 静态 factory 身份；没有读取或执行它 |

不能从这些零字段证明“运行时没有子节点”，也不能把磁盘 parent 关系当成反向 children 注册完成。

## 导出身份

旧工程库存只含限量名称/候选，不含完整 Engine 名称到 RVA 索引。旧 `offline-class-registration-exports/discovery.json` 保留了部分实际名称与原始字节，包括 index 6118 的字符串位于前缀 `?classInfo@` 之前，index 6119 是 Actor。因此新检索复用这些已哈希固定的名字缩小 lower-bound 范围，随后逐项读取必要 ordinal/EAT。

新报告：[offline-engine-class-parent-exports/discovery.json](offline-engine-class-parent-exports/discovery.json)。新增读取 **166 B 导出元数据**，第六个前缀匹配即命中：

```
?classInfo@Entity@GAME@@1VRTTI_ClassInfo@2@B
name index = 6124
RVA = 0x41CF30
```

读取在此立即停止，没有枚举剩余类或全部别名，没有读取这些导出指向的数据或代码。准确结论是该导出绑定了 **Entity::classInfo**，并非由邻接布局或 factory 名称猜测。

结合此前 Actor/Object 记录，可确认磁盘上的 parent 链：

```
Actor (Engine 41CFC0) → Entity (Engine 41CF30) → Object (Engine 41CF00)
```

本链不包括已初始化 children、实际遍历结果或原生实例构造。

## 注册 writer 的定位起点

旧导出证据中相邻 index 1369、1370 的完整名称分别为 `??_FResourceLoader@GAME@@QEAAXXZ` 和以 `?AchievementIsUnlocked@` 开头的名称。按 PE 已排序命名导出表合同，`??__E?classInfo@Entity@GAME@@` 落在这两个相邻名称之间，因此该命名形式没有导出；这不排除未导出的初始化器或其他命名。

旧 Engine 边界报告 `offline-depot-ctor-boundaries/boundary.json` 保存了 `.pdata`：RVA `424000`、size 146520、12210 项。它曾读取包含 PE 入口字段的 optional-header 范围，却没有保存该字段原始值，故不能从旧报告恢复入口值。

新工具 `read_engine_root_entry.mjs` 只补读文件偏移 328 的 **4 B**，得到 Engine PE entry RVA **`2DD044`**，见 [offline-engine-root-entry/metadata.json](offline-engine-root-entry/metadata.json)。没有读取主 EXE 入口，也没有用 factory `499A0` 或虚表槽作为 writer 候选。

后续正文只允许从此入口的已证直接调用或明确初始化表项决定目标，累计正文上限 8192 B。精确 unwind 范围仍只代表片段；没有准确边界时必须显式限制入口前缀，不能读取邻近函数代替目标。
