using System;
using System.Threading;
namespace GrimDynasty.ReverseClient.Actions {
 // An explicitly established OWN BCL synchronization identity. This is not a
 // native CRITICAL_SECTION allocation or evidence of native thread startup.
 public sealed class NavigationCrowdCriticalSection:NavigationView {
  public readonly object Identity;
  int outstanding;
  public NavigationCrowdCriticalSection():this(new object()){}
  public NavigationCrowdCriticalSection(object identity){if(identity==null)throw new ArgumentNullException("identity");Identity=identity;}
  // Diagnostic only; no corresponding native field is claimed.
  public int OwnOutstandingEnters{get{return Interlocked.CompareExchange(ref outstanding,0,0);}}
  internal void Enter(uint at){RequireLive(at);Monitor.Enter(Identity);Interlocked.Increment(ref outstanding);}
  internal void Exit(){Monitor.Exit(Identity);Interlocked.Decrement(ref outstanding);}
 }
 public sealed class NavigationCrowdAdapterState:NavigationView {
  public readonly Field<NavigationQuery> Query10=new Field<NavigationQuery>();
  public readonly NavigationQueryFilter Filter18;
  public readonly NavigationPoint3 Extent11C;
  public readonly Field<NavigationCrowdCriticalSection> Critical138=new Field<NavigationCrowdCriticalSection>();
  public NavigationCrowdAdapterState():this(new NavigationQueryFilter(),new NavigationPoint3()){}
  public NavigationCrowdAdapterState(NavigationQueryFilter filter,NavigationPoint3 extent){if(filter==null||extent==null)throw new ArgumentNullException("inline view");Filter18=filter;Extent11C=extent;}
 }
 // A declared stride4 address in a managed reference buffer; not an integer
 // native pointer or a cross-primitive alias. One-past-end addresses are valid.
 public sealed class NavigationCrowdRefAddress {
  public readonly NavigationRefBuffer Buffer;public readonly int Index;
  public NavigationCrowdRefAddress(NavigationRefBuffer buffer,int index){if(buffer==null)throw new ArgumentNullException("buffer");if(index<0||index>buffer.Cells.Length)throw new ArgumentOutOfRangeException("index");Buffer=buffer;Index=index;}
 }
 public sealed class NavigationCrowdRefVector:NavigationView {
  public readonly Field<NavigationCrowdRefAddress> Begin00=new Field<NavigationCrowdRefAddress>(),End08=new Field<NavigationCrowdRefAddress>(),Capacity10=new Field<NavigationCrowdRefAddress>();
  // Initial owned backing identity, for diagnostics. Operations still reread
  // the actual three address fields; no mirror count is kept.
  public readonly NavigationRefBuffer Storage;
  public NavigationCrowdRefVector(int capacity,int count):this(new NavigationRefBuffer(capacity),count){}
  public NavigationCrowdRefVector(NavigationRefBuffer storage,int count){if(storage==null)throw new ArgumentNullException("storage");if(count<0||count>storage.Cells.Length)throw new ArgumentOutOfRangeException("count");Storage=storage;Begin00.Write(new NavigationCrowdRefAddress(storage,0));End08.Write(new NavigationCrowdRefAddress(storage,count));Capacity10.Write(new NavigationCrowdRefAddress(storage,storage.Cells.Length));}
 }
 public sealed class NavigationCrowdAdapterBoundaries {
  internal readonly NavigationNearestBoundaries Nearest;
  internal readonly NavigationCorridorBoundaries Corridor;
  public NavigationCrowdAdapterBoundaries(NavigationMeshTilesState mesh):this(mesh,null){}
  public NavigationCrowdAdapterBoundaries(NavigationMeshTilesState mesh,NavigationCorridorState search){if(mesh==null)throw new ArgumentNullException("mesh");Nearest=new NavigationNearestBoundaries(mesh.Spatial);if(search!=null)Corridor=new NavigationCorridorBoundaries(mesh,search);}
  public void UseObservedNearestConstants(){Nearest.UseObservedNearestConstants();}
  public void UseObservedCorridorConstants(){if(Corridor==null)throw new InvalidOperationException("Explicit corridor state required");Corridor.UseObservedCorridorConstants();}
  public void UseObservedConstants(){UseObservedNearestConstants();if(Corridor!=null)UseObservedCorridorConstants();}
 }
 // Engine030790/030B30/030BB0: r1-crowd-adapter-registration. Resize030D20:
 // object-lifecycle/rea-binary-parser-helpers/04,05, same Engine fingerprint.
 // Production query functions are called directly; there is no result callback.
 // Supported domain: explicit shared typed identities, preallocated vectors,
 // single-thread stable input lifetimes and the existing query FP domain.
 // Native allocation/EH, arbitrary byte aliases and loaded state are unmodeled.
 public static class NavigationCrowdAdapterContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null own crowd adapter view");var v=value as NavigationView;if(v!=null)v.RequireLive(at);return value;}
  static Field<float> Cell(NavigationPoint3 point,int axis,uint at){Need(point,at);try{return axis==0?point.X:axis==1?point.Y:point.Z;}catch(MissingContract e){throw new MissingContract(at,e.Message,e);}}
  static Field<NavigationQuery> QueryBits(NavigationCrowdAdapterState state,uint at){return Need(state,at).Query10.Snapshot();}
  static Field<NavigationCrowdCriticalSection> LockBits(NavigationCrowdAdapterState state,uint at){return Need(state,at).Critical138.Snapshot();}
  static NavigationQuery DispatchQuery(Field<NavigationQuery> bits,uint callAt){
   // The frozen sealed query type cannot encode an unknown pointer value.
   // This is an OWN call-dispatch boundary, not a claimed native dereference.
   // Known-null is passed through so the real callee determines its prefix.
   try{return bits.Read(callAt);}catch(MissingContract e){throw new MissingContract(callAt,"Own typed query dispatch requires observed pointer bits; native callee prefix not evaluated",e);}
  }
  sealed class HeldLock {
   internal readonly NavigationCrowdCriticalSection Value;internal bool Entered;
   internal HeldLock(NavigationCrowdCriticalSection value){Value=value;Entered=true;}
  }
  static HeldLock Enter(Field<NavigationCrowdCriticalSection> bits,uint at){var value=Need(bits.Read(at),at);value.Enter(at);return new HeldLock(value);}
  static void Leave(HeldLock held,Field<NavigationCrowdCriticalSection> bits,uint at){var value=Need(bits.Read(at),at);if(!Object.ReferenceEquals(value.Identity,held.Value.Identity))throw new MissingContract(at,"Leave targets a different own critical-section identity");held.Value.Exit();held.Entered=false;}
  static void Cleanup(HeldLock held){if(held!=null&&held.Entered){held.Value.Exit();held.Entered=false;}}
  static uint Nearest(Field<NavigationQuery> query,NavigationCrowdAdapterState state,NavigationPoint3 point,NavigationPoint3 extent,Field<uint> reference,NavigationPoint3 output,NavigationCrowdAdapterBoundaries b,Invocation i,uint callAt){
   i.At=callAt;var q=DispatchQuery(query,callAt);return NavigationNearestContracts.Nearest(q,point,extent,state.Filter18,reference,output,Need(b,callAt).Nearest,i);
  }
  public static uint Project030B30(NavigationCrowdAdapterState state,NavigationPoint3 point,NavigationPoint3 optionalOutput,NavigationCrowdAdapterBoundaries b,Invocation invocation){
   var i=Need(invocation,0x030b30);i.At=0x030b4f;var held=Enter(LockBits(state,0x030b45),i.At);
   try{
    var query=QueryBits(state,0x030b55);var reference=new Field<uint>();
    uint status=Nearest(query,state,point,state.Extent11C,reference,optionalOutput,b,i,0x030b76);
    var leave=LockBits(state,0x030b7b);
    // CMOVNZ r32,m32 fetches its memory operand even when not selected.
    uint candidate=reference.Read(0x030b8c),result=(status&0x40000000u)!=0?candidate:0u;
    i.At=0x030b91;Leave(held,leave,i.At);return result;
   }finally{Cleanup(held);} // OWN exception cleanup, not recovered native EH.
  }
  public static byte Project030BB0(NavigationCrowdAdapterState state,NavigationPoint3 point,Field<float> horizontalExtent,NavigationPoint3 optionalOutput,NavigationCrowdAdapterBoundaries b,Invocation invocation){
   var i=Need(invocation,0x030bb0);var enter=LockBits(state,0x030bc7);
   // Native third argument is XMM2, copied to XMM6 before entering the lock.
   var horizontal=Need(horizontalExtent,0x030bd1).Snapshot();i.At=0x030bd7;var held=Enter(enter,i.At);
   try{
    var vertical=Cell(state.Extent11C,1,0x030bdd).Snapshot();var query=QueryBits(state,0x030bea);
    var extent=new NavigationPoint3();extent.Y.CopyFrom(vertical);extent.X.CopyFrom(horizontal);extent.Z.CopyFrom(horizontal);
    var reference=new Field<uint>();uint status=Nearest(query,state,point,extent,reference,optionalOutput,b,i,0x030c16);
    byte result=(byte)(((status&0x40000000u)!=0&&reference.Read(0x030c22)!=0)?1:0);
    var leave=LockBits(state,0x030c2f);i.At=0x030c36;Leave(held,leave,i.At);return result;
   }finally{Cleanup(held);}
  }
  static Field<NavigationCrowdRefAddress> AddressBits(NavigationCrowdRefVector vector,int slot,uint at){Need(vector,at);return (slot==0?vector.Begin00:slot==1?vector.End08:vector.Capacity10).Snapshot();}
  static NavigationCrowdRefAddress Address(Field<NavigationCrowdRefAddress> bits,uint at){return Need(bits.Read(at),at);}
  static long Difference(NavigationCrowdRefAddress left,NavigationCrowdRefAddress right,uint at){if(!Object.ReferenceEquals(left.Buffer,right.Buffer))throw new MissingContract(at,"Own stride4 subtraction requires the same declared backing identity");return (long)left.Index-right.Index;}
  static NavigationCrowdRefAddress Offset(NavigationCrowdRefAddress address,long elements,uint at){long next=(long)address.Index+elements;if(next<0||next>address.Buffer.Cells.Length)throw new MissingContract(at,"Formed address outside own declared stride4 allocation domain");return new NavigationCrowdRefAddress(address.Buffer,(int)next);}
  public static void Resize030D20(NavigationCrowdRefVector vector,ulong target,Invocation invocation){
   var i=Need(invocation,0x030d20);var endBits=AddressBits(vector,1,0x030d2a);var beginBits=AddressBits(vector,0,0x030d31);
   var end=Address(endBits,0x030d3a);var begin=Address(beginBits,0x030d3a);long signedSize=Difference(end,begin,0x030d3a);ulong size=unchecked((ulong)signedSize);
   if(target<size){long delta=unchecked((long)(target-size));i.At=0x030d49;var next=Offset(end,delta,i.At);i.At=0x030d4d;vector.End08.Write(next);return;}
   if(size>=target)return;
   var capacity=Address(AddressBits(vector,2,0x030d65),0x030d6f);ulong remaining=unchecked((ulong)Difference(capacity,end,0x030d6f)),growth=target-size;
   if(remaining<growth){
    if(unchecked(0x3ffffffffffffffful-size)<growth){i.At=0x030d97;throw new MissingContract(i.At,"Native vector length_error outside own normal storage domain");}
    i.At=0x030dc7;throw new MissingContract(i.At,"Vector growth requires02E0F0; own adapter supports explicit preallocated capacity only");
   }
   end=Address(AddressBits(vector,1,0x030dcc),0x030dd8);begin=Address(AddressBits(vector,0,0x030dd8),0x030dd8);
   ulong current=unchecked((ulong)Difference(end,begin,0x030dd8));ulong added=unchecked(target-current);
   i.At=0x030de6;
   // This own memset domain requires the complete range to be declared before
   // writing; native partial page faults are not simulated as per-word faults.
   Need(end.Buffer,i.At);if(added>Int32.MaxValue||(ulong)end.Index+added>(ulong)end.Buffer.Cells.Length)throw new MissingContract(i.At,"Zero-fill exceeds declared own reference storage");
   for(int n=0;n<(int)added;n++)end.Buffer.At((long)end.Index+n,i.At).Write(0);
   end=Address(AddressBits(vector,1,0x030deb),0x030def);begin=Address(AddressBits(vector,0,0x030def),0x030def);
   long lateSize=Difference(end,begin,0x030def),deltaEnd=unchecked((long)(target-unchecked((ulong)lateSize)));
   // ADD [end],RBX rereads the destination after the prior end/begin arithmetic.
   var latest=Address(AddressBits(vector,1,0x030dfd),0x030dfd);var final=Offset(latest,deltaEnd,0x030dfd);i.At=0x030dfd;vector.End08.Write(final);
  }
  static NavigationRefBuffer CorridorBuffer(Field<NavigationCrowdRefAddress> bits,uint at){var address=Address(bits,at);if(address.Index!=0)throw new MissingContract(at,"Frozen corridor buffer interface cannot represent a nonzero begin address; no pointer rebasing guess");return address.Buffer;}
  public static byte FindCorridor030790(NavigationCrowdAdapterState state,NavigationPoint3 from,NavigationPoint3 to,NavigationCrowdRefVector vector,NavigationCrowdAdapterBoundaries b,Invocation invocation){
   var i=Need(invocation,0x030790);i.At=0x0307ce;var held=Enter(LockBits(state,0x0307ca),i.At);
   try{
    var firstReference=new Field<uint>();var secondReference=new Field<uint>();var first=new NavigationPoint3();var second=new NavigationPoint3();
    uint status=Nearest(QueryBits(state,0x0307fd),state,from,state.Extent11C,firstReference,first,b,i,0x030801);byte result=0;
    if((status&0x80000000u)==0&&firstReference.Read(0x030811)!=0){
     status=Nearest(QueryBits(state,0x030840),state,to,state.Extent11C,secondReference,second,b,i,0x030844);
     if((status&0x80000000u)==0){
      uint endReference=secondReference.Read(0x030850);
      if(endReference!=0){
       secondReference.Write(0);i.At=0x030868;Resize030D20(vector,32,i);
       var outputBits=AddressBits(vector,0,0x03086d);uint startReference=firstReference.Read(0x03089e);var query=QueryBits(state,0x0308a5);
       i.At=0x0308a9;var q=DispatchQuery(query,i.At);var output=CorridorBuffer(outputBits,i.At);
       var boundary=Need(b,i.At).Corridor;if(boundary==null)throw new MissingContract(i.At,"Explicit production corridor search state not supplied");
       status=NavigationCorridorContracts.FindPath(q,startReference,endReference,first,second,state.Filter18,output,secondReference,32,boundary,i);
       if((status&0x80000000u)==0){uint count=secondReference.Read(0x0308b5);i.At=0x0308bc;Resize030D20(vector,count,i);result=1;}
      }
     }
    }
    var leave=LockBits(state,0x0308c9);i.At=0x0308cd;Leave(held,leave,i.At);return result;
   }finally{Cleanup(held);}
  }
 }
}
