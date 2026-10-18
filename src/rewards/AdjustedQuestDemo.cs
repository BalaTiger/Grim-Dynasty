using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using GrimDynasty.TaskAuto;
using GrimDynasty.Rewards;
class AdjustedQuestDemo {
 static void Need(bool b,string s){if(!b)throw new Exception(s);}
 static int Main(){try{
  var q=ExampleKillQuest.Create(2);var a=q.Auto;a.Runtime.State.Difficulty=2;a.Runtime.World.Core.Difficulty=2;a.Runtime.State.Task.Objectives.Add(2,a.Runtime.State.Task.Objectives[0]);
  var c=new AdjustedCompletion{Wallet=new WalletSnapshot{MainPlayerResolved=true,Money16C4=50,MoneyNameCache=new NameCacheSnapshot()},Actor=new PointActor{MainPlayerResolved=true,Difficulty=2,Flags17F4=0,ObjectId=17,Skill176C=4,Attribute1768=3,SkillNameCache=new NameCacheSnapshot(),Events=q.Events},Events=new List<NumericEvent>(),QuestUidIndexComplete=true};
  var e=new NumericEvent{ConditionsKnown=true,Actions=new List<NumericAction>{new NumericAction{TypeId=4,Money=new MoneyAction{Amount=5}},new NumericAction{TypeId=15,Point=new PointAction{OriginalAmount=2,CurrentAmount=2}},new NumericAction{TypeId=18,Point=new PointAction{Attribute=true,OriginalAmount=1,CurrentAmount=1}}}};
  c.Events.Add(e);c.QuestsByUid.Add(2,new RewardQuestSnapshot{Quest=a.Runtime.World.Quests[a.Runtime.QuestReference],RewardTaskUidsAt50=new List<uint>()});a.CompletionRuntime=c;a.OnCompleteEventCount=1;
  var trace=new List<object>();Need(AutomaticTasks.Start(a).Status=="Known","start");AutomaticTasks.Update(a);trace.Add(new{step="accept",difficulty=2,money=c.Wallet.Money16C4,skill=c.Actor.Skill176C,attribute=c.Actor.Attribute1768,name_cache_cold=!c.Wallet.MoneyNameCache.Initialized});
  for(int i=0;i<2;i++){Need(q.Kill().Delivered==1,"kill event");Need(AutomaticTasks.Update(a).Status=="Known","automatic update");}
  Need(a.Runtime.State.Task.State==3&&c.Wallet.Money16C4==55&&c.Actor.Skill176C==10&&c.Actor.Attribute1768==6,"reward values");
  trace.Add(new{step="automatic_complete_and_adjust",multiplier=c.LastMultipliers[0],money=c.Wallet.Money16C4,skill=c.Actor.Skill176C,attribute=c.Actor.Attribute1768,head_effect_hash=c.Wallet.MoneyNameCache.Value,skill_cache_initialized=c.Actor.SkillNameCache.Initialized});
  Need(c.Actor.Produced.Count==1,"produced notification");trace.Add(new{step="produced_attribute_event",kind=c.Actor.Produced[0].Kind,player=((AttributePointNotification)c.Actor.Produced[0].Body).PlayerObjectId,reward_task_uid=c.QuestsByUid[2].RewardTaskUidsAt50[0],reward_history=a.RewardRecordedA3,kill_handler_detached=!q.Counter.CallbackOwnerAttached});
  Console.WriteLine(new JavaScriptSerializer().Serialize(new{status="passed",trace,scope="Own original scenario, decoded kill inputs, static-derived reward adjustment and cold Name hash, actual own wallet/point/history writes and produced attribute event. Known empty attribute subscribers; no original game, combat producer, UI, resource, save or network."}));return 0;
 }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
}
