using System;
using GrimDynasty.TaskItems;
class RecordCachePublicationAttemptTests {
 static int assertions;
 static void Need(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static byte[] B(string s){var b=new byte[s.Length];for(int i=0;i<s.Length;i++)b[i]=(byte)s[i];return b;}
 static RecordCacheEntry E(string s,object table){return new RecordCacheEntry(B(s),table);}
 static bool Same(int[] a,params int[] b){if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
 static bool Ring(RecordCachePublicationInspection s){
  int[] next=s.CopyNextLinks(),prev=s.CopyPreviousLinks(),order=s.CopyListOrder();bool[] removed=s.CopyRemovedNodes();
  if(next.Length!=prev.Length||next.Length!=removed.Length||order.Length!=s.Count||removed[0])return false;
  var seen=new bool[next.Length];seen[0]=true;int current=0;
  foreach(int id in order){if(id<=0||id>=next.Length||seen[id]||removed[id]||next[current]!=id||prev[id]!=current)return false;seen[id]=true;current=id;}
  if(next[current]!=0||prev[0]!=current)return false;
  for(int i=0;i<next.Length;i++)if(!removed[i]&&(!seen[i]||prev[next[i]]!=i||next[prev[i]]!=i))return false;
  return true;
 }
 static void Known(RecordCachePublicationAttemptResult r,bool inserted,int id){
  Need(r.Status=="Known"&&r.Reason==null&&r.StopAddress==null,"helper normal return reached");
  Need(r.Inserted==inserted&&r.ReturnedNodeId==id,"actual helper return node and flag");
  Need(r.MayHaveEffects&&!r.RequiresResolution,"entry and operation effects survive success");
 }
 static void Unsupported(RecordCachePublicationAttemptResult r,string reason){
  Need(r.Status=="Unsupported"&&r.Reason==reason&&r.StopAddress=="0x18002eae9","precise uncovered growth boundary");
  Need(r.MayHaveEffects&&r.RequiresResolution,"failure retains cumulative effects");
  Need(!r.Inserted.HasValue&&!r.ReturnedNodeId.HasValue,"native returns not reached after partial mutation");
 }
 static void Throws<T>(Action action,string why) where T:Exception {bool caught=false;try{action();}catch(T){caught=true;}Need(caught,why);}
 sealed class Payload:IDisposable {public int Disposals;public void Dispose(){Disposals++;}}
 static void NonGrowingAndDuplicate(){
  object table=new object();var seed=new[]{E("b",new object()),E("a",new object()),E("i",new object())};
  var a=new RecordCachePublicationAttempt(8,16,1.0f,seed,E("q",table));
  Need(!a.Attempted&&a.RequiresResolution&&a.Count==4,"prelinked entry needs completion");
  Throws<InvalidOperationException>(()=>a.ToCompletedSnapshot(),"entry snapshot is not completed cache");
  var before=a.Inspect();var r=a.RunOnce();Known(r,true,1);
  Need(a.Attempted&&!a.RequiresResolution&&a.Count==4&&a.BucketCount==8&&a.BucketCapacity==16,"no growth keeps capacity and count");
  var s=a.Inspect();Need(Same(s.CopyListOrder(),2,1,3,4)&&Ring(s),"cross-bucket candidate splice");
  Need(s.CopyBucketFirst()[4]==1&&s.CopyBucketLast()[4]==4&&s.CopyBucketFirst()[5]==2,"first updated and old last retained");
  Need(Same(before.CopyListOrder(),1,2,3,4)&&before.CopyBucketFirst()[4]==3,"inspection remains immutable");
  Need(r.RemovedNodeCount==0&&r.CandidateNodeId==1&&!s.CopyRemovedNodes()[1],"candidate remains live");
  var cache=a.ToCompletedSnapshot();Need(cache.Count==4&&Object.ReferenceEquals(cache.Find(B("q")).Entry.Table,table),"completed index produces real lookup snapshot");
  var depot=new RecordDepotState(new RegistrationGate(),cache,0);
  var load=RecordLoadFile.Apply(depot,new ObjectNameString(B("q"),15),RecordNameLocale.KnownC);
  Need(load.Status=="Known"&&load.CacheHit&&Object.ReferenceEquals(load.Table,table)&&depot.HitCount54==1,"explicit completed snapshot reaches actual LoadFile hit");
  Need(Object.ReferenceEquals(a.RunOnce(),r)&&a.Count==4&&Same(a.Inspect().CopyListOrder(),2,1,3,4),"successful attempt cannot publish candidate twice");

  var tail=new RecordCachePublicationAttempt(8,8,1.0f,new[]{E("a",null),E("i",null),E("b",null)},E("c",null));
  Known(tail.RunOnce(),true,1);var ts=tail.Inspect();
  Need(Same(ts.CopyListOrder(),2,3,4,1)&&ts.CopyPreviousLinks()[0]==1&&Ring(ts),"empty bucket moves candidate to tail");
  Need(ts.CopyBucketFirst()[2]==1&&ts.CopyBucketLast()[2]==1,"empty bucket initializes both endpoints");
  var equal=new RecordCachePublicationAttempt(1,1,1.0f,new RecordCacheEntry[0],E("",null));
  Known(equal.RunOnce(),true,1);
  Need(equal.BucketCount==1&&equal.Count==1&&Ring(equal.Inspect()),"threshold equality does not grow");
  var adjacent=new RecordCachePublicationAttempt(8,8,1.0f,new[]{E("a",null),E("i",null),E("b",null)},E("q",null));
  Known(adjacent.RunOnce(),true,1);
  Need(Same(adjacent.Inspect().CopyListOrder(),1,2,3,4)&&Ring(adjacent.Inspect()),"adjacent head not linked twice");

  var first=new Payload();var last=new Payload();var candidate=new Payload();
  var dup=new RecordCachePublicationAttempt(8,8,0.0001f,new[]{E("a",first),E("i",null),E("a",last),E("b",null)},E("a",candidate));
  var dr=dup.RunOnce();Known(dr,false,4);var ds=dup.Inspect();
  Need(dr.RemovedNodeCount==1&&dup.Count==4&&dup.BucketCount==8,"duplicate returns without maintenance even if overloaded");
  Need(Same(ds.CopyListOrder(),2,3,4,5)&&Ring(ds)&&ds.CopyRemovedNodes()[1],"candidate detached and count decreased");
  Need(ds.CopyBucketFirst()[4]==2&&ds.CopyBucketLast()[4]==4,"duplicate removal leaves indexed endpoints alone");
  Need(Object.ReferenceEquals(ds.EntryForNode(4).Table,last)&&Object.ReferenceEquals(ds.EntryForNode(1).Table,candidate),"return selects reverse duplicate; candidate is diagnostic tombstone");
  Need(first.Disposals==0&&last.Disposals==0&&candidate.Disposals==0,"metadata release does not Dispose opaque payload");
  Need(Object.ReferenceEquals(dup.ToCompletedSnapshot().Find(B("a")).Entry.Table,first),"forward lookup still selects first existing duplicate");
  Need(Object.ReferenceEquals(dup.RunOnce(),dr)&&dup.Count==4&&dr.MayHaveEffects,"duplicate attempt cannot remove candidate twice");
  var high=new byte[]{0x80,0,0xff};
  var raw=new RecordCachePublicationAttempt(1,1,4.0f,new[]{new RecordCacheEntry(high,last),new RecordCacheEntry(new byte[]{0x80},first)},new RecordCacheEntry(high,candidate));
  Known(raw.RunOnce(),false,2);
  Need(raw.Count==2&&raw.Inspect().CopyRemovedNodes()[1],"embedded NUL and high bytes compare at full stored length");
 }
 static void SingleGrowth(){
  var seed=new RecordCacheEntry[8];for(int i=0;i<seed.Length;i++)seed[i]=E(((char)('a'+i)).ToString(),new object());
  var a=new RecordCachePublicationAttempt(8,8,1.0f,seed,E("i",new object()));
  var r=a.RunOnce();Known(r,true,1);
  Need(a.BucketCount==64&&a.BucketCapacity==64&&a.Count==9,"eight buckets grow by eight");
  var s=a.Inspect();Need(Same(s.CopyListOrder(),1,2,3,4,5,6,7,8,9)&&Ring(s),"current-head traversal preserves fixture order for distinct new buckets");
  Need(s.CopyBucketFirst()[36]==1&&s.CopyBucketLast()[36]==1&&s.CopyBucketFirst()[12]==2,"new mask routes i and a to distinct buckets");
  Need(Object.ReferenceEquals(s.EntryForNode(2),seed[0])&&Object.ReferenceEquals(s.EntryForNode(9),seed[7]),"rehash preserves survivor identities");
  Need(r.RemovedNodeCount==0&&a.ToCompletedSnapshot().Count==9,"complete rehash exposes all live nodes");
  var retained=new RecordCachePublicationAttempt(8,128,1.0f,seed,E("i",null));Known(retained.RunOnce(),true,1);
  Need(retained.BucketCount==64&&retained.BucketCapacity==128,"adequate existing endpoint capacity retained");
  var doubled=new RecordCachePublicationAttempt(512,512,1.0f/512.0f,new[]{E("a",null)},E("b",null));Known(doubled.RunOnce(),true,1);
  Need(doubled.BucketCount==1024&&doubled.BucketCapacity==1024&&doubled.Count==2,"512 boundary doubles rather than multiplies by eight");
  Need(Ring(doubled.Inspect())&&!doubled.RequiresResolution,"nested maintenance at equality is known no-op");

  var survivor=new Payload();var oldTail=new Payload();var candidate=new Payload();
  var duplicateTail=new RecordCachePublicationAttempt(1,1,1.0f,new[]{E("a",survivor),E("a",oldTail)},E("b",candidate));
  var rr=duplicateTail.RunOnce();Known(rr,true,1);var rs=duplicateTail.Inspect();
  Need(duplicateTail.BucketCount==8&&duplicateTail.Count==2&&rr.RemovedNodeCount==1,"rehash removes duplicate old tail once");
  Need(Same(rs.CopyListOrder(),1,2)&&rs.CopyRemovedNodes()[3]&&!rs.CopyRemovedNodes()[1]&&Ring(rs),"saved-tail identity ends loop even when removed");
  Need(rs.CopyBucketFirst()[5]==1&&rs.CopyBucketFirst()[4]==2&&rs.CopyBucketLast()[4]==2,"processed bucket endpoints exclude removed duplicate");
  Need(Object.ReferenceEquals(duplicateTail.ToCompletedSnapshot().Find(B("a")).Entry.Table,survivor),"earlier processed duplicate survives");
  Need(survivor.Disposals==0&&oldTail.Disposals==0&&candidate.Disposals==0,"rehash removal does not dispose any Table");
 }
 static void StickyUnsupportedAtCaller(){
  var a=new RecordCachePublicationAttempt(8,8,1.0f/128.0f,new[]{E("a",new object())},E("i",new object()));
  var first=a.RunOnce();Unsupported(first,"RecordCacheRecursiveGrowthOutsideCoveredDomain");
  var s=a.Inspect();
  Need(a.Attempted&&a.RequiresResolution&&a.Count==2&&a.BucketCount==8&&a.BucketCapacity==8,"recursive guard is after publication prefix but before reset");
  Need(Same(s.CopyListOrder(),1,2)&&s.CopyBucketFirst()[4]==1&&s.CopyBucketLast()[4]==2&&Ring(s),"written first endpoint is not rolled back");
  Throws<InvalidOperationException>(()=>a.ToCompletedSnapshot(),"unresolved state cannot become cache input");
  var again=a.RunOnce();
  Need(Object.ReferenceEquals(first,again)&&again.MayHaveEffects&&again.RequiresResolution&&again.StopAddress=="0x18002eae9","caller gets same first stop and cumulative effects on retry");
  Need(a.Count==2&&Same(a.Inspect().CopyBucketFirst(),s.CopyBucketFirst())&&Same(a.Inspect().CopyListOrder(),s.CopyListOrder()),"retry performs no second count/link/reset");
  var budget=new RecordCachePublicationAttempt(4096,4096,1.0f/4096.0f,new[]{E("a",null)},E("b",null));
  var br=budget.RunOnce();Unsupported(br,"RecordCacheGrowthBucketBudgetOutsideCoveredDomain");
  Need(budget.Count==2&&budget.BucketCount==4096&&Ring(budget.Inspect()),"bucket budget leaves real prefix inspectable");
  Need(Object.ReferenceEquals(budget.RunOnce(),br)&&br.MayHaveEffects,"budget stop also sticks at caller");
  Throws<InvalidOperationException>(()=>budget.ToCompletedSnapshot(),"budget stop cannot continue as cache success");

  // The older diagnostic prefix remains safe to inspect directly. A replay's
  // local false does not erase its entry effects or the first unknown callee.
  var prefix=new RecordCachePublicationPrefixState(8,new[]{E("b",null),E("a",null)},E("i",null));
  var p1=prefix.Advance();var p2=prefix.Advance();
  Need(p1.PrefixMutated&&!p2.PrefixMutated&&p2.MayHaveEffects&&p2.RequiresResolution,"per-call no new mutation is not effect-free failure");
  Need(Object.ReferenceEquals(prefix.FirstResult,p1)&&p2.FirstStopAddress=="0x18002ea80"&&prefix.RequiresResolution,"first prefix stop remains available to consumer");
  Need(Same(prefix.Inspect().CopyListOrder(),2,1,3)&&prefix.Count==3,"legacy prefix replay preserves modified list");
  var dupPrefix=new RecordCachePublicationPrefixState(1,new[]{E("a",null)},E("a",null));
  var dp=dupPrefix.Advance();var dp2=dupPrefix.Advance();
  Need(!dp.PrefixMutated&&dp.MayHaveEffects&&dp.RequiresResolution&&dupPrefix.Count==2,"duplicate prefix still contains caller's linked candidate");
  Need(dp2.FirstStopAddress=="0x18002eb40"&&dp2.MayHaveEffects&&Object.ReferenceEquals(dupPrefix.FirstResult,dp),"duplicate stop not overwritten by replay result");
 }
 static void GuardsAndActualColdCallers(){
  var empty=new RecordCacheEntry[0];var candidate=E("q",null);
  Throws<ArgumentOutOfRangeException>(()=>new RecordCachePublicationAttempt(3,8,1,empty,candidate),"power-of-two bucket domain");
  Throws<ArgumentOutOfRangeException>(()=>new RecordCachePublicationAttempt(8,7,1,empty,candidate),"capacity below bucket count");
  Throws<ArgumentOutOfRangeException>(()=>new RecordCachePublicationAttempt(8,4097,1,empty,candidate),"capacity bound");
  Throws<ArgumentOutOfRangeException>(()=>new RecordCachePublicationAttempt(8,8,0,empty,candidate),"zero max load");
  Throws<ArgumentOutOfRangeException>(()=>new RecordCachePublicationAttempt(8,8,float.NaN,empty,candidate),"NaN max load");
  Throws<ArgumentOutOfRangeException>(()=>new RecordCachePublicationAttempt(8,8,float.PositiveInfinity,empty,candidate),"infinite max load");
  Throws<ArgumentException>(()=>new RecordCachePublicationAttempt(8,8,1,new[]{E("a",null),E("b",null),E("i",null)},candidate),"old seed contiguity");
  Throws<ArgumentNullException>(()=>new RecordCachePublicationAttempt(8,8,1,empty,null),"already-created candidate must be known");
  var arbitrary=new RecordCachePublicationAttempt(8,9,1,empty,candidate);
  Need(arbitrary.BucketCapacity==9,"capacity is vector extent rather than invented power-of-two constraint");
  var snap=arbitrary.Inspect();snap.CopyNextLinks()[0]=999;snap.CopyBucketFirst()[0]=999;snap.CopyRemovedNodes()[1]=true;
  Need(Ring(arbitrary.Inspect())&&!arbitrary.Inspect().CopyRemovedNodes()[1],"diagnostic arrays isolated");
  Throws<ArgumentOutOfRangeException>(()=>snap.EntryForNode(0),"sentinel not entry");

  // These are the real current outer callers. Missing node construction and
  // archive loading are not bypassed by the independent indexing helper.
  var depot=new RecordDepotState(new RegistrationGate(),new RecordCacheSnapshot(8,empty),9);int constructions=0,reads=0;
  var classes=new ClassInfoSnapshot(B("Own\0"),null,null,delegate{constructions++;return ObjectBaseConstructor.Create();});
  var name=new ObjectNameString(B("item"),15);
  var created=CreatePartialObject.Apply(depot,name,RecordNameLocale.KnownC,classes);
  Need(created.Status=="Unsupported"&&created.Reason=="RecordLoadUnsupported:UnimplementedRecordCacheMissLoader"&&created.Getter==null&&created.Factory==null&&constructions==0,"real Create stops before getter/factory on unresolved miss");
  var obj=ObjectBaseConstructor.Create();ObjectNameAssignment.Apply(obj,name);obj.Fields.Id2C=123;
  var pending=new RegistrationIndex(8,8,1,new[]{new RegistrationEntry(77,new object())});
  var ids=new RegistrationIndex(8,8,1,new RegistrationEntry[0]);var raw=new RegistrationIndex(8,8,1,new RegistrationEntry[0]);
  var manager=new ObjectRegistrationState(new RegistrationGate(),pending,ids,raw);
  var finished=FinishPartialObject.Apply(manager,depot,obj,77,RecordNameLocale.KnownC,delegate{reads++;});
  Need(finished.Status=="Unsupported"&&finished.Reason=="RecordLoadUnsupported:UnimplementedRecordCacheMissLoader"&&finished.Registration==null&&reads==0,"real Finish does not turn missing loader into successful Read");
  Need(obj.Fields.Id2C==123&&pending.Count==1&&ids.Count==0&&raw.Count==0&&depot.HitCount54==9&&!manager.Gate.IsHeldByCurrentThread&&!depot.Gate.IsHeldByCurrentThread,"real miss performed no publication or registration prefix");
 }
 static int Main(){
  try{
   NonGrowingAndDuplicate();SingleGrowth();StickyUnsupportedAtCaller();GuardsAndActualColdCallers();
   Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+assertions+",\"scenarios\":4,\"scope\":\"explicit already-linked candidate through bounded native indexing helper, sticky caller effects and real unresolved cold-caller guards; node construction and complete LoadFile remain unsupported\"}");
   return 0;
  }catch(Exception e){Console.Error.WriteLine(e);return 1;}
 }
}
