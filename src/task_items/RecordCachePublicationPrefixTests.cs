using System;
using GrimDynasty.TaskItems;
class RecordCachePublicationPrefixTests {
 static int assertions;
 static void Check(bool ok,string why){assertions++;if(!ok)throw new Exception(why);}
 static byte[] Key(string s){var b=new byte[s.Length];for(int i=0;i<s.Length;i++)b[i]=(byte)s[i];return b;}
 static RecordCacheEntry E(string s,object table){return new RecordCacheEntry(Key(s),table);}
 static bool Same(int[] a,params int[] b){if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
 static bool Links(RecordCachePublicationPrefixSnapshot s){
  int[] next=s.CopyNextLinks(),prev=s.CopyPreviousLinks(),order=s.CopyListOrder();
  if(next.Length!=s.Count+1||prev.Length!=s.Count+1||order.Length!=s.Count)return false;
  var seen=new bool[s.Count+1];seen[0]=true;int current=0;
  for(int i=0;i<order.Length;i++){
   int n=next[current];if(n<=0||n> s.Count||seen[n]||n!=order[i]||prev[n]!=current)return false;
   seen[n]=true;current=n;
  }
  if(next[current]!=0||prev[0]!=current)return false;
  for(int n=0;n<next.Length;n++)if(!seen[n]||prev[next[n]]!=n||next[prev[n]]!=n)return false;
  return true;
 }
 static void Unknown(RecordCachePublicationPrefixResult r,string reason,string address){
  Check(r.Status=="Unsupported"&&r.Reason==reason,"explicit unknown callee");
  Check(r.StopAddress==address,"exact stop address");
  Check(!r.Inserted.HasValue&&!r.ReturnedNodeId.HasValue,"native return fields not reached");
 }
 static void Throws<T>(Action a,string why) where T:Exception {bool caught=false;try{a();}catch(T){caught=true;}Check(caught,why);}
 static void UniquePrefixes(){
  var qb=new object();var old=new[]{E("b",new object()),E("a",new object()),E("i",new object())};
  var state=new RecordCachePublicationPrefixState(8,old,E("q",qb));
  var before=state.Inspect();
  Check(state.Count==4&&state.BucketCount==8&&!state.AdvanceAttempted,"explicit prelinked entry");
  Check(before.CandidateNodeId==1&&Same(before.CopyListOrder(),1,2,3,4),"candidate at caller head");
  Check(before.CopyBucketFirst()[4]==3&&before.CopyBucketLast()[4]==4&&before.CopyBucketFirst()[5]==2,"endpoints exclude candidate");
  Check(Links(before),"entry backlinks");
  var r=state.Advance();Unknown(r,"UnimplementedRecordCachePostInsertMaintenance","0x18002ea80");
  Check(r.PrefixMutated&&r.ComparedNodes==2&&!r.MatchedNodeId.HasValue,"reverse exhausted both old nodes");
  var after=state.Inspect();
  Check(Same(after.CopyListOrder(),2,1,3,4),"move candidate before old bucket first");
  Check(after.CopyBucketFirst()[4]==1&&after.CopyBucketLast()[4]==4,"occupied last preserved");
  Check(after.CopyBucketFirst()[5]==2&&after.CopyBucketLast()[5]==2,"other bucket preserved");
  Check(after.Count==4&&Links(after),"no second count increment; links remain circular");
  Check(Object.ReferenceEquals(after.EntryForNode(1).Table,qb),"opaque candidate table retained");
  Check(Same(before.CopyListOrder(),1,2,3,4)&&before.CopyBucketFirst()[4]==3,"inspection snapshot remains stable");
  int[] copy=after.CopyNextLinks();copy[0]=999;
  int[] bucketCopy=after.CopyBucketFirst();bucketCopy[4]=999;
  Check(Links(state.Inspect())&&state.Inspect().CopyBucketFirst()[4]==1,"diagnostic arrays isolated");
  Check(state.AdvanceAttempted,"stop seals further mutation");
  var repeated=state.Advance();
  Check(repeated.Status=="Unsupported"&&repeated.Reason=="RecordCachePublicationPrefixAlreadyStopped"&&!repeated.PrefixMutated,"unknown operation cannot be retried as complete state");
  Check(!repeated.Inserted.HasValue&&!repeated.ReturnedNodeId.HasValue&&Same(state.Inspect().CopyListOrder(),2,1,3,4),"repeat retains prefix");

  var adjacent=new RecordCachePublicationPrefixState(8,new[]{E("a",null),E("i",null),E("b",null)},E("q",null));
  var ar=adjacent.Advance();Unknown(ar,"UnimplementedRecordCachePostInsertMaintenance","0x18002ea80");
  Check(Same(adjacent.Inspect().CopyListOrder(),1,2,3,4)&&Links(adjacent.Inspect()),"already adjacent candidate is not spliced twice");
  Check(adjacent.Inspect().CopyBucketFirst()[4]==1&&adjacent.Inspect().CopyBucketLast()[4]==3,"only occupied first changes");
  var emptyBucket=new RecordCachePublicationPrefixState(8,new[]{E("a",null),E("i",null),E("b",null)},E("c",null));
  var er=emptyBucket.Advance();Unknown(er,"UnimplementedRecordCachePostInsertMaintenance","0x18002ea80");
  var es=emptyBucket.Inspect();
  Check(er.ComparedNodes==0&&Same(es.CopyListOrder(),2,3,4,1),"empty bucket appends at global tail");
  Check(es.CopyBucketFirst()[2]==1&&es.CopyBucketLast()[2]==1&&es.CopyPreviousLinks()[0]==1&&Links(es),"empty bucket endpoints and sentinel previous");
  var empty=new RecordCachePublicationPrefixState(8,new RecordCacheEntry[0],E("",null));
  var zr=empty.Advance();Unknown(zr,"UnimplementedRecordCachePostInsertMaintenance","0x18002ea80");
  Check(Same(empty.Inspect().CopyListOrder(),1)&&empty.Count==1&&Links(empty.Inspect()),"empty cache remains one node");
  Check(empty.Inspect().CopyBucketFirst()[5]==1&&empty.Inspect().CopyBucketLast()[5]==1,"empty key hashes full zero length");
 }
 static void DuplicateBoundary(){
  object first=new object(),last=new object(),candidate=new object();
  var seed=new[]{E("a",first),E("i",null),E("a",last),E("b",null)};
  var state=new RecordCachePublicationPrefixState(8,seed,E("a",candidate));
  var before=state.Inspect();var r=state.Advance();
  Unknown(r,"UnimplementedRecordCacheDuplicateRemoval","0x18002eb40");
  Check(r.ComparedNodes==1&&r.MatchedNodeId==4&&!r.PrefixMutated,"reverse last matching node observed");
  var after=state.Inspect();
  Check(Same(after.CopyListOrder(),1,2,3,4,5)&&after.Count==5&&Links(after),"unknown duplicate helper does not remove candidate");
  Check(Same(before.CopyBucketFirst(),after.CopyBucketFirst())&&Same(before.CopyBucketLast(),after.CopyBucketLast()),"duplicate endpoints untouched");
  Check(Object.ReferenceEquals(after.EntryForNode(1).Table,candidate)&&Object.ReferenceEquals(after.EntryForNode(4).Table,last),"candidate and old table both preserved");
  Check(Object.ReferenceEquals(new RecordCacheSnapshot(8,seed).Find(Key("a")).Entry.Table,first),"lookup's forward first match differs from publication reverse observation");
  var bytes=new byte[]{0x80,0,0xff};
  var full=new RecordCachePublicationPrefixState(1,new[]{new RecordCacheEntry(bytes,last),new RecordCacheEntry(new byte[]{0x80},first)},new RecordCacheEntry(bytes,candidate));
  var fr=full.Advance();Unknown(fr,"UnimplementedRecordCacheDuplicateRemoval","0x18002eb40");
  Check(fr.ComparedNodes==2&&fr.MatchedNodeId==2,"full length and embedded NUL survive reverse comparison");
  bytes[0]=0;
  Check(full.Inspect().EntryForNode(1).CopyKey()[0]==0x80&&full.Inspect().EntryForNode(1).CopyKey()[2]==0xff,"stored key owns raw high bytes");
  var prefix=new RecordCachePublicationPrefixState(1,new[]{E("a",first)},new RecordCacheEntry(new byte[]{0x61,0},candidate));
  var pr=prefix.Advance();Unknown(pr,"UnimplementedRecordCachePostInsertMaintenance","0x18002ea80");
  Check(pr.ComparedNodes==1&&!pr.MatchedNodeId.HasValue,"equal byte prefix with different length is unique");
  var nul=new RecordCachePublicationPrefixState(1,new[]{E("",null)},E("",null));
  var nr=nul.Advance();Unknown(nr,"UnimplementedRecordCacheDuplicateRemoval","0x18002eb40");
  Check(nr.ComparedNodes==1&&nr.MatchedNodeId==2&&nul.Count==2,"zero length duplicate has no synthetic removal");
 }
 static void CoverageGuards(){
  var candidate=E("q",null);
  Throws<ArgumentOutOfRangeException>(()=>new RecordCachePublicationPrefixState(0,new RecordCacheEntry[0],candidate),"zero buckets");
  Throws<ArgumentOutOfRangeException>(()=>new RecordCachePublicationPrefixState(3,new RecordCacheEntry[0],candidate),"non-power buckets");
  Throws<ArgumentOutOfRangeException>(()=>new RecordCachePublicationPrefixState(8192,new RecordCacheEntry[0],candidate),"bucket budget");
  Throws<ArgumentNullException>(()=>new RecordCachePublicationPrefixState(8,null,candidate),"unknown old list");
  Throws<ArgumentNullException>(()=>new RecordCachePublicationPrefixState(8,new RecordCacheEntry[0],null),"unknown already-created candidate");
  Throws<ArgumentException>(()=>new RecordCachePublicationPrefixState(8,new RecordCacheEntry[]{null},candidate),"unknown indexed node");
  Throws<ArgumentException>(()=>new RecordCachePublicationPrefixState(8,new[]{E("a",null),E("b",null),E("i",null)},candidate),"only old list must have contiguous buckets");
  var seed=new[]{E("a",null)};var kept=seed[0];var s=new RecordCachePublicationPrefixState(8,seed,candidate);seed[0]=E("b",null);
  Check(Object.ReferenceEquals(s.Inspect().EntryForNode(2),kept),"input array isolated");
  Throws<ArgumentOutOfRangeException>(()=>s.Inspect().EntryForNode(0),"sentinel has no entry");
  Throws<ArgumentOutOfRangeException>(()=>s.Inspect().EntryForNode(3),"unknown node id");
  Check(new RecordCachePublicationPrefixState(4096,new RecordCacheEntry[0],candidate).BucketCount==4096,"maximum bucket bound");
  var many=new RecordCacheEntry[4095];for(int i=0;i<many.Length;i++)many[i]=E("",null);
  Check(new RecordCachePublicationPrefixState(1,many,E("",null)).Count==4096,"node budget includes candidate");
  Throws<ArgumentOutOfRangeException>(()=>new RecordCachePublicationPrefixState(1,new RecordCacheEntry[4096],candidate),"candidate cannot exceed node budget");
  var large=new RecordCacheEntry(new byte[65536],null);var fifteen=new RecordCacheEntry[15];for(int i=0;i<fifteen.Length;i++)fifteen[i]=large;
  Check(new RecordCachePublicationPrefixState(1,fifteen,large).Count==16,"one MiB total key budget accepted");
  var sixteen=new RecordCacheEntry[16];for(int i=0;i<sixteen.Length;i++)sixteen[i]=large;
  Throws<ArgumentOutOfRangeException>(()=>new RecordCachePublicationPrefixState(1,sixteen,E("a",null)),"total key budget includes candidate");
 }
 static int Main(){
  try{
   UniquePrefixes();DuplicateBoundary();CoverageGuards();
   Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+assertions+",\"scenarios\":3,\"scope\":\"already-linked cache indexing prefix; native publication return remains unknown\"}");
   return 0;
  }catch(Exception e){Console.Error.WriteLine(e);return 1;}
 }
}
