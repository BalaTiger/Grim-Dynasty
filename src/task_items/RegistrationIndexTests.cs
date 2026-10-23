using System;
using GrimDynasty.TaskItems;
class RegistrationIndexTests {
 static int n;
 static void Need(bool condition,string message){n++;if(!condition)throw new Exception(message);}
 static RegistrationEntry E(uint key,object value=null){return new RegistrationEntry(key,value);}
 static RegistrationIndex Index(params RegistrationEntry[] entries){return new RegistrationIndex(8,8,100f,entries);}
 static void Order(RegistrationIndex index,params uint[] expected){
  uint[] actual=index.KeysInOrder();Need(actual.Length==expected.Length,"list length");
  for(int i=0;i<expected.Length;i++)Need(actual[i]==expected[i],"list key at "+i);
  Need(index.Count==(ulong)expected.Length,"count matches list");
 }
 static void Throws<T>(Action action,string message) where T:Exception {bool caught=false;try{action();}catch(T){caught=true;}Need(caught,message);}
 static void HashAndRawKeys(){
  Need(RegistrationIndex.Hash(0)==0x4d25767f9dce13f5UL,"FNV four zero bytes");
  Need(RegistrationIndex.Hash(1)==0xad2aca7747985764UL,"FNV little-endian one");
  Need(RegistrationIndex.Hash(0x80000000)==0x4d24f67f9dcd3a75UL,"FNV raw sign bit");
  Need(RegistrationIndex.Hash(uint.MaxValue)==0x994f76653e2a3951UL,"FNV all bits set");
  Need(RegistrationIndex.Hash(0x01020304)==0xb345225e3644edb5UL,"FNV byte order 04 03 02 01");
  Need(RegistrationIndex.Hash(0x04030201)==0xbe7a5e775165785dUL,"FNV distinct reversed byte order");
  var index=Index();var zero=new object();var high=new object();var all=new object();
  Need(index.BucketFor(0)==5&&index.BucketFor(1)==4&&index.BucketFor(8)==5,"mask of full FNV hash");
  Need(index.Insert(0,zero).Inserted==true,"raw zero is a container key");
  Need(index.Insert(0x80000000,high).Inserted==true,"raw high bit is distinct from zero");
  Need(index.Insert(uint.MaxValue,all).Inserted==true,"raw all-ones is not a container sentinel");
  Order(index,0x80000000,0,uint.MaxValue);
  Need(Object.ReferenceEquals(index.Insert(0,new object()).Value,zero),"zero keeps original payload");
  Need(Object.ReferenceEquals(index.Insert(0x80000000,new object()).Value,high),"sign bit keeps distinct original payload");
  Need(Object.ReferenceEquals(index.Insert(uint.MaxValue,new object()).Value,all),"all bits keep original payload");
 }
 static void InsertOrderAndDuplicate(){
  var old=new object();var index=Index(E(0,old),E(8),E(1));
  RegistrationNode original=index.FirstInBucket(5);var duplicate=index.Insert(0,new object());
  Need(duplicate.Supported&&duplicate.Inserted==false&&!duplicate.MayHaveEffects&&duplicate.Stage=="Duplicate","duplicate is a successful no-write result");
  Need(Object.ReferenceEquals(duplicate.Node,original)&&Object.ReferenceEquals(duplicate.Value,old),"duplicate returns the actual old node and payload");
  Order(index,0,8,1);
  var inserted=index.Insert(16,null);
  Need(inserted.Supported&&inserted.Inserted==true&&inserted.MayHaveEffects&&inserted.Value==null,"successful null payload remains a known value");
  Need(Object.ReferenceEquals(index.FirstInBucket(5),inserted.Node)&&index.LastInBucket(5).Key==8,"occupied bucket gains first, retains last");
  Order(index,16,0,8,1);
  inserted=index.Insert(2,old);
  Need(Object.ReferenceEquals(index.FirstInBucket(7),inserted.Node)&&Object.ReferenceEquals(index.LastInBucket(7),inserted.Node),"new bucket has one shared endpoint");
  Order(index,16,0,8,1,2);
  index.Insert(9,null);Order(index,16,0,8,9,1,2);
  Need(index.FirstInBucket(4).Key==9&&index.LastInBucket(4).Key==1,"insertion in a middle bucket preserves bucket segments");
  Need(index.FirstInBucket(0)==null&&index.LastInBucket(0)==null,"empty endpoints expose no sentinel key");
 }
 static void ConsecutiveRange(){
  var a=new object();var b=new object();var c=new object();
  var index=Index(E(0,a),E(0,b),E(8),E(0,c),E(1));
  var duplicate=index.Insert(0,new object());
  Need(duplicate.Inserted==false&&Object.ReferenceEquals(duplicate.Value,c),"reverse duplicate lookup returns last matching node in bucket");
  var removed=index.Erase(0);
  Need(removed.Supported&&removed.RemovedCount==2UL&&removed.MayHaveEffects&&removed.Stage=="Erased","erase selects first consecutive run only");
  Order(index,8,0,1);
  Need(index.FirstInBucket(5).Key==8&&index.LastInBucket(5).Key==0,"range erase preserves later equal-key run and endpoints");
  duplicate=index.Insert(0,new object());
  Need(duplicate.Inserted==false&&Object.ReferenceEquals(duplicate.Value,c),"later equal-key run retains payload");
  removed=index.Erase(0);Need(removed.Supported&&removed.RemovedCount==1UL,"subsequent erase reaches the later run");
  Order(index,8,1);
  Need(index.FirstInBucket(5).Key==8&&index.LastInBucket(5).Key==8,"last-node removal updates last endpoint");
  removed=index.Erase(8);Need(removed.Supported&&removed.RemovedCount==1UL,"bucket-only node erased while other buckets survive");
  Need(index.FirstInBucket(5)==null&&index.LastInBucket(5)==null,"removing the sole bucket node resets both endpoints");
  Order(index,1);
 }
 static void EndpointAndMiss(){
  var index=Index(E(0),E(8),E(16),E(1));
  var removed=index.Erase(8);Need(removed.Supported&&removed.RemovedCount==1UL,"middle node removed");
  Need(index.FirstInBucket(5).Key==0&&index.LastInBucket(5).Key==16,"middle removal retains both endpoints");Order(index,0,16,1);
  removed=index.Erase(0);Need(removed.Supported&&removed.RemovedCount==1UL,"first node removed");
  Need(index.FirstInBucket(5).Key==16&&index.LastInBucket(5).Key==16,"first endpoint moves to next");Order(index,16,1);
  removed=index.Erase(24);Need(removed.Supported&&removed.RemovedCount==0UL&&!removed.MayHaveEffects&&removed.Stage=="EraseMiss","missing key in occupied bucket does not clear nonempty list");
  removed=index.Erase(2);Need(removed.Supported&&removed.RemovedCount==0UL&&!removed.MayHaveEffects,"missing empty bucket does not clear nonempty list");Order(index,16,1);
  index.Insert(8,null);Order(index,8,16,1);
  var twoRuns=Index(E(0),E(0),E(1),E(1));
  removed=twoRuns.Erase(0);Need(removed.Supported&&removed.RemovedCount==2UL,"bucket boundary is an exclusive range end");Order(twoRuns,1,1);
 }
 static void GrowthBoundary(){
  int limit=RegistrationIndex.MaximumBuckets;
  var index=new RegistrationIndex(limit,limit,1f/limit,new RegistrationEntry[0]);var payload=new object();
  var first=index.Insert(0,payload);
  Need(first.Supported&&first.Inserted==true&&index.StateComplete&&index.BucketCount==limit,"load equal to threshold does not grow even at the bucket budget");
  var growth=index.Insert(8,new object());
  Need(!growth.Supported&&growth.Reason=="RegistrationIndexGrowthBucketBudgetOutsideCoveredDomain"&&growth.Stage=="PostLinkGrowth","growth beyond the bucket budget stops after linking and before bucket reset");
  Need(growth.MayHaveEffects&&!growth.Inserted.HasValue&&!index.StateComplete,"unknown native return retains effects and marks incomplete");
  Need(growth.Node!=null&&growth.Node.Key==8,"prefix result exposes already-linked node without claiming return success");
  Need(index.BucketCount==limit&&index.BucketCapacity==limit,"uncovered growth does not fabricate reset or enlarged buckets");Order(index,0,8);
  var rejected=index.Insert(0,new object());
  Need(!rejected.Supported&&!rejected.MayHaveEffects&&rejected.Stage=="IncompleteState","even duplicate is rejected after an incomplete operation");
  Need(!index.Erase(8).Supported&&!index.Clear().Supported,"erase and clear reject incomplete state");Order(index,0,8);
  Throws<InvalidOperationException>(delegate{index.FirstInBucket(5);},"incomplete bucket endpoints cannot be read as complete snapshot");
  var overloaded=new RegistrationIndex(8,8,0.125f,new[]{E(0,payload),E(8)});
  var duplicate=overloaded.Insert(0,new object());
  Need(duplicate.Supported&&duplicate.Inserted==false&&Object.ReferenceEquals(duplicate.Value,payload)&&overloaded.StateComplete&&overloaded.BucketCount==8&&overloaded.BucketCapacity==8,"explicit complete overloaded seed still permits duplicate bypass without growth");
  growth=overloaded.Insert(1,null);Need(growth.Supported&&growth.Inserted==true&&growth.MayHaveEffects&&overloaded.StateComplete&&overloaded.BucketCount==64&&overloaded.BucketCapacity==64,"new key in overloaded seed completes one sufficient growth layer");Order(overloaded,0,8,1);
  var exact=new RegistrationIndex(8,128,1f,new RegistrationEntry[0]);
  for(uint i=0;i<8;i++)Need(exact.Insert(i,null).Supported,"threshold equality supports all eight unique inserts");
  growth=exact.Insert(8,null);Need(growth.Supported&&growth.Inserted==true&&growth.MayHaveEffects&&exact.StateComplete&&exact.Count==9UL&&exact.BucketCount==64&&exact.BucketCapacity==128,"ninth unique insert rehashes after threshold equality while preserving sufficient existing capacity");
 }
 static void ClearBoundaries(){
  var index=new RegistrationIndex(16,16,1f,new RegistrationEntry[0]);
  var empty=index.Erase(123);
  Need(empty.Supported&&empty.Stage=="Cleared"&&empty.RemovedCount==0UL&&empty.MayHaveEffects&&index.StateComplete,"empty miss enters clear even with zero selected nodes");
  Order(index);Need(index.BucketCount==8&&index.BucketCapacity==16,"empty miss resets logical bucket count to eight while retaining capacity");
  for(int i=0;i<8;i++)Need(index.FirstInBucket(i)==null&&index.LastInBucket(i)==null,"clear fills both endpoints of every logical bucket");
  Need(index.Insert(16,null).Supported,"cleared state is usable with new mask");Order(index,16);
  index=Index(E(0),E(0));var clear=index.Erase(0);
  Need(clear.Supported&&clear.Stage=="Cleared"&&clear.MayHaveEffects&&clear.RemovedCount==2UL&&index.StateComplete,"whole equal-key range clears list and fills sentinel buckets");Order(index);
  index=Index(E(0),E(1));clear=index.Clear();
  Need(clear.Supported&&clear.MayHaveEffects&&clear.RemovedCount==2UL&&index.Count==0UL,"explicit clear returns original node count");
  Need(index.Clear().Supported&&index.StateComplete,"repeated empty clear with sufficient capacity completes");
  var retained=new RegistrationIndex(16,24,1f,new[]{E(0),E(8)});clear=retained.Clear();
  Need(clear.Supported&&retained.BucketCount==8&&retained.BucketCapacity==24,"larger capacity and stale unused endpoint storage are retained");
  retained.Insert(0,null);retained.Insert(8,null);Order(retained,8,0);
  Need(retained.FirstInBucket(5).Key==8&&retained.LastInBucket(5).Key==0,"reinsertion uses reset eight-bucket mask");
  var small=new RegistrationIndex(4,4,1f,new[]{E(0),E(1)});clear=small.Clear();
  Need(clear.Supported&&clear.Stage=="Cleared"&&clear.MayHaveEffects&&clear.RemovedCount==2UL&&small.StateComplete,"capacity below eight completes normal reserve after clearing list");Order(small);
  Need(small.BucketCount==8&&small.BucketCapacity==8,"clear reserves exactly eight buckets when prior capacity is insufficient");
  var emptySmall=new RegistrationIndex(1,1,1f,new RegistrationEntry[0]);clear=emptySmall.Erase(0);
  Need(clear.Supported&&clear.Stage=="Cleared"&&clear.MayHaveEffects&&clear.RemovedCount==0UL&&emptySmall.StateComplete&&emptySmall.BucketCount==8&&emptySmall.BucketCapacity==8,"empty low-capacity miss still completes reserve and bucket reset");
 }
 static void ExplicitSnapshotLimits(){
  Throws<ArgumentOutOfRangeException>(delegate{new RegistrationIndex(0,8,1f,new RegistrationEntry[0]);},"zero bucket count rejected");
  Throws<ArgumentOutOfRangeException>(delegate{new RegistrationIndex(3,8,1f,new RegistrationEntry[0]);},"non-power-of-two bucket count rejected");
  Throws<ArgumentOutOfRangeException>(delegate{new RegistrationIndex(8192,8192,1f,new RegistrationEntry[0]);},"bucket count outside model budget rejected");
  Throws<ArgumentOutOfRangeException>(delegate{new RegistrationIndex(16,8,1f,new RegistrationEntry[0]);},"insufficient declared capacity rejected");
  Throws<ArgumentOutOfRangeException>(delegate{new RegistrationIndex(8,8192,1f,new RegistrationEntry[0]);},"capacity outside model budget rejected");
  Throws<ArgumentOutOfRangeException>(delegate{new RegistrationIndex(8,8,float.NaN,new RegistrationEntry[0]);},"NaN threshold not in covered ordered-comparison domain");
  Throws<ArgumentOutOfRangeException>(delegate{new RegistrationIndex(8,8,float.PositiveInfinity,new RegistrationEntry[0]);},"infinite threshold outside declared coverage");
  Throws<ArgumentOutOfRangeException>(delegate{new RegistrationIndex(8,8,0,new RegistrationEntry[0]);},"nonpositive threshold outside declared coverage");
  Throws<ArgumentNullException>(delegate{new RegistrationIndex(8,8,1f,null);},"unknown seed cannot be assumed empty");
  Throws<ArgumentException>(delegate{Index((RegistrationEntry)null);},"unknown seed entry rejected");
  Throws<ArgumentException>(delegate{Index(E(0),E(1),E(8));},"noncontiguous bucket seed rejected");
  var source=new[]{E(0),E(1)};var index=Index(source);source[0]=E(999);Order(index,0,1);
  Throws<ArgumentOutOfRangeException>(delegate{index.LastInBucket(8);},"logical bucket bound checked");
  var many=new RegistrationEntry[RegistrationIndex.MaximumNodes];for(int i=0;i<many.Length;i++)many[i]=E(0);
  var limit=new RegistrationIndex(8,8,100000f,many);var result=limit.Insert(0,null);
  Need(result.Supported&&result.Inserted==false,"duplicate at node budget does not allocate or fail");
  result=limit.Insert(1,null);Need(!result.Supported&&!result.MayHaveEffects&&result.Stage=="NodeBudget"&&limit.StateComplete&&limit.Count==(ulong)many.Length,"model node budget rejects new allocation without invalidating state");
  Throws<ArgumentOutOfRangeException>(delegate{new RegistrationIndex(8,8,1f,new RegistrationEntry[RegistrationIndex.MaximumNodes+1]);},"oversized seed is outside model coverage");
 }
 static int Main(){try{HashAndRawKeys();InsertOrderAndDuplicate();ConsecutiveRange();EndpointAndMiss();GrowthBoundary();ClearBoundaries();ExplicitSnapshotLimits();Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+n+",\"scope\":\"Own explicit container snapshots and bounded single-layer reserve/rehash under successful allocation/release; no native memory, allocator internals, original execution or ObjectIdAllocator integration.\"}");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
