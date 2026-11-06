using System;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // The two QWORD pointer cells belong to the existing full Character owner.
 // Binding neither initializes a sound pointer nor reconstructs a SoundPak.
 public sealed class GameRunningSoundState {
  static readonly ConditionalWeakTable<GameMovementCharacterState,GameRunningSoundState> states=new ConditionalWeakTable<GameMovementCharacterState,GameRunningSoundState>();
  public readonly GameMovementCharacterState Owner;
  public readonly Field<object> Sound2210,Sound2110;
  GameRunningSoundState(GameMovementCharacterState owner){
   Owner=owner;
   // A typed full-Entity identity is the already existing MoveCharacter. Borrow
   // its actual cells before any observation; never copy values after Begin.
   // Isolated owners with an opaque identity keep their independent Unknowns.
   var move=owner.Entity.Identity as MoveCharacter;
   Sound2210=move==null?new Field<object>():move.Sound2210;
   Sound2110=move==null?new Field<object>():move.Sound2110;
  }
  public static GameRunningSoundState ForOwner(GameMovementCharacterState owner){if(owner==null)throw new ArgumentNullException("owner");return states.GetValue(owner,delegate(GameMovementCharacterState value){return new GameRunningSoundState(value);});}
 }
 public sealed class GameRunningSoundContext {
  public readonly GameMovementIdentityBridge Bridge;
  public GameRunningSoundContext(GameMovementIdentityBridge bridge){if(bridge==null)throw new ArgumentNullException("bridge");Bridge=bridge;}
 }
 public sealed class GameRunningSoundInvocation {
  public readonly Invocation Native=new Invocation();
  public readonly Field<object> PrimaryCapture=new Field<object>(),FallbackCapture=new Field<object>(),Selected=new Field<object>();
  public GameMovementCharacterState Owner;
  public bool Used,PrimaryCaptured,FallbackCaptured,IsPlayingReached,Returned;
 }
 // Saved actual Game05CDC0 pseudo+assembly, r1-game-running-sound-run1/04..05.
 // MOV preserves opaque knowledge; TEST is the first pointer consumption.
 // Only the double-null normal return is closed. A nonnull pointer stops at
 // the actual IsPlaying import call, before any Boolean/audio result is known.
 public static class GameRunningSoundContracts {
  public static void PlayLoopingRunningSound05CDC0(GameMovementCharacterState owner,GameRunningSoundInvocation c){
   if(c==null)throw new ArgumentNullException("invocation");if(c.Used)throw new InvalidOperationException("Use a fresh running-sound invocation");c.Used=true;
   try{
    c.Native.At=0x05cdca;if(owner==null)throw new MissingContract(c.Native.At,"Null Character at running-sound pointer load");owner.Entity.RequireLive(c.Native.At);c.Owner=owner;var fields=GameRunningSoundState.ForOwner(owner);
    c.PrimaryCapture.CopyFrom(fields.Sound2210.Snapshot());c.PrimaryCaptured=true;c.Native.At=0x05cdd4;
    if(c.PrimaryCapture.Read(c.Native.At)!=null)c.Selected.CopyFrom(c.PrimaryCapture);
    else{
     c.Native.At=0x05cdd9;owner.Entity.RequireLive(c.Native.At);c.FallbackCapture.CopyFrom(fields.Sound2110.Snapshot());c.FallbackCaptured=true;c.Native.At=0x05cde0;
     if(c.FallbackCapture.Read(c.Native.At)==null){c.Selected.CopyFrom(c.FallbackCapture);c.Native.At=0x05ce41;c.Returned=true;return;}
     c.Selected.CopyFrom(c.FallbackCapture);
    }
    c.Native.At=0x05cde8;c.IsPlayingReached=true;throw new MissingContract(c.Native.At,"Nonnull SoundPak requires actual IsPlaying import609A08; no playback result is supplied");
   }catch(MissingContract e){c.Native.At=e.Address;throw;}
  }
 }
}
