# 可编译ARZ/ARC资源模块

> 验证对象澄清（2026-10-18）：自写模块的编译、运行和离线测试不依赖原版DLL、Steam、BIOS或Sandbox；这些隔离/授权门槛仅约束合法原版的动态行为对照，不阻塞自写实现。最终目标是独立的行为等价可编译代码，当前尚非完整游戏。详见工程 docs/VALIDATION-TRACKS.md。

这是行为等价格式模块恢复的下一切片，不代表整个引擎或独立游戏已恢复。代码为自行实现，不加载原Game/Engine DLL，不执行陌生仓库代码。

## 源码与构建

`src/resource_codec/ResourceContainers.cs`：`ResourceInput`、`ArzArchive`、`ArcArchive`、条目与类型化记录；依赖同目录既有 `Lz4Block.cs`。`ResourceProbe.cs`提供只读CLI，`ResourceFixtureProbe.cs`仅供合成数据测试。

使用本机已安装Framework C#编译器，无NuGet或SDK下载：

```powershell
& .\src\resource_codec\build_containers.ps1 -IncludeTests
& .\.tooling\resource-containers-build\ResourceProbe.exe 'D:\Steam\steamapps\common\Grim Dawn\database\database.arz' --record records/items/gearweapons/swords1h/b000_sword.dbr
& .\.tooling\resource-containers-build\ResourceProbe.exe 'D:\Steam\steamapps\common\Grim Dawn\resources\Quests.arc' --member gdareab/mq_murdersandworse.qst
```

构建产物：`.tooling/resource-containers-build/GrimDynasty.Resources.dll`、`ResourceProbe.exe`，可选`ResourceFixtureProbe.exe`；均在私有忽略目录。CLI还支持`--index`和逐行输出摘要的`--verify-all`，不提供重打包或写原资源命令。CLI的文件入口限定已授权安装内.arz/.arc；内存流API供纯模块测试。

公开调用示例：

```csharp
using (var input = ResourceInput.OpenGameResource(path))
using (var db = new ArzArchive(input)) {
    ArzRecord r = db.ReadRecord(recordName);
    // r.fields / r.field_types preserve zero values, arrays and float32 values.
}
```

ResourceInput采用FileAccess.Read/FileShare.Read，仅按范围读索引与当前载荷，不把整套安装复制进内存或工程。归档Dispose关闭其输入；调用者用外层using保证构造失败也关闭句柄。实例为顺序读取，不宣称线程安全或原私有ABI兼容。

## 验收

正式报告 `acceptance.json`，最终构建耗时32.152秒完成：

- 完整ARZ索引34,180条，与Python全部元数据一致，包括类名、偏移、长度、时间戳。
- **34,180个记录体全部解码，9,623,280个字段**逐一进入独立类型化摘要比较：字段名、类型、数组长度、int32、字符串及float32位值；记录体SHA256另行比较，全部一致。
- Soldier、Gladius、Scrap三条记录的全部1,345字段同时与早先官方单条DBR导出一致。JSON会把浮点零序列化成整数记法，验收依field_types恢复类型再作官方CSV比较；库中仍为float32解码后的值。
- 英文归档11个成员、任务归档247个成员的完整索引、解压字节长度、SHA256及Adler32全部与Python一致；技能文本、物品文本和任务样本还与既有官方导出SHA256一致。
- 最终编译库的15项合成边界测试通过：零值/数组/类型、含非BMP汉字的UTF-8字符串、原始与混合LZ4分块、未知版本/字段类型、字符串索引、截断、超限、校验和与生产路径白名单。见`boundary-tests.json`。
- 输入ARZ/ARC的前后SHA256一致。校验仅读取现有官方样本，没有再次运行ArchiveTool或复制全量原资源。

复跑（Python3.11+，本机3.14.5），需给报告新文件名和测试新run-id；脚本拒绝覆盖既有文件：

```powershell
cd tools/resource_reader
python verify_compiled_containers.py --output ../../docs/compiled-containers/acceptance-local-new.json
python test_compiled_containers.py --run-id local-new
```

差分验收还依赖此前私有目录`.tooling/resource-reader-validation`中的少量官方样本；缺失时不能宣称官方交叉验证成功。纯库与合成测试不需要原版资源副本。

## 明确范围

仅本机ARZ marker2/version3和ARC version3。单次索引表或解压对象上限64MiB；数量上限及范围检查对损坏输入更严格，不声称复现原程序未定义行为。未知字段类型明确报错。保留原记录数值，没有修改数据库。

尚未恢复DLC叠加与字符串合并语义、ARZ尾部摘要全部含义、地图/模型等内部格式、异步资源调度、任务执行器或原存档格式。Windows虚拟化环境不足不会阻止这些离线模块；原版动态采样和以原版作参照的场景用例仍须满足独立隔离门槛；自写场景不以它们为启动前提。细化提案见`../resource-contracts/ISOLATION-PROPOSAL.md`。
