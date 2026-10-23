using System;

namespace GrimDynasty.ReverseClient.Actions {
 public enum NavigationContourOutcome { Success, NativeFailure, OwnUnsupported }
 public sealed class NavigationContourDomainException : InvalidOperationException {
  public readonly string Code; public readonly uint Rva;
  internal NavigationContourDomainException(string code,uint rva):base(code+" at Engine RVA "+rva.ToString("x")){Code=code;Rva=rva;}
 }
 public sealed class NavigationContourByte { public byte Value; public NavigationContourByte(byte value){Value=value;} }
 public sealed class NavigationContourScratch {
  public readonly byte[] Vertices;
  public readonly ushort[] Indices;
  public int Count,IndexCount;
  public int Capacity {get{return Vertices.Length/4;}}
  public NavigationContourScratch(int capacity){
   if(capacity<0||capacity>2040)throw new ArgumentOutOfRangeException("capacity","Own scratch domain is 0..4*(255+255)");
   Vertices=new byte[capacity*4];Indices=new ushort[capacity];
  }
 }
 public sealed class NavigationLayerContour {
  public int VertexCount {get;internal set;}
  public byte[] Vertices {get;internal set;}
  public byte RegionId {get;internal set;}
  public byte Area {get;internal set;}
 }
 public sealed class NavigationContourResult {
  public readonly NavigationLayerData Layer;
  public readonly NavigationLayerContour[] Contours;
  public readonly NavigationContourScratch Scratch;
  public readonly NavigationContourOutcome Outcome;
  public readonly uint? NativeStatus;
  public readonly string Detail;
  public readonly uint StopRva;
  public readonly int CellIndex;
  internal NavigationContourResult(NavigationLayerData layer,NavigationLayerContour[] contours,NavigationContourScratch scratch,NavigationContourOutcome outcome,uint? status,string detail,uint rva,int cell){Layer=layer;Contours=contours;Scratch=scratch;Outcome=outcome;NativeStatus=status;Detail=detail;StopRva=rva;CellIndex=cell;}
 }

 // Evidence: Engine269D70 in r1-tile-production-direct-stages;269590/269850/
 // 269BF0 in r1-nav-update-and-contours;2694E0/2693F0/269780 in
 // r1-nav-publication-and-contour-leaves. All are pinned to Engine SHA256
 // 4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153.
 // 269780's pseudo-code says void; its complete assembly supplies the actual
 // float XMM0 return and the ordered binary32 arithmetic below.
 // Directions: r1-navigation-contour-directions/inspection.json SHA
 // 86d13bfca164f76f817f42507f65ac8104b3c1a83f59428334532b56939ee05e.
 // One34BB44: offline-actor-remaining-dependencies/inspection.json SHA
 // 6639fbb457720057edb6ea37834e6f5aa2534593b26020fda7d83044b73325c5.
 // These are observed disk constants, not proof of unchanged loaded globals.
 // Own domain: stable layer/scratch during each synchronous call, disjoint
 // managed arrays, default CLR round-to-nearest binary32 with masked FP faults.
 // No native allocator callbacks/ABI/rollback, dangling pointers or concurrently
 // mutated fields are emulated. Managed allocation failure throws, never a fake
 // native 80000004. Inactive scratch bytes are not promoted to valid vertices.
 public static class NavigationLayerContours {
  public const uint SuccessStatus=0x40000000u,TraceFailureStatus=0x80000010u;
  static readonly int[] Dx={-1,0,1,0},Dz={0,1,0,-1};
  static NavigationContourDomainException Stop(string code,uint rva){return new NavigationContourDomainException(code,rva);}
  static float F(float value){return BitConverter.ToSingle(BitConverter.GetBytes(value),0);}
  static byte Read(NavigationLayerData layer,int plane,int index,uint rva){
   var p=layer.Plane(plane);if(index<0||index>=p.Count)throw Stop("PlaneExtent",rva);return p.Array[p.Offset+index];
  }
  static void Dimensions(NavigationLayerData layer){
   if(layer==null)throw new ArgumentNullException("layer");
   if(layer.Header.Array[layer.Header.Offset+48]!=layer.Width||layer.Header.Array[layer.Header.Offset+49]!=layer.Height)throw Stop("HeaderDimensionsChanged",0x269dad);
  }
  static byte V(NavigationContourScratch s,int index,int component,uint rva){
   if(index<0||index>=s.Capacity)throw Stop("VertexExtent",rva);return s.Vertices[index*4+component];
  }
  static void W(NavigationContourScratch s,int index,int component,byte value,uint rva){
   if(index<0||index>=s.Capacity)throw Stop("VertexExtent",rva);s.Vertices[index*4+component]=value;
  }
  static ushort I(NavigationContourScratch s,int index,uint rva){if(index<0||index>=s.Indices.Length)throw Stop("IndexExtent",rva);return s.Indices[index];}
  static void IW(NavigationContourScratch s,int index,ushort value,uint rva){if(index<0||index>=s.Indices.Length)throw Stop("IndexExtent",rva);s.Indices[index]=value;}

