using System;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 public struct NavigationTileQuantizedVertex {
  public readonly ushort X,Y,Z;
  public NavigationTileQuantizedVertex(ushort x,ushort y,ushort z){X=x;Y=y;Z=z;}
 }
 public struct NavigationTileBounds {
  public readonly float MinX,MinY,MinZ,MaxX,MaxY,MaxZ;
  public NavigationTileBounds(float minX,float minY,float minZ,float maxX,float maxY,float maxZ){MinX=minX;MinY=minY;MinZ=minZ;MaxX=maxX;MaxY=maxY;MaxZ=maxZ;}
 }
 public sealed class NavigationTilePolygon {
  // Borrowed, fixed six-slot views. The first FFFF vertex terminates the
  // polygon; later vertex and neighbor slots are not consumed by 26E3F0.
  public readonly ushort[] Vertices,Neighbors;
  public NavigationTilePolygon(ushort[] vertices,ushort[] neighbors){Vertices=vertices;Neighbors=neighbors;}
 }
 public sealed class NavigationTileDataInput {
  public readonly NavigationTileQuantizedVertex[] Vertices;
  public readonly NavigationTilePolygon[] Polygons;
  public readonly byte[] Areas;
  public readonly ushort[] Flags;
  public readonly NavigationTileBounds Bounds;
  public readonly float CellSize,CellHeight,WalkHeight,WalkRadius,WalkClimb;
  public readonly int TileX,TileY,TileLayer;
  // This DTO represents the 26E3F0 parameter shape used by cold 2658D0:
  // nvp=6; detail/offmesh/BV inputs absent; userId=0. A caller adapter must
  // separately preserve 265C90..265CA5 flags=(area!=0?1:0) when claiming that
  // exact cold caller path. The serializer itself copies explicit ushort flags.
  // All supplied geometry and configuration values remain explicit. No runtime
  // game configuration, polygon generator or default data is inferred here.
  public NavigationTileDataInput(NavigationTileQuantizedVertex[] vertices,NavigationTilePolygon[] polygons,byte[] areas,ushort[] flags,NavigationTileBounds bounds,float cellSize,float cellHeight,int tileX,int tileY,int tileLayer,float walkHeight,float walkRadius,float walkClimb){
   Vertices=vertices;Polygons=polygons;Areas=areas;Flags=flags;Bounds=bounds;CellSize=cellSize;CellHeight=cellHeight;TileX=tileX;TileY=tileY;TileLayer=tileLayer;WalkHeight=walkHeight;WalkRadius=walkRadius;WalkClimb=walkClimb;
  }
 }
 public sealed class NavigationTileDataOutput {
  public byte[] Bytes;
  public int Length;
 }
 public enum NavigationTileDataOutcome { Published, OwnUnsupported }
 public sealed class NavigationTileDataResult {
  public readonly NavigationTileDataOutcome Outcome;
  public readonly NavigationTileDataOutput Output;
  public readonly string Detail;
  public readonly int PolygonIndex,VertexIndex;
  internal NavigationTileDataResult(NavigationTileDataOutcome outcome,NavigationTileDataOutput output,string detail,int polygon,int vertex){Outcome=outcome;Output=output;Detail=detail;PolygonIndex=polygon;VertexIndex=vertex;}
 }

 // Engine 26E3F0 (not a file-format guess): saved source evidence in
 // docs/reverse-client/r1-tile-production-direct-stages/25,26,27.
 // Assembly SHA256 4cf3246e6ee26e1e224429c2e751e3c46956fec115bd13ecd393dded853d3cd9.
 // Caller 2658D0: r1-map-region-and-tile-product/11, SHA256
 // 79c2cb14c5c36792b795caab09dab675827cbc80416de203d2785927e3ffdabe.
  // Raw 34BB44=0000803f: docs/object-lifecycle/offline-actor-remaining-
  // dependencies/inspection.json, SHA256
  // 6639fbb457720057edb6ea37834e6f5aa2534593b26020fda7d83044b73325c5.
 // Engine SHA256 4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153.
 // 265B97 clears D0 bytes; 265C2E clears BV flag C8; null cache+68 skips the
 // processor. Thus detail +30, offmesh count +88 and BV +C8 remain zero.
 // 26F165 skips 26E170. No helper output or native allocator result is mocked.
 public static class NavigationTileData {
  public const int HeaderLength=100,VerticesPerPolygon=6;
  // Explicit OWN safety limits, not recovered native limits. The polygon cap
  // keeps serialized internal-neighbor +1 below the external-link bit 8000.
  public const int OwnMaximumPolygons=32767,OwnMaximumOutputBytes=64*1024*1024;
  static NavigationTileDataResult Reject(NavigationTileDataOutput output,string why,int polygon,int vertex){return new NavigationTileDataResult(NavigationTileDataOutcome.OwnUnsupported,output,why,polygon,vertex);}
  static uint Bits(float value){return unchecked((uint)BitConverter.ToInt32(BitConverter.GetBytes(value),0));}
  static bool Ordinary(float value){uint bits=Bits(value);return (bits&0x7f800000u)!=0x7f800000u&&((bits&0x7f800000u)!=0||(bits&0x007fffffu)==0);}
  static bool Nonnegative(float value){return Ordinary(value)&&value>=0;}
  static void U16(byte[] bytes,int at,ushort value){unchecked{bytes[at]=(byte)value;bytes[at+1]=(byte)(value>>8);}}
  static void U32(byte[] bytes,int at,uint value){unchecked{for(int b=0;b<4;b++)bytes[at+b]=(byte)(value>>(8*b));}}
  static void I32(byte[] bytes,int at,int value){U32(bytes,at,unchecked((uint)value));}
  static void F32(byte[] bytes,int at,float value){U32(bytes,at,Bits(value));}
  // Own IEEE binary32, round-to-nearest platform domain. A materialized four
  // byte roundtrip forces EACH scalar step to 32 bits even on an extended-
  // precision CLR; separate methods prevent an implicit multiply/add fusion.
  // Inputs/intermediates with subnormal/nonfinite bits are explicitly outside
  // this adapter's domain because original MXCSR DAZ/FTZ was not observed.
  [MethodImpl(MethodImplOptions.NoInlining)]
  static float Round32(double value){float single=(float)value;return BitConverter.ToSingle(BitConverter.GetBytes(single),0);}
  static bool NotTiny(double value){return value==0.0||Math.Abs(value)>=1.17549435082228750796873653722224568E-38;}
  static bool Coordinate(ushort vertex,float scale,float origin,out float value){
   double exactProduct=(double)vertex*(double)scale;
   if(!NotTiny(exactProduct)){value=0;return false;}
   float product=Round32(exactProduct);
   if(!Ordinary(product)){value=0;return false;}
   double sum=(double)product+(double)origin;
   // Reject cancellation into a subnormal BEFORE the narrowing conversion,
   // so a host that flushes it to zero cannot silently pass this domain gate.
   if(!NotTiny(sum)){value=0;return false;}
   value=Round32(sum);
   return Ordinary(value);
  }
  static long Aligned4(long size){return (size+3L)&~3L;}

  public static NavigationTileDataResult Build(NavigationTileDataInput input,NavigationTileDataOutput output){return Build(input,output,OwnMaximumOutputBytes);}
  // Stable input arrays for the whole synchronous call are required. Managed
  // allocation exceptions propagate WITHOUT changing output or pretending to
  // be native AL=false. All explicit own rejections also leave old Bytes and
  // Length untouched. Success publishes Bytes THEN Length (26F28F..26F2AE).
  public static NavigationTileDataResult Build(NavigationTileDataInput input,NavigationTileDataOutput output,int maximumOutputBytes){
   if(output==null)throw new ArgumentNullException("output");
   if(input==null)return Reject(output,"NullInput",-1,-1);
   if(maximumOutputBytes<0||maximumOutputBytes>OwnMaximumOutputBytes)return Reject(output,"OwnOutputBudget",-1,-1);
   if(input.Vertices==null||input.Vertices.Length==0||input.Vertices.Length>=65535)return Reject(output,"VertexCountDomain",-1,-1);
   if(input.Polygons==null||input.Polygons.Length==0||input.Polygons.Length>OwnMaximumPolygons)return Reject(output,"PolygonCountDomain",-1,-1);
   int vertices=input.Vertices.Length,polygons=input.Polygons.Length;
   if(input.Areas==null||input.Flags==null||input.Areas.Length!=polygons||input.Flags.Length!=polygons)return Reject(output,"AttributeExtent",-1,-1);
   var bounds=input.Bounds;
   if(!Ordinary(bounds.MinX)||!Ordinary(bounds.MinY)||!Ordinary(bounds.MinZ)||!Ordinary(bounds.MaxX)||!Ordinary(bounds.MaxY)||!Ordinary(bounds.MaxZ)||bounds.MinX>bounds.MaxX||bounds.MinY>bounds.MaxY||bounds.MinZ>bounds.MaxZ)return Reject(output,"BoundsDomain",-1,-1);
   if(!Ordinary(input.CellSize)||!Ordinary(input.CellHeight)||input.CellSize<=0||input.CellHeight<=0)return Reject(output,"CellScaleDomain",-1,-1);
   if(!Nonnegative(input.WalkHeight)||!Nonnegative(input.WalkRadius)||!Nonnegative(input.WalkClimb))return Reject(output,"WalkConfigurationDomain",-1,-1);
   float reciprocal=Round32(1.0/(double)input.CellSize); // DIVSS 34BB44(1.0f),cellSize.
   if(!Ordinary(reciprocal)||reciprocal==0)return Reject(output,"ReciprocalDomain",-1,-1);
   var counts=new byte[polygons];long edges=0,portals=0,triangles=0;
   for(int p=0;p<polygons;p++){
    var polygon=input.Polygons[p];
    if(polygon==null||polygon.Vertices==null||polygon.Neighbors==null||polygon.Vertices.Length!=6||polygon.Neighbors.Length!=6)return Reject(output,"PolygonSlotExtent",p,-1);
    int count=0;
    while(count<6&&polygon.Vertices[count]!=65535){
     if(polygon.Vertices[count]>=vertices)return Reject(output,"VertexIndexDomain",p,count);
     ushort neighbor=polygon.Neighbors[count];
     if((neighbor&0x8000)!=0){
      int direction=neighbor&15;
      // Native directions 4..E count a portal but leave zero destination bytes.
      // Retain a narrower explicit domain, rather than silently invent an edge.
      if(direction!=15&&direction>3)return Reject(output,"PortalDirectionDomain",p,count);
      if(direction!=15)portals++;
     }else if(neighbor>=polygons||neighbor>=32767)return Reject(output,"NeighborIndexDomain",p,count);
     count++;
    }
    // Native sum(k-2) has no clamp. Degenerate arity is an OWN rejection, not
    // normalized to an empty polygon or a fake successful triangle count.
    if(count<3)return Reject(output,"PolygonArityDomain",p,-1);
    counts[p]=(byte)count;edges+=count;triangles+=count-2;
   }
   long links=edges+2*portals;
   long vertexBytes=Aligned4(12L*vertices),polygonBytes=Aligned4(32L*polygons),linkBytes=Aligned4(12L*links),detailBytes=Aligned4(12L*polygons),triangleBytes=Aligned4(4L*triangles);
   long total=HeaderLength+vertexBytes+polygonBytes+linkBytes+detailBytes+triangleBytes;
   if(total>maximumOutputBytes||total>Int32.MaxValue)return Reject(output,"OwnOutputBudget",-1,-1);
   var world=new float[3*vertices];
   for(int v=0;v<vertices;v++){
    var vertex=input.Vertices[v];float x,y,z;
    if(!Coordinate(vertex.X,input.CellSize,bounds.MinX,out x)||!Coordinate(vertex.Y,input.CellHeight,bounds.MinY,out y)||!Coordinate(vertex.Z,input.CellSize,bounds.MinZ,out z))return Reject(output,"WorldCoordinateDomain",-1,v);
    world[3*v]=x;world[3*v+1]=y;world[3*v+2]=z;
   }
   var bytes=new byte[(int)total]; // 26EB13 memset: all unfilled slots stay zero.
   U32(bytes,0,0x444e4156u);I32(bytes,4,7);
   I32(bytes,8,input.TileX);I32(bytes,12,input.TileY);I32(bytes,16,input.TileLayer);
   // +20 userId, +40 detailVertCount, +48 bvNodeCount, +52 offMeshCount = 0.
   I32(bytes,24,polygons);I32(bytes,28,vertices);I32(bytes,32,(int)links);
   I32(bytes,36,polygons);I32(bytes,44,(int)triangles);I32(bytes,56,polygons);
   F32(bytes,60,input.WalkHeight);F32(bytes,64,input.WalkRadius);F32(bytes,68,input.WalkClimb);
   F32(bytes,72,bounds.MinX);F32(bytes,76,bounds.MinY);F32(bytes,80,bounds.MinZ);
   F32(bytes,84,bounds.MaxX);F32(bytes,88,bounds.MaxY);F32(bytes,92,bounds.MaxZ);F32(bytes,96,reciprocal);
   int vertexOffset=100,polygonOffset=vertexOffset+(int)vertexBytes,linkOffset=polygonOffset+(int)polygonBytes,detailOffset=linkOffset+(int)linkBytes,triangleOffset=detailOffset+(int)detailBytes;
   for(int v=0;v<world.Length;v++)F32(bytes,vertexOffset+v*4,world[v]);
   int triangleBase=0;
   for(int p=0;p<polygons;p++){
    int at=polygonOffset+p*32;var polygon=input.Polygons[p];int count=counts[p];
    U16(bytes,at+28,input.Flags[p]);bytes[at+30]=(byte)count;bytes[at+31]=(byte)(input.Areas[p]&63);
    for(int k=0;k<count;k++){
     U16(bytes,at+4+2*k,polygon.Vertices[k]);ushort neighbor=polygon.Neighbors[k],encoded;
     if((neighbor&0x8000)==0)encoded=(ushort)(neighbor+1);
     else{
      switch(neighbor&15){case 0:encoded=0x8004;break;case 1:encoded=0x8002;break;case 2:encoded=0x8000;break;case 3:encoded=0x8006;break;default:encoded=0;break;}
     }
     U16(bytes,at+16+2*k,encoded);
    }
    int detail=detailOffset+p*12;
    // No supplied detail vertices, but actual generated detail meshes/fans:
    // +0 vertBase=0, +4 triBase, +8 vertCount=0, +9 triCount, +10 padding=0.
    I32(bytes,detail+4,triangleBase);bytes[detail+9]=(byte)(count-2);
    for(int k=2;k<count;k++){
     int tri=triangleOffset+triangleBase*4;
     bytes[tri]=0;bytes[tri+1]=(byte)(k-1);bytes[tri+2]=(byte)k;
     byte flags=(byte)(k==2?5:4);if(k==count-1)flags|=16;bytes[tri+3]=flags;triangleBase++;
    }
   }
   var result=new NavigationTileDataResult(NavigationTileDataOutcome.Published,output,"ColdDomainSerializedWithManagedAllocation",-1,-1);
   output.Bytes=bytes;output.Length=bytes.Length;return result;
  }
 }
}
