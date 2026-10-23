using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Five focused new scenarios. No successful boundary overrides. OWN setup is
// explicit; the integrated mouse call uses the passed resolver and HotSlot body.
public static class PlayerAliveTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}
 public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Eq<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract error){Eq(at,error.Address,why);return;}throw new Exception("Expected Unsupported: "+why);}
 static void Invalid(Action body,string why){try{body();}catch(InvalidOperationException){Check(true,why);return;}throw new Exception("Expected binding rejection: "+why);}
 static void Unknown<T>(Field<T> field,string why){Missing(0,delegate{field.Read(0);},why);}
 static void Test(string name,Action body){try{body();scenarios++;}catch(Exception error){throw new Exception(name+": "+error.Message,error);}}
 sealed class Fixture {
  public readonly Controller Controller=new Controller();
  public readonly MoveCharacter Move=new MoveCharacter();
  public readonly GameMovementCharacterState Owner;
  public readonly GameMovementIdentityBridge Bridge;
  public readonly MoveContext Movement=new MoveContext();
  public readonly PlayerMouseContext Mouse;
  public readonly MouseRoleState Role;
  public readonly MouseControllerState MouseController;
  public readonly GameReachedGoalEngineState ManagerEngine=new GameReachedGoalEngineState();
  public readonly PlayerMousePlayerLookupContext Lookup;
  public readonly PlayerAliveContext Context;
  public readonly PlayerAliveState Alive;
  public readonly PlayerMouseAliveBoundaries Boundaries;
  public readonly NavigationView EngineIdentity=new NavigationView();
  public readonly Field<object> Connections=new Field<object>();
  public readonly GamePlayerVideoState Video;
  public readonly GameObjectManagerState Objects=new GameObjectManagerState();
  public readonly GameObjectManagerOwnCriticalSection Lock=new GameObjectManagerOwnCriticalSection();
  public readonly PlayerMouseState State=new PlayerMouseState(new object());
  public readonly MouseInputFrame Input=new MouseInputFrame();
  public readonly WorldPosition24 Position=new WorldPosition24();
  public Fixture():this(true){}
  public Fixture(bool bindAlive){
   var character=new AddressSymbol<ClassRecord>(0xa5cba0);character.Write(new ClassRecord());var player=new AddressSymbol<ClassRecord>(0xa5cab0);player.Write(new ClassRecord());
   var callbacks=new GameMovementCallbackContext(new WorldCoordinateBoundaries(new WorldCoordinateState()),new EntityMovementRegistry(),character,player);
   Owner=new GameMovementCharacterState(new EntityMovementState(Move));callbacks.Attach(Owner);Bridge=new GameMovementIdentityBridge(callbacks);Bridge.Bind(Owner,Move);
   Mouse=new PlayerMouseContext(new MoveBoundaries(Movement));Role=new MouseRoleState(Move,Owner.Entity.Virtual00);MouseController=new MouseControllerState(Controller);Mouse.Attach(MouseController);
   Lookup=new PlayerMousePlayerLookupContext(Mouse,Bridge,ManagerEngine);Lookup.BindRole(Role);Context=new PlayerAliveContext(Lookup);if(bindAlive)Alive=PlayerAliveState.Bind(Bridge,Move);
   Boundaries=new PlayerMouseAliveBoundaries(Context);Video=GamePlayerVideoState.Bind(EngineIdentity,EngineIdentity,Connections);
  }
  public GameObjectIdState SeedAlive(){
   Alive.Byte24B9.Write(0);Alive.Flags24B0.Write(2);var id=GameObjectIdState.BindEntity(Owner.Entity);id.ObjectId2C.Write(0x77);Mouse.GameEngineA66288.Write(EngineIdentity);Video.Video19B0.Write(null);return id;
  }
  public void SeedOuter(){
   SeedAlive();PlayerMouseControllerObjectFields.ForController(Controller).ObjectId30.Write(0x1234);Owner.Entity.Virtual00.Write(GameMovementPrimaryTable.Player);
   ManagerEngine.Critical421398.Write(new GameReachedGoalCriticalState());ManagerEngine.Instance4213A0.Write(Objects);
   var sentinel=new GameObjectManagerNode();var node=new GameObjectManagerNode();var buckets=new GameObjectManagerBuckets(1);
   Objects.OwnCritical18.Write(Lock);Objects.Sentinel38.Write(sentinel);Objects.Buckets48.Write(buckets);Objects.Mask60.Write(0);
   node.Key10.Write(0x1234);node.Object18.Write(Move);node.Next00.Write(sentinel);buckets.Cells[0].First00.Write(node);buckets.Cells[0].Last08.Write(node);
   Role.Byte242A.Write(0);Role.Byte4A70.Write(0);var hot=PlayerHotSlotState.Bind(Lookup,Role);hot.Key9C.Write(0);var head=new PlayerHotSlotTreeNode();head.Parent08.Write(head);head.Nil19.Write(1);hot.Header88.Write(head);
   var table=new PlayerMouseTable();table.BindObserved(ObservedPlayerMouseTable.Idle);State.Virtual00.Write(table);State.Owner08.Write(Controller);State.CachedRole10.Write(null);
   MouseController.Count2F0.Write(0);MouseController.State2A0.Write(State);MouseController.Byte431.Write(0);MouseController.Byte438.Write(7);MouseController.Dword43C.Write(99);
   Movement.EngineGlobal.Write(new WorldEngine());Input.Position.Write(Position);
  }
  public byte Player(PlayerAliveInvocation call){return PlayerAliveContracts.PlayerIsAlive3CE7B0(Alive,Context,call);}
  public byte Outer(Invocation call){return PlayerMouseContracts.HandleActionFromMouse15E1F0(Controller,1,1,0,Input,Boundaries,call);}
 }
 public static void Run(){scenarios=assertions=0;
  Test("One shared alive state and actual Character mask do not copy action or engine fields",delegate{
   var f=new Fixture(false);Missing(0x05bb80,delegate{f.Boundaries.CanAct460(GameMovementPrimaryTable.Character,f.Move);},"Alive lookup cannot auto-create state");
   var state=PlayerAliveState.Bind(f.Bridge,f.Move);Same(state,PlayerAliveState.Bind(f.Bridge,f.Move),"Explicit binding is stable");Same(state,PlayerAliveState.ForBoundRole(f.Lookup,f.Move,0),"Original role resolves exact state");Same(f.Owner,state.Owner,"Full owner identity retained");Same(f.Move,state.Role,"Original Move retained");
   Same(f.Mouse.GameEngineA66288,f.Context.GameEngineA66288,"Global field is an alias");Same(f.Connections,f.Video.Connections40F8,"Engine40F8 is a borrowed cell");Unknown(state.Byte24B9,"No default alive byte");Unknown(state.Flags24B0,"No default flags");Unknown(f.Video.Video19B0,"Binding does not infer null video");Unknown(f.Owner.ActionState24B4,"24B4 is untouched");
   var call=new PlayerAliveInvocation();Missing(0x05bb80,delegate{PlayerAliveContracts.CharacterIsAlive05BB80(state,call);},"Character consumes24B9 first");state.Byte24B9.Write(9);Eq((byte)0,PlayerAliveContracts.CharacterIsAlive05BB80(state,call),"Nonzero byte returns false without flags");Eq(0x05bb8bu,call.Native.At,"Early Character return");Unknown(state.Flags24B0,"Early false bypassed unknown24B0");
   state.Byte24B9.Write(0);uint[] flags={0,2,1,4,0x80000000u};byte[] answers={1,1,0,0,0};for(int n=0;n<flags.Length;n++){state.Flags24B0.Write(flags[n]);Eq(answers[n],PlayerAliveContracts.CharacterIsAlive05BB80(state,call),"Actual mask result");Eq(0x05bb99u,call.Native.At,"Mask return address");}
   state.Flags24B0.Write(2);f.Owner.Entity.Virtual00.Write(GameMovementPrimaryTable.Player);Eq((byte)1,f.Boundaries.CanAct460(GameMovementPrimaryTable.Character,f.Move),"Dispatch honors captured Character table despite current Player vptr");Check(!f.Boundaries.LastAlive.ObjectIdReturned&&!f.Boundaries.LastAlive.VideoReturned,"Character path never requests ID/video");
   Missing(0x15e230,delegate{f.Boundaries.CanAct460(new object(),f.Move);},"Unknown captured table has no fallback");Check(!f.Boundaries.LastAlive.Returned,"Unknown dispatch does not retain prior return");
  });
  Test("Player uses actual ObjectId and explicit null-video return with fresh global and flags",delegate{
   var f=new Fixture();var id=f.SeedAlive();var call=new PlayerAliveInvocation();Eq((byte)1,f.Player(call),"Flags2 and actual null video permit player");Eq(0x3ce7f4u,call.Native.At,"Player mask return");Eq(0x03a743u,call.ObjectId.At,"Real ObjectId returned");Eq(0x2fa309u,call.Video.At,"Real null-video return");Check(call.ObjectIdReturned&&call.VideoReturned&&call.Returned,"All actual callees completed");Eq(0x77u,call.CapturedId.Read(0),"ID from original2C");Same(f.EngineIdentity,call.CapturedEngine.Read(0),"Original global identity captured");Unknown(f.Connections,"Null video did not read40F8");
   f.Alive.Flags24B0.Write(1);Eq((byte)0,f.Player(call),"Changed flags observed after actual video return");f.Alive.Flags24B0.Write(0);id.ObjectId2C.Write(0x88776655);
   var other=new NavigationView();var otherConnections=new Field<object>();var otherVideo=GamePlayerVideoState.Bind(other,other,otherConnections);otherVideo.Video19B0.Write(null);f.Video.Video19B0.Write(new object());f.Mouse.GameEngineA66288.Write(other);
   Eq((byte)1,f.Player(call),"Next call observes new explicit engine");Eq(0x88776655u,call.CapturedId.Read(0),"Next call observes changed ObjectId");Same(other,call.CapturedEngine.Read(0),"New global is captured without a mirror");Unknown(otherConnections,"New engine also bypasses40F8");
   f.Alive.Byte24B9.Write(1);id.ObjectId2C.Forget();f.Mouse.GameEngineA66288.Forget();f.Alive.Flags24B0.Forget();Eq((byte)0,f.Player(call),"Byte gate bypasses all later unknowns");Eq(0x3ce7c9u,call.Native.At,"Early Player return");Check(call.Returned&&!call.ObjectIdReturned&&!call.VideoReturned,"Reused diagnostic clears prior callee completion");Unknown(call.CapturedId,"Prior captured ID cleared");Unknown(call.CapturedEngine,"Prior captured engine cleared");
   f.Alive.Byte24B9.Forget();Missing(0x3ce7b6,delegate{f.Player(call);},"Later unknown byte stops first");Check(!call.Returned&&!call.ObjectIdReturned&&!call.VideoReturned,"Failed reuse does not retain success");Unknown(call.Result,"Failed reuse has no old result");
  });
  Test("Player unknown and lifetime failures preserve ID before global before flags order",delegate{
   var f=new Fixture();var call=new PlayerAliveInvocation();f.Alive.Byte24B9.Write(0);
   Missing(0x03a740,delegate{f.Player(call);},"Missing explicit ObjectId binding wins over unknown engine");Check(!call.ObjectIdReturned,"No fabricated ID");Unknown(call.CapturedEngine,"Global not captured before ID");
   var id=GameObjectIdState.BindEntity(f.Owner.Entity);Missing(0x03a740,delegate{f.Player(call);},"Bound unknown2C still fails actual getter");id.ObjectId2C.Write(0);
   Missing(0x2fa29a,delegate{f.Player(call);},"Unknown engine snapshot consumed by video CMP");Check(call.ObjectIdReturned&&!call.VideoReturned,"Real zero ID is accepted before engine failure");Eq(0u,call.CapturedId.Read(0),"Zero ID is not special-cased");
   f.Mouse.GameEngineA66288.Write(new object());Missing(0x2fa29a,delegate{f.Player(call);},"Unbound engine identity cannot auto-create null19B0");f.Mouse.GameEngineA66288.Write(null);Missing(0x2fa29a,delegate{f.Player(call);},"Null engine is not null video");
   f.Mouse.GameEngineA66288.Write(f.EngineIdentity);Missing(0x2fa29a,delegate{f.Player(call);},"Explicit bound engine still has unknown19B0");f.Video.Video19B0.Write(null);Missing(0x3ce7e2,delegate{f.Player(call);},"Only actual false video permits24B0 consumption");Check(call.ObjectIdReturned&&call.VideoReturned&&!call.Returned,"Completed prefix retained at flags stop");
   f.Alive.Flags24B0.Write(0);f.Owner.Entity.MarkReleased();Missing(0x3ce7b6,delegate{f.Player(call);},"Released original role stops first actual field read");Check(!call.ObjectIdReturned&&!call.VideoReturned,"Owner release cannot reach ID or engine");
  });
  Test("Nonnull video stops at real connection call and engine identity lifetimes stay authoritative",delegate{
   var f=new Fixture();f.SeedAlive();var call=new PlayerAliveInvocation();f.Video.Video19B0.Write(new object());f.Alive.Flags24B0.Forget();
   Missing(0x2fa2b0,delegate{f.Player(call);},"Unknown40F8 MOV consumed by vptr dereference");f.Connections.Write(null);Missing(0x2fa2b0,delegate{f.Player(call);},"Null40F8 cannot return an object");f.Connections.Write(new object());Missing(0x2fa2b0,delegate{f.Player(call);},"Opaque connection has no inferred vptr view");
   var connection=new GamePlayerVideoConnection();f.Connections.Write(connection);Missing(0x2fa2b3,delegate{f.Player(call);},"Unknown connection vptr consumed at slot18 call");connection.Virtual00.Write(new object());Missing(0x2fa2b3,delegate{f.Player(call);},"Known opaque vptr has no successful slot18 target");Check(call.ObjectIdReturned&&!call.VideoReturned&&!call.Returned,"Nonnull video cannot manufacture completion");Unknown(f.Alive.Flags24B0,"Unresolved slot18 precedes flags");
   connection.MarkReleased();Missing(0x2fa2b0,delegate{f.Player(call);},"Released connection stops at vptr load");
   var identity=new NavigationView();var lifetime=new NavigationView();var connections=new Field<object>();var engine=GamePlayerVideoState.Bind(identity,lifetime,connections);Same(engine,GamePlayerVideoState.Bind(identity,lifetime,connections),"Stable engine binding");Invalid(delegate{GamePlayerVideoState.Bind(identity,lifetime,new Field<object>());},"Same engine cannot acquire a40F8 mirror");Invalid(delegate{GamePlayerVideoState.Bind(identity,new NavigationView(),connections);},"Same engine cannot acquire another lifetime");engine.Video19B0.Write(null);f.Mouse.GameEngineA66288.Write(identity);identity.MarkReleased();Missing(0x2fa29a,delegate{f.Player(call);},"Released NavigationView identity fails even when separate lifetime is live");
   var opaque=new object();var borrowed=new NavigationView();var own=GamePlayerVideoState.Bind(opaque,borrowed,new Field<object>());own.Video19B0.Write(null);f.Mouse.GameEngineA66288.Write(opaque);borrowed.MarkReleased();Missing(0x2fa29a,delegate{f.Player(call);},"Released borrowed lifetime also fails");
  });
  Test("Real lookup alive empty-HotSlot selector chain reaches Options and later false gate clears original input",delegate{
   var f=new Fixture();f.SeedOuter();var current=new ActionRecord();var pending=new ActionRecord();f.Controller.CurrentA0.Write(current);f.Controller.PendingA8.Write(pending);var outer=new Invocation();
   Missing(0x609ad8,delegate{f.Outer(outer);},"Full supported prefix reaches real next Options boundary");Eq(0x162238u,outer.At,"Caller remains at actual Options call site");
   var lookup=f.Boundaries.LastLookup;Check(lookup.Returned&&lookup.ClassReturned&&lookup.Lookup.Returned&&lookup.Lookup.OwnLockEntered&&lookup.Lookup.OwnLockExited,"Actual128530 hash/class prefix completed");Eq(4,lookup.Lookup.HashedBytes,"Lookup performed four-byte hash");Eq(1,lookup.Lookup.VisitedNodes,"Lookup traversed own bucket");Eq(0,f.Lock.OwnOutstandingEnters,"Actual own lock released");Same(f.Move,lookup.Result.Read(0),"Lookup returns original Move");Same(f.Move,f.State.CachedRole10.Read(0),"Super's real second resolver populated cache");
   Check(f.Boundaries.LastAlive.Returned&&f.Boundaries.LastAlive.ObjectIdReturned&&f.Boundaries.LastAlive.VideoReturned,"Actual Player gate completed");Eq((byte)1,f.Boundaries.LastAlive.Result.Read(0),"Gate allowed selector");Eq(1,f.Boundaries.DisplayedCalls,"Actual empty tree getter executed once");Eq(0x3eaedau,f.Boundaries.LastDisplayedInvocation.At,"Actual empty-tree return");
   var hot=PlayerHotSlotState.ForBoundRole(f.Lookup,f.Move,0);Unknown(hot.Key98,"Empty tree never needed fallback key");Unknown(f.Role.Byte4A61,"Empty Super never cleared4A61");Unknown(f.Input.B4,"B4 only copied before actual stop");Unknown(f.Input.ObjectId,"Primary first1 never consumed object-ID pointer");Unknown(f.MouseController.Byte430,"Tail byte only copied");Unknown(f.Position.Q0,"Position token only copied");Unknown(f.Position.D14,"Position tail only copied");Same(current,f.Controller.CurrentA0.Read(0),"No current action replacement");Same(pending,f.Controller.PendingA8.Read(0),"No pending action replacement");
   var oldAlive=f.Boundaries.LastAlive;f.Alive.Byte24B9.Write(1);f.Role.Byte242A.Forget();var inputId=Field<uint>.Of(53);f.Input.ObjectId.Write(inputId);Eq((byte)0,f.Outer(outer),"Actual false gate executes outer cleanup");Check(!Object.ReferenceEquals(oldAlive,f.Boundaries.LastAlive),"Each real gate call has fresh diagnostic");Check(!f.Boundaries.LastAlive.ObjectIdReturned&&!f.Boundaries.LastAlive.VideoReturned,"Dead byte bypassed ID/video this time");Eq(0u,inputId.Read(0),"Original input ID cleared");Eq((byte)0,f.MouseController.Byte438.Read(0),"Original controller438 cleared");Eq(0u,f.MouseController.Dword43C.Read(0),"Original controller43C cleared");Eq(1,f.Boundaries.DisplayedCalls,"Dead gate did not re-enter HotSlot");Unknown(f.Role.Byte242A,"False CanAct bypassed unknown242A");Same(current,f.Controller.CurrentA0.Read(0),"Cleanup still retains current action");Same(pending,f.Controller.PendingA8.Read(0),"Cleanup still retains pending action");
  });
 }
}
public static class PlayerAliveValidationProgram {
 public static int Main(string[] args){try{if(args==null||args.Length!=0)throw new ArgumentException("Expected no arguments");PlayerAliveTests.Run();Console.WriteLine("{\"status\":\"passed-new-player-alive-null-video-mouse-prefix-only\",\"scenarios\":"+PlayerAliveTests.Scenarios+",\"assertions\":"+PlayerAliveTests.Assertions+",\"old_test_suites_rerun\":0,\"native_math_calls\":0,\"system_library_loaded\":false,\"full_tick\":false,\"physical_mouse\":false,\"original_read\":false,\"original_executed\":false,\"backend_started\":false}");return 0;}catch(Exception error){Console.Error.WriteLine(error);return 1;}}
}
