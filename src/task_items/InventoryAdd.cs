using System;
using System.Collections.Generic;
using GrimDynasty.TokenActions;
using GrimDynasty.TaskAuto;
namespace GrimDynasty.TaskItems {
 public sealed class InventoryAddEngine {
  public uint? ClientPlayerId,ItemAddCount1C;
  public bool ClientGetterStable;
 }
 public sealed class InventoryAddContext {
  public ItemWorld Stock;
  public OfflineEvents Events;
  public uint? InventoryOwner20;
  public int? InventoryGender24;
  public InventoryAddEngine Engine;
  public List<HasItemCondition> CoveredConditions;
  public InventoryTakeContext SharedRemovalContext;
  public readonly List<QuestEvent> Produced=new List<QuestEvent>();
  public readonly List<string> Trace=new List<string>();
  public int LastDelivered;
 }
 public static class InventoryAdd {
  sealed class Plan {public ItemObject Item;public bool Count,Duplicate,Hook;}
  static MutationResult U(string s){return MutationResult.Unsupported(s);}
  static MutationResult Validate(InventoryAddContext c,uint id,bool skipPickupHook,out Plan plan){
   plan=null;if(c==null||c.Stock==null||c.Engine==null||c.Stock.InventoryItemIds==null||!c.InventoryOwner20.HasValue||!c.InventoryGender24.HasValue)return U("UnknownInventoryAddContext");
   if(!c.Engine.ClientGetterStable||!c.Engine.ClientPlayerId.HasValue||c.Stock.ClientPlayerId!=c.Engine.ClientPlayerId)return U("UnknownOrMismatchedAddClientSnapshot");
   ItemObject item;if(!c.Stock.Lookup(id,out item))return U("UnknownInventoryAddObjectIndex");
   if(id==0||item==null||item.Kind!=ItemObjectKind.Item||item.AddMethods!=ItemAddMethods.BaseItemObjectTable||item.IsRelic!=false)return U("AddRequiresExistingBaseItemProfile");
   // This exact primary Object table has GetItemType() == 0, SetGender ->
   // +0x874, and no special tutorial family. Transmute/save is unreachable.
   var p=new Plan{Item=item,Count=c.InventoryOwner20.Value==c.Engine.ClientPlayerId.Value||c.Engine.ClientPlayerId.Value==0,Duplicate=c.Stock.InventoryItemIds.Contains(id)};
   if(p.Count&&!c.Engine.ItemAddCount1C.HasValue)return U("UnknownEngineItemAddCounter");
   if(c.SharedRemovalContext!=null&&(!Object.ReferenceEquals(c.SharedRemovalContext.Stock,c.Stock)||!Object.ReferenceEquals(c.SharedRemovalContext.Events,c.Events)||c.SharedRemovalContext.InventoryOwnerObjectId!=c.InventoryOwner20))return U("AddSharedRemovalSnapshotMismatch");
   if(p.Duplicate){plan=p;return MutationResult.Known(false);}
   p.Hook=!skipPickupHook&&c.InventoryOwner20.Value==c.Engine.ClientPlayerId.Value;
   if(p.Hook&&(item.RemovalHookPackInitialized!=true||item.AddHook0CallbackPresent!=false))return U("InventoryAddHook0NotRecovered");
   if(c.Events==null)return U("UnknownInventoryAddEvents");
   var known=new Dictionary<OfflineHandler,HasItemCondition>();var covered=new List<OfflineHandler>();
   if(c.CoveredConditions!=null)foreach(var condition in c.CoveredConditions){
    if(condition==null||!Object.ReferenceEquals(condition.EventManager,c.Events)||!Object.ReferenceEquals(condition.ItemSnapshot,c.Stock))return U("AddItemReceiverSnapshotMismatch");
    var handler=condition.ItemHandler(16);if(!known.ContainsKey(handler)){known.Add(handler,condition);covered.Add(handler);}
   }
   List<OfflineHandler> actual;var admission=c.Events.CheckCovered(16,covered,out actual);if(admission.Status!="Known")return admission;
   var body=new ItemEventBody{PlayerId=c.InventoryOwner20.Value,ItemObjectId=id};foreach(var handler in actual){var check=known[handler].CheckReceive(body,16);if(check.Status!="Known")return check;}
   plan=p;return MutationResult.Known(false);
  }
  public static MutationResult Check(InventoryAddContext c,uint id,bool skipPickupHook){Plan p;return Validate(c,id,skipPickupHook,out p);}
  public static MutationResult Execute(InventoryAddContext c,uint id,bool skipPickupHook){
   Plan p;var check=Validate(c,id,skipPickupHook,out p);if(check.Status!="Known")return check;bool changed=false;
   if(p.Count){c.Engine.ItemAddCount1C=unchecked(c.Engine.ItemAddCount1C.Value+1);changed=true;c.Trace.Add("engine_counter_increment");}
   changed|=p.Item.RawGender874!=c.InventoryGender24;p.Item.RawGender874=c.InventoryGender24;c.Trace.Add("item_gender_set");
   if(p.Duplicate){c.Trace.Add("duplicate_return");return MutationResult.Known(changed);}
   // Source ordering is Send BEFORE append. Covered HasItem receivers only
   // set flags; they do not evaluate goals or mutate the inventory vector.
   var payload=new QuestEvent{Kind=16,Body=new ItemEventBody{PlayerId=c.InventoryOwner20.Value,ItemObjectId=id}};
   c.Produced.Add(payload);var sent=c.Events.Send(payload,16);if(sent.Status!="Known")throw new InvalidOperationException("Add context changed after preflight: "+sent.Reason);
   c.LastDelivered=sent.Delivered;c.Trace.Add("item_event16");c.Stock.InventoryItemIds.Add(id);c.Trace.Add("inventory_id_appended");
   if(p.Hook)c.Trace.Add("item_hook0_noop");c.Trace.Add("base_item_transmute_skipped");
   if(c.SharedRemovalContext!=null)c.SharedRemovalContext.DetachedObjects.Remove(id);
   return MutationResult.Known(true);
  }
 }
}
