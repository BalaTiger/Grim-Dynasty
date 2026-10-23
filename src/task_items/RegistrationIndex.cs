using System;
namespace GrimDynasty.TaskItems {
 public sealed class RegistrationEntry {
  public readonly uint Key;
  public readonly object Value;
  public RegistrationEntry(uint key,object value){Key=key;Value=value;}
 }
 public sealed class RegistrationNode {
  public readonly uint Key;
  public readonly object Value;
  internal RegistrationNode Next,Previous;
  internal RegistrationNode(uint key,object value){Key=key;Value=value;}
 }
 public sealed class RegistrationIndexResult {
  public readonly bool Supported,MayHaveEffects;
  public readonly string Reason,Stage;
  // Null means the native helper's return value has not been reached. Node
  // can still identify a linked prefix node when post-link maintenance stops.
  public readonly bool? Inserted;
  public readonly ulong? RemovedCount;
  public readonly RegistrationNode Node;
  public readonly object Value;
  internal RegistrationIndexResult(string reason,string stage,bool effects,bool? inserted,ulong? removed,RegistrationNode node){
   Supported=reason==null;Reason=reason;Stage=stage;MayHaveEffects=effects;
   Inserted=inserted;RemovedCount=removed;Node=node;Value=node==null?null:node.Value;
  }
 }
 // A model of explicit, caller-supplied snapshots, not native memory. It uses
 // one sentinel list and two endpoints per bucket; payloads are opaque objects.
 // The caller owns synchronization. No ObjectIdAllocator state is connected.
 // Coverage assumes successful native allocation/release contracts. Allocator
 // internals and their side effects are not reconstructed; payloads are never
 // disposed by this metadata model. It does not establish native registration.
 public sealed class RegistrationIndex {
  public const int MaximumBuckets=4096,MaximumNodes=65536;
  readonly RegistrationNode head;
  RegistrationNode[] first,last;
  int bucketCapacity;
  readonly float maxLoadFactor;
  int bucketCount,count;
  bool stateComplete=true;
  public ulong Count {get{return (ulong)count;}}
  public int BucketCount {get{return bucketCount;}}
  public int BucketCapacity {get{return bucketCapacity;}}
  public float MaxLoadFactor {get{return maxLoadFactor;}}
  // False means a native operation's observed prefix has run, but its normal
  // return is unknown. No subsequent mutation is accepted for that state.
  public bool StateComplete {get{return stateComplete;}}
  public RegistrationIndex(int bucketCount,int bucketCapacity,float maxLoadFactor,RegistrationEntry[] seed){
   if(bucketCount<1||bucketCount>MaximumBuckets||(bucketCount&(bucketCount-1))!=0)throw new ArgumentOutOfRangeException("bucketCount");
   if(bucketCapacity<bucketCount||bucketCapacity>MaximumBuckets)throw new ArgumentOutOfRangeException("bucketCapacity");
   if(float.IsNaN(maxLoadFactor)||float.IsInfinity(maxLoadFactor)||maxLoadFactor<=0)throw new ArgumentOutOfRangeException("maxLoadFactor");
   if(seed==null)throw new ArgumentNullException("seed");
   if(seed.Length>MaximumNodes)throw new ArgumentOutOfRangeException("seed");
   this.bucketCount=bucketCount;this.bucketCapacity=bucketCapacity;this.maxLoadFactor=maxLoadFactor;
   head=new RegistrationNode(0,null);head.Next=head;head.Previous=head;
   first=new RegistrationNode[bucketCapacity];last=new RegistrationNode[bucketCapacity];
   for(int i=0;i<bucketCapacity;i++){first[i]=head;last[i]=head;}
   var seen=new bool[bucketCount];int previousBucket=-1;
   foreach(RegistrationEntry entry in seed){
    if(entry==null)throw new ArgumentException("Unknown seed entry.","seed");
    int bucket=BucketFor(entry.Key);
    if(bucket!=previousBucket&&seen[bucket])throw new ArgumentException("Each bucket must occupy one contiguous list segment.","seed");
    var node=new RegistrationNode(entry.Key,entry.Value);LinkBefore(node,head);
    if(first[bucket]==head)first[bucket]=node;
    last[bucket]=node;seen[bucket]=true;previousBucket=bucket;count++;
   }
  }
  // 0x1801356F0 / 0x18017FE40: FNV-1a over four little-endian raw uint bytes.
  public static ulong Hash(uint key){
   unchecked {ulong hash=14695981039346656037UL;for(int shift=0;shift<32;shift+=8)hash=(hash^((key>>shift)&255U))*1099511628211UL;return hash;}
  }
  public int BucketFor(uint key){return (int)(Hash(key)&(ulong)(bucketCount-1));}
  public RegistrationNode FirstInBucket(int bucket){CheckBucket(bucket);return first[bucket]==head?null:first[bucket];}
  public RegistrationNode LastInBucket(int bucket){CheckBucket(bucket);return last[bucket]==head?null:last[bucket];}
  void CheckBucket(int bucket){
   if(!stateComplete)throw new InvalidOperationException("Bucket endpoints are not a completed native state.");
   if(bucket<0||bucket>=bucketCount)throw new ArgumentOutOfRangeException("bucket");
  }
  // This remains available after Unsupported to expose retained list effects.
  public uint[] KeysInOrder(){
   var keys=new uint[count];var node=head.Next;
   for(int i=0;i<count;i++){keys[i]=node.Key;node=node.Next;}
   return keys;
  }
  static void LinkBefore(RegistrationNode node,RegistrationNode position){
   node.Next=position;node.Previous=position.Previous;
   position.Previous.Next=node;position.Previous=node;
  }
  static RegistrationIndexResult Result(string stage,bool effects,bool? inserted,ulong? removed,RegistrationNode node){return new RegistrationIndexResult(null,stage,effects,inserted,removed,node);}
  static RegistrationIndexResult Unsupported(string reason,string stage,bool effects,RegistrationNode node){return new RegistrationIndexResult(reason,stage,effects,null,null,node);}
  RegistrationIndexResult Incomplete(){return Unsupported("RegistrationIndexStateIncomplete","IncompleteState",false,null);}
  public RegistrationIndexResult Insert(uint key,object value){
   if(!stateComplete)return Incomplete();
   int bucket=BucketFor(key);var begin=first[bucket];
   // Native lookup scans from bucket last backwards, preserving an existing
   // node and its value even in an explicitly seeded duplicate-key segment.
   if(begin!=head){
    var current=last[bucket];
    for(;;){if(current.Key==key)return Result("Duplicate",false,false,null,current);if(current==begin)break;current=current.Previous;}
   }
   if(count==MaximumNodes)return Unsupported("RegistrationIndexNodeBudgetOutsideCoveredDomain","NodeBudget",false,null);
   RegistrationNode node;
   try{node=new RegistrationNode(key,value);}
   catch(OutOfMemoryException){return Unsupported("RegistrationIndexAllocationFailureNotReconstructed","Allocation",false,null);}
   // 0x180020960 + 0x180021A80 establish successful allocation and payload.
   // 0x1801356F0's temporary head insertion plus splice has this final state:
   // an occupied bucket gains a first node, an empty bucket goes to list tail.
   LinkBefore(node,begin);count++;
   first[bucket]=node;if(begin==head)last[bucket]=node;
   // Bounds above make both integers and their power-of-two ratio exactly
   // representable in binary32. Equality does not trigger COMISS/JA growth.
   float load=(float)count/(float)bucketCount;
   if(load>maxLoadFactor){
    int target=bucketCount<512?bucketCount*8:bucketCount*2;
    // These are model coverage checks after the native insertion prefix,
    // before any bucket reset. They do not model native growth failure.
    if(target>MaximumBuckets){stateComplete=false;return Unsupported("RegistrationIndexGrowthBucketBudgetOutsideCoveredDomain","PostLinkGrowth",true,node);}
    if((float)count/(float)target>maxLoadFactor){stateComplete=false;return Unsupported("RegistrationIndexRecursiveGrowthOutsideCoveredDomain","PostLinkGrowth",true,node);}
    string reason=ResetBuckets(target);
    if(reason!=null){stateComplete=false;return Unsupported(reason,"BucketReserve",true,node);}
    RehashOneLayer();
   }
   return Result("Inserted",true,true,null,node);
  }
  // 0x18001B4F0 reserves exactly 2*target pointer slots when needed. Existing
  // capacity is retained. Storage ownership is modeled with managed arrays;
  // native allocator/alignment/free internals remain conditional contracts.
  string ResetBuckets(int target){
   if(bucketCapacity<target){
    RegistrationNode[] nextFirst,nextLast;
    try{
     nextFirst=new RegistrationNode[target];nextLast=new RegistrationNode[target];
     Array.Copy(first,nextFirst,bucketCount);Array.Copy(last,nextLast,bucketCount);
    }catch(OutOfMemoryException){return "RegistrationIndexBucketAllocationFailureNotReconstructed";}
    first=nextFirst;last=nextLast;bucketCapacity=target;
   }
   for(int i=0;i<target;i++){first[i]=head;last[i]=head;}
   bucketCount=target;return null;
  }
  void RehashOneLayer(){
   if(head.Next==head)return;
   // 0x1800207C0 saves the old tail once, then fetches the CURRENT head each
   // iteration. A saved-next traversal would revisit or skip moved nodes.
   var oldTail=head.Previous;
   for(;;){
    var current=head.Next;int bucket=BucketFor(current.Key);var begin=first[bucket];
    RegistrationNode duplicate=null;
    if(begin!=head){
     var scan=last[bucket];
     for(;;){if(scan.Key==current.Key){duplicate=scan;break;}if(scan==begin)break;scan=scan.Previous;}
    }
    if(duplicate!=null){
     // 0x18001FD50 removes this not-yet-indexed node. The existing bucket
     // endpoints refer only to processed survivors and must not be changed.
     current.Previous.Next=current.Next;current.Next.Previous=current.Previous;
     count--;current.Next=null;current.Previous=null;
     // The duplicate path returns immediately, without nested maintenance.
    }else{
     if(current.Next!=begin){
      current.Previous.Next=current.Next;current.Next.Previous=current.Previous;
      LinkBefore(current,begin);
     }
     first[bucket]=current;if(begin==head)last[bucket]=current;
     // 0x18001FD50 now calls 0207C0 again. The checked count/target load was
     // already <= threshold, and count can only decrease in this loop, so
     // every nested maintenance call in this domain is a proven no-op.
    }
    // Compare identity even if this iteration removed the original tail.
    if(current==oldTail)break;
   }
  }
  public RegistrationIndexResult Erase(uint key){
   if(!stateComplete)return Incomplete();
   int bucket=BucketFor(key);var begin=head;var end=head;
   // 0x18017FE40 starts at first, then returns only the first consecutive
   // equal-key run. A later run of the same key is deliberately not selected.
   if(first[bucket]!=head){
    var stop=last[bucket].Next;var current=first[bucket];
    while(current!=stop&&current.Key!=key)current=current.Next;
    if(current!=stop){begin=current;do{current=current.Next;}while(current!=stop&&current.Key==key);end=current;}
   }
   ulong removed=0;for(var current=begin;current!=end;current=current.Next)removed++;
   // An empty miss is (head,head), which satisfies this same full-clear test.
   if(begin==head.Next&&end==head)return ClearCore(removed);
   var next=begin;
   while(next!=end){var current=next;next=current.Next;RemoveNode(current,bucket);}
   return Result(removed==0?"EraseMiss":"Erased",removed!=0,null,removed,null);
  }
  void RemoveNode(RegistrationNode node,int bucket){
   // 0x180020880 updates the two endpoints before detaching the list node.
   if(node==last[bucket]){
    if(node==first[bucket]){first[bucket]=head;last[bucket]=head;}
    else last[bucket]=node.Previous;
   }else if(node==first[bucket])first[bucket]=node.Next;
   node.Previous.Next=node.Next;node.Next.Previous=node.Previous;count--;
   node.Next=null;node.Previous=null;
  }
  public RegistrationIndexResult Clear(){if(!stateComplete)return Incomplete();return ClearCore((ulong)count);}
  RegistrationIndexResult ClearCore(ulong removed){
   var current=head.Next;head.Next=head;head.Previous=head;count=0;
   while(current!=head){var next=current.Next;current.Next=null;current.Previous=null;current=next;}
   // 0x18017FCA0 clears the list before 0x18001B4F0 resets to eight buckets.
   // 0x18001BD30's post-reserve path calls 0x180021740 with an empty
   // source range, then 0x18001EF70 fills 16 sentinel pointer slots. The
   // capacity is retained; only the first eight bucket pairs are now logical.
   string reason=ResetBuckets(8);
   if(reason!=null){stateComplete=false;return Unsupported(reason,"BucketReserve",true,null);}
   return Result("Cleared",true,null,removed,null);
  }
 }
}
