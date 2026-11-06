# 宿主调用契约（纯静态推导，未执行）

本文汇总 `Engine.dll` 启动引导的全部导出入口与调用顺序，供编写宿主程序时直接照用。**所有内容来自纯静态分析（dumpbin 读函数体 + 导出表 + .pdata 边界），未启动或附加任何游戏进程。**

## 一、可用入口（均为 Engine.dll 导出，修饰名可精确定位）

| 入口 | RVA | 备注 |
|---|---|---|
| `WinWindow::WinWindow()` | `0x217710` | 只写 vtable 与字段，不建 HWND |
| `WinWindow::Initialize(string const&, int,int,int,int, void const*, Style, HWND__*)` | `0x2179b0` | 建真实窗口；返回 bool |
| `GraphicsEngineSettings::GraphicsEngineSettings()` | `0x906e0` | 默认写入插件名 `"direct3d11"`（长度 10） |
| `Engine::Engine()` | `0x72930` | 构造，仅写至 `+0x198` |
| **`Engine::Initialize(EWindow*, settings const&, bool, FileSystem*, string const&, bool, string,string,string,string, bool)`** | **`0x743d0`** | **一步完成设备创建与资源初始化** |
| `Engine::~Engine()` | `0x73050` | 析构 |
| `GraphicsEngine::CreateDevice(EWindow*, settings const&)` | `0x90ba0` | 由 `Initialize` 内部调用，宿主无需单独调 |
| `GraphicsEngine::Initialize(EWindow*, settings const&, ResourceLoader*)` | `0x90e60` | 由 `Initialize` 内部调用，宿主无需单独调 |

## 二、对象尺寸（必须按此分配缓冲）

| 对象 | 尺寸 | 依据 |
|---|---|---|
| `Engine` | **≥ `0xC28`** | `Initialize` 触及 `+0xC27`；构造只写 `+0x198` |
| `GraphicsEngine` | **`0x1C0`** | `mov ecx,0x1C0` 后立即 `operator new` |
| `WinWindow` | **≥ `0x48`** | 构造函数与 `Initialize` 写入的字段范围 |

**建议**：`Engine` 分配 `0x1000`，`WinWindow` 分配 `0x200`，留余量。

## 三、`Engine::Initialize` 参数映射（已逐项对应）

| 位置 | 类型 | 去向 |
|---|---|---|
| `rcx` | `this` | 保存在 `rsi` |
| `rdx` | `EWindow*` | 保存在 `r15`，透传给 `CreateDevice` 与 `GraphicsEngine::Initialize` |
| `r8` | `GraphicsEngineSettings const*` | 保存在 `r12`，同样透传 |
| `r9b` | `bool` | 写入 `[this+0x292]` |
| `stack+0xA0` | `FileSystem*` | 写入 `[this+0x358]` |
| `stack+0xB0` | `bool` | `[this+0xC21]` |
| `stack+0xB8` | `bool` | `[this+0xC22]` |
| `stack+0xC8` | `bool` | `[this+0xC25]` |
| `stack+0xD0` | `bool` | `[this+0xC26]` |
| `stack+0xD8` | `bool` | `[this+0xC27]` |

## 四、内部调用顺序（**与直觉相反，已验证**）

```
0x18007476b  GraphicsEngine::GraphicsEngine()        构造（0x1C0 字节）
0x180074781  GraphicsEngine::CreateDevice(...)       ← 先创建设备
0x180074aca  GraphicsEngine::Initialize(...)         ← 后初始化资源
```

- `GraphicsEngine` 指针存于 **`Engine+0x1F8`**
- `CreateDevice` 失败时：`test al,al; jne 成功`，失败走 `0x143900`（取日志对象）+ `0x146040`（格式化错误）
- `ResourceLoader` **由 `Initialize` 内部创建**并存 `[Engine+0x210]`（`0x1800748f5`），宿主**无需提供**

## 五、`FileSystem*` 能否传 `nullptr`

**在本步骤内可以**：`[this+0x358]` 在 3049 字节的 `Initialize` 体内**只被写一次（`0x180074443`），之后从不读取或解引用**。

**但**：全模块另有 46 处 `[reg+0x358]` 读取 —— 需注意**偏移相同不等于同一字段**（这些多半属于别的类）。是否被后续子系统解引用**尚未建立**，因此传 `nullptr` 的安全性**仅限本函数**。

## 六、必须如实标注的边界

**宿主需要自己把 Engine 对象指针写入 `[Engine.dll+0x421428]`。**

理由：该全局的发布者不在导出 API 内（`Engine::Engine()` 完全不引用它；`Engine::Initialize` 对其 3 处引用**全是读**）。这是游戏自身所做操作的**等价形式**，但**执行者是宿主**——不是我们恢复了游戏的发布步骤。

