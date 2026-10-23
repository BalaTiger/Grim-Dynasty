using System;
using System.Collections.Generic;

namespace GrimDynasty.ReverseClient.Actions {
 // Observations exercise the saved dispatch contract. They do not implement or
 // validate the native virtual methods supplied by these test boundaries.
 public static class ActionDispatchTests {
  public static int Assertions { get; private set; }
  public static int Scenarios { get; private set; }

  private sealed class Observations : Boundaries {
   public Func<Controller,Role> AI;
   public Func<ActionRecord,int> Kind;
   public Func<ActionRecord,byte> Active;
   public Func<ActionRecord,ActionRecord,ActionRecord,int> Resolve;
   public Func<Role,byte> Running;
   public Action<Role> RoleCall;
   public Action Log;
   public Action<ActionRecord,uint> Destroy;
   public Action<ActionRecord> Finish;
   public Func<ActionRecord,ActionRecord> Pre;
   public Func<Controller,ActionRecord,Environment,Invocation,int> Permission;
   public Action<Handler,ActionRecord,Counter,Invocation> Execution;
   public override Role GetAI(Controller c) { return AI==null?base.GetAI(c):AI(c); }
   public override int Kind58(ActionRecord a) { return Kind==null?base.Kind58(a):Kind(a); }
   public override byte Active30(ActionRecord a) { return Active==null?base.Active30(a):Active(a); }
   public override int Resolve80(ActionRecord a,ActionRecord b,ActionRecord c) { return Resolve==null?base.Resolve80(a,b,c):Resolve(a,b,c); }
   public override byte IsRunningSkill(Role r) { return Running==null?base.IsRunningSkill(r):Running(r); }
   public override void Role8A8(Role r) { if(RoleCall==null)base.Role8A8(r);else RoleCall(r); }
   public override void LogMissingRole() { if(Log==null)base.LogMissingRole();else Log(); }
   public override void Destroy0(ActionRecord a,uint f) { if(Destroy==null)base.Destroy0(a,f);else Destroy(a,f); }
   public override void Finish20(ActionRecord a) { if(Finish==null)base.Finish20(a);else Finish(a); }
   public override ActionRecord PreAction60(ActionRecord a) { return Pre==null?base.PreAction60(a):Pre(a); }
   public override int Check(Controller c,ActionRecord a,Environment e,Invocation i) { return Permission==null?base.Check(c,a,e,i):Permission(c,a,e,i); }
   public override void Execute(Handler h,ActionRecord a,Counter n,Invocation i) { if(Execution==null)base.Execute(h,a,n,i);else Execution(h,a,n,i); }
  }

  private static void Assert(bool value,string message) {
   Assertions++;if(!value)throw new Exception(message);
  }
  private static void Same(object actual,object expected,string message) {
   Assert(Object.ReferenceEquals(actual,expected),message);
  }
  private static void Normal(Outcome result) {
   Assert(result.ReturnedNormally,"Expected normal return: "+result.Boundary+" / "+result.Exception);
  }
  private static void Missing(Outcome result,uint address) {
   Assert(!result.ReturnedNormally,"Missing contract must stop");
   Assert(result.Exception is MissingContract,"Stop must identify a missing contract");
   Assert(result.Address==address,"Wrong missing-contract address");
  }
  private static void ExpectMissing(Action action,uint address) {
   MissingContract error=null;
   try { action(); } catch(MissingContract x) { error=x; }
   Assert(error!=null,"Expected missing contract, not a default result");
   Assert(error.Address==address,"Wrong direct missing-contract address");
  }
  private static void Sequence(List<string> actual,params string[] expected) {
   Assert(String.Join(",",actual.ToArray())==String.Join(",",expected),"Unexpected callback order: "+String.Join(",",actual.ToArray()));
  }
  private static ActionRecord ActionWithPre(byte flag) {
   var a=new ActionRecord();a.PreAction18.Write(flag);return a;
  }
  private static Controller ControllerWith(ActionRecord current,ActionRecord pending) {
   var c=new Controller();c.CurrentA0.Write(current);c.PendingA8.Write(pending);return c;
  }
  private static Handler HandlerWith(ActionRecord current) {
   var h=new Handler();h.Current08.Write(current);return h;
  }
  private static Role RoleWith(Handler handler) {
   var r=new Role();r.Handler24C0.Write(handler);return r;
  }
  private static long Cell(int incoming,int current) {
   return 0x3616cL+((long)incoming*26L+(long)current)*4L;
  }
  private static Environment EnvironmentWith(int incoming,int current,int permission) {
   var e=new Environment();var engine=new Engine();engine.PermissionCells.Add(Cell(incoming,current),permission);e.EngineGlobal.Write(engine);return e;
  }
  private static Observations LocalBoundary(Role role,int permission) {
   return new Observations {
    AI=c=>role,
    Permission=(c,a,e,i)=>permission
   };
  }
  private static void Scenario(string name,Action body) {
   try { body();Scenarios++; }
   catch(Exception x) { throw new Exception("ActionDispatchTests: "+name,x); }
  }

