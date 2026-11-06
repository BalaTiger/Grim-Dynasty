using System;
using GrimDynasty.ReverseClient.Actions;

public static class EngineAnimationChannelTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}public static int Assertions{get{return assertions;}}
 static void Check(bool yes,string why){assertions++;if(!yes)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(Object.Equals(expected,actual),why+": "+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Unknown<T>(Field<T> cell,string why){try{cell.Read(0);throw new Exception(why+" unexpectedly known");}catch(MissingContract){assertions++;}}
 static void Missing(uint at,Action action,string why){try{action();throw new Exception(why+" unexpectedly returned");}catch(MissingContract e){Eq(at,e.Address,why);}}
 static void Test(string name,Action action){action();scenarios++;Console.WriteLine("PASS channel: "+name);}
 sealed class Fixture {
  public readonly BorrowedMeshBounds Mesh;
  public readonly EngineAnimationChannelState Channel;
  public readonly EngineGraphicsResourceState Resource=new EngineGraphicsResourceState();
  public Fixture(){var entities=new EntityMovementRegistry();var bounds=new ActorBoundingBoxContext(entities);Mesh=bounds.BindMesh(new object());Channel=EngineAnimationChannelState.ForMesh(Mesh);}
  public EngineAnimationChannelInvocation Play(int blend){var c=new EngineAnimationChannelInvocation();var channel=EngineAnimationChannelContracts.GetAnimChannel0C24E0(Mesh,0,c);EngineAnimationChannelContracts.PlayAnimation0385B0(channel,Field<EngineGraphicsResourceState>.Of(Resource),Field<byte>.Of(0xa5),Field<float>.Of(2.5f),blend,c);return c;}
  public EngineAnimationQueueNode Empty(){var sentinel=new EngineAnimationQueueNode();sentinel.Next00.Write(sentinel);Channel.Sentinel38.Write(sentinel);return sentinel;}
 }
 public static void Run(){scenarios=assertions=0;
  Test("GetAnimChannel computes an address without reading channel fields",delegate{
   var f=new Fixture();var c=new EngineAnimationChannelInvocation();Same(f.Channel,EngineAnimationChannelContracts.GetAnimChannel0C24E0(f.Mesh,0,c),"Canonical inline channel");Eq(0x110L,c.ByteOffset,"Index0 offset");Check(c.GetterReturned&&!c.PlayEntered,"Only getter returned");Unknown(f.Channel.Resource10,"No resource initialized");Unknown(f.Channel.Speed20,"No speed initialized");
   var negative=new EngineAnimationChannelInvocation();Missing(0x0c24ee,delegate{EngineAnimationChannelContracts.GetAnimChannel0C24E0(f.Mesh,0xffffffffu,negative);},"Signed nonzero declared-storage boundary");Eq(-1L,negative.SignedIndex,"MOVSXD sign");Eq(0xa8L,negative.ByteOffset,"Actual negative address arithmetic retained");Check(!negative.GetterReturned,"No invented index clamp");
  });
  Test("First publication shares mesh speed and preserves exact write widths",delegate{
   var f=new Fixture();f.Channel.Resource10.Write(null);f.Channel.Byte19.Write(0x19);f.Channel.Byte1A.Write(0x1a);f.Channel.Byte1B.Write(0x1b);var c=f.Play(75);
   Check(c.Returned&&c.FieldsPublished,"Observed no-old-resource path returned");Same(f.Resource,f.Channel.Resource10.Read(0),"Same undecomposed resource pointer");Eq((byte)0xa5,f.Channel.Byte18.Read(0),"Raw byte preserved");Eq((byte)0x19,f.Channel.Byte19.Read(0),"Adjacent padding19 untouched");Eq((byte)0x1a,f.Channel.Byte1A.Read(0),"Adjacent padding1A untouched");Eq((byte)0x1b,f.Channel.Byte1B.Read(0),"Adjacent padding1B untouched");Eq(2.5f,f.Channel.Speed20.Read(0),"Speed publication");Eq(0u,f.Channel.Dword1C.Read(0),"Clock reset");Eq(0UL,f.Channel.Qword24.Read(0),"64-bit zero24");Eq(0u,f.Channel.Dword2C.Read(0),"32-bit zero2C");Eq(0UL,f.Channel.Qword30.Read(0),"64-bit zero30");Unknown(f.Channel.Sentinel38,"Nonzero blend never reads list");Unknown(f.Channel.Count40,"Count neither read nor written");Unknown(f.Resource.Virtual00,"No resource decoding inferred");
   int[] widths={4,8,1,4,8,4,8};uint[] offsets={0x1c,0x10,0x18,0x20,0x24,0x2c,0x30};Eq(7,c.Writes.Count,"Exactly seven field writes");for(int n=0;n<7;n++){Eq(widths[n],c.Writes[n].Width,"Width"+n);Eq(offsets[n],c.Writes[n].Offset,"Offset"+n);Same(f.Channel,c.Writes[n].Owner,"Shared target"+n);}
   Same(f.Channel,EngineAnimationChannelState.ForMesh(f.Mesh),"Second binding shares every mutable cell");
  });
  Test("A register MOV can publish Unknown speed and resource without decoding",delegate{
   var f=new Fixture();f.Channel.Resource10.Write(null);var c=new EngineAnimationChannelInvocation();EngineAnimationChannelContracts.PlayAnimation0385B0(f.Channel,new Field<EngineGraphicsResourceState>(),new Field<byte>(),new Field<float>(),-2,c);Check(c.Returned,"Native branch has no read of supplied values");Unknown(f.Channel.Resource10,"Opaque resource copied");Unknown(f.Channel.Byte18,"Opaque loop copied");Unknown(f.Channel.Speed20,"Opaque speed copied");
  });
  Test("Zero blend with an explicitly empty sentinel reaches publication",delegate{
   var f=new Fixture();f.Channel.Resource10.Write(null);var sentinel=f.Empty();var c=f.Play(0);Check(c.Returned,"Empty actual branch returns");Eq(0,c.ClearedNodes.Count,"No nonexistent old nodes cleared");Same(sentinel,f.Channel.Sentinel38.Read(0),"Sentinel preserved");Unknown(sentinel.Blend34,"Sentinel data is not a node");Unknown(f.Channel.Count40,"No guessed list count");
  });
  Test("Zero blend follows declared links and clears each old-node blend",delegate{
   var f=new Fixture();f.Channel.Resource10.Write(null);var s=f.Empty();var a=new EngineAnimationQueueNode();var b=new EngineAnimationQueueNode();s.Next00.Write(a);a.Next00.Write(b);b.Next00.Write(s);a.Blend34.Write(12);b.Blend34.Write(33);var c=f.Play(0);Eq(0u,a.Blend34.Read(0),"First blend zero");Eq(0u,b.Blend34.Read(0),"Second blend zero");Eq(2,c.ClearedNodes.Count,"Both actual links traversed");Same(a,c.ClearedNodes[0],"First identity");Same(b,c.ClearedNodes[1],"Second identity");Eq(9,c.Writes.Count,"Two4-byte clears then7 publication stores");Eq(0x038680u,c.Writes[0].At,"Actual clear instruction");Check(c.Returned,"List branch completed");
  });
  Test("A missing next link retains clears but prevents new animation publication",delegate{
   var f=new Fixture();f.Channel.Resource10.Write(null);var s=f.Empty();var a=new EngineAnimationQueueNode();s.Next00.Write(a);a.Blend34.Write(88);var c=new EngineAnimationChannelInvocation();Missing(0x038686,delegate{EngineAnimationChannelContracts.PlayAnimation0385B0(f.Channel,Field<EngineGraphicsResourceState>.Of(f.Resource),Field<byte>.Of(1),Field<float>.Of(1),0,c);},"Unknown next pointer consumed by comparison");Eq(0u,a.Blend34.Read(0),"Prefix clear retained");Same(null,f.Channel.Resource10.Read(0),"No new resource store");Unknown(f.Channel.Dword1C,"Clock reset not reached");Eq(1,c.Writes.Count,"Only preceding clear");Check(!c.Returned&&!c.FieldsPublished,"Unsupported is not successful publication");
  });
  Test("Nonempty old resource preserves pointer-capture priority before unknown insertion",delegate{
   var f=new Fixture();var old=new EngineGraphicsResourceState();f.Channel.Resource10.Write(old);var c=new EngineAnimationChannelInvocation();Missing(0x038611,delegate{EngineAnimationChannelContracts.PlayAnimation0385B0(f.Channel,Field<EngineGraphicsResourceState>.Of(f.Resource),Field<byte>.Of(1),Field<float>.Of(1),5,c);},"Unknown sentinel consumed after native record-copy prefix");Check(c.OldRecordCopyReached&&!c.InsertionAttempted,"No allocation fabricated");Same(old,f.Channel.Resource10.Read(0),"Old resource retained");Eq(0,c.Writes.Count,"No external writes");
   var sentinel=f.Empty();var d=new EngineAnimationChannelInvocation();Missing(0x038620,delegate{EngineAnimationChannelContracts.PlayAnimation0385B0(f.Channel,Field<EngineGraphicsResourceState>.Of(f.Resource),Field<byte>.Of(1),Field<float>.Of(1),5,d);},"Stops at true01F3D0 dependency");Check(d.InsertionAttempted&&!d.Returned,"Helper not substituted");Same(sentinel,d.CapturedFirst.Read(0),"Same node pointer passed");Unknown(d.CapturedPrevious,"Opaque Previous08 argument can remain unknown");Eq(0,d.Writes.Count,"No count or linkage update before unknown helper");
  });
  Test("Repeated play cannot silently overwrite a prior published animation",delegate{
   var f=new Fixture();f.Channel.Resource10.Write(null);var first=f.Play(1);Check(first.Returned,"First publication completed");var second=new EngineGraphicsResourceState();var c=new EngineAnimationChannelInvocation();Missing(0x038611,delegate{EngineAnimationChannelContracts.PlayAnimation0385B0(f.Channel,Field<EngineGraphicsResourceState>.Of(second),Field<byte>.Of(0),Field<float>.Of(9),1,c);},"Second call takes old-animation branch");Same(f.Resource,f.Channel.Resource10.Read(0),"First resource still owned");Eq(2.5f,f.Channel.Speed20.Read(0),"First speed retained");Eq((byte)0xa5,f.Channel.Byte18.Read(0),"First loop retained");Check(!c.FieldsPublished,"No false second publication");
  });
  Test("Released mesh can form an address but cannot dereference channel storage",delegate{
   var f=new Fixture();f.Mesh.MarkReleased();var c=new EngineAnimationChannelInvocation();Same(f.Channel,EngineAnimationChannelContracts.GetAnimChannel0C24E0(f.Mesh,0,c),"Getter only does address arithmetic");Missing(0x0385e9,delegate{EngineAnimationChannelContracts.PlayAnimation0385B0(f.Channel,Field<EngineGraphicsResourceState>.Of(f.Resource),Field<byte>.Of(1),Field<float>.Of(1),1,c);},"Lifetime checked at first actual dereference");Eq(0,c.Writes.Count,"Released storage unmodified");
  });
 }
}
