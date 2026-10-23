using System;
namespace GrimDynasty.TaskItems {
 public sealed class ObjectNameAssignmentResult {
  public readonly string Status,Reason;
  public readonly bool AssignmentSkipped,NameWritten,KeyWritten,CapacityGrew;
  public bool MayHaveEffects {get{return NameWritten||KeyWritten;}}
  internal ObjectNameAssignmentResult(string reason,bool skipped,bool written,bool key,bool grew){
   Status=reason==null?"Known":"Unsupported";Reason=reason;
   AssignmentSkipped=skipped;NameWritten=written;KeyWritten=key;CapacityGrew=grew;
  }
 }
 public static class ObjectNameAssignment {
  // Own coverage budgets, not native string limits. The native uint loop index
  // is sufficient throughout this domain; arbitrarily large strings are not.
  public const int MaximumNameBytes=65536;
  public const ulong MaximumNameCapacity=131072;
  static ObjectNameAssignmentResult Unsupported(string reason){return new ObjectNameAssignmentResult(reason,false,false,false,false);}
  static uint Hash(ObjectNameString name){
   uint hash=0x811c9dc5;
   for(int i=0;i<(int)name.Length;i++){
    uint signed=unchecked((uint)(int)(sbyte)name.ByteAt(i));
    hash=unchecked((hash^signed)*0x01000193u);
   }
   return hash;
  }
  // Covered entry is SetObjectName, whose helper arguments are position 0 and
  // count UINT64_MAX. This is not generic substring assignment/erase. Names
  // are valid, disjoint string storage unless they are the same object, with
  // exclusive caller access and normal native allocation/copy/release success.
  public static ObjectNameAssignmentResult Apply(ObjectBaseState target,ObjectNameString source){
   if(target==null)return Unsupported("UnknownObjectBaseState");
   if(source==null)return Unsupported("UnknownSourceName");
   if(source.Length>MaximumNameBytes)return Unsupported("NameLengthOutsideCoveredDomain");
   if(target.Name.Capacity>MaximumNameCapacity)return Unsupported("NameCapacityOutsideCoveredDomain");
   bool alias=Object.ReferenceEquals(target.Name,source),grew=false;
   if(!alias){
    ulong capacity=target.Name.Capacity;
    if(source.Length>capacity){
     // Engine 0x180022f9a..0x180022fda. Preserve the division/comparison,
     // including equality: max(rounded, old+old/2) is not always equivalent.
     ulong rounded=source.Length|15UL,half=capacity>>1;
     capacity=half>rounded/3?capacity+half:rounded;
     if(capacity>MaximumNameCapacity)return Unsupported("NameGrowthOutsideCoveredDomain");
     grew=true;
    }
    byte[] owned;
    try{owned=source.CopyBytes();}
    catch(OutOfMemoryException){return Unsupported("OwnNameSnapshotAllocationFailed");}
    // Logical normal-success outcome of reserve/copy/release then assignment.
    // No native pointer, intermediate allocator state, or failure rollback is
    // claimed; own allocation failure above occurs before model mutation.
    target.Name.ReplaceOwned(owned,capacity);
   }
   // Self-alias bypasses assignment but does not bypass this write. Embedded
   // NUL and signed high-bit bytes participate up to the full stored length.
   target.Fields.RawKey28=Hash(target.Name);
   return new ObjectNameAssignmentResult(null,alias,!alias,true,grew);
  }
 }
}
