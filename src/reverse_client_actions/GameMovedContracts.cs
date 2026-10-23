using System;
using System.Collections.Generic;

namespace GrimDynasty.ReverseClient.Actions {
 // One composition of the existing authoritative Character, Region, Level and
 // Entity registries. No second coordinate state or successful callback hook.
 public sealed class GameMovedContext {
  public readonly GameRotationPublicationContext Rotation;
  public readonly WorldFloorContext Floor;
  public GameMovementCallbackContext Callbacks {get{return Rotation.Callbacks;}}
  public GameMovedContext(GameRotationPublicationContext rotation,WorldFloorContext floor){
   if(rotation==null||floor==null)throw new ArgumentNullException("moved composition");
   if(!Object.ReferenceEquals(rotation.Publication.Regions,floor.Publications))throw new ArgumentException("Moved floor and publication must share the canonical Region/Level registry");
   Rotation=rotation;Floor=floor;
  }
 }
 public sealed class GameMovedInvocation {
  public readonly Invocation Native=new Invocation();
  public GameRotationInvocation Rotation {get;private set;}
  readonly List<uint> steps=new List<uint>();
  public WorldPosition24 LocalPosition {get;internal set;}
  public NavigationPoint3 CapturedDirection {get;internal set;}
  public bool FloorSucceeded {get;internal set;}
  public bool RotationReturned {get;internal set;}
  public bool NotificationReturned {get;internal set;}
  public uint[] ObservedAddresses {get{return steps.ToArray();}}
  public GameMovedInvocation(){Reset();}
  internal void Reset(){Native.At=0;Rotation=new GameRotationInvocation();steps.Clear();LocalPosition=null;CapturedDirection=null;FloorSucceeded=false;RotationReturned=false;NotificationReturned=false;}
  internal void At(uint at){Native.At=at;steps.Add(at);}
 }
 // Game066EE0 actual ordinary branch, r1-game-movement-interface13/14;
 // exact import/table bindings in r1-game-callback-dependencies. Own stable
 // typed cells, ordinary binary32, no native ABI/EH or concurrent mutation.
 // Stale frame's IsGameTimePaused1E9760 and action13/15's WorldVec3 subtraction
 // remain explicit stops. Floor false returns normally, without notification.
 public static class GameMovedContracts {
  static T Need<T>(T x,uint at)where T:class{if(x==null)throw new MissingContract(at,"Null own Game Moved dependency");return x;}
  static Field<float> Cell(NavigationCrowdAgent agent,NavigationPoint3 p,int n,uint at){
   Need(agent,at).RequireLive(at);Need(p,at).RequireLive(at);
   try{return n==0?p.X:n==1?p.Y:p.Z;}catch(MissingContract e){throw new MissingContract(at,"Moved data exceeds declared own agent extent",e);}
  }
  static void ActionTable(Field<object> bits,uint at){
   object table=bits.Read(at);
   if(!Object.ReferenceEquals(table,GameMovementPrimaryTable.Character)&&!Object.ReferenceEquals(table,GameMovementPrimaryTable.Player))throw new MissingContract(at,"Unbound Character primary slot458 target");
  }
  // WorldVec3 and WorldCoords constructors alias228AB0. The saved WorldVec3
  // binding and observed MOV qword[RCX],0;MOV RAX,RCX;RET permit this Q0 write.
  public static WorldPosition24 ConstructWorldVec228AB0(WorldPosition24 value,Invocation invocation){
   var i=Need(invocation,0x228ab0);i.At=0x228ab0;Need(value,i.At).Q0.Write(0);i.At=0x228aba;return value;
  }
  public static void CharacterMoved066EE0(GameMovementInterfaceView callback,int ticks,NavigationCrowdAgent agent,GameMovedContext context,GameMovedInvocation invocation){
   var c=Need(invocation,0x066ee0);c.Reset();CharacterMoved(callback,ticks,agent,context,c);
  }
  static void CharacterMoved(GameMovementInterfaceView callback,int ticks,NavigationCrowdAgent agent,GameMovedContext context,GameMovedInvocation c){
   c.At(0x066ee0);var ctx=Need(context,c.Native.At);var owner=ctx.Callbacks.Owner(callback,c.Native.At);var i=c.Native;
   c.At(0x066f03);
   if(ticks!=0){
    c.At(0x066f0b);uint last=EntityMovementContracts.GetLastFrameUpdated07E7C0(owner.Entity,i);
    c.At(0x066f11);var import=ctx.Rotation.Publication.Gates.EngineImport609050.Snapshot();
    c.At(0x066f1a);var global=Need(import.Read(i.At),i.At);
    if(!Object.ReferenceEquals(global,ctx.Callbacks.World.EngineGlobal))throw new MissingContract(i.At,"Frame import must borrow the shared gEngine cell");
    var capturedEngine=global.Snapshot();c.At(0x066f1d);var engine=ctx.Callbacks.Entities.Engine(capturedEngine.Read(i.At),i.At);
    uint frame=EntityMovementContracts.GetFrameCount077880(engine,i);c.At(0x066f25);
    if(last<unchecked(frame-1u)){c.At(0x066f29);throw new MissingContract(i.At,"IsGameTimePaused1E9760 body is unclosed; no paused/return value guessed");}
   }
   c.At(0x066f3b);var local=new WorldPosition24();c.LocalPosition=local;ConstructWorldVec228AB0(local,i);
   c.At(0x066f45);ulong region=EntityMovementContracts.GetRegion07F130(owner.Entity,i);
   c.At(0x066f55);Need(agent,i.At);
   if(WorldPositionInitializationContracts.SetFromWorldPosition22C690(local,agent.Position48,region,ctx.Callbacks.World,i)==0){c.At(0x06716e);return;}
   c.At(0x066f67);
   if(WorldFloorContracts.PutOnFloor22C620(local,ctx.Callbacks.World,ctx.Floor,i)==0){c.At(0x06716e);return;}
   c.FloorSucceeded=true;
   // Native MOV Z, MOVSD XY, store Z, capture vptr, store XY: unknown scalar
   // bits are transferred, not consumed as arithmetic prematurely.
   c.At(0x066f75);var z=Cell(agent,agent.Vector60,2,i.At).Snapshot();
   c.At(0x066f7c);var x=Cell(agent,agent.Vector60,0,i.At).Snapshot();var y=Cell(agent,agent.Vector60,1,i.At).Snapshot();
   var direction=new NavigationPoint3();c.CapturedDirection=direction;c.At(0x066f81);direction.Z.CopyFrom(z);
   c.At(0x066f84);owner.Entity.RequireLive(i.At);var first=owner.Entity.Virtual00.Snapshot();
   c.At(0x066f88);direction.X.CopyFrom(x);direction.Y.CopyFrom(y);
   c.At(0x066f8d);ActionTable(first,i.At);bool special=GameMovementCallbackContracts.GetActionState05B730(owner,i)==0x13;
   if(!special){c.At(0x066f98);owner.Entity.RequireLive(i.At);var second=owner.Entity.Virtual00.Snapshot();c.At(0x066fa0);ActionTable(second,i.At);special=GameMovementCallbackContracts.GetActionState05B730(owner,i)==0x15;}
   if(special){c.At(0x066fdf);throw new MissingContract(i.At,"Special action13/15 reaches WorldVec3 subtraction22C240; special interpolation/flip path remains unimplemented");}
   c.At(0x067146);var rotation=ctx.Rotation.Bind(owner);
   try{GameRotationPublicationContracts.RotateTowards05E160(rotation,ticks,direction,local,ctx.Rotation,c.Rotation);}finally{i.At=c.Rotation.Native.At;}
   c.RotationReturned=true;c.At(0x067152);SkillMovedContracts.Moved533410(SkillMovedManagerState.ForOwner(owner),i);c.NotificationReturned=true;c.At(0x06716e);
  }
  // Actual Engine25E087 call-site only. CrowdAgentData starts at Agent+48;
  // position and direction therefore borrow Position48 and Vector60, not54.
  // Earlier tick production and the remaining post-call tick tail are separate.
  public static void InvokeMovedCall25E087(NavigationCrowdAgent agent,int ticks,GameMovedContext context,GameMovedInvocation invocation){
   var c=Need(invocation,0x25e074);c.Reset();c.At(0x25e074);Need(agent,c.Native.At).RequireLive(c.Native.At);var callback=agent.Callback08.Snapshot();
   c.At(0x25e084);var view=callback.Read(c.Native.At) as GameMovementInterfaceView;Need(view,c.Native.At).FullOwner.Entity.RequireLive(c.Native.At);var table=view.Virtual38.Snapshot();
   c.At(0x25e087);object value=table.Read(c.Native.At);
   if(!Object.ReferenceEquals(value,GameMovementInterfaceTable.Character)&&!Object.ReferenceEquals(value,GameMovementInterfaceTable.Player))throw new MissingContract(c.Native.At,"Unobserved Moved slot18 target");
   CharacterMoved(view,ticks,agent,context,c);c.At(0x25e08a);
  }
 }
}
