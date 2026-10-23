using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// OWN hot-state fixtures. No original startup, successful getter delegate,
// cold client construction, physical mouse or game-runtime claim.
public static class GameMainPlayerTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}public static int Assertions{get{return assertions;}}
 static void Check(bool v,string why){assertions++;if(!v)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action action,string why){try{action();}catch(MissingContract e){Equal(at,e.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Unknown<T>(Field<T> field,string why){Missing(0,delegate{field.Read(0);},why);}
 static void Invalid(Action action,string why){try{action();}catch(InvalidOperationException){Check(true,why);return;}throw new Exception("Expected binding rejection: "+why);}
 static void Test(string name,Action action){try{action();scenarios++;Console.WriteLine("PASS "+name);}catch(Exception e){throw new Exception(name+": "+e.Message,e);}}
 sealed class Fixture {
  public readonly MoveCharacter Move=new MoveCharacter();
  public readonly ClassRecord CharacterClass=new ClassRecord(),PlayerClass=new ClassRecord();
  public readonly GameMovementCharacterState Owner;
  public readonly GameMovementIdentityBridge Bridge;
  public readonly MoveContext Movement=new MoveContext();
  public readonly PlayerMouseContext Mouse;
  public readonly PlayerMousePlayerLookupContext Lookup;
  public readonly GameReachedGoalEngineState Engine=new GameReachedGoalEngineState();
  public readonly GameObjectManagerState Objects=new GameObjectManagerState();
  public readonly GameObjectManagerOwnCriticalSection Lock=new GameObjectManagerOwnCriticalSection();
  public readonly GameObjectManagerNode Sentinel=new GameObjectManagerNode(),Node=new GameObjectManagerNode();
  public readonly GameObjectManagerBuckets Buckets=new GameObjectManagerBuckets(1);
  public readonly NavigationView GameEngine=new NavigationView(),Record=new NavigationView();
  public readonly Field<object> Connections=new Field<object>();
  public readonly GamePlayerVideoConnection Connection=new GamePlayerVideoConnection();
  public readonly Field<uint> RecordId=new Field<uint>();
  public readonly GamePlayerVideoState Video;
  public readonly GameMainPlayerConnectionFields ConnectionFields;
  public readonly GameMainPlayerRecordFields RecordFields;
  public Fixture(){
   var character=new AddressSymbol<ClassRecord>(0xa5cba0);character.Write(CharacterClass);var player=new AddressSymbol<ClassRecord>(0xa5cab0);player.Write(PlayerClass);
   var callbacks=new GameMovementCallbackContext(new WorldCoordinateBoundaries(new WorldCoordinateState()),new EntityMovementRegistry(),character,player);
   Owner=new GameMovementCharacterState(new EntityMovementState(Move));callbacks.Attach(Owner);Bridge=new GameMovementIdentityBridge(callbacks);Bridge.Bind(Owner,Move);
   Mouse=new PlayerMouseContext(new MoveBoundaries(Movement));Lookup=new PlayerMousePlayerLookupContext(Mouse,Bridge,Engine);Lookup.BindRole(new MouseRoleState(Move,Owner.Entity.Virtual00));
   Video=GamePlayerVideoState.Bind(GameEngine,GameEngine,Connections);ConnectionFields=GameMainPlayerConnectionFields.ForConnection(Connection);RecordFields=GameMainPlayerRecordFields.Bind(Record,Record,RecordId);
  }
  public void Hot(bool hit){
   Connections.Write(Connection);Connection.Virtual00.Write(GameMainPlayerConnectionTable.Observed767C98);ConnectionFields.Record20.Write(Record);RecordId.Write(0x1234);
   Owner.Entity.Virtual00.Write(GameMovementPrimaryTable.Player);Engine.Critical421398.Write(new GameReachedGoalCriticalState());Engine.Instance4213A0.Write(Objects);
   Objects.OwnCritical18.Write(Lock);Objects.Sentinel38.Write(Sentinel);Objects.Buckets48.Write(Buckets);Objects.Mask60.Write(0);Buckets.Cells[0].First00.Write(Sentinel);
   if(hit){Node.Key10.Write(0x1234);Node.Object18.Write(Move);Node.Next00.Write(Sentinel);Buckets.Cells[0].First00.Write(Node);Buckets.Cells[0].Last08.Write(Node);}
  }
  public MoveCharacter Resolve(GameMainPlayerInvocation call){return GameMainPlayerContracts.GetMainPlayer2EBB70(GameEngine,Lookup,call);}
  public void RealLookup(GameMainPlayerInvocation call,int nodes){Check(call.SingletonReturned&&call.LookupReturned&&call.Lookup.Returned,"Actual singleton and hash returned");Check(call.Lookup.OwnLockEntered&&call.Lookup.OwnLockExited&&!call.Lookup.OwnCleanupReleased,"Real OWN lock normal exit");Equal(4,call.Lookup.HashedBytes,"Four native FNV bytes");Equal(nodes,call.Lookup.VisitedNodes,"Native node count");Equal(0,Lock.OwnOutstandingEnters,"No held lock");}
 }
 public static void Run(){scenarios=assertions=0;
  Test("Exact connection methods borrow identity and preserve opaque pointer state",delegate{
   var f=new Fixture();Unknown(f.ConnectionFields.Record20,"No client pointer created by binding");Unknown(f.RecordId,"No default zero record ID");Unknown(f.Connection.Virtual00,"No invented vptr");
   Same(f.ConnectionFields,GameMainPlayerConnectionFields.ForConnection(f.Connection),"One connection field owner");Same(f.RecordFields,GameMainPlayerRecordFields.Bind(f.Record,f.Record,f.RecordId),"One record view");Same(f.RecordId,f.RecordFields.ObjectId10,"Borrowed original DWORD cell");Same(f.Connections,f.Video.Connections40F8,"Borrowed existing40F8");
   Invalid(delegate{GameMainPlayerRecordFields.Bind(f.Record,f.Record,new Field<uint>());},"Reject second ID cell");
   var i=new Invocation();GameMainPlayerContracts.SetOwner089CB0(f.Connection,new Field<object>(),i);Equal(0x089cb4u,i.At,"Actual writer returns");Unknown(f.ConnectionFields.Owner08,"Opaque unknown argument preserved");
   var owner=new object();GameMainPlayerContracts.SetOwner089CB0(f.Connection,Field<object>.Of(owner),i);Same(owner,f.ConnectionFields.Owner08.Read(0),"Writer retains passed identity");Unknown(f.ConnectionFields.Record20,"Owner writer does not invent record");
   var captured=GameMainPlayerContracts.GetRecordBits0D04E0(f.Connection,i);Equal(0x0d04e4u,i.At,"Getter returned unknown bits");Unknown(captured,"No early pointer consumption");
   f.ConnectionFields.Record20.Write(f.Record);captured=GameMainPlayerContracts.GetRecordBits0D04E0(f.Connection,i);Same(f.Record,captured.Read(0),"Exact record identity returned");
   f.ConnectionFields.Record20.Write(null);Same(f.Record,captured.Read(0),"Previously returned pointer is a captured value");Same(null,GameMainPlayerContracts.GetRecordBits0D04E0(f.Connection,i).Read(0),"Next actual getter rereads changed record pointer");
  });
  Test("Zero ID returns after one real getter without lookup or global access",delegate{
   var f=new Fixture();f.Connections.Write(f.Connection);f.Connection.Virtual00.Write(GameMainPlayerConnectionTable.Observed767C98);f.ConnectionFields.Record20.Write(f.Record);f.RecordId.Write(0);
   var call=new GameMainPlayerInvocation();Same(null,GameMainPlayerContracts.GetMainPlayer2EBB70(f.GameEngine,null,call),"Actual first zero path returns null");Equal(1,call.Slot18Returns,"Exactly one getter on zero path");Check(call.Returned&&!call.SingletonReturned&&!call.LookupReturned&&!call.ClassReturned,"No lookup/class prefix on zero path");Equal(0x2ebbf1u,call.Native.At,"Early native return");Unknown(call.SecondConnection,"Second connection not loaded");Unknown(call.SecondRecord,"Second getter not called");Unknown(f.Mouse.GameEngineA66288,"Captured-engine function never reads global");
   f.RecordId.Forget();Missing(0x2ebb86,delegate{f.Resolve(call);},"Unknown ID stops at first CMP");Equal(1,call.Slot18Returns,"Getter completes before ID unknown");Unknown(call.Result,"Previous success cleared");Check(!call.Returned,"Unknown ID is not a zero value");
  });
  Test("Nonzero ID performs two real getters and returns the same canonical player",delegate{
   var f=new Fixture();f.Hot(true);var call=new GameMainPlayerInvocation();Same(f.Move,f.Resolve(call),"Same existing player returned");Equal(2,call.Slot18Returns,"Both separate slot calls completed");Same(f.Connection,call.FirstConnection.Read(0),"First shared connection");Same(f.Connection,call.SecondConnection.Read(0),"Second shared connection");Same(f.Record,call.FirstRecord.Read(0),"First real record identity");Same(f.Record,call.SecondRecord.Read(0),"Second real record identity");Equal(0x1234u,call.CapturedId.Read(0),"Second DWORD10 captured");Same(f.Move,call.Lookup.Result.Read(0),"Hash map returns exact role");Same(f.Move,call.Result.Read(0),"Result retains role identity");f.RealLookup(call,1);Unknown(f.PlayerClass.Parent10,"Exact Player class bypasses parent");Unknown(f.Mouse.GameEngineA66288,"No global needed");Unknown(f.Move.MovementDC0,"Lookup never manufactures movement");
   var otherRecord=new NavigationView();var otherId=Field<uint>.Of(0u);GameMainPlayerRecordFields.Bind(otherRecord,otherRecord,otherId);f.ConnectionFields.Record20.Write(otherRecord);Same(null,f.Resolve(call),"Next invocation sees replaced Record20");Equal(1,call.Slot18Returns,"Replacement zero path skips hash");Same(otherRecord,call.FirstRecord.Read(0),"New record identity visible");
   otherId.Write(0x1234);Same(f.Move,f.Resolve(call),"Live borrowed ID mutation is visible");Equal(2,call.Slot18Returns,"Two real calls on updated ID");
   var replacement=new GamePlayerVideoConnection();replacement.Virtual00.Write(GameMainPlayerConnectionTable.Observed767C98);GameMainPlayerConnectionFields.ForConnection(replacement).Record20.Write(f.Record);f.Connections.Write(replacement);Same(f.Move,f.Resolve(call),"Existing40F8 mutation is visible");Same(replacement,call.SecondConnection.Read(0),"No copied connection mirror");
  });
  Test("Real miss and Player class restriction use existing hash and inheritance bodies",delegate{
   var f=new Fixture();f.Hot(false);var call=new GameMainPlayerInvocation();Same(null,f.Resolve(call),"Native sentinel miss");f.RealLookup(call,0);Check(!call.ClassReturned,"Miss skips class");
   f.Hot(true);f.Node.Object18.Write(null);Same(null,f.Resolve(call),"Known null map payload");f.RealLookup(call,1);
   f.Node.Object18.Write(f.Move);f.Owner.Entity.Virtual00.Write(GameMovementPrimaryTable.Character);f.CharacterClass.Parent10.Write(null);Same(null,f.Resolve(call),"Plain Character rejected by Player target");Check(call.ClassReturned,"Real Character class stub ran");
   var middle=new ClassRecord();middle.Parent10.Write(f.PlayerClass);f.CharacterClass.Parent10.Write(middle);Same(f.Move,f.Resolve(call),"OWN class graph exercises actual591480");middle.Parent10.Write(null);Same(null,f.Resolve(call),"Live incompatible parent rejected");
   f.CharacterClass.Parent10.Forget();Missing(0x2ebbcc,delegate{f.Resolve(call);},"Unknown parent stops at TEST");Check(call.ClassReturned&&!call.Returned,"Known class prefix retained");f.CharacterClass.Parent10.Write(middle);middle.Parent10.Write(middle);Missing(0x591484,delegate{f.Resolve(call);},"Cyclic own class data does not fabricate success");
  });
  Test("Unknown, null and stale pointer prefixes never become successful empty records",delegate{
   var f=new Fixture();var call=new GameMainPlayerInvocation();Missing(0x2ebb80,delegate{f.Resolve(call);},"Unknown40F8 consumed at vptr dereference");f.Connections.Write(null);Missing(0x2ebb80,delegate{f.Resolve(call);},"Null connection not treated as no player");
   f.Connections.Write(f.Connection);Missing(0x2ebb83,delegate{f.Resolve(call);},"Unknown vptr consumed at CALL");f.Connection.Virtual00.Write(new object());Missing(0x2ebb83,delegate{f.Resolve(call);},"Unbound table cannot inject method");f.Connection.Virtual00.Write(GameMainPlayerConnectionTable.Observed767C98);
   Missing(0x2ebb86,delegate{f.Resolve(call);},"Unknown returned pointer stops at caller");Equal(1,call.Slot18Returns,"Getter really returned opaque unknown");f.ConnectionFields.Record20.Write(null);Missing(0x2ebb86,delegate{f.Resolve(call);},"Null record cannot imply zero ID");f.ConnectionFields.Record20.Write(new object());Missing(0x2ebb86,delegate{f.Resolve(call);},"Unbound record cannot get invented fields");
   f.ConnectionFields.Record20.Write(f.Record);f.RecordId.Write(0);f.Record.MarkReleased();Missing(0x2ebb86,delegate{f.Resolve(call);},"Stale record lifetime checked at ID use");
   f.Connection.MarkReleased();Missing(0x2ebb80,delegate{f.Resolve(call);},"Stale connection checked before vptr");f.GameEngine.MarkReleased();Missing(0x2ebb79,delegate{f.Resolve(call);},"Stale engine checked before40F8");
   var g=new Fixture();g.Hot(true);g.Engine.Critical421398.Write(null);Missing(0x01c05e,delegate{g.Resolve(call);},"Cold singleton stays Unsupported");Equal(2,call.Slot18Returns,"Both getters precede cold init");Equal(0x1234u,call.CapturedId.Read(0),"ID captured before cold init");Check(!call.SingletonReturned&&!call.LookupReturned&&!call.Returned,"No fabricated singleton or result");
   g.Engine.Critical421398.Write(new GameReachedGoalCriticalState());g.Node.Object18.Forget();Missing(0x2ebbaf,delegate{g.Resolve(call);},"Unknown hash payload consumed after lookup returns");g.RealLookup(call,1);Check(!call.Returned,"Unknown payload does not return null");
   g.Node.Object18.Write(g.Move);g.Owner.Entity.Virtual00.Forget();Missing(0x2ebbba,delegate{g.Resolve(call);},"Unknown role vptr at class call");g.Owner.Entity.MarkReleased();Missing(0x2ebbb4,delegate{g.Resolve(call);},"Same canonical role lifetime used");g.Node.Object18.Write(new MoveCharacter());Missing(0x2ebbb4,delegate{g.Resolve(call);},"No synthetic role bridge");
  });
 }
}
public static class GameMainPlayerValidationProgram {
 public static int Main(){try{GameMainPlayerTests.Run();Console.WriteLine("{\"status\":\"passed-new-game-main-player-hot-components-only\",\"scenarios\":"+GameMainPlayerTests.Scenarios+",\"assertions\":"+GameMainPlayerTests.Assertions+",\"old_test_suites_rerun\":0,\"native_math_calls\":0,\"physical_mouse\":false,\"full_tick\":false,\"system_library_loaded\":false,\"original_read\":false,\"original_executed\":false,\"backend_started\":false}");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
