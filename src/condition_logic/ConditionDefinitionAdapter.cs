using System;
using System.Collections.Generic;
namespace GrimDynasty.Conditions {
 public static class ConditionDefinitionAdapter {
  public static EvaluationResult Evaluate(Dictionary<string,object> definition,bool skipWaitForCompletion,SimulatedContext context){
   try{
    object op=definition["operator"];var list=(List<object>)definition["conditions"];
    // Serialized empty lists have no operator and native Quest2Event::Read does
    // not allocate a collection for count zero. Caller semantics are separate.
    if(op==null)return EvaluationResult.Unsupported("AbsentNativeConditionContainerRequiresCallerSemantics");
    int mode=(string)op=="And"?0:(string)op=="Or"?1:-1;
    var specs=new List<ConditionSpec>();foreach(object item in list){var row=(Dictionary<string,object>)item;int type=Convert.ToInt32(row["type_id"]);var s=new ConditionSpec{TypeId=type,Version=Convert.ToInt32(row["version"])};
     if(type==4){s.ComparisonId=Convert.ToInt32(row["comparison_id"]);s.StoredDifficulty=Convert.ToUInt32(((Dictionary<string,object>)row["parameters"])["difficulty"]);}
     else if(type==5)s.ExpectedHardcore=(bool)((Dictionary<string,object>)row["parameters"])["hardcore"];
     specs.Add(s);
    }return ConditionEvaluator.Collection(specs,mode,skipWaitForCompletion,context);
   }catch(KeyNotFoundException){return EvaluationResult.Unsupported("MalformedDefinitionField");}catch(InvalidCastException){return EvaluationResult.Unsupported("MalformedDefinitionType");}catch(OverflowException){return EvaluationResult.Unsupported("MalformedDefinitionRange");}catch(NullReferenceException){return EvaluationResult.Unsupported("MissingDefinition");}
  }
 }
}
