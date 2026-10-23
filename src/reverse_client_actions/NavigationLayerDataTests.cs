using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using GrimDynasty.ReverseClient.Actions;

// New decoder cases only. No original installation, game, backend, native
// allocator or mock layer callback is used. RealSample reads one pinned saved
// workspace member; it does not repeat the old 14 PTHD fixture scenarios.
public static class NavigationLayerDataTests {
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
 static void Zero(ArraySegment<byte> view,int first){bool zero=true;for(int n=first;n<view.Count;n++)if(view.Array[view.Offset+n]!=0){zero=false;break;}Check(zero,"Unwritten work tail remains zero");}
 static void EqualBytes(byte[] expected,int offset,ArraySegment<byte> view,int count){bool same=true;for(int n=0;n<count;n++)if(expected[offset+n]!=view.Array[view.Offset+n]){same=false;break;}Check(same,"Expected copied bytes");}
 static byte[] Header(byte width,byte height){
  var h=new byte[56];for(int n=0;n<h.Length;n++)h[n]=(byte)(n*3+7);
  Put(h,0,0x44544C52u);Put(h,4,1);h[48]=width;h[49]=height;return h;
 }
 static NavigationPathingLayer Wrap(byte[] raw){
  var bytes=new byte[24+4+4+raw.Length];Put(bytes,0,0x44485450u);Put(bytes,4,1);Put(bytes,24,(uint)(4+raw.Length));Put(bytes,28,(uint)raw.Length);Buffer.BlockCopy(raw,0,bytes,32,raw.Length);
  return NavigationPathingData.Parse(bytes).Groups[0].Layers[0];
 }
 static NavigationPathingLayer Input(byte width,byte height,params byte[] body){
  var raw=new byte[56+body.Length];Buffer.BlockCopy(Header(width,height),0,raw,0,56);Buffer.BlockCopy(body,0,raw,56,body.Length);return Wrap(raw);
 }
 static void Reject(string code,long offset,Action body){try{body();throw new Exception("Expected own rejection "+code);}catch(NavigationLayerFormatException e){Equal(code,e.Code,"Format rejection code");Equal(offset,e.SourceOffset,"Absolute source offset");}}
 static void Bounds(Action body){try{body();throw new Exception("Expected argument rejection");}catch(ArgumentOutOfRangeException){assertions++;}}

