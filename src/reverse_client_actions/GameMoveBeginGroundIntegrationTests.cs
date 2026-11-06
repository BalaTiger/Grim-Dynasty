using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// One saved Ground candidate through the existing request/Begin/resource chain.
// X/Z and a probe height are OWN inputs; both final heights are computed by the
// actual saved Ground path before Agent Initialize/Created. No navigation Y is
// substituted, and no input event, sound result or continuous frame is supplied.
public static class GameMoveBeginGroundIntegrationTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}public static int Assertions{get{return assertions;}}
 public static int PriorFixtureSetupChecks,PositionInitializations;
 public static uint StopRva,PathResult,CorridorCount;
 public static float StartHeight,DestinationHeight;
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract e){Eq(at,e.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static int FixtureChecks(){return GameWaypointProductionIntegrationTests.Assertions+GameControllerMoveProductionIntegrationTests.Assertions+PlayerMoveRequestTests.Assertions+GameMoveBeginResourceIntegrationTests.Assertions;}
 static void GroundPosition(WorldPosition24 position,float x,float z,PlayerMoveRequestTests.Fixture request,NavigationSearchWrapperTests.Scene scene){
  position.Q0.Write(77);position.D8.Write(NavigationCoreContracts.ToBits(x));position.D10.Write(NavigationCoreContracts.ToBits(z));
  // 29.5 is a ray probe input, never the returned terrain height.
  position.DC.Write(NavigationCoreContracts.ToBits(29.5f));
  Eq((byte)1,WorldFloorContracts.PutOnFloor22C620(position,request.World,scene.Floor,new Invocation()),"Saved Ground supplies height");
  Eq(NavigationCoreContracts.ToBits(x),position.D8.Read(0),"Ground preserves requested X");
  Eq(NavigationCoreContracts.ToBits(z),position.D10.Read(0),"Ground preserves requested Z");
 }
 public static void InitializePositions(PlayerMoveRequestTests.Fixture request,NavigationSearchWrapperTests.Scene scene){
  PositionInitializations++;Same(request.Position,request.Input.Position.Read(0),"Input retains original destination object");
  GroundPosition(request.Owner.Entity.CoordsD8.Position00,62.25f,62.75f,request,scene);
  StartHeight=NavigationCoreContracts.FromBits(request.Owner.Entity.CoordsD8.Position00.DC.Read(0));
  GroundPosition(request.Position,64.25f,64.75f,request,scene);
  DestinationHeight=NavigationCoreContracts.FromBits(request.Position.DC.Read(0));
  // Historical values are checks only; they never seed the height fields.
  Check(Math.Abs(StartHeight-16.5022278f)<.0002f,"Computed start matches saved Ground candidate");
  Check(Math.Abs(DestinationHeight-16.0587463f)<.0002f,"Computed destination matches saved Ground candidate");
 }
 static void InitialPosition(GameMoveBeginResourceIntegrationTests.Fixture fixture){
  var f=fixture.Movement;var position=fixture.Request.Owner.Entity.CoordsD8.Position00;
  Eq(f.InitialX,position.D8.Read(0),"Entity X remains original Ground start");Eq(f.InitialY,position.DC.Read(0),"Entity Y remains original Ground start");Eq(f.InitialZ,position.D10.Read(0),"Entity Z remains original Ground start");
  Eq(f.InitialX,NavigationCoreContracts.ToBits(f.Agent.Position48.X.Read(0)),"Agent X was initialized from same Entity");Eq(f.InitialY,NavigationCoreContracts.ToBits(f.Agent.Position48.Y.Read(0)),"Agent Y was initialized from same Ground height");Eq(f.InitialZ,NavigationCoreContracts.ToBits(f.Agent.Position48.Z.Read(0)),"Agent Z was initialized from same Entity");
 }
 public static void Run(){
  scenarios=assertions=PriorFixtureSetupChecks=PositionInitializations=0;StopRva=PathResult=CorridorCount=0;StartHeight=DestinationHeight=0;
  int before=FixtureChecks();var scene=new NavigationSearchWrapperTests.Scene();
  var f=new GameMoveBeginResourceIntegrationTests.Fixture(scene,false,delegate(PlayerMoveRequestTests.Fixture request){InitializePositions(request,scene);});
  // Scene has four fixed constructor checks and no public assertion counter.
  PriorFixtureSetupChecks=4+FixtureChecks()-before;f.Ready();var r=f.Request;
  Eq(1,PositionInitializations,"OWN positions initialized once before Agent creation");InitialPosition(f);
  Same(scene.Regions,r.Regions,"Request shares saved scene Regions");Same(scene.Floor,r.Request.Path.Navigation.Floor,"Precheck shares same Ground context");Same(scene.Query,f.Movement.Query.Query10.Read(0),"Agent and path share saved query");
  Same(r.Position,r.Input.Position.Read(0),"Later fixture setup did not replace input destination");Eq(NavigationCoreContracts.ToBits(StartHeight),f.Movement.InitialY,"Initial snapshot taken after Ground setup");Eq(NavigationCoreContracts.ToBits(DestinationHeight),r.Position.DC.Read(0),"Later setup did not overwrite computed destination");
  Check(f.Add.Returned&&f.Add.ResourcePublished&&f.Add.Load.Cold&&f.Add.Load.FactoryReturned,"Actual factory/Add completed on same composition");Same(f.Add.Load.Construction.Result,f.Resource,"Entry retains actual factory resource");Same(r.Movement.EngineGlobal,f.ResourceManager.EngineGlobal,"No second Engine global");Same(r.Lookup,f.Animation.Lookup,"No second object lookup");
  Missing(0x05cdc0,delegate{f.Run();},"Actual Ground request reaches unresolved RunningSound");StopRva=0x05cdc0;
  var requestCall=r.Boundaries.LastRequest;var navigation=requestCall.Path.Precheck.Navigation;var handle=f.Handle;var production=f.Movement.Composition.Entry.MoveProductionCall;
  PathResult=requestCall.Path.Result;CorridorCount=navigation.CorridorCount.Read(0);Eq(0u,PathResult,"Real Ground-to-Ground precheck accepts this candidate");Check(requestCall.PathReturned&&requestCall.TransitionEntered&&!requestCall.Returned,"Request entered original state transition but has not returned");
  Check(navigation.FirstNearestReturned&&navigation.SecondNearestReturned&&navigation.CorridorReturned&&navigation.StraightReturned&&navigation.FloorReturned,"Real nearest/search/straight/Ground stages returned");Check(navigation.StartReference.Read(0)!=0&&navigation.EndReference.Read(0)!=0&&CorridorCount>0,"Saved polygons supply actual route");Eq(StartHeight,navigation.FromWorld.Y.Read(0),"Search consumes actual Ground start");Eq(DestinationHeight,navigation.ToWorld.Y.Read(0),"Search consumes actual Ground destination");
  Eq(1,handle.WaypointCalls.Count,"One original waypoint call");var waypoint=handle.WaypointCalls[0];Check(waypoint.Returned&&waypoint.Result==1,"Actual waypoint accepted");Eq(1,waypoint.MoveObjectCalls.Count,"One actual Agent submission");var submission=waypoint.MoveObjectCalls[0];Check(submission.LookupReturned&&submission.ProjectionReturned&&submission.Published&&submission.Returned,"Real registry/project/target publication completed");Same(f.Movement.Agent,submission.Agent.Read(0),"Original Agent selected");Same(r.Owner.Interface38,f.Movement.Agent.Callback08.Read(0),"Original Entity interface retained");
  var play=f.Play;Check(play.Returned&&play.ReturnValue==1&&play.HandStateReturned&&play.SyncedReturned,"Recovered Player/entry path returned normally");Same(f.Resource,play.ResourceCapture.Read(0),"Factory resource reaches Player");Same(f.Resource,f.Channel.Resource10.Read(0),"Factory resource reaches same mesh channel");Same(f.Actor,play.SyncedCall.Actor,"Same Actor reaches attached-list tail");Same(r.Objects,play.HandCall.LeftManager,"Original map used for left lookup");Same(r.Objects,play.HandCall.RightManager,"Original map used for independent right lookup");Eq(4,play.HandCall.LeftLookup.HashedBytes,"Left ID0 actually hashed");Eq(4,play.HandCall.RightLookup.HashedBytes,"Right ID0 actually hashed");Eq(5u,f.Set.Kind08.Read(0),"Actual Play publishes Kind5");Eq(0u,f.SetFields.Pending0C.Read(0),"Actual Play clears Pending");Eq(4u,f.Resource.Dword74.Read(0),"Resource remains constructed, not decoded");
  Check(handle.BeginEntered&&!handle.BeginReturned&&!handle.Returned&&!production.Returned&&!f.Movement.Composition.Entry.Returned,"Sound boundary prevents completed Begin");Eq(StopRva,handle.BeginNative.At,"Exact Begin stop");Eq(StopRva,production.Native.At,"Exact production stop");Same(production.AllocatedAction,f.Movement.Composition.Handler.Current08.Read(0),"Same action remains published");Eq((byte)1,f.Movement.Composition.Handler.Busy18.Read(0),"Busy prefix retained");Eq((byte)1,r.Manager.Moving38.Read(0),"Actual movement request retained");
  InitialPosition(f);Eq(2u,f.Movement.Agent.State70.Read(0),"Agent holds target request, not a completed frame");Eq(0u,f.Movement.Agent.RequestE8.Generation00.Read(0),"No frame update fabricated");Eq((byte)0,f.Movement.Agent.RequestE8.Complete50.Read(0),"No completed movement fabricated");
  Eq(0,r.Lock.OwnOutstandingEnters,"Original object locks released");Eq(0,f.Movement.Query.Critical138.Read(0).OwnOutstandingEnters,"Projection lock released");Check(!scene.Lock.IsHeldByCurrentThread,"Ground lock released");Check(!f.ResourceManager.SynchronizationC0.Read(0).IsHeldByCurrentThread,"Resource lock released");scenarios++;
 }
}
