using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using GrimDynasty.Resources;
using GrimDynasty.Quests;
using GrimDynasty.TaskLogic;
using GrimDynasty.TaskAuto;
using GrimDynasty.TaskItems;
public static class ItemCatalogProbe {
 public static int Main(string[] args){try{
  if(args.Length!=1)throw new ArgumentException("ItemCatalogProbe <authorized Quests.arc>");int quests=0,conditions=0,equal=0,greaterEqual=0;var names=new HashSet<string>(StringComparer.Ordinal);
  using(var input=ResourceInput.OpenGameResource(args[0]))using(var arc=new ArcArchive(input))foreach(var entry in arc.Entries.Values){var q=QuestDefinition.Read(arc.ReadMember(entry.name));quests++;
   foreach(Dictionary<string,object> t in (List<object>)q["tasks"])foreach(string group in new[]{"on_accept","objectives","on_complete"})foreach(Dictionary<string,object> node in (List<object>)t[group])foreach(Dictionary<string,object> row in (List<object>)((Dictionary<string,object>)node["conditions"])["conditions"])if(Convert.ToInt32(row["type_id"])==9){
    var values=(Dictionary<string,object>)row["parameters"];string name=(string)values["dbr"];uint target=Convert.ToUInt32(values["count"]);var stock=ItemWorld.Empty();stock.AsciiCLocaleKnown=true;stock.ClientPlayerId=1;stock.Objects.Add(100,new ItemObject{Kind=ItemObjectKind.Item,ObjectName=name,DataNameFromSlot590=name,PrefixFromSlot590="",SuffixFromSlot590="",CompleteByteFromSlot3F8=1,QuantityFromSlot618=0});stock.InventoryItemIds.Add(100);var bus=new OfflineEvents(true){DebugEnabled=false};Predicate p;HasItemCondition c;var read=HasItemDefinitionAdapter.Read(row,bus,stock,out p,out c);if(read.Status!="Known")throw new Exception(read.Reason);
    if(p.Version!=1||target<1||target>10000||(p.ComparisonId!=2&&p.ComparisonId!=3))throw new Exception("Official item domain changed");if(p.ComparisonId==2)equal++;else greaterEqual++;names.Add(name);var player=new World{MainPlayerResolved=true};if(c.Evaluate(player).Value!=false)throw new Exception("initial empty item count");c.Start();stock.Objects[100].QuantityFromSlot618=(int)target;
    if(bus.Send(new QuestEvent{Kind=16,Body=new ItemEventBody{PlayerId=1,ItemObjectId=100}},16).Delivered!=1||c.Evaluate(player).Value!=true||c.ChangedByte14!=1)throw new Exception("item-event/query closure");c.End();if(bus.Send(new QuestEvent(),16).Delivered!=0||bus.Send(new QuestEvent(),17).Delivered!=0)throw new Exception("item end lifecycle");c.Reset();if(c.Evaluate(player).Value!=true||stock.Objects[100].QuantityFromSlot618!=(int)target)throw new Exception("reset changed stock");conditions++;
   }
  }
  if(quests!=247||conditions!=84||equal!=2||greaterEqual!=82)throw new Exception("Official item inventory mismatch");
  Console.WriteLine(new JavaScriptSerializer().Serialize(new{status="passed",quests,conditions,equal_conditions=equal,greater_equal_conditions=greaterEqual,unique_records=names.Count,scope="Actual HasItem parameters, own declared inventory getter snapshots and decoded events. No native loot/GiveItem, real stashes/saves, network or original DLL execution."}));return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
