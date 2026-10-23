using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using GrimDynasty.ReverseClient.Actions;

// New Floor composition tests only. Passed Tree/Water/Publication suites are
// dependencies, never invoked here. Real saved data and OWN fixtures are named
// separately; no callback can inject an intersection or a successful floor.
public static class WorldFloorTestProgram {
 const string SavedPath=@"C:\Users\zhuzi\Documents\Grim-Dynasty\docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin";
 const string SavedHash="a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289";
 static int assertions,scenarios;
 static bool realUnknownStop,ownTrue,wrapperTrue;
 static void Check(bool value,string label){assertions++;if(!value)throw new Exception(label);}
 static void Scenario(Action action){action();scenarios++;}
 static uint Bits(float v){return BitConverter.ToUInt32(BitConverter.GetBytes(v),0);}
 static string Hash(byte[] v){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(v)).Replace("-","").ToLowerInvariant();}
 static void Missing(Action action,uint at){assertions++;try{action();}catch(MissingContract e){if(e.Address!=at)throw new Exception("Expected stop "+at.ToString("x")+", got "+e.Address.ToString("x"));return;}throw new Exception("Missing expected stop "+at.ToString("x"));}
 static void Unknown<T>(Field<T> field){Missing(delegate{field.Read(0xf001);},0xf001);}
 static void Invalid(Action action){bool found=false;try{action();}catch(InvalidOperationException){found=true;}Check(found,"Conflicting OWN publication must fail");}
 static Displacement3 Point(float x,float y,float z){var p=new Displacement3();p.X0.Write(x);p.Y4.Write(y);p.Z8.Write(z);return p;}
 static WorldPosition24 Position(ulong token,float x,float y,float z){var p=new WorldPosition24();p.Q0.Write(token);p.D8.Write(Bits(x));p.DC.Write(Bits(y));p.D10.Write(Bits(z));p.D14.Write(0xaabbccddu);return p;}
 static void Unchanged(WorldPosition24 p,ulong token,float x,float y,float z){Check(p.Q0.Read(0)==token&&p.D8.Read(0)==Bits(x)&&p.DC.Read(0)==Bits(y)&&p.D10.Read(0)==Bits(z)&&p.D14.Read(0)==0xaabbccddu,"Failed or stopped floor preserves all caller cells");}
 static void Box(WorldBounds24 b,float x,float y,float z,float ex,float ey,float ez){b.Center00.Write(x);b.Center04.Write(y);b.Center08.Write(z);b.Extent0C.Write(ex);b.Extent10.Write(ey);b.Extent14.Write(ez);}
 static GroundTriangleOperands Geometry(){var g=new GroundTriangleOperands();g.SelectObservedDiskOperands();return g;}
 sealed class Fixture {
  public readonly WorldCoordinateState Coordinates=new WorldCoordinateState();
  public readonly WorldRegion Region=new WorldRegion(new object());
  public readonly WorldPublicationRegistry Registry;
  public readonly WorldFloorContext Context;
  public readonly WorldFloorRegionView RegionView;
  public readonly WorldFloorLevelView Level;
  public readonly OwnReentrantRegionLock Sync=OwnReentrantRegionLock.InitializeOwn();
  public Fixture(GroundTriangleOperands geometry){
   Registry=new WorldPublicationRegistry(Coordinates);var operands=new WorldFloorOperands(geometry);operands.SelectObservedFloorDiskOperands();Context=new WorldFloorContext(Registry,operands);
   RegionView=Context.BindRegion(77,Region);Level=Context.BindLevel(new object());Region.Unavailable90.Write(0);Region.Critical18.Write(Sync);RegionView.Publication.Level68.Write(Level.Level);RegionView.Publication.Reset88.Write(99);
  }
  public byte Core(WorldPosition24 p){return WorldFloorContracts.PutOnFloor221DA0(null,p,Context,new Invocation());}
  public void NoGround(){var landscape=new WorldFloorLandscape(new object());landscape.Ground20.Write(null);Level.Landscape30.Write(landscape);}
  public void ExplicitEmptyActors(){Level.PathableActors.PublishOwnContents(0,new WorldFloorActorView[0],"OWN test explicitly published empty actors; not native loaded state");}
  public void ExplicitEmptyNeighbors(){RegionView.Neighbors.PublishOwnContents(0,new RegionPublicationView[0],"OWN test explicitly published empty neighbors; not native loaded state");}
  public void Ground(WorldFloorGroundOwner owner){var landscape=new WorldFloorLandscape(owner);landscape.Ground20.Write(owner);Level.Landscape30.Write(landscape);}
  public WorldFloorRegionView Neighbor(ulong token){return Context.BindRegion(token,new WorldRegion(new object()));}
 }
 static WorldFloorGroundOwner GroundOwner(OwnGroundData data){
  data.SelectObservedScalarSlot();var geometry=new GroundGeometryOperands();geometry.SelectObservedDiskOperands();
  var blocks=GroundGeometryContracts.CreateOwnBlockBounds(data,geometry,new Invocation());var states=new GroundBlockMaterialState[blocks.Length];
  for(int n=0;n<blocks.Length;n++){states[n]=new GroundBlockMaterialState(blocks[n]);states[n].Populate(geometry,new Invocation());}
  var binding=new GroundTerrainIntersectionBinding(data);binding.SelectObservedDiskSlots();var operands=new GroundBlockIntersectionOperands();operands.SelectObservedDiskOperands();
  var context=new GroundSpatialContext(binding,operands);context.SelectObservedDiskOperands();var tree=new GroundSpatialTree();
  GroundSpatialTreeContracts.PublishOwnColdBlocks(tree,data,states,context,new Invocation());return new WorldFloorGroundOwner(data,tree,context);
 }
 static WorldFloorGroundOwner OwnFlatGround(){
  using(var s=new MemoryStream()){using(var w=new BinaryWriter(s)){
   w.Write(2u);foreach(uint v in new uint[]{1,1,2,2,0,0,0,0})w.Write(v);for(int n=0;n<4;n++)w.Write(0u);for(int n=0;n<4;n++)w.Write((byte)2);
   byte[] name=Encoding.ASCII.GetBytes("OWN explicit floor material fixture");w.Write((uint)name.Length);w.Write(name);w.Write(0u);w.Flush();
   var data=OwnGroundColdLoader.DecodeType2Chunk(s.ToArray(),"OWN flat 2x2 fixture; not a real resource");var material=new GroundMaterialSurfaceState();material.SurfaceFC.Write(0x10203040u);data.TypeRecordAt(0,0).Material.Write(material);return GroundOwner(data);
  }}
 }
 static GroundWaterState SavedEmptyWater(byte[] chunk){
  var water=new GroundWaterState();water.Prerequisite10B0.Write(new object());water.Prerequisite10B8.Write(new object());
  var reader=new GroundWaterChunkReader(chunk,SavedHash+":422341+24; explicit OWN opaque nonnull prerequisites");
  Check(GroundWaterContracts.Load1F81E0(water,reader,new Invocation())&&reader.Cursor08.Read(0)==24,"Actual saved empty Water payload accepted with explicitly OWN prerequisites");return water;
 }
 static void Released(Fixture f){Check(!f.Sync.IsHeldByCurrentThread&&f.Sync.CurrentThreadDepth==0,"Region lock released before subsequent field consumption");Check(f.RegionView.Publication.Reset88.Read(0)==99,"Floor does not reuse LoadLevel/Reset88 behavior");}
 static void Offsets(WorldRegion region,int x,int y,int z){region.Offset3C.Write(x);region.Offset40.Write(y);region.Offset44.Write(z);}
 static void NeighborLevel(Fixture f,WorldFloorRegionView region,WorldFloorLevelView level,OwnReentrantRegionLock sync){region.Publication.Region.Unavailable90.Write(0);region.Publication.Region.Critical18.Write(sync);region.Publication.Level68.Write(level==null?null:level.Level);}

 public static int Main(string[] args){try{
  if(args.Length!=1||!String.Equals(args[0],SavedPath,StringComparison.OrdinalIgnoreCase))throw new Exception("Fixed saved member argument required");
  byte[] member=File.ReadAllBytes(SavedPath);Check(member.Length==1091357&&Hash(member)==SavedHash,"Saved member identity before Floor-only tests");
  var groundChunk=new byte[149073];Buffer.BlockCopy(member,273260,groundChunk,0,groundChunk.Length);var waterChunk=new byte[24];Buffer.BlockCopy(member,422341,waterChunk,0,24);
  Scenario(delegate{
   var f=new Fixture(Geometry());Check(Object.ReferenceEquals(f.RegionView,f.Context.BindRegion(77,f.Region))&&Object.ReferenceEquals(f.RegionView.Publication,f.Registry.BindRegionView(77,f.Region)),"Floor borrows canonical region view");
   Check(Object.ReferenceEquals(f.Level,f.Context.BindLevel(f.Level.Level.Identity))&&Object.ReferenceEquals(f.Level.Level,f.Registry.BindLevelView(f.Level.Level.Identity)),"Floor borrows canonical Level");
   Unknown(f.Level.Landscape30);Unknown(f.Level.TerrainFallback38);Unknown(f.Level.Water40);Unknown(f.Level.PathableActors54D8);Unknown(f.Level.PathableActors54E0);Unknown(f.RegionView.Neighbor100);Unknown(f.RegionView.Neighbor108);Unknown(f.Level.Level.Ready538C);Unknown(f.Level.Level.Spatial10);
   f.RegionView.SelectOwnColdNeighborWrites191980();Check(f.RegionView.Neighbor100.Read(0)==0&&f.RegionView.Neighbor108.Read(0)==0&&f.RegionView.Neighbors.CapacityEnd.Read(0)==0,"Explicit selection applies only saved cold neighbor writes");
   f.RegionView.Neighbors.PublishOwnContents(0x1000,new[]{f.RegionView.Publication},"OWN publication replaces selected cold vector");Check(f.RegionView.Neighbor108.Read(0)==0x1008,"Cold state does not force later publication empty");Invalid(delegate{f.RegionView.SelectOwnColdNeighborWrites191980();});
  });
  Scenario(delegate{
   var p=new WorldPosition24();p.Q0.Write(0);Check(WorldFloorContracts.PutOnFloor22C620(p,null,null,new Invocation())==0,"Null region wrapper skips boundaries/context/coordinates");Unknown(p.D8);Unknown(p.D14);
  });
  Scenario(delegate{
   var f=new Fixture(Geometry());f.Region.Unavailable90.Write(1);f.Region.Critical18.Forget();f.RegionView.Publication.Level68.Forget();var p=new WorldPosition24();p.Q0.Write(77);
   Check(f.Core(p)==0&&f.Context.LastRay==null,"Unavailable region returns before lock and coordinate reads");Unknown(p.D8);Unknown(f.Region.Critical18);
  });
  Scenario(delegate{
   var f=new Fixture(Geometry());f.RegionView.Publication.Level68.Write(null);var p=new WorldPosition24();p.Q0.Write(77);Check(f.Core(p)==0&&f.Context.LastRay==null,"Known null Level returns after release before ray");Released(f);Unknown(p.D8);
   f.RegionView.Publication.Level68.Forget();Missing(delegate{f.Core(p);},0x221de2);Released(f);
  });
  Scenario(delegate{
   var f=new Fixture(Geometry());f.Region.Critical18.Forget();var p=Position(77,.25f,7.5f,.75f);Missing(delegate{f.Core(p);},0x221dce);Check(!f.Sync.IsHeldByCurrentThread,"Unknown lock never acquires own lock");Unchanged(p,77,.25f,7.5f,.75f);
  });
  Scenario(delegate{
   var f=new Fixture(Geometry());f.Context.Operands.RayYOffset34BC00.Forget();var p=Position(77,.25f,7.5f,.75f);Missing(delegate{f.Core(p);},0x221e00);Released(f);Unknown(f.Context.LastRay.Origin00.Z8);Unchanged(p,77,.25f,7.5f,.75f);
   f.Context.Operands.SelectObservedFloorDiskOperands();p.DC.Forget();Missing(delegate{f.Core(p);},0x221e00);Unknown(f.Context.LastRay.Origin00.Z8);
  });
  Scenario(delegate{
   var f=new Fixture(Geometry());f.NoGround();f.Level.Water40.Write(null);f.ExplicitEmptyActors();f.ExplicitEmptyNeighbors();var p=Position(77,.25f,7.5f,.75f);p.D8.Forget();p.D10.Forget();f.Context.Operands.RayDirectionY34BDF4.Forget();
   Check(f.Core(p)==0,"Opaque MOV of Unknown X/Z/direction does not stop an established +Inf miss");Unknown(f.Context.LastRay.Origin00.X0);Unknown(f.Context.LastRay.Origin00.Z8);Unknown(f.Context.LastRay.Direction0C.Y4);Check(f.Context.LastRay.Origin00.Y4.Read(0)==10,"Actual Y arithmetic still occurs");
   f.Level.Water40.Write(SavedEmptyWater(waterChunk));Check(f.Core(p)==0,"Saved empty Water also leaves opaque ray fields unconsumed");Unknown(p.D8);Unknown(p.D10);Check(p.D14.Read(0)==0xaabbccddu,"Normal false does not copy Unknown temporary tail");
  });
  Scenario(delegate{
   var f=new Fixture(Geometry());f.Level.Landscape30.Write(null);f.Level.TerrainFallback38.Write(null);f.Level.Water40.Write(null);var p=Position(77,.25f,7.5f,.75f);
   Missing(delegate{f.Core(p);},0x221f02);Unknown(f.Context.LastGroundHit.Distance00);Unknown(f.Context.LastGroundHit.Material10);Released(f);Unchanged(p,77,.25f,7.5f,.75f);
  });
  Scenario(delegate{
   var f=new Fixture(Geometry());f.Level.Landscape30.Write(null);f.Level.TerrainFallback38.Write(new object());Missing(delegate{f.Core(Position(77,.25f,7.5f,.75f));},0x221ea6);Unknown(f.Level.Water40);Released(f);
   f.NoGround();Missing(delegate{f.Core(Position(77,.25f,7.5f,.75f));},0x221ec3);Check(float.IsPositiveInfinity(f.Context.LastGroundHit.Distance00.Read(0))&&f.Context.LastGroundHit.Material10.Read(0)==100,"Nonnull landscape initializes miss and skips fallback");
  });
  Scenario(delegate{
   var f=new Fixture(Geometry());f.NoGround();f.Level.Water40.Write(null);var p=Position(77,.25f,7.5f,.75f);Missing(delegate{f.Core(p);},0x13028a);Unknown(f.RegionView.Neighbor108);Released(f);Unchanged(p,77,.25f,7.5f,.75f);
   f.ExplicitEmptyActors();Missing(delegate{f.Core(p);},0x221f2e);f.ExplicitEmptyNeighbors();Check(f.Core(p)==0,"Explicit empty lists with known Ground null preserve +Inf normal false");Unchanged(p,77,.25f,7.5f,.75f);
  });
  Scenario(delegate{
   var f=new Fixture(Geometry());f.ExplicitEmptyActors();f.Context.Operands.Geometry.XorMask34C450.Forget();
   Check(WorldFloorContracts.PathableActorIntersection130270(f.Level,null,3,true,null,new Invocation())==3,"Established empty actor list returns incoming without ray/context/mask reads");
  });
  Scenario(delegate{
   var f=new Fixture(Geometry());var box=new WorldBounds24();box.Center00.Write(0);box.Center08.Write(0);box.Extent0C.Write(1);var actor=new WorldFloorActorView(new object(),box);
   f.Level.PathableActors.PublishOwnContents(0x2000,new[]{actor},"OWN one live actor bounds fixture");var ray=new GroundRay24(Point(5,0,0),Point(0,-1,0));f.Context.Operands.Geometry.XorMask34C450.Forget();
   Check(float.IsPositiveInfinity(WorldFloorContracts.PathableActorIntersection130270(f.Level,ray,float.PositiveInfinity,false,f.Context,new Invocation())),"Upper X rejection does not consume captured Unknown XOR or Y/Z extents");Unknown(box.Extent14);Unknown(box.Center04);
   box.Extent14.Write(1);ray.Origin00.X0.Write(0);Missing(delegate{WorldFloorContracts.PathableActorIntersection130270(f.Level,ray,2,false,f.Context,new Invocation());},0x13034c);
   f.Context.Operands.Geometry.SelectObservedDiskOperands();Box(box,0,10,0,1,1,1);Check(WorldFloorContracts.PathableActorIntersection130270(f.Level,ray,2,false,f.Context,new Invocation())==2,"Actor bottom above ray rejects");
   box.Center04.Write(0);ray.Origin00.Y4.Write(5);Check(WorldFloorContracts.PathableActorIntersection130270(f.Level,ray,2,true,f.Context,new Invocation())==2,"Actor top below segment end rejects");
   ray.Origin00.Y4.Write(0);Missing(delegate{WorldFloorContracts.PathableActorIntersection130270(f.Level,ray,2,true,f.Context,new Invocation());},0x130395);
  });
  Scenario(delegate{
   var f=new Fixture(Geometry());f.NoGround();f.Level.Water40.Write(null);f.ExplicitEmptyActors();var unavailable=f.Neighbor(88);unavailable.Publication.Region.Unavailable90.Write(1);var noLevel=f.Neighbor(99);var sync=OwnReentrantRegionLock.InitializeOwn();NeighborLevel(f,noLevel,null,sync);
   f.RegionView.Neighbors.PublishOwnContents(0x3000,new[]{unavailable.Publication,noLevel.Publication},"OWN unavailable/null-Level neighbor contents");f.Context.Operands.Geometry.AbsMask34C410.Forget();f.Context.Operands.NeighborMargin34BC8C.Forget();
   var p=Position(77,.25f,7.5f,.75f);Check(f.Core(p)==0,"Skipped neighbors never consume captured Unknown margin/mask; +Inf remains normal false");Unknown(unavailable.Publication.Region.Critical18);Unknown(noLevel.Publication.Region.Bounds70.Extent0C);Check(!sync.IsHeldByCurrentThread,"Null neighbor Level is tested after release");Unchanged(p,77,.25f,7.5f,.75f);
  });
  Scenario(delegate{
   var f=new Fixture(Geometry());f.NoGround();f.Level.Water40.Write(null);f.ExplicitEmptyActors();var neighbor=f.Neighbor(88);var nl=f.Context.BindLevel(new object());var sync=OwnReentrantRegionLock.InitializeOwn();NeighborLevel(f,neighbor,nl,sync);Offsets(f.Region,0,0,0);Offsets(neighbor.Publication.Region,0,0,0);
   neighbor.Publication.Region.Bounds70.Extent0C.Write(1);neighbor.Publication.Region.Bounds70.Center00.Write(0);f.RegionView.Neighbors.PublishOwnContents(0x4000,new[]{neighbor.Publication},"OWN live bounds-filter neighbor");var p=Position(77,100,7.5f,.75f);
   f.Context.Operands.NeighborMargin34BC8C.Forget();Missing(delegate{f.Core(p);},0x22202a);Check(!sync.IsHeldByCurrentThread,"Neighbor lock released before first margin arithmetic");
   f.Context.Operands.SelectObservedFloorDiskOperands();f.Context.Operands.Geometry.AbsMask34C410.Forget();Missing(delegate{f.Core(p);},0x22203a);f.Context.Operands.Geometry.SelectObservedDiskOperands();
   Check(f.Core(p)==0,"X-bound rejection skips Z bounds and Unknown neighbor actors");Unknown(nl.PathableActors.End);Unknown(neighbor.Publication.Region.Bounds70.Center08);
   p=Position(77,.25f,7.5f,100);neighbor.Publication.Region.Bounds70.Center08.Write(0);neighbor.Publication.Region.Bounds70.Extent14.Write(1);Check(f.Core(p)==0,"Z-bound rejection skips Unknown neighbor actors");
  });
  Scenario(delegate{
   var f=new Fixture(Geometry());f.NoGround();f.Level.Water40.Write(null);f.ExplicitEmptyActors();var neighbor=f.Neighbor(88);var nl=f.Context.BindLevel(new object());var sync=OwnReentrantRegionLock.InitializeOwn();NeighborLevel(f,neighbor,nl,sync);
   Offsets(f.Region,10,20,30);Offsets(neighbor.Publication.Region,8,17,26);Box(neighbor.Publication.Region.Bounds70,2.25f,0,4.75f,1,1,1);f.RegionView.Neighbors.PublishOwnContents(0x5000,new[]{neighbor.Publication},"OWN translated eligible neighbor");var p=Position(77,.25f,7.5f,.75f);
   Missing(delegate{f.Core(p);},0x13028a);Check(!sync.IsHeldByCurrentThread,"Unknown neighbor actors reached after releasing lock");
   var box=new WorldBounds24();Box(box,2.25f,13,4.75f,1,1,1);nl.PathableActors.PublishOwnContents(0x6000,new[]{new WorldFloorActorView(new object(),box)},"OWN translated actor candidate");Missing(delegate{f.Core(p);},0x130395);
   box.Center04.Write(15);Check(f.Core(p)==0,"Translated neighbor ray origin Y13 rejects actor bottom14; no fabricated hit");Unchanged(p,77,.25f,7.5f,.75f);
  });
  Scenario(delegate{
   var data=OwnGroundColdLoader.DecodeType2Chunk(groundChunk,SavedHash+":273260+149073");var ground=GroundOwner(data);var f=new Fixture(ground.Context.IntersectionOperands.Geometry);f.Ground(ground);var p=Position(77,64.25f,29.5f,64.75f);
   Missing(delegate{f.Core(p);},0x1d1528);Check(Object.ReferenceEquals(ground.Data,ground.Context.Terrain.Data),"Saved data remains same shared Ground binding");
   Check(float.IsPositiveInfinity(f.Context.LastGroundHit.Distance00.Read(0))&&f.Context.LastGroundHit.Material10.Read(0)==100,"Real material stop does not publish local triangle hit into floor output");
   Unknown(f.Level.Water40);Unknown(f.Level.PathableActors.End);Unknown(f.RegionView.Neighbors.End);for(uint n=0;n<data.TypeRecordCount;n++)Unknown(data.TypeRecordAt(n,0).Material);Released(f);Unchanged(p,77,64.25f,29.5f,64.75f);realUnknownStop=true;
  });
  Scenario(delegate{
   var ground=OwnFlatGround();var f=new Fixture(ground.Context.IntersectionOperands.Geometry);f.Ground(ground);f.Level.Water40.Write(SavedEmptyWater(waterChunk));f.ExplicitEmptyActors();f.ExplicitEmptyNeighbors();var p=Position(77,.25f,7.5f,.75f);var alias=new WorldPosition24(p);
   Check(f.Core(alias)==1,"Explicit OWN floor reaches normal true through actual Ground tree and real empty Water payload");Check(p.Q0.Read(0)==77&&p.D8.Read(0)==Bits(.25f)&&p.DC.Read(0)==Bits(0)&&p.D10.Read(0)==Bits(.75f),"Same-address output uses original ray and distance10");Unknown(p.D14);
   Check(f.Context.LastGroundHit.Distance00.Read(0)==10&&f.Context.LastGroundHit.Material10.Read(0)==0x10203040u,"Actual OWN triangle/material result propagated");Check(float.IsPositiveInfinity(f.Context.LastWaterHit.Distance00.Read(0)),"Saved empty Water contributes no nearer hit");Unknown(f.Context.LastWaterHit.Material10);Released(f);ownTrue=true;
  });
  Scenario(delegate{
   var ground=OwnFlatGround();var material=(GroundMaterialSurfaceState)ground.Data.TypeRecordAt(0,0).Material.Read(0);material.SurfaceFC.Forget();var f=new Fixture(ground.Context.IntersectionOperands.Geometry);f.Ground(ground);f.Level.Water40.Write(null);f.ExplicitEmptyActors();f.ExplicitEmptyNeighbors();var p=Position(77,.25f,7.5f,.75f);
   Check(f.Core(p)==1&&p.DC.Read(0)==Bits(0),"OWN valid material identity with Unknown FC remains opaque copied data, not a success prerequisite");Unknown(f.Context.LastGroundHit.Material10);Unknown(p.D14);
  });
  Scenario(delegate{
   var ground=OwnFlatGround();var f=new Fixture(ground.Context.IntersectionOperands.Geometry);f.Ground(ground);f.Level.Water40.Write(null);var p=Position(77,.25f,-2.5f,.75f);
   Check(f.Core(p)==1&&f.Context.LastGroundHit.Distance00.Read(0)==0,"Actual zero-distance Ground hit succeeds without actors/neighbors");Unknown(f.Level.PathableActors.End);Unknown(f.RegionView.Neighbors.End);Unknown(p.D14);
  });
  Scenario(delegate{
   var ground=OwnFlatGround();var f=new Fixture(ground.Context.IntersectionOperands.Geometry);f.Ground(ground);f.Level.Water40.Write(SavedEmptyWater(waterChunk));f.ExplicitEmptyActors();f.ExplicitEmptyNeighbors();var boundaries=new WorldCoordinateBoundaries(f.Coordinates);var engine=new WorldEngine();boundaries.EngineGlobal.Write(engine);var p=Position(77,.25f,7.5f,.75f);
   Check(WorldFloorContracts.PutOnFloor22C620(p,boundaries,f.Context,new Invocation())==1,"Wrapper reaches actual OWN floor normal true");Check(p.DC.Read(0)==Bits(0)&&!Object.ReferenceEquals(p.AddressIdentity,f.Context.LastWrapperTemporary.AddressIdentity),"Wrapper uses fresh temporary then output copy");Unknown(p.D14);Unknown(f.Context.LastWrapperTemporary.D14);Unknown(engine.World50);wrapperTrue=true;
  });
  Scenario(delegate{
   var f=new Fixture(Geometry());f.Region.Unavailable90.Write(1);var boundaries=new WorldCoordinateBoundaries(f.Coordinates);boundaries.EngineGlobal.Write(new WorldEngine());var p=Position(77,.25f,7.5f,.75f);
   Check(WorldFloorContracts.PutOnFloor22C620(p,boundaries,f.Context,new Invocation())==0,"Wrapper normal false preserves caller despite prepared temporary");Unchanged(p,77,.25f,7.5f,.75f);Unknown(f.Context.LastWrapperTemporary.D14);
   var wrong=new WorldCoordinateBoundaries(new WorldCoordinateState());Missing(delegate{WorldFloorContracts.PutOnFloor22C620(p,wrong,f.Context,new Invocation());},0x22c643);Unchanged(p,77,.25f,7.5f,.75f);
  });
  Check(Hash(File.ReadAllBytes(SavedPath))==SavedHash,"Saved member unchanged after Floor-only tests");
  Console.WriteLine("{\"status\":\"passed\",\"scenarios\":"+scenarios+",\"assertions\":"+assertions+",\"real_saved_ground_unknown_material_stop\":"+(realUnknownStop?"true":"false")+",\"explicitly_own_established_true\":"+(ownTrue?"true":"false")+",\"own_wrapper_true\":"+(wrapperTrue?"true":"false")+",\"real_material_binding_exercised\":false,\"real_map_floor_success\":false,\"native_load\":false,\"native_water_constructor_initialize\":false,\"old_movement_wrapper_wired\":false,\"gameplay_accepted\":false,\"saved_member_sha256\":\""+SavedHash+"\"}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}}
}
