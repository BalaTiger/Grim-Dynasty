using System;
using System.Collections.Generic;

namespace GrimDynasty.ReverseClient.Actions {
 // An OWN view of the SAME Water state used by the saved-payload loader.
 // Additional fields start Unknown, not zero. This is neither native object
 // allocation nor a substitute for the unresolved embedded constructor.
 public sealed class GroundWaterInitializationView {
  public readonly GroundWaterState Water;
  public readonly Field<object> Terrain10C0=new Field<object>(),Grid10C8=new Field<object>(),Region10D0=new Field<object>();
  readonly Dictionary<uint,Field<ulong>> earlyQwords=new Dictionary<uint,Field<ulong>>();
  static readonly uint[] offsets={0x28,0x30,0x38,0x58,0x60,0x68,0x98,0x40,0x88,0x90};
  public GroundWaterInitializationView(GroundWaterState water){
   if(water==null)throw new ArgumentNullException("water");Water=water;
   foreach(uint offset in offsets)earlyQwords.Add(offset,new Field<ulong>());
  }
  public Field<ulong> EarlyQword(uint offset){Field<ulong> cell;if(!earlyQwords.TryGetValue(offset,out cell))throw new ArgumentOutOfRangeException("offset");return cell;}
 }
 // Borrow actual OWN engine/provider cells; no default provider or device is
 // allocated, and no graphics implementation is represented by these views.
 public sealed class GroundWaterEngineView {
  public readonly Field<object> Provider1F8;
  public GroundWaterEngineView(Field<object> provider){if(provider==null)throw new ArgumentNullException("provider");Provider1F8=provider;}
 }
 public sealed class GroundWaterRenderProviderView {
  public readonly Field<object> Device58;
  public GroundWaterRenderProviderView(Field<object> device){if(device==null)throw new ArgumentNullException("device");Device58=device;}
 }
 // OWN diagnostic for two native void-return branches. Neither value means
 // Water initialization succeeded or that Load prerequisites are established.
 public enum GroundWaterInitializePrefixReturn { NoRenderProvider,NoRenderDevice }
 public static class GroundWaterInitializationPrefixes {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Missing OWN Water initialization view");return value;}
  static void At(Invocation invocation,uint address){invocation.At=address;}
  static Field<T> Capture<T>(Field<T> field,uint at,Invocation invocation){At(invocation,at);return Need(field,at).Snapshot();}
  static void Copy<T>(Field<T> destination,Field<T> value,uint at,Invocation invocation){At(invocation,at);Need(destination,at).CopyFrom(Need(value,at));}

  // Region is a captured register argument. Its opaque value is preserved
  // before writes, including an exact OWN argument/field alias. The original
  // constructor must call203940(this+10D8) BEFORE zeroing counts or10B0/10B8.
  // This method stops at that call; it never runs a fabricated success hook.
  public static void ConstructPrefix1F6A90(GroundWaterInitializationView view,Field<object> regionArgument,Invocation invocation){
   var i=Need(invocation,0x1f6aad);var region=regionArgument==null?null:regionArgument.Snapshot();
   uint[] offsets={0x28,0x30,0x38,0x58,0x60,0x68,0x98,0x40,0x88,0x90};
   uint[] stores={0x1f6aad,0x1f6ab1,0x1f6ab5,0x1f6ab9,0x1f6abd,0x1f6ac1,0x1f6ac5,0x1f6acc,0x1f6ad0,0x1f6ad7};
   for(int n=0;n<offsets.Length;n++){At(i,stores[n]);Need(view,i.At).EarlyQword(offsets[n]).Write(0);}
   Copy(view.Region10D0,region,0x1f6ade,i);
   At(i,0x1f6aec);throw new MissingContract(i.At,"Unresolved203940 embedded constructor at Water+10D8; later count/prerequisite zeroing has not happened");
  }

  // Exact1F6EA6..1F6EE7 prefix. Terrain is the captured R9 value, whereas the
  // stack Grid argument is loaded only AFTER Terrain publication. Width and
  // height are not consumed by this prefix; no dimensions are invented here.
  // Same Water.Prerequisite10B8 is written before the device TEST. Unknown
  // device bits therefore publish Unknown before their later consumption.
  public static GroundWaterInitializePrefixReturn InitializePrefix1F6E80(GroundWaterInitializationView view,Field<object> terrainArgument,Field<object> gridStackArgument,Field<object> engineGlobal,Invocation invocation){
   var i=Need(invocation,0x1f6ea6);var terrain=terrainArgument==null?null:terrainArgument.Snapshot();
   At(i,0x1f6ea6);view=Need(view,i.At);Copy(view.Terrain10C0,terrain,i.At,i);
   var grid=Capture(gridStackArgument,0x1f6ead,i);Copy(view.Grid10C8,grid,0x1f6eb5,i);
   var engineBits=Capture(engineGlobal,0x1f6ebc,i);
   At(i,0x1f6ec3);object engineObject=engineBits.Read(i.At);var engine=engineObject as GroundWaterEngineView;
   if(engine==null)throw new MissingContract(i.At,"gEngine has no established shared +1F8 view");
   var providerBits=Capture(engine.Provider1F8,i.At,i);
   At(i,0x1f6eca);object providerObject=providerBits.Read(i.At);
   if(providerObject==null){At(i,0x1f702a);return GroundWaterInitializePrefixReturn.NoRenderProvider;}
   At(i,0x1f6ed3);var provider=providerObject as GroundWaterRenderProviderView;
   if(provider==null)throw new MissingContract(i.At,"Render provider has no established shared +58 view");
   var deviceBits=Capture(provider.Device58,i.At,i);
   Copy(view.Water.Prerequisite10B8,deviceBits,0x1f6ed7,i);
   At(i,0x1f6ede);object device=deviceBits.Read(i.At);
   if(device==null){At(i,0x1f702a);return GroundWaterInitializePrefixReturn.NoRenderDevice;}
   At(i,0x1f6ee7);throw new MissingContract(i.At,"Unresolved1F6BF0 before WaterRenderInterface203210 and RenderDevice slot150; empty payload does not bypass these calls");
  }
 }
}
