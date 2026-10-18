using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using GrimDynasty.Resources;
using GrimDynasty.Quests;
using GrimDynasty.TaskAuto;
using GrimDynasty.Rewards;
public static class MoneyCatalogProbe {
 public static int Main(string[] args){try{
  if(args.Length!=1)throw new ArgumentException("MoneyCatalogProbe <authorized Quests.arc>");int quests=0,actions=0,negative=0,zero=0;var examples=new List<object>();
  using(var input=ResourceInput.OpenGameResource(args[0]))using(var arc=new ArcArchive(input))foreach(var entry in arc.Entries.Values){var q=QuestDefinition.Read(arc.ReadMember(entry.name));quests++;
   foreach(Dictionary<string,object> t in (List<object>)q["tasks"])foreach(string group in new[]{"on_accept","objectives","on_complete"})foreach(Dictionary<string,object> node in (List<object>)t[group])foreach(Dictionary<string,object> row in (List<object>)node["actions"])if(Convert.ToInt32(row["type_id"])==4){
    if(Convert.ToInt32(row["version"])!=0)throw new Exception("Unknown money action version");int amount=Convert.ToInt32(((Dictionary<string,object>)row["parameters"])["amount"]);if(amount<0)negative++;if(amount==0)zero++;
    var x=ExampleRewardQuest.Create(amount);var a=x.Quest.Auto;AutomaticTasks.Start(a);AutomaticTasks.Update(a);x.Quest.Kill();AutomaticTasks.Update(a);x.Quest.Kill();var result=AutomaticTasks.Update(a);
    if(result.Status!="Known"||a.Runtime.State.Task.State!=3||!x.Completion.Wallet.Money16C4.HasValue||x.Record.RewardTaskUidsAt50.Count!=1||x.Quest.Counter.CallbackOwnerAttached)throw new Exception("Completion/reward closure failed");actions++;
    if(examples.Count<4)examples.Add(new{member=entry.name,source_group=group,amount,initial_money=50,final_money=x.Completion.Wallet.Money16C4.Value,task_state=3,reward_records=1});
   }
  }
  if(quests!=247||actions!=254)throw new Exception("Official money action inventory mismatch");
  Console.WriteLine(new JavaScriptSerializer().Serialize(new{status="passed",quests,actual_money_parameters=actions,negative_parameters=negative,zero_parameters=zero,own_event_to_reward_chains=actions,examples,scope="Official amount parameters injected into own unconditional money completion fixture. Not original quest/event conditions or original dynamic reward oracle. Numeric rules validated by independent fixed static-native assertions."}));return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
