using System;
using System.Collections.Generic;
using GrimDynasty.TaskLogic;
using GrimDynasty.TokenActions;
using GrimDynasty.TaskRuntime;
using GrimDynasty.TaskAuto;
public static class AutoTaskTests {
 static int n;
 static void Is(bool b,string s){n++;if(!b)throw new Exception(s);}
 static AutoSnapshot Make(out Predicate p,out Objective goal){
  var world=new World{QuestIndexComplete=true,RepositoryTokensComplete=true,ServerPlayerId=1,MainPlayerResolved=true};world.Core.Difficulty=0;world.RepositoryTokens.Add(1,new List<string>());
  p=new Predicate{TypeId=18,Reference="ready",Expected=true};var group=new Group{Operation=0};group.Conditions.Add(p);goal=new Objective{Uid=2,ConditionsKnown=true,Conditions=group};var task=new TaskState{Uid=1,State=2};task.Objectives.Add(0,new List<Objective>{goal});var q=new QuestState();q.Tasks.Add(task);world.Quests.Add("q",q);
  var r=new RuntimeSnapshot{State=new TaskMutationSnapshot{Task=task,Difficulty=0,StatesByDifficulty=new[]{1,1,1}},World=world,QuestReference="q",DontPropagateByte=1,CompletionRecorded=new bool[3],Outbound=new OutboundSnapshot{ProviderIsKnownOutbound=true}};r.ObjectiveCounters.Add(goal,0);
  var a=new AutoSnapshot{Runtime=r,QuestUid=42,Events=new OfflineEvents(true){DebugEnabled=false},OnCompleteEventCount=0,RewardRecordedA3=new bool[3],MainPlayerFlags17F4=0};a.ConditionChangedBytes.Add(p,0);a.ObjectiveActionCounts.Add(goal,0);return a;
 }
 public static int Main(){try{Predicate p;Objective goal;var a=Make(out p,out goal);var r=a.Runtime;
  var first=AutomaticTasks.Update(a);Is(first.Status=="Known"&&first.NativeReturn==true&&r.ObjectiveCounters[goal]==2&&r.State.Task.State==2,"first tick pending");Is(first.Emitted.Count==1&&first.Emitted[0].Kind==42&&first.Emitted[0].FlagC==0&&first.Emitted[0].FlagD==0&&first.Emitted[0].QuestUid==42,"initial notification payload");
  Is(AutomaticTasks.Update(a).NativeReturn==false,"unchanged pending returns false");InboundTokens.Give(r.World,1,"ready");var second=AutomaticTasks.Update(a);Is(second.Status=="Known"&&second.NativeReturn==true&&r.State.Task.State==3&&r.ObjectiveCounters[goal]==1,"second tick automatically completes");Is(second.Emitted[0].FlagC==1&&second.Emitted[0].FlagD==1&&second.Emitted[0].FlagE==0,"transition notification");Is(a.RewardRecordedA3[0]&&!a.RewardRecordedA3[1]&&r.CompletionRecorded[0],"true-completion reward history flags");Is(r.Outbound.LastQuestReference=="q"&&r.Outbound.LastTaskUid==1,"outbound fields");
  a=Make(out p,out goal);r=a.Runtime;r.ObjectiveCounters[goal]=1;InboundTokens.Give(r.World,1,"ready");Is(AutomaticTasks.Update(a).NativeReturn==false&&r.State.Task.State==2,"already met unchanged does not auto complete");
  a=Make(out p,out goal);r=a.Runtime;r.ObjectiveCounters[goal]=1;var regressed=AutomaticTasks.Update(a);Is(regressed.NativeReturn==true&&r.ObjectiveCounters[goal]==2&&regressed.Emitted[0].FlagC==1,"met to unmet regression");
  a=Make(out p,out goal);r=a.Runtime;InboundTokens.Give(r.World,1,"ready");var immediate=AutomaticTasks.Update(a);Is(immediate.NativeReturn==true&&r.State.Task.State==3&&immediate.Emitted[0].FlagC==0,"initially true automatically completes with initial flags false");
  a=Make(out p,out goal);r=a.Runtime;InboundTokens.Give(r.World,1,"ready");a.OnCompleteEventCount=1;Is(AutomaticTasks.Update(a).Status=="Unsupported"&&r.State.Task.State==2&&r.ObjectiveCounters[goal]==0&&!a.RewardRecordedA3[0],"reward events reject before any writes");
  a=Make(out p,out goal);r=a.Runtime;InboundTokens.Give(r.World,1,"ready");r.DontPropagateByte=0;Is(AutomaticTasks.Update(a).Status=="Unsupported"&&r.ObjectiveCounters[goal]==0,"network rejects atomically");
  a=Make(out p,out goal);r=a.Runtime;a.Events.Register(42,new OfflineHandler{Invoke=e=>{throw new Exception("must not run");}});Is(AutomaticTasks.Update(a).Status=="Unsupported"&&r.ObjectiveCounters[goal]==0,"nonempty subscribers outside auto domain");
  a=Make(out p,out goal);r=a.Runtime;a.ConditionChangedBytes[p]=1;Is(AutomaticTasks.Update(a).Status=="Unsupported"&&r.ObjectiveCounters[goal]==0,"dirty condition outside domain");
  a=Make(out p,out goal);r=a.Runtime;r.State.Task.RuntimeBlocked=255;a.Events=null;Is(AutomaticTasks.Update(a).NativeReturn==false&&r.ObjectiveCounters[goal]==0,"blocked early no-op");
  a=Make(out p,out goal);r=a.Runtime;r.State.Task.Objectives[0].Clear();Is(AutomaticTasks.Update(a).NativeReturn==false&&r.State.Task.State==2,"empty objectives do not auto complete");
  a=Make(out p,out goal);r=a.Runtime;p.TypeId=17;var wait=AutomaticTasks.Update(a);Is(wait.NativeReturn==true&&r.State.Task.State==2&&r.ObjectiveCounters[goal]==2,"auto update does not skip wait");
  a=Make(out p,out goal);r=a.Runtime;r.ObjectiveCounters[goal]=2;InboundTokens.Give(r.World,1,"ready");a.ObjectiveActionCounts[goal]=1;Is(AutomaticTasks.Update(a).Status=="Unsupported"&&r.ObjectiveCounters[goal]==2,"objective actions reject before transition");
  a=Make(out p,out goal);r=a.Runtime;r.ObjectiveCounters[goal]=99;Is(AutomaticTasks.Update(a).Status=="Unsupported","invalid goal state domain");a.ConditionChangedBytes.Clear();Is(AutomaticTasks.Update(a).Status=="Unsupported","unknown dirty state");
  a=Make(out p,out goal);r=a.Runtime;p.TypeId=14;Is(AutomaticTasks.Update(a).Status=="Unsupported","task-sensitive query excluded");
  Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+n+"}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
