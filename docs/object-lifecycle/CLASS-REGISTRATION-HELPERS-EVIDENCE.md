# Item / QuestItem 注册辅助函数的有界证据

2026-10-20。本轮继续上一检查点的固定三个目标：Game RVA `5DAF40`、`5DA5DC`、`5DAEB8`。所有地址均为此版 Game.dll 的 RVA，除非明确标记 Engine。原文件只读；未加载或执行原 DLL。

原版：`D:\Steam\steamapps\common\Grim Dawn\x64\Game.dll`，11,385,856 B，SHA256 `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`。dumpbin SHA256 `cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815`。每次检查使用 64 KiB 缓冲区校验前后完整文件哈希；这些指纹读取及 dumpbin 自身未计量的元数据 I/O 不计入下面的语义读取数。

本轮新增正文合计 **280 B、77 条指令、14 次固定范围 dumpbin**。这包含三个 5 字节直接跳板后各一个 `CC`；这些是固定入口前缀，不能把补齐字节误称为逻辑函数的一部分。未重复读取此前的 Item/QuestItem writer 正文。

## 读取清单

| 报告目录（均在本目录） | 固定正文范围，末地址不含 | B | 指令 |
|---|---|---:|---:|
| `offline-class-static-helper-exit-entry` | `5DAEB8..5DAECF` | 23 | 8 |
| `offline-class-static-helper-exit-body` | `5DAE68..5DAEB7` | 79 | 23 |
| `offline-class-static-helper-exit-thunks` | `5DC3CE..5DC3D4`、`5DC3C2..5DC3C8` | 12 | 2 |
| `offline-class-static-helper-storage-entries` | `5DAF40..5DAF46`、`5DA5DC..5DA5E2` | 12 | 4 |
| `offline-class-static-helper-storage-bodies` | `5DA6C4..5DA700`、`5DA6BC..5DA6C2` | 66 | 22 |
| `offline-class-static-helper-storage-failure` | 三个 6 B 导入跳板；`5DB9F8..5DBA18`、`5DB9D8..5DB9F8` | 82 | 17 |
| `offline-class-static-helper-throw-thunk` | `5DC362..5DC368` | 6 | 1 |

每个目录保存原始小片段、dumpbin 输出、连续逐字节校对后的指令 JSON 和 `inspection.json`。计划位于 `class-static-init-plans/helper-*.json`。精确 `.pdata` 范围只证明 unwind 片段，不自动证明全部异常清理路径。

边界检查 `offline-class-helper-boundaries/metadata.json` 新读 72 B，复用旧的已钉住哈希的 `.pdata` 条目。它确认 `5DAF40` 和 `5DA5DC` 均在相邻 unwind 范围的空隙中，随后各读固定 6 B 前缀，没有选择邻近函数正文。

## 5DAF40：分配与失败转移

`5DAF40` 直接跳到 `5DA6C4`。后者保存原请求大小于 RBX，以原 RCX 首先调用 `5DC374`；该跳板指向 IAT `60B880`。

正常返回路径只在 RAX 非零时退出。若为零：

1. 将原请求大小传给 `5DC3A4`（IAT `60B890`）。
2. 其 EAX **非零**时重试分配；判断不是“大于零”，也没有可见重试次数上限。
3. 其 EAX 为零时，若原请求为 `UINT64_MAX`，调用 `5DB9F8`；其他请求调用 `5DB9D8`。

两个失败包装分别调用 `5DB960`、`5DB900` 构造栈上对象，然后把同一栈对象和不同描述地址交给 `5DC362`：

| 请求 | 包装入口 | 构造入口（未展开） | 描述地址（未读内容） |
|---|---|---|---|
| `UINT64_MAX` | `5DB9F8` | `5DB960` | `870548` |
| 其他 | `5DB9D8` | `5DB900` | `8704E8` |

`5DC362` 跳向 IAT `60B7B0`。两包装在此 call 后均为 `INT 3`，没有正常 `ret`。因此不能把分配失败模拟成返回 null 后让 writer 继续访问数组。外部处理器本身也可能抛出或不返回；本轮不假定它一定正常返回。

精确导入绑定见 `offline-class-helper-storage-imports/metadata.json`：`60B880=malloc`、`60B890=_callnewh`、`60B7B0=_CxxThrowException`。这使“堆分配、调用 new handler 重试、C++ 异常转移”的职责成立；**两个原生异常类名仍未证明**，不能仅因其形似常见 CRT 模式而命名为 `bad_alloc` 或 `bad_array_new_length`。异常对象构造、catch/EH/unwind 和进程最终状态均未恢复。

先前 writer 的大小计算仍须原样区分：32 位 `count+1` 回绕后符号扩展，以无符号 64 位乘 8，溢出则传 `UINT64_MAX`。例如旧 count=-1 得到请求 0，不能据此宣称原版先拒绝负 count；正常分配返回后 writer 仍会使用旧 count 作下标。自有托管模型对此的拒绝只是覆盖边界。

