using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using GrimDynasty.ReverseClient.Actions;

// Actual saved-resource production through polygon geometry and cold VAND7
// bytes. This is NOT tile insertion, cross-polygon navigation or a game.
// Caller configuration is explicit; no loaded runtime defaults are inferred.
// Stable synchronous input, null processor and explicitly empty obstacle
// collection only:2659BC..265ABC obstacle application is not implemented.
public static class MainmenuTilePipeline {
 const string Root=@"C:\Users\zhuzi\Documents\Grim-Dynasty";
 const string MemberHash="a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289";
 static string Hash(byte[] data,int at,int count){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(data,at,count)).Replace("-","").ToLowerInvariant();}
 static void Require(bool value,string why){if(!value)throw new InvalidOperationException(why);}
 public static int Main(string[] args){
  try{
   Require(args.Length==1&&args[0]=="--explicit-default-domain","Use --explicit-default-domain to select the evidenced cold configuration experiment");
   BuildExplicitColdTiles();return 0;
  }catch(Exception e){Console.Error.WriteLine("FAIL "+e);return 1;}
 }
 // Reusable real-data producer for the subsequent mesh integration. Returned
 // byte owners are the serializer outputs themselves; no fake query callbacks.
 public static List<NavigationTileDataOutput> BuildExplicitColdTiles(){
   var produced=new List<NavigationTileDataOutput>();
   string memberPath=Path.Combine(Root,@"docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin");
   Require((File.GetAttributes(memberPath)&FileAttributes.ReparsePoint)==0,"Saved member is a reparse file");
   byte[] member=new byte[1091357];
   using(var file=new FileStream(memberPath,FileMode.Open,FileAccess.Read,FileShare.Read)){
    Require(file.Length==member.Length,"Pinned saved member extent changed");int at=0;
    while(at<member.Length){int n=file.Read(member,at,member.Length-at);if(n==0)throw new EndOfStreamException();at+=n;}
   }
   Require(Hash(member,0,member.Length)==MemberHash,"Pinned saved member hash changed");
   Require(Hash(member,422373,442996)=="0a0a38176e69847c02d65e90805da18c8b849fe1806e16bfc03bcebabc07ccee","PTHD range hash changed");
   var parsed=NavigationPathingData.Parse(member,422373,442996);
   var sources=new List<NavigationPathingByteSlice>();var cache=NavigationLayerCache.Create(4096);
   foreach(var group in parsed.Groups)foreach(var layer in group.Layers){var reference=new NavigationLayerReference(0);Require(cache.Add(layer.Payload,reference)==NavigationLayerCache.Success,"Actual layer registration failed");sources.Add(layer.Payload);}
   Require(parsed.Groups.Count==16&&sources.Count==9,"Pinned PTHD layout changed");
   const int climbCells=4;const float tolerance=1.5f,cellSize=0.25f,cellHeight=0.2f,walkHeight=2f,walkRadius=0.5f,walkClimb=0.8f;
   Console.WriteLine("CONFIG explicit cold domain: climbCells=4 tolerance=1.5 cellSize=.25 cellHeight=.2 walkHeight=2 walkRadius=.5 walkClimb=.8; cache processor=null; empty obstacle collection; obstacle application excluded; stable synchronous input; runtime values unobserved");
   long totalVertices=0,totalPolygons=0,totalBytes=0;int tileCount=0;
   for(int ordinal=0;ordinal<sources.Count;ordinal++){
    var source=sources[ordinal];var slot=cache.GetSlot(ordinal);
    Require(Object.ReferenceEquals(source,slot.Header08)&&Object.ReferenceEquals(source,slot.Blob20),"Cache slice identity changed");
    var layer=NavigationLayerData.Decode(slot.Blob20);Require(Object.ReferenceEquals(layer.Source,source),"Decoder source identity changed");
    var regions=NavigationLayerRegions.Build(layer,climbCells);
    Require(regions.Outcome==NavigationLayerRegionOutcome.Success,"Region stop "+regions.Outcome+" "+regions.Detail);
    var contours=NavigationLayerContours.Build(layer,climbCells,tolerance);
    Require(contours.Outcome==NavigationContourOutcome.Success,"Contour stop "+contours.Outcome+" "+contours.Detail);
    var polygons=NavigationLayerPolygons.Build(contours);
    Require(Object.ReferenceEquals(polygons.Source,contours),"Polygon producer lost contour result identity");
    for(int c=0;c<contours.Contours.Length;c++)Require(Object.ReferenceEquals(polygons.Inputs[c].Record,contours.Contours[c]),"Polygon producer copied contour records");
    Require(polygons.Outcome==NavigationPolygonOutcome.Success,"Polygon stop "+polygons.Outcome+" "+polygons.Detail+" RVA "+polygons.StopRva.ToString("X"));
    Require(polygons.Nvp==6&&polygons.PolygonCount>0,"No nonempty actual polygon output; cannot claim tile production");
    var vertices=new NavigationTileQuantizedVertex[polygons.VertexCount];
    for(int n=0;n<vertices.Length;n++)vertices[n]=new NavigationTileQuantizedVertex(polygons.Vertices[n*3],polygons.Vertices[n*3+1],polygons.Vertices[n*3+2]);
    var faces=new NavigationTilePolygon[polygons.PolygonCount];var areas=new byte[faces.Length];var flags=new ushort[faces.Length];
    //265C90..CA5 changes the existing mutable polygon flag array BEFORE the
    // skipped processor callback. Do not initialize unrelated fields here.
    for(int n=0;n<faces.Length;n++){
     polygons.Flags[n]=(ushort)(polygons.Areas[n]!=0?1:0);
     var v=new ushort[6];var neighbors=new ushort[6];Array.Copy(polygons.Polygons,n*12,v,0,6);Array.Copy(polygons.Polygons,n*12+6,neighbors,0,6);
     faces[n]=new NavigationTilePolygon(v,neighbors);areas[n]=polygons.Areas[n];flags[n]=polygons.Flags[n];
    }
    //265BFC..C71 reads coordinates/bounds from the current borrowed cache
    // header, not the decoder's copied header. Stable during this own call.
    int h=slot.Header08.Offset;byte[] bytes=slot.Header08.Buffer;
    int x=BitConverter.ToInt32(bytes,h+8),z=BitConverter.ToInt32(bytes,h+12),level=BitConverter.ToInt32(bytes,h+16);
    var bounds=new NavigationTileBounds(BitConverter.ToSingle(bytes,h+20),BitConverter.ToSingle(bytes,h+24),BitConverter.ToSingle(bytes,h+28),BitConverter.ToSingle(bytes,h+32),BitConverter.ToSingle(bytes,h+36),BitConverter.ToSingle(bytes,h+40));
    var input=new NavigationTileDataInput(vertices,faces,areas,flags,bounds,cellSize,cellHeight,x,z,level,walkHeight,walkRadius,walkClimb);
    var output=new NavigationTileDataOutput();var serialized=NavigationTileData.Build(input,output);
    Require(serialized.Outcome==NavigationTileDataOutcome.Published,"Tile-format stop "+serialized.Outcome+" "+serialized.Detail);
    Require(output.Bytes!=null&&output.Length==output.Bytes.Length&&output.Length>=100,"No actual serialized output");
    Console.WriteLine("TILE ordinal="+ordinal+" x="+x+" z="+z+" layer="+level+" vertices="+polygons.VertexCount+" polygons="+polygons.PolygonCount+" bytes="+output.Length+" sha256="+Hash(output.Bytes,0,output.Length));
    produced.Add(output);
    totalVertices+=vertices.Length;totalPolygons+=faces.Length;totalBytes+=output.Length;tileCount++;
   }
   Require(Hash(member,0,member.Length)==MemberHash,"Actual borrowed source was modified");
   Console.WriteLine("PASS actual tiles="+tileCount+" vertices="+totalVertices+" polygons="+totalPolygons+" bytes="+totalBytes+"; complete saved-data-to-byte production in explicit cold domain; no mesh insertion/search/game acceptance");return produced;
 }
}
