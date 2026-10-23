using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Targeted OWN in-memory algorithm fixtures. Serialization, tile publication,
// actual mesh links, nearest-poly and263290 all run through production code.
// No injected success result, original process, original map or mouse movement.
public static class NavigationCrowdRaycastTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}public static int Assertions{get{return assertions;}}
 static void Check(bool condition,string why){assertions++;if(!condition)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Test(string name,Action action){action();scenarios++;Console.WriteLine("PASS "+name);}
 static void Missing(uint at,Action action,string why){try{action();}catch(MissingContract e){Equal(at,e.Address,why+" stop address");return;}throw new Exception("Expected explicit unknown: "+why);}
 static void Unknown<T>(Field<T> value,string why){Missing(0,delegate{value.Read(0);},why);}
 static NavigationPoint3 Point(float x,float y,float z){return new NavigationPoint3(Field<float>.Of(x),Field<float>.Of(y),Field<float>.Of(z));}
 static NavigationTileQuantizedVertex V(ushort x,ushort z){return new NavigationTileQuantizedVertex(x,0,z);}
 static ushort[] Quad(ushort a,ushort b,ushort c,ushort d){return new ushort[]{a,b,c,d,65535,65535};}
 static ushort[] Closed(){return new ushort[]{65535,65535,65535,65535,65535,65535};}
 static byte[] Serialize(int x,int z,NavigationTileQuantizedVertex[] vertices,NavigationTilePolygon[] polygons,NavigationTileBounds bounds){var areas=new byte[polygons.Length];var flags=new ushort[polygons.Length];for(int n=0;n<polygons.Length;n++){areas[n]=1;flags[n]=1;}var input=new NavigationTileDataInput(vertices,polygons,areas,flags,bounds,1,1,x,z,0,2,0.5f,1);var output=new NavigationTileDataOutput();var result=NavigationTileData.Build(input,output);Equal(NavigationTileDataOutcome.Published,result.Outcome,"Actual fixture serializer: "+result.Detail);return output.Bytes;}
 static uint Add(NavigationMeshTilesState mesh,byte[] bytes){var reference=new Field<uint>();var result=NavigationMeshTiles.Add(mesh,new NavigationMeshTileBytes(bytes,bytes.Length),reference);Equal(NavigationMeshTileOutcome.NativeSuccess,result.Outcome,"Actual tile publication: "+result.Detail);return reference.Read(0);}
 static byte[] Strip(){var a=Closed();var b=Closed();var c=Closed();a[2]=1;b[0]=0;b[2]=2;c[0]=1;return Serialize(0,0,new[]{V(0,0),V(0,1),V(1,0),V(1,1),V(2,0),V(2,1),V(3,0),V(3,1)},new[]{new NavigationTilePolygon(Quad(0,1,3,2),a),new NavigationTilePolygon(Quad(2,3,5,4),b),new NavigationTilePolygon(Quad(4,5,7,6),c)},new NavigationTileBounds(0,0,0,3,2,1));}
 static NavigationCrowdRaycastResult Output(int capacity){var r=new NavigationCrowdRaycastResult();r.Path10.Write(new NavigationRefWindow(new NavigationRefBuffer(Math.Max(capacity,0)),0));r.Capacity1C.Write(capacity);return r;}
 static void Normal(NavigationCrowdRaycastResult r,float x,float y,float z,string why){Equal(x,r.Normal04.X.Read(0),why+" X");Equal(y,r.Normal04.Y.Read(0),why+" Y");Equal(z,r.Normal04.Z.Read(0),why+" Z");}
 sealed class Fixture {
  public readonly NavigationMeshTilesState Mesh;
  public readonly NavigationCrowdRaycastBoundaries B;
  public readonly NavigationQuery Query=new NavigationQuery();
  public readonly NavigationCrowdAdapterState Adapter=new NavigationCrowdAdapterState();
  public readonly NavigationPoint3 From=Point(0.25f,0,0.5f),To=Point(2.75f,0,0.5f);
  public readonly uint Root;
  public NavigationTile Tile{get{return Mesh.Mesh.Tiles50.Read(0)[0];}}
  public NavigationPoly Poly(int n){return Tile.Polys10.Read(0)[n];}
  public Fixture():this(65536){}
  public Fixture(int budget){Mesh=NavigationMeshTilesState.Create(0,0,0,3,1,1,8,new NavigationSpatialState());Mesh.UseObservedLinkConstants();Root=Add(Mesh,Strip());Query.Mesh0.Write(Mesh.Mesh);Adapter.Query10.Write(Query);Adapter.Critical138.Write(new NavigationCrowdCriticalSection());Adapter.Filter18.Include100.Write(1);Adapter.Filter18.Exclude102.Write(0);Adapter.Extent11C.X.Write(0.5f);Adapter.Extent11C.Y.Write(1);Adapter.Extent11C.Z.Write(0.5f);B=new NavigationCrowdRaycastBoundaries(Mesh,budget);B.UseObservedRaycastConstants();}
  public uint Cast(NavigationPoint3 from,NavigationPoint3 to,NavigationCrowdRaycastResult r){return NavigationCrowdRaycastContracts.Raycast263290(Query,Root,from,to,Adapter.Filter18,r,B,new Invocation());}
  public byte Traverse(NavigationPoint3 from,NavigationPoint3 to){return NavigationCrowdRaycastContracts.CanTraverse0309F0(Adapter,from,to,B,new Invocation());}
  public NavigationMeshLink12 First(int n){return Mesh.Tile(Tile,0).Links20.Read(0)[Mesh.Poly(Poly(n),0).FirstLink00.Read(0)];}
 }
 sealed class PortalFixture {
  public readonly NavigationMeshTilesState Mesh;
  public readonly NavigationCrowdRaycastBoundaries B;
  public readonly NavigationQuery Query=new NavigationQuery();
  public readonly NavigationQueryFilter Filter=new NavigationQueryFilter();
  public readonly uint A,BRef;
  public readonly bool Rotated;
  public PortalFixture(bool rotated){Rotated=rotated;Mesh=NavigationMeshTilesState.Create(0,0,0,1,1,4,8,new NavigationSpatialState());Mesh.UseObservedLinkConstants();byte[] a,b;
   if(!rotated){a=Serialize(0,0,new[]{V(0,0),V(0,4),V(1,4),V(1,0)},new[]{new NavigationTilePolygon(Quad(0,1,2,3),new ushort[]{65535,65535,0x8002,65535,65535,65535})},new NavigationTileBounds(0,0,0,1,2,4));b=Serialize(1,0,new[]{V(0,0),V(0,2),V(1,2),V(1,0)},new[]{new NavigationTilePolygon(Quad(0,1,2,3),new ushort[]{0x8000,65535,65535,65535,65535,65535})},new NavigationTileBounds(1,0,1,2,2,3));}
   else{a=Serialize(0,0,new[]{V(0,0),V(0,1),V(4,1),V(4,0)},new[]{new NavigationTilePolygon(Quad(0,1,2,3),new ushort[]{65535,0x8001,65535,65535,65535,65535})},new NavigationTileBounds(0,0,0,4,2,1));b=Serialize(0,1,new[]{V(0,0),V(0,1),V(2,1),V(2,0)},new[]{new NavigationTilePolygon(Quad(0,1,2,3),new ushort[]{65535,65535,65535,0x8003,65535,65535})},new NavigationTileBounds(1,0,1,3,2,2));}
   A=Add(Mesh,a);BRef=Add(Mesh,b);Query.Mesh0.Write(Mesh.Mesh);Filter.Include100.Write(1);Filter.Exclude102.Write(0);B=new NavigationCrowdRaycastBoundaries(Mesh);B.UseObservedRaycastConstants();
  }
  public NavigationMeshLink12 Link{get{var tile=Mesh.Mesh.Tiles50.Read(0)[0];var p=tile.Polys10.Read(0)[0];return Mesh.Tile(tile,0).Links20.Read(0)[Mesh.Poly(p,0).FirstLink00.Read(0)];}}
  public uint Cast(float coordinate,NavigationCrowdRaycastResult output){var from=Rotated?Point(coordinate,0,0.25f):Point(0.25f,0,coordinate);var to=Rotated?Point(coordinate,0,1.75f):Point(1.75f,0,coordinate);return NavigationCrowdRaycastContracts.Raycast263290(Query,A,from,to,Filter,output,B,new Invocation());}
 }
 public static void Run(){
  scenarios=0;assertions=0;
  Test("Real three-polygon ray follows produced links and writes the borrowed output range",delegate{
   var f=new Fixture();var r=Output(3);var backing=r.Path10.Read(0).Buffer;var cell=backing.Cells[1];f.B.PortalScale34B9CC.Forget();Equal(0x40000000u,f.Cast(f.From,f.To,r),"Internal full links need no partial portal scale");Equal(3u,r.Count18.Read(0),"Three produced polygon references");Equal(f.Root,backing.Cells[0].Read(0),"First ref");Equal(f.Root|1u,backing.Cells[1].Read(0),"Actual neighbor");Equal(f.Root|2u,backing.Cells[2].Read(0),"Last ref");Same(cell,backing.Cells[1],"Output cell identity retained");Same(f.Mesh.Mesh,f.Query.Mesh0.Read(0),"Same query mesh");Equal(0x7f7fffffu,NavigationCoreContracts.ToBits(r.Fraction00.Read(0)),"No-hit sentinel is max finite, not clamped one");Normal(r,0,0,0,"No hit normal");Equal(0u,r.Reserved20.Read(0),"Observed reserved write");Unknown(f.B.PortalScale34B9CC,"Unused operand stays unknown");
  });
  Test("Capacity truncates only reference output and still completes real geometric traversal",delegate{
   var f=new Fixture();var r=Output(1);Equal(0x40000010u,f.Cast(f.From,f.To,r),"Native success with buffer-too-small detail");Equal(1u,r.Count18.Read(0),"Written count not total visited count");Equal(f.Root,r.Path10.Read(0).At(0,0).Read(0),"First ref retained");Equal(0x7f7fffffu,NavigationCoreContracts.ToBits(r.Fraction00.Read(0)),"Reached target despite output truncation");
   var zero=new NavigationCrowdRaycastResult();zero.Capacity1C.Write(0);Equal(0x40000010u,f.Cast(f.From,f.To,zero),"Zero-capacity ray still traverses actual links");Equal(0u,zero.Count18.Read(0),"Zero count");Unknown(zero.Path10,"Zero capacity never invents or reads an output pointer");Equal(0x7f7fffffu,NavigationCoreContracts.ToBits(zero.Fraction00.Read(0)),"Zero-output complete ray");
   var negative=new NavigationCrowdRaycastResult();negative.Capacity1C.Write(-1);Equal(0x40000010u,f.Cast(f.From,f.To,negative),"Capacity comparison is signed");Unknown(negative.Path10,"Negative capacity does not dereference output");
  });
  Test("Wall hit reports fraction and normal after all genuinely reached polygons",delegate{
   var f=new Fixture();var r=Output(5);Equal(0x40000000u,f.Cast(f.From,Point(3.75f,0,0.5f),r),"A wall hit is a native success result");Equal(3u,r.Count18.Read(0),"Three reached polygons");Equal(NavigationCoreContracts.F(2.75f/3.5f),r.Fraction00.Read(0),"Fraction to outer wall");Normal(r,-1,0,0,"Native normalized right-edge normal");Unknown(r.Path10.Read(0).At(3,0),"No invented polygon beyond wall");
  });
  Test("Live neighbor flags and offmesh type are read from the shared mesh at crossing",delegate{
   var f=new Fixture();var neighbor=f.Mesh.Spatial.Poly(f.Poly(1),0);neighbor.Flags1C.Write(2);var r=Output(4);Equal(0x40000000u,f.Cast(f.From,f.To,r),"Inclusion rejection stops at wall");Equal(1u,r.Count18.Read(0),"No filtered neighbor appended");Equal(0.3f,r.Fraction00.Read(0),"First shared edge fraction");Normal(r,-1,0,0,"Filtered edge normal");neighbor.Flags1C.Write(1);f.Adapter.Filter18.Exclude102.Write(1);r=Output(4);f.Cast(f.From,f.To,r);Equal(1u,r.Count18.Read(0),"Exclusion rejection");f.Adapter.Filter18.Exclude102.Write(0);neighbor.Type1F.Write(0x40);neighbor.Flags1C.Forget();r=Output(4);f.Cast(f.From,f.To,r);Equal(1u,r.Count18.Read(0),"Offmesh type rejected before unknown flags");Unknown(neighbor.Flags1C,"No default flags supplied");
  });
  Test("Actual partial east portal uses produced quantized Z interval",delegate{
   var f=new PortalFixture(false);Equal((byte)0,f.Link.Side09.Read(0),"Produced east side");Equal((byte)63,f.Link.Min0A.Read(0),"Actual lower quantization");Equal((byte)191,f.Link.Max0B.Read(0),"Actual upper quantization");var through=Output(4);Equal(0x40000000u,f.Cast(2,through),"Inside produced partial portal");Equal(2u,through.Count18.Read(0),"Crossed real neighboring tile");Equal(f.BRef,through.Path10.Read(0).At(1,0).Read(0),"Produced neighboring ref");Equal(0x7f7fffffu,NavigationCoreContracts.ToBits(through.Fraction00.Read(0)),"Reached target");var blocked=Output(4);f.Cast(0.5f,blocked);Equal(1u,blocked.Count18.Read(0),"Outside interval stays in first tile");Equal(0.5f,blocked.Fraction00.Read(0),"Wall fraction");Normal(blocked,-1,0,0,"Partial portal wall normal");
  });
  Test("Actual partial north portal follows X-coordinate branch",delegate{
   var f=new PortalFixture(true);Equal((byte)2,f.Link.Side09.Read(0),"Produced north side");Equal((byte)63,f.Link.Min0A.Read(0),"Produced lower bound");Equal((byte)191,f.Link.Max0B.Read(0),"Produced upper bound");var r=Output(4);Equal(0x40000000u,f.Cast(2,r),"Inside X interval");Equal(2u,r.Count18.Read(0),"Real north tile reached");Equal(f.BRef,r.Path10.Read(0).At(1,0).Read(0),"Real north reference");r=Output(4);f.Cast(0.5f,r);Equal(1u,r.Count18.Read(0),"Outside X interval blocked");Normal(r,0,0,-1,"North edge normal");
  });
  Test("Partial-portal unknown stops after written path and fraction without final count",delegate{
   var f=new PortalFixture(false);f.B.PortalScale34B9CC.Forget();var r=Output(3);Missing(0x26384c,delegate{f.Cast(2,r);},"Real partial portal consumes missing scale");Equal(0u,r.Count18.Read(0),"Final count publication not reached");Equal(f.A,r.Path10.Read(0).At(0,0).Read(0),"Already written first reference remains");Equal(0.5f,r.Fraction00.Read(0),"Already written first exit fraction");Normal(r,0,0,0,"No fabricated hit normal");Unknown(r.Path10.Read(0).At(1,0),"No unknown-link fallback to next tile");
  });
  Test("Invalid initial reference preserves exact three-cell entry prefix",delegate{
   var r=new NavigationCrowdRaycastResult();r.Fraction00.Write(7);r.Count18.Write(8);r.Reserved20.Write(9);Equal(0x80000008u,NavigationCrowdRaycastContracts.Raycast263290(null,0,null,null,null,r,null,new Invocation()),"Zero ref rejects before any query or boundary access");Equal(0f,r.Fraction00.Read(0),"Fraction initialized");Equal(0u,r.Count18.Read(0),"Count initialized");Equal(0u,r.Reserved20.Read(0),"Reserved initialized");Unknown(r.Normal04.X,"Normal is after validation");Unknown(r.Capacity1C,"Capacity not consumed");
   var f=new Fixture();f.Tile.Salt00.Write(99);r=new NavigationCrowdRaycastResult();Equal(0x80000008u,f.Cast(f.From,f.To,r),"Real validation rejects wrong salt");Unknown(r.Normal04.Z,"Wrong salt still precedes normal initialization");
  });
  Test("Unknown geometry copies opaquely then stops at first arithmetic use",delegate{
   var f=new Fixture();var vertices=f.Tile.Vertices18.Read(0);for(int n=0;n<vertices.Length;n++)vertices[n].Y.Forget();f.From.Y.Forget();f.To.Y.Forget();var r=Output(3);Equal(0x40000000u,f.Cast(f.From,f.To,r),"Raycast does not require copied Y payload");Equal(3u,r.Count18.Read(0),"Complete XZ path");Unknown(vertices[0].Y,"No reconstructed height");vertices[2].X.Forget();r=Output(3);Missing(0x26359a,delegate{f.Cast(f.From,f.To,r);},"Previous vertex X becomes required at edge subtraction");Equal(0f,r.Fraction00.Read(0),"No completed clipping result");Equal(0u,r.Count18.Read(0),"No written count");Unknown(r.Path10.Read(0).At(0,0),"No premature path append");
  });
  Test("Unknown epsilon and capacity retain distinct output prefixes",delegate{
   var f=new Fixture();var r=Output(3);f.B.ParallelEpsilon34B93C.Forget();Missing(0x2635db,delegate{f.Cast(f.From,f.To,r);},"Parallel predicate needs exact operand");Equal(0f,r.Fraction00.Read(0),"Before clipping output");Normal(r,0,0,0,"Normal entry writes survived");f.B.UseObservedRaycastConstants();r.Capacity1C.Forget();Missing(0x263645,delegate{f.Cast(f.From,f.To,r);},"Capacity read follows fraction publication");Equal(0.3f,r.Fraction00.Read(0),"Fraction prefix");Equal(0u,r.Count18.Read(0),"Count still initial");Unknown(r.Path10.Read(0).At(0,0),"No output before capacity read");
  });
  Test("Borrowed output bounds failure does not rewrite already produced prefix",delegate{
   var f=new Fixture();var r=Output(1);r.Capacity1C.Write(3);Missing(0x26365d,delegate{f.Cast(f.From,f.To,r);},"Second store exceeds declared borrowed extent");Equal(f.Root,r.Path10.Read(0).At(0,0).Read(0),"First written ref retained");Equal(0.7f,r.Fraction00.Read(0),"Second polygon fraction already published");Equal(0u,r.Count18.Read(0),"Local increment not final output count");
   r=Output(5);var buffer=r.Path10.Read(0).Buffer;r.Path10.Write(new NavigationRefWindow(buffer,1));r.Capacity1C.Write(3);Equal(0x40000000u,f.Cast(f.From,f.To,r),"Nonzero borrowed base uses same buffer");Unknown(buffer.At(0,0),"Prefix before borrowed base untouched");Equal(f.Root,buffer.At(1,0).Read(0),"First borrowed store");Equal(f.Root|2u,buffer.At(3,0).Read(0),"Last borrowed store");Unknown(buffer.At(4,0),"Suffix after produced path untouched");
  });
  Test("Native early clipping rejection is not converted to an invented failure code",delegate{
   var f=new Fixture();var r=Output(3);Equal(0x40000000u,f.Cast(Point(0.25f,0,2),Point(2.75f,0,2),r),"Native success status with zero reachable prefix");Equal(0f,r.Fraction00.Read(0),"No polygon accepted");Equal(0u,r.Count18.Read(0),"Zero actual path count");Normal(r,0,0,0,"No wall normal on early clipping rejection");Unknown(r.Path10.Read(0).At(0,0),"No first path write");
   r=Output(3);Equal(0x40000000u,f.Cast(Point(Single.NaN,0,0.5f),f.To,r),"Unordered numerator follows native JC early exit");Equal(0u,r.Count18.Read(0),"No arbitrary NaN movement success");Equal(0f,r.Fraction00.Read(0),"NaN early exit keeps zero fraction");
  });
  Test("Triangle tail gather executes actual three-vertex geometry",delegate{
   var f=new Fixture();var mesh=NavigationMeshTilesState.Create(0,0,0,2,2,1,8,new NavigationSpatialState());mesh.UseObservedLinkConstants();uint root=Add(mesh,Serialize(0,0,new[]{V(0,0),V(0,2),V(2,0)},new[]{new NavigationTilePolygon(new ushort[]{0,1,2,65535,65535,65535},Closed())},new NavigationTileBounds(0,0,0,2,2,2)));var q=new NavigationQuery();q.Mesh0.Write(mesh.Mesh);var b=new NavigationCrowdRaycastBoundaries(mesh);b.UseObservedRaycastConstants();var r=Output(2);Equal(0x40000000u,NavigationCrowdRaycastContracts.Raycast263290(q,root,Point(0.25f,0,0.25f),Point(0.75f,0,0.5f),f.Adapter.Filter18,r,b,new Invocation()),"Tail gather geometry succeeds");Equal(1u,r.Count18.Read(0),"Single triangle reference");Equal(root,r.Path10.Read(0).At(0,0).Read(0),"Produced triangle ref");Equal(0x7f7fffffu,NavigationCoreContracts.ToBits(r.Fraction00.Read(0)),"Inside triangle target");
  });
  Test("Cyclic malformed link list stops explicitly and never reports fabricated wall",delegate{
   var f=new Fixture(4);var link=f.First(0);uint index=f.Mesh.Poly(f.Poly(0),0).FirstLink00.Read(0);link.Edge08.Write(0);link.Next04.Write(index);var r=Output(3);Missing(0x2636b4,delegate{f.Cast(f.From,f.To,r);},"Own finite traversal budget");Equal(f.Root,r.Path10.Read(0).At(0,0).Read(0),"Valid first prefix retained");Equal(0u,r.Count18.Read(0),"No native finish fabricated");Normal(r,0,0,0,"No wall result from cycle");
  });
  Test("Wrapper performs both real nearest calls then uses original endpoints",delegate{
   var f=new Fixture();Equal((byte)1,f.Traverse(f.From,f.To),"Actual nearest and actual strip ray");var r=f.B.LastAdapterResult;Check(r!=null,"Real caller scratch was allocated");Equal(3u,r.Count18.Read(0),"Actual ray crossed three polygons");Equal(32,r.Capacity1C.Read(0),"Observed caller capacity");Same(f.Query,f.Adapter.Query10.Read(0),"Query identity preserved");Equal(0,f.B.OwnOutstandingEnters,"Own wrapper lock released");
   var originalFrom=Point(0.25f,0,1.2f);var originalTo=Point(2.75f,0,1.2f);Equal((byte)0,f.Traverse(originalFrom,originalTo),"Projected nearby endpoints do not substitute for original ray endpoints");Check(f.B.LastAdapterResult!=null,"Both nearest calls found nearby real polygons");Equal(0f,f.B.LastAdapterResult.Fraction00.Read(0),"Actual outside ray rejected before append");Equal(0u,f.B.LastAdapterResult.Count18.Read(0),"No projected ray success");Equal(1.2f,originalFrom.Z.Read(0),"Original endpoint unchanged");Equal(0,f.B.OwnOutstandingEnters,"Rejected original ray lock released");
  });
  Test("Wrapper no-nearest and missing extent preserve stop semantics and release own lock",delegate{
   var f=new Fixture();Equal((byte)0,f.Traverse(f.From,Point(100,0,100)),"Missing second nearest ref gives false");Check(f.B.LastAdapterResult==null,"No ray result fabricated after nearest miss");Equal(0,f.B.OwnOutstandingEnters,"Nearest miss released lock");f.Adapter.Extent11C.X.Forget();Missing(0x261995,delegate{f.Traverse(f.From,f.To);},"Actual nearest extent read");Check(f.B.LastAdapterResult==null,"No ray executed after unknown extent");Equal(0,f.B.OwnOutstandingEnters,"Own exception cleanup");
  });
  Test("Wrapper consumes late threshold only after real ray and preserves ordered comparison",delegate{
   var f=new Fixture();Equal(0x322bcc77u,NavigationCoreContracts.ToBits(f.B.ParallelEpsilon34B93C.Read(0)),"Raw observed parallel epsilon bits");Equal(0x3f7d70a4u,NavigationCoreContracts.ToBits(f.B.VisibleThreshold34BB38.Read(0)),"Raw observed wrapper threshold bits");f.B.VisibleThreshold34BB38.Forget();Missing(0x030aec,delegate{f.Traverse(f.From,f.To);},"Late threshold unknown");Equal(3u,f.B.LastAdapterResult.Count18.Read(0),"Completed actual ray remains observable");Equal(0,f.B.OwnOutstandingEnters,"Threshold exception releases own lock");f.B.VisibleThreshold34BB38.Write(Single.NaN);Equal((byte)0,f.Traverse(f.From,f.To),"SETA does not treat unordered comparison as true");Equal(0,f.B.OwnOutstandingEnters,"Unordered threshold released lock");
  });
  Test("Subnormal source stays Unsupported without losing entry writes",delegate{
   var f=new Fixture();var r=Output(3);Missing(0x26353e,delegate{f.Cast(Point(NavigationCoreContracts.FromBits(1),0,0.5f),f.To,r);},"Unobserved FP denormal behavior");Equal(0f,r.Fraction00.Read(0),"Entry fraction preserved");Equal(0u,r.Count18.Read(0),"Entry count preserved");Normal(r,0,0,0,"Normal prefix retained");Unknown(r.Path10.Read(0).At(0,0),"Unsupported is not a ray success");
  });
 }
}
public static class NavigationCrowdRaycastTestProgram {
 public static int Main(){try{NavigationCrowdRaycastTests.Run();Console.WriteLine("PASS "+NavigationCrowdRaycastTests.Scenarios+" scenarios, "+NavigationCrowdRaycastTests.Assertions+" assertions; actual owned mesh-link raycast and two-nearest adapter only; native runtime, original scene and mouse movement unverified");return 0;}catch(Exception e){Console.Error.WriteLine("FAIL after "+NavigationCrowdRaycastTests.Scenarios+" scenarios, "+NavigationCrowdRaycastTests.Assertions+" assertions: "+e);return 1;}}
}
