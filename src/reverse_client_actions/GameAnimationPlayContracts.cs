using System;
using System.Collections.Generic;

namespace GrimDynasty.ReverseClient.Actions {
 public sealed class GameAnimationPlayInvocation {
  public readonly Invocation Native=new Invocation(),MeshCall=new Invocation();
  public readonly EngineAnimationChannelInvocation ChannelCall=new EngineAnimationChannelInvocation();
  public readonly GameEquipmentHandStateInvocation HandCall=new GameEquipmentHandStateInvocation();
  public readonly EngineEntitySyncedAnimationInvocation SyncedCall=new EngineEntitySyncedAnimationInvocation();
  public readonly Field<object> SyncedPrimary=new Field<object>();
  public readonly Field<uint> HandState=new Field<uint>();
  public bool HandStateReturned,SyncedEntered,SyncedReturned;
  public readonly List<uint> Stages=new List<uint>();
  public readonly List<Field<AnimationSet>> FirstOverrides=new List<Field<AnimationSet>>(),SecondOverrides=new List<Field<AnimationSet>>();
  public readonly Field<AnimationSet> FirstSet=new Field<AnimationSet>(),SecondSet=new Field<AnimationSet>();
  public readonly Field<uint> KindCapture=new Field<uint>(),PendingCapture=new Field<uint>();
  public readonly Field<object> ActorPrimary=new Field<object>(),MeshPrimary=new Field<object>();
  public readonly Field<EngineGraphicsResourceState> ResourceCapture=new Field<EngineGraphicsResourceState>();
  public readonly Field<float> SubmittedSpeed=new Field<float>();
  public readonly Field<int> SubmittedBlend=new Field<int>();
  public readonly Field<byte> SubmittedBool=new Field<byte>();
  public AnimationSet RequestedSet;public GameAnimationEntryState Entry;public GameMovementCharacterState Owner;public BorrowedMeshBounds Mesh;
  public uint RequestedAnimation;public int SelectedIndex;
  public bool Used,PendingWritten,EntryEntered,LoopWritten,ExtraWritten,ResourceCaptured,PlayerEntered,KindCaptured,PendingCaptured,MeshEntered,MeshReturned,ChannelAttempted,ChannelEntered,ChannelReturned,AnimationSubmitted,HandStateEntered,PlayerReturned,EntryReturned,Returned;
  public byte ReturnValue;
 }
 // Recovered024DE0/020840/Player3E2900 prefix on the existing owner and resource
 // identities. Engine imports are bound by disk evidence and actual Rea bodies.
 // ChannelReturned means the address getter returned; AnimationSubmitted means
 // the recovered channel state publication returned, never decoding/rendering.
 // No callback can fabricate those results or complete an unresolved Player tail.
 // shortcut: exclusive stable OWN typed storage, finite normal FP and valid signed
 // conversion range. Native MXCSR/FTZ/DAZ/invalid conversions and exception cleanup
 // are not reconstructed; a missing dependency preserves already-published fields.
 // Player's pending IDs outside17..49 take the observed no-table branch (including5).
 // Pending17..49 stop before reading unverified jump-table data.
 public static class GameAnimationPlayContracts {
  static uint At(GameAnimationPlayInvocation c,uint at){c.Native.At=at;c.Stages.Add(at);return at;}
  static T Need<T>(T value,uint at,string why)where T:class{if(value==null)throw new MissingContract(at,why);return value;}
  static float Scalar(float value,uint at){if(Single.IsNaN(value)||Single.IsInfinity(value)||(value!=0&&Math.Abs(value)<1.1754943508222875E-38f))throw new MissingContract(at,"Native nonfinite/subnormal FP controls are outside the declared OWN domain");return value;}
  static int Truncate(float value,uint at){value=Scalar(value,at);if((double)value< -2147483648.0||(double)value>=2147483648.0)throw new MissingContract(at,"Native invalid CVTTSS2SI depends on unobserved FP controls");return (int)value;}
  static float Multiply(float x,float y,uint at){x=Scalar(x,at);y=Scalar(y,at);return Scalar(BitConverter.ToSingle(BitConverter.GetBytes(x*y),0),at);}
  static Field<AnimationSet> SelectCurrent(GameMovementCharacterState owner,bool second,GameAnimationPlayInvocation c){
   uint load=second?0x3e2958u:0x3e2935u,test=second?0x3e295bu:0x3e2938u,fallback=second?0x3e296bu:0x3e294au;
   var captures=second?c.SecondOverrides:c.FirstOverrides;
   for(int index=0;index<2;index++){
    At(c,load);owner.Entity.RequireLive(c.Native.At);var fields=GameCharacterAnimationFields.ForOwner(owner);
    var value=(index==0?fields.Set2600:fields.Set2608).Snapshot();captures.Add(value);At(c,test);if(value.Read(c.Native.At)!=null)return value;
   }
   At(c,fallback);owner.Entity.RequireLive(c.Native.At);return GameCharacterAnimationFields.ForOwner(owner).Set2578.Snapshot();
  }
  static void PlayerSubmit(GameMoveAnimationProductionContext context,GameMovementCharacterState owner,GameAnimationPlayInvocation c){
   c.PlayerEntered=true;c.Owner=owner;
   c.FirstSet.CopyFrom(SelectCurrent(owner,false,c));At(c,0x3e2951);var first=Need(c.FirstSet.Read(c.Native.At),c.Native.At,"Null first current AnimationSet");c.KindCapture.CopyFrom(first.Kind08.Snapshot());c.KindCaptured=true;
   c.SecondSet.CopyFrom(SelectCurrent(owner,true,c));At(c,0x3e2972);var second=Need(c.SecondSet.Read(c.Native.At),c.Native.At,"Null second current AnimationSet");c.PendingCapture.CopyFrom(GameAnimationSetFields.ForSet(second).Pending0C.Snapshot());c.PendingCaptured=true;
   At(c,0x3e2979);int kind=unchecked((int)c.KindCapture.Read(c.Native.At));
   if(kind==17||(kind>24&&kind<=28)){At(c,0x3e2997);if(c.PendingCapture.Read(c.Native.At)==1)c.SubmittedBlend.Write(0);}
   else if(kind==45){At(c,0x3e2991);c.SubmittedBlend.Write(75);}
   At(c,0x3e299e);uint pending=c.PendingCapture.Read(c.Native.At);
   if(unchecked(pending-17u)<=32u){At(c,0x3e29af);throw new MissingContract(c.Native.At,"Pending17..49 uses unverified jump-table bytes; no switch guessed");}
   At(c,0x3e29c6);owner.Entity.RequireLive(c.Native.At);c.MeshPrimary.CopyFrom(owner.Entity.Virtual00.Snapshot());At(c,0x3e29cc);ActorBoundingBoxContracts.RequireObservedDispatch(c.MeshPrimary,c.Native.At);c.MeshEntered=true;
   c.Mesh=ActorBoundingBoxContracts.GetMeshInstance06DA30(context.Speed.Actor,c.MeshCall);c.MeshReturned=true;
   At(c,0x3e29d8);c.ChannelAttempted=true;c.ChannelEntered=true;
   var channel=EngineAnimationChannelContracts.GetAnimChannel0C24E0(c.Mesh,0,c.ChannelCall);c.ChannelReturned=true;
   At(c,0x3e29f2);EngineAnimationChannelContracts.PlayAnimation0385B0(channel,c.ResourceCapture,c.SubmittedBool,c.SubmittedSpeed,c.SubmittedBlend.Read(c.Native.At),c.ChannelCall);c.AnimationSubmitted=true;
   At(c,0x3e29ff);c.HandStateEntered=true;
   c.HandState.Write(GameEquipmentHandStateContracts.GetHandState285E50(owner,context.Lookup,c.HandCall));c.HandStateReturned=true;
   At(c,0x3e2a04);owner.Entity.RequireLive(c.Native.At);c.SyncedPrimary.CopyFrom(owner.Entity.Virtual00.Snapshot());
   // Saved Player+2C0 -> Game5DA02E FF25 -> IAT609358 -> Engine07F490.
   // This fresh dispatch must not reuse the earlier Player+2F8 capture.
   At(c,0x3e2a2f);if(!Object.ReferenceEquals(c.SyncedPrimary.Read(c.Native.At),GameMovementPrimaryTable.Player))throw new MissingContract(c.Native.At,"Unbound current primary+2C0 synced-animation target");
   c.SyncedEntered=true;EngineEntitySyncedAnimationContracts.PlaySyncedAnimation07F490(context.Speed.Actor,c.PendingCapture,c.HandState,c.SubmittedBool,c.SubmittedBlend,c.SyncedCall);c.SyncedReturned=true;
  }
  static byte EntryPlay(GameMoveAnimationProductionContext context,MoveCharacter role,GameAnimationEntryState entry,Field<float> speed,Field<byte> loop,Field<uint> extra,GameAnimationPlayInvocation c){
   c.EntryEntered=true;var loopCapture=loop.Snapshot();var extraCapture=extra.Snapshot();
   At(c,0x020854);Need(entry,c.Native.At,"Null entry at loop store");entry.Byte28.CopyFrom(loopCapture);c.LoopWritten=true;At(c,0x020857);entry.Dword24.CopyFrom(extraCapture);c.ExtraWritten=true;
   At(c,0x02085b);byte enabled;if(loopCapture.Read(c.Native.At)!=0)enabled=1;else{At(c,0x02085f);enabled=extraCapture.Read(c.Native.At)!=0?(byte)1:(byte)0;}c.SubmittedBool.Write(enabled);
   At(c,0x020867);c.ResourceCapture.CopyFrom(entry.Resource10.Snapshot());c.ResourceCaptured=true;At(c,0x02086b);
   if(c.ResourceCapture.Read(c.Native.At)==null){c.EntryReturned=true;At(c,0x0208a0);return 0;}
   At(c,0x020870);c.SubmittedBlend.Write(Truncate(entry.Float18.Read(c.Native.At),c.Native.At));
   At(c,0x020875);Need(context,c.Native.At,"Original Actor composition is missing");var view=context.RequireOwner(role,c.Native.At);var owner=view.Owner;owner.Entity.RequireLive(c.Native.At);c.ActorPrimary.CopyFrom(owner.Entity.Virtual00.Snapshot());
   At(c,0x02087a);c.SubmittedSpeed.Write(Multiply(speed.Read(c.Native.At),entry.Float1C.Read(c.Native.At),c.Native.At));
   At(c,0x02088c);if(!Object.ReferenceEquals(c.ActorPrimary.Read(c.Native.At),GameMovementPrimaryTable.Player))throw new MissingContract(c.Native.At,"Only observed Player+2F8 maps to3E2900; Character/unknown targets remain unresolved");
   PlayerSubmit(context,owner,c);c.PlayerReturned=true;At(c,0x020893);c.EntryReturned=true;return 1;
  }
  public static byte Play024DE0(GameMoveAnimationProductionContext context,MoveCharacter role,Field<AnimationSet> set,uint animation,object unusedName,Field<float> speed,Field<byte> loop,Field<uint> extra,GameAnimationPlayInvocation c){
   if(set==null||speed==null||loop==null||extra==null||c==null)throw new ArgumentNullException("OWN input cells/invocation");if(c.Used)throw new InvalidOperationException("Use a fresh Play invocation");c.Used=true;c.RequestedAnimation=animation;
   try{
    At(c,0x024df0);c.RequestedSet=Need(set.Read(c.Native.At),c.Native.At,"Null AnimationSet at pending store");var fields=GameAnimationSetFields.ForSet(c.RequestedSet);fields.Pending0C.Write(animation);c.PendingWritten=true;
    At(c,animation<=51?0x024dfbu:0x024e02u);c.SelectedIndex=animation<=51?(int)animation:0;var entryCapture=fields.Entry(c.SelectedIndex).Snapshot();At(c,0x024e0d);c.Entry=Need(entryCapture.Read(c.Native.At),c.Native.At,"Null animation entry at table read");var table=c.Entry.Virtual00.Snapshot();var capturedSpeed=speed.Snapshot();
    At(c,0x024e23);if(table.Read(c.Native.At)!=0x1806d4d48UL)throw new MissingContract(c.Native.At,"Unbound entry virtual+8");byte result=EntryPlay(context,role,c.Entry,capturedSpeed,loop,extra,c);At(c,0x024e27);
    if(result!=0){At(c,0x024e2b);c.RequestedSet.Kind08.Write(animation);At(c,0x024e30);fields.Pending0C.Write(0);}
    c.ReturnValue=result;c.Returned=true;At(c,result!=0?0x024e41u:0x024e4eu);return result;
   }catch(MissingContract e){c.Native.At=e.Address;throw;}
  }
 }
}
