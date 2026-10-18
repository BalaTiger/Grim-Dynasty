using System.Collections.Generic;
using GrimDynasty.Rewards;
public sealed class ExampleRewardQuest {
 public ExampleKillQuest Quest;public MoneyCompletion Completion;public RewardQuestSnapshot Record;
 public static ExampleRewardQuest Create(params int[] amounts){
  var x=new ExampleRewardQuest();x.Quest=ExampleKillQuest.Create(2);x.Completion=new MoneyCompletion{Wallet=new WalletSnapshot{MainPlayerResolved=true,StaticMoneyNameReady=true,Money16C4=50},Events=new List<MoneyCompletionEvent>(),QuestUidIndexComplete=true};
  var actions=new List<MoneyAction>();foreach(int amount in amounts)actions.Add(new MoneyAction{Amount=amount});x.Completion.Events.Add(new MoneyCompletionEvent{ConditionsKnown=true,ConditionsAreNull=true,Actions=actions});
  var a=x.Quest.Auto;x.Record=new RewardQuestSnapshot{Quest=a.Runtime.World.Quests[a.Runtime.QuestReference],RewardTaskUidsAt50=new List<uint>()};x.Completion.QuestsByUid.Add(a.QuestUid.Value,x.Record);a.OnCompleteEventCount=x.Completion.Events.Count;a.CompletionRuntime=x.Completion;return x;
 }
}
