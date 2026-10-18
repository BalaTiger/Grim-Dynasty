using System;
using System.Collections.Generic;
using GrimDynasty.TaskAuto;
using GrimDynasty.Rewards;
public static class RewardAutoTests {
 static int n;static void Is(bool b,string s){n++;if(!b)throw new Exception(s);}
 static void ToLastEvent(ExampleRewardQuest x){var a=x.Quest.Auto;AutomaticTasks.Start(a);AutomaticTasks.Update(a);x.Quest.Kill();AutomaticTasks.Update(a);x.Quest.Kill();}
 public static int Main(){try{
  var x=ExampleRewardQuest.Create(75);ToLastEvent(x);var a=x.Quest.Auto;var r=a.Runtime;var result=AutomaticTasks.Update(a);Is(result.Status=="Known"&&r.State.Task.State==3&&x.Completion.Wallet.Money16C4==125,"event to completion to actual wallet update");Is(x.Record.RewardTaskUidsAt50.Count==1&&x.Record.RewardTaskUidsAt50[0]==r.State.Task.Uid,"reward task UID append");Is(a.RewardRecordedA3[0]&&r.CompletionRecorded[0]&&!x.Quest.Counter.CallbackOwnerAttached,"history and end lifecycle");
  Is(AutomaticTasks.Update(a).Status=="Unsupported"&&x.Completion.Wallet.Money16C4==125,"outside active domain does not award again");AutomaticTasks.Reset(a);ToLastEvent(x);Is(AutomaticTasks.Update(a).Status=="Known"&&x.Completion.Wallet.Money16C4==200,"reset repeated task money not suppressed by history");Is(x.Record.RewardTaskUidsAt50.Count==2&&x.Record.RewardTaskUidsAt50[0]==x.Record.RewardTaskUidsAt50[1],"native vector permits repeated task IDs");
  x=ExampleRewardQuest.Create(75,-100,0,7);ToLastEvent(x);Is(AutomaticTasks.Update(x.Quest.Auto).Status=="Known"&&x.Completion.Wallet.Money16C4==7,"action order including zero reset");Is(x.Record.RewardTaskUidsAt50.Count==1,"one queue append per completion, not per action");
  x=ExampleRewardQuest.Create(75);ToLastEvent(x);a=x.Quest.Auto;r=a.Runtime;x.Completion.Wallet.StaticMoneyNameReady=false;Is(AutomaticTasks.Update(a).Status=="Unsupported"&&r.State.Task.State==2&&x.Quest.Counter.ChangedByte14==1&&x.Completion.Wallet.Money16C4==50&&x.Record.RewardTaskUidsAt50.Count==0,"unrecovered action dependency rejects atomically");
  x=ExampleRewardQuest.Create(75);ToLastEvent(x);x.Completion.Events[0].ConditionsAreNull=false;Is(AutomaticTasks.Update(x.Quest.Auto).Status=="Unsupported"&&x.Completion.Wallet.Money16C4==50,"uncovered event condition");
  x=ExampleRewardQuest.Create(75);ToLastEvent(x);x.Completion.QuestsByUid.Clear();x.Completion.QuestUidIndexComplete=false;Is(AutomaticTasks.Update(x.Quest.Auto).Status=="Unsupported"&&x.Completion.Wallet.Money16C4==50,"unknown UID index");x.Completion.QuestUidIndexComplete=true;Is(AutomaticTasks.Update(x.Quest.Auto).Status=="Known"&&x.Completion.Wallet.Money16C4==125&&x.Record.RewardTaskUidsAt50.Count==0,"known missing UID lookup skips queue");
  x=ExampleRewardQuest.Create(75);ToLastEvent(x);x.Quest.Auto.OnCompleteEventCount=0;Is(AutomaticTasks.Update(x.Quest.Auto).Status=="Unsupported"&&x.Completion.Wallet.Money16C4==50,"provider event count mismatch");
  x=ExampleRewardQuest.Create(75);ToLastEvent(x);x.Record.RewardTaskUidsAt50=null;Is(AutomaticTasks.Update(x.Quest.Auto).Status=="Unsupported"&&x.Completion.Wallet.Money16C4==50,"unknown record vector");
  Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+n+"}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
