using System;

namespace GrimDynasty.ReverseClient.Actions {
 // Own safe data adapter for the observed 26CC50 + 109820 combination.
 // Not a native allocator, pointer-layout clone, tile cache or published mesh.
 // 26CC50: docs/reverse-client/r1-tile-cache-and-decompression/10,11.
 // 109820: docs/reverse-client/r1-tile-production-direct-stages/04,05.
 // Engine SHA256 4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153.
 public sealed class NavigationLayerFormatException : FormatException {
  public readonly string Code;
  public readonly long SourceOffset;
  internal NavigationLayerFormatException(string code,long offset,string message)
   :base(code+" at source offset "+offset+": "+message){Code=code;SourceOffset=offset;}
 }

 public sealed class NavigationLayerData {
  public const uint Magic=0x44544C52u;
  public const uint Version=1u;
  public const int HeaderLength=56;
  public const int MaximumWorkingBytes=4*255*255;

  // Source is the original PTHD layer view. Header and WorkData use a different,
  // newly owned array. This managed layout is [56-byte header][4*N work bytes],
  // not native [48-byte object][56-byte header][4*N work bytes]. RegionCount
  // separately represents the byte later written at native +8 by 269368;
  // other native object fields/addresses are not fabricated in this layout.
  public readonly NavigationPathingByteSlice Source;
  public readonly byte Width,Height;
  public readonly int CellCount,BytesWritten;
  public readonly ArraySegment<byte> Header,WorkData;
  // Shared mutable state for the separately recovered region builder. Initial
  // zero is proved by the full allocation memset at 26CD0B, not a guessed count.
  public byte RegionCount;
  readonly byte[] storage;
  NavigationLayerData(NavigationPathingByteSlice source,byte width,byte height,int cells,int written,byte[] bytes){
   Source=source;Width=width;Height=height;CellCount=cells;BytesWritten=written;storage=bytes;RegionCount=0;
   Header=new ArraySegment<byte>(bytes,0,HeaderLength);
   WorkData=new ArraySegment<byte>(bytes,HeaderLength,4*cells);
  }

  // Four consecutive N-byte views share one owner. Their later algorithmic
  // roles are not established by this decoder and are deliberately unnamed.
  public ArraySegment<byte> Plane(int index){
   if(index<0||index>3)throw new ArgumentOutOfRangeException("index");
   return new ArraySegment<byte>(storage,HeaderLength+index*CellCount,CellCount);
  }

  static uint U32(byte[] buffer,int offset){
   return (uint)buffer[offset]|((uint)buffer[offset+1]<<8)|((uint)buffer[offset+2]<<16)|((uint)buffer[offset+3]<<24);
  }
  static NavigationLayerFormatException Reject(string code,long offset,string message){return new NavigationLayerFormatException(code,offset,message);}
  public static NavigationLayerData Decode(NavigationPathingLayer layer){
   if(layer==null)throw new ArgumentNullException("layer");
   return Decode(layer.Payload,MaximumWorkingBytes);
  }
  public static NavigationLayerData Decode(NavigationPathingByteSlice source){return Decode(source,MaximumWorkingBytes);}

  // Stable input is required during this synchronous copy. No native allocator
  // callbacks, reentrant changes, frees, output-pointer publication or status
  // return codes are emulated. Exceptions are OWN safety/domain rejections.
  public static NavigationLayerData Decode(NavigationPathingByteSlice source,int maximumWorkingBytes){
   if(source==null)throw new ArgumentNullException("source");
   if(maximumWorkingBytes<0)throw new ArgumentOutOfRangeException("maximumWorkingBytes");
   byte[] input=source.Buffer;int offset=source.Offset,length=source.Length;
   if(input==null||offset<0||length<0||offset>input.Length||length>input.Length-offset)
    throw Reject("InvalidExtent",offset,"Borrowed source is outside its managed byte array");
   // Native reads fields without this extent check; it is not a native status.
   if(length<HeaderLength)throw Reject("ShortHeader",offset,"The observed layer header occupies 56 bytes");
   if(U32(input,offset)!=Magic)throw Reject("Magic",offset,"Expected the observed RLTD magic");
   if(U32(input,offset+4)!=Version)throw Reject("Version",offset+4,"Only observed layer version 1 is supported");
   byte width=input[offset+0x30],height=input[offset+0x31];
   int cells=(int)width*(int)height,capacity=4*cells,written=length-HeaderLength;
   if(capacity>maximumWorkingBytes)throw Reject("WorkingBudget",offset+0x30,"Four-plane working data exceeds the caller's own budget");
   // 109826 sign-extends inputLength, then 10982F memcpy copies that many bytes;
   // its capacity parameter is ignored. We refuse an unsafe destination overrun.
   if(written>capacity)throw Reject("Capacity",offset+HeaderLength,"Observed memcpy length would exceed the four-plane working area");
   // 26CD0B clears the native allocation; 26CD10..61 copies the entire header.
   // new byte[] provides the same initial zeros for the represented data only.
   byte[] owned=new byte[HeaderLength+capacity];
   Buffer.BlockCopy(input,offset,owned,0,HeaderLength);
   // The observed slot 109820 is a direct copy, NOT the ARC LZ4 codec. It writes
   // inputLength to outCount and returns 40000000. 26CC50 never checks outCount.
   // Short writes (including empty writes) therefore leave a zero tail. No
   // invented 3*N/exact-4*N constraint or special zero-dimension rejection.
   Buffer.BlockCopy(input,offset+HeaderLength,owned,HeaderLength,written);
   return new NavigationLayerData(source,width,height,cells,written,owned);
  }
 }
}
