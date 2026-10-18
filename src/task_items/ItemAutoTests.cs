using System;
using GrimDynasty.TaskAuto;
public static class ItemAutoTests {
 static int n;static void Is(bool b,string s){n++;if(!b)throw new Exception(s);}
 public static int Main(){try{
  var x=ExampleItemQuest.Create();var a=x.Auto;var r=a.Runtime;Is(AutomaticTasks.Start(a).Status=="Known"&&x.Condition.Owner16Attached&&x.Condition.Owner17Attached,"start");Is(AutomaticTasks.Update(a).NativeReturn==true&&r.ObjectiveCounters[x.Goal]==2,"pending");
  x.Stock.Objects[100].QuantityFromSlot618=1;Is(x.Notify(16).Delivered==1&&x.Condition.ChangedByte14==1,"declared inventory change event");var partial=AutomaticTasks.Update(a);Is(partial.Status=="Known"&&partial.NativeReturn==true&&r.State.Task.State==2&&partial.Emitted[0].FlagC==0,"partial item progress");
  x.Stock.Objects[100].QuantityFromSlot618=2;Is(x.Notify(17).Delivered==1,"second event route");var done=AutomaticTasks.Update(a);Is(done.Status=="Known"&&r.State.Task.State==3&&done.Emitted[0].FlagC==1,"automatic item completion");Is(!x.Condition.Owner16Attached&&!x.Condition.Owner17Attached&&x.Notify(16).Delivered==0&&x.Notify(17).Delivered==0,"end unregisters both");
  Is(AutomaticTasks.Reset(a).Status=="Known"&&x.Stock.Objects[100].QuantityFromSlot618==2&&r.ObjectiveCounters[x.Goal]==0,"reset leaves item stock intact");Is(AutomaticTasks.Start(a).Status=="Known"&&AutomaticTasks.Update(a).Status=="Known"&&r.State.Task.State==3,"restart with items already held auto completes");
  x=ExampleItemQuest.Create();a=x.Auto;r=a.Runtime;AutomaticTasks.Start(a);AutomaticTasks.Update(a);x.Stock.Objects[100].QuantityFromSlot618=2;x.Notify(16);x.Condition.SystemTimeSnapshot=4001;Is(AutomaticTasks.Update(a).Status=="Unsupported"&&x.Condition.ChangedByte14==1&&r.State.Task.State==2,"UI dependency atomic");x.Condition.SystemTimeSnapshot=0;a.OnCompleteEventCount=1;Is(AutomaticTasks.Update(a).Status=="Unsupported"&&r.ObjectiveCounters[x.Goal]==2,"reward dependency atomic");
  Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+n+"}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
