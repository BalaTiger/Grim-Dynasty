using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Explicit in-memory mesh views exercise recovered default producers. They do
// not load original maps, initialize native navigation or move a game actor.
// MockSelector cases below are callback/selector observations only.
public static class NavigationNearestTests {
 static int scenarios,assertions;
 public static int Scenarios {get{return scenarios;}}
 public static int Assertions {get{return assertions;}}
 static void Check(bool ok,string why){assertions++;if(!ok)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Unknown<T>(Field<T> value){try{value.Read(1);throw new Exception("Unexpected known field");}catch(MissingContract){assertions++;}}
 static void Missing(Outcome outcome,uint at){Check(!outcome.ReturnedNormally,"Expected explicit unsupported observation");Equal(at,outcome.Address,"Failure address");}
 static void Test(string name,Action body){body();scenarios++;Console.WriteLine("PASS "+name);}
 static NavigationPoint3 P(float x,float y,float z){var p=new NavigationPoint3();Set(p,x,y,z);return p;}
 static void Set(NavigationPoint3 p,float x,float y,float z){p.X.Write(x);p.Y.Write(y);p.Z.Write(z);}
 static void Point(NavigationPoint3 p,float x,float y,float z,string why){Equal(x,p.X.Read(1),why+" X");Equal(y,p.Y.Read(1),why+" Y");Equal(z,p.Z.Read(1),why+" Z");}
 static uint Ref(int tile,int poly){return 16u|((uint)tile<<2)|(uint)poly;}
 sealed class Layer {
  public readonly NavigationTile Tile=new NavigationTile();
  public readonly NavigationTileHeader Header=new NavigationTileHeader();
  public readonly NavigationPoly Poly=new NavigationPoly();
  public readonly NavigationTileSpatial TileView;
  public readonly NavigationHeaderSpatial HeaderView;
  public readonly NavigationPolySpatial PolyView;
  public readonly NavigationDetail12 Detail=new NavigationDetail12();
  public readonly NavigationTriangle4 Triangle=new NavigationTriangle4();
  public Layer(NavigationSpatialState spatial,float height){
   TileView=new NavigationTileSpatial(Tile);HeaderView=new NavigationHeaderSpatial(Header);PolyView=new NavigationPolySpatial(Poly);
   spatial.Attach(TileView);spatial.Attach(HeaderView);spatial.Attach(PolyView);
   Tile.Salt00.Write(1);Header.PolyCount18.Write(1);Tile.Header08.Write(Header);
   Poly.VertexCount1E.Write(3);for(int n=0;n<3;n++)Poly.Indices04[n].Write((ushort)n);
   Tile.Polys10.Write(new NavigationPoly[]{Poly});Tile.Vertices18.Write(new NavigationPoint3[]{P(0,height,0),P(4,height,0),P(0,height,4)});
   HeaderView.GridX08.Write(0);HeaderView.GridZ0C.Write(0);HeaderView.Climb44.Write(0);
   PolyView.Type1F.Write(0);PolyView.Flags1C.Write(1);TileView.Next60.Write(null);TileView.BVTree40.Write(null);
   Detail.VertexBase00.Write(0);Detail.TriangleBase04.Write(0);Detail.TriangleCount09.Write(1);
   Triangle.A00.Write(0);Triangle.B01.Write(1);Triangle.C02.Write(2);
   TileView.Details28.Write(new NavigationDetail12[]{Detail});TileView.TriangleRecords38.Write(new NavigationTriangle4[]{Triangle});
  }
 }
 sealed class F {
  public readonly NavigationSpatialState Spatial=new NavigationSpatialState();
  public readonly NavigationQuery Query=new NavigationQuery();
  public readonly NavigationMesh Mesh=new NavigationMesh();
  public readonly NavigationMeshSpatial MeshView;
  public readonly NavigationQueryFilter Filter=new NavigationQueryFilter();
  public readonly Layer[] Layers;
  public readonly NavigationNearestBoundaries B;
  public readonly NavigationPoint3 Extent=P(0.25f,100,0.25f);
  public F(params float[] heights){
   MeshView=new NavigationMeshSpatial(Mesh);Spatial.Attach(MeshView);Query.Mesh0.Write(Mesh);
   Mesh.PolyBits60.Write(2);Mesh.TileBits5C.Write(2);Mesh.SaltBits58.Write(28);Mesh.TileLimit30.Write((uint)heights.Length);
   MeshView.OriginX1C.Write(0);MeshView.OriginZ24.Write(0);MeshView.TileWidth28.Write(10);MeshView.TileDepth2C.Write(10);MeshView.HashMask38.Write(0);
   Layers=new Layer[heights.Length];var tiles=new NavigationTile[heights.Length];
   for(int n=0;n<heights.Length;n++){Layers[n]=new Layer(Spatial,heights[n]);tiles[n]=Layers[n].Tile;}
   Mesh.Tiles50.Write(tiles);for(int n=0;n<tiles.Length;n++){Layers[n].TileView.BindArrayLocation(tiles,n);Layers[n].TileView.Next60.Write(n+1<tiles.Length?tiles[n+1]:null);}
   MeshView.Buckets40.Write(new NavigationTile[]{tiles.Length==0?null:tiles[0]});Filter.Include100.Write(1);Filter.Exclude102.Write(0);
   B=new NavigationNearestBoundaries(Spatial);B.UseObservedNearestConstants();
  }
  public uint Nearest(NavigationPoint3 input,Field<uint> reference,NavigationPoint3 output){return NavigationNearestContracts.Nearest(Query,input,Extent,Filter,reference,output,B,new Invocation());}
  public uint Enumerate(NavigationPoint3 input,NavigationRefBuffer refs,Field<uint> count,int capacity){return NavigationEnumerationContracts.Enumerate(Query,input,Extent,Filter,refs,count,capacity,B,new Invocation());}
 }
 static NavigationBvNode16 Node(ushort min,ushort max,int index){var n=new NavigationBvNode16();n.MinX00.Write(min);n.MinY02.Write(min);n.MinZ04.Write(min);n.MaxX06.Write(max);n.MaxY08.Write(max);n.MaxZ0A.Write(max);n.Index0C.Write(index);return n;}
 static void Bv(Layer layer,NavigationBvNode16[] nodes){layer.TileView.BVTree40.Write(nodes);layer.HeaderView.BvNodeCount30.Write(nodes.Length);Set(layer.HeaderView.BoundsMin48,0,0,0);Set(layer.HeaderView.BoundsMax54,10,10,10);layer.HeaderView.BvQuantFactor60.Write(1);}
 static void MorePoly(F f,int count){
  var l=f.Layers[0];var polygons=new NavigationPoly[count];polygons[0]=l.Poly;
  for(int n=1;n<count;n++){var p=new NavigationPoly();p.VertexCount1E.Write(3);for(int k=0;k<3;k++)p.Indices04[k].Write((ushort)k);var side=new NavigationPolySpatial(p);side.Type1F.Write(0);side.Flags1C.Write(1);f.Spatial.Attach(side);polygons[n]=p;}
  l.Tile.Polys10.Write(polygons);l.Header.PolyCount18.Write((uint)count);
 }
 // Selector-only injected observations, not a production candidate producer.
 sealed class MockSelector:NavigationNearestBoundaries {
  public readonly uint[] References;
  public uint EnumerationStatus=NavigationCoreContracts.Success;
  public uint? CountOverride;
  public int ClosestCalls;
  public Action<int,uint,NavigationPoint3,NavigationPoint3,Field<byte>> Observe;
  public uint ClosestStatus=NavigationCoreContracts.Success;
  public readonly List<NavigationPoint3> Scratch=new List<NavigationPoint3>();
  public MockSelector(NavigationSpatialState spatial,params uint[] refs):base(spatial){References=refs;UseObservedNearestConstants();}
  public override uint Enumerate261950(NavigationQuery query,NavigationPoint3 point,NavigationPoint3 extent,NavigationQueryFilter filter,NavigationRefBuffer refs,Field<uint> count,int capacity){Equal(128,capacity,"Fixed native candidate capacity");for(int n=0;n<References.Length;n++)refs.Cells[n].Write(References[n]);count.Write(CountOverride.HasValue?CountOverride.Value:(uint)References.Length);return EnumerationStatus;}
  public override uint Closest260520(NavigationQuery query,uint reference,NavigationPoint3 point,NavigationPoint3 output,Field<byte> over){int n=ClosestCalls++;Scratch.Add(output);if(Observe!=null)Observe(n,reference,point,output,over);return ClosestStatus;}
 }
 // Observes the call boundary without dereferencing the three LEA addresses.
 // Returning zero here is a mock result, not an implemented height algorithm.
 sealed class MockNonConsumingHeight:NavigationNearestBoundaries {
  public int Calls;
  public MockNonConsumingHeight(NavigationSpatialState spatial):base(spatial){UseObservedNearestConstants();}
  public override byte Height26D580(NavigationPoint3 point,NavigationNearestPointAddress a,NavigationNearestPointAddress b,NavigationNearestPointAddress c,Field<float> height){Calls++;return 0;}
 }
 static uint Select(F f,MockSelector b,NavigationPoint3 input,Field<uint> reference,NavigationPoint3 output){return NavigationNearestContracts.Nearest(f.Query,input,f.Extent,f.Filter,reference,output,b,new Invocation());}
 public static void Run(){
  Test("production empty bucket returns no ref and preserves optional output",delegate{
   var f=new F();var refs=new NavigationRefBuffer(2);var count=Field<uint>.Of(99);Equal(NavigationCoreContracts.Success,f.Enumerate(P(1,2,1),refs,count,2),"Empty enumerate");Equal(0u,count.Read(1),"Empty count");Unknown(refs.Cells[0]);
   var result=Field<uint>.Of(77);var output=P(8,9,10);Equal(NavigationCoreContracts.Success,f.Nearest(P(1,2,1),result,output),"Empty nearest");Equal(0u,result.Read(1),"Explicit no-result ref");Point(output,8,9,10,"No invented endpoint");
   f.B.MaxFinite34BDCC.Forget();Missing(MoveContracts.Run(delegate(Invocation i){NavigationNearestContracts.Nearest(f.Query,P(1,2,1),f.Extent,f.Filter,result,output,f.B,i);}),0x261165);Equal(0u,result.Read(1),"Initial ref clear precedes constant read even with empty candidates");
  });
  Test("production enumeration and equal nearest preserve bucket chain order",delegate{
   var f=new F(0,8);var refs=new NavigationRefBuffer(3);var count=new Field<uint>();Equal(NavigationCoreContracts.Success,f.Enumerate(P(1,4,1),refs,count,3),"Enumerate both layers");Equal(2u,count.Read(1),"Two candidates");Equal(Ref(0,0),refs.Cells[0].Read(1),"First tile");Equal(Ref(1,0),refs.Cells[1].Read(1),"Second tile");
   var result=new Field<uint>();var output=new NavigationPoint3();f.Nearest(P(1,4,1),result,output);Equal(Ref(0,0),result.Read(1),"Equal distances keep first");Point(output,1,0,1,"First layer point");
   f.MeshView.Buckets40.Write(new NavigationTile[]{f.Layers[1].Tile});f.Layers[1].TileView.Next60.Write(f.Layers[0].Tile);f.Layers[0].TileView.Next60.Write(null);
   f.Nearest(P(1,4,1),result,output);Equal(Ref(1,0),result.Read(1),"Reversing actual chain changes tie winner");Point(output,1,8,1,"Second layer point");
  });
  Test("production linear filtering skips type include and exclude before vertices",delegate{
   var f=new F(0);MorePoly(f,4);var polys=f.Layers[0].Tile.Polys10.Read(1);
   f.Spatial.Poly(polys[0],1).Type1F.Write(0x40);f.Spatial.Poly(polys[1],1).Flags1C.Write(2);f.Spatial.Poly(polys[2],1).Flags1C.Write(3);f.Filter.Exclude102.Write(2);
   for(int n=0;n<3;n++){polys[n].VertexCount1E.Forget();polys[n].Indices04[0].Forget();}
   var refs=new NavigationRefBuffer(4);var count=new Field<uint>();Equal(NavigationCoreContracts.Success,f.Enumerate(P(1,0,1),refs,count,4),"Filtered enumerate");Equal(1u,count.Read(1),"Only fourth survives");Equal(Ref(0,3),refs.Cells[0].Read(1),"Native polygon index retained");Unknown(refs.Cells[1]);
  });
  Test("production enumeration capacity is a prefix with buffer-too-small status",delegate{
   var f=new F(0,8);var refs=new NavigationRefBuffer(2);refs.Cells[1].Write(77);var count=new Field<uint>();Equal(0x40000010u,f.Enumerate(P(1,4,1),refs,count,1),"Capacity status");Equal(1u,count.Read(1),"Exactly capacity");Equal(Ref(0,0),refs.Cells[0].Read(1),"First actual ref");Equal(77u,refs.Cells[1].Read(1),"Tail untouched");
  });
  Test("production negative grid coordinate uses floor not truncation",delegate{
   var f=new F(0);var l=f.Layers[0];l.HeaderView.GridX08.Write(-1);l.Tile.Vertices18.Write(new NavigationPoint3[]{P(-4,0,0),P(0,0,0),P(0,0,4)});var refs=new NavigationRefBuffer(1);var count=new Field<uint>();
   Equal(0x40000010u,f.Enumerate(P(-1,0,1),refs,count,1),"Negative cell found");Equal(1u,count.Read(1),"Matching -1 tile");Equal(Ref(0,0),refs.Cells[0].Read(1),"Negative cell ref");
  });
  Test("production candidate failure preserves emitted prefix and old external count",delegate{
   var f=new F(0,8);f.Layers[1].PolyView.Flags1C.Forget();var refs=new NavigationRefBuffer(3);var count=Field<uint>.Of(99);
   var outcome=MoveContracts.Run(delegate(Invocation i){NavigationEnumerationContracts.Enumerate(f.Query,P(1,4,1),f.Extent,f.Filter,refs,count,3,f.B,i);});Check(!outcome.ReturnedNormally,"Unknown filter payload is not a silent skip");Equal(Ref(0,0),refs.Cells[0].Read(1),"First emitted ref retained");Unknown(refs.Cells[1]);Equal(99u,count.Read(1),"External count commits only on normal return");
   var result=Field<uint>.Of(88);var output=P(7,8,9);Check(!MoveContracts.Run(delegate(Invocation i){NavigationNearestContracts.Nearest(f.Query,P(1,4,1),f.Extent,f.Filter,result,output,f.B,i);}).ReturnedNormally,"Nearest propagates enumeration exception");Equal(0u,result.Read(1),"Nearest ref cleared before enumeration");Point(output,7,8,9,"No closest call before enumeration completes");
  });
  Test("production BV negative escape skips an unread child and reaches next leaf",delegate{
   var f=new F(0);Bv(f.Layers[0],new NavigationBvNode16[]{Node(6,10,-2),new NavigationBvNode16(),Node(0,10,0)});var refs=new NavigationRefBuffer(2);var count=new Field<uint>();
   Equal(NavigationCoreContracts.Success,f.Enumerate(P(1,0,1),refs,count,2),"BV escape normal result");Equal(1u,count.Read(1),"One leaf after disjoint subtree");Equal(Ref(0,0),refs.Cells[0].Read(1),"Generated leaf ref");
   var result=new Field<uint>();var output=new NavigationPoint3();Equal(NavigationCoreContracts.Success,f.Nearest(P(1,2,1),result,output),"Default BV through closest and selector");Equal(Ref(0,0),result.Read(1),"BV generated selected ref");Point(output,1,0,1,"Real BV leaf detail point");
   Bv(f.Layers[0],new NavigationBvNode16[]{Node(6,10,-2),new NavigationBvNode16()});Equal(NavigationCoreContracts.Success,f.Enumerate(P(1,0,1),refs,count,2),"Escape to exactly tree end");Equal(0u,count.Read(1),"No leaf visited");
  });
  Test("production BV leaf flags and capacity preserve leaf order",delegate{
   var f=new F(0);MorePoly(f,3);var polys=f.Layers[0].Tile.Polys10.Read(1);f.Spatial.Poly(polys[0],1).Flags1C.Write(2);
   Bv(f.Layers[0],new NavigationBvNode16[]{Node(0,10,-4),Node(0,10,0),Node(0,10,2),Node(0,10,1)});
   var refs=new NavigationRefBuffer(2);var count=new Field<uint>();Equal(0x40000010u,f.Enumerate(P(1,0,1),refs,count,1),"BV capacity status");Equal(1u,count.Read(1),"Only one output");Equal(Ref(0,2),refs.Cells[0].Read(1),"BV order differs from polygon index order");Unknown(refs.Cells[1]);
  });
  Test("own bucket cycle and unread BV payload stop explicitly",delegate{
   var f=new F(0);f.Layers[0].TileView.Next60.Write(f.Layers[0].Tile);var result=Field<uint>.Of(88);var output=P(7,8,9);
   Check(!MoveContracts.Run(delegate(Invocation i){NavigationNearestContracts.Nearest(f.Query,P(1,0,1),f.Extent,f.Filter,result,output,f.B,i);}).ReturnedNormally,"Cycle outside finite own traversal domain");Equal(0u,result.Read(1),"Initial clear retained");Point(output,7,8,9,"No synthetic result for cyclic bucket");
   f.Layers[0].TileView.Next60.Write(null);Bv(f.Layers[0],new NavigationBvNode16[]{Node(0,10,0)});f.Layers[0].TileView.BVTree40.Read(1)[0].MaxZ0A.Forget();
   Check(!MoveContracts.Run(delegate(Invocation i){NavigationNearestContracts.Nearest(f.Query,P(1,0,1),f.Extent,f.Filter,result,output,f.B,i);}).ReturnedNormally,"Unknown BV payload not treated as empty");Point(output,7,8,9,"Unresolved BV keeps output");
  });
  Test("production ordinary closest resolves detail height rather than retaining input Y",delegate{
   var f=new F(0);f.Layers[0].Tile.Vertices18.Write(new NavigationPoint3[]{P(0,0,0),P(4,4,0),P(0,8,4)});var point=P(1,99,1);var output=new NavigationPoint3();var over=new Field<byte>();
   Equal(NavigationCoreContracts.Success,f.B.Closest260520(f.Query,Ref(0,0),point,output,over),"Default closest");Point(output,1,3,1,"Barycentric detail height");Equal((byte)1,over.Read(1),"Inside polygon");Point(point,1,99,1,"Separate input unchanged");
   var clamp=new NavigationPoint3();Equal(NavigationCoreContracts.Success,f.B.Clamp260AD0(f.Query,Ref(0,0),point,clamp),"Existing clamp leaf");Point(clamp,1,99,1,"Clamp contract differs from closest detail");
  });
  Test("production detail extra vertices stop after first accepted triangle",delegate{
   var f=new F(0);var l=f.Layers[0];l.TileView.DetailVertices30.Write(new NavigationPoint3[]{P(0,10,0),P(4,14,0),P(0,18,4)});l.Triangle.A00.Write(3);l.Triangle.B01.Write(4);l.Triangle.C02.Write(5);l.Detail.TriangleCount09.Write(2);l.TileView.TriangleRecords38.Write(new NavigationTriangle4[]{l.Triangle,new NavigationTriangle4()});
   var output=new NavigationPoint3();var over=new Field<byte>();Equal(NavigationCoreContracts.Success,f.B.Closest260520(f.Query,Ref(0,0),P(1,99,1),output,over),"First detail success skips unknown later triangle");Point(output,1,13,1,"Extra-vertex barycentric height");Equal((byte)1,over.Read(1),"Over polygon");
  });
  Test("production detail LEA defers vertex access to actual Height load order",delegate{
   var f=new F(0);var l=f.Layers[0];l.Triangle.A00.Write(3);l.Triangle.B01.Write(4);l.Triangle.C02.Write(5);
   var a=P(0,0,0);var b=P(4,4,0);var c=P(0,8,4);a.X.Forget();b.MarkReleased();l.TileView.DetailVertices30.Write(new NavigationPoint3[]{a,b,c});var output=new NavigationPoint3();var over=new Field<byte>();
   Missing(MoveContracts.Run(delegate(Invocation i){f.B.Closest260520(f.Query,Ref(0,0),P(1,99,1),output,over);}),0x26d584);Point(output,1,99,1,"Initial point prefix before first height load");Equal((byte)1,over.Read(1),"Inside flag precedes height");
   a.X.Write(0);c.X.Forget();Missing(MoveContracts.Run(delegate(Invocation i){f.B.Closest260520(f.Query,Ref(0,0),P(1,99,1),output,over);}),0x26d58d);Point(output,1,99,1,"C.X consumed before released B");
   c.X.Write(0);Missing(MoveContracts.Run(delegate(Invocation i){f.B.Closest260520(f.Query,Ref(0,0),P(1,99,1),output,over);}),0x26d59c);Point(output,1,99,1,"Released B first consumed by B.Z load");
  });
  Test("Mock non-consuming Height reaches deferred unknown null and missing vertices",delegate{
   var f=new F(0);var l=f.Layers[0];l.Triangle.A00.Write(3);l.Triangle.B01.Write(4);l.Triangle.C02.Write(5);var b=new MockNonConsumingHeight(f.Spatial);var output=new NavigationPoint3();var over=new Field<byte>();
   for(int mode=0;mode<4;mode++){
    if(mode==0)l.TileView.DetailVertices30.Forget();else if(mode==1)l.TileView.DetailVertices30.Write(null);else if(mode==2)l.TileView.DetailVertices30.Write(new NavigationPoint3[0]);else{var released=P(0,0,0);released.MarkReleased();l.TileView.DetailVertices30.Write(new NavigationPoint3[]{released,released,released});}
    Equal(NavigationCoreContracts.Success,b.Closest260520(f.Query,Ref(0,0),P(1,99,1),output,over),"LEA alone does not consume declared address payload");Equal(mode+1,b.Calls,"Mock height was reached once");Point(output,1,99,1,"Mock rejected height retains initial point");Equal((byte)1,over.Read(1),"Real polygon geometry before mock boundary");
   }
  });
  Test("production outside closest tests original point against detail triangle",delegate{
   var f=new F(0);var l=f.Layers[0];l.Tile.Vertices18.Write(new NavigationPoint3[]{P(0,0,0),P(4,4,0),P(0,8,4)});l.TileView.DetailVertices30.Write(new NavigationPoint3[]{P(0,10,0),P(4,14,0),P(0,18,4)});l.Triangle.A00.Write(3);l.Triangle.B01.Write(4);l.Triangle.C02.Write(5);
   var output=new NavigationPoint3();var over=new Field<byte>();Equal(NavigationCoreContracts.Success,f.B.Closest260520(f.Query,Ref(0,0),P(3,99,3),output,over),"Outside closest");Point(output,2,6,2,"Edge interpolation retained when original point fails detail containment");Equal((byte)0,over.Read(1),"Outside polygon flag");
  });
  Test("production closest failure gates and unknown detail retain precise prefix",delegate{
   var f=new F(0);var output=P(7,8,9);var over=Field<byte>.Of(77);f.Query.Mesh0.Forget();Equal(NavigationCoreContracts.Failure,f.B.Closest260520(f.Query,0,P(1,99,1),output,over),"Zero ref does not consume unknown mesh");Point(output,7,8,9,"Rejected ref leaves point");Equal((byte)77,over.Read(1),"Rejected ref leaves over flag");Unknown(f.Query.Mesh0);
   f.Query.Mesh0.Write(f.Mesh);f.Layers[0].TileView.Details28.Forget();Missing(MoveContracts.Run(delegate(Invocation i){f.B.Closest260520(f.Query,Ref(0,0),P(1,99,1),output,over);}),0x2609e4);Point(output,1,99,1,"Initial point copied before details consumed");Equal((byte)1,over.Read(1),"Geometry over flag precedes details");
   var g=new F(0);g.Layers[0].Poly.VertexCount1E.Write(4);g.Layers[0].Poly.Indices04[0].Forget();g.Layers[0].Tile.Vertices18.Forget();Set(output,7,8,9);over.Write(77);
   Missing(MoveContracts.Run(delegate(Invocation i){g.B.Closest260520(g.Query,Ref(0,0),P(1,99,1),output,over);}),0x260780);Point(output,7,8,9,"Unknown index consumed before captured unknown vertex pointer");Equal((byte)77,over.Read(1),"Geometry not reached");
   g.Layers[0].Poly.Indices04[0].Write(0);Missing(MoveContracts.Run(delegate(Invocation i){g.B.Closest260520(g.Query,Ref(0,0),P(1,99,1),output,over);}),0x260791);Point(output,7,8,9,"Vertex pointer first consumed by actual X load");
  });
  Test("production height finite tolerances NaN and degenerate triangles",delegate{
   var f=new F();var a=P(0,0,0);var b=P(4,4,0);var c=P(0,8,4);var h=Field<float>.Of(77);
   var p=P(1,99,1);p.Y.Forget();Equal((byte)1,f.B.Height26D580(p,a,b,c,h),"Input Y unused");Equal(3f,h.Read(1),"Real barycentric height");
   Equal((byte)1,f.B.Height26D580(P(-0.0002f,0,1),a,b,c,h),"Small negative barycentric coefficient accepted by observed tolerance");
   h.Write(77);Equal((byte)0,f.B.Height26D580(P(-0.0008f,0,1),a,b,c,h),"Beyond lower tolerance rejected");Equal(77f,h.Read(1),"Rejected height untouched");
   Equal((byte)0,f.B.Height26D580(P(float.NaN,0,1),a,b,c,h),"Unordered lower comparison rejected by JC");Equal(77f,h.Read(1),"NaN rejection retains height");
   Equal((byte)0,f.B.Height26D580(P(1,0,1),a,a,a,h),"Degenerate denominator produces rejected NaN coefficients");Equal(77f,h.Read(1),"Degenerate output untouched");
   Equal((byte)1,f.B.Height26D580(P(1,0,1),P(0,16777216,0),P(4,16777220,0),P(0,16777220,4),h),"Large finite heights");Equal(0x4b800000u,NavigationCoreContracts.ToBits(h.Read(1)),"Two successive binary32 additions each lose the unit increment");h.Write(77);
   c.Y.Forget();Missing(MoveContracts.Run(delegate(Invocation i){f.B.Height26D580(P(100,0,100),a,b,c,h);}),0x26d5df);Equal(77f,h.Read(1),"Triangle Y is read before containment rejection");
  });
  Test("production closest no-detail leaves original interior Y explicitly",delegate{
   var f=new F(0);f.Layers[0].Detail.TriangleCount09.Write(0);f.Layers[0].Detail.TriangleBase04.Forget();f.Layers[0].TileView.TriangleRecords38.Forget();var output=new NavigationPoint3();var over=new Field<byte>();
   Equal(NavigationCoreContracts.Success,f.B.Closest260520(f.Query,Ref(0,0),P(1,99,1),output,over),"Explicit zero count short circuit");Point(output,1,99,1,"No fabricated surface height");Equal((byte)1,over.Read(1),"Interior geometry is still known");
  });
  Test("production type40 uses endpoint distance ratio and preserves zero-distance NaN",delegate{
   var f=new F(0);var l=f.Layers[0];l.PolyView.Type1F.Write(0x40);l.Poly.VertexCount1E.Forget();l.TileView.Details28.Forget();l.Tile.Vertices18.Write(new NavigationPoint3[]{P(0,0,0),P(4,0,0)});var output=new NavigationPoint3();var over=Field<byte>.Of(77);
   Equal(NavigationCoreContracts.Success,f.B.Closest260520(f.Query,Ref(0,0),P(0,3,0),output,over),"Special two-endpoint branch does not consume polygon count or details");Point(output,1.5f,0,0,"Distance ratio 3 / (3 + 5), not orthogonal projection");Equal((byte)0,over.Read(1),"Special branch over flag");
   // Explicit own partial extent, not a claim that a negative-base Vec3 is a
   // valid native object. Y is visible; X lies outside the declared window.
   var window=new NavigationPointBuffer(new Field<float>[] {Field<float>.Of(77),Field<float>.Of(88)});over.Write(66);
   Missing(MoveContracts.Run(delegate(Invocation i){f.B.Closest260520(f.Query,Ref(0,0),P(0,3,0),window.PointAtScalar(-1,1),over);}),0x26070e);Equal(0f,window.Cells[0].Read(1),"Y store survives before unavailable X");Equal(88f,window.Cells[1].Read(1),"Z store not reached");Equal((byte)66,over.Read(1),"Flag follows all three stores");
   l.Tile.Vertices18.Write(new NavigationPoint3[]{P(2,4,6),P(2,4,6)});Equal(NavigationCoreContracts.Success,f.B.Closest260520(f.Query,Ref(0,0),P(2,4,6),output,over),"Both distances zero has no fallback");Check(float.IsNaN(output.X.Read(1)),"NaN X retained");Check(float.IsNaN(output.Y.Read(1)),"NaN Y retained");Check(float.IsNaN(output.Z.Read(1)),"NaN Z retained");Equal((byte)0,over.Read(1),"Normal flag despite NaN output");
  });
  Test("production nearest honors height threshold including equality and unordered climb",delegate{
   var f=new F(0,4.5f);var result=new Field<uint>();var output=new NavigationPoint3();
   f.Layers[0].HeaderView.Climb44.Write(3);f.Nearest(P(1,4,1),result,output);Equal(Ref(1,0),result.Read(1),"Metric one loses to quarter");Point(output,1,4.5f,1,"Closer upper layer");
   foreach(float climb in new float[]{4,5,float.NaN}){f.Layers[0].HeaderView.Climb44.Write(climb);f.Nearest(P(1,4,1),result,output);Equal(Ref(0,0),result.Read(1),"Threshold equality larger or unordered produces zero metric");Point(output,1,0,1,"First layer wins zero metric");}
   f.Layers[0].HeaderView.Climb44.Write(3);f.Nearest(P(1,-4,1),result,output);Equal(Ref(0,0),result.Read(1),"Absolute vertical difference for below-surface input");
  });
  Test("production nearest output alias changes later input while null output does not",delegate{
   var f=new F(0,8);var result=new Field<uint>();var input=P(1,5,1);var separate=new NavigationPoint3();f.Nearest(input,result,separate);Equal(Ref(1,0),result.Read(1),"Separate output sees original input in second candidate");Point(input,1,5,1,"Input untouched");
   f.Nearest(input,result,input);Equal(Ref(0,0),result.Read(1),"Same pointer writes feed later input reads");Point(input,1,0,1,"First winner remains under alias");
   f.Nearest(P(1,5,1),result,null);Equal(Ref(1,0),result.Read(1),"Optional output null does not skip selection");
  });
  Test("production nearest to same-poly corridor and straight endpoints",delegate{
   var f=new F(0);f.B.UseObservedEqualityConstant();var from=new NavigationPoint3();var to=new NavigationPoint3();var first=new Field<uint>();var last=new Field<uint>();f.Nearest(P(1,3,1),first,from);f.Nearest(P(2,4,1),last,to);Equal(Ref(0,0),first.Read(1),"Start selected by real enumeration and detail");Equal(first.Read(1),last.Read(1),"End on same polygon");
   var corridor=new NavigationRefBuffer(1);var count=new Field<uint>();Equal(NavigationCoreContracts.Success,NavigationCoreContracts.FindPath(f.Query,first.Read(1),last.Read(1),from,to,f.Filter,corridor,count,1,f.B,new Invocation()),"Recovered same-poly corridor");Equal(1u,count.Read(1),"One polygon corridor");
   var points=new NavigationPointBuffer(2);var flags=new NavigationFlagBuffer(2);Equal(0x40000010u,NavigationCoreContracts.StraightPath(f.Query,from,to,corridor,1,points,flags,count,2,f.B,new Invocation()),"Recovered straight endpoints fill capacity");Equal(2u,count.Read(1),"Two distinct endpoints");Point(points.PointAtScalar(0,1),1,0,1,"Selected start");Point(points.PointAtScalar(3,1),2,0,1,"Selected end");Equal((byte)1,flags.Cells[0].Read(1),"Start flag");Equal((byte)2,flags.Cells[1].Read(1),"End flag");
  });
  Test("Mock selector negative status and signed negative count are distinct",delegate{
   var f=new F();var result=Field<uint>.Of(77);var output=P(7,8,9);var b=new MockSelector(f.Spatial);b.EnumerationStatus=0x80001234;b.MaxFinite34BDCC.Forget();Equal(NavigationCoreContracts.Failure,Select(f,b,P(1,2,3),result,output),"Failure status canonicalized");Equal(0u,result.Read(1),"Ref cleared before failed producer");Equal(0,b.ClosestCalls,"No closest after failed enumeration");Point(output,7,8,9,"Output retained");
   b.EnumerationStatus=NavigationCoreContracts.Success;b.CountOverride=0xffffffff;Missing(MoveContracts.Run(delegate(Invocation i){NavigationNearestContracts.Nearest(f.Query,P(1,2,3),f.Extent,f.Filter,result,output,b,i);}),0x261165);
   b.UseObservedNearestConstants();Equal(NavigationCoreContracts.Success,Select(f,b,P(1,2,3),result,output),"Negative signed count skips candidates after max load");Equal(0u,result.Read(1),"No winner");Equal(0,b.ClosestCalls,"No fabricated huge unsigned loop");
  });
  Test("Mock selector ignores closest status and reuses scratch with reset over flag",delegate{
   var f=new F(0);var b=new MockSelector(f.Spatial,Ref(0,0),Ref(0,1));b.ClosestStatus=NavigationCoreContracts.Failure;
   b.Observe=delegate(int n,uint r,NavigationPoint3 p,NavigationPoint3 o,Field<byte> over){Equal((byte)0,over.Read(1),"Flag reset for every candidate");if(n==0){Set(o,3,0,4);over.Write(1);}else o.X.Write(1);};
   var result=new Field<uint>();var output=new NavigationPoint3();Equal(NavigationCoreContracts.Success,Select(f,b,P(0,10,0),result,output),"Closest failure code not checked");Equal(2,b.ClosestCalls,"Both callbacks");Same(b.Scratch[0],b.Scratch[1],"Same hidden point reused");Point(b.Scratch[1],1,0,4,"Unwritten tail retained from earlier callback");Equal(Ref(0,0),result.Read(1),"First over metric 100 beats second full metric 117");Point(output,3,0,4,"Winning first point");
  });
  Test("Mock selector unknown closest payload is not replaced by a default",delegate{
   var f=new F();var b=new MockSelector(f.Spatial,1);b.ClosestStatus=NavigationCoreContracts.Failure;var result=Field<uint>.Of(77);var output=P(7,8,9);
   Missing(MoveContracts.Run(delegate(Invocation i){NavigationNearestContracts.Nearest(f.Query,P(1,2,3),f.Extent,f.Filter,result,output,b,i);}),0x261206);Equal(1,b.ClosestCalls,"Producer returned but did not establish X");Equal(0u,result.Read(1),"Initial ref clear survives");Point(output,7,8,9,"No winner writes");
  });
  Test("Mock selector NaN replaces finite and later finite replaces NaN",delegate{
   var f=new F();var b=new MockSelector(f.Spatial,1,2,3);b.Observe=delegate(int n,uint r,NavigationPoint3 p,NavigationPoint3 o,Field<byte> over){Set(o,n==0?1:n==1?float.NaN:2,0,0);};var result=new Field<uint>();var output=new NavigationPoint3();
   Equal(NavigationCoreContracts.Success,Select(f,b,P(0,0,0),result,output),"NaN compare path");Equal(3u,result.Read(1),"Third replaces unordered best despite larger finite distance than first");Point(output,2,0,0,"Last winner");
   var second=new MockSelector(f.Spatial,1,2);second.Observe=b.Observe;Select(f,second,P(0,0,0),result,output);Equal(2u,result.Read(1),"Unordered candidate replaces finite best");Check(float.IsNaN(output.X.Read(1)),"NaN payload preserved");
  });
  Test("Mock selector sums squared distance in successive binary32 operations",delegate{
   var f=new F();var b=new MockSelector(f.Spatial,1,2);b.Observe=delegate(int n,uint r,NavigationPoint3 p,NavigationPoint3 o,Field<byte> over){Set(o,4096,n==0?1:0,n==0?1:0);};var result=new Field<uint>();var output=new NavigationPoint3();
   Select(f,b,P(0,0,0),result,output);Equal(1u,result.Read(1),"Each unit addition rounds away so second candidate is equal, not smaller");Point(output,4096,1,1,"Stable first winner under float32 arithmetic");
  });
  Test("Mock callback sees prior point writes but external ref commits only at end",delegate{
   var f=new F();var result=Field<uint>.Of(77);var output=P(7,8,9);var b=new MockSelector(f.Spatial,1,2);
   b.Observe=delegate(int n,uint r,NavigationPoint3 p,NavigationPoint3 o,Field<byte> over){Equal(0u,result.Read(1),"Ref still initial zero during callbacks");if(n==0)Set(o,1,2,3);else{Point(output,1,2,3,"Earlier winning point visible");throw new MissingContract(0x123456,"Injected callback stop; not native navigation");}};
   Missing(MoveContracts.Run(delegate(Invocation i){NavigationNearestContracts.Nearest(f.Query,P(0,0,0),f.Extent,f.Filter,result,output,b,i);}),0x123456);Point(output,1,2,3,"Winner prefix is not rolled back");Equal(0u,result.Read(1),"Final ref store never reached");
  });
 }
}
public static class NavigationNearestTestProgram {
 public static int Main(){try{NavigationNearestTests.Run();Console.WriteLine("PASS "+NavigationNearestTests.Scenarios+" scenarios, "+NavigationNearestTests.Assertions+" assertions; default production mesh/closest/selector paths and explicitly labelled Mock callback observations; not complete navigation or gameplay");return 0;}catch(Exception e){Console.Error.WriteLine("FAIL "+e);return 1;}}
}
