# 注册辅助函数与局部状态模型

2026-10-20。已续上既定停点，对 `5DAF40`、`5DA5DC`、`5DAEB8` 及其必要直接目标完成有界静态分析。未重复原来的 writer 取证或旧测试。

## 已确定的原生行为

| 目标 | 已证职责 | 失败语义 |
|---|---|---|
| `5DAF40` | 通过 `malloc` 分配，失败时调用 `_callnewh`，非零处理结果触发同尺寸重试 | 正常返回只可能是非零指针；处理结果为零时按尺寸是否 `UINT64_MAX` 进入两个不同的 C++ 抛异常路径。异常类名、构造和 unwind 未恢复 |
| `5DA5DC` | 跳到 `free`，释放旧 children storage | 无布尔成功码；可见跳板不析构旧 children、不清零 parent 字段。非法堆状态未验 |
| `5DAEB8` | 根据解码值选择 `_crt_atexit` 或 `_register_onexit_function` 登记回调 | 外部返回零且 callback 非空时最终返回 0，否则 -1。失败不撤销此前已发布的 children |

退出分支依据为 `ROR64([A580A0] XOR [A65CA8], [A580A0]&63)==UINT64_MAX`。未读取或猜测运行时全局状态，未执行退出回调。

本轮新增原版正文 **280 B / 77 条指令 / 14 次固定范围 dumpbin**；明确元数据读取 **1,679 B**。前后 20 次完整指纹校验使用 64 KiB 缓冲，原版 SHA256 均为 `cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2`。指纹字节和 dumpbin 内部 I/O 不计入语义读取数。

具体指令、导入绑定、限制与下一证据边界见 [证据记录](CLASS-REGISTRATION-HELPERS-EVIDENCE.md)。本轮没有读取 Engine.dll 或正式 ARZ。

## 自有实现边界

新增 `src/task_items/ClassRegistrationInitialization.cs`，只表达固定 Item/QuestItem writer 的局部状态转换。它保存 parent、名称、count、children storage 和 creator 身份；依赖操作由明确传入的自有回调提供，绝不把原版地址当作可调用函数。

该模型保留原先 children 的顺序和身份，每次调用均再次追加，不去重。它按证据顺序执行名称写入、分配、复制、追加、count 增加、旧 storage 释放、新 storage 发布及尾部登记。名称复制返回空仍继续；parent 为空仍走尾部登记。

结果分别记录完成的局部写入、外部回调尝试、未发布的新 storage 及尾部返回值。自有故障注入中，分配中断保留此前名称/parent；释放中断保留已增加的 count 和未发布的新数组；尾部登记 -1 保留已发布 children。这是托管模型的前缀效果报告，不是原版可恢复异常或 EH 的证明。

正常返回 -1 仍是已知局部结果，不能冒充“登记成功”。空 allocator 返回和非 `0/-1` 的尾部返回都超出已证正常合同。数组容量、负 count 和复制预算的限制明确属于自有覆盖边界；不宣称原版先安全拒绝这些状态。

纯退出判断函数覆盖旋转、两个调用选择、原始 EAX 等零判断以及空 callback 的两层返回转换。allocator 重试内部、真实 CRT 堆、退出表和 callback 正文没有实现；它们仍是依赖边界。本模块不生成假的 Engine 根图，也没有接通原生 factory 或正式样本分派。

## 验证与可恢复状态

本轮 **65 条新增断言 / 6 个场景组通过**，新增独立库、测试程序和完整自有 Item 模块编译均成功，见 [成功运行记录](class-registration-helpers-retry1-validation/targeted-run.json)。旧基线 **2,282 断言 / 83 场景组**未重跑，不能与新测试混称为一次完整回归。

构建接入只在现有 `build_items.ps1` 中新增源码和测试列表条目；其他已有产品源码保持原样。新增针对性脚本为 `src/task_items/build_class_registration.ps1`。

初次运行该专用脚本被 Windows PowerShell `AuthorizationManager` 以 `UnauthorizedAccess` 拒绝，未开始编译或测试；[首次日志与状态](class-registration-helpers-validation/targeted-run.json)保留。未重试该脚本、绕过或修改执行策略，改由 `.tooling/run-class-registration-helpers-retry1.mjs` 直接调用已安装 `csc.exe` 和编译出的自有测试程序完成验收。该工具路径不启动 PowerShell，未安装或扩权。

本轮之前检查点 `.tooling/checkpoints/class-registration-helpers-before/manifest.json` 的 SHA256 为 `ef8ac5d0b07a1012ea2b0414eb945579a1a7b1be81e6ebc9b654595e2befabd5`，它引用上一完整静态阶段检查点。新检查点为 `.tooling/checkpoints/class-registration-helpers-complete/manifest.json`；恢复时保留其引用的前置检查点。

HEAD 保持 `6dfdae419d1c92f7738eefe241937057d83134df`。未启动游戏、原 DLL、Rea 或 Java；未安装、扩权、接触存档/Steam 云或 commit/push。被拒绝的专用 PowerShell 脚本仍未执行，所需编译和测试已由上述直接工具调用完成；没有待批准或结果不确定的操作。这不是自动批准审查的拒绝。

## 剩余缺口与下一步

Engine Actor `41CFC0` 的父项是尚未识别的 `41CF30`，不能简化成 Object 根 `41CF00` 直接包含 Actor。下一最小证据是 **`41CF30` 的48字节记录及导出身份**，继而定位 Object、中间节点、Actor 的准确注册写入者与 children 顺序；不以 parent 静态值替代实际 children 写入证据，也不扩大为全引擎扫描。

本轮只确定这个下一证据边界，没有新增 Engine 读取。完整根注册图、异常具体类型及清理、原生对象注册、完整物品工厂和正式样本兼容仍未完成或未验。目标仍是独立、可重新编译的行为等价代码，不承诺还原原始源码，也不兼容原存档。
