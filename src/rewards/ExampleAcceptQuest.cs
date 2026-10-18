using System;
using System.Collections.Generic;
using GrimDynasty.Rewards;
public sealed class ExampleAcceptQuest {
 public ExampleRewardQuest Reward;
 public AcceptActions Accept;
 public TutorialSnapshot Tutorials;
 public static ExampleAcceptQuest Create(){
  var x=new ExampleAcceptQuest();x.Reward=ExampleRewardQuest.Create(75);var a=x.Reward.Quest.Auto;
  var definition=OwnAcceptQuestFixture.Read();var task=(Dictionary<string,object>)((List<object>)definition["tasks"])[0];List<AcceptEvent> events;
  var decoded=AcceptDefinitionAdapter.Read(task,a.Runtime.State.Task.Uid,out events);if(decoded.Status!="Known")throw new Exception(decoded.Reason);
  x.Tutorials=new TutorialSnapshot{PageCount36CD0=75,UnlockWords36CB8=new uint[3],UnlockOrder36CA0=new List<uint>(),Events=a.Events};
  x.Accept=new AcceptActions{Events=events,Wallet=x.Reward.Completion.Wallet,Tutorials=x.Tutorials,QuestUidIndexComplete=true};x.Accept.QuestsByUid.Add(a.QuestUid.Value,x.Reward.Record);
  a.Runtime.State.QuestFlags=Convert.ToUInt32(definition["flags"]);a.Runtime.State.OnAcceptEventCount=events.Count;a.TutorialRuntime=new TutorialTaskStart{State=x.Tutorials};a.AcceptRuntime=x.Accept;
  return x;
 }
}
