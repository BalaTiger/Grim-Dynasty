using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// New128530 composition cases only. OWN fixtures publish explicit hot map and
// class cells; lookup, class matching and outer dispatch use actual bodies.
// No boundary overrides, successful getter callbacks or native setup claims.
public static class PlayerMousePlayerLookupTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}
 public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract e){Equal(at,e.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Unknown<T>(Field<T> field,string why){Missing(0,delegate{field.Read(0);},why);}
 static void Test(string name,Action body){try{body();scenarios++;Console.WriteLine("PASS "+name);}catch(Exception e){throw new Exception(name+": "+e.Message,e);}}

 sealed class Fixture {
  public readonly Controller Controller=new Controller();
  public readonly ActionRecord Current=new ActionRecord(),Pending=new ActionRecord();
  public readonly MoveCharacter Move=new MoveCharacter();
  public readonly EntityMovementRegistry Entities=new EntityMovementRegistry();
  public readonly ClassRecord CharacterClass=new ClassRecord(),PlayerClass=new ClassRecord();
  public readonly GameMovementCallbackContext Callbacks;
  public readonly GameMovementCharacterState Owner;
  public readonly GameMovementIdentityBridge Bridge;
  public readonly GameMovementIdentityView View;
  public readonly MoveContext Movement=new MoveContext();
  public readonly PlayerMouseContext Mouse;
  public readonly MouseControllerState MouseController;
  public readonly MouseRoleState Role;
  public readonly PlayerMouseControllerObjectFields Fields;
  public readonly GameReachedGoalEngineState Engine=new GameReachedGoalEngineState();
  public readonly PlayerMousePlayerLookupContext Context;
  public readonly PlayerMousePlayerLookupBoundaries Boundaries;
  public readonly MouseInputFrame Input=new MouseInputFrame();
  public readonly GameObjectManagerState Objects=new GameObjectManagerState();
  public readonly GameObjectManagerOwnCriticalSection Lock=new GameObjectManagerOwnCriticalSection();
  public readonly GameObjectManagerNode Sentinel=new GameObjectManagerNode(),Node=new GameObjectManagerNode();
  public readonly GameObjectManagerBuckets Buckets=new GameObjectManagerBuckets(1);
  public Fixture(){
   var character=new AddressSymbol<ClassRecord>(0xa5cba0);character.Write(CharacterClass);var player=new AddressSymbol<ClassRecord>(0xa5cab0);player.Write(PlayerClass);
   Callbacks=new GameMovementCallbackContext(new WorldCoordinateBoundaries(new WorldCoordinateState()),Entities,character,player);
   Owner=new GameMovementCharacterState(new EntityMovementState(Move));Callbacks.Attach(Owner);Bridge=new GameMovementIdentityBridge(Callbacks);View=Bridge.Bind(Owner,Move);
   Mouse=new PlayerMouseContext(new MoveBoundaries(Movement));MouseController=new MouseControllerState(Controller);Mouse.Attach(MouseController);
   Role=new MouseRoleState(Move,Owner.Entity.Virtual00);Context=new PlayerMousePlayerLookupContext(Mouse,Bridge,Engine);Context.BindRole(Role);Boundaries=new PlayerMousePlayerLookupBoundaries(Context);
   Fields=PlayerMouseControllerObjectFields.ForController(Controller);Controller.CurrentA0.Write(Current);Controller.PendingA8.Write(Pending);MouseController.Byte438.Write(7);MouseController.Dword43C.Write(83);
  }
  public void Hot(bool hit){
   Fields.ObjectId30.Write(0x1234);Owner.Entity.Virtual00.Write(GameMovementPrimaryTable.Player);
   Engine.Critical421398.Write(new GameReachedGoalCriticalState());Engine.Instance4213A0.Write(Objects);
   Objects.OwnCritical18.Write(Lock);Objects.Sentinel38.Write(Sentinel);Objects.Buckets48.Write(Buckets);Objects.Mask60.Write(0);Buckets.Cells[0].First00.Write(Sentinel);
   if(hit){Node.Key10.Write(0x1234);Node.Object18.Write(Move);Node.Next00.Write(Sentinel);Buckets.Cells[0].First00.Write(Node);Buckets.Cells[0].Last08.Write(Node);}
  }
  public MoveCharacter Resolve(PlayerMousePlayerLookupInvocation call){return PlayerMousePlayerLookupContracts.Resolve128530(Controller,Context,call);}
  public byte Outer(){return PlayerMouseContracts.HandleActionFromMouse15E1F0(Controller,0,0,0,Input,Boundaries,new Invocation());}
  public void CompletedLookup(PlayerMousePlayerLookupInvocation call,int nodes){
   Check(call.SingletonReturned&&call.LookupReturned&&call.Lookup.Returned,"Real singleton and hash lookup returned");
   Check(call.Lookup.OwnLockEntered&&call.Lookup.OwnLockExited&&!call.Lookup.OwnCleanupReleased,"Hash lookup exited its actual OWN lock normally");
   Equal(0,Lock.OwnOutstandingEnters,"OWN lock is released");Equal(4,call.Lookup.HashedBytes,"Actual hash consumed all four key bytes");Equal(nodes,call.Lookup.VisitedNodes,"Actual bucket traversal count");
  }
  public void NoControllerWrites(){Same(Current,Controller.CurrentA0.Read(0),"Current action identity retained");Same(Pending,Controller.PendingA8.Read(0),"Pending action identity retained");Equal((byte)7,MouseController.Byte438.Read(0),"Controller438 retained");Equal(83u,MouseController.Dword43C.Read(0),"Controller43C retained");}
 }

 public static void Run(){scenarios=assertions=0;
  Test("Real hot hash hit and Player class stub return the original canonical MoveCharacter",delegate{
   var f=new Fixture();f.Hot(true);var call=new PlayerMousePlayerLookupInvocation();
   Same(f.Move,f.Resolve(call),"Lookup returns the original Move identity");Same(f.Move,call.Result.Read(0),"Public result preserves Move identity");Same(f.Move,call.Lookup.Result.Read(0),"Hash lookup returns the same object");Equal(0x1234u,call.CapturedObjectId.Read(0),"Controller30 supplies the captured key");
   Check(call.ClassReturned&&call.Returned,"Actual Player class comparison completed");Equal(0x128584u,call.Native.At,"Successful native return address");Equal(0ul,call.Lookup.BucketIndex.Read(0),"Explicit mask selects the actual bucket");f.CompletedLookup(call,1);
   Same(f.View,f.Bridge.ForMoveCharacter(f.Move,0),"Existing bridge remains canonical");Same(f.Role,f.Mouse.Role(f.Move,0),"Mouse view uses the same role identity");Unknown(f.PlayerClass.Parent10,"Exact Player match never consumes its unknown parent");f.NoControllerWrites();
  });
  Test("Real sentinel miss lets outer return false before class or mouse frame consumption",delegate{
   var f=new Fixture();f.Hot(false);f.Owner.Entity.Virtual00.Forget();
   Equal((byte)0,f.Outer(),"Actual resolver miss produces outer false");var call=f.Boundaries.LastLookup;f.CompletedLookup(call,0);Same(null,call.Result.Read(0),"Resolver publishes the real null result");Check(call.Returned&&!call.ClassReturned,"Miss returns without a class call");Equal(0x12858cu,call.Native.At,"Null native return address");
   Unknown(f.Role.Virtual00,"Miss bypasses unknown class vptr");Unknown(f.Input.Position,"Outer never consumes position after null role");Unknown(f.Input.B4,"Outer never consumes B4 after null role");Unknown(f.Input.ObjectId,"No false-role cleanup consumes the frame ID pointer");Unknown(f.Buckets.Cells[0].Last08,"Sentinel miss does not consume last node");Unknown(f.Sentinel.Object18,"Sentinel payload remains unknown");f.NoControllerWrites();
  });
  Test("Unique controller key and borrowed vptr expose late table and class-parent changes",delegate{
   var f=new Fixture();var again=PlayerMouseControllerObjectFields.ForController(f.Controller);Same(f.Fields,again,"One controller sidecar");Same(f.Fields.ObjectId30,again.ObjectId30,"One authoritative Controller30 cell");Same(f.Controller,again.Controller,"Sidecar retains original Controller");Same(f.Owner.Entity.Virtual00,f.Role.Virtual00,"Role borrows the original Entity vptr cell");Unknown(again.ObjectId30,"New key cell starts unknown");
   f.Hot(true);var call=new PlayerMousePlayerLookupInvocation();Same(f.Move,f.Resolve(call),"Initial Player table matches");
   f.Role.Virtual00.Write(GameMovementPrimaryTable.Character);f.CharacterClass.Parent10.Write(null);Same(GameMovementPrimaryTable.Character,f.Owner.Entity.Virtual00.Read(0),"Role write changes the canonical vptr");Same(null,f.Resolve(call),"Next lookup reads changed Character table");Check(call.ClassReturned&&call.Returned,"Character stub completed before class rejection");Same(null,call.Result.Read(0),"Rejected class replaces earlier successful result");
   f.CharacterClass.Parent10.Forget();Missing(0x128569,delegate{f.Resolve(call);},"Unknown class-parent MOV is consumed at TEST");Check(call.ClassReturned&&!call.Returned,"Class stub returned before parent-test failure");Unknown(call.Result,"Prior result is cleared before failed reuse");
   var middle=new ClassRecord();middle.Parent10.Write(f.PlayerClass);f.CharacterClass.Parent10.Write(middle);Same(f.Move,f.Resolve(call),"Actual591480 accepts the now-published ancestor chain");Check(call.ClassReturned&&call.Returned,"Ancestor match completes the resolver");f.CompletedLookup(call,1);f.NoControllerWrites();
  });
  Test("Unknown Object18 survives lookup return and lock release until caller TEST12854C",delegate{
   var f=new Fixture();f.Hot(true);f.Node.Object18.Forget();var call=new PlayerMousePlayerLookupInvocation();
   Missing(0x12854c,delegate{f.Resolve(call);},"Caller consumes unknown object bits only at TEST");f.CompletedLookup(call,1);Equal(0x12854cu,call.Native.At,"Failure belongs to the caller TEST");Check(!call.ClassReturned&&!call.Returned,"No class or resolver completion invented");Unknown(call.Lookup.Result,"Hash result preserves unknown payload");Unknown(call.Result,"Resolver result is unpublished");Unknown(f.Node.Object18,"Original payload remains unknown");Equal(0x1234u,call.CapturedObjectId.Read(0),"Key capture completed before payload failure");f.NoControllerWrites();
  });
  Test("Cold singleton stops before unknown key consumption and hot adapter still stops at actual CanAct",delegate{
   var cold=new Fixture();cold.Engine.Critical421398.Write(null);var call=new PlayerMousePlayerLookupInvocation();
   Missing(0x01c05e,delegate{cold.Resolve(call);},"Actual cold critical-section allocation remains Unsupported");Unknown(cold.Fields.ObjectId30,"Cold engine wins over unknown Controller30");Unknown(call.CapturedObjectId,"Entry MOV preserves unknown key");Check(!call.SingletonReturned&&!call.LookupReturned&&!call.ClassReturned&&!call.Returned,"No cold completion is invented");Check(!call.Lookup.OwnLockEntered&&!call.Lookup.Returned,"Hash lookup never entered");Equal(0,call.Lookup.HashedBytes,"No unknown key byte consumed");Unknown(call.Lookup.Result,"No hash result published");Unknown(cold.Engine.Instance4213A0,"Cold critical branch does not consume instance");cold.NoControllerWrites();
   var hot=new Fixture();hot.Hot(true);Missing(0x15e230,delegate{hot.Outer();},"Real resolver does not fabricate role virtual460");call=hot.Boundaries.LastLookup;hot.CompletedLookup(call,1);Same(hot.Move,call.Result.Read(0),"Real resolver completed before unsupported role call");Check(call.ClassReturned&&call.Returned,"Adapter used the actual class path");Unknown(hot.Role.Byte242A,"Unresolved CanAct precedes role242A");Unknown(hot.Input.Position,"Unresolved CanAct precedes position copy");Unknown(hot.Input.B4,"Unresolved CanAct precedes stack bool");hot.NoControllerWrites();
  });
 }
}
public static class PlayerMousePlayerLookupTestProgram {
 public static int Main(){try{PlayerMousePlayerLookupTests.Run();Console.WriteLine("PASS "+PlayerMousePlayerLookupTests.Scenarios+" scenarios, "+PlayerMousePlayerLookupTests.Assertions+" assertions; actual player lookup composition without successful external callbacks");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