  public static void Run() {
   Assertions=0;Scenarios=0;
   Scenario("permission 0 and pre-action byte zero",LocalZero);
   Scenario("permission 1 destroys identical pending before publishing",LocalPendingSameIdentity);
   Scenario("permission 2",()=>LocalReject(false));
   Scenario("permission 2 with null input",()=>LocalReject(true));
   Scenario("permission 3 has no invented execution",()=>LocalOther(3));
   Scenario("unknown permission has no invented execution",()=>LocalOther(-7));
   Scenario("permission 4, independent roles and late handler",LocalIndependentRoles);
   Scenario("cleanup reentry and late pending reads",LocalCleanupReentry);
   Scenario("pre-action publication after reentry",()=>LocalPrePublication(false));
   Scenario("pre-action may return input identity",()=>LocalPrePublication(true));
   Scenario("same action is not deduplicated during cleanup",LocalRepeatedIdentity);
   Scenario("unknown pre-action byte",()=>LocalUnsupportedPre(null));
   Scenario("nonzero pre-action byte",()=>LocalUnsupportedPre(1));
   Scenario("noncanonical nonzero pre-action byte",()=>LocalUnsupportedPre(0x80));
   Scenario("missing original role logs then cleans input",()=>LocalMissingRole(false));
   Scenario("missing original role with null input",()=>LocalMissingRole(true));
   Scenario("callback interruption preserves prefix",LocalInterruptedPrefix);
   Scenario("CheckAction can lose its independent role",LocalCheckRoleDisappears);
   Scenario("second kind is gate only and first kind is signed",CheckTwoKinds);
   Scenario("both permission indices are signed",CheckSignedIndices);
   Scenario("permission cell must be observed",CheckNoDefaultPermission);
   Scenario("unknown engine is not an empty matrix",CheckUnknownEngine);
   Scenario("active, global, pending and Resolve80 rereads",CheckResolveReentry);
   Scenario("matrix 3 can find current action gone",CheckResolveCurrentGone);
   Scenario("running skill returns before unknown matrix",CheckRunningShortCircuit);
   Scenario("current kind 8 skips unknown skill predicate",CheckKindEight);
   Scenario("second kind gate returns before unknown skill and matrix",CheckEarlyGate);
   Console.WriteLine("ActionDispatchTests: {0} scenarios, {1} assertions passed",Scenarios,Assertions);
  }

  private static void LocalZero() {
   var c=ControllerWith(null,null);var a=ActionWithPre(0);var h=HandlerWith(null);var b=LocalBoundary(RoleWith(h),0);int executions=0;
   b.Execution=(handler,selected,budget,i)=>{
    executions++;Same(handler,h,"Original role handler");Same(selected,a,"Input identity");
    Assert(budget.Value.Read(0)==100,"Local budget starts at 100");Assert(i.At==0x127f6a,"Executor call site");
    Same(c.CurrentA0.Read(0),null,"No wrapper published for null pre-action");Same(c.PendingA8.Read(0),null,"Pending cleared");
   };
   Normal(Dispatch.RunLocal(c,a,new Environment(),b));Assert(executions==1,"Exactly one executor call");
  }

