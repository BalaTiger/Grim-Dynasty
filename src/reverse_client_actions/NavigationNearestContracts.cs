using System;
namespace GrimDynasty.ReverseClient.Actions {
 // A formed point address, not an eagerly dereferenced vertex. The captured
 // pointer bits and scalar offset are consumed only at a component load. This
 // represents declared contiguous Vec3 cells, not arbitrary native byte aliases.
 public sealed class NavigationNearestPointAddress {
  readonly NavigationPoint3 direct;readonly Field<NavigationPoint3[]> verticesBits;
  readonly uint scalar;readonly bool deferred;
  public NavigationNearestPointAddress(NavigationPoint3 point){direct=point;}
  internal NavigationNearestPointAddress(Field<NavigationPoint3[]> pointerBits,uint scalarOffset){verticesBits=pointerBits;scalar=scalarOffset;deferred=true;}
  public Field<float> CellAt(int component,uint at){
   if(component<0||component>2)throw new MissingContract(at,"Outside own point-component domain");
   NavigationPoint3 point=direct;int axis=component;
   if(deferred){
    var vertices=verticesBits.Read(at);long offset=(long)scalar+component,index=offset/3;axis=(int)(offset%3);
    if(vertices==null||index>=vertices.Length)throw new MissingContract(at,"Outside declared own deferred vertex-table extent");
    point=vertices[(int)index];
   }
   if(point==null)throw new MissingContract(at,"Null deferred point address");
   return point.CellAt(axis,at);
  }
 }
 // Explicit own mesh views. These routines do not load original map data,
 // initialize a native query, implement native ABI/EH or observe a running game.
 public class NavigationNearestBoundaries:NavigationEnumerationBoundaries {
  public readonly Field<uint> AbsMask34C410=new Field<uint>();
  public readonly Field<float> Lower34BDD4=new Field<float>(),Upper34BB48=new Field<float>();
  public NavigationNearestBoundaries(NavigationSpatialState spatial):base(spatial){}
  // Explicit own constants from r1-navigation-nearest-constants and
  // r1-navigation-26d580-constants, separate from native loaded mutable memory.
  public void UseObservedNearestConstants(){UseObservedGeometryConstants();AbsMask34C410.Write(0x7fffffff);Lower34BDD4.Write(NavigationCoreContracts.FromBits(0xb8d1b717));Upper34BB48.Write(NavigationCoreContracts.FromBits(0x3f800347));}
  public virtual uint Enumerate261950(NavigationQuery query,NavigationPoint3 point,NavigationPoint3 extent,NavigationQueryFilter filter,NavigationRefBuffer refs,Field<uint> count,int capacity){return NavigationEnumerationContracts.Enumerate(query,point,extent,filter,refs,count,capacity,this,new Invocation());}
  public virtual uint Closest260520(NavigationQuery query,uint reference,NavigationPoint3 point,NavigationPoint3 output,Field<byte> overPoly){return NavigationNearestContracts.Closest(query,reference,point,output,overPoly,this);}
  public virtual byte Height26D580(NavigationPoint3 point,NavigationPoint3 a,NavigationPoint3 b,NavigationPoint3 c,Field<float> height){return NavigationNearestContracts.Height(point,a,b,c,height,this);}
  public virtual byte Height26D580(NavigationPoint3 point,NavigationNearestPointAddress a,NavigationNearestPointAddress b,NavigationNearestPointAddress c,Field<float> height){return NavigationNearestContracts.Height(point,a,b,c,height,this);}
 }
 public static class NavigationNearestContracts {
  static float F(float x){return NavigationCoreContracts.F(x);}
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null own navigation view");var view=value as NavigationView;if(view!=null)view.RequireLive(at);return value;}
  static T Entry<T>(T[] entries,long index,uint at)where T:class{if(entries==null||index<0||index>=entries.Length)throw new MissingContract(at,"Outside declared own navigation table extent");return Need(entries[(int)index],at);}
  static Field<float> Cell(NavigationPoint3 point,int component,uint at){return Need(point,at).CellAt(component,at);}
  static float Read(NavigationPoint3 point,int component,uint at){return Cell(point,component,at).Read(at);}
  static float Read(NavigationNearestPointAddress point,int component,uint at){return Need(point,at).CellAt(component,at).Read(at);}
  static float Vertex(NavigationPoint3[] vertices,uint index,int component,uint at){return Read(Entry(vertices,index,at),component,at);}
  static uint Mask(uint count){return unchecked((1U<<(int)(count&31))-1U);}
  static float Sqrt(float value){return F((float)Math.Sqrt(value));}
  static NavigationTileSpatial Tile(NavigationTile identity,uint at,NavigationNearestBoundaries b){Need(identity,at);return Need(b.Spatial.Tile(identity,at),at);}
  static NavigationPolySpatial Poly(NavigationPoly identity,uint at,NavigationNearestBoundaries b){Need(identity,at);return Need(b.Spatial.Poly(identity,at),at);}
  static NavigationHeaderSpatial Header(NavigationTileHeader identity,uint at,NavigationNearestBoundaries b){Need(identity,at);return Need(b.Spatial.Header(identity,at),at);}

