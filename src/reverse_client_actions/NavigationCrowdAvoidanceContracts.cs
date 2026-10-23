using System;

namespace GrimDynasty.ReverseClient.Actions {
 // OWN stride0x40 storage. Every cell starts unknown. Record accesses use these
 // exact cells; neither the records nor Group.Vector80's header are mirrored.
 public sealed class NavigationCrowdAvoidanceBuffer:NavigationView {
  public readonly Field<float>[] Cells;
  public NavigationCrowdAvoidanceBuffer(int capacity){if(capacity<0||capacity>Int32.MaxValue/16)throw new ArgumentOutOfRangeException("capacity");Cells=new Field<float>[capacity*16];for(int n=0;n<Cells.Length;n++)Cells[n]=new Field<float>();}
  public NavigationCrowdAvoidanceBuffer(Field<float>[] cells){if(cells==null||cells.Length%16!=0)throw new ArgumentException("Whole stride0x40 records required");Cells=(Field<float>[])cells.Clone();for(int n=0;n<Cells.Length;n++)if(Cells[n]==null)throw new ArgumentException("Null scalar cell");}
  public Field<float> Cell(long record,uint offset,uint at){RequireLive(at);if((offset&3)!=0||offset>0x3c||record<0||record>=Cells.Length/16)throw new MissingContract(at,"Outside declared own stride0x40 record extent");return Cells[(int)record*16+(int)(offset/4)];}
 }
 public sealed class NavigationCrowdAvoidanceAddress {
  public readonly NavigationCrowdAvoidanceBuffer Buffer;public readonly long Record;
  public NavigationCrowdAvoidanceAddress(NavigationCrowdAvoidanceBuffer buffer,long record){if(buffer==null)throw new ArgumentNullException("buffer");Buffer=buffer;Record=record;}
  public Field<float> Cell(long delta,uint offset,uint at){return Buffer.Cell(checked(Record+delta),offset,at);}
 }
 public sealed class NavigationCrowdAvoidanceVector {
  public readonly NavigationCrowdOpaqueVector Header;
  public readonly Field<object> Begin00,End08,Capacity10;
  public NavigationCrowdAvoidanceVector(NavigationCrowdOpaqueVector header){if(header==null)throw new ArgumentNullException("header");Header=header;Begin00=header.Begin00;End08=header.End08;Capacity10=header.Capacity10;}
  // Explicit OWN storage installation, not reconstructed native allocation.
  public void InstallOwnStorage(NavigationCrowdAvoidanceBuffer buffer,int count){if(buffer==null)throw new ArgumentNullException("buffer");if(count<0||count>buffer.Cells.Length/16)throw new ArgumentOutOfRangeException("count");Begin00.Write(new NavigationCrowdAvoidanceAddress(buffer,0));End08.Write(new NavigationCrowdAvoidanceAddress(buffer,count));Capacity10.Write(new NavigationCrowdAvoidanceAddress(buffer,buffer.Cells.Length/16));}
 }
 public sealed class NavigationCrowdAvoidanceConstants {
  public readonly Field<float> One34BB44=new Field<float>(),Two34BBD8=new Field<float>(),Half34BADC=new Field<float>(),SideThreshold34B9E0=new Field<float>();
  public readonly Field<uint> SignMask34C450=new Field<uint>();
  public readonly Field<float> Horizon34BC00=new Field<float>(),NegativeHalf34BDE8=new Field<float>(),Epsilon34B984=new Field<float>();
  public readonly Field<float> Ten34BC70=new Field<float>(),ThreeQuarter34BB08=new Field<float>(),TimeScale34BAB8=new Field<float>(),TimeBias34BA34=new Field<float>();
  public readonly Field<float> Third34BA9C=new Field<float>(),HalfStep34BAD0=new Field<float>(),Step34BB18=new Field<float>(),Quarter34BA80=new Field<float>(),MaxFinite34BDCC=new Field<float>(),SpeedTolerance34B9B0=new Field<float>();
  // BSS has no observed disk bytes. Keep all three cells unknown unless the
  // caller explicitly supplies OWN state; UseObservedDiskConstants never does.
  public readonly NavigationPoint3 Fallback422980=new NavigationPoint3();
  // Explicit OWN cold configuration using observed disk bits. No loaded
  // original globals or BSS422980/4228C0 are inferred/initialized here.
  public void UseObservedDiskConstants(){
   One34BB44.Write(NavigationCoreContracts.FromBits(0x3f800000));Two34BBD8.Write(NavigationCoreContracts.FromBits(0x40000000));Half34BADC.Write(NavigationCoreContracts.FromBits(0x3f000000));
   SideThreshold34B9E0.Write(NavigationCoreContracts.FromBits(0x3c23d70a));SignMask34C450.Write(0x80000000);
   Horizon34BC00.Write(NavigationCoreContracts.FromBits(0x40200000));NegativeHalf34BDE8.Write(NavigationCoreContracts.FromBits(0xbf000000));Epsilon34B984.Write(NavigationCoreContracts.FromBits(0x38d1b717));
   Ten34BC70.Write(NavigationCoreContracts.FromBits(0x41200000));ThreeQuarter34BB08.Write(NavigationCoreContracts.FromBits(0x3f400000));TimeScale34BAB8.Write(NavigationCoreContracts.FromBits(0x3ecccccd));TimeBias34BA34.Write(NavigationCoreContracts.FromBits(0x3dcccccd));
   Third34BA9C.Write(NavigationCoreContracts.FromBits(0x3eaaaaab));HalfStep34BAD0.Write(NavigationCoreContracts.FromBits(0x3ee5c8fb));Step34BB18.Write(NavigationCoreContracts.FromBits(0x3f65c8fb));Quarter34BA80.Write(NavigationCoreContracts.FromBits(0x3e800000));MaxFinite34BDCC.Write(NavigationCoreContracts.FromBits(0x7f7fffff));SpeedTolerance34B9B0.Write(NavigationCoreContracts.FromBits(0x3a83126f));
  }
 }
 // Engine SHA4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153.
 // Sources: r1-crowd-motion-branches/20 (25F250),
 // r1-crowd-direct-algorithms/11 (25EF20), r1-crowd-tick-operands/inspection.
 // The25EF20 pseudocode omits the scoring tail; assembly is authoritative here.
 // Supported OWN math: finite normal/zero binary32, round-to-nearest scalar
 // operations, masked exceptions, stable single-thread storage. Nonfinite,
 // subnormal operands/intermediates and invalid arithmetic stop at consumption;
 // native NaN/FTZ/DAZ/trap behavior is NOT claimed. No custom success callbacks.
 // Preprocess is only25F250's record-write prefix. Select25F250 additionally
 // executes the22-candidate/three-refinement selector with the concrete OWN
 // system-UCRT crowd view. Actual tick, Game Moved/floor/publication remain
 // outside this component. Unknown BSS fallback is copied, never defaulted.
 public static class NavigationCrowdAvoidanceContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null own avoidance view");var v=value as NavigationView;if(v!=null)v.RequireLive(at);return value;}
  static Field<float> P(NavigationPoint3 p,int axis,uint at){Need(p,at);try{return axis==0?p.X:axis==1?p.Y:p.Z;}catch(MissingContract e){throw new MissingContract(at,e.Message,e);}}
  static float Scalar(float value,uint at){uint bits=NavigationCoreContracts.ToBits(value),exp=bits&0x7f800000u;if(exp==0x7f800000u||(exp==0&&(bits&0x7fffffu)!=0))throw new MissingContract(at,"Outside own finite normal/zero avoidance floating domain");return value;}
  static float R(Field<float> bits,Invocation i,uint at){i.At=at;return Scalar(bits.Read(at),at);}
  static float Round(double value,Invocation i,uint at){i.At=at;if(Double.IsNaN(value)||Double.IsInfinity(value)||(value!=0&&Math.Abs(value)<1.1754943508222875E-38))throw new MissingContract(at,"Outside own finite normal/zero avoidance arithmetic domain");return Scalar(NavigationCoreContracts.F((float)value),at);}
  static float Add(float a,float b,Invocation i,uint at){return Round((double)a+b,i,at);}static float Sub(float a,float b,Invocation i,uint at){return Round((double)a-b,i,at);}static float Mul(float a,float b,Invocation i,uint at){return Round((double)a*b,i,at);}static float Div(float a,float b,Invocation i,uint at){return Round((double)a/b,i,at);}static float Sqrt(float a,Invocation i,uint at){return Round(Math.Sqrt(a),i,at);}
  // SSE min/max choose the second operand on equality, including signed zero.
  static float Min(float a,float b,Invocation i,uint at){i.At=at;return a<b?a:b;}static float Max(float a,float b,Invocation i,uint at){i.At=at;return a>b?a:b;}
  static NavigationCrowdAvoidanceAddress Address(Field<object> bits,uint at){object value=bits.Read(at);if(value==null)return null;var a=value as NavigationCrowdAvoidanceAddress;if(a==null)throw new MissingContract(at,"Header is not a bound own stride0x40 address");return a;}
  static long Count(NavigationCrowdAvoidanceVector v,Invocation i,uint endAt,uint beginAt){
   i.At=endAt;var end=Need(v,endAt).End08.Snapshot();i.At=beginAt;var begin=v.Begin00.Snapshot();var e=Address(end,beginAt);var b=Address(begin,beginAt);
   if(e==null&&b==null)return 0;if(e==null||b==null||!Object.ReferenceEquals(e.Buffer,b.Buffer))throw new MissingContract(beginAt,"Subtraction needs one own allocation or two null pointers");
   long count=unchecked(e.Record-b.Record);if(count<0||count>Int32.MaxValue)throw new MissingContract(beginAt,"Outside bounded forward own stride0x40 count domain");return count;
  }
  static Field<float> C(NavigationCrowdAvoidanceAddress a,long n,uint offset,uint at){return Need(a,at).Cell(n,offset,at);}
  static float Flip(float value,Field<uint> mask,Invocation i,uint at){i.At=at;return Scalar(NavigationCoreContracts.FromBits(NavigationCoreContracts.ToBits(value)^mask.Read(at)),at);}

