using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // This cell belongs to the already registered Engine, not GameEngine A66288
 // and not the earlier logging/permission-model globals. Binding writes no byte.
 public sealed class EngineNetworkState {
  static readonly ConditionalWeakTable<WorldEngine,EngineNetworkState> states=new ConditionalWeakTable<WorldEngine,EngineNetworkState>();
  public readonly EntityEngineState Owner;
  public readonly Field<byte> NetworkFlag6A8=new Field<byte>();
  EngineNetworkState(EntityEngineState owner){Owner=owner;}
  public static EngineNetworkState Bind(EntityEngineState owner){
   if(owner==null)throw new ArgumentNullException("engine owner");owner.RequireLive(0);
   lock(states){EngineNetworkState prior;if(states.TryGetValue(owner.Identity,out prior)){if(!Object.ReferenceEquals(prior.Owner,owner))throw new InvalidOperationException("Engine6A8 already belongs to another canonical owner");return prior;}var state=new EngineNetworkState(owner);states.Add(owner.Identity,state);return state;}
  }
  public static EngineNetworkState ForBound(WorldEngine engine,EntityMovementRegistry registry,uint at){
   EngineNetworkState state;if(engine==null||registry==null||!states.TryGetValue(engine,out state))throw new MissingContract(at,"Engine6A8 requires an explicit existing-engine binding");
   if(!Object.ReferenceEquals(state.Owner,registry.Engine(engine,at)))throw new MissingContract(at,"Engine6A8 and movement registry have different canonical owners");state.Owner.RequireLive(at);return state;
  }
 }
 public sealed class GameControllerMoveProductionContext {
  public readonly PlayerMousePlayerLookupContext Lookup;
  public readonly GameReachedGoalContext ReachedGoal;
  public readonly PlayerAliveContext Alive;
  // Actual A5C9F0 identity remains unbound until explicitly supplied. A real
  // ObjectManager miss returns before this fixed-address symbol is consumed.
  public readonly AddressSymbol<ClassRecord> ControllerBaseCharacterClass=new AddressSymbol<ClassRecord>(0xa5c9f0);
  // Dependency points outward through the existing base type, never back to
  // ControllerLookup.dll. Binding initializes no native object or class graph.
  public GameAssociatedLookupBoundaries ControllerAssociation{get;private set;}
  public void BindControllerAssociation(GameAssociatedLookupBoundaries association){
   if(association==null)throw new ArgumentNullException("association");
   if(!Object.ReferenceEquals(association.BoundMouseContext,Lookup.Mouse)||!Object.ReferenceEquals(association.ControllerBaseCharacterClass,ControllerBaseCharacterClass))throw new ArgumentException("Controller association must borrow the original mouse context and A5C9F0 symbol");
   if(ControllerAssociation!=null&&!Object.ReferenceEquals(ControllerAssociation,association))throw new InvalidOperationException("Controller association cannot be rebound");ControllerAssociation=association;
  }
  // Optional borrowed composition only. Binding initializes no role/global,
  // skill offset or Region; the old unbound interfaces keep their stops.
  public GamePathPrecheckContext BeginPath{get;private set;}
  public void BindBeginPath(GamePathPrecheckContext path){
   if(path==null)throw new ArgumentNullException("path");
   if(!Object.ReferenceEquals(path.Bridge,Lookup.Bridge)||!Object.ReferenceEquals(path.Movement,Lookup.Mouse.Movement.Context)||!Object.ReferenceEquals(path.Navigation.World,Lookup.Bridge.Callbacks.World)||!Object.ReferenceEquals(path.Navigation.World.EngineGlobal,EngineGlobal))throw new ArgumentException("Begin must borrow the original bridge, MoveContext, World state and Engine field");
   if(BeginPath!=null&&!Object.ReferenceEquals(BeginPath,path))throw new InvalidOperationException("Begin path cannot be rebound");BeginPath=path;
  }
  public GameWaypointProductionContext WaypointProduction{get;private set;}
  public void BindWaypoints(GameWaypointProductionContext waypoints){
   if(waypoints==null)throw new ArgumentNullException("waypoints");if(BeginPath==null||!Object.ReferenceEquals(BeginPath,waypoints.Path))throw new ArgumentException("Waypoint production must borrow the already bound Begin path");
   if(WaypointProduction!=null&&!Object.ReferenceEquals(WaypointProduction,waypoints))throw new InvalidOperationException("Waypoint production cannot be rebound");WaypointProduction=waypoints;
  }
  public Field<WorldEngine> EngineGlobal{get{return Lookup.Mouse.Movement.Context.EngineGlobal;}}
  // Explicit opt-in; no resource loading or pointer defaults occur here.
  public GameMoveAnimationProductionContext AnimationProduction{get;private set;}
  public void BindAnimations(GameMoveAnimationProductionContext animation){
   if(animation==null)throw new ArgumentNullException("animation");
   if(WaypointProduction==null||!Object.ReferenceEquals(animation.Bridge,Lookup.Bridge))throw new ArgumentException("Animation must borrow the original Bridge and an already opted-in waypoint path");
   if(AnimationProduction!=null&&!Object.ReferenceEquals(AnimationProduction,animation))throw new InvalidOperationException("Animation composition cannot be rebound");animation.BindLookup(Lookup);AnimationProduction=animation;
  }
  public GameRunningSoundContext RunningSound{get;private set;}
  public void BindRunningSound(GameRunningSoundContext sound){
   if(sound==null)throw new ArgumentNullException("sound");if(!Object.ReferenceEquals(sound.Bridge,Lookup.Bridge))throw new ArgumentException("Running sound must borrow the original identity bridge");
   if(RunningSound!=null&&!Object.ReferenceEquals(RunningSound,sound))throw new InvalidOperationException("Running-sound composition cannot be rebound");RunningSound=sound;
  }
  public GameControllerMoveProductionContext(PlayerMousePlayerLookupContext lookup,GameReachedGoalContext reachedGoal){
   if(lookup==null||reachedGoal==null)throw new ArgumentNullException("production composition");
   if(!Object.ReferenceEquals(lookup.Bridge,reachedGoal.Bridge)||!Object.ReferenceEquals(lookup.Engine,reachedGoal.Engine)||!Object.ReferenceEquals(lookup.Mouse.Movement.Context,reachedGoal.Move))throw new ArgumentException("Move production must borrow the original lookup, bridge, object manager and MoveContext");Lookup=lookup;ReachedGoal=reachedGoal;Alive=new PlayerAliveContext(lookup);
  }
 }
 public sealed class GameControllerHandleActionInvocation {
  public readonly Invocation Native=new Invocation();
  public readonly Invocation BeginNative=new Invocation(),StopNative=new Invocation(),NetworkSupportNative=new Invocation();
  public Field<WorldEngine> CapturedEngine{get;private set;}
  public Field<WorldEngine> FreshEngine{get;private set;}
  public readonly List<ControllerAiLookupInvocation> AiLookups=new List<ControllerAiLookupInvocation>();
  public readonly List<GameObjectManagerLookupInvocation> ActionLookups=new List<GameObjectManagerLookupInvocation>();
  public readonly List<Invocation> KindCalls=new List<Invocation>();
  public readonly List<PlayerAliveInvocation> AliveCalls=new List<PlayerAliveInvocation>();
  public readonly List<Field<object>> CapturedAliveTables=new List<Field<object>>();
  public readonly List<Invocation> CoordinateCalls=new List<Invocation>();
  public readonly List<EntityCoordinates64> CoordinateBuffers=new List<EntityCoordinates64>(),CoordinateReturns=new List<EntityCoordinates64>();
  public readonly List<WorldPosition> CoordinateWordBuffers=new List<WorldPosition>();
  public readonly List<ulong> RegionNullTestTokens=new List<ulong>();
  public readonly List<WorldDistanceInvocation> DistanceCalls=new List<WorldDistanceInvocation>();
  public readonly List<GameAssociatedLookupInvocation> AssociatedCalls=new List<GameAssociatedLookupInvocation>();
  public readonly List<GameWaypointProductionInvocation> WaypointCalls=new List<GameWaypointProductionInvocation>();
  public readonly List<GameRunningSoundInvocation> RunningSoundCalls=new List<GameRunningSoundInvocation>();
  public readonly GameMoveAnimationProductionInvocation Animation=new GameMoveAnimationProductionInvocation();
  public readonly List<Field<float>> DistanceResults=new List<Field<float>>();
  public readonly List<Invocation> SkillIdCalls=new List<Invocation>(),AlreadyThereCalls=new List<Invocation>(),DifferenceCalls=new List<Invocation>();
  public byte? NetworkFlag,NetworkSupport;
  public bool PredicateReturned,NetworkSupportReturned,FreshEngineCaptured,NetworkShimEntered,LocalEntered,BeginEntered,BeginReturned,StopEntered,StopReturned,Returned;
  public GameControllerHandleActionInvocation(){Begin();}
  internal void Begin(){Native.At=BeginNative.At=StopNative.At=NetworkSupportNative.At=0;CapturedEngine=new Field<WorldEngine>();FreshEngine=new Field<WorldEngine>();AiLookups.Clear();ActionLookups.Clear();KindCalls.Clear();AliveCalls.Clear();CapturedAliveTables.Clear();CoordinateCalls.Clear();CoordinateBuffers.Clear();CoordinateReturns.Clear();CoordinateWordBuffers.Clear();RegionNullTestTokens.Clear();DistanceCalls.Clear();AssociatedCalls.Clear();WaypointCalls.Clear();RunningSoundCalls.Clear();Animation.Reset();DistanceResults.Clear();SkillIdCalls.Clear();AlreadyThereCalls.Clear();DifferenceCalls.Clear();NetworkFlag=NetworkSupport=null;PredicateReturned=NetworkSupportReturned=FreshEngineCaptured=NetworkShimEntered=LocalEntered=BeginEntered=BeginReturned=StopEntered=StopReturned=Returned=false;}
  internal void Capture(Field<WorldEngine> source){CapturedEngine=source.Snapshot();}
  internal void CaptureFresh(Field<WorldEngine> source){FreshEngine=source.Snapshot();FreshEngineCaptured=true;}
 }
 public sealed class GameControllerMoveProductionInvocation {
  public readonly Invocation Native=new Invocation();
  public ControllerAiLookupInvocation FirstAI{get;private set;}
  public ControllerAiLookupInvocation SecondAI{get;private set;}
  public SkillReferenceNumberInvocation Skill{get;private set;}
  public GameMoveActionConstructionInvocation Construction{get;private set;}
  public GameControllerHandleActionInvocation Handle{get;private set;}
  public MoveActionRecord AllocatedAction,ConstructorResult;
  public GameReachedGoalActionView Primary;
  public GameMoveActionConstructionState ConstructionState;
  public EntityCoordinates64 BorrowedCoordinates;
  public WorldPosition24 LocalStart{get;private set;}
  public WorldPositionWordBridge LocalStartWords,DestinationWords;
  public Field<uint> CapturedFlags,CapturedSkillId,CapturedAnimation,CapturedOwnerId;
  public Field<float> CapturedSpeed;
  public readonly Field<uint> SkillReference=new Field<uint>();
  public bool AllocationReturned,FirstAIReturned,SkillReturned,SecondAIReturned,CoordinatesReturned,StartCopied,ConstructorReturned,HandleEntered,Returned;
  public GameControllerMoveProductionInvocation(){Begin();}
  internal void Begin(){
   Native.At=0;FirstAI=new ControllerAiLookupInvocation();SecondAI=new ControllerAiLookupInvocation();Skill=new SkillReferenceNumberInvocation();Construction=new GameMoveActionConstructionInvocation();Handle=new GameControllerHandleActionInvocation();
   AllocatedAction=ConstructorResult=null;Primary=null;ConstructionState=null;BorrowedCoordinates=null;LocalStart=new WorldPosition24();LocalStartWords=DestinationWords=null;
   CapturedFlags=CapturedSkillId=CapturedAnimation=CapturedOwnerId=null;CapturedSpeed=null;SkillReference.Forget();AllocationReturned=FirstAIReturned=SkillReturned=SecondAIReturned=CoordinatesReturned=StartCopied=ConstructorReturned=HandleEntered=Returned=false;
  }
 }
 // Saved Game124340/127DD0, Engine078620 and actual024FF0/081860 leaf bodies;
 // the latter use aliased export names, not different action/class identities.
 // shortcut: 5DA6C4 is an explicit normal managed allocation adapter only;
 // the OWN factory tracks its storage for the separately recovered free call.
 // Native allocation failure, heap ABI, unwind and concurrent mutation remain
 // unclosed; no CLR address is passed to a native allocator.
 public static class GameControllerMoveProductionContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Missing own controller-move observation");return value;}
  static uint At(Invocation i,uint at){i.At=at;return at;}
  // No externally supplied callbacks or virtual-kind answers. Each native
  // virtual dispatch reads the same current primary and underlying scalar.
  sealed class ProductionBoundaries:ControllerAiLookupBoundaries {
   readonly GameControllerHandleActionInvocation call;
   readonly GameControllerMoveProductionContext owner;
   readonly Dictionary<WorldPosition,EntityCoordinates64> hiddenFrames=new Dictionary<WorldPosition,EntityCoordinates64>();
   Invocation activeMove;
   public ProductionBoundaries(GameControllerMoveProductionContext context,GameControllerHandleActionInvocation call):base(context.Lookup){owner=context;this.call=call;}
   public override Role GetAI(Controller controller){var c=new ControllerAiLookupInvocation();call.AiLookups.Add(c);return ControllerAiLookupContracts.GetAI078D40(controller,LookupContext,c);}
   public override int Kind58(ActionRecord action){var state=ActionState(action,call.Native.At);var i=new Invocation();call.KindCalls.Add(i);return unchecked((int)GetKind024FF0(state.Action,i));}
   public override object ObjectManager(){return GameReachedGoalEngineContracts.GetObjectManager01C040(LookupContext.Engine,new Invocation());}
   public override object Lookup(object manager,uint id){var m=manager as GameObjectManagerState;if(m==null)throw new MissingContract(0x019d5f,"ObjectManager lacks its shared concrete lookup view");var c=new GameObjectManagerLookupInvocation();call.ActionLookups.Add(c);return GameObjectManagerLookupContracts.Lookup019D40(m,id,c);}
   public override ClassRecord Class0(object instance){
    uint at=activeMove==null?0x082c37u:activeMove.At;var move=instance as MoveCharacter;if(move==null)throw new MissingContract(at,"Action lookup class slot0 target is unbound");
    var view=LookupContext.Bridge.ForMoveCharacter(move,at);var table=view.Owner.Entity.Virtual00.Snapshot();object value=table.Read(at);
    if(Object.ReferenceEquals(value,GameMovementPrimaryTable.Player))return LookupContext.Bridge.Callbacks.PlayerClass.Read(0x016d10);
    if(Object.ReferenceEquals(value,GameMovementPrimaryTable.Character))return LookupContext.Bridge.Callbacks.CharacterClass.Read(0x019d20);
    throw new MissingContract(at,"Action lookup class slot0 requires an observed Player or Character primary");
   }
   public override byte Derived(ClassRecord parent,ClassRecord expected){return (byte)(GameMovementCallbackContracts.IsDerivedFrom591480(parent,expected,new Invocation())?1:0);}
   public override byte CanMove460(MoveCharacter role){
    uint at=activeMove==null?0x082c6au:activeMove.At;uint load=at==0x082c6a?0x082c64u:at;
    var view=LookupContext.Bridge.ForMoveCharacter(role,load);var bits=view.Owner.Entity.Virtual00.Snapshot();call.CapturedAliveTables.Add(bits);object table=bits.Read(at);var c=new PlayerAliveInvocation();call.AliveCalls.Add(c);
    if(Object.ReferenceEquals(table,GameMovementPrimaryTable.Player))return PlayerAliveContracts.PlayerIsAlive3CE7B0(PlayerAliveState.ForBoundRole(LookupContext,role,0x3ce7b6),owner.Alive,c);
    if(Object.ReferenceEquals(table,GameMovementPrimaryTable.Character))return PlayerAliveContracts.CharacterIsAlive05BB80(PlayerAliveState.ForBoundRole(LookupContext,role,0x05bb80),c);
    throw new MissingContract(at,"Unbound current primary for actual Character/Player IsAlive slot460");
   }
   public override WorldPosition Coords(MoveCharacter role,WorldPosition hiddenReturn){
    // Exact IAT609518 is the by-value64-byte GetCoords07F070, not00BBE0.
    // Retain the same native local for both calls in Begin. Its existing24B
    // head cells are borrowed and its40B tail starts Unknown, never zeroed.
    uint at=activeMove==null?0x082c80u:activeMove.At;Need(hiddenReturn,at);EntityCoordinates64 hidden;
    if(!hiddenFrames.TryGetValue(hiddenReturn,out hidden)){var bridge=WorldPositionWordBridge.FromWords(hiddenReturn,hiddenReturn);var basis=new Field<float>[9];for(int n=0;n<basis.Length;n++)basis[n]=new Field<float>();hidden=new EntityCoordinates64(bridge.Dwords,basis,new Field<uint>());hiddenFrames.Add(hiddenReturn,hidden);}
    var i=new Invocation();call.CoordinateCalls.Add(i);call.CoordinateBuffers.Add(hidden);call.CoordinateWordBuffers.Add(hiddenReturn);
    var result=EntityMovementContracts.GetCoords07F070(LookupContext.Bridge.ForMoveCharacter(role,0x07f076).Owner.Entity,hidden,i);call.CoordinateReturns.Add(result);
    if(!Object.ReferenceEquals(result,hidden))throw new MissingContract(at,"GetCoords returned an unsupported different hidden address");return hiddenReturn;
   }
   public override object Region(WorldPosition position){
    // Exact6094A0 ->22C0B0 is MOV RAX,[RCX];RET. These private callers only
    // null-test the result. A boxed nonzero word is NOT a bound Region object
    // or a claim of pointer identity; any future dereference needs its own view.
    ulong token=Need(position,0x22c0b0).Q0.Read(0x22c0b0);call.RegionNullTestTokens.Add(token);return token==0?null:(object)token;
   }
   public override object Associated(object manager,uint id){
    var typed=manager as GameObjectManagerState;if(typed==null)throw new MissingContract(0x019d5f,"Associated lookup requires the original concrete ObjectManager");
    var c=new GameAssociatedLookupInvocation();call.AssociatedCalls.Add(c);
    return GameAssociatedLookupContracts.Lookup077D60(typed,Field<uint>.Of(id),new AssociatedBoundaries(owner),c);
   }
   public override Field<object> CaptureAssociatedPrimary(object associated,uint at){return new AssociatedBoundaries(owner).CapturePrimary(associated,new Invocation{At=at});}
   public override void Associated60Captured(object associated,Field<object> primary,Field<uint> flags,Invocation i){
    object table=Need(primary,i.At).Read(i.At);
    if(!(associated is Controller))throw new MissingContract(i.At,"Associated virtual+60 requires an evidenced concrete target");
    var bound=owner.ControllerAssociation;if(bound==null)throw new MissingContract(i.At,"Controller association is unbound");bound.Slot60(associated,table,flags,i);
   }
   public override void Associated80Captured(object associated,Field<object> primary,Invocation i){
    object table=Need(primary,i.At).Read(i.At);
    if(!(associated is Controller))throw new MissingContract(i.At,"Associated virtual+80 requires an evidenced concrete target");
    var bound=owner.ControllerAssociation;if(bound==null)throw new MissingContract(i.At,"Controller association is unbound");bound.Slot80(associated,table,i);
   }
   sealed class AssociatedBoundaries:GameAssociatedLookupBoundaries {
    readonly GameControllerMoveProductionContext owner;
    public AssociatedBoundaries(GameControllerMoveProductionContext owner):base(owner.ControllerBaseCharacterClass){this.owner=owner;}
    public override Field<object> CapturePrimary(object instance,Invocation i){
     if(instance is Controller){
      var bound=owner.ControllerAssociation;if(bound==null)throw new MissingContract(i.At,"Controller association requires an explicit shared concrete binding");return bound.CapturePrimary(instance,i);
     }
     var role=instance as MoveCharacter;if(role==null)throw new MissingContract(i.At,"Associated primary requires an existing canonical MoveCharacter view");
     var view=owner.Lookup.Bridge.ForMoveCharacter(role,i.At);view.Owner.Entity.RequireLive(i.At);return view.Owner.Entity.Virtual00.Snapshot();
    }
    public override Field<ClassRecord> Class0(object instance,object capturedPrimary,Invocation i){
     if(instance is Controller){
      var bound=owner.ControllerAssociation;if(bound==null)throw new MissingContract(i.At,"Controller association requires an explicit shared concrete binding");return bound.Class0(instance,capturedPrimary,i);
     }
     if(Object.ReferenceEquals(capturedPrimary,GameMovementPrimaryTable.Player)){i.At=0x016d10;return Field<ClassRecord>.Of(owner.Lookup.Bridge.Callbacks.PlayerClass.Read(i.At));}
     if(Object.ReferenceEquals(capturedPrimary,GameMovementPrimaryTable.Character)){i.At=0x019d20;return Field<ClassRecord>.Of(owner.Lookup.Bridge.Callbacks.CharacterClass.Read(i.At));}
     throw new MissingContract(i.At,"Associated class slot0 target is unbound");
    }
   }
   // The prior real Associated077D60 lookup and any nonnull slot60 must return.
   public override float DistanceSquared(object world,WorldPosition first,WorldPosition second){
    var path=owner.BeginPath;if(path==null)return base.DistanceSquared(world,first,second);
    var c=new WorldDistanceInvocation();call.DistanceCalls.Add(c);
    var result=WorldDistanceContracts.GetDistanceSquared224770(first==null?null:WorldPositionWordBridge.FromWords(first).Dwords,second==null?null:WorldPositionWordBridge.FromWords(second).Dwords,path.NullDifference338458,c);call.DistanceResults.Add(result);
    // The legacy callback returns float, so bridge the returned value at its
    // actual next consumer:082CFE COMISS. Unknown may have returned normally
    // from224770; retain that callee result before the caller consumption fails.
    if(activeMove!=null)activeMove.At=0x082cfe;return result.Read(0x082cfe);
   }
   public override uint SkillId(MoveCharacter role,uint reference){
    if(owner.BeginPath==null)return base.SkillId(role,reference);
    var i=new Invocation();call.SkillIdCalls.Add(i);return SkillReferenceContracts.GetSkillId531C50(LookupContext.Bridge,role,reference,i);
   }
   public override byte AlreadyThere(MovementManager manager,WorldPosition position,float tolerance){
    var path=owner.BeginPath;if(path==null)return base.AlreadyThere(manager,position,tolerance);
    var i=new Invocation();call.AlreadyThereCalls.Add(i);return MoveManagerContracts.AlreadyThere(manager,position,tolerance,new AlreadyThereBoundaries(this,path),i);
   }
   public override byte MoveManager(MovementManager manager,WorldPosition position){
    var context=owner.WaypointProduction;if(context==null)return base.MoveManager(manager,position);
    var i=new GameWaypointProductionInvocation();call.WaypointCalls.Add(i);return GameWaypointProductionContracts.Move08EAF0(manager,position,context,i);
   }
   uint AnimationCaller(uint fallback){if(activeMove==null)throw new MissingContract(fallback,"Animation adapter requires the active production Move invocation");return activeMove.At;}
   public override Field<AnimationSet> AnimationsCaptured(MoveCharacter role){
    if(owner.AnimationProduction==null)return base.AnimationsCaptured(role);
    return GameMoveAnimationProductionContracts.GetAnimations(owner.AnimationProduction,role,AnimationCaller(0x05f9fb),call.Animation);
   }
   public override Field<float> AnimationSpeedCaptured(MoveCharacter role,uint index){
    if(owner.AnimationProduction==null)return base.AnimationSpeedCaptured(role,index);
    return GameMoveAnimationProductionContracts.GetSpeed(owner.AnimationProduction,role,index,AnimationCaller(0x05fa08),call.Animation);
   }
   public override byte PlayCaptured(Field<AnimationSet> set,MoveCharacter role,uint animation,object name,float speed,byte loop,uint extra){
    if(owner.AnimationProduction==null)return base.PlayCaptured(set,role,animation,name,speed,loop,extra);
    return GameMoveAnimationProductionContracts.Play(owner.AnimationProduction,role,set,animation,name,speed,loop,extra,AnimationCaller(0x05fa70),call.Animation);
   }
   public override uint State458(MoveCharacter role){
    if(owner.AnimationProduction==null)return base.State458(role);
    return GameMoveAnimationProductionContracts.GetState458(owner.AnimationProduction,role,AnimationCaller(0x082ddf),call.Animation);
   }
   public override void State450(MoveCharacter role,uint state){
    if(owner.AnimationProduction==null){base.State450(role,state);return;}
    GameMoveAnimationProductionContracts.SetState450(owner.AnimationProduction,role,state,AnimationCaller(0x05fa3f),call.Animation);
   }
   public override void RunningSound(MoveCharacter role){
    if(owner.RunningSound==null){base.RunningSound(role);return;}
    if(activeMove==null||activeMove.At!=0x082e06)throw new MissingContract(0x082e06,"Running sound requires the actual MoveBegin caller");
    var c=new GameRunningSoundInvocation();call.RunningSoundCalls.Add(c);
    var original=owner.RunningSound.Bridge.ForMoveCharacter(role,0x05cdca).Owner;GameRunningSoundContracts.PlayLoopingRunningSound05CDC0(original,c);
   }
   sealed class AlreadyThereBoundaries:MoveManagerBoundaries {
    readonly ProductionBoundaries production;readonly GamePathPrecheckContext path;
    public AlreadyThereBoundaries(ProductionBoundaries production,GamePathPrecheckContext path):base(path.Movement){this.production=production;this.path=path;}
    public override WorldPosition OwnerCoords(object owner,WorldPosition hidden){
     var role=owner as MoveCharacter;if(role==null)throw new MissingContract(0x07f076,"MovementManager owner lacks the original MoveCharacter identity");return production.Coords(role,hidden);
    }
    public override void Difference609A48(WorldPosition destination,Displacement3 output,WorldPosition coordinates){
     var i=new Invocation();production.call.DifferenceCalls.Add(i);
     //22C240 resolves the same canonical Region views and reads actual offset
     // cells, even for equal tokens. Never use the null-test boxed Q0 view or
     // skip Unknown offsets by algebraically cancelling equal Regions.
     WorldVectorDifferenceContracts.Subtract22C240(destination==null?null:WorldPositionWordBridge.FromWords(destination).Dwords,output,coordinates==null?null:WorldPositionWordBridge.FromWords(coordinates).Dwords,path.Navigation.World.State,path.NullDifference338458,i);
    }
   }
   public override void Start08(ActionRecord action){
    var state=ActionState(action,0x089b17);call.BeginEntered=true;var prior=activeMove;activeMove=call.BeginNative;
    try{MoveContracts.Begin(state.Action,this,activeMove);call.BeginReturned=true;}catch(MissingContract e){activeMove.At=e.Address;throw;}finally{activeMove=prior;}
   }
   public override void Stop18(ActionRecord action){
    uint at=activeMove==null?0x089c05u:activeMove.At;var state=ActionState(action,at);call.StopEntered=true;var prior=activeMove;activeMove=call.StopNative;
    try{MoveContracts.Stop(state.Action,this,activeMove);call.StopReturned=true;}catch(MissingContract e){activeMove.At=e.Address;throw;}finally{activeMove=prior;}
   }
  }
  static GameMoveActionConstructionState ActionState(ActionRecord action,uint at){
   var move=action as MoveActionRecord;if(move==null)throw new MissingContract(at,"Action requires the original MoveTo allocation");var state=GameMoveActionConstructionState.ForBound(move,at);
   if(!Object.ReferenceEquals(state.Primary.Virtual00.Read(at),GameReachedGoalActionTable.MoveTo))throw new MissingContract(at,"Action virtual target requires the observed MoveTo primary table");return state;
  }
  public static uint GetKind024FF0(MoveActionRecord action,Invocation invocation){
   var i=Need(invocation,0x024ff0);At(i,0x024ff0);var state=GameMoveActionConstructionState.ForBound(Need(action,i.At),i.At);uint result=state.Dword0C.Read(i.At);At(i,0x024ff3);return result;
  }
  public static byte SupportsNetwork081860(MoveActionRecord action,Invocation invocation){
   var i=Need(invocation,0x081860);At(i,0x081860);var state=GameMoveActionConstructionState.ForBound(Need(action,i.At),i.At);byte result=state.Byte10.Read(i.At);At(i,0x081864);return result;
  }
  public static byte IsNetworkEnabled078620(WorldEngine engine,GameControllerMoveProductionContext context,Invocation invocation){
   var i=Need(invocation,0x078620);At(i,0x078620);var ctx=Need(context,i.At);var state=EngineNetworkState.ForBound(engine,ctx.Lookup.Bridge.Callbacks.Entities,i.At);byte result=state.NetworkFlag6A8.Read(i.At);At(i,0x078627);return result;
  }
  public static void HandleAction127DD0(Controller controller,MoveActionRecord action,GameControllerMoveProductionContext context,GameControllerHandleActionInvocation invocation){
   var c=Need(invocation,0x127de0);c.Begin();var i=c.Native;
   try{
    var ctx=Need(context,At(i,0x127de0));At(i,0x127de7);c.Capture(ctx.EngineGlobal);
    At(i,0x127dea);byte network=IsNetworkEnabled078620(c.CapturedEngine.Read(0x078620),ctx,i);c.NetworkFlag=network;c.PredicateReturned=true;
    At(i,0x127df0);if(network!=0){
     At(i,0x127df4);var state=GameMoveActionConstructionState.ForBound(Need(action,i.At),i.At);var primary=state.Primary.Virtual00.Snapshot();
     At(i,0x127dfa);object table=primary.Read(i.At);
     if(!Object.ReferenceEquals(table,GameReachedGoalActionTable.MoveTo))throw new MissingContract(i.At,"Unbound action primary table for network slot50");
     byte supported=SupportsNetwork081860(state.Action,c.NetworkSupportNative);c.NetworkSupport=supported;c.NetworkSupportReturned=true;
     At(i,0x127dfd);if(supported!=0){
      At(i,0x127e01);At(i,0x127e08);c.CaptureFresh(ctx.EngineGlobal);At(i,0x127e0b);c.NetworkShimEntered=true;
      throw new MissingContract(i.At,"Actual GetNetworkShim609BF8 body is unclosed; fresh gEngine bits retained, no shim or send success invented");
     }
    }
    At(i,0x127e2e);c.LocalEntered=true;
    // No second Engine global is allocated. The ordinary kind4/empty-current
    // check returns before the old permission Environment is consumed. Other
    // paths retain its explicit missing-environment stop, never a default.
    Dispatch.Local(controller,action,null,new ProductionBoundaries(ctx,c),i);c.Returned=true;
   }catch(MissingContract e){i.At=e.Address;throw;}
  }
  public static void MoveTo124340(Controller controller,WorldPosition24 destination,Field<uint> flags,Field<uint> skillObjectId,Field<uint> animation,Field<float> speed,GameControllerMoveProductionContext context,GameControllerMoveProductionInvocation invocation){
   var c=Need(invocation,0x12435b);c.Begin();var i=c.Native;
   try{
    At(i,0x12435b);c.CapturedSkillId=Need(skillObjectId,i.At).Snapshot();At(i,0x12435e);c.CapturedFlags=Need(flags,i.At).Snapshot();
    // RDX is captured as an address here; its pointee is not read until082B97.
    var destinationAddress=destination;var controllerAddress=controller;
    At(i,0x12436c);c.AllocatedAction=GameMoveActionOwnHeap.AllocateOwn();c.AllocationReturned=true;
    var ctx=Need(context,i.At);c.Primary=new GameReachedGoalActionView(c.AllocatedAction);ctx.ReachedGoal.BindAction(c.Primary);c.ConstructionState=GameMoveActionConstructionState.Bind(c.AllocatedAction,c.Primary);
    At(i,0x12437f);var first=ControllerAiLookupContracts.GetAI078D40(controllerAddress,ctx.Lookup,c.FirstAI);c.FirstAIReturned=true;
    At(i,0x12438d);uint reference=SkillReferenceNumberContracts.GetSkillReferenceNumber531A30(ctx.Lookup.Bridge,first,c.CapturedSkillId,c.Skill);c.SkillReference.Write(reference);c.SkillReturned=true;
    At(i,0x124397);var second=ControllerAiLookupContracts.GetAI078D40(controllerAddress,ctx.Lookup,c.SecondAI);c.SecondAIReturned=true;
    At(i,0x12439f);var entity=ctx.Lookup.Bridge.ForMoveCharacter(second,0x00bbe0).Owner.Entity;c.BorrowedCoordinates=EngineMouseGetterContracts.GetCoordsUnparented00BBE0(entity,i);c.CoordinatesReturned=true;
    At(i,0x1243a5);c.BorrowedCoordinates.RequireLive(i.At);var position=c.BorrowedCoordinates.Position00;var q0=position.Q0.Snapshot();var d8=position.D8.Snapshot();var dc=position.DC.Snapshot();
    At(i,0x1243a8);c.LocalStart.Q0.CopyFrom(q0);c.LocalStart.D8.CopyFrom(d8);c.LocalStart.DC.CopyFrom(dc);
    At(i,0x1243ad);c.BorrowedCoordinates.RequireLive(i.At);var d10=position.D10.Snapshot();var d14=position.D14.Snapshot();At(i,0x1243b2);c.LocalStart.D10.CopyFrom(d10);c.LocalStart.D14.CopyFrom(d14);c.StartCopied=true;
    At(i,0x1243b8);c.CapturedAnimation=Need(animation,i.At).Snapshot();At(i,0x1243c3);c.CapturedSpeed=Need(speed,i.At).Snapshot();
    At(i,0x1243e2);c.CapturedOwnerId=PlayerMouseControllerObjectFields.ForController(Need(controllerAddress,i.At)).ObjectId30.Snapshot();
    c.LocalStartWords=WorldPositionWordBridge.FromDwords(c.LocalStart);if(destinationAddress!=null)c.DestinationWords=WorldPositionWordBridge.FromDwords(destinationAddress);
    At(i,0x1243e9);c.ConstructorResult=GameMoveActionConstructionContracts.Construct082B10(c.ConstructionState,c.CapturedOwnerId,c.LocalStartWords.Words,c.DestinationWords==null?null:c.DestinationWords.Words,c.CapturedFlags,c.SkillReference,c.CapturedSpeed,c.CapturedAnimation,c.Construction);c.ConstructorReturned=true;
    At(i,0x124409);c.HandleEntered=true;HandleAction127DD0(controllerAddress,c.ConstructorResult,ctx,c.Handle);c.Returned=true;
   }catch(MissingContract e){i.At=e.Address;throw;}
  }
 }
}
