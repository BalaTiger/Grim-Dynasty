using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Prepared integration tests, dependent on the TWO inactive animation patches.
// Existing fixtures are reused, not their suite Run methods. Animation/mesh
// pointers, fields and NoName used below are explicit OWN observations: they
// establish no native resource loader, constructor, Play success or live game.
public static class GameMoveAnimationProductionTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}public static int Assertions{get{return assertions;}}
 public static int PriorFixtureSetupChecks;
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract e){Eq(at,e.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Reject(Action body,string why){try{body();}catch(ArgumentException){Check(true,why);return;}throw new Exception("Expected OWN admission rejection: "+why);}
 static void Unknown<T>(Field<T> field,string why){Missing(0,delegate{field.Read(0);},why);}
 static void Test(string name,Action body){try{body();scenarios++;}catch(Exception e){throw new Exception(name+": "+e.Message,e);}}
 sealed class AnimationFixture {
  public readonly PlayerMoveRequestTests.Fixture Request;
  public readonly ActorBoundingBoxContext Bounds;
  public readonly ActorBoundingBoxState Actor;
  public readonly BorrowedMeshBounds Mesh;
  public readonly EngineAnimationSpeedContext Speed;
  public readonly EngineMeshAnimationSpeedFields SpeedFields;
  public readonly GameMoveAnimationProductionContext Context;
  public readonly GameCharacterAnimationFields Sets;
  public readonly GameMoveAnimationProductionInvocation Call=new GameMoveAnimationProductionInvocation();
  public AnimationFixture(PlayerMoveRequestTests.Fixture request){
   Request=request;Bounds=new ActorBoundingBoxContext(request.Callbacks.Entities);Actor=Bounds.Bind(request.Owner.Entity);Mesh=Bounds.BindMesh(new object());
   Speed=new EngineAnimationSpeedContext(Bounds,Actor);SpeedFields=Speed.BindExistingMesh(Mesh);Context=new GameMoveAnimationProductionContext(request.Bridge,request.Move,Speed);Sets=GameCharacterAnimationFields.ForOwner(request.Owner);
   // Merely constructing the OWN fixture leaves all mesh/set/speed fields
   // Unknown. Production receives these same objects and never replaces them.
  }
  public void ObserveFirstSetAndMesh(){var set=new AnimationSet();set.Kind08.Write(0);Sets.Set2600.Write(set);Actor.Mesh2668.Write(Mesh);}
 }
 static GameWaypointProductionIntegrationTests.Fixture Hot(NavigationSearchWrapperTests.Scene scene){
  int before=GameWaypointProductionIntegrationTests.Assertions+GameControllerMoveProductionIntegrationTests.Assertions+PlayerMoveRequestTests.Assertions;
  var f=new GameWaypointProductionIntegrationTests.Fixture(scene,true);
  PriorFixtureSetupChecks+=GameWaypointProductionIntegrationTests.Assertions+GameControllerMoveProductionIntegrationTests.Assertions+PlayerMoveRequestTests.Assertions-before;
  var c=f.Composition;var r=f.Request;c.Production.BindBeginPath(r.Request.Path);c.Production.BindWaypoints(f.Context);
  // Same explicit OWN hot observations as the already passed Begin fixture.
  r.Move.Gate26AF.Write(0);r.Move.AssociatedId16C0.Write(0);r.Move.CurrentSkillA90.Write(0);r.Engine.World50.Write(r.World);r.Movement.AlreadyThereConstant7A7FD8.Write(.025f);c.Skills.Offset3BC.Write(0);c.Skills.Offset3C0.Write(0);c.Skills.Offset3C4.Write(0);c.Skills.Offset3C8.Write(0);
  return f;
 }
 static GameControllerHandleActionInvocation Prefix(GameWaypointProductionIntegrationTests.Fixture f,uint at){
  var entry=f.Composition.Entry;var production=entry.MoveProductionCall;var handle=production.Handle;
  Check(f.Request.Boundaries.LastRequest.Path.Precheck.Navigation.CorridorCount.Read(0)>1,"Existing actual search used the saved map across polygons");
  Eq(1,handle.WaypointCalls.Count,"One actual waypoint request");Check(handle.WaypointCalls[0].Returned&&handle.WaypointCalls[0].Result==1,"Real waypoint body completed before animation");
  var submitted=handle.WaypointCalls[0].MoveObjectCalls[0];Check(submitted.Published&&submitted.Returned,"Actual Agent target publication retained");Same(f.Agent,submitted.Agent.Read(0),"Same Agent receives target");
  Check(handle.BeginEntered&&!handle.BeginReturned&&!production.Returned&&!entry.Returned,"Incomplete Begin is not reported as game success");Eq(at,handle.BeginNative.At,"Exact nested Begin failure");Eq(at,production.Native.At,"Production failure propagation");
  Same(production.AllocatedAction,f.Composition.Handler.Current08.Read(0),"Original action stays published");Eq((byte)1,f.Composition.Handler.Busy18.Read(0),"Failure keeps original busy prefix");Eq((byte)1,f.Request.Manager.Moving38.Read(0),"Accepted waypoint moving prefix retained");
  foreach(var owner in handle.Animation.Owners)Same(f.Request.Owner,owner,"Every animation call resolves same Bridge owner");
  Eq(0,f.Request.Lock.OwnOutstandingEnters,"Object lookup lock released");Check(!f.Scene.Lock.IsHeldByCurrentThread,"Ground lock released");return handle;
 }
 public static void Run(){
  scenarios=assertions=PriorFixtureSetupChecks=0;
  Test("Optional composition borrows original owner and real getters",delegate{
   var request=new PlayerMoveRequestTests.Fixture();var f=new AnimationFixture(request);Same(request.Owner,f.Context.View.Owner,"Same canonical full owner");Same(request.Move,f.Context.View.Move,"Same existing Move view");Same(f.Actor,f.Context.Speed.Actor,"Same original Actor sidecar");Same(f.Bounds,f.Context.Speed.Bounds,"Same supplied Bounds context");
   Unknown(f.Sets.Set2578,"Binding did not initialize fallback");Unknown(f.Sets.Set2600,"Binding did not initialize override");Unknown(f.SpeedFields.Speed130,"Binding did not invent speed");
   f.Sets.Set2600.Write(null);f.Sets.Set2608.Write(null);var result=GameMoveAnimationProductionContracts.GetAnimations(f.Context,request.Move,0x05f9fb,f.Call);Check(f.Call.SetCalls[0].Returned&&f.Call.SetCalls[0].FallbackCaptured,"Actual05AE40 returned unknown fallback");Missing(0x05fa05,delegate{result.Read(0x05fa05);},"Pointer remains opaque until caller use");
   f.ObserveFirstSetAndMesh();f.SpeedFields.Speed130.Write(2.25f);var speed=GameMoveAnimationProductionContracts.GetSpeed(f.Context,request.Move,0,0x05fa08,f.Call);Eq(2.25f,speed.Read(0),"Actual027410 and06DA30 return declared mesh field");Same(f.Mesh,f.Call.SpeedCalls[0].ReturnedMesh,"No replacement mesh");Same(request.Owner,f.Call.Owners[0],"Getter original owner");Same(request.Owner,f.Call.Owners[1],"Speed original owner");
   var otherMove=new MoveCharacter();var other=new GameMovementCharacterState(new EntityMovementState(otherMove));request.Callbacks.Attach(other);request.Bridge.Bind(other,otherMove);Reject(delegate{new GameMoveAnimationProductionContext(request.Bridge,otherMove,f.Speed);},"Different Entity cannot share this Actor composition");Missing(0x027416,delegate{GameMoveAnimationProductionContracts.GetSpeed(f.Context,otherMove,0,0x05fa08,f.Call);},"Runtime also rejects foreign original owner");
  });
  Test("State slots use current primary and the original24B4 and20A8 cells",delegate{
   var request=new PlayerMoveRequestTests.Fixture();var f=new AnimationFixture(request);var tail=GameReachedGoalCharacterFields.ForOwner(request.Owner);request.Owner.Entity.Virtual00.Write(GameMovementPrimaryTable.Player);request.Owner.ActionState24B4.Write(3);tail.Dword20A8.Write(0x1234);
   Eq(3u,GameMoveAnimationProductionContracts.GetState458(f.Context,request.Move,0x05fa17,f.Call),"Actual getter reads live24B4");request.Owner.ActionState24B4.Write(6);request.Owner.Entity.Virtual00.Write(GameMovementPrimaryTable.Character);Eq(6u,GameMoveAnimationProductionContracts.GetState458(f.Context,request.Move,0x082ddf,f.Call),"Later call reads changed field and changed observed primary");Same(GameMovementPrimaryTable.Character,f.Call.StateCalls[1].CapturedPrimary.Read(0),"Fresh table captured");
   GameMoveAnimationProductionContracts.SetState450(f.Context,request.Move,5,0x05fa3f,f.Call);Eq(5u,request.Owner.ActionState24B4.Read(0),"Original24B4 updated by actual05B710");Eq(0u,tail.Dword20A8.Read(0),"Actual following20A8 clear is retained");Eq(0x05b720u,f.Call.StateCalls[2].Native.At,"Both native stores reached return");Same(request.Owner,f.Call.StateCalls[2].Owner,"Setter has same owner");Check(f.Call.StateCalls[2].Dispatched&&f.Call.StateCalls[2].Returned,"Real setter completed");
   tail.Dword20A8.Write(99);request.Owner.Entity.Virtual00.Write(new object());Missing(0x05fa90,delegate{GameMoveAnimationProductionContracts.SetState450(f.Context,request.Move,1,0x05fa90,f.Call);},"Unknown current setter table cannot reuse last observed table");Eq(5u,request.Owner.ActionState24B4.Read(0),"Failed virtual dispatch performs no24B4 write");Eq(99u,tail.Dword20A8.Read(0),"Failed virtual dispatch performs no20A8 write");Check(!f.Call.StateCalls[3].Dispatched&&!f.Call.StateCalls[3].Returned,"No false setter completion");
   request.Owner.Entity.Virtual00.Forget();Missing(0x05fa17,delegate{GameMoveAnimationProductionContracts.GetState458(f.Context,request.Move,0x05fa17,f.Call);},"Unknown primary consumed at actual virtual call");request.Owner.Entity.Virtual00.Write(GameMovementPrimaryTable.Player);request.Owner.ActionState24B4.Forget();Missing(0x05b730,delegate{GameMoveAnimationProductionContracts.GetState458(f.Context,request.Move,0x05fa17,f.Call);},"Known table enters real getter before unknown state stop");Check(f.Call.StateCalls[5].Dispatched&&!f.Call.StateCalls[5].Returned,"Getter failure retained after dispatch");
  });
  var scene=new NavigationSearchWrapperTests.Scene();PriorFixtureSetupChecks+=4; // Existing saved-member constructor checks; excluded from new assertions.
  Test("Connected saved-map Begin preserves unknown first animation pointer",delegate{
   var f=Hot(scene);var a=new AnimationFixture(f.Request);a.Sets.Set2600.Write(null);a.Sets.Set2608.Write(null);f.Composition.Production.BindAnimations(a.Context);Same(a.Context,f.Composition.Production.AnimationProduction,"Explicit opt-in retained");f.Composition.Production.BindAnimations(a.Context);
   Missing(0x05fa05,delegate{f.Request.Outer(new Invocation());},"Actual first getter returns before caller consumes unknown fallback");var h=Prefix(f,0x05fa05);Eq(1,h.Animation.SetCalls.Count,"One actual getter");Check(h.Animation.SetCalls[0].Returned&&h.Animation.SetCalls[0].FallbackCaptured,"Unknown fallback reached actual getter RET");Eq(0,h.Animation.SpeedCalls.Count,"No speed call before failed pointer dereference");Eq(0,h.Animation.StateCalls.Count,"No state call before failed pointer dereference");Unknown(a.Sets.Set2578,"No fallback manufactured");
  });
  Test("Connected Begin skips unused Unknown speed then preserves name failure after real state writes",delegate{
   var f=Hot(scene);var a=new AnimationFixture(f.Request);a.ObserveFirstSetAndMesh();f.Composition.Production.BindAnimations(a.Context);Eq(0u,f.Request.Owner.ActionState24B4.Read(0),"Explicit OWN cold state from existing fixture");
   Missing(0x05fa4d,delegate{f.Request.Outer(new Invocation());},"Unknown old speed is not consumed when actual state is not5");var h=Prefix(f,0x05fa4d);Eq(2,h.Animation.SetCalls.Count,"Both actual getters returned before NoName read");Eq(1,h.Animation.SpeedCalls.Count,"One actual Engine speed call");Check(h.Animation.SpeedCalls[0].Returned&&h.Animation.SpeedCalls[0].SpeedCaptured,"Unknown MOVSS really returned");Unknown(h.Animation.SpeedCalls[0].Result,"No speed default");Eq(2,h.Animation.StateCalls.Count,"Real state getter and setter ran");Eq(0u,h.Animation.StateCalls[0].Value.Value,"Getter saw original0 state");Eq(5u,f.Request.Owner.ActionState24B4.Read(0),"Actual setter changed original state to5 before name stop");Eq(0u,GameReachedGoalCharacterFields.ForOwner(f.Request.Owner).Dword20A8.Read(0),"Actual second setter store cleared20A8");Eq(0x05b720u,h.Animation.StateCalls[1].Native.At,"Setter reached return");Eq(5,h.Animation.CallerSites.Count,"Exactly five getter/speed/state calls");Eq(0x05fa48u,h.Animation.CallerSites[4],"Second animation pointer precedes NoName");Unknown(f.Request.Movement.NoNameImport609568,"No guessed name singleton");
  });
  PlaybackBoundaryScenario(scene);
 }
 public static void RunPlaybackBoundaryRegression(){
  scenarios=assertions=PriorFixtureSetupChecks=0;
  var scene=new NavigationSearchWrapperTests.Scene();PriorFixtureSetupChecks+=4;
  PlaybackBoundaryScenario(scene);
 }
 static void PlaybackBoundaryScenario(NavigationSearchWrapperTests.Scene scene){
  Test("Connected Begin preserves the name boundary and the actual unresolved Play entry prefix",delegate{
   var unnamed=Hot(scene);var unnamedAnimation=new AnimationFixture(unnamed.Request);unnamedAnimation.ObserveFirstSetAndMesh();unnamed.Composition.Production.BindAnimations(unnamedAnimation.Context);
   Missing(0x05fa4d,delegate{unnamed.Request.Outer(new Invocation());},"Unknown NoName still stops before Play");var beforePlay=Prefix(unnamed,0x05fa4d);Eq(0,beforePlay.Animation.PlayCalls.Count,"No Play invocation before the name read succeeds");Eq(5u,unnamed.Request.Owner.ActionState24B4.Read(0),"Name failure retains the real state5 write");Check(!beforePlay.StopEntered,"Name failure does not stop the manager");
   // A new original-owner fixture avoids replaying Begin against the prior
   // failure's already-published state5 with an unknown old animation speed.
   var f=Hot(scene);var a=new AnimationFixture(f.Request);a.ObserveFirstSetAndMesh();var set=a.Sets.Set2600.Read(0);var fields=GameAnimationSetFields.ForSet(set);Unknown(fields.Entry(5),"Fixture leaves Entry5 uninitialized");Unknown(fields.Pending0C,"Fixture does not invent pending animation");f.Request.Movement.NoNameImport609568.Write(new object());f.Composition.Production.BindAnimations(a.Context);
   Missing(0x024e0d,delegate{f.Request.Outer(new Invocation());},"Actual Play writes pending5 then consumes the unknown Entry5");var h=Prefix(f,0x024e0d);Eq(2,h.Animation.SetCalls.Count,"Real first and second sets captured");Eq(1,h.Animation.SpeedCalls.Count,"Actual speed returned");Eq(2,h.Animation.StateCalls.Count,"No post-failed-Play state restoration fabricated");Eq(1,h.Animation.PlayCalls.Count,"One actual Play prefix captured");var play=h.Animation.PlayCalls[0];Same(set,play.RequestedSet,"Play uses the original AnimationSet identity");Eq(5u,play.RequestedAnimation,"Move requested animation5");Eq(5,play.SelectedIndex,"Entry5 selected without a default entry");Eq(0x024e0du,play.Native.At,"Play diagnostics retain the entry consumption stop");Check(play.Used&&play.PendingWritten&&!play.EntryEntered&&!play.Returned,"Pending store is retained before entry dispatch and return");Check(!play.ChannelEntered&&!play.AnimationSubmitted,"No channel or playback success is manufactured");Eq(5u,fields.Pending0C.Read(0),"Pending0C remains5 after the failed entry read");Eq(0u,set.Kind08.Read(0),"Kind08 remains0 without successful Play");Unknown(fields.Entry(5),"Failure does not fill the unknown Entry5");Eq(5u,f.Request.Owner.ActionState24B4.Read(0),"Character24B4 remains5 without state1 restoration");Check(!h.StopEntered&&!h.BeginReturned,"Unsupported does not stop the manager or complete Begin");
  });
 }
}