  // Exact in-place record prefix25F2AA..25F3EE; ends BEFORE atan2/sampling.
  // Pointer/header snapshots are opaque until arithmetic/dereference consumes
  // them. +38 is never read or written, and prior records survive a later stop.
  public static void Preprocess25F250(NavigationCrowdAvoidanceVector vector,NavigationPoint3 position,NavigationPoint3 desiredVelocity,NavigationCrowdAvoidanceConstants constants,Invocation invocation){
   var i=Need(invocation,0x25f250);var k=Need(constants,0x25f2aa);var one=k.One34BB44.Snapshot();PreprocessCore(vector,position,desiredVelocity,k,one,i);
  }
  static void PreprocessCore(NavigationCrowdAvoidanceVector vector,NavigationPoint3 position,NavigationPoint3 desiredVelocity,NavigationCrowdAvoidanceConstants k,Field<float> one,Invocation i){
   long size=Count(vector,i,0x25f2db,0x25f2df);if(size==0){i.At=0x25f3f4;return;}
   var sign=k.SignMask34C450.Snapshot();var threshold=k.SideThreshold34B9E0.Snapshot();long n=0;
   do{
    i.At=0x25f318;var pointer=vector.Begin00.Snapshot();
    i.At=0x25f31b;var a=Address(pointer,i.At);var dxBits=C(a,n,0,i.At).Snapshot();float dx=Sub(R(dxBits,i,0x25f31f),R(P(position,0,0x25f31f),i,0x25f31f),i,0x25f31f);
    var dyBits=C(a,n,4,0x25f323).Snapshot();float dy=Sub(R(dyBits,i,0x25f328),R(P(position,1,0x25f328),i,0x25f328),i,0x25f328);
    var dzBits=C(a,n,8,0x25f32d).Snapshot();float dz=Sub(R(dzBits,i,0x25f332),R(P(position,2,0x25f332),i,0x25f332),i,0x25f332);
    var sxBits=C(a,n,0x1c,0x25f337).Snapshot();var szBits=C(a,n,0x24,0x25f33f).Snapshot();
    float sx=Sub(R(sxBits,i,0x25f345),R(P(desiredVelocity,0,0x25f345),i,0x25f345),i,0x25f345);float sz=Sub(R(szBits,i,0x25f34a),R(P(desiredVelocity,2,0x25f34a),i,0x25f34a),i,0x25f34a);
    float xx=Mul(dx,dx,i,0x25f350),zz=Mul(dz,dz,i,0x25f357),yy=Mul(dy,dy,i,0x25f35e);float sum=Add(yy,xx,i,0x25f362);sum=Add(sum,zz,i,0x25f369);float length=Sqrt(sum,i,0x25f36d);float inverse=Div(R(one,i,0x25f371),length,i,0x25f371);
    dx=Mul(dx,inverse,i,0x25f375);dy=Mul(dy,inverse,i,0x25f379);dz=Mul(dz,inverse,i,0x25f380);
    i.At=0x25f387;var nx=C(a,n,0x28,i.At);var ny=C(a,n,0x2c,i.At);nx.Write(dx);ny.Write(dy);i.At=0x25f396;C(a,n,0x30,i.At).Write(dz);
    var liveZ=C(a,n,0x30,0x25f399).Snapshot();sz=Mul(sz,R(C(a,n,0x28,0x25f39e),i,0x25f39e),i,0x25f39e);float z=R(liveZ,i,0x25f3a7);float cross=Sub(Mul(z,sx,i,0x25f3a7),sz,i,0x25f3ab);
    i.At=0x25f3b0;if(cross>=R(threshold,i,0x25f3b0)){
     i.At=0x25f3c7;C(a,n,0x34,i.At).Write(z);var liveX=C(a,n,0x28,0x25f3cc).Snapshot();float flipped=Flip(R(liveX,i,0x25f3d1),sign,i,0x25f3d1);i.At=0x25f3d5;C(a,n,0x3c,i.At).Write(flipped);
    }else{
     float flipped=Flip(z,sign,i,0x25f3b6);i.At=0x25f3ba;C(a,n,0x34,i.At).Write(flipped);var liveX=C(a,n,0x28,0x25f3bf).Snapshot();i.At=0x25f3c2;C(a,n,0x3c,i.At).CopyFrom(liveX);
    }
    size=Count(vector,i,0x25f3da,0x25f3e1);n++;
   }while(n<size);i.At=0x25f3f4;
  }

