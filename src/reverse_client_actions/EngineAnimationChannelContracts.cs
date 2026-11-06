using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // Declared OWN storage only. Binding never constructs or zero-fills a native
 // mesh/channel/list. The one authoritative mesh speed cell is shared at+130.
 public sealed class EngineAnimationQueueNode {
  public readonly Field<EngineAnimationQueueNode> Next00=new Field<EngineAnimationQueueNode>(),Previous08=new Field<EngineAnimationQueueNode>();
  public readonly Field<uint> Blend34=new Field<uint>();
 }
 public sealed class EngineAnimationChannelState {
  static readonly ConditionalWeakTable<BorrowedMeshBounds,EngineAnimationChannelState> states=new ConditionalWeakTable<BorrowedMeshBounds,EngineAnimationChannelState>();
  public readonly BorrowedMeshBounds Mesh;
  public readonly Field<EngineGraphicsResourceState> Resource10=new Field<EngineGraphicsResourceState>();
  public readonly Field<byte> Byte18=new Field<byte>(),Byte19=new Field<byte>(),Byte1A=new Field<byte>(),Byte1B=new Field<byte>();
  public readonly Field<uint> Dword1C=new Field<uint>(),Dword2C=new Field<uint>();
  public readonly Field<float> Speed20;
  public readonly Field<ulong> Qword24=new Field<ulong>(),Qword30=new Field<ulong>(),Count40=new Field<ulong>();
  public readonly Field<EngineAnimationQueueNode> Sentinel38=new Field<EngineAnimationQueueNode>();
  EngineAnimationChannelState(BorrowedMeshBounds mesh){Mesh=mesh;Speed20=EngineMeshAnimationSpeedFields.ForMesh(mesh).Speed130;}
  public static EngineAnimationChannelState ForMesh(BorrowedMeshBounds mesh){if(mesh==null)throw new ArgumentNullException("mesh");return states.GetValue(mesh,delegate(BorrowedMeshBounds value){return new EngineAnimationChannelState(value);});}
 }
 public sealed class EngineAnimationChannelWrite {
  public readonly object Owner;public readonly uint At,Offset;public readonly int Width;
  internal EngineAnimationChannelWrite(object owner,uint at,uint offset,int width){Owner=owner;At=at;Offset=offset;Width=width;}
 }
 public sealed class EngineAnimationChannelInvocation {
  public readonly Invocation Native=new Invocation();
  public readonly List<EngineAnimationChannelWrite> Writes=new List<EngineAnimationChannelWrite>();
  public readonly List<EngineAnimationQueueNode> ClearedNodes=new List<EngineAnimationQueueNode>();
  public readonly Field<EngineGraphicsResourceState> OldResource=new Field<EngineGraphicsResourceState>();
  public readonly Field<EngineAnimationQueueNode> CapturedSentinel=new Field<EngineAnimationQueueNode>(),CapturedFirst=new Field<EngineAnimationQueueNode>(),CapturedPrevious=new Field<EngineAnimationQueueNode>();
  public EngineAnimationChannelState Channel;public long SignedIndex,ByteOffset;
  public bool Used,GetterReturned,PlayEntered,OldRecordCopyReached,InsertionAttempted,FieldsPublished,Returned;
 }
 // Actual Rea Engine0C24E0/0385B0, r1-engine-animation-channel-run1.
 // Supports declared index0; old-resource-null publication and finite existing
 // queue traversal. Non-null prior animation stops at the first unknown helper
 // 01F3D0 AFTER its pointer arguments are captured. This is state publication,
 // not animation decoding, rendering, native initialization or frame advancement.
 public static class EngineAnimationChannelContracts {
  static T Need<T>(T value,uint at,string message)where T:class{if(value==null)throw new MissingContract(at,message);return value;}
  static uint At(EngineAnimationChannelInvocation c,uint at){c.Native.At=at;return at;}
  static void Write<T>(EngineAnimationChannelInvocation c,object owner,Field<T> field,T value,uint at,uint offset,int width){At(c,at);field.Write(value);c.Writes.Add(new EngineAnimationChannelWrite(owner,at,offset,width));}
  static void Copy<T>(EngineAnimationChannelInvocation c,object owner,Field<T> field,Field<T> value,uint at,uint offset,int width){At(c,at);field.CopyFrom(value);c.Writes.Add(new EngineAnimationChannelWrite(owner,at,offset,width));}
  public static EngineAnimationChannelState GetAnimChannel0C24E0(BorrowedMeshBounds mesh,uint index,EngineAnimationChannelInvocation c){
   if(c==null)throw new ArgumentNullException("invocation");if(c.Used)throw new InvalidOperationException("Use a fresh channel invocation");c.Used=true;
   At(c,0x0c24e0);c.SignedIndex=unchecked((int)index);At(c,0x0c24e3);c.ByteOffset=c.SignedIndex*0x68L+0x110;
   At(c,0x0c24ee);if(c.SignedIndex!=0||mesh==null)throw new MissingContract(c.Native.At,"Address arithmetic completed; only a non-null existing mesh index0 has declared OWN storage, not a native bounds check");
   c.Channel=EngineAnimationChannelState.ForMesh(mesh);At(c,0x0c24f1);c.GetterReturned=true;return c.Channel;
  }
  public static void PlayAnimation0385B0(EngineAnimationChannelState channel,Field<EngineGraphicsResourceState> resource,Field<byte> loop,Field<float> speed,int blend,EngineAnimationChannelInvocation c){
   if(c==null||resource==null||loop==null||speed==null)throw new ArgumentNullException("OWN call cells");if(c.PlayEntered)throw new InvalidOperationException("Use a fresh Play invocation");c.PlayEntered=true;c.Channel=channel;
   // Registers are opaque captures until the corresponding native store/use.
   var resourceCapture=resource.Snapshot();var loopCapture=loop.Snapshot();var speedCapture=speed.Snapshot();
   try{
    At(c,0x0385e9);Need(channel,c.Native.At,"Null channel at old-resource comparison").Mesh.RequireLive(c.Native.At);c.OldResource.CopyFrom(channel.Resource10.Snapshot());
    if(c.OldResource.Read(c.Native.At)!=null){
     // Native MOVUPS/MOVSD copy40 bytes without consuming Unknown field values.
     // We record reaching that prefix, not a recovered 40-byte payload. It has
     // no externally visible writes before the unresolved allocation helper.
     At(c,0x03860a);c.OldRecordCopyReached=true;
     At(c,0x03860d);c.CapturedSentinel.CopyFrom(channel.Sentinel38.Snapshot());
     At(c,0x038611);var sentinel=Need(c.CapturedSentinel.Read(c.Native.At),c.Native.At,"Null previous-animation sentinel");c.CapturedFirst.CopyFrom(sentinel.Next00.Snapshot());
     At(c,0x038619);var first=Need(c.CapturedFirst.Read(c.Native.At),c.Native.At,"Null first previous-animation node");c.CapturedPrevious.CopyFrom(first.Previous08.Snapshot());
     At(c,0x038620);c.InsertionAttempted=true;throw new MissingContract(c.Native.At,"Engine01F3D0 node construction and097980 release remain unclosed; no insertion or release success substituted");
    }
    At(c,0x03866b);
    if(blend==0){
     At(c,0x03866f);c.CapturedSentinel.CopyFrom(channel.Sentinel38.Snapshot());At(c,0x038673);var sentinel=Need(c.CapturedSentinel.Read(c.Native.At),c.Native.At,"Null zero-blend sentinel");var current=sentinel.Next00.Snapshot();
     At(c,0x038676);var node=current.Read(c.Native.At);var visited=new HashSet<EngineAnimationQueueNode>();
     while(!Object.ReferenceEquals(node,sentinel)){
      At(c,0x038680);Need(node,c.Native.At,"Null previous-animation node at blend store");
      if(visited.Count>=4096||!visited.Add(node))throw new MissingContract(c.Native.At,"Declared finite OWN queue exceeded or cycle does not reach sentinel; prior writes retained");
      Write(c,node,node.Blend34,0u,0x038680,0x34,4);c.ClearedNodes.Add(node);
      At(c,0x038683);current=node.Next00.Snapshot();At(c,0x038686);node=current.Read(c.Native.At);sentinel=channel.Sentinel38.Read(c.Native.At);
     }
    }
    Write(c,channel,channel.Dword1C,0u,0x03868c,0x1c,4);
    Copy(c,channel,channel.Resource10,resourceCapture,0x03868f,0x10,8);
    Copy(c,channel,channel.Byte18,loopCapture,0x038693,0x18,1);
    Copy(c,channel,channel.Speed20,speedCapture,0x038697,0x20,4);
    Write(c,channel,channel.Qword24,0UL,0x0386a9,0x24,8);
    Write(c,channel,channel.Dword2C,0u,0x0386b0,0x2c,4);
    Write(c,channel,channel.Qword30,0UL,0x0386b3,0x30,8);
    c.FieldsPublished=true;At(c,0x0386d6);c.Returned=true;
   }catch(MissingContract e){c.Native.At=e.Address;throw;}
  }
 }
}
