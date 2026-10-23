using System;
using GrimDynasty.TaskItems;
class RecordCacheCallerPublicationTests {
 static int assertions;
 static void Need(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static byte[] B(string value){var bytes=new byte[value.Length];for(int i=0;i<bytes.Length;i++)bytes[i]=(byte)value[i];return bytes;}
 static RecordCacheEntry E(string key,object table){return new RecordCacheEntry(B(key),table);}
 static bool Same(int[] actual,params int[] expected){
  if(actual.Length!=expected.Length)return false;
  for(int i=0;i<actual.Length;i++)if(actual[i]!=expected[i])return false;
  return true;
 }
 static bool BytesEqual(byte[] actual,byte[] expected){
  if(actual.Length!=expected.Length)return false;
  for(int i=0;i<actual.Length;i++)if(actual[i]!=expected[i])return false;
  return true;
 }
 static void Throws<T>(Action action,string why) where T:Exception {
  bool caught=false;try{action();}catch(T){caught=true;}Need(caught,why);
 }
 sealed class Payload:IDisposable {public int Disposals;public void Dispose(){Disposals++;}}
 static bool Empty(RecordCacheNodePair pair){
  return pair.Name.Length==0&&pair.Name.Capacity==15&&pair.Name.FirstByte==0&&
   pair.Name.HeapStorageIdentity==null&&pair.Name.CopyBytes().Length==0;
 }
 static bool Ring(RecordCachePublicationInspection view){
  int[] next=view.CopyNextLinks(),previous=view.CopyPreviousLinks(),order=view.CopyListOrder();
  bool[] removed=view.CopyRemovedNodes(),seen=new bool[next.Length];seen[0]=true;int at=0;
  foreach(int node in order){
   if(node<=0||node>=next.Length||seen[node]||removed[node]||next[at]!=node||previous[node]!=at)return false;
   seen[node]=true;at=node;
  }
  if(next[at]!=0||previous[0]!=at)return false;
  for(int i=0;i<next.Length;i++)
   if(!removed[i]&&(!seen[i]||previous[next[i]]!=i||next[previous[i]]!=i))return false;
  return order.Length==view.Count;
 }
 static void ThreeWrites(RecordCacheCallerPublicationResult result){
  Need(result.CountIncremented&&result.HeadPreviousWritten&&result.PreviousNextWritten,
   "caller records count increment and both neighboring writes before publication");
  Need(result.NodeConstruction!=null&&result.NodeConstruction.Status=="Known"&&
   result.NodeConstruction.SourceNameCleared&&result.NodeConstruction.MayHaveEffects,
   "caller retains actual node construction and source consumption");
  Need(result.Publication!=null&&result.MayHaveEffects,"index result cannot erase earlier caller effects");
 }
 static void EmptyEntry(){
  var table=new Payload();var pair=new RecordCacheNodePair(B("q"),16,table);object storage=pair.Name.HeapStorageIdentity;
  var attempt=new RecordCacheCallerPublicationAttempt(8,16,1,new RecordCacheEntry[0],pair);
  var before=attempt.Inspect();
  Need(!attempt.Attempted&&!attempt.RequiresResolution&&attempt.Count==0&&before.Count==0,"prepared caller has not counted a candidate");
  Need(Same(before.CopyListOrder())&&Same(before.CopyNextLinks(),0,0)&&Same(before.CopyPreviousLinks(),0,0)&&before.EntryForNode(1)==null,"empty sentinel ring excludes reserved candidate slot");
  Need(before.CopyBucketFirst()[4]==0&&before.CopyBucketLast()[4]==0&&pair.Name.Length==1&&Object.ReferenceEquals(pair.Name.HeapStorageIdentity,storage),"preparation leaves buckets and source ownership untouched");
  Throws<InvalidOperationException>(()=>attempt.ToCompletedSnapshot(),"unrun caller cannot export completed cache");
  var result=attempt.RunOnce();ThreeWrites(result);var node=result.NodeConstruction.Node;var after=attempt.Inspect();
  Need(result.Status=="Known"&&result.Reason==null&&result.StopAddress==null&&result.LastCompletedStage=="CallerPublicationComplete"&&!result.RequiresResolution&&!attempt.RequiresResolution,"normal caller continuation reaches its own completion boundary");
  Need(attempt.Attempted&&attempt.Count==1&&Same(after.CopyListOrder(),1)&&Same(after.CopyNextLinks(),1,0)&&Same(after.CopyPreviousLinks(),1,0)&&Ring(after),"empty caller links one node into both directions of the sentinel ring");
  Need(after.BucketCount==8&&after.BucketCapacity==16&&after.CopyBucketFirst()[4]==1&&after.CopyBucketLast()[4]==1,"empty bucket publishes both endpoints without growth");
  Need(node.Next!=null&&Object.ReferenceEquals(node.Next,node.Previous)&&!Object.ReferenceEquals(node.Next,node),"empty caller passes a real sentinel identity rather than null/self-link arguments");
  Need(Empty(pair)&&Object.ReferenceEquals(node.Name.HeapStorageIdentity,storage)&&Object.ReferenceEquals(node.Table,table)&&Object.ReferenceEquals(pair.Table,table)&&table.Disposals==0,"heap name transfers while both Table observations remain opaque");
  var completed=attempt.ToCompletedSnapshot();
  Need(completed.Count==1&&Object.ReferenceEquals(completed.Find(B("q")).Entry,after.EntryForNode(1))&&Object.ReferenceEquals(completed.Find(B("q")).Entry.Table,table),"actual constructed entry reaches completed lookup snapshot");
  Need(before.Count==0&&before.EntryForNode(1)==null&&Same(before.CopyNextLinks(),0,0),"pre-run inspection remains isolated after real caller mutations");
  Need(Object.ReferenceEquals(attempt.RunOnce(),result)&&attempt.Count==1&&Object.ReferenceEquals(result.NodeConstruction.Node,node)&&Empty(pair),"successful replay neither reconstructs nor recounts the consumed source");
 }
 static void ExistingHeadAndTail(){
  var first=E("a",new object());var second=E("i",new object());var seed=new[]{first,second};
  var pair=new RecordCacheNodePair(B("q"),15,new object());
  var attempt=new RecordCacheCallerPublicationAttempt(8,8,1,seed,pair);seed[0]=E("z",null);
  var before=attempt.Inspect();
  Need(attempt.Count==2&&Same(before.CopyListOrder(),2,3)&&Same(before.CopyNextLinks(),2,0,3,0)&&Same(before.CopyPreviousLinks(),3,0,0,2),"nonempty entry has only seed links and no candidate count");
  Need(Object.ReferenceEquals(before.EntryForNode(2),first)&&before.CopyBucketFirst()[4]==2&&before.CopyBucketLast()[4]==3,"caller copies the public seed array and preserves initial bucket endpoints");
  var result=attempt.RunOnce();ThreeWrites(result);var after=attempt.Inspect();
  Need(result.Status=="Known"&&attempt.Count==3&&Same(after.CopyListOrder(),1,2,3)&&Ring(after),"same-bucket insertion keeps the actual caller head placement");
  Need(after.CopyNextLinks()[0]==1&&after.CopyPreviousLinks()[2]==1&&after.CopyPreviousLinks()[1]==0&&after.CopyNextLinks()[1]==2,"head insertion has both reciprocal caller writes and initialized candidate links");
  Need(after.CopyPreviousLinks()[0]==3&&after.CopyNextLinks()[3]==0&&after.CopyBucketFirst()[4]==1&&after.CopyBucketLast()[4]==3,"nonempty tail survives caller insertion and bucket-first update");
  var node=result.NodeConstruction.Node;
  Need(node.Next!=null&&node.Previous!=null&&!Object.ReferenceEquals(node.Next,node.Previous)&&!Object.ReferenceEquals(node.Next,node),"nonempty construction captures distinct old-head and sentinel identities");
  Need(Empty(pair)&&!result.NodeConstruction.HeapStorageTransferred&&BytesEqual(node.Name.CopyBytes(),B("q")),"short name is copied before source reset");

  var tail=new RecordCacheCallerPublicationAttempt(8,8,1,new[]{E("a",null),E("i",null),E("b",null)},new RecordCacheNodePair(B("c"),15,null));
  var tr=tail.RunOnce();ThreeWrites(tr);var ts=tail.Inspect();
  Need(tr.Status=="Known"&&Same(ts.CopyListOrder(),2,3,4,1)&&Ring(ts),"index publication may move the caller's new head to the global tail");
  Need(ts.CopyPreviousLinks()[0]==1&&ts.CopyNextLinks()[1]==0&&ts.CopyBucketFirst()[2]==1&&ts.CopyBucketLast()[2]==1,"new empty bucket retains correct tail links and endpoints");
  Need(tr.NodeConstruction.Node.Next!=null&&!Object.ReferenceEquals(tr.NodeConstruction.Node.Next,tr.NodeConstruction.Node.Previous),"constructor identity snapshots are not mistaken for live post-index links");
 }
 static void DuplicateTablesAndOwnership(){
  var oldFirst=new Payload();var oldLast=new Payload();var newlyLoaded=new Payload();
  var seeds=new[]{E("a",oldFirst),E("i",null),E("a",oldLast),E("b",null)};
  var original=new RecordCacheSnapshot(8,seeds);var pair=new RecordCacheNodePair(B("a"),31,newlyLoaded);object storage=pair.Name.HeapStorageIdentity;
  // Explicit already-loaded caller input, not a fabricated miss against the
  // same stable cache: a real lookup here would have hit the existing key.
  var attempt=new RecordCacheCallerPublicationAttempt(8,8,0.0001f,seeds,pair);
  var result=attempt.RunOnce();ThreeWrites(result);var view=attempt.Inspect();
  Need(result.Status=="Known"&&result.Publication.Inserted==false&&result.Publication.ReturnedNodeId==4&&result.Publication.RemovedNodeCount==1,"duplicate indexes by reverse bucket search and removes only the newly linked candidate");
  Need(attempt.Count==4&&Same(view.CopyListOrder(),2,3,4,5)&&Ring(view)&&view.CopyRemovedNodes()[1],"count increment precedes duplicate removal and leaves the original ring");
  Need(view.BucketCount==8&&view.CopyBucketFirst()[4]==2&&view.CopyBucketLast()[4]==4,"duplicate path skips overloaded maintenance and preserves existing endpoints");
  Need(Object.ReferenceEquals(attempt.ToCompletedSnapshot().Find(B("a")).Entry.Table,oldFirst)&&Object.ReferenceEquals(original.Find(B("a")).Entry.Table,oldFirst),"forward lookup retains original first Table despite reverse duplicate return identity");
  Need(Object.ReferenceEquals(result.NodeConstruction.Node.Table,newlyLoaded)&&Object.ReferenceEquals(view.EntryForNode(1).Table,newlyLoaded)&&Object.ReferenceEquals(pair.Table,newlyLoaded),"new Table remains distinct from the cache winner and survives diagnostic candidate tombstone");
  Need(Empty(pair)&&Object.ReferenceEquals(result.NodeConstruction.Node.Name.HeapStorageIdentity,storage),"source stays consumed even though the indexed duplicate removes the candidate");
  Need(oldFirst.Disposals==0&&oldLast.Disposals==0&&newlyLoaded.Disposals==0,"normal node/name release never calls Dispose on any opaque Table");
  Need(Object.ReferenceEquals(attempt.RunOnce(),result)&&attempt.Count==4&&Same(attempt.Inspect().CopyListOrder(),2,3,4,5),"duplicate replay cannot unlink twice or consume the empty source again");
 }
 static void GrowthAndPartialStop(){
  var seeds=new RecordCacheEntry[8];for(int i=0;i<seeds.Length;i++)seeds[i]=E(((char)('a'+i)).ToString(),new object());
  var growth=new RecordCacheCallerPublicationAttempt(8,8,1,seeds,new RecordCacheNodePair(B("i"),15,null));
  var gr=growth.RunOnce();ThreeWrites(gr);var grown=growth.Inspect();
  Need(gr.Status=="Known"&&growth.Count==9&&grown.BucketCount==64&&grown.BucketCapacity==64&&Ring(grown),"caller observes adopter's replacement endpoint arrays after one bounded growth");
  Need(grown.CopyBucketFirst().Length==64&&grown.CopyBucketFirst()[36]==1&&grown.CopyBucketLast()[36]==1&&growth.ToCompletedSnapshot().BucketCount==64,"inspection and completed snapshot use the new mask and endpoints");
  Need(Object.ReferenceEquals(growth.RunOnce(),gr)&&growth.Count==9,"successful growth cannot repeat caller writes");

  var source=new RecordCacheNodePair(B("i"),16,new Payload());
  var partial=new RecordCacheCallerPublicationAttempt(8,8,1.0f/128.0f,new[]{E("a",null)},source);
  var result=partial.RunOnce();ThreeWrites(result);var state=partial.Inspect();
  Need(result.Status=="Unsupported"&&result.Reason=="RecordCacheRecursiveGrowthOutsideCoveredDomain"&&result.StopAddress=="0x18002eae9"&&result.LastCompletedStage=="BucketPrefixWritten","recursive growth stops at the proven post-prefix boundary");
  Need(partial.Attempted&&partial.RequiresResolution&&result.RequiresResolution&&result.MayHaveEffects&&Empty(source),"caller exposes unresolved effects including already consumed source");
  Need(partial.Count==2&&state.BucketCount==8&&Same(state.CopyListOrder(),1,2)&&state.CopyBucketFirst()[4]==1&&state.CopyBucketLast()[4]==2&&Ring(state),"growth stop keeps count, both caller links and the newly written endpoint");
  Need(!result.Publication.Inserted.HasValue&&!result.Publication.ReturnedNodeId.HasValue,"unsupported maintenance does not invent native success outputs");
  Throws<InvalidOperationException>(()=>partial.ToCompletedSnapshot(),"partially published graph cannot become a completed cache");
  int[] first=state.CopyBucketFirst(),next=state.CopyNextLinks(),previous=state.CopyPreviousLinks();var node=result.NodeConstruction.Node;
  Need(Object.ReferenceEquals(partial.RunOnce(),result)&&Object.ReferenceEquals(result.NodeConstruction.Node,node)&&partial.Count==2&&Same(partial.Inspect().CopyNextLinks(),next)&&Same(partial.Inspect().CopyPreviousLinks(),previous)&&Same(partial.Inspect().CopyBucketFirst(),first),"sticky failure preserves the first graph without a second count/link/index attempt");
  var budget=new RecordCacheCallerPublicationAttempt(4096,4096,1.0f/4096.0f,new[]{E("a",null)},new RecordCacheNodePair(B("b"),15,null));
  var br=budget.RunOnce();
  Need(br.Status=="Unsupported"&&br.Reason=="RecordCacheGrowthBucketBudgetOutsideCoveredDomain"&&br.StopAddress=="0x18002eae9"&&br.RequiresResolution&&br.CountIncremented&&br.HeadPreviousWritten&&br.PreviousNextWritten&&budget.Count==2&&Ring(budget.Inspect()),"bucket ceiling also preserves real caller and publication effects");
  Throws<InvalidOperationException>(()=>budget.ToCompletedSnapshot(),"bucket-budget failure cannot export success");
 }
 static void RefusalsAndInterveningMove(){
  foreach(ulong capacity in new ulong[]{15,31}){
   var pair=new RecordCacheNodePair(B("taken"),capacity,new object());
   var attempt=new RecordCacheCallerPublicationAttempt(8,8,1,new[]{E("a",null)},pair);
   var other=new RecordCacheNodeConstructionAttempt(pair,null,null).RunOnce();
   Need(other.Status=="Known"&&Empty(pair),"separate fixture really consumes the prepared source first");
   var result=attempt.RunOnce();
   Need(result.Status=="Unsupported"&&result.Reason=="RecordCacheCallerSourceChangedBeforeEntry"&&result.StopAddress=="0x1801c5079"&&!result.MayHaveEffects&&!result.RequiresResolution&&!attempt.RequiresResolution,"changed short/heap source is rejected before this attempt acts");
   Need(result.NodeConstruction==null&&result.Publication==null&&!result.CountIncremented&&!result.HeadPreviousWritten&&!result.PreviousNextWritten&&attempt.Count==1&&Same(attempt.Inspect().CopyListOrder(),2)&&attempt.Inspect().EntryForNode(1)==null,"intervening move does not publish an invented empty-name candidate");
   Need(Object.ReferenceEquals(attempt.RunOnce(),result)&&attempt.Count==1&&Empty(pair),"source-change refusal is sticky without attributing other attempt effects to this one");
  }
  var absent=new RecordCacheCallerPublicationAttempt(8,8,1,new RecordCacheEntry[0],null);var ar=absent.RunOnce();
  Need(ar.Status=="Unsupported"&&ar.Reason=="UnknownRecordCacheNodePair"&&ar.NodeConstruction!=null&&ar.NodeConstruction.Status=="Unsupported"&&ar.Publication==null&&!ar.MayHaveEffects&&!ar.RequiresResolution&&absent.Count==0,"unknown pair preserves the construction refusal without claiming caller mutations");
  Throws<InvalidOperationException>(()=>absent.ToCompletedSnapshot(),"effect-free refusal is still not a completed publication");
  var oversized=new RecordCacheNodePair(B("a"),131073,null);
  var invalid=new RecordCacheCallerPublicationAttempt(8,8,1,new RecordCacheEntry[0],oversized);var ir=invalid.RunOnce();
  Need(ir.Status=="Unsupported"&&ir.Reason=="RecordCacheNodeNameCapacityOutsideCoveredDomain"&&!ir.MayHaveEffects&&oversized.Name.Capacity==131073&&oversized.Name.Length==1&&invalid.Count==0,"construction coverage refusal precedes source clear and caller count");
  var largeKey=new byte[65536];var largeEntry=new RecordCacheEntry(largeKey,null);var fullKeys=new RecordCacheEntry[16];
  for(int i=0;i<fullKeys.Length;i++)fullKeys[i]=largeEntry;
  var extra=new RecordCacheNodePair(B("x"),15,null);var total=new RecordCacheCallerPublicationAttempt(1,1,100,fullKeys,extra);var totalResult=total.RunOnce();
  Need(totalResult.Status=="Unsupported"&&totalResult.Reason=="RecordCacheCallerTotalKeyBudgetOutsideCoveredDomain"&&!totalResult.MayHaveEffects&&!totalResult.RequiresResolution&&total.Count==16&&extra.Name.Length==1,"aggregate seed-plus-candidate budget rejects before consumption while allowing repeated entry references as separate nodes");
  var retained=new RecordCacheNodePair(B("q"),15,null);
  Throws<ArgumentException>(()=>new RecordCacheCallerPublicationAttempt(8,8,1,new[]{E("a",null),E("b",null),E("i",null)},retained),"noncontiguous seed bucket is outside the explicit complete snapshot domain");
  Need(retained.Name.Length==1&&BytesEqual(retained.Name.CopyBytes(),B("q")),"constructor validation never consumes the supplied source");
 }
 static int Main(){
  try{
   EmptyEntry();ExistingHeadAndTail();DuplicateTablesAndOwnership();GrowthAndPartialStop();RefusalsAndInterveningMove();
   Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+assertions+",\"scenarios\":0,\"scope\":\"real bounded caller node construction, count and neighboring-link writes into shared publication storage; partial effects and sticky replay; no archive loading or native execution\"}");
   return 0;
  }catch(Exception e){Console.Error.WriteLine(e);return 1;}
 }
}
