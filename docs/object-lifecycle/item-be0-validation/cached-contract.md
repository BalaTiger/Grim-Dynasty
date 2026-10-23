# Item BE0：已保存证据的复用合同

本记录由工程内归档及源码静读整理；本审阅代理未读取原版、运行脚本、构建或测试，未修改生产源码。主代理随后审阅并单次执行 `tools/static_analysis/resolve_item_be0_import.mjs`，生成了成功的原始导入/导出绑定及调用方报告；本记录已只读核对这些新保存结果。

## 调用方与已确认绑定

`docs/rewards/rea-inventory-add/Item-constructor-primary-table-assembly.json` 的 `result.structuredContent.normalized_result` 保存：

| Game RVA | 已存指令 | 含义 |
| --- | --- | --- |
| 3238BE | OR RSI,-1 | RSI 全位为 1 |
| 3238C2 | MOV DWORD [RDI+BD8],ESI | Item+BD8 写 FFFFFFFF，只有 4 字节 |
| 3238C8 | LEA RBX,[RDI+BE0] | 保存子对象 this |
| 3238CF | MOV QWORD [RSP+58],RBX | 栈上保存该 this，不是 Item 字段 |
| 3238D4 | LEA EDX,[RSI+3] | EDX=2，32 位结果 |
| 3238D7 | MOV RCX,RBX | 参数 this=原 Item+BE0 |
| 3238DA | CALL QWORD [180609980] | 已确认的 Engine HookPack 构造导入调用 |

`docs/phase-1/boundary-audit/module-boundaries.json` 的 `$[2].importModules[3]` 为 `Engine.dll`，`symbols[304]` 精确名称为 `??0HookPack@GAME@@QEAA@I@Z`。它是构造函数的名称证据，不单独证明 EAT 地址。

`offline-questitem-imports/import-bindings.json` 的 `normal_imports[3].raw_metadata` 已保存同一 Game 导入表的原始记录：

- descriptor RVA A4297C/file offset 10751868，raw `c02aa4000000000000000000a66aa50000906000`：OFT=A42AC0、DLL 名称=A56AA6、FT=609000。
- `ownership_lookup_prefix` 保存 332 项；第 304 项 raw=`d896a40000000000`，指向 hint/name RVA A496D8。
- `609000+304*8=609980`；DLL 原始名称为 `456e67696e652e646c6c00`（Engine.dll）。
- 该旧报告没有针对 609980 的独立 `normal_imports` 记录，不能借其他导入的 hint。新增 reader 只补此 IAT、此 hint/name 和同 hint 的 Engine 精确导出；不匹配就停，不搜索替代名称或读取新 Engine 正文。

新报告 `offline-item-be0-import/inspection.json` 的状态为 `fixed-item-be0-caller-import-and-cached-engine-body-bound`，无 failures。Game 原始 hint=213、名称精确匹配；Engine 名称表 index=213/name RVA=3C1008、ordinal index=213/ordinal=214，EAT target=0EBDE0。至此 Game 609980 到旧 EBDE0 正文的身份链已经闭合，不再只是名称相似推断。

新 reader 同时核对 Game `[3238BE,32396E)` 的 176B/31 条指令，包含下一次调用 323968，但不跟随该调用。新增元数据 74B；Game/Engine 各一次前后 64KiB 流式全文件 hash，共四次身份哈希；均 stat 稳定、hash 保持、句柄关闭。仅调用一次 dumpbin 读取自有微型 PE，Engine 正文、`.pdata` 和原始 PE 头均未重读。新 `3238be-instructions.json` 的原始位宽与下述旧调用方合同完全一致。

## EBDE0 正常合同可直接复用

`offline-actor-198-constructor/inspection.json` 已成功核对 Engine `[0EBDE0,0EBEA5)`：197B、53 条指令、完整字节覆盖，原件前后 hash/stat 保持且句柄关闭。精确 `.pdata` 第 4025 项为 `e0bd0e00a5be0e0058b73700`，unwind RVA 37B758。这是旧 Actor 基类 `07D0DC -> EBDE0(this+198,4)` 直接调用的正文；新 Game 导出绑定已经确认为同一入口，正文无需再读。

该函数没有按“类型 2/4”选择不同算法的分支。EDX 直接保存为元素数量，先进行以下五次对象写入（S 为传入 this）：

| Engine RVA | 写入 |
| --- | --- |
| EBE06 | QWORD S+00 = preferred identity 180339B70 |
| EBE0B | DWORD S+08 = 0 |
| EBE0E | QWORD S+10 = 0 |
| EBE12 | DWORD S+18 = EDX |
| EBE15 | BYTE S+1C = 0 |

EDX=2 时 `0x80*2+8=0x108`，即 264B；固定值不会触发乘法/加法溢出饱和分支。正常后续顺序是：

