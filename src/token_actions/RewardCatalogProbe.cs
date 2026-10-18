using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using GrimDynasty.Resources;
using GrimDynasty.Quests;
using GrimDynasty.TokenActions;
public static class RewardCatalogProbe {
 public static int Main(string[] args){try{
  if(args.Length!=1)throw new ArgumentException("RewardCatalogProbe <authorized Quests.arc>");int quests=0,nodes=0,actions=0,rewardLeaves=0,rewardGroups=0;
  using(var input=ResourceInput.OpenGameResource(args[0]))using(var arc=new ArcArchive(input))foreach(var entry in arc.Entries.Values){var q=QuestDefinition.Read(arc.ReadMember(entry.name));quests++;
   foreach(Dictionary<string,object> task in (List<object>)q["tasks"])foreach(string key in new[]{"on_accept","objectives","on_complete"})foreach(Dictionary<string,object> node in (List<object>)task[key]){
    nodes++;var ids=new List<int?>();foreach(Dictionary<string,object> a in (List<object>)node["actions"]){int id=Convert.ToInt32(a["type_id"]);ids.Add(id);actions++;if(RewardClassifier.HasRewardActions(new int?[]{id}).Value==true)rewardLeaves++;}
    var r=RewardClassifier.HasRewardActions(ids);if(r.Status!="Known")throw new Exception("Known action IDs unsupported");if(r.Value.Value)rewardGroups++;
   }
  }
  if(quests!=247||nodes!=3901||actions!=3346||rewardLeaves!=2091)throw new Exception("Official reward parameter inventory mismatch");
  Console.WriteLine(new JavaScriptSerializer().Serialize(new{status="passed",quests,nodes,actions,reward_action_leaves=rewardLeaves,groups_containing_reward=rewardGroups,scope="Pure raw-type classification only. Event conditions, reward execution/scaling and original dynamic validation remain separate."}));return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
