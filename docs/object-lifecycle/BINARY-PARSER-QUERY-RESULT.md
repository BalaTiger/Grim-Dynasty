# Parser 固定查询已完成 — 2026-10-20

`Engine 0x18013F510` 的固定批次最终 **8/8 calls 通过**。开始于05:33:10.936 UTC，结束于05:34:39.463 UTC，耗时88.527秒。取得伪代码、汇编和直接callee列表；正常MCP close、最终closed session、Rea退出0与采样器正常退出均确认，无强制终止。见[完整摘要](rea-binary-parser-entry-retry-2/summary.json)。

## 本轮交付

- [parser伪代码](rea-binary-parser-entry-retry-2/04-Binary-parser-entry-0x18013f510-pseudocode.json)
- [parser汇编](rea-binary-parser-entry-retry-2/05-Binary-parser-entry-0x18013f510-assembly.json)
- [直接callee](rea-binary-parser-entry-retry-2/06-Binary-parser-entry-0x18013f510-direct-callees.json)
- [守卫修复与验证](GUARD-REPAIR-VALIDATION.md)：76项纯Node测试、115条最终真实Windows Job断言，以及未释放Rea导入门控的真实联测通过。
- [最终资源释放核验](rea-binary-parser-entry-retry-2/resource-release.json)：68个已记录进程身份全部退出，无活动Java/decompile，runtime文件数0；本次正常close已自行清理，无需额外删除目录。

固定计划SHA256：`adf41b0f3ec4d59f11352b21eb3173c8cb077a638a452c0f21f488b6b21251ff`。只执行预先限定的8次调用，没有追查新callee。

## 资源与原版

目标：`D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll`。前后SHA256均为`4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153`。

55个归属样本确认主Java实际`-Xmx384M`。最低physical available为12.024GiB，最低commit headroom为23.685GiB。后端树private扫描峰值0.908GiB，含控制器/采样器private峰值1.118GiB，working-set扫描峰值1.201GiB。不同进程峰值不可相加当作同时峰值，离散扫描可能遗漏短命进程峰值；这些数字不是未来任务上界。

专用Job直接核验进程归属和创建身份，辅助Java按固定安装路径和精确参数分类；不以名称/路径推断归属。主分析堆384M、启动双余量4.25GiB、运行双余量2GiB、并行重负载和失联保护均保留。正常结束确认Job为空，根进程先退出本身不能证明清理完成。

## 失败历史与修复范围

1. [原始失败](BINARY-PARSER-ATTEMPT-RESULT.md)发4次、完成3次：短命父链无法给Java归属，守卫保守停止。没有parser正文。
2. [第一次重试](rea-binary-parser-entry-retry-1/summary.json)同样发4次、完成3次：归属和384M实测已经通过，随后进程退出与`QueryFullProcessImageName`竞态使采样器停止。其[独立释放核验](rea-binary-parser-entry-retry-1/resource-release.json)确认26个身份全部退出、分析进程0、runtime文件0，不改写原失败摘要。
3. 最小native修复只允许在**同一个已打开句柄确认退出**时记录缺失image及错误码；存活或无法确认仍失败。115断言中实际观察到了error31、`HasExited=true`和image不可读分支。第二次重试即本次成功批次。

本轮外部后端执行及自测均按工具逐次审批实际执行，没有执行策略绕过或持久权限改动。没有审批拒绝待处理，没有仍运行或状态不明的本批分析进程。

## 下一检查点

parser正文现已取得；尚未完成逐指令协议审阅及独立行为实现。伪代码显示函数为`LoadTableBinary::Deserialize(BinaryReader&)`，包含两遍读取、tag 0–3路径、类型缓冲与字段元数据写入；这些是待与保存汇编逐项核对的初步观察，不能当作已实现的文件格式规范。

最低待闭合helper为`0x180030D20`（三个4字节向量resize）、`0x1800C69F0`（位存储resize）和`0x180284080`（字段索引初始化）。tag2-only输入也无条件调用位存储helper，不能以增量零为由绕过。另有已研究的`0x180284160`及分配/复制相关入口，尚未自动扩查。下一步应先复用现有汇编和历史helper证据，确定记录字节→字段状态→真实Class getter的最小正常域；未知初始化、截断/异常及发布副作用仍Unsupported。`LoadFile` miss、基类`13C1D0`、发布`05DEB0/02E770`与完整物品工厂仍未闭合。

按父会话要求，本轮到此交回可读检查点。行为源码未变，既有930断言/14组合场景是历史验收，本轮未重新构建行为模块。未启动原游戏或执行Engine.dll，未读取玩家存档/Steam云数据，未安装额外工具，未commit/push。