  private static void LocalPendingSameIdentity() {
   var a=ActionWithPre(0);var keep=new ActionRecord();var overwritten=new ActionRecord();var c=ControllerWith(keep,a);var b=LocalBoundary(new Role(),1);int destroys=0;
   b.Destroy=(target,flags)=>{
    destroys++;Same(target,a,"Identical pending must still be destroyed");Assert(flags==1,"Destructor flag");
    Same(c.PendingA8.Read(0),a,"Pending is not cleared before destructor");c.PendingA8.Write(overwritten);
   };
   Normal(Dispatch.RunLocal(c,a,new Environment(),b));Assert(destroys==1,"One pending destructor");
   Same(c.PendingA8.Read(0),a,"Publication overwrites reentrant pending");Same(c.CurrentA0.Read(0),keep,"Queue permission preserves current");
  }

  private static void LocalReject(bool nullInput) {
   var a=nullInput?null:new ActionRecord();var keep=new ActionRecord();var pending=new ActionRecord();var c=ControllerWith(keep,pending);var b=LocalBoundary(new Role(),2);int destroys=0;
   b.Destroy=(target,flags)=>{destroys++;Same(target,a,"Rejected input identity");Assert(flags==1,"Reject destructor flag");};
   Normal(Dispatch.RunLocal(c,a,new Environment(),b));Assert(destroys==(nullInput?0:1),"Null reject does not dereference input");
   Same(c.CurrentA0.Read(0),keep,"Reject preserves current");Same(c.PendingA8.Read(0),pending,"Reject preserves pending");
  }

  private static void LocalOther(int permission) {
   var keep=new ActionRecord();var pending=new ActionRecord();var c=ControllerWith(keep,pending);var b=LocalBoundary(new Role(),permission);
   Normal(Dispatch.RunLocal(c,new ActionRecord(),new Environment(),b));
   Same(c.CurrentA0.Read(0),keep,"Other result preserves current");Same(c.PendingA8.Read(0),pending,"Other result preserves pending");
  }

  private static void LocalIndependentRoles() {
   var a=ActionWithPre(0);var active=new ActionRecord();var initialHandler=HandlerWith(null);var lateHandler=HandlerWith(null);
   var originalRole=RoleWith(initialHandler);var checkedRole=RoleWith(HandlerWith(active));var c=ControllerWith(null,null);
   int aiCalls=0,roleCalls=0,executions=0;var b=new Observations();
   b.AI=owner=>{Same(owner,c,"Controller identity on both role lookups");aiCalls++;Assert(aiCalls<=2,"No extra role lookup");return aiCalls==1?originalRole:checkedRole;};
   b.Kind=target=>{if(Object.ReferenceEquals(target,a))return 4;Same(target,active,"Type comes from CheckAction's role");return 8;};
   b.Active=target=>{Same(target,active,"Active predicate uses CheckAction's role");return 1;};
   b.RoleCall=role=>{roleCalls++;Same(role,originalRole,"Permission 4 uses the initial role");originalRole.Handler24C0.Write(lateHandler);};
   b.Execution=(handler,selected,budget,i)=>{executions++;Same(handler,lateHandler,"Read initial role's replacement handler late");Same(selected,a,"Execute original input");};
   Normal(Dispatch.RunLocal(c,a,EnvironmentWith(4,8,4),b));
   Assert(aiCalls==2,"Local and Check perform independent GetAI");Assert(roleCalls==1&&executions==1,"Permission 4 proceeds to execution");
  }

