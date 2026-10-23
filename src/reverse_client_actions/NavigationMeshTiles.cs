using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
namespace GrimDynasty.ReverseClient.Actions {
 public enum NavigationMeshTileOutcome { NativeSuccess, NativeFailure, OwnUnsupported }
 public sealed class NavigationMeshTileResult {
  public readonly NavigationMeshTileOutcome Outcome; public readonly uint? NativeStatus;
  public readonly uint Reference,StopRva; public readonly string Detail;
  internal NavigationMeshTileResult(NavigationMeshTileOutcome o,uint? status,uint reference,uint at,string detail){Outcome=o;NativeStatus=status;Reference=reference;StopRva=at;Detail=detail;}
 }
 public sealed class NavigationMeshLink12:NavigationView {
  public readonly Field<uint> Reference00=new Field<uint>(),Next04=new Field<uint>();
  public readonly Field<byte> Edge08=new Field<byte>(),Side09=new Field<byte>(),Min0A=new Field<byte>(),Max0B=new Field<byte>();
  internal NavigationMeshTileBytes Owner; internal int Offset;
 }
 public sealed class NavigationMeshPolyLinks:NavigationView {
  public readonly NavigationPoly Identity; public readonly Field<uint> FirstLink00=new Field<uint>();
  public readonly Field<ushort>[] Neighbors10=new Field<ushort>[6];
  internal NavigationMeshTileBytes Owner; internal int Offset;
  internal NavigationMeshPolyLinks(NavigationPoly p){Identity=p;for(int n=0;n<6;n++)Neighbors10[n]=new Field<ushort>();}
 }
 public sealed class NavigationMeshHeaderFields:NavigationView {
  public readonly NavigationTileHeader Identity;
  public readonly Field<int> Layer10=new Field<int>(),VertexCount1C=new Field<int>(),MaxLinks20=new Field<int>(),DetailCount24=new Field<int>(),DetailVertexCount28=new Field<int>(),TriangleCount2C=new Field<int>(),OffMeshCount34=new Field<int>(),OffMeshBase38=new Field<int>();
  public readonly Field<float> WalkHeight3C=new Field<float>(),WalkRadius40=new Field<float>();
  internal NavigationMeshHeaderFields(NavigationTileHeader h){Identity=h;}
 }
 public sealed class NavigationMeshTileFields:NavigationView {
  public readonly NavigationTile Identity;
  public readonly Field<uint> LinkFree04=new Field<uint>(),Flags5C=new Field<uint>();
  public readonly Field<NavigationMeshLink12[]> Links20=new Field<NavigationMeshLink12[]>();
  public readonly Field<NavigationMeshTileBytes> Data50=new Field<NavigationMeshTileBytes>();
  public readonly Field<int> DataSize58=new Field<int>();
  // The cold domain has no offmesh records. A known null view denotes the
  // zero-length section, not a synthetic native pointer/address.
  public readonly Field<object> OffMesh48=new Field<object>();
  internal NavigationMeshTileFields(NavigationTile t){Identity=t;}
 }
 public sealed class NavigationMeshTileBytes:NavigationView {
  public readonly byte[] Bytes; public readonly int Length;
  internal NavigationTileHeader Header; internal NavigationMeshHeaderFields HeaderFields;
  internal NavigationPoly[] Polys; internal NavigationMeshPolyLinks[] PolyLinks;
  internal NavigationPoint3[] Vertices,DetailVertices; internal NavigationMeshLink12[] Links;
  internal NavigationDetail12[] Details; internal NavigationTriangle4[] Triangles;
  internal bool Materialized;
  public NavigationMeshTileBytes(byte[] bytes,int length){Bytes=bytes;Length=length;}
  internal void Extent(int offset,int count,uint at){RequireLive(at);if(Bytes==null||Length<0||Length>Bytes.Length||offset<0||count<0||(long)offset+count>Length)throw new MissingContract(at,"Outside declared own tile-byte extent");}
  internal byte B(int offset,uint at){Extent(offset,1,at);return Bytes[offset];}
  internal ushort U16(int offset,uint at){Extent(offset,2,at);return (ushort)(Bytes[offset]|Bytes[offset+1]<<8);}
  internal uint U32(int offset,uint at){Extent(offset,4,at);return (uint)(Bytes[offset]|Bytes[offset+1]<<8|Bytes[offset+2]<<16|Bytes[offset+3]<<24);}
  internal int I32(int offset,uint at){return unchecked((int)U32(offset,at));}
  internal float F32(int offset,uint at){return BitConverter.ToSingle(BitConverter.GetBytes(U32(offset,at)),0);}
  internal void W32(int offset,uint value,uint at){Extent(offset,4,at);for(int n=0;n<4;n++)Bytes[offset+n]=(byte)(value>>(8*n));}
  internal void W8(int offset,byte value,uint at){Extent(offset,1,at);Bytes[offset]=value;}
  internal void ReleaseViews(){MarkReleased();if(Header!=null)Header.MarkReleased();if(HeaderFields!=null)HeaderFields.MarkReleased();if(Polys!=null)foreach(var p in Polys)p.MarkReleased();if(PolyLinks!=null)foreach(var p in PolyLinks)p.MarkReleased();if(Vertices!=null)foreach(var p in Vertices)p.MarkReleased();if(Links!=null)foreach(var p in Links)p.MarkReleased();if(Details!=null)foreach(var p in Details)p.MarkReleased();if(DetailVertices!=null)foreach(var p in DetailVertices)p.MarkReleased();if(Triangles!=null)foreach(var p in Triangles)p.MarkReleased();}
 }
 // The existing query reads these exact Mesh/Spatial identities. No second
 // mesh or after-the-fact query synchronization is created. Geometry/header
 // bytes and their typed fields are stable for a registered tile's lifetime;
 // arbitrary external byte writes, partial aliases, concurrent mutation and
 // replacing public Field cells behind this adapter are outside its domain.
 // Link and first-link mutations below update the same retained byte array and
 // the same typed link cells. This is an OWN managed-address/allocation layer.
 public sealed class NavigationMeshTilesState:NavigationView {
  public readonly NavigationMesh Mesh; public readonly NavigationSpatialState Spatial;
  public readonly NavigationMeshSpatial MeshSpatial;
  public readonly Field<NavigationTile> Free48=new Field<NavigationTile>();
  public readonly Field<float> OriginY20=new Field<float>(); public readonly Field<int> BucketCount34=new Field<int>();
  public readonly Field<float> One34BB44=new Field<float>(),ByteScale34BD0C=new Field<float>();
  public readonly Field<float> EdgeTolerance34B9E0=new Field<float>(),ClimbScale34BBD8=new Field<float>();
  public readonly Field<uint> AbsMask34C410=new Field<uint>();
  public readonly uint InitializationStatus;
  readonly Dictionary<NavigationTile,NavigationMeshTileFields> tiles=new Dictionary<NavigationTile,NavigationMeshTileFields>();
  readonly Dictionary<NavigationPoly,NavigationMeshPolyLinks> polys=new Dictionary<NavigationPoly,NavigationMeshPolyLinks>();
  readonly Dictionary<NavigationTileHeader,NavigationMeshHeaderFields> headers=new Dictionary<NavigationTileHeader,NavigationMeshHeaderFields>();
  internal readonly Dictionary<NavigationTile,int> Indices=new Dictionary<NavigationTile,int>();
  internal readonly Dictionary<byte[],NavigationMeshTileBytes> ByteOwners=new Dictionary<byte[],NavigationMeshTileBytes>();
  NavigationMeshTilesState(NavigationSpatialState spatial){Spatial=spatial;Mesh=new NavigationMesh();MeshSpatial=new NavigationMeshSpatial(Mesh);Spatial.Attach(MeshSpatial);InitializationStatus=0x40000000;}
  // Explicit own constants from saved file-backed bytes:34BB44 in
  // offline-actor-remaining-dependencies/inspection.json and34BD0C in
  // r1-mesh-link-quantization/inspection.json. Loaded writable state is not
  // inferred; callers choose this disk-observed mathematical configuration.
  public void UseObservedLinkConstants(){One34BB44.Write(BitConverter.ToSingle(BitConverter.GetBytes(0x3f800000U),0));ByteScale34BD0C.Write(BitConverter.ToSingle(BitConverter.GetBytes(0x437f0000U),0));EdgeTolerance34B9E0.Write(BitConverter.ToSingle(BitConverter.GetBytes(0x3c23d70aU),0));ClimbScale34BBD8.Write(BitConverter.ToSingle(BitConverter.GetBytes(0x40000000U),0));AbsMask34C410.Write(0x7fffffffU);}
  public NavigationMeshTileFields Tile(NavigationTile identity,uint at){RequireLive(at);if(identity==null)throw new MissingContract(at,"Null mesh tile");identity.RequireLive(at);NavigationMeshTileFields v;if(!tiles.TryGetValue(identity,out v))throw new MissingContract(at,"Tile has no owned mesh field view");v.RequireLive(at);return v;}
  public NavigationMeshPolyLinks Poly(NavigationPoly identity,uint at){if(identity==null)throw new MissingContract(at,"Null mesh polygon");identity.RequireLive(at);NavigationMeshPolyLinks v;if(!polys.TryGetValue(identity,out v))throw new MissingContract(at,"Polygon has no link view");v.RequireLive(at);return v;}
  public NavigationMeshHeaderFields Header(NavigationTileHeader identity,uint at){if(identity==null)throw new MissingContract(at,"Null tile header");identity.RequireLive(at);NavigationMeshHeaderFields v;if(!headers.TryGetValue(identity,out v))throw new MissingContract(at,"Header has no mesh view");v.RequireLive(at);return v;}
  internal void Attach(NavigationMeshHeaderFields h){headers.Add(h.Identity,h);}
  internal void Attach(NavigationMeshPolyLinks p){polys.Add(p.Identity,p);}
  static int Pow2(int n){int v=1;while(v<n)v<<=1;return v;}
  static uint Bits(int n){uint b=0;int p=1;while(p<n){p<<=1;b++;}return b;}
  // 2664D0 positive cold domain, successful managed allocations only. The
  // explicit bounds are OWN budgets; allocator null/failure ABI is not modeled.
  // maxTiles4096/maxPolys1024 gives native (tile,poly,salt) bits(12,10,10).
  public static NavigationMeshTilesState Create(float originX,float originY,float originZ,float tileWidth,float tileDepth,int maxTiles,int maxPolys,NavigationSpatialState spatial){
   if(spatial==null)throw new ArgumentNullException("spatial");
   if(maxTiles<1||maxTiles>4096||maxPolys<1||maxPolys>1024)throw new ArgumentOutOfRangeException("Own positive mesh capacity domain");
   var s=new NavigationMeshTilesState(spatial);s.MeshSpatial.OriginX1C.Write(originX);s.OriginY20.Write(originY);s.MeshSpatial.OriginZ24.Write(originZ);s.MeshSpatial.TileWidth28.Write(tileWidth);s.MeshSpatial.TileDepth2C.Write(tileDepth);s.Mesh.TileLimit30.Write((uint)maxTiles);
   int buckets=Pow2(maxTiles/4);s.BucketCount34.Write(buckets);s.MeshSpatial.HashMask38.Write(buckets-1);
   var array=new NavigationTile[maxTiles];for(int n=0;n<array.Length;n++)array[n]=new NavigationTile();s.Mesh.Tiles50.Write(array);s.MeshSpatial.Buckets40.Write(new NavigationTile[buckets]);s.Free48.Write(null);
   for(int n=0;n<array.Length;n++){var tile=array[n];var f=new NavigationMeshTileFields(tile);var t=new NavigationTileSpatial(tile);s.tiles.Add(tile,f);s.Indices.Add(tile,n);s.Spatial.Attach(t);t.BindArrayLocation(array,n);tile.Salt00.Write(0);tile.Header08.Write(null);tile.Polys10.Write(null);tile.Vertices18.Write(null);f.LinkFree04.Write(0);f.Links20.Write(null);f.OffMesh48.Write(null);f.Data50.Write(null);f.DataSize58.Write(0);f.Flags5C.Write(0);t.Next60.Write(null);t.Details28.Write(null);t.DetailVertices30.Write(null);t.TriangleRecords38.Write(null);t.BVTree40.Write(null);}
   for(int n=array.Length-1;n>=0;n--){array[n].Salt00.Write(1);s.Spatial.Tile(array[n],0x266602).Next60.Write(s.Free48.Read(0x2665f6));s.Free48.Write(array[n]);}
   uint tb=Bits(maxTiles),pb=Bits(maxPolys);s.Mesh.TileBits5C.Write(tb);s.Mesh.PolyBits60.Write(pb);s.Mesh.SaltBits58.Write(Math.Min(31U,32U-tb-pb));return s;
  }
 }
 public static class NavigationMeshTiles {
  public const uint Success=0x40000000,Invalid=0x80000008,Duplicate=0x80000000,NoFreeTile=0x80000004;
  static NavigationMeshTileResult Done(uint status,uint reference){return new NavigationMeshTileResult((status&0x80000000)!=0?NavigationMeshTileOutcome.NativeFailure:NavigationMeshTileOutcome.NativeSuccess,status,reference,0,"");}
  //268820 returns a reference, not a status word. Null NativeStatus preserves
  // that ABI; an ordinary miss is NativeSuccess with Reference=0.
  static NavigationMeshTileResult LookupDone(uint reference){return new NavigationMeshTileResult(NavigationMeshTileOutcome.NativeSuccess,null,reference,0,"");}
  static NavigationMeshTileResult Unsupported(MissingContract e){return new NavigationMeshTileResult(NavigationMeshTileOutcome.OwnUnsupported,null,0,e.Address,e.Message);}
  static uint Mask(uint bits){return unchecked((1U<<(int)(bits&31))-1);}
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null owned mesh view");var v=value as NavigationView;if(v!=null)v.RequireLive(at);return value;}
  static T At<T>(T[] values,int index,uint at)where T:class{if(values==null||index<0||index>=values.Length)throw new MissingContract(at,"Outside owned mesh array extent");return Need(values[index],at);}
  static int Hash(int x,int z,int mask,uint at){long h=unchecked((int)(unchecked(x*(int)0x8da6b343)-unchecked(z*0x27e9c7bf)));long b=h&(long)mask;if(b<0||b>Int32.MaxValue)throw new MissingContract(at,"Own signed hash extent");return (int)b;}
  static NavigationTile[] Buckets(NavigationMeshTilesState s,uint at){return Need(s.Spatial.Mesh(s.Mesh,at).Buckets40.Read(at),at);}
  static NavigationTile Bucket(NavigationTile[] values,int index,uint at){if(index<0||index>=values.Length)throw new MissingContract(at,"Outside bucket extent");return values[index];}
  static void Budget(NavigationMeshTilesState s,int step,uint at){if(step>s.Spatial.OwnMaxBucketLinks)throw new MissingContract(at,"Own bounded chain/record traversal; no native cycle normalization");}
  static uint Ref(NavigationMeshTilesState s,NavigationTile tile,uint baseAt,uint polyAt,uint saltAt,uint tileAt,uint polyAgainAt){
   var array=Need(s.Mesh.Tiles50.Read(baseAt),baseAt);int index;if(!s.Indices.TryGetValue(tile,out index)||index>=array.Length||!Object.ReferenceEquals(array[index],tile))throw new MissingContract(baseAt,"Unknown tile/base address identity");
   uint pb=s.Mesh.PolyBits60.Read(polyAt);uint salt=tile.Salt00.Read(saltAt);uint tb=s.Mesh.TileBits5C.Read(tileAt);uint again=s.Mesh.PolyBits60.Read(polyAgainAt);return unchecked(((uint)index<<(int)(pb&31))|(salt<<(int)((tb+again)&31)));
  }
  static uint CapturedRef(NavigationMeshTilesState s,NavigationTile tile,uint polyAt,uint baseAt,uint saltAt,uint tileAt){
   uint pb=s.Mesh.PolyBits60.Read(polyAt);var array=Need(s.Mesh.Tiles50.Read(baseAt),baseAt);int index;if(!s.Indices.TryGetValue(tile,out index)||index>=array.Length||!Object.ReferenceEquals(array[index],tile))throw new MissingContract(baseAt,"Unknown captured tile/base identity");uint salt=tile.Salt00.Read(saltAt);uint tb=s.Mesh.TileBits5C.Read(tileAt);return unchecked(((uint)index<<(int)(pb&31))|(salt<<(int)((pb+tb)&31)));
  }
  public static NavigationMeshTileResult Lookup(NavigationMeshTilesState s,int x,int z,int layer){try{Need(s,0x268839);int hash=Hash(x,z,s.MeshSpatial.HashMask38.Read(0x268839),0x268840);var buckets=Buckets(s,0x268843);var tile=Bucket(buckets,hash,0x268847);int steps=0;while(tile!=null){Budget(s,++steps,0x268850);Need(tile,0x268850);var h=tile.Header08.Read(0x268850);if(h!=null){var hs=s.Spatial.Header(h,0x268859);if(hs.GridX08.Read(0x268859)==x&&hs.GridZ0C.Read(0x26885e)==z&&s.Header(h,0x268864).Layer10.Read(0x268864)==layer)return LookupDone(Ref(s,tile,0x268888,0x26888f,0x2688a0,0x2688a5,0x2688a8));}tile=s.Spatial.Tile(tile,0x26886a).Next60.Read(0x26886a);}return LookupDone(0);}catch(MissingContract e){return Unsupported(e);}}
  static void Point(NavigationPoint3 p,NavigationMeshTileBytes data,int offset){p.X.Write(data.F32(offset,0));p.Y.Write(data.F32(offset+4,0));p.Z.Write(data.F32(offset+8,0));}
  static int Count(int value,int maximum){if(value<0||value>maximum)throw new MissingContract(0,"Own cold record-count extent");return value;}
  // This finite, stable byte-to-typed projection is an adapter precondition,
  // not a newly inferred native parser/validation branch. Native publication
  // may already have happened when malformed data reaches this own stop.
  static void Materialize(NavigationMeshTilesState s,NavigationMeshTileBytes d){
   if(d.Materialized)throw new MissingContract(0,"A tile data identity cannot be registered twice or reused after release");d.Extent(0,100,0);
   if(s.ByteOwners.ContainsKey(d.Bytes))throw new MissingContract(0,"Own exclusive tile-byte identity already registered, including released data; no overlapping aliases");
   int np=Count(d.I32(0x18,0),1024),nv=Count(d.I32(0x1c,0),65534),nl=Count(d.I32(0x20,0),65536),nd=Count(d.I32(0x24,0),1024),ndv=Count(d.I32(0x28,0),65536),nt=Count(d.I32(0x2c,0),65536);
   if(d.I32(0x30,0)!=0)throw new MissingContract(0,"Own cold BV0 domain; native BV layout not materialized here");
   long total=100L+12L*nv+32L*np+12L*nl+12L*nd+12L*ndv+4L*nt;if(total>Int32.MaxValue)throw new MissingContract(0,"Own byte budget");d.Extent(0,(int)total,0);
   var h=new NavigationTileHeader();h.PolyCount18.Write((uint)np);var hf=new NavigationMeshHeaderFields(h);hf.Layer10.Write(d.I32(0x10,0));hf.VertexCount1C.Write(nv);hf.MaxLinks20.Write(nl);hf.DetailCount24.Write(nd);hf.DetailVertexCount28.Write(ndv);hf.TriangleCount2C.Write(nt);hf.OffMeshCount34.Write(d.I32(0x34,0));hf.OffMeshBase38.Write(d.I32(0x38,0));hf.WalkHeight3C.Write(d.F32(0x3c,0));hf.WalkRadius40.Write(d.F32(0x40,0));
   var hs=new NavigationHeaderSpatial(h);hs.GridX08.Write(d.I32(8,0));hs.GridZ0C.Write(d.I32(12,0));hs.BvNodeCount30.Write(0);hs.Climb44.Write(d.F32(0x44,0));hs.BvQuantFactor60.Write(d.F32(0x60,0));Point(hs.BoundsMin48,d,0x48);Point(hs.BoundsMax54,d,0x54);s.Spatial.Attach(hs);s.Attach(hf);d.Header=h;d.HeaderFields=hf;
   int cursor=100;d.Vertices=new NavigationPoint3[nv];for(int n=0;n<nv;n++){var p=new NavigationPoint3();Point(p,d,cursor+n*12);d.Vertices[n]=p;}cursor+=nv*12;
   d.Polys=new NavigationPoly[np];d.PolyLinks=new NavigationMeshPolyLinks[np];for(int n=0;n<np;n++){int at=cursor+n*32;var p=new NavigationPoly();var ps=new NavigationPolySpatial(p);var pl=new NavigationMeshPolyLinks(p);pl.Owner=d;pl.Offset=at;pl.FirstLink00.Write(d.U32(at,0));for(int k=0;k<6;k++){p.Indices04[k].Write(d.U16(at+4+k*2,0));pl.Neighbors10[k].Write(d.U16(at+16+k*2,0));}p.VertexCount1E.Write(d.B(at+30,0));ps.Flags1C.Write(d.U16(at+28,0));ps.Type1F.Write(d.B(at+31,0));s.Spatial.Attach(ps);s.Attach(pl);d.Polys[n]=p;d.PolyLinks[n]=pl;}cursor+=np*32;
   d.Links=new NavigationMeshLink12[nl];for(int n=0;n<nl;n++){int at=cursor+n*12;var l=new NavigationMeshLink12();l.Owner=d;l.Offset=at;l.Reference00.Write(d.U32(at,0));l.Next04.Write(d.U32(at+4,0));l.Edge08.Write(d.B(at+8,0));l.Side09.Write(d.B(at+9,0));l.Min0A.Write(d.B(at+10,0));l.Max0B.Write(d.B(at+11,0));d.Links[n]=l;}cursor+=nl*12;
   d.Details=new NavigationDetail12[nd];for(int n=0;n<nd;n++){int at=cursor+n*12;var v=new NavigationDetail12();v.VertexBase00.Write(d.U32(at,0));v.TriangleBase04.Write(d.U32(at+4,0));v.TriangleCount09.Write(d.B(at+9,0));d.Details[n]=v;}cursor+=nd*12;
   d.DetailVertices=new NavigationPoint3[ndv];for(int n=0;n<ndv;n++){var v=new NavigationPoint3();Point(v,d,cursor+n*12);d.DetailVertices[n]=v;}cursor+=ndv*12;
   d.Triangles=new NavigationTriangle4[nt];for(int n=0;n<nt;n++){int at=cursor+n*4;var v=new NavigationTriangle4();v.A00.Write(d.B(at,0));v.B01.Write(d.B(at+1,0));v.C02.Write(d.B(at+2,0));d.Triangles[n]=v;}d.Materialized=true;s.ByteOwners.Add(d.Bytes,d);
  }
  static void First(NavigationMeshPolyLinks p,uint value,uint at){p.RequireLive(at);p.Owner.W32(p.Offset,value,at);p.FirstLink00.Write(value);}
  static void Link32(NavigationMeshLink12 p,int field,uint value,uint at){p.RequireLive(at);p.Owner.W32(p.Offset+field,value,at);(field==0?p.Reference00:p.Next04).Write(value);}
  static void Link8(NavigationMeshLink12 p,int field,byte value,uint at){p.RequireLive(at);p.Owner.W8(p.Offset+field,value,at);(field==8?p.Edge08:field==9?p.Side09:field==10?p.Min0A:p.Max0B).Write(value);}
  static NavigationMeshLink12 Link(NavigationMeshTilesState s,NavigationTile tile,uint index,uint at){return At(s.Tile(tile,at).Links20.Read(at),unchecked((int)index),at);}
  static NavigationPoly PolyAt(NavigationTile tile,int index,uint at){return At(Need(tile,at).Polys10.Read(at),index,at);}
  static int PolyCount(NavigationMeshTilesState s,NavigationTile tile,uint at){return unchecked((int)Need(tile.Header08.Read(at),at).PolyCount18.Read(at));}
  static void InternalLinks(NavigationMeshTilesState s,NavigationTile tile){
   if(tile==null)return;uint basis=CapturedRef(s,tile,0x267341,0x267347,0x267375,0x267379);var tf=s.Tile(tile,0x267381);
   for(int n=0;n<PolyCount(s,tile,n==0?0x267385U:0x267432U);n++){Budget(s,n+1,0x2673a0);var p=PolyAt(tile,n,0x2673a0);var pl=s.Poly(p,0x2673a7);byte type=s.Spatial.Poly(p,0x2673a7).Type1F.Read(0x2673a7);First(pl,UInt32.MaxValue,0x2673ae);if((type&0xc0)==0x40)continue;int vc=p.VertexCount1E.Read(0x2673b9);if(vc>6)throw new MissingContract(0x2673d0,"Own six-neighbor extent");for(int k=vc-1;k>=0;k--){ushort neighbor=pl.Neighbors10[k].Read(0x2673d0);if(neighbor==0||(short)neighbor<0)continue;uint index=tf.LinkFree04.Read(0x2673dc);if(index==UInt32.MaxValue)continue;var link=Link(s,tile,index,0x2673e6);tf.LinkFree04.Write(link.Next04.Read(0x2673f2));neighbor=pl.Neighbors10[k].Read(0x2673f9);Link8(link,8,(byte)k,0x267400);Link8(link,9,255,0x267406);Link8(link,10,0,0x267406);Link32(link,0,unchecked((uint)(neighbor-1))|basis,0x26740d);Link8(link,11,0,0x267410);uint old=pl.FirstLink00.Read(0x267415);Link32(link,4,old,0x267418);First(pl,index,0x26741c);}}
  }
  static void BaseOffMesh(NavigationMeshTilesState s,NavigationTile tile){if(tile==null)return;CapturedRef(s,tile,0x26746a,0x267483,0x26749b,0x2674a0);var h=Need(tile.Header08.Read(0x2674a9),0x2674ad);if(s.Header(h,0x2674ad).OffMeshCount34.Read(0x2674ad)>0)throw new MissingContract(0x2674b6,"Offmesh positive path and267BA0 outside cold domain");}
  static void ExternalOffMesh(NavigationMeshTilesState s,NavigationTile tile,NavigationTile other,int side){if(tile==null)return;var h=Need(Need(other,0x26710d).Header08.Read(0x26710d),0x267114);if(s.Header(h,0x267114).OffMeshCount34.Read(0x267114)>0)throw new MissingContract(0x26711e,"Offmesh positive path/267BA0/268C20 outside cold domain");}
  static List<NavigationTile> SameCoordinate(NavigationMeshTilesState s,int x,int z,uint at){int hash=Hash(x,z,s.MeshSpatial.HashMask38.Read(at),at);var tile=Bucket(Buckets(s,at),hash,at);var result=new List<NavigationTile>();int steps=0;while(tile!=null){Budget(s,++steps,at);var h=Need(tile,at).Header08.Read(at);if(h!=null){var hs=s.Spatial.Header(h,at);if(hs.GridX08.Read(at)==x&&hs.GridZ0C.Read(at)==z&&result.Count<32)result.Add(tile);}tile=s.Spatial.Tile(tile,at).Next60.Read(at);}return result;}
  static List<NavigationTile> NeighborCoordinate(NavigationMeshTilesState s,int x,int z,int direction){
   // Rea2686C0 recovered switch; assembly confirms the arithmetic and calls.
   // Not a guessed direction table or an attempt to disassemble its data.
   unchecked{switch(direction){case 0:x++;break;case 1:x++;z++;break;case 2:z++;break;case 3:x--;z++;break;case 4:x--;break;case 5:x--;z--;break;case 6:z--;break;case 7:x++;z--;break;}}
   int hash=Hash(x,z,s.MeshSpatial.HashMask38.Read(0x2687bf),0x2687ca);var buckets=Buckets(s,0x2687c3);var tile=Bucket(buckets,hash,0x2687cd);var found=new List<NavigationTile>();int steps=0;
   while(tile!=null){Budget(s,++steps,0x2687e0);var h=Need(tile,0x2687e0).Header08.Read(0x2687e0);if(h!=null){var hs=s.Spatial.Header(h,0x2687e9);if(hs.GridX08.Read(0x2687e9)==x&&hs.GridZ0C.Read(0x2687ee)==z&&found.Count<32)found.Add(tile);}tile=s.Spatial.Tile(tile,0x268803).Next60.Read(0x268803);}return found;
  }
  // Explicit own IEEE binary32, nearest-even and masked exception domain.
  // Scalar MINSS/MAXSS use their second operand on NaN or equality. Every
  // arithmetic result is separately rounded; no FMA or double-only geometry.
  // DAZ/FTZ have not been observed: subnormal inputs/intermediates stop as an
  // OWN limitation. NaN/Infinity still follow recovered COMISS branches.
  static float Scalar(float x,uint at){uint b=BitConverter.ToUInt32(BitConverter.GetBytes(x),0);if((b&0x7f800000U)==0&&(b&0x7fffffU)!=0)throw new MissingContract(at,"Own floating domain excludes subnormal input; native MXCSR unobserved");return x;}
  [MethodImpl(MethodImplOptions.NoInlining)]
  static float Round(double x,uint at){if(x!=0&&!Double.IsNaN(x)&&!Double.IsInfinity(x)&&Math.Abs(x)<1.1754943508222875E-38)throw new MissingContract(at,"Own floating domain excludes a subnormal intermediate");return Scalar(BitConverter.ToSingle(BitConverter.GetBytes((float)x),0),at);}
  static float AddF(float a,float b,uint at){return Round((double)a+b,at);} static float SubF(float a,float b,uint at){return Round((double)a-b,at);} static float MulF(float a,float b,uint at){return Round((double)a*b,at);} static float DivF(float a,float b,uint at){return Round((double)a/b,at);}
  static float ReadF(Field<float> f,uint at){return Scalar(f.Read(at),at);}
  static float Coord(NavigationPoint3 p,int component,uint at){Need(p,at);return ReadF(component==0?p.X:component==1?p.Y:p.Z,at);}
  static float MinS(float a,float b){return a<b?a:b;} static float MaxS(float a,float b){return a>b?a:b;}
  sealed class EdgeMatch {internal uint Reference;internal float Min,Max;}
  static List<EdgeMatch> MatchEdges(NavigationMeshTilesState s,NavigationPoint3 start,NavigationPoint3 end,NavigationTile other,int direction){
   if(direction!=0&&direction!=2&&direction!=4&&direction!=6)throw new MissingContract(0x2667c1,"Own cardinal cold-edge domain; noncardinal native stack-derived floats are not invented");
   bool vertical=(direction&~4)==0;int axis=vertical?2:0,planeAxis=vertical?0:2;
   float a0=Coord(start,axis,vertical?0x2667e9U:0x2667a1U),a1=Coord(end,axis,vertical?0x2667eeU:0x2667a5U),lowA,highA,lowAY,highAY;
   if(a0>=a1){highAY=Coord(start,1,0x2667f6);lowA=a1;lowAY=Coord(end,1,0x2667ff);highA=a0;}else{lowAY=Coord(start,1,0x2667b0);lowA=a0;highAY=Coord(end,1,0x2667b9);highA=a1;} //JNC: unordered takes else.
   float plane=Coord(start,planeAxis,vertical?0x26683cU:0x266835U);uint basis=Ref(s,other,0x266852,0x266869,0x26687f,0x266884,0x266888);
   int count=PolyCount(s,other,0x266899);var result=new List<EdgeMatch>();if(count<=0)return result;
   var polygons=Need(other.Polys10.Read(0x2668db),0x2668db);float epsilon=ReadF(s.EdgeTolerance34B9E0,0x26693d);
   for(int n=0;n<count;n++){
    Budget(s,n+1,0x266950);var p=At(polygons,n,0x266950);var pl=s.Poly(p,0x266950);int vc=p.VertexCount1E.Read(0x266950);if(vc>6)throw new MissingContract(0x266970,"Own six-neighbor extent");
    for(int k=0;k<vc;k++){
     if(pl.Neighbors10[k].Read(0x266970)!=(ushort)(direction|0x8000))continue;
     ushort index0=p.Indices04[k].Read(0x26697b);var vertices=Need(other.Vertices18.Read(0x26697f),0x26697f);var b0p=At(vertices,index0,0x266989);var b1p=At(vertices,p.Indices04[(k+1)%vc].Read(0x266992),0x26699b);
     float otherPlane=Coord(b0p,planeAxis,vertical?0x2669c1U:0x2669b8U),delta=SubF(plane,otherPlane,0x2669c7);uint bits=BitConverter.ToUInt32(BitConverter.GetBytes(delta),0)&s.AbsMask34C410.Read(0x2669cb);float absolute=BitConverter.ToSingle(BitConverter.GetBytes(bits),0);if(absolute>epsilon)continue; //JA, unordered proceeds.
     float b0=Coord(b0p,axis,vertical?0x2669feU:0x2669f0U),b1=Coord(b1p,axis,vertical?0x266a05U:0x2669f6U),lowB,highB,lowBY,highBY;
     if(b0>=b1){highBY=Coord(b0p,1,0x266a25);lowB=b1;lowBY=Coord(b1p,1,0x266a30);highB=b0;}else{highBY=Coord(b1p,1,0x266a11);lowB=b0;lowBY=Coord(b0p,1,0x266a1c);highB=b1;}
     float aLowTrim=AddF(lowA,epsilon,0x266a42),bLowTrim=AddF(lowB,epsilon,0x266a4b),bHighTrim=SubF(highB,epsilon,0x266a50);
     float overlapLow=MaxS(bLowTrim,aLowTrim),aHighTrim=SubF(highA,epsilon,0x266a5d),overlapHigh=MinS(bHighTrim,aHighTrim);if(overlapLow>overlapHigh)continue;
     float riseA=SubF(highAY,lowAY,0x266a7c),riseB=SubF(highBY,lowBY,0x266a84),runA=SubF(highA,lowA,0x266a8d);
     float slopeA=DivF(riseA,runA,0x266a9d),productA=MulF(lowA,slopeA,0x266aa4),runB=SubF(highB,lowB,0x266aa8),interceptA=SubF(lowAY,productA,0x266aad),slopeB=DivF(riseB,runB,0x266ab5);
     float aAtHigh=MulF(slopeA,overlapHigh,0x266abc),productB=MulF(lowB,slopeB,0x266ac1),aAtLow=MulF(slopeA,overlapLow,0x266ac5);aAtHigh=AddF(aAtHigh,interceptA,0x266ac9);
     float interceptB=SubF(lowBY,productB,0x266acd),bAtHigh=MulF(slopeB,overlapHigh,0x266ad4);aAtLow=AddF(aAtLow,interceptA,0x266ad9);float bAtLow=MulF(slopeB,overlapLow,0x266add);bAtHigh=AddF(bAtHigh,interceptB,0x266ae1);bAtLow=AddF(bAtLow,interceptB,0x266ae5);
     float differenceHigh=SubF(bAtHigh,aAtHigh,0x266ae9),differenceLow=SubF(bAtLow,aAtLow,0x266aed),product=MulF(differenceHigh,differenceLow,0x266af4);
     bool accepted=!(product>=0); //JC: negative OR unordered.
     if(!accepted){var h=Need(other.Header08.Read(0x266afe),0x266b06);float lowSquared=MulF(differenceLow,differenceLow,0x266b02),climb=ReadF(s.Spatial.Header(h,0x266b06).Climb44,0x266b06);climb=MulF(climb,ReadF(s.ClimbScale34BBD8,0x266b0b),0x266b0b);float limit=MulF(climb,climb,0x266b13);accepted=!(lowSquared>limit);if(!accepted){float highSquared=MulF(differenceHigh,differenceHigh,0x266b1c);accepted=!(highSquared>limit);}}
     if(!accepted)continue;
     if(result.Count<4){var match=new EdgeMatch();match.Min=MaxS(lowB,lowA);match.Max=MinS(highB,highA);match.Reference=basis|(uint)n;result.Add(match);}break; //Break even once four outputs are full.
    }
   }
   return result;
  }
  static void ExternalLinks(NavigationMeshTilesState s,NavigationTile tile,NavigationTile other,int direction){
   if(tile==null)return;int count=PolyCount(s,tile,0x266e06);if(count<=0)return;float one=ReadF(s.One34BB44,0x266e5c),scale=ReadF(s.ByteScale34BD0C,0x266e6d);
   for(int n=0;n<count;n++){Budget(s,n+1,0x266e80);var p=PolyAt(tile,n,0x266e80);var pl=s.Poly(p,0x266e86);int vc=p.VertexCount1E.Read(0x266e86);if(vc>6)throw new MissingContract(0x266ea0,"Own six-neighbor extent");for(int k=0;k<vc;k++){
    ushort edge=pl.Neighbors10[k].Read(0x266ea0);if((short)edge>=0||(direction!=-1&&(edge&255)!=direction))continue;ushort index0=p.Indices04[k].Read(0x266ec0);var verts=Need(tile.Vertices18.Read(0x266ec6),0x266ec6);var start=At(verts,index0,0x266ed2);var end=At(verts,p.Indices04[(k+1)%vc].Read(0x266edc),0x266ee6);int side=edge&255;
    var matches=other==null?new List<EdgeMatch>():MatchEdges(s,start,end,other,(side-4)&7);
    foreach(var match in matches){var tf=s.Tile(tile,0x266f40);uint index=tf.LinkFree04.Read(0x266f40);if(index==UInt32.MaxValue)continue;var link=Link(s,tile,index,0x266f4c);tf.LinkFree04.Write(link.Next04.Read(0x266f54));Link32(link,0,match.Reference,0x266f61);Link8(link,8,(byte)k,0x266f65);Link8(link,9,(byte)side,0x266f6a);Link32(link,4,pl.FirstLink00.Read(0x266f6f),0x266f72);First(pl,index,0x266f77);
     int axis;if((side&~4)==0)axis=2;else if(((side-2)&~4)==0)axis=0;else continue;
     float origin=Coord(start,axis,axis==2?0x266f9cU:0x266f90U),finish=Coord(end,axis,axis==2?0x266fa1U:0x266f95U),span=SubF(finish,origin,0x266fae),high=SubF(match.Max,origin,0x266fb9),low=SubF(match.Min,origin,0x266fc0),reciprocal=DivF(one,span,0x266fc4);high=MulF(high,reciprocal,0x266fc8);low=MulF(low,reciprocal,0x266fcc);if(low>high){float swap=low;low=high;high=swap;} //JBE leaves unordered unswapped.
     low=low>=0?MinS(low,one):0;bool highNonnegative=high>=0;float q=MulF(low,scale,0x266fef);Link8(link,10,Quantize(q,0x266ff4),0x266ff9);high=highNonnegative?MinS(high,one):0;q=MulF(high,scale,0x267009);Link8(link,11,Quantize(q,0x26700e),0x267013);
    }
   }count=PolyCount(s,tile,0x267065);}
  }
  static byte Quantize(float value,uint at){if(Single.IsNaN(value)||Single.IsInfinity(value)||value>=9223372036854775808.0||value< -9223372036854775808.0)return 0;return unchecked((byte)(long)Math.Truncate((double)value));} //CVTTSS2SI r64 indefinite low byte=0.
  public static NavigationMeshTileResult Add(NavigationMeshTilesState s,byte[] bytes,int length,Field<uint> outputReference){return Add(s,new NavigationMeshTileBytes(bytes,length),outputReference);}
  public static NavigationMeshTileResult Add(NavigationMeshTilesState s,NavigationMeshTileBytes data,Field<uint> outputReference){try{
   Need(s,0x268270);Need(data,0x26827d);if(data.U32(0,0x26827d)!=0x444e4156)return Done(0x80000001,0);if(data.U32(4,0x2682a0)!=7)return Done(0x80000002,0);
   int z=data.I32(12,0x2682b8),x=data.I32(8,0x2682bc),hash=Hash(x,z,s.MeshSpatial.HashMask38.Read(0x2682d0),0x2682d4);var buckets=Buckets(s,0x2682da);var cur=Bucket(buckets,hash,0x2682de);int steps=0;
   while(cur!=null){Budget(s,++steps,0x2682e7);var h=Need(cur,0x2682e7).Header08.Read(0x2682e7);if(h!=null){var hs=s.Spatial.Header(h,0x2682f0);if(hs.GridX08.Read(0x2682f0)==x&&hs.GridZ0C.Read(0x2682f6)==z&&data.I32(16,0x2682fc)==s.Header(h,0x268300).Layer10.Read(0x268300))return Done(Duplicate,0);}cur=s.Spatial.Tile(cur,0x268309).Next60.Read(0x268309);}
   var tile=s.Free48.Read(0x26831a);if(tile==null)return Done(NoFreeTile,0);var ts=s.Spatial.Tile(tile,0x268327);s.Free48.Write(ts.Next60.Read(0x268327));ts.Next60.Write(null);
   x=data.I32(8,0x26835a);z=data.I32(12,0x268362);hash=Hash(x,z,s.MeshSpatial.HashMask38.Read(0x26836a),0x268372);buckets=Buckets(s,0x268376);ts.Next60.Write(Bucket(buckets,hash,0x26837a));buckets[hash]=tile; //268386 native publication, before fields.
   Materialize(s,data);var tf=s.Tile(tile,0x2683e9);tile.Vertices18.Write(data.Vertices);tf.LinkFree04.Write(0);tile.Polys10.Write(data.Polys);tf.Links20.Write(data.Links);ts.Details28.Write(data.Details);ts.DetailVertices30.Write(data.DetailVertices);ts.TriangleRecords38.Write(data.Triangles);tf.OffMesh48.Write(null);ts.BVTree40.Write(null);
   int last=data.I32(0x20,0x268452)-1;Link32(At(data.Links,last,0x268462),4,UInt32.MaxValue,0x268462);for(int n=0;n<data.I32(0x20,0x26847e)-1;n++)Link32(At(data.Links,n,0x26848a),4,(uint)(n+1),0x26848a);
   tile.Header08.Write(data.Header);tf.Data50.Write(data);tf.DataSize58.Write(data.Length);tf.Flags5C.Write(1);
   InternalLinks(s,tile);BaseOffMesh(s,tile);
   z=data.I32(12,0x2684c1);x=data.I32(8,0x2684c8);var same=SameCoordinate(s,x,z,0x2684dc);foreach(var other in same){if(!Object.ReferenceEquals(other,tile)){ExternalLinks(s,tile,other,-1);ExternalLinks(s,other,tile,-1);}ExternalOffMesh(s,tile,other,-1);ExternalOffMesh(s,other,tile,-1);}
   for(int direction=0;direction<8;direction++){z=data.I32(12,0x268590);x=data.I32(8,0x268599);var near=NeighborCoordinate(s,x,z,direction);foreach(var other in near){ExternalLinks(s,tile,other,direction);ExternalLinks(s,other,tile,(direction-4)&7);ExternalOffMesh(s,tile,other,direction);ExternalOffMesh(s,other,tile,(direction-4)&7);}}
   uint reference=0;if(outputReference!=null){reference=Ref(s,tile,0x26865b,0x268662,0x268673,0x268677,0x26867a);outputReference.Write(reference);}return Done(Success,reference);
  }catch(MissingContract e){return Unsupported(e);}}
  static void Unconnect(NavigationMeshTilesState s,NavigationTile tile,NavigationTile removed){
   if(tile==null||removed==null)return;uint pb=s.Mesh.PolyBits60.Read(0x266c7a);var array=s.Mesh.Tiles50.Read(0x266c81);uint tb=s.Mesh.TileBits5C.Read(0x266c8f);int ri;if(array==null||!s.Indices.TryGetValue(removed,out ri)||ri>=array.Length||!Object.ReferenceEquals(array[ri],removed))throw new MissingContract(0x266c81,"Unknown removed tile address");uint salt=removed.Salt00.Read(0x266cb6);uint reference=unchecked(((uint)ri<<(int)(pb&31))|(salt<<(int)((tb+pb)&31)));uint target=(reference>>(int)(pb&31))&Mask(tb);
   for(int n=0;n<PolyCount(s,tile,n==0?0x266cdeU:0x266da2U);n++){var p=PolyAt(tile,n,0x266d00);var pl=s.Poly(p,0x266d08);uint prev=UInt32.MaxValue,index=pl.FirstLink00.Read(0x266d08);int steps=0;while(index!=UInt32.MaxValue){Budget(s,++steps,0x266d20);var link=Link(s,tile,index,0x266d20);bool remove=false;if(link.Side09.Read(0x266d2a)!=255){uint b=s.Mesh.TileBits5C.Read(0x266d3a);uint r=link.Reference00.Read(0x266d43);uint pbits=s.Mesh.PolyBits60.Read(0x266d4a);remove=((r>>(int)(pbits&31))&Mask(b))==target;}if(remove){uint next=link.Next04.Read(0x266d5a);if(prev==UInt32.MaxValue)First(pl,next,0x266d65);else Link32(Link(s,tile,prev,0x266d72),4,next,0x266d72);var tf=s.Tile(tile,0x266d77);uint free=tf.LinkFree04.Read(0x266d77);Link32(Link(s,tile,index,0x266d7a),4,free,0x266d7e);tf.LinkFree04.Write(index);index=next;}else{prev=index;index=link.Next04.Read(0x266d8e);}}}
  }
  public static NavigationMeshTileResult Remove(NavigationMeshTilesState s,uint reference){try{
   if(reference==0)return Done(Invalid,0);Need(s,0x268991);uint pb=s.Mesh.PolyBits60.Read(0x268991),tb=s.Mesh.TileBits5C.Read(0x268998);uint index=(reference>>(int)(pb&31))&Mask(tb);if(unchecked((int)index)>=unchecked((int)s.Mesh.TileLimit30.Read(0x2689bb)))return Done(Invalid,0);
   uint salt=(reference>>(int)((tb+pb)&31))&Mask(s.Mesh.SaltBits58.Read(0x2689df));var tile=At(s.Mesh.Tiles50.Read(0x2689f1),unchecked((int)index),0x2689fc);if(tile.Salt00.Read(0x2689fc)!=salt)return Done(Invalid,0);
   var h=Need(tile.Header08.Read(0x268a0b),0x268a27);var hs=s.Spatial.Header(h,0x268a27);int x=hs.GridX08.Read(0x268a27),z=hs.GridZ0C.Read(0x268a2e);var buckets=Buckets(s,0x268a45);int hash=Hash(x,z,s.MeshSpatial.HashMask38.Read(0x268a49),0x268a4c);var cur=Bucket(buckets,hash,0x268a53);NavigationTile prev=null;int steps=0;
   while(cur!=null){Budget(s,++steps,0x268a60);if(Object.ReferenceEquals(cur,tile)){var next=s.Spatial.Tile(cur,0x268a73).Next60.Read(0x268a73);if(prev!=null)s.Spatial.Tile(prev,0x268a7c).Next60.Write(next);else buckets[hash]=next;break;}prev=cur;cur=s.Spatial.Tile(cur,0x268a68).Next60.Read(0x268a68);}
   h=Need(tile.Header08.Read(0x268a85),0x268a8c);hs=s.Spatial.Header(h,0x268a8c);var same=SameCoordinate(s,hs.GridX08.Read(0x268a8c),hs.GridZ0C.Read(0x268a90),0x268aa4);foreach(var other in same)if(!Object.ReferenceEquals(other,tile))Unconnect(s,other,tile);
   for(int dir=0;dir<8;dir++){h=Need(tile.Header08.Read(0x268b20),0x268b37);hs=s.Spatial.Header(h,0x268b37);z=hs.GridZ0C.Read(0x268b37);x=hs.GridX08.Read(0x268b3b);foreach(var other in NeighborCoordinate(s,x,z,dir))Unconnect(s,other,tile);}
   var tf=s.Tile(tile,0x268b89);if((tf.Flags5C.Read(0x268b89)&1)!=0){var data=tf.Data50.Read(0x268b8f);if(data!=null)data.ReleaseViews();tf.Data50.Write(null);tf.DataSize58.Write(0);}uint old=tile.Salt00.Read(0x268ba6);tile.Header08.Write(null);tf.Flags5C.Write(0);tf.LinkFree04.Write(0);tile.Polys10.Write(null);tile.Vertices18.Write(null);tf.Links20.Write(null);var ts=s.Spatial.Tile(tile,0x268bc5);ts.Details28.Write(null);ts.DetailVertices30.Write(null);ts.TriangleRecords38.Write(null);ts.BVTree40.Write(null);tf.OffMesh48.Write(null);uint nextSalt=unchecked(old+1)&Mask(s.Mesh.SaltBits58.Read(0x268bd9));tile.Salt00.Write(nextSalt==0?1:nextSalt);ts.Next60.Write(s.Free48.Read(0x268bf0));s.Free48.Write(tile);return Done(Success,0);
  }catch(MissingContract e){return Unsupported(e);}}
 }
}