  //26D580. Straight-line float32 register operations preserve operand/load order.
  // Input Y is unused, but all triangle Ys are consumed before containment gates.
  public static byte Height(NavigationPoint3 point,NavigationPoint3 a,NavigationPoint3 b,NavigationPoint3 c,Field<float> height,NavigationNearestBoundaries boundary){
   return Height(point,new NavigationNearestPointAddress(a),new NavigationNearestPointAddress(b),new NavigationNearestPointAddress(c),height,boundary);
  }
  public static byte Height(NavigationPoint3 point,NavigationNearestPointAddress a,NavigationNearestPointAddress b,NavigationNearestPointAddress c,Field<float> height,NavigationNearestBoundaries boundary){
   float x1=Read(a,0,0x26d584),x0=Read(a,2,0x26d588),x5=Read(c,0,0x26d58d),x2=Read(c,2,0x26d592);
   x5=F(x5-x1);float x3=Read(b,2,0x26d59c);x2=F(x2-x0);
   float x4=Read(point,0,0x26d5a6);x3=F(x3-x0);x4=F(x4-x1);
   float x7=Read(b,0,0x26d5bc);x7=F(x7-x1);float x8=x3,x9=x2;
   float x10=Read(c,1,0x26d5df);x10=F(x10-Read(a,1,0x26d5e5));
   float x11=Read(point,2,0x26d5f1);x11=F(x11-x0);x0=x5;x8=F(x8*x2);
   float x12=Read(b,1,0x26d609);x12=F(x12-Read(a,1,0x26d60f));
   x0=F(x0*x5);float x6=x11;x6=F(x6*x2);x11=F(x11*x3);x9=F(x9*x2);
   x2=Need(boundary,0x26d62b).One34BB44.Read(0x26d62b);
   x9=F(x9+x0);x0=x7;x0=F(x0*x5);x8=F(x8+x0);x0=x4;x0=F(x0*x5);
   x5=x3;x5=F(x5*x3);x6=F(x6+x0);x4=F(x4*x7);x0=x7;x0=F(x0*x7);
   x11=F(x11+x4);x5=F(x5+x0);x0=x8;x0=F(x0*x8);x1=x5;
   x5=F(x5*x6);x1=F(x1*x9);x6=F(x6*x8);x1=F(x1-x0);x0=x11;
   x0=F(x0*x8);x11=F(x11*x9);x2=F(x2/x1);x5=F(x5-x0);
   x0=boundary.Lower34BDD4.Read(0x26d69e);x11=F(x11-x6);x5=F(x5*x2);x11=F(x11*x2);
   if(!(x5>=x0))return 0; // COMISS;JC: also reject unordered.
   if(!(x11>=x0))return 0;
   x0=F(x11+x5);float upper=boundary.Upper34BB48.Read(0x26d6c7);
   if(x0>upper)return 0; // JA does not reject unordered.
   x10=F(x10*x5);x12=F(x12*x11);x10=F(x10+Read(a,1,0x26d6e2));x10=F(x10+x12);
   Need(height,0x26d6ed).Write(x10);return 1;
  }

  static void Gather(NavigationPoly poly,int ordinal,Field<NavigationPoint3[]> verticesBits,NavigationPoint3 destination,uint indexAt,uint xAt,uint yAt,uint zAt){
   poly.RequireLive(indexAt);uint index=Need(poly.Indices04[ordinal],indexAt).Read(indexAt);
   var vertices=verticesBits.Read(xAt);
   var value=Cell(Entry(vertices,index,xAt),0,xAt).Snapshot();destination.X.CopyFrom(value);
   value=Cell(Entry(vertices,index,yAt),1,yAt).Snapshot();destination.Y.CopyFrom(value);
   value=Cell(Entry(vertices,index,zAt),2,zAt).Snapshot();destination.Z.CopyFrom(value);
  }

