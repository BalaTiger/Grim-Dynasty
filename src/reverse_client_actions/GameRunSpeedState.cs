using System;
using System.Runtime.CompilerServices;
using GrimDynasty.CombatRuntime;

namespace GrimDynasty.ReverseClient.Actions {
 // The ordinary getter borrows the already unique cap/attribute/Duration
 // owners. Binding creates no native field values or constructor completion.
 public sealed class GameRunSpeedState {
  static readonly ConditionalWeakTable<GamePlayerRunSpeedCapState,GameRunSpeedState> states=new ConditionalWeakTable<GamePlayerRunSpeedCapState,GameRunSpeedState>();
  public readonly GamePlayerRunSpeedCapState Cap;
  public readonly GameDurationDamageManagerState Duration;
  public Field<byte> SpeedFlag26B3{get{return Cap.Identity.Move.SpeedFlag26B3;}}
  GameRunSpeedState(GamePlayerRunSpeedCapState cap){Cap=cap;Duration=GameDurationDamageManagerState.Bind(cap);}
  public static GameRunSpeedState Bind(GamePlayerRunSpeedCapState cap){
   if(cap==null)throw new ArgumentNullException("existing cap state");cap.RequireOwner(cap.Identity.Move,0);
   return states.GetValue(cap,delegate(GamePlayerRunSpeedCapState value){return new GameRunSpeedState(value);});
  }
  public void RequireOwner(MoveCharacter caller,uint at){Cap.RequireOwner(caller,at);if(!Object.ReferenceEquals(Duration.Cap,Cap))throw new MissingContract(at,"Duration and cap must share the original attribute owner");}
 }
 public sealed class GameRunSpeedContext {
  public readonly GameRunSpeedState State;
  public readonly AddressSymbol<ClassRecord> PetPlayerScalingClass;
  public readonly NativeAttributeConstants Constants;
  public readonly bool SnapshotApplicable,NormalFpEnvironment;
  public GameRunSpeedContext(GameRunSpeedState state,AddressSymbol<ClassRecord> petClass,NativeAttributeConstants constants,bool snapshotApplicable,bool normalFpEnvironment){
   if(state==null||petClass==null)throw new ArgumentNullException("existing speed state/class symbol");
   // Fixed-address identity is OWN setup, not a mutable class-tree default.
   var pet=petClass.Read(0xa5cb40);var callbacks=state.Cap.Bridge.Callbacks;
   if(Object.ReferenceEquals(pet,callbacks.PlayerClass.Read(0xa5cab0))||Object.ReferenceEquals(pet,callbacks.CharacterClass.Read(0xa5cba0)))throw new ArgumentException("Distinct native class symbols require distinct identities");
   State=state;PetPlayerScalingClass=petClass;Constants=constants;SnapshotApplicable=snapshotApplicable;NormalFpEnvironment=normalFpEnvironment;
  }
 }
}
