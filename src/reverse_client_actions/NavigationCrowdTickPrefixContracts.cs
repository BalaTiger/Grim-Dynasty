using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 public sealed class NavigationCrowdTickPrefixBoundaries {
  public readonly NavigationCrowdTickProductionBoundaries Production;
  public readonly NavigationCrowdRaycastBoundaries Raycast;
  // NoDiskBytes in r1-crowd-tick-operands: explicit OWN state only. These are
  // integer bounds seeds, not a guessed zero vector or another Agent position.
  public readonly NavigationCrowdInt3 Global4228C0=new NavigationCrowdInt3();
  public readonly Field<float> NeighborFactor34BC64=new Field<float>();
  public NavigationCrowdTickPrefixBoundaries(NavigationCrowdTickProductionBoundaries production,NavigationCrowdRaycastBoundaries raycast){Production=production;Raycast=raycast;}
  public void UseObservedDiskConstants(){NeighborFactor34BC64.Write(NavigationCoreContracts.FromBits(0x41000000));}
 }
 public sealed class NavigationCrowdTickPrefixInvocation {
  public readonly Invocation Native=new Invocation();
  public GameMovedInvocation EarlyMoved {get;private set;}
  public NavigationCrowdSynchronousReplanInvocation SynchronousReplan {get;internal set;}
  public readonly Field<float> Seconds=new Field<float>(),MaximumRadius=new Field<float>(),NeighborFactor=new Field<float>(),CenterX=new Field<float>(),CenterZ=new Field<float>();
  public NavigationCrowdAgent Agent {get;internal set;}
  public bool Reached25CB73 {get;internal set;}
  public bool EarlyMovedReturned {get;internal set;}
  public bool UpdateReturned {get;internal set;}
  public bool RaycastCalled {get;internal set;}
  public bool RaycastReturned {get;internal set;}
  public bool WaypointPopped {get;internal set;}
  readonly List<uint> steps=new List<uint>();
  public uint[] ObservedAddresses {get{return steps.ToArray();}}
  public NavigationCrowdTickPrefixInvocation(){Reset();}
  internal void Reset(){Native.At=0;EarlyMoved=new GameMovedInvocation();SynchronousReplan=null;Seconds.Forget();MaximumRadius.Forget();NeighborFactor.Forget();CenterX.Forget();CenterZ.Forget();Agent=null;Reached25CB73=false;EarlyMovedReturned=false;UpdateReturned=false;RaycastCalled=false;RaycastReturned=false;WaypointPopped=false;steps.Clear();}
  internal void At(uint at){Native.At=at;steps.Add(at);}
 }
 // Actual saved25C440 prefix through the nonempty-waypoint edge to25CB73.
 // Bounded OWN domain: one State2 Member, optional synchronous replan, one
 // resulting Work. State1 and asynchronous request creation remain Unsupported.
 // No Work reset occurs at entry: the real preceding tail must leave it empty.
 // Its native End read is not moved earlier merely to validate this precondition.
 // Created is a registration callback outside this body; callers may invoke the
 // existing real25AF13/066C20 before entry, retaining its actual flags0x481.
 // Stable concrete adapter bindings33A6C0, typed lifetimes, finite normal/zero
 // binary32 and representable CVTT results only. No injected success callbacks,
 // native thread/heap/EH/FP-state or arbitrary pointer alias claims.
 public static class NavigationCrowdTickPrefixContracts {
  static T Need<T>(T x,uint at)where T:class{if(x==null)throw new MissingContract(at,"Null OWN tick prefix view");var v=x as NavigationView;if(v!=null)v.RequireLive(at);return x;}
  static float Scalar(float x,uint at){uint u=NavigationCoreContracts.ToBits(x);if((u&0x7f800000u)==0x7f800000u||((u&0x7f800000u)==0&&(u&0x7fffffu)!=0))throw new MissingContract(at,"OWN prefix finite normal/zero floating domain");return x;}
  [MethodImpl(MethodImplOptions.NoInlining)] static float F(double x,uint at){if(x!=0&&Math.Abs(x)<1.1754943508222875E-38)throw new MissingContract(at,"OWN prefix excludes subnormal intermediates");return Scalar(NavigationCoreContracts.F((float)x),at);}
  static float R(Field<float> x,NavigationCrowdTickPrefixInvocation c,uint at){c.At(at);return Scalar(Need(x,at).Read(at),at);}
  static float Add(float x,float y,NavigationCrowdTickPrefixInvocation c,uint at){c.At(at);return F((double)x+y,at);}
  static float Sub(float x,float y,NavigationCrowdTickPrefixInvocation c,uint at){c.At(at);return F((double)x-y,at);}
  static float Mul(float x,float y,NavigationCrowdTickPrefixInvocation c,uint at){c.At(at);return F((double)x*y,at);}
  static int Trunc(float x,NavigationCrowdTickPrefixInvocation c,uint at){c.At(at);if((double)x<Int32.MinValue||(double)x>=2147483648.0)throw new MissingContract(at,"OWN prefix CVTT result outside representable signed32 domain");return (int)Math.Truncate(x);}
  static long Difference(NavigationCrowdPointerAddress<NavigationCrowdAgent> end,NavigationCrowdPointerAddress<NavigationCrowdAgent> begin,uint at){if(end==null&&begin==null)return 0;if(end==null||begin==null||!Object.ReferenceEquals(end.Buffer,begin.Buffer))throw new MissingContract(at,"OWN stride8 subtraction requires the same allocation");return (long)end.Index-begin.Index;}
  static NavigationCrowdPointAddress Point(object raw,uint at){if(raw==null)return null;var p=raw as NavigationCrowdPointAddress;if(p==null)throw new MissingContract(at,"Waypoint is not an OWN stride12 address");return p;}
  static long PointDifference(object end,object begin,uint at){var e=Point(end,at);var b=Point(begin,at);if(e==null&&b==null)return 0;if(e==null||b==null)throw new MissingContract(at,"OWN waypoint subtraction requires one allocation");try{if(!Object.ReferenceEquals(e.Buffer,b.Buffer))throw new MissingContract(at,"Different OWN waypoint allocations");return e.PointIndex-b.PointIndex;}catch(MissingContract ex){throw new MissingContract(at,ex.Message,ex);}}
  static bool SamePoint(object end,object begin,uint at){var e=Point(end,at);var b=Point(begin,at);if(e==null||b==null)return e==null&&b==null;try{return Object.ReferenceEquals(e.Buffer,b.Buffer)&&e.PointIndex==b.PointIndex;}catch(MissingContract ex){throw new MissingContract(at,ex.Message,ex);}}
  static void Table(Field<object> bits,uint at){object t=bits.Read(at);if(!Object.ReferenceEquals(t,GameMovementInterfaceTable.Character)&&!Object.ReferenceEquals(t,GameMovementInterfaceTable.Player))throw new MissingContract(at,"Unobserved Game movement interface target");}
  static GameMovementInterfaceView View(Field<object> bits,uint at){var v=bits.Read(at) as GameMovementInterfaceView;Need(v,at).FullOwner.Entity.RequireLive(at);return v;}
  static NavigationCrowdAdapterState Query(NavigationCrowdGroup group,NavigationCrowdTickPrefixInvocation c,uint load,uint dereference){c.At(load);var q=Need(group,load).Query08.Snapshot();c.At(dereference);return Need(q.Read(dereference),dereference);}
  static void CopyPoint(NavigationPoint3 to,NavigationPoint3 from,NavigationCrowdTickPrefixInvocation c,uint xyLoad,uint xyStore,uint zLoad,uint zStore){c.At(xyLoad);var x=Need(from,xyLoad).X.Snapshot();var y=from.Y.Snapshot();c.At(xyStore);Need(to,xyStore).X.CopyFrom(x);to.Y.CopyFrom(y);c.At(zLoad);var z=from.Z.Snapshot();c.At(zStore);to.Z.CopyFrom(z);}
  static void ProjectionFailed(NavigationCrowdAgent a,NavigationCrowdTickPrefixBoundaries b,NavigationCrowdTickPrefixInvocation c){
   c.At(0x25c615);var callback=a.Callback08.Snapshot();c.At(0x25c61d);a.State70.Write(6);c.At(0x25c62a);a.Reason74.Write(5);
   CopyPoint(a.VelocityB8,b.Production.Global422980,c,0x25c631,0x25c639,0x25c641,0x25c647);c.At(0x25c64d);a.Timer6C.Write(0);
   c.At(0x25c654);var view=View(callback,c.Native.At);var table=view.Virtual38.Snapshot();c.At(0x25c657);Table(table,c.Native.At);
   throw new MissingContract(c.Native.At,"Projection failure slot38 real Game body is outside this ready prefix; state6/reason5 prefix retained");
  }
  static void Project(NavigationCrowdGroup g,NavigationCrowdAgent a,Field<float> ten,NavigationCrowdTickPrefixBoundaries b,GameMovedContext game,NavigationCrowdTickPrefixInvocation c){
   c.At(0x25c595);if((a.Flags44.Read(c.Native.At)&0x20)!=0)return;
   var q=Query(g,c,0x25c59f,0x25c5ae);var projected=new NavigationPoint3();c.At(0x25c5b1);uint reference=NavigationCrowdAdapterContracts.Project030B30(q,a.Position48,projected,b.Production.Adapter,c.Native);
   if(reference==0){q=Query(g,c,0x25c5b8,0x25c5c9);c.At(0x25c5cc);if(NavigationCrowdAdapterContracts.Project030BB0(q,a.Position48,ten,projected,b.Production.Adapter,c.Native)==0){ProjectionFailed(a,b,c);return;}}
   c.At(0x25c5d3);var x=projected.X.Snapshot();bool changed=R(x,c,0x25c5d9)!=R(a.Position48.X,c,0x25c5d9);
   if(!changed){c.At(0x25c5df);var z=projected.Z.Snapshot();changed=R(z,c,0x25c5e5)!=R(a.Position48.Z,c,0x25c5e5);}
   CopyPoint(a.Position48,projected,c,0x25c5ed,0x25c5f3,0x25c5f8,0x25c5fc);
   if(changed){c.At(0x25c603);var callback=a.Callback08.Snapshot();c.At(0x25c60d);var view=View(callback,c.Native.At);var table=view.Virtual38.Snapshot();c.At(0x25c610);Table(table,c.Native.At);
    try{GameMovedContracts.CharacterMoved066EE0(view,0,a,game,c.EarlyMoved);}finally{c.Native.At=c.EarlyMoved.Native.At;}
    c.EarlyMovedReturned=true;c.At(0x25c613);
   }
  }
  static void BoundsAndUpdate(NavigationCrowdGroup g,NavigationCrowdAgent a,Field<float> half,GameMovedContext game,NavigationCrowdTickPrefixInvocation c){
   c.At(0x25c8d7);var radius=a.Radius10.Snapshot();c.At(0x25c8e0);var xb=a.Position48.X.Snapshot();c.At(0x25c8e5);var zb=a.Position48.Z.Snapshot();c.At(0x25c8ed);var yb=a.Position48.Y.Snapshot();
   float x=R(xb,c,0x25c8f2),r=R(radius,c,0x25c8f2);float loX=Sub(x,r,c,0x25c8f2);float y=R(yb,c,0x25c8f6);int ly=Trunc(y,c,0x25c8f6);
   y=Add(y,R(a.Height14,c,0x25c8fa),c,0x25c8fa);int lx=Trunc(loX,c,0x25c8ff);x=Add(x,r,c,0x25c906);float z=R(zb,c,0x25c90a);float loZ=Sub(z,r,c,0x25c90a);z=Add(z,r,c,0x25c90e);
   y=Add(y,R(half,c,0x25c912),c,0x25c912);x=Add(x,R(half,c,0x25c917),c,0x25c917);int lz=Trunc(loZ,c,0x25c91c),uy=Trunc(y,c,0x25c921);
   c.At(0x25c926);lz=Math.Min(lz,g.Lower10.Z.Read(c.Native.At));c.At(0x25c92f);ly=Math.Min(ly,g.Lower10.Y.Read(c.Native.At));c.At(0x25c938);int previousX=g.Lower10.X.Read(c.Native.At);z=Add(z,R(half,c,0x25c93c),c,0x25c93c);lx=Math.Min(lx,previousX);
   c.At(0x25c954);g.Lower10.X.Write(lx);g.Lower10.Y.Write(ly);c.At(0x25c95a);g.Lower10.Z.Write(lz);int uz=Trunc(z,c,0x25c95e),ux=Trunc(x,c,0x25c963);
   c.At(0x25c968);uz=Math.Max(uz,g.Upper1C.Z.Read(c.Native.At));c.At(0x25c971);uy=Math.Max(uy,g.Upper1C.Y.Read(c.Native.At));c.At(0x25c97f);ux=Math.Max(ux,g.Upper1C.X.Read(c.Native.At));
   c.At(0x25c993);g.Upper1C.X.Write(ux);g.Upper1C.Y.Write(uy);c.At(0x25c999);g.Upper1C.Z.Write(uz);
   c.At(0x25c99d);var maxInput=a.Radius10.Snapshot();c.At(0x25c9a1);var callback=a.Callback08.Snapshot();float mr=R(maxInput,c,0x25c9a5);float maximum=mr>0?mr:0; // XMM6 starts positive zero; MAXSS chooses second on equality.
   c.At(0x25c9a9);var view=View(callback,c.Native.At);var table=view.Virtual38.Snapshot();c.At(0x25c9ac);c.MaximumRadius.Write(maximum);c.At(0x25c9af);Table(table,c.Native.At);
   GameMovementCallbackContracts.CharacterUpdate066D60(view,new GameMovementParameters(a),Need(game,c.Native.At).Callbacks,c.Native);c.UpdateReturned=true;c.At(0x25c9b2);
  }
  static void Waypoints(NavigationCrowdGroup g,NavigationCrowdAgent a,Field<float> ten,NavigationCrowdTickPrefixBoundaries b,NavigationCrowdTickPrefixInvocation c){
   c.At(0x25ca77);uint flags=a.Flags44.Read(c.Native.At);var request=a.RequestE8;var points=new NavigationCrowdWaypointVector(request.WaypointVector38);var path=b.Production.Path;
   if((flags&0x80)!=0){var q=Query(g,c,0x25caa3,0x25caa8);c.At(0x25caab);byte straight=NavigationCrowdPathContracts.StraightPath030900(q,a.Position48,a.Target78,request.RefVector20,points,path,c.Native);
    if(straight!=0){c.At(0x25cabd);NavigationCrowdPathContracts.TrimWaypoints25BA90(request,a.Position48,path,c.Native);c.At(0x25cac2);
     if((a.Flags44.Read(c.Native.At)&0x400)!=0){c.At(0x25cacb);var end=points.End08.Snapshot();c.At(0x25cad2);long count=PointDifference(end.Read(c.Native.At),points.Begin00.Read(c.Native.At),c.Native.At);
      if(unchecked((ulong)count)>1){var capturedQuery=Query(g,c,0x25caeb,0x25caf7); // Captured vtable belongs to this concrete observed adapter class.
       c.At(0x25cafa);var second=NavigationCrowdPathContracts.Second25C080(request,path,c.Native);c.At(0x25caff);var liveQuery=g.Query08.Snapshot();c.At(0x25cb0b);var actual=Need(liveQuery.Read(c.Native.At),c.Native.At);
       if(!Object.ReferenceEquals(capturedQuery,actual))throw new MissingContract(c.Native.At,"OWN stable adapter binding changed across second-waypoint helper");var secondPoint=second.AsPoint(c.Native.At);var raycast=Need(b.Raycast,c.Native.At);c.RaycastCalled=true;
       byte visible=NavigationCrowdRaycastContracts.CanTraverse0309F0(actual,a.Position48,secondPoint,raycast,c.Native);c.RaycastReturned=true;
       if(visible!=0){c.At(0x25cb19);NavigationCrowdPathContracts.PopWaypoint25BCF0(request,path,c.Native);c.WaypointPopped=true;}
      }
     }
    }
   }else{c.At(0x25cb45);NavigationCrowdPathContracts.Resize030C60(points,1,path,c.Native);c.At(0x25cb55);var output=points.Begin00.Snapshot();var q=Query(g,c,0x25cb5c,0x25cb60);c.At(0x25cb63);var p=Point(output.Read(c.Native.At),c.Native.At);
    byte ok=NavigationCrowdAdapterContracts.Project030BB0(q,a.Target78,ten,p==null?null:p.AsPoint(c.Native.At),b.Production.Adapter,c.Native);
    if(ok==0){c.At(0x25cb6a);var begin=points.Begin00.Snapshot();c.At(0x25cb6d);points.End08.CopyFrom(begin);}
   }
   c.At(0x25cb28);var finalEnd=points.End08.Snapshot();c.At(0x25cb2c);if(SamePoint(finalEnd.Read(c.Native.At),points.Begin00.Read(c.Native.At),c.Native.At)){c.At(0x25cb31);a.NeedReplan140.Write(1);c.At(0x25cf79);throw new MissingContract(c.Native.At,"Empty waypoint takes the actual steering-skip edge; nonempty producer cannot be invoked");}
  }
  public static void RunSingleReady25C440(NavigationCrowdGroup group,NavigationCrowdAgent agent,NavigationCrowdPointerDomain<NavigationCrowdAgent> domain,int ticks,NavigationCrowdTickPrefixBoundaries boundaries,GameMovedContext game,NavigationCrowdTickPrefixInvocation invocation){
   var c=Need(invocation,0x25c440);c.Reset();var i=c.Native;c.At(0x25c461);var g=Need(group,i.At);var members=NavigationCrowdPointerHeader<NavigationCrowdAgent>.BorrowTyped(g.Members28);var end=members.Capture(NavigationCrowdPointerSlot.End,i.At);c.At(0x25c469);long memberCount=Difference(end.Read(i.At),members.Read(NavigationCrowdPointerSlot.Begin,i.At),i.At);
   c.At(0x25c4a1);var b=Need(boundaries,i.At);var production=Need(b.Production,i.At);var constants=Need(production.Constants,i.At);var scale=constants.SpeedTolerance34B9B0.Snapshot();
   var work=NavigationCrowdBorrowedPointerContracts.Work40(g,domain);c.At(0x25c4aa);var capacity=work.Capture(NavigationCrowdPointerSlot.Capacity,i.At);c.At(0x25c4ae);long workCapacity=Difference(capacity.Read(i.At),work.Read(NavigationCrowdPointerSlot.Begin,i.At),i.At);
   c.At(0x25c4b5);if(memberCount!=1)throw new MissingContract(i.At,"OWN prefix currently requires exactly one actual Member; no multi-agent ordering inferred");
   c.At(0x25c4bd);float tickFloat=F(ticks,i.At);float seconds=Mul(tickFloat,R(scale,c,0x25c4d2),c,0x25c4d2);c.At(0x25c4d7);c.Seconds.Write(seconds);
   c.At(0x25c4e0);if(unchecked((ulong)workCapacity)<1){c.At(0x25c4eb);NavigationCrowdBorrowedPointerContracts.Reallocate034FE0(work,1,i);}
   c.At(0x25c50e);NavigationCrowdProximityContracts.Prepare25E900(new NavigationCrowdProximityIndex(g),1,i);
   c.At(0x25c513);var seedX=b.Global4228C0.X.Snapshot();var seedY=b.Global4228C0.Y.Snapshot();c.At(0x25c51f);var ten=constants.Ten34BC70.Snapshot();c.At(0x25c528);c.MaximumRadius.Write(0);c.At(0x25c52c);var half=constants.Half34BADC.Snapshot();
   c.At(0x25c535);g.Lower10.X.CopyFrom(seedX);g.Lower10.Y.CopyFrom(seedY);c.At(0x25c53b);var seedZ=b.Global4228C0.Z.Snapshot();c.At(0x25c541);g.Lower10.Z.CopyFrom(seedZ);
   c.At(0x25c545);seedX=b.Global4228C0.X.Snapshot();seedY=b.Global4228C0.Y.Snapshot();c.At(0x25c54d);g.Upper1C.X.CopyFrom(seedX);g.Upper1C.Y.CopyFrom(seedY);c.At(0x25c553);seedZ=b.Global4228C0.Z.Snapshot();c.At(0x25c559);g.Upper1C.Z.CopyFrom(seedZ);
   c.At(0x25c570);var begin=members.Capture(NavigationCrowdPointerSlot.Begin,i.At);c.At(0x25c574);var p=Need(begin.Read(i.At),i.At);var a=Need(p.Buffer.At(p.Index,i.At).Read(i.At),i.At);if(!Object.ReferenceEquals(a,agent))throw new MissingContract(i.At,"Caller agent differs from the actual single Member");c.Agent=a;
   CopyPoint(a.PreviousC4,a.Position48,c,0x25c57f,0x25c584,0x25c58c,0x25c58f);Project(g,a,ten,b,game,c);
   c.At(0x25c65a);if(a.State70.Read(i.At)==1){c.At(0x25c664);throw new MissingContract(i.At,"State1 request-completion/lock prefix lies outside the ready State2 domain");}
   c.At(0x25c710);if(a.State70.Read(i.At)!=2){c.At(0x25c8c2);throw new MissingContract(i.At,"Non-State2 enters another actual membership path; ready producer not established");}
   c.At(0x25c71a);if(a.NeedReplan140.Read(i.At)!=0){c.At(0x25c727);c.SynchronousReplan=new NavigationCrowdSynchronousReplanInvocation();NavigationCrowdSynchronousReplanContracts.Run25C727(g,a,production,c.SynchronousReplan,i);}
   c.At(0x25c832);NavigationCrowdTickHeaderContracts.PublishReachedWorkAndProximity25C832(g,a,domain,production.Proximity,i);BoundsAndUpdate(g,a,half,game,c);
   c.At(0x25c9c6);int lowerX=g.Lower10.X.Read(i.At);c.At(0x25c9ca);int upperX=g.Upper1C.X.Read(i.At);c.At(0x25c9ce);var workEnd=work.Capture(NavigationCrowdPointerSlot.End,i.At);int widthX=unchecked(upperX-lowerX);c.At(0x25c9d4);long workCount=Difference(workEnd.Read(i.At),work.Read(NavigationCrowdPointerSlot.Begin,i.At),i.At);
   c.At(0x25c9d7);constants.One34BB44.Snapshot();c.At(0x25c9e4);int lowerZ=g.Lower10.Z.Read(i.At);c.At(0x25c9f8);int upperZ=g.Upper1C.Z.Read(i.At);int widthZ=unchecked(upperZ-lowerZ);
   float centerX=Add(F(lowerX,0x25c9e8),Mul(F(widthX,0x25ca18),R(half,c,0x25ca1f),c,0x25ca1f),c,0x25ca27);float centerZ=Mul(F(widthZ,0x25ca24),R(half,c,0x25ca2b),c,0x25ca2b);c.At(0x25ca30);c.CenterX.Write(centerX);centerZ=Add(F(lowerZ,0x25ca3a),centerZ,c,0x25ca3d);c.At(0x25ca41);c.CenterZ.Write(centerZ);
   c.At(0x25ca46);if(workCount==0){c.At(0x25d2ef);throw new MissingContract(i.At,"Actual empty Work skips steering; no single-Work producer invoked");}if(workCount!=1)throw new MissingContract(i.At,"OWN prefix requires one resulting Work; the prior tail must really clear Work");
   c.At(0x25ca4f);c.NeighborFactor.CopyFrom(b.NeighborFactor34BC64.Snapshot());c.At(0x25ca70);var wp=work.Capture(NavigationCrowdPointerSlot.Begin,i.At);c.At(0x25ca73);p=Need(wp.Read(i.At),i.At);if(!Object.ReferenceEquals(p.Buffer.At(p.Index,i.At).Read(i.At),a))throw new MissingContract(i.At,"Actual Work identity differs from the processed Member");
   Waypoints(g,a,ten,b,c);c.At(0x25cb73);c.Reached25CB73=true;
  }
 }
}
