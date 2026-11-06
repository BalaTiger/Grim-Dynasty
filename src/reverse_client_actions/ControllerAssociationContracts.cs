using System;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // Sole +38 cell for the already canonical mouse-controller view. This is
 // separate from Controller+30 and Character+16C0; creating the OWN field
 // supplies no native initial value, allocation or runtime vptr evidence.
 public sealed class ControllerAssociatedTargetState {
  static readonly ConditionalWeakTable<MouseControllerState,ControllerAssociatedTargetState> states=new ConditionalWeakTable<MouseControllerState,ControllerAssociatedTargetState>();
  public readonly MouseControllerState Mouse;
  public readonly Field<uint> Target38=new Field<uint>();
  ControllerAssociatedTargetState(MouseControllerState mouse){Mouse=mouse;}
  public static ControllerAssociatedTargetState ForController(PlayerMouseContext context,Controller controller,uint at){
   if(context==null)throw new MissingContract(at,"Missing shared mouse context");var mouse=context.Controller(controller,at);
   return states.GetValue(mouse,delegate(MouseControllerState value){return new ControllerAssociatedTargetState(value);});
  }
 }
 // Concrete adapter for the already shared ControllerPlayer identity only.
 // The expected symbol is borrowed from the caller's A5C9F0 declaration;
 // AddressSymbol exposes no address getter. Construction initializes no parent.
 public sealed class ControllerPlayerAssociatedLookupBoundaries:GameAssociatedLookupBoundaries {
  public readonly ControllerLookupContext ControllerContext;
  public readonly Field<PlayerMouseState> Last80State=new Field<PlayerMouseState>();
  public readonly Field<PlayerMouseTable> Last80Primary=new Field<PlayerMouseTable>();
  // Explicit OWN publication of the SetState composition used by the MoveTo
  // +0x70 body. Nothing binds it implicitly and no dependency is defaulted.
  public ControllerStateTransitionContext StateTransition{get;private set;}
  public readonly Field<PlayerMoveShortName> LastEndOfPathName=new Field<PlayerMoveShortName>();
  public void BindStateTransition(ControllerStateTransitionContext value){
   if(value==null)throw new ArgumentNullException("state transition");
   if(!Object.ReferenceEquals(value.Lookup.Mouse,ControllerContext.Mouse))throw new ArgumentException("State transition must use the original mouse context");
   if(StateTransition!=null&&!Object.ReferenceEquals(StateTransition,value))throw new InvalidOperationException("State transition composition cannot be replaced");
   StateTransition=value;
  }
  public ControllerPlayerAssociatedLookupBoundaries(ControllerLookupContext context,AddressSymbol<ClassRecord> expectedA5C9F0):base(expectedA5C9F0,context==null?null:context.Mouse){
   if(context==null)throw new ArgumentNullException("context");ControllerContext=context;
  }
  // Explicit OWN initialization from the saved disk layout, not execution of
  // a native writer or evidence of the current loaded class graph. Lookup
  // never calls this method. Preserve the terminal base's unknown parent.
  public void InitializeDiskParentChain(ClassRecord recordA5BF10,ClassRecord recordA5C990,ClassRecord recordA5C9C0){
   var first=ControllerContext.ControllerPlayerClassInfoA5C180.Read(0);
   var last=ControllerBaseCharacterClass.Read(0);
   var nodes=new ClassRecord[]{first,recordA5BF10,recordA5C990,recordA5C9C0,last};
   for(int n=0;n<nodes.Length;n++){
    if(nodes[n]==null)throw new ArgumentException("Disk parent chain requires five non-null identities");
    for(int p=0;p<n;p++)if(Object.ReferenceEquals(nodes[n],nodes[p]))throw new ArgumentException("Disk parent chain identities must be distinct");
   }
   for(int n=0;n<4;n++)nodes[n].Parent10.Write(nodes[n+1]);
  }
  public override Field<object> CapturePrimary(object instance,Invocation i){
   var controller=instance as Controller;if(controller==null)throw new MissingContract(i.At,"Associated object lacks an existing Controller identity");
   var state=ControllerContext.Mouse.Controller(controller,i.At);var view=ControllerContext.Mouse.Object(controller,i.At);
   if(!Object.ReferenceEquals(state.Identity,controller)||!Object.ReferenceEquals(view.Identity,controller))throw new MissingContract(i.At,"Associated Controller and primary view differ");
   return view.Virtual00.Snapshot();
  }
  public override Field<ClassRecord> Class0(object instance,object capturedPrimary,Invocation i){
   // Reuse the fixed A5C180 LEA result, not ExpectedImport2D7198 or A5C9F0.
   // Unknown fixed-symbol knowledge stays opaque until the077D82 comparison.
   return ControllerLookupContracts.Class0(instance,capturedPrimary,ControllerContext,i);
  }
  Controller RequireSlotController(object instance,object capturedPrimary,Invocation i){
   if(!Object.ReferenceEquals(capturedPrimary,ControllerLookupPrimaryTable.ObservedControllerPlayer))throw new MissingContract(i.At,"Associated controller slot requires the explicitly bound observed primary table");
   var controller=instance as Controller;if(controller==null)throw new MissingContract(i.At,"Associated slot lacks the canonical Controller identity");return controller;
  }
  public override void Slot60(object instance,object capturedPrimary,Field<uint> flags,Invocation i){
   var controller=RequireSlotController(instance,capturedPrimary,i);i.At=0x127b70;
   if(flags==null)throw new MissingContract(i.At,"Missing captured EDX field");
   // Actual127B70 MOV DWORD[RCX+38],EDX; unknown bits are copied, not tested.
   ControllerAssociatedTargetState.ForController(ControllerContext.Mouse,controller,i.At).Target38.CopyFrom(flags);i.At=0x127b73;
  }
  public override void Slot80(object instance,object capturedPrimary,Invocation i){
   Last80State.Forget();Last80Primary.Forget();var controller=RequireSlotController(instance,capturedPrimary,i);i.At=0x123810;
   var mouse=ControllerContext.Mouse.Controller(controller,i.At);uint primaryAt,jumpAt;
   if(mouse.Count2F0.Read(i.At)==0){i.At=0x12382f;Last80State.CopyFrom(mouse.State2A0);primaryAt=0x123836;jumpAt=0x123839;}
   else{
    i.At=0x12381a;var header=mouse.Overlay2E8.Snapshot();i.At=0x123821;var list=header.Read(i.At);if(list==null)throw new MissingContract(i.At,"Null overlay header dereference");
    var first=list.First00.Snapshot();i.At=0x123824;var node=first.Read(i.At);if(node==null)throw new MissingContract(i.At,"Null overlay node dereference");
    Last80State.CopyFrom(node.State10);primaryAt=0x123828;jumpAt=0x12382b;
   }
   i.At=primaryAt;var selected=Last80State.Read(i.At);if(selected==null)throw new MissingContract(i.At,"Null selected state dereference");Last80Primary.CopyFrom(selected.Virtual00);
   i.At=jumpAt;Last80Primary.Read(i.At);
   // The +70 target is accepted ONLY when the state explicitly publishes the
   // evidenced Prestart table whose +70 entry is the shared `ret 0` stub.
   // Nothing is inferred from a state name, a PlayerMouseTable field value or
   // a matching 0x015880 in another slot.
   ControllerAIStateTable known=null;try{known=ControllerAIStateTableBinding.Read(selected,i.At);}catch(MissingContract){}
   if(Object.ReferenceEquals(known,ControllerAIStateTable.Prestart)){
    if(ControllerAIStateTable.Prestart.Slot70Rva!=0x015880u)throw new MissingContract(i.At,"Evidenced Prestart +70 target changed");
    // Tail jump into 0x015880: the stub body is `ret 0`, so the +70 call ends
    // and EndOfPathReached returns to its caller with no state change.
    i.At=ControllerAIStateTable.Prestart.Slot70Rva;return;
   }
   if(Object.ReferenceEquals(known,ControllerAIStateTable.MoveTo)){
    // Decoded 0x1663F0: build the literal "Idle", build a state payload whose
    // first 12 bytes are explicitly zeroed, then call SetState123D50 with the
    // owning controller. The WorldVec3 built through the 0x180609720 slot is
    // NOT defaulted: its Position stays unknown, so SetState stops there.
    if(ControllerAIStateTable.MoveTo.Slot70Rva!=0x1663f0u)throw new MissingContract(i.At,"Evidenced MoveTo +70 target changed");
    if(StateTransition==null)throw new MissingContract(0x1663f0,"MoveTo EndOfPathReached SetState composition is unbound");
    var name=new PlayerMoveShortName();
    name.Inline00[0].Write(0x49);name.Inline00[1].Write(0x64);name.Inline00[2].Write(0x6c);name.Inline00[3].Write(0x65);
    name.Length10.Write(4UL);name.Capacity18.Write(15UL);
    LastEndOfPathName.Write(name);
    var payload=new PlayerMoveTransitionPayload();
    payload.Zero00.Write(0UL);payload.Zero08.Write(0u);payload.Padding0C.Write(0u);
    var transition=new ControllerStateTransitionInvocation();i.At=0x166450;
    ControllerStateTransitionContracts.SetState123D50(controller,name,payload,StateTransition,transition);
    i.At=0x16647f;return;
   }
   // No +70 target/behavior is inferred from the selected state's name.
   throw new MissingContract(i.At,"Selected state virtual+70 remains unbound");
  }
 }
 // Explicit OWN publication of a separately evidenced state vtable identity.
 // The vtable address and its +70 target are disk reads; binding this value
 // never installs a vptr, constructs a state, or infers a class from a name.
 // This sidecar is used because PlayerMouseState belongs to a prebuilt
 // dependency that this focused recipe does not recompile.
 public sealed class ControllerAIStateTable {
  public readonly uint VtableRva;
  public readonly uint Slot70Rva;
  // Disk 0x6E4110 (vtable 0x6E40A0 + 0x70) -> 0x180015880: bytes C2 00 00 are
  // `ret 0`, and the saved pseudo body at that exact entry is `return;`.
  public static readonly ControllerAIStateTable Prestart=new ControllerAIStateTable(0x6e40a0,0x015880);
  // RegisterStates0x15E440 stores 0x180744930 for "MoveTo"; +0x70 = 0x1663F0,
  // decoded on disk as SetState("Idle", zeroed data). See
  // docs/reverse-client/r1-player-state-vtables and r1-moveto-endofpath.
  public static readonly ControllerAIStateTable MoveTo=new ControllerAIStateTable(0x744930,0x1663f0);
  ControllerAIStateTable(uint vtableRva,uint slot70Rva){VtableRva=vtableRva;Slot70Rva=slot70Rva;}
 }
 public sealed class ControllerAIStateTableBinding {
  static readonly ConditionalWeakTable<PlayerMouseState,ControllerAIStateTableBinding> bindings=new ConditionalWeakTable<PlayerMouseState,ControllerAIStateTableBinding>();
  public readonly PlayerMouseState State;
  public readonly ControllerAIStateTable Table;
  ControllerAIStateTableBinding(PlayerMouseState state,ControllerAIStateTable table){State=state;Table=table;}
  public static ControllerAIStateTableBinding Bind(PlayerMouseState state,ControllerAIStateTable table){
   if(state==null||table==null)throw new ArgumentNullException("state table binding");
   lock(bindings){ControllerAIStateTableBinding prior;if(bindings.TryGetValue(state,out prior)){
    if(!Object.ReferenceEquals(prior.Table,table))throw new InvalidOperationException("State already publishes another evidenced table");return prior;}
    var created=new ControllerAIStateTableBinding(state,table);bindings.Add(state,created);return created;}
  }
  public static ControllerAIStateTable Read(PlayerMouseState state,uint at){
   ControllerAIStateTableBinding binding;if(state==null||!bindings.TryGetValue(state,out binding))throw new MissingContract(at,"Selected state has no evidenced table binding");return binding.Table;
  }
 }
 // One adjacent242B cell per existing MouseRoleState.242A remains the original
 // cell consumed by PlayerMouseContracts. Binding supplies no initial values.
 public sealed class CharacterMouseConstructor242AState {
  static readonly ConditionalWeakTable<MouseRoleState,CharacterMouseConstructor242AState> states=new ConditionalWeakTable<MouseRoleState,CharacterMouseConstructor242AState>();
  public readonly PlayerMousePlayerLookupContext Lookup;
  public readonly MouseRoleState Role;
  public readonly GameMovementCharacterState Owner;
  public readonly Field<byte> Byte242A;
  public readonly Field<byte> Byte242B=new Field<byte>();
  CharacterMouseConstructor242AState(PlayerMousePlayerLookupContext lookup,MouseRoleState role,GameMovementCharacterState owner){Lookup=lookup;Role=role;Owner=owner;Byte242A=role.Byte242A;}
  static GameMovementCharacterState Validate(PlayerMousePlayerLookupContext lookup,MouseRoleState role,uint at){
   var view=lookup.Bridge.ForMoveCharacter(role.Role,at);view.Owner.Entity.RequireLive(at);
   if(!Object.ReferenceEquals(lookup.Mouse.Role(role.Role,at),role)||!Object.ReferenceEquals(role.Virtual00,view.Owner.Entity.Virtual00))throw new MissingContract(at,"Constructor store requires the original mouse role and canonical Entity vptr cell");
   return view.Owner;
  }
  public static CharacterMouseConstructor242AState Bind(PlayerMousePlayerLookupContext lookup,MouseRoleState role){
   if(lookup==null||role==null)throw new ArgumentNullException("existing lookup/role");var owner=Validate(lookup,role,0);
   lock(states){CharacterMouseConstructor242AState prior;if(states.TryGetValue(role,out prior)){
    if(!Object.ReferenceEquals(prior.Lookup,lookup)||!Object.ReferenceEquals(prior.Owner,owner))throw new InvalidOperationException("Mouse role already has another constructor-state binding");prior.RequireLive(0);return prior;
   }var state=new CharacterMouseConstructor242AState(lookup,role,owner);states.Add(role,state);return state;}
  }
  public void RequireLive(uint at){
   CharacterMouseConstructor242AState current;
   if(!states.TryGetValue(Role,out current)||!Object.ReferenceEquals(current,this)||!Object.ReferenceEquals(Validate(Lookup,Role,at),Owner)||!Object.ReferenceEquals(Byte242A,Role.Byte242A))throw new MissingContract(at,"Constructor byte view is not the original shared identity");
  }
 }
 public static class CharacterMouseConstructorContracts {
  // Game052674: MOV word ptr [R15+242A],0. Conditional entry at this store
  // only; no constructor prefix, later calls or active-frame value is inferred.
  // Single-threaded OWN field model: no callback or native atomicity claim
  // between the two byte writes.05267E is the next instruction, not a return.
  public static void ApplyReachedWordZero052674(CharacterMouseConstructor242AState state,Invocation invocation){
   if(invocation==null)throw new MissingContract(0x052674,"Missing constructor-store invocation");invocation.At=0x052674;
   if(state==null)throw new MissingContract(invocation.At,"Missing explicit constructor byte binding");state.RequireLive(invocation.At);
   state.Byte242A.Write(0);state.Byte242B.Write(0);invocation.At=0x05267e;
  }
 }
 public static class ControllerMouseConstructorContracts {
  // Conditional entry at Game122A1A: RBX=this+2E8 and RSI=0, so the
  // qword store writes the original Count2F0. No earlier constructor call,
  // State2A0/overlay publication or active state is inferred.122A1E is next.
  public static void ApplyReachedCountZero122A1A(PlayerMouseContext context,MouseControllerState state,Invocation invocation){
   if(invocation==null)throw new MissingContract(0x122a1a,"Missing controller count-store invocation");invocation.At=0x122a1a;
   if(context==null||state==null)throw new MissingContract(invocation.At,"Missing canonical mouse controller state");
   if(!Object.ReferenceEquals(context.Controller(state.Identity,invocation.At),state))throw new MissingContract(invocation.At,"Controller count store requires the original registered mouse view");
   state.Count2F0.Write(0UL);invocation.At=0x122a1e;
  }
 }
}
