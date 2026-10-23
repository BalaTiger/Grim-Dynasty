using System;

namespace GrimDynasty.ReverseClient.Actions {
 public enum NavigationPolygonOutcome { Success, NativeFailure, OwnUnsupported }
 public sealed class NavigationPolygonDomainException : InvalidOperationException {
  public readonly string Code; public readonly uint Rva;
  internal NavigationPolygonDomainException(string code,uint rva):base(code+" at Engine RVA "+rva.ToString("x")){Code=code;Rva=rva;}
 }
 // Explicit borrowed input for own fixtures/platform callers. Production inputs
 // retain the actual mutable contour record and reread its fields via this view.
 public sealed class NavigationPolygonContourInput {
  readonly byte[] buffer; readonly int count; readonly byte area;
  public readonly NavigationLayerContour Record;
  public NavigationPolygonContourInput(byte[] quadBuffer,int vertexCount,byte areaId){buffer=quadBuffer;count=vertexCount;area=areaId;}
  internal NavigationPolygonContourInput(NavigationLayerContour record){Record=record;}
  public byte[] Vertices{get{return Record==null?buffer:Record.Vertices;}}
  public int VertexCount{get{return Record==null?count:Record.VertexCount;}}
  public byte Area{get{return Record==null?area:Record.Area;}}
 }
 public sealed class NavigationPolygonScratch {
  public byte[] RemovalFlags;
  public ushort[] HashHeads,VertexLinks,Indices,Triangles,LocalPolygons;
  public int SignedTriangleCount,LocalPolygonCount;
 }
 public sealed class NavigationPolygonResult {
  public readonly NavigationContourResult Source;
  public readonly NavigationPolygonContourInput[] Inputs;
  public readonly NavigationPolygonScratch Scratch=new NavigationPolygonScratch();
  public int Nvp{get;internal set;}
  public int VertexCount{get;internal set;}
  public int PolygonCount{get;internal set;}
  public ushort[] Vertices{get;internal set;}
  public ushort[] Polygons{get;internal set;}
  public ushort[] Flags{get;internal set;}
  public byte[] Areas{get;internal set;}
  public NavigationPolygonOutcome Outcome{get;internal set;}
  public uint? NativeStatus{get;internal set;}
  public string Detail{get;internal set;}
  public uint StopRva{get;internal set;}
  public int ContourIndex{get;internal set;}
  internal NavigationPolygonResult(NavigationContourResult source,NavigationPolygonContourInput[] inputs){Source=source;Inputs=inputs;ContourIndex=-1;}
 }

 // Engine26BD40 +26AB20/26AE40/26B120/26A0C0, saved Rea pseudo/assembly in
 // r1-tile-production-direct-stages and r1-nav-publish-and-polygon-core.
 // Triangulation predicates26A9C0/26A860/26A7D0/26A650/26A730 are traced in
 // r1-mesh-tile-and-predicates, r1-mesh-links-and-triangulation-leaf and
 // r1-triangulation-and-tile-connection. No generic triangulator is substituted.
 // Engine SHA2564c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153.
 // Own platform domain: stable borrowed inputs for this synchronous call,
 // managed allocations and fresh output object. No native allocator callback,
 // pointer lifetime, concurrent mutation or byte-overlap ABI is emulated.
 // Allocation failure throws; zero-length managed buffers are valid. Neither is
 // reported as a native allocator-null failure. Arrays retain capacity and all
 // completed writes on OwnUnsupported. Only counts identify initialized output.
 public static class NavigationLayerPolygons {
  public const uint SuccessStatus=0x40000000u;
  const ushort None=65535;
  static NavigationPolygonDomainException Stop(string code,uint rva){return new NavigationPolygonDomainException(code,rva);}
  static ushort U(ushort[] a,int at,uint rva){if(a==null||at<0||at>=a.Length)throw Stop("UshortExtent",rva);return a[at];}
  static void UW(ushort[] a,int at,ushort value,uint rva){if(a==null||at<0||at>=a.Length)throw Stop("UshortExtent",rva);a[at]=value;}
  static void StoreRange(ushort[] a,int at,int count,uint rva){if(a==null||at<0||(long)at+count>a.Length)throw Stop("UshortStoreExtent",rva);}
  static byte B(byte[] a,int at,uint rva){if(a==null||at<0||at>=a.Length)throw Stop("QuadExtent",rva);return a[at];}
  static int Next(int i,int n){return i+1<n?i+1:0;}
  static int Prev(int i,int n){return i>0?i-1:n-1;}
  static int Count(ushort[] a,int offset,uint rva){int n=0;while(n<6&&U(a,offset+n,rva)!=None)n++;return n;}
  static void Fill(ushort[] a,ushort value){for(int i=0;i<a.Length;i++)a[i]=value;}
  static int Cross(ushort[] v,int p,int q,int r,uint at){
   int px=U(v,p*3,at),pz=U(v,p*3+2,at),qx=U(v,q*3,at),qz=U(v,q*3+2,at),rx=U(v,r*3,at),rz=U(v,r*3+2,at);
   return unchecked((rz-pz)*(qx-px)-(qz-pz)*(rx-px));
  }

