using System;
using System.Collections.Generic;
using GrimDynasty.TaskLogic;
using GrimDynasty.TokenActions;
using GrimDynasty.TaskRuntime;
using GrimDynasty.TaskAuto;
using GrimDynasty.TaskKills;
public sealed class ExampleKillQuest {
 public AutoSnapshot Auto;public KillCondition Counter;public Predicate Predicate;public Objective Goal;public KillWorld Entities;public OfflineEvents Events;
 public static ExampleKillQuest Create(uint required){
  var x=new ExampleKillQuest();x.Events=new OfflineEvents(true){DebugEnabled=false};x.Entities=new KillWorld{IndexComplete=true};x.Entities.Entities.Add(17,new EntitySnapshot{Kind=EntityKind.Player});x.Entities.Entities.Add(88,new EntitySnapshot{Kind=EntityKind.Monster,RawByte20A4=0});
  var created=KillCondition.Create(1,3,required,new[]{"ming/enemies/tainted_soldier.dbr"},false,x.Events,x.Entities,out x.Counter);if(created.Status!="Known")throw new Exception(created.Reason);x.Counter.SystemTimeSnapshot=0;x.Counter.PureLocalizationContextKnown=true;
  var world=new World{QuestIndexComplete=true,MainPlayerResolved=true,ServerServiceSlot18NonNull=false};world.Core.Difficulty=0;x.Predicate=new Predicate{TypeId=10,Version=1,ComparisonId=3,Value=required};var group=new Group{Operation=0};group.Conditions.Add(x.Predicate);x.Goal=new Objective{Uid=1,ConditionsKnown=true,Conditions=group};var task=new TaskState{Uid=1,State=1};task.Objectives.Add(0,new List<Objective>{x.Goal});var quest=new QuestState();quest.Tasks.Add(task);world.Quests.Add("ming/border_patrol",quest);
  var r=new RuntimeSnapshot{State=new TaskMutationSnapshot{Task=task,Difficulty=0,QuestFlags=0,OnAcceptEventCount=0,StatesByDifficulty=new[]{1,1,1}},World=world,QuestReference="ming/border_patrol",DontPropagateByte=1,CompletionRecorded=new bool[3],Outbound=new OutboundSnapshot{ProviderIsKnownOutbound=true}};r.ObjectiveCounters.Add(x.Goal,0);
  x.Auto=new AutoSnapshot{Runtime=r,QuestUid=2,Events=x.Events,OnCompleteEventCount=0,RewardRecordedA3=new bool[3],MainPlayerFlags17F4=0};x.Auto.ConditionRuntimes.Add(x.Predicate,x.Counter);x.Auto.ObjectiveActionCounts.Add(x.Goal,0);return x;
 }
 public DispatchResult Kill(){return Events.Send(new QuestEvent{Kind=11,Body=new KillEventBody{SourceId=17,VictimId=88,RecordName="ming/enemies/tainted_soldier.dbr"}},11);}
}
