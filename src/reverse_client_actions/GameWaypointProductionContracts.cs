using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // OWN lifetime/storage sidecar, unique for the existing MovementManager.
 // Binding does not initialize the queue, owner, moving byte or positions.
 public sealed class GameWaypointManagerState {
  static readonly ConditionalWeakTable<MovementManager,GameWaypointManagerState> states=new ConditionalWeakTable<MovementManager,GameWaypointManagerState>();
  static readonly object gate=new object();
  public readonly GameWaypointProductionContext Context;
  public readonly WaypointManagerView View;
  readonly HashSet<WaypointMap> maps=new HashSet<WaypointMap>();
  readonly HashSet<Waypoint80> blocks=new HashSet<Waypoint80>();
  public bool OwnEmptyQueueInitialized{get;private set;}
  public int OwnLiveMaps{get{return maps.Count;}}public int OwnLiveBlocks{get{return blocks.Count;}}
  GameWaypointManagerState(MovementManager manager,GameWaypointProductionContext context){Context=context;View=new WaypointManagerView(manager);}
  internal static GameWaypointManagerState Bind(MovementManager manager,GameWaypointProductionContext context){
   if(manager==null||context==null)throw new ArgumentNullException("existing waypoint binding");
   lock(gate){GameWaypointManagerState prior;if(states.TryGetValue(manager,out prior)){if(!Object.ReferenceEquals(prior.Context,context))throw new InvalidOperationException("MovementManager already belongs to another waypoint composition");return prior;}var state=new GameWaypointManagerState(manager,context);states.Add(manager,state);return state;}
  }
  internal static GameWaypointManagerState ForBound(MovementManager manager,GameWaypointProductionContext context,uint at){GameWaypointManagerState state;if(manager==null||!states.TryGetValue(manager,out state)||!Object.ReferenceEquals(state.Context,context))throw new MissingContract(at,"MovementManager requires its original explicit waypoint binding");return state;}
  // Explicit fresh OWN queue choice, not recovered native constructor/AddAgent.
  // One-shot setup cannot erase a live queue; unrelated fields stay Unknown.
  static void RequireUnknown<T>(Field<T> field){try{field.Read(0);}catch(MissingContract){return;}throw new InvalidOperationException("OWN empty queue setup cannot overwrite an observed header");}
  public void InitializeOwnEmptyQueue(){
   if(OwnEmptyQueueInitialized)throw new InvalidOperationException("OWN queue has already been initialized");
   if(maps.Count!=0||blocks.Count!=0)throw new InvalidOperationException("OWN queue already owns allocations");
   var q=View.Queue40;RequireUnknown(q.Proxy00);RequireUnknown(q.Map08);RequireUnknown(q.MapSize10);RequireUnknown(q.Offset18);RequireUnknown(q.Count20);
   var proxy=new WaypointProxy();proxy.Container00.Write(q);q.Proxy00.Write(proxy);q.Map08.Write(null);q.MapSize10.Write(0);q.Offset18.Write(0);q.Count20.Write(0);OwnEmptyQueueInitialized=true;
  }
  // Bounded managed allocation/release only. Cells begin Unknown; the actual
  // Grow memmove/memset initializes them. Native heap/EH is not emulated.
  internal WaypointMap AllocateMap(ulong slots,uint stride){if(stride!=8||slots==0||slots>256)throw new MissingContract(0x0162d0,"OWN waypoint allocation domain is 1..256 eight-byte slots");var map=new WaypointMap(slots);maps.Add(map);return map;}
  internal Waypoint80 AllocateBlock(){var block=new Waypoint80();blocks.Add(block);return block;}
  internal void ReleaseBlock(Waypoint80 block){if(block==null||!blocks.Remove(block))throw new MissingContract(0x5da6bc,"Waypoint block is not a live allocation owned by this queue");}
  internal void ReleaseMap(WaypointMap map,ulong slots,uint at){if(map==null||map.Released||map.Slots!=slots||!maps.Remove(map))throw new MissingContract(at,"Waypoint map is not a matching live allocation owned by this queue");map.Released=true;}
 }
 // NavManager+08 search and +10 crowd remain parts of one canonical manager.
 // Creating this sidecar publishes neither +10 nor any Crowd/Agent registration.
 public sealed class GameWaypointNavigationState {
  static readonly ConditionalWeakTable<NavigationSearchManagerView,GameWaypointNavigationState> states=new ConditionalWeakTable<NavigationSearchManagerView,GameWaypointNavigationState>();
  static readonly object gate=new object();
  public readonly NavigationSearchManagerView Identity;
  public readonly GameWaypointProductionContext Context;
  public readonly Field<NavigationCrowdAdapterState> Outer10=new Field<NavigationCrowdAdapterState>();
  public readonly NavigationMoveManagerView MoveView;
  GameWaypointNavigationState(NavigationSearchManagerView identity,GameWaypointProductionContext context){Identity=identity;Context=context;MoveView=new NavigationMoveManagerView(Outer10);}
  internal static GameWaypointNavigationState Bind(NavigationSearchManagerView identity,GameWaypointProductionContext context){
   if(identity==null||context==null)throw new ArgumentNullException("existing navigation binding");
   lock(gate){GameWaypointNavigationState prior;if(states.TryGetValue(identity,out prior)){if(!Object.ReferenceEquals(prior.Context,context))throw new InvalidOperationException("NavManager already belongs to another waypoint composition");return prior;}var state=new GameWaypointNavigationState(identity,context);states.Add(identity,state);return state;}
  }
  internal static GameWaypointNavigationState ForBound(NavigationSearchManagerView identity,GameWaypointProductionContext context,uint at){GameWaypointNavigationState state;if(identity==null||!states.TryGetValue(identity,out state)||!Object.ReferenceEquals(state.Context,context))throw new MissingContract(at,"Returned canonical NavManager lacks its explicit shared +10 binding");return state;}
 }
 public sealed class GameWaypointProductionContext {
  public readonly GamePathPrecheckContext Path;
  public readonly NavigationMoveObjectContext Movement;
  readonly Dictionary<WorldRegion,ulong> regionTokens=new Dictionary<WorldRegion,ulong>();
  public GameWaypointProductionContext(GamePathPrecheckContext path,NavigationMoveObjectContext movement){
   if(path==null||movement==null)throw new ArgumentNullException("existing waypoint composition");
   if(!Object.ReferenceEquals(path.Bridge.Callbacks,movement.Addresses.Game)||!Object.ReferenceEquals(path.Navigation.World,movement.Addresses.Game.World)||!Object.ReferenceEquals(path.Navigation.World.EngineGlobal,path.Movement.EngineGlobal))throw new ArgumentException("Waypoint composition must borrow the same Bridge/Game, World and Engine field");Path=path;Movement=movement;
  }
  public GameWaypointManagerState BindManager(MovementManager manager){return GameWaypointManagerState.Bind(manager,this);}
  public GameWaypointNavigationState BindNavigation(NavigationSearchManagerView navigation){return GameWaypointNavigationState.Bind(navigation,this);}
  internal object Region(WorldPosition position,uint at){
   // The frozen object-return interface cannot carry unknown pointer bits.
   // This OWN adapter resolves them at the caller; it does not claim the raw
   // native22C0B0 MOV/RET prefix for unknown or unattached Region tokens.
   if(position==null)throw new MissingContract(at,"Null OWN Region input");ulong token=position.Q0.Read(at);if(token==0)return null;
   var region=Path.Navigation.World.State.Region(token,at);ulong prior;if(regionTokens.TryGetValue(region,out prior)&&prior!=token)throw new MissingContract(at,"Canonical Region changed its explicit token binding");regionTokens[region]=token;return region;
  }
  internal ulong Token(object region,uint at){if(region==null)return 0;var value=region as WorldRegion;ulong token;if(value==null||!regionTokens.TryGetValue(value,out token)||!Object.ReferenceEquals(Path.Navigation.World.State.Region(token,at),value))throw new MissingContract(at,"Region has no matching previously resolved canonical token");return token;}
 }
 public sealed class GameWaypointProductionInvocation {
  public readonly Invocation Native=new Invocation();
  public readonly List<Invocation> CoordinateCalls=new List<Invocation>(),FloorCalls=new List<Invocation>(),WorldVectorCalls=new List<Invocation>(),FromWorldCalls=new List<Invocation>(),NavigationCalls=new List<Invocation>(),ConstructorCalls=new List<Invocation>();
  public readonly List<EntityCoordinates64> CoordinateBuffers=new List<EntityCoordinates64>(),CoordinateReturns=new List<EntityCoordinates64>();
  public readonly List<WorldPosition> CoordinateWordBuffers=new List<WorldPosition>();
  public readonly List<byte> FloorResults=new List<byte>(),FromWorldResults=new List<byte>();
  public readonly List<NavigationMoveObjectInvocation> MoveObjectCalls=new List<NavigationMoveObjectInvocation>();
  public readonly List<GameMovementCharacterState> Owners=new List<GameMovementCharacterState>();
  public readonly List<NavigationSearchManagerView> NavigationReturns=new List<NavigationSearchManagerView>();
  public bool Returned;public byte Result;
  internal void Begin(){Native.At=0;CoordinateCalls.Clear();FloorCalls.Clear();WorldVectorCalls.Clear();FromWorldCalls.Clear();NavigationCalls.Clear();ConstructorCalls.Clear();CoordinateBuffers.Clear();CoordinateReturns.Clear();CoordinateWordBuffers.Clear();FloorResults.Clear();FromWorldResults.Clear();MoveObjectCalls.Clear();Owners.Clear();NavigationReturns.Clear();Returned=false;Result=0;}
 }
 // Connects existing saved08EAF0/073F70/08F4C0/08D9E0 bodies to actual
 //07F070,228AB0,22C1A0,22C690,22C620,01C510 and151130/25B300 contracts.
 // Normal no-portal route only; stable single-thread typed storage and OWN
 // allocation. Registry/Agent initialization is supplied explicitly elsewhere,
 // never inferred here. Agent target submission is not a movement frame.
 // Destination storage must not alias a block owned/released by this queue;
 // the frozen80-byte block view has no stale-pointer lifetime representation.
 public static class GameWaypointProductionContracts {
  static NavigationPoint3 Point(Displacement3 value){return value==null?null:new NavigationPoint3(value.X0,value.Y4,value.Z8);}
  static WorldPosition24 Position(WorldPosition value){return value==null?null:WorldPositionWordBridge.FromWords(value).Dwords;}
  sealed class ProductionBoundaries:WaypointBoundaries {
   readonly GameWaypointProductionContext owner;readonly GameWaypointManagerState state;readonly GameWaypointProductionInvocation call;
   readonly Dictionary<WorldPosition,EntityCoordinates64> frames=new Dictionary<WorldPosition,EntityCoordinates64>();
   public ProductionBoundaries(GameWaypointProductionContext owner,GameWaypointManagerState state,GameWaypointProductionInvocation call):base(owner.Path.Movement){this.owner=owner;this.state=state;this.call=call;Attach(state.View);}
   public override WorldPosition OwnerCoords(object instance,WorldPosition hidden){
    var role=instance as MoveCharacter;if(role==null)throw new MissingContract(0x07f076,"Waypoint owner lacks original MoveCharacter identity");if(hidden==null)throw new MissingContract(call.Native.At,"Null OWN coordinate output");
    EntityCoordinates64 frame;if(!frames.TryGetValue(hidden,out frame)){var basis=new Field<float>[9];for(int n=0;n<9;n++)basis[n]=new Field<float>();frame=new EntityCoordinates64(WorldPositionWordBridge.FromWords(hidden,hidden).Dwords,basis,new Field<uint>());frames.Add(hidden,frame);}
    var i=new Invocation();call.CoordinateCalls.Add(i);call.CoordinateBuffers.Add(frame);call.CoordinateWordBuffers.Add(hidden);var character=owner.Path.Bridge.ForMoveCharacter(role,0x07f076).Owner;call.Owners.Add(character);
    var result=EntityMovementContracts.GetCoords07F070(character.Entity,frame,i);call.CoordinateReturns.Add(result);if(!Object.ReferenceEquals(result,frame))throw new MissingContract(call.Native.At,"GetCoords returned an unsupported distinct hidden address");return hidden;
   }
   public override object Region(WorldPosition position){return owner.Region(position,call.Native.At);}
   public override void InitializePosition609720(WorldPosition position){var i=new Invocation();call.ConstructorCalls.Add(i);GameMovedContracts.ConstructWorldVec228AB0(Position(position),i);}
   public override void PutOnFloor609BB0(WorldPosition position){var i=new Invocation();call.FloorCalls.Add(i);call.FloorResults.Add(WorldFloorContracts.PutOnFloor22C620(Position(position),owner.Path.Navigation.World,owner.Path.Navigation.Floor,i));}
   public override Displacement3 WorldVector609D98(WorldPosition position,Displacement3 output){var i=new Invocation();call.WorldVectorCalls.Add(i);var point=Point(output);var returned=EntityMovementContracts.GetWorldPosition22C1A0(Position(position),point,owner.Path.Navigation.World.State,i);if(!Object.ReferenceEquals(point,returned))throw new MissingContract(call.Native.At,"World-vector getter returned an unsupported distinct hidden address");return output;}
   public override object Navigation(){var i=new Invocation();call.NavigationCalls.Add(i);var nav=NavigationSearchSingletonContracts.Get01C510(owner.Path.Globals,i);call.NavigationReturns.Add(nav);return nav;}
   public override byte MoveObject609F08(object navigation,object instance,Displacement3 vector){
    // The frozen151130 API takes a resolved typed owner. Establish that OWN
    // composition domain here, before entering it. For an unbound owner/nav
    // we do not claim native +10/+08 load ordering or a callee prefix.
    var nav=navigation as NavigationSearchManagerView;var bound=GameWaypointNavigationState.ForBound(nav,owner,0x151130);var role=instance as MoveCharacter;if(role==null)throw new MissingContract(0x151149,"MoveObject requires original MoveCharacter owner");var character=owner.Path.Bridge.ForMoveCharacter(role,0x151149).Owner;call.Owners.Add(character);
    var i=new NavigationMoveObjectInvocation();call.MoveObjectCalls.Add(i);return NavigationMoveObjectContracts.Move151130(bound.MoveView,character,Point(vector),owner.Movement,i);
   }
   // Caller ignores both return bytes. Record them without promoting zero to
   // failure/success. A callee MissingContract still escapes with its prefix.
   public override void FromWorld609DC0(WorldPosition output,Displacement3 vector,object region){var i=new Invocation();call.FromWorldCalls.Add(i);ulong token=owner.Token(region,call.Native.At);call.FromWorldResults.Add(WorldPositionInitializationContracts.SetFromWorldPosition22C690(Position(output),Point(vector),token,owner.Path.Navigation.World,i));}
   public override WaypointMap AllocateMap0162D0(ulong slots,uint stride){return state.AllocateMap(slots,stride);}
   public override void ReleaseMap016620(WaypointMap map,ulong slots,uint stride){if(stride!=8)throw new MissingContract(0x016620,"Unsupported OWN map stride");state.ReleaseMap(map,slots,0x016620);}
   public override Waypoint80 Allocate80(){return state.AllocateBlock();}
   public override void FreeBlock(Waypoint80 block){state.ReleaseBlock(block);}
   public override void FreeMap(WaypointMap map,ulong slots){state.ReleaseMap(map,slots,0x5da6bc);}
   // Cross-Region portal selection/production/execution inherit Unsupported.
  }
  public static byte Move08EAF0(MovementManager manager,WorldPosition destination,GameWaypointProductionContext context,GameWaypointProductionInvocation invocation){
   if(invocation==null)throw new MissingContract(0x08eaf0,"Missing waypoint invocation");invocation.Begin();var i=invocation.Native;
   try{if(context==null)throw new MissingContract(0x08eaf0,"Missing explicit waypoint composition");var state=GameWaypointManagerState.ForBound(manager,context,0x08eaf0);byte result=WaypointContracts.Move(state.View,destination,new ProductionBoundaries(context,state,invocation),i);invocation.Result=result;invocation.Returned=true;return result;}catch(MissingContract error){i.At=error.Address;throw;}
  }
 }
}
