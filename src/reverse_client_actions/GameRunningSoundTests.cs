using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Actual05CDC0 pointer selection and conditional no-sound return only. The
// integrated candidate reuses saved Ground and the existing resource/Player
// path. A completed request is not animation decoding or continuous movement.
public static class GameRunningSoundTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}public static int Assertions{get{return assertions;}}
 public static int PriorFixtureSetupChecks;
 public static byte GroundOuterResult;
 public static bool GroundBeginReturned,GroundOuterReturned,GroundRequestReturned;
 public static uint GroundPathResult,GroundSoundReturnRva;
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract e){Eq(at,e.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Unknown<T>(Field<T> value,string why){Missing(0,delegate{value.Read(0);},why);}
 static void Argument(Action body,string why){try{body();}catch(ArgumentException){Check(true,why);return;}throw new Exception("Expected argument rejection: "+why);}
 static void Invalid(Action body,string why){try{body();}catch(InvalidOperationException){Check(true,why);return;}throw new Exception("Expected binding rejection: "+why);}
 static void Test(string name,Action body){try{body();scenarios++;}catch(Exception e){throw new Exception(name+": "+e.Message,e);}}
 static GameMovementCharacterState Owner(){return new GameMovementCharacterState(new EntityMovementState(new object()));}
 static int FixtureChecks(){return GameWaypointProductionIntegrationTests.Assertions+GameControllerMoveProductionIntegrationTests.Assertions+PlayerMoveRequestTests.Assertions+GameMoveBeginResourceIntegrationTests.Assertions+GameMoveBeginGroundIntegrationTests.Assertions;}
 public static void RunSharedCellRegression(){
  scenarios=assertions=PriorFixtureSetupChecks=0;
  Test("Typed full Entity sound cells alias Move in both directions including Unknown",delegate{
   var move=new MoveCharacter();var owner=new GameMovementCharacterState(new EntityMovementState(move));var fields=GameRunningSoundState.ForOwner(owner);
   Same(move,owner.Entity.Identity,"Explicit full owner identity");Same(move.Sound2210,fields.Sound2210,"Primary is the original Field");Same(move.Sound2110,fields.Sound2110,"Fallback is the original Field");Unknown(fields.Sound2210,"Binding does not initialize primary");Unknown(fields.Sound2110,"Binding does not initialize fallback");
   var first=new object();move.Sound2210.Write(first);Same(first,fields.Sound2210.Read(0),"Move write visible to Begin");var second=new object();fields.Sound2110.Write(second);Same(second,move.Sound2110.Read(0),"Begin write visible to Stop");
   fields.Sound2210.Forget();Unknown(move.Sound2210,"Unknown propagates to Stop");move.Sound2110.Forget();Unknown(fields.Sound2110,"Unknown propagates to Begin");Same(fields,GameRunningSoundState.ForOwner(owner),"Same sidecar is retained");
   var isolated=GameRunningSoundState.ForOwner(new GameMovementCharacterState(new EntityMovementState(new object())));Check(!Object.ReferenceEquals(fields.Sound2210,isolated.Sound2210),"Opaque independent owner stays independent");Unknown(isolated.Sound2210,"Isolated primary still Unknown");Unknown(isolated.Sound2110,"Isolated fallback still Unknown");
  });
 }
 public static void Run(){
  scenarios=assertions=PriorFixtureSetupChecks=0;GroundOuterResult=0;GroundBeginReturned=GroundOuterReturned=GroundRequestReturned=false;GroundPathResult=GroundSoundReturnRva=0;
  Test("Unknown primary pointer stops at TEST before fallback load",delegate{
   var owner=Owner();var fields=GameRunningSoundState.ForOwner(owner);fields.Sound2110.Write(null);var c=new GameRunningSoundInvocation();
   Missing(0x05cdd4,delegate{GameRunningSoundContracts.PlayLoopingRunningSound05CDC0(owner,c);},"Opaque primary MOV does not consume Unknown");Check(c.PrimaryCaptured&&!c.FallbackCaptured&&!c.IsPlayingReached&&!c.Returned,"Only first MOV completed");Unknown(c.PrimaryCapture,"Unknown capture retained");Unknown(c.FallbackCapture,"Fallback not read");Same(null,fields.Sound2110.Read(0),"Lower known-null did not override primary Unknown");
  });
  Test("Known-null primary exposes Unknown fallback only at second TEST",delegate{
   var owner=Owner();var fields=GameRunningSoundState.ForOwner(owner);fields.Sound2210.Write(null);var c=new GameRunningSoundInvocation();
   Missing(0x05cde0,delegate{GameRunningSoundContracts.PlayLoopingRunningSound05CDC0(owner,c);},"Fallback consumed at its own TEST");Check(c.PrimaryCaptured&&c.FallbackCaptured&&!c.IsPlayingReached&&!c.Returned,"Both MOVs completed without fake sound result");Same(null,c.PrimaryCapture.Read(0),"Known primary null retained");Unknown(c.FallbackCapture,"Unknown fallback retained");
  });
  Test("Nonnull primary wins without reading lower pointer or inventing IsPlaying",delegate{
   var owner=Owner();var fields=GameRunningSoundState.ForOwner(owner);var primary=new object();fields.Sound2210.Write(primary);var c=new GameRunningSoundInvocation();
   Missing(0x05cde8,delegate{GameRunningSoundContracts.PlayLoopingRunningSound05CDC0(owner,c);},"Stop at actual IsPlaying call");Check(c.PrimaryCaptured&&!c.FallbackCaptured&&c.IsPlayingReached&&!c.Returned,"Primary selected before unclosed call");Same(primary,c.Selected.Read(0),"Original pointer passed to call boundary");Unknown(fields.Sound2110,"Lower field not defaulted");Unknown(c.FallbackCapture,"Lower MOV not executed");
  });
  Test("Nonnull fallback preserves its identity at actual IsPlaying boundary",delegate{
   var owner=Owner();var fields=GameRunningSoundState.ForOwner(owner);var fallback=new object();fields.Sound2210.Write(null);fields.Sound2110.Write(fallback);var c=new GameRunningSoundInvocation();
   Missing(0x05cde8,delegate{GameRunningSoundContracts.PlayLoopingRunningSound05CDC0(owner,c);},"No nonnull fallback success");Check(c.PrimaryCaptured&&c.FallbackCaptured&&c.IsPlayingReached&&!c.Returned,"Ordered primary and fallback prefix");Same(fallback,c.Selected.Read(0),"Same fallback pointer");Same(fallback,fields.Sound2110.Read(0),"Native prefix wrote no pointer field");
  });
  Test("Double-null returns at real RET and subsequent calls reread live fields",delegate{
   var owner=Owner();var fields=GameRunningSoundState.ForOwner(owner);fields.Sound2210.Write(null);fields.Sound2110.Write(null);var c=new GameRunningSoundInvocation();GameRunningSoundContracts.PlayLoopingRunningSound05CDC0(owner,c);
   Check(c.Returned&&c.PrimaryCaptured&&c.FallbackCaptured&&!c.IsPlayingReached,"Both null tests reach normal void return");Eq(0x05ce41u,c.Native.At,"Actual RET");Same(owner,c.Owner,"Original owner");Same(null,c.Selected.Read(0),"Double-null path selects no SoundPak");
   var later=new object();fields.Sound2210.Write(later);var next=new GameRunningSoundInvocation();Missing(0x05cde8,delegate{GameRunningSoundContracts.PlayLoopingRunningSound05CDC0(owner,next);},"Fresh call sees new pointer");Same(later,next.Selected.Read(0),"No stale null result");Same(null,c.PrimaryCapture.Read(0),"Earlier invocation remains captured, not a mutable alias");
  });
  Test("Explicit composition shares canonical owner and rejects foreign or rebound bridges",delegate{
   var r=new PlayerMoveRequestTests.Fixture();r.Movement.CharacterClass.Write(r.Callbacks.CharacterClass.Read(0xa5cba0));var production=new GameControllerMoveProductionContext(r.Lookup,new GameReachedGoalContext(r.Bridge,r.ManagerEngine,r.Movement));var sound=new GameRunningSoundContext(r.Bridge);var fields=GameRunningSoundState.ForOwner(r.Owner);
   Same(fields,GameRunningSoundState.ForOwner(r.Bridge.ForMoveCharacter(r.Move,0).Owner),"One shared sidecar through original bridge");Same(null,production.RunningSound,"Unbound production remains explicit");production.BindRunningSound(sound);production.BindRunningSound(sound);Same(sound,production.RunningSound,"Same context binding is idempotent");
   Argument(delegate{production.BindRunningSound(new GameRunningSoundContext(new GameMovementIdentityBridge(r.Callbacks)));},"Foreign bridge rejected before publication");Invalid(delegate{production.BindRunningSound(new GameRunningSoundContext(r.Bridge));},"Rebound context rejected");Same(sound,production.RunningSound,"Rejected binds retain original context");Unknown(fields.Sound2210,"Binding initializes no primary pointer");Unknown(fields.Sound2110,"Binding initializes no fallback pointer");
  });
  Test("Actual Ground request completes Begin through explicit double-null sound path",delegate{
   int before=FixtureChecks();var scene=new NavigationSearchWrapperTests.Scene();var f=new GameMoveBeginResourceIntegrationTests.Fixture(scene,false,delegate(PlayerMoveRequestTests.Fixture r){GameMoveBeginGroundIntegrationTests.InitializePositions(r,scene);});PriorFixtureSetupChecks+=4+FixtureChecks()-before;f.Ready();var request=f.Request;
   var sound=new GameRunningSoundContext(request.Bridge);f.Movement.Composition.Production.BindRunningSound(sound);var fields=GameRunningSoundState.ForOwner(request.Owner);fields.Sound2210.Write(null);fields.Sound2110.Write(null);
   GroundOuterResult=request.Outer(f.Outer);GroundOuterReturned=true;Eq((byte)1,GroundOuterResult,"Existing complete mouse-request path returns1");var call=request.Boundaries.LastRequest;var production=f.Movement.Composition.Entry.MoveProductionCall;var handle=f.Handle;GroundBeginReturned=handle.BeginReturned;GroundRequestReturned=call.Returned;GroundPathResult=call.Path.Result;
   Eq(0u,GroundPathResult,"Actual saved Ground precheck succeeds");Check(call.Path.Precheck.Navigation.FirstNearestReturned&&call.Path.Precheck.Navigation.SecondNearestReturned&&call.Path.Precheck.Navigation.CorridorReturned,"Actual map queries completed");Check(handle.BeginReturned&&handle.Returned&&production.Returned&&f.Movement.Composition.Entry.Returned&&call.TransitionReturned&&call.Returned,"Normal sound return reaches every existing caller");Eq(1,handle.RunningSoundCalls.Count,"One actual RunningSound dispatch");var native=handle.RunningSoundCalls[0];GroundSoundReturnRva=native.Native.At;Eq(0x05ce41u,GroundSoundReturnRva,"Real double-null RET");Check(native.Returned&&!native.IsPlayingReached,"No audio response mocked");Same(request.Owner,native.Owner,"Original Character owns both pointers");
   Check(f.Play.Returned&&f.Play.SyncedReturned,"Actual resource/Player tail returned before sound");Same(f.Add.Load.Construction.Result,f.Play.ResourceCapture.Read(0),"Factory object flows through Player");Same(f.Resource,f.Channel.Resource10.Read(0),"Same resource published to mesh");Eq(4u,f.Resource.Dword74.Read(0),"No resource decoding invented");Eq((byte)1,request.MouseController.Byte432.Read(0),"Outer publishes actual processed-request result");Eq((byte)0,f.Movement.Composition.Handler.Busy18.Read(0),"Executor clears busy only after Begin returned");Same(production.AllocatedAction,f.Movement.Composition.Handler.Current08.Read(0),"Original action retained");Eq((byte)1,request.Manager.Moving38.Read(0),"Accepted movement request remains active");
   Eq(0x15e365u,f.Outer.At,"Outer reached result publication");Eq(0x1639b0u,call.Native.At,"Request reached actual common return");Eq(0x1636bau,call.DecisionAt,"Actual SSO success branch");Eq((byte)1,f.Movement.Composition.State.Changed2D0.Read(0),"Existing Idle exit selected restoration value1");Eq(0ul,f.Movement.Composition.Handler.Count28.Read(0),"Original empty queue remains empty");Same(null,request.Controller.CurrentA0.Read(0),"No new controller-current action fabricated");Same(null,request.Controller.PendingA8.Read(0),"No pending action fabricated");
   Eq(1,handle.WaypointCalls.Count,"One real waypoint request");var submitted=handle.WaypointCalls[0].MoveObjectCalls[0];Check(submitted.Published&&submitted.Returned,"Actual Agent target publication");Same(f.Movement.Agent,submitted.Agent.Read(0),"Original Agent, no alternate mover");var position=request.Owner.Entity.CoordsD8.Position00;Eq(f.Movement.InitialX,position.D8.Read(0),"Entity X has not advanced");Eq(f.Movement.InitialY,position.DC.Read(0),"Entity keeps computed Ground Y");Eq(f.Movement.InitialZ,position.D10.Read(0),"Entity Z has not advanced");Eq(f.Movement.InitialY,NavigationCoreContracts.ToBits(f.Movement.Agent.Position48.Y.Read(0)),"Agent retains same Ground start");Eq(0u,f.Movement.Agent.RequestE8.Generation00.Read(0),"No frame/replan execution claimed");Eq((byte)0,f.Movement.Agent.RequestE8.Complete50.Read(0),"No arrival claimed");Eq(0,request.Lock.OwnOutstandingEnters,"Object locks released");Check(!scene.Lock.IsHeldByCurrentThread,"Ground lock released");
  });
 }
}
