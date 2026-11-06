using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Component preconditions for the reached25CF79 edge only. One Member/Work
// and retained velocity are explicit OWN observations, not a produced map
// prefix, an arrival, or a completed frame. No successful callback is supplied.
public static class NavigationEmptyWaypointContinuationTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}
 public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint expected,Action action,string why){try{action();}catch(MissingContract error){Eq(expected,error.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Unknown<T>(Field<T> value,string why){Missing(0,delegate{value.Read(0);},why);}
 static void Test(string name,Action action){try{action();scenarios++;}catch(Exception error){throw new Exception(name+": "+error.Message,error);}}
 sealed class Fixture {
  public readonly NavigationCrowdAgent Agent=new NavigationCrowdAgent();
  public readonly NavigationCrowdGroup Group=new NavigationCrowdGroup();
  public readonly NavigationCrowdPointerDomain<NavigationCrowdAgent> Domain=new NavigationCrowdPointerDomain<NavigationCrowdAgent>();
  public readonly Field<float> Seconds=new Field<float>();
  // These dependencies are intentionally absent: both actual early stops must
  // precede Proximity/waypoint steering, math, advancement and publication.
  public readonly NavigationCrowdTickProductionBoundaries Boundaries=new NavigationCrowdTickProductionBoundaries(null,null,null,null,null);
  public readonly Invocation Call=new Invocation();
  readonly NavigationCrowdPointerBuffer<NavigationCrowdAgent> members=new NavigationCrowdPointerBuffer<NavigationCrowdAgent>(1),work=new NavigationCrowdPointerBuffer<NavigationCrowdAgent>(1);
  readonly object workBegin,workEnd,workCapacity,memberBegin;
  public Fixture(){
   Agent.VelocityB8.X.Write(3);Agent.VelocityB8.Y.Write(-2);Agent.VelocityB8.Z.Write(5);
   members.At(0,0).Write(Agent);Group.Members28.InstallOwnStorage(members,1);
   work.At(0,0).Write(Agent);NavigationCrowdBorrowedPointerContracts.Work40(Group,Domain).InstallOwnStorage(work,1);
   workBegin=Group.Work40.Begin00.Read(0);workEnd=Group.Work40.End08.Read(0);workCapacity=Group.Work40.Capacity10.Read(0);memberBegin=Group.Members28.Begin00.Read(0);
  }
  public void Run(){NavigationCrowdTickProductionContracts.ProduceReachedSingleWork25CF79(Group,Agent,Domain,Seconds,Boundaries,Call);}
  public void Unchanged(){
   Eq(3f,Agent.VelocityB8.X.Read(0),"Retained velocity X");Eq(-2f,Agent.VelocityB8.Y.Read(0),"Retained velocity Y");Eq(5f,Agent.VelocityB8.Z.Read(0),"Retained velocity Z");
   Same(workBegin,Group.Work40.Begin00.Read(0),"Original Work begin");Same(workEnd,Group.Work40.End08.Read(0),"Work not cleared");Same(workCapacity,Group.Work40.Capacity10.Read(0),"Original Work capacity");Same(memberBegin,Group.Members28.Begin00.Read(0),"Original Member begin");Same(Agent,members.At(0,0).Read(0),"Same Member Agent");Same(Agent,work.At(0,0).Read(0),"Same Work Agent");
   Unknown(Seconds,"No advancement time consumed or initialized");Unknown(Agent.Position48.X,"No position published");Unknown(Agent.Heading54.X,"No heading produced");Unknown(Agent.RequestE8.WaypointVector38.Begin00,"No first waypoint fabricated");Unknown(Group.Vector80.Begin00,"No avoidance record list fabricated");
  }
 }
 public static void Run(){
  scenarios=assertions=0;
  Test("Unknown flags stop at real avoidance without neighbor population or steering",delegate{
   var f=new Fixture();Missing(0x25cfca,f.Run,"First actual empty-waypoint continuation read");Eq(0x25cfcau,f.Call.At,"Invocation retains flag consumer");f.Unchanged();Unknown(f.Agent.Flags44,"Flags remain Unknown");Unknown(f.Agent.Timer6C,"No speed read/write before flags");
   for(int slot=0;slot<3;slot++)Unknown(f.Agent.Words88B0[slot],"Original Unknown neighbor header slot"+slot);Eq(0,f.Domain.OwnActiveAllocations,"No neighbor allocation inferred");
  });
  Test("Existing nonempty neighbors stop at real avoidance record boundary and remain intact",delegate{
   var f=new Fixture();f.Agent.Flags44.Write(4);f.Agent.Timer6C.Write(NavigationCoreContracts.ToBits(1f));
   var other=new NavigationCrowdAgent();var storage=new NavigationCrowdPointerBuffer<NavigationCrowdAgent>(2);storage.At(0,0).Write(other);
   var neighbors=NavigationCrowdBorrowedPointerContracts.Neighbors88(f.Agent,f.Domain);neighbors.InstallOwnStorage(storage,1);var words=new ulong[3];for(int slot=0;slot<3;slot++)words[slot]=f.Agent.Words88B0[slot].Read(0);
   Missing(0x25d020,f.Run,"Nonnull neighbor path remains unresolved");Eq(0x25d020u,f.Call.At,"Invocation retains actual avoidance boundary");f.Unchanged();Eq(4u,f.Agent.Flags44.Read(0),"Original flags retained");Eq(NavigationCoreContracts.ToBits(1f),f.Agent.Timer6C.Read(0),"Original speed bits retained");
   for(int slot=0;slot<3;slot++)Eq(words[slot],f.Agent.Words88B0[slot].Read(0),"Original neighbor token slot"+slot);
   var begin=neighbors.Read(NavigationCrowdPointerSlot.Begin,0);var end=neighbors.Read(NavigationCrowdPointerSlot.End,0);var capacity=neighbors.Read(NavigationCrowdPointerSlot.Capacity,0);
   Same(storage,begin.Buffer,"Original neighbor backing");Same(storage,end.Buffer,"Original end backing");Same(storage,capacity.Buffer,"Original capacity backing");Eq(0,begin.Index,"Original neighbor begin index");Eq(1,end.Index,"Nonempty neighbor list retained");Eq(2,capacity.Index,"Original capacity retained");Same(other,begin.Buffer.At(begin.Index,0).Read(0),"Original neighbor identity");Unknown(storage.At(1,0),"Unused capacity not populated");Unknown(other.Flags44,"Neighbor contents not consumed/defaulted");Eq(1,f.Domain.OwnActiveAllocations,"No replacement neighbor allocation");
  });
 }
}
