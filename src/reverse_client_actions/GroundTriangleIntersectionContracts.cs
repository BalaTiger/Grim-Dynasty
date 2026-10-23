using System;

namespace GrimDynasty.ReverseClient.Actions {
 public sealed class GroundPlane4 {
  public readonly Field<float> X00,Y04,Z08,D0C;
  public GroundPlane4():this(new Field<float>(),new Field<float>(),new Field<float>(),new Field<float>()){}
  public GroundPlane4(Field<float> x,Field<float> y,Field<float> z,Field<float> d){if(x==null||y==null||z==null||d==null)throw new ArgumentNullException("plane cells");X00=x;Y04=y;Z08=z;D0C=d;}
 }
 public sealed class GroundIntersectionFace {
  public readonly object Identity=new object();
  public readonly Displacement3 A00=new Displacement3(),B0C=new Displacement3(),C18=new Displacement3();
  public readonly GroundPlane4 Plane24=new GroundPlane4();
 }
 public sealed class GroundRay24 {
  public readonly object Identity=new object();
  public readonly Displacement3 Origin00,Direction0C;
  public GroundRay24():this(new Displacement3(),new Displacement3()){}
  public GroundRay24(Displacement3 origin,Displacement3 direction){if(origin==null||direction==null)throw new ArgumentNullException("ray views");Origin00=origin;Direction0C=direction;}
 }
 public sealed class GroundIntersectionResult {
  public readonly Field<float> Distance;
  public readonly Field<uint> X,Z;
  public GroundIntersectionResult():this(new Field<float>(),new Field<uint>(),new Field<uint>()){}
  // Borrow actual output cells. X/Z may share one cell; writes retain order.
  // Cross-type/shifted byte aliases and concurrent access are outside OWN scope.
  public GroundIntersectionResult(Field<float> distance,Field<uint> x,Field<uint> z){if(distance==null||x==null||z==null)throw new ArgumentNullException("result cells");Distance=distance;X=x;Z=z;}
 }
 public sealed class GroundTriangleOperands {
  public readonly Field<uint> One34BB44=new Field<uint>(),XorMask34C450=new Field<uint>(),Infinity338458=new Field<uint>(),AbsMask34C410=new Field<uint>();
  public void SelectObservedDiskOperands(){One34BB44.Write(0x3f800000u);XorMask34C450.Write(0x80000000u);Infinity338458.Write(0x7f800000u);AbsMask34C410.Write(0x7fffffffu);}
 }
 // Engine044660,051B30,1CF420,1CF830,1DF220. Original SHA256:
 // 4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153.
 // OWN finite scalar domain, stable managed lifetimes, round-to-nearest and
 // per-operation binary32. No FTZ/DAZ/exception flags or native ABI are claimed.
 // Math.Sqrt followed by binary32 rounding is the OWN platform projection of
 // SQRTSS; degenerate/overflow/nonfinite paths explicitly stop, not native miss.
 public static class GroundTriangleIntersectionContracts {
  static T Need<T>(T x,uint at)where T:class{if(x==null)throw new MissingContract(at,"Null own ground intersection view");return x;}
  static uint At(Invocation i,uint at){i.At=at;return at;}
  static float F(float x){return BitConverter.ToSingle(BitConverter.GetBytes(x),0);}
  static uint Bits(float x){return BitConverter.ToUInt32(BitConverter.GetBytes(x),0);}
  static float Float(uint x){return BitConverter.ToSingle(BitConverter.GetBytes(x),0);}
  static float Finite(float x,uint at){if(float.IsNaN(x)||float.IsInfinity(x))throw new MissingContract(at,"OWN finite intersection arithmetic only; native FP exceptional paths unasserted");return x;}
  static float Read(Displacement3 p,int axis,Invocation i,uint at){At(i,at);p=Need(p,at);return Finite((axis==0?p.X0:axis==1?p.Y4:p.Z8).Read(at),at);}
  static float Add(float a,float b,Invocation i,uint at){return Finite(F(a+b),At(i,at));}
  static float Sub(float a,float b,Invocation i,uint at){return Finite(F(a-b),At(i,at));}
  static float Mul(float a,float b,Invocation i,uint at){return Finite(F(a*b),At(i,at));}
  static float Div(float a,float b,Invocation i,uint at){return Finite(F(a/b),At(i,at));}
  static float PlaneRead(GroundPlane4 p,int axis,Invocation i,uint at){At(i,at);p=Need(p,at);return Finite((axis==0?p.X00:axis==1?p.Y04:axis==2?p.Z08:p.D0C).Read(at),at);}
  static float Infinity(GroundTriangleOperands o,Invocation i,uint at){At(i,at);uint b=Need(o,at).Infinity338458.Read(at);if(b!=0x7f800000u)throw new MissingContract(at,"OWN requires observed infinity operand");return Float(b);}
  static float Axis(Displacement3 p,int axis,Invocation i,uint x,uint y,uint z){return Read(p,axis,i,axis==0?x:axis==1?y:z);}
  static float XorSign(float v,GroundTriangleOperands o,Invocation i,uint at){At(i,at);uint m=Need(o,at).XorMask34C450.Read(at);if(m!=0x80000000u)throw new MissingContract(at,"OWN requires observed scalar XOR lane");return Float(Bits(v)^m);}
  public static GroundPlane4 Plane044660(GroundPlane4 output,Displacement3 a,Displacement3 b,Displacement3 c,GroundTriangleOperands operands,Invocation invocation){
   var i=Need(invocation,0x044664);
   float x2=Read(c,2,i,0x044664);x2=Sub(x2,Read(a,2,i,0x04466d),i,0x04466d);
   float x4=Read(c,0,i,0x044672),x3=Read(b,2,i,0x044677);x3=Sub(x3,Read(a,2,i,0x04467d),i,0x04467d);x4=Sub(x4,Read(a,0,i,0x044682),i,0x044682);
   float x5=Read(b,1,i,0x044686);x5=Sub(x5,Read(a,1,i,0x04468c),i,0x04468c);
   float x6=Read(b,0,i,0x044696);x6=Sub(x6,Read(a,0,i,0x04469b),i,0x04469b);float x7=x2,x8=x4;
   float x9=Read(c,1,i,0x0446b6);x9=Sub(x9,Read(a,1,i,0x0446bc),i,0x0446bc);
   x8=Mul(x8,x3,i,0x0446c2);x2=Mul(x2,x6,i,0x0446c7);float x0=x9;x7=Mul(x7,x5,i,0x0446cf);x8=Sub(x8,x2,i,0x0446d3);x0=Mul(x0,x3,i,0x0446d8);
   At(i,0x0446dc);x3=Float(Need(operands,i.At).One34BB44.Read(i.At));if(Bits(x3)!=0x3f800000u)throw new MissingContract(i.At,"OWN requires explicit observed one operand");
   x9=Mul(x9,x6,i,0x0446e4);x7=Sub(x7,x0,i,0x0446ee);x2=x8;x4=Mul(x4,x5,i,0x0446f6);x2=Mul(x2,x8,i,0x0446fa);x0=x7;x9=Sub(x9,x4,i,0x044702);
   x0=Mul(x0,x7,i,0x044707);float x1=x9;x2=Add(x2,x0,i,0x04470f);x1=Mul(x1,x9,i,0x044713);x2=Add(x2,x1,i,0x04471b);
   x0=Finite(F((float)Math.Sqrt(x2)),At(i,0x04471f));x3=Div(x3,x0,i,0x044723);
   x8=Mul(x8,x3,i,0x044727);x7=Mul(x7,x3,i,0x04472c);
   At(i,0x044730);Need(output,i.At).Y04.Write(x8);x9=Mul(x9,x3,i,0x044736);
   At(i,0x04473b);output.X00.Write(x7);At(i,0x04473f);output.Z08.Write(x9);
   // Actual post-store rereads preserve supported exact-cell aliases with A.
   x8=Mul(x8,Read(a,1,i,0x044745),i,0x044745);x7=Mul(x7,Read(a,0,i,0x04474b),i,0x04474b);x9=Mul(x9,Read(a,2,i,0x04474f),i,0x04474f);
   x8=Add(x8,x7,i,0x044755);x8=Add(x8,x9,i,0x04475f);At(i,0x044769);uint mask=operands.XorMask34C450.Read(i.At);
   if(mask!=0x80000000u)throw new MissingContract(i.At,"OWN requires explicit observed scalar XOR lane");x8=Float(Bits(x8)^mask);
   At(i,0x044771);output.D0C.Write(x8);return output;
  }
  public static float PlaneRay051B30(GroundPlane4 plane,GroundRay24 ray,GroundTriangleOperands operands,Invocation invocation){
   var i=Need(invocation,0x051b34);float nx=PlaneRead(plane,0,i,0x051b34),ny=PlaneRead(plane,1,i,0x051b38);
   At(i,0x051b40);float x0=Mul(nx,Read(Need(ray,i.At).Direction0C,0,i,i.At),i,0x051b40);
   float x3=Mul(ny,Read(ray.Direction0C,1,i,0x051b48),i,0x051b48);float x1=PlaneRead(plane,2,i,0x051b4d);
   x1=Mul(x1,Read(ray.Direction0C,2,i,0x051b52),i,0x051b52);x3=Add(x3,x0,i,0x051b57);x3=Add(x3,x1,i,0x051b63);
   At(i,0x051b67);if(x3==0)return Infinity(operands,i,0x051b6c); // Native also returns Inf for unordered; OWN finite domain stops earlier.
   At(i,0x051b7e);var ox=ray.Origin00.X0.Snapshot();var oy=ray.Origin00.Y4.Snapshot();At(i,0x051b82);var oz=ray.Origin00.Z8.Snapshot();
   At(i,0x051b8a);float x2=Finite(oy.Read(i.At),i.At);At(i,0x051b90);x0=Finite(ox.Read(i.At),i.At);
   x2=Mul(x2,ny,i,0x051b95);At(i,0x051b9d);x1=Finite(oz.Read(i.At),i.At);
   x1=Mul(x1,PlaneRead(plane,2,i,0x051ba3),i,0x051ba3);x0=Mul(x0,nx,i,0x051ba8);x2=Add(x2,x0,i,0x051bac);x2=Add(x2,x1,i,0x051bb0);
   x2=Add(x2,PlaneRead(plane,3,i,0x051bb4),i,0x051bb4);x2=Div(x2,x3,i,0x051bb9);x2=XorSign(x2,operands,i,0x051bbd);
   At(i,0x051bc4);if(x2<0)return Infinity(operands,i,0x051b6c);return x2;
  }
  // This native body never dereferences Terrain/this; only face and point.
  public static bool PointInFace1CF420(GroundIntersectionFace face,Displacement3 point,GroundTriangleOperands operands,Invocation invocation){
   var i=Need(invocation,0x1cf424);At(i,0x1cf424);uint mask=Need(operands,i.At).AbsMask34C410.Read(i.At);if(mask!=0x7fffffffu)throw new MissingContract(i.At,"OWN requires observed ABS mask");
   At(i,0x1cf431);var f=Need(face,i.At);float ax=Float(Bits(PlaneRead(f.Plane24,0,i,i.At))&mask),ay=Float(Bits(PlaneRead(f.Plane24,1,i,0x1cf439))&mask),az=Float(Bits(PlaneRead(f.Plane24,2,i,0x1cf441))&mask);
   int j=2,k=1;if(ax>=ay&&ax>=az){}else if(ay>=ax&&ay>=az)k=0;else j=0; // X,Y,Z tie priority; (j,k)=(Z,Y),(Z,X),(X,Y).
   float x4=Axis(f.B0C,j,i,0x1cf4b8,0x1cf4b1,0x1cf4aa),x6=Axis(f.A00,j,i,0x1cf4d4,0x1cf4cd,0x1cf4c6);
   float x7=Axis(f.C18,k,i,0x1cf4f1,0x1cf4ea,0x1cf4e3),x5=Axis(f.A00,k,i,0x1cf50f,0x1cf508,0x1cf501);
   float x2=Axis(f.C18,j,i,0x1cf52a,0x1cf523,0x1cf51c),x3=Axis(f.A00,j,i,0x1cf546,0x1cf53f,0x1cf538);
   float x0=Axis(f.B0C,k,i,0x1cf563,0x1cf55c,0x1cf555),x1=Axis(f.A00,k,i,0x1cf581,0x1cf57a,0x1cf573);
   x7=Sub(x7,x5,i,0x1cf585);x4=Sub(x4,x6,i,0x1cf589);x0=Sub(x0,x1,i,0x1cf58d);x2=Sub(x2,x3,i,0x1cf591);x7=Mul(x7,x4,i,0x1cf595);x0=Mul(x0,x2,i,0x1cf599);x7=Sub(x7,x0,i,0x1cf59d);
   x5=Axis(f.B0C,j,i,0x1cf5b8,0x1cf5b1,0x1cf5aa);x6=Axis(point,j,i,0x1cf5d6,0x1cf5ce,0x1cf5c6);
   float x8=Axis(f.C18,k,i,0x1cf5f6,0x1cf5ee,0x1cf5e6);x4=Axis(point,k,i,0x1cf617,0x1cf60f,0x1cf607);
   x2=Axis(f.C18,j,i,0x1cf633,0x1cf62c,0x1cf625);x3=Axis(point,j,i,0x1cf651,0x1cf649,0x1cf641);
   x0=Axis(f.B0C,k,i,0x1cf66f,0x1cf668,0x1cf661);x1=Axis(point,k,i,0x1cf68f,0x1cf687,0x1cf67f);
   At(i,0x1cf694);float x9=Float(operands.One34BB44.Read(i.At));if(Bits(x9)!=0x3f800000u)throw new MissingContract(i.At,"OWN requires observed one operand");
   x8=Sub(x8,x4,i,0x1cf69d);float x11=x9;x5=Sub(x5,x6,i,0x1cf6a6);x11=Div(x11,x7,i,0x1cf6aa);
   x8=Mul(x8,x5,i,0x1cf6af);x0=Sub(x0,x1,i,0x1cf6b4);x2=Sub(x2,x3,i,0x1cf6b8);x0=Mul(x0,x2,i,0x1cf6c0);x8=Sub(x8,x0,i,0x1cf6c4);x8=Mul(x8,x11,i,0x1cf6c9);At(i,0x1cf6ce);if(x8<0)return false;
   x5=Axis(point,j,i,0x1cf6f1,0x1cf6e9,0x1cf6e1);x7=Axis(f.A00,j,i,0x1cf70d,0x1cf706,0x1cf6ff);
   x0=Axis(f.C18,k,i,0x1cf72a,0x1cf723,0x1cf71c);x6=Axis(f.A00,k,i,0x1cf748,0x1cf741,0x1cf73a);
   x3=Axis(f.C18,j,i,0x1cf763,0x1cf75c,0x1cf755);x4=Axis(f.A00,j,i,0x1cf77f,0x1cf778,0x1cf771);
   x1=Axis(point,k,i,0x1cf79e,0x1cf796,0x1cf78e);x2=Axis(f.A00,k,i,0x1cf7bc,0x1cf7b5,0x1cf7ae);
   x0=Sub(x0,x6,i,0x1cf7c0);x5=Sub(x5,x7,i,0x1cf7c4);x1=Sub(x1,x2,i,0x1cf7c8);x3=Sub(x3,x4,i,0x1cf7cc);x0=Mul(x0,x5,i,0x1cf7d0);x1=Mul(x1,x3,i,0x1cf7d4);x0=Sub(x0,x1,i,0x1cf7d8);x0=Mul(x0,x11,i,0x1cf7dc);
   At(i,0x1cf7e1);if(x0<0)return false;x0=Add(x0,x8,i,0x1cf7e7);x9=Sub(x9,x0,i,0x1cf7ec);At(i,0x1cf7f1);return x9>=0;
  }
  public static float FaceIntersection1CF830(GroundIntersectionFace face,GroundRay24 ray,GroundTriangleOperands operands,Invocation invocation){
   var i=Need(invocation,0x1cf850);At(i,0x051b34);float t=PlaneRay051B30(Need(face,i.At).Plane24,ray,operands,i);
   float inf=Infinity(operands,i,0x1cf859);if(t>=inf)return Infinity(operands,i,0x1cf96d);
   float x=Mul(t,Read(ray.Direction0C,0,i,0x1cf86d),i,0x1cf86d),y=Mul(t,Read(ray.Direction0C,1,i,0x1cf87c),i,0x1cf87c),z=Mul(t,Read(ray.Direction0C,2,i,0x1cf882),i,0x1cf882);
   x=Add(x,Read(ray.Origin00,0,i,0x1cf888),i,0x1cf888);y=Add(y,Read(ray.Origin00,1,i,0x1cf88d),i,0x1cf88d);z=Add(z,Read(ray.Origin00,2,i,0x1cf893),i,0x1cf893);
   var point=new Displacement3();At(i,0x1cf89e);point.X0.Write(x);At(i,0x1cf8a4);point.Y4.Write(y);At(i,0x1cf8aa);point.Z8.Write(z);
   if(!PointInFace1CF420(face,point,operands,i))return Infinity(operands,i,0x1cf96d);
   float x2=Read(face.B0C,1,i,0x1cf8bd),x1=Read(face.B0C,2,i,0x1cf8c2);x1=Sub(x1,Read(face.C18,2,i,0x1cf8c7),i,0x1cf8c7);x2=Sub(x2,Read(face.A00,1,i,0x1cf8cc),i,0x1cf8cc);
   float x4=Read(face.B0C,1,i,0x1cf8d1);x4=Sub(x4,Read(face.C18,1,i,0x1cf8d6),i,0x1cf8d6);float x5=Read(face.B0C,0,i,0x1cf8db);x5=Sub(x5,Read(face.C18,0,i,0x1cf8e0),i,0x1cf8e0);
   float x3=x2,x6=Read(face.B0C,2,i,0x1cf8ed);x6=Sub(x6,Read(face.A00,2,i,0x1cf8f2),i,0x1cf8f2);float x7=Read(face.B0C,0,i,0x1cf8fc);x7=Sub(x7,Read(face.A00,0,i,0x1cf901),i,0x1cf901);
   x3=Mul(x3,x1,i,0x1cf905);float x0=x6;x2=Mul(x2,x5,i,0x1cf90c);x6=Mul(x6,x5,i,0x1cf910);x0=Mul(x0,x4,i,0x1cf914);x3=Sub(x3,x0,i,0x1cf918);x0=x7;
   x7=Mul(x7,x4,i,0x1cf91f);x0=Mul(x0,x1,i,0x1cf923);x3=Mul(x3,Read(ray.Direction0C,0,i,0x1cf92a),i,0x1cf92a);x7=Sub(x7,x2,i,0x1cf92f);x6=Sub(x6,x0,i,0x1cf933);
   x7=Mul(x7,Read(ray.Direction0C,2,i,0x1cf937),i,0x1cf937);x6=Mul(x6,Read(ray.Direction0C,1,i,0x1cf93c),i,0x1cf93c);x6=Add(x6,x3,i,0x1cf941);x6=Add(x6,x7,i,0x1cf945);
   At(i,0x1cf94e);return x6<0?Infinity(operands,i,0x1cf96d):t;
  }
  static uint Index(ulong v,Invocation i,uint at){At(i,at);if(v>uint.MaxValue)throw new MissingContract(at,"OWN unsupported low32 index wrap");return (uint)v;}
  static float Cell(Field<OwnGroundCells> captured,uint index,Invocation i,uint at){At(i,at);return Finite(Float(Need(captured.Read(at),at).Read(index,at)),at);}
  static void XYZ(Displacement3 p,float x,float y,float z){p.X0.Write(x);p.Y4.Write(y);p.Z8.Write(z);}
  static void CopyPlane(GroundPlane4 to,GroundPlane4 from){to.X00.CopyFrom(from.X00);to.Y04.CopyFrom(from.Y04);to.Z08.CopyFrom(from.Z08);to.D0C.CopyFrom(from.D0C);}
  static void Candidate(float t,GroundIntersectionResult output,uint x,uint z,Invocation i,uint compare,uint distance,uint xx,uint zz){
   At(i,compare);var r=Need(output,compare);float prior=r.Distance.Read(compare);if(float.IsNaN(prior))throw new MissingContract(compare,"OWN best distance NaN unsupported; native unordered branch would update");
   if(t<prior){At(i,distance);r.Distance.Write(t);At(i,xx);r.X.Write(x);At(i,zz);r.Z.Write(z);}
  }
  // Actual ground triangles, not a bilinear-height or navigation-Y substitute.
  // Bounds are inclusive quad coordinates. No native clamps exist here: OWN
  // rejects wrap/out-of-buffer domains and limits this call to65536 quads.
  public static void Sweep1DF220(OwnGroundData data,GroundRay24 ray,uint xBegin,uint zBegin,uint xEnd,uint zEnd,GroundIntersectionResult output,GroundTriangleOperands operands,Invocation invocation){
   var i=Need(invocation,0x1df24c);At(i,0x1df24c);if(zBegin>zEnd)return;At(i,0x1df2c2);
   // Even with no inner X iterations, native EDI wraps at UINT_MAX and keeps
   // traversing Z. Reject that domain before the otherwise safe empty-X exit.
   if(zEnd==uint.MaxValue)throw new MissingContract(i.At,"OWN unsupported inclusive Z loop wrap");if(xBegin>xEnd)return;
   if(xEnd==uint.MaxValue||((ulong)xEnd-xBegin+1)*((ulong)zEnd-zBegin+1)>65536)throw new MissingContract(i.At,"OWN finite inclusive quad range limit");
   var face=new GroundIntersectionFace();var plane0=new GroundPlane4();var plane1=new GroundPlane4();
   for(uint z=zBegin;z<=zEnd;z++){
    uint nz=z+1;float fz=F((float)(long)z),fnz=F((float)(long)nz);
    for(uint x=xBegin;x<=xEnd;x++){
     At(i,0x1df2f0);var captured=Need(data,i.At).Cells128.Snapshot();uint nx=x+1;At(i,0x1df2fb);uint width=data.Dimension60.Read(i.At);
     uint row1=Index((ulong)width*nz,i,0x1df306),row0=Index((ulong)width*z,i,0x1df30d);float fx=F((float)(long)x),fnx=F((float)(long)nx);
     uint i01=Index((ulong)row1+x,i,0x1df324),i00=Index((ulong)row0+x,i,0x1df326);
     if(width<2||nx>=width)throw new MissingContract(At(i,0x1df32f),"OWN quad crosses declared row extent; native has no matching guard");
     float h01=Cell(captured,i01,i,0x1df32f),h00=Cell(captured,i00,i,0x1df335);uint i11=Index((ulong)i01+1,i,0x1df340);float h11=Cell(captured,i11,i,0x1df34e);uint i10=Index((ulong)i00+1,i,0x1df354);float h10=Cell(captured,i10,i,0x1df357);
     XYZ(face.A00,fx,h00,fz);XYZ(face.B0C,fx,h01,fnz);XYZ(face.C18,fnx,h11,fnz);
     At(i,0x1df3ba);var returned=Plane044660(plane0,face.A00,face.B0C,face.C18,operands,i);At(i,0x1df3ca);CopyPlane(face.Plane24,returned);
     float candidate=FaceIntersection1CF830(face,ray,operands,i);Candidate(candidate,output,x,z,i,0x1df3d7,0x1df3dc,0x1df3e0,0x1df3e3);
     // Same face identity and cached four corners; do not resample between faces.
     XYZ(face.A00,fnx,h11,fnz);XYZ(face.B0C,fnx,h10,fz);XYZ(face.C18,fx,h00,fz);
     At(i,0x1df428);returned=Plane044660(plane1,face.A00,face.B0C,face.C18,operands,i);At(i,0x1df43a);CopyPlane(face.Plane24,returned);
     candidate=FaceIntersection1CF830(face,ray,operands,i);Candidate(candidate,output,x,z,i,0x1df447,0x1df44c,0x1df450,0x1df453);
    }
   }
   At(i,0x1df4d9);
  }
 }
}
