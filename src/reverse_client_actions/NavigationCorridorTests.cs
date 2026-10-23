using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// New pure-memory fixtures for the recovered corridor algorithm. Geometry is
// serialized by the existing own TileData adapter, registered by production
// MeshTiles, and searched through production node-pool/heap operations. No
// callback supplies a corridor, portal midpoint, link, cost, or search result.
// These fixtures do not assert native runtime/loading or complete navigation.
public static class NavigationCorridorTests {
 static int scenarios,assertions;
 public static int Scenarios {get{return scenarios;}}
 public static int Assertions {get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Test(string name,Action body){body();scenarios++;Console.WriteLine("PASS "+name);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract e){Equal(at,e.Address,why+" address");return;}throw new Exception("Expected own unsupported observation: "+why);}
 static NavigationPoint3 Point(float x,float y,float z){var p=new NavigationPoint3();p.X.Write(x);p.Y.Write(y);p.Z.Write(z);return p;}
 static NavigationTileQuantizedVertex V(ushort x,ushort z){return new NavigationTileQuantizedVertex(x,0,z);}
 static ushort[] Six(ushort a,ushort b,ushort c){return new ushort[]{a,b,c,65535,65535,65535};}
 static ushort[] Closed(){return new ushort[]{65535,65535,65535,65535,65535,65535};}
 static byte[] Bytes(int x,int z,NavigationTileQuantizedVertex[] vertices,NavigationTilePolygon[] polys,byte[] areas,ushort[] flags,float width){
  var input=new NavigationTileDataInput(vertices,polys,areas,flags,new NavigationTileBounds(x,0,z,x+width,2,z+width),1,1,x,z,0,2,0.5f,1);
  var output=new NavigationTileDataOutput();var result=NavigationTileData.Build(input,output);
  Check(result.Outcome==NavigationTileDataOutcome.Published,"Own geometry fixture serializes: "+result.Detail);return output.Bytes;
 }
 static byte[] Pair(){return Bytes(0,0,new[]{V(0,0),V(1,0),V(1,1),V(0,1)},
  new[]{new NavigationTilePolygon(Six(0,1,2),Six(65535,65535,1)),new NavigationTilePolygon(Six(0,2,3),Six(0,65535,65535))},new byte[]{1,2},new ushort[]{1,2},1);}
 static byte[] Isolated(){return Bytes(0,3,new[]{V(0,0),V(1,0),V(0,1)},new[]{new NavigationTilePolygon(Six(0,1,2),Closed())},new byte[]{1},new ushort[]{1},1);}
 static byte[] Ring(){return Bytes(0,0,new[]{V(0,0),V(2,0),V(2,2),V(0,2),V(1,1)},
  new[]{new NavigationTilePolygon(Six(0,1,4),Six(65535,1,3)),new NavigationTilePolygon(Six(1,2,4),Six(65535,2,0)),
        new NavigationTilePolygon(Six(2,3,4),Six(65535,3,1)),new NavigationTilePolygon(Six(3,0,4),Six(65535,0,2))},
  new byte[]{1,2,3,4},new ushort[]{1,1,1,1},2);}
 static byte[] Square(int x,bool east){var neighbors=Closed();neighbors[east?2:0]=east?(ushort)0x8002:(ushort)0x8000;
  return Bytes(x,0,new[]{V(0,0),V(0,1),V(1,1),V(1,0)},new[]{new NavigationTilePolygon(new ushort[]{0,1,2,3,65535,65535},neighbors)},new byte[]{1},new ushort[]{1},1);}
 sealed class Fixture {
  public readonly NavigationMeshTilesState Mesh;
  public readonly NavigationQuery Query=new NavigationQuery();
  public readonly NavigationQueryFilter Filter=new NavigationQueryFilter();
  public readonly NavigationCorridorState Search;
  public readonly NavigationCorridorFilterCosts Costs;
  public readonly NavigationCorridorBoundaries B;
  public NavigationRefBuffer Output=new NavigationRefBuffer(8);
  public readonly Field<uint> Count=new Field<uint>();
  public uint Start,End;
  public NavigationPoint3 From=Point(0.8f,0,0.2f),To=Point(0.2f,0,0.8f);
  public Fixture(int nodes){
   Mesh=NavigationMeshTilesState.Create(0,0,0,1,1,4,8,new NavigationSpatialState());Mesh.UseObservedLinkConstants();Query.Mesh0.Write(Mesh.Mesh);
   Search=new NavigationCorridorState(Query);Search.Pool58.Write(NavigationSearchStorage.CreatePool26DC10(nodes,8));Search.Queue60.Write(NavigationSearchStorage.CreateQueue(32));
   Filter.Include100.Write(65535);Filter.Exclude102.Write(0);Costs=new NavigationCorridorFilterCosts(Filter);for(int n=0;n<64;n++)Costs.AreaCosts00[n].Write(1);
   Search.Attach(Costs);B=new NavigationCorridorBoundaries(Mesh,Search);B.UseObservedCorridorConstants();Count.Write(99);
  }
  public uint Add(byte[] bytes){var reference=new Field<uint>();var result=NavigationMeshTiles.Add(Mesh,new NavigationMeshTileBytes(bytes,bytes.Length),reference);Check(result.Outcome==NavigationMeshTileOutcome.NativeSuccess,"Fixture tile registered: "+result.Detail);return reference.Read(0);}
  public uint Call(int capacity){return NavigationCorridorContracts.FindPath(Query,Start,End,From,To,Filter,Output,Count,capacity,B,new Invocation());}
 }
 static Fixture Paired(int nodes){var f=new Fixture(nodes);f.Start=f.Add(Pair());f.End=f.Start|1;return f;}
 static void Path(Fixture f,params uint[] refs){Equal((uint)refs.Length,f.Count.Read(0),"Output count");for(int n=0;n<refs.Length;n++)Equal(refs[n],f.Output.Cells[n].Read(0),"Ordered corridor reference "+n);}
 static void Unknown<T>(Field<T> field,string why){Missing(0,delegate{field.Read(0);},why);}

