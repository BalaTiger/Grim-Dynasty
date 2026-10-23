using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Five new focused scenarios. Existing production DLLs are reused; no old test
// entrypoints or successful external-boundary delegates are invoked.
public static class EngineMouseGetterTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}} public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action action,string why){try{action();}catch(MissingContract error){Eq(at,error.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Invalid(Action action,string why){try{action();}catch(InvalidOperationException){Check(true,why);return;}throw new Exception("Expected binding rejection: "+why);}
 static void Unknown<T>(Field<T> field,string why){Missing(0,delegate{field.Read(0);},why);}
 static void Test(string name,Action action){try{action();scenarios++;}catch(Exception error){throw new Exception(name+": "+error.Message,error);}}
 sealed class Fixture {
  public readonly Controller Controller=new Controller(); public readonly MoveCharacter Move=new MoveCharacter();
  public readonly GameMovementCharacterState Owner; public readonly GameMovementIdentityBridge Bridge;
  public readonly MoveContext Movement=new MoveContext(); public readonly PlayerMouseContext Mouse;
  public readonly MouseRoleState Role; public readonly MouseControllerState MouseController;
  public readonly GameReachedGoalEngineState ManagerEngine=new GameReachedGoalEngineState();
  public readonly PlayerMousePlayerLookupContext Lookup; public readonly PlayerAliveState Alive;
  public readonly PlayerMouseOptionsBoundaries Boundaries;
  public readonly NavigationView GameEngineIdentity=new NavigationView();
  public readonly GameObjectManagerState Objects=new GameObjectManagerState(); public readonly GameObjectManagerOwnCriticalSection Lock=new GameObjectManagerOwnCriticalSection();
  public readonly PlayerMouseState State=new PlayerMouseState(new object()); public readonly MouseInputFrame Input=new MouseInputFrame(); public readonly WorldPosition24 Position=new WorldPosition24();
  public readonly WorldEngine Engine=new WorldEngine();public readonly EntityEngineState EngineLifetime;public readonly EngineOptionsState EngineOptions;
  public readonly NavigationView OptionsIdentity=new NavigationView();public readonly EngineOptionsObjectState Options;public readonly NavigationRefBuffer Values=new NavigationRefBuffer(14);
  public Fixture(){
   var character=new AddressSymbol<ClassRecord>(0xa5cba0);character.Write(new ClassRecord());var player=new AddressSymbol<ClassRecord>(0xa5cab0);player.Write(new ClassRecord());
   var callbacks=new GameMovementCallbackContext(new WorldCoordinateBoundaries(new WorldCoordinateState()),new EntityMovementRegistry(),character,player);
   Owner=new GameMovementCharacterState(new EntityMovementState(Move));callbacks.Attach(Owner);Bridge=new GameMovementIdentityBridge(callbacks);Bridge.Bind(Owner,Move);
   Mouse=new PlayerMouseContext(new MoveBoundaries(Movement));Role=new MouseRoleState(Move,Owner.Entity.Virtual00);MouseController=new MouseControllerState(Controller);Mouse.Attach(MouseController);
   Lookup=new PlayerMousePlayerLookupContext(Mouse,Bridge,ManagerEngine);Lookup.BindRole(Role);Alive=PlayerAliveState.Bind(Bridge,Move);Boundaries=new PlayerMouseOptionsBoundaries(new PlayerAliveContext(Lookup));
   Alive.Byte24B9.Write(0);Alive.Flags24B0.Write(2);GameObjectIdState.BindEntity(Owner.Entity).ObjectId2C.Write(0x77);
   Mouse.GameEngineA66288.Write(GameEngineIdentity);GamePlayerVideoState.Bind(GameEngineIdentity,GameEngineIdentity,new Field<object>()).Video19B0.Write(null);
   PlayerMouseControllerObjectFields.ForController(Controller).ObjectId30.Write(0x1234);Owner.Entity.Virtual00.Write(GameMovementPrimaryTable.Player);
   ManagerEngine.Critical421398.Write(new GameReachedGoalCriticalState());ManagerEngine.Instance4213A0.Write(Objects);
   var sentinel=new GameObjectManagerNode();var node=new GameObjectManagerNode();var buckets=new GameObjectManagerBuckets(1);
   Objects.OwnCritical18.Write(Lock);Objects.Sentinel38.Write(sentinel);Objects.Buckets48.Write(buckets);Objects.Mask60.Write(0);
   node.Key10.Write(0x1234);node.Object18.Write(Move);node.Next00.Write(sentinel);buckets.Cells[0].First00.Write(node);buckets.Cells[0].Last08.Write(node);
   Role.Byte242A.Write(0);Role.Byte4A70.Write(0);var hot=PlayerHotSlotState.Bind(Lookup,Role);hot.Key9C.Write(0);var head=new PlayerHotSlotTreeNode();head.Parent08.Write(head);head.Nil19.Write(1);hot.Header88.Write(head);
   var table=new PlayerMouseTable();table.BindObserved(ObservedPlayerMouseTable.Idle);State.Virtual00.Write(table);State.Owner08.Write(Controller);State.CachedRole10.Write(null);
   MouseController.Count2F0.Write(0);MouseController.State2A0.Write(State);MouseController.Byte431.Write(0);MouseController.Byte438.Write(7);MouseController.Dword43C.Write(99);
   Movement.EngineGlobal.Write(Engine);Input.Position.Write(Position);EngineLifetime=new EntityEngineState(Engine);EngineOptions=EngineOptionsState.Bind(EngineLifetime);Options=EngineOptionsObjectState.Bind(OptionsIdentity,OptionsIdentity);
  }
  public void SeedOptions(uint value){EngineOptions.Options3E0.Write(OptionsIdentity);Options.Integers48.Write(new OptionsIntPointer(Values,0));Values.Cells[13].Write(value);}
  public byte Outer(Invocation call){return PlayerMouseContracts.HandleActionFromMouse15E1F0(Controller,1,1,0,Input,Boundaries,call);}
 }
 public static void Run(){
  Test("Unparented LEA borrows the exact existing64-byte coordinate view without consuming payload",delegate{
   var entity=new EntityMovementState(new object());var call=new Invocation();var result=EngineMouseGetterContracts.GetCoordsUnparented00BBE0(entity,call);
   Same(entity.CoordsD8,result,"Exact coordinate object identity");Same(entity.CoordsD8.Position00,result.Position00,"Exact position fields");Eq(0x00bbe7u,call.At,"Real RET address");Unknown(entity.Parent130,"Parent pointer never read");Unknown(result.Position00.Q0,"LEA preserves unknown region");Unknown(result.Basis18[0],"LEA preserves unknown basis");
   result.Position00.D8.Write(0x3f800000);Eq(0x3f800000u,entity.CoordsD8.Position00.D8.Read(0),"Borrowed mutation immediately visible");entity.CoordsD8.Position00.D8.Write(0x40000000);Eq(0x40000000u,result.Position00.D8.Read(0),"Original mutation visible without sync");
   entity.MarkReleased();Same(result,EngineMouseGetterContracts.GetCoordsUnparented00BBE0(entity,call),"LEA still only forms borrowed address");Missing(0x77,delegate{result.RequireLive(0x77);},"Consumer rejects released coordinates");Missing(0x00bbe0,delegate{EngineMouseGetterContracts.GetCoordsUnparented00BBE0(null,call);},"Null own typed address has no synthetic D8 object");
  });
  Test("Options hot getter shares one engine3E0 cell and never fills unknown or cold state",delegate{
   var engine=new WorldEngine();var lifetime=new EntityEngineState(engine);var call=new Invocation();Missing(0x073677,delegate{EngineMouseGetterContracts.GetOptions073660(engine,call);},"Unbound engine rejected at first actual read");
   var state=EngineOptionsState.Bind(lifetime);Same(state,EngineOptionsState.Bind(lifetime),"Unique engine state reused");Invalid(delegate{EngineOptionsState.Bind(new EntityEngineState(engine));},"Mirror lifetime/cell refused");Unknown(state.Options3E0,"Binding does not initialize3E0");
   Missing(0x073677,delegate{EngineMouseGetterContracts.GetOptions073660(engine,call);},"Unknown3E0 is not null");state.Options3E0.Write(null);Missing(0x073686,delegate{EngineMouseGetterContracts.GetOptions073660(engine,call);},"Cold allocator is the first missing call");Same(null,state.Options3E0.Read(0),"Cold stop retains null");
   var first=new object();state.Options3E0.Write(first);Same(first,EngineMouseGetterContracts.GetOptions073660(engine,call),"Hot getter returns borrowed opaque identity");Eq(0x0736f6u,call.At,"Actual hot return");var second=new object();state.Options3E0.Write(second);Same(second,EngineMouseGetterContracts.GetOptions073660(engine,call),"Next call observes replaced3E0");Unknown(engine.World50,"No unrelated World50 initialization");
   lifetime.MarkReleased();Missing(0x073677,delegate{EngineMouseGetterContracts.GetOptions073660(engine,call);},"Existing engine lifetime is authoritative");
  });
  Test("Options integer getter sign-extends EDX and reads live DWORD storage without clamping",delegate{
   var id=new NavigationView();var state=EngineOptionsObjectState.Bind(id,id);Same(state,EngineOptionsObjectState.Bind(id,id),"Unique48 cell");Invalid(delegate{EngineOptionsObjectState.Bind(id,new NavigationView());},"Options lifetime mirror refused");var call=new Invocation();
   Missing(0x182d67,delegate{EngineMouseGetterContracts.GetInt182D60(id,0,call);},"Unknown pointer consumed only at actual DWORD read");state.Integers48.Write(null);Missing(0x182d67,delegate{EngineMouseGetterContracts.GetInt182D60(id,0,call);},"Null pointer has no default integer");
   var storage=new NavigationRefBuffer(3);var pointer=new OptionsIntPointer(storage,1);state.Integers48.Write(pointer);storage.Cells[0].Write(0xffffffff);storage.Cells[1].Write(0x80000000);storage.Cells[2].Write(0x7fffffff);
   Eq(-1,EngineMouseGetterContracts.GetInt182D60(id,0xffffffff,call),"FFFFFFFF sign-extends to preceding cell");Eq(Int32.MinValue,EngineMouseGetterContracts.GetInt182D60(id,0,call),"DWORD result keeps signed bit pattern");Eq(Int32.MaxValue,EngineMouseGetterContracts.GetInt182D60(id,1,call),"Next cell");Eq(0x182d6au,call.At,"Actual RET");
   Missing(0x182d67,delegate{EngineMouseGetterContracts.GetInt182D60(id,0xfffffffe,call);},"Index minus2 is outside explicit storage");Missing(0x182d67,delegate{EngineMouseGetterContracts.GetInt182D60(id,0x80000000,call);},"IntMin is not converted to huge unsigned or clamped");Missing(0x182d67,delegate{EngineMouseGetterContracts.GetInt182D60(id,2,call);},"No endpoint clamp");
   storage.Cells[1].Write(17);Eq(17,EngineMouseGetterContracts.GetInt182D60(id,0,call),"Live cell reread");state.Integers48.Write(new OptionsIntPointer(storage,0));Eq(-1,EngineMouseGetterContracts.GetInt182D60(id,0,call),"Live48 pointer reread");storage.Cells[0].Forget();Missing(0x182d67,delegate{EngineMouseGetterContracts.GetInt182D60(id,0,call);},"Unknown DWORD propagates at load");
   storage.Cells[0].Write(5);storage.MarkReleased();Missing(0x182d67,delegate{EngineMouseGetterContracts.GetInt182D60(id,0,call);},"Storage lifetime enforced at consumer");id.MarkReleased();Missing(0x182d60,delegate{EngineMouseGetterContracts.GetInt182D60(id,0,call);},"Released Options identity precedes48 load");
  });
  Test("Actual mouse lookup alive empty-tree primary chain reaches real idle Move280 boundary",delegate{
   var f=new Fixture();f.SeedOptions(0);var current=new ActionRecord();var pending=new ActionRecord();f.Controller.CurrentA0.Write(current);f.Controller.PendingA8.Write(pending);var call=new Invocation();
   Missing(0x165cb0,delegate{f.Outer(call);},"Known0 option advances to real idle move target, not movement success");Eq(0x162265u,call.At,"Actual Move280 caller");Eq(0x0736f6u,f.Boundaries.LastOptionsInvocation.At,"Real getter returned");Eq(0x182d6au,f.Boundaries.LastOptionIntInvocation.At,"Real indexed read returned");
   Check(f.Boundaries.LastLookup.Returned&&f.Boundaries.LastLookup.Lookup.OwnLockExited,"Actual lookup returned and released own lock");Check(f.Boundaries.LastAlive.Returned&&f.Boundaries.LastAlive.VideoReturned,"Actual alive/video prefix returned");Eq(0,f.Lock.OwnOutstandingEnters,"No retained lock");Eq(1,f.Boundaries.DisplayedCalls,"Real empty HotSlot getter ran");Same(f.Move,f.State.CachedRole10.Read(0),"Same actor identity remains cached");
   Same(current,f.Controller.CurrentA0.Read(0),"Move stop did not replace current action");Same(pending,f.Controller.PendingA8.Read(0),"Move stop did not replace pending action");Unknown(f.Input.B4,"No boolean default supplied");Unknown(f.Input.ObjectId,"No targetID default supplied");Unknown(f.Position.Q0,"No target coordinate invented");Unknown(f.MouseController.Byte432,"Unsupported move never publishes success byte");
   f.Values.Cells[13].Write(1);Eq((byte)0,f.Outer(call),"Changed nonzero option takes actual no-move return");Eq((byte)0,f.MouseController.Byte432.Read(0),"Outer publishes actual0 selector return");f.Values.Cells[13].Write(0xffffffff);Eq((byte)0,f.Outer(call),"Negative signed option remains nonzero for this exact caller");Same(current,f.Controller.CurrentA0.Read(0),"No action manufactured for blocked option");
  });
  Test("Actual integrated failure prefix distinguishes engine cold state missing Options and unknown13",delegate{
   var f=new Fixture();var call=new Invocation();Missing(0x073677,delegate{f.Outer(call);},"Engine3E0 unknown remains explicit after prior real prefix");Check(f.Boundaries.LastAlive.Returned,"Alive prefix completed before getter stop");Eq(0x162238u,call.At,"Caller stops in GetOptions");Check(f.Boundaries.LastOptionIntInvocation==null,"No integer getter before Options return");
   f.EngineOptions.Options3E0.Write(null);Missing(0x073686,delegate{f.Outer(call);},"Cold branch never converted to hot state");f.EngineOptions.Options3E0.Write(new object());Missing(0x182d60,delegate{f.Outer(call);},"Opaque hot Options pointer still lacks48 view");Eq(0x162246u,call.At,"Caller reached actual integer call");
   f.EngineOptions.Options3E0.Write(f.OptionsIdentity);Missing(0x182d67,delegate{f.Outer(call);},"Unknown48 preserved");f.Options.Integers48.Write(new OptionsIntPointer(f.Values,0));Missing(0x182d67,delegate{f.Outer(call);},"Unknown element13 preserved");Unknown(f.Values.Cells[0],"No entire array zero fill");Unknown(f.MouseController.Byte432,"Failure did not publish selector success");
   f.Values.Cells[13].Write(0);Missing(0x165cb0,delegate{f.Outer(call);},"Explicitly supplied own13 now reaches next true boundary");f.EngineLifetime.MarkReleased();Missing(0x073677,delegate{f.Outer(call);},"Existing engine release stops at getter rather than stale successful identity");
  });
 }
}
public static class EngineMouseGetterValidationProgram {
 public static int Main(string[] args){try{if(args==null||args.Length!=0)throw new ArgumentException("Expected no arguments");EngineMouseGetterTests.Run();Console.WriteLine("{\"status\":\"passed-new-engine-mouse-getter-components-only\",\"scenarios\":"+EngineMouseGetterTests.Scenarios+",\"assertions\":"+EngineMouseGetterTests.Assertions+",\"old_test_suites_rerun\":0,\"native_math_calls\":0,\"system_library_loaded\":false,\"full_tick\":false,\"physical_mouse\":false,\"original_read\":false,\"original_executed\":false,\"backend_started\":false}");return 0;}catch(Exception error){Console.Error.WriteLine(error);return 1;}}
}
