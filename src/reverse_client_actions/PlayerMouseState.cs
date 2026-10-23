using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GrimDynasty.ReverseClient.Actions {
 // New views borrow the existing Controller/MoveCharacter identities. These
 // cells were not present on the frozen action-submission views; no action,
 // position, current-action or pending-action state is copied into this layer.
 public sealed class MouseControllerState {
  public readonly Controller Identity;
  public readonly Field<PlayerMouseState> State2A0=new Field<PlayerMouseState>();
  public readonly Field<MouseStateList> Overlay2E8=new Field<MouseStateList>();
  public readonly Field<ulong> Count2F0=new Field<ulong>();
  public readonly Field<byte> Byte430=new Field<byte>(),Byte431=new Field<byte>(),Byte432=new Field<byte>(),Byte438=new Field<byte>();
  public readonly Field<uint> Dword43C=new Field<uint>();
  public MouseControllerState(Controller identity){if(identity==null)throw new ArgumentNullException("identity");Identity=identity;}
 }
 public sealed class MouseStateList { public readonly Field<MouseStateNode> First00=new Field<MouseStateNode>(); }
 public sealed class MouseStateNode { public readonly Field<PlayerMouseState> State10=new Field<PlayerMouseState>(); }
 public sealed class PlayerMouseState {
  public readonly object Identity;
  public readonly Field<PlayerMouseTable> Virtual00=new Field<PlayerMouseTable>();
  public readonly Field<Controller> Owner08=new Field<Controller>();
  public readonly Field<MoveCharacter> CachedRole10=new Field<MoveCharacter>();
  public PlayerMouseState(object identity){if(identity==null)throw new ArgumentNullException("identity");Identity=identity;}
 }
 public enum ObservedPlayerMouseTable { Startup,Idle,MoveTo }
 public sealed class PlayerMouseTable {
  public readonly Field<uint> Primary1B0=new Field<uint>(),Secondary1B8=new Field<uint>(),Super1C8=new Field<uint>();
  public readonly Field<uint> Item1E8=new Field<uint>(),FixedActor1F0=new Field<uint>(),Npc1F8=new Field<uint>();
  public readonly Field<uint> Skill260=new Field<uint>(),Fallback278=new Field<uint>(),Move280=new Field<uint>();
  // Fixed disk binding, not camera/state construction or runtime activation.
  public void BindObserved(ObservedPlayerMouseTable kind){
   if(kind!=ObservedPlayerMouseTable.Startup&&kind!=ObservedPlayerMouseTable.Idle&&kind!=ObservedPlayerMouseTable.MoveTo)throw new ArgumentOutOfRangeException("kind");
   Primary1B0.Write(0x161d00);Secondary1B8.Write(0x162290);Super1C8.Write(0x1624b0);
   Skill260.Write(kind==ObservedPlayerMouseTable.Startup?0x01a700u:0x165cd0u);Fallback278.Write(0x01a700);
   Move280.Write(kind==ObservedPlayerMouseTable.Startup?0x015880u:kind==ObservedPlayerMouseTable.Idle?0x165cb0u:0x1662d0u);
  }
 }
 // Captured vptr identity. The unknown concrete virtual bodies are boundaries,
 // not delegates installed as successful production implementations.
 public sealed class MouseObjectTable {}
 public class MouseObjectView {
  public readonly object Identity;
  public readonly Field<object> Virtual00;
  public readonly Field<int> Option10=new Field<int>(),SkillB0=new Field<int>();
  public readonly Field<uint> Kind888=new Field<uint>();
  public MouseObjectView(object identity):this(identity,new Field<object>()){}
  // Composition may borrow the existing EntityMovementState.Virtual00 cell.
  // Capturing its object value never creates a second synchronized vptr mirror.
  public MouseObjectView(object identity,Field<object> virtual00){if(identity==null||virtual00==null)throw new ArgumentNullException("mouse identity/vptr");Identity=identity;Virtual00=virtual00;}
 }
 public sealed class MouseRoleState:MouseObjectView {
  public readonly MoveCharacter Role;
  public readonly Field<byte> Byte242A=new Field<byte>(),Byte4A70=new Field<byte>(),Byte4A61=new Field<byte>();
  public readonly Field<MouseObjectTable> SkillManagerVirtual850=new Field<MouseObjectTable>();
  public readonly Field<MoveCharacter> HotSlotOwner4A48=new Field<MoveCharacter>();
  public MouseRoleState(MoveCharacter identity):base(identity){Role=identity;}
  public MouseRoleState(MoveCharacter identity,Field<object> virtual00):base(identity,virtual00){Role=identity;}
 }
 public sealed class MouseSkillSet {
  public readonly Field<object> Queued00=new Field<object>(),Alternate58=new Field<object>(),Alternate60=new Field<object>();
 }
 // Typed call-frame views preserve late stack-argument reads. Register bools
 // arrive as explicit bytes; no physical-button or modifier meaning is assigned.
 public sealed class MouseInputFrame {
  public readonly Field<byte> B4=new Field<byte>();
  public readonly Field<WorldPosition24> Position=new Field<WorldPosition24>();
  public readonly Field<Field<uint>> ObjectId=new Field<Field<uint>>();
  public readonly Field<Field<byte>> OptionalByte=new Field<Field<byte>>();
 }
 public sealed class MouseSelectorFrame {
  public readonly Field<WorldPosition24> Position=new Field<WorldPosition24>();
  public readonly Field<Field<uint>> ObjectId=new Field<Field<uint>>();
  public readonly Field<byte> TailByte=new Field<byte>();
 }
 public sealed class PlayerMouseContext {
  sealed class ReferenceComparer:IEqualityComparer<object> {
   public new bool Equals(object a,object b){return System.Object.ReferenceEquals(a,b);}
   public int GetHashCode(object value){return RuntimeHelpers.GetHashCode(value);}
  }
  public readonly MoveBoundaries Movement;
  public readonly Field<object> GameEngineA66288=new Field<object>();
  public readonly AddressSymbol<ClassRecord> ItemClass=new AddressSymbol<ClassRecord>(0xa5c8a0),NpcClass=new AddressSymbol<ClassRecord>(0xa5bfa0),FixedActorClass=new AddressSymbol<ClassRecord>(0xa5bee0),DestructibleClass=new AddressSymbol<ClassRecord>(0xa5c0f0),DecorationClass=new AddressSymbol<ClassRecord>(0xa5c240);
  readonly Dictionary<Controller,MouseControllerState> controllers=new Dictionary<Controller,MouseControllerState>();
  readonly Dictionary<object,MouseObjectView> objects=new Dictionary<object,MouseObjectView>(new ReferenceComparer());
  public PlayerMouseContext(MoveBoundaries movement){if(movement==null)throw new ArgumentNullException("movement");Movement=movement;}
  public void Attach(MouseControllerState value){if(value==null)throw new ArgumentNullException("value");MouseControllerState old;if(controllers.TryGetValue(value.Identity,out old)&&!System.Object.ReferenceEquals(old,value))throw new InvalidOperationException("Controller already has a different mouse view");controllers[value.Identity]=value;}
  public void Attach(MouseObjectView value){if(value==null)throw new ArgumentNullException("value");MouseObjectView old;if(objects.TryGetValue(value.Identity,out old)&&!System.Object.ReferenceEquals(old,value))throw new InvalidOperationException("Object already has a different mouse view");objects[value.Identity]=value;}
  public MouseControllerState Controller(Controller identity,uint at){MouseControllerState value;if(identity==null||!controllers.TryGetValue(identity,out value))throw new MissingContract(at,"Controller has no canonical mouse view");return value;}
  public MouseObjectView Object(object identity,uint at){MouseObjectView value;if(identity==null||!objects.TryGetValue(identity,out value))throw new MissingContract(at,"Object has no canonical mouse view");return value;}
  public MouseRoleState Role(MoveCharacter identity,uint at){var value=Object(identity,at) as MouseRoleState;if(value==null)throw new MissingContract(at,"Role has no canonical mouse view");return value;}
 }
 public class PlayerMouseBoundaries {
  public readonly PlayerMouseContext Context;
  public PlayerMouseBoundaries(PlayerMouseContext context){if(context==null)throw new ArgumentNullException("context");Context=context;}
  protected static MissingContract Missing(uint at,string name){return new MissingContract(at,name);}
  public virtual MoveCharacter ResolvePlayer128530(Controller controller){throw Missing(0x128530,"Player lookup/class validation for this Controller");}
  public virtual byte CanAct460(object table,MoveCharacter role){throw Missing(0x15e230,"Concrete role virtual+460");}
  public virtual byte IsMoving05BC20(MoveCharacter role){return Context.Movement.IsMoving(role);}
  public virtual byte CanInterrupt05BCE0(MoveCharacter role){throw Missing(0x05bce0,"CanInterruptMovement");}
  public virtual void StopMoving05FC90(MoveCharacter role,byte a,byte b){Context.Movement.StopMoving(role,a,b);}
  public virtual object ObjectManager609098(){return Context.Movement.ObjectManager();}
  public virtual object Entity0481A0(object manager,uint id){throw Missing(0x0481a0,"Entity lookup/class validation");}
  public virtual object Skill078240(object manager,uint id){throw Missing(0x078240,"Skill lookup/class validation");}
  public virtual ClassRecord Class0(object table,object instance){throw Missing(0,"Concrete object class virtual+0");}
  public virtual byte Derived591480(ClassRecord parent,ClassRecord expected){return Context.Movement.Derived(parent,expected);}
  public virtual object GetOptions609AD8(WorldEngine engine){throw Missing(0x609ad8,"Actual engine Options getter");}
  public virtual int OptionInt60A148(object options,uint index){throw Missing(0x60a148,"Actual Options.GetInt");}
  public virtual byte Attackable2EC880(object engine,object entity){throw Missing(0x2ec880,"IsAttackableClass");}
  public virtual object PrimarySlot15FFF0(Controller controller){throw Missing(0x15fff0,"GetPrimarySlotOption");}
  public virtual object SecondarySlot160070(Controller controller){throw Missing(0x160070,"GetSecondarySlotOption");}
  public virtual uint OptionIdA0(object table,object option){throw Missing(0,"Concrete hot-slot option virtual+A0");}
  public virtual uint DefaultSkillE0(object table,MoveCharacter role,uint index){throw Missing(0,"Concrete role+850 virtual+E0");}
  public virtual float SkillValue240(object table,object skill){throw Missing(0,"Concrete skill virtual+240");}
  public virtual byte SkillCheck4B8(object table,object skill,MoveCharacter role,uint objectId,float value){throw Missing(0,"Concrete skill virtual+4B8");}
  public virtual uint ObjectId609128(MoveCharacter role){throw Missing(0x609128,"Actual Object::GetObjectId");}
  public virtual WorldPosition24 DecorationPosition3F0(object table,object decoration,WorldPosition24 hidden,uint roleId,uint zero){throw Missing(0,"Concrete decoration virtual+3F0");}
  public virtual byte UseDefault4910C0(object skill){throw Missing(0x4910c0,"Skill::UseDefaultWhenUnavailable");}
  public virtual int SecondaryStatus3ECF00(MoveCharacter role){throw Missing(0x3ecf00,"PlayerHotSlotCtrl::GetSecondaryStatus");}
  public virtual void ActivateSecondary3ECDB0(MoveCharacter role){throw Missing(0x3ecdb0,"PlayerHotSlotCtrl::ActivateSecondarySlot");}
  public virtual MouseSkillSet DisplayedSkillSet3EAE40(MoveCharacter role){throw Missing(0x3eae40,"PlayerHotSlotCtrl::GetDisplayedSkillSet");}
  public virtual byte AlternateConfig3E2570(MoveCharacter role){throw Missing(0x3e2570,"Player::GetAlternateConfig");}
  public virtual void Interaction(PlayerMouseTable table,uint slot,PlayerMouseState state,byte a,Field<byte> b,Field<WorldPosition24> position,object entity,uint at){
   uint target=(slot==0x1e8?table.Item1E8:slot==0x1f0?table.FixedActor1F0:table.Npc1F8).Read(at);throw Missing(target,"Concrete player interaction slot");
  }
  // Register/stack pointer copies stay opaque until a concrete callee actually
  // uses them. In particular the observed01A700 XOR AL,AL;RET leaf ignores both.
  // Each call receives fresh wrapper snapshots: a callee may mutate the pointed
  // cells, but cannot rewrite the caller's captured pointer/value registers.
  public virtual byte Skill260(PlayerMouseTable table,PlayerMouseState state,byte a,Field<byte> b,uint skillId,Field<WorldPosition24> position,Field<Field<uint>> objectId,uint at){uint target=table.Skill260.Read(at);if(target==0x01a700)return 0;throw Missing(target,"Concrete selected-state skill dispatch");}
  public virtual byte Fallback278(PlayerMouseTable table,PlayerMouseState state,uint at){uint target=table.Fallback278.Read(at);if(target==0x01a700)return 0;throw Missing(target,"Concrete selected-state fallback predicate");}
  public virtual void Move280(PlayerMouseTable table,PlayerMouseState state,byte a,Field<byte> b,Field<WorldPosition24> position,uint at){uint target=table.Move280.Read(at);throw Missing(target,"Player movement request; no fabricated ActionRecord or successful navigation");}
 }
}
