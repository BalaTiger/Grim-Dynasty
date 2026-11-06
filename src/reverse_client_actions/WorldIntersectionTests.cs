using System;
using System.IO;
using System.Security.Cryptography;
using GrimDynasty.ReverseClient.Actions;

// Explicit OWN scene publication. No Level result callback, original DLL or game startup.
public static class WorldIntersectionTestProgram {
 const string Root=@"C:\Users\zhuzi\Documents\Grim-Dynasty";
 const string MemberHash="a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289";
 static int checks,cases;
 static float Float(uint v){return BitConverter.ToSingle(BitConverter.GetBytes(v),0);}
 static uint Bits(float v){return BitConverter.ToUInt32(BitConverter.GetBytes(v),0);}
 static void Check(bool v,string why){checks++;if(!v)throw new Exception(why);}
 static void Case(Action f){f();cases++;}
 static void Unknown<T>(Field<T> f,string why){bool stopped=false;try{f.Read(0);}catch(MissingContract){stopped=true;}Check(stopped,why);}
 static void Stops(Action f,uint address){try{f();throw new Exception("Expected Unsupported");}catch(MissingContract e){Check(e.Address==address,"Stop address "+e.Address.ToString("x")+" expected "+address.ToString("x"));}}
 sealed class Scene {
  public readonly WorldCoordinateState Coordinates=new WorldCoordinateState();
  public readonly GroundTriangleOperands Geometry;
  public readonly WorldFloorContext Floor;
  public readonly WorldIntersectionWorldView World;
  public readonly WorldIntersectionRegionNode Head=new WorldIntersectionRegionNode(),Node=new WorldIntersectionRegionNode();
  public readonly WorldRegion Region=new WorldRegion(new object());
  public readonly WorldFloorRegionView Published;
  public readonly WorldRayObserved36 Ray=new WorldRayObserved36();
  public readonly WorldIntersectionObserved44 Output=new WorldIntersectionObserved44();
  public readonly OwnReentrantRegionLock Lock=OwnReentrantRegionLock.InitializeOwn();
  public readonly Invocation Invocation=new Invocation();
  public Scene(GroundTriangleOperands geometry=null){
   Geometry=geometry??new GroundTriangleOperands();
   Geometry.SelectObservedDiskOperands();Floor=new WorldFloorContext(new WorldPublicationRegistry(Coordinates),new WorldFloorOperands(Geometry));
   World=new WorldIntersectionWorldView(new object(),Floor);Published=World.BindRegion(77,Region);
   World.HeadC0.Write(Head);Head.Next00.Write(Node);Node.Next00.Write(Head);Node.Region10.Write(Region);
   Region.Offset3C.Write(0);Region.Offset40.Write(0);Region.Offset44.Write(0);Box(10,0,0,1);
   Ray.Origin00.Q0.Write(77);Ray.Origin00.D8.Write(Bits(0));Ray.Origin00.DC.Write(Bits(0));Ray.Origin00.D10.Write(Bits(0));
   Ray.Direction18.X.Write(1);Ray.Direction18.Y.Write(0);Ray.Direction18.Z.Write(0);
   Output.Distance00.Write(123);Output.Unknown04.Write(0x12345678);Output.Position08.Q0.Write(456);
   Output.Position08.D8.Write(0xabcdef01);Output.Position08.DC.Write(0xabcdef02);Output.Position08.D10.Write(0xabcdef03);Output.Position08.D14.Write(0xabcdef04);
   Output.Entity20.Write(new object());Output.Surface28.Write(999);
  }
  public void Box(float x,float y,float z,float extent){Region.Bounds70.Center00.Write(x);Region.Bounds70.Center04.Write(y);Region.Bounds70.Center08.Write(z);Region.Bounds70.Extent0C.Write(extent);Region.Bounds70.Extent10.Write(extent);Region.Bounds70.Extent14.Write(extent);}
  public void Run(float maximum){WorldIntersectionContracts.GetIntersection220840(World,Ray,Output,0,false,maximum,false,Floor,Invocation);}
  public void Pick(){var move=new MoveContext();var engine=new WorldEngine();engine.World50.Write(World.Identity);move.EngineGlobal.Write(engine);WidgetGroundTargetContracts.MainPath2DDC0(Ray,Output,move,World,Field<uint>.Of(0x7f800000),Invocation);}
  public void Miss(){
   Check(float.IsPositiveInfinity(Output.Distance00.Read(0))&&Output.Entity20.Read(0)==null,"Only miss distance and entity reset");
   Check(Output.Unknown04.Read(0)==0x12345678&&Output.Surface28.Read(0)==999,"Padding and material remain unchanged on miss");
   Check(Output.Position08.Q0.Read(0)==456&&Output.Position08.D8.Read(0)==0xabcdef01&&Output.Position08.DC.Read(0)==0xabcdef02&&Output.Position08.D10.Read(0)==0xabcdef03&&Output.Position08.D14.Read(0)==0xabcdef04,"Entire old coordinate survives miss");
  }
 }
 sealed class SavedGround {
  public readonly OwnGroundData Data;
  public readonly GroundSpatialTree Tree=new GroundSpatialTree();
  public readonly GroundSpatialContext Context;
  public readonly TerrainMaterialCollisionAdapter Materials;
  public readonly float Top;
  public readonly double Triangle;
  public SavedGround(){
   byte[] member=File.ReadAllBytes(Path.Combine(Root,@"docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin"));
   using(var sha=SHA256.Create())Check(member.Length==1091357&&BitConverter.ToString(sha.ComputeHash(member)).Replace("-","").ToLowerInvariant()==MemberHash,"Pinned real saved map member");
   byte[] chunk=new byte[149073];Buffer.BlockCopy(member,273260,chunk,0,chunk.Length);
   Data=OwnGroundColdLoader.DecodeType2Chunk(chunk,MemberHash+":273260+149073");Data.SelectObservedScalarSlot();
   var records=new byte[5][];for(int n=0;n<5;n++)records[n]=File.ReadAllBytes(Path.Combine(Root,@"docs\reverse-client\r1-terrain-records\terrain-"+n+".json"));
   byte[] report=File.ReadAllBytes(Path.Combine(Root,@"docs\reverse-client\r1-terrain-surface-strings\inspection.json"));
   var evidence=TerrainSurfaceOperandEvidence.FromSavedReport(report,TerrainSurfaceOperandEvidence.SavedReportHash,TerrainSurfaceOperandEvidence.SavedReaderHash);
   Materials=TerrainMaterialCollisionAdapter.FromSavedRecords(Data,records,evidence);Materials.BindAll();
   var geometry=new GroundGeometryOperands();geometry.SelectObservedDiskOperands();
   var blocks=GroundGeometryContracts.CreateOwnBlockBounds(Data,geometry,new Invocation());
   var states=new GroundBlockMaterialState[blocks.Length];float top=float.NegativeInfinity;
   for(int n=0;n<blocks.Length;n++){states[n]=new GroundBlockMaterialState(blocks[n]);states[n].Populate(geometry,new Invocation());top=Math.Max(top,blocks[n].Box10.Center04.Read(0)+blocks[n].Box10.Extent10.Read(0));}
   Top=top;
   var terrain=new GroundTerrainIntersectionBinding(Data);terrain.SelectObservedDiskSlots();
   var operands=new GroundBlockIntersectionOperands();operands.SelectObservedDiskOperands();
   Context=new GroundSpatialContext(terrain,operands);Context.SelectObservedDiskOperands();
   GroundSpatialTreeContracts.PublishOwnColdBlocks(Tree,Data,states,Context,new Invocation());
   var cells=Data.Cells128.Read(0);uint a=cells.Read(8320,0),b=cells.Read(8449,0),c=cells.Read(8450,0);
   Check(a==0x41819c63&&b==0x41801dd7&&c==0x41800932,"Saved triangle vertices");Triangle=.25*Float(a)+.5*Float(b)+.25*Float(c);
  }
 }
 static WorldFloorLevelView AttachGround(Scene s,SavedGround ground,WorldRegion region,WorldFloorRegionView publication){
  region.Bounds70.Center00.Write(64);region.Bounds70.Center04.Write(64);region.Bounds70.Center08.Write(64);
  region.Bounds70.Extent0C.Write(512);region.Bounds70.Extent10.Write(512);region.Bounds70.Extent14.Write(512);
  region.Unavailable90.Write(0);region.Critical18.Write(s.Lock);
  var level=s.World.BindLevel(new object());publication.Publication.Level68.Write(level.Level);
  var landscape=new WorldFloorLandscape(new object());landscape.Ground20.Write(new WorldFloorGroundOwner(ground.Data,ground.Tree,ground.Context));level.Landscape30.Write(landscape);
  // Explicit OWN static scene. This is not proof of native loaded Spatial10.
  level.Level.Spatial10.Write(null);level.Water40.Write(new GroundWaterState());return level;
 }
 static void CameraRay(Scene s,float height){
  var engine=new PortalGameEngine();var table=new PortalCameraVTable();var targets=new CameraTargetBoundaries();
  var boundaries=new EvidenceCameraBoundaries(new MoveContext(),new PortalContext(),targets);
  var camera=new PortalCameraState(engine.Camera13B0);var target=new CameraTargetState(engine.Camera13B0);
  boundaries.Attach(camera);boundaries.AttachTarget(target);engine.Camera13B0.Virtual00.Write(table);
  target.Target120.Q0.Write(77);target.Target120.D8.Write(Bits(64.25f));target.Target120.DC.Write(Bits(16));target.Target120.D10.Write(Bits(64.75f));
  target.Offset55C.X.Write(0);target.Offset55C.Y.Write(0);target.Offset55C.Z.Write(0);target.Count138.Write(0);
  camera.Position94.Write((ulong)Bits(64.25f)|((ulong)Bits(height)<<32));camera.Position9C.Write(Bits(64.75f));
  float[] basis={1,0,0,0,0,1,0,-1,0};for(int n=0;n<9;n++)camera.Basis70[n].Write(basis[n]);
  var image=new CameraImageState(camera);image.Mode6C.Write(1);image.WidthA4.Write(2);image.HeightA8.Write(2);
  var context=new CameraWorldRayContext(boundaries,s.Coordinates);context.Attach(image);context.BindObservedGameCamera(table);context.UseObservedConstants();
  var point=new CameraImagePoint2(Field<float>.Of(.5f),Field<float>.Of(.5f));
  Check(Object.ReferenceEquals(s.Ray,CameraWorldRayContracts.GetRay229700(engine.Camera13B0,s.Ray,point,null,context,new Invocation())),"Actual camera ray writes supplied output");
  Check(s.Ray.Origin00.Q0.Read(0)==77&&s.Ray.Direction18.Y.Read(0)==-1,"Real camera contract produces the region-aware downward ray");
 }
 static void Hit(Scene s,SavedGround ground,ulong token){
  float y=Float(s.Output.Position08.DC.Read(0));
  Check(s.Output.Position08.Q0.Read(0)==token&&Float(s.Output.Position08.D8.Read(0))==64.25f&&Float(s.Output.Position08.D10.Read(0))==64.75f,"Actual hit coordinates and candidate region");
  Check(Math.Abs(y-ground.Triangle)<.0002&&Math.Abs(s.Output.Distance00.Read(0)-(ground.Top+32-ground.Triangle))<.0002,"World result agrees with independent real-map triangle oracle");
  Check(s.Output.Entity20.Read(0)==null&&s.Output.Surface28.Read(0)==ground.Materials.MaterialViewAt(1).SurfaceFC.Read(0),"Real Ground surface with no invented Entity");
  Unknown(s.Output.Position08.D14,"Native copied unwritten stack tail remains unknown");Check(s.Output.Unknown04.Read(0)==0x12345678,"Unwritten result+4 remains old");
 }
 public static int Main(string[] args){try{
  if(args.Length!=0)throw new Exception("No arguments");
  Case(delegate{var s=new Scene();s.Head.Next00.Write(s.Head);s.Ray.Origin00.Q0.Forget();s.Run(float.NaN);s.Miss();Check(s.Invocation.At==0x220a40,"Empty list returns without consuming ray or maxDistance");});
  Case(delegate{var s=new Scene();s.Head.Next00.Write(s.Head);s.Geometry.Infinity338458.Forget();s.Run(1);Unknown(s.Output.Distance00,"Opaque unknown infinity propagates without a fabricated value");Check(s.Output.Entity20.Read(0)==null&&s.Output.Position08.Q0.Read(0)==456,"Other native prefix still executes");});
  Case(delegate{var s=new Scene();s.World.HeadC0.Forget();Stops(delegate{s.Run(1);},0x220875);s.Miss();});
  Case(delegate{var s=new Scene();s.Region.Offset3C.Forget();Stops(delegate{s.Run(1);},0x2208b7);s.Miss();});
  Case(delegate{var s=new Scene();s.Run(1);s.Miss();Unknown(s.Region.Unavailable90,"Rejected bounds do not invent availability");Unknown(s.Region.Critical18,"Rejected bounds never enter unknown lock");});
  Case(delegate{var s=new Scene();s.Ray.Origin00.Q0.Write(0);s.Ray.Origin00.D8.Forget();s.Ray.Origin00.DC.Forget();s.Ray.Origin00.D10.Forget();s.Region.Offset3C.Forget();s.Region.Offset40.Forget();s.Region.Offset44.Forget();s.Region.Unavailable90.Write(1);s.Run(9);s.Miss();Unknown(s.Ray.Origin00.D8,"Null origin region does not initialize original XYZ");Unknown(s.Region.Critical18,"Equal broad distance continues to unavailable check, not lock");s.Region.Unavailable90.Forget();Stops(delegate{s.Run(9);},0x220955);});
  Case(delegate{var s=new Scene();s.Region.Unavailable90.Write(0);s.Region.Critical18.Write(s.Lock);s.Published.Publication.Level68.Write(null);s.Lock.Enter(0);try{s.Run(9);s.Miss();Check(s.Lock.CurrentThreadDepth==1,"Nested existing lock ownership retained");Unknown(s.Published.Publication.Reset88,"No unrelated GuaranteedGetLevel reset");}finally{s.Lock.Leave(0);}Check(!s.Lock.IsHeldByCurrentThread,"No lock leak");});
  Case(delegate{var s=new Scene();s.Region.Unavailable90.Write(0);s.Region.Critical18.Write(s.Lock);Stops(delegate{s.Run(9);},0x220980);Check(!s.Lock.IsHeldByCurrentThread,"Unknown Level consumed after Leave");s.Miss();});
  Case(delegate{var s=new Scene();s.Region.Unavailable90.Write(1);s.Run(9);s.Region.Unavailable90.Forget();s.Box(20,0,0,1);s.Run(9);s.Miss();Unknown(s.Region.Unavailable90,"New shared bounds read before availability");});
  var ground=new SavedGround();
  Case(delegate{var s=new Scene(ground.Context.IntersectionOperands.Geometry);var level=AttachGround(s,ground,s.Region,s.Published);CameraRay(s,ground.Top+32);s.Pick();Hit(s,ground,77);Unknown(level.Water40.Read(0).LayerCountA0,"Surface0 real Water early-out does not require empty layers");Check(!s.Lock.IsHeldByCurrentThread,"Completed hit released shared lock");});
  Case(delegate{var s=new Scene(ground.Context.IntersectionOperands.Geometry);AttachGround(s,ground,s.Region,s.Published);CameraRay(s,ground.Top+32);s.Node.Next00.Forget();Stops(delegate{s.Run(float.PositiveInfinity);},0x220a08);Hit(s,ground,77);});
  Case(delegate{var s=new Scene(ground.Context.IntersectionOperands.Geometry);AttachGround(s,ground,s.Region,s.Published);CameraRay(s,ground.Top+32);var second=new WorldRegion(new object());second.Offset3C.Write(0);second.Offset40.Write(0);second.Offset44.Write(0);var published=s.World.BindRegion(88,second);AttachGround(s,ground,second,published);var next=new WorldIntersectionRegionNode();next.Region10.Write(second);next.Next00.Write(s.Head);s.Node.Next00.Write(next);s.Run(float.PositiveInfinity);Hit(s,ground,77);s.Head.Next00.Write(next);next.Next00.Write(s.Node);s.Node.Next00.Write(s.Head);s.Run(float.PositiveInfinity);Hit(s,ground,88);});
  Case(delegate{var s=new Scene(ground.Context.IntersectionOperands.Geometry);var level=AttachGround(s,ground,s.Region,s.Published);CameraRay(s,ground.Top+32);level.Level.Spatial10.Write(new object());Stops(delegate{s.Run(float.PositiveInfinity);},0x1301de);s.Miss();Check(!s.Lock.IsHeldByCurrentThread,"Unknown entity query stops after release without committing incomplete Level hit");});
  Case(delegate{var s=new Scene();s.Region.Unavailable90.Write(1);Stops(delegate{s.Pick();},0x2de47);s.Miss();});
  Case(delegate{var s=new Scene();var move=new MoveContext();var engine=new WorldEngine();engine.World50.Write(new object());move.EngineGlobal.Write(engine);Stops(delegate{WidgetGroundTargetContracts.MainPath2DDC0(s.Ray,s.Output,move,s.World,Field<uint>.Of(0x7f800000),s.Invocation);},0x2de24);Check(s.Output.Distance00.Read(0)==123&&s.Output.Entity20.Read(0)!=null,"Wrong shared World does not start query");});
  Case(delegate{var s=new Scene();var level=s.World.BindLevel(new object());s.Region.Unavailable90.Write(0);s.Region.Critical18.Write(s.Lock);s.Published.Publication.Level68.Write(level.Level);level.Landscape30.Write(null);level.TerrainFallback38.Write(null);level.Level.Spatial10.Write(null);level.Water40.Write(null);Stops(delegate{s.Run(9);},0x2209cb);s.Miss();Check(!s.Lock.IsHeldByCurrentThread,"Uninitialized Level result fails at comparison after lock release");});
  Case(delegate{var s=new Scene();s.Ray.Direction18.X.Write(-1);Stops(delegate{s.Run(float.PositiveInfinity);},0x220955);s.Miss();});
  Console.WriteLine("{\"status\":\"passed-world-intersection-ground\",\"scenarios\":"+cases+",\"assertions\":"+checks+",\"real_saved_ground_hit\":true,\"actual_world_and_level_contracts\":true,\"actual_camera_ray\":true,\"widget_primary_hit\":true,\"native_level_success\":false,\"native_scene_initialization\":false,\"native_math_calls\":0,\"original_read\":false,\"original_executed\":false,\"backend_started\":false,\"gameplay_accepted\":false}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
