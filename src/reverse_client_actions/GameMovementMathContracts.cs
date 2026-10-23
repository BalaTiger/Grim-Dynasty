using System;

namespace GrimDynasty.ReverseClient.Actions {
 // Own contiguous typed 48-byte view. Its twelve cells start Unknown. A copied
 // view shares both cells and lifetime, never synchronized value mirrors.
 public sealed class GameMovementMatrix48 {
  sealed class Lifetime { public bool Released; }
  readonly Field<float>[] cells;
  readonly Lifetime life;
  public readonly object AddressIdentity;
  public GameMovementMatrix48():this(NewCells()){}
  public GameMovementMatrix48(Field<float>[] sharedCells){
   if(sharedCells==null||sharedCells.Length!=12)throw new ArgumentException("Exactly twelve cells required");
   cells=(Field<float>[])sharedCells.Clone();
   for(int n=0;n<12;n++){
    if(cells[n]==null)throw new ArgumentException("Null matrix cell");
    for(int k=0;k<n;k++)if(Object.ReferenceEquals(cells[n],cells[k]))throw new ArgumentException("A contiguous own matrix cannot repeat a cell");
   }
   life=new Lifetime();AddressIdentity=new object();
  }
  public GameMovementMatrix48(GameMovementMatrix48 sameAddress){
   if(sameAddress==null)throw new ArgumentNullException("sameAddress");
   cells=sameAddress.cells;life=sameAddress.life;AddressIdentity=sameAddress.AddressIdentity;
  }
  static Field<float>[] NewCells(){var a=new Field<float>[12];for(int n=0;n<12;n++)a[n]=new Field<float>();return a;}
  public Field<float>[] Cells { get{return (Field<float>[])cells.Clone();} }
  public Field<float> At(int index,uint at){RequireLive(at);if(index<0||index>=12)throw new MissingContract(at,"Outside own48-byte extent");return cells[index];}
  public void RequireLive(uint at){if(life.Released)throw new MissingContract(at,"Own matrix explicitly released");}
  public void MarkReleased(){life.Released=true;}
 }
 public sealed class GameMovementMathConstants {
  public readonly Field<float> One7A8054=new Field<float>();
  // Saved exact bytes0000803f in r1-position-publication-binding. This is
  // explicit own disk-constant setup, not observation of loaded mutable memory.
  public void UseObservedDiskOne(){One7A8054.Write(BitConverter.ToSingle(BitConverter.GetBytes(0x3f800000u),0));}
 }

