# 归档字符串映射生成结果

已实现有证据支持的正常状态逻辑投影，并接入现有加载链。调用者提供按顺序导入的原始字符串表；映射、base 和目标池由实现生成，无需再手工填数组。

首次完整回归 **1847 条断言 / 52 组场景、Demo 和 PEReader 通过**，新增 **122 / 6**，耗时 10.4078016 秒。未重复跑已通过测试。

## 已验证行为

- 初始导入按完整字节去重，按首次出现分配 ID。共同后处理追加缺失的空串。
- 追加 base 等于之前追加的原始项总数，包含重复项，不含初始池；空追加仍产生 selector。
- 无大小写折叠、字符编码转换或 NUL 截断。原生无效长度分支复用上一临时字符串；安全截断及预算停止保留前缀状态，未知映射槽不会冒充零。
- 三个自有物理 ARZ 的不同本地 ID 自动合并：初始池 4 项、两个追加池各 5 项，形成 10 项映射和 7 个目标字符串，base 为 0、5。已通过实际解码、重映射、parser、getter 和缓存。
- 生成的上下文是快照，逐项验证加载来源的字符串池；错误来源在分配重映射 scratch 前停止。关闭文件后的缓存命中不再次读取或导入。

## 接口

```csharp
var state = new ArchiveStringImportState();
var initial = ArzRecordRemapContext.ImportChecked(state, rawA, false);
var appended = ArzRecordRemapContext.ImportChecked(state, rawB, true);
var context = ArzRecordRemapContext.FromGeneratedImport(state, appended);
// Pass context to the existing ArzRecordBinarySource overload.
```

rawA/rawB 是已由独立有界 ARZ reader 读取的不可变数据。初始/追加顺序仍由调用者明确选择；这不是完整原生归档发现、构造或 offsets 选择器恢复。原有显式 mapping 接口及默认不 remap 接口保持原语义。

## 保留缺口

- Complete native archive construction/import/cache initialization (284080/284160), offset-vector acquisition and selection.
- Initial duplicate source IDs are deduplicated but lack a proven raw-ID remap; generated loading rejects that case.
- Native container layout, allocator failures/EH, scratch ownership, full factory/Read chain.
- Official archive sample compatibility remains separately unverified; no official sample read in this phase.

特别是：初始重复项可完成字符串去重，但原始 ID 不再保持时，生成加载上下文明确拒绝；没有发明初始映射修复。initial 无映射直通属于 fresh 自有模型合同，不能推断任意原生 archive 的 map 指针为空。native 节点/索引/allocator 的正常成功和有效状态是明示前提，指针缓存副作用并未作为完整原生初始化执行。

本轮仅使用已安装 dumpbin 定点读取 8 个逻辑函数的 9 个片段，共 **3326 字节 / 895 指令**；另核 6 字节既有比较 thunk 和 1 字节空串常量。比较绑定为 VCRUNTIME140.dll!memcmp。所有保存的原版读取前后和既有副本 SHA256 一致：`4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`。无原游戏/Rea/Java 启动、额外安装、扩权、存档/云访问或 commit/push。

## 证据与交接

- [完整取证合同](ARCHIVE-STRING-MAP-EVIDENCE.md)
- [机器验收](archive-string-map-validation/acceptance-archive-string-map.json)
- [回归输出](archive-string-map-validation/test-output.txt)
- [增量检查点](../../.tooling/checkpoints/archive-string-map-complete/manifest.json)，依赖上轮 record-remap-complete 检查点链；不包含完整原版 DLL。
