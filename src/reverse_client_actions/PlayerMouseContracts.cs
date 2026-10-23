using System;

namespace GrimDynasty.ReverseClient.Actions {
 // Game15E1F0 and161D00 from the retained exact assembly. Normal supported
 // pointer domain only: typed cell aliases and callback-visible mutation are
 // retained; native invalid addresses, EH and arbitrary byte overlap are not.
 public static partial class PlayerMouseContracts {
  static T Need<T>(T value,uint at)where T:class{if(value==null)throw new MissingContract(at,"Null own mouse view");return value;}
  static uint At(Invocation i,uint at){i.At=at;return at;}
  static T Call<T>(Invocation i,Func<T> call){try{return call();}catch(MissingContract e){if(e.Address!=0)throw;throw new MissingContract(i.At,e.Message,e);}}
  static Field<object> ObjTable(object value,PlayerMouseBoundaries b,uint at){return b.Context.Object(value,at).Virtual00.Snapshot();}
  static Field<PlayerMouseTable> StateTable(PlayerMouseState state,uint at){return Need(state,at).Virtual00.Snapshot();}
  static void EnsureRole(PlayerMouseState state,PlayerMouseBoundaries b,Invocation i,uint checkAt,uint ownerAt,uint callAt,uint writeAt){
   if(Need(state,At(i,checkAt)).CachedRole10.Read(i.At)==null){At(i,ownerAt);var owner=state.Owner08.Snapshot();At(i,callAt);var role=b.ResolvePlayer128530(owner.Read(i.At));At(i,writeAt);state.CachedRole10.Write(role);}
  }
  static MoveCharacter RefreshRole(PlayerMouseState state,PlayerMouseBoundaries b,Invocation i,uint checkAt,uint ownerAt,uint callAt,uint writeAt,uint readAt){
   EnsureRole(state,b,i,checkAt,ownerAt,callAt,writeAt);
   return state.CachedRole10.Read(At(i,readAt));
  }
  static Field<PlayerMouseState> Selected(MouseControllerState c,Invocation i,uint countAt,uint listAt,uint nodeAt,uint stateAt,uint directAt){
   if(c.Count2F0.Read(At(i,countAt))==0){At(i,directAt);return c.State2A0.Snapshot();}
   At(i,listAt);var list=c.Overlay2E8.Snapshot();At(i,nodeAt);var node=Need(list.Read(i.At),i.At).First00.Snapshot();At(i,stateAt);return Need(node.Read(i.At),i.At).State10.Snapshot();
  }
  static MouseSelectorFrame Selector(WorldPosition24 position,Field<Field<uint>> id){var frame=new MouseSelectorFrame();frame.Position.Write(position);frame.ObjectId.CopyFrom(id);return frame;}
  static byte InvokeSuper(PlayerMouseTable table,PlayerMouseState state,byte a,Field<byte> b,MouseSelectorFrame frame,PlayerMouseBoundaries boundaries,Invocation i){uint target=Need(table,i.At).Super1C8.Read(i.At);if(target!=0x1624b0)throw new MissingContract(target,"Unimplemented selected mouse interception slot");return ActivateSuperCaptured(state,a,b,frame,boundaries,i);}
  static byte InvokePrimary(PlayerMouseTable table,PlayerMouseState state,byte first,byte a,Field<byte> b,MouseSelectorFrame frame,PlayerMouseBoundaries boundaries,Invocation i){uint target=Need(table,i.At).Primary1B0.Read(i.At);if(target!=0x161d00)throw new MissingContract(target,"Unimplemented selected primary slot");return SelectPrimaryCaptured(state,first,a,b,frame,boundaries,i);}
  static byte InvokeSecondary(PlayerMouseTable table,PlayerMouseState state,byte a,Field<byte> b,MouseSelectorFrame frame,PlayerMouseBoundaries boundaries,Invocation i){uint target=Need(table,i.At).Secondary1B8.Read(i.At);if(target!=0x162290)throw new MissingContract(target,"Unimplemented selected secondary slot");return SelectSecondaryCaptured(state,a,b,frame,boundaries,i);}
  public static byte HandleActionFromMouse15E1F0(Controller controller,byte b1,byte b2,byte b3,MouseInputFrame frame,PlayerMouseBoundaries boundaries,Invocation invocation){
   var i=Need(invocation,0x15e219);var b=Need(boundaries,0x15e219);At(i,0x15e219);var role=b.ResolvePlayer128530(controller);
   if(role==null)return 0;
   var roleTable=ObjTable(role,b,At(i,0x15e22a));At(i,0x15e230);byte canAct=b.CanAct460(Need(roleTable.Read(i.At),i.At),role);
   bool clear=canAct==0;if(!clear)clear=b.Context.Role(role,At(i,0x15e23e)).Byte242A.Read(i.At)!=0;
   if(clear){
    var id=Need(frame,At(i,0x15e3bc)).ObjectId.Snapshot();
    b.Context.Controller(controller,At(i,0x15e3c6)).Byte438.Write(0);
    b.Context.Controller(controller,At(i,0x15e3cd)).Dword43C.Write(0);
    At(i,0x15e3d3);Need(id.Read(i.At),i.At).Write(0);return 0;
   }
   var source=Need(frame,At(i,0x15e24b)).Position.Snapshot();var local=new WorldPosition24();
   var input=Need(source.Read(At(i,0x15e253)),i.At);var tail0=input.D10.Snapshot();var tail1=input.D14.Snapshot();
   At(i,0x15e258);local.D10.CopyFrom(tail0);local.D14.CopyFrom(tail1);
   At(i,0x15e25e);var token=input.Q0.Snapshot();var x=input.D8.Snapshot();var y=input.DC.Snapshot();
   At(i,0x15e261);local.Q0.CopyFrom(token);local.D8.CopyFrom(x);local.DC.CopyFrom(y);
   if(b3!=0){At(i,0x15e26e);if(b.IsMoving05BC20(role)!=0){At(i,0x15e27a);if(b.CanInterrupt05BCE0(role)!=0){At(i,0x15e28d);b.StopMoving05FC90(role,1,1);}}}
   var c=b.Context.Controller(controller,0x15e292);var stateBits=Selected(c,i,0x15e292,0x15e29c,0x15e2a3,0x15e2a6,0x15e2ac);
   At(i,0x15e2b3);var state=stateBits.Read(i.At);var table=StateTable(state,i.At);var capturedId=frame.ObjectId.Snapshot();var call=Selector(local,capturedId);
   At(i,0x15e2c7);var fourth=frame.B4.Snapshot();At(i,0x15e2d5);byte intercepted=InvokeSuper(table.Read(i.At),state,b3,fourth,call,b,i);
   if(intercepted!=0){var optional=frame.OptionalByte.Snapshot();At(i,0x15e2e7);var pointer=optional.Read(i.At);if(pointer!=null){At(i,0x15e2f0);pointer.Write(0);}return 0;}
   if(c.Byte431.Read(At(i,0x15e2f8))==0&&b1==0)return 0;
   if(b2!=0){
    stateBits=Selected(c,i,0x15e30f,0x15e319,0x15e320,0x15e323,0x15e329);
    At(i,0x15e330);var tail=c.Byte430.Snapshot();At(i,0x15e33b);state=stateBits.Read(i.At);table=StateTable(state,i.At);At(i,0x15e342);fourth=frame.B4.Snapshot();
    call=Selector(local,capturedId);call.TailByte.CopyFrom(tail);At(i,0x15e35e);byte result=InvokePrimary(table.Read(i.At),state,b1,b3,fourth,call,b,i);
    At(i,0x15e365);c.Byte432.Write(result);return result;
   }
   stateBits=Selected(c,i,0x15e36d,0x15e377,0x15e37e,0x15e381,0x15e387);
   At(i,0x15e38e);var last=c.Byte430.Snapshot();At(i,0x15e39a);state=stateBits.Read(i.At);table=StateTable(state,i.At);At(i,0x15e3a1);fourth=frame.B4.Snapshot();
   call=Selector(local,capturedId);call.TailByte.CopyFrom(last);At(i,0x15e3b3);return InvokeSecondary(table.Read(i.At),state,b3,fourth,call,b,i);
  }
  static bool Matches(object entity,AddressSymbol<ClassRecord> symbol,PlayerMouseBoundaries b,Invocation i,uint loadAt,uint callAt,uint compareAt,uint parentAt,uint deriveAt){
   var table=ObjTable(entity,b,At(i,loadAt));At(i,callAt);var actual=Call(i,delegate{return b.Class0(Need(table.Read(i.At),i.At),entity);});
   var expected=symbol.Read(At(i,compareAt));if(Object.ReferenceEquals(actual,expected))return true;
   var parentBits=Need(actual,At(i,parentAt)).Parent10.Snapshot();var parent=parentBits.Read(At(i,parentAt+4));if(parent==null)return false;if(Object.ReferenceEquals(parent,expected))return true;
   At(i,deriveAt);return b.Derived591480(parent,expected)!=0;
  }
  static bool InteractionAllowed(PlayerMouseState state,PlayerMouseBoundaries b,Invocation i,uint ownerAt,uint aAt,uint bAt){
   At(i,ownerAt);var owner=state.Owner08.Snapshot();At(i,aAt);var c=b.Context.Controller(owner.Read(i.At),i.At);return c.Byte432.Read(i.At)==0&&c.Byte430.Read(At(i,bAt))==0;
  }
  static void Interact(PlayerMouseState state,byte a,Field<byte> c,MouseSelectorFrame frame,object entity,uint slot,PlayerMouseBoundaries b,Invocation i,uint tableAt,uint positionAt,uint callAt,uint ownerAt,uint clearAt){
   var table=StateTable(state,At(i,tableAt));var position=Need(frame,At(i,positionAt)).Position.Snapshot();At(i,callAt);b.Interaction(Need(table.Read(i.At),i.At),slot,state,a,c.Snapshot(),position.Snapshot(),entity,i.At);
   At(i,ownerAt);var owner=state.Owner08.Snapshot();At(i,clearAt);b.Context.Controller(owner.Read(i.At),i.At).Byte431.Write(0);
  }
  static int Option13(PlayerMouseBoundaries b,Invocation i,uint globalAt,uint getterAt,uint valueAt){
   At(i,globalAt);var engine=b.Context.Movement.Context.EngineGlobal.Snapshot();At(i,getterAt);var options=b.GetOptions609AD8(engine.Read(i.At));At(i,valueAt);return b.OptionInt60A148(options,13);
  }
  static uint DefaultSkill(PlayerMouseState state,MoveCharacter role,uint index,PlayerMouseBoundaries b,Invocation i,uint tableAt,uint callAt){
   var table=b.Context.Role(role,At(i,tableAt)).SkillManagerVirtual850.Snapshot();At(i,callAt);return Call(i,delegate{return b.DefaultSkillE0(Need(table.Read(i.At),i.At),role,index);});
  }
  public static byte SelectPrimary161D00(PlayerMouseState state,byte first,byte a,byte c,MouseSelectorFrame frame,PlayerMouseBoundaries boundaries,Invocation invocation){
   return SelectPrimaryCaptured(state,first,a,Field<byte>.Of(c),frame,boundaries,invocation);
  }
  static byte SelectPrimaryCaptured(PlayerMouseState state,byte first,byte a,Field<byte> c,MouseSelectorFrame frame,PlayerMouseBoundaries boundaries,Invocation invocation){
   var i=Need(invocation,0x161d27);var b=Need(boundaries,0x161d27);Field<WorldPosition24> position=null;Field<Field<uint>> originalId=null;object entity=null;
   if(first!=0)goto FallbackFresh;
   At(i,0x161d2f);originalId=Need(frame,i.At).ObjectId.Snapshot();At(i,0x161d37);uint id=Need(originalId.Read(i.At),i.At).Read(i.At);
   if(id==0)goto General;
   At(i,0x161d42);var manager=b.ObjectManager609098();At(i,0x161d4d);entity=b.Entity0481A0(manager,id);if(entity==null)goto General;
   if(Matches(entity,b.Context.ItemClass,b,i,0x161d5e,0x161d64,0x161d6d,0x161d72,0x161d80)&&InteractionAllowed(state,b,i,0x161d89,0x161d8d,0x161d96)){
    Interact(state,a,c,frame,entity,0x1e8,b,i,0x161d9f,0x161da6,0x161dba,0x161dc0,0x161dc4);return 0;
   }
   if(Matches(entity,b.Context.NpcClass,b,i,0x161dd2,0x161dd8,0x161de1,0x161de6,0x161df4)&&InteractionAllowed(state,b,i,0x161dfd,0x161e01,0x161e0a)){
    Interact(state,a,c,frame,entity,0x1f8,b,i,0x161e13,0x161e1a,0x161e2e,0x161e34,0x161e38);return 0;
   }
   if(Matches(entity,b.Context.FixedActorClass,b,i,0x161e46,0x161e4c,0x161e55,0x161e5a,0x161e68)&&InteractionAllowed(state,b,i,0x161e71,0x161e75,0x161e7e)){
    Interact(state,a,c,frame,entity,0x1f0,b,i,0x161e87,0x161e8e,0x161ea2,0x161ea8,0x161eac);return 0;
   }
   if(Matches(entity,b.Context.DestructibleClass,b,i,0x161eba,0x161ec0,0x161ec9,0x161ed2,0x161ee7))goto Destructible;
   if(!Matches(entity,b.Context.DecorationClass,b,i,0x161ef4,0x161efa,0x161f03,0x161f08,0x161f1d))goto General;
   if(Option13(b,i,0x161f31,0x161f34,0x161f42)!=0)return 0;
   if(!Matches(entity,b.Context.DecorationClass,b,i,0x161f50,0x161f56,0x161f58,0x161f5d,0x161f6e))entity=null;
   {
    var role=RefreshRole(state,b,i,0x161f7a,0x161f81,0x161f85,0x161f8a,0x161f8e);
    var table=StateTable(state,At(i,0x161f92));var targetTable=ObjTable(entity,b,At(i,0x161f95));At(i,0x161f98);uint roleId=b.ObjectId609128(role);
    At(i,0x161fac);var hidden=new WorldPosition24();var returned=Call(i,delegate{return b.DecorationPosition3F0(Need(targetTable.Read(i.At),i.At),entity,hidden,roleId,0);});
    At(i,0x161fc0);b.Move280(Need(table.Read(i.At),i.At),state,a,c.Snapshot(),Field<WorldPosition24>.Of(returned),i.At);return 1;
   }
  Destructible:
   if(!Matches(entity,b.Context.DestructibleClass,b,i,0x161fcb,0x161fd1,0x161fd3,0x161fd8,0x161fed))goto General;
   if(b.Context.Object(entity,At(i,0x161ffa)).Kind888.Read(i.At)!=0x2f)goto General;
   {
    var role=RefreshRole(state,b,i,0x162008,0x16200f,0x162013,0x162018,0x16201c);uint skillId=DefaultSkill(state,role,2,b,i,0x16202c,0x16202f);
    At(i,0x162038);manager=b.ObjectManager609098();At(i,0x162044);var skill=b.Skill078240(manager,skillId);if(skill==null)goto General;
    EnsureRole(state,b,i,0x162051,0x162058,0x16205c,0x162061);var targetTable=ObjTable(skill,b,At(i,0x162065));role=state.CachedRole10.Read(At(i,0x16206b));
    At(i,0x16206f);float value=Call(i,delegate{return b.SkillValue240(Need(targetTable.Read(i.At),i.At),skill);});
    At(i,0x162075);uint freshId=Need(originalId.Read(i.At),i.At).Read(i.At);At(i,0x162081);byte accepted=Call(i,delegate{return b.SkillCheck4B8(Need(targetTable.Read(i.At),i.At),skill,role,freshId,value);});if(accepted==0)goto General;
    At(i,0x16208b);position=frame.Position.Snapshot();var table=StateTable(state,At(i,0x162096));At(i,0x1620ae);b.Skill260(Need(table.Read(i.At),i.At),state,a,c.Snapshot(),skillId,position.Snapshot(),originalId.Snapshot(),i.At);return 0;
   }
  General:
   {
    var role=RefreshRole(state,b,i,0x1620bc,0x1620c3,0x1620c7,0x1620cc,0x1620d0);if(b.Context.Role(role,At(i,0x1620d4)).Byte4A70.Read(i.At)!=0)goto FallbackFresh;
    var localId=Field<uint>.Of(0);At(i,0x1620ec);manager=b.ObjectManager609098();At(i,0x1620f2);id=Need(originalId.Read(i.At),i.At).Read(i.At);At(i,0x1620f8);var attackEntity=b.Entity0481A0(manager,id);
    if(attackEntity!=null){At(i,0x162102);var engine=b.Context.GameEngineA66288.Snapshot();At(i,0x16210c);if(b.Attackable2EC880(engine.Read(i.At),attackEntity)!=0){At(i,0x162115);localId.Write(Need(originalId.Read(i.At),i.At).Read(i.At));}}
    if(localId.Read(At(i,0x162128))==0&&a==0&&Option13(b,i,0x162138,0x16213b,0x162149)<=0)goto FallbackFresh;
    At(i,0x162157);var owner=state.Owner08.Snapshot();At(i,0x16215b);var option=b.PrimarySlot15FFF0(owner.Read(i.At));if(option==null)goto FallbackFresh;
    var optionTable=ObjTable(option,b,At(i,0x162169));At(i,0x16216f);uint skillId=Call(i,delegate{return b.OptionIdA0(Need(optionTable.Read(i.At),i.At),option);});At(i,0x162175);position=frame.Position.Snapshot();if(skillId==0)goto FallbackCaptured;
    var table=StateTable(state,At(i,0x162185));var localIdPointer=Field<Field<uint>>.Of(localId);At(i,0x1621a8);if(b.Skill260(Need(table.Read(i.At),i.At),state,a,c.Snapshot(),skillId,position.Snapshot(),localIdPointer.Snapshot(),i.At)!=0)return 0;
    role=RefreshRole(state,b,i,0x1621b7,0x1621bd,0x1621c1,0x1621c6,0x1621ca);skillId=DefaultSkill(state,role,0,b,i,0x1621d7,0x1621da);
    table=StateTable(state,At(i,0x1621e0));At(i,0x162203);if(b.Skill260(Need(table.Read(i.At),i.At),state,a,c.Snapshot(),skillId,position.Snapshot(),localIdPointer.Snapshot(),i.At)!=0)return 0;
    table=StateTable(state,At(i,0x162212));At(i,0x162218);if(b.Fallback278(Need(table.Read(i.At),i.At),state,i.At)!=0)return 0;goto FallbackCaptured;
   }
  FallbackFresh:
   At(i,0x162226);position=Need(frame,i.At).Position.Snapshot();
  FallbackCaptured:
   if(Option13(b,i,0x162235,0x162238,0x162246)!=0)return 0;
   var moveTable=StateTable(state,At(i,0x162254));At(i,0x162265);b.Move280(Need(moveTable.Read(i.At),i.At),state,a,c.Snapshot(),position.Snapshot(),i.At);return 1;
  }
 }
}
