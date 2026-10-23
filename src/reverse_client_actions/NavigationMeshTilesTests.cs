using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// New, pure-memory mesh tile fixtures. VAND7 inputs are produced by the frozen
// own TileData serializer; they are not native runtime captures or gameplay.
// No file, process, backend or original game access is performed here.
public static class NavigationMeshTilesTests {
 static int scenarios,assertions;
 public static int Scenarios {get{return scenarios;}}
 public static int Assertions {get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Test(string name,Action body){body();scenarios++;Console.WriteLine("PASS "+name);}
 static uint U32(byte[] bytes,int at){return (uint)bytes[at]|((uint)bytes[at+1]<<8)|((uint)bytes[at+2]<<16)|((uint)bytes[at+3]<<24);}
 static ushort U16(byte[] bytes,int at){return (ushort)(bytes[at]|(bytes[at+1]<<8));}
 static void Put32(byte[] bytes,int at,uint value){unchecked{for(int n=0;n<4;n++)bytes[at+n]=(byte)(value>>(8*n));}}
 static void Put16(byte[] bytes,int at,ushort value){unchecked{bytes[at]=(byte)value;bytes[at+1]=(byte)(value>>8);}}
 static void EqualBytes(byte[] expected,byte[] actual,string why){
  Equal(expected.Length,actual.Length,why+" length");bool same=true;
  for(int n=0;n<expected.Length;n++)if(expected[n]!=actual[n])same=false;
  Check(same,why);
 }
 static NavigationTileQuantizedVertex V(ushort x,ushort y,ushort z){return new NavigationTileQuantizedVertex(x,y,z);}
 static ushort[] Ended(ushort a,ushort b,ushort c){return new ushort[]{a,b,c,65535,65535,65535};}
 static ushort[] Closed(){return new ushort[]{65535,65535,65535,65535,65535,65535};}
 static byte[] Serialize(int x,int z,int layer,NavigationTileQuantizedVertex[] vertices,NavigationTilePolygon[] polygons,NavigationTileBounds bounds){
  var areas=new byte[polygons.Length];var flags=new ushort[polygons.Length];
  for(int n=0;n<polygons.Length;n++){areas[n]=1;flags[n]=1;}
  var input=new NavigationTileDataInput(vertices,polygons,areas,flags,bounds,1f,1f,x,z,layer,2f,0.5f,1f);
  var output=new NavigationTileDataOutput();var result=NavigationTileData.Build(input,output);
  Equal(NavigationTileDataOutcome.Published,result.Outcome,"Own fixture serialized without fallback; detail="+result.Detail);
  Same(output,result.Output,"Own serializer output identity");
  Equal(output.Length,output.Bytes.Length,"Own fixture exact extent");return output.Bytes;
 }
 static byte[] Triangle(int x,int z,int layer){
  return Serialize(x,z,layer,new[]{V(0,0,0),V(1,0,0),V(0,0,1)},
   new[]{new NavigationTilePolygon(Ended(0,1,2),Closed())},
   new NavigationTileBounds(x,0,z,x+1,2,z+1));
 }
 static byte[] InternalPair(int x,int z,int layer){
  return Serialize(x,z,layer,new[]{V(0,0,0),V(1,0,0),V(1,0,1),V(0,0,1)},
   new[]{new NavigationTilePolygon(Ended(0,1,2),new ushort[]{65535,65535,1,65535,65535,65535}),
         new NavigationTilePolygon(Ended(0,2,3),new ushort[]{0,65535,65535,65535,65535,65535})},
   new NavigationTileBounds(x,0,z,x+1,2,z+1));
 }
 static NavigationMeshTilesState State(int tiles,int polygons,bool constants){
  var spatial=new NavigationSpatialState();
  var state=NavigationMeshTilesState.Create(0f,0f,0f,1f,1f,tiles,polygons,spatial);
  if(constants)state.UseObservedLinkConstants();return state;
 }
 static NavigationMeshTilesState State(){return State(4,8,true);}
 static Field<uint> Output(uint sentinel){var field=new Field<uint>();field.Write(sentinel);return field;}
 static void Native(uint status,NavigationMeshTileResult result,string why){
  Equal((uint?)status,result.NativeStatus,why+" exact native status");
  Equal((status&0x80000000u)==0?NavigationMeshTileOutcome.NativeSuccess:NavigationMeshTileOutcome.NativeFailure,result.Outcome,why+" outcome");
 }
 static void Unsupported(uint at,NavigationMeshTileResult result,string why){
  Equal(NavigationMeshTileOutcome.OwnUnsupported,result.Outcome,why+" own stop");
  Equal((uint?)null,result.NativeStatus,why+" no fabricated native status");Equal(at,result.StopRva,why+" stop address");
 }
 static void LookupReturned(NavigationMeshTileResult result,string why){
  Equal(NavigationMeshTileOutcome.NativeSuccess,result.Outcome,why+" normal return");Equal((uint?)null,result.NativeStatus,why+" lookup returns an integer reference, not a status");
 }
 static void Released(Action action,string why){
  try{action();}catch(MissingContract){assertions++;return;}throw new Exception("Expected explicit own released view: "+why);
 }
 static uint Added(NavigationMeshTilesState state,NavigationMeshTileBytes data){
  var output=Output(0xeeeeeeee);var result=NavigationMeshTiles.Add(state,data,output);
  Native(NavigationMeshTiles.Success,result,"Insert own serialized tile");
  Equal(result.Reference,output.Read(0),"Successful output reference write");return result.Reference;
 }
 static NavigationTile Slot(NavigationMeshTilesState state,int index){return state.Mesh.Tiles50.Read(0)[index];}
 static NavigationMeshPolyLinks Links(NavigationMeshTilesState state,NavigationTile tile,int polygon){return state.Poly(tile.Polys10.Read(0)[polygon],0);}
 static byte[] PortalSquare(int x,int z,int layer,int portalEdge,ushort portalDirection){
  var neighbors=Closed();neighbors[portalEdge]=portalDirection;
  return Serialize(x,z,layer,new[]{V(0,0,0),V(0,0,1),V(1,0,1),V(1,0,0)},
   new[]{new NavigationTilePolygon(new ushort[]{0,1,2,3,65535,65535},neighbors)},
   new NavigationTileBounds(x,0,z,x+1,2,z+1));
 }

 public static void Run(){
  scenarios=0;assertions=0;
  Test("Cold mesh establishes one identity graph and ordered free slots",delegate{
   var s=State();Equal(0x40000000u,s.InitializationStatus,"Native initialization result in managed allocation domain");
   Same(s.Mesh,s.MeshSpatial.Identity,"Same mesh spatial identity");Same(s.MeshSpatial,s.Spatial.Mesh(s.Mesh,0),"Registry retains exact spatial view");
   Equal(2u,s.Mesh.TileBits5C.Read(0),"Four slots use two tile bits");Equal(3u,s.Mesh.PolyBits60.Read(0),"Eight polygons use three polygon bits");Equal(27u,s.Mesh.SaltBits58.Read(0),"Remaining salt bits");
   var tiles=s.Mesh.Tiles50.Read(0);Same(tiles[0],s.Free48.Read(0),"First free slot zero");
   for(int n=0;n<tiles.Length;n++){Equal(1u,tiles[n].Salt00.Read(0),"Initial salt");Same(null,tiles[n].Header08.Read(0),"No invented loaded header");Same(n+1<tiles.Length?tiles[n+1]:null,s.Spatial.Tile(tiles[n],0).Next60.Read(0),"Free list order");}
   var miss=NavigationMeshTiles.Lookup(s,-7,11,2);LookupReturned(miss,"Empty lookup");Equal(0u,miss.Reference,"Empty means zero reference");
  });
  Test("Magic and version failures preserve free list output and input bytes",delegate{
   var s=State();var first=s.Free48.Read(0);var output=Output(0x13579bdf);
   var magic=Triangle(0,0,0);Put32(magic,0,0);var before=(byte[])magic.Clone();
   Native(0x80000001u,NavigationMeshTiles.Add(s,magic,magic.Length,output),"Wrong magic");Same(first,s.Free48.Read(0),"Wrong magic did not consume slot");Equal(0x13579bdfu,output.Read(0),"Wrong magic left output");EqualBytes(before,magic,"Wrong magic bytes untouched");
   var version=Triangle(0,0,0);Put32(version,4,8);before=(byte[])version.Clone();
   Native(0x80000002u,NavigationMeshTiles.Add(s,version,version.Length,output),"Wrong version");Same(first,s.Free48.Read(0),"Wrong version did not consume slot");Equal(0x13579bdfu,output.Read(0),"Wrong version left output");EqualBytes(before,version,"Wrong version bytes untouched");
  });
  Test("Declared byte extent is an own stop rather than fabricated native failure",delegate{
   var s=State();var first=s.Free48.Read(0);var output=Output(9);var bytes=Triangle(0,0,0);
   Unsupported(0x26827d,NavigationMeshTiles.Add(s,bytes,3,output),"Unreadable first DWORD");Same(first,s.Free48.Read(0),"No early publication");Equal(9u,output.Read(0),"Unknown read leaves output");
   var released=new NavigationMeshTileBytes(bytes,bytes.Length);released.MarkReleased();
   Unsupported(0x26827d,NavigationMeshTiles.Add(s,released,output),"Explicit released byte owner");Same(first,s.Free48.Read(0),"Released owner does not consume slot");
  });
  Test("Successful add retains exact byte owner and query identities",delegate{
   var s=State();var bytes=Triangle(-7,11,2);var data=new NavigationMeshTileBytes(bytes,bytes.Length);uint reference=Added(s,data);
   Equal(32u,reference,"Salt one slot zero polygon zero reference");var tile=Slot(s,0);var f=s.Tile(tile,0);
   Same(data,f.Data50.Read(0),"Registered owner identity");Same(bytes,f.Data50.Read(0).Bytes,"Real bytes retained without clone");Equal(bytes.Length,f.DataSize58.Read(0),"Declared registered extent");Equal(1u,f.Flags5C.Read(0),"Native owned-data flag");
   var h=tile.Header08.Read(0);Same(h,s.Header(h,0).Identity,"Same header view");Same(tile,s.Spatial.Tile(tile,0).Identity,"Same tile for existing query registry");Equal(-7,s.Spatial.Header(h,0).GridX08.Read(0),"Signed X");Equal(11,s.Spatial.Header(h,0).GridZ0C.Read(0),"Signed Z");Equal(2,s.Header(h,0).Layer10.Read(0),"Layer identity");
   Equal(1u,h.PolyCount18.Read(0),"Effective polygon count");Equal(3,tile.Vertices18.Read(0).Length,"Effective vertex view count");Same(null,f.OffMesh48.Read(0),"Known cold zero offmesh section");
   var lookup=NavigationMeshTiles.Lookup(s,-7,11,2);LookupReturned(lookup,"Lookup exact coordinates");Equal(reference,lookup.Reference,"Lookup same native integer reference");Equal(0u,NavigationMeshTiles.Lookup(s,-7,11,3).Reference,"Different layer is absent");
   Equal(UInt32.MaxValue,U32(bytes,136),"First-link write reached original polygon bytes");Equal(UInt32.MaxValue,U32(bytes,196),"Free-list terminal write reached original link bytes");
  });
  Test("Optional reference output does not prevent real registration",delegate{
   var s=State();var bytes=Triangle(3,4,0);var result=NavigationMeshTiles.Add(s,bytes,bytes.Length,null);
   Native(NavigationMeshTiles.Success,result,"Null optional output");Equal(0u,result.Reference,"No synthetic output ref when pointer absent");Equal(32u,NavigationMeshTiles.Lookup(s,3,4,0).Reference,"Registered tile remains discoverable");
  });
  Test("Same coordinates preserve distinct layers and newest bucket order",delegate{
   var s=State();var a=Triangle(0,0,0);var b=Triangle(0,0,1);uint ar=Added(s,new NavigationMeshTileBytes(a,a.Length));uint br=Added(s,new NavigationMeshTileBytes(b,b.Length));
   Equal(32u,ar,"First slot reference");Equal(40u,br,"Second slot reference");Equal(ar,NavigationMeshTiles.Lookup(s,0,0,0).Reference,"Older layer stays addressable");Equal(br,NavigationMeshTiles.Lookup(s,0,0,1).Reference,"Newer layer stays addressable");
   Same(Slot(s,1),s.MeshSpatial.Buckets40.Read(0)[0],"Newest matching bucket head");Same(Slot(s,0),s.Spatial.Tile(Slot(s,1),0).Next60.Read(0),"Previous layer same identity in chain");
  });
  Test("Duplicate rejection does not replace ownership or consume free slot",delegate{
   var s=State();var a=Triangle(0,0,0);var original=new NavigationMeshTileBytes(a,a.Length);Added(s,original);var free=s.Free48.Read(0);var before=(byte[])a.Clone();
   var b=Triangle(0,0,0);var fresh=(byte[])b.Clone();var output=Output(0xabcdef01);
   Native(NavigationMeshTiles.Duplicate,NavigationMeshTiles.Add(s,b,b.Length,output),"Duplicate xyz layer");Same(free,s.Free48.Read(0),"Free head unchanged");Same(original,s.Tile(Slot(s,0),0).Data50.Read(0),"Original ownership unchanged");Equal(0xabcdef01u,output.Read(0),"Output sentinel retained");EqualBytes(before,a,"Registered bytes unchanged");EqualBytes(fresh,b,"Rejected bytes unchanged");
  });
  Test("Pool exhaustion is distinct from duplicate and leaves output untouched",delegate{
   var s=State(1,8,true);var a=Triangle(0,0,0);uint reference=Added(s,new NavigationMeshTileBytes(a,a.Length));Equal(8u,reference,"Zero tile bits still retain salt");Same(null,s.Free48.Read(0),"Pool exhausted");
   var b=Triangle(20,20,0);var output=Output(71);Native(NavigationMeshTiles.NoFreeTile,NavigationMeshTiles.Add(s,b,b.Length,output),"No free tile");Equal(71u,output.Read(0),"Exhaustion has no output write");Equal(reference,NavigationMeshTiles.Lookup(s,0,0,0).Reference,"Existing tile intact");
  });
  Test("Internal adjacency writes directed links into the same owned bytes",delegate{
   var s=State();var bytes=InternalPair(0,0,0);uint basis=Added(s,new NavigationMeshTileBytes(bytes,bytes.Length));var tile=Slot(s,0);var f=s.Tile(tile,0);var links=f.Links20.Read(0);
   Equal(0u,Links(s,tile,0).FirstLink00.Read(0),"First polygon head");Equal(1u,Links(s,tile,1).FirstLink00.Read(0),"Second polygon head");Equal(basis|1u,links[0].Reference00.Read(0),"First points to polygon one");Equal(basis,links[1].Reference00.Read(0),"Second points to polygon zero");
   Equal((byte)2,links[0].Edge08.Read(0),"First edge index");Equal((byte)0,links[1].Edge08.Read(0),"Second edge index");Equal((byte)255,links[0].Side09.Read(0),"Internal-link side sentinel");Equal((byte)0,links[0].Min0A.Read(0),"Internal min");Equal((byte)0,links[0].Max0B.Read(0),"Internal max");Equal(2u,f.LinkFree04.Read(0),"Two links consumed");
   Equal(0u,U32(bytes,148),"First polygon raw head");Equal(1u,U32(bytes,180),"Second polygon raw head");Equal(basis|1u,U32(bytes,212),"First link raw reference");Equal(basis,U32(bytes,224),"Second link raw reference");Equal(UInt32.MaxValue,U32(bytes,216),"First raw chain end");
  });
  Test("Reverse edge traversal yields ascending edge order in linked list",delegate{
   var s=State();var bytes=Serialize(0,0,0,new[]{V(0,0,0),V(1,0,0),V(0,0,1)},new[]{new NavigationTilePolygon(Ended(0,1,2),new ushort[]{0,0,0,65535,65535,65535})},new NavigationTileBounds(0,0,0,1,2,1));
   // Repeated directed self-neighbors are an instruction-contract fixture,
   // not a claim that such topology occurs in a shipped level.
   uint basis=Added(s,new NavigationMeshTileBytes(bytes,bytes.Length));var tile=Slot(s,0);var f=s.Tile(tile,0);var links=f.Links20.Read(0);uint index=Links(s,tile,0).FirstLink00.Read(0);
   Equal(2u,index,"Last allocation becomes head");for(int edge=0;edge<3;edge++){Equal((byte)edge,links[index].Edge08.Read(0),"Ascending chain edge");Equal(basis,links[index].Reference00.Read(0),"Self-reference remains integer basis");index=links[index].Next04.Read(0);}Equal(UInt32.MaxValue,index,"Chain terminated");Equal(UInt32.MaxValue,f.LinkFree04.Read(0),"Exact three-link capacity exhausted");
  });
  Test("Zero stale salt and out-of-range references reject without removal",delegate{
   var s=State(3,8,true);var bytes=Triangle(0,0,0);uint reference=Added(s,new NavigationMeshTileBytes(bytes,bytes.Length));var free=s.Free48.Read(0);var before=(byte[])bytes.Clone();
   Native(NavigationMeshTiles.Invalid,NavigationMeshTiles.Remove(s,0),"Zero reference");Native(NavigationMeshTiles.Invalid,NavigationMeshTiles.Remove(s,reference+32),"Wrong salt");Native(NavigationMeshTiles.Invalid,NavigationMeshTiles.Remove(s,56),"Index three beyond three-slot array");
   Same(free,s.Free48.Read(0),"Invalid removes preserve free list");Equal(reference,NavigationMeshTiles.Lookup(s,0,0,0).Reference,"Original lookup intact");EqualBytes(before,bytes,"Invalid removes do not rewrite links");
  });
  Test("Remove releases data views and reuses live tile identity with new salt",delegate{
   var s=State();var bytes=InternalPair(0,0,0);var data=new NavigationMeshTileBytes(bytes,bytes.Length);uint original=Added(s,data);var tile=Slot(s,0);var h=tile.Header08.Read(0);var p=tile.Polys10.Read(0)[0];var v=tile.Vertices18.Read(0)[0];var link=s.Tile(tile,0).Links20.Read(0)[0];
   Native(NavigationMeshTiles.Success,NavigationMeshTiles.Remove(s,original),"Remove live tile");Equal(0u,NavigationMeshTiles.Lookup(s,0,0,0).Reference,"Lookup removed");Same(tile,s.Free48.Read(0),"Removed slot becomes free head");Equal(2u,tile.Salt00.Read(0),"Salt incremented");Same(null,tile.Header08.Read(0),"Header cleared");Same(null,s.Tile(tile,0).Data50.Read(0),"Owned data pointer cleared");Equal(0,s.Tile(tile,0).DataSize58.Read(0),"Data length cleared");
   Released(delegate{data.RequireLive(0);},"old data");Released(delegate{h.RequireLive(0);},"old header");Released(delegate{p.RequireLive(0);},"old polygon");Released(delegate{v.RequireLive(0);},"old vertex");Released(delegate{link.RequireLive(0);},"old link");tile.RequireLive(0);
   var fresh=Triangle(0,0,0);uint replacement=Added(s,new NavigationMeshTileBytes(fresh,fresh.Length));Equal(64u,replacement,"Same slot new salt reference");Same(tile,Slot(s,0),"Slot object identity unchanged");Native(NavigationMeshTiles.Invalid,NavigationMeshTiles.Remove(s,original),"Old salt cannot remove replacement");Equal(replacement,NavigationMeshTiles.Lookup(s,0,0,0).Reference,"Replacement remains");
  });
  Test("Replacement sequence does not roll back successful remove after bad add",delegate{
   var s=State();var oldBytes=Triangle(0,0,0);var old=new NavigationMeshTileBytes(oldBytes,oldBytes.Length);uint original=Added(s,old);var tile=Slot(s,0);
   Native(NavigationMeshTiles.Success,NavigationMeshTiles.Remove(s,original),"First replacement step removes");var bad=Triangle(0,0,0);Put32(bad,0,0);var output=Output(0x9999);
   Native(0x80000001u,NavigationMeshTiles.Add(s,bad,bad.Length,output),"Second replacement step rejects");Equal(0u,NavigationMeshTiles.Lookup(s,0,0,0).Reference,"Removed tile not restored");Same(tile,s.Free48.Read(0),"Freed slot remains free");Equal(2u,tile.Salt00.Read(0),"Salt not rolled back");Equal(0x9999u,output.Read(0),"Failed replacement output unchanged");Released(delegate{old.RequireLive(0);},"Original owner stays released");
  });
  Test("A fresh wrapper cannot rebind a previously owned byte array after release",delegate{
   var s=State();var bytes=Triangle(0,0,0);var owner=new NavigationMeshTileBytes(bytes,bytes.Length);uint original=Added(s,owner);
   var otherBytes=InternalPair(10,10,0);var otherOwner=new NavigationMeshTileBytes(otherBytes,otherBytes.Length);uint otherRef=Added(s,otherOwner);var otherTile=Slot(s,1);var otherFields=s.Tile(otherTile,0);var otherHeader=otherTile.Header08.Read(0);var otherLinks=otherFields.Links20.Read(0);var otherBefore=(byte[])otherBytes.Clone();
   var output=Output(0xdeadbeef);var free=s.Free48.Read(0);
   // While the original is registered, native duplicate detection happens
   // before the own ByteOwners check. It must not become a fake own status.
   Native(NavigationMeshTiles.Duplicate,NavigationMeshTiles.Add(s,new NavigationMeshTileBytes(bytes,bytes.Length),output),"Live array alias rejected as duplicate first");Same(free,s.Free48.Read(0),"Live duplicate did not consume free slot");Same(owner,s.Tile(Slot(s,0),0).Data50.Read(0),"Original owner was not replaced");Equal(0xdeadbeefu,output.Read(0),"Live duplicate leaves late output");
   Native(NavigationMeshTiles.Success,NavigationMeshTiles.Remove(s,original),"Release original ownership");Released(delegate{owner.RequireLive(0);},"Original owner remains released");var reuseSlot=Slot(s,0);Same(reuseSlot,s.Free48.Read(0),"Removed slot is next free");var releasedBytes=(byte[])bytes.Clone();
   var alias=new NavigationMeshTileBytes(bytes,bytes.Length);Unsupported(0,NavigationMeshTiles.Add(s,alias,output),"New wrapper does not erase byte-array ownership history");
   Same(Slot(s,2),s.Free48.Read(0),"Native pop-free prefix retained before own guard");Same(reuseSlot,s.MeshSpatial.Buckets40.Read(0)[0],"Native bucket publication prefix retained");Same(otherTile,s.Spatial.Tile(reuseSlot,0).Next60.Read(0),"Other registered tile remains in bucket chain");Same(null,reuseSlot.Header08.Read(0),"No new header projected");Same(null,s.Tile(reuseSlot,0).Data50.Read(0),"Rejected alias was not made owner");Equal(2u,reuseSlot.Salt00.Read(0),"Guard does not roll back or advance salt");Equal(0xdeadbeefu,output.Read(0),"Own guard leaves late output sentinel");EqualBytes(releasedBytes,bytes,"Rejected array not rewritten");
   Equal(0u,NavigationMeshTiles.Lookup(s,0,0,0).Reference,"No synthetic registration of rejected alias");Equal(otherRef,NavigationMeshTiles.Lookup(s,10,10,0).Reference,"Other registered reference preserved");Same(otherOwner,otherFields.Data50.Read(0),"Other owner preserved");Same(otherHeader,otherTile.Header08.Read(0),"Other header identity preserved");Same(otherLinks,otherFields.Links20.Read(0),"Other link-array identity preserved");Equal(0u,Links(s,otherTile,0).FirstLink00.Read(0),"Other first polygon link head retained");Equal(1u,Links(s,otherTile,1).FirstLink00.Read(0),"Other second polygon link head retained");EqualBytes(otherBefore,otherBytes,"Other complete registered bytes unchanged");
  });
  Test("Own materialization extent stop retains earlier bucket publication",delegate{
   var s=State();var bytes=Triangle(0,0,0);var output=Output(13);var first=Slot(s,0);var second=Slot(s,1);
   // Native has no newly invented full-format validator here. The managed
   // projection rejects the short stable extent after the proven bucket write.
   Unsupported(0,NavigationMeshTiles.Add(s,bytes,20,output),"Short owned projection");Same(second,s.Free48.Read(0),"Free slot already consumed");Same(first,s.MeshSpatial.Buckets40.Read(0)[0],"Published bucket prefix retained");Same(null,first.Header08.Read(0),"Header not yet published");Same(null,s.Tile(first,0).Data50.Read(0),"Ownership not yet published");Equal(13u,output.Read(0),"No success reference write");Equal(0u,NavigationMeshTiles.Lookup(s,0,0,0).Reference,"Null header is not a loaded tile");
  });
  Test("Zero link capacity stops at actual terminal-link address after field setup",delegate{
   var s=State();var bytes=Triangle(0,0,0);Put32(bytes,32,0);var before=(byte[])bytes.Clone();var output=Output(14);
   Unsupported(0x268462,NavigationMeshTiles.Add(s,bytes,bytes.Length,output),"Negative final-link index");var tile=Slot(s,0);Check(tile.Vertices18.Read(0)!=null,"Vertex pointer prefix exists");Check(tile.Polys10.Read(0)!=null,"Polygon pointer prefix exists");Equal(0,s.Tile(tile,0).Links20.Read(0).Length,"Explicit zero link view");Same(null,tile.Header08.Read(0),"Header still unpublished");Equal(14u,output.Read(0),"No late success output");EqualBytes(before,bytes,"Own bad-address stop avoids invalid write");
  });
  Test("Positive offmesh count is explicit unsupported with registration prefix",delegate{
   var s=State();var bytes=Triangle(0,0,0);Put32(bytes,52,1);var data=new NavigationMeshTileBytes(bytes,bytes.Length);var output=Output(15);
   Unsupported(0x2674b6,NavigationMeshTiles.Add(s,data,output),"Unclosed offmesh body");var tile=Slot(s,0);Same(data,s.Tile(tile,0).Data50.Read(0),"Owner publication is not rolled back");Check(tile.Header08.Read(0)!=null,"Header prefix survives");Equal(UInt32.MaxValue,Links(s,tile,0).FirstLink00.Read(0),"Internal-link stage already completed");Equal(15u,output.Read(0),"No fabricated completed output");Equal(32u,NavigationMeshTiles.Lookup(s,0,0,0).Reference,"Lookup observes partial native publication");
   Native(NavigationMeshTiles.Success,NavigationMeshTiles.Remove(s,32),"Published prefix can be removed");Released(delegate{data.RequireLive(0);},"Removed partial owner");
  });
  Test("Unobserved link constant stops after new tile publication not as success",delegate{
   var s=State(4,8,false);var a=Triangle(0,0,0);Added(s,new NavigationMeshTileBytes(a,a.Length));var b=Triangle(0,0,1);var owner=new NavigationMeshTileBytes(b,b.Length);var output=Output(16);
   Unsupported(0x266e5c,NavigationMeshTiles.Add(s,owner,output),"Missing observed constant at external-links entry");Same(owner,s.Tile(Slot(s,1),0).Data50.Read(0),"New owner already published");Equal(40u,NavigationMeshTiles.Lookup(s,0,0,1).Reference,"Published second layer still visible");Equal(16u,output.Read(0),"Missing constant never writes success output");
  });
  Test("Adjacent portals create real reciprocal links and removal retains internal edges",delegate{
   var s=State();
   var a=Serialize(0,0,0,new[]{V(0,0,0),V(1,0,0),V(1,0,1),V(0,0,1)},
    new[]{new NavigationTilePolygon(Ended(0,1,2),new ushort[]{65535,0x8002,1,65535,65535,65535}),new NavigationTilePolygon(Ended(0,2,3),new ushort[]{0,65535,65535,65535,65535,65535})},
    new NavigationTileBounds(0,0,0,1,2,1));
   var b=PortalSquare(1,0,0,0,0x8000);var aOwner=new NavigationMeshTileBytes(a,a.Length);var bOwner=new NavigationMeshTileBytes(b,b.Length);
   uint ar=Added(s,aOwner);uint br=Added(s,bOwner);var at=Slot(s,0);var bt=Slot(s,1);var af=s.Tile(at,0);var bf=s.Tile(bt,0);var al=af.Links20.Read(0);var bl=bf.Links20.Read(0);
   Equal(2u,Links(s,at,0).FirstLink00.Read(0),"External link prepended after two internal allocations");Equal(br,al[2].Reference00.Read(0),"A reaches B integer reference");Equal((byte)1,al[2].Edge08.Read(0),"A east edge");Equal((byte)0,al[2].Side09.Read(0),"A direction zero");Equal((byte)0,al[2].Min0A.Read(0),"Full portal lower bound");Equal((byte)255,al[2].Max0B.Read(0),"Full portal upper bound");Equal(0u,al[2].Next04.Read(0),"Internal link remains behind external");
   Equal(0u,Links(s,bt,0).FirstLink00.Read(0),"B external head");Equal(ar,bl[0].Reference00.Read(0),"B reciprocal reference");Equal((byte)4,bl[0].Side09.Read(0),"B opposite direction four");Equal((byte)0,bl[0].Edge08.Read(0),"B west edge");
   Equal(br,U32(a,212+24),"External link reference written to A original byte buffer");Equal(ar,U32(b,180),"Reciprocal link written to B original byte buffer");
   Native(NavigationMeshTiles.Success,NavigationMeshTiles.Remove(s,br),"Remove neighboring tile");Equal(0u,Links(s,at,0).FirstLink00.Read(0),"Only external head disconnected");Equal(1u,Links(s,at,1).FirstLink00.Read(0),"Second internal head untouched");Equal(ar|1u,al[0].Reference00.Read(0),"Internal reference survives");Equal((byte)255,al[0].Side09.Read(0),"Internal sentinel preserves link");Equal(2u,af.LinkFree04.Read(0),"Detached link returned to free head");Equal(3u,al[2].Next04.Read(0),"Freed link connects prior free list");Equal(0u,U32(a,148),"Raw polygon head also restored");Released(delegate{bOwner.RequireLive(0);},"Removed neighbor owner");
  });
  Test("Portal interval quantization preserves partial overlap rather than full edge",delegate{
   var s=State();
   var a=Serialize(0,0,0,new[]{V(0,0,0),V(0,0,4),V(1,0,4),V(1,0,0)},
    new[]{new NavigationTilePolygon(new ushort[]{0,1,2,3,65535,65535},new ushort[]{65535,65535,0x8002,65535,65535,65535})},new NavigationTileBounds(0,0,0,1,2,4));
   var b=Serialize(1,0,0,new[]{V(0,0,0),V(0,0,2),V(1,0,2),V(1,0,0)},
    new[]{new NavigationTilePolygon(new ushort[]{0,1,2,3,65535,65535},new ushort[]{0x8000,65535,65535,65535,65535,65535})},new NavigationTileBounds(1,0,1,2,2,3));
   uint ar=Added(s,new NavigationMeshTileBytes(a,a.Length));uint br=Added(s,new NavigationMeshTileBytes(b,b.Length));var al=s.Tile(Slot(s,0),0).Links20.Read(0)[0];var bl=s.Tile(Slot(s,1),0).Links20.Read(0)[0];
   Equal(br,al.Reference00.Read(0),"Partial overlap neighbor reference");Equal((byte)63,al.Min0A.Read(0),"Quarter times 255 truncates to 63");Equal((byte)191,al.Max0B.Read(0),"Three quarters times 255 truncates to 191");Equal(ar,bl.Reference00.Read(0),"Reverse reference");Equal((byte)0,bl.Min0A.Read(0),"Short edge completely covered lower");Equal((byte)255,bl.Max0B.Read(0),"Short edge completely covered upper");
  });
  Test("A portal does not fabricate a link to a closed neighboring polygon",delegate{
   var s=State();var a=PortalSquare(0,0,0,2,0x8002);var b=Triangle(1,0,0);Added(s,new NavigationMeshTileBytes(a,a.Length));Added(s,new NavigationMeshTileBytes(b,b.Length));
   Equal(UInt32.MaxValue,Links(s,Slot(s,0),0).FirstLink00.Read(0),"No matching portal on neighbor");Equal(UInt32.MaxValue,Links(s,Slot(s,1),0).FirstLink00.Read(0),"No reverse edge invented");Equal(0u,s.Tile(Slot(s,0),0).LinkFree04.Read(0),"No A link allocated");Equal(0u,s.Tile(Slot(s,1),0).LinkFree04.Read(0),"No B link allocated");
  });
  Test("Separated portal heights fail actual overlap rule without default connection",delegate{
   var s=State();var a=PortalSquare(0,0,0,2,0x8002);
   var b=Serialize(1,0,0,new[]{V(0,0,0),V(0,0,1),V(1,0,1),V(1,0,0)},new[]{new NavigationTilePolygon(new ushort[]{0,1,2,3,65535,65535},new ushort[]{0x8000,65535,65535,65535,65535,65535})},new NavigationTileBounds(1,3,0,2,5,1));
   Added(s,new NavigationMeshTileBytes(a,a.Length));Added(s,new NavigationMeshTileBytes(b,b.Length));
   Equal(UInt32.MaxValue,Links(s,Slot(s,0),0).FirstLink00.Read(0),"Height gap three exceeds twice explicit climb one");Equal(UInt32.MaxValue,Links(s,Slot(s,1),0).FirstLink00.Read(0),"Reverse also rejects height gap");
  });
  Test("Remove own lifetime stop keeps unlink prefix and does not recycle target",delegate{
   var s=State();var a=PortalSquare(0,0,0,2,0x8002);var b=PortalSquare(1,0,0,0,0x8000);var ownerA=new NavigationMeshTileBytes(a,a.Length);var ownerB=new NavigationMeshTileBytes(b,b.Length);
   Added(s,ownerA);uint br=Added(s,ownerB);var target=Slot(s,1);var free=s.Free48.Read(0);uint previousHead=Links(s,Slot(s,0),0).FirstLink00.Read(0);
   // Explicit own-adapter invalidation, not a claim about native exception
   // recovery. The target bucket is unlinked before the surviving link write.
   ownerA.MarkReleased();Unsupported(0x266d65,NavigationMeshTiles.Remove(s,br),"Released surviving owner at first-link write");Equal(0u,NavigationMeshTiles.Lookup(s,1,0,0).Reference,"Bucket unlink not rolled back");Same(ownerB,s.Tile(target,0).Data50.Read(0),"Target data not yet freed");Same(free,s.Free48.Read(0),"Target not recycled after own stop");Equal(1u,target.Salt00.Read(0),"Salt not advanced prematurely");Equal(previousHead,Links(s,Slot(s,0),0).FirstLink00.Read(0),"Failed write preserves previous typed head");ownerB.RequireLive(0);
  });
 }
}

public static class NavigationMeshTilesTestProgram {
 public static int Main(string[] args){try{
  if(args.Length!=0)throw new ArgumentException("No arguments: new pure-memory mesh tile fixtures only");
  NavigationMeshTilesTests.Run();Console.WriteLine("PASS "+NavigationMeshTilesTests.Scenarios+" scenarios, "+NavigationMeshTilesTests.Assertions+" assertions; own serialized geometry and recovered tile/link contracts, no native runtime or gameplay acceptance");return 0;
 }catch(Exception error){Console.Error.WriteLine("FAIL "+error);return 1;}}
}