因此任何基于此宿主的成果，都必须表述为：
> 用原版代码构造并初始化引擎对象、由宿主发布引擎全局

**不得**表述为"已恢复游戏的引擎启动发布流程"。

## 七、尚未建立

- 四个 `string` 与五个 `bool` 的语义
- `GraphicsEngine::Initialize` 内 6 轮循环是否依赖磁盘资源
- 后续子系统是否解引用 `FileSystem`
- `Engine::Initialize` 是否要求某些子系统提前存在

## 八、证据文件

- `docs/reverse-client/r1-startup-api-static-20261106.json`
- `docs/reverse-client/r1-bootstrap-sequence-20261106.json`
- `docs/reverse-client/r1-bootstrap-order-20261106.json`
- 函数体：`docs/reverse-client/r1-engine-window-run19/inspection.json`（`Engine::Initialize` 六段）
- `docs/reverse-client/r1-engine-window-run20/inspection.json`（`GraphicsEngine::Initialize`）

## 九、补充（第二轮）：参数类的导出与可用性

两个参数类都**有完整导出**，宿主可自行构造，不必只能传 null：

| 类 | 构造 | 析构 | 备注 |
|---|---|---|---|
| `FileSystem` | `0x86370`（无参） | `0x863C0` | 另有 `OpenFile 0x86CE0`、`OpenFileWithCheck 0x86E60`、`OpenFullFilePath 0x86FE0`、`OpenFileStream 0x87450` |
| `ResourceLoader` | `0x199E60`（**带 int**） | `0x19A0A0` | 对象 **`0xE0` 字节**；另有 `CreateMarker 0x19B240` |

### `ResourceLoader` 的 int 参数含义（已解出）

`Engine::Initialize` 在 `0x1800748EF` 处构造它：

```
mov ebx,[settings+0x0C]     ; 基准计数
lea eax,[rdi-1]             ; rdi = 第 4 个参数（bool）
cmp eax,ecx; cmovg ecx,eax  ; 下限 1
cmp ecx,ebx; cmovl ebx,ecx  ; 上限为 settings[+0x0C]
mov edx,ebx                 ; ← 传入的 int
call ResourceLoader::ResourceLoader(int)
mov [rsi+0x210], rax
call 0x19A830               ; 随后遍历其内部数组（(end-start)/8）
```

**即"工作线程数"**，由 `settings+0x0C` 与第 4 个 bool 共同钳制（下限 1）。

### 注意：`settings+0x0C` 未被默认构造初始化

`GraphicsEngineSettings::GraphicsEngineSettings()` 只写 `+0x00`（string）、`+0x10`、`+0x18`、`+0x20`、`+0x28`、`+0x2C`、`+0x2F`、`+0x30`，**不写 `+0x0C`**。宿主若直接使用默认构造的 settings，`+0x0C` 是未定义内容，会影响上面那个线程数——**建议宿主显式把它设为合理值（例如 1～4）**。

## 十、宿主可行性评估

| 条件 | 状态 |
|---|---|
| 窗口可建 | ✅ `WinWindow` 构造 + `Initialize` 已验证可执行（此前实测创建出真实 HWND） |
| settings 可建 | ✅ 默认构造可用，但 `+0x0C` 需宿主显式设置 |
| Engine 对象可分配 | ✅ 尺寸已知（≥`0xC28`） |
| `FileSystem` 可处理 | ✅ 可传 null（本函数不解引用）或自行构造真实实例 |
| `ResourceLoader` | ✅ 由 `Initialize` 内部创建，宿主无需干预 |
| 设备创建 | ✅ 由 `Initialize` 内部完成 |
| 引擎全局发布 | ⚠️ **须由宿主自行写入**（边界） |

**结论**：静态层面已具备编写宿主的全部信息；剩下的风险点是 `Initialize` 内部那些**未解语义的 string/bool 参数**，以及 `GraphicsEngine::Initialize` 的 6 轮循环是否依赖磁盘资源。

## 十一、重要修正（第四轮）：发布引擎全局是**前置条件**，不是事后动作

`Engine::Initialize` 的正文里**自己会读** `[Engine.dll+0x421428]`：

```
0x18007471f  mov rcx,[0x180421428]   ; ← 读引擎全局
0x180074726  call 0x180073660        ; 由该全局取一个对象
0x180074731  lea rdx,[rbp-20h]
0x180074735  call 0x180182CE0        ; Options::GetString(...)
0x18007473b  call 0x180143900
0x180074754  call qword ptr [r8]     ; 该字符串在 GraphicsEngine 分配前就被消费
```

