using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// New mouse-dispatch control-flow cases. OBSERVATION fixtures below provide
// explicitly named external observations only. They do not validate a native
// player/options/hot-slot producer, skill execution or movement completion.
// The production boundaries still stop at their unrecovered concrete bodies.
public static class PlayerMouseContractsTests {
 static int scenarios,assertions;
 public static int Scenarios{get{return scenarios;}}
 public static int Assertions{get{return assertions;}}
 static void Check(bool value,string why){assertions++;if(!value)throw new Exception(why);}
 static void Equal<T>(T expected,T actual,string why){Check(EqualityComparer<T>.Default.Equals(expected,actual),why+" expected="+expected+" actual="+actual);}
 static void Same(object expected,object actual,string why){Check(Object.ReferenceEquals(expected,actual),why);}
 static void Test(string name,Action body){body();scenarios++;Console.WriteLine("PASS "+name);}
 static void Missing(uint at,Action body,string why){try{body();}catch(MissingContract error){Equal(at,error.Address,why+" address");return;}throw new Exception("Expected unsupported observation: "+why);}
 static void Unknown<T>(Field<T> field,string why){Missing(0,delegate{field.Read(0);},why);}

 sealed class Fixture {
  public readonly Controller Controller=new Controller();
  public readonly ActionRecord Current=new ActionRecord(),Pending=new ActionRecord();
  public readonly MoveCharacter Role=new MoveCharacter();
  public readonly MoveContext Movement=new MoveContext();
  public readonly WorldEngine Engine=new WorldEngine();
  public readonly MouseControllerState Mouse;
  public readonly MouseRoleState RoleView;
  public readonly PlayerMouseState State=new PlayerMouseState(new object());
  public readonly PlayerMouseTable Table=new PlayerMouseTable();
  public readonly PlayerMouseContext Context;
  public readonly PlayerMouseBoundaries Production;
  public readonly Field<uint> ObjectId=Field<uint>.Of(91);
  public readonly MouseInputFrame Input=new MouseInputFrame();
  public readonly MouseSelectorFrame Selector=new MouseSelectorFrame();
  public Fixture():this(null){}
  public Fixture(Field<object> roleVptr){
   Controller.CurrentA0.Write(Current);Controller.PendingA8.Write(Pending);Movement.EngineGlobal.Write(Engine);
   Context=new PlayerMouseContext(new MoveBoundaries(Movement));Mouse=new MouseControllerState(Controller);RoleView=roleVptr==null?new MouseRoleState(Role):new MouseRoleState(Role,roleVptr);
   Context.Attach(Mouse);Context.Attach(RoleView);Production=new PlayerMouseBoundaries(Context);
   Table.BindObserved(ObservedPlayerMouseTable.Idle);State.Virtual00.Write(Table);State.Owner08.Write(Controller);State.CachedRole10.Write(Role);
   Mouse.State2A0.Write(State);Mouse.Count2F0.Write(0);Mouse.Byte438.Write(7);Mouse.Dword43C.Write(83);Mouse.Byte431.Write(9);Mouse.Byte432.Write(11);
   RoleView.Virtual00.Write(new MouseObjectTable());RoleView.Byte4A70.Write(0);RoleView.Byte4A61.Write(13);
   Input.ObjectId.Write(ObjectId);
  }
  public Observations Observe(){var result=new Observations(Context);result.ResolvedRole=Role;return result;}
  public byte Outer(PlayerMouseBoundaries b){return PlayerMouseContracts.HandleActionFromMouse15E1F0(Controller,0,0,0,Input,b,new Invocation());}
  public byte Primary(byte first,PlayerMouseBoundaries b){return PlayerMouseContracts.SelectPrimary161D00(State,first,0x23,0x45,Selector,b,new Invocation());}
  public byte Secondary(PlayerMouseBoundaries b){return PlayerMouseContracts.SelectSecondary162290(State,0x23,0x45,Selector,b,new Invocation());}
  public byte Super(PlayerMouseBoundaries b){return PlayerMouseContracts.ActivateSuper1624B0(State,0x23,0x45,Selector,b,new Invocation());}
  public void ActionIdentity(){Same(Current,Controller.CurrentA0.Read(0),"Existing current action unchanged");Same(Pending,Controller.PendingA8.Read(0),"Existing pending action unchanged");}
  public void OriginalCleanupFields(){Equal((byte)7,Mouse.Byte438.Read(0),"438 not cleared");Equal(83u,Mouse.Dword43C.Read(0),"43C not cleared");Equal(91u,ObjectId.Read(0),"Original object ID retained");}
 }

