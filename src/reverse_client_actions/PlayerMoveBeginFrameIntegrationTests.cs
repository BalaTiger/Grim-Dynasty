using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// A single recovered request/Begin followed by two bounded production frames.
// Explicit OWN cold vector, empty backing, pose/mesh bounds and frame clock are
// prerequisites, not recovered native startup. No second Move151130/Created,
// Character substitute, supplied route, input event or callback result is used.
public static partial class PlayerMoveBeginFrameIntegrationTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}public static int Assertions{get{return assertions;}}
 public static int PositiveAssertions,NegativeAssertions,PriorFixtureSetupChecks,CompletedFrames;
 public static bool BeginReturned,OuterReturned,PassedTwoFrames,NetHorizontalProgress;
 public static byte OuterResult;
 public static uint UnsupportedRva,DefaultUnknownStopRva,IdentityUnknownStopRva;
 public static string Stage,FailureScenario;
 public static long NativeMathCalls;
 public static float[] RequestedGroundTarget,AgentTarget;
 public static double InitialHorizontalDistance,FinalHorizontalDistance;
 public sealed class FrameResult {
  public int Frame;public float[] NavigationPosition,PublishedPosition;
  public float GroundY;public double BeforeDistance,AfterDistance;
  public bool DistanceDecreased,GroundDiffersFromNavigation,EarlyMovedReturned,SynchronousReplan;
  public uint Generation,Flags,SuffixReference;public int CorridorCount;
 }
 public static readonly List<FrameResult> Frames=new List<FrameResult>();
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static uint Bits(float x){return NavigationCoreContracts.ToBits(x);}static float Float(uint x){return NavigationCoreContracts.FromBits(x);}
 static void Point(NavigationPoint3 p,float x,float y,float z){p.X.Write(x);p.Y.Write(y);p.Z.Write(z);}
 static float[] Values(NavigationPoint3 p){return new[]{p.X.Read(0),p.Y.Read(0),p.Z.Read(0)};}
 static float[] Values(WorldPosition24 p){return new[]{Float(p.D8.Read(0)),Float(p.DC.Read(0)),Float(p.D10.Read(0))};}
 static double Distance(float x,float z,float[] target){double dx=(double)x-target[0],dz=(double)z-target[2];return Math.Sqrt(dx*dx+dz*dz);}
 static int FixtureChecks(){return GameWaypointProductionIntegrationTests.Assertions+GameControllerMoveProductionIntegrationTests.Assertions+PlayerMoveRequestTests.Assertions+GameMoveBeginResourceIntegrationTests.Assertions+GameMoveBeginGroundIntegrationTests.Assertions;}
 static void Unknown<T>(Field<T> f,string why){try{f.Read(0);}catch(MissingContract){Check(true,why);return;}throw new Exception("Expected Unknown: "+why);}
 static uint Missing(uint expected,Action body){try{body();}catch(MissingContract e){if(e.Address!=expected){UnsupportedRva=e.Address;throw;}Eq(expected,e.Address,"Exact live Unknown consumer");return e.Address;}throw new Exception("Expected Unsupported "+expected.ToString("x"));}
 static int WorkCount(NavigationCrowdGroup g){var b=g.Work40.Begin00.Read(0) as NavigationCrowdPointerAddress<NavigationCrowdAgent>;var e=g.Work40.End08.Read(0) as NavigationCrowdPointerAddress<NavigationCrowdAgent>;if(b==null&&e==null)return 0;Check(b!=null&&e!=null&&Object.ReferenceEquals(b.Buffer,e.Buffer),"One actual Work allocation");return e.Index-b.Index;}
 static int RefCount(NavigationCrowdAgent a){var b=a.RequestE8.RefVector20.Begin00.Read(0);var e=a.RequestE8.RefVector20.End08.Read(0);if(b==null&&e==null)return 0;Check(b!=null&&e!=null&&Object.ReferenceEquals(b.Buffer,e.Buffer),"One actual reference allocation");return e.Index-b.Index;}
 sealed class Fixture {
  public readonly NavigationSearchWrapperTests.Scene Scene;
  public readonly GameMoveBeginResourceIntegrationTests.Fixture Move;
  public readonly PlayerFramePublicationFixture Publication;
  public readonly NavigationCrowdAvoidanceConstants Constants=new NavigationCrowdAvoidanceConstants();
  public readonly NavigationCrowdTickProductionBoundaries Tick;
  public readonly NavigationCrowdTickPrefixBoundaries Boundary;
  public readonly NavigationCrowdPointerDomain<NavigationCrowdAgent> Domain=new NavigationCrowdPointerDomain<NavigationCrowdAgent>();
  public readonly NavigationCrowdTickPrefixInvocation Prefix=new NavigationCrowdTickPrefixInvocation();
  public readonly GameMovedReachedTailInvocation Tail=new GameMovedReachedTailInvocation();
  public readonly NavigationCrowdRequest RequestIdentity;
  public readonly float[] Target;
  public readonly bool Cold,ActualRegistration;
  public readonly GameReachedGoalStopProduction StopProduction;
  public Fixture(SystemUcrtRotationMath math,bool cold):this(math,cold,false){}
  public Fixture(SystemUcrtRotationMath math,bool cold,bool actualRegistration){
   Cold=cold;ActualRegistration=actualRegistration;Stage="saved-scene-and-own-constructor-input";int before=FixtureChecks();Scene=new NavigationSearchWrapperTests.Scene();
   Constants.UseObservedDiskConstants();Point(Constants.Fallback422980,0,0,0);
   Action<PlayerMoveRequestTests.Fixture> positions=delegate(PlayerMoveRequestTests.Fixture positionOwner){GameMoveBeginGroundIntegrationTests.InitializePositions(positionOwner,Scene);};
   // The legacy overload must retain Unknown constructor inputs. Only the
   // positive/Engine-identity cases opt into this shared OWN zero vector.
   Move=actualRegistration?new GameMoveBeginResourceIntegrationTests.Fixture(Scene,false,positions,cold?Constants.Fallback422980:null,true):cold?new GameMoveBeginResourceIntegrationTests.Fixture(Scene,false,positions,Constants.Fallback422980):new GameMoveBeginResourceIntegrationTests.Fixture(Scene,false,positions);
   PriorFixtureSetupChecks+=4+FixtureChecks()-before;Move.Ready();var m=Move.Movement;var r=Move.Request;RequestIdentity=m.Agent.RequestE8;
   if(cold){Same(Constants.Fallback422980,m.ConstructorGlobal,"Ctor borrows same global as tick");Eq(0u,Bits(m.Agent.Vector60.X.Read(0)),"Ctor copied OWN X");Eq(0u,Bits(m.Agent.Vector60.Y.Read(0)),"Ctor copied OWN Y");Eq(0u,Bits(m.Agent.Vector60.Z.Read(0)),"Ctor copied OWN Z");}
   else {Check(!Object.ReferenceEquals(Constants.Fallback422980,m.ConstructorGlobal),"Legacy ctor keeps its separate Unknown input");Unknown(m.ConstructorGlobal.X,"Default global X");Unknown(m.Agent.Vector60.X,"No post-construction zero repair");}
   // Explicit capacity only, with zero entries. Native request generation,
   // corridor and waypoint production still run later in the same RequestE8.
   m.Crowd.CriticalA0.Write(new NavigationCrowdCriticalSection());m.Agent.RequestE8.InstallOwnRefStorage(new NavigationRefBuffer(1024),0);new NavigationCrowdWaypointVector(m.Agent.RequestE8.WaypointVector38).InstallOwnStorage(new NavigationPointBuffer(1024),0);
   var costs=new NavigationCorridorFilterCosts(m.Query.Filter18);for(int n=0;n<64;n++)costs.AreaCosts00[n].Write(1);Scene.Search.Attach(costs);
   var state=actualRegistration?m.RegistrationState:new NavigationCrowdStateBoundaries(m.Movement.Adapter,Constants.Fallback422980);state.UseObservedGroupConstants();state.DisableAllAssert42144A.Write(0);var proximity=new NavigationCrowdProximityBoundaries(state);proximity.DisableNoFreeAssert422BA0.Write(0);
   var path=m.Movement.Path;path.UseObservedPathConstants();path.UseObservedStraightPathConstants();path.UseObservedEqualityConstant();path.UseObservedDiskNoOp00CE70OwnAdapter();path.DisableAllAssert42144A.Write(0);path.DisableSecondAssert422B92.Write(0);path.DisableTrimAssert422B93.Write(0);path.DisablePopAssert422B94.Write(0);
   Tick=new NavigationCrowdTickProductionBoundaries(path,proximity,m.Movement.Adapter,Constants,math);Tick.DisableFirstAssert422B91.Write(0);Tick.DisableHeadingAssert422B9C.Write(0);
   var ray=new NavigationCrowdRaycastBoundaries(Scene.Mesh);ray.UseObservedRaycastConstants();Boundary=new NavigationCrowdTickPrefixBoundaries(Tick,ray);Boundary.UseObservedDiskConstants();Boundary.Global4228C0.X.Write(0);Boundary.Global4228C0.Y.Write(0);Boundary.Global4228C0.Z.Write(0);
   Publication=new PlayerFramePublicationFixture(Move,Scene,math);
   var sound=GameRunningSoundState.ForOwner(r.Owner);sound.Sound2210.Write(null);sound.Sound2110.Write(null);m.Composition.Production.BindRunningSound(new GameRunningSoundContext(r.Bridge));
   if(actualRegistration){var goal=m.Composition.Production.ReachedGoal;StopProduction=GameReachedGoalStopProduction.Bind(goal,m.Context);var hook=GameReachedGoalCharacterFields.ForOwner(r.Owner).HookPack45F0;goal.BindHookPack(r.Bridge.ForMoveCharacter(r.Move,0),hook);Unknown(hook.Initialized1C,"Binding does not initialize native HookPack");Same(m.RegistrationState.DisableAllAssert42144A,m.Movement.DisableAllAssert42144A,"Activation/Stop/State share same assertion cell");}
   Stage="one-original-mouse-request-and-Begin";byte result=r.Outer(Move.Outer);Eq((byte)1,result,"Actual request/Begin returns1");Check(Move.Handle.BeginReturned&&Move.Handle.Returned&&m.Composition.Entry.Returned&&r.Boundaries.LastRequest.Returned,"Every existing caller returned");Eq(1,Move.Handle.WaypointCalls.Count,"Exactly one MoveBegin waypoint request");Eq(1,Move.Handle.WaypointCalls[0].MoveObjectCalls.Count,"Exactly one Move151130 from Begin");Same(m.Agent,Move.Handle.WaypointCalls[0].MoveObjectCalls[0].Agent.Read(0),"Begin selected original Agent");if(actualRegistration){Check(m.ActivationCall.Registration.CreatedReturned,"Original actual Add performed Created");Eq(0u,m.CreatedCall.At,"Legacy direct Created remains unused");}else Eq(0x25af15u,m.CreatedCall.At,"Only original Created completed");Eq(0u,m.Agent.RequestE8.Generation00.Read(0),"No prior frame Submit");Eq(0,RefCount(m.Agent),"No corridor injected");Eq(0,WorkCount(m.Group),"No Work injected");
   Check(Move.Play.Returned&&Move.Play.SyncedReturned&&Move.Handle.RunningSoundCalls[0].Returned,"Actual Player/resource and double-null sound return");Same(Move.Resource,Move.Channel.Resource10.Read(0),"Same factory resource in original mesh");Eq(4u,Move.Resource.Dword74.Read(0),"No decoder or native playback claimed");
   Target=Values(m.Agent.Target78);SameState();Released();
  }
  public void SameState(){
   var m=Move.Movement;var r=Move.Request;var a=m.Agent;
   Same(RequestIdentity,a.RequestE8,"Same live request across Begin/frames");Same(r.Owner.Interface38,a.Callback08.Read(0),"Original Player callback");Same(GameMovementPrimaryTable.Player,r.Owner.Entity.Virtual00.Read(0),"Original Player primary");Same(GameMovementInterfaceTable.Player,r.Owner.Interface38.Virtual38.Read(0),"Original Player interface");Eq(0x800004c5u,a.Flags44.Read(0),"Created Player flags not changed to Character");
   Same(r.Move,r.Owner.Entity.Identity,"Typed full Entity is original MoveCharacter");var sounds=GameRunningSoundState.ForOwner(r.Owner);Same(r.Move.Sound2210,sounds.Sound2210,"Begin/Stop borrow one primary sound cell");Same(r.Move.Sound2110,sounds.Sound2110,"Begin/Stop borrow one fallback sound cell");
   Same(a,m.Node.Agent18.Read(0),"Registry still selects original Agent");Same(a,m.Group.Members28.Begin00.Read(0).Buffer.At(0,0).Read(0),"Same real group member");Same(m.Crowd,m.Group.Owner00.Read(0),"Original Crowd");Same(m.Query,m.Group.Query08.Read(0),"Original query adapter");Same(Scene.Query,m.Query.Query10.Read(0),"Same saved query");Same(Scene.Floor,Publication.Moved.Floor,"Same Ground context");Same(r.Callbacks,Publication.Moved.Callbacks,"Same Player callback registry");Same(r.EngineLifetime,Publication.GateEngine.Engine,"Same Engine lifetime");Same(r.Movement.EngineGlobal,Publication.Gates.EngineImport609050.Read(0),"Same live Engine global");Same(Move.Mesh,Move.Actor.Mesh2668.Read(0),"Same animation/publication mesh");Same(m.Movement.Path,Tick.Path,"Same direct path boundaries");Same(m.Movement.Adapter,Tick.Adapter,"Same direct query boundaries");if(Cold)Same(m.ConstructorGlobal,Tick.Global422980,"Ctor and frame still share global identity");
   Eq(1,Move.Handle.WaypointCalls.Count,"No second Begin waypoint call");Eq(1,Move.Handle.WaypointCalls[0].MoveObjectCalls.Count,"No second Move151130");
   if(ActualRegistration){Same(a,m.ActivationCall.Registration.NewAgent,"Same actual registration Agent across all frames");Same(m.Node,m.ActivationCall.Registration.NewNode,"Same actual publication node");Same(m.Movement,StopProduction.Stop.Movement,"Stop borrows original movement context");Same(m.Registry,m.ActivationCall.Registration.Registry,"Same publication map across frames");Eq((byte)1,r.Manager.Registered6C.Read(0),"Actual activation byte preserved");}
  }
  public void Released(){var m=Move.Movement;Eq(0,Move.Request.Lock.OwnOutstandingEnters,"Object locks released");Eq(0,m.Crowd.OwnOutstandingEnters,"Crowd lock released");Eq(0,m.Query.Critical138.Read(0).OwnOutstandingEnters,"Query lock released");Check(!Scene.Lock.IsHeldByCurrentThread&&Scene.Lock.CurrentThreadDepth==0,"Ground lock released");Check(!Move.ResourceManager.SynchronizationC0.Read(0).IsHeldByCurrentThread,"Resource lock released");}
  public void PrefixFrame(int frame){PrefixFrame(frame,false);}
  public void PrefixFrame(int frame,bool allowEmptySteeringEdge){
   // Explicit OWN clock, not a recovered scheduler. LastFrame is an input to
   // the current callback contract, whose production body does not update it.
   Move.Request.EngineLifetime.FrameCount640.Write((uint)(100+frame));Move.Request.Owner.Entity.LastFrame178.Write((uint)(99+frame));Eq(0,WorkCount(Move.Movement.Group),"Prior real tail left Work empty");Stage="frame-"+(frame+1)+"-ready-prefix";
   if(allowEmptySteeringEdge)NavigationCrowdTickPrefixContracts.RunSingleReadyThroughSteeringChoice25C440(Move.Movement.Group,Move.Movement.Agent,Domain,50,Boundary,Publication.Moved,Prefix);
   else NavigationCrowdTickPrefixContracts.RunSingleReady25C440(Move.Movement.Group,Move.Movement.Agent,Domain,50,Boundary,Publication.Moved,Prefix);
  }
  public void FinishFrame(int frame){FinishFrame(frame,null);}
  public void FinishFrame(int frame,GameReachedGoalContext goal){Stage="frame-"+(frame+1)+"-steering-ground-publication-tail";GameMovedReachedTailContracts.ProduceInvokeAndFinishReachedLastWork(Move.Movement.Group,Move.Movement.Agent,Domain,Prefix.MaximumRadius,Prefix.NeighborFactor,Prefix.Seconds,50,Tick,Publication.Moved,Tail,goal);}
  public void FinishChosenFrame(int frame,GameReachedGoalContext goal){Stage="frame-"+(frame+1)+"-steering-choice-ground-publication-tail";GameMovedReachedTailContracts.ProduceInvokeAndFinishReachedSteeringChoice(Move.Movement.Group,Move.Movement.Agent,Domain,Prefix,50,Tick,Publication.Moved,Tail,goal);}
 }
 static void Scenario(string name,bool positive,Action body){FailureScenario=name;int before=assertions;try{body();scenarios++;}finally{if(positive)PositiveAssertions+=assertions-before;else NegativeAssertions+=assertions-before;}}
 static void ResetFrameResults(){
  scenarios=assertions=PositiveAssertions=NegativeAssertions=PriorFixtureSetupChecks=CompletedFrames=0;BeginReturned=OuterReturned=PassedTwoFrames=NetHorizontalProgress=false;OuterResult=0;UnsupportedRva=DefaultUnknownStopRva=IdentityUnknownStopRva=0;Frames.Clear();NativeMathCalls=0;RequestedGroundTarget=AgentTarget=null;InitialHorizontalDistance=FinalHorizontalDistance=0;Stage="open-own-pinned-math";FailureScenario="setup";
 }
 static void BoundaryRegressions(SystemUcrtRotationMath math){
   Scenario("default-Unknown-constructor",false,delegate{
    var f=new Fixture(math,false);DefaultUnknownStopRva=Missing(0x05e1ae,delegate{f.PrefixFrame(0);});Check(!f.Prefix.EarlyMovedReturned&&!f.Prefix.Reached25CB73,"Unknown direction is not a completed early callback/frame");Eq(0u,f.Move.Movement.Agent.RequestE8.Generation00.Read(0),"No later replan after Unknown");Unknown(f.Move.Movement.Agent.Vector60.X,"Unknown remains after actual consumer");f.SameState();f.Released();
   });
   Scenario("Player-Engine-identity-Unknown",false,delegate{
    var f=new Fixture(math,true);f.Publication.GateEngine.Identity668.Forget();IdentityUnknownStopRva=Missing(0x078117,delegate{f.PrefixFrame(0);});Check(!f.Prefix.EarlyMovedReturned&&!f.Prefix.Reached25CB73,"Unknown Player identity cannot produce completed early publication");Unknown(f.Publication.GateEngine.Identity668,"Identity not defaulted to local/server mode");f.SameState();f.Released();
   });
 }
 public static void RunBoundaryRegressions(){
  ResetFrameResults();using(var math=SystemUcrtRotationMath.OpenOwnProfile())try{BoundaryRegressions(math);Stage="passed-player-boundary-regressions";FailureScenario=null;}catch(MissingContract error){UnsupportedRva=error.Address;throw;}finally{NativeMathCalls=math.NativeMathCalls;}
 }
 public static void Run(){
  ResetFrameResults();using(var math=SystemUcrtRotationMath.OpenOwnProfile())try{
   BoundaryRegressions(math);
   Scenario("same-Player-Begin-then-two-bounded-frames",true,delegate{
    var f=new Fixture(math,true);var m=f.Move.Movement;var r=f.Move.Request;var a=m.Agent;var entity=r.Owner.Entity;BeginReturned=f.Move.Handle.BeginReturned;OuterReturned=r.Boundaries.LastRequest.Returned;OuterResult=r.MouseController.Byte432.Read(0);RequestedGroundTarget=Values(r.Position);AgentTarget=f.Target;InitialHorizontalDistance=Distance(a.Position48.X.Read(0),a.Position48.Z.Read(0),f.Target);
    for(int frame=0;frame<2;frame++){
     float beforeX=a.Position48.X.Read(0),beforeZ=a.Position48.Z.Read(0);double beforeDistance=Distance(beforeX,beforeZ,f.Target);f.PrefixFrame(frame);Check(f.Prefix.Reached25CB73&&f.Prefix.UpdateReturned,"Actual prefix completed");Same(a,f.Prefix.Agent,"Prefix uses original Agent");Eq(1,WorkCount(m.Group),"Prefix really produced Work");
     if(frame==0){var sync=f.Prefix.SynchronousReplan;Check(sync!=null&&sync.Returned&&sync.SubmitReturned&&sync.ReplanCleared,"Original request triggers real synchronous replan");Eq((byte)1,sync.Completion.Read(0),"Path request completed, not character arrival");Check(RefCount(a)>0,"Actual saved map corridor produced");}else Same(null,f.Prefix.SynchronousReplan,"Existing route avoids a second Submit");
     Eq(1u,a.RequestE8.Generation00.Read(0),"Exactly one real request generation");Eq((byte)0,a.NeedReplan140.Read(0),"Actual replan flag cleared");f.FinishFrame(frame);
     Check(f.Tail.Callback.CallbackReturned&&f.Tail.Callback.Moved.FloorSucceeded&&f.Tail.Callback.Moved.RotationReturned&&f.Tail.Callback.Moved.NotificationReturned&&f.Tail.Tail.Returned&&f.Tail.Tail.WorkCleared,"Actual steering/Moved/Ground/publication/tail returned");Check(!f.Tail.FullTick&&!f.Tail.Tail.FullTick,"Bounded frames do not claim full native Tick");Eq(0,WorkCount(m.Group),"Actual tail clears Work");Check(a.Position48.X.Read(0)!=beforeX||a.Position48.Z.Read(0)!=beforeZ,"Actual X/Z advances on each frame");Eq(Bits(a.Position48.X.Read(0)),entity.CoordsD8.Position00.D8.Read(0),"Original Entity X publication");Eq(Bits(a.Position48.Z.Read(0)),entity.CoordsD8.Position00.D10.Read(0),"Original Entity Z publication");float groundY=f.Scene.Floor.LastGroundHit.Position04.Y4.Read(0);Eq(Bits(groundY),entity.CoordsD8.Position00.DC.Read(0),"Entity Y comes from actual Ground hit, not navigation substitution");
     var after=Values(a.Position48);double afterDistance=Distance(after[0],after[2],f.Target);Frames.Add(new FrameResult{Frame=frame+1,NavigationPosition=after,PublishedPosition=Values(entity.CoordsD8.Position00),GroundY=groundY,BeforeDistance=beforeDistance,AfterDistance=afterDistance,DistanceDecreased=afterDistance<beforeDistance,GroundDiffersFromNavigation=Bits(groundY)!=Bits(after[1]),EarlyMovedReturned=f.Prefix.EarlyMovedReturned,SynchronousReplan=f.Prefix.SynchronousReplan!=null,Generation=a.RequestE8.Generation00.Read(0),Flags=a.Flags44.Read(0),SuffixReference=f.Tail.Tail.QueriedReference,CorridorCount=RefCount(a)});
     for(int n=0;n<3;n++)Eq(Bits(f.Target[n]),Bits(Values(a.Target78)[n]),"Original Agent target retained axis"+n);f.SameState();f.Released();CompletedFrames++;
    }
    FinalHorizontalDistance=Distance(a.Position48.X.Read(0),a.Position48.Z.Read(0),f.Target);NetHorizontalProgress=FinalHorizontalDistance<InitialHorizontalDistance;Check(NetHorizontalProgress,"Measured two-frame progress toward the same target");Check(FinalHorizontalDistance>0,"Two-frame slice has not arrived at target");Eq(2,CompletedFrames,"Exactly two real frames");PassedTwoFrames=true;
   });
   Stage="passed-same-player-Begin-two-bounded-frames";FailureScenario=null;
  }catch(MissingContract e){UnsupportedRva=e.Address;throw;}finally{NativeMathCalls=math.NativeMathCalls;}
 }
 // Independent bounded experiment, reusing the exact fixture above. This
 // entry never calls Run(), so it does not repeat its two-frame/negative suite.
 public const int ArrivalFrameLimit=64;
 public static int ArrivalAttemptedFrames,ArrivalCompletedFrames,ArrivalAfterStopFrames;
 public static bool ArrivalGoalPrefixWritten,ArrivalGoalEntered,ArrivalGoalReturned,ArrivalStopped,ArrivalSuccess;
 public static uint ArrivalFirstUnsupported;
 public static string ArrivalOutcome,ArrivalStage;
 public sealed class ArrivalFrameResult {
  public int Frame,WorkCount;public float[] NavigationPosition,PublishedPosition,Velocity;
  public float GroundY,Timer;public uint TimerBits,State70,Reason74,Generation,Flags,ActionState;
  public byte Moving,NeedReplan140;public byte? CurrentActionFlag11;public bool CurrentActionPresent,SteeringSkippedEmptyWaypoint;
  public double HorizontalDistance;
  public bool PrefixReturned,ContinuationEntered,TailEntered,TailReturned,GroundComputedThisFrame,GoalPrefixWritten,GoalEntered,GoalReturned,WorkCleared;
  public uint UnsupportedRva,PrefixRva,TailRva,GoalRva;public string Stage;
 }
 public static readonly List<ArrivalFrameResult> ArrivalFrames=new List<ArrivalFrameResult>();
 static ArrivalFrameResult RecordArrival(Fixture f,int frame,bool prefixReturned,bool tailEntered,uint unsupported){
  var m=f.Move.Movement;var a=m.Agent;var r=f.Move.Request;var active=m.Composition.Handler.Current08.Read(0);var move=active as MoveActionRecord;var tail=tailEntered?f.Tail.Tail:null;
  return new ArrivalFrameResult{Frame=frame+1,WorkCount=WorkCount(m.Group),NavigationPosition=Values(a.Position48),PublishedPosition=Values(r.Owner.Entity.CoordsD8.Position00),Velocity=Values(a.VelocityB8),GroundY=f.Scene.Floor.LastGroundHit.Position04.Y4.Read(0),Timer=Float(a.Timer6C.Read(0)),TimerBits=a.Timer6C.Read(0),State70=a.State70.Read(0),Reason74=a.Reason74.Read(0),Generation=a.RequestE8.Generation00.Read(0),Flags=a.Flags44.Read(0),ActionState=r.Owner.ActionState24B4.Read(0),Moving=r.Manager.Moving38.Read(0),NeedReplan140=a.NeedReplan140.Read(0),CurrentActionPresent=active!=null,CurrentActionFlag11=move==null?(byte?)null:move.Flag11.Read(0),HorizontalDistance=Distance(a.Position48.X.Read(0),a.Position48.Z.Read(0),f.Target),PrefixReturned=prefixReturned,SteeringSkippedEmptyWaypoint=f.Prefix.Reached25CF79,ContinuationEntered=tailEntered,TailEntered=tail!=null&&tail.Entered,TailReturned=tail!=null&&tail.Returned,GroundComputedThisFrame=f.Prefix.EarlyMoved.FloorSucceeded||(tailEntered&&f.Tail.Callback.Moved.FloorSucceeded),GoalPrefixWritten=tail!=null&&tail.GoalPrefixWritten,GoalEntered=tail!=null&&tail.GoalEntered,GoalReturned=tail!=null&&tail.GoalReturned,WorkCleared=tail!=null&&tail.WorkCleared,UnsupportedRva=unsupported,PrefixRva=f.Prefix.Native.At,TailRva=tail==null?0:tail.Native.At,GoalRva=tail==null||tail.GoalCall==null?0:tail.GoalCall.Native.At,Stage=Stage};
 }
 public static bool RunArrivalBounded(){
  ResetFrameResults();ArrivalFrames.Clear();ArrivalAttemptedFrames=ArrivalCompletedFrames=ArrivalAfterStopFrames=0;ArrivalGoalPrefixWritten=ArrivalGoalEntered=ArrivalGoalReturned=ArrivalStopped=ArrivalSuccess=false;ArrivalFirstUnsupported=0;ArrivalOutcome="not-run";ArrivalStage=Stage="arrival-open-own-pinned-math";FailureScenario="single-Player-bounded-arrival";
  using(var math=SystemUcrtRotationMath.OpenOwnProfile())try{
   var f=new Fixture(math,true);var m=f.Move.Movement;var r=f.Move.Request;var a=m.Agent;var goal=m.Composition.Production.ReachedGoal;
   BeginReturned=f.Move.Handle.BeginReturned;OuterReturned=r.Boundaries.LastRequest.Returned;OuterResult=r.MouseController.Byte432.Read(0);RequestedGroundTarget=Values(r.Position);AgentTarget=f.Target;InitialHorizontalDistance=Distance(a.Position48.X.Read(0),a.Position48.Z.Read(0),f.Target);
   Same(r.Bridge,goal.Bridge,"Goal borrows original bridge");Same(r.ManagerEngine,goal.Engine,"Goal borrows original ObjectManager context");Same(r.Movement,goal.Move,"Goal borrows original MoveContext");Same(m.Composition.Entry.MoveProductionCall.AllocatedAction,m.Composition.Handler.Current08.Read(0),"Keep the real active MoveTo action");Eq((byte)0,r.Move.SpeedFlag26B3.Read(0),"Existing composition supplies explicit OWN speed flag");Unknown(r.Manager.Registered6C,"No guessed registered byte to bypass NavStop");
   float[] stoppedPosition=null;ArrivalOutcome="bounded-running";
   for(int frame=0;frame<ArrivalFrameLimit;frame++){
    ArrivalAttemptedFrames=frame+1;bool prefixReturned=false,tailEntered=false;uint unsupported=0;
    try{
     f.PrefixFrame(frame,true);prefixReturned=true;Check((f.Prefix.Reached25CB73||f.Prefix.Reached25CF79)&&f.Prefix.UpdateReturned,"Actual ready prefix returned at a proven steering edge");tailEntered=true;f.FinishChosenFrame(frame,goal);
     Check(f.Tail.Callback.CallbackReturned&&f.Tail.Callback.Moved.FloorSucceeded&&f.Tail.Callback.Moved.NotificationReturned&&f.Tail.Tail.Returned&&f.Tail.Tail.WorkCleared,"Actual movement/publication/tail returned");
     Eq(Bits(a.Position48.X.Read(0)),r.Owner.Entity.CoordsD8.Position00.D8.Read(0),"Same Entity X on each completed frame");Eq(Bits(a.Position48.Z.Read(0)),r.Owner.Entity.CoordsD8.Position00.D10.Read(0),"Same Entity Z on each completed frame");Eq(Bits(f.Scene.Floor.LastGroundHit.Position04.Y4.Read(0)),r.Owner.Entity.CoordsD8.Position00.DC.Read(0),"Real Ground Y on each completed frame");Eq(0,WorkCount(m.Group),"Real tail cleared Work");f.SameState();f.Released();ArrivalCompletedFrames++;
     if(f.Tail.Tail.GoalReturned){
      Check(f.Tail.Tail.GoalCall.Returned&&f.Tail.Tail.GoalCall.CharacterStopReturned&&f.Tail.Tail.GoalCall.MovementCleared,"Actual Game arrival/stop callback returned");Eq((byte)0,r.Manager.Moving38.Read(0),"Real movement flag cleared");Same(null,m.Composition.Handler.Current08.Read(0),"Actual stop/destruction cleared current action");Eq(1u,r.Owner.ActionState24B4.Read(0),"Real action state is stopped");Eq(0u,a.Timer6C.Read(0),"Stopped Timer is actual zero");for(int axis=0;axis<3;axis++)Eq(0u,Bits(Values(a.VelocityB8)[axis]),"Actual stopped velocity axis"+axis);
      if(!ArrivalStopped){ArrivalStopped=true;stoppedPosition=Values(r.Owner.Entity.CoordsD8.Position00);}
     }else if(ArrivalStopped){
      // These would be actual further prefix/tail calls, never an empty-Work
      // shortcut. Current State4 continuation remains explicitly Unsupported.
      for(int axis=0;axis<3;axis++)Eq(Bits(stoppedPosition[axis]),Bits(Values(r.Owner.Entity.CoordsD8.Position00)[axis]),"Post-stop position unchanged axis"+axis);ArrivalAfterStopFrames++;
     }
    }catch(MissingContract error){unsupported=error.Address;ArrivalFirstUnsupported=UnsupportedRva=error.Address;ArrivalOutcome="unsupported-bounded-arrival-prefix";f.Released();}
    var record=RecordArrival(f,frame,prefixReturned,tailEntered,unsupported);ArrivalFrames.Add(record);ArrivalStage=record.Stage;ArrivalGoalPrefixWritten|=record.GoalPrefixWritten;ArrivalGoalEntered|=record.GoalEntered;ArrivalGoalReturned|=record.GoalReturned;FinalHorizontalDistance=record.HorizontalDistance;NetHorizontalProgress=FinalHorizontalDistance<InitialHorizontalDistance;
    if(unsupported!=0)return false;
    if(ArrivalStopped&&ArrivalAfterStopFrames>=2){ArrivalSuccess=true;ArrivalOutcome="passed-bounded-arrival-stop-and-no-drift";scenarios=1;return true;}
   }
   ArrivalOutcome="bounded-frame-limit-without-verified-stop";return false;
  }catch(MissingContract error){ArrivalFirstUnsupported=UnsupportedRva=error.Address;ArrivalStage=Stage;ArrivalOutcome="unsupported-bounded-arrival-prefix";return false;}finally{NativeMathCalls=math.NativeMathCalls;}
 }
 // Independent diagnostic: does not invoke the legacy Run/arrival/regression
 // entry points. Unknown Hook initialization is the expected stop, not a
 // successful Goal, completed arrival frame or post-stop/no-drift result.
 public const int RegisteredArrivalFrameLimit=30;
 public const uint RegisteredArrivalExpectedUnsupported=0x0ec01f;
  public const uint ColdHookArrivalExpectedUnsupported=0x0ebb19;
  public const uint SharedEngineArrivalExpectedUnsupported=0x0ebb19;
  public static bool ColdHookConstructorApplied,SharedEngineBindingVerified;
 public static bool ActualRegistration,RegisteredArrivalActivationReturned,RegisteredArrivalNewAgentMatched,RegisteredArrivalNewNodeMatched,RegisteredArrivalStopReturned,RegisteredArrivalOwnActionReleased,RegisteredArrivalHookBoundUnknown,RegisteredArrivalExpectedStopObserved;
  public static void RunRegisteredArrivalBounded(){RunRegisteredArrivalCore(false);}
  // Fresh execution from one real request. Reconstruct only the evidenced OWN
  // cold HookPack; borrow the existing Engine global and leave service state Unknown.
  public static void RunRegisteredArrivalWithColdHookBounded(){RunRegisteredArrivalCore(true);}
  public static void RunRegisteredArrivalWithSharedEngineBounded(){RunRegisteredArrivalCore(true);}
  static void RunRegisteredArrivalCore(bool constructOwnColdHook){
  ResetFrameResults();ArrivalFrames.Clear();ArrivalAttemptedFrames=ArrivalCompletedFrames=ArrivalAfterStopFrames=0;ArrivalGoalPrefixWritten=ArrivalGoalEntered=ArrivalGoalReturned=ArrivalStopped=ArrivalSuccess=false;ArrivalFirstUnsupported=0;ArrivalOutcome="registered-arrival-not-run";ArrivalStage=Stage="registered-arrival-open-own-pinned-math";FailureScenario="single-registered-Player-bounded-arrival";
   ActualRegistration=true;ColdHookConstructorApplied=SharedEngineBindingVerified=false;RegisteredArrivalActivationReturned=RegisteredArrivalNewAgentMatched=RegisteredArrivalNewNodeMatched=RegisteredArrivalStopReturned=RegisteredArrivalOwnActionReleased=RegisteredArrivalHookBoundUnknown=RegisteredArrivalExpectedStopObserved=false;
  using(var math=SystemUcrtRotationMath.OpenOwnProfile())try{
   var f=new Fixture(math,true,true);var m=f.Move.Movement;var r=f.Move.Request;var a=m.Agent;var goal=m.Composition.Production.ReachedGoal;var active=m.Composition.Entry.MoveProductionCall.AllocatedAction;
   RegisteredArrivalActivationReturned=m.ActivationCall.Returned&&m.ActivationCall.Registration.Returned&&m.ActivationCall.Registration.CreatedReturned&&m.ActivationCall.Registration.Joined;
   RegisteredArrivalNewAgentMatched=Object.ReferenceEquals(a,m.ActivationCall.Registration.NewAgent)&&Object.ReferenceEquals(a,m.Node.Agent18.Read(0))&&Object.ReferenceEquals(a,m.Group.Members28.Begin00.Read(0).Buffer.At(0,0).Read(0));RegisteredArrivalNewNodeMatched=Object.ReferenceEquals(m.Node,m.ActivationCall.Registration.NewNode)&&Object.ReferenceEquals(m.Node,m.Buckets.Cells[0].First00.Read(0));
   Check(RegisteredArrivalActivationReturned&&RegisteredArrivalNewAgentMatched&&RegisteredArrivalNewNodeMatched,"Original Add/Created/Join identities reach the real Begin chain");Same(r.Bridge,goal.Bridge,"Original arrival bridge");Same(r.Movement,goal.Move,"Original arrival MoveContext");Same(r.ManagerEngine,goal.Engine,"Original arrival ObjectManager");Same(f.StopProduction,goal.StopProduction,"Original explicit Stop production binding");Same(active,m.Composition.Handler.Current08.Read(0),"Keep actually constructed current action");Unknown(GameReachedGoalCharacterFields.ForOwner(r.Owner).HookPack45F0.Initialized1C,"Hook initialization is not guessed");RegisteredArrivalHookBoundUnknown=true;
    if(constructOwnColdHook){
     var hook=GameReachedGoalCharacterFields.ForOwner(r.Owner).HookPack45F0;
     var records=GameReachedGoalEngineContracts.ConstructOwnColdPack0EBDE0(hook,11,new Invocation());
     Eq(11u,hook.Count18.Read(0),"Character's observed Hook count11");Eq((byte)0,hook.Initialized1C.Read(0),"Cold constructor did not initialize Hook");Same(records,hook.Records10.Read(0),"Original inline Hook publishes own constructed records");
     Same(r.Movement.EngineGlobal,goal.Engine.BorrowedEngineGlobal,"Goal borrows the original mutable global cell");var engine=goal.Engine.CaptureEngine421428().Read(0);Same(r.Engine,engine.WorldIdentity,"Goal reads the original WorldEngine identity");Same(engine,GameReachedGoalLogEngine.ForWorldEngine(r.Engine),"One canonical per-Engine Hook view");Unknown(engine.LuaService68,"No default service or null state to bypass initialization");Unknown(goal.Engine.Engine421428,"Legacy independent component cell remains unused");SharedEngineBindingVerified=true;ColdHookConstructorApplied=true;RegisteredArrivalHookBoundUnknown=false;
    }
    BeginReturned=f.Move.Handle.BeginReturned;OuterReturned=r.Boundaries.LastRequest.Returned;OuterResult=r.MouseController.Byte432.Read(0);RequestedGroundTarget=Values(r.Position);AgentTarget=f.Target;InitialHorizontalDistance=Distance(a.Position48.X.Read(0),a.Position48.Z.Read(0),f.Target);ArrivalOutcome="registered-arrival-bounded-running";
   for(int frame=0;frame<RegisteredArrivalFrameLimit;frame++){
    ArrivalAttemptedFrames=frame+1;bool prefixReturned=false,tailEntered=false;uint unsupported=0;
    try{
     f.PrefixFrame(frame,true);prefixReturned=true;Check((f.Prefix.Reached25CB73||f.Prefix.Reached25CF79)&&f.Prefix.UpdateReturned,"Same registered Agent reaches actual steering edge");tailEntered=true;f.FinishChosenFrame(frame,goal);
     Check(f.Tail.Callback.CallbackReturned&&f.Tail.Callback.Moved.FloorSucceeded&&f.Tail.Callback.Moved.NotificationReturned&&f.Tail.Tail.Returned&&f.Tail.Tail.WorkCleared,"Actual movement/publication/tail completed before next frame");Eq(Bits(a.Position48.X.Read(0)),r.Owner.Entity.CoordsD8.Position00.D8.Read(0),"Same Entity X publication");Eq(Bits(a.Position48.Z.Read(0)),r.Owner.Entity.CoordsD8.Position00.D10.Read(0),"Same Entity Z publication");Eq(Bits(f.Scene.Floor.LastGroundHit.Position04.Y4.Read(0)),r.Owner.Entity.CoordsD8.Position00.DC.Read(0),"Published height is actual saved Ground hit");Eq(0,WorkCount(m.Group),"Only actual completed tail cleared Work");f.SameState();f.Released();ArrivalCompletedFrames++;
    }catch(MissingContract error){unsupported=error.Address;ArrivalFirstUnsupported=UnsupportedRva=error.Address;ArrivalOutcome="unsupported-registered-arrival-prefix";f.SameState();f.Released();}
    var record=RecordArrival(f,frame,prefixReturned,tailEntered,unsupported);ArrivalFrames.Add(record);ArrivalStage=record.Stage;ArrivalGoalPrefixWritten|=record.GoalPrefixWritten;ArrivalGoalEntered|=record.GoalEntered;ArrivalGoalReturned|=record.GoalReturned;FinalHorizontalDistance=record.HorizontalDistance;NetHorizontalProgress=FinalHorizontalDistance<InitialHorizontalDistance;
    if(tailEntered&&f.Tail.Tail.GoalCall!=null){var reached=f.Tail.Tail.GoalCall;RegisteredArrivalStopReturned=reached.ActionStopReturned&&reached.ActionDestructorReturned&&reached.CharacterStopReturned;RegisteredArrivalOwnActionReleased=f.StopProduction.LastDestruction!=null&&f.StopProduction.LastDestruction.OwnStorageReleased;}
    if(unsupported!=0){
      if(unsupported==(constructOwnColdHook?ColdHookArrivalExpectedUnsupported:RegisteredArrivalExpectedUnsupported)){
      Check(record.GoalPrefixWritten&&record.GoalEntered&&!record.GoalReturned&&!record.TailReturned&&!record.WorkCleared,"Unknown Hook prevents actual Goal and tail return");Check(RegisteredArrivalStopReturned&&RegisteredArrivalOwnActionReleased,"Actual Stop and own scalar deletion returned before Hook");Same(a,f.StopProduction.LastStop.Agent.Read(0),"Goal Stop looked up the actual registered Agent");Same(m.Registry,f.StopProduction.LastStop.Registry,"Goal Stop used original publication map");Same(r.Owner,f.StopProduction.LastStopOwner,"Original Player stopped");Same(active,f.StopProduction.LastDestruction.ReturnedRax,"Original constructor allocation deleted");Check(!f.StopProduction.LastDestruction.NativeHeapFreeExecuted,"OWN release is not native heap execution");Missing(0,delegate{active.RequireLive(0);});Same(null,m.Composition.Handler.Current08.Read(0),"Caller cleared current after actual destructor");Eq(1u,r.Owner.ActionState24B4.Read(0),"Actual stopped action state");Eq((byte)0,r.Manager.Moving38.Read(0),"Actual moving byte cleared");Eq(3u,a.State70.Read(0),"Actual Stop wrote Agent state3");Eq(0u,a.Timer6C.Read(0),"Actual Stop timer clear");Eq(1,WorkCount(m.Group),"Failed Goal leaves current Work intact");Eq(1UL,m.Registry.Count38.Read(0),"Stop does not deregister");Eq(1u,a.RequestE8.Generation00.Read(0),"Single original request, no per-frame resubmission");if(constructOwnColdHook){Eq((byte)0,GameReachedGoalCharacterFields.ForOwner(r.Owner).HookPack45F0.Initialized1C.Read(0),"Failed service read did not publish Initialized");Same(r.Engine,goal.Engine.CaptureEngine421428().Read(0).WorldIdentity,"Goal failure retains original Engine identity");Unknown(GameReachedGoalLogEngine.ForWorldEngine(r.Engine).LuaService68,"No service fabricated during arrival");}RegisteredArrivalExpectedStopObserved=true;ArrivalOutcome="observed-registered-arrival-stop-before-unknown-hook";
     }
     return;
    }
    if(record.GoalReturned){ArrivalOutcome="unexpected-registered-goal-return-with-unknown-hook";return;}
   }
   ArrivalOutcome="registered-arrival-frame-limit-without-expected-stop";
  }catch(MissingContract error){ArrivalFirstUnsupported=UnsupportedRva=error.Address;ArrivalStage=Stage;ArrivalOutcome="unsupported-registered-arrival-setup-or-prefix";}finally{NativeMathCalls=math.NativeMathCalls;}
 }
}
