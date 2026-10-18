using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using GrimDynasty.TaskLogic;
using GrimDynasty.TokenActions;
using GrimDynasty.TaskRuntime;
using GrimDynasty.TaskAuto;
public static class AutoQuestDemo {
 static void Need(bool b,string s){if(!b)throw new Exception(s);}
 public static int Main(){try{
  var trace=new List<object>();var world=new World{QuestIndexComplete=true,RepositoryTokensComplete=true,ServerPlayerId=1,ServerServiceSlot18NonNull=false,MainPlayerResolved=true};world.Core.Difficulty=0;world.RepositoryTokens.Add(1,new List<string>());
  var p=new Predicate{TypeId=18,Reference="MILITARY_REPORT_VERIFIED",Expected=true};var g=new Group{Operation=0};g.Conditions.Add(p);var goal=new Objective{Uid=1,ConditionsKnown=true,Conditions=g};var task=new TaskState{Uid=1,State=1};task.Objectives.Add(0,new List<Objective>{goal});var quest=new QuestState();quest.Tasks.Add(task);world.Quests.Add("ming/liaodong_report",quest);
  var runtime=new RuntimeSnapshot{State=new TaskMutationSnapshot{Task=task,Difficulty=0,QuestFlags=0,OnAcceptEventCount=0,StatesByDifficulty=new[]{1,1,1}},World=world,QuestReference="ming/liaodong_report",DontPropagateByte=1,CompletionRecorded=new bool[3],Outbound=new OutboundSnapshot{ProviderIsKnownOutbound=true}};runtime.ObjectiveCounters.Add(goal,0);
  var auto=new AutoSnapshot{Runtime=runtime,QuestUid=1,Events=new OfflineEvents(true){DebugEnabled=false},OnCompleteEventCount=0,RewardRecordedA3=new bool[3],MainPlayerFlags17F4=0};auto.ConditionChangedBytes.Add(p,0);auto.ObjectiveActionCounts.Add(goal,0);
  Need(Lifecycle.Start(runtime).Status=="Known","start");trace.Add(new{step="accept",task_state=task.State});
  var pending=AutomaticTasks.Update(auto);Need(pending.Status=="Known"&&task.State==2&&runtime.ObjectiveCounters[goal]==2,"pending tick");trace.Add(new{step="update_before_report",task_state=task.State,objective_state=runtime.ObjectiveCounters[goal],event_flag=pending.Emitted[0].FlagC});
  Need(InboundTokens.Give(world,1,"MILITARY_REPORT_VERIFIED").Status=="Known","inbound");trace.Add(new{step="apply_decoded_inbound_report_token"});
  var complete=AutomaticTasks.Update(auto);Need(complete.Status=="Known"&&task.State==3&&auto.RewardRecordedA3[0],"automatic completion");trace.Add(new{step="update_automatically_completes",task_state=task.State,objective_state=runtime.ObjectiveCounters[goal],reward_history_recorded=auto.RewardRecordedA3[0],event_flag=complete.Emitted[0].FlagC,network_send=false});
  Need(Evaluator.Evaluate(new Predicate{TypeId=1,Reference=runtime.QuestReference,TaskUid=1,Expected=true},world).Value==true,"completed query");trace.Add(new{step="completed_query",value=true});
  Need(Lifecycle.Reset(runtime).Status=="Known"&&task.State==1,"reset");trace.Add(new{step="reset",task_state=task.State,objective_state=runtime.ObjectiveCounters[goal]});
  Console.WriteLine(new JavaScriptSerializer().Serialize(new{status="passed",scenario="Original Liaodong report fixture",trace,scope="Own code/fixture only. Task.Update branch auto-completes through empty-event Complete(true) value semantics; no explicit manual completion, no original DLL/resources, rewards granted, subscribers, network, Steam or saves."}));return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
