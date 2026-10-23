using System;

namespace GrimDynasty.ReverseClient.Actions {
 // Diagnostics belong to this invocation; controller/role/global storage is
 // borrowed from the already existing player-lookup composition, never copied.
 public sealed class ControllerAiLookupInvocation {
  public readonly Invocation Native=new Invocation();
  public GameObjectManagerLookupInvocation Lookup{get;private set;}
  public readonly Field<uint> CapturedObjectId=new Field<uint>();
  public readonly Field<MoveCharacter> Result=new Field<MoveCharacter>();
  public bool SingletonReturned,LookupReturned,ClassReturned,Returned;
  public ControllerAiLookupInvocation(){Lookup=new GameObjectManagerLookupInvocation();}
  internal void Reset(){Native.At=0;Lookup=new GameObjectManagerLookupInvocation();CapturedObjectId.Forget();Result.Forget();SingletonReturned=false;LookupReturned=false;ClassReturned=false;Returned=false;}
 }
 // Game078D40: saved r1-movement-action-path/13 assembly. The class target is
 // Character A5CBA0, not Player A5CAB0. Actual hot01C040, locked019D40 and
 // 591480 bodies are reused. OWN map publication, canonical views and stable
 // sequential lifetimes are explicit preconditions, not native startup proof.
 public static class ControllerAiLookupContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null own controller AI lookup view");return value;}
  static MoveCharacter Return(MoveCharacter value,ControllerAiLookupInvocation call){
   call.Native.At=value==null?0x078d95u:0x078d8cu;call.Result.Write(value);call.Returned=true;return value;
  }
  static ClassRecord ClassInfo(object captured,PlayerMousePlayerLookupContext context,Invocation i){
   // The existing primary-table identities bind the observed exact LEA/RET
   // stubs only; no caller-supplied class callback can manufacture a match.
   if(Object.ReferenceEquals(captured,GameMovementPrimaryTable.Player)){i.At=0x016d10;return context.Bridge.Callbacks.PlayerClass.Read(i.At);}
   if(Object.ReferenceEquals(captured,GameMovementPrimaryTable.Character)){i.At=0x019d20;return context.Bridge.Callbacks.CharacterClass.Read(i.At);}
   throw new MissingContract(i.At,"Controller AI lookup class slot0 target is unbound");
  }
  public static MoveCharacter GetAI078D40(Controller controller,PlayerMousePlayerLookupContext context,ControllerAiLookupInvocation invocation){
   var call=Need(invocation,0x078d46);call.Reset();var i=call.Native;var ctx=Need(context,0x078d46);
   i.At=0x078d46;call.CapturedObjectId.CopyFrom(PlayerMouseControllerObjectFields.ForController(Need(controller,i.At)).ObjectId30);
   i.At=0x078d49;object manager=GameReachedGoalEngineContracts.GetObjectManager01C040(ctx.Engine,i);call.SingletonReturned=true;
   i.At=0x078d54;var typedManager=manager as GameObjectManagerState;
   if(typedManager==null)throw new MissingContract(0x019d5f,"Published ObjectManager lacks the shared concrete hash-lookup view");
   var result=GameObjectManagerLookupContracts.LookupBits019D40(typedManager,call.CapturedObjectId,call.Lookup);call.LookupReturned=true;
   i.At=0x078d5c;object found=result.Read(i.At);if(found==null)return Return(null,call);
   i.At=0x078d61;var move=found as MoveCharacter;
   if(move==null)throw new MissingContract(i.At,"Lookup Object18 is not an existing canonical MoveCharacter");
   var view=ctx.Bridge.ForMoveCharacter(move,i.At);view.Owner.Entity.RequireLive(i.At);var table=view.Owner.Entity.Virtual00.Snapshot();
   i.At=0x078d67;var actual=ClassInfo(table.Read(i.At),ctx,i);call.ClassReturned=true;
   i.At=0x078d70;var expected=ctx.Bridge.Callbacks.CharacterClass.Read(i.At);
   if(Object.ReferenceEquals(actual,expected))return Return(move,call);
   i.At=0x078d75;var parentBits=Need(actual,i.At).Parent10.Snapshot();i.At=0x078d79;var parent=parentBits.Read(i.At);
   if(parent==null)return Return(null,call);
   i.At=0x078d7e;if(Object.ReferenceEquals(parent,expected))return Return(move,call);
   i.At=0x078d83;if(GameMovementCallbackContracts.IsDerivedFrom591480(parent,expected,i))return Return(move,call);
   return Return(null,call);
  }
 }
 // This concrete GetAI is usable by Dispatch.Local/CheckAction and the later
 // construction path. It retains MoveBoundaries' existing Move start/stop
 // methods and exact unresolved defaults; it invents no successful callbacks.
 public class ControllerAiLookupBoundaries:MoveBoundaries {
  public readonly PlayerMousePlayerLookupContext LookupContext;
  public ControllerAiLookupInvocation LastLookup{get;private set;}
  public ControllerAiLookupBoundaries(PlayerMousePlayerLookupContext context):base(Require(context).Mouse.Movement.Context){LookupContext=context;}
  static PlayerMousePlayerLookupContext Require(PlayerMousePlayerLookupContext context){if(context==null)throw new ArgumentNullException("context");return context;}
  public override Role GetAI(Controller controller){LastLookup=new ControllerAiLookupInvocation();return ControllerAiLookupContracts.GetAI078D40(controller,LookupContext,LastLookup);}
 }
}