  //260520 is NOT260AD0: it initially copies an ordinary point and then may
  // replace its height from detail triangles. It also has a separate0x40 branch.
  public static uint Closest(NavigationQuery query,uint reference,NavigationPoint3 point,NavigationPoint3 output,Field<byte> overPoly,NavigationNearestBoundaries b){
   var meshBits=Need(query,0x26053b).Mesh0.Snapshot();if(reference==0)return NavigationCoreContracts.Failure;
   var mesh=Need(meshBits.Read(0x26055b),0x26055b);uint tileBits=mesh.TileBits5C.Read(0x26055b);
   mesh.RequireLive(0x260565);uint polyBits=mesh.PolyBits60.Read(0x260565);
   uint polyIndex=reference&Mask(polyBits),tileIndex=(reference>>(int)(polyBits&31))&Mask(tileBits);
   mesh.RequireLive(0x260588);if(tileIndex>=mesh.TileLimit30.Read(0x260588))return NavigationCoreContracts.Failure;
   mesh.RequireLive(0x260598);var tileBitsPointer=mesh.Tiles50.Snapshot();
   mesh.RequireLive(0x2605a2);uint saltBits=mesh.SaltBits58.Read(0x2605a2);
   var tile=Entry(tileBitsPointer.Read(0x2605af),tileIndex,0x2605af);
   uint salt=(reference>>(int)(unchecked(tileBits+polyBits)&31))&Mask(saltBits);
   if(tile.Salt00.Read(0x2605af)!=salt)return NavigationCoreContracts.Failure;
   tile.RequireLive(0x2605b4);var header=tile.Header08.Read(0x2605b4);if(header==null)return NavigationCoreContracts.Failure;
   Need(header,0x2605bd);if(polyIndex>=header.PolyCount18.Read(0x2605bd))return NavigationCoreContracts.Failure;
   tile.RequireLive(0x2605cd);var polygonsBits=tile.Polys10.Snapshot();
   var polygons=polygonsBits.Read(0x2605f3);var poly=Entry(polygons,polyIndex,0x2605f3);
   byte kind=Poly(poly,0x2605f3,b).Type1F.Read(0x2605f3);
   if((kind&0xc0)==0x40){
    tile.RequireLive(0x260601);var verticesBits=tile.Vertices18.Snapshot();
    poly.RequireLive(0x260605);uint ia=Entry(poly.Indices04,0,0x260605).Read(0x260605);
    poly.RequireLive(0x260620);uint ib=Entry(poly.Indices04,1,0x260620).Read(0x260620);
    var vertices=verticesBits.Read(0x260624);
    float x1=Vertex(vertices,ia,0,0x260624);x1=F(x1-Read(point,0,0x26062a));
    float x2=Vertex(vertices,ia,1,0x260630);x2=F(x2-Read(point,1,0x260637));
    float x0=Vertex(vertices,ia,2,0x26063e);x0=F(x0-Read(point,2,0x260649));
    float x6=Vertex(vertices,ib,1,0x260650),x5=Vertex(vertices,ib,2,0x260657);
    x2=F(x2*x2);x0=F(x0*x0);x1=F(x1*x1);x2=F(x2+x1);x1=x5;
    x1=F(x1-Read(point,2,0x260671));x5=F(x5-Vertex(vertices,ia,2,0x260678));x2=F(x2+x0);x0=x6;
    x0=F(x0-Read(point,1,0x260686));x6=F(x6-Vertex(vertices,ia,1,0x26068d));x1=F(x1*x1);
    float x7=Sqrt(x2);x2=Vertex(vertices,ib,0,0x26069c);float x3=x2;x0=F(x0*x0);
    x3=F(x3-Read(point,0,0x2606a9));x2=F(x2-Vertex(vertices,ia,0,0x2606af));
    x3=F(x3*x3);x3=F(x3+x0);x3=F(x3+x1);x0=Sqrt(x3);x0=F(x0+x7);x7=F(x7/x0);
    x6=F(x6*x7);x2=F(x2*x7);x6=F(x6+Vertex(vertices,ia,1,0x2606d8));
    x5=F(x5*x7);x2=F(x2+Vertex(vertices,ia,0,0x2606e3));x5=F(x5+Vertex(vertices,ia,2,0x2606f1));
    Cell(output,1,0x260700).Write(x6);Cell(output,0,0x26070e).Write(x2);Cell(output,2,0x260714).Write(x5);
    if(overPoly!=null)overPoly.Write(0);return NavigationCoreContracts.Success;
   }
   poly.RequireLive(0x26072b);int count=poly.VertexCount1E.Read(0x26072b);
   tile.RequireLive(0x260732);if(!Object.ReferenceEquals(polygons,tile.Polys10.Read(0x260732)))throw new MissingContract(0x260732,"Changed polygon table base outside own address-difference domain");
   var detailBits=Tile(tile,0x260747,b).Details28.Snapshot();
   if(count>6)throw new MissingContract(0x26074f,"Polygon exceeds declared own six-vertex scratch domain; native guard not inferred");
   var local=new NavigationPoint3[count];var distances=new Field<float>[count];var parameters=new Field<float>[count];
   for(int n=0;n<count;n++){local[n]=new NavigationPoint3();distances[n]=new Field<float>();parameters[n]=new Field<float>();}
   int gathered=0;
   if(count>=4){tile.RequireLive(0x260759);var vertices=tile.Vertices18.Snapshot();
    Gather(poly,0,vertices,local[0],0x260780,0x260791,0x260798,0x2607a0);
    Gather(poly,1,vertices,local[1],0x2607a8,0x2607b1,0x2607b8,0x2607c0);
    Gather(poly,2,vertices,local[2],0x2607c8,0x2607d1,0x2607d8,0x2607e0);
    Gather(poly,3,vertices,local[3],0x2607e8,0x2607f1,0x2607f8,0x260800);gathered=4;
   }
   if(gathered<count){tile.RequireLive(0x260817);var vertices=tile.Vertices18.Snapshot();for(int n=gathered;n<count;n++)Gather(poly,n,vertices,local[n],0x260836,0x260846,0x26084d,0x260855);}
   var copy=Cell(point,0,0x260863).Snapshot();Cell(output,0,0x26086c).CopyFrom(copy);
   copy=Cell(point,1,0x260875).Snapshot();Cell(output,1,0x26087d).CopyFrom(copy);
   copy=Cell(point,2,0x260884).Snapshot();Cell(output,2,0x260889).CopyFrom(copy);
   if(NavigationCoreContracts.PolygonGeometry(point,local,count,distances,parameters,b)==0){
    float best=b.MaxFinite34BDCC.Read(0x2608a7);int selected=-1,n=0;
    if(count>=4){uint[] addresses={0x2608d0,0x2608e1,0x2608f3,0x260904};for(int k=0;k<4;k++){float d=distances[k].Read(addresses[k]);if(!(d>=best)){best=d;selected=k;}}n=4;}
    for(;n<count;n++){float d=distances[n].Read(0x26092a);if(!(d>=best)){best=d;selected=n;}}
    if(count==0)throw new MissingContract(0x260951,"Native zero divisor outside own supported geometry domain; initial output retained");
    int next=unchecked(selected+1)%count;
    float ax=Read(Entry(local,selected,0x260953),0,0x260953),ay=Read(Entry(local,selected,0x26095a),1,0x26095a);
    float x=Read(Entry(local,next,0x26096a),0,0x26096a),t=Entry(parameters,selected,0x260970).Read(0x260970);
    x=F(F(F(x-ax)*t)+ax);float y=F(Read(Entry(local,next,0x26098d),1,0x26098d)-ay);
    Cell(output,0,0x260997).Write(x);float az=Read(Entry(local,selected,0x26099d),2,0x26099d);y=F(F(y*t)+ay);
    float z=F(Read(Entry(local,next,0x2609ac),2,0x2609ac)-az);Cell(output,1,0x2609b6).Write(y);z=F(F(z*t)+az);Cell(output,2,0x2609c4).Write(z);
    if(overPoly!=null)overPoly.Write(0);
   }else if(overPoly!=null)overPoly.Write(1);
   var details=detailBits.Read(0x2609e4);var detail=Entry(details,polyIndex,0x2609e4);
   int triangleCount=detail.TriangleCount09.Read(0x2609e4);if(triangleCount==0)return NavigationCoreContracts.Success;
   detail.RequireLive(0x2609f2);uint triangleBase=detail.TriangleBase04.Read(0x2609f2);
   var trianglesBits=Tile(tile,0x2609f7,b).TriangleRecords38.Snapshot();
   poly.RequireLive(0x2609fb);byte polyVertexCount=poly.VertexCount1E.Read(0x2609fb);
   var height=new Field<float>();
   for(uint n=0;n<(uint)triangleCount;n++){
    uint byteOffset=unchecked((triangleBase+n)*4);var triangles=trianglesBits.Read(0x260a16);
    var triangle=Entry(triangles,byteOffset/4,0x260a16);var vertices=new NavigationNearestPointAddress[3];
    for(int k=0;k<3;k++){
     triangle.RequireLive(0x260a16);byte index=(k==0?triangle.A00:k==1?triangle.B01:triangle.C02).Read(0x260a16);
     if(index<polyVertexCount){
      poly.RequireLive(0x260a22);uint vertexIndex=Entry(poly.Indices04,index,0x260a22).Read(0x260a22);
      tile.RequireLive(0x260a27);var baseVertices=tile.Vertices18.Snapshot();vertices[k]=new NavigationNearestPointAddress(baseVertices,unchecked(vertexIndex*3));
     }else{
      var extraVerticesBits=Tile(tile,0x260a3e,b).DetailVertices30.Snapshot();detail.RequireLive(0x260a42);
      uint vertexBase=detail.VertexBase00.Read(0x260a42);uint scalar=unchecked((unchecked((uint)index-polyVertexCount)+vertexBase)*3);
      // LEA retains the zero-extended scalar offset, including unaligned wrapped
      // offsets. Extent, unknown base and lifetime are checked only by the callee.
      vertices[k]=new NavigationNearestPointAddress(extraVerticesBits,scalar);
     }
    }
    if(b.Height26D580(point,vertices[0],vertices[1],vertices[2],height)!=0){var value=height.Snapshot();Cell(output,1,0x260aa7).CopyFrom(value);break;}
   }
   return NavigationCoreContracts.Success;
  }

