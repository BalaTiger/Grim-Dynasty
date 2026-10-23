using System;
namespace GrimDynasty.TaskItems {
 // Own projection of Item +858..86F and one deliberately untouched neighbor.
 // Null scalar values mean unknown bytes, not zero. Addresses are evidence
 // labels; this is not a native allocation, pointer or complete Item object.
 public sealed class ItemConstructor858Subobject {
  readonly IItemConstructorFields shared;
  ulong? raw858,raw860,raw868;
  readonly ulong? untouched850;
  public ulong? Raw858 {get{return shared==null?raw858:shared.ReadLittleEndian(0x858,8);}}
  public ulong? Raw860 {get{return shared==null?raw860:shared.ReadLittleEndian(0x860,8);}}
  public ulong? Raw868 {get{return shared==null?raw868:shared.ReadLittleEndian(0x868,8);}}
  // Same source-readable name; no compatibility promise for old compiled DLLs.
  public ulong? Untouched850 {get{return shared==null?untouched850:shared.ReadLittleEndian(0x850,8);}}
  public ItemConstructor858Subobject(ulong? raw858=null,ulong? raw860=null,
   ulong? raw868=null,ulong? untouched850=null){
   this.raw858=raw858;this.raw860=raw860;this.raw868=raw868;this.untouched850=untouched850;
  }
  ItemConstructor858Subobject(IItemConstructorFields fields){shared=fields;}
  public static ItemConstructor858Subobject CreateSharedView(IItemConstructorFields fields){
   if(fields==null)throw new ArgumentNullException("fields");
   return new ItemConstructor858Subobject(fields);
  }
  void ClearTriplet(){
   if(shared==null){raw858=0;raw860=0;raw868=0;}
   else{shared.Store(0x858,8,0);shared.Store(0x860,8,0);shared.Store(0x868,8,0);}
  }
  internal void InitializeFixedZeroPath(){
   // 1FA34A / 1FA350 / 1FA354, RCX = Item this +858.
   ClearTriplet();
   // 1FA358 calls 029BF0 with the SAME RCX and RDX=0.
   // 029BFF / 029C05 / 029C09 execute these stores again, then
   // 029C10 branches to 029C50: no allocator, exception helper or callback.
   ClearTriplet();
   // Its RAX=0 is ignored. 1FA35D returns the saved original subobject.
  }
 }
 public sealed class ItemConstructor858PrefixResult {
  public readonly ItemConstructor858Subobject Subobject;
  public readonly ItemScalarInitializationState Scalars;
  public string Status {get;internal set;}
  public string Reason {get;internal set;}
  public ItemConstructor858Subobject HelperReturnedSubobject {get;internal set;}
  public int HelperStoreInstructions {get;internal set;}
  public int ScalarStoreInstructions {get;internal set;}
  public bool HelperCompleted {get;internal set;}
  public ulong? NextInstructionAddress {get;internal set;}
  public bool MayHaveEffects {get{return HelperStoreInstructions!=0||ScalarStoreInstructions!=0;}}
  public bool IsCompleteConstructor {get{return false;}}
  internal ItemConstructor858PrefixResult(ItemConstructor858Subobject subobject,ItemScalarInitializationState scalars){
   Subobject=subobject;Scalars=scalars;Status="Unsupported";
   NextInstructionAddress=ItemConstructor858Prefix.EntryInstructionAddress;
  }
 }
 public static class ItemConstructor858Prefix {
  public const ulong EntryInstructionAddress=0x180323702;
  public const ulong HelperEntryAddress=0x1801fa340;
  public const ulong ZeroCalleeAddress=0x180029bf0;
  public const ulong NextInstructionAddress=0x180323743;
  // Precondition: execution has reached this local Item entry with valid,
  // exclusive own storage. Prior Actor/0410E0 construction is NOT established
  // by calling this method. The two projections describe disjoint regions of
  // the same intended Item. No native field identities/lifetimes are inferred.
  // Only the fixed RDX=0 call is covered; no nonzero 029BF0 API is exposed.
  // Missing projected storage is an own Unsupported boundary, not native EH.
  public static ItemConstructor858PrefixResult Apply(ItemConstructor858Subobject subobject,
   ItemScalarInitializationState scalars){
   var result=new ItemConstructor858PrefixResult(subobject,scalars);
   if(subobject==null){result.Reason="UnknownItem858Subobject";return result;}
   subobject.InitializeFixedZeroPath();
   result.HelperStoreInstructions=6;result.HelperCompleted=true;
   result.HelperReturnedSubobject=subobject;
   result.NextInstructionAddress=ItemScalarInitialization.EntryInstructionAddress;
   // Do not prevalidate the later region and erase the already executed prefix.
   if(scalars==null){result.Reason="UnknownItemScalarInitializationState";return result;}
   // Reuse the exact existing scalar store body without allocating another
   // report after the helper prefix has already changed the supplied state.
   scalars.ExecuteCoveredStores();
   result.ScalarStoreInstructions=6;
   result.NextInstructionAddress=ItemScalarInitialization.NextInstructionAddress;
   result.Status="Known";
   return result;
  }
 }
}
