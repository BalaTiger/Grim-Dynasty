using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 public sealed class NavigationCrowdTickTailInvocation {
  public readonly Invocation Native=new Invocation();
  readonly List<uint> steps=new List<uint>();
  public bool Entered {get;internal set;}
  public bool GoalPrefixWritten {get;internal set;}
  public bool ReferenceQueryReturned {get;internal set;}
  public uint QueriedReference {get;internal set;}
  public byte TrimResult {get;internal set;}
  public bool SpeedWritten {get;internal set;}
  public float MeasuredSpeed {get;internal set;}
  public bool StuckPrefixWritten {get;internal set;}
  public bool WorkCleared {get;internal set;}
  public bool Returned {get;internal set;}
  public bool FullTick {get{return false;}}
  public uint[] ObservedAddresses {get{return steps.ToArray();}}
  internal void Reset(){Native.At=0;steps.Clear();Entered=false;GoalPrefixWritten=false;ReferenceQueryReturned=false;QueriedReference=0;TrimResult=0;SpeedWritten=false;MeasuredSpeed=0;StuckPrefixWritten=false;WorkCleared=false;Returned=false;}
  internal void At(uint at){Native.At=at;steps.Add(at);}
 }
 // Saved Engine25C440 assembly, r1-crowd-movement-direct/08. This entry
 // assumes the real Moved call returned for the LAST Work element. The new
 // wrapper establishes that condition through the frozen single-Work producer;
 // direct callers declare a reached-block precondition, not a complete tick.
 // Stable typed OWN identities, finite normal/zero binary32, round-to-nearest;
 // no arbitrary aliases, concurrent mutation, native ABI/EH or MXCSR claims.
 // Flag80 calls the existing concrete query and reference trimming bodies.
 // Goal and stuck notifications stop at their actual indirect calls. Packed
 // DC/E0 uses the existing view's known-whole-word domain for partial writes;
 // an Unknown sibling is an explicit OWN representation limit, not native IO.
 public static class NavigationCrowdTickTailContracts {
  static T Need<T>(T x,uint at)where T:class{if(x==null)throw new MissingContract(at,"Null OWN tick tail view");var n=x as NavigationView;if(n!=null)n.RequireLive(at);return x;}
  static Field<float> Cell(NavigationCrowdAgent a,NavigationPoint3 p,int axis,uint at){Need(a,at);Need(p,at);try{return axis==0?p.X:axis==1?p.Y:p.Z;}catch(MissingContract e){throw new MissingContract(at,"Tail point outside declared own extent",e);}}
  static Field<float> Load(NavigationCrowdAgent a,NavigationPoint3 p,int axis,uint at,NavigationCrowdTickTailInvocation c){c.At(at);return Cell(a,p,axis,at).Snapshot();}
  static float Scalar(float x,uint at){uint bits=NavigationCoreContracts.ToBits(x);if((bits&0x7f800000u)==0x7f800000u||((bits&0x7f800000u)==0&&(bits&0x7fffffu)!=0))throw new MissingContract(at,"OWN tail finite normal/zero arithmetic domain");return x;}
  [MethodImpl(MethodImplOptions.NoInlining)] static float Round(double x,uint at){if(x!=0&&Math.Abs(x)<1.1754943508222875E-38)throw new MissingContract(at,"OWN tail domain excludes subnormal intermediates");return Scalar(NavigationCoreContracts.F((float)x),at);}
  static float Read(Field<float> x,uint at){return Scalar(x.Read(at),at);}
  static float Raw(Field<uint> x,uint at){return Scalar(NavigationCoreContracts.FromBits(x.Read(at)),at);}
  static float Sub(Field<float> a,Field<float> b,uint at,NavigationCrowdTickTailInvocation c){c.At(at);return Round((double)Read(a,at)-Read(b,at),at);}
  static float Difference(Field<float> a,NavigationCrowdAgent agent,int axis,uint at,NavigationCrowdTickTailInvocation c){c.At(at);return Sub(a,Cell(agent,agent.Position48,axis,at),at,c);}
  static float Mul(float a,float b,uint at,NavigationCrowdTickTailInvocation c){c.At(at);return Round((double)a*b,at);}
  static float Add(float a,float b,uint at,NavigationCrowdTickTailInvocation c){c.At(at);return Round((double)a+b,at);}
  static bool SameWaypoint(object a,object b,uint at){
   var x=a as NavigationCrowdPointAddress;var y=b as NavigationCrowdPointAddress;
   if((a!=null&&x==null)||(b!=null&&y==null))throw new MissingContract(at,"Tail comparison needs declared OWN waypoint addresses");
   if(x==null||y==null)return x==null&&y==null;
   try{return Object.ReferenceEquals(x.Buffer,y.Buffer)&&x.PointIndex==y.PointIndex;}catch(MissingContract e){throw new MissingContract(at,"Tail waypoint address unresolved at comparison",e);}
  }
  static void Goal(NavigationCrowdAgent a,NavigationCrowdTickProductionBoundaries b,NavigationCrowdTickTailInvocation c){
   c.At(0x25e127);var callback=Need(a,c.Native.At).Callback08.Snapshot();
   c.At(0x25e12f);a.State70.Write(4);a.Reason74.Write(0);c.GoalPrefixWritten=true;
   c.At(0x25e13a);var global=Need(b,c.Native.At).Global422980;Need(global,c.Native.At);var x=global.X.Snapshot();var y=global.Y.Snapshot();
   c.At(0x25e142);a.VelocityB8.X.CopyFrom(x);a.VelocityB8.Y.CopyFrom(y);
   c.At(0x25e14a);var z=global.Z.Snapshot();c.At(0x25e150);a.VelocityB8.Z.CopyFrom(z);
   c.At(0x25e156);a.Timer6C.Write(0);
   c.At(0x25e15a);var view=callback.Read(c.Native.At) as GameMovementInterfaceView;Need(view,c.Native.At).FullOwner.Entity.RequireLive(c.Native.At);var table=view.Virtual38.Snapshot();
   c.At(0x25e15d);object target=table.Read(c.Native.At);
   if(!Object.ReferenceEquals(target,GameMovementInterfaceTable.Character)&&!Object.ReferenceEquals(target,GameMovementInterfaceTable.Player))throw new MissingContract(c.Native.At,"Unobserved reached-goal slot28 target");
   throw new MissingContract(c.Native.At,"Reached-goal slot28 binds Game067170; its real action/controller/event body remains unimplemented");
  }
  static void Stuck(NavigationCrowdAgent a,NavigationCrowdTickProductionBoundaries b,NavigationCrowdTickTailInvocation c){
   c.At(0x25e229);var callback=Need(a,c.Native.At).Callback08.Snapshot();c.At(0x25e231);a.State70.Write(6);c.At(0x25e23e);a.Reason74.Write(3);c.StuckPrefixWritten=true;
   c.At(0x25e245);var global=Need(b,c.Native.At).Global422980;Need(global,c.Native.At);var x=global.X.Snapshot();var y=global.Y.Snapshot();c.At(0x25e24d);a.VelocityB8.X.CopyFrom(x);a.VelocityB8.Y.CopyFrom(y);
   c.At(0x25e255);var z=global.Z.Snapshot();c.At(0x25e25b);a.VelocityB8.Z.CopyFrom(z);c.At(0x25e261);a.Timer6C.Write(0);
   c.At(0x25e265);var view=callback.Read(c.Native.At) as GameMovementInterfaceView;Need(view,c.Native.At).FullOwner.Entity.RequireLive(c.Native.At);var table=view.Virtual38.Snapshot();c.At(0x25e268);table.Read(c.Native.At);
   throw new MissingContract(c.Native.At,"Actual stuck notification slot38 is unimplemented; State6 Reason3 and speed/global copies remain published");
  }
  static void LowSpeed(NavigationCrowdAgent a,Field<float> seconds,int ticks,float minimum,NavigationCrowdTickProductionBoundaries b,NavigationCrowdTickTailInvocation c){
   var x=Load(a,a.Position48,0,0x25e1b8,c);var y=Load(a,a.Position48,1,0x25e1bd,c);
   c.At(0x25e1c2);float dy=Sub(y,Cell(a,a.PreviousC4,1,c.Native.At),c.Native.At,c);c.At(0x25e1ca);float dx=Sub(x,Cell(a,a.PreviousC4,0,c.Native.At),c.Native.At,c);
   var z=Load(a,a.Position48,2,0x25e1d2,c);c.At(0x25e1d7);float dz=Sub(z,Cell(a,a.PreviousC4,2,c.Native.At),c.Native.At,c);
   dy=Mul(dy,dy,0x25e1df,c);dx=Mul(dx,dx,0x25e1e3,c);dz=Mul(dz,dz,0x25e1e7,c);float squared=Add(Add(dy,dx,0x25e1eb,c),dz,0x25e1ef,c);
   c.At(0x25e1f3);float distance=0;if(squared!=0){c.At(0x25e202);distance=Round(Math.Sqrt(squared),c.Native.At);}else c.At(0x25e1f9);
   c.At(0x25e206);float speed=Round((double)distance/Read(Need(seconds,c.Native.At),c.Native.At),c.Native.At);c.At(0x25e20b);bool slow=speed<minimum;
   var packed=new NavigationCrowdTickStateView(a).SpeedAndElapsed;
   c.At(0x25e20e);packed.WriteSpeedDC(speed,c.Native.At);c.SpeedWritten=true;c.MeasuredSpeed=speed;
   if(slow){c.At(0x25e218);int elapsed=unchecked(packed.ReadElapsedE0(c.Native.At)+ticks);packed.WriteElapsedE0(elapsed,c.Native.At);c.At(0x25e21e);elapsed=packed.ReadElapsedE0(c.Native.At);
    c.At(0x25e224);a.RequireLive(c.Native.At);int limit=unchecked((int)a.ParameterBits1C40[8].Read(c.Native.At));c.At(0x25e227);if(elapsed<=limit)return;
    Stuck(a,b,c);
   }
   c.At(0x25e26b);packed.WriteElapsedE0(0,c.Native.At);
  }
  public static void FinishReachedLastWork25E08A(NavigationCrowdGroup group,NavigationCrowdAgent agent,NavigationCrowdPointerDomain<NavigationCrowdAgent> domain,Field<float> capturedSeconds,int capturedTicks,NavigationCrowdTickProductionBoundaries production,NavigationCrowdTickTailInvocation invocation){
   var c=Need(invocation,0x25e08a);c.Reset();c.Entered=true;
   var tz=Load(agent,agent==null?null:agent.Target78,2,0x25e08a,c);
   var tx=Load(agent,agent.Target78,0,0x25e090,c);var ty=Cell(agent,agent.Target78,1,c.Native.At).Snapshot();
   c.At(0x25e099);var request=Need(agent.RequestE8,c.Native.At);var end=request.WaypointVector38.End08.Snapshot();
   c.At(0x25e0a6);request.RequireLive(c.Native.At);bool empty=SameWaypoint(request.WaypointVector38.Begin00.Read(c.Native.At),end.Read(c.Native.At),c.Native.At);
   Field<float> targetY;
   if(empty){c.At(0x25e0c2);targetY=ty.Snapshot();}
   else{c.At(0x25e0b6);var first=NavigationCrowdTickProductionContracts.First25BF40(request,production,c.Native);c.At(0x25e0bb);targetY=first.Cell(1,c.Native.At).Snapshot();}
   float dy=Difference(targetY,agent,1,0x25e0c8,c);
   float dx=Difference(tx,agent,0,0x25e0dc,c);
   c.At(0x25e0e1);agent.RequireLive(c.Native.At);var radius=agent.ParameterBits1C40[3].Snapshot();
   float dz=Difference(tz,agent,2,0x25e0e9,c);
   float hx=Difference(tx,agent,0,0x25e0ee,c);
   float hz=Difference(tz,agent,2,0x25e0f3,c);
   dy=Mul(dy,dy,0x25e0f8,c);dx=Mul(dx,dx,0x25e0fc,c);dz=Mul(dz,dz,0x25e100,c);float distance=Add(dy,dx,0x25e104,c);
   c.At(0x25e108);float r=Raw(radius,c.Native.At);float threshold=Mul(r,r,c.Native.At,c);distance=Add(distance,dz,0x25e10c,c);
   c.At(0x25e110);bool reached=distance<=threshold;
   if(!reached){hz=Mul(hz,hz,0x25e115,c);hx=Mul(hx,hx,0x25e119,c);float horizontal=Add(hz,hx,0x25e11d,c);c.At(0x25e121);reached=horizontal==0;}
   if(reached)Goal(agent,production,c);
   c.At(0x25e165);agent.RequireLive(c.Native.At);if((agent.Flags44.Read(c.Native.At)&0x80)!=0){
    c.At(0x25e16b);var query=Need(group,c.Native.At).Query08.Snapshot();c.At(0x25e176);var adapter=Need(query.Read(c.Native.At),c.Native.At);
    c.At(0x25e179);uint reference=NavigationCrowdAdapterContracts.Project030B30(adapter,agent.Position48,null,Need(production,c.Native.At).Adapter,c.Native);
    c.ReferenceQueryReturned=true;c.QueriedReference=reference;c.At(0x25e17c);
    c.At(0x25e185);byte retained=NavigationCrowdPathContracts.TrimReferences25BE90(agent.RequestE8,reference,c.Native);c.TrimResult=retained;
    c.At(0x25e18a);if(retained==0){c.At(0x25e18e);agent.RequireLive(c.Native.At);agent.NeedReplan140.Write(1);}
   }
   c.At(0x25e195);agent.RequireLive(c.Native.At);var minimum=agent.ParameterBits1C40[7].Snapshot();c.At(0x25e19a);float m=Raw(minimum,c.Native.At);
   if(m>0){c.At(0x25e1a4);float speed=Read(agent.Parameter18,c.Native.At);if(m<speed){c.At(0x25e1ae);if(agent.State70.Read(c.Native.At)==2)LowSpeed(agent,capturedSeconds,capturedTicks,m,production,c);}}
   // Captured integer ticks and last-element loop registers only move here;
   // they have no further data-dependent use in this reached final iteration.
   c.At(0x25e272);c.At(0x25e279);c.At(0x25e285);c.At(0x25e28b);
   var work=NavigationCrowdBorrowedPointerContracts.Work40(group,domain);
   c.At(0x25e2b1);work.CopyBeginToEnd(0x25e2b1,0x25e2eb);c.At(0x25e2eb);c.WorkCleared=true;
   c.At(0x25e2fb);c.Returned=true;
  }
 }
}
