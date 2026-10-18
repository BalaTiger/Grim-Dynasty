using System;
using System.Collections.Generic;
using GrimDynasty.TaskLogic;
using GrimDynasty.TokenActions;
namespace GrimDynasty.TaskRuntime {
 public sealed class RuntimeSnapshot {
  public TaskMutationSnapshot State;
  public readonly Dictionary<Objective,int> ObjectiveCounters=new Dictionary<Objective,int>();
  public byte? DontPropagateByte;
  public bool[] CompletionRecorded;
  public World World;
  public string QuestReference;
  public OutboundSnapshot Outbound;
 }
 public sealed class OutboundSnapshot {
  // Explicit provider identity, not inferred from multiplayer/game mode.
  public bool ProviderIsKnownOutbound;
  public string LastQuestReference;public uint LastTaskUid;
 }
 public static class Lifecycle {
  static MutationResult U(string s){return MutationResult.Unsupported(s);}
  public static bool KnownNoOp(Predicate p){
   if(p==null||(p.Version!=0&&!(p.TypeId==11&&p.Version==1)))return false;
   switch(p.TypeId){case 0:case 1:case 3:case 4:case 5:case 7:case 11:case 14:case 17:case 18:case 19:case 20:return true;}return false;
  }
  public static MutationResult ValidateObjectives(RuntimeSnapshot r,bool countersRequired){
   if(r==null||r.State==null||r.State.Task==null)return U("MissingTaskSnapshot");var s=r.State;
   if(!s.Difficulty.HasValue||s.Difficulty.Value>2)return U("UnknownDifficulty");
   List<Objective> list;if(!s.Task.Objectives.TryGetValue(s.Difficulty.Value,out list)||list==null)return U("UnknownObjectiveVector");
   foreach(var o in list){if(o==null||!o.ConditionsKnown)return U("UnknownObjectiveConditionPointer");if(countersRequired&&!r.ObjectiveCounters.ContainsKey(o))return U("UnknownObjectiveCounter");
    if(o.Conditions!=null)foreach(var p in o.Conditions.Conditions)if(!KnownNoOp(p))return U("UnrestoredConditionLifecycle");
   }return MutationResult.Known(false);
  }
  public static MutationResult Start(RuntimeSnapshot r){
   if(r==null||r.State==null||r.State.Task==null)return U("MissingTaskSnapshot");var s=r.State;var t=s.Task;
   if(t.State==1&&t.RuntimeBlocked==0){if(!s.QuestFlags.HasValue)return U("UnknownQuestFlags");if((s.QuestFlags.Value&4)!=0)return U("StartRequiresTutorial");if(s.OnAcceptEventCount!=0)return U("StartRequiresOnAcceptExecution");}
   if(t.RuntimeBlocked==0){var v=ValidateObjectives(r,false);if(v.Status!="Known")return v;}
   bool changed=t.State!=2;t.State=2;return MutationResult.Known(changed);
  }
  public static MutationResult Reset(RuntimeSnapshot r){
   if(r==null||r.State==null||r.State.Task==null)return U("MissingTaskSnapshot");var s=r.State;if(s.Task.State!=3)return MutationResult.Known(false);
   if(s.StatesByDifficulty==null||s.StatesByDifficulty.Length!=3)return U("UnknownDifficultyStateBank");var v=ValidateObjectives(r,true);if(v.Status!="Known")return v;
   foreach(var o in s.Task.Objectives[s.Difficulty.Value])r.ObjectiveCounters[o]=0;
   s.Task.State=1;s.StatesByDifficulty[(int)s.Difficulty.Value]=1;return MutationResult.Known(true);
  }
  public static MutationResult CompleteNoRewards(RuntimeSnapshot r){
   if(r==null||r.State==null||r.State.Task==null)return U("MissingTaskSnapshot");var s=r.State;
   if(s.Task.State!=2)return TaskMutations.CompleteWithoutCallbacks(s,false);
   if(s.StatesByDifficulty==null||s.StatesByDifficulty.Length!=3||r.CompletionRecorded==null||r.CompletionRecorded.Length!=3)return U("UnknownCompletionStateBanks");
   var v=ValidateObjectives(r,false);if(v.Status!="Known")return v;
   if(r.Outbound==null||!r.Outbound.ProviderIsKnownOutbound)return U("UnknownCompletionDispatchProvider");
   if(r.World==null||r.QuestReference==null)return U("MissingOutboundRepositorySnapshot");
   QuestState q;TaskState target=null;
   if(!r.World.Quests.TryGetValue(r.QuestReference,out q)){if(!r.World.QuestIndexComplete)return U("IncompleteOutboundQuestIndex");}
   else {
    if(q==null)return U("UnknownOutboundQuest");var ids=new HashSet<uint>();
    foreach(var task in q.Tasks){if(task==null||!ids.Add(task.Uid))return U("InconsistentOutboundTaskIndex");if(task.Uid==s.Task.Uid)target=task;}
    if(target!=null){if(!Object.ReferenceEquals(target,s.Task))return U("OutboundTargetSnapshotMismatch");if(!r.DontPropagateByte.HasValue)return U("UnknownDontPropagateByte");if(r.DontPropagateByte.Value==0)return U("NetworkSendSemanticsNotRecovered");}
   }
   int d=(int)s.Difficulty.Value;s.Task.State=3;s.StatesByDifficulty[d]=3;r.CompletionRecorded[d]=true;
   r.Outbound.LastQuestReference=r.QuestReference;r.Outbound.LastTaskUid=s.Task.Uid;
   return MutationResult.Known(true);
  }
 }
}
