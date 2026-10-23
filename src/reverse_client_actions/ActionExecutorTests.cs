using System;
using System.Collections.Generic;
namespace GrimDynasty.ReverseClient.Actions {
 public static class ActionExecutorTests {
  public static int Assertions,Scenarios;
  static void Check(bool value,string reason){Assertions++;if(!value)throw new Exception(reason);}
  static bool Same(object a,object b){return Object.ReferenceEquals(a,b);}
  sealed class Ports:Boundaries {
   public Action<ActionRecord> Start,StopCall,Destroy;
   public Action<Node> Release;
   public Func<Handler,Node,Node,Field<ActionRecord>,Node> Allocate;
   public int Starts,Stops,Destroys,Allocations,Releases;
   public override void Start08(ActionRecord a){Starts++;if(Start!=null)Start(a);}
   public override void Stop18(ActionRecord a){Stops++;if(StopCall!=null)StopCall(a);}
   public override void Destroy0(ActionRecord a,uint f){Destroys++;ActionExecutorTests.Check(f==1,"destructor flag");if(Destroy!=null)Destroy(a);}
   public override Node AllocateLinks(Handler h,Node n,Node p,Field<ActionRecord> slot){Allocations++;if(Allocate==null)return base.AllocateLinks(h,n,p,slot);return Allocate(h,n,p,slot);}
   public override void ReleaseNode(Node n){Releases++;if(Release!=null)Release(n);base.ReleaseNode(n);}
  }
  static Handler Empty(){var h=new Handler();var n=new Node();n.Next00.Write(n);n.Previous08.Write(n);h.Head20.Write(n);h.Count28.Write(0);h.Current08.Write(null);h.Busy18.Write(0);return h;}
  // Explicit fixture adapter for the unknown079CD0 call, not native evidence.
  static Node FixtureAllocate(Handler h,Node n,Node p,Field<ActionRecord> slot){var x=new Node();x.Next00.Write(n);x.Previous08.Write(p);return x;}
  static Node AppendFixture(Handler h,ActionRecord a){Node n=h.Head20.Read(0),p=n.Previous08.Read(0),x=FixtureAllocate(h,n,p,null);x.Value10.Write(a);p.Next00.Write(x);n.Previous08.Write(x);h.Count28.Write(h.Count28.Read(0)+1);return x;}
  static void Scenario(Action test){test();Scenarios++;}
  sealed class IntegrationPorts:Boundaries {
   public Role Role;public int GetAICount,StartCount;
   public override Role GetAI(Controller c){GetAICount++;return Role;}
   public override int Kind58(ActionRecord a){return 4;}
   public override void Start08(ActionRecord a){StartCount++;}
  }
  public static void Run(){
   Scenario(delegate{var b=new Boundaries();Check(Executor.Run(null,null,null,b).ReturnedNormally,"null skips all fields");});
   Scenario(delegate{var h=Empty();var a=new ActionRecord();var b=new Ports();var c=new Counter(100);var r=Executor.Run(h,a,c,b);Check(r.ReturnedNormally,"idle normal");Check(c.Value.Read(0)==99&&b.Starts==1,"budget/start");Check(Same(h.Current08.Read(0),a)&&h.Busy18.Read(0)==0,"publish and unbusy");});
   Scenario(delegate{var h=Empty();var a=new ActionRecord();var b=new Ports();var c=new Counter(4);b.Start=delegate{Check(h.Busy18.Read(0)==1&&Same(h.Current08.Read(0),a),"start observes live publication");throw new Exception("injected");};var r=Executor.Run(h,a,c,b);Check(!r.ReturnedNormally&&r.Address==0x089b17,"start failure location");Check(h.Busy18.Read(0)==1&&c.Value.Read(0)==3,"no finally rollback");});
   Scenario(delegate{var h=Empty();var old=new ActionRecord();var replacement=new ActionRecord();var next=new ActionRecord();h.Current08.Write(old);var b=new Ports();b.StopCall=delegate(ActionRecord a){Check(Same(a,old),"stop original");h.Current08.Write(replacement);};b.Destroy=delegate(ActionRecord a){Check(Same(a,replacement),"destroy late pointer");h.Current08.Write(old);};var r=Executor.Run(h,next,new Counter(2),b);Check(r.ReturnedNormally&&Same(h.Current08.Read(0),next),"stop clear then new publication");Check(b.Stops==1&&b.Destroys==1,"single stop/destroy");});
   Scenario(delegate{var h=Empty();var old=new ActionRecord();h.Current08.Write(old);var b=new Ports();b.StopCall=delegate{h.Current08.Write(null);};Check(Executor.Run(h,new ActionRecord(),new Counter(2),b).ReturnedNormally,"stop clears");Check(b.Destroys==0,"null reread skips destroy");});
   Scenario(delegate{var h=Empty();var old=new ActionRecord();h.Current08.Write(old);var b=new Ports();b.StopCall=delegate{throw new Exception("stop");};var c=new Counter(2);var r=Executor.Run(h,new ActionRecord(),c,b);Check(!r.ReturnedNormally&&r.Address==0x089c05,"stop failure");Check(Same(h.Current08.Read(0),old)&&h.Busy18.Read(0)==1&&c.Value.Read(0)==1,"stop failure prefix");});
   Scenario(delegate{var h=Empty();h.Busy18.Write(1);var c=new Counter(3);var r=Executor.Run(h,new ActionRecord(),c,new Boundaries());Check(!r.ReturnedNormally&&r.Address==0x079cd0,"unknown allocator explicit");Check(c.Value.Read(0)==2&&h.Count28.Read(0)==0,"allocator failure consumes budget only");});
   Scenario(delegate{var h=Empty();var first=new ActionRecord();var second=new ActionRecord();var c=new Counter(10);var b=new Ports();b.Allocate=FixtureAllocate;b.Start=delegate(ActionRecord a){if(Same(a,first)){Check(Executor.Run(h,second,c,b).ReturnedNormally,"reentrant queued");Check(c.Value.Read(0)==8&&h.Count28.Read(0)==1,"reentry budget");}};var r=Executor.Run(h,first,c,b);Check(r.ReturnedNormally,"queued action drained");Check(c.Value.Read(0)==7&&b.Starts==2&&b.Stops==1,"both iterations and nested call");Check(h.Count28.Read(0)==0&&b.Releases==1&&Same(h.Current08.Read(0),second),"FIFO and node lifetime");});
   Scenario(delegate{var h=Empty();var a=new ActionRecord();var c=new Counter(10);var b=new Ports();b.Allocate=FixtureAllocate;b.Start=delegate{if(b.Starts==1)Check(Executor.Run(h,a,c,b).ReturnedNormally,"duplicate action queued");};Check(Executor.Run(h,a,c,b).ReturnedNormally,"same pointer supported observation");Check(b.Starts==2&&b.Stops==1&&b.Destroys==1,"no identity deduplication");});
   Scenario(delegate{var h=Empty();Node empty=AppendFixture(h,null),last=AppendFixture(h,new ActionRecord());var b=new Ports();var c=new Counter(8);Check(Executor.Run(h,new ActionRecord(),c,b).ReturnedNormally,"null payload return");Check(empty.Freed&&!last.Freed&&h.Count28.Read(0)==1&&c.Value.Read(0)==7,"remaining queue preserved");});
   Scenario(delegate{var h=Empty();var current=new ActionRecord();h.Current08.Write(current);var a=new ActionRecord();Node x=AppendFixture(h,a),y=AppendFixture(h,a);var b=new Ports();var c=new Counter(0);Check(Executor.Run(h,a,c,b).ReturnedNormally,"budget clear");Check(x.Freed&&y.Freed&&b.Destroys==0&&b.Starts==0,"nodes only freed");Check(Same(h.Current08.Read(0),current)&&c.Value.Read(0)==0&&h.Count28.Read(0)==0,"current and budget unchanged");Node head=h.Head20.Read(0);Check(Same(head.Next00.Read(0),head)&&Same(head.Previous08.Read(0),head),"sentinel restored");});
   Scenario(delegate{var h=Empty();Node popped=AppendFixture(h,new ActionRecord()),rest=AppendFixture(h,new ActionRecord());var b=new Ports();var c=new Counter(1);Check(Executor.Run(h,new ActionRecord(),c,b).ReturnedNormally,"exhaust after pop");Check(popped.Freed&&rest.Freed&&b.Starts==1&&c.Value.Read(0)==0,"popped payload not started/destroyed");Check(b.Destroys==0,"clear no action destructor");});
   Scenario(delegate{var h=Empty();Node n=AppendFixture(h,new ActionRecord());var b=new Ports();b.Release=delegate{throw new Exception("free");};var r=Executor.Run(h,new ActionRecord(),new Counter(2),b);Check(!r.ReturnedNormally&&r.Address==0x089b54,"free callback failure");Check(h.Count28.Read(0)==0&&!n.Freed&&h.Busy18.Read(0)==0,"unlink prefix remains");});
   Scenario(delegate{var h=Empty();Node n=AppendFixture(h,new ActionRecord());var b=new Ports();b.Release=delegate{throw new Exception("clear free");};var r=Executor.Run(h,new ActionRecord(),new Counter(-1),b);Check(!r.ReturnedNormally&&r.Address==0x073bd3,"clear free failure");Node head=h.Head20.Read(0);Check(h.Count28.Read(0)==0&&Same(head.Next00.Read(0),head)&&!n.Freed,"clear prefix before first free");});
   Scenario(delegate{var h=Empty();h.Busy18.Write(1);h.Count28.Write(0x0aaaaaaaaaaaaaa9UL);var b=new Ports();Node allocated=null;b.Allocate=delegate(Handler z,Node n,Node p,Field<ActionRecord> slot){allocated=FixtureAllocate(z,n,p,slot);return allocated;};var a=new ActionRecord();var r=Executor.Run(h,a,new Counter(1),b);Check(!r.ReturnedNormally&&r.Address==0x089ba9,"exact capacity boundary");Check(b.Allocations==1&&Same(allocated.Value10.Read(0),a),"allocate/payload before capacity check");Check(h.Count28.Read(0)==0x0aaaaaaaaaaaaaa9UL&&!allocated.Freed,"no invented rollback/free");});
   Scenario(delegate{var h=Empty();h.Busy18.Write(1);h.Count28.Write(0x0aaaaaaaaaaaaaaaUL);var b=new Ports();b.Allocate=FixtureAllocate;Check(Executor.Run(h,new ActionRecord(),new Counter(1),b).ReturnedNormally,"unsigned count above constant not rejected");Check(h.Count28.Read(0)==0x0aaaaaaaaaaaaaabUL,"native unsigned subtraction condition");});
   Scenario(delegate{var h=Empty();h.Busy18.Write(1);Node original=h.Head20.Read(0),replacement=new Node();replacement.Next00.Write(replacement);replacement.Previous08.Write(replacement);var b=new Ports();Node allocated=null;b.Allocate=delegate(Handler z,Node n,Node p,Field<ActionRecord> slot){Check(Same(n,original)&&Same(p,original),"captured sentinel/tail");z.Head20.Write(replacement);z.Count28.Write(4);slot.Write(null);allocated=FixtureAllocate(z,n,p,slot);return allocated;};Check(Executor.Run(h,new ActionRecord(),new Counter(1),b).ReturnedNormally,"allocator mutation");Check(Same(h.Head20.Read(0),replacement)&&Same(original.Previous08.Read(0),allocated)&&h.Count28.Read(0)==5,"captured head plus late count");Check(allocated.Value10.Read(0)==null,"input pointer reread after allocator boundary");});
   Scenario(delegate{var h=Empty();var r=new Role();r.Handler24C0.Write(h);var b=new IntegrationPorts{Role=r};var c=new Controller();c.CurrentA0.Write(null);c.PendingA8.Write(null);var a=new ActionRecord();a.PreAction18.Write(0);Check(Dispatch.RunLocal(c,a,new Environment(),b).ReturnedNormally,"real Check-to-Local-to-Execute composition with observed virtual results");Check(b.GetAICount==2&&b.StartCount==1&&Same(h.Current08.Read(0),a),"same borrowed action arrives at start boundary");Check(h.Busy18.Read(0)==0&&c.CurrentA0.Read(0)==null,"composed final state");});
   Console.WriteLine("ActionExecutor: "+Scenarios+" scenarios, "+Assertions+" assertions passed (isolated contracts; allocator fixture is not native proof)");
  }
 }
 public static class TestProgram {
  public static int Main(){try{ActionDispatchTests.Run();ActionExecutorTests.Run();Console.WriteLine("PASS isolated action contracts; no native runtime or R1 gameplay acceptance");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
 }
}
