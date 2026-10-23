using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using GrimDynasty.ReverseClient.Actions;

public static class GroundGeometryTestProgram {
 const string SavedPath=@"C:\Users\zhuzi\Documents\Grim-Dynasty\docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin";
 const string SavedHash="a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289";
 static int assertions,scenarios;
 static void Check(bool ok,string message){assertions++;if(!ok)throw new Exception(message);}
 static uint Bits(float v){return BitConverter.ToUInt32(BitConverter.GetBytes(v),0);}
 static string Hash(byte[] b){using(var sha=SHA256.Create()){return BitConverter.ToString(sha.ComputeHash(b)).Replace("-","").ToLowerInvariant();}}
 static void Missing(Action action,uint at){assertions++;try{action();}catch(MissingContract e){if(at!=0&&e.Address!=at)throw new Exception("Wrong failure address: "+e.Address.ToString("x"));return;}throw new Exception("Expected OWN Unsupported");}
 static void Invalid(Action action){assertions++;try{action();}catch(InvalidDataException){return;}throw new Exception("Expected OWN safety extent/format failure");}
 static OwnGroundData Small(float a,float b,float c,float d){return OwnGroundColdLoader.DecodeType2Chunk(Fixture(a,b,c,d),"OWN analytical 2x2 fixture");}
 static byte[] Fixture(float a,float b,float c,float d){using(var s=new MemoryStream()){using(var w=new BinaryWriter(s)){
  w.Write(2u);foreach(uint v in new uint[]{1,1,2,2,0,0,0,0})w.Write(v);
  w.Write(Bits(a));w.Write(Bits(b));w.Write(Bits(c));w.Write(Bits(d));w.Write(new byte[4]);w.Write(3u);w.Write(Encoding.ASCII.GetBytes("own"));w.Write(0u);w.Flush();return s.ToArray();
 }}}
 static GroundGeometryOperands Operands(){var o=new GroundGeometryOperands();o.SelectObservedDiskOperands();return o;}
 static float Sample(OwnGroundData data,float x,float z){return GroundGeometryContracts.Sample1DE880(data,x,z,new Invocation());}
 static GroundBlockState Block(OwnGroundData data){var b=new GroundBlockState();b.TerrainA8.Write(data);b.ColumnB0.Write(0);b.RowB4.Write(0);b.WidthB8.Write(2);b.DepthBC.Write(2);return b;}
 static void Scenario(Action action){action();scenarios++;}
 public static int Main(string[] args){try{
  if(args.Length!=1||!String.Equals(args[0],SavedPath,StringComparison.OrdinalIgnoreCase))throw new Exception("Fixed saved member argument required");
  byte[] member=File.ReadAllBytes(SavedPath);Check(member.Length==1091357&&Hash(member)==SavedHash,"Saved input fingerprint");
  byte[] chunk=new byte[149073];Buffer.BlockCopy(member,273260,chunk,0,chunk.Length);OwnGroundData real=null;
  Scenario(delegate{
   real=OwnGroundColdLoader.DecodeType2Chunk(chunk,SavedHash+":273260+149073");uint[] h=real.HeaderWords();
   Check(h.Length==8&&h[0]==1&&h[1]==5&&h[2]==129&&h[3]==129&&h[4]==0&&h[5]==0&&h[6]==0&&h[7]==0,"Raw eight-word header");
   Check(real.ConsumedBytes==149073&&real.Cells128.Read(0).Count==16641&&real.RawArray120().Length==16641&&real.Stride118.Read(0)==1,"Exact saved payload consumption");
   var records=real.TypeRecords();Check(records.Length==5&&records[0].WeightBytes()==null,"Five unresolved types; first has no weights");
   string[] names={"records/terraintextures/earth/flat_rockdark.dbr","records/terraintextures/grass/poly_grasssparserocky.dbr","records/terraintextures/earth/flat_soilforestroots.dbr","records/terraintextures/plants/poly_cornstalks_dead01.dbr","records/terraintextures/earth/flat_leaflittermaplebrown.dbr"};
   bool all=true;for(int k=0;k<5;k++){all&=records[k].AsciiName()==names[k];if(k>0)all&=records[k].WeightBytes().Length==16384;}
   Check(all,"Saved names and four real weight layers");Missing(delegate{records[0].Material.Read(0);},0);Missing(delegate{real.ScalarSlot100.Read(0);},0);
  });
  Scenario(delegate{
   var s=Small(0,2,4,6);Check(Bits(Sample(s,.25f,.75f))==Bits(3.5f),"Actual bilinear operation chain");
   Check(Bits(Sample(s,-10,-10))==Bits(0)&&Bits(Sample(s,float.MaxValue,float.MaxValue))==Bits(6),"Finite coordinate clamp");
   Check(Bits(Sample(s,-0.0f,.5f))==Bits(2)&&Bits(Sample(s,1,.5f))==Bits(4),"Axis/edge interpolation");
   Check(Bits(Sample(s,0,0))==Bits(0)&&Bits(Sample(s,1,1))==Bits(6),"Integer endpoints");
  });
  Scenario(delegate{
   // These are actual saved cell bits, not an original-game execution oracle.
   int[] xs={0,1,128,0,64,128},zs={0,0,0,1,64,128};uint[] expected={0x384d8e9fu,0x4061184bu,0xc085936cu,0x3aeb0d15u,0x41819c63u,0x410d2393u};
   for(int k=0;k<xs.Length;k++)Check(Bits(Sample(real,xs[k],zs[k]))==expected[k],"Real integer sample "+k);
   Check(Bits(Sample(real,-1,-1))==expected[0]&&Bits(Sample(real,10000,10000))==expected[5],"Actual saved sample clamped borders");
  });
  Scenario(delegate{
   real.SelectObservedScalarSlot();var blocks=GroundGeometryContracts.CreateOwnBlockBounds(real,Operands(),new Invocation());
   Check(blocks.Length==225,"Native descriptor formula gives 15x15");bool shared=true;
   foreach(var b in blocks)shared&=Object.ReferenceEquals(b.TerrainA8.Read(0),real);Check(shared,"All blocks share the authoritative terrain object");
   var first=blocks[0];var last=blocks[224];Check(first.WidthB8.Read(0)==10&&first.DepthBC.Read(0)==10&&last.WidthB8.Read(0)==3&&last.DepthBC.Read(0)==3,"Overlapping ten-sample blocks with final three-sample edge");
   Check(first.Box10.Center00.Read(0)==4.5f&&first.Box10.Center08.Read(0)==4.5f&&first.Box10.Extent0C.Read(0)==4.5f&&first.Box10.Extent14.Read(0)==4.5f,"First block XZ bounds");
   Check(last.Box10.Center00.Read(0)==127&&last.Box10.Center08.Read(0)==127&&last.Box10.Extent0C.Read(0)==1&&last.Box10.Extent14.Read(0)==1,"Last block XZ bounds");
   // Independent saved-cell integer scans with per-step binary32 rounding.
   // Static golden references do not assert native runtime Load/collision.
   int[] indices={0,14,112,224};uint[] cy={0x411a9649u,0xc09d9fefu,0x4176dc14u,0x4139b54cu},ey={0x411a9615u,0x3f66ca48u,0x3f7dbda8u,0x403246e6u};
   for(int k=0;k<indices.Length;k++)Check(Bits(blocks[indices[k]].Box10.Center04.Read(0))==cy[k]&&Bits(blocks[indices[k]].Box10.Extent10.Read(0))==ey[k],"Real block scalar bounds "+indices[k]);
   Missing(delegate{last.EngineValue38.Read(0);},0);
  });
  Scenario(delegate{
   byte[] source=Fixture(0,2,4,6);var owned=OwnGroundColdLoader.DecodeType2Chunk(source,"OWN buffer lifetime fixture");source[36]=255;
   Check(Bits(Sample(owned,0,0))==Bits(0),"Decoded buffers own source data");var alias=owned.Cells128.Read(0);alias.WriteOwned(0,Bits(8));Check(Bits(Sample(owned,0,0))==Bits(8),"Sampling reads same live owned cell buffer");
   var raw=owned.RawArray120();raw[0]=255;Check(owned.RawArray120()[0]==0,"Raw packed data not externally mutable through a copy");
  });
  Scenario(delegate{
   var s=Small(0,2,4,6);Missing(delegate{Sample(s,float.NaN,0);},0x1de8a4);Missing(delegate{Sample(s,0,float.PositiveInfinity);},0x1de89c);
   s.Cells128.Read(0).WriteOwned(0,0x7f800000u);Missing(delegate{Sample(s,0,0);},0x1de90a);
   s=Small(-float.MaxValue,float.MaxValue,0,0);var stop=new Invocation();Missing(delegate{GroundGeometryContracts.Sample1DE880(s,.5f,0,stop);},0x1de922);Check(stop.At==0x1de922,"Arithmetic failure Invocation matches exception address");
   s=Small(0,2,4,6);s.Dimension60.Write(uint.MaxValue);Missing(delegate{Sample(s,0,0);},0x1de8a4);
   s.Dimension60.Write(100000);s.Dimension64.Write(100000);Missing(delegate{Sample(s,0,50000);},0x1de8e8);
   s=Small(0,2,4,6);s.Dimension60.Write(3);Missing(delegate{Sample(s,2,1);},0x1de90a);
   s.Cells128.Forget();Missing(delegate{Sample(s,0,0);},0x1de90a);s.Cells128.Write(null);Missing(delegate{Sample(s,0,0);},0x1de90a);
  });
  Scenario(delegate{
   var s=Small(0,2,4,6);var block=Block(s);block.Box10.Center00.Write(99);var o=new GroundGeometryOperands();
   Missing(delegate{GroundGeometryContracts.UpdateBounds1D0FC0(block,s,o,new Invocation());},0x1d1005);Check(block.Box10.Center00.Read(0)==99,"Unknown operand stops before bounds stores");
   o.SelectObservedDiskOperands();Missing(delegate{GroundGeometryContracts.UpdateBounds1D0FC0(block,s,o,new Invocation());},0x1d1064);
   s.SelectObservedScalarSlot();o.Half34BADC.Forget();Missing(delegate{GroundGeometryContracts.UpdateBounds1D0FC0(block,s,o,new Invocation());},0x1d10a5);Check(block.Box10.Center00.Read(0)==99,"Post-sampling missing half preserves write prefix");
   o.SelectObservedDiskOperands();Missing(delegate{GroundGeometryContracts.UpdateBounds1D0FC0(block,null,o,new Invocation());},0x1d1050);
   o.SelectObservedDiskOperands();GroundGeometryContracts.UpdateBounds1D0FC0(block,s,o,new Invocation());Check(block.Box10.Center04.Read(0)==3&&block.Box10.Extent10.Read(0)==3,"Real sampler connected to bounds");
   block.ColumnB0.Write(uint.MaxValue);Missing(delegate{GroundGeometryContracts.UpdateBounds1D0FC0(block,s,o,new Invocation());},0x1d0ff7);
  });
  Scenario(delegate{
   byte[] valid=Fixture(0,2,4,6);var truncated=new byte[valid.Length-1];Buffer.BlockCopy(valid,0,truncated,0,truncated.Length);Invalid(delegate{OwnGroundColdLoader.DecodeType2Chunk(truncated,"OWN truncated");});
   var extra=new byte[valid.Length+1];Buffer.BlockCopy(valid,0,extra,0,valid.Length);Invalid(delegate{OwnGroundColdLoader.DecodeType2Chunk(extra,"OWN extra");});
   var bad=(byte[])valid.Clone();bad[0]=3;Invalid(delegate{OwnGroundColdLoader.DecodeType2Chunk(bad,"OWN unsupported discriminator");});
   bad=(byte[])valid.Clone();bad[12]=0;Invalid(delegate{OwnGroundColdLoader.DecodeType2Chunk(bad,"OWN zero width");});
   bad=(byte[])valid.Clone();bad[56]=255;bad[57]=255;bad[58]=255;bad[59]=127;Invalid(delegate{OwnGroundColdLoader.DecodeType2Chunk(bad,"OWN oversized string");});
  });
  Check(Hash(File.ReadAllBytes(SavedPath))==SavedHash,"Saved input unchanged after tests");
  Console.WriteLine("{\"status\":\"passed\",\"scenarios\":"+scenarios+",\"assertions\":"+assertions+",\"saved_ground_pipeline\":true,\"native_load\":false,\"full_construct_blocks\":false,\"floor_collision\":false,\"gameplay_accepted\":false}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}}
}
