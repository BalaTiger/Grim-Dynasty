using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace GrimDynasty.ReverseClient.Actions {
 // Explicit complete, disjoint byte input; no C-string truncation or key/hash transform.
 public sealed class EngineResourceNameInput {
  readonly byte[] bytes;
  public EngineResourceNameInput(byte[] value){if(value==null)throw new ArgumentNullException("value");bytes=(byte[])value.Clone();}
  public int Length{get{return bytes.Length;}}
  internal byte Byte(int index){return bytes[index];}
 }
 public sealed class EngineResourceByteStorage {
  readonly Field<byte>[] cells;
  internal EngineResourceByteStorage(int size){cells=new Field<byte>[size];for(int n=0;n<size;n++)cells[n]=new Field<byte>();}
  public int Extent{get{return cells.Length;}}
  public Field<byte> Byte(int index){if(index<0||index>=cells.Length)throw new ArgumentOutOfRangeException("index");return cells[index];}
 }
 public sealed class EngineResourceStringState {
  public readonly EngineResourceByteStorage Inline=new EngineResourceByteStorage(16);
  public readonly Field<EngineResourceByteStorage> Heap=new Field<EngineResourceByteStorage>();
  public readonly Field<ulong> Length=new Field<ulong>(),Capacity=new Field<ulong>();
  public byte ReadByte(int index,uint at){
   var storage=Capacity.Read(at)<16?Inline:Heap.Read(at);
   if(storage==null)throw new MissingContract(at,"Null name storage at byte read");
   if(index<0||index>=storage.Extent)throw new MissingContract(at,"Name byte lies outside declared OWN storage");
   return storage.Byte(index).Read(at);
  }
  internal void PublishHeap(EngineResourceByteStorage heap){
   // The native pointer overwrites eight inline bytes; its address bits are not invented.
   for(int n=0;n<8;n++)Inline.Byte(n).Forget();Heap.Write(heap);
  }
 }
 // OWN platform object: allocation and initialization are separate so an initialization
 // failure retains the published +68 identity. This is not Win32 CS storage or its EH.
 public sealed class EngineResourceOwnMonitor {
  object gate;
  internal void Initialize(){gate=new object();}
  public bool Initialized{get{return gate!=null;}}
  public bool IsHeldByCurrentThread{get{return gate!=null&&Monitor.IsEntered(gate);}}
  public void Enter(){if(gate==null)throw new InvalidOperationException("OWN monitor is uninitialized");Monitor.Enter(gate);}
  public void Exit(){if(gate==null)throw new InvalidOperationException("OWN monitor is uninitialized");Monitor.Exit(gate);}
 }
 // One object is BOTH Resource and GraphicsAnim; no copied base view or later synchronization.
 public sealed class EngineGraphicsResourceState {
  public readonly Field<ulong> Virtual00=new Field<ulong>();
  public readonly EngineResourceStringState Name08=new EngineResourceStringState();
  public readonly Field<object> Loader28=new Field<object>(),Manager38=new Field<object>();
  public readonly Field<uint> Type30=new Field<uint>(),Dword40=new Field<uint>(),Dword4C=new Field<uint>();
  public readonly Field<ulong> Qword44=new Field<ulong>(),Qword50=new Field<ulong>(),Virtual58=new Field<ulong>(),Virtual60=new Field<ulong>();
  public readonly Field<EngineResourceOwnMonitor> Synchronization68=new Field<EngineResourceOwnMonitor>();
  public readonly Field<uint> Dword70=new Field<uint>(),Dword74=new Field<uint>(),Dword80=new Field<uint>();
  public readonly Field<ulong> Qword78=new Field<ulong>();
  public readonly EngineResourceStringState Name88=new EngineResourceStringState();
  public readonly Field<object> GraphicsA8=new Field<object>();
  public readonly Field<ulong> QwordB0=new Field<ulong>(),QwordB8=new Field<ulong>(),QwordC0=new Field<ulong>(),QwordC8=new Field<ulong>();
  public readonly Field<ulong> QwordD0=new Field<ulong>(),QwordD8=new Field<ulong>(),QwordE0=new Field<ulong>(),QwordE8=new Field<ulong>();
  public readonly Field<ulong> QwordF0=new Field<ulong>(),QwordF8=new Field<ulong>(),Qword100=new Field<ulong>(),Qword108=new Field<ulong>(),Qword110=new Field<ulong>();
  public readonly EngineResourceStringState Name118=new EngineResourceStringState(),Name138=new EngineResourceStringState(),Name158=new EngineResourceStringState(),Name178=new EngineResourceStringState();
  public readonly Field<byte> Byte198=new Field<byte>();
 }
 // Borrow the existing WorldEngine identity. Binding publishes no graphics object.
 public sealed class EngineGraphicsResourceEngineFields {
  static readonly ConditionalWeakTable<WorldEngine,EngineGraphicsResourceEngineFields> engines=new ConditionalWeakTable<WorldEngine,EngineGraphicsResourceEngineFields>();
  public readonly WorldEngine Owner;
  public readonly Field<object> Graphics1F8=new Field<object>();
  EngineGraphicsResourceEngineFields(WorldEngine owner){Owner=owner;}
  public static EngineGraphicsResourceEngineFields ForEngine(WorldEngine engine){if(engine==null)throw new ArgumentNullException("engine");return engines.GetValue(engine,delegate(WorldEngine owner){return new EngineGraphicsResourceEngineFields(owner);});}
 }
 public sealed class EngineResourceConstructionWrite {
  public readonly uint At,Offset;public readonly int Width;public readonly bool Heap;
  internal EngineResourceConstructionWrite(uint at,uint offset,int width,bool heap){At=at;Offset=offset;Width=width;Heap=heap;}
 }
 public sealed class EngineGraphicsResourceConstructionInvocation {
  public readonly Invocation Native=new Invocation();
  public readonly List<EngineResourceConstructionWrite> Writes=new List<EngineResourceConstructionWrite>();
  public readonly Field<WorldEngine> CapturedEngine=new Field<WorldEngine>();
  public readonly Field<object> CapturedGraphics=new Field<object>();
  public EngineGraphicsResourceState Target,AllocatedResource,BaseResult,GraphicsResult,Result;
  public bool BaseEntered,NameCopyReturned,BaseReturned,GraphicsEntered,GraphicsReturned,Returned;
  public int NativeRequestedAllocationBytes;
  internal void Reset(){Native.At=0;Writes.Clear();CapturedEngine.Forget();CapturedGraphics.Forget();Target=AllocatedResource=BaseResult=GraphicsResult=Result=null;BaseEntered=NameCopyReturned=BaseReturned=GraphicsEntered=GraphicsReturned=Returned=false;NativeRequestedAllocationBytes=0;}
 }
 // Engine SHA4c2ea14c...: r1-resource-constructor-run1/04..06 and
 // r1-graphics-animation-constructor-run1/04..06; factory create-and-key-run1/04..06.
 // String normal copy: old rea-object-name-assignment/04..06 and name-growth/04..06.
 // shortcut: disjoint complete names <=65536 bytes, stable sequential storage and normal
 // OWN allocation/Monitor success; native allocator failure/unwind, aliases and concurrency
 // remain unmodeled. Add those only when a production caller needs their proven contracts.
 public static class EngineGraphicsResourceConstruction {
  public const int MaximumNameBytes=65536;
  static void Note(EngineGraphicsResourceConstructionInvocation c,uint at,uint offset,int width,bool heap){c.Native.At=at;c.Writes.Add(new EngineResourceConstructionWrite(at,offset,width,heap));}
  static void Put<T>(EngineGraphicsResourceConstructionInvocation c,Field<T> field,T value,uint at,uint offset,int width){c.Native.At=at;field.Write(value);Note(c,at,offset,width,false);}
  static void Copy<T>(EngineGraphicsResourceConstructionInvocation c,Field<T> field,Field<T> source,uint at,uint offset){c.Native.At=at;field.CopyFrom(source);Note(c,at,offset,8,false);}
  static void Empty(EngineResourceStringState s,uint offset,uint capAt,uint lengthAt,uint byteAt,EngineGraphicsResourceConstructionInvocation c){
   Put(c,s.Capacity,15UL,capAt,offset+24,8);Put(c,s.Length,0UL,lengthAt,offset+16,8);Put(c,s.Inline.Byte(0),(byte)0,byteAt,offset,1);
   // The inline byte store invalidates any prior typed pointer view of those bytes.
   s.Heap.Forget();
  }
  static void CopyName(EngineResourceStringState s,Field<EngineResourceNameInput> source,EngineGraphicsResourceConstructionInvocation c){
   c.Native.At=0x023195;var name=source.Read(c.Native.At);if(name==null)throw new MissingContract(c.Native.At,"Null name input at native source-length load");
   // OWN coverage rejection occurs after the caller's actual empty-string prefix.
   if(name.Length>MaximumNameBytes)throw new MissingContract(c.Native.At,"Name exceeds explicit OWN complete-byte domain");
   int length=name.Length;EngineResourceByteStorage storage=s.Inline;bool heap=length>15;
   if(heap){
    // Fresh capacity15 and old length0 make22F70's observed growth exactly length|15.
    ulong capacity=(ulong)length|15UL;c.Native.At=0x022fe7;storage=new EngineResourceByteStorage(checked((int)capacity+1));
    Put(c,s.Capacity,15UL,0x023044,0x20,8);Put(c,s.Length,0UL,0x02304c,0x18,8);Put(c,s.Inline.Byte(0),(byte)0,0x023063,8,1);
    c.Native.At=0x023066;s.PublishHeap(storage);Note(c,0x023066,8,8,false);
    Put(c,s.Capacity,capacity,0x023069,0x20,8);Put(c,s.Length,0UL,0x02306d,0x18,8);
    c.Native.At=0x02307b;storage.Byte(0).Write(0);Note(c,0x02307b,0,1,true);
   }
   if(length==0){Put(c,s.Length,0UL,0x02324b,0x18,8);Put(c,s.Inline.Byte(0),(byte)0,0x02325c,8,1);}
   else{
    c.Native.At=0x023270;for(int n=0;n<length;n++)storage.Byte(n).Write(name.Byte(n));Note(c,0x023270,heap?0u:8u,length,heap);
    Put(c,s.Length,(ulong)length,0x02327a,0x18,8);c.Native.At=0x023288;storage.Byte(length).Write(0);Note(c,0x023288,heap?(uint)length:(uint)(8+length),1,heap);
   }
   c.Native.At=0x0232a4;c.NameCopyReturned=true;
  }
  static EngineGraphicsResourceState Resource(EngineGraphicsResourceState s,Field<object> manager,Field<EngineResourceNameInput> name,uint type,EngineGraphicsResourceConstructionInvocation c){
   c.BaseEntered=true;c.Native.At=0x199970;if(s==null)throw new MissingContract(0x19999d,"Null original resource storage");
   var managerCapture=manager.Snapshot();var nameCapture=name.Snapshot();
   Put(c,s.Virtual00,0x180342cc8UL,0x19999d,0,8);Empty(s.Name08,8,0x1999a4,0x1999ae,0x1999c1,c);
   c.Native.At=0x1999ce;CopyName(s.Name08,nameCapture,c);
   Put<object>(c,s.Loader28,null,0x1999d4,0x28,8);Put(c,s.Type30,type,0x1999d8,0x30,4);
   Put(c,s.Dword40,0u,0x1999db,0x40,4);Put(c,s.Qword44,0UL,0x1999de,0x44,8);Put(c,s.Dword4C,0u,0x1999e2,0x4c,4);
   Put(c,s.Virtual58,0x18033d398UL,0x1999f5,0x58,8);Put(c,s.Virtual60,0x180348880UL,0x1999ff,0x60,8);
   Put<EngineResourceOwnMonitor>(c,s.Synchronization68,null,0x199a03,0x68,8);
   c.Native.At=0x199a0c;var monitor=new EngineResourceOwnMonitor();Put(c,s.Synchronization68,monitor,0x199a11,0x68,8);
   c.Native.At=0x199a18;monitor.Initialize();
   Put(c,s.Dword70,0u,0x199a1e,0x70,4);Put(c,s.Qword78,0UL,0x199a21,0x78,8);Copy(c,s.Manager38,managerCapture,0x199a25,0x38);
   Put(c,s.Dword74,4u,0x199a29,0x74,4);Put(c,s.Qword50,0UL,0x199a30,0x50,8);
   c.BaseResult=s;c.BaseReturned=true;c.Native.At=0x199a43;return s;
  }
  static EngineGraphicsResourceState Graphics(EngineGraphicsResourceState s,Field<object> manager,Field<EngineResourceNameInput> name,Field<WorldEngine> engineGlobal,EngineGraphicsResourceConstructionInvocation c){
   c.GraphicsEntered=true;c.Native.At=0x095eac;Resource(s,manager,name,0,c);
   Put(c,s.Virtual00,0x18033f670UL,0x095eba,0,8);Empty(s.Name88,0x88,0x095ebd,0x095ec8,0x095ecf,c);
   Put(c,s.QwordB0,0UL,0x095ed5,0xb0,8);Put(c,s.QwordB8,0UL,0x095edc,0xb8,8);Put(c,s.QwordC0,0UL,0x095ee3,0xc0,8);
   Put(c,s.QwordD0,0UL,0x095eea,0xd0,8);Put(c,s.QwordD8,0UL,0x095ef1,0xd8,8);Put(c,s.QwordE0,0UL,0x095ef8,0xe0,8);
   Put(c,s.QwordE8,0UL,0x095eff,0xe8,8);Put(c,s.QwordF0,0UL,0x095f06,0xf0,8);Put(c,s.QwordF8,0UL,0x095f0d,0xf8,8);
   Put(c,s.Qword100,0UL,0x095f14,0x100,8);Put(c,s.Qword108,0UL,0x095f1b,0x108,8);Put(c,s.Qword110,0UL,0x095f22,0x110,8);
   Empty(s.Name118,0x118,0x095f29,0x095f34,0x095f3b,c);Empty(s.Name138,0x138,0x095f41,0x095f4c,0x095f53,c);
   Empty(s.Name158,0x158,0x095f59,0x095f64,0x095f6b,c);Empty(s.Name178,0x178,0x095f71,0x095f7c,0x095f83,c);
   c.Native.At=0x095f89;c.CapturedEngine.CopyFrom(engineGlobal.Snapshot());c.Native.At=0x095f90;
   var engine=c.CapturedEngine.Read(c.Native.At);if(engine==null)throw new MissingContract(c.Native.At,"Null gEngine at graphics-field dereference");
   c.CapturedGraphics.CopyFrom(EngineGraphicsResourceEngineFields.ForEngine(engine).Graphics1F8.Snapshot());
   Copy(c,s.GraphicsA8,c.CapturedGraphics,0x095f9a,0xa8);Put(c,s.QwordC8,0UL,0x095fa1,0xc8,8);Put(c,s.Dword80,0u,0x095fa8,0x80,4);Put(c,s.Byte198,(byte)0,0x095fae,0x198,1);
   c.GraphicsResult=s;c.GraphicsReturned=true;c.Native.At=0x095fb9;return s;
  }
  static void Admit(Field<object> manager,Field<EngineResourceNameInput> name,EngineGraphicsResourceConstructionInvocation c){if(manager==null||name==null||c==null)throw new ArgumentNullException("OWN input cells/invocation");c.Reset();}
  public static EngineGraphicsResourceState ConstructResource199970(EngineGraphicsResourceState state,Field<object> manager,Field<EngineResourceNameInput> name,uint type,EngineGraphicsResourceConstructionInvocation c){
   Admit(manager,name,c);c.Target=state;try{c.Result=Resource(state,manager,name,type,c);c.Returned=true;return c.Result;}catch(MissingContract e){c.Native.At=e.Address;throw;}catch(OutOfMemoryException e){throw new MissingContract(c.Native.At,"OWN allocation failed; native allocator/EH is unmodeled",e);}
  }
  public static EngineGraphicsResourceState ConstructGraphics095EA0(EngineGraphicsResourceState state,Field<object> manager,Field<EngineResourceNameInput> name,Field<WorldEngine> engineGlobal,EngineGraphicsResourceConstructionInvocation c){
   if(engineGlobal==null)throw new ArgumentNullException("engineGlobal");Admit(manager,name,c);c.Target=state;try{c.Result=Graphics(state,manager,name,engineGlobal,c);c.Returned=true;return c.Result;}catch(MissingContract e){c.Native.At=e.Address;throw;}catch(OutOfMemoryException e){throw new MissingContract(c.Native.At,"OWN allocation failed; native allocator/EH is unmodeled",e);}
  }
  public static EngineGraphicsResourceState Create094650(Field<object> manager,Field<EngineResourceNameInput> name,Field<WorldEngine> engineGlobal,EngineGraphicsResourceConstructionInvocation c){
   if(engineGlobal==null)throw new ArgumentNullException("engineGlobal");Admit(manager,name,c);
   try{c.Native.At=0x09466f;c.NativeRequestedAllocationBytes=0x1a0;var s=new EngineGraphicsResourceState();c.Target=c.AllocatedResource=s;c.Native.At=0x094682;Graphics(s,manager,name,engineGlobal,c);c.Result=s;c.Returned=true;c.Native.At=0x094692;return s;}
   catch(MissingContract e){c.Native.At=e.Address;throw;}catch(OutOfMemoryException e){throw new MissingContract(c.Native.At,"OWN allocation failed; native allocator/EH is unmodeled",e);}
  }
 }
}