 // Game591B80 and07E6F0, real Rea triplets in r1-game-movement-math.
 // Own domain: live separate output/input local buffers; shared typed cell
 // overlap is rejected before native steps. Shifted/cross-type byte aliases,
 // concurrent mutation, native allocation/ABI/EH and unmasked FP exceptions
 // are excluded. Every scalar operation rounds to binary32. SQRT uses the
 // platform double square root rounded to float; exact native MXCSR FTZ/DAZ,
 // alternative rounding modes and NaN payload selection are not claimed.
 // MOV snapshots preserve Unknown; arithmetic consumes it at its instruction.
 // No normalized-zero fallback: native divide-by-zero/NaN results propagate.
 public static class GameMovementMathContracts {
  static float F(float x){return BitConverter.ToSingle(BitConverter.GetBytes(x),0);}
  static Invocation Need(Invocation i,uint at){if(i==null)throw new MissingContract(at,"Missing own invocation");i.At=at;return i;}
  static Field<float> P(NavigationPoint3 p,int n,uint at){
   if(p==null)throw new MissingContract(at,"Null point outside own local-buffer domain");p.RequireLive(at);
   try{return n==0?p.X:n==1?p.Y:p.Z;}catch(MissingContract e){throw new MissingContract(at,"Point outside declared own extent",e);}
  }
  static Field<float> L(Field<float> cell,uint at,Invocation i){i.At=at;return cell.Snapshot();}
  static Field<float> M(Field<float> a,Field<float> b,uint at,Invocation i){i.At=at;float x=a.Read(at),y=b.Read(at);return Field<float>.Of(F(x*y));}
  static Field<float> A(Field<float> a,Field<float> b,uint at,Invocation i){i.At=at;float x=a.Read(at),y=b.Read(at);return Field<float>.Of(F(x+y));}
  static Field<float> S(Field<float> a,Field<float> b,uint at,Invocation i){i.At=at;float x=a.Read(at),y=b.Read(at);return Field<float>.Of(F(x-y));}
  static Field<float> D(Field<float> a,Field<float> b,uint at,Invocation i){i.At=at;float x=a.Read(at),y=b.Read(at);return Field<float>.Of(F(x/y));}
  static Field<float> Q(Field<float> a,uint at,Invocation i){i.At=at;return Field<float>.Of(F((float)Math.Sqrt(a.Read(at))));}
  static void W(GameMovementMatrix48 o,int n,Field<float> x,uint at,Invocation i){i.At=at;o.At(n,at).CopyFrom(x);}
  static void W2(GameMovementMatrix48 o,int n,Field<float> x,Field<float> y,uint at,Invocation i){
   // One native64-bit store; both source lanes captured before either write.
   var a=x.Snapshot();var b=y.Snapshot();i.At=at;o.At(n,at).CopyFrom(a);o.At(n+1,at).CopyFrom(b);
  }
  static void Output(GameMovementMatrix48 o,uint at){if(o==null)throw new MissingContract(at,"Null matrix output");o.RequireLive(at);}
  static void Separate(GameMovementMatrix48 o,Field<float>[] input,uint at){
   for(int k=0;k<12;k++)for(int n=0;n<input.Length;n++)if(Object.ReferenceEquals(o.At(k,at),input[n]))throw new MissingContract(at,"Output/input overlap is outside the observed separate-local-buffer domain");
  }
  static Field<float>[] PointCells(NavigationPoint3 p,uint at){return new[]{P(p,0,at),P(p,1,at),P(p,2,at)};}
  public static GameMovementMatrix48 Basis591B80(GameMovementMatrix48 o,NavigationPoint3 origin,
   NavigationPoint3 target,NavigationPoint3 up,GameMovementMathConstants constants,Invocation invocation){
   var i=Need(invocation,0x591b80);Output(o,i.At);Separate(o,PointCells(origin,i.At),i.At);Separate(o,PointCells(target,i.At),i.At);Separate(o,PointCells(up,i.At),i.At);
   if(constants==null)throw new MissingContract(i.At,"Missing own disk-constant view");
   // Exact register order, including non-XYZ input reads and intermediate stores.
   var x2=L(P(target,0,0x591b84),0x591b84,i);x2=S(x2,P(origin,0,0x591b89),0x591b89,i);
   var x0=L(P(target,2,0x591b8d),0x591b8d,i);x0=S(x0,P(origin,2,0x591b93),0x591b93,i);
   var x1=L(P(target,1,0x591b98),0x591b98,i);x1=S(x1,P(origin,1,0x591b9e),0x591b9e,i);
   var tail=x0.Snapshot();W2(o,6,x2,x1,0x591bbd,i);W(o,8,tail,0x591bc2,i);
   var x7=L(P(up,1,0x591bc5),0x591bc5,i);x1=L(P(up,2,0x591bcb),0x591bcb,i);x2=L(o.At(6,0x591bd1),0x591bd1,i);
   x0=M(x1,o.At(7,0x591bd9),0x591bd9,i);var x5=x2.Snapshot();var x3=L(P(up,0,0x591be1),0x591be1,i);
   x2=M(x2,x7,0x591be6,i);x5=M(x5,x1,0x591bea,i);var x8=M(x7,o.At(8,0x591bfe),0x591bfe,i);
   var x9=L(constants.One7A8054,0x591c04,i);x8=S(x8,x0,0x591c0d,i);x0=M(x3,o.At(8,0x591c15),0x591c15,i);
   x3=M(x3,o.At(7,0x591c1a),0x591c1a,i);x5=S(x5,x0,0x591c1f,i);x3=S(x3,x2,0x591c27,i);
   W2(o,0,x8,x5,0x591c2e,i);W(o,2,x3,0x591c3c,i);
   var x6=L(o.At(0,0x591c3f),0x591c3f,i);x7=L(o.At(2,0x591c43),0x591c43,i);x3=x6.Snapshot();
   x8=L(o.At(1,0x591c4b),0x591c4b,i);x5=x7.Snapshot();x2=L(o.At(6,0x591c54),0x591c54,i);
   x0=M(x8,o.At(8,0x591c5d),0x591c5d,i);x1=x7.Snapshot();x5=M(x5,o.At(7,0x591c65),0x591c65,i);
   x3=M(x3,o.At(8,0x591c6a),0x591c6a,i);x5=S(x5,x0,0x591c6f,i);x1=M(x1,x7,0x591c73,i);x0=x2.Snapshot();
   x2=M(x2,x8,0x591c7a,i);x0=M(x0,x7,0x591c7f,i);x3=S(x3,x0,0x591c83,i);
   x0=M(x6,o.At(7,0x591c8a),0x591c8a,i);x2=S(x2,x0,0x591c8f,i);W2(o,3,x5,x3,0x591c99,i);
   x0=M(x8,x8,0x591ca2,i);tail=x2.Snapshot();x2=M(x6,x6,0x591cb4,i);W(o,5,tail,0x591cb8,i);
   x2=A(x2,x0,0x591cbb,i);x2=A(x2,x1,0x591cc2,i);x0=Q(x2,0x591cca,i);x1=D(x9,x0,0x591cce,i);
   x6=M(x6,x1,0x591cd2,i);x7=M(x7,x1,0x591cd6,i);x8=M(x8,x1,0x591cdd,i);
   W2(o,0,x6,x8,0x591cff,i);W(o,2,x7,0x591d03,i);
   x5=L(o.At(3,0x591d06),0x591d06,i);x6=L(o.At(4,0x591d0b),0x591d0b,i);x2=x5.Snapshot();
   var x4=L(o.At(5,0x591d13),0x591d13,i);x0=M(x6,x6,0x591d1b,i);x1=x4.Snapshot();
   x2=M(x2,x5,0x591d22,i);x1=M(x1,x4,0x591d26,i);x2=A(x2,x0,0x591d2a,i);x2=A(x2,x1,0x591d31,i);
   x0=Q(x2,0x591d35,i);x3=D(x9,x0,0x591d39,i);x5=M(x5,x3,0x591d3d,i);x4=M(x4,x3,0x591d41,i);x6=M(x6,x3,0x591d45,i);
   W2(o,3,x5,x6,0x591d5e,i);W(o,5,x4,0x591d63,i);
   x4=L(o.At(6,0x591d66),0x591d66,i);x5=L(o.At(7,0x591d6b),0x591d6b,i);x2=x4.Snapshot();x3=L(o.At(8,0x591d73),0x591d73,i);
   x0=M(x5,x5,0x591d7b,i);x1=x3.Snapshot();x2=M(x2,x4,0x591d82,i);x1=M(x1,x3,0x591d86,i);
   x2=A(x2,x0,0x591d8a,i);x2=A(x2,x1,0x591d91,i);x0=Q(x2,0x591d95,i);x9=D(x9,x0,0x591d99,i);
   x4=M(x4,x9,0x591d9e,i);x5=M(x5,x9,0x591da3,i);x3=M(x3,x9,0x591da8,i);
   W2(o,6,x4,x5,0x591db9,i);W(o,8,x3,0x591dc8,i);
   var ox=L(P(origin,0,0x591dcb),0x591dcb,i);var oy=L(P(origin,1,0x591dcb),0x591dcb,i);W2(o,9,ox,oy,0x591dcf,i);
   var oz=L(P(origin,2,0x591dd4),0x591dd4,i);W(o,11,oz,0x591dd7,i);i.At=0x591dda;return o;
  }
  //07E6F0 body follows below. No table-driven algebra or library multiplication:
  // scalar register order is preserved, including D7/D10's different sum order.
  public static GameMovementMatrix48 Compose07E6F0(GameMovementMatrix48 o,GameMovementMatrix48 a,GameMovementMatrix48 b,Invocation invocation){
   var i=Need(invocation,0x07e6f0);Output(o,i.At);if(a==null||b==null)throw new MissingContract(i.At,"Null input matrix");a.RequireLive(i.At);b.RequireLive(i.At);Separate(o,a.Cells,i.At);Separate(o,b.Cells,i.At);
   Field<float> x0=null,x1=null,x2=null,x3=null;
   x2=L(a.At(0,0x07e6f0),0x07e6f0,i);
   x2=M(x2,b.At(0,0x07e6f4),0x07e6f4,i);
   x0=L(b.At(1,0x07e6f9),0x07e6f9,i);
   x0=M(x0,a.At(3,0x07e6ff),0x07e6ff,i);
   x1=L(b.At(2,0x07e704),0x07e704,i);
   x1=M(x1,a.At(6,0x07e70a),0x07e70a,i);
   x2=A(x2,x0,0x07e70f,i);
   x2=A(x2,x1,0x07e713,i);
   W(o,0,x2,0x07e717,i);
   x2=L(a.At(1,0x07e71b),0x07e71b,i);
   x2=M(x2,b.At(0,0x07e720),0x07e720,i);
   x0=L(a.At(4,0x07e725),0x07e725,i);
   x0=M(x0,b.At(1,0x07e72a),0x07e72a,i);
   x1=L(a.At(7,0x07e730),0x07e730,i);
   x1=M(x1,b.At(2,0x07e735),0x07e735,i);
   x2=A(x2,x0,0x07e73b,i);
   x2=A(x2,x1,0x07e73f,i);
   W(o,1,x2,0x07e743,i);
   x2=L(a.At(2,0x07e748),0x07e748,i);
   x2=M(x2,b.At(0,0x07e74d),0x07e74d,i);
   x0=L(a.At(5,0x07e752),0x07e752,i);
   x0=M(x0,b.At(1,0x07e757),0x07e757,i);
   x1=L(a.At(8,0x07e75d),0x07e75d,i);
   x1=M(x1,b.At(2,0x07e762),0x07e762,i);
   x2=A(x2,x0,0x07e768,i);
   x2=A(x2,x1,0x07e76c,i);
   W(o,2,x2,0x07e770,i);
   x2=L(b.At(4,0x07e775),0x07e775,i);
   x2=M(x2,a.At(3,0x07e77b),0x07e77b,i);
   x0=L(b.At(3,0x07e780),0x07e780,i);
   x0=M(x0,a.At(0,0x07e786),0x07e786,i);
   x1=L(b.At(5,0x07e78a),0x07e78a,i);
   x1=M(x1,a.At(6,0x07e790),0x07e790,i);
   x2=A(x2,x0,0x07e795,i);
   x2=A(x2,x1,0x07e799,i);
   W(o,3,x2,0x07e79d,i);
   x3=L(a.At(4,0x07e7a2),0x07e7a2,i);
   x3=M(x3,b.At(4,0x07e7a7),0x07e7a7,i);
   x0=L(a.At(1,0x07e7ad),0x07e7ad,i);
   x0=M(x0,b.At(3,0x07e7b2),0x07e7b2,i);
   x1=L(a.At(7,0x07e7b8),0x07e7b8,i);
   x1=M(x1,b.At(5,0x07e7bd),0x07e7bd,i);
   x3=A(x3,x0,0x07e7c3,i);
   x3=A(x3,x1,0x07e7c7,i);
   W(o,4,x3,0x07e7cb,i);
   x2=L(a.At(5,0x07e7d0),0x07e7d0,i);
   x2=M(x2,b.At(4,0x07e7d5),0x07e7d5,i);
   x0=L(a.At(2,0x07e7db),0x07e7db,i);
   x0=M(x0,b.At(3,0x07e7e0),0x07e7e0,i);
   x1=L(a.At(8,0x07e7e6),0x07e7e6,i);
   x1=M(x1,b.At(5,0x07e7eb),0x07e7eb,i);
   x2=A(x2,x0,0x07e7f1,i);
   x2=A(x2,x1,0x07e7f5,i);
   W(o,5,x2,0x07e7f9,i);
   x2=L(a.At(0,0x07e7fe),0x07e7fe,i);
   x2=M(x2,b.At(6,0x07e802),0x07e802,i);
   x0=L(a.At(3,0x07e808),0x07e808,i);
   x0=M(x0,b.At(7,0x07e80d),0x07e80d,i);
   x1=L(a.At(6,0x07e813),0x07e813,i);
   x1=M(x1,b.At(8,0x07e818),0x07e818,i);
   x2=A(x2,x0,0x07e81e,i);
   x2=A(x2,x1,0x07e822,i);
   W(o,6,x2,0x07e826,i);
   x0=L(a.At(1,0x07e82b),0x07e82b,i);
   x0=M(x0,b.At(6,0x07e830),0x07e830,i);
   x1=L(a.At(7,0x07e836),0x07e836,i);
   x1=M(x1,b.At(8,0x07e83b),0x07e83b,i);
   x3=L(a.At(4,0x07e841),0x07e841,i);
   x3=M(x3,b.At(7,0x07e846),0x07e846,i);
   x3=A(x3,x0,0x07e84c,i);
   x3=A(x3,x1,0x07e850,i);
   W(o,7,x3,0x07e854,i);
   x2=L(a.At(5,0x07e859),0x07e859,i);
   x2=M(x2,b.At(7,0x07e85e),0x07e85e,i);
   x0=L(a.At(2,0x07e864),0x07e864,i);
   x0=M(x0,b.At(6,0x07e869),0x07e869,i);
   x1=L(a.At(8,0x07e86f),0x07e86f,i);
   x1=M(x1,b.At(8,0x07e874),0x07e874,i);
   x2=A(x2,x0,0x07e87a,i);
   x2=A(x2,x1,0x07e87e,i);
   W(o,8,x2,0x07e882,i);
   x2=L(b.At(10,0x07e887),0x07e887,i);
   // 0x07e88d captures the same output identity; subsequent stores may still fail.
   x2=M(x2,a.At(3,0x07e890),0x07e890,i);
   x0=L(b.At(9,0x07e895),0x07e895,i);
   x0=M(x0,a.At(0,0x07e89b),0x07e89b,i);
   x1=L(b.At(11,0x07e89f),0x07e89f,i);
   x1=M(x1,a.At(6,0x07e8a5),0x07e8a5,i);
   x2=A(x2,x0,0x07e8aa,i);
   x2=A(x2,x1,0x07e8ae,i);
   x2=A(x2,a.At(9,0x07e8b2),0x07e8b2,i);
   W(o,9,x2,0x07e8b7,i);
   x0=L(b.At(9,0x07e8bc),0x07e8bc,i);
   x0=M(x0,a.At(1,0x07e8c2),0x07e8c2,i);
   x1=L(a.At(7,0x07e8c7),0x07e8c7,i);
   x1=M(x1,b.At(11,0x07e8cc),0x07e8cc,i);
   x3=L(b.At(10,0x07e8d2),0x07e8d2,i);
   x3=M(x3,a.At(4,0x07e8d8),0x07e8d8,i);
   x3=A(x3,x0,0x07e8dd,i);
   x3=A(x3,x1,0x07e8e1,i);
   x3=A(x3,a.At(10,0x07e8e5),0x07e8e5,i);
   W(o,10,x3,0x07e8ea,i);
   x2=L(b.At(10,0x07e8ef),0x07e8ef,i);
   x2=M(x2,a.At(5,0x07e8f5),0x07e8f5,i);
   x0=L(b.At(9,0x07e8fa),0x07e8fa,i);
   x0=M(x0,a.At(2,0x07e900),0x07e900,i);
   x1=L(a.At(8,0x07e905),0x07e905,i);
   x1=M(x1,b.At(11,0x07e90a),0x07e90a,i);
   x2=A(x2,x0,0x07e910,i);
   x2=A(x2,x1,0x07e914,i);
   x2=A(x2,a.At(11,0x07e918),0x07e918,i);
   W(o,11,x2,0x07e91d,i);
   i.At=0x07e922;return o;
  }
  public static GameMovementMatrix48 AxisRotation591A10(GameMovementMatrix48 output,NavigationPoint3 axis,float angle,Invocation invocation){
   var i=Need(invocation,0x591a30);throw new MissingContract(i.At,"cosf5DC392 then sinf5DC40A platform contracts are unclosed; no Math(double) substitution");
  }
 }
}