  //26AE40. ref outputs deliberately remain references: callers may supply the
  // same cell. Last matching A edge / first matching B edge wins, then outputs
  // are reread. Too many vertices returns before either output is touched.
  public static int MergeScore(ushort[] a,int ao,ushort[] b,int bo,ushort[] vertices,ref int edgeA,ref int edgeB){
   int na=Count(a,ao,0x26ae64),nb=Count(b,bo,0x26ae86);
   if(na+nb-2>6)return -1;
   edgeA=-1;edgeB=-1;
   for(int i=0;i<na;i++){
    ushort a0=U(a,ao+i,0x26aef2),a1=U(a,ao+Next(i,na),0x26aef2);if(a0>a1){ushort t=a0;a0=a1;a1=t;}
    for(int j=0;j<nb;j++){
     ushort b0=U(b,bo+j,0x26af32),b1=U(b,bo+Next(j,nb),0x26af32);if(b0>b1){ushort t=b0;b0=b1;b1=t;}
     if(a0==b0&&a1==b1){edgeA=i;edgeB=j;break;}
    }
   }
   if(edgeA<0||edgeB<0)return -1;
   int ea=edgeA,eb=edgeB;
   if(ea>=na||eb>=nb)throw Stop("MergeOutputAliasIndex",0x26af91);
   int p=U(a,ao+Prev(ea,na),0x26afb1),q=U(a,ao+ea,0x26afbb),r=U(b,bo+((eb+2)%nb),0x26afcf);
   if(Cross(vertices,p,q,r,0x26b01d)>=0)return -1;
   p=U(b,bo+Prev(eb,nb),0x26b042);q=U(b,bo+eb,0x26b04c);r=U(a,ao+((ea+2)%na),0x26b060);
   if(Cross(vertices,p,q,r,0x26b09e)>=0)return -1;
   p=U(a,ao+ea,0x26b0b3);q=U(a,ao+Next(ea,na),0x26b0cc);
   int dx=U(vertices,p*3,0x26b0e6)-U(vertices,q*3,0x26b0e6),dz=U(vertices,p*3+2,0x26b0ef)-U(vertices,q*3+2,0x26b0ef);
   return unchecked(dx*dx+dz*dz);
  }
  //26B120 reads both input sequences before writing six words to A. A and B may
  // share the managed array. This helper itself does NOT enforce merge<=6.
  public static void Merge(ushort[] a,int ao,ushort[] b,int bo,int edgeA,int edgeB){
   int na=Count(a,ao,0x26b146),nb=Count(b,bo,0x26b166),used=0;ushort[] temporary=new ushort[12];Fill(temporary,None);
   for(int k=0;k<na-1;k++){
    int index=unchecked(edgeA+1+k)%na;if(index<0)throw Stop("NegativeMergeIndex",0x26b1c8);
    temporary[used++]=U(a,ao+index,0x26b1d0);
   }
   for(int k=0;k<nb-1;k++){
    int index=unchecked(edgeB+1+k)%nb;if(index<0)throw Stop("NegativeMergeIndex",0x26b213);
    temporary[used++]=U(b,bo+index,0x26b21b);
   }
   StoreRange(a,ao,4,0x26b247);for(int k=0;k<4;k++)a[ao+k]=temporary[k];
   StoreRange(a,ao+4,2,0x26b24b);for(int k=4;k<6;k++)a[ao+k]=temporary[k];
  }