  // Full25F250: candidate0 plus3 rings of7; then3 strict-score refinements.
  // Platform is a sealed borrowed view of the same identity-checked system
  // UCRT owner, not an injected delegate or a managed atan2 approximation.
  // Output is written only at the final8+4 stores. Record-prefix writes remain
  // visible if a later scalar, platform call or unknown fallback stops.
  public static void Select25F250(NavigationCrowdAvoidanceVector vector,NavigationPoint3 position,float radius,float maxSpeed,NavigationPoint3 currentVelocity,NavigationPoint3 desiredVelocity,NavigationPoint3 output,NavigationCrowdAvoidanceConstants constants,SystemUcrtCrowdMath platform,Invocation invocation){
   var i=Need(invocation,0x25f250);var k=Need(constants,0x25f2aa);var one=k.One34BB44.Snapshot();
   PreprocessCore(vector,position,desiredVelocity,k,one,i);
   var desiredZ=P(desiredVelocity,2,0x25f3f4).Snapshot();var desiredX=P(desiredVelocity,0,0x25f3fa).Snapshot();
   float inverseMax=Div(R(one,i,0x25f407),Scalar(maxSpeed,0x25f407),i,0x25f407);
   i.At=0x25f40c;float heading=Need(platform,i.At).Atan2(R(desiredZ,i,i.At),R(desiredX,i,i.At),i.At);heading=Scalar(heading,i.At);
   var sampleX=new float[22];var sampleZ=new float[22]; // native local pair0 is an explicit QWORD0 at25F413.
   var third=k.Third34BA9C.Snapshot();var halfStep=k.HalfStep34BAD0.Snapshot();var step=k.Step34BB18.Snapshot();int sample=1;
   for(int ring=0;ring<3;ring++){
    float factor=Mul(3-ring,R(third,i,0x25f46e),i,0x25f46e);float angle=Mul(ring&1,R(halfStep,i,0x25f473),i,0x25f473);angle=Add(angle,heading,i,0x25f478);
    for(int spoke=0;spoke<7;spoke++){
     i.At=0x25f483;float cosine=Scalar(platform.Cos(angle,i.At),i.At);sampleX[sample]=Mul(cosine,factor,i,0x25f488);
     i.At=0x25f495;float sine=Scalar(platform.Sin(angle,i.At),i.At);float z=Mul(sine,factor,i,0x25f49a);angle=Add(angle,R(step,i,0x25f49e),i,0x25f49e);sampleZ[sample]=z;sample++;
    }
   }
   var centerX=Field<float>.Of(Mul(R(desiredX,i,0x25f4c1),R(k.Quarter34BA80,i,0x25f4c1),i,0x25f4c1));float sampleRadius=Mul(Scalar(maxSpeed,0x25f4d4),R(k.ThreeQuarter34BB08,i,0x25f4d4),i,0x25f4d4);
   var fallbackZ=P(k.Fallback422980,2,0x25f4dc).Snapshot();var centerZ=Field<float>.Of(Mul(R(desiredZ,i,0x25f4e2),R(k.Quarter34BA80,i,0x25f4e2),i,0x25f4e2));
   var fallbackX=P(k.Fallback422980,0,0x25f4eb).Snapshot();var fallbackY=P(k.Fallback422980,1,0x25f4eb).Snapshot();var maxScore=k.MaxFinite34BDCC.Snapshot();var half=k.Half34BADC.Snapshot();Field<float> outputY=null;
   for(int refinement=0;refinement<3;refinement++){
    var bestX=fallbackX.Snapshot();var bestY=fallbackY.Snapshot();var bestZ=fallbackZ.Snapshot();float speedLimit=Add(Scalar(maxSpeed,0x25f51b),R(k.SpeedTolerance34B9B0,i,0x25f51b),i,0x25f51b);var bestScore=maxScore.Snapshot();speedLimit=Mul(speedLimit,speedLimit,i,0x25f532);
    for(int n=0;n<22;n++){
     float z=Mul(sampleRadius,sampleZ[n],i,0x25f546);float x=Mul(sampleRadius,sampleX[n],i,0x25f54c);z=Add(z,R(centerZ,i,0x25f552),i,0x25f552);x=Add(x,R(centerX,i,0x25f557),i,0x25f557);
     float zz=Mul(z,z,i,0x25f562),xx=Mul(x,x,i,0x25f56c);float candidateSpeed2=Add(zz,xx,i,0x25f570);i.At=0x25f574;if(candidateSpeed2>speedLimit)continue;
     var candidate=new NavigationPoint3(Field<float>.Of(x),Field<float>.Of(0),Field<float>.Of(z));i.At=0x25f59d;float score=Score25EF20(vector,candidate,position,radius,currentVelocity,desiredVelocity,inverseMax,k,i);
     i.At=0x25f5a2;if(score<R(bestScore,i,i.At)){bestScore.Write(score);bestX.Write(x);bestY.Write(0);bestZ.Write(z);}
    }
    // MOVSS copies unknown bits too. Do not require fallback values at these
    // loads; a later ADDSS or opaque final store determines their actual use.
    outputY=bestY.Snapshot();centerX=bestX.Snapshot();centerZ=bestZ.Snapshot();sampleRadius=Mul(sampleRadius,R(half,i,0x25f5ed),i,0x25f5ed);
   }
   var finalX=centerX.Snapshot();var finalY=outputY.Snapshot();var finalZ=centerZ.Snapshot();
   i.At=0x25f667;var ox=P(output,0,i.At);var oy=P(output,1,i.At);ox.CopyFrom(finalX);oy.CopyFrom(finalY);i.At=0x25f66b;P(output,2,i.At).CopyFrom(finalZ);i.At=0x25f67a;
  }

