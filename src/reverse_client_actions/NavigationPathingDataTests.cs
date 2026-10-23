using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Pure-memory format cases, not original MAP samples or a native layer producer.
// Console output only; no file/network I/O, process launch or mesh publication.
public static class NavigationPathingDataTests {
 static int scenarios,assertions;
 public static int Scenarios {get{return scenarios;}}
 public static int Assertions {get{return assertions;}}
 static void Check(bool ok,string why){assertions++;if(!ok)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Test(string name,Action body){body();scenarios++;Console.WriteLine("PASS "+name);}
 static void Put(byte[] bytes,int offset,uint value){for(int n=0;n<4;n++)bytes[offset+n]=(byte)(value>>(8*n));}
 static byte[] Header(int minX,int minZ,int maxX,int maxZ){
  var bytes=new byte[24];Put(bytes,0,0x44485450u);Put(bytes,4,1);Put(bytes,8,unchecked((uint)minX));Put(bytes,12,unchecked((uint)minZ));Put(bytes,16,unchecked((uint)maxX));Put(bytes,20,unchecked((uint)maxZ));return bytes;
 }
 static byte[] Join(params byte[][] pieces){int length=0;foreach(var piece in pieces)length+=piece.Length;var result=new byte[length];int at=0;foreach(var piece in pieces){Array.Copy(piece,0,result,at,piece.Length);at+=piece.Length;}return result;}
 static byte[] Prefix(uint value){var bytes=new byte[4];Put(bytes,0,value);return bytes;}
 static byte[] Layer(params byte[] payload){return Join(Prefix((uint)payload.Length),payload);}
 static byte[] Group(params byte[][] layers){var bytes=Join(layers);return Join(Prefix((uint)bytes.Length),bytes);}
 static byte[] Data(params byte[][] groups){return Join(Header(-2,-3,4,5),Join(groups));}
 static void Reject(string code,long offset,Action body){
  try{body();throw new Exception("Expected own format rejection "+code);}
  catch(NavigationPathingFormatException error){Equal(code,error.Code,"Format code");Equal(offset,error.SourceOffset,"Absolute source offset");}
 }
 static void Bounds(Action body){try{body();throw new Exception("Expected borrowed-slice bounds rejection");}catch(ArgumentOutOfRangeException){assertions++;}}
 static void Bytes(byte[] expected,NavigationPathingByteSlice actual){Equal(expected.Length,actual.Length,"Slice length");for(int n=0;n<expected.Length;n++)Equal(expected[n],actual[n],"Slice byte "+n);}

 public static void Run(){
  scenarios=0;assertions=0;
  Test("Header only retains signed coordinates without inventing a mesh",delegate{
   var bytes=Header(Int32.MinValue,Int32.MaxValue,-1,0);var data=NavigationPathingData.Parse(bytes);
   Equal(Int32.MinValue,data.MinimumGridX,"signed min X");Equal(Int32.MaxValue,data.MinimumGridZ,"signed min Z");Equal(-1,data.MaximumGridX,"signed max X");Equal(0,data.MaximumGridZ,"signed max Z");
   Equal(0,data.Groups.Count,"No inferred groups for any grid range");Same(bytes,data.Source.Buffer,"Original input identity");Equal(0,data.Source.Offset,"Whole source offset");Equal(24,data.Source.Length,"Header extent");
  });
  Test("Nested order and nonaligned payloads preserve exact boundaries",delegate{
   var bytes=Data(Group(Layer(0xA1,0xB2,0xC3),Layer(7)),Group(Layer(8,9)));var data=NavigationPathingData.Parse(bytes);
   Equal(2,data.Groups.Count,"Group order count");var first=data.Groups[0];var second=data.Groups[1];
   Equal(12u,first.DeclaredLength,"Group counts layer prefixes and bytes");Equal(24,first.LengthPrefixOffset,"First group prefix");Equal(28,first.EncodedLayers.Offset,"First group body");Equal(2,first.Layers.Count,"Two layers");
   Equal(28,first.Layers[0].LengthPrefixOffset,"First layer prefix");Equal(32,first.Layers[0].Payload.Offset,"First payload");Equal(3u,first.Layers[0].DeclaredLength,"Payload count excludes prefix");Bytes(new byte[]{0xA1,0xB2,0xC3},first.Layers[0].Payload);
   Equal(35,first.Layers[1].LengthPrefixOffset,"Unaligned second prefix has no padding");Bytes(new byte[]{7},first.Layers[1].Payload);
   Equal(40,second.LengthPrefixOffset,"Second group immediately follows");Equal(6u,second.DeclaredLength,"Second group size");Bytes(new byte[]{8,9},second.Layers[0].Payload);
   Same(bytes,first.EncodedLayers.Buffer,"Borrowed group identity");Same(bytes,second.Layers[0].Payload.Buffer,"Borrowed layer identity");
  });
  Test("Repeated zero groups and zero layers are distinct ordered records",delegate{
   var data=NavigationPathingData.Parse(Data(Group(),Group(),Group(Layer(),Layer()),Group()));
   Equal(4,data.Groups.Count,"Zero groups retained");Equal(0,data.Groups[0].Layers.Count,"Zero group has no layer");Equal(0u,data.Groups[1].DeclaredLength,"Repeated zero group length");Equal(28,data.Groups[1].LengthPrefixOffset,"Zero group consumes prefix");
   Equal(2,data.Groups[2].Layers.Count,"Zero layer records retained");Equal(40,data.Groups[2].Layers[0].Payload.Offset,"First empty payload address");Equal(44,data.Groups[2].Layers[1].Payload.Offset,"Second empty payload address");Equal(0,data.Groups[2].Layers[0].Payload.Length,"Empty layer length");Equal(44,data.Groups[3].LengthPrefixOffset,"Continued outer sequence");
   Check(!Object.ReferenceEquals(data.Groups[0],data.Groups[1]),"Distinct zero group records");Check(!Object.ReferenceEquals(data.Groups[2].Layers[0],data.Groups[2].Layers[1]),"Distinct zero layer records");
  });
  Test("Selected extent and borrowed payload retain original buffer identity",delegate{
   var encoded=Data(Group(Layer(10,11)));var bytes=Join(new byte[]{0x99,0x98,0x97},encoded,new byte[]{0xEE,0xEF});var before=(byte[])bytes.Clone();
   var data=NavigationPathingData.Parse(bytes,3,encoded.Length);var payload=data.Groups[0].Layers[0].Payload;
   Same(bytes,data.Source.Buffer,"Subrange does not copy original buffer");Same(bytes,payload.Buffer,"Payload shares original buffer");Equal(3,data.Source.Offset,"Subrange absolute source offset");Equal(encoded.Length,data.Source.Length,"Subrange excludes surrounding bytes");Equal(35,payload.Offset,"Absolute payload offset includes base");
   Equal(before.Length,bytes.Length,"Parser did not resize input");for(int n=0;n<bytes.Length;n++)Equal(before[n],bytes[n],"Parser did not write source "+n);
   bytes[payload.Offset]=12;Equal((byte)12,payload[0],"Later payload mutation visible through borrowed view");Put(bytes,3+8,77);Equal(-2,data.MinimumGridX,"Header metadata is explicitly a parse-time snapshot");
  });
  Test("Extent checks reject negative and overflowing requests before reads",delegate{
   var bytes=Header(0,0,0,0);
   Reject("InvalidExtent",-1,delegate{NavigationPathingData.Parse(bytes,-1,24);});Reject("InvalidExtent",0,delegate{NavigationPathingData.Parse(bytes,0,-1);});
   Reject("InvalidExtent",Int32.MaxValue,delegate{NavigationPathingData.Parse(bytes,Int32.MaxValue,Int32.MaxValue);});Reject("InvalidExtent",1,delegate{NavigationPathingData.Parse(bytes,1,Int32.MaxValue);});Reject("InvalidExtent",25,delegate{NavigationPathingData.Parse(bytes,25,0);});
   try{NavigationPathingData.Parse(null);throw new Exception("Expected null input rejection");}catch(ArgumentNullException){assertions++;}
  });
  Test("Short header precedes magic and rejects partial fixed fields",delegate{
   foreach(int length in new int[]{0,1,4,8,23})Reject("ShortHeader",0,delegate{NavigationPathingData.Parse(new byte[length]);});
   Reject("ShortHeader",24,delegate{NavigationPathingData.Parse(Header(0,0,0,0),24,0);});
  });
  Test("Magic and version are checked in observed order",delegate{
   var bytes=Header(0,0,0,0);Put(bytes,0,0);Put(bytes,4,9);Reject("Magic",0,delegate{NavigationPathingData.Parse(bytes);});
   Put(bytes,0,0x44485450u);Reject("Version",4,delegate{NavigationPathingData.Parse(bytes);});Put(bytes,4,0);Reject("Version",4,delegate{NavigationPathingData.Parse(bytes);});
   bytes=Join(new byte[2],bytes);Reject("Version",6,delegate{NavigationPathingData.Parse(bytes,2,24);});
  });
  Test("Trailing partial group length never becomes ignored padding",delegate{
   foreach(int length in new int[]{1,2,3}){var bytes=Join(Data(Group()),new byte[length]);Reject("TruncatedGroupLength",28,delegate{NavigationPathingData.Parse(bytes);});}
  });
  Test("Outer length is unsigned and cannot wrap or overrun input extent",delegate{
   foreach(uint length in new uint[]{1u,0x7FFFFFFFu,0x80000000u,UInt32.MaxValue}){var bytes=Join(Header(0,0,0,0),Prefix(length));Reject("GroupExtent",24,delegate{NavigationPathingData.Parse(bytes);});}
   var selected=Data(Group(Layer(1,2)));Reject("GroupExtent",24,delegate{NavigationPathingData.Parse(selected,0,selected.Length-1);});
  });
  Test("Incomplete inner length is rejected within its group",delegate{
   foreach(uint length in new uint[]{1u,2u,3u}){var bytes=Join(Header(0,0,0,0),Prefix(length),new byte[(int)length]);Reject("TruncatedLayerLength",28,delegate{NavigationPathingData.Parse(bytes);});}
   var residual=Join(Header(0,0,0,0),Prefix(5),Layer(),new byte[]{0x55});Reject("TruncatedLayerLength",32,delegate{NavigationPathingData.Parse(residual);});
  });
  Test("Inner unsigned length cannot consume the following group",delegate{
   foreach(uint length in new uint[]{1u,0x7FFFFFFFu,0x80000000u,UInt32.MaxValue}){var bytes=Join(Header(0,0,0,0),Prefix(4),Prefix(length),Group());Reject("LayerExtent",28,delegate{NavigationPathingData.Parse(bytes);});}
   var tooLong=Join(Header(0,0,0,0),Prefix(6),Prefix(3),new byte[]{1,2});Reject("LayerExtent",28,delegate{NavigationPathingData.Parse(tooLong);});
  });
  Test("Valid prefix does not hide malformed later group or layer",delegate{
   var badGroup=Join(Data(Group(Layer(1))),Prefix(9));Reject("GroupExtent",33,delegate{NavigationPathingData.Parse(badGroup);});
   var badLayer=Join(Header(0,0,0,0),Prefix(9),Layer(1),Prefix(1));Reject("LayerExtent",33,delegate{NavigationPathingData.Parse(badLayer);});
  });
  Test("Grid values remain raw even when ranges are reversed",delegate{
   var bytes=Join(Header(9,7,-8,-6),Group(),Group(Layer()));var data=NavigationPathingData.Parse(bytes);
   Equal(9,data.MinimumGridX,"Raw start X");Equal(7,data.MinimumGridZ,"Raw start Z");Equal(-8,data.MaximumGridX,"Raw end X");Equal(-6,data.MaximumGridZ,"Raw end Z");Equal(2,data.Groups.Count,"No fabricated grid-count rule");
  });
  Test("Exposed record order is read-only and byte access stays within slice",delegate{
   var data=NavigationPathingData.Parse(Data(Group(Layer(1))));
   try{((IList<NavigationPathingGroup>)data.Groups).Clear();throw new Exception("Expected read-only group collection");}catch(NotSupportedException){assertions++;}
   try{((IList<NavigationPathingLayer>)data.Groups[0].Layers).Clear();throw new Exception("Expected read-only layer collection");}catch(NotSupportedException){assertions++;}
   var slice=data.Groups[0].Layers[0].Payload;Bounds(delegate{byte value=slice[-1];});Bounds(delegate{byte value=slice[1];});
   var empty=NavigationPathingData.Parse(Data(Group(Layer()))).Groups[0].Layers[0].Payload;Bounds(delegate{byte value=empty[0];});
  });
 }
}

public static class NavigationPathingDataTestProgram {
 public static int Main(){
  try{NavigationPathingDataTests.Run();Console.WriteLine("PASS "+NavigationPathingDataTests.Scenarios+" scenarios, "+NavigationPathingDataTests.Assertions+" assertions; strict PTHD framing only, no native layer construction or mesh publication");return 0;}
  catch(Exception error){Console.Error.WriteLine("FAIL "+error);return 1;}
 }
}
