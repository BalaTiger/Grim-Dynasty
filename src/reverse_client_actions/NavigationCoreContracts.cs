using System;
namespace GrimDynasty.ReverseClient.Actions {
 internal sealed class NavigationLifetime { internal bool Released; }
 public class NavigationView {
  internal readonly NavigationLifetime Life;
  public NavigationView(){Life=new NavigationLifetime();}
  internal NavigationView(NavigationLifetime life){Life=life;}
  public void MarkReleased(){Life.Released=true;}
  public void RequireLive(uint at){if(Life.Released)throw new MissingContract(at,"Own navigation view explicitly released; native lifetime is not inferred");}
 }
 public sealed class NavigationPoint3:NavigationView {
  readonly Field<float> x,y,z;readonly NavigationPointBuffer buffer;readonly int scalar;readonly bool deferred;
  public Field<float> X{get{return CellAt(0,0);}} public Field<float> Y{get{return CellAt(1,0);}} public Field<float> Z{get{return CellAt(2,0);}}
  public NavigationPoint3(){x=new Field<float>();y=new Field<float>();z=new Field<float>();}
  public NavigationPoint3(Field<float> x,Field<float> y,Field<float> z){if(x==null||y==null||z==null)throw new ArgumentNullException("point cell");this.x=x;this.y=y;this.z=z;}
  // LEA forms an address; neither the buffer nor its cells are read until use.
  internal NavigationPoint3(NavigationPointBuffer buffer,int scalar):base(buffer==null?new NavigationLifetime():buffer.Life){this.buffer=buffer;this.scalar=scalar;deferred=true;}
  internal Field<float> CellAt(int component,uint at){RequireLive(at);if(!deferred)return component==0?x:component==1?y:z;if(buffer==null)throw new MissingContract(at,"Null deferred point buffer");return buffer.At((long)scalar+component,at);}
 }
 public sealed class NavigationRefBuffer:NavigationView {
  public readonly Field<uint>[] Cells;
  public NavigationRefBuffer(int length){if(length<0)throw new ArgumentOutOfRangeException("length");Cells=new Field<uint>[length];for(int n=0;n<length;n++)Cells[n]=new Field<uint>();}
  public NavigationRefBuffer(Field<uint>[] cells){if(cells==null)throw new ArgumentNullException("cells");Cells=(Field<uint>[])cells.Clone();for(int n=0;n<Cells.Length;n++)if(Cells[n]==null)throw new ArgumentException("Null buffer cell");}
  public Field<uint> At(long index,uint at){RequireLive(at);if(index<0||index>=Cells.Length)throw new MissingContract(at,"Outside declared own reference-buffer extent");return Cells[(int)index];}
 }
 public sealed class NavigationPointBuffer:NavigationView {
  public readonly Field<float>[] Cells;
  public NavigationPointBuffer(int pointCapacity){if(pointCapacity<0)throw new ArgumentOutOfRangeException("pointCapacity");Cells=new Field<float>[checked(pointCapacity*3)];for(int n=0;n<Cells.Length;n++)Cells[n]=new Field<float>();}
  public NavigationPointBuffer(Field<float>[] scalarCells){if(scalarCells==null)throw new ArgumentNullException("scalarCells");Cells=(Field<float>[])scalarCells.Clone();for(int n=0;n<Cells.Length;n++)if(Cells[n]==null)throw new ArgumentException("Null buffer cell");}
  public Field<float> At(long index,uint at){RequireLive(at);if(index<0||index>=Cells.Length)throw new MissingContract(at,"Outside declared own point-buffer extent");return Cells[(int)index];}
  public NavigationPoint3 PointAtScalar(int index,uint at){return new NavigationPoint3(this,index);}
 }
 public sealed class NavigationFlagBuffer:NavigationView {
  public readonly Field<byte>[] Cells;
  public NavigationFlagBuffer(int length){if(length<0)throw new ArgumentOutOfRangeException("length");Cells=new Field<byte>[length];for(int n=0;n<length;n++)Cells[n]=new Field<byte>();}
  public Field<byte> At(long index,uint at){RequireLive(at);if(index<0||index>=Cells.Length)throw new MissingContract(at,"Outside declared own flag-buffer extent");return Cells[(int)index];}
 }
 public sealed class NavigationQuery:NavigationView { public readonly Field<NavigationMesh> Mesh0=new Field<NavigationMesh>(); }
 public sealed class NavigationMesh:NavigationView {
  public readonly Field<uint> PolyBits60=new Field<uint>(),TileBits5C=new Field<uint>(),SaltBits58=new Field<uint>(),TileLimit30=new Field<uint>();
  public readonly Field<NavigationTile[]> Tiles50=new Field<NavigationTile[]>();
 }
 public sealed class NavigationTile:NavigationView {
  public readonly Field<uint> Salt00=new Field<uint>();
  public readonly Field<NavigationTileHeader> Header08=new Field<NavigationTileHeader>();
  public readonly Field<NavigationPoly[]> Polys10=new Field<NavigationPoly[]>();
  public readonly Field<NavigationPoint3[]> Vertices18=new Field<NavigationPoint3[]>();
 }
 public sealed class NavigationTileHeader:NavigationView { public readonly Field<uint> PolyCount18=new Field<uint>(); }
 public sealed class NavigationPoly:NavigationView {
  public readonly Field<byte> VertexCount1E=new Field<byte>();
  public readonly Field<ushort>[] Indices04=new Field<ushort>[6];
  public NavigationPoly(){for(int n=0;n<Indices04.Length;n++)Indices04[n]=new Field<ushort>();}
 }
 public class NavigationCoreBoundaries {
  public readonly Field<float> Epsilon422BA4=new Field<float>();
  public readonly Field<float> One34BB44=new Field<float>(),MaxFinite34BDCC=new Field<float>();
  bool equalityReady;
  // Explicit OWN warm-state establishment from262480's observed31800000 write.
  // This does not execute/recreate TLS, CRT locks or the25F770 slow path.
  public void UseObservedEqualityConstant(){Epsilon422BA4.Write(NavigationCoreContracts.FromBits(0x31800000));equalityReady=true;}
  // Explicit own constants from saved original bytes, not an assertion about
  // mutable loaded memory. 34BB44: offline-actor-remaining-dependencies;
  // 34BDCC/34B938: r1-navigation-constants. No live process is loaded here.
  public void UseObservedGeometryConstants(){One34BB44.Write(NavigationCoreContracts.FromBits(0x3f800000));MaxFinite34BDCC.Write(NavigationCoreContracts.FromBits(0x7f7fffff));}
  internal void RequireEqualityReady(){if(!equalityReady)throw new MissingContract(0,"Own equality warm state not explicitly established; native TLS initialization remains outside scope");}
  public virtual byte ValidateRef2688C0(NavigationMesh mesh,uint reference){return NavigationCoreContracts.ValidateReference(mesh,reference);}
  public virtual uint Clamp260AD0(NavigationQuery query,uint reference,NavigationPoint3 point,NavigationPoint3 output){return NavigationCoreContracts.Clamp(query,reference,point,output,this);}
  public virtual byte Equal25F770(NavigationPoint3 a,NavigationPoint3 b){return NavigationCoreContracts.Equal(a,b,this);}
  public virtual uint Append2623B0(NavigationPoint3 point,byte flag,NavigationPointBuffer points,NavigationFlagBuffer flags,Field<uint> count,int capacity){return NavigationCoreContracts.Append(point,flag,points,flags,count,capacity,this);}
 }
 // Native observations projected into independent/shared same-typed cells.
 // No mesh loading, nearest search, arbitrary byte aliases, native allocation,
 // concurrent lifetime or CPU MXCSR behavior is implied by these own views.
 public static class NavigationCoreContracts {
  public const uint Failure=0x80000008,Success=0x40000000,Continue=0x20000000;
  public static float FromBits(uint x){return BitConverter.ToSingle(BitConverter.GetBytes(x),0);}
  public static uint ToBits(float x){return BitConverter.ToUInt32(BitConverter.GetBytes(x),0);}
  public static float F(float x){return BitConverter.ToSingle(BitConverter.GetBytes(x),0);}
  static T Need<T>(T x,uint at)where T:class{if(x==null)throw new MissingContract(at,"Null view outside supported native-address domain");var view=x as NavigationView;if(view!=null)view.RequireLive(at);return x;}
  static T Entry<T>(T[] a,uint index,uint at)where T:class{if(a==null||index>=a.Length)throw new MissingContract(at,"Outside declared own table extent");return Need(a[(int)index],at);}
  static uint Mask(uint bits){return unchecked((1U<<(int)(bits&31))-1U);}
  static Field<float> Cell(NavigationPoint3 p,int component,uint at){return Need(p,at).CellAt(component,at);}
  static float Read(NavigationPoint3 p,int component,uint at){return Cell(p,component,at).Read(at);}
  public static byte ValidateReference(NavigationMesh mesh,uint reference){
   if(reference==0)return 0;
   Need(mesh,0x2688d5);uint polyBits=mesh.PolyBits60.Read(0x2688d5);
   mesh.RequireLive(0x2688e6);uint tileBits=mesh.TileBits5C.Read(0x2688e6);
   uint tileIndex=(reference>>(int)(polyBits&31))&Mask(tileBits);
   mesh.RequireLive(0x2688f9);if(tileIndex>=mesh.TileLimit30.Read(0x2688f9))return 0;
   mesh.RequireLive(0x268921);uint saltBits=mesh.SaltBits58.Read(0x268921);
   mesh.RequireLive(0x268925);var tiles=mesh.Tiles50.Read(0x268925);
   var tile=Entry(tiles,tileIndex,0x26892f);uint salt=(reference>>(int)(unchecked(tileBits+polyBits)&31))&Mask(saltBits);
   if(tile.Salt00.Read(0x26892f)!=salt)return 0;
   tile.RequireLive(0x268933);var header=tile.Header08.Read(0x268933);if(header==null)return 0;
   Need(header,0x268951);return (byte)(((reference&Mask(polyBits))<header.PolyCount18.Read(0x268951))?1:0);
  }
  public static byte Equal(NavigationPoint3 a,NavigationPoint3 b,NavigationCoreBoundaries boundaries){
   Need(boundaries,0);boundaries.RequireEqualityReady();
   float z=Read(b,2,0x25f7a5),x=Read(b,0,0x25f7aa),y=Read(b,1,0x25f7ae);
   y=F(y-Read(a,1,0x25f7b3));z=F(z-Read(a,2,0x25f7b8));x=F(x-Read(a,0,0x25f7bd));
   y=F(y*y);z=F(z*z);x=F(x*x);float sum=F(F(y+x)+z);
   float epsilon=boundaries.Epsilon422BA4.Read(0x25f7da);
   return (byte)(epsilon>sum?1:0); // COMISS epsilon,sum; SETA: unordered false.
  }
  public static uint Append(NavigationPoint3 point,byte flag,NavigationPointBuffer points,NavigationFlagBuffer flags,Field<uint> count,int capacity,NavigationCoreBoundaries b){
   int n=unchecked((int)Need(count,0x2623d6).Read(0x2623d6));
   if(n>0){
    int last=unchecked(n*3-3);var prior=new NavigationPoint3(points,last);
    if(b.Equal25F770(prior,point)!=0){
     if(flags!=null){int index=unchecked((int)count.Read(0x2623fc)-1);flags.At(index,0x262402).Write(flag);}
     return Continue;
    }
   }
   n=unchecked((int)count.Read(0x26240d));int scalar=unchecked(n*3);
   var value=Cell(point,0,0x262412).Snapshot();Need(points,0x26241c).At(scalar,0x26241c).CopyFrom(value);
   value=Cell(point,1,0x262420).Snapshot();points.At((long)scalar+1,0x262423).CopyFrom(value);
   value=Cell(point,2,0x262428).Snapshot();points.At((long)scalar+2,0x26242b).CopyFrom(value);
   if(flags!=null)flags.At(unchecked((int)count.Read(0x262435)),0x262438).Write(flag);
   count.Write(unchecked(count.Read(0x26243c)+1));n=unchecked((int)count.Read(0x26243e));
   if(flag==2||n>=capacity)return Success|(n>=capacity?0x10U:0U);
   return Continue;
  }
  public static uint FindPath(NavigationQuery query,uint startRef,uint endRef,NavigationPoint3 from,NavigationPoint3 to,object filter,NavigationRefBuffer corridor,Field<uint> count,int capacity,NavigationCoreBoundaries b,Invocation i){
   i.At=0x261cdb;Need(count,i.At).Write(0);
   if(startRef==0||endRef==0)return Failure;
   i.At=0x261cf8;var mesh=Need(query,i.At).Mesh0.Read(i.At);
   i.At=0x261cfe;if(b.ValidateRef2688C0(mesh,startRef)==0)return Failure;
   i.At=0x261d10;if(b.ValidateRef2688C0(mesh,endRef)==0)return Failure;
   if(startRef!=endRef){i.At=0x261d51;throw new MissingContract(i.At,"Unsupported multi-polygon pool/heap search; validated prefix retained");}
   i.At=0x261d29;Need(corridor,i.At).At(0,i.At).Write(startRef);
   i.At=0x261d30;count.Write(1);return Success; // capacity/from/to/filter not read on this branch.
  }
  public static uint StraightPath(NavigationQuery query,NavigationPoint3 from,NavigationPoint3 to,NavigationRefBuffer corridor,int pathCount,NavigationPointBuffer points,NavigationFlagBuffer flags,Field<uint> count,int capacity,NavigationCoreBoundaries b,Invocation i){
   i.At=0x2624ba;Need(count,i.At).Write(0);
   i.At=0x2624be;uint first=Need(corridor,i.At).At(0,i.At).Read(i.At);if(first==0)return Failure;
   var start=new NavigationPoint3();var end=new NavigationPoint3();
   i.At=0x2624df;uint status=b.Clamp260AD0(query,first,from,start);if((status&0x80000000)!=0)return Failure;
   i.At=0x262506;uint last=corridor.At(unchecked(pathCount-1),i.At).Read(i.At);
   i.At=0x26250c;status=b.Clamp260AD0(query,last,to,end);if((status&0x80000000)!=0)return Failure;
   i.At=0x262552;status=b.Append2623B0(start,1,points,flags,count,capacity);if(status!=Continue)return status;
   if(pathCount>1){i.At=0x2625c2;throw new MissingContract(i.At,"Unsupported multi-polygon funnel; initial append prefix retained");}
   i.At=0x262bc6;int n=unchecked((int)count.Read(i.At));bool append=true;
   if(n>0){int scalar=unchecked(n*3-3);var prior=new NavigationPoint3(points,scalar);i.At=0x262bdf;append=b.Equal25F770(prior,end)==0;}
   if(!append){if(flags!=null){int index=unchecked((int)count.Read(0x262bed)-1);flags.At(index,0x262bf5).Write(2);}}
   else{
    i.At=0x262bfb;int scalar=unchecked((int)count.Read(i.At)*3);
    i.At=0x262bff;var x=Cell(end,0,i.At).Snapshot();i.At=0x262c04;var y=Cell(end,1,i.At).Snapshot();
    i.At=0x262c0e;Need(points,i.At).At(scalar,i.At).CopyFrom(x);
    i.At=0x262c13;var z=Cell(end,2,i.At).Snapshot();i.At=0x262c18;points.At((long)scalar+2,i.At).CopyFrom(z);
    i.At=0x262c1e;points.At((long)scalar+1,i.At).CopyFrom(y);
    if(flags!=null){i.At=0x262c29;int index=unchecked((int)count.Read(i.At));i.At=0x262c2d;flags.At(index,i.At).Write(2);}
    i.At=0x262c31;count.Write(unchecked(count.Read(i.At)+1));
   }
   i.At=0x262c35;return Success|(unchecked((int)count.Read(i.At))>=capacity?0x10U:0U);
  }
  static float VertexRead(NavigationPoint3[] vertices,int index,int component,uint at){return Read(Entry(vertices,unchecked((uint)index),at),component,at);}
  static Field<float> Scalar(Field<float>[] cells,int index,uint at){return Entry(cells,unchecked((uint)index),at);}
  // 26D730: exact XZ-only arithmetic, writes parameter before distance, and
  // supports same-typed shared cells by retaining every native reread. FP trap
  // modes, arbitrary byte overlap and concurrent mutations remain outside scope.
  public static byte PolygonGeometry(NavigationPoint3 point,NavigationPoint3[] vertices,int count,Field<float>[] distances,Field<float>[] parameters,NavigationCoreBoundaries b){
   if(count<=0)return 0;
   float one=Need(b,0x26d78d).One34BB44.Read(0x26d78d);bool inside=false;int previous=count-1;
   for(int current=0;current<count;current++){
    float pz=Read(point,2,0x26d7b0),az=VertexRead(vertices,previous,2,0x26d7b8),bz=VertexRead(vertices,current,2,0x26d7c2);
    if((az>pz)!=(bz>pz)){
     float crossing=VertexRead(vertices,previous,0,0x26d7d7);
     crossing=F(crossing-VertexRead(vertices,current,0,0x26d7e0));
     float dzPoint=F(pz-bz),dzEdge=F(az-bz);
     crossing=F(crossing*dzPoint);crossing=F(crossing/dzEdge);
     crossing=F(crossing+VertexRead(vertices,current,0,0x26d7f9));
     if(crossing>Read(point,0,0x26d7ff))inside=!inside;
    }
    float dx=VertexRead(vertices,current,0,0x26d80b),dz=F(bz-az);
    dx=F(dx-VertexRead(vertices,previous,0,0x26d815));float relativeZ=F(pz-az);
    float lenZ=F(dz*dz),lenX=F(dx*dx),numerZ=F(relativeZ*dz),length=F(lenZ+lenX);
    float relativeX=Read(point,0,0x26d835);relativeX=F(relativeX-VertexRead(vertices,previous,0,0x26d839));
    float numerX=F(relativeX*dx),numerator=F(numerZ+numerX);
    Scalar(parameters,previous,0x26d84a).Write(numerator);
    if(length>0f)Scalar(parameters,previous,0x26d859).Write(F(numerator/length));
    float t=Scalar(parameters,previous,0x26d85f).Read(0x26d85f);
    if(!(t>=0f))Scalar(parameters,previous,0x26d86a).Write(FromBits(0)); // negative OR unordered
    else if(t>one)Scalar(parameters,previous,0x26d876).Write(FromBits(0x3f800000));
    t=Scalar(parameters,previous,0x26d87e).Read(0x26d87e);
    float residualZ=F(t*dz),residualX=F(t*dx);
    residualZ=F(residualZ+VertexRead(vertices,previous,2,0x26d88f));
    residualX=F(residualX+VertexRead(vertices,previous,0,0x26d896));
    residualZ=F(residualZ-Read(point,2,0x26d8a4));residualX=F(residualX-Read(point,0,0x26d8a9));
    residualZ=F(residualZ*residualZ);residualX=F(residualX*residualX);
    Scalar(distances,previous,0x26d8b9).Write(F(residualZ+residualX));previous=current;
   }
   return (byte)(inside?1:0);
  }
  static void GatherVertex(NavigationPoly poly,int polyIndex,NavigationPoint3[] vertices,NavigationPoint3 destination,uint indexAt,uint xAt,uint yAt,uint zAt){
   poly.RequireLive(indexAt);uint index=poly.Indices04[polyIndex].Read(indexAt);
   var value=Cell(Entry(vertices,index,xAt),0,xAt).Snapshot();destination.X.CopyFrom(value);
   value=Cell(Entry(vertices,index,yAt),1,yAt).Snapshot();destination.Y.CopyFrom(value);
   value=Cell(Entry(vertices,index,zAt),2,zAt).Snapshot();destination.Z.CopyFrom(value);
  }
  // 260AD0: decode first, gather the native six-vertex scratch domain, then
  // reproduce26D730 and the outside-edge interpolation. A >6 count exceeds
  // this own declared scratch view; it is not a fabricated native failure code.
  public static uint Clamp(NavigationQuery query,uint reference,NavigationPoint3 point,NavigationPoint3 output,NavigationCoreBoundaries b){
   // The MOV captures unknown pointer bits too; the zero-ref branch never
   // consumes them. Require their value only at the first actual dereference.
   var meshBits=Need(query,0x260af1).Mesh0.Snapshot();if(reference==0)return Failure;
   var mesh=meshBits.Read(0x260b10);
   Need(mesh,0x260b10);uint tileBits=mesh.TileBits5C.Read(0x260b10);
   mesh.RequireLive(0x260b1a);uint polyBits=mesh.PolyBits60.Read(0x260b1a);
   uint polyIndex=reference&Mask(polyBits),tileIndex=(reference>>(int)(polyBits&31))&Mask(tileBits);
   mesh.RequireLive(0x260b3c);if(tileIndex>=mesh.TileLimit30.Read(0x260b3c))return Failure;
   mesh.RequireLive(0x260b4b);var tiles=mesh.Tiles50.Read(0x260b4b);
   mesh.RequireLive(0x260b55);uint saltBits=mesh.SaltBits58.Read(0x260b55);
   var tile=Entry(tiles,tileIndex,0x260b61);uint salt=(reference>>(int)(unchecked(tileBits+polyBits)&31))&Mask(saltBits);
   if(tile.Salt00.Read(0x260b61)!=salt)return Failure;
   tile.RequireLive(0x260b66);var header=tile.Header08.Read(0x260b66);if(header==null)return Failure;
   Need(header,0x260b6f);if(polyIndex>=header.PolyCount18.Read(0x260b6f))return Failure;
   tile.RequireLive(0x260b7f);var polys=tile.Polys10.Read(0x260b7f);
   var poly=Entry(polys,polyIndex,0x260ba0);int n=poly.VertexCount1E.Read(0x260ba0);
   if(n>6)throw new MissingContract(0x260ba0,"Polygon exceeds own six-index/scratch domain; no native validation guard inferred");
   var local=new NavigationPoint3[n];var distances=new Field<float>[n];var parameters=new Field<float>[n];
   for(int k=0;k<n;k++){local[k]=new NavigationPoint3();distances[k]=new Field<float>();parameters[k]=new Field<float>();}
   int gathered=0;
   if(n>=4){
    tile.RequireLive(0x260bb5);var vertices=tile.Vertices18.Read(0x260bb5);
    GatherVertex(poly,0,vertices,local[0],0x260be0,0x260bf1,0x260bf8,0x260c00);
    GatherVertex(poly,1,vertices,local[1],0x260c08,0x260c11,0x260c18,0x260c20);
    GatherVertex(poly,2,vertices,local[2],0x260c28,0x260c31,0x260c38,0x260c40);
    GatherVertex(poly,3,vertices,local[3],0x260c48,0x260c51,0x260c58,0x260c60);gathered=4;
   }
   if(gathered<n){tile.RequireLive(0x260c77);var vertices=tile.Vertices18.Read(0x260c77);for(int k=gathered;k<n;k++)GatherVertex(poly,k,vertices,local[k],0x260c99,0x260ca9,0x260cb0,0x260cb8);}
   if(PolygonGeometry(point,local,n,distances,parameters,b)!=0){
    var value=Cell(point,0,0x260cec).Snapshot();Cell(output,0,0x260cee).CopyFrom(value);
    value=Cell(point,1,0x260cf2).Snapshot();Cell(output,1,0x260cf5).CopyFrom(value);
    value=Cell(point,2,0x260cfa).Snapshot();Cell(output,2,0x260cfd).CopyFrom(value);return Success;
   }
   float best=Need(b,0x260d07).MaxFinite34BDCC.Read(0x260d07);int bestIndex=-1,kScan=0;
   if(n>=4){uint[] readAt={0x260d30,0x260d41,0x260d53,0x260d64};for(int k=0;k<4;k++){float d=distances[k].Read(readAt[k]);if(!(d>=best)){best=d;bestIndex=k;}}kScan=4;}
   for(;kScan<n;kScan++){float d=distances[kScan].Read(0x260d8a);if(!(d>=best)){best=d;bestIndex=kScan;}}
   if(n==0)throw new MissingContract(0x260db1,"Native divisor is zero outside own supported geometry domain");
   int next=unchecked(bestIndex+1)%n;
   float ax=VertexRead(local,bestIndex,0,0x260db4),ay=VertexRead(local,bestIndex,1,0x260dbb);
   float x=VertexRead(local,next,0,0x260dcb),t=Scalar(parameters,bestIndex,0x260dd1).Read(0x260dd1);
   x=F(F(F(x-ax)*t)+ax);
   float y=F(VertexRead(local,next,1,0x260de6)-ay);Cell(output,0,0x260df0).Write(x);
   float az=VertexRead(local,bestIndex,2,0x260df6);y=F(F(y*t)+ay);
   float z=F(VertexRead(local,next,2,0x260e05)-az);Cell(output,1,0x260e0f).Write(y);
   z=F(F(z*t)+az);Cell(output,2,0x260e1e).Write(z);return Success;
  }
 }
}
