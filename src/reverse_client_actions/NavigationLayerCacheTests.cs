using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace GrimDynasty.ReverseClient.Actions {
 // Main performs only memory fixtures. The optional saved-sample entry accepts
 // bytes from its caller and performs no filesystem, native or network I/O.
 public static class NavigationLayerCacheTests {
  public static int Assertions {get;private set;}
  public static int Scenarios {get;private set;}
  static void Check(bool condition,string message){Assertions++;if(!condition)throw new Exception(message);}
  static void Eq(long expected,long actual,string message){Check(expected==actual,message+" expected "+expected+" actual "+actual);}
  static void Same(object expected,object actual,string message){Check(Object.ReferenceEquals(expected,actual),message);}
  static void Case(string name,Action action){action();Scenarios++;}
  static void Throws<T>(Action action,string message) where T:Exception {
   try{action();}catch(T){Assertions++;return;}throw new Exception(message+": expected "+typeof(T).Name);
  }
  static void Domain(string code,Action action){
   try{action();}catch(NavigationLayerCacheDomainException e){Check(e.Code==code,"domain "+code+" actual "+e.Code);return;}
   throw new Exception("Expected own domain rejection "+code);
  }
  static void Put(byte[] buffer,int at,uint value){buffer[at]=(byte)value;buffer[at+1]=(byte)(value>>8);buffer[at+2]=(byte)(value>>16);buffer[at+3]=(byte)(value>>24);}
  static void Put(NavigationPathingByteSlice slice,int at,uint value){Put(slice.Buffer,slice.Offset+at,value);}
  static uint Read(NavigationPathingByteSlice slice,int at){return (uint)slice[at]|((uint)slice[at+1]<<8)|((uint)slice[at+2]<<16)|((uint)slice[at+3]<<24);}
  static int Signed(NavigationPathingByteSlice slice,int at){return unchecked((int)Read(slice,at));}
  static byte[] Header(int x,int z,int layer,int size){
   byte[] b=new byte[size];Put(b,0,NavigationLayerCache.LayerMagic);Put(b,4,1);
   Put(b,8,unchecked((uint)x));Put(b,12,unchecked((uint)z));Put(b,16,unchecked((uint)layer));return b;
  }
  static byte[] Wrapped(byte[] payload){
   byte[] b=new byte[32+payload.Length];Put(b,0,NavigationPathingData.Magic);Put(b,4,1);
   Put(b,24,(uint)(payload.Length+4));Put(b,28,(uint)payload.Length);Array.Copy(payload,0,b,32,payload.Length);return b;
  }
  static NavigationPathingByteSlice Slice(byte[] payload){return NavigationPathingData.Parse(Wrapped(payload)).Groups[0].Layers[0].Payload;}
  static NavigationPathingByteSlice Layer(int x,int z,int layer){return Slice(Header(x,z,layer,60));}
  static uint[] Output(int size){uint[] result=new uint[size];for(int n=0;n<size;n++)result[n]=0xfeedbeefu;return result;}
  static void Untouched(uint[] result,int start){for(int n=start;n<result.Length;n++)Eq(0xfeedbeefu,result[n],"untouched output "+n);}
  static void BytesEqual(byte[] first,byte[] second){Eq(first.Length,second.Length,"byte extent");for(int n=0;n<first.Length;n++)Eq(first[n],second[n],"byte "+n);}

  public static void Run(){
   Assertions=0;Scenarios=0;
   Case("positive managed pool",delegate{
    var c=NavigationLayerCache.Create(8);Eq(8,c.Capacity,"capacity");Eq(2,c.BucketCount,"buckets");Eq(1,c.BucketMask,"mask");Eq(3,c.IndexBits,"index bits");Eq(29,c.SaltBits,"salt bits");Eq(0,c.Count,"count");
    var p=c.FreeHead;
    for(int n=0;n<8;n++){
     Same(c.GetSlot(n),p,"free identity");Eq(n,p.Index,"free order");Eq(1,p.Salt00,"initial salt");Same(null,p.Header08,"header zero");Same(null,p.Payload10,"payload zero");Same(null,p.Blob20,"blob zero");Eq(0,p.PayloadLength18,"payload length zero");Eq(0,p.BlobLength28,"blob length zero");Eq(0,p.Flags2C,"flags zero");p=p.Next30;
    }
    Same(null,p,"free terminator");
   });
   Case("capacity arithmetic",delegate{
    var a=NavigationLayerCache.Create(1);Eq(1,a.BucketCount,"one bucket");Eq(0,a.IndexBits,"one index bits");Eq(31,a.SaltBits,"salt capped at31");
    var b=NavigationLayerCache.Create(5);Eq(1,b.BucketCount,"floor before power2");Eq(3,b.IndexBits,"non power2 bits");
    var c=NavigationLayerCache.Create(4096);Eq(1024,c.BucketCount,"observed capacity buckets");Eq(1023,c.BucketMask,"observed mask");Eq(12,c.IndexBits,"observed index bits");Eq(20,c.SaltBits,"observed salt bits");Eq(4095,c.GetSlot(4095).Index,"last slot");
   });
   Case("own capacity and slot domain",delegate{
    Throws<ArgumentOutOfRangeException>(delegate{NavigationLayerCache.Create(0);},"zero");Throws<ArgumentOutOfRangeException>(delegate{NavigationLayerCache.Create(-1);},"negative");Throws<ArgumentOutOfRangeException>(delegate{NavigationLayerCache.Create(4097);},"own upper limit");
    var c=NavigationLayerCache.Create(1);Throws<ArgumentOutOfRangeException>(delegate{c.GetSlot(-1);},"negative index");Throws<ArgumentOutOfRangeException>(delegate{c.GetSlot(1);},"end index");
   });
   Case("reference null and typed identity",delegate{
    var c=NavigationLayerCache.Create(8);Eq(0,c.EncodeReference(null),"null reference");Eq(8,c.EncodeReference(c.GetSlot(0)),"free slot formula no occupied check");Eq(15,c.EncodeReference(c.GetSlot(7)),"last slot formula");
    Domain("ForeignSlot",delegate{c.EncodeReference(NavigationLayerCache.Create(8).GetSlot(0));});
   });
   Case("add retains borrowed views",delegate{
    var input=Layer(-4,7,2);input.Buffer[input.Offset+56]=0x53;var before=(byte[])input.Buffer.Clone();var c=NavigationLayerCache.Create(8);var r=new NavigationLayerReference(99);
    Eq(NavigationLayerCache.Success,c.Add(input,r),"add");Eq(8,r.Value,"encoded output");Eq(1,c.Count,"count");Same(c.GetSlot(1),c.FreeHead,"free advanced");
    var p=c.GetSlot(0);Same(input,p.Header08,"header identity");Same(input,p.Blob20,"blob identity");Same(input,p.Payload10.Source,"payload source identity");Same(input.Buffer,p.Payload10.Buffer,"payload backing identity");Eq(input.Offset+56,p.Payload10.BufferOffset,"payload address");Eq(4,p.PayloadLength18,"payload length");Eq(4,p.Payload10.Length,"view length");Eq(60,p.BlobLength28,"blob length");Eq(0,p.Flags2C,"literal flags zero");Eq(0x53,p.Payload10[0],"payload byte");Same(null,p.Next30,"empty bucket next");BytesEqual(before,input.Buffer);
   });
   Case("optional output and header-only layer",delegate{
    var c=NavigationLayerCache.Create(1);Eq(NavigationLayerCache.Success,c.Add(Slice(Header(0,0,0,56)),null),"null optional output");Eq(0,c.GetSlot(0).PayloadLength18,"zero payload length");Eq(1,c.Count,"registered");Same(null,c.FreeHead,"exhausted free");Throws<ArgumentOutOfRangeException>(delegate{byte unused=c.GetSlot(0).Payload10[0];},"empty payload index");
   });
   Case("magic and version failure order",delegate{
    var c=NavigationLayerCache.Create(2);var r=new NavigationLayerReference(123);byte[] magic=new byte[4];Put(magic,0,7);Eq(NavigationLayerCache.WrongMagic,c.Add(Slice(magic),r),"magic does not read absent version");
    byte[] version=new byte[8];Put(version,0,NavigationLayerCache.LayerMagic);Put(version,4,2);Eq(NavigationLayerCache.WrongVersion,c.Add(Slice(version),r),"version before short header");Eq(123,r.Value,"failure output");Eq(0,c.Count,"failure count");Same(c.GetSlot(0),c.FreeHead,"failure free");
   });
   Case("own extent rejects without native status",delegate{
    var c=NavigationLayerCache.Create(2);var r=new NavigationLayerReference(55);
    Domain("UnreadableWord",delegate{c.Add(Slice(new byte[3]),r);});byte[] first=new byte[4];Put(first,0,NavigationLayerCache.LayerMagic);Domain("UnreadableWord",delegate{c.Add(Slice(first),r);});
    byte[] shortHeader=new byte[8];Put(shortHeader,0,NavigationLayerCache.LayerMagic);Put(shortHeader,4,1);Domain("LayerHeaderExtent",delegate{c.Add(Slice(shortHeader),r);});
    Throws<ArgumentNullException>(delegate{c.Add(null,r);},"null source");Eq(55,r.Value,"own failure output");Eq(0,c.Count,"own failure count");Same(c.GetSlot(0),c.FreeHead,"own failure free");
   });
   Case("duplicate before exhausted pool",delegate{
    var c=NavigationLayerCache.Create(1);var first=Layer(1,2,3);Eq(NavigationLayerCache.Success,c.Add(first,null),"first");var r=new NavigationLayerReference(77);
    Eq(NavigationLayerCache.Duplicate,c.Add(Layer(1,2,3),r),"duplicate wins over pool full");Eq(77,r.Value,"duplicate output");Same(first,c.GetSlot(0).Header08,"original identity retained");
    Eq(NavigationLayerCache.PoolFull,c.Add(Layer(1,2,4),r),"unique full");Eq(77,r.Value,"full output");Eq(1,c.Count,"no new registration");Same(null,c.GetSlot(0).Next30,"no new chain");
   });
   Case("coordinate layers reverse insertion",delegate{
    var c=NavigationLayerCache.Create(8);for(int n=0;n<3;n++)Eq(NavigationLayerCache.Success,c.Add(Layer(10,-9,n),null),"layer add");
    var output=Output(35);Eq(3,c.Enumerate(10,-9,output),"matching layers");Eq(10,output[0],"newest");Eq(9,output[1],"middle");Eq(8,output[2],"oldest");Untouched(output,3);Same(c.GetSlot(1),c.GetSlot(2).Next30,"chain shared identity");
   });
   Case("hash collision filters coordinates",delegate{
    var c=NavigationLayerCache.Create(8);Eq(c.BucketIndex(0,0),c.BucketIndex(2,0),"fixture collision");c.Add(Layer(0,0,0),null);c.Add(Layer(2,0,0),null);var output=Output(32);Eq(1,c.Enumerate(0,0,output),"collision excluded");Eq(8,output[0],"matching older slot");Untouched(output,1);
    var empty=Output(32);Eq(0,c.Enumerate(4,0,empty),"same bucket no matching coords");Untouched(empty,0);
   });
   Case("wrapped signed hash",delegate{
    var c=NavigationLayerCache.Create(16);Eq(1,c.BucketIndex(-1,0),"negative x");Eq(3,c.BucketIndex(0,-1),"negative z");Eq(0,c.BucketIndex(Int32.MinValue,0),"wrapped minimum");Eq(0,c.BucketIndex(Int32.MinValue,Int32.MinValue),"two wrapped minima");Eq(0,c.BucketIndex(Int32.MaxValue,Int32.MaxValue),"two maxima difference");
   });
   Case("live coordinate edits same bucket",delegate{
    var c=NavigationLayerCache.Create(8);var input=Layer(0,0,0);c.Add(input,null);Put(input,8,2);var output=Output(32);Eq(1,c.Enumerate(2,0,output),"new coords live");Eq(8,output[0],"same identity");Eq(0,c.Enumerate(0,0,Output(32)),"old coords gone");Eq(1,c.Count,"same registration");
   });
   Case("header edits do not rehash",delegate{
    var c=NavigationLayerCache.Create(8);var input=Layer(0,0,0);c.Add(input,null);Put(input,8,1);Eq(0,c.Enumerate(0,0,Output(32)),"old bucket sees changed coords");Eq(0,c.Enumerate(1,0,Output(32)),"new bucket not populated");Same(input,c.GetSlot(0).Header08,"retains changed original");Eq(1,c.Count,"no move or new registration");
   });
   Case("live layer id in duplicate check",delegate{
    var c=NavigationLayerCache.Create(8);var input=Layer(0,0,0);c.Add(input,null);Put(input,16,3);Eq(NavigationLayerCache.Duplicate,c.Add(Layer(0,0,3),null),"new id duplicate");Eq(NavigationLayerCache.Success,c.Add(Layer(0,0,0),null),"old id now free");Eq(2,c.Enumerate(0,0,Output(32)),"both identities");
   });
   Case("registered magic is not revalidated",delegate{
    var c=NavigationLayerCache.Create(8);var input=Layer(3,4,0);c.Add(input,null);Put(input,0,0);Put(input,4,2);Eq(1,c.Enumerate(3,4,Output(32)),"enum only coords");Eq(NavigationLayerCache.Duplicate,c.Add(Layer(3,4,0),null),"existing header only key");Eq(NavigationLayerCache.WrongMagic,c.Add(input,null),"incoming magic still validated");
   });
   Case("same backing distinct views",delegate{
    var bytes=Wrapped(Header(3,4,0,60));var one=NavigationPathingData.Parse(bytes).Groups[0].Layers[0].Payload;var two=NavigationPathingData.Parse(bytes).Groups[0].Layers[0].Payload;Check(!Object.ReferenceEquals(one,two),"different slice objects");Same(one.Buffer,two.Buffer,"same byte storage");
    var c=NavigationLayerCache.Create(8);c.Add(one,null);Eq(NavigationLayerCache.Duplicate,c.Add(two,null),"shared bytes duplicate");Same(one,c.GetSlot(0).Header08,"first view identity kept");
   });
   Case("payload remains borrowed",delegate{
    var c=NavigationLayerCache.Create(8);var input=Layer(0,0,0);c.Add(input,null);input.Buffer[input.Offset+59]=0xac;Eq(0xac,c.GetSlot(0).Payload10[3],"later payload mutation visible");Eq(4,c.GetSlot(0).PayloadLength18,"stored size unchanged");Throws<ArgumentOutOfRangeException>(delegate{byte unused=c.GetSlot(0).Payload10[-1];},"negative payload index");Throws<ArgumentOutOfRangeException>(delegate{byte unused=c.GetSlot(0).Payload10[4];},"end payload index");
   });
   Case("fixed32 newest references",delegate{
    var c=NavigationLayerCache.Create(40);for(int n=0;n<40;n++)Eq(NavigationLayerCache.Success,c.Add(Layer(-3,6,n),null),"many add");var output=Output(36);Eq(32,c.Enumerate(-3,6,output),"native fixed cap");for(int n=0;n<32;n++)Eq(64u+(uint)(39-n),output[n],"bounded chain order");Untouched(output,32);Eq(40,c.Count,"cache not truncated");Same(c.GetSlot(0),c.GetSlot(1).Next30,"older chain retained");
   });
   Case("output extent is own precondition",delegate{
    var c=NavigationLayerCache.Create(8);c.Add(Layer(0,0,0),null);Domain("OutputExtent",delegate{c.Enumerate(0,0,new uint[31]);});Throws<ArgumentNullException>(delegate{c.Enumerate(0,0,null);},"null output");Eq(1,c.Count,"output error no mutation");Eq(1,c.Enumerate(0,0,Output(32)),"still enumerable");
   });
   Console.WriteLine("NavigationLayerCache: "+Scenarios+" scenarios, "+Assertions+" assertions passed (pure memory; no native cache construction)");
  }

  static string Digest(byte[] bytes,int offset,int length){
   using(var hash=SHA256.Create()){return BitConverter.ToString(hash.ComputeHash(bytes,offset,length)).Replace("-","").ToLowerInvariant();}
  }

  // A distinct actual-sample integration entry. Caller supplies the previously
  // extracted project artifact, not an original game pathname. No IO occurs.
  // This validates registration of the nine compressed layers, not decoding,
  // obstacle construction, navmesh topology or client compatibility.
  public static void RunSavedMainMenu(byte[] bytes){
   Case("saved mainmenu compressed-layer registration",delegate{
    if(bytes==null)throw new ArgumentNullException("bytes");Eq(1091357,bytes.Length,"saved member extent");
    const string memberHash="a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289";
    Check(Digest(bytes,0,bytes.Length)==memberHash,"saved member fingerprint");
    Check(Digest(bytes,422373,442996)=="0a0a38176e69847c02d65e90805da18c8b849fe1806e16bfc03bcebabc07ccee","PTHD fingerprint");
    var data=NavigationPathingData.Parse(bytes,422373,442996);Eq(16,data.Groups.Count,"observed group count");
    var layers=new List<NavigationPathingByteSlice>();foreach(var group in data.Groups)foreach(var layer in group.Layers)layers.Add(layer.Payload);Eq(9,layers.Count,"observed layer count");
    var cache=NavigationLayerCache.Create(4096);
    for(int n=0;n<layers.Count;n++){
     var input=layers[n];var output=new NavigationLayerReference(0xeeeeeeeeu);Eq(NavigationLayerCache.Success,cache.Add(input,output),"actual add "+n);Eq(4096u+(uint)n,output.Value,"actual reference "+n);
     var slot=cache.GetSlot(n);Same(input,slot.Header08,"actual header identity");Same(input,slot.Blob20,"actual blob identity");Same(bytes,slot.Payload10.Buffer,"actual member backing");Eq(input.Offset+56,slot.Payload10.BufferOffset,"actual payload offset");Eq(input.Length-56,slot.PayloadLength18,"actual payload length");Eq(128,input[0x30],"observed width byte");Eq(128,input[0x31],"observed height byte");
    }
    for(int n=0;n<layers.Count;n++){
     int x=Signed(layers[n],8),z=Signed(layers[n],12);var expected=new List<uint>();
     for(int j=layers.Count-1;j>=0;j--)if(Signed(layers[j],8)==x&&Signed(layers[j],12)==z)expected.Add(4096u+(uint)j);
     uint[] actual=Output(32);Eq(expected.Count,cache.Enumerate(x,z,actual),"actual matching coordinate layers");for(int j=0;j<expected.Count;j++)Eq(expected[j],actual[j],"actual reverse insertion");Untouched(actual,expected.Count);
    }
    var duplicateOutput=new NavigationLayerReference(0x12345678u);Eq(NavigationLayerCache.Duplicate,cache.Add(layers[0],duplicateOutput),"actual duplicate");Eq(0x12345678u,duplicateOutput.Value,"actual duplicate output untouched");Eq(9,cache.Count,"actual registration count");Same(cache.GetSlot(9),cache.FreeHead,"actual remaining pool");Check(Digest(bytes,0,bytes.Length)==memberHash,"saved member unchanged");
   });
   Console.WriteLine("NavigationLayerCache saved mainmenu passed; cumulative "+Scenarios+" scenarios, "+Assertions+" assertions");
  }
 }

 public static class NavigationLayerCacheTestProgram {
  public static int Main(){
   try{NavigationLayerCacheTests.Run();return 0;}catch(Exception error){Console.Error.WriteLine(error.ToString());return 1;}
  }
 }
}
