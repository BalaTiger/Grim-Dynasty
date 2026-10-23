using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// New pure-memory integration fixtures: own serialized geometry -> production
// MeshTiles -> production corridor search -> production portal/funnel. No
// callback returns a portal, waypoint or successful path. This is not a native
// runtime, loaded-scene or movement test. Old suites are not invoked here.
public static class NavigationStraightPathTests {
 static int scenarios,assertions;
 public static int Scenarios {get{return scenarios;}}
 public static int Assertions {get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Test(string name,Action body){body();scenarios++;Console.WriteLine("PASS "+name);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract e){Equal(at,e.Address,why+" address");return;}throw new Exception("Expected own observation/extent stop: "+why);}
 static void Unknown<T>(Field<T> cell,string why){Missing(0,delegate{cell.Read(0);},why);}
 static NavigationPoint3 Point(float x,float y,float z){var p=new NavigationPoint3();p.X.Write(x);p.Y.Write(y);p.Z.Write(z);return p;}
 static NavigationTileQuantizedVertex V(ushort x,ushort z){return new NavigationTileQuantizedVertex(x,0,z);}
 static ushort[] Quad(ushort a,ushort b,ushort c,ushort d){return new ushort[]{a,b,c,d,65535,65535};}
 static ushort[] Closed(){return new ushort[]{65535,65535,65535,65535,65535,65535};}
 static byte[] Serialize(int x,int z,NavigationTileQuantizedVertex[] vertices,NavigationTilePolygon[] polygons,float width){
  var areas=new byte[polygons.Length];var flags=new ushort[polygons.Length];for(int n=0;n<polygons.Length;n++){areas[n]=1;flags[n]=1;}
  var input=new NavigationTileDataInput(vertices,polygons,areas,flags,new NavigationTileBounds(x,0,z,x+width,2,z+width),1,1,x,z,0,2,0.5f,1);
  var output=new NavigationTileDataOutput();var result=NavigationTileData.Build(input,output);Check(result.Outcome==NavigationTileDataOutcome.Published,"Own geometry serialized: "+result.Detail);return output.Bytes;
 }
 static byte[] Strip(){var a=Closed();a[2]=1;var b=Closed();b[0]=0;b[2]=2;var c=Closed();c[0]=1;
  return Serialize(0,0,new[]{V(0,0),V(0,1),V(1,0),V(1,1),V(2,0),V(2,1),V(3,0),V(3,1)},
   new[]{new NavigationTilePolygon(Quad(0,1,3,2),a),new NavigationTilePolygon(Quad(2,3,5,4),b),new NavigationTilePolygon(Quad(4,5,7,6),c)},3);}
 static byte[] Bend(){var a=Closed();a[2]=1;var b=Closed();b[0]=0;b[1]=2;var c=Closed();c[3]=1;
  return Serialize(0,0,new[]{V(0,0),V(0,1),V(2,1),V(2,0),V(3,1),V(3,0),V(2,3),V(3,3)},
   new[]{new NavigationTilePolygon(Quad(0,1,2,3),a),new NavigationTilePolygon(Quad(3,2,4,5),b),new NavigationTilePolygon(Quad(2,6,7,4),c)},3);}
 static byte[] Island(){return Serialize(4,0,new[]{V(0,0),V(0,1),V(1,1),V(1,0)},new[]{new NavigationTilePolygon(Quad(0,1,2,3),Closed())},1);}
 sealed class Fixture {
  public readonly NavigationMeshTilesState Mesh;
  public readonly NavigationQuery Query=new NavigationQuery();
  public readonly NavigationStraightPathBoundaries B;
  public readonly NavigationCorridorState Search;
  public readonly NavigationCorridorBoundaries CB;
  public readonly NavigationQueryFilter Filter=new NavigationQueryFilter();
  public NavigationRefBuffer Corridor=new NavigationRefBuffer(16);
  public readonly Field<uint> CorridorCount=new Field<uint>(),Count=new Field<uint>();
  public NavigationPointBuffer Output=new NavigationPointBuffer(8);
  public NavigationFlagBuffer Flags=new NavigationFlagBuffer(8);
  public NavigationPoint3 From=Point(0.25f,0,0.5f),To=Point(2.75f,0,0.5f);
  public uint Start,End;
  public int PathCount;
  public Fixture(){
   Mesh=NavigationMeshTilesState.Create(0,0,0,1,1,4,8,new NavigationSpatialState());Mesh.UseObservedLinkConstants();Query.Mesh0.Write(Mesh.Mesh);
   Search=new NavigationCorridorState(Query);Search.Pool58.Write(NavigationSearchStorage.CreatePool26DC10(32,16));Search.Queue60.Write(NavigationSearchStorage.CreateQueue(32));
   var costs=new NavigationCorridorFilterCosts(Filter);for(int n=0;n<64;n++)costs.AreaCosts00[n].Write(1);Search.Attach(costs);Filter.Include100.Write(1);Filter.Exclude102.Write(0);
   CB=new NavigationCorridorBoundaries(Mesh,Search);CB.UseObservedCorridorConstants();B=new NavigationStraightPathBoundaries(Mesh);B.UseObservedStraightPathConstants();B.UseObservedEqualityConstant();Count.Write(99);
  }
  public uint Add(byte[] bytes){var resultRef=new Field<uint>();var result=NavigationMeshTiles.Add(Mesh,new NavigationMeshTileBytes(bytes,bytes.Length),resultRef);Check(result.Outcome==NavigationMeshTileOutcome.NativeSuccess,"Actual mesh links produced: "+result.Detail);return resultRef.Read(0);}
  public uint Find(){uint status=NavigationCorridorContracts.FindPath(Query,Start,End,From,To,Filter,Corridor,CorridorCount,16,CB,new Invocation());PathCount=(int)CorridorCount.Read(0);return status;}
  public uint Call(int capacity){return NavigationStraightPathContracts.StraightPath(Query,From,To,Corridor,PathCount,Output,Flags,Count,capacity,B,new Invocation());}
 }
 static Fixture Straight(){var f=new Fixture();f.Start=f.Add(Strip());f.End=f.Start|2;Equal(0x40000000u,f.Find(),"Production search finds strip corridor");Equal(3,f.PathCount,"Three actual polygons");return f;}
 static Fixture Corner(){var f=new Fixture();f.Start=f.Add(Bend());f.End=f.Start|2;f.To=Point(2.5f,0,2.75f);Equal(0x40000000u,f.Find(),"Production search finds bent corridor");Equal(3,f.PathCount,"Three bend polygons");return f;}
 static void Position(Fixture f,int index,float x,float y,float z,string why){Equal(x,f.Output.Cells[index*3].Read(0),why+" X");Equal(y,f.Output.Cells[index*3+1].Read(0),why+" Y");Equal(z,f.Output.Cells[index*3+2].Read(0),why+" Z");}
 static void Flag(Fixture f,int index,byte value,string why){Equal(value,f.Flags.Cells[index].Read(0),why);}

