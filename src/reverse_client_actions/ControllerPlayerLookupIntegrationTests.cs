using System;
using GrimDynasty.ReverseClient.Actions;

// Explicit OWN publication only. Both singleton reads and both map lookups
// execute existing contracts; no native initialization or success override.
public static class ControllerPlayerLookupIntegrationTestProgram {
 static int scenarios,assertions;
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Case(Action body){body();scenarios++;}
 static void Stop(Action body,uint address){try{body();throw new Exception("Expected Unsupported");}catch(MissingContract error){Check(error.Address==address,"Unsupported "+error.Address.ToString("x")+" expected "+address.ToString("x"));}}
 static void Unknown<T>(Field<T> value,string why){bool unknown=false;try{value.Read(0);}catch(MissingContract){unknown=true;}Check(unknown,why);}
 internal sealed class Fixture {
  public readonly Controller Controller=new Controller();
  public readonly ActionRecord Current=new ActionRecord(),Pending=new ActionRecord();
  public readonly MoveCharacter Move=new MoveCharacter();
  public readonly EntityMovementRegistry Entities=new EntityMovementRegistry();
  public readonly ClassRecord CharacterClass=new ClassRecord(),PlayerClass=new ClassRecord(),ControllerClass=new ClassRecord();
  public readonly GameMovementCallbackContext Callbacks;
  public readonly GameMovementCharacterState Owner;
  public readonly GameMovementIdentityBridge Bridge;
  public readonly GameMovementIdentityView View;
  public readonly PlayerMouseContext Mouse=new PlayerMouseContext(new MoveBoundaries(new MoveContext()));
  public readonly MouseControllerState MouseController;
  public readonly MouseRoleState Role;
  public readonly PlayerMouseControllerObjectFields ControllerFields;
  public readonly GameReachedGoalEngineState Engine=new GameReachedGoalEngineState();
  public readonly GameObjectManagerState Objects=new GameObjectManagerState();
  public readonly GameObjectManagerOwnCriticalSection Lock=new GameObjectManagerOwnCriticalSection();
  public readonly GameObjectManagerNode Sentinel=new GameObjectManagerNode(),ControllerNode=new GameObjectManagerNode(),RoleNode=new GameObjectManagerNode();
  public readonly GameObjectManagerBuckets Buckets=new GameObjectManagerBuckets(1);
  public readonly PlayerMousePlayerLookupContext PlayerLookup;
  public readonly PlayerMousePlayerLookupBoundaries LookupBoundaries;
  public readonly PlayerAliveContext AliveContext;
  public readonly PlayerMouseAliveBoundaries AliveBoundaries;
  public readonly ControllerLookupContext ControllerLookup;
  public readonly ControllerLookupInvocation ControllerCall=new ControllerLookupInvocation();
  public readonly Invocation FirstControllerId=new Invocation(),FirstSingleton=new Invocation();
  public readonly Field<uint> CapturedControllerKey=new Field<uint>();
  public object CapturedControllerManager;
  public Controller CapturedController;
  public readonly WorldPosition24 Position=new WorldPosition24();
  public readonly WidgetMouseCallerState Widget;
  public readonly Field<uint> InputId=Field<uint>.Of(91);
  public readonly WidgetMouseCallerInvocation Caller=new WidgetMouseCallerInvocation();
  public Fixture(){
   var character=new AddressSymbol<ClassRecord>(0xa5cba0);character.Write(CharacterClass);var player=new AddressSymbol<ClassRecord>(0xa5cab0);player.Write(PlayerClass);
   Callbacks=new GameMovementCallbackContext(new WorldCoordinateBoundaries(new WorldCoordinateState()),Entities,character,player);
   Owner=new GameMovementCharacterState(new EntityMovementState(Move));Callbacks.Attach(Owner);Bridge=new GameMovementIdentityBridge(Callbacks);View=Bridge.Bind(Owner,Move);
   MouseController=new MouseControllerState(Controller);Mouse.Attach(MouseController);
   var controllerView=new MouseObjectView(Controller);controllerView.Virtual00.Write(ControllerLookupPrimaryTable.ObservedControllerPlayer);Mouse.Attach(controllerView);
   ControllerLookup=new ControllerLookupContext(Mouse,Field<ClassRecord>.Of(ControllerClass));ControllerLookup.ControllerPlayerClassInfoA5C180.Write(ControllerClass);
   PlayerLookup=new PlayerMousePlayerLookupContext(Mouse,Bridge,Engine);Role=new MouseRoleState(Move,Owner.Entity.Virtual00);PlayerLookup.BindRole(Role);
   LookupBoundaries=new PlayerMousePlayerLookupBoundaries(PlayerLookup);AliveContext=new PlayerAliveContext(PlayerLookup);AliveBoundaries=new PlayerMouseAliveBoundaries(AliveContext);
   ControllerFields=PlayerMouseControllerObjectFields.ForController(Controller);ControllerFields.ObjectId30.Write(0x1234);Owner.Entity.Virtual00.Write(GameMovementPrimaryTable.Player);Move.AssociatedId16C0.Write(7);
   Objects.OwnCritical18.Write(Lock);Objects.Sentinel38.Write(Sentinel);Objects.Buckets48.Write(Buckets);Objects.Mask60.Write(0);
   ControllerNode.Key10.Write(7);ControllerNode.Object18.Write(Controller);ControllerNode.Next00.Write(RoleNode);
   RoleNode.Key10.Write(0x1234);RoleNode.Object18.Write(Move);RoleNode.Next00.Write(Sentinel);Buckets.Cells[0].First00.Write(ControllerNode);Buckets.Cells[0].Last08.Write(RoleNode);
   Engine.Critical421398.Write(new GameReachedGoalCriticalState());Engine.Instance4213A0.Write(Objects);
   Controller.CurrentA0.Write(Current);Controller.PendingA8.Write(Pending);MouseController.Byte438.Write(7);MouseController.Dword43C.Write(99);
   Widget=new WidgetMouseCallerState(new object(),Position);Widget.Byte121.Write(0);Widget.Byte1D0.Write(1);Widget.Pointer868.Write(null);Widget.Byte85.Write(1);Widget.Byte848.Write(0);Widget.Byte849.Write(0);
  }
  public void CaptureController(){
   // Explicit OWN16C0 publication is consumed by the actual62310 body.
   // Its result remains distinct from Controller+30 and role Object+2C.
   CapturedControllerKey.Forget();FirstSingleton.At=0;CapturedControllerManager=null;CapturedController=null;
   CapturedControllerKey.Write(ControllerLookupContracts.CharacterGetControllerId62310(Bridge,Move,FirstControllerId));
   CapturedControllerManager=GameReachedGoalEngineContracts.GetObjectManager01C040(Engine,FirstSingleton);
   ControllerLookupContracts.Resolve1BCD0(CapturedControllerManager as GameObjectManagerState,CapturedControllerKey,ControllerLookup,ControllerCall);
   CapturedController=ControllerLookupContracts.BorrowCanonicalController(ControllerCall,Mouse);
  }
  public void CallCaptured(PlayerMouseBoundaries boundaries){
   WidgetMouseCallerContracts.Call225A2(Widget,Field<byte>.Of(0),Field<byte>.Of(1),Field<Controller>.Of(CapturedController),Field<WorldPosition24>.Of(Position),InputId,boundaries,Caller);
  }
  public void Run(PlayerMouseBoundaries boundaries){CaptureController();CallCaptured(boundaries);}
  public void Prefix(){
   Check(FirstControllerId.At==0x62316&&FirstSingleton.At==0x01c103&&ControllerCall.LookupReturned&&ControllerCall.FilterAccepted&&ControllerCall.ClassReturned,"Real62310 then first hot singleton/map/class prefix completed");
   Check(Object.ReferenceEquals(CapturedControllerManager,Objects)&&Object.ReferenceEquals(CapturedController,Controller),"First manager and Controller retain exact OWN identities");
   Check(Caller.ReachedCall&&Object.ReferenceEquals(Caller.CapturedController.Read(0),Controller),"Widget passed the same filtered Controller");
   Check(Object.ReferenceEquals(ControllerFields,PlayerMouseControllerObjectFields.ForController(CapturedController)),"Controller+30 has one authoritative sidecar");
  }
  public void LookupReturned(PlayerMousePlayerLookupBoundaries boundaries,int nodes){
   var call=boundaries.LastLookup;Check(call.SingletonReturned&&call.LookupReturned&&call.Lookup.Returned,"Second singleton and actual role hash lookup completed");
   Check(call.CapturedObjectId.Read(0)==0x1234&&call.Lookup.HashedBytes==4&&call.Lookup.VisitedNodes==nodes,"Role lookup uses distinct Controller+30 key and real bucket traversal");
   Check(call.Lookup.OwnLockExited&&Lock.OwnOutstandingEnters==0,"OWN map lock released");
  }
  public void ActionsUnchanged(){Check(Object.ReferenceEquals(Controller.CurrentA0.Read(0),Current)&&Object.ReferenceEquals(Controller.PendingA8.Read(0),Pending),"No current/pending action replacement");}
  public PlayerAliveState BindAlive(){return PlayerAliveState.Bind(Bridge,Move);}
 }
 static GameObjectManagerState EmptyManager(out GameObjectManagerOwnCriticalSection held){
  var manager=new GameObjectManagerState();held=new GameObjectManagerOwnCriticalSection();var sentinel=new GameObjectManagerNode();var buckets=new GameObjectManagerBuckets(1);
  manager.OwnCritical18.Write(held);manager.Sentinel38.Write(sentinel);manager.Buckets48.Write(buckets);manager.Mask60.Write(0);buckets.Cells[0].First00.Write(sentinel);return manager;
 }
 public static int Main(){try{
  Case(delegate{
   var f=new Fixture();f.Move.AssociatedId16C0.Forget();f.Engine.Critical421398.Forget();
   Stop(delegate{f.CaptureController();},0x62310);
   Check(f.FirstSingleton.At==0&&!f.ControllerCall.LookupReturned&&f.Lock.OwnOutstandingEnters==0,"Unknown16C0 stops before singleton or lookup");Unknown(f.CapturedControllerKey,"No controller key fabricated after unknown getter");
   f.Move.AssociatedId16C0.Write(7);Stop(delegate{f.CaptureController();},0x01c056);
   Check(f.FirstControllerId.At==0x62316&&f.CapturedControllerKey.Read(0)==7,"62310 returned before unknown singleton critical pointer is consumed");
   f.Engine.Critical421398.Write(new GameReachedGoalCriticalState());f.CaptureController();
   var alias=f.View.BorrowAssociatedId16C0(0);Check(Object.ReferenceEquals(alias,f.Move.AssociatedId16C0),"Getter borrows the original full-owner16C0 cell");
   alias.Write(8);f.ControllerNode.Key10.Write(8);var roleId=GameObjectIdState.BindEntity(f.Owner.Entity);roleId.ObjectId2C.Write(0x77);
   Stop(delegate{f.Run(f.LookupBoundaries);},0x15e230);f.Prefix();f.LookupReturned(f.LookupBoundaries,2);
   Check(f.CapturedControllerKey.Read(0)==8&&f.ControllerFields.ObjectId30.Read(0)==0x1234&&roleId.ObjectId2C.Read(0)==0x77,"Later call rereads changed16C0 while both other IDs stay distinct");
   f.Owner.Entity.MarkReleased();Stop(delegate{ControllerLookupContracts.CharacterGetControllerId62310(f.Bridge,f.Move,new Invocation());},0x62310);f.ActionsUnchanged();
  });
  Case(delegate{
   var f=new Fixture();Stop(delegate{f.Run(f.LookupBoundaries);},0x15e230);f.Prefix();f.LookupReturned(f.LookupBoundaries,2);
   var lookup=f.LookupBoundaries.LastLookup;Check(lookup.Returned&&lookup.ClassReturned&&Object.ReferenceEquals(lookup.Result.Read(0),f.Move),"Real128530 returns original canonical role before base CanAct boundary");
   Check(Object.ReferenceEquals(f.Role.Virtual00,f.Owner.Entity.Virtual00)&&Object.ReferenceEquals(f.View,f.Bridge.ForMoveCharacter(f.Move,0)),"Same Entity vptr cell and bidirectional bridge");
   Check(Object.ReferenceEquals(f.Caller.Frame.ObjectId.Read(0),f.InputId)&&f.InputId.Read(0)==91,"Input ID pointer is not Controller+30");Unknown(f.Role.Byte242A,"CanAct boundary precedes242A");f.ActionsUnchanged();
  });
  Case(delegate{
   var f=new Fixture();f.ControllerNode.Next00.Write(f.Sentinel);f.Buckets.Cells[0].Last08.Write(f.ControllerNode);f.Role.Virtual00.Forget();f.Run(f.LookupBoundaries);f.Prefix();f.LookupReturned(f.LookupBoundaries,1);
   Check(f.Caller.ActionReturned&&f.Caller.ActionResult.Read(0)==0&&f.LookupBoundaries.LastLookup.Result.Read(0)==null,"Actual role map miss yields action false without fake CanAct");
   Check(!f.LookupBoundaries.LastLookup.ClassReturned&&f.InputId.Read(0)==91&&f.MouseController.Byte438.Read(0)==7,"Miss bypasses class and false-CanAct cleanup");Unknown(f.Role.Virtual00,"Miss bypasses unknown vptr");f.ActionsUnchanged();
  });
  Case(delegate{
   var f=new Fixture();f.CaptureController();f.Engine.Instance4213A0.Forget();Stop(delegate{f.CallCaptured(f.LookupBoundaries);},0x01c0ae);
   Check(!f.LookupBoundaries.LastLookup.SingletonReturned&&!f.LookupBoundaries.LastLookup.LookupReturned,"Role stage rereads unknown4213A0, not captured controller manager");
   f.Engine.Instance4213A0.Write(null);Stop(delegate{f.CallCaptured(f.LookupBoundaries);},0x01c0b7);Check(!f.LookupBoundaries.LastLookup.SingletonReturned,"Null instance retains real cold Enter dependency");
   f.Engine.Instance4213A0.Write(new object());Stop(delegate{f.CallCaptured(f.LookupBoundaries);},0x019d5f);Check(f.LookupBoundaries.LastLookup.SingletonReturned&&!f.LookupBoundaries.LastLookup.LookupReturned,"Wrong published manager identity is rejected after getter");
   GameObjectManagerOwnCriticalSection held;var replacement=EmptyManager(out held);f.Engine.Instance4213A0.Write(replacement);f.CallCaptured(f.LookupBoundaries);f.Prefix();
   Check(f.Caller.ActionReturned&&f.Caller.ActionResult.Read(0)==0&&f.LookupBoundaries.LastLookup.Lookup.VisitedNodes==0,"Second read uses replacement manager's real empty bucket");
   Check(Object.ReferenceEquals(f.RoleNode.Object18.Read(0),f.Move)&&held.OwnOutstandingEnters==0,"Old map unchanged; replacement OWN lock released");f.ActionsUnchanged();
  });
  Case(delegate{
   var f=new Fixture();f.CaptureController();f.ControllerFields.ObjectId30.Forget();Stop(delegate{f.CallCaptured(f.LookupBoundaries);},0x019d81);f.Prefix();
   var lookup=f.LookupBoundaries.LastLookup;Check(lookup.SingletonReturned&&!lookup.LookupReturned&&lookup.Lookup.OwnCleanupReleased&&f.Lock.OwnOutstandingEnters==0,"Unknown Controller+30 consumed only by second real hash; OWN failure cleanup");Unknown(lookup.CapturedObjectId,"Unknown key captured without a default");
   f.ControllerFields.ObjectId30.Write(0x5555);f.CallCaptured(f.LookupBoundaries);
   Check(f.Caller.ActionReturned&&f.Caller.ActionResult.Read(0)==0&&f.LookupBoundaries.LastLookup.CapturedObjectId.Read(0)==0x5555,"Later call consumes changed shared key, not old1234 or controller key7");f.ActionsUnchanged();
  });
  Case(delegate{
   var f=new Fixture();f.RoleNode.Object18.Forget();Stop(delegate{f.Run(f.LookupBoundaries);},0x12854c);f.Prefix();f.LookupReturned(f.LookupBoundaries,2);
   Check(!f.LookupBoundaries.LastLookup.ClassReturned,"Unknown returned object is consumed after map lock release");
   f.RoleNode.Object18.Write(new object());Stop(delegate{f.Run(f.LookupBoundaries);},0x128551);
   f.RoleNode.Object18.Write(new MoveCharacter());Stop(delegate{f.Run(f.LookupBoundaries);},0x128551);
   Check(!f.LookupBoundaries.LastLookup.ClassReturned&&!f.Caller.ActionReturned,"Wrong type or unbound role cannot manufacture a canonical owner");Check(Object.ReferenceEquals(f.View,f.Bridge.ForMoveCharacter(f.Move,0)),"Original bridge survives rejected identities");f.ActionsUnchanged();
  });
  Case(delegate{
   var f=new Fixture();f.Role.Virtual00.Forget();Stop(delegate{f.Run(f.LookupBoundaries);},0x128557);f.Prefix();f.LookupReturned(f.LookupBoundaries,2);
   Check(Object.ReferenceEquals(f.Role.Virtual00,f.Owner.Entity.Virtual00),"Unknown vptr is the same canonical Entity cell");
   f.Role.Virtual00.Write(new object());Stop(delegate{f.Run(f.LookupBoundaries);},0x128557);Check(!f.LookupBoundaries.LastLookup.ClassReturned,"Unrecognized concrete class slot stays Unsupported");
   f.Role.Virtual00.Write(GameMovementPrimaryTable.Character);f.CharacterClass.Parent10.Write(null);f.Run(f.LookupBoundaries);
   Check(f.Caller.ActionReturned&&f.Caller.ActionResult.Read(0)==0&&f.LookupBoundaries.LastLookup.ClassReturned&&f.LookupBoundaries.LastLookup.Result.Read(0)==null,"Changed shared vptr runs actual Character class getter and rejects unrelated class");f.ActionsUnchanged();
  });
  Case(delegate{
   var f=new Fixture();Stop(delegate{f.Run(f.AliveBoundaries);},0x3ce7b6);f.Prefix();f.LookupReturned(f.AliveBoundaries,2);
   Check(f.AliveBoundaries.LastLookup.Returned&&f.AliveBoundaries.LastAlive!=null&&!f.AliveBoundaries.LastAlive.Returned,"Real Alive override reached missing own state binding, not base460 stub");
   var alive=f.BindAlive();Stop(delegate{f.Run(f.AliveBoundaries);},0x3ce7b6);Unknown(alive.Byte24B9,"Binding does not initialize alive byte");Unknown(alive.Flags24B0,"Binding does not initialize alive flags");f.ActionsUnchanged();
  });
  Case(delegate{
   var f=new Fixture();var alive=f.BindAlive();alive.Byte24B9.Write(1);f.Run(f.AliveBoundaries);f.Prefix();f.LookupReturned(f.AliveBoundaries,2);
   var gate=f.AliveBoundaries.LastAlive;Check(gate.Returned&&gate.Result.Read(0)==0&&!gate.ObjectIdReturned&&!gate.VideoReturned,"Explicit OWN nonzero byte takes real early false branch");
   Check(f.Caller.ActionReturned&&f.Caller.ActionResult.Read(0)==0&&f.InputId.Read(0)==0&&f.MouseController.Byte438.Read(0)==0&&f.MouseController.Dword43C.Read(0)==0,"Actual false-CanAct clears original borrowed ID and Controller fields");
   Check(f.Widget.Byte1D0.Read(0)==1,"Unreached super-interception optional byte is not cleared");Unknown(alive.Flags24B0,"Early false does not consume flags");Unknown(f.Mouse.GameEngineA66288,"Early false does not consume GameEngine global");f.ActionsUnchanged();
  });
  Case(delegate{
   var f=new Fixture();var alive=f.BindAlive();alive.Byte24B9.Write(0);Stop(delegate{f.Run(f.AliveBoundaries);},0x03a740);
   var id=GameObjectIdState.BindEntity(f.Owner.Entity);id.ObjectId2C.Write(0x77);Stop(delegate{f.Run(f.AliveBoundaries);},0x2fa29a);
   Check(f.AliveBoundaries.LastAlive.ObjectIdReturned&&!f.AliveBoundaries.LastAlive.VideoReturned&&f.AliveBoundaries.LastAlive.CapturedId.Read(0)==0x77,"Actual role2C getter precedes unknown engine, distinct from both lookup keys");
   var identity=new NavigationView();var connections=new Field<object>();var video=GamePlayerVideoState.Bind(identity,identity,connections);f.Mouse.GameEngineA66288.Write(identity);
   Stop(delegate{f.Run(f.AliveBoundaries);},0x2fa29a);Unknown(video.Video19B0,"Binding does not invent null video");
   video.Video19B0.Write(null);Stop(delegate{f.Run(f.AliveBoundaries);},0x3ce7e2);
   Check(f.AliveBoundaries.LastAlive.VideoReturned&&!f.AliveBoundaries.LastAlive.Returned,"Real null-video return reaches still unknown alive flags");Unknown(connections,"Null video bypasses unknown connection");Check(f.InputId.Read(0)==91,"Failed gate leaves input ID untouched");f.ActionsUnchanged();
  });
  Case(delegate{
   var f=new Fixture();var alive=f.BindAlive();alive.Byte24B9.Write(0);alive.Flags24B0.Write(0);GameObjectIdState.BindEntity(f.Owner.Entity).ObjectId2C.Write(0x77);
   var identity=new NavigationView();var connections=new Field<object>();var video=GamePlayerVideoState.Bind(identity,identity,connections);video.Video19B0.Write(null);f.Mouse.GameEngineA66288.Write(identity);
   Stop(delegate{f.Run(f.AliveBoundaries);},0x15e23e);f.Prefix();f.LookupReturned(f.AliveBoundaries,2);
   Check(f.AliveBoundaries.LastAlive.Returned&&f.AliveBoundaries.LastAlive.Result.Read(0)==1&&f.AliveBoundaries.LastAlive.ObjectIdReturned&&f.AliveBoundaries.LastAlive.VideoReturned,"Explicit OWN conditions execute real PlayerAlive true path");
   Check(Object.ReferenceEquals(f.AliveContext.GameEngineA66288,f.Mouse.GameEngineA66288)&&Object.ReferenceEquals(f.AliveBoundaries.LastLookup.Result.Read(0),f.Move),"Alive consumes same global cell and original role");
   Unknown(f.Role.Byte242A,"Next unresolved state is242A initialization, not missing CanAct body");Unknown(f.Position.Q0,"242A stop precedes position copy");
   Check(!f.Caller.ActionReturned&&f.InputId.Read(0)==91&&f.MouseController.Byte438.Read(0)==7,"No action result or false cleanup invented after true gate");f.ActionsUnchanged();
  });
  Console.WriteLine("{\"status\":\"passed-controller-player-lookup-own-integration\",\"scenarios\":"+scenarios+",\"assertions\":"+assertions+",\"actual_controller_id_getter\":true,\"actual_controller_lookup\":true,\"actual_player_lookup\":true,\"canonical_role_shared\":true,\"actual_alive_prefix\":true,\"movement_executed\":false,\"native_initialization_verified\":false,\"physical_mouse_verified\":false}");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
