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
   ProduceInvokeAndFinishReachedLastWork(group,agent,domain,maximumRadius,neighborFactor,seconds,capturedTicks,production,context,invocation,null);
  }
  public static void ProduceInvokeAndFinishReachedLastWork(NavigationCrowdGroup group,NavigationCrowdAgent agent,NavigationCrowdPointerDomain<NavigationCrowdAgent> domain,
   Field<float> maximumRadius,Field<float> neighborFactor,Field<float> seconds,int capturedTicks,NavigationCrowdTickProductionBoundaries production,GameMovedContext context,GameMovedReachedTailInvocation invocation,GameReachedGoalContext goal){
   if(invocation==null)throw new MissingContract(0x25cb73,"Null reached-tail invocation");invocation.Reset();
   GameMovedReachedContinuationContracts.ProduceAndInvokeReachedCallback(group,agent,domain,maximumRadius,neighborFactor,seconds,capturedTicks,production,context,invocation.Callback);
   NavigationCrowdTickTailContracts.FinishReachedLastWork25E08A(group,agent,domain,seconds,capturedTicks,production,invocation.Tail,goal);
  }
  // Select only from the actual returned prefix's two mutually exclusive
  // edges. Empty waypoint is a steering skip, not Goal or a successful tick.
  public static void ProduceInvokeAndFinishReachedSteeringChoice(NavigationCrowdGroup group,NavigationCrowdAgent agent,NavigationCrowdPointerDomain<NavigationCrowdAgent> domain,
   NavigationCrowdTickPrefixInvocation prefix,int capturedTicks,NavigationCrowdTickProductionBoundaries production,GameMovedContext context,GameMovedReachedTailInvocation invocation,GameReachedGoalContext goal){
   if(invocation==null)throw new MissingContract(0x25cf79,"Null steering-choice invocation");invocation.Reset();
   if(prefix==null||!Object.ReferenceEquals(prefix.Agent,agent)||!prefix.UpdateReturned||!prefix.SteeringChoiceReturned||prefix.Reached25CB73==prefix.Reached25CF79)throw new MissingContract(0x25cf79,"Steering choice requires one real returned prefix edge for the same Agent");
   uint expected=prefix.Reached25CF79?0x25cf79u:0x25cb73u;
   if(prefix.Native.At!=expected)throw new MissingContract(expected,"Prefix has not returned at its declared steering edge");
   if(prefix.Reached25CF79)GameMovedReachedContinuationContracts.ProduceAndInvokeReachedCallback25CF79(group,agent,domain,prefix.Seconds,capturedTicks,production,context,invocation.Callback);
   else GameMovedReachedContinuationContracts.ProduceAndInvokeReachedCallback(group,agent,domain,prefix.MaximumRadius,prefix.NeighborFactor,prefix.Seconds,capturedTicks,production,context,invocation.Callback);
   NavigationCrowdTickTailContracts.FinishReachedLastWork25E08A(group,agent,domain,prefix.Seconds,capturedTicks,production,invocation.Tail,goal);
  }
 }
}
