using GrimDynasty.Conditions;
using GrimDynasty.TaskLogic;
using GrimDynasty.TokenActions;
namespace GrimDynasty.TaskAuto {
 // Provider contract; equivalence applies only to independently evidenced
 // implementations, not to arbitrary callers implementing this interface.
 public interface IConditionRuntime {
  OfflineEvents EventManager {get;}
  bool Matches(Predicate p);
  EvaluationResult Evaluate(World world);
  byte ChangedByte14 {get;set;}
  byte TextByte15 {get;}
  MutationResult CheckStart();
  MutationResult Start();
  MutationResult CheckEnd();
  MutationResult End();
  MutationResult Reset();
  MutationResult CheckReset();
  // Covered providers must prove OnChanged has no visible side effect here.
  MutationResult CheckOnChangedNoDisplay();
 }
}
