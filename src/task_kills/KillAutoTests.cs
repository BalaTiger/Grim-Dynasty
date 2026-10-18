using System;
using GrimDynasty.TaskLogic;
using GrimDynasty.TaskAuto;
public static class KillAutoTests {
 static int n;static void Is(bool b,string s){n++;if(!b)throw new Exception(s);}
 public static int Main(){try{
  var x=ExampleKillQuest.Create(2);var a=x.Auto;var r=a.Runtime;
  Is(AutomaticTasks.Start(a).Status=="Known"&&r.State.Task.State==2&&x.Counter.CallbackOwnerAttached,"start registers kill handler");Is(AutomaticTasks.Update(a).NativeReturn==true&&r.ObjectiveCounters[x.Goal]==2,"first tick pending");
  Is(x.Kill().Delivered==1&&x.Counter.Current==1&&x.Counter.ChangedByte14==1,"first kill event");var first=AutomaticTasks.Update(a);Is(first.Status=="Known"&&first.NativeReturn==true&&r.State.Task.State==2&&r.ObjectiveCounters[x.Goal]==2,"partial kill progress not goal complete");Is(first.Emitted[0].FlagC==0&&x.Counter.ChangedByte14==0&&x.Counter.TextByte15==1,"dirty clear and partial progress notification");
  Is(x.Kill().Delivered==1&&x.Counter.Current==2,"second kill");var second=AutomaticTasks.Update(a);Is(second.Status=="Known"&&r.State.Task.State==3&&r.ObjectiveCounters[x.Goal]==1&&a.RewardRecordedA3[0],"kill threshold automatically completes");Is(second.Emitted[0].FlagC==1&&!x.Counter.CallbackOwnerAttached,"completion notification and end hook");Is(x.Kill().Delivered==0&&x.Counter.Current==2,"completed task unsubscribed");
  Is(AutomaticTasks.Reset(a).Status=="Known"&&r.State.Task.State==1&&x.Counter.Current==0&&r.ObjectiveCounters[x.Goal]==0,"full reset with nonnoop condition reset");Is(a.RewardRecordedA3[0],"reset preserves history");Is(AutomaticTasks.Start(a).Status=="Known"&&x.Counter.CallbackOwnerAttached,"restart reattaches same handler");
  x=ExampleKillQuest.Create(2);a=x.Auto;r=a.Runtime;AutomaticTasks.Start(a);AutomaticTasks.Update(a);x.Kill();x.Counter.SystemTimeSnapshot=4001;var blocked=AutomaticTasks.Update(a);Is(blocked.Status=="Unsupported"&&x.Counter.Current==1&&x.Counter.ChangedByte14==1&&r.ObjectiveCounters[x.Goal]==2&&r.State.Task.State==2,"UI dependency rejects update without swallowing existing event progress");x.Counter.SystemTimeSnapshot=0;Is(AutomaticTasks.Update(a).Status=="Known"&&x.Counter.ChangedByte14==0,"resume with supported clock context");
  x=ExampleKillQuest.Create(1);a=x.Auto;r=a.Runtime;AutomaticTasks.Start(a);x.Counter.Current=2;x.Counter.ChangedByte14=1;r.ObjectiveCounters[x.Goal]=1;
  var p=new Predicate{TypeId=4,Version=0,ComparisonId=2,Value=0};var g=new Group{Operation=0};g.Conditions.Add(p);var firstGoal=new Objective{Uid=2,ConditionsKnown=true,Conditions=g};r.State.Task.Objectives[0].Insert(0,firstGoal);r.ObjectiveCounters.Add(firstGoal,0);a.ConditionChangedBytes.Add(p,0);a.ObjectiveActionCounts.Add(firstGoal,0);
  var quirk=AutomaticTasks.Update(a);Is(quirk.Status=="Known"&&quirk.NativeReturn==false&&quirk.Emitted.Count==0,"later already-met dirty objective clears native return flag");Is(r.ObjectiveCounters[firstGoal]==1&&x.Counter.ChangedByte14==0&&r.State.Task.State==2,"earlier objective write remains even when native return false");
  x=ExampleKillQuest.Create(1);a=x.Auto;r=a.Runtime;AutomaticTasks.Start(a);AutomaticTasks.Update(a);x.Kill();a.OnCompleteEventCount=1;Is(AutomaticTasks.Update(a).Status=="Unsupported"&&x.Counter.ChangedByte14==1&&r.State.Task.State==2&&r.ObjectiveCounters[x.Goal]==2,"unsupported completion actions preserve dirty pending goal state");
  x=ExampleKillQuest.Create(1);a=x.Auto;r=a.Runtime;a.Events=new OfflineEvents(true){DebugEnabled=false};Is(AutomaticTasks.Start(a).Status=="Unsupported"&&r.State.Task.State==1,"inconsistent global event manager rejected");
  Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+n+"}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
