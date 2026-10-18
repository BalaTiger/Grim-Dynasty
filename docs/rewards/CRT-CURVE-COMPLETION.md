# CRT 曲线与经验倍率：完成审计

已完成的统一构建通过695条固定断言，生成10个自有DLL模块和9个独立场景。本次新增56条CRT/表达式断言及15条经验倍率断言。验收归档时只核对文件和哈希，没有重复启动分析或测试。见 [验收数据](acceptance-crt.json)。

## 已实现与静态依据

`src/rewards/SystemCrtMath.cs`调用现有System32 UCRT的float powf和私有C locale的_atof_l；原安装目录内任何DLL均未加载。Game.dll静态导入证明powf经0x1805dc404跳板到IAT 0x18060b900，解析器先调用atof再CVTSD2SS。运算符恢复单精度运算、常量折叠、左结合幂、优先级与无符号playerLevel读取；2^3^2为64，2^-2按原栈规则拒绝，2^(-2)可接受。小数先转double再转float；没有用通用double Math.Pow替代float powf。

ReceiveExperience静态指令恢复percent*0.01f、amount转float、乘法、加0.5f、向64位整数截断，再加低32位。零倍率直接保留值；NaN的UCOMISS/JZ路径跳过倍率；非有限/越界转换保留Unsupported。75加50%得到113；75减100%得到1；无符号溢出按低32位保留。倍率只在统一入口应用一次。

## 精度与场景结果

10万组固定种子诊断中，powf与(float)Math.Pow出现7处差异，最大1ULP。十进制中点与二次舍入有专门断言。独立原创场景使用playerLevel^1.5*60.25+0.5和50%倍率：75奖励变113，角色从1级升2级，最终经验163、技能点3、属性点1、字幕入队、奖励UID1；浮点控制字前后均524319。

只读提取本体/GDX1/GDX2的playerlevels记录，逐级比较自有通用解析器与另写固定结构参考，共288项float位模式和取整阈值全部一致，并通过3个使用官方数值参数的自有升级场景。全部改用double会在本体产生42个、GDX1/GDX2各57个不同整数阈值。GDX3该已知路径记录不存在，未推断覆盖关系或替代记录。这是独立系统库参考验证，不是运行原版解析器所得的动态真值。

## 依赖与证据范围

仅启用当前Windows x64、显式C数字locale、round-to-nearest、异常屏蔽和保留非正规数的已检查配置。系统库SHA256为3c60056371f82e4744185b6f2fa0c69042b1e78804685944132974dd13f3b6d9。私有locale释放，控制字只查询；没有setlocale、改变浮点控制、安装软件或修改系统设置。未验证原进程运行时locale，未知/非C配置不承诺兼容；errno和浮点状态标志不作为游戏逻辑状态模拟。

Rea 6.5.0本轮离线启动，3次实际查询成功并已关闭：见rea-crt-bridge内Parser-atof-conversion-assembly、powf-import-thunk、Operator-factory及close_binary记录。补充证据见../task-kills/ghidra-selected/equation-precedence.json与equation-pow-constant-fold.json。Rea与直接Ghidra证据分开保留。

验收归档重新核对四个ARZ、System32 UCRT及全部打包文件哈希。未执行原版EXE/DLL，未访问真实/云存档，未修改安装或Steam设置，未绕过保护，未commit、推送或发布。修改前备份保留；只打包原创代码与必要元数据，没有复制原游戏资源。

## 剩余边界

绘制字幕、音效/粒子、非空宠物/星座/事件订阅者、服务回调、十级教程、真实存档及网络整合仍不在已验证子集中。当前成果是受边界约束的离线子系统，不构成可替换完整原引擎或已完成明末Mod的声明。官方Mod工具与原创资源路线仍按第一阶段规划推进，二十八星宿未写入主游戏。

## 官方技术来源

- [UCRT部署：Windows10/11采用系统UCRT](https://learn.microsoft.com/en-us/cpp/windows/universal-crt-deployment?view=msvc-170)
- [pow/powf签名](https://learn.microsoft.com/en-us/cpp/c-runtime-library/reference/pow-powf-powl?view=msvc-170)
- [atof与_atof_l](https://learn.microsoft.com/en-us/cpp/c-runtime-library/reference/atof-atof-l-wtof-wtof-l?view=msvc-170)
- [创建私有locale对象](https://learn.microsoft.com/en-us/cpp/c-runtime-library/reference/create-locale-wcreate-locale?view=msvc-170)
- [控制字查询与mask](https://learn.microsoft.com/en-us/cpp/c-runtime-library/reference/controlfp-s?view=msvc-170)
