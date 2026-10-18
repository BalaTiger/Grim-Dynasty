using System;
using System.Collections.Generic;
using GrimDynasty.Conditions;
namespace GrimDynasty.TaskItems {
 public enum ItemObjectKind {Unknown,Item,ControllerCharacter,Other}
 public enum ItemStackMethods {Unknown,DirectItemFields88C6B0}
 public enum ItemAddMethods {Unknown,BaseItemObjectTable}
 public sealed class ItemObject {
  public ItemObjectKind Kind;
  public string ObjectName,DataNameFromSlot590,PrefixFromSlot590,SuffixFromSlot590;
  public byte? CompleteByteFromSlot3F8;
  int? sampledQuantityFromSlot618;
  public ItemStackMethods StackMethods;
  public bool? IsRelic;
  public ItemAddMethods AddMethods;
  public int? RawGender874;
  public bool? AddHook0CallbackPresent;
  public bool? IsShield,RemovalHookPackInitialized,RemovalHook1CallbackPresent;
  public uint? RawStack88C,RawReplicaStack6B0;
  public bool DirectStackMethodsKnown {get {return StackMethods==ItemStackMethods.DirectItemFields88C6B0||AddMethods==ItemAddMethods.BaseItemObjectTable;}}
  public int? QuantityFromSlot618 {get {if(DirectStackMethodsKnown)return RawStack88C.HasValue?(int?)unchecked((int)RawStack88C.Value):null;return sampledQuantityFromSlot618;}set {sampledQuantityFromSlot618=value;}}
  public bool EquipmentIndexComplete;
  public readonly Dictionary<uint,int> EquippedLocations=new Dictionary<uint,int>();
 }
 public sealed class SackSnapshot {public List<uint> ItemIds;}
 public sealed class ItemWorld {
  public bool ObjectIndexComplete,ReagentIndexComplete,MainPlayerIdentityStable,AsciiCLocaleKnown;
  public uint? RawControllerId16C0,ClientPlayerId;
  public List<uint> InventoryItemIds;
  public List<SackSnapshot> PersonalSacks,TransferSacks;
  public readonly Dictionary<uint,ItemObject> Objects=new Dictionary<uint,ItemObject>();
  public readonly Dictionary<string,uint> ReagentIds=new Dictionary<string,uint>(StringComparer.Ordinal);
  public bool Lookup(uint id,out ItemObject value){return Objects.TryGetValue(id,out value)||ObjectIndexComplete;}
  public static ItemWorld Empty(){return new ItemWorld{ObjectIndexComplete=true,ReagentIndexComplete=true,MainPlayerIdentityStable=true,RawControllerId16C0=0,InventoryItemIds=new List<uint>(),PersonalSacks=new List<SackSnapshot>(),TransferSacks=new List<SackSnapshot>()};}
 }
 public sealed class CountResult {
  public string Status,Reason;public int? Count;
  public static CountResult Known(int n){return new CountResult{Status="Known",Count=n};}
  public static CountResult Unsupported(string s){return new CountResult{Status="Unsupported",Reason=s};}
 }
 public static class ItemCountQuery {
  static CountResult U(string s){return CountResult.Unsupported(s);}
  public static bool Ascii(string s){if(s==null)return false;foreach(char c in s)if(c>127)return false;return true;}
  public static EvaluationResult Match(string a,string b,bool foldSlash,bool cString,bool localeKnown){
   if(a==null||b==null)return EvaluationResult.Unsupported("UnknownItemName");if(cString){int n=a.IndexOf('\0');if(n>=0)a=a.Substring(0,n);n=b.IndexOf('\0');if(n>=0)b=b.Substring(0,n);}
   if(!Ascii(a)||!Ascii(b))return EvaluationResult.Unsupported("NonAsciiItemCaseMappingNotRecovered");
   if(a.Length!=b.Length)return EvaluationResult.Known(false);
   for(int i=0;i<a.Length;i++){char x=a[i],y=b[i];if((x>='A'&&x<='Z')||(y>='A'&&y<='Z')){if(!localeKnown)return EvaluationResult.Unsupported("UnknownCrtCaseLocale");if(x>='A'&&x<='Z')x=(char)(x+32);if(y>='A'&&y<='Z')y=(char)(y+32);}
    if(x!=y&&!(foldSlash&&((x=='/'&&y=='\\')||(x=='\\'&&y=='/'))))return EvaluationResult.Known(false);
   }return EvaluationResult.Known(true);
  }
  static CountResult Items(ItemWorld w,IList<uint> ids,string query,bool complete,bool inventory){
   if(ids==null)return U("UnknownItemIdVector");int sum=0;
   foreach(uint id in ids){ItemObject item;if(!w.Lookup(id,out item))return U("UnknownItemObject");if(item==null)continue;
    if(item.Kind==ItemObjectKind.Unknown)return U("UnknownItemRtti");if(item.Kind!=ItemObjectKind.Item)continue;
    if(complete){if(!item.CompleteByteFromSlot3F8.HasValue)return U("UnknownItemCompleteResult");if(item.CompleteByteFromSlot3F8.Value==0)continue;}
    var match=Match(query,item.DataNameFromSlot590,true,false,w.AsciiCLocaleKnown);if(match.Status!="Known")return U(match.Reason);if(!match.Value.Value)continue;
    if(inventory){ItemObject controller;if(!w.Lookup(w.RawControllerId16C0.Value,out controller))return U("UnknownControllerObject");if(controller!=null){if(controller.Kind==ItemObjectKind.Unknown)return U("UnknownControllerRtti");if(controller.Kind==ItemObjectKind.ControllerCharacter){int location;if(!controller.EquippedLocations.TryGetValue(id,out location)){if(!controller.EquipmentIndexComplete)return U("UnknownEquipmentLocation");location=0;}if(location!=0)continue;}}}
    if(item.PrefixFromSlot590==null)return U("UnknownItemPrefix");if(item.PrefixFromSlot590.Length!=0)continue;
    if(item.SuffixFromSlot590==null)return U("UnknownItemSuffix");if(item.SuffixFromSlot590.Length!=0)continue;
    if(!item.QuantityFromSlot618.HasValue)return U("UnknownItemQuantityResult");sum=unchecked(sum+item.QuantityFromSlot618.Value);
   }return CountResult.Known(sum);
  }
  static CountResult Sacks(ItemWorld w,IList<SackSnapshot> sacks,string query,bool complete){
   if(sacks==null)return U("UnknownSackVector");int sum=0;foreach(var sack in sacks){if(sack==null)continue;var count=Items(w,sack.ItemIds,query,complete,false);if(count.Status!="Known")return count;sum=unchecked(sum+count.Count.Value);}return CountResult.Known(sum);
  }
  public static CountResult CountForHasItem(ItemWorld w,string query,bool complete){
   if(w==null||!w.MainPlayerIdentityStable||!w.RawControllerId16C0.HasValue)return U("UnknownStableMainPlayerInventoryContext");if(!Ascii(query))return U("ItemQueryOutsideAsciiDomain");
   var inventory=Items(w,w.InventoryItemIds,query,complete,true);if(inventory.Status!="Known")return inventory;int sum=inventory.Count.Value;
   uint reagentId;if(w.ReagentIds.TryGetValue(query,out reagentId)){
    ItemObject reagent;if(!w.Lookup(reagentId,out reagent))return U("UnknownReagentObject");if(reagent!=null){if(reagent.Kind==ItemObjectKind.Unknown)return U("UnknownReagentRtti");if(reagent.Kind==ItemObjectKind.Item){if(!reagent.QuantityFromSlot618.HasValue)return U("UnknownReagentQuantity");sum=unchecked(sum+reagent.QuantityFromSlot618.Value);}}
   }else if(!w.ReagentIndexComplete)return U("IncompleteReagentIndex");
   var personal=Sacks(w,w.PersonalSacks,query,complete);if(personal.Status!="Known")return personal;sum=unchecked(sum+personal.Count.Value);
   var transfer=Sacks(w,w.TransferSacks,query,complete);if(transfer.Status!="Known")return transfer;sum=unchecked(sum+transfer.Count.Value);
   return CountResult.Known(sum);
  }
 }
}
