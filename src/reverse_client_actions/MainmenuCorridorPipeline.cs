using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Actual saved mainmenu layers, recovered tile production/publication and
// production corridor search. Graph traversal below selects a demanding pair
// of endpoints only: it never supplies a route to the recovered search.
public static class MainmenuCorridorPipeline {
 static void Require(bool ok,string why){if(!ok)throw new InvalidOperationException(why);}
 static NavigationPoint3 Point(float x,float y,float z){var p=new NavigationPoint3();p.X.Write(x);p.Y.Write(y);p.Z.Write(z);return p;}
 static NavigationTile Tile(NavigationMeshTilesState s,uint r){return s.Mesh.Tiles50.Read(0)[(r>>10)&4095];}
 static NavigationPoly Poly(NavigationMeshTilesState s,uint r){return Tile(s,r).Polys10.Read(0)[r&1023];}
 static IEnumerable<uint> Neighbors(NavigationMeshTilesState s,uint r){
  var tile=Tile(s,r);uint index=s.Poly(Poly(s,r),0).FirstLink00.Read(0);var seen=new HashSet<uint>();
  while(index!=UInt32.MaxValue){
   Require(seen.Add(index),"Actual link chain cycle");var links=s.Tile(tile,0).Links20.Read(0);Require(index<links.Length,"Actual link extent");var link=links[index];
   uint target=link.Reference00.Read(0);Require(NavigationCoreContracts.ValidateReference(s.Mesh,target)==1,"Actual link target invalid");
   yield return target;index=link.Next04.Read(0);
  }
 }
 static NavigationPoint3 Center(NavigationMeshTilesState s,uint r){
  var poly=Poly(s,r);var vertices=Tile(s,r).Vertices18.Read(0);int n=poly.VertexCount1E.Read(0);Require(n>=3&&n<=6,"Actual polygon vertex extent");float x=0,y=0,z=0;
  for(int i=0;i<n;i++){var v=vertices[poly.Indices04[i].Read(0)];x+=v.X.Read(0);y+=v.Y.Read(0);z+=v.Z.Read(0);}return Point(x/n,y/n,z/n);
 }
 static bool HasEdge(NavigationMeshTilesState s,uint a,uint b){foreach(uint r in Neighbors(s,a))if(r==b)return true;return false;}
 static void Search(NavigationMeshTilesState mesh,NavigationQuery query,NavigationQueryFilter filter,NavigationCorridorBoundaries b,uint chosenFrom,uint chosenTo,string label){
  var source=Center(mesh,chosenFrom);var destination=Center(mesh,chosenTo);var from=new NavigationPoint3();var to=new NavigationPoint3();
  var start=Field<uint>.Of(0);var end=Field<uint>.Of(0);var extents=Point(.125f,2f,.125f);
  uint a=NavigationNearestContracts.Nearest(query,source,extents,filter,start,from,b,new Invocation());
  uint z=NavigationNearestContracts.Nearest(query,destination,extents,filter,end,to,b,new Invocation());
  Require(a==0x40000000&&z==0x40000000&&start.Read(0)!=0&&end.Read(0)!=0,"Real endpoint nearest query failed");
  Require((start.Read(0)>>10)!=(end.Read(0)>>10),"Probe must cross actual tiles");
  var path=new NavigationRefBuffer(4096);var count=Field<uint>.Of(99);
  uint status=NavigationCorridorContracts.FindPath(query,start.Read(0),end.Read(0),from,to,filter,path,count,4096,b,new Invocation());
  Require(status==0x40000000,"Corridor not complete: status="+status.ToString("X8"));uint length=count.Read(0);Require(length>2&&length<4096,"Nontrivial complete corridor expected");
  Require(path.At(0,0).Read(0)==start.Read(0)&&path.At(length-1,0).Read(0)==end.Read(0),"Returned endpoints differ from nearest-produced refs");
  int crossings=0;var refs=new List<string>();
  for(uint i=0;i<length;i++){
   uint r=path.At(i,0).Read(0);Require(NavigationCoreContracts.ValidateReference(mesh.Mesh,r)==1,"Search returned invalid actual ref");refs.Add(r.ToString("X8"));
   if(i!=0){uint previous=path.At(i-1,0).Read(0);Require(HasEdge(mesh,previous,r),"Search emitted a nonadjacent pair");if((previous>>10)!=(r>>10))crossings++;}
  }
  Require(crossings>0,"Actual corridor did not cross a tile boundary");
  Console.WriteLine("CORRIDOR "+label+" status="+status.ToString("X8")+" count="+length+" tileCrossings="+crossings+" refs="+String.Join(",",refs.ToArray()));
 }
 public static int Main(string[] args){try{
  Require(args.Length==1&&args[0]=="--explicit-default-domain","Explicit own domain required");
  var produced=MainmenuTilePipeline.BuildExplicitColdTiles();var spatial=new NavigationSpatialState();var mesh=NavigationMeshTilesState.Create(0,0,0,32,32,4096,1024,spatial);mesh.UseObservedLinkConstants();
  var actualRefs=new List<uint>();
  foreach(var output in produced){
   byte[] bytes=output.Bytes;int x=BitConverter.ToInt32(bytes,8),z=BitConverter.ToInt32(bytes,12);
   Require(BitConverter.ToSingle(bytes,72)==x*32f&&BitConverter.ToSingle(bytes,80)==z*32f,"Explicit mesh grid/bounds mismatch");
   var owner=new NavigationMeshTileBytes(bytes,output.Length);var r=Field<uint>.Of(0);var added=NavigationMeshTiles.Add(mesh,owner,r);
   Require(added.Outcome==NavigationMeshTileOutcome.NativeSuccess&&added.NativeStatus==0x40000000,"Actual mesh insertion: "+added.Detail);
   Require(Object.ReferenceEquals(mesh.Tile(Tile(mesh,r.Read(0)),0).Data50.Read(0),owner),"Actual source owner copied");
   int n=Tile(mesh,r.Read(0)).Polys10.Read(0).Length;for(int p=0;p<n;p++)actualRefs.Add(r.Read(0)|(uint)p);
  }
  // Choose the largest reachable component, then a far endpoint by link hops.
  // The queue and distance table are discarded before invoking FindPath.
  uint sourceRef=0,targetRef=0;int bestSize=0,bestDistance=0;
  var classified=new HashSet<uint>();
  foreach(uint seed in actualRefs){
   if(classified.Contains(seed))continue;var distance=new Dictionary<uint,int>();var queue=new Queue<uint>();distance.Add(seed,0);queue.Enqueue(seed);uint far=seed;int hops=0;
   while(queue.Count!=0){uint r=queue.Dequeue();classified.Add(r);foreach(uint next in Neighbors(mesh,r)){if(distance.ContainsKey(next))continue;int d=distance[r]+1;distance.Add(next,d);queue.Enqueue(next);if(d>hops&&(next>>10)!=(seed>>10)){hops=d;far=next;}}}
   if(hops>0&&distance.Count>bestSize){bestSize=distance.Count;bestDistance=hops;sourceRef=seed;targetRef=far;}
  }
  Require(sourceRef!=0&&targetRef!=0&&bestDistance>1,"Real resource lacks a nontrivial cross-tile probe domain");
  Console.WriteLine("PROBE actualPolygons="+actualRefs.Count+" reachableComponent="+bestSize+" endpointSeparationHops="+bestDistance+"; selection traversal supplies no route to search");
  var query=new NavigationQuery();query.Mesh0.Write(mesh.Mesh);var filter=new NavigationQueryFilter();filter.Include100.Write(1);filter.Exclude102.Write(0);
  var search=new NavigationCorridorState(query);search.Pool58.Write(NavigationSearchStorage.CreatePool26DC10(4096,1024));search.Queue60.Write(NavigationSearchStorage.CreateQueue(4096));
  var costs=new NavigationCorridorFilterCosts(filter);for(int i=0;i<64;i++)costs.AreaCosts00[i].Write(1f);search.Attach(costs);
  var b=new NavigationCorridorBoundaries(mesh,search);b.UseObservedCorridorConstants();b.UseObservedNearestConstants();
  Search(mesh,query,filter,b,sourceRef,targetRef,"forward");Search(mesh,query,filter,b,targetRef,sourceRef,"reverse-reused-query");
  Require(Object.ReferenceEquals(query.Mesh0.Read(0),mesh.Mesh),"Query mesh identity changed");
  Console.WriteLine("PASS actual nearest-to-cross-polygon corridor twice through shared mutable query and real mesh; explicit pool4096/hash1024/cost1, no native golden, funnel, physical mouse, movement or game acceptance");return 0;
 }catch(Exception e){Console.Error.WriteLine("FAIL "+e);return 1;}}
}
