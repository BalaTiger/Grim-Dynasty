using System;
using System.Collections.Generic;
using GrimDynasty.TaskLogic;
using GrimDynasty.TokenActions;
using GrimDynasty.TaskAuto;
namespace GrimDynasty.Rewards {
 public sealed class ExperienceEvent {
  public NumericEvent Gate;
  public List<ExperienceAction> Actions;
 }
 public sealed class ExperienceCompletion : ICompletionRuntime {
  public ExperienceWorld World;
  public List<ExperienceEvent> Events;
  public bool QuestUidIndexComplete;
  public readonly Dictionary<uint,RewardQuestSnapshot> QuestsByUid=new Dictionary<uint,RewardQuestSnapshot>();
  bool activeReward;
  MutationResult Validate(AutoSnapshot a){
   if(a==null||a.Runtime==null||a.Runtime.State==null||a.Runtime.State.Task==null||a.Runtime.World==null||a.Runtime.World.Core==null||World==null||Events==null||a.OnCompleteEventCount!=Events.Count)return MutationResult.Unsupported("UnknownExperienceCompletionContext");
   if(!a.Runtime.World.MainPlayerResolved||!World.MainPlayerQueryKnown||!World.MainPlayerId.HasValue||!a.QuestUid.HasValue||a.Runtime.State.Difficulty!=a.Runtime.World.Core.Difficulty)return MutationResult.Unsupported("ExperienceCompletionIdentityOrDifficultyMismatch");
   ExperienceActor main;if(!World.Actors.TryGetValue(World.MainPlayerId.Value,out main)||main==null||main.ObjectId!=World.MainPlayerId.Value||main.IsPlayer!=true)return MutationResult.Unsupported("ExperienceCompletionMainPlayerSnapshotMismatch");
   if(World.Events!=null&&!Object.ReferenceEquals(World.Events,a.Events))return MutationResult.Unsupported("ExperienceCompletionEventManagerMismatch");
   if(a.Runtime.World.Level.HasValue&&main.Level1760.HasValue&&a.Runtime.World.Level!=main.Level1760)return MutationResult.Unsupported("ExperienceCompletionLevelProjectionMismatch");
   var gates=new List<NumericEvent>();foreach(var e in Events){if(e==null||e.Gate==null||(e.Gate.Actions!=null&&e.Gate.Actions.Count!=0))return MutationResult.Unsupported("UnknownOrMixedExperienceCompletionEvent");gates.Add(e.Gate);}
   var adjustment=RewardAdjustment.Calculate(gates,a.Runtime.State.Difficulty,a.RewardRecordedA3,a.MainPlayerFlags17F4);if(adjustment.Status!="Known")return MutationResult.Unsupported(adjustment.Reason);
   var preview=World.Copy();bool reward=false;
   foreach(var e in Events){var active=RewardAdjustment.Evaluate(e.Gate,a.Runtime.State.Difficulty.Value,uint.MaxValue);if(active.Status!="Known")return MutationResult.Unsupported(active.Reason);if(!active.Value.Value||e.Actions==null)continue;
    foreach(var action in e.Actions){var result=ExperienceRewards.Execute(preview,action);if(result.Status!="Known")return result;reward=true;}
   }
   if(reward){RewardQuestSnapshot q;if(!QuestsByUid.TryGetValue(a.QuestUid.Value,out q)){if(!QuestUidIndexComplete)return MutationResult.Unsupported("IncompleteRewardQuestUidIndex");}
    else{QuestState byName;if(q==null||q.RewardTaskUidsAt50==null||a.Runtime.QuestReference==null||!a.Runtime.World.Quests.TryGetValue(a.Runtime.QuestReference,out byName)||!Object.ReferenceEquals(q.Quest,byName))return MutationResult.Unsupported("RewardQuestIndexSnapshotMismatch");}}
   return MutationResult.Known(false);
  }
  public MutationResult Check(AutoSnapshot a){return Validate(a);}
  public MutationResult BeforeRewardHistory(AutoSnapshot a){
   var valid=Validate(a);if(valid.Status!="Known")return valid;activeReward=false;bool changed=false;
   // GiveExperience's ModifyValue hook is RET; adjustment never scales Amount.
   foreach(var e in Events){var active=RewardAdjustment.Evaluate(e.Gate,a.Runtime.State.Difficulty.Value,uint.MaxValue);if(!active.Value.Value||e.Actions==null)continue;
    foreach(var action in e.Actions){activeReward=true;var r=ExperienceRewards.Execute(World,action);if(r.Status!="Known")throw new InvalidOperationException("Experience snapshot changed during commit: "+r.Reason);changed|=r.Changed.Value;}
   }ExperienceActor main;if(World.MainPlayerId.HasValue&&World.Actors.TryGetValue(World.MainPlayerId.Value,out main)&&main!=null&&main.Level1760.HasValue){changed|=a.Runtime.World.Level!=main.Level1760;a.Runtime.World.Level=main.Level1760;}return MutationResult.Known(changed);
  }
  public MutationResult AfterRewardHistory(AutoSnapshot a){RewardQuestSnapshot q;if(activeReward&&QuestsByUid.TryGetValue(a.QuestUid.Value,out q)){q.RewardTaskUidsAt50.Add(a.Runtime.State.Task.Uid);return MutationResult.Known(true);}return MutationResult.Known(false);}
 }
}
