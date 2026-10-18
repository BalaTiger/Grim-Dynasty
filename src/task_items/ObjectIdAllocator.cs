using System;
using System.Collections.Generic;
using GrimDynasty.TokenActions;
namespace GrimDynasty.TaskItems {
 public sealed class ObjectIdState {
  internal readonly object Gate=new object();
  public uint? Counter168;
  public byte? Wrapped16C;
  public bool? NetworkManagerPresent,NetworkReady6A8;
  public int? AssignedSlot1A8,PeerSlot78;
  public bool IndexComplete;
  // Raw keys of the native map at ObjectManager+0x30. Allocation searches
  // these exact counter keys; it does not insert or reserve the output ID.
  public HashSet<uint> IndexedKeys;
 }
 public static class ObjectIdAllocator {
  const uint LowMask=0x0fffffff;
  static MutationResult U(string s){return MutationResult.Unsupported(s);}
  static MutationResult Plan(ObjectIdState s,out uint id,out uint next,out byte wrapped){
   id=0;next=0;wrapped=0;if(s==null||!s.Counter168.HasValue||!s.Wrapped16C.HasValue||!s.NetworkManagerPresent.HasValue)return U("UnknownObjectIdState");
   int slot=0;
   if(s.NetworkManagerPresent.Value){
    if(!s.NetworkReady6A8.HasValue)return U("UnknownObjectIdNetworkGate");
    if(s.NetworkReady6A8.Value){if(!s.AssignedSlot1A8.HasValue)return U("UnknownObjectIdAssignedSlot");slot=s.AssignedSlot1A8.Value;if(slot==-1){if(!s.PeerSlot78.HasValue)return U("UnknownObjectIdFallbackPeerSlot");slot=unchecked(s.PeerSlot78.Value+1);}}
   }
   uint counter=s.Counter168.Value;wrapped=s.Wrapped16C.Value;
   if((counter&LowMask)==LowMask){wrapped=1;counter=0;}
   uint candidate;
   if(wrapped==0)candidate=counter;
   else{
    if(!s.IndexComplete||s.IndexedKeys==null)return U("IncompleteWrappedObjectIdIndex");
    int probes=0;
    while(s.IndexedKeys.Contains(counter)){
     if(probes++>=4096)return U("ObjectIdProbeBudgetOutsideCoveredDomain");
     counter=unchecked(counter+1);if((counter&LowMask)==LowMask)counter=0;
    }
    candidate=counter;
   }
   next=unchecked(counter+1);id=unchecked((uint)slot<<28)|candidate;return MutationResult.Known(false);
  }
  public static MutationResult Check(ObjectIdState s,out uint id){
   id=0;if(s==null)return U("UnknownObjectIdState");lock(s.Gate){uint next;byte wrapped;return Plan(s,out id,out next,out wrapped);}
  }
  public static MutationResult Allocate(ObjectIdState s,out uint id){
   id=0;if(s==null)return U("UnknownObjectIdState");lock(s.Gate){uint next;byte wrapped;var result=Plan(s,out id,out next,out wrapped);if(result.Status!="Known")return result;bool changed=s.Counter168!=next||s.Wrapped16C!=wrapped;s.Counter168=next;s.Wrapped16C=wrapped;return MutationResult.Known(changed);}
  }
 }
}