  //2694E0 returns only the observed low byte to its callers. A low connection
  // bit wins over the corresponding high portal bit. No native neighbor bound
  // check exists: an out-of-plane address is our Unsupported, never FF/portal.
  public static byte Neighbor(NavigationLayerData layer,int x,int z,int direction){
   Dimensions(layer);if(direction<0||direction>3)throw Stop("DirectionDomain",0x2694fc);
   int cell=unchecked(layer.Width*z+x);byte c=Read(layer,2,cell,0x269512);byte bit=(byte)(1<<direction);
   if((c&bit&15)!=0){int neighbor=unchecked((z+Dz[direction])*layer.Width+Dx[direction]+x);return Read(layer,3,neighbor,0x269580);}
   if(((c>>4)&bit)!=0)return unchecked((byte)(direction-8));return 255;
  }
  public static bool Append(NavigationContourScratch s,int x,byte y,int z,int edge){
   if(s==null)throw new ArgumentNullException("s");int n=s.Count;
   if(n>1&&V(s,n-1,3,0x26942a)==edge){
    if(V(s,n-2,0,0x269435)==V(s,n-1,0,0x269432)&&V(s,n-1,0,0x26943b)==x){W(s,n-1,1,y,0x269442);W(s,n-1,2,unchecked((byte)z),0x269448);return true;}
    if(V(s,n-2,2,0x269460)==V(s,n-1,2,0x26945c)&&V(s,n-1,2,0x269467)==z){W(s,n-1,0,unchecked((byte)x),0x26946f);W(s,n-1,1,y,0x269473);return true;}
   }
   if(unchecked(n+1)>s.Capacity)return false;
   W(s,n,0,unchecked((byte)x),0x2694b6);W(s,n,3,unchecked((byte)edge),0x2694bd);W(s,n,1,y,0x2694c6);W(s,n,2,unchecked((byte)z),0x2694ca);s.Count=unchecked(s.Count+1);return true;
  }

  public static bool Trace(NavigationLayerData layer,int startX,int startZ,NavigationContourScratch s){
   Dimensions(layer);if(s==null)throw new ArgumentNullException("s");int width=layer.Width,height=layer.Height;s.Count=0;
   byte region=Read(layer,3,unchecked(width*startZ+startX),0x2695e8);int first=-1;
   for(int q=0;q<4;q++){int d=(q-1)&3;if(Neighbor(layer,startX,startZ,d)!=region){first=d;break;}}
   if(first==-1)return true;
   int x=startX,z=startZ,dir=first,iterations=0,limit=width*height;
   while(iterations<limit){
    byte edge=Neighbor(layer,x,z,dir);int nextX=x,nextZ=z,nextDir;
    int cell=unchecked(width*z+x);
    if(edge!=Read(layer,3,cell,0x26966d)){
     int vx=x,vz=z;
     if(dir==0)vz=unchecked(z+1);else if(dir==1){vx=unchecked(x+1);vz=unchecked(z+1);}else if(dir==2)vx=unchecked(x+1);
     if(!Append(s,vx,Read(layer,0,cell,0x2696a6),vz,edge))return false;
     nextDir=(dir+1)&3;
    }else{nextX=unchecked(x+Dx[dir]);nextZ=unchecked(z+Dz[dir]);nextDir=(dir-1)&3;}
    // Native tests the OLD tuple AFTER processing that edge. It also returns
    // success upon exhausting width*height; no invented closure test is added.
    if(iterations>0&&x==startX&&z==startZ&&dir==first)break;
    iterations++;x=nextX;z=nextZ;dir=nextDir;
   }
   if(s.Count<=0)throw Stop("TraceHasNoReadableLastVertex",0x269745);
   if(V(s,s.Count-1,0,0x269748)==V(s,0,0,0x269745)&&V(s,s.Count-1,2,0x269751)==V(s,0,2,0x26974d))s.Count--;
   return true;
  }

