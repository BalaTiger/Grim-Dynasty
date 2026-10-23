using System;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // One explicit sidecar for the already bound full Character identity. These
 // are new24B0/24B9 cells, never aliases of the unrelated ActionState24B4.
 // Character and Player bodies both use this exact state; binding initializes
 // no native field and does not implicitly establish an ObjectId binding.
 public sealed class PlayerAliveState {
  static readonly ConditionalWeakTable<object,PlayerAliveState> owners=new ConditionalWeakTable<object,PlayerAliveState>();
  static readonly object gate=new object();
  public readonly GameMovementCharacterState Owner;
  public readonly MoveCharacter Role;
  public readonly Field<uint> Flags24B0=new Field<uint>();
  public readonly Field<byte> Byte24B9=new Field<byte>();
  PlayerAliveState(GameMovementIdentityView view){Owner=view.Owner;Role=view.Move;}
  public static PlayerAliveState Bind(GameMovementIdentityBridge bridge,MoveCharacter role){
   if(bridge==null)throw new ArgumentNullException("bridge");var view=bridge.ForMoveCharacter(role,0);view.Owner.Entity.RequireLive(0);
   lock(gate){PlayerAliveState prior;if(owners.TryGetValue(view.Owner.Entity.Identity,out prior)){if(!Object.ReferenceEquals(prior.Owner,view.Owner)||!Object.ReferenceEquals(prior.Role,role))throw new InvalidOperationException("Alive identity already has another canonical owner");return prior;}var value=new PlayerAliveState(view);owners.Add(view.Owner.Entity.Identity,value);return value;}
  }
  public static PlayerAliveState ForBoundRole(PlayerMousePlayerLookupContext context,MoveCharacter role,uint at){
   if(context==null)throw new MissingContract(at,"Missing existing alive lookup context");var owner=context.Bridge.ForMoveCharacter(role,at).Owner;PlayerAliveState state;
   if(!owners.TryGetValue(owner.Entity.Identity,out state)||!Object.ReferenceEquals(state.Owner,owner)||!Object.ReferenceEquals(state.Role,role))throw new MissingContract(at,"Alive fields require explicit original-owner binding");state.RequireLive(at);return state;
  }
  public void RequireLive(uint at){PlayerAliveState actual;if(!owners.TryGetValue(Owner.Entity.Identity,out actual)||!Object.ReferenceEquals(actual,this))throw new MissingContract(at,"Alive fields are not the unique original-owner state");Owner.Entity.RequireLive(at);}
 }
 // A view of an explicitly supplied OWN engine identity/lifetime. Existing
 // Connections40F8 is BORROWED so a PortalGameEngine cell can be shared later;
 // there is no second40F8 mirror. The new19B0 field starts Unknown. Binding is
 // not evidence of native construction or a loaded null video object.
 public sealed class GamePlayerVideoState {
  static readonly ConditionalWeakTable<object,GamePlayerVideoState> engines=new ConditionalWeakTable<object,GamePlayerVideoState>();
  static readonly object gate=new object();
  public readonly object Identity;
  public readonly NavigationView Lifetime;
  public readonly Field<object> Video19B0=new Field<object>();
  public readonly Field<object> Connections40F8;
  GamePlayerVideoState(object identity,NavigationView lifetime,Field<object> connections){Identity=identity;Lifetime=lifetime;Connections40F8=connections;}
  public static GamePlayerVideoState Bind(object identity,NavigationView lifetime,Field<object> connections40F8){
   if(identity==null||lifetime==null||connections40F8==null)throw new ArgumentNullException("existing engine identity/lifetime/40F8");var address=identity as NavigationView;if(address!=null)address.RequireLive(0);lifetime.RequireLive(0);
   lock(gate){GamePlayerVideoState prior;if(engines.TryGetValue(identity,out prior)){if(!Object.ReferenceEquals(prior.Lifetime,lifetime)||!Object.ReferenceEquals(prior.Connections40F8,connections40F8))throw new InvalidOperationException("Engine identity already has another lifetime or40F8 cell");return prior;}var value=new GamePlayerVideoState(identity,lifetime,connections40F8);engines.Add(identity,value);return value;}
  }
  public static GamePlayerVideoState ForBoundIdentity(object identity,uint at){GamePlayerVideoState value;if(identity==null||!engines.TryGetValue(identity,out value))throw new MissingContract(at,"GameEngine video predicate requires explicit existing identity/lifetime binding");value.RequireLive(at);return value;}
  public void RequireLive(uint at){GamePlayerVideoState actual;if(!engines.TryGetValue(Identity,out actual)||!Object.ReferenceEquals(actual,this))throw new MissingContract(at,"GameEngine video view is not its unique bound state");var address=Identity as NavigationView;if(address!=null)address.RequireLive(at);Lifetime.RequireLive(at);}
 }
 // Only an explicit OWN connection/vptr view. No concrete slot18 target has
 // been recovered, so no method can inject a successful returned object.
 public sealed class GamePlayerVideoConnection:NavigationView {
  public readonly Field<object> Virtual00=new Field<object>();
 }
 public sealed class PlayerAliveContext {
  public readonly PlayerMousePlayerLookupContext Lookup;
  public readonly Field<object> GameEngineA66288;
  public PlayerAliveContext(PlayerMousePlayerLookupContext lookup){if(lookup==null)throw new ArgumentNullException("lookup");Lookup=lookup;GameEngineA66288=lookup.Mouse.GameEngineA66288;}
 }
 public sealed class PlayerAliveInvocation {
  public readonly Invocation Native=new Invocation();
  public Invocation ObjectId{get;private set;}
  public Invocation Video{get;private set;}
  public readonly Field<uint> CapturedId=new Field<uint>();
  public readonly Field<object> CapturedEngine=new Field<object>();
  public readonly Field<byte> Result=new Field<byte>();
  public bool ObjectIdReturned,VideoReturned,Returned;
  public PlayerAliveInvocation(){ObjectId=new Invocation();Video=new Invocation();}
  internal void Reset(){Native.At=0;ObjectId=new Invocation();Video=new Invocation();CapturedId.Forget();CapturedEngine.Forget();Result.Forget();ObjectIdReturned=false;VideoReturned=false;Returned=false;}
 }
 public static class PlayerAliveContracts {
  static PlayerAliveInvocation Begin(PlayerAliveInvocation call,uint at){if(call==null)throw new MissingContract(at,"Missing alive invocation");call.Reset();call.Native.At=at;return call;}
  static byte Return(byte value,uint at,PlayerAliveInvocation call){call.Native.At=at;call.Result.Write(value);call.Returned=true;return value;}
  static PlayerAliveState Live(PlayerAliveState state,uint at){if(state==null)throw new MissingContract(at,"Missing explicitly bound alive fields");state.RequireLive(at);return state;}
  // Actual Game05BB80: byte gate, then DWORD maskFFFFFFFD. Only bit1 is ignored.
  public static byte CharacterIsAlive05BB80(PlayerAliveState state,PlayerAliveInvocation invocation){
   var call=Begin(invocation,0x05bb80);var i=call.Native;
   if(Live(state,i.At).Byte24B9.Read(i.At)!=0)return Return(0,0x05bb8b,call);
   i.At=0x05bb8c;state.RequireLive(i.At);byte value=(byte)((state.Flags24B0.Read(i.At)&0xfffffffdu)==0?1:0);return Return(value,0x05bb99,call);
  }
  // Actual Game2FA290 through its first unresolved virtual call. Null19B0 is
  // a complete real false path. Nonnull ALWAYS reads40F8 and calls its slot18
  // before the ID comparison, video reload/slot168 or37300/37308 scan. Those
  // unreachable suffixes are not modeled as successful empty-vector fallbacks.
  public static byte IsPlayerInVideo2FA290(Field<object> engineBits,uint objectId,Invocation invocation){
   if(invocation==null)throw new MissingContract(0x2fa29a,"Missing video invocation");var i=invocation;i.At=0x2fa29a;
   if(engineBits==null)throw new MissingContract(i.At,"Missing captured GameEngine pointer bits");var engine=GamePlayerVideoState.ForBoundIdentity(engineBits.Read(i.At),i.At);
   if(engine.Video19B0.Read(i.At)==null){i.At=0x2fa309;return 0;}
   i.At=0x2fa2a9;engine.RequireLive(i.At);var connectionBits=engine.Connections40F8.Snapshot();
   i.At=0x2fa2b0;var connection=connectionBits.Read(i.At) as GamePlayerVideoConnection;if(connection==null)throw new MissingContract(i.At,"Connection40F8 has no explicit OWN vptr/lifetime view");connection.RequireLive(i.At);var table=connection.Virtual00.Snapshot();
   i.At=0x2fa2b3;table.Read(i.At);throw new MissingContract(i.At,"GameEngine connection slot18 target remains unbound; no returned object or video result fabricated");
  }
  // Actual Game3CE7B0, using the passed Object::GetObjectId03A740 body and the
  // same global Field as the existing mouse context. No delegate supplies IDs
  // or video results. The object ID is read before even capturing the engine.
  public static byte PlayerIsAlive3CE7B0(PlayerAliveState state,PlayerAliveContext context,PlayerAliveInvocation invocation){
   var call=Begin(invocation,0x3ce7b6);var i=call.Native;
   if(Live(state,i.At).Byte24B9.Read(i.At)!=0)return Return(0,0x3ce7c9,call);
   i.At=0x3ce7ca;call.CapturedId.Write(GameObjectIdContracts.GetObjectId03A740(state.Owner.Entity.Identity,call.ObjectId));call.ObjectIdReturned=true;
   i.At=0x3ce7d0;if(context==null)throw new MissingContract(i.At,"Missing shared GameEngine global context");call.CapturedEngine.CopyFrom(context.GameEngineA66288);
   i.At=0x3ce7d9;byte video=IsPlayerInVideo2FA290(call.CapturedEngine,call.CapturedId.Read(i.At),call.Video);call.VideoReturned=true;
   i.At=0x3ce7de;if(video!=0)return Return(0,0x3ce7c9,call);
   i.At=0x3ce7e2;state.RequireLive(i.At);byte value=(byte)((state.Flags24B0.Read(i.At)&0xfffffffdu)==0?1:0);return Return(value,0x3ce7f4,call);
  }
 }
 // Exact disk slot bindings: Character6DF6D0+460 ->05BB80,
 // Player787100+460 ->3CE7B0. Dispatch consumes the caller's captured table;
 // it never rereads/replaces that table with a convenient current identity.
 public class PlayerMouseAliveBoundaries:PlayerMouseDisplayedSkillSetBoundaries {
  public readonly PlayerAliveContext AliveContext;
  public PlayerAliveInvocation LastAlive{get;private set;}
  public PlayerMouseAliveBoundaries(PlayerAliveContext context):base(Require(context).Lookup){AliveContext=context;}
  static PlayerAliveContext Require(PlayerAliveContext context){if(context==null)throw new ArgumentNullException("context");return context;}
  public override byte CanAct460(object table,MoveCharacter role){
   LastAlive=new PlayerAliveInvocation();
   if(Object.ReferenceEquals(table,GameMovementPrimaryTable.Player))return PlayerAliveContracts.PlayerIsAlive3CE7B0(PlayerAliveState.ForBoundRole(LookupContext,role,0x3ce7b6),AliveContext,LastAlive);
   if(Object.ReferenceEquals(table,GameMovementPrimaryTable.Character))return PlayerAliveContracts.CharacterIsAlive05BB80(PlayerAliveState.ForBoundRole(LookupContext,role,0x05bb80),LastAlive);
   LastAlive.Native.At=0x15e230;throw new MissingContract(0x15e230,"Captured role primary+460 table has no observed IsAlive binding");
  }
 }
}
