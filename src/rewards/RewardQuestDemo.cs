using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using GrimDynasty.TaskAuto;
public static class RewardQuestDemo {
 static void Need(bool b,string s){if(!b)throw new Exception(s);}
 public static int Main(){try{
  var x=ExampleRewardQuest.Create(75);var a=x.Quest.Auto;var trace=new List<object>();Need(AutomaticTasks.Start(a).Status=="Known","start");AutomaticTasks.Update(a);trace.Add(new{step="accept",task_state=a.Runtime.State.Task.State,money=x.Completion.Wallet.Money16C4});
  for(int i=1;i<=2;i++){Need(x.Quest.Kill().Delivered==1,"event");var r=AutomaticTasks.Update(a);Need(r.Status=="Known","update");trace.Add(new{step="kill_event_then_update",kills=x.Quest.Counter.Current,task_state=a.Runtime.State.Task.State,money=x.Completion.Wallet.Money16C4});}
  Need(a.Runtime.State.Task.State==3&&x.Completion.Wallet.Money16C4==125&&x.Record.RewardTaskUidsAt50.Count==1,"reward");trace.Add(new{step="completion_reward_record",task_uid=x.Record.RewardTaskUidsAt50[0],reward_history=a.RewardRecordedA3[0],handler_detached=!x.Quest.Counter.CallbackOwnerAttached});
  Console.WriteLine(new JavaScriptSerializer().Serialize(new{status="passed",trace,scope="Actual writes to own runtime wallet and reward UID list after automatic task completion; exact GiveMoney branches. Null event conditions, warmed internal name state, closed UI time gate, no native combat producer, real player/save, original DLL, network or reward UI."}));return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
