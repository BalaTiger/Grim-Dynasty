using System;

namespace GrimDynasty.ReverseClient.Actions {
 public sealed class EngineEntitySyncedAnimationInvocation {
  public readonly Invocation Native=new Invocation();
  public readonly Field<ulong> EndCapture=new Field<ulong>(),BeginCapture=new Field<ulong>(),FreshBegin=new Field<ulong>();
  public readonly Field<uint> AnimationCapture=new Field<uint>(),HandCapture=new Field<uint>();
  public readonly Field<byte> LoopCapture=new Field<byte>();
  public readonly Field<int> BlendCapture=new Field<int>();
  public ActorBoundingBoxState Actor;public long ShiftedCount;
  public bool Used,CountComputed,Returned;
 }
 // Engine07F490 propagates a request to attached Entities; it is not decoding.
 // Borrow the SAME Entity+78/+80 words already used by constructor07D020 and
 // bounding-box publication. Exact 64-bit SUB/SAR2 controls the empty return.
 // shortcut: nonempty ID storage and Engine0236A0/class0/virtual2C0 dispatch
 // remain unsupported at the first DWORD dereference. No empty default, fake
 // lookup callback or inferred equivalence with Game019D40 is supplied.
 public static class EngineEntitySyncedAnimationContracts {
  public static void PlaySyncedAnimation07F490(ActorBoundingBoxState actor,Field<uint> animation,Field<uint> hand,Field<byte> loop,Field<int> blend,EngineEntitySyncedAnimationInvocation c){
   if(c==null||animation==null||hand==null||loop==null||blend==null)throw new ArgumentNullException("OWN input cells/invocation");
   if(c.Used)throw new InvalidOperationException("Use a fresh synced-animation invocation");c.Used=true;c.Actor=actor;
   try{
    c.Native.At=0x07f49d;if(actor==null)throw new MissingContract(c.Native.At,"Missing existing Actor/Entity view");actor.Entity.RequireLive(c.Native.At);c.EndCapture.CopyFrom(actor.AttachedEnd80.Snapshot());
    c.Native.At=0x07f4a6;c.BeginCapture.CopyFrom(actor.AttachedBegin78.Snapshot());long difference=unchecked((long)(c.EndCapture.Read(c.Native.At)-c.BeginCapture.Read(c.Native.At)));
    c.Native.At=0x07f4aa;c.LoopCapture.CopyFrom(loop.Snapshot());c.Native.At=0x07f4ae;c.ShiftedCount=difference>>2;c.CountComputed=true;
    c.Native.At=0x07f4b2;c.HandCapture.CopyFrom(hand.Snapshot());c.Native.At=0x07f4b5;c.AnimationCapture.CopyFrom(animation.Snapshot());
    c.Native.At=0x07f4bb;if(c.ShiftedCount!=0){c.Native.At=0x07f4d0;c.BlendCapture.CopyFrom(blend.Snapshot());c.Native.At=0x07f4e3;c.FreshBegin.CopyFrom(actor.AttachedBegin78.Snapshot());c.Native.At=0x07f4e7;c.FreshBegin.Read(c.Native.At);throw new MissingContract(c.Native.At,"Nonempty attached ID storage and subsequent Engine lookup/dispatch are not reconstructed");}
    c.Native.At=0x07f575;c.Returned=true;
   }catch(MissingContract e){c.Native.At=e.Address;throw;}
  }
 }
}
