using System;
using System.Text;
using GrimDynasty.TaskItems;
class BinaryArchiveContextTests {
 static int n;
 static void Need(bool value,string message){n++;if(!value)throw new Exception(message);}
 static byte[] B(string value){return Encoding.ASCII.GetBytes(value);}
 static byte[] C(string value){return B(value+"\0");}
 static TableString S(string value){return new TableString(B(value));}
 static bool Rejected(Action action){try{action();return false;}catch(ArgumentException){return true;}}
 static bool Same(byte[] a,byte[] b){if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
 static BinaryFieldSnapshot Fields(ulong key,uint index){return new BinaryFieldSnapshot(0,0,new[]{new[]{new BinaryFieldEntry(key,new BinaryFieldValue(2,index))}});}
 static bool UnknownLayout(BinaryNameIdLookupResult result){return result!=null&&!result.NativeLayoutKnown&&result.Bucket==-1&&result.Visited==-1&&result.NodeIndex==-1;}
 static LoadTableBinarySnapshot Table(BinaryPointerCacheSnapshot names,BinaryStringPool pool,BinaryFieldSnapshot fields,int[] buffer,BinaryNameIdSnapshot ids){return new LoadTableBinarySnapshot(new RegistrationGate(),true,BinaryWarmState.FromPool(names,pool,fields,buffer,ids));}
 static RecordTableGetterResult Get(LoadTableBinarySnapshot table,string query,ulong address,TableString fallback){return RecordTableGetter.GetValue(table,C(query),fallback,address);}
 static void ColdWarmAndGrowthScenario(){
  var value=S("Own");var array=new[]{S(""),S("Class"),value,S("Unused")};var pool=BinaryStringPool.FromExisting(array);
  var ids=BinaryNameIdSnapshot.FromCompleteUniquePool(pool);var names=BinaryPointerCacheSnapshot.FromArchiveInitializedUniquePool(4,0,0x1000);
  var buffer=new[]{2};var table=Table(names,pool,Fields(1,0),buffer,ids);array[2]=S("changed");buffer[0]=3;
  Need(names.Shift==2&&names.Mask==3&&names.BucketCount==4&&names.Capacity==2&&names.UsedCount==1&&names.NativeClearedSlotBytes==24,"the explicit small initial context contains the post-empty-query slot and exact initial cleared prefix");
  var empty=names.GetSlot(0);
  Need(empty.Address==0x1000&&empty.CachedId==0&&Object.ReferenceEquals(names.CopyBucketChain(0)[0],empty)&&names.GetSlot(1)==null,"slot zero is the caller token for the supplied empty ID; unused own storage is writable");
  var first=Get(table,"Class",0x2004,null);
  Need(first.Status=="Known"&&Object.ReferenceEquals(first.Value,value)&&!first.UsedDefault&&first.Binary.Cold&&first.CacheNodeInserted&&first.CacheCountWritten&&first.MayHaveEffects&&names.UsedCount==2,"cold lookup through the complete logical pool feeds the actual numeric getter and preserves original value identity");
  Need(UnknownLayout(first.Binary.NameLookup)&&first.Binary.NameLookup.Found&&first.Binary.NameLookup.Entry.Id==1&&Same(first.Binary.NameLookup.Entry.CopyKey(),B("Class")),"logical lookup reports the original ID and full query bytes without fabricating native bucket or visited-node metadata");
  Need(first.GateAcquired&&first.GateReleased&&!table.Gate.IsHeldByCurrentThread&&first.Binary.NameTemporaryReleased&&Object.ReferenceEquals(names.GetSlot(1),first.Binary.PointerEntry),"cold publication retains its actual pointer slot and releases the table gate");
  var keyCopy=first.Binary.NameLookup.Entry.CopyKey();keyCopy[0]=0;var chainCopy=names.CopyBucketChain(1);chainCopy[0]=null;
  var again=Get(table,"Class",0x2004,null);
  Need(again.Status=="Known"&&Object.ReferenceEquals(again.Value,value)&&!again.Binary.Cold&&!again.Binary.Stale&&again.Binary.NameLookup==null&&!again.MayHaveEffects&&names.UsedCount==2,"a repeated address uses the same stable shared string and does not repeat the logical lookup or insertion");
  Need(Same(first.Binary.NameLookup.Entry.CopyKey(),B("Class"))&&Object.ReferenceEquals(names.CopyBucketChain(1)[0],first.Binary.PointerEntry),"exported lookup keys and bucket rows cannot alter the retained context");
  var grown=Get(table,"Class",0x3004,null);
  Need(grown.Status=="Known"&&Object.ReferenceEquals(grown.Value,value)&&grown.CacheStorageGrown&&grown.CacheNodeInserted&&names.Capacity==4&&names.UsedCount==3&&grown.Binary.PreviousCacheCapacity==2&&grown.Binary.CurrentCacheCapacity==4,"a full small initialized cache uses the existing relocation implementation before a new cold insertion");
  Need(!Object.ReferenceEquals(empty,names.GetSlot(0))&&names.GetSlot(0).CachedId==0&&!Object.ReferenceEquals(first.Binary.PointerEntry,names.GetSlot(1))&&names.GetSlot(1).CachedId==1&&names.NativeClearedSlotBytes==24,"growth detaches old storage while the initial-clear observation remains an initial historical fact");
  var fallback=S("default");var miss=Get(table,"missing",0x4004,fallback);
  Need(miss.Status=="Known"&&miss.UsedDefault&&Object.ReferenceEquals(miss.Value,fallback)&&UnknownLayout(miss.Binary.NameLookup)&&!miss.Binary.NameLookup.Found&&miss.Binary.NameId==-1&&!miss.MayHaveEffects&&names.UsedCount==3,"logical name miss remains numeric signed minus-one lookup and inserts no fabricated cache ID");
 }
 static void StaleAndFullBytesScenario(){
  var value=S("raw-value");var pool=BinaryStringPool.FromExisting(new[]{S(""),S("A"),S("A\0tail"),value});
  var names=BinaryPointerCacheSnapshot.FromArchiveInitializedUniquePool(4,0,0x1000);var entry=names.GetSlot(0);
  var table=Table(names,pool,Fields(1,0),new[]{3},BinaryNameIdSnapshot.FromCompleteUniquePool(pool));
  var stale=Get(table,"A\0unread",0x1000,null);
  Need(stale.Status=="Known"&&Object.ReferenceEquals(stale.Value,value)&&stale.Binary.Stale&&!stale.Binary.Cold&&stale.CacheIdWritten&&stale.CacheIdChanged&&entry.CachedId==1&&names.UsedCount==1,"the initialized empty-query token can take the proven stale branch when the explicit caller supplies changed query contents");
  Need(UnknownLayout(stale.Binary.NameLookup)&&stale.Binary.NameLookup.Entry.Id==1&&Same(stale.Binary.NameLookup.Entry.CopyKey(),B("A"))&&stale.KeyLength==1,"CString extraction precedes full-byte pool lookup, so an interior-NUL stored key is not confused with its prefix");
  var again=Get(table,"A\0different",0x1000,null);
  Need(again.Status=="Known"&&!again.MayHaveEffects&&!again.Binary.Stale&&Object.ReferenceEquals(again.Value,value)&&again.Binary.NameLookup==null,"the warm CString comparison ignores only the suffix beyond its matching first NUL");
  var fallback=S("default");var back=Get(table,"",0x1000,fallback);
  Need(back.Status=="Known"&&back.UsedDefault&&Object.ReferenceEquals(back.Value,fallback)&&back.CacheIdWritten&&entry.CachedId==0&&back.Binary.NameLookup.Entry.Id==0&&names.UsedCount==1,"stale lookup can restore the actual empty ID without allocating another slot");
  var missingPool=Table(names,null,Fields(0,0),new[]{-1},null);var stopped=Get(missingPool,"",0x1000,null);
  Need(stopped.Status=="Unsupported"&&stopped.Reason=="UnknownBinarySharedStringPool"&&!stopped.MayHaveEffects&&stopped.GateReleased,"FromPool null retains unknown shared storage rather than a known empty pool");
  var invalid=new BinaryNameCacheEntry(0x5000,0x100000000UL);var oversized=Table(new BinaryPointerCacheSnapshot(0,0,new[]{new[]{invalid}}),pool,Fields(0,0),new[]{3},null);
  var fullId=Get(oversized,"",0x5000,null);
  Need(fullId.Status=="Unsupported"&&fullId.Reason=="CachedBinaryNameIndexOutsideCoveredDomain"&&!fullId.MayHaveEffects,"cached qword IDs are bounded before narrowing to the int-based pool API");
 }
 static void ExplicitLargeProfileAndBoundaries(){
  const ulong address=0x0000000400001000;var names=BinaryPointerCacheSnapshot.FromArchiveInitializedUniquePool(82999,82687,address);
  Need(names.BucketCount==16384&&names.Mask==16383&&names.Shift==2&&names.Capacity==41499&&names.UsedCount==1&&names.NativeClearedSlotBytes==497976,"the explicit 82999-entry profile uses the proven strict log interval and original-count capacity formula");
  var empty=names.GetSlot(0);int populated=0,total=0;
  for(uint i=0;i<(uint)names.BucketCount;i++){var row=names.CopyBucketChain(i);if(row.Length!=0)populated++;total+=row.Length;}
  uint bucket=unchecked((uint)(address>>2))&names.Mask;
  Need(populated==1&&total==1&&empty.Address==address&&empty.CachedId==82687&&Object.ReferenceEquals(names.CopyBucketChain(bucket)[0],empty)&&names.GetSlot(41498)==null,"large complete buckets contain only the single empty-query cache entry and preserve the explicit 64-bit address token");
  Need(Rejected(delegate{new BinaryPointerCacheSnapshot(2,0,new BinaryNameCacheEntry[4097][]);}),"the legacy public snapshot still rejects more than 4096 buckets");
  Need(Rejected(delegate{new BinaryPointerCacheSnapshot(0,0,new[]{new BinaryNameCacheEntry[0]},0,4097,new BinaryNameCacheEntry[4097]);}),"the legacy allocated-storage constructor still rejects capacity above 4096");
  foreach(int count in new[]{0,3,32,35,64,67,131072,131073})
   Need(Rejected(delegate{BinaryPointerCacheSnapshot.FromArchiveInitializedUniquePool(count,0,0x1000);}),"initial count "+count+" is outside the explicit range or touches an uncovered native log boundary");
  Need(Rejected(delegate{BinaryPointerCacheSnapshot.FromArchiveInitializedUniquePool(4,-1,0x1000);})&&Rejected(delegate{BinaryPointerCacheSnapshot.FromArchiveInitializedUniquePool(4,4,0x1000);})&&Rejected(delegate{BinaryPointerCacheSnapshot.FromArchiveInitializedUniquePool(4,0,0);}),"the profile requires an in-range known empty ID and a nonzero explicit query token");
  var below=BinaryPointerCacheSnapshot.FromArchiveInitializedUniquePool(31,30,1);
  var above=BinaryPointerCacheSnapshot.FromArchiveInitializedUniquePool(36,35,1);
  Need(below.BucketCount==4&&below.Capacity==15&&below.NativeClearedSlotBytes==168&&above.BucketCount==8&&above.Capacity==18&&above.NativeClearedSlotBytes==216,"strict intervals on both sides of the rejected req-eight boundary use floor-log and raw integer divisions");
  var upper=BinaryPointerCacheSnapshot.FromArchiveInitializedUniquePool(131071,131070,1);
  Need(upper.BucketCount==16384&&upper.Capacity==65535&&upper.NativeClearedSlotBytes==786408,"the largest covered original count remains inside this instance's slot and bucket budgets");
 }
 static void LegacyContracts(){
  var duplicate=BinaryStringPool.FromExisting(new[]{S("same"),S("same")});
  Need(!duplicate.IsUnique&&Rejected(delegate{BinaryNameIdSnapshot.FromCompleteUniquePool(duplicate);})&&Rejected(delegate{BinaryNameIdSnapshot.FromCompleteUniquePool(null);}),"the complete-unique factory does not silently discard duplicates or accept unknown pool storage");
  Need(Rejected(delegate{new BinaryWarmState(null,new TableString[4097],null,null);}),"the old array API preserves its 4096-string budget");
  var longString=new TableString(new byte[65536]);var pool=new TableString[33];for(int i=0;i<pool.Length;i++)pool[i]=longString;
  Need(Rejected(delegate{new BinaryWarmState(null,pool,null,null);}),"the old array API still counts all payload slots against its 2 MiB budget");
  var entry=new BinaryNameIdEntry(B("A"),0);var ids=new BinaryNameIdSnapshot(1,new[]{entry});
  var names=new BinaryPointerCacheSnapshot(0,0,new[]{new BinaryNameCacheEntry[0]},0,1,new BinaryNameCacheEntry[1]);
  var value=S("value");var state=new BinaryWarmState(names,new[]{S("A"),value},Fields(0,0),new[]{1},ids);
  var result=Get(new LoadTableBinarySnapshot(new RegistrationGate(),true,state),"A",0x1000,null);
  Need(result.Status=="Known"&&Object.ReferenceEquals(result.Value,value)&&result.Binary.NameLookup.NativeLayoutKnown&&result.Binary.NameLookup.Bucket==0&&result.Binary.NameLookup.Visited==1&&result.Binary.NameLookup.NodeIndex==0&&Object.ReferenceEquals(result.Binary.NameLookup.Entry,entry),"the existing small name snapshot retains its exact native-layout metadata and immutable entry identity");
 }
 static int Main(){try{ColdWarmAndGrowthScenario();StaleAndFullBytesScenario();ExplicitLargeProfileAndBoundaries();LegacyContracts();Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+n+",\"scenarios\":2,\"scope\":\"Own explicit unique-pool lookup and bounded initialized pointer-cache profiles through the actual getter; no original archive read, native allocator execution or runtime-address inference.\"}");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
