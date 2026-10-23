using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// OWN publication fixtures only. The singleton, four-byte FNV lookup, class
// stubs, inheritance walk and existing Dispatch call are production bodies.
public static class ControllerAiLookupTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}} public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract e){Equal(at,e.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Unknown<T>(Field<T> value,string why){Missing(0,delegate{value.Read(0);},why);}
 static void Test(string name,Action body){try{body();scenarios++;Console.WriteLine("PASS "+name);}catch(Exception e){throw new Exception(name+": "+e.Message,e);}}
 sealed class Fixture {
  public readonly Controller Controller=new Controller();
  public readonly MoveCharacter Move=new MoveCharacter();
  public readonly ActionRecord Current=new ActionRecord(),Pending=new ActionRecord();
  public readonly ClassRecord CharacterClass=new ClassRecord(),PlayerClass=new ClassRecord();
  public readonly EntityMovementRegistry Entities=new EntityMovementRegistry();
  public readonly GameMovementCallbackContext Callbacks;
  public readonly GameMovementCharacterState Owner;
  public readonly GameMovementIdentityBridge Bridge;
  public readonly GameMovementIdentityView View;
  public readonly MoveContext Movement=new MoveContext();
  public readonly PlayerMouseContext Mouse;
  public readonly MouseRoleState MouseRole;
  public readonly PlayerMouseControllerObjectFields Fields;
  public readonly GameReachedGoalEngineState Engine=new GameReachedGoalEngineState();
  public readonly PlayerMousePlayerLookupContext Context;
  public readonly ControllerAiLookupBoundaries Boundaries;
  public readonly GameObjectManagerState Objects=new GameObjectManagerState();
  public readonly GameObjectManagerOwnCriticalSection Lock=new GameObjectManagerOwnCriticalSection();
  public readonly GameObjectManagerNode Sentinel=new GameObjectManagerNode(),Node=new GameObjectManagerNode();
  public readonly GameObjectManagerBuckets Buckets=new GameObjectManagerBuckets(1);
  public Fixture(){
   var character=new AddressSymbol<ClassRecord>(0xa5cba0);character.Write(CharacterClass);var player=new AddressSymbol<ClassRecord>(0xa5cab0);player.Write(PlayerClass);
   Callbacks=new GameMovementCallbackContext(new WorldCoordinateBoundaries(new WorldCoordinateState()),Entities,character,player);
   Owner=new GameMovementCharacterState(new EntityMovementState(Move));Callbacks.Attach(Owner);Bridge=new GameMovementIdentityBridge(Callbacks);View=Bridge.Bind(Owner,Move);
   Mouse=new PlayerMouseContext(new MoveBoundaries(Movement));Context=new PlayerMousePlayerLookupContext(Mouse,Bridge,Engine);
   MouseRole=new MouseRoleState(Move,Owner.Entity.Virtual00);Context.BindRole(MouseRole);Boundaries=new ControllerAiLookupBoundaries(Context);
   Fields=PlayerMouseControllerObjectFields.ForController(Controller);Controller.CurrentA0.Write(Current);Controller.PendingA8.Write(Pending);
  }
  public void Hot(bool hit){
   Fields.ObjectId30.Write(0x1234);Owner.Entity.Virtual00.Write(GameMovementPrimaryTable.Character);
   Engine.Critical421398.Write(new GameReachedGoalCriticalState());Engine.Instance4213A0.Write(Objects);
   Objects.OwnCritical18.Write(Lock);Objects.Sentinel38.Write(Sentinel);Objects.Buckets48.Write(Buckets);Objects.Mask60.Write(0);Buckets.Cells[0].First00.Write(Sentinel);
   if(hit){Node.Key10.Write(0x1234);Node.Object18.Write(Move);Node.Next00.Write(Sentinel);Buckets.Cells[0].First00.Write(Node);Buckets.Cells[0].Last08.Write(Node);}
  }
  public MoveCharacter Resolve(ControllerAiLookupInvocation call){return ControllerAiLookupContracts.GetAI078D40(Controller,Context,call);}
  public void RealLookup(ControllerAiLookupInvocation call,int nodes){
   Check(call.SingletonReturned&&call.LookupReturned&&call.Lookup.Returned,"Actual singleton and lookup completed");
   Check(call.Lookup.OwnLockEntered&&call.Lookup.OwnLockExited&&!call.Lookup.OwnCleanupReleased,"Actual OWN lock exited normally");
   Equal(0,Lock.OwnOutstandingEnters,"No outstanding OWN lock");Equal(4,call.Lookup.HashedBytes,"Real four-byte FNV");Equal(nodes,call.Lookup.VisitedNodes,"Real node traversal");
  }
  public void NoWrites(){Same(Current,Controller.CurrentA0.Read(0),"Current action unchanged");Same(Pending,Controller.PendingA8.Read(0),"Pending action unchanged");Unknown(Move.MovementDC0,"Resolver does not manufacture manager");}
 }
 public static void Run(){scenarios=assertions=0;
  Test("Character target accepts the canonical Character while Player resolver rejects it",delegate{
   var f=new Fixture();f.Hot(true);var call=new ControllerAiLookupInvocation();
   Same(f.Move,f.Resolve(call),"GetAI returns original Character");Same(f.Move,call.Result.Read(0),"Result shares identity");Same(f.Move,call.Lookup.Result.Read(0),"Hash output shares identity");Same(f.View,f.Bridge.ForMoveCharacter(f.Move,0),"Existing bridge unchanged");
   Same(f.Fields,PlayerMouseControllerObjectFields.ForController(f.Controller),"GetAI borrows the one Controller30 sidecar");Same(f.Movement,f.Boundaries.Context,"Adapter borrows existing MoveContext");Same(f.Owner.Entity.Virtual00,f.MouseRole.Virtual00,"Mouse and GetAI share the original vptr");
   Equal(0x1234u,call.CapturedObjectId.Read(0),"Exact Controller30 key");Equal(0x078d8cu,call.Native.At,"Successful return site");Check(call.ClassReturned&&call.Returned,"Class path returned");Unknown(f.CharacterClass.Parent10,"Exact Character match bypasses unknown parent");f.RealLookup(call,1);
   f.CharacterClass.Parent10.Write(null);Same(null,PlayerMousePlayerLookupContracts.Resolve128530(f.Controller,f.Context,new PlayerMousePlayerLookupInvocation()),"Existing Player resolver still rejects plain Character");f.NoWrites();
  });
  Test("Player direct and deeper Character ancestry use live original class cells",delegate{
   var f=new Fixture();f.Hot(true);f.Owner.Entity.Virtual00.Write(GameMovementPrimaryTable.Player);f.PlayerClass.Parent10.Write(f.CharacterClass);var call=new ControllerAiLookupInvocation();
   Same(f.Move,f.Resolve(call),"Actual Player stub plus Character parent accepted");f.RealLookup(call,1);
   var middle=new ClassRecord();middle.Parent10.Write(f.CharacterClass);f.PlayerClass.Parent10.Write(middle);Same(f.Move,f.Resolve(call),"Actual591480 traverses the new parent chain");Check(call.Returned&&call.ClassReturned,"Deep match completed");
   middle.Parent10.Write(null);Same(null,f.Resolve(call),"Updated incompatible parent rejects same role");Same(null,call.Result.Read(0),"Old successful result replaced");Equal(0x078d95u,call.Native.At,"Class rejection returns null");
   f.PlayerClass.Parent10.Forget();Missing(0x078d79,delegate{f.Resolve(call);},"Unknown parent consumed at actual TEST");Check(call.ClassReturned&&!call.Returned,"Class stub completes before missing parent");Unknown(call.Result,"Failure clears prior result");
   f.PlayerClass.Parent10.Write(middle);middle.Parent10.Write(middle);Missing(0x591484,delegate{f.Resolve(call);},"Own cyclic class domain stops rather than returns success");f.NoWrites();
  });
  Test("Sentinel miss and stored null return without vptr or role fields",delegate{
   var f=new Fixture();f.Hot(false);f.Owner.Entity.Virtual00.Forget();var call=new ControllerAiLookupInvocation();
   Same(null,f.Resolve(call),"Sentinel miss returns null");f.RealLookup(call,0);Check(call.Returned&&!call.ClassReturned,"Miss skips class");Equal(0x078d95u,call.Native.At,"Null return site");Unknown(f.Buckets.Cells[0].Last08,"Empty bucket never reads end");Unknown(f.Sentinel.Object18,"Sentinel payload is not used");
   f.Hot(true);f.Node.Object18.Write(null);Same(null,f.Resolve(call),"Stored null is also a real null result");f.RealLookup(call,1);Check(call.Returned&&!call.ClassReturned,"Null payload skips class");f.NoWrites();
  });
  Test("Unknown key and payload stop after the exact real singleton or lock prefix",delegate{
   var cold=new Fixture();cold.Engine.Critical421398.Write(null);var call=new ControllerAiLookupInvocation();
   Missing(0x01c05e,delegate{cold.Resolve(call);},"Cold allocation remains Unsupported before key hash");Unknown(call.CapturedObjectId,"Entry MOV retained unknown key");Check(!call.SingletonReturned&&!call.LookupReturned&&!call.Returned,"Cold path cannot claim return");Equal(0,call.Lookup.HashedBytes,"No key hash before singleton");
   var f=new Fixture();f.Hot(true);f.Fields.ObjectId30.Forget();Missing(0x019d81,delegate{f.Resolve(call);},"Hot hash consumes unknown key");Check(call.SingletonReturned&&!call.LookupReturned,"Hot singleton preceded unknown hash key");Check(call.Lookup.OwnLockEntered&&call.Lookup.OwnCleanupReleased&&!call.Lookup.OwnLockExited,"OWN cleanup released lock on unresolved hash key");Equal(0,f.Lock.OwnOutstandingEnters,"Cleanup releases actual lock");Unknown(call.Result,"Hash failure has no AI result");
   f.Fields.ObjectId30.Write(0x1234);f.Node.Object18.Forget();Missing(0x078d5c,delegate{f.Resolve(call);},"Caller TEST consumes unknown Object18 after lookup returns");f.RealLookup(call,1);Check(!call.ClassReturned&&!call.Returned,"Unknown payload never reaches class");Unknown(call.Lookup.Result,"Lookup preserves unknown pointer bits");Unknown(call.Result,"No successful AI result fabricated");f.NoWrites();
  });
  Test("Stale, unbound and unknown-vptr objects retain lookup prefix without new identities",delegate{
   var f=new Fixture();f.Hot(true);var call=new ControllerAiLookupInvocation();f.Owner.Entity.Virtual00.Forget();
   Missing(0x078d67,delegate{f.Resolve(call);},"Unknown vptr consumed at class call");f.RealLookup(call,1);Check(!call.ClassReturned&&!call.Returned,"Unknown vptr cannot supply class");
   f.Owner.Entity.Virtual00.Write(new object());Missing(0x078d67,delegate{f.Resolve(call);},"Unbound concrete class slot stops");
   f.Owner.Entity.Virtual00.Write(GameMovementPrimaryTable.Character);f.Owner.Entity.MarkReleased();Missing(0x078d61,delegate{f.Resolve(call);},"Released canonical owner fails at first dereference");f.RealLookup(call,1);Unknown(call.Result,"Stale owner cannot return");
   f.Node.Object18.Write(new MoveCharacter());Missing(0x078d61,delegate{f.Resolve(call);},"No synthetic bridge for unbound MoveCharacter");f.Node.Object18.Write(new object());Missing(0x078d61,delegate{f.Resolve(call);},"No fabricated MoveCharacter for unrelated payload");f.NoWrites();
  });
  Test("Concrete GetAI plugs into existing Dispatch and stops at the next genuine boundary",delegate{
   var f=new Fixture();f.Hot(true);Same(f.Move,f.Boundaries.GetAI(f.Controller),"Production adapter result is original role");f.RealLookup(f.Boundaries.LastLookup,1);
   var outcome=Dispatch.RunLocal(f.Controller,new ActionRecord(),new GrimDynasty.ReverseClient.Actions.Environment(),f.Boundaries);
   Check(!outcome.ReturnedNormally,"Unresolved action kind is not dispatch success");Equal(0x160111u,outcome.Address,"After actual GetAI, real CheckAction reaches Kind58 boundary");Same(f.Move,f.Boundaries.LastLookup.Result.Read(0),"Dispatch used actual GetAI result");f.RealLookup(f.Boundaries.LastLookup,1);f.NoWrites();
   var miss=new Fixture();miss.Hot(false);outcome=Dispatch.RunLocal(miss.Controller,new ActionRecord(),new GrimDynasty.ReverseClient.Actions.Environment(),miss.Boundaries);
   Equal(0x127e79u,outcome.Address,"Actual null AI stops at unclosed native logging");Check(!outcome.ReturnedNormally,"Missing-role logging is not invented");Same(null,miss.Boundaries.LastLookup.Result.Read(0),"Dispatch retained actual null lookup");miss.NoWrites();
  });
 }
}
public static class ControllerAiLookupValidationProgram {
 public static int Main(){try{ControllerAiLookupTests.Run();Console.WriteLine("{\"status\":\"passed-new-controller-ai-lookup-only\",\"scenarios\":"+ControllerAiLookupTests.Scenarios+",\"assertions\":"+ControllerAiLookupTests.Assertions+",\"old_test_suites_rerun\":0,\"native_math_calls\":0,\"physical_mouse\":false,\"full_tick\":false,\"system_library_loaded\":false,\"original_read\":false,\"original_executed\":false,\"backend_started\":false}");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
