using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using GrimDynasty.TaskLogic;
using GrimDynasty.TokenActions;
using GrimDynasty.TaskRuntime;
public static class TaskRuntimeDemo {
 static void Need(bool b,string why){if(!b)throw new Exception(why);}
 public static int Main(){try{
  var trace=new List<object>();var world=new World{QuestIndexComplete=true,RepositoryTokensComplete=true,ServerPlayerId=1,ServerServiceSlot18NonNull=false};world.Core.Difficulty=0;world.RepositoryTokens.Add(1,new List<string>());
  var group=new Group{Operation=0};group.Conditions.Add(new Predicate{TypeId=18,Reference="MILITARY_REPORT_VERIFIED",Expected=true});
  var goal=new Objective{Uid=1,ConditionsKnown=true,Conditions=group};var task=new TaskState{Uid=1,State=1};task.Objectives.Add(0,new List<Objective>{goal});var quest=new QuestState();quest.Tasks.Add(task);world.Quests.Add("ming/liaodong_report",quest);
  var runtime=new RuntimeSnapshot{State=new TaskMutationSnapshot{Task=task,Difficulty=0,QuestFlags=0,OnAcceptEventCount=0,StatesByDifficulty=new[]{1,1,1}},World=world,QuestReference="ming/liaodong_report",DontPropagateByte=1,CompletionRecorded=new bool[3],Outbound=new OutboundSnapshot{ProviderIsKnownOutbound=true}};runtime.ObjectiveCounters.Add(goal,0);
  Need(Lifecycle.Start(runtime).Status=="Known"&&task.State==2,"start");trace.Add(new{step="accept",task_state=task.State});
  var condition=new Predicate{TypeId=11,Reference=runtime.QuestReference,TaskUid=1,Expected=true};Need(Evaluator.Evaluate(condition,world).Value==false,"initial goal false");trace.Add(new{step="query_goal_before_report",value=false});
  Need(InboundTokens.Give(world,1,"MILITARY_REPORT_VERIFIED").Status=="Known","inbound grant");trace.Add(new{step="apply_decoded_inbound_token",token="MILITARY_REPORT_VERIFIED"});
  Need(Evaluator.Evaluate(condition,world).Value==true,"goal now true");trace.Add(new{step="query_goal_after_report",value=true});
  Need(Lifecycle.CompleteNoRewards(runtime).Status=="Known"&&task.State==3,"complete");trace.Add(new{step="explicit_complete_without_rewards",task_state=task.State,network_send=false});
  Need(Evaluator.Evaluate(new Predicate{TypeId=1,Reference=runtime.QuestReference,TaskUid=1,Expected=true},world).Value==true,"complete predicate");trace.Add(new{step="query_complete",value=true});
  Need(Lifecycle.Reset(runtime).Status=="Known"&&task.State==1,"reset");trace.Add(new{step="reset",task_state=task.State,objective_state=runtime.ObjectiveCounters[goal]});
  Console.WriteLine(new JavaScriptSerializer().Serialize(new{status="passed",scenario="Liaodong report: own original fixture",trace,scope="Own code and own fixture only; no original DLL, resource, Steam, save, network, automatic update or reward execution."}));return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
