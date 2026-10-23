using System;
using System.Collections.Generic;
using GrimDynasty.ReverseClient.Actions;

// Six new displayed-set component scenarios. Trees and bindings are explicit
// OWN inputs, not native construction/population or physical mouse activity.
// The Super prefix uses the concrete producer without successful callbacks.
public static class PlayerHotSlotDisplayedTests {
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
  public readonly MoveCharacter Move=new MoveCharacter();
  public readonly GameMovementCharacterState Owner;
  public readonly GameMovementIdentityBridge Bridge;
  public readonly PlayerMouseContext Mouse;
  public readonly MouseRoleState Role;
  public readonly PlayerMousePlayerLookupContext Context;
  public readonly PlayerHotSlotState Hot;
  public readonly PlayerMouseDisplayedSkillSetBoundaries Boundaries;
  public Fixture():this(true){}
  public Fixture(bool bindHot){
   var character=new AddressSymbol<ClassRecord>(0xa5cba0);character.Write(new ClassRecord());
   var player=new AddressSymbol<ClassRecord>(0xa5cab0);player.Write(new ClassRecord());
   var callbacks=new GameMovementCallbackContext(new WorldCoordinateBoundaries(new WorldCoordinateState()),new EntityMovementRegistry(),character,player);
   Owner=new GameMovementCharacterState(new EntityMovementState(Move));callbacks.Attach(Owner);Bridge=new GameMovementIdentityBridge(callbacks);Bridge.Bind(Owner,Move);
   Mouse=new PlayerMouseContext(new MoveBoundaries(new MoveContext()));Role=new MouseRoleState(Move,Owner.Entity.Virtual00);
   Context=new PlayerMousePlayerLookupContext(Mouse,Bridge,new GameReachedGoalEngineState());Context.BindRole(Role);
   if(bindHot)Hot=PlayerHotSlotState.Bind(Context,Role);Boundaries=new PlayerMouseDisplayedSkillSetBoundaries(Context);
  }
 }
 sealed class Tree {
  public readonly PlayerHotSlotTreeNode Head=new PlayerHotSlotTreeNode();
  public readonly PlayerHotSlotTreeNode Root;
  public Tree(uint key,MouseSkillSet value){Head.Nil19.Write(1);Root=Leaf(Head,key,value);Head.Parent08.Write(Root);}
 }
 static PlayerHotSlotTreeNode Leaf(PlayerHotSlotTreeNode head,uint key,MouseSkillSet value){
  var node=new PlayerHotSlotTreeNode();node.Nil19.Write(0);node.Key20.Write(key);node.Value28.Write(value);node.Left00.Write(head);node.Right10.Write(head);return node;
 }
 static PlayerHotSlotTreeNode Empty(Fixture f,byte nil){var head=new PlayerHotSlotTreeNode();head.Nil19.Write(nil);head.Parent08.Write(head);f.Hot.Header88.Write(head);return head;}
 static void Select(Fixture f,bool fallback,uint key){f.Hot.Key9C.Write(fallback?0u:key);if(fallback)f.Hot.Key98.Write(key);}
 static MouseSkillSet Get(Fixture f,Invocation i){return PlayerHotSlotDisplayedContracts.GetDisplayedSkillSet3EAE40(f.Hot,i);}
 public static void Run(){scenarios=assertions=0;
  Test("Explicit HotSlot binding preserves original owner mouse and shared field cells",delegate{
   var f=new Fixture(false);
   Missing(0x1234,delegate{PlayerHotSlotState.ForBoundRole(f.Context,f.Move,0x1234);},"Lookup cannot implicitly create HotSlot state");
   var hot=PlayerHotSlotState.Bind(f.Context,f.Role);Same(hot,PlayerHotSlotState.Bind(f.Context,f.Role),"Same explicit binding is stable");Same(hot,PlayerHotSlotState.ForBoundRole(f.Context,f.Move,0),"Lookup returns exact bound state");
   Same(f.Move,hot.Role,"Original Move identity");Same(f.Owner,hot.Owner,"Original full owner");Same(f.Role,hot.Mouse,"Original mouse view");Same(f.Owner.Entity.Virtual00,f.Role.Virtual00,"Mouse still borrows canonical Entity vptr");
   Same(f.Role.HotSlotOwner4A48,hot.Owner00,"Owner00 aliases original4A48");Same(f.Role.Byte4A61,hot.Byte19,"Byte19 aliases original4A61");Same(f.Role.Byte4A70,hot.Byte28,"Byte28 aliases original4A70");
   Unknown(hot.Header88,"Binding does not invent a tree");Unknown(hot.Key98,"Binding does not select fallback key");Unknown(hot.Key9C,"Binding does not select active key");Unknown(hot.Owner00,"Binding does not invent hot-slot owner");
   hot.Owner00.Write(f.Move);f.Role.Byte4A61.Write(9);hot.Byte28.Write(7);
   Same(f.Move,f.Role.HotSlotOwner4A48.Read(0),"Owner write reaches original cell");Eq((byte)9,hot.Byte19.Read(0),"Original4A61 write visible through alias");Eq((byte)7,f.Role.Byte4A70.Read(0),"Alias write reaches original4A70");
   Invalid(delegate{PlayerHotSlotState.Bind(f.Context,new MouseRoleState(f.Move,f.Owner.Entity.Virtual00));},"Different mouse lifetime view cannot replace same role binding");Same(f.Role,f.Mouse.Role(f.Move,0),"Rejected binding preserves original registered view");
  });
  Test("Empty tree exact key missing key and known null value retain distinct real returns",delegate{
   var f=new Fixture();var i=new Invocation();f.Hot.Key9C.Write(0);var empty=Empty(f,0x80);
   Same(null,Get(f,i),"Any nonzero nil flag denotes explicit empty tree");Eq(0x3eaedau,i.At,"Empty tree returns without payload load");Unknown(f.Hot.Key98,"Empty fallback tree never consumes unknown98");Unknown(empty.Key20,"Empty header key is not consumed");Unknown(empty.Value28,"Empty header payload is not consumed");
   var set=new MouseSkillSet();var tree=new Tree(10,set);f.Hot.Header88.Write(tree.Head);f.Hot.Key98.Write(10);
   Same(set,Get(f,i),"Exact fallback key returns existing set");Eq(0x3eaed7u,i.At,"Exact lookup returns after real value load");
   tree.Root.Value28.Write(null);Same(null,Get(f,i),"Known null node payload is a normal matched result");Eq(0x3eaed7u,i.At,"Matched null has payload-load return");
   tree.Root.Value28.Forget();f.Hot.Key98.Write(9);Same(null,Get(f,i),"Lower bound above query is rejected by equality check");Eq(0x3eaedau,i.At,"Lower-bound mismatch uses miss return");Unknown(tree.Root.Value28,"Mismatch does not consume unknown candidate payload");
   f.Hot.Key98.Write(11);Same(null,Get(f,i),"Query above maximum leaves header candidate");Eq(0x3eaedau,i.At,"Header candidate uses miss return");
  });
  Test("Unsigned lower bound uses nonzero9C and falls back to98 only for zero9C",delegate{
   var f=new Fixture();var i=new Invocation();var low=new MouseSkillSet();var middle=new MouseSkillSet();var high=new MouseSkillSet();
   var tree=new Tree(0x80000000u,middle);tree.Root.Left00.Write(Leaf(tree.Head,0,low));tree.Root.Right10.Write(Leaf(tree.Head,UInt32.MaxValue,high));f.Hot.Header88.Write(tree.Head);
   f.Hot.Key9C.Write(0x80000000u);Same(middle,Get(f,i),"High-bit active key is compared unsigned");Unknown(f.Hot.Key98,"Nonzero9C ignores unknown98");
   f.Hot.Key9C.Write(UInt32.MaxValue);f.Hot.Key98.Write(0);Same(high,Get(f,i),"Nonzero9C wins over contradictory fallback98");
   f.Hot.Key9C.Write(0);Same(low,Get(f,i),"Zero9C selects actual key0 from98");
   f.Hot.Key98.Write(0x80000000u);Same(middle,Get(f,i),"Fallback branch also compares high-bit keys unsigned");
   f.Hot.Key9C.Write(0x7fffffffu);Same(null,Get(f,i),"Unsigned lower-bound candidate must equal query");Eq(0x3eaedau,i.At,"Unequal high-bit candidate is not returned");
  });
  Test("Fresh direct reads and concrete Super producer preserve the actual queue clear prefix",delegate{
   var f=new Fixture();var i=new Invocation();var first=new MouseSkillSet();var right=new MouseSkillSet();var changed=new MouseSkillSet();var replacement=new MouseSkillSet();
   var tree=new Tree(5,first);var child=Leaf(tree.Head,7,right);tree.Root.Right10.Write(child);f.Hot.Header88.Write(tree.Head);f.Hot.Key9C.Write(5);
   Same(first,Get(f,i),"Initial direct read");f.Hot.Key9C.Write(7);Same(right,Get(f,i),"Next direct call observes changed selection key");
   child.Value28.Write(changed);Same(changed,Get(f,i),"Next direct call observes changed payload");var next=new Tree(7,replacement);f.Hot.Header88.Write(next.Head);Same(replacement,Get(f,i),"Next direct call observes changed tree header");
   Same(replacement,f.Boundaries.DisplayedSkillSet3EAE40(f.Move),"Concrete boundary invokes actual getter");var prior=f.Boundaries.LastDisplayedInvocation;
   Same(replacement,f.Boundaries.DisplayedSkillSet3EAE40(f.Move),"Second concrete call reads same actual tree");Check(!Object.ReferenceEquals(prior,f.Boundaries.LastDisplayedInvocation),"Each boundary call has fresh invocation");Eq(2,f.Boundaries.DisplayedCalls,"Diagnostic counter records concrete calls");
   prior=f.Boundaries.LastDisplayedInvocation;f.Hot.Key9C.Forget();Missing(0x3eae47,delegate{f.Boundaries.DisplayedSkillSet3EAE40(f.Move);},"Failed later call reads newly Unknown9C");Check(!Object.ReferenceEquals(prior,f.Boundaries.LastDisplayedInvocation),"Failed call also owns fresh invocation");Eq(0x3eaed7u,prior.At,"Prior successful diagnostic is not rewritten");Eq(0x3eae47u,f.Boundaries.LastDisplayedInvocation.At,"Failed call cannot retain earlier successful address");
   var super=new Fixture();var set=new MouseSkillSet();var option=new object();set.Queued00.Write(option);var actual=new Tree(3,set);super.Hot.Header88.Write(actual.Head);super.Hot.Key9C.Write(3);super.Hot.Byte28.Write(0);super.Hot.Byte19.Write(13);
   var state=new PlayerMouseState(new object());state.CachedRole10.Write(super.Move);var table=new PlayerMouseTable();table.BindObserved(ObservedPlayerMouseTable.Idle);state.Virtual00.Write(table);var frame=new MouseSelectorFrame();var call=new Invocation();
   Missing(0x162575,delegate{PlayerMouseContracts.ActivateSuper1624B0(state,0x23,0x45,frame,super.Boundaries,call);},"Unbound queued option stops at actual object-view lookup");
   Eq(2,super.Boundaries.DisplayedCalls,"Super executed both concrete displayed-set getters");Same(null,set.Queued00.Read(0),"Actual Super consumed original queue");Eq((byte)0,super.Role.Byte4A61.Read(0),"Actual Super cleared original4A61 cell");Eq((byte)0,super.Hot.Byte19.Read(0),"HotSlot alias sees original clear");Eq(0x3eaed7u,super.Boundaries.LastDisplayedInvocation.At,"Both getter calls returned through real payload load");Same(null,super.Boundaries.LastLookup,"Explicit existing cached role bypasses resolver");
   var previous=super.Boundaries.LastDisplayedInvocation;var optionView=new MouseObjectView(option);super.Mouse.Attach(optionView);set.Queued00.Write(option);super.Hot.Byte19.Write(13);
   Missing(0x16257e,delegate{PlayerMouseContracts.ActivateSuper1624B0(state,0x23,0x45,frame,super.Boundaries,call);},"Unknown real option vptr stops at late virtual call");
   Eq(4,super.Boundaries.DisplayedCalls,"Second Super invocation again called real getter twice");Check(!Object.ReferenceEquals(previous,super.Boundaries.LastDisplayedInvocation),"Repeated Super has fresh getter diagnostics");Same(null,set.Queued00.Read(0),"Queue clear remains after vptr failure");Eq((byte)0,super.Role.Byte4A61.Read(0),"Original4A61 clear remains after vptr failure");Same(super.Move,state.CachedRole10.Read(0),"Existing cached Move identity retained");
   Unknown(optionView.Virtual00,"No option vptr was synthesized");Unknown(frame.Position,"Opaque entry position snapshot was not consumed");Unknown(frame.ObjectId,"Failure precedes late object-ID pointer capture");
  });
  Test("Unknown tree fields stop at actual consumers in both key branches",delegate{
   var initial=new Fixture();var i=new Invocation();Missing(0x3eae47,delegate{Get(initial,i);},"Entry TEST consumes9C before unknown header");
   foreach(bool fallback in new bool[]{true,false}){
    var f=new Fixture();Select(f,fallback,7);uint parent=fallback?0x3eae5du:0x3eae9eu,nil=fallback?0x3eae61u:0x3eaea2u,compare=fallback?0x3eae68u:0x3eaea8u,childNil=fallback?0x3eae7au:0x3eaebau;
    Missing(parent,delegate{Get(f,i);},"Unknown header consumed at actual parent load");
    var head=new PlayerHotSlotTreeNode();f.Hot.Header88.Write(head);Missing(nil,delegate{Get(f,i);},"Unknown parent pointer consumed at first nil dereference");
    var root=new PlayerHotSlotTreeNode();head.Parent08.Write(root);Missing(nil,delegate{Get(f,i);},"Unknown root nil flag precedes key read");
    root.Nil19.Write(0);Missing(compare,delegate{Get(f,i);},"Unknown node key consumed by comparison");root.Key20.Write(7);
    Missing(childNil,delegate{Get(f,i);},"Unknown left link captured at MOV and consumed at child nil read");
    var child=new PlayerHotSlotTreeNode();root.Left00.Write(child);Missing(childNil,delegate{Get(f,i);},"Unknown child nil flag consumed before value");
    child.Nil19.Write(1);Missing(0x3eaed3,delegate{Get(f,i);},"Exact candidate unknown payload consumed at actual value load");
    root.Value28.Write(null);Same(null,Get(f,i),"Known null payload returns normally after same prefix");Eq(0x3eaed7u,i.At,"Known null exact payload reaches real return");
    root.Key20.Write(6);Missing(childNil,delegate{Get(f,i);},"Unknown right link is also consumed at child nil read");
   }
   var fallbackUnknown=new Fixture();fallbackUnknown.Hot.Key9C.Write(0);var tree=new Tree(7,new MouseSkillSet());fallbackUnknown.Hot.Header88.Write(tree.Head);
   Missing(0x3eae68,delegate{Get(fallbackUnknown,i);},"Nonempty fallback tree first consumes captured Unknown98 in comparison");
  });
  Test("Released explicit nodes and cycles stop without manufacturing a tree result",delegate{
   var i=new Invocation();
   foreach(bool fallback in new bool[]{true,false}){
    uint parent=fallback?0x3eae5du:0x3eae9eu,nil=fallback?0x3eae61u:0x3eaea2u,compare=fallback?0x3eae68u:0x3eaea8u,childNil=fallback?0x3eae7au:0x3eaebau;
    var f=new Fixture();Select(f,fallback,7);var tree=new Tree(7,new MouseSkillSet());f.Hot.Header88.Write(tree.Head);tree.Head.MarkReleased();Missing(parent,delegate{Get(f,i);},"Released header stops at parent dereference");
    f=new Fixture();Select(f,fallback,7);tree=new Tree(7,new MouseSkillSet());f.Hot.Header88.Write(tree.Head);tree.Root.MarkReleased();Missing(nil,delegate{Get(f,i);},"Released root stops at first nil dereference");
    f=new Fixture();Select(f,fallback,7);tree=new Tree(7,new MouseSkillSet());f.Hot.Header88.Write(tree.Head);var deadChild=new PlayerHotSlotTreeNode();deadChild.Nil19.Write(1);deadChild.MarkReleased();tree.Root.Left00.Write(deadChild);Missing(childNil,delegate{Get(f,i);},"Released child stops at late nil dereference");
    f=new Fixture();Select(f,fallback,7);tree=new Tree(7,new MouseSkillSet());f.Hot.Header88.Write(tree.Head);tree.Root.Left00.Write(tree.Root);Missing(compare,delegate{Get(f,i);},"Left self-cycle stops at repeated key comparison");
    tree.Root.Key20.Write(6);tree.Root.Right10.Write(tree.Root);Missing(compare,delegate{Get(f,i);},"Right self-cycle also stops at repeated key comparison");
   }
   var owner=new Fixture();owner.Owner.Entity.MarkReleased();Missing(0x3eae40,delegate{Get(owner,i);},"Released canonical owner stops before9C consumption");
  });
 }
}

public static class PlayerHotSlotDisplayedValidationProgram {
 public static int Main(string[] args){
  try{
   if(args==null||args.Length!=0)throw new ArgumentException("Expected no arguments");
   PlayerHotSlotDisplayedTests.Run();
   Console.WriteLine("{\"status\":\"passed-new-hot-slot-displayed-components-only\",\"scenarios\":"+PlayerHotSlotDisplayedTests.Scenarios+",\"assertions\":"+PlayerHotSlotDisplayedTests.Assertions+",\"old_test_suites_rerun\":0,\"native_math_calls\":0,\"system_library_loaded\":false,\"full_tick\":false,\"physical_mouse\":false,\"original_read\":false,\"original_executed\":false,\"backend_started\":false}");
   return 0;
  }catch(Exception error){Console.Error.WriteLine(error);return 1;}
 }
}
