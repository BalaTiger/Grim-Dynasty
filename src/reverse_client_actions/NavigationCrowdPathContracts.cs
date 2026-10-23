using System;
using System.Threading;
namespace GrimDynasty.ReverseClient.Actions {
 // A stride12 address into the existing scalar-cell buffer. A deferred address
 // is an opaque MOV/LEA result: forming it does not inspect the pointed memory.
 public sealed class NavigationCrowdPointAddress {
  readonly NavigationPointBuffer buffer;readonly long index;
  readonly Field<object> deferred;readonly long delta;
  public NavigationCrowdPointAddress(NavigationPointBuffer buffer,long pointIndex){if(buffer==null)throw new ArgumentNullException("buffer");this.buffer=buffer;index=pointIndex;}
  internal NavigationCrowdPointAddress(Field<object> bits,long offset){deferred=bits;delta=offset;}
  internal NavigationCrowdPointAddress Resolve(uint at){if(deferred==null)return this;object raw=deferred.Read(at);var p=raw as NavigationCrowdPointAddress;if(p==null)throw new MissingContract(at,"Unknown/null/non-stride12 borrowed address outside own memory domain");p=p.Resolve(at);return new NavigationCrowdPointAddress(p.buffer,unchecked(p.index+delta));}
  public NavigationPointBuffer Buffer{get{return Resolve(0).buffer;}}
  public long PointIndex{get{return Resolve(0).index;}}
  public Field<float> Cell(int axis,uint at){if(axis<0||axis>2)throw new ArgumentOutOfRangeException("axis");var p=Resolve(at);if(p.index<0||p.index>Int32.MaxValue/3)throw new MissingContract(at,"Outside own stride12 address extent");return p.buffer.At(p.index*3+axis,at);}
  public NavigationPoint3 AsPoint(uint at){var p=Resolve(at);if(p.index<Int32.MinValue/3||p.index>Int32.MaxValue/3)throw new MissingContract(at,"Own deferred point offset exceeds Int32 scalar domain");return p.buffer.PointAtScalar((int)(p.index*3),at);}
 }
 public sealed class NavigationCrowdWaypointVector {
  public readonly NavigationCrowdOpaqueVector Header;
  public readonly Field<object> Begin00,End08,Capacity10;
  NavigationPointBuffer lastGrowthAllocation;
  // Actual fresh local from the most recent OWN allocation, including a later
  // unsupported copy/free prefix. This diagnostic is not a native header field.
  public NavigationPointBuffer LastGrowthAllocation{get{return lastGrowthAllocation;}}
  internal void Allocated(NavigationPointBuffer buffer){lastGrowthAllocation=buffer;}
  public NavigationCrowdWaypointVector(NavigationCrowdOpaqueVector header){if(header==null)throw new ArgumentNullException("header");Header=header;Begin00=header.Begin00;End08=header.End08;Capacity10=header.Capacity10;}
  // Explicit OWN setup, not the native constructor's three null pointer writes.
  public void InstallOwnStorage(NavigationPointBuffer buffer,int count){if(buffer==null)throw new ArgumentNullException("buffer");if(buffer.Cells.Length%3!=0||count<0||count>buffer.Cells.Length/3)throw new ArgumentOutOfRangeException("count");Begin00.Write(new NavigationCrowdPointAddress(buffer,0));End08.Write(new NavigationCrowdPointAddress(buffer,count));Capacity10.Write(new NavigationCrowdPointAddress(buffer,buffer.Cells.Length/3));}
 }
 public sealed class NavigationCrowdPathBoundaries {
  public readonly NavigationStraightPathBoundaries Straight;
  public readonly Field<float> NearThreshold34B984=new Field<float>();
  public readonly Field<byte> DisableTrimAssert422B93=new Field<byte>(),DisableSecondAssert422B92=new Field<byte>(),DisablePopAssert422B94=new Field<byte>(),DisableAllAssert42144A=new Field<byte>();
  int outstanding;bool ownDiskNoOp00CE70;
  public NavigationCrowdPathBoundaries(NavigationMeshTilesState mesh){Straight=new NavigationStraightPathBoundaries(mesh);}
  public int OwnOutstandingEnters{get{return Interlocked.CompareExchange(ref outstanding,0,0);}}
  internal void Entered(){Interlocked.Increment(ref outstanding);}internal void Left(){Interlocked.Decrement(ref outstanding);}
  // Raw observation r1-crowd-motion-bindings; no claim about loaded globals.
  public void UseObservedPathConstants(){NearThreshold34B984.Write(NavigationCoreContracts.FromBits(0x38d1b717));}
  public void UseObservedStraightPathConstants(){Straight.UseObservedStraightPathConstants();}
  public void UseObservedEqualityConstant(){Straight.UseObservedEqualityConstant();}
  // An explicit OWN platform choice using the preserved disk leaf's RET. This
  // does not establish that a loaded original00CE70 remains unpatched/no-op.
  public void UseObservedDiskNoOp00CE70OwnAdapter(){ownDiskNoOp00CE70=true;}
  internal void RequireDiskNoOp00CE70(uint at){if(!ownDiskNoOp00CE70)throw new MissingContract(at,"Loaded00CE70 behavior is unobserved; explicit own disk-leaf no-op adapter not selected");}
 }
 // Engine030C60/030900/25BA90/25BE90/25C080/25BCF0, saved in
 // r1-crowd-motion-branches, Engine SHA4c2ea14c...8a93153. This is a shared
 // typed-address implementation, not a native DLL loader or movement callback.
 // Normal domain: stable single-thread allocations, aligned stride12/stride4
 // pointers, binary32 arithmetic with masked FP traps. Subnormals and arbitrary
 // byte aliases are outside this OWN domain. Memmove validates its entire own
 // range before copying opaque cells; native partial page faults are not modeled.
 // Global diagnostic flags remain unknown until explicitly supplied. Diagnostic
 // callbacks, native heap/EH and the loaded behavior of00CE70 remain unproved.
 public static class NavigationCrowdPathContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null own crowd path view");var v=value as NavigationView;if(v!=null)v.RequireLive(at);return value;}
  static Field<float> Cell(NavigationPoint3 point,int axis,uint at){Need(point,at);try{return axis==0?point.X:axis==1?point.Y:point.Z;}catch(MissingContract e){throw new MissingContract(at,e.Message,e);}}
  static NavigationCrowdWaypointVector Vector(NavigationCrowdRequest request,uint at){return new NavigationCrowdWaypointVector(Need(request,at).WaypointVector38);}
  static Field<object> Bits(NavigationCrowdWaypointVector v,int field,uint at){Need(v,at);return (field==0?v.Begin00:field==1?v.End08:v.Capacity10).Snapshot();}
  static NavigationCrowdPointAddress Address(Field<object> bits,uint at){object raw=bits.Read(at);if(raw==null)return null;var p=raw as NavigationCrowdPointAddress;if(p==null)throw new MissingContract(at,"Opaque header is not bound to own stride12 address");return p.Resolve(at);}
  static long Difference(Field<object> end,Field<object> begin,uint at){return Difference(Address(end,at),Address(begin,at),at);}
  static long Difference(NavigationCrowdPointAddress end,NavigationCrowdPointAddress begin,uint at){if(end==null&&begin==null)return 0;if(end==null||begin==null||!Object.ReferenceEquals(end.Buffer,begin.Buffer))throw new MissingContract(at,"Pointer subtraction needs one declared stride12 allocation or two null pointers");return unchecked(end.PointIndex-begin.PointIndex);}
  static object Offset(NavigationCrowdPointAddress p,long amount,uint at){if(p==null){if(amount==0)return null;throw new MissingContract(at,"Own address domain does not interpret arithmetic from a null pointer");}return new NavigationCrowdPointAddress(p.Buffer,unchecked(p.PointIndex+amount));}
  static bool Same(NavigationCrowdPointAddress a,NavigationCrowdPointAddress b){return a==null?b==null:b!=null&&Object.ReferenceEquals(a.Buffer,b.Buffer)&&a.PointIndex==b.PointIndex;}
  static float Scalar(float v,uint at){uint bits=NavigationCoreContracts.ToBits(v);if((bits&0x7f800000u)==0&&(bits&0x7fffffu)!=0)throw new MissingContract(at,"Own floating domain excludes subnormal operands; native MXCSR is unobserved");return v;}
  static float Round(double v,uint at){if(v!=0&&!Double.IsNaN(v)&&!Double.IsInfinity(v)&&Math.Abs(v)<1.1754943508222875E-38)throw new MissingContract(at,"Own floating domain excludes subnormal intermediate");return Scalar(NavigationCoreContracts.F((float)v),at);}
  static Field<float> PointCell(Field<object> pointer,long offset,int axis,uint at){return new NavigationCrowdPointAddress(pointer,offset).Cell(axis,at);}
  static void MovePoints(Field<object> beginBits,long removed,long count,uint at){
   var begin=Address(beginBits,at);if(count<0||count>Int32.MaxValue/3||removed<0)throw new MissingContract(at,"Own memmove range is not a bounded forward point range");if(count==0)return;Need(begin,at);var buffer=Need(begin.Buffer,at);long start=begin.PointIndex,source=unchecked(start+removed),last=unchecked(source+count);
   if(start<0||source<start||last<source||last>buffer.Cells.Length/3)throw new MissingContract(at,"Own point memmove exceeds declared allocation");
   int scalars=(int)count*3;var snapshot=new Field<float>[scalars];for(int n=0;n<scalars;n++)snapshot[n]=buffer.At(source*3+n,at).Snapshot();for(int n=0;n<scalars;n++)buffer.At(start*3+n,at).CopyFrom(snapshot[n]);
  }
  public static void Resize030C60(NavigationCrowdWaypointVector vector,ulong target,NavigationCrowdPathBoundaries b,Invocation invocation){
   var i=Need(invocation,0x030c60);var end=Bits(vector,1,0x030c75);i.At=0x030c89;long signed=Difference(end,Bits(vector,0,i.At),i.At);ulong size=unchecked((ulong)signed);
   if(size>target){i.At=0x030cab;object next=Offset(Address(end,i.At),unchecked((long)(target-size)),i.At);i.At=0x030caf;vector.End08.Write(next);return;}
   if(size==target)return;
   i.At=0x030cce;ReserveAdditional030E10(vector,unchecked(target-size),i);
   i.At=0x030cd3;Need(b,i.At).RequireDiskNoOp00CE70(i.At);
   end=Bits(vector,1,0x030cd8);i.At=0x030cdf;ulong current=unchecked((ulong)Difference(end,Bits(vector,0,i.At),i.At));long amount=unchecked((long)(target-current));
   i.At=0x030cfd;vector.End08.Write(Offset(Address(Bits(vector,1,i.At),i.At),amount,i.At));
  }
  //030E10 is a reserve wrapper, not an allocator. Preserve both late Begin
  // reads and the captured Cap/End values; the next direct body is030EE0.
  public static void ReserveAdditional030E10(NavigationCrowdWaypointVector vector,ulong additional,Invocation invocation){
   var i=Need(invocation,0x030e10);var capacity=Bits(vector,2,0x030e1c);var end=Bits(vector,1,0x030e30);i.At=0x030e34;ulong spare=unchecked((ulong)Difference(capacity,end,i.At));if(spare>=additional)return;
   i.At=0x030e4c;ulong size=unchecked((ulong)Difference(end,Bits(vector,0,i.At),i.At));const ulong max=0x1555555555555555ul;
   if(unchecked(max-size)<additional){i.At=0x030e7e;throw new MissingContract(i.At,"Native stride12 vector length_error outside own allocation domain");}
   i.At=0x030e85;ulong total=unchecked((ulong)Difference(capacity,Bits(vector,0,i.At),i.At));ulong required=unchecked(size+additional),half=total>>1,candidate=0;
   if(total<=unchecked(max-half))candidate=unchecked(total+half);ulong requested=candidate>=required?candidate:required;
   i.At=0x030ec6;Reallocate030EE0(vector,requested,i);
  }
  static void CopyPoints021300(Field<object> begin,Field<object> end,NavigationPointBuffer destination,Invocation i){
   //r1-crowd-point-copy: unlike034FE0 this is a forward per-record8+4 loop.
   // The caller's positive fresh allocation makes R8 nonnull. No arbitrary
   // null-destination arithmetic, backward range or cross-allocation iterator
   // domain is inferred. Equality itself does not inspect pointed storage.
   i.At=0x021300;var first=Address(begin,i.At);var last=Address(end,i.At);if(Same(first,last))return;
   if(first==null||last==null||!Object.ReferenceEquals(first.Buffer,last.Buffer)||last.PointIndex<first.PointIndex)throw new MissingContract(i.At,"Own021300 requires an equal or forward same-allocation range");
   long source=first.PointIndex,limit=last.PointIndex,output=0;var buffer=first.Buffer;
   do{
    i.At=0x02130a;if(source<0||source>Int32.MaxValue/3)throw new MissingContract(i.At,"Outside own source scalar address domain");
    var x=buffer.At(source*3,i.At).Snapshot();var y=buffer.At(source*3+1,i.At).Snapshot();
    i.At=0x02130e;var dx=Need(destination,i.At).At(output*3,i.At);var dy=destination.At(output*3+1,i.At);dx.CopyFrom(x);dy.CopyFrom(y);
    i.At=0x021313;var z=buffer.At(source*3+2,i.At).Snapshot();i.At=0x021316;destination.At(output*3+2,i.At).CopyFrom(z);
    output=unchecked(output+1);source=unchecked(source+1);i.At=0x021322;
   }while(source!=limit);
   i.At=0x021327; // returned advanced R8 is not consumed by030EE0.
  }
  // Actual030EE0: r1-crowd-point-reallocation.022F00/023090 are explicit OWN
  // managed allocation/release adapters, not a recovered native heap or EH.
  public static void Reallocate030EE0(NavigationCrowdWaypointVector vector,ulong capacity,Invocation invocation){
   var i=Need(invocation,0x030ee0);i.At=0x030f03;if(capacity==0||capacity>Int32.MaxValue/3)throw new MissingContract(i.At,"Own stride12 allocator supports positive Int32 scalar extents only; native zero/oversized allocation is unknown");
   var fresh=new NavigationPointBuffer((int)capacity);Need(vector,i.At).Allocated(fresh);
   var end=Bits(vector,1,0x030f08);var begin=Bits(vector,0,0x030f0f);i.At=0x030f15;CopyPoints021300(begin,end,fresh,i);
   begin=Bits(vector,0,0x030f1a);end=Bits(vector,1,0x030f27);i.At=0x030f2e;long size=Difference(end,begin,i.At);var old=Address(begin,0x030f44);
   if(old!=null){var cap=Bits(vector,2,0x030f49);i.At=0x030f50;long allocated=Difference(cap,begin,i.At);i.At=0x030f6c;var buffer=Need(old.Buffer,i.At);if(old.PointIndex!=0||allocated!=buffer.Cells.Length/3||buffer.Cells.Length%3!=0||size<0||size>allocated)throw new MissingContract(i.At,"Own release requires the complete original stride12 allocation and a valid live extent");buffer.MarkReleased();}
   i.At=0x030f79;vector.Capacity10.Write(new NavigationCrowdPointAddress(fresh,(long)capacity));i.At=0x030f8a;vector.End08.Write(new NavigationCrowdPointAddress(fresh,size));i.At=0x030f8e;vector.Begin00.Write(new NavigationCrowdPointAddress(fresh,0));
  }
  public static NavigationCrowdPointAddress Second25C080(NavigationCrowdRequest request,NavigationCrowdPathBoundaries b,Invocation invocation){
   var i=Need(invocation,0x25c080);i.At=0x25c08b;if(Need(b,i.At).DisableSecondAssert422B92.Read(i.At)==0){var v=Vector(request,0x25c09b);var end=Bits(v,1,0x25c09b);i.At=0x25c0a9;ulong size=unchecked((ulong)Difference(end,Bits(v,0,i.At),i.At));if(size<2){i.At=0x25c0c7;if(b.DisableAllAssert42144A.Read(i.At)==0){i.At=0x25c0e3;throw new MissingContract(i.At,"Native second-waypoint diagnostic branch not implemented");}}}
   i.At=0x25c1a4;var begin=Bits(Vector(request,i.At),0,i.At);i.At=0x25c1a8;return new NavigationCrowdPointAddress(begin,1);
  }
  public static void PopWaypoint25BCF0(NavigationCrowdRequest request,NavigationCrowdPathBoundaries b,Invocation invocation){
   var i=Need(invocation,0x25bcf0);i.At=0x25bcff;if(Need(b,i.At).DisablePopAssert422B94.Read(i.At)==0){var check=Vector(request,0x25bd0f);var end=Bits(check,1,0x25bd0f);i.At=0x25bd13;if(Same(Address(Bits(check,0,i.At),i.At),Address(end,i.At))){i.At=0x25bd1d;if(b.DisableAllAssert42144A.Read(i.At)==0){i.At=0x25bd35;throw new MissingContract(i.At,"Native pop-waypoint diagnostic branch not implemented");}}}
   var v=Vector(request,0x25bdee);var e=Bits(v,1,0x25bdee);i.At=0x25bdfc;ulong size=unchecked((ulong)Difference(e,Bits(v,0,i.At),i.At));if(size>1){var begin=Bits(v,0,0x25be19);e=Bits(v,1,0x25be20);i.At=0x25be24;long count=unchecked(Difference(e,begin,i.At)-1);i.At=0x25be46;MovePoints(begin,1,count,i.At);}
   e=Bits(v,1,0x25be4c);i.At=0x25be54;ulong target=unchecked((ulong)(Difference(e,Bits(v,0,i.At),i.At)-1));i.At=0x25be7e;Resize030C60(v,target,b,i);
  }
  public static void TrimWaypoints25BA90(NavigationCrowdRequest request,NavigationPoint3 point,NavigationCrowdPathBoundaries b,Invocation invocation){
   var i=Need(invocation,0x25ba90);var v=Vector(request,0x25baa3);var end=Bits(v,1,0x25baa3);i.At=0x25bab1;long initial=Difference(end,Bits(v,0,i.At),i.At);if(initial==0)return;
   var px=Cell(point,0,0x25bacf).Snapshot();var pz=Cell(point,2,0x25bad4).Snapshot();var capturedBegin=Bits(v,0,0x25badd);var threshold=Need(b,0x25bae0).NearThreshold34B984.Snapshot();uint removed=0;
   do{
    var x=PointCell(capturedBegin,removed,0,0x25baf4).Snapshot();var z=PointCell(capturedBegin,removed,2,0x25bafa).Snapshot();
    i.At=0x25bb01;float dx=Round((double)Scalar(x.Read(i.At),i.At)-Scalar(px.Read(i.At),i.At),i.At);i.At=0x25bb05;float dz=Round((double)Scalar(z.Read(i.At),i.At)-Scalar(pz.Read(i.At),i.At),i.At);
    i.At=0x25bb09;float xx=Round((double)dx*dx,i.At);i.At=0x25bb0d;float zz=Round((double)dz*dz,i.At);i.At=0x25bb11;float sum=Round((double)zz+xx,i.At);i.At=0x25bb15;if(sum>Scalar(threshold.Read(i.At),i.At))break; // JA: unordered falls through.
    end=Bits(v,1,0x25bb1a);removed=unchecked(removed+1);i.At=0x25bb20;ulong size=unchecked((ulong)Difference(end,capturedBegin,i.At));if((ulong)removed>=size)break;
   }while(true);
   if(removed==0)return;
   i.At=0x25bb46;if(b.DisableTrimAssert422B93.Read(i.At)==0){end=Bits(v,1,0x25bb53);i.At=0x25bb5a;ulong size=unchecked((ulong)Difference(end,capturedBegin,i.At));if((ulong)removed>size){i.At=0x25bb78;if(b.DisableAllAssert42144A.Read(i.At)==0){i.At=0x25bba0;throw new MissingContract(i.At,"Native trimmed-waypoint diagnostic branch not implemented");}}}
   end=Bits(v,1,0x25bc61);i.At=0x25bc68;ulong liveSize=unchecked((ulong)Difference(end,Bits(v,0,i.At),i.At));if((ulong)removed<liveSize){var begin=Bits(v,0,0x25bc82);end=Bits(v,1,0x25bc88);i.At=0x25bc8c;long remaining=unchecked(Difference(end,begin,i.At)-removed);i.At=0x25bcb5;MovePoints(begin,removed,remaining,i.At);}
   end=Bits(v,1,0x25bcbb);i.At=0x25bcc2;ulong target=unchecked((ulong)(Difference(end,Bits(v,0,i.At),i.At)-removed));i.At=0x25bcdb;Resize030C60(v,target,b,i);
  }
  static Field<NavigationCrowdRefAddress> RefBits(NavigationCrowdRefVector v,int field,uint at){Need(v,at);return (field==0?v.Begin00:v.End08).Snapshot();}
  static long RefDifference(Field<NavigationCrowdRefAddress> endBits,Field<NavigationCrowdRefAddress> beginBits,uint at){var end=endBits.Read(at);var begin=beginBits.Read(at);if(end==null&&begin==null)return 0;if(end==null||begin==null||!Object.ReferenceEquals(end.Buffer,begin.Buffer))throw new MissingContract(at,"Own ref subtraction requires one allocation or two null pointers");return (long)end.Index-begin.Index;}
  static void MoveRefs(Field<NavigationCrowdRefAddress> bits,long removed,long count,uint at){var begin=Need(bits.Read(at),at);var buffer=Need(begin.Buffer,at);long source=(long)begin.Index+removed;if(removed<0||count<0||count>Int32.MaxValue||source<0||source+count>buffer.Cells.Length)throw new MissingContract(at,"Own ref memmove exceeds declared allocation");var snapshot=new Field<uint>[(int)count];for(int n=0;n<snapshot.Length;n++)snapshot[n]=buffer.At(source+n,at).Snapshot();for(int n=0;n<snapshot.Length;n++)buffer.At((long)begin.Index+n,at).CopyFrom(snapshot[n]);}
  public static byte TrimReferences25BE90(NavigationCrowdRequest request,uint reference,Invocation invocation){
   var i=Need(invocation,0x25be90);var v=Need(request,0x25be96).RefVector20;var end=RefBits(v,1,0x25be96);i.At=0x25be9e;long initial=RefDifference(end,RefBits(v,0,i.At),i.At);uint removed=0;
   if(initial!=0){var begin=RefBits(v,0,0x25bead);do{i.At=0x25beb3;var p=Need(begin.Read(i.At),i.At);if(reference==Need(p.Buffer,i.At).At((long)p.Index+removed,i.At).Read(i.At))break;end=RefBits(v,1,0x25beb9);removed=unchecked(removed+1);i.At=0x25bec0;if((ulong)removed>=unchecked((ulong)RefDifference(end,begin,i.At)))break;}while(true);
    if(removed!=0){end=RefBits(v,1,0x25bed4);i.At=0x25bed8;if((ulong)removed<unchecked((ulong)RefDifference(end,begin,i.At))){end=RefBits(v,1,0x25beec);i.At=0x25bef4;long count=unchecked(RefDifference(end,begin,i.At)-removed);i.At=0x25bf05;MoveRefs(begin,removed,count,i.At);}
     end=RefBits(v,1,0x25bf0b);i.At=0x25bf12;ulong target=unchecked((ulong)(RefDifference(end,RefBits(v,0,i.At),i.At)-removed));i.At=0x25bf1c;NavigationCrowdAdapterContracts.Resize030D20(v,target,i);
    }
   }
   end=RefBits(v,1,0x25bf26);i.At=0x25bf2a;ulong final=unchecked((ulong)RefDifference(end,RefBits(v,0,i.At),i.At));return (byte)(final>=1?1:0);
  }
  sealed class HeldLock {internal NavigationCrowdCriticalSection Value;internal bool Entered;}
  static HeldLock Enter(Field<NavigationCrowdCriticalSection> bits,NavigationCrowdPathBoundaries b,uint at){var value=Need(bits.Read(at),at);Monitor.Enter(value.Identity);b.Entered();return new HeldLock{Value=value,Entered=true};}
  static void Leave(HeldLock held,Field<NavigationCrowdCriticalSection> bits,NavigationCrowdPathBoundaries b,uint at){var current=Need(bits.Read(at),at);if(!Object.ReferenceEquals(current.Identity,held.Value.Identity))throw new MissingContract(at,"Late leave targets a different own critical-section identity");Monitor.Exit(held.Value.Identity);b.Left();held.Entered=false;}
  static NavigationPointBuffer OutputBuffer(Field<object> bits,uint at){var p=Address(bits,at);if(p==null)return null;if(p.PointIndex!=0)throw new MissingContract(at,"Frozen point-buffer call requires zero-based own allocation; arbitrary pointer-offset dispatch is unmodeled");return p.Buffer;}
  static NavigationRefBuffer CorridorBuffer(Field<NavigationCrowdRefAddress> bits,uint at){var p=bits.Read(at);if(p==null)return null;if(p.Index!=0)throw new MissingContract(at,"Frozen corridor-buffer call requires zero-based own allocation");return p.Buffer;}
  public static byte StraightPath030900(NavigationCrowdAdapterState state,NavigationPoint3 from,NavigationPoint3 to,NavigationCrowdRefVector references,NavigationCrowdWaypointVector waypoints,NavigationCrowdPathBoundaries b,Invocation invocation){
   var i=Need(invocation,0x030900);var lockBits=Need(state,0x030931).Critical138.Snapshot();i.At=0x030935;var held=Enter(lockBits,Need(b,i.At),i.At);
   try{
    var count=Field<uint>.Of(0);i.At=0x030957;Resize030C60(waypoints,4,b,i);
    var output=Bits(waypoints,0,0x03095c);var path=RefBits(references,0,0x03095f);var end=RefBits(references,1,0x030962);i.At=0x030966;int pathCount=unchecked((int)RefDifference(end,path,i.At));var query=Need(state,0x03099a).Query10.Snapshot();
    i.At=0x03099e;NavigationQuery q;try{q=query.Read(i.At);}catch(MissingContract e){throw new MissingContract(i.At,"Own typed query dispatch needs observed pointer bits; native callee prefix not evaluated",e);}
    uint status=NavigationStraightPathContracts.StraightPath(q,from,to,CorridorBuffer(path,i.At),pathCount,OutputBuffer(output,i.At),null,count,4,b.Straight,i);byte result;
    if((status&0x80000000u)!=0){var begin=Bits(waypoints,0,0x0309aa);i.At=0x0309ad;waypoints.End08.CopyFrom(begin);result=0;}
    else{uint n=count.Read(0x0309b6);i.At=0x0309c0;Resize030C60(waypoints,n,b,i);result=1;}
    var leave=Need(state,0x0309c8).Critical138.Snapshot();i.At=0x0309cc;Leave(held,leave,b,i.At);return result;
   }finally{if(held.Entered){Monitor.Exit(held.Value.Identity);b.Left();held.Entered=false;}} // OWN cleanup, not native EH recovery.
  }
 }
}
