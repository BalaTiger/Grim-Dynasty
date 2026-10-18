using System;
using System.Collections.Generic;
using GrimDynasty.Rewards;
using GrimDynasty.TaskItems;
using GrimDynasty.TaskLogic;
public sealed class ExampleDeliveryQuest {
 public ExampleItemQuest Quest;
 public InventoryTakeContext Inventory;
 public ItemObject Item;
 public WalletSnapshot Wallet;
 public RewardQuestSnapshot Record;
 public static ExampleDeliveryQuest Create(){
  var x=new ExampleDeliveryQuest();x.Quest=ExampleItemQuest.Create();var a=x.Quest.Auto;var d=OwnDeliveryFixture.Read();var t=(Dictionary<string,object>)((List<object>)d["tasks"])[0];var objective=(Dictionary<string,object>)((List<object>)t["objectives"])[0];var conditions=(Dictionary<string,object>)objective["conditions"];var row=(Dictionary<string,object>)((List<object>)conditions["conditions"])[0];Predicate p;HasItemCondition c;
  var read=HasItemDefinitionAdapter.Read(row,a.Events,x.Quest.Stock,out p,out c);if(read.Status!="Known")throw new Exception(read.Reason);c.SystemTimeSnapshot=0;c.PureLocalizationContextKnown=true;
  x.Quest.Condition=c;x.Quest.Goal.Conditions=new Group{Operation=0};x.Quest.Goal.Conditions.Conditions.Add(p);a.ConditionRuntimes.Clear();a.ConditionRuntimes.Add(p,c);
  x.Item=x.Quest.Stock.Objects[100];x.Item.QuantityFromSlot618=999;x.Item.StackMethods=ItemStackMethods.DirectItemFields88C6B0;x.Item.RawStack88C=3;x.Item.RawReplicaStack6B0=99;x.Item.IsRelic=false;
  x.Inventory=new InventoryTakeContext{Stock=x.Quest.Stock,Events=a.Events,CharacterObjectId=1,CoveredConditions=new List<HasItemCondition>{c}};
  var completion=(Dictionary<string,object>)((List<object>)t["on_complete"])[0];var action=(Dictionary<string,object>)((List<object>)completion["actions"])[0];var parameters=(Dictionary<string,object>)action["parameters"];
  x.Wallet=new WalletSnapshot{MainPlayerResolved=true,StaticMoneyNameReady=true,Money16C4=50};x.Record=new RewardQuestSnapshot{Quest=a.Runtime.World.Quests[a.Runtime.QuestReference],RewardTaskUidsAt50=new List<uint>()};
  var reward=new MoneyCompletion{Wallet=x.Wallet,QuestUidIndexComplete=true,Events=new List<MoneyCompletionEvent>{new MoneyCompletionEvent{ConditionsKnown=true,ConditionsAreNull=true,Actions=new List<MoneyAction>{new MoneyAction{Version=Convert.ToInt32(action["version"]),Amount=Convert.ToInt32(parameters["amount"])}}}}};reward.QuestsByUid.Add(a.QuestUid.Value,x.Record);a.CompletionRuntime=reward;a.OnCompleteEventCount=1;
  return x;
 }
}
