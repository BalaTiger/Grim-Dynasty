// Synthetic ARZv3 bytes for own tests only; no game assets or file access.
using System;
using System.IO;

namespace GrimDynasty.Resources {
 public sealed class ArzRawFixtureRecord {
  public readonly uint NameId,UnpackedLength;
  public readonly byte[] PackedBody,ClassBytes;
  public ArzRawFixtureRecord(uint nameId,byte[] rawBody)
   :this(nameId,ArzRawFixture.LiteralBlock(rawBody),checked((uint)rawBody.Length),new byte[]{73,116,101,109}){}
  public ArzRawFixtureRecord(uint nameId,byte[] packedBody,uint unpackedLength,byte[] classBytes){
   if(packedBody==null||classBytes==null)throw new ArgumentNullException();
   NameId=nameId;PackedBody=(byte[])packedBody.Clone();UnpackedLength=unpackedLength;ClassBytes=(byte[])classBytes.Clone();
  }
 }
 public sealed class ArzRawFixtureData {
  // Mutable fixture bytes deliberately allow individual malformed-input tests.
  public readonly byte[] Bytes;
  public readonly int[] PayloadOffsets,EntryOffsets;
  public readonly int RecordTableOffset,RecordTableSize,StringTableOffset,StringTableSize;
  internal ArzRawFixtureData(byte[] bytes,int[] payloads,int[] entries,int rt,int rs,int st,int ss){
   Bytes=bytes;PayloadOffsets=payloads;EntryOffsets=entries;RecordTableOffset=rt;RecordTableSize=rs;StringTableOffset=st;StringTableSize=ss;
  }
 }
 public static class ArzRawFixture {
  public static ArzRawFixtureData Build(byte[][] pool,params ArzRawFixtureRecord[] records){
   if(pool==null||records==null)throw new ArgumentNullException();
   using(var memory=new MemoryStream())using(var writer=new BinaryWriter(memory)){
    writer.Write(new byte[24]);var payloads=new int[records.Length];var entries=new int[records.Length];
    for(int i=0;i<records.Length;i++){
     if(records[i]==null)throw new ArgumentException("null fixture record");
     payloads[i]=checked((int)memory.Position);writer.Write(records[i].PackedBody);
    }
    int rt=checked((int)memory.Position);
    for(int i=0;i<records.Length;i++){
     var record=records[i];entries[i]=checked((int)memory.Position);
     writer.Write(record.NameId);writer.Write(checked((uint)record.ClassBytes.Length));writer.Write(record.ClassBytes);
     writer.Write(checked((uint)(payloads[i]-24)));writer.Write(checked((uint)record.PackedBody.Length));writer.Write(record.UnpackedLength);
     writer.Write(0x89abcdefU);writer.Write(0x01234567U);
    }
    int st=checked((int)memory.Position),rs=checked(st-rt);
    writer.Write(checked((uint)pool.Length));
    foreach(byte[] value in pool){
     if(value==null)throw new ArgumentException("null fixture string");
     writer.Write(checked((uint)value.Length));writer.Write(value);
    }
    int ss=checked((int)memory.Position-st);writer.Flush();byte[] bytes=memory.ToArray();
    U16(bytes,0,2);U16(bytes,2,3);U32(bytes,4,checked((uint)rt));U32(bytes,8,checked((uint)rs));
    U32(bytes,12,checked((uint)records.Length));U32(bytes,16,checked((uint)st));U32(bytes,20,checked((uint)ss));
    return new ArzRawFixtureData(bytes,payloads,entries,rt,rs,st,ss);
   }
  }
  public static byte[] LiteralBlock(byte[] body){
   if(body==null)throw new ArgumentNullException("body");
   using(var memory=new MemoryStream()){
    memory.WriteByte(checked((byte)(Math.Min(body.Length,15)<<4)));
    if(body.Length>=15){
     int remaining=body.Length-15;
     while(remaining>=255){memory.WriteByte(255);remaining-=255;}
     memory.WriteByte(checked((byte)remaining));
    }
    memory.Write(body,0,body.Length);return memory.ToArray();
   }
  }
  public static byte[] Field(ushort typeTag,uint key,params uint[] words){
   if(words==null||words.Length>ushort.MaxValue)throw new ArgumentException("fixture field count");
   using(var memory=new MemoryStream())using(var writer=new BinaryWriter(memory)){
    writer.Write(typeTag);writer.Write(checked((ushort)words.Length));writer.Write(key);
    foreach(uint word in words)writer.Write(word);
    writer.Flush();return memory.ToArray();
   }
  }
  public static byte[] Join(params byte[][] pieces){
   using(var memory=new MemoryStream()){
    foreach(byte[] piece in pieces){if(piece==null)throw new ArgumentException("null fixture piece");memory.Write(piece,0,piece.Length);}
    return memory.ToArray();
   }
  }
  public static void U16(byte[] bytes,int at,ushort value){
   bytes[at]=unchecked((byte)value);bytes[at+1]=unchecked((byte)(value>>8));
  }
  public static void U32(byte[] bytes,int at,uint value){
   for(int i=0;i<4;i++)bytes[at+i]=unchecked((byte)(value>>(8*i)));
  }
 }
}
