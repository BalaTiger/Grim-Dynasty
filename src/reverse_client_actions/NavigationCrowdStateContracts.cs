using System;
using System.Threading;
namespace GrimDynasty.ReverseClient.Actions {
 // Declared stride8 OWN address space. A null native pointer is represented by
 // a known-null Field, never by a zero-capacity array's non-null address.
 public sealed class NavigationCrowdPointerBuffer<T>:NavigationView where T:class {
  public readonly Field<T>[] Cells;
  public NavigationCrowdPointerBuffer(int capacity){if(capacity<0)throw new ArgumentOutOfRangeException("capacity");Cells=new Field<T>[capacity];for(int n=0;n<capacity;n++)Cells[n]=new Field<T>();}
  public Field<T> At(int index,uint at){RequireLive(at);if(index<0||index>=Cells.Length)throw new MissingContract(at,"Outside own stride8 storage extent");return Cells[index];}
 }
 public sealed class NavigationCrowdPointerAddress<T> where T:class {
  public readonly NavigationCrowdPointerBuffer<T> Buffer;public readonly int Index;
  public NavigationCrowdPointerAddress(NavigationCrowdPointerBuffer<T> buffer,int index){if(buffer==null)throw new ArgumentNullException("buffer");if(index<0||index>buffer.Cells.Length)throw new ArgumentOutOfRangeException("index");Buffer=buffer;Index=index;}
 }
 public sealed class NavigationCrowdPointerVector<T>:NavigationView where T:class {
  public readonly Field<NavigationCrowdPointerAddress<T>> Begin00=new Field<NavigationCrowdPointerAddress<T>>(),End08=new Field<NavigationCrowdPointerAddress<T>>(),Capacity10=new Field<NavigationCrowdPointerAddress<T>>();
  NavigationCrowdPointerBuffer<T> lastGrowthAllocation;
  // Diagnostic fresh local from the actual growth call, including a later
  // unsupported copy/free prefix. Not a native field or an allocator callback.
  public NavigationCrowdPointerBuffer<T> LastGrowthAllocation{get{return lastGrowthAllocation;}}
  internal void GrowthAllocated(NavigationCrowdPointerBuffer<T> buffer){lastGrowthAllocation=buffer;}
  public void InstallOwnStorage(NavigationCrowdPointerBuffer<T> storage,int count){if(storage==null)throw new ArgumentNullException("storage");if(count<0||count>storage.Cells.Length)throw new ArgumentOutOfRangeException("count");Begin00.Write(new NavigationCrowdPointerAddress<T>(storage,0));End08.Write(new NavigationCrowdPointerAddress<T>(storage,count));Capacity10.Write(new NavigationCrowdPointerAddress<T>(storage,storage.Cells.Length));}
 }
 // Only pointer copies/reset are currently consumed for these vectors. Their
 // element format, allocation, neighbors and population are NOT defaulted.
 public sealed class NavigationCrowdOpaqueVector {
  public readonly Field<object> Begin00=new Field<object>(),End08=new Field<object>(),Capacity10=new Field<object>();
 }
 public sealed class NavigationCrowdRequest:NavigationView {
  public readonly Field<uint> Generation00=new Field<uint>();
  public readonly NavigationPoint3 From04=new NavigationPoint3(),To10=new NavigationPoint3();
  public readonly Field<uint> Gap1C=new Field<uint>();
  public readonly NavigationCrowdRefVector RefVector20;
  public readonly NavigationCrowdOpaqueVector WaypointVector38=new NavigationCrowdOpaqueVector();
  public readonly Field<byte> Complete50=new Field<byte>();
  public NavigationCrowdRequest(){
   // Reuse the frozen typed vector's fields, not its constructor's own storage
   // assumptions. This view has UNKNOWN pointers until observed writes/setup.
   RefVector20=new NavigationCrowdRefVector(0,0);RefVector20.Begin00.Forget();RefVector20.End08.Forget();RefVector20.Capacity10.Forget();
  }
  public void InstallOwnRefStorage(NavigationRefBuffer storage,int count){
   if(storage==null)throw new ArgumentNullException("storage");if(count<0||count>storage.Cells.Length)throw new ArgumentOutOfRangeException("count");
   RefVector20.Begin00.Write(new NavigationCrowdRefAddress(storage,0));RefVector20.End08.Write(new NavigationCrowdRefAddress(storage,count));RefVector20.Capacity10.Write(new NavigationCrowdRefAddress(storage,storage.Cells.Length));
  }
 }
 public sealed class NavigationCrowdAgent:NavigationView {
  public readonly Field<uint> Index00=new Field<uint>(),Gap04=new Field<uint>();
  public readonly Field<object> Callback08=new Field<object>();
  public readonly Field<float> Radius10=new Field<float>(),Height14=new Field<float>(),Parameter18=new Field<float>();
  public readonly Field<uint>[] ParameterBits1C40=new Field<uint>[10];
  public readonly Field<uint> Flags44=new Field<uint>();
  public readonly NavigationPoint3 Position48=new NavigationPoint3(),Heading54=new NavigationPoint3(),Vector60=new NavigationPoint3();
  public readonly Field<uint> Timer6C=new Field<uint>(),State70=new Field<uint>(),Reason74=new Field<uint>();
  public readonly NavigationPoint3 Target78=new NavigationPoint3();
  public readonly Field<uint> Gap84=new Field<uint>();
  public readonly Field<ulong>[] Words88B0=new Field<ulong>[6];
  public readonly NavigationPoint3 VelocityB8=new NavigationPoint3(),PreviousC4=new NavigationPoint3();
  public readonly Field<uint> GapD0=new Field<uint>(),GapD4=new Field<uint>(),GapD8=new Field<uint>(),GapE4=new Field<uint>();
  public readonly Field<ulong> WordDC=new Field<ulong>();
  public readonly NavigationCrowdRequest RequestE8=new NavigationCrowdRequest();
  public readonly Field<byte>[] Gap13913F=new Field<byte>[7],Gap141147=new Field<byte>[7];
  public readonly Field<byte> NeedReplan140=new Field<byte>();
  public NavigationCrowdAgent(){for(int n=0;n<ParameterBits1C40.Length;n++)ParameterBits1C40[n]=new Field<uint>();for(int n=0;n<Words88B0.Length;n++)Words88B0[n]=new Field<ulong>();for(int n=0;n<7;n++){Gap13913F[n]=new Field<byte>();Gap141147[n]=new Field<byte>();}}
 }
 public sealed class NavigationCrowdInt3 {
  public readonly Field<int> X=new Field<int>(),Y=new Field<int>(),Z=new Field<int>();
 }
 public sealed class NavigationCrowdManager:NavigationView {
  public readonly NavigationCrowdInt3 Margin00=new NavigationCrowdInt3();
  public readonly Field<uint> MaxGroups0C=new Field<uint>();
  public readonly NavigationCrowdPointerVector<NavigationCrowdGroup> Groups10=new NavigationCrowdPointerVector<NavigationCrowdGroup>();
  public readonly Field<NavigationCrowdCriticalSection> CriticalA0=new Field<NavigationCrowdCriticalSection>();
  public readonly Field<NavigationCrowdAdapterState> QueryB8=new Field<NavigationCrowdAdapterState>();
  int held;
  public int OwnOutstandingEnters{get{return Interlocked.CompareExchange(ref held,0,0);}}
  internal void Acquired(){Interlocked.Increment(ref held);}internal void Released(){Interlocked.Decrement(ref held);}
 }
 public sealed class NavigationCrowdGroup:NavigationView {
  public readonly Field<NavigationCrowdManager> Owner00=new Field<NavigationCrowdManager>();
  public readonly Field<NavigationCrowdAdapterState> Query08=new Field<NavigationCrowdAdapterState>();
  public readonly NavigationCrowdInt3 Lower10=new NavigationCrowdInt3(),Upper1C=new NavigationCrowdInt3();
  public readonly NavigationCrowdPointerVector<NavigationCrowdAgent> Members28=new NavigationCrowdPointerVector<NavigationCrowdAgent>();
  public readonly NavigationCrowdOpaqueVector Work40=new NavigationCrowdOpaqueVector();
  public readonly Field<object> Pointer58=new Field<object>(),Pointer60=new Field<object>(),Pointer70=new Field<object>();
  public readonly Field<uint> Count68=new Field<uint>(),Gap6C=new Field<uint>(),Count78=new Field<uint>(),Gap7C=new Field<uint>();
  public readonly NavigationCrowdOpaqueVector Vector80=new NavigationCrowdOpaqueVector();
 }
 public sealed class NavigationCrowdStateBoundaries {
  public readonly NavigationCrowdAdapterBoundaries Adapter;
  // Mutable loaded globals remain unknown. Opaque constructor copies propagate
  // this knowledge; no zero vector is silently supplied.
  public readonly NavigationPoint3 Global422980=new NavigationPoint3();
  public readonly Field<byte> DisableAgentAssert422B9F=new Field<byte>(),DisableGroupAssert422B90=new Field<byte>(),DisableAllAssert42144A=new Field<byte>();
  public readonly Field<float> Half34BADC=new Field<float>();
  NavigationCrowdGroup lastGroup;
  // Diagnostic actual local allocation only; not a native published field.
  public NavigationCrowdGroup LastGroupAllocation{get{return lastGroup;}}
  public NavigationCrowdStateBoundaries(NavigationCrowdAdapterBoundaries adapter){Adapter=adapter;}
  // Exact disk observation r1-navigation-compressor-slot/inspection.json,
  // SHA191e63e5cc34d9b1abf1326426827a27041743913768a3159e73af3dc62126de.
  // This explicit setup does not assert that a loaded operand was observed.
  public void UseObservedGroupConstants(){Half34BADC.Write(BitConverter.ToSingle(BitConverter.GetBytes(0x3f000000u),0));}
  internal void Allocated(NavigationCrowdGroup group){lastGroup=group;}
 }
 // Evidence: r1-crowd-agent-group-registration25E6C0/25A250/25C340,
 // r1-crowd-query-production25A4F0, r1-crowd-movement-direct25C78D.
 //02D950 is the saved same-Engine dumpbin fragment offline-archive-init,
 // instructions SHA44cad43b788a8b1af493049e2e9b80f0f4fb11602fb819365095c5d9e2c96544.
 // Supported: explicit shared typed identities, stable single-thread lifetimes,
 // normal own managed allocations and disjoint declared stack/heap addresses.
 // No native heap/OOM, arbitrary byte aliases, native locks/EH or callbacks are
 // emulated. These fragments do NOT perform a full Crowd update or movement.
 public static class NavigationCrowdStateContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null own crowd-state view");var v=value as NavigationView;if(v!=null)v.RequireLive(at);return value;}
  static Field<float> Cell(NavigationPoint3 point,int axis,uint at){Need(point,at);try{return axis==0?point.X:axis==1?point.Y:point.Z;}catch(MissingContract e){throw new MissingContract(at,e.Message,e);}}
  static void CopyXY(NavigationPoint3 to,NavigationPoint3 from,uint load,uint store){var x=Cell(from,0,load).Snapshot();var y=Cell(from,1,load).Snapshot();Cell(to,0,store).CopyFrom(x);Cell(to,1,store).CopyFrom(y);}
  static void CopyZ(NavigationPoint3 to,NavigationPoint3 from,uint load,uint store){var z=Cell(from,2,load).Snapshot();Cell(to,2,store).CopyFrom(z);}
  static void NullVector<T>(NavigationCrowdPointerVector<T> v)where T:class{v.Begin00.Write(null);v.End08.Write(null);v.Capacity10.Write(null);}
  static void NullOpaque(NavigationCrowdOpaqueVector v){v.Begin00.Write(null);v.End08.Write(null);v.Capacity10.Write(null);}
  public static NavigationCrowdAgent InitializeAgent25E6C0(NavigationCrowdAgent agent,Field<object> callback,NavigationCrowdStateBoundaries b,Invocation invocation){
   var i=Need(invocation,0x25e6e7);i.At=0x25e6e7;Need(agent,i.At).Index00.Write(0xffffffffu);
   agent.Callback08.CopyFrom(Need(callback,0x25e6ed));agent.Radius10.Write(1);agent.Height14.Write(1);agent.Parameter18.Write(1);
   for(int n=0;n<10;n++)agent.ParameterBits1C40[n].Write(0);agent.Flags44.Write(0);
   var global=Need(b,0x25e71f).Global422980;
   CopyXY(agent.Position48,global,0x25e71f,0x25e727);CopyZ(agent.Position48,global,0x25e72c,0x25e732);
   CopyXY(agent.Heading54,global,0x25e735,0x25e73d);CopyZ(agent.Heading54,global,0x25e742,0x25e748);
   CopyXY(agent.Vector60,global,0x25e74b,0x25e753);CopyZ(agent.Vector60,global,0x25e758,0x25e75e);
   agent.Timer6C.Write(0);agent.State70.Write(0);agent.Reason74.Write(0);
   CopyXY(agent.Target78,global,0x25e768,0x25e770);CopyZ(agent.Target78,global,0x25e775,0x25e77b);
   for(int n=0;n<6;n++)agent.Words88B0[n].Write(0);
   CopyXY(agent.VelocityB8,global,0x25e7ab,0x25e7b3);CopyZ(agent.VelocityB8,global,0x25e7bb,0x25e7c1);
   agent.WordDC.Write(0);agent.RequestE8.Generation00.Write(0);
   agent.RequestE8.RefVector20.Begin00.Write(null);agent.RequestE8.RefVector20.End08.Write(null);agent.RequestE8.RefVector20.Capacity10.Write(null);
   NullOpaque(agent.RequestE8.WaypointVector38);agent.RequestE8.Complete50.Write(0);agent.NeedReplan140.Write(0);
   // The assertion flag is read BEFORE comparing the original argument pointer.
   i.At=0x25e80a;if(b.DisableAgentAssert422B9F.Read(i.At)==0){i.At=0x25e816;if(callback.Read(i.At)==null){i.At=0x25e81f;if(b.DisableAllAssert42144A.Read(i.At)==0){i.At=0x25e82e;throw new MissingContract(i.At,"Native null-callback diagnostics not implemented");}}}
   i.At=0x25e8d7;return agent;
  }
  static bool Same<T>(NavigationCrowdPointerAddress<T> a,NavigationCrowdPointerAddress<T> b)where T:class{return a==null?b==null:b!=null&&Object.ReferenceEquals(a.Buffer,b.Buffer)&&a.Index==b.Index;}
  static long Difference<T>(NavigationCrowdPointerAddress<T> end,NavigationCrowdPointerAddress<T> begin,uint at)where T:class{if(end==null&&begin==null)return 0;if(end==null||begin==null||!Object.ReferenceEquals(end.Buffer,begin.Buffer))throw new MissingContract(at,"Own pointer difference needs the same declared allocation or two null pointers");return (long)end.Index-begin.Index;}
  static NavigationCrowdPointerAddress<T> Advance<T>(NavigationCrowdPointerAddress<T> address,uint at)where T:class{if(address==null||address.Index>=address.Buffer.Cells.Length)throw new MissingContract(at,"Pointer increment leaves declared own stride8 address domain");return new NavigationCrowdPointerAddress<T>(address.Buffer,address.Index+1);}
  // Shared header core preserves these public typed entry points. The same
  // algorithm now operates directly on Work40 and Agent+88 borrowed cells.
  public static void ReserveAdditional034EC0<T>(NavigationCrowdPointerVector<T> vector,ulong additional,Invocation invocation)where T:class{
   NavigationCrowdBorrowedPointerContracts.ReserveAdditional034EC0(NavigationCrowdPointerHeader<T>.BorrowTyped(vector),additional,invocation);
  }
  public static void Reallocate034FE0<T>(NavigationCrowdPointerVector<T> vector,ulong capacity,Invocation invocation)where T:class{
   NavigationCrowdBorrowedPointerContracts.Reallocate034FE0(NavigationCrowdPointerHeader<T>.BorrowTyped(vector),capacity,invocation);
  }
  public static NavigationCrowdGroup GetGroup25A250(NavigationCrowdManager manager,NavigationCrowdInt3 lower,NavigationCrowdInt3 upper,NavigationCrowdStateBoundaries b,Invocation invocation){
   var i=Need(invocation,0x25a260);var groups=Need(manager,0x25a260).Groups10;var endBits=groups.End08.Snapshot();
   i.At=0x25a274;var end=endBits.Read(i.At);var begin=groups.Begin00.Read(i.At);
   if(!Same(begin,end)){i.At=0x25a286;var p=Need(groups.Begin00.Read(i.At),i.At);var first=p.Buffer.At(p.Index,i.At).Read(0x25a289);if(first!=null){i.At=0x25a4d4;return first;}}
   i.At=0x25a292;if(Need(b,i.At).DisableGroupAssert422B90.Read(i.At)==0){
    var liveEnd=groups.End08.Snapshot();i.At=0x25a2ab;long count=Difference(liveEnd.Read(i.At),groups.Begin00.Read(i.At),i.At);uint max=manager.MaxGroups0C.Read(0x25a2ae);
    if(unchecked((ulong)count)>=max){i.At=0x25a2be;if(b.DisableAllAssert42144A.Read(i.At)==0){i.At=0x25a2de;throw new MissingContract(i.At,"Native group limit diagnostics not implemented");}}
   }
   // Explicit normal managed allocation domain; not native allocator recovery.
   i.At=0x25a39c;var group=new NavigationCrowdGroup();b.Allocated(group);
   i.At=0x25a3ac;int lx=Need(lower,i.At).X.Read(i.At);lx=unchecked(lx-manager.Margin00.X.Read(0x25a3af));
   int ux=Need(upper,0x25a3b1).X.Read(0x25a3b1);ux=unchecked(ux+manager.Margin00.X.Read(0x25a3b4));
   int uz=upper.Z.Read(0x25a3b6),lz=lower.Z.Read(0x25a3ba);uz=unchecked(uz+manager.Margin00.Z.Read(0x25a3be));lz=unchecked(lz-manager.Margin00.Z.Read(0x25a3c2));
   int ly=lower.Y.Read(0x25a3c9);ly=unchecked(ly-manager.Margin00.Y.Read(0x25a3cd));int uy=upper.Y.Read(0x25a3d4);uy=unchecked(uy+manager.Margin00.Y.Read(0x25a3d8));
   var query=manager.QueryB8.Snapshot();group.Lower10.X.Write(lx);group.Lower10.Y.Write(ly);group.Query08.CopyFrom(query);group.Owner00.Write(manager);group.Lower10.Z.Write(lz);
   group.Upper1C.X.Write(ux);group.Upper1C.Y.Write(uy);group.Upper1C.Z.Write(uz);
   NullVector(group.Members28);NullOpaque(group.Work40);group.Pointer58.Write(null);group.Pointer60.Write(null);group.Count68.Write(0);group.Pointer70.Write(null);group.Count78.Write(0);NullOpaque(group.Vector80);
   // The actual local group pointer is disjoint from the declared heap vector;
   // only the non-alias inline append branch25A4AA is in this OWN address domain.
   var oldEnd=groups.End08.Snapshot();i.At=0x25a463;end=oldEnd.Read(i.At);i.At=0x25a4aa;var capacity=groups.Capacity10.Read(i.At);
   if(Same(end,capacity)){i.At=0x25a4b3;ReserveGroupSlot058190(groups,i);}
   i.At=0x25a4bc;end=groups.End08.Read(i.At);if(end!=null){i.At=0x25a4c1;end.Buffer.At(end.Index,i.At).Write(group);}
   i.At=0x25a4c4;groups.End08.Write(Advance(groups.End08.Read(i.At),i.At));i.At=0x25a4c9;return group;
  }
  //058190 is the separately evidenced reserve-one wrapper. Its arithmetic
  // feeds the shared034FE0 copy/release/publication path below.
  static void ReserveGroupSlot058190(NavigationCrowdPointerVector<NavigationCrowdGroup> vector,Invocation i){
   var capBits=Need(vector,0x058194).Capacity10.Snapshot();var endBits=vector.End08.Snapshot();
   i.At=0x0581a2;var cap=capBits.Read(i.At);var end=endBits.Read(i.At);ulong available=unchecked((ulong)Difference(cap,end,i.At));if(available>=1)return;
   var beginBits=vector.Begin00.Snapshot();i.At=0x0581bc;var begin=beginBits.Read(i.At);ulong count=unchecked((ulong)Difference(end,begin,i.At));
   const ulong maximum=0x1ffffffffffffffful;if(unchecked(maximum-count)<1){i.At=0x0581d6;throw new MissingContract(i.At,"Native vector length_error outside own normal allocation domain");}
   i.At=0x0581dd;ulong capacity=unchecked((ulong)Difference(cap,begin,i.At));ulong requested=unchecked(count+1),half=capacity>>1;
   ulong candidate=unchecked(maximum-half)>=capacity?unchecked(capacity+half):0;if(candidate>=requested)requested=candidate;
   i.At=0x05820a;Reallocate034FE0(vector,requested,i);
  }
  //034CF0 is called with25C340's fresh local pointer: this local cannot alias
  // the declared managed heap buffer. The native alias-input arm is not needed
  // for this private caller-specific path, and no generic append is claimed.
  static void AppendAgentLocal034CF0(NavigationCrowdPointerVector<NavigationCrowdAgent> vector,Field<NavigationCrowdAgent> value,Invocation i){
   var endBits=Need(vector,0x034cfa).End08.Snapshot();i.At=0x034d04;var end=endBits.Read(i.At);i.At=0x034d4e;var capacity=vector.Capacity10.Read(i.At);
   if(Same(end,capacity)){i.At=0x034d5c;ReserveAdditional034EC0(vector,1,i);}
   i.At=0x034d65;end=vector.End08.Read(i.At);if(end!=null){var pointer=value.Snapshot();i.At=0x034d6d;end.Buffer.At(end.Index,i.At).CopyFrom(pointer);}
   i.At=0x034d70;vector.End08.Write(Advance(vector.End08.Read(i.At),i.At));
  }
  static float Scalar(float value,uint at){
   // Ordinary finite, non-subnormal operands only. Native MXCSR/DAZ/FTZ and
   // masked invalid CVTTSS2SI results are not inferred from managed casts.
   if(Single.IsNaN(value)||Single.IsInfinity(value)||(value!=0&&Math.Abs(value)<1.1754943508222875E-38f))throw new MissingContract(at,"Own crowd bounds FP domain excludes nonfinite/subnormal values and unobserved MXCSR modes");return value;
  }
  static float FP(float value,uint at){return Scalar(BitConverter.ToSingle(BitConverter.GetBytes(value),0),at);}
  static int Cvt(float value,uint at){value=Scalar(value,at);if((double)value< -2147483648.0||(double)value>=2147483648.0)throw new MissingContract(at,"CVTTSS2SI invalid/overflow result depends on unobserved native FP control state");return (int)value;}
  public static void Join25C340(NavigationCrowdGroup group,NavigationCrowdAgent agent,NavigationCrowdStateBoundaries b,Invocation invocation){
   var i=Need(invocation,0x25c34a);var members=Need(group,0x25c34a).Members28;var end=members.End08.Snapshot();i.At=0x25c351;long count=Difference(end.Read(i.At),members.Begin00.Read(i.At),i.At);
   i.At=0x25c35d;Need(agent,i.At).Index00.Write(unchecked((uint)count));i.At=0x25c364;AppendAgentLocal034CF0(members,Field<NavigationCrowdAgent>.Of(agent),i);
   // MOVSS copies bits; first arithmetic/convert consumes the corresponding
   // value. In particular unknown Y must not preempt the earlier X-radius.
   var radiusBits=agent.Radius10.Snapshot();var xBits=Cell(agent.Position48,0,0x25c373).Snapshot();var zBits=Cell(agent.Position48,2,0x25c378).Snapshot();var yBits=Cell(agent.Position48,1,0x25c380).Snapshot();
   i.At=0x25c385;float x=Scalar(xBits.Read(i.At),i.At),radius=Scalar(radiusBits.Read(i.At),i.At);float lowX=FP(x-radius,i.At);
   i.At=0x25c389;float y=Scalar(yBits.Read(i.At),i.At);int iy=Cvt(y,i.At);
   i.At=0x25c38d;float topY=FP(y+Scalar(agent.Height14.Read(i.At),i.At),i.At);int ix=Cvt(lowX,0x25c392);
   float highX=FP(x+radius,0x25c399);i.At=0x25c39d;float z=Scalar(zBits.Read(i.At),i.At);float lowZ=FP(z-radius,i.At);float highZ=FP(z+radius,0x25c3a1);int iz=Cvt(lowZ,0x25c3a5);
   var halfBits=Need(b,0x25c3aa).Half34BADC.Snapshot();i.At=0x25c3b2;int oldZ=group.Lower10.Z.Read(i.At);
   i.At=0x25c3b6;float half=Scalar(halfBits.Read(i.At),i.At);highZ=FP(highZ+half,i.At);topY=FP(topY+half,0x25c3ba);
   // Memory CMOV sources are fetched regardless of whether selected.
   int source=group.Lower10.Z.Read(0x25c3be);if(oldZ<iz)iz=source;highX=FP(highX+half,0x25c3c3);
   int oldY=group.Lower10.Y.Read(0x25c3c7);source=group.Lower10.Y.Read(0x25c3ca);if(oldY<iy)iy=source;
   int oldX=group.Lower10.X.Read(0x25c3ce);source=group.Lower10.X.Read(0x25c3d5);if(oldX<ix)ix=source;
   i.At=0x25c3e3;group.Lower10.X.Write(ix);group.Lower10.Y.Write(iy);i.At=0x25c3e8;group.Lower10.Z.Write(iz);
   int uz=Cvt(highZ,0x25c3ec),uy=Cvt(topY,0x25c3f1);int oldUZ=group.Upper1C.Z.Read(0x25c3f6);source=group.Upper1C.Z.Read(0x25c3fa);if(oldUZ>uz)uz=source;
   int ux=Cvt(highX,0x25c3ff);int oldUY=group.Upper1C.Y.Read(0x25c404);source=group.Upper1C.Y.Read(0x25c408);if(oldUY>uy)uy=source;
   int oldUX=group.Upper1C.X.Read(0x25c412);source=group.Upper1C.X.Read(0x25c416);if(oldUX>ux)ux=source;
   i.At=0x25c426;group.Upper1C.X.Write(ux);group.Upper1C.Y.Write(uy);i.At=0x25c42b;group.Upper1C.Z.Write(uz);
  }
  sealed class HeldLock {internal NavigationCrowdCriticalSection Value;internal NavigationCrowdManager Manager;internal bool Entered;}
  static HeldLock Enter(NavigationCrowdManager manager,Field<NavigationCrowdCriticalSection> bits,uint at){var value=Need(bits.Read(at),at);Monitor.Enter(value.Identity);manager.Acquired();return new HeldLock{Value=value,Manager=manager,Entered=true};}
  static void Leave(HeldLock held,Field<NavigationCrowdCriticalSection> bits,uint at){var current=Need(bits.Read(at),at);if(!Object.ReferenceEquals(current.Identity,held.Value.Identity))throw new MissingContract(at,"Manager leave changed own lock identity");Monitor.Exit(held.Value.Identity);held.Manager.Released();held.Entered=false;}
  static void Cleanup(HeldLock held){if(held!=null&&held.Entered){Monitor.Exit(held.Value.Identity);held.Manager.Released();held.Entered=false;}}
  static bool SameRef(NavigationCrowdRefAddress a,NavigationCrowdRefAddress b){return a==null?b==null:b!=null&&Object.ReferenceEquals(a.Buffer,b.Buffer)&&a.Index==b.Index;}
  //02D950's input here is its caller's fresh localDWORD, disjoint from the own
  // heap buffer. Alias-input branch is outside this private adapter's domain.
  static void AppendLocal02D950(NavigationCrowdRefVector vector,Field<uint> value,Invocation i){
   var oldEnd=Need(vector,0x02d95a).End08.Snapshot();i.At=0x02d964;var end=oldEnd.Read(i.At);i.At=0x02d9ac;var cap=vector.Capacity10.Read(i.At);
   if(SameRef(end,cap)){i.At=0x02d9ba;throw new MissingContract(i.At,"02DE40 vector reallocation outside explicit preallocated request domain");}
   i.At=0x02d9c3;end=vector.End08.Read(i.At);if(end!=null){var bits=value.Snapshot();i.At=0x02d9ca;Need(end.Buffer,i.At).At(end.Index,i.At).CopyFrom(bits);}
   i.At=0x02d9cc;end=vector.End08.Read(i.At);if(end==null||end.Index>=end.Buffer.Cells.Length)throw new MissingContract(i.At,"Ref end increment outside declared own stride4 address domain");vector.End08.Write(new NavigationCrowdRefAddress(end.Buffer,end.Index+1));
  }
  public static void Submit25A4F0(NavigationCrowdManager manager,NavigationCrowdRequest request,uint generation,byte mode,NavigationCrowdStateBoundaries b,Invocation invocation){
   var i=Need(invocation,0x25a50b);var lockBits=Need(manager,0x25a50b).CriticalA0.Snapshot();i.At=0x25a518;var held=Enter(manager,lockBits,i.At);
   try{
    i.At=0x25a51e;if(Need(request,i.At).Generation00.Read(i.At)==generation){
     var queryBits=manager.QueryB8.Snapshot();i.At=0x25a529;var query=Need(queryBits.Read(i.At),i.At);
     if(mode==0){i.At=0x25a53d;byte result=NavigationCrowdAdapterContracts.FindCorridor030790(query,request.From04,request.To10,request.RefVector20,Need(b,i.At).Adapter,i);i.At=0x25a53f;request.Complete50.Write(result);}
     else{i.At=0x25a54b;uint reference=NavigationCrowdAdapterContracts.Project030B30(query,request.To10,null,Need(b,i.At).Adapter,i);if(reference!=0){i.At=0x25a55f;AppendLocal02D950(request.RefVector20,Field<uint>.Of(reference),i);i.At=0x25a564;request.Complete50.Write(1);}else{i.At=0x25a56a;request.Complete50.Write(0);}}
    }
    var leave=manager.CriticalA0.Snapshot();i.At=0x25a589;Leave(held,leave,i.At);
   }finally{Cleanup(held);} // OWN exception cleanup, not native EH recovery.
  }
  // Explicit ENTRY FRAGMENT25C78D..25C7DB only. Caller is responsible for the
  // earlier real state/flag branch. Return means request submission, not motion;
  // the subsequent completion/failure callback and full25C440 remain unmodeled.
  public static void SubmitSynchronousFragment25C78D(NavigationCrowdGroup group,NavigationCrowdAgent agent,byte mode,NavigationCrowdStateBoundaries b,Invocation invocation){
   var i=Need(invocation,0x25c78d);Need(agent,0x25c78d);var x=Cell(agent.Position48,0,0x25c78d).Snapshot();var y=Cell(agent.Position48,1,0x25c78d).Snapshot();
   var request=agent.RequestE8;var owner=Need(group,0x25c799).Owner00.Snapshot();Cell(request.From04,0,0x25c7a1).CopyFrom(x);Cell(request.From04,1,0x25c7a1).CopyFrom(y);CopyZ(request.From04,agent.Position48,0x25c7a9,0x25c7ac);
   CopyXY(request.To10,agent.Target78,0x25c7af,0x25c7b4);CopyZ(request.To10,agent.Target78,0x25c7b9,0x25c7bf);
   request.WaypointVector38.End08.CopyFrom(request.WaypointVector38.Begin00.Snapshot());request.RefVector20.End08.CopyFrom(request.RefVector20.Begin00.Snapshot());
   i.At=0x25c7d2;request.Generation00.Write(unchecked(request.Generation00.Read(i.At)+1u));uint generation=request.Generation00.Read(0x25c7d4);request.Complete50.Write(0);
   // The frozen manager type cannot carry opaque unknown pointer bits through
   // a call; this is an explicit OWN dispatch stop at the actual call boundary.
   i.At=0x25c7db;var manager=owner.Read(i.At);Submit25A4F0(manager,request,generation,mode,b,i);i.At=0x25c7e0;
  }
 }
}
