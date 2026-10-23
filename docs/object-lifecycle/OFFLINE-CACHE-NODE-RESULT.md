# 离线节点构造：静态证据与最小实现

2026-10-20。Rea 节点查询仍暂停；本次使用本机已安装的 Microsoft `dumpbin`，仅恢复已知 `01F170`、`022860` 及必要 caller `05DEB0` 的正常成功语义。新增独立节点构造模型；没有接通完整 LoadFile miss。

## 证据范围

原文件为 `D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll`，SHA256：

`4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`

[提取记录](offline-cache-node-inspection/extraction.json) 保存原文件前后哈希、PE runtime-function 边界、原始字节及既有 caller 证据哈希。[反汇编记录](offline-cache-node-inspection/disassembly-summary.json) 保存实际命令、输出哈希和完整字节覆盖核对。

| 目标 | 半开范围 | 字节数 | 指令数 |
| --- | --- | ---: | ---: |
| 节点分配与链接字段 | `18001F170..18001F1B4` | 68 | 22 |
| 名称移动 | `180022860..1800228E1` | 129 | 36 |
| 已知构造 caller | `18005DEB0..18005DF0C` | 92 | 24 |

三段共 **289 字节**，全部与提取原始字节逐点一致；caller 两个相对 CALL 的目标及全部指令也与旧 Ghidra 归档相符。289 是解释及反汇编的代码范围，**全文件哈希与只读工作副本的创建读取了整个 DLL**。完整副本仅保留在 `.tooling/offline-cache-node/Engine.readonly.dll`，不纳入检查点。

已有工具路径为 `D:/Microsoft Visual Studio/2022/BuildTools/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe`，文件 SHA256 `cfee7ed764b7b0866aae0be12f9c0ad2a7f5aecc4046ba2b7de26335827ca815`。现场只读签名检查为 Microsoft Corporation、Authenticode Valid，版本 `14.44.35229.0`；摘要中这些现场检查信息为静态记录，脚本本身固定验证工具哈希。

固定调用使用 `/NOPDB /ERRORREPORT:NONE /DISASM:BYTES /RANGE`，单次限时10秒、输出64 KiB。三次最终采集分别35/31/31毫秒，另有一次帮助查询及两次单函数探测，均已记录。没有目标加载、符号服务、后端、安装或权限扩大。提取进程自身峰值 RSS 为60284 KiB，反汇编控制进程自身为41896 KiB；这不是工具进程树总内存测量。

## 已闭合的正常成功语义

`01F17A` 以 `ECX=0x38` 覆盖入口 RCX，故先前 caller 的 RCX 来源缺口可关闭。正常有效非 null 分配后，新节点 `+0/+8` 保存 RDX/R8；若入口 RDX 为零，两字段均指向新节点自身。此函数不改邻节点或 count，也不清零其余40字节。

`05DEB0` 将节点 `+10` 作为 fresh 名称目标，初始化首字节0、length0、capacity15，再执行 `022860`：

- source capacity 小于16时，调用 `memmove(dst,src,length+1)`。本模型有效短串域为 capacity15、length≤15，包括逻辑终止符。
- capacity≥16时，转移同一个 heap buffer 身份，不复制堆载荷。短内容或空内容只要 capacity≥16仍走此分支。
- 目标得到源原 length/capacity，源重置为 capacity15、length0、首字节0。剩余 inline 字节与 padding 的值不在逻辑模型内。
- 原函数正常返回寄存器 RAX 保持旧 capacity；caller 忽略它，最后返回原节点。
- 名称移动完成之后，caller 才读取源 pair `+20` 的 opaque Table 并写节点 `+30`。源 Table 不清除，不构造或 Dispose Table。

[固定导入绑定](offline-cache-node-inspection/move-import-binding.json) 证明 `0x180302728 → VCRUNTIME140.dll!memmove`，hint61。IAT/OFT目标qword均为 `0x417C86`，符号原始字节为 `6d656d6d6f766500`。仅解释947字节必要元数据，没有导出全导入表或跟进新函数体；标准 memmove 正常完成仍作为外部契约使用。

## 自有实现与可观察边界

[RecordCacheNodeConstruction.cs](../../src/task_items/RecordCacheNodeConstruction.cs) 提供独立 owned-name/pair/node 及 `RunOnce` attempt。名称完整字节长度限65536、capacity限131072；这些是自有覆盖预算，不是原生限制。输入构造克隆外部数组，公开输出也返回副本；heap身份是不可写的自有存储对象标记，不是原生地址。

短串建立独立存储，堆分支将实际同一自有 Storage 转移给新节点，再清空源。节点只初始化自身 next/previous；next非空时原 previous（包括明确null）保留，next为空时忽略传入 previous并双自指。构造不挂链、不改 count、不发布索引。`ToEntrySnapshot` 是显式不可变投影；后续索引测试必须另行提供既有 publication 模型要求的已挂链、已计数入口。

所有可能失败的自有结果、名称及节点分配放在源修改前。自有分配失败返回 Unsupported，源保持原样；这不等同于原生 OOM 回滚。一次 attempt 缓存同一结果，成功或拒绝后不会重复消费源。现有 ObjectNameString、名称赋值、缓存查找及索引实现保持原样。

该实现仅承诺有效存储、非别名 fresh 目标、独占访问及原生分配/复制正常完成的逻辑结果。原函数的 null分配分支不能解释为安全null返回：`01F19D` 会从RAX生成 `+8` 地址并继续写入。原生 allocator/EH、无效指针、别名/重叠目标、完整原始内存布局及失败中间态均未恢复。

## 验证与剩余工作

行为回归共 **1260条断言、27组场景、1个ItemQuestDemo通过**，新增51条断言及2个组合场景。验证结果见 [本阶段验收记录](offline-cache-node-validation/acceptance-offline-cache-node.json) 和 [完整测试输出](offline-cache-node-validation/test-output.txt)。仅运行本次行为变更需要的自有 C#4 `/checked+` 回归，没有重跑已通过的监控诊断测试，也没有运行原版游戏或 DLL。

首轮 wrapper 在全部测试通过之后，因 PowerShell 7 不支持 `ReflectionOnlyLoadFrom` 而于元数据步骤退出1；原 failed summary和完整日志保持原样。Windows PowerShell 5 的独立脚本调用又被本机 AuthorizationManager 拒绝（SecurityError/UnauthorizedAccess），没有重试该调用或改执行策略。随后仅用当前环境的只读 `PEReader/GetMetadataReader` 完成 [程序集引用检查](offline-cache-node-validation/assembly-references.json)，依赖只有自有模块、mscorlib及System.Core。测试没有重跑；修订后的 wrapper 已改用该元数据接口，但未宣称整份修订 wrapper再次执行通过。

完整 LoadFile miss 仍返回 `UnimplementedRecordCacheMissLoader`。archive取数、普通Table构造、临时名称构造及完整调用链/异常和计时行为，不能由本次独立节点构造自动推导为已完成；原生物品工厂与对象注册亦未因此补齐。

原 QueryFullProcessImageName 故障没有重现，根因仍未知；[诊断增强检查点](MONITOR-DIAGNOSTIC-RESULT.md) 和 [原失败报告](CACHE-NODE-QUERY-FAILURE.md) 保持历史原样。本次没有启动Rea/Java、安装、提权、系统配置变更、存档或Steam云访问、commit或push；无待批或结果不明的运行操作。
