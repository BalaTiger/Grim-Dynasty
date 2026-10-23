using System;

namespace GrimDynasty.ReverseClient.Actions {
 // Own managed storage for the observed compressed-layer pool, not the complete
 // native cache object, obstacle container, allocator ABI, decoder or navmesh.
 // Evidence: 263E40 pool/bucket initialization; 264200 add; 2640F0 enumeration;
 // 2641C0 reference encoding. Engine SHA256
 // 4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153.
 // 2660F0 clears three container pointers, but 266010 still calls unexamined
 // 2661A0/2663E0. This type does not create a native obstacle container.
 public sealed class NavigationLayerCacheDomainException : InvalidOperationException {
  public readonly string Code;
  internal NavigationLayerCacheDomainException(string code,string message):base(code+": "+message){Code=code;}
 }

 public sealed class NavigationLayerReference {
  public uint Value;
  public NavigationLayerReference(uint value){Value=value;}
 }

 // Header08 and Blob20 retain the SAME supplied slice object. The payload view
 // begins at byte 0x38 of that slice; neither Add nor this view copies bytes.
 public sealed class NavigationLayerPayloadView {
  public readonly NavigationPathingByteSlice Source;
  public readonly int RelativeOffset;
  public readonly int Length;
  internal NavigationLayerPayloadView(NavigationPathingByteSlice source){Source=source;RelativeOffset=0x38;Length=source.Length-0x38;}
  public byte[] Buffer {get{return Source.Buffer;}}
  public int BufferOffset {get{return Source.Offset+RelativeOffset;}}
  public byte this[int index] {
   get{if(index<0||index>=Length)throw new ArgumentOutOfRangeException("index");return Source[RelativeOffset+index];}
  }
 }

 public sealed class NavigationLayerCacheSlot {
  internal readonly NavigationLayerCache Owner;
  public readonly int Index;
  public readonly uint Salt00;
  public NavigationPathingByteSlice Header08 {get;internal set;}
  public NavigationLayerPayloadView Payload10 {get;internal set;}
  public int PayloadLength18 {get;internal set;}
  public NavigationPathingByteSlice Blob20 {get;internal set;}
  public int BlobLength28 {get;internal set;}
  public uint Flags2C {get;internal set;}
  public NavigationLayerCacheSlot Next30 {get;internal set;}
  internal NavigationLayerCacheSlot(NavigationLayerCache owner,int index){Owner=owner;Index=index;Salt00=1;}
 }

 public sealed class NavigationLayerCache {
  public const uint Success=0x40000000u;
  public const uint Duplicate=0x80000000u;
  public const uint WrongMagic=0x80000001u;
  public const uint WrongVersion=0x80000002u;
  public const uint PoolFull=0x80000004u;
  public const uint LayerMagic=0x44544c52u;
  public const int MaximumSupportedCapacity=4096;
  readonly NavigationLayerCacheSlot[] slots,buckets;
  NavigationLayerCacheSlot free;
  int count;
  public int Capacity {get{return slots.Length;}}
  public int BucketCount {get{return buckets.Length;}}
  public int BucketMask {get{return buckets.Length-1;}}
  public int IndexBits {get;private set;}
  public int SaltBits {get;private set;}
  public int Count {get{return count;}}
  public NavigationLayerCacheSlot FreeHead {get{return free;}}

  // The 1..4096 limit is an OWN bounded allocation domain, not a native status.
  // Managed allocation succeeds or throws; native allocator-failure prefixes,
  // cache reinitialization and partially allocated native objects are excluded.
  public static NavigationLayerCache Create(int capacity){
   if(capacity<1||capacity>MaximumSupportedCapacity)throw new ArgumentOutOfRangeException("capacity","Own supported capacity is 1..4096");
   return new NavigationLayerCache(capacity);
  }
  NavigationLayerCache(int capacity){
   uint rounded=NextPowerOfTwo((uint)capacity);
   int bits=0;while(rounded>1){rounded>>=1;bits++;}
   IndexBits=bits;SaltBits=Math.Min(31,32-bits);
   int bucketCount=(int)NextPowerOfTwo((uint)(capacity/4));
   if(bucketCount==0)bucketCount=1;
   slots=new NavigationLayerCacheSlot[capacity];buckets=new NavigationLayerCacheSlot[bucketCount];
   // 263FC2..264038 zero slots/buckets, then traverse last to first: salt=1,
   // next=current free, free=slot. All managed fields here start known zero.
   for(int n=capacity-1;n>=0;n--){var slot=new NavigationLayerCacheSlot(this,n);slot.Next30=free;slots[n]=slot;free=slot;}
  }
  static uint NextPowerOfTwo(uint value){
   if(value==0)return 0;
   value--;value|=value>>1;value|=value>>2;value|=value>>4;value|=value>>8;value|=value>>16;return value+1;
  }
  public NavigationLayerCacheSlot GetSlot(int index){if(index<0||index>=slots.Length)throw new ArgumentOutOfRangeException("index");return slots[index];}

