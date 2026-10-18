using System;
using System.Collections.Generic;
using GrimDynasty.TaskLogic;
using GrimDynasty.TaskAuto;
using GrimDynasty.TokenActions;
namespace GrimDynasty.TaskItems {
 public static class HasItemDefinitionAdapter {
  public static MutationResult Read(Dictionary<string,object> row,OfflineEvents bus,ItemWorld stock,out Predicate p,out HasItemCondition c){
   p=null;c=null;try{
    int version=Convert.ToInt32(row["version"]);if(Convert.ToInt32(row["type_id"])!=9)return MutationResult.Unsupported("NotHasItemDefinition");object raw;int cmp=row.TryGetValue("comparison_id",out raw)?Convert.ToInt32(raw):Array.IndexOf(new[]{"Less","LessEqual","Equal","GreaterEqual","Greater","NotEqual"},Convert.ToString(row["comparison"]));var values=(Dictionary<string,object>)row["parameters"];uint target=Convert.ToUInt32(values["count"]);bool flag=version==0?true:(bool)values["complete"];var result=HasItemCondition.Create(version,cmp,target,flag,(string)values["dbr"],bus,stock,out c);if(result.Status=="Known")p=new Predicate{TypeId=9,Version=version,ComparisonId=cmp,Value=target};return result;
   }catch(Exception e){if(e is KeyNotFoundException||e is InvalidCastException||e is OverflowException||e is NullReferenceException)return MutationResult.Unsupported("MalformedHasItemDefinition:"+e.GetType().Name);throw;}
  }
 }
}
