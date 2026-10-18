using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using GrimDynasty.TaskAuto;
public static class ItemQuestDemo {
 static void Need(bool b,string s){if(!b)throw new Exception(s);}
 public static int Main(){try{
  var x=ExampleItemQuest.Create();var trace=new List<object>();var r=x.Auto.Runtime;Need(AutomaticTasks.Start(x.Auto).Status=="Known","start");AutomaticTasks.Update(x.Auto);trace.Add(new{step="start_and_update",task_state=r.State.Task.State,objective_state=r.ObjectiveCounters[x.Goal]});
  for(int amount=1;amount<=2;amount++){x.Stock.Objects[100].QuantityFromSlot618=amount;Need(x.Notify(amount==1?16u:17u).Delivered==1,"notify");var updated=AutomaticTasks.Update(x.Auto);Need(updated.Status=="Known","update");trace.Add(new{step="declare_updated_stock_then_dispatch_event_and_update",declared_quantity=amount,task_state=r.State.Task.State,objective_state=r.ObjectiveCounters[x.Goal]});}
  Need(r.State.Task.State==3&&!x.Condition.Owner16Attached&&!x.Condition.Owner17Attached,"complete");Need(AutomaticTasks.Reset(x.Auto).Status=="Known","reset");trace.Add(new{step="reset_without_removing_items",task_state=r.State.Task.State,quantity=x.Stock.Objects[100].QuantityFromSlot618});
  Console.WriteLine(new JavaScriptSerializer().Serialize(new{status="passed",trace,scope="Own fixture with declared runtime item snapshots and decoded events. Count query and condition/quest transitions restored; native loot/GiveItem, real stash/save loading, text display, reward grants, network and original execution not performed."}));return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
