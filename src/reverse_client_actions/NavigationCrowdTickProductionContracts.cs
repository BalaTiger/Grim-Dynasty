using System;
using System.Runtime.CompilerServices;
namespace GrimDynasty.ReverseClient.Actions {
 // A MOV-like returned pointer, not a copied waypoint. Unknown address bits
 // may be returned by25BF40 and are consumed only at the caller dereference.
 public sealed class NavigationCrowdFirstWaypoint {
  readonly Field<object> bits;
  internal NavigationCrowdFirstWaypoint(Field<object> captured){bits=captured;}
  public Field<float> Cell(int axis,uint at){var p=bits.Read(at) as NavigationCrowdPointAddress;if(p==null)throw new MissingContract(at,"First waypoint is not a bound OWN stride12 address");return p.Cell(axis,at);}
 }
 public sealed class NavigationCrowdTickProductionBoundaries {
  public readonly NavigationCrowdPathBoundaries Path;
  public readonly NavigationCrowdProximityBoundaries Proximity;
  public readonly NavigationCrowdAdapterBoundaries Adapter;
  public readonly NavigationCrowdAvoidanceConstants Constants;
  public readonly SystemUcrtRotationMath Math;
  // OWN diagnostic only, never consulted by production or treated as native
  // state. Null means the projection block has not returned a known result.
  public string ProjectionOutcome{get;internal set;}
  public uint ProjectionReference{get;internal set;}
  public readonly Field<byte> DisableFirstAssert422B91=new Field<byte>(),DisableHeadingAssert422B9C=new Field<byte>();
  // Both fields are unobserved loaded BSS; never initialized by disk constants.
  public NavigationPoint3 Global422980{get{return Constants.Fallback422980;}}
  public NavigationCrowdTickProductionBoundaries(NavigationCrowdPathBoundaries path,NavigationCrowdProximityBoundaries proximity,NavigationCrowdAdapterBoundaries adapter,NavigationCrowdAvoidanceConstants constants,SystemUcrtRotationMath math){Path=path;Proximity=proximity;Adapter=adapter;Constants=constants;Math=math;}
 }
 // Engine25C440/25BF40 saved Rea assembly in r1-crowd-movement-direct and
 // r1-crowd-motion-helpers. This is a reached-block continuation, NOT a full
 // tick. Earlier callback/update/replan/bounds and caller dt/max-radius inputs
 // must really exist. The bounded OWN domain has one identical Work/Member
 // agent and actual proximity output. Nonempty collision/avoidance population
 // is an explicit stop; no empty array is substituted for unknown neighbors.
 // All fields are authoritative shared cells. There is no success delegate.
 // Full finite normal/zero binary32 domain, round-to-nearest, stable lifetime;
 // arbitrary byte aliases, SIMD exceptions/MXCSR and native diagnostic effects
 // are not modeled. At25E087 this component STOPS before066EE0, preserving the
 // real position and Vector60 prefix. Navigation projection is not floor.
 public static class NavigationCrowdTickProductionContracts {
  static T Need<T>(T x,uint at)where T:class{if(x==null)throw new MissingContract(at,"Null OWN tick production view");var v=x as NavigationView;if(v!=null)v.RequireLive(at);return x;}
  static Field<float> C(NavigationPoint3 p,int axis,uint at){Need(p,at);return axis==0?p.X:axis==1?p.Y:p.Z;}
  static float Scalar(float x,uint at){uint u=NavigationCoreContracts.ToBits(x);if((u&0x7f800000u)==0x7f800000u||((u&0x7f800000u)==0&&(u&0x7fffffu)!=0))throw new MissingContract(at,"OWN finite normal/zero tick arithmetic domain");return x;}
  [MethodImpl(MethodImplOptions.NoInlining)] static float F(double x,uint at){if(x!=0&&Math.Abs(x)<1.1754943508222875E-38)throw new MissingContract(at,"OWN tick domain excludes subnormal intermediates");return Scalar(NavigationCoreContracts.F((float)x),at);}
  static float R(Field<float> f,Invocation i,uint at){i.At=at;return Scalar(Need(f,at).Read(at),at);}
  static float Raw(Field<uint> f,Invocation i,uint at){i.At=at;return Scalar(NavigationCoreContracts.FromBits(f.Read(at)),at);}
  static float Add(float a,float b,Invocation i,uint at){i.At=at;return F((double)a+b,at);}static float Sub(float a,float b,Invocation i,uint at){i.At=at;return F((double)a-b,at);}static float Mul(float a,float b,Invocation i,uint at){i.At=at;return F((double)a*b,at);}static float Div(float a,float b,Invocation i,uint at){i.At=at;return F((double)a/b,at);}static float Root(float a,Invocation i,uint at){i.At=at;return F(Math.Sqrt(a),at);}
  static float Length(float x,float y,float z,Invocation i,uint square,uint sum,uint root){float xx=Mul(x,x,i,square),yy=Mul(y,y,i,square),zz=Mul(z,z,i,square);float d=Add(Add(xx,yy,i,sum),zz,i,sum);return d==0?0:Root(d,i,root);}
  static void CopyPoint(NavigationPoint3 to,NavigationPoint3 from,Invocation i,uint load,uint xy,uint z){var x=C(from,0,load).Snapshot();var y=C(from,1,load).Snapshot();var zz=C(from,2,load).Snapshot();i.At=xy;C(to,0,xy).CopyFrom(x);C(to,1,xy).CopyFrom(y);i.At=z;C(to,2,z).CopyFrom(zz);}
  static NavigationCrowdPointAddress Point(Field<object> bits,uint at){var value=bits.Read(at);if(value==null)return null;var p=value as NavigationCrowdPointAddress;if(p==null)throw new MissingContract(at,"Not an OWN waypoint pointer");return p;}
  static long PointCount(NavigationCrowdRequest r,uint endAt,uint beginAt){Need(r,endAt);var e=r.WaypointVector38.End08.Snapshot();var b=r.WaypointVector38.Begin00.Snapshot();var ep=Point(e,beginAt);var bp=Point(b,beginAt);if(ep==null&&bp==null)return 0;if(ep==null||bp==null)throw new MissingContract(beginAt,"OWN waypoint subtraction needs one allocation");try{if(!Object.ReferenceEquals(ep.Buffer,bp.Buffer))throw new MissingContract(beginAt,"Different waypoint allocations");return ep.PointIndex-bp.PointIndex;}catch(MissingContract ex){throw new MissingContract(beginAt,ex.Message,ex);}}
  public static NavigationCrowdFirstWaypoint First25BF40(NavigationCrowdRequest r,NavigationCrowdTickProductionBoundaries b,Invocation i){
   Need(i,0x25bf40);i.At=0x25bf4b;if(Need(b,i.At).DisableFirstAssert422B91.Read(i.At)==0){ulong n=unchecked((ulong)PointCount(r,0x25bf5b,0x25bf5f));i.At=0x25bf7d;if(n<1){i.At=0x25bf87;if(Need(b.Path,i.At).DisableAllAssert42144A.Read(i.At)==0){i.At=0x25bfa3;throw new MissingContract(i.At,"Native first-waypoint diagnostic branch not reconstructed");}}}
   i.At=0x25c071;var captured=Need(r,i.At).WaypointVector38.Begin00.Snapshot();i.At=0x25c07d;return new NavigationCrowdFirstWaypoint(captured);
  }
  static long PointerCount(NavigationCrowdPointerHeader<NavigationCrowdAgent> h,uint endAt,uint subAt){var e=h.Capture(NavigationCrowdPointerSlot.End,endAt).Read(subAt);var b=h.Read(NavigationCrowdPointerSlot.Begin,subAt);if(e==null&&b==null)return 0;if(e==null||b==null||!Object.ReferenceEquals(e.Buffer,b.Buffer))throw new MissingContract(subAt,"OWN pointer subtraction needs one allocation");return e.Index-b.Index;}
  // This entry guard declares the bounded composition domain; it is not an
  // invented native branch or proof that the preceding25C440 blocks ran.
  static void SingleWorkDomain(NavigationCrowdGroup g,NavigationCrowdAgent a,NavigationCrowdPointerDomain<NavigationCrowdAgent> d){const uint at=0x25cb73;var work=NavigationCrowdBorrowedPointerContracts.Work40(Need(g,at),d);var member=NavigationCrowdPointerHeader<NavigationCrowdAgent>.BorrowTyped(g.Members28);if(PointerCount(work,at,at)!=1||PointerCount(member,at,at)!=1)throw new MissingContract(at,"OWN reached continuation requires exactly one Work and one Member; no multi-agent phase ordering inferred");var wp=work.Read(NavigationCrowdPointerSlot.Begin,at);var mp=member.Read(NavigationCrowdPointerSlot.Begin,at);if(!Object.ReferenceEquals(wp.Buffer.At(wp.Index,at).Read(at),a)||!Object.ReferenceEquals(mp.Buffer.At(mp.Index,at).Read(at),a))throw new MissingContract(at,"Work/Member must reference this same actual agent");}
  public static void ProduceReachedSingleWork25CB73(NavigationCrowdGroup g,NavigationCrowdAgent a,NavigationCrowdPointerDomain<NavigationCrowdAgent> domain,Field<float> capturedMaximumRadius,Field<float> capturedNeighborFactor,Field<float> capturedSeconds,NavigationCrowdTickProductionBoundaries b,Invocation i){
   Need(i,0x25cb73);Need(a,0x25cb73);Need(b,0x25cb73);b.ProjectionOutcome=null;b.ProjectionReference=0;SingleWorkDomain(g,a,domain);
   NavigationCrowdTickHeaderContracts.PopulateReachedNeighbors25CB73(g,a,domain,capturedMaximumRadius,capturedNeighborFactor,b.Proximity,i);
   Steer25CBBD(a,b,i);Avoid25CFCA(g,a,domain,b,i);Advance25D307(a,domain,capturedSeconds,b,i);
   Correct25DB74(g,a,domain,b,i);Publish25DE07(g,a,capturedSeconds,b,i);
  }
  // First waypoint is the actual request-produced address; no direction or
  // velocity is injected. Optional two-point smoothing uses the real second.
  static void Steer25CBBD(NavigationCrowdAgent a,NavigationCrowdTickProductionBoundaries b,Invocation i){
   var k=Need(b.Constants,0x25cbc4);i.At=0x25cbc4;var p=First25BF40(a.RequestE8,b,i);i.At=0x25cbc9;bool smooth=(a.Flags44.Read(i.At)&0x200)!=0;
   var py=p.Cell(1,0x25cbd4).Snapshot();var px=p.Cell(0,0x25cbd9).Snapshot();float y=Sub(R(py,i,0x25cbdd),R(a.Position48.Y,i,0x25cbdd),i,0x25cbdd);float x=Sub(R(px,i,0x25cbe2),R(a.Position48.X,i,0x25cbe2),i,0x25cbe2);var pz=p.Cell(2,0x25cbe7).Snapshot();float z=Sub(R(pz,i,0x25cbec),R(a.Position48.Z,i,0x25cbec),i,0x25cbec);
   float yy=Mul(y,y,i,0x25cbf4),xx=Mul(x,x,i,0x25cbfb),zz=Mul(z,z,i,0x25cc02);float norm=Root(Add(Add(yy,xx,i,0x25cc06),zz,i,0x25cc0d),i,0x25cc11);float inv=Div(R(k.One34BB44,i,0x25cc15),norm,i,0x25cc15);x=Mul(x,inv,i,0x25cc1f);y=Mul(y,inv,i,0x25cc23);z=Mul(z,inv,i,0x25cc27);i.At=0x25cc31;a.VelocityB8.X.Write(x);a.VelocityB8.Y.Write(y);i.At=0x25cc41;a.VelocityB8.Z.Write(z);
   if(smooth&&unchecked((ulong)PointCount(a.RequestE8,0x25cc4d,0x25cc54))>=2){
    p=First25BF40(a.RequestE8,b,i);var second=NavigationCrowdPathContracts.Second25C080(a.RequestE8,b.Path,i);
    float fz=Sub(R(p.Cell(2,0x25cc8c),i,0x25cc8c),R(a.Position48.Z,i,0x25cc95),i,0x25cc95),fx=Sub(R(p.Cell(0,0x25cc91),i,0x25cc91),R(a.Position48.X,i,0x25cc9a),i,0x25cc9a);
    float sz=Sub(R(second.Cell(2,0x25cc9f),i,0x25cc9f),R(a.Position48.Z,i,0x25cca8),i,0x25cca8),sx=Sub(R(second.Cell(0,0x25cca4),i,0x25cca4),R(a.Position48.X,i,0x25ccad),i,0x25ccad);
    float fl=Root(Add(Mul(fx,fx,i,0x25ccb8),Mul(fz,fz,i,0x25ccbc),i,0x25ccc0),i,0x25ccd3);float sl=Root(Add(Mul(sz,sz,i,0x25ccdd),Mul(sx,sx,i,0x25cce1),i,0x25cce5),i,0x25ccef);
    if(sl!=0&&sl>R(k.SpeedTolerance34B9B0,i,0x25ccf3)){float si=Div(R(k.One34BB44,i,0x25ccfd),sl,i,0x25ccfd);sx=Mul(sx,si,i,0x25cd01);sz=Mul(sz,si,i,0x25cd05);}
    sx=Mul(Mul(sx,fl,i,0x25cd0c),R(k.Half34BADC,i,0x25cd14),i,0x25cd14);sz=Mul(Mul(sz,fl,i,0x25cd10),R(k.Half34BADC,i,0x25cd19),i,0x25cd19);fx=Sub(fx,sx,i,0x25cd1e);fz=Sub(fz,sz,i,0x25cd22);float fi=Div(R(k.One34BB44,i,0x25cd43),Root(Add(Mul(fz,fz,i,0x25cd34),Mul(fx,fx,i,0x25cd2d),i,0x25cd38),i,0x25cd3f),i,0x25cd43);x=Mul(fx,fi,i,0x25cd4a);z=Mul(fz,fi,i,0x25cd4e);i.At=0x25cd52;a.VelocityB8.X.Write(x);i.At=0x25cd5a;a.VelocityB8.Z.Write(z);
   }
   var speed=a.Parameter18.Snapshot();float vx=Mul(R(a.VelocityB8.X,i,0x25cd6d),R(speed,i,0x25cd6d),i,0x25cd6d),vy=Mul(R(a.VelocityB8.Y,i,0x25cd75),R(speed,i,0x25cd75),i,0x25cd75),vz=Mul(R(a.VelocityB8.Z,i,0x25cd7d),R(speed,i,0x25cd7d),i,0x25cd7d);i.At=0x25cd85;a.VelocityB8.X.Write(vx);i.At=0x25cd8d;a.VelocityB8.Y.Write(vy);i.At=0x25cd95;a.VelocityB8.Z.Write(vz);
   if(Raw(a.ParameterBits1C40[4],i,0x25cd9d)>0){i.At=0x25cda8;if((a.Flags44.Read(i.At)&0x800)==0)throw new MissingContract(0x25cdb5,"Positive separation requires its BSS accumulator/neighbor loop; this bounded producer preserves desired prefix");}i.At=0x25cf79;
  }
  static void Avoid25CFCA(NavigationCrowdGroup g,NavigationCrowdAgent a,NavigationCrowdPointerDomain<NavigationCrowdAgent> domain,NavigationCrowdTickProductionBoundaries b,Invocation i){
   i.At=0x25cfca;uint flags=a.Flags44.Read(i.At);if((flags&12)==0||(flags&0x800)!=0)return;float speed=Raw(a.Timer6C,i,0x25cfdf);if(speed==0)return;
   if((flags&4)!=0){long n=PointerCount(NavigationCrowdBorrowedPointerContracts.Neighbors88(a,domain),0x25cff2,0x25cffc);if(n!=0){i.At=0x25d020;throw new MissingContract(i.At,"Reached nonempty stride40 avoidance-record population is not reconstructed; no fake records");}}
   speed=Raw(a.Timer6C,i,0x25d24d);var max=a.Parameter18.Snapshot();float cz=Mul(speed,R(a.Heading54.Z,i,0x25d26b),i,0x25d26b),cy=Mul(speed,R(a.Heading54.Y,i,0x25d286),i,0x25d286),cx=Mul(speed,R(a.Heading54.X,i,0x25d292),i,0x25d292);float radius=R(a.Radius10,i,0x25d29d);var current=new NavigationPoint3(Field<float>.Of(cx),Field<float>.Of(cy),Field<float>.Of(cz));var output=new NavigationPoint3();var vector=new NavigationCrowdAvoidanceVector(g.Vector80);
   i.At=0x25d2ae;NavigationCrowdAvoidanceContracts.Select25F250(vector,a.Position48,radius,R(max,i,0x25d2ae),current,a.VelocityB8,output,b.Constants,Need(b.Math,i.At).Crowd,i);
   var ox=output.X.Snapshot();var oz=output.Z.Snapshot();i.At=0x25d2c2;a.VelocityB8.X.CopyFrom(ox);i.At=0x25d2c6;a.VelocityB8.Z.CopyFrom(oz);var begin=g.Vector80.Begin00.Snapshot();i.At=0x25d2d5;g.Vector80.End08.CopyFrom(begin);
  }
  static void Advance25D307(NavigationCrowdAgent a,NavigationCrowdPointerDomain<NavigationCrowdAgent> domain,Field<float> dt,NavigationCrowdTickProductionBoundaries b,Invocation i){
   float accel=Raw(a.ParameterBits1C40[0],i,0x25d30c);if(accel>0){var max=a.Parameter18.Snapshot();float sum=Add(Mul(accel,R(dt,i,0x25d317),i,0x25d317),Raw(a.Timer6C,i,0x25d31c),i,0x25d31c);float m=R(max,i,0x25d321);i.At=0x25d325;a.Timer6C.Write(NavigationCoreContracts.ToBits(m<sum?m:sum));}else{var max=a.Parameter18.Snapshot();i.At=0x25d32f;CopyFloatBits(a.Timer6C,max);}
   float x=R(a.VelocityB8.X,i,0x25d34a),y=R(a.VelocityB8.Y,i,0x25d34e),z=R(a.VelocityB8.Z,i,0x25d352);float len=Length(x,y,z,i,0x25d34a,0x25d356,0x25d36d);float captured=Raw(a.Timer6C,i,0x25d376);if(len<captured){i.At=0x25d37b;a.Timer6C.Write(NavigationCoreContracts.ToBits(len));}if(Raw(a.ParameterBits1C40[6],i,0x25d385)>Raw(a.Timer6C,i,0x25d385)){i.At=0x25db0a;return;}
   i.At=0x25d38f;if((a.Flags44.Read(i.At)&0x40)!=0){i.At=0x25d395;a.Timer6C.Write(NavigationCoreContracts.ToBits(captured));}
   x=R(a.VelocityB8.X,i,0x25d3aa);z=R(a.VelocityB8.Z,i,0x25d3ae);float horizontal=Add(Mul(x,x,i,0x25d3aa),Mul(z,z,i,0x25d3ae),i,0x25d3b2);
   if(horizontal>R(b.Constants.Epsilon34B984,i,0x25d3b6)){y=R(a.VelocityB8.Y,i,0x25d3e1);x=R(a.VelocityB8.X,i,0x25d3e8);z=R(a.VelocityB8.Z,i,0x25d3ec);float inverse=Div(R(b.Constants.One34BB44,i,0x25d3ff),Length(x,y,z,i,0x25d3e1,0x25d3f0,0x25d3fb),i,0x25d3ff);x=Mul(inverse,x,i,0x25d409);y=Mul(inverse,y,i,0x25d40d);z=Mul(inverse,z,i,0x25d411);i.At=0x25d41b;a.Heading54.X.Write(x);a.Heading54.Y.Write(y);i.At=0x25d43d;a.Heading54.Z.Write(z);}else CopyPoint(a.Heading54,b.Global422980,i,0x25d42a,0x25d432,0x25d43d);
   i.At=0x25d440;if(b.DisableHeadingAssert422B9C.Read(i.At)==0){
    // The saved import is _finite(double). In the declared finite normal/zero
    // domain CVTPS2PD is exact and this predicate is true; this is a concrete
    // primitive predicate, not a caller-supplied success callback.
    R(a.Heading54.X,i,0x25d455);R(a.Heading54.Y,i,0x25d467);R(a.Heading54.Z,i,0x25d479);
   }
   i.At=0x25d54d;uint flags=a.Flags44.Read(i.At);float travel=Mul(R(dt,i,0x25d554),Raw(a.Timer6C,i,0x25d554),i,0x25d554);float pz=Add(Mul(travel,R(a.Heading54.Z,i,0x25d565),i,0x25d565),R(a.Position48.Z,i,0x25d57d),i,0x25d57d),py=Add(Mul(travel,R(a.Heading54.Y,i,0x25d56f),i,0x25d56f),R(a.Position48.Y,i,0x25d583),i,0x25d583),px=Add(Mul(travel,R(a.Heading54.X,i,0x25d578),i,0x25d578),R(a.Position48.X,i,0x25d589),i,0x25d589);
   if((flags&0x800)==0){long n=PointerCount(NavigationCrowdBorrowedPointerContracts.Neighbors88(a,domain),0x25d5b4,0x25d5bd);if(n!=0){i.At=(flags&0x10)!=0?0x25d610u:0x25d900u;throw new MissingContract(i.At,"Nonempty collision branch/slot38 is not reconstructed; no position published");}}
   i.At=0x25da86;if(a.State70.Read(i.At)!=6){float ty=Sub(R(a.Target78.Y,i,0x25da96),R(a.Position48.Y,i,0x25da96),i,0x25da96),tx=Sub(R(a.Target78.X,i,0x25da9b),R(a.Position48.X,i,0x25da9b),i,0x25da9b),tz=Sub(R(a.Target78.Z,i,0x25daa8),R(a.Position48.Z,i,0x25daa8),i,0x25daa8);float dist=Length(ty,tx,tz,i,0x25daad,0x25dab9,0x25dad0);if(dist<=travel)CopyPoint(a.Position48,a.Target78,i,0x25dadd,0x25dae8,0x25daed);else{i.At=0x25dafc;a.Position48.X.Write(px);a.Position48.Y.Write(py);i.At=0x25db01;a.Position48.Z.Write(pz);}}i.At=0x25db0a;
  }
  static void CopyFloatBits(Field<uint> to,Field<float> from){try{to.Write(NavigationCoreContracts.ToBits(from.Read(0)));}catch(MissingContract){to.Forget();}}
  static void Correct25DB74(NavigationCrowdGroup g,NavigationCrowdAgent a,NavigationCrowdPointerDomain<NavigationCrowdAgent> d,NavigationCrowdTickProductionBoundaries b,Invocation i){
   var x=b.Global422980.X.Snapshot();var y=b.Global422980.Y.Snapshot();i.At=0x25db87;CopyFloatBits(a.GapD0,x);CopyFloatBits(a.GapD4,y);var z=b.Global422980.Z.Snapshot();i.At=0x25db95;CopyFloatBits(a.GapD8,z);i.At=0x25db9b;uint flags=a.Flags44.Read(i.At);if((flags&0x801)==1){i.At=0x25dbac;if(a.State70.Read(i.At)!=2)throw new MissingContract(0x25dbb6,"Nonmoving-member second proximity population lies outside this reached State2 continuation");long n=PointerCount(NavigationCrowdBorrowedPointerContracts.Neighbors88(a,d),0x25dbfd,0x25dc06);if(n!=0)throw new MissingContract(0x25dc20,"Nonempty correction loop/slot30 outside bounded one-agent continuation");}i.At=0x25ddd8;
  }
  static void Publish25DE07(NavigationCrowdGroup g,NavigationCrowdAgent a,Field<float> dt,NavigationCrowdTickProductionBoundaries b,Invocation i){
   float x=Add(R(a.Position48.X,i,0x25de0c),Raw(a.GapD0,i,0x25de0c),i,0x25de0c);i.At=0x25de14;a.Position48.X.Write(x);float y=Add(Raw(a.GapD4,i,0x25de21),R(a.Position48.Y,i,0x25de21),i,0x25de21);i.At=0x25de26;a.Position48.Y.Write(y);float z=Add(Raw(a.GapD8,i,0x25de33),R(a.Position48.Z,i,0x25de33),i,0x25de33);i.At=0x25de38;a.Position48.Z.Write(z);i.At=0x25de3d;uint flags=a.Flags44.Read(i.At);
   if((flags&2)!=0)throw new MissingContract(0x25de4a,"Flags2 displacement-heading branch is outside this first continuous producer");
   i.At=0x25df0a;if(a.State70.Read(i.At)!=2)throw new MissingContract(0x25e30a,"Non-State2 still calls real Moved slot18; callback not implemented");
   float smoothing=Raw(a.ParameterBits1C40[9],i,0x25df1a);if(smoothing<=0)CopyPoint(a.Vector60,a.Heading54,i,0x25e006,0x25e00e,0x25e013);else{
    float vx=R(a.Vector60.X,i,0x25df33),vy=R(a.Vector60.Y,i,0x25df37),vz=R(a.Vector60.Z,i,0x25df3b);float norm=Add(Add(Mul(vx,vx,i,0x25df33),Mul(vy,vy,i,0x25df37),i,0x25df3f),Mul(vz,vz,i,0x25df3b),i,0x25df43);if(norm<=R(b.Constants.Epsilon34B984,i,0x25df47))CopyPoint(a.Vector60,a.Heading54,i,0x25df50,0x25df58,0x25df5d);
    float dot=Add(Mul(R(a.Vector60.Z,i,0x25df70),R(a.Heading54.Z,i,0x25df70),i,0x25df70),Mul(R(a.Vector60.X,i,0x25df6b),R(a.Heading54.X,i,0x25df6b),i,0x25df6b),i,0x25df76);i.At=0x25df7f;float angle=Scalar(Need(b.Math,i.At).Acos(dot,i.At),i.At);if(angle>R(b.Constants.Epsilon34B984,i,0x25df84)){
     float dx=Sub(R(a.Heading54.X,i,0x25dfaa),Mul(dot,R(a.Vector60.X,i,0x25df9a),i,0x25df9a),i,0x25dfaa),dz=Sub(R(a.Heading54.Z,i,0x25dfb3),Mul(dot,R(a.Vector60.Z,i,0x25df9f),i,0x25df9f),i,0x25dfb3);float turn=Mul(smoothing,R(dt,i,0x25dfae),i,0x25dfae);turn=turn<angle?turn:angle;i.At=0x25dfc1;float sine=Scalar(b.Math.Sin(turn,i.At),i.At);float sx=Mul(sine,dx,i,0x25dfd2),sz=Mul(sine,dz,i,0x25dfd7);i.At=0x25dfdc;float cosine=Scalar(b.Math.Cos(turn,i.At),i.At);float oz=Add(sz,Mul(cosine,R(a.Vector60.Z,i,0x25dfe4),i,0x25dfe4),i,0x25dfee),ox=Add(sx,Mul(cosine,R(a.Vector60.X,i,0x25dfe9),i,0x25dfe9),i,0x25dff3);i.At=0x25dff8;a.Vector60.Z.Write(oz);i.At=0x25dffe;a.Vector60.X.Write(ox);
    }
   }
   if((flags&0x20)==0){i.At=0x25e01c;var query=g.Query08.Snapshot();i.At=0x25e02c;var q=query.Read(i.At);if(q==null)throw new MissingContract(i.At,"Unknown/null real navigation adapter");var output=new NavigationPoint3();uint reference=NavigationCrowdAdapterContracts.Project030B30(q,a.Position48,output,Need(b.Adapter,i.At),i);b.ProjectionReference=reference;if(reference!=0){b.ProjectionOutcome="030B30-success";i.At=0x25e039;a.Position48.Y.CopyFrom(output.Y);}else{b.ProjectionOutcome="030B30-failed";i.At=0x25e040;query=g.Query08.Snapshot();i.At=0x25e054;q=query.Read(i.At);byte success=NavigationCrowdAdapterContracts.Project030BB0(q,a.Position48,b.Constants.Ten34BC70,output,b.Adapter,i);b.ProjectionOutcome=success!=0?"030BB0-success":"both-failed";if(success!=0){CopyPoint(a.Position48,output,i,0x25e05b,0x25e065,0x25e06a);i.At=0x25e06d;a.NeedReplan140.Write(1);}}}else b.ProjectionOutcome="native-flag20-skip";
   // Do not invoke an injected callback or mark the whole tick successful.
   i.At=0x25e074;var callback=a.Callback08.Snapshot();i.At=0x25e084;var view=callback.Read(i.At) as GameMovementInterfaceView;if(view==null)throw new MissingContract(i.At,"Callback identity lacks the existing concrete Game movement interface view");view.FullOwner.Entity.RequireLive(i.At);var table=view.Virtual38.Snapshot();i.At=0x25e087;object target=table.Read(i.At);if(!Object.ReferenceEquals(target,GameMovementInterfaceTable.Character)&&!Object.ReferenceEquals(target,GameMovementInterfaceTable.Player))throw new MissingContract(i.At,"Unobserved Moved virtual slot target");throw new MissingContract(i.At,"Actual Position48/Vector60 and saved066EE0 slot binding established; real Moved callback and floor/frame/publication dependencies remain unimplemented");
  }
 }
}