 public static void RunMemory(){
  scenarios=0;assertions=0;
  Test("Complete opaque header copied while short write leaves zero tail",delegate{
   var layer=Input(2,2,1,2,3);var data=NavigationLayerData.Decode(layer);
   Equal((byte)2,data.Width,"Width");Equal((byte)2,data.Height,"Height");Equal(4,data.CellCount,"Cell count");Equal(3,data.BytesWritten,"Native copy length");Equal(56,data.Header.Count,"Full header");Equal(16,data.WorkData.Count,"Four-plane capacity");Equal((byte)0,data.RegionCount,"Proven initial cleared region byte");
   EqualBytes(layer.Payload.Buffer,layer.Payload.Offset,data.Header,56);EqualBytes(layer.Payload.Buffer,layer.Payload.Offset+56,data.WorkData,3);Zero(data.WorkData,3);
   Same(layer.Payload,data.Source,"Borrowed source view retained");Check(!Object.ReferenceEquals(data.Source.Buffer,data.Header.Array),"Header is newly owned");Same(data.Header.Array,data.WorkData.Array,"Header/work share own owner");
  });
  Test("Actual slot copies compressed-looking bytes without LZ4 expansion",delegate{
   var data=NavigationLayerData.Decode(Input(2,2,0x14,0x61,1,0));
   Equal(4,data.BytesWritten,"109820 copies length verbatim");EqualBytes(new byte[]{0x14,0x61,1,0},0,data.WorkData,4);Zero(data.WorkData,4);
  });
  Test("Exact capacity creates four correctly placed shared views",delegate{
   var data=NavigationLayerData.Decode(Input(2,1,0,1,2,3,4,5,6,7));
   Equal(8,data.BytesWritten,"Exact copy capacity");
   for(int n=0;n<4;n++){var plane=data.Plane(n);Same(data.WorkData.Array,plane.Array,"Plane shares owner");Equal(2,plane.Count,"N bytes per plane");Equal(56+n*2,plane.Offset,"Own layout plane offset");Equal((byte)(n*2),plane.Array[plane.Offset],"Plane source order");}
   Bounds(delegate{data.Plane(-1);});Bounds(delegate{data.Plane(4);});
  });
  Test("Source and owned copies remain independent after either is changed",delegate{
   var input=Input(1,1,3,4,5);var data=NavigationLayerData.Decode(input);var second=NavigationLayerData.Decode(input);
   Check(!Object.ReferenceEquals(data.WorkData.Array,second.WorkData.Array),"Each decode owns a distinct allocation");
   input.Payload.Buffer[input.Payload.Offset+8]=99;input.Payload.Buffer[input.Payload.Offset+56]=88;
   Equal((byte)31,data.Header.Array[8],"Header snapshot independent of source");Equal((byte)3,data.WorkData.Array[data.WorkData.Offset],"Work snapshot independent of source");
   data.WorkData.Array[data.WorkData.Offset+1]=77;Equal((byte)4,input.Payload[57],"Output change does not write borrowed source");Equal((byte)4,second.WorkData.Array[second.WorkData.Offset+1],"Output change does not write second decode");
   Same(input.Payload.Buffer,data.Source.Buffer,"Retained source still shows original identity");Equal((byte)88,data.Source[56],"Borrowed source sees later input change");
  });
  Test("Zero width or height with empty copy is supported data",delegate{
   foreach(var input in new NavigationPathingLayer[]{Input(0,0),Input(0,255),Input(255,0)}){
    var data=NavigationLayerData.Decode(input);Equal(0,data.CellCount,"Zero-area count");Equal(0,data.BytesWritten,"Empty memcpy");Equal(0,data.WorkData.Count,"Zero work extent");Equal(56,data.Header.Array.Length,"Only represented header retained");
    for(int n=0;n<4;n++){Equal(0,data.Plane(n).Count,"Empty plane");Same(data.WorkData.Array,data.Plane(n).Array,"Empty views retain owner");}
   }
  });
  Test("Empty payload with nonzero dimensions preserves full zero work area",delegate{
   var data=NavigationLayerData.Decode(Input(3,2));Equal(6,data.CellCount,"Cell count");Equal(0,data.BytesWritten,"Actual zero write");Equal(24,data.WorkData.Count,"Allocated work area");Zero(data.WorkData,0);
  });
  Test("Byte dimension maxima and explicit working budget",delegate{
   var input=Input(255,255,0xAB);var data=NavigationLayerData.Decode(input.Payload,260100);
   Equal(65025,data.CellCount,"Unsigned byte multiplication");Equal(260100,data.WorkData.Count,"Maximum four-plane bytes");Equal(260156,data.Header.Array.Length,"Own layout allocation size");Equal((byte)0xAB,data.WorkData.Array[data.WorkData.Offset],"One copied byte");Zero(data.WorkData,1);
   Reject("WorkingBudget",input.Payload.Offset+48,delegate{NavigationLayerData.Decode(input.Payload,260099);});Bounds(delegate{NavigationLayerData.Decode(input.Payload,-1);});
  });
  Test("Over-capacity memcpy is an own rejection before allocation publication",delegate{
   var input=Input(1,1,1,2,3,4,5);string before=Hash(input.Payload.Buffer,0,input.Payload.Buffer.Length);
   Reject("Capacity",input.Payload.Offset+56,delegate{NavigationLayerData.Decode(input);});Equal(before,Hash(input.Payload.Buffer,0,input.Payload.Buffer.Length),"Rejected input unchanged");
   var zero=Input(0,2,1);Reject("Capacity",zero.Payload.Offset+56,delegate{NavigationLayerData.Decode(zero);});
  });
  Test("Short headers are own extent failures rather than native status codes",delegate{
   foreach(int length in new int[]{0,1,4,8,48,49,55}){var input=Wrap(new byte[length]);Reject("ShortHeader",input.Payload.Offset,delegate{NavigationLayerData.Decode(input);});}
  });
  Test("Bad magic precedes version and both preserve input",delegate{
   var raw=Header(1,1);Put(raw,0,0);Put(raw,4,2);var input=Wrap(raw);Reject("Magic",input.Payload.Offset,delegate{NavigationLayerData.Decode(input);});
   Put(input.Payload.Buffer,input.Payload.Offset,0x44544C52u);Reject("Version",input.Payload.Offset+4,delegate{NavigationLayerData.Decode(input);});
   Put(input.Payload.Buffer,input.Payload.Offset+4,UInt32.MaxValue);Reject("Version",input.Payload.Offset+4,delegate{NavigationLayerData.Decode(input);});
  });
  Test("Null adapters reject without inventing an output object",delegate{
   try{NavigationLayerData.Decode((NavigationPathingLayer)null);throw new Exception("Expected null layer rejection");}catch(ArgumentNullException){assertions++;}
   try{NavigationLayerData.Decode((NavigationPathingByteSlice)null);throw new Exception("Expected null source rejection");}catch(ArgumentNullException){assertions++;}
  });
  Test("Opaque float and padding bits remain uninterpreted",delegate{
   var raw=Header(1,1);Put(raw,20,0x7FC12345u);Put(raw,24,0x80000000u);raw[54]=0xDE;raw[55]=0xAD;var input=Wrap(raw);var data=NavigationLayerData.Decode(input);
   EqualBytes(raw,0,data.Header,56);Equal((byte)0xDE,data.Header.Array[54],"Padding byte 54");Equal((byte)0xAD,data.Header.Array[55],"Padding byte 55");Zero(data.WorkData,0);
  });
 }

