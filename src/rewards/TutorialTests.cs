using System;
using System.Collections.Generic;
using GrimDynasty.Rewards;
using GrimDynasty.TaskAuto;
class TutorialTests {
 static int n;static void Need(bool b,string s){n++;if(!b)throw new Exception(s);}
 static TutorialSnapshot State(){return new TutorialSnapshot{PageCount36CD0=75,UnlockWords36CB8=new uint[3],UnlockOrder36CA0=new List<uint>(),Events=new OfflineEvents(true){DebugEnabled=false}};}
 static ExampleExperienceQuest Quest(){var x=ExampleLevelUpQuest.Create();x.Player.Level1760=9;x.Quest.Auto.Runtime.World.Level=9;x.Player.Progression.FormulaSamples[9]=60;x.Player.Progression.FormulaSamples[10]=160;x.Completion.World.LevelDisplay.LocalizedReachedText[10]="升至十级";x.Completion.World.Tutorials=State();x.Completion.World.Tutorials.Events=x.Completion.World.Events;return x;}
 static void Last(ExampleExperienceQuest x){AutomaticTasks.Start(x.Quest.Auto);AutomaticTasks.Update(x.Quest.Auto);x.Quest.Kill();AutomaticTasks.Update(x.Quest.Auto);x.Quest.Kill();}
 static void Main(){
  var s=State();Need(TutorialRules.CheckUnlock(s,20,true).Status=="Known"&&s.UnlockWords36CB8[0]==0&&s.Produced.Count==0&&s.UnlockOrder36CA0.Count==0,"preflight copies all tutorial values");
  Need(TutorialRules.ExecuteAction(s,20).Status=="Known"&&s.UnlockWords36CB8[0]==(1u<<20)&&s.UnlockOrder36CA0[0]==20,"first unlock mutates bit and ordered vector");
  Need(s.Produced.Count==1&&s.Produced[0].Route==0x36&&s.Produced[0].Payload.Kind==0x36&&((TutorialNotification)s.Produced[0].Payload.Body).Page==20,"unlock event page payload");
  s.Events=null;s.UnlockOrder36CA0=null;Need(TutorialRules.Unlock(s,20,true).Changed==false&&s.Produced.Count==1,"duplicate skips vector and notification dependencies");
  s=State();s.UnlockOrder36CA0=null;s.UnlockWords36CB8=null;s.Events=null;Need(TutorialRules.Unlock(s,75,true).Changed==false&&TutorialRules.Unlock(s,uint.MaxValue,true).Changed==false,"unsigned out of range skips downstream fields");
  s=State();s.Events=null;foreach(uint page in new uint[]{0,31,32,63,64,74})Need(TutorialRules.Unlock(s,page,false).Status=="Known","word boundary silent unlock");Need(s.UnlockWords36CB8[0]==0x80000001&&s.UnlockWords36CB8[1]==0x80000001&&s.UnlockWords36CB8[2]==0x401&&s.Produced.Count==0,"bit31 and cross-word state preserve order");
  s=State();s.PageCount36CD0=null;Need(TutorialRules.Unlock(s,0,false).Status=="Unsupported","unknown count rejected");
  s=State();s.UnlockWords36CB8=new uint[1];Need(TutorialRules.Unlock(s,32,false).Status=="Unsupported"&&s.UnlockOrder36CA0.Count==0,"partial bit word rejected");
  s=State();s.Events.Register(0x36,new OfflineHandler{Invoke=e=>{throw new Exception("must not execute");}});Need(TutorialRules.Unlock(s,20,true).Status=="Unsupported"&&s.UnlockWords36CB8[0]==0&&s.UnlockOrder36CA0.Count==0,"unrecovered handler fails before writes");
  Need(TutorialRules.Unlock(s,20,false).Status=="Known"&&s.Produced.Count==0,"false notify avoids unresolved route");
  s=State();s.Events.Register(0x36,new OfflineHandler{Invoke=e=>{throw new Exception("wrong route");}});s.UnlockWords36CB8=null;s.UnlockOrder36CA0=null;Need(TutorialRules.Show(s,20).Status=="Known"&&s.Produced.Count==1&&s.Produced[0].Route==0x37&&s.Produced[0].Payload.Kind==0x36,"show uses route37 with payload36 and no unlock dependencies");
  Need(TutorialRules.Show(s,20).Status=="Known"&&s.Produced.Count==2,"show repeats even while not unlocked");
  s=State();s.Events.Register(0x37,new OfflineHandler{Invoke=e=>{}});Need(TutorialRules.Show(s,20).Status=="Unsupported"&&s.Produced.Count==0,"show subscriber remains explicit unsupported");
  var x=Quest();var w=x.Completion.World;var p=x.Player;Need(ExperienceRewards.Execute(w,new ExperienceAction{Amount=75}).Status=="Known"&&p.Level1760==10&&w.Tutorials.UnlockOrder36CA0.Count==1,"level10 closes unlock branch");
  Need(w.ProducedLevelEvents.Count==3&&w.ProducedLevelEvents[0].Kind==0x36&&w.ProducedLevelEvents[1].Kind==0x31&&w.ProducedLevelEvents[2].Kind==0x30,"tutorial before skill before final level notification");
  x=Quest();w=x.Completion.World;p=x.Player;w.Tutorials.UnlockWords36CB8[0]=1u<<20;Need(ExperienceRewards.Execute(w,new ExperienceAction{Amount=75}).Status=="Known"&&w.Tutorials.Produced.Count==0&&w.ProducedLevelEvents.Count==2,"already unlocked level10 does not notify again");
  x=Quest();w=x.Completion.World;p=x.Player;p.Level1760=8;p.Progression.FormulaSamples[8]=30;p.Progression.FormulaSamples[11]=300;w.LevelDisplay.LocalizedReachedText[11]="十一级";Need(ExperienceRewards.Execute(w,new ExperienceAction{Amount=200}).Status=="Known"&&p.Level1760==11&&w.ProducedLevelEvents.Count==5&&w.ProducedLevelEvents[0].Kind==0x31&&w.ProducedLevelEvents[1].Kind==0x36&&w.ProducedLevelEvents[2].Kind==0x31,"multi-level preserves tutorial/skill interleaving");
  x=Quest();w=x.Completion.World;p=x.Player;w.Events.Register(0x36,new OfflineHandler{Invoke=e=>{}});Need(ExperienceRewards.Execute(w,new ExperienceAction{Amount=75}).Status=="Unsupported"&&p.Level1760==9&&p.Experience1764==50&&w.Tutorials.UnlockWords36CB8[0]==0&&!w.ExperienceNameCache.Initialized,"tutorial callback rejection atomic with experience");
  x=Quest();w=x.Completion.World;p=x.Player;x.Completion.Events[0].Actions.Add(new ExperienceAction{Amount=500});Last(x);Need(AutomaticTasks.Update(x.Quest.Auto).Status=="Unsupported"&&p.Level1760==9&&w.Tutorials.UnlockOrder36CA0.Count==0&&w.Tutorials.Produced.Count==0&&x.Quest.Auto.Runtime.State.Task.State==2,"later reward failure rolls back tutorial preview");
  x=Quest();Last(x);Need(AutomaticTasks.Update(x.Quest.Auto).Status=="Known"&&x.Player.Level1760==10&&x.Completion.World.Tutorials.UnlockOrder36CA0.Count==1&&x.Completion.QuestsByUid[2].RewardTaskUidsAt50.Count==1,"quest completion unlock plus reward history");
  Console.WriteLine("Tutorial assertions passed: "+n);
 }
}
