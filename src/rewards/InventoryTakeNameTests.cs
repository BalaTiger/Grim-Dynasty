using System;
using System.Collections.Generic;
using GrimDynasty.TaskItems;
using GrimDynasty.TaskAuto;
class InventoryTakeNameTests {
 static int n;static void Need(bool b,string s){n++;if(!b)throw new Exception(s);}
 static ExampleDeliveryQuest Started(){var x=ExampleDeliveryQuest.Create();x.Quest.Stock.AsciiCLocaleKnown=true;AutomaticTasks.Start(x.Quest.Auto);AutomaticTasks.Update(x.Quest.Auto);return x;}
 const string Query="ming/items/military_ration.dbr";
 static void Main(){
  var x=Started();int taken;Need(InventoryTake.CheckInventoryByName(x.Inventory,"MING\\ITEMS\\MILITARY_RATION.DBR",2).Status=="Known"&&x.Item.RawStack88C==3&&x.Quest.Condition.ChangedByte14==0,"name selection preflight is read-only");
  Need(InventoryTake.ExecuteInventoryByName(x.Inventory,"MING\\ITEMS\\MILITARY_RATION.DBR",2,out taken).Status=="Known"&&taken==2&&x.Item.RawStack88C==1&&x.Inventory.LastDelivered==1&&AutomaticTasks.Update(x.Quest.Auto).Status=="Known"&&x.Wallet.Money16C4==125,"native case/slash name matching drives inventory and quest completion");
  x=Started();x.Item.ObjectName="ming/items/other.dbr";Need(InventoryTake.ExecuteInventoryByName(x.Inventory,Query,2,out taken).Status=="Known"&&taken==0&&x.Item.RawStack88C==3,"take selects ObjectName rather than item-info database name");
  x=Started();Need(InventoryTake.ExecuteInventoryByName(x.Inventory,"ming/items/*.dbr",2,out taken).Status=="Known"&&taken==0&&x.Item.RawStack88C==3,"no invented glob matching");
  x=Started();x.Quest.Stock.RawControllerId16C0=20;var controller=new ItemObject{Kind=ItemObjectKind.ControllerCharacter};controller.EquippedLocations.Add(100,1);x.Quest.Stock.Objects.Add(20,controller);Need(InventoryTake.ExecuteInventoryByName(x.Inventory,Query,2,out taken).Status=="Known"&&taken==0&&x.Item.RawStack88C==3,"fixed skip-equipment profile retains equipped object");
  x=Started();x.Quest.Stock.RawControllerId16C0=20;x.Quest.Stock.Objects.Add(20,new ItemObject{Kind=ItemObjectKind.ControllerCharacter,EquipmentIndexComplete=false});Need(InventoryTake.ExecuteInventoryByName(x.Inventory,Query,2,out taken).Status=="Unsupported"&&x.Item.RawStack88C==3,"unknown equipped location refuses before mutation");
  x=Started();x.Item.RawStack88C=1;x.Quest.Stock.InventoryItemIds.Add(101);x.Quest.Stock.Objects.Add(101,new ItemObject{Kind=ItemObjectKind.Item,ObjectName=Query,StackMethods=ItemStackMethods.DirectItemFields88C6B0,IsRelic=false,RawStack88C=3,RawReplicaStack6B0=3});Need(InventoryTake.ExecuteInventoryByName(x.Inventory,Query,2,out taken).Status=="Unsupported"&&x.Item.RawStack88C==1&&x.Quest.Stock.Objects[101].RawStack88C==3,"first matching full-stack path not bypassed to consume a later stack");
  x=Started();x.Quest.Stock.InventoryItemIds.Add(999);x.Quest.Stock.Objects.Add(0,null);x.Quest.Stock.ObjectIndexComplete=false;Need(InventoryTake.ExecuteInventoryByName(x.Inventory,Query,2,out taken).Status=="Known"&&taken==2&&x.Item.RawStack88C==1,"fulfilled first stack stops before unvisited unknown object");
  foreach(int count in new[]{0,-1,-2}){x=Started();x.Inventory.Stock=null;Need(InventoryTake.ExecuteInventoryByName(x.Inventory,Query,count,out taken).Status=="Known"&&taken==0&&x.Item.RawStack88C==3,"name-entry nonpositive request differs from object entry");}
  x=Started();x.Quest.Stock.AsciiCLocaleKnown=false;Need(InventoryTake.ExecuteInventoryByName(x.Inventory,Query,2,out taken).Status=="Unsupported"&&x.Item.RawStack88C==3,"unknown CRT name profile not guessed");
  x=Started();x.Item.ObjectName=Query+"\0ignored";Need(InventoryTake.ExecuteInventoryByName(x.Inventory,Query,2,out taken).Status=="Known"&&taken==2,"object name copied from C string truncates at NUL");
  x=Started();Need(InventoryTake.ExecuteInventoryByName(x.Inventory,Query+"\0ignored",2,out taken).Status=="Known"&&taken==0&&x.Item.RawStack88C==3,"query keeps full std string length including NUL");
  x=Started();x.Item.PrefixFromSlot590="own_prefix";Need(InventoryTake.ExecuteInventoryByName(x.Inventory,Query,2,out taken).Status=="Known"&&taken==2&&x.Item.RawStack88C==1,"fixed affix-filter false does not import HasItem affix exclusions");
  Console.WriteLine("Inventory name take assertions passed: "+n);
 }
}
