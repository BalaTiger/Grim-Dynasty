# tag3 与官方废铁记录解析结果

本轮已完成 **tag3 fresh 正常路径和较多字段的有界解析**。自有完整回归首次运行通过
**1,971 断言 / 57 场景组**，新增 **124 断言 / 5 场景**。
同一官方 `scrapmetal.dbr` 的 **38 字段已成功解析并逐项对照**；这不等于完整多归档初始化或物品工厂兼容。

## 实现与测试

`BinaryRecordParser` 按原始 DWORD `!=0` 将 tag3 转成 LSB-first uint32 位数组，
descriptor 保存累计 bit index 和原 count。`BooleanCount` / `CopyBooleanWords()` 独立于 tag0/1/2 的 raw word 数组；
`CopyRawValues(3)` 仍拒绝，避免把已转换的布尔位冒充原始输入 word。
重复 key 延续最新前插优先，真实字符串 getter 遇到 tag3 返回原 default。

字段索引按证据覆盖：

| 字段数 | 桶数/结果 |
| --- | --- |
| 2–15 | 4 桶 |
| 18–31 | 8 桶 |
| 34–63 | 16 桶 |
| 16/17、32/33 | `BinaryRecordBucketRoundingOutsideCoveredDomain`，零副作用拒绝 |
| >63 | 自主字段覆盖上限拒绝 |

整数边界仍缺目标 CRT `log` 精确返回位及 MXCSR 证据，未用宿主 Math.Log 或期望结果替代。
fresh slot capacity 恰为字段数，因此这条路径不触发后续增长；原生初始 slot 清零仅为 `24*(N>>1)` 字节。

新增自有测试包括：

- 四种 tag 交错，0/1/2/`80000000`/`FFFFFFFF` 布尔输入、多个元素、原始数值位及各类型独立偏移。
- 跨 bit 31/32、63/64 的字边界，重复布尔 key，以及布尔和字符串字段双向遮蔽。
- 2/15/18/31/34/38/63 字段成功布局，16/17/32/33 舍入拒绝及上限拒绝。
- 完整 backing bytes 下的逐字节窗口截断、65,536/65,537 输入 word 总预算、零 count、未知 tag。
- 输入、导出和 reader 生命周期隔离；参数异常与拒绝后的 gate / cursor / consumed 状态。

四处旧的 tag3 非法输入 fixture 改为真正未知 tag4/tag5，原断言含义与数量保持。
原混合/单类型、ARZ、自动映射、重映射、cache、getter 和对象相关回归全部通过；
另保留 Demo 与自有程序集元数据检查。编译采用系统 C#4 `csc /checked+`。

[完整测试输出](parser-tag3-validation/test-output.txt)；[测试记录](parser-tag3-validation/test-summary.json)。
串行构建与回归耗时 **10.466 秒**。没有为通过样本而增加全库字符串池预算，也未重跑监控器。

## 同一官方记录的实际结果

读取 `D:\Steam\steamapps\common\Grim Dawn\database\database.arz`，大小 **58,529,720 B**，
前后 SHA256 均为：
`9e3532250cdb611a4cc989997c8f97fcec2d6c422063a845e6c43cd538c30603`。

先验证冻结的旧 probe SHA，再锁定同一 record-name ID **6883** 的
`records/items/questitems/scrapmetal.dbr`。
本轮仅解释原档偏移 **42,034,543** 的 **323 B** 压缩载荷，不重读全体元数据或字符串池。
另有前后各一次全档流式指纹读取，buffer **64 KiB**，没有整档数组或新归档副本。

| 检查 | 实际结果 |
| --- | --- |
| 两个自有解码器 | 323 B → 456 B，逐字节一致；不是原生执行 oracle |
| body SHA256 | `5cbd10f21b424b0bec879fe101e77772fe42965ac363a5503c46907a62d91c7f` |
| 更新后 parser | `Known`，两遍均结束于456，completed fields=38，rewound=true |
| reader/gate | reader已消费，gate获取/释放成功 |
| 字段存储 | 16桶、mask15、shift0、38槽，初始原生slot清零元数据456 B |
| descriptor 与 tag0/1/2 | 全部原ID、count、各类型offset及uint32位模式与冻结观察一致 |
| tag3 | 7位：`0,0,0,0,1,1,0`，packed word **`0x30`** |
| pool / getter / factory | 未提供池，未调用官方上下文的 getter 或工厂 |

