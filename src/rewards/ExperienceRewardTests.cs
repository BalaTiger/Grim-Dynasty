using System;
using System.Collections.Generic;
using GrimDynasty.Rewards;
using GrimDynasty.TaskAuto;
class ExperienceRewardTests {
 static int n;static void Need(bool b,string s){n++;if(!b)throw new Exception(s);}
 static void Last(ExampleExperienceQuest x){Need(AutomaticTasks.Start(x.Quest.Auto).Status=="Known","start");AutomaticTasks.Update(x.Quest.Auto);x.Quest.Kill();AutomaticTasks.Update(x.Quest.Auto);x.Quest.Kill();}
 static void Main(){
  var x=ExampleExperienceQuest.Create(75);var w=x.Completion.World;var p=x.Player;
  Need(ExperienceRewards.Execute(w,new ExperienceAction{Amount=75}).Status=="Known"&&p.Experience1764==175&&p.Lifetime1968==275,"local XP and lifetime writes");
  Need(p.Level1760==4&&p.EquationInput1710==4,"no level change but formula input written");
  Need(w.ExperienceNameCache.Initialized&&w.ExperienceNameCache.Value==0xcb999846,"XP cold Name initialized");
  Need(w.DispatchTrace.Count==1&&w.DispatchTrace[0].TargetId==17&&w.DispatchTrace[0].Amount==75&&w.DispatchTrace[0].ExperienceSource==2&&w.DispatchTrace[0].OriginId==0&&w.DispatchTrace[0].OtherFieldsZero,"exact XP Bonus shape");
  Need(p.Bio.Life==100&&p.Bio.Energy==40&&p.Bio.Stamina==30&&p.Bio.LifeStats[0]==0,"zero bonus bio and stats row");
  p.Bio.DirtyGate16B1=1;ExperienceRewards.Execute(w,new ExperienceAction{Amount=1});Need(p.Bio.Dirty1698==1,"bio dirty gate write");
  x=ExampleExperienceQuest.Create(0);w=x.Completion.World;p=x.Player;p.Experience1764=null;p.NextThreshold=null;p.ExperienceGainPercent=null;p.DevotionSkillVectorEmpty=null;Need(ExperienceRewards.Execute(w,new ExperienceAction()).Status=="Known"&&p.Bio.LifeStats.ContainsKey(0),"zero skips ReceiveExperience but still processes zero Bio");
  x=ExampleExperienceQuest.Create(1);w=x.Completion.World;p=x.Player;p.Experience1764=uint.MaxValue;p.Lifetime1968=uint.MaxValue;Need(ExperienceRewards.Execute(w,new ExperienceAction{Amount=1}).Status=="Known"&&p.Experience1764==0&&p.Lifetime1968==0,"both counters wrap");
  x=ExampleExperienceQuest.Create(75);w=x.Completion.World;p=x.Player;p.NextThreshold.FormulaResult=175.49f;Need(ExperienceRewards.Execute(w,new ExperienceAction{Amount=75}).Status=="Unsupported"&&p.Experience1764==100&&!w.ExperienceNameCache.Initialized,"rounding and exact threshold reject before writes");
  p.NextThreshold.FormulaResult=175.5f;Need(ExperienceRewards.Execute(w,new ExperienceAction{Amount=75}).Status=="Known"&&p.Experience1764==175,"single precision half rounds threshold upward");
  x=ExampleExperienceQuest.Create(75);w=x.Completion.World;w.Route=ExperienceRoute.NetworkOutbound;Need(ExperienceRewards.Execute(w,new ExperienceAction{Amount=75}).Status=="Unsupported"&&x.Player.Experience1764==100&&!w.ExperienceNameCache.Initialized&&w.DispatchTrace.Count==0,"network interface rejected atomically");
  w.Route=ExperienceRoute.Unknown;Need(ExperienceRewards.Execute(w,new ExperienceAction{Amount=75}).Status=="Unsupported","unknown interface");
  x=ExampleExperienceQuest.Create(75);w=x.Completion.World;w.ClientPlayerId=0;w.MainPlayerId=0;w.Actors.Clear();x.Player.ObjectId=0;w.Actors.Add(0,x.Player);Need(ExperienceRewards.Execute(w,new ExperienceAction{Amount=75}).Status=="Known"&&x.Player.Experience1764==100&&w.DispatchTrace.Count==1,"origin zero equals client zero suppresses bonus");
  x=ExampleExperienceQuest.Create(75);w=x.Completion.World;w.ClientManagerResolved=false;w.ClientPlayerId=null;Need(ExperienceRewards.CharacterExperienceOutbound(w,17,75).Status=="Known"&&x.Player.Experience1764==100&&!w.ExperienceNameCache.Initialized,"direct outbound missing inbound manager noop");
  Need(ExperienceRewards.Execute(w,new ExperienceAction{Amount=75}).Status=="Unsupported"&&!w.ExperienceNameCache.Initialized,"script action requires stable client identity");
  x=ExampleExperienceQuest.Create(75);w=x.Completion.World;w.MainPlayerId=null;Need(ExperienceRewards.Execute(w,new ExperienceAction{Amount=75}).Status=="Known"&&w.ExperienceNameCache.Initialized&&w.DispatchTrace.Count==0,"no main player still initializes Name");
  x=ExampleExperienceQuest.Create(75);w=x.Completion.World;p=x.Player;p.IsPlayer=false;Need(ExperienceRewards.Execute(w,new ExperienceAction{Amount=75}).Status=="Known"&&p.Experience1764==100&&w.DispatchTrace.Count==0,"nonplayer main query noop");
  x=ExampleExperienceQuest.Create(75);w=x.Completion.World;p=x.Player;p.IsAlive=false;p.Bio=null;Need(ExperienceRewards.Execute(w,new ExperienceAction{Amount=75}).Status=="Known"&&p.Experience1764==175,"source 2 XP precedes alive gate");
  x=ExampleExperienceQuest.Create(75);w=x.Completion.World;p=x.Player;p.DevotionSkillVectorEmpty=false;Need(ExperienceRewards.Execute(w,new ExperienceAction{Amount=75}).Status=="Unsupported"&&p.Experience1764==100,"devotion XP path not silently skipped");
  p.DevotionSkillVectorEmpty=true;p.ExperienceGainPercent=1;Need(ExperienceRewards.Execute(w,new ExperienceAction{Amount=75}).Status=="Unsupported"&&p.Experience1764==100,"modifier outside domain");
  p.ExperienceGainPercent=0;p.MaximumLevel175C=4;Need(ExperienceRewards.Execute(w,new ExperienceAction{Amount=75}).Status=="Unsupported","level cap outside domain");
  x=ExampleExperienceQuest.Create(75);w=x.Completion.World;p=x.Player;p.Bio.Life=200;Need(ExperienceRewards.Execute(w,new ExperienceAction{Amount=75}).Status=="Unsupported"&&p.Experience1764==100,"zero bonus clamps cannot be dropped outside domain");
  x=ExampleExperienceQuest.Create(75);w=x.Completion.World;p=x.Player;p.Bio.LifeStatsIndexComplete=false;Need(ExperienceRewards.Execute(w,new ExperienceAction{Amount=75}).Status=="Unsupported"&&p.Experience1764==100,"unknown stats index");
  p.Bio.LifeStats.Add(0,3);Need(ExperienceRewards.Execute(w,new ExperienceAction{Amount=75}).Status=="Known"&&p.Bio.LifeStats[0]==3,"existing stats row does not need complete index");
  x=ExampleExperienceQuest.Create(75);Last(x);Need(AutomaticTasks.Update(x.Quest.Auto).Status=="Known"&&x.Quest.Auto.Runtime.State.Task.State==3&&x.Player.Experience1764==175,"automatic quest to real own XP write");Need(x.Completion.QuestsByUid[2].RewardTaskUidsAt50.Count==1&&x.Quest.Auto.RewardRecordedA3[0]&&!x.Quest.Counter.CallbackOwnerAttached,"XP reward record and end lifecycle");
  x=ExampleExperienceQuest.Create(950);Last(x);Need(AutomaticTasks.Update(x.Quest.Auto).Status=="Unsupported"&&x.Quest.Auto.Runtime.State.Task.State==2&&x.Player.Experience1764==100&&!x.Completion.World.ExperienceNameCache.Initialized&&x.Completion.QuestsByUid[2].RewardTaskUidsAt50.Count==0,"level crossing rejects completion atomically");
  x=ExampleExperienceQuest.Create(500);x.Completion.Events[0].Actions.Add(new ExperienceAction{Amount=500});Last(x);Need(AutomaticTasks.Update(x.Quest.Auto).Status=="Unsupported"&&x.Player.Experience1764==100&&!x.Completion.World.ExperienceNameCache.Initialized,"later action failure prevents earlier award");
  Console.WriteLine("Experience reward assertions passed: "+n);
 }
}
