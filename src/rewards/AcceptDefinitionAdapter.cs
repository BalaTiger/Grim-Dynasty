using System;
using System.Collections.Generic;
using GrimDynasty.TaskLogic;
using GrimDynasty.TaskAuto;
using GrimDynasty.TaskItems;
using GrimDynasty.TokenActions;
namespace GrimDynasty.Rewards {
 public sealed class AcceptAction {public int TypeId,Version;public uint TutorialPage;public MoneyAction Money;}
 public sealed class AcceptEvent {public bool ConditionsKnown;public Group Conditions;public List<AcceptAction> Actions;public readonly Dictionary<Predicate,HasItemCondition> ItemConditions=new Dictionary<Predicate,HasItemCondition>();}
 public static class AcceptDefinitionAdapter {
  public static MutationResult ReadWithItems(Dictionary<string,object> task,uint expectedTaskUid,OfflineEvents bus,ItemWorld stock,out List<AcceptEvent> events){
   events=null;List<AcceptEvent> copy;var decoded=Read(task,expectedTaskUid,out copy);if(decoded.Status!="Known")return decoded;
   try{
    var nodes=(List<object>)task["on_accept"];
    for(int i=0;i<nodes.Count;i++){
     var rows=(List<object>)((Dictionary<string,object>)((Dictionary<string,object>)nodes[i])["conditions"])["conditions"];
     if(copy[i].Conditions==null){if(rows.Count!=0)return MutationResult.Unsupported("AcceptItemConditionShapeMismatch");continue;}
     for(int j=0;j<rows.Count;j++){
      var row=(Dictionary<string,object>)rows[j];if(Convert.ToInt32(row["type_id"])!=9)continue;
      Predicate p;HasItemCondition item;var read=HasItemDefinitionAdapter.Read(row,bus,stock,out p,out item);if(read.Status!="Known")return read;
      copy[i].Conditions.Conditions[j]=p;copy[i].ItemConditions.Add(p,item);
     }
    }
    events=copy;return MutationResult.Known(false);
   }catch(Exception e){if(e is KeyNotFoundException||e is InvalidCastException||e is OverflowException||e is ArgumentException||e is NullReferenceException)return MutationResult.Unsupported("MalformedAcceptItemDefinition:"+e.GetType().Name);throw;}
  }
  public static MutationResult Read(Dictionary<string,object> task,uint expectedTaskUid,out List<AcceptEvent> events){
   events=null;
   try{
    if(task==null||Convert.ToUInt32(task["uid"])!=expectedTaskUid)return MutationResult.Unsupported("AcceptTaskUidMismatch");
    var result=new List<AcceptEvent>();
    foreach(Dictionary<string,object> node in (List<object>)task["on_accept"]){
     var e=new AcceptEvent{ConditionsKnown=true,Conditions=DefinitionAdapter.ReadGroup((Dictionary<string,object>)node["conditions"]),Actions=new List<AcceptAction>()};
     foreach(Dictionary<string,object> row in (List<object>)node["actions"]){
      var a=new AcceptAction{TypeId=Convert.ToInt32(row["type_id"]),Version=Convert.ToInt32(row["version"])};
      // Retain unknown action identity so a false event gate can skip it.
      if(a.TypeId==4||a.TypeId==27){var fields=(Dictionary<string,object>)row["parameters"];int value=Convert.ToInt32(fields[a.TypeId==4?"amount":"page"]);if(a.TypeId==4)a.Money=new MoneyAction{Version=a.Version,Amount=value};else a.TutorialPage=unchecked((uint)value);}
      e.Actions.Add(a);
     }result.Add(e);
    }
    events=result;return MutationResult.Known(false);
   }catch(Exception e){if(e is KeyNotFoundException||e is InvalidCastException||e is OverflowException||e is ArgumentException||e is NullReferenceException)return MutationResult.Unsupported("MalformedAcceptDefinition:"+e.GetType().Name);throw;}
  }
 }
}