  public static float DistanceSquared(int px,int pz,int ax,int az,int bx,int bz){
   float dx=F((float)unchecked(bx-ax)),dz=F((float)unchecked(bz-az));
   float xx=F(dx*dx),zz=F(dz*dz),length=F(zz+xx);
   float pointX=F((float)unchecked(px-ax)),pointZ=F((float)unchecked(pz-az));
   float t=F(F(pointX*dx)+F(pointZ*dz));if(length>0)t=F(t/length);
   // COMISS t,0 / JC: unordered also chooses +0. MINSS uses observed 1.0.
   if(float.IsNaN(t)||t<0)t=0;else if(!(t<1.0f))t=1.0f;
   float rx=F(F(F(t*dx)+F((float)ax))-F((float)px));
   float rz=F(F(F(t*dz)+F((float)az))-F((float)pz));
   return F(F(rz*rz)+F(rx*rx));
  }

  public static void Simplify(NavigationContourScratch s,float tolerance){
   if(s==null)throw new ArgumentNullException("s");s.IndexCount=0;
   for(int n=0;n<s.Count;n++)if(V(s,n,3,0x2698a1)!=V(s,(n+1)%s.Count,3,0x2698b0)){IW(s,s.IndexCount,unchecked((ushort)n),0x2698be);s.IndexCount++;}
   if(s.IndexCount<2){
    // Native next reads vertex0 even with Count<=0 and may later divide by0.
    // Do not treat zero-initialized but inactive managed scratch as native data.
    if(s.Count<=0)throw Stop("EmptySimplificationInput",0x269909);
    int min=0,max=0;int minX=V(s,0,0,0x269909),minZ=V(s,0,2,0x26990d),maxX=minX,maxZ=minZ;
    for(int n=1;n<s.Count;n++){int x=V(s,n,0,0x269920),z=V(s,n,2,0x269924);if(x<minX||(x==minX&&z<minZ)){min=n;minX=x;minZ=z;}if(x>maxX||(x==maxX&&z>maxZ)){max=n;maxX=x;maxZ=z;}}
    s.IndexCount=0;IW(s,0,(ushort)min,0x269960);s.IndexCount++;IW(s,s.IndexCount,(ushort)max,0x26996f);s.IndexCount++;
   }
   int k=0;
   while(k<s.IndexCount){
    int a=I(s,k,0x2699ad);int ax=V(s,a,0,0x2699b9),az=V(s,a,2,0x2699c1);
    int b=I(s,(k+1)%s.IndexCount,0x2699e1);int bx=V(s,b,0,0x2699ee),bz=V(s,b,2,0x2699f6);
    int step,end,v;
    if(ax<bx||(ax==bx&&az<bz)){step=1;end=b;v=(a+1)%s.Count;}else{step=s.Count-1;end=a;v=(b+step)%s.Count;}
    float best=0;int bestIndex=-1;
    while(v!=end){float d=DistanceSquared(V(s,v,0,0x269a74),V(s,v,2,0x269a6f),ax,az,bx,bz);if(d>best){best=d;bestIndex=v;}v=(v+step)%s.Count;}
    // Ordered strict > reproduces COMISS/JBE including NaN tolerance.
    if(bestIndex!=-1&&best>F(tolerance*tolerance)){
     int old=s.IndexCount;s.IndexCount=old+1;
     for(int n=old;n>k;n--){ushort value=I(s,n-1,0x269ad4);IW(s,n,value,0x269ad9);}IW(s,k+1,(ushort)bestIndex,0x269af0);
    }else k++;
   }
   int first=0;for(int n=1;n<s.IndexCount;n++){ushort current=I(s,first,0x269b60);if(I(s,n,0x269b65)<current)first=n;}
   s.Count=0;
   for(int n=0;n<s.IndexCount;n++){
    int source=I(s,(n+first)%s.IndexCount,0x269ba6),target=s.Count;
    // Deliberately in-place, individual bytes. No snapshot of all input quads.
    W(s,target,0,V(s,source,0,0x269bb4),0x269bbc);W(s,target,1,V(s,source,1,0x269bbe),0x269bc4);W(s,target,2,V(s,source,2,0x269bc7),0x269bcd);W(s,target,3,V(s,source,3,0x269bd0),0x269bd6);s.Count++;
   }
  }

