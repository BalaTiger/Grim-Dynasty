using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using GrimDynasty.Resources;
using GrimDynasty.Quests;
using GrimDynasty.TaskLogic;
using GrimDynasty.TaskAuto;
using GrimDynasty.TaskKills;
public static class KillCatalogProbe {
 public static int Main(string[] args){try{
  if(args.Length!=1)throw new ArgumentException("KillCatalogProbe <authorized Quests.arc>");int quests=0,conditions=0,eventsSent=0;var names=new HashSet<string>(StringComparer.Ordinal);
  using(var input=ResourceInput.OpenGameResource(args[0]))using(var arc=new ArcArchive(input))foreach(var entry in arc.Entries.Values){var q=QuestDefinition.Read(arc.ReadMember(entry.name));quests++;
   foreach(Dictionary<string,object> t in (List<object>)q["tasks"])foreach(string key in new[]{"on_accept","objectives","on_complete"})foreach(Dictionary<string,object> node in (List<object>)t[key])foreach(Dictionary<string,object> row in (List<object>)((Dictionary<string,object>)node["conditions"])["conditions"])if(Convert.ToInt32(row["type_id"])==10){
    var bus=new OfflineEvents(true){DebugEnabled=false};var world=new KillWorld{IndexComplete=true};world.Entities.Add(1,new EntitySnapshot{Kind=EntityKind.Player});world.Entities.Add(2,new EntitySnapshot{Kind=EntityKind.Monster,RawByte20A4=0});Predicate p;KillCondition c;var read=KillDefinitionAdapter.Read(row,bus,world,out p,out c);if(read.Status!="Known")throw new Exception(read.Reason);
    if(p.Version!=1||p.ComparisonId!=3||p.Value<1||p.Value>10000||c.NormalizedNames.Count==0)throw new Exception("Official HasKilled parameter domain changed");foreach(string name in c.NormalizedNames)names.Add(name);if(c.Evaluate(null).Value!=false)throw new Exception("Initial count predicate");
    c.Start();for(uint i=0;i<p.Value;i++){var result=bus.Send(new QuestEvent{Kind=11,Body=new KillEventBody{SourceId=1,VictimId=2,RecordName=c.NormalizedNames[0]}},11);if(result.Status!="Known"||result.Delivered!=1||c.Current!=i+1)throw new Exception("Kill dispatch/count closure");eventsSent++;}
    if(c.Evaluate(null).Value!=true)throw new Exception("Threshold predicate");c.End();if(bus.Send(new QuestEvent{Kind=11},11).Delivered!=0)throw new Exception("End lifecycle unsubscribe");c.Reset();if(c.Current!=0||c.Evaluate(null).Value!=false||c.ChangedByte14!=1||c.TextByte15!=1)throw new Exception("Reset flags");conditions++;
   }
  }
  if(quests!=247||conditions!=160||names.Count!=193||eventsSent!=368)throw new Exception("Official HasKilled inventory mismatch");
  Console.WriteLine(new JavaScriptSerializer().Serialize(new{status="passed",quests,conditions,unique_normalized_names=names.Count,decoded_events_dispatched=eventsSent,scope="Actual HasKilled v1 parameters; explicit player/monster entity snapshots, event-name payload and own event manager. No original combat, quest reward execution, DLL, saves or network."}));return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
