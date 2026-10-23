using System;
using System.Collections.Generic;
namespace GrimDynasty.ReverseClient.Actions {
 public sealed class NavigationCorridorFilterCosts:NavigationView {
  public readonly NavigationQueryFilter Identity;
  public readonly Field<float>[] AreaCosts00=new Field<float>[64];
  public NavigationCorridorFilterCosts(NavigationQueryFilter identity){if(identity==null)throw new ArgumentNullException("identity");Identity=identity;for(int n=0;n<64;n++)AreaCosts00[n]=new Field<float>();}
 }
 // Attach mutable search state to the existing query/filter identities. This
 // does not initialize native query storage or choose default terrain costs.
 public sealed class NavigationCorridorState:NavigationView {
  public readonly NavigationQuery Identity;
  public readonly Field<NavigationNodePool> Pool58=new Field<NavigationNodePool>();
  public readonly Field<NavigationNodeQueue> Queue60=new Field<NavigationNodeQueue>();
  public readonly int OwnMaxSteps;
  readonly Dictionary<NavigationQueryFilter,NavigationCorridorFilterCosts> filters=new Dictionary<NavigationQueryFilter,NavigationCorridorFilterCosts>();
  public NavigationCorridorState(NavigationQuery identity):this(identity,1048576){}
  public NavigationCorridorState(NavigationQuery identity,int ownMaxSteps){if(identity==null)throw new ArgumentNullException("identity");if(ownMaxSteps<1)throw new ArgumentOutOfRangeException("ownMaxSteps");Identity=identity;OwnMaxSteps=ownMaxSteps;}
  public void Attach(NavigationCorridorFilterCosts costs){if(costs==null)throw new ArgumentNullException("costs");NavigationCorridorFilterCosts prior;if(filters.TryGetValue(costs.Identity,out prior)&&!Object.ReferenceEquals(prior,costs))throw new InvalidOperationException("Filter already has a different cost view");filters[costs.Identity]=costs;}
  public NavigationCorridorFilterCosts Costs(NavigationQueryFilter identity,uint at){RequireLive(at);if(identity==null)throw new MissingContract(at,"Null corridor filter identity");identity.RequireLive(at);NavigationCorridorFilterCosts view;if(!filters.TryGetValue(identity,out view))throw new MissingContract(at,"Filter costs not explicitly attached");view.RequireLive(at);return view;}
  internal void Identify(NavigationQuery query,uint at){RequireLive(at);if(!Object.ReferenceEquals(query,Identity))throw new MissingContract(at,"Search sidecar belongs to another query identity");Identity.RequireLive(at);}
 }
 public sealed class NavigationCorridorBoundaries:NavigationNearestBoundaries {
  public readonly NavigationMeshTilesState MeshState;
  public readonly NavigationCorridorState Search;
  public readonly Field<float> HeuristicScale34BB3C=new Field<float>(),Half34BADC=new Field<float>(),PortalScale34B9CC=new Field<float>();
  static NavigationSpatialState SpatialOf(NavigationMeshTilesState mesh){if(mesh==null)throw new ArgumentNullException("mesh");return mesh.Spatial;}
  public NavigationCorridorBoundaries(NavigationMeshTilesState mesh,NavigationCorridorState search):base(SpatialOf(mesh)){if(search==null)throw new ArgumentNullException("search");MeshState=mesh;Search=search;}
  // Explicit disk-observed mathematical configuration. No loaded state, native
  // allocator, thread/TLS or floating control-register state is established.
  public void UseObservedCorridorConstants(){UseObservedNearestConstants();HeuristicScale34BB3C.Write(NavigationCoreContracts.FromBits(0x3f7fbe77));Half34BADC.Write(NavigationCoreContracts.FromBits(0x3f000000));PortalScale34B9CC.Write(NavigationCoreContracts.FromBits(0x3b808081));}
 }
 // Engine261CB0: r1-navigation-core/07,08. Pool/heap/entry callees are in
 // r1-navigation-cross-poly-corridor; entry portal is r1-corridor-portal-helper.
 // Original Engine SHA2564c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153.
 // Supported own domain: explicit stable managed mesh/query identities, typed
 // shared cells, masked FP exceptions and per-instruction binary32 rounding.
 // Arbitrary byte aliases, native allocation faults, concurrent lifetimes and
 // nondefault MXCSR/FTZ/DAZ are not implied. MissingContract preserves the actual
 // prefix and is never translated to a fabricated native status. This produces
 // a reference corridor only: it does not implement262480 funnel or movement.
 public static class NavigationCorridorContracts {
  public const uint Failure=0x80000008u,Success=0x40000000u;
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null corridor view");var view=value as NavigationView;if(view!=null)view.RequireLive(at);return value;}
  static T Entry<T>(T[] array,long index,uint at)where T:class{if(array==null||index<0||index>=array.Length)throw new MissingContract(at,"Outside own corridor table extent");return Need(array[(int)index],at);}
  static float F(float x){return NavigationCoreContracts.F(x);}
  static float Sqrt(float x){return F((float)Math.Sqrt(x));}
  static float Read(NavigationPoint3 point,int axis,uint at){return Cell(point,axis,at).Read(at);}
  // The frozen library's public component properties resolve the same borrowed
  // scalar address as its internal CellAt, but report getter failures at0.
  // Reattribute only that own address-resolution failure to this actual load;
  // never read the field payload during address formation or rebuild old code.
  static Field<float> Cell(NavigationPoint3 point,int axis,uint at){Need(point,at);try{return axis==0?point.X:axis==1?point.Y:point.Z;}catch(MissingContract e){throw new MissingContract(at,e.Message,e);}}
  static float NodeRead(NavigationSearchNode node,int axis,uint at){Need(node,at);return Read(node.Position00,axis,at);}
  static uint Mask(uint bits){return unchecked((1u<<(int)(bits&31))-1u);}
  static NavigationNodePool Pool(NavigationCorridorState state,uint at){state.RequireLive(at);state.Identity.RequireLive(at);return Need(state.Pool58.Read(at),at);}
  static NavigationNodeQueue Queue(NavigationCorridorState state,uint at){state.RequireLive(at);state.Identity.RequireLive(at);return Need(state.Queue60.Read(at),at);}
  static void Budget(ref long steps,NavigationCorridorState state,uint at){if(++steps>state.OwnMaxSteps)throw new MissingContract(at,"Own finite search/link/parent traversal budget exceeded");}
  static float Cost(NavigationCorridorBoundaries b,NavigationQueryFilter filter,uint area,uint at){return b.Search.Costs(filter,at).AreaCosts00[area].Read(at);}
  static Field<NavigationMeshLink12[]> Links(NavigationCorridorBoundaries b,NavigationTile tile,uint at){Need(tile,at);return b.MeshState.Tile(tile,at).Links20.Snapshot();}
  static NavigationMeshLink12 Link(Field<NavigationMeshLink12[]> pointer,uint index,uint at){return Entry(pointer.Read(at),index,at);}
  // LEA/ADD forms a stride1C address; it does not inspect the addressed node.
  // The pool array base is captured at formation and its element/lifetime is
  // consumed only at a subsequent node load. Arbitrary native null-base address
  // arithmetic is outside this own declared managed-array address domain.
  sealed class NodeAddress {
   readonly NavigationSearchNode direct;readonly Field<NavigationSearchNode[]> nodes;readonly int index;readonly bool deferred;
   public NodeAddress(NavigationSearchNode node){direct=node;}
   public NodeAddress(NavigationNodePool pool,int index,uint at){Need(pool,at);nodes=pool.Nodes00.Snapshot();this.index=index;deferred=true;}
   public NavigationSearchNode Read(uint at){return deferred?Entry(nodes.Read(at),index,at):Need(direct,at);}
  }
  // Search decode is the observed zero-extended64-bit SHR, not the32-bit SHR
  // of2688C0. No extra salt/header validation is inserted inside the loop.
  static void Decode(NavigationQuery query,uint reference,bool neighbor,NavigationCorridorBoundaries b,out NavigationTile tile,out Field<NavigationPoly[]> polygons,out uint polygonIndex){
   uint at=neighbor?0x261f4du:0x261eaau;var mesh=Need(Need(query,at).Mesh0.Read(at),at);
   at=neighbor?0x261f57u:0x261ebau;b.MeshState.RequireLive(at);
   if(!Object.ReferenceEquals(mesh,b.MeshState.Mesh))throw new MissingContract(at,"Own link state is bound to a different mesh identity");
   uint tileBits=mesh.TileBits5C.Read(at);at=neighbor?0x261f5bu:0x261ebeu;mesh.RequireLive(at);uint polyBits=mesh.PolyBits60.Read(at);
   ulong tileIndex=((ulong)reference>>(int)(polyBits&63))&Mask(tileBits);polygonIndex=reference&Mask(polyBits);
   at=neighbor?0x261f79u:0x261ee9u;mesh.RequireLive(at);var tiles=mesh.Tiles50.Snapshot();
   at=neighbor?0x261f92u:0x261eedu;tile=Entry(tiles.Read(at),(long)tileIndex,at);polygons=tile.Polys10.Snapshot();
  }
  public static uint FindPath(NavigationQuery query,uint startRef,uint endRef,NavigationPoint3 from,NavigationPoint3 to,NavigationQueryFilter filter,NavigationRefBuffer corridor,Field<uint> count,int capacity,NavigationCorridorBoundaries b,Invocation invocation){
   var i=Need(invocation,0x261cdb);i.At=0x261cdb;Need(count,i.At).Write(0);
   if(startRef==0||endRef==0)return Failure;
   i.At=0x261cf8;var initialMesh=Need(query,i.At).Mesh0.Read(i.At);
   i.At=0x261cfe;if(Need(b,i.At).ValidateRef2688C0(initialMesh,startRef)==0)return Failure;
   i.At=0x261d10;if(b.ValidateRef2688C0(initialMesh,endRef)==0)return Failure;
   if(startRef==endRef){i.At=0x261d29;Need(corridor,i.At).At(0,i.At).Write(startRef);i.At=0x261d30;count.Write(1);return Success;}
   i.At=0x261d51;var state=b.Search;state.Identify(query,i.At);var pool=Pool(state,i.At);
   NavigationSearchStorage.ClearPool(pool,0x261d66,0x261d6a,0x261d76);
   i.At=0x261d7d;var queue=Queue(state,i.At);i.At=0x261d83;queue.Count0C.Write(0);
   i.At=0x261d87;pool=Pool(state,i.At);i.At=0x261d8b;var start=NavigationSearchStorage.GetNode26DCA0(pool,startRef,0);
   // A null FIRST node has no native branch to0x20. Preserve input-copy/load
   // ordering, then stop at the first dereference if no owned node exists.
   var bits=Cell(from,0,0x261d90).Snapshot();var scale=b.HeuristicScale34BB3C.Snapshot();
   Cell(Need(start,0x261da6).Position00,0,0x261da6).CopyFrom(bits);
   bits=Cell(from,1,0x261da8).Snapshot();Cell(start.Position00,1,0x261dab).CopyFrom(bits);
   bits=Cell(from,2,0x261dae).Snapshot();Cell(start.Position00,2,0x261db1).CopyFrom(bits);
   start.Packed14.Transform(0xff000000,0);var packed=start.Packed14.Snapshot();start.Cost0C.Write(0f);packed.Transform(0xe7ffffff,0x04000000);
   float dz=Read(to,2,0x261dd4);dz=F(dz-Read(from,2,0x261dd9));float dx=Read(to,0,0x261dde),dy=Read(to,1,0x261de2);
   dy=F(dy-Read(from,1,0x261de7));dx=F(dx-Read(from,0,0x261dec));dz=F(dz*dz);
   start.Reference18.Write(startRef);start.Packed14.CopyFrom(packed);dy=F(dy*dy);dx=F(dx*dx);
   float heuristic=F(Sqrt(F(F(dy+dx)+dz))*scale.Read(0x261e11));start.Total10.Write(heuristic);
   i.At=0x261e1b;queue=Queue(state,i.At);i.At=0x261e1f;NavigationSearchStorage.Push25F680(queue,start);
   i.At=0x261e24;queue=Queue(state,i.At);float bestMetric=Need(start,0x261e2d).Total10.Read(0x261e2d);var best=start;uint status=Success;long steps=0;uint heapCheckAt=0x261e44;
   while(Need(queue,heapCheckAt).Count0C.Read(heapCheckAt)!=0){
    Budget(ref steps,state,0x261e70);var heapBits=queue.Heap00.Snapshot();var heap=heapBits.Read(0x261e73);var currentBits=Entry(heap,0,0x261e73).Snapshot();
    int qcount=queue.Count0C.Read(0x261e76);queue.Count0C.Write(unchecked(qcount-1));qcount=queue.Count0C.Read(0x261e79);
    var replacement=Entry(heap,qcount,0x261e7d).Snapshot();i.At=0x261e81;NavigationSearchStorage.TrickleDown26DDC0(queue,replacement);
    var current=Need(currentBits.Read(0x261e86),0x261e86);current.Packed14.Transform(0xfbffffff,0);current.Packed14.Transform(0xffffffff,0x08000000);
    uint currentRef=current.Reference18.Read(0x261e96);var currentPacked=current.Packed14.Snapshot();
    if(currentRef==endRef){best=current;break;}
    NavigationTile currentTile;Field<NavigationPoly[]> currentPolygons;uint currentPolyIndex;
    Decode(query,currentRef,false,b,out currentTile,out currentPolygons,out currentPolyIndex);
    uint parentIndex=currentPacked.ReadMasked(0x00ffffff,0x261ef7),parentRef=0;
    if(parentIndex!=0){i.At=0x261f00;pool=Pool(state,i.At);var parentAddress=new NodeAddress(pool,unchecked((int)parentIndex-1),0x261f0c);parentRef=parentAddress.Read(0x261f0f).Reference18.Read(0x261f0f);}
    var currentPoly=Entry(currentPolygons.Read(0x261f16),currentPolyIndex,0x261f16);uint linkIndex=b.MeshState.Poly(currentPoly,0x261f16).FirstLink00.Read(0x261f16);
    while(linkIndex!=UInt32.MaxValue){
     Budget(ref steps,state,0x261f30);var linkBits=Links(b,currentTile,0x261f30);var link=Link(linkBits,linkIndex,0x261f39);uint neighborRef=link.Reference00.Read(0x261f39);
     if(neighborRef!=0&&neighborRef!=parentRef){
      NavigationTile neighborTile;Field<NavigationPoly[]> neighborPolygons;uint neighborPolyIndex;
      Decode(query,neighborRef,true,b,out neighborTile,out neighborPolygons,out neighborPolyIndex);
      var neighborPoly=Entry(neighborPolygons.Read(0x261f96),neighborPolyIndex,0x261f96);ushort flags=b.Spatial.Poly(neighborPoly,0x261f96).Flags1C.Read(0x261f96);
      if((Need(filter,0x261f9a).Include100.Read(0x261f9a)&flags)!=0&&(Need(filter,0x261fa7).Exclude102.Read(0x261fa7)&flags)==0){
       byte side=Need(link,0x261fb4).Side09.Read(0x261fb4),nodeState=side==255?(byte)0:(byte)(side>>1);
       i.At=0x261fc8;pool=Pool(state,i.At);i.At=0x261fce;var node=NavigationSearchStorage.GetNode26DCA0(pool,neighborRef,nodeState);
       if(node==null)status|=0x20;
       else{
        if(Need(node,0x261ff6).Packed14.ReadMasked(0x1c000000,0x261ff6)==0){
         i.At=0x262021;NavigationSearchStorage.Entry2631E0(b.MeshState,currentRef,currentPoly,currentTile,neighborRef,neighborPoly,neighborTile,node.Position00,b.Half34BADC,b.PortalScale34B9CC); // Native ignores status.
        }
        uint area=(uint)(b.Spatial.Poly(currentPoly,0x26202d).Type1F.Read(0x26202d)&63);float cost,h;
        if(neighborRef==endRef){
         float nx=NodeRead(node,0,0x26204d),ny=NodeRead(node,1,0x262051);dy=F(ny-NodeRead(current,1,0x262064));float nz=NodeRead(node,2,0x26206a);dx=F(nx-NodeRead(current,0,0x26206f));
         float tx=Read(to,0,0x262075);dz=F(nz-NodeRead(current,2,0x26207c));float ty=Read(to,1,0x262082);tx=F(tx-nx);float tz=Read(to,2,0x26208b);ty=F(ty-ny);tz=F(tz-nz);
         dy=F(dy*dy);dx=F(dx*dx);dz=F(dz*dz);ty=F(ty*ty);float segment=F(dy+dx);tx=F(tx*tx);tz=F(tz*tz);segment=F(segment+dz);float tail=F(ty+tx);
         cost=Sqrt(segment);tail=F(tail+tz);cost=F(cost*Cost(b,filter,area,0x2620cb));uint endArea=(uint)(b.Spatial.Poly(neighborPoly,0x2620d1).Type1F.Read(0x2620d1)&63);
         cost=F(cost+Need(current,0x2620d8).Cost0C.Read(0x2620d8));tail=Sqrt(tail);tail=F(tail*Cost(b,filter,endArea,0x2620e2));cost=F(cost+tail);h=0f;
        }else{
         float nx=NodeRead(node,0,0x2620f1),ny=NodeRead(node,1,0x2620f5);dx=F(nx-NodeRead(current,0,0x2620fd));float nz=NodeRead(node,2,0x262103);dy=F(ny-NodeRead(current,1,0x26210b));dz=F(nz-NodeRead(current,2,0x262114));
         dx=F(dx*dx);dy=F(dy*dy);dz=F(dz*dz);cost=Sqrt(F(F(dy+dx)+dz));cost=F(cost*Cost(b,filter,area,0x262133));cost=F(cost+Need(current,0x262141).Cost0C.Read(0x262141));
         dz=Read(to,2,0x262147);dx=Read(to,0,0x26214c);dz=F(dz-nz);dy=Read(to,1,0x262154);dx=F(dx-nx);dy=F(dy-ny);
         dz=F(dz*dz);dx=F(dx*dx);dy=F(dy*dy);h=F(Sqrt(F(F(dx+dy)+dz))*scale.Read(0x26217c));
        }
        packed=Need(node,0x262181).Packed14.Snapshot();float total=F(h+cost);bool improve=true;
        if(packed.ReadMasked(0x04000000,0x26218f)!=0&&total>=node.Total10.Read(0x262193))improve=false;
        if(improve&&packed.ReadMasked(0x08000000,0x26219d)!=0&&total>=node.Total10.Read(0x2621a1))improve=false;
        if(improve){
         i.At=0x2621ab;pool=Pool(state,i.At);int index=NavigationSearchStorage.IndexOf(pool,current,0x2621b2);
         node.Cost0C.Write(cost);node.Total10.Write(total);node.Reference18.Write(neighborRef);
         packed=node.Packed14.Snapshot();packed.Transform(0xf7000000,unchecked((uint)(index+1))&0x00ffffff);node.Packed14.CopyFrom(packed);
         if((packed.ReadMasked(0x04000000,0x2621f6)&0x04000000)!=0){i.At=0x2621fc;queue=Queue(state,i.At);i.At=0x262200;NavigationSearchStorage.Modify25F6F0(queue,node);}
         else{node.Packed14.Transform(0xffffffff,0x04000000);i.At=0x26220e;queue=Queue(state,i.At);i.At=0x262212;NavigationSearchStorage.Push25F680(queue,node);}
         // COMISS/JNC rejects ordered >=, but unordered updates this best node.
         if(!(h>=bestMetric)){bestMetric=h;best=node;}
        }
       }
      }
     }
     linkBits=Links(b,currentTile,0x26223f);link=Link(linkBits,linkIndex,0x262248);linkIndex=link.Next04.Read(0x262248);
    }
    i.At=0x262259;queue=Queue(state,i.At);heapCheckAt=0x262264;
   }
   if(Need(best,0x2622b2).Reference18.Read(0x2622b2)!=endRef)status|=0x40;
   // Reverse the node parent chain in place BEFORE any corridor output writes.
   NavigationSearchNode previous=null;var reverseAddress=new NodeAddress(best);
   do{
    Budget(ref steps,state,0x2622c0);var reverse=reverseAddress.Read(0x2622c0);packed=reverse.Packed14.Snapshot();uint parent=packed.ReadMasked(0x00ffffff,0x2622c7);NodeAddress oldParent=null;
    if(parent!=0){i.At=0x2622d6;pool=Pool(state,i.At);oldParent=new NodeAddress(pool,unchecked((int)parent-1),0x2622de);}
    uint newParent=0;if(previous!=null){i.At=0x2622eb;pool=Pool(state,i.At);newParent=unchecked((uint)(NavigationSearchStorage.IndexOf(pool,previous,0x2622ef)+1));}
    packed.Transform(0xff000000,newParent&0x00ffffff);reverse.Packed14.CopyFrom(packed);previous=reverse;reverseAddress=oldParent;
   }while(reverseAddress!=null);
   uint written=0;long outputIndex=0;var outputAddress=new NodeAddress(previous);
   do{
    Budget(ref steps,state,0x262340);var outputNode=outputAddress.Read(0x262340);uint reference=outputNode.Reference18.Read(0x262340);written=unchecked(written+1);
    i.At=0x262346;Need(corridor,i.At).At(outputIndex,i.At).Write(reference);outputIndex++;
    if(outputIndex>=capacity){status|=0x10;break;}
    uint parent=outputNode.Packed14.ReadMasked(0x00ffffff,0x262355);if(parent==0)break;
    i.At=0x26235f;pool=Pool(state,i.At);outputAddress=new NodeAddress(pool,unchecked((int)parent-1),0x262367);
   }while(outputAddress!=null);
   i.At=0x26237a;count.Write(written);return status;
  }
 }
}
