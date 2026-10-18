using System.Collections.Generic;
using GrimDynasty.TaskItems;
public static class ExampleFullDelivery {
 public static ExampleDeliveryQuest Create(params int[] stacks){
  var x=ExampleDeliveryQuest.Create();x.Quest.Stock.AsciiCLocaleKnown=true;x.Item.RawStack88C=(uint)stacks[0];EnableItem(x.Item);
  x.Inventory.ActorRemovalMethods=InventoryActorRemovalMethods.Player;x.Inventory.DestroyAfterFullTake=false;x.Inventory.AttachedActionItemIds=new List<uint>();x.Inventory.MirrorPlayerPresent=false;x.Inventory.InventoryOwnerObjectId=1;
  for(int i=1;i<stacks.Length;i++){
   uint id=(uint)(100+i);var item=new ItemObject{Kind=ItemObjectKind.Item,ObjectName="ming/items/military_ration.dbr",DataNameFromSlot590="ming/items/military_ration.dbr",PrefixFromSlot590="",SuffixFromSlot590="",CompleteByteFromSlot3F8=1,StackMethods=ItemStackMethods.DirectItemFields88C6B0,RawStack88C=(uint)stacks[i],RawReplicaStack6B0=(uint)stacks[i],IsRelic=false};EnableItem(item);x.Quest.Stock.Objects.Add(id,item);x.Quest.Stock.InventoryItemIds.Add(id);
  }return x;
 }
 static void EnableItem(ItemObject item){item.IsShield=false;item.RemovalHookPackInitialized=true;item.RemovalHook1CallbackPresent=false;}
}
