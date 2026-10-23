using System;
namespace GrimDynasty.TaskItems {
 public static class RecordDepotInitialization {
  // Logical projection after normal successful Engine 1c4ad0 construction.
  // Container C=depot+8: 03c740 establishes an empty sentinel list, load factor
  // bits 3f800000, then calls 01b4f0(C,8) for eight buckets/capacity, mask7.
  // 1c4b43 zeros +50/+54; 1c4b64 zeros +5c/+60.
  //
  // Native allocator, critical-section initialization and fallback-table ctor
  // must have completed normally under this contract. We do not construct
  // their native objects, vptrs, pointers or exception handlers here. The
  // +48 fallback table and +58 fallback counter are outside this binary
  // success-path model; normal default cache misses still remain unsupported.
  // The supplied own gate is explicit and is not a native critical section.
  public static RecordDepotState CreateFresh(RegistrationGate gate){
   if(gate==null)throw new ArgumentNullException("gate");
   return new RecordDepotState(gate,new RecordCacheSnapshot(8,new RecordCacheEntry[0]),
    0,8,1.0f,0,0,0);
  }
 }
}
