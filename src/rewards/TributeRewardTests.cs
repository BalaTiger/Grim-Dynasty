using System;
using System.Collections.Generic;
using GrimDynasty.Rewards;
using GrimDynasty.TaskAuto;
class TributeRewardTests {
 static int n;static void Need(bool b,string s){n++;if(!b)throw new Exception(s);}
 static PointActor Actor(uint balance){return new PointActor{MainPlayerResolved=true,Tribute16C8=balance,Events=new OfflineEvents(true){DebugEnabled=false}};}
 static void Main(){
  var p=Actor(10);Need(TributeRewards.Execute(new TributeAction{Amount=5},p).Status=="Known"&&p.Tribute16C8==15&&p.Produced.Count==1,"positive produces notification");Need(p.Produced[0].Kind==0x3f&&p.Produced[0].Body==null,"kind only notification");
  p=Actor(149);TributeRewards.Execute(new TributeAction{Amount=5},p);Need(p.Tribute16C8==150&&p.Produced.Count==1,"cap 150");
  p=Actor(150);TributeRewards.Execute(new TributeAction{Amount=5},p);Need(p.Tribute16C8==150&&p.Produced.Count==0,"cap no notification");
  p=Actor(uint.MaxValue);TributeRewards.Execute(new TributeAction{Amount=1},p);Need(p.Tribute16C8==0&&p.Produced.Count==0,"wrap before unsigned cap");
  p=Actor(200);TributeRewards.Execute(new TributeAction{Amount=1},p);Need(p.Tribute16C8==150&&p.Produced.Count==0,"over cap decreases without notification");
  p=Actor(10);p.Events=null;TributeRewards.Execute(new TributeAction{Amount=-4},p);Need(p.Tribute16C8==6&&p.Produced.Count==0,"negative needs no event context");
  TributeRewards.Execute(new TributeAction{Amount=-20},p);Need(p.Tribute16C8==0,"negative clamp");
  p=Actor(10);TributeRewards.Execute(new TributeAction{Amount=0},p);Need(p.Tribute16C8==0&&p.Produced.Count==0,"zero clears tribute");
  p=Actor(10);p.Events.Register(0x3f,new OfflineHandler{Invoke=e=>{}});Need(TributeRewards.Execute(new TributeAction{Amount=1},p).Status=="Unsupported"&&p.Tribute16C8==10&&p.Produced.Count==0,"unknown subscriber rejects before writes");
  p=Actor(10);p.MainPlayerResolved=false;p.Tribute16C8=null;Need(TributeRewards.Execute(new TributeAction{Amount=1},p).Status=="Known","missing actor noop");
  p=Actor(10);Need(TributeRewards.Execute(new TributeAction{Version=1,Amount=1},p).Status=="Unsupported"&&p.Tribute16C8==10,"unknown version");
  var q=ExampleKillQuest.Create(2);var a=q.Auto;p=Actor(149);p.Difficulty=0;p.Flags17F4=0;p.Events=q.Events;
  var c=new AdjustedCompletion{Actor=p,Events=new List<NumericEvent>{new NumericEvent{ConditionsKnown=true,Actions=new List<NumericAction>{new NumericAction{TypeId=26,Tribute=new TributeAction{Amount=5}}}}},QuestUidIndexComplete=true};c.QuestsByUid.Add(2,new RewardQuestSnapshot{Quest=a.Runtime.World.Quests[a.Runtime.QuestReference],RewardTaskUidsAt50=new List<uint>()});a.CompletionRuntime=c;a.OnCompleteEventCount=1;
  AutomaticTasks.Start(a);AutomaticTasks.Update(a);q.Kill();AutomaticTasks.Update(a);q.Kill();Need(AutomaticTasks.Update(a).Status=="Known"&&p.Tribute16C8==150&&a.Runtime.State.Task.State==3,"automatic tribute completion");Need(p.Produced.Count==1&&p.Produced[0].Kind==0x3f&&c.QuestsByUid[2].RewardTaskUidsAt50.Count==1,"completion event and UID queue");
  Console.WriteLine("Tribute reward assertions passed: "+n);
 }
}
