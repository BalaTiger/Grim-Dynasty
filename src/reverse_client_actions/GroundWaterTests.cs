using System;
using System.IO;
using System.Security.Cryptography;
using GrimDynasty.ReverseClient.Actions;

public static class GroundWaterTestProgram {
 const string SavedPath=@"C:\Users\zhuzi\Documents\Grim-Dynasty\docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin";
 const string SavedHash="a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289";
 static int assertions,scenarios;
 static void Check(bool value,string message){assertions++;if(!value)throw new Exception(message);}
 static void Scenario(Action action){action();scenarios++;}
 static uint Bits(float f){return BitConverter.ToUInt32(BitConverter.GetBytes(f),0);}
 static float Float(uint b){return BitConverter.ToSingle(BitConverter.GetBytes(b),0);}
 static string Hash(byte[] b){using(var s=SHA256.Create())return BitConverter.ToString(s.ComputeHash(b)).Replace("-","").ToLowerInvariant();}
 static void Missing(Action action,uint at){assertions++;try{action();}catch(MissingContract e){if(e.Address!=at)throw new Exception("Wrong Unsupported address "+e.Address.ToString("x"));return;}throw new Exception("Expected Unsupported "+at.ToString("x"));}
 static GroundWaterState OwnPrerequisites(){
  var w=new GroundWaterState();
  // Explicit OWN opaque nonnull identities, never a claim that native
  // Ctor1F6A90/Initialize1F6E80 was run or that either native pointee is valid.
  w.Prerequisite10B0.Write(new object());w.Prerequisite10B8.Write(new object());return w;
 }
 static GroundWaterChunkReader Reader(byte[] b){return new GroundWaterChunkReader(b,SavedHash+":422341+24");}
 static GroundTriangleOperands Operands(){var o=new GroundTriangleOperands();o.SelectObservedDiskOperands();return o;}
 static Displacement3 Point(float x,float y,float z){var p=new Displacement3();p.X0.Write(x);p.Y4.Write(y);p.Z8.Write(z);return p;}
 static GroundBlockHit20 Output(){return new GroundBlockHit20(Field<float>.Of(99),Point(71,72,73),Field<uint>.Of(987));}
 static void CheckInitialized(GroundBlockHit20 o){Check(Bits(o.Distance00.Read(0))==0x7f800000u,"Distance is exact +Inf");Check(Bits(o.Position04.X0.Read(0))==0&&Bits(o.Position04.Y4.Read(0))==0&&Bits(o.Position04.Z8.Read(0))==0,"Position written exact positive zeros");}
 static byte[] ReplaceWord(byte[] source,int word,uint value){var b=(byte[])source.Clone();Buffer.BlockCopy(BitConverter.GetBytes(value),0,b,word*4,4);return b;}
 static bool Load(GroundWaterState w,GroundWaterChunkReader r){return GroundWaterContracts.Load1F81E0(w,r,new Invocation());}
 public static int Main(string[] args){try{
  if(args.Length!=1||!String.Equals(args[0],SavedPath,StringComparison.OrdinalIgnoreCase))throw new Exception("Fixed saved member argument required");
  byte[] member=File.ReadAllBytes(SavedPath);Check(member.Length==1091357&&Hash(member)==SavedHash,"Saved member fingerprint");
  byte[] realChunk=new byte[24];Buffer.BlockCopy(member,422341,realChunk,0,24);
  GroundWaterState real=null;GroundRay24 realRay=null;
  Scenario(delegate{
   uint[] expected={0xfeedcba9u,2u,0u,0u,0u,0u};for(int n=0;n<6;n++)Check(BitConverter.ToUInt32(realChunk,n*4)==expected[n],"Actual chunk9 DWORD "+n);
   byte[] groundChunk=new byte[149073];Buffer.BlockCopy(member,273260,groundChunk,0,groundChunk.Length);
   var ground=OwnGroundColdLoader.DecodeType2Chunk(groundChunk,SavedHash+":273260+149073");
   float y=GroundGeometryContracts.Sample1DE880(ground,0,0,new Invocation());
   realRay=new GroundRay24(Point(0,(float)(y+2.5f),0),Point(0,-1,0));
   real=OwnPrerequisites();object p0=real.Prerequisite10B0.Read(0),p1=real.Prerequisite10B8.Read(0);var r=Reader(realChunk);
   Check(Load(real,r)&&r.Cursor08.Read(0)==24,"Actual24B version2 empty load returns true and consumes24");
   Check(real.LayerCountA0.Read(0)==0&&real.PathCountA4.Read(0)==0&&real.WaveCountA8.Read(0)==0,"All counts produced from real bytes");
   Check(Object.ReferenceEquals(p0,real.Prerequisite10B0.Read(0))&&Object.ReferenceEquals(p1,real.Prerequisite10B8.Read(0)),"Same explicit OWN prerequisites retained");
   var o=Output();Check(!GroundWaterContracts.GetIntersection1F9BD0(real,realRay,o,7,float.PositiveInfinity,Operands(),new Invocation()),"Real loaded empty Water returns false for actual floor surface7");CheckInitialized(o);Check(o.Material10.Read(0)==987,"Material is not initialized by empty water");
  });
  Scenario(delegate{var w=new GroundWaterState();var r=Reader(realChunk);Missing(delegate{Load(w,r);},0x1f8212);Check(r.Cursor08.Read(0)==0,"Unknown10B0 stops before reader");Missing(delegate{w.LayerCountA0.Read(123);},123);});
  Scenario(delegate{var w=new GroundWaterState();w.Prerequisite10B0.Write(null);Check(!Load(w,null),"Known null10B0 returns false without reader/10B8");Missing(delegate{w.Prerequisite10B8.Read(1);},1);});
  Scenario(delegate{var w=new GroundWaterState();w.Prerequisite10B0.Write(new object());var r=Reader(realChunk);Missing(delegate{Load(w,r);},0x1f8220);Check(r.Cursor08.Read(0)==0,"Unknown10B8 before reader");w.Prerequisite10B8.Write(null);Check(!Load(w,r)&&r.Cursor08.Read(0)==0,"Null10B8 native false, no cursor consumption");});
  Scenario(delegate{var o=Output();o.Material10.Forget();real.WaveCountA8.Forget();Check(!GroundWaterContracts.GetIntersection1F9BD0(real,realRay,o,7,float.NaN,Operands(),new Invocation()),"Empty intersection does not consume max-distance or wave count");CheckInitialized(o);Missing(delegate{o.Material10.Read(77);},77);Missing(delegate{real.WaveCountA8.Read(78);},78);});
  Scenario(delegate{var o=Output();Check(!GroundWaterContracts.GetIntersection1F9BD0(null,null,o,0,float.NaN,Operands(),new Invocation()),"Surface0 initializes output then skips state and Ray");CheckInitialized(o);Check(o.Material10.Read(0)==987,"Surface0 retains material");});
  Scenario(delegate{var w=new GroundWaterState();var o=Output();Missing(delegate{GroundWaterContracts.GetIntersection1F9BD0(w,realRay,o,7,1,Operands(),new Invocation());},0x1f9c2c);CheckInitialized(o);Check(o.Material10.Read(0)==987,"Unknown liveA0 preserves init prefix/material");});
  Scenario(delegate{var w=new GroundWaterState();w.LayerCountA0.Write(0);var o=Output();Missing(delegate{GroundWaterContracts.GetIntersection1F9BD0(w,realRay,o,7,1,Operands(),new Invocation());},0x1f9cd1);CheckInitialized(o);Check(o.Material10.Read(0)==987,"Unknown liveA4 preserves prefix");});
  Scenario(delegate{var w=new GroundWaterState();w.LayerCountA0.Write(1);var o=Output();Missing(delegate{GroundWaterContracts.GetIntersection1F9BD0(w,realRay,o,7,1,Operands(),new Invocation());},0x1f9c42);CheckInitialized(o);Check(o.Material10.Read(0)==987,"Nonempty layer is Unsupported, not miss");});
  Scenario(delegate{var w=new GroundWaterState();w.LayerCountA0.Write(0);w.PathCountA4.Write(1);var o=Output();Missing(delegate{GroundWaterContracts.GetIntersection1F9BD0(w,realRay,o,7,1,Operands(),new Invocation());},0x1f9ce2);CheckInitialized(o);Check(o.Material10.Read(0)==987,"Nonempty path is Unsupported, not miss");});
  Scenario(delegate{var w=OwnPrerequisites();var r=Reader(ReplaceWord(realChunk,0,0));Missing(delegate{Load(w,r);},0x1f8246);Check(r.Cursor08.Read(0)==4,"Invalid magic advances firstDWORD then stops before unresolved logger");Missing(delegate{w.LayerCountA0.Read(1);},1);});
  Scenario(delegate{var w=OwnPrerequisites();var r=Reader(ReplaceWord(realChunk,1,1));Missing(delegate{Load(w,r);},0x1f8279);Check(r.Cursor08.Read(0)==8,"Version warning consumes only magic/version");Missing(delegate{w.LayerCountA0.Read(1);},1);});
  Scenario(delegate{
   uint[] points={0x1f8232,0x1f8264,0x1f82a1,0x1f82b1,0x1f82c6,0x1f82e0};
   for(int n=0;n<6;n++){byte[] shortChunk=new byte[n*4];Buffer.BlockCopy(realChunk,0,shortChunk,0,shortChunk.Length);var w=OwnPrerequisites();w.LayerCountA0.Write(91);w.PathCountA4.Write(92);w.WaveCountA8.Write(93);var r=Reader(shortChunk);int captured=n;Missing(delegate{Load(w,r);},points[captured]);Check(r.Cursor08.Read(0)==n*4,"Bounded reader cursor prefix "+n);Check(w.LayerCountA0.Read(0)==(n>=4?0u:91u)&&w.PathCountA4.Read(0)==(n>=5?0u:92u)&&w.WaveCountA8.Read(0)==93u,"Exact header field-write prefix "+n);}
  });
  Scenario(delegate{var w=OwnPrerequisites();var r=Reader(ReplaceWord(realChunk,2,1));Missing(delegate{Load(w,r);},0x1f8316);Check(r.Cursor08.Read(0)==24&&w.LayerCountA0.Read(0)==0&&w.PathCountA4.Read(0)==0&&w.WaveCountA8.Read(0)==0,"Nonemptytype stops at reserve after all header stores");});
  Scenario(delegate{var w=OwnPrerequisites();var r=Reader(ReplaceWord(realChunk,3,1));Missing(delegate{Load(w,r);},0x1f85a5);Check(r.Cursor08.Read(0)==24&&w.LayerCountA0.Read(0)==1,"Layer allocation stop retains header");});
  Scenario(delegate{var w=OwnPrerequisites();var r=Reader(ReplaceWord(realChunk,4,1));Missing(delegate{Load(w,r);},0x1f86b9);Check(r.Cursor08.Read(0)==24&&w.PathCountA4.Read(0)==1,"Path allocation stop retains header");});
  Scenario(delegate{var w=OwnPrerequisites();var r=Reader(ReplaceWord(realChunk,5,1));Missing(delegate{Load(w,r);},0x1f87c5);Check(r.Cursor08.Read(0)==24&&w.WaveCountA8.Read(0)==1,"Wave allocation stop retains header");});
  Scenario(delegate{byte[] source=(byte[])realChunk.Clone();var r=Reader(source);source[0]=0;Check(Load(OwnPrerequisites(),r),"OWN reader retains stable copy");byte[] trailing=new byte[28];Buffer.BlockCopy(realChunk,0,trailing,0,24);trailing[24]=0xab;var tail=Reader(trailing);Check(Load(OwnPrerequisites(),tail)&&tail.Cursor08.Read(0)==24&&tail.Length==28,"Native load consumes header only, not unrelated trailing bytes");});
  Scenario(delegate{var o=Output();var operands=Operands();operands.Infinity338458.Forget();Missing(delegate{GroundWaterContracts.GetIntersection1F9BD0(real,realRay,o,7,0,operands,new Invocation());},0x1f9be5);Check(o.Distance00.Read(0)==99&&o.Position04.X0.Read(0)==71&&o.Material10.Read(0)==987,"Required constant fails before output writes");});
  Scenario(delegate{var p=Point(71,72,73);var o=new GroundBlockHit20(p.X0,p,Field<uint>.Of(987));Missing(delegate{GroundWaterContracts.GetIntersection1F9BD0(real,realRay,o,7,0,Operands(),new Invocation());},0x1f9bf1);Check(o.Distance00.Read(0)==71&&p.Y4.Read(0)==72&&p.Z8.Read(0)==73,"OWN impossible overlapping output cells rejected before first write");});
  Check(Hash(File.ReadAllBytes(SavedPath))==SavedHash,"Saved input unchanged after tests");
  Console.WriteLine("{\"status\":\"passed\",\"scenarios\":"+scenarios+",\"assertions\":"+assertions+",\"scope\":\"real saved empty Water chunk; explicit OWN nonnull initialization prerequisites; no native constructor/Initialize or nonempty water\"}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}}
}
