# 辽西驿堡：巡路哨阶段

运行本目录 `gd-scene.exe`。默认 `player` 槽：不存在则创建新角色，存在则读取。独立槽示例：`gd-scene.exe patroltrial "Patrol Recruit"`。

WASD／方向键移动，E 交互，F5 保存，F9 读取，Esc 保存退出。出驿堡东门可见巡路哨；它会巡路、靠近角色并返回岗点。巡路状态与角色一同保存。

已有两项物品交付任务与一次性奖励。战斗、买卖、装备尚未开放；巡路哨不是已恢复完整数值的敌人。所有档案在本目录 `runtime-data/saves`，与原游戏存档无关。

本版通过无窗口测试（42 巡路、16 整合、22 场景回归），另有 129 项条件生命值模型断言；未自动启动窗口。`validation/patrol/patrol-scene.png` 为无窗口渲染预览。完整边界见工程 `docs/M3-PATROL-CHECKPOINT.md`。
