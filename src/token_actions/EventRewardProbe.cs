using System;
using System.Collections.Generic;
using GrimDynasty.Conditions;
using GrimDynasty.TaskLogic;
namespace GrimDynasty.TokenActions {
 public sealed class EventSnapshot {
  public bool ConditionPointerKnown,ActionPointerKnown;
  public Group Conditions;
  public List<int?> ActionTypes;
 }
 public static class EventRewardProbe {
  // Quest2Task::HasRewardActions(vector<Quest2Event*>) 0x180427700.
  // Preserve evaluation order: Evaluate event BEFORE checking its action ptr.
  public static EvaluationResult HasRewardActions(IList<EventSnapshot> events,World world){
   if(events==null)return EvaluationResult.Unsupported("UnknownEventVector");
   for(int i=0;i<events.Count;i++){
    var e=events[i];if(e==null)return EvaluationResult.Unsupported("UnknownEvent:"+i);
    var gate=Evaluator.EventGate(e.Conditions,e.ConditionPointerKnown,world);
    if(gate.Status!="Known")return gate;if(!gate.Value.Value)continue;
    if(!e.ActionPointerKnown)return EvaluationResult.Unsupported("UnknownActionPointer:"+i);
    if(e.ActionTypes==null)continue;
    var r=RewardClassifier.HasRewardActions(e.ActionTypes);if(r.Status!="Known"||r.Value.Value)return r;
   }return EvaluationResult.Known(false);
  }
 }
}
