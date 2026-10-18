using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using GrimDynasty.TaskAuto;
class ItemAcceptQuestDemo {
 static void Need(bool b,string s){if(!b)throw new Exception(s);}
 static int Main(){try{Console.OutputEncoding=new System.Text.UTF8Encoding(false);var cases=new List<object>();
  foreach(int initial in new[]{1,2}){
   var x=ExampleItemAcceptQuest.Create(initial);var a=x.Quest.Auto;Need(AutomaticTasks.Start(a).Status=="Known","start item-gated acceptance");uint moneyAtAccept=x.Wallet.Money16C4.Value;var historyAtAccept=new List<uint>(x.Record.RewardTaskUidsAt50);Need(moneyAtAccept==(initial==1?50u:75u),"actual item-gated branch");
   Need(AutomaticTasks.Update(a).Status=="Known","first item update");
   if(initial==1){Need(a.Runtime.State.Task.State==2,"still waiting for second ration");x.Quest.Stock.Objects[100].QuantityFromSlot618=2;Need(x.Quest.Notify(16).Status=="Known","own decoded item notification");Need(AutomaticTasks.Update(a).Status=="Known","item goal completes");}
   Need(a.Runtime.State.Task.State==3&&x.Wallet.Money16C4==(initial==1?125u:150u)&&!x.Quest.Condition.Owner16Attached&&!x.Quest.Condition.Owner17Attached,"item-goal reward closure");
   cases.Add(new{declared_items_at_accept=initial,accept_money=moneyAtAccept,accept_reward_uids=historyAtAccept,final_money=x.Wallet.Money16C4,final_reward_uids=x.Record.RewardTaskUidsAt50,final_task_state=a.Runtime.State.Task.State,item_callbacks_detached=!x.Quest.Condition.Owner16Attached&&!x.Quest.Condition.Owner17Attached});
  }
  Console.WriteLine(new JavaScriptSerializer().Serialize(new{status="passed",scene="军需核验条件发饷",cases,scope="Own QST2 HasItem-gated acceptance actions, shared declared item snapshot and actual existing item-goal lifecycle plus money completion. Quantity change is an explicit supplied input, not recovered loot/GiveItem or original inventory capture. No original execution, save access, renderer or settings."}));return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
