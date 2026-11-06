WorldCamera 包装层与目标生产检查点

已取得真实 Rea procedure_pseudo_code / procedure_assembly / procedure_callees：Engine2290E0，固定Engine哈希4c2ea14c…，8/8生命周期调用成功。汇编确认先从虚槽+18返回对象捕获Q0，再调用Camera044A10，按XY→Z→Q0写入原隐藏输出，D14不写。反编译的简化参数类型不能代替汇编ABI。

自有实现：WorldCameraImageContracts及EXE2176C..217B6归一化切片，复用同一相机身份与具体已证GetTarget实现。只减视口原点、除有符号宽高，不clamp或补默认值。CameraWorldRay仅6增1删，增加原点字段并提取getter核验。

实际验证：2次产品编译，11个新增场景＋1个受影响GetRay回归，共73断言通过；编译日志无警告。使用Mode1自有夹具，没有原版动态对照、系统数学调用、旧整套复跑或游戏验收。原Mode0旧套件本轮未重跑。产物位于.tooling/r1-worldcamera-imagepoint-validation。

已有5853B运行时采样现在全部有候选指令解码，新增67条/262B；未知callee正常返回仍是条件，不能称完整调用链。新增分支会改ObjectId/重复命令参数，不直接产生RSI目标。

本次仅172B磁盘元数据确认2DDC0的候选范围[2DDC0,2E1FD)，1085B；30953C磁盘位模式7F800000是正无穷。没有读取该正文，加载后边界/常量和命中语义未验证，不能默认通过。全文件SHA256流式读取另计，原EXE前后哈希相同。

仍缺地面目标生产、实际加载IAT/点击调用证据、控制器查找与独立场景/UI呈现。因此没有可交互游戏交付。既有GetRay CopyPairBits的半已知qword保真限制仍待处理；本轮新wrapper不走它，相关回归仅全已知状态，不声称该限制已修复。

释放核查：2026-11-04T18:37:52.3226833Z，65个已记录PID均不存在，原游戏/调试器列表空，Rea及构建Job空并已释放，无待决操作。原游戏本轮未启动，原存档/Steam设置不动；历史失败与UNKNOWN保留，无commit/push/deploy。

下一步按JSON中的有限计划继续；新原版窗口需先通知用户，不重新尝试失败断点。
