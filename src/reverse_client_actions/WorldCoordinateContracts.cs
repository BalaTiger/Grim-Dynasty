using System;
using System.Collections.Generic;
namespace GrimDynasty.ReverseClient.Actions {
 // One authoritative24-byte view. D14 is independent knowledge, never a
 // synthesized high half of a coarse Q16. This deliberately does not mirror or
 // synchronize the frozen WorldPosition type. A same-address alias shares all
 // cells and the address identity; arbitrary shifted/cross-type aliases are out.
 public sealed class WorldPosition24 {
  public readonly object AddressIdentity;
  public readonly Field<ulong> Q0;
  public readonly Field<uint> D8,DC,D10,D14;
  public WorldPosition24(){AddressIdentity=new object();Q0=new Field<ulong>();D8=new Field<uint>();DC=new Field<uint>();D10=new Field<uint>();D14=new Field<uint>();}
  public WorldPosition24(WorldPosition24 sameAddress){if(sameAddress==null)throw new ArgumentNullException("sameAddress");AddressIdentity=sameAddress.AddressIdentity;Q0=sameAddress.Q0;D8=sameAddress.D8;DC=sameAddress.DC;D10=sameAddress.D10;D14=sameAddress.D14;}
 }
 public sealed class WorldBounds24 {
  public readonly Field<float> Center00=new Field<float>(),Center04=new Field<float>(),Center08=new Field<float>();
  public readonly Field<float> Extent0C=new Field<float>(),Extent10=new Field<float>(),Extent14=new Field<float>();
 }
 // Explicit region identity and observed inline fields only. Allocating this
 // view does not initialize a native region, lock, level, bounds or coordinate.
 public sealed class WorldRegion {
  public readonly object Identity;
  public readonly WorldBounds24 Bounds70=new WorldBounds24();
  public readonly Field<int> Offset3C=new Field<int>(),Offset40=new Field<int>(),Offset44=new Field<int>();
  public readonly Field<byte> Unavailable90=new Field<byte>();
  public readonly Field<object> Critical18=new Field<object>();
  public WorldRegion(object identity){if(identity==null)throw new ArgumentNullException("identity");Identity=identity;}
 }
 public sealed class WorldCoordinateState {
  readonly Dictionary<ulong,WorldRegion> regions=new Dictionary<ulong,WorldRegion>();
  // Stable, explicit token binding; no zero token, duplicate identity view or
  // implicit address/ABA equivalence is inferred from a coordinate copy.
  public void AttachRegion(ulong token,WorldRegion region){
   if(token==0)throw new ArgumentOutOfRangeException("token");if(region==null)throw new ArgumentNullException("region");
   WorldRegion prior;if(regions.TryGetValue(token,out prior)){if(!Object.ReferenceEquals(prior,region))throw new InvalidOperationException("Region token already has a different view");return;}
   foreach(var pair in regions)if(Object.ReferenceEquals(pair.Value.Identity,region.Identity))throw new InvalidOperationException("Region identity is already attached at another token/view");
   regions.Add(token,region);
  }
  public WorldRegion Region(ulong token,uint at){WorldRegion region;if(token==0||!regions.TryGetValue(token,out region))throw new MissingContract(at,"Own region token has no explicit shared identity binding");return region;}
 }
 public sealed class WorldCoordinateBoundaries {
  public readonly WorldCoordinateState State;
  public readonly Field<WorldEngine> EngineGlobal;
  // The instruction consumes raw XOR bits, not a presumed minus sign. This
  // stays unknown until explicitly supplied from a fixed observed operand or
  // a clearly identified own fixture; no decimal/algorithmic guess is used.
  public readonly Field<uint> XorMask34C450=new Field<uint>();
  WorldPosition24 lastFloorTemporary;
  // Diagnostic reference to the actual fresh local used by the last non-null
  // wrapper call. This is NOT a native public field or a way to inject success.
  public WorldPosition24 LastFloorTemporary{get{return lastFloorTemporary;}}
  public WorldCoordinateBoundaries(WorldCoordinateState state):this(state,new Field<WorldEngine>()){}
  public WorldCoordinateBoundaries(WorldCoordinateState state,Field<WorldEngine> engineGlobal){if(state==null)throw new ArgumentNullException("state");if(engineGlobal==null)throw new ArgumentNullException("engineGlobal");State=state;EngineGlobal=engineGlobal;}
  // r1-region-bounds-mask: exact disk bits00000080LE. This is an explicit own
  // configuration operation, not evidence for the mutable loaded operand.
  public void UseObservedBoundsMask(){XorMask34C450.Write(0x80000000u);}
  internal void ObserveFloorTemporary(WorldPosition24 value){lastFloorTemporary=value;}
 }
 // Evidence: r1-navigation-consumer-direct22C350/22C620 and
 // r1-movement-floor-core0664D0/221DA0, original Engine SHA256
 // 4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153.
 // Binary32 operation order and ordinary masked FP completion are projected
 // into own typed cells; native allocation/EH, locks, lifetimes, FP trap modes,
 // FTZ/DAZ, NaN payload choices and arbitrary byte aliases are not emulated.
 // Translation supports only the observed bool=true same-region path. Real
 // world floor/intersection and cross-region services remain explicit stops.
 public static class WorldCoordinateContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null own world-coordinate view");return value;}
  static float F(float value){return BitConverter.ToSingle(BitConverter.GetBytes(value),0);}
  static float Float(uint bits){return BitConverter.ToSingle(BitConverter.GetBytes(bits),0);}
  static uint Bits(float value){return BitConverter.ToUInt32(BitConverter.GetBytes(value),0);}
  static float Read(Field<uint> cell,uint at){return Float(cell.Read(at));}
  public static byte Contains0664D0(WorldBounds24 bounds,WorldPosition24 point,WorldCoordinateBoundaries b,Invocation invocation){
   var i=Need(invocation,0x0664d4);i.At=0x0664d4;float extentX=Need(bounds,i.At).Extent0C.Read(i.At);
   i.At=0x0664d9;float y=Read(Need(point,i.At).DC,i.At);i.At=0x0664de;float z=Read(point.D10,i.At);
   i.At=0x0664e3;y=F(y-bounds.Center04.Read(i.At));i.At=0x0664e8;z=F(z-bounds.Center08.Read(i.At));
   i.At=0x0664f1;float x=Read(point.D8,i.At);i.At=0x0664f5;x=F(x-bounds.Center00.Read(i.At));
   if(x>extentX)return 0; // Upper JA rejects ordered greater-than only.
   i.At=0x0664fe;float extentY=bounds.Extent10.Read(i.At);if(y>extentY)return 0;
   i.At=0x066508;float extentZ=bounds.Extent14.Read(i.At);if(z>extentZ)return 0;
   i.At=0x066512;uint mask=Need(b,i.At).XorMask34C450.Read(i.At);
   // Lower JC rejects less-than OR unordered. Do not replace raw XOR with
   // negation, nor turn these three ordered comparisons into one absolute value.
   if(!(x>=Float(Bits(extentX)^mask)))return 0;
   if(!(y>=Float(Bits(extentY)^mask)))return 0;
   if(!(z>=Float(Bits(extentZ)^mask)))return 0;
   return 1;
  }
  public static byte TranslateTrue22C350(WorldPosition24 position,Displacement3 delta,WorldCoordinateBoundaries b,Invocation invocation){
   var i=Need(invocation,0x22c35e);i.At=0x22c35e;ulong originalRegion=Need(position,i.At).Q0.Read(i.At);if(originalRegion==0)return 0;
   // Native captures its ray scratch even on the true branch. These opaque MOV
   // copies need valid cell addresses but do not invent known scalar values.
   i.At=0x22c379;position.D8.Snapshot();position.DC.Snapshot();i.At=0x22c37e;position.D10.Snapshot();
   i.At=0x22c39e;Need(delta,i.At).X0.Snapshot();delta.Y4.Snapshot();i.At=0x22c3a5;delta.Z8.Snapshot();
   // bool=true skips portal enumeration. Arithmetic rereads the live operands;
   // it does not use the earlier scratch snapshot or precompute all three sums.
   i.At=0x22c452;float x=Read(position.D8,i.At);i.At=0x22c459;x=F(x+delta.X0.Read(i.At));i.At=0x22c45f;position.D8.Write(Bits(x));
   i.At=0x22c466;float y=delta.Y4.Read(i.At);i.At=0x22c46c;y=F(y+Read(position.DC,i.At));i.At=0x22c473;position.DC.Write(Bits(y));
   i.At=0x22c47a;float z=delta.Z8.Read(i.At);i.At=0x22c480;z=F(z+Read(position.D10,i.At));i.At=0x22c487;position.D10.Write(Bits(z));
   i.At=0x22c56a;ulong regionToken=position.Q0.Read(i.At);
   // LEA region+70 alone is not a region lookup/dereference. Resolve its own
   // identity at0664D0's first bounds load, after all three coordinate writes.
   i.At=0x0664d4;var region=Need(b,i.At).State.Region(regionToken,i.At);
   if(Contains0664D0(region.Bounds70,position,b,i)!=0)return 1;
   // The world pointer's MOV is captured before the actual +50 dereference.
   i.At=0x22c580;var enginePointer=b.EngineGlobal.Snapshot();i.At=0x22c58f;var engine=Need(enginePointer.Read(i.At),i.At);engine.World50.Snapshot();
   i.At=0x22c593;throw new MissingContract(i.At,"World::GetRegionContainingPoint21CFB0 and returned-region193AC0 conversion remain unsupported; translated XYZ prefix retained");
  }
  public static byte WorldPutOnFloor221DA0(object world,WorldPosition24 position,WorldCoordinateBoundaries b,Invocation invocation){
   return WorldFloorPrefix221DA0(Field<object>.Of(world),position,b,invocation);
  }
  static byte WorldFloorPrefix221DA0(Field<object> capturedWorld,WorldPosition24 position,WorldCoordinateBoundaries b,Invocation invocation){
   // The observed early path never reads RCX/this; do not invent a null-world
   // rejection. The public wrapper still performs its real engine+50 load.
   var i=Need(invocation,0x221db5);i.At=0x221db5;ulong regionToken=Need(position,i.At).Q0.Read(i.At);
   i.At=0x221dbb;var region=Need(b,i.At).State.Region(regionToken,i.At);if(region.Unavailable90.Read(i.At)!=0)return 0;
   i.At=0x221dca;region.Critical18.Snapshot();
   i.At=0x221dce;throw new MissingContract(i.At,"EnterCriticalSection import302328 is not integrated; no lock success or subsequent Level68/floor result is fabricated");
  }
  public static byte PutOnFloor22C620(WorldPosition24 position,WorldCoordinateBoundaries b,Invocation invocation){
   var i=Need(invocation,0x22c626);i.At=0x22c626;ulong region=Need(position,i.At).Q0.Read(i.At);if(region==0)return 0;
   var temporary=new WorldPosition24();Need(b,0x22c631).ObserveFloorTemporary(temporary);
   i.At=0x22c631;var x=position.D8.Snapshot();var y=position.DC.Snapshot();i.At=0x22c63b;temporary.Q0.Write(region);
   i.At=0x22c640;var z=position.D10.Snapshot();i.At=0x22c643;var enginePointer=b.EngineGlobal.Snapshot();
   i.At=0x22c64a;temporary.D8.CopyFrom(x);temporary.DC.CopyFrom(y);i.At=0x22c650;temporary.D10.CopyFrom(z); // D14 is never copied from caller.
   i.At=0x22c654;var engine=Need(enginePointer.Read(i.At),i.At);var worldPointer=engine.World50.Snapshot();
   i.At=0x22c658;
   // The callee's current supported prefix does not consume its RCX value.
   // Preserve a captured opaque world pointer rather than require known bits
   // merely for argument transfer. Later world-dependent code must read it.
   if(WorldFloorPrefix221DA0(worldPointer,temporary,b,i)==0)return 0;
   // Future supported floor completion must preserve this16+8 copy contract:
   // a callee-unwritten local D14 stays Unknown, never zero or old caller D14.
   i.At=0x22c661;var q=temporary.Q0.Snapshot();x=temporary.D8.Snapshot();y=temporary.DC.Snapshot();
   i.At=0x22c668;z=temporary.D10.Snapshot();var padding=temporary.D14.Snapshot();
   i.At=0x22c66e;position.Q0.CopyFrom(q);position.D8.CopyFrom(x);position.DC.CopyFrom(y);
   i.At=0x22c671;position.D10.CopyFrom(z);position.D14.CopyFrom(padding);return 1;
  }
 }
}