  //2610F0. Enumeration and candidate points are actual default producers once
  // supplied mesh/query state is valid. Mock overrides remain test observations.
  public static uint Nearest(NavigationQuery query,NavigationPoint3 point,NavigationPoint3 extent,NavigationQueryFilter filter,Field<uint> resultRef,NavigationPoint3 optionalOutput,NavigationNearestBoundaries b,Invocation invocation){
   invocation.At=0x26112c;Need(resultRef,invocation.At).Write(0);
   var references=new NavigationRefBuffer(128);var count=Field<uint>.Of(0);
   invocation.At=0x261135;uint status=b.Enumerate261950(query,point,extent,filter,references,count,128);
   if((status&0x80000000)!=0)return NavigationCoreContracts.Failure;
   int length=unchecked((int)count.Read(0x261156));float best=b.MaxFinite34BDCC.Read(0x261165);uint selected=0;
   if(length>0){
    uint mask=b.AbsMask34C410.Read(0x2611c2);var closest=new NavigationPoint3();var overPoly=new Field<byte>();
    for(int n=0;n<length;n++){
     uint reference=references.At(n,0x2611d0).Read(0x2611d0);overPoly.Write(0);
     invocation.At=0x2611f4;b.Closest260520(query,reference,point,closest,overPoly); // Native ignores return status.
     byte over=overPoly.Read(0x2611f9);
     float px=Read(point,0,0x261201),x=Read(closest,0,0x261206),py=Read(point,1,0x26120c),dx=F(px-x);
     float y=Read(closest,1,0x261216),pz=Read(point,2,0x26121c),dy=F(py-y),z=Read(closest,2,0x261226),dz=F(pz-z),metric;
     if(over!=0){
      var mesh=Need(Need(query,0x261232).Mesh0.Read(0x261232),0x261241);
      dy=NavigationCoreContracts.FromBits(NavigationCoreContracts.ToBits(dy)&mask);
      mesh.RequireLive(0x261241);uint shift=mesh.PolyBits60.Read(0x261241)&63;
      mesh.RequireLive(0x261249);uint tileBits=mesh.TileBits5C.Read(0x261249);
      ulong tileIndex=(((ulong)reference)>>(int)shift)&Mask(tileBits);
      mesh.RequireLive(0x261254);var tiles=mesh.Tiles50.Read(0x261254);var tile=Entry(tiles,(long)tileIndex,0x26125c);
      var header=tile.Header08.Read(0x26125c);float climb=Header(header,0x261261,b).Climb44.Read(0x261261);
      dy=F(dy-climb);metric=dy>0f?F(dy*dy):0f; // JBE also sends unordered to +0.
     }else{float y2=F(dy*dy);dx=F(dx*dx);dz=F(dz*dz);metric=F(F(dx+y2)+dz);}
     if(!(metric>=best)){
      if(optionalOutput!=null){Cell(optionalOutput,0,0x26129c).Write(x);Cell(optionalOutput,1,0x2612a0).Write(y);Cell(optionalOutput,2,0x2612a5).Write(z);}
      best=metric;selected=reference;
     }
    }
   }
   invocation.At=0x261305;resultRef.Write(selected);return NavigationCoreContracts.Success;
  }
  public static uint Nearest2610F0(NavigationQuery query,NavigationPoint3 point,NavigationPoint3 extent,NavigationQueryFilter filter,Field<uint> resultRef,NavigationPoint3 optionalOutput,NavigationNearestBoundaries b,Invocation invocation){return Nearest(query,point,extent,filter,resultRef,optionalOutput,b,invocation);}
 }
}