  private static void LocalCleanupReentry() {
   var old=new ActionRecord();var stalePending=new ActionRecord();var fromFinish=new ActionRecord();var fromDestroy=new ActionRecord();
   var latePending=new ActionRecord();var discardedPending=new ActionRecord();var survivingCurrent=new ActionRecord();var a=ActionWithPre(0);
   var c=ControllerWith(old,stalePending);var role=RoleWith(HandlerWith(null));var lateHandler=HandlerWith(null);var b=LocalBoundary(role,0);var order=new List<string>();
   b.Finish=target=>{order.Add("finish");Same(target,old,"Finish saved old current");Same(c.CurrentA0.Read(0),null,"Current cleared before finish");c.CurrentA0.Write(fromFinish);};
   b.Destroy=(target,flags)=>{
    Assert(flags==1,"Cleanup destructor flag");
    if(Object.ReferenceEquals(target,fromFinish)){
     order.Add("destroy-current");Same(c.CurrentA0.Read(0),fromFinish,"Current reread after finish");c.CurrentA0.Write(fromDestroy);c.PendingA8.Write(latePending);
    } else {
     order.Add("destroy-pending");Same(target,latePending,"Pending read after current destructor");Same(c.CurrentA0.Read(0),null,"Current cleared again before pending destructor");
     Same(c.PendingA8.Read(0),latePending,"Pending not pre-cleared");c.CurrentA0.Write(survivingCurrent);c.PendingA8.Write(discardedPending);
    }
   };
   b.Pre=target=>{order.Add("pre");Same(target,a,"Pre input");Same(c.PendingA8.Read(0),null,"Post-destructor clear overwrites pending publication");Same(c.CurrentA0.Read(0),survivingCurrent,"Pending callback's current survives");role.Handler24C0.Write(lateHandler);return null;};
   b.Execution=(handler,selected,budget,i)=>{order.Add("execute");Same(handler,lateHandler,"Handler reread after pre-action");Same(selected,a,"Null pre keeps input");Same(c.CurrentA0.Read(0),survivingCurrent,"No extra clear on null pre path");};
   Normal(Dispatch.RunLocal(c,a,new Environment(),b));Sequence(order,"finish","destroy-current","destroy-pending","pre","execute");
  }

  private static void LocalPrePublication(bool aliasInput) {
   var a=ActionWithPre(0);var pre=aliasInput?a:new ActionRecord();var madeDuringPre=new ActionRecord();var madeDuringDestroy=new ActionRecord();var pending=new ActionRecord();
   var c=ControllerWith(null,null);var b=LocalBoundary(RoleWith(HandlerWith(null)),0);var order=new List<string>();
   b.Pre=target=>{order.Add("pre");c.CurrentA0.Write(madeDuringPre);c.PendingA8.Write(pending);return pre;};
   b.Destroy=(target,flags)=>{order.Add("destroy");Same(target,madeDuringPre,"Fresh current after pre callback");Assert(flags==1,"Pre replacement destructor flag");c.CurrentA0.Write(madeDuringDestroy);};
   b.Execution=(handler,selected,budget,i)=>{order.Add("execute");Same(selected,pre,"Use actual returned pre identity");Same(c.CurrentA0.Read(0),a,"Publish original input after destructor");Same(c.PendingA8.Read(0),pending,"No unproved pending clear after pre callback");};
   Normal(Dispatch.RunLocal(c,a,new Environment(),b));Sequence(order,"pre","destroy","execute");
  }

  private static void LocalRepeatedIdentity() {
   var a=ActionWithPre(0);var c=ControllerWith(a,a);var b=LocalBoundary(RoleWith(HandlerWith(null)),0);int finishes=0,destroys=0,executions=0;
   b.Finish=target=>{finishes++;Same(target,a,"Same input can be old current");c.CurrentA0.Write(a);};
   b.Destroy=(target,flags)=>{destroys++;Same(target,a,"Same identity is not deduplicated");Assert(flags==1,"Repeated destructor flag");};
   b.Execution=(handler,selected,budget,i)=>{executions++;Same(selected,a,"Identity forwarded even after observed cleanup calls");};
   Normal(Dispatch.RunLocal(c,a,new Environment(),b));Assert(finishes==1&&destroys==2&&executions==1,"Preserve repeated lifecycle calls without ownership invention");
  }

  private static void LocalUnsupportedPre(byte? flag) {
   var a=new ActionRecord();if(flag.HasValue)a.PreAction18.Write(flag.Value);
   var c=ControllerWith(null,null);var b=LocalBoundary(RoleWith(HandlerWith(null)),0);int executions=0;b.Execution=(h,x,n,i)=>{executions++;};
   Missing(Dispatch.RunLocal(c,a,new Environment(),b),flag.HasValue?0x081899u:0x08188fu);
   Assert(executions==0,"Unsupported pre-action must not execute");Same(c.CurrentA0.Read(0),null,"Current cleanup prefix retained");Same(c.PendingA8.Read(0),null,"Pending cleanup prefix retained");
  }

