# 本机官方 ARZ 单记录检验

已在当前机器找到并读取一个官方安装归档。**单条记录的独立读取、解码已取得实际证据；现有行为 parser 和完整加载链仍明确拒绝。** 没有增加合成断言，也没有把结构观察计作 native parser 通过。

## 实际文件与样本

路径：`D:\Steam\steamapps\common\Grim Dawn\database\database.arz`。本轮只枚举该标准 database 目录，找到 database.arz；没有全盘、旧机或 DLC 目录搜索。

- 文件大小 **58,529,720 字节**；marker **2**、ARZ 格式版本 **3**。游戏发行/构建版本未从格式版本推断。
- SHA256：`9e3532250cdb611a4cc989997c8f97fcec2d6c422063a845e6c43cd538c30603`。两次探针均保留读取前后相同哈希。
- **34,180** 条记录；record table **1,342,625 B**；string table **3,978,638 B**，含 **82,999** 字符串和 **3,646,638 B** payload。
- 唯一样本：`records/items/questitems/scrapmetal.dbr`，原始 record-name ID **6883**，class **QuestItem**。
- payload 绝对偏移 **42,034,543**，压缩 **323 B**，解压 **456 B**。输出长度驱动 profile 消耗恰好 323 B，与现有输入长度驱动 decoder 的输出逐字节一致。
- body SHA256：`5cbd10f21b424b0bec879fe101e77772fe42965ac363a5503c46907a62d91c7f`，也与迁移报告中的历史 scrap body 哈希一致。两份自有实现的一致性并非原游戏执行 oracle。

## 现有实现实际停在哪里

| 层 | 本轮实际结果 |
| --- | --- |
| Source 构造 | `ArgumentOutOfRangeException(readLimits)`：真实池 82,999 项 / 3,646,638 B 超出既有 4,096 项 / 2 MiB 上限。 |
| 使用原加载链池上限的 raw reader | 读取 header 和 string count 后 `ArzRawStringCountBudget`；尚未分配完整池。 |
| 独立 raw reader，按已估算元数据配置本次读取 | 成功验证完整元数据，只解码指定记录；未改变 parser/source 常量。 |
| 现有 BinaryRecordParser，直接接未改动 body | `Unsupported: BinaryRecordTagOutsideCoveredDomain`。第 **5** 字段（从0计ordinal4），偏移 **48**，tag **3**，原 key ID **916**，`allowTransparency`。游标保持 **0**，reader 未消费，门锁正常释放。 |
| 独立 wire observer | 观察到 **38** 字段/38 words，tag0/1/2/3 分别 **8/11/12/7**，最终游标456，无重复key。只保留原ID和uint32位，不模拟native tag3 bit storage。 |

`Class` 的原 key ID 为13，tag2值ID为28355，对应`QuestItem`。只导出实际引用的49个字符串注释（原ID范围1..48634）；没有裁剪、重排或伪造一个可供现有getter使用的dense pool。现有parser本身无需pool即可检查body，因此本轮精确观察到了tag3拒绝，而不是被池限制遮住。

## 可行性判断与后续缺口

完整 raw 元数据数组估算 **5,985,283 B**；加入选定小记录的保守数组上限为 **6,247,466 B**，在既有可配置 **8 MiB** working预算内。单record packed/body各上限64KiB，本次实际仅323/456 B。完成探针进程的实测峰值工作集 **29.57 MiB**；它与累计数组预算口径不同，也不代表编译器或整棵进程树峰值。

可行性尚不等于已验证成功：tag3恢复后，该记录38字段仍超过现有parser的15字段覆盖域；完整池规模和原生归档初始化/选择器仍需有证据的实现。generated initial import 的65,536源项预算也小于82,999，这是源码/头部推导，未为测试该guard另复制整池。下一步最直接的真实数据目标是tag3分支及较多字段的索引初始化，不是单纯调高常量。

## 读取范围与失败保全

[精确读取范围](official-record-probe-retry1/read-scope.json)列出每次offset和长度。完整索引/字符串表各只读取一次；没有批量解码记录。选定323 B payload读取两次用于两decoder对照。另为两次探针分别做前后哈希，共4遍全文件顺序指纹读取，每次buffer64KiB；没有整文件分配或复制。

首探针因自身检查参数`knownNameIds=null`停止，当时仅解释了28 B头部/count，未读完整索引或payload。失败源码、可执行文件、日志及前后哈希均保留。补齐最小合法自有name-ID参数后在新目录完成一次探针，未修改生产代码或预算。此参数仅用于触发Source预算guard，不冒充真实归档名称上下文。

无原游戏代码、Rea/Java、dumpbin运行；无安装/扩权、存档或Steam云访问、commit/push。当前无权限拒绝、待批准或不确定动作。不需要用户再提供样本位置。

- [本轮机器验收](official-record-probe-retry1/acceptance-official-record.json)
- [实际探针数据](official-record-probe-retry1/probe.json)
- [首轮停止记录](official-record-probe/probe.json)
- [增量检查点](../../.tooling/checkpoints/official-record-probe-complete/manifest.json)
