using System;
using System.Collections.Generic;
namespace GrimDynasty.ReverseClient.Actions {
 public enum NavigationCrowdPointerSlot { Begin,End,Capacity }
 // Values from this address space are OWN tokens, never addresses of native or
 // managed process memory. Ranges are disjoint across all element types and
 // domains and never reused. Only explicitly registered live storage resolves.
 static class NavigationCrowdOwnPointerAddressSpace {
  static readonly object gate=new object();static ulong next=0x100000000000ul;
  internal static ulong Allocate(int capacity,uint at){lock(gate){ulong bytes=checked(((ulong)capacity+2ul)*8ul);if(next>UInt64.MaxValue-bytes)throw new MissingContract(at,"OWN pointer token space exhausted");ulong value=next;next+=bytes;return value;}}
 }
 public sealed class NavigationCrowdPointerDomain<T> where T:class {
  sealed class Allocation {internal ulong Base;internal NavigationCrowdPointerBuffer<T> Buffer;}
  readonly Dictionary<NavigationCrowdPointerBuffer<T>,Allocation> allocations=new Dictionary<NavigationCrowdPointerBuffer<T>,Allocation>();
  public int OwnActiveAllocations{get{return allocations.Count;}}
  public ulong EncodeOwn(NavigationCrowdPointerAddress<T> pointer,uint at){
   if(pointer==null)return 0;pointer.Buffer.RequireLive(at);Allocation a;
   if(!allocations.TryGetValue(pointer.Buffer,out a)){a=new Allocation{Base=NavigationCrowdOwnPointerAddressSpace.Allocate(pointer.Buffer.Cells.Length,at),Buffer=pointer.Buffer};allocations.Add(pointer.Buffer,a);}
   return a.Base+(ulong)pointer.Index*8ul;
  }
  public NavigationCrowdPointerAddress<T> ResolveOwn(ulong token,uint at){
   if(token==0)return null;
   foreach(var a in allocations.Values){ulong size=(ulong)a.Buffer.Cells.Length*8ul;if(token>=a.Base&&token-a.Base<=size){ulong offset=token-a.Base;if((offset&7)!=0)throw new MissingContract(at,"Unaligned OWN stride8 token");a.Buffer.RequireLive(at);return new NavigationCrowdPointerAddress<T>(a.Buffer,(int)(offset>>3));}}
   throw new MissingContract(at,"Token has no live binding in this explicit OWN address domain; native pointers are not inferred");
  }
  internal void Release(NavigationCrowdPointerBuffer<T> storage,uint at){storage.RequireLive(at);storage.MarkReleased();allocations.Remove(storage);}
 }
 // A captured MOV preserves the original cell's knowledge. Resolving the
 // pointer happens only at its use, never while taking an opaque snapshot.
 public sealed class NavigationCrowdPointerSnapshot<T> where T:class {
  readonly Field<NavigationCrowdPointerAddress<T>> typed;readonly Field<object> opaque;readonly Field<ulong> raw;readonly NavigationCrowdPointerDomain<T> domain;
  internal NavigationCrowdPointerSnapshot(Field<NavigationCrowdPointerAddress<T>> cell){typed=cell.Snapshot();}
  internal NavigationCrowdPointerSnapshot(Field<object> cell){opaque=cell.Snapshot();}
  internal NavigationCrowdPointerSnapshot(Field<ulong> cell,NavigationCrowdPointerDomain<T> ownDomain){raw=cell.Snapshot();domain=ownDomain;}
  public NavigationCrowdPointerAddress<T> Read(uint at){
   if(typed!=null)return typed.Read(at);if(raw!=null)return domain.ResolveOwn(raw.Read(at),at);
   object value=opaque.Read(at);if(value==null)return null;var address=value as NavigationCrowdPointerAddress<T>;if(address==null)throw new MissingContract(at,"Opaque pointer is not an explicitly typed OWN stride8 address");return address;
  }
 }
 // These are address views of EXISTING header cells, not temporary vectors.
 // A typed null header can be captured lazily by a no-hit query, as in native
 // code: validation occurs only when a header field is actually touched.
 public sealed class NavigationCrowdPointerHeader<T> where T:class {
  readonly NavigationView owner;readonly NavigationCrowdPointerVector<T> typed;readonly NavigationCrowdOpaqueVector opaque;readonly Field<ulong>[] raw;readonly NavigationCrowdPointerDomain<T> domain;readonly bool typedKind;
  NavigationCrowdPointerBuffer<T> lastGrowthAllocation;
  NavigationCrowdPointerHeader(NavigationCrowdPointerVector<T> vector){typedKind=true;typed=vector;owner=vector;}
  NavigationCrowdPointerHeader(NavigationView owner,NavigationCrowdOpaqueVector header,NavigationCrowdPointerDomain<T> domain){this.owner=owner;opaque=header;this.domain=domain;}
  NavigationCrowdPointerHeader(NavigationView owner,Field<ulong> begin,Field<ulong> end,Field<ulong> capacity,NavigationCrowdPointerDomain<T> domain){if(domain==null)throw new ArgumentNullException("domain");this.owner=owner;raw=new[]{begin,end,capacity};this.domain=domain;}
  public static NavigationCrowdPointerHeader<T> BorrowTyped(NavigationCrowdPointerVector<T> vector){return new NavigationCrowdPointerHeader<T>(vector);}
  public static NavigationCrowdPointerHeader<T> BorrowOpaque(NavigationView owner,NavigationCrowdOpaqueVector header,NavigationCrowdPointerDomain<T> domain){return new NavigationCrowdPointerHeader<T>(owner,header,domain);}
  public static NavigationCrowdPointerHeader<T> BorrowRaw(NavigationView owner,Field<ulong> begin,Field<ulong> end,Field<ulong> capacity,NavigationCrowdPointerDomain<T> domain){return new NavigationCrowdPointerHeader<T>(owner,begin,end,capacity,domain);}
  public void RequireLive(uint at){if(owner==null)throw new MissingContract(at,"Null OWN header owner");owner.RequireLive(at);if(!typedKind&&opaque==null&&raw==null)throw new MissingContract(at,"Null OWN header");}
  static int Index(NavigationCrowdPointerSlot slot){int n=(int)slot;if(n<0||n>2)throw new ArgumentOutOfRangeException("slot");return n;}
  Field<NavigationCrowdPointerAddress<T>> TypedCell(int n){return n==0?typed.Begin00:n==1?typed.End08:typed.Capacity10;}
  Field<object> OpaqueCell(int n){return n==0?opaque.Begin00:n==1?opaque.End08:opaque.Capacity10;}
  public NavigationCrowdPointerSnapshot<T> Capture(NavigationCrowdPointerSlot slot,uint at){RequireLive(at);int n=Index(slot);if(typedKind)return new NavigationCrowdPointerSnapshot<T>(TypedCell(n));if(raw!=null){if(raw[n]==null)throw new MissingContract(at,"Null raw header cell");return new NavigationCrowdPointerSnapshot<T>(raw[n],domain);}return new NavigationCrowdPointerSnapshot<T>(OpaqueCell(n));}
  public NavigationCrowdPointerAddress<T> Read(NavigationCrowdPointerSlot slot,uint at){return Capture(slot,at).Read(at);}
  public void Write(NavigationCrowdPointerSlot slot,NavigationCrowdPointerAddress<T> value,uint at){RequireLive(at);int n=Index(slot);if(typedKind)TypedCell(n).Write(value);else if(raw!=null){if(raw[n]==null)throw new MissingContract(at,"Null raw header cell");raw[n].Write(domain.EncodeOwn(value,at));}else OpaqueCell(n).Write(value);}
  // MOV begin -> end must not turn an unknown source into a failed read or 0.
  public void CopyBeginToEnd(uint load,uint store){RequireLive(load);if(typedKind){var bits=typed.Begin00.Snapshot();RequireLive(store);typed.End08.CopyFrom(bits);}else if(raw!=null){if(raw[0]==null||raw[1]==null)throw new MissingContract(load,"Null raw header cell");var bits=raw[0].Snapshot();RequireLive(store);raw[1].CopyFrom(bits);}else{var bits=opaque.Begin00.Snapshot();RequireLive(store);opaque.End08.CopyFrom(bits);}}
  public NavigationCrowdPointerBuffer<T> LastGrowthAllocation{get{return typedKind&&typed!=null?typed.LastGrowthAllocation:lastGrowthAllocation;}}
  internal void GrowthAllocated(NavigationCrowdPointerBuffer<T> storage,uint at){RequireLive(at);lastGrowthAllocation=storage;if(typedKind)typed.GrowthAllocated(storage);}
  internal void Release(NavigationCrowdPointerBuffer<T> storage,uint at){if(domain!=null)domain.Release(storage,at);else{storage.RequireLive(at);storage.MarkReleased();}}
  public void InstallOwnStorage(NavigationCrowdPointerBuffer<T> storage,int count){if(storage==null)throw new ArgumentNullException("storage");if(count<0||count>storage.Cells.Length)throw new ArgumentOutOfRangeException("count");storage.RequireLive(0);Write(NavigationCrowdPointerSlot.Begin,new NavigationCrowdPointerAddress<T>(storage,0),0);Write(NavigationCrowdPointerSlot.End,new NavigationCrowdPointerAddress<T>(storage,count),0);Write(NavigationCrowdPointerSlot.Capacity,new NavigationCrowdPointerAddress<T>(storage,storage.Cells.Length),0);}
 }
 public static class NavigationCrowdBorrowedPointerContracts {
  public static NavigationCrowdPointerHeader<NavigationCrowdAgent> Work40(NavigationCrowdGroup group,NavigationCrowdPointerDomain<NavigationCrowdAgent> domain){return NavigationCrowdPointerHeader<NavigationCrowdAgent>.BorrowOpaque(group,group==null?null:group.Work40,domain);}
  public static NavigationCrowdPointerHeader<NavigationCrowdAgent> Neighbors88(NavigationCrowdAgent agent,NavigationCrowdPointerDomain<NavigationCrowdAgent> domain){return NavigationCrowdPointerHeader<NavigationCrowdAgent>.BorrowRaw(agent,agent==null?null:agent.Words88B0[0],agent==null?null:agent.Words88B0[1],agent==null?null:agent.Words88B0[2],domain);}
  static Invocation Call(Invocation value,uint at){if(value==null)throw new MissingContract(at,"Null invocation");return value;}
  static NavigationCrowdPointerHeader<T> Header<T>(NavigationCrowdPointerHeader<T> value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null OWN pointer header");value.RequireLive(at);return value;}
  static ulong Difference<T>(NavigationCrowdPointerAddress<T> end,NavigationCrowdPointerAddress<T> begin,uint at)where T:class{if(end==null&&begin==null)return 0;if(end==null||begin==null||!Object.ReferenceEquals(end.Buffer,begin.Buffer))throw new MissingContract(at,"OWN pointer difference needs the same declared allocation or two null pointers");long count=(long)end.Index-begin.Index;if(count<0)throw new MissingContract(at,"Reversed extent outside OWN normal pointer-vector growth domain");return (ulong)count;}
  // Same saved034EC0/034FE0 algorithm used by legacy typed and borrowed headers.
  // Successful managed allocation is an explicit platform domain, not recovery
  // of native022F00/023090, allocator faults, heap byte aliases, or native EH.
  public static void ReserveAdditional034EC0<T>(NavigationCrowdPointerHeader<T> vector,ulong additional,Invocation invocation)where T:class{
   var i=Call(invocation,0x034ec4);var v=Header(vector,0x034ec4);var capBits=v.Capture(NavigationCrowdPointerSlot.Capacity,0x034ec4);var endBits=v.Capture(NavigationCrowdPointerSlot.End,0x034ecb);
   i.At=0x034ed2;var cap=capBits.Read(i.At);var end=endBits.Read(i.At);ulong available=Difference(cap,end,i.At);if(available>=additional){i.At=0x034f41;return;}
   var beginBits=v.Capture(NavigationCrowdPointerSlot.Begin,0x034ede);i.At=0x034eeb;var begin=beginBits.Read(i.At);ulong count=Difference(end,begin,i.At);const ulong maximum=0x1ffffffffffffffful;
   if(unchecked(maximum-count)<additional){i.At=0x034f04;throw new MissingContract(i.At,"Native length_error outside OWN normal pointer-vector allocation domain");}
   i.At=0x034f0b;ulong capacity=Difference(cap,begin,i.At),required=unchecked(additional+count),half=capacity>>1;ulong candidate=unchecked(maximum-half)>=capacity?unchecked(capacity+half):0;if(candidate>=required)required=candidate;
   i.At=0x034f38;Reallocate034FE0(v,required,i);
  }
  public static void Reallocate034FE0<T>(NavigationCrowdPointerHeader<T> vector,ulong capacity,Invocation invocation)where T:class{
   var i=Call(invocation,0x035002);i.At=0x035002;if(capacity==0||capacity>Int32.MaxValue)throw new MissingContract(i.At,"Native022F00 zero/large allocation outside OWN positive Int32-sized managed domain");NavigationCrowdPointerBuffer<T> fresh;
   try{fresh=new NavigationCrowdPointerBuffer<T>((int)capacity);}catch(OutOfMemoryException e){throw new MissingContract(i.At,"OWN managed allocation failed; native022F00 OOM/EH unknown",e);}
   var v=Header(vector,0x035007);v.GrowthAllocated(fresh,0x035007);var beginBits=v.Capture(NavigationCrowdPointerSlot.Begin,0x035007);var endBits=v.Capture(NavigationCrowdPointerSlot.End,0x03500d);
   i.At=0x035014;var end=endBits.Read(i.At);var begin=beginBits.Read(i.At);ulong copiedCount=Difference(end,begin,i.At);i.At=0x035017;if(copiedCount>capacity)throw new MissingContract(i.At,"Native memmove span exceeds OWN newly allocated destination extent");
   if(copiedCount!=0){begin.Buffer.RequireLive(i.At);for(int n=0;n<(int)copiedCount;n++){var bits=begin.Buffer.At(begin.Index+n,i.At).Snapshot();fresh.At(n,i.At).CopyFrom(bits);}}
   beginBits=v.Capture(NavigationCrowdPointerSlot.Begin,0x03501d);endBits=v.Capture(NavigationCrowdPointerSlot.End,0x035020);i.At=0x035024;end=endBits.Read(i.At);begin=beginBits.Read(i.At);ulong liveCount=Difference(end,begin,i.At);
   if(begin!=null){var capBits=v.Capture(NavigationCrowdPointerSlot.Capacity,0x035030);i.At=0x03503a;var cap=capBits.Read(i.At);ulong oldCapacity=Difference(cap,begin,i.At);i.At=0x035041;
    if(begin.Index!=0||cap==null||cap.Index!=begin.Buffer.Cells.Length||liveCount>oldCapacity)throw new MissingContract(i.At,"Native023090 argument is not a complete valid OWN allocation extent");v.Release(begin.Buffer,i.At);
   }
   i.At=0x03504f;v.Write(NavigationCrowdPointerSlot.Capacity,new NavigationCrowdPointerAddress<T>(fresh,(int)capacity),i.At);i.At=0x035053;if(liveCount>capacity)throw new MissingContract(i.At,"New end exceeds OWN allocated extent after capacity publication");
   i.At=0x035057;v.Write(NavigationCrowdPointerSlot.End,new NavigationCrowdPointerAddress<T>(fresh,(int)liveCount),i.At);i.At=0x03505b;v.Write(NavigationCrowdPointerSlot.Begin,new NavigationCrowdPointerAddress<T>(fresh,0),i.At);i.At=0x03506d;
  }
 }
}
