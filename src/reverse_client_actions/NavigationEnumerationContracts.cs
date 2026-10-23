using System;
using System.Collections.Generic;
namespace GrimDynasty.ReverseClient.Actions {
 public class NavigationEnumerationBoundaries:NavigationCoreBoundaries {
  public readonly NavigationSpatialState Spatial;
  public NavigationEnumerationBoundaries(NavigationSpatialState spatial){if(spatial==null)throw new ArgumentNullException("spatial");Spatial=spatial;}
  public virtual int EnumerateTile261320(NavigationQuery query,NavigationTile tile,NavigationPoint3 min,NavigationPoint3 max,NavigationQueryFilter filter,NavigationRefWindow output,int capacity){return NavigationEnumerationContracts.EnumerateTile(query,tile,min,max,filter,output,capacity,this);}
 }
 // Own bounded typed address/cell domain. All native fields remain explicit.
 // Floating arithmetic models masked exceptions and round-to-nearest binary32;
 // it does not configure/emulate MXCSR, FTZ/DAZ or concurrent native mutation.
 public static class NavigationEnumerationContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null navigation address");var v=value as NavigationView;if(v!=null)v.RequireLive(at);return value;}
  static T At<T>(T[] array,long index,uint at)where T:class{if(array==null||index<0||index>=array.Length)throw new MissingContract(at,"Outside declared navigation table extent");return Need(array[(int)index],at);}
  static float F(float v){return NavigationCoreContracts.F(v);}
  static float Read(NavigationPoint3 p,int axis,uint at){return Need(p,at).CellAt(axis,at).Read(at);}
  static float V(NavigationPoint3[] vertices,uint index,int axis,uint at){return Read(At(vertices,index,at),axis,at);}
  static float MinSse(float first,float second){return first<second?first:second;}
  static float MaxSse(float first,float second){return first>second?first:second;}
  public static int Cvtt32(float value){if(float.IsNaN(value)||value>=2147483648f||value<(-2147483648f))return Int32.MinValue;return (int)Math.Truncate((double)value);}
  public static long Cvtt64(float value){if(float.IsNaN(value)||value>=9223372036854775808f||value<(-9223372036854775808f))return Int64.MinValue;return (long)Math.Truncate((double)value);}
  public static int FloorGridIndex(float value){
   int converted=Cvtt32(value);
   if(converted!=Int32.MinValue){float back=F((float)converted);if(back!=value&&!float.IsNaN(back)&&!float.IsNaN(value)){converted=unchecked(converted-(int)(NavigationCoreContracts.ToBits(value)>>31));value=F((float)converted);}}
   return Cvtt32(value);
  }
  static NavigationMesh MeshFrom(NavigationQuery query,uint capture,uint use){var bits=Need(query,capture).Mesh0.Snapshot();return Need(bits.Read(use),use);}
  static uint BaseReference(NavigationMesh mesh,NavigationTile tile,NavigationTileSpatial ts,NavigationTile[] capturedBase,uint arrayAt,uint polyFirst,uint saltAt,uint tileBitsAt,uint polySecond){
   int index=ts.ArrayIndex(capturedBase,arrayAt);
   mesh.RequireLive(polyFirst);uint p1=mesh.PolyBits60.Read(polyFirst);
   tile.RequireLive(saltAt);uint salt=tile.Salt00.Read(saltAt);
   mesh.RequireLive(tileBitsAt);uint t=mesh.TileBits5C.Read(tileBitsAt);
   mesh.RequireLive(polySecond);uint p2=mesh.PolyBits60.Read(polySecond);
   return unchecked(((uint)index<<(int)(p1&31))|(salt<<(int)(unchecked(t+p2)&31)));
  }
  public static uint Enumerate(NavigationQuery query,NavigationPoint3 point,NavigationPoint3 extent,NavigationQueryFilter filter,NavigationRefBuffer output,Field<uint> count,int capacity,NavigationEnumerationBoundaries b,Invocation i){
   float px=Read(point,0,0x261963),py=Read(point,1,0x26196a),pz=Read(point,2,0x261972);
   var meshBits=Need(query,0x26197a).Mesh0.Snapshot();
   float ey=Read(extent,1,0x26197d),ez=Read(extent,2,0x261983);
   float minY=F(py-ey),maxY=F(ey+py),ex=Read(extent,0,0x261995);
   float minZ=F(pz-ez),minX=F(px-ex),maxX=F(ex+px),maxZ=F(ez+pz);
   var lo=new NavigationPoint3();lo.X.Write(minX);lo.Y.Write(minY);lo.Z.Write(minZ);
   var hi=new NavigationPoint3();hi.X.Write(maxX);hi.Y.Write(maxY);hi.Z.Write(maxZ);
   var initialMesh=Need(meshBits.Read(0x2619d4),0x2619d4);var spatial=Need(b,0x2619d4).Spatial;
   float grid=F(minX-spatial.Mesh(initialMesh,0x2619d4).OriginX1C.Read(0x2619d4));
   grid=F(grid/spatial.Mesh(initialMesh,0x2619e6).TileWidth28.Read(0x2619e6));int xMin=FloorGridIndex(grid);
   grid=F(maxX-spatial.Mesh(initialMesh,0x261a1c).OriginX1C.Read(0x261a1c));
   grid=F(grid/spatial.Mesh(initialMesh,0x261a26).TileWidth28.Read(0x261a26));int xMax=FloorGridIndex(grid);
   grid=F(maxZ-spatial.Mesh(initialMesh,0x261a66).OriginZ24.Read(0x261a66));
   grid=F(grid/spatial.Mesh(initialMesh,0x261a70).TileDepth2C.Read(0x261a70));int zMax=FloorGridIndex(grid);
   grid=F(minZ-spatial.Mesh(initialMesh,0x261aaf).OriginZ24.Read(0x261aaf));
   grid=F(grid/spatial.Mesh(initialMesh,0x261abb).TileDepth2C.Read(0x261abb));int zMin=FloorGridIndex(grid);
   int total=0;long cells=0,rows=0;int rowHash=unchecked(zMin*(-0x27e9c7bf));
   for(int z=zMin;z<=zMax;z=unchecked(z+1),rowHash=unchecked(rowHash-0x27e9c7bf)){
    if(++rows>spatial.OwnMaxGridCells)throw new MissingContract(0x261b43,"Own row-iteration domain exhausted, including rows with an empty X range");
    int hash=unchecked(rowHash-unchecked(xMin*0x72594cbd));
    for(int x=xMin;x<=xMax;x=unchecked(x+1),hash=unchecked(hash-0x72594cbd)){
     if(++cells>spatial.OwnMaxGridCells)throw new MissingContract(0x261b60,"Own grid-iteration domain exhausted; partial reference writes retained");
     var currentMesh=MeshFrom(query,0x261b60,0x261b6d);
     long mask=spatial.Mesh(currentMesh,0x261b6d).HashMask38.Read(0x261b6d);
     long bucket=((long)hash)&mask; // both operands sign-extended from32 bits
     var bucketBits=spatial.Mesh(currentMesh,0x261b74).Buckets40.Snapshot();var buckets=bucketBits.Read(0x261b78);
     if(buckets==null||bucket<0||bucket>=buckets.Length)throw new MissingContract(0x261b78,"Bucket address outside declared table");
     var tile=buckets[(int)bucket];var selected=new NavigationTile[32];int selectedCount=0;long links=0;var seen=new HashSet<NavigationTile>();
     while(tile!=null){
      if(++links>spatial.OwnMaxBucketLinks||!seen.Add(tile))throw new MissingContract(0x261b81,"Own finite bucket-chain domain exceeded or cyclic chain");
      var header=Need(tile,0x261b81).Header08.Read(0x261b81);
      if(header!=null&&spatial.Header(header,0x261b8a).GridX08.Read(0x261b8a)==x&&spatial.Header(header,0x261b8f).GridZ0C.Read(0x261b8f)==z){if(selectedCount<32)selected[selectedCount++]=tile;}
      tile=spatial.Tile(tile,0x261ba6).Next60.Read(0x261ba6);
     }
     for(int n=0;n<selectedCount;n++){
      i.At=0x261bf4;int added=b.EnumerateTile261320(query,selected[n],lo,hi,filter,new NavigationRefWindow(output,total),unchecked(capacity-total));
      total=unchecked(total+added);
      if(total>=capacity){i.At=0x261ca7;Need(count,i.At).Write(unchecked((uint)total));return 0x40000010;}
     }
    }
   }
   i.At=0x261c63;Need(count,i.At).Write(unchecked((uint)total));return 0x40000000;
  }
  public static int EnumerateTile(NavigationQuery query,NavigationTile tile,NavigationPoint3 min,NavigationPoint3 max,NavigationQueryFilter filter,NavigationRefWindow output,int capacity,NavigationEnumerationBoundaries b){
   var spatial=Need(b,0x26133f).Spatial;var ts=spatial.Tile(tile,0x26133f);var tree=ts.BVTree40.Read(0x26133f);
   if(tree!=null)return EnumerateBv(query,tile,ts,tree,min,max,filter,output,capacity,b);
   var mesh=MeshFrom(query,0x2615cf,0x2615e5);mesh.RequireLive(0x2615e5);var array=mesh.Tiles50.Read(0x2615e5);
   uint reference=BaseReference(mesh,tile,ts,array,0x2615e5,0x2615ec,0x2615fe,0x261602,0x261606);
   var headerBits=Need(tile,0x26160e).Header08.Snapshot();var header=Need(headerBits.Read(0x26161a),0x26161a);
   int total=0,polyIndex=0;if(unchecked((int)header.PolyCount18.Read(0x26161a))<=0)return 0;
   do{
    if(polyIndex>=spatial.OwnMaxRecords)throw new MissingContract(0x261650,"Own polygon-iteration domain exceeded");
    var polyBits=Need(tile,0x261650).Polys10.Snapshot();var poly=At(polyBits.Read(0x261658),polyIndex,0x261658);
    if((spatial.Poly(poly,0x261658).Type1F.Read(0x261658)&0xc0)!=0x40){
     ushort flags=spatial.Poly(poly,0x261673).Flags1C.Read(0x261673);
     if((Need(filter,0x26167b).Include100.Read(0x26167b)&flags)!=0&&(Need(filter,0x261688).Exclude102.Read(0x261688)&flags)==0){
      var vertexBits=Need(tile,0x261695).Vertices18.Snapshot();poly.RequireLive(0x26169e);uint first=poly.Indices04[0].Read(0x26169e);
      poly.RequireLive(0x2616a3);int verticesCount=poly.VertexCount1E.Read(0x2616a3);
      if(verticesCount>6)throw new MissingContract(0x2616a3,"Polygon exceeds own six-index domain, not a native guard");
      var vertices=vertexBits.Read(0x2616ac);
      float minX=V(vertices,first,0,0x2616ac),minY=V(vertices,first,1,0x2616b1),minZ=V(vertices,first,2,0x2616bc);
      float maxX=minX,maxY=minY,maxZ=minZ;int k=1;
      if(verticesCount>=5){
       poly.RequireLive(0x261700);uint a=poly.Indices04[1].Read(0x261700);
       poly.RequireLive(0x26170d);uint c=poly.Indices04[2].Read(0x26170d);
       float ax=V(vertices,a,0,0x261712),ay=V(vertices,a,1,0x261717),az=V(vertices,a,2,0x261720);
       poly.RequireLive(0x26172e);uint d=poly.Indices04[3].Read(0x26172e);
       float cy=V(vertices,c,1,0x261733),cx=V(vertices,c,0,0x26174b),cz=V(vertices,c,2,0x261761);
       poly.RequireLive(0x261770);uint e=poly.Indices04[4].Read(0x261770);
       float dx=V(vertices,d,0,0x26177a),dz=V(vertices,d,2,0x26178e),dy=V(vertices,d,1,0x261798);
       float ex=V(vertices,e,0,0x2617a9),ey=V(vertices,e,1,0x2617b3),ez=V(vertices,e,2,0x2617ce);
       Bounds(ax,ay,az,ref minX,ref minY,ref minZ,ref maxX,ref maxY,ref maxZ);
       Bounds(cx,cy,cz,ref minX,ref minY,ref minZ,ref maxX,ref maxY,ref maxZ);
       Bounds(dx,dy,dz,ref minX,ref minY,ref minZ,ref maxX,ref maxY,ref maxZ);
       Bounds(ex,ey,ez,ref minX,ref minY,ref minZ,ref maxX,ref maxY,ref maxZ);k=5;
      }
      for(;k<verticesCount;k++){
       poly.RequireLive(0x26182b);uint index=poly.Indices04[k].Read(0x26182b);
       float x=V(vertices,index,0,0x261837),y=V(vertices,index,1,0x26183c),z=V(vertices,index,2,0x261845);
       Bounds(x,y,z,ref minX,ref minY,ref minZ,ref maxX,ref maxY,ref maxZ);
      }
      bool overlaps=true;
      if(!(maxX>=Read(min,0,0x261887)))overlaps=false;else if(minX>Read(max,0,0x26188f))overlaps=false;
      if(!(maxY>=Read(min,1,0x261898)))overlaps=false;else if(minY>Read(max,1,0x26189f))overlaps=false;
      bool zPass=maxZ>=Read(min,2,0x2618a9);if(zPass&&minZ>Read(max,2,0x2618b0))zPass=false;
      if(zPass&&overlaps&&total<capacity){Need(output,0x2618d4).At(total,0x2618d4).Write(reference|unchecked((uint)polyIndex));total=unchecked(total+1);}
     }
    }
    headerBits=Need(tile,0x2618e5).Header08.Snapshot();polyIndex=unchecked(polyIndex+1);header=Need(headerBits.Read(0x2618f3),0x2618f3);
   }while(polyIndex<unchecked((int)header.PolyCount18.Read(0x2618f3)));
   return total;
  }
  static void Bounds(float x,float y,float z,ref float minX,ref float minY,ref float minZ,ref float maxX,ref float maxY,ref float maxZ){
   minX=MinSse(x,minX);maxX=MaxSse(x,maxX);minY=MinSse(y,minY);maxY=MaxSse(y,maxY);maxZ=MaxSse(maxZ,z);minZ=MinSse(z,minZ);
  }
  static float ClampBound(float value,float low,NavigationHeaderSpatial hs,int axis,uint highAt){if(!(value>=low))return low;hs.RequireLive(highAt);hs.Identity.RequireLive(highAt);return MinSse(Read(hs.BoundsMax54,axis,highAt),value);}
  static int EnumerateBv(NavigationQuery query,NavigationTile tile,NavigationTileSpatial ts,NavigationBvNode16[] tree,NavigationPoint3 min,NavigationPoint3 max,NavigationQueryFilter filter,NavigationRefWindow output,int capacity,NavigationEnumerationBoundaries b){
   var spatial=b.Spatial;var headerBits=Need(tile,0x26136d).Header08.Snapshot();float value=Read(min,0,0x261371);
   var header=Need(headerBits.Read(0x261376),0x261376);int length=spatial.Header(header,0x261376).BvNodeCount30.Read(0x261376);
   var hs=spatial.Header(header,0x26137a);float originX=Read(hs.BoundsMin48,0,0x26137a);
   float quant=spatial.Header(header,0x26137f).BvQuantFactor60.Read(0x26137f);
   float lowX=ClampBound(value,originX,hs,0,0x261395);
   spatial.Header(header,0x26139e);float originY=Read(hs.BoundsMin48,1,0x26139e);lowX=F(lowX-originX);
   value=Read(min,1,0x2613a7);float lowY=ClampBound(value,originY,hs,1,0x2613b7);
   spatial.Header(header,0x2613c0);float originZ=Read(hs.BoundsMin48,2,0x2613c0);lowY=F(lowY-originY);
   value=Read(min,2,0x2613c9);float lowZ=ClampBound(value,originZ,hs,2,0x2613d9);
   value=Read(max,0,0x2613e2);lowZ=F(lowZ-originZ);float highX=ClampBound(value,originX,hs,0,0x2613f6);
   value=Read(max,1,0x261400);highX=F(highX-originX);float highY=ClampBound(value,originY,hs,1,0x261415);
   value=Read(max,2,0x26141f);highY=F(highY-originY);float highZ=ClampBound(value,originZ,hs,2,0x261434);
   var meshBits=Need(query,0x26143e).Mesh0.Snapshot();highZ=F(highZ-originZ);
   lowZ=F(lowZ*quant);lowX=F(lowX*quant);
   var mesh=Need(meshBits.Read(0x261458),0x261458);var array=mesh.Tiles50.Read(0x261458);int tileIndex=ts.ArrayIndex(array,0x261458);
   ushort qMinZ=unchecked((ushort)(Cvtt64(lowZ)&0xfffe));float one=b.One34BB44.Read(0x261461);
   lowY=F(lowY*quant);ushort qMinX=unchecked((ushort)(Cvtt64(lowX)&0xfffe)),qMinY=unchecked((ushort)(Cvtt64(lowY)&0xfffe));
   highX=F(highX*quant);highY=F(highY*quant);highX=F(highX+one);highZ=F(highZ*quant);highY=F(highY+one);highZ=F(highZ+one);
   ushort qMaxX=unchecked((ushort)(Cvtt64(highX)|1)),qMaxZ=unchecked((ushort)(Cvtt64(highZ)|1)),qMaxY=unchecked((ushort)(Cvtt64(highY)|1));
   mesh.RequireLive(0x2614d0);uint p1=mesh.PolyBits60.Read(0x2614d0);
   tile.RequireLive(0x2614e5);uint salt=tile.Salt00.Read(0x2614e5);
   mesh.RequireLive(0x2614e9);uint tileBits=mesh.TileBits5C.Read(0x2614e9);mesh.RequireLive(0x2614ed);uint p2=mesh.PolyBits60.Read(0x2614ed);
   uint reference=unchecked(((uint)tileIndex<<(int)(p1&31))|(salt<<(int)(unchecked(tileBits+p2)&31)));
   int total=0;long visited=0,cursor=0;
   while(cursor<length){
    if(++visited>spatial.OwnMaxRecords)throw new MissingContract(0x261510,"Own BV traversal domain exceeded");
    var node=At(tree,cursor,0x261512);bool overlaps=true;
    if(qMinX>node.MaxX06.Read(0x261512))overlaps=false;else{node.RequireLive(0x261521);if(qMaxX<node.MinX00.Read(0x261521))overlaps=false;}
    node.RequireLive(0x261529);if(qMinY>node.MaxY08.Read(0x261529))overlaps=false;else{node.RequireLive(0x261534);if(qMaxY<node.MinY02.Read(0x261534))overlaps=false;}
    node.RequireLive(0x26153d);if(qMinZ>node.MaxZ0A.Read(0x26153d))overlaps=false;else{node.RequireLive(0x261544);if(qMaxZ<node.MinZ04.Read(0x261544))overlaps=false;}
    node.RequireLive(0x26154d);int index=node.Index0C.Read(0x26154d);bool leaf=index>=0;
    if(leaf&&overlaps){
     var polyBits=Need(tile,0x261566).Polys10.Snapshot();var poly=At(polyBits.Read(0x26156d),index,0x26156d);ushort flags=spatial.Poly(poly,0x26156d).Flags1C.Read(0x26156d);
     if((Need(filter,0x261571).Include100.Read(0x261571)&flags)!=0&&(Need(filter,0x26157a).Exclude102.Read(0x26157a)&flags)==0&&total<capacity){Need(output,0x26159b).At(total,0x26159b).Write(reference|unchecked((uint)index));total=unchecked(total+1);}
    }
    if(overlaps||leaf)cursor++;
    else{node.RequireLive(0x2615aa);int advance=unchecked(-node.Index0C.Read(0x2615aa));if(advance<=0)throw new MissingContract(0x2615b3,"Negative/zero own BV escape cannot establish finite forward traversal; native NEG is not Abs");cursor+=advance;}
   }
   return total;
  }
 }
}
