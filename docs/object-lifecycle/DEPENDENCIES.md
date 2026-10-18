# 对象创建与保存接口依赖清单

本清单先于本阶段实现建立；仅列静态入口和已知覆盖，不代表物品生成已恢复。当前可用库存仅接收明确已有对象；原版程序、存档和系统均不运行/改写。

| 层次 | 静态入口 | 本阶段开始时状态与依赖 |
|---|---|---|
| Replica描述到物品 | Game Item::CreateItem 0x180324120 | 待核实：记录加载、动态类型、构造、复制信息与登记 |
| GiveItem生成 | Game 0x18046BDE0 | 待核实：工厂、种子、缓存；不将已有对象复用称为生成 |
| GiveItem激活 | Game 0x18046BAA0 | 待核实：前后钩子、生成和info缓存生命周期 |
| 角色内创建 | Game 0x18012B610 | 待核实：记录到对象、原控制器和已有库存接口衔接 |
| 对象ID | Engine ObjectManager::CreateObjectID 0x18017E4B0 | 待核实：计数、保留/删除ID、锁与边界 |
| 部分创建 | Engine 0x18017E170 | 待核实：工厂和加载，不能用纯ID冒充对象 |
| 完成部分创建 | Engine 0x18017E230 | 待核实：赋ID、注册表、初始化/通知 |
| 完整文件创建 | Engine 0x18017DEA0 | 待核实；未运行，不读取任何存档 |
| 已有对象加入库存 | Game 0x18031EA80 | 已恢复明确base Item配置；先事件16后追加，空hook0，类型0不走蜕变保存 |
| Player GiveItem | Game 0x1803D3FC0 | 未恢复完整姿态、装备、控制器、容量、音效和失败掉落路径 |
| 对象销毁/注销 | ObjectManager::DestroyObjectEx及调用者 | 未恢复；当前整堆仅destroy=false移出库存并保留有效对象 |

## 保存边界（只列代码依赖，不访问保存文件）

- Player::TakeItemFromStashes 0x1803E4B50的主角路径会调用SaveTransferStash、SaveReagents；完整仓库交付仍Unsupported。
- Inventory::AddItemToInventory中兼容类型可能登记蜕变并SaveTransmutes；已恢复base Item类型getter为0，静态证明不进入此分支。派生类型不据此放宽。
- 生成、联网CreateItem和完整文件创建可能依赖更多序列化/资源/世界接口，尚未核实，不以记录“保存请求”代替未恢复保存语义。

本阶段将根据实际Rea结果追加证据与可实施小分支；未知继续Unsupported，不触碰真实或云存档，不绕过保护。
