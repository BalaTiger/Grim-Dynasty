using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace GrimDynasty.ReverseClient.Actions {
 // A strict, managed format adapter, NOT the complete native 1099B0 operation.
 // Evidence: docs/reverse-client/r1-world-load-and-nav-publication/04 and 05,
 // Engine.dll SHA256 4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153.
 // 109A0A/20/31: length >= 24, PTHD, version 1. The four signed DWORDs
 // at +08/+0C/+10/+14 are grid minima/maxima. Native location validation,
 // allocation, layer addition, cache construction,
 // publication and completion records are deliberately outside this adapter.
 // Native invalid-input paths may still append completion records; a format
 // exception here is an OWN rejection, never a native status/return value.
 public sealed class NavigationPathingFormatException : FormatException {
  public readonly string Code;
  public readonly long SourceOffset;
  internal NavigationPathingFormatException(string code,long offset,string message)
   :base(code+" at source offset "+offset+": "+message){Code=code;SourceOffset=offset;}
 }

 // A borrowed view: no copy and no ownership transfer. Keep Buffer alive and
 // stable throughout Parse and any consumer operation requiring stable bytes.
 // Metadata is a parse-time snapshot; later payload mutations remain visible.
 public sealed class NavigationPathingByteSlice {
  public readonly byte[] Buffer;
  public readonly int Offset;
  public readonly int Length;
  internal NavigationPathingByteSlice(byte[] buffer,int offset,int length){Buffer=buffer;Offset=offset;Length=length;}
  public byte this[int index] {
   get {
    if(index<0||index>=Length)throw new ArgumentOutOfRangeException("index");
    return Buffer[Offset+index];
   }
  }
 }

 public sealed class NavigationPathingLayer {
  public readonly int LengthPrefixOffset;
  public readonly uint DeclaredLength;
  public readonly NavigationPathingByteSlice Payload;
  internal NavigationPathingLayer(int prefix,uint length,NavigationPathingByteSlice payload){LengthPrefixOffset=prefix;DeclaredLength=length;Payload=payload;}
 }

 public sealed class NavigationPathingGroup {
  public readonly int LengthPrefixOffset;
  public readonly uint DeclaredLength;
  // Contains each layer length prefix and its payload, not this group's prefix.
  public readonly NavigationPathingByteSlice EncodedLayers;
  public readonly ReadOnlyCollection<NavigationPathingLayer> Layers;
  internal NavigationPathingGroup(int prefix,uint length,NavigationPathingByteSlice bytes,List<NavigationPathingLayer> layers){
   LengthPrefixOffset=prefix;DeclaredLength=length;EncodedLayers=bytes;Layers=layers.AsReadOnly();
  }
 }

 public sealed class NavigationPathingData {
  public const uint Magic=0x44485450u;
  public const uint Version=1u;
  public const int HeaderLength=24;
  public readonly NavigationPathingByteSlice Source;
  public readonly int MinimumGridX,MinimumGridZ,MaximumGridX,MaximumGridZ;
  public readonly ReadOnlyCollection<NavigationPathingGroup> Groups;
  NavigationPathingData(NavigationPathingByteSlice source,int minX,int minZ,int maxX,int maxZ,List<NavigationPathingGroup> groups){
   Source=source;MinimumGridX=minX;MinimumGridZ=minZ;MaximumGridX=maxX;MaximumGridZ=maxZ;Groups=groups.AsReadOnly();
  }

  static NavigationPathingFormatException Reject(string code,long offset,string message){return new NavigationPathingFormatException(code,offset,message);}
  static uint U32(byte[] buffer,int offset){
   return (uint)buffer[offset]|((uint)buffer[offset+1]<<8)|((uint)buffer[offset+2]<<16)|((uint)buffer[offset+3]<<24);
  }

  public static NavigationPathingData Parse(byte[] buffer){
   if(buffer==null)throw new ArgumentNullException("buffer");
   return Parse(buffer,0,buffer.Length);
  }

  // Requires a stable source during parsing: no callbacks or concurrent writes.
  // Native 109A9C reads the outer length; 109AC0 reads a child length before
  // passing a borrowed payload address to 264200. 109C35 and 109C5E re-read
  // those lengths AFTER callbacks. This pure parser does not emulate reentry,
  // mutations or the native wrapping subtraction/!=0 loops.
  // All extent checks and exact nested consumption below are OWN safety rules.
  // There is no inferred alignment, group/grid cardinality or bbox validity rule.
  public static NavigationPathingData Parse(byte[] buffer,int offset,int length){
   if(buffer==null)throw new ArgumentNullException("buffer");
   // Subtract before adding so signed/uint length conversions cannot overflow.
   if(offset<0||length<0||offset>buffer.Length||length>buffer.Length-offset)
    throw Reject("InvalidExtent",offset,"Requested extent is outside the supplied byte array");
   if(length<HeaderLength)throw Reject("ShortHeader",offset,"PTHD header requires 24 bytes");
   if(U32(buffer,offset)!=Magic)throw Reject("Magic",offset,"Unsupported magic; expected PTHD");
   if(U32(buffer,offset+4)!=Version)throw Reject("Version",offset+4,"Only observed PTHD version 1 is supported");
   int minX=unchecked((int)U32(buffer,offset+8)),minZ=unchecked((int)U32(buffer,offset+12));
   // Native end coordinates are consumed after the layer loop (109C99+).
   // Under this adapter's stable-input domain these are metadata snapshots only.
   int maxX=unchecked((int)U32(buffer,offset+16)),maxZ=unchecked((int)U32(buffer,offset+20));
   int end=offset+length,cursor=offset+HeaderLength;
   var groups=new List<NavigationPathingGroup>();
   while(cursor<end){
    int groupPrefix=cursor;
    if(end-cursor<4)throw Reject("TruncatedGroupLength",cursor,"Incomplete outer uint32 length");
    uint groupLength=U32(buffer,cursor);cursor+=4;
    if(groupLength>(uint)(end-cursor))throw Reject("GroupExtent",groupPrefix,"Outer payload exceeds the supplied PTHD extent");
    int groupStart=cursor,groupEnd=cursor+(int)groupLength;
    var layers=new List<NavigationPathingLayer>();
    while(cursor<groupEnd){
     int layerPrefix=cursor;
     if(groupEnd-cursor<4)throw Reject("TruncatedLayerLength",cursor,"Incomplete inner uint32 length within its group");
     uint layerLength=U32(buffer,cursor);cursor+=4;
     if(layerLength>(uint)(groupEnd-cursor))throw Reject("LayerExtent",layerPrefix,"Layer payload exceeds its enclosing group");
     layers.Add(new NavigationPathingLayer(layerPrefix,layerLength,new NavigationPathingByteSlice(buffer,cursor,(int)layerLength)));
     cursor+=(int)layerLength;
    }
    groups.Add(new NavigationPathingGroup(groupPrefix,groupLength,new NavigationPathingByteSlice(buffer,groupStart,(int)groupLength),layers));
   }
   return new NavigationPathingData(new NavigationPathingByteSlice(buffer,offset,length),minX,minZ,maxX,maxZ,groups);
  }
 }
}