  private static void LocalMissingRole(bool nullInput) {
   var a=nullInput?null:new ActionRecord();var c=new Controller();var b=new Observations();var order=new List<string>();
   b.AI=owner=>null;b.Log=()=>order.Add("log");b.Destroy=(target,flags)=>{order.Add("destroy");Same(target,a,"Missing-role input identity");Assert(flags==1,"Missing-role destructor flag");};
   Normal(Dispatch.RunLocal(c,a,new Environment(),b));Sequence(order,nullInput?new[]{"log"}:new[]{"log","destroy"});
  }

  private static void LocalInterruptedPrefix() {
   var old=new ActionRecord();var published=new ActionRecord();var pending=new ActionRecord();var c=ControllerWith(old,pending);var b=LocalBoundary(new Role(),0);var failure=new Exception("observed callback interruption");
   b.Finish=target=>{Same(c.CurrentA0.Read(0),null,"Clear precedes interrupted finish");c.CurrentA0.Write(published);throw failure;};
   var result=Dispatch.RunLocal(c,ActionWithPre(0),new Environment(),b);
   Assert(!result.ReturnedNormally,"Callback interruption stops local path");Assert(result.Address==0x127ee7,"Interruption call site");Same(result.Exception,failure,"Preserve actual callback exception");
   Same(c.CurrentA0.Read(0),published,"No invented rollback/finally clear");Same(c.PendingA8.Read(0),pending,"Unreached pending clear stays unreached");
  }

  private static void LocalCheckRoleDisappears() {
   var old=new ActionRecord();var pending=new ActionRecord();var c=ControllerWith(old,pending);var firstRole=new Role();var b=new Observations();int aiCalls=0;
   b.AI=owner=>{aiCalls++;return aiCalls==1?firstRole:null;};b.Kind=target=>4;
   Missing(Dispatch.RunLocal(c,ActionWithPre(0),new Environment(),b),0x16011a);
   Assert(aiCalls==2,"Second lookup can disappear");Same(c.CurrentA0.Read(0),old,"No cleanup after failed independent lookup");Same(c.PendingA8.Read(0),pending,"Pending unchanged before permission");
  }

  private static void CheckTwoKinds() {
   var a=new ActionRecord();var c=ControllerWith(null,null);var role=RoleWith(HandlerWith(null));int kinds=0,running=0;var b=new Observations();
   b.AI=owner=>role;b.Kind=target=>{Same(target,a,"Only incoming kind calls");kinds++;return kinds==1?-2:8;};b.Running=r=>{running++;Same(r,role,"Skill role");return 0;};
   var e=EnvironmentWith(-2,0,2);e.EngineGlobal.Read(0).PermissionCells.Add(Cell(8,0),1);
   Assert(Dispatch.CheckAction(c,a,e,b,new Invocation())==2,"Matrix uses first signed kind, not second gate kind");Assert(kinds==2&&running==1,"Two kind reads and one skill check");
  }

  private static void CheckSignedIndices() {
   var a=new ActionRecord();var active=new ActionRecord();var role=RoleWith(HandlerWith(active));var b=new Observations();int kinds=0;
   b.AI=c=>role;b.Active=x=>1;b.Kind=x=>{kinds++;return Object.ReferenceEquals(x,a)?-2:-3;};b.Running=r=>0;
   Assert(Dispatch.CheckAction(ControllerWith(null,null),a,EnvironmentWith(-2,-3,-9),b,new Invocation())==-9,"Negative row and column use signed displacement; cell value returned unchanged");
   Assert(kinds==2,"Active gate skips second incoming kind");
  }

  private static Observations EmptyCurrentCheck(Role role) {
   return new Observations { AI=c=>role,Kind=a=>8,Running=r=>0 };
  }
  private static void CheckNoDefaultPermission() {
   var e=new Environment();e.EngineGlobal.Write(new Engine());var b=EmptyCurrentCheck(RoleWith(HandlerWith(null)));
   ExpectMissing(()=>{Dispatch.CheckAction(ControllerWith(null,null),new ActionRecord(),e,b,new Invocation());},0x160195);
  }
  private static void CheckUnknownEngine() {
   var b=EmptyCurrentCheck(RoleWith(HandlerWith(null)));
   ExpectMissing(()=>{Dispatch.CheckAction(ControllerWith(null,null),new ActionRecord(),new Environment(),b,new Invocation());},0x16018e);
  }

