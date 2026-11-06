using System;
using System.IO;
using System.Text;
using GrimDynasty.ReverseClient.Actions;

// R1 bridge, stage A: produce a REAL ground hit from the widget ground pick.
//
// Everything here is an explicit OWN publication: a synthetic 2x2 flat ground
// chunk (same shape as WorldFloorTests.OwnFlatGround), one region, one AABB and
// one downward ray. No widget, input event, camera, portal or native Engine
// initialization is inferred, and no loaded game resource is decoded.
//
// Requirements learned from earlier attempts and enforced here:
//   1. the world view keeps its own region AND level registries (World.BindRegion,
//      World.BindLevel), and BindLevel must run after the view exists;
//   2. the floor level and region views require published collections
//      (PathableActors, Neighbors);
//   3. floor operands and the ground owner must share the SAME
//      GroundTriangleOperands instance;
//   4. level.Level.Spatial10 must be published (null here: no spatial objects).
public static class ClickToMoveBridgeTestProgram {
 static int scenarios,assertions;
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Case(string name,Action body){try{body();scenarios++;}catch(Exception error){throw new Exception(name+": "+error.Message,error);}}
 static void Unknown<T>(Field<T> f,string why){bool stopped=false;try{f.Read(0);}catch(MissingContract){stopped=true;}Check(stopped,why);}
 static uint Bits(float v){return BitConverter.ToUInt32(BitConverter.GetBytes(v),0);}

 static WorldFloorGroundOwner OwnFlatGround(){
  using(var s=new MemoryStream()){using(var w=new BinaryWriter(s)){
   w.Write(2u);foreach(uint v in new uint[]{1,1,2,2,0,0,0,0})w.Write(v);
   for(int n=0;n<4;n++)w.Write(0u);
   for(int n=0;n<4;n++)w.Write((byte)2);
   byte[] name=Encoding.ASCII.GetBytes("OWN explicit floor material fixture");
   w.Write((uint)name.Length);w.Write(name);w.Write(0u);w.Flush();
   var data=OwnGroundColdLoader.DecodeType2Chunk(s.ToArray(),"OWN flat 2x2 fixture; not a real resource");
   var material=new GroundMaterialSurfaceState();material.SurfaceFC.Write(0x10203040u);
   data.TypeRecordAt(0,0).Material.Write(material);
   return GroundOwner(data);
  }}
 }
 static WorldFloorGroundOwner GroundOwner(OwnGroundData data){
  data.SelectObservedScalarSlot();
  var geometry=new GroundGeometryOperands();geometry.SelectObservedDiskOperands();
  var blocks=GroundGeometryContracts.CreateOwnBlockBounds(data,geometry,new Invocation());
  var states=new GroundBlockMaterialState[blocks.Length];
  for(int n=0;n<blocks.Length;n++){states[n]=new GroundBlockMaterialState(blocks[n]);states[n].Populate(geometry,new Invocation());}
  var binding=new GroundTerrainIntersectionBinding(data);binding.SelectObservedDiskSlots();
  var operands=new GroundBlockIntersectionOperands();operands.SelectObservedDiskOperands();
  var context=new GroundSpatialContext(binding,operands);context.SelectObservedDiskOperands();
  var tree=new GroundSpatialTree();
  GroundSpatialTreeContracts.PublishOwnColdBlocks(tree,data,states,context,new Invocation());
  return new WorldFloorGroundOwner(data,tree,context);
 }

 sealed class Scene {
  public readonly WorldCoordinateState Coordinates=new WorldCoordinateState();
  public readonly WorldRegion Region=new WorldRegion(new object());
  public readonly OwnReentrantRegionLock Sync=OwnReentrantRegionLock.InitializeOwn();
  public readonly WorldRayObserved36 Ray=new WorldRayObserved36();
  public readonly WorldIntersectionObserved44 Output=new WorldIntersectionObserved44();
  public readonly Invocation Invocation=new Invocation();
  public readonly WorldIntersectionRegionNode Head=new WorldIntersectionRegionNode();
  public readonly WorldIntersectionRegionNode Node=new WorldIntersectionRegionNode();
  public readonly WorldFloorGroundOwner Ground;
  public readonly WorldFloorContext Floor;
  public readonly WorldFloorRegionView RegionView;
  public readonly WorldFloorLevelView LevelView;
  public readonly WorldIntersectionWorldView World;