  public static float Score25EF20(NavigationCrowdAvoidanceVector vector,NavigationPoint3 candidate,NavigationPoint3 position,float radius,NavigationPoint3 currentVelocity,NavigationPoint3 desiredVelocity,float inverseMaxSpeed,NavigationCrowdAvoidanceConstants constants,Invocation invocation){
   var i=Need(invocation,0x25ef20);long count=Count(vector,i,0x25ef2a,0x25ef31);var k=Need(constants,0x25ef53);var horizon=k.Horizon34BC00.Snapshot();var earliest=horizon.Snapshot();float sideSum=0;
   if(count!=0){
    var two=k.Two34BBD8.Snapshot();var candidateZ=P(candidate,2,0x25ef81).Snapshot();i.At=0x25ef87;var capturedBegin=vector.Begin00.Snapshot();var half=k.Half34BADC.Snapshot();var one=k.One34BB44.Snapshot();var negativeHalf=k.NegativeHalf34BDE8.Snapshot();var candidateX=P(candidate,0,0x25efc7).Snapshot();
    float baseX=Mul(R(candidateX,i,0x25efcc),R(two,i,0x25efcc),i,0x25efcc);float baseZ=Mul(R(candidateZ,i,0x25efd1),R(two,i,0x25efd1),i,0x25efd1);
    baseX=Sub(baseX,R(P(currentVelocity,0,0x25efd6),i,0x25efd6),i,0x25efd6);baseZ=Sub(baseZ,R(P(currentVelocity,2,0x25efdb),i,0x25efdb),i,0x25efdb);
    for(long n=0;n<count;n++){
     i.At=0x25effc;var a=Address(capturedBegin,i.At);float rx=Sub(baseX,R(C(a,n,0x10,i.At),i,i.At),i,i.At);float rz=Sub(baseZ,R(C(a,n,0x18,0x25f002),i,0x25f002),i,0x25f002);
     var pxBits=C(a,n,0,0x25f008).Snapshot();float px=Sub(R(pxBits,i,0x25f00c),R(P(position,0,0x25f00c),i,0x25f00c),i,0x25f00c);float rsum=Add(Scalar(radius,0x25f011),R(C(a,n,0xc,0x25f011),i,0x25f011),i,0x25f011);
     float sideX=Mul(rx,R(C(a,n,0x28,0x25f01a),i,0x25f01a),i,0x25f01a);float sideZ=Mul(rz,R(C(a,n,0x30,0x25f023),i,0x25f023),i,0x25f023);
     sideX=Mul(sideX,R(half,i,0x25f02c),i,0x25f02c);sideZ=Mul(sideZ,R(half,i,0x25f030),i,0x25f030);float zz=Mul(rz,rz,i,0x25f034);float sideA=Add(sideZ,sideX,i,0x25f039);
     sideX=Mul(rx,R(C(a,n,0x34,0x25f041),i,0x25f041),i,0x25f041);sideA=Add(sideA,R(half,i,0x25f046),i,0x25f046);sideX=Mul(sideX,R(two,i,0x25f04a),i,0x25f04a);
     sideZ=Mul(rz,R(C(a,n,0x3c,0x25f052),i,0x25f052),i,0x25f052);sideZ=Mul(sideZ,R(two,i,0x25f057),i,0x25f057);float sideB=Add(sideZ,sideX,i,0x25f05b);float xx=Mul(rx,rx,i,0x25f063);float side=Min(sideA,sideB,i,0x25f068);
     var pzBits=C(a,n,8,0x25f06c).Snapshot();float pz=Sub(R(pzBits,i,0x25f071),R(P(position,2,0x25f071),i,0x25f071),i,0x25f071);float speed2=Add(zz,xx,i,0x25f077);side=Max(side,0,i,0x25f07c);
     float epsilon=R(k.Epsilon34B984,i,0x25f080);bool below=speed2<epsilon;side=Min(side,R(one,i,0x25f088),i,0x25f088);sideSum=Add(sideSum,side,i,0x25f08d);
     if(!below){
      rsum=Mul(rsum,rsum,i,0x25f09b);float pzz=Mul(pz,pz,i,0x25f09f);float dotZ=Mul(pz,rz,i,0x25f0a6);float pxx=Mul(px,px,i,0x25f0ab);float dotX=Mul(px,rx,i,0x25f0af);float pos2=Add(pzz,pxx,i,0x25f0b4);float dot=Add(dotZ,dotX,i,0x25f0b8);float c=Sub(pos2,rsum,i,0x25f0bc);float disc=Mul(dot,dot,i,0x25f0c3);c=Mul(c,speed2,i,0x25f0c7);disc=Sub(disc,c,i,0x25f0cc);
      if(disc>=0){
       float root=Sqrt(disc,i,0x25f0d5),inv=Div(R(one,i,0x25f0dd),speed2,i,0x25f0dd);float t0=Sub(dot,root,i,0x25f0e5),t1=Add(root,dot,i,0x25f0e9);t0=Mul(t0,inv,i,0x25f0ed);t1=Mul(t1,inv,i,0x25f0f1);
       if(t0<0&&t1>0)t0=Mul(t0,R(negativeHalf,i,0x25f0ff),i,0x25f0ff);
       if(t0>=0)earliest.Write(Min(t0,R(earliest,i,0x25f109),i,0x25f109));
      }
      two=k.Two34BBD8.Snapshot();
     }
     half=k.Half34BADC.Snapshot();
    }
    horizon=k.Horizon34BC00.Snapshot();sideSum=Div(sideSum,Round(count,i,0x25f163),i,0x25f166);
   }
   var cx=P(candidate,0,0x25f173).Snapshot();var cz=P(candidate,2,0x25f178).Snapshot();var dxBits=P(desiredVelocity,0,0x25f17e).Snapshot();var dzBits=P(desiredVelocity,2,0x25f182).Snapshot();
   float dx=Sub(R(dxBits,i,0x25f187),R(cx,i,0x25f187),i,0x25f187),dz=Sub(R(dzBits,i,0x25f18b),R(cz,i,0x25f18b),i,0x25f18b);float dxx=Mul(dx,dx,i,0x25f18f),dzz=Mul(dz,dz,i,0x25f193),ds=Add(dzz,dxx,i,0x25f197);float dlen=ds==0?0:Sqrt(ds,i,0x25f1a8);
   var vxBits=P(currentVelocity,0,0x25f1ac).Snapshot();var vzBits=P(currentVelocity,2,0x25f1b0).Snapshot();float vx=Sub(R(vxBits,i,0x25f1b5),R(cx,i,0x25f1b5),i,0x25f1b5);float vz=Sub(R(vzBits,i,0x25f1c2),R(cz,i,0x25f1c2),i,0x25f1c2);
   float invSpeed=Scalar(inverseMaxSpeed,0x25f1ce);float desiredPenalty=Mul(dlen,invSpeed,i,0x25f1ce);float vxx=Mul(vx,vx,i,0x25f1d2);desiredPenalty=Mul(desiredPenalty,R(k.Ten34BC70,i,0x25f1d6),i,0x25f1d6);float vzz=Mul(vz,vz,i,0x25f1de),vs=Add(vzz,vxx,i,0x25f1e2);float vlen=vs==0?0:Sqrt(vs,i,0x25f1ee);
   float time=Mul(R(earliest,i,0x25f1f2),R(k.TimeScale34BAB8,i,0x25f1f2),i,0x25f1f2);float sidePenalty=Mul(sideSum,R(k.ThreeQuarter34BB08,i,0x25f202),i,0x25f202);time=Add(time,R(k.TimeBias34BA34,i,0x25f20b),i,0x25f20b);
   float currentPenalty=Mul(vlen,invSpeed,i,0x25f213);currentPenalty=Mul(currentPenalty,R(k.ThreeQuarter34BB08,i,0x25f21c),i,0x25f21c);float timePenalty=Div(R(horizon,i,0x25f224),time,i,0x25f224);
   float score=Add(currentPenalty,desiredPenalty,i,0x25f22e);score=Add(score,sidePenalty,i,0x25f232);score=Add(score,timePenalty,i,0x25f23c);i.At=0x25f246;return score;
  }
 }
}
