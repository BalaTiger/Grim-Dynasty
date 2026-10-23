using System;
namespace GrimDynasty.TaskItems {
 // A complete, valid logical byte-string snapshot. This constructor supplies
 // model input; it is not a recovered std::string constructor or allocator.
 public sealed class ObjectNameString {
  byte[] payload;
  ulong capacity;
  public ObjectNameString(byte[] bytes,ulong knownCapacity){
   if(bytes==null)throw new ArgumentNullException("bytes");
   if(knownCapacity<15||knownCapacity<(ulong)bytes.Length||knownCapacity==ulong.MaxValue)
    throw new ArgumentOutOfRangeException("knownCapacity");
   payload=(byte[])bytes.Clone();capacity=knownCapacity;
  }
  public ulong Length {get{return (ulong)payload.Length;}}
  public ulong Capacity {get{return capacity;}}
  public byte TerminatorByte {get{return 0;}}
  public byte FirstByte {get{return payload.Length==0?(byte)0:payload[0];}}
  public byte[] CopyBytes(){return (byte[])payload.Clone();}
  internal byte ByteAt(int index){return payload[index];}
  internal void ReplaceOwned(byte[] bytes,ulong knownCapacity){payload=bytes;capacity=knownCapacity;}
 }
}
