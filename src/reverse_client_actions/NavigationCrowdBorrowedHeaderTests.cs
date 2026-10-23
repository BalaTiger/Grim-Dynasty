using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using GrimDynasty.ReverseClient.Actions;
public static class NavigationCrowdBorrowedHeaderTests {
 static int scenarios,assertions;
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Equal<T>(T a,T b,string why){Check(EqualityComparer<T>.Default.Equals(a,b),why+" expected="+a+" actual="+b);}
 static void Same(object a,object b,string why){Check(Object.ReferenceEquals(a,b),why);}
 static void Test(string name,Action body){body();scenarios++;Console.WriteLine("PASS "+name);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract e){Equal(at,e.Address,why);return;}throw new Exception("Expected explicit stop "+why);}
 static void Unknown<T>(Field<T> cell,string why){Missing(0,delegate{cell.Read(0);},why);}
 static NavigationCrowdAgent Agent(float x){var a=new NavigationCrowdAgent();a.State70.Write(2);a.Flags44.Write(0);a.Position48.X.Write(x);a.Position48.Y.Write(0);a.Position48.Z.Write(0);a.Radius10.Write(0.25f);a.Height14.Write(2);a.ParameterBits1C40[1].Write(1);a.ParameterBits1C40[2].Write(1);a.Words88B0[0].Write(0);a.Words88B0[1].Write(0);a.Words88B0[2].Write(0);return a;}
 static Field<float> Factor(){return Field<float>.Of(BitConverter.ToSingle(BitConverter.GetBytes(0x41000000u),0));} // saved34BC64 disk bits; explicit OWN choice, not loaded observation
 sealed class Fixture {
  public readonly NavigationCrowdGroup Group=new NavigationCrowdGroup();
  public readonly NavigationCrowdPointerDomain<NavigationCrowdAgent> Domain=new NavigationCrowdPointerDomain<NavigationCrowdAgent>();
  public readonly NavigationCrowdProximityIndex Index;
  public readonly NavigationCrowdProximityBoundaries B;
  public Fixture(){Group.Work40.Begin00.Write(null);Group.Work40.End08.Write(null);Group.Work40.Capacity10.Write(null);Group.Pointer58.Write(null);Group.Pointer60.Write(null);Group.Count68.Write(0);Index=new NavigationCrowdProximityIndex(Group);var s=new NavigationCrowdStateBoundaries(null);s.UseObservedGroupConstants();B=new NavigationCrowdProximityBoundaries(s);NavigationCrowdProximityContracts.Prepare25E900(Index,1,new Invocation());}
  public NavigationCrowdPointerHeader<NavigationCrowdAgent> Work{get{return NavigationCrowdBorrowedPointerContracts.Work40(Group,Domain);}}
  public NavigationCrowdPointerHeader<NavigationCrowdAgent> Neighbors(NavigationCrowdAgent a){return NavigationCrowdBorrowedPointerContracts.Neighbors88(a,Domain);}
  public void Publish(NavigationCrowdAgent a){NavigationCrowdTickHeaderContracts.PublishReachedWorkAndProximity25C832(Group,a,Domain,B,new Invocation());}
  public void Insert(NavigationCrowdAgent a){NavigationCrowdProximityContracts.Insert25EA00(Index,a,B,new Invocation());}
  public void Query(NavigationCrowdAgent a,Field<float> max,Field<float> factor){NavigationCrowdTickHeaderContracts.PopulateReachedNeighbors25CB73(Group,a,Domain,max,factor,B,new Invocation());}
 }
 static NavigationCrowdPointerAddress<NavigationCrowdAgent> Begin(NavigationCrowdPointerHeader<NavigationCrowdAgent> h){return h.Read(NavigationCrowdPointerSlot.Begin,0);}
 static int Count(NavigationCrowdPointerHeader<NavigationCrowdAgent> h){var b=Begin(h);var e=h.Read(NavigationCrowdPointerSlot.End,0);return b==null?0:e.Index-b.Index;}
 static NavigationCrowdAgent At(NavigationCrowdPointerHeader<NavigationCrowdAgent> h,int n){var b=Begin(h);return b.Buffer.At(b.Index+n,0).Read(0);}
 public static void Run(){
  Test("Work and raw neighbor views borrow authoritative cells and shared element identity",delegate{
   var f=new Fixture();var a=Agent(0);var storage=new NavigationCrowdPointerBuffer<NavigationCrowdAgent>(3);storage.At(0,0).Write(a);f.Work.InstallOwnStorage(storage,1);var n=f.Neighbors(a);n.InstallOwnStorage(storage,1);
   Same(f.Group.Work40.Begin00.Read(0),f.Work.Read(NavigationCrowdPointerSlot.Begin,0),"Object cell address identity");Same(storage,Begin(n).Buffer,"Raw pointer resolves same backing");Same(a,At(n,0),"Same agent pointee");Equal(f.Domain.EncodeOwn(new NavigationCrowdPointerAddress<NavigationCrowdAgent>(storage,1),0),a.Words88B0[1].Read(0),"Raw header is authoritative");
   a.Words88B0[1].Write(f.Domain.EncodeOwn(new NavigationCrowdPointerAddress<NavigationCrowdAgent>(storage,2),0));Equal(2,Count(n),"External original-cell write immediately visible");var replacement=Agent(0.5f);storage.At(0,0).Write(replacement);Same(replacement,At(f.Work,0),"No pointee copy or deferred sync");Unknown(storage.At(2,0),"Spare cell unknown");
  });
  Test("Opaque begin to end copy preserves unknown and does not demand capacity",delegate{
   var f=new Fixture();var a=Agent(0);a.Words88B0[0].Forget();a.Words88B0[2].Forget();var n=f.Neighbors(a);n.CopyBeginToEnd(0x25cb7f,0x25cb8e);Unknown(a.Words88B0[1],"Unknown raw MOV");Unknown(a.Words88B0[2],"Capacity untouched");f.Group.Work40.Begin00.Forget();f.Work.CopyBeginToEnd(10,11);Unknown(f.Group.Work40.End08,"Unknown object MOV");
  });
  Test("Raw growth uses original header and preserves duplicate null and unknown elements",delegate{
   var f=new Fixture();var a=Agent(0);var n=f.Neighbors(a);var old=new NavigationCrowdPointerBuffer<NavigationCrowdAgent>(4);old.At(0,0).Write(a);old.At(1,0).Write(a);old.At(2,0).Write(null);n.InstallOwnStorage(old,4);ulong stale=a.Words88B0[0].Read(0);
   NavigationCrowdBorrowedPointerContracts.ReserveAdditional034EC0(n,1,new Invocation());Equal(6,n.Read(NavigationCrowdPointerSlot.Capacity,0).Index,"Actual 1.5 growth");Equal(4,Count(n),"Count retained");Same(a,At(n,0),"Identity0");Same(a,At(n,1),"Duplicate");Same(null,At(n,2),"Known null");Unknown(Begin(n).Buffer.At(3,0),"Unknown copied");Unknown(Begin(n).Buffer.At(5,0),"No tail clearing");Same(Begin(n).Buffer,n.LastGrowthAllocation,"Actual diagnostic allocation");Missing(0,delegate{old.At(0,0);},"Old backing released");Missing(123,delegate{f.Domain.ResolveOwn(stale,123);},"Stale raw binding removed");Equal(1,f.Domain.OwnActiveAllocations,"Only current raw allocation bound");
  });
  Test("Wrong-domain unaligned and unregistered tokens never become empty pointers",delegate{
   var f=new Fixture();var a=Agent(0);var n=f.Neighbors(a);n.InstallOwnStorage(new NavigationCrowdPointerBuffer<NavigationCrowdAgent>(2),1);ulong token=a.Words88B0[0].Read(0);var other=new NavigationCrowdPointerDomain<NavigationCrowdAgent>();other.EncodeOwn(new NavigationCrowdPointerAddress<NavigationCrowdAgent>(new NavigationCrowdPointerBuffer<NavigationCrowdAgent>(2),0),0);
   Missing(30,delegate{other.ResolveOwn(token,30);},"No cross-domain collision");Missing(31,delegate{f.Domain.ResolveOwn(token+1,31);},"Alignment");a.Words88B0[0].Write(0x180422980ul);Missing(32,delegate{n.Read(NavigationCrowdPointerSlot.Begin,32);},"Original-looking address not guessed");Equal(1,f.Domain.OwnActiveAllocations,"Bad reads cannot create mappings");
  });
  Test("Repeated real growth releases active bindings and never reuses a stale token",delegate{
   var f=new Fixture();var a=Agent(0);var n=f.Neighbors(a);NavigationCrowdBorrowedPointerContracts.Reallocate034FE0(n,1,new Invocation());ulong first=a.Words88B0[0].Read(0);var buffers=new List<NavigationCrowdPointerBuffer<NavigationCrowdAgent>>();
   for(int x=0;x<12;x++){buffers.Add(Begin(n).Buffer);NavigationCrowdBorrowedPointerContracts.Reallocate034FE0(n,2,new Invocation());Equal(1,f.Domain.OwnActiveAllocations,"Released mappings reclaim active entries");Check(a.Words88B0[0].Read(0)!=first,"Never reuses earliest token");}
   foreach(var b in buffers)Missing(0,delegate{b.At(0,0);},"Each prior allocation expired");Missing(44,delegate{f.Domain.ResolveOwn(first,44);},"Old raw token remains invalid");
  });
  Test("Late unknown capacity retains actual copy and the previous live raw header",delegate{
   var f=new Fixture();var a=Agent(0);var n=f.Neighbors(a);var old=new NavigationCrowdPointerBuffer<NavigationCrowdAgent>(2);old.At(0,0).Write(a);n.InstallOwnStorage(old,1);ulong begin=a.Words88B0[0].Read(0),end=a.Words88B0[1].Read(0);a.Words88B0[2].Forget();
   Missing(0x3503a,delegate{NavigationCrowdBorrowedPointerContracts.Reallocate034FE0(n,3,new Invocation());},"First capacity consumption after copy");Same(a,n.LastGrowthAllocation.At(0,0).Read(0),"Copy retained");Unknown(n.LastGrowthAllocation.At(1,0),"Spare unknown");Equal(begin,a.Words88B0[0].Read(0),"Begin retained");Equal(end,a.Words88B0[1].Read(0),"End retained");old.RequireLive(0);Equal(1,f.Domain.OwnActiveAllocations,"Unpublished fresh local not bound");
  });
  Test("No-growth raw reserve skips unknown begin and expired owner fails at header access",delegate{
   var f=new Fixture();var a=Agent(0);var n=f.Neighbors(a);n.InstallOwnStorage(new NavigationCrowdPointerBuffer<NavigationCrowdAgent>(3),1);a.Words88B0[0].Forget();var i=new Invocation();NavigationCrowdBorrowedPointerContracts.ReserveAdditional034EC0(n,2,i);Equal(0x34f41u,i.At,"Early return");Unknown(a.Words88B0[0],"Begin remains unknown");Same(null,n.LastGrowthAllocation,"No allocation");a.MarkReleased();Missing(0x34ec4,delegate{NavigationCrowdBorrowedPointerContracts.ReserveAdditional034EC0(n,1,new Invocation());},"Owner checked on first header read");
  });
  Test("Reached work branch grows real Work40 then populates the actual proximity index",delegate{
   var f=new Fixture();var a=Agent(0.1f);var b=Agent(0.2f);var c=Agent(0.3f);f.Publish(a);var first=Begin(f.Work).Buffer;f.Publish(b);f.Publish(c);Equal(3,Count(f.Work),"Three actual appends");Same(a,At(f.Work,0),"Order a");Same(b,At(f.Work,1),"Order b");Same(c,At(f.Work,2),"Order c");Equal(3u,f.Group.Count78.Read(0),"Three real index inserts");Missing(0,delegate{first.At(0,0);},"Growth invalidates first allocation");Unknown(a.Words88B0[3],"Unwritten other raw member remains unknown");Equal(0.1f,a.Position48.X.Read(0),"Does not claim movement");
  });
  Test("State other than2 inserts proximity without touching Work header",delegate{
   var f=new Fixture();var a=Agent(0);a.State70.Write(6);f.Group.Work40.Begin00.Forget();f.Group.Work40.End08.Forget();f.Group.Work40.Capacity10.Forget();f.Publish(a);Equal(1u,f.Group.Count78.Read(0),"Nonmoving member still indexed");Unknown(f.Group.Work40.End08,"No Work append");
  });
  Test("Actual1000 flag skips work and index without consuming their fields",delegate{
   var a=Agent(0);a.Flags44.Write(0x1000);var i=new Invocation();NavigationCrowdTickHeaderContracts.PublishReachedWorkAndProximity25C832(null,a,null,null,i);Equal(0x25c8d7u,i.At,"Native skip branch");a.State70.Write(6);NavigationCrowdTickHeaderContracts.PublishReachedWorkAndProximity25C832(null,a,null,null,i);Equal(0x25c8d7u,i.At,"Other-state skip branch");
  });
  Test("Unknown work state and flags stop at actual tests without publishing",delegate{
   var f=new Fixture();var a=Agent(0);a.State70.Forget();Missing(0x25c832,delegate{f.Publish(a);},"State compare");Same(null,f.Group.Work40.Begin00.Read(0),"No append");a.State70.Write(2);a.Flags44.Forget();Missing(0x25c83c,delegate{f.Publish(a);},"Moving flag");a.State70.Write(6);Missing(0x25c8c2,delegate{f.Publish(a);},"Other state flag");Equal(0u,f.Group.Count78.Read(0),"No insertion");
  });
  Test("Work append is retained when real index insertion later consumes unknown position",delegate{
   var f=new Fixture();var a=Agent(0);a.Position48.X.Forget();object priorFree=f.Group.Pointer70.Read(0);Missing(0x25ea1b,delegate{f.Publish(a);},"Position in actual Insert");Equal(1,Count(f.Work),"Earlier append retained");Same(a,At(f.Work,0),"Published same failing member");Check(!Object.ReferenceEquals(priorFree,f.Group.Pointer70.Read(0)),"Insert already removed free head");Equal(0u,f.Group.Count78.Read(0),"Failed insert does not claim count");
  });
  Test("Reached neighbor path writes original raw cells with real query ordering and duplicate identity",delegate{
   var f=new Fixture();var self=Agent(0);var a=Agent(0.2f);var b=Agent(0.3f);f.Publish(self);f.Publish(a);f.Publish(b);f.Insert(a);var n=f.Neighbors(self);var old=new NavigationCrowdPointerBuffer<NavigationCrowdAgent>(1);old.At(0,0).Write(self);n.InstallOwnStorage(old,1);f.Query(self,Field<float>.Of(0.25f),Factor());Equal(3,Count(n),"Three actual neighbors excluding self");Same(a,At(n,0),"Most recent duplicate at head");Same(b,At(n,1),"Other neighbor");Same(a,At(n,2),"First duplicate retained");Equal(3,n.Read(NavigationCrowdPointerSlot.Capacity,0).Index,"Real growth1 to2 to3");Missing(0,delegate{old.At(0,0);},"Old array released");Equal(1,f.Domain.OwnActiveAllocations,"Only latest raw allocation active");Unknown(self.Timer6C,"No invented speed");
  });
  Test("Neighbor reset precedes unknown radius or captured register arithmetic failure",delegate{
   var f=new Fixture();var a=Agent(0);var n=f.Neighbors(a);n.InstallOwnStorage(new NavigationCrowdPointerBuffer<NavigationCrowdAgent>(2),1);a.Radius10.Forget();Missing(0x25cb92,delegate{f.Query(a,Field<float>.Of(1),Factor());},"Radius consumption");Equal(a.Words88B0[0].Read(0),a.Words88B0[1].Read(0),"Reset completed");a.Radius10.Write(0.25f);Missing(0x25cb92,delegate{f.Query(a,new Field<float>(),Factor());},"Max radius register unknown");Missing(0x25cb9e,delegate{f.Query(a,Field<float>.Of(1),new Field<float>());},"Factor register unknown");Equal(0,Count(n),"No fabricated neighbors");
  });
  Test("Unknown captured height stops inside true callee after raw reset",delegate{
   var f=new Fixture();var a=Agent(0);var n=f.Neighbors(a);n.InstallOwnStorage(new NavigationCrowdPointerBuffer<NavigationCrowdAgent>(2),1);a.Height14.Forget();Missing(0x25ebb9,delegate{f.Query(a,Field<float>.Of(0.25f),Factor());},"Height first arithmetic");Equal(0,Count(n),"Caller reset retained");
  });
  Test("Unknown mask stays unconsumed on empty query then stops at matching node test",delegate{
   var f=new Fixture();var a=Agent(0);a.ParameterBits1C40[2].Forget();a.Words88B0[0].Forget();a.Words88B0[2].Forget();f.Query(a,Field<float>.Of(0.25f),Factor());Unknown(a.Words88B0[1],"Empty query preserved copied unknown");Unknown(a.ParameterBits1C40[2],"Mask not defaulted");f.Insert(Agent(0.1f));Missing(0x25ecf5,delegate{f.Query(a,Field<float>.Of(0.25f),Factor());},"First real mask use");Unknown(a.Words88B0[1],"No append after failed mask");
  });
  Test("Unknown raw End copied from Begin fails only at first actual output append",delegate{
   var f=new Fixture();var a=Agent(0);a.Words88B0[0].Forget();f.Insert(Agent(0.1f));Missing(0x25ed44,delegate{f.Query(a,Field<float>.Of(0.25f),Factor());},"Opaque output consume");Unknown(a.Words88B0[1],"No invented pointer");Equal(0ul,a.Words88B0[2].Read(0),"Capacity preserved");
  });
  Test("Legacy null output remains legal on no hit and fails only on real append",delegate{
   var f=new Fixture();var a=Agent(0);NavigationCrowdProximityContracts.Neighbors25EB90(f.Index,a.Position48,1,2,1,a,null,f.B,new Invocation());f.Insert(Agent(0.1f));Missing(0x25ed3b,delegate{NavigationCrowdProximityContracts.Neighbors25EB90(f.Index,a.Position48,1,2,1,a,null,f.B,new Invocation());},"Lazy null output");
  });
 }
 public static int Main(){try{Run();Console.WriteLine(new JavaScriptSerializer().Serialize(new{status="passed",scenarios=scenarios,assertions=assertions,shared_authoritative_headers=true,own_address_domain=true,reached_caller_fragments_only=true,original_read=false,original_executed=false,bss_defaults_inferred=false,tick_executed=false,character_movement_validated=false,gameplay_accepted=false}));return 0;}catch(Exception e){Console.Error.WriteLine("FAIL after "+scenarios+" scenarios, "+assertions+" assertions: "+e);return 1;}}
}
