using System;
using System.Collections.Generic;
using GrimDynasty.Conditions;
namespace GrimDynasty.TaskLogic {
 // Explicit, complete simulated snapshots. No process, game DLL or save access.
 public sealed class Predicate {
  public int TypeId,Version,ComparisonId;
  public uint Value,TaskUid,ObjectiveUid;
  public string Reference;
  public bool Expected;
 }
 public sealed class Group {public int Operation;public readonly List<Predicate> Conditions=new List<Predicate>();}
 public sealed class Objective {public uint Uid;public bool ConditionsKnown;public Group Conditions;}
 public sealed class TaskState {
  public uint Uid;public int State;public byte RuntimeBlocked;
  public readonly Dictionary<uint,List<Objective>> Objectives=new Dictionary<uint,List<Objective>>();
 }
 public sealed class QuestState {public readonly List<TaskState> Tasks=new List<TaskState>();}
 public sealed class World {
  public SimulatedContext Core=new SimulatedContext();
  public uint? Level,GameMode;public bool MainPlayerResolved;
  public bool? ServerServiceSlot18NonNull;
  public bool QuestIndexComplete,RepositoryTokensComplete;
  public uint? ServerPlayerId;
  public readonly Dictionary<string,QuestState> Quests=new Dictionary<string,QuestState>(StringComparer.Ordinal);
  public readonly Dictionary<uint,List<string>> ModeOneTokens=new Dictionary<uint,List<string>>(),OtherModeTokens=new Dictionary<uint,List<string>>(),RepositoryTokens=new Dictionary<uint,List<string>>();
 }
 public static class Evaluator {
  static EvaluationResult U(string reason){return EvaluationResult.Unsupported(reason);}
  static EvaluationResult K(bool v){return EvaluationResult.Known(v);}
  static bool IsKnown(EvaluationResult r){return r.Status=="Known";}
  public static EvaluationResult EventGate(Group group,bool pointerKnown,World w){return !pointerKnown?U("UnknownEventConditionPointer"):group==null?K(true):Collection(group,false,w,0);}
  public static EvaluationResult Evaluate(Predicate p,World w){return Evaluate(p,w,0);}
  public static EvaluationResult Collection(Group g,bool skipWait,World w){return Collection(g,skipWait,w,0);}
  static EvaluationResult Collection(Group g,bool skipWait,World w,int depth){
   if(g==null)return U("MissingCollection");if(depth>64)return U("RecursionDomainExceeded");
   if(g.Operation!=0&&g.Operation!=1)return U("CollectionOperator:"+g.Operation);
   var r=K(g.Operation==0);
   for(int i=0;i<g.Conditions.Count;i++){
    var p=g.Conditions[i];if(p==null)return U("MissingCondition");
    if(skipWait&&p.TypeId==17){r.Skipped.Add(i);continue;}r.Evaluated.Add(i);
    var leaf=Evaluate(p,w,depth+1);if(!IsKnown(leaf)){r.Status=leaf.Status;r.Value=null;r.Reason=leaf.Reason;return r;}
    if((g.Operation==0&&!leaf.Value.Value)||(g.Operation==1&&leaf.Value.Value)){r.Value=leaf.Value;return r;}
   }return r;
  }
  static EvaluationResult Evaluate(Predicate p,World w,int depth){
   if(p==null)return U("MissingCondition");if(depth>64)return U("RecursionDomainExceeded");
   if(p.Version!=0&&!(p.TypeId==11&&p.Version==1))return U("ConditionVersion:"+p.Version);
   if(p.TypeId==4||p.TypeId==5||p.TypeId==17)return ConditionEvaluator.Evaluate(new ConditionSpec{TypeId=p.TypeId,Version=p.Version,ComparisonId=p.ComparisonId,StoredDifficulty=p.Value,ExpectedHardcore=p.Expected},w==null?null:w.Core);
   if(w==null)return U("MissingWorld");
   if(p.TypeId==3){
    if(!w.MainPlayerResolved||!w.Level.HasValue)return U("MissingResolvedPlayerLevel");uint a=w.Level.Value,b=p.Value;bool v;
    switch(p.ComparisonId){case 0:v=a<b;break;case 1:v=a<=b;break;case 2:v=a==b;break;case 3:v=a>=b;break;case 4:v=a>b;break;case 5:v=a!=b;break;default:return U("Comparison:"+p.ComparisonId);}return K(v);
   }
   if(p.TypeId==7||p.TypeId==18||p.TypeId==20)return Token(p,w);
   if(p.TypeId!=0&&p.TypeId!=1&&p.TypeId!=11&&p.TypeId!=14&&p.TypeId!=19)return U("ConditionType:"+p.TypeId);
   if(p.Reference==null)return U("MissingQuestReference");QuestState q;
   if(!w.Quests.TryGetValue(p.Reference,out q)){
    if(!w.QuestIndexComplete)return U("IncompleteQuestIndex");return K(p.TypeId==14?!p.Expected:false);
   }
   if(q==null)return U("UnknownQuestSnapshot");
   if(p.TypeId==0){
    bool on=false;foreach(var t in q.Tasks){if(t==null)return U("UnknownTaskSnapshot");if(!w.ServerServiceSlot18NonNull.HasValue)return U("MissingServerServiceSlot18");if((w.ServerServiceSlot18NonNull.Value||t.RuntimeBlocked==0)&&t.State==2){on=true;break;}}return K(on==p.Expected);
   }
   if(p.TypeId==19){foreach(var t in q.Tasks){if(t==null)return U("UnknownTaskSnapshot");if(t.State!=3&&t.RuntimeBlocked!=0)return K(p.Expected);}return K(!p.Expected);}
   TaskState task=null;var ids=new HashSet<uint>();foreach(var t in q.Tasks){if(t==null)return U("UnknownTaskSnapshot");if(!ids.Add(t.Uid))return U("DuplicateTaskUidSnapshot");if(t.Uid==p.TaskUid)task=t;}
   if(task==null)return K(p.TypeId==14?!p.Expected:false);
   if(p.TypeId==1)return K((task.State==3)==p.Expected);
   if(!w.ServerServiceSlot18NonNull.HasValue)return U("MissingServerServiceSlot18");
   bool active=(w.ServerServiceSlot18NonNull.Value||task.RuntimeBlocked==0)&&task.State==2;
   if(p.TypeId==14)return K(active==p.Expected);
   if(!active)return K(false);
   if(w.Core==null||!w.Core.Difficulty.HasValue||w.Core.Difficulty.Value>2)return U("MissingOrOutOfDomainDifficulty");
   List<Objective> objectives;if(!task.Objectives.TryGetValue(w.Core.Difficulty.Value,out objectives)||objectives==null)return U("UnknownDifficultyObjectives");
   uint uid=p.Version==0?0:p.ObjectiveUid;
   if(uid==0){foreach(var obj in objectives){var r=ObjectiveGate(obj,true,w,depth+1);if(!IsKnown(r))return r;if(!r.Value.Value)return K(!p.Expected);}return K(p.Expected);}
   foreach(var obj in objectives){if(obj==null)return U("UnknownObjectiveSnapshot");if(obj.Uid==uid){var r=ObjectiveGate(obj,false,w,depth+1);return IsKnown(r)?K(r.Value.Value==p.Expected):r;}}
   return K(false);
  }
  static EvaluationResult ObjectiveGate(Objective o,bool skipWait,World w,int depth){if(o==null||!o.ConditionsKnown)return U("UnknownObjectiveConditions");return o.Conditions==null?K(true):Collection(o.Conditions,skipWait,w,depth);}
  static EvaluationResult Token(Predicate p,World w){
   if(p.Reference==null||!ValidUnicode(p.Reference))return U("TokenStringOutsideDecodedUtf8Domain");
   int nul=p.Reference.IndexOf('\0');string query=nul<0?p.Reference:p.Reference.Substring(0,nul);
   if(p.TypeId==7){
    if(!w.MainPlayerResolved||!w.GameMode.HasValue)return U("MissingResolvedPlayerOrMode");
    if(w.Core==null||!w.Core.Difficulty.HasValue||w.Core.Difficulty.Value>2)return U("MissingOrOutOfDomainDifficulty");
    List<string> bank;var banks=w.GameMode.Value==1?w.ModeOneTokens:w.OtherModeTokens;
    if(!banks.TryGetValue(w.Core.Difficulty.Value,out bank)||bank==null)return U("UnknownPlayerTokenBank");
    var r=Contains(bank,query);return IsKnown(r)?K(r.Value.Value==p.Expected):r;
   }
   if(!w.RepositoryTokensComplete)return U("IncompleteRepositoryTokenSnapshot");
   if(p.TypeId==18){
    if(!w.ServerPlayerId.HasValue)return U("UnknownServerPlayerId");List<string> bank;
    if(!w.RepositoryTokens.TryGetValue(w.ServerPlayerId.Value,out bank))return K(!p.Expected);
    var r=Contains(bank,query);return IsKnown(r)?K(r.Value.Value==p.Expected):r;
   }
   var keys=new List<uint>(w.RepositoryTokens.Keys);keys.Sort();
   foreach(uint key in keys){var r=Contains(w.RepositoryTokens[key],query);if(!IsKnown(r))return r;if(r.Value.Value)return K(p.Expected);}
   return K(!p.Expected);
  }
  static bool ValidUnicode(string s){for(int i=0;i<s.Length;i++){if(char.IsHighSurrogate(s[i])){if(++i>=s.Length||!char.IsLowSurrogate(s[i]))return false;}else if(char.IsLowSurrogate(s[i]))return false;}return true;}
  static EvaluationResult Contains(List<string> bank,string query){
   if(bank==null)return U("UnknownTokenVector");foreach(string token in bank){if(token==null||!ValidUnicode(token))return U("TokenStringOutsideDecodedUtf8Domain");if(String.Equals(token,query,StringComparison.Ordinal))return K(true);}return K(false);
  }
 }
}
