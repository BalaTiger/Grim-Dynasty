using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using GrimDynasty.TaskAuto;
class AcceptQuestDemo {
 static void Need(bool b,string s){if(!b)throw new Exception(s);}
 static int Main(){try{Console.OutputEncoding=new System.Text.UTF8Encoding(false);
  var x=ExampleAcceptQuest.Create();var a=x.Reward.Quest.Auto;var wallet=x.Accept.Wallet;x.Reward.Record.RewardTaskUidsAt50.Add(99);var trace=new List<object>();
  Need(AutomaticTasks.Start(a).Status=="Known","accept actions");Need(wallet.Money16C4==75&&x.Tutorials.UnlockOrder36CA0.Count==2&&x.Reward.Record.RewardTaskUidsAt50.Count==2,"accept writes");
  trace.Add(new{step="decode_own_qst2_and_accept",money=wallet.Money16C4,tutorials=new List<uint>(x.Tutorials.UnlockOrder36CA0),reward_uids=new List<uint>(x.Reward.Record.RewardTaskUidsAt50),kill_goal_registered=x.Reward.Quest.Counter.CallbackOwnerAttached});
  AutomaticTasks.Update(a);for(int i=0;i<2;i++){Need(x.Reward.Quest.Kill().Delivered==1,"kill input");Need(AutomaticTasks.Update(a).Status=="Known","update");}
  Need(a.Runtime.State.Task.State==3&&wallet.Money16C4==150&&x.Reward.Record.RewardTaskUidsAt50.Count==3&&!x.Reward.Quest.Counter.CallbackOwnerAttached,"complete closure");
  trace.Add(new{step="automatic_complete_after_two_kills",money=wallet.Money16C4,reward_uids=x.Reward.Record.RewardTaskUidsAt50,task_state=a.Runtime.State.Task.State,kill_goal_detached=!x.Reward.Quest.Counter.CallbackOwnerAttached});
  Console.WriteLine(new JavaScriptSerializer().Serialize(new{status="passed",scene="边军领饷",trace,scope="Own QST2 acceptance definition decoded into actual tutorial/money actions before task activation and goal registration, then existing automatic money completion. Explicit known-empty notification subscribers; no original execution, resources, saves or renderer."}));return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
