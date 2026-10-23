using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using GrimDynasty.ReverseClient.Actions;

public static class GroundTriangleIntersectionTestProgram {
 const string SavedPath=@"C:\Users\zhuzi\Documents\Grim-Dynasty\docs\reverse-client\r1-mainmenu-complete-member\mainmenu.map.bin";
 const string SavedHash="a0e4559c417802c284b19038bfa9e23f3419996457dd78b4d2669d292313b289";
 static int assertions,scenarios;
 static void Check(bool ok,string message){assertions++;if(!ok)throw new Exception(message);}
 static uint Bits(float x){return BitConverter.ToUInt32(BitConverter.GetBytes(x),0);}
 static Displacement3 Point(float x,float y,float z){var p=new Displacement3();p.X0.Write(x);p.Y4.Write(y);p.Z8.Write(z);return p;}
 static GroundTriangleOperands Operands(){var o=new GroundTriangleOperands();o.SelectObservedDiskOperands();return o;}
 static string Hash(byte[] b){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(b)).Replace("-","").ToLowerInvariant();}
 static void Copy(Displacement3 to,Displacement3 from){to.X0.CopyFrom(from.X0);to.Y4.CopyFrom(from.Y4);to.Z8.CopyFrom(from.Z8);}
 static GroundIntersectionFace Face(Displacement3 a,Displacement3 b,Displacement3 c){var f=new GroundIntersectionFace();Copy(f.A00,a);Copy(f.B0C,b);Copy(f.C18,c);GroundTriangleIntersectionContracts.Plane044660(f.Plane24,f.A00,f.B0C,f.C18,Operands(),new Invocation());return f;}
 static GroundRay24 Ray(float x,float y,float z,float dx,float dy,float dz){return new GroundRay24(Point(x,y,z),Point(dx,dy,dz));}
 static GroundIntersectionResult Result(){var r=new GroundIntersectionResult();r.Distance.Write(float.PositiveInfinity);r.X.Write(999);r.Z.Write(999);return r;}
 static OwnGroundData Ground(uint w,uint d,float[] cells){using(var s=new MemoryStream()){using(var b=new BinaryWriter(s)){b.Write(2u);foreach(uint v in new uint[]{1,1,w,d,0,0,0,0})b.Write(v);foreach(float f in cells)b.Write(Bits(f));b.Write(new byte[(int)(w*d)]);b.Write(3u);b.Write(Encoding.ASCII.GetBytes("own"));b.Write(0u);b.Flush();return OwnGroundColdLoader.DecodeType2Chunk(s.ToArray(),"OWN triangle fixture");}}}
 static GroundIntersectionResult Sweep(OwnGroundData data,GroundRay24 ray,uint x,uint z,uint ex,uint ez){var r=Result();GroundTriangleIntersectionContracts.Sweep1DF220(data,ray,x,z,ex,ez,r,Operands(),new Invocation());return r;}
 static void Missing(Action action,uint at){assertions++;try{action();}catch(MissingContract e){if(e.Address!=at)throw new Exception("Wrong stop address "+e.Address.ToString("x"));return;}throw new Exception("Expected OWN Unsupported");}
 static void Scenario(Action action){action();scenarios++;}
 public static int Main(string[] args){try{
  if(args.Length!=1||!String.Equals(args[0],SavedPath,StringComparison.OrdinalIgnoreCase))throw new Exception("Fixed saved member argument required");
  Scenario(delegate{
   var p=new GroundPlane4();var returned=GroundTriangleIntersectionContracts.Plane044660(p,Point(2,3,4),Point(2,3,5),Point(3,3,5),Operands(),new Invocation());
   Check(Object.ReferenceEquals(p,returned),"Plane return identity");Check(p.X00.Read(0)==0&&p.Y04.Read(0)==1&&p.Z08.Read(0)==0&&p.D0C.Read(0)==-3,"Horizontal plane with offset");
   p=new GroundPlane4();GroundTriangleIntersectionContracts.Plane044660(p,Point(0,0,0),Point(0,0,1),Point(1,0,1),Operands(),new Invocation());Check(Bits(p.D0C.Read(0))==0x80000000u,"Observed XOR preserves negative zero D");
  });
  Scenario(delegate{
   var a=Point(2,3,4);var p=new GroundPlane4(a.X0,a.Y4,a.Z8,new Field<float>());
   GroundTriangleIntersectionContracts.Plane044660(p,a,Point(2,3,5),Point(3,3,5),Operands(),new Invocation());
   Check(a.X0.Read(0)==0&&a.Y4.Read(0)==1&&a.Z8.Read(0)==0,"Supported exact-cell output/A aliases");Check(p.D0C.Read(0)==-1,"D reads A after normal writes");
  });
  Scenario(delegate{
   var o=Operands();o.XorMask34C450.Forget();var p=new GroundPlane4();p.D0C.Write(88);var stop=new Invocation();
   Missing(delegate{GroundTriangleIntersectionContracts.Plane044660(p,Point(2,3,4),Point(2,3,5),Point(3,3,5),o,stop);},0x044769);
   Check(stop.At==0x044769&&p.X00.Read(0)==0&&p.Y04.Read(0)==1&&p.Z08.Read(0)==0&&p.D0C.Read(0)==88,"Unknown XOR preserves exact partial write prefix");
   o=Operands();o.One34BB44.Forget();p=new GroundPlane4();p.X00.Write(77);
   Missing(delegate{GroundTriangleIntersectionContracts.Plane044660(p,Point(2,3,4),Point(2,3,5),Point(3,3,5),o,new Invocation());},0x0446dc);Check(p.X00.Read(0)==77,"Unknown one before writes");
  });
  Scenario(delegate{
   var a=Point(1,2,3);var p=new GroundPlane4();var stop=new Invocation();
   Missing(delegate{GroundTriangleIntersectionContracts.Plane044660(p,a,a,a,Operands(),stop);},0x044723);Check(stop.At==0x044723,"Degenerate plane is Unsupported, not miss or fake success");
   Missing(delegate{p.Y04.Read(0x044730);},0x044730);
   Missing(delegate{GroundTriangleIntersectionContracts.Plane044660(p,Point(0,0,0),Point(0,0,1),Point(1,0,float.NaN),Operands(),new Invocation());},0x044664);
  });
  Scenario(delegate{
   var f=Face(Point(0,0,0),Point(0,0,1),Point(1,0,1));var o=Operands();
   Check(GroundTriangleIntersectionContracts.PlaneRay051B30(f.Plane24,Ray(.25f,10,.75f,0,-1,0),o,new Invocation())==10,"Real ray/plane distance");
   Check(GroundTriangleIntersectionContracts.FaceIntersection1CF830(f,Ray(.25f,10,.75f,0,-1,0),o,new Invocation())==10,"Downward front face");
   Check(float.IsPositiveInfinity(GroundTriangleIntersectionContracts.FaceIntersection1CF830(f,Ray(.25f,-1,.75f,0,1,0),o,new Invocation())),"Back face rejected despite positive plane t");
   Check(float.IsPositiveInfinity(GroundTriangleIntersectionContracts.FaceIntersection1CF830(f,Ray(.25f,-1,.75f,0,-1,0),o,new Invocation())),"Behind ray rejected");
   var parallel=new GroundRay24(new Displacement3(),Point(1,0,0));Check(float.IsPositiveInfinity(GroundTriangleIntersectionContracts.PlaneRay051B30(f.Plane24,parallel,o,new Invocation())),"Parallel ray avoids unknown origin");
   Check(float.IsPositiveInfinity(GroundTriangleIntersectionContracts.FaceIntersection1CF830(f,Ray(2,10,.5f,0,-1,0),o,new Invocation())),"Plane hit outside triangle rejected");
   Check(GroundTriangleIntersectionContracts.FaceIntersection1CF830(f,Ray(.25f,0,.75f,0,-1,0),o,new Invocation())==0,"Zero distance accepted");
   Check(GroundTriangleIntersectionContracts.FaceIntersection1CF830(f,Ray(-.25f,1,-.75f,1,-2,3),o,new Invocation())==.5f,"Nonunit oblique direction preserves parameter t and XYZ synthesis");
  });
  Scenario(delegate{
   var o=Operands();var flat=Face(Point(0,0,0),Point(0,0,1),Point(1,0,1));
   Check(GroundTriangleIntersectionContracts.PointInFace1CF420(flat,Point(.25f,0,.75f),o,new Invocation()),"Y dominant interior");
   Check(!GroundTriangleIntersectionContracts.PointInFace1CF420(flat,Point(.75f,0,.25f),o,new Invocation()),"Projected outside");
   Check(GroundTriangleIntersectionContracts.PointInFace1CF420(flat,Point(1,0,1),o,new Invocation()),"Vertex boundary included");
   var xf=Face(Point(0,0,0),Point(0,1,0),Point(0,0,1));Check(GroundTriangleIntersectionContracts.PointInFace1CF420(xf,Point(0,.25f,.25f),o,new Invocation()),"X dominant projection");
   var zf=Face(Point(0,0,0),Point(1,0,0),Point(0,1,0));Check(GroundTriangleIntersectionContracts.PointInFace1CF420(zf,Point(.25f,.25f,0),o,new Invocation()),"Z dominant reversed axis order");
   var tie=Face(Point(0,0,0),Point(1,1,0),Point(0,0,1));var p=Point(.25f,.25f,.25f);p.X0.Forget();Check(GroundTriangleIntersectionContracts.PointInFace1CF420(tie,p,o,new Invocation()),"X/Y tie drops X without reading unknown point X");
   tie=Face(Point(0,0,0),Point(1,0,0),Point(0,1,1));p=Point(.25f,.25f,.25f);p.Y4.Forget();Check(GroundTriangleIntersectionContracts.PointInFace1CF420(tie,p,o,new Invocation()),"Y/Z tie drops Y");
  });
  Scenario(delegate{
   var data=Ground(2,2,new float[]{0,0,0,4});var r=Sweep(data,Ray(.75f,10,.25f,0,-1,0),0,0,0,0);
   Check(Math.Abs(r.Distance.Read(0)-9)<0.00001f&&r.X.Read(0)==0&&r.Z.Read(0)==0,"Second triangle height1; bilinear height0.75 would be wrong");
   r=Sweep(data,Ray(.25f,10,.75f,0,-1,0),0,0,0,0);Check(Math.Abs(r.Distance.Read(0)-9)<0.00001f,"First triangle branch");
   r=Sweep(data,Ray(.5f,10,.5f,0,-1,0),0,0,0,0);Check(Math.Abs(r.Distance.Read(0)-8)<0.00001f,"Shared diagonal");
   data.Cells128.Read(0).WriteOwned(3,Bits(8));r=Sweep(data,Ray(.75f,10,.25f,0,-1,0),0,0,0,0);Check(Math.Abs(r.Distance.Read(0)-8)<0.00001f,"Same owned cells remain authoritative");
   var alias=new Field<uint>();var ar=new GroundIntersectionResult(Field<float>.Of(float.PositiveInfinity),alias,alias);var grid=Ground(3,3,new float[9]);
   GroundTriangleIntersectionContracts.Sweep1DF220(grid,Ray(1.5f,10,.5f,0,-1,0),1,0,1,0,ar,Operands(),new Invocation());Check(ar.Distance.Read(0)==10&&alias.Read(0)==0,"Borrowed X/Z output alias retains X then Z write order");
   var equal=Result();equal.Distance.Write(10);equal.X.Write(77);equal.Z.Write(88);GroundTriangleIntersectionContracts.Sweep1DF220(grid,Ray(.5f,10,.5f,0,-1,0),0,0,0,0,equal,Operands(),new Invocation());Check(equal.Distance.Read(0)==10&&equal.X.Read(0)==77&&equal.Z.Read(0)==88,"Both equal-distance diagonal candidates preserve original output");
  });
  Scenario(delegate{
   byte[] bytes=File.ReadAllBytes(SavedPath);Check(bytes.Length==1091357&&Hash(bytes)==SavedHash,"Pinned real saved member");var chunk=new byte[149073];Buffer.BlockCopy(bytes,273260,chunk,0,chunk.Length);var data=OwnGroundColdLoader.DecodeType2Chunk(chunk,SavedHash+":273260+149073");
   int[] xs={0,1,64,127},zs={0,0,64,127};var cells=data.Cells128.Read(0);
   for(int k=0;k<xs.Length;k++){
    int x=xs[k],z=zs[k];float h00=BitConverter.ToSingle(BitConverter.GetBytes(cells.Read((uint)(z*129+x),0)),0),h01=BitConverter.ToSingle(BitConverter.GetBytes(cells.Read((uint)((z+1)*129+x),0)),0),h11=BitConverter.ToSingle(BitConverter.GetBytes(cells.Read((uint)((z+1)*129+x+1),0)),0);
    // Independent double barycentric reference for the first triangle at(.25,.75).
    // Tolerance covers per-instruction binary32 plane normalization/coordinate sums;
    // this is not an original runtime comparison or a bilinear reference.
    double expected=32-(.25*(double)h00+.5*(double)h01+.25*(double)h11);var r=Sweep(data,Ray(x+.25f,32,z+.75f,0,-1,0),(uint)x,(uint)z,(uint)x,(uint)z);
    Check(Math.Abs(r.Distance.Read(0)-expected)<0.0002&&r.X.Read(0)==(uint)x&&r.Z.Read(0)==(uint)z,"Actual saved quad intersection "+k);
   }
   var multi=Sweep(data,Ray(64.25f,32,64.75f,0,-1,0),63,63,65,65);Check(!float.IsInfinity(multi.Distance.Read(0))&&multi.X.Read(0)==64&&multi.Z.Read(0)==64,"Actual multi-quad scan selects real hit");
   Check(Hash(File.ReadAllBytes(SavedPath))==SavedHash,"Saved member unchanged");
  });
  Scenario(delegate{
   var data=Ground(3,3,new float[9]);data.Cells128.Read(0).WriteOwned(2,Bits(float.NaN));var r=Result();var stop=new Invocation();
   Missing(delegate{GroundTriangleIntersectionContracts.Sweep1DF220(data,Ray(.25f,10,.75f,0,-1,0),0,0,1,0,r,Operands(),stop);},0x1df357);
   Check(stop.At==0x1df357&&r.Distance.Read(0)==10&&r.X.Read(0)==0&&r.Z.Read(0)==0,"Later cell Unsupported retains prior real hit prefix");
   r=new GroundIntersectionResult();data=Ground(2,2,new float[4]);Missing(delegate{GroundTriangleIntersectionContracts.Sweep1DF220(data,Ray(.25f,10,.75f,0,-1,0),0,0,0,0,r,Operands(),new Invocation());},0x1df3d7);
   Missing(delegate{r.X.Read(0x1df3e0);},0x1df3e0);
  });
  Scenario(delegate{
   GroundTriangleIntersectionContracts.Sweep1DF220(null,null,0,1,0,0,null,null,new Invocation());GroundTriangleIntersectionContracts.Sweep1DF220(null,null,1,0,0,1,null,null,new Invocation());Check(true,"Finite empty ranges do not dereference inputs");
   Missing(delegate{GroundTriangleIntersectionContracts.Sweep1DF220(null,null,1,0,0,uint.MaxValue,null,null,new Invocation());},0x1df2c2);
   var data=Ground(2,2,new float[4]);var r=Result();Missing(delegate{GroundTriangleIntersectionContracts.Sweep1DF220(data,Ray(0,1,0,0,-1,0),1,0,1,0,r,Operands(),new Invocation());},0x1df32f);
   data.Cells128.Forget();Missing(delegate{GroundTriangleIntersectionContracts.Sweep1DF220(data,Ray(0,1,0,0,-1,0),0,0,0,0,r,Operands(),new Invocation());},0x1df32f);
   var face=Face(Point(0,0,0),Point(0,0,1),Point(1,0,1));var o=Operands();o.AbsMask34C410.Forget();Missing(delegate{GroundTriangleIntersectionContracts.FaceIntersection1CF830(face,Ray(.25f,10,.75f,0,-1,0),o,new Invocation());},0x1cf424);
   Missing(delegate{GroundTriangleIntersectionContracts.FaceIntersection1CF830(null,Ray(0,1,0,0,-1,0),Operands(),new Invocation());},0x051b34);
  });
  Console.WriteLine("{\"status\":\"passed\",\"scenarios\":"+scenarios+",\"assertions\":"+assertions+",\"ground_plane\":true,\"ground_intersection\":true,\"saved_triangle_ray\":true,\"native_load\":false,\"floor_collision\":false,\"gameplay_accepted\":false}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}}
}
