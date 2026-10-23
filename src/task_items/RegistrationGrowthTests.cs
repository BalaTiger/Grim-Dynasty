using System;
using GrimDynasty.TaskItems;
class RegistrationGrowthTests {
 static int n;
 static void Need(bool condition,string message){n++;if(!condition)throw new Exception(message);}
 static RegistrationEntry E(uint key,object value=null){return new RegistrationEntry(key,value);}
 static void Order(RegistrationIndex index,params uint[] expected){
  uint[] actual=index.KeysInOrder();Need(actual.Length==expected.Length&&index.Count==(ulong)expected.Length,"ordered list count");
  for(int i=0;i<expected.Length;i++)Need(actual[i]==expected[i],"ordered key at "+i);
 }
 static RegistrationNode Existing(RegistrationIndex index,uint key){
  var result=index.Insert(key,new object());
  Need(result.Supported&&result.Inserted==false&&!result.MayHaveEffects,"existing-key observation must not mutate");return result.Node;
 }
 static void EightfoldOrderAndIdentity(){
  var a=new object();var b=new object();var c=new object();var d=new object();var e=new object();
  var index=new RegistrationIndex(8,8,0.75f,new[]{E(0,a),E(8,b),E(64,c),E(16,d),E(128,e),E(1)});
  var zero=Existing(index,0);var eight=Existing(index,8);var sixtyFour=Existing(index,64);var sixteen=Existing(index,16);var oneTwentyEight=Existing(index,128);
  var payload=new object();var result=index.Insert(2,payload);
  Need(result.Supported&&result.Inserted==true&&result.MayHaveEffects&&index.StateComplete,"one growth completes original unique insertion");
  Need(index.BucketCount==64&&index.BucketCapacity==64,"eightfold growth reserves exactly target capacity");
  // New bucket numbers are 53,61,53,5,53,36,23 in pre-rehash order.
  // Bucket segments follow first encounter, not numeric bucket order; distinct
  // keys encountered in the same new bucket prepend to that segment.
  Order(index,128,64,0,8,16,1,2);
  Need(Object.ReferenceEquals(index.FirstInBucket(53),oneTwentyEight)&&Object.ReferenceEquals(index.LastInBucket(53),zero),"new bucket endpoints preserve original node identities");
  Need(Object.ReferenceEquals(Existing(index,64),sixtyFour)&&Object.ReferenceEquals(Existing(index,8),eight)&&Object.ReferenceEquals(Existing(index,16),sixteen),"rehash moves nodes instead of allocating replacements");
  Need(Object.ReferenceEquals(Existing(index,0).Value,a)&&Object.ReferenceEquals(Existing(index,128).Value,e),"rehash preserves opaque payload identities");
  Need(Object.ReferenceEquals(Existing(index,2),result.Node)&&Object.ReferenceEquals(result.Value,payload),"original insertion returns its own surviving node");
  var erased=index.Erase(64);Need(erased.Supported&&erased.RemovedCount==1UL,"range deletion works with rebuilt endpoints");Order(index,128,0,8,16,1,2);
  var clear=index.Clear();Need(clear.Supported&&clear.RemovedCount==6UL&&index.BucketCount==8&&index.BucketCapacity==64,"clear resets logical buckets without undoing reserved capacity");
 }
 static void DuplicateTailAndSurvivor(){
  var oldestValue=new object();var latestValue=new object();var newValue=new object();
  var index=new RegistrationIndex(8,64,0.375f,new[]{E(0,oldestValue),E(8),E(0,latestValue)});
  var oldest=index.FirstInBucket(5);var oldTail=Existing(index,0);
  Need(Object.ReferenceEquals(oldTail.Value,latestValue)&&!Object.ReferenceEquals(oldTail,oldest),"pre-growth reverse lookup sees the last explicit duplicate");
  var inserted=index.Insert(64,newValue);
  Need(inserted.Supported&&inserted.Inserted==true&&index.StateComplete,"growth terminates after deleting the original tail");
  Need(index.Count==3UL&&index.BucketCount==64&&index.BucketCapacity==64,"duplicate rehash decrements count and retains sufficient capacity");
  Order(index,0,64,8);
  var survivor=Existing(index,0);
  Need(Object.ReferenceEquals(survivor,oldest)&&Object.ReferenceEquals(survivor.Value,oldestValue),"first encountered equal key survives; later tail payload does not overwrite it");
  Need(!Object.ReferenceEquals(survivor,oldTail)&&Object.ReferenceEquals(index.LastInBucket(53),inserted.Node),"deleted duplicate is not a bucket endpoint");
  Need(Object.ReferenceEquals(Existing(index,64).Value,newValue),"inserted payload survives duplicate cleanup of another key");
  Need(index.Erase(0).RemovedCount==1UL,"post-growth unique survivor erases once");Order(index,64,8);
 }
 static void AdjacentFinalNodeAndThreshold(){
  var original=new object();var index=new RegistrationIndex(8,96,1f/32f,new[]{E(0,original)});
  var originalNode=Existing(index,0);var inserted=index.Insert(64,null);
  Need(inserted.Supported&&inserted.Inserted==true&&index.BucketCount==64&&index.BucketCapacity==96,"one-layer load equal to threshold completes and preserves spare capacity");
  // The final old-tail node is already immediately before bucket first; the
  // splice is skipped while its first endpoint is still updated.
  Order(index,0,64);
  Need(Object.ReferenceEquals(index.FirstInBucket(53),originalNode)&&Object.ReferenceEquals(index.LastInBucket(53),inserted.Node),"already-adjacent old tail is processed exactly once");
  Need(Object.ReferenceEquals(Existing(index,0).Value,original),"old-tail survivor retains payload");
 }
 static void GrowthFactorBoundary(){
  var below=new RegistrationIndex(256,256,1f/256f,new[]{E(0)});
  Need(below.Insert(1,null).Supported&&below.BucketCount==2048&&below.BucketCapacity==2048,"bucket count below 512 grows eightfold");Order(below,0,1);
  var at=new RegistrationIndex(512,512,1f/512f,new[]{E(0)});
  var result=at.Insert(1,null);
  Need(result.Supported&&result.Inserted==true&&at.BucketCount==1024&&at.BucketCapacity==1024,"bucket count 512 grows twofold");Order(at,0,1);
  Need(at.FirstInBucket(1013).Key==0&&at.FirstInBucket(868).Key==1,"new mask uses all target bucket bits");
  var spare=new RegistrationIndex(512,2048,1f/512f,new[]{E(0)});
  Need(spare.Insert(1,null).Supported&&spare.BucketCount==1024&&spare.BucketCapacity==2048,"twofold growth does not shrink already sufficient capacity");
 }
 static void SuccessiveIndependentGrowth(){
  var payload=new object();var index=new RegistrationIndex(8,8,0.125f,new RegistrationEntry[0]);
  var zero=index.Insert(0,payload);var sixtyFour=index.Insert(64,null);
  Need(zero.Supported&&sixtyFour.Supported&&index.StateComplete&&index.BucketCount==64&&index.BucketCapacity==64,"first independent call grows eight to sixty-four");
  for(uint key=1;key<=7;key++){
   Need(index.Insert(key,null).Supported,"later unique insertion completes");
   if(key<7)Need(index.BucketCount==64&&index.StateComplete,"intervening calls stay within sixty-four bucket threshold");
  }
  Need(index.StateComplete&&index.Count==9UL&&index.BucketCount==512&&index.BucketCapacity==512,"later independent call reserves and grows sixty-four to five hundred twelve");
  Order(index,0,64,1,2,3,4,5,6,7);
  Need(Object.ReferenceEquals(Existing(index,0),zero.Node)&&Object.ReferenceEquals(Existing(index,64),sixtyFour.Node),"both earlier node identities survive two separate rehashes");
  Need(Object.ReferenceEquals(Existing(index,0).Value,payload),"later duplicate returns payload retained through two growth operations");
 }
 static void UnsupportedPrefixes(){
  var recursive=new RegistrationIndex(8,64,1f/64f,new[]{E(0)});
  var result=recursive.Insert(64,null);
  Need(!result.Supported&&result.Reason=="RegistrationIndexRecursiveGrowthOutsideCoveredDomain"&&result.Stage=="PostLinkGrowth","one layer still overloaded stops at declared coverage boundary");
  Need(result.MayHaveEffects&&!result.Inserted.HasValue&&!recursive.StateComplete,"unsupported growth retains insertion without claiming native return");
  Need(recursive.BucketCount==8&&recursive.BucketCapacity==64,"recursive-domain check occurs before bucket reset or reserve");Order(recursive,64,0);
  Need(result.Node!=null&&result.Node.Key==64,"unsupported prefix identifies inserted node");
  Need(!recursive.Insert(0,null).Supported&&!recursive.Erase(64).Supported&&!recursive.Clear().Supported,"incomplete prefix rejects further mutations");Order(recursive,64,0);
  var duplicateReduction=new RegistrationIndex(8,64,3f/64f,new[]{E(0),E(0),E(0)});
  result=duplicateReduction.Insert(1,null);
  Need(!result.Supported&&result.Reason=="RegistrationIndexRecursiveGrowthOutsideCoveredDomain","future duplicate deletion cannot authorize skipping first nested growth");Order(duplicateReduction,0,0,0,1);
  Need(duplicateReduction.BucketCount==8,"coverage rejection does not pre-deduplicate or partially reset buckets");
  var limited=new RegistrationIndex(4096,4096,1f/4096f,new[]{E(0)});
  result=limited.Insert(1,null);
  Need(!result.Supported&&result.Reason=="RegistrationIndexGrowthBucketBudgetOutsideCoveredDomain"&&result.Stage=="PostLinkGrowth","target beyond model bucket budget remains unsupported");
  Need(limited.BucketCount==4096&&limited.BucketCapacity==4096&&result.MayHaveEffects&&!limited.StateComplete,"bucket-budget boundary preserves old metadata plus new node");Order(limited,0,1);
 }
 static void DuplicateBypassAndClearReserve(){
  var value=new object();var overloaded=new RegistrationIndex(8,8,1f/1024f,new[]{E(0,value),E(8)});
  var existing=overloaded.Insert(0,new object());
  Need(existing.Supported&&existing.Inserted==false&&!existing.MayHaveEffects&&Object.ReferenceEquals(existing.Value,value),"duplicate bypasses all growth and reserve even when one layer would not suffice");
  Need(overloaded.BucketCount==8&&overloaded.BucketCapacity==8&&overloaded.StateComplete,"duplicate leaves complete overloaded explicit seed intact");Order(overloaded,0,8);
  var small=new RegistrationIndex(2,2,1f,new[]{E(0),E(0)});
  var clear=small.Erase(0);
  Need(clear.Supported&&clear.RemovedCount==2UL&&clear.MayHaveEffects&&small.StateComplete,"full-range erase reserves enough space for reset");
  Need(small.BucketCount==8&&small.BucketCapacity==8,"clear reserve grows exactly to eight buckets");Order(small);
  for(int i=0;i<8;i++)Need(small.FirstInBucket(i)==null&&small.LastInBucket(i)==null,"clear reserve fills every endpoint pair");
  var empty=new RegistrationIndex(1,1,1f,new RegistrationEntry[0]);clear=empty.Erase(uint.MaxValue);
  Need(clear.Supported&&clear.RemovedCount==0UL&&clear.MayHaveEffects&&empty.BucketCount==8&&empty.BucketCapacity==8,"empty miss also reserves and resets to eight buckets");
  Need(empty.Insert(uint.MaxValue,value).Supported&&Object.ReferenceEquals(Existing(empty,uint.MaxValue).Value,value),"clear-reserved state supports raw all-ones reinsertion");
 }
 static int Main(){try{EightfoldOrderAndIdentity();DuplicateTailAndSurvivor();AdjacentFinalNodeAndThreshold();GrowthFactorBoundary();SuccessiveIndependentGrowth();UnsupportedPrefixes();DuplicateBypassAndClearReserve();Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+n+",\"scope\":\"Own container metadata with successful allocation/release contracts, one growth layer and target buckets <=4096; no native memory, recursive growth, allocator internals or original execution.\"}");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
