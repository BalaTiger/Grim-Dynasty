// Own pure implementation of a statically evidenced condition subset.
// No original DLL, Steam API, Entity, save, network or OS state access.
using System;
using System.Collections.Generic;
namespace GrimDynasty.Conditions {
 public sealed class SimulatedContext {
  public uint? Difficulty;
  public byte? MainPlayerHardcoreByte;
 }
 public sealed class ConditionSpec {
  public int TypeId,Version,ComparisonId;
  public uint StoredDifficulty;
  public uint DifficultyOverride=uint.MaxValue;
  public bool ExpectedHardcore;
 }
 public sealed class EvaluationResult {
  public string Status;
  public bool? Value;
  public string Reason;
  public readonly List<int> Evaluated=new List<int>();
  public readonly List<int> Skipped=new List<int>();
  public static EvaluationResult Known(bool value){return new EvaluationResult{Status="Known",Value=value};}
  public static EvaluationResult Unsupported(string reason){return new EvaluationResult{Status="Unsupported",Value=null,Reason=reason};}
 }
 public static class ConditionEvaluator {
  // Native IsDifficulty compares [this+0x20] against actual/override with signed
  // SETL/SETLE/SETE/SETGE/SETG/SETNE. It does not compare actual against stored.
  public static EvaluationResult Evaluate(ConditionSpec condition,SimulatedContext context){
   if(condition==null)return EvaluationResult.Unsupported("MissingCondition");
   if(condition.Version!=0)return EvaluationResult.Unsupported("ConditionVersion:"+condition.Version);
   if(condition.TypeId==17)return EvaluationResult.Known(false);
   if(condition.TypeId==5){if(context==null||!context.MainPlayerHardcoreByte.HasValue)return EvaluationResult.Unsupported("MissingSimulatedMainPlayerHardcoreByte");return EvaluationResult.Known((condition.ExpectedHardcore?(byte)1:(byte)0)==context.MainPlayerHardcoreByte.Value);}
   if(condition.TypeId!=4)return EvaluationResult.Unsupported("ConditionType:"+condition.TypeId);
   if(context==null||!context.Difficulty.HasValue)return EvaluationResult.Unsupported("MissingSimulatedDifficulty");
   uint actual=condition.DifficultyOverride==uint.MaxValue?context.Difficulty.Value:condition.DifficultyOverride;
   int stored=unchecked((int)condition.StoredDifficulty),observed=unchecked((int)actual);bool value;
   switch(condition.ComparisonId){case 0:value=stored<observed;break;case 1:value=stored<=observed;break;case 2:value=stored==observed;break;case 3:value=stored>=observed;break;case 4:value=stored>observed;break;case 5:value=stored!=observed;break;default:return EvaluationResult.Unsupported("Comparison:"+condition.ComparisonId);}
   return EvaluationResult.Known(value);
  }
  public static EvaluationResult Collection(IList<ConditionSpec> conditions,int operation,bool skipWaitForCompletion,SimulatedContext context){
   if(conditions==null)return EvaluationResult.Unsupported("MissingConditionList");
   if(operation!=0&&operation!=1)return EvaluationResult.Unsupported("CollectionOperator:"+operation);
   var trace=EvaluationResult.Known(operation==0);
   for(int i=0;i<conditions.Count;i++){
    var condition=conditions[i];if(condition==null){trace.Status="Unsupported";trace.Value=null;trace.Reason="MissingCondition:"+i;return trace;}
    if(skipWaitForCompletion&&condition.TypeId==17){trace.Skipped.Add(i);continue;}
    trace.Evaluated.Add(i);var result=Evaluate(condition,context);
    if(result.Status!="Known"){trace.Status=result.Status;trace.Value=null;trace.Reason=result.Reason;return trace;}
    if(operation==0&&!result.Value.Value){trace.Value=false;return trace;}
    if(operation==1&&result.Value.Value){trace.Value=true;return trace;}
   }return trace;
  }
 }
}
