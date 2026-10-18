using System.Collections.Generic;
using GrimDynasty.Rewards;
public sealed class ExampleExperienceQuest {
 public ExampleKillQuest Quest;
 public ExperienceCompletion Completion;
 public ExperienceActor Player;
 public static ExampleExperienceQuest Create(uint amount){
  var x=new ExampleExperienceQuest();x.Quest=ExampleKillQuest.Create(2);
  x.Player=new ExperienceActor{ObjectId=17,IsPlayer=true,IsAlive=true,DevotionSkillVectorEmpty=true,Experience1764=100,Level1760=4,MaximumLevel175C=100,Lifetime1968=200,EquationInput1710=-1,ExperienceGainPercent=0,NextThreshold=new ThresholdSample{Input=4,FormulaResult=1000},Bio=new ZeroBonusBio{GetterResultsStable=true,Life=100,MaximumLife=150,ReserveLifeCap=150,Energy=40,MaximumEnergy=50,Stamina=30,MaximumStamina=50,HealingPercent=0,PercentHealingPercent=0,DirtyGate16B1=0,LifeStatsIndexComplete=true}};
  var w=new ExperienceWorld{ExperienceNameCache=new NameCacheSnapshot(),MainPlayerQueryKnown=true,MainPlayerId=17,ClientManagerResolved=true,ClientPlayerId=17,EntityIndexComplete=true,Route=ExperienceRoute.LocalInbound};w.Actors.Add(17,x.Player);
  x.Completion=new ExperienceCompletion{World=w,Events=new List<ExperienceEvent>{new ExperienceEvent{Gate=new NumericEvent{ConditionsKnown=true},Actions=new List<ExperienceAction>{new ExperienceAction{Amount=amount}}}},QuestUidIndexComplete=true};
  var a=x.Quest.Auto;x.Completion.QuestsByUid.Add(a.QuestUid.Value,new RewardQuestSnapshot{Quest=a.Runtime.World.Quests[a.Runtime.QuestReference],RewardTaskUidsAt50=new List<uint>()});a.CompletionRuntime=x.Completion;a.OnCompleteEventCount=1;return x;
 }
}
