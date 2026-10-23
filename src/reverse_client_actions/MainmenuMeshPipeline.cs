using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Actual saved mainmenu data -> polygons -> VAND7 -> the existing mutable
// query identities. No renderer, input, actor movement or native execution.
public static class MainmenuMeshPipeline {
 static void Require(bool value,string why){if(!value)throw new InvalidOperationException(why);}
 static NavigationPoint3 Point(float x,float y,float z){var p=new NavigationPoint3();p.X.Write(x);p.Y.Write(y);p.Z.Write(z);return p;}
 static void Success(NavigationMeshTileResult r,string operation){Require(r.Outcome==NavigationMeshTileOutcome.NativeSuccess&&r.NativeStatus==NavigationMeshTiles.Success,operation+": "+r.Outcome+" "+r.Detail+" at "+r.StopRva.ToString("X"));}
 public static int Main(string[] args){try{
  Require(args.Length==1&&args[0]=="--explicit-default-domain","Explicit own cold domain required");
  List<NavigationTileDataOutput> produced=MainmenuTilePipeline.BuildExplicitColdTiles();
  var spatial=new NavigationSpatialState();
  var state=NavigationMeshTilesState.Create(0,0,0,32,32,4096,1024,spatial);
  state.UseObservedLinkConstants();
  Console.WriteLine("MESH CONFIG explicit origin(0,0,0), tile width/depth32, capacity4096/1024, observed disk link constants; runtime initialization unobserved");
  var owners=new List<NavigationMeshTileBytes>();var refs=new List<uint>();
  foreach(var output in produced){
   byte[] bytes=output.Bytes;int x=BitConverter.ToInt32(bytes,8),z=BitConverter.ToInt32(bytes,12),layer=BitConverter.ToInt32(bytes,16);
   Require(BitConverter.ToSingle(bytes,72)==x*32f&&BitConverter.ToSingle(bytes,80)==z*32f&&BitConverter.ToSingle(bytes,84)==(x+1)*32f&&BitConverter.ToSingle(bytes,92)==(z+1)*32f,"Actual header grid/bounds do not match this explicit coordinate domain");
   var owner=new NavigationMeshTileBytes(bytes,output.Length);var reference=Field<uint>.Of(0);
   var added=NavigationMeshTiles.Add(state,owner,reference);Success(added,"Actual tile insertion");
   Require(reference.Read(0)!=0&&reference.Read(0)==added.Reference,"Late output reference not published");
   var looked=NavigationMeshTiles.Lookup(state,x,z,layer);Require(looked.Outcome==NavigationMeshTileOutcome.NativeSuccess&&looked.NativeStatus==null&&looked.Reference==added.Reference,"Coordinate lookup identity changed or unsupported; native lookup returns a reference, not a status");
   int slot=(int)((added.Reference>>10)&4095);var tile=state.Mesh.Tiles50.Read(0)[slot];
   Require(Object.ReferenceEquals(state.Tile(tile,0).Data50.Read(0),owner)&&Object.ReferenceEquals(owner.Bytes,bytes),"Tile byte owner was copied");
   owners.Add(owner);refs.Add(added.Reference);
  }
  Require(owners.Count==9,"Actual nine tile owners required");
  var query=new NavigationQuery();query.Mesh0.Write(state.Mesh);
  var b=new NavigationNearestBoundaries(spatial);b.UseObservedNearestConstants();
  var filter=new NavigationQueryFilter();filter.Include100.Write(1);filter.Exclude102.Write(0);
  long totalPolygons=0,internalLinks=0,externalLinks=0;int nearestQueries=0;
  for(int ordinal=0;ordinal<refs.Count;ordinal++){
   uint basis=refs[ordinal];var tile=state.Mesh.Tiles50.Read(0)[(int)((basis>>10)&4095)];
   var polys=tile.Polys10.Read(0);var vertices=tile.Vertices18.Read(0);bool queried=false;
   for(int p=0;p<polys.Length;p++){
    var poly=polys[p];totalPolygons++;Require(NavigationCoreContracts.ValidateReference(state.Mesh,basis|(uint)p)==1,"Produced polygon reference is invalid");
    uint link=state.Poly(poly,0).FirstLink00.Read(0);var seen=new HashSet<uint>();
    while(link!=UInt32.MaxValue){
     Require(seen.Add(link),"Produced link chain has a cycle");var links=state.Tile(tile,0).Links20.Read(0);Require(link<links.Length,"Produced link outside allocated extent");var entry=links[(int)link];
     Require(NavigationCoreContracts.ValidateReference(state.Mesh,entry.Reference00.Read(0))==1,"Produced link target is not in this mesh");
     if(entry.Side09.Read(0)==255)internalLinks++;else externalLinks++;link=entry.Next04.Read(0);
    }
    if(!queried&&(spatial.Poly(poly,0).Flags1C.Read(0)&1)!=0){
     int count=poly.VertexCount1E.Read(0);Require(count>=3&&count<=6,"Actual polygon count outside cold domain");float x=0,y=0,z=0;
     for(int n=0;n<count;n++){var v=vertices[poly.Indices04[n].Read(0)];x+=v.X.Read(0);y+=v.Y.Read(0);z+=v.Z.Read(0);}
     // A query chosen from actual produced geometry, not an injected result.
     var center=Point(x/count,y/count,z/count);var found=Field<uint>.Of(0);var nearest=new NavigationPoint3();
     uint status=NavigationNearestContracts.Nearest(query,center,Point(.125f,1f,.125f),filter,found,nearest,b,new Invocation());
     Require((status&0x80000000)==0&&found.Read(0)!=0&&NavigationCoreContracts.ValidateReference(state.Mesh,found.Read(0))==1,"Actual geometry nearest lookup failed");
     Console.WriteLine("NEAREST tile="+ordinal+" ref="+found.Read(0).ToString("X8")+" point="+nearest.X.Read(0)+","+nearest.Y.Read(0)+","+nearest.Z.Read(0));
     nearestQueries++;queried=true;
    }
   }
   Require(queried,"No walkable polygon available for this actual tile query");
  }
  Require(internalLinks>0&&externalLinks>0,"This sample did not exercise both internal and adjacent tile links");
  Console.WriteLine("PASS actual mesh tiles="+owners.Count+" polygons="+totalPolygons+" internalLinks="+internalLinks+" externalLinks="+externalLinks+" realNearestQueries="+nearestQueries+"; no cross-polygon corridor, mouse movement or gameplay acceptance");return 0;
 }catch(Exception e){Console.Error.WriteLine("FAIL "+e);return 1;}}
}