 public static void Run(){
  scenarios=0;assertions=0;
  Test("Production cross-polygon search retains mesh query node and byte identities",delegate{
   var f=Paired(16);var pool=f.Search.Pool58.Read(0);var nodes=pool.Nodes00.Read(0);var startNode=nodes[0];var endNode=nodes[1];
   var tile=f.Mesh.Mesh.Tiles50.Read(0)[0];var data=f.Mesh.Tile(tile,0).Data50.Read(0);var bytes=data.Bytes;var links=f.Mesh.Tile(tile,0).Links20.Read(0);
   Equal(0x40000000u,f.Call(8),"Untruncated complete path");Path(f,f.Start,f.End);
   Same(f.Mesh.Mesh,f.Query.Mesh0.Read(0),"Query uses the registered mesh");Same(pool,f.Search.Pool58.Read(0),"Shared pool was cleared and used, not replaced");Same(startNode,nodes[0],"Start node identity");Same(endNode,nodes[1],"End node identity");Same(data,f.Mesh.Tile(tile,0).Data50.Read(0),"Mesh byte owner retained");Same(bytes,data.Bytes,"Original owned byte array retained");Same(links,f.Mesh.Tile(tile,0).Links20.Read(0),"Actual adjacency links retained");
   Equal(2,pool.Count20.Read(0),"Two unique nodes allocated");Equal(f.Start,startNode.Reference18.Read(0),"Start reference is uint");Equal(f.End,endNode.Reference18.Read(0),"End reference is uint");
   Equal(0.5f,endNode.Position00.X.Read(0),"Actual shared-edge midpoint X");Equal(0.5f,endNode.Position00.Z.Read(0),"Actual shared-edge midpoint Z");
   Check(endNode.Cost0C.Read(0)>0.84f&&endNode.Cost0C.Read(0)<0.86f,"Cost includes both start-to-edge and edge-to-destination segments");
   var otherQuery=new NavigationQuery();otherQuery.Mesh0.Write(f.Mesh.Mesh);Missing(0x261d51,delegate{NavigationCorridorContracts.FindPath(otherQuery,f.Start,f.End,f.From,f.To,f.Filter,f.Output,f.Count,8,f.B,new Invocation());},"Same mesh does not permit substituting another query for the registered search owner");Equal(0u,f.Count.Read(0),"Identity stop follows initial count clear");Equal(2,pool.Count20.Read(0),"Wrong query did not reset actual pool");
  });
  Test("Actual external tile links produce a cross-tile corridor",delegate{
   var f=new Fixture(16);f.Start=f.Add(Square(0,true));f.End=f.Add(Square(1,false));f.From=Point(0.25f,0,0.5f);f.To=Point(1.75f,0,0.5f);
   Equal(0x40000000u,f.Call(8),"Connected external portal path");Path(f,f.Start,f.End);var goal=f.Search.Pool58.Read(0).Nodes00.Read(0)[1];
   Equal(1f,goal.Position00.X.Read(0),"Actual full-width edge midpoint X");Equal(0.5f,goal.Position00.Z.Read(0),"Actual full-width edge midpoint Z");Equal(1.5f,goal.Cost0C.Read(0),"External path distance cost");
  });
  Test("Disconnected target returns the reachable partial corridor",delegate{
   var f=Paired(16);uint middle=f.End;f.End=f.Add(Isolated());f.To=Point(0.2f,0,3.2f);
   Equal(0x40000040u,f.Call(8),"Native partial-result detail");Path(f,f.Start,middle);Equal(2,f.Search.Pool58.Read(0).Count20.Read(0),"Unreachable target does not fabricate a node");
  });
  Test("Neighbor node exhaustion combines partial and out-of-nodes status",delegate{
   var f=Paired(1);Equal(0x40000060u,f.Call(8),"Partial plus exhausted-node detail bits");Path(f,f.Start);Equal(1,f.Search.Pool58.Read(0).Count20.Read(0),"No allocation beyond declared pool");
  });
  Test("Exact capacity reports buffer detail despite containing the complete corridor",delegate{
   var f=Paired(16);f.Output=new NavigationRefBuffer(2);Equal(0x40000010u,f.Call(2),"Native exact-fill detail");Path(f,f.Start,f.End);
  });
  Test("Short capacity reverses the complete parent chain before copying its prefix",delegate{
   var f=Paired(16);var observed=f.Search.Pool58.Read(0).Nodes00.Read(0);observed[0].Packed14.Write(0xe0000000);observed[1].Packed14.Write(0xe0000000);f.Output=new NavigationRefBuffer(1);Equal(0x40000010u,f.Call(1),"Native short-buffer detail");Path(f,f.Start);
   var nodes=f.Search.Pool58.Read(0).Nodes00.Read(0);Equal(2u,nodes[0].Packed14.ReadMasked(0xffffff,0),"Start now points forward to end node");Equal(0u,nodes[1].Packed14.ReadMasked(0xffffff,0),"Goal terminates fully reversed chain");
   Equal(0xe8000000u,nodes[0].Packed14.ReadMasked(0xff000000,0),"Reversal preserves observed high bits and closed flag");Equal(0xe8000000u,nodes[1].Packed14.ReadMasked(0xff000000,0),"Goal high byte also preserved");
  });
  Test("Zero capacity still attempts and counts the first native output write",delegate{
   var f=Paired(16);f.Output=new NavigationRefBuffer(1);Equal(0x40000010u,f.Call(0),"Native capacity-zero behavior, not a safe empty-output invention");Path(f,f.Start);
  });
  Test("Include and exclude fields gate actual neighbors before costs or allocation",delegate{
   var f=Paired(16);f.Filter.Include100.Write(1);for(int n=0;n<64;n++)f.Costs.AreaCosts00[n].Forget();
   Equal(0x40000040u,f.Call(8),"Neighbor flag2 fails include1");Path(f,f.Start);Equal(1,f.Search.Pool58.Read(0).Count20.Read(0),"Filtered neighbor has no search node");
   f.Filter.Include100.Write(65535);f.Filter.Exclude102.Write(2);Equal(0x40000040u,f.Call(8),"Exclude rejects even when include matches");Path(f,f.Start);
  });
  Test("Area costs select between two genuine routes and reuse the same storage",delegate{
   var f=new Fixture(16);f.Start=f.Add(Ring());f.End=f.Start|2;f.From=Point(1,0,0.2f);f.To=Point(1,0,1.8f);var pool=f.Search.Pool58.Read(0);var nodes=pool.Nodes00.Read(0);var first=nodes[0];
   f.Costs.AreaCosts00[2].Write(10);Equal(0x40000000u,f.Call(8),"Expensive right route");Path(f,f.Start,f.Start|3,f.End);
   f.Costs.AreaCosts00[2].Write(1);f.Costs.AreaCosts00[4].Write(10);Equal(0x40000000u,f.Call(8),"Expensive left route");Path(f,f.Start,f.Start|1,f.End);
   f.Costs.AreaCosts00[4].Write(1);Equal(0x40000000u,f.Call(8),"Equal-cost alternatives");Path(f,f.Start,f.Start|1,f.End);
   Same(pool,f.Search.Pool58.Read(0),"Pool identity reused");Same(nodes,pool.Nodes00.Read(0),"Same node backing array");Same(first,nodes[0],"First node reused after real clear");Check(pool.Count20.Read(0)<=4,"Old allocation count did not accumulate");
  });
  Test("Production node hash distinguishes side state and preserves uninitialized high bits",delegate{
   var pool=NavigationSearchStorage.CreatePool26DC10(2,1);var nodes=pool.Nodes00.Read(0);nodes[1].Packed14.Write(0xe0000000);
   var first=NavigationSearchStorage.GetNode26DCA0(pool,0xf1234567,0);Same(nodes[0],first,"First actual pool slot");
   Same(first,NavigationSearchStorage.GetNode26DCA0(pool,0xf1234567,0),"Same integer ref and state returns identical node");
   Equal(0x1fffffffu,first.Packed14.KnownMask,"Unwritten top three bits remain unknown");Missing(0,delegate{first.Packed14.Read(0);},"No invented fully initialized packed word");Unknown(first.Position00.X,"Allocation does not initialize position");
   var other=NavigationSearchStorage.GetNode26DCA0(pool,0xf1234567,1);Same(nodes[1],other,"Same ref with different side state needs a distinct node");Equal(0xe1000000u,other.Packed14.Read(0),"Preserved known high bits plus native side bits");
   Same(null,NavigationSearchStorage.GetNode26DCA0(pool,0x12345678,0),"Production pool exhaustion returns null");Equal(2,pool.Count20.Read(0),"Failed allocation does not increment count");Same(first,NavigationSearchStorage.GetNode26DCA0(pool,0xf1234567,0),"Existing identity remains searchable after exhaustion");
  });
  Test("Production heap observes equal totals NaN and exact node identity",delegate{
   var q=NavigationSearchStorage.CreateQueue(4);var a=new NavigationSearchNode();var b=new NavigationSearchNode();var c=new NavigationSearchNode();a.Total10.Write(5);b.Total10.Write(5);c.Total10.Write(8);
   NavigationSearchStorage.Push25F680(q,a);NavigationSearchStorage.Push25F680(q,b);NavigationSearchStorage.Push25F680(q,c);var cells=q.Heap00.Read(0);Same(a,cells[0].Read(0),"Equal incoming total does not replace existing root");Same(b,cells[1].Read(0),"Equal node remains at insertion slot");
   c.Total10.Write(1);NavigationSearchStorage.Modify25F6F0(q,c);Same(c,cells[0].Read(0),"Decrease-key moves the same object up");
   q.Count0C.Write(2);NavigationSearchStorage.TrickleDown26DDC0(q,cells[2].Snapshot());Same(b,cells[0].Read(0),"Equal replacement preserves left-child preference");
   var n=new NavigationSearchNode();n.Total10.Write(Single.NaN);NavigationSearchStorage.Push25F680(q,n);Same(n,cells[0].Read(0),"Unordered comparison bubbles NaN upward rather than treating it as infinity");
   var absent=new NavigationSearchNode();NavigationSearchStorage.Modify25F6F0(q,absent);Same(n,cells[0].Read(0),"Absent node is found by identity, with no attempt to read its unknown total");
  });
  Test("Heap unknown-total failure preserves the native count increment prefix",delegate{
   var q=NavigationSearchStorage.CreateQueue(2);var known=new NavigationSearchNode();known.Total10.Write(1);NavigationSearchStorage.Push25F680(q,known);var unknown=new NavigationSearchNode();
   Missing(0x25f69e,delegate{NavigationSearchStorage.Push25F680(q,unknown);},"Candidate total is required only after count increment");Equal(2,q.Count0C.Read(0),"Increment is not rolled back");Same(known,q.Heap00.Read(0)[0].Read(0),"Existing root was not changed");Unknown(q.Heap00.Read(0)[1],"No manufactured new heap pointer after failure");
  });
  Test("Zero and invalid references clear output count before any search reset",delegate{
   var f=Paired(16);uint original=f.Start;var pool=f.Search.Pool58.Read(0);pool.Count20.Write(7);f.Start=0;f.Query.Mesh0.Forget();
   Equal(0x80000008u,f.Call(8),"Zero reference native failure without consuming unknown mesh");Equal(0u,f.Count.Read(0),"Count cleared before validation");Equal(7,pool.Count20.Read(0),"Invalid input does not reset search pool");Unknown(f.Output.Cells[0],"No path synthesized on validation failure");
   f.Start=original;f.Query.Mesh0.Write(f.Mesh.Mesh);f.End=1;f.Count.Write(99);Equal(0x80000008u,f.Call(8),"Nonzero invalid salt rejected by actual validator");Equal(0u,f.Count.Read(0),"Count again cleared");Equal(7,pool.Count20.Read(0),"Second validation failure does not reset pool");
  });
  Test("Same-polygon fast path does not invent requirements for search storage or costs",delegate{
   var f=Paired(16);f.End=f.Start;f.Search.Pool58.Forget();f.Search.Queue60.Forget();f.Filter.Include100.Forget();f.Filter.Exclude102.Forget();f.From.X.Forget();f.To.Z.Forget();f.B.HeuristicScale34BB3C.Forget();
   Equal(0x40000000u,f.Call(0),"Native same-reference path ignores capacity and unused fields");Path(f,f.Start);Unknown(f.Search.Pool58,"Unused pool stays unknown");Unknown(f.Search.Queue60,"Unused queue stays unknown");
  });
  Test("Unknown filter stops after closing start and before allocating neighbor",delegate{
   var f=Paired(16);f.Filter.Include100.Forget();Missing(0x261f9a,delegate{f.Call(8);},"Unknown include mask is not pass-all");
   Equal(0u,f.Count.Read(0),"No final output count on missing observation");Unknown(f.Output.Cells[0],"No output prefix yet");var pool=f.Search.Pool58.Read(0);Equal(1,pool.Count20.Read(0),"Neighbor was not allocated");Equal(0x08000000u,pool.Nodes00.Read(0)[0].Packed14.ReadMasked(0x1c000000,0),"Start is already closed");Equal(0,f.Search.Queue60.Read(0).Count0C.Read(0),"Start pop remains consumed");
  });
  Test("Unknown current or goal area cost preserves computed portal and allocated-node prefix",delegate{
   var f=Paired(16);f.Costs.AreaCosts00[1].Forget();Missing(0x2620cb,delegate{f.Call(8);},"Unknown current-polygon cost is not default one");
   var pool=f.Search.Pool58.Read(0);var end=pool.Nodes00.Read(0)[1];Equal(2,pool.Count20.Read(0),"Neighbor allocated before cost access");Equal(0.5f,end.Position00.X.Read(0),"Real portal midpoint survives cost stop");Equal(0f,end.Cost0C.Read(0),"GetNode zero cost has not been replaced");Equal(0u,end.Packed14.ReadMasked(0x1cffffff,0),"Parent and open flags not yet published");Equal(0u,f.Count.Read(0),"No final count");
   f.Costs.AreaCosts00[1].Write(1);f.Costs.AreaCosts00[2].Forget();Missing(0x2620e2,delegate{f.Call(8);},"Goal area is separately consumed for final segment");Same(end,pool.Nodes00.Read(0)[1],"Same pool slot reused on second attempt");Equal(0f,end.Cost0C.Read(0),"Computed intermediate cost is not prematurely published");
  });
  Test("Opaque start-position copy preserves unknown X until its first arithmetic read",delegate{
   var f=Paired(16);f.From.X.Forget();Missing(0x261dec,delegate{f.Call(8);},"Unobserved X survives initial MOV copies");var start=f.Search.Pool58.Read(0).Nodes00.Read(0)[0];
   Unknown(start.Position00.X,"Node X copy remains unknown");Equal(0f,start.Position00.Y.Read(0),"Y copy completed");Equal(0.2f,start.Position00.Z.Read(0),"Z copy completed");Equal(0f,start.Cost0C.Read(0),"Cost zero prefix retained");Equal(0u,start.Packed14.ReadMasked(0x1c000000,0),"Open flag has not been published at arithmetic failure");Equal(0,f.Search.Queue60.Read(0).Count0C.Read(0),"No heap push before arithmetic completes");
  });
  Test("Missing heuristic constant stops after start flags but before heap push",delegate{
   var f=Paired(16);f.B.HeuristicScale34BB3C.Forget();Missing(0x261e11,delegate{f.Call(8);},"Unknown constant is consumed by MULSS after opaque load");var start=f.Search.Pool58.Read(0).Nodes00.Read(0)[0];
   Equal(0x04000000u,start.Packed14.ReadMasked(0x1c000000,0),"Open flag already written");Equal(0f,start.Total10.Read(0),"Original GetNode total persists until computed total store");Equal(0,f.Search.Queue60.Read(0).Count0C.Read(0),"No heap push");Equal(0u,f.Count.Read(0),"Output remains cleared");
  });
  Test("Output extent failure retains reversed parents and leaves final count unwritten",delegate{
   var f=Paired(16);f.Output=new NavigationRefBuffer(0);Missing(0x262346,delegate{f.Call(0);},"First native output write still requires real storage");var nodes=f.Search.Pool58.Read(0).Nodes00.Read(0);
   Equal(2u,nodes[0].Packed14.ReadMasked(0xffffff,0),"Start parent already reversed");Equal(0u,nodes[1].Packed14.ReadMasked(0xffffff,0),"Goal parent already reversed");Equal(0u,f.Count.Read(0),"Local written increment has not reached output count");
  });
  Test("Shared output-count cell observes final count after ordered reference writes",delegate{
   var f=Paired(16);f.Output=new NavigationRefBuffer(new Field<uint>[]{f.Count,new Field<uint>()});Equal(0x40000000u,f.Call(8),"Same typed output/count cell is an explicit own alias fixture");
   Same(f.Count,f.Output.Cells[0],"Exact shared cell, not copied value");Equal(2u,f.Output.Cells[0].Read(0),"Final count overwrites first reference");Equal(f.End,f.Output.Cells[1].Read(0),"Second reference remains");
  });
 }
}

public static class NavigationCorridorTestProgram {
 public static int Main(){try{NavigationCorridorTests.Run();Console.WriteLine("PASS "+NavigationCorridorTests.Scenarios+" scenarios, "+NavigationCorridorTests.Assertions+" assertions; production corridor, search storage and mesh links over own geometry; not native runtime or complete navigation");return 0;}catch(Exception e){Console.Error.WriteLine("FAIL after "+NavigationCorridorTests.Scenarios+" scenarios, "+NavigationCorridorTests.Assertions+" assertions: "+e);return 1;}}
}