1. EBE41 调用 2DC900，请求 264B。旧 allocator 证据所支持的正常返回是非空；EBDE0 自身的显式零返回分支不构成“operator new 可正常返回 null”的证明。
2. 已知正常非空分配后，EBE5B 调用 2DDB14，其已绑定 `memset`：清零整个 264B。
3. EBE60 将 QWORD cookie 写为 2，元素首址=分配 token+8。
4. EBE85 调用 2DCC3C：RCX=元素首址、EDX=80h、R8D=2、R9=EBEB0，栈第五参数 EBF00。正常循环先检查构造目标，再执行叶构造；只有叶构造返回后才推进元素地址和次数。正常循环没有调用第五参数，异常清理不能据此补全。
5. 每次 2DCC75 调用 2DD4C0 的加载后检查行为仍是边界。旧显式磁盘初值 profile 只依据保存的槽/LoadConfig/CE70 `RET imm16=0 / C2 0000`；这里的 0 是额外栈调整量，不是 EAX 返回值，不能推广为实际加载后的 guard 恒返回或可跳过。
6. EBEB0 正常叶正文是 70B/17 条指令、14 次写入；候选窗口余下 10B 为 CC。单独叶构造保留每个 128B 元素中未写的 34B；整段 memset 后这些字节才已知为零。
7. 两元素正常完成后，EBE8B 才写 QWORD S+10=分配 token+8，EBE8F 选择 RAX=原 S，EBEA4 RET。未知检查或叶未返回时不能提前发布此指针。

既有 `ActorConstructorArray.cs` 将数量固定为 4、分配大小固定为 520B，并连带执行 Actor 基类前后段。因此可复用其已证字节合同与测试经验，不能直接把旧整入口用于 Item+BE0，也不能把 264B 数组伪装成旧 520B 数组。Item 状态应沿用原共享存储，独立数组保持自身 token 和 264B 范围；不修改已有 Actor 区域。

## 正常返回后的最小连续段

EBDE0 保存并恢复 RSI/RBP/RBX。旧 Item 调用方因此继续使用 RSI=-1、RBP=0、RBX=Item+BE0。3238E0 仅装入常量地址，3238E7 才将子对象首 QWORD 覆写为 preferred identity 180770AD8。不能在 EBDE0 正常返回前提前写此最终值。

随后到下一调用之前，共有 17 次 Item 字段写入：BE0 QWORD 身份；C00 BYTE0；C02 BYTE0；C04 WORD0；C06 BYTE0；C08 DWORD FFFFFFFF；C10 字符串的 C28 QWORD capacity15、C20 QWORD length0、C10 首 BYTE0；C30/C38/C40/C48/C50/C58 六个 QWORD0；C60 DWORD0；C64 BYTE0。C10 capacity 刚被写为 15，因此旧 caller 的 `<16` 分支落到 inline 首字节，不读取旧堆指针。

末次已证写入是 32395E 的 C64 BYTE，覆盖终点 C65；这是最小已证写范围，不是完整 Item 大小。所有窄写邻域、BD8 后四字节、子对象 padding 以及未提及的字符串尾字节都保留原有值/未知状态。

323965 将 RCX=原 Item；323968 调用 `[180609128]` 是下一直接依赖停点。此处不跟随、不假装返回，不执行 32396E 的后续 `[Item+538]=EAX`。本段不恢复整个 Item 工厂、运行时 IAT、分配失败/EH 或加载后的 guard。

## 来源 SHA256

| 工程相对路径 | SHA256 |
| --- | --- |
| docs/rewards/rea-inventory-add/Item-constructor-primary-table-assembly.json | 7a078d11495b5b7c10a07d7b6309c9d633f8ec39dfc444e5f0decfbd5efe1a3f |
| docs/phase-1/boundary-audit/module-boundaries.json | d1ec79916baeb45b3257f7c7c2ce356c262f804ae9b2577888a0d14de53e7ba7 |
| docs/object-lifecycle/offline-questitem-imports/import-bindings.json | 94d695f5026e1f50a7c1a2968d6f091146db634e28618ef0565b671857aa6f5e |
| docs/object-lifecycle/offline-actor-198-constructor/inspection.json | 36958aecbb0e7905f3d002d68b0f77b48230271194935f73724a40cac11bdc54 |
| docs/object-lifecycle/offline-actor-198-constructor/0ebde0-instructions.json | 9083e6b9037ded4e5ee7224b5544ac2b9abae3b6a1ecca612e9d7663b8f484ce |
| docs/object-lifecycle/offline-actor-198-array-helpers/2dcc3c-instructions.json | a8c49acbfc80c793124df217df0aace5fb16786e4aa13ba660739d5751d794c3 |
| docs/object-lifecycle/offline-actor-198-leaf-prefixes/0ebeb0-instructions.json | cced24bf2aa60a7272eaea738d25c28337fc7bb23f332c563682d93cad146bb5 |
| docs/object-lifecycle/offline-actor-198-zero-import/metadata.json | adacf28328640ed5b4fb8ae42ed17f26f70763071cbd53e79c4067390dacc7dc |
| tools/static_analysis/resolve_item_be0_import.mjs | 73a5d9d2126b3802efb1e90db7251db7cd4d5a700e58ab5e0110f6348fef9b62 |
| docs/object-lifecycle/offline-item-be0-import/inspection.json | 1392ed5594df6499211ce2c0910abf7a5f8fba7501aa1b38361acfdfd21822e5 |
| docs/object-lifecycle/offline-item-be0-import/3238be-instructions.json | 62c166c9aa0b41f44d3f6628a48496de7eaf54d511a1fe6161932e3001ed118d |
| docs/object-lifecycle/offline-item-be0-import/3238be-caller.bin | ae2521516b7e7efc8480fb10efb0f19b7c40a4973f5e2764f587b958cba21a04 |

本记录的“可复用”结论以原 Engine SHA256 `4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153` 为前提；未声明真实运行时对象或原版兼容验证。
