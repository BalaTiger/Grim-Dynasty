using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using GrimDynasty.ReverseClient.Actions;

// Six new composition scenarios. Reuses the passed Floor and material adapter
// DLLs; no old test entrypoint, original input, navigation height or Game bridge.
public static class RealGroundFloorIntegrationTestProgram {
 const string Root=@"C:\Users\zhuzi\Documents\Grim-Dynasty";
 const string MemberRelative=@"docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin";
 const string ReportRelative=@"docs\reverse-client\r1-terrain-surface-strings\inspection.json";
 const string MemberHash="a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289";
 static int assertions,scenarios;
 static float floorY,floorDistance;
 static double triangleHeight,bilinearHeight;
 static void Check(bool value,string label){assertions++;if(!value)throw new Exception(label);}
 static void Scenario(Action action){action();scenarios++;}
 static uint Bits(float value){return BitConverter.ToUInt32(BitConverter.GetBytes(value),0);}
 static float Float(uint value){return BitConverter.ToSingle(BitConverter.GetBytes(value),0);}
 static string Hash(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
 static void Missing(Action action,uint at){assertions++;try{action();}catch(MissingContract e){if(e.Address!=at)throw new Exception("Expected stop "+at.ToString("x")+", got "+e.Address.ToString("x"));return;}throw new Exception("Expected stop "+at.ToString("x"));}
 static void Unknown<T>(Field<T> value){Missing(delegate{value.Read(0xf001);},0xf001);}
 static WorldPosition24 Position(float x,float z){var p=new WorldPosition24();p.Q0.Write(77);p.D8.Write(Bits(x));p.DC.Write(Bits(29.5f));p.D10.Write(Bits(z));p.D14.Write(0xaabbccddu);return p;}
 static void Unchanged(WorldPosition24 p,float x,float z){Check(p.Q0.Read(0)==77&&p.D8.Read(0)==Bits(x)&&p.DC.Read(0)==Bits(29.5f)&&p.D10.Read(0)==Bits(z)&&p.D14.Read(0)==0xaabbccddu,"Stopped/false Floor preserves every caller cell");}
 static byte[] ReadSaved(string relative){return File.ReadAllBytes(Path.Combine(Root,relative));}
 static string RecordRelative(int index){return @"docs\reverse-client\r1-terrain-records\terrain-"+index+".json";}
 sealed class Scene {
  public readonly OwnGroundData Data;
  public readonly TerrainMaterialCollisionAdapter Materials;
  public readonly WorldFloorGroundOwner Ground;
  public readonly GroundWaterState Water;
  public readonly WorldCoordinateState Coordinates=new WorldCoordinateState();
  public readonly WorldPublicationRegistry Registry;
  public readonly WorldFloorContext Floor;
  public readonly WorldRegion Region=new WorldRegion(new object());
  public readonly WorldFloorRegionView RegionView;
  public readonly WorldFloorLevelView Level;
  public readonly OwnReentrantRegionLock Sync=OwnReentrantRegionLock.InitializeOwn();
  public Scene(byte[] member,byte[][] records,byte[] operandReport){
   var chunk=new byte[149073];Buffer.BlockCopy(member,273260,chunk,0,chunk.Length);
   Data=OwnGroundColdLoader.DecodeType2Chunk(chunk,MemberHash+":273260+149073");Data.SelectObservedScalarSlot();
   var evidence=TerrainSurfaceOperandEvidence.FromSavedReport(operandReport,TerrainSurfaceOperandEvidence.SavedReportHash,TerrainSurfaceOperandEvidence.SavedReaderHash);
   Materials=TerrainMaterialCollisionAdapter.FromSavedRecords(Data,records,evidence);Materials.BindAll();
   var geometry=new GroundGeometryOperands();geometry.SelectObservedDiskOperands();var blocks=GroundGeometryContracts.CreateOwnBlockBounds(Data,geometry,new Invocation());var states=new GroundBlockMaterialState[blocks.Length];
   for(int n=0;n<blocks.Length;n++){states[n]=new GroundBlockMaterialState(blocks[n]);states[n].Populate(geometry,new Invocation());}
   var binding=new GroundTerrainIntersectionBinding(Data);binding.SelectObservedDiskSlots();var operands=new GroundBlockIntersectionOperands();operands.SelectObservedDiskOperands();var spatial=new GroundSpatialContext(binding,operands);spatial.SelectObservedDiskOperands();
   var tree=new GroundSpatialTree();GroundSpatialTreeContracts.PublishOwnColdBlocks(tree,Data,states,spatial,new Invocation());Ground=new WorldFloorGroundOwner(Data,tree,spatial);
   // Real saved24B empty payload with explicitly OWN opaque prerequisites;
   // never a claim that native Water constructor/Initialize has executed.
   var waterBytes=new byte[24];Buffer.BlockCopy(member,422341,waterBytes,0,24);Water=new GroundWaterState();Water.Prerequisite10B0.Write(new object());Water.Prerequisite10B8.Write(new object());
   var reader=new GroundWaterChunkReader(waterBytes,MemberHash+":422341+24; OWN opaque prerequisites");Check(GroundWaterContracts.Load1F81E0(Water,reader,new Invocation())&&reader.Cursor08.Read(0)==24,"Saved empty Water payload establishes live counts");
   Registry=new WorldPublicationRegistry(Coordinates);var floorOperands=new WorldFloorOperands(operands.Geometry);floorOperands.SelectObservedFloorDiskOperands();Floor=new WorldFloorContext(Registry,floorOperands);
   RegionView=Floor.BindRegion(77,Region);Level=Floor.BindLevel(new object());Region.Unavailable90.Write(0);Region.Critical18.Write(Sync);RegionView.Publication.Level68.Write(Level.Level);RegionView.Publication.Reset88.Write(99);
   var landscape=new WorldFloorLandscape(Data);landscape.Ground20.Write(Ground);Level.Landscape30.Write(landscape);Level.Water40.Write(Water);
   // Actor and neighbor vectors deliberately remain Unknown here. Only the
   // explicit OWN publication methods below supply their scene contents.
  }
  public void PublishOwnActors(WorldFloorActorView[] actors,string sourceIdentity){Level.PathableActors.PublishOwnContents(0,actors,sourceIdentity);}
  public void PublishOwnNeighbors(RegionPublicationView[] neighbors,string sourceIdentity){RegionView.Neighbors.PublishOwnContents(0,neighbors,sourceIdentity);}
  public byte Put(WorldPosition24 position){return WorldFloorContracts.PutOnFloor221DA0(null,position,Floor,new Invocation());}
 }
 static void SharedIdentities(Scene scene){
  Check(Object.ReferenceEquals(scene.Data,scene.Materials.Data)&&Object.ReferenceEquals(scene.Data,scene.Ground.Data)&&Object.ReferenceEquals(scene.Data,scene.Ground.Context.Terrain.Data),"Material adapter, Ground owner and actual query borrow one saved data object");
  Check(Object.ReferenceEquals(scene.Region,scene.Coordinates.Region(77,0))&&Object.ReferenceEquals(scene.RegionView.Publication,scene.Registry.BindRegionView(77,scene.Region)),"Floor uses same canonical region/publication");
  Check(Object.ReferenceEquals(scene.Level.Level,scene.Registry.BindLevelView(scene.Level.Level.Identity))&&Object.ReferenceEquals(scene.RegionView.Publication.Level68.Read(0),scene.Level.Level),"Floor borrows same canonical Level");
  Check(Object.ReferenceEquals(scene.Level.Water40.Read(0),scene.Water)&&Object.ReferenceEquals(scene.Floor.Operands.Geometry,scene.Ground.Context.IntersectionOperands.Geometry),"Water and geometry operands retain authoritative identities");
  for(uint n=0;n<5;n++)Check(Object.ReferenceEquals(scene.Materials.RecordAt(n),scene.Data.TypeRecordAt(n,0))&&Object.ReferenceEquals(scene.Data.TypeRecordAt(n,0).Material.Read(0),scene.Materials.MaterialViewAt(n)),"Same live saved record/material identity "+n);
  Check(!scene.Sync.IsHeldByCurrentThread&&scene.Sync.CurrentThreadDepth==0&&scene.RegionView.Publication.Reset88.Read(0)==99,"Floor released shared lock and left LoadLevel-only reset untouched");
 }
 static void ActualCentralHit(Scene scene){
  var ray=scene.Floor.LastRay;var hit=scene.Floor.LastGroundHit;
  Check(ray.Origin00.X0.Read(0)==64.25f&&ray.Origin00.Y4.Read(0)==32&&ray.Origin00.Z8.Read(0)==64.75f&&ray.Direction0C.Y4.Read(0)==-1,"Actual Floor ray uses original coordinates and observed operands");
  float y=hit.Position04.Y4.Read(0),distance=hit.Distance00.Read(0);Check(distance>0&&distance<32&&Math.Abs((double)y-triangleHeight)<0.0002,"Actual Ground hit agrees with independent saved triangle height");
  Check(Math.Abs((double)y-bilinearHeight)>0.0006,"Triangle collision result is not replaced by bilinear sampling");
  Check(hit.Position04.X0.Read(0)==64.25f&&hit.Position04.Z8.Read(0)==64.75f&&hit.Material10.Read(0)==scene.Materials.MaterialViewAt(1).SurfaceFC.Read(0),"Real saved Grass material and collision XYZ flow through Floor");
 }
 public static int Main(string[] args){try{
  if(args.Length!=1||!String.Equals(args[0],Root,StringComparison.Ordinal))throw new Exception("Fixed workspace argument required");
  byte[] member=ReadSaved(MemberRelative);Check(member.Length==1091357&&Hash(member)==MemberHash,"Pinned saved129x129 member");var records=new byte[5][];var recordHashes=new string[5];for(int n=0;n<5;n++){records[n]=ReadSaved(RecordRelative(n));recordHashes[n]=Hash(records[n]);}
  byte[] operandReport=ReadSaved(ReportRelative);Check(Hash(operandReport)==TerrainSurfaceOperandEvidence.SavedReportHash,"Pinned actual surface operand receipt");var scene=new Scene(member,records,operandReport);
  // Independent geometric oracle at(64.25,64.75): saved Face1CF420 first
  // triangle A00,B01,C11 has barycentric weights(.25,.5,.25). Double
  // precision oracle allows0.0002 binary32 plane/intersection roundoff.
  var cells=scene.Data.Cells128.Read(0);uint[] indices={8320,8321,8449,8450};uint[] expected={0x41819c63u,0x41816b17u,0x41801dd7u,0x41800932u};var h=new double[4];
  for(int n=0;n<4;n++){uint value=cells.Read(indices[n],0);Check(value==expected[n],"Pinned saved center vertex bits "+n);h[n]=Float(value);}
  triangleHeight=.25*h[0]+.5*h[2]+.25*h[3];bilinearHeight=.25*(.75*h[0]+.25*h[1])+.75*(.75*h[2]+.25*h[3]);Check(Math.Abs(triangleHeight-bilinearHeight)>.0008,"Saved cell distinguishes collision triangle from bilinear height");

  Scenario(delegate{
   var p=Position(64.25f,64.75f);Unknown(scene.Level.PathableActors.End);Unknown(scene.RegionView.Neighbors.End);Missing(delegate{scene.Put(p);},0x13028a);
   ActualCentralHit(scene);SharedIdentities(scene);Check(float.IsPositiveInfinity(scene.Floor.LastWaterHit.Distance00.Read(0)),"Saved empty Water actually queried before Unknown actor boundary");Unknown(scene.Floor.LastWaterHit.Material10);Unchanged(p,64.25f,64.75f);
  });
  Scenario(delegate{
   scene.PublishOwnActors(new WorldFloorActorView[0],"OWN integration scene explicitly establishes empty actors; native loaded population unverified");var p=Position(64.25f,64.75f);
   Missing(delegate{scene.Put(p);},0x221f2e);ActualCentralHit(scene);SharedIdentities(scene);Unchanged(p,64.25f,64.75f);Unknown(scene.RegionView.Neighbors.End);
  });
  Scenario(delegate{
   scene.PublishOwnNeighbors(new RegionPublicationView[0],"OWN integration scene explicitly establishes empty neighbors; native loaded population unverified");var p=Position(64.25f,64.75f);var alias=new WorldPosition24(p);
   Check(scene.Put(alias)==1,"Saved real Ground and five materials reach actual Floor true under explicit OWN actor/neighbor state");ActualCentralHit(scene);SharedIdentities(scene);
   floorY=Float(p.DC.Read(0));floorDistance=scene.Floor.LastGroundHit.Distance00.Read(0);Check(p.Q0.Read(0)==77&&p.D8.Read(0)==Bits(64.25f)&&p.D10.Read(0)==Bits(64.75f)&&p.DC.Read(0)==Bits(scene.Floor.LastGroundHit.Position04.Y4.Read(0)),"Caller alias receives actual collision height while retaining region and XZ");Unknown(p.D14);
   Check(Math.Abs((double)floorY-triangleHeight)<.0002&&Math.Abs((double)floorDistance-(32-triangleHeight))<.0002,"True output and distance match independent triangle oracle");
   Unknown(scene.Level.Level.Spatial10);Unknown(scene.Level.Level.Ready538C);Unknown(scene.Level.TerrainFallback38);
  });
  Scenario(delegate{
   var boundaries=new WorldCoordinateBoundaries(scene.Coordinates);var engine=new WorldEngine();boundaries.EngineGlobal.Write(engine);var p=Position(64.25f,64.75f);
   Check(WorldFloorContracts.PutOnFloor22C620(p,boundaries,scene.Floor,new Invocation())==1,"Existing wrapper completes through the same real-data Floor scene");ActualCentralHit(scene);SharedIdentities(scene);
   Check(p.DC.Read(0)==Bits(floorY)&&p.Q0.Read(0)==77&&p.D8.Read(0)==Bits(64.25f)&&p.D10.Read(0)==Bits(64.75f),"Wrapper copies real collision coordinates to caller");Check(!Object.ReferenceEquals(p.AddressIdentity,scene.Floor.LastWrapperTemporary.AddressIdentity),"Wrapper retains its own fresh temporary");Unknown(p.D14);Unknown(scene.Floor.LastWrapperTemporary.D14);Unknown(engine.World50);
  });
  Scenario(delegate{
   // Withdraw only the live selected material pointer. This must reach the
   // actual Block stop, proving Floor has no detached material-success cache.
   var material=scene.Materials.MaterialViewAt(1);scene.Data.TypeRecordAt(1,0).Material.Forget();var p=Position(64.25f,64.75f);Missing(delegate{scene.Put(p);},0x1d1528);
   Check(float.IsPositiveInfinity(scene.Floor.LastGroundHit.Distance00.Read(0))&&scene.Floor.LastGroundHit.Material10.Read(0)==100,"Unresolved live material preserves outer Floor hit initialization");Unknown(scene.Floor.LastWaterHit.Distance00);Unchanged(p,64.25f,64.75f);
   Check(Object.ReferenceEquals(scene.Materials.BindRecord(1),material),"Restoring the explicit binding reuses original material identity");SharedIdentities(scene);
  });
  Scenario(delegate{
   var p=Position(-1000,-1000);Check(scene.Put(p)==0,"Actual saved Ground external ray miss returns normal false with established OWN lists");Unchanged(p,-1000,-1000);SharedIdentities(scene);
   Check(float.IsPositiveInfinity(scene.Floor.LastGroundHit.Distance00.Read(0))&&float.IsPositiveInfinity(scene.Floor.LastWaterHit.Distance00.Read(0)),"Actual Ground and saved empty Water both retain miss distance");
   Check(Hash(ReadSaved(MemberRelative))==MemberHash&&Hash(ReadSaved(ReportRelative))==TerrainSurfaceOperandEvidence.SavedReportHash,"Saved input and operand receipt unchanged");for(int n=0;n<5;n++)Check(Hash(ReadSaved(RecordRelative(n)))==recordHashes[n],"Saved record unchanged "+n);
  });
  Console.WriteLine("{\"status\":\"passed\",\"scenarios\":"+scenarios+",\"assertions\":"+assertions+",\"real_ground_cells\":16641,\"material_records\":5,\"saved_ground_floor_in_own_scene\":true,\"same_material_identity\":true,\"explicit_own_actor_neighbor_state\":true,\"unknown_actor_stop\":\"13028a\",\"unknown_neighbor_stop\":\"221f2e\",\"live_material_stop\":\"1d1528\",\"floor_y\":"+floorY.ToString("R",CultureInfo.InvariantCulture)+",\"collision_distance\":"+floorDistance.ToString("R",CultureInfo.InvariantCulture)+",\"triangle_height_oracle\":"+triangleHeight.ToString("R",CultureInfo.InvariantCulture)+",\"bilinear_height_counterexample\":"+bilinearHeight.ToString("R",CultureInfo.InvariantCulture)+",\"nav_height_used\":false,\"native_scene_startup\":false,\"native_water_constructor_initialize\":false,\"real_crowd_game_wired\":false,\"old_movement_wrapper_wired\":false,\"original_read\":false,\"original_executed\":false,\"gameplay_accepted\":false,\"saved_member_sha256\":\""+MemberHash+"\"}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}}
}