  //26A9C0: byte XZ coordinates, 15-bit indices, convex and reflex branches.
  // Keep the native load/short-circuit order, including JNS on subtraction.
  static bool InCone(int i,int j,int count,byte[] vertices,ushort[] indices){
   int current=(U(indices,i,0x26a9e5)&0x7fff)*4,target=(U(indices,j,0x26a9ea)&0x7fff)*4;
   int next=(U(indices,Next(i,count),0x26aa0b)&0x7fff)*4;
   int cz=B(vertices,current+2,0x26aa13),nx=B(vertices,next,0x26aa1f),cx=B(vertices,current,0x26aa2c);
   int previous=(U(indices,Prev(i,count),0x26aa39)&0x7fff)*4;
   int nz=B(vertices,next+2,0x26aa3d),pz=B(vertices,previous+2,0x26aa4c),px=B(vertices,previous,0x26aa52);
   int cross=unchecked((cx-px)*(nz-pz)-(nx-px)*(cz-pz));
   if(cross<=0){
    int tx=B(vertices,target,0x26aa72),tz=B(vertices,target+2,0x26aa79);
    if(unchecked((tx-cx)*(pz-cz)-(tz-cz)*(px-cx))>=0)return false;
    if(unchecked((nz-tz)*(cx-tx)-(cz-tz)*(nx-tx))>=0)return false;
   }else{
    int tz=B(vertices,target+2,0x26aab1),tx=B(vertices,target,0x26aab9);
    if(unchecked((tx-cx)*(nz-cz)-(nx-cx)*(tz-cz))>0)return true;
    if(unchecked((cx-tx)*(pz-tz)-(cz-tz)*(px-tx))<=0)return false;
   }
   return true;
  }
  //26A860: nonincident edges only; coordinate-equal endpoints skip the
  // intersection helper even when their original vertex indices differ.
  static bool Diagonal(int i,int j,int count,byte[] vertices,ushort[] indices){
   int a=(U(indices,i,0x26a88d)&0x7fff)*4,b=(U(indices,j,0x26a89f)&0x7fff)*4;
   for(int edge=0;edge<count;edge++){
    int end=Next(edge,count);if(edge==i||end==i||edge==j||end==j)continue;
    int c=(U(indices,edge,0x26a8f8)&0x7fff)*4,ax=B(vertices,a,0x26a8fd);
    int d=(U(indices,end,0x26a90e)&0x7fff)*4,cx=B(vertices,c,0x26a91b);
    if(ax==cx){int cz=B(vertices,c+2,0x26a92f);if(B(vertices,a+2,0x26a934)==cz)continue;}
    int bx=B(vertices,b,0x26a93a);
    if(bx==cx){int cz=B(vertices,c+2,0x26a941);if(B(vertices,b+2,0x26a946)==cz)continue;}
    int dx=B(vertices,d,0x26a94b);
    if(ax==dx){int dz=B(vertices,d+2,0x26a954);if(B(vertices,a+2,0x26a959)==dz)continue;}
    if(bx==dx){int dz=B(vertices,d+2,0x26a963);if(B(vertices,b+2,0x26a968)==dz)continue;}
    if(Intersect(vertices,a,b,c,d))return false;
   }
   return true;
  }
  //26A973 supplies RCX/RDX/R8/R9 = a/b/c/d; the pseudocode omits two args.
  //26A7D0 keeps four-argument 26A650 then four ordered 26A730 short circuits.
  public static bool Intersect(byte[] vertices,int a,int b,int c,int d){
   return IntersectStrict(vertices,a,b,c,d)||Between(vertices,a,b,c)||Between(vertices,a,b,d)||Between(vertices,c,d,a)||Between(vertices,c,d,b);
  }
  //26A650/26A730 consume only AL. Upper return-register bits on an early zero
  // path are unspecified and not promoted to a boolean. Arguments are borrowed
  // byte offsets into quad storage, not native pointers or vertex ordinals.
  public static bool IntersectStrict(byte[] vertices,int a,int b,int c,int d){
   int cx=B(vertices,c,0x26a666),cz=B(vertices,c+2,0x26a66a),az=B(vertices,a+2,0x26a671),ax=B(vertices,a,0x26a679);
   int bx=B(vertices,b,0x26a67f),bz=B(vertices,b+2,0x26a685);
   int crossC=unchecked((cz-az)*(bx-ax)-(cx-ax)*(bz-az));if(crossC==0)return false;
   int dz=B(vertices,d+2,0x26a6a0),dx=B(vertices,d,0x26a6a5);
   int crossD=unchecked((dz-az)*(bx-ax)-(dx-ax)*(bz-az));if(crossD==0)return false;
   int crossA=unchecked((az-cz)*(dx-cx)-(ax-cx)*(dz-cz));if(crossA==0)return false;
   int crossB=unchecked((bz-cz)*(dx-cx)-(bx-cx)*(dz-cz));if(crossB==0)return false;
   return ((crossC<0)!=(crossD<0))&&((crossA<0)!=(crossB<0));
  }
  public static bool Between(byte[] vertices,int a,int b,int c){
   int az=B(vertices,a+2,0x26a73a),bz=B(vertices,b+2,0x26a73e),ax=B(vertices,a,0x26a742);
   int cx=B(vertices,c,0x26a746),cz=B(vertices,c+2,0x26a74a),bx=B(vertices,b,0x26a752);
   if(unchecked((bx-ax)*(cz-az))!=unchecked((cx-ax)*(bz-az)))return false;
   if(ax!=bx){if(ax<=cx){if(cx<=bx)return true;if(ax<cx)return false;}return cx>=bx;}
   if(az<=cz){if(cz<=bz)return true;if(az<cz)return false;}return cz>=bz;
  }
  static bool Ear(int i,int j,int count,byte[] vertices,ushort[] indices){return InCone(i,j,count,vertices,indices)&&Diagonal(i,j,count,vertices,indices);}
  public static int Triangulate(int count,byte[] vertices,ushort[] indices,ushort[] triangles){
   if(count<3||count>32768)throw Stop("TriangulationIndexDomain",0x26ab54);
   int produced=0;
   for(int i=0;i<count;i++){int middle=Next(i,count),end=Next(middle,count);if(Ear(i,end,count,vertices,indices))indices[middle]|=0x8000;}
   while(count>3){
    int best=-1,bestLength=-1;
    for(int i=0;i<count;i++){
     int middle=Next(i,count);if((U(indices,middle,0x26ac18)&0x8000)==0)continue;
     int end=Next(middle,count),a=(U(indices,i,0x26ac1f)&0x7fff)*4,c=(U(indices,end,0x26ac37)&0x7fff)*4;
     int endZ=B(vertices,c+2,0x26ac3f),endX=B(vertices,c,0x26ac45),startX=B(vertices,a,0x26ac4e),startZ=B(vertices,a+2,0x26ac53);
     int dx=endX-startX,dz=endZ-startZ,length=dx*dx+dz*dz;
     if(bestLength<0||length<bestLength){best=i;bestLength=length;}
    }
    if(best<0)return -produced;
    int remove=Next(best,count),endVertex=Next(remove,count);
    UW(triangles,produced*3,(ushort)(U(indices,best,0x26acb3)&0x7fff),0x26acbf);
    UW(triangles,produced*3+1,(ushort)(U(indices,remove,0x26acc4)&0x7fff),0x26accf);
    produced++;
    UW(triangles,produced*3-1,(ushort)(U(indices,endVertex,0x26ace1)&0x7fff),0x26aced);
    count--;
    for(int k=remove;k<count;k++)UW(indices,k,U(indices,k+1,0x26ad11),0x26ad11);
    int current=remove<count?remove:0,previous=Prev(current,count),before=Prev(previous,count);
    bool ear=Ear(before,current,count,vertices,indices);uint at=ear?0x26ad74u:0x26ad83u;ushort value=U(indices,previous,at);
    UW(indices,previous,ear?(ushort)(value|0x8000):(ushort)(value&0x7fff),at);
    ear=Ear(previous,Next(current,count),count,vertices,indices);at=ear?0x26adc8u:0x26addbu;value=U(indices,current,at);
    UW(indices,current,ear?(ushort)(value|0x8000):(ushort)(value&0x7fff),at);
   }
   UW(triangles,produced*3,(ushort)(U(indices,0,0x26adee)&0x7fff),0x26adf6);
   UW(triangles,produced*3+1,(ushort)(U(indices,1,0x26adfb)&0x7fff),0x26ae04);
   UW(triangles,produced*3+2,(ushort)(U(indices,2,0x26ae09)&0x7fff),0x26ae12);
   return produced+1;
  }