  public static byte CornerHeight(NavigationLayerData layer,int x,int height,int z,int climb,NavigationContourByte marker){
   Dimensions(layer);int width=layer.Width,rows=layer.Height;byte max=0,ports=15,previousRegion=255;bool same=true;int count=0;
   for(int dz=-1;dz<1;dz++)for(int dx=-1;dx<1;dx++){
    int cx=unchecked(x+dx),cz=unchecked(z+dz);if(cx<0||cz<0||cx>=width||cz>=rows)continue;int cell=unchecked(cz*width+cx);
    byte y=Read(layer,0,cell,0x269c70);int delta=unchecked((int)y-height),sign=delta>>31;int abs=unchecked((delta^sign)-sign);
    if(abs>climb||Read(layer,1,cell,0x269c8b)==0)continue;
    if(y>max)max=y;ports=(byte)(ports&(Read(layer,2,cell,0x269ca5)>>4));
    if(previousRegion!=255&&previousRegion!=Read(layer,3,cell,0x269cbd))same=false;
    previousRegion=Read(layer,3,cell,0x269cd0);count++;
   }
   if(marker==null)throw Stop("NullMarkerCell",0x269d3d);marker.Value=0;
   int bits=0;for(int n=0;n<4;n++)bits+=(ports>>n)&1;
   if(count>1&&bits==1){byte value=marker.Value;if(same)value=1;marker.Value=value;}
   return max;
  }

  public static NavigationContourResult Build(NavigationLayerData layer,int climb,float tolerance){
   if(layer==null)throw new ArgumentNullException("layer");NavigationLayerContour[] records=null;NavigationContourScratch scratch=null;int cell=-1;
   try{
    Dimensions(layer);int width=layer.Width,height=layer.Height;records=new NavigationLayerContour[layer.RegionCount];for(int n=0;n<records.Length;n++)records[n]=new NavigationLayerContour();
    scratch=new NavigationContourScratch(4*(width+height));
    for(int z=0;z<height;z++)for(int x=0;x<width;x++){
     cell=z*width+x;byte region=Read(layer,3,cell,0x269ecb);if(region==255)continue;
     if(region>=records.Length)throw Stop("RegionRecordExtent",0x269ee3);var output=records[region];if(output.VertexCount>0)continue;
     output.RegionId=region;output.Area=Read(layer,1,cell,0x269ef8);
     if(!Trace(layer,x,z,scratch))return new NavigationContourResult(layer,records,scratch,NavigationContourOutcome.NativeFailure,TraceFailureStatus,"TraceAppendCapacity",0x269f12,cell);
     Simplify(scratch,tolerance);output.VertexCount=scratch.Count;
     if(scratch.Count>0){
      output.Vertices=new byte[scratch.Count*4];int previous=scratch.Count-1;
      for(int n=0;n<scratch.Count;n++){
       byte nextEdge=V(scratch,n,3,0x269f7a);var marker=new NavigationContourByte(0);
       byte vx=V(scratch,previous,0,0x269f85),vz=V(scratch,previous,2,0x269f89),vy=V(scratch,previous,1,0x269f8f);
       byte h=CornerHeight(layer,vx,vy,vz,climb,marker);int at=previous*4;
       output.Vertices[at]=vx;output.Vertices[at+1]=h;output.Vertices[at+2]=V(scratch,previous,2,0x269fc5);output.Vertices[at+3]=15;
       byte portal=unchecked((byte)(nextEdge+8));if(portal<=6)output.Vertices[at+3]=portal;if(marker.Value!=0)output.Vertices[at+3]|=0x80;previous=n;
      }
     }
    }
    return new NavigationContourResult(layer,records,scratch,NavigationContourOutcome.Success,SuccessStatus,"ObservedManagedContourPath",0,cell);
   }catch(NavigationContourDomainException error){return new NavigationContourResult(layer,records,scratch,NavigationContourOutcome.OwnUnsupported,null,error.Code,error.Rva,cell);}
  }
 }
}
