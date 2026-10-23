using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Six new067170/05FC30/05B710 composition drafts only. The successful cases
// explicitly use an empty Handler current action, a real ObjectManager miss,
// and initialized event9 with null Lua. They are not full walking acceptance.
public static class GameReachedGoalTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}
 public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action action,string why){try{action();}catch(MissingContract e){Eq(at,e.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Unknown<T>(Field<T> value){Missing(0,delegate{value.Read(0);},"Unknown remains Unknown");}
 static void Test(string name,Action action){try{action();scenarios++;}catch(Exception e){throw new Exception(name+": "+e.Message,e);}}
 sealed class Fixture {
  public readonly MoveCharacter Move=new MoveCharacter();public readonly EntityMovementRegistry Entities=new EntityMovementRegistry();
  public readonly GameMovementCallbackContext Callbacks;public readonly GameMovementCharacterState Owner;public readonly GameMovementIdentityBridge Bridge;public readonly GameMovementIdentityView View;
  public readonly MoveContext MoveContext=new MoveContext();public readonly GameReachedGoalEngineState Engine=new GameReachedGoalEngineState();public readonly GameReachedGoalContext Context;
  public readonly Handler Handler=new Handler();public readonly MovementManager Movement=new MovementManager();
  public readonly GameObjectManagerState Objects=new GameObjectManagerState();public readonly GameObjectManagerOwnCriticalSection Lock=new GameObjectManagerOwnCriticalSection();
  public readonly GameObjectManagerNode Sentinel=new GameObjectManagerNode();public readonly GameObjectManagerBuckets Buckets=new GameObjectManagerBuckets(1);
  public readonly GameReachedGoalCharacterFields Fields;public readonly GameReachedGoalHookPackState Hooks;
  public Fixture(){
   var character=new AddressSymbol<ClassRecord>(0xa5cba0);character.Write(new ClassRecord());var player=new AddressSymbol<ClassRecord>(0xa5cab0);player.Write(new ClassRecord());
   Callbacks=new GameMovementCallbackContext(new WorldCoordinateBoundaries(new WorldCoordinateState()),Entities,character,player);Owner=new GameMovementCharacterState(new EntityMovementState(Move));Callbacks.Attach(Owner);Bridge=new GameMovementIdentityBridge(Callbacks);View=Bridge.Bind(Owner,Move);
   MoveContext.CharacterClass.Write(Callbacks.CharacterClass.Read(0));Context=new GameReachedGoalContext(Bridge,Engine,MoveContext);Fields=GameReachedGoalCharacterFields.ForOwner(Owner);Hooks=Fields.HookPack45F0;Context.BindHookPack(View,Hooks);
  }
  // Explicit OWN setup. No native constructor, populated loaded action list,
  // singleton initialization, registration or hook loading is inferred.
  public void EmptyHandlerHot(uint action){
   Owner.Entity.Virtual00.Write(GameMovementPrimaryTable.Character);Owner.ActionState24B4.Write(action);Move.Handler24C0.Write(Handler);Handler.Current08.Write(null);Move.MovementDC0.Write(Movement);Movement.Moving38.Write(1);Move.AssociatedId16C0.Write(17);
   Engine.Critical421398.Write(new GameReachedGoalCriticalState());Engine.Instance4213A0.Write(Objects);Objects.OwnCritical18.Write(Lock);Objects.Sentinel38.Write(Sentinel);Objects.Buckets48.Write(Buckets);Objects.Mask60.Write(0);Buckets.Cells[0].First00.Write(Sentinel);
   Hooks.Initialized1C.Write(1);Hooks.Records10.Write(new GameReachedGoalHookRecords(10));Hooks.Records10.Read(0).At(9,0).Lua40.Write(null);
  }
  public MoveActionRecord ActiveMove(){
   var action=new MoveActionRecord();action.OwnerId08.Write(0x1234);action.SkillReference7C.Write(0x89abcdef);action.Flags78.Write(0x17);action.Flag11.Write(1);var view=new GameReachedGoalActionView(action);view.Virtual00.Write(GameReachedGoalActionTable.MoveTo);Context.BindAction(view);Handler.Current08.Write(action);
   var node=new GameObjectManagerNode();node.Key10.Write(0x1234);node.Object18.Write(Move);node.Next00.Write(Sentinel);Buckets.Cells[0].First00.Write(node);Buckets.Cells[0].Last08.Write(node);return action;
  }
  public void Call(GameReachedGoalInvocation invocation){GameReachedGoalContracts.InvokeFromInterface(Owner.Interface38,Context,invocation);}
  public void StateOneMovingZero(){Eq(1u,Owner.ActionState24B4.Read(0),"Actual state setter published1");Eq(0u,Fields.Dword20A8.Read(0),"Actual setter cleared20A8");Eq((byte)0,Movement.Moving38.Read(0),"Original Movement Moving38 cleared");Same(Movement,Move.MovementDC0.Read(0),"Original Movement identity retained");Same(null,Handler.Current08.Read(0),"Explicit empty Handler remains empty");}
  public void NoActionOrMovementWrite(uint action){Eq(action,Owner.ActionState24B4.Read(0),"Original action state retained");Eq((byte)1,Movement.Moving38.Read(0),"Original Moving38 retained");Unknown(Fields.Dword20A8);}
  public void Miss(GameReachedGoalInvocation c){Check(c.SingletonReturned&&c.LookupReturned,"Real singleton and lookup returned");Check(c.Lookup.OwnLockEntered&&c.Lookup.OwnLockExited&&!c.Lookup.OwnCleanupReleased,"Actual OWN Monitor enter and normal exit");Eq(0,Lock.OwnOutstandingEnters,"Monitor released");Eq(4,c.Lookup.HashedBytes,"Real four-byte hash");Eq(0,c.Lookup.VisitedNodes,"Actual sentinel miss");Eq(0ul,c.Lookup.BucketIndex.Read(0),"Explicit mask selects bucket0");Same(null,c.Lookup.Result.Read(0),"Actual lookup miss result");Unknown(Buckets.Cells[0].Last08);Unknown(Sentinel.Object18);}
 }
 public static void Run(){scenarios=assertions=0;
  Test("Conditional empty-handler hot domain performs setter movement clear real miss and inactive event9",delegate{
   foreach(uint action in new uint[]{5,6}){var f=new Fixture();f.EmptyHandlerHot(action);var c=new GameReachedGoalInvocation();f.Call(c);f.StateOneMovingZero();f.Miss(c);Check(c.CharacterStopReturned&&!c.ActionStopReturned,"Character stop returned without claiming an action End call");Check(c.MovementCleared&&c.HookReturned&&c.Returned,"Conditional suffix completed");Same(f.Movement,c.CapturedMovement.Read(0),"Captured original manager");Eq(17u,c.CapturedAssociatedId.Read(0),"Captured original associated ID");Eq(0x0ec0cau,c.Native.At,"Actual tail-jump hook return");Same(f.Move,f.Owner.Entity.Identity,"Full owner retains same Move identity");Same(f.Owner,f.Callbacks.Owner(f.Owner.Interface38,0),"Canonical callback owner");Same(f.View,f.Bridge.ForMoveCharacter(f.Move,0),"Unique bidirectional bridge");Unknown(f.Movement.Owner00);Unknown(f.Movement.Registered6C);Unknown(f.Hooks.Records10.Read(0).At(9,0).Type60);Unknown(f.Hooks.Records10.Read(0).At(0,0).Lua40);Unknown(f.Engine.Critical421398.Read(0).Native10);}
  });
  Test("Unique20A8 sidecar starts unknown and actual setter writes both authoritative cells",delegate{
   var f=new Fixture();var again=GameReachedGoalCharacterFields.ForOwner(f.Owner);Same(f.Fields,again,"One sidecar per owner");Same(f.Fields.Dword20A8,again.Dword20A8,"Same actual20A8 cell");Same(f.Hooks,again.HookPack45F0,"Same embedded HookPack");Unknown(f.Fields.Dword20A8);Unknown(f.Owner.ActionState24B4);Unknown(f.Hooks.Initialized1C);Unknown(f.Hooks.Records10);
   var i=new Invocation();GameReachedGoalContracts.SetActionState05B710(f.View,7,i);Eq(7u,f.Owner.ActionState24B4.Read(0),"Actual state write");Eq(0u,again.Dword20A8.Read(0),"Alias sees actual20A8 clear");Eq(0x05b720u,i.At,"Observed setter return");Unknown(f.Owner.Entity.Virtual00);Unknown(f.Move.Handler24C0);Unknown(f.Move.MovementDC0);
  });
  Test("Unknown primary action handler and movement stop at exact reads with their real prefixes",delegate{
   var f=new Fixture();f.EmptyHandlerHot(5);f.Owner.Entity.Virtual00.Forget();var c=new GameReachedGoalInvocation();Missing(0x067185,delegate{f.Call(c);},"First primary virtual call target");f.NoActionOrMovementWrite(5);Check(!c.CharacterStopReturned&&!c.MovementCleared,"No prefix writes after unknown primary");
   f=new Fixture();f.EmptyHandlerHot(5);f.Owner.ActionState24B4.Forget();c=new GameReachedGoalInvocation();Missing(0x05b730,delegate{f.Call(c);},"Actual action getter");Unknown(f.Owner.ActionState24B4);Unknown(f.Fields.Dword20A8);Eq((byte)1,f.Movement.Moving38.Read(0),"Moving retained after unknown action");
   f=new Fixture();f.EmptyHandlerHot(5);f.Move.Handler24C0.Forget();c=new GameReachedGoalInvocation();Missing(0x05fc44,delegate{f.Call(c);},"Handler pointer dereference");f.NoActionOrMovementWrite(5);
   f=new Fixture();f.EmptyHandlerHot(6);f.Handler.Current08.Forget();c=new GameReachedGoalInvocation();Missing(0x05fc48,delegate{f.Call(c);},"Captured current action TEST");f.NoActionOrMovementWrite(6);
   f=new Fixture();f.EmptyHandlerHot(5);f.Move.MovementDC0.Forget();c=new GameReachedGoalInvocation();Missing(0x067204,delegate{f.Call(c);},"Moving store requires real captured manager");Eq(1u,f.Owner.ActionState24B4.Read(0),"Setter prefix retained before missing Movement");Eq(0u,f.Fields.Dword20A8.Read(0),"20A8 setter prefix retained");Eq((byte)1,f.Movement.Moving38.Read(0),"Unreachable manager not silently cleared");Unknown(c.CapturedMovement);Check(c.CharacterStopReturned&&!c.MovementCleared&&!c.SingletonReturned,"Stop returned but movement and singleton did not");
  });
  Test("Hook initialization and active Lua boundaries retain already published stop prefixes",delegate{
   for(int kind=0;kind<3;kind++){var f=new Fixture();f.EmptyHandlerHot(5);if(kind==0)f.Hooks.Initialized1C.Forget();else if(kind==1)f.Hooks.Initialized1C.Write(0);else{var record=f.Hooks.Records10.Read(0).At(9,0);record.Lua40.Write(new object());record.Type60.Write(0);}var c=new GameReachedGoalInvocation();uint at=kind==0?0x0ec01fu:kind==1?0x0ec02au:0x0ec06du;Missing(at,delegate{f.Call(c);},"Actual Hook boundary");f.StateOneMovingZero();f.Miss(c);Check(c.CharacterStopReturned&&c.MovementCleared&&!c.ActionStopReturned,"Actual prior conditional stop retained");Check(!c.HookReturned&&!c.Returned,"Uninitialized/active hook is not fake completion");}
  });
  Test("Reused successful invocation clears diagnostics before unknown entry target",delegate{
   var f=new Fixture();f.EmptyHandlerHot(6);var c=new GameReachedGoalInvocation();f.Call(c);Check(c.Returned&&c.HookReturned&&c.Lookup.Returned,"Initial conditional completion");f.Owner.Entity.Virtual00.Forget();Missing(0x067185,delegate{f.Call(c);},"Second entry primary unknown");Check(!c.ActionStopReturned&&!c.CharacterStopReturned&&!c.MovementCleared&&!c.SingletonReturned&&!c.LookupReturned&&!c.HookReturned&&!c.Returned,"Every prior completion flag cleared");Unknown(c.CapturedMovement);Unknown(c.CapturedAssociatedId);Check(!c.Lookup.Returned&&!c.Lookup.OwnLockEntered&&!c.Lookup.OwnLockExited&&!c.Lookup.OwnCleanupReleased,"Nested lookup diagnostics cleared");Eq(0,c.Lookup.HashedBytes,"Old hash progress cleared");Eq(0,c.Lookup.VisitedNodes,"Old traversal progress cleared");Unknown(c.Lookup.Result);Unknown(c.Lookup.Hash);Unknown(c.Lookup.BucketIndex);f.StateOneMovingZero();
   // A direct nested helper call has a narrower diagnostic scope than a new
   // outer invocation: clear its own outcomes while retaining prior outer data.
   var nested=new Fixture();nested.EmptyHandlerHot(5);var nc=new GameReachedGoalInvocation();nested.Call(nc);GameReachedGoalContracts.CharacterStop05FC30(nested.View,nested.Context,nc);Check(nc.CharacterStopReturned&&!nc.ActionStopReturned,"Direct empty-handler CharacterStop returns");nested.Handler.Current08.Forget();Missing(0x05fc48,delegate{GameReachedGoalContracts.CharacterStop05FC30(nested.View,nested.Context,nc);},"Reused direct CharacterStop current action Unknown");Check(!nc.ActionStopReturned&&!nc.CharacterStopReturned,"Direct helper clears its own prior results before failure");Check(nc.MovementCleared&&nc.SingletonReturned&&nc.LookupReturned&&nc.HookReturned&&nc.Returned,"Direct helper retains unrelated earlier outer diagnostic results");Check(nc.Lookup.Returned&&nc.Lookup.OwnLockExited,"Direct helper retains nested lookup diagnostics");Same(nested.Movement,nc.CapturedMovement.Read(0),"Direct helper retains outer manager capture");Eq(17u,nc.CapturedAssociatedId.Read(0),"Direct helper retains outer associated ID capture");Eq(1u,nested.Owner.ActionState24B4.Read(0),"Earlier setter prefix retained");Eq((byte)0,nested.Movement.Moving38.Read(0),"Earlier actual movement clear retained");
  });
  Test("Real active MoveTo End reaches actual owner lookup and SkillId boundary without clearing action",delegate{
   var released=new Fixture();released.EmptyHandlerHot(5);var dead=released.ActiveMove();dead.MarkReleased();var c=new GameReachedGoalInvocation();Missing(0x05fc4d,delegate{released.Call(c);},"Released action before vptr load");Same(dead,released.Handler.Current08.Read(0),"Released action remains original current");released.NoActionOrMovementWrite(5);Check(!c.ActionStopReturned&&!c.CharacterStopReturned&&!c.MovementCleared,"Released action is not treated as a successful End");Eq((byte)1,dead.Flag11.Read(0),"Released action flag preserved");
   var f=new Fixture();f.EmptyHandlerHot(5);var active=f.ActiveMove();c=new GameReachedGoalInvocation();Missing(0x531c50,delegate{f.Call(c);},"Real MoveContracts.Stop SkillReference to ID dependency");Same(active,f.Handler.Current08.Read(0),"Current remains same live MoveTo action");f.NoActionOrMovementWrite(5);Check(!c.ActionStopReturned&&!c.CharacterStopReturned&&!c.MovementCleared&&!c.SingletonReturned&&!c.HookReturned&&!c.Returned,"Active End failure cannot be promoted to arrival completion");Eq((byte)1,active.Flag11.Read(0),"End has not reached its final flag clear");Eq(0x1234u,active.OwnerId08.Read(0),"Actual owner lookup key preserved");Eq(0x89abcdefu,active.SkillReference7C.Read(0),"Skill reference consumed without guessed ID");Eq(0x17u,active.Flags78.Read(0),"Actual flags retained");Eq(0,f.Lock.OwnOutstandingEnters,"Actual owner lookup released OWN Monitor before skill stop");Same(f.Move,f.Buckets.Cells[0].First00.Read(0).Object18.Read(0),"Lookup object is same shared MoveCharacter");Same(f.Callbacks.CharacterClass.Read(0),f.MoveContext.CharacterClass.Read(0),"Actual Class0 getter and Move expected class share symbolic identity");Unknown(f.Callbacks.CharacterClass.Read(0).Parent10);Unknown(f.Move.SpeedFlag26B3);Unknown(f.Move.Sound2210);Unknown(f.Movement.Registered6C);Unknown(c.CapturedMovement);Unknown(c.CapturedAssociatedId);
  });
 }
}