  struct Edge {public ushort A,B,SlotA,SlotB,PolyA,PolyB;}
  static void AddEdge(Edge[] edges,ushort[] heads,ushort[] links,ref int count,ushort a,ushort b,int poly,int slot,uint rva){
   if(count>=65535||count>=edges.Length)throw Stop("EdgeIndexDomain",rva);
   if(a>=heads.Length)throw Stop("AdjacencyVertexIndex",rva);
   edges[count].A=a;edges[count].B=b;edges[count].PolyA=(ushort)poly;edges[count].SlotA=(ushort)slot;edges[count].PolyB=(ushort)poly;edges[count].SlotB=255;
   links[count]=heads[a];heads[a]=(ushort)count;count++;
  }
  //26A0C0: two oriented edge passes followed by original contour portal labels.
  // Portal interval overlap is STRICT, Y is not tested, and later matching
  // contour segments overwrite the earlier portal label in stable input order.
  public static void BuildAdjacency(ushort[] polygons,int polygonCount,ushort[] vertices,int vertexCount,NavigationPolygonContourInput[] contours){
   if(polygonCount<0||polygonCount>=65535||vertexCount<0||vertexCount>=65536)throw Stop("AdjacencyCountDomain",0x26a0ed);
   if(contours==null)throw new ArgumentNullException("contours");
   ushort[] heads=new ushort[vertexCount],links=new ushort[polygonCount*6];Fill(heads,None);
   Edge[] edges=new Edge[polygonCount*6];int count=0;
   for(int pass=0;pass<2;pass++)for(int p=0;p<polygonCount;p++)for(int slot=0;slot<6;slot++){
    ushort a=U(polygons,p*12+slot,pass==0?0x26a1a5u:0x26a263u);if(a==None)break;
    ushort b=slot+1<6?U(polygons,p*12+slot+1,0x26a1bd):None;if(b==None)b=U(polygons,p*12,0x26a1c7);
    if(pass==0){if(a<b)AddEdge(edges,heads,links,ref count,a,b,p,slot,0x26a1d8);}
    else if(a>b){
     if(b>=heads.Length)throw Stop("AdjacencyVertexIndex",0x26a2a5);int edge=heads[b],steps=0;
     while(edge!=None){if(edge>=count||++steps>count)throw Stop("AdjacencyLinkDomain",0x26a2b5);
      if(edges[edge].B==a&&edges[edge].PolyA==edges[edge].PolyB)break;edge=links[edge];}
     if(edge==None)AddEdge(edges,heads,links,ref count,b,a,p,slot,0x26a2ec);
     else{edges[edge].PolyB=(ushort)p;edges[edge].SlotB=(ushort)slot;}
    }
   }
   for(int c=0;c<contours.Length;c++){
    NavigationPolygonContourInput contour=contours[c];if(contour==null)throw Stop("NullContour",0x26a368);
    int n=contour.VertexCount;if(n<3)continue;
    for(int j=0;j<n;j++){
     byte[] q=contour.Vertices;int prev=(j==0?n-1:j-1)*4,now=j*4;byte direction=(byte)(B(q,prev+3,0x26a3ac)&15);if(direction==15)continue;
     bool vertical=(direction&0xfd)==0;int fixedComponent=vertical?0:2,rangeComponent=vertical?2:0;
     int fixedValue=B(q,prev+fixedComponent,0x26a3c5),lo=B(q,prev+rangeComponent,0x26a3ca),hi=B(q,now+rangeComponent,0x26a3ce);
     if(lo>hi){int t=lo;lo=hi;hi=t;}
     for(int e=0;e<count;e++)if(edges[e].PolyA==edges[e].PolyB){
      int a=edges[e].A*3,b=edges[e].B*3;
      if(U(vertices,a+fixedComponent,0x26a41d)!=fixedValue||U(vertices,b+fixedComponent,0x26a424)!=fixedValue)continue;
      int elo=U(vertices,a+rangeComponent,0x26a42c),ehi=U(vertices,b+rangeComponent,0x26a42f);if(elo>ehi){int t=elo;elo=ehi;ehi=t;}
      if(lo<ehi&&elo<hi)edges[e].SlotB=direction;
     }
    }
   }
   for(int e=0;e<count;e++){
    Edge edge=edges[e];if(edge.PolyA!=edge.PolyB){
     UW(polygons,edge.PolyA*12+6+edge.SlotA,edge.PolyB,0x26a5a9);
     UW(polygons,edge.PolyB*12+6+edge.SlotB,edge.PolyA,0x26a5b8);
    }else if(edge.SlotB!=255)UW(polygons,edge.PolyA*12+6+edge.SlotA,(ushort)(edge.SlotB|0x8000),0x26a5e9);
   }
  }

