using System;
using GrimDynasty.TaskItems;
class RecordCacheNodeConstructionTests {
 static int assertions,scenarios;
 static void Need(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static byte[] B(string value){var bytes=new byte[value.Length];for(int i=0;i<bytes.Length;i++)bytes[i]=(byte)value[i];return bytes;}
 static byte[] Bytes(int length){var bytes=new byte[length];for(int i=0;i<length;i++)bytes[i]=(byte)(i%256);if(length>0)bytes[length-1]=0xff;return bytes;}
 static bool Same(byte[] a,byte[] b){if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
 sealed class Payload:IDisposable {public int Disposals;public void Dispose(){Disposals++;}}
 static bool EmptySource(RecordCacheNodePair pair){
  return pair.Name.Length==0&&pair.Name.Capacity==15&&pair.Name.FirstByte==0&&
   pair.Name.TerminatorByte==0&&pair.Name.HeapStorageIdentity==null&&pair.Name.CopyBytes().Length==0;
 }
 static void ShortCopies(){
  Need(RecordCacheConstructedNode.NativeLayoutBytes==56,"observed node allocation is 0x38 bytes");
  foreach(int length in new[]{0,1,15}){
   byte[] bytes=Bytes(length);var table=new Payload();var pair=new RecordCacheNodePair(bytes,15,table);
   var result=new RecordCacheNodeConstructionAttempt(pair,null,new object()).RunOnce();
   Need(result.Status=="Known"&&result.Reason==null&&result.SourceNameCleared&&!result.HeapStorageTransferred&&result.MayHaveEffects&&result.LastCompletedStage=="NameMovedAndTableStored","normal inline move completes at length "+length);
   Need(!Object.ReferenceEquals(result.Node.Name,pair.Name)&&result.Node.Name.Length==(ulong)length&&result.Node.Name.Capacity==15&&result.Node.Name.HeapStorageIdentity==null&&result.Node.Name.TerminatorByte==0&&Same(result.Node.Name.CopyBytes(),bytes),"fresh inline destination retains full payload including NUL/high bytes at length "+length);
   Need(EmptySource(pair)&&Object.ReferenceEquals(pair.Table,table)&&Object.ReferenceEquals(result.Node.Table,table)&&table.Disposals==0,"inline source reset does not clear or dispose Table at length "+length);
   byte[] copy=result.Node.Name.CopyBytes();if(copy.Length>0)copy[0]^=0xff;
   Need(Same(result.Node.Name.CopyBytes(),bytes)&&EmptySource(pair),"inline destination survives source reset and mutable output copy at length "+length);
  }
 }
 static void HeapTransfers(){
  int[] lengths={0,1,16,33};ulong[] capacities={16,16,16,63};
  for(int i=0;i<lengths.Length;i++){
   byte[] bytes=Bytes(lengths[i]);var table=new Payload();var pair=new RecordCacheNodePair(bytes,capacities[i],table);
   object storage=pair.Name.HeapStorageIdentity;
   var result=new RecordCacheNodeConstructionAttempt(pair,null,null).RunOnce();
   Need(storage!=null&&result.Status=="Known"&&result.SourceNameCleared&&result.HeapStorageTransferred&&result.MayHaveEffects,"capacity selects heap path even for empty or short payload "+i);
   Need(Object.ReferenceEquals(result.Node.Name.HeapStorageIdentity,storage)&&result.Node.Name.Length==(ulong)lengths[i]&&result.Node.Name.Capacity==capacities[i]&&Same(result.Node.Name.CopyBytes(),bytes)&&result.Node.Name.TerminatorByte==0,"heap identity, complete length and original capacity transfer without growth "+i);
   Need(EmptySource(pair)&&Object.ReferenceEquals(pair.Table,table)&&Object.ReferenceEquals(result.Node.Table,table)&&table.Disposals==0,"heap source relinquishes storage while Table remains opaque "+i);
  }
 }
 static void IsolationAndSingleAttempt(){
  byte[] input={0x41,0,0x80,0xff},expected=(byte[])input.Clone();
  var table=new Payload();var pair=new RecordCacheNodePair(input,31,table);object storage=pair.Name.HeapStorageIdentity;
  input[0]=0x42;var sourceCopy=pair.Name.CopyBytes();sourceCopy[1]=0x43;
  Need(Same(pair.Name.CopyBytes(),expected),"supplied pair clones input and returns isolated byte copies");
  var attempt=new RecordCacheNodeConstructionAttempt(pair,new object(),new object());
  Need(!attempt.Attempted&&pair.Name.Length==4,"creating an attempt does not consume the pair");
  var result=attempt.RunOnce();var node=result.Node;
  Need(result.Status=="Known"&&attempt.Attempted&&Object.ReferenceEquals(node.Name.HeapStorageIdentity,storage),"first attempt transfers the supplied heap identity");
  Need(Object.ReferenceEquals(attempt.RunOnce(),result)&&Object.ReferenceEquals(attempt.RunOnce().Node,node)&&Object.ReferenceEquals(node.Name.HeapStorageIdentity,storage)&&EmptySource(pair),"RunOnce returns the same successful result without consuming the cleared source again");
  var nodeCopy=node.Name.CopyBytes();nodeCopy[2]=0;
  var entry=node.ToEntrySnapshot();var keyCopy=entry.CopyKey();keyCopy[3]=0;
  Need(Same(node.Name.CopyBytes(),expected)&&Same(entry.CopyKey(),expected)&&Object.ReferenceEquals(entry.Table,table)&&table.Disposals==0,"node and immutable entry projection do not expose writable name storage");
 }
 static void LinkArguments(){
  var previous=new object();var self=new RecordCacheNodeConstructionAttempt(new RecordCacheNodePair(B("a"),15,null),null,previous).RunOnce();
  Need(self.Status=="Known"&&Object.ReferenceEquals(self.Node.Next,self.Node)&&Object.ReferenceEquals(self.Node.Previous,self.Node)&&self.Node.Table==null,"null next overrides previous and creates two self links with a known null Table");
  var next=new object();var linked=new RecordCacheNodeConstructionAttempt(new RecordCacheNodePair(B("b"),15,null),next,previous).RunOnce();
  Need(linked.Status=="Known"&&Object.ReferenceEquals(linked.Node.Next,next)&&Object.ReferenceEquals(linked.Node.Previous,previous),"nonnull next preserves both supplied link identities");
  var nullPrevious=new RecordCacheNodeConstructionAttempt(new RecordCacheNodePair(B("c"),15,null),next,null).RunOnce();
  Need(nullPrevious.Status=="Known"&&Object.ReferenceEquals(nullPrevious.Node.Next,next)&&nullPrevious.Node.Previous==null,"nonnull next does not replace an explicitly null previous link");
 }
 static void RejectedPair(RecordCacheNodePair pair,string reason){
  byte[] before=pair.Name.CopyBytes();ulong capacity=pair.Name.Capacity;object identity=pair.Name.HeapStorageIdentity,table=pair.Table;
  var attempt=new RecordCacheNodeConstructionAttempt(pair,null,null);var result=attempt.RunOnce();
  Need(result.Status=="Unsupported"&&result.Reason==reason&&result.Node==null&&!result.SourceNameCleared&&!result.HeapStorageTransferred&&!result.MayHaveEffects&&result.LastCompletedStage=="BeforeEntry","coverage rejection precedes modeled node/source effects");
  Need(Same(pair.Name.CopyBytes(),before)&&pair.Name.Capacity==capacity&&Object.ReferenceEquals(pair.Name.HeapStorageIdentity,identity)&&Object.ReferenceEquals(pair.Table,table),"rejected source keeps bytes, capacity, ownership and Table");
  Need(attempt.Attempted&&Object.ReferenceEquals(attempt.RunOnce(),result)&&Same(pair.Name.CopyBytes(),before),"rejected attempt caches its result without consuming source");
 }
 static void CoverageBoundaries(){
  byte[] maximum=Bytes(65536);var pair=new RecordCacheNodePair(maximum,131072,null);object storage=pair.Name.HeapStorageIdentity;
  var result=new RecordCacheNodeConstructionAttempt(pair,null,null).RunOnce();
  Need(result.Status=="Known"&&result.HeapStorageTransferred&&result.Node.Name.Length==65536&&result.Node.Name.Capacity==131072&&Object.ReferenceEquals(result.Node.Name.HeapStorageIdentity,storage),"inclusive name and capacity budgets support normal ownership transfer");
  Need(Same(result.Node.Name.CopyBytes(),maximum)&&EmptySource(pair),"maximum-size payload remains complete after source reset");
  RejectedPair(new RecordCacheNodePair(Bytes(65537),65537,new object()),"RecordCacheNodeNameLengthOutsideCoveredDomain");
  RejectedPair(new RecordCacheNodePair(B("a"),131073,new object()),"RecordCacheNodeNameCapacityOutsideCoveredDomain");
  var missing=new RecordCacheNodeConstructionAttempt(null,null,null);var absent=missing.RunOnce();
  Need(absent.Status=="Unsupported"&&absent.Reason=="UnknownRecordCacheNodePair"&&absent.Node==null&&!absent.MayHaveEffects&&!absent.SourceNameCleared&&!absent.HeapStorageTransferred,"unknown pair cannot become a known empty node");
  Need(missing.Attempted&&Object.ReferenceEquals(missing.RunOnce(),absent),"unknown-pair result is also stable across replay");
 }
 static void ExplicitPrelinkedPublicationFixture(){
  var table=new Payload();var source=new RecordCacheNodePair(B("q"),16,table);var emptyCache=new RecordCacheSnapshot(8,new RecordCacheEntry[0]);
  var construction=new RecordCacheNodeConstructionAttempt(source,null,null).RunOnce();
  var entry=construction.Node.ToEntrySnapshot();
  Need(construction.Status=="Known"&&emptyCache.Count==0&&Object.ReferenceEquals(construction.Node.Next,construction.Node)&&Object.ReferenceEquals(entry.Table,table),"construction and projection alone do not link or count the node in an existing cache");
  // This constructor supplies a DIFFERENT, explicit already-linked and
  // already-counted entry graph. It does not claim construction performed
  // the native caller's neighboring-link writes or count increment.
  var publication=new RecordCachePublicationAttempt(8,8,1.0f,new RecordCacheEntry[0],entry);
  Need(publication.Count==1&&publication.RequiresResolution&&Object.ReferenceEquals(publication.Inspect().EntryForNode(1),entry),"publication fixture explicitly owns its supplied prelinked candidate");
  var published=publication.RunOnce();
  Need(published.Status=="Known"&&published.Inserted==true&&published.ReturnedNodeId==1&&publication.Count==1&&!publication.RequiresResolution,"bounded publication completes the separately supplied entry graph");
  var cache=publication.ToCompletedSnapshot();var depot=new RecordDepotState(new RegistrationGate(),cache,4);
  var loaded=RecordLoadFile.Apply(depot,new ObjectNameString(B("Q"),15),RecordNameLocale.KnownC);
  Need(loaded.Status=="Known"&&loaded.CacheHit&&Object.ReferenceEquals(loaded.Table,table)&&Object.ReferenceEquals(loaded.Lookup.Entry,entry)&&depot.HitCount54==5&&loaded.LockReleased&&!depot.Gate.IsHeldByCurrentThread,"explicit completed snapshot feeds a real normalized LoadFile hit");
  Need(EmptySource(source)&&Object.ReferenceEquals(source.Table,table)&&table.Disposals==0&&emptyCache.Count==0,"subsequent publication neither consumes source twice nor mutates the original empty snapshot");
  scenarios++;
 }
 static void OriginalEmptyCacheStillMisses(){
  var table=new Payload();var pair=new RecordCacheNodePair(B("item"),31,table);
  var construction=new RecordCacheNodeConstructionAttempt(pair,null,null).RunOnce();
  var entry=construction.Node.ToEntrySnapshot();
  var depot=new RecordDepotState(new RegistrationGate(),new RecordCacheSnapshot(8,new RecordCacheEntry[0]),7);
  var loaded=RecordLoadFile.Apply(depot,new ObjectNameString(B("item"),15),RecordNameLocale.KnownC);
  Need(construction.Status=="Known"&&Same(entry.CopyKey(),B("item"))&&Object.ReferenceEquals(entry.Table,table),"independent node construction has a valid explicit snapshot projection");
  Need(loaded.Status=="Unsupported"&&loaded.Reason=="UnimplementedRecordCacheMissLoader"&&loaded.FailureStage=="CacheMiss"&&loaded.Lookup!=null&&!loaded.Lookup.Found&&!loaded.CacheHit&&loaded.Table==null,"constructed node is not an implicit implementation of native LoadFile miss");
  Need(depot.Cache.Count==0&&depot.HitCount54==7&&!loaded.MayHaveEffects&&loaded.LockAcquired&&loaded.LockReleased&&!depot.Gate.IsHeldByCurrentThread&&table.Disposals==0,"real miss retains its count and releases its gate without publication");
  scenarios++;
 }
 static int Main(){
  try{
   ShortCopies();HeapTransfers();IsolationAndSingleAttempt();LinkArguments();CoverageBoundaries();
   ExplicitPrelinkedPublicationFixture();OriginalEmptyCacheStillMisses();
   Console.WriteLine("{\"status\":\"passed\",\"fixed_assertions\":"+assertions+",\"scenarios\":"+scenarios+",\"scope\":\"independent bounded normal-success 05DEB0 node construction and string ownership transfer; explicit prelinked publication fixture and real unresolved LoadFile miss; no native OOM or original execution\"}");
   return 0;
  }catch(Exception e){Console.Error.WriteLine(e);return 1;}
 }
}
