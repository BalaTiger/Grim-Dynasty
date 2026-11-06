using System;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // Missing inline cells borrow canonical identities; neither is a native
 // startup default. Creating these views does not activate or register.
 public sealed class GameMovementActivationFields {
  static readonly ConditionalWeakTable<MovementManager,GameMovementActivationFields> states=new ConditionalWeakTable<MovementManager,GameMovementActivationFields>();
  public readonly MovementManager Manager;
  public readonly Field<byte> Disabled6D=new Field<byte>();
  GameMovementActivationFields(MovementManager manager){Manager=manager;}
  public static GameMovementActivationFields ForManager(MovementManager manager){if(manager==null)throw new ArgumentNullException("manager");return states.GetValue(manager,delegate(MovementManager m){return new GameMovementActivationFields(m);});}
 }
 public sealed class GameMovementActivationEngineFields {
  static readonly ConditionalWeakTable<WorldEngine,GameMovementActivationEngineFields> states=new ConditionalWeakTable<WorldEngine,GameMovementActivationEngineFields>();
  public readonly WorldEngine Engine;
  public readonly Field<byte> Disabled292=new Field<byte>();
  GameMovementActivationEngineFields(WorldEngine engine){Engine=engine;}
  public static GameMovementActivationEngineFields ForEngine(WorldEngine engine){if(engine==null)throw new ArgumentNullException("engine");return states.GetValue(engine,delegate(WorldEngine e){return new GameMovementActivationEngineFields(e);});}
 }
 public sealed class GameMovementActivationContext {
  public readonly GameWaypointProductionContext Waypoint;
  public readonly NavigationRegistrationContext Registration;
  public GameMovementActivationContext(GameWaypointProductionContext waypoint,NavigationRegistrationContext registration){
   if(waypoint==null||registration==null)throw new ArgumentNullException("activation composition");
   if(!Object.ReferenceEquals(waypoint.Movement,registration.Movement))throw new ArgumentException("Activation and registration must use the original movement composition");
   Waypoint=waypoint;Registration=registration;
  }
 }
 public sealed class GameMovementActivationInvocation {
  public readonly Invocation Native=new Invocation();
  public Invocation Navigation{get;private set;}
  public NavigationRegistrationInvocation Registration{get;private set;}
  public WorldEngine Engine;
  public NavigationSearchManagerView NavigationResult;
  public object CapturedOwner;
  public bool ManagerDisabled,EngineDisabled,FlagWritten,NavigationReturned,RegistrationEntered,Returned;
  internal void Begin(){Native.At=0;Navigation=new Invocation();Registration=new NavigationRegistrationInvocation();Engine=null;NavigationResult=null;CapturedOwner=null;ManagerDisabled=EngineDisabled=FlagWritten=NavigationReturned=RegistrationEntered=Returned=false;}
 }
 // Game08ED40, r1-movement-activation-run1/05. +6C=1 precedes singleton
 // and AddObject, and survives their failures. The two early returns leave
 // the previous +6C untouched. This is not a loaded-runtime initialization
 // claim; both gate cells and the engine/singleton publications are required.
 // Compile beside GameWaypointProduction to access its unique +10 binding.
 public static class GameMovementActivationContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Missing original activation identity");return value;}
  public static void Activate08ED40(MovementManager manager,GameMovementActivationContext context,GameMovementActivationInvocation call){
   if(call==null)throw new ArgumentNullException("invocation");call.Begin();var i=call.Native;
   try{
    i.At=0x08ed46;
    if(GameMovementActivationFields.ForManager(Need(manager,i.At)).Disabled6D.Read(i.At)!=0){call.ManagerDisabled=true;i.At=0x08ed83;call.Returned=true;return;}
    i.At=0x08ed56;var waypoint=Need(context,i.At).Waypoint;
    call.Engine=Need(waypoint.Path.Movement.EngineGlobal.Read(i.At),i.At);
    i.At=0x08ed59;
    if(GameMovementActivationEngineFields.ForEngine(call.Engine).Disabled292.Read(i.At)!=0){call.EngineDisabled=true;i.At=0x08ed83;call.Returned=true;return;}
    i.At=0x08ed62;manager.Registered6C.Write(1);call.FlagWritten=true;
    i.At=0x08ed66;call.NavigationResult=NavigationSearchSingletonContracts.Get01C510(waypoint.Path.Globals,call.Navigation);call.NavigationReturned=true;
    i.At=0x08ed6c;call.CapturedOwner=manager.Owner00.Read(i.At);
    i.At=0x08ed77;
    // Typed address/view resolution is an OWN composition condition before
    // entering151020, not an invented native pointer-load or callback result.
    var navigation=GameWaypointNavigationState.ForBound(call.NavigationResult,waypoint,i.At);
    GameMovementCharacterState owner=null;
    if(call.CapturedOwner!=null){var move=call.CapturedOwner as MoveCharacter;if(move==null)throw new MissingContract(i.At,"Activation owner is not the original MoveCharacter view");owner=waypoint.Path.Bridge.ForMoveCharacter(move,i.At).Owner;}
    call.RegistrationEntered=true;
    NavigationRegistrationContracts.Add151020(navigation.MoveView,owner,context.Registration,call.Registration);
    i.At=call.Registration.Native.At;call.Returned=true;
   }catch(MissingContract error){i.At=error.Address;throw;}
  }
 }
}
