using System;

namespace GrimDynasty.ReverseClient.Actions {
 public sealed class GameMovedReachedContinuationInvocation {
  public readonly Invocation Producer=new Invocation();
  public readonly GameMovedInvocation Moved=new GameMovedInvocation();
  public bool KnownCallbackBoundaryReached {get;internal set;}
  public bool CallbackReturned {get;internal set;}
  public bool FullTick {get{return false;}}
 }
 // Bounded own composition over the frozen passed producer. That old method
 // intentionally terminates at its concrete25E087 boundary; it is not edited
 // or mistaken for a successful tick. This adapter admits only its exact known
 // boundary diagnostic, then executes the real newly implemented call body.
 // It never catches a downstream066EE0/Floor/publication/skill failure.
 // CapturedTicks is the explicitly supplied original integer argument; it is
 // not inferred from seconds or invented from agent motion. Earlier tick work
 // and the post-CALL25E08A tail remain outside this composition.
 public static class GameMovedReachedContinuationContracts {
  const string FrozenBoundary="Actual Position48/Vector60 and saved066EE0 slot binding established; real Moved callback and floor/frame/publication dependencies remain unimplemented";
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null reached callback composition dependency");return value;}
  public static void ProduceAndInvokeReachedCallback(NavigationCrowdGroup group,NavigationCrowdAgent agent,NavigationCrowdPointerDomain<NavigationCrowdAgent> domain,
   Field<float> maximumRadius,Field<float> neighborFactor,Field<float> seconds,int capturedTicks,NavigationCrowdTickProductionBoundaries production,GameMovedContext context,GameMovedReachedContinuationInvocation invocation){
   var c=Need(invocation,0x25cb73);c.Producer.At=0;c.Moved.Reset();c.KnownCallbackBoundaryReached=false;c.CallbackReturned=false;
   bool exactBoundary=false;
   try{NavigationCrowdTickProductionContracts.ProduceReachedSingleWork25CB73(group,agent,domain,maximumRadius,neighborFactor,seconds,production,c.Producer);}
   catch(MissingContract error){
    if(error.Address!=0x25e087||c.Producer.At!=0x25e087||!String.Equals(error.Message,FrozenBoundary,StringComparison.Ordinal))throw;
    exactBoundary=true;
   }
   if(!exactBoundary)throw new MissingContract(0x25e087,"Frozen producer did not terminate at the expected concrete callback boundary");
   // An unknown/foreign table can fail at the SAME address; address equality
   // alone cannot authorize continuation. Recheck the exact authoritative
   // Agent.Callback08, interface table and shared owner, without copying them.
   Need(agent,0x25e074).RequireLive(0x25e074);var callback=agent.Callback08.Snapshot();
   var view=callback.Read(0x25e084) as GameMovementInterfaceView;Need(view,0x25e084).FullOwner.Entity.RequireLive(0x25e084);
   var bits=view.Virtual38.Snapshot();object table=bits.Read(0x25e087);
   if(!Object.ReferenceEquals(table,GameMovementInterfaceTable.Character)&&!Object.ReferenceEquals(table,GameMovementInterfaceTable.Player))throw new MissingContract(0x25e087,"Unobserved Moved table cannot resume at a same-address diagnostic");
   Need(context,0x25e087).Callbacks.Owner(view,0x25e087);c.KnownCallbackBoundaryReached=true;
   GameMovedContracts.InvokeMovedCall25E087(agent,capturedTicks,context,c.Moved);c.CallbackReturned=true;
  }
 }
}
