using System;

namespace GrimDynasty.ReverseClient.Actions {
 // Game162290/1624B0, retained r1-player-mouse-handlers-retry-1/08 and11.
 // Registers carry uninterpreted input bytes. No physical button, key modifier,
 // press/release or held-state mapping is inferred. Shared pointer MOVs capture
 // opaque knowledge; actual branches/calls consume it. The production boundary
 // methods remain explicit Unsupported wherever their concrete body is unclosed.
 public static partial class PlayerMouseContracts {
  public static byte SelectSecondary162290(PlayerMouseState state,byte a,byte b,MouseSelectorFrame frame,PlayerMouseBoundaries boundaries,Invocation invocation){
   return SelectSecondaryCaptured(state,a,Field<byte>.Of(b),frame,boundaries,invocation);
  }
  static byte SelectSecondaryCaptured(PlayerMouseState state,byte a,Field<byte> b,MouseSelectorFrame frame,PlayerMouseBoundaries boundaries,Invocation invocation){
   var i=Need(invocation,0x16229c);At(i,0x16229c);bool missingRole=Need(state,i.At).CachedRole10.Read(i.At)==null;
   At(i,0x1622a1);var position=Need(frame,i.At).Position.Snapshot();
   if(missingRole){
    At(i,0x1622b1);var owner=state.Owner08.Snapshot();At(i,0x1622b5);var resolved=Need(boundaries,i.At).ResolvePlayer128530(owner.Read(i.At));
    At(i,0x1622ba);state.CachedRole10.Write(resolved);
   }
   At(i,0x1622be);var role=state.CachedRole10.Read(i.At);
   At(i,0x1622c2);if(Need(boundaries,i.At).Context.Role(role,i.At).Byte4A70.Read(i.At)!=0){At(i,0x162471);return 0;}
   At(i,0x1622cf);var slotOwner=state.Owner08.Snapshot();At(i,0x1622e7);object option=boundaries.SecondarySlot160070(slotOwner.Read(i.At));
   At(i,0x1622ef);if(option==null){At(i,0x162471);return 0;}
   At(i,0x1622f8);int optionKind=boundaries.Context.Object(option,i.At).Option10.Read(i.At);
   if(optionKind!=0){
    role=RefreshRole(state,boundaries,i,0x162410,0x162417,0x16241b,0x162420,0x162424);
    At(i,0x16242f);int status=boundaries.SecondaryStatus3ECF00(role);
    At(i,0x162434);if(status==1){
     role=RefreshRole(state,boundaries,i,0x162439,0x162440,0x162444,0x162449,0x16244d);
     At(i,0x162458);boundaries.ActivateSecondary3ECDB0(role);
    }
    At(i,0x162471);return 0;
   }
   At(i,0x162302);var optionTable=ObjTable(option,boundaries,i.At);
   At(i,0x162308);uint skillId=Call(i,delegate{return boundaries.OptionIdA0(Need(optionTable.Read(i.At),i.At),option);});
   At(i,0x162310);object manager=boundaries.ObjectManager609098();
   At(i,0x16231b);object skill=boundaries.Skill078240(manager,skillId);
   At(i,0x162323);if(skill==null){At(i,0x162471);return 0;}
   At(i,0x16232c);byte tail=frame.TailByte.Read(i.At);
   At(i,0x162334);var objectId=frame.ObjectId.Snapshot();
   bool skipFirst=false;
   if(tail!=0){At(i,0x16233e);skipFirst=boundaries.Context.Object(skill,i.At).SkillB0.Read(i.At)==1;}
   if(!skipFirst){
    // The state vptr is captured BEFORE the repeated option-ID virtual call.
    // That call may mutate the live state table, option or argument pointers.
    At(i,tail!=0?0x162347u:0x16237du);optionTable=ObjTable(option,boundaries,i.At);
    At(i,tail!=0?0x16234du:0x162383u);var stateTable=StateTable(state,i.At);
    At(i,tail!=0?0x162350u:0x162386u);skillId=Call(i,delegate{return boundaries.OptionIdA0(Need(optionTable.Read(i.At),i.At),option);});
    At(i,tail!=0?0x16236eu:0x1623a4u);
    byte accepted=boundaries.Skill260(Need(stateTable.Read(i.At),i.At),state,a,b.Snapshot(),skillId,position.Snapshot(),objectId.Snapshot(),i.At);
    if(accepted!=0){At(i,0x162471);return 0;}
   }
   At(i,0x1623b5);byte useDefault=boundaries.UseDefault4910C0(skill);
   if(useDefault!=0){
    role=RefreshRole(state,boundaries,i,0x1623c2,0x1623c9,0x1623cd,0x1623d2,0x1623d6);
    At(i,0x1623e3);var defaultTable=boundaries.Context.Role(role,i.At).SkillManagerVirtual850.Snapshot();
    At(i,0x1623e6);skillId=Call(i,delegate{return boundaries.DefaultSkillE0(Need(defaultTable.Read(i.At),i.At),role,0);});
    At(i,0x1623ec);var stateTable=StateTable(state,i.At);
    At(i,0x162407);boundaries.Skill260(Need(stateTable.Read(i.At),i.At),state,a,b.Snapshot(),skillId,position.Snapshot(),objectId.Snapshot(),i.At);
   }
   At(i,0x162471);return 0; // Every observed normal Secondary return clears AL.
  }

