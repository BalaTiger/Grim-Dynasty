using System;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // New fields only: the canonical owner, MoveCharacter and MovementDC0 remain
 // those of the existing bridge. Binding neither creates a manager nor seeds
 // any flag/ratio. All request paths reread these shared cells at native sites.
 public sealed class PlayerMoveRequestRoleState {
  static readonly ConditionalWeakTable<object,PlayerMoveRequestRoleState> states=new ConditionalWeakTable<object,PlayerMoveRequestRoleState>();
  static readonly object gate=new object();
  public readonly GameMovementCharacterState Owner;
  public readonly MoveCharacter Role;
  public readonly Field<byte> Special45B4=new Field<byte>();
  public readonly Field<float> MaximumRatio4BF0=new Field<float>();
  PlayerMoveRequestRoleState(GameMovementIdentityView view){Owner=view.Owner;Role=view.Move;}
  public static PlayerMoveRequestRoleState Bind(GameMovementIdentityBridge bridge,MoveCharacter role){
   if(bridge==null)throw new ArgumentNullException("bridge");var view=bridge.ForMoveCharacter(role,0);
   lock(gate){PlayerMoveRequestRoleState old;if(states.TryGetValue(view.Owner.Entity.Identity,out old)){if(!Object.ReferenceEquals(old.Owner,view.Owner)||!Object.ReferenceEquals(old.Role,role))throw new InvalidOperationException("Request fields already belong to another canonical role");return old;}var value=new PlayerMoveRequestRoleState(view);states.Add(view.Owner.Entity.Identity,value);return value;}
  }
  public static PlayerMoveRequestRoleState ForBound(GameMovementIdentityBridge bridge,MoveCharacter role,uint at){
   if(bridge==null)throw new MissingContract(at,"Missing original request bridge");var view=bridge.ForMoveCharacter(role,at);PlayerMoveRequestRoleState state;
   if(!states.TryGetValue(view.Owner.Entity.Identity,out state)||!Object.ReferenceEquals(state.Owner,view.Owner)||!Object.ReferenceEquals(state.Role,role))throw new MissingContract(at,"45B4/4BF0 require an explicit unique role binding");return state;
  }
 }
 // Sole37748 cell for the already bound GameEngine. No duplicate global,
 // connection object,40F8 or19B0 is introduced by this view.
 public sealed class PlayerMoveRequestEngineState {
  static readonly ConditionalWeakTable<object,PlayerMoveRequestEngineState> states=new ConditionalWeakTable<object,PlayerMoveRequestEngineState>();
  static readonly object gate=new object();
  public readonly GamePlayerVideoState Owner;
  public readonly Field<uint> Override37748=new Field<uint>();
  PlayerMoveRequestEngineState(GamePlayerVideoState owner){Owner=owner;}
  public static PlayerMoveRequestEngineState Bind(GamePlayerVideoState owner){
   if(owner==null)throw new ArgumentNullException("owner");owner.RequireLive(0);
   lock(gate){PlayerMoveRequestEngineState old;if(states.TryGetValue(owner.Identity,out old)){if(!Object.ReferenceEquals(old.Owner,owner))throw new InvalidOperationException("GameEngine37748 already has another owner");return old;}var value=new PlayerMoveRequestEngineState(owner);states.Add(owner.Identity,value);return value;}
  }
  public static PlayerMoveRequestEngineState ForBound(object identity,uint at){PlayerMoveRequestEngineState value;if(identity==null||!states.TryGetValue(identity,out value))throw new MissingContract(at,"GameEngine37748 requires explicit existing-identity binding");value.Owner.RequireLive(at);return value;}
 }
 public sealed class PlayerMoveRequestContext {
  public readonly PlayerMousePlayerLookupContext Lookup;
  public readonly GamePathPrecheckContext Path;
  public readonly Field<float> Half7A8024=new Field<float>(),DistanceSquared7A7FA0=new Field<float>(),MinusOne7A81AC=new Field<float>(),Radius7A80E4=new Field<float>();
  public readonly Field<string> MoveToName675168=new Field<string>();
  public Field<float> One7A8054{get{return Path.One7A8054;}}
  public PlayerMoveRequestContext(PlayerMousePlayerLookupContext lookup,GamePathPrecheckContext path){
   if(lookup==null||path==null)throw new ArgumentNullException("lookup/path");
   if(!Object.ReferenceEquals(lookup.Bridge,path.Bridge)||!Object.ReferenceEquals(lookup.Mouse.Movement.Context,path.Movement))throw new ArgumentException("Request must share original actor bridge and MoveContext with precheck");
   if(!Object.ReferenceEquals(path.Navigation.World.EngineGlobal,path.Movement.EngineGlobal))throw new ArgumentException("Request and world floor must borrow the same gEngine Field, not synchronized copies");Lookup=lookup;Path=path;
  }
  // Explicit OWN selection from the two retained exact constant inspections;
  // not an observation that a loaded native process kept these disk values.
  public void SelectObservedRequestConstants(){One7A8054.Write(NavigationCoreContracts.FromBits(0x3f800000));Half7A8024.Write(NavigationCoreContracts.FromBits(0x3f000000));DistanceSquared7A7FA0.Write(NavigationCoreContracts.FromBits(0x3a23d70b));MinusOne7A81AC.Write(NavigationCoreContracts.FromBits(0xbf800000));Radius7A80E4.Write(NavigationCoreContracts.FromBits(0x41200000));MoveToName675168.Write("MoveTo");}
 }
 public sealed class PlayerMovePathInvocation {
  public readonly Invocation Native=new Invocation(),MainPlayer=new Invocation(),Options=new Invocation(),Integer=new Invocation();
  public GamePathPrecheckInvocation Precheck{get;private set;}
  public readonly Field<object> CapturedEngine=new Field<object>(),FreshEngine=new Field<object>();
  public readonly Field<MoveCharacter> MainPlayerResult=new Field<MoveCharacter>();
  public Field<float> CapturedMinimum{get;private set;}
  public Field<float> MinimumDot{get;private set;}
  public bool MainPlayerReturned,OptionsReturned,IntegerReturned,PrecheckReturned,Returned;
  public uint Result;
  public PlayerMovePathInvocation(){Begin();}
  internal void Begin(){Native.At=MainPlayer.At=Options.At=Integer.At=0;CapturedEngine.Forget();FreshEngine.Forget();MainPlayerResult.Forget();CapturedMinimum=new Field<float>();MinimumDot=new Field<float>();Precheck=new GamePathPrecheckInvocation();MainPlayerReturned=OptionsReturned=IntegerReturned=PrecheckReturned=Returned=false;Result=0;}
 }
 public sealed class PlayerMoveTransitionPayload {
  public readonly Field<ulong> Zero00=new Field<ulong>();
  public readonly Field<uint> Zero08=new Field<uint>(),Padding0C=new Field<uint>();
  public readonly WorldPosition24 Destination10=new WorldPosition24();
 }
 public sealed class PlayerMoveShortName {
  public readonly Field<byte>[] Inline00=new Field<byte>[16];
  public readonly Field<ulong> Length10=new Field<ulong>(),Capacity18=new Field<ulong>();
  public PlayerMoveShortName(){for(int n=0;n<Inline00.Length;n++)Inline00[n]=new Field<byte>();}
 }
 public sealed class PlayerMoveRequestInvocation {
  public readonly Invocation Native=new Invocation(),Unparented=new Invocation(),Coordinates=new Invocation(),Difference=new Invocation(),Name=new Invocation();
  public PlayerMousePlayerLookupInvocation Lookup{get;private set;}
  public PlayerMovePathInvocation Path{get;private set;}
  public WorldPosition24 Destination{get;private set;}
  public WorldPosition24 Source{get;private set;}
  public WorldPosition24 PathOutput{get;private set;}
  public EntityCoordinates64 ParentAwareCoordinates{get;private set;}
  public EntityCoordinates64 BorrowedUnparented{get;internal set;}
  public Displacement3 Delta{get;private set;}
  public PlayerMoveTransitionPayload Payload{get;private set;}
  public PlayerMoveShortName StateName{get;private set;}
  public bool RegionReturned,UnparentedReturned,FirstDifferenceReturned,CoordsReturned,SecondDifferenceReturned,PathReturned,NamePrepared,TransitionPayloadPrepared,Returned;
  public uint DecisionAt;
  public PlayerMoveRequestInvocation(){Begin();}
  internal void Begin(){Native.At=Unparented.At=Coordinates.At=Difference.At=Name.At=0;Lookup=new PlayerMousePlayerLookupInvocation();Path=new PlayerMovePathInvocation();Destination=new WorldPosition24();Source=new WorldPosition24();PathOutput=new WorldPosition24();ParentAwareCoordinates=new EntityCoordinates64();BorrowedUnparented=null;Delta=new Displacement3();Payload=new PlayerMoveTransitionPayload();StateName=new PlayerMoveShortName();RegionReturned=UnparentedReturned=FirstDifferenceReturned=CoordsReturned=SecondDifferenceReturned=PathReturned=NamePrepared=TransitionPayloadPrepared=Returned=false;DecisionAt=0;}
 }
 public static class PlayerMoveRequestContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null own move-request view");return value;}
  static uint At(Invocation i,uint at){i.At=at;return at;}
  static float F(float value){return NavigationCoreContracts.F(value);}
  static void Return(PlayerMoveRequestInvocation c,uint at){c.DecisionAt=at;c.Native.At=0x1639b0;c.Returned=true;}
  static MoveCharacter Resolve(PlayerMouseState state,PlayerMoveRequestContext context,PlayerMoveRequestInvocation c,uint load,uint test,uint ownerAt,uint call,uint store){
   var i=c.Native;At(i,load);var bits=Need(state,i.At).CachedRole10.Snapshot();var role=bits.Read(At(i,test));if(role!=null)return role;
   At(i,ownerAt);var controller=state.Owner08.Snapshot();At(i,call);c.Lookup.Native.At=0x128536;role=PlayerMousePlayerLookupContracts.Resolve128530(controller.Read(c.Lookup.Native.At),Need(context,i.At).Lookup,c.Lookup);At(i,store);state.CachedRole10.Write(role);return role;
  }
  static void Copy16Then8(WorldPosition24 source,WorldPosition24 target,Invocation i,uint load16,uint store16,uint load8,uint store8){
   At(i,load16);var q=Need(source,i.At).Q0.Snapshot();var x=source.D8.Snapshot();var y=source.DC.Snapshot();At(i,store16);target.Q0.CopyFrom(q);target.D8.CopyFrom(x);target.DC.CopyFrom(y);
   At(i,load8);var z=source.D10.Snapshot();var pad=source.D14.Snapshot();At(i,store8);target.D10.CopyFrom(z);target.D14.CopyFrom(pad);
  }
  // Exact3CE890 wrapper. MainPlayer is the real independently recovered
  // producer, never a supplied successful callback. Options/08E200 are also
  // concrete production calls. An unresolved main-player dependency remains
  // its actual stop; no non-main-player or37748 value is guessed to bypass it.
  public static uint FindPath3CE890(MoveCharacter role,WorldPosition24 destination,Field<float> minimumDistance,WorldPosition24 output,Field<float> ratioBaseline,PlayerMoveRequestContext context,PlayerMovePathInvocation invocation){
   var c=Need(invocation,0x3ce8a7);c.Begin();var i=c.Native;var ctx=Need(context,At(i,0x3ce8a7));c.CapturedEngine.CopyFrom(ctx.Lookup.Mouse.GameEngineA66288);
   At(i,0x3ce8b1);c.MinimumDot.CopyFrom(ctx.MinusOne7A81AC);At(i,0x3ce8c1);c.CapturedMinimum.CopyFrom(Need(minimumDistance,i.At));
   At(i,0x3ce8c4);c.MainPlayer.At=0x2ebb79;var main=GameMainPlayerContracts.GetMainPlayer2EBB70(c.CapturedEngine.Read(c.MainPlayer.At),ctx.Lookup,c.MainPlayer);c.MainPlayerResult.Write(main);c.MainPlayerReturned=true;
   At(i,0x3ce8c9);if(Object.ReferenceEquals(role,main)){
    At(i,0x3ce8ce);c.FreshEngine.CopyFrom(ctx.Lookup.Mouse.GameEngineA66288);At(i,0x3ce8d5);bool overrideOption=PlayerMoveRequestEngineState.ForBound(c.FreshEngine.Read(i.At),i.At).Override37748.Read(i.At)!=0;
    if(!overrideOption){At(i,0x3ce8de);var engine=ctx.Path.Movement.EngineGlobal.Snapshot();At(i,0x3ce8e8);var options=EngineMouseGetterContracts.GetOptions073660(engine.Read(0x073677),c.Options);c.OptionsReturned=true;At(i,0x3ce8f6);overrideOption=EngineMouseGetterContracts.GetInt182D60(options,13,c.Integer)>0;c.IntegerReturned=true;}
    if(overrideOption){At(i,0x3ce900);c.MinimumDot.Write(0);}
   }
   At(i,0x3ce903);var baseline=Need(ratioBaseline,i.At).Snapshot();At(i,0x3ce90f);var radius=ctx.Radius7A80E4.Snapshot();
   At(i,0x3ce91a);var view=ctx.Lookup.Bridge.ForMoveCharacter(role,i.At);var manager=view.BorrowMovementDC0(i.At).Snapshot();
   At(i,0x3ce92c);var ratio=PlayerMoveRequestRoleState.ForBound(ctx.Lookup.Bridge,role,i.At).MaximumRatio4BF0.Snapshot();At(i,0x3ce94e);
   c.Result=GamePathPrecheckContracts.FindPath08E200(manager.Read(0x08e232),destination,radius,c.CapturedMinimum,ratio,c.MinimumDot,output,null,baseline,Field<byte>.Of(0),ctx.Path,c.Precheck);c.PrecheckReturned=true;c.Returned=true;i.At=0x3ce96e;return c.Result;
  }
  // Only the exact nonoverlapping six-byte SSO assignment used at16367A.
  // Existing016460 evidence proves capacity15 avoids growth and5DC344 copies
  // the six bytes. This managed copy is OWN memory adaptation, not native CRT
  // execution or a general std::string ABI/allocation/alias implementation.
  static void PrepareName(PlayerMoveRequestContext ctx,PlayerMoveRequestInvocation c){
   var i=c.Native;At(i,0x16365a);c.StateName.Capacity18.Write(15);At(i,0x163664);c.StateName.Length10.Write(0);At(i,0x163668);c.StateName.Inline00[0].Write(0);
   At(i,0x16367a);var n=c.Name;n.At=0x01647d;ulong capacity=c.StateName.Capacity18.Read(n.At);n.At=0x0164eb;if(capacity<6)throw new MissingContract(n.At,"Fixed MoveTo name unexpectedly needs native growth");
   n.At=0x01655e;string text=ctx.MoveToName675168.Read(n.At);if(text!="MoveTo")throw new MissingContract(n.At,"Only the pinned six-byte MoveTo source is in this nonalias SSO domain");for(int k=0;k<6;k++)c.StateName.Inline00[k].Write((byte)text[k]);n.At=0x016568;c.StateName.Length10.Write(6);n.At=0x016576;c.StateName.Inline00[6].Write(0);n.At=0x01658c;c.NamePrepared=true;
  }
  // Idle165CB0 is a JMP to163490. The ordinary false45B4 path and native
  // early returns are covered. Camera retry, special reposition and fallback
  // projection are explicit stops; no unsuccessful path becomes movement.
  public static void MoveIdle165CB0(PlayerMouseState state,byte first,Field<byte> ignoredSecond,Field<WorldPosition24> position,PlayerMoveRequestContext context,PlayerMoveRequestInvocation invocation){
   var c=Need(invocation,0x163490);c.Begin();var i=c.Native;At(i,0x163490);if(first!=0){Return(c,0x163492);return;}
   // R8B is never used anywhere in this actual163490 body.
   At(i,0x1634d6);var target=Need(position,i.At).Read(0x22c0b0);var token=Need(target,0x22c0b0).Q0.Snapshot();c.RegionReturned=true;
   At(i,0x1634dc);if(token.Read(i.At)==0){Return(c,0x1634df);return;}
   var role=Resolve(state,context,c,0x1634e5,0x1634e9,0x1634ee,0x1634f2,0x1634f7);var ctx=Need(context,At(i,0x1634fb));var view=ctx.Lookup.Bridge.ForMoveCharacter(role,i.At);var managerBits=view.BorrowMovementDC0(i.At).Snapshot();
   At(i,0x163502);var one=ctx.One7A8054.Snapshot();At(i,0x16350a);bool moving=Need(managerBits.Read(i.At),i.At).Moving38.Read(i.At)!=0;Field<float> threshold=moving?ctx.Half7A8024.Snapshot():one.Snapshot();
   Copy16Then8(target,c.Destination,i,0x16351d,0x163520,0x163524,0x163529);
   role=Resolve(state,ctx,c,0x16352e,0x163532,0x163537,0x16353b,0x163540);At(i,0x163547);view=ctx.Lookup.Bridge.ForMoveCharacter(role,0x16354d);c.BorrowedUnparented=EngineMouseGetterContracts.GetCoordsUnparented00BBE0(view.Owner.Entity,c.Unparented);c.UnparentedReturned=true;c.BorrowedUnparented.RequireLive(0x16354d);
   Copy16Then8(c.BorrowedUnparented.Position00,c.Source,i,0x16354d,0x163550,0x163554,0x163559);
   At(i,0x16356a);WorldVectorDifferenceContracts.Subtract22C240(c.Destination,c.Delta,c.Source,ctx.Path.Navigation.World.State,ctx.Path.NullDifference338458,c.Difference);c.FirstDifferenceReturned=true;
   float z=c.Delta.Z8.Read(At(i,0x163570));At(i,0x163575);float zz=F(z*z);float x=c.Delta.X0.Read(At(i,0x163579));At(i,0x16357e);float xx=F(x*x);At(i,0x163582);float distance=F(zz+xx);float t=threshold.Read(At(i,0x163586));float thresholdSquared=F(t*t);At(i,0x16358a);if(!(distance>=thresholdSquared)){Return(c,0x16358d);return;}
   role=Resolve(state,ctx,c,0x163593,0x163597,0x16359c,0x1635a0,0x1635a8);At(i,0x1635ac);if(PlayerMoveRequestRoleState.ForBound(ctx.Lookup.Bridge,role,i.At).Special45B4.Read(i.At)!=0){At(i,0x1635bf);throw new MissingContract(i.At,"Special45B4 StopMoving and reposition path remains outside this ordinary request domain");}
   At(i,0x1635d5);view=ctx.Lookup.Bridge.ForMoveCharacter(role,i.At);managerBits=view.BorrowMovementDC0(i.At).Snapshot();At(i,0x1635e0);var owner=Need(managerBits.Read(i.At),i.At).Owner00.Snapshot();
   At(i,0x1635e3);var ownerRole=owner.Read(0x07f076) as MoveCharacter;var ownerView=ctx.Lookup.Bridge.ForMoveCharacter(ownerRole,0x07f076);var coordinates=EntityMovementContracts.GetCoords07F070(ownerView.Owner.Entity,c.ParentAwareCoordinates,c.Coordinates);c.CoordsReturned=true;
   At(i,0x1635f5);WorldVectorDifferenceContracts.Subtract22C240(c.Destination,c.Delta,coordinates.Position00,ctx.Path.Navigation.World.State,ctx.Path.NullDifference338458,c.Difference);c.SecondDifferenceReturned=true;
   float y=c.Delta.Y4.Read(At(i,0x1635fb));At(i,0x163601);float yy=F(y*y);x=c.Delta.X0.Read(At(i,0x163605));At(i,0x16360b);xx=F(x*x);At(i,0x16360f);distance=F(yy+xx);z=c.Delta.Z8.Read(At(i,0x163613));At(i,0x163619);zz=F(z*z);At(i,0x16361d);distance=F(distance+zz);At(i,0x163621);if(!(distance>ctx.DistanceSquared7A7FA0.Read(i.At))){Return(c,0x163628);return;}
   At(i,0x163633);GameMovedContracts.ConstructWorldVec228AB0(c.PathOutput,c.Coordinates);At(i,0x163651);uint result=FindPath3CE890(role,c.Destination,Field<float>.Of(0),c.PathOutput,Field<float>.Of(0),ctx,c.Path);c.PathReturned=true;
   At(i,0x163656);if(result!=0){At(i,result==2||result==4?0x1636d7u:0x163850u);throw new MissingContract(i.At,"Native path failure requires camera retry or fallback projection; not a successful move");}
   PrepareName(ctx,c);At(i,0x163680);c.Payload.Zero00.Write(0);At(i,0x163685);c.Payload.Zero08.Write(0);
   Copy16Then8(c.PathOutput,c.Payload.Destination10,i,0x163689,0x16368e,0x163693,0x163699);c.TransitionPayloadPrepared=true;
   At(i,0x1636a8);Need(state,i.At).Owner08.Snapshot();At(i,0x1636ac);throw new MissingContract(0x123d50,"Real MoveTo state transition123D50 is unimplemented; payload/name are only caller-local preparation, no action or Agent submission occurred");
  }
 }
 public class PlayerMouseMoveRequestBoundaries:PlayerMouseOptionsBoundaries {
  public readonly PlayerMoveRequestContext Request;
  public PlayerMoveRequestInvocation LastRequest{get;private set;}
  public PlayerMouseMoveRequestBoundaries(PlayerAliveContext alive,PlayerMoveRequestContext request):base(alive){if(request==null||!Object.ReferenceEquals(request.Lookup,LookupContext))throw new ArgumentException("Request must reuse the exact mouse lookup context");Request=request;}
  public override void Move280(PlayerMouseTable table,PlayerMouseState state,byte a,Field<byte> b,Field<WorldPosition24> position,uint at){
   LastRequest=new PlayerMoveRequestInvocation();if(table==null)throw new MissingContract(at,"Null captured mouse table");uint target=table.Move280.Read(at);if(target!=0x165cb0)throw new MissingContract(target,"Only the actual Idle Move280 target165CB0 is composed");PlayerMoveRequestContracts.MoveIdle165CB0(state,a,b,position,Request,LastRequest);
  }
 }
}
