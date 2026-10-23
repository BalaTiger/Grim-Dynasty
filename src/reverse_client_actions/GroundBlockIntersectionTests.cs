using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using GrimDynasty.ReverseClient.Actions;

public static class GroundBlockIntersectionTestProgram {
 const string SavedPath=@"C:\Users\zhuzi\Documents\Grim-Dynasty\docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin";
 const string SavedHash="a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289";
 static int assertions,scenarios;
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Scenario(Action action){action();scenarios++;}
 static void Missing(Action action,uint at){assertions++;try{action();}catch(MissingContract e){if(e.Address!=at)throw new Exception("Wrong Unsupported address "+e.Address.ToString("x"));return;}throw new Exception("Expected Unsupported "+at.ToString("x"));}
 static uint Bits(float x){return BitConverter.ToUInt32(BitConverter.GetBytes(x),0);}
 static float Float(uint x){return BitConverter.ToSingle(BitConverter.GetBytes(x),0);}
 static string Hash(byte[] b){using(var s=SHA256.Create())return BitConverter.ToString(s.ComputeHash(b)).Replace("-","").ToLowerInvariant();}
 static Displacement3 Point(float x,float y,float z){var p=new Displacement3();p.X0.Write(x);p.Y4.Write(y);p.Z8.Write(z);return p;}
 static GroundRay24 Ray(float x,float y,float z,float dx,float dy,float dz){return new GroundRay24(Point(x,y,z),Point(dx,dy,dz));}
 static GroundGeometryOperands Geometry(){var o=new GroundGeometryOperands();o.SelectObservedDiskOperands();return o;}
 static GroundBlockIntersectionOperands Operands(){var o=new GroundBlockIntersectionOperands();o.SelectObservedDiskOperands();return o;}
 static GroundTerrainIntersectionBinding Binding(OwnGroundData d){var b=new GroundTerrainIntersectionBinding(d);b.SelectObservedDiskSlots();return b;}
 static GroundBlockHit20 Result(){return new GroundBlockHit20(Field<float>.Of(99),Point(71,72,73),Field<uint>.Of(987));}
 static bool UnwrittenXYZ(GroundBlockHit20 r){return r.Position04.X0.Read(0)==71&&r.Position04.Y4.Read(0)==72&&r.Position04.Z8.Read(0)==73;}
 static OwnGroundData Data(byte membership,params byte[] weights){using(var s=new MemoryStream()){using(var b=new BinaryWriter(s)){
  b.Write(2u);foreach(uint x in new uint[]{1,(uint)weights.Length+1,2,2,0,0,0,0})b.Write(x);
  for(int k=0;k<4;k++)b.Write(0u);for(int k=0;k<4;k++)b.Write(membership);
  for(int k=0;k<=weights.Length;k++){var name=Encoding.ASCII.GetBytes("own"+k);b.Write((uint)name.Length);b.Write(name);if(k!=0)b.Write(weights[k-1]);}
  b.Write(0u);b.Flush();var d=OwnGroundColdLoader.DecodeType2Chunk(s.ToArray(),"OWN explicit block fixture");d.SelectObservedScalarSlot();return d;
 }}}
 static GroundBlockMaterialState State(OwnGroundData d){var s=new GroundBlockMaterialState(GroundGeometryContracts.CreateOwnBlockBounds(d,Geometry(),new Invocation())[0]);s.Populate(Geometry(),new Invocation());return s;}
 static GroundMaterialSurfaceState Surface(OwnGroundData d,uint record,uint raw){var m=new GroundMaterialSurfaceState();m.SurfaceFC.Write(raw);d.TypeRecordAt(record,0).Material.Write(m);return m;}
 static void Run(GroundBlockMaterialState s,GroundRay24 ray,GroundBlockHit20 r,GroundTerrainIntersectionBinding b,GroundBlockIntersectionOperands o,Invocation i){GroundBlockIntersectionContracts.Intersect1D1330(s,ray,r,b,o,i);}
 public static int Main(string[] args){try{
  if(args.Length!=1||!String.Equals(args[0],SavedPath,StringComparison.OrdinalIgnoreCase))throw new Exception("Fixed saved member argument required");
  Scenario(delegate{
   var d=Data(2);var s=State(d);s.Block.TerrainA8.Forget();var r=Result();
   Run(s,Ray(4,1,4,0,-1,0),r,null,Operands(),new Invocation());
   Check(float.IsPositiveInfinity(r.Distance00.Read(0))&&r.Material10.Read(0)==100&&UnwrittenXYZ(r),"Box miss writes Inf/100, leaves XYZ and never needs Terrain/slots");
   r=Result();var o=Operands();o.Geometry.Infinity338458.Forget();Missing(delegate{Run(s,null,r,null,o,new Invocation());},0x1d1340);
   Check(r.Distance00.Read(0)==99&&r.Material10.Read(0)==987&&UnwrittenXYZ(r),"Unknown first constant before output stores");
  });
  Scenario(delegate{
   var d=Data(0);var s=State(d);Check(s.CandidatesC8.Count==0,"Actually populated bitmap produces no candidates");
   var b=Binding(d);b.SlotF0.Forget();b.SlotD8.Forget();var r=Result();
   Run(s,Ray(-.25f,1,-.75f,1,-2,3),r,b,Operands(),new Invocation());
   Check(r.Distance00.Read(0)==.5f&&r.Material10.Read(0)==100,"Genuine empty candidate branch keeps native default");
   Check(r.Position04.X0.Read(0)==.25f&&r.Position04.Y4.Read(0)==0&&r.Position04.Z8.Read(0)==.75f,"Nonunit oblique ray produces actual XYZ");
   Missing(delegate{s.Block.EngineValue38.Read(0x1d1340);},0x1d1340);
  });
  Scenario(delegate{
   var d=Data(2);var s=State(d);var r=Result();var b=Binding(d);b.Slot58.Forget();
   Missing(delegate{Run(s,Ray(.25f,10,.75f,0,-1,0),r,b,Operands(),new Invocation());},0x1d13da);
   Check(float.IsPositiveInfinity(r.Distance00.Read(0))&&r.Material10.Read(0)==100&&UnwrittenXYZ(r),"Unknown sweep dispatch preserves initial prefix");
   b.SelectObservedDiskSlots();var i=new Invocation();r=Result();
   Missing(delegate{Run(s,Ray(.25f,10,.75f,0,-1,0),r,b,Operands(),i);},0x1d1528);
   Check(i.At==0x1d1528&&r.Distance00.Read(0)==10&&r.Material10.Read(0)==100&&UnwrittenXYZ(r),"Unknown resource pointer is consumed at TEST, after real distance but before XYZ");
   s.Block.TerrainA8.Forget();r=Result();Missing(delegate{Run(s,Ray(.25f,10,.75f,0,-1,0),r,b,Operands(),new Invocation());},0x1d13a5);
  });
  Scenario(delegate{
   var d=Data(2);var s=State(d);var b=Binding(d);var ray=Ray(.25f,10,.75f,0,-1,0);var r=Result();
   d.TypeRecordAt(0,0).Material.Write(null);Run(s,ray,r,b,Operands(),new Invocation());
   Check(r.Distance00.Read(0)==10&&r.Material10.Read(0)==100&&r.Position04.Y4.Read(0)==0,"Explicit fixture null follows observed branch; not real resource resolution");
   d.TypeRecordAt(0,0).Material.Write(new object());r=Result();Missing(delegate{Run(s,ray,r,b,Operands(),new Invocation());},0x1d152d);
   Check(r.Distance00.Read(0)==10&&UnwrittenXYZ(r),"Opaque resource identity has no fabricated FC field");
   var m=new GroundMaterialSurfaceState();d.TypeRecordAt(0,0).Material.Write(m);r=Result();Run(s,ray,r,b,Operands(),new Invocation());
   Missing(delegate{r.Material10.Read(0x1d1533);},0x1d1533);Missing(delegate{m.SurfaceFC.Read(0x1d152d);},0x1d152d);
   Check(r.Distance00.Read(0)==10&&r.Position04.X0.Read(0)==.25f&&r.Position04.Y4.Read(0)==0&&r.Position04.Z8.Read(0)==.75f,"Known valid material field copies Unknown DWORD and completes XYZ; resource value is not fabricated");
   m.SurfaceFC.Write(0xfedcba98);Run(s,ray,r,b,Operands(),new Invocation());
   Check(Object.ReferenceEquals(m,d.TypeRecordAt(0,0).Material.Read(0))&&r.Material10.Read(0)==0xfedcba98&&r.Position04.Z8.Read(0)==.75f,"Same resource identity and full raw DWORD transfer");
  });
  Scenario(delegate{
   var d=Data(14,153,102);var s=State(d);var m=Surface(d,0,77);var r=Result();var v=s.CandidatesC8.StorageIdentity;
   Run(s,Ray(.25f,10,.75f,0,-1,0),r,Binding(d),Operands(),new Invocation());
   Check(s.CandidatesC8.ReadAt(0,0)==0&&s.CandidatesC8.ReadAt(1,0)==1&&s.CandidatesC8.ReadAt(2,0)==2,"Actual candidate production order");
   Check(r.Material10.Read(0)==77,"Previous-score attenuation selects base; cumulative opacity would select top");
   Check(Object.ReferenceEquals(v,s.CandidatesC8.StorageIdentity)&&Object.ReferenceEquals(d,s.Block.TerrainA8.Read(0)),"Consumer retains live vector/terrain identities");
   m.SurfaceFC.Write(78);Run(s,Ray(.25f,10,.75f,0,-1,0),r,Binding(d),Operands(),new Invocation());Check(r.Material10.Read(0)==78,"Resource field reread on next real invocation");
  });
  Scenario(delegate{
   var d=Data(14,13,255);var s=State(d);Surface(d,2,22);var r=Result();Run(s,Ray(.25f,10,.75f,0,-1,0),r,Binding(d),Operands(),new Invocation());
   Check(r.Material10.Read(0)==22,"Strict best>remaining break avoids lower unknown resources");
   d=Data(12,0,0);s=State(d);Surface(d,1,11);r=Result();Run(s,Ray(.25f,10,.75f,0,-1,0),r,Binding(d),Operands(),new Invocation());
   Check(r.Material10.Read(0)==11,"Zero-score ties retain selected vector index0, not last visited native record");
   var b=Binding(d);b.SlotF0.Forget();r=Result();Missing(delegate{Run(s,Ray(.25f,10,.75f,0,-1,0),r,b,Operands(),new Invocation());},0x1d14b1);
   Check(r.Distance00.Read(0)==10&&UnwrittenXYZ(r),"Weight dispatch failure preserves hit distance");
   b.SelectObservedDiskSlots();b.SlotD8.Forget();Missing(delegate{Run(s,Ray(.25f,10,.75f,0,-1,0),r,b,Operands(),new Invocation());},0x1d1512);
  });
  Scenario(delegate{
   Check(GroundBlockIntersectionContracts.Weight1DE850(null,0,uint.MaxValue,uint.MaxValue,new Invocation())==255,"record0 AL=FF does not access receiver/dimensions/coordinates");
   var d=Data(6,93);Check(GroundBlockIntersectionContracts.Weight1DE850(d,1,0,0,new Invocation())==93,"Actual record+10 byte plane");
   Missing(delegate{GroundBlockIntersectionContracts.Weight1DE850(d,1,1,0,new Invocation());},0x1de879);
   d.Dimension60.Forget();Missing(delegate{GroundBlockIntersectionContracts.Weight1DE850(d,1,0,0,new Invocation());},0x1de864);
   var pointer=GroundBlockIntersectionContracts.Material1DE810(d,0,new Invocation());Missing(delegate{pointer.Read(0x1d1528);},0x1d1528);
   d.TypeRecordAt(0,0).Material.Write(null);Missing(delegate{pointer.Read(0x1d1528);},0x1d1528);
   Check(GroundBlockIntersectionContracts.Material1DE810(d,0,new Invocation()).Read(0)==null,"Returned pointer capture retains prior Unknown; later call reads actual record anew");
  });
  Scenario(delegate{
   var d=Data(2);var s=State(d);var m=new GroundMaterialSurfaceState();d.TypeRecordAt(0,0).Material.Write(m);
   var r=new GroundBlockHit20(Field<float>.Of(99),Point(71,72,73),m.SurfaceFC);
   Run(s,Ray(.25f,10,.75f,0,-1,0),r,Binding(d),Operands(),new Invocation());
   Check(Object.ReferenceEquals(r.Material10,m.SurfaceFC)&&m.SurfaceFC.Read(0)==100,"Exact DWORD cell alias observes caller initial100 before later material capture");
   Check(r.Distance00.Read(0)==10&&r.Position04.Y4.Read(0)==0,"Alias does not suppress geometric completion");
  });
  Scenario(delegate{
   byte[] saved=File.ReadAllBytes(SavedPath);Check(saved.Length==1091357&&Hash(saved)==SavedHash,"Exact saved real member fingerprint");
   var chunk=new byte[149073];Buffer.BlockCopy(saved,273260,chunk,0,chunk.Length);var d=OwnGroundColdLoader.DecodeType2Chunk(chunk,SavedHash);d.SelectObservedScalarSlot();
   var blocks=GroundGeometryContracts.CreateOwnBlockBounds(d,Geometry(),new Invocation());var cells=d.Cells128.Read(0);Check(blocks.Length==225,"Real clipped block descriptors");
   foreach(int origin in new int[]{4,64,127}){
    float x=origin+.25f,z=origin+.75f;var s=new GroundBlockMaterialState(blocks[(origin/9)*15+origin/9]);s.Populate(Geometry(),new Invocation());Check(s.CandidatesC8.Count>0,"Real populated candidates "+origin);
    var r=Result();var i=new Invocation();Missing(delegate{Run(s,Ray(x,32,z,0,-1,0),r,Binding(d),Operands(),i);},0x1d1528);
    uint start=(uint)(origin*129+origin);double h00=Float(cells.Read(start,0)),h01=Float(cells.Read(start+129,0)),h11=Float(cells.Read(start+130,0));
    double expected=32-(h00+.25*(h11-h01)+.75*(h01-h00));
    Check(Math.Abs(r.Distance00.Read(0)-expected)<.0002&&i.At==0x1d1528,"Real geometry computes distance before resource Unknown "+origin);
    Check(r.Material10.Read(0)==100&&UnwrittenXYZ(r)&&Object.ReferenceEquals(d,s.Block.TerrainA8.Read(0)),"Real resource failure prefix/ownership "+origin);
   }
   Check(Hash(File.ReadAllBytes(SavedPath))==SavedHash,"Saved input remains unchanged");
  });
  Console.WriteLine("{\"status\":\"passed\",\"scenarios\":"+scenarios+",\"assertions\":"+assertions+",\"ground_block_intersection\":true,\"saved_geometry_to_resource_unknown\":true,\"real_material_resolved\":false,\"native_load\":false,\"world_floor\":false,\"gameplay_accepted\":false}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}}
}
