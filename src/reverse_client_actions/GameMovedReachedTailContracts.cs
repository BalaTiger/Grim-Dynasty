using System;
namespace GrimDynasty.ReverseClient.Actions {
 public sealed class GameMovedReachedTailInvocation {
  public GameMovedReachedContinuationInvocation Callback {get;private set;}
  public NavigationCrowdTickTailInvocation Tail {get;private set;}
  public bool FullTick {get{return false;}}
  public GameMovedReachedTailInvocation(){Reset();}
  internal void Reset(){Callback=new GameMovedReachedContinuationInvocation();Tail=new NavigationCrowdTickTailInvocation();}
 }
 // Adds only the final reached single-Work suffix to the unchanged passed
 // producer/callback composition. No callback exception is caught and no
 // previous frame's diagnostics are reused. This entry does not run the
 // prefix: returning25E2FB here alone does not establish execution from25C440.
 public static class GameMovedReachedTailContracts {
  public static void ProduceInvokeAndFinishReachedLastWork(NavigationCrowdGroup group,NavigationCrowdAgent agent,NavigationCrowdPointerDomain<NavigationCrowdAgent> domain,
   Field<float> maximumRadius,Field<float> neighborFactor,Field<float> seconds,int capturedTicks,NavigationCrowdTickProductionBoundaries production,GameMovedContext context,GameMovedReachedTailInvocation invocation){
   if(invocation==null)throw new MissingContract(0x25cb73,"Null reached-tail invocation");invocation.Reset();
   GameMovedReachedContinuationContracts.ProduceAndInvokeReachedCallback(group,agent,domain,maximumRadius,neighborFactor,seconds,capturedTicks,production,context,invocation.Callback);
   NavigationCrowdTickTailContracts.FinishReachedLastWork25E08A(group,agent,domain,seconds,capturedTicks,production,invocation.Tail);
  }
 }
}
