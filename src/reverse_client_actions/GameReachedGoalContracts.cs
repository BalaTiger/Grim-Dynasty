using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // One sidecar per existing canonical full owner. New inline cells start
 // Unknown, including20A8 and the HookPack initialization/record fields.
 public sealed class GameReachedGoalCharacterFields {
  static readonly ConditionalWeakTable<GameMovementCharacterState,GameReachedGoalCharacterFields> owners=new ConditionalWeakTable<GameMovementCharacterState,GameReachedGoalCharacterFields>();
  public readonly GameMovementCharacterState Owner;
  public readonly Field<uint> Dword20A8=new Field<uint>();
  public readonly GameReachedGoalHookPackState HookPack45F0=new GameReachedGoalHookPackState();
  GameReachedGoalCharacterFields(GameMovementCharacterState owner){Owner=owner;}
  public static GameReachedGoalCharacterFields ForOwner(GameMovementCharacterState owner){if(owner==null)throw new ArgumentNullException("owner");return owners.GetValue(owner,delegate(GameMovementCharacterState value){return new GameReachedGoalCharacterFields(value);});}
 }
 public sealed class GameReachedGoalActionTable {
  public readonly uint Rva;
  GameReachedGoalActionTable(uint rva){Rva=rva;}
  public static readonly GameReachedGoalActionTable MoveTo=new GameReachedGoalActionTable(0x6e54c0);
 }
 public sealed class GameReachedGoalActionView {
  public readonly ActionRecord Action;
  public readonly Field<object> Virtual00=new Field<object>();
  public GameReachedGoalActionView(ActionRecord action){if(action==null)throw new ArgumentNullException("action");Action=action;}
 }
 public sealed class GameReachedGoalInvocation {
  public readonly Invocation Native=new Invocation();
  public readonly GameObjectManagerLookupInvocation Lookup=new GameObjectManagerLookupInvocation();
  public readonly Field<MovementManager> CapturedMovement=new Field<MovementManager>();
  public readonly Field<uint> CapturedAssociatedId=new Field<uint>();
  public bool ActionStopReturned,CharacterStopReturned,MovementCleared,SingletonReturned,LookupReturned,HookReturned,Returned;
  internal void Reset(){Native.At=0;Lookup.Reset();CapturedMovement.Forget();CapturedAssociatedId.Forget();ActionStopReturned=false;CharacterStopReturned=false;MovementCleared=false;SingletonReturned=false;LookupReturned=false;HookReturned=false;Returned=false;}
 }
 public sealed class GameReachedGoalContext {
  public readonly GameMovementIdentityBridge Bridge;
  public readonly GameReachedGoalEngineState Engine;
  public readonly MoveContext Move;
  internal readonly GameReachedGoalMoveBoundaries MoveCalls;
  readonly Dictionary<GameMovementCharacterState,GameReachedGoalHookPackState> hooks=new Dictionary<GameMovementCharacterState,GameReachedGoalHookPackState>();
  readonly Dictionary<ActionRecord,GameReachedGoalActionView> actions=new Dictionary<ActionRecord,GameReachedGoalActionView>();
  public GameReachedGoalContext(GameMovementIdentityBridge bridge,GameReachedGoalEngineState engine,MoveContext move){
   if(bridge==null||engine==null||move==null)throw new ArgumentNullException("reached-goal context");Bridge=bridge;Engine=engine;Move=move;
   // Explicit symbolic identities must agree; no Game/Move field is copied.
   if(!Object.ReferenceEquals(move.CharacterClass.Read(0xa5cba0),bridge.Callbacks.CharacterClass.Read(0xa5cba0)))throw new ArgumentException("Game and Move CharacterClass symbols must denote one identity");
   MoveCalls=new GameReachedGoalMoveBoundaries(this);
  }
  public void BindHookPack(GameMovementIdentityView view,GameReachedGoalHookPackState pack){
   Bridge.RequireBound(view,0);if(pack==null)throw new ArgumentNullException("pack");GameReachedGoalHookPackState old;
   if(!Object.ReferenceEquals(pack,GameReachedGoalCharacterFields.ForOwner(view.Owner).HookPack45F0))throw new ArgumentException("Inline HookPack must alias this canonical owner's unique cells");
   if(hooks.TryGetValue(view.Owner,out old)&&!Object.ReferenceEquals(old,pack))throw new InvalidOperationException("Inline HookPack view cannot be replaced");hooks[view.Owner]=pack;
  }
  public void BindAction(GameReachedGoalActionView view){
   if(view==null)throw new ArgumentNullException("view");GameReachedGoalActionView old;
   if(actions.TryGetValue(view.Action,out old)&&!Object.ReferenceEquals(old,view))throw new InvalidOperationException("Action already has another primary view");actions[view.Action]=view;
  }
  internal GameReachedGoalHookPackState Hook(GameMovementIdentityView view,uint at){Bridge.RequireBound(view,at);GameReachedGoalHookPackState pack;if(!hooks.TryGetValue(view.Owner,out pack))throw new MissingContract(at,"Inline full-owner45F0 HookPack lacks explicit shared binding");return pack;}
  internal Field<object> ActionPrimary(ActionRecord action,uint at){
   GameReachedGoalActionView view;if(action==null||!actions.TryGetValue(action,out view))throw new MissingContract(at,"Action primary view lacks an explicit shared binding");
   var move=action as MoveActionRecord;if(move==null)throw new MissingContract(at,"This action type has no represented live primary view");move.RequireLive(at);return view.Virtual00;
  }
 }
 // These adapters invoke the already recovered Move body and the real OWN
 // ObjectManager hash lookup. Unclosed skill/sound/navigation/lifetime calls
 // retain MoveBoundaries' Unsupported defaults; no success callback is added.
 internal sealed class GameReachedGoalMoveBoundaries:MoveBoundaries {
  readonly GameReachedGoalContext owner;
  internal GameReachedGoalMoveBoundaries(GameReachedGoalContext context):base(context.Move){owner=context;}
  public override object ObjectManager(){return GameReachedGoalEngineContracts.GetObjectManager01C040(owner.Engine,new Invocation());}
  public override object Lookup(object manager,uint id){var m=manager as GameObjectManagerState;if(m==null)throw new MissingContract(0x019d5f,"ObjectManager result has no declared shared lookup view");return GameObjectManagerLookupContracts.Lookup019D40(m,id,new GameObjectManagerLookupInvocation());}
  public override ClassRecord Class0(object instance){
   var move=instance as MoveCharacter;if(move==null)throw new MissingContract(0x082f27,"Lookup object class slot0 target is unbound");
   var v=owner.Bridge.ForMoveCharacter(move,0x082f27);var table=v.Owner.Entity.Virtual00.Snapshot();
   GameReachedGoalContracts.Primary(table,0x082f27);
   return Object.ReferenceEquals(table.Read(0x082f27),GameMovementPrimaryTable.Player)?owner.Bridge.Callbacks.PlayerClass.Read(0x016d10):owner.Bridge.Callbacks.CharacterClass.Read(0x019d20);
  }
  public override byte Derived(ClassRecord parent,ClassRecord expected){return (byte)(GameMovementCallbackContracts.IsDerivedFrom591480(parent,expected,new Invocation())?1:0);}
  public override uint State458(MoveCharacter move){return GameMovementCallbackContracts.GetActionState05B730(owner.Bridge.ForMoveCharacter(move,0x05b730).Owner,new Invocation());}
  public override void State450(MoveCharacter move,uint state){GameReachedGoalContracts.SetActionState05B710(owner.Bridge.ForMoveCharacter(move,0x05b710),state,new Invocation());}
 }
 // Game067170 and05FC30, retaining repeated primary-table reads, the captured
 // Handler identity, and inline full-owner DC0/16C0 aliases supplied by Bridge.
 // Conditional hot arrival requires an explicitly empty current action, a
 // real lookup miss, and initialized inactive event9. An active MoveTo action
 // runs its recovered End body and stops at unresolved real dependencies;
 // action destruction and associated-controller virtuals remain unclosed.
 public static class GameReachedGoalContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null own reached-goal view");return value;}
  internal static void Primary(Field<object> bits,uint at){object t=Need(bits,at).Read(at);if(!Object.ReferenceEquals(t,GameMovementPrimaryTable.Character)&&!Object.ReferenceEquals(t,GameMovementPrimaryTable.Player))throw new MissingContract(at,"Unobserved Character/Player primary virtual target");}
  static uint Action(GameMovementIdentityView view,uint load,uint call,Invocation i){i.At=load;view.Owner.Entity.RequireLive(load);var table=view.Owner.Entity.Virtual00.Snapshot();i.At=call;Primary(table,call);return GameMovementCallbackContracts.GetActionState05B730(view.Owner,i);}
  public static void SetActionState05B710(GameMovementIdentityView view,uint state,Invocation i){
   i.At=0x05b710;Need(view,i.At).Bridge.RequireBound(view,i.At);view.Owner.ActionState24B4.Write(state);
   i.At=0x05b716;GameReachedGoalCharacterFields.ForOwner(view.Owner).Dword20A8.Write(0);
   i.At=0x05b720;
  }
  public static void CharacterStop05FC30(GameMovementIdentityView view,GameReachedGoalContext context,GameReachedGoalInvocation c){
   var i=Need(c,0x05fc3a).Native;c.ActionStopReturned=false;c.CharacterStopReturned=false;
   i.At=0x05fc3a;Need(context,i.At).Bridge.RequireBound(view,i.At);var handlerBits=view.Move.Handler24C0.Snapshot();
   i.At=0x05fc44;var handler=Need(handlerBits.Read(i.At),i.At);var current=handler.Current08.Snapshot();
   i.At=0x05fc48;var action=current.Read(i.At);
   if(action!=null){
    i.At=0x05fc4d;var table=context.ActionPrimary(action,i.At).Snapshot();i.At=0x05fc50;
    if(!Object.ReferenceEquals(table.Read(i.At),GameReachedGoalActionTable.MoveTo))throw new MissingContract(i.At,"Unbound action virtual18 target");
    var move=action as MoveActionRecord;if(move==null)throw new MissingContract(i.At,"MoveTo table requires the same explicit MoveActionRecord view");
    MoveContracts.Stop(move,context.MoveCalls,i);c.ActionStopReturned=true;
    i.At=0x05fc53;var late=handler.Current08.Snapshot();i.At=0x05fc57;action=late.Read(i.At);
    if(action!=null){i.At=0x05fc5c;var destructor=context.ActionPrimary(action,i.At).Snapshot();i.At=0x05fc64;destructor.Read(i.At);throw new MissingContract(i.At,"Action scalar-deleting destructor slot0 is unbound; flags1 and captured late action are not discarded");}
    i.At=0x05fc66;handler.Current08.Write(null);
   }
   i.At=0x05fc6e;view.Owner.Entity.RequireLive(i.At);var primary=view.Owner.Entity.Virtual00.Snapshot();i.At=0x05fc83;Primary(primary,i.At);
   SetActionState05B710(view,1,i);c.CharacterStopReturned=true;
  }
  public static void InvokeFromInterface(GameMovementInterfaceView callback,GameReachedGoalContext context,GameReachedGoalInvocation invocation){
   var c=Need(invocation,0x06717a);c.Reset();var i=c.Native;i.At=0x06717a;var ctx=Need(context,i.At);var view=ctx.Bridge.ForCallback(callback,i.At);
   bool candidate=Action(view,0x06717a,0x067185,i)==5;
   if(!candidate)candidate=Action(view,0x067190,0x067198,i)==6;
   if(!candidate)candidate=Action(view,0x0671a3,0x0671ab,i)==0x13;
   if(!candidate)candidate=Action(view,0x0671b6,0x0671be,i)==0x15;
   if(candidate&&Action(view,0x0671c9,0x0671d1,i)!=0x13&&Action(view,0x0671dc,0x0671e4,i)!=0x15){
    i.At=0x0671ef;view.Owner.Entity.RequireLive(i.At);var primary=view.Owner.Entity.Virtual00.Snapshot();i.At=0x0671f7;Primary(primary,i.At);CharacterStop05FC30(view,ctx,c);
   }
   i.At=0x0671fd;c.CapturedMovement.CopyFrom(view.BorrowMovementDC0(i.At));
   i.At=0x067204;Need(c.CapturedMovement.Read(i.At),i.At).Moving38.Write(0);c.MovementCleared=true;
   i.At=0x067208;c.CapturedAssociatedId.CopyFrom(view.BorrowAssociatedId16C0(i.At));
   i.At=0x06720e;object manager=GameReachedGoalEngineContracts.GetObjectManager01C040(ctx.Engine,i);c.SingletonReturned=true;
   i.At=0x067219;var lookup=manager as GameObjectManagerState;if(lookup==null)throw new MissingContract(0x019d5f,"Published ObjectManager lacks the shared concrete lookup view");
   var result=GameObjectManagerLookupContracts.LookupBits019D40(lookup,c.CapturedAssociatedId,c.Lookup);c.LookupReturned=true;
   i.At=0x067221;if(result.Read(i.At)!=null){i.At=0x067226;throw new MissingContract(i.At,"Non-null associated object vptr/class0/controller80 requires a proven shared object view and exact virtual targets");}
   i.At=0x06725d;var hooks=ctx.Hook(view,i.At);i.At=0x067273;GameReachedGoalEngineContracts.ExecuteEventHook0EC010(hooks,9,ctx.Engine,i);c.HookReturned=true;c.Returned=true;
  }
 }
}