**因此宿主必须在调用 `Initialize` **之前**把 Engine 对象指针写入 `0x421428`**，否则会在 `0x18007471f` 解引用空指针。

**这也反过来说明**：游戏自己不可能靠 `Initialize` 发布该全局——它必然是**先发布、后调用**，与工厂/访问器模式一致。

### 修正后的宿主调用顺序

1. 加载 `Engine.dll`，解析导出
2. `WinWindow::WinWindow()` + `WinWindow::Initialize(...)` → 真实 `EWindow`
3. `GraphicsEngineSettings::GraphicsEngineSettings()`（记得显式设置 `+0x0C`）
4. 分配 `Engine` 缓冲（≥`0xC28`）→ `Engine::Engine()`
5. **把 Engine 指针写入 `Engine.dll+0x421428`**（发布步骤，必须在此之前完成）
6. `Engine::Initialize(window, settings, bool, FileSystem*, string, bool, string, string, string, string)`

## 十二、四个 string 参数的用途（部分解出）

| 调用 | 符号 | 说明 |
|---|---|---|
| `0x1b30c0` | `SoundManager::SoundManager(ResourceLoader*)` | 构造，存 `Engine+0x58` |
| `0x1b3710` | **`SoundManager::Initialize(EWindow*, std::string const&)`** | **消费第 1 个 string**（`[rbp+0xC0]`） |
| `0x182ce0` | **`Options::GetString(StringName)`** | 引擎会**从 Options 对象读字符串** |

即：这些 string **不是自由标签**，各自被特定子系统初始化器消费；且至少有一条路径要**从 `Options` 对象读配置字符串**。

**尚未解出**：另外 3 个 string 的消费者、`SoundManager::Initialize` 能否容忍空串、`Options` 对象是否必须先存在。

## 十三、第五轮：两个剩余风险点均消解

### `Options` 不需要宿主预先创建

`0x73660` 就是 **`Engine::GetOptions()`**，惰性访问器：

```
cmp qword ptr [rcx+0x3E0], 0     ; Engine+0x3E0 缓存
jne 直接返回
operator new(0x150)              ; Options 336 字节
call Options::Options()          ; 0x1808D0
mov [this+0x3E0], rax
lea rdx,[this+0xB8]
call Options::LoadDefaults(...)  ; 0x182140
lea rdx,[this+0xD8]
call 0x1821B0
```

**它只依赖 `Engine+0xB8` 与 `+0xD8`**，而这两个字段由 `Engine::Engine()` 构造写入（构造在 `0x729C0` 处对 `+0xB8` 做 `lea`，是 `std::string` 构造）。

**因此"先发布、后 Initialize"是安全的**：`Initialize` 早期只触碰构造函数已初始化的前段字段，其余子系统按需创建。

### `LoadDefaults` 语义上不需要外部配置

函数名即"载入默认值"；其调用列表中没有直接出现文件 I/O 导入（`CreateFileA`/`ReadFile`/`fopen` 等在更下层）。

## 十四、最终宿主序列（定稿）

```
1. 加载 Engine.dll，解析导出
2. WinWindow::WinWindow()  +  WinWindow::Initialize(...)      → 真实 EWindow
3. GraphicsEngineSettings::GraphicsEngineSettings()            → 记得显式设 +0x0C
4. 分配 ≥0xC28 字节  +  Engine::Engine()
5. 把 Engine 指针写入 Engine.dll+0x421428                      ← 必须在第 6 步之前
6. Engine::Initialize(window, settings, bool, FileSystem*, string, bool, string, string, string, string)
7. 成功后设备已存在；每帧 BeginFrame → PresentSurface
```

## 十五、边界声明（必须随成果一并陈述）

第 5 步**由宿主执行**。它复现了游戏所做的操作，但**不是**恢复了游戏自身的发布代码——静态搜索已证明三个模块中不存在对该槽的存储指令。

任何基于此宿主的成果，表述应为：
> 用原版代码构造并初始化引擎对象，由宿主发布引擎全局

**不得**表述为"已恢复游戏的引擎启动发布流程"。

## 十六、静态阶段结论

入口、对象尺寸、参数映射、调用顺序、文件依赖**全部从静态读取建立**；两个曾经的阻塞点（无休止追发布者、担心的磁盘依赖）**均已消解为对宿主无影响**。

剩余不确定项（末三个 string 语义、`LoadDefaults` 无配置时是否成功、`SoundManager::Initialize` 能否容忍假串、设备是否需要插件内嵌之外的着色器数据）**只能靠实跑验证**，超出本目标的"纯静态"约束。
