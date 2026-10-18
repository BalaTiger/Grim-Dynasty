using System;
using System.Collections.Generic;
using GrimDynasty.TaskItems;
using GrimDynasty.TaskAuto;
class FullInventoryTakeTests {
 static int n;static void Need(bool b,string s){n++;if(!b)throw new Exception(s);}
 const string Name="ming/items/military_ration.dbr";
 static ExampleDeliveryQuest Start(params int[] stacks){var x=ExampleFullDelivery.Create(stacks);AutomaticTasks.Start(x.Quest.Auto);AutomaticTasks.Update(x.Quest.Auto);return x;}
 static void Main(){
  foreach(int request in new[]{2,3,5}){
   var x=Start(3);int taken;Need(InventoryTake.ExecuteInventoryByName(x.Inventory,Name,request,out taken).Status=="Known"&&taken==Math.Min(request,3),"less/equal/more actual taken");
   int count=ItemCountQuery.CountForHasItem(x.Quest.Stock,Name,true).Count.Value;Need(count==(request==2?1:0)&&x.Inventory.LastDelivered==1&&x.Quest.Condition.ChangedByte14==1,"query reflects membership removal and receiver ran");
   if(request>=3){Need(x.Quest.Stock.InventoryItemIds.Count==0&&x.Quest.Stock.Objects.ContainsKey(100)&&x.Item.RawStack88C==3&&x.Item.RawReplicaStack6B0==99,"full path removes references, retains live object and does not zero stack fields");Need(String.Join(",",x.Inventory.RemovalTrace)=="detach_action_noop:100,inventory_ids_removed:100,item_event17:100,item_hook1_noop:100,controller_notify_noop:100,object_retained:100","native remove/event/hook/notify order");}
   Need(AutomaticTasks.Update(x.Quest.Auto).Status=="Known"&&x.Wallet.Money16C4==125&&x.Record.RewardTaskUidsAt50.Count==1&&!x.Quest.Condition.Owner17Attached,"one completion and callback unregister");
   Need(x.Inventory.Events.Send(new QuestEvent{Kind=17,Body=new ItemEventBody{PlayerId=1,ItemObjectId=100}},17).Delivered==0&&x.Wallet.Money16C4==125&&x.Record.RewardTaskUidsAt50.Count==1,"late event cannot replay reward");
   if(request>=3){int again;int events=x.Inventory.Produced.Count;Need(InventoryTake.ExecuteInventoryByName(x.Inventory,Name,request,out again).Status=="Known"&&again==0&&x.Inventory.Produced.Count==events,"repeat name take has no dangling inventory reference");Need(InventoryTake.Execute(x.Inventory,100,3,out again).Status=="Unsupported","direct detached-object repeat outside present-inventory scope");}
  }
  var m=Start(1,3);int got;Need(InventoryTake.CheckInventoryByName(m.Inventory,Name,3).Status=="Known"&&m.Quest.Stock.InventoryItemIds.Count==2&&m.Item.RawStack88C==1&&m.Quest.Stock.Objects[101].RawStack88C==3&&m.Inventory.Produced.Count==0,"whole multi-stack plan preflight is read-only");
  Need(InventoryTake.ExecuteInventoryByName(m.Inventory,Name,3,out got).Status=="Known"&&got==3&&m.Quest.Stock.InventoryItemIds.Count==1&&m.Quest.Stock.InventoryItemIds[0]==101&&m.Quest.Stock.Objects[101].RawStack88C==1&&m.Inventory.Produced.Count==2&&m.Inventory.LastDelivered==2,"full first stack then retained second stack");
  Need(((ItemEventBody)m.Inventory.Produced[0].Body).ItemObjectId==100&&((ItemEventBody)m.Inventory.Produced[1].Body).ItemObjectId==101&&AutomaticTasks.Update(m.Quest.Auto).Status=="Known"&&m.Record.RewardTaskUidsAt50.Count==1,"multi-stack event order and single quest completion");
  m=Start(1,2);Need(InventoryTake.ExecuteInventoryByName(m.Inventory,Name,9,out got).Status=="Known"&&got==3&&m.Quest.Stock.InventoryItemIds.Count==0&&m.Inventory.DetachedObjects.Count==2,"over-request exhausts several stacks and returns available total");
  m=Start(1,3);m.Quest.Stock.Objects[101].RemovalHook1CallbackPresent=true;Need(InventoryTake.ExecuteInventoryByName(m.Inventory,Name,9,out got).Status=="Unsupported"&&got==0&&m.Quest.Stock.InventoryItemIds.Count==2&&m.Inventory.Produced.Count==0&&m.Quest.Condition.ChangedByte14==0,"unsupported later full hook rejects before earlier removal");
  m=Start(1,3);m.Quest.Stock.Objects[101].IsRelic=true;Need(InventoryTake.ExecuteInventoryByName(m.Inventory,Name,3,out got).Status=="Unsupported"&&m.Quest.Stock.InventoryItemIds.Count==2&&m.Inventory.DetachedObjects.Count==0,"later relic rejects whole plan");
  m=Start(3);m.Inventory.DestroyAfterFullTake=true;Need(InventoryTake.Execute(m.Inventory,100,3,out got).Status=="Unsupported"&&m.Quest.Stock.InventoryItemIds.Count==1&&m.Inventory.Produced.Count==0,"actual destruction remains unsupported before writes");
  m=Start(3);m.Inventory.DestroyAfterFullTake=null;Need(InventoryTake.Execute(m.Inventory,100,3,out got).Status=="Unsupported"&&m.Quest.Stock.InventoryItemIds.Count==1,"unknown destruction flag rejected");
  m=Start(3);m.Inventory.AttachedActionItemIds.Add(100);Need(InventoryTake.Execute(m.Inventory,100,3,out got).Status=="Unsupported"&&m.Quest.Stock.InventoryItemIds.Count==1,"attached action cannot be silently detached");
  m=Start(3);m.Inventory.MirrorPlayerPresent=true;Need(InventoryTake.Execute(m.Inventory,100,3,out got).Status=="Unsupported"&&m.Inventory.Produced.Count==0,"mirror player and clone destroy branch excluded");
  m=Start(3);m.Item.IsShield=true;Need(InventoryTake.Execute(m.Inventory,100,3,out got).Status=="Unsupported"&&m.Inventory.Produced.Count==0,"shield side effect not guessed");
  m=Start(3);m.Item.RemovalHookPackInitialized=false;Need(InventoryTake.Execute(m.Inventory,100,3,out got).Status=="Unsupported"&&m.Quest.Stock.InventoryItemIds.Count==1,"lazy hook initialization not skipped");
  m=Start(3);m.Item.RemovalHook1CallbackPresent=null;Need(InventoryTake.Execute(m.Inventory,100,3,out got).Status=="Unsupported"&&m.Quest.Stock.InventoryItemIds.Count==1,"unknown hook pointer rejected");
  m=Start(3);m.Quest.Stock.Objects.Add(20,new ItemObject{Kind=ItemObjectKind.ControllerCharacter});m.Quest.Stock.RawControllerId16C0=20;Need(InventoryTake.Execute(m.Inventory,100,3,out got).Status=="Unsupported"&&m.Quest.Stock.InventoryItemIds.Count==1,"nonempty controller removal side effects excluded");
  m=Start(3);m.Inventory.Events.Register(17,new OfflineHandler{Invoke=e=>{throw new Exception("must not run");}});Need(InventoryTake.Execute(m.Inventory,100,3,out got).Status=="Unsupported"&&m.Quest.Stock.InventoryItemIds.Count==1&&m.Quest.Condition.ChangedByte14==0,"unknown subscriber prevents full removal");
  m=Start(3);m.Quest.Stock.InventoryItemIds.Insert(0,101);m.Quest.Stock.InventoryItemIds.Add(100);m.Quest.Stock.InventoryItemIds.Add(102);Need(InventoryTake.Execute(m.Inventory,100,3,out got).Status=="Known"&&String.Join(",",m.Quest.Stock.InventoryItemIds)=="101,102"&&m.Inventory.Produced.Count==1,"inventory removes every matching ID and preserves other order");
  m=Start(3);Need(InventoryTake.Execute(m.Inventory,100,-1,out got).Status=="Known"&&got==3&&m.Quest.Stock.InventoryItemIds.Count==0,"object-entry minus one takes whole stack");
  m=Start(3);m.Inventory.CharacterObjectId=2;m.Inventory.InventoryOwnerObjectId=1;Need(InventoryTake.Execute(m.Inventory,100,3,out got).Status=="Known"&&((ItemEventBody)m.Inventory.Produced[0].Body).PlayerId==1&&m.Quest.Condition.ChangedByte14==1,"full event uses inventory owner field rather than partial character ID");
  Console.WriteLine("Full inventory take assertions passed: "+n);
 }
}
