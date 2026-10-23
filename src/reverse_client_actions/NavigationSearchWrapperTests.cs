using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using GrimDynasty.ReverseClient.Actions;

// New wrapper composition only. All endpoints and path/floor results below
// come from the saved map and existing production algorithms. Explicit OWN
// filter/pool/Region/actor/neighbor setup is not loaded-game initialization.
public static class NavigationSearchWrapperTests {
 const string Root=@"C:\Users\zhuzi\Documents\Grim-Dynasty";
 const string MemberPath=@"docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin";
 const string MemberHash="a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289";
 static int scenarios,assertions;
 static readonly List<object> samples=new List<object>();
 static void Check(bool value,string label){assertions++;if(!value)throw new Exception(label);}
 static void Scenario(Action action){action();scenarios++;}
 static uint Bits(float x){return NavigationCoreContracts.ToBits(x);}
 static float Float(uint x){return NavigationCoreContracts.FromBits(x);}
 static string Hash(byte[] bytes){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
 static MissingContract Missing(Action action,uint at){assertions++;try{action();}catch(MissingContract e){if(at!=0&&e.Address!=at)throw new Exception("Expected "+at.ToString("x")+", got "+e.Address.ToString("x"),e);return e;}throw new Exception("Expected Unsupported "+at.ToString("x"));}
 static void Unknown<T>(Field<T> value){Missing(delegate{value.Read(0xf001);},0xf001);}
 static WorldPosition24 Position(float x,float y,float z){var p=new WorldPosition24();p.Q0.Write(77);p.D8.Write(Bits(x));p.DC.Write(Bits(y));p.D10.Write(Bits(z));p.D14.Write(0xaabbccddu);return p;}
 static WorldPosition24 Position(NavigationPoint3 p){return Position(p.X.Read(0),p.Y.Read(0),p.Z.Read(0));}
 static float[] Values(WorldPosition24 p){return new[]{Float(p.D8.Read(0)),Float(p.DC.Read(0)),Float(p.D10.Read(0))};}
 static float[] Values(NavigationPoint3 p){return new[]{p.X.Read(0),p.Y.Read(0),p.Z.Read(0)};}
 static NavigationPoint3 Center(NavigationTile tile,NavigationPoly poly){var p=new NavigationPoint3();float x=0,y=0,z=0;int n=poly.VertexCount1E.Read(0);var vertices=tile.Vertices18.Read(0);for(int j=0;j<n;j++){var v=vertices[poly.Indices04[j].Read(0)];x+=v.X.Read(0);y+=v.Y.Read(0);z+=v.Z.Read(0);}p.X.Write(x/n);p.Y.Write(y/n);p.Z.Write(z/n);return p;}
 public sealed class Scene {
  public readonly WorldCoordinateState Regions=new WorldCoordinateState();
  public readonly WorldRegion Region=new WorldRegion(new object());
  public readonly WorldCoordinateBoundaries World;
  public readonly WorldFloorContext Floor;
  public readonly WorldFloorGroundOwner Ground;
  public readonly WorldFloorRegionView RegionView;
  public readonly WorldFloorLevelView Level;
  public readonly OwnReentrantRegionLock Lock=OwnReentrantRegionLock.InitializeOwn();
  public readonly NavigationMeshTilesState Mesh;
  public readonly NavigationQuery Query=new NavigationQuery();
  public readonly NavigationCorridorState Search;
  public NavigationPoint3 From,To;
  public uint SourceRef,TargetRef;
  public Scene(){
   byte[] member=File.ReadAllBytes(Path.Combine(Root,MemberPath));Check(member.Length==1091357&&Hash(member)==MemberHash,"Pinned saved mainmenu member");var groundBytes=new byte[149073];Buffer.BlockCopy(member,273260,groundBytes,0,groundBytes.Length);
   var data=OwnGroundColdLoader.DecodeType2Chunk(groundBytes,MemberHash+":273260+149073");data.SelectObservedScalarSlot();var records=new byte[5][];for(int n=0;n<5;n++)records[n]=File.ReadAllBytes(Path.Combine(Root,@"docs\reverse-client\r1-terrain-records\terrain-"+n+".json"));
   byte[] operandReport=File.ReadAllBytes(Path.Combine(Root,@"docs\reverse-client\r1-terrain-surface-strings\inspection.json"));var evidence=TerrainSurfaceOperandEvidence.FromSavedReport(operandReport,TerrainSurfaceOperandEvidence.SavedReportHash,TerrainSurfaceOperandEvidence.SavedReaderHash);TerrainMaterialCollisionAdapter.FromSavedRecords(data,records,evidence).BindAll();
   var geometry=new GroundGeometryOperands();geometry.SelectObservedDiskOperands();var blocks=GroundGeometryContracts.CreateOwnBlockBounds(data,geometry,new Invocation());var states=new GroundBlockMaterialState[blocks.Length];for(int n=0;n<blocks.Length;n++){states[n]=new GroundBlockMaterialState(blocks[n]);states[n].Populate(geometry,new Invocation());}
   var binding=new GroundTerrainIntersectionBinding(data);binding.SelectObservedDiskSlots();var operands=new GroundBlockIntersectionOperands();operands.SelectObservedDiskOperands();var spatial=new GroundSpatialContext(binding,operands);spatial.SelectObservedDiskOperands();var tree=new GroundSpatialTree();GroundSpatialTreeContracts.PublishOwnColdBlocks(tree,data,states,spatial,new Invocation());Ground=new WorldFloorGroundOwner(data,tree,spatial);
   var registry=new WorldPublicationRegistry(Regions);var floorOperands=new WorldFloorOperands(operands.Geometry);floorOperands.SelectObservedFloorDiskOperands();Floor=new WorldFloorContext(registry,floorOperands);RegionView=Floor.BindRegion(77,Region);Level=Floor.BindLevel(new object());
   Region.Offset3C.Write(0);Region.Offset40.Write(0);Region.Offset44.Write(0);Region.Unavailable90.Write(0);Region.Critical18.Write(Lock);RegionView.Publication.Level68.Write(Level.Level);
   Region.Bounds70.Center00.Write(64);Region.Bounds70.Center04.Write(32);Region.Bounds70.Center08.Write(64);Region.Bounds70.Extent0C.Write(256);Region.Bounds70.Extent10.Write(256);Region.Bounds70.Extent14.Write(256);
   var landscape=new WorldFloorLandscape(data);landscape.Ground20.Write(Ground);Level.Landscape30.Write(landscape);var water=new GroundWaterState();water.Prerequisite10B0.Write(new object());water.Prerequisite10B8.Write(new object());var waterBytes=new byte[24];Buffer.BlockCopy(member,422341,waterBytes,0,24);Check(GroundWaterContracts.Load1F81E0(water,new GroundWaterChunkReader(waterBytes,MemberHash+":422341+24; explicit OWN prerequisites"),new Invocation()),"Actual saved empty water");Level.Water40.Write(water);
   Level.PathableActors.PublishOwnContents(0,new WorldFloorActorView[0],"Explicit OWN wrapper scene: no actors; not a recovered loaded population");RegionView.Neighbors.PublishOwnContents(0,new RegionPublicationView[0],"Explicit OWN same-Region scene: no neighbors; not a recovered loaded population");
   World=new WorldCoordinateBoundaries(Regions);World.EngineGlobal.Write(new WorldEngine());World.UseObservedBoundsMask();
   Mesh=NavigationMeshTilesState.Create(0,0,0,32,32,4096,1024,new NavigationSpatialState());Mesh.UseObservedLinkConstants();var roots=new List<uint>();
   foreach(var tile in MainmenuTilePipeline.BuildExplicitColdTiles()){var reference=new Field<uint>();var added=NavigationMeshTiles.Add(Mesh,new NavigationMeshTileBytes(tile.Bytes,tile.Length),reference);if(added.Outcome!=NavigationMeshTileOutcome.NativeSuccess||added.NativeStatus!=0x40000000)throw new Exception("Saved tile insertion: "+added.Detail);roots.Add(reference.Read(0));}
   Check(roots.Count==9,"Actual nine saved layers");
   foreach(uint root in roots){var tile=Tile(root);var polys=tile.Polys10.Read(0);for(int n=0;n<polys.Length&&SourceRef==0;n++){
    var poly=polys[n];if((Mesh.Spatial.Poly(poly,0).Flags1C.Read(0)&1)==0)continue;uint link=Mesh.Poly(poly,0).FirstLink00.Read(0);var links=Mesh.Tile(tile,0).Links20.Read(0);var seen=new HashSet<uint>();
    while(link!=UInt32.MaxValue){if(link>=links.Length||!seen.Add(link))throw new Exception("Actual saved link extent/cycle");var edge=links[link];uint next=edge.Reference00.Read(0);if((next>>10)!=(root>>10)&&(Mesh.Spatial.Poly(Poly(next),0).Flags1C.Read(0)&1)!=0){SourceRef=root|(uint)n;TargetRef=next;From=Center(tile,poly);To=Center(Tile(next),Poly(next));break;}link=edge.Next04.Read(0);}
   }if(SourceRef!=0)break;}
   Check(SourceRef!=0&&TargetRef!=0,"Select real adjacent cross-tile endpoints, no route supplied");Query.Mesh0.Write(Mesh.Mesh);Search=new NavigationCorridorState(Query);Search.Pool58.Write(NavigationSearchStorage.CreatePool26DC10(4096,1024));Search.Queue60.Write(NavigationSearchStorage.CreateQueue(4096));
  }
  public NavigationTile Tile(uint r){return Mesh.Mesh.Tiles50.Read(0)[(int)((r>>10)&4095)];}
  public NavigationPoly Poly(uint r){return Tile(r).Polys10.Read(0)[(int)(r&1023)];}
  public void Released(){Check(!Lock.IsHeldByCurrentThread&&Lock.CurrentThreadDepth==0,"Actual Floor Region lock released");}
 }
 sealed class Frame {
  public readonly Scene S;
  public readonly NavigationSearchImplementationView Impl=NavigationSearchImplementationView.ForIdentity(new object());
  public readonly NavigationSearchManagerView Manager=NavigationSearchManagerView.ForIdentity(new object());
  public readonly NavigationSearchWrapperContext Context;
  public readonly NavigationSearchWrapperInvocation Call=new NavigationSearchWrapperInvocation();
  public WorldPosition24 From,To,Output=new WorldPosition24();
  public Field<float> Radius=Field<float>.Of(10),Length=new Field<float>();
  public readonly Field<uint> Reference=Field<uint>.Of(0);
  public readonly Field<byte> Partial=new Field<byte>();
  public NavigationPoint3 Direction=new NavigationPoint3();
  public object Vector;
  public Frame(Scene scene){S=scene;From=Position(scene.From);To=Position(scene.To);Impl.Query10.Write(scene.Query);Impl.Filter18.Include100.Write(1);Impl.Filter18.Exclude102.Write(0);for(int n=0;n<64;n++)Impl.Costs.AreaCosts00[n].Write(1);scene.Search.Attach(Impl.Costs);Impl.SelectObservedConstructorExtents10A530();Impl.Searches76E8.Write(0);Manager.Implementation08.Write(Impl);Context=new NavigationSearchWrapperContext(scene.Mesh,scene.Search,scene.World,scene.Floor);Context.SelectObservedAlgorithmConstants();}
  public byte Run(){return NavigationSearchWrapperContracts.FindPath150590(Manager,From,To,Radius,Output,Reference,Length,Vector,Direction,Partial,Context,Call);}
 }
 static void GeneralResult(Frame f){
  Check(f.Call.FirstNearestReturned&&f.Call.SecondNearestReturned&&f.Call.CorridorReturned&&f.Call.StraightReturned&&!f.Call.CacheHit,"All real noncache query stages completed");Check(f.Call.Returned&&f.Call.Result==1&&f.Impl.Searches76E8.Read(0)==1,"Wrapper return and one real counter increment");
  uint count=f.Call.CorridorCount.Read(0),points=f.Call.PointCount.Read(0);Check(count>=2&&points>=2,"Actual nontrivial corridor and waypoints");Check(f.Impl.Corridor1F0.At(0,0).Read(0)==f.Call.StartReference.Read(0)&&f.Impl.Corridor1F0.At(count-1,0).Read(0)==f.Call.EndReference.Read(0),"Real corridor connects nearest endpoints");Check(f.Reference.Read(0)==f.Call.EndReference.Read(0)&&f.Length.Read(0)>0,"Reference and route length published");
  Check(Object.ReferenceEquals(f.S.Query,f.Impl.Query10.Read(0))&&Object.ReferenceEquals(f.S.Mesh.Mesh,f.S.Query.Mesh0.Read(0))&&Object.ReferenceEquals(f.S.Region,f.S.Regions.Region(f.Output.Q0.Read(0),0)),"Same query, mesh and canonical Region throughout");
 }
 public static void Run(){scenarios=assertions=0;samples.Clear();var s=new Scene();uint realEnd=0;
  Scenario(delegate{
   var id=new object();var impl=NavigationSearchImplementationView.ForIdentity(id);Check(Object.ReferenceEquals(impl,NavigationSearchImplementationView.ForIdentity(id)),"One Impl owner view");Unknown(impl.Query10);Unknown(impl.Searches76E8);Unknown(impl.Filter18.Include100);Unknown(impl.Extent75F4.X);Unknown(impl.Points11F0.At(0,0));impl.SelectObservedConstructorExtents10A530();Check(Bits(impl.Extent75F4.X.Read(0))==0x3d4ccccd&&Bits(impl.Extent75F4.Y.Read(0))==0x40400000&&Bits(impl.Extent75F4.Z.Read(0))==0x3d4ccccd,"Only observed ctor extent scalars selected");Unknown(impl.Searches76E8);Check(impl.Corridor1F0.Cells.Length==1024&&impl.Points11F0.Cells.Length==6144&&impl.Flags71F0.Cells.Length==1024,"Explicit bounded nonoverlapping storage");
  });
  Scenario(delegate{
   var f=new Frame(s);Check(f.Run()==1,"Real saved-map endpoint-to-endpoint wrapper succeeds");GeneralResult(f);realEnd=f.Reference.Read(0);Check(f.Call.TranslateReturned&&f.Call.FloorReturned,"Actual Translate then Ground Floor completed");Unknown(f.Output.D14);Check(s.Floor.LastGroundHit.Distance00.Read(0)>0&&!Single.IsInfinity(s.Floor.LastGroundHit.Distance00.Read(0)),"Output used an actual saved Ground triangle");Check(Math.Abs(Float(f.Output.DC.Read(0))-s.Floor.LastGroundHit.Position04.Y4.Read(0))<.0002,"Floor height comes from Ground, not navigation height");Unknown(f.Partial);s.Released();
   samples.Add(new{name="saved-map-endpoints",from=Values(f.From),requested=Values(f.To),output=Values(f.Output),nearest_from=f.Call.StartReference.Read(0),nearest_to=f.Call.EndReference.Read(0),corridor=f.Call.CorridorCount.Read(0),waypoints=f.Call.PointCount.Read(0),length=f.Length.Read(0),direction=Values(f.Direction),first_extent=Values(f.Impl.Extent75F4),second_extent=Values(f.Call.SecondExtent),ground_material=s.Floor.LastGroundHit.Material10.Read(0)});
  });
  Scenario(delegate{
   var f=new Frame(s);f.Reference.Write(realEnd);f.Impl.Searches76E8.Forget();f.Impl.Corridor1F0.MarkReleased();f.Vector=new object();f.Output=new WorldPosition24(f.From);Check(f.Run()==1,"Cache hit with original source/output alias");Check(f.Call.CacheHit&&!f.Call.CorridorReturned&&!f.Call.StraightReturned&&f.Call.TranslateReturned&&f.Call.FloorReturned,"Cached projection only; no counter/path/vector branch");Unknown(f.Impl.Searches76E8);Unknown(f.Length);Unknown(f.Direction.X);Unknown(f.Partial);Unknown(f.From.D14);Check(Object.ReferenceEquals(f.Output.AddressIdentity,f.From.AddressIdentity)&&Math.Abs(Float(f.From.D8.Read(0))-s.To.X.Read(0))<.002,"Fresh16+8 source copy and actual translation reach borrowed alias");s.Released();
  });
  Scenario(delegate{
   var f=new Frame(s);f.From=Position(-10000,0,-10000);f.Reference.Forget();f.Impl.Searches76E8.Forget();Check(f.Run()==0,"Real first nearest miss");Check(f.Call.FirstNearestReturned&&!f.Call.SecondNearestReturned&&f.Call.StartReference.Read(0)==0,"First miss stops before second query");Unknown(f.Reference);Unknown(f.Output.Q0);Unknown(f.Length);Unknown(f.Impl.Searches76E8);
   f.From=Position(s.From);f.To=Position(10000,0,10000);Check(f.Run()==0,"Real second nearest miss");Check(f.Call.FirstNearestReturned&&f.Call.SecondNearestReturned&&!f.Call.CorridorReturned&&f.Call.EndReference.Read(0)==0,"Second miss has no path or output");Unknown(f.Output.Q0);Unknown(f.Impl.Searches76E8);
  });
  Scenario(delegate{
   var f=new Frame(s);f.Manager.Implementation08.Forget();Missing(delegate{f.Run();},0x10c76a);Unknown(f.Call.FromWorld.X);Unknown(f.Output.Q0);Check(!f.Call.FirstNearestReturned,"Unknown implementation consumed at first actual dereference");
   f.Manager.Implementation08.Write(f.Impl);f.Impl.Query10.Forget();var stop=Missing(delegate{f.Run();},0x10c7ef);Check(stop.Message.Contains("OWN adapter"),"Unknown query is an explicit adapter boundary, not a claimed native callee prefix");Unknown(f.Call.LocalRefOrCount);Check(f.Call.SecondExtent.X.Read(0)==10&&f.Call.SecondExtent.Y.Read(0)==3&&f.Call.SecondExtent.Z.Read(0)==10,"Second extent captured before first query");Check(f.Call.FromWorld.X.Read(0)==s.From.X.Read(0)&&f.Call.ToWorld.Z.Read(0)==s.To.Z.Read(0),"Native coordinate prefix retained before Unknown query");
   f.Impl.Query10.Write(null);Missing(delegate{f.Run();},0x26197a);Check(f.Call.LocalRefOrCount.Read(0)==0&&!f.Call.FirstNearestReturned,"Known null reaches real callee and preserves its clear prefix");
   f.Impl.Query10.Write(s.Query);f.Impl.Extent75F4.X.Forget();Missing(delegate{f.Run();},0x261995);Check(f.Call.LocalRefOrCount.Read(0)==0&&!f.Call.FirstNearestReturned,"Actual nearest cleared shared output before live first-extent failure");Check(f.Call.SecondExtent.X.Read(0)==10&&f.Call.SecondExtent.Y.Read(0)==3,"First live extent is distinct from captured second extent");Check(f.Impl.Searches76E8.Read(0)==0,"No path counter increment before nearest completes");
   f.Impl.SelectObservedConstructorExtents10A530();f.Impl.Corridor1F0.MarkReleased();Missing(delegate{f.Run();},0);Check(f.Impl.Searches76E8.Read(0)==1&&!f.Call.CorridorReturned&&f.Call.LocalRefOrCount.Read(0)==0,"Released corridor storage retains counter increment and actual path count clear");Check(f.Reference.Read(0)==0,"No reference success after real storage failure");Unknown(f.Output.Q0);
  });
  Scenario(delegate{
   var f=new Frame(s);s.Region.Unavailable90.Forget();try{Missing(delegate{f.Run();},0x221dbb);Check(f.Call.TranslateReturned&&!f.Call.FloorReturned&&!f.Call.Returned,"Floor unknown preserves actual Translate prefix");Check(f.Impl.Searches76E8.Read(0)==1&&f.Call.StraightReturned,"Actual path buffers already produced");Check(f.Reference.Read(0)==0,"Reference update waits for normal Floor return");Unknown(f.Length);Check(f.Output.D14.Read(0)==0xaabbccddu&&Math.Abs(Float(f.Output.D8.Read(0))-s.To.X.Read(0))<.002,"Copied tail and translated endpoint retained on Floor stop");
    s.Region.Unavailable90.Write(1);var normalFalse=new Frame(s);Check(normalFalse.Run()==1&&normalFalse.Call.FloorReturned,"Real Floor normal false does not change wrapper's native true");Check(normalFalse.Output.D14.Read(0)==0xaabbccddu&&normalFalse.Reference.Read(0)==realEnd,"Normal false preserves copied padding then allows reference publication");
   }finally{s.Region.Unavailable90.Write(0);}s.Released();
  });
  Scenario(delegate{
   var f=new Frame(s);f.Vector=new object();Missing(delegate{f.Run();},0x10caf7);Check(f.Call.FloorReturned&&!f.Call.Returned&&f.Reference.Read(0)==realEnd&&f.Length.Read(0)>0,"Nonnull vector fails only after endpoint/ref/length prefix");Unknown(f.Direction.X);Unknown(f.Output.D14);s.Released();
   var one=new Frame(s);one.To=Position(s.From);Missing(delegate{one.Run();},0x10cc31);Check(one.Call.PointCount.Read(0)==1&&one.Call.FloorReturned&&one.Reference.Read(0)!=0&&one.Length.Read(0)==0,"One-point route still reaches unconditional second-point direction read after all earlier outputs");Unknown(one.Direction.X);Unknown(one.Impl.Points11F0.At(3,0));s.Released();
  });
  Scenario(delegate{
   var f=new Frame(s);f.Length=f.Radius;f.Direction=f.Impl.Extent75F4;Check(f.Run()==1,"Supported same-typed output aliases");GeneralResult(f);Check(f.Call.SecondExtent.X.Read(0)==10&&f.Call.SecondExtent.Z.Read(0)==10,"Radius register captured before aliased length output");float x=f.Direction.X.Read(0),y=f.Direction.Y.Read(0),z=f.Direction.Z.Read(0);Check(Math.Abs((double)x*x+(double)y*y+(double)z*z-1)<.0001,"Direction writes live Impl extent cells after all queries");Check(Object.ReferenceEquals(f.Direction.X,f.Impl.Extent75F4.X),"No detached extent snapshot");s.Released();
  });
  Scenario(delegate{
   var f=new Frame(s);f.Output=null;f.Direction=null;s.Region.Unavailable90.Forget();try{Check(f.Run()==1,"Null endpoint skips Floor and preserves Unknown floor state");Check(!f.Call.TranslateReturned&&!f.Call.FloorReturned&&f.Reference.Read(0)==realEnd&&f.Length.Read(0)>0,"Output-null path still publishes ref and length");Unknown(s.Region.Unavailable90);}finally{s.Region.Unavailable90.Write(0);}
   var other=new WorldRegion(new object());other.Offset44.Write(0);s.Regions.AttachRegion(88,other);f.To.Q0.Write(88);Missing(delegate{f.Run();},0x10c746);Check(!f.Call.FirstNearestReturned&&!f.Call.Returned,"Cross-Region domain stops before any native search");
  });
  Scenario(delegate{
   var hit=Position(64.25f,29.5f,64.75f);Check(WorldFloorContracts.PutOnFloor22C620(hit,s.World,s.Floor,new Invocation())==1,"Actual saved Ground supplies click-height input");Check(Math.Abs(Float(hit.DC.Read(0))-16.05875f)<.0002,"Pinned real Ground height, no Y offset");
   var f=new Frame(s);f.To=hit;byte result=f.Run();Check(f.Call.Returned&&(result==0||result==1),"Actual saved Ground destination yields a normal recorded result");Check(f.Call.SecondExtent.Y.Read(0)==3&&f.Call.SecondExtent.X.Read(0)==10,"Player wrapper uses captured10/3/10, not Crowd.125/2/.125");Check(Float(hit.DC.Read(0))<17,"Wrapper leaves original Ground destination unchanged");
   if(result==1){Check(f.Reference.Read(0)!=0&&NavigationCoreContracts.ValidateReference(s.Mesh.Mesh,f.Reference.Read(0))==1,"Actual click path's returned reference validates");Unknown(f.Output.D14);}else{Unknown(f.Output.Q0);Unknown(f.Length);}s.Released();
   samples.Add(new{name="saved-ground-destination",from=Values(f.From),requested=Values(hit),result=result,first_nearest=f.Call.FirstNearestReturned,second_nearest=f.Call.SecondNearestReturned,end_reference=f.Call.EndReference.Read(0),output=result==1?Values(f.Output):null,second_extent=Values(f.Call.SecondExtent)});
  });
  Scenario(delegate{
   var f=new Frame(s);var tail=new NavigationPointBuffer(1);tail.At(2,0).Write(123);f.Direction=tail.PointAtScalar(2,0);Missing(delegate{f.Run();},0x10cc8a);Check(tail.At(2,0).Read(0)==123,"Native eight-byte XY store validates both cells before writing X");Check(f.Call.FloorReturned&&!f.Call.Returned&&f.Reference.Read(0)==realEnd&&f.Length.Read(0)>0,"Bounded direction stop preserves completed endpoint, reference and distance");Unknown(f.Output.D14);s.Released();
  });
  Check(Hash(File.ReadAllBytes(Path.Combine(Root,MemberPath)))==MemberHash,"Saved map remains unchanged");
 }
 public static int Main(string[] args){try{if(args.Length!=0)throw new ArgumentException("Fixed new wrapper tests take no arguments");Run();Console.WriteLine(new JavaScriptSerializer().Serialize(new{status="passed-new-navigation-search-wrapper-checks",scenarios=scenarios,assertions=assertions,samples=samples,own_initialization=true,native_loaded_state=false,injected_success=false,old_suite_main_called=false,original_read=false,original_executed=false}));return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
