using System;
namespace GrimDynasty.TaskItems {
 // Exclusive own field storage, not native memory or a constructor callback.
 // Reads return the little-endian value, or null when any requested byte is
 // unknown; stores change exactly the requested width without clearing neighbors.
 public interface IItemConstructorFields {
  ulong? ReadLittleEndian(int offset,int width);
  void Store(int offset,int width,ulong value);
 }
 // Only the scalar storage touched by the Item constructor's local segment,
 // plus two deliberately untouched neighboring fields. Null means unobserved
 // storage, not zero. This is neither a complete Item nor native memory/ABI.
 // Callers provide exclusive access to this own state.
 public sealed class ItemScalarInitializationState {
  readonly IItemConstructorFields shared;
  ulong? raw870,raw878,raw880,raw88C,raw894;
  uint? raw89C;
  readonly uint? untouched888,replicaStack6B0;
  public ulong? Raw870 {get{return shared==null?raw870:shared.ReadLittleEndian(0x870,8);}}
  public ulong? Raw878 {get{return shared==null?raw878:shared.ReadLittleEndian(0x878,8);}}
  public ulong? Raw880 {get{return shared==null?raw880:shared.ReadLittleEndian(0x880,8);}}
  public ulong? Raw88C {get{return shared==null?raw88C:shared.ReadLittleEndian(0x88C,8);}}
  public ulong? Raw894 {get{return shared==null?raw894:shared.ReadLittleEndian(0x894,8);}}
  public uint? Raw89C {get{return shared==null?raw89C:ReadSharedDword(0x89C);}}
  // These names remain source-readable; changing fields to properties is not
  // a binary ABI compatibility promise for previously compiled assemblies.
  public uint? Untouched888 {get{return shared==null?untouched888:ReadSharedDword(0x888);}}
  public uint? ReplicaStack6B0 {get{return shared==null?replicaStack6B0:ReadSharedDword(0x6B0);}}
  // These overlapping DWORD views share storage with the QWORD stores.
  // In particular, setting Stack88C must not leave the upper +890 unchanged.
  public uint? Gender874 {get{return shared!=null?ReadSharedDword(0x874):raw870.HasValue?(uint?)(uint)(raw870.Value>>32):null;}}
  public uint? Stack88C {get{return shared!=null?ReadSharedDword(0x88C):raw88C.HasValue?(uint?)unchecked((uint)raw88C.Value):null;}}
  public uint? Word890 {get{return shared!=null?ReadSharedDword(0x890):raw88C.HasValue?(uint?)(uint)(raw88C.Value>>32):null;}}
  public uint? Word898 {get{return shared!=null?ReadSharedDword(0x898):raw894.HasValue?(uint?)(uint)(raw894.Value>>32):null;}}
  public ItemScalarInitializationState(ulong? initial870=null,ulong? initial878=null,
   ulong? initial880=null,ulong? initial88C=null,ulong? initial894=null,uint? initial89C=null,
   uint? untouched888=null,uint? replicaStack6B0=null){
   raw870=initial870;raw878=initial878;raw880=initial880;raw88C=initial88C;
   raw894=initial894;raw89C=initial89C;this.untouched888=untouched888;this.replicaStack6B0=replicaStack6B0;
  }
  ItemScalarInitializationState(IItemConstructorFields fields){shared=fields;}
  public static ItemScalarInitializationState CreateSharedView(IItemConstructorFields fields){
   if(fields==null)throw new ArgumentNullException("fields");
   return new ItemScalarInitializationState(fields);
  }
  uint? ReadSharedDword(int offset){
   ulong? value=shared.ReadLittleEndian(offset,4);
   return value.HasValue?(uint?)unchecked((uint)value.Value):null;
  }
  internal void ExecuteCoveredStores(){
   // Game 323714 establishes EBP=0 before these six stores. No previous
   // register value, helper, global, string operation or service is read.
   if(shared!=null){
    shared.Store(0x870,8,0);shared.Store(0x878,8,0);shared.Store(0x880,8,0);
    shared.Store(0x88C,8,1);shared.Store(0x894,8,0);shared.Store(0x89C,4,0);
    return;
   }
   raw870=0; // 323716: QWORD +870
   raw878=0; // 32371D: QWORD +878
   raw880=0; // 323724: QWORD +880
   raw88C=1; // 32372B: QWORD +88C (not merely its low DWORD)
   raw894=0; // 323736: QWORD +894
   raw89C=0; // 32373D: DWORD +89C
  }
 }
 public sealed class ItemScalarInitializationResult {
  public readonly string Status,Reason;
  public readonly ItemScalarInitializationState State;
  public readonly int CompletedStores;
  public readonly bool MayHaveEffects;
  public readonly ulong? NextInstructionAddress;
  public bool IsCompleteConstructor {get{return false;}}
  internal ItemScalarInitializationResult(ItemScalarInitializationState state){
   State=state;Status=state==null?"Unsupported":"Known";
   Reason=state==null?"UnknownItemScalarInitializationState":null;
   CompletedStores=state==null?0:6;MayHaveEffects=state!=null;
   NextInstructionAddress=state==null?(ulong?)null:ItemScalarInitialization.NextInstructionAddress;
  }
 }
 public static class ItemScalarInitialization {
  public const ulong EntryInstructionAddress=0x180323714;
  public const ulong NextInstructionAddress=0x180323743;
  // Exact local fallthrough segment only. "Known" means these six stores
  // completed; it does not mean Item/Actor construction or QuestItem factory
  // creation succeeded. The segment has no native return value.
  // Predecessor 1FA340 and following string/helper work remain outside it.
  // Do not expose this operation as a ClassInfo constructor callback.
  public static ItemScalarInitializationResult Apply(ItemScalarInitializationState state){
   // Prepare the own report before the first store. Independent assignments
   // allocate no additional representation. Shared stores use the supplied own
   // storage; exceptions propagate with any prefix effects, without rollback.
   var result=new ItemScalarInitializationResult(state);
   if(state!=null)state.ExecuteCoveredStores();
   return result;
  }
 }
}
