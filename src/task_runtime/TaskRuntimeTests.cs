using System;
using System.Collections.Generic;
using GrimDynasty.TaskLogic;
using GrimDynasty.TokenActions;
using GrimDynasty.TaskRuntime;
public static class TaskRuntimeTests {
 static int n;
 static void C(MutationResult r,bool? c,string label){n++;if(r.Changed!=c||(c.HasValue?r.Status!="Known":r.Status!="Unsupported"))throw new Exception(label+":"+r.Reason);}
 static void Is(bool b,string label){n++;if(!b)throw new Exception(label);}
 public static int Main(){try{
  var w=new World{QuestIndexComplete=true,ServerServiceSlot18NonNull=false};w.Core.Difficulty=0;
  var goal=new Objective{Uid=2,ConditionsKnown=true,Conditions=new Group{Operation=0}};goal.Conditions.Conditions.Add(new Predicate{TypeId=17});
  var t=new TaskState{Uid=1,State=1};t.Objectives.Add(0,new List<Objective>{goal});var q=new QuestState();q.Tasks.Add(t);w.Quests.Add("q",q);
  var r=new RuntimeSnapshot{State=new TaskMutationSnapshot{Task=t,Difficulty=0,QuestFlags=1,OnAcceptEventCount=0,StatesByDifficulty=new[]{1,1,1}},World=w,QuestReference="q",DontPropagateByte=1,CompletionRecorded=new bool[3],Outbound=new OutboundSnapshot{ProviderIsKnownOutbound=true}};r.ObjectiveCounters.Add(goal,2);
  C(Lifecycle.Start(r),true,"start with wait lifecycle noop");Is(t.State==2,"active");Is(Evaluator.Evaluate(new Predicate{TypeId=11,Reference="q",TaskUid=1,Expected=true},w).Value==true,"all objectives caller skips wait");
  C(Lifecycle.CompleteNoRewards(r),true,"active no reward complete suppressed network");Is(t.State==3&&r.State.StatesByDifficulty[0]==3&&r.CompletionRecorded[0]&&!r.CompletionRecorded[1],"completion banks");
  Is(r.Outbound.LastQuestReference=="q"&&r.Outbound.LastTaskUid==1,"outbound cached fields updated even no send");Is(Evaluator.Evaluate(new Predicate{TypeId=1,Reference="q",TaskUid=1,Expected=true},w).Value==true,"complete query");
  C(Lifecycle.Reset(r),true,"reset nonempty wait goal");Is(t.State==1&&r.ObjectiveCounters[goal]==0&&r.CompletionRecorded[0],"reset does not clear recorded completion flag");
  C(Lifecycle.Start(r),true,"restart");r.DontPropagateByte=0;C(Lifecycle.CompleteNoRewards(r),null,"network unresolved");Is(t.State==2&&r.State.StatesByDifficulty[0]==1,"no partial state mutation");
  r.DontPropagateByte=null;C(Lifecycle.CompleteNoRewards(r),null,"unknown propagation flag");r.DontPropagateByte=255;r.Outbound.ProviderIsKnownOutbound=false;C(Lifecycle.CompleteNoRewards(r),null,"unknown dispatch provider");r.Outbound.ProviderIsKnownOutbound=true;
  goal.Conditions.Conditions.Add(new Predicate{TypeId=99});C(Lifecycle.CompleteNoRewards(r),null,"unrecovered lifecycle");goal.Conditions.Conditions.RemoveAt(1);
  C(Lifecycle.CompleteNoRewards(r),true,"nonzero suppression raw byte");r.ObjectiveCounters.Clear();C(Lifecycle.Reset(r),null,"unknown objective counter");Is(t.State==3,"reset atomic");r.ObjectiveCounters.Add(goal,1);
  foreach(int type in new[]{0,1,3,4,5,7,11,14,17,18,19,20})Is(Lifecycle.KnownNoOp(new Predicate{TypeId=type}),"no op class "+type);
  Is(!Lifecycle.KnownNoOp(new Predicate{TypeId=10}),"unknown hook");Is(!Lifecycle.KnownNoOp(new Predicate{TypeId=4,Version=1}),"unverified hook version");Is(Lifecycle.KnownNoOp(new Predicate{TypeId=11,Version=1}),"same v1 class hook");
  var stats=new TokenStatsSnapshot{DefenseBuilt=0,PowerupActive=uint.MaxValue};C(TokenStats.Granted(stats,"SURVIVALMODE_DEFENSEBUILT"),true,"defense increment");Is(stats.DefenseBuilt==1,"defense value");C(TokenStats.Granted(stats,"SURVIVALMODE_POWERUPACTIVE"),true,"powerup increment wrap");Is(stats.PowerupActive==0,"uint wrap");
  C(TokenStats.Granted(stats,"survivalmode_defensebuilt"),false,"case");C(TokenStats.Granted(stats,"SURVIVALMODE_DEFENSEBUILT\0"),false,"length");C(TokenStats.Granted(null,"other"),false,"unrelated token needs no stats snapshot");C(TokenStats.Granted(null,"SURVIVALMODE_DEFENSEBUILT"),null,"needed counter missing");C(TokenStats.Granted(stats,null),null,"missing token");
  Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+n+"}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
