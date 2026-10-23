using System;

namespace GrimDynasty.ReverseClient.Actions {
 // Same mutable fields used by Load and GetIntersection. No constructor
 // defaults: Engine1F6A90/1F6E80 have not yet been recovered. The two opaque
 // prerequisite fields are only compared with null in the supported empty
 // Load path; this does not validate any object they point to.
 public sealed class GroundWaterState {
  public readonly object Identity=new object();
  public readonly Field<object> Prerequisite10B0=new Field<object>(),Prerequisite10B8=new Field<object>();
  public readonly Field<uint> LayerCountA0=new Field<uint>(),PathCountA4=new Field<uint>(),WaveCountA8=new Field<uint>();
 }
 // Explicit OWN copied-input cursor. It represents BinaryReader+8 and owns
 // only the supplied chunk bytes. Bounds rejection is OWN safety policy,
 // not the native reader's memory-fault behavior. No low32 pointer wrapping,
 // byte aliases, concurrent mutation or native storage lifetime is claimed.
 public sealed class GroundWaterChunkReader {
  readonly byte[] bytes;
  public readonly Field<int> Cursor08=new Field<int>();
  public readonly string SourceIdentity;
  public int Length{get{return bytes.Length;}}
  public GroundWaterChunkReader(byte[] chunk,string sourceIdentity){
   if(chunk==null)throw new ArgumentNullException("chunk");
   if(chunk.Length>1048576)throw new ArgumentException("OWN water chunk limit");
   bytes=(byte[])chunk.Clone();SourceIdentity=sourceIdentity;Cursor08.Write(0);
  }
  internal uint Read32(uint loadAt,uint advanceAt,Invocation i){
   i.At=loadAt;int cursor=Cursor08.Read(loadAt);
   if(cursor<0||cursor>bytes.Length||bytes.Length-cursor<4)throw new MissingContract(loadAt,"OWN bounded water reader extent");
   uint value=(uint)bytes[cursor]|((uint)bytes[cursor+1]<<8)|((uint)bytes[cursor+2]<<16)|((uint)bytes[cursor+3]<<24);
   i.At=advanceAt;Cursor08.Write(cursor+4);return value;
  }
 }
 // Evidence: r1-ground-resources-and-water/08,11,14. Engine SHA256
 // 4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153.
 // Supported normal domain: Load version2/typeCount0/layers0/paths0/waves0,
 // then GetIntersection with zero live layer/path counts (or surface==0).
 // Real mainmenu chunk9 supplies all four zero counts; the API never invents
 // them. Prerequisite10B0/10B8 must be established explicitly by an OWN caller
 // or a later recovered initializer; merely allocating GroundWaterState is
 // not a valid initialization. This is not native NewWater/Initialize.
 // Stable single-thread cells only. Distinct DWORDs within the output must
 // not share a managed Field; arbitrary shifted byte aliasing is unsupported.
 public static class GroundWaterContracts {
  static T Need<T>(T v,uint at)where T:class{if(v==null)throw new MissingContract(at,"Null OWN water view");return v;}
  static uint At(Invocation i,uint at){i.At=at;return at;}
  static float Float(uint bits){return BitConverter.ToSingle(BitConverter.GetBytes(bits),0);}
  public static bool Load1F81E0(GroundWaterState water,GroundWaterChunkReader reader,Invocation invocation){
   var i=Need(invocation,0x1f8212);At(i,0x1f8212);water=Need(water,i.At);
   if(water.Prerequisite10B0.Read(i.At)==null){At(i,0x1f8990);return false;}
   At(i,0x1f8220);if(water.Prerequisite10B8.Read(i.At)==null){At(i,0x1f8990);return false;}
   // Header reads advance the shared cursor before comparison/field stores.
   // Invalid magic/version invokes gEngine logging, whose runtime source is
   // unresolved here. Do not invent a silent native rejection or success.
   At(i,0x1f822e);reader=Need(reader,i.At);
   uint magic=reader.Read32(0x1f8232,0x1f8239,i);
   At(i,0x1f823d);if(magic!=0xfeedcba9u)throw new MissingContract(At(i,0x1f8246),"Invalid magic requires unresolved gEngine logger before native AL0");
   uint version=reader.Read32(0x1f8264,0x1f8270,i);
   At(i,0x1f8274);if(version!=2)throw new MissingContract(At(i,0x1f8279),"Version warning requires unresolved gEngine logger; only observed version2 supported");
   uint typeCount=reader.Read32(0x1f82a1,0x1f82ad,i);
   uint layerCount=reader.Read32(0x1f82b1,0x1f82b7,i);
   At(i,0x1f82bb);water.LayerCountA0.Write(layerCount);
   uint pathCount=reader.Read32(0x1f82c6,0x1f82cc,i);
   At(i,0x1f82d0);water.PathCountA4.Write(pathCount);
   uint waveCount=reader.Read32(0x1f82e0,0x1f82e6,i);
   At(i,0x1f82ea);water.WaveCountA8.Write(waveCount);
   At(i,0x1f830d);if(typeCount!=0)throw new MissingContract(At(i,0x1f8316),"Nonempty WaterType temporary-vector reserve/loader path unsupported");
   // Re-read the actual shared fields at native branch points. Header prefix
   // remains visible if a later unsupported allocation branch is reached.
   At(i,0x1f8578);if(water.LayerCountA0.Read(i.At)!=0)throw new MissingContract(At(i,0x1f85a5),"Nonempty WaterLayer allocation/load path unsupported");
   At(i,0x1f8693);if(water.PathCountA4.Read(i.At)!=0)throw new MissingContract(At(i,0x1f86b9),"Nonempty WaterPath allocation/load path unsupported");
   At(i,0x1f879f);if(water.WaveCountA8.Read(i.At)!=0)throw new MissingContract(At(i,0x1f87c5),"Nonempty WaterWaveSet allocation/load path unsupported");
   // The actual zero temporary vector requires neither release nor free.
   At(i,0x1f898c);return true;
  }
  public static bool GetIntersection1F9BD0(GroundWaterState water,GroundRay24 ray,GroundBlockHit20 output,uint surface,float maximumDistance,GroundTriangleOperands operands,Invocation invocation){
   var i=Need(invocation,0x1f9be5);At(i,0x1f9be5);
   uint infinity=Need(operands,i.At).Infinity338458.Read(i.At);
   if(infinity!=0x7f800000u)throw new MissingContract(i.At,"OWN requires observed Engine338458 positive infinity bits");
   At(i,0x1f9bf1);output=Need(output,i.At);
   var d=output.Distance00;var x=output.Position04.X0;var y=output.Position04.Y4;var z=output.Position04.Z8;
   if(Object.ReferenceEquals(d,x)||Object.ReferenceEquals(d,y)||Object.ReferenceEquals(d,z)||Object.ReferenceEquals(x,y)||Object.ReferenceEquals(x,z)||Object.ReferenceEquals(y,z))throw new MissingContract(i.At,"OWN overlapping output DWORD views unsupported");
   output.Distance00.Write(Float(infinity));
   At(i,0x1f9bf9);output.Position04.X0.Write(0f);output.Position04.Y4.Write(0f);
   At(i,0x1f9c00);output.Position04.Z8.Write(0f);
   At(i,0x1f9c07);if(surface==0){At(i,0x1f9c0c);return false;}
   // maximumDistance is only captured by MOVSS; ray is only a borrowed
   // pointer. Neither is consumed in the empty path. No invented finite gate.
   At(i,0x1f9c2c);water=Need(water,i.At);
   if(water.LayerCountA0.Read(i.At)!=0)throw new MissingContract(At(i,0x1f9c42),"Nonempty WaterLayer pointer traversal/RayIntersection1FF950 unsupported");
   At(i,0x1f9cd1);if(water.PathCountA4.Read(i.At)!=0)throw new MissingContract(At(i,0x1f9ce2),"Nonempty WaterPath pointer traversal/RayIntersection2013D0 unsupported");
   // WaveCountA8 is not read anywhere in this native function.
   // Native COMISS(+Inf,distance); SETA: ordered strict greater only.
   At(i,0x1f9d87);float distance=output.Distance00.Read(i.At);
   At(i,0x1f9d98);return !float.IsNaN(distance)&&Float(infinity)>distance;
   // Material10 remains untouched in every supported path. Native hit paths
   // write7 only after an accepted RayIntersection; those are not implemented.
  }
 }
}
