using System;

namespace GrimDynasty.ReverseClient.Actions {
 // Diagnostics only. A matched reference can itself be FFFFFFFF; Returned
 // and Matched distinguish that path from a miss or an unsupported prefix.
 public sealed class SkillReferenceNumberInvocation {
  public readonly Invocation Native;
  public readonly Field<uint> CapturedObjectId=new Field<uint>();
  public bool Matched,Returned;
  public uint MatchVectorOffset,MatchLocalIndex;
  public uint? ReturnedReference;
  public SkillMovedTarget MatchedTarget;
  public int SlotsRead,GetterCalls,GetterReturns;
  public SkillReferenceNumberInvocation():this(new Invocation()){}
  public SkillReferenceNumberInvocation(Invocation native){if(native==null)throw new ArgumentNullException("native");Native=native;Begin();}
  internal void Begin(){Native.At=0;CapturedObjectId.Forget();Matched=Returned=false;MatchVectorOffset=MatchLocalIndex=0;ReturnedReference=null;MatchedTarget=null;SlotsRead=GetterCalls=GetterReturns=0;}
 }
 // Game531A30: RCX=the existing embedded SkillManager, EDX=uint ObjectID,
 // EAX=uint reference. Complete saved assembly: r1-movement-action-path/15.
 // This is an OWN same-allocation stride8 model, not native heap discovery,
 // a SkillManager constructor, concurrent mutation or a submitted action.
 public static class SkillReferenceNumberContracts {
  // Fixed instruction sites for the five copies of the observed search loop.
  // The columns are initial end/sub/shift/test; begin/slot/test/call/compare;
  // next end/inc/sub/shift/compare; matched result load/add.
  sealed class Sites {
   public readonly uint End,Subtract,Shift,InitialTest,Begin,Slot,TargetTest,Getter,Compare,NextEnd,Increment,NextSubtract,NextShift,NextCompare,ResultLoad,ResultAdd;
   public Sites(uint end,uint sub,uint shift,uint test,uint begin,uint slot,uint targetTest,uint getter,uint compare,uint nextEnd,uint increment,uint nextSub,uint nextShift,uint nextCompare,uint resultLoad,uint resultAdd){End=end;Subtract=sub;Shift=shift;InitialTest=test;Begin=begin;Slot=slot;TargetTest=targetTest;Getter=getter;Compare=compare;NextEnd=nextEnd;Increment=increment;NextSubtract=nextSub;NextShift=nextShift;NextCompare=nextCompare;ResultLoad=resultLoad;ResultAdd=resultAdd;}
  }
  static readonly Sites[] sites={
   new Sites(0x531a44,0x531a4a,0x531a50,0x531a59,0x531a60,0x531a64,0x531a68,0x531a6d,0x531a73,0x531a7b,0x531a7f,0x531a81,0x531a85,0x531a8b,0x531c1d,0),
   new Sites(0x531a90,0x531a99,0x531aa0,0x531aa4,0x531ab0,0x531ab7,0x531abb,0x531ac0,0x531ac6,0x531ace,0x531ad5,0x531ad7,0x531ade,0x531ae4,0x531c21,0x531c27),
   new Sites(0x531ae9,0x531af2,0x531af9,0x531afd,0x531b10,0x531b17,0x531b1b,0x531b20,0x531b26,0x531b2e,0x531b35,0x531b37,0x531b3e,0x531b44,0x531c2b,0x531c31),
   new Sites(0x531b49,0x531b52,0x531b59,0x531b5d,0x531b70,0x531b77,0x531b7b,0x531b80,0x531b86,0x531b8e,0x531b95,0x531b97,0x531b9e,0x531ba4,0x531c35,0x531c3b),
   new Sites(0x531ba9,0x531bb0,0x531bb7,0x531bbb,0x531bd0,0x531bd7,0x531bdb,0x531be0,0x531be6,0x531bea,0x531bf1,0x531bf3,0x531bfa,0x531c00,0x531c3f,0x531c45)
  };
  static void Header(SkillReferenceManagerState state,NavigationCrowdPointerVector<SkillMovedTarget> vector,uint at,Invocation i){i.At=at;state.RequireLive(at);vector.RequireLive(at);}
  static Field<NavigationCrowdPointerAddress<SkillMovedTarget>> End(SkillReferenceManagerState state,NavigationCrowdPointerVector<SkillMovedTarget> vector,uint at,Invocation i){Header(state,vector,at,i);return vector.End08.Snapshot();}
  static long Difference(SkillReferenceManagerState state,NavigationCrowdPointerVector<SkillMovedTarget> vector,Field<NavigationCrowdPointerAddress<SkillMovedTarget>> endBits,uint at,Invocation i){
   Header(state,vector,at,i);var beginBits=vector.Begin00.Snapshot();var end=endBits.Read(at);var begin=beginBits.Read(at);
   if(end==null&&begin==null)return 0;
   if(end==null||begin==null||!Object.ReferenceEquals(end.Buffer,begin.Buffer))throw new MissingContract(at,"Pointer subtraction is outside the explicit same-allocation OWN stride8 domain");
   return unchecked(((long)end.Index-(long)begin.Index)*8L);
  }
  static ulong Shift(long span,uint at,Invocation i){i.At=at;return unchecked((ulong)(span>>3));}
  static Field<SkillMovedTarget> Slot(SkillReferenceManagerState state,NavigationCrowdPointerVector<SkillMovedTarget> vector,uint index,Sites s,Invocation i){
   Header(state,vector,s.Begin,i);var beginBits=vector.Begin00.Snapshot();i.At=s.Slot;var begin=beginBits.Read(i.At);
   if(begin==null)throw new MissingContract(i.At,"Null OWN skill-reference-number slot address");
   ulong element=unchecked((ulong)begin.Index+(ulong)index);
   if(element>Int32.MaxValue)throw new MissingContract(i.At,"Skill-reference-number slot exceeds declared OWN storage extent");
   return begin.Buffer.At((int)element,i.At).Snapshot();
  }
  static bool Search(SkillReferenceManagerState state,NavigationCrowdPointerVector<SkillMovedTarget> vector,Field<uint> offset,uint vectorOffset,Sites s,Field<uint> objectId,SkillReferenceNumberInvocation c,out uint result){
   var i=c.Native;result=0;var end=End(state,vector,s.End,i);long span=Difference(state,vector,end,s.Subtract,i);
   if(vectorOffset==0x20){i.At=0x531a4e;if(objectId==null)throw new MissingContract(i.At,"Missing OWN ObjectID input cell");c.CapturedObjectId.CopyFrom(objectId);}
   ulong count=Shift(span,s.Shift,i);uint index=0;i.At=s.InitialTest;if(count==0)return false;
   do{
    var bits=Slot(state,vector,index,s,i);c.SlotsRead++;i.At=s.TargetTest;var target=bits.Read(i.At);
    if(target!=null){
     i.At=s.Getter;c.GetterCalls++;uint actual=GameObjectIdContracts.GetObjectId03A740(target,i);c.GetterReturns++;
     i.At=s.Compare;if(c.CapturedObjectId.Read(i.At)==actual){
      c.Matched=true;c.MatchedTarget=target;c.MatchVectorOffset=vectorOffset;c.MatchLocalIndex=index;
      i.At=s.ResultLoad;
      if(offset==null)result=index;
      else{state.RequireLive(i.At);var baseBits=offset.Snapshot();i.At=s.ResultAdd;result=unchecked(baseBits.Read(i.At)+index);}
      return true;
     }
    }
    // Native order matters: capture End, increment uint index, then read the
    // current Begin for subtraction. Neither header nor count is cached.
    end=End(state,vector,s.NextEnd,i);i.At=s.Increment;index=unchecked(index+1u);
    span=Difference(state,vector,end,s.NextSubtract,i);count=Shift(span,s.NextShift,i);i.At=s.NextCompare;
   }while((ulong)index<count);
   return false;
  }
  static uint Return(uint value,SkillReferenceNumberInvocation c){c.ReturnedReference=value;c.Returned=true;c.Native.At=0x531c1c;return value;}
  public static uint GetSkillReferenceNumber531A30(GameMovementIdentityBridge bridge,MoveCharacter role,Field<uint> objectId,SkillReferenceNumberInvocation c){
   if(c==null)throw new MissingContract(0x531a30,"Missing skill-reference-number invocation");c.Begin();c.Native.At=0x531a30;
   return GetSkillReferenceNumber531A30(SkillReferenceManagerState.ForMoveCharacter(bridge,role,0x531a30),objectId,c);
  }
  public static uint GetSkillReferenceNumber531A30(SkillReferenceManagerState state,Field<uint> objectId,SkillReferenceNumberInvocation invocation){
   if(invocation==null)throw new MissingContract(0x531a30,"Missing skill-reference-number invocation");var c=invocation;c.Begin();c.Native.At=0x531a30;
   if(state==null)throw new MissingContract(c.Native.At,"Null OWN SkillManager view");uint result;
   if(Search(state,state.Collections20,null,0x20,sites[0],objectId,c,out result))return Return(result,c);
   if(Search(state,state.Collections258,state.Offset3BC,0x258,sites[1],objectId,c,out result))return Return(result,c);
   if(Search(state,state.Collections378,state.Offset3C0,0x378,sites[2],objectId,c,out result))return Return(result,c);
   if(Search(state,state.Collections2C8,state.Offset3C4,0x2c8,sites[3],objectId,c,out result))return Return(result,c);
   if(Search(state,state.Collections360,state.Offset3C8,0x360,sites[4],objectId,c,out result))return Return(result,c);
   c.Native.At=0x531c05;return Return(UInt32.MaxValue,c);
  }
 }
}
