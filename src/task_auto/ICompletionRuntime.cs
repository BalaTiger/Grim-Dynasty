using GrimDynasty.TokenActions;
namespace GrimDynasty.TaskAuto {
 // Explicit evidenced completion action provider, not native arbitrary-script support.
 public interface ICompletionRuntime {
  MutationResult Check(AutoSnapshot snapshot);
  MutationResult BeforeRewardHistory(AutoSnapshot snapshot);
  MutationResult AfterRewardHistory(AutoSnapshot snapshot);
 }
}
