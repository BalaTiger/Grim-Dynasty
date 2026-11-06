using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // Platform adapter for the already explicit normal managed allocation at
 // Game12436C. Only this factory can establish allocation provenance; a bound
 // constructor view alone is not evidence that an arbitrary action is ours.
 // Tokens are reference identities, never native/CLR addresses. No Adopt API,
 // native heap call, allocation-failure/EH or concurrent native use is modeled.
 public static class GameMoveActionOwnHeap {
  sealed class Allocation {internal bool Released;}
  static readonly ConditionalWeakTable<MoveActionRecord,Allocation> allocations=new ConditionalWeakTable<MoveActionRecord,Allocation>();
  public static MoveActionRecord AllocateOwn(){
   var action=new MoveActionRecord();lock(allocations){allocations.Add(action,new Allocation());}return action;
  }
  public static void ReleaseOwn(MoveActionRecord action,uint at){
   if(action==null)throw new MissingContract(at,"Missing original OWN MoveTo allocation");
   lock(allocations){
    Allocation allocation;
    if(!allocations.TryGetValue(action,out allocation))throw new MissingContract(at,"MoveTo storage was not created by this OWN allocation factory; no foreign/native free is inferred");
    if(allocation.Released)throw new MissingContract(at,"OWN MoveTo allocation already released; no repeated free is performed");
    action.RequireLive(at);action.MarkReleased();allocation.Released=true;
   }
  }
 }
 public sealed class GameMoveActionConstructionTable {
  public readonly uint Rva;
  GameMoveActionConstructionTable(uint rva){Rva=rva;}
  public static readonly GameMoveActionConstructionTable Engine339EF0=new GameMoveActionConstructionTable(0x339ef0),Game6E5710=new GameMoveActionConstructionTable(0x6e5710);
 }
 // One sidecar for missing inline cells, borrowing the existing action/vptr.
 // Allocation establishes no native values; the constructor writes them later.
 public sealed class GameMoveActionConstructionState {
  static readonly ConditionalWeakTable<MoveActionRecord,GameMoveActionConstructionState> states=new ConditionalWeakTable<MoveActionRecord,GameMoveActionConstructionState>();
  public readonly MoveActionRecord Action;
  public readonly GameReachedGoalActionView Primary;
  public Field<object> Virtual00{get{return Primary.Virtual00;}}
  public readonly Field<uint> Dword0C=new Field<uint>();
  public readonly Field<byte> Byte10=new Field<byte>();
  public readonly WorldPosition24 Position20=new WorldPosition24();
  GameMoveActionConstructionState(MoveActionRecord action,GameReachedGoalActionView primary){Action=action;Primary=primary;}
  public static GameMoveActionConstructionState Bind(MoveActionRecord action,GameReachedGoalActionView primary){
   if(action==null||primary==null)throw new ArgumentNullException("action primary view");
   if(!Object.ReferenceEquals(primary.Action,action))throw new ArgumentException("Construction must borrow the same action's primary view");action.RequireLive(0);
   lock(states){GameMoveActionConstructionState prior;if(states.TryGetValue(action,out prior)){if(!Object.ReferenceEquals(prior.Primary,primary))throw new InvalidOperationException("Action construction already owns another primary view");return prior;}var state=new GameMoveActionConstructionState(action,primary);states.Add(action,state);return state;}
  }
  public static GameMoveActionConstructionState ForBound(MoveActionRecord action,uint at){
   if(action==null)throw new MissingContract(at,"Missing original MoveTo action");action.RequireLive(at);GameMoveActionConstructionState state;
   if(!states.TryGetValue(action,out state))throw new MissingContract(at,"MoveTo construction sidecar requires explicit shared primary binding");return state;
  }
  internal void RequireBound(uint at){
   if(!Object.ReferenceEquals(Primary.Action,Action)||!Object.ReferenceEquals(ForBound(Action,at),this))throw new MissingContract(at,"MoveTo constructor lost its unique original action/view binding");
  }
 }
 public sealed class GameMoveActionConstructionWrite {
  public readonly uint Address,Offset;
  public readonly int Bytes;
  public GameMoveActionConstructionWrite(uint address,uint offset,int bytes){Address=address;Offset=offset;Bytes=bytes;}
 }
 public sealed class GameMoveActionConstructionInvocation {
  public readonly Invocation Native=new Invocation();
  public readonly List<GameMoveActionConstructionWrite> Writes=new List<GameMoveActionConstructionWrite>();
  public Field<uint> CapturedOwnerId;
  public MoveActionRecord EngineReturnedRax,ReturnedRax;
  public bool EngineReturned,WorldVecReturned,Returned;
  internal void Begin(){Native.At=0;Writes.Clear();CapturedOwnerId=null;EngineReturnedRax=ReturnedRax=null;EngineReturned=WorldVecReturned=Returned=false;}
 }
 // Evidence: r1-movement-action-path08 (082B10), exact IAT609ED0 binding,
 // r1-move-production-engine-run1/05 (008300); WorldVec228AB0 already closed.
 // shortcut: typed whole-word aliases only; byte-unaligned memory, native ABI,
 // allocator/EH and concurrent mutation require separately evidenced adapters.
 public static class GameMoveActionConstructionContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Missing own MoveTo constructor observation");return value;}
  static uint At(Invocation invocation,uint at){invocation.At=at;return at;}
  static void Store(GameMoveActionConstructionInvocation c,uint at,uint offset,int bytes){c.Native.At=at;c.Writes.Add(new GameMoveActionConstructionWrite(at,offset,bytes));}
  // Both Engine unsigned-argument ctor and copy-ctor exports alias008300.
  // EDX/source is unused: neither OwnerId08 nor any source field is consumed.
  public static MoveActionRecord ConstructCharacterAction008300(GameMoveActionConstructionState state,Invocation invocation){
   var i=Need(invocation,0x008300);At(i,0x008307);Need(state,i.At).RequireBound(i.At);state.Virtual00.Write(GameMoveActionConstructionTable.Engine339EF0);At(i,0x00830d);return state.Action;
  }
  public static MoveActionRecord Construct082B10(GameMoveActionConstructionState state,Field<uint> ownerId,WorldPosition start,WorldPosition destination,Field<uint> flags,Field<uint> skillReference,Field<float> speed,Field<uint> animation,GameMoveActionConstructionInvocation c){
   if(c==null)throw new ArgumentNullException("invocation");c.Begin();var i=c.Native;
   At(i,0x082b39);c.CapturedOwnerId=Need(ownerId,i.At).Snapshot();
   At(i,0x082b3e);Need(state,i.At).RequireBound(i.At);var action=state.Action;
   c.EngineReturnedRax=ConstructCharacterAction008300(state,i);c.EngineReturned=true;c.Writes.Add(new GameMoveActionConstructionWrite(0x008307,0,8));
   Store(c,0x082b4c,0,8);state.Virtual00.Write(GameMoveActionConstructionTable.Game6E5710);
   Store(c,0x082b4f,8,4);action.OwnerId08.CopyFrom(c.CapturedOwnerId);
   Store(c,0x082b53,0x0c,4);state.Dword0C.Write(0);
   Store(c,0x082b5b,0x10,2);state.Byte10.Write(0);action.Flag11.Write(1);
   Store(c,0x082b62,0x14,4);action.Scalar14.Write(BitConverter.ToSingle(BitConverter.GetBytes(0x437a0000u),0));
   Store(c,0x082b6a,0x18,1);action.PreAction18.Write(0);
   At(i,0x082b73);GameMovedContracts.ConstructWorldVec228AB0(state.Position20,i);c.WorldVecReturned=true;c.Writes.Add(new GameMoveActionConstructionWrite(0x228ab0,0x20,8));
   Store(c,0x082b81,0,8);state.Virtual00.Write(GameReachedGoalActionTable.MoveTo);
   // MOVUPS captures all16 bytes before either destination word is changed.
   At(i,0x082b84);Need(start,i.At);var first0=start.Q0.Snapshot();var first8=start.Q8.Snapshot();
   Store(c,0x082b87,0x48,16);action.Start48.Q0.CopyFrom(first0);action.Start48.Q8.CopyFrom(first8);
   At(i,0x082b8c);var first16=start.Q16.Snapshot();Store(c,0x082b91,0x58,8);action.Start48.Q16.CopyFrom(first16);
   At(i,0x082b97);Need(destination,i.At);var second0=destination.Q0.Snapshot();var second8=destination.Q8.Snapshot();
   Store(c,0x082b9a,0x60,16);action.Destination60.Q0.CopyFrom(second0);action.Destination60.Q8.CopyFrom(second8);
   At(i,0x082b9f);var second16=destination.Q16.Snapshot();Store(c,0x082ba4,0x70,8);action.Destination60.Q16.CopyFrom(second16);
   At(i,0x082baa);var copiedFlags=Need(flags,i.At).Snapshot();Store(c,0x082bae,0x78,4);action.Flags78.CopyFrom(copiedFlags);
   At(i,0x082bb2);var copiedSkill=Need(skillReference,i.At).Snapshot();Store(c,0x082bb6,0x7c,4);action.SkillReference7C.CopyFrom(copiedSkill);
   At(i,0x082bba);var copiedSpeed=Need(speed,i.At).Snapshot();Store(c,0x082bc0,0x80,4);action.Speed80.CopyFrom(copiedSpeed);
   At(i,0x082bc9);var copiedAnimation=Need(animation,i.At).Snapshot();Store(c,0x082bcd,0x84,4);action.Animation84.CopyFrom(copiedAnimation);
   Store(c,0x082bd4,0x0c,4);state.Dword0C.Write(4);Store(c,0x082bdc,0x10,1);state.Byte10.Write(1);
   At(i,0x082be1);c.ReturnedRax=action;c.Returned=true;At(i,0x082bf9);return action;
  }
 }
}
