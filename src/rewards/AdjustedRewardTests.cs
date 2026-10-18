using System;
using System.Collections.Generic;
using GrimDynasty.Conditions;
using GrimDynasty.TaskAuto;
using GrimDynasty.Rewards;
class AdjustedRewardTests {
 static int n;static void Need(bool b,string s){n++;if(!b)throw new Exception(s);}
 static NumericEvent Event(){return new NumericEvent{ConditionsKnown=true,Actions=new List<NumericAction>()};}
 static NumericAction Point(bool attrib,uint amount){return new NumericAction{TypeId=attrib?18:15,Point=new PointAction{Attribute=attrib,OriginalAmount=amount,CurrentAmount=amount}};}
 static NumericEvent Difficulty(uint d){var e=Event();e.Conditions=new List<ConditionSpec>{new ConditionSpec{TypeId=4,StoredDifficulty=d,ComparisonId=2}};return e;}
 static AdjustedCompletion Setup(ExampleKillQuest q,uint d){
  var a=q.Auto;a.Runtime.State.Difficulty=d;a.Runtime.World.Core.Difficulty=d;
  if(d!=0)a.Runtime.State.Task.Objectives.Add(d,a.Runtime.State.Task.Objectives[0]);
  var c=new AdjustedCompletion{Wallet=new WalletSnapshot{MainPlayerResolved=true,Money16C4=50,MoneyNameCache=new NameCacheSnapshot()},Actor=new PointActor{MainPlayerResolved=true,Difficulty=d,Flags17F4=0,ObjectId=17,Skill176C=4,Attribute1768=3,SkillNameCache=new NameCacheSnapshot(),Events=q.Events},Events=new List<NumericEvent>(),QuestUidIndexComplete=true};
  c.QuestsByUid.Add(2,new RewardQuestSnapshot{Quest=a.Runtime.World.Quests[a.Runtime.QuestReference],RewardTaskUidsAt50=new List<uint>()});a.CompletionRuntime=c;return c;
 }
 static void Last(ExampleKillQuest q){Need(AutomaticTasks.Start(q.Auto).Status=="Known","start");AutomaticTasks.Update(q.Auto);q.Kill();AutomaticTasks.Update(q.Auto);q.Kill();}
 static void Main(){
  Need(RewardNames.Create(new byte[0])==0x811c9dc5,"empty FNV basis");
  Need(RewardNames.Create(new byte[]{65,0,66})==0xc40bf6cc,"C string terminates");
  Need(RewardNames.Create(new byte[]{255})==unchecked((0x811c9dc5u^0xffffffffu)*0x1000193u),"signed char FNV");
  var cache=new NameCacheSnapshot();Need(cache.EnsureHeadEffect().Status=="Known"&&cache.Value==0xcb999846&&cache.InitializationCount==1,"cold HeadEffect");cache.EnsureHeadEffect();Need(cache.InitializationCount==1,"one initialization");
  cache.Value=0;Need(cache.EnsureHeadEffect().Status=="Unsupported","inconsistent warm cache");
  var wallet=new WalletSnapshot{MainPlayerResolved=false,MoneyNameCache=new NameCacheSnapshot()};Need(MoneyRewards.Execute(new MoneyAction(),wallet).Status=="Known"&&wallet.MoneyNameCache.Initialized,"init precedes missing actor");
  var e=Event();var events=new[]{e};
  Need(RewardAdjustment.Calculate(events,0,new bool[3],0).Multipliers[0]==1,"normal multiplier");
  Need(RewardAdjustment.Calculate(events,1,new bool[3],0).Multipliers[0]==2,"elite catchup");
  Need(RewardAdjustment.Calculate(events,2,new bool[3],0).Multipliers[0]==3,"ultimate catchup");
  Need(RewardAdjustment.Calculate(events,2,new[]{true,false,true},0).Multipliers[0]==1,"history excludes banks");
  Need(RewardAdjustment.Calculate(events,2,new bool[3],3).Multipliers[0]==1,"profile excludes lower banks");
  Need(RewardAdjustment.Calculate(events,1,new bool[3],2).Multipliers[0]==1,"elite profile bit");
  Need(RewardAdjustment.Calculate(events,2,new[]{true,true,true},0).Multipliers[0]==0,"all history scales to zero");
  e=Difficulty(2);Need(RewardAdjustment.Calculate(new[]{e},2,new bool[3],0).Multipliers[0]==-1,"difficulty-specific event is not scaled");
  Need(e.Conditions[0].DifficultyOverride==uint.MaxValue,"override restored after dry run");
  e.Conditions[0].TypeId=5;Need(RewardAdjustment.Calculate(new[]{e},2,new bool[3],0).Status=="Unsupported","unknown override lifecycle");
  var q=ExampleKillQuest.Create(2);var c=Setup(q,2);e=Event();e.Actions.Add(new NumericAction{TypeId=4,Money=new MoneyAction{Amount=5}});e.Actions.Add(Point(false,2));e.Actions.Add(Point(true,1));c.Events.Add(e);q.Auto.OnCompleteEventCount=1;Last(q);
  Need(AutomaticTasks.Update(q.Auto).Status=="Known"&&q.Auto.Runtime.State.Task.State==3,"automatic adjusted completion");
  Need(c.Wallet.Money16C4==55&&c.Actor.Skill176C==10&&c.Actor.Attribute1768==6,"money unscaled and points times three");
  Need(c.Wallet.MoneyNameCache.Initialized&&c.Actor.SkillNameCache.Initialized,"cold caches committed");
  Need(c.Actor.Produced.Count==1&&c.Actor.Produced[0].Kind==0x32,"completion produces attribute event");
  Need(c.LastMultipliers[0]==3&&e.Actions[1].Point.CurrentAmount==6&&e.Actions[1].Point.OriginalAmount==2,"actual adjusted action write");
  Need(c.QuestsByUid[2].RewardTaskUidsAt50.Count==1&&q.Auto.RewardRecordedA3[0]&&q.Auto.RewardRecordedA3[1]&&q.Auto.RewardRecordedA3[2],"reward queue and history banks");
  q=ExampleKillQuest.Create(2);c=Setup(q,2);e=Difficulty(2);var point=Point(false,2);point.Point.CurrentAmount=7;e.Actions.Add(point);c.Events.Add(e);q.Auto.OnCompleteEventCount=1;Last(q);Need(AutomaticTasks.Update(q.Auto).Status=="Known"&&c.Actor.Skill176C==11&&point.Point.CurrentAmount==7,"specific event preserves current amount");
  q=ExampleKillQuest.Create(2);c=Setup(q,0);e=Difficulty(1);e.Actions.Add(Point(false,2));c.Events.Add(e);q.Auto.OnCompleteEventCount=1;Last(q);Need(AutomaticTasks.Update(q.Auto).Status=="Known"&&c.Actor.Skill176C==4&&!c.Actor.SkillNameCache.Initialized&&c.QuestsByUid[2].RewardTaskUidsAt50.Count==0,"inactive event neither action nor reward queue");
  q=ExampleKillQuest.Create(2);c=Setup(q,0);e=Event();e.Actions.Add(new NumericAction{TypeId=4,Money=new MoneyAction{Amount=9}});e.Actions.Add(Point(true,1));c.Events.Add(e);q.Auto.OnCompleteEventCount=1;q.Events.Register(0x32,new OfflineHandler{Invoke=x=>{}});Last(q);Need(AutomaticTasks.Update(q.Auto).Status=="Unsupported"&&c.Wallet.Money16C4==50&&!c.Wallet.MoneyNameCache.Initialized&&q.Auto.Runtime.State.Task.State==2,"unsupported subscriber rejects before all writes");
  q=ExampleKillQuest.Create(2);c=Setup(q,0);e=Event();point=Point(false,1);e.Actions.Add(point);e.Actions.Add(point);c.Events.Add(e);q.Auto.OnCompleteEventCount=1;Need(c.Check(q.Auto).Status=="Unsupported"&&!c.Actor.SkillNameCache.Initialized,"aliased actions conservative rejection");
  Console.WriteLine("Adjusted reward assertions passed: "+n);
 }
}
