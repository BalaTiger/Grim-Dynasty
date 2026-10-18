using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using GrimDynasty.Resources;
using GrimDynasty.Quests;
using GrimDynasty.TaskLogic;
using GrimDynasty.TokenActions;
using GrimDynasty.TaskRuntime;
public static class TaskRuntimeCatalogProbe {
 static void Require(bool ok,string reason){if(!ok)throw new Exception(reason);}
 public static int Main(string[] args){try{
  if(args.Length!=1)throw new ArgumentException("TaskRuntimeCatalogProbe <authorized Quests.arc>");int quests=0,candidates=0,chains=0,goalTrue=0,goalFalse=0,goalUnknown=0,statsInputs=0,statsChanges=0,inboundClosures=0;var examples=new List<object>();
  using(var input=ResourceInput.OpenGameResource(args[0]))using(var arc=new ArcArchive(input))foreach(var entry in arc.Entries.Values){var q=QuestDefinition.Read(arc.ReadMember(entry.name));quests++;
   foreach(Dictionary<string,object> t in (List<object>)q["tasks"]){
    foreach(string key in new[]{"on_accept","objectives","on_complete"})foreach(Dictionary<string,object> node in (List<object>)t[key])foreach(Dictionary<string,object> a in (List<object>)node["actions"]){
     int actionType=Convert.ToInt32(a["type_id"]);
     if(actionType==9||actionType==10){
      string token=(string)((Dictionary<string,object>)a["parameters"])["token"];
      var receiver=new World{RepositoryTokensComplete=true,ServerPlayerId=42};receiver.RepositoryTokens.Add(42,new List<string>());
      Require(InboundTokens.Give(receiver,42,token).Status=="Known","inbound give unsupported");
      Require(Evaluator.Evaluate(new Predicate{TypeId=18,Reference=token,Expected=true},receiver).Value==true,"inbound give/read failed");
      Require(InboundTokens.Remove(receiver,42,token).Status=="Known","inbound remove unsupported");
      Require(Evaluator.Evaluate(new Predicate{TypeId=18,Reference=token,Expected=true},receiver).Value==false,"inbound remove/read failed");inboundClosures++;
     }
     if(actionType==9){var stats=new TokenStatsSnapshot{DefenseBuilt=0,PowerupActive=0};var v=TokenStats.Granted(stats,(string)((Dictionary<string,object>)a["parameters"])["token"]);Require(v.Status=="Known","stats unsupported");statsInputs++;if(v.Changed==true)statsChanges++;}}
    if(!(bool)t["dont_propagate"]||(Convert.ToUInt32(q["flags"])&4)!=0||((List<object>)t["on_accept"]).Count!=0)continue;
    bool hooks=true;foreach(Dictionary<string,object> o in (List<object>)t["objectives"]){var g=DefinitionAdapter.ReadGroup((Dictionary<string,object>)o["conditions"]);if(g!=null)foreach(var p in g.Conditions)if(!Lifecycle.KnownNoOp(p))hooks=false;}
    if(!hooks)continue;candidates++;
    for(uint d=0;d<3;d++){
     var world=new World{QuestIndexComplete=true,MainPlayerResolved=true,Level=10,GameMode=0,RepositoryTokensComplete=true,ServerPlayerId=1,ServerServiceSlot18NonNull=false};world.Core.Difficulty=d;world.Core.MainPlayerHardcoreByte=0;world.OtherModeTokens.Add(d,new List<string>());
     var task=new TaskState{Uid=Convert.ToUInt32(t["uid"]),State=1};var os=new List<Objective>();task.Objectives.Add(d,os);
     foreach(Dictionary<string,object> o in (List<object>)t["objectives"])os.Add(new Objective{Uid=Convert.ToUInt32(o["uid"]),ConditionsKnown=true,Conditions=DefinitionAdapter.ReadGroup((Dictionary<string,object>)o["conditions"])});
     string reference="explicit-fixture:"+entry.name;var qs=new QuestState();qs.Tasks.Add(task);world.Quests.Add(reference,qs);
     var runtime=new RuntimeSnapshot{State=new TaskMutationSnapshot{Task=task,Difficulty=d,QuestFlags=Convert.ToUInt32(q["flags"]),OnAcceptEventCount=0,StatesByDifficulty=new[]{1,1,1}},World=world,QuestReference=reference,DontPropagateByte=1,CompletionRecorded=new bool[3],Outbound=new OutboundSnapshot{ProviderIsKnownOutbound=true}};
     foreach(var o in os)runtime.ObjectiveCounters.Add(o,2);
     Require(Lifecycle.Start(runtime).Status=="Known"&&task.State==2,"start failed");
     var ready=Evaluator.Evaluate(new Predicate{TypeId=11,Reference=reference,TaskUid=task.Uid,Expected=true},world);if(ready.Status!="Known")goalUnknown++;else if(ready.Value.Value)goalTrue++;else goalFalse++;
     Require(Lifecycle.CompleteNoRewards(runtime).Status=="Known"&&task.State==3&&runtime.CompletionRecorded[d],"complete failed");
     Require(Evaluator.Evaluate(new Predicate{TypeId=1,Reference=reference,TaskUid=task.Uid,Expected=true},world).Value==true,"complete query failed");
     Require(runtime.Outbound.LastQuestReference==reference&&runtime.Outbound.LastTaskUid==task.Uid,"outbound cached fields missing");
     Require(Lifecycle.Reset(runtime).Status=="Known"&&task.State==1,"reset failed");foreach(var o in os)Require(runtime.ObjectiveCounters[o]==0,"objective reset failed");chains++;
     if(d==0&&examples.Count<5)examples.Add(new{member=entry.name,task_uid=task.Uid,objective_count=os.Count,goal_status=ready.Status,goal_value=ready.Value,trace=new[]{"declared_state1","Start:state2","AreObjectivesMetQuery","explicit_Complete(false):state3","QuestTaskComplete:true","Reset:state1"}});
    }
   }
  }
  Require(quests==247&&candidates==5&&chains==15&&statsInputs==439&&statsChanges==0&&inboundClosures==666,"official runtime parameter inventory mismatch");
  Console.WriteLine(new JavaScriptSerializer().Serialize(new{status="passed",quests,candidates,completed_state_chains=chains,goal_true=goalTrue,goal_false=goalFalse,goal_unsupported=goalUnknown,inbound_token_closures=inboundClosures,stats_parameters=statsInputs,stats_counter_changes=statsChanges,examples,scope="Actual task UIDs/conditions/dont_propagate parameter integration; explicitly declared initial runtime state, exact fixture quest keys and outbound provider. Explicit Complete(false), no automatic Update, rewards, real game, saves or network execution."}));return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
