using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using GrimDynasty.TaskItems;
using GrimDynasty.TaskAuto;
class FullInventoryDeliveryDemo {
 static void Need(bool b,string s){if(!b)throw new Exception(s);}
 const string Name="ming/items/military_ration.dbr";
 static int Main(){try{Console.OutputEncoding=new System.Text.UTF8Encoding(false);var cases=new List<object>();
  foreach(string mode in new[]{"less","equal","more","multiple_stacks"}){
   var x=mode=="multiple_stacks"?ExampleFullDelivery.Create(1,3):ExampleFullDelivery.Create(3);var a=x.Quest.Auto;AutomaticTasks.Start(a);AutomaticTasks.Update(a);
   int request=mode=="less"?2:mode=="more"?5:3;int taken;Need(InventoryTake.ExecuteInventoryByName(x.Inventory,Name,request,out taken).Status=="Known","take plan");int remaining=ItemCountQuery.CountForHasItem(x.Quest.Stock,Name,true).Count.Value;
   Need(taken==(mode=="less"?2:3)&&remaining==(mode=="less"||mode=="multiple_stacks"?1:0),"actual amount and inventory query");
   Need(AutomaticTasks.Update(a).Status=="Known"&&a.Runtime.State.Task.State==3&&x.Wallet.Money16C4==125&&x.Record.RewardTaskUidsAt50.Count==1&&!x.Quest.Condition.Owner17Attached,"complete once and unregister");
   int eventCount=x.Inventory.Produced.Count;if(remaining==0){int again;Need(InventoryTake.ExecuteInventoryByName(x.Inventory,Name,request,out again).Status=="Known"&&again==0&&x.Inventory.Produced.Count==eventCount&&x.Wallet.Money16C4==125&&x.Record.RewardTaskUidsAt50.Count==1,"empty inventory request does not repeat completion");}
   var late=x.Inventory.Events.Send(new QuestEvent{Kind=17,Body=new ItemEventBody{PlayerId=1,ItemObjectId=100}},17);Need(late.Delivered==0,"late removed-item event sees no task subscriber");
   cases.Add(new{mode,request,taken,remaining_inventory_count=remaining,inventory_ids=new List<uint>(x.Quest.Stock.InventoryItemIds),detached_object_ids=new List<uint>(x.Inventory.DetachedObjects),detached_objects_still_resolvable=x.Quest.Stock.Objects.ContainsKey(100),raw_stack_of_object100=x.Item.RawStack88C,events=eventCount,order=x.Inventory.RemovalTrace,money=x.Wallet.Money16C4,reward_uids=x.Record.RewardTaskUidsAt50,task_callbacks_detached=!x.Quest.Condition.Owner16Attached&&!x.Quest.Condition.Owner17Attached});
  }
  Console.WriteLine(new JavaScriptSerializer().Serialize(new{status="passed",scene="整堆交付与多堆移出库存",cases,scope="Own non-relic stacks, present inventory IDs and explicit Player detach/notify no-op branches. Inventory remove-all then event17 then known empty initialized hook and absent controller, destroy=false retains valid objects. No original execution, destructor, object-manager unregister, save, loot/GiveItem or renderer."}));return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
