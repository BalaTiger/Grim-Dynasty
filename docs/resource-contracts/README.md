# 资源加载与所有权契约：静态恢复

> 验证对象澄清（2026-10-18）：自写模块的编译、运行和离线测试不依赖原版DLL、Steam、BIOS或Sandbox；这些隔离/授权门槛仅约束合法原版的动态行为对照，不阻塞自写实现。最终目标是独立的行为等价可编译代码，当前尚非完整游戏。详见工程 docs/VALIDATION-TRACKS.md。

本阶段以恢复可编译行为等价实现为目标。完成20个原生PE函数范围的串行分析：首批9、所有权补充7、数据库补充4；每个采用完整x64 RuntimeFunction范围，最大2103字节，不以几百字节截断大函数。堆512MiB、单CPU、无全程序自动分析。全部反编译完成；被调函数、异常清理funclet和推断C++类型并非因此全部恢复。另有Rea/Ghidra完整Quest2::Load结果，见 `../quest-structure/rea-quest-load/Quest2-Load-full.json`。

## 已观察调用链

| 范围/地址 | 观察与实现约束 |
|---|---|
| Engine::LoadDatabase，0x180074280 | 创建MemoryMappedFile；MapViewOfFile的desiredAccess=4（FILE_MAP_READ）；调用0x180280000导入，然后UnmapViewOfFile，再关闭映射/文件句柄。输入映射的指针不得保存为长期有效指针 |
| MemoryMappedFile::Open，0x18014f2c0 | CreateFileA以GENERIC_READ、FILE_SHARE_READ、OPEN_EXISTING；CreateFileMappingA保护=2（PAGE_READONLY）；记录文件长度低32位。析构0x18014f270关闭其持有句柄 |
| 数据库导入0x180280000 | 区分第一次载入与追加载入，检查头部条件，依次调用载荷、字符串、记录表、摘要处理；追加路径有字符串ID映射及记录替换分支。不能仅按文件加载顺序就推导所有DLC覆盖语义 |
| 载荷0x180280160 | 调用内部存储分配/获取函数0x18027ff20；在内部模式标志、输入及范围条件满足时，复制源offset24至记录表开始前的载荷区，并登记段偏移。未恢复所有存储模式，不声称任何模式都可脱离映射运行 |
| 字符串0x180280960 | 构建临时读者，首载交给字符串库0x180283a80、追加走0x1802805b0，再执行0x180280ab0；临时分配由同模块释放。字符串库细节仍为后续目标 |
| 记录表0x180280220 | 每条分配0x40字节元数据，复制类名字串与数值字段；载荷偏移按段重新基址；追加时字符串ID映射；查找已有项时释放/替换其记录对象。这是元数据复制证据，不是跨模块ABI承诺 |
| 摘要0x1802809f0 | 按头部位与首载/追加状态读取尾16字节或累计Adler32片段。当前Python读者未声称验证ARZ尾部全部摘要语义；ARC成员Adler32另有独立验证 |
| Archive::OpenRead，0x180038e40 | 空路径失败；CreateFileA只读；保存句柄、模式、文件名；读取并验证归档头后建立查询索引。头部函数0x1800397b0实际要求一次读到0x800字节且version=3；自写格式测试的微型合成ARC并非官方工具兼容性承诺 |
| Archive索引0x180039be0 | 分配哈希桶，按成员名计算桶并串接成员；这属于索引，不改变载荷格式 |
| Archive::Close，0x180038f40 | 关闭句柄；布尔参数和内部模式决定保留部分数据或释放内部块。不能把参数简单命名为“彻底销毁”后假定所有路径相同 |
| Quest2::Load，0x1804206d0 | 输入BinaryReader，读取QST2/version、UID、flags、任务；分配200字节任务对象；维护任务列表和UID索引；本地化依赖LocalizationManager单例。不是无依赖纯函数 |
| Quest2Task::Read，0x1804256d0 | 读取UID/flags及三组节点；按当前难度放入目标集合，并为其他难度克隆条件；单字节读取isBlocker与dontPropagate。格式解析器不模拟这些运行时副本 |
| Quest2析构，0x180420280 | 销毁本地化及各task，再释放容器存储；task析构0x180425440遍历三套目标及accept/complete事件，调用各对象虚析构并清理容器 |

## 恢复实现所采用的边界

自写接口接收有界不可变字节，不接受原C++对象地址；格式解码返回自有数据结构。内存/句柄由创建者关闭，解压输出与索引元数据使用本模块所有权。恢复代码应自行管理生命周期，不把本机MSVC std::string、std::vector或原版私有分配器作为公开ABI。

原生私有函数的伪代码参数类型有推断成分，代码地址仅适用于本机已哈希版本。存在内部模式、异常路径、全局单例、CRT分配器与线程副作用，不能据此直接调用Initialize或从新宿主构造原GameEngine。

`Engine::LoadDatabase`包装层的成功返回也不能当作完整数据有效性证明：可见反编译路径中导入函数返回值没有作为最终判定条件。自写读者对未知/损坏数据采取明确错误，当前等价目标限定已验证合法输入，不复制原程序潜在未定义行为。

## 可重新编译成果

`src/resource_codec/Lz4Block.cs` 提供 `GrimDynasty.Resources.Lz4Block.Decode(byte[],int)`，仅依赖.NET基础库，另有同源测试CLI。构建使用本机既有 `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`，无需安装SDK：

```powershell
& .\src\resource_codec\build_lz4.ps1
& .\.tooling\resource-reader-build\Lz4Probe.exe --self-test
```

自测8项；三个DBR与三个ARC压缩块共402061字节输出，与Python读者哈希一致，见 `compiled-lz4-acceptance.json`。DBR字段及ARC成员先前已与官方工具交叉核对。新EXE是自写纯格式测试程序，不加载或运行原DLL；不等于独立游戏场景验收完成。

## 仍缺的行为契约

数据库存储模式与完整DLC字符串合并/覆盖，资源异步加载与线程锁，错误恢复、任务运行时求值与Lua实现，设备/窗口初始化及其逆序销毁。原生动态对照需满足 `ISOLATION-GATE.md`；当前Sandbox/Hyper-V与固件虚拟化均未启用，不能开展原版动态对照；不阻塞自写场景离线开发，也不宣称原版对照已完成。
