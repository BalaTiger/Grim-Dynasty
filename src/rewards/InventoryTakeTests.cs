using System;
using System.Collections.Generic;
using GrimDynasty.TaskItems;
using GrimDynasty.TaskAuto;
class InventoryTakeTests {
 static int n;static void Need(bool b,string s){n++;if(!b)throw new Exception(s);}
 static ExampleDeliveryQuest Started(){var x=ExampleDeliveryQuest.Create();if(AutomaticTasks.Start(x.Quest.Auto).Status!="Known")throw new Exception("start fixture");AutomaticTasks.Update(x.Quest.Auto);return x;}
 static void Main(){
  var x=Started();int taken;Need(x.Item.QuantityFromSlot618==3,"declared direct getter reads backing field instead of sampled 999");
  Need(InventoryTake.Check(x.Inventory,100,2).Status=="Known"&&x.Item.RawStack88C==3&&x.Item.RawReplicaStack6B0==99&&x.Quest.Condition.ChangedByte14==0&&x.Inventory.Produced.Count==0,"preflight leaves stack, replica and subscribed condition unchanged");
  Need(InventoryTake.Execute(x.Inventory,100,2,out taken).Status=="Known"&&taken==2&&x.Item.RawStack88C==1&&x.Item.RawReplicaStack6B0==1&&x.Item.QuantityFromSlot618==1,"native two-field stack mutation visible to item getter");
  var e=x.Inventory.Produced[0];var body=(ItemEventBody)e.Body;Need(e.Kind==17&&body.PlayerId==1&&body.ItemObjectId==100&&x.Inventory.LastDelivered==1&&x.Quest.Condition.ChangedByte14==1&&x.Quest.Condition.TextByte15==1,"actual nonempty HasItem callback marks both flags");
  Need(AutomaticTasks.Update(x.Quest.Auto).Status=="Known"&&x.Quest.Auto.Runtime.State.Task.State==3&&x.Wallet.Money16C4==125&&x.Record.RewardTaskUidsAt50.Count==1&&!x.Quest.Condition.Owner16Attached&&!x.Quest.Condition.Owner17Attached,"inventory mutation drives condition goal and completion reward");
  x=Started();Need(InventoryTake.Execute(x.Inventory,100,0,out taken).Status=="Known"&&taken==0&&x.Item.RawStack88C==3&&x.Item.RawReplicaStack6B0==3&&x.Inventory.LastDelivered==1&&x.Quest.Condition.ChangedByte14==1,"object-entry zero request still sets replica and sends17");
  x=Started();x.Inventory.Events.Register(17,new OfflineHandler{Invoke=p=>{throw new Exception("unrestored handler must not run");}});Need(InventoryTake.Execute(x.Inventory,100,2,out taken).Status=="Unsupported"&&taken==0&&x.Item.RawStack88C==3&&x.Item.RawReplicaStack6B0==99&&x.Quest.Condition.ChangedByte14==0&&x.Inventory.Produced.Count==0,"unknown extra receiver atomically rejects before inventory writes");
  x=Started();x.Inventory.CoveredConditions=null;Need(InventoryTake.Execute(x.Inventory,100,2,out taken).Status=="Unsupported"&&x.Item.RawStack88C==3,"registered receiver not silently skipped without coverage");
  x=Started();x.Item.ObjectName=null;Need(InventoryTake.Execute(x.Inventory,100,2,out taken).Status=="Unsupported"&&x.Item.RawStack88C==3&&x.Quest.Condition.ChangedByte14==0,"late receiver lookup failure rejected before two-field setter");
  x=Started();x.Inventory.Stock=ItemWorld.Empty();Need(InventoryTake.Execute(x.Inventory,100,2,out taken).Status=="Known"&&taken==0&&x.Inventory.Produced.Count==0,"known absent object returns before actor and receiver dependencies");
  x=Started();x.Inventory.Stock=ItemWorld.Empty();x.Inventory.Stock.ObjectIndexComplete=false;Need(InventoryTake.Execute(x.Inventory,100,2,out taken).Status=="Unsupported"&&taken==0,"unknown object index differs from missing object");
  x=Started();x.Item.Kind=ItemObjectKind.Other;Need(InventoryTake.Execute(x.Inventory,100,2,out taken).Status=="Known"&&taken==0&&x.Item.RawStack88C==3&&x.Inventory.Produced.Count==0,"known nonitem does not mutate or notify");
  x=Started();x.Item.Kind=ItemObjectKind.Unknown;Need(InventoryTake.Execute(x.Inventory,100,2,out taken).Status=="Unsupported"&&x.Item.RawStack88C==3,"unknown RTTI not nonitem");
  x=Started();x.Item.IsRelic=true;Need(InventoryTake.Execute(x.Inventory,100,2,out taken).Status=="Unsupported"&&x.Item.RawStack88C==3,"relic fragment branches remain unsupported");
  x=Started();x.Item.IsRelic=null;Need(InventoryTake.Execute(x.Inventory,100,2,out taken).Status=="Unsupported"&&x.Item.RawStack88C==3,"unknown relic status rejected");
  x=Started();x.Item.StackMethods=ItemStackMethods.Unknown;Need(InventoryTake.Execute(x.Inventory,100,2,out taken).Status=="Unsupported"&&x.Item.RawStack88C==3,"sampled count does not prove setter implementation");
  x=Started();x.Item.RawStack88C=null;Need(x.Item.QuantityFromSlot618==null&&InventoryTake.Execute(x.Inventory,100,2,out taken).Status=="Unsupported"&&x.Item.RawReplicaStack6B0==99,"direct getter never falls back to stale sampled quantity");
  foreach(int amount in new[]{3,4,-1,-2}){x=Started();Need(InventoryTake.Execute(x.Inventory,100,amount,out taken).Status=="Unsupported"&&x.Item.RawStack88C==3&&x.Item.RawReplicaStack6B0==99&&x.Inventory.Produced.Count==0,"full removal or signed request outside bounded branch");}
  x=Started();x.Inventory.CharacterObjectId=null;Need(InventoryTake.Execute(x.Inventory,100,2,out taken).Status=="Unsupported"&&x.Item.RawStack88C==3,"unknown event actor preserves stack");
  x=Started();x.Inventory.CharacterObjectId=2;Need(InventoryTake.Execute(x.Inventory,100,2,out taken).Status=="Known"&&x.Item.RawStack88C==1&&x.Inventory.LastDelivered==1&&x.Quest.Condition.ChangedByte14==0,"receiver retains client-player filter for foreign event actor");
  x=Started();var other=ExampleDeliveryQuest.Create();x.Inventory.CoveredConditions=new List<HasItemCondition>{other.Quest.Condition};Need(InventoryTake.Execute(x.Inventory,100,2,out taken).Status=="Unsupported"&&x.Item.RawStack88C==3,"receiver manager/stock mismatch refused");
  x=Started();x.Quest.Condition.End();x.Inventory.CoveredConditions=null;Need(InventoryTake.Execute(x.Inventory,100,2,out taken).Status=="Known"&&x.Item.RawStack88C==1&&x.Inventory.LastDelivered==0&&x.Inventory.Produced.Count==1,"known empty route preserves notification production");
  Console.WriteLine("Inventory take assertions passed: "+n);
 }
}
