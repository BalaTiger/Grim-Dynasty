using System;
using System.Runtime.CompilerServices;
namespace GrimDynasty.ReverseClient.Actions {
 // Exact caller fragments of25C440, not a complete tick. Entry25C832 assumes
 // control has ACTUALLY reached that block after earlier query/replan/callback
 // work. Entry25CB73 requires the nonempty waypoint branch. Neither method
 // asserts those predecessors occurred or invokes a replacement success hook.
 // Shared header storage and real25EA00/25EB90 producers are used throughout.
 public static class NavigationCrowdTickHeaderContracts {
  static T Need<T>(T v,uint at)where T:class{if(v==null)throw new MissingContract(at,"Null OWN crowd tick view");var n=v as NavigationView;if(n!=null)n.RequireLive(at);return v;}
  static bool Same(NavigationCrowdPointerAddress<NavigationCrowdAgent> a,NavigationCrowdPointerAddress<NavigationCrowdAgent> b){return a==null?b==null:b!=null&&Object.ReferenceEquals(a.Buffer,b.Buffer)&&a.Index==b.Index;}
  static float Scalar(float value,uint at){uint bits=BitConverter.ToUInt32(BitConverter.GetBytes(value),0);if((bits&0x7f800000u)==0x7f800000u||((bits&0x7f800000u)==0&&(bits&0x7fffffu)!=0))throw new MissingContract(at,"OWN finite normal/zero FP domain; native MXCSR and nonfinite cases unverified");return value;}
  [MethodImpl(MethodImplOptions.NoInlining)]
  static float FP(double value,uint at){if(value!=0&&Math.Abs(value)<1.17549435082228750796873653722224568E-38)throw new MissingContract(at,"OWN FP domain excludes subnormal intermediates");return Scalar(BitConverter.ToSingle(BitConverter.GetBytes((float)value),0),at);}
  public static void PublishReachedWorkAndProximity25C832(NavigationCrowdGroup group,NavigationCrowdAgent agent,NavigationCrowdPointerDomain<NavigationCrowdAgent> domain,NavigationCrowdProximityBoundaries boundaries,Invocation invocation){
   var i=Need(invocation,0x25c832);i.At=0x25c832;
   if(Need(agent,i.At).State70.Read(i.At)==2){
    i.At=0x25c83c;if((agent.Flags44.Read(i.At)&0x1000)!=0){i.At=0x25c8d7;return;}
    var work=NavigationCrowdBorrowedPointerContracts.Work40(group,domain);var endBits=work.Capture(NavigationCrowdPointerSlot.End,0x25c849);i.At=0x25c854;var end=endBits.Read(i.At);
    // RBP+508 is this caller's disjoint stack local; only its non-alias arm
    // is supported in the OWN address model, never an arbitrary byte alias.
    i.At=0x25c89e;var cap=work.Read(NavigationCrowdPointerSlot.Capacity,i.At);if(Same(end,cap)){i.At=0x25c8ac;NavigationCrowdBorrowedPointerContracts.ReserveAdditional034EC0(work,1,i);}
    i.At=0x25c8b1;end=work.Read(NavigationCrowdPointerSlot.End,i.At);if(end!=null){i.At=0x25c8ba;end.Buffer.At(end.Index,i.At).Write(agent);}
    i.At=0x25c8bd;end=work.Read(NavigationCrowdPointerSlot.End,i.At);if(end==null||end.Index>=end.Buffer.Cells.Length)throw new MissingContract(i.At,"Increment outside OWN stride8 storage");end.Buffer.RequireLive(i.At);work.Write(NavigationCrowdPointerSlot.End,new NavigationCrowdPointerAddress<NavigationCrowdAgent>(end.Buffer,end.Index+1),i.At);
   }
   i.At=0x25c8c2;if((agent.Flags44.Read(i.At)&0x1000)==0){i.At=0x25c8d2;NavigationCrowdProximityContracts.Insert25EA00(new NavigationCrowdProximityIndex(Need(group,i.At)),agent,boundaries,i);}
   i.At=0x25c8d7;
  }
  // capturedMaximumRadius is XMM6 from the actual earlier agent loop, and
  // capturedNeighborFactor is XMM12 loaded from34BC64 before this block. They
  // retain Unknown; this fragment does not invent a global or recompute them.
  public static void PopulateReachedNeighbors25CB73(NavigationCrowdGroup group,NavigationCrowdAgent agent,NavigationCrowdPointerDomain<NavigationCrowdAgent> domain,Field<float> capturedMaximumRadius,Field<float> capturedNeighborFactor,NavigationCrowdProximityBoundaries boundaries,Invocation invocation){
   var i=Need(invocation,0x25cb73);i.At=0x25cb73;var radiusBits=Need(agent,i.At).Radius10.Snapshot();var neighbors=NavigationCrowdBorrowedPointerContracts.Neighbors88(agent,domain);
   neighbors.CopyBeginToEnd(0x25cb7f,0x25cb8e);i.At=0x25cb92;float radius=Scalar(radiusBits.Read(i.At),i.At);float sum=FP((double)radius+Scalar(Need(capturedMaximumRadius,i.At).Read(i.At),i.At),i.At);
   var mask=agent.ParameterBits1C40[2].Snapshot();var height=agent.Height14.Snapshot();i.At=0x25cb9e;float scaled=FP((double)radius*Scalar(Need(capturedNeighborFactor,i.At).Read(i.At),i.At),i.At);
   // MAXSS returns its second operand on equality (including signed zero).
   i.At=0x25cbb1;float searchRadius=scaled>sum?scaled:sum;i.At=0x25cbb8;
   NavigationCrowdProximityContracts.NeighborsCapturedBorrowed25EB90(new NavigationCrowdProximityIndex(Need(group,i.At)),agent.Position48,Field<float>.Of(searchRadius),height,mask,agent,neighbors,boundaries,i);
   i.At=0x25cbbd; // next direct target25BF40 is deliberately NOT executed here.
  }
 }
}
