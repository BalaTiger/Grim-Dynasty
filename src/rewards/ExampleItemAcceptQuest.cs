using System;
using System.Collections.Generic;
using GrimDynasty.Rewards;
public sealed class ExampleItemAcceptQuest {
 public ExampleItemQuest Quest;
 public AcceptActions Accept;
 public MoneyCompletion Completion;
 public WalletSnapshot Wallet;
 public RewardQuestSnapshot Record;
 public static ExampleItemAcceptQuest Create(int declaredQuantity){
  var x=new ExampleItemAcceptQuest();x.Quest=ExampleItemQuest.Create();x.Quest.Stock.Objects[100].QuantityFromSlot618=declaredQuantity;var a=x.Quest.Auto;
  var d=OwnItemAcceptFixture.Read();var task=(Dictionary<string,object>)((List<object>)d["tasks"])[0];List<AcceptEvent> events;
  var read=AcceptDefinitionAdapter.ReadWithItems(task,1,a.Events,x.Quest.Stock,out events);if(read.Status!="Known")throw new Exception(read.Reason);
  x.Wallet=new WalletSnapshot{MainPlayerResolved=true,StaticMoneyNameReady=true,Money16C4=50};x.Record=new RewardQuestSnapshot{Quest=a.Runtime.World.Quests[a.Runtime.QuestReference],RewardTaskUidsAt50=new List<uint>()};
  x.Accept=new AcceptActions{Events=events,Wallet=x.Wallet,QuestUidIndexComplete=true};x.Accept.QuestsByUid.Add(a.QuestUid.Value,x.Record);a.AcceptRuntime=x.Accept;a.Runtime.State.OnAcceptEventCount=events.Count;
  x.Completion=new MoneyCompletion{Wallet=x.Wallet,QuestUidIndexComplete=true,Events=new List<MoneyCompletionEvent>{new MoneyCompletionEvent{ConditionsKnown=true,ConditionsAreNull=true,Actions=new List<MoneyAction>{new MoneyAction{Amount=75}}}}};x.Completion.QuestsByUid.Add(a.QuestUid.Value,x.Record);a.CompletionRuntime=x.Completion;a.OnCompleteEventCount=1;
  return x;
 }
}
