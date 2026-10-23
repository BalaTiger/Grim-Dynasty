using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using GrimDynasty.ReverseClient.Actions;

// New OWN handoff probe only. One canonical Region has explicit origin0, as
// does the produced NAV mesh. No native pick caller/startup or walking success
// is inferred. All ray, terrain intersection and nearest algorithms are real;
// zero nearest reference remains a normal miss. No old test Main is invoked.
public static class CameraGroundNavigationHandoffTests {
 const string Root=@"C:\Users\zhuzi\Documents\Grim-Dynasty";
 const string MemberRelative=@"docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin";
 const string SurfaceRelative=@"docs\reverse-client\r1-terrain-surface-strings\inspection.json";
 const string MemberHash="a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289";
 const ulong RegionToken=77;
 static int scenarios,assertions;
 static readonly List<object> samples=new List<object>();
 public static int Scenarios{get{return scenarios;}}
 public static int Assertions{get{return assertions;}}
 public static object[] Samples{get{return samples.ToArray();}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Unknown<T>(Field<T> value,string why){bool unknown=false;try{value.Read(0);}catch(MissingContract){unknown=true;}Check(unknown,why);}
 static uint Bits(float value){return BitConverter.ToUInt32(BitConverter.GetBytes(value),0);}
 static float Float(uint value){return BitConverter.ToSingle(BitConverter.GetBytes(value),0);}
 static float F(float value){return Float(Bits(value));}
 static bool Finite(float value){return !Single.IsNaN(value)&&!Single.IsInfinity(value);}
 static ulong Pair(float x,float y){return (ulong)Bits(x)|((ulong)Bits(y)<<32);}
 static string Hash(byte[] bytes){using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
 static byte[] ReadSaved(string relative){return File.ReadAllBytes(Path.Combine(Root,relative));}
 static string RecordRelative(int n){return @"docs\reverse-client\r1-terrain-records\terrain-"+n+".json";}

 sealed class Scene {
  public readonly WorldCoordinateState Coordinates=new WorldCoordinateState();
  public readonly WorldRegion Region=new WorldRegion(new object());
  public readonly OwnGroundData Data;
  public readonly TerrainMaterialCollisionAdapter Materials;
  public readonly GroundSpatialTree Tree=new GroundSpatialTree();
  public readonly GroundSpatialContext Ground;
  public readonly NavigationMeshTilesState Mesh;
  public readonly NavigationQuery Query=new NavigationQuery();
  public readonly NavigationCrowdAdapterState Adapter=new NavigationCrowdAdapterState();
  public readonly NavigationCrowdAdapterBoundaries Navigation;
  public readonly PortalGameEngine CameraEngine=new PortalGameEngine();
  public readonly PortalCameraVTable CameraTable=new PortalCameraVTable();
  public readonly PortalCameraState Camera;
  public readonly CameraTargetState Target;
  public readonly EvidenceCameraBoundaries CameraBoundaries;
  public readonly CameraImageState Image;
  public readonly CameraWorldRayContext Rays;
  public readonly CameraImagePoint2 Center=new CameraImagePoint2(Field<float>.Of(.5f),Field<float>.Of(.5f));
  public readonly float CameraHeight;
  readonly string[] recordHashes=new string[5];
  public Scene(){
   byte[] member=ReadSaved(MemberRelative);Check(member.Length==1091357&&Hash(member)==MemberHash,"Pinned saved mainmenu member");
   var chunk=new byte[149073];Buffer.BlockCopy(member,273260,chunk,0,chunk.Length);Data=OwnGroundColdLoader.DecodeType2Chunk(chunk,MemberHash+":273260+149073");Data.SelectObservedScalarSlot();
   var records=new byte[5][];for(int n=0;n<5;n++){records[n]=ReadSaved(RecordRelative(n));recordHashes[n]=Hash(records[n]);}
   var report=ReadSaved(SurfaceRelative);Check(Hash(report)==TerrainSurfaceOperandEvidence.SavedReportHash,"Pinned Ground surface operand receipt");
   var evidence=TerrainSurfaceOperandEvidence.FromSavedReport(report,TerrainSurfaceOperandEvidence.SavedReportHash,TerrainSurfaceOperandEvidence.SavedReaderHash);Materials=TerrainMaterialCollisionAdapter.FromSavedRecords(Data,records,evidence);Materials.BindAll();
   var geometry=new GroundGeometryOperands();geometry.SelectObservedDiskOperands();var blocks=GroundGeometryContracts.CreateOwnBlockBounds(Data,geometry,new Invocation());var states=new GroundBlockMaterialState[blocks.Length];float top=Single.NegativeInfinity;
   for(int n=0;n<blocks.Length;n++){states[n]=new GroundBlockMaterialState(blocks[n]);states[n].Populate(geometry,new Invocation());top=Math.Max(top,F(blocks[n].Box10.Center04.Read(0)+blocks[n].Box10.Extent10.Read(0)));}
   var binding=new GroundTerrainIntersectionBinding(Data);binding.SelectObservedDiskSlots();var operands=new GroundBlockIntersectionOperands();operands.SelectObservedDiskOperands();Ground=new GroundSpatialContext(binding,operands);Ground.SelectObservedDiskOperands();GroundSpatialTreeContracts.PublishOwnColdBlocks(Tree,Data,states,Ground,new Invocation());CameraHeight=F(top+32);
   Check(Finite(CameraHeight)&&CameraHeight>top,"Explicit camera height above saved Ground bounds");

   // Same saved tile producer and explicit configuration as the passed NAV
   // request fixture, without its unused Agent/Group or opaque callback setup.
   Mesh=NavigationMeshTilesState.Create(0,0,0,32,32,4096,1024,new NavigationSpatialState());Mesh.UseObservedLinkConstants();int inserted=0;
   foreach(var tile in MainmenuTilePipeline.BuildExplicitColdTiles()){
    var owner=new NavigationMeshTileBytes(tile.Bytes,tile.Length);var reference=new Field<uint>();var added=NavigationMeshTiles.Add(Mesh,owner,reference);
    if(added.Outcome!=NavigationMeshTileOutcome.NativeSuccess||added.NativeStatus!=0x40000000||reference.Read(0)==0)throw new Exception("Actual saved tile insertion failed: "+added.Detail);inserted++;
   }
   Check(inserted==9,"All nine actual saved layers inserted");Query.Mesh0.Write(Mesh.Mesh);Adapter.Query10.Write(Query);Adapter.Filter18.Include100.Write(1);Adapter.Filter18.Exclude102.Write(0);Adapter.Extent11C.X.Write(.125f);Adapter.Extent11C.Y.Write(2);Adapter.Extent11C.Z.Write(.125f);Adapter.Critical138.Write(new NavigationCrowdCriticalSection());Navigation=new NavigationCrowdAdapterBoundaries(Mesh);Navigation.UseObservedConstants();
   Region.Offset3C.Write(0);Region.Offset40.Write(0);Region.Offset44.Write(0);Coordinates.AttachRegion(RegionToken,Region);
   Check(Mesh.MeshSpatial.OriginX1C.Read(0)==0&&Mesh.OriginY20.Read(0)==0&&Mesh.MeshSpatial.OriginZ24.Read(0)==0,"Explicit NAV origin matches the single Region frame");

   Camera=new PortalCameraState(CameraEngine.Camera13B0);Target=new CameraTargetState(CameraEngine.Camera13B0);CameraBoundaries=new EvidenceCameraBoundaries(new MoveContext(),new PortalContext(),new CameraTargetBoundaries());CameraBoundaries.Attach(Camera);CameraBoundaries.AttachTarget(Target);CameraEngine.Camera13B0.Virtual00.Write(CameraTable);
   Target.Target120.Q0.Write(RegionToken);Target.Target120.DC.Write(Bits(16));Target.Offset55C.X.Write(0);Target.Offset55C.Y.Write(0);Target.Offset55C.Z.Write(0);Target.Count138.Write(0);
   float[] basis={1,0,0,0,0,1,0,-1,0};for(int n=0;n<9;n++)Camera.Basis70[n].Write(basis[n]);Image=new CameraImageState(Camera);Image.Mode6C.Write(1);Image.WidthA4.Write(2);Image.HeightA8.Write(2);Rays=new CameraWorldRayContext(CameraBoundaries,Coordinates);Rays.Attach(Image);Rays.BindObservedGameCamera(CameraTable);Rays.UseObservedConstants();
   Check(Object.ReferenceEquals(Coordinates,Rays.Regions)&&Object.ReferenceEquals(Region,Coordinates.Region(RegionToken,0)),"Camera and Ground selection share one canonical Region registry");
  }
  public GroundBlockHit20 Cast(float x,float z){
   Camera.Position94.Write(Pair(x,CameraHeight));Camera.Position9C.Write(Bits(z));Target.Target120.D8.Write(Bits(x));Target.Target120.D10.Write(Bits(z));
   var ray=new WorldRayObserved36();Check(Object.ReferenceEquals(ray,CameraWorldRayContracts.GetRay229700(CameraEngine.Camera13B0,ray,Center,null,Rays,new Invocation())),"Actual camera hidden output returned");
   Check(Rays.LastRayGetterCount==2&&Object.ReferenceEquals(Rays.LastRayFirstHidden,Rays.LastRayFirstReturned)&&Object.ReferenceEquals(Rays.LastRaySecondHidden,Rays.LastRaySecondReturned),"Both real GameCamera target calls completed");
   Check(Object.ReferenceEquals(Region,Coordinates.Region(ray.Origin00.Q0.Read(0),0)),"Ground query selects the same actual Region identity");
   Check(ray.Direction18.X.Read(0)==0&&ray.Direction18.Y.Read(0)==-1&&ray.Direction18.Z.Read(0)==0,"Actual center ray points vertically down");Unknown(ray.Origin00.D14,"Ray origin padding stays unknown");
   var localRay=new GroundRay24();localRay.Origin00.X0.Write(Float(ray.Origin00.D8.Read(0)));localRay.Origin00.Y4.Write(Float(ray.Origin00.DC.Read(0)));localRay.Origin00.Z8.Write(Float(ray.Origin00.D10.Read(0)));localRay.Direction0C.X0.CopyFrom(ray.Direction18.X);localRay.Direction0C.Y4.CopyFrom(ray.Direction18.Y);localRay.Direction0C.Z8.CopyFrom(ray.Direction18.Z);
   var hit=new GroundBlockHit20();hit.Distance00.Write(Single.PositiveInfinity);hit.Material10.Write(100);var selected=GroundSpatialTreeContracts.Query1D0100(Tree.Root10.Read(0),localRay,hit,7,Single.PositiveInfinity,null,0,Ground,new Invocation());
   Check(selected!=null&&Object.ReferenceEquals(selected.Block.TerrainA8.Read(0),Data),"Actual ray hits the original saved Ground data");Check(hit.Position04.X0.Read(0)==x&&hit.Position04.Z8.Read(0)==z,"Ground hit retains the requested XZ");
   Check(Finite(hit.Distance00.Read(0))&&hit.Distance00.Read(0)>0&&Math.Abs((double)hit.Position04.Y4.Read(0)-TriangleHeight(x,z))<.0002,"Ground triangle agrees with independent saved-cell oracle");return hit;
  }
  double TriangleHeight(float x,float z){
   int ix=(int)Math.Floor(x),iz=(int)Math.Floor(z);if(ix<0||ix>=128||iz<0||iz>=128)throw new Exception("Outside fixed saved Ground oracle extent");var cells=Data.Cells128.Read(0);uint a=(uint)(iz*129+ix);double h00=Float(cells.Read(a,0)),h10=Float(cells.Read(a+1,0)),h01=Float(cells.Read(a+129,0)),h11=Float(cells.Read(a+130,0));double fx=(double)x-ix,fz=(double)z-iz;return fz>=fx?(1-fz)*h00+(fz-fx)*h01+fx*h11:(1-fx)*h00+(fx-fz)*h10+fz*h11;
  }
  public NavigationPoint3 WorldPoint(GroundBlockHit20 hit){
   // Explicit OWN typed handoff, not an invented native pick caller. The
   // original collision height is preserved through the real22C1A0 conversion.
   var local=new WorldPosition24();local.Q0.Write(RegionToken);local.D8.Write(Bits(hit.Position04.X0.Read(0)));local.DC.Write(Bits(hit.Position04.Y4.Read(0)));local.D10.Write(Bits(hit.Position04.Z8.Read(0)));var world=new NavigationPoint3();
   Check(Object.ReferenceEquals(world,EntityMovementContracts.GetWorldPosition22C1A0(local,world,Coordinates,new Invocation())),"Actual region-to-world conversion returns supplied output");
   Check(world.X.Read(0)==hit.Position04.X0.Read(0)&&world.Y.Read(0)==hit.Position04.Y4.Read(0)&&world.Z.Read(0)==hit.Position04.Z8.Read(0),"Explicit origin0 conversion adds no height or coordinate offset");Unknown(local.D14,"Typed handoff does not invent padding");return world;
  }
  public void Unchanged(){Check(Hash(ReadSaved(MemberRelative))==MemberHash&&Hash(ReadSaved(SurfaceRelative))==TerrainSurfaceOperandEvidence.SavedReportHash,"Saved member and operand receipt unchanged");for(int n=0;n<5;n++)Check(Hash(ReadSaved(RecordRelative(n)))==recordHashes[n],"Saved material record unchanged "+n);}
 }
 sealed class TileInspection {
  public int X,Z,WalkableXZ,WalkableXYZ;
  public float MinY;
  public bool AllVerticesAtOrAboveMin=true;
 }
 static TileInspection InspectPointTile(Scene s,NavigationPoint3 point){
  float x=point.X.Read(0),y=point.Y.Read(0),z=point.Z.Read(0),ex=s.Adapter.Extent11C.X.Read(0),ey=s.Adapter.Extent11C.Y.Read(0),ez=s.Adapter.Extent11C.Z.Read(0);var r=new TileInspection();r.X=(int)Math.Floor(x/32);r.Z=(int)Math.Floor(z/32);
  Check((int)Math.Floor(F(x-ex)/32)==r.X&&(int)Math.Floor(F(x+ex)/32)==r.X&&(int)Math.Floor(F(z-ez)/32)==r.Z&&(int)Math.Floor(F(z+ez)/32)==r.Z,"This fixed query box lies inside one tile");
  var found=NavigationMeshTiles.Lookup(s.Mesh,r.X,r.Z,0);Check(found.Outcome==NavigationMeshTileOutcome.NativeSuccess&&found.Reference!=0,"Actual tile lookup finds the saved layer");
  var tile=s.Mesh.Mesh.Tiles50.Read(0)[(int)((found.Reference>>10)&4095)];var header=s.Mesh.Spatial.Header(tile.Header08.Read(0),0);r.MinY=header.BoundsMin48.Y.Read(0);Check(s.Mesh.Spatial.Tile(tile,0).BVTree40.Read(0)==null,"Produced cold tiles use real polygon AABB enumeration");
  var vertices=tile.Vertices18.Read(0);foreach(var vertex in vertices)r.AllVerticesAtOrAboveMin&=vertex.Y.Read(0)>=r.MinY;
  foreach(var poly in tile.Polys10.Read(0)){
   ushort flags=s.Mesh.Spatial.Poly(poly,0).Flags1C.Read(0);if((flags&s.Adapter.Filter18.Include100.Read(0))==0||(flags&s.Adapter.Filter18.Exclude102.Read(0))!=0)continue;
   float minX=Single.PositiveInfinity,minY=minX,minZ=minX,maxX=Single.NegativeInfinity,maxY=maxX,maxZ=maxX;int count=poly.VertexCount1E.Read(0);
   for(int n=0;n<count;n++){var v=vertices[poly.Indices04[n].Read(0)];minX=Math.Min(minX,v.X.Read(0));minY=Math.Min(minY,v.Y.Read(0));minZ=Math.Min(minZ,v.Z.Read(0));maxX=Math.Max(maxX,v.X.Read(0));maxY=Math.Max(maxY,v.Y.Read(0));maxZ=Math.Max(maxZ,v.Z.Read(0));}
   if(maxX>=F(x-ex)&&minX<=F(x+ex)&&maxZ>=F(z-ez)&&minZ<=F(z+ez)){r.WalkableXZ++;if(maxY>=F(y-ey)&&minY<=F(y+ey))r.WalkableXYZ++;}
  }
  return r;
 }
 static void Probe(Scene s,string name,float x,float z,float savedGroundY,bool expectedMiss){
  var hit=s.Cast(x,z);float groundY=hit.Position04.Y4.Read(0);Check(Math.Abs((double)groundY-savedGroundY)<.0002,"Actual Ground height agrees with the retained fixed sample");var point=s.WorldPoint(hit);var inspection=InspectPointTile(s,point);var projected=new NavigationPoint3();var invocation=new Invocation();
  uint reference=NavigationCrowdAdapterContracts.Project030B30(s.Adapter,point,projected,s.Navigation,invocation);
  Check(invocation.At==0x030b91&&s.Adapter.Critical138.Read(0).OwnOutstandingEnters==0,"Actual nearest adapter returned and released its lock");
  Check(s.Adapter.Extent11C.X.Read(0)==.125f&&s.Adapter.Extent11C.Y.Read(0)==2&&s.Adapter.Extent11C.Z.Read(0)==.125f,"Original search extents remain unchanged");Check(point.X.Read(0)==x&&point.Y.Read(0)==groundY&&point.Z.Read(0)==z,"Query input keeps original Ground XYZ");
  Check(reference==0?inspection.WalkableXYZ==0:inspection.WalkableXYZ>0,"Actual reference agrees with independent relevant-tile AABB inspection");
  float[] navPoint=null;
  if(reference==0){Check(reference==0,"Normal nearest miss is recorded as zero");Unknown(projected.X,"Miss leaves projected X unknown");Unknown(projected.Y,"Miss leaves projected Y unknown");Unknown(projected.Z,"Miss leaves projected Z unknown");}
  else{Check(NavigationCoreContracts.ValidateReference(s.Mesh.Mesh,reference)==1,"Actual nonzero nearest reference validates");Check(Finite(projected.X.Read(0)),"Projected X is finite");Check(Finite(projected.Y.Read(0)),"Projected Y is finite");Check(Finite(projected.Z.Read(0)),"Projected Z is finite");navPoint=new[]{projected.X.Read(0),projected.Y.Read(0),projected.Z.Read(0)};}
  if(expectedMiss){Check(inspection.X==1&&inspection.Z==1&&Bits(inspection.MinY)==0x419b1042u,"Ready sample uses the retained tile1,1 vertical bound");Check(inspection.AllVerticesAtOrAboveMin&&F(groundY+2)<inspection.MinY,"Produced vertex lower bound excludes the entire Ground search Y window");Check(inspection.WalkableXZ>0&&inspection.WalkableXYZ==0,"Walkable XZ candidates are excluded by Y only");Check(reference==0,"Ready Ground point is a real nearest miss, not fabricated navigation success");}
  samples.Add(new{name=name,ground=new[]{x,groundY,z},region_token=RegionToken,region_offset=new[]{0,0,0},nearest_reference=reference,navigation_hit=reference!=0,projected=navPoint,tile=new[]{inspection.X,inspection.Z},tile_min_y=inspection.MinY,walkable_xz=inspection.WalkableXZ,walkable_xyz=inspection.WalkableXYZ,expected_negative=expectedMiss,extent=new[]{.125f,2f,.125f},actual_camera_getters=s.Rays.LastRayGetterCount});scenarios++;
 }
 public static void Run(){scenarios=assertions=0;samples.Clear();var scene=new Scene();Probe(scene,"camera-click",64.25f,64.75f,16.05875f,false);Probe(scene,"ready-frame-1-ground",63.0971069f,52.82203f,17.2317123f,true);Probe(scene,"ready-frame-2-ground",63.1942139f,52.84406f,17.2245941f,true);scene.Unchanged();}
}
public static class CameraGroundNavigationHandoffTestProgram {
 public static int Main(string[] args){try{if(args.Length!=0)throw new ArgumentException("No arguments: fixed saved OWN handoff probe");CameraGroundNavigationHandoffTests.Run();Console.WriteLine(new JavaScriptSerializer().Serialize(new{status="passed-new-camera-ground-nearest-handoff-checks",scenarios=CameraGroundNavigationHandoffTests.Scenarios,assertions=CameraGroundNavigationHandoffTests.Assertions,samples=CameraGroundNavigationHandoffTests.Samples,explicit_own_origin0=true,search_extent_changed=false,height_offset_added=false,native_pick_caller=false,native_scene_startup=false,walking_accepted=false,old_suite_main_called=false,original_read=false,original_executed=false}));return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