  public static NavigationPolygonResult Build(NavigationContourResult source){
   if(source==null)throw new ArgumentNullException("source");
   NavigationPolygonContourInput[] inputs=new NavigationPolygonContourInput[source.Contours.Length];
   for(int i=0;i<inputs.Length;i++)if(source.Contours[i]!=null)inputs[i]=new NavigationPolygonContourInput(source.Contours[i]);
   if(source.Outcome!=NavigationContourOutcome.Success){var stopped=new NavigationPolygonResult(source,inputs);stopped.Outcome=NavigationPolygonOutcome.OwnUnsupported;stopped.Detail="ContourStageNotSuccessful";stopped.StopRva=0x26bd40;return stopped;}
   return BuildCore(source,inputs);
  }
  public static NavigationPolygonResult Build(NavigationPolygonContourInput[] inputs){if(inputs==null)throw new ArgumentNullException("inputs");return BuildCore(null,inputs);}
  static NavigationPolygonResult BuildCore(NavigationContourResult source,NavigationPolygonContourInput[] inputs){
   var result=new NavigationPolygonResult(source,inputs);var scratch=result.Scratch;
   try{
    int total=0,capacity=0,maximum=0;
    for(int c=0;c<inputs.Length;c++){
     if(inputs[c]==null)throw Stop("NullContour",0x26bda0);int n=inputs[c].VertexCount;if(n<3)continue;
     if(n>32768||(long)total+n>=65536)throw Stop("VertexIndexDomain",0x26bda7);
     total+=n;capacity+=n-2;if(n>maximum)maximum=n;
    }
    result.Nvp=6;scratch.RemovalFlags=new byte[total];
    result.Vertices=new ushort[total*3];result.Polygons=new ushort[capacity*12];result.Areas=new byte[capacity];result.Flags=new ushort[capacity];
    result.VertexCount=0;result.PolygonCount=0;Fill(result.Polygons,None);
    scratch.HashHeads=new ushort[256];Fill(scratch.HashHeads,None);scratch.VertexLinks=new ushort[total];
    scratch.Indices=new ushort[maximum];scratch.Triangles=new ushort[maximum*3];scratch.LocalPolygons=new ushort[maximum*6];
    for(int c=0;c<inputs.Length;c++){
     result.ContourIndex=c;NavigationPolygonContourInput contour=inputs[c];int n=contour.VertexCount;if(n<3)continue;
     if(n>maximum)throw Stop("ContourCountChanged",0x26c023);
     for(int j=0;j<n;j++)scratch.Indices[j]=(ushort)j;
     int signed=Triangulate(n,contour.Vertices,scratch.Indices,scratch.Triangles);scratch.SignedTriangleCount=signed;int triangles=signed<1?-signed:signed;
     for(int j=0;j<contour.VertexCount;j++){
      byte[] q=contour.Vertices;int z=B(q,j*4+2,0x26c06c),y=B(q,j*4+1,0x26c071),x=B(q,j*4,0x26c076);
      int bucket=unchecked(z*(int)0xcb1ab31fu-x*0x72594cbd)&255;ushort head=scratch.HashHeads[bucket],index=head;int steps=0;
      while(index!=None){if(index>=result.VertexCount||++steps>result.VertexCount)throw Stop("VertexHashLinkDomain",0x26c0b8);
       if(result.Vertices[index*3]==x&&result.Vertices[index*3+2]==z&&Math.Abs(result.Vertices[index*3+1]-y)<=2)break;index=scratch.VertexLinks[index];}
      if(index==None){
       if(result.VertexCount>=65535||result.VertexCount>=total)throw Stop("VertexOutputExtent",0x26c0f8);
       index=(ushort)result.VertexCount;result.VertexCount++;result.Vertices[index*3]=(ushort)x;result.Vertices[index*3+1]=(ushort)y;result.Vertices[index*3+2]=(ushort)z;
       scratch.VertexLinks[index]=head;scratch.HashHeads[bucket]=index;
      }
      UW(scratch.Indices,j,index,0x26c126);if((B(q,j*4+3,0x26c129)&128)!=0)scratch.RemovalFlags[index]=1;
     }
     Fill(scratch.LocalPolygons,None);int localCount=0;
     for(int t=0;t<triangles;t++){
      ushort a=U(scratch.Triangles,t*3,0x26c1c5),b=U(scratch.Triangles,t*3+1,0x26c1c0),d=U(scratch.Triangles,t*3+2,0x26c1cf);
      // Native compares original triangle indices BEFORE coalescing, even if
      // three different original indices map to repeated output vertices.
      if(a==b||a==d||b==d)continue;
      UW(scratch.LocalPolygons,localCount*6,U(scratch.Indices,a,0x26c1e0),0x26c1e6);
      UW(scratch.LocalPolygons,localCount*6+1,U(scratch.Indices,b,0x26c1ee),0x26c1f4);
      UW(scratch.LocalPolygons,localCount*6+2,U(scratch.Indices,d,0x26c1fb),0x26c201);localCount++;
     }
     while(localCount>1){
      int best=0,bestA=0,bestB=0,bestEA=0,bestEB=0;
      for(int a=0;a<localCount-1;a++)for(int b=a+1;b<localCount;b++){
       int ea=0,eb=0,score=MergeScore(scratch.LocalPolygons,a*6,scratch.LocalPolygons,b*6,result.Vertices,ref ea,ref eb);
       if(score>best){best=score;bestA=a;bestB=b;bestEA=ea;bestEB=eb;}
      }
      if(best<=0)break;
      Merge(scratch.LocalPolygons,bestA*6,scratch.LocalPolygons,bestB*6,bestEA,bestEB);
      for(int k=0;k<4;k++)scratch.LocalPolygons[bestB*6+k]=scratch.LocalPolygons[(localCount-1)*6+k];
      for(int k=4;k<6;k++)scratch.LocalPolygons[bestB*6+k]=scratch.LocalPolygons[(localCount-1)*6+k];localCount--;
     }
     scratch.LocalPolygonCount=localCount;
     for(int p=0;p<localCount;p++){
      if(result.PolygonCount>=capacity)throw Stop("PolygonOutputExtentBeforeNativeOverflowWrite",0x26c3ac);
      for(int k=0;k<6;k++)result.Polygons[result.PolygonCount*12+k]=scratch.LocalPolygons[p*6+k];
      result.Areas[result.PolygonCount]=contour.Area;result.PolygonCount++;
     }
    }
    for(int v=0;v<result.VertexCount;v++)if(scratch.RemovalFlags[v]!=0)throw Stop("MissingVertexRemoval26B260And26B480",0x26c4a7);
    BuildAdjacency(result.Polygons,result.PolygonCount,result.Vertices,result.VertexCount,inputs);
    result.Outcome=NavigationPolygonOutcome.Success;result.NativeStatus=SuccessStatus;result.Detail="ManagedPolygonNormalReturn";
   }catch(NavigationPolygonDomainException e){result.Outcome=NavigationPolygonOutcome.OwnUnsupported;result.NativeStatus=null;result.Detail=e.Code;result.StopRva=e.Rva;}
   return result;
  }
 }
}