  // 32-bit wrapped products, then SIGN-EXTEND both hash and mask before AND.
  // This preserves the instruction semantics, even though our positive mask
  // domain means the final array index is always nonnegative.
  public int BucketIndex(int x,int z){
   int mixed=unchecked(x*unchecked((int)0x8da6b343u)-z*0x27e9c7bf);
   return (int)((long)mixed&(long)BucketMask);
  }
  static uint Word(NavigationPathingByteSlice input,int at){
   if(input==null)throw new ArgumentNullException("input");
   if(at<0||input.Length-at<4)throw new NavigationLayerCacheDomainException("UnreadableWord","The supplied slice lacks a required DWORD");
   return (uint)input[at]|((uint)input[at+1]<<8)|((uint)input[at+2]<<16)|((uint)input[at+3]<<24);
  }
  static int SignedWord(NavigationPathingByteSlice input,int at){return unchecked((int)Word(input,at));}

  // 2641C0: null -> 0; otherwise (salt << (indexBits & 31)) | slot index.
  // Typed slots model exactly aligned members of this pool, not arbitrary
  // pointer subtraction, foreign slots or cross-type overlapping memory.
  public uint EncodeReference(NavigationLayerCacheSlot slot){
   if(slot==null)return 0;
   if(!Object.ReferenceEquals(slot.Owner,this))throw new NavigationLayerCacheDomainException("ForeignSlot","Slot does not belong to this pool");
   return unchecked((slot.Salt00<<(IndexBits&31))|(uint)slot.Index);
  }

  // Input is borrowed and strongly retained, with no native ownership transfer.
  // It must be stable throughout each operation: concurrent mutation/reentry,
  // aliasing source bytes with managed cache fields or output cells are outside
  // this domain. Between operations, byte edits are visible through live header
  // reads; they do NOT move the slot into a new bucket or refresh cached lengths.
  // Magic/version failures have native codes and leave output/state untouched.
  // A valid magic/version requires a complete 0x38-byte header in our safe domain;
  // that extent rejection is OWN behavior, not a fabricated native status.
  public uint Add(NavigationPathingByteSlice input,NavigationLayerReference output){
   if(Word(input,0)!=LayerMagic)return WrongMagic;
   if(Word(input,4)!=1)return WrongVersion;
   if(input.Length<0x38)throw new NavigationLayerCacheDomainException("LayerHeaderExtent","Successful registration requires a complete 0x38-byte layer header in this adapter");
   int z=SignedWord(input,0x0c),x=SignedWord(input,8);
   NavigationLayerCacheSlot node=buckets[BucketIndex(x,z)];
   while(node!=null){
    NavigationPathingByteSlice header=node.Header08;
    if(header!=null&&SignedWord(header,8)==x&&SignedWord(header,0x0c)==z){
     uint incomingLayer=Word(input,0x10); // 264279 before existing header +10.
     if(incomingLayer==Word(header,0x10))return Duplicate;
    }
    node=node.Next30;
   }
   node=free;if(node==null)return PoolFull;
   // 26429C..2642FA: pop free, clear next, re-read incoming coordinates,
   // publish bucket head, then write the remaining fields in instruction order.
   free=node.Next30;node.Next30=null;
   int bucket=BucketIndex(SignedWord(input,8),SignedWord(input,0x0c));
   node.Next30=buckets[bucket];buckets[bucket]=node;
   node.Flags2C=0;
   node.Payload10=new NavigationLayerPayloadView(input);
   node.PayloadLength18=unchecked(input.Length-0x38);
   node.Header08=input;
   node.Blob20=input;
   node.BlobLength28=input.Length;
   if(output!=null)output.Value=EncodeReference(node); // 264305 then 26430A.
   count++;
   return Success;
  }

  // 2640F0: output capacity is fixed at 32 in the native body. OWN buffer bounds
  // reject anything smaller up front. No clear is performed; suffix is untouched.
  // Traversal follows bucket chain order, including AFTER 32 matches are written.
  public int Enumerate(int x,int z,uint[] output){
   if(output==null)throw new ArgumentNullException("output");
   if(output.Length<32)throw new NavigationLayerCacheDomainException("OutputExtent","Enumeration requires at least 32 uint cells");
   int written=0;
   NavigationLayerCacheSlot node=buckets[BucketIndex(x,z)];
   while(node!=null){
    NavigationPathingByteSlice header=node.Header08;
    if(header!=null&&SignedWord(header,8)==x&&SignedWord(header,0x0c)==z&&written<32){
     // 264169/16F/179: salt, pool-relative index, then shift. Pool/index/shift
     // are immutable in this managed domain; no general raw-pointer alias is implied.
     uint salt=node.Salt00;
     int index=node.Index;
     int shift=IndexBits;
     output[written]=unchecked((salt<<(shift&31))|(uint)index);written++;
    }
    node=node.Next30;
   }
   return written;
  }
 }
}
