using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace GrimDynasty.ReverseClient.Actions {
 // Pure managed algorithm fixtures. Expected geometry below is derived from
 // the saved instruction contracts, not a captured native execution golden.
 // The separate pinned-sample entry accepts bytes and performs no IO.
 public static class NavigationLayerContoursTests {
  public static int Assertions {get;private set;}
  public static int Scenarios {get;private set;}
  static void Check(bool condition,string why){Assertions++;if(!condition)throw new Exception(why);}
  static void Eq(long expected,long actual,string why){Check(expected==actual,why+" expected="+expected+" actual="+actual);}
  static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
  static void Float(float expected,float actual,string why){Eq(BitConverter.ToInt32(BitConverter.GetBytes(expected),0),BitConverter.ToInt32(BitConverter.GetBytes(actual),0),why+" binary32");}
  static void Case(string name,Action body){body();Scenarios++;Console.WriteLine("PASS "+name);}
  static NavigationContourDomainException Domain(string code,Action body){
   try{body();}catch(NavigationContourDomainException e){Check(e.Code==code,"own domain expected "+code+" actual "+e.Code);return e;}
   throw new Exception("Expected own domain stop "+code);
  }
  static void Argument(Action body){try{body();}catch(ArgumentOutOfRangeException){Assertions++;return;}throw new Exception("Expected own argument range rejection");}
  static void Put(byte[] b,int at,uint value){for(int n=0;n<4;n++)b[at+n]=(byte)(value>>(8*n));}
  static byte[] Fill(int count,byte value){var b=new byte[count];for(int n=0;n<count;n++)b[n]=value;return b;}
  static string Digest(byte[] b,int offset,int count){using(var h=SHA256.Create()){return BitConverter.ToString(h.ComputeHash(b,offset,count)).Replace("-","").ToLowerInvariant();}}
  static void Bytes(byte[] expected,byte[] actual,string why){Eq(expected.Length,actual.Length,why+" length");for(int n=0;n<expected.Length;n++)Eq(expected[n],actual[n],why+" byte "+n);}
  static void Quad(NavigationContourScratch s,int index,int x,int y,int z,int edge){int p=index*4;Eq(x,s.Vertices[p],"quad x");Eq(y,s.Vertices[p+1],"quad y");Eq(z,s.Vertices[p+2],"quad z");Eq(edge,s.Vertices[p+3],"quad edge");}
  static NavigationContourScratch Scratch(int capacity,params byte[] quads){
   if(quads.Length%4!=0||quads.Length>capacity*4)throw new ArgumentException("Invalid own scratch fixture");
   var s=new NavigationContourScratch(capacity);Array.Copy(quads,s.Vertices,quads.Length);s.Count=quads.Length/4;return s;
  }
  static NavigationLayerData Layer(byte width,byte height,byte[] heights,byte[] areas,byte[] connections,byte[] regions){
   int cells=width*height;byte[][] planes={heights,areas,connections,regions};
   var raw=new byte[56+4*cells];Put(raw,0,NavigationLayerData.Magic);Put(raw,4,NavigationLayerData.Version);raw[48]=width;raw[49]=height;
   for(int n=0;n<4;n++){if(planes[n].Length!=cells)throw new ArgumentException("Invalid own plane fixture");Array.Copy(planes[n],0,raw,56+n*cells,cells);}
   var wrapped=new byte[32+raw.Length];Put(wrapped,0,NavigationPathingData.Magic);Put(wrapped,4,1);Put(wrapped,24,(uint)(raw.Length+4));Put(wrapped,28,(uint)raw.Length);Array.Copy(raw,0,wrapped,32,raw.Length);
   // Use the public parser: no internal slice constructor or invented owner.
   return NavigationLayerData.Decode(NavigationPathingData.Parse(wrapped).Groups[0].Layers[0]);
  }
  static void Set(NavigationLayerData layer,int plane,int index,byte value){var p=layer.Plane(plane);p.Array[p.Offset+index]=value;}
  static byte Get(NavigationLayerData layer,int plane,int index){var p=layer.Plane(plane);return p.Array[p.Offset+index];}
  static NavigationLayerData Rectangle(){
   const int width=8,height=8;var areas=new byte[64];var connections=new byte[64];
   for(int z=2;z<6;z++)for(int x=2;x<6;x++){
    int p=z*width+x;areas[p]=7;
    if(x>2)connections[p]|=1;if(z<5)connections[p]|=2;if(x<5)connections[p]|=4;if(z>2)connections[p]|=8;
   }
   return Layer(width,height,Fill(64,9),areas,connections,Fill(64,255));
  }
  static void RegionSuccess(NavigationLayerRegionResult r){Check(r.Outcome==NavigationLayerRegionOutcome.Success,"region stage succeeded: "+r.Outcome+" "+r.Detail);Check(r.NativeStatus==0x40000000u,"region status");}
  static void ContourSuccess(NavigationContourResult r){Check(r.Outcome==NavigationContourOutcome.Success,"contour stage succeeded: "+r.Outcome+" "+r.Detail);Check(r.NativeStatus==NavigationLayerContours.SuccessStatus,"contour status");}

  public static void Run(){
   Assertions=0;Scenarios=0;
   Case("scratch own extent and zero capacity",delegate{
    var s=new NavigationContourScratch(0);Eq(0,s.Capacity,"zero scratch capacity");Check(!NavigationLayerContours.Append(s,1,2,3,4),"zero-cap append returns false");Eq(0,s.Count,"no count write");
    Argument(delegate{new NavigationContourScratch(-1);});Argument(delegate{new NavigationContourScratch(2041);});Eq(2040,new NavigationContourScratch(2040).Capacity,"maximum own capacity");
   });
   Case("append publishes exact byte quad",delegate{
    var s=new NavigationContourScratch(2);Check(NavigationLayerContours.Append(s,258,37,-1,511),"append");Eq(1,s.Count,"count");Quad(s,0,2,37,255,255);Quad(s,1,0,0,0,0);
   });
   Case("vertical coalescing precedes full-capacity test",delegate{
    var s=Scratch(2,4,1,2,9,4,2,3,9);var owner=s.Vertices;
    Check(NavigationLayerContours.Append(s,4,33,8,9),"full-cap coalesce");Eq(2,s.Count,"count unchanged");Same(owner,s.Vertices,"same mutable storage");Quad(s,0,4,1,2,9);Quad(s,1,4,33,8,9);
   });
   Case("horizontal coalescing precedes full-capacity test",delegate{
    var s=Scratch(2,2,1,4,9,3,2,4,9);Check(NavigationLayerContours.Append(s,8,44,4,9),"full-cap coalesce");Eq(2,s.Count,"count unchanged");Quad(s,0,2,1,4,9);Quad(s,1,8,44,4,9);
   });
   Case("append full rejection preserves complete prefix",delegate{
    var s=Scratch(2,4,1,2,9,4,2,3,9);var before=(byte[])s.Vertices.Clone();
    Check(!NavigationLayerContours.Append(s,4,99,9,10),"changed edge forbids coalescing");Eq(2,s.Count,"failed count");Bytes(before,s.Vertices,"failed append prefix");
    Check(!NavigationLayerContours.Append(s,260,99,9,9),"full int x comparison before byte truncation");Bytes(before,s.Vertices,"integer coordinate mismatch prefix");
    Check(!NavigationLayerContours.Append(s,4,99,9,265),"full int edge comparison before byte truncation");Bytes(before,s.Vertices,"integer edge mismatch prefix");
   });
   Case("single existing point cannot coalesce",delegate{
    var s=Scratch(2,4,1,2,9);Check(NavigationLayerContours.Append(s,4,22,8,9),"second distinct quad");Eq(2,s.Count,"two points");Quad(s,0,4,1,2,9);Quad(s,1,4,22,8,9);
   });
   Case("neighbor low connection wins over high portal",delegate{
    var layer=Layer(3,3,Fill(9,0),Fill(9,1),Fill(9,0),new byte[]{10,11,12,13,14,15,16,17,18});Set(layer,2,4,0xff);
    Eq(13,NavigationLayerContours.Neighbor(layer,1,1,0),"west low bit");Eq(17,NavigationLayerContours.Neighbor(layer,1,1,1),"positive z low bit");Eq(15,NavigationLayerContours.Neighbor(layer,1,1,2),"east low bit");Eq(11,NavigationLayerContours.Neighbor(layer,1,1,3),"negative z low bit");
    Set(layer,3,3,77);Eq(77,NavigationLayerContours.Neighbor(layer,1,1,0),"live shared region byte");
   });
   Case("neighbor portal sentinels and ordinary absent edge",delegate{
    var layer=Layer(1,1,new byte[]{0},new byte[]{1},new byte[]{0xf0},new byte[]{8});for(int d=0;d<4;d++)Eq(248+d,NavigationLayerContours.Neighbor(layer,0,0,d),"high portal "+d);
    Set(layer,2,0,0);Eq(255,NavigationLayerContours.Neighbor(layer,0,0,0),"no edge");Domain("DirectionDomain",delegate{NavigationLayerContours.Neighbor(layer,0,0,4);});
   });
   Case("neighbor invalid low address stops instead of portal fallback",delegate{
    var layer=Layer(1,1,new byte[]{0},new byte[]{1},new byte[]{0x11},new byte[]{8});
    var e=Domain("PlaneExtent",delegate{NavigationLayerContours.Neighbor(layer,0,0,0);});Eq(0x269580,e.Rva,"actual region load stop");Eq(0x11,Get(layer,2,0),"source connection retained");
   });
   Case("distance projection and endpoint clamps",delegate{
    Float(9f,NavigationLayerContours.DistanceSquared(3,3,0,0,6,0),"interior projection");Float(13f,NavigationLayerContours.DistanceSquared(-2,3,0,0,6,0),"before start");Float(13f,NavigationLayerContours.DistanceSquared(8,3,0,0,6,0),"after end");Float(25f,NavigationLayerContours.DistanceSquared(7,9,4,5,4,5),"zero-length segment");Float(2f,NavigationLayerContours.DistanceSquared(2,0,0,0,2,2),"diagonal");
   });
   Case("distance endpoint reversal retains finite geometry",delegate{
    Float(9f,NavigationLayerContours.DistanceSquared(3,3,6,0,0,0),"reversed horizontal");Float(9f,NavigationLayerContours.DistanceSquared(3,3,0,6,0,0),"vertical projection");Float(0f,NavigationLayerContours.DistanceSquared(6,0,0,0,6,0),"endpoint exact");
   });
   Case("simplify refines square and preserves same arrays",delegate{
    var s=Scratch(8,0,1,0,255,0,2,4,255,4,3,4,255,4,4,0,255);var vertices=s.Vertices;var indices=s.Indices;
    NavigationLayerContours.Simplify(s,0f);Eq(4,s.Count,"noncollinear corners");Eq(4,s.IndexCount,"refined selected count");Same(vertices,s.Vertices,"in-place vertices");Same(indices,s.Indices,"same index workspace");
    Quad(s,0,0,1,0,255);Quad(s,1,0,2,4,255);Quad(s,2,4,3,4,255);Quad(s,3,4,4,0,255);
   });
   Case("simplify ordered threshold equality does not insert",delegate{
    var s=Scratch(8,0,1,0,255,0,2,2,255,4,3,2,255,4,4,0,255);
    // Diagonal distance squared is 3.2, so infinity cannot insert; negative
    // finite tolerance is squared and must agree with positive tolerance.
    var other=Scratch(8,0,1,0,255,0,2,2,255,4,3,2,255,4,4,0,255);
    NavigationLayerContours.Simplify(s,Single.PositiveInfinity);Eq(2,s.Count,"infinite threshold");Quad(s,0,0,1,0,255);Quad(s,1,4,3,2,255);
    var positive=Scratch(8,0,1,0,255,0,2,2,255,4,3,2,255,4,4,0,255);NavigationLayerContours.Simplify(other,-1f);NavigationLayerContours.Simplify(positive,1f);Eq(positive.Count,other.Count,"squared sign");Bytes(positive.Vertices,other.Vertices,"squared tolerance geometry");
    var exact=Scratch(8,0,1,0,0,2,2,2,0,4,3,0,0);NavigationLayerContours.Simplify(exact,2f);Eq(2,exact.Count,"best=4 equals tolerance squared=4");
   });
   Case("NaN tolerance keeps fallback endpoints and inactive suffix",delegate{
    var s=Scratch(8,3,10,3,5,0,11,0,5,2,12,3,5,4,13,4,5);var before=(byte[])s.Vertices.Clone();
    NavigationLayerContours.Simplify(s,Single.NaN);Eq(2,s.Count,"unordered threshold does not refine");Eq(2,s.IndexCount,"selected endpoints only");Eq(1,s.Indices[0],"lexicographic minimum");Eq(3,s.Indices[1],"lexicographic maximum");
    Quad(s,0,0,11,0,5);Quad(s,1,4,13,4,5);for(int n=8;n<s.Vertices.Length;n++)Eq(before[n],s.Vertices[n],"inactive suffix "+n);
   });
   Case("marker boundaries survive enormous tolerance",delegate{
    var s=Scratch(8,1,10,1,1,1,20,4,2,4,30,4,1,4,40,1,2);NavigationLayerContours.Simplify(s,Single.PositiveInfinity);Eq(4,s.Count,"all marker boundaries retained");for(int n=0;n<4;n++){Eq(n,s.Indices[n],"current boundary index");Eq(n%2==0?1:2,s.Vertices[n*4+3],"marker retained");}
   });
   Case("fallback ties retain duplicate index instead of inventing uniqueness",delegate{
    var s=Scratch(4,2,9,3,7);NavigationLayerContours.Simplify(s,Single.NaN);Eq(2,s.Count,"native fallback keeps duplicate min max");Eq(0,s.Indices[0],"min index");Eq(0,s.Indices[1],"max index");Quad(s,0,2,9,3,7);Quad(s,1,2,9,3,7);
   });
   Case("empty simplify is own stop after index reset",delegate{
    var s=new NavigationContourScratch(4);s.IndexCount=3;s.Vertices[0]=83;var before=(byte[])s.Vertices.Clone();Domain("EmptySimplificationInput",delegate{NavigationLayerContours.Simplify(s,1.5f);});Eq(0,s.IndexCount,"index count reset prefix");Eq(0,s.Count,"inactive input count");Bytes(before,s.Vertices,"no promotion of inactive storage");
   });
   Case("corner height FF sentinel allows first concrete region",delegate{
    var layer=Layer(2,2,new byte[]{10,11,12,13},Fill(4,1),Fill(4,0x10),new byte[]{255,7,7,7});var marker=new NavigationContourByte(99);
    Eq(13,NavigationLayerContours.CornerHeight(layer,1,10,1,4,marker),"maximum accepted height");Eq(1,marker.Value,"initial FF is sentinel not mismatch");
   });
   Case("corner region mismatch stays sticky across FF",delegate{
    var layer=Layer(2,2,new byte[]{10,11,12,13},Fill(4,1),Fill(4,0x10),new byte[]{7,255,7,7});var marker=new NavigationContourByte(99);
    Eq(13,NavigationLayerContours.CornerHeight(layer,1,10,1,4,marker),"height independent of mismatch");Eq(0,marker.Value,"mismatch not reset by FF");
   });
   Case("corner filters area and climb before height marker",delegate{
    var layer=Layer(2,2,new byte[]{10,11,12,99},new byte[]{1,0,1,1},Fill(4,0x10),Fill(4,3));var marker=new NavigationContourByte(99);
    Eq(12,NavigationLayerContours.CornerHeight(layer,1,10,1,2,marker),"accepted inclusive climb");Eq(1,marker.Value,"two accepted same-region cells");
    Eq(10,NavigationLayerContours.CornerHeight(layer,1,10,1,1,marker),"one accepted cell");Eq(0,marker.Value,"one cell is not marker");
    Eq(0,NavigationLayerContours.CornerHeight(layer,1,10,1,-1,marker),"negative climb accepts no ordinary delta");Eq(0,marker.Value,"marker always cleared");
   });
   Case("corner portal intersection requires exactly one bit",delegate{
    var layer=Layer(2,2,Fill(4,10),Fill(4,1),Fill(4,0x30),Fill(4,3));var marker=new NavigationContourByte(99);
    Eq(10,NavigationLayerContours.CornerHeight(layer,1,10,1,0,marker),"height");Eq(0,marker.Value,"two common bits not marker");Set(layer,2,3,0x10);NavigationLayerContours.CornerHeight(layer,1,10,1,0,marker);Eq(1,marker.Value,"one intersected bit");Set(layer,2,2,0x20);NavigationLayerContours.CornerHeight(layer,1,10,1,0,marker);Eq(0,marker.Value,"empty intersection");
   });
   Case("corner outside bounds and null marker stop",delegate{
    var layer=Layer(1,1,new byte[]{8},new byte[]{1},new byte[]{0x10},new byte[]{3});var marker=new NavigationContourByte(99);
    Eq(8,NavigationLayerContours.CornerHeight(layer,0,8,0,0,marker),"single in-bounds corner cell");Eq(0,marker.Value,"only one accepted cell");Eq(0,NavigationLayerContours.CornerHeight(layer,-3,8,-3,0,marker),"no in-bounds cells");Domain("NullMarkerCell",delegate{NavigationLayerContours.CornerHeight(layer,0,8,0,0,null);});
   });
   Case("real region rectangle traces and builds four corners",delegate{
    var layer=Rectangle();var region=NavigationLayerRegions.Build(layer,4);RegionSuccess(region);Eq(1,layer.RegionCount,"single connected rectangle");var owned=layer.WorkData.Array;string before=Digest(owned,0,owned.Length);
    var s=new NavigationContourScratch(64);Check(NavigationLayerContours.Trace(layer,2,2,s),"trace");Eq(4,s.Count,"coalesced four corners");Quad(s,0,2,9,2,255);Quad(s,1,2,9,6,255);Quad(s,2,6,9,6,255);Quad(s,3,6,9,2,255);
    var r=NavigationLayerContours.Build(layer,4,1.5f);ContourSuccess(r);Same(layer,r.Layer,"same layer identity");Same(owned,r.Layer.WorkData.Array,"same owned planes");Eq(1,r.Contours.Length,"region indexed records");Eq(4,r.Contours[0].VertexCount,"valid noncollinear contour");Eq(0,r.Contours[0].RegionId,"region id");Eq(7,r.Contours[0].Area,"area");
    Bytes(new byte[]{2,9,2,15,2,9,6,15,6,9,6,15,6,9,2,15},r.Contours[0].Vertices,"rectangle output");Check(before==Digest(owned,0,owned.Length),"contours leave layer bytes unchanged");
   });
   Case("trace capacity false preserves already-written first vertex",delegate{
    var layer=Rectangle();RegionSuccess(NavigationLayerRegions.Build(layer,4));var s=new NavigationContourScratch(1);Check(!NavigationLayerContours.Trace(layer,2,2,s),"second corner cannot append");Eq(1,s.Count,"prefix count");Quad(s,0,2,9,2,255);
   });
   Case("trace width-height iteration cap is not closure",delegate{
    var layer=Layer(2,1,new byte[]{6,6},new byte[]{1,1},new byte[]{4,1},new byte[]{255,255});RegionSuccess(NavigationLayerRegions.Build(layer,4));var s=new NavigationContourScratch(12);
    Check(NavigationLayerContours.Trace(layer,0,0,s),"observed success after two iterations");Eq(2,s.Count,"only two emitted points");Quad(s,0,0,6,0,255);Quad(s,1,0,6,1,255);
    var r=NavigationLayerContours.Build(layer,4,1.5f);ContourSuccess(r);Eq(2,r.Contours[0].VertexCount,"native success does not establish valid polygon");Check(r.Contours[0].VertexCount<3,"explicitly invalid for downstream polygon use");
   });
   Case("single-cell iteration cap reaches explicit empty simplification stop",delegate{
    var layer=Layer(1,1,new byte[]{6},new byte[]{1},new byte[]{0},new byte[]{255});RegionSuccess(NavigationLayerRegions.Build(layer,4));var s=new NavigationContourScratch(8);
    Check(NavigationLayerContours.Trace(layer,0,0,s),"one iteration returns true");Eq(0,s.Count,"same first and last point removed");var r=NavigationLayerContours.Build(layer,4,1.5f);
    Check(r.Outcome==NavigationContourOutcome.OwnUnsupported,"empty native read is own unsupported");Check(!r.NativeStatus.HasValue,"no invented native failure status");Check(r.Detail=="EmptySimplificationInput","precise stop");Eq(0,r.Scratch.Count,"trace prefix retained");Eq(0,r.Contours[0].VertexCount,"no invented rectangle");Eq(1,r.Contours[0].Area,"area published before stop");
   });
   Case("region record mismatch preserves record prefix and reports own stop",delegate{
    var layer=Layer(2,1,new byte[]{6,6},new byte[]{7,7},new byte[]{0,0},new byte[]{255,3});layer.RegionCount=1;var r=NavigationLayerContours.Build(layer,4,1.5f);
    Check(r.Outcome==NavigationContourOutcome.OwnUnsupported,"record index own domain");Check(r.Detail=="RegionRecordExtent","precise record stop");Check(!r.NativeStatus.HasValue,"not native status");Eq(1,r.CellIndex,"first invalid cell");Eq(1,r.Contours.Length,"allocated prefix");Eq(0,r.Contours[0].VertexCount,"no fabricated contour");
   });
   Case("changed dimensions stop before scratch and records",delegate{
    var layer=Rectangle();RegionSuccess(NavigationLayerRegions.Build(layer,4));layer.Header.Array[layer.Header.Offset+48]=7;var r=NavigationLayerContours.Build(layer,4,1.5f);
    Check(r.Outcome==NavigationContourOutcome.OwnUnsupported,"changed extent domain");Check(r.Detail=="HeaderDimensionsChanged","precise stop");Same(null,r.Scratch,"scratch not allocated");Same(null,r.Contours,"records not allocated");Check(!r.NativeStatus.HasValue,"no native status");
   });
   Case("empty traversable layer produces no invented contours",delegate{
    var layer=Layer(4,4,Fill(16,0),Fill(16,0),Fill(16,0),Fill(16,99));RegionSuccess(NavigationLayerRegions.Build(layer,4));Eq(0,layer.RegionCount,"no active region");var r=NavigationLayerContours.Build(layer,4,1.5f);ContourSuccess(r);Eq(0,r.Contours.Length,"zero records");Eq(0,r.Scratch.Count,"no active scratch vertices");
   });
   Console.WriteLine("NavigationLayerContours: "+Scenarios+" scenarios, "+Assertions+" assertions passed (pure memory; no native runtime comparison)");
  }

  // The caller supplies the already extracted, pinned member. There are no
  // paths, files, native loads, backend requests, or automatic sample fallback.
  // Explicit climb/tolerance are experimental inputs, not observed runtime
  // defaults. Success here is contour production, not polygon publication.
  public static void RunSavedMainMenu(byte[] bytes){
   Case("saved mainmenu cache decode region contour pipeline",delegate{
    if(bytes==null)throw new ArgumentNullException("bytes");Eq(1091357,bytes.Length,"fixed whole member size");
    const string memberHash="a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289";
    const string pathingHash="0a0a38176e69847c02d65e90805da18c8b849fe1806e16bfc03bcebabc07ccee";
    Check(Digest(bytes,0,bytes.Length)==memberHash,"fixed whole-member SHA256");Check(Digest(bytes,422373,442996)==pathingHash,"fixed PTHD SHA256");
    var parsed=NavigationPathingData.Parse(bytes,422373,442996);Eq(16,parsed.Groups.Count,"fixed group count");
    var sources=new List<NavigationPathingByteSlice>();foreach(var group in parsed.Groups)foreach(var layer in group.Layers)sources.Add(layer.Payload);Eq(9,sources.Count,"fixed layer count");
    var cache=NavigationLayerCache.Create(4096);var references=new List<uint>();
    foreach(var source in sources){var reference=new NavigationLayerReference(0xeeeeeeeeu);Eq(NavigationLayerCache.Success,cache.Add(source,reference),"saved layer registration");references.Add(reference.Value);}
    Eq(9,cache.Count,"registered layer count");
    Console.WriteLine("CONFIG explicitClimb=4 explicitTolerance=1.5 managedCapacity=4096; these are experiment parameters, not observed native runtime defaults");
    long totalContours=0,totalValid=0,totalVertices=0,totalMarkers=0;
    for(int n=0;n<sources.Count;n++){
     var slot=cache.GetSlot(n);Same(sources[n],slot.Header08,"same header slice "+n);Same(sources[n],slot.Blob20,"same payload slice "+n);Same(bytes,slot.Blob20.Buffer,"same member owner "+n);Eq(references[n],cache.EncodeReference(slot),"same reference "+n);
     var layer=NavigationLayerData.Decode(slot.Blob20);Same(slot.Blob20,layer.Source,"decoder borrowed identity "+n);var owner=layer.WorkData.Array;
     var regions=NavigationLayerRegions.Build(layer,4);RegionSuccess(regions);Same(layer,regions.Layer,"region layer identity "+n);Same(owner,regions.Regions.Array,"region owned plane identity "+n);
     var result=NavigationLayerContours.Build(layer,4,1.5f);ContourSuccess(result);Same(layer,result.Layer,"contour layer identity "+n);Same(owner,result.Layer.WorkData.Array,"contour owned plane identity "+n);
     long valid=0,vertices=0,markers=0;string first="none";
     for(int c=0;c<result.Contours.Length;c++){
      var contour=result.Contours[c];Check(contour!=null,"allocated contour record "+n+":"+c);
      if(contour.VertexCount<3)continue;valid++;vertices+=contour.VertexCount;
      Check(contour.Vertices!=null&&contour.Vertices.Length==contour.VertexCount*4,"valid contour extent "+n+":"+c);
      for(int v=0;v<contour.VertexCount;v++)if((contour.Vertices[v*4+3]&0x80)!=0){if(markers==0)first="("+contour.RegionId+","+v+")";markers++;}
     }
     totalContours+=result.Contours.Length;totalValid+=valid;totalVertices+=vertices;totalMarkers+=markers;
     Console.WriteLine("CONTOURS layer="+n+" reference="+references[n]+" climb=4 tolerance=1.5 allRecords="+result.Contours.Length+" validAtLeast3="+valid+" validVertices="+vertices+" validMarker80="+markers+" firstMarker80(region,index)="+first+" outcome="+result.Outcome+" detail="+result.Detail);
    }
    Check(Digest(bytes,0,bytes.Length)==memberHash,"complete borrowed member unchanged");
    Console.WriteLine("CONTOURS TOTAL layers="+sources.Count+" allRecords="+totalContours+" validAtLeast3="+totalValid+" validVertices="+totalVertices+" validMarker80="+totalMarkers+"; short contours excluded from valid counts; no final polygon mesh or client compatibility claim");
   });
   Console.WriteLine("NavigationLayerContours saved mainmenu passed; cumulative "+Scenarios+" scenarios, "+Assertions+" assertions");
  }
 }
 public static class NavigationLayerContoursTestProgram {
  public static int Main(){try{NavigationLayerContoursTests.Run();return 0;}catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}}
 }
}