 // These are test observations for ordering/failure-prefix checks, never
 // installed as recovered production implementations. Skill260 delegates to
 // its production stop; there is no successful skill or movement callback.
 sealed class Observations:PlayerMouseBoundaries {
  public MoveCharacter ResolvedRole;
  public byte Act;
  public int OptionValue;
  public int ResolveCalls,GateCalls,OptionsCalls,DisplayedCalls;
  public readonly object Options=new object();
  public object GateTable;
  public MouseSkillSet FirstDisplayed,SecondDisplayed;
  public bool ObserveOptionId;
  public bool CaptureKnownArguments;
  public uint ObservedId;
  public PlayerMouseState MutatedState;
  public PlayerMouseTable ReplacementTable,CapturedSkillTable;
  public bool MutateOuterOnDisplayed;
  public MouseControllerState OuterController;
  public PlayerMouseState ReplacementSelected,CapturedMoveState;
  public MouseInputFrame OuterFrame;
  public WorldPosition24 ReplacementPosition,OriginalPositionToMutate;
  public byte ReplacementB4;
  public PlayerMouseTable CapturedMoveTable;
  public byte CapturedMoveA,CapturedMoveB;
  public WorldPosition24 CapturedMovePosition;
  public byte CapturedA,CapturedB;
  public WorldPosition24 CapturedPosition;
  public Field<uint> CapturedId;
  public Observations(PlayerMouseContext context):base(context){Act=0;}
  public override MoveCharacter ResolvePlayer128530(Controller controller){ResolveCalls++;return ResolvedRole;}
  public override byte CanAct460(object table,MoveCharacter role){GateCalls++;GateTable=table;return Act;}
  public override object GetOptions609AD8(WorldEngine engine){return Options;}
  public override int OptionInt60A148(object options,uint index){Same(Options,options,"OBSERVATION actual options argument");Equal(13u,index,"OBSERVATION requested option index");OptionsCalls++;return OptionValue;}
  public override MouseSkillSet DisplayedSkillSet3EAE40(MoveCharacter role){
   DisplayedCalls++;
   if(MutateOuterOnDisplayed){
    OuterController.State2A0.Write(ReplacementSelected);OuterFrame.B4.Write(ReplacementB4);OuterFrame.Position.Write(ReplacementPosition);
    if(OriginalPositionToMutate!=null){OriginalPositionToMutate.Q0.Write(0x9999);OriginalPositionToMutate.D8.Write(77);OriginalPositionToMutate.DC.Write(88);OriginalPositionToMutate.D10.Write(99);OriginalPositionToMutate.D14.Write(0xabcdef01);}
   }
   return DisplayedCalls==1?FirstDisplayed:SecondDisplayed;
  }
  public override uint OptionIdA0(object table,object option){
   if(!ObserveOptionId)return base.OptionIdA0(table,option);
   if(MutatedState!=null)MutatedState.Virtual00.Write(ReplacementTable);
   return ObservedId;
  }
  public override byte Skill260(PlayerMouseTable table,PlayerMouseState state,byte a,Field<byte> b,uint skillId,Field<WorldPosition24> position,Field<Field<uint>> objectId,uint at){
   CapturedSkillTable=table;CapturedA=a;
   // Only the explicit known-argument observation cases consume the copied
   // byte/pointer payloads. Unknown ABI transfers reach the real boundary unchanged.
   if(CaptureKnownArguments){CapturedB=b.Read(at);CapturedPosition=position.Read(at);CapturedId=objectId.Read(at);}
   return base.Skill260(table,state,a,b,skillId,position,objectId,at);
  }
  public override void Move280(PlayerMouseTable table,PlayerMouseState state,byte a,Field<byte> b,Field<WorldPosition24> position,uint at){
   CapturedMoveTable=table;CapturedMoveState=state;CapturedMoveA=a;
   if(CaptureKnownArguments){CapturedMoveB=b.Read(at);CapturedMovePosition=position.Read(at);}
   base.Move280(table,state,a,b,position,at);
  }
 }

