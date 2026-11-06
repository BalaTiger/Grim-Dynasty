using System;
using GrimDynasty.ReverseClient.Actions;
using MoveFixture=GameControllerMoveProductionTests.Fixture;
using MouseFixture=ControllerPlayerLookupIntegrationTestProgram.Fixture;

// Explicit OWN hot publications and conditionally reached constructor stores.
// The two existing fixtures are reused; neither previous suite/Main is run.
// No virtual callback manufactures success. The actual nonzero Controller
// association executes the observed +60 DWORD store. World50 and state+70
// remain explicit stops; no continuous movement or active initialization claim.
public static class ControllerMoveAssociationIntegrationTestProgram {
 static int scenarios,assertions;
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Case(string name,Action body){try{body();scenarios++;}catch(Exception error){throw new Exception(name+": "+error.Message,error);}}
 static void Stop(uint address,Action body){try{body();}catch(MissingContract error){Check(error.Address==address,"Unsupported "+error.Address.ToString("x")+" expected "+address.ToString("x"));return;}throw new Exception("Expected Unsupported "+address.ToString("x"));}
 static void Throws<T>(Action body)where T:Exception{try{body();}catch(T){Check(true,"Expected "+typeof(T).Name);return;}throw new Exception("Expected "+typeof(T).Name);}
 static void Unknown<T>(Field<T> field,string why){bool unknown=false;try{field.Read(0);}catch(MissingContract){unknown=true;}Check(unknown,why);}
 sealed class Association {
  public readonly MoveFixture F=new MoveFixture();
  public readonly MouseControllerState MouseController;
  public readonly MouseObjectView Primary;
  public readonly ClassRecord ControllerClass=new ClassRecord(),BaseClass=new ClassRecord(),BF10=new ClassRecord(),C990=new ClassRecord(),C9C0=new ClassRecord();
  public readonly ControllerLookupContext Lookup;
  public readonly ControllerPlayerAssociatedLookupBoundaries Boundaries;
  public readonly GameObjectManagerNode ControllerNode=new GameObjectManagerNode();
  public Association(){
   MouseController=new MouseControllerState(F.Controller);F.Lookup.Mouse.Attach(MouseController);
   Primary=new MouseObjectView(F.Controller);Primary.Virtual00.Write(ControllerLookupPrimaryTable.ObservedControllerPlayer);F.Lookup.Mouse.Attach(Primary);
   Lookup=new ControllerLookupContext(F.Lookup.Mouse,Field<ClassRecord>.Of(ControllerClass));Lookup.ControllerPlayerClassInfoA5C180.Write(ControllerClass);
   Boundaries=new ControllerPlayerAssociatedLookupBoundaries(Lookup,F.Context.ControllerBaseCharacterClass);
   // One shared map: role key1234 and Controller key7 must not be substituted.
   ControllerNode.Key10.Write(7);ControllerNode.Object18.Write(F.Controller);ControllerNode.Next00.Write(F.Sentinel);
   F.Node.Next00.Write(ControllerNode);F.Buckets.Cells[0].Last08.Write(ControllerNode);
   F.Move.Gate26AF.Write(0);F.Move.AssociatedId16C0.Write(7);F.PutSkill(0);
  }
  public void Bind(){F.Context.BindControllerAssociation(Boundaries);}
  public void DiskParents(){F.Context.ControllerBaseCharacterClass.Write(BaseClass);Boundaries.InitializeDiskParentChain(BF10,C990,C9C0);}
  public void Run(){
   // Each call is an independent OWN submission with an explicitly empty
   // handler. This is not a native reset/cleanup claim for the previous stop.
   F.EmptyLocal();F.Run();
  }
  public GameAssociatedLookupInvocation Associated(){Check(F.Call.Handle.AssociatedCalls.Count==1,"One actual077D60 call");return F.Call.Handle.AssociatedCalls[0];}
  public void Prefix(uint stop){
   var call=F.Call;Check(call.FirstAIReturned&&call.SkillReturned&&call.SecondAIReturned&&call.CoordinatesReturned&&call.ConstructorReturned,"Actual lookups, skill, coordinates and constructor returned");
   Same(F.Move,call.FirstAI.Result.Read(0),"First GetAI original role");Same(F.Move,call.SecondAI.Result.Read(0),"Second GetAI original role");
   Check(call.Handle.LocalEntered&&call.Handle.BeginEntered&&!call.Handle.BeginReturned&&!call.Handle.Returned&&!call.Returned,"Actual local Begin is incomplete");
   Check(call.Native.At==stop&&call.Handle.Native.At==stop&&call.Handle.BeginNative.At==stop,"Outer and Begin diagnostics preserve the exact stop");
   Same(call.AllocatedAction,call.ConstructorResult,"Original action allocation returned");Same(call.AllocatedAction,F.Handler.Current08.Read(0),"Executor published this same action before Begin");
   Check(F.Handler.Busy18.Read(0)==1,"Partial executor Busy18 publication retained");
   Same(null,F.Controller.CurrentA0.Read(0),"Controller current remains its original null");Same(null,F.Controller.PendingA8.Read(0),"Controller pending remains its original null");
   Check(F.Lock.OwnOutstandingEnters==0,"OWN map locks released");Unknown(F.Move.MovementDC0,"No Agent/navigation manager fabricated");
   Check(F.Call.Handle.DistanceCalls.Count==0&&F.Call.Handle.WaypointCalls.Count==0,"No distance or waypoint execution claimed");
  }
 }
 static void ConfigureActualAlive(MouseFixture f){
  var alive=f.BindAlive();alive.Byte24B9.Write(0);alive.Flags24B0.Write(0);
  GameObjectIdState.BindEntity(f.Owner.Entity).ObjectId2C.Write(0x77);
  var identity=new NavigationView();var video=GamePlayerVideoState.Bind(identity,identity,new Field<object>());video.Video19B0.Write(null);f.Mouse.GameEngineA66288.Write(identity);
  var state=CharacterMouseConstructor242AState.Bind(f.PlayerLookup,f.Role);CharacterMouseConstructorContracts.ApplyReachedWordZero052674(state,new Invocation());
 }
 // Only the two changed NotifyAssociated callers are exercised here. Region,
 // CanMove, AlreadyThere and failed MoveManager answers select branches as in
 // the existing MoveContractsTests observations; they are NOT production
 // navigation, class lookup, physical movement or a successful +80 adapter.
 sealed class NotifyCallerInputs:MoveBoundaries {
  public readonly MoveCharacter Role=new MoveCharacter();
  public readonly MovementManager Manager=new MovementManager();
  public readonly Controller AssociatedObject=new Controller();
  public readonly object ManagerIdentity=new object(),RegionIdentity=new object();
  public readonly WorldPosition Destination=new WorldPosition();
  public readonly Field<object> UnknownPrimary=new Field<object>();
  public readonly bool FirstAlready;
  public readonly Invocation Call=new Invocation();
  public int AlreadyCalls,MoveRequests,RegionCalls,IsMovingCalls,ManagerCalls,LookupCalls,CaptureCalls,ConsumeCalls;
  public uint LookupId,CaptureAt,ConsumeAt;
  public byte MovingAtCapture;
  public object LookupManager,CapturedObject,ConsumedObject;
  public Field<object> ConsumedPrimary;
  public NotifyCallerInputs(bool firstAlready):base(new MoveContext()){
   FirstAlready=firstAlready;Context.AlreadyThereConstant7A7FD8.Write(0.025f);
   Role.MovementDC0.Write(Manager);Role.AssociatedId16C0.Write(7);Manager.Registered6C.Write(0);
   // First branch must really clear9. The second starts explicitly OWN0;
   // its Notify branch itself has no Moving38 write and must preserve0.
   Manager.Moving38.Write(firstAlready?(byte)9:(byte)0);
  }
  public void Run(){MoveContracts.Move(Role,Destination,0,7,2.5f,this,Call);}
  public override object Region(WorldPosition position){RegionCalls++;return RegionIdentity;}
  public override byte CanMove460(MoveCharacter role){Same(Role,role,"Caller uses original role for branch input");return 1;}
  public override byte AlreadyThere(MovementManager manager,WorldPosition position,float tolerance){
   Same(Manager,manager,"AlreadyThere receives original manager");ControllerMoveAssociationIntegrationTestProgram.Check(tolerance==0.025f,"Original tolerance cell consumed");AlreadyCalls++;
   Same(AlreadyCalls==1?Destination:Manager.Position20,position,"Correct destination/current-position branch input");return FirstAlready||AlreadyCalls==2?(byte)1:(byte)0;
  }
  public override byte MoveManager(MovementManager manager,WorldPosition position){Same(Manager,manager,"Failed request uses original manager");Same(Destination,position,"Failed request uses original destination");MoveRequests++;return 0;}
  public override byte IsMoving(MoveCharacter role){Same(Role,role,"Fallback IsMoving borrows original role");IsMovingCalls++;return Manager.Moving38.Read(0x05f971);}
  public override object ObjectManager(){ManagerCalls++;return ManagerIdentity;}
  public override object Associated(object manager,uint id){LookupCalls++;LookupManager=manager;LookupId=id;return AssociatedObject;}
  public override Field<object> CaptureAssociatedPrimary(object associated,uint at){CaptureCalls++;CapturedObject=associated;CaptureAt=at;MovingAtCapture=Manager.Moving38.Read(at);return UnknownPrimary;}
  public override void Associated80Captured(object associated,Field<object> primary,Invocation i){
   ConsumeCalls++;ConsumedObject=associated;ConsumedPrimary=primary;ConsumeAt=i.At;primary.Read(i.At);
   throw new Exception("Unknown caller vptr unexpectedly became readable");
  }
  public void AssertStopped(uint captureAt,uint callAt){
   ControllerMoveAssociationIntegrationTestProgram.Check(Call.At==callAt&&CaptureAt==captureAt&&ConsumeAt==callAt,"Exact caller capture and indirect-call instruction addresses");
   ControllerMoveAssociationIntegrationTestProgram.Check(CaptureCalls==1&&ConsumeCalls==1&&LookupCalls==1&&ManagerCalls==1,"One singleton, associated result, capture and consumption");
   Same(ManagerIdentity,LookupManager,"Original manager passed into association branch input");ControllerMoveAssociationIntegrationTestProgram.Check(LookupId==7,"Original associated ID passed unchanged");
   Same(AssociatedObject,CapturedObject,"Capture keeps returned associated identity");Same(AssociatedObject,ConsumedObject,"Consumption keeps same associated identity");
   ControllerMoveAssociationIntegrationTestProgram.Check(!Object.ReferenceEquals(UnknownPrimary,ConsumedPrimary),"Actual Notify makes the captured field snapshot");Unknown(UnknownPrimary,"Source vptr remains unknown");Unknown(ConsumedPrimary,"Captured vptr remains unknown until indirect call");
   ControllerMoveAssociationIntegrationTestProgram.Check(MovingAtCapture==0&&Manager.Moving38.Read(0)==0,"Zero moving state is visible before capture and retained after stop");
  }
 }
 public static int Main(){try{
  Case("Binding requires the original mouse and exact shared symbol",delegate{
   var a=new Association();Throws<ArgumentNullException>(delegate{a.F.Context.BindControllerAssociation(null);});
   var foreignMouse=new PlayerMouseContext(new MoveBoundaries(new MoveContext()));var foreign=new ControllerLookupContext(foreignMouse,Field<ClassRecord>.Of(a.ControllerClass));
   Throws<ArgumentException>(delegate{a.F.Context.BindControllerAssociation(new ControllerPlayerAssociatedLookupBoundaries(foreign,a.F.Context.ControllerBaseCharacterClass));});
   var sameAddressDifferentCell=new AddressSymbol<ClassRecord>(0xa5c9f0);sameAddressDifferentCell.Write(a.BaseClass);
   Throws<ArgumentException>(delegate{a.F.Context.BindControllerAssociation(new ControllerPlayerAssociatedLookupBoundaries(a.Lookup,sameAddressDifferentCell));});
   Same(null,a.F.Context.ControllerAssociation,"Rejected binds publish no association");
   a.Bind();a.Bind();Same(a.Boundaries,a.F.Context.ControllerAssociation,"Exact same binding is idempotent");Same(a.F.Lookup.Mouse,a.Boundaries.BoundMouseContext,"Mouse object is borrowed");Same(a.F.Context.ControllerBaseCharacterClass,a.Boundaries.ControllerBaseCharacterClass,"Expected symbol cell is borrowed");
   Throws<InvalidOperationException>(delegate{a.F.Context.BindControllerAssociation(new ControllerPlayerAssociatedLookupBoundaries(a.Lookup,a.F.Context.ControllerBaseCharacterClass));});
   Same(a.Boundaries,a.F.Context.ControllerAssociation,"Rejected rebind preserves first binding");Unknown(a.ControllerClass.Parent10,"Binding does not initialize a class graph");Unknown(a.MouseController.Count2F0,"Binding does not initialize active mouse state");
  });
  Case("Real MoveTo and Begin return original Controller and execute slot60 store",delegate{
   var a=new Association();a.Bind();a.DiskParents();var target=ControllerAssociatedTargetState.ForController(a.F.Lookup.Mouse,a.F.Controller,0);Unknown(target.Target38,"Binding supplies no target initial value");a.F.Flags.Write(0xdeadbeef);
   Stop(0x082cf4,a.Run);a.Prefix(0x082cf4);var c=a.Associated();
   Check(c.LookupReturned&&c.Lookup.Returned&&c.Lookup.HashedBytes==4&&c.Lookup.VisitedNodes==2,"Actual locked hash lookup traversed distinct role and Controller nodes");
   Same(a.F.Controller,c.CapturedObject.Read(0),"Map captured original Controller");Same(a.F.Controller,c.Result.Read(0),"Accepted result keeps original Controller identity");Same(a.ControllerClass,c.ReturnedClass.Read(0),"Concrete51AC0 returned A5C180 binding");
   Check(c.PrimaryCaptured&&c.ClassReturned&&c.DerivedEntered&&c.DerivedReturned&&c.Returned&&c.Derived.At==0x5914a2,"Real class getter and591480 accepted saved disk parents");
   Check(a.F.Move.AssociatedId16C0.Read(0)==7&&a.F.ControllerFields.ObjectId30.Read(0)==0x1234,"Associated and role IDs remain separate");
   Check(a.F.Call.Handle.AliveCalls.Count==1&&a.F.Call.Handle.AliveCalls[0].Returned&&a.F.Call.Handle.CoordinateReturns.Count==2,"Actual Alive and both GetCoords returned around60");
   Same(target,ControllerAssociatedTargetState.ForController(a.F.Lookup.Mouse,a.F.Controller,0),"One target sidecar for canonical controller view");Check(target.Target38.Read(0)==0xdeadbeef,"Real127B70 DWORD store updated the original target field");Unknown(a.F.WorldEngine.World50,"Next real world dependency remains unknown");Unknown(a.BaseClass.Parent10,"Terminal base parent remains unknown");Unknown(a.MouseController.State2A0,"No active state created by MoveTo integration");
  });
  Case("Missing binding or missing canonical Controller view fails before class dispatch",delegate{
   var a=new Association();Stop(0x077d73,a.Run);a.Prefix(0x077d73);var c=a.Associated();Check(c.LookupReturned&&!c.PrimaryCaptured&&!c.ClassReturned&&!c.Returned,"Map return is not class acceptance");
   var b=new Association();b.Bind();b.DiskParents();var other=new Controller();b.ControllerNode.Object18.Write(other);Stop(0x077d73,b.Run);b.Prefix(0x077d73);Same(other,b.Associated().CapturedObject.Read(0),"Actual foreign identity captured, never replaced with caller Controller");
  });
  Case("Unknown gate key vptr and expected class keep distinct failure prefixes",delegate{
   var gate=new Association();gate.Bind();gate.F.Move.Gate26AF.Forget();Stop(0x082c98,gate.Run);gate.Prefix(0x082c98);Check(gate.F.Call.Handle.AssociatedCalls.Count==0,"Existing unknown gate stops before association");
   var key=new Association();key.Bind();key.F.Move.AssociatedId16C0.Forget();Stop(0x082ca5,key.Run);key.Prefix(0x082ca5);Check(key.F.Call.Handle.AssociatedCalls.Count==0,"Unknown16C0 is not default key0");
   var vptr=new Association();vptr.Bind();vptr.DiskParents();vptr.Primary.Virtual00.Forget();Stop(0x077d79,vptr.Run);vptr.Prefix(0x077d79);Check(vptr.Associated().PrimaryCaptured&&!vptr.Associated().ClassReturned,"Unknown vptr consumed only at actual slot0 call");
   var symbol=new Association();symbol.Bind();Stop(0x077d82,symbol.Run);symbol.Prefix(0x077d82);Check(symbol.Associated().ClassReturned&&!symbol.Associated().Returned,"Real class returned before unknown A5C9F0 compare");
   symbol.F.Context.ControllerBaseCharacterClass.Write(symbol.BaseClass);Stop(0x077d8b,symbol.Run);symbol.Prefix(0x077d8b);Check(!symbol.Associated().DerivedEntered,"Unknown direct parent stops before helper");
  });
  Case("Missing map entry and known null bypass binding; unknown Object18 does not",delegate{
   var miss=new Association();miss.F.Move.AssociatedId16C0.Write(8);Stop(0x082cf4,miss.Run);miss.Prefix(0x082cf4);var c=miss.Associated();Check(c.Returned&&c.Native.At==0x077dae&&!c.PrimaryCaptured,"Real missing key returns null without adapter");Same(null,c.Result.Read(0),"Map miss is known null");Check(miss.F.Call.Handle.CoordinateReturns.Count==2,"Miss advances to second actual GetCoords");Unknown(miss.F.WorldEngine.World50,"World pointer remains the next real unknown");
   var empty=new Association();empty.ControllerNode.Object18.Write(null);Stop(0x082cf4,empty.Run);empty.Prefix(0x082cf4);Check(empty.Associated().Returned&&!empty.Associated().ClassReturned,"Known null bypasses all class work");
   var unknown=new Association();unknown.ControllerNode.Object18.Forget();Stop(0x077d6e,unknown.Run);unknown.Prefix(0x077d6e);Check(!unknown.Associated().PrimaryCaptured&&!unknown.Associated().Returned,"Opaque object is not null");Unknown(unknown.Associated().CapturedObject,"Unknown pointer bits preserved");
  });
  Case("Next OWN submission rereads mutable map and saved parent chain",delegate{
   var a=new Association();a.Bind();a.DiskParents();var target=ControllerAssociatedTargetState.ForController(a.F.Lookup.Mouse,a.F.Controller,0);a.F.Flags.Write(11);Stop(0x082cf4,a.Run);Same(a.F.Controller,a.Associated().Result.Read(0),"First request accepted original Controller");Check(target.Target38.Read(0)==11,"First target publication");
   a.ControllerNode.Key10.Write(8);Stop(0x082cf4,a.Run);Same(null,a.Associated().Result.Read(0),"Changed map key makes a real miss on next independent request");
   a.F.Move.AssociatedId16C0.Write(8);a.F.Flags.Write(22);Stop(0x082cf4,a.Run);Same(a.F.Controller,a.Associated().Result.Read(0),"Changed full-role ID observes same updated node");Check(target.Target38.Read(0)==22,"New accepted request writes same live target field");
   a.C9C0.Parent10.Write(null);a.F.Flags.Write(33);Stop(0x082cf4,a.Run);var c=a.Associated();Check(c.DerivedEntered&&c.DerivedReturned&&c.Returned,"Actual helper reevaluated changed parent");Same(null,c.Result.Read(0),"Changed class graph rejects instead of replaying prior acceptance");Same(null,a.C9C0.Parent10.Read(0),"Lookup does not repair the graph from disk");Check(target.Target38.Read(0)==22,"Rejected association does not execute slot60 or replace earlier value");a.Prefix(0x082cf4);
  });
  Case("Old MoveCharacter association remains independent of new Controller binding",delegate{
   var a=new Association();a.F.Move.AssociatedId16C0.Write(0x1234);
   // Deliberate OWN class expectation to exercise the prior MoveCharacter
   // adapter; this is not the native A5C9F0-to-Player class relationship.
   a.F.Context.ControllerBaseCharacterClass.Write(a.F.PlayerClass);
   Stop(0x082cd1,a.Run);a.Prefix(0x082cd1);var c=a.Associated();Same(a.F.Move,c.Result.Read(0),"Legacy role identity preserved");Same(a.F.PlayerClass,c.ReturnedClass.Read(0),"Actual Player016D10 branch still used");Check(c.Returned&&!c.DerivedEntered,"Exact OWN class match skips helper");Same(null,a.F.Context.ControllerAssociation,"Legacy path needs no Controller binding");
  });
  Case("Opaque constructor flags are copied by real slot60 without premature consumption",delegate{
   var a=new Association();a.Bind();a.DiskParents();var target=ControllerAssociatedTargetState.ForController(a.F.Lookup.Mouse,a.F.Controller,0);target.Target38.Write(99);a.F.Flags.Forget();Stop(0x082cf4,a.Run);a.Prefix(0x082cf4);Check(a.Associated().Returned,"Association completed before opaque flag copy");Same(a.F.Controller,a.Associated().Result.Read(0),"Accepted Controller identity preserved");Unknown(a.F.Call.AllocatedAction.Flags78,"Constructor copied opaque flags without defaulting");Unknown(target.Target38,"127B70 overwrites earlier known value with original unknown bits");Check(a.F.Call.Handle.CoordinateReturns.Count==2,"No false082CCE stop: second coordinates really returned after store");
  });
  Case("Direct slot60 retains captured table and copied flag snapshot",delegate{
   var a=new Association();var target=ControllerAssociatedTargetState.ForController(a.F.Lookup.Mouse,a.F.Controller,0);var table=a.Primary.Virtual00.Snapshot();var flags=Field<uint>.Of(0x10203040);var copied=flags.Snapshot();var i=new Invocation();
   a.Primary.Virtual00.Forget();flags.Write(0xaabbccdd);a.Boundaries.Slot60(a.F.Controller,table.Read(0x082cd1),copied,i);
   Check(i.At==0x127b73&&target.Target38.Read(0)==0x10203040,"Captured table and opaque register snapshot reach real store/RET");Unknown(a.Primary.Virtual00,"Store does not repair or reread live vptr");
   copied.Write(1);Check(target.Target38.Read(0)==0x10203040,"Store copies bits without aliasing its register source");
   Stop(0x082cd1,delegate{i.At=0x082cd1;a.Boundaries.Slot60(a.F.Controller,new object(),Field<uint>.Of(5),i);});Check(target.Target38.Read(0)==0x10203040,"Unbound table cannot reach target store");
   Stop(0x127b70,delegate{ControllerAssociatedTargetState.ForController(a.F.Lookup.Mouse,new Controller(),0x127b70);});
  });
  Case("Slot80 unknown count does not initialize either state source",delegate{
   var a=new Association();var i=new Invocation();Stop(0x123810,delegate{a.Boundaries.Slot80(a.F.Controller,ControllerLookupPrimaryTable.ObservedControllerPlayer,i);});
   Check(i.At==0x123810,"Actual count compare is the first unknown");Unknown(a.Boundaries.Last80State,"No state selected before count compare");Unknown(a.Boundaries.Last80Primary,"No table captured before state selection");Unknown(a.MouseController.Count2F0,"No default zero count");Unknown(a.MouseController.State2A0,"Direct source untouched");Unknown(a.MouseController.Overlay2E8,"Overlay source untouched");
  });
  Case("Slot80 direct state uses original identity and stops at unclosed70",delegate{
   var a=new Association();var i=new Invocation();a.MouseController.Count2F0.Write(0);
   Stop(0x123836,delegate{a.Boundaries.Slot80(a.F.Controller,ControllerLookupPrimaryTable.ObservedControllerPlayer,i);});Unknown(a.Boundaries.Last80State,"Unknown direct-state bits retained");
   var state=new PlayerMouseState(new object());a.MouseController.State2A0.Write(state);
   Stop(0x123839,delegate{a.Boundaries.Slot80(a.F.Controller,ControllerLookupPrimaryTable.ObservedControllerPlayer,i);});Same(state,a.Boundaries.Last80State.Read(0),"Same direct state identity");Unknown(a.Boundaries.Last80Primary,"Unknown table retained until actual JMP");
   var table=new PlayerMouseTable();state.Virtual00.Write(table);Stop(0x123839,delegate{a.Boundaries.Slot80(a.F.Controller,ControllerLookupPrimaryTable.ObservedControllerPlayer,i);});Same(table,a.Boundaries.Last80Primary.Read(0),"Known captured table still does not supply unknown slot70 body");Unknown(a.MouseController.Overlay2E8,"Direct branch does not consume or initialize overlay");
  });
  Case("Slot80 PreStart empty70 completes only with the evidenced table binding",delegate{
   var a=new Association();var i=new Invocation();a.MouseController.Count2F0.Write(0);
   var table=new PlayerMouseTable();table.BindObserved(ObservedPlayerMouseTable.Startup);
   var state=new PlayerMouseState(new object());a.MouseController.State2A0.Write(state);state.Virtual00.Write(table);
   ControllerAIStateTableBinding.Bind(state,ControllerAIStateTable.Prestart);
   a.Boundaries.Slot80(a.F.Controller,ControllerLookupPrimaryTable.ObservedControllerPlayer,i);
   Check(i.At==0x015880,"Actual +70 target is the shared 015880 ret 0 stub");Same(state,a.Boundaries.Last80State.Read(0),"Direct state identity retained");Same(table,a.Boundaries.Last80Primary.Read(0),"Captured table retained");
   Same(ControllerAIStateTable.Prestart,ControllerAIStateTableBinding.Read(state,0),"Evidenced table publication is not consumed or rewritten");Unknown(a.MouseController.Overlay2E8,"Empty +70 does not initialize the overlay list");Check(a.F.Context.ControllerAssociation==null,"Calling slot80 does not publish an association");
  });
  Case("Slot80 forty-plus table values or a foreign PreStart binding never choose70",delegate{
   var a=new Association();var i=new Invocation();a.MouseController.Count2F0.Write(0);
   var startup=new PlayerMouseTable();startup.BindObserved(ObservedPlayerMouseTable.Startup);
   Check(startup.Move280.Read(0)==0x015880,"Startup Move280 also names 015880, but that is a different slot");
   var state=new PlayerMouseState(new object());a.MouseController.State2A0.Write(state);state.Virtual00.Write(startup);
   Stop(0x123839,delegate{a.Boundaries.Slot80(a.F.Controller,ControllerLookupPrimaryTable.ObservedControllerPlayer,i);});
   var other=new PlayerMouseState(new object());ControllerAIStateTableBinding.Bind(other,ControllerAIStateTable.Prestart);
   Stop(0x123839,delegate{a.Boundaries.Slot80(a.F.Controller,ControllerLookupPrimaryTable.ObservedControllerPlayer,i);});
   Same(startup,a.Boundaries.Last80Primary.Read(0),"Selected table is preserved on either rejection");Unknown(a.MouseController.Overlay2E8,"Rejected selection does not touch the overlay list");
  });
  Case("Slot80 MoveTo binding without a transition composition stops at the decoded target",delegate{
   var a=new Association();var i=new Invocation();a.MouseController.Count2F0.Write(0);
   var state=new PlayerMouseState(new object());a.MouseController.State2A0.Write(state);state.Virtual00.Write(new PlayerMouseTable());
   ControllerAIStateTableBinding.Bind(state,ControllerAIStateTable.MoveTo);
   Stop(0x1663f0,delegate{a.Boundaries.Slot80(a.F.Controller,ControllerLookupPrimaryTable.ObservedControllerPlayer,i);});
   Unknown(a.Boundaries.LastEndOfPathName,"No name is built when the composition is unbound");
  });
  Case("Slot80 MoveTo binding with a transition view reaches the registry read",delegate{
   var a=new Association();var i=new Invocation();a.MouseController.Count2F0.Write(0);
   var state=new PlayerMouseState(new object());a.MouseController.State2A0.Write(state);state.Virtual00.Write(new PlayerMouseTable());
   ControllerAIStateTableBinding.Bind(state,ControllerAIStateTable.MoveTo);
   a.Boundaries.BindStateTransition(new ControllerStateTransitionContext(a.F.Lookup,Field<float>.Of(1f),Field<float>.Of(0f)));
   ControllerStateTransitionState.Bind(a.F.Lookup.Mouse,a.MouseController);
   Stop(0x07896f,delegate{a.Boundaries.Slot80(a.F.Controller,ControllerLookupPrimaryTable.ObservedControllerPlayer,i);});
   Check(a.Boundaries.LastEndOfPathName.Read(0)!=null,"Decoded Idle name was built before the registry read");
  });
  Case("Slot80 MoveTo end-of-path reaches the unmodelled Idle entry body",delegate{
   var a=new Association();var i=new Invocation();a.MouseController.Count2F0.Write(0);
   var table=new PlayerMouseTable();table.BindObserved(ObservedPlayerMouseTable.MoveTo);
   var state=new PlayerMouseState(new object());a.MouseController.State2A0.Write(state);state.Virtual00.Write(table);
   ControllerPlayerLifecycleSlots.Bind(table).SelectObserved(ObservedPlayerMouseTable.MoveTo);ControllerPlayerStateFields.Bind(state,new NavigationView());
   ControllerAIStateTableBinding.Bind(state,ControllerAIStateTable.MoveTo);
   a.Boundaries.BindStateTransition(new ControllerStateTransitionContext(a.F.Lookup,Field<float>.Of(1f),Field<float>.Of(0f)));
   var ts=ControllerStateTransitionState.Bind(a.F.Lookup.Mouse,a.MouseController);
   var header=new ControllerStateRegistryNode();var nil=new ControllerStateRegistryNode();var idle=new ControllerStateRegistryNode();
   header.Nil19.Write(1);nil.Nil19.Write(1);idle.Nil19.Write(0);
   idle.Left00.Write(nil);idle.Right10.Write(nil);idle.Parent08.Write(header);header.Parent08.Write(idle);
   var idleName="Idle";idle.Name20.Capacity18.Write(15);idle.Name20.Length10.Write((ulong)idleName.Length);for(int n=0;n<idleName.Length;n++)idle.Name20.Inline00[n].Write((byte)idleName[n]);
   var idleState=new PlayerMouseState(new object());var idleTable=new PlayerMouseTable();idleTable.BindObserved(ObservedPlayerMouseTable.Idle);
   ControllerPlayerLifecycleSlots.Bind(idleTable).SelectObserved(ObservedPlayerMouseTable.Idle);ControllerPlayerStateFields.Bind(idleState,new NavigationView());idleState.Virtual00.Write(idleTable);idle.State40.Write(idleState);
   ts.Header290.Write(header);ts.Changed2D0.Write(0);ts.Data2A8.Qword00.Write(0);
   Check(ControllerPlayerLifecycleSlots.ForBound(idleTable,0).Begin220.Read(0)==0x165b30,"Idle Begin220 is published on the idle table");
   Check(ControllerPlayerLifecycleSlots.ForBound(table,0).End228.Read(0)==0x015880,"MoveTo End228 is the shared stub");
   var overlay=new MouseStateList();var sentinel=new MouseStateNode();ControllerOverlaySentinel.BindOwnSameAddress(overlay,sentinel);overlay.First00.Write(sentinel);a.MouseController.Overlay2E8.Write(overlay);
   // Exit, payload copy and state publication all ran; the stop is the new state entry read, whose Idle body 0x165B30 is not modelled (only MoveTo 0x166020 is).
   Stop(0x123ee0,delegate{a.Boundaries.Slot80(a.F.Controller,ControllerLookupPrimaryTable.ObservedControllerPlayer,i);});
   Check(a.Boundaries.LastEndOfPathName.Read(0)!=null,"Idle name was built");
   Check(ts.Data2A8.Qword00.Read(0)==0UL,"Payload head was copied into the state data before Entry");
   Same(idleState,ts.Current2A0.Read(0),"Registry node published the idle state");
  });
  Case("Slot80 MoveTo binding builds Idle and enters the modelled SetState",delegate{
   var a=new Association();var i=new Invocation();a.MouseController.Count2F0.Write(0);
   var state=new PlayerMouseState(new object());a.MouseController.State2A0.Write(state);state.Virtual00.Write(new PlayerMouseTable());
   ControllerAIStateTableBinding.Bind(state,ControllerAIStateTable.MoveTo);
   a.Boundaries.BindStateTransition(new ControllerStateTransitionContext(a.F.Lookup,Field<float>.Of(1f),Field<float>.Of(0f)));
   uint at=0;try{a.Boundaries.Slot80(a.F.Controller,ControllerLookupPrimaryTable.ObservedControllerPlayer,i);}catch(MissingContract error){at=error.Address;}
   var name=a.Boundaries.LastEndOfPathName.Read(0);
   Check(name!=null,"Decoded name object was built");Check(name.Length10.Read(0)==4&&name.Inline00[0].Read(0)==0x49&&name.Inline00[1].Read(0)==0x64&&name.Inline00[2].Read(0)==0x6c&&name.Inline00[3].Read(0)==0x65,"Decoded literal is the four bytes of Idle");
   Check(name.Capacity18.Read(0)==15,"SSO capacity preserved without inventing a heap name");
   Check(at==0x123d68,"Without a transition-state binding SetState stops before its registry read");
  });
  Case("Slot80 overlay traverses original list node state and rereads mutations",delegate{
   var a=new Association();var i=new Invocation();a.MouseController.Count2F0.Write(1);
   Stop(0x123821,delegate{a.Boundaries.Slot80(a.F.Controller,ControllerLookupPrimaryTable.ObservedControllerPlayer,i);});
   var list=new MouseStateList();a.MouseController.Overlay2E8.Write(list);Stop(0x123824,delegate{a.Boundaries.Slot80(a.F.Controller,ControllerLookupPrimaryTable.ObservedControllerPlayer,i);});
   var node=new MouseStateNode();list.First00.Write(node);Stop(0x123828,delegate{a.Boundaries.Slot80(a.F.Controller,ControllerLookupPrimaryTable.ObservedControllerPlayer,i);});Unknown(a.Boundaries.Last80State,"Unknown node state transported without defaulting");
   var state=new PlayerMouseState(new object());node.State10.Write(state);Stop(0x12382b,delegate{a.Boundaries.Slot80(a.F.Controller,ControllerLookupPrimaryTable.ObservedControllerPlayer,i);});Same(state,a.Boundaries.Last80State.Read(0),"Exact overlay state identity");Unknown(a.Boundaries.Last80Primary,"Unknown table reaches actual indirect JMP");
   var table=new PlayerMouseTable();state.Virtual00.Write(table);Stop(0x12382b,delegate{a.Boundaries.Slot80(a.F.Controller,ControllerLookupPrimaryTable.ObservedControllerPlayer,i);});Same(table,a.Boundaries.Last80Primary.Read(0),"Table observed without inventing slot70");
   var other=new PlayerMouseState(new object());node.State10.Write(other);Stop(0x12382b,delegate{a.Boundaries.Slot80(a.F.Controller,ControllerLookupPrimaryTable.ObservedControllerPlayer,i);});Same(other,a.Boundaries.Last80State.Read(0),"A later invocation rereads live node state");Unknown(a.Boundaries.Last80Primary,"Prior known table is not retained after state mutation");Unknown(a.MouseController.State2A0,"Overlay branch does not initialize direct state");
  });
  Case("Move already-there caller clears moving before05F909 capture and unknown05F91E call",delegate{
   var f=new NotifyCallerInputs(true);Check(f.Manager.Moving38.Read(0)==9,"Nonzero OWN moving state before actual InlineStop");Stop(0x05f91e,f.Run);f.AssertStopped(0x05f909,0x05f91e);
   Check(f.AlreadyCalls==1&&f.MoveRequests==0&&f.RegionCalls==1&&f.IsMovingCalls==0,"Actual already-there branch skips request and fallback");
  });
  Case("Move failure fallback preserves OWN zero before05F9B4 capture and unknown05F9C9 call",delegate{
   var f=new NotifyCallerInputs(false);Check(f.Manager.Moving38.Read(0)==0,"Explicit OWN initial zero, not a claimed fallback write");Stop(0x05f9c9,f.Run);f.AssertStopped(0x05f9b4,0x05f9c9);
   Check(f.AlreadyCalls==2&&f.MoveRequests==1&&f.RegionCalls==2&&f.IsMovingCalls==1,"Actual failure/current-position branch selected from fixture inputs");Unknown(f.Role.Handler24C0,"Zero IsMoving skips unproved Executor.Stop/handler dependencies");
  });
  Case("Reached count store only accepts exact registered controller state",delegate{
   var f=new MouseFixture();var wrong=new MouseControllerState(f.Controller);wrong.Count2F0.Write(17);var i=new Invocation();
   Stop(0x122a1a,delegate{ControllerMouseConstructorContracts.ApplyReachedCountZero122A1A(f.Mouse,wrong,i);});Check(wrong.Count2F0.Read(0)==17,"Rejected parallel state unchanged");Unknown(f.MouseController.Count2F0,"Canonical state not initialized by rejected store");
   var unregistered=new MouseControllerState(new Controller());Stop(0x122a1a,delegate{ControllerMouseConstructorContracts.ApplyReachedCountZero122A1A(f.Mouse,unregistered,i);});Unknown(unregistered.Count2F0,"Unregistered state remains unknown");
   Stop(0x122a1a,delegate{ControllerMouseConstructorContracts.ApplyReachedCountZero122A1A(null,f.MouseController,i);});
   ControllerMouseConstructorContracts.ApplyReachedCountZero122A1A(f.Mouse,f.MouseController,i);Check(i.At==0x122a1e&&f.MouseController.Count2F0.Read(0)==0,"Conditional qword store completed at next instruction");Unknown(f.MouseController.State2A0,"Store does not publish active state");Unknown(f.MouseController.Overlay2E8,"Store does not initialize overlay list");Check(f.MouseController.Byte438.Read(0)==7&&f.MouseController.Dword43C.Read(0)==99,"Adjacent controller fields preserved");
  });
  Case("Conditional count store advances actual mouse caller only to unknown selected state",delegate{
   var f=new MouseFixture();f.CaptureController();ConfigureActualAlive(f);Stop(0x15e292,delegate{f.CallCaptured(f.AliveBoundaries);});
   var i=new Invocation();ControllerMouseConstructorContracts.ApplyReachedCountZero122A1A(f.Mouse,f.MouseController,i);
   Stop(0x15e2b3,delegate{f.CallCaptured(f.AliveBoundaries);});
   Check(f.AliveBoundaries.LastLookup.Returned&&f.AliveBoundaries.LastAlive.Returned&&f.AliveBoundaries.LastAlive.Result.Read(0)==1,"Actual128530/Alive succeeded under explicit OWN publications");Same(f.Controller,f.Caller.CapturedController.Read(0),"Real62310 and1BCD0 Controller is unchanged");Same(f.Move,f.AliveBoundaries.LastLookup.Result.Read(0),"Original role/vptr remains shared");
   Check(f.Caller.ReachedCall&&!f.Caller.ActionReturned&&f.Caller.Mouse.At==0x15e2b3,"Unknown selected state remains explicit, no MoveTo success claimed");Unknown(f.MouseController.State2A0,"No invented state from cold count store");Unknown(f.Position.Q0,"No default region was required for opaque position copy");
   f.MouseController.Count2F0.Write(1);Stop(0x15e2a3,delegate{f.CallCaptured(f.AliveBoundaries);});Check(f.MouseController.Count2F0.Read(0)==1,"Later external count change is live, not reset to constructor value");Unknown(f.MouseController.Overlay2E8,"Unknown overlay is not an empty list");Check(f.InputId.Read(0)==91&&f.MouseController.Byte438.Read(0)==7,"No false-gate cleanup after unknown state");Same(f.Current,f.Controller.CurrentA0.Read(0),"Existing current action untouched");Same(f.Pending,f.Controller.PendingA8.Read(0),"Existing pending action untouched");
  });
  Console.WriteLine("{\"status\":\"passed-controller-move-association-prefix\",\"scenarios\":"+scenarios+",\"assertions\":"+assertions+",\"actual_move_to_begin\":true,\"actual_controller_association\":true,\"shared_identity\":true,\"conditional_count_store\":true,\"actual_slot60_store\":true,\"actual_slot80_selection\":true,\"unknown_state_slot70_retained\":true,\"prestart_slot70_empty_stub\":true,\"moveto_endofpath_setstate_entered\":true,\"caller_routing_regressions\":2,\"caller_navigation_is_fixture_input\":true,\"caller_production_navigation_verified\":false,\"old_suites_executed\":false,\"native_active_state_initialized\":false,\"native_constructor_completed\":false,\"runtime_class_graph_verified\":false,\"movement_executed\":false,\"physical_mouse_verified\":false}");return 0;
 }catch(Exception error){Console.Error.WriteLine(error);return 1;}}
}
