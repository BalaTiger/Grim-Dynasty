using System;
using System.Collections.Generic;
namespace GrimDynasty.ReverseClient.Actions {
 // New sidecars preserve frozen core identities. Every native field begins
 // unknown; attaching a view does not initialize a mesh, filter or tile.
 public sealed class NavigationQueryFilter:NavigationView {
  public readonly Field<ushort> Include100=new Field<ushort>(),Exclude102=new Field<ushort>();
 }
 public sealed class NavigationMeshSpatial:NavigationView {
  public readonly NavigationMesh Identity;
  public readonly Field<float> OriginX1C=new Field<float>(),OriginZ24=new Field<float>(),TileWidth28=new Field<float>(),TileDepth2C=new Field<float>();
  public readonly Field<int> HashMask38=new Field<int>();
  public readonly Field<NavigationTile[]> Buckets40=new Field<NavigationTile[]>();
  public NavigationMeshSpatial(NavigationMesh identity){if(identity==null)throw new ArgumentNullException("identity");Identity=identity;}
 }
 public sealed class NavigationTileSpatial:NavigationView {
  public readonly NavigationTile Identity;
  public readonly Field<NavigationTile> Next60=new Field<NavigationTile>();
  public readonly Field<NavigationDetail12[]> Details28=new Field<NavigationDetail12[]>();
  public readonly Field<NavigationPoint3[]> DetailVertices30=new Field<NavigationPoint3[]>();
  public readonly Field<NavigationTriangle4[]> TriangleRecords38=new Field<NavigationTriangle4[]>();
  public readonly Field<NavigationBvNode16[]> BVTree40=new Field<NavigationBvNode16[]>();
  NavigationTile[] addressArray;int addressIndex;bool addressKnown;
  public NavigationTileSpatial(NavigationTile identity){if(identity==null)throw new ArgumentNullException("identity");Identity=identity;}
  // Declares an OWN stride68 array address. No native allocation/address is
  // fabricated. Different bases, partial aliases and duplicate identity slots
  // need another address model and stop explicitly in this supported domain.
  public void BindArrayLocation(NavigationTile[] array,int index){
   if(array==null||index<0||index>=array.Length||!Object.ReferenceEquals(array[index],Identity))throw new ArgumentException("Array location must contain this exact tile identity");
   for(int n=0;n<array.Length;n++)if(n!=index&&Object.ReferenceEquals(array[n],Identity))throw new ArgumentException("One tile identity cannot declare two addresses");
   if(addressKnown&&(!Object.ReferenceEquals(array,addressArray)||index!=addressIndex))throw new InvalidOperationException("Tile address already bound");
   addressArray=array;addressIndex=index;addressKnown=true;
  }
  internal int ArrayIndex(NavigationTile[] actualBase,uint at){
   RequireLive(at);Identity.RequireLive(at);
   if(!addressKnown||!Object.ReferenceEquals(actualBase,addressArray)||addressIndex>=actualBase.Length||!Object.ReferenceEquals(actualBase[addressIndex],Identity))throw new MissingContract(at,"Unobserved or unsupported tile/base-address relation; no synthetic native address");
   return addressIndex;
  }
 }
 public sealed class NavigationHeaderSpatial:NavigationView {
  public readonly NavigationTileHeader Identity;
  public readonly Field<int> GridX08=new Field<int>(),GridZ0C=new Field<int>(),BvNodeCount30=new Field<int>();
  public readonly Field<float> Climb44=new Field<float>(),BvQuantFactor60=new Field<float>();
  public readonly NavigationPoint3 BoundsMin48=new NavigationPoint3(),BoundsMax54=new NavigationPoint3();
  public NavigationHeaderSpatial(NavigationTileHeader identity){if(identity==null)throw new ArgumentNullException("identity");Identity=identity;}
 }
 public sealed class NavigationPolySpatial:NavigationView {
  public readonly NavigationPoly Identity;
  public readonly Field<ushort> Flags1C=new Field<ushort>();
  public readonly Field<byte> Type1F=new Field<byte>();
  public NavigationPolySpatial(NavigationPoly identity){if(identity==null)throw new ArgumentNullException("identity");Identity=identity;}
 }
 public sealed class NavigationDetail12:NavigationView {
  public readonly Field<uint> VertexBase00=new Field<uint>(),TriangleBase04=new Field<uint>();
  public readonly Field<byte> TriangleCount09=new Field<byte>();
 }
 public sealed class NavigationTriangle4:NavigationView {
  public readonly Field<byte> A00=new Field<byte>(),B01=new Field<byte>(),C02=new Field<byte>();
  // The fourth byte is not read by the recovered closest-point path.
 }
 public sealed class NavigationBvNode16:NavigationView {
  public readonly Field<ushort> MinX00=new Field<ushort>(),MinY02=new Field<ushort>(),MinZ04=new Field<ushort>(),MaxX06=new Field<ushort>(),MaxY08=new Field<ushort>(),MaxZ0A=new Field<ushort>();
  public readonly Field<int> Index0C=new Field<int>();
 }
 public sealed class NavigationRefWindow {
  public readonly NavigationRefBuffer Buffer;public readonly int BaseIndex;
  public NavigationRefWindow(NavigationRefBuffer buffer,int baseIndex){Buffer=buffer;BaseIndex=baseIndex;}
  public Field<uint> At(int index,uint at){if(Buffer==null)throw new MissingContract(at,"Null output-reference address");return Buffer.At((long)BaseIndex+index,at);}
 }
 public sealed class NavigationSpatialState {
  readonly Dictionary<NavigationMesh,NavigationMeshSpatial> meshes=new Dictionary<NavigationMesh,NavigationMeshSpatial>();
  readonly Dictionary<NavigationTile,NavigationTileSpatial> tiles=new Dictionary<NavigationTile,NavigationTileSpatial>();
  readonly Dictionary<NavigationTileHeader,NavigationHeaderSpatial> headers=new Dictionary<NavigationTileHeader,NavigationHeaderSpatial>();
  readonly Dictionary<NavigationPoly,NavigationPolySpatial> polys=new Dictionary<NavigationPoly,NavigationPolySpatial>();
  // These are own execution-domain limits, never native count defaults/statuses.
  public readonly int OwnMaxGridCells,OwnMaxBucketLinks,OwnMaxRecords;
  public NavigationSpatialState():this(4096,4096,65536){}
  public NavigationSpatialState(int gridCells,int bucketLinks,int records){if(gridCells<1||bucketLinks<1||records<1)throw new ArgumentOutOfRangeException("own execution limits");OwnMaxGridCells=gridCells;OwnMaxBucketLinks=bucketLinks;OwnMaxRecords=records;}
  static void Add<K,V>(Dictionary<K,V> map,K key,V value)where V:class{V old;if(map.TryGetValue(key,out old)&&!Object.ReferenceEquals(old,value))throw new InvalidOperationException("Identity already attached to a different spatial view");map[key]=value;}
  public void Attach(NavigationMeshSpatial v){if(v==null)throw new ArgumentNullException("view");Add(meshes,v.Identity,v);}
  public void Attach(NavigationTileSpatial v){if(v==null)throw new ArgumentNullException("view");Add(tiles,v.Identity,v);}
  public void Attach(NavigationHeaderSpatial v){if(v==null)throw new ArgumentNullException("view");Add(headers,v.Identity,v);}
  public void Attach(NavigationPolySpatial v){if(v==null)throw new ArgumentNullException("view");Add(polys,v.Identity,v);}
  static V Get<K,V>(Dictionary<K,V> map,K key,uint at)where K:NavigationView where V:NavigationView{if(key==null)throw new MissingContract(at,"Null spatial identity");key.RequireLive(at);V v;if(!map.TryGetValue(key,out v))throw new MissingContract(at,"Spatial identity has no explicit attached view");v.RequireLive(at);return v;}
  public NavigationMeshSpatial Mesh(NavigationMesh identity,uint at){return Get(meshes,identity,at);}
  public NavigationTileSpatial Tile(NavigationTile identity,uint at){return Get(tiles,identity,at);}
  public NavigationHeaderSpatial Header(NavigationTileHeader identity,uint at){return Get(headers,identity,at);}
  public NavigationPolySpatial Poly(NavigationPoly identity,uint at){return Get(polys,identity,at);}
 }
}
