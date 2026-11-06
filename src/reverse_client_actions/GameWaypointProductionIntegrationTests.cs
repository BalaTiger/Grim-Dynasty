using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Saved resources plus explicit OWN hot bindings. No prior suite Run is called.
// This proves Agent target request production, not physical input or motion.
public static class GameWaypointProductionIntegrationTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}public static int Assertions{get{return assertions;}}
 public static int PriorFixtureSetupChecks;
 public static bool ActualTargetSubmitted,RepeatPreservedIdentity,IgnoredFloorZero,PostSubmissionPrefix,BeginWaypointConnected;
 public static uint NextStopRva;
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract error){Eq(at,error.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Unknown<T>(Field<T> value,string why){Missing(0,delegate{value.Read(0);},why);}
 static void Invalid(Action body,string why){try{body();}catch(InvalidOperationException){Check(true,why);return;}throw new Exception("Expected conflicting binding: "+why);}
 static void Test(string name,Action body){try{body();scenarios++;}catch(Exception error){throw new Exception(name+": "+error.Message,error);}}
 static void Point(NavigationPoint3 p,float x,float y,float z){p.X.Write(x);p.Y.Write(y);p.Z.Write(z);}
 static void IntPoint(NavigationCrowdInt3 p,int x,int y,int z){p.X.Write(x);p.Y.Write(y);p.Z.Write(z);}
 static float Float(uint value){return NavigationCoreContracts.FromBits(value);}
 public sealed class Fixture {
  public readonly NavigationSearchWrapperTests.Scene Scene;
  public readonly GameControllerMoveProductionIntegrationTests.Composition Composition;
  public readonly PlayerMoveRequestTests.Fixture Request;
  public readonly NavigationCrowdAgent Agent;
  public readonly NavigationCrowdManager Crowd=new NavigationCrowdManager();
  public readonly NavigationCrowdGroup Group;
  public readonly NavigationCrowdAdapterState Query=new NavigationCrowdAdapterState();
  public readonly NavigationMoveRegistry Registry;
  public readonly NavigationMoveRegistryNode Sentinel=new NavigationMoveRegistryNode(),Node;
  public readonly NavigationMoveRegistryBuckets Buckets=new NavigationMoveRegistryBuckets(1);
  public readonly NavigationMoveObjectContext Movement;
  public readonly NavigationSearchManagerView Navigation;
  public readonly GameWaypointProductionContext Context;
  public readonly GameWaypointManagerState Manager;
  public readonly GameWaypointNavigationState Nav;
  public readonly GameWaypointProductionInvocation Call=new GameWaypointProductionInvocation();
  public readonly Invocation CreatedCall=new Invocation();
  public readonly NavigationPoint3 ConstructorGlobal;
  public readonly bool ActualRegistration;
  public readonly NavigationCrowdStateBoundaries RegistrationState;
  public readonly NavigationRegistrationContext Registration;
  public readonly GameMovementActivationContext Activation;
  public readonly GameMovementActivationInvocation ActivationCall;
  public readonly WorldPositionWordBridge Destination;
  public readonly uint InitialX,InitialY,InitialZ;
  public Fixture(NavigationSearchWrapperTests.Scene scene,bool initializeQueue):this(scene,initializeQueue,null){}
  public Fixture(NavigationSearchWrapperTests.Scene scene,bool initializeQueue,Action<PlayerMoveRequestTests.Fixture> initializeOwnPositions):this(scene,initializeQueue,initializeOwnPositions,null){}
  public Fixture(NavigationSearchWrapperTests.Scene scene,bool initializeQueue,Action<PlayerMoveRequestTests.Fixture> initializeOwnPositions,NavigationPoint3 ownConstructorGlobal):this(scene,initializeQueue,initializeOwnPositions,ownConstructorGlobal,false){}
  public Fixture(NavigationSearchWrapperTests.Scene scene,bool initializeQueue,Action<PlayerMoveRequestTests.Fixture> initializeOwnPositions,NavigationPoint3 ownConstructorGlobal,bool actualRegistration){
   ActualRegistration=actualRegistration;
   Scene=scene;Composition=new GameControllerMoveProductionIntegrationTests.Composition(scene,true);PriorFixtureSetupChecks+=2;Request=Composition.Request;
   // Optional OWN input setup only, before the real Agent constructor/Created
   // reads Entity coordinates. No result or runtime callback is replaced.
   if(initializeOwnPositions!=null)initializeOwnPositions(Request);
   Query.Query10.Write(scene.Query);Query.Filter18.Include100.Write(1);Query.Filter18.Exclude102.Write(0);Point(Query.Extent11C,.125f,2,.125f);Query.Critical138.Write(new NavigationCrowdCriticalSection());
   var adapter=new NavigationCrowdAdapterBoundaries(scene.Mesh,scene.Search);adapter.UseObservedConstants();
   // Opt-in OWN cold input is borrowed before the actual constructor. The old
   // overload keeps its fresh Unknown global; no Agent is patched afterwards.
   var state=ownConstructorGlobal==null?new NavigationCrowdStateBoundaries(adapter):new NavigationCrowdStateBoundaries(adapter,ownConstructorGlobal);RegistrationState=state;ConstructorGlobal=state.Global422980;state.UseObservedGroupConstants();state.DisableAgentAssert422B9F.Write(0);state.DisableGroupAssert422B90.Write(0);state.DisableAllAssert42144A.Write(0);
   if(!actualRegistration){Agent=new NavigationCrowdAgent();Same(Agent,NavigationCrowdStateContracts.InitializeAgent25E6C0(Agent,Field<object>.Of(Request.Owner.Interface38),state,new Invocation()),"Real constructor returns original Agent");}
   // Explicit OWN Entity prerequisites feed the actual Player Created dispatch.
   // No Agent field is overwritten after construction to force a movement arm.
   var entity=Request.Owner.Entity;entity.Height4A0.Write(2);entity.Scale3AC.Write(1);entity.RadiusComponent49C.Write(0);entity.RadiusComponent4A4.Write(0);entity.RadiusScale3A8.Write(.25f);Point(entity.Motion1D0,2,0,0);
   Request.Owner.SpeedScaleDCC.Write(0);Request.Owner.FlagDC8.Write(0);Request.Owner.FlagDC9.Write(0);Request.Owner.FlagEC2.Write(0);Request.Owner.ActionState24B4.Write(0);Request.Owner.Interface38.Virtual38.Write(GameMovementInterfaceTable.Player);
   Same(GameMovementPrimaryTable.Player,entity.Virtual00.Read(0),"Existing actual Player primary retained");
   if(!actualRegistration){GameMovementCallbackContracts.InvokeCreatedCall25AF13(Request.Owner.Interface38,Agent,Request.Callbacks,CreatedCall);Eq(0x25af15u,CreatedCall.At,"Actual Created call returned");Eq(0x800004c5u,Agent.Flags44.Read(0),"Actual Player Created flags");Eq(2f,Agent.Parameter18.Read(0),"Actual Created computed speed");Eq(.5f,Agent.Radius10.Read(0),"Actual Created computed radius");}
   InitialX=Request.Owner.Entity.CoordsD8.Position00.D8.Read(0);InitialY=Request.Owner.Entity.CoordsD8.Position00.DC.Read(0);InitialZ=Request.Owner.Entity.CoordsD8.Position00.D10.Read(0);
   Crowd.Groups10.InstallOwnStorage(new NavigationCrowdPointerBuffer<NavigationCrowdGroup>(1),0);IntPoint(Crowd.Margin00,0,0,0);Crowd.MaxGroups0C.Write(1);Crowd.QueryB8.Write(Query);
   var lower=new NavigationCrowdInt3();var upper=new NavigationCrowdInt3();IntPoint(lower,0,0,0);IntPoint(upper,128,64,128);
   Group=NavigationCrowdStateContracts.GetGroup25A250(Crowd,lower,upper,state,new Invocation());Group.Members28.InstallOwnStorage(new NavigationCrowdPointerBuffer<NavigationCrowdAgent>(1),0);if(!actualRegistration)NavigationCrowdStateContracts.Join25C340(Group,Agent,state,new Invocation());
   var addresses=new NavigationMoveAddressSpace(Request.Callbacks);addresses.BindOwnEntityAddress(Request.Owner,0x100000000UL);
   var path=new NavigationCrowdPathBoundaries(scene.Mesh);Movement=actualRegistration?new NavigationMoveObjectContext(addresses,adapter,path,state.DisableAllAssert42144A):new NavigationMoveObjectContext(addresses,adapter,path);Movement.UseObservedConstants();
   Registry=NavigationMoveRegistry.ForManager(Crowd);Registry.Sentinel30.Write(Sentinel);Registry.Buckets40.Write(Buckets);Registry.Mask58.Write(0);
   if(actualRegistration){
    // Explicit OWN empty header and exact no-growth domain, before Add creates
    // its Agent/node. Never erase an already published legacy registration.
    Sentinel.Next00.Write(Sentinel);Sentinel.Prev08.Write(Sentinel);Registry.Count38.Write(0);Registry.BucketCount60.Write(1);Registry.MaxLoadFactor28.Write(1);Buckets.Cells[0].First00.Write(Sentinel);Buckets.Cells[0].Last08.Write(Sentinel);
   }else{
    // Preserve the old diagnostic's explicit OWN publication and Unknown6C.
    Node=new NavigationMoveRegistryNode();Node.Key10.Write(addresses.Address(Request.Owner.Interface38,0));Node.Agent18.Write(Agent);Node.Next00.Write(Sentinel);Buckets.Cells[0].First00.Write(Node);Buckets.Cells[0].Last08.Write(Node);
   }
   var outer=NavigationMoveOuterView.ForAdapter(Query);outer.Vtable00.Write(0x33a6c0);outer.Crowd08.Write(Crowd);
   Navigation=Request.Request.Path.Globals.Instance4213B8.Read(0);Context=new GameWaypointProductionContext(Request.Request.Path,Movement);Manager=Context.BindManager(Request.Manager);Nav=Context.BindNavigation(Navigation);Nav.Outer10.Write(Query);if(initializeQueue)Manager.InitializeOwnEmptyQueue();
   Destination=WorldPositionWordBridge.FromDwords(Request.Position);
   if(actualRegistration){
    Registration=new NavigationRegistrationContext(Movement,state);Activation=new GameMovementActivationContext(Context,Registration);ActivationCall=new GameMovementActivationInvocation();
    // OWN enabled gates, not native cold-init evidence. Only actual08ED40
    // writes6C; no caller pre-sets it to force a successful Stop branch.
    GameMovementActivationFields.ForManager(Request.Manager).Disabled6D.Write(0);GameMovementActivationEngineFields.ForEngine(Request.Engine).Disabled292.Write(0);
    GameMovementActivationContracts.Activate08ED40(Request.Manager,Activation,ActivationCall);
    var publication=ActivationCall.Registration;Check(ActivationCall.Returned&&publication.Returned&&publication.CreatedReturned&&publication.Joined,"Actual Activation/Add/Created/Join completed");Agent=publication.NewAgent;Node=publication.NewNode;Check(Agent!=null&&Node!=null,"Only real Add supplies the Agent and node");Same(Group,publication.Group,"Actual Add joins original saved-map group");Same(Agent,Node.Agent18.Read(0),"Published same new Agent");Same(Agent,Group.Members28.Begin00.Read(0).Buffer.At(0,0).Read(0),"Actual Join appended same new Agent");Same(Registry,publication.Registry,"Original registry identity");Eq(1UL,Registry.Count38.Read(0),"One actual publication");Eq((byte)1,Request.Manager.Registered6C.Read(0),"Actual activation writer");Eq(0x800004c5u,Agent.Flags44.Read(0),"Actual registered Player Created flags");Eq(2f,Agent.Parameter18.Read(0),"Actual registered Player speed");Eq(.5f,Agent.Radius10.Read(0),"Actual registered Player radius");Eq(0u,CreatedCall.At,"Legacy direct Created was not called");
   }
  }
  public byte Run(){return GameWaypointProductionContracts.Move08EAF0(Request.Manager,Destination.Words,Context,Call);}
  public void QueuedRecordConsumed(){Eq(0ul,Manager.View.Queue40.Count20.Read(0),"Next consumed the sole record");Eq(0ul,Manager.View.Queue40.Offset18.Read(0),"Last pop reset offset");Eq(1,Manager.OwnLiveMaps,"One queue map stays owned");Eq(1,Manager.OwnLiveBlocks,"Consumed storage remains owned until next Clear");}
  public void SameOwner(){SameOwner(Call);}
  public void SameOwner(GameWaypointProductionInvocation call){
   Same(Request.Manager,Manager.View.Identity,"Original MovementManager");Same(Request.Move,Request.Manager.Owner00.Read(0),"Original owner pointer");Same(Request.Owner.Interface38,Agent.Callback08.Read(0),"Constructor retained original interface identity");Same(Agent,Node.Agent18.Read(0),"Registry points to existing Agent");Same(Agent,Group.Members28.Begin00.Read(0).Buffer.Cells[0].Read(0),"Actual Join published same Agent");Same(Crowd,Group.Owner00.Read(0),"Actual group has original Crowd");Same(Query,Group.Query08.Read(0),"Actual group has original query");Same(Nav.Outer10,Nav.MoveView.Outer10,"Search and movement borrow one +10 cell");
   foreach(var owner in call.Owners)Same(Request.Owner,owner,"Every live owner resolution uses original state");
  }
  public void NoMotion(){
   var position=Request.Owner.Entity.CoordsD8.Position00;Eq(InitialX,position.D8.Read(0),"No fabricated Entity X motion");Eq(InitialY,position.DC.Read(0),"No fabricated Entity Y motion");Eq(InitialZ,position.D10.Read(0),"No fabricated Entity Z motion");Eq(Float(InitialX),Agent.Position48.X.Read(0),"Agent X remains initial");Eq(Float(InitialY),Agent.Position48.Y.Read(0),"Agent Y remains initial");Eq(Float(InitialZ),Agent.Position48.Z.Read(0),"Agent Z remains initial");
   Unknown(Agent.Vector60.X,"Unclosed Vector60 X remains unknown");Unknown(Agent.Vector60.Y,"Unclosed Vector60 Y remains unknown");Unknown(Agent.Vector60.Z,"Unclosed Vector60 Z remains unknown");Eq(0u,Agent.RequestE8.Generation00.Read(0),"No fake Submit/replan execution");Eq((byte)0,Agent.RequestE8.Complete50.Read(0),"No fake request completion");
  }
  public void Submitted(){Submitted(Call);}
  public void Submitted(GameWaypointProductionInvocation call){
   Eq(1,call.MoveObjectCalls.Count,"One real MoveObject call");var move=call.MoveObjectCalls[0];Check(move.LookupReturned&&move.ProjectionReturned&&move.Published&&move.Returned,"Actual lookup/project/store request completed");Check(!move.CompressionReturned,"Actual Created bit7 skips compression");Eq(0x800004c5u,Agent.Flags44.Read(0),"Real Created flags were not forced to another branch");Same(Agent,move.Agent.Read(0),"MoveObject selects original Agent");Check(move.ProjectedReference.Read(0)!=0,"Saved mesh produced a real polygon reference");Eq(8,move.HashedBytes,"Actual registry hashes the entire +38 address");Eq(1,move.VisitedNodes,"Actual registry compared its single node");Eq(2u,Agent.State70.Read(0),"MoveObject produced request state2");Eq((byte)1,Agent.NeedReplan140.Read(0),"MoveObject produced replan flag");Check(!Single.IsNaN(Agent.Target78.X.Read(0))&&!Single.IsNaN(Agent.Target78.Y.Read(0))&&!Single.IsNaN(Agent.Target78.Z.Read(0)),"Real projected target is known");Eq(0,Query.Critical138.Read(0).OwnOutstandingEnters,"Projection lock released");SameOwner(call);NoMotion();
  }
 }
 public static void RunBeginWithoutAnimationRegression(){
  scenarios=assertions=PriorFixtureSetupChecks=0;ActualTargetSubmitted=RepeatPreservedIdentity=IgnoredFloorZero=PostSubmissionPrefix=BeginWaypointConnected=false;NextStopRva=0;var scene=new NavigationSearchWrapperTests.Scene();PriorFixtureSetupChecks+=4;BeginWithoutAnimationRegression(scene);
 }
 static void BeginWithoutAnimationRegression(NavigationSearchWrapperTests.Scene scene){
  Test("Saved mouse request and Begin submit actual waypoint before animation boundary",delegate{
   var f=new Fixture(scene,true);var c=f.Composition;var r=f.Request;c.Production.BindBeginPath(r.Request.Path);c.Production.BindWaypoints(f.Context);
   // Explicit OWN hot observations retain the prior Begin fixture's domain.
   r.Move.Gate26AF.Write(0);r.Move.AssociatedId16C0.Write(0);r.Move.CurrentSkillA90.Write(0);r.Engine.World50.Write(r.World);r.Movement.AlreadyThereConstant7A7FD8.Write(.025f);c.Skills.Offset3BC.Write(0);c.Skills.Offset3C0.Write(0);c.Skills.Offset3C4.Write(0);c.Skills.Offset3C8.Write(0);
   Missing(0x05ae40,delegate{r.Outer(new Invocation());},"Actual accepted movement reaches unclosed Animation accessor");
   var request=r.Boundaries.LastRequest;var entry=c.Entry;var production=entry.MoveProductionCall;var handle=production.Handle;
   Check(request.PathReturned&&request.Path.Precheck.NavigationReturned&&request.TransitionEntered&&!request.TransitionReturned&&!request.Returned,"Actual saved-map request reached incomplete state entry");Check(request.Path.Precheck.Navigation.CorridorCount.Read(0)>1,"Original navigation search produced nontrivial corridor");Check(entry.RunSpeedReturned&&production.ConstructorReturned&&handle.BeginEntered&&!handle.BeginReturned&&!handle.Returned&&!production.Returned&&!entry.Returned,"Real production prefix is retained without claiming completed Begin");
   Eq(1,handle.WaypointCalls.Count,"Begin dispatched newly connected actual waypoint adapter");var waypoint=handle.WaypointCalls[0];Check(waypoint.Returned&&waypoint.Result==1,"Waypoint accepted before unresolved animation");f.Submitted(waypoint);f.QueuedRecordConsumed();Eq((byte)1,r.Manager.Moving38.Read(0),"Waypoint wrote original manager moving byte");Same(production.AllocatedAction,c.Handler.Current08.Read(0),"Same original action remains published");Eq((byte)1,c.Handler.Busy18.Read(0),"Incomplete Begin retains busy prefix");Eq(0x05ae40u,handle.BeginNative.At,"Begin preserves real animation stop");Eq(0x05ae40u,production.Native.At,"Production propagates animation stop");Eq(0x05ae40u,entry.Native.At,"State entry propagates animation stop");Eq(0,r.Lock.OwnOutstandingEnters,"Object lookup lock released");Check(!scene.Lock.IsHeldByCurrentThread,"Ground lock released");BeginWaypointConnected=true;NextStopRva=0x05ae40;
  });
  }
 public static void Run(){
  scenarios=assertions=PriorFixtureSetupChecks=0;ActualTargetSubmitted=RepeatPreservedIdentity=IgnoredFloorZero=PostSubmissionPrefix=BeginWaypointConnected=false;NextStopRva=0;var scene=new NavigationSearchWrapperTests.Scene();PriorFixtureSetupChecks+=4;
  Test("Binding preserves unique shared state and leaves unknown queue bytes",delegate{
   var f=new Fixture(scene,false);Same(f.Manager,f.Context.BindManager(f.Request.Manager),"Manager binding idempotent");Same(f.Nav,f.Context.BindNavigation(f.Navigation),"Navigation binding idempotent");Same(f.Context,f.Manager.Context,"Persistent original composition");Unknown(f.Manager.View.Queue40.Count20,"Bind did not initialize Count");Unknown(f.Manager.View.Queue40.Map08,"Bind did not initialize Map");Unknown(f.Manager.View.Position08.Q0,"Bind did not initialize current-position cache");Unknown(f.Request.Manager.Position20.Q0,"Bind did not initialize destination");Check(!f.Manager.OwnEmptyQueueInitialized,"Queue setup remains explicit");Eq(0,f.Manager.OwnLiveMaps,"Binding allocates no native-looking map");
   var other=new GameWaypointProductionContext(f.Request.Request.Path,f.Movement);Invalid(delegate{other.BindManager(f.Request.Manager);},"Reject conflicting manager context");Invalid(delegate{other.BindNavigation(f.Navigation);},"Reject conflicting navigation context");f.SameOwner();f.NoMotion();
   f.Manager.View.Queue40.Count20.Write(1);Invalid(delegate{f.Manager.InitializeOwnEmptyQueue();},"Observed nonempty Count cannot be overwritten by OWN setup");Eq(1ul,f.Manager.View.Queue40.Count20.Read(0),"Observed Count retained after refusal");Unknown(f.Manager.View.Queue40.Proxy00,"Refusal published no proxy");Unknown(f.Manager.View.Queue40.Map08,"Refusal published no map");Check(!f.Manager.OwnEmptyQueueInitialized,"Refusal did not claim initialization");
  });
  Test("Saved floor and navigation submit to same Agent and repeat with persistent queue",delegate{
   var f=new Fixture(scene,true);Unknown(f.Agent.Target78.X,"No target seeded before actual call");Eq((byte)1,f.Run(),"Ordinary real request accepted");Check(f.Call.Returned,"Waypoint call really returned");f.Submitted();f.QueuedRecordConsumed();Eq((byte)1,f.Request.Manager.Moving38.Read(0),"Moving byte written only after both conversions");Eq(2,f.Call.FloorResults.Count,"Both real floor calls returned");Eq((byte)1,f.Call.FloorResults[0],"Destination received actual Ground");Eq((byte)1,f.Call.FloorResults[1],"Manager destination received actual Ground");Eq(1,f.Call.FromWorldResults.Count,"Actual region conversion returned");Eq((byte)1,f.Call.FromWorldResults[0],"Same-region conversion accepted");Eq(2,f.Call.WorldVectorCalls.Count,"Both actual22C1A0 conversions ran");Eq(2,f.Call.CoordinateReturns.Count,"Both actual64B coordinate calls returned");Same(f.Navigation,f.Call.NavigationReturns[0],"Live singleton is canonical search manager");
   for(int n=0;n<f.Call.CoordinateReturns.Count;n++){Same(f.Call.CoordinateBuffers[n],f.Call.CoordinateReturns[n],"Returned hidden64B identity");Same(f.Call.CoordinateWordBuffers[n],f.Call.CoordinateBuffers[n].AddressIdentity,"Hidden64B borrows exact word local");Unknown(f.Call.CoordinateBuffers[n].Tail3C,"Unwritten tail is not zero-filled");}
   var target=WorldPositionWordBridge.FromWords(f.Request.Manager.Position20).Dwords;Eq(f.Request.Position.D8.Read(0),target.D8.Read(0),"Manager destination X shares actual conversion output");Eq(f.Request.Position.D10.Read(0),target.D10.Read(0),"Manager destination Z shares actual conversion output");Unknown(target.D14,"Unwritten padding remains unknown");
   var firstMap=f.Manager.View.Queue40.Map08.Read(0);Invalid(delegate{f.Manager.InitializeOwnEmptyQueue();},"Second initialization cannot erase owned storage");Same(firstMap,f.Manager.View.Queue40.Map08.Read(0),"Refused initialization retains current map");Check(!firstMap.Released,"Refusal did not free live map");f.QueuedRecordConsumed();Eq((byte)1,f.Run(),"Second request accepted");Check(firstMap.Released,"Real Clear releases previous owned map");Check(!Object.ReferenceEquals(firstMap,f.Manager.View.Queue40.Map08.Read(0)),"Second Grow uses new owned storage");Same(f.Manager,f.Context.BindManager(f.Request.Manager),"Second call retains manager/queue identity");f.QueuedRecordConsumed();f.Submitted();Eq(1,f.Call.MoveObjectCalls.Count,"Reused invocation does not accumulate old success");ActualTargetSubmitted=RepeatPreservedIdentity=true;
  });
  Test("Uninitialized queue stops before owner coordinates or allocation",delegate{
   var f=new Fixture(scene,false);Missing(0x073f84,delegate{f.Run();},"Actual Clear reads unknown count");Check(!f.Call.Returned,"No normal return");Eq(0,f.Call.CoordinateCalls.Count,"No owner call before Clear");Eq(0,f.Call.MoveObjectCalls.Count,"No navigation request");Eq(0,f.Manager.OwnLiveMaps,"No map allocation");Eq(0,f.Manager.OwnLiveBlocks,"No block allocation");Unknown(f.Manager.View.Queue40.Count20,"No zero default");Eq((byte)0,f.Request.Manager.Moving38.Read(0),"Moving byte untouched");
  });
  Test("Unknown live navigation +10 preserves already consumed queue",delegate{
   var f=new Fixture(scene,true);f.Nav.Outer10.Forget();Missing(0x151130,delegate{f.Run();},"Actual MoveObject first read remains unknown");f.QueuedRecordConsumed();Check(!f.Call.Returned,"No fabricated return");Eq(1,f.Call.NavigationReturns.Count,"Singleton really returned before +10 read");Same(f.Navigation,f.Call.NavigationReturns[0],"Same published singleton");Eq(1,f.Call.MoveObjectCalls.Count,"Actual MoveObject entered");Check(!f.Call.MoveObjectCalls[0].Returned&&!f.Call.MoveObjectCalls[0].Published,"No fake projection/request");Eq(1,f.Call.CoordinateReturns.Count,"Only initial coordinates ran");Eq(0,f.Call.FromWorldCalls.Count,"No conversion after failed navigation");Unknown(f.Agent.Target78.X,"Target not seeded on failure");Eq((byte)0,f.Request.Manager.Moving38.Read(0),"Moving byte not written");
  });
  Test("Actual absent registry entry returns zero without restoring consumed record",delegate{
   var f=new Fixture(scene,true);f.Buckets.Cells[0].First00.Write(f.Sentinel);f.Buckets.Cells[0].Last08.Forget();f.Movement.DisableMissingAgent422B8D.Write(1);Eq((byte)0,f.Run(),"Real map miss returns false");Check(f.Call.Returned,"Waypoint false is a completed return");f.QueuedRecordConsumed();var move=f.Call.MoveObjectCalls[0];Check(move.LookupReturned&&move.Returned&&!move.ProjectionReturned&&!move.Published,"Actual miss bypasses projection");Same(null,move.Agent.Read(0),"No replacement Agent");Eq(8,move.HashedBytes,"Real address lookup executed");Eq(0,move.VisitedNodes,"Sentinel bucket is truly empty");Eq(0,f.Call.FromWorldCalls.Count,"No manager destination conversion");Unknown(f.Agent.Target78.X,"No default target");Eq(0u,f.Agent.State70.Read(0),"Constructor idle state retained");Eq((byte)0,f.Agent.NeedReplan140.Read(0),"No fake replan");Eq((byte)0,f.Request.Manager.Moving38.Read(0),"Moving flag unchanged");f.NoMotion();
  });
  Test("Actual floor zero returns are ignored by evidenced waypoint caller",delegate{
   var f=new Fixture(scene,true);var available=scene.Region.Unavailable90.Snapshot();scene.Region.Unavailable90.Write(1);try{Eq((byte)1,f.Run(),"Caller continues after both real floor returns0");f.Submitted();Eq(2,f.Call.FloorResults.Count,"Both floor calls returned");Eq((byte)0,f.Call.FloorResults[0],"First actual unavailable-region branch");Eq((byte)0,f.Call.FloorResults[1],"Second actual unavailable-region branch");Eq((byte)1,f.Call.FromWorldResults[0],"Same-region bounds still accepted");Eq((byte)1,f.Request.Manager.Moving38.Read(0),"Actual late moving store reached");IgnoredFloorZero=true;}finally{scene.Region.Unavailable90.CopyFrom(available);}
  });
  Test("Unknown bounds after real target publication retains request and coordinate prefix",delegate{
   var f=new Fixture(scene,true);var extent=scene.Region.Bounds70.Extent0C.Snapshot();scene.Region.Bounds70.Extent0C.Forget();try{Missing(0x0664d4,delegate{f.Run();},"Actual translation consumes unknown extent after XYZ stores");f.Submitted();f.QueuedRecordConsumed();Check(!f.Call.Returned,"No waypoint success after unknown bounds");Eq(1,f.Call.FloorResults.Count,"First real floor returned before navigation");Eq(1,f.Call.FromWorldCalls.Count,"Real initialization entered");Eq(0,f.Call.FromWorldResults.Count,"No conversion return fabricated");Eq(0x0664d4u,f.Call.FromWorldCalls[0].At,"Exact bounds consumer");Eq(2,f.Call.CoordinateReturns.Count,"Second owner coordinates completed before failure");Eq((byte)0,f.Request.Manager.Moving38.Read(0),"Late moving store not reached");var target=WorldPositionWordBridge.FromWords(f.Request.Manager.Position20).Dwords;Eq(77ul,target.Q0.Read(0),"Region store retained");Eq(f.Request.Position.D8.Read(0),target.D8.Read(0),"Translated X store retained");Eq(f.Request.Position.D10.Read(0),target.D10.Read(0),"Translated Z store retained");Unknown(target.D14,"D14 not initialized by conversion");PostSubmissionPrefix=true;}finally{scene.Region.Bounds70.Extent0C.CopyFrom(extent);}
  });
  BeginWithoutAnimationRegression(scene);
}
}
