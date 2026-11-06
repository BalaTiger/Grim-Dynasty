using System;

namespace GrimDynasty.ReverseClient.Actions {
 // Compile with GameWaypointProductionContracts, not with the lower Goal
 // assembly. Every call borrows its original bridge, movement, singleton and
 // +10 view. Binding publishes no native registration/global/heap state.
 // Stop is the actual151150->25B560 body; destruction is actual088EB0 with
 // the explicit OWN allocation domain. Neither is animation/frame acceptance.
 public sealed class GameReachedGoalStopProduction:IGameReachedGoalStopProduction {
  public GameReachedGoalContext Context{get;private set;}
  public readonly GameWaypointProductionContext Waypoint;
  public readonly NavigationStopObjectContext Stop;
  public Invocation LastNavigation{get;private set;}
  public NavigationSearchManagerView LastNavigationReturn{get;private set;}
  public NavigationStopObjectInvocation LastStop{get;private set;}
  public GameMovementCharacterState LastStopOwner{get;private set;}
  public GameMoveActionDestructionInvocation LastDestruction{get;private set;}
  public int NavigationCalls{get;private set;}
  public int StopCalls{get;private set;}
  public int DestructionCalls{get;private set;}
  GameReachedGoalStopProduction(GameReachedGoalContext goal,GameWaypointProductionContext waypoint){
   Context=goal;Waypoint=waypoint;Stop=NavigationStopObjectContext.ForMovement(waypoint.Movement);
  }
  public static GameReachedGoalStopProduction Bind(GameReachedGoalContext goal,GameWaypointProductionContext waypoint){
   if(goal==null||waypoint==null)throw new ArgumentNullException("original Goal/waypoint composition");
   if(!Object.ReferenceEquals(goal.Bridge,waypoint.Path.Bridge)||!Object.ReferenceEquals(goal.Move,waypoint.Path.Movement))throw new ArgumentException("Goal and stop must use the same original Bridge and MoveContext");
   if(goal.StopProduction!=null){var old=goal.StopProduction as GameReachedGoalStopProduction;if(old==null||!Object.ReferenceEquals(old.Waypoint,waypoint))throw new InvalidOperationException("Goal already has a different stop production binding");return old;}
   var result=new GameReachedGoalStopProduction(goal,waypoint);goal.BindStopProduction(result);return result;
  }
  public object Navigation(){
   LastNavigation=new Invocation();LastNavigationReturn=null;NavigationCalls++;
   LastNavigationReturn=NavigationSearchSingletonContracts.Get01C510(Waypoint.Path.Globals,LastNavigation);return LastNavigationReturn;
  }
  public void StopObject(object navigation,object owner){
   LastStop=new NavigationStopObjectInvocation();LastStopOwner=null;StopCalls++;
   // Resolve the declared OWN typed composition before entering151150. A
   // missing binding does not claim the native callee's pointer-load prefix.
   var bound=GameWaypointNavigationState.ForBound(navigation as NavigationSearchManagerView,Waypoint,0x151150);
   if(owner!=null){var move=owner as MoveCharacter;if(move==null)throw new MissingContract(0x151150,"StopObject owner lacks the original MoveCharacter identity");LastStopOwner=Context.Bridge.ForMoveCharacter(move,0x151150).Owner;}
   NavigationStopObjectContracts.Stop151150(bound.MoveView,LastStopOwner,Stop,LastStop);
  }
  public void Destroy(GameReachedGoalActionView view,uint flags,Invocation invocation){
   if(invocation==null)throw new ArgumentNullException("invocation");LastDestruction=new GameMoveActionDestructionInvocation();DestructionCalls++;
   try{
    var action=view==null?null:view.Action as MoveActionRecord;
    var state=GameMoveActionConstructionState.ForBound(action,0x088f12);
    if(!Object.ReferenceEquals(state.Primary,view))throw new MissingContract(0x088f12,"Deleting action must borrow its canonical constructor/Goal primary view");
    GameMoveActionDestructionContracts.Destroy088EB0(state,flags,LastDestruction);invocation.At=LastDestruction.Native.At;
   }catch(MissingContract error){invocation.At=error.Address;throw;}
  }
 }
}
