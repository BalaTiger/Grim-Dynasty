using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Reuses only saved Scene/Composition constructors. Explicit OWN hot inputs
// below are not a recovered native cold start; no callback supplies a result.
public static class GameControllerMoveBeginIntegrationTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}public static int Assertions{get{return assertions;}}
 public static int AffectedRegressionScenarios{get{return 2;}}public static int NewScenarios{get{return scenarios-AffectedRegressionScenarios;}}
 public static int PriorFixtureSetupChecks;
 public static bool GateCrossed,AssociatedMissReturned,DistanceReturned,SkillReturned,AlreadyThereReturned;
 public static uint SavedCorridorCount,NextStopRva;
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract error){Eq(at,error.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Unknown<T>(Field<T> value,string why){Missing(0,delegate{value.Read(0);},why);}
 static void Throws<T>(Action body,string why)where T:Exception{try{body();}catch(T){Check(true,why);return;}throw new Exception("Expected "+typeof(T).Name+": "+why);}
 static void Test(string name,Action body){try{body();scenarios++;}catch(Exception error){throw new Exception(name+": "+error.Message,error);}}
 static GameControllerMoveProductionIntegrationTests.Composition New(NavigationSearchWrapperTests.Scene scene,bool bind){var c=new GameControllerMoveProductionIntegrationTests.Composition(scene,bind);PriorFixtureSetupChecks+=2;return c;}
 static void OwnInputs(GameControllerMoveProductionIntegrationTests.Composition c){
  var f=c.Request;c.Production.BindBeginPath(f.Request.Path);f.Move.Gate26AF.Write(0);f.Move.AssociatedId16C0.Write(0);f.Move.CurrentSkillA90.Write(0);
  // Receiver is an explicit OWN object;224770's supported branch never reads it.
  f.Engine.World50.Write(f.World);f.Movement.AlreadyThereConstant7A7FD8.Write(.025f);
  c.Skills.Offset3BC.Write(0);c.Skills.Offset3C0.Write(0);c.Skills.Offset3C4.Write(0);c.Skills.Offset3C8.Write(0);
 }
 static void Direct(GameControllerMoveProductionIntegrationTests.Composition c,GameControllerMoveProductionInvocation call){var f=c.Request;GameControllerMoveProductionContracts.MoveTo124340(f.Controller,f.Position,Field<uint>.Of(0),Field<uint>.Of(0),Field<uint>.Of(5),Field<float>.Of(100),c.Production,call);}
 static void CopyPosition(WorldPosition24 a,WorldPosition24 b){b.Q0.CopyFrom(a.Q0);b.D8.CopyFrom(a.D8);b.DC.CopyFrom(a.DC);b.D10.CopyFrom(a.D10);b.D14.CopyFrom(a.D14);}
 static void Miss(GameAssociatedLookupInvocation c,GameControllerMoveProductionIntegrationTests.Composition owner){
  Check(c.LookupReturned&&c.Lookup.Returned&&c.Returned&&!c.ClassReturned,"Real077D60 returned after actual map miss without class call");Same(null,c.Result.Read(0),"Actual lookup result is null");Eq(4,c.Lookup.HashedBytes,"Actual map hashes four input bytes");Eq(1,c.Lookup.VisitedNodes,"Actual populated bucket compared the other key");Check(c.Lookup.OwnLockEntered&&c.Lookup.OwnLockExited,"Real map lock released normally");Eq(0,owner.Request.Lock.OwnOutstandingEnters,"No outstanding ObjectManager lock");
 }
 static void Incomplete(GameControllerMoveProductionIntegrationTests.Composition c,GameControllerMoveProductionInvocation call,uint at){
  Check(call.ConstructorReturned&&call.Handle.BeginEntered&&!call.Handle.BeginReturned&&!call.Handle.Returned&&!call.Returned,"Production Begin remains incomplete");Same(call.AllocatedAction,c.Handler.Current08.Read(0),"Same action remains published at failure");Eq((byte)1,c.Handler.Busy18.Read(0),"Busy prefix retained, no fabricated cleanup");Eq(at,call.Native.At,"Outer preserves exact nested stop");Eq(at,call.Handle.BeginNative.At,"Begin preserves exact nested stop");Eq(0,c.Request.Lock.OwnOutstandingEnters,"No lookup lock leak");
 }
 public static void Run(){
  scenarios=assertions=PriorFixtureSetupChecks=0;GateCrossed=AssociatedMissReturned=DistanceReturned=SkillReturned=AlreadyThereReturned=false;SavedCorridorCount=NextStopRva=0;var scene=new NavigationSearchWrapperTests.Scene();PriorFixtureSetupChecks+=4;
  Test("Bound adapters preserve unknown Gate26AF",delegate{
   var c=New(scene,true);c.Production.BindBeginPath(c.Request.Request.Path);var call=new GameControllerMoveProductionInvocation();Missing(0x082c98,delegate{Direct(c,call);},"Binding never initializes Gate26AF");Incomplete(c,call,0x082c98);Eq(0,call.Handle.AssociatedCalls.Count,"No association after unknown gate");Eq(0,call.Handle.DistanceCalls.Count,"No distance after unknown gate");Unknown(c.Request.Move.Gate26AF,"Gate remains unknown");Unknown(c.Request.Move.AttackFlags2688,"No later attack prefix");
  });
  Test("Saved-map request crosses actual association distance skill and AlreadyThere",delegate{
   var c=New(scene,true);OwnInputs(c);var f=c.Request;Missing(0x08eaf0,delegate{f.Outer(new Invocation());},"Real next dependency remains MovementManager MoveTo");var entry=c.Entry;var call=entry.MoveProductionCall;var h=call.Handle;Incomplete(c,call,0x08eaf0);
   Check(f.Boundaries.LastRequest.PathReturned&&f.Boundaries.LastRequest.Path.Precheck.NavigationReturned&&entry.RunSpeedReturned,"Saved map path and real speed returned");SavedCorridorCount=f.Boundaries.LastRequest.Path.Precheck.Navigation.CorridorCount.Read(0);Check(SavedCorridorCount>1,"Actual route spans saved polygons");Eq(1,h.AssociatedCalls.Count,"One actual association lookup before movement");Miss(h.AssociatedCalls[0],c);Eq(1,h.DistanceCalls.Count,"One real224770 call");Check(h.DistanceCalls[0].Returned&&h.DistanceCalls[0].SameRegion,"Same-region native distance branch returned");Eq(0f,h.DistanceResults[0].Read(0),"Captured current start matches second coordinate result");
   Eq(3,h.CoordinateReturns.Count,"Two Begin coords plus original manager-owner coords");Same(h.CoordinateBuffers[0],h.CoordinateBuffers[1],"Both Begin calls reuse exact hidden64B frame");Check(!Object.ReferenceEquals(h.CoordinateBuffers[1],h.CoordinateBuffers[2]),"AlreadyThere has its distinct native local");Same(h.CoordinateWordBuffers[2],h.CoordinateBuffers[2].AddressIdentity,"AlreadyThere local preserves hidden identity");
   Eq(1,h.SkillIdCalls.Count,"Real SetAttackTarget531C50 executed");Eq(0x531d48u,h.SkillIdCalls[0].At,"Actual empty-vector reference search returned");Eq(0u,f.Move.AttackSkill26A8.Read(0),"Real returned skill ID was stored");Eq(0u,f.Move.AttackFlags2688.Read(0),"Actual attack flags stored");Eq(1,h.AlreadyThereCalls.Count,"Actual manager distance ran");Eq(1,h.DifferenceCalls.Count,"Actual22C240 ran");Eq(0x22c2d8u,h.DifferenceCalls[0].At,"Concrete Region offset subtraction returned");
   Same(f.Manager,f.Move.MovementDC0.Read(0),"Original movement manager used");Same(f.Move,f.Manager.Owner00.Read(0),"Original owner used");Eq((byte)0,f.Manager.Moving38.Read(0),"No movement success invented");Unknown(f.Move.AttackPosition2690.Q16,"Unwritten high padding remains unknown");Missing(0,delegate{c.Production.ControllerBaseCharacterClass.Read(0);},"Miss did not initialize unknown class symbol");
   GateCrossed=AssociatedMissReturned=DistanceReturned=SkillReturned=AlreadyThereReturned=true;NextStopRva=0x08eaf0;
  });
  Test("Unknown skill offset preserves preceding attack writes",delegate{
   var c=New(scene,true);OwnInputs(c);c.Skills.Offset3BC.Forget();var call=new GameControllerMoveProductionInvocation();Missing(0x531c8c,delegate{Direct(c,call);},"Empty vectors do not skip native offset read");Incomplete(c,call,0x531c8c);Miss(call.Handle.AssociatedCalls[0],c);Check(call.Handle.DistanceCalls[0].Returned,"Distance really completed first");Eq(0u,c.Request.Move.AttackFlags2688.Read(0),"Flags write survives failure");Eq(c.Request.Position.Q0.Read(0),c.Request.Move.AttackPosition2690.Q0.Read(0),"Destination prefix already copied");Unknown(c.Request.Move.AttackSkill26A8,"No skill store before531C50 returns");Eq(0,call.Handle.AlreadyThereCalls.Count,"No later movement attempt");
  });
  Test("Returned unknown distance fails only at caller COMISS",delegate{
   var c=New(scene,true);OwnInputs(c);c.Network.NetworkFlag6A8.Forget();var call=new GameControllerMoveProductionInvocation();Missing(0x078620,delegate{Direct(c,call);},"Obtain constructed action without starting Begin");var start=WorldPositionWordBridge.FromWords(call.AllocatedAction.Start48).Dwords;start.Q0.Write(0);c.Network.NetworkFlag6A8.Write(0);Missing(0x082cfe,delegate{GameControllerMoveProductionContracts.HandleAction127DD0(c.Request.Controller,call.AllocatedAction,c.Production,call.Handle);},"Unknown338458 survives callee return then failsCOMISS");var h=call.Handle;Miss(h.AssociatedCalls[0],c);Check(h.DistanceCalls[0].Returned&&h.DistanceCalls[0].NullRegion,"Actual null-region callee returned");Unknown(h.DistanceResults[0],"No infinity invented");Eq(0x082cfeu,h.BeginNative.At,"Caller consumer stop retained");Eq(0,h.SkillIdCalls.Count,"No skill call after comparison failure");Unknown(c.Request.Move.AttackFlags2688,"No attack writes before comparison returns");
  });
  Test("Real distance greater than100 preserves reposition stop",delegate{
   var c=New(scene,true);OwnInputs(c);c.Network.NetworkFlag6A8.Forget();var call=new GameControllerMoveProductionInvocation();Missing(0x078620,delegate{Direct(c,call);},"Construct before explicit OWN position mutation");var start=WorldPositionWordBridge.FromWords(call.AllocatedAction.Start48).Dwords;float x=NavigationCoreContracts.FromBits(start.D8.Read(0));start.D8.Write(NavigationCoreContracts.ToBits(x+11));c.Network.NetworkFlag6A8.Write(0);Missing(0x05d9a0,delegate{GameControllerMoveProductionContracts.HandleAction127DD0(c.Request.Controller,call.AllocatedAction,c.Production,call.Handle);},"Actual reposition remains uncomposed");Eq(121f,call.Handle.DistanceResults[0].Read(0),"Real binary32 subtraction/square result");Eq(0,call.Handle.SkillIdCalls.Count,"No later skill path");Unknown(c.Request.Move.AttackFlags2688,"No attack writes after unresolved reposition");
  });
  Test("AlreadyThere must read same-token Region offsets",delegate{
   var c=New(scene,true);OwnInputs(c);var region=c.Request.Regions.Region(77,0);var old=region.Offset3C.Snapshot();region.Offset3C.Forget();var call=new GameControllerMoveProductionInvocation();try{Missing(0x22c261,delegate{Direct(c,call);},"Equal Region tokens do not cancel unknownoffsets");Incomplete(c,call,0x22c261);Check(call.Handle.DistanceCalls[0].Returned,"224770 same-token distance needs no offsets");Eq(0x531d48u,call.Handle.SkillIdCalls[0].At,"Skill reference completed before AlreadyThere");Eq(1,call.Handle.AlreadyThereCalls.Count,"AlreadyThere entered");Eq(1,call.Handle.DifferenceCalls.Count,"22C240 attempted actual offset reads");}finally{region.Offset3C.CopyFrom(old);}
  });
  Test("AlreadyThere true executes stop and a second real association lookup",delegate{
   var c=New(scene,true);OwnInputs(c);var f=c.Request;CopyPosition(f.Owner.Entity.CoordsD8.Position00,f.Position);f.Manager.Registered6C.Write(0);f.Manager.Moving38.Write(7);var call=new GameControllerMoveProductionInvocation();Missing(0x082ddf,delegate{Direct(c,call);},"After actual same-position branch unknown state458 remains explicit");Incomplete(c,call,0x082ddf);Eq(2,call.Handle.AssociatedCalls.Count,"Begin and same-position notification each reread real map");Miss(call.Handle.AssociatedCalls[0],c);Miss(call.Handle.AssociatedCalls[1],c);Eq((byte)0,f.Manager.Moving38.Read(0),"Actual inline stop clears previous moving byte");Eq(2,call.Handle.SkillIdCalls.Count,"SetAttackTarget and post-Move SkillId both really ran");Eq(1,call.Handle.AlreadyThereCalls.Count,"Single actual AlreadyThere call");
  });
  Test("Gate rejection uses real Stop18 skill adapter without fake cleanup",delegate{
   var c=New(scene,true);OwnInputs(c);c.Request.Move.Gate26AF.Write(1);c.Skills.Offset3BC.Forget();var call=new GameControllerMoveProductionInvocation();Missing(0x531c8c,delegate{Direct(c,call);},"Stop18 also consumes actual offset3BC");Incomplete(c,call,0x531c8c);Check(call.Handle.StopEntered&&!call.Handle.StopReturned,"FailStart invoked actual incomplete Stop18");Eq(0x531c8cu,call.Handle.StopNative.At,"Stop retains exact531C50 failure");Eq(0,call.Handle.AssociatedCalls.Count,"Gate rejection skips Begin association");Eq(0,call.Handle.DistanceCalls.Count,"Gate rejection skips distance");Eq(1,call.Handle.SkillIdCalls.Count,"Stop uses actual skill adapter");Eq((byte)1,call.AllocatedAction.Flag11.Read(0),"Late StopFlag clear not fabricated");
  });
  Test("Binding rejects other ownership and unbound distance remains Unsupported",delegate{
   var c=New(scene,true);OwnInputs(c);var f=c.Request;c.Production.BindBeginPath(f.Request.Path);Same(f.Request.Path,c.Production.BeginPath,"Same path binding idempotent");Throws<ArgumentNullException>(delegate{c.Production.BindBeginPath(null);},"Null path rejected");var other=new GamePathPrecheckContext(f.Bridge,f.Movement,f.Request.Path.Globals,f.Request.Path.Navigation);Throws<InvalidOperationException>(delegate{c.Production.BindBeginPath(other);},"No rebinding even with matching fields");var wrong=new GamePathPrecheckContext(f.Bridge,new MoveContext(),f.Request.Path.Globals,f.Request.Path.Navigation);Throws<ArgumentException>(delegate{c.Production.BindBeginPath(wrong);},"OtherMoveContext rejected");
   var u=New(scene,true);u.Request.Move.Gate26AF.Write(0);u.Request.Move.AssociatedId16C0.Write(0);u.Request.Engine.World50.Write(u.Request.World);var call=new GameControllerMoveProductionInvocation();Missing(0x609a10,delegate{Direct(u,call);},"Without explicit BeginPath real association returns then original distance stop remains");Miss(call.Handle.AssociatedCalls[0],u);Eq(0,call.Handle.DistanceCalls.Count,"No adapter silently bound");
  });
  Test("Real nonnull association cannot invent its target class or slots",delegate{
   var c=New(scene,true);OwnInputs(c);c.Request.Move.AssociatedId16C0.Write(0x1234);var call=new GameControllerMoveProductionInvocation();Missing(0x077d82,delegate{Direct(c,call);},"Actual map hit and PlayerClass cannot fabricate unknownA5C9F0");Incomplete(c,call,0x077d82);var a=call.Handle.AssociatedCalls[0];Check(a.LookupReturned&&a.ClassReturned&&!a.Returned,"Actual nonnull class prefix retained");Same(c.Request.Move,a.Lookup.Result.Read(0),"Actual hit preserves original role identity");Eq(0,call.Handle.DistanceCalls.Count,"No unproved association slot60 completion");
  });
  Test("Unbound production still stops before allocation",delegate{
   var c=New(scene,false);Missing(0x124340,delegate{c.Request.Outer(new Invocation());},"Original state producer stop retained");Same(null,c.Entry.MoveProductionCall,"No invented action invocation");Same(null,c.Handler.Current08.Read(0),"Handler untouched");Eq((byte)0,c.Handler.Busy18.Read(0),"No busy publication");
  });
 }
}
