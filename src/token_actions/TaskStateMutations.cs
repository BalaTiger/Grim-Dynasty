using System;
using System.Collections.Generic;
using GrimDynasty.TaskLogic;
namespace GrimDynasty.TokenActions {
 public sealed class TaskMutationSnapshot {
  public TaskState Task;public uint? Difficulty,QuestFlags;
  public int[] StatesByDifficulty;
  // null means unknown. Only the empty event list is in the restored domain.
  public int? OnAcceptEventCount;
 }
 public static class TaskMutations {
  static MutationResult U(string why){return MutationResult.Unsupported(why);}
  static bool Difficulty(TaskMutationSnapshot s){return s.Difficulty.HasValue&&s.Difficulty.Value<3;}
  static bool EmptyObjectives(TaskMutationSnapshot s){List<Objective> list;return Difficulty(s)&&s.Task.Objectives.TryGetValue(s.Difficulty.Value,out list)&&list!=null&&list.Count==0;}
  public static MutationResult Start(TaskMutationSnapshot s){
   if(s==null||s.Task==null)return U("MissingTaskSnapshot");var task=s.Task;
   if(task.State==1&&task.RuntimeBlocked==0){
    if(!s.QuestFlags.HasValue)return U("UnknownQuestFlags");
    if((s.QuestFlags.Value&4)!=0)return U("StartRequiresTutorialSideEffect");
    if(!s.OnAcceptEventCount.HasValue||s.OnAcceptEventCount.Value!=0)return U("StartRequiresOnAcceptExecution");
   }
   if(task.RuntimeBlocked==0&&!EmptyObjectives(s))return U("StartRequiresObjectiveLifecycleCallbacks");
   bool changed=task.State!=2;task.State=2;return MutationResult.Known(changed);
  }
  public static MutationResult CompleteWithoutCallbacks(TaskMutationSnapshot s,bool executeRewards){
   if(s==null||s.Task==null)return U("MissingTaskSnapshot");
   // Native old-state2 always invokes lifecycle and outbound completion, even
   // with rewards=false. Do not partially mutate and pretend it completed.
   if(s.Task.State==2)return U("CompleteActiveRequiresCallbacksAndOutboundDispatch");
   if(!Difficulty(s)||s.StatesByDifficulty==null||s.StatesByDifficulty.Length!=3)return U("UnknownDifficultyStateBank");
   int next=3;if(s.Task.State==1){if(!s.QuestFlags.HasValue)return U("UnknownQuestFlags");if((s.QuestFlags.Value&4)!=0)next=1;}
   int d=(int)s.Difficulty.Value;bool changed=s.Task.State!=next||s.StatesByDifficulty[d]!=next;
   s.Task.State=next;s.StatesByDifficulty[d]=next;return MutationResult.Known(changed);
  }
  public static MutationResult Reset(TaskMutationSnapshot s){
   if(s==null||s.Task==null)return U("MissingTaskSnapshot");if(s.Task.State!=3)return MutationResult.Known(false);
   if(!Difficulty(s)||s.StatesByDifficulty==null||s.StatesByDifficulty.Length!=3)return U("UnknownDifficultyStateBank");
   if(!EmptyObjectives(s))return U("ResetRequiresObjectiveCounterAndLifecycleSemantics");
   s.Task.State=1;s.StatesByDifficulty[(int)s.Difficulty.Value]=1;return MutationResult.Known(true);
  }
 }
}
