using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using GrimDynasty.TaskAuto;
public static class KillQuestDemo {
 static void Need(bool b,string s){if(!b)throw new Exception(s);}
 public static int Main(){try{
  var x=ExampleKillQuest.Create(2);var trace=new List<object>();var r=x.Auto.Runtime;
  Need(AutomaticTasks.Start(x.Auto).Status=="Known","start");trace.Add(new{step="start_and_register_event11",task_state=r.State.Task.State});
  Need(AutomaticTasks.Update(x.Auto).Status=="Known","initial tick");trace.Add(new{step="initial_update",objective_state=r.ObjectiveCounters[x.Goal]});
  for(int i=1;i<=2;i++){
   Need(x.Kill().Delivered==1,"event dispatch");var update=AutomaticTasks.Update(x.Auto);Need(update.Status=="Known","update");trace.Add(new{step="decoded_kill_event_then_update",count=x.Counter.Current,task_state=r.State.Task.State,objective_state=r.ObjectiveCounters[x.Goal],notification_flag=update.Emitted[0].FlagC});
  }
  Need(r.State.Task.State==3&&!x.Counter.CallbackOwnerAttached,"automatic completion");Need(x.Kill().Delivered==0,"unsubscribe");trace.Add(new{step="post_completion_event_not_delivered",counter=x.Counter.Current});
  Need(AutomaticTasks.Reset(x.Auto).Status=="Known"&&x.Counter.Current==0,"reset");trace.Add(new{step="reset",task_state=r.State.Task.State,count=x.Counter.Current});
  Console.WriteLine(new JavaScriptSerializer().Serialize(new{status="passed",scenario="Original border patrol fixture",trace,scope="Decoded event/entity snapshots, evidenced HasKilled v1 lifecycle and automatic empty-action completion. UI time gate closed, no original combat producer, text renderer, reward grant, network, DLL/resources, Steam or saves."}));return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
