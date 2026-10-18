using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using GrimDynasty.Resources;
using GrimDynasty.Quests;
using GrimDynasty.TaskLogic;
using GrimDynasty.TokenActions;
using GrimDynasty.TaskRuntime;
using GrimDynasty.TaskAuto;
using GrimDynasty.TaskKills;
public static class KillAutoCatalogProbe {
 public static int Main(string[] args){try{
  if(args.Length!=1)throw new ArgumentException("KillAutoCatalogProbe <authorized Quests.arc>");var scenarios=new List<object>();
  for(uint d=0;d<3;d++){
   int quests=0,tasks=0,changed=0,unchanged=0,completed=0,unsupported=0,killConditions=0;var reasons=new Dictionary<string,int>();
   using(var input=ResourceInput.OpenGameResource(args[0]))using(var arc=new ArcArchive(input))foreach(var entry in arc.Entries.Values){var q=QuestDefinition.Read(arc.ReadMember(entry.name));quests++;
    foreach(Dictionary<string,object> t in (List<object>)q["tasks"]){tasks++;
     var world=new World{QuestIndexComplete=true,RepositoryTokensComplete=true,ServerPlayerId=1,MainPlayerResolved=true,GameMode=0,Level=10,ServerServiceSlot18NonNull=false};world.Core.Difficulty=d;world.Core.MainPlayerHardcoreByte=0;world.RepositoryTokens.Add(1,new List<string>());world.OtherModeTokens.Add(d,new List<string>());
     var task=new TaskState{Uid=Convert.ToUInt32(t["uid"]),State=2};var goals=new List<Objective>();task.Objectives.Add(d,goals);string reference="explicit-fixture:"+entry.name;var quest=new QuestState();quest.Tasks.Add(task);world.Quests.Add(reference,quest);
     var r=new RuntimeSnapshot{State=new TaskMutationSnapshot{Task=task,Difficulty=d,QuestFlags=Convert.ToUInt32(q["flags"]),StatesByDifficulty=new[]{1,1,1}},World=world,QuestReference=reference,DontPropagateByte=(bool)t["dont_propagate"]?(byte)1:(byte)0,CompletionRecorded=new bool[3],Outbound=new OutboundSnapshot{ProviderIsKnownOutbound=true}};
     var a=new AutoSnapshot{Runtime=r,QuestUid=Convert.ToUInt32(q["uid"]),Events=new OfflineEvents(true){DebugEnabled=false},OnCompleteEventCount=((List<object>)t["on_complete"]).Count,RewardRecordedA3=new bool[3],MainPlayerFlags17F4=0};
     var entities=new KillWorld{IndexComplete=true};entities.Entities.Add(1,new EntitySnapshot{Kind=EntityKind.Player});entities.Entities.Add(2,new EntitySnapshot{Kind=EntityKind.Monster,RawByte20A4=0});
     foreach(Dictionary<string,object> o in (List<object>)t["objectives"]){var goal=new Objective{Uid=Convert.ToUInt32(o["uid"]),ConditionsKnown=true,Conditions=DefinitionAdapter.ReadGroup((Dictionary<string,object>)o["conditions"])};if(goal.Conditions!=null){
      var rawRows=(List<object>)((Dictionary<string,object>)o["conditions"])["conditions"];
      for(int i=0;i<rawRows.Count;i++){var row=(Dictionary<string,object>)rawRows[i];if(Convert.ToInt32(row["type_id"])==10){Predicate p;KillCondition counter;var parsed=KillDefinitionAdapter.Read(row,a.Events,entities,out p,out counter);if(parsed.Status!="Known")throw new Exception(parsed.Reason);counter.SystemTimeSnapshot=0;counter.PureLocalizationContextKnown=true;goal.Conditions.Conditions[i]=p;a.ConditionRuntimes.Add(p,counter);killConditions++;}}
     }
     goals.Add(goal);r.ObjectiveCounters.Add(goal,0);a.ObjectiveActionCounts.Add(goal,((List<object>)o["actions"]).Count);if(goal.Conditions!=null)foreach(var p in goal.Conditions.Conditions)a.ConditionChangedBytes.Add(p,0);}
     var started=AutomaticTasks.Start(a);var result=started.Status=="Known"?AutomaticTasks.Update(a):AutoResult.Unsupported(started.Reason);if(result.Status=="Known"){if(result.NativeReturn.Value)changed++;else unchanged++;if(task.State==3)completed++;}
     else{unsupported++;if(!reasons.ContainsKey(result.Reason))reasons.Add(result.Reason,0);reasons[result.Reason]++;if(task.State!=2||a.RewardRecordedA3[0])throw new Exception("Unsupported updated snapshot");foreach(var goal in goals)if(r.ObjectiveCounters[goal]!=0)throw new Exception("Unsupported mutated objective");}
    }
   }
   if(quests!=247||tasks!=459||changed+unchanged+unsupported!=459)throw new Exception("Official task inventory mismatch");scenarios.Add(new{difficulty=d,quests,tasks,changed,unchanged,automatically_completed=completed,unsupported,registered_condition_parameters=killConditions,reasons});
  }
  Console.WriteLine(new JavaScriptSerializer().Serialize(new{status="passed",scenarios,scope="Actual official definitions; Start of an explicitly active task replays lifecycle hooks, then Update, with restored HasKilled providers and a declared no-kill/clean world. No original runtime capture or rewards; unsupported Update inputs unchanged."}));return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