 // Dedicated ABI observation: only this fixture rewrites received wrappers.
 // Both skill results still come from the actual bound 01A700 zero body.
 sealed class SecondaryTransferObservations:PlayerMouseBoundaries {
  public sealed class CallObservation {
   public readonly Field<byte> B;
   public readonly Field<WorldPosition24> Position;
   public readonly Field<Field<uint>> ObjectId;
   public readonly byte BValue;
   public readonly WorldPosition24 PositionValue;
   public readonly Field<uint> IdValue;
   public readonly uint PayloadValue,SkillId;
   public byte Returned;
   public CallObservation(Field<byte> b,Field<WorldPosition24> position,Field<Field<uint>> objectId,uint skillId,uint at){
    B=b;Position=position;ObjectId=objectId;SkillId=skillId;
    // This scenario supplies known values explicitly; these reads belong only
    // to the observation, not to the native zero-return implementation.
    BValue=b.Read(at);PositionValue=position.Read(at);IdValue=objectId.Read(at);PayloadValue=IdValue.Read(at);
   }
  }
  public readonly object Option=new object(),Manager=new object(),Skill=new object();
  public readonly WorldPosition24 ReplacementPosition=new WorldPosition24();
  public readonly Field<uint> ReplacementId=Field<uint>.Of(0xabcdef01);
  public readonly List<CallObservation> Calls=new List<CallObservation>();
  public int UseDefaultCalls,DefaultSkillCalls;
  public SecondaryTransferObservations(PlayerMouseContext context):base(context){}
  public override object SecondarySlot160070(Controller controller){return Option;}
  public override uint OptionIdA0(object table,object option){return 0x2468;}
  public override object ObjectManager609098(){return Manager;}
  public override object Skill078240(object manager,uint id){return Skill;}
  public override byte UseDefault4910C0(object skill){UseDefaultCalls++;return 1;}
  public override uint DefaultSkillE0(object table,MoveCharacter role,uint index){DefaultSkillCalls++;return 0x1357;}
  public override byte Skill260(PlayerMouseTable table,PlayerMouseState state,byte a,Field<byte> b,uint skillId,Field<WorldPosition24> position,Field<Field<uint>> objectId,uint at){
   var call=new CallObservation(b,position,objectId,skillId,at);Calls.Add(call);
   if(Calls.Count==1){
    call.IdValue.Write(0xaabbccdd); // The pointee is shared with the caller.
    b.Write(0xee);position.Write(ReplacementPosition);objectId.Write(ReplacementId);
   }
   call.Returned=base.Skill260(table,state,a,b,skillId,position,objectId,at);return call.Returned;
  }
 }

