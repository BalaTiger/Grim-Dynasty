using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using GrimDynasty.Resources;
using GrimDynasty.Quests;
using GrimDynasty.TaskLogic;
using GrimDynasty.TokenActions;
public static class TokenActionCatalogProbe {
 public static int Main(string[] args){try{
  if(args.Length!=1)throw new ArgumentException("TokenActionCatalogProbe <authorized Quests.arc>");
  int quests=0,gives=0,removes=0,other=0;var examples=new List<object>();
  using(var input=ResourceInput.OpenGameResource(args[0]))using(var arc=new ArcArchive(input))foreach(var entry in arc.Entries.Values){
   var q=QuestDefinition.Read(arc.ReadMember(entry.name));quests++;
   foreach(Dictionary<string,object> task in (List<object>)q["tasks"])foreach(string group in new[]{"on_accept","objectives","on_complete"})foreach(Dictionary<string,object> node in (List<object>)task[group])foreach(Dictionary<string,object> action in (List<object>)node["actions"]){
    int id=Convert.ToInt32(action["type_id"]);if(id!=9&&id!=10){other++;continue;}
    string token=(string)((Dictionary<string,object>)action["parameters"])["token"];
    if(Convert.ToInt32(action["version"])!=0)throw new Exception("Uncovered token action version");
    // Exercise each actual resource parameter independently in a complete
    // synthetic repository. No event scheduling or reward execution inferred.
    var w=new World{RepositoryTokensComplete=true,ServerPlayerId=42};w.RepositoryTokens.Add(42,new List<string>());
    var result=Mutations.SetPlayerToken(w,42,token);if(result.Status!="Known")throw new Exception("Repository set unsupported: "+result.Reason);
    var p=new Predicate{TypeId=18,Reference=token,Expected=true};if(Evaluator.Evaluate(p,w).Value!=true)throw new Exception("Set/read closure failed");
    result=Mutations.RemovePlayerToken(w,42,token);if(result.Status!="Known")throw new Exception("Repository remove unsupported: "+result.Reason);
    if(Evaluator.Evaluate(p,w).Value!=false)throw new Exception("Remove/read closure failed");
    if(id==9)gives++;else removes++;
    if(examples.Count<3)examples.Add(new{member=entry.name,action_id=id,token});
   }
  }
  if(quests!=247||gives!=439||removes!=227||other!=2680)throw new Exception("Official action inventory mismatch");
  Console.WriteLine(new JavaScriptSerializer().Serialize(new{status="passed",quests,give_parameters=gives,remove_parameters=removes,other_actions=other,parameter_closures=gives+removes,examples,scope="Repository mutation/read closure using actual token parameters, synthetic player42. Full ScriptableAction execution and original dynamic equivalence not implied."}));return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