 public static void Run(){
  scenarios=0;assertions=0;
  Test("Production three-polygon strip reduces to start and endpoint",delegate{
   var f=Straight();var corridor=f.Corridor;var query=f.Query;var mesh=f.Mesh.Mesh;
   Equal(0x40000000u,f.Call(8),"Untruncated funnel success");Equal(2u,f.Count.Read(0),"Portals are constraints rather than mandatory waypoints");Position(f,0,0.25f,0,0.5f,"Start");Position(f,1,2.75f,0,0.5f,"End");Flag(f,0,1,"Start flag");Flag(f,1,2,"End flag");
   Same(corridor,f.Corridor,"Same input reference buffer");Same(query,f.Search.Identity,"Search and funnel query identity");Same(mesh,f.Query.Mesh0.Read(0),"Same registered mesh");Equal(f.Start|1,f.Corridor.Cells[1].Read(0),"Middle polygon remains in input corridor");Unknown(f.Output.Cells[6],"No extra synthetic portal waypoint");
  });
  Test("Real L-shaped corridor emits its geometric corner in both directions",delegate{
   var f=Corner();Equal(0x40000000u,f.Call(8),"Forward bend");Equal(3u,f.Count.Read(0),"Exactly one necessary corner");Position(f,0,0.25f,0,0.5f,"Forward start");Position(f,1,2,0,1,"Inside corner");Position(f,2,2.5f,0,2.75f,"Forward end");Flag(f,0,1,"Start");Flag(f,1,0,"Ordinary corner");Flag(f,2,2,"End");
   uint old=f.Start;f.Start=f.End;f.End=old;var point=f.From;f.From=f.To;f.To=point;Equal(0x40000000u,f.Find(),"Real reverse corridor search");Equal(0x40000000u,f.Call(8),"Reverse bend");Equal(3u,f.Count.Read(0),"Reverse necessary corner count");Position(f,0,2.5f,0,2.75f,"Reverse start");Position(f,1,2,0,1,"Reverse corner");Position(f,2,0.25f,0,0.5f,"Reverse end");Flag(f,1,0,"Reverse ordinary corner");Flag(f,2,2,"Reverse end");
  });
  Test("Start on first portal is not duplicated as a waypoint",delegate{
   var f=Straight();f.From=Point(1,0,0.5f);Equal(0x40000000u,f.Call(8),"First-portal near-start skip");Equal(2u,f.Count.Read(0),"No duplicate portal start");Position(f,0,1,0,0.5f,"Portal start");Position(f,1,2.75f,0,0.5f,"End");Flag(f,1,2,"Endpoint marked");
  });
  Test("Interior endpoint heights survive clamp without invented ground sampling",delegate{
   var f=Straight();f.From.Y.Write(7);f.To.Y.Write(9);Equal(0x40000000u,f.Call(8),"XZ corridor with independent observed Y");Equal(2u,f.Count.Read(0),"No invented height waypoint");Position(f,0,0.25f,7,0.5f,"Preserved start height");Position(f,1,2.75f,9,0.5f,"Preserved end height");
  });
  Test("Exact capacity and initial truncation preserve native flags and status",delegate{
   var f=Straight();Equal(0x40000010u,f.Call(2),"Exact final count equals capacity");Equal(2u,f.Count.Read(0),"Complete exact-capacity path");Flag(f,1,2,"Exact fill retains end flag");
   Equal(0x40000010u,f.Call(1),"Start append fills capacity");Equal(1u,f.Count.Read(0),"Stop immediately after start");Flag(f,0,1,"Truncated start is not relabeled endpoint");
   Equal(0x40000010u,f.Call(0),"Native append writes before capacity check");Equal(1u,f.Count.Read(0),"Zero capacity still writes one point into declared storage");Flag(f,0,1,"Zero-capacity start flag");
  });
  Test("Bend truncation returns the corner without falsely marking it as destination",delegate{
   var f=Corner();Equal(0x40000010u,f.Call(2),"Intermediate append fills buffer");Equal(2u,f.Count.Read(0),"Start and necessary corner only");Position(f,1,2,0,1,"Last retained corner");Flag(f,1,0,"Truncated ordinary corner");Unknown(f.Output.Cells[6],"Endpoint was not appended after early return");
  });
  Test("Optional flags and coincident single-polygon endpoint are supported",delegate{
   var f=Straight();f.Flags=null;Equal(0x40000000u,f.Call(8),"No optional flag buffer");Equal(2u,f.Count.Read(0),"Coordinates still emitted");
   f.Flags=new NavigationFlagBuffer(8);f.PathCount=1;f.To=f.From;Equal(0x40000000u,f.Call(8),"Coincident endpoint within first polygon");Equal(1u,f.Count.Read(0),"Equal end reuses existing point");Flag(f,0,2,"End flag replaces start flag at same point");
  });
  Test("Invalid endpoint reference fails before any output point is published",delegate{
   var f=Straight();f.Corridor.Cells[2].Write(0);Equal(0x80000008u,f.Call(8),"Last-reference clamp fails");Equal(0u,f.Count.Read(0),"Count cleared");Unknown(f.Output.Cells[0],"Valid start clamp was local only");Unknown(f.Flags.Cells[0],"No start flag before both clamps succeed");
  });
  Test("First-reference zero and zero path count preserve actual read order",delegate{
   var f=Straight();f.Corridor.Cells[0].Write(0);f.Query.Mesh0.Forget();Equal(0x80000008u,f.Call(8),"Zero first ref rejects before unknown query mesh");Equal(0u,f.Count.Read(0),"Count clear survives early failure");
   f.Query.Mesh0.Write(f.Mesh.Mesh);f.Corridor.Cells[0].Write(f.Start);f.PathCount=0;Missing(0x262506,delegate{f.Call(8);},"Native last-ref read uses pathCount minus one after first clamp");Equal(0u,f.Count.Read(0),"No sanitized empty-path success");Unknown(f.Output.Cells[0],"No start output on invalid declared extent");
  });
  Test("Unknown first-portal threshold stops after the start output prefix",delegate{
   var f=Straight();Equal(0x358637beu,NavigationCoreContracts.ToBits(f.B.FirstPortalThreshold34B960.Read(0)),"Disk-observed threshold is one ULP above a 1e-6f literal");f.B.FirstPortalThreshold34B960.Forget();Missing(0x26274a,delegate{f.Call(8);},"Unknown threshold is not invented from expected route");Equal(1u,f.Count.Read(0),"Start retained");Position(f,0,0.25f,0,0.5f,"Start before threshold comparison");Flag(f,0,1,"Start flag retained");Unknown(f.Output.Cells[3],"No later point");
  });
  Test("Unknown warm equality value stops at the real funnel comparison",delegate{
   var f=Straight();f.B.Epsilon422BA4.Forget();Missing(0x26284c,delegate{f.Call(8);},"Known warm-state marker does not supply an unknown epsilon");Equal(1u,f.Count.Read(0),"Start append precedes funnel equality");Flag(f,0,1,"Start flag survives");Unknown(f.Output.Cells[3],"No guessed corner or endpoint");
  });
  Test("Final coordinate extent failure preserves previously appended start",delegate{
   var f=Straight();f.PathCount=1;f.To=Point(0.75f,0,0.5f);f.Output=new NavigationPointBuffer(1);Missing(0x262c0e,delegate{f.Call(8);},"Final endpoint X write requires declared backing extent");Equal(1u,f.Count.Read(0),"Count increment has not happened");Position(f,0,0.25f,0,0.5f,"Start prefix");Flag(f,0,1,"Start flag unchanged");
  });
  Test("Final flag extent failure occurs after all endpoint coordinates but before count",delegate{
   var f=Straight();f.PathCount=1;f.To=Point(0.75f,0,0.5f);f.Flags=new NavigationFlagBuffer(1);Missing(0x262c2d,delegate{f.Call(8);},"Missing final flag cell");Equal(1u,f.Count.Read(0),"Count not incremented before failed flag write");Position(f,1,0.75f,0,0.5f,"Endpoint coordinate prefix");Flag(f,0,1,"Existing start flag preserved");
  });
  Test("A real broken corridor closes on current polygon with partial flag zero",delegate{
   var f=Straight();uint island=f.Add(Island());f.Corridor.Cells[1].Write(island);f.PathCount=2;f.To=Point(4.75f,0,0.5f);
   Equal(0x40000040u,f.Call(8),"Valid refs without adjacency return native partial detail");Equal(2u,f.Count.Read(0),"Start plus current-polygon clamp");Position(f,1,1,0,0.5f,"Last reachable polygon boundary");Flag(f,1,0,"Partial close is not destination flag2");
   Equal(0x40000050u,f.Call(2),"Partial plus exact-capacity detail");Equal(2u,f.Count.Read(0),"No discarded partial point");
  });
  Test("Actual partial search corridor can finish locally without reaching requested goal",delegate{
   var f=Straight();f.End=f.Add(Island());f.To=Point(4.75f,0,0.5f);Equal(0x40000040u,f.Find(),"Production search reports disconnected requested target");Equal(3,f.PathCount,"Reachable strip corridor retained");
   Equal(0x40000000u,f.Call(8),"Funnel succeeds relative to provided partial corridor");Equal(2u,f.Count.Read(0),"Locally straight reachable portion");Position(f,1,3,0,0.5f,"Goal clamped to final reachable polygon");Flag(f,1,2,"Local end flag does not erase upstream partial-search status");Check(f.Output.Cells[3].Read(0)!=f.To.X.Read(0),"Requested goal remains unreached");
  });
  Test("Portal wrapper retains type-write prefixes and fills the same endpoint bundle",delegate{
   var f=Straight();var p=new NavigationPortalEndpoints();var left=p.Left;var right=p.Right;var first=Field<byte>.Of(77);var second=Field<byte>.Of(88);
   f.Query.Mesh0.Forget();Equal(0x80000008u,NavigationPortalWrapper.GetPortal262D30(f.Query,0,f.End,p,first,second,f.Mesh,f.B.PortalScale34B9CC),"Invalid first reference does not consume captured unknown mesh pointer");Equal((byte)77,first.Read(0),"First type untouched");Equal((byte)88,second.Read(0),"Second type untouched");Unknown(left.X,"No coordinate write");f.Query.Mesh0.Write(f.Mesh.Mesh);
   Equal(0x80000008u,NavigationPortalWrapper.GetPortal262D30(f.Query,f.Start,0,p,first,second,f.Mesh,f.B.PortalScale34B9CC),"Invalid second reference");Equal((byte)0,first.Read(0),"First type was already written");Equal((byte)88,second.Read(0),"Invalid second type remains untouched");Unknown(right.Z,"No endpoints before both validation steps");
   Equal(0x80000008u,NavigationPortalWrapper.GetPortal262D30(f.Query,f.Start,f.End,p,first,second,f.Mesh,f.B.PortalScale34B9CC),"Nonadjacent valid references");Equal((byte)0,second.Read(0),"Second type precedes missing-link failure");Unknown(left.X,"Missing link does not invent endpoint");
   f.B.PortalScale34B9CC.Forget();Equal(0x40000000u,NavigationPortalWrapper.GetPortal262D30(f.Query,f.Start,f.Start|1,p,first,second,f.Mesh,f.B.PortalScale34B9CC),"Internal portal does not read unused clipping constant");Same(left,p.Left,"Same caller-held left point");Same(right,p.Right,"Same caller-held right point");Equal(1f,left.X.Read(0),"Shared boundary X");Equal(1f,left.Z.Read(0),"Directed left endpoint Z");Equal(0f,right.Z.Read(0),"Directed right endpoint Z");
  });
 }
}
public static class NavigationStraightPathTestProgram {
 public static int Main(){try{NavigationStraightPathTests.Run();Console.WriteLine("PASS "+NavigationStraightPathTests.Scenarios+" scenarios, "+NavigationStraightPathTests.Assertions+" assertions; production portal/funnel over owned geometry and real corridor search; not native runtime or movement");return 0;}catch(Exception e){Console.Error.WriteLine("FAIL after "+NavigationStraightPathTests.Scenarios+" scenarios, "+NavigationStraightPathTests.Assertions+" assertions: "+e);return 1;}}
}