  private static void CheckResolveReentry() {
   var incoming=new ActionRecord();var initial=new ActionRecord();var typed=new ActionRecord();var resolver=new ActionRecord();var pending=new ActionRecord();
   var firstHandler=HandlerWith(initial);var typedHandler=HandlerWith(typed);var resolvedHandler=HandlerWith(resolver);var role=RoleWith(firstHandler);var c=ControllerWith(null,null);
   var e=EnvironmentWith(4,5,0);var lateEngine=new Engine();lateEngine.PermissionCells.Add(Cell(4,5),3);var b=new Observations();var order=new List<string>();
   b.Kind=a=>{if(Object.ReferenceEquals(a,incoming)){order.Add("incoming-kind");return 4;}Same(a,typed,"Kind rereads handler after active callback");order.Add("current-kind");return 5;};
   b.AI=owner=>{order.Add("get-ai");return role;};
   b.Active=a=>{order.Add("active");Same(a,initial,"Initial active identity");role.Handler24C0.Write(typedHandler);return 1;};
   b.Running=r=>{order.Add("running");role.Handler24C0.Write(resolvedHandler);c.PendingA8.Write(pending);e.EngineGlobal.Write(lateEngine);return 0;};
   b.Resolve=(a,x,p)=>{order.Add("resolve");Same(a,resolver,"Resolve uses latest current identity");Same(x,incoming,"Resolve input identity");Same(p,pending,"Resolve reads latest pending");return 4;};
   Assert(Dispatch.CheckAction(c,incoming,e,b,new Invocation())==4,"Read replacement engine and forward Resolve80 result");
   Sequence(order,"incoming-kind","get-ai","active","current-kind","running","resolve");
  }

  private static void CheckResolveCurrentGone() {
   var incoming=new ActionRecord();var active=new ActionRecord();var h=HandlerWith(active);var role=RoleWith(h);var b=new Observations();
   b.AI=c=>role;b.Active=a=>1;b.Kind=a=>Object.ReferenceEquals(a,incoming)?4:5;
   b.Running=r=>{h.Current08.Write(null);return 0;};
   Assert(Dispatch.CheckAction(ControllerWith(null,null),incoming,EnvironmentWith(4,5,3),b,new Invocation())==0,"Matrix 3 with no fresh current returns zero without Resolve80");
  }

  private static void CheckRunningShortCircuit() {
   var incoming=new ActionRecord();var active=new ActionRecord();var b=new Observations();b.AI=c=>RoleWith(HandlerWith(active));b.Active=a=>1;b.Kind=a=>Object.ReferenceEquals(a,incoming)?4:5;b.Running=r=>1;
   Assert(Dispatch.CheckAction(ControllerWith(null,null),incoming,new Environment(),b,new Invocation())==0,"Running skill bypasses unobserved matrix");
  }
  private static void CheckKindEight() {
   var incoming=new ActionRecord();var active=new ActionRecord();var b=new Observations();b.AI=c=>RoleWith(HandlerWith(active));b.Active=a=>1;b.Kind=a=>Object.ReferenceEquals(a,incoming)?4:8;
   Assert(Dispatch.CheckAction(ControllerWith(null,null),incoming,EnvironmentWith(4,8,2),b,new Invocation())==2,"Current kind 8 skips missing IsRunningSkill boundary");
  }
  private static void CheckEarlyGate() {
   int kinds=0;var b=new Observations();b.AI=c=>RoleWith(HandlerWith(null));b.Kind=a=>{kinds++;return 4;};
   Assert(Dispatch.CheckAction(ControllerWith(null,null),new ActionRecord(),new Environment(),b,new Invocation())==0,"Gate returns without unknown skill or engine");Assert(kinds==2,"Gate rereads incoming kind");
  }
 }
}
