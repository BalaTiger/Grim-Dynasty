using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using GrimDynasty.TaskItems;
using GrimDynasty.TaskAuto;
class RuntimeIdInventoryDemo {
 static void Need(bool b,string s){if(!b)throw new Exception(s);}
 static int Main(){try{
  Console.OutputEncoding=new System.Text.UTF8Encoding(false);
  var x=ExampleInventoryCycle.Create();var stock=x.Base.Quest.Stock;
  stock.InventoryItemIds.Clear();stock.Objects.Clear();
  foreach(uint k in new uint[]{1,40,41})stock.Objects.Add(k,new ItemObject{Kind=ItemObjectKind.Other});
  var state=new ObjectIdState{Counter168=40,Wrapped16C=1,NetworkManagerPresent=false,IndexComplete=true,IndexedKeys=new HashSet<uint>{1,40,41}};
  uint id;Need(ObjectIdAllocator.Allocate(state,out id).Status=="Known"&&id==42&&state.Counter168==43,"native-derived ID allocation");
  // Explicit own fixture descriptor supply. This is NOT native creation/registration.
  var supplied=x.Base.Item;stock.Objects.Add(id,supplied);state.IndexedKeys.Add(id);
  AutomaticTasks.Start(x.Tasks[1]);AutomaticTasks.Update(x.Tasks[1]);
  Need(InventoryAdd.Execute(x.Add,id,false).Status=="Known"&&AutomaticTasks.Update(x.Tasks[1]).Status=="Known"&&x.Base.Wallet.Money16C4==90,"allocated ID drives add and task reward");
  var added=new{inventory_ids=new List<uint>(stock.InventoryItemIds),money=x.Base.Wallet.Money16C4,reward_uids=new List<uint>(x.Base.Record.RewardTaskUidsAt50)};
  AutomaticTasks.Start(x.Tasks[0]);AutomaticTasks.Update(x.Tasks[0]);int taken;
  Need(InventoryTake.ExecuteInventoryByName(x.Base.Inventory,"ming/items/military_ration.dbr",3,out taken).Status=="Known"&&taken==3&&AutomaticTasks.Update(x.Tasks[0]).Status=="Known"&&x.Base.Wallet.Money16C4==165,"allocated ID drives remove and task reward");
  Need(stock.InventoryItemIds.Count==0&&Object.ReferenceEquals(supplied,stock.Objects[id])&&x.Base.Record.RewardTaskUidsAt50.Count==2,"retained own descriptor and exactly two rewards");
  Console.WriteLine(new JavaScriptSerializer().Serialize(new{status="passed",scene="runtime_id_inventory_tasks",allocated_id=id,next_counter=state.Counter168,wrapped=state.Wrapped16C,after_add=added,after_remove=new{inventory_ids=stock.InventoryItemIds,money=x.Base.Wallet.Money16C4,reward_uids=x.Base.Record.RewardTaskUidsAt50,same_supplied_descriptor=true},scope="Native-derived CreateObjectID state machine plus explicit original C# fixture descriptor supply, existing inventory/task branches. Fixture dictionary insertion is not restored native construction, registration, generation, callbacks, saves or original execution."}));return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