 // Only this saved workspace artifact is read, by explicit Main execution.
 // No native DLL is loaded. Expected positions/source hashes were observed by
 // r1-mainmenu-pathing-data-probe; decoded hashes are reported, not fabricated
 // golden native outputs. This is real-data compatibility for the copy adapter.
 public static void RunRealSample(){
  Test("Pinned real mainmenu PTHD feeds all nine layer copies",delegate{
   const string path=@"C:\Users\zhuzi\Documents\Grim-Dynasty\docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin";
   const string memberHash="a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289";
   var info=new FileInfo(path);Check(info.Exists&&info.Length==1091357,"Pinned saved member extent");Check((info.Attributes&FileAttributes.ReparsePoint)==0,"Saved member is not a reparse file");
   byte[] bytes;
   using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)){
    Equal(1091357L,stream.Length,"Opened fixed saved member size");bytes=new byte[1091357];int read=0;
    while(read<bytes.Length){int n=stream.Read(bytes,read,bytes.Length-read);if(n==0)throw new EndOfStreamException();read+=n;}
   }
   Equal(memberHash,Hash(bytes,0,bytes.Length),"Pinned saved member SHA256");Equal("0a0a38176e69847c02d65e90805da18c8b849fe1806e16bfc03bcebabc07ccee",Hash(bytes,422373,442996),"Proven chunk SHA256");
   var parsed=NavigationPathingData.Parse(bytes,422373,442996);
   int[] expectedGroups={0,5,6,9,10,11,13,14,15};int[] expectedOffsets={422405,471637,520853,570077,619293,668509,717729,766945,816161};
   string[] sourceHashes={
    "3fcc91360c4427ccd1cb2205efc532a2465e796b6ca877456115ca2bcd880804","198fb0a05eb182644fe357cc0c6fe77bf42e315afeb0e675ad4eabfff4387639","cd235afd515fa66af54d0898ec828e350d2769d6d509da8dc987c4f746d6a8c0",
    "c24b3489af8e296c73dbaf10a342c164e9d296c3eaf51ba07da30a06cce67b74","f614df3e8c8025a70fdd1341a173a0e625481dde36bab187674984427fec31bc","5ca5fa026b2f544175f52e8dfd923df059ffa96daed40dc575315743eef034e8",
    "0350ff2e44df0344e8f5c9142eb043e80e3ba4bf0c38282ee889ec8ed5d95f4c","f5b04c00a7c9997d25b42e06514470f8ef51d1ad150b25fc59ec681db86b5e9c","a4af31a9e83857e62e0133397e7b4e32c634c35bb4b17040b84581b30cde44bb"};
   int ordinal=0;byte[] previousOwned=null;
   for(int g=0;g<parsed.Groups.Count;g++)foreach(var layer in parsed.Groups[g].Layers){
    Check(ordinal<9,"No unexpected real layer");Equal(expectedGroups[ordinal],g,"Pinned group order");Equal(expectedOffsets[ordinal],layer.Payload.Offset,"Pinned layer range");Equal(49208,layer.Payload.Length,"Pinned layer size");Equal(sourceHashes[ordinal],Hash(bytes,layer.Payload.Offset,layer.Payload.Length),"Pinned layer source SHA256");
    var data=NavigationLayerData.Decode(layer);Same(bytes,data.Source.Buffer,"Real source borrowed from whole member");Check(!Object.ReferenceEquals(bytes,data.WorkData.Array),"Real work newly owned");Check(!Object.ReferenceEquals(previousOwned,data.WorkData.Array),"Distinct layer ownership");previousOwned=data.WorkData.Array;
    Equal((byte)128,data.Width,"Observed real header width");Equal((byte)128,data.Height,"Observed real header height");Equal(49152,data.BytesWritten,"Observed real copy length");Equal(65536,data.WorkData.Count,"Real four-plane capacity");Equal((byte)0,data.RegionCount,"Real decoded region count starts cleared");EqualBytes(bytes,layer.Payload.Offset,data.Header,56);EqualBytes(bytes,layer.Payload.Offset+56,data.WorkData,data.BytesWritten);Zero(data.WorkData,data.BytesWritten);
    for(int p=0;p<4;p++){Same(data.WorkData.Array,data.Plane(p).Array,"Real plane owner");Equal(data.CellCount,data.Plane(p).Count,"Real plane extent");}
    Console.WriteLine("REAL layer="+ordinal+" group="+g+" sourceOffset="+layer.Payload.Offset+" width="+data.Width+" height="+data.Height+" copied="+data.BytesWritten+" capacity="+data.WorkData.Count+" workSHA256="+Hash(data.WorkData));ordinal++;
   }
   Equal(9,ordinal,"All nine real layers decoded");Equal(memberHash,Hash(bytes,0,bytes.Length),"All copies preserve source member bytes");
  });
 }
}

public static class NavigationLayerDataTestProgram {
 public static int Main(){
  try{NavigationLayerDataTests.RunMemory();NavigationLayerDataTests.RunRealSample();Console.WriteLine("PASS "+NavigationLayerDataTests.Scenarios+" scenarios, "+NavigationLayerDataTests.Assertions+" assertions; 12 new memory scenarios plus nine real saved layers; no native object, tile cache or mesh publication");return 0;}
  catch(Exception e){Console.Error.WriteLine("FAIL "+e);return 1;}
 }
}
