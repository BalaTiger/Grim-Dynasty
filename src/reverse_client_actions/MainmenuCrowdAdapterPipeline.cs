using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Saved mainmenu resource -> existing cold tile producer -> real mesh links ->
// recovered crowd-adapter slots. Endpoint selection reads actual geometry; it
// never supplies a nearest result or a corridor. Explicit owned cold parameters
// and managed synchronization are not native runtime defaults or a native gold.
public static class MainmenuCrowdAdapterPipeline {
 static void Require(bool value,string why){if(!value)throw new InvalidOperationException(why);}
 static NavigationPoint3 Point(float x,float y,float z){var p=new NavigationPoint3();p.X.Write(x);p.Y.Write(y);p.Z.Write(z);return p;}
 static NavigationTile Tile(NavigationMeshTilesState state,uint r){return state.Mesh.Tiles50.Read(0)[(int)((r>>10)&4095)];}
 static NavigationPoly Poly(NavigationMeshTilesState state,uint r){return Tile(state,r).Polys10.Read(0)[(int)(r&1023)];}
 static NavigationPoint3 Center(NavigationMeshTilesState state,uint r){
  var poly=Poly(state,r);var vertices=Tile(state,r).Vertices18.Read(0);int n=poly.VertexCount1E.Read(0);Require(n>=3&&n<=6,"Actual polygon vertex extent");float x=0,y=0,z=0;
  for(int j=0;j<n;j++){var v=vertices[poly.Indices04[j].Read(0)];x+=v.X.Read(0);y+=v.Y.Read(0);z+=v.Z.Read(0);}return Point(x/n,y/n,z/n);
 }
 static IEnumerable<uint> Neighbors(NavigationMeshTilesState state,uint r){
  var tile=Tile(state,r);var links=state.Tile(tile,0).Links20.Read(0);uint link=state.Poly(Poly(state,r),0).FirstLink00.Read(0);var seen=new HashSet<uint>();
  while(link!=UInt32.MaxValue){Require(seen.Add(link)&&link<links.Length,"Actual link chain cycle or missing extent");var entry=links[(int)link];uint target=entry.Reference00.Read(0);Require(NavigationCoreContracts.ValidateReference(state.Mesh,target)==1,"Invalid actual link target");yield return target;link=entry.Next04.Read(0);}
 }
 static bool Linked(NavigationMeshTilesState state,uint from,uint to){foreach(uint r in Neighbors(state,from))if(r==to)return true;return false;}
 public static int Main(string[] args){try{
  Require(args.Length==1&&args[0]=="--explicit-default-domain","Explicit own cold domain required");
  var produced=MainmenuTilePipeline.BuildExplicitColdTiles();var mesh=NavigationMeshTilesState.Create(0,0,0,32,32,4096,1024,new NavigationSpatialState());mesh.UseObservedLinkConstants();
  var roots=new List<uint>();var owners=new List<NavigationMeshTileBytes>();
  foreach(var output in produced){
   byte[] bytes=output.Bytes;int x=BitConverter.ToInt32(bytes,8),z=BitConverter.ToInt32(bytes,12);
   Require(BitConverter.ToSingle(bytes,72)==x*32f&&BitConverter.ToSingle(bytes,80)==z*32f&&BitConverter.ToSingle(bytes,84)==(x+1)*32f&&BitConverter.ToSingle(bytes,92)==(z+1)*32f,"Actual bounds mismatch explicit grid");
   var owner=new NavigationMeshTileBytes(bytes,output.Length);var resultRef=new Field<uint>();var result=NavigationMeshTiles.Add(mesh,owner,resultRef);
   Require(result.Outcome==NavigationMeshTileOutcome.NativeSuccess&&result.NativeStatus==0x40000000,"Real tile insertion: "+result.Detail);uint r=resultRef.Read(0);
   Require(r!=0&&Object.ReferenceEquals(mesh.Tile(Tile(mesh,r),0).Data50.Read(0),owner)&&Object.ReferenceEquals(owner.Bytes,bytes),"Actual tile owner identity changed");roots.Add(r);owners.Add(owner);
  }
  Require(roots.Count==9,"Expected all nine pinned real resource layers");
  var query=new NavigationQuery();query.Mesh0.Write(mesh.Mesh);var adapter=new NavigationCrowdAdapterState();adapter.Query10.Write(query);adapter.Filter18.Include100.Write(1);adapter.Filter18.Exclude102.Write(0);
  adapter.Extent11C.X.Write(0.125f);adapter.Extent11C.Y.Write(2);adapter.Extent11C.Z.Write(0.125f);var critical=new NavigationCrowdCriticalSection();adapter.Critical138.Write(critical);
  var search=new NavigationCorridorState(query);search.Pool58.Write(NavigationSearchStorage.CreatePool26DC10(4096,1024));search.Queue60.Write(NavigationSearchStorage.CreateQueue(4096));var costs=new NavigationCorridorFilterCosts(adapter.Filter18);for(int n=0;n<64;n++)costs.AreaCosts00[n].Write(1);search.Attach(costs);
  var b=new NavigationCrowdAdapterBoundaries(mesh,search);b.UseObservedConstants();
  Console.WriteLine("ADAPTER CONFIG explicit own query/filter include1 exclude0, extents(.125,2,.125), pool4096/hash1024/cost1, preallocated32-reference vector, own reentrant Monitor; runtime configuration and native golden unobserved");
  int projections=0;uint chosenFrom=0,chosenTo=0;
  for(int ordinal=0;ordinal<roots.Count;ordinal++){
   uint basis=roots[ordinal];var polys=Tile(mesh,basis).Polys10.Read(0);bool projected=false;
   for(int p=0;p<polys.Length;p++){
    uint reference=basis|(uint)p;Require(NavigationCoreContracts.ValidateReference(mesh.Mesh,reference)==1,"Actual polygon ref invalid");
    if((mesh.Spatial.Poly(polys[p],0).Flags1C.Read(0)&1)==0)continue;
    if(!projected){
     var source=Center(mesh,reference);var output=new NavigationPoint3();var borrowedX=output.X;
     uint found=NavigationCrowdAdapterContracts.Project030B30(adapter,source,output,b,new Invocation());
     Require(found!=0&&NavigationCoreContracts.ValidateReference(mesh.Mesh,found)==1,"Actual slot18 nearest failed");Require(Object.ReferenceEquals(borrowedX,output.X),"Slot18 replaced borrowed output cell");
     byte bounded=NavigationCrowdAdapterContracts.Project030BB0(adapter,source,Field<float>.Of(0.125f),output,b,new Invocation());Require(bounded==1,"Actual slot20 radius projection failed");
     Console.WriteLine("ADAPTER PROJECT tile="+ordinal+" ref="+found.ToString("X8")+" point="+output.X.Read(0)+","+output.Y.Read(0)+","+output.Z.Read(0));projected=true;projections++;
    }
    if(chosenFrom==0)foreach(uint next in Neighbors(mesh,reference)){
     if((reference>>10)==(next>>10)||(mesh.Spatial.Poly(Poly(mesh,next),0).Flags1C.Read(0)&1)==0)continue;
     chosenFrom=reference;chosenTo=next;break;
    }
   }
   Require(projected,"Actual tile has no walkable polygon for adapter probe");
  }
  Require(projections==9&&chosenFrom!=0&&chosenTo!=0,"Nine actual projections and a genuine external edge required");
  var from=Center(mesh,chosenFrom);var to=Center(mesh,chosenTo);uint start=NavigationCrowdAdapterContracts.Project030B30(adapter,from,null,b,new Invocation()),end=NavigationCrowdAdapterContracts.Project030B30(adapter,to,null,b,new Invocation());
  Require(start!=0&&end!=0&&(start>>10)!=(end>>10),"Actual adapter-selected endpoints must remain in distinct tiles");
  var vector=new NavigationCrowdRefVector(32,0);var backing=vector.Storage;byte foundPath=NavigationCrowdAdapterContracts.FindCorridor030790(adapter,from,to,vector,b,new Invocation());
  Require(foundPath==1,"Actual slot0 corridor adapter failed");int count=vector.End08.Read(0).Index;Require(count>1&&count<=32,"Nontrivial actual corridor expected within fixed native slot capacity");Require(Object.ReferenceEquals(backing,vector.Begin00.Read(0).Buffer)&&Object.ReferenceEquals(backing,vector.End08.Read(0).Buffer),"Path vector backing changed");
  Require(backing.Cells[0].Read(0)==start&&backing.Cells[count-1].Read(0)==end,"Adapter bool alone is insufficient: actual endpoint reachability required by this probe");
  int crossings=0;var path=new List<string>();
  for(int n=0;n<count;n++){uint r=backing.Cells[n].Read(0);Require(NavigationCoreContracts.ValidateReference(mesh.Mesh,r)==1,"Adapter returned invalid ref");path.Add(r.ToString("X8"));if(n!=0){uint prior=backing.Cells[n-1].Read(0);Require(Linked(mesh,prior,r),"Adapter route contains nonadjacent pair");if((prior>>10)!=(r>>10))crossings++;}}
  Require(crossings>0,"Expected actual external-tile transition");Require(Object.ReferenceEquals(query,adapter.Query10.Read(0))&&Object.ReferenceEquals(mesh.Mesh,query.Mesh0.Read(0)),"Shared adapter/query/mesh identities changed");Require(critical.OwnOutstandingEnters==0,"Own Monitor enter not released");
  for(int n=0;n<roots.Count;n++)Require(Object.ReferenceEquals(owners[n],mesh.Tile(Tile(mesh,roots[n]),0).Data50.Read(0)),"Registered source owner changed after queries");
  Console.WriteLine("ADAPTER CORRIDOR count="+count+" tileCrossings="+crossings+" refs="+String.Join(",",path.ToArray()));
  Console.WriteLine("PASS actual saved tiles=9, slot18 projections=11, slot20 projections=9, slot0 corridor=1; explicit owned cold parameters, real production query calls and shared identities; no native golden, loaded crowd, character movement or game acceptance");return 0;
 }catch(Exception e){Console.Error.WriteLine("FAIL "+e);return 1;}}
}
