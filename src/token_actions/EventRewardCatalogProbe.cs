using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using GrimDynasty.Resources;
using GrimDynasty.Quests;
using GrimDynasty.TaskLogic;
using GrimDynasty.TokenActions;
public static class EventRewardCatalogProbe {
 public static int Main(string[] args){try{
  if(args.Length!=1)throw new ArgumentException("EventRewardCatalogProbe <authorized Quests.arc>");var results=new List<object>();
  for(uint d=0;d<3;d++){
   int vectors=0,yes=0,no=0,unknown=0;var reasons=new Dictionary<string,int>();var w=new World{MainPlayerResolved=true,Level=10,GameMode=0,QuestIndexComplete=true,RepositoryTokensComplete=true,ServerPlayerId=1,ServerServiceSlot18NonNull=false};w.Core.Difficulty=d;w.Core.MainPlayerHardcoreByte=0;w.OtherModeTokens.Add(d,new List<string>());
   using(var input=ResourceInput.OpenGameResource(args[0]))using(var arc=new ArcArchive(input))foreach(var entry in arc.Entries.Values){var q=QuestDefinition.Read(arc.ReadMember(entry.name));
    foreach(Dictionary<string,object> task in (List<object>)q["tasks"])foreach(string key in new[]{"on_accept","on_complete"}){
     var list=new List<EventSnapshot>();foreach(Dictionary<string,object> node in (List<object>)task[key]){
      var ids=new List<int?>();foreach(Dictionary<string,object> a in (List<object>)node["actions"])ids.Add(Convert.ToInt32(a["type_id"]));
      list.Add(new EventSnapshot{ConditionPointerKnown=true,Conditions=DefinitionAdapter.ReadGroup((Dictionary<string,object>)node["conditions"]),ActionPointerKnown=true,ActionTypes=ids});
     }
     vectors++;var r=EventRewardProbe.HasRewardActions(list,w);if(r.Status=="Known"){if(r.Value.Value)yes++;else no++;}else{unknown++;if(!reasons.ContainsKey(r.Reason))reasons.Add(r.Reason,0);reasons[r.Reason]++;}
    }
   }
   if(vectors!=918)throw new Exception("Task event vector inventory mismatch");results.Add(new{difficulty=d,vectors,contains_reward=yes,no_reward=no,unsupported=unknown,reasons});
  }
  Console.WriteLine(new JavaScriptSerializer().Serialize(new{status="passed",results,scope="Task event-vector reward predicate only, explicit synthetic world. No action execution or objective overload inferred."}));return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
