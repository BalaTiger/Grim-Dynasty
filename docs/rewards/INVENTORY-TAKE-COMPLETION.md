# 自有库存实扣、任务事件与奖励闭环

物品数量不再只能在场景中手动改变。本阶段恢复Character::TakeItemFromCharacter按对象扣除的普通非遗物部分堆叠分支，并继续恢复库存按记录名选择对象的安全子集。新函数实际写自有库存的两个堆叠字段，发送17号事件，调用已恢复的非空HasItem回调，驱动物品目标及完成奖励。

## 实际场景

`InventoryDeliveryDemo.exe`读取内存生成的原创QST2条件（HasItem v1、剩余数量<=1）与75钱币完成动作，绑定已有物品目标运行时。初始主堆叠3、同步堆叠99、钱包50；旧的数量快照故意为999，以证明新getter读取实际存储。

场景按`MING\ITEMS\MILITARY_RATION.DBR`选择已有库存对象100，调用新扣除函数取走2份。主字段+0x88C和同步字段+0x6B0都写成1，getter读到1；17号事件送达一个真实注册的HasItem处理器，将dirty/text字节置1。随后Update满足目标，任务状态变3、钱包125、奖励UID[1]、16/17回调注销。对象与库存ID保留，符合部分堆叠路径。初始化之后没有手工写数量。

实际输出：[inventory-delivery-scene.json](inventory-delivery-scene.json)。最终统一构建807条断言、13个独立场景通过，仍为10个原创DLL；本轮新增25条库存与15条名称入口规则断言。原始构建日志在inventory-take-build-output.txt，部分中文有控制台旧代码页影响，故场景另直接捕获UTF-8原始字节；ASCII结果及构建完成行完整。

## 静态来源与函数范围

Rea 6.5.0离线既有缓存完成14次查询并关闭，原始来源在[rea-inventory-take](rea-inventory-take/)：

- Character::TakeItemFromCharacter(objectId, count, bool, bool, bool)，0x180061000：取得物品、读取堆叠、区分遗物；普通物品扣后仍大于0时调用+0x610 setter，然后发送Kind/route均为0x11的事件，载荷+4为角色ID、+8为物品ID。
- Item::SetStackSize 0x1803289B0：依次写+0x88C与+0x6B0；Item::GetStackSize 0x1803289A0读取+0x88C。C#新getter在声明该方法配置后读取RawStack88C，未知主字段不回退旧样本。
- Character::TakeItemFromCharacter(name, ...)，0x180060B20：遍历库存ID副本，比较ObjectName，按标志跳过装备，再调用对象入口，扣足即停止。本实现固定skip-equipment=true、affix-filter=false；该配置不进入附加物品向量，且名称入口非正数量返回0。
- 名称比较0x180593650：完整std::string长度、ASCII C locale大小写和斜杠折叠。对象名从C字符串复制所以截断NUL，查询名保留完整长度。没有通配符语义。

普通物品仍要求显式已知的getter/setter方法配置及IsRelic=false，不能仅凭“Item”导出表名称推定虚表槽。原始PE导出/方法表观察与Rea xrefs分开保存；从xrefs过滤到72组相邻直接Set/Get函数指针。这些是静态方法对证据，不证明任何运行中对象的类型，也不自动给未恢复生成器产物赋予配置。观察到的其他虚表槽（如Transmuter）不在支持域。

## 有状态回调与原子拒绝

OfflineEvents新增CheckCovered，仅核对实际路由订阅者是否全部在调用者提供的已知集合中；它本身不证明任意delegate等价。InventoryTake只构造封闭的HasItemCondition集合，并检查相同事件管理器与相同物品对象库，再对实际订阅者逐个CheckReceive。CheckReceive执行相同的接收验证但不写dirty/text。所有接收者都已检查后才写两个堆叠字段、发送事件。

因此非空、已恢复的物品目标回调确实执行；未知额外订阅者、缺失对象名/客户端身份/方法配置、对象库不一致或未覆盖事件日志均在库存写入前Unsupported。4096次分发预算在预检中检查，防止写后才触发分发预算失败。并发或回调外部突变不在稳定上下文范围；已覆盖HasItem处理器只修改其标志，不改路由。

库存操作及其事件为单独事务边界，后续任务Update/奖励继续使用自身预检。没有声称把两个独立原入口合成为新的自动回滚游戏事务。

## 关键边界

按对象入口请求0仍调用setter并发事件，可把不同步的副字段更新为主数量；测试验证这一点。按名称入口请求0或负数则在读取库存之前返回0，两者没有混为同一规则。

名称路径使用ObjectName，不偷换成HasItem计数使用的item-info数据库名；固定关闭affix-filter时不会套用计数器的词缀排除。装备跳过、未知装备位置拒绝、已满足首堆即停止、首匹配堆需整堆删除则拒绝（不跳到后堆）、对象NUL与查询NUL区别均已验证。

## 为什么完整仓库交付入口仍Unsupported

GameEngine::QuestCommandTakeItem（0x1802F30E0）转调接口+0x278；0x18030ECC0再调用Character虚槽+0x5C8。Player::TakeItemFromStashes（0x1803E4B50）在主角路径调用材料、个人仓库、共享仓库扣除后，还会调用SaveTransferStash和SaveReagents。保存及额外来源未恢复，因此没有把库存子集冒充完整QuestCommand，也没有执行这些保存函数或接触存档。

整堆删除需要角色虚槽+0x788、库存列表移除、+0x5C0及可能对象销毁；遗物还涉及片数和完整度。上述均继续Unsupported。当前只覆盖扣后保留正余量的非遗物堆叠；随机掉落、GiveItem生成、整堆销毁及完整仓库交付未恢复。

## 验收与安全边界

边界测试覆盖真实双字段写入及getter、只读预检、已恢复非空回调、未知额外回调拒绝、缺少覆盖集合、接收前提失败、缺失/未知对象、非物品与未知RTTI、遗物、未知setter、未知主字段、全量/过量/负数请求、角色过滤、跨快照接收者、空路由，以及名称匹配/装备/词缀/NUL/提前终止。相关回归与最终统一构建通过。一个名称用例曾把已访问控制器也设为未知，修正为明确空控制器后通过；没有放宽实现的未知信息拒绝。

所有变更前备份保留，全部产物指纹一致，Game.dll、Engine.dll、Quests.arc哈希不变。[验收数据](acceptance-inventory-take.json)记录源码、来源、清单与实际轨迹。未执行或加载原版程序/DLL，未访问真实或云存档，未改Steam/系统、绕保护、安装软件、commit、推送或发布。