  public static byte ActivateSuper1624B0(PlayerMouseState state,byte a,byte b,MouseSelectorFrame frame,PlayerMouseBoundaries boundaries,Invocation invocation){
   return ActivateSuperCaptured(state,a,Field<byte>.Of(b),frame,boundaries,invocation);
  }
  static byte ActivateSuperCaptured(PlayerMouseState state,byte a,Field<byte> b,MouseSelectorFrame frame,PlayerMouseBoundaries boundaries,Invocation invocation){
   var i=Need(invocation,0x1624c4);At(i,0x1624c4);bool missingRole=Need(state,i.At).CachedRole10.Read(i.At)==null;
   At(i,0x1624c9);var position=Need(frame,i.At).Position.Snapshot();
   if(missingRole){
    At(i,0x1624d9);var owner=state.Owner08.Snapshot();At(i,0x1624dd);var resolved=Need(boundaries,i.At).ResolvePlayer128530(owner.Read(i.At));
    At(i,0x1624e2);state.CachedRole10.Write(resolved);
   }
   // RBX retains this role across both getters and all later callbacks. A late
   // mutation of state.CachedRole10 does not select a different role for them.
   At(i,0x1624e6);var role=state.CachedRole10.Read(i.At);
   At(i,0x1624ea);var roleView=Need(boundaries,i.At).Context.Role(role,i.At);
   if(roleView.Byte4A70.Read(i.At)!=0){At(i,0x1624f3);return 0;}
   At(i,0x162506);var first=boundaries.DisplayedSkillSet3EAE40(role);
   At(i,0x16250b);if(first==null){At(i,0x1625af);return 0;}
   At(i,0x162514);if(first.Queued00.Read(i.At)==null){At(i,0x1625af);return 0;}
   At(i,0x16252a);var selected=Field<object>.Of(null);
   At(i,0x16252c);var second=boundaries.DisplayedSkillSet3EAE40(role);
   At(i,0x162534);if(second!=null){
    At(i,0x162539);selected=second.Queued00.Snapshot();At(i,0x16253c);
    if(selected.Read(i.At)!=null){At(i,0x162541);second.Queued00.Write(null);}
    else{
     // This MOV reads the HotSlotCtrl owner POINTER at role+4A48. Do not
     // substitute the captured role itself for this distinct argument.
     At(i,0x16254a);var owner=roleView.HotSlotOwner4A48.Snapshot();
     At(i,0x162551);byte alternate=boundaries.AlternateConfig3E2570(owner.Read(i.At));
     if(alternate!=0){At(i,0x16255a);selected=second.Alternate60.Snapshot();}
     else{At(i,0x162560);selected=second.Alternate58.Snapshot();}
    }
   }
   At(i,0x162569);roleView.Byte4A61.Write(0);
   At(i,0x162570);object option=selected.Read(i.At);if(option==null){At(i,0x1625af);return 0;}
   At(i,0x162575);var optionTable=ObjTable(option,boundaries,i.At);
   At(i,0x16257b);var stateTable=StateTable(state,i.At);
   At(i,0x16257e);uint skillId=Call(i,delegate{return boundaries.OptionIdA0(Need(optionTable.Read(i.At),i.At),option);});
   // Unlike Secondary, the ObjectId pointer is first captured only after the
   // option-ID callback. Position remains its original entry-MOV snapshot.
   At(i,0x162592);var objectId=frame.ObjectId.Snapshot();
   At(i,0x1625a1);byte accepted=boundaries.Skill260(Need(stateTable.Read(i.At),i.At),state,a,b.Snapshot(),skillId,position.Snapshot(),objectId.Snapshot(),i.At);
   if(accepted!=0){At(i,0x1625ab);return 1;}At(i,0x1625af);return 0;
  }
 }
}
