using System;
using System.Collections.Generic;

namespace GrimDynasty.ReverseClient.Actions {
 public enum EngineAnimationNameLocale { Unknown, KnownC }
 // One mutable 24-byte logical record shared by cache, idle list and resource vector.
 public sealed class EngineAnimationResourcePair {
  public readonly Field<uint> Count00=new Field<uint>(),Upper04=new Field<uint>();
  public readonly Field<EngineGraphicsResourceState> Resource08=new Field<EngineGraphicsResourceState>();
  public readonly Field<uint> Index10=new Field<uint>(),UnknownTail14=new Field<uint>();
 }
 public sealed class EngineAnimationResourceCacheNode {
  public EngineResourceNameInput Key{get;private set;}
  internal readonly EngineResourceStringState Name10=new EngineResourceStringState();
  public readonly Field<EngineAnimationResourcePair> Pair30=new Field<EngineAnimationResourcePair>();
  public EngineAnimationResourceCacheNode(byte[] key,Field<EngineAnimationResourcePair> pair){if(pair==null)throw new ArgumentNullException("pair");Key=new EngineResourceNameInput(key);Pair30.CopyFrom(pair.Snapshot());}
  internal EngineAnimationResourceCacheNode(){}
  internal void BindCopiedKey(EngineResourceNameInput key){Key=key;}
 }
 // Explicit complete OWN normal storage, not a parser for native pointers. Per-bucket
 // lists preserve native traversal order and first duplicate identity without a raw graph.
 public sealed class EngineAnimationResourceCache {
  readonly List<EngineAnimationResourceCacheNode>[] buckets;
  public readonly object SentinelIdentity=new object();
  public int BucketCount{get{return buckets.Length;}}
  public ulong Mask{get{return (ulong)buckets.Length-1;}}
  public int Count{get;private set;}
  public EngineAnimationResourceCache(EngineAnimationResourceCacheNode[] orderedEntries){
   if(orderedEntries==null)throw new ArgumentNullException("orderedEntries");if(orderedEntries.Length>8)throw new ArgumentOutOfRangeException("orderedEntries","Initial eight-bucket max-load1 normal profile admits at most eight seeds");
   buckets=new List<EngineAnimationResourceCacheNode>[8];for(int n=0;n<8;n++)buckets[n]=new List<EngineAnimationResourceCacheNode>(9);
   var unique=new HashSet<EngineAnimationResourceCacheNode>();
   foreach(var entry in orderedEntries){if(entry==null||entry.Key==null||!unique.Add(entry))throw new ArgumentException("Each supplied cache node must have a distinct initialized identity");if(entry.Key.Length>EngineGraphicsResourceConstruction.MaximumNameBytes)throw new ArgumentOutOfRangeException("orderedEntries");buckets[(int)(Hash(entry.Key)&Mask)].Add(entry);Count++;}
  }
  internal static ulong Hash(EngineResourceNameInput name){ulong h=0xcbf29ce484222325UL;for(int n=0;n<name.Length;n++)h=unchecked((h^name.Byte(n))*0x100000001b3UL);return h;}
  internal EngineAnimationResourceCacheNode Find(EngineResourceNameInput key,ulong hash,EngineAnimationResourceLoadInvocation c){
   c.Bucket=(int)(hash&Mask);foreach(var node in buckets[c.Bucket]){c.Visited++;if(node.Key.Length!=key.Length)continue;bool same=true;for(int n=0;n<key.Length;n++)if(node.Key.Byte(n)!=key.Byte(n)){same=false;break;}if(same)return node;}return null;
  }
  internal EngineAnimationResourceCacheNode FindLast(EngineResourceNameInput key,ulong hash,EngineAnimationResourceLoadInvocation c){
   var bucket=buckets[(int)(hash&Mask)];for(int i=bucket.Count-1;i>=0;i--){var node=bucket[i];c.InsertionVisited++;if(node.Key.Length!=key.Length)continue;bool same=true;for(int n=0;n<key.Length;n++)if(node.Key.Byte(n)!=key.Byte(n)){same=false;break;}if(same)return node;}return null;
  }
  internal void IncrementCount(){Count++;}
  internal void LinkAtBucketStart(EngineAnimationResourceCacheNode node,ulong hash){buckets[(int)(hash&Mask)].Insert(0,node);}
 }
 public sealed class EngineAnimationResourceManagerState {
  public readonly object Identity;
  public readonly Field<ulong> Virtual00=new Field<ulong>();
  public readonly Field<object> Loader38=new Field<object>();
  public readonly Field<EngineAnimationResourceCache> Cache40=new Field<EngineAnimationResourceCache>();
  public readonly Field<LinkedList<EngineAnimationResourcePair>> Idle98=new Field<LinkedList<EngineAnimationResourcePair>>();
  public readonly Field<ulong> IdleCountA0=new Field<ulong>();
  public readonly Field<EngineResourceOwnMonitor> SynchronizationC0=new Field<EngineResourceOwnMonitor>();
  public readonly Field<List<EngineAnimationResourcePair>> ResourceVector80=new Field<List<EngineAnimationResourcePair>>();
  // Execution context borrows the existing global cell; this is not a native manager member.
  public readonly Field<WorldEngine> EngineGlobal;
  public bool RequiresResolution{get;internal set;}
  EngineAnimationResourceManagerState(Field<WorldEngine> engine){Identity=this;EngineGlobal=engine;}
  public static EngineAnimationResourceManagerState CreateOwnInitialized(Field<object> loader,Field<WorldEngine> engine,EngineAnimationResourceCacheNode[] cache,EngineAnimationResourcePair[] idle){
   if(loader==null||engine==null||idle==null)throw new ArgumentNullException("OWN manager initialization inputs");
   var state=new EngineAnimationResourceManagerState(engine);state.Virtual00.Write(0x18033f478UL);state.Loader38.CopyFrom(loader.Snapshot());state.Cache40.Write(new EngineAnimationResourceCache(cache));
   var list=new LinkedList<EngineAnimationResourcePair>();foreach(var pair in idle){if(pair==null)throw new ArgumentException("Known OWN idle pair identity required");list.AddLast(pair);}state.Idle98.Write(list);state.IdleCountA0.Write((ulong)idle.Length);
   var monitor=new EngineResourceOwnMonitor();monitor.Initialize();state.SynchronizationC0.Write(monitor);state.ResourceVector80.Write(new List<EngineAnimationResourcePair>());return state;
  }
 }
 public sealed class EngineAnimationResourceLoadInvocation {
  public readonly Invocation Native=new Invocation();
  public readonly EngineResourceStringState LocalName=new EngineResourceStringState(),InsertionName=new EngineResourceStringState();
  public readonly List<int> KeyWrittenIndices=new List<int>();
  public readonly List<uint> Stages=new List<uint>();
  public readonly Field<ulong> CapturedFactoryTable=new Field<ulong>();
  public readonly Field<object> CapturedLoader=new Field<object>();
  public readonly Field<EngineGraphicsResourceState> ReturnedResourceReference=new Field<EngineGraphicsResourceState>();
  public EngineGraphicsResourceConstructionInvocation Construction;
  public EngineAnimationResourceCacheNode FoundNode,InsertionMapNode,AllocatedNode;
  public readonly Field<EngineAnimationResourcePair> InsertionPayload20=new Field<EngineAnimationResourcePair>();
  public EngineAnimationResourcePair FirstPair,CallerPair,AllocatedPair;
  public LinkedListNode<EngineAnimationResourcePair> RemovedIdleNode;
  public EngineResourceNameInput ProcessedName;
  public ulong QueryHash;
  public int Bucket,Visited,InsertionVisited;
  public bool LockAcquired,NativeNormalUnlock,OwnSafetyUnlock,NameCopied,KeyProcessed,LookupReturned,Cold,FactoryReturned,LoaderCopied;
  public bool IdleUnlinked,IdleCountDecremented,OwnIdleNodeReleased,CountIncremented,PairFirst16Copied,PairTail8Copied,InsertionNameCopied,InsertionEntered,InsertionReturned,Returned;
  public bool LocalNameNormalRelease,InsertionNameNormalRelease;
  public bool InsertionHelperReturned,InsertionHelperInserted,IndexWritten,VectorAppended,MapPublished,MapMutationStarted,MapCountIncremented,NodeConstructorReturned;
  public int RequestedGrowthBuckets;
  public int NativePairAllocationBytes;
 }
 // Saved03AC50/03C8E0/27B3C0/03C480. KnownC is a supplied precondition, never a
 // discovered game locale. There are no decoder/play callbacks or configurable factories.
 // shortcut: stable exclusive complete OWN storage, <=65536-byte disjoint names and
 // normal managed allocation/free/Monitor semantics; native allocator/EH, malformed raw
 // graphs and concurrent mutation require separate evidence and remain Unsupported.
 public static class EngineAnimationResourceManager {
  static uint At(EngineAnimationResourceLoadInvocation c,uint at){c.Native.At=at;c.Stages.Add(at);return at;}
  static T Need<T>(T value,uint at,string why)where T:class{if(value==null)throw new MissingContract(at,why);return value;}
  static void Empty(EngineResourceStringState s,EngineAnimationResourceLoadInvocation c,uint cap,uint length,uint first){At(c,cap);s.Capacity.Write(15);At(c,length);s.Length.Write(0);At(c,first);s.Inline.Byte(0).Write(0);s.Heap.Forget();}
  static void CopyFresh(EngineResourceStringState destination,Field<EngineResourceNameInput> input,EngineAnimationResourceLoadInvocation c){
   At(c,0x023195);var source=Need(input.Read(c.Native.At),c.Native.At,"Name pointer is known null");if(source.Length>EngineGraphicsResourceConstruction.MaximumNameBytes)throw new MissingContract(c.Native.At,"Name exceeds OWN complete-byte coverage");
   EngineResourceByteStorage target=destination.Inline;int length=source.Length;
   if(length>15){ulong capacity=(ulong)length|15UL;At(c,0x022fe7);target=new EngineResourceByteStorage((int)capacity+1);At(c,0x023044);destination.Capacity.Write(15);At(c,0x02304c);destination.Length.Write(0);At(c,0x023063);destination.Inline.Byte(0).Write(0);At(c,0x023066);destination.PublishHeap(target);At(c,0x023069);destination.Capacity.Write(capacity);At(c,0x02306d);destination.Length.Write(0);At(c,0x02307b);target.Byte(0).Write(0);}
   if(length==0){At(c,0x02324b);destination.Length.Write(0);At(c,0x02325c);destination.Inline.Byte(0).Write(0);}
   else{At(c,0x023270);for(int n=0;n<length;n++)target.Byte(n).Write(source.Byte(n));At(c,0x02327a);destination.Length.Write((ulong)length);At(c,0x023288);target.Byte(length).Write(0);}
   At(c,0x0232a4);
  }
  static EngineResourceNameInput Snapshot(EngineResourceStringState name,uint at){int length=checked((int)name.Length.Read(at));var bytes=new byte[length];for(int n=0;n<length;n++)bytes[n]=name.ReadByte(n,at);return new EngineResourceNameInput(bytes);}
  static void Canonicalize(EngineResourceStringState name,EngineAnimationNameLocale locale,EngineAnimationResourceLoadInvocation c){
   At(c,0x27b3cf);ulong length=name.Length.Read(c.Native.At);uint index=0;
   while(index<length){At(c,0x27b3e5);ulong capacity=name.Capacity.Read(c.Native.At);At(c,0x27b3f7);byte value=name.ReadByte((int)index,c.Native.At);var storage=capacity<16?name.Inline:Need(name.Heap.Read(c.Native.At),c.Native.At,"Null key heap");
    if(value==0x5c){At(c,0x27b443);storage.Byte((int)index).Write(0x2f);}
    else{At(c,capacity<16?0x27b42au:0x27b418u);if(value>=0x80)throw new MissingContract(c.Native.At,"Signed high-byte tolower input is outside covered CRT domain");if(locale!=EngineAnimationNameLocale.KnownC)throw new MissingContract(c.Native.At,"Runtime tolower locale is not known C");byte lower=value>=0x41&&value<=0x5a?(byte)(value+0x20):value;At(c,capacity<16?0x27b430u:0x27b41eu);storage.Byte((int)index).Write(lower);}
    c.KeyWrittenIndices.Add((int)index);index=unchecked(index+1);At(c,0x27b44b);length=name.Length.Read(c.Native.At);
   }At(c,0x27b465);
  }
  static void Warm(EngineAnimationResourceManagerState manager,EngineAnimationResourceCacheNode node,EngineAnimationResourceLoadInvocation c){
   At(c,0x03acdc);var first=Need(node.Pair30.Read(c.Native.At),c.Native.At,"Null cache pair at count comparison");c.FirstPair=first;uint count=first.Count00.Read(c.Native.At);
   if(count==0){At(c,0x03ace8);var idle=Need(manager.Idle98.Read(c.Native.At),c.Native.At,"Null idle-list head");var current=idle.First;
    while(current!=null&&!Object.ReferenceEquals(current.Value,first)){At(c,0x03ad00);current=current.Next;}
    if(current!=null){c.RemovedIdleNode=current;At(c,0x03ad1e);idle.Remove(current);c.IdleUnlinked=true;At(c,0x03ad28);
     At(c,0x03ad2c);manager.IdleCountA0.Write(unchecked(manager.IdleCountA0.Read(c.Native.At)-1));c.IdleCountDecremented=true;At(c,0x03ad33);c.OwnIdleNodeReleased=true;
    }
   }
   At(c,0x03ad3c);var pair=Need(node.Pair30.Read(c.Native.At),c.Native.At,"Null pair at count increment");pair.Count00.Write(unchecked(pair.Count00.Read(c.Native.At)+1));c.CountIncremented=true;
   At(c,0x03ad42);pair=Need(node.Pair30.Read(c.Native.At),c.Native.At,"Null pair at resource reference capture");c.ReturnedResourceReference.CopyFrom(pair.Resource08.Snapshot());
  }
  static void InsertKey03CA50(EngineAnimationResourceManagerState manager,EngineResourceNameInput key,EngineAnimationResourceLoadInvocation c){
   // Incoming R9 is overwritten by key capacity at03CA82; it is not an insertion flag.
   At(c,0x03cab0);ulong hash=EngineAnimationResourceCache.Hash(key);At(c,0x03cac4);var cache=Need(manager.Cache40.Read(c.Native.At),c.Native.At,"Unknown cache for insertion");
   c.InsertionMapNode=cache.FindLast(key,hash,c);
   if(c.InsertionMapNode!=null){At(c,0x03cb47);c.InsertionHelperInserted=false;At(c,0x03cb4a);c.InsertionHelperReturned=true;At(c,0x03cc64);return;}
   At(c,0x03cb76);At(c,0x02ef60);var node=new EngineAnimationResourceCacheNode();c.AllocatedNode=node;
   Empty(node.Name10,c,0x02ef80,0x02ef88,0x02ef9f);At(c,0x02efaf);CopyFresh(node.Name10,Field<EngineResourceNameInput>.Of(key),c);node.BindCopiedKey(key);
   At(c,0x02efb4);var payload=c.InsertionPayload20.Snapshot();At(c,0x02efb8);node.Pair30.CopyFrom(payload);At(c,0x02efce);c.NodeConstructorReturned=true;
   // Native count is incremented before link/endpoints. OWN lists reserve nine slots
   // up front, but a platform failure still retains the actual completed prefix.
   At(c,0x03cba0);c.MapMutationStarted=true;cache.IncrementCount();c.MapCountIncremented=true;
   At(c,0x03cba4);cache.LinkAtBucketStart(node,hash);At(c,0x03cbac);c.MapPublished=true;c.InsertionMapNode=node;
   // Equivalent normal bucket-local order: a miss is placed before this bucket's first
   // node. Raw link pointers and allocator/EH are outside this OWN storage profile.
   At(c,0x03cc3d);At(c,0x02eabf);
   if(cache.Count>8){c.RequestedGrowthBuckets=64;manager.RequiresResolution=true;At(c,0x02eae9);throw new MissingContract(c.Native.At,"Ninth unique node is published; native64-bucket growth/rehash is outside this bounded normal profile");}
   At(c,0x02eb3f);At(c,0x03cc43);c.InsertionHelperInserted=true;At(c,0x03cc46);c.InsertionHelperReturned=true;At(c,0x03cc64);
  }
  // Caller supplies exclusive ownership; Load03AC50 holds the actual OWN manager gate.
  internal static void Insert03C480(EngineAnimationResourceManagerState manager,EngineResourceNameInput key,EngineAnimationResourcePair caller,EngineAnimationResourceLoadInvocation c){
   if(manager.RequiresResolution)throw new MissingContract(0x03c480,"Prior partial manager publication requires resolution");
   try{
   c.InsertionEntered=true;At(c,0x03c4aa);c.NativePairAllocationBytes=24;c.AllocatedPair=new EngineAnimationResourcePair();
   var count=caller.Count00.Snapshot();var upper=caller.Upper04.Snapshot();var resource=caller.Resource08.Snapshot();At(c,0x03c4b5);c.AllocatedPair.Count00.CopyFrom(count);c.AllocatedPair.Upper04.CopyFrom(upper);c.AllocatedPair.Resource08.CopyFrom(resource);c.PairFirst16Copied=true;
   var index=caller.Index10.Snapshot();var tail=caller.UnknownTail14.Snapshot();At(c,0x03c4bd);c.AllocatedPair.Index10.CopyFrom(index);c.AllocatedPair.UnknownTail14.CopyFrom(tail);c.PairTail8Copied=true;
   Empty(c.InsertionName,c,0x03c4c7,0x03c4d0,0x03c4d9);At(c,0x03c4ed);CopyFresh(c.InsertionName,Field<EngineResourceNameInput>.Of(key),c);c.InsertionNameCopied=true;At(c,0x03c4f2);c.InsertionPayload20.Write(c.AllocatedPair);
   At(c,0x03c50b);InsertKey03CA50(manager,key,c);
   //03C480 ignores the helper's duplicate/inserted flag and still appends this NEW pair.
   At(c,0x03c511);if(c.InsertionName.Capacity.Read(c.Native.At)>=16)At(c,0x03c52a);c.InsertionNameNormalRelease=true;
   At(c,0x03c536);var vector=Need(manager.ResourceVector80.Read(c.Native.At),c.Native.At,"Unknown OWN resource vector");
   At(c,0x03c541);c.AllocatedPair.Index10.Write(unchecked((uint)vector.Count));c.IndexWritten=true;
   At(c,0x03c5af);try{vector.Add(c.AllocatedPair);}catch(Exception){if(c.MapMutationStarted)manager.RequiresResolution=true;throw;}c.VectorAppended=true;At(c,0x03c5b2);c.InsertionReturned=true;At(c,0x03c5cc);
   }catch(Exception){if(c.MapMutationStarted)manager.RequiresResolution=true;throw;}
  }
  public static Field<EngineGraphicsResourceState> Load03AC50(EngineAnimationResourceManagerState manager,Field<EngineResourceNameInput> requested,EngineAnimationNameLocale locale,EngineAnimationResourceLoadInvocation c){
   if(manager==null||requested==null||c==null)throw new ArgumentNullException("OWN load input");if(c.LockAcquired||c.Stages.Count!=0)throw new InvalidOperationException("Use one fresh invocation per attempted load");
   if(manager.RequiresResolution)throw new MissingContract(0x03ac50,"Prior OWN partial manager mutation requires resolution");
   EngineResourceOwnMonitor held=null;bool acquired=false;
   try{
    At(c,0x03ac7d);held=Need(manager.SynchronizationC0.Read(c.Native.At),c.Native.At,"Null manager lock");if(!held.Initialized)throw new MissingContract(c.Native.At,"Manager OWN synchronization is not initialized");held.Enter();acquired=c.LockAcquired=true;
    Empty(c.LocalName,c,0x03ac84,0x03ac8d,0x03ac96);At(c,0x03acaa);CopyFresh(c.LocalName,requested.Snapshot(),c);c.NameCopied=true;
    At(c,0x03acb5);Canonicalize(c.LocalName,locale,c);c.KeyProcessed=true;c.ProcessedName=Snapshot(c.LocalName,0x03c8f7);
    At(c,0x03c930);c.QueryHash=EngineAnimationResourceCache.Hash(c.ProcessedName);At(c,0x03c944);var cache=Need(manager.Cache40.Read(c.Native.At),c.Native.At,"Cache initialization is not known");c.FoundNode=cache.Find(c.ProcessedName,c.QueryHash,c);c.LookupReturned=true;At(c,0x03acd2);
    if(c.FoundNode!=null)Warm(manager,c.FoundNode,c);
    else{
     c.Cold=true;c.CallerPair=new EngineAnimationResourcePair();At(c,0x03ad48);c.CallerPair.Count00.Write(1);c.CallerPair.Upper04.Write(0);At(c,0x03ad51);c.CapturedFactoryTable.CopyFrom(manager.Virtual00.Snapshot());
     At(c,0x03ad5c);if(c.CapturedFactoryTable.Read(c.Native.At)!=0x18033f478UL)throw new MissingContract(c.Native.At,"Factory virtual+8 is not the observed AnimationResourceManager094650 target");
     c.Construction=new EngineGraphicsResourceConstructionInvocation();var resource=EngineGraphicsResourceConstruction.Create094650(Field<object>.Of(manager.Identity),Field<EngineResourceNameInput>.Of(c.ProcessedName),manager.EngineGlobal,c.Construction);c.FactoryReturned=true;
     At(c,0x03ad62);c.CallerPair.Resource08.Write(resource);c.ReturnedResourceReference.Write(resource);At(c,0x03ad67);c.CapturedLoader.CopyFrom(manager.Loader38.Snapshot());At(c,0x03ad6b);Need(resource,c.Native.At,"Null factory result at loader write").Loader28.CopyFrom(c.CapturedLoader);
     c.LoaderCopied=true;At(c,0x03ad7c);Insert03C480(manager,c.ProcessedName,c.CallerPair,c);
    }
    At(c,0x03ad82);if(c.LocalName.Capacity.Read(c.Native.At)>=16)At(c,0x03ad9b);c.LocalNameNormalRelease=true;Empty(c.LocalName,c,0x03ada0,0x03ada9,0x03adb2);
    At(c,0x03adbb);var exit=Need(manager.SynchronizationC0.Read(c.Native.At),c.Native.At,"Null current manager lock at normal leave");if(!Object.ReferenceEquals(exit,held))throw new MissingContract(c.Native.At,"Current lock differs from the OWN acquired monitor");exit.Exit();acquired=false;c.NativeNormalUnlock=true;At(c,0x03add5);c.Returned=true;return c.ReturnedResourceReference.Snapshot();
   }catch(MissingContract error){c.Native.At=error.Address;if(c.IdleUnlinked||c.CountIncremented||c.MapMutationStarted||c.VectorAppended)manager.RequiresResolution=true;throw;}
   catch(Exception error){if(c.IdleUnlinked||c.CountIncremented||c.MapMutationStarted||c.VectorAppended)manager.RequiresResolution=true;throw new MissingContract(c.Native.At,"OWN platform operation failed; native EH is not modeled",error);}
   finally{if(acquired){held.Exit();c.OwnSafetyUnlock=true;}}
  }
 }
}
