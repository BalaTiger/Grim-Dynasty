using System;
using System.Collections.Generic;
using GrimDynasty.Rewards;
using GrimDynasty.TaskItems;
using GrimDynasty.TaskLogic;
using GrimDynasty.TaskRuntime;
using GrimDynasty.TaskAuto;
using GrimDynasty.TokenActions;
public sealed class ExampleInventoryCycle {
 public ExampleDeliveryQuest Base;
 public AutoSnapshot[] Tasks;
 public HasItemCondition[] Conditions;
 public InventoryAddContext Add;
 public static ExampleInventoryCycle Create(){
  var x=new ExampleInventoryCycle();x.Base=ExampleFullDelivery.Create(3);var world=x.Base.Quest.Auto.Runtime.World;var bus=x.Base.Quest.Events;
  x.Base.Item.AddMethods=ItemAddMethods.BaseItemObjectTable;x.Base.Item.AddHook0CallbackPresent=false;x.Base.Item.RawGender874=0;
  var d=OwnInventoryCycleFixture.Read();var rows=(List<object>)d["tasks"];x.Tasks=new AutoSnapshot[rows.Count];x.Conditions=new HasItemCondition[rows.Count];x.Base.Record.Quest.Tasks.Clear();
  for(int i=0;i<rows.Count;i++){
   var t=(Dictionary<string,object>)rows[i];var objective=(Dictionary<string,object>)((List<object>)t["objectives"])[0];var conditions=(Dictionary<string,object>)objective["conditions"];var row=(Dictionary<string,object>)((List<object>)conditions["conditions"])[0];Predicate p;HasItemCondition condition;
   var read=HasItemDefinitionAdapter.Read(row,bus,x.Base.Quest.Stock,out p,out condition);if(read.Status!="Known")throw new Exception(read.Reason);condition.SystemTimeSnapshot=0;condition.PureLocalizationContextKnown=true;x.Conditions[i]=condition;
   var goal=new Objective{Uid=1,ConditionsKnown=true,Conditions=new Group{Operation=0}};goal.Conditions.Conditions.Add(p);var state=new TaskState{Uid=Convert.ToUInt32(t["uid"]),State=1};state.Objectives.Add(0,new List<Objective>{goal});x.Base.Record.Quest.Tasks.Add(state);
   var runtime=new RuntimeSnapshot{State=new TaskMutationSnapshot{Task=state,Difficulty=0,QuestFlags=0,OnAcceptEventCount=0,StatesByDifficulty=new[]{1,1,1}},World=world,QuestReference=x.Base.Quest.Auto.Runtime.QuestReference,DontPropagateByte=1,CompletionRecorded=new bool[3],Outbound=new OutboundSnapshot{ProviderIsKnownOutbound=true}};runtime.ObjectiveCounters.Add(goal,0);
   var a=new AutoSnapshot{Runtime=runtime,QuestUid=3,Events=bus,OnCompleteEventCount=1,RewardRecordedA3=new bool[3],MainPlayerFlags17F4=0};a.ConditionRuntimes.Add(p,condition);a.ObjectiveActionCounts.Add(goal,0);x.Tasks[i]=a;
   var complete=(Dictionary<string,object>)((List<object>)t["on_complete"])[0];var action=(Dictionary<string,object>)((List<object>)complete["actions"])[0];var values=(Dictionary<string,object>)action["parameters"];
   var reward=new MoneyCompletion{Wallet=x.Base.Wallet,QuestUidIndexComplete=true,Events=new List<MoneyCompletionEvent>{new MoneyCompletionEvent{ConditionsKnown=true,ConditionsAreNull=true,Actions=new List<MoneyAction>{new MoneyAction{Version=Convert.ToInt32(action["version"]),Amount=Convert.ToInt32(values["amount"])}}}}};reward.QuestsByUid.Add(3,x.Base.Record);a.CompletionRuntime=reward;
  }
  x.Base.Quest.Auto=x.Tasks[0];x.Base.Quest.Condition=x.Conditions[0];x.Base.Quest.Goal=x.Tasks[0].Runtime.State.Task.Objectives[0][0];x.Base.Inventory.CoveredConditions=new List<HasItemCondition>(x.Conditions);
  x.Add=new InventoryAddContext{Stock=x.Base.Quest.Stock,Events=bus,InventoryOwner20=1,InventoryGender24=1,Engine=new InventoryAddEngine{ClientPlayerId=1,ClientGetterStable=true,ItemAddCount1C=7},CoveredConditions=new List<HasItemCondition>(x.Conditions),SharedRemovalContext=x.Base.Inventory};return x;
 }
}