 public static void Run(){
  scenarios=0;assertions=0;
  Test("Production mouse dependencies stop explicitly before inventing a player or options result",delegate{
   var f=new Fixture();Missing(0x128530,delegate{f.Outer(f.Production);},"Real unresolved player dependency");f.OriginalCleanupFields();f.ActionIdentity();
   Missing(0x609ad8,delegate{f.Primary(1,f.Production);},"Real unresolved options getter");Unknown(f.Selector.ObjectId,"No invented object-ID pointer");Unknown(f.Selector.Position,"No invented position pointer");
  });
  Test("OBSERVATION null player returns false without controller cleanup or frame consumption",delegate{
   var f=new Fixture();var observed=f.Observe();observed.ResolvedRole=null;
   Equal((byte)0,f.Outer(observed),"Null observed player returns false");Equal(1,observed.ResolveCalls,"One resolver observation");Equal(0,observed.GateCalls,"No role virtual call");
   f.OriginalCleanupFields();Unknown(f.Input.Position,"Position pointer never consumed");Unknown(f.Input.B4,"Late stack bool never consumed");f.ActionIdentity();
  });
  Test("OBSERVATION false role gate preserves cleanup order through a missing final ID destination",delegate{
   var f=new Fixture();var observed=f.Observe();
   Equal((byte)0,f.Outer(observed),"False role gate returns false");Equal((byte)0,f.Mouse.Byte438.Read(0),"438 cleared");Equal(0u,f.Mouse.Dword43C.Read(0),"43C cleared");Equal(0u,f.ObjectId.Read(0),"Object ID cleared last");Unknown(f.RoleView.Byte242A,"False role gate short-circuits 242A");
   Same(f.RoleView.Virtual00.Read(0),observed.GateTable,"Role gate receives its captured table identity");
   f.Mouse.Byte438.Write(7);f.Mouse.Dword43C.Write(83);f.ObjectId.Write(91);f.Input.ObjectId.Write(null);
   Missing(0x15e3d3,delegate{f.Outer(observed);},"Null final ID destination stops at its store");Equal((byte)0,f.Mouse.Byte438.Read(0),"438 prefix retained");Equal(0u,f.Mouse.Dword43C.Read(0),"43C prefix retained");Equal(91u,f.ObjectId.Read(0),"Detached ID cell was not written");f.ActionIdentity();
  });
  Test("OBSERVATION primary first byte bypasses unknown role ID position and tail when option is nonzero",delegate{
   var f=new Fixture();var observed=f.Observe();observed.OptionValue=-3;f.State.Owner08.Forget();f.State.CachedRole10.Forget();f.State.Virtual00.Forget();
   Equal((byte)0,f.Primary(0x80,observed),"Noncanonical nonzero first byte takes option fallback");Equal(1,observed.OptionsCalls,"One final options read");Equal(0,observed.ResolveCalls,"No eager role-cache refill");
   Unknown(f.Selector.ObjectId,"Unused ID pointer stays unknown");Unknown(f.Selector.Position,"Opaque position pointer is not consumed");Unknown(f.Selector.TailByte,"Sixth bool is not consumed");f.ActionIdentity();
  });
  Test("OBSERVATION zero option reaches the real Idle movement boundary without creating an action",delegate{
   var f=new Fixture();var observed=f.Observe();var position=new WorldPosition24();f.Selector.Position.Write(position);f.State.CachedRole10.Forget();
   Missing(0x165cb0,delegate{f.Primary(1,observed);},"Observed Idle slot280 retains its actual missing body");Equal(1,observed.OptionsCalls,"One options observation");
   Unknown(position.Q0,"Movement boundary does not synthesize region");Unknown(position.D14,"Movement boundary does not fill padding");Unknown(f.Selector.ObjectId,"First-byte path did not require ID reference");Unknown(f.Selector.TailByte,"Ignored sixth bool still unknown");f.ActionIdentity();
  });
  Test("Actual secondary and super 4A70 gates return false without reading later frame data",delegate{
   var f=new Fixture();f.RoleView.Byte4A70.Write(1);f.State.Owner08.Forget();f.State.Virtual00.Forget();
   Equal((byte)0,f.Secondary(f.Production),"Secondary observed field gate");Equal((byte)0,f.Super(f.Production),"Super observed field gate");
   Unknown(f.Selector.Position,"Unused position remains unknown");Unknown(f.Selector.ObjectId,"Unused object-ID pointer remains unknown");Unknown(f.Selector.TailByte,"Unused tail remains unknown");Equal((byte)13,f.RoleView.Byte4A61.Read(0),"Gate bypasses later 4A61 store");f.ActionIdentity();
  });
  Test("OBSERVATION outer primary writes its real false result to 432 while secondary leaves 432 alone",delegate{
   var f=new Fixture();var observed=f.Observe();observed.Act=1;observed.OptionValue=-3;f.RoleView.Byte242A.Write(0);f.RoleView.Byte4A70.Write(1);var position=new WorldPosition24();f.Input.Position.Write(position);f.Input.B4.Forget();
   Equal((byte)0,PlayerMouseContracts.HandleActionFromMouse15E1F0(f.Controller,1,1,0,f.Input,observed,new Invocation()),"Real primary option gate returns false through outer");Equal((byte)0,f.Mouse.Byte432.Read(0),"Primary result writes 432");Equal(1,observed.OptionsCalls,"Primary performed the final options observation");
   f.Mouse.Byte432.Write(0x7b);
   Equal((byte)0,PlayerMouseContracts.HandleActionFromMouse15E1F0(f.Controller,1,0,0,f.Input,observed,new Invocation()),"Real secondary role gate returns false through outer");Equal((byte)0x7b,f.Mouse.Byte432.Read(0),"Secondary has no 432 result store");Equal(1,observed.OptionsCalls,"Secondary gate did not read primary options");
   Unknown(f.Input.B4,"Both outer gate paths copy the unknown fourth bool without consuming it");Unknown(f.Mouse.Byte430,"Unknown forwarded tail was never consumed");Unknown(position.Q0,"Original position token remains opaque");Unknown(position.D14,"Original tail remains opaque");f.ActionIdentity();
  });
  Test("OBSERVATION outer reselects state and B4 after super while retaining its earlier local position copy",delegate{
   var f=new Fixture();var observed=f.Observe();observed.Act=1;f.RoleView.Byte242A.Write(0);var original=new WorldPosition24();original.Q0.Write(0x778899);original.D8.Write(1);original.DC.Write(2);original.D10.Write(3);f.Input.Position.Write(original);f.Input.B4.Write(0x11);
   var replacement=new PlayerMouseState(new object());var table=new PlayerMouseTable();table.BindObserved(ObservedPlayerMouseTable.MoveTo);replacement.Virtual00.Write(table);
   var otherPosition=new WorldPosition24();observed.CaptureKnownArguments=true;observed.MutateOuterOnDisplayed=true;observed.OuterController=f.Mouse;observed.ReplacementSelected=replacement;observed.OuterFrame=f.Input;observed.ReplacementPosition=otherPosition;observed.ReplacementB4=0x91;observed.OriginalPositionToMutate=original;
   Missing(0x1662d0,delegate{PlayerMouseContracts.HandleActionFromMouse15E1F0(f.Controller,1,1,0,f.Input,observed,new Invocation());},"Fresh MoveTo state reaches its actual unsupported movement body");Equal(1,observed.DisplayedCalls,"Real super stopped after its observed null displayed set");Same(replacement,f.Mouse.State2A0.Read(0),"Observed state replacement persists");Same(replacement,observed.CapturedMoveState,"Outer selected the changed state");Same(table,observed.CapturedMoveTable,"Changed selected state supplied the movement table");
   Equal((byte)0,observed.CapturedMoveA,"Saved outer b3 forwarded");Equal((byte)0x91,observed.CapturedMoveB,"Late B4 read observes the callback mutation");Same(otherPosition,f.Input.Position.Read(0),"Original frame pointer mutation persists");
   Check(!Object.ReferenceEquals(original,observed.CapturedMovePosition)&&!Object.ReferenceEquals(otherPosition,observed.CapturedMovePosition),"Movement receives the local copy rather than either source address");Equal(0x778899UL,observed.CapturedMovePosition.Q0.Read(0),"Local region captured before super");Equal(1u,observed.CapturedMovePosition.D8.Read(0),"Local X bits captured before super");Equal(2u,observed.CapturedMovePosition.DC.Read(0),"Local Y bits captured before super");Equal(3u,observed.CapturedMovePosition.D10.Read(0),"Local Z bits captured before super");Unknown(observed.CapturedMovePosition.D14,"Early unknown tail snapshot survives later source mutation");
   Unknown(f.Mouse.Byte430,"Unused sixth bool remains opaque");Equal((byte)11,f.Mouse.Byte432.Read(0),"Movement failure precedes outer result publication");f.ActionIdentity();
  });
  Test("OBSERVATION super queue is consumed before a later unknown option vptr stops dispatch",delegate{
   var f=new Fixture();var observed=f.Observe();var option=new object();var view=new MouseObjectView(option);f.Context.Attach(view);var set=new MouseSkillSet();set.Queued00.Write(option);observed.FirstDisplayed=set;observed.SecondDisplayed=set;
   Missing(0x16257e,delegate{f.Super(observed);},"Unknown option vptr is consumed at its late virtual call");Equal(2,observed.DisplayedCalls,"Both displayed-set calls occurred");Same(null,set.Queued00.Read(0),"Actual queued-option clear retained");Equal((byte)0,f.RoleView.Byte4A61.Read(0),"Actual 4A61 clear retained");Equal(91u,f.ObjectId.Read(0),"No ID write before skill dispatch");f.ActionIdentity();
  });
  Test("OBSERVATION super retains the captured state table across the option-ID call",delegate{
   var f=new Fixture();var observed=f.Observe();var option=new object();var view=new MouseObjectView(option);view.Virtual00.Write(new MouseObjectTable());f.Context.Attach(view);var set=new MouseSkillSet();set.Queued00.Write(option);observed.FirstDisplayed=set;observed.SecondDisplayed=set;
   var replacement=new PlayerMouseTable();replacement.BindObserved(ObservedPlayerMouseTable.Startup);observed.CaptureKnownArguments=true;observed.ObserveOptionId=true;observed.ObservedId=0x55667788;observed.MutatedState=f.State;observed.ReplacementTable=replacement;
   var position=new WorldPosition24();f.Selector.Position.Write(position);f.Selector.ObjectId.Write(f.ObjectId);
   Missing(0x165cd0,delegate{f.Super(observed);},"Captured Idle slot260 still stops despite new Startup vptr");Same(replacement,f.State.Virtual00.Read(0),"Observed vptr mutation retained");Same(f.Table,observed.CapturedSkillTable,"Original table was captured before option-ID call");
   Equal((byte)0x23,observed.CapturedA,"First register byte forwarded verbatim");Equal((byte)0x45,observed.CapturedB,"Second register byte forwarded verbatim");Same(position,observed.CapturedPosition,"Original position address forwarded");Same(f.ObjectId,observed.CapturedId,"Original ID reference forwarded");
   Same(null,set.Queued00.Read(0),"Queue clear survives the real skill stop");Equal((byte)0,f.RoleView.Byte4A61.Read(0),"4A61 clear survives the real skill stop");f.ActionIdentity();
  });
  Test("Actual Startup zero body accepts opaque pointer transfers after OBSERVATION option production",delegate{
   var f=new Fixture();var observed=f.Observe();f.Table.BindObserved(ObservedPlayerMouseTable.Startup);var option=new object();var view=new MouseObjectView(option);view.Virtual00.Write(new MouseObjectTable());f.Context.Attach(view);var set=new MouseSkillSet();set.Queued00.Write(option);observed.FirstDisplayed=set;observed.SecondDisplayed=set;observed.ObserveOptionId=true;observed.ObservedId=0x1234;
   Equal((byte)0,f.Super(observed),"Actual 01A700 body returns zero without dereferencing transferred pointers");Same(f.Table,observed.CapturedSkillTable,"Observed Startup table supplied the real zero body");Equal(2,observed.DisplayedCalls,"Super still performs both displayed-set calls");
   Unknown(f.Selector.Position,"Entry position MOV copied Unknown without consuming it");Unknown(f.Selector.ObjectId,"Late object-ID pointer MOV copied Unknown without consuming it");Unknown(f.Selector.TailByte,"Unrelated trailing bool remains unknown");Same(null,set.Queued00.Read(0),"Queued option clear precedes zero-body return");Equal((byte)0,f.RoleView.Byte4A61.Read(0),"4A61 clear precedes zero-body return");Equal(91u,f.ObjectId.Read(0),"Unpassed known ID cell was not substituted");f.ActionIdentity();
  });
  Test("OBSERVATION wrapper writes cannot change the second real Startup call while ID payload writes remain shared",delegate{
   var sharedVptr=new Field<object>();var f=new Fixture(sharedVptr);Same(sharedVptr,f.RoleView.Virtual00,"Role view borrows the supplied canonical vptr field");var observed=new SecondaryTransferObservations(f.Context);f.Table.BindObserved(ObservedPlayerMouseTable.Startup);
   var optionView=new MouseObjectView(observed.Option);optionView.Virtual00.Write(new MouseObjectTable());optionView.Option10.Write(0);f.Context.Attach(optionView);f.RoleView.SkillManagerVirtual850.Write(new MouseObjectTable());
   var position=new WorldPosition24();f.Selector.Position.Write(position);f.Selector.ObjectId.Write(f.ObjectId);f.Selector.TailByte.Write(0);
   Equal((byte)0,f.Secondary(observed),"Secondary returns false after both real zero-body calls");Equal(2,observed.Calls.Count,"Both Startup skill calls reached the actual boundary");Equal(1,observed.UseDefaultCalls,"Explicit default-use observation follows the first zero result");Equal(1,observed.DefaultSkillCalls,"Explicit default skill observation feeds the second call");
   var first=observed.Calls[0];var second=observed.Calls[1];Equal((byte)0,first.Returned,"First result came from actual 01A700");Equal((byte)0,second.Returned,"Second result came from actual 01A700");Equal(0x2468u,first.SkillId,"First observed option ID forwarded");Equal(0x1357u,second.SkillId,"Second observed default ID forwarded");
   Equal((byte)0x45,first.BValue,"First copied B4 starts with the input value");Equal((byte)0x45,second.BValue,"First wrapper write did not replace saved B4");Same(position,first.PositionValue,"First pointer targets the supplied position");Same(position,second.PositionValue,"First wrapper write did not replace saved position");Same(f.ObjectId,first.IdValue,"First pointer targets the supplied ID cell");Same(f.ObjectId,second.IdValue,"First wrapper write did not replace saved ID reference");Equal(91u,first.PayloadValue,"First observation sees the original ID payload");Equal(0xaabbccddu,second.PayloadValue,"Second call sees the shared ID payload write");Equal(0xaabbccddu,f.ObjectId.Read(0),"Original ID cell retains the payload write");
   Equal((byte)0xee,first.B.Read(0),"First B4 wrapper was actually overwritten");Same(observed.ReplacementPosition,first.Position.Read(0),"First position wrapper was actually overwritten");Same(observed.ReplacementId,first.ObjectId.Read(0),"First ID wrapper was actually overwritten");Check(!Object.ReferenceEquals(first.B,second.B),"Each call owns a distinct B4 wrapper");Check(!Object.ReferenceEquals(first.Position,second.Position),"Each call owns a distinct position wrapper");Check(!Object.ReferenceEquals(first.ObjectId,second.ObjectId),"Each call owns a distinct ID wrapper");Same(position,f.Selector.Position.Read(0),"Original frame position pointer is unchanged");Same(f.ObjectId,f.Selector.ObjectId.Read(0),"Original frame ID pointer is unchanged");f.ActionIdentity();
  });
  Test("OBSERVATION repeated displayed-set calls use the second view before alternate-config failure",delegate{
   var f=new Fixture();var observed=f.Observe();var token=new object();var first=new MouseSkillSet();first.Queued00.Write(token);var second=new MouseSkillSet();second.Queued00.Write(null);observed.FirstDisplayed=first;observed.SecondDisplayed=second;f.RoleView.HotSlotOwner4A48.Write(f.Role);
   Missing(0x3e2570,delegate{f.Super(observed);},"Real alternate-config dependency remains unsupported");Equal(2,observed.DisplayedCalls,"Displayed set reread instead of cached");Same(token,first.Queued00.Read(0),"First set only served the entry gate");Same(null,second.Queued00.Read(0),"Second set supplies the null queued option");Equal((byte)13,f.RoleView.Byte4A61.Read(0),"Failure occurs before later 4A61 clear");f.ActionIdentity();
  });
 }
}
public static class PlayerMouseContractTestProgram {
 public static int Main(){try{PlayerMouseContractsTests.Run();Console.WriteLine("PASS "+PlayerMouseContractsTests.Scenarios+" scenarios, "+PlayerMouseContractsTests.Assertions+" assertions; new player mouse state-flow contracts with explicit external observations only");return 0;}catch(Exception error){Console.Error.WriteLine(error);return 1;}}
}
