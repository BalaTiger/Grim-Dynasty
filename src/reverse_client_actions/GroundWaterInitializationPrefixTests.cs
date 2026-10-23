using System;
using GrimDynasty.ReverseClient.Actions;

public static class GroundWaterInitializationPrefixTestProgram {
 static int scenarios,assertions;
 static void Check(bool value,string message){assertions++;if(!value)throw new Exception(message);}
 static void Scenario(Action body){body();scenarios++;}
 static void Missing(Action body,uint at){assertions++;try{body();}catch(MissingContract e){if(e.Address!=at)throw new Exception("Wrong stop "+e.Address.ToString("x")+" instead of "+at.ToString("x"));return;}throw new Exception("Expected Unsupported "+at.ToString("x"));}
 static GroundWaterInitializationView View(){return new GroundWaterInitializationView(new GroundWaterState());}
 static Field<object> Engine(Field<object> provider){return Field<object>.Of(new GroundWaterEngineView(provider));}
 static GroundWaterInitializePrefixReturn Initialize(GroundWaterInitializationView v,Field<object> terrain,Field<object> grid,Field<object> engine,Invocation i){return GroundWaterInitializationPrefixes.InitializePrefix1F6E80(v,terrain,grid,engine,i);}
 public static int Main(string[] args){try{
  if(args.Length!=0)throw new Exception("No arguments expected; this suite reads no original or sample files");
  Scenario(delegate{
   var v=View();object region=new object(),oldTree=new object(),oldDevice=new object();v.Water.Prerequisite10B0.Write(oldTree);v.Water.Prerequisite10B8.Write(oldDevice);v.Water.LayerCountA0.Write(31);v.Water.PathCountA4.Write(32);v.Water.WaveCountA8.Write(33);
   var i=new Invocation();Missing(delegate{GroundWaterInitializationPrefixes.ConstructPrefix1F6A90(v,Field<object>.Of(region),i);},0x1f6aec);
   foreach(uint offset in new uint[]{0x28,0x30,0x38,0x58,0x60,0x68,0x98,0x40,0x88,0x90})Check(v.EarlyQword(offset).Read(0)==0,"Observed pre-call QWORD zero at "+offset.ToString("x"));
   Check(Object.ReferenceEquals(region,v.Region10D0.Read(0))&&i.At==0x1f6aec,"Borrowed region published before embedded-constructor stop");
   Check(Object.ReferenceEquals(oldTree,v.Water.Prerequisite10B0.Read(0))&&Object.ReferenceEquals(oldDevice,v.Water.Prerequisite10B8.Read(0)),"No post-203940 prerequisite zeroing fabricated");
   Check(v.Water.LayerCountA0.Read(0)==31&&v.Water.PathCountA4.Read(0)==32&&v.Water.WaveCountA8.Read(0)==33,"Count QWORD/DWORD clears have not yet occurred");
  });
  Scenario(delegate{
   var v=View();v.Region10D0.Write(new object());Missing(delegate{GroundWaterInitializationPrefixes.ConstructPrefix1F6A90(v,new Field<object>(),new Invocation());},0x1f6aec);
   Missing(delegate{v.Region10D0.Read(123);},123);Missing(delegate{v.Water.Prerequisite10B0.Read(124);},124);
   Check(v.EarlyQword(0x90).Read(0)==0,"Opaque unknown region does not prematurely stop preceding stores");
  });
  Scenario(delegate{
   var v=View();object terrain=new object(),grid=new object(),oldDevice=new object();v.Water.Prerequisite10B8.Write(oldDevice);
   Missing(delegate{Initialize(v,Field<object>.Of(terrain),Field<object>.Of(grid),new Field<object>(),new Invocation());},0x1f6ec3);
   Check(Object.ReferenceEquals(terrain,v.Terrain10C0.Read(0))&&Object.ReferenceEquals(grid,v.Grid10C8.Read(0)),"Both input pointer stores precede gEngine consumption");
   Check(Object.ReferenceEquals(oldDevice,v.Water.Prerequisite10B8.Read(0)),"Unknown gEngine does not touch device field");
  });
  Scenario(delegate{
   var v=View();object oldTree=new object(),oldDevice=new object();v.Water.Prerequisite10B0.Write(oldTree);v.Water.Prerequisite10B8.Write(oldDevice);v.Water.LayerCountA0.Write(19);var i=new Invocation();
   var returned=Initialize(v,new Field<object>(),new Field<object>(),Engine(Field<object>.Of(null)),i);
   Check(returned==GroundWaterInitializePrefixReturn.NoRenderProvider&&i.At==0x1f702a,"Known null provider reaches actual void-return branch");
   Missing(delegate{v.Terrain10C0.Read(12);},12);Missing(delegate{v.Grid10C8.Read(13);},13);
   Check(Object.ReferenceEquals(oldTree,v.Water.Prerequisite10B0.Read(0))&&Object.ReferenceEquals(oldDevice,v.Water.Prerequisite10B8.Read(0))&&v.Water.LayerCountA0.Read(0)==19,"Provider-null does not reset prior Water state or assert readiness");
  });
  Scenario(delegate{
   var v=View();object oldDevice=new object();v.Water.Prerequisite10B8.Write(oldDevice);
   var provider=Engine(new Field<object>());Missing(delegate{Initialize(v,Field<object>.Of(null),Field<object>.Of(null),provider,new Invocation());},0x1f6eca);
   Check(Object.ReferenceEquals(oldDevice,v.Water.Prerequisite10B8.Read(0)),"Unknown provider consumed by TEST before +58/device store");
  });
  Scenario(delegate{
   var v=View();v.Water.Prerequisite10B8.Write(new object());var device=new Field<object>();var provider=new GroundWaterRenderProviderView(device);var i=new Invocation();
   Missing(delegate{Initialize(v,Field<object>.Of(null),Field<object>.Of(null),Engine(Field<object>.Of(provider)),i);},0x1f6ede);
   Missing(delegate{v.Water.Prerequisite10B8.Read(14);},14);Missing(delegate{device.Read(15);},15);
   Check(i.At==0x1f6ede,"Unknown device copied into same Water field before native TEST stop");
  });
  Scenario(delegate{
   var v=View();object oldTree=new object();v.Water.Prerequisite10B0.Write(oldTree);var provider=new GroundWaterRenderProviderView(Field<object>.Of(null));var i=new Invocation();
   var returned=Initialize(v,Field<object>.Of(null),Field<object>.Of(null),Engine(Field<object>.Of(provider)),i);
   Check(returned==GroundWaterInitializePrefixReturn.NoRenderDevice&&v.Water.Prerequisite10B8.Read(0)==null&&i.At==0x1f702a,"Device-null is published then actual void-return branch");
   Check(Object.ReferenceEquals(oldTree,v.Water.Prerequisite10B0.Read(0)),"No invented tree clear or creation on device-null return");
  });
  Scenario(delegate{
   var v=View();object terrain=new object(),device=new object();v.Terrain10C0.Write(new object());var provider=new GroundWaterRenderProviderView(Field<object>.Of(device));var i=new Invocation();
   Missing(delegate{Initialize(v,Field<object>.Of(terrain),v.Terrain10C0,Engine(Field<object>.Of(provider)),i);},0x1f6ee7);
   Check(Object.ReferenceEquals(terrain,v.Grid10C8.Read(0)),"Stack Grid load observes earlier Terrain write for exact OWN cell alias");
   Check(Object.ReferenceEquals(device,v.Water.Prerequisite10B8.Read(0))&&i.At==0x1f6ee7,"Same device identity published before unresolved initializer helper");
   Missing(delegate{v.Water.Prerequisite10B0.Read(16);},16);Missing(delegate{v.Water.LayerCountA0.Read(17);},17);
  });
  Scenario(delegate{
   var v=View();var providerCell=Field<object>.Of(null);var engine=Engine(providerCell);
   Check(Initialize(v,Field<object>.Of(null),Field<object>.Of(null),engine,new Invocation())==GroundWaterInitializePrefixReturn.NoRenderProvider,"First live provider read observes null");
   var deviceCell=Field<object>.Of(null);providerCell.Write(new GroundWaterRenderProviderView(deviceCell));
   Check(Initialize(v,Field<object>.Of(null),Field<object>.Of(null),engine,new Invocation())==GroundWaterInitializePrefixReturn.NoRenderDevice,"Next call rereads same authoritative provider cell");
   object device=new object();deviceCell.Write(device);Missing(delegate{Initialize(v,Field<object>.Of(null),Field<object>.Of(null),engine,new Invocation());},0x1f6ee7);
   Check(Object.ReferenceEquals(device,v.Water.Prerequisite10B8.Read(0)),"Borrowed provider device mutation visible without copying/syncing");
  });
  Console.WriteLine("{\"status\":\"passed\",\"scenarios\":"+scenarios+",\"assertions\":"+assertions+",\"scope\":\"observed constructor prefix and Initialize render gates only; no native full initialization\",\"native_constructor_initialize\":false,\"original_read\":false,\"gameplay_accepted\":false}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}}
}
