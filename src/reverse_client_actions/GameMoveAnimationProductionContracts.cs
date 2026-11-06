using System;
using System.Collections.Generic;

namespace GrimDynasty.ReverseClient.Actions {
 // Optional OWN composition for ONE already bridged Character/Player. The
 // caller supplies its existing Actor/mesh context, not a parallel controller
 // or a reconstructed resource. Binding publishes no animation or speed.
 public sealed class GameMoveAnimationProductionContext {
  public readonly GameMovementIdentityBridge Bridge;
  public readonly GameMovementIdentityView View;
  public readonly EngineAnimationSpeedContext Speed;
  public PlayerMousePlayerLookupContext Lookup{get;private set;}
  // Borrow the existing globals/map used by mouse lookup. Binding creates no
  // native singleton, equipment record or successful object lookup.
  public void BindLookup(PlayerMousePlayerLookupContext lookup){
   if(lookup==null)throw new ArgumentNullException("lookup");
   if(!Object.ReferenceEquals(lookup.Bridge,Bridge))throw new ArgumentException("Animation hand lookup must borrow the same identity bridge");
   if(Lookup!=null&&!Object.ReferenceEquals(Lookup,lookup))throw new InvalidOperationException("Animation lookup cannot be rebound");Lookup=lookup;
  }
  public GameMoveAnimationProductionContext(GameMovementIdentityBridge bridge,MoveCharacter move,EngineAnimationSpeedContext speed){
   if(bridge==null||move==null||speed==null)throw new ArgumentNullException("existing animation composition");
   var view=bridge.ForMoveCharacter(move,0);
   if(!Object.ReferenceEquals(bridge.Callbacks.Entities,speed.Bounds.Entities)||!Object.ReferenceEquals(view.Owner.Entity,speed.Actor.Entity))throw new ArgumentException("Animation must borrow the same Bridge owner, Entity registry and existing Actor");
   Bridge=bridge;View=view;Speed=speed;
  }
  internal GameMovementIdentityView RequireOwner(MoveCharacter move,uint at){
   var view=Bridge.ForMoveCharacter(move,at);
   if(!Object.ReferenceEquals(view,View)||!Object.ReferenceEquals(view.Owner.Entity,Speed.Actor.Entity))throw new MissingContract(at,"Animation composition does not own this original Character view");
   return view;
  }
 }
 public sealed class GameMoveAnimationStateCall {
  public readonly Invocation Native=new Invocation();
  public readonly Field<object> CapturedPrimary=new Field<object>();
  public readonly uint LoadAt,CallAt;public readonly bool IsWrite;
  public GameMovementCharacterState Owner;
  public uint? Value;
  public bool PrimaryCaptured,Dispatched,Returned;
  internal GameMoveAnimationStateCall(uint load,uint call,bool write){LoadAt=load;CallAt=call;IsWrite=write;}
 }
 // Passive observations only; no event/callback can supply Play success or
 // change fields during the production calls. The owning Handle resets this
 // once, preserving both getters and state transitions within a failed Begin.
 public sealed class GameMoveAnimationProductionInvocation {
  public readonly Invocation Native=new Invocation();
  public readonly List<uint> CallerSites=new List<uint>();
  public readonly List<GameMovementCharacterState> Owners=new List<GameMovementCharacterState>();
  public readonly List<GameGetAnimationSetInvocation> SetCalls=new List<GameGetAnimationSetInvocation>();
  public readonly List<EngineAnimationSpeedInvocation> SpeedCalls=new List<EngineAnimationSpeedInvocation>();
  public readonly List<GameMoveAnimationStateCall> StateCalls=new List<GameMoveAnimationStateCall>();
  public readonly List<GameAnimationPlayInvocation> PlayCalls=new List<GameAnimationPlayInvocation>();
  public void Reset(){Native.At=0;CallerSites.Clear();Owners.Clear();SetCalls.Clear();SpeedCalls.Clear();StateCalls.Clear();PlayCalls.Clear();}
 }
 public static class GameMoveAnimationProductionContracts {
  static GameMoveAnimationProductionInvocation BeginCall(GameMoveAnimationProductionInvocation c,uint at){if(c==null)throw new MissingContract(at,"Missing animation composition diagnostics");c.Native.At=at;c.CallerSites.Add(at);return c;}
  static GameMovementIdentityView Owner(GameMoveAnimationProductionContext context,MoveCharacter move,uint at,GameMoveAnimationProductionInvocation c){
   if(context==null)throw new MissingContract(at,"Animation composition is not bound");var view=context.RequireOwner(move,at);c.Owners.Add(view.Owner);return view;
  }
  public static Field<AnimationSet> GetAnimations(GameMoveAnimationProductionContext context,MoveCharacter move,uint caller,GameMoveAnimationProductionInvocation invocation){
   var c=BeginCall(invocation,caller);var nested=new GameGetAnimationSetInvocation();c.SetCalls.Add(nested);
   try{
    if(caller!=0x05f9fb&&caller!=0x05fa48)throw new MissingContract(caller,"Animation getter caller is outside saved05F870 scope");
    var owner=Owner(context,move,0x05ae50,c);var result=GameCharacterAnimationContracts.GetAnimationSet05AE40(owner.Owner,nested);c.Native.At=nested.Native.At;return result;
   }catch(MissingContract e){c.Native.At=e.Address;throw;}
  }
  public static Field<float> GetSpeed(GameMoveAnimationProductionContext context,MoveCharacter move,uint index,uint caller,GameMoveAnimationProductionInvocation invocation){
   var c=BeginCall(invocation,caller);var nested=new EngineAnimationSpeedInvocation();c.SpeedCalls.Add(nested);
   try{
    if(caller!=0x05fa08)throw new MissingContract(caller,"Animation-speed caller is outside saved05F870 scope");
    Owner(context,move,0x027416,c);var result=EngineAnimationSpeedContracts.GetAnimationSpeed027410(context.Speed,index,nested);c.Native.At=nested.Native.At;return result;
   }catch(MissingContract e){c.Native.At=e.Address;throw;}
  }
  static GameMovementIdentityView StateOwner(GameMoveAnimationProductionContext context,MoveCharacter move,GameMoveAnimationStateCall state,GameMoveAnimationProductionInvocation c){
   state.Native.At=state.LoadAt;var view=Owner(context,move,state.LoadAt,c);state.Owner=view.Owner;state.CapturedPrimary.CopyFrom(view.Owner.Entity.Virtual00.Snapshot());state.PrimaryCaptured=true;
   state.Native.At=state.CallAt;ActorBoundingBoxContracts.RequireObservedDispatch(state.CapturedPrimary,state.CallAt);state.Dispatched=true;return view;
  }
  public static uint GetState458(GameMoveAnimationProductionContext context,MoveCharacter move,uint caller,GameMoveAnimationProductionInvocation invocation){
   var c=BeginCall(invocation,caller);uint load;if(caller==0x05fa17)load=0x05fa0e;else if(caller==0x082ddf)load=0x082dd9;else throw new MissingContract(caller,"Action-state getter caller lies outside saved Move scope");
   var state=new GameMoveAnimationStateCall(load,caller,false);c.StateCalls.Add(state);
   try{var view=StateOwner(context,move,state,c);uint value=GameMovementCallbackContracts.GetActionState05B730(view.Owner,state.Native);state.Value=value;state.Returned=true;c.Native.At=state.Native.At;return value;}
   catch(MissingContract e){state.Native.At=c.Native.At=e.Address;throw;}
  }
  public static void SetState450(GameMoveAnimationProductionContext context,MoveCharacter move,uint value,uint caller,GameMoveAnimationProductionInvocation invocation){
   var c=BeginCall(invocation,caller);uint load;if(caller==0x05fa3f)load=0x05fa34;else if(caller==0x05fa90)load=0x05fa85;else if(caller==0x05f991)load=0x05f986;else throw new MissingContract(caller,"Action-state setter caller lies outside saved Move scope");
   var state=new GameMoveAnimationStateCall(load,caller,true);state.Value=value;c.StateCalls.Add(state);
   try{var view=StateOwner(context,move,state,c);GameReachedGoalContracts.SetActionState05B710(view,value,state.Native);state.Returned=true;c.Native.At=state.Native.At;}
   catch(MissingContract e){state.Native.At=c.Native.At=e.Address;throw;}
  }
  public static byte Play(GameMoveAnimationProductionContext context,MoveCharacter move,Field<AnimationSet> set,uint animation,object name,float speed,byte loop,uint extra,uint caller,GameMoveAnimationProductionInvocation invocation){
   var c=BeginCall(invocation,caller);var nested=new GameAnimationPlayInvocation();c.PlayCalls.Add(nested);
   try{
    if(caller!=0x05fa70)throw new MissingContract(caller,"Play caller is outside saved Move05F870 scope");
    // Defer Actor/owner consumption to020875. A null-resource entry can return0
    // after its own field writes without reading the Actor, exactly as observed.
    byte result=GameAnimationPlayContracts.Play024DE0(context,move,set,animation,name,Field<float>.Of(speed),Field<byte>.Of(loop),Field<uint>.Of(extra),nested);c.Native.At=nested.Native.At;return result;
   }catch(MissingContract e){c.Native.At=e.Address;throw;}
  }
 }
}
