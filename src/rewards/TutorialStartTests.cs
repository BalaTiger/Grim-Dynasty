using System;
using System.Collections.Generic;
using GrimDynasty.Rewards;
using GrimDynasty.TaskAuto;
class TutorialStartTests {
 static int n;static void Need(bool b,string s){n++;if(!b)throw new Exception(s);}
 static ExampleExperienceQuest Quest(){var x=ExampleLevelUpQuest.Create();x.Quest.Auto.Runtime.State.QuestFlags=4;x.Completion.World.Tutorials=new TutorialSnapshot{PageCount36CD0=75,UnlockWords36CB8=new uint[3],UnlockOrder36CA0=new List<uint>(),Events=x.Quest.Events};x.Quest.Auto.TutorialRuntime=new TutorialTaskStart{State=x.Completion.World.Tutorials};return x;}
 static void Main(){
  var x=Quest();var a=x.Quest.Auto;var s=x.Completion.World.Tutorials;
  Need(AutomaticTasks.Start(a).Status=="Known"&&a.Runtime.State.Task.State==2&&s.UnlockOrder36CA0[0]==55&&s.UnlockWords36CB8[1]==(1u<<23)&&x.Quest.Counter.CallbackOwnerAttached,"start tutorial then activates and registers goal");
  Need(AutomaticTasks.Start(a).Status=="Known"&&s.Produced.Count==1&&s.UnlockOrder36CA0.Count==1,"repeated active start skips tutorial");
  x=Quest();a=x.Quest.Auto;s=x.Completion.World.Tutorials;a.Runtime.State.Task.RuntimeBlocked=1;a.TutorialRuntime=null;Need(AutomaticTasks.Start(a).Status=="Known"&&a.Runtime.State.Task.State==2&&!x.Quest.Counter.CallbackOwnerAttached&&s.Produced.Count==0,"blocked start skips tutorial and hooks");
  x=Quest();a=x.Quest.Auto;s=x.Completion.World.Tutorials;a.Runtime.State.OnAcceptEventCount=1;Need(AutomaticTasks.Start(a).Status=="Unsupported"&&a.Runtime.State.Task.State==1&&s.UnlockOrder36CA0.Count==0&&!x.Quest.Counter.CallbackOwnerAttached,"unknown onAccept rejects before tutorial");
  x=Quest();a=x.Quest.Auto;s=x.Completion.World.Tutorials;x.Quest.Events.Register(0x36,new OfflineHandler{Invoke=e=>{}});Need(AutomaticTasks.Start(a).Status=="Unsupported"&&a.Runtime.State.Task.State==1&&s.UnlockWords36CB8[1]==0&&!x.Quest.Counter.CallbackOwnerAttached,"tutorial subscriber failure preserves task and lifecycle");
  x=Quest();a=x.Quest.Auto;s=x.Completion.World.Tutorials;x.Quest.Goal.ConditionsKnown=false;Need(AutomaticTasks.Start(a).Status=="Unsupported"&&s.UnlockOrder36CA0.Count==0&&a.Runtime.State.Task.State==1,"later objective validation failure leaves tutorial unchanged");
  x=Quest();a=x.Quest.Auto;s=x.Completion.World.Tutorials;s.PageCount36CD0=55;Need(AutomaticTasks.Start(a).Status=="Known"&&s.Produced.Count==0&&a.Runtime.State.Task.State==2,"tutorial upper bound no-op still starts task");
  x=Quest();a=x.Quest.Auto;s=x.Completion.World.Tutorials;s.UnlockWords36CB8[1]=1u<<23;s.Events=null;Need(AutomaticTasks.Start(a).Status=="Known"&&s.Produced.Count==0,"already unlocked start page needs no event route");
  x=Quest();a=x.Quest.Auto;s=x.Completion.World.Tutorials;a.TutorialRuntime=null;Need(AutomaticTasks.Start(a).Status=="Unsupported"&&s.UnlockOrder36CA0.Count==0,"missing explicit adapter remains unsupported");
  x=Quest();a=x.Quest.Auto;s=x.Completion.World.Tutorials;a.Runtime.State.QuestFlags=0;a.TutorialRuntime=null;Need(AutomaticTasks.Start(a).Status=="Known"&&s.Produced.Count==0,"unflagged quest needs no tutorial context");
  Console.WriteLine("Tutorial start assertions passed: "+n);
 }
}
