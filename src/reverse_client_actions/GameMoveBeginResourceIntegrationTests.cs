using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Same saved-map movement composition + real resource factory/Add + Player tail.
// Fixture inputs are explicit OWN observations, not original cold-start proof.
// A requested name creates an unloaded resource; no file decoder, sound answer,
// native input event or frame update is supplied by a callback or stub.
public static class GameMoveBeginResourceIntegrationTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}public static int Assertions{get{return assertions;}}
 public static int PriorFixtureSetupChecks;
 public static uint NormalStopRva,NullResourceStopRva,GroundStopRva,GroundPathResult,CorridorCount;
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract e){Eq(at,e.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Unknown<T>(Field<T> field,string why){Missing(0,delegate{field.Read(0);},why);}
 static void Test(string name,Action body){try{body();scenarios++;}catch(Exception e){throw new Exception(name+": "+e.Message,e);}}
 static int OldChecks(){return GameWaypointProductionIntegrationTests.Assertions+GameControllerMoveProductionIntegrationTests.Assertions+PlayerMoveRequestTests.Assertions;}
 static EngineResourceStringState Name(){var s=new EngineResourceStringState();byte[] bytes={79,87,78,92,77,79,86,69,46,65,78,77};s.Capacity.Write(15);s.Length.Write((ulong)bytes.Length);for(int n=0;n<bytes.Length;n++)s.Inline.Byte(n).Write(bytes[n]);s.Inline.Byte(bytes.Length).Write(0);return s;}
 public sealed class Fixture {
  public readonly GameWaypointProductionIntegrationTests.Fixture Movement;
  public PlayerMoveRequestTests.Fixture Request{get{return Movement.Request;}}
  public readonly ActorBoundingBoxContext Bounds;public readonly ActorBoundingBoxState Actor;public readonly BorrowedMeshBounds Mesh;
  public readonly EngineAnimationSpeedContext Speed;public readonly GameMoveAnimationProductionContext Animation;
  public readonly AnimationSet Set=new AnimationSet();public readonly GameAnimationEntryState Entry=new GameAnimationEntryState();
  public readonly GameAnimationSetFields SetFields;public readonly EngineAnimationChannelState Channel;
  public readonly object Graphics=new object(),Loader=new object();public readonly GameAnimationResourceBindingContext Resources;
  public readonly EngineAnimationResourceManagerState ResourceManager;public readonly GameAnimationResourceBindingInvocation Add=new GameAnimationResourceBindingInvocation();
  public readonly GameEquipmentManagerState Equipment;public readonly Invocation Outer=new Invocation();
  public Fixture(NavigationSearchWrapperTests.Scene scene,bool missingManager):this(scene,missingManager,null){}
  public Fixture(NavigationSearchWrapperTests.Scene scene,bool missingManager,Action<PlayerMoveRequestTests.Fixture> initializeOwnPositions):this(scene,missingManager,initializeOwnPositions,null){}
  public Fixture(NavigationSearchWrapperTests.Scene scene,bool missingManager,Action<PlayerMoveRequestTests.Fixture> initializeOwnPositions,NavigationPoint3 ownConstructorGlobal):this(scene,missingManager,initializeOwnPositions,ownConstructorGlobal,false){}
  public Fixture(NavigationSearchWrapperTests.Scene scene,bool missingManager,Action<PlayerMoveRequestTests.Fixture> initializeOwnPositions,NavigationPoint3 ownConstructorGlobal,bool actualRegistration){
   int before=OldChecks();Movement=new GameWaypointProductionIntegrationTests.Fixture(scene,true,initializeOwnPositions,ownConstructorGlobal,actualRegistration);PriorFixtureSetupChecks+=OldChecks()-before;
   var r=Request;var c=Movement.Composition;c.Production.BindBeginPath(r.Request.Path);c.Production.BindWaypoints(Movement.Context);
   r.Move.Gate26AF.Write(0);r.Move.AssociatedId16C0.Write(0);r.Move.CurrentSkillA90.Write(0);r.Engine.World50.Write(r.World);r.Movement.AlreadyThereConstant7A7FD8.Write(.025f);c.Skills.Offset3BC.Write(0);c.Skills.Offset3C0.Write(0);c.Skills.Offset3C4.Write(0);c.Skills.Offset3C8.Write(0);
   Bounds=new ActorBoundingBoxContext(r.Callbacks.Entities);Actor=Bounds.Bind(r.Owner.Entity);Mesh=Bounds.BindMesh(new object());Speed=new EngineAnimationSpeedContext(Bounds,Actor);Speed.BindExistingMesh(Mesh);Animation=new GameMoveAnimationProductionContext(r.Bridge,r.Move,Speed);Actor.Mesh2668.Write(Mesh);
   Set.Kind08.Write(0);SetFields=GameAnimationSetFields.ForSet(Set);SetFields.Entry(5).Write(Entry);Entry.Virtual00.Write(0x1806d4d48UL);Entry.Resource10.Write(null);GameCharacterAnimationFields.ForOwner(r.Owner).Set2600.Write(Set);
   // No Float18 or old channel Resource10 default. Individual scenarios declare
   // those prerequisites or verify the real failure when they remain Unknown.
   Channel=EngineAnimationChannelState.ForMesh(Mesh);r.Movement.NoNameImport609568.Write(new object());c.Production.BindAnimations(Animation);
   Equipment=GameEquipmentManagerState.ForOwner(r.Owner);Equipment.Begin08.Write(null);Equipment.End10.Write(null);ActorBoundingBoxContracts.InitializeAttachedIdFields07D020(Actor,new Invocation());
   Resources=new GameAnimationResourceBindingContext(r.Movement.EngineGlobal,EngineAnimationNameLocale.KnownC);EngineGraphicsResourceEngineFields.ForEngine(r.Engine).Graphics1F8.Write(Graphics);
   ResourceManager=EngineAnimationResourceManagerState.CreateOwnInitialized(Field<object>.Of(Loader),r.Movement.EngineGlobal,new EngineAnimationResourceCacheNode[0],new EngineAnimationResourcePair[0]);EngineGraphicsAnimationFields.ForGraphics(Graphics).Manager20.Write(missingManager?null:ResourceManager);
   var selected=GameCharacterAnimationContracts.GetAnimationSet05AE40(r.Owner,new GameGetAnimationSetInvocation());
   Eq(missingManager?(byte)0:(byte)1,GameAnimationResourceBinding.Add024D80(Resources,selected,5,Field<EngineResourceStringState>.Of(Name()),Field<float>.Of(1),Field<float>.Of(0),Add),"Actual Add return");
  }
  public void Run(){Request.Outer(Outer);}
  public GameControllerHandleActionInvocation Handle{get{return Movement.Composition.Entry.MoveProductionCall.Handle;}}
  public GameAnimationPlayInvocation Play{get{return Handle.Animation.PlayCalls[0];}}
  public EngineGraphicsResourceState Resource{get{return Entry.Resource10.Read(0);}}
  public void Ready(){Entry.Float18.Write(3);Channel.Resource10.Write(null);}
 }
 static void ResourceCreated(Fixture f){
  Check(f.Add.Returned&&f.Add.ResourcePublished&&f.Add.Load.Cold&&f.Add.Load.FactoryReturned&&f.Add.Load.Returned,"Actual cold factory/cache/Add completed");Same(f.Resource,f.Add.Load.Construction.Result,"Constructed resource is entry resource");Same(f.Resource,f.ResourceManager.ResourceVector80.Read(0)[0].Resource08.Read(0),"Cache/reference vector shares resource");Same(f.ResourceManager,f.Resource.Manager38.Read(0),"Resource stores same manager");Same(f.Loader,f.Resource.Loader28.Read(0),"Actual loader publication retained");Same(f.Graphics,f.Resource.GraphicsA8.Read(0),"Same Graphics identity");Same(f.Request.Movement.EngineGlobal,f.Resources.EngineGlobal,"Resource context borrows existing Engine cell");Same(f.Resources.EngineGlobal,f.ResourceManager.EngineGlobal,"Manager borrows same cell");Same(f.Request.Lookup,f.Animation.Lookup,"BindAnimations borrows original object lookup");Eq(4u,f.Resource.Dword74.Read(0),"Resource remains in constructed state4, not decoded");Check(!f.ResourceManager.SynchronizationC0.Read(0).IsHeldByCurrentThread,"Resource manager lock released");
 }
 static void SubmittedPrefix(Fixture f,uint at){
  var request=f.Request.Boundaries.LastRequest;var h=f.Handle;var production=f.Movement.Composition.Entry.MoveProductionCall;
  Check(request.PathReturned&&request.Path.Precheck.NavigationReturned&&request.TransitionEntered&&!request.Returned,"Actual request reaches incomplete transition");CorridorCount=request.Path.Precheck.Navigation.CorridorCount.Read(0);Check(CorridorCount>1,"Saved-map route really crosses polygons");
  Eq(1,h.WaypointCalls.Count,"One waypoint request");var waypoint=h.WaypointCalls[0];Check(waypoint.Returned&&waypoint.Result==1,"Actual waypoint body returned");Eq(1,waypoint.MoveObjectCalls.Count,"One actual Agent submission");var move=waypoint.MoveObjectCalls[0];Check(move.Published&&move.Returned&&move.ProjectionReturned,"Actual registry/project/target writes returned");Same(f.Movement.Agent,move.Agent.Read(0),"Original Agent identity");Same(f.Request.Owner.Interface38,f.Movement.Agent.Callback08.Read(0),"Same owner callback interface");
  Check(h.BeginEntered&&!h.BeginReturned&&!h.Returned&&!production.Returned&&!f.Movement.Composition.Entry.Returned,"Downstream Unsupported is not a completed Begin");Eq(at,h.BeginNative.At,"Exact Begin failure");Eq(at,production.Native.At,"Exact production failure");Same(production.AllocatedAction,f.Movement.Composition.Handler.Current08.Read(0),"Same action remains published");Eq((byte)1,f.Movement.Composition.Handler.Busy18.Read(0),"Busy prefix retained");Eq((byte)1,f.Request.Manager.Moving38.Read(0),"Accepted waypoint prefix retained");Eq(5u,f.Request.Owner.ActionState24B4.Read(0),"Actual shared state5 write retained");
  var pos=f.Request.Owner.Entity.CoordsD8.Position00;Eq(f.Movement.InitialX,pos.D8.Read(0),"No Entity X frame movement invented");Eq(f.Movement.InitialY,pos.DC.Read(0),"No Entity Y frame movement invented");Eq(f.Movement.InitialZ,pos.D10.Read(0),"No Entity Z frame movement invented");Eq(2u,f.Movement.Agent.State70.Read(0),"Agent target-request state2");Eq(0u,f.Movement.Agent.RequestE8.Generation00.Read(0),"No simulated frame update");Eq((byte)0,f.Movement.Agent.RequestE8.Complete50.Read(0),"No simulated request completion");Unknown(f.Request.MouseController.Byte432,"No processed-click success on failed transition");Eq(0,f.Request.Lock.OwnOutstandingEnters,"Object locks released");Check(!f.Movement.Scene.Lock.IsHeldByCurrentThread,"Ground lock released");
 }
 public static void Run(){
  scenarios=assertions=PriorFixtureSetupChecks=0;NormalStopRva=NullResourceStopRva=GroundStopRva=GroundPathResult=CorridorCount=0;
  var scene=new NavigationSearchWrapperTests.Scene();PriorFixtureSetupChecks+=4;
  Test("Same saved-map request carries actual cold and warm resource identity through Player to RunningSound",delegate{
   var f=new Fixture(scene,false);ResourceCreated(f);f.Ready();
   var other=new GameAnimationEntryState();other.Virtual00.Write(0x1806d4d48UL);other.Resource10.Write(null);f.SetFields.Entry(6).Write(other);var warm=new GameAnimationResourceBindingInvocation();
   Eq((byte)1,GameAnimationResourceBinding.Add024D80(f.Resources,Field<AnimationSet>.Of(f.Set),6,Field<EngineResourceStringState>.Of(Name()),Field<float>.Of(1),Field<float>.Of(0),warm),"Same manager actual warm Add");Same(f.Resource,other.Resource10.Read(0),"Second entry borrows same resource");Check(!warm.Load.Cold&&warm.Load.Construction==null,"No duplicate factory");Eq(2u,warm.Load.FirstPair.Count00.Read(0),"Shared reference count incremented");Eq(1,f.ResourceManager.Cache40.Read(0).Count,"One canonical name");
   Missing(0x05cdc0,delegate{f.Run();},"Next unimplemented direct dependency is RunningSound");NormalStopRva=0x05cdc0;SubmittedPrefix(f,NormalStopRva);var p=f.Play;
   Check(p.Returned&&p.ReturnValue==1&&p.HandStateReturned&&p.SyncedReturned,"Actual Player/entry return completed");Same(f.Resource,p.ResourceCapture.Read(0),"Factory result directly reaches Player");Same(f.Resource,f.Channel.Resource10.Read(0),"Same factory object reaches mesh channel");Same(f.Actor,p.SyncedCall.Actor,"Same Actor+78/+80");Same(f.Request.Objects,p.HandCall.LeftManager,"Left uses original movement manager map");Same(f.Request.Objects,p.HandCall.RightManager,"Right independently uses same map");Eq(4,p.HandCall.LeftLookup.HashedBytes,"LeftID0 still hashed");Eq(4,p.HandCall.RightLookup.HashedBytes,"RightID0 still hashed");Eq(5u,f.Set.Kind08.Read(0),"Normal request publishes Kind5");Eq(0u,f.SetFields.Pending0C.Read(0),"Normal request clears Pending");Eq(100f,f.Channel.Speed20.Read(0),"Actual run speed reaches channel");Eq(4u,f.Resource.Dword74.Read(0),"Request return did not mark resource decoded");
   Eq(2,f.Handle.SkillIdCalls.Count,"SetAttackTarget and post-Play each call real531C50");Eq(0x531d48u,f.Handle.SkillIdCalls[1].At,"Post-Play empty skill scan returned0");Eq(3,f.Handle.ActionLookups.Count,"Post-PlayID0 still performs real object lookup");var last=f.Handle.ActionLookups[2];Check(last.Returned&&last.OwnLockExited,"Post-Play lookup really returned");Eq(4,last.HashedBytes,"Post-Play key hashed");Same(null,last.Result.Read(0),"Actual post-Play map miss");Eq(3,f.Handle.Animation.StateCalls.Count,"Post-Play state5 reread reached RunningSound");Eq(5u,f.Handle.Animation.StateCalls[2].Value.Value,"Actual state reread");
  });
  Test("Resource creation does not invent the unrelated entry blend constructor field",delegate{
   var f=new Fixture(scene,false);ResourceCreated(f);f.Channel.Resource10.Write(null);Missing(0x020870,delegate{f.Run();},"Add does not initialize Float18");SubmittedPrefix(f,0x020870);Unknown(f.Entry.Float18,"Blend remains Unknown");Check(f.Play.ResourceCaptured&&!f.Play.PlayerEntered&&!f.Play.Returned,"Actual resource pointer reaches conversion before stop");Same(f.Resource,f.Play.ResourceCapture.Read(0),"Factory resource retained");Same(null,f.Channel.Resource10.Read(0),"No channel write yet");Eq(5u,f.SetFields.Pending0C.Read(0),"Pending prefix remains");Eq(0u,f.Set.Kind08.Read(0),"Current kind unchanged");
  });
  Test("Ready resource and entry do not default the original mesh channel to empty",delegate{
   var f=new Fixture(scene,false);ResourceCreated(f);f.Entry.Float18.Write(3);Missing(0x0385e9,delegate{f.Run();},"Unobserved old channel resource stops actual publication");SubmittedPrefix(f,0x0385e9);Check(f.Play.ChannelReturned&&!f.Play.AnimationSubmitted&&!f.Play.HandStateEntered&&!f.Play.Returned,"Real channel getter returned but no publication success");Unknown(f.Channel.Resource10,"No native-empty constructor guessed");Eq(5u,f.SetFields.Pending0C.Read(0),"Failed Player retains pending");Eq(0u,f.Set.Kind08.Read(0),"Failed Player cannot publish current kind");
  });
  Test("Actual null-manager Add and null-resource Play enter real StopManager boundary",delegate{
   var f=new Fixture(scene,true);Check(f.Add.Returned&&f.Add.ReturnValue==0&&!f.Add.ResourcePublished&&f.Add.Load==null,"Actual Add normal false");Same(null,f.Entry.Resource10.Read(0),"Entry retains null");Missing(0x08ed00,delegate{f.Run();},"No fabricated StopManager response");NullResourceStopRva=0x08ed00;SubmittedPrefix(f,NullResourceStopRva);Check(f.Play.Returned&&f.Play.ReturnValue==0&&!f.Play.PlayerEntered&&!f.Play.AnimationSubmitted,"Play false is distinct from request success");Eq(2,f.Handle.Animation.StateCalls.Count,"No later State1 write after unknown stop");Eq(0u,f.Set.Kind08.Read(0),"False leaves current kind");Eq(5u,f.SetFields.Pending0C.Read(0),"False leaves pending");Unknown(f.Entry.Float18,"Null resource bypasses unknown blend");Unknown(f.Channel.Resource10,"No channel fabricated on false");Eq(0,f.ResourceManager.Cache40.Read(0).Count,"Unused manager has no created resource");
  });
  Test("Same composition on real Ground start preserves nearest miss and camera retry stop",delegate{
   var f=new Fixture(scene,false);ResourceCreated(f);f.Ready();var start=f.Request.Owner.Entity.CoordsD8.Position00;Eq((byte)1,WorldFloorContracts.PutOnFloor22C620(start,f.Request.World,scene.Floor,new Invocation()),"Actual saved Ground provides start height");uint y=start.DC.Read(0);Check(y!=f.Movement.InitialY,"Ground start is distinct from declared navigation-center start");Missing(0x1636d7,delegate{f.Run();},"No hidden navigation-height fallback");GroundStopRva=0x1636d7;var request=f.Request.Boundaries.LastRequest;GroundPathResult=request.Path.Result;Eq(4u,GroundPathResult,"Actual nearest start misses");var nav=request.Path.Precheck.Navigation;Check(nav.FirstNearestReturned&&!nav.SecondNearestReturned,"First nearest returned before second");Eq(0u,nav.StartReference.Read(0),"Actual first nearest reference is zero");Check(request.PathReturned&&request.Path.Precheck.NavigationReturned&&!request.TransitionEntered&&!request.Returned,"Failure occurs before MoveBegin");Check(!request.NamePrepared&&!request.TransitionPayloadPrepared,"No successful transition payload fabricated");Eq(y,start.DC.Read(0),"Actual Ground height retained");Same(null,f.Movement.Composition.Handler.Current08.Read(0),"No action published");Eq((byte)0,f.Request.Manager.Moving38.Read(0),"No movement started");Unknown(f.SetFields.Pending0C,"No Play invocation");Same(null,f.Channel.Resource10.Read(0),"Resource exists but no channel submission");Eq(4u,f.Resource.Dword74.Read(0),"Resource remains constructed");Eq(0,f.Request.Lock.OwnOutstandingEnters,"Object lock released");Check(!scene.Lock.IsHeldByCurrentThread,"Ground lock released");
  });
 }
}
