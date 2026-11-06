using System;
using System.Collections.Generic;

namespace GrimDynasty.ReverseClient.Actions {
 public sealed class GameMoveActionDestructionInvocation {
  public readonly Invocation Native=new Invocation();
  public readonly List<GameMoveActionConstructionWrite> Writes=new List<GameMoveActionConstructionWrite>();
  public uint CapturedFlags;
  public GameMoveActionConstructionTable EngineReturnedRax;
  public MoveActionRecord ReturnedRax;
  public bool EngineEntered,EngineReturned,FreeAttempted,OwnStorageReleased,Returned;
  public bool NativeHeapFreeExecuted{get{return false;}}
  internal void Reset(uint flags){Native.At=0;Writes.Clear();CapturedFlags=flags;EngineReturnedRax=null;ReturnedRax=null;EngineEntered=EngineReturned=FreeAttempted=OwnStorageReleased=Returned=false;}
 }
 // Evidence: r1-move-action-delete-run1/05 (088EB0), exact609ED8 binding,
 // r1-engine-stop-final-dependencies-run1/08 (0485E0). Both bodies mutate
 // the constructor's same primary view. No other action bytes are cleared.
 // Explicit OWN domain: scalar flags0/1, canonical live managed action/view,
 // serial access. Array cookies/iteration, native ABI/EH and native lifetime
 // are not recovered. Exact60B888 -> api-ms-win-crt-heap-l1-1-0!free is in
 // offline-class-helper-storage-imports/metadata.json (8b8138b7...767e9c1).
 // flags1 releases only the same action created by GameMoveActionOwnHeap.
 // Foreign/unregistered storage still stops after the real base prefix at
 // 088F2A; this adapter never calls native free or clears a caller's Handler.
 // Saved object-lifecycle chain (each *-instructions.json SHA256):
 // offline-class-static-helper-storage-entries/5da5dc ->5DA6BC:
 // 96cdc5257964995fc7c0052c9a92bebc53f3ebded8712fbe7f73a12d727ac7db
 // offline-class-static-helper-storage-bodies/5da6bc ->5DC36E:
 // 266f0a580e7f876d1132d00e11ac75150afd90799a1309234e104a0a2f0901af
 // offline-class-static-helper-storage-failure/5dc36e ->[60B888]:
 // 5e7d7d09c441c3d4ce9c44505d5505e7a4a496c2d7f65f72203bf1f3886d3651
 public static class GameMoveActionDestructionContracts {
  static GameMoveActionConstructionState Require(GameMoveActionConstructionState state,uint at){
   if(state==null)throw new MissingContract(at,"Missing original MoveTo action construction state");
   var bound=GameMoveActionConstructionState.ForBound(state.Action,at);
   if(!Object.ReferenceEquals(bound,state)||!Object.ReferenceEquals(state.Primary.Action,state.Action))
    throw new MissingContract(at,"Destructor must borrow the canonical action and primary view");
   return state;
  }
  public static GameMoveActionConstructionTable DestroyCharacterAction0485E0(GameMoveActionConstructionState state,Invocation i){
   if(i==null)throw new ArgumentNullException("invocation");
   i.At=0x0485e7;Require(state,i.At).Virtual00.Write(GameMoveActionConstructionTable.Engine339EF0);
   // The observed RAX is the table address loaded by LEA, not original this.
   i.At=0x0485ea;return GameMoveActionConstructionTable.Engine339EF0;
  }
  public static MoveActionRecord Destroy088EB0(GameMoveActionConstructionState state,uint flags,GameMoveActionDestructionInvocation c){
   if(c==null)throw new ArgumentNullException("invocation");c.Reset(flags);var i=c.Native;
   i.At=0x088ece;
   if((flags&2u)!=0){i.At=0x088eda;throw new MissingContract(i.At,"Array cookie/stride88 vector destruction is outside the scalar domain");}
   if(flags>1)throw new MissingContract(i.At,"Only explicit scalar flags0 and1 are covered; no inferred higher flag bits");
   i.At=0x088f12;Require(state,i.At);var action=state.Action;
   state.Virtual00.Write(GameMoveActionConstructionTable.Game6E5710);c.Writes.Add(new GameMoveActionConstructionWrite(i.At,0,8));
   i.At=0x088f15;c.EngineEntered=true;
   c.EngineReturnedRax=DestroyCharacterAction0485E0(state,i);c.EngineReturned=true;c.Writes.Add(new GameMoveActionConstructionWrite(0x0485e7,0,8));
   i.At=0x088f1c;
   if((flags&1u)!=0){i.At=0x088f2a;c.FreeAttempted=true;GameMoveActionOwnHeap.ReleaseOwn(action,i.At);c.OwnStorageReleased=true;}
   i.At=0x088f30;c.ReturnedRax=action;c.Returned=true;i.At=0x088f42;return action;
  }
 }
}