## 5DA5DC：释放旧 storage

链为 `5DA5DC → 5DA6BC → 5DC36E → [60B888]`，最终导入为 `free`。这些跳板不检查 RCX，不读取/使用 EDX，不设置可检查的成功码，也没有进行 ClassInfo 析构调用。

因此在已读 writer 中，这一步是释放旧 children 指针数组；旧元素仍是指向既有 ClassInfo 的指针。可见顺序是 **append → parent.count++ → free(old storage) → publish(new storage)**。不能把它实现成递归销毁旧 children，不能将未知的 RAX 当成释放成功判据，也不能声称失败时已自动回滚 count。

Microsoft 的 [`free` 文档](https://learn.microsoft.com/en-us/cpp/c-runtime-library/reference/free?view=msvc-170)说明 null 参数会被忽略，非法指针不属于正常使用合同。本轮没有验证本机运行时堆、非法指针结果或释放错误处理；不为原版捏造一个可测试的 free 失败返回值。自有故障注入只用于检查中断点之前的效果保留。

## 5DAEB8：退出回调登记与返回转换

`5DAEB8` 调用 `5DAE68`；后者令：

```
x = qword [Game A580A0]
decoded = ROR64(x XOR qword [Game A65CA8], x & 63)
```

当 `decoded == UINT64_MAX` 时调用 `5DC3CE(callback)`，否则调用 `5DC3C2(&A65CA8, callback)`。此计算来自指令，不要求读取当前全局状态；本轮没有读取运行时 cookie/onexit 表，也没有因常见命名而把数据结构布局当成已证事实。

`offline-class-helper-exit-imports/metadata.json` 新读 58 B，精确绑定：

| IAT | 模块 | 名称 | 实参 |
|---|---|---|---|
| `60B958` | `api-ms-win-crt-runtime-l1-1-0.dll` | `_crt_atexit` | callback |
| `60B968` | 同上 | `_register_onexit_function` | `&A65CA8`、callback |

无论哪个分支，内层只检 EAX 是否为零。零时返回原 callback，任何非零时返回 0。外层把内层非零 RAX 变为 int 0，零 RAX 变为 int -1（实际 RAX 是零扩展的 `00000000FFFFFFFF`）。所以 null callback 即使外部返回 0，最终仍为 -1。

Item/QuestItem 传入的已证 callback 身份分别为 Game preferred VA `180604450`、`180605530`。没有执行或读取它们的正文，不能宣称已恢复其清理行为。登记失败发生在 children 发布之后，不能撤销名称、parent/count/children 的已发生写入。

Microsoft 的 [onexit 表接口文档](https://learn.microsoft.com/en-us/cpp/c-runtime-library/execute-onexit-table-initialize-onexit-table-register-onexit-function?view=msvc-170)说明该接口用于追加退出回调。其运行时 opaque 表内部实现未读取；本项目的分支与状态码依据是当前 Game 指令。

## 外部文档与原版证据的边界

[`_callnewh`](https://learn.microsoft.com/en-us/cpp/c-runtime-library/reference/callnewh?view=msvc-170) 文档说明它调用已安装的 new handler；[`_CxxThrowException`](https://learn.microsoft.com/en-us/cpp/c-runtime-library/reference/cxxthrowexception?view=msvc-170) 文档说明它启动运行时异常处理。这些是导入 API 的公开合同，不是本机 CRT 实现、异常类身份或一次成功执行的证明。没有调用这些原生导入。

## Engine 根注册图最小剩余证据

复用 `offline-class-registration-metadata/metadata.json` 已知：Object `41CF00`、Actor `41CFC0`；但 Actor.parent 是 **`41CF30`**，不是 Object。下一步最小数据对象是这个中间地址的 **48 B ClassInfo 记录及其已有导出身份**，不能按相邻布局给它起名。

精确待证字段是 Object 的 `41CF08/41CF18/41CF20`，中间节点的 `41CF38/41CF40/41CF48/41CF50`，Actor 的 `41CFC8/41CFD8/41CFE0`。若中间 parent 仍非 Object，只能沿明确指针决定下一步。parent 文件值不证明反向 children 已注册；必须定位对应字段写入者及初始化来源，再读取其必要边界。

目前没有这些 Engine writer 的已证函数入口。本轮**不开展全引擎写引用扫描**，也不以 factory 槽 `04A0D0/049810` 或 ClassInfo vslot `274E30` 充当 writer 候选。已知 dispatcher `274EC0` 和 root caller `17E170` 可复用，无需重读。

即使恢复一条根到 Item 的路径，前面的兄弟子树仍可能影响按顺序查找。路径存在、完整图、实际分派以及真正物品构造须分开验收。正式样本兼容、原生运行时对象注册及完整 Item/QuestItem 工厂继续标为未验。
