using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Fixed own fixtures for the saved cold 26E3F0 instruction path. These are not
// captured native tile goldens. No real sample, native DLL, query, allocator
// callback, contour builder, polygon generator or tile publisher is executed.
// Arbitrary ushort flags test 26E3F0's field width; they are NOT claimed to be
// the flags produced by cold 2658D0, whose caller adapter must supply area?1:0.
public static class NavigationTileDataTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}
 public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object a,object b,string why){Check(Object.ReferenceEquals(a,b),why);}
 static void Test(string name,Action body){body();scenarios++;Console.WriteLine("PASS "+name);}
 static uint U32(byte[] bytes,int at){return (uint)bytes[at]|((uint)bytes[at+1]<<8)|((uint)bytes[at+2]<<16)|((uint)bytes[at+3]<<24);}
 static ushort U16(byte[] bytes,int at){return (ushort)(bytes[at]|(bytes[at+1]<<8));}
 static float Float(uint bits){return BitConverter.ToSingle(BitConverter.GetBytes(bits),0);}
 static uint Bits(float value){return unchecked((uint)BitConverter.ToInt32(BitConverter.GetBytes(value),0));}
 static void Bytes(byte[] actual,int at,params byte[] expected){bool same=true;for(int i=0;i<expected.Length;i++)if(actual[at+i]!=expected[i])same=false;Check(same,"Expected literal byte sequence at "+at);}
 static void Zero(byte[] actual,int at,int length){bool zero=true;for(int i=0;i<length;i++)if(actual[at+i]!=0)zero=false;Check(zero,"Zero reserved bytes at "+at+" length "+length);}
 static NavigationTileQuantizedVertex V(ushort x,ushort y,ushort z){return new NavigationTileQuantizedVertex(x,y,z);}
 static NavigationTilePolygon P(ushort[] indices,ushort[] neighbors){return new NavigationTilePolygon(indices,neighbors);}
 static ushort[] Closed(){return new ushort[]{65535,65535,65535,65535,65535,65535};}
 static NavigationTileBounds Bounds(){return new NavigationTileBounds(-10,2,-5,10,12,15);}
 static NavigationTileDataInput Input(NavigationTileQuantizedVertex[] vertices,NavigationTilePolygon[] polygons,byte[] areas,ushort[] flags,NavigationTileBounds bounds,float cellSize,float cellHeight){
  return new NavigationTileDataInput(vertices,polygons,areas,flags,bounds,cellSize,cellHeight,-7,11,2,1.5f,0.5f,1.0f);
 }
 static NavigationTileDataInput Triangle(){return Input(new[]{V(0,0,0),V(1,3,0),V(0,0,1)},new[]{P(new ushort[]{0,1,2,65535,65535,65535},Closed())},new byte[]{0xC3},new ushort[]{0xBEEF},Bounds(),2.0f,0.5f);}
 static NavigationTileDataInput Copy(NavigationTileDataInput i,NavigationTileQuantizedVertex[] vertices,NavigationTilePolygon[] polygons,byte[] areas,ushort[] flags,NavigationTileBounds bounds,float cs,float ch){return Input(vertices,polygons,areas,flags,bounds,cs,ch);}
 static byte[] Published(NavigationTileDataInput input,int expectedLength){
  var output=new NavigationTileDataOutput();var result=NavigationTileData.Build(input,output);
  Equal(NavigationTileDataOutcome.Published,result.Outcome,"Cold tile serialized");Same(output,result.Output,"Shared publication object");Equal(expectedLength,output.Length,"Exact output length");Equal(expectedLength,output.Bytes.Length,"Owned array length");return output.Bytes;
 }
 static void Rejected(NavigationTileDataInput input,string detail,int budget){
  byte[] previous={0xAA,0xBB,0xCC};var output=new NavigationTileDataOutput{Bytes=previous,Length=123};var result=NavigationTileData.Build(input,output,budget);
  Equal(NavigationTileDataOutcome.OwnUnsupported,result.Outcome,"Explicit own-domain rejection, not native false");Equal(detail,result.Detail,"Specific rejection");Same(output,result.Output,"Same output owner on failure");Same(previous,output.Bytes,"Previous output array not replaced");Equal(123,output.Length,"Previous length not cleared");Bytes(previous,0,0xAA,0xBB,0xCC);
 }
 static void Rejected(NavigationTileDataInput input,string detail){Rejected(input,detail,NavigationTileData.OwnMaximumOutputBytes);}

 public static void Run(){
  scenarios=0;assertions=0;
  Test("Triangle produces exact VAND7 header and 220-byte layout",delegate{
   var input=Triangle();byte[] bytes=Published(input,220);
   uint[] header={0x444E4156u,7,0xFFFFFFF9u,11,2,0,1,3,3,1,0,1,0,0,1,0x3FC00000u,0x3F000000u,0x3F800000u,0xC1200000u,0x40000000u,0xC0A00000u,0x41200000u,0x41400000u,0x41700000u,0x3F000000u};
   for(int i=0;i<header.Length;i++)Equal(header[i],U32(bytes,i*4),"Literal header word "+i);
   Bytes(bytes,0,0x56,0x41,0x4E,0x44,7,0,0,0); // VAND, version7, little-endian.
   uint[] xyz={0xC1200000u,0x40000000u,0xC0A00000u,0xC1000000u,0x40600000u,0xC0A00000u,0xC1200000u,0x40000000u,0xC0400000u};
   for(int i=0;i<xyz.Length;i++)Equal(xyz[i],U32(bytes,100+4*i),"Literal transformed coordinate "+i);
   Zero(bytes,136,4);Bytes(bytes,140,0,0,1,0,2,0,0,0,0,0,0,0);Zero(bytes,152,12);Bytes(bytes,164,0xEF,0xBE,3,3);
   Zero(bytes,168,36);Bytes(bytes,204,0,0,0,0,0,0,0,0,0,1,0,0);Bytes(bytes,216,0,1,2,0x15);
   Equal((byte)0xC3,input.Areas[0],"Source area bits not cleared");Equal((ushort)0xBEEF,input.Flags[0],"Source flags retained");Equal((ushort)65535,input.Polygons[0].Vertices[3],"Source terminator retained");
  });
  Test("Two polygons keep directed adjacency and cumulative detail triangle bases",delegate{
   var vertices=new[]{V(0,0,0),V(1,0,0),V(1,0,1),V(0,0,1)};
   var first=P(new ushort[]{0,1,2,65535,65535,65535},new ushort[]{65535,65535,1,65535,65535,65535});
   var second=P(new ushort[]{0,2,3,65535,65535,65535},new ushort[]{0,65535,65535,65535,65535,65535});
   byte[] bytes=Published(Input(vertices,new[]{first,second},new byte[]{17,60},new ushort[]{0x1111,0x2222},Bounds(),2,0.5f),316);
   Equal(2u,U32(bytes,24),"Two polygons");Equal(6u,U32(bytes,32),"Six reserved links");Equal(2u,U32(bytes,44),"Two triangles");
   Equal((ushort)2,U16(bytes,148+16+4),"Zero-based neighbor one serialized as two");Equal((ushort)1,U16(bytes,180+16),"Zero-based neighbor zero serialized as one");
   Bytes(bytes,176,0x11,0x11,3,17);Bytes(bytes,208,0x22,0x22,3,60);Zero(bytes,212,72);
   Bytes(bytes,284,0,0,0,0,0,0,0,0,0,1,0,0);Bytes(bytes,296,0,0,0,0,1,0,0,0,0,1,0,0);Bytes(bytes,308,0,1,2,0x15,0,1,2,0x15);
  });
  Test("Six-slot portal mapping and fan edge flags follow actual nonsequential encodings",delegate{
   var vertices=new[]{V(0,0,0),V(1,0,0),V(2,0,1),V(2,0,2),V(1,0,3),V(0,0,2)};
   var polygon=P(new ushort[]{0,1,2,3,4,5},new ushort[]{0x8000,0xA001,0x8002,0x8003,0x800F,0});
   byte[] bytes=Published(Input(vertices,new[]{polygon},new byte[]{255},new ushort[]{0xAAFF},Bounds(),2,0.5f),400);
   Equal(14u,U32(bytes,32),"Six edges plus two links per four portals");Equal(4u,U32(bytes,44),"Hexagon has four generated fan triangles");
   ushort[] neighbors={0x8004,0x8002,0x8000,0x8006,0,1};for(int i=0;i<6;i++)Equal(neighbors[i],U16(bytes,172+16+2*i),"Literal portal/neighbor encoding "+i);
   Bytes(bytes,200,0xFF,0xAA,6,63);Zero(bytes,204,168);Bytes(bytes,372,0,0,0,0,0,0,0,0,0,4,0,0);
   Bytes(bytes,384,0,1,2,5,0,2,3,4,0,3,4,4,0,4,5,0x14);
  });
  Test("First FFFF ends a polygon and unused slots serialize as zero",delegate{
   var input=Triangle();input.Polygons[0].Vertices[4]=65534;input.Polygons[0].Vertices[5]=999;input.Polygons[0].Neighbors[3]=0x8004;input.Polygons[0].Neighbors[4]=12345;
   byte[] bytes=Published(input,220);Zero(bytes,146,6);Zero(bytes,158,6);Equal(3u,U32(bytes,32),"Ignored tail adds no link capacity");Equal((byte)3,bytes[166],"Ignored tail adds no vertices");
  });
  Test("Separate binary32 multiply and add differ from a fused expression",delegate{
   var input=Triangle();var vertices=new[]{V(3,0,0),V(0,1,0),V(0,0,1)};
   var bounds=new NavigationTileBounds(-3,Float(0x80000000u),0,10,10,10);
   byte[] bytes=Published(Input(vertices,input.Polygons,input.Areas,input.Flags,bounds,Float(0x3F800001u),0.5f),220);
   Equal(0x35000000u,U32(bytes,100),"3*(1+2^-23) rounds before subtracting3");Check(U32(bytes,100)!=0x34C00000u,"Not fused exact product-add");
   Equal(0x80000000u,U32(bytes,76),"Opaque negative zero bound preserved in header");Equal(0u,U32(bytes,104),"Positive zero product plus negative zero rounds to positive zero");Equal(0x3F7FFFFEu,U32(bytes,96),"Explicit float32 reciprocal");
  });
  Test("Full ushort quantized coordinates are unsigned",delegate{
   var input=Triangle();var vertices=new[]{V(65535,65535,65535),V(1,0,0),V(0,0,1)};
   byte[] bytes=Published(Input(vertices,input.Polygons,input.Areas,input.Flags,new NavigationTileBounds(0,0,0,70000,70000,70000),1,1),220);
   Equal(0x477FFF00u,U32(bytes,100),"X 65535 is not signed minus one");Equal(0x477FFF00u,U32(bytes,104),"Y 65535");Equal(0x477FFF00u,U32(bytes,108),"Z 65535");
  });
  Test("Null inputs and invalid top-level counts preserve previous publication",delegate{
   var i=Triangle();Rejected(null,"NullInput");
   Rejected(Copy(i,new NavigationTileQuantizedVertex[0],i.Polygons,i.Areas,i.Flags,i.Bounds,i.CellSize,i.CellHeight),"VertexCountDomain");
   Rejected(Copy(i,new NavigationTileQuantizedVertex[65535],i.Polygons,i.Areas,i.Flags,i.Bounds,i.CellSize,i.CellHeight),"VertexCountDomain");
   Rejected(Copy(i,i.Vertices,new NavigationTilePolygon[0],i.Areas,i.Flags,i.Bounds,i.CellSize,i.CellHeight),"PolygonCountDomain");
   Rejected(Copy(i,i.Vertices,i.Polygons,new byte[0],i.Flags,i.Bounds,i.CellSize,i.CellHeight),"AttributeExtent");
   try{NavigationTileData.Build(i,null);throw new Exception("Expected output argument rejection");}catch(ArgumentNullException){assertions++;}
  });
  Test("Malformed polygon extents arity and vertex indices are own rejections",delegate{
   var i=Triangle();
   Rejected(Copy(i,i.Vertices,new[]{P(new ushort[]{0,1,2},Closed())},i.Areas,i.Flags,i.Bounds,i.CellSize,i.CellHeight),"PolygonSlotExtent");
   Rejected(Copy(i,i.Vertices,new[]{P(new ushort[]{0,1,65535,65535,65535,65535},Closed())},i.Areas,i.Flags,i.Bounds,i.CellSize,i.CellHeight),"PolygonArityDomain");
   Rejected(Copy(i,i.Vertices,new[]{P(new ushort[]{0,1,3,65535,65535,65535},Closed())},i.Areas,i.Flags,i.Bounds,i.CellSize,i.CellHeight),"VertexIndexDomain");
  });
  Test("Unknown portal directions and out-of-range internal indices are not skipped",delegate{
   var i=Triangle();i.Polygons[0].Neighbors[0]=0x8004;Rejected(i,"PortalDirectionDomain");i.Polygons[0].Neighbors[0]=1;Rejected(i,"NeighborIndexDomain");
  });
  Test("Unsupported float domains stop without publishing default coordinates",delegate{
   var i=Triangle();
   foreach(float scale in new[]{0f,-1f,Single.NaN,Single.PositiveInfinity,Float(1)})Rejected(Copy(i,i.Vertices,i.Polygons,i.Areas,i.Flags,i.Bounds,scale,i.CellHeight),"CellScaleDomain");
   Rejected(Copy(i,i.Vertices,i.Polygons,i.Areas,i.Flags,i.Bounds,Single.MaxValue,i.CellHeight),"ReciprocalDomain");
   Rejected(Copy(i,i.Vertices,i.Polygons,i.Areas,i.Flags,i.Bounds,i.CellSize,Single.MaxValue),"WorldCoordinateDomain");
   Rejected(Copy(i,new[]{V(0,1,0),V(1,0,0),V(0,0,1)},i.Polygons,i.Areas,i.Flags,new NavigationTileBounds(0,Float(0x80800000u),0,10,10,10),i.CellSize,Float(0x00800001u)),"WorldCoordinateDomain");
   Rejected(Copy(i,i.Vertices,i.Polygons,i.Areas,i.Flags,new NavigationTileBounds(2,0,0,1,1,1),i.CellSize,i.CellHeight),"BoundsDomain");
   Rejected(new NavigationTileDataInput(i.Vertices,i.Polygons,i.Areas,i.Flags,i.Bounds,i.CellSize,i.CellHeight,0,0,0,-1,0,0),"WalkConfigurationDomain");
  });
  Test("Exact budget boundary leaves no partially published array",delegate{
   var i=Triangle();Rejected(i,"OwnOutputBudget",219);Rejected(i,"OwnOutputBudget",-1);
   var output=new NavigationTileDataOutput();Equal(NavigationTileDataOutcome.Published,NavigationTileData.Build(i,output,220).Outcome,"Exact 220-byte budget accepted");Equal(220,output.Length,"Exact resulting size");
  });
  Test("Success replaces both output fields and copied bytes outlive input mutation",delegate{
   var i=Triangle();byte[] old={1,2,3};var output=new NavigationTileDataOutput{Bytes=old,Length=77};var result=NavigationTileData.Build(i,output);
   Equal(NavigationTileDataOutcome.Published,result.Outcome,"Success");Check(!Object.ReferenceEquals(old,output.Bytes),"Fresh owned publication");Equal(220,output.Length,"Length replaced after bytes");Bytes(old,0,1,2,3);
   byte[] published=output.Bytes;i.Vertices[0]=V(99,99,99);i.Areas[0]=0;i.Flags[0]=0;i.Polygons[0].Vertices[0]=2;
   Equal(0xC1200000u,U32(published,100),"Published float coordinate not an input view");Bytes(published,140,0,0);Bytes(published,164,0xEF,0xBE,3,3);Same(output,result.Output,"Result retains same output owner");
  });
 }
}
public static class NavigationTileDataTestProgram {
 public static int Main(){try{NavigationTileDataTests.Run();Console.WriteLine("PASS "+NavigationTileDataTests.Scenarios+" scenarios, "+NavigationTileDataTests.Assertions+" assertions; cold VAND7 byte serialization only; no real tile golden, native execution, tile insertion or navigation compatibility");return 0;}catch(Exception e){Console.Error.WriteLine("FAIL "+e);return 1;}}
}
