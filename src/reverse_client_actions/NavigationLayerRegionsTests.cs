using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using GrimDynasty.ReverseClient.Actions;

// Integer algorithm fixtures plus an OPTIONAL pinned saved-data experiment.
// No native function execution, backend, allocator mock or expected callback
// result. None of the generated region outputs is a captured native golden.
public static class NavigationLayerRegionsTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}
 public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Test(string name,Action body){body();scenarios++;Console.WriteLine("PASS "+name);}
 static void Put(byte[] bytes,int at,uint value){for(int n=0;n<4;n++)bytes[at+n]=(byte)(value>>(8*n));}
 static string Hash(byte[] bytes,int offset,int length){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes,offset,length)).Replace("-","").ToLowerInvariant();}
 static string Hash(ArraySegment<byte> view){return Hash(view.Array,view.Offset,view.Count);}
 static byte[] Filled(int size,byte value){var bytes=new byte[size];for(int i=0;i<size;i++)bytes[i]=value;return bytes;}
 static NavigationLayerData Fixture(byte width,byte height,byte[] heights,byte[] areas){
  int cells=width*height;if(heights.Length!=cells||areas.Length!=cells)throw new ArgumentException("Bad own fixture extent");
  var raw=new byte[56+4*cells];Put(raw,0,NavigationLayerData.Magic);Put(raw,4,NavigationLayerData.Version);raw[48]=width;raw[49]=height;raw[54]=0xB7;
  Buffer.BlockCopy(heights,0,raw,56,cells);Buffer.BlockCopy(areas,0,raw,56+cells,cells);
  for(int i=0;i<cells;i++){raw[56+2*cells+i]=(byte)(i%251);raw[56+3*cells+i]=91;}
  var pthd=new byte[32+raw.Length];Put(pthd,0,0x44485450u);Put(pthd,4,1);Put(pthd,24,(uint)(raw.Length+4));Put(pthd,28,(uint)raw.Length);Buffer.BlockCopy(raw,0,pthd,32,raw.Length);
  return NavigationLayerData.Decode(NavigationPathingData.Parse(pthd).Groups[0].Layers[0]);
 }
 static NavigationLayerData Flat(byte width,byte height,params byte[] areas){return Fixture(width,height,new byte[width*height],areas);}
 static void Plane(NavigationLayerData layer,byte[] expected){
  var regions=layer.Plane(3);Equal(expected.Length,regions.Count,"Expected plane extent");bool same=true;
  for(int i=0;i<expected.Length;i++)if(expected[i]!=regions.Array[regions.Offset+i]){same=false;break;}
  Check(same,"Exact region byte sequence");
 }
 static void Success(NavigationLayerRegionResult result,int raw,int count){
  Equal(NavigationLayerRegionOutcome.Success,result.Outcome,"Successful managed region stage");Equal((uint?)0x40000000u,result.NativeStatus,"Observed success status");
  Equal(raw,result.RawRegionCount,"Raw row-sweep region count");Equal((byte)count,result.Layer.RegionCount,"Dense region count");Check(result.RegionCountWritten,"Count written on success");
  Same(result.Layer.WorkData.Array,result.Regions.Array,"Output shares mutable owned work identity");Equal(result.Layer.Plane(3).Offset,result.Regions.Offset,"Output is fourth plane, not copy");
 }

 public static void RunMemory(){
  scenarios=0;assertions=0;
  Test("Flat area reuses sweep and retains owned planes/source identity",delegate{
   var layer=Fixture(3,2,Filled(6,12),Filled(6,7));var owner=layer.WorkData.Array;string input=Hash(layer.Source.Buffer,0,layer.Source.Buffer.Length),prefix=Hash(owner,0,56+3*layer.CellCount);
   var result=NavigationLayerRegions.Build(layer,0);Success(result,1,1);Plane(layer,new byte[6]);Same(layer,result.Layer,"Same mutable layer returned");Same(owner,result.Regions.Array,"No detached output plane");
   Equal(input,Hash(layer.Source.Buffer,0,layer.Source.Buffer.Length),"Borrowed source unchanged");Equal(prefix,Hash(owner,0,56+3*layer.CellCount),"Header/heights/areas/connections unchanged");
  });
  Test("Signed climb and exact unsigned-height difference boundary",delegate{
   var inclusive=Fixture(2,1,new byte[]{0,255},new byte[]{1,1});Success(NavigationLayerRegions.Build(inclusive,255),1,1);Plane(inclusive,new byte[]{0,0});
   var split=Fixture(2,1,new byte[]{0,255},new byte[]{1,1});Success(NavigationLayerRegions.Build(split,254),2,2);Plane(split,new byte[]{0,1});
   var negative=Flat(2,2,1,1,1,1);Success(NavigationLayerRegions.Build(negative,-1),4,4);Plane(negative,new byte[]{0,1,2,3});
   var max=Fixture(2,1,new byte[]{255,0},new byte[]{1,1});Success(NavigationLayerRegions.Build(max,Int32.MaxValue),1,1);
  });
  Test("Zero areas stay FF and different areas never share regions",delegate{
   var layer=Flat(5,2,0,9,10,9,0,0,9,10,9,0);Success(NavigationLayerRegions.Build(layer,0),3,3);Plane(layer,new byte[]{255,0,1,2,255,255,0,1,2,255});
  });
  Test("One sweep meeting different upper regions creates then merges a raw region",delegate{
   var layer=Flat(3,2,1,0,1,1,1,1);Success(NavigationLayerRegions.Build(layer,0),3,1);Plane(layer,new byte[]{0,255,0,0,0,0});
  });
  Test("Upper contacts split sweeps and consecutive neighbor duplicates collapse",delegate{
   var layer=Flat(4,2,1,1,1,1,1,1,0,1);Success(NavigationLayerRegions.Build(layer,0),3,1);Plane(layer,new byte[]{0,0,0,0,0,0,255,0});
  });
  Test("Ring keeps multiple group connections and first equal-size merge candidate",delegate{
   // Four raw regions, then two dense groups. Choosing the second equal-size
   // neighbor or globally merging any connected regions changes these bytes.
   var layer=Flat(3,3,1,1,1,1,0,1,1,1,1);Success(NavigationLayerRegions.Build(layer,0),4,2);Plane(layer,new byte[]{1,1,1,1,255,0,1,1,1});
  });
  Test("255 distinct regions are valid while FF remains the cell sentinel",delegate{
   var areas=new byte[255];var expected=new byte[255];for(int i=0;i<255;i++){areas[i]=(byte)(1+i%2);expected[i]=(byte)i;}
   var layer=Flat(255,1,areas);Success(NavigationLayerRegions.Build(layer,0),255,255);Plane(layer,expected);
  });
  Test("Region ID failure leaves final prior rows and local current row IDs",delegate{
   var areas=new byte[765];for(int i=0;i<255;i++){areas[i]=(byte)(1+i%2);areas[255+i]=3;areas[510+i]=4;}
   var layer=Flat(255,3,areas);layer.RegionCount=197;var result=NavigationLayerRegions.Build(layer,0);
   Equal(NavigationLayerRegionOutcome.NativeFailure,result.Outcome,"Observed ID exhaustion");Equal((uint?)0x80000010u,result.NativeStatus,"Exact native failure status");Equal(255,result.RawRegionCount,"All available raw IDs assigned");Equal(255,result.CellIndex,"Failure row starts at second row");Check(!result.RegionCountWritten,"Failure does not publish count");Equal((byte)197,layer.RegionCount,"Old count preserved");Same(layer.WorkData.Array,result.Regions.Array,"Failure returns shared partial output");
   var expected=new byte[765];for(int i=0;i<255;i++){expected[i]=(byte)i;expected[255+i]=0;expected[510+i]=255;}Plane(layer,expected);
  });
  Test("Seventeenth neighbor rejects before native record overrun",delegate{
   var areas=new byte[66];for(int i=0;i<33;i++){areas[i]=(byte)(i%2==0?1:0);areas[33+i]=1;}
   var layer=Flat(33,2,areas);layer.RegionCount=88;var result=NavigationLayerRegions.Build(layer,0);
   Equal(NavigationLayerRegionOutcome.OwnUnsupported,result.Outcome,"Own safe-domain rejection");Equal((uint?)null,result.NativeStatus,"No invented native failure status");Equal("CurrentRegionNeighborCapacity",result.Detail,"Precise bounded-record stop");Equal(65,result.CellIndex,"Seventeenth adjacency at last lower cell");Equal(18,result.RawRegionCount,"Completed row-sweep prefix");Equal((byte)88,layer.RegionCount,"Count unchanged before merge");Check(!result.RegionCountWritten,"No false final count");
   var expected=new byte[66];for(int i=0;i<33;i++){expected[i]=(byte)(i%2==0?i/2:255);expected[33+i]=17;}Plane(layer,expected);
  });
  Test("Live dimensions changed after allocation stop without clearing output",delegate{
   var layer=Flat(2,2,1,1,1,1);layer.RegionCount=66;var before=Hash(layer.Plane(3));layer.Header.Array[layer.Header.Offset+48]=1;
   var result=NavigationLayerRegions.Build(layer,0);Equal(NavigationLayerRegionOutcome.OwnUnsupported,result.Outcome,"No snapshot fallback");Equal("HeaderDimensionsChanged",result.Detail,"Own extent domain");Equal((uint?)null,result.NativeStatus,"Not native status");Equal(before,Hash(layer.Plane(3)),"Own preflight rejection has no output writes");Equal((byte)66,layer.RegionCount,"Old count unchanged");
  });
  Test("Rebuild uses current owned area bytes and updates retained output view",delegate{
   var layer=Flat(3,1,1,1,1);var first=NavigationLayerRegions.Build(layer,0);Success(first,1,1);
   var areas=layer.Plane(1);areas.Array[areas.Offset+1]=0;var second=NavigationLayerRegions.Build(layer,0);Success(second,2,2);Plane(layer,new byte[]{0,255,1});
   Same(first.Regions.Array,second.Regions.Array,"Both result views share current state");Equal((byte)255,first.Regions.Array[first.Regions.Offset+1],"Earlier borrowed result sees later build");Equal((byte)2,first.Layer.RegionCount,"Earlier result retains same mutable count owner");
  });
  Test("Empty and all-blocked input use documented managed zero-size allocation",delegate{
   foreach(var layer in new NavigationLayerData[]{Flat(0,0,new byte[0]),Flat(0,255,new byte[0]),Flat(255,0,new byte[0]),Flat(3,2,new byte[6])}){
    layer.RegionCount=61;Success(NavigationLayerRegions.Build(layer,0),0,0);Plane(layer,Filled(layer.CellCount,255));
   }
   try{NavigationLayerRegions.Build(null,0);throw new Exception("Expected null rejection");}catch(ArgumentNullException){assertions++;}
  });
 }

 // climb MUST be explicit. The chosen value 4 is a reproducible experiment
 // using the separately evidenced default .8f/.2f configuration domain and
 // 2658D0 truncation. It is NOT an observed loaded game's runtime value:
 // intermediate initialization/writers remain outside this component's proof.
 public static void RunRealSample(int climb){
  Test("Nine pinned saved layers produce region data for explicit climb="+climb,delegate{
   const string path=@"C:\Users\zhuzi\Documents\Grim-Dynasty\docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin";
   const string memberHash="a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289";
   var info=new FileInfo(path);Check(info.Exists&&info.Length==1091357,"Pinned saved member extent");Check((info.Attributes&FileAttributes.ReparsePoint)==0,"Pinned member is not reparse file");
   byte[] bytes;
   using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)){
    Equal(1091357L,stream.Length,"Opened pinned member length");bytes=new byte[1091357];int n=0;
    while(n<bytes.Length){int read=stream.Read(bytes,n,bytes.Length-n);if(read==0)throw new EndOfStreamException();n+=read;}
   }
   Equal(memberHash,Hash(bytes,0,bytes.Length),"Pinned whole saved member SHA256");Equal("0a0a38176e69847c02d65e90805da18c8b849fe1806e16bfc03bcebabc07ccee",Hash(bytes,422373,442996),"Pinned PTHD chunk SHA256");
   var parsed=NavigationPathingData.Parse(bytes,422373,442996);int[] expectedGroups={0,5,6,9,10,11,13,14,15};int ordinal=0;
   for(int g=0;g<parsed.Groups.Count;g++)foreach(var input in parsed.Groups[g].Layers){
    Check(ordinal<9,"No extra real layer");Equal(expectedGroups[ordinal],g,"Pinned nonempty group order");var layer=NavigationLayerData.Decode(input);Equal((byte)128,layer.Width,"Real layer width");Equal((byte)128,layer.Height,"Real layer height");
    var prefix=Hash(layer.WorkData.Array,0,56+3*layer.CellCount);var result=NavigationLayerRegions.Build(layer,climb);
    Console.WriteLine("REAL regions layer="+ordinal+" group="+g+" explicitClimb="+climb+" outcome="+result.Outcome+" status="+(result.NativeStatus.HasValue?result.NativeStatus.Value.ToString("X8"):"none")+" raw="+result.RawRegionCount+" dense="+layer.RegionCount+" detail="+result.Detail+" cell="+result.CellIndex+" regionsSHA256="+Hash(layer.Plane(3)));
    Equal(NavigationLayerRegionOutcome.Success,result.Outcome,"Real saved input supported in this explicit climb domain");Equal((uint?)0x40000000u,result.NativeStatus,"Region stage success");Check(result.RegionCountWritten,"Final count published");Same(layer,result.Layer,"Result retains same layer");Same(layer.WorkData.Array,result.Regions.Array,"Regions share decoder's owner");
    Equal(prefix,Hash(layer.WorkData.Array,0,56+3*layer.CellCount),"Region stage preserves header/first three planes");
    var area=layer.Plane(1);var regions=layer.Plane(3);var seen=new bool[255];bool valid=true;
    for(int i=0;i<layer.CellCount;i++){
     byte id=regions.Array[regions.Offset+i];
     if(area.Array[area.Offset+i]==0){if(id!=255)valid=false;}
     else if(id==255||id>=layer.RegionCount)valid=false;
     else seen[id]=true;
    }
    Check(valid,"Every active cell has an in-range region; blocked cells stay FF");int seenCount=0;for(int i=0;i<seen.Length;i++)if(seen[i])seenCount++;Equal((int)layer.RegionCount,seenCount,"Dense output contains every published region ID");ordinal++;
   }
   Equal(9,ordinal,"All nine saved layers processed");Equal(memberHash,Hash(bytes,0,bytes.Length),"Original saved member bytes unchanged");
  });
 }
}

public static class NavigationLayerRegionsTestProgram {
 public static int Main(string[] args){
  try{
   if(args.Length!=0&&(args.Length!=2||args[0]!="--real-climb"))throw new ArgumentException("Use no arguments or --real-climb <signed integer>");
   int climb=0;if(args.Length==2)climb=Int32.Parse(args[1],System.Globalization.CultureInfo.InvariantCulture);
   NavigationLayerRegionsTests.RunMemory();if(args.Length==2)NavigationLayerRegionsTests.RunRealSample(climb);
   Console.WriteLine("PASS "+NavigationLayerRegionsTests.Scenarios+" scenarios, "+NavigationLayerRegionsTests.Assertions+" assertions; managed 268CF0 region component; no captured native golden, contours, mesh publication or game compatibility");return 0;
  }catch(Exception e){Console.Error.WriteLine("FAIL "+e);return 1;}
 }
}
