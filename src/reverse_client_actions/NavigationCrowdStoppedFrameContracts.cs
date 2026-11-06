using System;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 public sealed class NavigationCrowdStoppedFrameBoundaries {
  public readonly NavigationCrowdProximityBoundaries Proximity;
  public readonly NavigationCrowdAvoidanceConstants Constants;
  public readonly NavigationPoint3 Global422980;
  public readonly Field<float> CorrectionNeighborFactor34BC38=new Field<float>();
  public NavigationCrowdStoppedFrameBoundaries(NavigationCrowdProximityBoundaries proximity,NavigationCrowdAvoidanceConstants constants,NavigationPoint3 global422980){if(global422980==null)throw new ArgumentNullException("global422980");Proximity=proximity;Constants=constants;Global422980=global422980;}
  public void UseObservedDiskConstant(){CorrectionNeighborFactor34BC38.Write(NavigationCoreContracts.FromBits(0x40800000));}
 }
 public sealed class NavigationCrowdStoppedFrameInvocation {
  public readonly Invocation Native=new Invocation();
  public readonly Field<float> MaximumRadius=new Field<float>(),CenterX=new Field<float>(),CenterZ=new Field<float>();
  public bool InsertReturned{get;internal set;}public bool UpdateReturned{get;internal set;}
  public bool NeighborQueryReturned{get;internal set;}public bool EmptyNeighbors{get;internal set;}
  public bool GapStored{get;internal set;}public bool WorkCleared{get;internal set;}public bool SuffixReturned{get;internal set;}
  public bool CompleteFrameReturned{get{return false;}}
  // Receipt reset only; this never changes a native-model Agent or group.
  public void Reset(){Native.At=0;MaximumRadius.Forget();CenterX.Forget();CenterZ.Forget();InsertReturned=UpdateReturned=NeighborQueryReturned=EmptyNeighbors=GapStored=WorkCleared=SuffixReturned=false;}
 }
 // Reached25C8C2 continuation ONLY, not a complete25C440 frame. Evidence:
 // r1-crowd-movement-direct/08-CrowdMovementDirect-0x18025c440-procedure_assembly.json
 // SHA62da8b3465041af3522b3e36a72e4680485d0c9b2a832ab8b3207ce27bf5b8ae;
 // r1-crowd-tick-operands/inspection.json
 // SHAa61c64361ffb10912b26e103456f12fc1b26b7434f9d1c17b5dac9c2ee112529.
 // One stable live OWN Member, State3 and already prepared proximity/bounds;
 // prior projection, previous-position copy, captured member count and native
 // register/FP state are NOT established here. Operands are explicit stable
 // OWN inputs; no global or flag receives a default. No navigation-query stub.
 // Work0 skips steering/integration but still executes all-member correction.
 // Nonempty correction and correction-produced Work remain Unsupported.
 public static class NavigationCrowdStoppedFrameContracts {
  static T Need<T>(T x,uint at)where T:class{if(x==null)throw new MissingContract(at,"Null OWN stopped suffix view");var v=x as NavigationView;if(v!=null)v.RequireLive(at);return x;}
  static float Scalar(float x,uint at){uint u=NavigationCoreContracts.ToBits(x);if((u&0x7f800000u)==0x7f800000u||((u&0x7f800000u)==0&&(u&0x7fffffu)!=0))throw new MissingContract(at,"OWN stopped suffix finite normal/zero FP domain");return x;}
  [MethodImpl(MethodImplOptions.NoInlining)] static float F(double x,uint at){if(x!=0&&Math.Abs(x)<1.1754943508222875E-38)throw new MissingContract(at,"OWN stopped suffix excludes subnormal intermediates");return Scalar(NavigationCoreContracts.F((float)x),at);}
  static float R(Field<float> f,Invocation i,uint at){i.At=at;return Scalar(Need(f,at).Read(at),at);}
  static float Raw(Field<uint> f,Invocation i,uint at){i.At=at;return Scalar(NavigationCoreContracts.FromBits(Need(f,at).Read(at)),at);}
  static int Trunc(float x,Invocation i,uint at){i.At=at;if((double)x<Int32.MinValue||(double)x>=2147483648.0)throw new MissingContract(at,"OWN stopped suffix CVTT outside signed32 domain");return (int)Math.Truncate(x);}
  static long Count(NavigationCrowdPointerHeader<NavigationCrowdAgent> h,uint load,uint sub){var e=h.Capture(NavigationCrowdPointerSlot.End,load).Read(sub);var b=h.Read(NavigationCrowdPointerSlot.Begin,sub);if(e==null&&b==null)return 0;if(e==null||b==null||!Object.ReferenceEquals(e.Buffer,b.Buffer))throw new MissingContract(sub,"OWN pointer subtraction requires one allocation");e.Buffer.RequireLive(sub);long n=(long)e.Index-b.Index;if(n<0)throw new MissingContract(sub,"Reversed OWN pointer extent");return n;}
  static void CopyBits(Field<uint> to,Field<float> bits){try{to.Write(NavigationCoreContracts.ToBits(bits.Read(0)));}catch(MissingContract){to.Forget();}}
  static void BoundsAndUpdate(NavigationCrowdGroup g,NavigationCrowdAgent a,Field<float> half,GameMovementCallbackContext callbacks,NavigationCrowdStoppedFrameInvocation c){
   var i=c.Native;i.At=0x25c8d7;var radius=a.Radius10.Snapshot();i.At=0x25c8e0;var xb=a.Position48.X.Snapshot();i.At=0x25c8e5;var zb=a.Position48.Z.Snapshot();i.At=0x25c8ed;var yb=a.Position48.Y.Snapshot();
   float x=R(xb,i,0x25c8f2),r=R(radius,i,0x25c8f2),loX=F((double)x-r,i.At),y=R(yb,i,0x25c8f6);int ly=Trunc(y,i,0x25c8f6);
   y=F((double)y+R(a.Height14,i,0x25c8fa),i.At);int lx=Trunc(loX,i,0x25c8ff);i.At=0x25c906;x=F((double)x+r,i.At);float z=R(zb,i,0x25c90a),loZ=F((double)z-r,i.At);i.At=0x25c90e;z=F((double)z+r,i.At);
   y=F((double)y+R(half,i,0x25c912),i.At);x=F((double)x+R(half,i,0x25c917),i.At);int lz=Trunc(loZ,i,0x25c91c),uy=Trunc(y,i,0x25c921);
   i.At=0x25c926;lz=Math.Min(lz,g.Lower10.Z.Read(i.At));i.At=0x25c92f;ly=Math.Min(ly,g.Lower10.Y.Read(i.At));i.At=0x25c938;int oldX=g.Lower10.X.Read(i.At);z=F((double)z+R(half,i,0x25c93c),i.At);lx=Math.Min(lx,oldX);
   i.At=0x25c954;g.Lower10.X.Write(lx);g.Lower10.Y.Write(ly);i.At=0x25c95a;g.Lower10.Z.Write(lz);int uz=Trunc(z,i,0x25c95e),ux=Trunc(x,i,0x25c963);
   i.At=0x25c968;uz=Math.Max(uz,g.Upper1C.Z.Read(i.At));i.At=0x25c971;uy=Math.Max(uy,g.Upper1C.Y.Read(i.At));i.At=0x25c97f;ux=Math.Max(ux,g.Upper1C.X.Read(i.At));i.At=0x25c993;g.Upper1C.X.Write(ux);g.Upper1C.Y.Write(uy);i.At=0x25c999;g.Upper1C.Z.Write(uz);
   i.At=0x25c99d;var maxInput=a.Radius10.Snapshot();i.At=0x25c9a1;var callback=a.Callback08.Snapshot();float mr=R(maxInput,i,0x25c9a5),maximum=mr>0?mr:0;
   i.At=0x25c9a9;var view=callback.Read(i.At) as GameMovementInterfaceView;var table=Need(view,i.At).Virtual38.Snapshot();i.At=0x25c9ac;c.MaximumRadius.Write(maximum);i.At=0x25c9af;object target=table.Read(i.At);
   if(!Object.ReferenceEquals(target,GameMovementInterfaceTable.Player)&&!Object.ReferenceEquals(target,GameMovementInterfaceTable.Character))throw new MissingContract(i.At,"Unknown movement Update vtable");
   GameMovementCallbackContracts.CharacterUpdate066D60(view,new GameMovementParameters(a),callbacks,i);c.UpdateReturned=true;i.At=0x25c9b2;
  }
  public static void FinishReachedSingleState3NoWork25C8C2(NavigationCrowdGroup group,NavigationCrowdAgent agent,NavigationCrowdPointerDomain<NavigationCrowdAgent> domain,NavigationCrowdStoppedFrameBoundaries boundaries,GameMovementCallbackContext callbacks,NavigationCrowdStoppedFrameInvocation invocation){
   FinishReachedSingleState3NoWorkCore(group,agent,domain,boundaries,callbacks,null,null,invocation);
  }
  // Only the real common prefix supplies these XMM8/XMM13 captures. Work0
  // bypasses25DB22, so the initial25C4A1 threshold must not be reloaded here.
  public static void FinishReachedSingleState3NoWorkCaptured25C8C2(NavigationCrowdGroup group,NavigationCrowdAgent agent,NavigationCrowdPointerDomain<NavigationCrowdAgent> domain,NavigationCrowdStoppedFrameBoundaries boundaries,GameMovementCallbackContext callbacks,Field<float> half,Field<float> threshold,NavigationCrowdStoppedFrameInvocation invocation){
   Need(half,0x25c8c2);Need(threshold,0x25c8c2);FinishReachedSingleState3NoWorkCore(group,agent,domain,boundaries,callbacks,half,threshold,invocation);
  }
  static void FinishReachedSingleState3NoWorkCore(NavigationCrowdGroup group,NavigationCrowdAgent agent,NavigationCrowdPointerDomain<NavigationCrowdAgent> domain,NavigationCrowdStoppedFrameBoundaries boundaries,GameMovementCallbackContext callbacks,Field<float> capturedHalf,Field<float> capturedThreshold,NavigationCrowdStoppedFrameInvocation invocation){
   var c=Need(invocation,0x25c8c2);c.Reset();var i=c.Native;i.At=0x25c8c2;var g=Need(group,i.At);var a=Need(agent,i.At);var b=Need(boundaries,i.At);Need(domain,i.At);
   // OWN continuation-domain guard, not an invented native precondition branch.
   var members=NavigationCrowdPointerHeader<NavigationCrowdAgent>.BorrowTyped(g.Members28);if(Count(members,i.At,i.At)!=1)throw new MissingContract(i.At,"Stopped suffix requires one stable actual Member");var mp=members.Read(NavigationCrowdPointerSlot.Begin,i.At);
   if(!Object.ReferenceEquals(mp.Buffer.At(mp.Index,i.At).Read(i.At),a)||a.State70.Read(i.At)!=3)throw new MissingContract(i.At,"Stopped suffix requires this same actual State3 Member");
   // The bounds and proximity bodies borrow one34BADC cell through the
   // registration-state view. Constants.Half34BADC is not another global.
   var constants=Need(b.Constants,i.At);var half=capturedHalf??Need(b.Proximity,i.At).Half34BADC.Snapshot();var threshold=capturedThreshold??constants.SpeedTolerance34B9B0.Snapshot();
   if((a.Flags44.Read(i.At)&0x1000)==0){i.At=0x25c8d2;NavigationCrowdProximityContracts.Insert25EA00(new NavigationCrowdProximityIndex(g),a,b.Proximity,i);c.InsertReturned=true;}
   BoundsAndUpdate(g,a,half,callbacks,c);
   i.At=0x25c9c6;int lowerX=g.Lower10.X.Read(i.At);i.At=0x25c9ca;int upperX=g.Upper1C.X.Read(i.At);var work=NavigationCrowdBorrowedPointerContracts.Work40(g,domain);i.At=0x25c9d4;long workCount=Count(work,0x25c9ce,i.At);
   i.At=0x25c9d7;constants.One34BB44.Snapshot();i.At=0x25c9e4;int lowerZ=g.Lower10.Z.Read(i.At);i.At=0x25c9f8;int upperZ=g.Upper1C.Z.Read(i.At);
   float fx=F(lowerX,0x25c9e8),wx=F(unchecked(upperX-lowerX),0x25ca18);float cx=F((double)wx*R(half,i,0x25ca1f),i.At);i.At=0x25ca27;cx=F((double)fx+cx,i.At);float wz=F(unchecked(upperZ-lowerZ),0x25ca24),cz=F((double)wz*R(half,i,0x25ca2b),i.At);i.At=0x25ca30;c.CenterX.Write(cx);float fz=F(lowerZ,0x25ca3a);i.At=0x25ca3d;cz=F((double)fz+cz,i.At);i.At=0x25ca41;c.CenterZ.Write(cz);
   i.At=0x25ca46;if(workCount!=0)throw new MissingContract(i.At,"Nonempty existing Work requires the real steering/integration path; it is not cleared here");
   i.At=0x25d2ef; // actual Work0 edge still enters the all-Member correction loop.
   i.At=0x25db49;var neighborFactor=b.CorrectionNeighborFactor34BC38.Snapshot();
   i.At=0x25db70;var correctionBegin=members.Capture(NavigationCrowdPointerSlot.Begin,i.At);
   i.At=0x25db74;var gx=b.Global422980.X.Snapshot();var gy=b.Global422980.Y.Snapshot();
   i.At=0x25db7c;var correctionMember=Need(correctionBegin.Read(i.At),i.At);if(!Object.ReferenceEquals(correctionMember.Buffer.At(correctionMember.Index,i.At).Read(i.At),a))throw new MissingContract(i.At,"Correction reloaded a different Member after Update; stable single-member domain ended");
   i.At=0x25db87;CopyBits(a.GapD0,gx);CopyBits(a.GapD4,gy);i.At=0x25db8f;var gz=b.Global422980.Z.Snapshot();i.At=0x25db95;CopyBits(a.GapD8,gz);c.GapStored=true;
   i.At=0x25db9b;uint flags=a.Flags44.Read(i.At);
   if((flags&0x801)==1){
    i.At=0x25dbac;if(a.State70.Read(i.At)!=3)throw new MissingContract(i.At,"State changed outside stable stopped suffix domain");
    i.At=0x25dbb6;var radius=a.Radius10.Snapshot();var neighbors=NavigationCrowdBorrowedPointerContracts.Neighbors88(a,domain);neighbors.CopyBeginToEnd(0x25dbc2,0x25dbcc);i.At=0x25dbd0;var mask=a.ParameterBits1C40[2].Snapshot();
    float r=R(radius,i,0x25dbd3);float sum=F((double)r+R(c.MaximumRadius,i,0x25dbd3),i.At);i.At=0x25dbd8;var height=a.Height14.Snapshot();float product=F((double)R(radius,i,0x25dbe6)*R(neighborFactor,i,0x25dbe6),i.At);i.At=0x25dbf4;float range=product>sum?product:sum;
    i.At=0x25dbf8;NavigationCrowdProximityContracts.NeighborsCapturedBorrowed25EB90(new NavigationCrowdProximityIndex(g),a.Position48,Field<float>.Of(range),height,mask,a,neighbors,b.Proximity,i);c.NeighborQueryReturned=true;i.At=0x25dc06;long n=Count(neighbors,0x25dbfd,i.At);
    i.At=0x25dc14;if(n!=0){i.At=0x25dc20;throw new MissingContract(i.At,"Nonempty correction/slot30 is not implemented by the stopped empty-neighbor suffix");}c.EmptyNeighbors=true;
    i.At=0x25dd7b;if(a.State70.Read(i.At)!=2){i.At=0x25dd81;if((a.Flags44.Read(i.At)&0x1000)==0){var zbits=a.GapD8.Snapshot();var xbits=a.GapD0.Snapshot();float x=Raw(xbits,i,0x25dd9e);x=F((double)x*x,i.At);float z=Raw(zbits,i,0x25dda2);z=F((double)z*z,i.At);i.At=0x25dda6;float length=F((double)x+z,i.At);float limit=R(threshold,i,0x25ddaa);if(!(length<limit)){i.At=0x25ddba;throw new MissingContract(i.At,"Nonzero correction needs real Work append and non-State2 Moved publication");}}}
   }
   i.At=0x25ddc8;i.At=0x25ddd8;i.At=0x25dde1; // captured Work count is still zero.
   work.CopyBeginToEnd(0x25e2b1,0x25e2eb);i.At=0x25e2eb;c.WorkCleared=true;i.At=0x25e2fb;c.SuffixReturned=true;
  }
 }
}
