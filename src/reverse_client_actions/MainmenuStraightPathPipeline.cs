using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Actual saved mainmenu layers, recovered tile production/publication and
// production corridor/straight-path search. Graph traversal selects a pair
// of endpoints only: it never supplies a route to the recovered search.
public static class MainmenuStraightPathPipeline {
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
  VerifyStraightPath(mesh,query,from,to,path,length,label);
  Console.WriteLine("CORRIDOR "+label+" status="+status.ToString("X8")+" count="+length+" tileCrossings="+crossings+" refs="+String.Join(",",refs.ToArray()));
 }
 // Independent probe consistency check, not the reconstructed collision code.
 // Every sampled XZ position must lie in a convex polygon of the returned
 // corridor. This is not a proof of swept-body collision or native gameplay.
 static bool InPolygonXZ(NavigationMeshTilesState mesh,uint reference,double x,double z){
  var p=Poly(mesh,reference);var vertices=Tile(mesh,reference).Vertices18.Read(0);int count=p.VertexCount1E.Read(0);bool positive=false,negative=false;
  for(int i=0;i<count;i++){
   var a=vertices[p.Indices04[i].Read(0)];var b=vertices[p.Indices04[(i+1)%count].Read(0)];
   double dx=(double)b.X.Read(0)-a.X.Read(0),dz=(double)b.Z.Read(0)-a.Z.Read(0);double cross=dx*(z-a.Z.Read(0))-dz*(x-a.X.Read(0));double tolerance=.002*Math.Sqrt(dx*dx+dz*dz);
   if(cross>tolerance)positive=true;if(cross< -tolerance)negative=true;if(positive&&negative)return false;
  }return true;
 }
 static void VerifyStraightPath(NavigationMeshTilesState mesh,NavigationQuery query,NavigationPoint3 from,NavigationPoint3 to,NavigationRefBuffer corridor,uint length,string label){
  var b=new NavigationStraightPathBoundaries(mesh);b.UseObservedStraightPathConstants();b.UseObservedEqualityConstant();
  var points=new NavigationPointBuffer(256);var flags=new NavigationFlagBuffer(256);var count=Field<uint>.Of(99);
  uint status=NavigationStraightPathContracts.StraightPath(query,from,to,corridor,(int)length,points,flags,count,256,b,new Invocation());
  Require(status==0x40000000,"Actual waypoint result is not complete: "+status.ToString("X8"));uint n=count.Read(0);Require(n>=2&&n<256,"Nontrivial bounded waypoint output required");
  Require(flags.At(0,0).Read(0)==1&&flags.At(n-1,0).Read(0)==2,"Start/end waypoint flags missing");
  var output=new List<string>();int samples=0;
  for(uint i=0;i<n;i++){
   float x=points.At(i*3,0).Read(0),y=points.At(i*3+1,0).Read(0),z=points.At(i*3+2,0).Read(0);
   Require(!Single.IsNaN(x)&&!Single.IsInfinity(x)&&!Single.IsNaN(y)&&!Single.IsInfinity(y)&&!Single.IsNaN(z)&&!Single.IsInfinity(z),"Actual waypoint is non-finite");
   output.Add(x+","+y+","+z+":"+flags.At(i,0).Read(0));
   if(i==0)Require(Math.Abs(x-from.X.Read(0))<.002&&Math.Abs(z-from.Z.Read(0))<.002,"Clamped actual start differs in XZ");
   if(i==n-1)Require(Math.Abs(x-to.X.Read(0))<.002&&Math.Abs(z-to.Z.Read(0))<.002,"Clamped actual end differs in XZ");
   if(i==0)continue;
   double ax=points.At((i-1)*3,0).Read(0),az=points.At((i-1)*3+2,0).Read(0),dx=x-ax,dz=z-az;int steps=(int)Math.Ceiling(Math.Sqrt(dx*dx+dz*dz)/.125);Require(steps<10000,"Own probe sample budget");steps=Math.Max(steps,1);
   for(int step=0;step<=steps;step++){
    double t=(double)step/steps,sx=ax+dx*t,sz=az+dz*t;bool inside=false;
    for(uint p=0;p<length;p++)if(InPolygonXZ(mesh,corridor.At(p,0).Read(0),sx,sz)){inside=true;break;}
    Require(inside,"Waypoint segment leaves the actual corridor XZ union at "+sx+","+sz);samples++;
   }
  }
  Console.WriteLine("WAYPOINTS "+label+" status="+status.ToString("X8")+" count="+n+" ownXZConsistencySamples="+samples+" points="+String.Join(";",output.ToArray()));
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
  Console.WriteLine("PASS actual nearest-to-corridor-to-waypoints twice through shared real mesh; explicit pool4096/hash1024/cost1 and own equality warm state, no native golden, physical mouse, actor movement or game acceptance");return 0;
 }catch(Exception e){Console.Error.WriteLine("FAIL "+e);return 1;}}
}
