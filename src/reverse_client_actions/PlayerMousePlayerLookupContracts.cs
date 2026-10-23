using System;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // Controller+30 is the lookup key, not an Object+2C ID or a second Object18.
 // This sole sidecar starts Unknown and may be borrowed by later GetAI code.
 public sealed class PlayerMouseControllerObjectFields {
  static readonly ConditionalWeakTable<Controller,PlayerMouseControllerObjectFields> controllers=new ConditionalWeakTable<Controller,PlayerMouseControllerObjectFields>();
  public readonly Controller Controller;
  public readonly Field<uint> ObjectId30=new Field<uint>();
  PlayerMouseControllerObjectFields(Controller controller){Controller=controller;}
  public static PlayerMouseControllerObjectFields ForController(Controller controller){
   if(controller==null)throw new ArgumentNullException("controller");
   return controllers.GetValue(controller,delegate(Controller value){return new PlayerMouseControllerObjectFields(value);});
  }
 }
 public sealed class PlayerMousePlayerLookupContext {
  public readonly PlayerMouseContext Mouse;
  public readonly GameMovementIdentityBridge Bridge;
  public readonly GameReachedGoalEngineState Engine;
  public PlayerMousePlayerLookupContext(PlayerMouseContext mouse,GameMovementIdentityBridge bridge,GameReachedGoalEngineState engine){
   if(mouse==null||bridge==null||engine==null)throw new ArgumentNullException("player lookup context");Mouse=mouse;Bridge=bridge;Engine=engine;
  }
  // Explicit own setup binds the existing full owner to the already existing
  // MoveCharacter. Its mouse vptr must be the very same Entity cell; no value
  // synchronization, new role, or successful CanAct getter is installed here.
  public void BindRole(MouseRoleState role){
   if(role==null)throw new ArgumentNullException("role");var view=Bridge.ForMoveCharacter(role.Role,0);
   if(!Object.ReferenceEquals(role.Virtual00,view.Owner.Entity.Virtual00))throw new ArgumentException("Mouse role must borrow the canonical Entity primary-vptr cell");
   Mouse.Attach(role);
  }
 }
 public sealed class PlayerMousePlayerLookupInvocation {
  public readonly Invocation Native=new Invocation();
  public GameObjectManagerLookupInvocation Lookup{get;private set;}
  public readonly Field<uint> CapturedObjectId=new Field<uint>();
  public readonly Field<MoveCharacter> Result=new Field<MoveCharacter>();
  public bool SingletonReturned,LookupReturned,ClassReturned,Returned;
  public PlayerMousePlayerLookupInvocation(){Lookup=new GameObjectManagerLookupInvocation();}
  internal void Reset(){Native.At=0;Lookup=new GameObjectManagerLookupInvocation();CapturedObjectId.Forget();Result.Forget();SingletonReturned=false;LookupReturned=false;ClassReturned=false;Returned=false;}
 }
 // Game128530 exact retained body, r1-game-mouse-state/08. Only the represented
 // normal domain is implemented: real hot singleton, own locked hash lookup,
 // canonical MoveCharacter identity and observed Character/Player class slots.
 // No delegate can manufacture the lookup or class result. Non-represented
 // object identities, cold initialization and other virtual tables stop.
 public static class PlayerMousePlayerLookupContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null own player-lookup view");return value;}
  static MoveCharacter Return(MoveCharacter value,PlayerMousePlayerLookupInvocation call){
   call.Native.At=value==null?0x12858cu:0x128584u;call.Result.Write(value);call.Returned=true;return value;
  }
  static ClassRecord ClassInfo(object captured,PlayerMousePlayerLookupContext context,Invocation i){
   // Reuse the canonical symbols already used by the passed callback/bridge
   // component. These exact LEA/RET slot bodies have no mutable class snapshot.
   if(Object.ReferenceEquals(captured,GameMovementPrimaryTable.Player)){i.At=0x016d10;return context.Bridge.Callbacks.PlayerClass.Read(i.At);}
   if(Object.ReferenceEquals(captured,GameMovementPrimaryTable.Character)){i.At=0x019d20;return context.Bridge.Callbacks.CharacterClass.Read(i.At);}
   throw new MissingContract(i.At,"Concrete lookup object class slot0 target is unbound");
  }
  public static MoveCharacter Resolve128530(Controller controller,PlayerMousePlayerLookupContext context,PlayerMousePlayerLookupInvocation invocation){
   var call=Need(invocation,0x128536);call.Reset();var i=call.Native;var ctx=Need(context,0x128536);
   i.At=0x128536;call.CapturedObjectId.CopyFrom(PlayerMouseControllerObjectFields.ForController(Need(controller,i.At)).ObjectId30);
   i.At=0x128539;object manager=GameReachedGoalEngineContracts.GetObjectManager01C040(ctx.Engine,i);call.SingletonReturned=true;
   i.At=0x128544;var typedManager=manager as GameObjectManagerState;
   if(typedManager==null)throw new MissingContract(0x019d5f,"Published ObjectManager has no shared concrete hash-lookup view");
   var result=GameObjectManagerLookupContracts.LookupBits019D40(typedManager,call.CapturedObjectId,call.Lookup);call.LookupReturned=true;
   i.At=0x12854c;object found=result.Read(i.At);if(found==null)return Return(null,call);
   i.At=0x128551;var move=found as MoveCharacter;
   if(move==null)throw new MissingContract(i.At,"Lookup Object18 is not an existing canonical MoveCharacter view");
   var view=ctx.Bridge.ForMoveCharacter(move,i.At);view.Owner.Entity.RequireLive(i.At);var table=view.Owner.Entity.Virtual00.Snapshot();
   i.At=0x128557;var actual=ClassInfo(table.Read(i.At),ctx,i);call.ClassReturned=true;
   i.At=0x128560;var expected=ctx.Bridge.Callbacks.PlayerClass.Read(i.At);
   if(Object.ReferenceEquals(actual,expected))return Return(move,call);
   i.At=0x128565;var parentBits=Need(actual,i.At).Parent10.Snapshot();i.At=0x128569;var parent=parentBits.Read(i.At);
   if(parent==null)return Return(null,call);
   i.At=0x12856e;if(Object.ReferenceEquals(parent,expected))return Return(move,call);
   i.At=0x128573;if(GameMovementCallbackContracts.IsDerivedFrom591480(parent,expected,i))return Return(move,call);
   return Return(null,call);
  }
 }
 // Adapter replaces only the formerly Unsupported128530 producer. Four passed
 // entry bodies and all other real Unsupported boundaries remain unchanged.
 public class PlayerMousePlayerLookupBoundaries:PlayerMouseBoundaries {
  public readonly PlayerMousePlayerLookupContext LookupContext;
  public PlayerMousePlayerLookupInvocation LastLookup{get;private set;}
  public PlayerMousePlayerLookupBoundaries(PlayerMousePlayerLookupContext context):base(Require(context).Mouse){LookupContext=context;}
  static PlayerMousePlayerLookupContext Require(PlayerMousePlayerLookupContext context){if(context==null)throw new ArgumentNullException("context");return context;}
  public override MoveCharacter ResolvePlayer128530(Controller controller){LastLookup=new PlayerMousePlayerLookupInvocation();return PlayerMousePlayerLookupContracts.Resolve128530(controller,LookupContext,LastLookup);}
 }
}
