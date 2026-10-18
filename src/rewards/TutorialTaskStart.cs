using System;
using GrimDynasty.TokenActions;
using GrimDynasty.TaskAuto;
namespace GrimDynasty.Rewards {
 public sealed class TutorialTaskStart : IStartTutorialRuntime {
  public TutorialSnapshot State;
  public MutationResult Check(AutoSnapshot a){
   if(a==null||State==null)return MutationResult.Unsupported("UnknownStartTutorialContext");
   if(State.Events!=null&&!Object.ReferenceEquals(State.Events,a.Events))return MutationResult.Unsupported("StartTutorialEventManagerMismatch");
   return TutorialRules.CheckUnlock(State,0x37,true);
  }
  public MutationResult Execute(AutoSnapshot a){var ready=Check(a);return ready.Status=="Known"?TutorialRules.Unlock(State,0x37,true):ready;}
 }
}
