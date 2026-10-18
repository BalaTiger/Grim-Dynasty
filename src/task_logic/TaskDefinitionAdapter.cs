using System;
using System.Collections.Generic;
using GrimDynasty.Conditions;
namespace GrimDynasty.TaskLogic {
 public static class DefinitionAdapter {
  public static Group ReadGroup(Dictionary<string,object> d){
   var rows=(List<object>)d["conditions"];object op=d["operator"];
   if(op==null){if(rows.Count!=0)throw new ArgumentException("Null operator with nonempty conditions");return null;}
   var g=new Group{Operation=(string)op=="And"?0:(string)op=="Or"?1:-1};
   foreach(Dictionary<string,object> row in rows)g.Conditions.Add(ReadPredicate(row));return g;
  }
  public static Predicate ReadPredicate(Dictionary<string,object> row){
   var p=new Predicate{TypeId=Convert.ToInt32(row["type_id"]),Version=Convert.ToInt32(row["version"])};
   object c;if(row.TryGetValue("comparison_id",out c))p.ComparisonId=Convert.ToInt32(c);
   var a=(Dictionary<string,object>)row["parameters"];
   switch(p.TypeId){
    case 0:p.Reference=(string)a["quest"];p.Expected=(bool)a["on"];break;
    case 1:p.Reference=(string)a["quest"];p.TaskUid=Convert.ToUInt32(a["task"]);p.Expected=(bool)a["complete"];break;
    case 3:p.Value=Convert.ToUInt32(a["level"]);break;
    case 4:p.Value=Convert.ToUInt32(a["difficulty"]);break;
    case 5:p.Expected=(bool)a["hardcore"];break;
    case 7:case 18:case 20:p.Reference=(string)a["token"];p.Expected=(bool)a["has"];break;
    case 11:p.Reference=(string)a["questFile"];p.TaskUid=Convert.ToUInt32(a["taskUid"]);if(p.Version==1)p.ObjectiveUid=Convert.ToUInt32(a["objectiveUid"]);p.Expected=(bool)a["satisfied"];break;
    case 14:p.Reference=(string)a["quest"];p.TaskUid=Convert.ToUInt32(a["task"]);p.Expected=(bool)a["on"];break;
    case 19:p.Reference=(string)a["quest"];p.Expected=(bool)a["blocked"];break;
   }return p;
  }
  public static EvaluationResult Event(Dictionary<string,object> d,World w){
   try{return Evaluator.EventGate(ReadGroup(d),true,w);}catch(Exception e){if(e is KeyNotFoundException||e is InvalidCastException||e is OverflowException||e is ArgumentException||e is NullReferenceException)return EvaluationResult.Unsupported("MalformedDefinition:"+e.GetType().Name);throw;}
  }
 }
}