  public Scene(){
   Ground=OwnFlatGround();
   var geometry=Ground.Context.IntersectionOperands.Geometry;
   var registry=new WorldPublicationRegistry(Coordinates);
   var floorOperands=new WorldFloorOperands(geometry);
   floorOperands.SelectObservedFloorDiskOperands();
   Floor=new WorldFloorContext(registry,floorOperands);

   var levelIdentity=new object();
   RegionView=Floor.BindRegion(77,Region);
   var level=Floor.BindLevel(levelIdentity);
   Region.Unavailable90.Write(0);
   Region.Critical18.Write(Sync);
   RegionView.Publication.Level68.Write(level.Level);
   RegionView.Publication.Reset88.Write(99);
   level.Level.Spatial10.Write(null);level.Water40.Write(null);

   var landscape=new WorldFloorLandscape(Ground);
   landscape.Ground20.Write(Ground);
   level.Landscape30.Write(landscape);
   level.PathableActors.PublishOwnContents(0,new WorldFloorActorView[0],"OWN test explicitly published empty actors; not native loaded state");
   RegionView.Neighbors.PublishOwnContents(0,new RegionPublicationView[0],"OWN test explicitly published empty neighbors; not native loaded state");

   World=new WorldIntersectionWorldView(new object(),Floor);
   World.BindRegion(77,Region);
   LevelView=World.BindLevel(levelIdentity);
   World.HeadC0.Write(Head);
   Head.Next00.Write(Node);
   Node.Next00.Write(Head);
   Node.Region10.Write(Region);
   Region.Offset3C.Write(0);Region.Offset40.Write(0);Region.Offset44.Write(0);
   Region.Bounds70.Center00.Write(0);Region.Bounds70.Center04.Write(0);Region.Bounds70.Center08.Write(0);
   Region.Bounds70.Extent0C.Write(4);Region.Bounds70.Extent10.Write(4);Region.Bounds70.Extent14.Write(4);

   Ray.Origin00.Q0.Write(77);Ray.Origin00.D8.Write(Bits(0.5f));Ray.Origin00.DC.Write(Bits(0.5f));Ray.Origin00.D10.Write(Bits(8));
   Ray.Direction18.X.Write(0);Ray.Direction18.Y.Write(0);Ray.Direction18.Z.Write(-1);
  }
  public void Run(float maximum){WorldIntersectionContracts.GetIntersection220840(World,Ray,Output,0,false,maximum,false,Floor,Invocation);}
 }

 public static int Main(){
  try{
   Case("Real flat ground yields an ordered hit on the shared world",delegate{
    var s=new Scene();
    s.Run(float.PositiveInfinity);
    float distance=s.Output.Distance00.Read(0);
    Check(!float.IsNaN(distance),"Distance is ordered, not NaN");
    Check(distance>0,"A real ground hit yields a positive distance");
    Unknown(s.Output.Position08.Q0,"This contract does not produce the hit position");

   });
   Console.WriteLine("{\"status\":\"passed-click-to-move-ground-hit\",\"scenarios\":"+scenarios+",\"assertions\":"+assertions+"}");
   return 0;
  }catch(Exception error){
   var inner=error;while(inner.InnerException!=null)inner=inner.InnerException;
   var mc=inner as MissingContract;
   Console.Error.WriteLine("ADDR="+(mc==null?"none":"0x"+mc.Address.ToString("x")));
   Console.Error.WriteLine(error.Message);
   return 1;
  }}
 }