`Class` 的原 key ID **13**、value ID **28355** → `QuestItem` 仅为冻结的原 ID 注释；
没有把49个引用字符串裁剪或重编号成伪完整 pool。
探针仅在精确成功状态返回 exit0，Unsupported 返回2，实际失败返回1。

[官方复验详细结果](official-parsed-body-validation/probe.json)；
[执行记录](official-parsed-body-validation/run-summary.json)。
编译和单次复验共 **0.728 秒**；探针进程峰值 working set **27.41 MiB**，
该值不是全部工具进程树或硬内存预算的测量。

## 最后 leaf 的工具问题与取证

已确认 caller `0481BC → 0483A0`，但该 leaf 无 `.pdata` 条目。初次命令：

```text
dumpbin /NOLOGO /NOPDB /ERRORREPORT:NONE /DISASM:BYTES /RANGE:0x1800483a0,0x1800483a0 "C:\Users\zhuzi\Documents\Grim-Dynasty\.tooling\offline-cache-node\Engine.readonly.dll"
```

dumpbin 退出0但只输出 `48`，读取器报 `Undecoded instruction`。
2字节区间又将 `48`、`85` 输出为两行，触发读取器过严的 `2 != 1` 单行断言。
两次失败保留，修复连续原始字节行的解析后，在同一有界前缀方案内取得完整 CFG：

```text
0483A0 test rdx,rdx
0483A3 je   0483B9
0483A5 test rcx,rcx
0483A8 je   0483AF
0483AA mov  eax,[r8]
0483AD mov  [rcx],eax
0483AF add  rcx,4
0483B3 sub  rdx,1
0483B7 jne  0483A5
0483B9 ret
```

共26字节、10指令、无CALL；正常 non-null 目标下，确实填零全部 `ceil(bits/32)` DWORD，
不写 vector 元数据或 bit count。完整 fresh 准备链因此闭合。
最终 leaf 取证0.830秒，累计请求51字节、去重26字节；全 bit-helper 链独立代码984字节，
另读取 log 跳板6字节与常数8字节。相邻字节没有被计作函数体，未整库扫描。

[完整证据合同](BINARY-BOOLEAN-PARSER-EVIDENCE.md)；
[leaf 指令](offline-binary-bits-fill-prefix-walk-v3/0483a0-instructions.json)；
[命令、地址映射及哈希](offline-binary-bits-fill-prefix-walk-v3/inspection.json)。

所有原件/只读副本的 Engine SHA256 均保持
`4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`。

## 保留的缺口与交接

原档 **82,999 字符串 / 3,646,638 B** 仍超过现有 Source 的4,096项/2 MiB限制，
generated import 的源项预算也未放宽。保留原 ID 的只读 lazy resolver 已评估为可独立设计，
但本轮 body parser 不需要它；尚未将其实现或接入真实 getter。

非 fresh 解析、原生分配失败/EH、整数舍入边界、完整多归档初始化、ClassInfo/具体 Read、
完整物品工厂和原生注册继续标记未验证。自有 preflight 截断拒绝不是原生异常或回滚行为。
未启动原游戏/Rea/Java，未安装或扩权，未访问存档/Steam云，未 commit/push；
本轮无权限拒绝或待批准动作。

HEAD 保持 `6dfdae419d1c92f7738eefe241937057d83134df`。
修改前64个工程文件已备份；旧官方探针40项及副本完整保留。
[验收汇总](parser-tag3-validation/acceptance-parser-tag3.json)；
[保全审计](parser-tag3-validation/preservation-audit.json)；
[新检查点](../../.tooling/checkpoints/parser-tag3-complete-retry1/manifest.json)。

封存时首个脚本把所有 `.arz` 扩展名一律拒绝，因而在遇到测试自行生成的 fixture 时停止；验收与测试记录已先写成。原失败脚本与部分副本保留。已将7个自有小型 fixture 与上一检查点逐个比对大小和SHA（均一致，123–139 B），在独立 retry1 检查点完成封存；没有重跑测试、读取或复制原版归档。[封存恢复记录](parser-tag3-validation/seal-retry1.json)。
