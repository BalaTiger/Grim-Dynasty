using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using GrimDynasty.ReverseClient.Actions;

// Two new composition cases, not a rerun of earlier Camera or Ground suites.
// Real GameCamera target getters and044A10/229700 feed actual saved Ground
// geometry/material/tree queries. Camera state, normalized image coordinates,
// same-region Ground selection, query flags and output initialization are OWN
// inputs; no physical mouse, native scene startup or native pick caller is claimed.
public static class CameraGroundRayIntegrationTestProgram {
 const string Root=@"C:\Users\zhuzi\Documents\Grim-Dynasty";
 const string MemberRelative=@"docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin";
 const string ReportRelative=@"docs\reverse-client\r1-terrain-surface-strings\inspection.json";
 const string MemberHash="a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289";
 const ulong RegionToken=77;
 static int assertions,scenarios;
 static float hitY,hitDistance,cameraY;
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Scenario(Action body){body();scenarios++;}
 static uint Bits(float value){return BitConverter.ToUInt32(BitConverter.GetBytes(value),0);}
 static float Float(uint bits){return BitConverter.ToSingle(BitConverter.GetBytes(bits),0);}
 static float F(float value){return Float(Bits(value));}
 static ulong Pair(float x,float y){return (ulong)Bits(x)|((ulong)Bits(y)<<32);}
 static string Hash(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
 static byte[] ReadSaved(string relative){return File.ReadAllBytes(Path.Combine(Root,relative));}
 static string RecordRelative(int n){return @"docs\reverse-client\r1-terrain-records\terrain-"+n+".json";}
 static void Unknown<T>(Field<T> field,string why){bool unknown=false;try{field.Read(0);}catch(MissingContract){unknown=true;}Check(unknown,why);}
 static void Vec(CameraVector3 value,float x,float y,float z){value.X.Write(x);value.Y.Write(y);value.Z.Write(z);}

 sealed class SavedGround {
  public readonly OwnGroundData Data;
  public readonly TerrainMaterialCollisionAdapter Materials;
  public readonly GroundSpatialTree Tree=new GroundSpatialTree();
  public readonly GroundSpatialContext Context;
  public readonly float HighestBlockTop;
  public SavedGround(byte[] member,byte[][] records,byte[] report){
   var chunk=new byte[149073];Buffer.BlockCopy(member,273260,chunk,0,chunk.Length);
   Data=OwnGroundColdLoader.DecodeType2Chunk(chunk,MemberHash+":273260+149073");Data.SelectObservedScalarSlot();
   var evidence=TerrainSurfaceOperandEvidence.FromSavedReport(report,TerrainSurfaceOperandEvidence.SavedReportHash,TerrainSurfaceOperandEvidence.SavedReaderHash);
   Materials=TerrainMaterialCollisionAdapter.FromSavedRecords(Data,records,evidence);Materials.BindAll();
   var geometry=new GroundGeometryOperands();geometry.SelectObservedDiskOperands();
   var blocks=GroundGeometryContracts.CreateOwnBlockBounds(Data,geometry,new Invocation());
   var states=new GroundBlockMaterialState[blocks.Length];float top=float.NegativeInfinity;
   for(int n=0;n<blocks.Length;n++){
    states[n]=new GroundBlockMaterialState(blocks[n]);states[n].Populate(geometry,new Invocation());
    top=Math.Max(top,F(blocks[n].Box10.Center04.Read(0)+blocks[n].Box10.Extent10.Read(0)));
   }
   HighestBlockTop=top;
   var binding=new GroundTerrainIntersectionBinding(Data);binding.SelectObservedDiskSlots();
   var operands=new GroundBlockIntersectionOperands();operands.SelectObservedDiskOperands();
   Context=new GroundSpatialContext(binding,operands);Context.SelectObservedDiskOperands();
   GroundSpatialTreeContracts.PublishOwnColdBlocks(Tree,Data,states,Context,new Invocation());
  }
 }

 sealed class OwnCamera {
  public readonly PortalGameEngine Engine=new PortalGameEngine();
  public readonly PortalCameraVTable Table=new PortalCameraVTable();
  public readonly CameraTargetBoundaries Targets=new CameraTargetBoundaries();
  public readonly EvidenceCameraBoundaries Boundaries;
  public readonly PortalCameraState Base;
  public readonly CameraTargetState Target;
  public readonly WorldCoordinateState Regions=new WorldCoordinateState();
  public readonly WorldRegion Region=new WorldRegion(new object());
  public readonly CameraImageState Image;
  public readonly CameraWorldRayContext Context;
  public readonly CameraImagePoint2 Input=new CameraImagePoint2(Field<float>.Of(.5f),Field<float>.Of(.5f));
  public OwnCamera(float height){
   Base=new PortalCameraState(Engine.Camera13B0);Target=new CameraTargetState(Engine.Camera13B0);
   Boundaries=new EvidenceCameraBoundaries(new MoveContext(),new PortalContext(),Targets);
   Boundaries.Attach(Base);Boundaries.AttachTarget(Target);Engine.Camera13B0.Virtual00.Write(Table);
   Target.Target120.Q0.Write(RegionToken);Target.Target120.D8.Write(Bits(64.25f));Target.Target120.DC.Write(Bits(16));Target.Target120.D10.Write(Bits(64.75f));
   Vec(Target.Offset55C,0,0,0);Target.Count138.Write(0);
   Base.Position94.Write(Pair(64.25f,height));Base.Position9C.Write(Bits(64.75f));
   float[] basis={1,0,0,0,0,1,0,-1,0};for(int n=0;n<9;n++)Base.Basis70[n].Write(basis[n]);
   Region.Offset3C.Write(100);Region.Offset40.Write(-200);Region.Offset44.Write(300);Regions.AttachRegion(RegionToken,Region);
   Image=new CameraImageState(Base);Image.Mode6C.Write(1);Image.WidthA4.Write(2);Image.HeightA8.Write(2);
   Context=new CameraWorldRayContext(Boundaries,Regions);Context.Attach(Image);Context.BindObservedGameCamera(Table);Context.UseObservedConstants();
  }
  public WorldRayObserved36 Cast(){
   var output=new WorldRayObserved36();
   Check(Object.ReferenceEquals(output,CameraWorldRayContracts.GetRay229700(Engine.Camera13B0,output,Input,null,Context,new Invocation())),"Actual WorldRay hidden output returned");
   Check(Context.LastRayGetterCount==2&&!Object.ReferenceEquals(Context.LastRayFirstHidden,Context.LastRaySecondHidden),"Two real getter calls use distinct fresh hidden storage");
   Check(Object.ReferenceEquals(Context.LastRayFirstHidden,Context.LastRayFirstReturned)&&Object.ReferenceEquals(Context.LastRaySecondHidden,Context.LastRaySecondReturned),"Both real GameCamera getters return their supplied storage");
   var first=Boundaries.RawTargetFor(Context.LastRayFirstHidden);var second=Boundaries.RawTargetFor(Context.LastRaySecondHidden);
   Check(first.Q0.Read(0)==RegionToken&&second.Q0.Read(0)==RegionToken&&first.DC.Read(0)==Bits(17)&&second.DC.Read(0)==Bits(17),"Both actual Game target bodies apply the observed vertical offset");
   Check(output.Origin00.Q0.Read(0)==RegionToken&&Object.ReferenceEquals(Region,Regions.Region(output.Origin00.Q0.Read(0),0)),"Ray token resolves to the existing canonical region");
   Check(output.Origin00.D8.Read(0)==Bits(64.25f)&&output.Origin00.DC.Read(0)==Bits(cameraY)&&output.Origin00.D10.Read(0)==Bits(64.75f),"Ray origin is the actual shared camera position");
   Check(output.Direction18.X.Read(0)==0&&output.Direction18.Y.Read(0)==-1&&output.Direction18.Z.Read(0)==0,"Actual center-image projection and normalization point down");
   Unknown(output.Origin00.D14,"WorldRay padding remains unknown");Unknown(Image.FovA0,"Mode1 did not invent or consume FOV");
   Check(Context.Math==null,"Mode1 completes without a tangent provider");return output;
  }
 }

 static GroundRay24 SameRegionGroundRay(WorldRayObserved36 worldRay,OwnCamera camera){
  // Explicit OWN typed representation adapter. GroundRay24 has no region
  // token; keep region selection separate and consume this generated ray's
  // exact local values. This is not a recovered World::GetIntersection caller.
  Check(Object.ReferenceEquals(camera.Region,camera.Regions.Region(worldRay.Origin00.Q0.Read(0),0)),"Explicit same-region Ground selection");
  var ray=new GroundRay24();ray.Origin00.X0.Write(Float(worldRay.Origin00.D8.Read(0)));ray.Origin00.Y4.Write(Float(worldRay.Origin00.DC.Read(0)));ray.Origin00.Z8.Write(Float(worldRay.Origin00.D10.Read(0)));
  ray.Direction0C.X0.CopyFrom(worldRay.Direction18.X.Snapshot());ray.Direction0C.Y4.CopyFrom(worldRay.Direction18.Y.Snapshot());ray.Direction0C.Z8.CopyFrom(worldRay.Direction18.Z.Snapshot());return ray;
 }
 static GroundBlockHit20 NewOwnHit(){var hit=new GroundBlockHit20();hit.Distance00.Write(float.PositiveInfinity);hit.Material10.Write(100);return hit;}
 static void SameGround(SavedGround ground){
  Check(Object.ReferenceEquals(ground.Data,ground.Materials.Data)&&Object.ReferenceEquals(ground.Data,ground.Context.Terrain.Data),"Material binding and query share actual decoded GroundData");
  for(uint n=0;n<5;n++)Check(Object.ReferenceEquals(ground.Materials.RecordAt(n),ground.Data.TypeRecordAt(n,0))&&Object.ReferenceEquals(ground.Data.TypeRecordAt(n,0).Material.Read(0),ground.Materials.MaterialViewAt(n)),"Actual saved record/material identity "+n);
 }
 public static int Main(string[] args){try{
  if(args.Length!=1||!String.Equals(args[0],Root,StringComparison.Ordinal))throw new Exception("Fixed saved-workspace argument required");
  byte[] member=ReadSaved(MemberRelative);Check(member.Length==1091357&&Hash(member)==MemberHash,"Pinned saved mainmenu member");
  var records=new byte[5][];var recordHashes=new string[5];for(int n=0;n<5;n++){records[n]=ReadSaved(RecordRelative(n));recordHashes[n]=Hash(records[n]);}
  byte[] report=ReadSaved(ReportRelative);Check(Hash(report)==TerrainSurfaceOperandEvidence.SavedReportHash,"Pinned actual surface operand receipt");
  var ground=new SavedGround(member,records,report);
  // OWN camera placement above every actual Block box makes maxDistance=1
  // reject the broad-phase candidates. Query1D0100 does not promise a second
  // maximum-distance check after triangle intersection, so do not test that.
  cameraY=F(ground.HighestBlockTop+32f);Check(!float.IsNaN(cameraY)&&!float.IsInfinity(cameraY)&&cameraY-ground.HighestBlockTop>1,"OWN camera is above all actual Block bounds");
  var camera=new OwnCamera(cameraY);var cells=ground.Data.Cells128.Read(0);
  uint[] indices={8320,8321,8449,8450},expected={0x41819c63u,0x41816b17u,0x41801dd7u,0x41800932u};var h=new double[4];
  for(int n=0;n<4;n++){uint bits=cells.Read(indices[n],0);Check(bits==expected[n],"Saved center vertex bits "+n);h[n]=Float(bits);}
  double triangle=.25*h[0]+.5*h[2]+.25*h[3];double bilinear=.25*(.75*h[0]+.25*h[1])+.75*(.75*h[2]+.25*h[3]);
  Check(Math.Abs(triangle-bilinear)>.0008,"Saved sample distinguishes triangle collision from height interpolation");

  Scenario(delegate{
   var produced=camera.Cast();var ray=SameRegionGroundRay(produced,camera);var hit=NewOwnHit();
   var entry=GroundSpatialTreeContracts.Query1D0100(ground.Tree.Root10.Read(0),ray,hit,7,float.PositiveInfinity,null,0,ground.Context,new Invocation());
   Check(entry!=null,"Actual produced camera ray reaches saved Ground triangle");
   hitY=hit.Position04.Y4.Read(0);hitDistance=hit.Distance00.Read(0);
   Check(hitDistance>0&&!float.IsInfinity(hitDistance)&&Math.Abs((double)hitY-triangle)<.0002,"Actual hit agrees with independent saved triangle oracle");
   Check(Math.Abs((double)hitY-bilinear)>.0006,"Hit is not substituted bilinear or navigation height");
   Check(hit.Position04.X0.Read(0)==64.25f&&hit.Position04.Z8.Read(0)==64.75f&&Math.Abs((double)hitDistance-(cameraY-triangle))<.0002,"Generated ray supplies actual hit XYZ and distance");
   Check(hit.Material10.Read(0)==ground.Materials.MaterialViewAt(1).SurfaceFC.Read(0),"Saved Grass material flows through actual Block intersection");
   Check(Object.ReferenceEquals(entry.Block.TerrainA8.Read(0),ground.Data),"Returned Block retains original saved Terrain identity");SameGround(ground);
  });
  Scenario(delegate{
   var produced=camera.Cast();var ray=SameRegionGroundRay(produced,camera);var hit=NewOwnHit();
   var entry=GroundSpatialTreeContracts.Query1D0100(ground.Tree.Root10.Read(0),ray,hit,7,1,null,0,ground.Context,new Invocation());
   Check(entry==null&&float.IsPositiveInfinity(hit.Distance00.Read(0))&&hit.Material10.Read(0)==100,"Insufficient broad-phase maxDistance yields actual miss without output overwrite");
   Unknown(hit.Position04.X0,"Miss keeps unknown X");Unknown(hit.Position04.Y4,"Miss keeps unknown Y");Unknown(hit.Position04.Z8,"Miss keeps unknown Z");
   SameGround(ground);
  });
  Check(Hash(ReadSaved(MemberRelative))==MemberHash&&Hash(ReadSaved(ReportRelative))==TerrainSurfaceOperandEvidence.SavedReportHash,"Saved member and operand receipt unchanged");
  for(int n=0;n<5;n++)Check(Hash(ReadSaved(RecordRelative(n)))==recordHashes[n],"Saved terrain record unchanged "+n);
  Console.WriteLine("{\"status\":\"passed-new-camera-ground-integration\",\"scenarios\":"+scenarios+",\"assertions\":"+assertions+",\"camera_y\":"+cameraY.ToString("R",CultureInfo.InvariantCulture)+",\"hit_y\":"+hitY.ToString("R",CultureInfo.InvariantCulture)+",\"hit_distance\":"+hitDistance.ToString("R",CultureInfo.InvariantCulture)+",\"triangle_oracle\":"+triangle.ToString("R",CultureInfo.InvariantCulture)+",\"actual_game_camera_getters\":true,\"actual_image_projection\":true,\"actual_ray_normalization\":true,\"saved_ground_material_tree\":true,\"short_distance_miss\":true,\"explicit_own_camera_and_same_region_query\":true,\"physical_mouse\":false,\"native_pick_caller\":false,\"native_scene_startup\":false,\"native_tangent_calls\":0,\"original_read\":false,\"original_executed\":false,\"gameplay_accepted\":false}");return 0;
 }catch(Exception error){Console.Error.WriteLine(error);return 1;}}
}
