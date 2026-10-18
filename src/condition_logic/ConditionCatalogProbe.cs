using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using GrimDynasty.Resources;
using GrimDynasty.Quests;
using GrimDynasty.Conditions;
public static class ConditionCatalogProbe {
 public static int Main(string[] args){try{
  if(args.Length!=1)throw new ArgumentException("ConditionCatalogProbe <authorized Quests.arc>");
  int quests=0,leaves=0,waits=0,unknown=0;var equalityTrue=new int[3];var contexts=new List<object>();
  for(uint difficulty=0;difficulty<3;difficulty++)for(int skip=0;skip<2;skip++){
   int knownTrue=0,knownFalse=0,unsupported=0,absent=0,collections=0;var reasons=new Dictionary<string,int>();
   using(var input=ResourceInput.OpenGameResource(args[0]))using(var arc=new ArcArchive(input))foreach(var entry in arc.Entries.Values){
    var q=QuestDefinition.Read(arc.ReadMember(entry.name));if(difficulty==0&&skip==0)quests++;
    foreach(Dictionary<string,object> task in (List<object>)q["tasks"])foreach(string group in new[]{"on_accept","objectives","on_complete"})foreach(Dictionary<string,object> node in (List<object>)task[group]){
     var definition=(Dictionary<string,object>)node["conditions"];
     if(difficulty==0&&skip==0)foreach(Dictionary<string,object> row in (List<object>)definition["conditions"]){int type=Convert.ToInt32(row["type_id"]);if(type==4)leaves++;else if(type==17)waits++;else unknown++;}
     // Direct real-data equality checks use fixed simulated contexts, not game state.
     if(skip==0)foreach(Dictionary<string,object> row in (List<object>)definition["conditions"])if(Convert.ToInt32(row["type_id"])==4){
      var values=(Dictionary<string,object>)row["parameters"];var s=new ConditionSpec{TypeId=4,Version=Convert.ToInt32(row["version"]),ComparisonId=Convert.ToInt32(row["comparison_id"]),StoredDifficulty=Convert.ToUInt32(values["difficulty"])};
      var leaf=ConditionEvaluator.Evaluate(s,new SimulatedContext{Difficulty=difficulty});if(leaf.Status!="Known")throw new Exception("supported catalog leaf became unsupported");if(leaf.Value.Value)equalityTrue[difficulty]++;
     }
     var r=ConditionDefinitionAdapter.Evaluate(definition,skip!=0,new SimulatedContext{Difficulty=difficulty,MainPlayerHardcoreByte=0});collections++;
     if(r.Status=="Known"){if(r.Value.Value)knownTrue++;else knownFalse++;}else{unsupported++;string reason=r.Reason;if(!reasons.ContainsKey(reason))reasons.Add(reason,0);reasons[reason]++;if(reason=="AbsentNativeConditionContainerRequiresCallerSemantics")absent++;}
    }
   }
   contexts.Add(new{simulated_difficulty=difficulty,skip_wait=skip!=0,collections,known_true=knownTrue,known_false=knownFalse,unsupported,absent_native_container=absent,unsupported_reasons=reasons});
  }
  if(quests!=247||leaves!=1784||waits!=260||equalityTrue[0]!=595||equalityTrue[1]!=594||equalityTrue[2]!=595)throw new Exception("official data parameter integration mismatch");
  Console.WriteLine(new JavaScriptSerializer{MaxJsonLength=1000000}.Serialize(new{status="passed",quests,difficulty_leaves=leaves,wait_leaves=waits,other_leaves=unknown,difficulty_equality_true_counts=equalityTrue,contexts,proof_scope="own compiled evaluator in simulated contexts; data counts and static-native-derived rules; original game not executed"}));return 0;
 }catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}}
}
