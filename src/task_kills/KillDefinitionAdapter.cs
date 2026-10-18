using System;
using System.Collections.Generic;
using GrimDynasty.TaskLogic;
using GrimDynasty.TaskAuto;
using GrimDynasty.TokenActions;
namespace GrimDynasty.TaskKills {
 public static class KillDefinitionAdapter {
  public static MutationResult Read(Dictionary<string,object> row,OfflineEvents events,KillWorld entities,out Predicate predicate,out KillCondition condition){
   predicate=null;condition=null;
   try{
    int type=Convert.ToInt32(row["type_id"]),version=Convert.ToInt32(row["version"]);if(type!=10)return MutationResult.Unsupported("NotHasKilledDefinition");
    object raw;int comparison;if(row.TryGetValue("comparison_id",out raw))comparison=Convert.ToInt32(raw);else{string text=Convert.ToString(row["comparison"]);comparison=Array.IndexOf(new[]{"Less","LessEqual","Equal","GreaterEqual","Greater","NotEqual"},text);}
    var values=(Dictionary<string,object>)row["parameters"];uint required=Convert.ToUInt32(values["count"]);var names=new List<string>();foreach(object name in (System.Collections.IList)values["names"])names.Add((string)name);
    var result=KillCondition.Create(version,comparison,required,names,false,events,entities,out condition);if(result.Status!="Known")return result;
    predicate=new Predicate{TypeId=10,Version=version,ComparisonId=comparison,Value=required};return result;
   }catch(Exception e){if(e is KeyNotFoundException||e is InvalidCastException||e is OverflowException||e is NullReferenceException)return MutationResult.Unsupported("MalformedKillDefinition:"+e.GetType().Name);throw;}
  }
 }
}
