using System;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // The same embedded SkillManager at canonical full owner+850. Collections258
 // and360 are the exact vector objects already used by533410. Other newly
 // represented cells start Unknown. No native constructor is inferred.
 public sealed class SkillReferenceManagerState {
  static readonly ConditionalWeakTable<GameMovementCharacterState,SkillReferenceManagerState> owners=new ConditionalWeakTable<GameMovementCharacterState,SkillReferenceManagerState>();
  public readonly GameMovementCharacterState Owner;
  public readonly SkillMovedManagerState Notifications;
  public readonly NavigationCrowdPointerVector<SkillMovedTarget> Collections20=new NavigationCrowdPointerVector<SkillMovedTarget>();
  public readonly NavigationCrowdPointerVector<SkillMovedTarget> Collections258,Collections360;
  public readonly NavigationCrowdPointerVector<SkillMovedTarget> Collections378=new NavigationCrowdPointerVector<SkillMovedTarget>(),Collections2C8=new NavigationCrowdPointerVector<SkillMovedTarget>();
  public readonly Field<uint> Offset3BC=new Field<uint>(),Offset3C0=new Field<uint>(),Offset3C4=new Field<uint>(),Offset3C8=new Field<uint>();
  SkillReferenceManagerState(GameMovementCharacterState owner){Owner=owner;Notifications=SkillMovedManagerState.ForOwner(owner);Collections258=Notifications.Collections258;Collections360=Notifications.Collections360;}
  public static SkillReferenceManagerState ForOwner(GameMovementCharacterState owner){if(owner==null)throw new ArgumentNullException("owner");return owners.GetValue(owner,delegate(GameMovementCharacterState value){return new SkillReferenceManagerState(value);});}
  public static SkillReferenceManagerState ForMoveCharacter(GameMovementIdentityBridge bridge,MoveCharacter role,uint at){if(bridge==null)throw new MissingContract(at,"Missing existing Game/Move identity bridge");return ForOwner(bridge.ForMoveCharacter(role,at).Owner);}
  public void RequireLive(uint at){Owner.Entity.RequireLive(at);}
 }
 // Game531C50, saved complete assembly. Bounded OWN stride8 storage only:
 // two null headers or same-allocation typed pointers have a known difference.
 // Negative extents are NOT normalized/rejected: modulo64 subtraction, SAR3,
 // then unsigned comparison can enter a slot load even when end<begin.
 // No native heap, arbitrary/unaligned pointers or concurrent mutation model.
 public static class SkillReferenceContracts {
  static void Header(SkillReferenceManagerState state,NavigationCrowdPointerVector<SkillMovedTarget> vector,uint at,Invocation i){i.At=at;state.RequireLive(at);vector.RequireLive(at);}
  static long Difference(SkillReferenceManagerState state,NavigationCrowdPointerVector<SkillMovedTarget> vector,uint endLoad,uint subtract,Invocation i){
   Header(state,vector,endLoad,i);var endBits=vector.End08.Snapshot();
   Header(state,vector,subtract,i);var beginBits=vector.Begin00.Snapshot();var end=endBits.Read(i.At);var begin=beginBits.Read(i.At);
   long byteDifference;
   if(end==null&&begin==null)byteDifference=0;
   else{if(end==null||begin==null||!Object.ReferenceEquals(end.Buffer,begin.Buffer))throw new MissingContract(i.At,"Pointer subtraction is outside the explicit same-allocation OWN stride8 domain");byteDifference=unchecked(((long)end.Index-(long)begin.Index)*8L);}
   return byteDifference;
  }
  static ulong Shift(long difference,uint at,Invocation i){i.At=at;return unchecked((ulong)(difference>>3));}
  static uint Offset(SkillReferenceManagerState state,uint reference,Field<uint> offset,uint at,Invocation i){i.At=at;state.RequireLive(at);return unchecked(reference-offset.Read(at));}
  static Field<SkillMovedTarget> Slot(SkillReferenceManagerState state,NavigationCrowdPointerVector<SkillMovedTarget> vector,uint index,uint beginLoad,uint slotLoad,Invocation i){
   Header(state,vector,beginLoad,i);var beginBits=vector.Begin00.Snapshot();i.At=slotLoad;var begin=beginBits.Read(i.At);
   if(begin==null)throw new MissingContract(i.At,"Null OWN skill-reference slot address");
   ulong element=unchecked((ulong)begin.Index+(ulong)index);if(element>Int32.MaxValue)throw new MissingContract(i.At,"Skill-reference slot exceeds declared OWN storage extent");
   return begin.Buffer.At((int)element,i.At).Snapshot();
  }
  static SkillMovedTarget Test(Field<SkillMovedTarget> bits,uint at,Invocation i){i.At=at;return bits.Read(at);}
  static uint Id(SkillMovedTarget target,Invocation i){i.At=0x531c74;return GameObjectIdContracts.GetObjectId03A740(target,i);}
  public static uint GetSkillId531C50(GameMovementIdentityBridge bridge,MoveCharacter role,uint reference,Invocation i){return GetSkillId531C50(SkillReferenceManagerState.ForMoveCharacter(bridge,role,0x531c50),reference,i);}
  public static uint GetSkillId531C50(SkillReferenceManagerState state,uint reference,Invocation invocation){
   if(invocation==null)throw new MissingContract(0x531c50,"Missing skill-reference invocation");var i=invocation;i.At=0x531c50;if(state==null)throw new MissingContract(i.At,"Null OWN SkillManager view");
   long span=Difference(state,state.Collections20,0x531c50,0x531c54,i);ulong count=Shift(span,0x531c58,i);uint index=reference;
   i.At=0x531c5f;if((ulong)index<count){var target=Test(Slot(state,state.Collections20,index,0x531c64,0x531c68,i),0x531c6c,i);if(target!=null)return Id(target,i);}
   span=Difference(state,state.Collections258,0x531c7b,0x531c85,i);index=Offset(state,reference,state.Offset3BC,0x531c8c,i);count=Shift(span,0x531c93,i);
   i.At=0x531c97;if((ulong)index<count){var target=Test(Slot(state,state.Collections258,index,0x531c9c,0x531ca3,i),0x531ca7,i);if(target!=null)return Id(target,i);}
   span=Difference(state,state.Collections378,0x531cac,0x531cb6,i);index=Offset(state,reference,state.Offset3C0,0x531cbd,i);count=Shift(span,0x531cc4,i);
   i.At=0x531cc8;if((ulong)index<count){var target=Test(Slot(state,state.Collections378,index,0x531ccd,0x531cd4,i),0x531cd8,i);if(target!=null)return Id(target,i);}
   span=Difference(state,state.Collections2C8,0x531cdd,0x531ce7,i);index=Offset(state,reference,state.Offset3C4,0x531cee,i);count=Shift(span,0x531cf5,i);
   i.At=0x531cf9;if((ulong)index<count){var target=Test(Slot(state,state.Collections2C8,index,0x531cfe,0x531d05,i),0x531d09,i);if(target!=null)return Id(target,i);}
   // The final SUB EDX occurs before either final header load, unlike2..4.
   index=Offset(state,reference,state.Offset3C8,0x531d12,i);span=Difference(state,state.Collections360,0x531d18,0x531d1f,i);count=Shift(span,0x531d26,i);
   i.At=0x531d2d;if((ulong)index<count){var target=Test(Slot(state,state.Collections360,index,0x531d32,0x531d39,i),0x531d3d,i);if(target!=null)return Id(target,i);}
   i.At=0x531d48;return 0;
  }
 }
}
